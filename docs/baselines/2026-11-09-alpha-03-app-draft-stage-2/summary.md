> Copied from the research branch `codex/alpha-03` at `480d4a4` (9 Nov 2026); that branch is not merged. Links into `tools/alpha-03/` or to other reports of that branch point to files that exist only there, and local Windows paths are shown with `%USERPROFILE%`.

# App draft benchmark: gemma3 versus thinking-off gemma4

Default fictitious development/robustness cases only; no hidden or validation inference. Ordinary app prompts, parser, legal data, model caps and fallback behavior unchanged.

**Stage2 complete**, source `12aa7cc91f668ac626e00a05a80ae3af0d230c42`: gemma4 first then gemma3, each of the **12 original files unchanged**, in one batch per model. Both native exits0 and guarded completions true, two empty warm-ups each before1/11. No benchmark retry/restart. The owner replaced the proposed20 cases with these12 and forbade writing/adapting cases. After this report is pushed and checked, Stage3 is already authorised without a further go.

Conditions: mains power, **Best performance**, desktop app open and idle in template mode, browsers/VS Code closed, no concurrent build/test/model workload. Serial models; empty CPU/context-matched load before cases 1/11/21/31/41 as needed; **512 MiB** available-memory guard and model-private-plus-app physical-RAM guard. Load time is separate from original case time; amortised batch includes loads, waits and cleanup. Resource sampling every two seconds can miss short peaks.

The owner opened the app manually and confirmed template/idle after the computer-use helper was unavailable; it was not automated through a fallback UI mechanism. All380 timed samples pass the power/headroom conditions; no incomplete sample, context flag or resource-sharing retry. Search measured0 cores in both preflights and remained Running/Auto, without a pause/startup change. Temporary system-awake status was restored after the stage; Ollama was empty afterwards. One serial pass per model, owner-fixed model order: no estimate of run-to-run uncertainty.

| All12 draft metrics | gemma4:e4b, think=false | gemma3:4b, think omitted |
|---|---:|---:|
| Formally complete | 11/12 | 11/12 |
| Hard / number / stray Latin / ungrounded / wrong-law issues | 0 / 0 / 0 / 0 / 0 | 0 / 0 / 0 / 0 / 0 |
| Wrong-law paragraphs / example copies removed | 0 / 0 | 0 / 0 |
| Substance-check legal points | 7 | 7 |
| Section retries / fallbacks | 0 / 0 | 1 / 0 |
| Case failures | 0 | 0 |

Both formal misses are `02-debt-french-notes` (robustness); all10 development drafts are formally complete. The shared7 legal points split4 development/3 robustness. No semantic/legal correctness claim follows from these automatic checks. The one gemma3 section retry occurs in12-app-run-18-oct; no fallback or benchmark retry was substituted. Cut-off exception counters are0 in both; gemma3 has **three app-stopped streams with no final native timing**, so native answer-cap observations cover10/13 calls there (zero reported hits, three unknown), versus12/12 for gemma4. Missing native timing is not recorded as zero tokens or proof that a cap was not reached. Case wall times include these streams.

| measure | gemma3:4b (thinking omitted) | gemma4:e4b (thinking off) |
|---|---:|---:|
| Complete guarded run | True | True |
| think value sent | omitted | false |
| timeouts / empty answers / cut-off answers | 0 / 0 / 0 | 0 / 0 / 0 |
| All cases: failures | 0 | 0 |
| development cases | 10 | 10 |
| development formally complete | 10/10 | 10/10 |
| development Hard | 0 | 0 |
| development Numbers | 0 | 0 |
| development Latin | 0 | 0 |
| development Ungrounded | 0 | 0 |
| development WrongLaw | 0 | 0 |
| development RemovedWrongLaw | 0 | 0 |
| development LegalPoints | 4 | 4 |
| development CopiesRemoved | 0 | 0 |
| development Retried | 1 | 0 |
| development FellBack | 0 | 0 |
| development casesWithFallback | 0 | 0 |
| development seconds/case | 39.226128 | 25.707502 |
| robustness cases | 2 | 2 |
| robustness formally complete | 1/2 | 1/2 |
| robustness Hard | 0 | 0 |
| robustness Numbers | 0 | 0 |
| robustness Latin | 0 | 0 |
| robustness Ungrounded | 0 | 0 |
| robustness WrongLaw | 0 | 0 |
| robustness RemovedWrongLaw | 0 | 0 |
| robustness LegalPoints | 3 | 3 |
| robustness CopiesRemoved | 0 | 0 |
| robustness Retried | 0 | 0 |
| robustness FellBack | 0 | 0 |
| robustness casesWithFallback | 0 | 0 |
| robustness seconds/case | 35.644465 | 22.888624 |
| All cases, seconds/case | 38.629184 | 25.237689 |
| Amortised seconds/case | 39.521172 | 27.109365 |
| Native calls / calls without final native timing | 13 / 3 | 12 / 0 |
| Native answers at cap / context flags | 0 / 0 | 0 / 0 |
| modelWorkingSetPeakGiB | 6.288589 | 7.212105 |
| modelPrivatePeakGiB | 6.672466 | 7.936943 |
| modelAndAppWorkingSetPeakGiB | 6.544525 | 7.473358 |
| availableMinimumGiB | 4.965858 | 3.959732 |
| otherCpuMean | 0.288061 | 0.187897 |
| otherCpuP95 | 0.451856 | 0.324628 |
| otherCpuMax | 1.290985 | 0.509333 |
| samples | 225 | 155 |
| Invalid timed power/headroom samples | 0 | 0 |

