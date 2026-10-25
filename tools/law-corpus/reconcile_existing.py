#!/usr/bin/env python3
"""Account for every legacy article without changing any legacy text or flags."""
from __future__ import annotations

from collections import Counter
from datetime import date
import difflib
import hashlib
import json
from pathlib import Path
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
CORPUS = ROOT / "data/law/corpus"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(text):
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def comparison(text):
    return " ".join(unicodedata.normalize("NFC", text).split())


def main():
    imported = {}
    for path in sorted(CORPUS.glob("*/articles.json")):
        content = read(path)
        for article in content["articles"]:
            imported[(content["codeId"], article["canonicalNumber"])] = article
    rows = []
    for path in sorted((ROOT / "data/law/codes").glob("*.json")):
        legacy = read(path)
        for old in legacy["articles"]:
            article = imported.get((legacy["id"], old["number"]))
            row = {"codeId": legacy["id"], "number": old["number"], "legacyFile": path.relative_to(ROOT).as_posix(),
                   "legacyTextSha256": digest(old["text"]), "legacyConsolidatedAsOf": legacy.get("consolidatedAsOf"),
                   "category": "not-imported", "reason": "Outside the delivered priority subset; text/version comparison is not established.",
                   "versionsCompared": []}
            if article and article["versions"]:
                for version in article["versions"]:
                    exact = old["text"] == version["text"]
                    normalized = comparison(old["text"]) == comparison(version["text"])
                    row["versionsCompared"].append({"versionId": version["versionId"], "verificationState": version["verification"]["state"],
                        "temporalStatus": version["temporalStatus"], "corpusTextSha256": digest(version["text"]),
                        "exactEquality": exact, "conservativeComparisonEqual": normalized,
                        "diff": list(difflib.unified_diff(old["text"].splitlines(), version["text"].splitlines(),
                                                         fromfile="legacy", tofile=version["versionId"], lineterm=""))})
                equal = any(v["conservativeComparisonEqual"] for v in row["versionsCompared"])
                row["category"] = "same-text" if equal else "different-text"
                row["reason"] = ("Equality under named NFC/whitespace comparison; raw equality is recorded separately."
                                 if equal else "Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.")
            rows.append(row)
    counts = dict(Counter(r["category"] for r in rows))
    record = {"generatedOn": date.today().isoformat(), "normalizations": ["Unicode NFC", "collapse whitespace for comparison only"],
              "legacyArticles": len(rows), "counts": counts,
              "limitations": ["Different wording does not itself prove a different legal version.",
                              "No unavailable corpus article is guessed, and no legacy verified flag is changed.",
                              "Corpus transcription verification states remain authoritative limitations on any comparison."], "articles": rows}
    target = CORPUS / "research/reconciliation.json"
    target.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    doc = ROOT / "docs/LAW-CORPUS.md"
    text = doc.read_text(encoding="utf-8")
    begin, end = "<!-- RECONCILIATION:BEGIN -->", "<!-- RECONCILIATION:END -->"
    if text.count(begin) != 1 or text.count(end) != 1:
        raise ValueError("Exactly one reconciliation marker pair is required; refusing to overwrite the document.")
    lines = [f"All **{len(rows)}** legacy records are accounted for: " + "; ".join(f"{k}: {v}" for k, v in sorted(counts.items())) + ".",
             "", "Differences and raw hashes: [machine-readable reconciliation](../data/law/corpus/research/reconciliation.json).",
             "", "| Legacy code | Article | Result |", "|---|---|---|"]
    lines += [f"| {r['codeId']} | {r['number']} | {r['category']} |" for r in rows]
    for row in rows:
        if row["category"] != "different-text":
            continue
        lines += ["", f"### Text difference: {row['codeId']} {row['number']}", "", row["reason"]]
        for version in row["versionsCompared"]:
            lines += ["", f"Compared with `{version['versionId']}`; corpus transcription state `{version['verificationState']}`.", "", "```diff", *version["diff"], "```"]
    before, remainder = text.split(begin)
    _, after = remainder.split(end)
    doc.write_text(before + begin + "\n" + "\n".join(lines) + "\n" + end + after, encoding="utf-8")
    print(json.dumps({"legacyArticles": len(rows), "counts": counts}))


if __name__ == "__main__":
    main()
