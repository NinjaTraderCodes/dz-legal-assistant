# Specification: the reader of lawyer notes (step 4)

**The job.** From a lawyer's note, produce what the rules need, each answer with the words it comes from: the contract `INoteReader` (`research/core-logic/src/DzLegal.CoreLogic.Contracts/NoteReading.cs`). The reader **proposes**; the lawyer confirms. An honest "I do not know" (abstention) is a good answer: the app then asks. A confident wrong answer is the failure to avoid.

**Revised on 14 November 2026.** The reader now comes first (owner, 14 November 2026). It is built on the label scheme frozen now ([../notes/SCHEME.md](../notes/SCHEME.md), version 1.0), it is trained on notes written by Codex, by a local model or by code, and **nothing that changes it is chosen on a note a Claude model wrote**. Slots that the rules add later become extra outputs without retraining from zero ([scheme-extension.md](scheme-extension.md); "A new head" below).

**Targets on hidden v3** (the brief): claim type 95 %; outside the catalogue recognised 95 %; polarity 95 %; each other slot 90 %; abstentions counted separately. Exact definitions, and what each size can prove: [scorer.md](scorer.md).

**Limits** (the brief): CPU only; in .NET (ONNX Runtime, MIT) or plain code; about 150 MB on disk at most, everything included; under one second per note on the owner's laptop (i5-1335U, 16 GB, no GPU); every licence allows commercial redistribution; nothing from a hidden set in training. **Training** happens on the same laptop, at night on mains and on days the owner says it is free; a free GPU (Kaggle, Colab free tier) only after asking the owner, with fictitious notes and code only, no hidden data, no payment details, no new account without a yes.

## Three rules that come before any experiment

1. **The tuning rule (owner, 14 November 2026).** "Anything that changes the reader (encoder choice, early stopping, temperature scaling, thresholds, head selection) is tuned only on the Codex half of dev and on clean notes. The Sonnet half is used only to measure and report, never to choose or tune. Same for the challenge set." In this file, **a choice** is any of: which encoder; when training stops; a setting picked by comparing runs (a learning rate, a loss variant, a weight); which rung, head or recipe is kept; whether composed notes stay; a temperature; a threshold; whether the rule vote is on; the cap on windows; the form of quantisation. Every choice is made with the loader's purpose `tune` ([notes-pipeline.md](notes-pipeline.md), "The loader"), which refuses every Sonnet-written note. The training and calibration scripts have no other way to read a note.
2. **Every score that counts is measured on the 8-bit files, through the .NET tokenizer's ids.** A 32-bit score, or a score on Python's own tokenisation, is never reported as the reader's.
3. **No Claude output trains the reader** ([../notes/TERMS.md](../notes/TERMS.md)). The loader's purpose `train` refuses such a note. Part 7's fine-tuned copies were trained on Claude-written notes and are never a starting point.

## What you may use

Everything below is allowed without asking, as is the small pure-Python package that HANDBOOK section 3 allows in an environment of your own. **Anything else is a stop point: ask the owner first** (HANDBOOK section 8, "any download not listed").

| What | Where, or where from | Notes |
|---|---|---|
| The Python environment of Part 7 | `C:\Users\TheShadow\dz-arch-research\venv` (Python 3.12.10; torch 2.15.0+cpu, transformers 5.22.2, onnxruntime 1.31.1, onnx 1.23.2, scikit-learn 1.9.1, tokenizers 0.23.3, sentencepiece 0.2.3, numpy 2.5.4) | Use it as it is: install nothing into it. Record its package list with every result. |
| Python 3.12 with pytest | `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312` (`py -3.12`, pytest 9.0.3) | for the Python scripts' tests. The default `python` is 3.14 and has none of the packages above. |
| `pdftotext`, `pdftoppm` | installed | |
| CAMeLBERT-mix, **the original pre-trained model** | its official model page: https://huggingface.co/CAMeL-Lab/bert-base-arabic-camelbert-mix, revision `9be352797bdf28a9ae21e2ae582aaaca7abdb22d` (Apache 2.0, read on 13 November 2026) | Download into `C:\Users\TheShadow\dz-core-research\models\pretrained\camelbert-mix\`; record the size and SHA-256 of every file, the revision, the licence link and the date. |
| CAMeLBERT-MSA, **the original pre-trained model** | its official model page: https://huggingface.co/CAMeL-Lab/bert-base-arabic-camelbert-msa, revision `277069fd3645fedb22b746caf38d111aadee0241` (Apache 2.0, read on 13 November 2026) | Download into `...\models\pretrained\camelbert-msa\`; the same records. |
| The Official Journal issues | joradp.dz, through the one fetch script `oj-fetch` | step 2 |
| NuGet `Microsoft.ML.OnnxRuntime` 1.31.1 and `Microsoft.ML.Tokenizers` 3.0.0 (MIT) | NuGet, in phase A | in `research/core-logic/Directory.Packages.props` only |
| The local Ollama models of Part 7 | already installed | step 5; digests in `docs/research/fable-part7/run/models.json` on the branch `fable/architecture-review` |

**Part 7's files are on another branch.** Read them with `git show fable/architecture-review:docs/research/fable-part7/<path>` (for example `encoders/results/cls-camelbert-mix.json`, `run/models.json`). Never write there.

