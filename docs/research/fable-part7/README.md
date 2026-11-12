# Part 7 working files (Arabic-first small models, measured)

Working files behind Part 7 of `../FABLE-ARCHITECTURE-REPORT.md`, pushed so that the reviewer can read exactly what runs. Brief: `docs/briefs/FABLE-ARABIC-SMALL-MODELS-BRIEF.md` (branch `claude/case-file-site`). Nothing here is used by the app. Fictitious data only; no hidden-set text; no model or encoder weights.

State on 12 November 2026, 15:50: the model list is final and reviewed; **no guarded run has started**. The mains run planned for the night of 11 November was not launched; the owner now wants the run on battery (section 1b), and its runner awaits review. All encoder trainings are finished; the last result and the speed and memory figures are still to be added.

## 1. The guarded run of the generative models (`run/`)

| File | What it is |
|---|---|
| `run/Run-ArabicModels.ps1` | The runner the owner launches. Same guards and argument lines as `tools/alpha-03/Run-OwnerAlpha01.ps1` and `Measure-AppBenchmarks.ps1` at `480d4a4`; three stages per model: `facts` (hidden set v2, claim type given), `grounds` (the app's analysis on the 46 development cases), `draft` (the 12 locked files of stage 2, checked against `DRAFT-STAGE-2.json`). |
| `run/Run-ArabicModels-Battery.ps1` | The battery variant (section 1b). |
| `run/models.json` | The models, in running order, each with the digest recorded after download. The runner skips a model whose installed digest differs. |
| `run/harness-480d4a4.diff` | The only difference between the frozen harness (`480d4a4`, `codex/alpha-03`) and the scratch copy that runs: (1) two added lines, a comment and one switch arm, so that a model outside the four Gemma names runs in guarded mode when its recorded digest is given; (2) since 12 November, the battery mode described in section 1b (three files in all). |
| `run/make_low_effort.py` | How the two Jais 3 files were prepared: a copy of the maker's GGUF with two edits in its chat template (default reasoning effort "low"; the low-effort opening closes its empty block and ends with a line break). Weights byte-identical (hash over all tensors printed by the script). |

**The command** (PowerShell; mains power and Best performance; the desktop app open and idle in template mode; VS Code and browsers closed):

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\TheShadow\dz-arch-research\F\run\Run-ArabicModels.ps1" -RunRoot "C:\Users\TheShadow\dz-arabic-runs\night-1" -Stages "facts,grounds,draft"
```

If a model or a stage fails in the night, the follow-up is the same command with a **new** run folder and `-Only "<model name as in models.json>"` (several names separated by commas), and `-Stages` reduced to the stages still missing. A failed stage is never retried inside a run; the model's other stages still run.

**The models, in running order.**

| # | Name in `models.json` | Source file | File size (bytes) | Licence | Commercial redistribution? | Thinking value sent |
|---|---|---|---:|---|---|---|
| 1 | `jais3-0.9b-low:latest` | `IFM/Jais-3-0.9B-GGUF`, `Jais-3-0.9B-Q4_K_M.gguf`, template edited as above | 666,192,384 | Apache 2.0 | yes | omitted |
| 2 | `hf.co/mradermacher/jais-family-590m-chat-GGUF:Q4_K_M` | `jais-family-590m-chat.Q4_K_M.gguf` (community conversion of `inception42/jais-family-590m-chat`) | 503,581,952 | Apache 2.0 | yes | omitted |
| 3 | `hf.co/tiiuae/Falcon-H1-1.5B-Deep-Instruct-GGUF:Q4_K_M` | `Falcon-H1-1.5B-Deep-Instruct-Q4_K_M.gguf` | 938,465,920 | TII Falcon Licence | yes, with conditions: the acceptable-use policy passed on as an enforceable term, the licence given to recipients, and the TII statement in public statements about a derivative | omitted |
| 4 | `hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M` | `unsloth/Qwen3.5-2B-GGUF`, `Qwen3.5-2B-Q4_K_M.gguf` | 1,280,835,840 (Ollama also installs the repository's vision part, 668,227,264: 1,949,063,585 installed) | Apache 2.0 | yes | `false` (the string `"false"` in `models.json`, so that the runner adds `--think false`) |
| 5 | `jais3-3.7b-low:latest` | `IFM/Jais-3-3.7B-GGUF`, `Jais-3-3.7B-Q4_K_M.gguf`, template edited as above | 3,156,606,176 | Apache 2.0 | yes | omitted |

Licence and size sources, with links and dates: section 7.7 of the report (F1-S1, S6, S7, S14).

**Qwen3.5-2B and thinking** (checked on 11 November with the harness's options): with `think: false` it answers directly (3 answer tokens on a one-word test; 45 on average through the reader); with no thinking value it reasons for all 300 tokens and returns an empty answer. The harness switches thinking off by itself only for names that start with `qwen3`, so the value is set in `models.json`. The 8-bit copy that was on the laptop (`qwen3.5:2b`, 2.68 GB) has been removed and is not measured.

**Why Jais 3 runs as an edited copy.** As downloaded, with what the harness sends (no thinking flag, or `think: false`), Jais-3-0.9B spends all 300 answer tokens on a reasoning trace and returns an empty answer. Ollama 0.40.16 rejects `think: "low"` for this download and ignores a Modelfile template. The maker's no-trace mode is selected inside the chat template, so the template's default is changed in a copy. Each answer then begins with two marker lines that the unchanged parsers skip; the fact summaries count them as "unreadable lines", which are not critical errors.

**Not rehearsed in guarded mode.** The harness refuses to start while VS Code is open, and this session runs inside VS Code. Checked instead: the script parses; every digest in `models.json` matches the installed model; each stage ran outside guarded mode on a few development cases (smoke tests, not results); a guarded start reached the resource-monitor check, so the added switch arm works.

## 1b. Battery variant (12 November 2026, at the owner's request; not started)

The owner wants the whole measurement run on battery, as a realistic-use measurement. `run/Run-ArabicModels-Battery.ps1` is a copy of the runner above in which only the power handling and the order differ. Battery timings will be kept apart from the mains figures of 8 and 9 November in every table that shows time.

**What differs from `Run-ArabicModels.ps1`.**

| Point | Battery variant |
|---|---|
| Power | The laptop must be on battery for the whole run (`acLineStatus` 0). Best performance is not required; the Windows power mode is left as found and recorded in `power-before.json` and `power-after.json` of every stage (battery percent, time, mode, energy saver). |
| Before each stage | Battery at 25 % or more; otherwise the run stops cleanly, with nothing started. |
| During a stage | Below 15 % the runner writes the harness's own operator-stop file (`measurement.stop`); the stage is marked `complete: false`, `lowBatteryStop: true` in `RUN-INDEX.json` and is never counted. If the harness has not stopped after 5 minutes its process is ended. The run then stops. |
| When the run stops | It prints, and writes to `REMAINING.txt`, the stages that remain, in order, and the exact command to resume in a new run folder. |
| Order | By stage: `facts` for the five models (jais3-0.9b-low, jais-family-590m, Falcon-H1-1.5B-Deep, Qwen3.5-2B, jais3-3.7b-low), then `grounds` for the five, then `draft` for the five, then `gemma4:e4b` (thinking off, committed digest `dc35e8d9…`) on `grounds` and `draft` only, as the battery speed reference. A guard refuses `facts` for any `gemma` model. |
| New switches | `-StartAt <n>` starts at stage n of the plan; `-PlanOnly` prints the plan and exits without creating anything. |
| Per stage, also recorded | `energySaverSamples`: how many resource samples of the stage had Windows' energy saver on. |

Unchanged: the hidden-folder handling, the browser, editor, build and idle-app guards, the quiet-CPU preflight, the digest check, every argument line, the 12-file drafting check, fresh output folder, no retry.

**The resume command uses `-StartAt`, not only `-Only` and `-Stages`.** With the order by stage, what remains after a stop is not a rectangle of models by stages (for example two models still to do on `facts`, then everything on `grounds` and `draft`), so one `-Only`/`-Stages` pair cannot express it. The plan is deterministic, so `-StartAt n` with the same `-Stages` and `-Only` resumes exactly; the runner prints that command itself. `-Only` and `-Stages` still work as filters.

**The frozen harness refuses battery by itself**, in two places, so the runner alone could not do this: its live guard stops with "mains disconnected" or "Best performance not verified", and at the end of a fact run every resource sample taken off mains or outside Best performance is counted invalid, which makes the run incomplete. The scratch harness therefore has a battery mode, switched on only by the environment variable `DZLEGAL_PART7_POWER=battery` that the battery runner sets (`run/harness-480d4a4.diff`, now three files):

- live guard (`MeasurementGuard.cs`): in battery mode it stops with "mains connected during a battery run" if the charger is plugged in, and does not require Best performance; without the variable it behaves exactly as before;
- end-of-run sample check (`FactRunMeasurement.cs`): in battery mode a sample is invalid if the laptop was on mains; the memory and idle-app conditions are unchanged;
- labels: the summary and the run manifest say "Battery run: on battery throughout, Windows power mode as found (not Best performance)" instead of "Mains / Best performance required".

The whole scratch tree was compared with `git archive 480d4a4` (source, data, prompts, cases): only these three files differ.

**Two errors found in the runner reviewed in `1e8b3a1` / `167d753`, and fixed in both runners.** They were found by `-PlanOnly`; the mains run was never started, so nothing was lost.

1. `$PSScriptRoot` is empty inside `param()` defaults in Windows PowerShell 5.1 when a script is started with `-File`, so the default path of `models.json` failed at once. The default is now set after `param()`.
2. `@(Get-Content … | ConvertFrom-Json)` gives one element holding the whole list in Windows PowerShell 5.1, so the five models were seen as one model whose name was the five names joined, and every stage would have been skipped at the digest check. The list is now assigned first and wrapped after.

**Rehearsed** on 12 November on battery, with a stand-in for the harness (a process that waits and exits 0; no model, no hidden folder) and the guards that cannot pass inside VS Code relaxed in the rehearsal copy only: a normal stage (the stand-in received `DZLEGAL_PART7_POWER=battery`; `power-before.json`, `power-after.json` and the index were written); a stage that hits the lower battery limit (stop file written, stage marked not counted, remaining stages and resume command printed); and a battery below the before-stage minimum (nothing started). The real guarded path still cannot be rehearsed from inside VS Code.

**What was found on this laptop on battery** (12 November, 15:35, 86 %):

- The Windows power mode on battery is **Best power efficiency**, not Balanced (the power plan is Balanced). It is left as found and recorded.
- **Energy saver switches on at 30 %** (the plan's battery setting). With the limits of 25 % and 15 %, any stage that runs below 30 % runs with energy saver on, which is a different speed regime. The runner records it per stage (`energySaverSamples`), and the report will mark those timings. Keeping every timing in one regime would need the limits above 30 %, at the cost of more charges.
- Battery: 47.3 Wh at full charge (52.5 Wh by design, 763 cycles).

**Estimated minutes per stage on battery.** No model has yet been timed on battery on this laptop, so these are ranges, from the mains figures (Gemma 4 E4B: 21 s per case on the hidden set, 56 s on the analysis, 25 s per draft) and from smoke tests of each model on a busy CPU. The first stage will calibrate them: the runner prints the battery percent before and after every stage.

| Model | `facts` (100 files) | `grounds` (46 cases) | `draft` (12 files) |
|---|---:|---:|---:|
| jais3-0.9b-low | 30 to 45 | 30 to 45 | 8 to 12 |
| jais-family-590m | 8 to 15 | 8 to 15 | 3 to 6 |
| Falcon-H1-1.5B-Deep | 30 to 50 | 30 to 45 | 12 to 18 |
| Qwen3.5-2B (4-bit) | 12 to 18 | 12 to 20 | 5 to 8 |
| jais3-3.7b-low | 35 to 60 | 40 to 60 | 12 to 20 |
| gemma4:e4b (reference) | not run | 65 to 105 | 8 to 13 |

Total: about 6 to 9 hours of running. One charge gives roughly 35 to 40 Wh between 100 % and the limits, which is about 1.5 to 2.5 hours under load, so **about 3 to 6 charges**, most likely 4 or 5.

**The command** (PowerShell; charger unplugged; the desktop app open and idle in template mode; VS Code and browsers closed):

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\TheShadow\dz-arch-research\F\run\Run-ArabicModels-Battery.ps1" -RunRoot "C:\Users\TheShadow\dz-arabic-runs\battery-1"
```

To see the plan without running anything, add `-PlanOnly`.

## 2. Encoders as claim-type classifiers (`encoders/`, `synthetic-data/`)

**Task.** 22 classes: the 21 claim types and "other". Trained on synthetic notes only; tested once on the 46 development cases of `tests/grounds-cases/cases.json` (44 development, 2 robustness; 4 of them out of scope, expected "other"). For comparison on the same 46 cases, in the app's analysis: Gemma 4 E4B 46, Gemma 3 4B 41.

**Recipe, fixed before any test result** (`encoders/train_cls.py`): 5 epochs, AdamW, learning rate 3e-5 with 6 % warm-up and linear decay, batch 16, at most 160 tokens, weight decay 0.01, seed 20261111; 10 % of the synthetic notes of each class held out as a synthetic check (150 notes), 1,392 trained on. "With the section filter" allows only the claim types of the court section's area, plus "other", as the app does for the local model.

**Results so far.**

| Encoder | Licence | Parameters | Synthetic check | 46 cases, alone | 46 cases, with the section filter | Out of scope declined |
|---|---|---:|---:|---:|---:|---:|
| `alger-ia/dziribert` | Apache 2.0 | 124 M | 86.7 % | 37 | 38 | 4 of 4 |
| `jhu-clsp/mmBERT-small` | MIT | 141 M | 91.3 % | 35 | 37 | 4 of 4 |
| `microsoft/Multilingual-MiniLM-L12-H384` (alternate) | MIT | 118 M | 24.7 % | 6 | 6 | 4 of 4 |
| `CAMeL-Lab/bert-base-arabic-camelbert-mix` | Apache 2.0 | 109 M | 84.0 % | 37 | 40 | 4 of 4 |
| `intfloat/multilingual-e5-small` | MIT | 118 M | training | | | |

- The MiniLM alternate did not learn under this recipe (24.7 % on its own synthetic check); its 6 of 46 are the four out-of-scope cases and two others. It says the recipe is too short for that shape, not that the shape cannot do the task.
- Per-case errors are in `encoders/results/cls-<name>.json`.
- Training seconds in those files are **not** clean timings: other work ran on the laptop at the same time.

**Compressed to 8-bit** (ONNX, dynamic quantisation; `encoders/export_measure.py`, `encoders/ort_run.py`), first pass. File size and accuracy are final; speed and memory will be re-measured on an idle CPU and are not given here.

| Encoder | 32-bit file | 8-bit file | 8-bit: 46 cases alone / with the section filter |
|---|---:|---:|---:|
| DziriBERT | 475 MB | 119 MB | 36 / 37 |
| mmBERT-small | 537 MB | 135 MB | 31 / 33 |

**Synthetic data** (`synthetic-data/`): `make_plan.py` draws, for each note, the class, the language style (formal Arabic 52 %, Darija 26 %, French 14 %, formal Arabic with French terms 8 %), the length, the voice, whether a legal term may appear, and how many dates and amounts; `writer-instructions.md` is the text the four writers received; `sample-20.jsonl` holds 20 of the 1,542 notes with their plan attributes. Limits to keep in mind: "other" has 240 notes against 62 per claim type; the notes were written by Claude models, which is acceptable for this measurement, whereas a classifier shipped in the product would need data from a teacher whose terms allow it (Part 4 of the report).

## 3. Tokens per Arabic word

Measured with each model's own tokenizer on the repository's fictitious notes (57 notes, 1,261 words; `encoders/tok_per_word.py`, `encoders/results/tokens-per-word.json`). Fewer tokens per word means a shorter prompt and a faster answer for the same text.

| Tokenizer | Tokens per word |
|---|---:|
| Gemma 4 E4B and Gemma 3 4B (the app's models) | 2.27 |
| Falcon-H1-1.5B-Deep | 2.20 |
| Qwen3.5-2B | 2.14 |
| Jais-3-0.9B | 1.82 |
| Jais-3-3.7B | 1.48 |
| jais-family-590m | 1.39 |
| mmBERT-small | 2.29 |
| multilingual-e5-small, Multilingual-MiniLM | 1.77 |
| DziriBERT | 1.50 |
| CAMeLBERT-mix | 1.47 |

## 4. Draft of Part 7

`PART-7-DRAFT.md` holds the sections that are ready: 7.1 (candidates), 7.2 (method) and 7.7 (sources).

## 5. What will be added after the run

Each model's hidden v2 `summary.md` (nothing else from those folders), the analysis results on the 46 development cases, the 12 drafts per model, the remaining encoder results (with speed and memory measured on an idle CPU), and the rest of Part 7.
