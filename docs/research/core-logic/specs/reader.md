# Specification: the classifier and the reader (step 4)

**The job.** From a lawyer's note, produce what the rules need, each answer with the words it comes from: the contract `INoteReader` (`research/core-logic/src/DzLegal.CoreLogic.Contracts/NoteReading.cs`). The reader **proposes**; the lawyer confirms. An honest "I do not know" (abstention) is a good answer: the app then asks. A confident wrong answer is the failure to avoid.

**Targets on hidden v3** (the brief): claim type 95 %; outside the catalogue recognised 95 %; polarity 95 %; each other slot 90 %; abstentions counted separately. Exact definitions, and what each size can prove: [scorer.md](scorer.md).

**Limits** (the brief): CPU only; in .NET (ONNX Runtime, MIT) or plain code; about 150 MB on disk at most, everything included; under one second per note on the owner's laptop (i5-1335U, 16 GB, no GPU); every licence allows commercial redistribution; nothing from a hidden set in training. **Training** happens on the same laptop; a free GPU (Kaggle, Colab free tier) only after asking the owner, with fictitious notes and code only, no hidden data, no payment details, no new account without a yes.

## What you may use

Everything below is allowed without asking. **Anything else is a stop point: ask the owner first** (HANDBOOK section 8, "any download not listed").

