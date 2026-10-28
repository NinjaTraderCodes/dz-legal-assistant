#!/usr/bin/env python3
"""Clean text from the Unicode text layer of a two-column JO issue (written for JO 48/2022, Law 22-13).

Observed on that issue (26 Oct 2026), each handled by one deterministic, tested step:
1. Text blocks come out of reading order (page 4 starts mid-column). Lines are re-ordered from glyph geometry:
   right column, then left; top to bottom (`page_lines`).
2. Lines mixing a bold run (article labels) or a detached mark are emitted in visual run order
   («: يختص… ّة 531 ”الماد»). Words of a line are re-ordered by their right edge, right to left (`page_lines`).
3. Some digits are emitted as Arabic-Indic code points (٠ ٢ ٣ ٤ ٨ ٩) where the page prints the same Western digit
   glyphs as elsewhere: mapped through DIGITS (`clean`).
4. Kashida stretching is emitted as tatweel runs with spaces around them («ي ــــ ت ــــ م» for «يتم»). A tatweel is
   never word-initial or word-final, so a run and its surrounding spaces are removed (`clean`).
5. Harakat are emitted detached and displaced («ويبّلغ» for «ويبلّغ», «إ ّلا»). They are not carried: the corpus text
   of this code has no harakat. A mark and the spaces around it are removed, except that word-final tanween keeps
   the following word space (`clean`).
What this does not fix (spaces the layer drops or adds inside words, e.g. «يختصالقسم», «ه ذا») is left for the OCR
comparison and the image review, which record each correction.
"""
from __future__ import annotations

import re
import statistics
import unicodedata

import pypdfium2 as pdfium

DIGITS = str.maketrans("٠١٢٣٤٥٦٧٨٩", "0123456789")
TATWEEL_RUN = re.compile(r"\s*ـ+\s*")
TANWEEN = re.compile(r"\s*[ً-ٍ]+")
OTHER_MARKS = re.compile(r"\s*[َ-ْ]+\s*")
SPACES = re.compile(r"[ \t ]+")


def clean(text: str) -> str:
    text = unicodedata.normalize("NFC", text).translate(DIGITS)
    text = TATWEEL_RUN.sub("", text)
    text = OTHER_MARKS.sub("", text)
    text = TANWEEN.sub("", text)
    return SPACES.sub(" ", text).strip()


def page_lines(document: pdfium.PdfDocument, page_number: int, header_band: float = 0.06) -> list[dict]:
    """Layer lines of one page in reading order: {"column", "y", "right", "left", "raw", "text"}.

    y is the PDF baseline height (points, origin bottom); the running header band at the top is dropped.
    """
    page = document[page_number - 1]
    width, height = page.get_size()
    textpage = page.get_textpage()
    lines, current = [], []
    for i in range(textpage.count_chars()):
        char = textpage.get_text_range(i, 1)
        if char in "\r\n":
            if current:
                lines.append(current)
            current = []
            continue
        current.append((char, textpage.get_charbox(i)))
    if current:
        lines.append(current)
    out = []
    for line in lines:
        ink = [box for char, box in line if not char.isspace()]
        if not ink:
            continue
        y = statistics.median((b[1] + b[3]) / 2 for b in ink)
        if y > height * (1 - header_band):
            continue
        words, word = [], []
        for char, box in line:
            if char == " ":
                if word:
                    words.append(word)
                word = []
            else:
                word.append((char, box))
        if word:
            words.append(word)
        raw = " ".join("".join(c for c, _ in w) for w in words)
        ordered = sorted(words, key=lambda w: -max(b[2] for _, b in w))
        right, left = max(b[2] for b in ink), min(b[0] for b in ink)
        column = "right" if (right + left) / 2 > width / 2 else "left"
        out.append({"page": page_number, "column": column, "y": round(y, 1), "right": round(right, 1), "left": round(left, 1),
                    "raw": raw, "text": " ".join("".join(c for c, _ in w) for w in ordered)})
    out.sort(key=lambda r: (r["column"] != "right", -r["y"]))
    return out


def classify(lines: list[dict], indent_min: float = 6.0, heading_min: float = 20.0) -> list[dict]:
    """Mark each line as a paragraph start (first line indented from the column's right edge), a continuation,
    or a heading (centred: far from the right edge). The column edge is the most common right edge (whole points) of
    its lines, so a wide title or a stray line cannot move it."""
    rights: dict[tuple, list[int]] = {}
    for line in lines:
        rights.setdefault((line["page"], line["column"]), []).append(round(line["right"]))
    edges = {key: float(statistics.mode(values)) for key, values in rights.items()}
    for line in lines:
        gap = edges[(line["page"], line["column"])] - line["right"]
        line["kind"] = "heading" if gap > heading_min else "start" if gap > indent_min else "continuation"
    return lines


def paragraphs(lines: list[dict]) -> list[dict]:
    """Join classified lines into paragraphs: {"kind": "paragraph"|"heading", "text", "lines": [line refs]}."""
    out: list[dict] = []
    for line in lines:
        text = clean(line["text"])
        if not text:
            continue
        ref = {"page": line["page"], "column": line["column"], "y": line["y"]}
        if line["kind"] == "continuation" and out and out[-1]["kind"] == "paragraph":
            out[-1]["text"] += " " + text
            out[-1]["lines"].append(ref)
        else:
            out.append({"kind": "heading" if line["kind"] == "heading" else "paragraph", "text": text, "lines": [ref]})
    return out
