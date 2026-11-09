"""Read exactly four owner-authorised summary.md files; emit aggregate fields only."""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--report-day", required=True)
parser.add_argument("--check-references-only", action="store_true")
args = parser.parse_args()
assert re.fullmatch(r"\d{4}-\d{2}-\d{2}", args.report_day)
repo = Path(__file__).resolve().parents[2]
base = Path(os.environ["LOCALAPPDATA"]) / "DzLegalAssistant/alpha-03-owner-runs"
names = {
    "gemma3:4b": "hidden-v2-alpha01-gemma3",
    "gemma4:e4b": "hidden-v2-alpha01-gemma4-think-off",
    "gemma4:e2b": "hidden-v2-alpha01-gemma4-e2b-think-off-" + args.report_day,
    "gemma3n:e2b": "hidden-v2-alpha01-gemma3n-e2b-" + args.report_day,
}


def extract(model, folder):
    # No glob, directory listing or supporting-file read is permitted here.
    assert folder == names[model]
    path = base / folder / "summary.md"
    raw = path.read_bytes()
    text = raw.decode("utf-8-sig").replace("\r\n", "\n")
    assert "\nRun complete." in text, "Only completed summaries qualify"
    row = re.search(r"^\| hidden \|(.+)\|$", text, re.M)
    assert row, "Expected the original scorer's hidden partition row"
    values = [v.strip() for v in row[1].split("|")]
    assert len(values) == 9
    keys = ("casesRead", "casesWithCriticalError", "plaintiffPosition", "decisiveEventsFound",
            "datesOnRightEvent", "polarityRight", "requestsFound", "failures", "caseSecondsRounded")
    assert len(values) == len(keys)
    result = dict(zip(keys, values))
    for key in ("casesRead", "casesWithCriticalError", "failures", "caseSecondsRounded"):
        result[key] = int(result[key])
    result["casesWithoutCriticalError"] = result["casesRead"] - result["casesWithCriticalError"]
    outside = re.search(r"(\d+) case\(s\) outside the claim catalogue", text)
    result["outOfCatalogueAbstained"] = int(outside[1]) if outside else 0
    result["attemptedFiles"] = result["casesRead"] + result["outOfCatalogueAbstained"]
    for label, key in (("Critical errors", "criticalErrors"), ("Other differences", "otherDifferences")):
        line = re.search(r"^" + label + r": (.*)$", text, re.M)
        assert line
        result[key] = {m[1].strip(): int(m[2]) for m in re.finditer(r"([^,]+?) (\d+)(?:,|$)", line[1])}
    batch = re.search(r"Full batch including loads/waits: ([\d.]+) s; per attempted case: ([\d.]+) s\. Loads: (\d+)\.", text)
    assert batch
    result.update(batchSeconds=float(batch[1]), amortisedSeconds=float(batch[2]), loads=int(batch[3]))
    memory = re.search(r"working-set peak: ([\d.]+) GiB; private peak: ([\d.]+) GiB; concurrent model\+app working-set peak: ([\d.]+) GiB; minimum machine available: ([\d.]+) GiB\. Resource samples: (\d+); invalid power/headroom samples: (\d+)\.", text)
    assert memory
    for key, value in zip(("modelWorkingSetPeakGiB", "modelPrivatePeakGiB", "modelAppPeakGiB", "availableMinimumGiB"), memory.groups()[:4]):
        result[key] = float(value)
    result.update(resourceSamples=int(memory[5]), invalidPowerHeadroomSamples=int(memory[6]))
    native = re.search(r"^\| facts\.claim \|(.+)\|$", text, re.M)
    assert native
    native_values = [v.strip() for v in native[1].split("|") if v.strip()]
    native_keys = ("calls", "loadSecondsAverageRounded", "promptTokensAverage", "promptTokensPerSecond", "answerTokensAverage", "writeTokensPerSecond", "firstWordSeconds", "contextFlags")
    assert len(native_values) == len(native_keys)
    result["nativeTiming"] = dict(zip(native_keys, native_values))
    result["thinkSent"] = "false" if model.startswith("gemma4:") else "omitted"
    identity = re.search(r"^Model: (.*); digest: ([a-f0-9]{64}); think value sent: (false|omitted)\.$", text, re.M)
    if identity:
        assert identity[1] == model and identity[3] == result["thinkSent"]
        result["digestInSummary"] = identity[2]
    else:
        assert model in ("gemma3:4b", "gemma4:e4b")
    result.update(summaryPath=str(path), summarySha256=hashlib.sha256(raw).hexdigest(), runComplete=True)
    return result


