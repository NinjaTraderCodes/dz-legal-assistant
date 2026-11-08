# Alpha-03 — Report 1: reading assertions faithfully

6 November 2026. **Stop 1 only; no reader implementation.** Branch `codex/alpha-03`, created from the fetched default-branch commit `c86048dd8bab2b889c5164530624b8354f51afa1`.

## In plain words

The reader usually recognises the subject of a case. It does not reliably preserve what the notes say about each fact: whether it happened, whether a party contests it, and which date or sum belongs to it. Valid output and real numbers are insufficient. A real date attached to the wrong act is still wrong.

The most revealing failure is in our experiment design. The original development and robustness partitions contain **60 labelled events, all affirmed**. They could reward a reader that never learnt denial or dispute. The owner's clarification confirms that validation contains just one denied and one disputed event. Polarity then affects 35 of the 58 failed unseen cases. Development was missing the main task, not merely some difficult wording.

I propose a bounded experiment in **scored, element-by-element decisions with the whole case available**, followed by a separately measured binding step. Calibration is an optional component, not the explanation assumed in advance. We need new fictitious development contrasts, a frozen audit split by scenario, and stop rules before fitting anything. Keeping Alpha-01's binding is a useful ablation, but an inadequate final plan: even perfect polarity alone would have brought the old unseen score only to 47/90, barely above the 50% pass mark.

Two local history folders materially change the seed's account. Alpha-02 already tried clause alignment, deterministic polarity, focused follow-ups, English instructions and constrained JSON. GLiNER 2.5 already ran on all 61 original cases, with poor extraction and no bound values. I would not repeat either pipeline under a new name. The remaining new experiment is the use of complete choice scores, checked prefix reuse and restrained calibration, without requiring a fallible clause aligner to find a denial first.

I keep the pass mark and the current answer key. A listed document affirms its named event as an assertion in the notes; it does not establish judicial proof. A party's denial of a fact relied upon in the claim makes it disputed, even when the plaintiff's assertion is implicit. Item 7 states the exact annotation rule for the owner and hidden-v2 author.

## 1. Product and the lawyer's decisions

The desktop product prepares an Arabic statement of claim offline. Code supplies the form, legal material and checks; the local model assists with analysis and wording; the lawyer confirms proposed choices, reviews the draft and signs. The form keeps the lawyer's facts and requests as supplied rather than trusting a model to rewrite the requested relief. The public demo shares presentation logic but is not the runtime target of this work.

Today the app's analysis proposes claim types and articles. The lawyer confirms them. **Alpha-01 fact reading is only a harness experiment**, and Alpha-03 must remain there. A correct fact sheet is an input to future legal reasoning, not a legal decision, proof of an allegation, or permission to export. Nothing in this proposal changes the app, analysis/drafting path, formal checks, export blockers, authorship marks, recovery, unsaved-work confirmation or review-before-export.

The target output is a plaintiff position, events with actor/status/source and code-found values, and requests. With `--claims given`, claim recognition and out-of-catalogue rejection are deliberately outside the scored reading task. The original 44 development cases therefore become 40 in-catalogue cases. The old validation denominator similarly becomes 14 of 15. We must not describe a `given` result as automatic whole-chain accuracy.

If we later expose unresolved items to a lawyer, the useful question is about one identified proposition and its sources, not “is this entire case right?”. Here that remains a harness measurement, with no UI changes. The primary pass mark remains accuracy **without** confirmations.

## 2. Diagnosis, evidence and limits of the evidence

### Evidence examined

I read the repository rules, Alpha-03 brief, complete seed including §2b and §4, the requested fact-reading/Alpha-01/hidden-set/Alpha-02 history and review sections, the two shared hidden summaries, all four Alpha-01 given-run summaries, and the requested reader/provider/harness/prompt/catalogue code. I inspected only development and robustness case content from `tests/fact-cases`; historical validation results in the required reports are already disclosed history, not a new tuning set.

The owner then authorised two exact external history folders. Both exist directly under `C:\Users\TheShadow`: `dz-alpha02-probes` and `dz-gliner25-benchmark-20261028`. I read selected notes, scripts, synthetic probe outputs and aggregate artifacts read-only. I did not run their Python scripts, copy their files, modify them, inspect their caches or virtual environments, or open any other folder in `dz-laptop-shots`. The Alpha-02 notes explicitly describe its upper-bound set as 16 modified development cases plus four agent-written composites; these are seen development experiments, not the independent hidden set. No hidden-set material was encountered in the inspected history. No hidden paths were opened, listed or copied.

The full Alpha-02 Reports 1 and 2 remain unavailable. Its `INTERIM-FINDINGS.md` records that Report 1 was accepted, Report 2 was not yet written, and further probes and A1–A4 followed. I use those interim notes as history, not as a substitute for a recovered final report. Checking a separate Claude chat was unnecessary for this account.

### Each error class

| Error class | Development evidence | Diagnosis and what is not established |
|---|---|---|
| Polarity | `wages` and `wages-overtime` were read as denied in Alpha-01 run 2 although their unpaid-wage propositions are affirmed. `rent-basic` says «لم يدفع ... رغم التنبيه»: the non-payment and notice are both asserted, yet notice is missed in runs 1, 3 and 4. Alpha-02's later `dismissal` probe aligned the dismissal correctly but its code made it denied because a procedural qualification contained a negation. | The model confuses negation in the wording with denial of the proposition. A code cue can make the same scope mistake. Dispute also requires resolving the target of a party's denial. These are observed failures of the designs tested, not proof that a 4B model can never do the task. |
| Date/amount binding | `non-delivery-machines` contains purchase/payment on 02/04/2025 and a delivery deadline on 02/05/2025. The key forbids the latter on `contract_concluded`. `sale-price` distinguishes a 2,000,000 price from a 1,500,000 payment. Alpha-02's synthetic noisy non-delivery and monthly/total probes exercised competing values; its final composite output still put a monthly amount on arrears. | Choosing an existing value proves existence, not its relationship to the event. A sentence can contain several acts; an event's details can span sentences. Historical Alpha-01 development summaries do not show the 27 binding errors seen in the old hidden summary. They cannot establish those unseen cases' individual causes. The developmental inputs, parser and local probes establish the design vulnerability. |
| Decisive event missed | `rent-basic`: notice missing in run 4. `wages`: attached conciliation report missed in run 4. `divorce-husband`: marriage missed in runs 2–3. Alpha-02's final pipeline missed non-delivery in `non-delivery-machines` and unpaid invoices in `commercial-invoices`, despite focused follow-ups identifying them; a lexical agreement gate discarded the answers. | Omission combines recognition failures, misunderstood compound sentences, format handling and over-strict recovery gates. “Absent” must be an explicit decision; an unreadable or missing answer is not evidence of absence. Sentence alignment can create omissions before polarity is ever judged. |
| Requested amount | `khul` is the sole original in-catalogue development request with a labelled amount: 300,000. Runs 1–2 missed that amount because of answer/template/parser failures. There is no original development `notes.requests` field. Alpha-02's monthly/total probe and rent composite supplied the missing contrast; its final pipeline could read the payment request's total while still misbinding event arrears. | A request amount is a separate semantic slot from a contract price, monthly rate, payment or damages. Treating all numbers as interchangeable candidates is a design weakness. The old development evidence supports a format failure, not a claim that the unseen ten errors all have one cause. The seed's “all monthly-for-total” description is too narrow: the shared hidden summary also reports damages/request cross-binding. |
| Position/actor reversed | `contract-damages` chose seller in run 1 and lessee in runs 3–4, among eight possible roles. `annulment-fraud-land` reversed buyer/seller in run 2 despite explicit wording identifying the seller as defendant. Alpha-01's worked-example role was copied in run 1. | There is model role confusion and example/default bias. `Parse` starts with the first role if no valid answer arrives. An incorrect selected role then deterministically swaps role-dependent actors in commercial leases. Other actors are fixed by catalogue entries, so the high actor score partly measures catalogue assumptions, not independent reading. |

The repeated disputed-loan failure is already disclosed in the required historical reports. It remains a useful warning, but I have not reopened its validation input or used it to design a new phrase-specific correction.

### What the design itself contributes

`ClaimFactReader.Parse` supplies important safety against invented values and unknown codes, but also makes decisions the model did not explicitly make:

- A value ID without a readable status becomes `affirmed`, including an invalid value ID which is subsequently discarded. This is a **code-created route toward affirmation**. Four unreadable hidden lines do not explain 45 polarity errors; raw hidden answers are unavailable, so its contribution there is unknown.
- A missing role answer silently retains the first role. Requests survive only when affirmed. Unanswered elements disappear. These defaults merge uncertainty with substantive decisions.
- Evidence sentence IDs are inferred from the selected date/amount, not chosen independently for the event. Events with no values can have no sentence citation. A chosen value is marked `InCitedSentence` by construction. That check cannot detect whether the sentence supports the event.
- Date and amount are independently taken as the first matching kind. They may come from different sentences. There is no enforced event/value relationship, field-specific request binding or attribution check.
- `CaseText` splits on lines and sentence punctuation, not proposition boundaries. Splitting harder by conjunction is not a reliable cure: Alpha-02 already lost second facts and negation scope that way.
- `FactValues` is a numeric candidate detector. Numbers in references and identifiers can look like money or dates. Alpha-02 identified this too. Candidates must retain offsets, field and surrounding words; being a parseable number does not make one a legally meaningful amount.

The catalogue also asks compound questions: delivery includes services, some breach labels combine alternatives, and certain actor assignments presume a direction of performance even for claims allowing both sides as plaintiff. I would preserve those Alpha-01 contracts and record them as limitations while testing new evidence choices. I would not silently repair the catalogue and claim the result came from calibration.

### What measurement misses

The development key is sparse. Of the 40 in-catalogue cases it labels 53 events, eight event dates, ten event amounts and one request amount; none supplies `notes.requests` or `documents.list`. Adding the out-of-catalogue development events and two robustness cases gives the owner's 60 all-affirmed events. This is not a polarity stress test or realistic binding distribution.

`FactsEvaluator.Score` compares listed gold events and forbidden readings. Extra events outside those lists generally escape critical scoring. Missing event dates/amounts and missing requests are non-critical; even a request with a required amount incurs no request-amount error if the entire request is omitted. Unknown actors are not counted as swaps. Multiple predictions of a gold kind allow a best-match choice. The oracle-answer unit test proves expressibility under these rules, not complete semantic correctness.

These are reasons to report coverage and omissions alongside the headline, not reasons to relax it. A dense supplemental key should annotate every asked element and request, including absent items, with independently reviewed evidence. A model should not earn a better result merely by dropping values or requests. I propose a supplementary completeness score, not a retrospective change to the fixed critical-error score.

The seed suggests auditing hidden polarities from the shared summaries. A summary without its source text cannot establish who denied which proposition. I will not relabel hidden cases from error codes, infer that the online model was right, or seek their private texts. The owner's exact rule in item 7 resolves the intended semantics prospectively.

## 3. Why development did not hold, and the recovered history

### The failure to generalise

The original development/robustness labels never require a denied or disputed answer. The supplied validation evidence adds only two non-affirmed events and was itself used while tuning Alpha-01. Repeated fixes therefore rewarded known examples while leaving the largest unseen failure mode nearly unmeasured. Many development scenarios have only one or two variants and short, clean lines; realistic notes add competing numbers, attribution and qualifications.

The development progression was not monotonic evidence of better comprehension. Runs 1–4 had 5, 6, 4 and 3 failed development cases out of 40. Parser changes recovered some answers, and particular failures moved around. Run 4 still has three unreadable lines in its summary, despite the narrative's statement that there were no format slips. Across all 56 in-catalogue/robustness cases it had six failed cases, not four. I use the tables and individual observations rather than the seed's approximate “4 in 56”.

Whole-case scoring compounds small item errors. The old hidden summary has 58 failed cases, of which the Alpha-01 report says 25 have just one critical error. Several moderate item accuracies can coexist with only 36% of complete cases right. This does not require claiming that every per-item capability collapsed; it requires testing more items, more difficult items, and their dependencies.

Speed did not transfer either. Run 4 reports 735 prompt tokens and 44.9 tok/s; the old hidden run reports 1,175 and 22 tok/s. The later Alpha-02 probes warn that the harness's ratio can include cache effects. Case length, prefix reuse, power and machine state must be separated. A1–A4 added machine-state reporting; they did not establish that a new multi-call reader meets its budget.

### What has actually been tried

