# Alpha-01 hidden-v2 summary comparison

**Both phone runs completed once, with exit0. Gemma4:e4b keeps the strongest reported accuracy; gemma4:e2b is the fastest and smallest in sampled memory.** The two phone columns have41/90 and42/90 cases without a critical error, versus58/90 for e4b and39/90 for the original gemma3. Their requests-found rates are only63% and69%, versus95% for e4b and92% for gemma3; missing-request differences rise to65/54 from9/14. The small2/3-case gains over gemma3 in the critical-error-free column therefore do not establish an overall quality improvement. All measurements are on this Windows laptop, not phone hardware. No reader, scoring rule, case, prompt, runtime setting or model default is changed after viewing these summaries.

Owner-authorised blind runs, claim type given. Only the four exact summary.md files were read; no input folder, case output, console or supporting evaluation file was opened or listed. Aggregate fields below are copied/derived only from those summaries; original case-level lines are not reproduced in this report.

Mains / **Best performance**, idle template-mode app, closed browsers/VS Code, serial models, ten-case reload and **512 MiB** guard. CPU, context4096, temperature0.2, repeat penalty1.1 and300-token cap; unchanged Alpha-01 prompt/parser/scorer. Gemma4 think=false, gemma3/gemma3n thinking omitted. Prior4B thinking values come from the frozen owner commands; new phone summaries also record the value explicitly. No runtime-model switch.

Cases without a critical error are the scorer's green cases; this does not mean every dense field is identical. Cases outside the given claim catalogue abstain and are excluded from the90-case accuracy table. Scorer seconds/case are rounded to whole seconds and refer to read cases; amortised batch time includes all attempted files, loads, waits and cleanup, and is rounded to milliseconds. No unrounded value is inferred from per-case lines.

## Frozen identities, timing and cleanup

Pre-inference freeze **5ffc12c**, pushed after Release build0warnings/errors and all1034 tests. [PHONE-MODELS.json](../../../tools/alpha-03/PHONE-MODELS.json) was recorded before any phone call, SHA256 `ba783d092f5d8ef11d177ee84a690052245c810b76796ab151a765d1e8317cda`. The checked harness's code/runtime binaries were copied to the wrapper's normal output folder and hash-matched before timing; no case/data/app file was copied in that deployment. A benign whole-console redirection fixture passed with zero model calls. The source/binary identity record is outside evaluation outputs in the task's download-metadata folder.

| Model | Digest | Licence | Installed bytes | Think actually sent |
|---|---|---|---:|---|
| gemma4:e2b | `b37049369adfe3d2b653af0ab301a062ca5cbe96ace6aa0d3f2559ee9b563fc2` | Apache2.0 | 4,587,156,505 | false |
| gemma3n:e2b | `dae166dc6a5e93061da12b75b887ce7c72cb91785d547b88c7503047f8411ffd` | Gemma Terms of Use | 7,836,324,540 | omitted |

| Run | Start UTC,9Nov | End UTC,9Nov | Native wrapper exit |
|---|---|---|---:|
| gemma4:e2b | 14:29:55.4649521 | 14:42:14.3025971 | 0 |
| gemma3n:e2b | 14:42:19.3470322 | 15:12:42.2507829 | 0 |

The same owner wrapper ran serially with each whole console redirected to its sibling file and unread; both outputs were fresh and neither run was retried. Before timing, the owner reopened and confirmed the app idle in template mode; the known startup-resident gemma3 was unloaded under the owner's earlier instruction, before any timing. The app remains open. Search measured0 cores in both preflights and stayed Running/Auto; no service/startup setting changed. Temporary awake state was restored at15:12:42.2606755UTC. Ollama was empty after both runs. All **1193 new timed power/headroom samples** are valid; both native context-flag columns are0. No build/test or other model job ran during either timed window.

Pulls were completed serially before timing, both exit0: gemma4:e2b02:35:40.8727933–04:40:08.9432455UTC, then gemma3n:e2b04:40:08.9559138–06:55:18.7155562UTC. Download time, the earlier progress-display reset and the later app-reopen stop are excluded from case/batch timings. Setup, launch, binary and cleanup metadata are retained under `%TEMP%\dzlegal-stage3-downloads-20261109-033540`, separate from evaluation outputs. No contents of any evaluation metadata file or console were read by the agent. Historical4B columns below are the original8Nov summaries, not new repeats; there is one pass per phone model and no estimate of run-to-run or cross-session uncertainty.

