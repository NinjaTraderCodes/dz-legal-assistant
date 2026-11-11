# Part 7 working files (Arabic-first small models, measured)

Working files behind Part 7 of `../FABLE-ARCHITECTURE-REPORT.md`, pushed so that the reviewer can read exactly what runs. Brief: `docs/briefs/FABLE-ARABIC-SMALL-MODELS-BRIEF.md` (branch `claude/case-file-site`). Nothing here is used by the app. Fictitious data only; no hidden-set text; no model or encoder weights.

State on 11 November 2026, evening: the guarded run of the generative models has **not** started; two of the encoder trainings are still running.

## 1. The guarded run of the generative models (`run/`)

| File | What it is |
|---|---|
| `run/Run-ArabicModels.ps1` | The runner the owner launches. Same guards and argument lines as `tools/alpha-03/Run-OwnerAlpha01.ps1` and `Measure-AppBenchmarks.ps1` at `480d4a4`; three stages per model: `facts` (hidden set v2, claim type given), `grounds` (the app's analysis on the 46 development cases), `draft` (the 12 locked files of stage 2, checked against `DRAFT-STAGE-2.json`). |
| `run/models.json` | The models, in running order, each with the digest recorded after download. The runner skips a model whose installed digest differs. |
| `run/harness-480d4a4.diff` | The only difference between the frozen harness (`480d4a4`, `codex/alpha-03`) and the scratch copy that runs: two added lines, a comment and one switch arm, so that a model outside the four Gemma names runs in guarded mode when its recorded digest is given. |
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
| 4 | `hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M` (digest pending: download in progress) | `unsloth/Qwen3.5-2B-GGUF`, `Qwen3.5-2B-Q4_K_M.gguf` | 1,280,835,840 | Apache 2.0 | yes | to be settled by a smoke test after the download |
| 5 | `jais3-3.7b-low:latest` | `IFM/Jais-3-3.7B-GGUF`, `Jais-3-3.7B-Q4_K_M.gguf`, template edited as above | 3,156,606,176 | Apache 2.0 | yes | omitted |

Licence and size sources, with links and dates: section 7.7 of the report (F1-S1, S6, S7, S14).

**Why Jais 3 runs as an edited copy.** As downloaded, with what the harness sends (no thinking flag, or `think: false`), Jais-3-0.9B spends all 300 answer tokens on a reasoning trace and returns an empty answer. Ollama 0.40.16 rejects `think: "low"` for this download and ignores a Modelfile template. The maker's no-trace mode is selected inside the chat template, so the template's default is changed in a copy. Each answer then begins with two marker lines that the unchanged parsers skip; the fact summaries count them as "unreadable lines", which are not critical errors.

**Not rehearsed in guarded mode.** The harness refuses to start while VS Code is open, and this session runs inside VS Code. Checked instead: the script parses; every digest in `models.json` matches the installed model; each stage ran outside guarded mode on a few development cases (smoke tests, not results); a guarded start reached the resource-monitor check, so the added switch arm works.

## 2. Encoders as claim-type classifiers (`encoders/`, `synthetic-data/`)

**Task.** 22 classes: the 21 claim types and "other". Trained on synthetic notes only; tested once on the 46 development cases of `tests/grounds-cases/cases.json` (44 development, 2 robustness; 4 of them out of scope, expected "other"). For comparison on the same 46 cases, in the app's analysis: Gemma 4 E4B 46, Gemma 3 4B 41.

**Recipe, fixed before any test result** (`encoders/train_cls.py`): 5 epochs, AdamW, learning rate 3e-5 with 6 % warm-up and linear decay, batch 16, at most 160 tokens, weight decay 0.01, seed 20261111; 10 % of the synthetic notes of each class held out as a synthetic check (150 notes), 1,392 trained on. "With the section filter" allows only the claim types of the court section's area, plus "other", as the app does for the local model.

**Results so far.**

| Encoder | Licence | Parameters | Synthetic check | 46 cases, alone | 46 cases, with the section filter | Out of scope declined |
|---|---|---:|---:|---:|---:|---:|
| `alger-ia/dziribert` | Apache 2.0 | 124 M | 86.7 % | 37 | 38 | 4 of 4 |
| `jhu-clsp/mmBERT-small` | MIT | 141 M | 91.3 % | 35 | 37 | 4 of 4 |
| `microsoft/Multilingual-MiniLM-L12-H384` (alternate) | MIT | 118 M | 24.7 % | 6 | 6 | 4 of 4 |
| `CAMeL-Lab/bert-base-arabic-camelbert-mix` | Apache 2.0 | 110 M | training | | | |
| `intfloat/multilingual-e5-small` | MIT | 118 M | to train | | | |

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

## 4. What will be added after the run

Each model's hidden v2 `summary.md` (nothing else from those folders), the analysis results on the 46 development cases, the 12 drafts per model, the remaining encoder results, and the draft of Part 7.
