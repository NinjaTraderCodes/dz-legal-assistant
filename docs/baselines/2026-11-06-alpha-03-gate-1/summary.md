# Alpha-03 gate 1 — four choices available; suffix reuse not observed

6 November 2026. Reader/code baseline: `5a3f846eddaf896b383c2027aeb21c0744ed4a0c`, `codex/alpha-03`. This is an API capability probe, **not an accuracy benchmark**, new reader, or hidden-set run.

**Outcome: stop.** The four-label score prerequisite worked on the requests below; cheap changing-suffix reuse did not. Gate 2, rung 0, synthetic case writing and reader/prompt implementation have not started.

## Environment and request contract

- Endpoint: `http://127.0.0.1:11434/api/chat`, redirects refused; one request at a time.
- Ollama: `0.40.15`; `gemma3:4b`, digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`.
- CPU only (`num_gpu: 0`), `num_ctx: 4096`, `num_predict: 1`, temperature `0.2`, repeat penalty `1.1`, keep-alive `15m`. The four case-level requests use seed `0`.
- Windows battery query reported status `2` (mains); active scheme Balanced. No persistent runtime/environment settings changed. No concurrent bench was launched.
- `stream: false`, `logprobs: true`, supported `top_logprobs: 20`. Constrained requests use a root integer schema with enum `[1,2,3,4]`.
- One empty system message and one user message. The shared user prefix uses the existing Alpha-01 status definitions, a numeric-code instruction, and numbered facts from **original development** case `loan-basic`. The final question takes each of the first three loan catalogue elements. No gold labels are sent. This temporary wire payload is a capability fixture, not a candidate Alpha-03 prompt asset.
- The first two case requests have 1,026 identical initial user-text characters; their options and schema are identical. The final control's user message is byte-for-byte identical to the preceding third question, but omits `format`.

## Results

| Request | Prompt tokens | Cached tokens | Prompt evaluation s | Wall s | First output | Finite class scores |
|---|---:|---:|---:|---:|---|---:|
| Generic numeric smoke | 37 | 0 | 1.958 | 10.777 | `3` | 4 |
| `loan-basic`, element 1 | 413 | 0 | 20.472 | 20.681 | `1` | 4 |
| Same case, element 2 suffix | 408 | 0 | 21.856 | 21.947 | `3` | 4 |
| Same case, element 3 suffix | 404 | 0 | 22.083 | 22.217 | `1` | 4 |
| Same element-3 message, unconstrained control | 404 | 399 | 0.942 | 1.011 | `1` | 4 |

Each valid request returned exactly one log-probability record and one generated token. The four class alternatives have token strings `1`, `2`, `3`, `4` and byte arrays `[49]`, `[50]`, `[51]`, `[52]`. All four scores are finite; none was fabricated for a missing alternative. The generated choices are recorded for reproducibility, not scored against the answer key.

The paired third-question scores were:

| Choice | Constrained log probability | Unconstrained control log probability |
|---|---:|---:|
| `1` | -0.001413 | -0.001512 |
| `2` | -7.587545 | -7.586201 |
| `3` | -7.257667 | -7.130989 |
| `4` | -8.832320 | -8.719225 |

The label ranking survives the control, with a maximum difference of about 0.128; this is not proof of distribution identity or calibrated confidence. The explicit cache counter and wall time distinguish the repeated-message control from the changing questions. No claim is made that all turn arrangements fail, nor that a shared textual prefix is free in the current arrangement.

An attempted `top_logprobs: 100` request received HTTP 400: `top_logprobs must be between 0 and 20`. It did not execute inference. Its local request, empty response file and failed-request diagnostic were retained; the supported-value sequence has separate filenames. The smoke probe had already demonstrated 20 works. A `length` completion at this deliberate one-token cap is expected; no fact sheet was parsed from it.

## Commands and local records

Raw requests/responses remain only at:

`C:\Users\TheShadow\AppData\Local\Temp\dzlegal-alpha03-gate1-20261106-210057`

The requests were generated in PowerShell from the unchanged original prompt definitions, the development input and the catalogue; then sent serially with the following transport (substitute each stored request filename for `<request-file>`):

```powershell
$body = Get-Content -LiteralPath '<request-file>' -Encoding UTF8 -Raw
$watch = [Diagnostics.Stopwatch]::StartNew()
$reply = Invoke-RestMethod -Uri http://127.0.0.1:11434/api/chat `
    -Method Post -ContentType 'application/json' `
    -Body ([Text.Encoding]::UTF8.GetBytes($body)) `
    -MaximumRedirection 0 -TimeoutSec 180 -ErrorAction Stop
$watch.Stop()
```

The valid sequence is `request.json` (generic smoke), then `loan-basic-top20-0-request.json` through `loan-basic-top20-3-request.json`. Each has a corresponding `response.json` or `-response.json`; `loan-basic-top20-summary.json` holds counters and timings. Replaying these calls is **not authorised by this stop record**: the experiment is stopped pending the owner's next decision. They describe how the recorded calls were made.

SHA-256 over the stored UTF-8 artifact bytes:

| Artifact | SHA-256 |
|---|---|
| `loan-basic-top20-0-request.json` | `5bdb0e9148d16d500abb6410cd5ada4292d31658eb1cbabe1429a13b777feaef` |
| `loan-basic-top20-1-request.json` | `d911a16def0284844d30809cbdd566d45eee55e3d97567a3409f2335ce86feb4` |
| `loan-basic-top20-2-request.json` | `c4a750d34634ef7e00c2375c13099fff76dd9dbc771eb861c759112de6693ee7` |
| `loan-basic-top20-3-request.json` | `e7f8a24b7a7128d8aafa5d603fe5c9b0d075273d19e62d09220980c35ea70de5` |
| `loan-basic-top20-summary.json` | `ad5fd5b13ad7be3349e9626d5b369e9a4fe86b31058f79362c650e81659cd260` |

## Limits and next decision

Only one original case and three question suffixes were used, so this does not establish accuracy or general cache behaviour. It does establish that this bounded probe did not satisfy gate 1. No peak-memory measurement or full fresh-cost claim is made; rung 0 and its 40-case time/memory comparison remain pending. No validation, robustness, hidden or new synthetic case was evaluated.

Proposed next probe, **not run**: keep the common case as an unchanged earlier chat turn and vary only the final question turn, using the existing conversation mechanism. The owner must decide whether to authorise that bounded continuation. No alternative runtime, model switch, calibration or more case writing proceeds automatically.
