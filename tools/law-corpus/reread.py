#!/usr/bin/env python3
"""Third, targeted OCR reading for the tokens no stream confirmed in a check_native result.

Whole-column OCR loses things a narrow crop keeps: Tesseract drops short last lines and lines touching a kashida,
PP-OCRv5 drops final full stops. For each unconfirmed disagreement, the strip of the official column crop around it
(review_items.strips: from the nearest confirmed word before to the nearest after) is read again by Tesseract (best
model, page segmentation 6, at 1x and 2x). The disputed tokens count as confirmed only if this reading agrees with
them under the same rule (native_check.token_statuses), inside a window of the surrounding native text. Extra tokens
are never resolved here. Everything read is kept (strip PNG, OCR text, hashes) for the image review.

Usage: reread.py ARTICLES.json CHECK.json MANIFEST.json SOURCE --name NAME --out CHECKED.json
"""
from __future__ import annotations

import argparse
import subprocess

from PIL import Image

import check_native
import native_check as nc
import pipeline
import review_items

TESSERACT = pipeline.TOOLS / "ocr/Library/bin/tesseract.exe"
TESSDATA = pipeline.TOOLS / "ocr-best"
WINDOW = 6


def read_strip(path: pipeline.Path) -> str:
    result = subprocess.run([str(TESSERACT), str(path), "stdout", "-l", "ara", "--psm", "6", "--tessdata-dir", str(TESSDATA)],
                            capture_output=True, timeout=120)
    return result.stdout.decode("utf-8", errors="replace")


def confirms(canonical: list[str], first: int, last: int, ocr_text: str) -> bool:
    lo, hi = max(0, first - WINDOW), min(len(canonical), last + WINDOW + 1)
    status, _ = nc.token_statuses(canonical[lo:hi], nc.tokens(ocr_text))
    return all(status[i - lo] in nc.OK_STATES for i in range(first, last + 1))


def run(articles: list[dict], check: dict, manifest_path, source: str, name: str) -> dict:
    manifest = pipeline.load_manifest(manifest_path)
    folders = dict(check_native.evidence_folders(manifest, source))
    stream = check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(list(folders.items()))))
    by_number = {a["number"]: a for a in articles}
    out_dir = pipeline.CACHE / "reviews" / name / "reread"
    out_dir.mkdir(parents=True, exist_ok=True)
    for result in check["results"]:
        article = by_number[result["number"]]
        canonical = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        span = result["located"].get("tesseract")
        ocr = stream[span[0]:span[1]] if span else []
        pairs = review_items.matched_pairs(canonical, ocr)
        for k, d in enumerate(result["disagreements"]):
            if d["kind"] != "unconfirmed" or not ocr:
                continue
            first = d["at"]
            last = first + len(nc.tokens(d["canonical"])) - 1
            readings = []
            for n, region in enumerate(review_items.strips(ocr, pairs, first, last, folders)):
                crop = Image.open(folders[region["source"]] / "crop.png").crop(region["box"])
                for scale in (1, 2):
                    image = crop if scale == 1 else crop.resize((crop.width * 2, crop.height * 2), Image.LANCZOS)
                    path = out_dir / f"{result['number'].replace(' ', '_')}-{k}-{n}-x{scale}.png"
                    image.save(path)
                    text = read_strip(path)
                    readings.append({"evidenceId": region["source"], "box": region["box"], "scale": scale,
                                     "imageSha256": pipeline.hash_file(path), "ocr": text})
            d["reread"] = {"readings": readings, "confirmed": any(confirms(canonical, first, last, r["ocr"]) for r in readings)}
        open_items = [d for d in result["disagreements"] if not (d["kind"] == "unconfirmed" and d.get("reread", {}).get("confirmed"))]
        result["agreesAfterReread"] = bool(result["located"]) and not open_items
    agreed = sum(r["agreesAfterReread"] for r in check["results"])
    check["reread"] = {"rule": "reread.py: Tesseract best, psm 6, strip of the column crop at 1x and 2x; disputed tokens confirmed by "
                                "native_check.token_statuses in a window of 6 tokens; extras never resolved",
                       "agreed": agreed, "agreementRate": round(agreed / len(check["results"]), 4) if check["results"] else 0}
    return check


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("articles", type=pipeline.Path)
    parser.add_argument("check", type=pipeline.Path)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("source")
    parser.add_argument("--name", required=True)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    articles = pipeline.read_json(args.articles)
    if isinstance(articles, dict):
        articles = articles["quotedArticles"]
    result = run(articles, pipeline.read_json(args.check), args.manifest.resolve(), args.source, args.name)
    pipeline.write_json(args.out, result)
    left = sum(1 for r in result["results"] for d in r["disagreements"]
               if not (d["kind"] == "unconfirmed" and d.get("reread", {}).get("confirmed")))
    print(f"after re-read: {result['reread']['agreed']}/{len(result['results'])} agree ({result['reread']['agreementRate']:.1%}); {left} disagreements left")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
