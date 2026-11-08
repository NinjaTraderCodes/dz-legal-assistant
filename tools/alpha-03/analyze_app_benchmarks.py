"""Audit and report only this task's default grounds/drafting benchmark outputs; no inference."""
import argparse
import hashlib
import json
import math
import re
from datetime import datetime
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("run_root", type=Path)
parser.add_argument("--write-docs", action="store_true")
parser.add_argument("--report-suffix", default="")
args = parser.parse_args()
if args.report_suffix and not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", args.report_suffix):
    raise ValueError("Use a lowercase alphanumeric/hyphen report suffix")
repo = Path(__file__).resolve().parents[2]
read = lambda p: json.loads(p.read_text(encoding="utf-8-sig"))
best = "ded574b5-45a0-4f42-8737-46345c09c238"
models = ("gemma3:4b", "gemma4:e4b")
grounds = [c for c in read(repo / "tests/grounds-cases/cases.json")["cases"] if c.get("partition", "development") in ("development", "robustness")]
drafts = [(p.stem, read(p).get("partition", "development")) for p in sorted((repo / "tests/drafting-cases").glob("*.json"))]
assert all(part in ("development", "robustness") for _, part in drafts)
expected = {"grounds": [c["name"] for c in grounds], "draft": [name for name, _ in drafts]}


def resources(folder, completion):
    start = datetime.fromisoformat(completion["StartedUtc"])
    end = datetime.fromisoformat(completion["FinishedUtc"])
    samples, incomplete = [], 0
    for line in (folder / "resources.jsonl").read_text(encoding="utf-8-sig").splitlines():
        try:
            sample = json.loads(line)
        except json.JSONDecodeError:
            incomplete += 1
            continue
        if start <= datetime.fromisoformat(sample["utc"]) <= end:
            samples.append(sample)
    if not samples:
        return dict(samples=0, incompleteJsonLines=incomplete, invalidPowerOrHeadroomSamples=["No timed resource coverage"],
            modelWorkingSetPeakGiB=None, modelPrivatePeakGiB=None, modelAndAppWorkingSetPeakGiB=None,
            availableMinimumGiB=None, otherCpuMean=None, otherCpuP95=None, otherCpuMax=None)
    invalid = []
    for s in samples:
        p = s["power"]
        valid = p["acLineStatus"] == 1 and p["energySaver"] == 0
        valid &= all(p[k] == best for k in ("actualOverlayGuid", "effectiveOverlayGuid", "configuredAcGuid"))
        valid &= all(p[k] == 0 for k in ("actualOverlayResult", "effectiveOverlayResult", "configuredAcResult"))
        valid &= s["availablePhysicalBytes"] >= 512 * 1024**2 and s["appWorkingSetBytes"] > 0
        valid &= s["modelPrivateBytes"] + s["appWorkingSetBytes"] <= s["totalPhysicalBytes"]
        if not valid:
            invalid.append(s["utc"])
    cpu = sorted(float(s.get("otherCores") or 0) for s in samples)
    return dict(samples=len(samples), incompleteJsonLines=incomplete, invalidPowerOrHeadroomSamples=invalid,
        modelWorkingSetPeakGiB=max(s["modelWorkingSetBytes"] for s in samples) / 1024**3,
        modelPrivatePeakGiB=max(s["modelPrivateBytes"] for s in samples) / 1024**3,
        modelAndAppWorkingSetPeakGiB=max(s["modelWorkingSetBytes"] + s["appWorkingSetBytes"] for s in samples) / 1024**3,
        availableMinimumGiB=min(s["availablePhysicalBytes"] for s in samples) / 1024**3,
        otherCpuMean=sum(cpu) / len(cpu), otherCpuP95=cpu[max(0, math.ceil(.95 * len(cpu)) - 1)], otherCpuMax=max(cpu))


def describe(command, rows):
    def quality(subset):
        n = len(subset)
        q = dict(cases=n, failures=sum(r["Error"] is not None for r in subset),
            meanCaseSeconds=sum(r["Seconds"] for r in subset) / n if n else None)
        m = [r["Metrics"] for r in subset]
        if command == "grounds":
            out = [v for v in m if not v["Expect"]]
            q.update(claimExact=sum(v["ClaimExact"] for v in m),
                coreFound=sum(v["ExpectedFound"] for v in m), coreExpected=sum(len(v["ExpectedArticles"]) for v in m),
                modelCoreFound=sum(v["ModelExpectedFound"] for v in m),
                outOfScope=len(out), outOfScopeDeclined=sum(v["Abstained"] for v in out),
                offClaimArticles=sum(len(v["OffClaim"]) for v in m), excludedAuthorities=sum(len(v["ExcludedProposed"]) for v in m))
        else:
            for key in ("Hard", "Numbers", "Latin", "Ungrounded", "WrongLaw", "RemovedWrongLaw", "LegalPoints", "CopiesRemoved", "Retried", "FellBack"):
                q[key] = sum(v.get(key, 0) for v in m)
            q["formallyComplete"] = sum(v.get("Complete", False) for v in m)
            q["casesWithFallback"] = sum(v.get("FellBack", 0) > 0 for v in m)
        return q
    return dict(all=quality(rows), byPartition={p: quality([r for r in rows if r["Metrics"]["Partition"] == p]) for p in ("development", "robustness")})


