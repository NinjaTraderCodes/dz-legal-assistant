# Alpha-03 rung 0: gemma4:e4b, thinking off

**Unchanged Alpha-01 with `Think = false`: 39/40 original development cases without a critical error; zero failures or cap hits.** Mean exact case time **8.793 seconds**; sampled model/server peak **7.813 GiB**. This is a reference configuration, not a runtime-model switch or an unseen-case result.

## Protocol and power

7 November 2026, source commit `e62ff1ca3bbb87c5c84be57bb214e09dbebcd742`, with the new measurement host under `tools/alpha-03/ModelCheck`. The host directly calls the existing `AlphaFacts.Reader`, `ClaimFactReader`, `OllamaModelProvider` and `FactsEvaluator`. No production source, parser, prompt, catalogue, answer key or case was changed. The same 40 original in-catalogue development cases ran serially, claims given. Four out-of-catalogue cases were excluded before inference. No validation or hidden cases were run.

The only model-setting change from the prior gemma4 reference is **the existing provider's `Think = false`**. Temperature 0.2, repeat penalty 1.1, context 4096, CPU-only, keep-alive 15 minutes and reader cap **300 tokens** remain unchanged. The request recorder and independent post-run audit verified `think: false`, streaming, `num_gpu: 0`, `num_ctx: 4096`, `num_predict: 300` on all **40 wire requests**. Responses were streamed through the normal provider; instrumentation did not buffer or replace the stream.

The bounded plan was 40 cases, with a stop on the first cap or technical failure and no higher cap. Actual: **40 attempted, 0 unattempted**, 0 cap hits, 0 technical failures, answers **17–56 tokens**. No retry or extra configuration followed.

**Mains power; Best performance throughout.** Actual, effective and configured AC mode GUIDs were `ded574b5-45a0-4f42-8737-46345c09c238`; **167 resource samples, zero violations**. Balanced base plan recorded separately. An idle real desktop app remained resident for the entire batch. Build servers were shut down before inference; no build, test or other model benchmark ran concurrently.

After mains was connected, Windows Search became persistently busy. Timing was delayed. At the owner's explicit request, `WSearch` was temporarily stopped; its Automatic startup setting was unchanged. Quiet preflight samples preceded the run. Sampled other-process CPU averaged **0.365 core equivalents**, maximum **1.683**, a brief OS/WMI/agent spike; no Search process appeared in the retained top-process samples during inference. Search was restored to its original Running/Automatic state after the run. The owned idle app closed normally, both probe models were unloaded and Best performance was retained. Before/after service and cleanup records remain external.

## Model and environment

- Ollama **0.40.15**, loopback `127.0.0.1:11434`.
- Installed `gemma4:e4b` digest **`dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`**, nominal metadata 7.5B / Q4_K_M, **6,583,656,505 disk bytes**. Local `/api/show` licence: **Apache License 2.0**. No weights are redistributed.
- Windows 11 build 26100; i5-1335U, 10 cores / 12 logical processors; **15.681 GiB visible physical RAM**; .NET SDK 10.0.403.
- Measurement binary SHA-256 **`bdbb4d51763f8e41e1f20096e0cc848688513d88fb2803956b5d2bad43618f8f`**. Its source was built with 0 warnings/errors before inference.

## Existing evaluator result

| Partition | Cases | Critical cases | Position | Decisive events | Date binding | Polarity | Requests | Failures | Bench rounded s/case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 1 | 100% | 100% | 100% | 98% | 93% | 0 | 9 |

Critical occurrences: **polarity 1** — `annulment-fraud-land`, `concealment: denied instead of affirmed`. All other existing critical error types: **0 observed**. No answer-key change was made.

Other differences, using the evaluator's existing classification: **unreadable lines 6** (six case-level occurrences, eight individual lines) and **missing request 2** (`divorce` in `divorce-husband`; `reinstatement` in `dismissal`). Unreadable-line cases: `rescission-sale`, `contract-damages`, `divorce-husband`, `dismissal`, `dismissal-grave-fault`, `non-delivery-machines`. These remain disclosed even though the primary 39/40 score counts critical errors only.

