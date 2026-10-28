#!/usr/bin/env python3
"""Glyph-level decoding fix for the Justice Ministry compilations set in subset Times New Roman (Type0, Identity-H,
no ToUnicode, no cmap): the Family Code bilingual compilation and the Civil Code 2007 compilation.

Finding (28 Oct 2026, glyph IDs paired with PDFium's characters object by object, text reversed to visual order):
- glyph 987 is decoded by PDFium as «آ» in every subset (regular and bold), although its neighbours 986 and 988 decode
  as «ك»; real «آ» comes from glyphs 897/898. Glyph 987 is a kaf form: words like «آان», «الترآة» are «كان», «التركة».
- glyph 1127 is decoded as «ѧ» (U+0467); it is the kashida (stretch) glyph, i.e. tatweel.
Every other glyph decodes to one stable character in the sample pages. This module applies only these two entries,
as an explicit table, per PDF text object, and flags any object where the «آ» glyphs and characters cannot be paired.

Pairing: inside one text object PDFium gives the logical order and the content stream the visual order, so the «آ»
characters of the reversed text appear in the same order as the «آ»-producing glyphs. Ligatures and spaces do not
change that order. If the counts differ, or an «آ» comes from a glyph outside the table, the object is flagged.

Usage: sgg_glyphs.py PDF --out TABLE.json   (word table raw -> decoded, conflicts and flags, for all pages)
"""
from __future__ import annotations

import argparse
import ctypes
import re
from collections import Counter, defaultdict

KAF_AS_MADDA = 987
MADDA = {897, 898, 1013, 1014}  # alef madda forms, and the lam-alef-madda ligatures «لآ»
KASHIDA = 1127
ALEF_MADDA = "آ"
STRETCH = "ѧ"


def madda_flags(text: str, glyphs: list[int]) -> tuple[list[bool] | None, str | None]:
    """For each «آ» character of one text object (logical order): True if it is the kaf glyph."""
    madda_glyphs = [g for g in glyphs if g == KAF_AS_MADDA or g in MADDA]
    count = text.count(ALEF_MADDA)
    if count != len(madda_glyphs):
        return None, f"{count} «آ» characters but {len(madda_glyphs)} «آ» glyphs"
    return [g == KAF_AS_MADDA for g in reversed(madda_glyphs)], None


def decode_object(text: str, glyphs: list[int]) -> tuple[str | None, str | None]:
    """Corrected text of one PDF text object, or (None, reason) when it cannot be decided."""
    madda_glyphs = [g for g in glyphs if g == KAF_AS_MADDA or g in MADDA]
    positions = [i for i, ch in enumerate(text) if ch == ALEF_MADDA]
    if len(positions) != len(madda_glyphs):
        return None, f"{len(positions)} «آ» characters but {len(madda_glyphs)} «آ» glyphs"
    chars = list(text)
    # Text is logical order, glyphs visual (left to right): the last «آ» character pairs with the first «آ» glyph.
    for position, glyph in zip(reversed(positions), madda_glyphs):
        if glyph == KAF_AS_MADDA:
            chars[position] = "ك"
    return "".join(chars).replace(STRETCH, "ـ"), None


def text_objects(pdf_path, page_numbers=None):
    """(page, PDFium text, glyph IDs) for every text object set in a Type0 font."""
    import pypdfium2 as pdfium
    import pypdfium2.raw as R
    from pypdf import PdfReader
    from pypdf.generic import ContentStream

    document, reader = pdfium.PdfDocument(str(pdf_path)), PdfReader(str(pdf_path))
    for number in page_numbers or range(len(reader.pages)):
        page = document[number]
        textpage = page.get_textpage()
        objects = []
        for obj in page.get_objects():
            if obj.type != R.FPDF_PAGEOBJ_TEXT:
                continue
            n = R.FPDFTextObj_GetText(obj.raw, textpage.raw, None, 0)
            buffer = (ctypes.c_ushort * n)()
            R.FPDFTextObj_GetText(obj.raw, textpage.raw, buffer, n * 2)
            objects.append(ctypes.string_at(buffer, n * 2).decode("utf-16-le").rstrip("\x00"))
        pdf_page = reader.pages[number]
        fonts = {k: v.get_object() for k, v in pdf_page["/Resources"]["/Font"].items()}
        operations, font = [], None
        for operands, op in ContentStream(pdf_page.get_contents(), reader).operations:
            if op == b"Tf":
                font = operands[0]
            elif op in (b"Tj", b"TJ", b"'", b'"'):
                parts = operands[0] if op == b"TJ" else [operands[-1]]
                data = b"".join(x.original_bytes for x in parts if hasattr(x, "original_bytes"))
                operations.append((fonts[font].get("/Subtype"), data))
        type0 = [d for kind, d in operations if kind == "/Type0"]
        if len(objects) != len(type0):
            objects = [t for t in objects if re.search("[؀-ۿѧﹰ-﻿]", t)]
        if len(objects) != len(type0):
            yield number + 1, None, None
            continue
        for text, data in zip(objects, type0):
            yield number + 1, text, [int.from_bytes(data[i:i + 2], "big") for i in range(0, len(data), 2)]


