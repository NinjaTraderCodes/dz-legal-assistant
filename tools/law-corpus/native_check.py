"""Compare cleaned native official text (Ministry HTML or compilation text) with an independent OCR reading
of the official printed page, under the conservative rule of the current-code delivery brief (26 Oct 2026).

Rule (documented, deterministic, tested in test_native_check.py):
  * Both sides are tokenised into words and punctuation marks.
  * Alignment only (never the verdict) uses a folded form: NFC, tatweel/harakat removed, alef/hamza seats, ى/ي and
    ة/ه unified.
  * Always counted, exactly (after NFC, tatweel and harakat removal): any token containing a digit, every
    punctuation mark, and the negation words لا لم لن ليس غير إلا with their prefixed forms (ولا، فلا، وليس، بغير…).
  * Ordinary words: a one-to-one substitution is tolerated as OCR letter noise only when both words have at least
    four letters and their folded forms differ by one edit; a split/merge is tolerated when the joined folded forms
    are equal. Every tolerance is counted and reported.
  * Dash widths (- ‐ ‑ ‒ – — −) count as one mark: OCR cannot tell them apart, but a dash must still be read.
  * Anything else (inserted, missing or substituted tokens) is a disagreement that must be settled from the page
    image by a person. An empty OCR span never agrees.
"""
from __future__ import annotations

import difflib
import re
import unicodedata

TATWEEL = "ـ"
HARAKAT = re.compile("[ً-ٰٟۖ-ۭ]")
PUNCT = set("،؛؟.,:;()[]«»\"'-–—‐‑‒−/!?…*")
TOKEN = re.compile(r"[؀-ۿݐ-ݿA-Za-z0-9٠-٩۰-۹]+|[^\s؀-ۿݐ-ݿA-Za-z0-9]")
NEGATIONS = {"لا", "لم", "لن", "ليس", "ليست", "غير", "إلا", "الا"}
NEGATION_PREFIXES = ("و", "ف", "ب", "أ", "ل")
DIGITS = str.maketrans("٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹‐‑‒–—−", "01234567890123456789------")


def exact_form(token: str) -> str:
    """The form compared exactly for critical tokens: NFC, no tatweel, no harakat, ASCII digits, one dash."""
    return HARAKAT.sub("", unicodedata.normalize("NFC", token).replace(TATWEEL, "")).translate(DIGITS)


def fold(token: str) -> str:
    """Alignment-only form; never used to decide agreement of a critical token."""
    t = exact_form(token)
    t = re.sub("[إأآٱ]", "ا", t).replace("ى", "ي").replace("ة", "ه").replace("ؤ", "و").replace("ئ", "ي")
    return t


def tokens(text: str) -> list[str]:
    return TOKEN.findall(unicodedata.normalize("NFC", text).replace(TATWEEL, ""))


def is_negation(token: str) -> bool:
    t = exact_form(token)
    if t in NEGATIONS:
        return True
    return len(t) > 2 and t[0] in NEGATION_PREFIXES and t[1:] in NEGATIONS


def critical(token: str) -> bool:
    return any(c.isdigit() for c in token) or (len(token) == 1 and token in PUNCT) or is_negation(token)


def edits(a: str, b: str) -> int:
    """Levenshtein distance, small strings only."""
    previous = list(range(len(b) + 1))
    for i, ca in enumerate(a, 1):
        current = [i]
        for j, cb in enumerate(b, 1):
            current.append(min(previous[j] + 1, current[j - 1] + 1, previous[j - 1] + (ca != cb)))
        previous = current
    return previous[-1]