| Attempt | Evidence and result | Consequence for Alpha-03 |
|---|---|---|
| Universal fact sheet, then claim-conditioned Alpha-01 | Repository: universal reader 10/61 fully right; Alpha-01 run 4 37/40 development, 11/14 validation; old hidden 32/90. Whole-chain run: 38/44 development, 9/15 validation, 2/2 robustness; 55/61 claim types right. | Keep confirmed claim types and closed values. Do not revive a whole-catalogue JSON sheet. |
| Schema/quotes, prompt negation rules, tolerant parsing | Seed §2b and 25 Oct review: correct structure did not establish correct meaning; real quotations were attached to unrelated values. | Schema is plumbing; source presence is not semantic validation. Do not add another list of prompt patches. |
| Alpha-02 sentence/clause pipeline | External `INTERIM-FINDINGS.md`, `exp/` scripts, final composite and development outputs: clause alignment, copied values, code polarity, focused follow-ups and lexical agreement were built as probes. Four initial composites: strict 1/4 for both Alpha-01 and pipeline, with different errors. Pipeline timings 51–117 s; final outputs still lose events and mis-scope negation. | This is already tried, contrary to the seed's repository-only account. Do not repeat keyword alignment plus negation regexes as the proposed new solution. |
| Alpha-02 focused and format variants | Interim notes: focused alignment 7/10, hand-tuned lexical matcher 8/10; single-question prompts reported worse. English variant 2/4 composites; constrained JSON removed format defects but used 441–588 answer tokens and 135–178 s, without better alignment. | Focus alone is not a new contribution. A further trial must isolate a genuinely different scored decision, not repeat prose generation or add JSON verbosity. |
| Alpha-02 prefix reuse | Interim notes: identical prompts reused work, but changing prefixes/suffixes gave smaller savings; final warm-up saved 4 s of 48 s. | The seed's near-free prefix assumption is not an established budget. Measure the exact intended conversation and charge every uncached token. |
| Alpha-02 upper-bound/model/gate runs | Local `ub-local-given/summary.md`: Alpha-01 15/20, 30 s, on modified development plus composites. Interim notes report hosted 26B and Nemotron 18/20 given; section mismatch caused read-mode failures, and removing the filter helped hosted models but harmed some matching local cases. | These are seen synthetic probes and partly different tasks, not new hidden accuracy. Do not change claim routing for a given-mode fact-reading brief. |
| A1–A4 and Nemotron | Repository: hosted court section becomes a hint; reasoning disabled for Nemotron, made default online; machine state recorded. Shared given summary: Nemotron 64/90, still 20 polarity errors. | Model capacity matters, but does not identify the cause of the remaining errors. Hosted experiments do not authorise runtime network access here. |
| GLiNER 2.5 universal extraction, 28 Oct | External model `fastino/gliner2.5-multi-v1`, revision `cf5593a5d45e3bbf204b9df621b13b1c0cf25ee3`; CPU float32, four threads. 61 predictions, no failed calls, mean 13.04 s, peak process working set 2,970 MiB. Three extraction tasks: relation/position classification, event spans with actor/polarity, requests; relation edges for values. Output audit: 62 events, 47 requests, **zero bound dates, event amounts or request amounts**. | An outside extraction reader has already been measured. It is not a promising drop-in replacement on this evidence. File size and valid spans do not establish reading quality. |
| GLiNER 2.5 schema alternatives | Synthetic record probe: event kinds missing, wrong relation/request, about 36.7 s. Typed-span probe: no events or requests, about 60.5 s. | “Try a schema with records/typed spans” is also not new. A claim-conditioned or trained encoder would be a different experiment, with no gain assumed. |
| E5 and other local models | Repository seed: E5 similarity classifier weak; Qwen2.5 3B weaker, Qwen3 4B slow. Installed tags currently include Gemma 4 E4B, but neither its presence nor the Alpha-02 plan to test it is an accuracy result. | NLI pair scoring remains distinct from similarity/extraction, but adds model, licensing and runtime work. Defer it rather than restart the failed GLiNER pipeline. |

I also **re-scored existing GLiNER predictions**, without rerunning inference or changing any external artifact, against the current scorer on development and robustness only:

```powershell
dotnet run -c Release --no-build --no-restore --project src/DzLegal.Harness -- facts score --partition development,robustness --predictions C:\Users\TheShadow\dz-gliner25-benchmark-20261028\predictions.json --reader gliner25-historical-rescore
```

Result: 41/44 development and 2/2 robustness cases have a critical error; critical counts are 46 missing decisive events, 28 wrong positions, seven polarities and four actor swaps. Of the three development successes, one is out of catalogue: **2/40 in-catalogue cases are fully right**. This uses a universal reader with no supplied claim type, so it is not a fair estimate of a hypothetical claim-conditioned GLiNER. It is direct evidence against repeating this particular extraction setup. The reported 13 s is stored historical inference time, not the time spent scoring today. No validation predictions were scored.

## 4. Proposed approach and alternatives

### First isolate the decision, then improve binding

The first candidate keeps `gemma3:4b`, the confirmed claim, Alpha-01's existing value candidates and baseline value/role/request output. It replaces event statuses through **independent, scored choices about each element with the whole case available**. Do not condition later answers on earlier guessed statuses. Share an identical case/instruction prefix where the installed server actually reuses it. Keep full attribution context: deciding on a single pointed sentence can hide the denial in the next one.

Start with the owner's four outcomes, represented by four checked single-token codes. Compare uncalibrated scores with a calibrated version. No status is inferred from a value ID. A missing/invalid answer or unavailable class score is a recorded unresolved decision, never `affirmed` or `absent` by default. The new reader is separate; Alpha-01 retains exactly its old parser and prompt.

This partly overlaps Alpha-02's failed short-question trials, and its novelty must be stated narrowly: **complete numeric choice scores, controlled token/label bias, explicit missing decisions and measured cache behaviour**. It is not novel merely because questions are short. If the scores do not separate correct and incorrect statuses across scenario families, calibration has no signal to recover and this path stops.

If this earns a gain, add a separate evidence/binding component. Ask an element to identify supporting and contesting sentence IDs; do not force one sentence or one keyword per clause. Code checks IDs and restricts value candidates to explicitly selected source spans, preserving fields and offsets. The model still chooses among candidate values when several occur together. Same-sentence membership is a filter, not proof of the right attachment. Cross-sentence values require an explicit additional evidence link rather than silently selecting a nearby number.

Requests receive their own remedy-to-amount choice. Prefer an explicit matching request in `notes.requests`; fall back to a request expressed in facts when that field is absent, as it is in the old development cases. Never infer a request amount from a monthly rate, perform unwritten arithmetic, or substitute the debt for damages. If the text does not resolve a choice, keep it unresolved and count that honestly.

I reject two seed constraints as universal rules. A single written purchase/payment date can support two acts (`non-delivery-machines`); forbidding value reuse would introduce an error. Chronological plausibility is not a licence to reorder the notes or assign dates: it may flag an inconsistency, but cannot select the answer. Scope/attribution cues may identify candidates or trigger a check, never set polarity for an entire sentence. Alpha-02 already demonstrated why that shortcut fails.

### Calibration and capability checks

