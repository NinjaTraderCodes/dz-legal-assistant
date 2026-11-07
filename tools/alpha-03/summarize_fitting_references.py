"""Summarize recorded fitting references only; never run inference or read audit labels."""

import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import re
import statistics


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def stats(values):
    ordered = sorted(values)
    if not ordered:
        return None
    return dict(n=len(ordered), mean=statistics.mean(ordered),
                median=statistics.median(ordered),
                p95=ordered[math.ceil(.95 * len(ordered)) - 1], max=ordered[-1])


def explicit_absences(answer):
    # Supplement only: parsed facts remain the original reader's output. Missing
    # rows never become correct absences. The raw answers remain reviewable.
    result = set()
    seen = set()
    for raw in answer.splitlines():
        line = raw.strip().lstrip("*`-•").rstrip("*`").strip()
        line = line.translate(str.maketrans("٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹", "01234567890123456789"))
        match = re.match(r"^([rRsS]?\d+|[pP])\s*[)|:]\s*(.+)$", line)
        if not match:
            continue
        ident, body = match.group(1).lower().lstrip("s"), match.group(2)
        status = re.search(r"\b(stated|affirmed|yes|denied|no|disputed|absent|none|نعم|لا)\b", body, re.I)
        if ident in seen or not (status or re.search(r"\b[vV]\d+\b", body)):
            continue
        seen.add(ident)
        if status and status.group().lower() in ("absent", "none"):
            result.add(ident)
    return result


