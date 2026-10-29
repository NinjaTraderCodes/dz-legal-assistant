#!/usr/bin/env python3
"""Family Code native text from the Justice Ministry bilingual compilation (Arabic column), ready for the OCR comparison.

Steps, each deterministic:
1. Characters are read in PDFium index order inside the reviewed Arabic column (collect_family_native.arabic_column),
   so each one keeps its text-page index and box. On every page this equals PDFium's bounded text except for stray
   punctuation boxes (a full stop drawn apart), which are attached to the line they overlap.
2. Glyph fix (sgg_glyphs): «آ» produced by the kaf glyph 987 becomes «ك»; the kashida glyph (U+0467) is tatweel.
3. Lines: the compilation is rotated (270°); a line's start is its smallest y. First lines of paragraphs are indented
   (start about 12 points after the column edge); centred lines (far from both edges) are headings.
4. Articles start at a paragraph opening with «المادة … :»; headings give the path; the Ministry's annotations are
   removed from the text and kept as evidence: «(الأمر رقم 05-02 …)» (amended or inserted by Ordinance 05-02),
   «(ملغاة بالأمر …)» (repealed), footnote lines starting with «*», page numbers.
5. text_clean.paragraph (tatweel, harakat, spaces before punctuation).
Usage: prepare_family.py --out FILE
"""
from __future__ import annotations

import argparse
import re
from collections import Counter

import pypdfium2 as pdfium
from pypdf import PdfReader

import pipeline
import sgg_glyphs
import text_clean
from collect_family_native import arabic_column
from collect_civil_native import page_labels

SOURCE = pipeline.CACHE / "current-digital/family-consolidated-bilingual.pdf"
ANNOTATION = re.compile(r"\(\s*(?:معدلة\s*)?(?:الأمر|القانون)\s+رقم[^)]*\)\.?")
REPEALED = re.compile(r"\(\s*ملغاة\s+بالأمر[^)]*\)\.?")
KEYWORDS = [("الكتاب", 0), ("الباب", 1), ("الفصل", 2), ("القسم", 3)]
BARE_HEADING = re.compile(r"^(?:الكتاب|الباب|الفصل|القسم)\s+(?:الأول|الثاني|الثالث|الرابع|الخامس|السادس|السابع|الثامن|التاسع|العاشر)$")


def article_label(text: str) -> dict | None:
    # PDF5 puts the suffix index after the colon: «المادة 8 مكرر :1 (الأمر...)».
    # Reuse the source-label parser rather than treating that index as body text.
    return next((label for label in page_labels(text) if label["start"] == 0), None)


def page_lines(document, reader, index: int, decisions: dict, *, french: bool = False) -> list[dict]:
    left, bottom, right, top = arabic_column(reader.pages[index].cropbox, reader.pages[index].rotation)
    if french:
        bottom, top = top, float(reader.pages[index].cropbox.top)
    textpage = document[index].get_textpage()
    lines, current = [], []
    for i in range(textpage.count_chars()):
        ch = textpage.get_text_range(i, 1)
        x0, y0, x1, y1 = textpage.get_charbox(i)
        if not (left - 1 <= (x0 + x1) / 2 <= right + 1 and bottom - 1 <= (y0 + y1) / 2 <= top + 1):
            continue
        if ch in "\r\n":
            if current:
                lines.append(current)
            current = []
            continue
        if ch == sgg_glyphs.ALEF_MADDA and decisions.get(i):
            ch = "ك"
        current.append((ch, (x0, y0, x1, y1)))
    if current:
        lines.append(current)
    out = []
    for line in lines:
        ink = [b for c, b in line if not c.isspace()]
        if not ink:
            continue
        # The stretch glyph (kashida) is typography: removed before label matching, as text_clean does later anyway.
        text = "".join(c for c, _ in line).replace(sgg_glyphs.STRETCH, "").replace("ـ", "").strip()
        start, end = min(b[1] for b in ink), max(b[3] for b in ink)
        if french:
            start, end = -end, -start
        out.append({"text": text, "start": start, "end": end,
                    "x": (min(b[0] for b in ink) + max(b[2] for b in ink)) / 2, "page": index + 1})
    # A line made of punctuation only (a full stop drawn apart) joins the line it overlaps horizontally.
    merged = []
    for line in out:
        if re.fullmatch(r"[.،:؛]+", line["text"]):
            host = min(out, key=lambda o: abs(o["x"] - line["x"]) if o is not line and not re.fullmatch(r"[.،:؛]+", o["text"]) else 1e9)
            host["text"] += line["text"]
            continue
        merged.append(line)
    return merged


def classify(lines: list[dict]) -> None:
    edge_start = min(l["start"] for l in lines)
    edge_end = max(l["end"] for l in lines)
    for line in lines:
        indent, tail = line["start"] - edge_start, edge_end - line["end"]
        line["kind"] = ("heading" if indent > 30 and tail > 30 else "start" if indent > 6 else "continuation")


