#!/usr/bin/env python3
"""Exact-word overlay on the Family comparison; preserve the older, explicitly tolerant comparison as evidence.

Alignment may fold letters, but acceptance requires exact_form for every word, not only critical tokens. One-letter
substitutions and Arabic-letter folds never pass. Identical-letter OCR word joins are allowed; digit joins are not.
Cached targeted readings can confirm their original disputed token range only. Unconfirmed punctuation is retained
separately under the approved rule. This script runs no OCR and grants no corpus verification state.
"""
from __future__ import annotations

import check_native
import native_check as nc
import pipeline
import reread

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def audit(articles: list[dict], check: dict) -> dict:
    results = {r["number"]: r for r in check["results"]}
    out = []
    for article in articles:
        result = results[article["number"]]
        tokens = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        skip = len(nc.tokens(article["label"] + " :"))
        statuses = [nc.token_statuses(tokens, nc.tokens(text), strict=True)[0] for text in result["ocr"].values()]
        accepted = [any(s[i] in {"exact", "merge"} for s in statuses) for i in range(len(tokens))]
        for d in result["disagreements"]:
            if d["kind"] != "unconfirmed":
                continue
            first, last = d["at"], d["at"] + len(nc.tokens(d["canonical"])) - 1
            lo, hi = max(0, first - reread.WINDOW), min(len(tokens), last + reread.WINDOW + 1)
            for reading in d.get("reread", {}).get("readings", []):
                states, _ = nc.token_statuses(tokens[lo:hi], nc.tokens(reading["ocr"]), strict=True)
                for i in range(first, last + 1):
                    accepted[i] |= states[i - lo] in {"exact", "merge"}
        groups = []
        i = skip
        while i < len(tokens):
            if accepted[i]:
                i += 1
                continue
            end = i + 1
            while end < len(tokens) and not accepted[end]:
                end += 1
            text = " ".join(tokens[i:end])
            groups.append({"kind": "unconfirmed", "canonical": text, "at": i,
                           "punctuationOnly": not any(c.isalpha() or c.isdigit() for c in text)})
            i = end
        # Extra words still need review; this overlay cannot settle additions just by confirming native tokens.
        groups += [d | {"punctuationOnly": not any(c.isalpha() or c.isdigit() for c in " ".join(d.get("ocr") or []))}
                   for d in result["disagreements"] if d["kind"] == "extra-in-all-streams"]
        out.append(result | {"agrees": not groups, "agreesAfterReread": not groups, "disagreements": groups})
    return {"source": check["source"], "strict": True, "rule": __doc__.strip(),
            "results": out, "articles": len(out), "agreed": sum(r["agrees"] for r in out),
            "wordReviewGroups": sum(not d["punctuationOnly"] for r in out for d in r["disagreements"]),
            "punctuationReviewGroups": sum(d["punctuationOnly"] for r in out for d in r["disagreements"]),
            "verificationEffect": "none"}


def main() -> int:
    for name in ("base-unchanged", "amended"):
        inputs = WORK / f"{name}-input.json"
        check_path = WORK / f"{name}-reread.json"
        check = pipeline.read_json(check_path)
        if check["inputSha256"] != pipeline.hash_file(inputs):
            raise ValueError(f"{name}: reread belongs to different prepared input")
        result = audit(pipeline.read_json(inputs), check)
        result |= {"inputSha256": pipeline.hash_file(inputs), "rereadSha256": pipeline.hash_file(check_path)}
        pipeline.write_json(WORK / f"{name}-exact-audit.json", result)
        print(name, result["agreed"], "/", result["articles"], "exact agreements;", result["wordReviewGroups"],
              "word/digit/extra groups;", result["punctuationReviewGroups"], "punctuation groups")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