## Each phone run

**gemma4:e2b, thinking off:** 100attempted files,90read and10out-of-catalogue abstentions;49cases with a critical error, **41/90 without one**. Position93%, decisive events93%, dates89%, polarity85%, requests63%, runtime failures0. Read-case mean **7s** (rounded); full batch719.421s, amortised **7.194s** per attempted file,10loads. Model/server working-set/private peaks **4.844/5.537GiB**, concurrent model+app5.094GiB, available minimum6.003GiB;340samples,0invalid. Critical counts: polarity34, decisive omissions10, forbidden readings9, position6, date binding6, amount binding5, actor swaps3, request amount0. Other differences: missing requests65, unreadable lines36, missing events27, missing dates11, missing amounts6, relation3. Native90calls: average load0.0s(rounded), prompt1085tokens, prompt read249.7tok/s, answer54tokens, write19.7tok/s, first word4.4s, context flags0. Warm loads are included in the batch bill, not inferred to be free from the rounded per-call load field.

**gemma3n:e2b, thinking omitted:** 100attempted files,90read and10out-of-catalogue abstentions;48cases with a critical error, **42/90 without one**. Position97%, decisive events93%, dates77%, polarity86%, requests69%, runtime failures0. Read-case mean **19s** (rounded); full batch1808.142s, amortised **18.081s** per attempted file,10loads. Model/server working-set/private peaks **6.674/7.574GiB**, concurrent model+app6.858GiB, available minimum4.537GiB;853samples,0invalid. Critical counts: polarity34, decisive omissions9, forbidden readings9, amount binding7, date binding7, position3, actor swaps2, request amount2. Other differences: missing requests54, unreadable lines40, missing dates28, missing events24, missing amounts7, relation2. Native90calls: average load0.0s(rounded), prompt1089tokens, prompt read69.1tok/s, answer48tokens, write14.1tok/s, first word15.8s, context flags0.

## All four models, every aggregate field

| Measure | gemma3:4b | gemma4:e4b | gemma4:e2b | gemma3n:e2b |
|---|---:|---:|---:|---:|
| thinkSent | omitted | false | false | omitted |
| attemptedFiles | 100 | 100 | 100 | 100 |
| casesRead | 90 | 90 | 90 | 90 |
| casesWithCriticalError | 51 | 32 | 49 | 48 |
| casesWithoutCriticalError | 39 | 58 | 41 | 42 |
| outOfCatalogueAbstained | 10 | 10 | 10 | 10 |
| plaintiffPosition | 94 % | 100 % | 93 % | 97 % |
| decisiveEventsFound | 89 % | 98 % | 93 % | 93 % |
| datesOnRightEvent | 83 % | 92 % | 89 % | 77 % |
| polarityRight | 86 % | 91 % | 85 % | 86 % |
| requestsFound | 92 % | 95 % | 63 % | 69 % |
| failures | 0 | 0 | 0 | 0 |
| caseSecondsRounded | 28 | 21 | 7 | 19 |
| batchSeconds | 2591.863 | 2001.927 | 719.421 | 1808.142 |
| amortisedSeconds | 25.919 | 20.019 | 7.194 | 18.081 |
| loads | 10 | 10 | 10 | 10 |
| modelWorkingSetPeakGiB | 6.237 | 7.244 | 4.844 | 6.674 |
| modelPrivatePeakGiB | 6.62 | 7.968 | 5.537 | 7.574 |
| modelAppPeakGiB | 6.484 | 7.424 | 5.094 | 6.858 |
| availableMinimumGiB | 3.684 | 3.684 | 6.003 | 4.537 |
| resourceSamples | 1219 | 944 | 340 | 853 |
| invalidPowerHeadroomSamples | 0 | 0 | 0 | 0 |
| criticalErrors: actor swapped | 2 | 0 | 3 | 2 |
| criticalErrors: amount on wrong event | 10 | 4 | 5 | 7 |
| criticalErrors: date on wrong event | 7 | 4 | 6 | 7 |
| criticalErrors: forbidden reading | 11 | 4 | 9 | 9 |
| criticalErrors: missing decisive event | 15 | 3 | 10 | 9 |
| criticalErrors: plaintiff position | 5 | 0 | 6 | 3 |
| criticalErrors: polarity | 31 | 24 | 34 | 34 |
| criticalErrors: request amount | 2 | 0 | 0 | 2 |
| otherDifferences: amount not given | 10 | 6 | 6 | 7 |
| otherDifferences: date not given | 18 | 8 | 11 | 28 |
| otherDifferences: missing event | 30 | 14 | 27 | 24 |
| otherDifferences: missing request | 14 | 9 | 65 | 54 |
| otherDifferences: relation | 2 | 0 | 3 | 2 |
| otherDifferences: unreadable lines | 3 | 11 | 36 | 40 |
| nativeTiming: calls | 90 | 90 | 90 | 90 |
| nativeTiming: loadSecondsAverageRounded | 0.0 | 0.0 | 0.0 | 0.0 |
| nativeTiming: promptTokensAverage | 1089 | 1085 | 1085 | 1089 |
| nativeTiming: promptTokensPerSecond | 47.1 | 63.1 | 249.7 | 69.1 |
| nativeTiming: answerTokensAverage | 48 | 43 | 54 | 48 |
| nativeTiming: writeTokensPerSecond | 9.7 | 11.0 | 19.7 | 14.1 |
| nativeTiming: firstWordSeconds | 23.2 | 17.2 | 4.4 | 15.8 |
| nativeTiming: contextFlags | 0 | 0 | 0 | 0 |

