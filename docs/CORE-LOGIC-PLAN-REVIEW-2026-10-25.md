# Critical review of the legal-core rebuild plan

25 October 2026 · Review of `core-logic-plan.md` · Repository baseline `543a518883d763ad4156320407d211af6f72b7c9`, branch `claude/stoic-fermat-mi1gko`.

**Verdict: keep the architecture direction, but revise the guarantees and delivery sequence before implementation.** The plan correctly moves toward reviewed legal knowledge, explicit facts, conditional applicability, measured retrieval and independent evaluation. It does not yet establish that the proposed 4B pipeline can make every legal decision safely, produce a ready-to-sign petition from incomplete information, cover ordinary practice comprehensively, or retain the asserted latency on an 8 GB laptop.

The most consequential new observation from this review is experimental: a local schema-constrained Gemma extraction returned dates correctly but attached the unrelated source quote **«لم يُذكر»** to both supplied dates. The JSON matched the requested structure and every quote appeared verbatim in the input. The proposed quote-presence check would accept those incorrect evidence links. Structured output and provenance are useful controls; neither establishes that an assertion follows from its source.

This document follows the five review sections requested in the plan. Plan references below refer to its numbered sections; file references identify the current implementation. The plan is assessed as a proposal, not treated as evidence that its promised results have been achieved. No repository file was created, edited, deleted, staged or committed. All live probes used invented facts and the already-installed local model; no weights were downloaded, settings changed persistently, or case text sent to an online service.

## 1. Coverage of the audit findings

The earlier audit is [CORE-LOGIC-REVIEW-2026-10-25.md](C:/Users/TheShadow/dz-legal-assistant/docs/CORE-LOGIC-REVIEW-2026-10-25.md). “Solved” below means an explicit design change removes the particular structural defect **if implemented as specified**. It does not mean implemented, legally approved, or empirically qualified. “Partly” means the right direction is present but an essential mechanism or acceptance condition is missing.

| Audit finding | Proposed response | Assessment | What still matters |
|---|---|---|---|
| §4: absent Civil 139/140 and wrong dog/wall expectations | §§4.1–4.3 import them, correct fixtures and separate liability branches | Partly | Review the whole basis, custodian/owner roles, exceptions and remedies; changing one expected number is insufficient. |
| §4: absent Commercial 187 bis 1 and lease regime facts | §§4.1–4.3 add transition text, dates and branches | Partly | “Version on facts date” needs a reviewed transition rule and distinct contract, renewal, breach and filing events. |
| §4: CPCA 504 and labour prerequisites | §§4.2–4.4 include text, prerequisites and deadline calculation | Partly | Calendar arithmetic, actual delivery versus issue date, procedural effects, applicable exceptions and missing/conflicting dates need explicit rules. |
| §4: Civil 308/309 and 379 missing | §§4.2–4.3 add texts and conditions | Partly | Those articles are starting points; limitation exceptions/suspension/interruption and warranty qualification require linked rules and cases. |
| §4: special regimes outside the partial library | §§4.2–4.3 add special laws | Partly | “Others chosen with lawyers” is not an inventory proving comprehensive coverage. |
| §5: categories are ad hoc and too broad | §4.3 systematic coverage and narrower branches | Partly | Code structure helps inventory; it is not a complete litigation taxonomy. Separate relationship, cause, remedy, forum, stage and temporal regime. |
| §5: chosen court area can exclude the right claim | §4.4 includes the court section in extraction | Not addressed explicitly | Reading the field does not remove the hard candidate restriction in `AnalyseAsync`. Specify how routing ambiguity and mixed matters are handled. |
| §6: tuned developer fixtures, no independent hold-out cases | §4.1 independent lawyers, contrasts and sealed portion | Solved in design | Requires actual expert contributions, adjudication and protection against repeated test-set tuning. None is supplied by the plan itself. |
| §6: raw mistakes hidden by corrections/default additions | §§4.1, 4.7 evaluate steps and compare old/new paths | Partly | Log raw facts/condition answers, corrected values, default injections and removals separately. Existing catalogue metrics cannot become the gold standard. |
| §6: drafting benchmark begins with supplied citations | §4.1 scores extraction through drafting | Partly | Explicitly require an end-to-end run from raw case fields without fixture citations or expected labels being supplied to the pipeline. |
| §6: mixed attempts appended to a summary | §4.1 immutable separate runs and fingerprints | Solved in design | Add case-set/schema/retrieval/runtime identity and failed/incomplete-run records. |
| §6: role stability on repeated rent case does not establish general accuracy | §4.1 independent contrasted cases | Solved in design | Count distinct scenarios, not repeated generations, as independent legal observations. |
| §7 A: animal keyword override ignores negation | §§4.4, 4.7 explicit status and replacement of `Corrected` | Partly | A model may still misread negation, its scope, or who denied the fact. Source quotes do not prove correct interpretation. |
| §7 B: a real date assigned to the wrong event passes | §4.4 typed dates and role-faithfulness checks | Partly | The final semantic checker is asserted, not specified. A wrong extracted event can authorize the same wrong draft. |
| §7 C: Family 53 harm clause outside first 180 characters | §§4.2, 4.4–4.5 whole relevant clauses | Solved for the fixed-prefix defect | Include surrounding qualifications and parent context; retrieval still has to find the clause. |
| §7 D: loaded Family 52 unreachable automatically; no claim means no candidates | §§4.4–4.5 retrieval beyond catalogue-only expansion | Solved for the hard catalogue boundary | Measure required-authority recall, including articles without a cause mapping. |
| §7 E: first 12 articles starve the second claim | §§4.5, 4.7 fair context allocation | Partly | Replace the arbitrary cap with a defined issue/role-aware token budget, mandatory dependencies and an overflow policy. |
| §7 F: requests/documents absent from analysis | §4.4 step 1 reads every field | Solved for the input omission | Conflict resolution, field provenance and input limits still need design. |
| §7 F: input/data/model versions absent from analysis freshness | §4.1 run fingerprints | Not addressed for the app | Harness fingerprints do not invalidate an app analysis, confirmation or in-flight result when its dependencies change. |
| §7 G: incomplete `why.ar` becomes deterministic rule wording | §4.3 reviewed conditions, exceptions and rule sentences | Partly | Rules need executable semantics, branch-specific wording, dependencies and amendment-triggered re-review. |
| Cross-cutting: machine integrity tests presented as legal correctness | §§4.1, 4.3 legal review and per-step scoring | Partly | Specify legal acceptance gates and permissible conclusions; green tests alone remain insufficient. |

