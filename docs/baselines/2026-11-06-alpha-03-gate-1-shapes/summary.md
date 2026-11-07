# Alpha-03 gate 1 — earlier-turn, append-only and one-call probes

6 November 2026. Baseline commit: `759a90135aa70d3e9742479626dd341ee36f6790`, branch `codex/alpha-03`. Eight API calls, serial, on **only original development case `loan-basic`**. No new cases or candidate reader. The owner authorised these three shapes after accepting the first cache-gate stop, and explicitly requires a wait before gate 2 because a separate CPU job is running.

**Result:** gate 1 passes on **(b), append-only history**, for the three tested questions. Shape (a) fails reuse; tested shape (c) fails complete class-score availability. No gate-2 timing claim follows from the wall times below.

## Shared settings and provenance

- Ollama `0.40.15`, endpoint `http://127.0.0.1:11434/api/chat`, redirects refused, no proxy/runtime configuration changes.
- `gemma3:4b`, previously recorded digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; no other model called.
- CPU only (`num_gpu: 0`), context `4096`, temperature `0.2`, repeat penalty `1.1`, seed `0`, keep-alive `15m`, `stream: false`, `logprobs: true`, `top_logprobs: 20`.
- (a)/(b): integer enum `[1,2,3,4]`, `num_predict: 1`. (c): six-item integer-enum array, `num_predict: 64` to permit six statuses and framing; this is the sole output-length adjustment.
- Common status definitions and original case text are reused from the prior gate fixture. Element labels come from unchanged `data/law/claim-elements.json`. No gold event labels are supplied.
- (a) and (b) use the first three elements in their original order. (c) asks all six elements in that order, using an unfilled fixed six-status template. No correctness-driven follow-up or format variant was added.
- No new power-state qualification was attempted for gate 2. The owner's concurrent-CPU warning applies to these diagnostic times. No peak-memory measurement or representative latency result is claimed.

## Per-request observations

| Shape/call | Prompt tokens | Cached tokens | Prompt evaluation s | Wall s | Generated tokens | Output | Complete four-label score sets |
|---|---:|---:|---:|---:|---:|---|---:|
| a-1 | 428 | 0 | 21.409 | 33.118 | 1 | `1` | 1/1 |
| a-2 | 423 | 0 | 21.584 | 21.665 | 1 | `1` | 1/1 |
| a-3 | 419 | 0 | 24.447 | 24.517 | 1 | `1` | 1/1 |
| b-1 | 413 | 0 | 17.902 | 17.955 | 1 | `1` | 1/1 |
| b-2 | 445 | 408 | 2.302 | 2.348 | 1 | `2` | 1/1 |
| b-3 | 473 | 440 | 2.461 | 2.506 | 1 | `3` | 1/1 |
| c-1 | 521 | 0 | 23.107 | 27.003 | 19 | `[1, 1, 1, 4, 1, 1]` | 5/6 |
| c-2 | 521 | 516 | 0.659 | 3.941 | 19 | `[1, 1, 4, 1, 1, 3]` | 5/6 |

Totals over the bounded calls: **(a) 79.300 s** for three calls; **(b) 22.809 s** for three; **(c) 30.944 s** for two (27.003 s initial, 3.941 s repeated). These are neither average whole-reader times nor an estimate for realistic longer notes.

(a)/(b) each end with `done_reason: length` because exactly one output token was requested. Each returned digit is exactly one token and has all four finite alternatives (`1`/`2`/`3`/`4`, bytes `[49]`/`[50]`/`[51]`/`[52]`). No partial fact sheet was parsed. Both (c) calls end normally with `done_reason: stop` and valid six-item arrays.

## Shape definitions and checks

**(a):** empty system message; unchanged common user turn containing case/instructions; fixed neutral assistant acknowledgement `حاضر.`; final user question. Only the final question changes. The acknowledgement is a fixture, not model-generated case evidence. All three requests report zero cached tokens.

