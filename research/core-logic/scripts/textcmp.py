"""text-compare: comparing two Arabic texts at three levels (docs/research/core-logic/specs/article-store.md).

Level 1   identical (but for leading and trailing spaces): proof that a quote was not altered.
Level 2   the same letters: NFC, tatweel, diacritics, runs of spaces, the hamza forms of alef, alef maqsura and ya in any
          position, ta marbuta and ha at the end of a word, Arabic and Western digits, punctuation with the spaces around it.
Level 2o  the same letters but for the seat of a hamza: an orthographic difference, recorded and never a correction.
Level 3   the same words in order: level 2 (or 2o) with line breaks ignored (a difference of paragraph division).

Digits are never normalised away, and no word is ever dropped, so a changed digit or a missing negation is always a
difference. The result for each pair: the level reached and, when below level 2, the differing words with ten words of
context on each side.
"""
from __future__ import annotations

import difflib
import re
import unicodedata

DIACRITICS = re.compile(r"[ؐ-ًؚ-ٰٟۖ-ۭ]")
TATWEEL = "ـ"
ALEF = str.maketrans({"أ": "ا", "إ": "ا", "آ": "ا", "ٱ": "ا"})
DIGITS = str.maketrans("٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹", "01234567890123456789")
# Punctuation (Arabic and Latin, brackets, quotation marks, dashes) is removed with the spaces around it.
# The Journal of 1975 prints its full stops as a bullet or a middle dot («•», «·»): punctuation too.
PUNCT = re.compile(r"[،؛؟٪-٭۔.,;:!?()\[\]{}«»\"'“”‘’\-–—_/\\*…•·]+")
NEGATIONS = ("لا", "لم", "لن", "ليس", "غير", "إلا", "ما لم", "دون")


def _base(text: str) -> str:
    text = unicodedata.normalize("NFC", text).replace(TATWEEL, "")
    text = DIACRITICS.sub("", text)
    text = text.translate(ALEF).translate(DIGITS)
    text = text.replace("ى", "ي")
    text = PUNCT.sub(" ", text)
    text = re.sub(r"ة(?=\s|$)", "ه", text)
    return text


def norm2(text: str, keep_lines: bool = True) -> str:
    lines = [re.sub(r"[ \t ]+", " ", _base(line)).strip() for line in text.split("\n")]
    lines = [line for line in lines if line]
    return "\n".join(lines) if keep_lines else " ".join(lines)


def _seats(text: str, variant: int) -> str:
    if variant == 0:
        return text.replace("ئ", "ء").replace("ؤ", "ء")
    return text.replace("ئ", "ي").replace("ؤ", "و").replace("ء", "")


def level(a: str, b: str) -> str:
    if a.strip() == b.strip():
        return "1"
    if norm2(a) == norm2(b):
        return "2"
    if any(_seats(norm2(a), v) == _seats(norm2(b), v) for v in (0, 1)):
        return "2o"
    # Level 2s: the same letters in each paragraph, but for the spaces between words. The Journal's print joins or
    # splits words after a non-joining letter (أو حدث / أوحدث, و79 / و 79, ما لم / مالم) and a reader cannot always
    # tell (CPCA 441, 13 November 2026). Orthographic, like 2o: recorded, never a correction.
    pa, pb = norm2(a).split("\n"), norm2(b).split("\n")
    if len(pa) == len(pb) and all(x.replace(" ", "") == y.replace(" ", "") for x, y in zip(pa, pb)):
        return "2s"
    a3, b3 = norm2(a, keep_lines=False), norm2(b, keep_lines=False)
    if a3 == b3 or any(_seats(a3, v) == _seats(b3, v) for v in (0, 1)):
        return "3"
    return "none"


def _tokens(text: str) -> list[tuple[str, str]]:
    """(original word, its level-2 form), in reading order, line breaks ignored. Punctuation-only words are dropped."""
    out = []
    for word in text.split():
        n = norm2(word, keep_lines=False)
        if n:
            out.append((word, n))
    return out


def differences(a: str, b: str, context: int = 10) -> list[dict]:
    """The differing words (originals), with ten words of context on each side, aligned on the level-2 forms."""
    ta, tb = _tokens(a), _tokens(b)
    sm = difflib.SequenceMatcher(a=[t[1] for t in ta], b=[t[1] for t in tb], autojunk=False)
    out = []
    for op, i1, i2, j1, j2 in sm.get_opcodes():
        if op == "equal":
            continue
        out.append({
            "op": op,
            "a": " ".join(t[0] for t in ta[i1:i2]),
            "b": " ".join(t[0] for t in tb[j1:j2]),
            "before": " ".join(t[0] for t in ta[max(0, i1 - context):i1]),
            "after": " ".join(t[0] for t in ta[i2:i2 + context]),
            "aWordIndex": i1,
        })
    return out


def compare(a: str, b: str) -> dict:
    lv = level(a, b)
    result = {"level": lv, "differences": []}
    if lv in ("2o", "2s", "3", "none"):
        result["differences"] = differences(a, b)
    return result
