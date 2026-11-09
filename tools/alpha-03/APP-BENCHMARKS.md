# Default app benchmark comparison

**All requested stages complete; stop after final report push.** [Stage3 four-model summary comparison](../../docs/baselines/2026-11-09-alpha-03-hidden-v2-phone-models/summary.md): G3/G4E4B/G4E2B/G3nE2B critical-free39/58/41/42 of90, amort25.919/20.019/7.194/18.081s. Bothphoneexits0/all1193newguard samplesvalid; only permitted summariesread, no case/console inspection. Newphone requests-found63/69% versusG4E4B95%, so critical-free counts do not imply equal dense quality. Original4Bcolumnsnotrerun. No runtime/reader/scorer change. Stage2 and its24 unchanged copied output documents are already pushed; links below. Reproduction commands are historical instructions, not authorisation for another run.

**Stage2 finished:** [12-case draft report](../../docs/baselines/2026-11-09-alpha-03-app-draft-stage-2/summary.md). Both exits0, formal11/12,7legalpoints, no hard/number/Latin/ungrounded/wrong-law issues; G4=25.237689s,WS/private7.212105/7.936943GiB; G3=38.629184s,6.288589/6.672466. One G3sectionretry/no fallbacks; three G3app-stopped streams lack final native timing. All380 samples pass; originals unchanged. Push/check this report, then proceed to already authorised Stage3; no new go required.

**Current go: Stage2 then Stage3 without another wait.** The owner amended Stage2 to the **12 existing files unchanged**, from `tests/drafting-cases` and `tests/drafting-cases-more`; do not write/adapt cases. `DRAFT-STAGE-2.json` pins their order, bytes and coverage before inference. Use the same12 for G4thinkfalse then G3thinkomitted:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File tools/alpha-03/Measure-AppBenchmarks.ps1 -RunRoot "<fresh external directory>" -Stage draft -Gemma4First -IncludeMoreDrafts -PauseBusySearch
python -B tools/alpha-03/analyze_app_benchmarks.py "<same directory>" --stage draft --include-more-drafts --write-docs --report-suffix stage-2
```

Optional `draft --extra-cases <folder>` appends that folder's existing files to the first folder's ordered batch, retains their original relative `extends` resolution, checks unique IDs and fingerprints both folders. The normal one-folder default is unchanged. All12 share one reload cadence per model (before1/11), not two separate batches. The runner checks the pinned original hashes before timing. Coverage is limited to eight primary types plus the explicit lease-rescission component in08; the manifest lists the12 other catalogue types without a distinct case. Auxiliary loan damages are not counted as a standalone damages case.

Push the Stage2 report after required checks, then proceed directly to Stage3: pull both approved phone models, record digests before timing, G4E2Bthinkfalse first and G3nE2Bthinkomitted next. Use the owner wrapper, same Alpha-01 reader/settings/cap/guards, fresh external outputs. Agent may read only the four exact `summary.md` files needed for the comparison, never the hidden input folder, case outputs or consoles. Stop after Stage3, or immediately on any failed precondition. Earlier stage-specific stop notes below are historical.

**Stage1 finished on9 November:** both46-case grounds runs exited0 and passed all recorded guards. [Full comparison](../../docs/baselines/2026-11-09-alpha-03-app-grounds-stage-1/summary.md): gemma4 claim46/46/core51/68/56.231844s; gemma3 claim41/46/core48/68/69.931247s. Models unloaded, awake state restored, Search remained running. **Wait for a new owner go before Stage2 or3.** The commands below document reproduction, not permission to rerun.

**Current authorisation (9 November): Stage 1 only.** Restart all 46 grounds cases in a fresh external directory, **gemma4:e4b first, then gemma3:4b**. At gemma4 case27, report its first27 against the preserved gemma3 prefix on the same IDs, then continue. After both complete, report and wait. Draft and phone-model stages need a separate owner go. No hidden or validation data is used in this stage.

Source `e31e298` completed 27/46 gemma3 grounds cases, then a transient resource-log sharing violation stopped the collector. Its native exit-code recorder lost the code; it remains null in the original record. The 8 November reports are retained and marked superseded by this staged restart. Never splice that prefix into a complete run.

The repaired collector enables `Measure-Resources.ps1 -SharedAppend` only for these app benchmarks: its writer explicitly shares read/write access and avoids PowerShell's conflicting `Add-Content` handle. The monitor's default behavior for existing scripts is unchanged. A fallback retries only resource-log sharing/lock errors, bounded to ten 25 ms delays and a fresh check each time; thresholds and all other stop conditions are unchanged. It never retries a model request or a case. `AppBenchmarkProcess.ps1` retains the native process handle before exit polling and rejects a missing exit code. `Test-AppBenchmarkProcess.ps1` verifies native exits 0/7 and a real simultaneous shared reader/writer with zero model calls; two added C# sharing fixtures verify bounded reads and no repeated inference. No real-model rerun followed the repair.

Owner-authorised task after `cda67fc`: extend the optional `--think true|false` override from facts to grounds and draft, then compare gemma3:4b (thinking omitted) with gemma4:e4b (thinking off). No production/default model, prompt, legal data, app path, caps or default fallback behavior changes. The previous Alpha-01 freeze and its fingerprints remain historical at their original commit.

The default inputs contain 46 grounds cases (44 development, 2 robustness; 4 out of scope) and five drafting cases (3 development, 2 robustness). No default file is validation or hidden data. The runner fixes these two repository paths, passes `--partition development,robustness` explicitly to grounds, and uses all five default drafts. Guarded mode rejects other partitions. It never points at an owner evaluation directory or reads the previous blind-run output.

Build and run the full tests first, then shut down build servers. On mains power and **Best performance**, with browsers/VS Code and heavy applications closed and the app open/idle in template mode:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File tools/alpha-03/Measure-AppBenchmarks.ps1 -RunRoot "<fresh external directory>" -Stage grounds -Gemma4First -PauseBusySearch
```

