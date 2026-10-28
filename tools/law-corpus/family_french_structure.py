#!/usr/bin/env python3
"""French column of the pinned bilingual Family compilation: presence, numbering and paragraph-count cross-check.

This is a structural comparison, never a translation or legal-meaning check. Geometric paragraph counts that differ
are reported for review, not forced equal. Arabic remains the canonical language.
"""
from __future__ import annotations

import re
from collections import Counter

import pypdfium2 as pdfium
from pypdf import PdfReader

import pipeline
import prepare_family as family

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
LABEL = re.compile(r"^(?:Art\.|Article)\s*(1er|\d+(?:\.?\s*bis(?:\s*\d+)?)?)\s*\.?\s*[–—-]\s*")
ANNOTATION = re.compile(r"\((?:Ordonnance|Abrog[^)]*)[^)]*\)", re.IGNORECASE)


def number(text: str) -> tuple[str, int] | None:
    m = LABEL.match(text)
    if not m:
        return None
    parts = re.sub(r"(\d)\.?\s*bis", r"\1 bis", m[1].replace("1er", "1")).split()
    return " ".join("مكرر" if p == "bis" else p for p in parts), m.end()


def main() -> int:
    arabic = pipeline.read_json(WORK / "native.json")
    if pipeline.hash_file(family.SOURCE) != arabic["source"]["sha256"]:
        raise ValueError("Family source differs from its pin")
    reader, doc = PdfReader(str(family.SOURCE)), pdfium.PdfDocument(str(family.SOURCE))
    articles, current, notes = [], None, []
    for p in arabic["pages"]:
        lines = family.page_lines(doc, reader, p - 1, {}, french=True)
        family.classify(lines)
        for line in lines:
            text = line["text"]
            if re.fullmatch(r"\d+\s*bis", text):
                continue
            if text.startswith("*"):
                notes.append({"page": p, "text": text})
                continue
            if current and current["number"] == "224" and text.startswith("Fait à Alger"):
                current = None
                continue
            label = number(text)
            if label:
                current = {"number": label[0], "paragraphs": [text[label[1]:]], "pages": [p]}
                articles.append(current)
            elif line["kind"] == "heading":
                notes.append({"page": p, "heading": text})
            elif current:
                if line["kind"] == "start":
                    current["paragraphs"].append(text)
                else:
                    current["paragraphs"][-1] += " " + text
                if p not in current["pages"]:
                    current["pages"].append(p)
    for a in articles:
        a["repealed"] = any("abrog" in p.lower() for p in a["paragraphs"])
        a["paragraphs"] = [t for p in a["paragraphs"] if (t := ANNOTATION.sub("", p).strip()) and not re.fullmatch(r"[.\s]+", t)]
    counts = Counter(a["number"] for a in articles)
    expected = {a["number"] for a in arabic["articles"]}
    by_number = {a["number"]: a for a in articles}
    differences = [{"number": a["number"], "arabicParagraphs": len(a["paragraphs"]),
                    "frenchParagraphs": len(by_number[a["number"]]["paragraphs"])} for a in arabic["articles"]
                   if a["number"] in by_number and len(a["paragraphs"]) != len(by_number[a["number"]]["paragraphs"])]
    record = {"source": arabic["source"], "method": __doc__.strip(), "articles": articles, "notes": notes,
              "missing": sorted(expected - set(counts)), "unexpected": sorted(set(counts) - expected),
              "duplicates": {k: v for k, v in counts.items() if v != 1}, "paragraphCountDifferences": differences,
              "verificationEffect": "none; paragraph differences need source review; no translation check inferred"}
    pipeline.write_json(WORK / "french-structure.json", record)
    print(len(articles), "French identities; missing", len(record["missing"]), "duplicates", len(record["duplicates"]),
          "paragraph count differences", len(differences))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
