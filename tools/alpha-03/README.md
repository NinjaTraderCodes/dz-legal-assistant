# Alpha-03 measurement probes

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
