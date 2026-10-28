#!/usr/bin/env python3
"""Apply source-image Family decisions with exact old-text guards; never infer whole-code completion."""
from __future__ import annotations

import copy

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def apply(articles: list[dict], audit: dict, review: dict, root=pipeline.ROOT) -> list[dict]:
    by_number = {a["number"]: a for a in copy.deepcopy(articles)}
    decisions = review["decisions"]
    def key(d):
        return (d["number"], d["kind"], d["at"], d["canonical"], tuple(d.get("ocr") or []))
    mapped = {}
    for d in decisions:
        if key(d) in mapped:
            raise ValueError("duplicate image decision")
        if d["decision"] not in {"native-matches-print", "print-differs", "settled-by-correction"}:
            raise ValueError("unresolved image decision")
        if not d.get("images") or len(d["images"]) != len(d["imageSha256s"]):
            raise ValueError("image decision lacks bound evidence")
        for image, expected in zip(d["images"], d["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != expected:
                raise ValueError("review image changed or escaped the workspace")
        mapped[key(d)] = d
    results = {r["number"]: r for r in audit["results"]}
    expected = {key(d | {"number": r["number"]}) for r in audit["results"] for d in r["disagreements"]}
    if set(mapped) != expected:
        raise ValueError("image decisions do not cover exactly the outstanding audit items")
    ids = {d["id"]: d for d in decisions}
    for d in decisions:
        if d["decision"] == "settled-by-correction":
            target = ids.get(d.get("correctionId"))
            if not target or target["decision"] != "print-differs" or target["number"] != d["number"]:
                raise ValueError("linked correction is absent or belongs to another article")
        if d["decision"] != "print-differs":
            continue
        article = by_number[d["number"]]
        if sum(p.count(d["from"]) for p in article["paragraphs"]) != 1:
            raise ValueError(f"old-text correction guard failed: {d['id']}")
        article["paragraphs"] = [p.replace(d["from"], d["to"]) for p in article["paragraphs"]]
        article["text"] = "\n".join(article["paragraphs"])
    for number, article in by_number.items():
        relevant = [d for d in decisions if d["number"] == number]
        if number not in results:
            raise ValueError("article lacks an exact audit")
        article["bodyCheck"] = {
            "result": "image-resolved" if relevant else "strict-ocr-agreement",
            "textSha256": pipeline.hash_text(article["text"]),
            "imageDecisions": [d["id"] for d in relevant],
            "imageSha256s": list(dict.fromkeys(h for d in relevant for h in d["imageSha256s"])),
            "rule": "NFC/tatweel/harakat and digit-script forms as explicitly defined in native_check.exact_form; "
                    "no ordinary-word one-letter tolerance or Arabic-letter fold in acceptance; source-image decisions for remaining items",
            "verificationEffect": "body comparison only; paragraph, structural, temporal and whole-code checks remain separate",
        }
    return list(by_number.values())


def main() -> int:
    inputs = WORK / "amended-input.json"
    audit = pipeline.read_json(WORK / "amended-exact-audit.json")
    if audit["inputSha256"] != pipeline.hash_file(inputs):
        raise ValueError("exact audit belongs to different input")
    review = pipeline.read_json(WORK / "image-decisions-amended-exact.json")
    result = apply(pipeline.read_json(inputs), audit, review)
    pipeline.write_json(WORK / "amended-corrected.json", result)
    print(len(result), "amended body comparisons settled;", sum(d["decision"] == "print-differs" for d in review["decisions"]),
          "guarded corrections; no whole-code verification inferred")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
