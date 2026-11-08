# Alpha-03 measurement probes

**New authorised task after the frozen comparison:** `--think` now also applies to `grounds` and `draft`, with omission preserving defaults. [APP-BENCHMARKS.md](APP-BENCHMARKS.md) describes the default development/robustness comparison, ten-case reloads and existing memory/power/browser guards. This authorises no hidden or validation run. Its new source commit does not change the historical frozen candidate at `cda67fc`.

**Historical frozen handoff:** the owner froze unchanged Alpha-01 with gemma4:e4b and thinking off. The completed gemma3 offline diagnosis gives 36/96 baseline, 21/96 uncalibrated and 23/96 corrected critical-error-free cases, with 26/63/46 false affirmations. No scored arm is selected. See [OWNER-RUN.md](OWNER-RUN.md) for the two frozen commands and [OWNER-FREEZE.json](OWNER-FREEZE.json) for that commit's source/model fingerprints. The historical probe and fitting instructions below do not authorise further work.

Run from the repository root in Windows PowerShell with .NET 10 and the already installed Ollama models. The owner authorised these bounded measurements before the 144-case freeze. They replay the accepted gate-1 wire fixture on the original development inputs; candidate reader and prompt work still awaits the freeze.

All requests use `127.0.0.1`, CPU inference and the accepted fixture's settings. Run one benchmark at a time on mains power in **Best performance** mode, after build/tests finish and competing heavy work ends. Keep outputs outside the repository. These scripts make no downloads or persistent runtime configuration changes. `Power.ps1` checks the AC source, configured AC mode and actual/effective overlay before and after chains/model batches; the resource monitor samples those values throughout. It requires the Best performance GUID `ded574b5-45a0-4f42-8737-46345c09c238`. Windows can retain a Balanced base plan while using that performance mode; see [Microsoft's AC power-mode API](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powergetuserconfiguredacpowermode).

If Windows blocks local script files, launch these scripts with `powershell -NoProfile -ExecutionPolicy RemoteSigned -File ...`, or use `Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned` in their parent shell. This applies only to that process; do not change a persistent execution policy. Keep the PowerShell files in UTF-8 with a BOM for Arabic literals in Windows PowerShell 5.1.

The power helper uses Windows 11 AC-mode APIs available on the measured laptop. An unsupported host stops the check rather than assuming a power mode.

## Prepare inputs

Use a new output directory. `facts export` supplies numbered sentences without answers. `case-manifest.json` supplies only original development names, confirmed claims, note lengths and question counts; no gold event labels enter the requests.

```powershell
$runRoot = Join-Path ([IO.Path]::GetTempPath()) 'dzlegal-alpha03-gate2-20261107'
New-Item -ItemType Directory -Path $runRoot
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts export --cases tests/fact-cases --partition development --out (Join-Path $runRoot 'development-inputs.json')
$catalog = (Get-Content data/law/claim-elements.json -Encoding utf8 -Raw | ConvertFrom-Json).claims
$dev = @((Get-Content tests/fact-cases/cases.json -Encoding utf8 -Raw | ConvertFrom-Json).cases | Where-Object { $_.partition -eq 'development' -and $_.claim -in $catalog.claim })
$manifest = @($dev | ForEach-Object {
    $c = $_
    $ce = $catalog | Where-Object claim -eq $c.claim
    [pscustomobject]@{name=$c.name;claim=$c.claim;notesCharacters=([string]$c.inputs.'notes.facts').Length;eventQuestions=@($ce.elements).Count}
})
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $runRoot 'case-manifest.json') -Encoding utf8
```

The dated run uses the accepted external `b-1-request.json` from the 6 November gate-1 shape probe (request SHA-256 `cb95460ba73413548a706f759a54d6f149eb7d70831355788a2508f159b29be3`). Its raw location is recorded in the gate-1 summary. Supply that file through `-FixturePath`; no new status definitions are composed here.

## Run serially

Start `Measure-Resources.ps1 -RunRoot <directory>` in a separate hidden PowerShell process before measurements, and wait for its first `resources.jsonl` record. Keep an idle desktop app resident and let its startup warm-up finish. Record and unload any model loaded by that startup before benchmarking. The monitor samples at approximately two-second intervals; starting it and compiling its small Windows memory-query helper happen before timing.

