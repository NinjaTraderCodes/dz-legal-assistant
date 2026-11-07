# Alpha-03: complete gemma4 reference repeat

All **96 fitting cases** completed from case1, with ten planned unload/reloads, on mains and **Best performance**. No browser/VS Code guard failure, transport/context failure or answer cap. This completes the owner's full repeat request; the earlier 37-call stopped prefix is not reused. No candidate or audit inference was run during this measurement.

**The first complete gemma4 accuracy/facts remain the semantic reference: 69/96 without critical error.** This repeat supplies qualified cost/memory and diagnostic repeatability only. Completed gemma3 reference remains36/96. No runtime-model switch.

Source `7539f762efde1ae994dd262f329c099ad7273011`, branch `codex/alpha-03`; frozen v3 content `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5` verified unchanged. Timed UTC interval **2026-11-07 22:57:52–23:24:13** (8 November locally). External root `%TEMP%\dzlegal-alpha03-gemma4-repeat2-20261107-235312`. Old first root `%TEMP%\dzlegal-alpha03-v3-fitting-20261107-215737` and stopped-repeat root remain read-only; all500/119 archived artifact hashes verified.

## Side-by-side references

Quality uses the first complete predictions for each model. Gemma4 cost/resources below use this repeat, rather than its browser-confounded first timing. These are separate serial runs, not a matched background-load experiment.

| Reference | Critical-free overall | Polarity families | Binding families | Mean case seconds | Mean with loads/waits |
|---|---:|---:|---:|---:|---:|
| gemma3:4b, retained | 36/96 | 23/72 | 13/24 | 20.211819 | 20.756463 |
| gemma4:e4b, thinking off | 69/96 retained | 46/72 retained | 23/24 retained | 15.448330 repeat | 16.469249 repeat |

| Family kind | gemma3 mean / median / p95 / max seconds | gemma4 repeat mean / median / p95 / max seconds |
|---|---|---|
| Polarity | 19.455098 / 16.592911 / 30.750584 / 42.566106 | 14.934314 / 13.370882 / 24.199475 / 25.701978 |
| Binding | 22.481980 / 21.760112 / 33.091903 / 35.251069 | 16.990379 / 16.871405 / 24.817895 / 24.961559 |

Overall gemma4 repeat median/p95/max: **13.555914 / 24.809719 / 25.701978** seconds. Case sum1483.039679; full batch1581.047867 seconds, difference98.008188 for reload/settling/orchestration. All ten warm-up wall times are separate: **7.951261, 7.609862, 7.232118, 7.168582, 7.387654, 7.137104, 7.126724, 7.112756, 7.113051, 7.386023** seconds; mean7.322513. Unload total .122747 seconds. Native load durations are absent/null, never inferred. Reload before ordinals1/11/.../91; no rescue reset or retry.

Installed Ollama0.40.15; CPU only, context4096, temperature.2, repeat penalty1.1, cap300, keep15m, gemma4 `think:false`, no new seed/schema/logprob option. Every outgoing JSON request equals its first-run counterpart (96/96); every full source sentence is present. This is unchanged Alpha-01. Original model metadata: gemma3 4.3B Q4_K_M, disk3,349,593,375 bytes, Gemma Terms of Use, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; gemma4 7.5B Q4_K_M, disk6,583,656,505 bytes, Apache2.0, digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No weights bundled.

## Repeatability, all96

Exact decoded strings include whitespace/line endings, with no normalisation. Verdict means whole-case critical pass/fail; error multiset and parsed facts are stricter separate checks. No best-answer selection.

| Group | Cases | Exact answers | Same verdict | Same error multiset | Identical parsed facts |
|---|---:|---:|---:|---:|---:|
| All | 96 | 84 | 94 | 92 | 90 |
| Polarity | 72 | 64 | 70 | 68 | 66 |
| Binding | 24 | 20 | 24 | 24 | 24 |
| Fold1 | 32 | 22 | 31 | 29 | 28 |
| Fold2 | 32 | 32 | 32 | 32 | 32 |
| Fold3 | 32 | 30 | 31 | 31 | 30 |

One fail-to-pass (`a03-006`), one pass-to-fail (`a03-036`). Both complete runs are69/96,46/72 polarity,23/24 binding, but not the same69 cases. Fold critical-free counts first16/25/28, repeat15/25/29. Twelve changed answer strings:003,006,018,027,030,033,034,036,039,109,110,112 (all prefixed`a03-`). Changed parsed facts:003,006,033,034,036,039. Changed error multisets:006,033,036,039. This demonstrates run variation under unchanged inputs/settings; one-point fitting gains must be interpreted with that limitation. No additional repeat is selected from these results.

## Dense and error diagnostics

First gemma4 results remain those used for fitting comparisons. Repeat-only diagnostics are explicitly labelled here; no accuracy is replaced. Complete retained gemma3/per-kind/fold data are in [the first-reference summary](../2026-11-07-alpha-03-fitting-references-v3/summary.md).

| Measure | First gemma4, retained | Repeat, diagnostic |
|---|---:|---:|
| Dense status correct | 329/372 | 329/372 |
| Non-affirmed errors | 35/160 | 34/160 |
| False affirmations | 16 | 16 |
| Denied target recall | 9/18 | 10/18 |
| Disputed target recall | 16/18 | 16/18 |
| Known event dates correct / omitted / wrong | 164/1/1 of166 | 164/1/1 of166 |
| Known event amounts correct / omitted / wrong | 72/2/0 of74 | 73/1/0 of74 |
| Requests present | 226/228 | 226/228 |
| Known request amounts correct / omitted / wrong | 86/0/1 of87 | 86/0/1 of87 |
| Events with gold-source overlap | 172/277 | 173/277 |

