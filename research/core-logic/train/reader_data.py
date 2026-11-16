"""What the heads train on (docs/research/core-logic/specs/reader.md, rung 4): one note's cached encoder outputs, its
pieces, the dates and amounts the plain-code parsers found, and the targets built from its gold labels.

Inputs, all written by the .NET tools (the loader's only door is `notes export`):
- an export folder: notes.jsonl and manifest.json (`notes export`), values.jsonl (`reader values`);
- the cache folder of the 8-bit encoder: <id>.npz (tokens T x 768, note 768, 16-bit floats) and <id>.json (pieces);
- the label space (`notes label-space`).

What carries a loss (reader.md, "What carries a loss"): only the elements of a note's gold claim types; an actor
`unknown` is not a target; a gold value is matched to a parser span when the quotes overlap and the normal forms are
equal, and a gold value the parsers missed carries no role loss; a blanked field carries none.
"""
from __future__ import annotations

import json
import os
from dataclasses import dataclass, field

import numpy as np

FIELDS = ["subject", "facts", "requests", "documents"]
POLARITIES = ["notMentioned", "stated", "denied", "disputed"]
ACTORS = ["plaintiff", "defendant", "other", "unknown"]
DISPUTERS = ["plaintiff", "defendant", "other"]
OUTSIDE = "outside"


class Space:
    """The label lists, in a fixed order: the heads' rows follow it."""

    def __init__(self, path: str):
        with open(path, encoding="utf-8") as f:
            d = json.load(f)
        self.claims = [c["id"] for c in d["claims"]] + [OUTSIDE]
        self.claim_index = {c: i for i, c in enumerate(self.claims)}
        self.elements = [(c["id"], e["kind"]) for c in d["claims"] for e in c["elements"]]
        self.element_index = {e: i for i, e in enumerate(self.elements)}
        self.element_takes = {(c["id"], e["kind"]): (e["date"], e["amount"]) for c in d["claims"] for e in c["elements"]}
        self.elements_of = {c["id"]: [(c["id"], e["kind"]) for e in c["elements"]] for c in d["claims"]}
        self.roles_of = {c["id"]: [(r["relation"], r["position"]) for r in c["roles"]] for c in d["claims"]}
        self.party_claims = [c for c in self.claims[:-1] if len(self.roles_of[c]) > 1]
        self.requests = list(d["requests"])
        self.request_index = {r: i for i, r in enumerate(self.requests)}
        self.slots = d["slots"]
        self.class_slots = [s for s in self.slots if s["type"] != "date"]
        self.date_slots = [s["id"] for s in self.slots if s["type"] == "date"]
        kinds = sorted({k for _, k in self.elements})
        # Value roles: the event kinds, the request kinds and the date slots, one learned vector each.
        self.value_roles = [f"event:{k}" for k in kinds] + [f"request:{r}" for r in self.requests] + [f"slot:{s}" for s in self.date_slots]
        self.value_role_index = {r: i for i, r in enumerate(self.value_roles)}

    def slot_classes(self, slot: dict) -> list[str]:
        return ["yes", "no", "unknown"] if slot["type"] == "yesNoUnknown" else list(slot["choices"]) + ["unknown"]

    def candidate_roles(self, kind: str, claims: list[str]) -> list[str]:
        """The roles a date or an amount may take in a note of these claim types (reader.md, "Values to roles")."""
        roles = []
        for c in claims:
            for (_, k) in self.elements_of.get(c, []):
                date, amount = self.element_takes[(c, k)]
                if (kind == "date" and date) or (kind == "amount" and amount):
                    r = f"event:{k}"
                    if r not in roles:
                        roles.append(r)
        if kind == "amount":
            roles += [f"request:{r}" for r in self.requests]
        else:
            roles += [f"slot:{s}" for s in self.date_slots]
        return roles

    def role_name(self, role: str) -> str:
        """The role as the contract writes it: the event kind, the request kind or the slot id."""
        return role.split(":", 1)[1]


@dataclass
class Note:
    id: str
    tokens: np.ndarray            # T x 768, float32
    vector: np.ndarray            # 768
    fields: np.ndarray            # T, the field of each piece (0 to 3)
    starts: np.ndarray            # T
    lengths: np.ndarray           # T
    text: dict
    values: list                  # the parsers' values: dicts kind, normalized, field, start, length
    writer: str = ""
    gold: dict | None = None
    blanked: set = field(default_factory=set)


def load_export(folder: str, cache: str) -> tuple[list[Note], dict]:
    with open(os.path.join(folder, "manifest.json"), encoding="utf-8") as f:
        manifest = json.load(f)
    values = {}
    with open(os.path.join(folder, "values.jsonl"), encoding="utf-8") as f:
        for line in f:
            if line.strip():
                v = json.loads(line)
                values[v["id"]] = v["values"]
    notes = []
    with open(os.path.join(folder, "notes.jsonl"), encoding="utf-8") as f:
        for line in f:
            if not line.strip():
                continue
            n = json.loads(line)
            z = np.load(os.path.join(cache, n["id"] + ".npz"))
            with open(os.path.join(cache, n["id"] + ".json"), encoding="utf-8") as g:
                pieces = json.load(g)["pieces"]
            tokens = z["tokens"].astype(np.float32)
            if len(pieces) != tokens.shape[0]:
                raise ValueError(f"{n['id']}: {len(pieces)} pieces against {tokens.shape[0]} vectors")
            notes.append(Note(
                id=n["id"], tokens=tokens, vector=z["note"].astype(np.float32).reshape(-1),
                fields=np.array([FIELDS.index(p["field"]) if p["field"] in FIELDS else 0 for p in pieces], dtype=np.int64),
                starts=np.array([p["Start"] for p in pieces], dtype=np.int64),
                lengths=np.array([p["Length"] for p in pieces], dtype=np.int64),
                text=n["text"], values=values.get(n["id"], []),
                writer=((n.get("provenance") or {}).get("writer") or {}).get("family", ""),
                gold=n.get("gold"), blanked=set(n.get("blanked") or [])))
    return notes, manifest


