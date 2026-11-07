# Alpha-03 fitting references: operational stop

**Stopped before request 26 of the first 96-case reference batch.** Gemma3 completed 25 fitting cases; the next request was blocked by the declared available-memory guard. Gemma4, candidate scoring/calibration/binding, compatibility checks, audit and validation did not run. This is an incomplete reference measurement, not a candidate quality-gate failure or a complete model comparison.

The corpus remains at the owner-reviewed second freeze, content hash `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642`. Production source, catalogue, prompts, original cases and frozen case payload are unchanged. Source base: `e539b8e13c075974ca8c778c90b6e88b6905be99`; the new measurement host was built and fingerprinted before inference.

## Authorisation and protocol

References were required first: unchanged Alpha-01 with given claims on the 96 fitting members, separately reporting polarity/binding kinds, gemma3 and gemma4 thinking off. The host calls the existing reader/provider/parser/evaluator directly. It selects fitting manifest IDs before case deserialisation and never sends audit cases. Each model starts unloaded, then stays resident across cases, with no per-case reset or retry. Context 4096, CPU only, temperature 0.2, repeat penalty 1.1, 300 answer tokens. Gemma3 omits `think` because it is a non-thinking model; gemma4 would use provider `Think = false`.

Before HTTP, the recorder checks the wire shape and a resource sample no more than ten seconds old. Mains, actual/effective/configured Best performance, a resident real app, available physical memory at least 512 MiB, and model private bytes plus app working set no larger than physical RAM are required. The 512 MiB guard is a conservative operational interruption rule, not the brief's fixed capacity pass mark or a measurement of maximum feasible allocation.

Owner clarifications: a passing model may continue after the other fails a score/binding **quality** gate, but mandatory runtime/capability preconditions still stop the stage. Final audit is one locked two-model batch with a primary selected from fitting and recorded beforehand. Only that primary is gated; the secondary cannot replace it after audit. No primary has been selected because fitting has not begun.

## Machine, power and stop evidence

- Windows build 26100, 12 logical processors, visible physical RAM 16,836,857,856 bytes (15.681 GiB), Ollama 0.40.15 on loopback.
- **Mains and Best performance throughout:** all 244 samples in the reference window have AC=1 and actual/effective/configured overlay `ded574b5-45a0-4f42-8737-46345c09c238`. Battery 100% at setup, 99% during inference. The manifest's Balanced *base scheme* is distinct from that verified overlay; the mode was not changed.
- Real desktop app resident throughout. Resource samples approximately every two seconds, fresh paging counters approximately every ten seconds. No build, test or other model run overlapped inference.
- Windows Search became busy in preflight, before any reference call. Running/Automatic was saved, then temporarily stopped under the owner's earlier explicit authorisation. **Restored Running/Automatic after interruption.** Models unloaded, owned app closed normally and owned monitor stopped. Other owner processes were untouched.
- Reference window **20:17:31.768–20:26:08.459 UTC**, batch 516.659 seconds. First call included a 4.595-second model load. No context-truncation flag or 300-token cap hit in 25 calls; maximum prompt 1,093 tokens, maximum answer 59.
- The last sample before request 26 was **20:26:07.443 UTC**, 532,328,448 bytes available (**507.668 MiB**, below 512 MiB). Monitor freshness, power/app and model-private-plus-app checks passed. Thus the failed condition was machine headroom. No HTTP request or native timing exists for `a03-026`.
- Whole-window minimum available **487,448,576 bytes (464.867 MiB)**. Sampled model/server working-set peak **7.499 GiB**, private-byte peak **7.887 GiB**, simultaneous model/server-plus-app working-set peak **7.669 GiB**. Whole-machine commit peak **18.480 GiB**. Working-set sums may double-count shared pages and samples miss short peaks; this does not show that the full batch fits.
- Background CPU mean **0.600**, median **0.474**, p95 **1.330**, maximum **3.357 core equivalents** per sampled interval; browser activity appeared among later peaks. Sampler PowerShell mean **0.022** cores. Paging observed: 49 fresh samples, peak page reads **2,167/s**, pages input **17,214/s**, sampled pages output **0/s**. File loading can also cause reads. The partial, memory-pressured batch does **not** qualify a full quiet cost measurement.

The separate resident-gemma3 growth issue was not investigated. No server upgrade, persistent setting change, cap increase, guard relaxation, per-case unload rescue or second-model fallback was performed.

## Partial reference results

The first 25 fitting cases in frozen file order ran, `a03-001`–`a03-025`: six complete polarity families and the first member of a seventh. **All are polarity-kind; no binding-kind case ran.** This is neither a representative 96-case score nor completed grouped cross-validation. The first 24 alone must not replace the disclosed 25-case denominator.

| Reference | Completed / planned | Without critical error | Binding-kind completed | Seconds/case mean / median / p95 / max | Sampled model/server WS peak |
|---|---:|---:|---:|---|---:|
| Alpha-01, gemma3:4b | 25 / 96 | 5 / 25 | 0 | 20.662 / 17.788 / 29.851 / 31.851 | 7.499 GiB |
| Alpha-01, gemma4:e4b, thinking off | 0 / 96 | unavailable | 0 | unavailable | unavailable |

Seconds include cold first load and observed pressure; later 24-call mean 20.396 seconds. These are informative partial timings, not a fresh cost-gate pass. p95 uses nearest rank. No rerun replaced a difficult or slow answer.

