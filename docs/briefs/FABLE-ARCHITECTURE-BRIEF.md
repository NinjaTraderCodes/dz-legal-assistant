# Brief: rethink DZ Legal Assistant from first principles

*For a single deep-research session with the most capable model available. Written 10 November 2026.*

## Who you are in this session

You are not a helpful assistant tidying up a plan. You are the strongest independent reviewer and architect this project will ever get: part legal-tech auditor, part small-language-model researcher, part systems inventor. The team has spent weeks improving this product step by step. What it needs from you now is what step-by-step work cannot give: a **brutally honest audit**, and **genuinely new architectures**, reasoned from first principles and checked against the state of the art.

Two failure modes to avoid above all:

1. **Polite incrementalism**: suggesting tweaks to what exists because it exists. Question every component, including whether it should exist at all.
2. **Unanchored brilliance**: exciting ideas with no numbers, no source and no way to test them cheaply. Every idea you recommend must come with a back-of-envelope cost, the evidence for it, and the cheapest experiment that could prove it wrong.

## The product, in brief

**DZ Legal Assistant** is an offline Windows desktop app for Algerian lawyers. It drafts the *عريضة افتتاح دعوى* (statement of claim) in formal Arabic, checks the mandatory elements of form, and proposes the applicable articles from the official texts, quoted by code, never from the model's memory. The lawyer reviews every AI-written section before export. Case data is covered by professional secrecy and **never leaves the lawyer's computer**: the AI runs locally, reached only on `127.0.0.1`. A public web demo uses the same screens with a hosted model, on fictitious cases only.

Repository (public): `github.com/NinjaTraderCodes/dz-legal-assistant`, default branch `claude/stoic-fermat-mi1gko`. If you can read it, read at least:

| What | Where |
|---|---|
| Project rules | `AGENTS.md` |
| Product, measured results, local model set-up | `README.md`, `docs/PRODUCT-DETAILS.md`, `docs/ARCHITECTURE.md` |
| The form and the document's sections | `data/document-types/requete-introductive/definition.json` |
| The 21 claim types and their articles | `data/law/claims/*.json` |
| The law library | `data/law/codes/*.json`, `docs/LAW-LIBRARY.md` |
| The model prompts | `data/law/prompts/*.txt`, `data/document-types/requete-introductive/prompts/*.txt` |
| The AI pipeline | `src/DzLegal.Drafting/` (`GroundsAnalyzer.cs`, `ArguedGrounds.cs`, `ClaimFactReader.cs`, `DraftingService.cs`, `SubstanceChecks.cs`, `NarrativeChecks.cs`) |
| How accuracy is measured | `docs/HIDDEN-SET.md`, `docs/baselines/` (summaries only) |
| The last research round | `docs/LOCAL-READER-SEED.md`, `docs/briefs/ALPHA-03-BRIEF.md` |
| The model-setup work in progress | branch `codex/model-setup`: `docs/MODEL-SETUP-PROPOSAL.md`, `docs/MODEL-SETUP-IMPLEMENTATION.md` |

If you cannot read the repository, say so at the start and work from this brief.

### How it works today

1. **The form** (pure code): parties, court, lawyer, claim, notes on facts and requests, documents, date and place. A rule engine checks the mandatory elements (CPCA art. 15 and related): 360 of 360 correct on 24 test statements.
2. **The analysis** (`GroundsAnalyzer`), three stages: (1) the model reads the facts and translates them into the codes' vocabulary and recognises the claim type; (2) the law library is searched by BM25, no model, and the claim type's catalogue articles are added; (3) the model chooses among those candidates by key, with a role and a reason. The lawyer must confirm the claim type before anything is used.
3. **The drafting** (`ArguedGrounds` and the section prompts): the model writes the legal grounds paragraph by paragraph; the code quotes each article from the official text, and automatic checks catch wrong law, numbers, stray Latin words, ungrounded terms and copied examples, with retries and safe fallbacks. The rest of the document is templated from the form.
4. **A separate fact reader** (`ClaimFactReader`, "Alpha-01"), used in the test bench to measure reading quality: parties' positions, decisive events, dates, amounts, requests, and whether each fact is stated, denied or disputed.

