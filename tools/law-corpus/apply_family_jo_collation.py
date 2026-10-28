#!/usr/bin/env python3
"""Replay every original-JO collation; apply guarded fixes and quarantine unresolved edition conflicts.

The Ministry transcription check remains bound to its original text. JO checks have separate final-text hashes.
An unresolved source disagreement never becomes a passed verification check or a silently chosen legal reading.
"""
from __future__ import annotations

import copy
import re

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
SOURCE_SHA = "72a9290493fe6b94b7b4ab6547cfebafb6d4f85c7530a2c80a9fcad8ca378c9b"


def indexed(items: list[dict]) -> dict[str, dict]:
    result = {i["number"]: i for i in items}
    if len(result) != len(items):
        raise ValueError("Duplicate article/decision")
    return result


def apply(articles: list[dict], viewed: list[dict], audit: dict, review: dict,
          corrections: dict, conflicts: dict, root=pipeline.ROOT) -> list[dict]:
    bodies, views, results = indexed(copy.deepcopy(articles)), indexed(viewed), indexed(audit["results"])
    decisions, fixes, disputed = (indexed(review["decisions"]), indexed(corrections["corrections"]),
                                  indexed(conflicts["conflicts"]))
    if set(bodies) != set(views) or set(bodies) != set(results):
        raise ValueError("Original-JO inventory differs from the current unchanged body set")
    if review["sourceId"] != "A1984024" or review["sourceSha256"] != SOURCE_SHA:
        raise ValueError("Wrong legislative source")
    if conflicts["sourceId"] != review["sourceId"] or conflicts["sourceSha256"] != SOURCE_SHA:
        raise ValueError("Conflicts belong to a different source")
    expected = {n for n,r in results.items() if not r["agrees"]}
    if set(decisions) != expected:
        raise ValueError("Missing or out-of-scope original-JO image decision")
    differing = {n for n,d in decisions.items() if d["outcome"] == "wording-differs"}
    if set(fixes) & set(disputed) or set(fixes) | set(disputed) != differing:
        raise ValueError("Every wording difference needs exactly one guarded fix or unresolved conflict")
    for number, body in bodies.items():
        original = body["text"]
        if original != "\n".join(body["paragraphs"]):
            raise ValueError("Body/paragraph mismatch")
        decision = decisions.get(number)
        images = []
        if decision:
            if decision["outcome"] not in {"same-clauses-edition-orthography", "wording-differs"} or not decision["note"].strip():
                raise ValueError("Unresolved or undocumented image reading")
            if (decision["candidateTextSha256"] != pipeline.hash_text(original)
                    or decision["viewTextSha256"] != pipeline.hash_text(views[number]["text"])):
                raise ValueError("Stale candidate or viewed text")
            if (not decision["images"] or len(decision["images"]) != len(decision["imageSha256s"])
                    or decision["regions"] != results[number]["regions"]):
                raise ValueError("Missing or changed bound proof regions")
            for image, sha in zip(decision["images"], decision["imageSha256s"]):
                path = (root / image).resolve()
                if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
                    raise ValueError("Changed or unsafe source image")
            images = decision["imageSha256s"]
        elif re.sub(r"\s+", " ", views[number]["text"]) != re.sub(r"\s+", " ", original):
            raise ValueError("Unreviewed change to an OCR-agreeing candidate")
        if number in fixes:
            fix = fixes[number]
            if not fix["from"] or original.count(fix["from"]) != 1 or fix["from"] == fix["to"] or not fix["reason"].strip():
                raise ValueError("Guarded correction does not uniquely match old text")
            body["paragraphs"] = [p.replace(fix["from"], fix["to"]) for p in body["paragraphs"]]
            body["text"] = "\n".join(body["paragraphs"])
        if number in disputed:
            conflict = disputed[number]
            if (original.count(conflict["ministryQuote"]) != 1 or not conflict["joQuote"]
                    or conflict["joQuote"] == conflict["ministryQuote"] or not conflict["reason"].strip()):
                raise ValueError("Conflict does not identify two distinct source readings")
        body["joCheck"] = {"result": "unresolved-source-conflict" if number in disputed else
                                      "corrected-from-original-JO" if number in fixes else
                                      "source-image-collation" if decision else "strict-ocr-agreement",
                           "sourceSha256": SOURCE_SHA, "textSha256": pipeline.hash_text(body["text"]),
                           "ministryTextSha256": pipeline.hash_text(original), "imageSha256s": images,
                           "note": disputed[number]["reason"] if number in disputed else
                                   fixes[number]["reason"] if number in fixes else
                                   decision["note"] if decision else "Original strict audit agrees; no image decision inferred.",
                           "conflict": disputed.get(number),
                           "verificationEffect": "Original-JO body collation only; no temporal or whole-code certification"}
    return list(bodies.values())


def main() -> None:
    review = pipeline.read_json(WORK / "jo-collation-decisions.json")
    for key, filename in [("inputSha256", "base-unchanged-input.json"), ("auditSha256", "base-unchanged-exact-audit.json")]:
        if review[key] != pipeline.hash_file(WORK / filename):
            raise ValueError("Review input/audit changed")
    bodies = apply(pipeline.read_json(WORK / "unchanged-ministry-reviewed.json"),
                   pipeline.read_json(WORK / "base-unchanged-input.json"),
                   pipeline.read_json(WORK / "base-unchanged-exact-audit.json"), review,
                   pipeline.read_json(WORK / "jo-collation-corrections.json"),
                   pipeline.read_json(WORK / "jo-collation-conflicts.json"))
    if len(bodies) != 190:
        raise ValueError("Expected 190 unchanged Family bodies")
    pipeline.write_json(WORK / "unchanged-jo-collated.json", bodies)
    counts = {state: sum(a["joCheck"]["result"] == state for a in bodies)
              for state in sorted({a["joCheck"]["result"] for a in bodies})}
    pipeline.write_json(WORK / "jo-collation-summary.json", {
        "sourceId": "A1984024", "sourceSha256": SOURCE_SHA,
        "inputs": {name: pipeline.hash_file(WORK / name) for name in ["unchanged-ministry-reviewed.json",
            "base-unchanged-input.json", "base-unchanged-exact-audit.json", "jo-collation-decisions.json",
            "jo-collation-corrections.json", "jo-collation-conflicts.json"]},
        "bodyCount": len(bodies), "imageDecisionCount": len(review["decisions"]), "counts": counts,
        "unresolvedArticles": [a["number"] for a in bodies if a["joCheck"]["conflict"]],
        "verificationEffect": "No whole-code certification; four official-edition conflicts remain explicit"})
    print("Original-JO collation replayed:", counts)


if __name__ == "__main__":
    main()
