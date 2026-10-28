#!/usr/bin/env python3
"""Compare unannotated base-number Civil candidates with cached original-JO OCR; never infer current legal status.

Editorial annotation absence does not establish amendment history. Differences still require source decisions,
and agreements do not certify paths, paragraphs, applicability or complete current consolidation.
"""
from __future__ import annotations

import re

import check_native
import family_literal
import native_check as nc
import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/civil-full-manifest.json"


def main() -> None:
    native_path = WORK / "native-candidates.json"
    native = pipeline.read_json(native_path)
    articles = [a | {"label": "المادة " + ("الأولى" if a["number"] == "1" else a["number"])}
                for a in native["articles"] if a["numberScope"] == "main" and a["number"].isdigit()
                and not re.match(r"^\((?:معدلة|جديدة|ملغاة)\)", a["text"])]
    inputs = WORK / "base-unannotated-input.json"
    pipeline.write_json(inputs, articles)
    manifest = pipeline.load_manifest(MANIFEST)
    source = next(s for s in manifest["sources"] if s["id"] == "A1975078")
    if pipeline.hash_file(pipeline.CACHE / "A1975078.pdf") != source["sha256"]:
        raise ValueError("Original Civil JO differs from its pin")
    ids = {e["id"] for e in manifest["extracts"] if e["sourceId"] == source["id"]}
    records = pipeline.evidence_by_id(pipeline.CACHE, ids)
    if set(records) != ids:
        raise ValueError("Original Civil source evidence is incomplete")
    for record, _ in records.values():
        if record["sourceSha256"] != source["sha256"]:
            raise ValueError("Source evidence belongs to a different publication")
        for artifact in record["artifacts"]:
            path = (pipeline.CACHE / artifact["path"]).resolve()
            if not path.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(path) != artifact["sha256"]:
                raise ValueError("Original Civil evidence changed")
    print("Aligning", len(articles), "unannotated base-number candidates with two cached OCR streams", flush=True)
    comparison = check_native.run(articles, MANIFEST, source["id"])
    comparison["inputSha256"] = pipeline.hash_file(inputs)
    comparison["verificationEffect"] = "none; inherited approximate alignment retained only as diagnostic evidence"
    pipeline.write_json(WORK / "base-unannotated-comparison.json", comparison)
    by_number = {r["number"]: r for r in comparison["results"]}
    results = []
    for article in articles:
        result = by_number[article["number"]]
        tokens = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        offset = len(nc.tokens(article["label"] + " :"))
        words, marks = family_literal.compare(tokens, result["ocr"], list(range(offset,len(tokens))))
        results.append(result | {"wordAgrees": len(tokens) > offset and not words,
                                 "agrees": len(tokens) > offset and not words and not marks,
                                 "disagreements": words + marks})
    output = {"source": source, "nativeSha256": pipeline.hash_file(native_path),
              "inputSha256": pipeline.hash_file(inputs), "manifestSha256": pipeline.hash_file(MANIFEST),
              "articles": len(results), "wordAgreed": sum(r["wordAgrees"] for r in results),
              "wordReviewGroups": sum(not d["punctuationOnly"] for r in results for d in r["disagreements"]),
              "punctuationReviewGroups": sum(d["punctuationOnly"] for r in results for d in r["disagreements"]),
              "method": "family_literal exact word/digit/negation acceptance; Arabic-letter folds and one-letter tolerance cannot pass",
              "results": results, "verificationEffect": __doc__.strip()}
    pipeline.write_json(WORK / "base-unannotated-exact-audit.json", output)
    print(output["wordAgreed"], "/", len(results), "strict word agreements;", output["wordReviewGroups"],
          "word/digit/extra groups;", output["punctuationReviewGroups"], "punctuation groups; no checked states")


if __name__ == "__main__":
    main()
