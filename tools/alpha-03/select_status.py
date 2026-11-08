"""Apply the predeclared grouped status/calibration gates; never inspect audit labels."""
import argparse
import copy
import json
from pathlib import Path
from summarize_fitting_references import quality, read


def missing(metrics):
    c = metrics["counts"]
    return sum(c.get(k, 0) for k in ("dateMissing", "amountMissing", "requestAmountMissing"))


def count(metrics, name):
    return metrics["counts"].get(name, 0)


def reductions(candidate, reference, strict=True):
    compare = (lambda a, b: a < b) if strict else (lambda a, b: a <= b)
    return (compare(count(candidate, "criticalCases"), count(reference, "criticalCases"))
            and compare(count(candidate, "nonAffirmedErrors"), count(reference, "nonAffirmedErrors"))
            and count(candidate, "falseAffirmations") <= count(reference, "falseAffirmations"))


def safe_omissions(candidate, reference):
    return missing(candidate) <= missing(reference) and count(candidate, "decisiveOmissions") <= count(reference, "decisiveOmissions")


def metrics(rows, cases, catalog):
    result = quality(rows, cases, catalog)
    result["projectedSeconds"] = result.pop("seconds")
    return result


def summary(rows, cases, catalog):
    return dict(overall=metrics(rows, cases, catalog),
                byKind={k: metrics([r for r in rows if r["Kind"] == k], cases, catalog) for k in ("polarity", "binding")},
                byFold={str(f): metrics([r for r in rows if r["Fold"] == f], cases, catalog) for f in (1, 2, 3)})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("collection_root", type=Path)
    parser.add_argument("first_reference_root", type=Path)
    parser.add_argument("qualified_g4_cost_root", type=Path)
    parser.add_argument("evaluation_file", type=Path)
    parser.add_argument("output_file", type=Path)
    args = parser.parse_args()
    workspace = Path(__file__).resolve().parents[2]
    output = args.output_file.resolve()
    if output.exists() or output == workspace or workspace in output.parents:
        raise ValueError("Use a fresh external output file")
    members = {m["name"]: m for m in read(workspace / "tests/alpha-03-cases/split-manifest.json")["members"] if m["split"] == "fitting"}
    cases = {c["name"]: c for c in read(workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in members}
    catalog = {c["claim"]: c for c in read(workspace / "data/law/claim-elements.json")["claims"]}
    evaluated = read(args.evaluation_file)
    assert len(evaluated) == 16
    models, selected = {}, {}
    for model in ("gemma3:4b", "gemma4:e4b"):
        slug = model.replace(":", "-")
        baseline_rows = read(args.first_reference_root / ("reference-" + slug) / "case-results.json")
        baseline_rows = copy.deepcopy(baseline_rows)
        assert [r["Name"] for r in baseline_rows] == list(members)
        cost_root = args.first_reference_root if model == "gemma3:4b" else args.qualified_g4_cost_root
        baseline_cost = {r["Name"]: r["Seconds"] for r in read(cost_root / ("reference-" + slug) / "case-results.json")}
        for row in baseline_rows:
            row["Seconds"] = baseline_cost[row["Name"]]
        baseline = summary(baseline_rows, cases, catalog)
        assert 96 - count(baseline["overall"], "criticalCases") == (36 if model == "gemma3:4b" else 69)
        collection = read(args.collection_root / ("fitting-" + slug) / "case-results.json")
        completion = read(args.collection_root / ("fitting-" + slug) / "completion.json")
        assert completion["Complete"] and len(collection) == 96
        status_cost = {r["Name"]: r for r in collection}
        overhead = completion["BatchSeconds"] - sum(r["Seconds"] for r in collection)
        assert overhead >= 0
        arms = {}
        for candidate in (c for c in evaluated if c["Model"] == model):
            rows = copy.deepcopy(candidate["Rows"])
            assert [r["Name"] for r in rows] == list(members)
            for row in rows:
                collected = status_cost[row["Name"]]
                seconds = collected["Chains"][0]["WallSeconds"] if candidate["Order"] == "fixed" else collected["Seconds"]
                row["Seconds"] = baseline_cost[row["Name"]] + seconds + overhead / 96
            arms[candidate["Arm"]] = dict(order=candidate["Order"], regularization=candidate["Regularization"], **summary(rows, cases, catalog))
        assert len(arms) == 8
        for arm in arms.values():
            overall = arm["overall"]
            reference = arms[arm["order"] + ":uncalibrated"]
            calibration_folds = [f for f in ("1", "2", "3") if reductions(arm["byFold"][f], reference["byFold"][f])]
            calibration_pass = arm["regularization"] is None or (
                reductions(overall, reference["overall"]) and safe_omissions(overall, reference["overall"])
                and len(calibration_folds) >= 2)
            no_regression_folds = [f for f in ("1", "2", "3")
                                   if count(arm["byFold"][f], "criticalCases") <= count(baseline["byFold"][f], "criticalCases")]
            score_pass = (reductions(overall, baseline["overall"]) and safe_omissions(overall, baseline["overall"])
                          and len(no_regression_folds) >= 2)
            cost_pass = overall["projectedSeconds"]["mean"] <= 90
            arm["gates"] = dict(calibration=calibration_pass, calibrationStrictFolds=calibration_folds,
                score=score_pass, criticalNoRegressionFolds=no_regression_folds, projectedCost=cost_pass,
                survives=calibration_pass and score_pass and cost_pass)
        passing = [(name, a) for name, a in arms.items() if a["gates"]["survives"]]
        if passing:
            def rank(item):
                name, a = item
                return (count(a["overall"], "criticalCases"), count(a["overall"], "falseAffirmations"),
                        count(a["overall"], "nonAffirmedErrors"), missing(a["overall"]),
                        a["overall"]["projectedSeconds"]["mean"], a["regularization"] is not None,
                        a["order"] != "fixed", -(a["regularization"] or 0))
            name, arm = min(passing, key=rank)
            selected[model] = dict(arm=name, order=arm["order"], lambda_=arm["regularization"])
            selected[model]["lambda"] = selected[model].pop("lambda_")
        else:
            selected[model] = None
        models[model] = dict(baseline=baseline, arms=arms, measuredCollectionSeconds=completion["BatchSeconds"],
                            collectionReloadSettlingSeconds=overhead, accuracyReference="first complete record only",
                            costQualification="component projection only; one scored collection load/wait bill, no duplicate reference reload bill")
    result = dict(selected=selected, models=models, allScorePathsFailed=all(v is None for v in selected.values()),
                  corpusHash="9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5",
                  auditUsed=False, validationUsed=False, primaryModel=None,
                  primaryRule="Choose only after surviving fitting/binding work; record before locked audit")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: result[k] for k in ("selected", "allScorePathsFailed", "primaryModel")}, indent=2))


if __name__ == "__main__":
    main()