**(b):** first request contains the common case and question 1 in its user message. Each following request appends the model's exact preceding one-token answer as an assistant message and then the next user question. Artifact comparison confirms that requests b-2 and b-3 each retain every previous message unchanged and add exactly two messages; each appended assistant content equals the actual previous response. No key, corrected answer or argmax replaces the generated answer. The two extensions report 408 and 440 cached tokens.

**(c):** one user message contains the unchanged case, all six element questions and a fixed `[?, ?, ?, ?, ?, ?]` template. The one-digit-only instruction from the scalar fixture is removed because six answers are required. The output schema is an array with exactly six integer items, each in `[1,2,3,4]`. This is the multi-answer/single-call arrangement tested, not a new extraction reader. Both calls return structurally valid arrays, but structure does not guarantee complete raw score alternatives.

The six (c) status token indices are **1, 4, 7, 10, 13, 16**, zero-based, verified against the parsed six-value array and reconstructed token stream. At index **1**, choice **`3`** is absent from `top_logprobs: 20` in **both** calls. At each of the remaining five status positions, all four class alternatives are present and finite. The missing score is unavailable, not known to be zero or negative infinity. Thus this tested (c) format fails the required four-score condition even though its arrays are valid. Its repeated-call cache reuse is not a rescue for that failure.

The two (c) request hashes are identical, including the fixed seed, but the returned arrays differ. No accuracy/reliability guarantee is inferred from a fixed seed. No gold scoring or model/prompt tuning was performed.

## Interpretation and pause

The observations are consistent with the owner's extension-versus-cutback hypothesis. They **do not prove** that sliding-window cache mechanics are the cause. They establish the operational difference in the tested requests: changing a final question in (a) did not reuse work, whereas literal history extension in (b) did.

Both **(b) and (c) condition later answers on earlier generated answers**. This can propagate an earlier error or change later score margins through context/repetition. Any future calibration or ablation must use the actual generated-history distribution and retain that trade-off; replacing history with correct labels would change the task. The sampled digits are reported, not judged as correct facts.

The next eligible request shape is **(b)**. **Gate 2 has not started and must wait for the owner to confirm that competing heavy CPU work has ended.** Rung 0, new cases, calibration, candidate prompt/reader implementation and validation/hidden runs remain pending. No additional request variants are run under this record.

## Local artifacts and reproduction

Raw wire requests, responses and `summary.json` are outside the repository at:

`C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-gate1-shapes-20261106-213237`

Call order was `a-1` through `a-3`, `b-1` through `b-3`, then `c-1`, `c-2`. Each stem has `-request.json` and `-response.json`. The prior gate summary's `Invoke-RestMethod` transport applies unchanged, with a 300-second per-call timeout; local wall time is measured around the HTTP call. Describing these requests is not authorisation to replay them while the explicit gate-2 pause is pending.

SHA-256 over stored request bytes:

| Artifact | SHA-256 |
|---|---|
| `a-1-request.json` | `c54cb2da917dcbaa4f6b8ae0ce25fb3a88bca9d0a8162b9c787e746a224cc0fd` |
| `a-2-request.json` | `d0ea7f23526659fd698b45801e850676a45281820e3da1bd074a343f05abd992` |
| `a-3-request.json` | `e8d2c9af41deba93e3a69aa228c63a17e41bf8b67bc624fc6198a91878e7eaa5` |
| `b-1-request.json` | `cb95460ba73413548a706f759a54d6f149eb7d70831355788a2508f159b29be3` |
| `b-2-request.json` | `f454c5c7df3bccfffe260db140434e1124fc48ea6c4e73d58850085e22803efa` |
| `b-3-request.json` | `2d73253c2b0caf85a00a042632e8dc8942073b1e051866104d7c2cb831c21e83` |
| `c-1-request.json`, `c-2-request.json` | `33028cc90b545cdf3084acb51a775c9a115eb863565763b2b3bca17690f31957` |
| `summary.json` | `da4a282cdf8e4f400e6ce9c0e4ff179758369efa7a6c70c7ed65872af336dd25` |

Only aggregate probe results, hashes and non-sensitive fixture identifiers are added to the repository. No raw case/request/response files are copied into it, and no application/harness/provider code, original prompt, catalogue, existing tests or original cases are changed.