**Model files are never committed.** An 8-bit base encoder is over GitHub's 100 MB limit, and a commit holding one would block every later push. Models, token vectors and checkpoints live in `C:\Users\TheShadow\dz-core-research\models\`; the repository holds their SHA-256, their sizes and the exact commands that rebuild them. **Unit tests use a tiny fixture model committed for the plumbing** (the one named exception to "model files are never committed": random weights, made by a script that is committed with it) (a few kilobytes: it checks loading, shapes, offsets and the contract, not accuracy). Accuracy and parity runs are commands, not unit tests, so `dotnet test` is green in a clean clone that has no model file.

## The ladder: cheapest first, each rung with its stop rule

**Stop climbing when the targets are met on the Codex half of dev and of the challenge set with margin**: at the rates that the size table of [scorer.md](scorer.md) says would be "met" at v3's sizes, not merely at the target. Report every rung, also the ones that lost: the brief says "measure each idea; keep what wins". **The mirror set chooses nothing.**

| # | Rung | Phase | Cost | Stops when |
|---|---|---|---|---|
| 1 | **The scorer**, with its six tests | A | half a day | the tests are green and its hash is in the state file, before any model exists |
| 2 | **Plain code:** the date and amount parsers; the table of polarity cues; **and the reader's front end** (the .NET tokenizer, the windows, `reader ids`, `reader encode`, the 8-bit export of CAMeLBERT-mix: "The front end" below) | A | 3 days | dates and amounts: 98 % exact on the code-made value corpus (below); then 95 % on the Codex half of dev when it exists. Under the bar: fix the parser, never the corpus |
| 3 | **A linear model on character n-grams** for the claim type and for outside | B | hours | reported. It is the "no deep model" line and the leakage probe; nothing is decided on it |
| 4 | **A pre-trained Arabic encoder, frozen, with light heads:** (a) the encoder choice at 1,000 notes; (b) the learning curve at 1,000, 2,000 and 4,000, with and without composed notes | B | 2 to 3 days, minutes per run | (a) by the rule in "Rung 4"; (b) at the report: **owner stop point** |
| 5 | **Training the encoder's top layers** | C | at most two overnight runs | run **only if** (a) on the tune notes at 4,000 a target line is more than 2 points under the rate that would be "met" at v3's size ([scorer.md](scorer.md), size table), (b) the curve is flat (the tuning score rises by less than 1.5 points from 2,000 to 4,000), **and** (c) the owner answered "fine-tuning" at the learning-curve stop point. Stops after two overnight runs: more is an owner stop point |
| 6 | **Polarity as its own problem** | throughout | – | reported per polarity, per difficulty and per family at every rung |
| 7 | **Clean against Codex-trained**, at equal size | C | hours | the table exists; "close" is within 2 points on claim type and polarity |
| 8 | **Calibration and abstention** | C | 1 day | each target met on the tune notes at its coverage floor, or reported as not met with the best pair |
| 9 | **In .NET**: the heads, the decoding, `INoteReader`, parity, timings | C | 2 days | parity: the same labels as Python, confidences within 0.01, on 200 dev notes; under one second a note; the size counted |
| 10 | **The mirror set, once; the pre-commitment; hidden v3** | E | – | at most two candidates; the owner's one run ([hidden-v3-commands.md](hidden-v3-commands.md)) |

