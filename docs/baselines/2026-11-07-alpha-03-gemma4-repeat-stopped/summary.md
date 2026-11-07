# Gemma4 timing repeat: browser guard stopped before request 38

**37/96 model calls completed; Firefox process presence blocked request 38. Full repeatability and qualified 96-case cost remain unavailable.** Stop before fitting/candidates/audit, under the existing precondition rule. No selective continuation or automatic restart.

Owner authorised gemma4 only, all96 from case1, retaining the completed gemma3 reference and **the first gemma4 accuracy/facts (69/96)**. This attempt does not replace either semantic reference. V3 remains `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`; source `e54cd8b49ed00c02bc065d3546832b0dc570e763`, Release build0warnings/errors, all990tests passed and pushed before timing. The reader/prompt/provider/settings are unchanged.

## Exact stop

At **22:36:36.420 UTC**, the repaired per-request guard detected Firefox PIDs **17140, 19020, 13636**. This identifies process presence, not who opened a window or why the processes started. It made no request for `a03-038`: there are 37 outgoing bodies, 37 native timings and no `call-038-request.json`.

The sample was fresh (22:36:36.330 UTC); mains and all three Best-performance GUIDs passed. Available physical was 4,679,462,912 bytes (about4.36GiB), comfortably above512MiB; model-private plus app working set remained below physical. Thus browser presence, not power/headroom/context, caused the stop. The strict guard catches idle/background browser processes too; no exemption or relaxation was applied.

Evaluator metadata has38 attempts including the blocked technical evaluation,58 unattempted; **59 cases received no model inference**. The blocked evaluation must never count as an answer or a matching critical verdict. Four planned loads completed, before fitting ordinals1/11/21/31. No retries, cap increase, altered case, source truncation or model upgrade.

## Partial repeatability — denominator 37, not 96

Every completed outgoing JSON body is identical to its first-run counterpart; input fingerprints and order match. Exact answer means the decoded string including whitespace/line endings. Verdict means whole-case critical pass/fail. Error multisets and parsed facts are separate diagnostics.

| Metric | Completed-prefix matches |
|---|---:|
| Exact answers | 31/37 |
| Critical pass/fail verdict | 37/37 |
| Full error multiset | 36/37 |
| Exact parsed facts | 34/37 |
| Pass-to-fail / fail-to-pass | 0 / 0 |

Exact-answer differences: `a03-015`, `a03-017`, `a03-018`, `a03-027`, `a03-033`, `a03-034`. Parsed-fact differences: `a03-015`, `a03-033`, `a03-034`; error-multiset difference: `a03-033`. These are descriptive comparisons, not prompts for case-specific repairs. Both prefixes have20/37 without critical error. The first full-run **69/96 remains unchanged**, not replaced by this unrepresentative prefix. Repeats are not independent cases.

The full comparator still refuses an incomplete run by default. Its new explicit `--allow-partial-diagnostic` mode records planned96/completed37/fullRepeatAvailable=false, excludes only evaluations without actual successful native calls, and writes `partial-repeatability.json` to the new root. It does not modify the first root or claim the requested full96 comparison is fulfilled.

## Recorded partial outputs and resources

37 actual cases:33 polarity-kind (16 without critical error),4 binding-kind (all4). Critical occurrences/cases: missing decisive event5/5, wrong polarity5/5, forbidden affirmation11/10. Other critical types0. Noncritical: missing non-decisive event7, omitted amount1, unreadable lines in4 cases. Counts overlap. No cap or context flag, no transport failure among actual calls; one blocked browser-guard attempt separately.

Dense status153/182 correct, unresolved0; non-affirmed errors24/79, false affirmations11. Known dates73/74 correct with1 omission, no wrong return; amounts58/59 with1 omission, no wrong return. Request presence90/90, known amounts36/36. Source overlap77/140 non-absent events. Dense denied/absent scoring remains unchanged by decisive flags.

All322 timed-window power samples verify **mains and actual/effective/configured Best performance**. Visible physical15.681GiB; real idle app resident. Model/server sampled working-set peak7.190GiB, private7.916GiB, concurrent model-plus-app7.369GiB, machine available minimum4.232GiB, commit peak13.346GiB. Other CPU mean.697/p95.935/max1.398 cores; sampler mean.018. Paging64 fresh samples, peak reads1314/input174692/output0 per second, with model-load file reads included. These are partial observations, not a full quiet-batch memory qualification or a cause investigation of resident growth.

Informative partial case seconds: mean17.228, median15.947, nearest-rank p9525.875, max26.277. Batch679.735seconds includes four empty warm-ups (mean7.629seconds, native load time unavailable), unloads/waits/orchestration and blocked evaluation. No full96 cost comparison is reported. Default300-token reader cap, context4096, CPU-only, temperature.2, repeat penalty1.1, gemma4 `think:false`; model digest/runtime remain dc35e8d9...300c9c / Ollama0.40.15. Licence/size/full digest are unchanged from the preceding complete-reference summary.

## Artifacts, cleanup and handoff

Root `%TEMP%\dzlegal-alpha03-gemma4-repeat-20261107-232428`; raw bodies, outputs, loads, manifest, exact failure sample/process IDs, power/resources and cleanup retained outside repo. First root `%TEMP%\dzlegal-alpha03-v3-fitting-20261107-215737` read-only and unchanged.

```powershell
& tools/alpha-03/Measure-FittingReferences.ps1 -RunRoot $newExternalRoot -ModelNames gemma4:e4b
# After stop and cleanup; no more model calls:
python -B tools/alpha-03/compare_reference_repeat.py $firstRoot $newExternalRoot --allow-partial-diagnostic
python -B tools/alpha-03/summarize_fitting_references.py $newExternalRoot
python -B tools/alpha-03/freeze_cases.py --verify
```

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `37f250f9a6e32472354adc94902a75e70337710e32e1bf8cfa3cfc664f86a0c8` |
| `precondition-failure.json` | `55ff85983e02f132f30955fe6afa4b6aa20e9a6a83c6e7d2ea33b6b150ecddf6` |
| `partial-repeatability.json` | `739e417e97ed6590c2abad6923da2348e63ccb2e99be5982c72bba8eacb693dc` |
| `reference-analysis.json` | `f65a4f959a2944c642bec9597a392c0bb66277e83b9b1d7195ac722862de8799` |
| `artifact-hashes.json` | `4fda2a61317ad098d2dc7f480d230e77f3edd8b4abf985682dc0dd3bb5d4361b` |

Measurement model unloaded, owned idle app/monitor closed normally; owner browser processes untouched. Search remained Running/Automatic without pause; Best performance retained. No scored candidate/prompt, calibration, binding, compatibility, audit or original validation inference. No corpus edit, hidden access, merge/PR or runtime-model switch.

New changes are measurement/diagnostic reporting only: comparator partial mode and checkpoints under `tools/alpha-03/`, this summary and `docs/ALPHA-03.md`. No unit test added/updated/skipped/weakened. Required post-commit Release/full-test and push results are reported in the handoff. A new full96 attempt requires the owner's answer under the agreed precondition stop; all browser processes, including Firefox, must remain absent throughout that timed attempt.