**The plan substantially addresses the audit, but “it solves every finding” would be premature.** The most explicit omissions are app freshness/races and court-area exclusion. The largest unresolved capabilities are semantic faithfulness, temporal applicability, treatment of missing information and legally reviewed coverage.

## 2. What is wrong, missing or risky

### 2.1 The product promise is internally inconsistent

Section 1 promises a correct, complete, ready-to-sign document across the full practical range. Sections 2 and 4.6 forbid obtaining missing decisive information and make notes ignorable. Section 4.4 permits a marked gap after a failed retry. All those outcomes cannot be guaranteed together.

Consider two otherwise identical files: one has a legally decisive prerequisite satisfied, the other does not, and neither input states which. No local or online model can recover the omitted fact reliably. Generic wording can avoid an invented contract date; it cannot establish timeliness, a condition precedent or the applicability of one of two incompatible regimes. A common legal rule may exist without being sufficient for the requested remedy.

Keep the no-interrogation workflow if that is the product choice. Revise the promise to: **the app always produces the best supported draft and a separate record of unresolved legal dependencies; readiness to sign depends on the lawyer's review of those dependencies.** Do not assert that a condition or deadline was respected when the inputs do not establish it. This recommendation does not require introducing a new export blocker.

There is also a concrete compatibility decision: current `UpdateExportBlockers` blocks `(...)`, while §4.4 proposes a marked gap and §2 promises a complete one-pass draft with unchanged blockers. Define the failure output and its export behavior explicitly. Preserving the present placeholder behavior is coherent; promising a universally complete export at the same time is not. [Current export logic](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Presentation/ViewModels/MainWindowViewModel.cs:326).

### 2.2 Quoting a source proves presence, not support

The plan says checked quotations solve negation and date meaning “at the source.” That overstates their capability. A quote can be real but irrelevant, incomplete, attributed to the wrong party, or incompatible with the extracted predicate.

In the live lease probe, the source explicitly distinguished commencement from an unknown signing date. The model returned:

```json
{
  "lease_start": "01/03/2024",
  "lease_start_quote": "لم يُذكر",
  "contract_signed": null,
  "contract_signed_quote": "لم يُذكر",
  "notice_date": "05/10/2024",
  "notice_quote": "لم يُذكر"
}
```

All three quotations exist in the input, so the planned presence check passes. Two support links are incorrect. An identical warm repetition and a later cold instrumentation check reproduced this answer with the same seed. These are repetitions of one synthetic case, not three independent accuracy observations. Raw requests/responses and timings accompany this review.

Use field IDs, source revision, exact offsets and complete enough spans to retain the subject, predicate and negation. Derive dates/amounts from the quoted span where possible, rather than generating them independently. Add deterministic binding checks where they are meaningful. Measure omissions and incorrect bindings separately. Permit an explicit unresolved result. These checks improve reliability; do not claim they prove unrestricted semantic entailment.

### 2.3 “The program makes every legal decision” hides a model dependency

If the model answers a condition incorrectly, deterministic code can apply the wrong legal consequence perfectly. Questions such as whether an obligation was seriously breached, whether notice was legally valid, or whether someone had custody are not necessarily narrow literal-reading tasks. Legal interpretation has partly moved into the condition questions.

Define primitive facts separately from legal predicates. Each executable rule needs Boolean composition (`all`, `any`, exclusions), typed operands, role constraints, exceptions, priority and a trace of which factual states were used. Some standards should remain unresolved for lawyer judgment. A reviewed prose question plus an enum is not an executable, complete legal rule.

Do not treat failure of prompted extraction as a problem that fine-tuning will necessarily solve. First determine whether the fault is extraction, ambiguity, an invalid question, missing evidence, an incorrect rule or a task beyond the supported scope.

### 2.4 Alleged, denied and proved facts are different dimensions

