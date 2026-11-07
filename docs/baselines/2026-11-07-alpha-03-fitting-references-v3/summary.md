# V3 fitting references: complete outputs, gemma4 cost unqualified

**Both models completed all 96 fitting cases, with ten planned loads each. The final resource audit found Brave active during gemma4, violating the owner's closed-browser timing condition. Stop before candidate code/fitting.** Gemma3 is the completed reference under the requested conditions; gemma4's recorded answers remain descriptive, but its timing/memory comparison is unqualified. Do not drop the affected cases, reuse the earlier v2 partial, or silently rerun selected cases.

V3 content hash `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`; source commit `a6e95c3f6238d9ed057874ab4d7ac1f48540dd20`. V3 flags were corrected/frozen in `b075740`; both commits passed Release builds (zero warnings/errors), all 990 tests, and were pushed before timing. Production reader/provider/parser/evaluator, catalogue and prompt were unchanged; gemma4 uses provider `Think = false`, gemma3 omits the unsupported thinking option.

## Protocol and audit

All 96 fitting manifest members, frozen file order, given claim types. Context 4096, GPU layers 0, temperature .2, repeat penalty 1.1, 300 answer tokens. Every ten complete cases: unload, empty CPU/context-matched warm-up, fresh monitor checks. Last block has six cases. Case time excludes reloads; total amortised bill includes unloads, loads, waits and host orchestration. Empty warm-up responses omit native `load_duration`; only observed wall time is reported, with native time unavailable.

Mains and actual/effective/configured **Best performance** throughout: all **941 gemma3 + 754 gemma4** timed-window samples pass. Windows build 26100, 12 logical processors, visible physical 16,836,857,856 bytes (15.681 GiB), Ollama 0.40.15. The Balanced base scheme is distinct from the verified Best-performance overlay. Real idle desktop app resident; model/server/app memory sampled about every two seconds, system paging about every ten. Builds/tests finished and build servers stopped before timing. Initial available physical about 10.2 GiB, preflight other CPU below .5 cores; no browser/VS Code at start.

Independent audit: **192/192 reader requests and 20/20 planned reloads**, zero wire/source-text mismatches, every complete source sentence present, exact fitting membership/order, no audit case, no truncation flag, no transport failure/retry. Reloads occur at fitting ordinals 1/11/.../91, not necessarily those case IDs because audit families are skipped. Each original response/output is retained. The one gemma3 cap is a semantic reference failure with partial output retained under the preregistered protocol, without raising the cap.

### Timing-condition failure and repair

Brave appears in **20 resource samples from 21:53:09.059 to 21:53:50.076 UTC**, in gemma4 phases `a03-114`, `a03-115`, `a03-116` and reload 9. Total other CPU reached **5.698 core equivalents**; minimum machine available was **0.841 GiB**. The 512 MiB guard still passed, but the separate closed-browser condition did not. The old monitor recorded only the top three CPU processes; this is an observed interval, not proof of the exact browser lifetime. No active browser was observed in the gemma3 samples.

The start-only browser check was insufficient. The agent missed the reopening live; final audit caught it. Therefore gemma4 seconds are **informative only**, and a complete qualified two-model cost comparison remains unavailable. Retain all 96 outputs and all resource samples; no selective exclusion, replacement or claims that another job cannot affect timing. A brief gemma3 Windows servicing/WMI burst around 21:13:56–21:14:03 UTC is also retained; no service settings were changed.

The repaired `MeasurementGuard` directly checks browser/VS Code process presence before **every reader HTTP request**, after planned unload and after warm-up, in addition to power/app/freshness/512 MiB checks. It records exact reason, process IDs/names and sample before stopping; no automatic reset/retry. This repair was made after inference and is not attributed to the measured source commit.

No-model end-to-end guard checks passed: clean fixture accepted; 511 MiB rejected; battery rejected; a benign short-lived process named `brave` rejected with its exact owned PID; clean recovery accepted. The fixture has no browser/network/model behavior, stops normally via its owned marker (30-second maximum), and touches no owner process. Tests/resources live outside the corpus, with no new legal case. Full post-commit Release/test checks are reported in the handoff.

## Complete descriptive reference outputs

These are author-known fitting results, not audit/hidden accuracy. They do not make either model the runtime model or qualify a new reader. Gemma4 resource/timing columns are marked unqualified throughout.

| Alpha-01 reference | All cases without critical error | Polarity kind | Binding kind | Cap hits |
|---|---:|---:|---:|---:|
| gemma3:4b | 36/96 (37.5%) | 23/72 | 13/24 | 1 |
| gemma4:e4b, thinking off | 69/96 (71.9%) | 46/72 | 23/24 | 0 |

