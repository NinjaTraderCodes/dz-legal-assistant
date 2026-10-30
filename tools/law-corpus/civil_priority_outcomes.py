#!/usr/bin/env python3
"""Check the274 Civil identities and current instruction outcomes independently of extracted numbering."""
from __future__ import annotations

from collections import Counter

import civil_priority_amendments as amendments
import pipeline
from scope_inventory import number_key

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
RANGES = {"general-contract": (54,123), "liability": (124,140), "sale": (351,412),
          "consumption-loan": (450,458), "lease": (467,537), "use-loan": (538,548), "works": (549,570)}
INSERTIONS = {"general-contract": {"72 مكرر"}, "liability": {"124 مكرر", "140 مكرر", "140 مكرر 1"},
              "lease": {"467 مكرر", "469 مكرر", "469 مكرر 1", "469 مكرر 2", "469 مكرر 3", "469 مكرر 4", "507 مكرر", "507 مكرر 1"}}


def expected() -> set[str]:
    return {str(n) for first,last in RANGES.values() for n in range(first,last+1)} | set().union(*INSERTIONS.values())


def audit(bodies: list[dict], operations: list[dict], native: dict) -> dict:
    selected = expected()
    rows = {a["number"]: a for a in bodies}
    if len(rows) != len(bodies) or set(rows) != selected:
        raise ValueError("Civil outcomes differ from the independent ranges and inserted identities")
    targets = amendments.target_map(operations, selected)
    if Counter(o["operation"] for o in targets.values()) != {"replace":48,"replace-paragraphs":1,"insert":12,"repeal":40}:
        raise ValueError("The current independently read operation inventory changed")
    output = []
    for number in sorted(selected, key=number_key):
        body = rows[number]
        if body["text"] != "\n".join(body["paragraphs"]):
            raise ValueError("Civil body and published paragraph projection disagree")
        op = targets.get(number)
        repeal = op is not None and op["operation"] == "repeal"
        if (repeal != (body["compilationBodyKind"] == "repeal-marker")
                or repeal and (body["text"] or body["paragraphs"]) or not repeal and not body["text"]):
            raise ValueError("A repeal outcome cannot stand in for an operative or former body")
        if number == "489" and (len(body["paragraphs"]) != 2 or "الباقي بدون تغيير" in body["text"]):
            raise ValueError("Partial489must preserve its original second paragraph")
        if len(native[number]["path"]) < 3:
            raise ValueError("Every selected identity needs its recorded book/title/chapter path")
        output.append({"number": number, "operation": op["operation"] if op else "unchanged-body",
            "publicationId": op["publicationId"] if op else "ordinance-75-58",
            "textSha256": pipeline.hash_text(body["text"]), "paragraphs": len(body["paragraphs"]),
            "path": native[number]["path"]})
    return {"identities":len(rows),"operations":dict(Counter(r["operation"] for r in output)),
            "currentIdentityAndOutcomeInventoryPassed":True,"historicalConsolidationComplete":False,
            "fullCivilComplete":False,"partialPackageChecked":False,"results":output}


def main() -> None:
    frozen = pipeline.read_json(WORK / "priority-chapter-inventory.json")
    chapters = {c["id"]:c for c in frozen["chapters"]}
    if set(chapters) != set(RANGES):
        raise ValueError("Civil whole-chapter selection changed")
    native = {a["number"]:a for a in pipeline.read_json(WORK / "native-candidates.json")["articles"] if a["numberScope"] == "main"}
    covered = []
    for name,(first,last) in RANGES.items():
        values = set(map(str,range(first,last+1))) | INSERTIONS.get(name,set())
        if set(chapters[name]["articles"]) != values or len(values) != len(chapters[name]["articles"]):
            raise ValueError("A source-defined Civil chapter is incomplete or includes its successor unit")
        prefix = native[str(first)]["path"][:3]
        if any(native[number]["path"][:3] != prefix for number in values):
            raise ValueError("A selected identity escaped its source-read book/title/chapter boundary")
        covered.append({"id":name,"path":prefix,"articles":sorted(values,key=number_key)})
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    bodies = pipeline.read_json(WORK / "priority-collated-body-projection.json")["projectedCandidates"]
    result = audit(bodies,inventory["operations"],native)
    result.update(method=__doc__.strip(), coveredChapters=covered,
                  inputFiles={name:pipeline.hash_file(WORK/name) for name in ["priority-chapter-inventory.json",
                      "priority-amendment-inventory.json","priority-collated-body-projection.json","native-candidates.json"]})
    pipeline.write_json(WORK / "priority-current-outcome-audit.json", result)
    print("274identities/7whole chapters;173unchanged bodies,48replacements,1partial,12insertions,40repeals; no checked-state grant")


if __name__ == "__main__":
    main()