The proposed `stated / denied / uncertain` status does not identify **who** asserts or denies a proposition. A plaintiff's allegation can be disputed and still properly appear as that plaintiff's allegation in a petition. Conversely, an unopposed narrative is not judicial proof.

Use at least speaker/source, proposition polarity, whether it is contested, and documentary status. Unknown and conflicting are separate states. A document listed as “contract” establishes that the lawyer listed a contract, not that its unseen contents establish every required term. Keep “asserted basis,” “supporting material listed” and “legally established” separate. Section 4.4's reference to “proved facts” should become “supported input propositions, preserving their attribution and status.”

A rule engine must propagate missing/conflicting information, rather than treating it as false, satisfied, or resolved by the nearest category. A remedy can remain a possible proposal while a decisive condition is unresolved. “Propose only when every condition is met” otherwise risks silently suppressing useful claims in incomplete or disputed files.

### 2.5 Temporal law needs more than an article version and a facts date

Store publication, commencement, amendment/repeal, transition and review dates separately. A case can involve signing, performance, breach, renewal, notice, report delivery, filing and procedural steps under different rules. The applicable version is a result of a reviewed temporal rule, not simply `versionAt(event.date)`.

The original CPCA article 2 itself distinguishes immediate procedural application from periods already running under the earlier law. That is a direct reason the plan's single “relevant facts date” formulation is insufficient. [Official CPCA publication, p. 3](https://www.joradp.dz/FTP/jo-arabe/2008/A2008021.pdf).

For deadlines, specify the trigger event and recipient, calendar units, start/end inclusivity, calendar/holiday dependencies where applicable, suspension/interruption and legal consequence. The legal reviewer must approve the particular rules. Do not implement six months as a generic 180-day addition or equate the report's issue date with delivery.

Article 504's filing period and the Commercial Code's transition provisions are good first contrasting tests. Preserve alternative temporal branches when facts do not distinguish them; do not silently synthesize a shared rule that has not been reviewed as sufficient.

### 2.6 Corpus completeness and action completeness are different projects

Full codes are valuable for research and coverage accounting. They do not establish that every practical action has been modeled: special legislation, implementing rules, interpretive authorities, forum/procedure and available relief are additional dimensions.

Use a coverage matrix: matter → relationship → requested remedy → forum/procedure → temporal regime → required authorities → reviewed rules → independent cases → supported automation status. Record unsupported and unresolved cells. Give “complete” a denominator and a release cut-off date.

Following code structure should help find missing modules. It should not mechanically turn every article in 124–140 into a separate cause of action; some provisions are shared conditions, exceptions or consequences. Prefer composable rules and remedy branches to an explosion of labels.

The current six partial codes/81 articles and 20 unverified claims remain the factual baseline. Importing more text is not permission to mark either transcription or applicability as lawyer-verified. [Data model](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Law/LawModel.cs:17), [claim validation](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Law/LegalKnowledgeLoader.cs).

### 2.7 Reviewing only source disagreements is insufficient

Two documents on official websites can reproduce the same compilation. Two extractions can share an OCR or parsing defect. Both documents can omit the same amendment or represent different dates. Equal normalized text does not prove that every article was imported or that the text is current.

Compare like-for-like temporal versions. Keep raw page images and text, conservative comparison text and a separate search normalization. Preserve paragraph boundaries, article suffixes, numbers, negation, footnotes and operative versus historical text. Normalization differences require explanation; a text hash establishes identity only.

The current `LawTextFold` removes punctuation/spacing/diacritics and even folds `آ` to `ك` for a specific source encoding problem. That should not become a universal Arabic verification transform. A normalized match should be recorded as a match under a named transformation, not promoted automatically to a human verification record. [Implementation](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Law/LawTextFold.cs:10).

Review every discrepancy, all decisive provisions/temporal rules in supported modules, and a sample of matched material. Check expected article inventory and amendment coverage independently of text equality. Maintain separate transcription, currentness, legal-rule and review states. Section 4 contains the source availability findings.

### 2.8 Clause retrieval needs parent context and routes independent of extraction

Clauses can refer to a preceding subject, an exception in the next paragraph, a definition, or another article. Index small units, but retrieve a bounded **rule with its necessary context**, including heading, parent article, qualifiers, exceptions and version. Otherwise clause retrieval recreates the 180-character problem at a different boundary.

Use a union of reviewed cause descriptions/conditions, exact citation matches, catalogue seeds, lexical retrieval and, if qualified, semantic retrieval. Search raw fields as well as extracted propositions. If the first model drops the decisive animal, contract or date fact, queries built only from extracted facts cannot recover that omission.

A blanket expansion from 124 to 125–140 can add mutually irrelevant regimes and consume the context budget. Graph edges need types, direction, conditions, hop/budget limits and version identity. Candidate expansion is not a decision that the added authority applies.

The existing `ArticleIndex` indexes whole non-repealed articles and headings; it does not already provide a clause/version index. Its previous noise problem will not disappear solely because the corpus becomes larger. [BM25 implementation](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Law/ArticleIndex.cs:16), [current candidate stage](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Drafting/GroundsAnalyzer.cs:220).

### 2.9 About three candidates is an unqualified bottleneck

