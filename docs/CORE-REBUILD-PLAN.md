# Rebuilding the legal core (plan v3, 25 October 2026)

This plan answers the core-logic audit (`docs/CORE-LOGIC-REVIEW-2026-10-25.md`) and the review of plan v2. It is the reference for both workstreams:
- **the law corpus** (Codex, branch `codex/law-corpus`);
- **the analysis and drafting** (Claude Code, default branch).

## 1. What we are building

The lawyer gets a complete draft of the عريضة افتتاح دعوى in one pass. Its legal basis is the right cause of action and the articles in force for the facts, with prerequisites and deadlines taken into account, and every factual statement true to what the lawyer wrote.

**Where a decisive fact is not in the case,**
- the draft never claims it;
- the draft stays complete;
- a quiet, non-blocking note in the legal-points panel says what is open.

The lawyer is never questioned. The export blockers that exist today stay unchanged.

## 2. Fixed constraints

- **Offline at run time.**
  - `gemma3:4b` through Ollama on loopback; 8 GB laptops; no GPU.
  - No automatic downloads and no remote fallback.
- **Arabic first.**
- **The lawyer confirms the claim type and the articles,** as today. AI-written text is marked and reviewed before export.
- **Speed.** Each stage has a prompt, output and retry budget, measured against today on the same machine and cases.
- **Fictitious data only in tests.**

## 3. No lawyer: how legal facts are established

There is no lawyer or legal reviewer. Legal facts come only from:
1. **Official texts.**
   - The Journal Officiel (Arabic, with the French edition as a structural cross-check).
   - Ministry amendment indexes, for discovery only.
   - Codes are consolidated by us from the original plus each amending text, because no complete, current Arabic consolidation is available.
2. **Published decisions** of the Supreme Court and the Council of State, for test cases and to support rules.
3. **Mechanical checks:**
   - the text layer against an independent OCR of the page image;
   - an inventory of article numbers;
   - an amendment ledger;
   - every rule tracing to a quoted clause.
4. **Independent strong models, during development only.**
   - They must agree, and every agreement must trace to a quoted clause or a decision.
   - An adversarial model searches for counterexamples, and each counterexample becomes a test.

**Verification states** (never "verified by a lawyer"):
- *text checked*;
- *traceable*;
- *models agree*;
- *court-attested*.

**The draft's rule sentence quotes the official clause.** It does not paraphrase it.

**Remaining limit:** a point of practice written in neither the texts nor the decisions can be missed.

## 4. Design

### 4.1 Evidence: facts bound to sentences

- **Sentences.** Every field the lawyer wrote is split by code into numbered sentences, each with its field and revision. Fields: subject, facts, requests, documents, parties, court section.
- **Extraction.** The model extracts each fact as a typed item:
  - the parties and their roles;
  - the relationship;
  - events, each with its kind (contract signed, lease started, payment due, notice served, report delivered…), its date and its amount;
  - the remedies requested;
  - the documents.

  Each item **cites sentence numbers. It does not copy quotes.**
- **Checks.**
  - A date or amount must be found in its cited sentence, and is taken from there.
  - The kind of event must agree with the sentence's words. This is checked where a deterministic check is meaningful.
  - An item that fails is discarded.
- **What each fact records:**
  - **who asserts it** (the plaintiff's account; «ينكر المدعى عليه…» is the defendant's position);
  - **its polarity:** affirmed or denied;
  - **its status:** stated, unknown or conflicting.
- **What these checks do and do not do.** They make negation and date roles explicit and testable. They do not prove that the reading is correct; the evaluation measures that.

### 4.2 Legal knowledge: causes of action as executable rules

- **Facts are separate from legal conditions.**
  - A condition is a rule over facts: `all` / `any` / `not`, typed operands, role constraints.
  - It also lists its exceptions, its priority, and the clause it quotes.
- **Unknown or conflicting stays unknown.** It is propagated, never turned into "no".
  - A cause of action with an unresolved condition is still proposed, and its open condition becomes a note.
- **Each cause of action has:**
  - its conditions;
  - the evidence typically listed;
  - its defences and exceptions;
  - its prerequisites (formal notice, conciliation report, deadlines, limitation);
  - its remedies, each with its own conditions;
  - its temporal rules.
