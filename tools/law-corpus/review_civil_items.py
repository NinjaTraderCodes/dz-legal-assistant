#!/usr/bin/env python3
"""Generate a bounded batch of Civil word/digit/extra review strips using the existing source-review renderer.

Generation is not inspection. Punctuation-only items remain in the exact audit but are outside this word-review
batch. The source-order indices cover qualified local identities as well as the general code numbering.
"""
from __future__ import annotations

import argparse

import civil_ministry_audit as civil
import pipeline
import review_items


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--start", type=int, default=0)
    parser.add_argument("--count", type=int, default=50)
    parser.add_argument("--per-sheet", type=int, default=6)
    args = parser.parse_args()
    if args.start < 0 or not 1 <= args.count <= 100 or not 1 <= args.per_sheet <= 12:
        raise ValueError("Invalid review batch size or source-order offset")
    inputs = civil.WORK / "ministry-raw-input.json"
    audit_path = civil.WORK / "ministry-page-bounded-exact-audit.json"
    check = pipeline.read_json(audit_path)
    if check["inputSha256"] != pipeline.hash_file(inputs) or check["manifestSha256"] != pipeline.hash_file(civil.MANIFEST):
        raise ValueError("Civil source/input changed since exact comparison")
    articles = pipeline.read_json(inputs)[args.start:args.start + args.count]
    wanted = {a["number"] for a in articles}
    results = [r | {"disagreements": [d for d in r["disagreements"] if not d["punctuationOnly"]],
                    "agrees": r["wordAgrees"], "agreesAfterReread": r["wordAgrees"]}
               for r in check["results"] if r["number"] in wanted]
    name = f"civil-exact-{args.start:04d}-{args.start + len(articles):04d}"
    index = review_items.build(articles, check | {"results": results}, civil.MANIFEST,
                               "civil-consolidated-ar", name, args.per_sheet)
    index |= {"auditSha256": pipeline.hash_file(audit_path), "inputSha256": pipeline.hash_file(inputs),
              "sourceOrderStart": args.start, "sourceOrderEnd": args.start + len(articles),
              "inspection": "not inferred from generation"}
    pipeline.write_json(pipeline.CACHE / "reviews" / name / (name + "-items.json"), index)
    print(len(index["items"]), "word-review items on", len(index["sheets"]), "source sheets; no decisions inferred")


if __name__ == "__main__":
    main()
