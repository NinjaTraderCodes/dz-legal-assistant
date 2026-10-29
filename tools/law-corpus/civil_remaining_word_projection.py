#!/usr/bin/env python3
"""Replay the63 actually read priority discrepancies and project source-bound spaces in84 research bodies.

This is not a typed import or full-article certificate. Ordinary OCR join agreement does not independently
certify native word boundaries; annotations, paragraphs, current amendments and temporal limits remain separate.
"""
from __future__ import annotations

from collections import defaultdict

import civil_flag_spacing
import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def main() -> None:
    review_path = WORK / "image-readings-priority-remaining.json"
    review = pipeline.read_json(review_path)
    for key, name in [("nativeSha256", "native-candidates.json"), ("auditSha256", "ministry-page-bounded-exact-audit.json"),
                      ("priorityInventorySha256", "priority-chapter-inventory.json")]:
        if review[key] != pipeline.hash_file(WORK / name):
            raise ValueError("Priority remaining review belongs to another input/audit/inventory")
    native = pipeline.read_json(WORK / "native-candidates.json")
    if review["sourceSha256"] != native["source"]["sha256"]:
        raise ValueError("Priority remaining reading belongs to another official source")
    bodies = {a["candidateKey"]: a for a in native["articles"]}
    audit = pipeline.read_json(WORK / "ministry-page-bounded-exact-audit.json")
    selected = review["selectedOperativeCandidates"]
    if len(selected) != 84 or len(set(selected)) != 84 or len(review["excludedCurrentRepealMarkers"]) != 37:
        raise ValueError("Priority remaining selection/repeal inventory changed")
    expected = {f"{r['number']}#{i}": d for r in audit["results"] if r["number"] in selected
                for i, d in enumerate(d for d in r["disagreements"] if not d["punctuationOnly"])}
    readings = {r["itemId"]: r for r in review["readings"]}
    if set(expected) != set(readings) or len(readings) != len(review["readings"]) or len(readings) != 63:
        raise ValueError("Every selected discrepancy needs its own actual source reading")
    changes = defaultdict(list)
    for reading in readings.values():
        flag = expected[reading["itemId"]]
        if (any(reading[k] != flag[k] for k in ("kind", "at", "canonical")) or reading["ocrExtra"] != flag.get("ocr")
                or reading["inputTextSha256"] != pipeline.hash_text(bodies[reading["candidateKey"]]["text"])
                or reading["inspection"] != "actually read; not human/lawyer verification"
                or not reading["note"].strip() or not reading["images"]
                or len(reading["images"]) != len(reading["imageSha256s"])):
            raise ValueError("Priority reading has stale body/flag data or lacks actual source proof")
        for image, sha in zip(reading["images"], reading["imageSha256s"]):
            path = (pipeline.ROOT / image).resolve()
            if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Priority image proof changed or escaped the workspace")
        if reading["proposal"] is not None:
            proposal = reading["proposal"]
            if (reading["finding"] != "source-read-spacing-restoration-proposed" or flag["kind"] != "unconfirmed"
                    or proposal["old"] != flag["canonical"]):
                raise ValueError("Spacing proposal does not address its actual source flag")
            changes[reading["candidateKey"]].append({"old": proposal["old"], "new": proposal["new"], "at": flag["at"]})
        elif reading["finding"] not in {"source-read-editorial-callout-retained", "source-read-ocr-duplicate-or-punctuation-artifact",
                                        "source-read-outside-next-article-label", "source-read-outside-heading-or-footer", "source-read-native-retained"}:
            raise ValueError("Unknown non-repair priority finding")
    output = []
    for key in selected:
        value = civil_flag_spacing.project(bodies[key], changes[key])
        output.append({"candidateKey": key, "number": bodies[key]["number"], "numberScope": "main", **value,
                       "qaState": "saved discrepancy groups covered; word-boundary/annotation/paragraph/amendment/date QA remains"})
    count = sum(len(v["appliedSpacingOperations"]) for v in output)
    if count != 21:
        raise ValueError("The independently source-read proposal inventory changed")
    pipeline.write_json(WORK / "priority-remaining-word-projection.json", {
        "method": __doc__.strip(), "reviewSha256": pipeline.hash_file(review_path),
        "nativeSha256": review["nativeSha256"], "auditSha256": review["auditSha256"],
        "priorityInventorySha256": review["priorityInventorySha256"],
        "sourceDiscrepanciesActuallyReadAndReplayed": len(readings), "spacingRepairsApplied": count,
        "projectedCandidates": output, "excludedCurrentRepealMarkers": review["excludedCurrentRepealMarkers"],
        "knownWordBoundaryFollowup": {"candidateKey": "main:456", "raw": "الا قتصادي", "sourceWord": "الاقتصادي",
            "note": "Observed on both the Ministry78 and original84-21 print. The older diagnostic can accept an identical-letter OCR join; absence of a flag is not a word-boundary certificate. Needs its own exact source-bound projection."},
        "typedCivilChanged": False, "fullArticleChecked": False})
    print("84 separate research bodies;63 source findings replayed;21 token-anchored spacing repairs; no typed import")


if __name__ == "__main__":
    main()