def compare(canonical: str, ocr: str) -> dict:
    """Token comparison under the rule above. Returns agreement, tolerated noise and disagreements."""
    left, right = tokens(canonical), tokens(ocr)
    result = {"canonicalTokens": len(left), "ocrTokens": len(right), "toleratedLetterNoise": [],
              "toleratedSplitMerge": [], "disagreements": []}
    if not left or not right:
        result["disagreements"].append({"kind": "empty", "canonical": " ".join(left), "ocr": " ".join(right)})
        result["agrees"] = False
        return result
    matcher = difflib.SequenceMatcher(a=[fold(t) for t in left], b=[fold(t) for t in right], autojunk=False)
    for op, a1, a2, b1, b2 in matcher.get_opcodes():
        if op == "equal":
            for la, ra in zip(left[a1:a2], right[b1:b2]):
                if critical(la) or critical(ra):
                    if exact_form(la) != exact_form(ra):
                        result["disagreements"].append({"kind": "critical", "canonical": la, "ocr": ra, "at": a1})
            continue
        seg_l, seg_r = left[a1:a2], right[b1:b2]
        if op == "replace" and not any(critical(t) for t in seg_l + seg_r):
            fl, fr = [fold(t) for t in seg_l], [fold(t) for t in seg_r]
            if len(seg_l) == len(seg_r) and all(len(x) >= 4 and len(y) >= 4 and edits(x, y) <= 1 for x, y in zip(fl, fr)):
                result["toleratedLetterNoise"].extend({"canonical": x, "ocr": y} for x, y in zip(seg_l, seg_r))
                continue
            if "".join(fl) == "".join(fr):
                result["toleratedSplitMerge"].append({"canonical": " ".join(seg_l), "ocr": " ".join(seg_r)})
                continue
        result["disagreements"].append({"kind": op, "canonical": " ".join(seg_l), "ocr": " ".join(seg_r), "at": a1})
    result["agrees"] = not result["disagreements"]
    return result


def locate(canonical: str, ocr_tokens: list[str]) -> tuple[int, int] | None:
    """The span of an OCR token stream that best covers the canonical text (start, end), by folded alignment."""
    left = [fold(t) for t in tokens(canonical)]
    right = [fold(t) for t in ocr_tokens]
    if not left or not right:
        return None
    matcher = difflib.SequenceMatcher(a=left, b=right, autojunk=False)
    blocks = [b for b in matcher.get_matching_blocks() if b.size > 0]
    if not blocks:
        return None
    # The longest run anchors the article; extend over neighbouring blocks in canonical order.
    anchor = max(blocks, key=lambda b: b.size)
    chosen = [anchor]
    for b in sorted(blocks, key=lambda b: b.a):
        if b is anchor:
            continue
        last = max(chosen, key=lambda c: c.a)
        first = min(chosen, key=lambda c: c.a)
        if b.a > last.a and b.b >= last.b + last.size and b.b - (last.b + last.size) <= 3 + (b.a - last.a - last.size) * 2:
            chosen.append(b)
        elif b.a < first.a and b.b + b.size <= first.b and first.b - (b.b + b.size) <= 3 + (first.a - b.a - b.size) * 2:
            chosen.append(b)
    start = min(c.b for c in chosen) - min(c.a for c in chosen) if min(c.a for c in chosen) <= 3 else min(c.b for c in chosen)
    end_block = max(chosen, key=lambda c: c.a + c.size)
    tail = len(left) - (end_block.a + end_block.size)
    end = end_block.b + end_block.size + (tail if tail <= 3 else 0)
    return max(0, start), min(len(right), end)


def tsv_lines(tsv_text: str) -> list[dict]:
    """Tesseract TSV words grouped into lines and re-ordered top to bottom.

    Tesseract emits indented paragraph lines as separate blocks, sometimes after the following lines; within one column
    the reading order is simply the vertical order of the lines. Word order inside a line is kept as Tesseract gives it
    (logical order). Coordinates are those of the OCR image (the column crop).
    """
    groups: dict[tuple, dict] = {}
    rows = tsv_text.splitlines()
    for row in rows[1:]:
        cells = row.split("\t")
        if len(cells) < 12 or cells[0] != "5" or not cells[11].strip():
            continue
        key = (int(cells[2]), int(cells[3]), int(cells[4]))
        left, top, width, height = (int(c) for c in cells[6:10])
        line = groups.setdefault(key, {"words": [], "left": left, "top": top, "right": left + width, "bottom": top + height})
        line["words"].append({"text": cells[11], "left": left, "top": top, "width": width, "height": height, "conf": float(cells[10])})
        line["left"], line["top"] = min(line["left"], left), min(line["top"], top)
        line["right"], line["bottom"] = max(line["right"], left + width), max(line["bottom"], top + height)
    return sorted(groups.values(), key=lambda l: ((l["top"] + l["bottom"]) / 2, -l["right"]))