def page_decisions(pdf_path, page_numbers=None) -> dict:
    """Per page: text-page character index of every «آ» -> True (kaf glyph), False (real madda) or None (undecided).

    PDFium does not say which text object a text-page character comes from, so each «آ» character is assigned to the
    text object whose bounds contain its centre (the smallest, if several), then paired with that object's «آ» glyphs
    by order. An object whose count of assigned characters differs from its «آ» glyphs leaves them undecided."""
    import pypdfium2 as pdfium
    import pypdfium2.raw as R

    document = pdfium.PdfDocument(str(pdf_path))
    objects_by_page: dict[int, list] = defaultdict(list)
    for page, text, glyphs in text_objects(pdf_path, page_numbers):
        objects_by_page[page].append((text, glyphs))
    result = {}
    for page, items in objects_by_page.items():
        pdf_page = document[page - 1]
        textpage = pdf_page.get_textpage()
        text_objs = [o for o in pdf_page.get_objects() if o.type == R.FPDF_PAGEOBJ_TEXT]
        if items[0][0] is None:
            result[page] = None
            continue
        # text_objects() may have dropped non-Arabic objects; keep the bounds of the ones it paired.
        bounds = []
        texts = [t for t, _ in items]
        pool = [(o, o.get_pos()) for o in text_objs]
        cursor = 0
        for text, glyphs in items:
            while cursor < len(pool):
                obj, box = pool[cursor]
                cursor += 1
                n = R.FPDFTextObj_GetText(obj.raw, textpage.raw, None, 0)
                buffer = (ctypes.c_ushort * n)()
                R.FPDFTextObj_GetText(obj.raw, textpage.raw, buffer, n * 2)
                if ctypes.string_at(buffer, n * 2).decode("utf-16-le").rstrip("\x00") == text:
                    bounds.append((box, text, glyphs))
                    break
        decisions: dict[int, bool | None] = {}
        assigned: dict[int, list[int]] = defaultdict(list)
        for i in range(textpage.count_chars()):
            if textpage.get_text_range(i, 1) != ALEF_MADDA:
                continue
            x0, y0, x1, y1 = textpage.get_charbox(i)
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            inside = [k for k, ((l, b, r, t), _, _) in enumerate(bounds) if l - 0.5 <= cx <= r + 0.5 and b - 0.5 <= cy <= t + 0.5]
            if not inside:
                decisions[i] = None
                continue
            k = min(inside, key=lambda k: (bounds[k][0][2] - bounds[k][0][0]) * (bounds[k][0][3] - bounds[k][0][1]))
            assigned[k].append(i)
        for k, indices in assigned.items():
            _, text, glyphs = bounds[k]
            flags, _ = madda_flags(text, glyphs)
            if flags is None or len(flags) != len(indices):
                decisions.update({i: None for i in indices})
            else:
                decisions.update(dict(zip(sorted(indices), flags)))
        result[page] = decisions
    return result


def corrected(text: str, page_text_indices: list[int] | None, decisions: dict[int, bool | None]) -> tuple[str, int]:
    """Apply the decisions to a text taken from the text page. `page_text_indices[j]` is the text-page character index
    of text[j] (None for the full-page range, where they are equal). Returns the text and the count of «آ» left
    undecided. U+0467 (the kashida glyph) becomes tatweel."""
    chars, undecided = list(text), 0
    for j, ch in enumerate(chars):
        if ch != ALEF_MADDA:
            continue
        flag = decisions.get(j if page_text_indices is None else page_text_indices[j])
        if flag is None:
            undecided += 1
        elif flag:
            chars[j] = "ك"
    return "".join(chars).replace(STRETCH, "ـ"), undecided


def word_table(pdf_path, page_numbers=None) -> dict:
    """Raw word -> decoded word for every word that contains «آ», with conflicts and undecidable objects."""
    decided: dict[str, Counter] = defaultdict(Counter)
    flags, unaligned_pages = [], []
    for page, text, glyphs in text_objects(pdf_path, page_numbers):
        if text is None:
            unaligned_pages.append(page)
            continue
        if ALEF_MADDA not in text:
            continue
        fixed, reason = decode_object(text, glyphs)
        if fixed is None:
            flags.append({"page": page, "text": text, "reason": reason})
            continue
        for raw, new in zip(text.split(), fixed.split()):
            if ALEF_MADDA in raw:
                decided[raw.replace(STRETCH, "")][new.replace("ـ", "")] += 1
    table = {raw: c.most_common(1)[0][0] for raw, c in decided.items() if len(c) == 1}
    conflicts = {raw: dict(c) for raw, c in decided.items() if len(c) > 1}
    return {"table": table, "conflicts": conflicts, "flags": flags, "unalignedPages": unaligned_pages}


def main() -> int:
    import json
    parser = argparse.ArgumentParser()
    parser.add_argument("pdf")
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    result = word_table(args.pdf)
    with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(result, handle, ensure_ascii=False, indent=2)
    changed = sum(1 for k, v in result["table"].items() if k != v)
    print(f"{len(result['table'])} words with alef-madda decided ({changed} become kaf), {len(result['conflicts'])} conflicts, "
          f"{len(result['flags'])} undecidable objects, {len(result['unalignedPages'])} unaligned pages")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