selected = list(names.items())[:2] if args.check_references_only else list(names.items())
results = {model: extract(model, folder) for model, folder in selected}
assert len({r["attemptedFiles"] for r in results.values()}) == 1
assert len({r["casesRead"] for r in results.values()}) == 1
if args.check_references_only:
    print(json.dumps({model: dict(casesRead=r["casesRead"], casesWithoutCriticalError=r["casesWithoutCriticalError"], amortisedSeconds=r["amortisedSeconds"]) for model, r in results.items()}, indent=2))
    raise SystemExit(0)
destination = repo / "docs/baselines" / (args.report_day + "-alpha-03-hidden-v2-phone-models")
if destination.exists():
    raise ValueError("Refusing to overwrite a previous report")
destination.mkdir(parents=True)
(destination / "summary-fields.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
lines = ["# Alpha-01 hidden-v2 summary comparison", "",
    "Owner-authorised blind runs, claim type given. Only the four exact summary.md files were read; no input folder, case output, console or supporting evaluation file was opened or listed. Aggregate fields below are copied/derived only from those summaries; original case-level lines are not reproduced in this report.", "",
    "Mains / **Best performance**, idle template-mode app, closed browsers/VS Code, serial models, ten-case reload and **512 MiB** guard. CPU, context4096, temperature0.2, repeat penalty1.1 and300-token cap; unchanged Alpha-01 prompt/parser/scorer. Gemma4 think=false, gemma3/gemma3n thinking omitted. Prior4B thinking values come from the frozen owner commands; new phone summaries also record the value explicitly. No runtime-model switch.", "",
    "Cases without a critical error are the scorer's green cases; this does not mean every dense field is identical. Cases outside the given claim catalogue abstain and are excluded from the90-case accuracy table. Scorer seconds/case are rounded to whole seconds and refer to read cases; amortised batch time includes all attempted files, loads, waits and cleanup, and is rounded to milliseconds. No unrounded value is inferred from per-case lines.", "",
    "| Measure | " + " | ".join(results) + " |", "|---|---:|---:|---:|---:|"]


def row(label, values):
    lines.append("| " + label + " | " + " | ".join(str(v) for v in values) + " |")


for key in ("thinkSent", "attemptedFiles", "casesRead", "casesWithCriticalError", "casesWithoutCriticalError",
            "outOfCatalogueAbstained", "plaintiffPosition", "decisiveEventsFound", "datesOnRightEvent", "polarityRight",
            "requestsFound", "failures", "caseSecondsRounded", "batchSeconds", "amortisedSeconds", "loads",
            "modelWorkingSetPeakGiB", "modelPrivatePeakGiB", "modelAppPeakGiB", "availableMinimumGiB",
            "resourceSamples", "invalidPowerHeadroomSamples"):
    row(key, [r[key] for r in results.values()])
for kind in ("criticalErrors", "otherDifferences"):
    for key in sorted({key for r in results.values() for key in r[kind]}):
        row(kind + ": " + key, [r[kind].get(key, 0) for r in results.values()])
for key in next(iter(results.values()))["nativeTiming"]:
    row("nativeTiming: " + key, [r["nativeTiming"][key] for r in results.values()])
lines += ["", "Error-category zero means absent from the original scorer's nonzero-count list. The summaries do not provide strict all-fields-right counts, unrounded scorer means, dense status confusion matrices, sampled background CPU distributions or per-load timings; none is fabricated. Resource peaks are sampled and can miss short spikes. No case-level analysis/tuning or retry follows this comparison.", "",
    "## Permitted source summaries", ""]
for model, result in results.items():
    lines += [f'- {model}: `{result["summaryPath"]}`; SHA256 `{result["summarySha256"]}`.']
(destination / "summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(json.dumps({model: {key: r[key] for key in ("casesRead", "casesWithoutCriticalError", "caseSecondsRounded", "amortisedSeconds", "modelWorkingSetPeakGiB", "modelPrivatePeakGiB")} for model, r in results.items()}, indent=2))