results, roots = {}, []
for command in ("grounds", "draft"):
    results[command] = {}
    for model in models:
        folder = args.run_root / (command + "-" + model.replace(":", "-"))
        if not folder.exists():
            results[command][model] = dict(attempted=False)
            continue
        roots.append(folder)
        if not (folder / "measurement-completion.json").exists():
            results[command][model] = dict(attempted=True, complete=False, reason="No measurement completion record")
            continue
        completion = read(folder / "measurement-completion.json")
        rows = read(folder / "measurement-cases.json") if (folder / "measurement-cases.json").exists() else []
        assert [r["Name"] for r in rows] == expected[command][:len(rows)]
        assert all(r["Metrics"]["Partition"] in ("development", "robustness") for r in rows)
        loads = read(folder / "measurement-loads.json") if (folder / "measurement-loads.json").exists() else []
        calls = read(folder / "measurement-calls.json") if (folder / "measurement-calls.json").exists() else []
        resource = resources(folder, completion)
        complete = completion["Complete"] and len(rows) == len(expected[command]) and not resource["invalidPowerOrHeadroomSamples"]
        if complete:
            assert [v["BeforeOrdinal"] for v in loads] == list(range(1, len(rows) + 1, 10))
            assert read(folder / "process-completion.json")["exitCode"] == 0
        results[command][model] = dict(attempted=True, complete=complete, planned=len(expected[command]), completion=completion,
            quality=describe(command, rows), resources=resource, loads=loads,
            amortisedSeconds=completion["BatchSeconds"] / len(rows) if rows else None,
            nativeCalls=len(calls), callsWithoutFinalNativeTiming=sum(c["Native"] is None for c in calls),
            nativeAnswerAtCap=sum(c["LimitReached"] is True for c in calls),
            contextFlags=sum(c["Native"] is not None and c["Native"]["PromptMayBeTruncated"] for c in calls))

target = args.run_root / "app-benchmark-analysis.json"
if target.exists():
    raise ValueError("Refusing to overwrite an existing analysis")
target.write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
hashes = {str(p.relative_to(args.run_root)).replace("\\", "/"): hashlib.sha256(p.read_bytes()).hexdigest()
    for folder in roots for p in folder.rglob("*") if p.is_file()}
(args.run_root / "app-benchmark-hashes.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")