**Scope:** 21 claim types. Civil: loan repayment, unpaid sale price, contract rescission, sale annulment for fraud, contract damages, tort damages, liability for things, unpaid rent, eviction of an occupier without title. Commercial: commercial debt, commercial lease eviction, sale non-delivery. Family: divorce by the husband, tatliq, khul', nafaqa, custody, custody forfeiture, visitation. Labour: abusive dismissal, unpaid wages.

**The law library** holds 1,361 articles from 6 texts, but unevenly: the CPCA (1,084 articles) and the Family Code (227) are complete; the **Civil Code has only 29 articles**, the Commercial Code 4, the two labour laws 17 between them. A fuller corpus is being built on a separate branch (`codex/law-corpus`). Treat this gap as part of the audit.

### The machine the product must run on

Owner's laptop, typical of the target: Windows 11, Intel Core i5-1335U (10 cores, 12 threads), **16 GB RAM, no graphics card** (Intel UHD only), about 9.5 GB free disk. Many Algerian lawyers have **8 GB** laptops and slow or metered internet. The local engine is Ollama 0.40.15, CPU only, context 4,096, temperature 0.2.

### What has been measured

**Hidden test sets** (written by independent authors, never opened by the developers, 90 in-scope cases each plus 10 out of scope; "fully right" means no critical error in the reading):

| Set | Model | Fully right | Position | Events | Dates | Stance | Requests | s/case | Peak RAM |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| v1 (31 Oct) | Nemotron 3 Ultra, online | 71% | 100% | 99% | 95% | 93% | 99% | 4 | n/a |
| v1 (30 Oct) | gemma3:4b, battery | 36% | 97% | 88% | 80% | 81% | 93% | ~60 | |
| v2 (8 Nov) | gemma3:4b | 43% | 94% | 89% | 83% | 86% | 92% | 28 | 6.5 GB |
| v2 (8 Nov) | **gemma4:e4b**, thinking off | **64%** | 100% | 98% | 92% | 91% | 95% | 21 | 7.4 GB |
| v2 (9 Nov) | gemma4:e2b, thinking off | 46% (41 of 90) | 93% | 93% | 89% | 85% | **63%** | **7** | 5.1 GB |
| v2 (9 Nov) | gemma3n:e2b | 47% (42 of 90) | 97% | 93% | 77% | 86% | 69% | 19 | 6.9 GB |

All rows use the same reader (Alpha-01); only the model changes. With the claim type read by the online model itself: claim type right in 94% of cases, all 10 out-of-scope cases declined. Gemma 4's remaining critical errors: 24 stance errors (19 of them a **disputed fact read as stated**), 4 amounts and 4 dates on the wrong event, 4 forbidden readings, 3 missing decisive events. Gemma 4 E2B understands the case about as well as Gemma 3 4B but loses requests: 36 answer lines its parser could not read, against 3 for Gemma 3.

**The app's own steps, on its 46 development cases** (optimistic: the app was tuned on them):

| | gemma3:4b | gemma4:e4b |
|---|---:|---:|
| Claim type exact | 41/46 | 46/46 |
| Core articles found, model's own choice | 41/68 | 49/68 |
| Out-of-scope declined | 3/4 | 4/4 |
| Analysis time per case | 70 s | 56 s |
| Drafting (12 cases, 8 claim types): formally complete | 11/12 | 11/12 |
| Drafting time per case | 39 s | 25 s |

A human reading of the 24 drafts found no swapped roles or invented facts in either model; Gemma 4's grounds were closer to the facts in 7 of 12 cases, and Gemma 3 missed the decisive fact twice. Problems shared by both models come from the pipeline: the general tort article (124) used in contract claims, a damages paragraph with no matching request, a damages request with no grounds. **Drafting has never been measured for 13 of the 21 claim types**, including every family type except tatliq.

**Download sizes** (the first-impression problem): gemma3:4b 3.35 GB, gemma4:e4b 6.58 GB, gemma4:e2b 4.59 GB, as Ollama installs them (the E-models include vision and audio parts the app never uses). The engine itself is now a 30 MB CPU-only bundle.

### What has already been tried, so you don't repeat it