The prior E5 experiment's top-three nearest-description shortlist included the expected claim in only **37/46** cases. With that particular shortlist, even a perfect downstream condition checker could not recover the other nine. This was an older claim-description experiment, not the proposed hybrid clause retriever; it is evidence against assuming a top-three gate, not proof hybrid retrieval will fail. [Previous study](C:/Users/TheShadow/dz-legal-assistant/LOCAL-MODEL-REPORT.md:84).

Allocate candidates by identified issue and remedy, preserve mixed/alternative claims, and measure candidate coverage before choosing the cap. Use conditional expansion and deduplicated shared conditions. Record what was excluded and why. “Recall without adding noise” needs precision and end-to-end applicability measures: some additional candidates may be worthwhile when later checks reject them correctly.

### 2.10 The final semantic checker needs a credible implementation

The plan says every date, amount and party must keep its meaning. Existing checks already include targeted party-role heuristics, as well as token-presence checks; they are limited, not wholly absent. [Current checks](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Drafting/ArguedGrounds.cs:901).

An unrestricted Arabic sentence can introduce causation, notice service, admissions, fault, entitlement or remedy even without changing a single date/name/amount. Checking only those tokens does not establish factual faithfulness. Asking the same model to re-read its own sentence supplies another uncertain judgment, not a proof.

For critical assertions, use code-rendered sentences or constrained assertion structures referring to supported proposition IDs. Let the model improve connective language within those boundaries. If freer prose remains, describe the checker as a measured error detector, retain attribution and disclose unresolved semantic checks. A second independent checker is a candidate experiment, not a guaranteed CPU-feasible solution.

### 2.11 The plan misses freshness, cancellation and persistence dependencies

Current `FactsHash` covers only subject, facts and court section. Adding fields to the prompt does not change that. Include relevant fields, source revisions, case identity, legal-rule/corpus/index versions, prompt/schema, model digest/options and review selections in the appropriate dependency hashes. Hashes for reproducibility and cache invalidation have related but different purposes.

Tag each background job with its case/revision; discard results that no longer match. Re-run or mark stale when dependencies change. The app currently starts analysis on leaving Facts and does not automatically restart every time a later field changes. A new input dependency on requests/documents must fit that workflow without repeated full analyses on each keystroke. [FactsHash](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Drafting/GroundsAnalyzer.cs:176), [staleness](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Presentation/ViewModels/MainWindowViewModel.Grounds.cs:293), [background trigger](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Presentation/ViewModels/MainWindowViewModel.Waiting.cs:65).

Retain canonical article/claim identities and migrate saved cases deliberately. Pin old drafts to their content versions. Preserve manual selections, edits and confirmation meaning; never silently reinterpret an old case after a corpus update. An amendment must invalidate dependent rule wording/reviews and rebuild affected retrieval artifacts.

### 2.12 Gold evaluation needs decision rules and protected data splits

The independent gold set is one of the strongest parts of this plan. Add two-lawyer annotation/adjudication for disputed cases, acceptable alternative answers, mandatory/optional/inapplicable authorities and unresolved facts. A single exact label/list is often an incomplete target.

Use development, validation and sealed release-test partitions grouped by scenario family. A held-out set repeatedly consulted to choose prompts, retrieval models, thresholds and fine-tunes becomes a validation set. Repeating the same case three times measures stability, not three independent legal cases. Strong online judges can assist on invented data, but cannot validate the legal knowledge they helped generate without independent expert checks.

Measure critical false-positive legal conclusions, unsupported assertions, missed prerequisites, false “resolved” states, abstention, lost issues and unnecessary warnings. Run the actual raw-fields → analysis → lawyer-selection simulation → drafting → export-state path; no oracle citations in that run. Use oracle facts/candidates separately for diagnosis. Record failed, canceled, truncated and repaired attempts, with schema, dataset, corpus, index, runtime, model and settings identities.

Specify release gates before measuring. Report uncertainty by distinct scenario and claim family. For illustration, zero failures among 100 independent cases still gives an approximately 3% one-sided 95% binomial upper bound on failure probability; repeated generations do not provide that same sample size. This is a statistical illustration, not a target legal error allowance.

### 2.13 Fine-tuning and offline maintenance are secondary workstreams

LoRA can improve a well-defined extraction task; it cannot supply a missing date, repair an incorrect legal rule or prove corpus completeness. Synthetic cases need label/semantic checks, failure-focused sampling and expert auditing. Keep generated near-duplicates and scenario families out of the sealed test set. Validate the final quantized artifact, tokenizer/template, runtime support and distribution terms, not only the training checkpoint.

A versioned offline legal corpus also needs an owner and release process: amendment discovery, review, dependency invalidation, update package integrity, rollback and a visible “reviewed through” date. This can be delivered through manual offline bundles. Runtime connectivity is not required, but indefinitely current law cannot be guaranteed without maintenance.

Preserve the present desktop loopback/privacy architecture for the new embedding component too. No automatic model acquisition or remote fallback when a local asset is missing. Offline source/data packages and diagnostic logs need the same boundary. [Existing loopback guard](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.ModelProviders.Ollama/LoopbackGuard.cs:11).

## 3. Feasibility on the target laptop

### 3.1 What was measured for this review