```powershell
& tools/alpha-03/Measure-OrderedChoices.ps1 -RunRoot $runRoot -FixturePath $acceptedGate1Fixture
& tools/alpha-03/Measure-Alpha01.ps1 -RunRoot $runRoot
Set-Content -LiteralPath (Join-Path $runRoot 'monitor.stop') -Value 'complete'
```

`Measure-OrderedChoices.ps1` asks all 147 catalogue event questions across the 40 original in-catalogue development cases. It also runs three predeclared reversed orders. Each chain starts after an explicit unload and context/CPU-matched empty warm-up, whose cost is recorded separately. Every extension includes the actual generated answer unchanged. It records raw requests, responses, finite-score availability, sampled digits, four-score argmaxes, cache counters and wall times. Report missing scores as unavailable. Chain timing includes local orchestration; HTTP timing is also retained. These chains leave roles, value binding and requests to the retained Alpha-01 pass, whose cost must be added when judging the proposed reader's budget.

`Measure-Alpha01.ps1` invokes the existing harness unchanged, first with `gemma3:4b`, then `gemma4:e4b`. Each model starts unloaded. The harness excludes four out-of-catalogue development cases, leaving 40 inference cases. It preserves each model's normal thinking behaviour and the existing 300-token reader cap. Per-case internal times in the harness summary are rounded to seconds; timestamped stdout is supplementary instrumentation. Capture exact installed model digests and licences with `/api/tags` and `/api/show` alongside the run.

`Measure-Resources.ps1` records simultaneous sums of Ollama/runner working sets and private bytes, desktop app residence, available physical memory, commit, and background process CPU deltas. It also samples system page-read/input/output rates approximately every ten seconds; `pagingSampleUpdated` distinguishes new readings from retained ones. Model-file loading can itself cause page reads, so distinguish startup from sustained inference when interpreting them. Working-set sums can count shared pages twice. Sampled peaks can miss short spikes; per-process lifetime peak fields are retained separately. No thermal qualification is provided. Sampling errors go to `monitor-errors.log` so a gap is visible.

These measurements do not score the status-only chains as complete readers. An order comparison reports observed changes in generated labels and score argmaxes; without repeated same-order controls it cannot attribute every change exclusively to order. No new cases or validation/hidden inputs are read by these scripts.

The completed 7 November run is documented in `docs/baselines/2026-11-07-alpha-03-gate-2/summary.md` and the two rung-0 summaries. Its resident batches grew in memory within one runner PID; report observed batch peaks rather than extrapolating the reset-per-case footprint. The current authorised work stops before case writing.

## Bounded follow-up authorised after gate 2

`Measure-BlindHistory.ps1 -RunRoot <new external directory> -PriorRoot <accepted gate-2 directory>` performs only the three selected forward repeats and the three selected forward/reverse blind-history comparisons. It reuses the recorded original question messages and all sampling options. Repeat chains carry their new actual answers; they never replay old answers as history. Blind chains constrain output to bare JSON `null` while reading the raw four-label alternatives. The first blind call must establish one-token output and four finite label scores. Numeric `0` is the sole fallback if `null` cannot serve as a one-token constant or its schema is rejected. Once a neutral token is selected, it is fixed for every completed blind chain. A failed token/score/cache precondition stops that arm and is recorded; no missing score is filled in.

The owner subsequently allowed only these answer/score/cache diagnostics on battery. `-AllowBatteryDiagnostic` records the actual power source and marks elapsed seconds informative only; it does not change `Power.ps1` or relax any timed model check. `Analyze-BlindHistory.ps1` independently audits all completed raw requests/responses and compares the old forward/reverse, repeated forward and blind forward/reverse results without model calls. It tolerates only `1e-12` of JSON floating-point serialization error and records the observed maximum.

Build `ModelCheck/ModelCheck.csproj -c Release` before timing, then shut down build servers. This small console host directly uses the existing `AlphaFacts.Reader`, `OllamaModelProvider` and `FactsEvaluator`; production source and the reader's request construction stay unchanged. The sole model-setting change is `Think = false`. Its loopback request recorder verifies `think: false`, `stream: true`, CPU-only context 4096 and `num_predict: 300` on the wire. It preserves streaming instead of buffering responses for instrumentation.

