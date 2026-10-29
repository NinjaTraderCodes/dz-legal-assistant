#!/usr/bin/env python3
"""Reconcile the source-read Civil instruction inventory with compilation annotations, without applying bodies.

An explicit partial quotation cannot be promoted to a whole-article replacement. Source/body collation,
consolidation and temporal applicability remain separate gates, even when the target inventory agrees.
"""
from __future__ import annotations

from collections import Counter

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
ANNOTATION = {"replace": "(معدلة)", "replace-paragraphs": "(معدلة)",
              "insert": "(جديدة)", "repeal": "(ملغاة)"}
CITATIONS = {"law-84-21": "21-84", "law-05-10": "10-05", "law-07-05": "05-07"}
SOURCE_IDS = {"law-84-21": "A1984072", "law-05-10": "A2005044", "law-07-05": "A2007031"}


def target_map(operations: list[dict], selected: set[str]) -> dict:
    result, ids = {}, set()
    for op in operations:
        if (not op["id"] or op["id"] in ids or op["publicationId"] not in CITATIONS
                or op["operation"] not in ANNOTATION or not op["numbers"]
                or not op["pdfPages"] or not isinstance(op["instructionArticle"], int)):
            raise ValueError("Unknown, duplicate or unlocated amendment operation")
        ids.add(op["id"])
        if op["sourceId"] != SOURCE_IDS[op["publicationId"]]:
            raise ValueError("Amendment instruction is bound to another official publication")
        for number in op["numbers"]:
            if number not in selected or number in result:
                raise ValueError("Amendment target is duplicated or outside the selected chapters")
            if (number == "489") != (op["operation"] == "replace-paragraphs"):
                raise ValueError("The source-read489 remainder cannot be replaced as a whole article")
            if op["operation"] == "replace-paragraphs" and (
                    op["publicationId"] != "law-07-05" or op["instructionArticle"] != 6
                    or op.get("replacedParagraphs") != [1] or op.get("retainedParagraphs") != [2]):
                raise ValueError("The partial489 operation must preserve its source-read second paragraph")
            result[number] = op
    return result


def reconcile(operations: list[dict], bodies: list[dict], selected: set[str]) -> dict:
    rows = {a["number"]: a for a in bodies}
    if len(rows) != len(bodies) or set(rows) != selected:
        raise ValueError("Every selected identity needs exactly one current layout projection")
    targets = target_map(operations, selected)
    annotated = {n for n, a in rows.items() if a["sourceStatusAnnotation"] is not None}
    if set(targets) != annotated:
        raise ValueError("Source instruction targets and compilation annotations disagree")
    for number, op in targets.items():
        body = rows[number]
        if body["sourceStatusAnnotation"] != ANNOTATION[op["operation"]]:
            raise ValueError("Instruction action disagrees with the compilation status label")
        is_repeal = op["operation"] == "repeal"
        if (is_repeal != (body["compilationBodyKind"] == "repeal-marker")
                or is_repeal and (body["text"] or body["paragraphs"])
                or not is_repeal and not body["text"]):
            raise ValueError("A repeal marker cannot substitute for an operative or former body")
        notes = [m for m in body["separatedCompilationMetadata"] if m["kind"] == "footnote-callout"]
        if len(notes) != 1 or "بالقانون رقم " + CITATIONS[op["publicationId"]] not in notes[0]["footnoteText"]:
            raise ValueError("Compilation citation names another amending publication")
        if number == "489" and len(body["paragraphs"]) != 2:
            raise ValueError("The second489paragraph must remain present before body collation")
    return {"selectedIdentities": len(selected), "explicitCurrentBodyTargets": len(targets),
            "untargetedSelectedIdentities": len(selected - set(targets)),
            "countsByOperation": dict(Counter(op["operation"] for op in targets.values())),
            "countsByPublication": dict(Counter(op["publicationId"] for op in targets.values())),
            "repealTargets": sorted(n for n, op in targets.items() if op["operation"] == "repeal"),
            "bodyCollationPending": True, "typedCivilChanged": False, "fullArticleChecked": False}


def main() -> None:
    path = WORK / "priority-amendment-inventory.json"
    review = pipeline.read_json(path)
    for name, sha in review["inputFiles"].items():
        item = (WORK / name).resolve()
        if not item.is_relative_to(WORK.resolve()) or pipeline.hash_file(item) != sha:
            raise ValueError("Current amendment inventory input changed")
    sources = {s["id"]: s for s in review["sources"]}
    manifest = pipeline.read_json(pipeline.ROOT / "tools/law-corpus/civil-full-manifest.json")
    pinned = {s["id"]: s["sha256"] for s in manifest["sources"]}
    pinned["A1984072"] = "0bbf0e406386b9d709c855a855c0d362c42de549d2a6f31c26b35d3cc6b6383a"
    if set(sources) != {"A1983005", "A1988018", "A1989006", "A2005044", "A2007031", "A1984072"}:
        raise ValueError("Every discovered Civil-amending source must be accounted for")
    for source in sources.values():
        pdf = (pipeline.ROOT / source["cachePath"]).resolve()
        pipeline.official_url(source["url"])
        if (not pdf.is_relative_to(pipeline.CACHE.resolve()) or source["sha256"] != pinned[source["id"]]
                or pipeline.hash_file(pdf) != source["sha256"]):
            raise ValueError("Official amendment source pin changed")
    proofs = {(p["sourceId"], p["pdfPage"]): p for p in review["imagesActuallyRead"]}
    if len(proofs) != len(review["imagesActuallyRead"]) or len(proofs) != 23:
        raise ValueError("Instruction source-page reading inventory changed")
    for proof in proofs.values():
        image = (pipeline.ROOT / proof["image"]).resolve()
        if (proof["sourceSha256"] != sources[proof["sourceId"]]["sha256"]
                or not image.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(image) != proof["sha256"]):
            raise ValueError("Instruction image proof changed")
    for op in review["operations"]:
        if any((op["sourceId"], p) not in proofs for p in op["pdfPages"]):
            raise ValueError("Instruction lacks an actual source page reading")
    exclusions = review["selectedBodyExclusions"]
    if {r["sourceId"] for r in exclusions} != {"A1983005", "A1988018", "A1989006"} or len(exclusions) != 3:
        raise ValueError("The three explicitly out-of-scope body amendments need separate source findings")
    for row in exclusions:
        if (row["selectedBodyTargets"] or not row["note"].strip()
                or any((row["sourceId"], p) not in proofs for p in row["pdfPages"])):
            raise ValueError("Out-of-scope amendment finding lacks source support")
    inventory = pipeline.read_json(WORK / "priority-chapter-inventory.json")
    selected = {n for c in inventory["chapters"] for n in c["articles"]}
    bodies = pipeline.read_json(WORK / "priority-layout-projection.json")["projectedCandidates"]
    audit = reconcile(review["operations"], bodies, selected)
    if (audit["countsByOperation"] != {"replace": 48, "replace-paragraphs": 1, "insert": 12, "repeal": 40}
            or audit["countsByPublication"] != {"law-84-21": 2, "law-05-10": 28, "law-07-05": 71}
            or audit["selectedIdentities"] != 274):
        raise ValueError("Independently source-read current target partition changed")
    pipeline.write_json(WORK / "priority-amendment-inventory-audit.json", {
        "method": __doc__.strip(), "inventorySha256": pipeline.hash_file(path),
        "instructionPagesReplayed": len(proofs), **audit})
    print("101target instructions reconciled:48full replacements,1partial,12insertions,40repeals; body collation pending")


if __name__ == "__main__":
    main()
