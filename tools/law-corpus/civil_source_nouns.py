#!/usr/bin/env python3
"""Two explicitly source-read nouns whose broken native prefix resembles an independent negation.

This is a closed transcription exception registry, not a general negation override or runtime legal rule.
All other spacing changes must use the unchanged general guards.
"""
from __future__ import annotations

import unicodedata

import civil_flag_spacing
import native_check as nc
import pipeline

SOURCE_SHA = "17a555bf81d5b99bf67417f3d42c157fd5df69499d6190cffbf182bc3566b573"
SOURCE_NOUNS = {"main:64": ("الا يجاب", "الايجاب"), "main:456": ("الا قتصادي", "الاقتصادي")}


def project(body: dict, reading: dict, source_sha: str, root=pipeline.ROOT) -> dict:
    expected = SOURCE_NOUNS.get(body["candidateKey"])
    proposal = reading["proposal"]
    if (expected is None or proposal is None or (proposal["old"], proposal["new"]) != expected
            or reading["candidateKey"] != body["candidateKey"] or reading["sourceReadNounNotNegation"] is not True
            or reading["sourceReading"] != expected[1] or source_sha != SOURCE_SHA
            or reading["finding"] != "source-read-joined-word-proposed"
            or reading["inspection"] != "actually read; not human/lawyer verification"):
        raise ValueError("Only the two explicitly source-read noun restorations are permitted")
    text = body["text"]
    if (pipeline.hash_text(text) != reading["inputTextSha256"] or text != "\n".join(body["paragraphs"])
            or text != unicodedata.normalize("NFC", text) or nc.TATWEEL in text):
        raise ValueError("Source noun proof belongs to another raw body")
    if not reading["images"] or len(reading["images"]) != len(reading["imageSha256s"]):
        raise ValueError("Source noun needs its actual image reading")
    for image, sha in zip(reading["images"], reading["imageSha256s"]):
        path = (root / image).resolve()
        if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
            raise ValueError("Source noun image proof changed or escaped the workspace")
    old, new = expected
    if not civil_flag_spacing.deletion_only(old, new) or any(c.isdigit() for c in old + new):
        raise ValueError("Source noun cannot change letters or digits")
    prefix = body["printedLabel"].rstrip(": ").strip() + " : "
    spans = list(nc.TOKEN.finditer(prefix + text))
    at = reading["at"]
    if type(at) is not int or at < 0 or at + 2 > len(spans):
        raise ValueError("Source noun token position is outside the raw body")
    selected = spans[at:at + 2]
    start, end = selected[0].start() - len(prefix), selected[-1].end() - len(prefix)
    if start < 0 or [m.group() for m in selected] != nc.tokens(old) or text[start:end] != old:
        raise ValueError("Source noun does not cover the exact flagged source span")
    final = text[:start] + new + text[end:]
    return {"text": final, "paragraphs": final.split("\n"), "textSha256": pipeline.hash_text(final),
            "rawTextSha256": pipeline.hash_text(text), "sourceNounRestoration": {"old": old, "new": new, "at": at,
                "sourceSha256": source_sha, "imageSha256s": reading["imageSha256s"],
                "note": "Actual printed noun, not an independent source negation; closed source-bound transcription exception."}}
