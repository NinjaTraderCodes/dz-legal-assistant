# Alpha-03: scored fitting stopped on a missing class score

**Stopped before corpus calibration or accuracy evaluation.** Gemma3 completed96 fitting cases/744 four-score positions. Gemma4 completed three cases, then omitted class`4` from `top_logprobs:20` at `a03-004`, reverse question5 (`debt_acknowledged`), native request47. The fourth case remains partial. The required all-four-score precondition fails, so the **entire stage stops**, including evaluation of the complete gemma3 collection. Permission to continue a passing model after the other's *quality* failure does not waive a missing-score/runtime precondition.

No imputation, negative-infinity substitution, larger top-k, prompt/schema change, retry, alternative runtime or new inference followed. No primary chosen, parameters fitted on corpus labels, binding implementation/run, compatibility check, audit inference or original validation inference. Semantic references remain first gemma3 **36/96** and first gemma4 **69/96**; no scored-reader accuracy is known. Hidden material untouched.

Run source **`2daa20f84ba104a72017c8e034c7325f4aee14d5`**, pushed after solution/collector Release0warnings/errors and all1001 tests passed. Frozen v3 **`9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`** verified before and after. Reader/prompt unchanged from the bounded successful capability probe, prompt hash `f697cdf9769765e60ddc6031e20584b10f2dd9293fef80012a6004aa2ef39d8d`. Raw root `%TEMP%\dzlegal-alpha03-scored-fitting-20261108-150244`.

## What failed

The actual request still has CPU0, context4096, temperature.2, repeat penalty1.1, seed0, cap1, literal0 integer schema, `top_logprobs:20`, `think:false` and exact append-only history. The returned neutral`0` is one byte-exact token. The failed position has **634 prompt tokens and604 cached tokens**: this is not a context/cache/power/headroom failure.

| Native alternative | Returned log-probability |
|---|---:|
| 1 affirmed | -6.9355788230896 |
| 2 denied | -10.099605560302734 |
| 3 disputed | -12.997161865234375 |
| 4 absent | **not returned; value unknown** |

All20 returned alternatives are preserved in the raw response. Label3 is the last returned alternative; absence of4 does not supply its score. No inference about the fact's true status or a model accuracy error is made from this. The first-input capability pass established availability at24 positions, not at every future proposition. This failure falsifies that broader availability assumption for the current gemma4 shape/interface.

Gemma4 coverage: **47 actual native calls,46 usable four-score positions,1 unusable position,697 planned positions never requested**. Three whole cases/36 positions complete; case4 has11 calls (six forward, five reverse), ten valid four-score positions before the failure. Seven whole chains complete. All39 extensions reuse cache. Maximum observed prompt711. The partial record is not a96-case comparison and is not used to select better answers or parameters.

The collector had a bookkeeping bug: `InvalidDataException` was not in its exception filter and does **not** inherit `IOException` on the installed runtime (base is `SystemException`). It escaped, PowerShell stopped its pipeline, and **the original gemma4 `completion.json` is absent**. That absence is retained, not repaired retrospectively. `observed-stop.json` and reproducible `stopped-diagnostic.json` explicitly describe the partial/crashed run. All47 request/response pairs and the three completed rows remain intact. No further dotnet collector process remained after the crash.

## Gemma3 complete component costs; gemma4 diagnostic prefix

Gemma3 timed UTC **14:03:36.103–14:50:29.527** on8 November. Ten planned loads,96 cases,192 chains,744/744 finite score sets,552/552 positive-cache extensions. Independent audit verifies every full prompt/source sentence/history array, score vector/bytes/options/context and native count. First-position cache varies and is recorded separately; it is not required positive. Maximum prompt807, below4096.

| Measured gemma3 component, seconds | Mean | Median | p95 | Max |
|---|---:|---:|---:|---:|
| Fixed forward chain | 14.645931 | 14.349098 | 18.527558 | 19.281648 |
| Reverse chain | 14.091814 | 14.143835 | 18.433179 | 18.873566 |
| Both chains, case clock | 28.738421 | 28.629251 | 36.961368 | 37.569868 |

