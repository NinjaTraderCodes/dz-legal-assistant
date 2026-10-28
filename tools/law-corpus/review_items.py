#!/usr/bin/env python3
"""Review items for the disagreements of a check_native result: for each one, the strip of the official column crop
between the nearest OCR-confirmed words before and after it (so a line Tesseract dropped is still shown), the native
text around it with the unconfirmed tokens highlighted, and what each OCR stream read there.

Usage: review_items.py ARTICLES.json CHECK.json MANIFEST.json SOURCE --name NAME [--per-sheet 8]
Writes strips and sheets under tools/law-corpus/.cache/evidence/reviews/NAME/ and an index NAME-items.json there.
The sheets decide nothing; decisions are recorded separately, bound to the strip hashes.
"""
from __future__ import annotations

import argparse
import difflib

from PIL import Image

import check_native
import native_check as nc
import pipeline
import review_sheets

CONTEXT = 8


def matched_pairs(canonical: list[str], ocr: list[dict]) -> list[tuple[int, int]]:
    matcher = difflib.SequenceMatcher(a=[nc.fold(t) for t in canonical], b=[nc.fold(t["token"]) for t in ocr], autojunk=False)
    return [(m.a + k, m.b + k) for m in matcher.get_matching_blocks() for k in range(m.size)]


def strips(ocr: list[dict], pairs: list[tuple[int, int]], first: int, last: int, folders: dict, tight: bool = False) -> list[dict]:
    """Crop regions (per evidence crop) from the confirmed word before `first` to the confirmed word after `last`.

    Default margins are those the targeted re-read (reread.py) was run with. `tight` (review sheets): a small margin,
    widened only on a side without a confirmed word, so a strip shows the disputed line at full resolution."""
    before = [b for a, b in pairs if a < first and ocr[b].get("box")]
    after = [b for a, b in pairs if a > last and ocr[b].get("box")]
    lo = ocr[before[-1]] if before else (ocr[after[0]] if after else None)
    hi = ocr[after[0]] if after else lo
    if lo is None:
        return []
    out = []
    for source in dict.fromkeys([lo["source"], hi["source"]]):
        crop = Image.open(folders[source] / "crop.png")
        top = lo["box"][1] if lo["source"] == source else 0
        bottom = hi["box"][3] if hi["source"] == source else crop.height
        if lo["source"] != hi["source"] and source == lo["source"]:
            bottom = crop.height
        # Two extra lines below: the disputed token often sits in the line after the last confirmed word (an article's
        # last line, which whole-column OCR tends to drop), and nothing confirmed follows it.
        up, down = (40, 150) if not tight else (160 if not before else 30, 160 if not after else 40)
        out.append({"source": source, "box": [0, max(0, top - up), crop.width, min(crop.height, bottom + down)]})
    return out


def build(articles: list[dict], check: dict, manifest_path, source: str, name: str, per_sheet: int) -> dict:
    manifest = pipeline.load_manifest(manifest_path)
    folders = dict(check_native.evidence_folders(manifest, source))
    stream = check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(list(folders.items()))))
    by_number = {a["number"]: a for a in articles}
    out_dir = pipeline.CACHE / "reviews" / name
    (out_dir / "strips").mkdir(parents=True, exist_ok=True)
    items = []
    for result in check["results"]:
        if result.get("agreesAfterReread", result["agrees"]):
            continue
        article = by_number[result["number"]]
        canonical = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        span = result["located"].get("tesseract")
        ocr = stream[span[0]:span[1]] if span else []
        pairs = matched_pairs(canonical, ocr)
        for k, d in enumerate(result["disagreements"]):
            if d.get("reread", {}).get("confirmed"):
                continue
            first = d["at"]
            last = first + max(1, len(nc.tokens(d["canonical"]))) - 1
            regions = strips(ocr, pairs, first, last, folders, tight=True) if ocr else []
            images = []
            for n, region in enumerate(regions):
                path = out_dir / "strips" / f"{result['number'].replace(' ', '_')}-{k}-{n}.png"
                Image.open(folders[region["source"]] / "crop.png").crop(region["box"]).save(path)
                images.append(str(path))
            highlight = list(range(first, last + 1)) if d["kind"] == "unconfirmed" else []
            lo, hi = max(0, first - CONTEXT), min(len(canonical), last + CONTEXT + 1)
            # Where a mark may be missing (or every stream adds something), «⟨?⟩» shows the place in the native text.
            gap = first + 1 if d["kind"] in ("mark-candidate", "semicolon-sample") else first if d["kind"] == "extra-in-all-streams" else None
            snippet = " ".join(("⟨?⟩ " if i == gap else "") + (("[" + t + "]") if i in highlight else t)
                               for i, t in enumerate(canonical[lo:hi], lo)) + (" ⟨?⟩" if gap is not None and gap >= hi else "")
            readings = {n: v for n, v in result["ocr"].items()}
            note = (f"{d['kind']}: " + (f"canonical «{d['canonical']}»" if d["kind"] == "unconfirmed" else f"OCR reads {d['ocr']} at ⟨?⟩")
                    + " | " + " | ".join(f"{n}: …{_near(v, canonical, first)}…" for n, v in readings.items()))
            items.append({"id": f"{result['number']}#{k}", "number": result["number"], "kind": d["kind"], "at": first,
                          "canonical": d["canonical"], "ocr": d.get("ocr"), "title": f"{result['number']} #{k} {d['kind']}",
                          "images": images, "regions": regions, "snippet": snippet, "text": snippet, "note": note,
                          "imageSha256s": [pipeline.hash_file(pipeline.Path(p)) for p in images]})
    sheets = review_sheets.render([{**i, "text": i["snippet"], "highlight": []} for i in items], out_dir, name, per_sheet)
    index = {"name": name, "check": check.get("source"), "items": items, "sheets": sheets}
    pipeline.write_json(out_dir / f"{name}-items.json", index)
    return index


def _near(stream_text: str, canonical: list[str], at: int) -> str:
    tokens = stream_text.split()
    if not tokens:
        return ""
    centre = int(at / max(1, len(canonical)) * len(tokens))
    return " ".join(tokens[max(0, centre - CONTEXT):centre + CONTEXT])


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("articles", type=pipeline.Path)
    parser.add_argument("check", type=pipeline.Path)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("source")
    parser.add_argument("--name", required=True)
    parser.add_argument("--per-sheet", type=int, default=8)
    args = parser.parse_args()
    articles = pipeline.read_json(args.articles)
    if isinstance(articles, dict):
        articles = articles["quotedArticles"]
    index = build(articles, pipeline.read_json(args.check), args.manifest.resolve(), args.source, args.name, args.per_sheet)
    print(len(index["items"]), "items on", len(index["sheets"]), "sheets")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
