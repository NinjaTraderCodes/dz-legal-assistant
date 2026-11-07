# Alpha-03: same-order repeat and blind history

**Same-order answers were stable in this one repeat. Blind history met the token/score/cache conditions, but did not remove order dependence: 4/17 argmaxes changed.** These are bounded diagnostics, not a calibrated reader or an accuracy result.

## Scope and conditions

Owner-authorised follow-up to the accepted gate-2 run, at source commit `e62ff1ca3bbb87c5c84be57bb214e09dbebcd742`. Only the same three preselected original development cases were used: `loan-no-section`, `loan-basic`, `non-delivery-machines`. No new case, prompt wording, reader, calibration, validation model check or hidden-data access.

The owner expressly authorised parts 1 and 2 **on battery**. All 139 resource samples during these chains report AC disconnected, battery 81–77%, actual/effective overlay `961cc777-2547-4f9d-8174-7d86181b8a7a`. Configured AC mode stayed Best performance (`ded574b5-45a0-4f42-8737-46345c09c238`). Background activity averaged 1.578 CPU-core equivalents, maximum 3.144. **Seconds below are informative only, not cost measurements; do not pool them with the mains gate-2 timings.**

Ollama `0.40.15`; CPU-only `gemma3:4b`, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`, local licence metadata Gemma Terms of Use. All model/sampling/context options and original question messages were reused from the accepted gate-2 artifacts: temperature 0.2, seed 0, repeat penalty 1.1, context 4096, output cap one token, keep-alive 15 minutes. Every chain began with unload and a separate empty context/CPU-matched warm-up. Calls were serial.

## Same-order repeat

Three forward chains, with each newly generated answer retained literally. No old answer was replayed into the new history.

| Case | Positions | Generated changes versus old forward | Argmax changes versus old forward | Old forward/reverse generated changes | Old forward/reverse argmax changes |
|---|---:|---:|---:|---:|---:|
| loan-no-section | 6 | 0 | 0 | 3 | 3 |
| loan-basic | 6 | 0 | 0 | 2 | 3 |
| non-delivery-machines | 5 | 0 | 0 | 1 | 2 |
| **Total** | **17** | **0** | **0** | **6** | **8** |

This control supports an order/history explanation of the earlier differences on these cases: the observed same-order label variation was zero. One repeat on three cases is not a distributional or causal guarantee. Raw score vectors need not be identical merely because argmaxes agree.

## Blind history

The first neutral attempt used schema `{"type":"null"}` with the unchanged one-token cap. It emitted only `n` (one token, `done_reason: length`), not complete `null`. That attempt was unusable and is retained separately. The sole allowed fallback, schema `{"type":"integer","enum":[0]}`, then worked and was fixed for all six completed blind chains. No cap was increased and no other constant was searched.

Every blind answer was literal `0`, outside status labels `1` affirmed, `2` denied, `3` disputed, `4` absent. Every extension included all preceding messages unchanged and the actual forced `0`; no sampled/argmax status was inserted into history. Four finite alternatives were read from `top_logprobs: 20`, before the schema constrained the emitted token. The forced zero is a carrier, never a predicted class.

| Arm | Usable positions / four finite scores | Fresh calls cached zero | Extensions with reuse | Cached tokens per extension | Maximum prompt tokens |
|---|---:|---:|---:|---:|---:|
| Repeat forward | 17/17 | 3/3 | 14/14 | 373–526 | 571 |
| Blind forward + reverse | 34/34 | 6/6 | 28/28 | 373–529 | 571 |

| Case | Blind forward/reverse argmax changes |
|---|---:|
| loan-no-section | 2/6 |
| loan-basic | 2/6 |
| non-delivery-machines | 0/5 |
| **Total** | **4/17** |

The remaining changes were `payment_made` (affirmed → disputed) and `notice_served` (affirmed → disputed) in `loan-no-section`; `debt_acknowledged` (affirmed → disputed) and `deadline_passed` (disputed → affirmed) in `loan-basic`. These are score argmaxes, without calibration or an accuracy claim against the sparse all-affirmed key.

Blind history removes the earlier **choices**, but later positions still see earlier **questions**, their order and fixed zeros. A zero is not guaranteed semantically inert. The 8/17 to 4/17 observed reduction does not establish a general improvement or justify selecting/calibrating a reader on these three cases. Cache capability passes on this shape; semantic order robustness remains unresolved.

## Informative elapsed seconds on battery

Chain wall seconds exclude the separately recorded warm-up and include local orchestration.

| Case | Repeat forward | Blind forward | Blind reverse |
|---|---:|---:|---:|
| loan-no-section | 23.681 | 23.843 | 23.392 |
| loan-basic | 25.500 | 26.055 | 25.328 |
| non-delivery-machines | 23.841 | 23.463 | 22.765 |

These seconds do not qualify, revise or invalidate the accepted mains cost gate.

## Reproduction and audit

From the repository root, after resource/app preparation:

```powershell
& tools/alpha-03/Measure-BlindHistory.ps1 -RunRoot $runRoot -PriorRoot $acceptedGate2Directory -AllowBatteryDiagnostic
& tools/alpha-03/Analyze-BlindHistory.ps1 -RunRoot $runRoot -PriorRoot $acceptedGate2Directory
```

Raw outputs: `%TEMP%\dzlegal-alpha03-blind-followup-20261107-191514`. Prior run: `%TEMP%\dzlegal-alpha03-gate2-clean-20261107-165004`. No files from the prior run were edited. The prior original-development manifest SHA-256 is checked against `9ff1cda372f8c9846d918b2ebc7b86945f93084a1536d11e1ab8dd1153f0581c`.

Independent raw-artifact audit: **51 usable requests, zero message/option/token/score/cache mismatches**. Numeric score comparison allows at most `1e-12` for Windows PowerShell JSON round trips; observed maximum discrepancy was `1.7763568394002505e-15`. Raw responses remain authoritative. The audit checks byte-exact emitted tokens, exact message roles/content and preservation, schema isolation, no status-bearing assistant history in the blind arm, four finite class scores, and first/extension cache counters. The failed `null` attempt is disclosed, not included in the 51 usable positions.

SHA-256:

| Artifact | Hash |
|---|---|
| `blind-history-plan.json` | `ed6fd8f8ae4947e9940df727445a997749ccf9ab5138126d07dca0ab6aae0370` |
| `followup-chains.json` | `bc27584b3a0e8edd8afd2ec8782cfdb77b04bb56b92d5a0f80d4fc11b083185d` |
| `blind-history-audit.json` | `23c9e93c94f2104d4d765d75c28d3e8466387a6eb06a3647ae5a185035897b9d` |

The event-by-event generated/argmax matrix and complete four-score vectors are retained in the external audit/chains files; requests, raw responses, power and resource records remain alongside them. The separate owner note about the **previous resident gemma3 batch** is outside this probe; no investigation of it was undertaken.
