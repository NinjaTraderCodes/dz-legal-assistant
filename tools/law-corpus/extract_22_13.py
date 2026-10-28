#!/usr/bin/env python3
"""Law 22-13 (JO 48/2022, Arabic, pages 3-11): cleaned text-layer paragraphs cut into the law's own articles and the
CPCA article bodies it quotes. Cleaning: jo_layer (documented, tested). Nothing here is checked text: the result goes
to the OCR comparison and the image review.

A quoted body starts at a paragraph opening with «”المادة N :» and ends at the paragraph ending with the closing
quotation mark (“ or ”), optionally followed by the law's own punctuation. The quotation marks are removed; a full stop
printed after the closing mark is kept, as for the other quoted bodies of this corpus.
Usage: extract_22_13.py --out FILE
"""
from __future__ import annotations

import argparse
import re

import pypdfium2 as pdfium

import jo_layer
import pipeline

SOURCE = "A2022048"
PAGES = range(3, 12)
QUOTED_LABEL = re.compile(r"^[”“]المادة (\d+(?: مكرر ?\d*)?) : ?(.*)$")
LAW_LABEL = re.compile(r"^المادة (الأولى|\d+) :\s*(.*)$")
CLOSE = re.compile(r"\s*[“”]([.،]?)$")


def canonical_number(label: str) -> str:
    label = re.sub(r"مكرر ?(\d+)", r"مكرر \1", label)
    return label.strip()


def extract(pdf_path) -> dict:
    document = pdfium.PdfDocument(pdf_path)
    lines = []
    for page in PAGES:
        lines += jo_layer.page_lines(document, page)
    blocks = jo_layer.paragraphs(jo_layer.classify(lines))
    law_articles, quoted, headings = [], [], []
    current = None
    open_quote = False
    for block in blocks:
        text = block["text"]
        if block["kind"] == "heading":
            headings.append({"text": text, "lines": block["lines"], "afterQuoted": quoted[-1]["number"] if quoted else None})
            continue
        law = LAW_LABEL.match(text)
        label = QUOTED_LABEL.match(text)
        if label and not open_quote or (label and current is None):
            current = {"number": canonical_number(label.group(1)), "printedLabel": text.split(":")[0].strip("”“ "),
                       "paragraphs": [], "lines": [], "lawArticle": law_articles[-1]["number"] if law_articles else None}
            quoted.append(current)
            text = label.group(2)
            open_quote = True
        elif law and not open_quote:
            law_articles.append({"number": law.group(1), "paragraphs": [law.group(2)] if law.group(2) else [], "lines": block["lines"]})
            current = None
            continue
        if open_quote and current is not None:
            close = CLOSE.search(text)
            if close:
                text = text[:close.start()] + close.group(1)
                open_quote = False
            current["paragraphs"].append(text.strip())
            current["lines"] += block["lines"]
        elif law_articles:
            law_articles[-1]["paragraphs"].append(text)
            law_articles[-1]["lines"] += block["lines"]
    return {"source": SOURCE, "pages": list(PAGES), "method": "jo_layer.page_lines/classify/paragraphs/clean",
            "lawArticles": law_articles, "quotedArticles": quoted, "headings": headings}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    pins = {s["id"]: s for s in pipeline.read_json(pipeline.ROOT / "tools/law-corpus/scope-source-manifest.json")["sources"]}
    path = pipeline.CACHE / f"{SOURCE}.pdf"
    if pipeline.hash_file(path) != pins[SOURCE]["sha256"]:
        raise ValueError("cached JO 48/2022 differs from its pin")
    result = extract(path)
    result["sourcePin"] = pins[SOURCE]
    pipeline.write_json(pipeline.Path(args.out), result)
    print(len(result["lawArticles"]), "law articles;", len(result["quotedArticles"]), "quoted CPCA bodies;", len(result["headings"]), "headings")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