The raw evaluator summary contains 26 attempted evaluations, 21 with a critical failure, including the blocked request as one technical failure. The semantic table reports **25 actual calls and 20 critical cases**, keeping the blocked attempt separate. Seventy cases were never attempted by the evaluator; **71 cases received no model inference**. Successful cases: `a03-015`, `a03-016`, `a03-021`, `a03-023`, `a03-025`.

| Existing evaluator difference | Occurrences | Cases touched | Critical under unchanged scorer |
|---|---:|---:|---|
| Missing decisive event | 23 | 17 | yes |
| Forbidden affirmation | 5 | 4 | yes |
| Wrong polarity | 1 | 1 | yes |
| Date not given | 8 | 8 | no |
| Amount not given | 9 | 9 | no |
| Missing request | 1 | 1 | no |

Other critical types, including wrong attached dates/amounts, wrong requested amount, role/actor reversal and invented values: zero in this partial record. Counts overlap; do not add cases touched into a total. Missing values stay visible even when the original scorer does not make them critical.

Dense supplemental status decisions use original parsed facts and explicit numbered `absent`/`none` rows only; missing rows are never imputed absent:

| Gold status | Affirmed output | Denied output | Disputed output | Explicit absent output | Unresolved |
|---|---:|---:|---:|---:|---:|
| Affirmed | 70 | 0 | 0 | 3 | 0 |
| Denied | 0 | 0 | 0 | 18 | 0 |
| Disputed | 1 | 0 | 3 | 2 | 0 |
| Absent | 4 | 0 | 0 | 30 | 0 |

Status correctness **103/131**, non-affirmed errors **25/58**, false affirmations **5**. Target recall: affirmed **6/7**, denied **0/6**, disputed **3/6**, absent **6/6**. All 18 denied event labels became explicit absent. That is a coverage/polarity failure even though the legacy polarity statistic conditions on returned events and reports only one wrong polarity. Labels and scoring were not revised from these answers.

Known event dates **43/54 correct, 11 omitted, zero wrong returned**; amounts **32/42 correct, 10 omitted, zero wrong returned**. These dense known-value counts include omissions on dropped events, so differ from legacy conditional totals. Request presence **53/59 correct**, one missing and five extra requests; known request amounts **19/20 correct, one omitted**. Of 97 non-absent gold events, 66 returned some sentence citation and 53 overlapped an annotated event source. Citation overlap is a source diagnostic, not semantic verification; Alpha-01's sources come from value binding, with no new evidence selector.

Incomplete folds: fold 1 has 9 calls / 6 critical cases, fold 2 has 8 / 6, fold 3 has 8 / 8. They cannot select calibration or establish score-gate passage.

## Models and reproducibility

| Installed reference | Package licence metadata | Parameters / quantisation | Disk bytes | Digest |
|---|---|---|---:|---|
| gemma3:4b | Gemma Terms of Use | 4.3B / Q4_K_M | 3,349,593,375 | `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` |
| gemma4:e4b | Apache 2.0 | 7.5B / Q4_K_M | 6,583,656,505 | `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c` |

The second row records the installed package, not a result from this run. No weights were bundled and neither model becomes the runtime model from this evidence.

```powershell
dotnet build tools/alpha-03/FittingReferences/FittingReferences.csproj -c Release
# Shut build servers down, start idle app/resource monitor, verify power and load.
& tools/alpha-03/Measure-FittingReferences.ps1 -RunRoot $externalRunRoot
# After the stop, cleanup and monitor termination:
python -B tools/alpha-03/summarize_fitting_references.py $externalRunRoot
python -B tools/alpha-03/freeze_cases.py --verify
```

Raw root: `%TEMP%\dzlegal-alpha03-fitting-20261107-211459`. It holds 25 exact requests, all answers/facts/timings/errors, original-evaluator summaries, completion/manifest, pre-inference source/binary hashes, environment/model licence/digest metadata, resource samples and cleanup records. Raw outputs remain external. `reference-analysis.json` is the deterministic supplement; `artifact-hashes.json` fingerprints retained artifacts.

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `a54d92b93c4232f679c83dc4e9a7e7c9d4c6bdb543f7f497064a408edb51da3e` |
| `reference-gemma3-4b/case-results.json` | `7b3587de7e7753756397e11edd098b035363858fe18c3e4bdc460360acd50c61` |
| `reference-analysis.json` | `713f3d16884c5fb1537b05d403010a754a490667c703631577e6626dca1dcbe5` |
| `artifact-hashes.json` | `ebaf0cd4607e8d0944c71641c45c7aa4d0cb8598596993a40c59c9b299968f18` |
| Pre-inference `source-hashes.json` | `375c956282dea54107a9751f0b27132445729e87ef5debf644821df7e65d8316` |

## Stop and remaining decision

**Runtime precondition failed; stopped before another model or candidate.** No claim about score usefulness, calibration, order averaging, binding improvement, full-batch speed/memory qualification or audit success. Audit inference and original validation checks remain zero. Audit disputed recall remains indicative only, as previously recorded.

A possible owner-authorised continuation is bounded resident reference batches starting unloaded, with every boundary and cold load disclosed, under quieter conditions and sufficient headroom. That changes residence protocol and needs a decision before execution; it was not silently applied. No investigation of the separate memory-growth issue is proposed for this stopped run.

New measurement tools: `FittingReferences/` host/readme/project, `Measure-FittingReferences.ps1`, summariser and checkpoint under `tools/alpha-03/`. Files outside that folder: `docs/ALPHA-03.md` and this summary only. No unit tests added/updated/skipped/weakened. The real app ran for residency and the feature was exercised through the harness; no UI change or screenshot was requested. Required post-commit Release build/full-test and push results are reported in the handoff.
