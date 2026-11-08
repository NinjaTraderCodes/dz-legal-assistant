# Alpha-03: capability on the actual scored-reader shape

**Both models pass this bounded capability check.** One frozen fitting input (`a03-001`), forward and reverse, gives12 score positions/model. All24 positions have four finite byte-exact single-token scores, and all20 within-chain extensions reuse cache. No semantic accuracy evaluation, calibration, prompt adjustment or model selection was performed. Full fitting can proceed under the already-authorised protocol; any later missing-score/cache/context/runtime failure still stops the stage.

Source **`11fb3b2473cf36347a41c0020d2aa896e03d2462`**, whose solution/collector builds passed0warnings/errors and full1001 tests passed before the pause. Owner resumed on8 November. V3 corpus hash **`9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`** re-verified. Reader and the single prompt were unchanged after the pause; prompt SHA-256 **`f697cdf9769765e60ddc6031e20584b10f2dd9293fef80012a6004aa2ef39d8d`**. Raw root `%TEMP%\dzlegal-alpha03-scored-capability-20261108-145017`.

## Shape and counters

First input selected from the frozen fitting manifest, before reading its inputs; gold/annotations/family/fold never enter the prediction prompt. Complete numbered notes and the existing complete catalogue proposition are sent. Initial user turn contains notes plus first question; each request then preserves its exact prefix and appends the actual assistant `0` and next question. Four alternatives1A/2D/3U/4absent are read before the forced-zero schema masks output. Actual neutral history has no generated-label accuracy. Earlier questions remain visible; earlier chosen classes do not.

Ollama0.40.15, literal loopback, CPU0, context4096, temperature.2, repeat penalty1.1, seed0, cap1, `top_logprobs:20`, nonstream chat, integer enum`[0]`, keep15m. Gemma4 `think:false`; non-thinking gemma3 omits the switch. No fallback/interpolation, altered grammar, score clipping or increased top-k. All outgoing requests/raw responses preserved.

| Model | Four-score sets | Reused extensions | Forward cached tokens, q1–q6 | Reverse cached tokens, q1–q6 | Maximum prompt tokens |
|---|---:|---:|---|---|---:|
| gemma3:4b | 12/12 | 10/10 | 0,560,592,620,648,678 | 52,563,593,621,649,681 | 723 |
| gemma4:e4b, thinking off | 12/12 | 10/10 | 0,563,593,619,645,673 | 537,566,594,620,646,676 | 711 |

New-chain first-position cache is reported separately, not required zero: the reverse chain starts after forward, without an intervening unload. Gemma4 reused much more of that common prefix. This is observed server behaviour, not a conclusion about cache internals. Every within-chain extension must reuse cache regardless. The two order arms later share the same recorded vectors and are not independent repeats.

The independent auditor verifies exact whole prompt/history arrays against the frozen source sentences and catalogue, all24 wire/settings/byte/finite-score checks, saved-versus-native vectors, mode/selection/position counts, loads and timed power/headroom. One malformed/missing position would fail the audit and could not become a correct absence. No missing-score condition occurred here.

## Diagnostic time, memory and conditions

**Mains and Best performance** throughout every timed sample: gemma3 **17/17**, gemma4 **12/12**, energy saver off, actual/effective/configured AC overlay `ded574b5-45a0-4f42-8737-46345c09c238`. Closed browser/Code and idle real app resident. Recent preflight background CPU <=.5 cores. No concurrent model/build/test. No sampled browser activity or live measurement-guard failure. Owner now closes the browser manually; no further agent browser-close attempts are authorised. Search remained Running/Automatic, never paused.

| Component seconds | gemma3 | gemma4 thinking off |
|---|---:|---:|
| Forward six-question chain | 14.026115 | 10.291134 |
| Reverse six-question chain | 13.331288 | 3.848200 |
| Complete pair, excluding load | 27.361746 | 14.144661 |
| Empty warm-up wall | 4.098604 | 8.879621 |
| Unload wall | .056233 | .041079 |
| Full one-case batch, loads/waits included | 35.172609 | 26.391006 |

Native load duration unavailable/null. Only one case/model: these are diagnostic component costs, **not** full-fitting latency qualification, an integrated-reader result or a projected mean for144 cases. A full-fitting projection must also charge the retained Alpha-01 component and the selected chains, with one reload bill per10 complete pipelines, as predeclared. No unscheduled reset/retry: each model has one initial empty warm-up; gemma3 unloaded before switching model.

| Sampled GiB | gemma3 | gemma4 |
|---|---:|---:|
| Model/server working-set peak | 4.236893 | 6.684273 |
| Model/server private peak | 4.618511 | 7.411507 |
| Concurrent model + app working-set peak | 4.408741 | 6.856331 |
| Machine available minimum | 6.901161 | 4.521255 |

Background other-CPU mean/p95: gemma3 .461/.697 cores, gemma4 .442/.541. 512MiB guard and model-private-plus-app physical-memory check pass. Sampling can miss short spikes and working-set sums can double-count shared pages. Single-case peaks do not qualify ten-case residence or explain memory growth; that separate issue was not investigated.

Installed packages unchanged: gemma3 4.3B Q4_K_M,3,349,593,375 bytes, Gemma Terms of Use, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; gemma4 7.5B Q4_K_M,6,583,656,505 bytes, Apache2.0, digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. Neither becomes the runtime model. First semantic references36/96 and69/96 remain retained, with qualified gemma4 cost from its full repeat.

## Reproduction and artifacts

```powershell
# Fresh external root, owner-closed browser, mains/Best performance, quiet app/monitor setup.
dotnet tools/alpha-03/ScoredFitting/bin/Release/net10.0/ScoredFitting.dll capability gemma3:4b tests/alpha-03-cases data/law tools/alpha-03/scored-status.ar.txt $newG3Output $resourceRoot
# Unload gemma3, then the identical command for gemma4:e4b with a fresh output.
# After normal cleanup and monitor termination:
python -B tools/alpha-03/audit_scored_collection.py $resourceRoot --mode capability
```

`Measure-ScoredFitting.ps1 -RunRoot <fresh-resource-root> -Mode fitting` is the serial full-fitting wrapper. It never closes browsers, refuses existing model outputs and stops before the next model on failure. `audit_scored_collection.py --mode fitting` applies the same exact checks to all96 inputs/model and all744 positions/model; it requires complete output and never fills missing data. These measurement tools were completed after the bounded probe and audited its existing raw records without rerunning inference.

Normal cleanup completed: model unloaded, owned app15044 and monitor11004 exited; identifiers now historical. Search unchanged, Best performance retained. All **89 artifact hashes independently verified**. Raw output stays external; first/stopped/repeat reference roots untouched.

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `16768543c7d95ae193283e21829fb56d54dc25a5a0cc92a2ba5f1ed820a133c6` |
| `capability-audit.json` | `317dcafebdd792820bc6481436b51fc18c5d5af4fefd1b5275dd0ab68d64c969` |
| `artifact-hashes.json` | `dde62a36fc76776d9da4cb1c36586c315e50bb04f71cb03afd787efc01bfacaa` |

Next: full fitting collection, paired order/calibration ablations on grouped fitting folds only, gated binding, fitting-selected primary recorded before locked audit. No original validation. Capability pass is bounded; whole-case quality/cost gates remain open. Outside `tools/alpha-03/`, this checkpoint changes this summary and `docs/ALPHA-03.md` only. No existing tests changed/skipped/weakened; auditor itself checks actual raw wire data and wrapper syntax parses. Required post-commit Release/full tests and push are reported in the handoff.