Error-category zero means absent from the original scorer's nonzero-count list. The summaries do not provide strict all-fields-right counts, unrounded scorer means, dense status confusion matrices, sampled background CPU distributions or per-load timings; none is fabricated. Resource peaks are sampled and can miss short spikes. No case-level analysis/tuning or retry follows this comparison.

Per-answer cap counts and exact input-fingerprint equivalence across sessions are also not established by these summaries; the source input path/protocol was owner-fixed and no supporting evaluation file was opened to extend the evidence. The original300-token cap remains in force. No accuracy pass mark, production-model switch or legal completeness conclusion follows from this comparison. The owner can review the two full permitted summaries locally: [gemma4:e2b summary](C:/Users/TheShadow/AppData/Local/DzLegalAssistant/alpha-03-owner-runs/hidden-v2-alpha01-gemma4-e2b-think-off-2026-11-09/summary.md) and [gemma3n:e2b summary](C:/Users/TheShadow/AppData/Local/DzLegalAssistant/alpha-03-owner-runs/hidden-v2-alpha01-gemma3n-e2b-2026-11-09/summary.md). The report shares their aggregate fields and hashes, not their case-level lines.

Stage2 and its24 copied fictitious draft reports are already pushed: [comparison](../2026-11-09-alpha-03-app-draft-stage-2/summary.md), [side-by-side files](../2026-11-09-alpha-03-app-draft-stage-2/outputs/). Stop after this Stage3 report's commit, required checks and push; no further inference or validation check is authorised by this task.

## Permitted source summaries

- gemma3:4b: `C:\Users\TheShadow\AppData\Local\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma3\summary.md`; SHA256 `2fc5614cb7e7b74bb215628699ddfeb82962e6b5efec5fb1d76670271c7533ef`.
- gemma4:e4b: `C:\Users\TheShadow\AppData\Local\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma4-think-off\summary.md`; SHA256 `087a2e2a722a5d2d164cc711f6fd6fe17940eb9bbcc5a0d68155d82ab32b9e25`.
- gemma4:e2b: `C:\Users\TheShadow\AppData\Local\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma4-e2b-think-off-2026-11-09\summary.md`; SHA256 `304e892de3fd421e97b07bf91bf4d790b8826dd8d2315974548ee43113b8bc81`.
- gemma3n:e2b: `C:\Users\TheShadow\AppData\Local\DzLegalAssistant\alpha-03-owner-runs\hidden-v2-alpha01-gemma3n-e2b-2026-11-09\summary.md`; SHA256 `377d96bf2d69f649ac321cdbf7922eca1687d578b2c4cf21291220a6276eb800`.
