#!/usr/bin/env python3
"""Crop the Civil compilation's current-code body above its historical footnotes, with no fresh source download.

Every prepared body line must remain inside its page crop. Source geometry chooses the divider, never OCR words.
The independent original/amending JO evidence is retained and not overwritten by this additional witness.
"""
from __future__ import annotations

import re
from contextlib import closing

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def checked_region(width: float, height: float, divider_top: float | None, boxes: list[list[float]]) -> list[float]:
    bottom = min(.94, 1 - (divider_top + 2) / height) if divider_top is not None else .94
    region = [.09, .045, .92, bottom]
    if not region[1] < bottom < 1:
        raise ValueError("Footnote divider leaves no body region")
    for left, low, right, high in boxes:
        if (left < width * region[0] or right > width * region[2]
                or low < height * (1-bottom) or high > height * (1-region[1])):
            raise ValueError("Civil OCR crop would clip a prepared body line")
    return region


def main() -> None:
    import pypdfium2 as pdfium
    native = pipeline.read_json(WORK / "native-candidates.json")
    pdf = pipeline.CACHE / "current-digital/civil-consolidated-ar.pdf"
    if pipeline.hash_file(pdf) != native["source"]["sha256"]:
        raise ValueError("Civil source differs from pin")
    boxes_by_page = {p: [] for p in native["pages"]}
    for article in native["articles"]:
        for line in article["lineEvidence"]:
            boxes_by_page[line["page"]].append(line["box"])
    extracts, boundaries = [], []
    with closing(pdfium.PdfDocument(str(pdf))) as document:
        for p in native["pages"]:
            page = document[p-1]
            tp = page.get_textpage()
            raw = tp.get_text_range(index=0, count=tp.count_chars())
            indices = [i for match in re.finditer(r"_{4,}",raw) for i in range(match.start(),match.end())]
            divider = max((tp.get_charbox(i)[3] for i in indices), default=None)
            width,height = page.get_size()
            region = checked_region(width,height,divider,boxes_by_page[p])
            extracts.append({"id": f"civil-ministry-{p:03d}-body", "sourceId": "civil-consolidated-ar",
                             "pdfPage": p, "region": region, "language": "ara", "psm": 4})
            boundaries.append({"pdfPage": p, "dividerTopPdfPoints": divider,
                               "preparedBodyLinesContained": len(boxes_by_page[p]), "region": region})
            tp.close()
            page.close()
    manifest = {"manifestVersion": 1, "sources": [{"id": "civil-consolidated-ar", **{
        k:native["source"][k] for k in ("url","sha256","retrievedOn")}}], "extracts": extracts}
    pipeline.write_json(pipeline.ROOT / "tools/law-corpus/civil-ministry-manifest.json",manifest)
    pipeline.write_json(WORK / "ministry-crop-boundaries.json", {
        "sourceSha256": native["source"]["sha256"], "nativeSha256": pipeline.hash_file(WORK / "native-candidates.json"),
        "method": __doc__.strip(), "pages": boundaries, "verificationEffect": "geometry containment only; no legal-text check"})
    print(len(extracts), "Civil body crops; every prepared body line is contained; historical footnotes excluded")


if __name__ == "__main__":
    main()
