"""Compare two complete gemma4 reference records; preserve first-run accuracy."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def error_set(row):
    return sorted((e["Type"], e["Detail"], e["Critical"]) for e in row["Errors"])


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("first_run_root", type=Path)
parser.add_argument("rerun_root", type=Path)
args = parser.parse_args()
assert args.first_run_root.resolve() != args.rerun_root.resolve(), "Preserve the first-run directory read-only"
first_folder = args.first_run_root / "reference-gemma4-e4b"
second_folder = args.rerun_root / "reference-gemma4-e4b"
first, second = read(first_folder / "case-results.json"), read(second_folder / "case-results.json")
assert len(first) == len(second) == 96
assert [r["Name"] for r in first] == [r["Name"] for r in second]
assert read(first_folder / "manifest.json")["Fingerprints"] == read(second_folder / "manifest.json")["Fingerprints"]
assert all(read(folder / "completion.json")["Complete"] for folder in (first_folder, second_folder))
assert all(r["Failure"] is None and r["Timings"] is not None for r in first + second)
rows = []
for a, b in zip(first, second):
    rows.append(dict(name=a["Name"], kind=a["Kind"], fold=a["Fold"],
                     answerExact=a["Answer"] == b["Answer"],
                     verdictSame=a["HasCritical"] == b["HasCritical"],
                     errorSetSame=error_set(a) == error_set(b),
                     parsedFactsExact=canonical(a["Facts"]) == canonical(b["Facts"]),
                     firstCritical=a["HasCritical"], rerunCritical=b["HasCritical"]))


def counts(items):
    return dict(cases=len(items), answerExact=sum(r["answerExact"] for r in items),
                verdictSame=sum(r["verdictSame"] for r in items), errorSetSame=sum(r["errorSetSame"] for r in items),
                parsedFactsExact=sum(r["parsedFactsExact"] for r in items),
                firstCorrect=sum(not r["firstCritical"] for r in items),
                rerunCorrectDiagnosticOnly=sum(not r["rerunCritical"] for r in items),
                passToFail=sum(not r["firstCritical"] and r["rerunCritical"] for r in items),
                failToPass=sum(r["firstCritical"] and not r["rerunCritical"] for r in items))


output = dict(rule="Owner instruction: first-run accuracy remains the semantic reference; rerun provides timing/memory and repeatability only. No replacement or selection of better answers.",
              answerMatch="Exact decoded string including whitespace and line endings; no normalization",
              verdictMatch="Same whole-case critical pass/fail; complete error-set and parsed-facts matches separately",
              firstSha256=hashlib.sha256((first_folder / "case-results.json").read_bytes()).hexdigest(),
              rerunSha256=hashlib.sha256((second_folder / "case-results.json").read_bytes()).hexdigest(),
              overall=counts(rows), byKind={kind: counts([r for r in rows if r["kind"] == kind]) for kind in ("polarity", "binding")},
              byFold={str(fold): counts([r for r in rows if r["fold"] == fold]) for fold in (1, 2, 3)}, rows=rows)
(args.rerun_root / "repeatability.json").write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
print(json.dumps(output["overall"], indent=2))