The [official Ollama chat API](https://docs.ollama.com/api/chat) documents output log probabilities, top alternatives and cache counters. That is API documentation, not proof that this installed model/runtime returns all four required scores. Read-only metadata checks found Ollama **0.40.15**, .NET SDK **10.0.403**, and installed `gemma3:4b` digest prefix `c30276f7dffe`. No scored inference or cache experiment has been run for Alpha-03 at Stop 1.

Stop 2 must establish that the actual choice codes are single tokens at the decision position, all four finite scores are available, whitespace/JSON framing does not move that position, and any constrained decoding does not destroy the margins being calibrated. “Top four tokens” need not contain the four labels. Missing labels cannot be given fabricated zero probabilities. An empty case legitimately implies absent, so content-free calibration may subtract task meaning rather than mere label bias.

[Contextual calibration](https://arxiv.org/abs/2102.09690) and [batch calibration](https://arxiv.org/abs/2309.17249) motivate an experiment; their published results are not evidence for Arabic legal assertions. I prefer one small global correction fitted on the new development fitting split, with its strength selected by grouped cross-validation. Compare no correction with a predeclared small strength grid. Avoid claim-specific or phrase-specific thresholds. Freeze parameters before internal audit/validation; never recalibrate from hidden-v2 cases, even without their labels. Report per-class confusion and false affirmations alongside whole-case results. A score margin is not a certified probability of correctness.

### Alternatives weighed

| Alternative | Decision |
|---|---|
| Owner's polarity scores/calibration, unchanged binding | First ablation. Smallest new mechanism, but a narrow accuracy ceiling if binding remains wrong. |
| Alpha-02 clause alignment + code polarity | Do not repeat. Its outputs already show scope errors, missing second facts and budget overruns. |
| Schema-only, quotes, English wording or more prompt rules | Do not repeat as accuracy fixes. Retain small output constraints only where needed to make a score usable. |
| GLiNER universal extraction | Reject this configuration on the recovered results. A different encoder task must earn its own pilot. |
| Dedicated NLI judge | Reserve option: pair a whole local context with a proposition and separately test attribution. NLI contradiction is not automatically legal “denied”. No immediate dependency or model download. |
| Voting | Defer. Correlated wrong answers can win a vote; extra questions and decoding must fit the measured budget. |
| Gemma 4 E4B / other model / fine-tuning | Separate candidate only after a task/split is fixed. Check exact asset licence, CPU speed, memory and final artifact; no inherited benchmark or redistribution claim. Fine-tuning cannot cure a sparse or ambiguous key. |
| Lawyer confirmations | Useful secondary safety measurement; cannot turn abstentions into primary-score successes. Initially report after at most two confirmed items, separately from fully automatic accuracy. |
| Law retrieval or app routing changes | Out of this number and outside the permitted implementation scope. |

## 5. Expected unseen result and resource budget

My planning range for a successful combined candidate is **45–60% fully right without confirmation**, with low confidence and a working target near 50–55%. This is a forecast to falsify, not a measured result or promise of passing. I do not predict 71% or Alpha-02's original 80% from these data.

The old unseen counterfactual is informative: perfect polarity alone yields 47/90; perfect binding alone 39/90; both 64/90. Starting at 32/90, passing requires at least 13 net recovered cases. A polarity-only candidate would need almost every polarity-only recoverable case and very few regressions. Binding and omitted events must therefore be measured, not assumed away. As an illustrative budget, recovering nine polarity-only, four binding-only and three jointly blocked cases while regressing two would produce 46/90 (51%). Those are assumptions, not predictions about individual hidden cases. Hidden v2 has a different mix.

At six event questions, the **design budget**, using the historical fresh rates of 22 prompt tok/s and 5.5 answer tok/s, is:

| Work | Budget |
|---|---:|
| Shared case and compact instructions, approximately 900–1,050 uncached tokens | 41–48 s |
| Six short suffixes, at most 35 fresh tokens each | at most 10 s |
| Baseline compact output plus six status tokens, approximately 45–55 tokens | 8–10 s |
| Bounded binding/evidence additions, at most 220 fresh and 35 answer tokens | at most 17 s |
| HTTP, code and normal load allowance | 3–5 s |
| Total target | approximately 79–90 s |

This budget is tight. It explicitly includes the retained baseline answer. A second full case read, verbose evidence output, repeated retries or a failed cache assumption breaks it. Prefix placement changes are part of the experiment and must be compared with the original prompt, not assumed free. At the slow end there is no retry allowance: unresolved output is a failure for the relevant accuracy measures. Long cases need separate reporting; never truncate facts to meet the budget.

Report mean (the existing bench comparison), median, p95 and maximum wall time, including failed calls, plus cold-load and warm behaviour. Keep comparisons serial on the same plugged-in laptop, runtime, digest, context and CPU settings; record power, background load and cache counters. Do not infer fresh prompt speed from a total-token/cache-influenced ratio. Audit the meaning of counters on this installed server.

No Alpha-03 memory result exists yet. Keep a single 4B model initially. Measure the server and its runner together, app residence and whole-machine headroom/paging; distinguish a sampled concurrent peak from process lifetime peaks. The current facts harness records machine state/timings, **not peak inference memory**, despite the seed implying that memory reporting already exists. Add that measurement before qualification. Stop if model/optional encoder plus the running app cannot fit the available 16 GB without sustained memory pressure. A model's disk size is not its RAM requirement.

## 6. Plan, new data, ablations and stop rules

Everything below is **proposed Stop 2 work**, conditional on the owner approving this report. None of these reader, prompt, test or synthetic-case files has been written.

### Prevent a second tuned case set

Create `tests/alpha-03-cases/` separately from `tests/fact-cases`, labelled **Codex-authored fictitious development material**, with provenance, scenario/template families, split IDs and a frozen manifest. Plan 36 scenario families with four contrasts each (144 cases): asserted, notes-level denied, party-disputed and absent propositions. Cover the 21 claims, inverse/negative proposition wording, roles, irrelevant negations, cross-sentence denials, two events per sentence, documents-only evidence, identifier distractors and rates beside totals. Not every case needs every difficulty; include ordinary affirmative notes so anti-affirmation bias is visible.

Assign 24 complete scenario families (96 cases) to fitting and 12 (48) to a fixed internal audit **before inference**. All paraphrases, number substitutions, role swaps and related templates stay on one side; random row splitting would leak the task. Have labels reviewed against item 7 and code-checked for vocabulary/value existence before running models. Dense labels cover every asked item, not only selected decisive facts. Audit labels must not be adjusted after seeing model answers unless the owner adjudicates an independently demonstrated annotation error and the score/version change is disclosed.

Use grouped cross-validation within the fitting families for the tiny calibration grid and component choices. Freeze the candidate and its parameters before the audit. Inspect audit aggregates once; no per-case repair loop. **If its gate fails, stop and report to the owner**, rather than rewriting the prompt from its errors and rerunning it as if still independent. If the owner later authorises further development, that audit is retired as a tuning source and cannot serve again as evidence of generalisation. This author-written audit is never called blind or hidden; hidden v2 is still the owner's sole independent final measure.

The original 40 in-catalogue development cases remain a compatibility gate (at most three critical failures), not the fitting signal for polarity. Preserve the two robustness cases as separately reported regression checks. Use the 15 original validation cases at most three times after design freezes, as checks only. A failed check leads to a stop/report, not phrase-specific tuning. Historical validation summaries read for this report do not make that set fresh again.

### Files and order

| Order | Proposed files | Approximate size and purpose | Principal risk |
|---|---|---|---|
| 1 | `tools/alpha-03/README.md`, capability probe and run manifest support | 150–250 lines; exact serial commands, loopback-only score/token/cache probes, version/digest and resource capture | Silent unsupported options, cache assumptions, missing score alternatives |
| 2 | `tests/alpha-03-cases/README.md`, scenario cases and split manifest | 144 fictitious cases plus 100–150 lines of checking/split tooling; no original case edits | Synthetic style leakage, wrong dispute scope, tuning to authored templates |
| 3 | `src/DzLegal.Harness/` new experiment reader/runner, supplementary scoring/resource diagnostics; focused tests in `tests/DzLegal.Law.Tests/` | About 250–400 implementation lines plus 150–250 test lines | Different denominator, silently counting unresolved/missing items as correct, reading validation by default |
| 4 | `src/DzLegal.ModelProviders.Ollama/` opt-in scored-request support, and `IModelProvider.cs` only if needed | About 120–220 lines plus provider contract tests; leave existing request JSON/behaviour unchanged when unused | Stream framing, score position and constrained-vs-unconstrained distribution differences |
| 5 | New `src/DzLegal.Drafting/ScoredFactReader.cs`; new `data/law/prompts/read-scored-elements.ar.txt` | About 200–300 reader lines and a short new prompt; original reader/catalogue/prompt untouched | New focus variant repeats Alpha-02 without a score-based benefit |
| 6 | New binding/evidence helper and tests; harness ablation switches | About 150–250 lines plus 100–180 test lines; preserve original sentence IDs and code-found values | Wrong source selection, cross-sentence loss, rejecting legitimate shared values |
| 7 | `docs/ALPHA-03.md`, `docs/baselines/<date>-alpha-03-*/` summaries | Commands, fingerprints, results and failures for each rung; raw outputs outside repo | Selective reporting or accidental case/output disclosure |

These are estimates, not commitments to add every layer. Prefer a minimal external prototype through the existing scoring interface before changing provider interfaces. `facts export` currently omits the given claim ID and `facts score` does not apply Alpha-01's out-of-scope exclusion automatically: the experiment must supply the claim from the harness and select/report the same 40-case denominator. Do not read gold event labels into the prediction path. Build no alternative runtime silently; the brief requires Ollama at runtime.

Preserve the original parser/output path and add regression fixtures proving unchanged Alpha-01 parsing and old request serialisation. New tests should target behaviours: a party denial apart from its allegation, negative proposition versus negation, denial of a reason rather than its event, documentary assertions, invalid/missing scores and IDs, false monthly/total attachment, legitimately shared values, cancellation and partial output. No tests are removed, skipped or weakened.

### Gates and component attribution

1. **Capability gate first.** Require all four usable scores and real suffix reuse on the intended request shape. If log probabilities or required scores are unavailable, stop and tell the owner. A generated-choice baseline or a dedicated NLI pilot could be proposed instead, but neither is an automatic fallback. Do not upgrade Ollama, download another model or change persistent server settings to conceal a failed precondition.
2. **Fresh cost gate.** On fitting cases spanning short and long notes, measure the actual token and wall-time bill. Abandon the proposed call pattern if serial full reads put projected mean over 90 s or the context over 4,096 tokens. Report rather than truncate input or shift unnoticed to another runtime.
3. **Score usefulness gate.** Compare Alpha-01, focused uncalibrated choices and calibrated choices on the same fitting-family folds. Calibration survives only if it reduces both pooled failed cases and non-affirmed status errors, without increasing false affirmations, across at least two thirds of folds. A grid of ever more phrase/claim-specific corrections is not allowed. If neither focused version improves over Alpha-01 on the new contrasts, stop this path.
4. **Binding gate.** With polarity held fixed, binding must reduce wrong attachments on fitting folds without replacing them with missing values/requests or increasing decisive omissions. Independently measure evidence selection accuracy. If source selection is no better than the previous quote/alignment approach, abandon that component and report; do not describe checked IDs as semantic verification.
5. **Audit gate.** Before validation, require at least 50% of internal audit cases fully right, at least 80% recall separately on denied and disputed target propositions, no new increase in false affirmations relative to Alpha-01 on that audit, at most three critical failures on the original 40 development cases, and mean time at most 90 s with measured memory headroom. These are engineering stop rules, not estimates of hidden-v2 success. The audit is too small for confident population guarantees.
6. **Validation gate.** Reserve up to three frozen checks, log every one. A regression beyond the three-failure historical in-catalogue validation result, a failed time/memory gate, or evidence of the old polarity failure is a reason to stop and report, not tune on that case. Zero validation model checks have been used in Stop 1.

For every added component keep an on/off comparison: focus/cache shape, score versus generated selection where measurable, calibration, evidence restriction, binding, and optional unresolved threshold. Report interacting components as interactions, not additive independent gains. Every run reports each critical-error type, whole-case failures, omissions/completeness, time and memory. Do not add deduplicated totals to overlapping error-category counts. Repeated inference, if used to measure stability, is reported as repeated runs of the same cases, not independent new evidence.

## 7. Pass mark, critical errors and the exact answer-key rule

**No requested change** to the fixed pass mark: at least 50% of in-catalogue hidden-v2 cases fully right without asking, at most 90 s/case on the plugged-in laptop, model/server/optional encoder fitting 16 GB with the app running, and at most three failed original development cases out of 40. Keep the 71% stretch. Passing is a research milestone, not approval to ship a reader that still fails nearly half of complete cases.

**No requested relaxation** of critical errors or existing gold labels. Add supplementary dense correctness/completeness and unresolved/confirmation columns, with the legacy primary score alongside. Do not make absent/unknown/flagged predictions magically correct, or change errors after seeing hidden v2. If later data demonstrate a key error, present the source and exact proposed correction to the owner before a new frozen evaluation; do not repair it silently.

The following is the owner's clarification of the **existing annotation rule**, to be used unchanged by the hidden-v2 author:

1. **Affirmed (`stated` in an Alpha-01 answer):** the lawyer's notes assert the particular proposition, including a proposition which is itself negative. Thus an assertion of unpaid wages affirms `wages_unpaid`. A document named in `documents.list` affirms the event it is named after. The reader records an assertion in the lawyer's notes, **not judicial proof**. A source-only-from-documents marker may be recorded as information and does not alter polarity.
2. **Disputed:** a party denies or contests the same fact that the plaintiff relies on. The plaintiff's assertion may be implicit in bringing the claim; it need not be repeated in the notes. Therefore “the defendant denies receiving the loan” is **disputed**, not denied or absent. Contest attaches to its target proposition, not every event in the sentence or case. For the dismissal element «سرّح المدعى عليه المدعي أو فصله من العمل», which describes the act alone, a contested reason leaves the dismissal affirmed; a contested act makes it disputed. This is the owner's settled annotation decision, supplied directly for this report; the hidden-v2 brief was not opened.
3. **Denied:** the notes themselves state that the proposition did not happen, rather than merely reporting a party's opposition to it. For a notice event, «لم يوجه أي إعذار» is denied. Negation must be interpreted relative to the complete element: «لم يدفع» affirms non-payment and denies payment. A negation elsewhere in the sentence must not reverse an unrelated conceded event such as notice in «لم يدفع رغم إعذاره».
4. **Absent:** no assertion, notes-level denial, party contest or named-document assertion of that proposition is present. An unanswered or malformed model decision is **unresolved**, not evidence that the text is absent.

Apply these rules to each event at the granularity it describes, preserving attribution and qualifications as evidence. A named document is not a blanket override of an explicit contest of that event. The four labels are statuses of propositions, not a truth/proof scale. Do not introduce a new conflict/tie rule from an unreviewed example; report any genuinely contradictory annotation scenario to the owner before treating it as a training label.

## Stop 1 verification and handoff

- Only `docs/ALPHA-03.md` is authored for this stop. No source, prompt, catalogue, original test, case set, model, dependency or runtime setting is changed. No changed files outside the report's authorised path; existing untracked owner files remain untouched.
- No new tests are added or existing tests updated for this document-only change. The initial sandbox build/test attempts failed during NuGet signature retrieval (`NU1301`), before a meaningful test result. With the required access, `dotnet build -c Release` passed with **0 warnings, 0 errors**, and `dotnet test -c Release` passed **990 tests, 0 failed, 0 skipped** across seven projects, including 306 desktop tests and 78 web tests. Post-commit checks are recorded at handoff.
- Stop 1 runs no new reader experiment, training or validation inference. The only new fact score is the read-only development/robustness re-score of historical GLiNER predictions above. Aggregate historical validation diagnostics were read in the authorised history; validation case text/answers were not used for tuning. Hidden v2 remains untouched.
- No screenshots are requested by this brief. `dotnet run --project src/DzLegal.App -c Release --no-build --no-restore` started the existing desktop process, which was responsive to the process-status query. This is a startup smoke check only. No case was opened. There is no new app feature to exercise at Stop 1, and no live manual interaction or visual review is claimed: this session has no available Computer Use execution tool. Automated desktop tests are not a substitute for that review.
- Remaining empirical uncertainties are explicit preconditions for Stop 2: usable four-way scores, exact cache behaviour/cost, score separability and calibration transfer, evidence selection, resource headroom and the unknown hidden-v2 distribution. The earlier missing reports remain missing; the recovered interim history above supersedes the seed's claim that no outside extraction/Alpha-02 experiments were recorded.

**Stop here after committing, checking and pushing this report. Await the owner's answer before writing reader code or running the proposed experiments.**

## Stop 2 — approved amendments and first gate result (6 November 2026)

The owner approved Report 1 with three amendments, which supersede the original implementation order:

1. Gate 1 (four usable single-token scores and suffix reuse), then gate 2 (fresh cost on original development cases, including short and long), precede all new case writing. A failed gate requires a stop and report.
2. Rung 0 compares **unchanged Alpha-01** with installed `gemma4:e4b` and `gemma3:4b` on the original 40 in-catalogue development cases, and later on the frozen fitting families. Record each exact digest, licence, time and memory. This is a reference, not a runtime-model switch.
3. Write, check and freeze all 144 fictitious cases and their split manifest, including a content hash, **before any Alpha-03 prompt or reader code**. Subsequent case edits require owner-adjudicated annotation errors and a disclosed new hash.

The owner initially reserved review of the dismissal example, then settled it in the subsequent gate-1 authorisation: the element describes the act alone, so a contested reason leaves the act affirmed and a contested act makes it disputed. Item 7 now records that decision. No other annotation rule changed, and no part of the hidden-v2 brief was read.

### Gate 1: stopped on missing suffix reuse

**The four-choice score probe worked, but the tested shared-prefix request shape did not reuse the case when its question suffix changed. Gate 1 is not passed.** No gate-2 runs, rung-0 inference, new cases, candidate prompt files, reader code, calibration or validation runs followed this result.

The [gate summary](baselines/2026-11-06-alpha-03-gate-1/summary.md) records the requests, exact results, local artifact locations and hashes. At `5a3f846`, Ollama 0.40.15 on mains power, with CPU-only `gemma3:4b`, returned all four finite scores (`1`–`4`, each represented as a one-byte token at the first output position). Three questions about the original development case `loan-basic` shared the case/instructions inside one user message; only the final question changed. The first two requests share 1,026 user-text characters, with identical model options and output schema.

| Request | Total prompt tokens | Cached tokens | Prompt evaluation seconds | Wall seconds |
|---|---:|---:|---:|---:|
| First element question | 413 | 0 | 20.472 | 20.681 |
| Changed suffix, second element | 408 | 0 | 21.856 | 21.947 |
| Changed suffix, third element | 404 | 0 | 22.083 | 22.217 |
| Identical third-question user message, schema removed as a control | 404 | 399 | 0.942 | 1.011 |

This is evidence against assuming cheap **within-message suffix changes** in this configuration. It is not evidence that every cache mechanism or message arrangement fails: the identical-message control reused work. Both constrained and unconstrained requests returned four finite choice scores; their label ranking was unchanged in the paired control, with a maximum absolute log-probability difference of about 0.128. No calibration or semantic accuracy conclusion is drawn from these probes.

There was also one invalid request for 100 alternatives. The server rejected it with HTTP 400 (`top_logprobs` permits 0–20). The earlier smoke probe had already worked with 20; correcting the request to that supported value was a parameter correction, not recovery from an unsupported-score precondition. The failed request and empty response artifact remain in the external record. A one-token generation intentionally returns `done_reason: length`; no partial case answer was accepted as a fact sheet.

### Reference metadata, without a rung-0 accuracy claim

The installed tags are unchanged from Stop 1:

| Model | Installed digest | Licence returned by local `/api/show` |
|---|---|---|
| `gemma3:4b` | `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` | Gemma Terms of Use |
| `gemma4:e4b` | `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c` | Apache License 2.0 |

These are local metadata observations, not a redistribution review or a newly measured comparison. Rung 0 has **zero of 40 cases run for either model in Stop 2**. Time and peak-memory results therefore remain unavailable. One process-status sample after the smoke probe saw the runner at approximately 3.68 billion working-set bytes and the server at 36 million; that is not a peak or proof of fitting alongside the app. No weights, licence assets or model settings were installed or changed.

### Proposed next action, awaiting the owner

I would next test a **message-boundary form using the existing `ModelRequest.Earlier` mechanism**, placing the common case in an unchanged earlier turn and changing only the final question turn. The observed difference between changed and identical user messages makes that a specific hypothesis worth testing; it does not prove the server's cache is keyed by whole messages. This would remain a bounded capability probe, without cases, a candidate reader or a runtime change. If approved and it passes, gate 2 and rung 0 still have to be completed before writing and freezing the 144 cases.

I have **not** run that alternative or silently changed the server, model or request strategy after the failed gate. Rung 0 is pending because the instruction to stop on a failed prerequisite takes precedence over continuing the measurement programme.

### Change and check record

Only this report and the gate summary are changed in this stop. No tests are added, modified, skipped or weakened. No source, existing prompt, catalogue or original case file is changed. Raw probe outputs stay outside the repository. Validation checks used in Stop 2: **0**; hidden material accessed: **none**. Build/test results for this documentation commit are reported in the handoff; the known intermittent unload-test failure from the Stop-1 post-commit run remains disclosed in that earlier handoff.

**Stopped at gate 1. Await the owner's decision before any further model experiment or case writing.**

## Stop 2 — bounded gate-1 continuation (6 November 2026)

The owner authorised three request arrangements on the same original development case, `loan-basic`, serially, using the same model and sampling settings. This supersedes the preceding gate-1 pause for those probes only. The owner also required an explicit wait **before gate 2**, because another job is using the laptop's CPU.

**Gate 1 passes on append-only shape (b), within this bounded probe. Gate 2 has not started.** Shape (a) did not reuse the prefix, and the tested one-call shape (c) did not supply all four class scores at every status position. The [full summary](baselines/2026-11-06-alpha-03-gate-1-shapes/summary.md) records all eight calls, totals, token positions and artifact hashes.

| Shape | Calls | Cached tokens, in call order | Four finite scores at every status? | Wall seconds, in call order |
|---|---:|---|---|---|
| (a) Unchanged earlier case turn; changed final question | 3 | 0, 0, 0 | Yes, at all three tested answers | 33.118, 21.665, 24.517 |
| (b) Exact append-only conversation, carrying actual one-token answers | 3 | 0, 408, 440 | Yes, at all three tested answers | 17.955, 2.348, 2.506 |
| (c) One six-status array per case, fixed order/template | 2 | 0, 516 | **No:** choice `3` missing at the first status in both calls; the other five statuses each have all four | 27.003, 3.941 |

### What was actually sent and checked

For (a), the common case/instructions are an unchanged user turn, followed by the fixed neutral assistant acknowledgement `حاضر.`, then a separate final user question. The acknowledgement is a fixed fixture, not a model judgement or read fact. Only the question changes across the three requests; each request is independent of the preceding answer.

For (b), the first user message contains the common case and question 1. Requests 2 and 3 preserve every previous message exactly, then add the actual preceding assistant answer and the next question. The recorded answers are `1`, `2`, `3`; none was replaced with a gold answer or a score argmax. An artifact check confirmed the earlier message arrays were unchanged and the appended answer contents were verbatim. All four label alternatives are finite at the first/only output token of each request. The observed continuation cache counts therefore satisfy the reuse part of this bounded gate.

For (c), a single user message asks all six original loan elements at once, with a fixed six-position template and an array schema whose items are integer enums `[1,2,3,4]`. Its cap is 64 tokens to allow all six statuses and framing, rather than the one-token cap for (a)/(b); all other model/sampling options remain the same. Each call completed with a valid six-element array and 19 generated tokens. The status token indices are 1, 4, 7, 10, 13 and 16 (zero-based). Both calls lack choice `3` in `top_logprobs: 20` at index 1. A missing score was not treated as zero, negative infinity, or a known low probability. Although valid, this particular array format does not pass the full-score gate. No further schema/wording variants were tried.

The same call-(c) request and seed produced different arrays on its cold and cached executions. This is another reason not to equate a fixed seed or schema with stable semantic decisions. These probes are not accuracy comparisons: no prediction was scored against gold, and no wording was adjusted from an answer's correctness.

### Interpretation and design trade-off

The results are **consistent with** the owner's hypothesis that extension is reusable where cutting back to change a suffix is not. They do not establish Gemma's sliding-window implementation as the cause: no cache internals were inspected, and only one case and three element questions were used for (a)/(b). Cached-token counters, not wall-time improvements alone, support the observed reuse.

Shapes **(b) and (c) expose earlier generated answers to later decisions**, as Alpha-01 does within its existing answer. That departs from Report 1's preference for independent questions. A wrong earlier answer, the ordered choice codes and repeat penalties may influence later scores; calibration would have to be evaluated on that actual ordered, generated-history distribution. Do not substitute corrected history at runtime or report conditional token probabilities as independent marginal judgements. Shape (b)'s cache success does not establish reading accuracy or make that dependence harmless.

These wall times are **diagnostic gate-1 observations under the owner's reported concurrent CPU load**, not gate-2 timing evidence or a demonstration of the 90-second budget. A (b) chain covering only three questions is not a complete six-element reader, and does not include Alpha-01 value binding, requests or role work.

### Next step and current pause

The next eligible shape is **(b)** for gate 2. Before it starts, the owner must confirm that the other heavy CPU job has finished. No fresh-cost measurement, rung-0 inference, fitting-family writing, candidate prompt/reader code or validation/hidden run has started. The 144-case freeze still precedes any candidate prompt or reader implementation; the authorised wire fixtures do not become a shipped prompt.

Only this report and the new probe summary are edited for this continuation. No tests or application/harness/provider code changed. I have left this documentation update uncommitted while waiting for the CPU to be available, so the repository's required post-commit build/test cycle does not add a heavy competing job. The last committed documentation (`759a901`) already passed its Release build and all 990 tests. This continuation does not claim a new build/test result.

**Waiting before gate 2, as explicitly requested. No further model calls are authorised by this record while that wait is pending.**

## Stop 2 — timing preflight (7 November 2026)

The owner confirmed that competing CPU work had finished and authorised gate 2 on shape (b), a small reverse-order check, then unchanged Alpha-01 with both installed models on the original 40 development cases, followed by a stop before case writing. The preceding gate-1 record was committed and pushed to `codex/alpha-03` as `5dc52cb`.

The post-commit Release build passed with **0 warnings and 0 errors**. The first full test run had 989 passes and the previously recorded unload-test failure (`Expected 1, Actual 0`). The unchanged test passed alone. A second full run repeated that failure and was interrupted while still in app tests; it is not reported as a completed run. The subsequent complete run, `dotnet test -c Release --no-build -m:1 -- xUnit.ParallelizeTestCollections=false`, passed **all 990 tests, with 0 skipped**. This uses a [documented xUnit runner setting](https://xunit.net/docs/config-runsettings) to execute collections serially. No assertion, test, source behaviour or repository test configuration was changed. The production cause of the intermittent failure has not been fixed in this task.

The prepared measurement tools in `tools/alpha-03/` reuse the accepted external scalar fixture. The forward plan covers all **147 catalogue event questions across 40 cases**, with fresh caches and separate model-load measurements. Three cases were selected for reversal before inference: `loan-no-section`, `loan-basic`, `non-delivery-machines`. The retained Alpha-01 pass supplies roles, binding and requests and must be counted in the projected total cost. Original notes span only 62–172 characters, limiting what their longer half can establish. These are authorised measurement fixtures; no candidate reader, new prompt wording or new cases have been written.

**Timing has not started.** After build-server shutdown, the mains-power preflight failed. A direct Windows `GetSystemPowerStatus` check at **2026-11-07 15:06:09 UTC** returned `ACLineStatus = 0` (offline), battery **29%**. The app and resource monitor had not been started, and gate 2/rung 0 made **zero inference calls**. The brief requires plugged-in timing, so the owner was asked to connect mains power and this work is waiting for that answer. The external record is `%TEMP%\dzlegal-alpha03-gate2-20261107\preflight-stop.json`. Prepared tooling and this preflight note remain uncommitted pending the measurements and their final report. No validation or hidden input has been used.

## Stop 2 — interrupted timings and terminal handoff (7 November 2026)

The owner connected mains, selected Best performance and required that mode for every timed run. Windows AC/overlay queries initially still reported efficiency mode. The requested Best performance AC mode was applied and verified before inference; all sampled timed-phase actual/effective/configured mode values agree with `ded574b5-45a0-4f42-8737-46345c09c238`. The Balanced base plan remained active separately. The measurement tools now record and check those power values, alongside CPU load, simultaneous model/server/app memory, machine headroom and paging.

The attempted gate-2 run completed **21 chains and 85 status responses**, all with four finite single-token scores; **64/64 extensions reused cached tokens**, all first calls cached zero, and the largest prompt was 571 tokens. Diagnostic chain mean was 12.780 seconds with separate empty warm-up mean 4.225 seconds. These figures **do not qualify gate 2**: monitoring found sustained VS Code C# Dev Kit CPU work, with other processes averaging 1.461 core equivalents and peaking at 4.276. Available physical memory also briefly fell to about 46 MiB. Whole-reader cost, memory fit, reverse-order effects and both rung-0 comparisons remain unresolved. Details and artifact hashes are in the [interrupted-run summary](baselines/2026-11-07-alpha-03-gate-2-interrupted/summary.md).

The owner explained that this agent is running inside VS Code's terminal and asked for a pause to reopen it from standalone PowerShell before closing VS Code. The benchmark was stopped, its model unloaded, its resource monitor stopped and its idle app asked to close normally. Owner processes were left untouched; Best performance remains enabled. [Resume instructions](../tools/alpha-03/RESUME.md) preserve the exact state and outstanding work. A clean run must use a new external directory and must not be pooled with these interrupted timings.

Measurement tools and this diagnostic record remain local and uncommitted at the handoff. The last successful commit/push is still `5dc52cb`; its Release build and all 990 tests passed as recorded above. No post-commit check is claimed for the pending tools. No reader, new prompt wording, new cases, source changes, test changes, validation check or hidden-data access occurred. The next step remains the authorised gate-2/order/rung-0 measurements, then committing/pushing tools and results with required checks, and stopping **before any case writing**.

## Stop 2 — completed cost/order/reference measurements (7 November 2026)

**The cost/context gate passes on the original development cases; accuracy and integrated memory qualification remain open.** Shape (b) completed all **147 event questions in 40 fresh forward chains**, mean **11.358 seconds**. Adding the full unchanged gemma3 Alpha-01 batch cost for roles, binding and requests gives a **24.532 s/case projected mean**, below 90 seconds. Maximum scored prompt was **571 tokens**. This is a sum of separately measured components, with no new candidate reader implemented or timed end to end. Substantially longer notes still need measurement.

The owner moved the agent to standalone PowerShell and closed VS Code. Every timed run used **mains power and Best performance**, verified through actual, effective and configured AC mode GUIDs `ded574b5-45a0-4f42-8737-46345c09c238`, with zero sampled violations. The Balanced base plan was recorded separately. No other model benchmark, build or test ran concurrently. Ordinary OS/desktop/agent load remained recorded rather than assumed to be zero. Raw outputs occupy a new external directory; the interrupted run was not pooled into the results.

| Measurement | Result |
|---|---|
| Shape (b), all forward/reverse status positions | **164/164** single-token responses with four finite scores |
| Fresh first calls / literal-history extensions | **43/43** cached zero / **121/121** reused 353–529 tokens |
| Generated statuses changed under reversal | **6/17**, across all three checked cases |
| Four-score argmaxes changed under reversal | **8/17** |
| Unchanged Alpha-01, gemma3:4b | **38/40 without a critical error**, 0 call failures; bench mean **13 s/case** |
| Unchanged Alpha-01, gemma4:e4b | **0 successful readings**, **40/40 length-limit failures**; bench mean **39 s/case** |

An independent audit of the stored artifacts verified all request settings, exact previous-message preservation, actual generated-answer history and the first `loan-basic` prompt's equality with the accepted fixture, with **zero mismatches**. No missing class score was fabricated. The four-score argmax is diagnostic here; no calibration was fitted.

### Cost and order implications

The shorter 20 original notes (62–99 characters) took a mean 11.097 seconds for their complete status chains; the longer 20 (100–172 characters) took 11.619 seconds. With the retained baseline's rounded case rows added, the respective projected means are **23.047** and **25.919 seconds**. These groups have different claim/question mixes and both contain very short notes; they do not establish cost on realistic lengthy files or dedicated requests/documents fields.

The forward chain's median/p95/maximum are 11.271/14.776/15.336 seconds. Empty runner loading averaged a separate 3.082 seconds. The 24.532-second projection uses the full gemma3 CLI wall time, including setup/report overhead, divided by 40, plus the chain mean. Alpha-01's normal across-case server cache behaviour was retained; its existing provider does not expose cached-token counters. The eventual integrated reader must be measured under its actual call/reset policy, including additional binding/evidence work. Do not compare this short development-set projection directly with the older hidden-set 63-second mean under different inputs/power conditions.

The order check is a warning about the carrier design, even though cache reuse works. Later answers can see earlier generated answers. Reversal changed score argmaxes as well as sampled digits; the small check cannot isolate every change from runtime/sampling variation without same-order repeats. It gives no evidence that a global correction will remove that dependence. The original all-affirmed key must not become the fitting signal for denied/disputed choices. The approved 144-case freeze and grouped fitting/audit protocol still precede any candidate prompt/reader implementation.

### Reference quality and memory implications

Gemma3's two critical errors were a plaintiff-position error in `contract-damages` and a missed decisive notice in `rent-basic`. Existing non-critical omissions remain disclosed in its summary. Its 100% polarity column is conditional on the old sparse key, whose development/robustness events are all affirmed; it is not evidence of denied/disputed competence. No evaluator, key or pass-mark rule was changed.

Gemma4 was run exactly as authorised: the same reader and 300-token cap, with thinking left unset. Every call reached that cap and the existing provider rejected the cutoff before a fact reading reached the evaluator. Its “Critical errors: none” line therefore cannot be read as semantic success. The metadata advertises thinking capability, but this run did not isolate how the allowance was spent. No thinking-off, higher-cap or other rescue variant was run, and no runtime model switch was made.

The reset-per-case score chains had sampled model/server peak **4.082 GiB**, simultaneous model-plus-app peak **4.260 GiB**, with at least **5.323 GiB** whole-machine physical memory available. The resident references reveal a separate limit:

| Resident reference | Model/server sampled peak GiB | Simultaneous model + app peak GiB | Minimum available physical GiB |
|---|---:|---:|---:|
| gemma3:4b | **7.852** | 7.954 | 1.808 |
| gemma4:e4b | **9.942** | 10.042 | 0.396 |

Each batch used one runner PID throughout. Gemma3's early/late mean model footprint grew from about 4.001 to 7.377 GiB; gemma4's from 6.607 to 9.840 GiB. The growth's cause and plateau were not established. Updated samples observed no page outputs, with page-read spikes and zero median reads; machine commit reached 18.996 GiB for gemma4. These measurements do not certify absence of all paging or persistent integrated-reader memory stability. A case reset is a possible future policy with an observed loading bill, not a fix implemented or qualified here. The fresh-chain footprint must not be substituted for the observed resident-batch peaks.

### Records, scope and stop

Full protocol, per-group timings, order changes, hashes and memory limits: [gate 2](baselines/2026-11-07-alpha-03-gate-2/summary.md). Full existing-evaluator results, each critical error, licence, digest, disk size, timing and memory: [gemma3 rung 0](baselines/2026-11-07-alpha-03-rung-0-gemma3-4b/summary.md), [gemma4 rung 0](baselines/2026-11-07-alpha-03-rung-0-gemma4-e4b/summary.md). The [interrupted attempt](baselines/2026-11-07-alpha-03-gate-2-interrupted/summary.md) remains separately disclosed. Raw data, wire audit, manifests and resource logs stay at `%TEMP%\dzlegal-alpha03-gate2-clean-20261107-165004`.

New tooling under `tools/alpha-03/`: accepted-fixture chain measurement, unchanged Alpha-01 invocation, resource/paging sampling, power verification, README and the retained historical terminal-handoff checkpoint. Files changed outside that folder are only this report and the four new baseline summaries named above. The existing `src/`, `data/` and `tests/` contents are unchanged. Tests added/updated/skipped/weakened: **none**. The real app was launched for memory residence; the feature was exercised through the authorised terminal/harness commands. No UI change or screenshot was requested.

The measurement tools/results are committed on `codex/alpha-03`, with required post-commit Release build/full-test results reported in the handoff. The previously observed unload-test scheduling failure remains disclosed in the preflight record; serial xUnit collection execution preserves all assertions. No claim of a new candidate, calibration gain, hidden-v2 readiness or final reader freeze is made. Validation model checks remain **0**; hidden/evaluation data access remains **none**; new case files remain **0**.

**Stopped after gate 2, the order check and rung 0, before writing any case, as requested.**

## Stop 2 — bounded repeat, blind-history and thinking-off probe (7 November 2026)

**The forward repeat was stable; blind history retained usable scores/cache reuse but remained order-sensitive. Gemma4 with thinking off became a usable Alpha-01 reference: 39/40 without a critical error, 8.793 s/case, sampled model/server peak 7.813 GiB.** No new case, candidate prompt/reader or calibration followed these results.

The owner authorised exactly three original-case forward repeats, forward/reverse blind histories on those same cases, and the original 40-case gemma4 reference with the existing provider's `think: false`. The owner subsequently allowed the first two parts on battery, with seconds informative only, and required a wait for mains before the third. That wait and confirmation occurred. No battery seconds were used to revise the accepted cost gate.

| Diagnostic | Result |
|---|---|
| Same-order repeat, generated statuses | **0/17 changed** from the accepted forward run |
| Same-order repeat, four-score argmaxes | **0/17 changed** |
| Previous forward/reverse comparison | 6/17 generated; 8/17 argmax changes |
| Blind forward/reverse argmax comparison | **4/17 changed** |
| Blind usable positions | **34/34** one-token `0` outputs and four finite status scores |
| Blind fresh calls / extensions | **6/6 cached zero; 28/28 reused 373–529 tokens** |

The unchanged cases were `loan-no-section`, `loan-basic`, `non-delivery-machines`. Bare `null` emitted only `n` under the fixed one-token cap, so the authorised sole fallback `0` was used throughout the completed blind chains. Every extension preserved earlier messages and actual forced zeros verbatim; no earlier class choice was inserted. The independent raw audit covered all 51 usable repeat/blind requests with zero message/option/token/score/cache mismatches; the unusable null attempt remains separately recorded.

This one repeat supports order/history dependence over observed run-to-run label variation on these cases. Blind history hides earlier choices but exposes earlier questions, their order and a fixed zero. It does not remove order dependence, and the small 8/17 versus 4/17 comparison is not evidence of a general accuracy improvement. No missing score was fabricated and no correction was fitted to these cases. Full per-case changes, power state, informative seconds, vectors/artifact references and hashes: [repeat/blind summary](baselines/2026-11-07-alpha-03-blind-history/summary.md).

### Thinking-off reference

On **mains and Best performance**, unchanged Alpha-01 with gemma4:e4b and `Think = false` completed **40/40** original in-catalogue development cases. All 40 wire requests retained streaming, CPU-only context 4096 and **300-token cap**. There were **zero cap hits or technical failures**; answers used 17–56 tokens. The bounded stop rule was first cap/technical failure, with no higher cap or retry. All forty therefore ran as planned.

The existing evaluator found **one critical polarity error**: `annulment-fraud-land`, `concealment` denied instead of affirmed. Other critical error types: zero observed. Non-critical differences: unreadable-line occurrences in six cases (eight individual lines), and two missing requests (`divorce`, `reinstatement`). Position, decisive-event and date-binding columns were 100%; polarity 98%; requests 93%. These retain the existing sparse, all-affirmed development key's limits; denied/disputed competence and unseen-case gain remain unmeasured.

Exact case-time mean/median/p95/maximum were **8.793 / 8.762 / 11.945 / 21.866 seconds**. Full host invocation was **352.823 seconds**, or 8.821/case, including the cold model load. Sampled model/server working-set peak **7.813 GiB**, simultaneous model + app peak **7.990 GiB**, minimum machine available physical memory **3.215 GiB**. These are batch observations, not long-session qualification. Installed digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`, local licence Apache 2.0, 6,583,656,505 disk bytes. No runtime model switch or weights redistribution occurred.

There were **167 continuous power samples and zero mains/Best performance violations**. Windows Search became busy after mains connection, so timing waited. At the owner's explicit request its service was temporarily stopped without changing startup settings; quiet preflight preceded inference, and Running/Automatic was restored after it. The idle real app stayed resident, then closed normally; probe models were unloaded. Best performance remains configured. Full evaluator result, exact/native timings, memory/paging limits, request audit, reproducible commands and hashes: [thinking-off reference](baselines/2026-11-07-alpha-03-rung-0-gemma4-think-off/summary.md).

### Scope, checks and stop

Measurement-only additions under `tools/alpha-03/`: repeat/blind invocation and independent audit, thinking-off invocation and audit, and a small host that directly calls the existing reader/provider/evaluator. No production source or existing prompt/reader was modified. Files changed outside that folder: this report, the two summaries linked above, and the separate [owner memory note](OWNER-GEMMA3-RESIDENT-MEMORY-NOTE-2026-11-07.md). That note repeats the already accepted resident gemma3 observation (about 4.0 to 7.4 GiB); **no investigation of it was undertaken**.

The measurement host built with 0 warnings/errors before inference; all four new PowerShell measurement/analysis files parsed cleanly, and both raw audits completed successfully. Required post-commit Release build/full-test results and branch push are reported in the handoff. Tests added/updated/skipped/weakened: **none**. The real app was run for residence; the measurement feature was exercised through the terminal/harness. No UI changes or screenshots were requested.

Validation model checks remain **0**; hidden/evaluation access **none**; new case files **0**. The approved 144-case freeze still precedes all candidate prompt/reader implementation. **Stopped after this bounded probe, before writing any case, awaiting the owner's decision.**

## Stop 2 — 144-case data freeze (7 November 2026)

**The approved corpus is written, checked and frozen: 144 fictitious cases in 36 families, covering all 21 claims. No inference on these cases and no new candidate reader/prompt/calibration code preceded or followed the freeze.** The earlier authorised probes remain original-development measurements only.

Content hash: **`a3eb985d8f3bafe6bbe43c6afcd742ca0debc09e5b106921c4d4cb352d3bac68`**. [Archived first freeze record](../tests/alpha-03-cases/revisions/freeze-v1.json), [corpus instructions](../tests/alpha-03-cases/README.md), [split manifest](../tests/alpha-03-cases/split-manifest.json), [complete summary](baselines/2026-11-07-alpha-03-case-freeze/summary.md).

| Group | Families / cases | Polarity families / cases | Binding families / cases |
|---|---:|---:|---:|
| Fitting | 24 / 96 | 18 / 72 | 6 / 24 |
| Internal audit | 12 / 48 | 9 / 36 | 3 / 12 |
| Each of 3 fitting folds | 8 / 32 | 6 / 24 | 2 / 8 |

The plan's seeded allocation and pre-authoring hash were fixed before case prose. Related families are grouped by claim, so neither a claim's paraphrases/number substitutions nor its role variants cross fitting/audit or fitting folds. Fitting covers 13 claims, audit 8; all 21 appear overall. The static checker reconstructs this assignment. This is deliberately stricter than a random row split, with heterogeneous folds and no claim of population representativeness.

### Owner realism amendment and annotation

The owner rejected artificial denied/absent marriage files for divorce and khul, and authorised analogous substitutions wherever denial/absence would remove the realistic basis of the scenario. **Nine families are binding families**, with reasons recorded in the plan/manifest: fraud's core concealment scenario, personal-harm core scenario, rent arrears, titleless occupation, lessee renewal-refusal compensation, divorce, khul, maintenance arrears and dismissal. The other **27 families retain all four target polarity contrasts**, giving 27 targets of each class overall (18 each on fitting, 9 each on audit). Ancillary denials do not erase an independent asserted claim basis.

Marriage is affirmed in every divorce/khul file. Their contrasts distinguish marriage from separation, a previous third-party divorce and document dates; and khul offers from historical dowry, waiting-period/child-maintenance components, actual requested aggregates and monthly housing amounts. Every relevant value is tied to its source sentence. Dismissal follows the settled act-only rule: contested reason affirmed; contested act disputed. No new tie/conflict rule was introduced.

Dense keys contain **548 event labels** (340 affirmed, 60 denied, 28 disputed, 120 absent), **368 request labels**, 78 competing-value notes and 32 document-only event annotations. They cover all asked items rather than only decisive targets. Role, event, request and value evidence uses actual `CaseText` IDs. Known-null bindings are not filled by arithmetic. The existing evaluator projection remains separate from dense absence/request/completeness checks; the original keys, evaluator and pass mark are unchanged.

The author reviewed all **144/144** cases against item 7 and the realism amendment, including common text, alternate sources and request scope. This is an author review, not independent legal validation. Pre-freeze corrections included actor mapping, a common sentence pre-asserting an intended optional event, documentary markers and a request negation with excessive scope. No prediction motivated an edit. The [review record](../tests/alpha-03-cases/review.md) gives each family's check and limitations. No unresolved contradictory annotation was retained.

### Checks, fitting requirements and stop

The existing harness found **no problems in 144 cases**. The new static checker, using unchanged production sentence/value/catalogue APIs, passed **1,939 citation checks** with zero problems and built with zero warnings/errors. Structural checks passed membership, exact 96/48 grouping, three fitting folds, both kinds in every group, date/amount syntax, no duplicate inputs and no substantial cross-split numeric templates. Notes span 93–1,007 characters (12 at least 700); all read fields together span 186–1,126 characters. Longer real files remain outside this synthetic coverage.

The one-way freeze includes cases, per-case/file hashes, plan, authored source, rules, review and checking tools. Regeneration was attempted after freezing and refused; verification confirmed the same content hash. No case, source or split has changed since. Owner-adjudicated annotation corrections alone may change case data later, with a disclosed new hash.

The owner's two fitting variables remain explicit and separate from case authorship: **fixed-order blind history versus forward/reverse score averaging**, charging the complete cost of both; and **every candidate on gemma3:4b and gemma4:e4b with thinking off**, side by side with time/memory. Use the same grouped fitting protocol and report polarity/binding families separately. Predeclare precise averaging/selection details before inference. Neither becomes the runtime model from these comparisons. No fitting or audit run is authorised by this freeze record itself.

Files outside `tools/alpha-03/`: the new `tests/alpha-03-cases/` corpus/metadata, this report, the new freeze summary and **one root `.gitattributes` rule** that pins the frozen tool-attributes file to LF for Windows byte-hash portability. All `src/`, `data/`, original `tests/fact-cases/`, UI and lawyer-safety code remain unchanged. Unit tests added/updated/skipped/weakened: none; the new corpus checker and guard were exercised through the CLI. The real-app launch/normal-close smoke evidence is from the accepted preceding probe; runtime code is unchanged and there is no new app-facing feature or screenshot requirement here. Required post-commit Release build/full-test and push results are reported in the handoff.

Candidate/fitting/audit model runs on this corpus: **0**. Original validation model checks: **0**. Hidden/evaluation data access: **none**. **Stopped after the 144-case freeze, before candidate runs, for owner review.**

## Stop 2 — owner-adjudicated text correction and second freeze (7 November 2026)

**Re-frozen after removing artificial explanatory hints from the notes. All labels, values, dates, source identities and links, families, splits and folds are unchanged. No inference preceded or followed the correction.** The owner accepted the first freeze's hashes, facts check, coverage, splits and sampled labels, then adjudicated this text-only annotation-quality correction. This is the explicitly authorised exception to the frozen-data rule.

| Version | Content SHA-256 |
|---|---|
| First freeze, commit `41a9a86` | `a3eb985d8f3bafe6bbe43c6afcd742ca0debc09e5b106921c4d4cb352d3bac68` |
| Second freeze, current | `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642` |

The [old record](../tests/alpha-03-cases/revisions/freeze-v1.json) is preserved byte-for-byte; the [current record](../tests/alpha-03-cases/freeze.json) links to it. The [correction inventory](../tests/alpha-03-cases/revisions/v2-text-correction.json) includes the owner's decision, 93 source-edit rules, the full case/sentence before-and-after text, the changed IDs and the invariant result. The current hash covers 17 payload files, including this history and the bounded correction checker. It is a data re-freeze, not a candidate-reader freeze for hidden v2.

### What changed and what was held fixed

All **144 cases / 381 distinct sentences** were reviewed. **123 cases and 219 sentence texts changed**: 85 fitting cases and 38 audit cases. The other 21 case objects are identical. Removed wording includes `صراحة`, `بمجموع صريح`, instructions not to count a deposit or expense, declarations that a number is not an amount, explanations that a document does not establish an event, missing-value announcements, and statements telling the reader how to scope an assertion. Basic facts, transaction nouns, actual requests and time units remain; natural `فقط` can remain.

The correction was previewed outside the repository, checked, then applied to the authored source and assembled static cases. A comparison against the pinned first commit proves that every gold/dense label and value, date, actor, document-source marker, role, case ID, sentence ID/field, evidence edge and group assignment is unchanged. **Every numeric token, including competing figures and identifiers, stays in the same source sentence and in the same order.** No sentence is added, removed, split, merged or moved. Only the input text and its matching sentence-snapshot text change; manifest hashes are recomputed for that text. The family-plan bytes remain unchanged.

The immutable label/citation projection has SHA-256 **`8676499900ce8e8b012a54bb68537d51f1ee9d8609361297e0d739b07142819c`**, identical before/after. This excludes the deliberately rewritten sentence text; source identities and links are separately checked. The correction checker reads local Git objects from `41a9a86`, so that predecessor must be present in the checkout. No predictions, candidate outputs or private history were used.

**Cases changed (complete list):**

`a03-005`, `a03-006`, `a03-007`, `a03-008`, `a03-009`, `a03-010`, `a03-011`, `a03-012`, `a03-013`, `a03-014`, `a03-015`, `a03-016`;
`a03-021`, `a03-022`, `a03-023`, `a03-024`, `a03-025`, `a03-026`, `a03-027`, `a03-028`, `a03-029`, `a03-030`, `a03-031`, `a03-032`;
`a03-033`, `a03-034`, `a03-035`, `a03-036`, `a03-037`, `a03-038`, `a03-039`, `a03-040`, `a03-041`, `a03-042`, `a03-043`, `a03-044`;
`a03-045`, `a03-046`, `a03-047`, `a03-048`, `a03-050`, `a03-053`, `a03-054`, `a03-055`, `a03-056`, `a03-057`, `a03-058`, `a03-059`;
`a03-060`, `a03-061`, `a03-062`, `a03-063`, `a03-064`, `a03-065`, `a03-066`, `a03-067`, `a03-068`, `a03-069`, `a03-070`, `a03-071`;
`a03-072`, `a03-077`, `a03-078`, `a03-079`, `a03-080`, `a03-081`, `a03-082`, `a03-083`, `a03-084`, `a03-085`, `a03-086`, `a03-087`;
`a03-088`, `a03-089`, `a03-090`, `a03-091`, `a03-092`, `a03-093`, `a03-094`, `a03-095`, `a03-096`, `a03-098`, `a03-100`, `a03-101`;
`a03-102`, `a03-103`, `a03-104`, `a03-105`, `a03-106`, `a03-107`, `a03-108`, `a03-109`, `a03-110`, `a03-111`, `a03-112`, `a03-113`;
`a03-114`, `a03-115`, `a03-116`, `a03-117`, `a03-118`, `a03-119`, `a03-120`, `a03-121`, `a03-122`, `a03-123`, `a03-124`, `a03-125`;
`a03-126`, `a03-127`, `a03-128`, `a03-129`, `a03-130`, `a03-131`, `a03-132`, `a03-137`, `a03-138`, `a03-139`, `a03-140`, `a03-141`;
`a03-142`, `a03-143`, `a03-144`.

### Natural rewrites used to preserve attribution or scope

Simply deleting a whole explanatory clause could obscure the underlying transaction, period, party or contested proposition. The following cases use natural factual rewrites instead; none retains an unresolved label ambiguity:

| Cases | Attribution/scope retained |
|---|---|
| `a03-061`, `a03-062`, `a03-063`, `a03-064` | The contractual monthly obligation and the actual payment of a security deposit, without telling the reader to exclude the deposit |
| `a03-098` | Earlier divorce from another wife, expressed as marital history rather than a date-exclusion instruction |
| `a03-109`, `a03-110`, `a03-112` | Actual relief, waiting-period/child components, earlier child maintenance and housing periods/units, without answer-key total labels |
| `a03-117`, `a03-119` | Periodic entitlement and accrued debt stated as facts, without rate-versus-total coaching |
| `a03-137`, `a03-140` | Dismissal asserted while its reason is contested, without saying how to label the act |

### Checks, audit interpretation and stop

The existing harness still reports **144 cases; no problem found**. The unchanged dense checker passes **548 event labels, 368 request labels and 1,939 citation checks with zero problems**. Structure/group checks and the new old-versus-current invariant check pass. Family kinds remain 27 polarity / 9 binding; fitting/audit remain 96/48 with the same three fitting folds. Current facts text spans 93–761 characters, with six cases at least 700 characters; all read fields span 157–868. These replace the earlier version's text-length observations, not its unchanged labels or groups.

**The audit contains only nine disputed target propositions and ten disputed event labels overall across its 48 cases. Its disputed-recall gate is indicative only.** One target answer changes target recall by 11.1 percentage points. Report the original threshold and exact counts alongside observations; these few author-known cases cannot establish precise population recall. No split or label is altered to compensate for that limitation.

The [second-freeze summary](baselines/2026-11-07-alpha-03-case-refreeze/summary.md) records commands and hashes. Tools changed only for this bounded owner-authorised correction, history-aware freeze verification and LF preservation; the normal builder still refuses a frozen corpus. Files outside `tools/alpha-03/`: changed corpus text/metadata and the new revision history under `tests/alpha-03-cases/`, this report and the new summary. All production code, prompts, law data and original fact cases remain unchanged. Unit tests added/updated/skipped/weakened: none. Required post-commit Release build/full-test and branch push results are reported in the handoff.

Corpus inference, fitting, calibration, candidate runs and validation model checks remain **0**; hidden/evaluation access remains **none**. **Stopped after the owner-adjudicated second freeze, before any inference.**

## Stop 2 — fitting-stage authorisation and reference protocol (7 November 2026)

The owner verified the second freeze and authorised references first, then grouped fitting of scored-status/order/calibration ablations, a separate binding component only if the score gate passes, and one locked audit batch before any validation check. The frozen corpus remains unchanged at `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642`.

Two clarifications were settled before fitting: **a passing model may continue when the other fails a score/binding gate**; and **the audit runs both models once as one locked batch, with a primary model selected from fitting and recorded in this report before audit inference**. The audit gate applies only to that primary. The second column is informational and can never replace the primary after seeing audit results. Missing scores, cache/context failure or runtime/measurement preconditions still require a stop/report. No runtime-model switch is authorised by these experiments.

The reference host under `tools/alpha-03/FittingReferences/` directly calls unchanged Alpha-01, its provider/parser and existing evaluator. It selects exactly the 96 fitting manifest IDs before case deserialisation. Gemma3 is non-thinking and retains its original wire settings; gemma4 receives `Think = false`. Each model starts unloaded, then retains normal residence across cases. Context 4096, CPU-only, temperature 0.2, repeat penalty 1.1 and the 300-token reader cap remain unchanged. There are no per-case resets, retries or cap increases. Critical semantic errors and cap hits remain reference results; a technical/context/measurement precondition stops the batch before another model or candidate starts.

Before each request, a fresh resource sample must confirm mains, actual/effective/configured Best performance and idle-app residence. A conservative operational memory guard stops below **512 MiB machine available physical memory**, or when model private bytes plus app working set exceed visible physical RAM. This is a headroom/interruption rule, not a measurement of maximum feasible model allocation. It does not investigate the cause of the previously reported resident-gemma3 growth. Case times, native timings, raw requests/answers, per-kind summaries and simultaneous CPU/memory/paging/power samples are retained outside the repository.

The reference host built with zero warnings/errors before inference; build servers were shut down. Initial quiet preflight passed, then Windows Search became busy before any reference call. Under the owner's earlier explicit temporary-pause authorisation, its Running/Automatic state was recorded and the service stopped without changing startup settings; it must be restored when the measurement stage stops or finishes. Timed references began only after load settled, on mains and Best performance with the real app resident. Raw run directory: `%TEMP%\dzlegal-alpha03-fitting-20261107-211459`. Reference source files and binary were hashed before timing; no candidate prompt/reader code has been written while references run.

### Reference interruption — stop before candidate work

**The runtime precondition failed before request 26 of the gemma3 reference.** Only `a03-001`–`a03-025` received inference: 25 of 96 fitting cases, all polarity-kind, with **5/25 without a critical error**. The guard blocked `a03-026` before HTTP/native timing, so the raw evaluator has 26 attempts including one technical failure, while the partial semantic denominator is 25. Seventy-one planned cases received no inference. Gemma4, candidate prompts/readers, fitting/calibration/order ablations, binding, compatibility checks and audit did not run.

The fresh stop sample had **507.668 MiB** machine available physical memory, below the preregistered 512 MiB operational guard; power/app, monitor freshness and model-private-plus-app checks passed. Whole-window minimum **464.867 MiB**, sampled model/server working-set peak **7.499 GiB**, simultaneous model-plus-app **7.669 GiB**, private bytes **7.887 GiB**, commit **18.480 GiB**. The incomplete batch showed paging and some later browser CPU activity. It does not establish the fixed 16 GB capacity failure or qualify a complete quiet cost gate. Partial mean **20.662 s**, median **17.788**, p95 **29.851**, maximum **31.851**, including cold first load; these are informative partial observations.

The original evaluator recorded 23 decisive-event omissions across 17 completed cases, five forbidden affirmations across four, and one wrong returned polarity. Dense scoring exposes omitted denials: **18/18 denied labels became explicit absent**; disputed labels were 3/6 correct, false affirmations totalled five. Known-value omissions, requests, source coverage, error counts, incomplete folds, digests/licences, commands and hashes are in the [interrupted fitting-reference summary](baselines/2026-11-07-alpha-03-fitting-reference-interrupted/summary.md). No label/source changed from these answers and no grouped choice was fitted.

The model was unloaded, owned app closed normally, monitor stopped and **Windows Search restored Running/Automatic**. Best performance retained. The separate resident-gemma3 growth issue was not investigated. No cap increase, runner reset rescue, guard relaxation or model fallback followed. A bounded-batch residence protocol could be proposed to the owner, with cold loads/boundaries disclosed, but has not been authorised or run.

Files outside `tools/alpha-03/`: this report and the linked summary only. Production code, prompts, law data, original tests and frozen corpus unchanged. Tests added/updated/skipped/weakened: none. Required post-commit Release build/full-test and push results are reported in the handoff. **Stopped on runtime precondition, before any candidate/audit; original validation model checks remain 0, hidden/evaluation access none.**

## Owner-adjudicated v3: decisive flags and restarted measurement protocol

The owner accepted the runtime stop, corrected the authored corpus's blanket decisive default and authorised a restart. **V3 is frozen at `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`**, replacing v2 content hash `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642`. The v2 record is preserved in `tests/alpha-03-cases/revisions/freeze-v2.json`; the complete 548-event decision inventory and reasons are in `revisions/v3-decisiveness.json`. Case-file hash is `415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67`, split-manifest hash `1a9b132b481c7e3719f5f66b8f955c5aee56a2c16fc0e76b945520ef033e762f`.

The applied rule is `docs/HIDDEN-SET.md`'s **decisive when missing this fact would get the case wrong**, with the owner's explicit override retaining every family's contrast target. Distinct claim foundations, independent material grounds, remedy prerequisites and material procedural/prior-adjudication facts remain decisive. Redundant negative alternatives and ancillary history that does not ground a sought remedy do not. Polarity alone never determines decisiveness. The lease's performance/accrual start differs from formation; prior criminal rulings remain distinct adjudication facts; the indemnity-seeking lessee's no-arrears assertion is not duplicated by an affirmed payment event. These are author-applied engineering annotations, not independent legal validation.

Every dense event now has an explicit boolean: **422 true / 126 false**. All 428 non-absent legacy gold events have matching explicit flags: **395 true / 33 false**. All **108 contrast targets**, including 27 absent targets, remain decisive. Absent targets remain excluded from legacy gold events as before; their dense label and forbidden affirmation checks remain. The dense per-status and known-value/source measures keep all their labels and denominators: denied versus absent is still an error regardless of the decisive flag.

Every gold true-to-false change:

| Cases | Event(s) | Omission reason |
|---|---|---|
| a03-001–a03-008 | payment_made | Denied payment already carried by affirmed unpaid loan/debt |
| a03-009–a03-012 | payment_made | Denied payment already carried by affirmed unpaid sale price |
| a03-032 | payment_made | Historical payment; only annulment/damages sought, no restitution |
| a03-033–a03-036 | delivery_late, delivery_missing | Actual broken-goods breach is affirmed other, not either denied alternative |
| a03-037–a03-040 | delivery_missing | Actual nonconformity breach is affirmed other |
| a03-069–a03-072 | notice_served | Ancillary prior demand in a titleless-occupation file |
| a03-073–a03-076 | payment_made | Denied payment already carried by affirmed unpaid commercial price |

The 33 changed gold flags comprise 29 fitting events and four audit events; all other gold flags remain true. Explicit dense flags are newly written for all 548 events, so all 144 case objects receive the added field. The invariant checker proves that after removing only decisive fields, v2 and v3 case objects are identical: **no text, polarity, value, date, actor, source, citation, request, family, split or fold changes**. It also enforces the exact per-event inventory and target flags. Historical v2 text checks run against their exact committed snapshot; the new successor proof is separate. The normal builder still refuses a frozen corpus and now requires the explicit decision inventory.

Static results: existing harness **144 cases, no problem found**; strengthened dense checker **548 event labels, 368 request labels, 1,939 citation checks, zero problems**, built with zero warnings/errors; unchanged grouping and three folds; v3 payload/hash verification passes. The [v3 summary](baselines/2026-11-07-alpha-03-case-refreeze-v3/summary.md) records the correction and checks. Unit tests added/updated/skipped/weakened: none. Files outside `tools/alpha-03/`: corpus annotation/manifest/freeze/history/rule/review metadata, this report and that summary. Production code, original cases and reader prompts unchanged.

The interrupted 25-call v2 partial reference is **discarded from every comparison**, preserved only as history, with no v3 rescoring or selective continuation. Both models restart at case 1. For every remaining timed run, close browser/heavy apps, retain mains/Best performance and the 512 MiB guard, and unload/reload every ten cases. Record each load/warm-up separately from case time and also report the total amortised bill, including loads. This owner-adjudicated residence policy supersedes the previous single resident batch; it does not investigate the separate memory-growth cause. References still precede candidate implementation; fitting-only grouped comparisons, primary fixed before the locked two-model audit, and the stop before validation all stand.

V3 correction commit `b0757400aa69b78ca816a9c0c856d476cd05c4af`: required post-commit Release build **0 warnings/errors**; full suite **990 passed, 0 failed/skipped**, serial xUnit collections without changed assertions. The new reference host separately builds with zero warnings/errors. It pins v3, reloads before cases 1/11/.../91, saves exact empty warm-up/unload bodies and responses, native/wall load times and boundaries. Fresh monitor checks follow planned unload and warm-up, and the existing guard runs before each reader request. Failure records the exact sample/reason; no unscheduled reset or retry. Case clocks exclude those operations; the batch clock and supplemental aggregate retain their full amortised cost. The wrapper refuses browser/VS Code processes at start. No v3 model inference or candidate code has started at this protocol checkpoint.

Protocol commit `a6e95c3f6238d9ed057874ab4d7ac1f48540dd20`: solution/reference-host Release builds zero warnings/errors, all **990 tests passed, 0 skipped/failed**; both commits pushed. Fresh reference run `%TEMP%\dzlegal-alpha03-v3-fitting-20261107-215737` began at 20:58 UTC on mains/Best performance, idle real app resident, no browser/VS Code, build servers stopped and no concurrent test/build/model job. Initial physical headroom about 10.2 GiB, quiet preflight other CPU below 0.5 cores. Windows Search was idle and remains Running/Automatic; no service pause in this run. Task Manager closed normally; Telegram had no closeable window and remained a quiet background client. Empty warm-up wall times are recorded separately; its response may omit native `load_duration`, retained as unavailable rather than inferred. The interrupted partial is not read by the new prediction/analysis path. Candidate/audit/validation inference remains zero while references run.

### V3 references complete; closed-browser condition failed during gemma4

Both references completed all **96 fitting cases and ten planned loads**, without transport/context failures or retries. Independent audit verified **192 reader requests and 20 reloads**, exact fitting selection/order and full source text, zero wire mismatches. No audit case was sent. All 1,695 timed-window power samples verify mains and actual/effective/configured Best performance.

| Unchanged Alpha-01 | No critical error, all 96 | Polarity /72 | Binding /24 | Mean case seconds | Amortised seconds including loads |
|---|---:|---:|---:|---:|---:|
| gemma3:4b | 36 (37.5%) | 23 | 13 | 20.212 | 20.756 |
| gemma4:e4b, thinking off | 69 (71.9%) | 46 | 23 | **15.598, unqualified** | **16.647, unqualified** |

The full-log audit found **Brave active in 20 samples from 21:53:09 to 21:53:50 UTC**, during gemma4 cases `a03-114`–`a03-116` and reload 9. Other CPU peaked at **5.698 cores**, and available physical fell to **0.841 GiB**. The 512 MiB guard passed but the owner's closed-browser timing condition failed. **The start-only browser check was insufficient and I missed the reopening live. Final audit supersedes the sparse live status snapshots. Gemma4 timing/memory comparison is unqualified; stop before candidates.** No affected case is dropped or retried to make a quieter result.

Gemma3's sampled model/server working-set peak was **6.422 GiB**, private **6.898**, concurrent model-plus-app **6.581**, machine available minimum **4.269**. Other CPU mean/p95 **.384/.613 cores**, with a retained brief Windows servicing/WMI burst; no active browser observed. Gemma4's model/server peak was **7.214 GiB**, private **7.999**, concurrent model-plus-app **7.317**, with browser-confounded machine headroom. Neither model becomes the runtime model. Memory evidence is conditional on the owner's ten-case residence protocol, not unrestricted session stability.

Dense outcomes preserve the denied/absent distinction after the flag correction: statuses **275/372** versus **329/372**; non-affirmed errors **71/160** versus **35/160**; false affirmations **26** versus **16**; denied target recall **0/18** versus **9/18**; disputed target recall **5/18** versus **16/18**. Known event dates correct/omitted/wrong: gemma3 **130/25/11**, gemma4 **164/1/1** out of 166. Known request amounts correct/omitted/wrong: **77/6/4** versus **86/0/1** out of 87. The sole gemma3 cap (`a03-130`, 42.566 seconds) is retained with partial output; gemma4 had zero caps. The [complete reference summary](baselines/2026-11-07-alpha-03-fitting-references-v3/summary.md) lists every error category, both family kinds/folds, all loads, resources, licence/digest data, commands and artifact hashes.

The repaired guard checks browser/VS Code presence before every reader HTTP call and after each planned unload/warm-up, records the exact reason/process IDs/sample and stops. Five no-model end-to-end checks pass: clean resource fixture, 511 MiB rejection, battery rejection, an owned benign process-name fixture detecting reopening, and clean recovery. No actual browser behavior/network/model call or new legal case is used in these tests. Native fixtures exit normally. All production paths remain unchanged.

Models unloaded, owned app closed normally, monitor stopped; Windows Search remained **Running/Automatic**, never paused; Best performance retained. Frozen v3 payload re-verified. The separate resident-memory growth issue was not investigated. Measurement additions/repair and the declared finite grouped fitting protocol are under `tools/alpha-03/`; only this report and the new reference summary change outside it in this checkpoint. Unit tests changed/skipped/weakened: none; five standalone no-model guard checks added. Required post-commit builds/full tests and branch push are reported in the handoff.

**Stopped on the closed-browser precondition before any scored reader/prompt, fitting/calibration, binding, compatibility, audit or original validation inference.** Proposed owner decision: retain the completed gemma3 reference and restart all **96 gemma4 cases from case 1** under the repaired guard, excluding this unqualified batch from the eventual comparison while preserving its record. No selective rerun or use of the old v2 partial. No rerun is executed before the owner answers, as brief §8 requires for a failed precondition.

### Owner-authorised gemma4 repeat and continuation

The owner authorised **gemma4 only, all96 from case1**, under the repaired per-request browser guard, retaining the completed gemma3 reference. The owner also requires repeatability counts and explicitly retains **the first gemma4 run's accuracy**. Therefore the semantic references remain gemma3 **36/96** and first gemma4 **69/96**, including their original per-case facts. The new gemma4 run supplies qualified time/memory and repeatability only; its diagnostic accuracy cannot replace the original or select better per-case answers. The first run remains unchanged and read-only. This supersedes the earlier proposal to exclude its accuracy; its timing remains unqualified.

Repeatability definitions fixed before the rerun: exact decoded answer string, including whitespace/line endings; whole-case critical pass/fail verdict; additionally identical full error multiset and parsed facts. Report counts out of96, pass-to-fail/fail-to-pass, and both family kinds/folds, without treating repeats as independent cases. `compare_reference_repeat.py` checks matching names/order/input fingerprints and complete outputs, reads the first record without modification, and writes only to the new external run root. The reference wrapper/auditor now accept an explicit model selection; the reader/settings are unchanged. No candidate code is written before this reference completes.

After qualified references, continue the frozen fitting-only protocol, optional binding gated by score usefulness, fitting-selected primary recorded before the locked two-model audit, primary-only audit gate, and stop before validation. Prior stop rules and the ten-case reload/512 MiB/mains/Best-performance conditions remain. No fresh permission is required unless a stop rule fails.

Repeat protocol/tool commit `e54cd8b49ed00c02bc065d3546832b0dc570e763` passed required Release build0warnings/errors and all990tests0failed/skipped, then was pushed. New external root `%TEMP%\dzlegal-alpha03-gemma4-repeat-20261107-232428`; gemma4-only run begins under the repaired guard after quiet preflight (about11.4GiB available, other CPU<.5 cores), mains/Best performance, unchanged digest/runtime, idle app residence and build-server shutdown. WSearch remains Running/Automatic without a service pause. No old output is overwritten; no candidate code/inference has started at this checkpoint.

#### Repeat stopped at the repaired browser guard

**37/96 actual calls completed; request38 was blocked before HTTP.** At22:36:36UTC the guard detected three Firefox processes. It checks process presence, including background/idle processes; this record does not determine who opened a window or why they started. Power, app/monitor freshness and memory checks passed (about4.36GiB available at the failed check). The guard therefore worked as declared; no exemption, reset rescue or selective continuation followed.

Partial repeatability over all37 actual calls: **31/37 exact answer strings, 37/37 critical pass/fail verdicts, 36/37 full error multisets, 34/37 parsed facts**. Both prefixes are20/37 without critical error, with zero verdict transitions. All37 outgoing request bodies match the first run exactly. This is a completed-prefix diagnostic, not the requested full96 repeatability result. The blocked evaluation is excluded from matching: evaluator38 attempts includes one technical failure,58 unattempted, hence59 without inference. The first full gemma4 accuracy/facts **69/96 remain the semantic reference**; gemma3 **36/96** remains retained.

The [stopped-repeat summary](baselines/2026-11-07-alpha-03-gemma4-repeat-stopped/summary.md) records changes/matches, error/status/value/source counts, partial seconds/resources, failure identifiers and hashes. The comparator now has an explicit partial-diagnostic mode while its default still refuses incomplete runs. Old reference directories are untouched. Partial timing is informative only; no qualified96-case cost or memory result is claimed.

Model unloaded; owned app/monitor closed normally; Search remains Running/Automatic, Best performance retained; owner Firefox processes untouched. Frozen v3 payload unchanged. Files outside `tools/alpha-03/` in this checkpoint: this report and the linked summary only. Unit tests added/changed/skipped/weakened: none. Post-commit Release/full-test and push results are reported in the handoff. **Stopped under the failed-precondition rule before any candidate/fitting/audit/validation; wait for the owner's answer before another full96 attempt.**

#### Second full repeat authorised

The owner answered “Ready: restart all 96”. A new all96 gemma4-only run starts from case1, source `7539f762efde1ae994dd262f329c099ad7273011`, external root `%TEMP%\dzlegal-alpha03-gemma4-repeat2-20261107-235312`. The stopped prefix is retained separately and reused for no result. Mains, Best performance, closed browser/Code, idle app residence and the ten-case reload/512MiB guards remain required. Initial available memory about11.2GiB; recent background CPU .420 cores at preflight. An earlier .510-core preflight rejection sent no model requests; the wrapper started after the quiet check passed. Search remains Running/Automatic, with no pause. No candidate code or inference is started during reference timing. The first full gemma4 accuracy/facts69/96 remain the semantic reference; this run supplies qualified cost/memory and the full repeatability check if it completes.

#### Complete repeat and continuation (8 November locally)

**All96 completed**, UTC22:57:52–23:24:13 on7 November, ten planned loads. Full wire/source audit passes, all96 outgoing requests equal the first run, zero caps/transport/context failures, no browser occurrence/guard stop. All746 timed samples mains and **Best performance**. Mean case **15.448330 seconds**, median13.555914, p9524.809719, max25.701978; total with loads/waits1581.047867 seconds, **16.469249 per case**. Ten warm-ups mean7.322513 seconds, native load duration unavailable. Model/server WS/private **7.273197/7.998604GiB**, concurrent model+app7.424625, available minimum **4.063122GiB**. Background other CPU mean/p95 **.731/1.026 cores**, mainly Task Manager/desktop, above retained gemma3 .384/.613; power/browser/residence conditions pass but this is not a matched CPU-idle speedup experiment.

Full96 repeatability: **84 exact answer strings,94 same critical verdicts,92 same error multisets,90 identical parsed facts**. One fail-to-pass (`a03-006`), one pass-to-fail (`a03-036`). Repeat diagnostic score69/96 equals the first69/96 but the successful cases differ. **First gemma4 accuracy/facts stay the semantic baseline**, alongside retained gemma3; qualified gemma4 cost comes from this repeat. No selective answers or partial-prefix reuse. The [complete-repeat summary](baselines/2026-11-08-alpha-03-gemma4-repeat/summary.md) reports both kinds/folds, dense diagnostics, every error type/load, memory/power/background conditions, commands and hashes. All260 new,500 first and119 stopped artifact hashes independently verified; v3 freeze unchanged.

Model unloaded; owned app/monitor exited normally; Search Running/Automatic without a pause; Best performance retained. No candidate/prompt, fitting/calibration, binding, compatibility, audit or original validation inference yet. At this checkpoint files outside `tools/alpha-03/` are this report and the linked summary only; no unit tests added/changed/skipped/weakened. After the measurement-record commit and required Release/full-test checks, continue the already-authorised finite grouped fitting protocol on fitting families only. The repeat shows that small gains can reflect run variation; it does not authorise a new repeat/search grid or replacing the first reference. Primary remains to be selected from fitting before the locked audit; stop rules and stop-before-validation remain.

### Owner-requested safe pause before candidate inference

Complete-repeat commit `a8bf3fd77e26396e68e41be57162d76a324476fe` passed Release **0warnings/errors**, all **990 tests0failed/skipped**, and was pushed/remote verified. The owner then requested a safe pause and an estimate. **Paused; no candidate model request has been sent.** All96 reference results are retained; model unloaded, owned app/monitor exited. Search remains Running/Automatic; Best performance retained. Resume only when the owner asks.

The initial harness-only source checkpoint adds `ScoredStatusReader.cs`, one scored-status Arabic prompt, a fitting-only `ScoredFitting` collector and protocol tests using synthetic HTTP responses. It changes no production/app path, Alpha-01 code, existing prompt, provider, catalogue or frozen case. Scoring uses exact1–4 alternatives in top20 while forced literal0 carries the append-only history. Missing/duplicate/nonfinite/incorrect-byte scores, nonzero carrier, lost extension cache or context overflow stop; no fallback. The status-only application preserves baseline roles/bindings/requests and reports missing values/sources rather than creating them. Collector mode pins v3 and selects fitting inputs before reading them, never gold/annotations; raw requests/responses remain external, with the same ten-case reload and measurement guard. This is source implementation only: real capability on **both** models and the fitting score gate remain untested. Calibration/selection/binding/compatibility/audit work is not implemented or inferred yet.

On resume, verify the saved source/tests and frozen hash, then fresh mains/Best-performance/quiet/browser/app/monitor preflight and bounded first-fitting-case capability on both models, forward/reverse. No prompt/schema rescue after a failed precondition. Continue the declared grouped fitting/binding/primary-before-audit protocol only if gates pass. `tools/alpha-03/FITTING-RESUME.md` gives the exact checkpoint and next command. The planning estimate is roughly **6–10 more hours if gates pass**, including implementation/checks and serial fitting/binding/compatibility/audit; live capability failure could stop much sooner. No promise of audit readiness or unseen gain follows from the reference result.

Files outside `tools/alpha-03/` at this paused source checkpoint: this report, new `src/DzLegal.Harness/ScoredStatusReader.cs`, and new `tests/DzLegal.Law.Tests/ScoredStatusReaderTests.cs`. Tests cover score completeness/finiteness/bytes, exact neutral history, cache/context failures, preserved bindings/requests/no invented recovered values, tie order and endpoint/model restrictions; no existing test changed/skipped/weakened. Post-commit solution/collector builds, full tests and push are recorded in the handoff. No visual feature changed; the real idle app was run throughout reference timing; no new screenshots required. No original validation or audit inference, and no hidden material access.

### Resumed: actual scored-reader capability (8 November)

The owner resumed. The saved source commit **`11fb3b2473cf36347a41c0020d2aa896e03d2462`** passed solution/collector Release builds0warnings/errors and **1001 tests0failed/skipped**, then was pushed. Before any probe, v3 hash, model digests and Ollama0.40.15 were verified. The owner closed Brave manually; **all future browser closing is left to the owner**. No model request was sent while it remained open. Mains and **Best performance**, idle real app and quiet preflight passed.

**Both models pass the bounded actual-reader capability check.** First frozen fitting input `a03-001`, forward and reverse, yields **12/12 four finite byte-exact score positions and10/10 cache extensions per model**. Independent audit confirms all24 exact full prompts/source sentences, actual zero histories, native score vectors and settings. Max contexts gemma3723/gemma4711. Forward cached counts G3`0,560,592,620,648,678`, G4`0,563,593,619,645,673`; reverse G3`52,563,593,621,649,681`, G4`537,566,594,620,646,676`. First positions are separate from the extension gate. The large model difference in reverse-prefix reuse is measured behaviour, not proof of cache internals or an accuracy gain.

Single-case forward/reverse wall seconds G3 **14.026115/13.331288**, G4thinking-off **10.291134/3.848200**; empty warm-ups4.098604/8.879621 separately. These diagnostic times do not qualify the full-fitting or integrated-reader mean. All17/12 timed power samples mains/Best, memory guards pass; available minimum6.901161/4.521255GiB. No semantic accuracy scoring, calibration, prompt adjustment or model selection; bounded capability may still fail on a later fitting question. The one prompt and reader remain unchanged from11fb3b2. Full fitting/score/binding/compatibility/audit gates remain open.

The [capability summary](baselines/2026-11-08-alpha-03-scored-capability/summary.md) records source/settings/counters, licence/digests, conditions/resources and commands/hashes. Raw root `%TEMP%\dzlegal-alpha03-scored-capability-20261108-145017`, all89 artifact hashes verified. Model unloaded; owned app/monitor exited normally; Search Running/Automatic without change, Best performance retained. Old reference roots unchanged. New tools `audit_scored_collection.py` and `Measure-ScoredFitting.ps1` reproduce the independent checks and serial96-case collection without changing the reader/prompt. No tests changed/skipped/weakened. Outside `tools/alpha-03/`, only this report and the linked summary change in this checkpoint. Continue the already-authorised grouped fitting after required commit/build/full tests; no original validation or audit inference yet.

### Full scored collection: stop on gemma4's missing score

Source **`2daa20f84ba104a72017c8e034c7325f4aee14d5`**, solution/collector builds0warnings/errors and all1001tests passed/pushed. Fresh quiet preflight about.34 other CPU cores,11.25GiB available, mains and **Best performance**, owner-closed browser, idle real app. Both models/digests, single prompt and v3 freeze unchanged. Serial G3 then G4,96 fitting cases each, forward/reverse, reload every10 complete cases, same512MiB guard; no build/test/calibrator ran alongside timing.

**G3 completed96/96 cases,744/744 usable four-score positions,552/552 cache extensions andten loads. G4 failed the score precondition on request47**, `a03-004`, reverse question5 (`debt_acknowledged`): labels1/2/3 finite, **label4 absent from top20**. Neutral0,634 prompt/604 cached tokens and power/headroom valid. Actual log scores1−6.935579,2−10.099606,3−12.997162;4unknown. This is a score-access/capability failure, not a measured polarity accuracy failure. The bounded first-input pass did not establish all-position availability. No missing-score imputation, top-k/schema/prompt repair, retry or alternative runtime followed.

The **entire stage stops**, including offline scoring/calibration of completedG3. Permission for a passing model to proceed after the other's quality failure does not override the missing-score stop. G4 has only3 whole cases and partial4:47 actual calls,46 valid positions,1invalid,697 unrequested. All39 extensions reuse cache. The collector let InvalidDataException escape because it was missing from its catch filter and is not anIOException. Consequently its original G4completion file is absent; raw47 request/response pairs and completed rows are preserved, and supplementary observed-stop/diagnostic records disclose the crash rather than manufacturing completion.

G3 pair mean **28.738421 seconds**, median28.629251, p9536.961368,max37.569868; full collection2813.423862seconds (**29.306499/case including loads/waits**). Forward/reverse means14.645931/14.091814. Conditional component projections with retained Alpha-01 and one load bill35.425827fixed/49.518317averaged seconds, without binding; no integrated latency/quality qualification. G3 modelWS/private5.779316/6.162811GiB, concurrentapp5.960472, available minimum5.412941. G4 partial modelWS/private6.767456/7.494129, appsum6.920105, available minimum4.551441. All1328/43 timed samples mains/Best and guards pass. Paging spike disclosed in summary; no memory-growth cause investigated.

The [stopped-fitting summary](baselines/2026-11-08-alpha-03-scored-fitting-stopped/summary.md) gives complete G3 costs by order/kind/length, all loads/resources, G4partial evidence/coverage, exact commands and hashes. Raw root `%TEMP%\dzlegal-alpha03-scored-fitting-20261108-150244`, all1658 artifact hashes verified; first500/qualified-repeat260/capability89 hashes unchanged. First semantic references36/96 and69/96 remain retained. **No scored-reader accuracy, corpus calibration, selected parameters, primary, binding, compatibility, audit or original validation inference exists.** Every semantic error-type column is unmeasured, not zero.

Offline fitting/evaluation/selection source was prepared during collection but never executed on corpus data. Seven numerical fixtures validate its math/gates without legal cases. After the stop, only failure bookkeeping was repaired: explicit InvalidDataException handling with failed case/order/call, and wrapperfinally metadata/unload. A syntheticHTTP no-retry test passes; two mocked-native/mocked-HTTP wrapper failures pass with0realHTTP and no second model. Reader prediction logic, prompt, schema, top20 and missing-score rule unchanged. Solution, collector and offline evaluator builds0warnings/errors; new scored tests12/12 pass. Required post-commit full suite/push reported in handoff. Existing tests never changed/skipped/weakened.

Models unloaded; owned app/monitor exited normally; Search stayed Running/Automatic, Best performance retained; owner processes untouched, browser closing owner-only. Freeze unchanged. Outside `tools/alpha-03/` this checkpoint changes this report, the linked summary and `tests/DzLegal.Law.Tests/ScoredStatusReaderTests.cs` only. Await owner under brief§8. Recommended bounded next step, **not executed**, is owner-authorised offline evaluation of completedG3 using the fixed grid; that narrows only this diagnostic stage and does not waive the unresolved G4/final-audit requirements. Keeping both-model capability mandatory requires a new score-access design before any further probe. No automatic continuation or tuning from the failed case.

### Owner's revised freeze plan — fixed before offline results

The owner now authorises offline diagnosis of completed gemma3 records only, skips the gemma4 missing-label probe, order/correction tuning, binding and48-case audit for now, and independently fixes the candidate to **unchanged Alpha-01, gemma4:e4b, thinking off**. This supersedes the preceding stop for the offline diagnosis and this minimal harness freeze only. No new inference or evaluation-folder access is authorised to the agent. The candidate does not depend on the diagnostic result.

Before inspecting results, the diagnostic is fixed to **catalogue-forward scores, uncalibrated versus lambda0.1**, the middle of the previously declared grid. No grid/order comparison, winner selection, full-fitting refit or deployment of offsets. Three grouped manifest folds remain: train the same balanced boxed zero-sum correction on64 fitting cases and report its held-out32, pooling all96/372 dense events out of fold. Original Alpha-01 gemma3 roles/bindings/requests/facts remain the pinned semantic reference. Audit/validation labels do not enter diagnosis. The frozen candidate remains Alpha-01 itself; scored status code is experimental only.

### Offline result and owner-requested pause

The authorised offline diagnosis is complete, **with0new model inference**. Unchanged Alpha-01 gemma3 gives **36/96** critical-error-free cases, uncalibrated forward scores **21/96**, and fixed lambda0.1 correction **23/96**. False affirmations **26/63/46**. Polarity-family critical-free cases **23/72,12/72,14/72**; binding-family **13/24,9/24,9/24**. Dense correct statuses **275/372,188/372,197/372**. Denied recall0/47→3/47→10/47; disputed5/18→15/18→14/18, but disputed precision45.5%→11.3%→12.1%; absent84/95→6/95→17/95. Scored reading overproduces disputed facts and loses absence discrimination. The small correction does not restore Alpha-01. No scored arm/offset selected or full-fit refit performed.

The [complete offline report](baselines/2026-11-08-alpha-03-gemma3-offline/summary.md) includes status recall/precision and false affirmations by kind, all three folds/offsets, every error category, omissions/coverage, existing cost projection, reproduction and hashes. Raw diagnostic root `%TEMP%\dzlegal-alpha03-gemma3-offline-20261108-163515`. Exact first baseline errors reproduced through unchanged official scorer before replacements. No reverse/order tuning, gemma4 missing-label probe, binding,48-case audit or original validation evaluation. The owner's independent choice remains unchanged Alpha-01 gemma4 thinking off.

The owner required the same blind wrapper for both future runs: fixed case path passed only to harness, outside-repo output, no wrapper reads/listing/copying of case files/answers/per-case lines, console only counts and guard events. Implementation is saved: optional `facts --alpha --think true|false` passes the local-model override and records it only when explicit; unset Gemma defaults and existing Qwen thinking default are preserved. Original Alpha-01 reader/parser/prompt/catalogue/provider/options are untouched. A separate opt-in `--measurement-root` harness mode uses the existing guard source and performs ten-case reloads, private case logging/scoring, model-digest checks and sampled timing/memory summary internally. It prints only counts/guard events. `Run-OwnerAlpha01.ps1` delegates the fixed case path, reads only resource metadata, redirects stderr without reading it, and never opens case/answer files. It requires an already-open idle desktop app, mains/Best performance, closed browsers/VSCode and fresh local output outside repo/case folder. **The wrapper has not been invoked by the agent.**

Nine focused mock tests pass for streaming `think` wire/defaults/CPU/context/cap, local-only override, reload1/11/21, guard/transport stopping without retry, and console privacy. Wrapper syntax parsed only. CLI smoke used an empty agent-owned temporary directory: valid flag exit0 with0model calls; invalid flag exit2 before attempting nonexistent input. Solution and existing measurement tools compile0warnings/errors. The owner then requested a safe pause. Save/check/push this checkpoint; **do not issue final freeze commands or continue until the owner resumes**. Final wrapper/privacy review, exact freeze documentation/hashes and the two owner commands remain for resumption. No evaluation folder or results accessed; no new corpus edits, app changes or screenshots required. Post-commit full tests/push are reported in the handoff.

Outside the brief's harness/tools areas this checkpoint changes this report, the linked offline summary, and new `tests/DzLegal.Law.Tests/HarnessThinkingTests.cs` and `FactRunMeasurementTests.cs`. Existing tests unchanged/skipped/weakened: none. This is a safe source checkpoint, not a claim that the frozen candidate passed independent evaluation.

### Final freeze under the owner's revised plan

**Decision: freeze unchanged Alpha-01 on gemma4:e4b with `think: false`.** The owner selected this candidate before the offline results. The baseline remains unchanged Alpha-01 on gemma3:4b, with the thinking option omitted. No scored-reader arm or calibration offsets enter the candidate, and the app's runtime model does not change. The owner explicitly deferred the missing-label probe, order/correction tuning, binding component, 48-case audit and original validation checks; this handoff does not claim those gates or the hidden-v2 pass mark have passed.

The completed gemma3 fitting-fold diagnosis is **36 / 21 / 23 critical-error-free cases out of 96**, respectively Alpha-01, uncalibrated forward scores and the fixed lambda 0.1 global correction. False affirmations are **26 / 63 / 46**. The correction recovers only two whole cases relative to uncalibrated scores and remains below Alpha-01, with more false affirmations. Polarity-family results are 23/72, 12/72 and 14/72; binding-family results 13/24, 9/24 and 9/24. The [offline summary](baselines/2026-11-08-alpha-03-gemma3-offline/summary.md) preserves every status class, error type, fold, offset and hash. No new inference, selection or full-fitting refit was performed.

Freeze review compared the original reader, CaseFacts, IModelProvider, Ollama provider/options, AlphaFacts, FactsEvaluator, element catalogue and Alpha-01 prompt against **task-start commit `c86048dd8bab2b889c5164530624b8354f51afa1`**: all unchanged. The only ordinary CLI addition is an optional local-model `--think true|false`; omission preserves existing Gemma and Qwen defaults. Its wire test verifies the override, streaming, CPU-only context 4096, temperature 0.2, repeat penalty 1.1 and unchanged 300-token cap. The separately opted-in measurement mode and shared owner wrapper preserve ten-case unload/reloads, separate load times, 512 MiB headroom, resident idle app, mains/Best performance, context and browser guards. They stop without retries and retain partial records privately. Neither wrapper nor monitor reads case files, answers or per-case lines; only the harness does. Wrapper syntax and source were reviewed without invoking it.

The accepted existing mains/Best-performance cost evidence is unchanged: gemma3 mean case 20.211819 s / amortised 20.756463 s, model working-set/private peaks 6.422150/6.897877 GiB; qualified gemma4 thinking-off repeat mean 15.448330 s / amortised 16.469249 s, model working-set/private peaks 7.273197/7.998604 GiB and concurrent model+app working-set peak 7.424625 GiB. Sampling can miss short peaks. First-run semantic references remain 36/96 and 69/96; the gemma4 repeat's cost does not replace its first-run accuracy. See the [reference summary](baselines/2026-11-07-alpha-03-fitting-references-v3/summary.md) and [qualified-repeat summary](baselines/2026-11-08-alpha-03-gemma4-repeat/summary.md) for all errors, timings, repeatability and conditions. No new battery timings are used.

Installed model identities remain gemma3 digest `c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291` (Gemma Terms; 3,349,593,375 bytes) and gemma4 digest `dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c` (Apache 2.0; 6,583,656,505 bytes). The harness pins these digests; the run manifest captures the installed Ollama version (previously measured 0.40.15). No weights are bundled or downloaded. Corpus v3 remains `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`, reverified without edits.

The [owner instructions](../tools/alpha-03/OWNER-RUN.md) contain the two exact PowerShell commands for the same wrapper, serial once-only execution on mains/Best performance and fresh output under `%LOCALAPPDATA%\DzLegalAssistant\alpha-03-owner-runs`. The agent has not run those commands, opened/listed/copied hidden v2, or accessed any evaluation outputs. The wrapper's sole case-consuming child is the harness on the fixed owner-supplied path. Its console exposes progress counts and guard events only; stderr is redirected unread to the private output folder. Source fingerprints and the exact task-changed files outside `src/DzLegal.Harness` / `tools/alpha-03` are recorded in [OWNER-FREEZE.json](../tools/alpha-03/OWNER-FREEZE.json), using LF-normalised SHA-256 for text. Its 39 source fingerprints and 38 outside-area paths have manifest hash `8575f4a93960ab1561a6167c94af21625eb6e231a506a3b68768a3fe8157c07c` under that convention. The final pushed commit identifies the complete checkout; the manifest excludes itself and changing reports to avoid a circular hash.

The owner authorised the final source review, build and full tests on battery (AC disconnected, 87% at review). Earlier full-suite attempts ended when the owner requested the pause; they are not recorded as successful. New tests remain the six thinking/default wire cases, three measurement/privacy/failure fixtures and the previously reported scored protocol tests; no existing test was changed, skipped or weakened. Required final post-commit build/full-test outcomes and the exact freeze commit are reported in the final handoff. No app/UI behaviour changes or new screenshots are involved; the feature smoke was the ordinary CLI with an empty owned input folder and mocked HTTP requests, never a model benchmark. Uncertainty remains the unseen-set accuracy, per-run variation, sampled rather than continuous memory peaks and the deferred audit/validation gates. Stop after handoff; the owner runs hidden v2.
