"""Deterministic clean-up of native official text (Justice Ministry HTML) before the OCR comparison.

Each step only removes typography; no letter, digit or punctuation mark is added, replaced or guessed:
- NFC normalisation;
- tatweel (U+0640, kashida stretching such as «تنفيـــذي») removed;
- harakat (U+064B-U+0652) removed: the CPCA corpus text carries none (the 2008 HTML has 56 scattered marks; the JO 48/2022
  text layer displaces them), and the OCR comparison never reads them;
- runs of spaces collapsed; no space kept before «،» «.» «؛» or inside parentheses.
Marks the HTML lost (for example every «:» of the CPCA HTML) are not restored here: they come back only through the
OCR comparison and an image decision.
"""
from __future__ import annotations

import re
import unicodedata

TATWEEL = "ـ"
HARAKAT = re.compile(r"[ً-ْ]")
SPACES = re.compile(r"\s+")
BEFORE_MARK = re.compile(r" +([،.؛])")
AFTER_OPEN = re.compile(r"\( +")
BEFORE_CLOSE = re.compile(r" +\)")


def paragraph(text: str) -> str:
    text = unicodedata.normalize("NFC", text).replace(TATWEEL, "")
    text = HARAKAT.sub("", text)
    text = SPACES.sub(" ", text).strip()
    text = BEFORE_MARK.sub(r"\1", text)
    text = AFTER_OPEN.sub("(", text)
    return BEFORE_CLOSE.sub(")", text)
