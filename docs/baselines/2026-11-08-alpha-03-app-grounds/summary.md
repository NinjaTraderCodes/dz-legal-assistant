# App grounds benchmark: gemma3 versus thinking-off gemma4

The authorised restart is now complete: see the [9 November Stage1 full comparison](../2026-11-09-alpha-03-app-grounds-stage-1/summary.md). The original partial observations below remain intact.

**Superseded for the owner-authorised 9 November restart.** Retained intact as the interrupted run's record and the same-27-case interim reference. It is not the completed Stage 1 baseline; replacement results will be recorded separately.

**Stopped on a collector I/O race after 27/46 gemma3 cases. No complete comparison is available.** The guard could not open the live `resources.jsonl` while PowerShell's resource monitor held its append handle. This is a measurement failure. All 54 model calls completed with zero model errors, cap hits and context flags. No gemma4 grounds or drafting run started; no case was retried.

The recorded prefix contains 25 development and two robustness cases; 19 development cases, including all four out-of-scope cases, were never attempted. Every number below is descriptive of that prefix only, and must not be used as the full baseline. The harness's final summary was never reached; its existing case reports and exact-metric JSON remain intact. The original launcher recorded a null native exit code; the wrapper itself exited 1. The missing code is not reconstructed or overwritten.

Native window: 2026-11-08 20:05:51.8688602–20:40:46.9903496 UTC (21:05:51–21:40:46 local). All **984 timed samples** passed mains/Best-performance/physical-headroom checks. The 512 MiB guard did not trip; minimum available was 1.881725 GiB. Background CPU mean/p95/maximum are retained below, including the benchmark harness and normal system work. No memory-growth cause was investigated.

Collector repair, without further inference: this runner now opts into an explicit shared-read/write append handle; the legacy monitor mode stays unchanged. Sharing/lock violations alone receive at most ten 25 ms delays, each followed by a fresh read and all unchanged guards. Other failures still stop immediately; no HTTP/model/case retry is added. The monitor also recorded one “Stream was not readable” error at the stop timestamp; that original record is retained. The launcher retains the native process handle before polling and rejects a missing exit code. Two new C# sharing fixtures prove success after a brief lock and bounded failure without an inference call; three native PowerShell fixtures prove exit 0, exit 7 and writing while a reader holds the file. All 19 focused C# tests pass, build 0 warnings/errors. Full post-commit checks are reported in the handoff. Await the owner before a fresh full restart from case 1; never append the remaining cases to this prefix.

Default fictitious development/robustness cases only; no hidden or validation inference. Ordinary app prompts, parser, legal data, model caps and fallback behavior unchanged.

Conditions: mains power, **Best performance**, desktop app open and idle in template mode, browsers/VS Code closed, no concurrent build/test/model workload. Serial models; empty CPU/context-matched load before cases 1/11/21/31/41 as needed; **512 MiB** available-memory guard and model-private-plus-app physical-RAM guard. Load time is separate from original case time; amortised batch includes loads, waits and cleanup. Resource sampling every two seconds can miss short peaks.

| measure | gemma3:4b (thinking omitted) | gemma4:e4b (thinking off) |
|---|---:|---:|
| Complete guarded run | False | not measured |
| development cases | 25 | not measured |
| development claim exact | 23/25 | not measured |
| development core recall | 27/39 | not measured |
| development out-of-scope declined | not reached (0/4 planned cases) | not measured |
| development seconds/case | 77.177146 | not measured |
| robustness cases | 2 | not measured |
| robustness claim exact | 2/2 | not measured |
| robustness core recall | 3/4 | not measured |
| robustness out-of-scope declined | n/a (no such cases) | not measured |
| robustness seconds/case | 73.790629 | not measured |
| All cases, seconds/case | 76.926293 | not measured |
| Amortised seconds/case | 77.597092 | not measured |
| Native calls / calls without final native timing | 54 / 0 | not measured |
| Native answers at cap / context flags | 0 / 0 | not measured |
| modelWorkingSetPeakGiB | 9.293472 | not measured |
| modelPrivatePeakGiB | 9.731831 | not measured |
| modelAndAppWorkingSetPeakGiB | 9.508934 | not measured |
| availableMinimumGiB | 1.881725 | not measured |
| otherCpuMean | 0.503509 | not measured |
| otherCpuP95 | 0.775936 | not measured |
| otherCpuMax | 1.428015 | not measured |
| samples | 984 | not measured |
| Invalid timed power/headroom samples | 0 | not measured |

Gemma3: Gemma Terms, installed digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`. Gemma4: Apache 2.0, installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights/downloads/runtime model switch.

Core recall is the unchanged app's final article recall, including catalogue-required articles; model-only recall is preserved in the original summary below and the analysis JSON. Draft checks are automatic checks of the final safeguarded text, including retries/removals/fallbacks; they do not establish legal correctness. A stream that the app stops early may have no final native timing; its measured wall time is retained.

Raw run root: `C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-app-benchmarks-20261108-210534`. `app-benchmark-analysis.json` holds exact metrics and every load; `app-benchmark-hashes.json` hashes the unmodified run artifacts. Supplementary `observed-collector-stop.json` discloses the stop and missing exit code without editing original records. The corrected runner's reproducible command is `Measure-AppBenchmarks.ps1 -RunRoot <fresh external folder>` after build/tests, owner-authorised restart and quiet/power checks, then `python -B tools/alpha-03/analyze_app_benchmarks.py <folder> --write-docs` after timing ends. Empty warm-up responses omitted native `load_duration`; the null values below are not invented zeroes.

## gemma3:4b source, loads and command

Source commit: `e31e298624f6e4943a147ff92abf418e8f768c99`.

```text
"C:\Users\TheShadow\dz-legal-assistant\src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll" "grounds" "--models" "gemma3:4b" "--out" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-app-benchmarks-20261108-210534\grounds-gemma3-4b" "--measurement-root" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-app-benchmarks-20261108-210534\grounds-gemma3-4b" "--endpoint" "http://127.0.0.1:11434" "--law-root" "C:\Users\TheShadow\dz-legal-assistant\data\law" "--data" "C:\Users\TheShadow\dz-legal-assistant\data\document-types" "--cases" "C:\Users\TheShadow\dz-legal-assistant\tests\grounds-cases" "--partition" "development,robustness"
```

| Before case | Unload wall s | Empty warm-up wall s | Native load s |
|---:|---:|---:|---:|
| 1 | 0.022714 | 2.988629 | None |
| 11 | 0.008354 | 3.545154 | None |
| 21 | 0.009858 | 3.298679 | None |

## Original gemma3:4b harness summary

# Grounds analysis bench

## Preserved artifact checksums

All **60 original run files** match the artifact manifest. Derived records were added separately; no original exit/completion/result was rewritten.

| Derived artifact | SHA-256 |
|---|---|
| `app-benchmark-analysis.json` | `8560a9f865e377704876c5b100f0b5fe0a3b24c46d7ba09138b850fa39cebbfb` |
| `app-benchmark-hashes.json` | `d5254087ca4381f8d5c12bcc03423abd844d9e39e56ecb800f220d826b8a7e67` |
| `observed-collector-stop.json` | `9ce42e74798b864fbd12d314eb81ea8bef6b7440e7f4d7674b06bcee440050b5` |
