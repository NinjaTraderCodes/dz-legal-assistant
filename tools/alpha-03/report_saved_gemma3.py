"""Summarise the fixed gemma3 diagnosis; no arm selection, refit or inference."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from summarize_fitting_references import quality, read

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("diagnostic_root", type=Path)
parser.add_argument("first_reference_root", type=Path)
parser.add_argument("collection_root", type=Path)
args = parser.parse_args()
workspace = Path(__file__).resolve().parents[2]
members = {m["name"]: m for m in read(workspace / "tests/alpha-03-cases/split-manifest.json")["members"] if m["split"] == "fitting"}
cases = {c["name"]: c for c in read(workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in members}
catalog = {c["claim"]: c for c in read(workspace / "data/law/claim-elements.json")["claims"]}
baseline_path = args.first_reference_root / "reference-gemma3-4b/case-results.json"
assert hashlib.sha256(baseline_path.read_bytes()).hexdigest() == "0dc3123e10bcedfc77566f1c71bec35925d4e446dc327ebf9b51d221a09ff91b"
baseline = read(baseline_path)
evaluated = read(args.diagnostic_root / "status-evaluation.json")
assert len(evaluated) == 2 and all(c["Model"] == "gemma3:4b" and c["Order"] == "fixed" for c in evaluated)
assert {c["Regularization"] for c in evaluated} == {None, .1}
collected = read(args.collection_root / "fitting-gemma3-4b/case-results.json")
completion = read(args.collection_root / "fitting-gemma3-4b/completion.json")
assert completion["Complete"]
load_bill = (completion["BatchSeconds"] - sum(r["Seconds"] for r in collected)) / 96
forward = {r["Name"]: r["Chains"][0]["WallSeconds"] for r in collected}
base_time = {r["Name"]: r["Seconds"] for r in baseline}


def describe(rows, projected):
    q = quality(rows, cases, catalog)
    q["projectedCaseSeconds" if projected else "recordedCaseSeconds"] = q.pop("seconds")
    q["criticalFreeCases"] = q["counts"]["cases"] - q["counts"]["criticalCases"]
    q["byStatusClass"] = {}
    for label in ("affirmed", "denied", "disputed", "absent"):
        truth = sum(q["confusion"].get(label, {}).values())
        predicted = sum(row.get(label, 0) for row in q["confusion"].values())
        correct = q["confusion"].get(label, {}).get(label, 0)
        q["byStatusClass"][label] = dict(gold=truth, predicted=predicted, correct=correct,
            recall=correct / truth if truth else None, precision=correct / predicted if predicted else None)
    return q


groups = [("Alpha-01", baseline, False)]
for candidate in evaluated:
    rows = copy.deepcopy(candidate["Rows"])
    for row in rows:
        row["Seconds"] = base_time[row["Name"]] + forward[row["Name"]] + load_bill
    groups.append((candidate["Arm"], rows, True))
result = {}
for name, rows, projected in groups:
    assert [r["Name"] for r in rows] == list(members)
    result[name] = dict(overall=describe(rows, projected),
        byKind={k: describe([r for r in rows if r["Kind"] == k], projected) for k in ("polarity", "binding")},
        byFold={str(f): describe([r for r in rows if r["Fold"] == f], projected) for f in (1, 2, 3)})
payload = dict(results=result, model="gemma3:4b", order="forward", fixedCorrectionLambda=.1,
    selectedArm=None, offsetsRefit=False, inferenceCalls=0, auditUsed=False, validationUsed=False,
    frozenCandidate="owner selected unchanged Alpha-01 gemma4:e4b think=false before diagnosis")
target = args.diagnostic_root / "diagnostic-summary.json"
if target.exists():
    raise ValueError("Refusing to overwrite a previous diagnostic")
target.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in args.diagnostic_root.iterdir() if p.is_file() and p.name != "artifact-hashes.json"}
(args.diagnostic_root / "artifact-hashes.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
for name, group in result.items():
    print(name, json.dumps(dict(criticalFree=group["overall"]["criticalFreeCases"], counts=group["overall"]["counts"],
        byStatusClass=group["overall"]["byStatusClass"],
        byKind={k: dict(criticalFree=v["criticalFreeCases"], counts=v["counts"], classes=v["byStatusClass"]) for k, v in group["byKind"].items()},
        foldCritical=[group["byFold"][str(f)]["counts"]["criticalCases"] for f in (1, 2, 3)])))