Total collection **2813.423862 seconds**, **29.306499/case** including loads/settling/orchestration. Separate empty warm-ups: **4.013064,2.783018,2.844949,2.790465,2.841990,2.762243,3.028730,2.784998,2.817888,2.842950** seconds; mean2.951029. Unload total .125572 seconds. Native load durations absent/null. Total non-case bill54.535487 seconds, .568078/case. No unscheduled reset/retry; unload/reload before ordinals1/11/.../91.

| Gemma3 family kind | Cases | Forward mean | Reverse mean | Pair mean |
|---|---:|---:|---:|---:|
| Polarity | 72 | 14.836659 | 14.863562 | 29.700910 |
| Binding | 24 | 14.073744 | 11.776572 | 25.850953 |

Shorter48 inputs (all read fields189–282 characters): forward/reverse/pair means **13.925888/13.799280/27.725844** seconds. Longer48 (282–820): **15.365973/14.384348/29.750997**. Equal-length ties split by case name; no input truncated. These length groups have different case/question mixes and are descriptive, not a causal length experiment.

For context only, applying the predeclared component bill to retained Alpha-01 mean20.211819 gives a **35.425827-second fixed-order projection** or **49.518317-second order-averaged projection**, with one .568078 status-load/wait bill and no duplicate reference reload bill. These are projections **without binding**, not integrated-reader measurements or a quality qualification. Shared collected chains are not independent reruns; forward-only residence has not been independently measured. No arm selected.

Gemma4 completed-prefix pair times: case001 **18.377928** (13.369259 forward/5.003641 reverse),002 **18.286003** (13.012761/5.272380),003 **18.222871** (13.221116/5.000822). Initial warm-up8.630993 seconds, unload .039480, native load absent. Partial case4's full wall time and overall process completion were not captured because of the crash. Do not manufacture a96-case mean, infer accuracy, or compare this prefix with complete gemma3 costs.

## Power, memory and background conditions

Every sampled model window has mains and **Best performance**, energy saver off, actual/effective/configured AC overlay `ded574b5-45a0-4f42-8737-46345c09c238`: gemma3 **1328/1328**, partial gemma4 **43/43** samples. Real idle app resident, owner-closed browsers/Code absent at guards; no sampled browser appearance, headroom or measurement failure. No model/build/test/calibration job ran concurrently with timing. Initial available11.25GiB, quiet preflight other CPU about.34 cores. Browser closing remains owner-only.

| Sampled GiB | Gemma3 complete | Gemma4 partial, diagnostic |
|---|---:|---:|
| Model/server working-set peak | 5.779316 | 6.767456 |
| Model/server private peak | 6.162811 | 7.494129 |
| Concurrent model + app working-set peak | 5.960472 | 6.920105 |
| Machine available minimum | 5.412941 | 4.551441 |
| Commit peak | 11.662502 | 13.194916 |

Gemma3 resource peaks by kind remain chronological within shared ten-case resident blocks; full values in the structural audit. Background other CPU mean/p95/max **.424/.653/1.408 cores**, partial gemma4 **.493/1.005/1.220**; monitor means .019/.021. Working-set sums may double-count shared pages and sampling misses short spikes. Partial gemma4's resource window uses its recorded first load14:50:30.018 to last native response `created_at`14:52:00.947, not a fabricated batch completion time.

Paging: gemma3265 fresh samples, peak page reads72/s, input1106/s, output109435/s; only **one** fresh sample has nonzero output. Partial gemma4 nine fresh samples, reads19/s, input50/s, output0. This is reported, not described as no paging or attributed to a cause. The separate resident-memory-growth issue was not investigated. Headroom/private-plus-app guards passed; no unrestricted residence-stability claim follows.

Installed assets unchanged: Ollama0.40.15; gemma3 4.3B Q4_K_M,3,349,593,375 disk bytes, Gemma Terms of Use, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; gemma4 7.5B Q4_K_M,6,583,656,505 bytes, Apache2.0, digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. No download, weights bundled or runtime-model change.

## What is saved and checked