**Not on the ladder unless needed:** a small model trained from scratch (the brief's invitation: its own tokenizer of about 8,000 pieces, a 4-layer, 256-wide encoder, masked-word pre-training on the notes and the law texts, one day, kept only within a point of the big encoder or better); an ensemble of the rules, the linear model and the encoder; distillation into the small model. Try one only if the 8-bit encoder cannot fit the 150 MB or a target is still missed after rung 5, and say so to the owner first: each is "training beyond the plan" in cost.

### What is measured on which set

| What | Notes | Loader purpose |
|---|---|---|
| Training (the loss) | `train`, `pilot` when allowed, `composed` up to a quarter, `clean` split `train` (rung 7), a slot pack's train part | `train` |
| Every choice (rule 1) | **one pooled set, the tune notes:** the Codex half of `dev` and the Codex half of `challenge`, and the `clean` split `dev` once it exists (phase C). A comparison always uses the same tune notes for both candidates. For a new slot's head only: the Codex half of its pack's dev part | `tune` |
| Every reported score | both halves of `dev` and of `challenge`, **shown per writer**, per difficulty and per polarity family; the clean test split; the app's own cases | `measure` |
| The forecast of v3 | `mirror`, once, for the at most two final candidates | `measure`, at the pre-commitment only |
| The verdict | hidden v3, once, run by the owner | – |

**The tuning score.** Where a rule says "better on the tune notes", "points" or "the curve", it means this one number, defined in [scorer.md](scorer.md) ("The tuning score") and computed by `score --tuning` on the notes the loader gives for `tune`, pooled. Three seeds; the means are compared. **The spread** of a candidate is its highest seed minus its lowest; two candidates **do not differ** when the gap of their means is no larger than the larger of their two spreads. Every "better", "lower", "rises" and "wins" in this file and in the handbook's tables means: beyond the spread.

**"A target is missed"** means: a target line of [scorer.md](scorer.md), measured on the tune notes, is under the rate its size table gives as "met" at v3's size. "Missed by more than 2 points" is measured against that rate.

**The style gap** is the tuning score's formula computed on the Codex half of dev minus the same on the Sonnet half (the second with purpose `measure`). **It is reported, at every rung, and it decides nothing.** The executor changes nothing because of it. At the learning-curve stop point the owner may choose a remedy (more style cards for the Codex writer; a larger share of composed notes, up to a quarter; rung 5); whatever is chosen is then judged on the tune notes, and the Sonnet half is reported again.

**The order of the choices at rung 4**, so that nothing is chosen twice: with 1,000 Codex-written notes, three seeds, focal loss, no composed notes and no rule vote: (i) the encoder; then, with that encoder, (ii) focal loss against weighted cross-entropy; then (iii) the curve at 1,000, 2,000 and 4,000, with and without composed notes, the composed notes decided at its end; (iv) the rule vote, at rung 6, on the 4,000.

### Rung 2. Plain code

- **Dates:** every format of the labelling guide (section 8): `dd/mm/yyyy` with `/`, `-` or `.`; day + month name (both the Levantine names and the Algerian ones: جانفي، فيفري، مارس، أفريل، ماي، جوان، جويلية، أوت، سبتمبر، أكتوبر، نوفمبر، ديسمبر; also يناير، فبراير … for completeness); Arabic-Indic digits; «سنة 2015»; month + year (normalised `yyyy-mm`). Output: normalised value and exact UTF-16 span in the original string.
- **Amounts:** digits with `.`, space or `,` as separators; words («خمسمائة ألف», «مليون ونصف»); words with digits in brackets; units دج، دينار، د.ج، سنتيم (÷ 100), مليون / مليار.
- **Polarity cues,** as a table with scopes, tested sentence by sentence against the hard cases of the guide's section 4.2 and its 41 kind rows: negation particles (لم، لا، لن، ما، ليس، غير، عدم، دون، بدون، بلا، انعدام، لا وجود لـ); **restriction** (لم/لا/ما … إلا / سوى / غير: the scope after the particle is affirmed); refusal and omission verbs (امتنع، رفض، تخلف، تقاعس، أحجم، أغفل، أهمل); dispute verbs with their subject (ينكر، ينفي، ينازع، يدعي، يزعم، يتمسك، يدفع بـ، يطعن في); "despite" (رغم، مع أن، بالرغم من: the clause after it is affirmed); future and intention (سـ، سوف، يعتزم، ينوي: not an event).
- **The trigger words** of each event kind are a file, `src/DzLegal.CoreLogic.Reader/cues.json` (event kind → Arabic words and stems), written by the executor from the Arabic of the guide's table 5.1 and of the element propositions, never from a note. **The bar:** on the guide's 37 hard cases and on the worked sentences of table 5.1 the cue table gives no wrong vote (it may give none).
- **Written forms the parsers must read** (one list, shared with the code-made corpus and the style cards): dates `dd/mm/yyyy`, `yyyy/mm/dd`, with `/`, `-` or `.`; «12 مارس 2025»; «في 12 من شهر مارس 2025»; month and year; «سنة 2015»; both families of month names; Arabic-Indic digits. Amounts in digits with `.`, a space or `,` between thousands (a comma followed by exactly two digits at the end is the decimal part); in words; with دج، دينار، د.ج، سنتيم، مليون، ملايين، مليار، ملايير. **Not emitted as values:** a relative date («منذ شهرين»), a Hijri date, a sum in a foreign currency: they have no normal form here; the scorer counts the gold values so marked apart.
- The rules give, for an element whose trigger words they find, a polarity vote and the span. They do not need to be right everywhere: they must be **independent of the learned model**, so that a disagreement means something.

**The code-made value corpus** (rung 2's own test, needing no note): code renders 2,000 dates and 2,000 amounts in every written form of the style cards ([scenario-sampler.md](scenario-sampler.md)) inside carrier sentences, each with its normal form and its span. The parsers must read 98 % of them exactly (value and span). The corpus's generator and the parsers are written from the guide's section 8, not from each other.

This rung alone is scored on every open test set. Its date and amount reading is expected to be near exact and is what the final reader uses for values; a learned model only links values to roles.

### The front end (phase A, with rung 2)

Rung 4 trains on stored encoder outputs, and those are made in .NET. So this much of the reader is built in phase A:

1. `NoteReading.cs` gains `RoleReading` and `ClaimGuess.Role` ("The parties head" below), with a contract test, **before the scorer is written**.
2. The two NuGet packages; the WordPiece tokenizer from `vocab.txt` with the settings of the model's `tokenizer_config.json`; the input sequence, the windows and the stitching (below); `reader ids` and `reader encode`.
3. **The 8-bit encoder.** Download CAMeLBERT-mix (the table above). `train/export_encoder.py`: load the base model (`AutoModel`, the pinned revision, no head), export to ONNX with the inputs `input_ids`, `attention_mask`, `token_type_ids` (all zeros) and the output `last_hidden_state`, batch and sequence as dynamic axes, the highest opset ONNX Runtime 1.31.1 reads; then `onnxruntime.quantization.quantize_dynamic` with 8-bit integer weights. Part 7's `export_measure.py` (`git show fable/architecture-review:docs/research/fable-part7/encoders/export_measure.py`) shows the calls for a fine-tuned classifier: reuse its quantisation step, not its model class. Record the file's size and SHA-256. CAMeLBERT-MSA is downloaded and exported the same way at rung 4a, not before.
4. **Done when:** on the 500 pilot notes the .NET piece ids equal those of the Python `transformers` tokenizer (every difference explained in the report, the .NET tokenizer being the reference); the offset round trip passes (rung 9); `reader encode` has written the cache for the pilot notes.

### Rung 3. The linear model

Character 2- to 5-grams (after the level-2 normalisation of [article-store.md](article-store.md)), TF-IDF, logistic regression (scikit-learn, from Part 7's environment), one-vs-rest so that two claims can both fire, plus an `outside` class, balanced class weights, a class answered at 0.5 or more; trained at each size of the curve. Regularisation strength chosen among 1, 10 and 100 on the tune notes. Trained in seconds; a few megabytes. Report it everywhere as the "no deep model" line. Its largest weights are the leakage probe ([probes.md](probes.md)), which also runs it on word 1- and 2-grams, apart.

### Rung 4. A frozen encoder with heads

**Encoder.** Two candidates, both original pre-trained models from their official pages (the table above), both Apache 2.0, both 109 M parameters:

- **CAMeLBERT-mix** (`CAMeL-Lab/bert-base-arabic-camelbert-mix`), trained on formal Arabic, dialects and classical Arabic. **Why it is first:** it is the encoder Part 7 measured on this task. Fine-tuned there, it identified the claim type on 37 of the 46 open cases alone and 40 with the section filter, the best of six encoders.
- **CAMeLBERT-MSA** (`CAMeL-Lab/bert-base-arabic-camelbert-msa`), trained on formal Arabic only. **Why it is tried:** the notes of this stage are formal Arabic only. Part 7 did not measure it.

**The encoder choice (rung 4a).** With 1,000 training notes, the same heads and recipe on both encoders, three seeds each: **keep CAMeLBERT-mix unless CAMeLBERT-MSA is better by a full point of the tuning score and beyond the spread.** The reader ships one encoder. Part 7's own fine-tuned classifier is evidence about the encoder, never a starting point. Both repositories give a WordPiece vocabulary file (`vocab.txt`) and no `tokenizer.json`: build the .NET tokenizer from the vocabulary file, and take the lower-casing and accent settings from each model's `tokenizer_config.json`, not from memory.

**Size and speed.**

> **Measured on the owner's laptop on 13 November 2026** (18:43 to 18:45; mains power, Windows power mode Best performance, energy saver off; ONNX Runtime 1.31.1 in Python, 4 threads, one note at a time, each model alone in a fresh process). Result files and scripts: `docs/research/fable-part7/encoders/` on the branch `fable/architecture-review`.
>
> | Encoder (fine-tuned as Part 7's claim-type classifier) | Form | Model file | Tokenizer | 46 cases, alone / with the section filter | Time per short note (about 26 to 40 tokens) | 128 tokens | 256 tokens | 512 tokens | Peak memory, private |
> |---|---|---:|---:|---:|---:|---:|---:|---:|---:|
> | CAMeLBERT-mix | 32-bit | 416.4 MB | 0.7 MB | 37 / 40 | 25.6 ms | 101 ms | 246 ms | 485 ms | 0.90 GiB |
> | CAMeLBERT-mix | 8-bit | 104.8 MB | 0.7 MB | 35 / 38 | 9.4 ms | 48 ms | 99 ms | 276 ms | 0.51 GiB |
> | DziriBERT | 32-bit | 475.0 MB | 1.2 MB | 37 / 38 | 24.5 ms | 86 ms | 195 ms | 475 ms | 1.01 GiB |
> | DziriBERT | 8-bit | 119.4 MB | 1.2 MB | 36 / 37 | 10.6 ms | 44 ms | 101 ms | 264 ms | 0.54 GiB |
> | mmBERT-small | 32-bit | 537.0 MB | 32.8 MB | 35 / 37 | 19.8 ms | 57 ms | 129 ms | 424 ms | 1.51 GiB |
> | mmBERT-small | 8-bit | 135.0 MB | 32.8 MB | 31 / 33 | 11.4 ms | 37 ms | 85 ms | 237 ms | 0.78 GiB |
>
> What the figures say for this step:
> - **Time is not the constraint.** The 8-bit CAMeLBERT-mix takes 9 ms on a short note and 0.28 s on a full window of 512 tokens; a note of two windows stays near half a second. The limit of one second per note leaves room for the heads and for the plain-code rung.
> - **Size forces the 8-bit form.** 104.8 MB in 8-bit against 416.4 MB in 32-bit; the limit is about 150 MB for everything. **Every score that counts must be measured on the 8-bit model**, not on the 32-bit one it was trained as.
> - **The 8-bit form cost accuracy in Part 7:** CAMeLBERT-mix lost 2 of 46 cases (37 to 35 alone, 40 to 38 with the section filter), DziriBERT 1, mmBERT-small 4. That was dynamic quantisation, with no calibration and no training that takes quantisation into account, on 46 cases where one or two is within chance. Measure the loss on dev at each rung; if it is more than a point on any target, try a calibrated or quantisation-aware form before giving up the model.
> - **Memory:** about 0.51 GiB private for the 8-bit model in Python (0.19 GiB working set); the .NET process will differ and must be measured.
> - mmBERT-small's tokenizer file alone is 32.8 MB, which puts its 8-bit form over the limit with the runtime.
> - On battery, in Windows' Best power efficiency mode, the same files were about twice as slow (12 November). The one-second limit should be checked there too, and reported apart.

Two facts to carry into your own measurements of the .NET reader, which go in `docs/research/core-logic/reports/reader.md`:
- **A long note is more than one window.** A long note (about a page, some 350 words) is about 515 CAMeLBERT tokens at Part 7's 1.47 tokens per word, so one or two windows of 512. Measure the median and the 95th percentile per note on dev notes of each length class, not per sentence.
- **The budget leaves room for one encoder.** About 105 MB for the 8-bit encoder, about 20 MB for ONNX Runtime's native library (win-x64 only), about 1 MB for the tokenizer, plus the heads and the thresholds: about 130 MB.

**The input.** One sequence per note: `[CLS] subject [SEP] facts [SEP] requests [SEP] documents [SEP]` (an empty field keeps its `[SEP]`). Every piece carries its **field** (0 to 3) and its **offset** in the field as the lawyer typed it (UTF-16 start and length), kept through normalisation and tokenisation. No marker piece is added to the vocabulary: the heads add a learned field embedding (4 rows) to the token vectors, so a frozen encoder needs no new piece.

**Windows.** At most 512 pieces per window, special pieces included. A note that fits is one window. A longer note is cut into windows that overlap by 128 pieces, at most **three** (a note beyond three is cut there and counted in the report); each window is encoded with its own `[CLS]` first and `[SEP]` last. The windows' token vectors are **stitched** into one sequence for the whole note, which holds the note's own pieces only (the `[SEP]` between fields included, with the field before it; no window's own `[CLS]` or closing `[SEP]`): in an overlap each piece keeps the vector from the window in which it lies farther from the window's edge. The note vector is the mean of the windows' `[CLS]` vectors. The heads then see one sequence, whatever the note's length.

**One pass, several heads.** `X` is the stitched sequence (T pieces × 768). All heads share two layers: `Z = dropout(GELU(Linear 768→256 (X + field embedding)))` (T × 256) and `c = GELU(Linear 768→256 (note vector))`. A **query** is a learned vector of 256 numbers. For a query `q`: the token logits are `u = Z · (W q) / 16` with one shared 256×256 matrix `W`; the **evidence scores** are `sigmoid(u)`; the **pooled vector** is `v = softmax(u) · Z`. So one set of logits both chooses the words and gathers them.

| Head | Parameters | Output | Loss |
|---|---|---|---|
| **Claim type** | one query per class (22: the 21 claim types and `outside`); shared `Linear 768→1` on `[v; q; c]` | one sigmoid per class, with its evidence from the query's own logits (trained on the gold `claimEvidence`). Answers by the decoding rule below. `outsideKind` is a label of the notes only: the reader does not output it | binary cross-entropy |
| **Elements** | one query per element (75); shared `Linear 512→4` (polarity), `Linear 512→4` (actor), `Linear 512→3` (disputedBy), each on `[v; q]` | polarity `stated`, `denied`, `disputed`, `notMentioned`; the actor; who disputes | focal loss (γ = 2) on polarity; cross-entropy on actor and disputedBy where the gold gives them |
| **Evidence** | none of its own: the token logits `u` of each query | the best contiguous stretch of pieces (the stretch that maximises the sum of `sigmoid(u) − 0.5`), mapped back to a `TextSpan` | binary cross-entropy against the gold quotes' pieces, for every label that carries a quote |
| **Values to roles** | a table of 256-number role vectors (one per event kind, request kind and date slot); `Linear 768→256`, GELU, `Linear 256→1` on `[z; ctx; r]`, where `z` is the mean of `Z` over the value's pieces, `ctx` the mean over 16 pieces each side, `r` the role vector | for each date and amount that rung 2 found and each candidate role: belongs or not. Candidates: the event kinds of the answered claim types that take that kind of value, the request kinds (amounts), the date slots | binary cross-entropy |
| **Requests** | one query per request kind (24); shared `Linear 512→1` | asked or not, with evidence; the amount comes from the roles head | binary cross-entropy |
| **Slots** | one query per yes/no or choice slot (5 in scheme 1.0); its own `Linear 512→k` (k = 3, or the choices and `unknown`) | the value, with evidence. The three date slots are value roles: **the reader still emits a `SlotReading` for each of them**, its value the date whose role score for that slot is highest and at least the threshold, `unknown` otherwise | cross-entropy |
| **Parties** (output 2 of the scheme) | one query per claim type that offers a choice of role (7); its own `Linear 512→roles` | the plaintiff's role. There is no `unknown` class (every generated note expresses its role): under its threshold the reader abstains | cross-entropy |

All heads together are under one million numbers: a few megabytes.

**The parties head.** The contract gains an optional part: `ClaimGuess.Role` (`RoleReading`: `Relation`, `Position`, `Confidence`, `Abstained`, `Evidence`), null when the reader gives none. For the 14 claim types with one role the reader fills it from `claim-elements.json`, with the claim's own evidence. Add the record to `NoteReading.cs` with a contract test, and the line to [../INTEGRATION-CONTRACT.md](../INTEGRATION-CONTRACT.md), section 4, in the commit that adds the head. An integrator that ignores `Role` loses nothing.

**What carries a loss.** Only the elements of a note's gold claim types (the others carry none). An actor `unknown` is not a target. A gold date or amount is matched to a span the parsers found when the gold quote overlaps the span and the normal forms are equal; a gold value the parsers missed carries no role loss and is counted as a parser miss. A blanked field, and a field the note's scheme version does not know, carry none.

**Decoding before calibration (phases B and C until rung 8).** Training and tuning need answers, so one decoder is fixed now, in Python (`train/decode.py`) and later the same in .NET (the parity run of rung 9 compares them):

- claim types: the classes at 0.5 or more, at most the three highest; when none reaches 0.5, the single highest; `outside` only when it is the highest, and then alone;
- polarity, actor, who disputes, slots, parties: the highest class;
- requests and value roles: at 0.5 or more; an event's date (or amount) is the value with the highest role score of at least 0.5 for that event kind, the first in the note on a tie;
- evidence: the best stretch;
- **no abstention and no rule vote** before rungs 6 and 8: the decoder always answers, and the confidence is kept.

**What the reader enforces on itself (from rung 8 on).** An answer other than `notMentioned` or `unknown` whose best stretch has no piece with an evidence score of 0.5 or more becomes an **abstention**. When the rule vote of rung 2 and the head disagree on an element's polarity, the reader abstains on that element (kept only if it is better on the tune notes: rung 6).

**The stored encoder outputs.** Run the **8-bit** encoder once over every open note, in .NET, through ONNX Runtime with the .NET tokenizer's ids (`reader encode`, rung 9's tool), and store per note: `<work>\reader\cache\<first 12 hex digits of the encoder file's SHA-256>\<note id>.npz` (two arrays of 16-bit floats: `tokens`, T × 768, the stitched vectors; `note`, 768) and `<note id>.json` (the piece ids, each piece's field and offsets). `<work>` is `C:\Users\TheShadow\dz-core-research`. About 1.5 GB for 4,000 notes, about 3 GB for all open sets. Heads trained on 32-bit vectors would see shifted inputs once run on the 8-bit encoder, so their scores would not count. The cache makes the learning curve, the seeds and every later head cost minutes.

**The recipe for the heads** (`research/core-logic/train/train_heads.py`, with tests on a tiny fixture):

| Setting | Value |
|---|---|
| Optimiser | AdamW, learning rate 1e-3, weight decay 0.01, 5 % linear warm-up then cosine decay, gradient norm clipped at 1.0 |
| Batch | 32 notes |
| Epochs | at most 20; **early stopping on the tuning score** (purpose `tune`), patience 3 |
| Dropout | 0.1 on `Z` |
| Loss weights | claim 1; polarity 2; evidence 1; actor and disputedBy 0.5; value roles 1; requests 1; slots 1; parties 0.5 |
| Rare classes | focal loss on polarity; also one run with plain cross-entropy and inverse-frequency class weights. Keep the better on the tune notes; report both |
| Blanked fields | no loss (notes pipeline, section 5). A field the note's scheme version does not know: no loss |
| Polarity families | the members of a family go into the same batch |
| Seeds | 11, 22, 33 |
| Environment | Part 7's Python environment, as it is; 4 threads; nothing else running |

`PY train_heads.py --cache <cache folder> --train <export folder> --tune <export folder> --seed <n> --loss focal|weighted --out <run folder>` (Part 7's Python; the two export folders are written by `notes export`, and a folder whose manifest's purpose is `measure` is refused). The tests of `train/` run under Part 7's Python and are part of the light checks when a commit touches `train/`.

**The 8-bit form against the 32-bit one, once at this rung:** with the chosen encoder and 1,000 notes, store 32-bit vectors of those notes and of the tune notes in a temporary folder, train the same heads on them, compare the tuning scores, report the difference, delete the folder. A loss of more than a point: rung 5, point 4.

A run on cached vectors takes minutes. The learning curve is 3 sizes × 3 seeds × 2 (with and without composed notes) = 18 runs: one evening.

### Rung 5. Training the encoder

**Only under the rule of the ladder's table.** If it runs:

1. **Top two layers.** The lower ten layers stay frozen: store their outputs once (32-bit) and train the top two layers and the heads on them. Learning rate 2e-5 for the encoder layers and 5e-4 for the heads, batch 16, 3 epochs, the same early stopping. Measure one epoch's time first; **tell the owner before any run longer than one night.**
2. **Quantise** the trained encoder to 8 bits; **rebuild the cache** with it; **refit the heads** on the new cache for at most 5 epochs; score that. A fine-tuned encoder's score before this step is not the reader's.
3. If the top two layers gained at least a point of the tuning score and a target is still missed: the same with the top four layers, as the second and last overnight run.
4. If the 8-bit copy loses more than a point against the 32-bit one: static quantisation with 200 training notes for calibration; then, if still more than a point, one more epoch with the quantised weights simulated. Report both figures.

After two overnight runs, anything more is "training beyond the plan": an owner stop point. No commit, build or test suite runs while a training or a timing runs.

**If a training run cannot be made to work** (a crash, a loss that does not fall, an out-of-memory error): two more attempts, each with one named change (a smaller batch, 384 pieces per window, a lower learning rate). After the third failure or one lost day: **stop and ask the owner.** Only with the owner's approval does the Codex training backup start ([codex-calls.md](codex-calls.md), "The training backup").

### Rung 6. Polarity, a first-class problem

It was 23 of Jais-3-3.7B's 40 critical errors on hidden v2. So:

- **Polarity families** in training and in test: one scenario written three or four times, only one element's polarity changed. Never split between sides. Score **per family**: a family is right only if every member is.
- **The challenge set:** 50 notes per difficulty, 25 by each writer ([scenario-sampler.md](scenario-sampler.md)). Results per difficulty and per writer, always. At 50 notes a difficulty can show a weakness but cannot prove a 95 % target.
- **The rule vote:** when rung 2's cues and the head disagree on an element's polarity, the reader abstains on that element. Measure accuracy on answers and coverage with and without it **on the Codex half of the challenge set and of dev**; keep it if accuracy on answers rises by at least a point while coverage stays over its floor. Report both rows.
- **Errors by kind:** stated↔denied flips are the critical ones; count them separately everywhere.

### Rung 7. Clean against Codex-trained

At equal size (700 notes each), the same recipe, three seeds: score both on dev and challenge (per writer) and on the clean test split. Then the reader trained on the 700 clean notes against the best Codex-trained reader actually built. **"Close" = within 2 points on claim type and on polarity, on the Codex half of dev** (this handbook's working value, not the owner's). The table decides which notes train the shipped reader (notes pipeline, section 8); if close, both candidates go to the mirror set.

### Rung 8. Confidence and abstention

`research/core-logic/train/calibrate.py`, purpose `tune` only (the Codex half of dev and the clean dev split, together):

- **Temperature scaling**, fitted by minimising the negative log-likelihood: one temperature for the claim head, one for polarity, one for requests, one for value roles, one for parties, and **one for each slot** (so that a slot added later has its own and moves no other). If a reliability table is still crooked (a bin off by more than 5 points with at least 50 answers), isotonic regression for that head.
- **Thresholds.** Where the scorer allows abstention (the claim, polarity, each slot, parties): the lowest threshold on the answer's confidence at which accuracy on answered items reaches the rate that would be "met" at v3's size ([scorer.md](scorer.md), size table: for example 96.4 % on the claim type, 98.5 % on outside, 96 % on a slot's known values) while coverage stays at or over its floor. One threshold for the claim, one for polarity (judged on the pooled mentioned elements), one per slot (each slot has its own target), one for parties. If no threshold does both: keep the one at the coverage floor and report "target not met at the floor", with the accuracy reached. The claim head's 0.5 for "this class is answered" stays as it is; the threshold decides abstention on the note. **Requests and value roles have no abstention**: their 0.5 is applied after the temperature.
- **One set of files per candidate.** When two candidates go to the mirror set (rung 7), each has its own thresholds file, its own export and its own parity run.
- **Check, without retuning,** on the Sonnet halves, the challenge set and the clean test split. A reader calibrated on the tune notes and overconfident on the Sonnet half has learned Codex's manner; say so.
- The temperatures and thresholds are one file, `reader-thresholds.json`, hashed in `ReaderInfo.Files` and in the pre-commitment.

### Rung 9. In .NET

- **The .NET tokenizer is the reference.** Training uses the piece ids that `Microsoft.ML.Tokenizers` produces (`reader ids`), never Python's own tokenisation: a single differing piece shifts every evidence span.
- **Offsets:** evidence points into the **original** string (UTF-16 units, before normalisation). Test the offset map by round trip on notes with diacritics, tatweel, Arabic-Indic digits, a zero-width joiner and mixed line breaks.
- **The files the reader loads**, from a folder given at start (the tests point it at a tiny fixture model): `reader-encoder.onnx` (8-bit, about 105 MB), `reader-heads.onnx` (a few megabytes; inputs: `tokens` (1 × T × 768), `fields` (1 × T), `note` (1 × 768), `value_spans` (V × 2: the first and last piece of each value the parsers found) and `value_kinds` (V: date or amount); outputs: `claim` (22), `polarity` (75 × 4), `actor` (75 × 4), `disputedBy` (75 × 3), `requests` (24), one `slot_<id>` per slot head, one `parties_<claim>` per parties head, `value_roles` (V × the roles of `reader-labels.json`), and `token_logits` (one row per query, T long). The decoding keeps, for a value, only the roles that are candidates in this note), `reader-thresholds.json`, `reader-labels.json` (the order of every class and query, and the scheme version), `vocab.txt`. **The encoder and the heads are two files on purpose**: a new head changes only the heads file ([scheme-extension.md](scheme-extension.md)). `ReaderInfo.Files` holds the SHA-256 of all five; `ReaderInfo.Version` is `1.<the scheme's minor version>.<build>`.
- **The project** `DzLegal.CoreLogic.Reader` implements `INoteReader`: the tokenizer, the windows and the stitching, the two ONNX sessions, the decoding, the parsers and the cue table of rung 2, the abstention rules. **The tool** `reader`: `reader ids --notes <file> --out <folder>` (piece ids and offsets), `reader encode --notes <file> --model <folder>` (writes the cache), `reader values --text <file>` (the parsers alone: used by step 5's faithfulness script), `reader read --notes <jsonl> --setting typeGiven|typeUnknown --model <folder> --out <file>` (one `NoteReading` per line **into the file; nothing of a note or a reading is ever printed**, so that the same command can run on a hidden set), `reader parity --notes <file> --python <file> --model <folder>` (the Python file is `train/decode.py`'s output for the same notes: one `NoteReading` per line), `reader time --notes <file> --model <folder>` (per length class of the style cards: very short, short, medium, long).
- **Export:** the heads with `train/export_heads.py` (ONNX, the opset ONNX Runtime 1.31.1 supports); the encoder with `train/export_encoder.py` ("The front end"), from the original weights or from rung 5's.
- **Parity** (a command, run on the real model): on 200 notes of the Codex half of dev, the .NET reader's labels equal the Python heads' on the same cache, and every confidence is within 0.01.
- **The measuring guard.** `scripts/measure_guard.py` runs before and after every timing: the laptop is on mains (the system's power status), the CPU was under 15 % for the ten seconds before, and the active power scheme (`powercfg /getactivescheme`) is recorded. A timing whose guard failed before or after is void and is not reported. Memory is the peak private bytes of the process that holds the model (for a local generative model: Ollama's runner process).
- **Measure** on the laptop, on mains with Best performance and an idle CPU: time per note (median and 95th percentile, for each length class), peak memory, bytes on disk of everything the reader loads. Under one second and about 150 MB, or the report says by how much it misses.
- Packages go in `research/core-logic/Directory.Packages.props` only (`Microsoft.ML.OnnxRuntime` 1.31.1, `Microsoft.ML.Tokenizers` 3.0.0, both MIT). **Check the current API in the packages' own documentation before writing against it**; do not write quantisation or tokenizer calls from memory.
- Everything the reader loads is a local file. No download at run time, ever.

## A new head: a slot the rules added later

For a slot that [scheme-extension.md](scheme-extension.md) decides the reader should read:

1. Add the slot to `reader-labels.json` **at the end** of its list (an existing position never moves).
2. `train/train_heads.py --new-head <slot id> --from <heads checkpoint>`: **only the new query and the new slot's own `Linear 512→k` are trained.** The shared layers (`Z`, `c`, `W`), every other query and every other layer are frozen and copied bit for bit. The loss is counted only on notes whose `schemeVersion` knows the slot (the slot's pack). AdamW 1e-3, at most 20 epochs, early stopping on the slot's accuracy on the Codex half of its pack's dev part.
3. Export a new `reader-heads.onnx`. **`OlderOutputsUnchanged`** (a command, run on the real files): on the whole of dev, every output that existed before is identical, value and confidence, with the old and the new heads file. It cannot fail unless a shared layer moved: find which, never relax it.
4. Fit the new head's **own** temperature and threshold (a new entry of `reader-thresholds.json`; no existing entry changes) on the Codex half of the slot's dev pack. Run `OlderOutputsUnchanged` with the old and the new heads file **and** the old and the new thresholds file. `ReaderInfo.Version` takes the scheme's new minor version. Only then is the slot's `readFromNote` set to true ([scheme-extension.md](scheme-extension.md)).

A date slot needs no head: add its role vector to the roles table and train that one vector the same way.

**If the encoder was fine-tuned** (rung 5), a later head is trained on that encoder's cache; the encoder is never fine-tuned again for a slot.

## Decision table

| Situation | Do |
|---|---|
| CAMeLBERT-MSA and mix are within a point at rung 4a, on the tune notes, or within the spread | Keep mix. |
| The style gap is more than 5 points | Report it. **Change nothing because of it.** The owner may choose a remedy at the learning-curve stop point; it is then judged on the tune notes. |
| A head is under its target on the tune notes at 4,000 and the curve still rises by more than 1.5 points | The curve report recommends more notes for that head's cells, with their cost. The owner decides. |
| A head is under its target and the curve is flat | Rung 5, if its rule allows; otherwise the head ships with the threshold that reaches the target (it abstains more), and the coverage is reported. |
| Composed notes lower the tuning score, beyond the spread, at two of the three sizes | Drop them from the shipped recipe; say so. |
| The rule vote, measured on the tune notes, costs more coverage than its floor allows | Ship without it; report both rows. |
| The 8-bit encoder loses more than a point of the tuning score against the 32-bit one | Rung 5, point 4 (static quantisation first). |
| .NET and Python differ by more than 0.01 on a confidence | A tokenizer, offset or stitching fault. Find the first differing piece on the parity notes; do not relax the bar. |
| A note takes more than one second | Measure per stage. Usual cause: three windows. Report the notes affected. The cap may be lowered to two only if the tuning score does not fall beyond the spread on the tune notes. |
| The cache does not fit on the disk | Store 4,000 training notes and the test sets only; recompute the rest when needed. 32-bit vectors are never kept beyond the one comparison that needs them, and no head that ships is trained on them. |
| A package's API is not what this file assumes | The package's documentation wins. Note the difference in the report. |
| A training run fails three times, or a day is lost | Stop and ask the owner (rung 5, last paragraph). |
| The Codex training backup's three sessions do not solve it | Stop. The owner chooses: do without that rung, or a free GPU. |
| The reader meets every target on the tune notes before rung 5 | Do not run rung 5. |
| You are about to look at a Sonnet-half score to decide something | Stop: that is the tuning rule. Decide on the tune notes, then report the Sonnet half. |

## What the step 4 report contains (`reports/reader.md`)

0. First line of the report: every figure for a Sonnet-written half, the mirror set or hidden v3 rests on labels that two Claude models agreed on (HANDBOOK section 5b); the figures for the Codex halves and the clean splits rest on two families.
1. The ladder table, every rung: the targets' measures with intervals, both settings, **the Codex half and the Sonnet half of dev and of the challenge set side by side**, the clean test split, the app's own cases.
2. Polarity per class, per difficulty, per family; critical flips.
3. The learning curve; the encoder choice; the clean-against-Codex table; with and without composed notes.
4. Calibration: the reliability tables on the tune notes and, unchanged, on the Sonnet halves.
5. Size, time, memory in .NET; the parity result.
6. The probes ([probes.md](probes.md)).
7. The pre-commitment for v3, with the mirror set's scores for the at most two final candidates (scored once); then, after the owner's run, the three-verdict table, the headline by the rule of [scorer.md](scorer.md), and the plain sentence on what the sizes can prove.
8. What did not work, with numbers.

## Traps

- **Choosing on the Sonnet half, the mirror set or a hidden set.** Rule 1. The loader refuses; do not go around it.
- **A 32-bit score reported for an 8-bit product.**
- **Evidence that is right by accident.** Score evidence overlap; a right polarity pointing at the wrong words will mislead the lawyer who checks it.
- **Outside the catalogue is not "low confidence".** Train it as a class with near-miss notes, and also measure the open-set behaviour: hold one `outsideKind` out of training entirely and test on it.
- **The app's own cases are few and written by people.** If the reader does well on v3 and badly on them, believe them.
- **Codex's manner.** The training notes have one writer. The Sonnet half, the clean test split and the app's own cases are the three places where that shows; report all three at every rung.
