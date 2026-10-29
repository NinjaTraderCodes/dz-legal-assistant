#!/usr/bin/env python3
"""Delete only source-read spaces at exact raw Civil audit-token spans; retain every other byte of text.

No digit join, moved word boundary, absorbed independent negation, guessed token position or cross-paragraph
replacement is accepted. The caller must replay the source/body/image bindings for each proposed correction.
"""
from __future__ import annotations

import unicodedata

import check_native
import native_check as nc
import pipeline


def deletion_only(old: str, new: str) -> bool:
    if not old or not new or old == new:
        return False
    cursor = 0
    for char in old:
        if cursor < len(new) and char == new[cursor]:
            cursor += 1
        elif char != " ":
            return False
    return cursor == len(new)


def project(body: dict, corrections: list[dict]) -> dict:
    text = body["text"]
    if (text != "\n".join(body["paragraphs"]) or unicodedata.normalize("NFC", text) != text or nc.TATWEEL in text
            or nc.tokens(check_native.comparable(text)) != nc.tokens(text)):
        raise ValueError("Raw token mapping requires intact paragraphs, NFC, no tatweel or changed dot leaders")
    prefix = body["printedLabel"].rstrip(": ").strip() + " : "
    full = prefix + text
    spans = list(nc.TOKEN.finditer(full))
    operations = []
    for correction in corrections:
        old, new, at = correction["old"], correction["new"], correction["at"]
        if (not deletion_only(old, new) or any(c.isdigit() for c in old + new)
                or any(c in "\r\n\t" for c in old + new)):
            raise ValueError("A source-read spacing projection can only delete spaces, never join digits or move characters")
        before, after = nc.tokens(old), nc.tokens(new)
        old_neg = [t for t in before if nc.is_negation(t) or t == "ما"]
        new_neg = [t for t in after if nc.is_negation(t) or t == "ما"]
        if old_neg != new_neg:
            if not (len(after) == 1 and new_neg == after and correction.get("sourceReadJoinedNegation") is True):
                raise ValueError("Independent negation cannot be absorbed; an exact restoration needs its own source-read marker")
        if type(at) is not int or at < 0 or at + len(before) > len(spans):
            raise ValueError("Source-read token position is outside the raw body")
        selected = spans[at:at + len(before)]
        if [m.group() for m in selected] != before:
            raise ValueError("Flagged token position does not contain the proposed old phrase")
        start, end = selected[0].start() - len(prefix), selected[-1].end() - len(prefix)
        if (start < 0 or text[start:end] != old or "\n" in text[start:end]
                or any(start < prior["end"] and prior["start"] < end for prior in operations)):
            raise ValueError("Spacing span changes a label, paragraph boundary, overlapping correction or different raw phrase")
        operations.append({"start": start, "end": end, "old": old, "new": new, "at": at,
                           "sourceReadJoinedNegation": correction.get("sourceReadJoinedNegation", False)})
    final = text
    for operation in sorted(operations, key=lambda op: op["start"], reverse=True):
        final = final[:operation["start"]] + operation["new"] + final[operation["end"]:]
    return {"text": final, "paragraphs": final.split("\n"), "textSha256": pipeline.hash_text(final),
            "rawTextSha256": pipeline.hash_text(text), "appliedSpacingOperations": operations}