def piece_mask(note: Note, quotes: list) -> np.ndarray:
    """1 for each piece that overlaps one of the quotes (field and UTF-16 range)."""
    m = np.zeros(len(note.fields), dtype=np.float32)
    for q in quotes or []:
        if q.get("start") is None or q.get("field") not in FIELDS:
            continue
        f, a, b = FIELDS.index(q["field"]), q["start"], q["start"] + q["length"]
        hit = (note.fields == f) & (note.lengths > 0) & (note.starts < b) & (note.starts + note.lengths > a)
        m[hit] = 1.0
    return m


def value_pieces(note: Note, v: dict) -> np.ndarray:
    f, a, b = FIELDS.index(v["field"]), v["start"], v["start"] + v["length"]
    idx = np.nonzero((note.fields == f) & (note.lengths > 0) & (note.starts < b) & (note.starts + note.lengths > a))[0]
    return idx


@dataclass
class Targets:
    claims: np.ndarray                       # 22 multi-hot
    claim_evidence: np.ndarray | None        # T, for the gold claims' queries
    elements: list                           # (element index, polarity, actor or -1, disputer or -1, evidence mask or None)
    requests: np.ndarray                     # 24, 1 asked / 0 not / -1 no loss
    request_evidence: dict                   # request index -> mask
    slots: list                              # (class-slot index, class index, evidence mask or None)
    parties: list                            # (party-claim index, role index, evidence mask or None)
    value_roles: list                        # (value index, role index, 0/1)


def targets(space: Space, note: Note) -> Targets:
    g = note.gold
    claims_gold = list(g["claims"])
    claims = np.zeros(len(space.claims), dtype=np.float32)
    for c in claims_gold:
        if c in space.claim_index:
            claims[space.claim_index[c]] = 1.0
    claim_evidence = piece_mask(note, g.get("claimEvidence")) if g.get("claimEvidence") else None

    elements = []
    gold_events = {(e["claim"], e["kind"]): e for e in g.get("events", [])}
    for c in claims_gold:
        for key in space.elements_of.get(c, []):
            path = f"events/{key[0]}/{key[1]}/polarity"
            if path in note.blanked:
                continue
            e = gold_events.get(key)
            pol = POLARITIES.index(e["polarity"]) if e else 0
            actor = ACTORS.index(e.get("actor", "unknown")) if e and e.get("actor", "unknown") != "unknown" else -1
            disp = DISPUTERS.index(e["disputedBy"]) if e and e["polarity"] == "disputed" and e.get("disputedBy") in DISPUTERS else -1
            ev = piece_mask(note, e.get("evidence")) if e and pol != 0 and e.get("evidence") else None
            elements.append((space.element_index[key], pol, actor, disp, ev))

    requests = np.zeros(len(space.requests), dtype=np.float32)
    request_evidence = {}
    for r in g.get("requests", []):
        if r["kind"] in space.request_index:
            i = space.request_index[r["kind"]]
            requests[i] = 1.0
            if r.get("evidence"):
                request_evidence[i] = piece_mask(note, r["evidence"])
    for b in note.blanked:
        if b.startswith("requests/") and b.split("/")[1] in space.request_index:
            requests[space.request_index[b.split("/")[1]]] = -1.0

    slots = []
    gold_slots = {s["slot"]: s for s in g.get("slots", [])}
    for i, s in enumerate(space.class_slots):
        if s["id"] not in gold_slots or f"slots/{s['id']}" in note.blanked:
            continue
        value = gold_slots[s["id"]]["value"]
        classes = space.slot_classes(s)
        if value not in classes:
            continue
        ev = piece_mask(note, gold_slots[s["id"]].get("evidence")) if value != "unknown" else None
        slots.append((i, classes.index(value), ev))

    parties = []
    for r in g.get("roles", []):
        if r["claim"] in space.party_claims and f"roles/{r['claim']}" not in note.blanked:
            pair = (r["relation"], r["position"])
            if pair in space.roles_of[r["claim"]]:
                parties.append((space.party_claims.index(r["claim"]), space.roles_of[r["claim"]].index(pair), piece_mask(note, r.get("evidence"))))

    # Value roles: each parser value matched to a gold value (overlapping quotes, the same normal form).
    value_roles = []
    gold_values = g.get("values", [])
    for vi, v in enumerate(note.values):
        match = [gv for gv in gold_values if gv["kind"] == v["kind"] and gv.get("normalized") == v["normalized"]
                 and gv["at"].get("field") == v["field"] and gv["at"].get("start") is not None
                 and gv["at"]["start"] < v["start"] + v["length"] and v["start"] < gv["at"]["start"] + gv["at"]["length"]]
        if not match:
            continue  # a parser value the gold does not hold, or holds differently: no loss
        if any(b.startswith(f"values/{v['field']}:") and b.endswith("/roles") for b in note.blanked):
            continue
        gold_roles = set()
        for gv in match:
            gold_roles |= set(gv.get("roles") or [])
        for role in space.candidate_roles(v["kind"], claims_gold):
            value_roles.append((vi, space.value_role_index[role], 1.0 if space.role_name(role) in gold_roles else 0.0))
    return Targets(claims, claim_evidence, elements, requests, request_evidence, slots, parties, value_roles)