- **Temporal rules name their trigger event:** signing, breach, renewal, report delivery or filing.
  - Transition rules are explicit, e.g. CPCA art. 2 and Commercial 187 bis 1.
  - Deadlines are counted in calendar units, with defined start and end, never as a fixed number of days.
  - When the facts do not decide between two regimes, both branches are kept.
- **Coverage is tracked in a matrix:** matter → relationship → remedy → forum → temporal regime → authorities → rules → test cases → supported status. Unsupported cells are recorded.

### 4.3 Candidates: union, not a single gate

- **The candidate set is the union of:**
  - exact article references in the lawyer's text;
  - the seeds of the reviewed causes of action;
  - BM25 over paragraph units **with their article context** (heading, the whole article when short, adjacent exception paragraphs);
  - later, a semantic retriever if it is qualified.
- **Queries use the raw fields as well as the extracted facts,** so a fact dropped by extraction can still be found.
- **The court section is a hint, not a hard filter.**
- **No fixed top-3 or top-12.** The budget is allocated per issue and per remedy. Dependencies are mandatory; overflow is recorded.
- **Semantic retrieval comes only once it earns its place.**
  - First, compare exact references + seeds + BM25.
  - Then multilingual-E5-small (int8 ONNX, about 136 MB).
  - A heavier model only if it clearly earns its cost.

### 4.4 Decision and drafting

- **The program evaluates the rules** over the extracted facts. The lawyer confirms, and sees the cited sentences for each condition.
- **The draft is written condition by condition:**
  - the rule sentence quotes the clause;
  - **dates, amounts and parties are rendered by code** from the bound facts;
  - the model writes the connecting language for all conditions in **one batched call**, with targeted retries as today.
- **The existing `ArguedGrounds` guards stay.** The role check is described as a measured error detector, not a proof.
- **A paragraph that still fails keeps today's behaviour:** the `(...)` marked gap, which blocks export.

### 4.5 Freshness and compatibility

- **The analysis depends on:**
  - every input field;
  - the corpus and rule versions;
  - the prompt and schema;
  - the model digest and options.
- **When any of them changes,** the analysis is marked stale.
- **Background jobs carry the case revision.** A result for an older revision is discarded.
- **Saved cases** keep the versions they were drafted with. A corpus update never silently reinterprets an old case.

## 5. Evaluation

- **Test cases come from published decisions.**
  - The decision's facts are rewritten as a fictitious petition.
  - The expected articles, cause and prerequisite are what the court applied.
- **Contrast pairs** change one decisive fact.
- **Out-of-scope cases** are included.
- **Each case lists:**
  - its mandatory, optional and excluded authorities;
  - its acceptable alternatives;
  - its unresolved facts.
- **Partitions:** development, validation, and a sealed release test, grouped by scenario family.
- **Scores per stage:**
  - extraction: binding, attribution, negation, conflicts, omissions;
  - candidate recall;
  - applicability, including false "met" on unknown conditions;
  - drafting faithfulness;
  - export state.
- **One run goes from the raw fields to the export state,** with no preloaded citations.
- **Every run is stored separately,** with the code, data, prompt, schema, model and settings identities, including failed runs.
- **The current path is kept as the baseline** until each slice beats it.

## 6. Delivery in slices

Each slice leaves the app working, with the old path kept for comparison.

| Slice | Content | Owner |
|---|---|---|
| A | Baseline of the current path; the new test-case format; the dog/wall/commercial-lease expected answers corrected from the official text; the coverage matrix started | Claude Code |
| B | Evidence (4.1) and freshness (4.5); the court section becomes a hint; Ollama structured output in the provider | Claude Code |
| L | The law corpus: the priority articles first, then the Civil Code, CPCA, Family Code, Commercial Code, labour, special laws | Codex, in parallel |
| C | First executable modules end to end: debt and roles; animal and building liability (138/139/140); the labour deadline (504) | Claude Code, on the L priority articles |
| D | The candidate union with BM25 over contextual paragraphs | Claude Code |
| E | Drafting with code-rendered facts and quoted clauses | Claude Code |
| F | Old/new comparison on the same machine; memory and latency on the target profile | Both |
| G | Expansion of the modules over the full corpus; semantic retrieval trial; fine-tuning only for measured residual errors | Both |