```powershell
dotnet build tools/alpha-03/ModelCheck/ModelCheck.csproj -c Release
# After build servers stop, mains/Best performance and resource/app setup are verified:
& tools/alpha-03/Measure-BlindHistory.ps1 -RunRoot $runRoot -PriorRoot $acceptedGate2Directory
& tools/alpha-03/Analyze-BlindHistory.ps1 -RunRoot $runRoot -PriorRoot $acceptedGate2Directory
& tools/alpha-03/Measure-ThinkOff.ps1 -RunRoot $runRoot
& tools/alpha-03/Analyze-ThinkOff.ps1 -RunRoot $runRoot
```

`Measure-ThinkOff.ps1 -RunRoot <directory with active resource monitor>` wraps the host with strict mains/Best performance checks, fresh model unloads, phase tagging, wire invocation metadata and timestamped stdout. Its output directory must be new. It leaves model unload, monitor shutdown and any explicitly authorised temporary Windows Search pause/restoration to the measurement session.

The host plans 40 original in-catalogue development cases, serially. It stops at the first observed 300-token cap or technical failure, discloses attempted/unattempted counts and never increases the cap. Exact case times and native timings are saved beside the unchanged-evaluator summary; raw request bodies remain external. Use the resource monitor and pre/post power records as in the earlier run. No case writing, calibration or candidate prompt/reader implementation is part of this probe.

The owner-only resident gemma3 memory note is in `docs/OWNER-GEMMA3-RESIDENT-MEMORY-NOTE-2026-11-07.md`. That issue is outside this follow-up and is not investigated here.

## Authored case freeze

The subsequent owner-authorised data stage is complete in `tests/alpha-03-cases/`: 144 fictitious cases, 36 families, 96 fitting/48 audit cases, with polarity/binding family kinds and source-linked dense keys. The case README, annotation rules, review and `freeze.json` record its exact scope and mutation policy. This is author-known internal development/audit material, never hidden v2.

`build_cases.py` only assembles hand-authored fixtures from `source/authoring.py` and the preassigned family plan; it has no provider, classifier, prompt or model calls. It refuses regeneration after freeze. `CaseCheck` uses the existing catalogue, sentence splitting and value checks for dense validation. `freeze_cases.py --verify` independently reconstructs groups, checks membership/digests and verifies the current payload hash. `--freeze` refuses an existing record; do not overwrite it.

```powershell
dotnet build tools/alpha-03/CaseCheck/CaseCheck.csproj -c Release
dotnet tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll tests/alpha-03-cases data/law
python -B tools/alpha-03/freeze_cases.py --verify
```

The owner requires future grouped fitting comparisons of fixed-order blind history versus forward/reverse score averaging, and every candidate on both gemma3:4b and gemma4:e4b with thinking off, reporting costs and separate family-kind results. No such run or candidate implementation is part of this data freeze. The current stop is after checking/committing/pushing the frozen corpus.

The owner subsequently adjudicated removal of explanatory text hints before inference. Revision 2 changes 123 case texts while retaining every annotation, value/date and source-sentence identity/link. It archives the first record and includes the exact edit inventory in the new 17-file payload. `revise_case_text.py --verify` compares against pinned local Git objects from `41a9a86`, verifies all invariants and requires that predecessor to be present. Its `--prepare` mode was bounded to that owner-approved correction, using a temporary preview and static checks; it refuses a later frozen version. Normal case regeneration remains blocked after the re-freeze. Run the verification command, not preparation, on this completed version.

## Fitting references and operational stop

`FittingReferences/README.md` documents the manifest-selected 96-case host. `Measure-FittingReferences.ps1` runs models serially, retains normal residence and prevents the second model after technical/runtime failure. Alpha-01 and its wire settings remain unchanged (gemma4 thinking off). Build first, then start idle app/monitor and verify mains/Best performance/quiet load. Cleanup and restoration of owner-authorised service pauses remain the session's responsibility.

`summarize_fitting_references.py <external run root>` analyses recorded fitting results, emits external `reference-analysis.json` and artifact hashes, and never calls a model. It separates native-timed inferences from blocked attempts, reports both family kinds/incomplete folds, original evaluator errors, dense status/completeness/source diagnostics and sampled resources. Explicit numbered absence differs from unresolved output; known-value omissions remain visible. Null gold values are not automatically forbidden values. Per-kind memory is chronological evidence, not an isolated complexity measurement.

