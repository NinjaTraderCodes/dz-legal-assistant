#!/usr/bin/env python3
"""Replay the prepared Family candidates and their two-source OCR comparison, without granting checks.

Uses the pinned Ministry compilation, the saved 40-crop manifest and hash-validated cached OCR artifacts.
Unchanged candidates are compared with JO24/1984; annotated replacement/insertion candidates with JO15/2005.
The five repeal markers need separate review of the amendment instruction. Headings, paragraph boundaries,
amendment inventory and tolerated comparison forms are not established by a token agreement.
"""
from __future__ import annotations

import argparse

import check_native
import pipeline
import prepare_family

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/family-current-manifest.json"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=pipeline.Path, default=WORK)
    args = parser.parse_args()
    native = prepare_family.build()
    pipeline.write_json(args.out / "native.json", native)
    base, amended, repealed = [], [], []
    for article in native["articles"]:
        if article["repealedBy0502"]:
            repealed.append(article["number"])
            continue
        label = "المادة " + ("الأولى" if article["number"] == "1" else article["number"])
        item = article | {"label": label}
        (amended if article["amendedBy0502"] else base).append(item)
    summaries = {}
    for name, articles, source in (("base-unchanged", base, "A1984024"), ("amended", amended, "A2005015")):
        inputs = args.out / f"{name}-input.json"
        pipeline.write_json(inputs, articles)
        comparison = check_native.run(articles, MANIFEST, source)
        comparison["inputSha256"] = pipeline.hash_file(inputs)
        comparison["verificationEffect"] = "none; differences, boundaries, paragraphing and legal operations need review"
        pipeline.write_json(args.out / f"{name}-comparison.json", comparison)
        summaries[name] = {"articles": comparison["articles"], "agreed": comparison["agreed"],
                           "comparisonSha256": pipeline.hash_file(args.out / f"{name}-comparison.json")}
        print(f"{name}: {comparison['agreed']}/{comparison['articles']} token comparisons agree")
    pipeline.write_json(args.out / "preparation-summary.json", {
        "nativeSha256": pipeline.hash_file(args.out / "native.json"),
        "manifestSha256": pipeline.hash_file(MANIFEST),
        "articles": len(native["articles"]), "comparisons": summaries,
        "repealMarkersNotComparedAsBodies": repealed,
        "verificationEffect": "none; this is preparation and comparison, not a typed corpus delivery",
        "remaining": ["Independent 05-02 operation inventory, including five repeals",
                      "Word/digit/negation discrepancy decisions against the source images",
                      "Paragraph boundaries, heading paths and French structural checks",
                      "Current typed delivery, legally required temporal handling and 20-article source sampling"],
    })
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
