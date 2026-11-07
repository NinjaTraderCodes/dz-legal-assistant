# Alpha-03 gate 2 — fresh cost and order check

7 November 2026, after moving the agent to standalone PowerShell and closing VS Code. Branch `codex/alpha-03`; unchanged harness/reader commit `5dc52cbf24b670f6f9dad39d294676e29bd2b1ff`. Measurement scripts are committed with this report; their executed byte hashes are below.

**The original-development time/context gate passes on shape (b).** All 40 forward chains completed, asking every catalogue event question: 147 questions. Mean chain cost was **11.358 s**. Adding the measured unchanged gemma3 Alpha-01 batch cost for the retained role/value/request pass gives a **24.532 s/case projected mean**, below 90 seconds. Largest scored prompt: **571 tokens**, below 4,096. This is a projection from separately measured components; no integrated candidate reader has been built or timed.

**Order sensitivity and growing resident memory remain unresolved.** Reversal changed 6/17 generated statuses and 8/17 four-score argmaxes. The separate resident Alpha-01 batches also grew substantially in memory. Neither cost viability nor complete score availability establishes semantic accuracy or long-session memory stability.

## Conditions and protocol

**Power for every timed run: mains, Best performance.** Actual, effective and configured AC mode GUIDs all remained `ded574b5-45a0-4f42-8737-46345c09c238`; no sampled violation. The underlying Balanced base plan remained active separately. Heavy applications and VS Code were closed; no other model benchmark, build or test ran concurrently. Ordinary desktop/agent activity remained observable: the recorded other-process mean was about 0.94 core equivalents during case phases on 12 logical processors. That counter excludes model processes, Idle, System and PowerShell; PowerShell CPU is recorded separately. This is measured laptop operation, not an otherwise idle operating system.

Windows 11 build 26100; Intel Core i5-1335U, 10 cores / 12 logical processors; 16,836,857,856 visible physical bytes; .NET SDK 10.0.403; Ollama 0.40.15. An idle desktop app remained resident throughout measurement. The app, monitor and benchmark models were stopped/unloaded before analysis and build/tests. Best performance was retained.

Only original development inputs were used. The selection manifest has 40 in-catalogue cases and 147 event questions, with no gold labels in requests. Every case starts after explicit unload and an empty CPU/context-matched warm-up. That loading bill is recorded separately. This is runner-cold loading with the operating-system file cache available, not a cold-boot disk experiment.

The accepted gate-1 `b-1-request.json` supplies unchanged status definitions and settings: gemma3:4b, CPU (`num_gpu: 0`), context 4,096, temperature 0.2, repeat penalty 1.1, seed 0, one generated token, integer enum `[1,2,3,4]`, `stream: false`, `logprobs: true`, `top_logprobs: 20`, keep-alive 15 minutes. Each extension appends the actual generated answer verbatim and the next catalogue question. Roles, binding and requests remain in the retained Alpha-01 pass; their measured cost is included in the projection, rather than treating the status chain as a complete reader.

Commands, from the repository root, with RemoteSigned scoped only to the executing PowerShell process:

```powershell
& tools/alpha-03/Measure-OrderedChoices.ps1 -RunRoot $runRoot -FixturePath $acceptedGate1Fixture
& tools/alpha-03/Measure-Alpha01.ps1 -RunRoot $runRoot
```

The README describes input preparation, idle-app residence and resource sampling. Raw output directory: `%TEMP%\dzlegal-alpha03-gate2-clean-20261107-165004`. The earlier interrupted directory is retained separately and contributes no qualifying timing observations.

## Complete forward-case cost

| Component / projection | Mean s | Median s | p95 s | Maximum s |
|---|---:|---:|---:|---:|
| All event questions, 40 fresh chains | 11.358 | 11.271 | 14.776 | 15.336 |
| Separate empty runner warm-up | 3.082 | 3.116 | 3.228 | 3.310 |
| Retained unchanged Alpha-01, gemma3 (rounded case rows) | 13.125 | 13 | 18 | 22 |
| Sum by case, using those rounded baseline rows | 24.483 | 24.705 | 31.143 | 33.053 |

Percentiles use nearest rank; medians average the two middle values. Alpha-01's per-case rows are rounded to seconds, so its row-derived statistics and their sums are approximate. Its original bench mean is 13 s/case. The full gemma3 process, including setup/report overhead, took **526.951 s / 40 = 13.174 s/case**; adding 11.358 gives the conservative batch-based projection **24.532 s/case**. All cases, including the two semantically failed baseline cases, contribute their time.

The 3.082 s runner warm-up is outside chain timing. Adding that observed startup cost would give a planning figure near 27.6 s/case if a reset boundary were adopted, but that is not an integrated measurement or a chosen runtime policy. There is no retry, calibration, evidence/binding extension or new-reader measurement in these totals. Existing Alpha-01 runs retain the server's natural across-case cache behaviour; baseline cached-token counters are not exposed by its current timings interface.

### Shorter and longer original notes

These are the shorter and longer halves by original `notes.facts` length (median 99.5 characters), not newly written cases or a realistic long-document stress test. Claim/question mix differs between halves.

| Group | Cases | Note characters | Event questions | Mean chain s | Mean retained baseline s, rounded rows | Mean projected sum s |
|---|---:|---|---:|---:|---:|---:|
| Shorter | 20 | 62–99 | 69 | 11.097 | 11.950 | 23.047 |
| Longer | 20 | 100–172 | 78 | 11.619 | 14.300 | 25.919 |

The original set does not establish cost on substantially longer lawyer notes, nor on dedicated requests/documents fields. Those still require measurement after the authorised case freeze and eventual implementation.

## Cache and score contract

