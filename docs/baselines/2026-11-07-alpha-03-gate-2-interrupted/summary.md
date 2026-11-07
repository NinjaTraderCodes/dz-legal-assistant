# Alpha-03 gate 2 — interrupted for competing CPU work

7 November 2026. Harness/reader commit `5dc52cbf24b670f6f9dad39d294676e29bd2b1ff`, branch `codex/alpha-03`. Measurement tools are still local changes. **Gate 2 is unresolved; rung 0 and the reverse-order check have not run.**

**Power for every recorded timed chain: mains, Best performance.** Actual, effective and configured AC mode GUID: `ded574b5-45a0-4f42-8737-46345c09c238`. All 170 sampled timed-phase records agree; zero observed power violations. The Balanced base plan remained active, independently of that performance mode. Before inference, the Windows AC-specific API had reported efficiency mode despite the owner's UI setting; the requested Best performance AC setting was applied and verified. See [Microsoft's mode definitions](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powergetuserconfiguredacpowermode).

## Why this run stopped

The owner required timings with nothing else heavy running. Monitoring found sustained competing work from VS Code's C# Dev Kit server, identified by executable/parent metadata without reading command-line arguments or case files. Across timed-phase samples, other processes averaged **1.461 CPU-core equivalents**, peaking at **4.276**; 103/170 samples exceeded one core. The C# server reached **2.120 cores**. The benchmark was interrupted after 21 completed chains. The owner then explained that this agent runs inside VS Code's terminal and requested a pause to move the session to standalone PowerShell.

These timings are retained as **contended diagnostic observations**, not qualifying gate-2 measurements. A complete fresh run must use a new output directory after CPU conditions are checked. Do not pool it with this interrupted run.

## What completed

| Observation | Result |
|---|---:|
| Complete forward chains | 21 of 40 planned |
| Recorded status responses | 85 of 147 planned |
| Single-token answers with all four finite class scores | 85/85 |
| Fresh first requests with zero cached tokens | 21/21 |
| Extensions with positive cache reuse | 64/64 |
| Cached tokens in extensions | 355–526 |
| Maximum prompt tokens | 571 of 4,096 |
| Diagnostic chain mean / min / max | 12.780 / 11.049 / 14.811 s |
| Separate empty warm-up mean | 4.225 s |
| Reverse-order chains | 0 |
| Alpha-01 rung-0 inferences | 0 |

Every completed chain asked all catalogue event questions for that claim, carrying actual generated answers verbatim into later turns. Roles, value binding and requests still require the retained Alpha-01 pass, whose cost has not yet been measured here. The chain figures therefore cannot establish a complete reader's cost or reading accuracy. Earlier-answer dependence remains the design trade-off documented in Report 1.

CPU-only `gemma3:4b`, Ollama `0.40.15`; context 4,096, temperature 0.2, repeat penalty 1.1, seed 0, one output token, integer-enum format, `logprobs: true`, `top_logprobs: 20`, keep-alive 15 minutes. The accepted gate-1 fixture's wording/settings were reused. Each case began after unload and an empty context/CPU-matched warm-up. Loading is separate from chain timing.

Model digest: `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`; installed licence metadata: **Gemma Terms of Use**. `gemma4:e4b` was not called; its installed metadata remains Apache 2.0, digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`.

## Memory and limits

The idle desktop app was resident. Sampled simultaneous Ollama/runner working sets peaked at **4.019 GiB**, private bytes at **4.419 GiB**; app residence during timed samples ranged **154–176 MiB**. Whole-machine available physical memory fell as low as **0.045 GiB, about 46 MiB**. This is substantial memory pressure; model working set alone does not qualify the 16 GB requirement. The monitor retains system page-read/input/output rates to distinguish loading from sustained paging in a clean run. No memory qualification follows from this interrupted sample.

Working-set sums may count shared pages twice. Sampling is approximately every two seconds, with paging counters refreshed approximately every ten seconds, so brief peaks can be missed. Startup/preparation includes monitor validation and restarts before inference. A PowerShell 5 array-wrapping error was caught by the input guard and corrected before any model call. No wording or case was changed from model correctness.

## Artifacts and handoff

Raw files remain outside the repository at `%TEMP%\dzlegal-alpha03-gate2-20261107`. They include `gate2-plan.json`, `gate2-results.json`, `calls.jsonl`, per-chain requests/responses and power records, `resources.jsonl`, model metadata, tool hashes, `power-correction.json`, `interrupted-cpu-load.json` and `interrupted-statistics.json`.

| Artifact | SHA-256 |
|---|---|
| Original development selection manifest | `9ff1cda372f8c9846d918b2ebc7b86945f93084a1536d11e1ab8dd1153f0581c` |
| `calls.jsonl` | `5c6e4990ca507685e500ab1e3cb3deab11260bbde4edde0df63520b7cd039934` |
| `gate2-results.json` | `a85f5827e91f0078013b86a7c8ec875de551d1c16b5c0d4ece704dd8149bb9ea` |

At handoff the benchmark was stopped, its model unloaded, the resource monitor stopped and the idle app asked to close normally. VS Code and the owner's processes were left untouched. Best performance stays enabled. Resume instructions are in `tools/alpha-03/RESUME.md`.

No validation/hidden input, new cases, candidate prompt or reader implementation was used. No existing source, original prompt/catalogue, original cases or tests changed. Tests added/changed: none. The last committed code passed its Release build and all 990 tests with serial xUnit collections; these pending tools/results have not yet been committed or had their required post-commit checks.