| Critical error | Gemma3 occurrences / cases | Gemma4 occurrences / cases |
|---|---:|---:|
| Missing decisive event | 44 / 37 | 12 / 12 |
| Forbidden affirmation | 26 / 24 | 16 / 13 |
| Wrong returned polarity | 24 / 21 | 9 / 8 |
| Date on wrong event | 11 / 8 | 1 / 1 |
| Wrong requested amount | 4 / 3 | 1 / 1 |
| Plaintiff position | 1 / 1 | 0 / 0 |

Other critical types, including wrong event amount, swapped actor and invented value: zero. Counts overlap. Noncritical differences: gemma3 missing non-decisive event 18, omitted date 16, omitted event amount 16, missing request 8, unreadable lines in six cases; gemma4 8, 0, 2, 2 and ten cases respectively. The legacy conditional date/polarity counts differ from dense completeness; do not equate them.

Dense metrics retain every label, including denied backgrounds now non-decisive:

| Metric | Gemma3 | Gemma4 |
|---|---:|---:|
| All statuses correct | 275/372 | 329/372 |
| Unresolved statuses | 6 | 0 |
| Non-affirmed errors | 71/160 | 35/160 |
| False affirmations | 26 | 16 |
| Denied labels correct | 0/47 | 28/47 |
| Disputed labels correct | 5/18 | 16/18 |
| Denied target recall | 0/18 | 9/18 |
| Disputed target recall | 5/18 | 16/18 |
| Affirmed target recall | 15/18 | 17/18 |
| Explicit absent targets correct | 17/18 | 18/18 |
| Known event dates correct / omitted / wrong | 130 / 25 / 11 of 166 | 164 / 1 / 1 of 166 |
| Known event amounts correct / omitted / wrong | 57 / 17 / 0 of 74 | 72 / 2 / 0 of 74 |
| Request presence correct | 205/228 | 226/228 |
| Missing / extra requests | 8 / 15 | 2 / 0 |
| Known request amounts correct / omitted / wrong | 77 / 6 / 4 of 87 | 86 / 0 / 1 of 87 |
| Non-absent events with any source | 172/277 | 181/277 |
| Non-absent events with gold-source overlap | 146/277 | 172/277 |

Missing/unreadable rows are unresolved, never automatically correct absent. The six gemma3 unresolved statuses come with the capped case; all outputs remain included. Source overlap is a diagnostic of the old value-derived citations, not semantic verification or source precision. No new evidence selector was run. Incomplete returned values stay visible despite noncritical omission scoring.

Whole-case critical failures by fold (32 cases each): gemma3 **22, 22, 16**; gemma4 **16, 7, 4**. No calibration/order/candidate selection was fitted to these outputs. The catalogue's separate non-delivery-of-a-thing and stopped/breached-works questions remain as frozen; no polarity was changed from model answers.

## Time, loads and sampled memory

| Metric | Gemma3 | Gemma4 — UNQUALIFIED timing |
|---|---:|---:|
| Case mean / median / p95 / max, seconds | 20.212 / 17.138 / 31.534 / 42.566 | 15.598 / 13.977 / 24.149 / 25.325 |
| Total batch / amortised seconds per case | 1992.620 / 20.756 | 1598.079 / 16.647 |
| Polarity case mean / sampled model-server WS peak | 19.455 s / 6.422 GiB | 15.207 s / 7.214 GiB |
| Binding case mean / sampled model-server WS peak | 22.482 s / 5.769 GiB | 16.771 s / 7.190 GiB |
| Sampled model/server WS / private peak | 6.422 / 6.898 GiB | 7.214 / 7.999 GiB |
| Concurrent model/server plus app WS peak | 6.581 GiB | 7.317 GiB |
| Machine available physical minimum | 4.269 GiB | 0.841 GiB, browser-confounded |
| Whole-machine commit peak | 13.167 GiB | 17.575 GiB, browser-confounded |
| Other CPU mean / p95 / maximum, cores | .384 / .613 / 2.028 | .566 / 1.145 / 5.698 |
| Sampler PowerShell mean cores | .018 | .019 |
| Fresh paging samples; peak reads / input / output per second | 188; 30 / 121 / 0 | 151; 1368 / 327123 / 0 |