def ocr_tokens_from_tsv(tsv_text: str, source: str = "") -> list[dict]:
    """Token stream (reading order) with the pixel box of the word each token came from."""
    out = []
    for line_no, line in enumerate(tsv_lines(tsv_text)):
        for word in line["words"]:
            for tok in tokens(word["text"]):
                out.append({"token": tok, "source": source, "line": line_no,
                            "box": [word["left"], word["top"], word["left"] + word["width"], word["top"] + word["height"]]})
    return out


def line_bands(gray, ink_threshold: int = 160, min_ink: int = 3, min_gap: int = 4, min_height: int = 8,
               pad: int = 4) -> list[tuple[int, int]]:
    """Text-line bands of a single column image from its horizontal ink profile (deterministic).

    gray: 2-D numpy uint8 array. A row is inked when it has at least min_ink pixels darker than ink_threshold.
    Bands closer than min_gap blank rows are merged; bands thinner than min_height are dropped as rules/noise.
    Returns (top, bottom) pixel rows, padded and clipped to the image. Tesseract's own page segmentation was observed to
    drop whole indented lines on the JO 21/2008 layout, so each band is recognized on its own (single-line mode).
    """
    import numpy as np
    inked = (gray < ink_threshold).sum(axis=1) >= min_ink
    bands, start, gap = [], None, 0
    for y, on in enumerate(inked):
        if on:
            if start is None:
                start = y
            gap = 0
            end = y
        elif start is not None:
            gap += 1
            if gap >= min_gap:
                bands.append((start, end))
                start, gap = None, 0
    if start is not None:
        bands.append((start, end))
    height = gray.shape[0]
    return [(max(0, t - pad), min(height, b + 1 + pad)) for t, b in bands if b - t + 1 >= min_height]


OK_STATES = {"exact", "noise", "merge"}


def token_statuses(canonical_tokens: list[str], ocr_tokens: list[str], *, strict: bool = False) -> tuple[list[str], dict[int, list[str]]]:
    """Per canonical token: exact | noise | merge | split | miss; and OCR tokens inserted before canonical index i.

    merge: OCR joined words that the canonical text separates (letters identical) — tolerated OCR spacing noise, unless
    a canonical part is a single letter (a word broken by a stray space in the native text).
    split: the canonical text joins what OCR separates (letters identical) — NOT tolerated, because a native-text
    clean-up (e.g. joining kashida-split words) could have merged two words wrongly.
    strict: folding is alignment-only for every token; ordinary-word edit noise and digit joins cannot pass.
    """
    status = ["miss"] * len(canonical_tokens)
    inserted: dict[int, list[str]] = {}
    if not canonical_tokens or not ocr_tokens:
        return status, inserted
    matcher = difflib.SequenceMatcher(a=[fold(t) for t in canonical_tokens], b=[fold(t) for t in ocr_tokens], autojunk=False)
    for op, a1, a2, b1, b2 in matcher.get_opcodes():
        seg_l, seg_r = canonical_tokens[a1:a2], ocr_tokens[b1:b2]
        if op == "equal":
            for k, (la, ra) in enumerate(zip(seg_l, seg_r)):
                status[a1 + k] = "miss" if (strict or critical(la) or critical(ra)) and exact_form(la) != exact_form(ra) else "exact"
            continue
        if op == "replace":
            fl, fr = [fold(t) for t in seg_l], [fold(t) for t in seg_r]
            if not strict and not any(critical(t) for t in seg_l + seg_r) and len(seg_l) == len(seg_r) \
                    and all(len(x) >= 4 and len(y) >= 4 and edits(x, y) <= 1 for x, y in zip(fl, fr)):
                for k in range(len(seg_l)):
                    status[a1 + k] = "noise"
                continue
            if "".join(exact_form(t) for t in seg_l) == "".join(exact_form(t) for t in seg_r):
                if strict and any(any(c.isdigit() for c in t) for t in seg_l + seg_r):
                    continue
                state = "merge" if len(seg_l) > len(seg_r) else "split"
                if state == "merge" and any(len(exact_form(t)) == 1 and t.isalpha() for t in seg_l):
                    state = "split"  # «ه ذا», «ع ليه»: a lone letter in the native text is a broken word, not OCR spacing
                for k in range(len(seg_l)):
                    status[a1 + k] = state
                continue
        if op in ("replace", "insert"):
            inserted.setdefault(a1, []).extend(seg_r)
    return status, inserted


