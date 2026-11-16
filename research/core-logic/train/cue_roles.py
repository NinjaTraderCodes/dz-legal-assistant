"""Dates and amounts with their role, by the cue table (the owner's route experiment, 17 November 2026).

From a model's readings, two new sets of readings in which only what each date and amount is for changes:
- `cues`: the cue table and the parsers alone, no learning. A value takes the event kind whose trigger word (cues.json,
  `reader triggers`) stands nearest to it in its own sentence, among the event kinds of the reading's claim types that take
  that kind of value; a tie between two kinds, or no trigger, leaves it open (no role). A date linked to `notice_served`
  also answers `notice_date`, one linked to `contract_concluded` also `contract_date`. Request amounts have no cue: open.
- `hybrid`: the cue's role where it gives one; the model's roles where the cues leave the value open.
Each event's date and amount, each request's amount and the date slots are then read from the new roles (the decoder's
rule: the first value in the note with that role). Everything else is the model's reading, unchanged.

    python cue_roles.py --readings <folder with answers.jsonl, given.jsonl> --export <measure export> --triggers <triggers.jsonl>
                        --space <label-space.json> --out <folder> (writes <out>\\cues and <out>\\hybrid)
"""
from __future__ import annotations

import argparse
import copy
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reader_data import OUTSIDE, Space  # noqa: E402


def sentence(text: str, at: int) -> int:
    """The index of the sentence a position lies in (the comparison's rule: «.» not between digits, «؟», «?», «!», «؛», a line break)."""
    n = 0
    for i, c in enumerate(text[:at]):
        if c in "؟?!؛\n" or (c == "." and not (0 < i < len(text) - 1 and text[i - 1].isdigit() and text[i + 1].isdigit())):
            n += 1
    return n


def cue_role(space: Space, note_text: dict, value: dict, triggers: list, claims: list):
    kinds = {r.split(":", 1)[1] for r in space.candidate_roles(value["kind"], claims) if r.startswith("event:")}
    text = note_text.get(value["field"]) or ""
    s = sentence(text, value["start"])
    best = []
    for t in triggers:
        if t["kind"] not in kinds or t["field"] != value["field"] or sentence(text, t["start"]) != s:
            continue
        a0, a1, b0, b1 = value["start"], value["start"] + value["length"], t["start"], t["start"] + t["length"]
        d = 0 if (a0 < b1 and b0 < a1) else (b0 - a1 if b0 >= a1 else a0 - b1)
        best.append((d, t["kind"]))
    if not best:
        return None
    best.sort()
    if len(best) > 1 and best[0][0] == best[1][0] and best[0][1] != best[1][1]:
        return None
    kind = best[0][1]
    roles = [kind]
    if value["kind"] == "date" and kind == "notice_served":
        roles.append("notice_date")
    if value["kind"] == "date" and kind == "contract_concluded":
        roles.append("contract_date")
    return roles


def relink(space: Space, reading: dict, note_text: dict, triggers: list, hybrid: bool) -> dict:
    r = copy.deepcopy(reading)
    claims = [a["claimId"] for a in r["claim"]["answers"] if a["claimId"] != OUTSIDE]
    for v in r["values"]:
        flat = {"kind": v["kind"], "field": v["span"]["field"], "start": v["span"]["start"], "length": v["span"]["length"]}
        cue = cue_role(space, note_text, flat, triggers, claims)
        v["roles"] = cue if cue is not None else (v["roles"] if hybrid else [])

    def value_for(role, kind):
        for v in r["values"]:
            if v["kind"] == kind and role in v["roles"]:
                return dict(v)
        return None

    for e in r["events"]:
        e.pop("date", None)
        e.pop("amount", None)
        if e["polarity"] != "notMentioned":
            takes_date, takes_amount = space.element_takes.get((e["claim"], e["kind"]), (False, False))
            if takes_date and (d := value_for(e["kind"], "date")):
                e["date"] = d
            if takes_amount and (a := value_for(e["kind"], "amount")):
                e["amount"] = a
    for q in r["requests"]:
        q.pop("amount", None)
        if (a := value_for(q["kind"], "amount")):
            q["amount"] = a
    for s in r["slots"]:
        if s["slot"] in space.date_slots:
            d = value_for(s["slot"], "date")
            s.update({"value": d["normalized"] if d else "unknown", "evidence": [d["span"]] if d else []})
    r["info"] = dict(r["info"], version=r["info"]["version"] + ("+hybrid-cues" if hybrid else "+cues-only"))
    return r


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    for name in ("readings", "export", "triggers", "space", "out"):
        p.add_argument(f"--{name}", required=True)
    a = p.parse_args(argv)
    space = Space(a.space)
    texts = {}
    with open(os.path.join(a.export, "notes.jsonl"), encoding="utf-8") as f:
        for line in f:
            if line.strip():
                n = json.loads(line)
                texts[n["id"]] = n["text"]
    triggers = {}
    with open(a.triggers, encoding="utf-8") as f:
        for line in f:
            if line.strip():
                t = json.loads(line)
                triggers[t["id"]] = t["triggers"]
    for mode in ("cues", "hybrid"):
        out = os.path.join(a.out, mode)
        os.makedirs(out, exist_ok=True)
        for name in ("answers.jsonl", "given.jsonl"):
            with open(os.path.join(a.readings, name), encoding="utf-8") as f, open(os.path.join(out, name), "w", encoding="utf-8", newline="\n") as g:
                for line in f:
                    if line.strip():
                        x = json.loads(line)
                        x["reading"] = relink(space, x["reading"], texts[x["id"]], triggers.get(x["id"], []), mode == "hybrid")
                        g.write(json.dumps(x, ensure_ascii=False) + "\n")
        print(f"{mode} -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
