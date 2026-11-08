# Default app benchmark comparison

**Current stop:** source `e31e298` completed 27/46 gemma3 grounds cases, then a transient resource-log sharing violation stopped the collector. The native exit-code recorder also lost the code; it remains null in the original record. No other model/command ran. Summaries under `docs/baselines/2026-11-08-alpha-03-app-grounds/` and `...-app-draft/` label the prefix and missing results explicitly. Preserve the entire run; await the owner before a fresh full restart, with no reuse of the prefix as a complete baseline.

The repaired collector enables `Measure-Resources.ps1 -SharedAppend` only for these app benchmarks: its writer explicitly shares read/write access and avoids PowerShell's conflicting `Add-Content` handle. The monitor's default behavior for existing scripts is unchanged. A fallback retries only resource-log sharing/lock errors, bounded to ten 25 ms delays and a fresh check each time; thresholds and all other stop conditions are unchanged. It never retries a model request or a case. `AppBenchmarkProcess.ps1` retains the native process handle before exit polling and rejects a missing exit code. `Test-AppBenchmarkProcess.ps1` verifies native exits 0/7 and a real simultaneous shared reader/writer with zero model calls; two added C# sharing fixtures verify bounded reads and no repeated inference. No real-model rerun followed the repair.

Owner-authorised task after `cda67fc`: extend the optional `--think true|false` override from facts to grounds and draft, then compare gemma3:4b (thinking omitted) with gemma4:e4b (thinking off). No production/default model, prompt, legal data, app path, caps or default fallback behavior changes. The previous Alpha-01 freeze and its fingerprints remain historical at their original commit.

The default inputs contain 46 grounds cases (44 development, 2 robustness; 4 out of scope) and five drafting cases (3 development, 2 robustness). No default file is validation or hidden data. The runner fixes these two repository paths, passes `--partition development,robustness` explicitly to grounds, and uses all five default drafts. Guarded mode rejects other partitions. It never points at an owner evaluation directory or reads the previous blind-run output.

Build and run the full tests first, then shut down build servers. On mains power and **Best performance**, with browsers/VS Code and heavy applications closed and the app open/idle in template mode:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File tools/alpha-03/Measure-AppBenchmarks.ps1 -RunRoot "<fresh external directory>"
```

The runner executes the actual harness `grounds --models` and `draft --models` commands, in that order, gemma3 then gemma4, one run each. Only gemma4 receives `--think false`. Every model/command has its own fresh output and resource monitor. Installed model digests are checked against the already recorded artefacts. No weights are downloaded. Browser closing is owner-only; the runner changes neither power nor services. It unloads the two benchmark models during setup, refusing a different resident model.

Opt-in `--measurement-root`, matching `--out` outside the repository, adds case boundaries and a provider guard around the existing benchmarks. It checks fresh power/app/browser/memory state before every request and after requests/cases. It unloads/reloads before cases 1/11/21/31/41 as needed, with empty CPU/context-matched warm-ups and separate load timings, then unloads at the end. The existing **512 MiB** available-memory and model-private-plus-app physical-RAM guards are reused unchanged. Context warnings and transport/guard errors stop the batch without a rescue reload, next model or retry. Existing app-level output checks, retries, removed paragraphs, token-limit outcomes and fallback behavior remain part of the drafting result.

`measurement-cases.json` contains the exact existing evaluator metrics and unrounded original case times. `measurement-calls.json` contains per-call wall/native timing, cap and context metadata; an app-stopped stream can have no final native timing, which is kept null. Loads and completion are separate JSON files. Original summaries and per-case outputs are preserved. Two-second resource samples report model/server working-set/private peaks, concurrent model+app working set, minimum available memory and background CPU; sampling can miss short peaks.

After all inference and monitors stop:

```powershell
python -B tools/alpha-03/analyze_app_benchmarks.py "<same directory>" --write-docs
```

The offline analyser checks case order, allowed partitions, load boundaries, exits, completion and every timed power/headroom sample. It writes derived analysis/hashes externally and two summaries under `docs/baselines/<start-date>-alpha-03-app-grounds/` and `...-app-draft/`, including the unchanged original summaries. Development and robustness stay separate. Core recall includes catalogue-required additions; model-only recall is also preserved. Draft automatic checks do not establish legal correctness. Partial/unqualified runs are labelled, never silently replaced or retried.

For an owner-authorised restart, use a new raw directory and pass `--report-suffix rerun-1` to the analyser so the earlier same-day stop reports stay intact. The analyser refuses to overwrite existing reports or analysis artifacts.

Tests exercise actual grounds/draft CLI invocations against a local mock server with and without thinking overrides, shared option validation, unchanged legacy defaults, case-boundary reloads and scorer results, permanent memory/context failure, streaming pieces and preserved cutoff outcomes. They make no real model call. Existing tests are retained.
