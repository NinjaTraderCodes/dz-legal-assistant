"""The predeclared global-offset grid, fitted on grouped fitting folds only."""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path

LABELS = ("affirmed", "denied", "disputed", "absent")
LAMBDAS = (.01, .1, 1.0)


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def project(values):
    low, high = min(values) - 3, max(values) + 3
    for _ in range(100):
        shift = (low + high) / 2
        total = sum(max(-3, min(3, x - shift)) for x in values)
        if total > 0:
            low = shift
        else:
            high = shift
    shift = (low + high) / 2
    return [max(-3, min(3, x - shift)) for x in values]


def choose(scores, offsets):
    assert len(scores) == len(offsets) == 4 and all(math.isfinite(x) for x in scores + offsets)
    return max(range(4), key=lambda i: scores[i] + offsets[i]) + 1


def objective_gradient(samples, offsets, regularization):
    counts = collections.Counter(s["label"] for s in samples)
    if set(counts) != set(range(4)):
        raise ValueError("Every training fold must contain all four classes; no fallback")
    gradient = [regularization * x for x in offsets]
    loss = regularization * sum(x * x for x in offsets) / 2
    for sample in samples:
        values = [x + b for x, b in zip(sample["scores"], offsets)]
        peak = max(values)
        exp = [math.exp(x - peak) for x in values]
        total = sum(exp)
        weight = 1 / (4 * counts[sample["label"]])
        loss += weight * (math.log(total) + peak - values[sample["label"]])
        for i in range(4):
            gradient[i] += weight * (exp[i] / total - (i == sample["label"]))
    return loss, gradient


def fit(samples, regularization):
    offsets = [0.0] * 4
    converged = False
    change = None
    for iteration in range(1, 1001):
        _, gradient = objective_gradient(samples, offsets, regularization)
        updated = project([b - g / (.5 + regularization) for b, g in zip(offsets, gradient)])
        change = max(abs(a - b) for a, b in zip(updated, offsets))
        offsets = updated
        if change < 1e-10:
            converged = True
            break
    assert abs(sum(offsets)) < 1e-9 and all(-3 <= b <= 3 and math.isfinite(b) for b in offsets)
    return dict(offsets=offsets, iterations=iteration, converged=converged, lastChange=change,
                objective=objective_gradient(samples, offsets, regularization)[0],
                classCounts=dict(collections.Counter(LABELS[s["label"]] for s in samples)))


def matrix(rows, cases, members, order):
    samples = []
    for row in rows:
        member = members[row["Name"]]
        assert row["Fold"] == member["fold"] and row["Kind"] == member["familyKind"]
        forward = {q["Event"]: q["Scores"] for q in row["Chains"][0]["Questions"]}
        reverse = {q["Event"]: q["Scores"] for q in row["Chains"][1]["Questions"]}
        gold = cases[row["Name"]]["annotation"]["events"]
        assert set(forward) == set(reverse) == {g["kind"] for g in gold}
        for event in gold:
            kind = event["kind"]
            scores = forward[kind] if order == "fixed" else [(a + b) / 2 for a, b in zip(forward[kind], reverse[kind])]
            assert len(scores) == 4 and all(math.isfinite(s) for s in scores)
            samples.append(dict(name=row["Name"], event=kind, fold=member["fold"], scores=scores,
                                label=LABELS.index(event["polarity"])))
    return samples


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("collection_root", type=Path)
    parser.add_argument("output_root", type=Path)
    parser.add_argument("--refit", type=Path, help="Selected arms only, after gates; emits final all-fitting offsets")
    args = parser.parse_args()
    workspace = Path(__file__).resolve().parents[2]
    output = args.output_root.resolve()
    if output == workspace or workspace in output.parents:
        raise ValueError("Use an external output root")
    output.mkdir(parents=True, exist_ok=True)
    case_path = workspace / "tests/alpha-03-cases/cases.json"
    manifest_path = workspace / "tests/alpha-03-cases/split-manifest.json"
    assert hashlib.sha256(case_path.read_bytes()).hexdigest() == "415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67"
    assert hashlib.sha256(manifest_path.read_bytes()).hexdigest() == "1a9b132b481c7e3719f5f66b8f955c5aee56a2c16fc0e76b945520ef033e762f"
    members = {m["name"]: m for m in read(manifest_path)["members"] if m["split"] == "fitting"}
    cases = {c["name"]: c for c in read(case_path)["cases"] if c["name"] in members}
    audit = read(args.collection_root / "fitting-audit.json")
    assert set(audit) == {"gemma3:4b", "gemma4:e4b"}, "Both model collections must pass; a partial/single-model audit is insufficient"
    assert all(m["cases"] == 96 and m["fourFiniteScoreSets"] == 744 for m in audit.values())
    candidates, fits, final = [], [], {}
    selection = read(args.refit) if args.refit else None
    if selection is not None:
        assert selection["corpusHash"] == "9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5"
    selected = selection["selected"] if selection is not None else None
    for model in ("gemma3:4b", "gemma4:e4b"):
        rows = read(args.collection_root / ("fitting-" + model.replace(":", "-")) / "case-results.json")
        assert [r["Name"] for r in rows] == list(members)
        for order in ("fixed", "averaged"):
            samples = matrix(rows, cases, members, order)
            assert len(samples) == 372
            if selected is not None:
                pick = selected.get(model)
                if pick is None or pick["order"] != order:
                    continue
                lam = pick["lambda"]
                assert lam in (None,) + LAMBDAS
                final[model] = dict(order=order, **(fit(samples, lam) if lam is not None else dict(offsets=[0.0] * 4)),
                                    regularization=lam, trainingIds=list(members))
                continue
            for regularization in (None,) + LAMBDAS:
                choices = {name: {} for name in members}
                arm = order + (":uncalibrated" if regularization is None else ":lambda=" + str(regularization))
                for held_out in (1, 2, 3):
                    training = [s for s in samples if s["fold"] != held_out]
                    held = [s for s in samples if s["fold"] == held_out]
                    training_ids = [name for name in members if members[name]["fold"] != held_out]
                    held_ids = [name for name in members if members[name]["fold"] == held_out]
                    assert not set(training_ids) & set(held_ids) and len(training_ids) == 64 and len(held_ids) == 32
                    fitted = fit(training, regularization) if regularization is not None else dict(offsets=[0.0] * 4)
                    fits.append(dict(model=model, arm=arm, heldOutFold=held_out, lambda_=regularization,
                                     trainingIds=training_ids, heldOutIds=held_ids, **fitted))
                    for sample in held:
                        choices[sample["name"]][sample["event"]] = choose(sample["scores"], fitted["offsets"])
                assert sum(len(c) for c in choices.values()) == 372
                candidates.append(dict(model=model, arm=arm, order=order, regularization=regularization, choices=choices))
    target = output / ("selected-offsets.json" if selected is not None else "status-predictions.json")
    if target.exists():
        raise ValueError("Refusing to overwrite previous parameters/predictions")
    payload = dict(selected=final) if selected is not None else dict(candidates=candidates, foldFits=fits)
    payload["corpusHash"] = "9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5"
    target.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print("Wrote", target, "; no audit/validation labels or inference used")


if __name__ == "__main__":
    main()