The first run stopped after 25 gemma3 calls at the declared 512 MiB machine-headroom guard. No gemma4/candidate/audit followed. See `docs/baselines/2026-11-07-alpha-03-fitting-reference-interrupted/summary.md`. Corpus unchanged. `FITTING-RESUME.md` records the stopped checkpoint, not authority to resume or alter residence policy.

The owner subsequently corrected decisive flags and authorised v3 (`9fc0eb64...da3a5`), with all old partial predictions excluded from comparisons. `revise_decisiveness.py` is the bounded correction/invariant checker; the frozen inventory explicitly labels all 548 events and the builder requires it. Do not repeat preparation on the frozen successor. `freeze_cases.py --verify` checks the complete version chain.

For every remaining timed run, owner-required protocol is browser/heavy apps closed, mains/Best performance, unload/reload every ten cases and the unchanged 512 MiB guard. The reference host now handles cases 1/11/.../91 boundaries internally and performs separate empty CPU/context-matched warm-ups. `loads.json` and exact raw reload requests/responses preserve load/unload/native times; case time excludes these operations, while amortised whole-batch cost includes them and monitor waits. No unscheduled resets rescue a failing block. Both references restart at case 1. `MeasurementGuard.cs` records the exact failure reason/sample and supports a `measurement.stop` marker, without production changes. No candidate prompt/reader has been written while references are pending.

Both v3 references finished, but final full-log audit detected Brave reopening during gemma4; its cost comparison is unqualified and the stage stopped before candidates. See the v3 fitting-reference summary. `audit_references.py <run-root>` independently checks all96 fitting requests/model, ten reloads/model, source completeness, wire settings and observed browser activity. The summariser now reports per-kind resources and separates model directories from derived `reference-*.json` files; artifact hashes exclude the disposable console trace to avoid changing-output hashes. Raw data is never replaced.

The guard now checks browser/VS Code process presence before every reader call and after unload/warm-up. `GuardCheck` and `Test-MeasurementGuard.ps1` are standalone no-model regression checks, with synthetic resource snapshots and a benign owned process-name fixture. They add no legal cases and do not alter application/unit tests. No timed rerun after the detected precondition failure is authorised until the owner answers; `FITTING-RESUME.md` starts with that current stop. `FITTING-PROTOCOL.md` declares the finite order/calibration/model selection before any future scored inference, not a candidate implementation.

The owner has now authorised a full gemma4-only repeat, retaining both original semantic reference accuracies/facts (gemma3 36/96, first gemma4 69/96). Use `Measure-FittingReferences.ps1 -ModelNames gemma4:e4b` and `audit_references.py <new-root> --models gemma4:e4b` on a fresh external root; the default still selects both models. `compare_reference_repeat.py <first-root> <rerun-root>` reports exact answers, critical pass/fail verdicts, full error multisets and parsed-facts matches, transitions and per-kind/fold counts. It refuses the same root twice and writes only the new root. The repeat's timing/memory qualifies the cost column; its accuracy is diagnostic and cannot replace the first accuracy or supply preferred baseline answers. Then continue the authorised fitting/locked-primary audit, stopping under the existing gates and before validation.

That repeat stopped after37 actual calls when the strict per-request guard detected Firefox process presence before request38. Its full96 cost/repeatability is unavailable; all first semantic references remain retained. The comparator's explicit `--allow-partial-diagnostic` option reports every completed native call from a precondition-stopped attempt, records planned96/completedN/fullRepeatAvailable=false and excludes blocked evaluations from matches. Default mode still refuses an incomplete run. No automatic resume or selective best-answer replacement. See `docs/baselines/2026-11-07-alpha-03-gemma4-repeat-stopped/summary.md`; further model work waits for the owner under the stop rule.

The owner then authorised a new full96 restart, now complete: see `docs/baselines/2026-11-08-alpha-03-gemma4-repeat/summary.md`. All96 wire/source checks pass, ten loads, mains/Best performance, no browser/transport/context/cap failure. Exact answers84/96, critical verdicts94/96 match the first run; first accuracy/facts remain retained. Mean case15.448330/amortised16.469249 seconds with background load disclosed. Old stopped notes above are historical.