| Error occurrences | First gemma4 | Repeat |
|---|---:|---:|
| Missing event, nondecisive | 8 | 8 |
| Missing decisive event | 12 | 11 |
| Polarity | 9 | 10 |
| Forbidden reading | 16 | 16 |
| Amount not given | 2 | 1 |
| Unreadable lines | 10 | 10 |
| Date on wrong event | 1 | 1 |
| Missing request | 2 | 2 |
| Request amount | 1 | 1 |

All other evaluator error types zero for these two gemma4 records. Error categories overlap within cases; occurrences are not failed-case counts. Denied/absent distinctions remain counted, including nondecisive events. Repeat dense confusion: A203A/4U/5N of212; D29D/14N/4A of47; U16U/2A of18; N81N/3D/10A/1U of95. Zero unresolved statuses. Binding families:60/60 status correct, one critical requested amount error; polarity families:269/312 correct and26 critical cases. Source overlap is diagnostic, not independent semantic verification.

## Power, memory and load qualification

Every timed resource sample **746/746** reports mains, energy saver off, and actual/effective/configured AC overlay `ded574b5-45a0-4f42-8737-46345c09c238` (**Best performance**). App stayed resident; fresh-sample, browser/Code, 512MiB available and model-private-plus-app guards passed. No active browser observed in the resource record; direct process checks also ran before every reader request and after reload steps. The periodic monitor stores only the top-three other CPU processes, so it is not an exhaustive historical process inventory between checks.

| Resource, GiB | Retained gemma3 | gemma4 repeat |
|---|---:|---:|
| Model/server working-set peak | 6.422150 | 7.273197 |
| Model/server private peak | 6.897877 | 7.998604 |
| Concurrent model + app working-set peak | 6.580795 | 7.424625 |
| Machine available minimum | 4.268772 | 4.063122 |

Gemma4 per-kind model WS/private/model+app/available minimum: polarity **7.273197/7.998604/7.424625/4.135094**; binding **7.190422/7.915638/7.341862/4.063122** GiB. Overall commit peak13.598026GiB. Peaks are sampled sums, may double-count shared pages and miss short spikes; per-kind measurements are chronological in shared resident blocks. This qualifies only the agreed ten-case residence protocol, not unrestricted residence stability.

Preflight recent other-CPU mean.420 cores, no model call during a preceding .510-core quiet rejection. During timing other-CPU mean/median/p95/max **.731/.744/1.026/3.293 cores**, chiefly Task Manager/desktop activity; retained gemma3 mean/p95 .384/.613. No competing model/build/test job was run. **Timing is under the declared power/browser/residence conditions, with this background-load difference disclosed; it is not a controlled CPU-idle speedup estimate.** Monitor mean.019 cores.149 fresh paging samples, peak reads1119/s, pages input238816/s during load phases, output0; case-phase polarity/binding page-input peaks816/522. No claim of no paging or memory-growth cause investigation.

Normal cleanup: model unloaded, owned idle app and monitor exited; Search stayed Running/Automatic throughout, never paused; Best performance retained. Owner processes untouched. No candidate, calibration, binding, compatibility, audit or original validation inference yet.

## Reproduction and artifacts

```powershell
# Build/check tools first, then quiet mains/Best-performance/app/monitor preflight.
& tools/alpha-03/Measure-FittingReferences.ps1 -RunRoot $newExternalRoot -ModelNames gemma4:e4b
# After normal cleanup and stopping the monitor:
python -B tools/alpha-03/audit_references.py $newExternalRoot --models gemma4:e4b
python -B tools/alpha-03/compare_reference_repeat.py $firstExternalRoot $newExternalRoot
python -B tools/alpha-03/summarize_fitting_references.py $newExternalRoot
python -B tools/alpha-03/freeze_cases.py --verify
```

All260 new artifact hashes independently verified. All96 full outgoing JSON bodies independently compared with the first record. First/stopped records unchanged. Exact raw requests/responses, facts/errors/native times, loads, manifests, binary/source hashes, environment/model metadata, power/resource samples and cleanup stay external.

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `37c6fe051f0084b09d3464074647efdced0ee78a4b2d971459a502495ca17f9f` |
| `reference-analysis.json` | `a94b1d2adde396198a93c4e2914c2023059ab1a206c746754513612e7d9a439e` |
| `reference-wire-audit.json` | `1a614698aa41aeb78be11db939a08d3decb4aac90be68c8419292b32ab3dafc2` |
| `repeatability.json` | `786c68b4abdb1255027380ca1a076014fd8c5d8d208c4c734cc1eee0fcfdb9dd` |
| `artifact-hashes.json` | `fe1207c9ef940c086d415be1d23e6d5d945bfab37e9af2b665547e04d2a7e073` |

Proceed under the already-authorised frozen fitting protocol; no audit/validation use to tune. Files changed outside `tools/alpha-03/` at this checkpoint: this summary and `docs/ALPHA-03.md` only. Frozen corpus unchanged. No unit test added/changed/skipped/weakened; required post-commit Release/full-test results recorded in the report/handoff.
