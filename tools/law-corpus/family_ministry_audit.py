#!/usr/bin/env python3
"""Literal OCR check of the official Ministry edition against that same edition's printed Arabic pages.

Keep the JO comparison separate: edition spelling is not an OCR error. The superior amending JO wording and its
explicit image corrections still control the 37 replaced/inserted bodies. This additional witness cannot overrule it.
Annotation tokens are excluded only from body acceptance, retained in full for alignment and auditing, and the
remaining token projection must exactly equal the already prepared body. No approximate-word acceptance is used.
"""
from __future__ import annotations

import check_native
import family_literal
import native_check as nc
import pipeline
import prepare_family as family
import text_clean

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/family-ministry-manifest.json"


def input_for(article: dict) -> dict:
    label = "المادة " + ("الأولى" if article["number"] == "1" else article["number"])
    raw = "\n".join(text_clean.paragraph(p) for p in article["rawParagraphs"])
    excluded = [(m.start(), m.end()) for pattern in (family.ANNOTATION, family.REPEALED) for m in pattern.finditer(raw)]
    flags = [not any(lo <= m.start() and m.end() <= hi for lo, hi in excluded) for m in nc.TOKEN.finditer(raw)]
    raw_tokens = nc.tokens(raw)
    projected = [t for t, keep in zip(raw_tokens, flags) if keep]
    body_tokens = nc.tokens(article["text"])
    if [nc.exact_form(t) for t in projected] != [nc.exact_form(t) for t in body_tokens]:
        raise ValueError(f"annotation exclusion changed the body of {article['number']}")
    offset = len(nc.tokens(label + " :"))
    return article | {"label": label, "text": raw, "paragraphs": raw.split("\n"), "preparedBody": article["text"],
                      "bodyTokenIndices": [offset + i for i, keep in enumerate(flags) if keep]}


def audit(articles: list[dict], check: dict) -> dict:
    by_number = {r["number"]: r for r in check["results"]}
    out = []
    for article in articles:
        result = by_number[article["number"]]
        full = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        body = article["bodyTokenIndices"]
        words, marks = family_literal.compare(full, result["ocr"], body)
        out.append(result | {"agrees": bool(body) and not words and not marks,
                             "wordAgrees": bool(body) and not words,
                             "agreesAfterReread": bool(body) and not words and not marks,
                             "disagreements": words + marks, "bodyTokens": len(body),
                             "repealMarkerOnly": not body})
    return {"source": "family-consolidated-bilingual", "method": __doc__.strip(), "results": out,
            "articles": len(out), "bodyArticles": sum(not r["repealMarkerOnly"] for r in out),
            "agreed": sum(r["agrees"] for r in out),
            "wordAgreed": sum(r["wordAgrees"] for r in out),
            "wordReviewGroups": sum(not d["punctuationOnly"] for r in out for d in r["disagreements"]),
            "punctuationReviewGroups": sum(d["punctuationOnly"] for r in out for d in r["disagreements"]),
            "verificationEffect": "additional transcription witness only; no whole-code or amendment check inferred"}


def main() -> int:
    native = pipeline.read_json(WORK / "native.json")
    inputs = [input_for(a) for a in native["articles"]]
    pipeline.write_json(WORK / "ministry-raw-input.json", inputs)
    check = check_native.run(inputs, MANIFEST, "family-consolidated-bilingual")
    pipeline.write_json(WORK / "ministry-raw-comparison.json", check)
    result = audit(inputs, check)
    result |= {"inputSha256": pipeline.hash_file(WORK / "ministry-raw-input.json"), "manifestSha256": pipeline.hash_file(MANIFEST),
               "source": native["source"]}
    pipeline.write_json(WORK / "ministry-body-exact-audit.json", result)
    print(result["wordAgreed"], "/", result["bodyArticles"], "word agreements;", result["wordReviewGroups"],
          "word/digit/extra groups;", result["punctuationReviewGroups"], "punctuation groups")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