- **A cleverer reader on the same small model** ("Alpha-03", a scored stance reader with probes): falsified, 21 to 23 against 36 of 96 on the fitting cases. The big gain came from a stronger model with the same reader.
- **Thinking mode on Gemma 4**: spent the whole 300-token answer allowance thinking on every case; off is mandatory.
- **Other local models**: qwen2.5:3b weaker; qwen3:4b too slow and reasons in English; qwen2.5:7b drafts better but is slower.
- **A text-only copy of Gemma 3** (vision part removed): about 0.8 GB less memory, identical answers.
- **A hosted model for the demo**: works well, but it can never be the desktop product's default, because case data must not leave the computer. An opt-in online mode with client names hidden on the laptop is planned for later, as the lawyer's choice, case by case.
- The research seed `docs/LOCAL-READER-SEED.md` lists further ideas considered and why; read it before proposing something similar.

## Non-negotiable constraints

- **Privacy:** case data never leaves the computer. No telemetry. The app talks to the model only on loopback. Any network use (for example, downloading a model) happens only on the lawyer's explicit click and is disclosed.
- **Hardware:** must run on CPU only; 16 GB is the comfortable target, 8 GB must still give a useful product.
- **Law:** articles are quoted by code from the official texts, never generated. The lawyer confirms the claim type and reviews every AI-written section before export. Keep these safety behaviours.
- **Licences:** every model, dataset and library shipped must allow redistribution; say which licence each of your proposals depends on.
- **Data:** tests and samples use fictitious cases only. You may propose generating synthetic fictitious cases; say how their realism and legal accuracy would be checked by a practising lawyer.
- **Evaluation honesty:** the hidden sets v1 and v2 are now seen at summary level. Any change to the reader or the pipeline must be measured on a **new hidden set (v3) by an independent author**. Never propose tuning against hidden-set results.
- **This session is research only:** change no existing file. Work on your own branch, `fable/architecture-review`, created from `claude/case-file-site`. The only thing you write is your report, as one new file, `docs/research/FABLE-ARCHITECTURE-REPORT.md`, pushed to that branch. Never merge, and never push to any other branch. If you cannot push, give the whole report in your answer instead.

## What to deliver

### Part 1. A brutal audit: are we ready for production?

Give a verdict first, in one paragraph: ready for what, for whom, and what is missing. Then go deep:

1. **Coverage against real practice.** Which kinds of statements of claim do Algerian lawyers actually file most, by court section (civil, commercial, family, social, real estate, urgent proceedings, administrative)? Find real statistics where they exist (Ministry of Justice, court activity reports, bar sources, academic work), and say clearly when you are estimating. Which frequent claim types are missing from our 21? Which of our 21 are rare? What share of a typical lawyer's statements of claim could the product handle today?
2. **Legal completeness.** Is a 29-article Civil Code enough for the civil claim types we offer? What does each claim type need that the library and catalogue lack (articles, procedural rules, deadlines, competent court, required documents)? What legal errors could the current design produce that no automatic check would catch?
3. **The pipeline, component by component.** For each stage: what it is for, how it fails, how we would know, and whether it should exist. Pay special attention to the reading → analysis → drafting chain, where an early mistake flows downstream, and to the known error classes above.
4. **The measurement itself.** Are 90 cases enough to tell the models apart, and with what uncertainty? What does "fully right" miss? What would a production-grade evaluation look like, including drafting quality judged by lawyers?
5. **Ranked findings**: every problem you find, ordered by harm to a lawyer, each with a fix and its cost.

### Part 2. Solve the model-size problem

A first download of 3.35 to 6.58 GB is a poor first impression on a slow connection, and the model's size drives memory, speed and which laptops can run the product at all. Find the smallest system that gives **at least Gemma 4 E4B's accuracy on our tasks**, and the best trade-offs below that. Consider every layer: what the model must do at all (task decomposition), which tasks need a generative model and which could use something far smaller (encoders, taggers, classifiers, rules, retrieval, constrained decoding, morphological analysers), quantisation (down to 4, 2 or 1.58 bits), removing unused vision and audio parts, vocabulary pruning, distillation, progressive delivery (a small model shipped in the installer, a larger one optional), and anything else you find. For each serious option, give the expected size on disk, peak RAM, latency on the i5-1335U, the accuracy risk, and the evidence.

