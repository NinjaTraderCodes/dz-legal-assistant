# App grounds benchmark: gemma3 versus thinking-off gemma4

Default fictitious development/robustness cases only; no hidden or validation inference. Ordinary app prompts, parser, legal data, model caps and fallback behavior unchanged.

**Stage1 complete; stop and await the owner's go.** Fresh case1 restart from source `7eb9d5b99d8d41f8eba43ff186ba27fe786a7530`, **gemma4 first, then gemma3**, with no overlap or retry. Both native exits0 and measurement completions true. The [8 November interrupted grounds report](../2026-11-08-alpha-03-app-grounds/summary.md) is preserved as superseded; its prefix is used only for the separately requested interim comparison below. No Stage2 draft or Stage3 model pull/run was attempted.

Conditions: mains power, **Best performance**, desktop app open and idle in template mode, browsers/VS Code closed, no concurrent build/test/model workload. Serial models; empty CPU/context-matched load before cases 1/11/21/31/41 as needed; **512 MiB** available-memory guard and model-private-plus-app physical-RAM guard. Load time is separate from original case time; amortised batch includes loads, waits and cleanup. Resource sampling every two seconds can miss short peaks.

The owner confirmed the reopened app's template/idle mode before timing. All **2776 timed resource samples** pass the recorded power/headroom conditions; there are no incomplete JSON lines, context flags or resource sharing retries. Windows Search's CPU group measured0 cores in both five-second preflights, so WSearch was **not stopped**, remained Running/Auto and retained its startup type. Temporary system-awake status was held from00:05:23.1643585 to01:44:14.7624932 UTC and the prior thread state was restored. Ollama was empty afterwards; the agent-opened idle desktop app then closed normally for post-commit checks.

**Timing limitation:** at00:10:28 UTC, the monitor attributed about0.75 CPU cores to Task Manager. Its normal close request succeeded at00:11:08.7567148 UTC during the early gemma4 block. This event is preserved in `background-window-event.json`; the early timings remain included and unadjusted. The complete background-CPU distributions are below. This is one serial pass per model, with initial model order fixed by the owner; it is descriptive evidence, without an estimate of run-to-run uncertainty.

| measure | gemma3:4b (thinking omitted) | gemma4:e4b (thinking off) |
|---|---:|---:|
| Complete guarded run | True | True |
| think value sent | omitted | false |
| timeouts / empty answers / cut-off answers | 0 / 0 / 0 | 0 / 0 / 0 |
| All cases: failures | 0 | 0 |
| All cases: claim exact | 41/46 | 46/46 |
| All cases: core recall | 48/68 | 51/68 |
| All cases: model-only core recall | 41/68 | 49/68 |
| All cases: out-of-scope declined | 3/4 | 4/4 |
| development cases | 44 | 44 |
| development claim exact | 39/44 | 44/44 |
| development core recall | 45/64 | 48/64 |
| development out-of-scope declined | 3/4 | 4/4 |
| development seconds/case | 69.613523 | 56.042667 |
| robustness cases | 2 | 2 |
| robustness claim exact | 2/2 | 2/2 |
| robustness core recall | 3/4 | 3/4 |
| robustness out-of-scope declined | n/a (no such cases measured) | n/a (no such cases measured) |
| robustness seconds/case | 76.921192 | 60.393743 |
| All cases, seconds/case | 69.931247 | 56.231844 |
| Amortised seconds/case | 70.559730 | 57.412001 |
| Native calls / calls without final native timing | 88 / 0 | 87 / 0 |
| Native answers at cap / context flags | 0 / 0 | 0 / 0 |
| modelWorkingSetPeakGiB | 9.319675 | 8.007034 |
| modelPrivatePeakGiB | 9.709381 | 8.733517 |
| modelAndAppWorkingSetPeakGiB | 9.542950 | 8.265053 |
| availableMinimumGiB | 2.229450 | 2.793434 |
| otherCpuMean | 0.301622 | 0.275594 |
| otherCpuP95 | 0.434126 | 0.790905 |
| otherCpuMax | 2.032561 | 2.042522 |
| samples | 1529 | 1247 |
| Invalid timed power/headroom samples | 0 | 0 |

