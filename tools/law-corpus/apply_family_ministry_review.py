#!/usr/bin/env python3
"""Apply explicit whole-article image readings to unchanged Family bodies; never infer whole-code completion.

The Ministry compilation is the transcription witness, not a substitute legislative publication. The separate JO
comparisons remain recorded. The 37 amending-JO bodies and five repeals are deliberately outside this operation.
"""
from __future__ import annotations

import copy

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def apply(articles: list[dict], audit: dict, review: dict, structure: dict, root=pipeline.ROOT) -> list[dict]:
    base = {a["number"]: copy.deepcopy(a) for a in articles if not a["amendedBy0502"] and not a["repealedBy0502"]}
    results = {r["number"]: r for r in audit["results"]}
    decisions = {}
    for decision in review["decisions"]:
        number = decision["number"]
        if number in decisions or number not in base:
            raise ValueError("duplicate review or article outside unchanged scope")
        if decision["decision"] not in {"body-and-paragraphs-match-print", "paragraph-break-differs"}:
            raise ValueError("unresolved image decision")
        if not decision["images"] or len(decision["images"]) != len(decision["imageSha256s"]):
            raise ValueError("image decision lacks bound evidence")
        for image, expected in zip(decision["images"], decision["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != expected:
                raise ValueError("review image changed or escaped the workspace")
        article = base[number]
        if pipeline.hash_text(article["text"]) != decision["textSha256"]:
            raise ValueError("review belongs to a different body")
        if decision["decision"] == "paragraph-break-differs":
            after = decision["joinAfter"]
            paragraphs = article["paragraphs"]
            if not 0 < after < len(paragraphs):
                raise ValueError("paragraph join is outside the article")
            article["paragraphs"] = paragraphs[:after - 1] + [paragraphs[after - 1] + " " + paragraphs[after]] + paragraphs[after + 1:]
            article["text"] = "\n".join(article["paragraphs"])
        decisions[number] = decision
    structural = {r["number"] for r in structure["paragraphCountDifferences"]}
    for number, article in base.items():
        if number not in results:
            raise ValueError("unchanged article lacks literal audit")
        result = results[number]
        word_items = [d for d in result["disagreements"] if not d["punctuationOnly"]]
        if result["wordAgrees"] != (not word_items):
            raise ValueError("word agreement contradicts the recorded discrepancies")
        decision = decisions.get(number)
        if (word_items or number in structural) and not decision:
            raise ValueError(f"article {number} still needs image review")
        # Unreviewed punctuation remains disclosed, as authorized; word/digit differences never take this path.
        marks = [] if decision else [d for d in result["disagreements"] if d["punctuationOnly"]]
        article["bodyCheck"] = {
            "result": "image-resolved" if decision else "strict-ocr-word-agreement",
            "textSha256": pipeline.hash_text(article["text"]),
            "imageSha256s": decision["imageSha256s"] if decision else [],
            "unconfirmedPunctuation": marks,
            "paragraphCheck": "source-image" if decision else "native-layout; no French-count discrepancy",
            "rule": "family_literal exact word/number acceptance, with native_check.exact_form normalization; "
                    "remaining word discrepancies require an explicit hash-bound source-image decision",
            "verificationEffect": "Ministry-edition body comparison only; JO collation, paths, temporal and whole-code checks remain separate",
        }
    return list(base.values())


def main() -> int:
    inputs = WORK / "ministry-raw-input.json"
    audit_path = WORK / "ministry-body-exact-audit.json"
    audit = pipeline.read_json(audit_path)
    review = pipeline.read_json(WORK / "image-decisions-ministry-whole.json")
    if audit["inputSha256"] != pipeline.hash_file(inputs) or review["inputSha256"] != pipeline.hash_file(inputs):
        raise ValueError("audit/review belongs to a different input")
    if review["auditSha256"] != pipeline.hash_file(audit_path):
        raise ValueError("review belongs to a different audit")
    result = apply(pipeline.read_json(inputs), audit, review, pipeline.read_json(WORK / "french-structure.json"))
    if len(result) != 190:
        raise ValueError("unchanged Family inventory is not 190 bodies")
    pipeline.write_json(WORK / "unchanged-ministry-reviewed.json", result)
    print(len(result), "unchanged bodies word-settled;", len(review["decisions"]), "whole-article image readings;",
          sum(bool(a["bodyCheck"]["unconfirmedPunctuation"]) for a in result), "bodies retain punctuation notes")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
