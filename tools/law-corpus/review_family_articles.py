#!/usr/bin/env python3
"""Whole-article Family source views. Generation never records an inspection or resolves a discrepancy.

Usage: review_family_articles.py FIRST LAST --name NAME
Source boxes come from the cached OCR alignment, expanded above/below so dropped closing lines and printed labels
can be inspected. A missing or misaligned source is an explicit review problem, not an automatic match.
"""
from __future__ import annotations

import argparse

from PIL import Image

import check_native
import pipeline
import review_sheets

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/family-current-manifest.json"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("first", type=int)
    parser.add_argument("last", type=int)
    parser.add_argument("--name", required=True)
    parser.add_argument("--edition", choices=("jo", "ministry"), default="jo")
    parser.add_argument("--structure", action="store_true", help="Include paragraph-count differences even when body words agree")
    args = parser.parse_args()
    ministry = args.edition == "ministry"
    articles = pipeline.read_json(WORK / ("ministry-raw-input.json" if ministry else "base-unchanged-input.json"))
    audit = pipeline.read_json(WORK / ("ministry-body-exact-audit.json" if ministry else "base-unchanged-exact-audit.json"))
    structural = {r["number"] for r in pipeline.read_json(WORK / "french-structure.json")["paragraphCountDifferences"]} if args.structure else set()
    results = {r["number"]: r for r in audit["results"]}
    manifest = pipeline.ROOT / "tools/law-corpus/family-ministry-manifest.json" if ministry else MANIFEST
    source = "family-consolidated-bilingual" if ministry else "A1984024"
    folders = dict(check_native.evidence_folders(pipeline.load_manifest(manifest), source))
    out = pipeline.CACHE / "reviews" / args.name
    out.mkdir(parents=True, exist_ok=True)
    items = []
    for article in articles:
        number = article["number"]
        if ministry and (article["amendedBy0502"] or article["repealedBy0502"]):
            continue
        if not args.first <= int(number) <= args.last:
            continue
        result = results[number]
        if result.get("wordAgrees", result["agrees"]) and number not in structural:
            continue
        images, regions = [], []
        for k, region in enumerate(result["regions"]):
            image = Image.open(folders[region["evidenceId"]] / "crop.png")
            box = [0, max(0, region["box"][1] - 100), image.width, min(image.height, region["box"][3] + 150)]
            path = out / f"article-{number}-{k}.png"
            image.crop(box).save(path)
            images.append(str(path))
            regions.append({"evidenceId": region["evidenceId"], "box": box})
        highlights = [i for d in result["disagreements"] if d["kind"] == "unconfirmed"
                      for i in range(d["at"] - len(article["label"].split()) - 1,
                                     d["at"] - len(article["label"].split()) - 1 + len(check_native.nc.tokens(d["canonical"])))]
        items.append({"id": number, "number": number, "title": "Family " + number + " — whole article; NOT YET REVIEWED",
                      "text": article["text"], "paragraphs": article["paragraphs"], "images": images,
                      "imageSha256s": [pipeline.hash_file(pipeline.Path(p)) for p in images], "regions": regions,
                      "highlight": highlights, "textSha256": pipeline.hash_text(article["text"]),
                      "note": "Source: " + source + ". Compare the whole body and paragraph breaks; report clipped or wrong-line images. "
                              + str(len(result["disagreements"])) + " exact-audit items."})
    sheets = review_sheets.render(items, out, args.name, per_sheet=2)
    pipeline.write_json(out / "index.json", {"name": args.name, "source": source, "items": items,
                                           "sheets": sheets, "inspection": "not-inferred-from-generation"})
    print(len(items), "article views;", len(sheets), "sheets; no decisions inferred")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