The available machine is an **i5-1335U with about 16 GB of physical RAM**, not the target 8 GB configuration. The live probes recorded AC connected and battery charging; earlier speed studies were mostly on battery. These conditions must not be pooled as equivalent trials.

The installed server reported **Ollama 0.40.11**. The already-installed `gemma3:4b` tag had digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` and Q4_K_M weights. Requests set `num_gpu=0`, `num_ctx=4096`, temperature 0, seed 101 and bounded output. Ollama reported zero VRAM use and context 4096. No application code/provider was exercised or altered; these were isolated `/api/chat` capability probes.

| Synthetic task | Wall time | Prompt / generated tokens | Observation |
|---|---:|---:|---|
| Lease date extraction, initially unloaded | 44.35 s | 329 / 91 | Valid requested JSON shape; supplied dates correct; two source bindings wrong. |
| Identical extraction, warm repetition | 18.55 s | 329 / 91 | 324 prompt tokens cached; same incorrect evidence links. |
| Explicitly negated animal condition | 14.15 s | 172 / 34 | Answer `no`; real quote, but not the explicit negation sentence. |
| Contradictory payment accounts | 14.05 s | 182 / 35 | Answer `conflicting`; quote includes only the plaintiff's account. |
| Lease extraction, new cold runner-inclusive capture | 40.38 s | 329 / 91 | Same output; corrected process coverage for the memory observation below. |

All five completed without output truncation. Their response objects were checked for expected fields, value types and enum choices; this was a small schema smoke check, not a conformance suite. There were only three distinct stories, with the lease story repeated. No accuracy percentage is justified.

The observed condition answers are encouraging for narrow text tasks, while their evidence links need stronger support checks. The lease counterexample directly disproves the assumption that valid JSON plus matching quotations makes the fact representation checked for meaning.

Evidence: `environment.json`, `runner-environment.json`, `measurements.json`, `runner-measurements.json`, and the corresponding `*-request.json`, `*-response.json` and `*-samples.json` files. Scripts are `measure.ps1` and `measure-runner.ps1`. These live results are separate from the 14–22 October studies.

### 3.2 Structured output is available, but the application does not expose it yet

Ollama accepts a JSON schema through the request's `format` field; this capability is confirmed by the official documentation and the installed-runtime probes. The present `ModelRequest` has no schema field and `OllamaModelProvider.ChatRequest` serializes no `format`. Provider/request changes and validation/error handling are therefore required. [Ollama documentation](https://docs.ollama.com/capabilities/structured-outputs), [current request](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Drafting/IModelProvider.cs:32), [provider body](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.ModelProviders.Ollama/OllamaModelProvider.cs:206).

Use small bounded schemas, explicit unknown/conflicting states, limited arrays, legal enum IDs and application-side validation. Handle incomplete streamed JSON, timeouts, context exhaustion, unsupported schema constructs and malformed source references. Pin the qualified server/model versions; low temperature is not a semantic guarantee.

Earlier schema-constrained **claim-only classification** produced 138 syntactically valid answers but only 126/138 corrected labels, 42/46 in each repetition. That task is different from condition reading and does not disprove the new design; it does show that format reliability alone has already failed to establish better legal classification. [Recorded study](C:/Users/TheShadow/dz-legal-assistant/LOCAL-MODEL-REPORT.md:49).

### 3.3 Memory: feasible candidates exist; physical 8 GB qualification is still missing

The corrected live capture sampled `llama-server`, `ollama` and `ollama app` every approximately 250 ms. Their **summed peak working set was 3.571 GiB; summed peak private bytes 4.021 GiB**. Minimum system-available memory during that capture was 1.616 GiB. Working set and private bytes are different measures and must not be added. These sums exclude the legal desktop app and other applications; shared pages may be counted in more than one process. They are not whole-product RAM requirements.

An initial sampler included only `ollama*` and missed the separately named runner; its process-peak columns in `measurements.json` are **invalid as inference memory figures**. They are retained for transparency and superseded by `runner-measurements.json`. The initial cold call also observed severe whole-system memory pressure, so its timing is not a clean latency baseline.

For embeddings, BGE-M3 is 567M parameters and the current standard Ollama listing is a **1.2 GB model file**; that is not resident RAM. Its publisher documents 1,024-dimensional embeddings and an 8,192-token maximum. It deserves measurement but is a relatively costly first addition to an 8 GB system. [Ollama model listing](https://ollama.com/library/bge-m3), [publisher specification](https://huggingface.co/BAAI/bge-m3).

An already-completed local experiment used official multilingual-E5-small int8 ONNX assets of about 135.9 MB, with roughly **447 MiB Python-process peak RSS**, model load 0.823 s and median short-query encoding 11.83 ms. These are historical observations on this 16 GB host, not .NET or 8 GB deployment measurements. It is the more economical initial semantic candidate. Its old nearest-description classifier's poor accuracy should not be confused with paragraph retrieval performance. [Previous measurements](C:/Users/TheShadow/dz-legal-assistant/LOCAL-MODEL-REPORT.md:50).

Ship vectors with exact model/tokenizer/pooling/normalization identifiers; changing any of these requires rebuilding them. E5 requires query/passage prefixes and truncates at 512 tokens, so one full-case query can silently lose later issues. Use issue-sized queries and measure truncation. Its documented embedding width is 384. [E5 publisher instructions](https://huggingface.co/intfloat/multilingual-e5-small/raw/main/README.md).

For scale only, 20,000 float32 vectors require about 29.3 MiB at width 384 or 78.1 MiB at width 1,024, before text and index overhead. That is an arithmetic illustration, not this corpus's measured clause count. A dedicated vector database is not inherently required at that size.

Start with the existing BM25 plus reviewed seeds/dependencies. Benchmark E5 ONNX when the gold retrieval task exists; compare BGE only if there is evidence the extra cost buys useful recall. Qualify one request at a time, limited embedding threads and controlled model residence. Ollama may unload models under memory pressure, and parallel contexts increase memory. [Runtime behavior](https://docs.ollama.com/faq).

**No new physical 8 GB certification was performed.** A real target-machine test must include the legal app, model, encoder, corpus/index, normal background use, paging, cold start, sustained use and cancellation. Halving this machine's RAM in a spreadsheet does not provide that qualification.

### 3.4 Latency: preserving 1–3 minutes is a target, not an established baseline

The earlier adjacent battery full-flow baseline was 172.97 s on one company fixture; another earlier cold series was 208.62 s. Both excluded human confirmation/review and neither certified a physical 8 GB laptop. The 52–84 s figures elsewhere are drafting-stage measurements, not comparable complete petitions. [Speed study](C:/Users/TheShadow/dz-legal-assistant/SPEED-REPORT.md:9).

Current extraction probes generated about 5 tokens/s and the shorter condition probes about 6.4–6.6 tokens/s. As a budgeting illustration, **500 additional generated tokens cost roughly 76–100 s of decoding**, before new prompt processing, model load or retries. Listing every event with quotations and dozens of condition answers can easily be longer than the old short reading/selection answers. Equal call counts do not imply equal work.

Specify whether applications for all conditions are generated in one batch. The existing planner batches applications and retries individual failures; one model call for every legal condition could erase its speed advantages. The current 4,096-token window includes input and answer. All fields, retrieved clauses, examples and schemas compete for that same budget. [Current token cap](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.Drafting/ArguedGrounds.cs:254), [configured context](C:/Users/TheShadow/dz-legal-assistant/src/DzLegal.ModelProviders.Ollama/OllamaOptions.cs:17).

A stable prefix helps only when the runtime actually reuses it; the identical warm probe demonstrated reuse, not reuse across different stage schemas, clauses and examples. Retrieval of similar few-shot examples is another component with cost and leakage risks. Treat “task last,” number of examples and language choice as ablations rather than fixed performance facts.

Set per-stage prompt/output/retry budgets, latency percentiles and cold/warm definitions. Benchmark old and new on the same hardware/runtime/settings and case mix, with quality held constant. Preserve background progress, cancellation and input revisions. The full proposed pipeline does not exist yet, so its end-to-end latency and structured-versus-unconstrained overhead remain **unmeasured**.

## 4. Official sources and the second-source strategy

### 4.1 The best available official routes

Use the Arabic Journal Officiel publications as the publication record, and government code compilations/ministry amendment indexes as aids to consolidation and discovery. A compilation must carry its own as-of date. The SGG describes its compilation/consolidation work, which is useful context but not a guarantee that a particular downloadable compilation covers every current amendment. [SGG explanation](https://joradp.dz/HFR/Sgg.htm).

| Material | Official source route | What was established or remains limited |
|---|---|---|
| Civil Code and amendment discovery | [Justice Ministry code/amendment page](https://droit.mjustice.gov.dz/fr/content/code-civil); [original JO 78/1975](https://www.joradp.dz/FTP/jo-arabe/1975/A1975078.pdf) | The ministry index lists the original and amendments through 07-05. That list is a useful starting inventory, not an independently completed 2026 currentness audit. |
| Civil consolidation candidate | [Justice Ministry-hosted FCivil.pdf](https://droit.mjustice.gov.dz/sites/default/files/portail/sources-officielles/codes/FCivil.pdf) | Search-indexed official compilation includes 2007 amendment annotations. Direct fetch timed out during this review; French text cannot serve as a byte-comparable Arabic source B. |
| CPCA | [Official original](https://www.joradp.dz/FTP/jo-arabe/2008/A2008021.pdf), [22-13 amendment in JO 48/2022](https://www.joradp.dz/FTP/jo-arabe/2022/A2022048.pdf), [ministry code index](https://droit.mjustice.gov.dz/fr/content/codes-0) | Both JO files opened. The original plus amendment must be consolidated provision by provision; the 2008 document alone is not a current consolidated edition. |
| Family Code | [Original JO 24/1984](https://www.joradp.dz/FTP/jo-arabe/1984/A1984024.pdf), [05-02 in JO 15/2005](https://www.joradp.dz/FTP/jo-arabe/2005/A2005015.pdf), [ministry code index](https://droit.mjustice.gov.dz/fr/content/codes-0) | Official PDFs opened; the index identifies the amendment. A source being official is separate from a verified complete import/version chain. |
| Commercial Code | [Justice Ministry code/amendment index](https://droit.mjustice.gov.dz/fr/content/codes-0), [Commerce Ministry code page](https://www.commerce.gov.dz/fr/code-du-commerce), [05-02 in JO 11/2005](https://www.joradp.dz/FTP/jo-arabe/2005/A2005011.pdf) | The indexed ministry list includes amendments beyond the repository's 2007 compilation, including 2015 and 2022. Direct ministry page fetches were unreliable. Do not claim the entire old edition is current. |
| Labour 90-11 | [MTESS legal text/consolidation route](https://www.mtess.gov.dz/fr/loi-n-90-11-du-21-avril-1990-relative-aux-relations-de-travail-page-488-rectificatif/), [ministry legislation index](https://www.mtess.gov.dz/en/labour-legislation/), [original JO 17/1990](https://www.joradp.dz/FTP/jo-arabe/1990/A1990017.pdf) | Indexed ministry material exposes amendment and application-text references, including later changes than the repository's 1996 as-of date. The article/version impact still needs review. |
| Labour 90-04 | [Original JO 6/1990](https://www.joradp.dz/FTP/jo-arabe/1990/A1990006.pdf), [MTESS legislation guide](https://www.mtess.gov.dz/fr/legislation-du-travail/) | The guide identifies amended 90-04. Original plus discovered amendments and cross-cutting procedure are needed; the guide is not itself a verified consolidated statute. |
| Interpretation of prerequisites | [Supreme Court published decisions](https://coursupreme.dz/) and the [504 decision cited in the earlier audit](https://coursupreme.dz/decision/%D9%85%D9%84%D9%81-%D8%B1%D9%82%D9%85-1488919-%D9%82%D8%B1%D8%A7%D8%B1-%D8%A8%D8%AA%D8%A7%D8%B1%D9%8A%D8%AE-07-10-2021/) | Separate interpretation records from statutory text. Select reviewed, relevant authorities rather than importing an undifferentiated judgment dump. |

Availability is a material issue: the current [Arabic SGG codes landing page](https://www.joradp.dz/HAR/Codes.htm) and [French counterpart](https://www.joradp.dz/HFR/Codes.htm) reported that they are being updated. Legacy `TRV/FCivil.pdf` and `TRV/FCom.pdf` links returned 404 in this review; several ministry direct opens timed out although indexed results remained available. Those observations do not prove the materials are unobtainable, but they do prevent treating source B as an already-confirmed, stable dependency.

I did **not** verify a complete, up-to-date Arabic consolidated source B for all six legal texts. Before basing the whole import schedule on it, complete a short source-acquisition milestone: exact files/URLs, language, edition/date, publisher/lineage, amendment coverage, download integrity and reuse terms. If unavailable, reconstruct the consolidation from the JO ledger and review it; do not silently substitute an old compilation or an unreviewed private website.

### 4.2 Is the two-source comparison sound?

**Yes as quality control; no as the sole basis for verification or completeness.** Its value depends on known provenance, equal temporal scope and sufficiently independent preparation. A government ministry mirroring an SGG PDF is a second host, not necessarily an independent transcription. A French translation is useful for legal cross-checks but does not directly validate exact Arabic text.

Require a reconciliation record for each provision: source lineage, version, raw text/image, extracted text, transformation rules, comparison result, reviewed discrepancy, reviewer and dependent rules. Separately reconcile the expected article inventory and amendment ledger. An exact match cannot find an amendment absent from both inputs.

Review all disagreements, all changes affecting automated supported rules, decisive articles/temporal exceptions, and a sample of equal texts. Verification can then state precisely what was checked. The corpus maintenance process must be able to mark a formerly reviewed rule pending review after its authority changes. “A person reviews only disagreements” should be removed from the plan.

## 5. Proposed changes, in order of importance

### 5.1 Amend the commitments before building

1. **Change the universal ready-to-sign promise.** Keep uninterrupted drafting, preserve the existing export policy, and represent unresolved decisive facts honestly. Define the marked-gap output and do not claim deadlines/conditions are met when unknown.
2. **Replace quote-presence assurance with an evidence-binding specification.** Add source spans/field revisions, attribution, polarity, conflict, typed events and validation limits. Change “this solves negation/dates” to “this makes them explicit and testable.”
3. **Specify the rule engine's semantics.** Primitive factual propositions, legal predicates, exceptions, precedence, temporal triggers and unknown propagation must be explicit. Treat uncertain model answers as uncertain inputs, never as proof merely because the code is deterministic.
4. **Add full analysis dependencies and job revision handling.** Include relevant fields/content/model/schema versions, invalidation after changes, stale-result rejection and saved-case migration. Explicitly resolve court-section hard filtering and mixed matters.
5. **Define scope, legal review ownership and source acquisition.** A coverage matrix and versioned review deliverables are prerequisites for expanding supported automation. Keep text acquisition independent of claims that the whole practice domain is covered.
6. **Add measurable release criteria and protected evaluation partitions.** Correct old expectations with expert review, separate candidate retrieval from applicability and evaluate the full raw-input path. “At least as good” needs bounded legal-error, missing-information and performance criteria.
7. **Make semantic retrieval optional until it earns its cost.** First compare exact references + reviewed seeds + BM25/context expansion. Then evaluate E5, and only then heavier alternatives if necessary. Do not impose top three or a blanket 124→140 expansion without recall evidence.
8. **Design faithful drafting around supported assertions.** Batch short applications, code-render critical date/party assertions where possible, retain targeted retries and define remaining semantic limits. Avoid adding a large unqualified verifier to meet a guarantee that it cannot prove.
9. **Add offline legal-content lifecycle and compatibility.** Review expiry/dependencies, index rebuilds, integrity, rollback, visible cut-off dates and preserved old-case provenance belong in this plan.
10. **Keep fine-tuning conditional.** Identify residual error classes first. A fine-tune is a separate measured candidate after rules, input semantics and evaluation are stable.

### 5.2 Deliver vertical slices before a wholesale rebuild

The proposed order makes full Civil Code import a dependency ahead of the first extraction/pipeline improvements, then postpones labour/special laws until after the new architecture. That delays learning and can leave current social functionality underqualified during migration. Maintain the current path while proving a small end-to-end slice.

| Milestone | Concrete deliverable | Exit evidence |
|---|---|---|
| A. Baseline and review contract | Frozen old-path manifests, new annotation/evaluation schema, corrected dog/wall/lease keys, scope and source inventory | Reviewable legal labels, raw versus corrected traces, end-to-end baseline, explicit missing-information policy |
| B. Evidence and freshness | All-field snapshot, bound source spans, attribution/negation/conflict/date events, revision invalidation | Minimal pairs for the lease-date and negated-animal failures; no cross-case/stale result accepted; measured extraction omissions and wrong bindings |
| C. Small reviewed legal modules | A few high-value contrasting modules: ordinary debt roles, animal/building liability, and a labour prerequisite/deadline path; include supporting texts and versions | Reviewed rule dependencies, exceptions/remedies, explicit unresolved outcomes and current-case compatibility |
| D. Deterministic application with lexical retrieval | Exact references, reviewed seeds, BM25 over contextual units, typed dependencies; controlled candidates beyond the old catalogue | Candidate recall and false-positive applicability by issue; context-budget overflow behavior; retained manual article selection |
| E. Bound drafting | Supported-assertion plan, reviewed rule wording, batched applications, targeted repair | Blind factual/legal reading and the actual export-state evaluation; all failure outputs accounted for |
| F. Resource and release qualification | Paired old/new measurements, physical 8 GB whole-app test, tested offline assets and update migration | Agreed latency/memory/quality gates on supported modules; no unsupported performance claim |
| G. Expansion and experiments | Expand code corpus and reviewed matter matrix; trial semantic retrieval and then optional fine-tuning | Gains on development/validation followed by a sealed release test, with resource costs and regression results |

The corpus acquisition/import work can advance alongside these milestones without requiring the first release to solve every code. This is independent work scheduling, not a recommendation to bypass review. Each slice should preserve the functioning app and the old comparison path until its replacement is qualified.

### 5.3 Minimum acceptance contract to add to the plan

Before implementation, agree on measurable thresholds and who can approve them. At minimum:

- **Integrity:** no unknown article/version IDs, dangling dependencies, wrong source offsets or silent context truncation in accepted results; rejected and unresolved outputs counted.
- **Evidence:** distinct measures for event/party binding, attribution, negation, contradiction and omitted decisive facts; critical relations checked in final text, not only in extraction.
- **Legal applicability:** expert-reviewed mandatory/optional/excluded authorities, temporal branches, exceptions and remedies; false assertions that an unknown prerequisite is met reported separately.
- **Coverage:** results per supported module and nearby unsupported matters; retrieval evaluated before the selector; no fixed shortlist accepted without required-issue recall evidence.
- **Drafting:** complete end-to-end test without preloaded gold citations; gaps, repairs, fallback text, misleading completeness and export behavior included in scoring.
- **Resources:** the same case distribution and runtime for old/new comparison, cold and warm timings, retry-inclusive percentiles, whole-app peak memory/paging and sustained physical 8 GB operation.
- **Regression/maintenance:** saved cases, manual selections, confirmation/review, cancellation, law updates and offline loading remain correct. Existing green engineering tests are necessary alongside these legal evaluations.

This is a practical path toward a stronger legal assistant. The evidence supports building it incrementally. It does not support a claim that reviewed law data plus a deterministic wrapper around 4B answers makes legal reasoning universally reliable.

---

### Review boundaries and artifact record

I read the complete 342-line plan, the earlier audit, the relevant current code/data interfaces and the local model/speed evidence. I checked official runtime/model documentation and targeted official legal-source routes. I did not certify all Algerian amendments, perform legal validation in place of practising lawyers, build the proposed pipeline, fine-tune, download embeddings, or run a physical 8 GB test.

No new unit-test run was needed for this analysis-only work; running the repository build/tests would create build artifacts and would not resolve the plan's legal assumptions. Previously reported test passes remain historical evidence. Live tests were limited to the five isolated fictitious API probes above; they are reproducible smoke evidence, not a product accuracy benchmark.

`input-hashes.json` identifies the reviewed plan/audit, `commit.txt` identifies the code baseline, `prior-embedding-summary.json` records the independently recounted prior shortlist result, and `repository-check.json` records unchanged Git status/tracked diff. Raw measurement files and scripts are retained beside this report. Temporary model residence was removed after the probes; the local server again reported no loaded models.
