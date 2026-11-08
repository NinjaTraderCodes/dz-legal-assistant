"""Owner-authorised diagnosis only: fixed forward order, fixed lambda .1, no selection/refit/inference."""
import argparse
import hashlib
import json
from pathlib import Path
from fit_status import choose, fit, matrix, read

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("collection_root", type=Path)
parser.add_argument("output_root", type=Path)
args = parser.parse_args()
workspace = Path(__file__).resolve().parents[2]
output = args.output_root.resolve()
if output.exists() or output == workspace or workspace in output.parents:
    raise ValueError("Use a fresh external diagnostic directory")
case_path = workspace / "tests/alpha-03-cases/cases.json"
manifest_path = workspace / "tests/alpha-03-cases/split-manifest.json"
assert hashlib.sha256(case_path.read_bytes()).hexdigest() == "415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67"
assert hashlib.sha256(manifest_path.read_bytes()).hexdigest() == "1a9b132b481c7e3719f5f66b8f955c5aee56a2c16fc0e76b945520ef033e762f"
members = {m["name"]: m for m in read(manifest_path)["members"] if m["split"] == "fitting"}
cases = {c["name"]: c for c in read(case_path)["cases"] if c["name"] in members}
audit = read(args.collection_root / "fitting-audit-gemma3-4b.json")["gemma3:4b"]
assert audit["cases"] == 96 and audit["fourFiniteScoreSets"] == 744 and audit["extensionReuse"] == 552
rows = read(args.collection_root / "fitting-gemma3-4b/case-results.json")
assert [r["Name"] for r in rows] == list(members)
samples = matrix(rows, cases, members, "fixed")
assert len(samples) == 372
candidates, fits = [], []
for regularization in (None, .1):
    choices = {name: {} for name in members}
    arm = "fixed:uncalibrated" if regularization is None else "fixed:lambda=0.1"
    for held_out in (1, 2, 3):
        train_ids = [n for n in members if members[n]["fold"] != held_out]
        held_ids = [n for n in members if members[n]["fold"] == held_out]
        assert len(train_ids) == 64 and len(held_ids) == 32 and not set(train_ids) & set(held_ids)
        training = [s for s in samples if s["fold"] != held_out]
        fitted = fit(training, regularization) if regularization is not None else dict(offsets=[0.0] * 4)
        fits.append(dict(model="gemma3:4b", arm=arm, heldOutFold=held_out, trainingIds=train_ids, heldOutIds=held_ids, **fitted))
        for sample in samples:
            if sample["fold"] == held_out:
                choices[sample["name"]][sample["event"]] = choose(sample["scores"], fitted["offsets"])
    assert sum(len(c) for c in choices.values()) == 372
    candidates.append(dict(model="gemma3:4b", arm=arm, order="fixed", regularization=regularization, choices=choices))
output.mkdir(parents=True)
payload = dict(candidates=candidates, foldFits=fits,
    corpusHash="9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5",
    purpose="diagnostic only; owner froze unchanged Alpha-01 gemma4 thinking-off independently; no selection or full-fit refit",
    fixedBeforeResults=dict(model="gemma3:4b", order="forward", correctionLambda=.1), auditUsed=False, validationUsed=False)
(output / "status-predictions.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
print("Saved two fixed forward-order diagnostic arms and their grouped out-of-fold parameters; no inference or selection")