| What | Where, or where from | Notes |
|---|---|---|
| The Python environment of Part 7 | `C:\Users\TheShadow\dz-arch-research\venv` (Python 3.12.10; torch 2.15.0+cpu, transformers 5.22.2, onnxruntime 1.31.1, onnx 1.23.2, scikit-learn 1.9.1, tokenizers 0.23.3, sentencepiece 0.2.3, numpy 2.5.4) | Use it as it is: install nothing into it. Record its package list with every result. |
| Python 3.12 with pytest | `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312` (`py -3.12`, pytest 9.0.3) | for the Python scripts' tests. The default `python` is 3.14 and has none of the packages above. |
| `pdftotext`, `pdftoppm` | installed | |
| CAMeLBERT-mix | the local copy `C:\Users\TheShadow\dz-arch-research\F\models\camelbert-mix` | Copy it to `C:\Users\TheShadow\dz-core-research\models\camelbert-mix\` with its SHA-256, size, licence link and date, and use the copy (the original belongs to Part 7's folder). |
| CAMeLBERT-MSA | its official model page (`CAMeL-Lab/bert-base-arabic-camelbert-msa`) | record size, SHA-256, licence link and date of the download |
| The Official Journal issues | joradp.dz, through the one fetch script `oj-fetch` | step 2 |
| NuGet `Microsoft.ML.OnnxRuntime` and `Microsoft.ML.Tokenizers` (MIT) | NuGet, when step 4 starts | in `research/core-logic/Directory.Packages.props` only |
| The local Ollama models of Part 7 | already installed | step 5; digests in `docs/research/fable-part7/run/models.json` on the branch `fable/architecture-review` |

**Part 7's files are on another branch.** Read them with `git show fable/architecture-review:docs/research/fable-part7/<path>` (for example `encoders/results/cls-camelbert-mix.json`, `run/models.json`). Never write there.

**Model files are never committed.** An 8-bit base encoder is over GitHub's 100 MB limit, and a commit holding one would block every later push. Models, token vectors and checkpoints live in `C:\Users\TheShadow\dz-core-research\models\`; the repository holds their SHA-256, their sizes and the exact commands that rebuild them. **Unit tests use a tiny fixture model committed for the plumbing** (a few kilobytes: it checks loading, shapes, offsets and the contract, not accuracy). Accuracy and parity runs are commands, not unit tests, so `dotnet test` is green in a clean clone that has no model file.

## The ladder: cheapest first, each rung measured before the next

**Every rung is reported on dev, challenge, the clean test split and the app's own cases**, in both settings, with intervals. **Stop climbing when the targets are met on dev and challenge with margin**: at the rates that the size table of [scorer.md](scorer.md) says would be "met" at v3's sizes, not merely at the target. Report every rung, also the ones that lost: the brief says "measure each idea; keep what wins". **The mirror set chooses nothing**: it is scored once, for the at most two final candidates, at the pre-commitment ([hidden-v3-commands.md](hidden-v3-commands.md), section 7).

| # | Rung | Cost | What it gives |
|---|---|---|---|
| 1 | **The scorer**, with its tests | half a day | the yardstick, fixed before any model |
| 2 | **Plain code:** exact reading of dates and amounts; rules for negation, restriction, refusal verbs and dispute verbs | 2 days | the floor; every value with its exact span; an independent vote on polarity |
| 3 | **A linear model on character n-grams** for the claim type and for outside | hours | a strong, tiny, fast baseline; and the leakage probe |
| 4 | **A pre-trained Arabic encoder, frozen,** with light heads | 2 days | claim type, each element's polarity with its words, value-to-role links, slots, in one pass |
| 5 | **The same with the top layers trained,** then one or two full overnight runs | 2 to 4 days | only if rung 4 misses a target |
| 6 | **Polarity as its own problem** | throughout | families, challenge set, rule vote, abstention |
| 7 | **A small model trained from scratch** on notes and law text | one day, bounded | the brief's invitation to invent; kept only if it wins |
| 8 | **Calibration and abstention** | 1 day | confidences that mean something |
| 9 | **Clean-trained against Codex-trained** | hours | which notes the shipped classifier is trained on |
| 10 | **In .NET,** with the same tokenizer | 2 days | the thing the app can run |

**Two optional rungs, measured only if cheap** (an afternoon each, after rung 7): an **ensemble** of the rules, the linear model and the encoder (a vote or a small stacker trained on dev's folds; the rules and the linear model add a few megabytes, so it fits the budget); and **distillation** of the big encoder into rung 7's small model (the small model trained on the big one's outputs over the training notes; it would win on size and speed if it came within a point). An ensemble with a second encoder does not fit: one 8-bit encoder already takes most of the 150 MB (rung 4, "Size and speed").

### Rung 2. Plain code

- **Dates:** every format of the labelling guide (section 8): `dd/mm/yyyy` with `/`, `-` or `.`; day + month name (both the Levantine names and the Algerian ones: جانفي، فيفري، مارس، أفريل، ماي، جوان، جويلية، أوت، سبتمبر، أكتوبر، نوفمبر، ديسمبر; also يناير، فبراير … for completeness); Arabic-Indic digits; «سنة 2015»; month + year (normalised `yyyy-mm`). Output: normalised value and exact UTF-16 span in the original string.
- **Amounts:** digits with `.`, space or `,` as separators; words («خمسمائة ألف», «مليون ونصف»); words with digits in brackets; units دج، دينار، د.ج، سنتيم (÷ 100), مليون / مليار.
- **Polarity cues,** as a table with scopes, tested sentence by sentence against the hard cases of the guide's section 4.2 and its 41 kind rows: negation particles (لم، لا، لن، ما، ليس، غير، عدم، دون، بدون، بلا، انعدام، لا وجود لـ); **restriction** (لم/لا/ما … إلا / سوى / غير: the scope after the particle is affirmed); refusal and omission verbs (امتنع، رفض، تخلف، تقاعس، أحجم، أغفل، أهمل); dispute verbs with their subject (ينكر، ينفي، ينازع، يدعي، يزعم، يتمسك، يدفع بـ، يطعن في); "despite" (رغم، مع أن، بالرغم من: the clause after it is affirmed); future and intention (سـ، سوف، يعتزم، ينوي: not an event).
- The rules give, for an element whose trigger words they find, a polarity vote and the span. They do not need to be right everywhere: they must be **independent of the learned model**, so that a disagreement means something.

This rung alone is scored on every open test set. Its date and amount reading is expected to be near exact and is what the final reader uses for values; a learned model only links values to roles.

### Rung 3. The linear model

Character 2- to 5-grams (after the level-2 normalisation of [article-store.md](article-store.md)), TF-IDF, a linear classifier, one-vs-rest so that two claims can both fire, plus an `outside` class. Trained in seconds; a few megabytes. Report it everywhere as the "no deep model" line. Its largest weights are the leakage probe ([probes.md](probes.md)).

### Rung 4. A frozen encoder with heads

**Encoder.** Start with **CAMeLBERT-mix** (`CAMeL-Lab/bert-base-arabic-camelbert-mix`, Apache 2.0, 109 M parameters): on Part 7's 46 open cases it identified the claim type in 37 alone and 40 with the section filter, the best of six encoders, level with DziriBERT (37 and 38) and ahead of mmBERT-small (35 and 37); the two multilingual small models were far behind (e5-small 9 and 12, MiniLM 6 and 6). Since the notes are now formal Arabic only, also try **CAMeLBERT-MSA** (same licence, trained on formal Arabic). Check each model's licence file on the day and record the link and date.

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

Two facts to carry into your own measurements of the .NET reader, which go in `docs/research/core-logic/reports/step-4.md`:
- **A long note is more than one window.** A long note (about a page, some 350 words) is about 515 CAMeLBERT tokens at Part 7's 1.47 tokens per word, so one or two windows of 512. Measure the median and the 95th percentile per note on dev notes of each length class, not per sentence.
- **The budget leaves room for one encoder.** About 105 MB for the 8-bit encoder, about 20 MB for ONNX Runtime's native library (win-x64 only), about 1 MB for the tokenizer, plus the heads and the thresholds: about 130 MB.

**One pass, several heads.** Encode the note once (fields joined with a separator token and a field marker; notes longer than the window are cut into overlapping windows and the heads' outputs merged by maximum). On the token vectors:

- **Claim type:** a linear layer on the pooled vector, sigmoid per type plus `outside`. Answers = the types above their threshold, at most three.
- **Polarity with evidence, per element:** each element (claim type, event kind) has a learned query vector; attention of the query over the tokens gives (a) a 4-way polarity for that element and (b) per-token evidence weights, trained against the gold evidence spans. The evidence span answered is the best-scoring stretch; **an answer other than `NotMentioned` with no span above the floor is turned into an abstention by the reader itself**.
- **Value roles:** for each date and amount that rung 2 found, the vector of its span joined with each candidate role's query → belongs or not. A value may have several roles.
- **Slots:** each `readFromNote` slot as a small classification head with the same evidence mechanism; date slots as value roles.
- **Requests:** multi-label over the request kinds, with evidence.

Frozen first: run the **8-bit** encoder once over all notes, through ONNX Runtime with the .NET tokenizer's ids (rung 10), store the token vectors, and train the heads in minutes on CPU. Heads trained on 32-bit vectors would see shifted inputs once run on the 8-bit encoder, so their scores would not count. The vectors are 768 numbers of 2 bytes per token: about 1.2 to 1.8 GB for 4,000 notes of 200 to 300 tokens, several GB for all the open sets. They live in `C:\Users\TheShadow\dz-core-research\models\vectors\`, never in the repository. That makes the learning curve, the with/without-composed comparison and the seeds cheap.

**Blanked fields** carry no loss (notes pipeline, section 5). Class weights or focal loss for the rare polarities; report both.

### Rung 5. Training the encoder

Only if rung 4 misses. First the top two to four layers at a small learning rate; then, if still short, one full overnight run (measure the time for one epoch first and tell the owner before a run of more than one night; anything beyond two overnight runs is "training beyond the plan": an owner stop point). Quantise to 8 bits after training and **score the 8-bit model**; if the loss from quantisation is more than a point on any target, try quantisation-aware fine-tuning before giving up the larger model. No commit, build or test suite runs while a training or a timing runs: commits wait for the CPU to be idle.

### Rung 6. Polarity, a first-class problem

It was 23 of Jais-3-3.7B's 40 critical errors on hidden v2. So:

- **Polarity families** in training and in test: one scenario written three or four times, only one element's polarity changed. Never split between sides. Score **per family**: a family is right only if every member is.
- **The challenge set:** at least 50 notes per difficulty, with the difficulties listed in [scenario-sampler.md](scenario-sampler.md) (the challenge set's row). Results per difficulty, always. At 50 notes a difficulty can show a weakness but cannot prove a 95 % target (scorer.md, size table).
- **Contrast in training:** pairs from a family in the same batch; optionally a contrastive term that pushes their element vectors apart.
- **The rule vote:** when rung 2's rules and the model disagree on an element's polarity, the reader **abstains** on that element (to be measured: does this raise accuracy on answers enough to pay for the coverage it costs?). Report accuracy and coverage with and without it.
- **Errors by kind:** stated↔denied flips are the critical ones; count them separately everywhere.

### Rung 7. A small model from scratch (bounded: one day)

The brief invites it: "a model designed and trained from scratch for Algerian legal notes (its own tokenizer for formal legal Arabic; morphology; a legal lexicon)". One honest attempt: a tokenizer of about 8,000 pieces trained on the notes and on the official law texts of the article store; a 4-layer, 256-wide encoder (about 5 to 8 M parameters, under 30 MB); pre-training by masked words on the same texts for a few hours; the same heads. Compare on dev and challenge with rungs 3 and 4 at equal training notes. **Keep it only if it is within a point of the big encoder** (it would then win on size and speed) **or better.** Otherwise report the numbers and drop it. Do not spend a second day.

### Rung 8. Confidence and abstention

- **Calibrate** on dev (temperature scaling per head, or isotonic regression if the reliability table is crooked): among answers given with confidence c, a share c are right.
- **Thresholds** for abstention are chosen on dev, **subject to the coverage floors fixed in advance** ([scorer.md](scorer.md)): a reader that abstains on half the notes meets no target. Aim them at the accuracy that would be "met" at v3's size (scorer.md, size table: for example 96.4 % on the claim type, 98.5 % on outside, 96 % on a slot's known values), not at the bare target: a reader tuned to exactly 95 % on dev will come out "inconclusive" on v3.
- Check the calibration on challenge and the clean test split without retuning. A reader that is calibrated on dev and overconfident on clean notes has learned the generator; say so.
- The thresholds file is part of the reader (hashed in `ReaderInfo.Files` and in the pre-commitment).

### Rung 9. Clean against Codex-trained

At equal size (1,000 notes each), same recipe, three seeds: score both on dev, challenge and the clean test split. "Close" = within 2 points on claim type and polarity. The table decides which notes train the shipped classifier (notes pipeline, section 8). Also: with and without composed notes, at each size, decided on dev and challenge.

### Rung 10. In .NET

- **The .NET tokenizer is the reference.** Train on the token ids that `Microsoft.ML.Tokenizers` produces (a small .NET tool writes ids and offsets for every note; the Python training code reads them). Never tokenise in Python for training and in .NET for use: a single differing piece shifts every evidence span.
- **Offsets:** evidence must point into the **original** string (UTF-16 units, before normalisation). Keep the offset map through normalisation and tokenisation; test it by round trip on notes with diacritics, tatweel, Arabic-Indic digits, a zero-width joiner and mixed line breaks.
- **Export** to ONNX; quantise to 8 bits; run in ONNX Runtime from the project `DzLegal.CoreLogic.Reader` implementing `INoteReader`. The model files are loaded from a folder given at start (the reader's tests point it at the fixture model). **Parity test** (a command, run on the real model): on 200 dev notes, the .NET reader's answers equal the Python model's (same labels; confidences within 0.01 before quantisation; after quantisation, report the label changes).
- **Measure** on the laptop, on mains with Best performance and an idle CPU: time per note (median and 95th percentile, short and long notes), peak memory, bytes on disk of everything the reader loads. Under one second and about 150 MB, or the report says by how much it misses.
- Packages go in `research/core-logic/Directory.Packages.props` only (`Microsoft.ML.OnnxRuntime`, `Microsoft.ML.Tokenizers`, both MIT). **Check the current API in the packages' own documentation before writing against it**; do not write quantisation or tokenizer calls from memory.
- Everything the reader loads is a local file. No download at run time, ever.

## What the step 4 report contains

1. The ladder table, every rung, on dev, challenge, the clean test split and the app's own cases: the targets' measures with intervals, both settings.
2. Polarity per class, per difficulty, per family; critical flips.
3. The learning curve and the clean-versus-Codex table (both decided on dev and challenge).
4. Size, time, memory in .NET.
5. The probes (leakage, masking, writer identity).
6. The pre-commitment for v3, with the mirror set's scores for the at most two final candidates (scored once); then, after the owner's run, the three-verdict table, with the headline by the rule of [scorer.md](scorer.md), and the plain sentence on what the sizes can prove.
7. What did not work, with numbers.

## Traps

- **Choosing on the mirror set.** Models, thresholds and the stopping point are chosen on dev and challenge. The mirror set is scored once, for the at most two final candidates, to predict v3.
- **A 32-bit score reported for an 8-bit product.**
- **Evidence that is right by accident.** Score evidence overlap; a right polarity pointing at the wrong words will mislead the lawyer who checks it.
- **Outside the catalogue is not "low confidence".** Train it as a class with near-miss notes, and also measure the open-set behaviour: notes of a kind never seen (hold one `outsideKind` out of training entirely and test on it).
- **The app's own cases are few and written by people.** If the reader does well on v3 and badly on them, believe them.