def assemble(lines: list[dict]) -> tuple[list[dict], list[dict]]:
    articles, path, heading_buffer, current, notes = [], {}, [], None, []
    signature = False
    for line in lines:
        text = line["text"]
        if current and current["number"] == "224" and (signature or text.startswith("حرر بالجزائر")):
            # The closing place/date and signature are outside article 224 on PDF43.
            signature = True
            notes.append({"page": line["page"], "paratext": text})
            continue
        if re.fullmatch(r"\d+", text):  # page number
            continue
        if text.startswith("*"):
            notes.append({"page": line["page"], "footnote": text})
            marked = text.lstrip("* ")
            if BARE_HEADING.fullmatch(marked):
                # The publisher's footnote marker precedes the maintenance chapter in PDF index order.
                heading_buffer.append(marked)
            continue
        if (line["kind"] == "heading" or BARE_HEADING.fullmatch(text)) and not article_label(text):
            heading_buffer.append(text)
            continue
        if heading_buffer:
            new_level = None
            for h in heading_buffer:
                h = ANNOTATION.sub("", h).strip()
                if not h:
                    continue
                level = next((lv for kw, lv in KEYWORDS if h.startswith(kw)), None)
                if level is not None:
                    path = {k: v for k, v in path.items() if k < level}
                    path[level] = h
                    new_level = level
                elif new_level is not None:
                    path[new_level] += " - " + h
                    new_level = None
                else:
                    # Unnumbered subheadings such as «العدة» and «الحضانة» are siblings, not cumulative chapter titles.
                    path[4] = h
            heading_buffer = []
        label = article_label(text) if line["kind"] == "start" or current is None else None
        if label:
            number = label["canonicalNumberCandidate"]
            current = {"number": number, "printedLabel": label["rawLabel"].strip(), "rawParagraphs": [text[label["end"]:]],
                       "path": [path[k] for k in sorted(path)], "pages": [line["page"]]}
            articles.append(current)
            continue
        if current is None:
            continue
        if line["kind"] == "start" or not current["rawParagraphs"][-1]:
            current["rawParagraphs"].append(text)
        else:
            current["rawParagraphs"][-1] += " " + text
        if line["page"] not in current["pages"]:
            current["pages"].append(line["page"])
    for article in articles:
        raw = "\n".join(article["rawParagraphs"])
        article["amendedBy0502"] = bool(ANNOTATION.search(raw))
        article["repealedBy0502"] = bool(REPEALED.search(raw))
        paragraphs = [text_clean.paragraph(REPEALED.sub("", ANNOTATION.sub("", p))) for p in article["rawParagraphs"]]
        article["paragraphs"] = [p for p in paragraphs if p]
        article["text"] = "\n".join(article["paragraphs"])
    return articles, notes


def validate_inventory(articles: list[dict], expected: list[str]) -> None:
    actual = Counter(a["number"] for a in articles)
    if actual != Counter(expected) or any(count != 1 for count in actual.values()):
        raise ValueError(f"Family preparation changed the source inventory: missing={Counter(expected) - actual}; "
                         f"extra={actual - Counter(expected)}; duplicates={[n for n, c in actual.items() if c != 1]}")


def build() -> dict:
    reader, document = PdfReader(str(SOURCE)), pdfium.PdfDocument(str(SOURCE))
    batch = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/current-batches/family-official-native-2005.json")
    if pipeline.hash_file(SOURCE) != batch["source"]["sha256"]:
        raise ValueError("Family compilation differs from its pin")
    pages = [p["pdfPage"] for p in batch["pages"]]
    decisions = sgg_glyphs.page_decisions(SOURCE, [p - 1 for p in pages])
    unaligned = [p for p in pages if decisions.get(p) is None]
    if unaligned:
        raise ValueError(f"Family glyph objects did not align on pages {unaligned}")
    undecided = sum(1 for d in decisions.values() for v in d.values() if v is None)
    lines = []
    for p in pages:
        page = page_lines(document, reader, p - 1, decisions[p])
        classify(page)
        lines += page
    articles, notes = assemble(lines)
    validate_inventory(articles, [a["canonicalNumberCandidate"] for a in batch["articles"]])
    return {"source": batch["source"], "pages": pages, "glyphFix": {"undecidedMadda": undecided, "rule": sgg_glyphs.__doc__.strip().splitlines()[0]},
            "preparationState": "source candidates only; article bodies, heading paths, paragraphs and amendments need comparison and review",
            "verificationEffect": "none",
            "articles": articles, "footnotes": notes}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    result = build()
    pipeline.write_json(pipeline.Path(args.out), result)
    numbers = [a["number"] for a in result["articles"]]
    print(len(numbers), "articles;", sum(a["amendedBy0502"] for a in result["articles"]), "annotated 05-02;",
          sum(a["repealedBy0502"] for a in result["articles"]), "repealed;", result["glyphFix"]["undecidedMadda"], "undecided madda")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