if args.write_docs:
    starts = [read(p / "process-completion.json")["startedUtc"] for p in roots if (p / "process-completion.json").exists()]
    day = datetime.fromisoformat(min(starts)).astimezone().date().isoformat() if starts else datetime.now().date().isoformat()
    for command in ("grounds", "draft"):
        destination = repo / "docs/baselines" / (day + "-alpha-03-app-" + command + ("-" + args.report_suffix if args.report_suffix else ""))
        if destination.exists():
            raise ValueError("Refusing to overwrite a previous report")
        destination.mkdir(parents=True)
        lines = ["# App " + command + " benchmark: gemma3 versus thinking-off gemma4", "",
            "Default fictitious development/robustness cases only; no hidden or validation inference. Ordinary app prompts, parser, legal data, model caps and fallback behavior unchanged.", "",
            "Conditions: mains power, **Best performance**, desktop app open and idle in template mode, browsers/VS Code closed, no concurrent build/test/model workload. Serial models; empty CPU/context-matched load before cases 1/11/21/31/41 as needed; **512 MiB** available-memory guard and model-private-plus-app physical-RAM guard. Load time is separate from original case time; amortised batch includes loads, waits and cleanup. Resource sampling every two seconds can miss short peaks.", "",
            "| measure | gemma3:4b (thinking omitted) | gemma4:e4b (thinking off) |", "|---|---:|---:|"]
        def row(label, callback):
            values = []
            for model in models:
                result = results[command][model]
                values.append(str(callback(result)) if result.get("quality") else "not measured")
            lines.append("| " + label + " | " + " | ".join(values) + " |")
        def number(value):
            return "n/a" if value is None else f"{value:.6f}"
        def fraction(found, expected):
            return f"{found}/{expected}" if expected else "n/a (no such cases measured)"
        row("Complete guarded run", lambda r: str(r["complete"]))
        for part in ("development", "robustness"):
            row(part + " cases", lambda r, p=part: r["quality"]["byPartition"][p]["cases"])
            if command == "grounds":
                row(part + " claim exact", lambda r, p=part: f'{r["quality"]["byPartition"][p]["claimExact"]}/{r["quality"]["byPartition"][p]["cases"]}')
                row(part + " core recall", lambda r, p=part: f'{r["quality"]["byPartition"][p]["coreFound"]}/{r["quality"]["byPartition"][p]["coreExpected"]}')
                row(part + " out-of-scope declined", lambda r, p=part: fraction(r["quality"]["byPartition"][p]["outOfScopeDeclined"], r["quality"]["byPartition"][p]["outOfScope"]))
            else:
                row(part + " formally complete", lambda r, p=part: f'{r["quality"]["byPartition"][p]["formallyComplete"]}/{r["quality"]["byPartition"][p]["cases"]}')
                for key in ("Hard", "Numbers", "Latin", "Ungrounded", "WrongLaw", "RemovedWrongLaw", "LegalPoints", "CopiesRemoved", "Retried", "FellBack", "casesWithFallback"):
                    row(part + " " + key, lambda r, p=part, k=key: r["quality"]["byPartition"][p][k])
            row(part + " seconds/case", lambda r, p=part: number(r["quality"]["byPartition"][p]["meanCaseSeconds"]))
        row("All cases, seconds/case", lambda r: number(r["quality"]["all"]["meanCaseSeconds"]))
        row("Amortised seconds/case", lambda r: number(r["amortisedSeconds"]))
        row("Native calls / calls without final native timing", lambda r: f'{r["nativeCalls"]} / {r["callsWithoutFinalNativeTiming"]}')
        row("Native answers at cap / context flags", lambda r: f'{r["nativeAnswerAtCap"]} / {r["contextFlags"]}')
        for key in ("modelWorkingSetPeakGiB", "modelPrivatePeakGiB", "modelAndAppWorkingSetPeakGiB", "availableMinimumGiB", "otherCpuMean", "otherCpuP95", "otherCpuMax", "samples"):
            row(key, lambda r, k=key: number(r["resources"][k]) if k != "samples" else r["resources"][k])
        row("Invalid timed power/headroom samples", lambda r: len(r["resources"]["invalidPowerOrHeadroomSamples"]))
        lines += ["", "Gemma3: Gemma Terms, installed digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`. Gemma4: Apache 2.0, installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights/downloads/runtime model switch.", "",
            "Core recall is the unchanged app's final article recall, including catalogue-required articles; model-only recall is preserved in the original summary below and the analysis JSON. Draft checks are automatic checks of the final safeguarded text, including retries/removals/fallbacks; they do not establish legal correctness. A stream that the app stops early may have no final native timing; its measured wall time is retained.", "",
            "Raw run root: `" + str(args.run_root) + "`. `app-benchmark-analysis.json` holds exact metrics and every load; `app-benchmark-hashes.json` hashes the unmodified run artifacts. Reproduce with `Measure-AppBenchmarks.ps1 -RunRoot <fresh external folder>` after build/tests and quiet/power checks, then `python -B tools/alpha-03/analyze_app_benchmarks.py <folder> --write-docs` after timing ends.", ""]
        for model in models:
            result = results[command][model]
            if not result.get("quality"):
                continue
            folder = args.run_root / (command + "-" + model.replace(":", "-"))
            lines += ["## " + model + " source, loads and command", "", "Source commit: `" + str(read(folder / "manifest.json")["Commit"]) + "`.", "", "```text", (folder / "command.txt").read_text(encoding="utf-8-sig").strip(), "```", "", "| Before case | Unload wall s | Empty warm-up wall s | Native load s |", "|---:|---:|---:|---:|"]
            for load in result["loads"]:
                lines.append(f'| {load["BeforeOrdinal"]} | {load["UnloadWallSeconds"]:.6f} | {load["WarmupWallSeconds"]:.6f} | {load["NativeLoadSeconds"]} |')
            summary = folder / "summary.md"
            lines += ["", "## Original " + model + " harness summary", "", summary.read_text(encoding="utf-8-sig") if summary.exists() else "No complete harness summary; partial run only."]
        (destination / "summary.md").write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")
print(json.dumps({c: {m: dict(complete=r.get("complete", False), quality=r.get("quality"), resources=r.get("resources")) for m, r in values.items()} for c, values in results.items()}, indent=2))