Gemma3: Gemma Terms, installed digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`. Gemma4: Apache 2.0, installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights/downloads/runtime model switch.

Core recall is the unchanged app's final article recall, including catalogue-required articles; model-only recall is preserved in the original summary below and the analysis JSON. Draft checks are automatic checks of the final safeguarded text, including retries/removals/fallbacks; they do not establish legal correctness. A stream that the app stops early may have no final native timing; its measured wall time is retained.

Raw run root: `C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-stage1-20261109-010521`. `app-benchmark-analysis.json` holds exact metrics and every load; `app-benchmark-hashes.json` hashes all174 unmodified per-model run artifacts, independently rechecked after collection. Both runs have the same full input fingerprints and source commit. Reproduce only after authorisation, build/tests and quiet/power checks with `Measure-AppBenchmarks.ps1 -RunRoot <fresh external folder> -Stage grounds -Gemma4First -PauseBusySearch`, then `python -B tools/alpha-03/analyze_app_benchmarks.py <folder> --stage grounds --write-docs --report-suffix <fresh suffix>` after timing ends.

Aggregate analysis SHA256: `713913216db1e81ed71c5e1711f1be492bd67e401ec850eb3fa1965e83dc804c`. Per-model artifact-hash manifest SHA256: `ec4df8d610ec5c0f5e8eca44654ceeadc27e6d5e6358329e8dc1f671994ca39d`. Interim snapshot SHA256: `8efc6a9b94973c3ccd449b9abc0bea77b3987351ac71d2815c5d38e7285b4b0e`.

## Requested interim at gemma4 case27

Posted while gemma4 continued, before the fresh gemma3 run. Exactly the same27 IDs (25 development, two robustness; no out-of-scope cases), checked against the retained old prefix hash and identical cases/law/form fingerprints. These are the requested interim numbers, not the full-run comparison above.

| Measure | Gemma3 old interrupted prefix | Gemma4 first27, think=false |
|---|---:|---:|
| Claim exact | 25/27 | 27/27 |
| Core recall | 30/43 | 33/43 |
| Seconds/case | 76.926293 | 60.318588 |

## Execution and verification

| Model | Start UTC | End UTC | Exit | Cases / calls / loads |
|---|---|---|---:|---:|
| gemma4:e4b, think=false | 2026-11-09 00:05:44.6235434 | 2026-11-09 00:49:48.2024164 | 0 | 46 / 87 / 5 |
| gemma3:4b, think omitted | 2026-11-09 00:50:06.9745413 | 2026-11-09 01:44:14.4076488 | 0 | 46 / 88 / 5 |

Before timing, source7eb9d5b passed the required Release build with **0 warnings/errors**, all **1026 tests**, and the PowerShell native-exit/shared-I/O/order/awake fixtures (zero model calls). The app's actual grounds commands above provide the manual feature check; no UI change or screenshot requirement applies. Post-report commit build/test results are in the final handoff. Model working set includes Ollama/server processes; the concurrent model+app peak is distinct from their individual peaks, and no memory-growth cause was investigated. All five loads are itemised separately below; Ollama returned no native load-duration field for the empty warm-ups, retained as None rather than fabricated.

## gemma3:4b source, loads and command

Source commit: `7eb9d5b99d8d41f8eba43ff186ba27fe786a7530`.

```text
"C:\Users\TheShadow\dz-legal-assistant\src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll" "grounds" "--models" "gemma3:4b" "--out" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-stage1-20261109-010521\grounds-gemma3-4b" "--measurement-root" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-stage1-20261109-010521\grounds-gemma3-4b" "--endpoint" "http://127.0.0.1:11434" "--law-root" "C:\Users\TheShadow\dz-legal-assistant\data\law" "--data" "C:\Users\TheShadow\dz-legal-assistant\data\document-types" "--cases" "C:\Users\TheShadow\dz-legal-assistant\tests\grounds-cases" "--partition" "development,robustness"
```

| Before case | Unload wall s | Empty warm-up wall s | Native load s |
|---:|---:|---:|---:|
| 1 | 0.014885 | 4.109657 | None |
| 11 | 0.008322 | 3.338984 | None |
| 21 | 0.008439 | 3.097568 | None |
| 31 | 0.007208 | 3.115946 | None |
| 41 | 0.013608 | 2.843261 | None |

## Original gemma3:4b harness summary

# Grounds analysis bench

## ollama gemma3:4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 89 % | 89 % | 70 % | 61 % | 13 % | 75 % | 0 % | 3.1 | 0 | 0 | 70 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 89 % | 69 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 2/2 core · 0 off-claim · 63s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 92s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 88s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 67s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 78s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 68s
- ✗ `rescission-sale` expected contract-rescission → unpaid-sale-price · 0/2 core · 5 off-claim · 81s
- ✗ `contract-damages` expected contract-damages → unpaid-sale-price · 0/2 core · 3 off-claim · 67s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 82s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 62s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 80s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 80s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 60s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 80s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 76s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 72s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 58s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 72s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 62s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 81s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 61s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 76s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 64s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 80s
- ✓ `tatliq-harm` expected tatliq → tatliq · 0/1 core · 0 off-claim · 97s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 75s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 97s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 70s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 86s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 71s
- ✓ `custody-transfer` expected custody-forfeiture → custody-forfeiture · 1/2 core · 0 off-claim · 89s
- ✓ `visitation` expected visitation → visitation · 0/1 core · 0 off-claim · 58s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 57s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 76s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 54s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 74s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 69s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 56s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 33s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✗ `none-commercial-company` expected none → commercial-debt · 0/0 core · 3 off-claim · 57s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 57s
- ✗ `annulment-fraud-land` expected sale-annulment-fraud → unpaid-sale-price · 0/2 core · 4 off-claim · 72s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 65s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 75 % | 0 | 77 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 70s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 84s

### Where the time goes (gemma3:4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 45 | 0.0 | 1417 | 42.9 | 130 | 10.1 | 33.1 | 0 |
| grounds.selection | 43 | 0.0 | 771 | 43.8 | 94 | 10.6 | 17.7 | 0 |


## gemma4:e4b source, loads and command

Source commit: `7eb9d5b99d8d41f8eba43ff186ba27fe786a7530`.

```text
"C:\Users\TheShadow\dz-legal-assistant\src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll" "grounds" "--models" "gemma4:e4b" "--out" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-stage1-20261109-010521\grounds-gemma4-e4b" "--measurement-root" "C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-stage1-20261109-010521\grounds-gemma4-e4b" "--endpoint" "http://127.0.0.1:11434" "--law-root" "C:\Users\TheShadow\dz-legal-assistant\data\law" "--data" "C:\Users\TheShadow\dz-legal-assistant\data\document-types" "--cases" "C:\Users\TheShadow\dz-legal-assistant\tests\grounds-cases" "--partition" "development,robustness" "--think" "false"
```

| Before case | Unload wall s | Empty warm-up wall s | Native load s |
|---:|---:|---:|---:|
| 1 | 0.021140 | 8.954796 | None |
| 11 | 0.011246 | 8.696790 | None |
| 21 | 0.007880 | 8.203437 | None |
| 31 | 0.008367 | 7.695235 | None |
| 41 | 0.009634 | 7.706481 | None |

## Original gemma4:e4b harness summary

# Grounds analysis bench

## ollama gemma4:e4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 100 % | 100 % | 75 % | 72 % | 0 % | 100 % | 0 % | 2.5 | 0 | 0 | 56 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 100 % | 74 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 47s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 69s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 77s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 61s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 2/2 core · 0 off-claim · 67s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 54s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 60s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 59s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 70s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 54s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 62s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 60s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 55s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 66s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 65s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 61s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 51s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 61s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 49s
- ✓ `commercial-supplies` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 67s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 43s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 60s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 53s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 63s
- ✓ `tatliq-harm` expected tatliq → tatliq · 0/1 core · 0 off-claim · 72s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 59s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 73s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 54s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 65s
- ✓ `custody` expected custody → custody · 1/2 core · 0 off-claim · 55s
- ✓ `custody-transfer` expected custody-forfeiture → custody-forfeiture · 1/2 core · 0 off-claim · 66s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 46s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 43s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 66s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 46s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 64s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 63s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 41s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 29s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✓ `none-commercial-company` expected none → — · 0/0 core · 0 off-claim · 28s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 43s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 58s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 59s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 75 % | 0 | 60 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 48s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 73s

### Where the time goes (gemma4:e4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 45 | 0.0 | 1415 | 59.4 | 131 | 10.0 | 23.9 | 0 |
| grounds.selection | 42 | 0.0 | 780 | 60.0 | 85 | 9.6 | 13.1 | 0 |