While collection ran, offline source was prepared but **never executed on corpus predictions**: `fit_status.py` (exact three-fold/lambda grid and selected-only refit), `StatusEvaluation/` (unchanged official scorer, pinned first-reference hashes), `select_status.py` (predeclared gates/ranking), and numerical fixtures. The shared summariser adds explicit four-class decisions and dense decisive omissions. This is implementation, not a fitting/calibration/selection result. `fit_status.py` requires a complete **two-model** audit; a single-model audit cannot silently qualify it. No `status-predictions.json`, fitted offsets or primary exists.

After the stop, failure handling was repaired only: collector explicitly catches `InvalidDataException`, records failed case/order/call and writes incomplete completion; wrapper saves failure metadata and unloads in `finally`, including a thrown native-pipeline error. New synthetic HTTP regression proves an unavailable class stops before another question with no retry. Two standalone mocked-native/mocked-HTTP fixtures prove cleanup and no second-model start for returned/thrown failures, **zero real HTTP/model calls**. Seven numerical tests pass. Solution and both tool builds pass0warnings/errors; required post-commit full-suite/push outcome is reported in the handoff. No prediction logic, prompt, schema, top20 setting or missing-score rule changed. Existing tests unchanged/skipped/weakened: none.

Measurement commands (do not resume inference without owner decision):

```powershell
# Original collection, already stopped:
& tools/alpha-03/Measure-ScoredFitting.ps1 -RunRoot $freshExternalRoot -Mode fitting
# Read-only diagnosis of existing records; no corpus calibration/accuracy:
python -B tools/alpha-03/diagnose_scored_stop.py $stoppedRoot
python -B tools/alpha-03/audit_scored_collection.py $stoppedRoot --mode fitting --models gemma3:4b
python -B tools/alpha-03/test_fit_status.py
& tools/alpha-03/Test-ScoredFailure.ps1 -OutputRoot $newExternalFixtureRoot
python -B tools/alpha-03/freeze_cases.py --verify
```

The complete gemma3 structural result is deliberately named `fitting-audit-gemma3-4b.json`; **no full `fitting-audit.json` exists**. `stopped-diagnostic.json` is explicitly stageComplete=false. All **1658 artifact hashes verified**. First reference500, qualified gemma4 repeat260 and capability89 artifact hashes also verified unchanged. Raw native outputs remain external, including all791 request/response pairs. No missing completion file was reconstructed.

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `5a2c081a10a98e8bcc33cd8acc2b9885efad622fc7c169eed694c933e5423e41` |
| `fitting-audit-gemma3-4b.json` | `f28cde0daa3cbb11e1890b16d02a9ee7d778fdd20b929ca0541a3e4ed7ebca16` |
| `stopped-diagnostic.json` | `7de639ab4fcfb0fe6426f04f83643709d78d83637df7f14458c7cb75fc506bb4` |
| `observed-stop.json` | `e6c2991948150fe266199f695e258cbfaa4536b6f45e1b93e81791970f54b61e` |
| `artifact-hashes.json` | `d082298362fe705ce1460d83d0d5413111731641fc422ebd01c6b6bc56dd1eef` |

Normal cleanup: both models unloaded; owned app12592 and monitor2456 exited (IDs now historical); Search stayed Running/Automatic and was never paused, Best performance retained. Owner processes untouched. Outside `tools/alpha-03/` this checkpoint changes `docs/ALPHA-03.md`, this summary, and `tests/DzLegal.Law.Tests/ScoredStatusReaderTests.cs` only. No frozen case/source/value/date/status/flag changed.

**Await owner decision under brief§8.** The bounded next step I recommend, if explicitly authorised, is offline evaluation of the already-complete gemma3 collection using the fixed grid. That would narrow this diagnostic stage to gemma3; it would not waive the unresolved gemma4 requirement or authorise a different final audit. If both-model capability remains mandatory, the current shape cannot qualify as-is and a different score-access design must be proposed before another probe. Neither option has been executed. No prompt repair from this case, top-k expansion or guessed score is proposed as an automatic continuation.