The owner requested a safe pause before any candidate inference. `ScoredFitting` and harness-only `ScoredStatusReader` are an initial source checkpoint, not a qualified reader. Build with `dotnet build tools/alpha-03/ScoredFitting/ScoredFitting.csproj -c Release`. The single `scored-status.ar.txt` uses the unchanged complete catalogue propositions and owner-set polarity rules. No input gold/annotation fields enter the prompt. All four byte-exact single-token scores must be finite in top20; literal0 alone is carried in exact append-only history. No score imputation/retry or schema fallback. Missing scores/cache/context/power/browser/headroom stop the stage. Measurement guard source is linked from the reference host, not duplicated.

After the owner resumes, bounded `capability` mode (first fitting case, both orders, each model serially) precedes any full fitting collection. The command takes mode/model/casesRoot/lawRoot/promptPath/freshExternalOutput/resourceRoot; see `FITTING-RESUME.md`. `fitting` mode selects96 manifest fitting inputs only and reloads every10 complete cases, recording all raw calls/loads. Outputs refuse existing/repository directories. No live capability, candidate accuracy/cost, calibration, binding, compatibility or audit result is claimed. Synthetic-HTTP tests check missing/duplicate/nonfinite/byte-mismatched scores, neutral/history/cache/context rules, no fabricated recovered values, preserved baseline facts, tie order and local model/endpoint restrictions. Resume protocol and the owner's pause take precedence over historical active notes.

Owner resumed; the actual unchanged scored shape passed bounded capability on both models,24/24 positions and20/20 cache extensions across first fitting input001 forward/reverse. See `docs/baselines/2026-11-08-alpha-03-scored-capability/summary.md`. **Browser closing is now owner-only**. `Measure-ScoredFitting.ps1 -RunRoot <fresh external resource root> -Mode fitting` collects both models serially with existing guards and ten-case reloads, stopping before any next model on failure. It assumes the documented quiet mains/Best-performance/app/monitor setup and does not close any app. `audit_scored_collection.py <root> --mode capability|fitting` checks exact prompt/source/history arrays, native finite byte-exact scores, positive extension reuse, context/options/counts, load requests and every timed power/headroom sample, then writes derived audit/artifact hashes to the new root only. It requires complete records; missing calls are never filled. Do not modify archived reference/capability records or run analysis/build/tests concurrently with timed inference. Capability is not an accuracy or full-fitting cost result; finite selection/gates remain in `FITTING-PROTOCOL.md`.

**Current stage stopped on gemma4 missing class4 at native request47**, after G3 completed96/744. See `docs/baselines/2026-11-08-alpha-03-scored-fitting-stopped/summary.md` and top of `FITTING-RESUME.md`; wait for owner. No corpus calibration/evaluation/selection/binding/compatibility/audit/validation was run. Single-model audits now have explicit suffixes (`fitting-audit-gemma3-4b.json`), never the fullstage filename. `diagnose_scored_stop.py` reproduces this specific saved partial failure from raw records without imputing scores or fabricating the missing completion file. Collector exception handling and wrapperfinally cleanup are repaired; `Test-ScoredFailure.ps1` tests returned/thrown native failures with mocked native/HTTP functions, no model or real HTTP, no second-model continuation. The added C# synthetic test ensures unavailable class scores stop before another question, without retry.

Saved offline source, **not executed on corpus data**: `fit_status.py <complete-collection-root> <new-external-output>` requires complete two-model audit and fits exactly the declared3fold/grid; `StatusEvaluation` builds separately and would apply statuses through unchanged `FactsEvaluator.Score`, pinning both first semantic-reference hashes; `select_status.py` would apply the declared paired gates/ranking, with qualified G4 repeat costs only; selected-only `fit_status.py --refit <selection>` would freeze final offsets. These are not permission to bypass the stop or silently switch to one model. `python -B tools/alpha-03/test_fit_status.py` runs seven numerical fixtures only. Shared quality code adds explicit choices and dense decisive omissions without altering legacy scoring; no old artifact root is regenerated. Both models remain runtime-unchanged.
