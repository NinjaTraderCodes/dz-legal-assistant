# Seed: lifting the local fact reader above 36%

**What this is.** The research seed for the brief that will hand this problem to a separate coding agent. It states the measured problem, what exists, which ideas fit it and which do not, and how a change must be measured. It is not the brief: the brief will choose from it, set the scope and unlock the protected areas named in section 7.

**Date:** 6 November 2026. Figures come from `docs/baselines/` and `docs/HIDDEN-SET.md`; outside facts are listed with their sources at the end, and anything not checked by a run here is marked as such.

## 1. The problem, measured

The figure is **32 of 90 hidden cases fully right (36%)**: `gemma3:4b` on the laptop, CPU only, the claim type given, reader Alpha-01 (`docs/baselines/2026-10-30-hidden-given/`). The online model (Nemotron 3 Ultra) reads the same 90 cases with the same reader and gets 64 (71%).

"Fully right" means no critical error: a reversed role, a missing decisive event, a date or amount on the wrong event, a denied or disputed fact read as affirmed, a swapped actor, an invented value, a wrong requested amount (`docs/FACT-READING.md`).

### Where the 58 failed cases fail

| Error | Errors | Cases touched | Cases fully right if this alone were fixed |
|---|---:|---:|---:|
| Polarity (denied or disputed read as affirmed, and the reverse) | 45 (+ most of 15 forbidden readings) | 35 | 47 (52%) |
| Value bound to the wrong event (19 dates, 8 amounts) | 27 | 22 | 39 (43%) |
| Decisive event missed | 16 | 15 | 42 (47%) |
| Wrong requested amount (a monthly sum for a total, a damage for the debt) | 10 | 10 | 37 (41%) |
| Plaintiff position, actor | 3, 3 | 4 | — |

Fixed together:
- **polarity and binding:** 64 of 90 (71%), the online model's score;
- **polarity and missed events:** 59 (66%);
- **polarity and request amounts:** 54 (60%).

Computed from the summary's per-case lines; a forbidden reading is counted as polarity when it is an affirmed denial, as binding when it is a value on the wrong event.

**Polarity is the first target, binding the second.** Retrieval, articles and drafting are not part of this number at all.

### Three facts that shape any solution

1. **The time budget is prompt reading, not writing.** Per case:
   - about 1,175 prompt tokens read at **22 tokens/s** (54 s before the first word);
   - 46 answer tokens written at 5.5 tokens/s (8 s);
   - 63 s in all, on battery.

   Any design that sends more text per case pays at 22 tokens/s. A design that reuses an already-read prefix pays almost nothing for it.
2. **Prompt tuning has stopped generalising.** Runs 1 to 4 of Alpha-01 brought the development cases down to about 4 critical errors in 56 (`docs/ALPHA-01.md`). The hidden set then gave 58 in 90. The rules added to the prompt fixed the cases they were written from. A fifth round of prompt rules is not the way forward.
3. **The online model still makes 20 polarity errors.** A 550B model failing on the same thing points partly at the questions and the answer key:
   - Is a fact the defendant contests "disputed" or "affirmed by the plaintiff"?
   - Is «رغم إعذاره» a notice "stated"?

   Some of the 45 local polarity errors may be ambiguity, not weakness. It must be measured before it is fixed (section 6, step 0).

## 2. What exists today