- **164/164** status responses, including reversal, have one output token, correct digit bytes and all four finite alternatives.
- **43/43** first requests have zero cached tokens.
- **121/121** extensions reuse tokens, range **353–529** (mean 424.405).
- Forward prompt-token total 64,320, cached total 44,941; thus 19,379 uncached tokens by these counters. Prompt evaluation total 449.653 s. Do not divide all prompt tokens by fresh-evaluation time and call that fresh throughput.
- The independent artifact audit checked every request's settings, all 121 exact history extensions and actual-answer preservation, and equality of the first `loan-basic` prompt with the accepted fixture. **Zero mismatches.**

Single-token requests end at their intentional one-token cap. This differs from a truncated multi-line Alpha-01 reading, which the unchanged provider rejects. Missing alternatives were never assigned fabricated zero or negative-infinity scores. No labels, offsets or thresholds were fitted.

## Preselected reverse-order check

| Case | Questions | Generated statuses changed | Four-score argmaxes changed | Reverse chain s |
|---|---:|---:|---:|---:|
| `loan-no-section` | 6 | 3 | 3 | 14.802 |
| `loan-basic` | 6 | 2 | 3 | 16.982 |
| `non-delivery-machines` | 5 | 1 | 2 | 15.688 |
| Total | 17 | **6** | **8** | — |

For example, `loan-basic`'s `deadline_passed` changed from generated/argmax `disputed` to `denied`; its `payment_made` generated digit remained `absent`, while its four-score argmax changed from `absent` to `stated`. These are observations, not adjudicated correct answers. Actual generated history is used in both orders.

Later questions see earlier answers, and scores move as well as sampled digits. Calibration would have to work on that conditional distribution. This small paired check has no repeated same-order control, so it cannot isolate every change from sampling/runtime variation or estimate population error rates. It is sufficient to retain answer-history/order sensitivity as a material design risk. No wording was repaired from these outputs.

## Memory

For forward/reverse case phases, sampled model-server/runner peak was **4.082 GiB**, simultaneous model-plus-app peak **4.260 GiB**, and minimum whole-machine available physical memory **5.323 GiB**. App residence at the model peak was 181.5 MiB. These reset-per-case chains fit the measured machine with headroom.

The separate **resident** Alpha-01 batches are important additional evidence:

| Reference | Model/server sampled peak GiB | Simultaneous model + app peak GiB | Minimum available physical GiB | Early / late model means GiB |
|---|---:|---:|---:|---:|
| gemma3:4b | 7.852 | 7.954 | 1.808 | 4.001 / 7.377 |
| gemma4:e4b | 9.942 | 10.042 | 0.396 | 6.607 / 9.840 |

Early means use seconds 20–80 of each batch; late means use its last 60 seconds. Each resident batch used one runner PID throughout, so these observations are not explained by summing two successive runner instances. The cause and eventual plateau of the growth were not investigated. Do not equate the reset-chain peak with a persistent integrated reader's memory requirement. Gemma4 had little spare physical memory near the end.

The approximately two-second sampler reported no errors. Updated approximately ten-second paging samples had median page-read rate zero and no observed page outputs in each phase, with read spikes (including loads); this does not establish absence of all paging. Maximum commit reached 16.406 GiB in the gemma3 resident batch and 18.996 GiB in gemma4, both above the approximately 15.68 GiB visible physical RAM. Private bytes are not resident bytes; working-set sums may double-count shared pages; sampled peaks can miss spikes. Long-session/integrated memory qualification remains pending.

## Provenance and stop

Installed licences/digests and complete reference results are in the two rung-0 summaries. No model assets were bundled, downloaded or switched into the app. The measured harness source, existing prompts/catalogue, original cases and tests are unchanged. Validation checks: **0**. Hidden/evaluation data accessed: **none**. New cases: **0**.

| Artifact | SHA-256 |
|---|---|
| Original development selection manifest | `9ff1cda372f8c9846d918b2ebc7b86945f93084a1536d11e1ab8dd1153f0581c` |
| Numbered development inputs | `6436ed220d1c78c49231e318cc55fca7e760c1be3c766c296aeb65a71c2ee325` |
| `calls.jsonl` | `214c529ab5ea191523dfa11787dcb3deed1796c070bc5df977d1c70eaab3d454` |
| `gate2-results.json` | `54e4c9268dce93dabadded900d4316566cb5f42f76589e6aaa7fe02d4a09e6bc` |
| `wire-audit.json` | `118f7263a4db62cbd866673ad803d2db94ad7d67f4306a4ad7d9ec87bacbe7e4` |
| `order-check.json` | `3230ca1105e018566d99ec6432ebd724fa9014d323093c4fb212501157f19fd9` |
| `resources.jsonl` | `d2be761a010e1e23a28eb11ab05743efff3179e9b5518e58f7f2ad3179e64e2f` |
| `memory-growth.json` | `64a3f0228bf1e9ed3735ed28feb0a921de9adfb77be46495f550b8b89726d74f` |
| Executed `Measure-OrderedChoices.ps1` | `17d153f082c38ec824fbe7134e4c658dfc1ce8ac70486fb8460972672130f61d` |
| Executed `Measure-Alpha01.ps1` | `8c8ef619245d965ab50ad109cb451b877e6e602f41ec3b4044594d942302602a` |
| Executed `Measure-Resources.ps1` | `cc8167a8a00e1a96b36c3619645ce8766d92bf2c1f7a4f75f810adb51dc78e5c` |
| Executed `Power.ps1` | `8c851654f6b722fcebfd66cc88126dce600878b62d40769a7b4df9711dce0894` |

**Stopped after the authorised cost/order/rung-0 measurements, before case writing.** No candidate accuracy, calibration, 144-case freeze or hidden-v2 readiness is claimed. Required post-commit Release build/test results are reported in the handoff.