def quality(rows, cases, catalog):
    counts = collections.Counter()
    confusion = collections.defaultdict(collections.Counter)
    targets = collections.defaultdict(collections.Counter)
    errors = collections.Counter()
    error_cases = collections.Counter()
    for row in rows:
        case = cases[row["Name"]]
        counts["cases"] += 1
        counts["criticalCases"] += bool(row["HasCritical"])
        errors.update(e["Type"] for e in row["Errors"])
        error_cases.update(set(e["Type"] for e in row["Errors"]))
        counts["capHits"] += bool(row["ReachedCap"])
        facts = row["Facts"] or {}
        events = {e["Kind"]: e for e in facts.get("Events", [])}
        requests = {q["Kind"]: q for q in facts.get("Requests", [])}
        absences = explicit_absences(row["Answer"])
        questions = catalog[case["claim"]]
        event_ids = {e["event"]: str(i + 1) for i, e in enumerate(questions["elements"])}
        for gold in case["annotation"]["events"]:
            predicted = events.get(gold["kind"])
            label = predicted["Polarity"] if predicted else (
                "absent" if event_ids[gold["kind"]] in absences else "unresolved")
            confusion[gold["polarity"]][label] += 1
            if gold["kind"] == case["annotation"]["target"]:
                targets[gold["polarity"]][label] += 1
            counts["askedEvents"] += 1
            counts["statusCorrect"] += label == gold["polarity"]
            counts["unresolvedStatus"] += label == "unresolved"
            if gold["polarity"] != "affirmed":
                counts["nonAffirmedLabels"] += 1
                counts["nonAffirmedErrors"] += label != gold["polarity"]
                counts["falseAffirmations"] += label == "affirmed"
            if gold["polarity"] != "absent":
                counts["sourceEligibleEvents"] += 1
                sources = set(predicted["Sentences"]) if predicted else set()
                counts["eventsWithAnySource"] += bool(sources)
                counts["eventsWithGoldSourceOverlap"] += bool(sources.intersection(gold["evidence"]))
            for field in ("date", "amount"):
                if gold[field] is None:
                    continue
                counts[field + "Known"] += 1
                value = predicted.get(field.title()) if predicted else None
                if value is None:
                    counts[field + "Missing"] += 1
                elif value["Normalized"] != gold[field]:
                    counts[field + "Wrong"] += 1
                else:
                    counts[field + "Correct"] += 1
        for gold in case["annotation"]["requests"]:
            predicted = requests.get(gold["kind"])
            counts["askedRequests"] += 1
            counts["requestPresenceCorrect"] += bool(predicted) == gold["present"]
            counts["requestMissing"] += gold["present"] and not predicted
            counts["requestExtra"] += not gold["present"] and bool(predicted)
            if gold["amount"] is not None:
                counts["requestAmountKnown"] += 1
                value = predicted.get("Amount") if predicted else None
                suffix = "Missing" if value is None else "Correct" if value["Normalized"] == gold["amount"] else "Wrong"
                counts["requestAmount" + suffix] += 1
    return dict(counts=dict(counts), confusion=dict(confusion), targetConfusion=dict(targets),
                errorOccurrences=dict(errors), casesByErrorType=dict(error_cases),
                seconds=stats([r["Seconds"] for r in rows]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("run_root", type=Path)
    parser.add_argument("--workspace", type=Path, default=Path.cwd())
    args = parser.parse_args()
    manifest = read(args.workspace / "tests/alpha-03-cases/split-manifest.json")
    fitting = {m["name"] for m in manifest["members"] if m["split"] == "fitting"}
    cases = {c["name"]: c for c in read(args.workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in fitting}
    catalog = {c["claim"]: c for c in read(args.workspace / "data/law/claim-elements.json")["claims"]}
    resources = [json.loads(line) for line in (args.run_root / "resources.jsonl").read_text(encoding="utf-8-sig").splitlines()]
    output = {}
    for folder in sorted(args.run_root.glob("reference-*")):
        completion = read(folder / "completion.json")
        rows = read(folder / "case-results.json")
        if any(r["Name"] not in fitting for r in rows):
            raise ValueError("Non-fitting record: analysis refused")
        completed = [r for r in rows if r["Timings"] is not None]
        loads = read(folder / "loads.json") if (folder / "loads.json").exists() else []
        samples = [s for s in resources if completion["StartedUtc"][:23] <= s["utc"][:23] <= completion["FinishedUtc"][:23]]
        paging = [s["paging"] for s in samples if s.get("pagingSampleUpdated") and s.get("paging")]
        best = "ded574b5-45a0-4f42-8737-46345c09c238"
        valid_power = sum(s["power"]["acLineStatus"] == 1 and all(s["power"][key] == best for key in ("actualOverlayGuid", "effectiveOverlayGuid", "configuredAcGuid")) for s in samples)
        output[completion["Model"]] = dict(
            completion=completion, inferenceCompleted=len(completed),
            blockedBeforeNativeTiming=[r["Name"] for r in rows if r["Timings"] is None],
            actualRequestFiles=len(list(folder.glob("call-*-request.json"))),
            reloads=loads,
            warmupWallSeconds=stats([r["WarmupWallSeconds"] for r in loads]),
            unloadWallSeconds=sum(r["UnloadWallSeconds"] for r in loads),
            nativeLoadSeconds=stats([r["NativeLoadSeconds"] for r in loads if r["NativeLoadSeconds"] is not None]),
            amortizedSecondsIncludingLoads=completion["BatchSeconds"] / len(completed) if completed else None,
            overall=quality(completed, cases, catalog),
            byKind={kind: quality([r for r in completed if r["Kind"] == kind], cases, catalog) for kind in ("polarity", "binding")},
            byFold={str(fold): quality([r for r in completed if r["Fold"] == fold], cases, catalog) for fold in (1, 2, 3)},
            resources=dict(samples=len(samples), powerValidSamples=valid_power,
                modelWorkingSetPeak=max(s["modelWorkingSetBytes"] for s in samples),
                modelPrivatePeak=max(s["modelPrivateBytes"] for s in samples),
                modelAppWorkingSetPeak=max(s["modelWorkingSetBytes"] + s["appWorkingSetBytes"] for s in samples),
                availablePhysicalMinimum=min(s["availablePhysicalBytes"] for s in samples),
                commitPeak=max(s["commitBytes"] for s in samples),
                otherCores=stats([s["otherCores"] or 0 for s in samples]),
                monitorCores=stats([s["powershellCores"] or 0 for s in samples]),
                pagingFreshSamples=len(paging),
                pageReadsPeak=max(p["PageReadsPersec"] for p in paging),
                pagesInputPeak=max(p["PagesInputPersec"] for p in paging),
                pagesOutputPeak=max(p["PagesOutputPersec"] for p in paging)),
            promptTokens=stats([r["Timings"]["PromptTokens"] for r in completed]),
            answerTokens=stats([r["Timings"]["AnswerTokens"] for r in completed]))
    target = args.run_root / "reference-analysis.json"
    target.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    hashes = {p.relative_to(args.run_root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
              for p in sorted(args.run_root.rglob("*")) if p.is_file() and p.name != "artifact-hashes.json"}
    (args.run_root / "artifact-hashes.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(output, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
