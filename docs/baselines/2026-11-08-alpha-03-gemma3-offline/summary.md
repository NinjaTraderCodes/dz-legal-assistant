# Saved gemma3 scores: fixed offline diagnosis

**The tested scored reader is worse than unchanged Alpha-01 on these fitting cases.** Critical-error-free cases: Alpha-01 **36/96**, uncalibrated forward scores **21/96**, fixed global correction **23/96**. False affirmations: **26,63,46**. Correction partly improves the scored arm but does not recover the baseline. No scored arm or offsets are selected for deployment.

The owner independently chose **unchanged Alpha-01, gemma4:e4b, thinking off** before these results and waived order/correction tuning, binding and48-case audit for now. This diagnosis does not determine that candidate. No new model inference, gemma4 probe, audit or original validation evaluation was performed. The agent has not accessed the owner's evaluation folder or run the owner wrapper.

## Fixed method and provenance

Before inspecting results: catalogue-forward order only; uncalibrated versus **lambda0.1**, the middle of the previously declared grid. No grid search, reverse averaging, winner selection or full-fitting refit. Four zero-sum offsets in[-3,3] with class-balanced cross-entropy and L2 penalty; fixed optimiser/step/1000-iteration ceiling/1e-10 stopping tolerance from the approved protocol. Three manifest folds, each trained on64 cases and evaluated on32, with claim/scenario groups kept apart. All96 cases/372 dense events are held out once. Audit and validation labels do not enter training or scoring.

Frozen corpus `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`; saved native collection source2daa20f, complete gemma3 structural audit744 score positions and552 extensions. Status-only ablations preserve first Alpha-01 roles, bindings, requests and rejected lines. Newly recovered events receive no invented values or sources; absent removes an event. Official `FactsEvaluator.Score` reproduced the exact first-reference errors before evaluating the replacements. First reference file SHA-256 `0dc3123e10bcedfc77566f1c71bec35925d4e446dc327ebf9b51d221a09ff91b`.

Raw diagnostic root `%TEMP%\dzlegal-alpha03-gemma3-offline-20261108-163515`; original collection and reference roots remain unchanged. `status-predictions.json` records fold membership/offsets, `status-evaluation.json` official error results and retained facts, `diagnostic-summary.json` dense/status/kind/fold measures. No final parameter file exists.

## Whole cases and dense statuses

| Measure | Alpha-01 | Uncalibrated | Fixed correction |
|---|---:|---:|---:|
| Critical-error-free, all | 36/96 | 21/96 | 23/96 |
| Polarity families | 23/72 | 12/72 | 14/72 |
| Binding families | 13/24 | 9/24 | 9/24 |
| Dense statuses correct | 275/372 | 188/372 | 197/372 |
| Non-affirmed errors | 71/160 | 136/160 | 119/160 |
| False affirmations, all | 26 | 63 | 46 |
| False affirmations, polarity families | 25 | 58 | 41 |
| False affirmations, binding families | 1 | 5 | 5 |
| Unresolved statuses | 6 | 0 | 0 |
| Decisive event omissions | 44 | 2 | 8 |

Fewer omissions do not imply a better reader: the scored arm assigns many absent facts an affirmed/disputed status. Scores/wording/carrier/focus changed together, so this cannot isolate log probabilities as the cause. Dense denied-versus-absent errors remain counted. Bindings/requests were held fixed, not re-read; the status ablation is not a source-verified final fact sheet.

## Status classes

Recall is correct/gold; precision is correct/predicted. A dash means no predicted class, not zero precision.

| True status | Gold | Alpha-01 recall / precision | Uncalibrated recall / precision | Corrected recall / precision |
|---|---:|---|---|---|
| Affirmed | 212 | 186/212 (87.7%) /87.7% | 164/212 (77.4%) /72.2% | 156/212 (73.6%) /77.2% |
| Denied | 47 | 0/47 (0%) /— | 3/47 (6.4%) /75.0% | 10/47 (21.3%) /34.5% |
| Disputed | 18 | 5/18 (27.8%) /45.5% | 15/18 (83.3%) /11.3% | 14/18 (77.8%) /12.1% |
| Absent | 95 | 84/95 (88.4%) /58.7% | 6/95 (6.3%) /75.0% | 17/95 (17.9%) /68.0% |

Disputed predictions rise from11 to133 uncalibrated and116 corrected, for18 true disputed events. High disputed recall therefore comes with very poor precision. Absent recall collapses. This is not explained by a single correctable lean toward affirmation.

Per family kind, correct/gold by status:

| Kind and arm | Affirmed | Denied | Disputed | Absent |
|---|---:|---:|---:|---:|
| Polarity / Alpha-01 | 137/158 | 0/42 | 5/18 | 83/94 |
| Polarity / uncalibrated | 119/158 | 2/42 | 15/18 | 6/94 |
| Polarity / corrected | 113/158 | 9/42 | 14/18 | 17/94 |
| Binding / Alpha-01 | 49/54 | 0/5 | n/a | 1/1 |
| Binding / uncalibrated | 45/54 | 1/5 | n/a | 0/1 |
| Binding / corrected | 43/54 | 1/5 | n/a | 0/1 |

Both scored arms predict9 disputed events in binding families, which contain no true disputed label. Every per-kind precision/confusion and target-status measure remains in the external diagnostic JSON.

## Folds, offsets and remaining errors

| Held-out fold,32 cases each | Alpha-01 critical-free / non-affirmed errors / false affirmations | Uncalibrated | Corrected |
|---|---|---|---|
| 1 | 10 /27 /15 | 5 /52 /27 | 6 /48 /23 |
| 2 | 10 /20 /5 | 7 /31 /15 | 8 /21 /7 |
| 3 | 16 /24 /6 | 9 /53 /21 | 9 /50 /16 |

The correction's offsets in code order affirmed/denied/disputed/absent:

| Held-out fold | Offsets | Iterations |
|---|---|---:|
| 1 | -1.372403,+1.315755,-1.392387,+1.449035 | 75 |
| 2 | -1.600205,+1.473236,-1.778141,+1.905110 | 80 |
| 3 | -1.418824,+1.361757,-1.374318,+1.431385 | 76 |

All converge within the declared bounds/tolerance. They are fold-training parameters only. Nothing is refitted on all96 or used in the frozen gemma4 candidate.

| Official error occurrences | Alpha-01 | Uncalibrated | Corrected |
|---|---:|---:|---:|
| Plaintiff position | 1 | 1 | 1 |
| Missing decisive event | 44 | 2 | 8 |
| Other missing event | 18 | 0 | 0 |
| Polarity | 24 | 93 | 89 |
| Forbidden affirmation | 26 | 63 | 46 |
| Date not given | 16 | 25 | 23 |
| Date on wrong event | 11 | 11 | 11 |
| Amount not given | 16 | 17 | 17 |
| Missing request | 8 | 8 | 8 |
| Wrong request amount | 4 | 4 | 4 |
| Unreadable lines | 6 | 6 | 6 |

Categories overlap within cases; all other official error types zero. Dense known-date omissions25, event-amount omissions17 and requested-amount omissions6 remain unchanged. Dates130/166, event amounts57/74 and request amounts77/87 correctly bound in every arm; source overlap146/277 and request presence205/228 also unchanged. Alpha-01's one original answer cap remains in the retained facts; the scored stage's forced one-token outputs are not additional truncation failures. No end-to-end pipeline was rerun.

No new timing or memory run. Saved forward-chain mean14.645931 seconds plus retained Alpha-01 case mean20.211819 and one recorded load/wait allowance.568078 gives a **35.425827-second component projection**, identical for the two status arms apart from unmeasured trivial offset arithmetic. This is not a measured integrated reader and includes no binding component. Prior native timings/resources were recorded on mains/Best performance, with the app resident; see the stopped-collection summary for all loads and memory limits.

## Reproduction and hashes

```powershell
python -B tools/alpha-03/evaluate_saved_gemma3.py $savedCollection $freshExternalDiagnostic
dotnet tools/alpha-03/StatusEvaluation/bin/Release/net10.0/StatusEvaluation.dll tests/alpha-03-cases $firstReference "$freshExternalDiagnostic\status-predictions.json" "$freshExternalDiagnostic\status-evaluation.json"
python -B tools/alpha-03/report_saved_gemma3.py $freshExternalDiagnostic $firstReference $savedCollection
```

| Artifact | SHA-256 |
|---|---|
| `status-predictions.json` | `4787910e67e9438cd4363e2cc02c504c4c8c1493bb7fcdc7756e35dc3d7d7cfb` |
| `status-evaluation.json` | `4961f01295e6f9ab436f12179d2d13a6ad443f42f97c5abdfaebd87e6bb31007` |
| `diagnostic-summary.json` | `ce3bac8a21590f62e2071de32757e79e355ca1d72570b6b84e695ec99f05454f` |
| `artifact-hashes.json` | `bb2312cadd32ac29d645a77e4a8dc3280ff682d4ecbce4e1f48ff71f6ff5b3cb` |

The owner requested a safe pause during preparation of the separate Alpha-01 gemma4 freeze. The thinking flag and guarded/private owner-run code are saved, compiled and mock-tested, but **final freeze commands have not been issued and the owner wrapper was never executed by the agent**. No hidden/evaluation files were opened, listed or copied.
