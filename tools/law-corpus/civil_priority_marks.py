#!/usr/bin/env python3
"""Account for all53 selected Ministry punctuation flags without changing a legal character."""
from __future__ import annotations

from collections import Counter

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def main() -> None:
    path = WORK / "priority-ministry-mark-readings.json"
    review = pipeline.read_json(path)
    for name, sha in review["inputFiles"].items():
        file = (WORK / name).resolve()
        if not file.is_relative_to(WORK.resolve()) or pipeline.hash_file(file) != sha:
            raise ValueError("Ministry mark review input changed")
    native = {a["candidateKey"]: a for a in pipeline.read_json(WORK / "native-candidates.json")["articles"]}
    bodies = {a["number"]: a for a in pipeline.read_json(WORK / "priority-collated-body-projection.json")["projectedCandidates"]}
    results = pipeline.read_json(WORK / "ministry-page-bounded-exact-audit.json")["results"]
    expected = {(r["candidateKey"], i): flag for r in results if r["numberScope"] == "main" and r["printedNumber"] in bodies
                for i, flag in enumerate(r["disagreements"]) if flag["punctuationOnly"]}
    readings = {(r["candidateKey"], r["flagIndex"]): r for r in review["readings"]}
    if len(readings) != len(review["readings"]) or set(readings) != set(expected) or len(readings) != 53:
        raise ValueError("Every scoped Ministry punctuation flag needs exactly one decision")
    for key, reading in readings.items():
        body, raw = bodies[reading["number"]], native[key[0]]
        if (reading["flag"] != expected[key] or reading["inspection"] != "actually read; not human/lawyer verification"
                or not reading["note"].strip() or reading["inputTextSha256"] != pipeline.hash_text(raw["text"])
                or reading["projectedTextSha256"] != pipeline.hash_text(body["text"])):
            raise ValueError("Ministry mark decision is stale or has no actual reading")
        callout = reading["finding"] == "editorial-callout-already-separated"
        if callout:
            if (reading["number"] not in {"455", "483", "489", "490", "492", "503"}
                    or reading["flag"]["canonical"] not in {"(", ")"}
                    or not any(m["kind"] == "footnote-callout" for m in body["separatedCompilationMetadata"])):
                raise ValueError("A legal number cannot be relabeled as a separated editorial callout")
        elif reading["finding"] != "source-visible-body-mark-retained":
            raise ValueError("Unknown Ministry punctuation treatment")
        if not reading["proofs"] or {p["pdfPage"] for p in reading["proofs"]} != set(raw["pages"]):
            raise ValueError("Ministry mark proof misses a source page")
        for proof in reading["proofs"]:
            for name, hash_name in [("image", "imageSha256"), ("sourceImage", "sourceImageSha256")]:
                image = (pipeline.ROOT / proof[name]).resolve()
                if not image.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(image) != proof[hash_name]:
                    raise ValueError("Ministry mark image proof changed")
    findings = dict(Counter(r["finding"] for r in readings.values()))
    if findings != {"source-visible-body-mark-retained": 41, "editorial-callout-already-separated": 12}:
        raise ValueError("Ministry body-mark/editorial-callout partition changed")
    pipeline.write_json(WORK / "priority-ministry-mark-audit.json", {
        "reviewSha256": pipeline.hash_file(path), "method": __doc__.strip(),
        "flagsAccountedFor": 53, "findings": findings, "punctuationChanges": 0,
        "unconfirmedPunctuationFlags": 0, "typedCivilChanged": False, "fullArticleChecked": False})
    print("53 Ministry flags replayed:41visible legal marks retained,12editorial parentheses already separated")


if __name__ == "__main__":
    main()
