#!/usr/bin/env python3
"""Record explicit visual collation with the original JO; generation and OCR never infer a visual verdict.

Current Ministry edition spellings remain distinct from literal JO transcription. A collation verdict concerns
clauses, words, figures, negation and paragraph structure; it does not silently normalize either raw witness.
"""
from __future__ import annotations

import argparse

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
VIEWS = pipeline.CACHE / "reviews/family-jo-final"
RECORD = WORK / "jo-collation-decisions.json"


def record(numbers: list[str], note: str, outcome: str, extra_images: list[str]) -> None:
    inputs = pipeline.read_json(WORK / "base-unchanged-input.json")
    current = {a["number"]: a for a in pipeline.read_json(WORK / "unchanged-ministry-reviewed.json")}
    audit_path = WORK / "base-unchanged-exact-audit.json"
    audit = pipeline.read_json(audit_path)
    results = {r["number"]: r for r in audit["results"]}
    data = pipeline.read_json(RECORD) if RECORD.exists() else {
        "sourceId": "A1984024", "sourceSha256": "72a9290493fe6b94b7b4ab6547cfebafb6d4f85c7530a2c80a9fcad8ca378c9b",
        "reviewedOn": "2026-10-28", "reviewer": "Codex (AI), actual reading of official source images; not human/lawyer verification",
        "inputSha256": pipeline.hash_file(WORK / "base-unchanged-input.json"), "auditSha256": pipeline.hash_file(audit_path),
        "decisions": [], "verificationEffect": "Independent original-JO collation; literal typography, current Ministry transcription and legal applicability remain separate"}
    seen = {d["number"] for d in data["decisions"]}
    by_number = {a["number"]: a for a in inputs}
    if not note.strip() or len(numbers) != len(set(numbers)) or set(numbers) & seen:
        raise ValueError("Explicit unique unrecorded numbers and a review note are required")
    for number in numbers:
        if number not in by_number or results[number]["agrees"]:
            raise ValueError("Number is outside the outstanding original-JO view set")
        images = sorted(VIEWS.glob(f"article-{number}-*.png")) + [(pipeline.ROOT / p).resolve() for p in extra_images]
        if not images or any(not p.is_relative_to(pipeline.ROOT.resolve()) or not p.exists() for p in images):
            raise ValueError("Missing or unsafe visual evidence")
        data["decisions"].append({"number": number, "outcome": outcome, "note": note,
            "viewTextSha256": pipeline.hash_text(by_number[number]["text"]),
            "candidateTextSha256": pipeline.hash_text(current[number]["text"]),
            "images": [str(p.relative_to(pipeline.ROOT)).replace("\\", "/") for p in images],
            "imageSha256s": [pipeline.hash_file(p) for p in images], "regions": results[number]["regions"]})
    pipeline.write_json(RECORD, data)
    print(len(data["decisions"]), "explicit JO image collations recorded; no whole-code state inferred")


def check() -> None:
    data = pipeline.read_json(RECORD)
    if data["inputSha256"] != pipeline.hash_file(WORK / "base-unchanged-input.json") or data["auditSha256"] != pipeline.hash_file(WORK / "base-unchanged-exact-audit.json"):
        raise ValueError("Collation input/audit changed")
    current = {a["number"]: a for a in pipeline.read_json(WORK / "unchanged-ministry-reviewed.json")}
    seen = set()
    for decision in data["decisions"]:
        number = decision["number"]
        if number in seen or decision["candidateTextSha256"] != pipeline.hash_text(current[number]["text"]):
            raise ValueError("Duplicate or stale candidate collation")
        seen.add(number)
        if not decision["images"] or len(decision["images"]) != len(decision["imageSha256s"]):
            raise ValueError("Missing bound proof")
        for image, expected in zip(decision["images"], decision["imageSha256s"]):
            path = (pipeline.ROOT / image).resolve()
            if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != expected:
                raise ValueError("Collation proof changed")
    audit = pipeline.read_json(WORK / "base-unchanged-exact-audit.json")
    outstanding = {r["number"] for r in audit["results"] if not r["agrees"]}
    print(len(seen), "of", len(outstanding), "image collations present; missing", sorted(outstanding - seen, key=int))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--record", nargs="+", default=[])
    parser.add_argument("--note", default="")
    parser.add_argument("--outcome", choices=("same-clauses-edition-orthography", "wording-differs", "needs-full-page"), default="same-clauses-edition-orthography")
    parser.add_argument("--extra-image", action="append", default=[])
    args = parser.parse_args()
    if args.record:
        record(args.record, args.note, args.outcome, args.extra_image)
    else:
        check()


if __name__ == "__main__":
    main()
