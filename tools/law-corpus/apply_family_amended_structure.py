#!/usr/bin/env python3
"""Replay explicit paragraph/body-boundary readings of every corrected Family amendment body.

This does not infer a new text, a commencement date, a historical application or whole-code verification.
"""
from __future__ import annotations

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def replay(articles: list[dict], review: dict, root=pipeline.ROOT) -> dict[str, dict]:
    expected = {a["number"]: a for a in articles}
    decisions = {d["number"]: d for d in review["decisions"]}
    if len(expected) != len(articles) or len(decisions) != len(review["decisions"]) or set(decisions) != set(expected):
        raise ValueError("Missing, duplicate or unexpected amended structure reading")
    for number, decision in decisions.items():
        article = expected[number]
        if (decision["outcome"] != "paragraphs-and-body-boundaries-match"
                or decision["textSha256"] != pipeline.hash_text(article["text"])
                or article["text"] != "\n".join(article["paragraphs"])
                or decision["paragraphCount"] != len(article["paragraphs"]) or not decision["note"].strip()):
            raise ValueError("Unresolved or stale amended paragraph reading")
        if not decision["images"] or len(decision["images"]) != len(decision["imageSha256s"]):
            raise ValueError("Amended structure reading lacks bound source images")
        for image, sha in zip(decision["images"],decision["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Amended source image changed or escaped workspace")
    return decisions


def main() -> dict[str, dict]:
    path = WORK / "amended-corrected.json"
    review = pipeline.read_json(WORK / "amended-structure-decisions.json")
    if review["candidateSha256"] != pipeline.hash_file(path):
        raise ValueError("Amended structure review belongs to a different candidate set")
    decisions = replay(pipeline.read_json(path),review)
    if len(decisions) != 37:
        raise ValueError("Expected37 amended body readings")
    pipeline.write_json(WORK / "amended-structure-summary.json", {
        "candidateSha256":pipeline.hash_file(path),
        "reviewSha256":pipeline.hash_file(WORK / "amended-structure-decisions.json"),
        "sourceLocatorOverridesSha256":pipeline.hash_file(WORK / "amended-source-locator-overrides.json"),
        "articles":len(decisions), "paragraphCounts":{n:d["paragraphCount"] for n,d in decisions.items()},
        "verificationEffect":__doc__.strip()})
    print("All37 amended Family paragraph/body-boundary readings replayed; no text change or whole-code state inferred")
    return decisions


if __name__ == "__main__":
    main()
