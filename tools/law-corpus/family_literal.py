"""Exact Family word/mark alignment without the legacy tokenizer's Arabic comma/semicolon attachment."""
from __future__ import annotations

import unicodedata

import native_check as nc


def expand(tokens: list[str]) -> tuple[list[str], list[int]]:
    pieces, owners = [], []
    for owner, token in enumerate(tokens):
        word = ""
        for ch in token:
            if unicodedata.category(ch)[0] in {"L", "M", "N"}:
                word += ch
            else:
                if word:
                    pieces.append(word)
                    owners.append(owner)
                    word = ""
                pieces.append(ch)
                owners.append(owner)
        if word:
            pieces.append(word)
            owners.append(owner)
    return pieces, owners


def compare(tokens: list[str], streams: dict[str, str], body: list[int]) -> tuple[list[dict], list[dict]]:
    canonical, owners = expand(tokens)
    readings = {name: nc.token_statuses(canonical, expand(nc.tokens(text))[0], strict=True) for name, text in streams.items()}
    body_set = set(body)
    missing_words, missing_marks = set(), set()
    for i, (token, owner) in enumerate(zip(canonical, owners)):
        if owner not in body_set or any(states[i] in {"exact", "merge"} for states, _ in readings.values()):
            continue
        numeric_separator = (i > 0 and i + 1 < len(canonical)
                             and canonical[i - 1].isdigit() and canonical[i + 1].isdigit())
        if any(c.isalpha() or c.isdigit() for c in token) or numeric_separator:
            missing_words.add(owner)
        else:
            missing_marks.add(owner)
    groups = []
    for i in sorted(missing_words):
        if groups and groups[-1]["last"] == i - 1:
            groups[-1]["last"] = i
            groups[-1]["canonical"] += " " + tokens[i]
        else:
            groups.append({"kind": "unconfirmed", "at": i, "last": i, "canonical": tokens[i], "punctuationOnly": False})
    marks = [{"kind": "unconfirmed", "at": i, "canonical": tokens[i], "punctuationOnly": True}
             for i in sorted(missing_marks - missing_words)]
    # An extra word needs consensus; retain the original source-bound gap, without an Arabic-letter fold.
    if readings and body:
        gaps = set().union(*(set(inserted) for _, inserted in readings.values()))
        for gap in sorted(gaps):
            owner = owners[gap] if gap < len(owners) else len(tokens)
            if not body[0] <= owner <= body[-1] + 1:
                continue
            common = None
            for _, inserted in readings.values():
                found = {nc.exact_form(t) for g in (gap - 1, gap, gap + 1) for t in inserted.get(g, [])
                         if any(c.isalpha() or c.isdigit() for c in t)}
                common = found if common is None else common & found
            if common:
                groups.append({"kind": "extra-in-all-streams", "at": owner, "canonical": "", "ocr": sorted(common),
                               "punctuationOnly": False})
    return groups, marks
