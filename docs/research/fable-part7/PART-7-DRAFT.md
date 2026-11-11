> **Draft of Part 7, pushed for review on 11 November 2026.** Ready: 7.1 (candidates), 7.2 (method) and 7.7 (sources). Sections 7.3 to 7.6 (results, the comparison table, the recommendation) follow the guarded run. 7.2 is written as it will stand once the run is done.

## Part 7. Arabic-first small models, measured on this laptop

**Why this part exists.** The owner's verdict on Part 6 was that it gave more questions than solutions. This part runs models on the laptop with the project's own test bench and reports numbers. Papers are used only to choose what to run.

**What "measured" means here.** Every accuracy, time and memory figure below was produced on the i5-1335U laptop (16 GB, no GPU) on 11 November 2026 or later, on mains power with Windows set to Best performance (checked by the project's own `Assert-AlphaPower`), with Ollama 0.40.16. Figures for Gemma 4 E4B, Gemma 3 4B, Gemma 4 E2B and Gemma 3n E2B are the committed ones of 8 and 9 November (Ollama 0.40.15) and were not rerun.

### 7.1 The candidates (Step 1)

One research unit (F1) verified each candidate through the Hugging Face API and its model card on 11 November 2026: 21 source records, sources in 7.7. A model whose licence forbids commercial redistribution is listed, marked and not measured.

**Generative models up to about 2B parameters, Arabic-first or Arabic-strong.** Scores are the Jais 3 report's own table (ArabicMMLU / Arabic Legal (HELM) / IFEval-Ar), without a reasoning trace, vendor-run on unquantised weights; they are the reason to measure, not a measurement.

| Model | Released | Parameters | Licence: commercial redistribution? | 4-bit file | Published Arabic scores | Decision |
|---|---|---|---|---|---|---|
| `inception42/Jais-3-0.9B` | 29 Sep 2026 | 1.08B | Apache 2.0: yes | 666 MB | 60.0 / 55.6 / 63.4 | **measure** |
| `inception42/Jais-3-3.7B` | 29 Sep 2026 | 3.7B by name (5.06B counted on the Hub) | Apache 2.0: yes | 3.16 GB | 76.4 / 64.1 / 71.0 (above Gemma 4 E4B's 70.6 / 59.2 / 67.8) | **measure**, as the larger reference |
| `tiiuae/Falcon-H1-1.5B-Deep-Instruct` | 1 May 2025 | 1.55B | TII Falcon licence: yes, with conditions to pass on | 938 MB | 48.7 / 48.8 / 49.3 | **measure** |
| `inception42/jais-family-590m-chat` | 2 Aug 2024 | 0.77B | Apache 2.0: yes | 504 MB | ArabicMMLU 39.1 (its card) | **measure** (context only 2,048 tokens) |
| `Qwen/Qwen3.5-2B` | 28 Feb 2026 | 1.88B | Apache 2.0: yes | 1.28 GB | 44.9 / 27.1 / 40.4 | **measure**, as the multilingual control |
| `ibm-research/granite-4.0-h-3b-ar` | 16 Jun 2026 | 3.4B | Apache 2.0: yes | 2.06 GB | 59.8 / 56.1 / 43.1 | not measured: above 2B, weak instruction following |
| `silma-ai/SILMA-Kashif-2B-Instruct-v1.0` | 26 Jan 2025 | 2.6B | Gemma terms: with conditions | 1.71 GB | 4.90 of 10 on SILMA's own benchmark | not measured: its maker says it is for retrieval-augmented use only |
| `MBZUAI-Paris/Atlas-Chat-2B` | 16 Sep 2024 | 2.6B | Gemma terms: with conditions | 1.71 GB | DarijaMMLU 45.0 | not measured: Moroccan Darija, 1.7 GB |
| Falcon-H1-Arabic (3B and up), Falcon-Arabic | announced 5 Jan 2026 | 3B+ | not published | no weights | vendor: about 62 % on OALL | cannot be measured: still no public weights |
| `hammh0a/Hala-350M`, `-700M`, `-1.2B` | 13 Sep 2025 | 0.35 to 1.17B | CC BY-NC 4.0: **no** | 731 MB (1.2B) | 45.0 / 41.1 / 44.1 | **excluded by licence** |
| `CohereLabs/tiny-aya-global` | 2026 | 3.3B | CC BY-NC: **no** | 2.14 GB | 55.9 / 52.2 / 58.2 | **excluded by licence** |
| `inception42/jais-family-1p3b-chat`, `-2p7b-chat` | 2 Aug 2024 | 1.56B, 2.95B | Apache 2.0: yes (gated) | 1.05 GB, 2.61 GB | ArabicMMLU 42.2, 50.0 | not measured: below Jais-3-0.9B at a larger size |
| `RightNowAI/RightNow-Arabic-0.5B-Turbo` | 10 Apr 2026 | 0.52B | Apache 2.0: yes | 424 MB | ArabicMMLU 23.2 (below the 25 % chance level) | not measured |
| ArabianGPT, AraGPT2, `darja-gpt-50m` | 2021 to 2026 | 0.05 to 1.6B | mixed | none | none | raw language models with no instruction tuning |

- [K] No instruction-tuned generative model for **Algerian** Darija of 2B or less exists on the Hub (searches for "algerian", "dziri", "darija", "darja"; F1 S11). No Ollama library tag exists for any Arabic-first model; all pulls are `hf.co/<repo>:Q4_K_M` (F1 S4).
- [K] **Jais 3 thinks by default.** Its chat template sets `reasoning_effort` to "high" when nothing is passed and has no off switch; "low" opens the answer with an empty block and writes no trace (F1 S2, S5). Ollama forwards a string `think` value as `reasoning_effort`, but a boolean `think: false`, which is all the project's harness can send, is ignored by this template (F1 S3). How this was handled is in 7.2.

**Encoders.**

| Model | Parameters | Licence: commercial redistribution of fine-tuned weights? | fp32 file | Tokenizer | Decision |
|---|---|---|---|---|---|
| `jhu-clsp/mmBERT-small` (21 Aug 2025) | 140M (42M outside the embeddings) | MIT: yes | 564 MB | 256,000 sub-words; Algerian Arabic in its language list | **measure** |
| `CAMeL-Lab/bert-base-arabic-camelbert-mix` | 110M | Apache 2.0: yes | 439 MB | 30,000 Arabic word-pieces; trained on formal, dialectal and classical Arabic | **measure** |
| `alger-ia/dziribert` | 124M | Apache 2.0: yes | 498 MB | 50,000 word-pieces; Algerian text in Arabic and Latin script | **measure** |
| `intfloat/multilingual-e5-small` (30 Jun 2023) | 118M (21M outside the embeddings) | MIT: yes | 471 MB | 250,037 SentencePiece | **measure** |
| `microsoft/Multilingual-MiniLM-L12-H384` | same shape as e5-small | MIT: yes | 471 MB | same | alternate to e5-small |
| `aubmindlab/bert-base-arabertv02`, AraELECTRA | 135M | own licence: commercial use allowed, **fine-tuned weights must carry the same licence** | 543 MB | 64,000 word-pieces | not measured: share-alike on the weights we would ship |
| `UBC-NLP/MARBERTv2`, `ARBERTv2` | 163M | "exclusively for research": **no** | 654 MB | 100,000 word-pieces | **excluded by licence** |
| `NAMAA-Space/AraModernBert-Base-V1.0` | 150M | Apache 2.0: yes | 598 MB | 50,280 | not measured: Arabic XNLI 0.47 on its own card |
| DZAIR, algerianDeBERTa (2026) | 60M to 124M | Apache 2.0 / custom code | 242 MB+ | — | not measured: below DziriBERT on their own tests |

**Shortlist.** Generative (5): Jais-3-0.9B (the only model under 1 GB with Arabic and legal scores near Gemma 4 E2B); Falcon-H1-1.5B-Deep (second-best Arabic evidence under 1 GB, no thinking mode); jais-family-590m (the only other Arabic-first Apache model under 1 GB); Qwen3.5-2B (what a multilingual model of the same class does, to show what "Arabic-first" buys); Jais-3-3.7B (the reference: half E4B's file, above it on the vendor's Arabic tests). Encoders (4): mmBERT-small, CAMeLBERT-mix, DziriBERT, multilingual-e5-small.

### 7.2 How the generative models were run (Step 2), and what had to differ from the 9 November run

The brief asks for the project's own instruments, unchanged, exactly as for the Gemma 4 E2B run. Three things made that impossible to the letter. Each was put to the owner and the deviation is stated here so that it can be judged.

1. **The owner's guarded command accepts four model names only.** `tools/alpha-03/Run-OwnerAlpha01.ps1` (commit `480d4a4` of `codex/alpha-03`) validates `-Model` against `gemma3:4b`, `gemma4:e4b`, `gemma4:e2b`, `gemma3n:e2b`; and the frozen harness has the same lock inside its guarded mode (`FactRunMeasurement.ResolveDigest`: "Measured owner runs require an approved model"). [K]
   - **What was done, with the owner's agreement (11 November):** a scratch copy of the frozen source, outside the repository, with **one added line** in `src/DzLegal.Harness/FactRunMeasurement.cs`: `_ when expectedDigest is { Length: 64 } => null,` so that any other model runs only with its recorded digest, as the two phone models did. The reader, its prompt, the parser, the scorer and every inference option (CPU only, context 4,096, temperature 0.2, repeat penalty 1.1, 300-token cap, reload every ten cases, 512 MiB memory guard) are untouched. A runner script, also outside the repository, repeats the owner's wrapper line for line (mains and Best performance, the desktop app open and idle, no browser, editor or build running, a quiet-CPU preflight, a fresh output folder, no retry) and takes the models and digests from a list. It runs three stages per model: the hidden set v2 reading, the app's analysis on the 46 development cases, and the 12 locked drafting files of stage 2 (checked against `DRAFT-STAGE-2.json`, as `Measure-AppBenchmarks.ps1` does). A stage that fails is not retried; the model's other stages still run, which is this runner's choice for an unattended night.
   - The app, the legal data, the prompts and the reader are not changed, on any branch. At the reviewer's request the runner, the model list, the two-line harness difference and the encoder scripts and results are pushed with this report under `docs/research/fable-part7/`, and were reviewed there before the run (commit `1e8b3a1`).
2. **The guard refuses to start while VS Code is open**, and this session runs inside VS Code. So the owner launched the runs himself from a PowerShell window, as on 8 and 9 November; I read only each run's `summary.md` and never opened the hidden folder.
3. **Jais 3 cannot be used as downloaded.** [K, measured] With what the harness sends (no thinking flag, or `think: false`), Jais-3-0.9B spent all 300 answer tokens on a reasoning trace and returned an empty answer; Ollama 0.40.16 rejects the "low" setting for this download ("does not support thinking"), ignores a Modelfile template, and mis-tokenises a replaced template. The maker's no-trace mode ("low effort", the mode behind its published no-trace scores) is selected inside the chat template, so the model was run as a **copy of its GGUF file with two edits in the maker's chat template** (metadata only): the default reasoning effort is "low" instead of "high", and the low-effort opening closes its empty block and ends with a line break. The weights are byte-identical (same SHA-256 over all 255 tensors). Each answer then begins with two marker lines (`<ifm|think_faster>`, `</ifm|think_faster>`) that the unchanged parsers skip; in the fact summaries they appear as "unreadable lines", which are not critical errors. The same was done for Jais-3-3.7B. This is a property of how the model must be packaged for this app, and part of the result.

**Other differences from 8 and 9 November**, none of them chosen: Ollama is 0.40.16 (it was 0.40.15); the comparison models were not rerun, as the brief instructs. Qwen3.5-2B is the 4-bit file from the Hub (`hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M`). The harness switches thinking off by itself only for names that start with `qwen3`, so `--think false` is passed for it: with that value it answers directly, and without it it reasons for all 300 answer tokens and returns nothing (checked before the run). Ollama installs the repository's vision part with it (668 MB), so the installed size is 1.95 GB for a 1.28 GB language file.

**What is reported for each model**: from the hidden v2 summary, cases fully right and every field of the summary table, seconds per case, the peak memory lines; from the app's analysis on the 46 development cases, claim type exact, core articles, out-of-scope declined and seconds per case; the file size; and tokens per Arabic word, measured on the repository's own fictitious notes (57 notes, 1,261 words).

### 7.7 Sources for Part 7

Opened on 11 November 2026 by unit F1 through the Hugging Face API (`https://huggingface.co/api/models/<repo>?blobs=true`) and each model card; 47 quoted passages were matched against the fetched pages. "Date" is the publication or repository date. Ids continue the earlier numbering as F1-S1 to F1-S21. Everything in 7.3 to 7.5 is this part's own measurement and has no outside source.

| Id | Source | Link | Date | How directly it applies |
|---|---|---|---|---|
| F1-S1 | Jais 3 model cards and official GGUF repositories (sizes, licence, settings; "Quantized variants have not been separately evaluated yet") | https://huggingface.co/inception42/Jais-3-0.9B ; https://huggingface.co/IFM/Jais-3-0.9B-GGUF ; https://huggingface.co/IFM/Jais-3-3.7B-GGUF | 2026-09-29; GGUF 2026-10-12 | direct |
| F1-S2 | Jais 3 chat template (the reasoning effort defaults to `'high'`; "Supported values: high, medium, low.") | https://huggingface.co/inception42/Jais-3-0.9B/raw/main/chat_template.jinja | 2026-09-29 | direct: thinking is on unless "low" is passed |
| F1-S3 | Ollama 0.40.16: bundled llama.cpp build and how `think` reaches the template; llama.cpp pull request 29535 ("model : add K2 Horizon dense and MoVA support") | https://github.com/ollama/ollama/releases/tag/v0.40.16 ; https://github.com/ggml-org/llama.cpp/pull/29535 | 2026-11-09; 2026-10-06 | direct |
| F1-S4 | Ollama library (no Arabic-first model) and the Hugging Face pull syntax | https://ollama.com/library ; https://huggingface.co/docs/hub/ollama | read 2026-11-11 | direct |
| F1-S5 | Jais 3 technical report: Arabic and legal scores with and without a reasoning trace, Gemma 4 E2B and E4B on the same scale | https://arxiv.org/abs/2611.10224 | 2026-11-06 | direct for choosing; vendor-run, unquantised, multiple-choice |
| F1-S6 | Jais family of 2024 (590M to 2.7B; context 2,048; ArabicMMLU 39.1 / 42.2 / 50.0) and community GGUF files | https://huggingface.co/inception42/jais-family-590m-chat ; https://huggingface.co/mradermacher/jais-family-590m-chat-GGUF | 2024-08-02 | indirect |
| F1-S7 | Falcon-H1 small models and the TII Falcon licence ("royalty free and without charge"; the use policy passed on as an enforceable term; the TII statement) | https://huggingface.co/tiiuae/Falcon-H1-1.5B-Deep-Instruct-GGUF ; https://falconllm.tii.ae/falcon-terms-and-conditions.html | 2025-05-01; licence December 2024 | direct |
| F1-S8 | Falcon-H1-Arabic announcement; no public weights (six repository ids answer HTTP 401; none of TII's 137 public repositories has "Arabic" in its name) | https://huggingface.co/blog/tiiuae/falcon-h1-arabic | 2026-01-05 | direct: cannot be measured |
| F1-S9 | SILMA Kashif 2B ("should ONLY be used in RAG setups") and the Gemma terms | https://huggingface.co/silma-ai/SILMA-Kashif-2B-Instruct-v1.0 ; https://ai.google.dev/gemma/terms | 2025-01-26; terms 2026-04-01 | indirect |
| F1-S10 | Atlas-Chat-2B ("instruction-tuned for Darija, the colloquial Arabic of Morocco") | https://huggingface.co/MBZUAI-Paris/Atlas-Chat-2B | 2024-09-16 | indirect: Moroccan |
| F1-S11 | Hub searches for Algerian Darija generators (`algerian`, `dziri`, `darija`, `darja`, `arq`, `maghreb`); `darja-gpt-50m`, `Qwen3-4B-Algerian-Darja` | https://huggingface.co/touati-kamel/darja-gpt-50m ; https://huggingface.co/DarjaCore/Qwen3-4B-Algerian-Darja | 2026-09-23 | direct: none of 2B or less with instructions |
| F1-S12 | IBM Arabic Granite | https://huggingface.co/ibm-research/granite-4.0-h-3b-ar | 2026-06-16 | indirect: above 2B |
| F1-S13 | Other Arabic-first small generators found by search (Hala: "license: cc-by-nc-4.0"; ArabianGPT: "a raw pre-trained model"; RightNow-Arabic-0.5B) | https://huggingface.co/hammh0a/Hala-1.2B ; https://huggingface.co/riotu-lab/ArabianGPT-08B-V2 ; https://huggingface.co/RightNowAI/RightNow-Arabic-0.5B-Turbo | 2025-09-13; 2024; 2026-04-10 | screening |
| F1-S14 | Qwen3.5-2B GGUF files (the multilingual control) | https://huggingface.co/unsloth/Qwen3.5-2B-GGUF | 2026-02-28 | direct |
| F1-S15 | DziriBERT card and paper ("at least as good as MARBERT on the Algerian dialect and even much better when dealing with Roman characters") | https://huggingface.co/alger-ia/dziribert ; https://arxiv.org/abs/2109.12346 | 2021-09-25 | direct; tweets, not legal notes |
| F1-S16 | Other Algerian encoders of 2026 (DZAIR, algerianDeBERTa, MagharibiBERT) | https://huggingface.co/algerian-nlp/DZAIR ; https://huggingface.co/81melody/algerianDeBERTa | 2026-09-16; 2026-04-20 | indirect; self-reported |
| F1-S17 | CAMeLBERT cards | https://huggingface.co/CAMeL-Lab/bert-base-arabic-camelbert-mix | April 2021 | direct |
| F1-S18 | AraBERT and AraELECTRA licence ("you must distribute your contributions under the same license as this one") | https://github.com/aub-mind/arabert/blob/master/arabert/LICENSE | 2020 to 2021 | direct for the licence |
| F1-S19 | MARBERT and ARBERT ("exclusively for research") | https://github.com/UBC-NLP/marbert | 2021 | direct: excluded |
| F1-S20 | Small multilingual encoders with MIT licences (mmBERT-small, Multilingual-MiniLM-L12-H384, multilingual-e5-small) | https://huggingface.co/jhu-clsp/mmBERT-small ; https://huggingface.co/microsoft/Multilingual-MiniLM-L12-H384 ; https://huggingface.co/intfloat/multilingual-e5-small | 2025-08-21; 2022; 2023-06-30 | direct |
| F1-S21 | Small Arabic encoders of 2025 and 2026 (AraModernBERT, Waqf arabic-modernbert-unified and others) | https://huggingface.co/NAMAA-Space/AraModernBert-Base-V1.0 | 2025-02-01 | screening |

**The project's own comparison figures** (not rerun): the hidden set v2 summaries for Gemma 4 E4B (58 of 90), Gemma 3 4B (39), Gemma 4 E2B (41) and Gemma 3n E2B (42); the stage 1 analysis results (claim type 46 of 46 for Gemma 4 E4B, 41 of 46 for Gemma 3 4B); and the stage 2 drafts, all committed on `codex/alpha-03` at `480d4a4` under `docs/baselines/`.