### Part 3. Architect the optimal harness, in a novel way

Design the system you would build if you started today, knowing everything above.

**Method, in this order:**

1. **Decompose.** List every atomic decision the product must make, from "which claim type is this?" to "is this paragraph's argument sound?". For each one, ask: does it need language understanding at all? What is the smallest mechanism that could make it reliably? Could the lawyer make it faster than a model, with one well-designed question?
2. **Diverge before you converge.** Produce at least **eight genuinely different architectures** before judging any. At least two must use no generative model at run time; at least one must fit in under 300 MB; at least one must change the interaction itself (for example, the app asking the lawyer targeted questions instead of extracting everything from free notes); at least one must be something you have not seen proposed anywhere. Label each idea *known*, *adapted from another field* or *novel*.
3. **Attack each one.** Steelman it in two sentences, then run a pre-mortem: it is six months later and it failed; why? Give its numbers (size, RAM, speed, accuracy risk, build effort).
4. **Converge.** Recommend one architecture, or a staged path between two, and explain why it beats the others on what matters most to a lawyer: correctness, completeness, speed, privacy, and first impression.
5. **Make it falsifiable.** For the recommendation, list the cheapest experiments that could prove it wrong, each with a pass mark set in advance, in the order to run them.

### Part 4. Could we train our own model on this machine?

Answer honestly, with the arithmetic. Training compute is roughly 6 × parameters × training tokens FLOPs; estimate what this CPU can really sustain, and from that what we could train or fine-tune in a day, a week and a month: a small encoder (for example an Arabic BERT-class model) for classification and tagging, LoRA on a 0.5 to 4B generative model, distillation from a larger teacher, or a model built from scratch. Cover:

- **The data:** where it would come from (synthetic fictitious cases from a stronger teacher, the official texts, public Arabic legal corpora), how much is needed, how a lawyer would check it, and the licence terms of each source, including whether a teacher model's terms allow training on its outputs.
- **What the trained model would replace** in the pipeline, and its expected size and speed.
- **Arabic legal language specifically:** which existing Arabic or Arabic-capable models and tools are the best starting points (for example Arabic encoders, Arabic-centric LLMs, morphological analysers), with sizes and licences.
- **The cut-off:** where CPU-only training stops being sensible. If a few hours of rented GPU would change the picture, say so separately, with the cost, but keep the CPU-only answer complete.

### Part 5. Research the state of the art

Search widely and recently (2025 and 2026): small language models and on-device inference, distillation and task-specific fine-tuning, structured and grammar-constrained generation, encoder-based information extraction, retrieval-augmented and citation-grounded legal drafting, Arabic and legal NLP, extreme quantisation and ternary models, models with elastic or nested sizes, speculative decoding on CPU, evaluation of legal AI. Look beyond mainstream NLP too: legal expert systems, document assembly, form-based legal tech, weak supervision, program synthesis. For every source you rely on, give the link and date, and say how directly it applies to us.

## How to think

- Separate clearly what you **know** (with a source), what you **estimate** (with the arithmetic), and what you **speculate**. Never present a speculation as a finding.
- Prefer numbers to adjectives. "Smaller" means nothing; "a 110 M-parameter encoder at 8 bits, about 120 MB, under 1 s per case on this CPU" is useful.
- When the evidence is weak, say so and design the experiment instead of guessing.
- Disagree with this brief where it is wrong, including its framing, its constraints or the team's past decisions, and explain why.
- Spend your effort where the stakes are: a wrong article or a missing mandatory element harms a client; a slower draft does not.

## Format of the answer

1. **Verdict** (one paragraph) and the **five decisions** you would make this month.
2. Part 1: the audit, with the ranked findings table.
3. Part 2: the model-size options table and your answer.
4. Part 3: the eight-plus architectures (one table, then a short section each), the recommendation, a diagram of it, and the falsification experiments.
5. Part 4: the training analysis with the arithmetic.
6. Part 5: the sources, grouped by topic.
7. **A 90-day roadmap**: what to build, in which order, with each step's success measure and the point at which to stop or change course.

Write in clear English; use Arabic only for legal terms and examples. Be as long as the substance requires and no longer.