p95 is nearest rank. Per-kind resource peaks are chronological within resident ten-case blocks; loads are reported at model level, not allocated selectively to a kind. WS sums can double-count shared pages and sampling misses short spikes. The large gemma4 page-input peaks occurred in load phases, where file reads are expected; case-phase paging and browser activity remain visible in raw data. No claim of no paging, thermal qualification, unrestricted residence stability or a memory-growth cause investigation.

Each warm-up wall time in seconds (native load duration unavailable):

- Gemma3: **3.870059, 2.868166, 2.842932, 2.862130, 2.869652, 2.870610, 2.830894, 2.834642, 2.863052, 2.851270**. Unloads total .119619 seconds.
- Gemma4: **8.676873, 7.953561, 7.151857, 7.426206, 7.429952, 7.169531, 7.193238, 7.847353, 9.061379, 7.583652**. Unloads total .121885 seconds.

The reference provider does not expose cached-token counts in `ModelTimings`. Its prompt-token/time ratio can include cache effects; do not describe it as uncached prompt throughput. Raw scored-chain cache counters are still required in the future capability gate. Cap case `a03-130` took 42.566 seconds and retains its partial answer; no cap increase/retry.

Installed packages: gemma3 4.3B Q4_K_M, 3,349,593,375 disk bytes, Gemma Terms of Use, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; gemma4 7.5B Q4_K_M, 6,583,656,505 bytes, Apache 2.0, digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`. Exact licence metadata remains in raw model-show snapshots. No weights bundled or runtime-model switch.

## Reproduction, artifacts and stop

```powershell
dotnet build tools/alpha-03/FittingReferences/FittingReferences.csproj -c Release
# Finish builds/tests; stop build servers; close browser/heavy apps; verify quiet power/app/monitor.
& tools/alpha-03/Measure-FittingReferences.ps1 -RunRoot $newExternalRoot
# After normal cleanup and monitor termination:
python -B tools/alpha-03/audit_references.py $newExternalRoot
python -B tools/alpha-03/summarize_fitting_references.py $newExternalRoot
python -B tools/alpha-03/freeze_cases.py --verify
# No-model guard regression, with no real browser present:
dotnet build tools/alpha-03/GuardCheck/GuardCheck.csproj -c Release
& tools/alpha-03/Test-MeasurementGuard.ps1 -OutputRoot $newExternalGuardCheckRoot
```

Raw root `%TEMP%\dzlegal-alpha03-v3-fitting-20261107-215737`: all 192 outgoing reader requests, 20 unload/warm-up pairs and responses, exact case facts/answers/errors/native timings, manifests/source-binary hashes, power/resource logs, original evaluator reports and cleanup. `reference-wire-audit.json` and `reference-analysis.json` are derived reproducible summaries. Outputs remain external. `artifact-hashes.json` excludes itself and the disposable console trace `analysis-stdout.txt` to avoid self/changing-output fingerprints.

| Artifact | SHA-256 |
|---|---|
| `resources.jsonl` | `64ede778490d5c4f178ac3a13043271c4ffd90e21b80657b6d11030e0b96b8a4` |
| `reference-analysis.json` | `20d54d9159155b17a7d41298f90dab8276582deee1c74e8f3895612da3550e13` |
| `reference-wire-audit.json` | `28f3ad6bfbb8b225b5637595d89ec5e20cd9e0e5fcd26b74d6ba0b73ee6400db` |
| `artifact-hashes.json` | `13df8028ae118a32f4daf56663fa8512d3b340838056f52564d138d7bceb8ca9` |

Owned models unloaded, app closed normally and monitor stopped. Search remained **Running/Automatic**, never paused in this run; Best performance retained. Current v3 hash re-verified. No scored reader/prompt, candidate inference, calibration, binding, audit or original validation check was started. `FITTING-PROTOCOL.md` declares the finite grouped grid/selection/cost accounting before any scored inference; it does not authorise bypassing this stop.

**Stop under brief §8's precondition rule and the owner's closed-browser condition.** Suggested owner decision: retain the completed gemma3 reference; rerun **all 96 gemma4 cases from case 1** under the repaired guard, retaining this record as unqualified and excluding it from the eventual timing/model selection comparison. No selective affected-case retry. Then continue the previously authorised fitting/locked-primary audit. This proposal is prepared, not executed pending that decision.

Files outside `tools/alpha-03/` in this measurement/guard checkpoint: this summary and `docs/ALPHA-03.md` only. V3 corpus remains unchanged. Unit tests changed/skipped/weakened: none; five no-model guard checks added. No owner process was killed, hidden material accessed, merge/PR performed or runtime-model change made.
