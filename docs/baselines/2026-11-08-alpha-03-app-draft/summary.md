# App draft benchmark: gemma3 versus thinking-off gemma4

**Neither drafting model was run.** The preceding gemma3 grounds batch stopped after 27/46 cases on a resource-log sharing violation in the collector. The serial runner stopped before gemma4 grounds and both drafting runs, as required by the precondition rule. No drafting score, time, peak memory, cap or failure count was measured; “not measured” is not zero or success. See the [grounds stop record](../2026-11-08-alpha-03-app-grounds/summary.md) for the evidence, preserved partials and collector-only repair. No inference was retried. A fresh full four-run restart requires the owner's decision.

Default fictitious development/robustness cases only; no hidden or validation inference. Ordinary app prompts, parser, legal data, model caps and fallback behavior unchanged.

Planned conditions, not a record of a drafting run: mains power, **Best performance**, desktop app open and idle in template mode, browsers/VS Code closed, no concurrent build/test/model workload. Serial models; empty CPU/context-matched load before cases 1/11/21/31/41 as needed; **512 MiB** available-memory guard and model-private-plus-app physical-RAM guard. Load time is separate from original case time; amortised batch includes loads, waits and cleanup. Resource sampling every two seconds can miss short peaks.

| measure | gemma3:4b (thinking omitted) | gemma4:e4b (thinking off) |
|---|---:|---:|
| Complete guarded run | not measured | not measured |
| development cases | not measured | not measured |
| development formally complete | not measured | not measured |
| development Hard | not measured | not measured |
| development Numbers | not measured | not measured |
| development Latin | not measured | not measured |
| development Ungrounded | not measured | not measured |
| development WrongLaw | not measured | not measured |
| development RemovedWrongLaw | not measured | not measured |
| development LegalPoints | not measured | not measured |
| development CopiesRemoved | not measured | not measured |
| development Retried | not measured | not measured |
| development FellBack | not measured | not measured |
| development casesWithFallback | not measured | not measured |
| development seconds/case | not measured | not measured |
| robustness cases | not measured | not measured |
| robustness formally complete | not measured | not measured |
| robustness Hard | not measured | not measured |
| robustness Numbers | not measured | not measured |
| robustness Latin | not measured | not measured |
| robustness Ungrounded | not measured | not measured |
| robustness WrongLaw | not measured | not measured |
| robustness RemovedWrongLaw | not measured | not measured |
| robustness LegalPoints | not measured | not measured |
| robustness CopiesRemoved | not measured | not measured |
| robustness Retried | not measured | not measured |
| robustness FellBack | not measured | not measured |
| robustness casesWithFallback | not measured | not measured |
| robustness seconds/case | not measured | not measured |
| All cases, seconds/case | not measured | not measured |
| Amortised seconds/case | not measured | not measured |
| Native calls / calls without final native timing | not measured | not measured |
| Native answers at cap / context flags | not measured | not measured |
| modelWorkingSetPeakGiB | not measured | not measured |
| modelPrivatePeakGiB | not measured | not measured |
| modelAndAppWorkingSetPeakGiB | not measured | not measured |
| availableMinimumGiB | not measured | not measured |
| otherCpuMean | not measured | not measured |
| otherCpuP95 | not measured | not measured |
| otherCpuMax | not measured | not measured |
| samples | not measured | not measured |
| Invalid timed power/headroom samples | not measured | not measured |

Gemma3: Gemma Terms, installed digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`. Gemma4: Apache 2.0, installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights/downloads/runtime model switch.

Core recall is the unchanged app's final article recall, including catalogue-required articles; model-only recall is preserved in the original summary below and the analysis JSON. Draft checks are automatic checks of the final safeguarded text, including retries/removals/fallbacks; they do not establish legal correctness. A stream that the app stops early may have no final native timing; its measured wall time is retained.

Raw run root: `C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-app-benchmarks-20261108-210534`. `app-benchmark-analysis.json` holds exact metrics and every load; `app-benchmark-hashes.json` hashes the unmodified run artifacts. Reproduce with `Measure-AppBenchmarks.ps1 -RunRoot <fresh external folder>` after build/tests and quiet/power checks, then `python -B tools/alpha-03/analyze_app_benchmarks.py <folder> --write-docs` after timing ends.
