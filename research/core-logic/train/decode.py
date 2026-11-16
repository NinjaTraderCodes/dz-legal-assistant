"""The decoder of phases B and C (docs/research/core-logic/specs/reader.md, "Decoding before calibration"): from the heads'
outputs to the contract's NoteReading, as the scorer reads it (`{"id", "reading"}` per line, camelCase, enums as
camelCase strings).

- claim types: the classes at 0.5 or more, at most the three highest; when none reaches 0.5, the single highest;
  `outside` only when it is the highest, and then alone;
- polarity, actor, who disputes, slots, parties: the highest class;
- requests and value roles: at 0.5 or more; an event's date (or amount) is the value with the highest role score of at
  least 0.5 for that event kind, the first in the note on a tie;
- evidence: the best contiguous stretch (the stretch that maximises the sum of sigmoid(u) - 0.5), within one field;
- no abstention and no rule vote: the decoder always answers, and the confidence is kept.

With `given` (the gold claim types), the claim answer is those types and the rest is read for them ("type given").
"""
from __future__ import annotations

import numpy as np
import torch

from reader_data import ACTORS, DISPUTERS, FIELDS, OUTSIDE, POLARITIES, Note, Space, value_pieces


def best_stretch(note: Note, u: np.ndarray) -> list:
    """The stretch of pieces within one field that maximises sum(sigmoid(u) - 0.5), as one TextSpan; [] when none is positive."""
    gain = 1.0 / (1.0 + np.exp(-u)) - 0.5
    best, best_span = 0.0, None
    run, start = 0.0, None
    for i in range(len(gain)):
        if note.lengths[i] == 0 or (start is not None and note.fields[i] != note.fields[start]):
            run, start = 0.0, None
            if note.lengths[i] == 0:
                continue
        if start is None or run <= 0:
            run, start = 0.0, i
        run += gain[i]
        if run > best:
            best, best_span = run, (start, i)
    if best_span is None:
        return []
    a, b = best_span
    s = int(note.starts[a])
    return [{"field": FIELDS[int(note.fields[a])], "start": s, "length": int(note.starts[b] + note.lengths[b]) - s}]


def answered_claims(space: Space, p: np.ndarray) -> list[int]:
    order = list(np.argsort(-p, kind="stable"))
    top = order[0]
    if space.claims[top] == OUTSIDE:
        return [top]
    chosen = [i for i in order[:3] if p[i] >= 0.5 and space.claims[i] != OUTSIDE]
    return chosen or [top]


