#!/usr/bin/env python3
"""Pipeline manifest of whole-column crops for official two-column pages (JO issues, Ministry compilations).

Column boundaries come from the page pixels, deterministically: the header rule (first long horizontal line near the
top), the frame and the column rule (vertical lines inked over most of the body height). Each column region stops a few
pixels inside those rules, because Tesseract was observed to drop whole text lines that touch a vertical rule
(JO 21/2008, 26 Oct 2026). Reading order for Arabic: right column, then left. A page without a detectable column rule is
split at its middle and reported, never silently treated as one column.

Usage: make_column_manifest.py SOURCE_ID FIRST LAST --out MANIFEST [--psm 4] [--append]
The source must already be pinned in scope-source-manifest.json (or --sources FILE) and cached.
"""
from __future__ import annotations

import argparse

import numpy as np
import pypdfium2 as pdfium

import pipeline

DETECT_DPI = 150


def page_columns(gray: np.ndarray) -> dict:
    h, w = gray.shape
    ink = gray < 160
    rows = ink.mean(axis=1)
    top = next((y for y in range(int(0.15 * h)) if rows[y] > 0.5), None)
    bottom = next((y for y in range(h - 1, int(0.9 * h), -1) if rows[y] > 0.5), None)
    body_top = (top + 4) if top is not None else int(0.06 * h)
    body_bottom = (bottom - 4) if bottom is not None else int(0.985 * h)
    body = ink[body_top:body_bottom]
    cols = np.where(body.mean(axis=0) > 0.5)[0]
    groups: list[list[int]] = []
    for x in cols:
        if groups and x - groups[-1][1] <= 3:
            groups[-1][1] = int(x)
        else:
            groups.append([int(x), int(x)])
    left_frame = [g for g in groups if g[1] < 0.12 * w]
    right_frame = [g for g in groups if g[0] > 0.88 * w]
    mid = [g for g in groups if 0.35 * w < g[0] < 0.65 * w]
    x0 = left_frame[-1][1] + 3 if left_frame else int(0.02 * w)
    x3 = right_frame[0][0] - 3 if right_frame else int(0.98 * w)
    note = None
    if mid:
        x1, x2 = mid[0][0] - 3, mid[-1][1] + 3
    else:
        x1 = x2 = w // 2
        note = "no column rule detected; split at the page middle"
    return {"right": [x2 / w, body_top / h, x3 / w, body_bottom / h], "left": [x0 / w, body_top / h, x1 / w, body_bottom / h], "note": note}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source")
    parser.add_argument("first", type=int)
    parser.add_argument("last", type=int)
    parser.add_argument("--out", required=True)
    parser.add_argument("--psm", type=int, default=4)
    parser.add_argument("--sources", default=str(pipeline.ROOT / "tools/law-corpus/scope-source-manifest.json"))
    parser.add_argument("--append", action="store_true")
    args = parser.parse_args()
    pins = {s["id"]: s for s in pipeline.read_json(pipeline.Path(args.sources))["sources"]}
    source = pins[args.source]
    path = pipeline.CACHE / f"{source['id']}.pdf"
    if pipeline.hash_file(path) != source["sha256"]:
        raise ValueError("cached source differs from its pin")
    out = pipeline.Path(args.out)
    manifest = pipeline.read_json(out) if args.append and out.exists() else {"manifestVersion": 1, "sources": [], "extracts": []}
    if all(s["id"] != source["id"] for s in manifest["sources"]):
        manifest["sources"].append(source)
    known = {e["id"] for e in manifest["extracts"]}
    document = pdfium.PdfDocument(path)
    notes = []
    for number in range(args.first, args.last + 1):
        page = document[number - 1]
        gray = np.array(page.render(scale=DETECT_DPI / 72).to_pil().convert("L"))
        page.close()
        layout = page_columns(gray)
        if layout["note"]:
            notes.append(f"page {number}: {layout['note']}")
        for side in ("right", "left"):
            ident = f"{source['id'].lower()}-{number:03}-{side}"
            if ident in known:
                continue
            region = [round(v, 4) for v in layout[side]]
            manifest["extracts"].append({"id": ident, "sourceId": source["id"], "pdfPage": number, "region": region,
                                         "language": "ara", "psm": args.psm})
    pipeline.write_json(out, manifest)
    print(f"{len(manifest['extracts'])} extracts in {out}")
    for n in notes:
        print(n)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
