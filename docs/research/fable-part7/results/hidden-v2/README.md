# Hidden set v2: the five generative models (12 November 2026)

Fact reading with the unchanged Alpha-01 reader, claim type given, 90 cases read and 10 out-of-catalogue cases left out, one serial pass per model, no retry of a completed stage. Blind operator: only each run's `summary.md` and the run indexes were read; the hidden folder and every other file of the run folders were never opened. The summaries here are copies **without their 100 per-case lines** (hidden case names with error types), as in the committed comparison of 9 November; the full files stay on the laptop.

**Power.** Three models completed on battery (Windows mode "Best power efficiency", energy saver never on during a counted stage). Two completed on mains with Best performance, at the owner's decision, after their battery stages were interrupted (see the last section). Accuracy does not depend on the power source; **time does**, so battery and mains times are in separate columns and must not be compared with each other.

## Accuracy (every field of the summary table)

"Fully right" is the scorer's count of cases without a critical error. The four Gemma rows are the committed figures of 8 and 9 November (`docs/baselines/2026-11-09-alpha-03-hidden-v2-phone-models/summary.md` on `codex/alpha-03`), not rerun.

| Model | File | Fully right (of 90) | With a critical error | Plaintiff position | Decisive events found | Dates on the right event | Polarity right | Requests found | Failures |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Gemma 4 E4B (the app's model; committed) | 5.49 GB + 0.99 GB vision | **58** | 32 | 100 % | 98 % | 92 % | 91 % | 95 % | 0 |
| **Jais-3-3.7B**, no-trace copy | 3.16 GB | **50** | 40 | 94 % | 97 % | 80 % | 90 % | 98 % | 0 |
| Gemma 3n E2B (committed) | 7.8 GB installed | 42 | 48 | 97 % | 93 % | 77 % | 86 % | 69 % | 0 |
| Gemma 4 E2B (committed) | 4.6 GB installed | 41 | 49 | 93 % | 93 % | 89 % | 85 % | 63 % | 0 |
| Gemma 3 4B (committed) | 3.3 GB installed | 39 | 51 | 94 % | 89 % | 83 % | 86 % | 92 % | 0 |
| **Falcon-H1-1.5B-Deep** | 0.94 GB | **25** | 65 | 86 % | 86 % | 58 % | 86 % | 79 % | 0 |
| **Qwen3.5-2B**, 4-bit, thinking off | 1.28 GB (1.95 GB installed) | **21** | 69 | 90 % | 93 % | 58 % | 73 % | 77 % | 0 |
| **Jais-3-0.9B**, no-trace copy | 0.67 GB | **14** | 76 | 82 % | 52 % | 70 % | 76 % | 49 % | 0 |
| **jais-family-590m** | 0.50 GB | **2** | 88 | 81 % | 1 % | 100 % (of the few it gave) | 50 % | 0 % | 0 |

**Critical errors by kind** (a case can have several):

| Model | Polarity | Missing decisive event | Date on wrong event | Amount on wrong event | Plaintiff position | Forbidden reading | Request amount | Actor swapped |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Jais-3-3.7B | 23 | 4 | 6 | 8 | 5 | 6 | 3 | 2 |
| Falcon-H1-1.5B-Deep | 32 | 19 | 27 | 17 | 13 | 9 | 8 | 3 |
| Qwen3.5-2B | 70 | 10 | 10 | 16 | 9 | 14 | 23 | 1 |
| Jais-3-0.9B | 34 | 64 | 9 | 6 | 16 | 11 | 1 | 2 |
| jais-family-590m | 1 | 133 | 0 | 0 | 17 | 0 | 0 | 0 |

**Other differences** (not critical): Jais-3-3.7B: unreadable lines 90, missing event 31, date not given 25, amount not given 11, relation 3, missing request 3. Falcon: missing request 37, date not given 35, unreadable lines 25, missing event 23, amount not given 7, relation 5. Qwen3.5-2B: date not given 55, missing request 40, unreadable lines 15, amount not given 10, missing event 5, relation 4. Jais-3-0.9B: unreadable lines 90, missing request 90, missing event 66, date not given 16, amount not given 14, relation 8. jais-family-590m: missing request 176, missing event 135, unreadable lines 83, relation 8.

- The "unreadable lines 90" of the two Jais 3 models are the two marker lines that open every answer of the no-trace copy (the parsers skip them); they are not critical errors.

## Time per case, power and battery

Seconds per read case are the scorer's rounded figure; "per attempted file" is the whole batch (100 files, 10 loads, waits) divided by 100.

| Model | Power during the counted stage | Seconds per case, **battery** | Seconds per case, **mains** | Per attempted file (s) | Answer tokens (avg) | Prompt read (tokens/s) | Writing (tokens/s) | Battery used by the stage |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Jais-3-0.9B | battery, Best power efficiency | 17 | | 15.576 | 178 | 134.9 | 17.2 | 21 points (74 → 53 %) |
| jais-family-590m | battery, Best power efficiency | 10 | | 9.418 | 210 | 249.6 | 31.4 | 13 points (53 → 40 %) |
| Qwen3.5-2B | battery, Best power efficiency | 16 | | 15.338 | 46 | 76.6 | 13.6 | 21 points (57 → 36 %) |
| Falcon-H1-1.5B-Deep | mains, Best performance | | 16 | 14.401 | 52 | 87.7 | 17.4 | not on battery |
| Jais-3-3.7B | mains, Best performance | | 20 | 18.148 | 41 | 51.7 | 8.4 | not on battery |
| Gemma 4 E4B (committed, 8 Nov) | mains, Best performance | | 21 | 20.019 | | | | |
| Gemma 4 E2B (committed, 9 Nov) | mains, Best performance | | 7 | 7.194 | 54 | 249.7 | 19.7 | |
| Gemma 3n E2B (committed, 9 Nov) | mains, Best performance | | 19 | 18.081 | 48 | 69.1 | 14.1 | |
| Gemma 3 4B (committed, 8 Nov) | mains, Best performance | | 28 | 25.919 | | | | |

- Battery drain under load was about 0.8 points per minute (21 points in 26 minutes; 13 in 16; 21 in 26).
- The two interrupted battery attempts of Jais-3-3.7B are not counted; the runner's console showed 38 of 100 files after 1,120 seconds in the second one, with energy saver on for part of it.

## Memory (sampled every two seconds; short peaks can be missed)

| Model | Model working-set peak | Model private peak | Model + app peak | Lowest memory left on the machine |
|---|---:|---:|---:|---:|
| Jais-3-0.9B | 1.440 GiB | 1.504 GiB | 1.698 GiB | 9.116 GiB |
| jais-family-590m | 1.682 GiB | 1.745 GiB | 1.904 GiB | 8.928 GiB |
| Qwen3.5-2B | 2.780 GiB | 3.594 GiB | 3.031 GiB | 8.484 GiB |
| Jais-3-3.7B | 4.597 GiB | 4.683 GiB | 4.763 GiB | 6.924 GiB |
| Falcon-H1-1.5B-Deep | 5.352 GiB | 5.559 GiB | 5.520 GiB | 5.996 GiB |
| Gemma 4 E4B (committed) | 7.244 GiB | 7.968 GiB | 7.424 GiB | 3.684 GiB |
| Gemma 4 E2B (committed) | 4.844 GiB | 5.537 GiB | 5.094 GiB | 6.003 GiB |
| Gemma 3n E2B (committed) | 6.674 GiB | 7.574 GiB | 6.858 GiB | 4.537 GiB |
| Gemma 3 4B (committed) | 6.237 GiB | 6.620 GiB | 6.484 GiB | 3.684 GiB |

- Falcon-H1-1.5B-Deep needs more memory than Jais-3-3.7B (5.4 against 4.6 GiB) for a file a third of the size. No model stays under 1 GiB.

## What the sessions were

| Run folder | Stage | Result | Battery | Why it ended |
|---|---|---|---|---|
| `battery-facts-1` | Jais-3-0.9B | complete | 74 → 53 % | |
| `battery-facts-1` | jais-family-590m | complete | 53 → 40 % | |
| `battery-facts-1` | Falcon-H1-1.5B-Deep | not complete, not counted | 40 → 26 % | the charger was plugged in during the stage; the run then stopped ("the laptop is on mains") |
| `battery-facts-2` | Qwen3.5-2B | complete | 57 → 36 % | |
| `battery-facts-2` | Jais-3-3.7B | not complete, not counted | 36 → 14 % | the 15 % limit; energy saver was on for 188 samples (its threshold was still 30 %) |
| `battery-facts-3` | Jais-3-3.7B | not complete, not counted | 32 → 18 % | stopped by the harness above the 15 % limit (the charger was plugged in, by the owner's account; the stage folder was not opened to confirm); energy saver on for 70 samples |
| `mains-facts-1` | Falcon-H1-1.5B-Deep | complete | mains | |
| `mains-facts-1` | Jais-3-3.7B | complete | mains | |

- The three counted battery stages have zero energy-saver samples and zero invalid power or headroom samples; so do the two mains stages.
- A weakness of the battery runner, to fix before session 2: after the Falcon stage was interrupted, the resume command it printed started at the next stage, so Falcon was listed under "did not complete" but left out of the resume.
- The energy-saver threshold was still 30 % on 12 November at 20:00, not 10 %.
