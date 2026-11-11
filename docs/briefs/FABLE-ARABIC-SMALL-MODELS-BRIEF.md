# Brief: find and measure the best Arabic-first small models

*For Fable, continuing on `fable/architecture-review`, 11 November 2026. The owner's verdict on Part 6: it gave more questions than solutions. This session must give measured answers.*

## The change of method

Part 6 read papers and measured nothing. This session does the opposite: it **runs models on this laptop, with the project's own test bench, and reports numbers**. A paper is only a reason to choose what to measure.

Part 6 also passed over the models that matter most: small models **made for Arabic**, and models made for **Algerian** language. Jais-3-0.9B (Apache 2.0, 666 MB at 4 bits) scores 55.6 on Arabic Legal against 59.2 for Gemma 4 E4B, yet it was set aside without a test. Arabic-first models also tokenise Arabic into fewer pieces per word, which makes them faster on this CPU for the same note. They are the priority now.

## What to deliver

Measured results, a ranked shortlist, and one recommendation the owner can act on. Not proposals for later experiments. Questions to lawyers are allowed only as a final appendix of at most three questions, each with what its answer would change.

## Step 1. Find the candidates (short, sourced)

Search for every model that is **Arabic-first or Arabic-strong, small, and redistributable**:

- **Generative, up to about 2B parameters:** at least the Jais 3 family (0.9B; and the 3.7B as a larger reference), the Falcon-H1 family's small sizes and any Arabic-specific Falcon release, SILMA's small models, Atlas-Chat (Darija), IBM's Arabic Granite models, and any newer Arabic-first model of 2025 and 2026 you find.
- **Encoders, for classification and tagging:** Algerian-dialect encoders (for example DziriBERT), CAMeLBERT, AraBERT, AraELECTRA, and Arabic or multilingual small encoders with a clean licence.

For each: exact model id, release date, parameters, licence (and whether it allows commercial redistribution), 4-bit or int8 file size, availability as GGUF (for Ollama or llama.cpp) or ONNX, and its published Arabic and legal scores. **Verify, don't assume:** a model whose licence forbids commercial use is listed, marked, and not measured.

Then choose a shortlist: **at most 5 generative models and at most 4 encoders**, and say why each one made it.

## Step 2. Measure the generative models on this laptop

Use the project's own instruments, unchanged, exactly as for the Gemma 4 E2B run of 9 November (`docs/baselines/2026-11-09-alpha-03-hidden-v2-phone-models/`):

1. **Fact reading on hidden set v2**, claim type given, with the owner's guarded command `tools/alpha-03/Run-OwnerAlpha01.ps1` and the unchanged Alpha-01 reader. You are a blind operator: never open the hidden folder; read only each run's `summary.md`. This is allowed because only the model changes; the reader does not.
2. **The app's own analysis on the 46 development cases** (the stage 1 benchmark: claim type exact, core articles, out-of-scope declined, time per case).
3. **For the best one or two, the drafting benchmark** on the same 12 cases as stage 2, and a side-by-side reading of the drafted grounds against Gemma 4 E4B's committed outputs.

Report for each: cases fully right, every field of the summary, time per case, peak memory, file size, and **tokens per Arabic word** for its tokenizer.

Comparison points already measured (do not rerun): gemma4:e4b 58 of 90, gemma3:4b 39, gemma4:e2b 41, gemma3n:e2b 42.

## Step 3. Measure the encoders on what they are for

Claim type is the cleanest test: a closed set of 21 types plus "other".

- Fine-tune each shortlisted encoder **on the laptop** as a claim-type classifier. Training data: synthetic fictitious notes you generate across the 21 types and "other", in formal Arabic, Darija and French. No hidden-set case, ever.
- Test on the 46 development cases' claim types, which the app already uses (Gemma 4 E4B: 46 of 46; Gemma 3 4B: 41 of 46).
- Report accuracy, size, memory and time per case.

If one encoder is clearly ahead, and time allows, also try it on one fact-reading sub-task with a clear answer: which request kinds a note asks for.

## Step 4. If a small Arabic model is close, try one fine-tune

Only if the best generative model of step 2 is within about 10 cases of Gemma 4 E4B on hidden v2: fine-tune it once, with LoRA, on synthetic fictitious cases written by a teacher whose licence allows training on its outputs (Gemma 4 31B or 26B is Apache 2.0). Stop and ask the owner before any spending or any API key; never print a key. Measure the result exactly as in step 2. Because it is a changed model trained on data unrelated to v2, report it as indicative; the definitive measure stays a future hidden set v3.

## Step 5. The answer

1. **One table:** every measured model, generative and encoder, against Gemma 4 E4B, with accuracy, size on disk, peak memory and time per case.
2. **The recommendation**, in plain words: which model or combination the app should use, for which task, at what download size, and what it costs in accuracy, if anything. If nothing under 1 GB is good enough yet, name the smallest thing that is, and what one step would close the gap.
3. **What changed since Part 6:** which of its estimates the measurements confirmed or overturned.

Write it as Part 7 of `docs/research/FABLE-ARCHITECTURE-REPORT.md`, and push only to `fable/architecture-review`.

## Rules

- **Laptop and disk:** about 5 GB free. Download one model at a time and remove it (`ollama rm`, or delete the file) once measured. Keep gemma3:4b and gemma4:e4b. Take timing and memory measurements only on mains power with Best performance; if the laptop is on battery, say so and wait.
- **Hidden sets:** never open them; only the owner's command reads them, and only `summary.md` leaves it. Nothing is tuned on v1 or v2.
- **Data:** fictitious cases only. No case data, no real names.
- **Repository:** change no existing file and no other branch. Your only file is the report. Do not touch the app, the legal data, the prompts or the reader.
- **Sources:** every claim about a model (licence, size, score) with its link and date, as in Parts 5 and 6.
- **Budget:** keep agent runs lean (about 10); the time should go into running models, not into more reading.
