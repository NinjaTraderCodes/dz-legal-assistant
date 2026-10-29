#!/usr/bin/env python3
"""Replay source-bound Civil word review into a separate corrected research file, retaining raw native evidence.

This first review batch permits only explicitly inspected letter-identical spacing repairs. Digit joins, removal
of a separate negation, letter substitutions, ambiguous replacements and stale/missing proof fail closed. It does
not certify operative law, paragraphs, headings, amendments or dates, and never changes the typed Civil package.
"""
from __future__ import annotations

import argparse

import native_check as nc
import pipeline
import civil_ministry_audit as civil


def repair(article: dict, corrections: list[dict]) -> dict:
    text, paragraphs = article["text"], list(article["paragraphs"])
    if text != "\n".join(paragraphs):
        raise ValueError("Candidate paragraphs do not reconstruct the native body")
    for correction in corrections:
        old, new, count = correction["old"], correction["new"], correction["expectedOccurrences"]
        if (not old or not new or count != 1 or old == new or old.replace(" ", "") != new
                or any(ch.isdigit() for ch in old + new) or any(nc.is_negation(t) for t in nc.tokens(old))):
            raise ValueError("Correction is not an allowed source-read spacing repair")
        if text.count(old) != count or sum(p.count(old) for p in paragraphs) != count:
            raise ValueError("Spacing repair is absent, ambiguous or crosses paragraph boundaries")
        text = text.replace(old, new)
        paragraphs = [p.replace(old, new) for p in paragraphs]
    if text != "\n".join(paragraphs):
        raise ValueError("Correction changed paragraph reconstruction")
    return {"candidateKey": article["candidateKey"], "number": article["number"], "numberScope": article["numberScope"],
            "rawCandidateTextSha256": pipeline.hash_text(article["text"]), "text": text, "paragraphs": paragraphs,
            "textSha256": pipeline.hash_text(text), "corrections": corrections,
            "qaState": "word-corrected research body; editorial annotations/callouts retained"}


def apply(native: dict, audit: dict, review: dict, root=pipeline.ROOT) -> dict:
    if native["source"]["sha256"] != review["sourceSha256"]:
        raise ValueError("Civil word reading belongs to another source")
    start, end = review["sourceOrderStart"], review["sourceOrderEnd"]
    if not 0 <= start < end <= len(native["articles"]):
        raise ValueError("Invalid Civil word-review source-order range")
    articles = {a["candidateKey"]: a for a in native["articles"][start:end]}
    expected = {f"{r['number']}#{i}": d for r in audit["results"] if r["number"] in articles
                for i, d in enumerate(d for d in r["disagreements"] if not d["punctuationOnly"])}
    seen, repairs = set(), {}
    for decision in review["decisions"]:
        ident, number = decision["itemId"], decision["candidateKey"]
        if ident in seen or ident not in expected or number not in articles or not ident.startswith(number + "#"):
            raise ValueError("Duplicate or out-of-scope Civil word decision")
        seen.add(ident)
        flag = expected[ident]
        if (any(decision[k] != flag[k] for k in ("kind", "at", "canonical"))
                or decision["ocrExtra"] != flag.get("ocr")
                or decision["inputTextSha256"] != pipeline.hash_text(articles[number]["text"])):
            raise ValueError("Civil word decision no longer matches the body or discrepancy")
        if not decision["images"] or len(decision["images"]) != len(decision["imageSha256s"]) or not decision["note"].strip():
            raise ValueError("Civil word decision lacks actual reading proof")
        for image, sha in zip(decision["images"], decision["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Civil word proof changed or escaped the repository")
        verdict, correction = decision["verdict"], decision["correction"]
        if verdict == "native-spacing-correction":
            if flag["kind"] != "unconfirmed" or correction is None or correction["old"] != flag["canonical"]:
                raise ValueError("Spacing repair does not address the actual source-read flag")
            repairs.setdefault(number, []).append(correction)
        elif verdict in {"source-reads-native", "outside-article-next-label", "outside-article-heading-or-page-label"}:
            if correction is not None or (verdict.startswith("outside-") and flag["kind"] != "extra-in-all-streams"):
                raise ValueError("Non-repair verdict cannot change text or silently drop an unconfirmed word")
        else:
            raise ValueError("Unknown Civil word-review verdict")
    if seen != set(expected):
        raise ValueError("Civil word-review batch has missing source decisions")
    corrected = [repair(articles[number], changes) for number, changes in repairs.items()]
    return {"method": __doc__.strip(), "sourceOrderStart": start, "sourceOrderEnd": end,
            "wordFlagsActuallyRead": len(seen), "correctedCandidates": corrected,
            "correctionsApplied": sum(len(a["corrections"]) for a in corrected),
            "verificationEffect": "Targeted word review only; raw native data and typed corpus unchanged"}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--decisions", type=pipeline.Path, default=civil.WORK / "image-decisions-ministry-words.json")
    parser.add_argument("--out", type=pipeline.Path, default=civil.WORK / "ministry-word-corrected.json")
    args = parser.parse_args()
    native_path = civil.WORK / "native-candidates.json"
    audit_path = civil.WORK / "ministry-page-bounded-exact-audit.json"
    review_path = args.decisions
    review = pipeline.read_json(review_path)
    if review["nativeSha256"] != pipeline.hash_file(native_path) or review["auditSha256"] != pipeline.hash_file(audit_path):
        raise ValueError("Civil native input or exact audit changed since image review")
    output = apply(pipeline.read_json(native_path), pipeline.read_json(audit_path), review)
    output |= {"nativeSha256": pipeline.hash_file(native_path), "auditSha256": pipeline.hash_file(audit_path),
               "reviewSha256": pipeline.hash_file(review_path)}
    pipeline.write_json(args.out, output)
    print(output["wordFlagsActuallyRead"], "source decisions replayed;", output["correctionsApplied"],
          "guarded spacing repairs in", len(output["correctedCandidates"]), "derived research bodies; typed corpus unchanged")


if __name__ == "__main__":
    main()