For this command the runner executes only `grounds --models`, gemma4 then gemma3, one run each. Only gemma4 receives `--think false`. Every model/command has its own fresh output and resource monitor. Installed model digests are checked against the already recorded artefacts. No weights are downloaded. Browser closing is owner-only. It unloads the two benchmark models during setup, refusing a different resident model. The runner's legacy no-switch plan remains unchanged; it is not the current stage command.

The thread requests temporary system-awake status and restores its prior status in `finally`, recording `awake-state.json`; no sleep or power-plan setting changes. At the owner's direction, optional `-PauseBusySearch` measures the SearchIndexer/SearchProtocolHost/SearchFilterHost CPU group for five seconds before each model. If it exceeds 0.5 core equivalents, the runner temporarily stops WSearch, restoring an originally running service after the stage. It never changes the startup type. Per-model `search-state.json` and stage `search-session.json` preserve the measurements and actions. Failure to obtain the requested conditions stops the stage.

Opt-in `--measurement-root`, matching `--out` outside the repository, adds case boundaries and a provider guard around the existing benchmarks. It checks fresh power/app/browser/memory state before every request and after requests/cases. It unloads/reloads before cases 1/11/21/31/41 as needed, with empty CPU/context-matched warm-ups and separate load timings, then unloads at the end. The existing **512 MiB** available-memory and model-private-plus-app physical-RAM guards are reused unchanged. Context warnings and transport/guard errors stop the batch without a rescue reload, next model or retry. Existing app-level output checks, retries, removed paragraphs, token-limit outcomes and fallback behavior remain part of the drafting result.

`measurement-cases.json` contains the exact existing evaluator metrics and unrounded original case times. `measurement-calls.json` contains per-call wall/native timing, cap and context metadata; an app-stopped stream can have no final native timing, which is kept null. Loads and completion are separate JSON files. Original summaries and per-case outputs are preserved. Two-second resource samples report model/server working-set/private peaks, concurrent model+app working set, minimum available memory and background CPU; sampling can miss short peaks.

After all inference and monitors stop:

```powershell
python -B tools/alpha-03/analyze_app_benchmarks.py "<same directory>" --stage grounds --write-docs --report-suffix stage-1
```

The offline analyser checks case order, allowed partitions, actual think option, load boundaries, exits, completion and every timed power/headroom sample. With the stage command above, it writes derived analysis/hashes externally and only `docs/baselines/<start-date>-alpha-03-app-grounds-stage-1/summary.md`, including the unchanged original summaries. Development and robustness stay separate. Core recall includes catalogue-required additions; model-only recall is also preserved. Timeouts, empty answers, cut-off exceptions and native answer-cap counts are reported separately. Draft automatic checks do not establish legal correctness. Partial/unqualified runs are labelled, never silently replaced or retried.

On `INTERIM27_READY`, run `python -B tools/alpha-03/interim_app_grounds.py <new root> <preserved 8 November public default-case root>`. It checks the retained prefix's content hash, the exact27 IDs and the cases/law/form fingerprints, writes one non-overwriting interim snapshot, and reports the requested three metrics. It performs no model inference and never reads an owner evaluation directory.

For an owner-authorised restart, use a new raw directory and pass `--report-suffix rerun-1` to the analyser so the earlier same-day stop reports stay intact. The analyser refuses to overwrite existing reports or analysis artifacts.

Tests exercise actual grounds/draft CLI invocations against a local mock server with and without thinking overrides, shared option validation, unchanged legacy defaults, case-boundary reloads and scorer results, permanent memory/context failure, streaming pieces and preserved cutoff outcomes. They make no real model call. Existing tests are retained.