def reading(space: Space, note: Note, out: dict, model, given: list[str] | None, info: dict) -> dict:
    sig = lambda t: torch.sigmoid(t).detach().numpy()  # noqa: E731
    pc = sig(out["claim"])
    claim_u = out["claim_u"].detach().numpy()
    if given is None:
        chosen = answered_claims(space, pc)
        claims = [space.claims[i] for i in chosen]
    else:
        claims = list(given)
        chosen = [space.claim_index[c] for c in claims if c in space.claim_index]
    party_probs = [torch.softmax(x, -1).detach().numpy() for x in out["party"]]
    answers = []
    for c in claims:
        i = space.claim_index.get(c)
        guess = {"claimId": c, "confidence": float(pc[i]) if i is not None else 1.0,
                 "evidence": best_stretch(note, claim_u[i]) if i is not None else []}
        roles = space.roles_of.get(c, [])
        if len(roles) == 1:
            guess["role"] = {"relation": roles[0][0], "position": roles[0][1], "confidence": float(guess["confidence"]),
                             "abstained": False, "evidence": guess["evidence"]}
        elif len(roles) > 1:
            k = space.party_claims.index(c)
            j = int(np.argmax(party_probs[k]))
            guess["role"] = {"relation": roles[j][0], "position": roles[j][1], "confidence": float(party_probs[k][j]),
                             "abstained": False, "evidence": best_stretch(note, out["party_u"][k].detach().numpy())}
        answers.append(guess)
    others = [{"claimId": space.claims[i], "confidence": float(pc[i]), "evidence": []}
              for i in np.argsort(-pc, kind="stable") if space.claims[i] not in claims][:5]

    # Values and their roles.
    inside = [c for c in claims if c != OUTSIDE]
    values = []
    role_scores = []  # per value: {role name: score}
    for vi, v in enumerate(note.values):
        scores = {}
        ctx = out["value_ctx"][vi] if vi < len(out["value_ctx"]) else None
        if ctx is not None:
            cands = space.candidate_roles(v["kind"], inside)
            if cands:
                s = torch.sigmoid(model.role_scores(ctx, torch.tensor([space.value_role_index[r] for r in cands]))).detach().numpy()
                scores = {space.role_name(r): float(x) for r, x in zip(cands, s)}
        role_scores.append(scores)
        values.append({"kind": v["kind"], "normalized": v["normalized"],
                       "span": {"field": v["field"], "start": v["start"], "length": v["length"]},
                       "roles": [r for r, x in scores.items() if x >= 0.5], "confidence": 1.0})

    def value_for(role: str, kind: str):
        best, best_i = 0.5, None
        for i, v in enumerate(note.values):
            x = role_scores[i].get(role, 0.0)
            if v["kind"] == kind and x >= best and (best_i is None or x > best):
                best, best_i = x, i
        if best_i is None:
            return None
        return dict(values[best_i], confidence=best)

    pol = torch.softmax(out["polarity"], -1).detach().numpy()
    act = torch.softmax(out["actor"], -1).detach().numpy()
    dis = torch.softmax(out["disputer"], -1).detach().numpy()
    element_u = out["element_u"].detach().numpy()
    events = []
    for c in inside:
        for key in space.elements_of.get(c, []):
            i = space.element_index[key]
            j = int(np.argmax(pol[i]))
            e = {"claim": key[0], "kind": key[1], "polarity": POLARITIES[j], "confidence": float(pol[i][j]), "abstained": False,
                 "actor": ACTORS[int(np.argmax(act[i]))], "evidence": [] if j == 0 else best_stretch(note, element_u[i])}
            if POLARITIES[j] == "disputed":
                e["disputedBy"] = DISPUTERS[int(np.argmax(dis[i]))]
            if j != 0:
                takes_date, takes_amount = space.element_takes[key]
                if takes_date and (d := value_for(key[1], "date")) is not None:
                    e["date"] = d
                if takes_amount and (a := value_for(key[1], "amount")) is not None:
                    e["amount"] = a
            events.append(e)

    slot_u = out["slot_u"].detach().numpy()
    slots = []
    for k, s in enumerate(space.class_slots):
        p = torch.softmax(out["slot"][k], -1).detach().numpy()
        j = int(np.argmax(p))
        value = space.slot_classes(s)[j]
        slots.append({"slot": s["id"], "value": value, "confidence": float(p[j]), "abstained": False,
                      "evidence": [] if value == "unknown" else best_stretch(note, slot_u[k])})
    for sid in space.date_slots:
        d = value_for(sid, "date")
        slots.append({"slot": sid, "value": d["normalized"] if d else "unknown", "confidence": d["confidence"] if d else 1.0,
                      "abstained": False, "evidence": [d["span"]] if d else []})

    pr = sig(out["request"])
    request_u = out["request_u"].detach().numpy()
    requests = []
    for i, kind in enumerate(space.requests):
        if pr[i] >= 0.5:
            r = {"kind": kind, "confidence": float(pr[i]), "evidence": best_stretch(note, request_u[i])}
            if (a := value_for(kind, "amount")) is not None:
                r["amount"] = a
            requests.append(r)

    return {"claim": {"answers": answers, "others": others, "abstained": False},
            "events": events, "slots": slots, "requests": requests, "values": values, "info": info}


def value_index_lists(note: Note) -> list:
    return [value_pieces(note, v) for v in note.values]