The old development/robustness gold events are all affirmed. This 98% polarity column therefore does **not** demonstrate denied/disputed competence, balanced classification or full extraction completeness. The old sparse key also does not score every unlabelled event prediction. No calibration, threshold fitting or candidate selection used these answers.

The earlier gemma4 reference with thinking unset had **40/40 length failures**, versus this check's 40 usable readings below the cap. This supports the thinking-off configuration as a usable reference under the unchanged cap. Wire verification establishes the switch; the older failure stream did not preserve its thinking channel, so no claim is made to have measured that channel directly. The earlier unchanged gemma3 reference was 38/40 without a critical error. This small all-affirmed development comparison does not establish a general model ranking.

## Time and memory

| Exact case-time statistic | Seconds |
|---|---:|
| Mean | 8.793 |
| Median | 8.762 |
| Nearest-rank p95 | 11.945 |
| Maximum, cold first case | 21.866 |
| Full batch loop | 351.837 |
| Full host process invocation | 352.823 |
| Invocation / 40 cases | 8.821 |

The first case includes **8.652 seconds** native model loading. Later cases retain the normal resident model/cache behaviour; no per-case reset was introduced. Existing native table: 40 calls; mean 719 prompt tokens, 40 answer tokens; aggregate prompt rate 127.2 tokens/s, write rate 13.8 tokens/s; mean first-word time 5.9 seconds; zero prompt-truncation flags. No substantially long-document stress test or integrated Alpha-03 reader is represented.

| Sampled memory measurement | GiB |
|---|---:|
| Model/server working-set peak | **7.813** |
| Simultaneous model + app working-set peak | **7.990** |
| Model/server private-byte peak | 8.542 |
| Minimum machine available physical memory | **3.215** |
| Maximum machine commit | 13.854 |

One runner PID, `17268`, was observed. Working-set sums may count shared pages twice and sampling can miss short peaks. Thirty-four updated paging observations had median page-read rate zero, maximum 1,219 reads/s, maximum 255,696 pages input/s, and zero observed page outputs. Read spikes include model loading; these samples do not certify absence of all paging or long-session stability. Background residence/free memory differ from the earlier batches, so these are this run's measurements, not a controlled attribution of memory differences. The separate resident-gemma3 issue was not investigated.

## Reproduction and artifacts

Build before timing, shut down build servers, establish strict mains/Best performance and app/resource residence, then:

```powershell
& tools/alpha-03/Measure-ThinkOff.ps1 -RunRoot $runRoot
& tools/alpha-03/Analyze-ThinkOff.ps1 -RunRoot $runRoot
```

The wrapper invokes:

```powershell
dotnet tools/alpha-03/ModelCheck/bin/Release/net10.0/ModelCheck.dll tests/fact-cases data/law $externalOutput
```

Raw directory: `%TEMP%\dzlegal-alpha03-blind-followup-20261107-191514\rung0-gemma4-e4b-think-off`; resource, power, installed-model metadata and cleanup records are in its parent. Raw case answers and requests remain external. No case data was copied into a new development set.

Unchanged fingerprints: prompt `60e6f29368b4e77711f89697281137a5dc760f54133c4f85694ea7bc6dc68154`; elements `aa40f534266d6a5cb9f76b2de463b757005e8a861a4271b577964359ae1cfe33`; original case folder `425da3236a75bffbfbbbee07c076c4c9b5fddb71a899a073ef2c19b6ce712f18`.

| External artifact | SHA-256 |
|---|---|
| `summary.md` | `650efffaedd517cecd6e49f87d22c267a1a5127a3e9c74dcc68ab18513f1c7ee` |
| `manifest.json` | `fc1834668664a4188c1bdc6786ab0b34f46dbab78ae810ebf4a87ae030b2f811` |
| `case-results.json` | `01e73e34fcd6b27b9f948935acb1f5d02c085680e0e19d4be04f88a2f26e227a` |
| `audit.json` | `3710f95d76bf7e34d2557e60be14af2b5b28b8efa6bb80ddb5d5a39bd990cbd4` |
| Parent `resources.jsonl` | `a480d843ba38f23694d7d0a18dc0f96f5ebedcf9e40284ede270837b0c04a86c` |

No production/test/prompt changes, validation model checks, hidden-data access or new case writing. Stopped after the authorised three-part follow-up.