Gemma3: Gemma Terms, installed digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`. Gemma4: Apache 2.0, installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights/downloads/runtime model switch.

Core recall is the unchanged app's final article recall, including catalogue-required articles; model-only recall is preserved in the original summary below and the analysis JSON. Draft checks are automatic checks of the final safeguarded text, including retries/removals/fallbacks; they do not establish legal correctness. A stream that the app stops early may have no final native timing; its measured wall time is retained.

Raw run root: `%USERPROFILE%\AppData\Local\Temp\dzlegal-alpha03-stage2-20261109-031349`. `app-benchmark-analysis.json` holds exact metrics and every load; `app-benchmark-hashes.json` hashes all82 per-model artifacts, independently rechecked. Both source commits and all input fingerprints match. Original12 hashes and their manifest contenthash `aacb10fbbf04413a8bb8de27b8c3ee704b50a5c411dbe535e705a28ee8937432` still match. Aggregate analysis SHA256 `03f3be32406011fad18812a29a3c3830aa27880abe811e785556d4a07382b03f`; artifact-hash manifest SHA256 `7468c5add9749b8c8054b34a7578cd1c71fa6a108565935b7fb3c59946852a5e`.

Reproduce only with authorisation and fresh external output: `Measure-AppBenchmarks.ps1 -RunRoot <folder> -Stage draft -Gemma4First -IncludeMoreDrafts -PauseBusySearch`, then `python -B tools/alpha-03/analyze_app_benchmarks.py <folder> --stage draft --include-more-drafts --write-docs --report-suffix <fresh suffix>` after timing. No case is copied/adapted; `--extra-cases` preserves each original folder's relative inheritance. Before timing: Release build0 warnings/errors, all1034 tests and32 focused mocks pass. Required post-report commit checks use a separate .NET artifacts directory so the idle app can remain open, avoiding its DLL locks; their outcome is recorded in the handoff. All existing tests are retained.

## Fixed case list and coverage

The catalogue has21 types. Coverage here means a direct subject/request in the original notes, not a classifier prediction or a new annotation of evaluation cases. Eight primary types are represented;08 also explicitly seeks lease rescission. Incidental delay damages in loan files are not a standalone contract-damages scenario, and09 is the original unpaid-sale-price file rather than a distinct commercial-debt scenario.

| Order / original case ID | Partition | Direct coverage |
|---|---|---|
| 01-debt-arabic | development | loan-repayment |
| 02-debt-french-notes | robustness | loan-repayment |
| 03-rent-darija-notes | robustness | unpaid-rent |
| 04-company-defendant | development | sale-non-delivery |
| 05-real-property | development | sale-annulment-fraud |
| 06-tatliq | development | tatliq |
| 07-wages | development | unpaid-wages |
| 08-rent-eviction | development | unpaid-rent; explicit lease contract-rescission |
| 09-company-plaintiff | development | unpaid-sale-price |
| 10-niltamis | development | loan-repayment |
| 11-tort-woman | development | tort-damages |
| 12-app-run-18-oct | development | loan-repayment |

No distinct case covers: **abusive-dismissal, commercial-debt, commercial-lease-eviction, contract-damages, custody, custody-forfeiture, divorce-by-husband, eviction-occupier-without-title, khul, liability-for-things, nafaqa, visitation**. Results cannot establish drafting quality for these missing types. [Pinned files/order/hash manifest](../../../tools/alpha-03/DRAFT-STAGE-2.json).

## gemma3:4b source, loads and command

Source commit: `12aa7cc91f668ac626e00a05a80ae3af0d230c42`.

```text
"%USERPROFILE%\dz-legal-assistant\src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll" "draft" "--models" "gemma3:4b" "--out" "%USERPROFILE%\AppData\Local\Temp\dzlegal-alpha03-stage2-20261109-031349\draft-gemma3-4b" "--measurement-root" "%USERPROFILE%\AppData\Local\Temp\dzlegal-alpha03-stage2-20261109-031349\draft-gemma3-4b" "--endpoint" "http://127.0.0.1:11434" "--law-root" "%USERPROFILE%\dz-legal-assistant\data\law" "--data" "%USERPROFILE%\dz-legal-assistant\data\document-types" "--cases" "%USERPROFILE%\dz-legal-assistant\tests\drafting-cases" "--extra-cases" "%USERPROFILE%\dz-legal-assistant\tests\drafting-cases-more"
```

| Before case | Unload wall s | Empty warm-up wall s | Native load s |
|---:|---:|---:|---:|
| 1 | 0.025181 | 3.849521 | None |
| 11 | 0.012560 | 2.867724 | None |

## Original gemma3:4b harness summary

# Drafting bench

Rules v0.9.5, 12 cases, run 2026-11-09 03:19.

Automatic columns cannot see swapped roles or invented reasoning: read each output too.

Cases marked (robustness) have notes in Darija or French: formal Arabic is the supported input, so they show how the drafting copes, not its quality.

| model | case | run | total s | slowest section s | hard issues | number issues | stray Latin words | ungrounded terms | wrong law (misquoted or from memory) | wrong-law paragraphs removed | legal points | example copies removed | retried | fell back | formally complete | error |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| gemma3:4b | 01-debt-arabic | 1 | 40 | 40 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma3:4b | 02-debt-french-notes (robustness) | 1 | 40 | 40 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | NO | |
| gemma3:4b | 03-rent-darija-notes (robustness) | 1 | 31 | 31 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma3:4b | 04-company-defendant | 1 | 33 | 33 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 05-real-property | 1 | 33 | 33 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 06-tatliq | 1 | 29 | 29 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma3:4b | 07-wages | 1 | 37 | 37 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 08-rent-eviction | 1 | 40 | 40 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 09-company-plaintiff | 1 | 31 | 31 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 10-niltamis | 1 | 50 | 50 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 11-tort-woman | 1 | 36 | 36 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 12-app-run-18-oct | 1 | 63 | 63 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 1 | 0 | yes | |

## Where the time goes

Load = reading the model from disk (should be ~0 after the first call). Prompt read = how fast the model reads what we send; write = how fast it writes.

### gemma3:4b

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds-argued | 10 | 0.0 | 1204 | 44.2 | 116 | 10.1 | 27.8 | 0 |


## gemma4:e4b source, loads and command

Source commit: `12aa7cc91f668ac626e00a05a80ae3af0d230c42`.

```text
"%USERPROFILE%\dz-legal-assistant\src\DzLegal.Harness\bin\Release\net10.0\DzLegal.Harness.dll" "draft" "--models" "gemma4:e4b" "--out" "%USERPROFILE%\AppData\Local\Temp\dzlegal-alpha03-stage2-20261109-031349\draft-gemma4-e4b" "--measurement-root" "%USERPROFILE%\AppData\Local\Temp\dzlegal-alpha03-stage2-20261109-031349\draft-gemma4-e4b" "--endpoint" "http://127.0.0.1:11434" "--law-root" "%USERPROFILE%\dz-legal-assistant\data\law" "--data" "%USERPROFILE%\dz-legal-assistant\data\document-types" "--cases" "%USERPROFILE%\dz-legal-assistant\tests\drafting-cases" "--extra-cases" "%USERPROFILE%\dz-legal-assistant\tests\drafting-cases-more" "--think" "false"
```

| Before case | Unload wall s | Empty warm-up wall s | Native load s |
|---:|---:|---:|---:|
| 1 | 0.023411 | 8.444658 | None |
| 11 | 0.007859 | 7.429685 | None |

## Original gemma4:e4b harness summary

# Drafting bench

Rules v0.9.5, 12 cases, run 2026-11-09 03:14.

Automatic columns cannot see swapped roles or invented reasoning: read each output too.

Cases marked (robustness) have notes in Darija or French: formal Arabic is the supported input, so they show how the drafting copes, not its quality.

| model | case | run | total s | slowest section s | hard issues | number issues | stray Latin words | ungrounded terms | wrong law (misquoted or from memory) | wrong-law paragraphs removed | legal points | example copies removed | retried | fell back | formally complete | error |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| gemma4:e4b | 01-debt-arabic | 1 | 29 | 28 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 02-debt-french-notes (robustness) | 1 | 27 | 27 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | NO | |
| gemma4:e4b | 03-rent-darija-notes (robustness) | 1 | 18 | 18 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 04-company-defendant | 1 | 19 | 19 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 05-real-property | 1 | 19 | 19 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 06-tatliq | 1 | 19 | 19 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 07-wages | 1 | 21 | 21 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 08-rent-eviction | 1 | 25 | 25 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 09-company-plaintiff | 1 | 23 | 23 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 10-niltamis | 1 | 37 | 37 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 11-tort-woman | 1 | 22 | 22 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma4:e4b | 12-app-run-18-oct | 1 | 44 | 44 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | yes | |

## Where the time goes

Load = reading the model from disk (should be ~0 after the first call). Prompt read = how fast the model reads what we send; write = how fast it writes.

### gemma4:e4b

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds-argued | 12 | 0.0 | 1176 | 70.5 | 111 | 13.1 | 16.7 | 0 |