**The reader (Alpha-01, harness only; the app's analysis still uses `read-facts.ar.txt`):**
- `src/DzLegal.Drafting/CaseFacts.cs`: `CaseText` cuts the subject, facts, requests and documents into numbered sentences (`s1`, `s2`…). `FactValues` finds every date and amount by code.
- `src/DzLegal.Drafting/ClaimFactReader.cs`:
  - lists those values as `v1`, `v2`…;
  - asks one numbered question per element and request of the confirmed claim type (`data/law/claim-elements.json`, 21 types, 1 to 6 elements each);
  - parses one line per question: `n | stated/denied/disputed/absent | v3 v5`.

  The model cannot invent a value. It chooses a status and value numbers. The role follows from the claim type by code, unless the type allows several.
- `data/law/prompts/read-claim-elements.ar.txt`: the instructions, the case, the values, the questions, then a template to fill.

**So the design is already neuro-symbolic.** Code holds the values, the roles and the vocabulary; the model judges. What is still left wholly to the model, in one free-text answer covering every question at once, is the hard part: **polarity and binding**.

**The provider** (`src/DzLegal.ModelProviders.Ollama`):
- loopback only, CPU only (`num_gpu` 0);
- `num_ctx` 4096, temperature 0.2;
- **no `format` (schema), no logprobs, one call per case**.

`docs/CORE-LOGIC-PLAN-REVIEW-2026-10-25.md` §3.2 already found that Ollama's `format` works and that the request type has no field for it.

**The bench** (`src/DzLegal.Harness`):
- `facts --alpha` runs the reader; `--claims given|read`.
- `facts export` / `facts score` score **any outside tool's predictions** against the same answer key and the same critical errors. A Python prototype (an encoder, a new pipeline) can be measured before any C# is written.
- `AlphaFactsTests` proves the questions can express every answer the key needs.

**The rules of measure** (`docs/HIDDEN-SET.md`):
- The 100-case hidden set has been **seen at summary level**. Under rule 7, a changed reader needs a **new hidden set** written by someone who has seen neither the prompts nor the cases.
- The 61 cases in `tests/fact-cases` are development, validation and robustness.

## 3. The five suggestions received, against this problem

| Suggestion | Fits the 36%? | Why |
|---|---|---|
| 1. Neuro-symbolic hybrid (model parses, rules decide) | **Already the design.** | Values, roles, vocabulary and the formal check are code. The next step of the same idea is to move *more* of polarity and binding into code and narrow scored choices (§4, A, C, E, F). |
| 2. Hierarchical GraphRAG over the law | **No.** | The 36% involves no retrieval: the claim type is given and no article is read. It may matter later for choosing articles. |
| 3. Multi-agent pipeline | **Partly, re-cut.** | "Agents" as separate long prompts would each re-read the case at 22 tokens/s. The useful form is **several short questions over one cached prefix** (§4, B). |
| 4. BM25 + dense search + cross-encoder rerank | **Not for retrieval here; yes for the encoder.** | Nothing is retrieved. But the cross-encoder idea, a small encoder judging a (sentence, statement) pair, is exactly a polarity judge (§4, D). |
| 5. Grammar-constrained decoding | **Yes, but small.** | It removes unreadable lines, wrong-kind values and slips (4 unreadable lines, a few cut-offs). It does not make a wrong judgement right: a schema-constrained classification was already tried and stayed at 126/138. It is the plumbing that makes A and B safe. |

## 4. The candidate techniques

Each is stated as a hypothesis to measure, with the errors it aims at, its cost on this laptop and how it fits the code. **None has been run here yet.**

### A. Score the choices instead of generating them (polarity)

Ask each question so that the answer is one token among four, A/B/C/D or 1/2/3/4 (check they are single tokens for the model), and read the **log-probabilities** of the four.

Then:
- **calibrate them:** contextual calibration (Zhao et al. 2021) or batch calibration (Zhou et al., ICLR 2024). This removes the model's standing bias towards "stated", which is what most of the 45 errors look like.
- **keep the margin:** a narrow one is an honest "unsure" (§4, K).

Notes:
- **Logprobs:** Ollama's API has `logprobs` / `top_logprobs` on `/api/chat` and `/api/generate`. The Python client added them in PR #601, and a third party documents them for Ollama 0.20. Confirm on the installed version with `gemma3:4b` before building on it.
- **Cost:** one answer token per question instead of 5 to 8.
- **Risk:** calibration fitted on development cases is a tuning step. Fit it on development, check it on validation, measure on the new hidden set.

### B. Short questions over one cached prefix (all errors; the enabler)

Put everything shared first: the instructions, the sentences, the values. Then ask each element as its own short turn.

- **The cost works out.** The prefix (about 1,000 tokens, 45 s) is read once. Each question adds about 40 to 60 tokens (2 to 3 s at 22 tokens/s) and 1 to 6 answer tokens. Six questions come to about 60 to 65 s, today's budget, with a focused question each time.
- **What it relies on:** Ollama keeps the prompt's KV state for a request that shares the prefix while the model stays loaded. The warm probe of 25 Oct showed reuse on an identical prompt.
- **What must be measured:** reuse when only the suffix changes, through `prompt_eval_count`/`prompt_eval_duration`; with `num_parallel` 1, so one slot holds the cache.
- **The fallback runtime:** `llama-server` (llama.cpp) offers explicit `cache_prompt` and slot save/restore. It would replace Ollama, a larger change to weigh: loopback only, licence MIT.

### C. Point first, judge second (binding, polarity)

For each element, the model first names **the sentence** that speaks of it (`s4`, or none). Pointing is a narrower task than judging, and a sentence id is checkable.

Then:
- **the status** is judged on that sentence alone, a short and clean question;
- **the values** are restricted by code to those in that sentence. The prompt already states the rule ("a date belongs to the event in its own sentence") and the model breaks it 27 times. Code can enforce it.

Cost: one extra short answer per element over the cached prefix (B).

### D. A small encoder as the polarity judge (polarity, missed events)

Polarity is natural-language inference:
- *premise:* the sentence;
- *hypothesis:* the element's statement («لم يدفع المدعى عليه الأجرة»).

The model answers entailment (stated), contradiction (denied) or neutral (absent). "Disputed" is a claim plus a denial attributed to the other party, read as two hypotheses («يدعي المدعي أن…», «ينكر المدعى عليه أن…»).

**Candidates, all CPU-sized (about 280M parameters, tens of milliseconds a pair):**
- `MoritzLaurer/mDeBERTa-v3-base-xnli-multilingual-nli-2mil7`: Arabic in its training data, MIT, ONNX weights published.
- `fastino` GLiNER2.5 multilingual: schema-driven classification and extraction, on mDeBERTa-v3-base, Apache 2.0.

**The path:**
- zero-shot first, through `facts export/score`, with no C# needed;
- if promising, fine-tune it on synthetic labelled pairs (J);
- in the app: `Microsoft.ML.OnnxRuntime` (MIT) and a SentencePiece tokenizer. Check `Microsoft.ML.Tokenizers` against mDeBERTa's.

**Cautions:**
- Negation is a known weakness of NLI models too (Truong et al. 2023; Vrabcová et al. 2025).
- "Disputed" has no NLI label of its own.
- It adds a model file (about 300 MB at fp32, less in int8) and its licence must sit beside it.

### E. A code layer for negation, attribution and dispute cues (polarity)

Arabic marks most of these openly:
- **negation:** لم، لن، لا، ليس، ما، غير، دون، عدم;
- **attribution:** يدعي، يزعم، يقول، يؤكد، يتمسك;
- **contest:** ينكر، ينفي، ينازع، يعترض، يجحد;
- **contrast:** بينما، في حين، إلا أن، رغم، غير أن.

Use them as **features**, not verdicts:
- as a line under each sentence for the model or the encoder: «s5: إنكار منسوب إلى المدعى عليه»;
- as a **veto**: "stated" on a sentence carrying a contest cue by the other party is sent back to be asked again.

**Scope is the trap.** «لم يدفع رغم إعذاره» negates the payment, not the notice, and run 1 tripped on exactly that. Every cue rule is measured on development and validation, not written from a failure. Arabic negation-scope work exists (Mahany et al., IJICIS 2023, a transformer; earlier rule-based systems).

### F. Binding as an assignment, with constraints (binding, request amounts)

Each value gets a short code-made description:
- its sentence and field;
- the two or three words before it («بتاريخ الإعذار», «أجرة شهرية قدرها»);
- unit cues: شهريا، سنويا، مجموع، باقي، قسط، تعويض.

Score each (element, value) pair, by logprobs (A) or by the pointed sentence (C). Then choose by code, under constraints:
- one value per slot;
- a value on at most one event, unless the text repeats it;
- **dates in a plausible order:** a contract before its breach, a notice before the claim;
- **a requested amount** taken first from the requests field, and a monthly sum never given as a total when a total is written.

10 of the 58 failures are request amounts of exactly the monthly-for-total kind.

### G. Constrained output, as plumbing

There are two ways to get it:
- **Ollama:** `format` with a small JSON schema, with enums per field: the status, and value ids limited to those of the right kind.
- **llama-server:** a GBNF grammar for the existing line format, which costs fewer tokens than JSON.

Notes:
- **At 5.5 tokens/s,** JSON keys are paid for, so keep keys to one letter or use the grammar.
- **Known limits:** llama.cpp converts only part of JSON Schema, and large grammars have performance caveats.
- **Its gain is reliability.** It also makes A's single-token answers certain.

### H. Several samples, one vote

With a cached prefix (B), extra samples cost only their answer tokens. Options:
- three samples at a moderate temperature, a vote per question (self-consistency, Wang et al. 2022);
- or two phrasings of each question.

Disagreement is also a confidence signal for K. Measure the gain against the time it adds.

### I. The model itself

The candidates have to fit 16 GB of RAM, run on CPU and meet the time budget:

| Model | Note | Licence |
|---|---|---|
| `gemma3:4b` (today), and its QAT build | QAT keeps more accuracy at 4-bit | Gemma terms |
| **Gemma 4 E4B** (April 2026) | E2B/E4B sized for devices, 140+ languages; MMMLU 76.6% for E4B as reported | **Apache 2.0**, which simplifies a fine-tuned redistribution |
| Qwen3.5 4B | widely recommended on CPU; Arabic unmeasured; Qwen3 4B lost accuracy in Arabic tool use (100% → 82%) | Apache 2.0 |
| Arabic-native 7B (Falcon-H1-Arabic, ALLaM, Fanar) | stronger Arabic, about twice the time per token | check each |
| NuExtract 2.0 | built for extraction; the 2B is MIT, **the 4B is research-only** | — |

**The rules for the comparison:**
- every candidate goes through the same bench, on the same laptop, at the same settings;
- record seconds per case and peak memory, as the harness already does;
- the Ollama tags and version needed (Gemma 4 needs a recent Ollama) are checked on the machine, not taken from these notes.

### J. Fine-tuning on fictitious cases (the largest lever, the largest process)

The earlier review asked for fine-tuning only once the residual error classes were known. **They now are:** polarity and binding.

**The plan:**
- **Data:** a LoRA on the chosen base, trained on synthetic fictitious cases (a), labelled at the element level (b).
  - (a) Written by a generator that has seen neither the hidden set nor the validation cases, in the style rules of `docs/HIDDEN-SET.md`.
  - (b) Labelled by a strong teacher model plus the code checks: values exist, the sentence is cited, the label comes from the vocabulary.
- **Training mix:** contrast pairs that differ only in a negation or a denial; dates of different events in one sentence; monthly next to total.
- **Delivery:** an adapter through the Modelfile `ADAPTER`, non-quantized, built against the exact base. Ollama documents Gemma 1/2 adapters; Gemma 3/4 must be checked. Or a merged GGUF. Gemma 4's Apache 2.0 matters here.
- **Training** runs anywhere on fictitious data; the result runs offline.

**Cautions:**
- It cannot fix an ambiguous answer key (section 1, fact 3).
- Each release of the adapter is a new frozen version to measure.

### K. Let the reader say "unsure" (all errors; product)

A narrow logprob margin (A), a split vote (H) or a cue veto (E) can produce an **"unsure" element**, shown to the lawyer to confirm, instead of a silent guess.

- **The lawyer already confirms** the claim type with one click, so the same pattern fits elements.
- **The scoring would gain one column:** cases fully right *without asking*, and cases fully right *after at most N confirmations*.
- **Low-confidence errors** that are caught are no longer silent errors. That is the safety the product promises, and it should be counted honestly, not hidden in the headline.

### L. Speed, to pay for B, C and H

Settings to measure on the laptop:
- flash attention;
- a `q8_0` KV cache (`OLLAMA_FLASH_ATTENTION`, `OLLAMA_KV_CACHE_TYPE`);
- `num_batch` and threads;
- the 22 tokens/s on battery against the 33 measured on 29 Oct;
- one model kept loaded.

Every second saved in prompt reading buys a question.

### Not for this number

GraphRAG, dense retrieval and rerankers (§3), and the drafting of the grounds: all of them can matter for choosing articles and for writing, but none touches the 36%.

## 5. Constraints any solution keeps

- **Offline at run time:**
  - the model is reached on loopback only;
  - no download while the lawyer works;
  - every bundled model ships with its licence beside it.
- **16 GB of RAM, CPU only, about 60 to 90 s per case.** A slower reader must buy its accuracy with a stated number.
- **The model never invents a value:** values stay code-found and code-checked. Every fact still cites its sentence.
- **Fictitious data only,** for training, testing and samples.
- **Every measured change is reported per error type, with seconds per case and peak memory.** It is never folded into one figure.

## 6. How a change is measured

0. **Audit the answer key's polarity first.** Re-label a sample of the 45 + 20 polarity cases independently, at summary level only, without opening the hidden texts. If the definition of "disputed" is the cause, write one rule for it, in `docs/HIDDEN-SET.md` and in the questions alike.
1. **Write hidden set v2** under rule 7, by an author who has not seen the prompts, the cases or this seed's error list. Keep it out of the repository.
2. **Develop on `tests/fact-cases`** (development), check on its validation partition, and touch hidden v2 once per frozen version.
3. **Climb one rung at a time,** with an ablation for each, each measured on the same laptop:
   - B (plumbing);
   - then A;
   - then C;
   - then E and F;
   - then D or H;
   - then I or J.
4. **Prototype outside the app first** (`facts export` / `facts score`). Wire into the app only once a reader passes, through the decision rule in `docs/FACT-READING.md`.

## 7. What the brief must unlock

`AGENTS.md` protects these areas. The brief has to name what may change:
- `src/DzLegal.Drafting`: `ClaimFactReader`, `CaseFacts`, `FactValues`.
- `src/DzLegal.ModelProviders.Ollama`, and `IModelProvider`'s `ModelRequest`: `format`, logprobs, several turns over one prefix.
- `src/DzLegal.Harness`: new scoring columns ("unsure", time, memory).
- `data/law/prompts/` and `data/law/claim-elements.json`: question wording, statements for the encoder.
- New dependencies: ONNX Runtime, the encoder's files and licences, a second runtime if `llama-server` is chosen.

The lawyer-safety behaviour stays exactly as it is, and nothing reaches the app's analysis until the bench says so.

## Sources

**Code and measurements:**
- In this repository: `docs/HIDDEN-SET.md`, `docs/ALPHA-01.md`, `docs/FACT-READING.md`, `docs/CORE-LOGIC-PLAN-REVIEW-2026-10-25.md`, `docs/baselines/2026-10-30-hidden-given/summary.md`, `docs/baselines/2026-10-31-hidden-nemotron-given/summary.md`.

**Runtimes and decoding:**
- Ollama logprobs:
  - [ollama-python PR #601](https://github.com/ollama/ollama-python/pull/601/files);
  - [a third party on Ollama 0.20's `logprobs`/`top_logprobs`](https://github.com/cryptoAGI/bankml/releases/tag/v0.4.2);
  - [LangChain's Ollama integration](https://docs.langchain.com/oss/python/integrations/chat/ollama).
- Ollama prefix reuse:
  - [Prompt caching (Ollama book)](https://leanpub.com/read/ollama/prompt-caching);
  - [`num_parallel` and per-slot KV memory](https://dev.to/multigrid/ollamas-numparallel-setting-for-concurrent-requests-386l).
- Ollama adapters: [Modelfile `ADAPTER`](https://ollama.readthedocs.io/en/modelfile), [importing adapters](https://ollama.readthedocs.io/en/import/).
- llama.cpp grammars and JSON Schema: [GBNF guide](https://raw.github.com/ggerganov/llama.cpp/master/./grammars/README.md), [grammar size issue](https://github.com/ggml-org/llama.cpp/issues/25895).

**Calibration and voting:**
- Calibration: [Zhao et al. 2021, Calibrate Before Use](https://arxiv.org/pdf/2102.09690), [Zhou et al. 2024, Batch Calibration](https://arxiv.org/pdf/2309.17249).
- Self-consistency: Wang et al. 2022, arXiv 2203.11171.

**Encoders and Arabic NLP:**
- Encoders: [mDeBERTa-v3-base-xnli-multilingual-nli-2mil7](https://huggingface.co/MoritzLaurer/mDeBERTa-v3-base-xnli-multilingual-nli-2mil7), [its ONNX weights](https://huggingface.co/Xenova/mDeBERTa-v3-base-xnli-multilingual-nli-2mil7), [GLiNER2 paper](https://arxiv.org/pdf/2507.18546), [GLiNER2 on PyPI](https://pypi.org/project/gliner2/2.1.0/).
- Negation:
  - [Truong et al. 2023, Language models are not naysayers](https://ar5iv.labs.arxiv.org/html/2306.08189);
  - [Vrabcová et al. 2025, Negation: a pink elephant](https://www.ics.muni.cz/en/research/publications/2514066);
  - [Mahany et al. 2023, Arabic negation and speculation scope](https://ijicis.journals.ekb.eg/article_289204.html).

**Models:**
- Gemma 4: [the-decoder](https://the-decoder.com/googles-gemma-4-is-now-available-with-apache-2-0-licensing-for-the-first-time/), [9to5Google](https://9to5google.com/2026/04/02/google-gemma-4/), [Gemma on Ollama (Google)](https://ai.google.dev/gemma/docs/integrations/ollama?hl=hi).
- Qwen3.5 and Arabic:
  - [Qwen 3.5 with Ollama](https://apidog.com/ar/blog/use-qwen-3-5-with-ollama/);
  - [Arabic tool-calling benchmark review](https://www.themoonlight.io/review/arabic-prompts-with-english-tools-a-benchmark);
  - [Evaluation of SLMs for Arabic](https://arxiv.org/pdf/2606.21460);
  - [Arabic local models overview](https://www.promptquorum.com/local-llms/best-arabic-local-llms-2026).
- NuExtract 2.0: [NuMind](https://numind.ai/blog/outclassing-frontier-llms----nuextract-2-0-takes-the-lead-in-information-extraction).

The sandbox that wrote this seed could search the web but not open most pages (docs.ollama.com, arxiv.org were blocked). Version-specific claims above are leads to confirm on the laptop, not established facts.