def _insert_key(token: str) -> str | None:
    t = fold(token)
    if any(c.isdigit() for c in t) or (len(token) == 1 and token in "،.؛:()"):
        return exact_form(token)
    return t if len(t) >= 2 and not (len(token) == 1 and token in PUNCT) else None


def compare_streams(canonical: str, spans: dict[str, list[str]], skip: int = 0) -> dict:
    """Combine independent OCR streams (name -> token span) under the rule in the module docstring.

    A canonical token agrees when at least one stream reads it (exact for critical tokens; one-letter noise or an
    OCR-side merge for ordinary words). An extra token counts only when every stream inserts the same token at the same
    place (±1 position), so one recognizer's junk is never a finding. The first `skip` canonical tokens (the printed
    article label used for alignment) are excluded from the verdict.
    """
    left = tokens(canonical)
    per_stream = {name: token_statuses(left, span) for name, span in spans.items()}
    ok = [any(per_stream[n][0][i] in OK_STATES for n in per_stream) for i in range(len(left))]
    disagreements = []
    i = skip
    while i < len(left):
        if ok[i]:
            i += 1
            continue
        j = i
        while j < len(left) and not ok[j]:
            j += 1
        disagreements.append({"kind": "unconfirmed", "canonical": " ".join(left[i:j]), "at": i,
                              "streams": {n: per_stream[n][0][i:j] for n in per_stream}})
        i = j
    if per_stream:
        names = list(per_stream)
        gaps = set().union(*[set(per_stream[n][1]) for n in names])
        for gap in sorted(g for g in gaps if g > skip):
            common = None
            for n in names:
                keys = {k for g in (gap - 1, gap, gap + 1) for k in map(_insert_key, per_stream[n][1].get(g, [])) if k}
                common = keys if common is None else common & keys
            if common:
                disagreements.append({"kind": "extra-in-all-streams", "canonical": "", "at": gap, "ocr": sorted(common)})
    counted = len(left) - skip
    return {"agrees": not disagreements and counted > 0 and bool(spans), "canonicalTokens": counted,
            "confirmedBy": {n: sum(1 for k in range(skip, len(left)) if per_stream[n][0][k] in OK_STATES) for n in per_stream},
            "disagreements": disagreements}


def locate_span(canonical_tokens: list[str], ocr_tokens: list[str], start: int = 0, window: int = 4000) -> tuple[int, int] | None:
    """Best contiguous span of ocr_tokens[start:start+window] for the canonical tokens (label first), by folded alignment.

    Anchored on the longest matching run, extended over neighbouring matched runs whose OCR gap stays proportionate to
    the canonical gap; unmatched article ends are extended one-to-one. Returns absolute (begin, end) or None.
    """
    window_tokens = ocr_tokens[start:start + window]
    a = [fold(t) for t in canonical_tokens]
    b = [fold(t) for t in window_tokens]
    if not a or not b:
        return None
    blocks = [m for m in difflib.SequenceMatcher(a=a, b=b, autojunk=False).get_matching_blocks() if m.size > 0]
    if not blocks:
        return None
    anchor = max(range(len(blocks)), key=lambda k: blocks[k].size)
    if blocks[anchor].size < min(3, len(a)):
        return None
    lo = hi = anchor
    while lo > 0:
        prev, cur = blocks[lo - 1], blocks[lo]
        if cur.b - (prev.b + prev.size) <= 2 * (cur.a - (prev.a + prev.size)) + 8:
            lo -= 1
        else:
            break
    while hi < len(blocks) - 1:
        cur, nxt = blocks[hi], blocks[hi + 1]
        if nxt.b - (cur.b + cur.size) <= 2 * (nxt.a - (cur.a + cur.size)) + 8:
            hi += 1
        else:
            break
    first, last = blocks[lo], blocks[hi]
    begin = max(0, first.b - first.a)
    end = min(len(b), last.b + last.size + (len(a) - (last.a + last.size)))
    return start + begin, start + end
