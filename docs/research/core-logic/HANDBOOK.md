# Handbook: the core logic of DZ Legal Assistant

*Written by Fable 5.2 on 12 and 13 November 2026, and revised by Fable 5.2 on 14 November 2026, for the agent that carries out the work (Opus 5.5). You have not seen the conversations in which this was planned. Everything you need is in this file and the files it links to. Read this file to the end before you do anything; then read the brief ([brief/FABLE-CORE-LOGIC-BRIEF.md](brief/FABLE-CORE-LOGIC-BRIEF.md)).*

**What the revision changed.** The first handbook put the legal rules first and the notes reader last, and gave Codex many roles. On 14 November the owner changed both: **the reader comes first**, the high-harm rules run beside it, the law clean-up comes before the freeze; and **Codex keeps exactly three roles** (section 4). The work is now ordered in phases (section 7), not in steps 1 to 6. The step numbers survive as names: step 1 is the rule records, step 2 the texts, step 3 the notes, step 4 the reader, step 5 the question whether a generative model is needed, step 6 the freeze.

**State of things at this revision (14 November 2026, late evening).**

- Step 2's first pass is done: the article store, the Official Journal scan from 2003 to 10 November 2026, the seed inventory, and the triage of the Civil Code, the CPCA and Law 22-13 ([reports/step-2a.md](reports/step-2a.md)).
- **One module, `formal-notice`, has been through the whole rule pipeline** (`rules/final/formal-notice.json`). It was the pipeline's first real test and found about fifteen faults in the first handbook, all fixed in the scripts, the prompts and this revision ([reports/step-1-tracking.md](reports/step-1-tracking.md), [reports/handbook-check-2.md](reports/handbook-check-2.md)).
- **One run of that module is prepared and not launched.** It is your first task (phase 0).
- Nothing of step 3, 4, 5 or 6 exists yet beyond the contract and the specifications.

**The brief and the owner's later decisions.** The brief is the owner's statement of the task. Where the owner's decisions (section 2) differ from it, **the decisions win**, and a later decision wins over an earlier one. The known differences from the brief: hidden set v3 has about 1,100 notes; the builder launches the v3 commands that print counts, and only the scoring run is the owner's; "a forfeiture blocks export" means the clean export only; a grade is given per item of a record, not per JSON field.

## 0. How to use this handbook

| If you need | Read |
|---|---|
| what the task is and why it is built this way | sections 1, 5 and 5b |
| what the owner decided and what is forbidden | sections 2 and 3 |
| which model plays which role | section 4 |
| what to do next, with commands, "done when" and a decision table | section 7 |
| when you must stop and ask the owner | section 8 |
| where you are after a stop | [STATE-FORMAT.md](STATE-FORMAT.md), then `C:\Users\TheShadow\dz-core-research\STATE.md` |
| the format of a rule record | [rules/rule-record.schema.json](rules/rule-record.schema.json), [rules/DERIVER-GUIDE.md](rules/DERIVER-GUIDE.md), section 6, [rules/examples/](rules/examples/README.md) |
| how sure a rule is, and what it may do | [rules/GRADES.md](rules/GRADES.md) |
| how one unit goes through its stages | [specs/unit-driver.md](specs/unit-driver.md), [specs/record-checks.md](specs/record-checks.md) |
| how the checks are themselves tested | [specs/planted-errors.md](specs/planted-errors.md) |
| the missing articles and the corrections | [law/README.md](law/README.md), [law/law-delta.schema.json](law/law-delta.schema.json) |
| what a note's labels are, and their frozen list | [notes/SCHEME.md](notes/SCHEME.md), [notes/LABELLING-GUIDE.md](notes/LABELLING-GUIDE.md), [notes/note.schema.json](notes/note.schema.json) |
| how a later slot reaches the reader | [specs/scheme-extension.md](specs/scheme-extension.md) |
| who may write, filter and train on which notes | [notes/TERMS.md](notes/TERMS.md), [specs/notes-pipeline.md](specs/notes-pipeline.md) |
| the reader's architecture, recipes and experiments | [specs/reader.md](specs/reader.md), [specs/scorer.md](specs/scorer.md) |
| the questions the app asks the lawyer | [slots.json](slots.json) |
| the exact prompt of an agent or a Codex call | [prompts/README.md](prompts/README.md) |
| what a script must do | [specs/](specs/): `article-store`, `official-journal-scan`, `record-checks`, `rule-evaluator`, `unit-driver`, `planted-errors`, `codex-calls`, `scenario-sampler`, `notes-pipeline`, `probes`, `scorer`, `reader`, `scheme-extension`, `step5-templates`, `hidden-v3-commands`, `v2-adapter` |
| what the reviewer will integrate, and how | [INTEGRATION-CONTRACT.md](INTEGRATION-CONTRACT.md) |
| the code that already exists | `research/core-logic/README.md`, `research/core-logic/scripts/README.md` |

**When the handbook is silent or wrong.** First look in the decision table of the phase you are in (section 7): it lists the situations that were foreseen, each with what to do. If your situation is not there: (1) go back to the reason given for the step and choose what serves that reason; (2) choose the option that protects the lawyer and the hidden sets; (3) write what you decided, and why, in the state file and in your next report, and add the row to the decision table in the same commit. If the choice touches a hard rule of section 3 or a stop point of section 8, do not choose: ask the owner. **Fix the handbook when you find it wrong**, in the same commit as the work that showed it, and say so in the report.

## 1. The goal, and the one fact that shapes everything

The app drafts an Algerian statement of claim (عريضة افتتاح دعوى) on an ordinary laptop, offline. The architecture report found that what can hurt a lawyer is not the prose: it is **the decisions**: the claim type, the articles, the deadlines, the steps required before filing, the competent court and section. Those are rules, not text generation. And Part 7 measured that no ready-made model under 1 GB reads a lawyer's notes well enough.

So this work builds the app's **core logic**:

1. **A reader** that turns a lawyer's note into the facts the rules need, showing the words behind each answer (steps 3 and 4). It comes first because everything else in the app depends on it and nothing measured so far does it well enough.
2. **Rules** for the 21 claim types: complete, machine-readable, each resting on the official text (steps 1 and 2). The rules that can hurt most (deadlines, steps before filing, the court) come first.
3. A measured answer to: how much of the petition can templates write, is a generative model still needed, and can the app work under 1 GB? (step 5)
4. A freeze from which every figure can be rebuilt (step 6).

**The one fact: there is no human reviewer.** No lawyer will check a rule or a note. A wrong deadline that looks right would reach a lawyer's desk. Therefore nothing here may rest on a model's say-so, including yours. Every product carries its own proof, and the proof is of three kinds, used together everywhere:

- **The official text**, quoted by a script, never typed by a model.
- **Independence**: two derivations that did not see each other; a writer and a checker of different model families wherever the owner's decisions allow one; a hidden set written from another plan.
- **Attack**: an agent whose only job is to break the result, and planted errors that show whether the checkers can find anything.

And one rule for when proof runs out: **say so, and choose the reading that protects the lawyer.** Warn rather than stay silent; ask rather than guess; never block on a reading.

Each piece of the method exists because, without it, a specific error would pass unseen. Section 5 lists those pieces with the error each one stops. **When you are tempted to skip one to save time, read its line there first.**

## 2. The owner's decisions

The owner is the product owner and developer. A reviewer (another Claude) writes briefs and integrates; the owner relays. These decisions are binding. Words in quotation marks are the owner's. **A later decision replaces an earlier one where they differ; the replaced parts are marked.**

### 12 November 2026

**Money.** "No API and no spending at all: the hard rule is $0. Do not use OpenRouter or any paid service. Use what is already on this laptop, under existing subscriptions."

**Codex and Claude** (replaced in part on 14 November, evening). Codex CLI (`codex exec`) writes notes, "about 20 notes per call, in an empty scratch folder outside every repository with a read-only sandbox". "Record the writer and judge of every note, so the training notes can be replaced later if needed." "Respect Codex's usage limits: work in batches across the day and report progress; start with 4,000 training notes and show me the learning curve at 1,000, 2,000 and 4,000 before generating more."

**Training.** "Laptop first, never rent." If training time becomes the bottleneck, a free GPU only (Kaggle Notebooks, or Google Colab's free tier). "Upload only the fictitious notes and the code, never anything from a hidden set, and no payment details. Ask me before creating any account."

**Hidden set v3.** Writer: isolated Claude subagents, used only for this, write v3 from their own plan straight into `C:\dz-hidden-v3` and return counts only ("no note, no label, no excerpt in its reply"). "Do not use Gemma or Jais as a v3 judge." Size: about 1,100 notes (40 per claim type; 200 near-miss; about 60 more for the two rarest slots; every slot at least 100 times; at least 150 denied and 150 disputed events). "Every slot" is read as: every slot the reader reads from the note, with a value other than "unknown". (The relabeller is now Opus: 14 November, evening.)

**Clean notes.** "Yes, 1,000 clean notes. First write 20 with each of Jais-3-3.7B and Gemma 4 E4B and keep the better writer of formal legal Arabic. If the classifier trained on the clean notes comes close to the one trained on Codex notes, plan for the shipped classifier to be trained on clean notes only, and keep the Codex notes for development, judging and testing."

**The Official Journal scan** from 2003 to today, approved with four additions: search the code names as well as the law numbers with "modifié", "complété", "abrogé"; "be polite to joradp.dz: respect its robots.txt, one download at a time with a pause of a few seconds, and resume where you stopped rather than restart"; "keep a list of every issue read (number, date, hash, matches found) as evidence, and delete the PDFs after reading"; "the check must re-find the known amendments (Laws 05-02, 05-10, 07-05, 22-13); if it misses one, the scan is not trusted and you say so."

**Disagreements between writer and checker.** "Blank the field, keep the note, in training notes only, with three limits": drop the whole note if they disagree on the claim type or on more than about a fifth of its fields; report the rate of blanked fields per slot and per polarity; write targeted notes to refill those cases. "In every test set (development, the mirror test, hidden v3), drop the whole note on any disagreement."

**Depth of step 1.** One unit per claim type.

**The six changes made when the first plan was approved:** (1) Fable writes the handbook and stops; **you carry out the work**. (2) A model is named for every role (section 4). (3) The integration contract is defined ([INTEGRATION-CONTRACT.md](INTEGRATION-CONTRACT.md)). (4) **"Block" means what it means in the app today:** the clean export is blocked and the export with the «مسودة» mark stays allowed. Never a hard stop. (5) The optional run of the finished reader on hidden v2 counts only through the unchanged Alpha-01 scorer ([specs/v2-adapter.md](specs/v2-adapter.md)); otherwise the comparison is dropped. (6) Notes composed by code from short phrases: marked, capped at a quarter of any training set, never in a test set, and the reader is measured with and without them.

### 13 November 2026

1. **No folder locks.** "Do not change the permissions of C:\dz-hidden-v2, and do not lock C:\dz-hidden-v3 either." The protection for both hidden sets is the no-open rule, Codex's empty scratch folder with a read-only sandbox, and the Codex event-log check.
2. **The grade rule.** "A rule resting on an unverified text must still warn, and is also marked "to check"; it can never block the clean export and never be silent. "To check" is never weaker than a warning."
3. **Codex safeguards.** "Run the read probe at the start of every day's batch, and make no real call that day unless the probe is rejected by the check"; "keep Codex CLI at 0.167.0 for the whole task; if it updates itself, re-run both probes before any real call and tell me"; "never use anything from a discarded call, not even part of its output."

### 14 November 2026, during the first module

1. **Law 91-29 art. 73** («أو التي» against «والتي») was settled from the page and the French edition; the library correction is in the delta. **Law 90-04 art. 19 ¶2:** the printed Arabic is kept and the probable misprint recorded; any rule relying on that paragraph is grade C with both readings (`law/text-disputes.json`).
2. **The texts given to derivation C and the boundary writer:** "give derivation C and the boundary-case writer every chapter the module's seed inventory touches (never A's or B's records), and note in the report how many cites fell outside the chapters given."
3. **Tracking:** "track the A-B agreement rate before reconciliation for every module, and state for each module whether a deriver changed its own examples after running the tool; the blind boundary cases stay the test that counts."
4. **The boundary runner,** for "ask" and "unsettled" cases: "at least one matched item must give the expected response, AND no matched item may contradict it (no block or stronger effect, and no answer that asserts the opposite)."
5. **A disputed boundary case** gets the second family's blind view; "if Codex sides with the boundary writer on any of them, that case becomes uncertain (both readings, warn) rather than settled by Opus alone". Failed cases that two families hold wrong stay **unedited and counted as failures, with the two-family reason**.
6. **Civil Code 120:** the paragraph-division correction is in the delta, with the three page readings as proof.
7. **A prior step** "may say "done" only when the means is a listed one and the notice came before the petition day; otherwise it stays open". **"The lawyer must never see "done" beside an open question about the same notice."** This is a rule for every prior step, not only for the formal notice.
8. **The API keys:** "make the removal automatic rather than a prompt instruction: every agent and every Codex call starts in a shell where both keys are already unset (one wrapper used by all launches), with a test that the wrapper's environment has neither key."

### 14 November 2026, evening: the new order and Codex's three roles

1. **The order, by impact on the software:** "the reader of the lawyer's notes comes first, the high-harm rules in parallel, the law clean-up later." Steps 3 and 4 start now, on a label scheme frozen now. Step 1 runs beside them, "high-harm modules first (limitation and periods with CPCA 504, labour conciliation, courts and sections and exits from the ordinary courts), then claim types". Step 5 once a few claim types are final. Deferred until before the freeze: step 2's second pass, text passes beyond what a record needs, the remaining Journal triage, the Legal Review answers, "with rules on unverified text staying grade D ("to check", never block) until then". Hidden v3 and the freeze last.
2. **Codex has three roles, and no other:**
   - "Note writer: the pilot, the training notes, and development and challenge notes alongside Claude's (20 notes per call, 2 to 4 calls in parallel, each in its own empty scratch folder, read-only sandbox, event-log check)."
   - "Second family for checks: the support check, the blind view on disputed boundary cases, and the second page readings where no other two-family or mechanical proof exists, in the background. No attack on every record."
   - "Training backup only: training is run by Opus on the laptop. If Opus cannot get a training run working, ask me; with my approval, Codex may write and run the training code in its own separate folder (not read-only for that job), using only the training notes, never near C:\dz-hidden*, any repository other than its own copy, or a hidden set's results."
3. **Codex's speed:** "Today's figures already pass": about 10 Codex minutes per unit, in the background. The local models are tried as a second view "only if Codex fails".
4. **Terms.** "Codex writes the training notes, on my own reading that a small offline classifier for Algerian legal notes does not compete with OpenAI. I take responsibility for that reading." The owner's ChatGPT plan is a personal plan. **OpenAI's terms were not quoted, because OpenAI's page refused the fetch.** "Keep the writer of every note recorded, so the Codex notes can be replaced if ever needed." ([notes/TERMS.md](notes/TERMS.md))
5. **The filter on training notes is not Claude:** "code checks and a local model (Gemma 4 E4B or Jais-3-3.7B) decide keep or drop; Opus only measures label noise on a sample and stops to show me if it passes 10 %."
6. **Claude never writes training notes:** "Claude (Opus or Sonnet) writes test notes, the mirror set and hidden v3, never training notes, unless Anthropic authorizes it."
7. **What may tune the reader:** "anything that changes the reader (encoder choice, early stopping, temperature scaling, thresholds, head selection) is tuned only on the Codex half of dev and on clean notes. The Sonnet half is used only to measure and report, never to choose or tune. Same for the challenge set."
8. **Blocking.** A rule may block the clean export "only when the text was proved mechanically or by two families, the value is matched by script, derivations A and B agreed, no attack is open, and the planted-error round caught that kind of error 90 % of the time; otherwise the rule warns. If Codex passes the speed trial, a Codex check counts as the second family for these conditions." (Of the trial the owner then said: "Today's figures already pass"; so a Codex check counts.)
9. **Party kinds.** The owner confirmed the plan's reading: the app's relation and plaintiff-position lists and each event's actor, added to the reader's output as an optional part. ([notes/SCHEME.md](notes/SCHEME.md), section 3, says exactly which pairs that covers, and which values of the app's lists it never outputs.)
10. **The laptop** writes and filters notes every night on mains, and by day whenever the owner says it is free.
11. **Process.** Light checks after each commit, the full CI before every push; one driver for a unit's stages; the owner's questions bundled per batch of units (since 15 November: product and process questions only; questions of legal reading go to `reports/open-legal-questions.md`, below).

### 15 November 2026, after question batch 1 (the owner's words, essentials)

1. **The owner's standing rule of 15 November 2026:** the owner is not the legal reviewer, and there is none. A question of legal reading (which reading of the law is right) is **never sent to the owner**: the record keeps the protective behaviour it has (it warns, keeps both readings, never blocks), the item stays at grade C or D, and the question is recorded in `reports/open-legal-questions.md` with its readings and what each would change (`unit.py questions <unit>`). The owner receives only decisions about the product, the process, the budget, tools, data or the stop points.
2. Batch 1: keep today's behaviour for all 13 questions (o1 to o14) and move them to `open-legal-questions.md`.
3. bc1: add a condition form that compares a date slot with another date (the petition date and other date slots) to the rule language and the evaluator, with tests, **before the `periods` module starts**; then run bc1 again.
4. The 11 `oneFamily` items of formal-notice: no third support check now; Codex's support check reads them in the planted-error round, before any of them could reach grade A ([specs/planted-errors.md](specs/planted-errors.md)).
5. The executor's four unforeseen choices of phase 0 (the known-limits file, bc1 decided with the support answers, Codex's view on bc7 before follow-up 3, the renamed support ids) and its two prompt edits are accepted.
6. **15 November 2026, evening (two decisions):** (a) "stripping the 100 identical quote and source fields by script, after checking them against the store, was the right choice; record it in the report" (court-sections' verifier had carried them over unchanged from the quoted copy). (b) **Forfeitures whose readings differ on the last day:** "where the readings differ on the last day, the record must not block on the earlier reading. Block only once the latest defensible last day has passed, warn on the days in between, and state both readings with their sources (including whether CPCA art. 405 applies to the Law 90-04 deadline). Apply the same rule to every forfeiture in the other modules." The evaluator does it through a deadline's `otherCountings` ([specs/rule-evaluator.md](specs/rule-evaluator.md), "Deadlines"); every module with a forfeiture gets an `owner` entry in a follow-up.

### 16 November 2026, after the filter trial (the owner's words)

Gemma 4 E4B took about 38 seconds a note on mains (the plan had assumed 10 to 15), so filtering every training note would take about 40 hours.

1. "Gemma will not check every training note. No 40-hour filtering."
2. "Finish tonight's trial, the 20-note Opus check and the Jais sample, as planned; they are short."
3. "Filtering from now on: the code checks on every note (seconds), then the reader flags the suspicious notes itself (train with cross-validation; flag the notes where it confidently disagrees with the writer's label), and only the flagged notes go to Opus, or to Gemma if the 20-note check shows it reliable on those fields."
4. "Do not filter the 499 pilot notes with Gemma. Use them, as they passed the code checks, to train a first rough reader now, and score it on development notes it never saw (never the hidden sets). The first score comes before the 4,000 notes are written."
5. "The pilot report's calendar uses this route."

6. **The same evening, on the flagged notes and the first score:** "Approved: the 100 development notes as you describe them, and the training script." (50 written by Codex and relabelled blind by Sonnet, 50 written by Sonnet and relabelled blind by Opus, a note dropped on any disagreement, scores shown per writer.) "Flagged notes: option (a). A flagged note is dropped, never judged or corrected by a Claude model; if many are dropped, Codex writes replacements for their scenarios. Opus only checks a sample of the flags to measure how many were right. My 14 November rule stands unchanged."

7. **17 November 2026, after the first score (the owner's words):** "Two instructions; finish the noise measurement first, then:"
   1. "Before the pilot report and before the replacement batch, run one experiment that decides the route (overnight on mains is fine): Fine-tune the encoder itself, not only heads on its frozen vectors, on the 493 pilot notes; same dev notes, same settings rule, three seeds if time allows, otherwise one and say so. Floors beside it: the logistic regression on the encoder vectors, and a plain word and character n-gram classifier on the note text. Dates and amounts with their role: score the cue table and the parsers alone (no learning), and a hybrid where cues decide and the model fills only what the cues leave open. Report claim type, outside, polarity, requests and date/amount roles for each, per writer. The pilot report's calendar uses the winning route. The Sonnet gap suggests the reader learned Codex's style: in the 4,000 training notes, widen the style variety (more style cards, several Codex prompt variants, shorter and messier notes)."
   2. "For the 21 claim types, full checking only where a rule can block. A field that can only warn (grades B, C, D) gets one derivation pair, the reconciliation and one attack. No third derivation, no Codex blind view, no follow-up run. Whatever stays uncertain is written as "open, warn" and the work moves on. At most one follow-up run per unit. Teach the wording check to ignore «اليومين» when it compares two last days."
8. **17 November 2026, evening (the owner's words):** "Keep the Opus adversary as the one attack for warn-only claim types; it searches the official sources, which is what found the real changes in the law. No Sonnet attack for those." And, with 3 to 4 hours on mains:
   1. "Run the fine-tuning with one seed per setting (2 runs), and say so in the report. If the result is within the spread of the n-gram classifier's 0.663, say so and propose the remaining 4 seeds for a later night."
   2. "In parallel, since they use no local CPU: apply your nine proposed guide rules (approved), run the guide test on the touched items, and send the queued retries and refills."
   3. "Then complete the pilot report and stop. If anything would run past 4 hours, stop it cleanly as in a safe pause, push, and tell me."
9. **17 November 2026, at the pilot's stop point (the owner's words):** "Yes, with these details:"
   1. "Go on to the 4,000 training notes with the wider style and guide 1.3. Add the sampler's coherence rule, with its test, before the first call (no khul or tatliq beside a custody claim whose earlier divorce is stated)."
   2. "No extra seeds now. Decide the route at the 1,000-note point of the learning curve instead: train the n-gram classifier, the fine-tuned encoder (small setting only, three seeds, one evening on mains), and a combination where the n-grams decide claim type and outside and the fine-tuned model the rest. Report every line per writer."
   3. "Scorer: fix the evidence check so a fact written twice may be quoted at either place, as guide 1.3 allows. Version it (scorer 1.1) and report the pilot's evidence figure under both versions. No answer's right or wrong changes."
   4. "Run the guide test once more with Codex as the tester (one call), so a belief the two Claude testers share is checked by another family."
10. **17 November 2026, evening (the owner's words):** "Run the training alongside the noise check; they use different resources. If the noise check comes back over 10%, the training results of that night are set aside and the stop point applies as usual."
11. **18 November 2026, the leaner plan (the owner's words, with the answers given the same afternoon):** "Continue, with a leaner plan from the owner. If any cut below would lower the quality of what ships, say exactly how before applying it."
   1. "Route: approved. Keep both routes and add the composite (each line from the route that wins it)."
   2. Training notes: the stop rule at 2,000 was dropped on the executor's warning (a 2-point gain is inside the noise of a 62-note dev set): "Drop the stop rule: write all 4,000 training notes; at about 2,000 a day it is only a day and a half of free Codex. Finish the dev set to about 300 kept as well (Sonnet half now, Codex half in the windows after the Civil Code fetch and the training notes), and compare the routes at 4,000 on that dev set, paired by note." "One noise check, at 2,000, not one per 1,000."
   3. "Dev set: about 300 kept notes." "Challenge set: not cut entirely. Keep a small one for polarity only (negation, restriction, partial admission, contested facts): about 100 kept notes, written and relabelled like the dev set. The other difficulties stay cut." "Clean notes from a local model: cut."
   4. "Claim types, since Claude's limit is now the bottleneck: no full derivation for the remaining 17. For each, compare the app's current catalogue (data/law/claims) with the official text and the final modules by script, and derive only what is missing or wrong, on the light route. The remaining modules: only those a claim type needs." Answer on blocking: the few items that would block get a second, independent derivation (only those items), so they can still block. contract-rescission, whose two derivations were done, finishes on the full route.
   5. "The missing Civil Code articles (165 to 173, the 324 مكرر group): fetch them in the next Codex window, before more training notes, since six units wait on them."
   6. "Hidden v3: about 300 notes. Step 5: deferred."
   7. "Kept: planted-error round 1, the thresholds, the reader in .NET, the freeze."

This replaces the local filter on every note (section 2's decision 4 of 14 November, and [specs/notes-pipeline.md](specs/notes-pipeline.md), section 4a) as the route for training notes. The filter trial's result is still reported.

### 15 November 2026, at the start of phase 0

1. **What a Claude model may write:** "you, a Claude model, may write what the training notes are made from (the sampler, the style cards, the prompts, the labelling guide): that is ordinary software work, and the text the reader trains on is written by Codex or a local model".
2. **What may change the generation:** "only findings on Codex-written or locally written notes may change the writer prompt, the style cards or the sampler."
3. **The budget** of section 9 (about 375 Opus, 550 Sonnet, 520 Codex calls, 17 laptop nights) is accepted as the working budget. "More than 8 filter nights still comes to me at the pilot."
4. **A filled prompt's paths:** "prefer refilling it by the filling script with the correct paths; if it must be a hand edit, record it in the ledger with the exact before and after."

**The owner watches usage.** Before any fan-out of agents or Codex calls, say how many runs it is (section 9 has the budget). Prefer the lean way when two ways serve the purpose equally.

## 3. Hard rules

Breaking one of these is not a judgment call. If a rule stops you, stop and tell the owner.

**Hidden sets.**
- Never open, list or search `C:\dz-hidden*` (v1, v2, the future `C:\dz-hidden-v3`), their results folders, or the zip files of the same name. Never read a transcript of an agent that wrote hidden notes. Only `summary.md` leaves a hidden run, through the owner's command.
- Nothing is tuned on v1, v2 or v3, ever.
- **No folder is locked.** The access rights of the hidden folders are never changed. You and all your agents run under the owner's account, so nothing technical stops you: the rule is the protection.
- Never open `dz-eval`, `dz-fixed`, `%TEMP%\dzlegal-*`, or the untracked files of the main folder `C:\Users\TheShadow\dz-legal-assistant`.

**Money, keys, accounts.**
- $0. No paid API, service or rented machine. No new account without asking.
- **The two API keys.** `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` are Windows user variables, so every process inherits them. `research/core-logic/scripts/clean_env.py` removes them from every script that imports it (all of them do), from every Codex call, and from a shell command run as `python clean_env.py <command>`. Agents started by the Agent tool inherit the session's environment, which no script can change: **the session itself is started through the wrapper, and no agent is launched from a session whose key check does not print `clean`** (section 7, "Every session": the owner's rule is that the removal is automatic, not a prompt instruction). The prompts' own line "remove both keys before your first command" stays as a second safeguard. Never print, log or commit a key.
- The owner's name and email address never go into a prompt, a search, a request header or a file sent anywhere.

**Who may write what (terms).** Training notes are written by Codex, by the local models, or by code. **A Claude model never writes a training note, never labels one, and never decides whether one is kept.** Claude writes and relabels test sets, and measures. Every note records its writer, its filter and its relabellers ([notes/TERMS.md](notes/TERMS.md)).

**What may tune the reader.** A choice that changes the reader (the encoder, an epoch count, a temperature, a threshold, which head or rung is kept) is made only on notes written by Codex or by a local model: the Codex half of dev, the Codex half of the challenge set, and the clean-dev notes. Notes written by Sonnet (the other halves, the mirror set, hidden v3) only measure. The loader enforces this ([specs/notes-pipeline.md](specs/notes-pipeline.md), "The loader").

**Tools and downloads that are allowed.** Anything not on this list: ask the owner first.
- Already on the laptop: .NET 10; Python 3.12 with pytest (`C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`); the Python environment `C:\Users\TheShadow\dz-arch-research\venv` (torch 2.15.0+cpu, transformers 5.22.2, onnxruntime 1.31.1, onnx 1.23.2, scikit-learn 1.9.1, numpy 2.5.4), left from Part 7, used as it is; `pdftotext` and `pdftoppm`; Windows PowerShell 5.1 (PowerShell 7 is **not** installed: never write `pwsh`); Codex CLI 0.167.0; Ollama with `gemma4:e4b`, `jais3-3.7b-low`, `jais3-0.9b-low`, `hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M` and `gemma3:4b`.
- **The encoders step 4 starts from, and no other:** the two original pre-trained models, each from its official page, in the revision named here (both Apache 2.0, read on 13 November 2026): `CAMeL-Lab/bert-base-arabic-camelbert-mix`, revision `9be352797bdf28a9ae21e2ae582aaaca7abdb22d`; and `CAMeL-Lab/bert-base-arabic-camelbert-msa`, revision `277069fd3645fedb22b746caf38d111aadee0241`. For each download record the size and SHA-256 of every file, the revision, the licence link and the date, and put the files in `C:\Users\TheShadow\dz-core-research\models\pretrained\`. No account is needed; if the page asks for one, stop and ask the owner.
- **Never start from a Part 7 fine-tuned copy.** Those were fine-tuned on notes written by Claude models and are not for anything that ships.
- Downloads: issues of the Official Journal from `joradp.dz`, through the one fetching script; the two encoders above; the NuGet packages `Microsoft.ML.OnnxRuntime` 1.31.1 and `Microsoft.ML.Tokenizers` 3.0.0 (MIT); a small pure-Python package from PyPI when a script needs one, installed into an environment of your own under `C:\Users\TheShadow\dz-core-research\`, never into Part 7's (record its name, version and licence in the report).

**Branches and folders.**
- Work only in the worktree `C:\Users\TheShadow\dz-fable-core`, on `fable/core-logic`. Push only to `fable/core-logic`. Never merge, rebase or force-push; never commit to another branch.
- **Create and edit files only under `docs/research/core-logic/` and `research/core-logic/`; never change a file outside them.** Outside them, nothing: not the app, `src/`, `data/`, `tests/`, the harness, Codex's corpus or its branch `codex/law-corpus` (read it with `git show 9b60990:<path>`).
- Working files that are not products (the store, page images, queues, logs, the state file, the note sets while they are being built) live outside every repository, under `C:\Users\TheShadow\dz-core-research\`.
- **Model files are never committed.** Weights and ONNX files live in `C:\Users\TheShadow\dz-core-research\models\`; the repository holds their hashes, their sizes and the commands that rebuild them.
- **Note sets are not committed** (the repository is public). They live in `C:\Users\TheShadow\dz-core-research\notes-work\sets\<set>.jsonl`, and the repository holds their counts, statistics and SHA-256. Ask the owner before committing any set.
- Never name a folder `bench…` (the repository ignores such folders).

**After every commit: the light checks.** `PY research\core-logic\scripts\check_commit.py` (`PY` is the Python of section 7, "Every session"; about a minute): the commit touches nothing outside the two folders; pytest passes; and, when the commit touches the research code, its tests, a schema or an example, `research\core-logic\CoreLogic.sln` builds with 0 warnings and its tests pass. A failing check is fixed in a new commit; a failing head is never pushed. **Before every push: the full CI**, on a CPU as idle as you can get: the root solution's build and tests, the research solution's, pytest, and one build of both solutions with SDK 10.0.115. **That build is the one named exception to the two-folders rule** (the owner's standing practice): in the worktree's root `global.json` set `sdk.version` to `10.0.115` and `rollForward` to `disable`, build both solutions, then `git checkout -- global.json`, and check that `git status` shows it unchanged. It is never committed. Never delete, skip or weaken a test; never suppress a warning. Small commits, clear messages, ending with the `Co-Authored-By:` line of the model that made them. Agents run a published copy of the rules tool, so a build does not disturb them ([specs/unit-driver.md](specs/unit-driver.md)); nothing is built, tested or committed while a training or a timing runs.

**Data.** Fictitious only: no real person, company or case. Notes are **formal Arabic only** (no Darija, no French, no mixed text).

**Measurements.** Time and memory are measured only on mains power, Windows power mode Best performance, CPU idle. Otherwise the figure is not reported.

**Codex.** Every call follows [specs/codex-calls.md](specs/codex-calls.md): its own empty scratch folder outside every repository, read-only sandbox, `--ephemeral`, an output schema, and **the event-log rule**: a call in which Codex ran a command or opened a file is thrown away whole. Note writing runs 2 to 4 calls in parallel; every other Codex call goes through one background queue. The daily read probe and the pinned CLI version of 13 November stay. A usage-limit refusal stops the batch, never loops. Codex is used in its three roles only.

**The app's safety behaviour** (export blockers, AI-authorship marks, review before export, the recovery copy) is not yours to touch. No rule may have an effect stronger than `blockCleanExport`, and no rule that fires may be shown as less than a warning.

**A built-in safety check** may refuse an action. Never work around such a refusal; tell the owner.

**Legal honesty.** Every record keeps `"verified": false`. Never write, in a record, a report or a message for the lawyer, that a rule was verified by a lawyer or is certain. Every legal statement has its official source and date; every claim about a model has its link and date. This handbook's own article numbers (section 6) are pointers, not statements of the law.

## 4. Who does what

| Role | Model | Why this one |
|---|---|---|
| **You: the executor.** All code, scripts and tests; training the reader; orchestration; reports | Opus 5.5 | |
| Derivation A; reconciler; verifier of attacks; adversary with web access; settling a difference between two page readings; sorting of dropped notes | Opus 5.5 (fresh agents) | the roles that need the deepest reading of a text |
| Derivation B; **third derivation of a module, by the flat route**; **attack on the finished record**; boundary-case writer; Official Journal triage; first page reading; checking short Arabic strings (a new slot's question, step 5's template sentences) | Sonnet 5.5 (fresh agents) | a different model from A; the record is Opus's, so Sonnet attacks it |
| **Note writer:** the pilot, the training notes, half of dev, half of the challenge set, the polarity families | **Codex** | the owner's decision of 14 November |
| **Second family for checks:** the support check; the blind view on disputed boundary cases; a second page reading where no other two-family or mechanical proof exists | **Codex** | the owner's decision of 14 November |
| Filter of training notes (keep, blank or drop) | code, then a local model: the better of Gemma 4 E4B and Jais-3-3.7B at the filter trial (for the clean notes: the local model that did not write them, if it passed the trial) | never Claude (terms) |
| Label-noise measurement on training notes | Opus 5.5 (no tool) | measures only; changes no note |
| The other half of dev and of the challenge set; planners and writers of the mirror set and hidden v3 | Sonnet 5.5 (fresh; isolated for the mirror set and v3) | Claude writes test sets only |
| Blind relabelling: Codex-written test notes; the clean dev and test splits; the probe pool, once | Sonnet 5.5 (no tool) | two families |
| Blind relabelling: Sonnet-written test notes; the mirror set and hidden v3 | Opus 5.5 (no tool for the open sets; for the mirror set and hidden v3 an isolated agent that reads one file and writes one) | one family: see section 5b |
| Third blind judge of the mirror set | Sonnet 5.5, by the closed question-sheet route | differs from the relabeller by model and by route |
| Legal Review answers | Opus 5.5 and Sonnet 5.5, independently; each then attacks the other's answer | two answers by two models |
| Guide test (and the scheme's 75 cell items); ranking of the two clean writers; the two judges of step 5 | Sonnet 5.5 and Opus 5.5, independently | |
| Realism review (test sets only, never a training set) | Sonnet 5.5 for notes written by Codex or a local model; Opus 5.5 for notes written by Sonnet | a model that did not write the note |
| Clean notes, and the phrases of the composed notes | the better writer of Jais-3-3.7B and Gemma 4 E4B (local) | the owner's decision of 12 November |
| Petition sections in step 5 | templates in code, Jais-3-0.9B, Qwen3.5-2B, Jais-3-3.7B, Gemma 4 E4B (local) | the candidates Part 7 measured |
| Rebuild from the freeze document (step 6) | Sonnet 5.5 | if the lighter model can follow it, it is clear enough |
| Training backup, **only with the owner's approval** | Codex, in its own folder | the owner's decision of 14 November ([specs/codex-calls.md](specs/codex-calls.md), "The training backup") |

**Codex does not:** derive, attack a record, write a checklist, relabel, judge, or touch anything hidden.

Seven rules about agents:

1. **Fresh, never forked.** An agent that must be independent is started with the Agent tool, the model named above, and the prompt template **unchanged** with its placeholders filled by `prompts.py`. A fork carries your context, and your context holds what the agent must not see.
2. **You orchestrate; you do not take a role that must be independent of you.** You never "finish" a derivation, "fix" a label, map a free-text answer onto a record by hand, or "complete" a v3 batch yourself. A failed run is run again by a fresh agent. **A file an agent wrote is never rewritten by a script or by you**: a change to a record goes through a verifier run.
3. **Absolute paths.** An agent's shell may start in another folder, on another branch. Every prompt for an agent that reads or writes files opens by naming the worktree and says where the agent may read and write.
4. **Allow-lists.** A prompt says which paths the agent may read. Everything else is off limits, and the prompts name the tempting places. Every fresh agent is handed the user's memory index and `AGENTS.md` automatically; the prompts forbid following them.
5. **Judges use no tool.** A relabeller, a guide tester, a realism reviewer, the noise measurer, the sorter of dropped notes, a ranker or a page reader gets its inputs in its prompt file (or as the listed image files) and answers in its reply. Reading that one file is the only tool call it may make. `records.py ingest-reply` copies the answer from the agent's transcript and checks that; a run with any other tool use is discarded and run again. **One exception:** the isolated relabellers of the mirror set and of hidden v3 read one file, write one file and reply with two integers, and their transcripts are never read ([specs/hidden-v3-commands.md](specs/hidden-v3-commands.md)).
6. **Everything an agent must produce is described in the prompt's body.** The text above the line `--- PROMPT ---` is for you; the agent never sees it. Three runs were lost on 14 November because an output shape sat only in a header. `tests/test_prompts.py` checks each prompt whose answer has a schema.
7. **A known bias, to state wherever a mirror or v3 score appears:** the mirror set and hidden v3 are written by Sonnet and relabelled by Opus, both Claude models. Section 5b says what that weakens.

## 5. What each safeguard is for

Each line names the error that would pass unseen without it. **(review)** marks what a reviewer's attack on the first plan forced; **(first module)** marks what `formal-notice` taught on 14 November.

| # | Safeguard | The weakness it fixes |
|---|---|---|
| 1 | **A rule may cite only a text that is in the store**, and a text not yet verified keeps its rule at grade D ("to check", never blocking). | A rule derived from a wording nobody checked is wrong however carefully it is reasoned. (The first handbook verified every text before any rule; the new order defers the re-readings and lets the grade carry the doubt.) |
| 2 | **Agents point, a script quotes** (review): a record names article, version, paragraph; the script inserts the words. Whole paragraphs only. | A model misquotes, or "quotes" from memory a text that says something else. |
| 3 | **The exception-neighbour flag**: an uncited paragraph that opens an exception next to a cited one must be answered. | The rule is cited and its exception, one paragraph down, is silently dropped. |
| 4 | **Numbers are read from the quoted text by script** and must equal the field. | "Ten days" in the field beside "fifteen" in the text; limitation written as forfeiture. |
| 5 | **Agreement between the corpus and the library counts only where their sources differ** (review); the "critical use" rule of [specs/article-store.md](specs/article-store.md). | Comparing a text with its own copy proves nothing. |
| 6 | **A missing article is read twice, by two families where no mechanical proof exists, a third reading settles, and every number is checked against the French edition** (review). | One reader's plausible misreading of a scan. |
| 7 | **Amendments are searched by machine** in every Official Journal issue since 2003; the scan is not trusted if it misses a known amendment. | Relying on models' memory of what was amended. |
| 8 | **Two derivations by different routes** (A from the Arabic text; B from the case and the French edition), and **a third, flat derivation of each module** from the fixed texts only, compared on its mechanical fields. | Two agents reading the same article in the same order make the same mistake. A module's error is copied into many records. |
| 9 | **Checklists written from memory before any text is read**, merged, and answered line by line. | What nobody found in the texts leaves no trace unless it was written down first as a question. |
| 10 | **One rule evaluator, and comparison case by case**: both derivations' examples and blind boundary cases run through both records. | Two records with the same fields and different conditions look equal in a field comparison. Derivers write examples their own record passes. |
| 11 | **Reconciliation from the text, or "uncertain"**; the comparison report is committed before the reconciler runs. | A reconciler picks the better-argued side, or smooths a difference to raise the agreement figure. |
| 12 | **Source tiers**: Official Journal page; official consolidated text; everything else is a lead. | Confident, outdated statements of Algerian law on the web taken as law. |
| 13 | **Confidence is computed, with an effect** (review): grades A to D from checkable facts; only grade A may block, under the owner's conditions of 14 November. | A model's "high confidence" means nothing; a block resting on a reading would harm the lawyer; a silent rule would too. |
| 14 | **Seeded false pairs in every support check, and planted errors in finished records, run early.** Planted errors are now the main proof that the checks catch mistakes ([specs/planted-errors.md](specs/planted-errors.md)). | A checker that says "fine" to everything looks exactly like a checker with nothing to find. |
| 15 | **A polarity table for each of the 75 elements, frozen before any volume** (review), and **the guide test**. | Without it, labellers disagree because the guide is silent, and their "agreement" measures nothing. |
| 16 | **Code decides the case, a model writes the wording**: the sampler, with style cards drawn independently of the labels. | A model asked for notes writes one tidy note a thousand times and chooses the facts itself. |
| 17 | **A pilot of 500 notes before volume.** | Committing weeks of budget to a pipeline whose drop rate and cost were never measured. |
| 18 | **Dropped notes are kept, counted per polarity, sorted by cause, and refilled.** | Dropping disagreements silently removes the hard cases the reader most needs. |
| 19 | **The mirror set**, built exactly like v3 but open, with a third blind judge; **it chooses nothing** and is scored once. | Nobody could otherwise say how many wrong labels survive in v3. |
| 20 | **Splits by writing call and scenario family; a loader that refuses test notes and refuses Sonnet-written notes for tuning; near-copy and leakage probes.** | The same note, slightly changed, in training and test; choices made on the test writer's manner. |
| 21 | **The event-log rule** for Codex, and its Claude twin: a judge run with any tool use is discarded. | A read-only sandbox stops Codex from writing, not from reading. |
| 22 | **The scorer first; three verdicts with intervals; the worst test set as the headline; a commitment before the hidden run; at most two candidates.** | A yardstick written after the model bends to it; a hidden set used as a tuning set. |
| 23 | **Cheap experiments first on a laptop without a GPU** (review); every score on the 8-bit model. | Nights of training spent before knowing they are needed. |
| 24 | **The .NET tokenizer is the reference; training uses the numbers it produces** (review). | Python and .NET tokenise one word differently and every highlighted span is shifted. |
| 25 | **Step 5's main measure is mechanical** (review). | Model judges prefer fluent prose, including fluent inventions. |
| 26 | **The freeze is rebuilt by a lighter model in a clean clone.** | A freeze document only its author can follow. |
| 27 | **Derivers read a neutral guide, not the files that hold answers**; allow-lists; absolute paths. | The grade table and the slot list's help texts stated things the derivers were supposed to find. |
| 28 | **A proof label on every result** (`twoFamilies`, `mechanical`, `oneFamily`). | A reader of a report cannot otherwise tell a check made by two families from one made inside one. |
| 29 | **Known-truth probes for the relabeller**: two code-composed notes, their labels confirmed by a blind Sonnet relabel, mixed blind into every relabel batch of the mirror set and of hidden v3. | The relabeller's own error rate would otherwise be unknown. |
| 30 | **The local filter is tested with decoys and planted contradictions, and Opus measures the label noise it leaves.** | A small local model that answers "yes" to everything would keep every wrong note. |
| 31 | **One driver for a unit's stages; the verifier receives every kind of finding at once** (first module). | Stages run by hand were skipped or run out of order; the verifier had to be run three times. |
| 32 | **"Done" never beside an open question** about the same step (first module, owner's rule). | A lawyer reads "notice: done" and misses the warning that its means is doubtful. |
| 33 | **Every prompt's output shape is in its body** (first module). | Three runs produced unusable output because the format was only in the header. |

## 5b. Where the proof is weaker than the first handbook's, and what compensates

The first handbook used a second model family against every model-made product. The owner's decision of 14 November keeps Codex for the closed checks and the note writing, and moves the roles of the table below to Claude models. Say what follows plainly **wherever a result of these roles appears** (a report table, a grade summary, the freeze document):

1. **The rules.** Derivations A, B and C, the attack, the adversary and the verifier are all Claude models. A legal belief they share (for example a French-law habit that Algerian law does not have) is invisible to all of them. Planted errors cannot measure this: nobody can plant what nobody knows. What still guards a rule: the quotes and the values come from scripts; **Codex's support check still reads every statement against its text**; the adversary must bring official sources; the Journal triage brings special laws as leads.
2. **The mirror set and hidden v3.** Their writer (Sonnet) and relabeller (Opus) are both Claude models, and **a case of these sets is its writer's own** (the owner's decision of 12 November: "from their own plan"), not made by code. So nothing outside the family checks one of their labels: a label error both models make survives. A note is kept only when the writer's labels and the blind relabel are equal on every field.
3. **The Sonnet-written halves of dev and of the challenge set** are relabelled by Opus: one family again, on sets that only measure. There the scenario is made by code, so a note is kept only when the code-made labels and the blind relabel are equal: one voice outside the family remains.

4. **The Journal triage, the settlements of page readings, the Legal Review answers, the guide test and the cells table** are made by Claude models only. The triage decides what the amendment ledger holds, and a grade A needs a clean ledger.
5. **The relabellers of the mirror set and of hidden v3 cannot be watched.** Nothing technical stops one from opening the writers' labels, and the builder never reads a transcript of an agent that saw hidden notes ([specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), section 1, says what narrows this risk).

**What each role that left Codex lost, and what stands in for it:**

| Role in the first handbook (Codex) | Now | What is lost | What compensates |
|---|---|---|---|
| Third derivation of a module | Sonnet, by the flat route | a second family's reading of the module | another route (texts only, closed values, a script compares); Codex's support check |
| Checklist from memory before each claim type | cut | a third memory | the two derivers' lists, the catalogue's questions, the triage leads |
| Attack on every finished record | Sonnet | a second family's close reading | Codex's support check of every statement; planted errors |
| The cross-check sample (roles reversed) | the two halves of dev and of the challenge set | nothing: the halves measure the same thing on the sets that count | per-writer scores |
| Blind relabel of the mirror set and of hidden v3 | Opus | any check of those labels outside the Claude family | the probes; the mirror's third judge by another route |
| Third judge of the mirror set | Sonnet, by closed question sheets | a judge of another model than the writer | another route; said beside the figure |
| Second judge of step 5 | Opus | a second family's taste | the faithfulness script decides; the lower judge counts |
| Attack on the Legal Review answers | each answer attacked by the other Claude model | a second family | cites checked by script; "no text" said where no text settles |
| Guide test, realism review, ranking of the clean writers | Sonnet and Opus | a second family's reading of the guide and of the notes | the code-made scenario for labels; the filter trial's planted contradictions |

**Compensations, each with its own specification:**

- **Mechanical checks, which no model decides:** quotes inserted by script from the store; numbers and sanction words read from the quoted text; the neighbour flags; the store's refusal of a cite; the boundary runner; the code checks of a note (every date, amount, quote and name); the parsers; step 5's faithfulness script.
- **Swapped routes:** where the same family checks its own work, it does so by another road: the third derivation from flat texts against derivations made with the store and the tool; the attack on a rendered record without tools against a verifier with them; the third judge's closed sheets against the relabeller's free labelling; each Legal Review answer attacked by the other model.

- **Planted errors, early and counted per kind** ([specs/planted-errors.md](specs/planted-errors.md)). Round 1 runs as soon as `formal-notice` and the four high-harm modules are final; round 2 on six claim types. A rule may block only if its kinds of error were caught 90 % of the time.
- **Known-truth probes** ([specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), "Probes for the relabeller"): two code-composed notes in every batch Opus relabels. Their labels are certain; a batch under 95 % on them is relabelled; they are never scored as test notes.
- **The mirror set's third judge, by another route** (the same file, section 7b): a fresh Sonnet answers closed question sheets about every kept mirror note. Its figure is a floor on the wrong labels that survive, from inside one family, and is said to be one.
- **The proof label** ([specs/article-store.md](specs/article-store.md), [rules/GRADES.md](rules/GRADES.md)): each verified text, each graded item and each report table says `twoFamilies`, `mechanical` or `oneFamily`.
- **Per-writer scores**: every dev and challenge score is shown for the Codex half and for the Sonnet half.

## 6. The units, and a rule record in brief

**33 units:** the 21 claim types of `data/law/claims/*.json`, and 12 shared modules written once and copied into the claim records by script.

**The 21 claim types:** `loan-repayment`, `unpaid-sale-price`, `contract-rescission`, `sale-annulment-fraud`, `contract-damages`, `tort-damages`, `liability-for-things`, `unpaid-rent`, `eviction-occupier-without-title`, `commercial-debt`, `commercial-lease-eviction`, `sale-non-delivery`, `divorce-by-husband`, `tatliq`, `khul`, `nafaqa`, `custody`, `custody-forfeiture`, `visitation`, `abusive-dismissal`, `unpaid-wages`. Their areas, labels, descriptions and elements are in `data/law/claims/` and `data/law/claim-elements.json` (read; never change).

**The 12 modules.** Article numbers in the last column are **pointers to start from** (those the brief names, and a few from memory marked "?"): the derivers find the texts in the store, and nothing here is a statement of the law. **High-harm** modules come first (section 7).

| Module id | What it must answer | Where to start |
|---|---|---|
| `court-sections` (high-harm) | Which section of the court hears which matter; what happens when a court has no sections or a case is listed before the wrong one. | CPCA 32, 423?, 500, 512, 515, 531?, 536 مكرر |
| `territorial-jurisdiction` | Which court by place, for each kind of matter; the special rules (real property, family, labour, contracts, torts); whether the rule is of public order. | CPCA 37 to 40, 426, 501, 45 to 47? |
| `ordinary-court-exits` (high-harm) | Which cases leave the ordinary courts: administrative courts; the specialised commercial courts; the criminal judge; and procedures that replace a claim (order to pay). What tells them apart. | CPCA 800, 801, 536 مكرر and following, 306? |
| `periods` (high-harm) | Limitation and forfeiture: the general periods, **how a period is counted**, what starts, suspends and interrupts it, who may raise it, whether the court raises it of its own motion; the counting of procedural periods. | Civil Code 308 to 322 (308, 309, 312, 321 named by the brief); CPCA 405, 67 to 69; and CPCA 504, the time limit of the labour claim, as the first period these rules must count (its own deadline item is written in `labour-prelitigation`, which is reconciled after `periods` is final and reads it) |
| `formal-notice` (done) | When a formal notice is required before a claim or before damages, its form and means, when it is not needed, and the effect of its absence. | Civil Code 119, 179, 180, 181 |
| `labour-prelitigation` (high-harm) | Conciliation before a labour claim, its exceptions, the record, the time limit to file and its sanction, the section and the court. | Law 90-04 arts. 19 and following (and its first articles), 36, 37; CPCA 500 to 510 |
| `land-registry-publication` | When a petition about real property must be published at the land registry, how, and the sanction. | CPCA 17, 519? |
| `conciliation-mediation` | Conciliation and mediation offered or required by the court; the reconciliation attempts in family matters; the conciliation required before the specialised commercial court. | CPCA 536 مكرر 4, 439 and following?, 990 and following?; Family Code 49? |
| `petition-formal-elements` | The mandatory elements of the petition and what their absence costs; standing, interest; fees. (Feeds the 22 Legal Review questions.) | CPCA 13, 14, 15, 17, 426 |
| `commercial-lease-regimes` | The two regimes of commercial leases (before and after the 2005 reform): which contracts fall under which, by what date; notice to quit, its form and period; renewal, refusal, eviction compensation. | Commercial Code 169 to 202, 173, 187 مكرر; Law 05-02 |
| `proof` | Who must prove what; when writing is required and above what sum; freedom of proof in commercial matters; the force of an official and of a private document; admission, oath. | Civil Code 323 and following; Commercial Code 30? |
| `accessory-requests` | Damages for delay and their conditions, interest, the penalty payment, costs, provisional execution: the text behind each accessory request. | Civil Code 182, 186, 174?, 175?; CPCA 419, 323, 625? |

Things to know about the modules:

- **A lead on the court rules, from the corpus report:** Law 22-13 (art. 13) is recorded there as delaying its new court rules (CPCA 32, 33, 531, 536 مكرر, 536 مكرر 4, 800, 801) until the new courts are installed. `court-sections` and `ordinary-court-exits` must read that text and answer it in their checklist: a rule that sends a case to a court that does not exist yet would harm the lawyer.
- **Leads the triage recorded** (`law/journal-scan/triage-other-wave.json`, `leads`): free-zone employment (labour), missing persons (family), the maintenance fund (nafaqa), the new Code of Criminal Procedure (exits). The driver hands each to the adversary of the unit it names.
- **Notes the store carries.** Whenever the store gives a cited article an `inForceNote`, or more than one wording, the record must answer it in its checklist.
- **Modules made of statements.** `court-sections`, `proof`, `petition-formal-elements` and `accessory-requests` consist mostly of statements and requests. A claim record imports those by id.
- If a derivation shows that a module should be split, merged or added, do it, record why in the state file and tell the owner in the report. The number 12 is a plan, not a rule.

**A rule record in brief** (the schema describes every field; [rules/DERIVER-GUIDE.md](rules/DERIVER-GUIDE.md) explains it for derivers):

- `basis`: statements. Each is a **pointer** (`cite`: code, article, paragraph numbers counted from 1, optionally a version) and a short `says`. A script adds `quote`, `source`, `grading`.
- `elements`, `requests`, `court` (the section rule, the territory rules, the exits), `deadlines`, `priorSteps`, `documents`, `boundaries`.
- **Conditions** test `slots` ([slots.json](slots.json)); an unknown slot makes a condition unknown, and the rule asks. A deriver that needs a new slot declares it in `proposedSlots`.
- `examples`, `openPoints`, `checklist`. A claim record `imports` blocks of modules; it never restates a module's rule.

Folders under `rules/`: `derivations/a|b/` (as the agents wrote them), `derivations/a|b/quoted/` (the scripts' copies with quotes), `derivations/c/` (the flat third derivations of modules, as returned), `boundary-cases/`, `reports/`, `reconciled/`, `final-candidates/` (the verifier's corrected records), `final/`. Only `final/` is a product; the rest is the proof, and is committed too.

## 7. The work, in phases

One executor session runs two tracks by day. Codex writes notes in the background. The laptop filters notes and trains at night and on free days.

| Phase | Reader track (steps 3 and 4) | Rules track (step 1) |
|---|---|---|
| **0** | Close `formal-notice`. Retool: the light checks, the driver, Codex's pool, the grade rule. | – |
| **A** | Scheme 1.0 frozen; scorer; parsers and the reader's front end; sampler; the pilot and the filter trial. **Stop point.** | The four high-harm modules |
| **B** | Dev and challenge sets; training notes to 4,000; the clean-writer trial and the composed notes; the first rungs; the learning curve. **Stop point.** | Planted-error round 1; the other 7 modules; the 21 claim types; planted-error round 2 after the sixth |
| **C** | Fine-tuning if needed; clean notes; calibration; the .NET reader | Step 5, once 6 claim types are final |
| **D** | Slot packs and heads for the slots the records added | The deferred law work; the regrade; the Legal Review answers |
| **E** | Two stop points; the mirror set; the two pre-commitments; hidden v3; the freeze (step 6) | – |

Expected end: about 15 December 2026 (section 9).

### Every session

`PY` below, and in every file this handbook links, is `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe -X utf8`. **Never type bare `python`**: on this laptop it is another Python, without pytest. Paths are under the worktree `C:\Users\TheShadow\dz-fable-core` unless they start with a drive; `<work>` is `C:\Users\TheShadow\dz-core-research`.

1. Read `<work>\STATE.md` as [STATE-FORMAT.md](STATE-FORMAT.md) says. Do not act from memory of a conversation.
2. `PY research\core-logic\scripts\clean_env.py --check`. It prints `clean`, or the names of the keys the session was started with. **This is a gate** (first row of the table).
3. `git status` and `git log -3`; the last lines of `<work>\ledger\runs.jsonl`.
4. If Codex will be used today: `codex --version` must print `codex-cli 0.167.0`; `codex login status` (note only "logged in: yes" or "no"); `PY research\core-logic\scripts\codex_run.py probe1` must be accepted and `… probe2` must be **rejected** by the check.

**You are ready when:** the state file's "next action" is the thing you are about to do; the key check printed `clean`; nothing is running that you do not know of; and, for a Codex day, the gate file of today exists.

| Situation | Do |
|---|---|
| The key check prints key names | **Launch no agent.** Tell the owner: "close every VS Code window, then start it with `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe C:\Users\TheShadow\dz-fable-core\research\core-logic\scripts\clean_env.py code C:\Users\TheShadow\dz-legal-assistant`". Scripts and Codex calls are clean whatever the session holds, and may run meanwhile. (The owner's rule of 14 November: the removal is automatic, never a prompt instruction. The prompts' own key line stays as a second safeguard, never as the first.) |
| Is something still running? | Agents of an earlier session are never running. The Codex pool or queue runs exactly when its lock file names a live process ([specs/codex-calls.md](specs/codex-calls.md)). The Official Journal scan and the local filter resume from their own lists. |
| The ledger shows a run "running" and nothing is running | Mark it `failed`. A file that run half wrote: if git tracks it, `git checkout -- <path>`; otherwise delete it. Then launch the run again, fresh. A Codex batch: accepted answers are kept; the rest goes back to the queue by the rules of [specs/codex-calls.md](specs/codex-calls.md). |
| Codex's version is not 0.167.0 | No Codex call. Run both probes, tell the owner, wait. Do not update or reinstall anything yourself. |
| The second probe is not rejected | No Codex call that day. Tell the owner. |
| The worktree has uncommitted files | Finished outputs: check them (schema, tests) and commit. Half-written agent outputs: as above. Never complete one by hand. |

### Phase 0. Close formal-notice and retool (about a day and a half)

**Why first:** the module is one run from done, and every later unit needs the tools the first module showed were missing.

**Inputs.** `<work>\runs\step1\formal-notice\rules-verify-followup2.prompt.md` (a prompt prepared on 14 November); `docs\research\core-logic\rules\reports\formal-notice.attacks.followup2-input.json` (its two entries: `owner-done`, `store-120`); the working-folder scripts of 14 November ([specs/unit-driver.md](specs/unit-driver.md) says what becomes of each).

**Do, in this order.** `R` stands for `docs\research\core-logic\rules`.

1. **`check_commit.py` and the published rules tool first** (items 1 and 2 of the retool list of [specs/unit-driver.md](specs/unit-driver.md)), so that every later commit is checked and no build can disturb an agent.
2. **The follow-up run** (Opus 5.5, fresh). Fill the prompt again from today's template (the prepared file was filled before the template gained the `owner` and `store` kinds): `PY research\core-logic\scripts\prompts.py fill rules-verify --unit formal-notice --out <work>\runs\step1\formal-notice\rules-verify-followup2.prompt.md`, then set its record, input and register paths to the follow-up's (the final candidate; `…followup2-input.json`; `…followup2.json`), as the prepared file had them. It applies the owner's rule 7 of 14 November to the three notice steps and changes the cite of statement `b12` from «civil-code 120 ¶2» to ¶1. Write its ledger lines by hand (the driver does not exist yet). Outputs: `R\reports\formal-notice.attacks.followup2.json`, a new `R\final-candidates\formal-notice.json`.
3. **The scripts on the result** (from `research\core-logic\scripts`; `<T>` and `<S>` are the tool and the store of [specs/unit-driver.md](specs/unit-driver.md); `C` is `..\..\..\docs\research\core-logic\rules`):
   - `<T> check --store <S> C\final-candidates\formal-notice.json`
   - `PY records.py quote C\final-candidates\formal-notice.json` (it writes `C\final-candidates\quoted\formal-notice.json`)
   - `PY records.py check-values C\final-candidates\quoted\formal-notice.json --report C\reports\formal-notice.values.final.json`
   - `PY records.py flag-neighbours C\final-candidates\quoted\formal-notice.json --report C\reports\formal-notice.flags.final.json`
   - `<T> examples C\final-candidates\formal-notice.json`, and `<T> examples C\final-candidates\formal-notice.json --boundary C\boundary-cases\formal-notice.json --json > <work>\runs\step1\formal-notice\boundary.final.json`
   - `PY records.py grade C\final-candidates\quoted\formal-notice.json --idmap C\reports\formal-notice.idmap.json --differences C\reports\formal-notice.differences.json --attacks C\reports\formal-notice.attacks.json --attacks C\reports\formal-notice.attacks.followup.json --attacks C\reports\formal-notice.attacks.followup2.json --out C\final\formal-notice.json --report C\reports\formal-notice.grades.json`
   - `PY records.py check-checklist C\final\formal-notice.json --checklist C\reports\formal-notice.checklist.json --flags C\reports\formal-notice.flags.final.json --report C\reports\formal-notice.checklist-check.json`
4. **The rest of the retool list** (items 3 to 13 of [specs/unit-driver.md](specs/unit-driver.md), each with tests, one commit each).
5. **Regrade formal-notice with the new grade** (`PY unit.py regrade --all`) and rewrite its rows of [reports/step-1-tracking.md](reports/step-1-tracking.md) (`PY unit.py tracking formal-notice`). **Expect no item at grade A**: no planted-error round has run (`plantedNotRun`), so every item that declared a block warns. That is the owner's rule working, not a fault.
6. The full CI; push.

**Done when.**
- `<T> check --store` passes on `final\formal-notice.json`; `check-checklist` reports `ok: true`; no item is grade D for an unverified text; no item is grade A; every item has `textProof` and `proof`.
- Boundary cases bc6, bc8 and bc12 pass, **or** each one that still fails after one more follow-up is in `reports\questions\batch-1.md` with the runner's message (expected: 8 of 12; the other four are bc3, bc4, bc9, which two families hold wrong, and bc7, which is open).
- `PY unit.py status formal-notice` shows every stage done from the files alone (the legacy map).
- Every test the retool list names passes; `check_commit.py` passes on its own commit.

| Situation | Do |
|---|---|
| bc6, bc8 or bc12 still fails after the run | One more follow-up: `PY unit.py verify-input formal-notice --followup` (it writes `…followup3-input.json` with the runner's message as an `owner` entry), one verifier run, the scripts again. Still failing: `PY unit.py questions --batch 1 formal-notice`, and go on. |
| The run changed an example's expected outcome | Allowed only where the owner's rule requires it (a step that said "done" now stays open). List each in the tracking file. Any other changed expectation: `git checkout -- docs\research\core-logic\rules\final-candidates\formal-notice.json`, rerun fresh. **Check this before step 3**: `quote` overwrites the quoted copy. |
| `<T> check --store` fails on a cite | The store has no such paragraph. Do not edit the record: a follow-up with the failure as a `store` entry. |
| The follow-up changed statement `b12` | Its support-check answer is stale: `PY unit.py codex formal-notice` asks Codex again for the changed statements; until it returns the item's `proof` is `oneFamily`. |
| The full CI fails only on `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemory…` | Run that test alone three times. If it passes three times, push and say so in the report. Once a day, at a pause, run the full CI on an idle CPU until it is green. |
| Any other CI failure, or `check_commit.py` fails | Do not push. Fix the cause in a new commit. |

**Traps.** Do not absorb `run_codex_c.py`: Codex no longer derives. Do not publish the tool while an agent is running. Do not "fix" a failing boundary case by editing its file: case files are never edited.

**Report to the owner** (no stop point): the commit id; formal-notice's grades (none A, and why) and its boundary count; the tools added; the sentence that every check on this module except Codex's support check was made by Claude models (section 5b); anything uncertain.

### Phase A, reader track. The scheme, the yardstick and the pilot (about four days)

**Why this shape:** safeguards 15 to 17, 22 and 30. Specifications: [notes/SCHEME.md](notes/SCHEME.md), [specs/scorer.md](specs/scorer.md), [specs/scenario-sampler.md](specs/scenario-sampler.md), [specs/notes-pipeline.md](specs/notes-pipeline.md) (its "commands of the notes tool" table names every command below), [specs/reader.md](specs/reader.md) (rungs 1 and 2, "The front end").

**Inputs.** `data\law\claim-elements.json`, `data\law\claims\*.json`, `src\DzLegal.Drafting\CaseFacts.cs` (read only); `docs\research\core-logic\slots.json`; the labelling guide; `notes\guide-test.json` (45 items); the contract `INoteReader`.

**Outputs.** The frozen `notes\SCHEME.md`; `notes\cells-test.json`; the projects `DzLegal.CoreLogic.Scoring`, `DzLegal.CoreLogic.Notes`, `DzLegal.CoreLogic.Reader` (front end and plain code) under `research\core-logic\src`, with tests; `research\core-logic\train\export_encoder.py`; `<work>\models\pretrained\camelbert-mix\` and the 8-bit encoder with their hashes; `<work>\notes-work\` (the queue, the pilot's notes); `reports\pilot.md`.

**Do, in this order.**

1. **Freeze scheme 1.0** ([specs/notes-pipeline.md](specs/notes-pipeline.md), section 1; **5 runs**, say so first): 30 new guide-test items (one Sonnet run); the guide test on all 75 items (Sonnet, Opus); the 75 cell items of [prompts/notes-cells.md](prompts/notes-cells.md) (Sonnet, Opus). Then the freeze commit and the stamp commit.
2. **The contract, the loader, the scorer, in that order** ([specs/scorer.md](specs/scorer.md), "What the scorer's hash covers"): `RoleReading` and `ClaimGuess.Role` in `NoteReading.cs` with a contract test; the loader with its refusals; the scorer with its tests, the role line and the tuning score. Record the scorer's hash in the state file **before any model exists**.
3. **The plain code and the front end** ([specs/reader.md](specs/reader.md), rung 2 and "The front end"): the date and amount parsers and the cue table with `cues.json`; the tokenizer, the windows, `reader ids`, `reader encode`; the download of CAMeLBERT-mix and its 8-bit export.
4. **The notes code:** sampler version 1, the style cards, the fictitious cast lists, `writer-labels`, `build-spans`, the code checks, the filter's sheet builder, the commands of the notes tool.
5. **The pilot:** 500 scenarios; 25 Codex calls of 20 notes (at most 5 more for scenarios that come back), the pool at 2 writers, then 3, then 4 ([specs/codex-calls.md](specs/codex-calls.md)). Say "25 to 30 Codex calls" to the owner before the first.
6. **The filter trial** on 100 pilot notes, for both local models; then the winning filter on all 500, at night or on a free day.
7. **The noise measurement:** 100 kept notes: 4 Opus runs relabel them blind ([prompts/notes-noise-audit.md](prompts/notes-noise-audit.md)), a script lists the fields that differ, one Opus sorting run per 40 differing fields says which side is right ([specs/notes-pipeline.md](specs/notes-pipeline.md), "The label noise"). It changes no note.
8. **The pilot report** `reports\pilot.md` (its contents: [specs/notes-pipeline.md](specs/notes-pipeline.md), section 2) and the calendar. **Stop point.**

**Done when.**
- `notes\SCHEME.md` is frozen and stamped, its hashes and cells table filled; the guide-test results show both testers on all 75 items; no item is unexplained.
- The scorer's tests pass (the role line and the tuning score among them); its hash is in the state file.
- The parsers read 98 % of the code-made value corpus exactly; the cue table gives no wrong vote on the guide's 37 hard cases; the .NET piece ids equal the Python tokenizer's on the 500 pilot notes, every difference explained.
- The loader's refusal tests pass (a test-set note in training; a Sonnet-written note for tuning; a note without provenance; more than a quarter composed).
- The pilot report holds every item of its list, the feasibility paragraph and the calendar to 4,000 notes.

| Situation | Do |
|---|---|
| A guide-test item is answered differently by the two testers, or against its key | Fix the guide's wording, bump its version, rerun that item with both. Still two answers: write the item into SCHEME.md's list of known gaps, and into `reports/open-legal-questions.md` when the two answers differ on a reading of the law, or into the owner's question batch when they differ on a labelling convention (a data decision); it then counts as explained. Do not freeze with an unexplained item. (The cell items are outside this rule.) |
| The items run returns fewer than 30 items, or lists `problems` | Fewer: a fresh run for the missing number. Each problem is a defect of the guide: fix it, bump the version, before the guide test. |
| A Codex call is rejected by the event-log check | Its notes are discarded and its scenarios go back to the queue once. Two rejections in a row from one writer: stop that writer, keep the others, tell the owner. |
| A usage-limit refusal | The pool stops. Record the time and the calls made. Do not retry before the window the refusal names; the remaining calls of that pool size run then. Report the calls of the window: that figure is the calendar's input. With no refusal at all, report the calls and tokens as a lower bound. |
| More than 30 % of a call's notes fail the code checks | Read ten failures. One cause explains more than half: fix the prompt once, bump its version, rerun two calls. Still over 30 %: stop point. |
| Neither local model passes the filter trial | Keep the one that caught the larger share of its plants, mark every pilot note `filterWeak`, and put the question to the owner at the pilot stop point with the table. Do not start volume. |
| The label noise is over 10 % | Stop. Show the owner the figure, the share of notes with a wrong label and the three most common kinds (described, with one fictitious example each). |
| The label noise is between 5 % and 10 % | Go on; change the sheet for the most common kind, for the notes filtered from then on; measure again on the next 1,000; say so in the report. |
| The filter alone would need more than 8 laptop nights at the measured pace | A question for the owner at the pilot stop point (a shorter sheet, or the faster model). |
| The laptop is on battery, or the owner needs it | The filter does not run. It resumes from its queue; nothing is lost. |

**Traps.** The pilot's notes are training notes: the only Claude readings of them are the noise measurement's, the sorting of blanked fields, and your own reading of failures when a row above says "read"; none changes a note. The sheet is never tried again on the 100 measured notes. Do not freeze the scheme after the pilot: its labels would then follow the notes. Do not let a pool run past midnight: the day's probe is yours to run.

**Report to the owner (stop point):** the pilot report; the calendar; the count of Codex calls, Claude runs and laptop nights that phase B needs; the sentence that the guide test and the cells table were decided by two Claude models; the one question: "go on to 4,000 training notes with this filter and this pace?"

### Phases A and B, rules track. One unit through the driver

**Why this shape:** safeguards 2 to 4, 8 to 14, 27, 31 and 32. Specifications: [specs/unit-driver.md](specs/unit-driver.md) (every command, file and stage), [specs/record-checks.md](specs/record-checks.md), [rules/GRADES.md](rules/GRADES.md), [specs/planted-errors.md](specs/planted-errors.md).

**Inputs, for a unit.** The store and its text views; `law\seed-inventory.json`; `slots.json`; for a claim type, the final records of the modules it imports. **Outputs:** the files of the stage table, all under `docs\research\core-logic\rules\`, and the unit's rows of `reports\step-1-tracking.md`.

**The order of units.**

1. **Phase A, the high-harm modules:** `periods`, then `labour-prelitigation`, `court-sections` and `ordinary-court-exits`. `periods` goes first and alone, because it fixes how a period is counted and every deadline cites it; the other three may run their stages 0 to 4 beside it, but **no other unit with a deadline is reconciled before `periods` is final**. When `periods` is final, the evaluator's counting is replaced by what it established ([specs/unit-driver.md](specs/unit-driver.md), last paragraph).
2. **Phase B:** planted-error round 1 on the five final modules. Then the other seven modules, three in flight: `territorial-jurisdiction`, `petition-formal-elements`, `proof`, `accessory-requests`, `land-registry-publication`, `conciliation-mediation`, `commercial-lease-regimes`.
3. **Then the claim types**, three in flight, the first six chosen so that step 5 can start: `unpaid-wages`, `abusive-dismissal`, `loan-repayment`, `unpaid-rent`, `commercial-debt`, `nafaqa`; **planted-error round 2 as soon as those six are final**; then the remaining fifteen, area by area. A claim type is not reconciled before every module it imports is **final**.

**The stages of a unit.** `PY unit.py next <unit>` runs the scripts that are due and prints the agent launches that are due, each with its model and its filled prompt file; `PY unit.py done <unit> <stage> <agent id>` records a returned run. The driver writes the run ledger.

| # | Stage | Who | Prompt or script | Output |
|---|---|---|---|---|
| 0 | The unit's texts: every chapter its seed articles touch, and one heading up | script | `records.py unit-texts <unit> --seed` | `reports/<unit>.texts.json` |
| 0b | The French sources of derivation B | script; a Sonnet reading only for pages before 2003 | [specs/unit-driver.md](specs/unit-driver.md) | `law/french-sources.json` |
| 2 | Derivation A and derivation B, blind to each other | Opus 5.5; Sonnet 5.5 | `rules-derive-a.md`, `rules-derive-b.md` | `derivations/a|b/<unit>.*` |
| 2c | Third derivation (modules only), flat | Sonnet 5.5, no tool | `rules-derive-c.md` | `derivations/c/<unit>.json` |
| 2d | Boundary cases, blind to all records; never edited afterwards | Sonnet 5.5 | `rules-boundary-cases.md` | `boundary-cases/<unit>.json` |
| 3 | The derivers' text requests | script; readers only for a text a rule must cite | [law/README.md](law/README.md) | `law/requests.jsonl`, the store |
| 4 | Check, quote, values, flags, merged checklist, A against B by field and by case, C's comparison. **Commit.** | scripts | [specs/record-checks.md](specs/record-checks.md) | `reports/<unit>.*` |
| 5 | Reconcile | Opus 5.5 | `rules-reconcile.md` | `reconciled/<unit>.json`, `reports/<unit>.reconciliation.md` |
| 6 | Scripts again; the id map completed; the boundary cases run | scripts | | `reports/<unit>.*` |
| 7 | Adversary (web); attack; support check with seeded pairs | Opus 5.5; Sonnet 5.5; **Codex** (queue) | `rules-adversary.md`, `rules-attack.md`, `rules-support-check-codex.md` | `reports/<unit>.attacks.adversary.json`, `.attacks.sonnet.json`, `.support.json` |
| 8 | Verify **everything at once**: the attack lists, C's differences, the value mismatches, the store's refusals, the unanswered neighbour flags, the boundary cases the record fails | Opus 5.5 | `rules-verify.md` | `reports/<unit>.attacks.json`, `final-candidates/<unit>.json` |
| 8b | Blind view on boundary cases the verifier set aside | **Codex** (queue) | `boundary-second-view-codex.md` | `reports/<unit>.boundary-view.json` |
| 9 | Scripts again; slots; flatten (claim types); grade; checklist check; the tracking rows | scripts | | `final/<unit>.json` |

**In parallel:** stages 2, 2c and 2d of one unit; the same stage across up to three units. **One run per unit for every agent stage.** Say the number of runs before each launch: a module has 8 Claude runs and 2 Codex calls; a claim type 7 and 2; one more verifier run for each follow-up.

**Questions of legal reading are never sent to the owner** (the owner's standing rule of 15 November 2026). `PY unit.py questions <units>` writes each unit's section of `reports/open-legal-questions.md`: each question in one plain sentence, the readings, **the protective behaviour the record keeps** (it warns, keeps both readings, never blocks; the item stays C or D), and what would change with each answer (entry 4, written by the executor from the readings only). The owner's batch, `reports/questions/batch-<n>.md`, holds only decisions about the product, the process, the budget, tools, data or the stop points; the work never waits for it, and an answer becomes an `owner` entry of a follow-up where it touches a record.

**Done when, for a unit.** `<T> check --store` passes on the final record; every worked example passes; `check-checklist` is `ok`; no final record has a `proposedSlots` entry; every statement has a quote, a source, a grade and a proof label written by script; `unit.py status` shows stage 5 and stage 8 done (every difference has its line; every finding has a verdict with a quote); the unit's rows of the tracking file are filled.

**Done when, for the track.** Phase A: the four high-harm modules are final. Phase B: all 33 units are final, both planted-error rounds are scored in `rules/reports/planted-errors.md`, and `rules/reports/grades.md` is written (texts still "to check" until phase D).

| Situation | Do |
|---|---|
| A deriver's run ends without its record, or its record fails `<T> check --store` | Delete what it wrote. Launch the same prompt fresh. Never repair it. Two failures: read the tool's message; if the prompt or the guide is at fault, fix it (version bump), then a third run. |
| A deriver requests a text the store lacks | `need: basis`: fetch and read it now ([law/README.md](law/README.md): an article added to the delta gets a Sonnet and a Codex reading), then rerun **that derivation** fresh. `need: context`: listed; phase D decides. |
| Stage 0b finds no French text for an instrument | The entry says so, and derivation B is told so in its prompt. It is a line of the tracking file, not a stop. |
| A deriver changed one of its own examples after running the tool | Its reply lists it. Allowed when only the expected questions changed and the rule did not; record it in the tracking file. If an expected effect changed: keep the run, and add the example as a case difference for the reconciler. |
| Stage 0 lists chapter members missing from the store | Fetch only those in a chapter that holds one of the unit's own pointer articles. The others stay listed as `missingFromStore`; phase D fetches those a final record cites. |
| The unit cites an instrument whose triage is open (Commercial Code, Family Code, labour laws) | Go on. Its items cannot be grade A (`triageOpen`, `preScanGap`). The adversary's `{{leads}}` holds every untriaged hit that names the instrument. |
| A and B agree on under 40 % of compared fields | Go on to the reconciler; say so in the tracking file. Under 25 % on two units running: stop and read both prompts for a shared misunderstanding before the next unit. |
| The reconciler adds an item neither derivation had | Kept; graded `addedAfterDerivation` (never A). The verifier is told to test it first. |
| The support check accepts more than one seeded false pair in five | Its "yes" answers in that call do not count. Run the call once more with fresh pairs. Again: stop Codex for this role and tell the owner (stop point "Codex fails its bar"). |
| A unit waits more than 10 minutes for the Codex queue, or Codex refuses for a usage limit | Record it. The unit goes on; its items carry `supportCheckPending` and cannot be A until the call returns and a follow-up has decided its findings. |
| The verifier sets a boundary case aside | Stage 8b. Codex gives the record's answer: the case stays a counted failure with the two-family reason. Codex gives the case writer's answer, or a third answer: the case becomes **uncertain**, and a follow-up makes the record keep both readings and warn. |
| A question turns on which reading of the law is right (an open point, a disputed or uncertain case, a last-day difference) | Never sent to the owner (standing rule of 15 November 2026). Keep the protective behaviour (warn, both readings, never block), leave the item at C or D, and record it with `PY unit.py questions <unit>` in `reports/open-legal-questions.md`; write its entry 4 from the readings. |
| A boundary case differs from the record only by its last day | Not sent to Codex. It goes into `reports/open-legal-questions.md` with both dates, and counts as a failure meanwhile. |
| A boundary case fails only because the evaluator knows no holiday or rest day | An evaluator limit: a line of the tracking file, not an attack. |
| A step says "done" while another item still asks or warns about the same step | The runner reports it; it is an `owner` entry of the verifier's input (rule 7 of 14 November). |
| An attack names an item that a claim record imports from a module | It goes to the module's next follow-up; then the module is regraded and the claim types that import it are re-flattened and regraded. |
| After stage 9 a neighbour flag is unanswered, a value does not match, or the store refuses a cite | A follow-up. More than two verifier runs on one unit: say so in the batch report to the owner. |
| A boundary case fails only by a limit of the evaluator or of the condition language, as a verifier found | A line of `rules/reports/<unit>.boundary-limits.json` (case, kind, why, found by): listed in the tracking file and in `open-legal-questions.md`; not an attack, not sent to Codex. |
| A store correction changes the paragraph numbers of a cited article | A follow-up changes the cites. Never by script. |
| A unit's item is grade D for an unverified text | Expected before phase D. It warns, marked "to check". List it; do not start a text pass for it. |
| You "know" a record is wrong | Raise an attack with a source and send it through a follow-up like any other. |

**Traps.** Deriving a claim type before its modules are final bakes a module's unsettled point into many records. Sonnet's B is not a lesser A: check the reconciliation reports for "A's reasoning" used as a reason. The adversary will bring many tier-3 leads; an unconfirmed specific lead becomes an open point, not a dismissal. Case law is not in the store: a rule that needs a Supreme Court principle is `interpretive`, grade C at best. Publish the rules tool only when no agent of a unit is running.

**Planted-error rounds** ([specs/planted-errors.md](specs/planted-errors.md)). Round 1 after the fifth final module; round 2 after the sixth final claim type. After each: `PY unit.py regrade --all`. Until round 2 a kind counts as caught only when every one of its plants was caught and there were at least three. Neither round is a stop point.

**Report to the owner, after each batch of three units:** the commit id; units final; for each: agreement before reconciliation, attacks fixed, refuted and open, grades with how many may block, boundary cases passed; the proof sentence of section 5b; the entries added to `open-legal-questions.md` (counted, never put to the owner) and the owner's batch file if a product or process decision is needed; the running totals of runs and calls against section 9; what is next.

### Phase B, reader track. The sets, the volume and the first rungs (about a week)

**Why this shape:** safeguards 16, 18, 20 and 23. Specifications: [specs/notes-pipeline.md](specs/notes-pipeline.md) (sections 3 to 10 and its commands table), [specs/reader.md](specs/reader.md) (rungs 3 and 4), [specs/probes.md](specs/probes.md).

**Inputs.** The owner's answer at the pilot stop point (the pace, the filter); the frozen scheme; the notes tool; the front end and the 8-bit encoder. **Outputs:** `<work>\notes-work\sets\*.jsonl` with their counts and SHA-256 in the repository; `<work>\reader\cache\`; `research\core-logic\train\` (`train_heads.py`, `decode.py`, `linear.py`, with tests); `reports\learning-curve.md`.

**Do.**

1. **Dev and challenge sets** (`notes sample`, `notes write`, `notes ingest`, `notes relabel`, `notes compare`). Dev: 600 kept, 300 written by Codex and 300 by Sonnet, from scenarios drawn the same way. Challenge: 12 difficulties of 50, 25 by each writer, by the recipes of [specs/scenario-sampler.md](specs/scenario-sampler.md). Relabel blind: Codex-written notes by Sonnet, Sonnet-written notes by Opus ([prompts/notes-judge.md](prompts/notes-judge.md), 25 notes a run, no tool). A test note is kept only if the scenario's labels and the blind relabel are equal on every field.
2. **Training notes to 4,000 kept.** Codex writes; the code checks and the local filter decide; blank-and-keep under the owner's rule; the cells are counted at 3,000, so that the refills fall inside the 4,000. Opus measures the noise on 100 of every 1,000 kept.
3. **The clean-writer trial** (20 scenarios, both local models, two ranking runs: [specs/notes-pipeline.md](specs/notes-pipeline.md), section 8, point 1), then **the phrases and the composed notes** (section 9): 1,500 composed notes and the 150 probes; the probes confirmed by one blind Sonnet relabel (6 runs).
4. **The cache** (`reader encode`) for every open note.
5. **Rungs 3 and 4** of [specs/reader.md](specs/reader.md), in its fixed order of choices: the linear baseline; the encoder choice at 1,000 notes (CAMeLBERT-MSA is downloaded and exported for this); the loss variant; the frozen encoder with its heads at 1,000, 2,000 and 4,000, three seeds, with and without composed notes; the one comparison of the 8-bit form with the 32-bit one. **Every choice on the tune notes** (the Codex halves of dev and of the challenge set).
6. **The probes and the reviews** ([specs/probes.md](specs/probes.md)): near-copies and leakage on every open set; the realism review of the two halves of dev (about 6 runs; the clean test split's in phase C); the odd-one-out (2 runs).
7. **The learning-curve report** `reports\learning-curve.md`. **Stop point.**

**Done when.** Each set meets its line of the "done when" table of [specs/notes-pipeline.md](specs/notes-pipeline.md), section 7; every kept note is schema-valid with its provenance; a test asserts that no note with `trainingAllowed: true` has a Claude model as writer, filter or relabeller; the curve report shows, per size and per seed, the tuning score and each target of [specs/scorer.md](specs/scorer.md) on the Codex half and on the Sonnet half of dev, the challenge set per difficulty and per writer, the style gap, and the probes.

| Situation | Do |
|---|---|
| The style gap is more than 5 points | Report it. **Change nothing because of it.** At the stop point the owner may choose a remedy (more style cards for the Codex writer; more composed notes, up to a quarter; rung 5); it is then judged on the tune notes. |
| A cell is under its share of 60 when 3,000 notes are kept | Targeted scenarios for it in the calls that lead to 4,000. Still under 60 at 4,000: reported with its count. Nothing beyond 4,000 is written before the owner's answer. |
| Composed notes lower the tuning score, beyond the spread, at two of the three sizes | Drop them from the shipped recipe; say so. |
| The tuning score still rises by more than 1.5 points from 2,000 to 4,000 and a target is missed on the tune notes | The report recommends more notes, with the Codex calls and nights they cost. The owner decides. |
| The curve is flat and a target is missed by more than 2 points on the tune notes | The report recommends rung 5 (phase C), not more notes. The owner decides. |
| CAMeLBERT-MSA is not better than mix by a full point of the tuning score, beyond the spread | Keep mix. |
| The relabel drops more than 35 % of a writer's test notes | [specs/notes-pipeline.md](specs/notes-pipeline.md), decision table. A fault seen only on Sonnet-written notes changes no prompt: it is reported. |
| The composed notes or the phrases are not ready when the curve is drawn | Draw the curve without them and say so; add their rows when they exist. |
| You are about to choose something from a figure on a Sonnet-written note | Do not. Choose on the tune notes; report the other figure beside it. |

**Traps.** The Sonnet halves share their writer with the mirror set and hidden v3: a reader chosen on them is fitted to the hidden set's manner. A realism review never reads a training set. A polarity group is written together and relabelled apart. The cache is keyed by the encoder's hash: never mix vectors of two encoders.

**Report to the owner (stop point):** the curve; what it means in one paragraph; the style gap; the cells still short; the recommendation by the two rows above; the sentence that the Sonnet-half figures rest on labels two Claude models agreed on; the one question: "more notes, fine-tuning, or go on as it is?"

### Phase C. The reader finished, and step 5 (about a week)

**Inputs.** The owner's answer at the learning-curve stop point; the 4,000 training notes and their cache; six final claim types for step 5. **Outputs:** the clean set; the reader's five files per candidate under `<work>\models\reader\<candidate>\` with their hashes in the repository; `DzLegal.CoreLogic.Reader` complete; `reports\reader.md`; `docs\research\core-logic\results\step-5\`; `reports\step-5.md`.

**Reader track** ([specs/reader.md](specs/reader.md), rungs 5 to 9).

1. **Rung 5 (training the encoder's top layers), only if** its rule fires on the tune notes **and** the owner answered "fine-tuning"; two overnight runs at most.
2. **Clean notes:** 1,000 by the better local writer: 700 for training, 100 clean-dev, 200 as the clean test split ([specs/notes-pipeline.md](specs/notes-pipeline.md), section 8); the realism review of the clean test split (3 runs); one noise measurement on the clean training notes. Then rung 7: clean against Codex-trained.
3. **Rungs 6 and 8:** the rule vote; calibration and thresholds, on the tune notes (now with the clean dev split).
4. **Rung 9:** the heads in .NET, the decoding, `INoteReader`; parity on 200 notes of the Codex half of dev; time, memory and bytes under the measuring guard.

**Done when (reader track).** `reports\reader.md` holds items 0 to 6 and 8 of [specs/reader.md](specs/reader.md)'s list; parity passed (the same labels, confidences within 0.01); the thresholds file of each candidate is hashed; a note takes under one second (median and 95th percentile, per length class) and the reader's files are under 150 MB, or the report says by how much they miss; `dotnet test` is green in a clone without model files.

| Situation | Do |
|---|---|
| A training run fails (a crash, a loss that does not fall, an out-of-memory error) | Two more attempts, each with one named change. After the third failure or one lost day: stop and ask the owner. Only with the owner's approval does the Codex training backup start ([specs/codex-calls.md](specs/codex-calls.md)). |
| The backup's three sessions do not solve it | Stop. The owner chooses: do without that rung, or a free GPU. |
| The 8-bit model loses more than a point of the tuning score against the 32-bit one | Static quantisation with 200 training notes for calibration; then, if needed, one epoch with the quantised weights simulated. Report both figures. |
| The reader in .NET differs from Python by more than 0.01 on a confidence | A tokenizer, offset or stitching fault. Find the first differing piece on the parity notes; do not relax the bar. |
| A note takes more than one second | Measure per stage. Usual cause: three windows. Report the notes affected; the cap goes to two only if the tuning score does not fall beyond the spread. |
| The clean-trained reader is close ([specs/notes-pipeline.md](specs/notes-pipeline.md), section 8: within 2 points on four lines, on the tune notes) | Tell the owner: the decision of 12 November then plans the shipped reader on clean notes only, as that section's point 5 defines it. Both candidates go to the mirror set, each with its own files. |
| Training would need more than two overnight runs | Stop point. |
| A timing's guard failed | The figure is void. Measure again on mains, idle. |

**Step 5** ([specs/step5-templates.md](specs/step5-templates.md)), once six claim types are final. In the app today, thirteen of the petition's fourteen sections are already written by templates, and one by the model: the legal discussion. So step 5 measures the two sections a model could still be needed for: **the legal discussion** (one paragraph per element, from the record's articles and the confirmed facts) and **the facts**. 120 fact sheets; each part written by a template and by the four local models; the faithfulness script on every output; two blind judges (Sonnet and Opus) on a sample, 24 runs; two Arabic checks of the templates (Sonnet); time, memory and the bytes of each set-up; about two laptop nights. The requests stay the lawyer's own lines, as the app's own audit decided. **Done when** `reports\step-5.md` holds the one table (for each section, the smallest set-up that is faithful 98 % of the time and usable 90 % of the time) and the two answers: how much templates can write, and whether the app can work under 1 GB. Its own decision table is in the specification. **Nothing is fine-tuned without the owner's word.**

**Traps.** A 32-bit score is not the reader's. A fine-tuned encoder's score counts only after quantisation, a new cache and refitted heads. A "clean only" reader never uses an encoder trained on Codex notes. In step 5 the script decides faithfulness; a judge never overrules it, and no model rewrites the requests.

**Report to the owner:** the commit id; the ladder table with intervals, the Codex half and the Sonnet half side by side; size, time and memory; the step 5 table and its two answers; the one-family sentences (the Sonnet-half figures; step 5's judges); what did not work.

### Phase D. The slots the records added, and the deferred law work (about five days)

**Inputs.** The 33 final records and `slots.json`; the scan's evidence files; `law\triage-status.json`; the reader of phase C and its cache. **Outputs:** `notes\SCHEME.md` section 10 filled (and scheme 1.1 if a slot is read); `law\inventory.json`, `law\law-delta.json`, `law\amendment-ledger.json`, `law\triage-status.json`, `law\REPORT.md`; every `final\*.json` regraded; `rules\reports\grades.md`; `docs\research\core-logic\legal-review-answers.md`.

**Reader track.** For each slot the final records added, decide by the table of [specs/scheme-extension.md](specs/scheme-extension.md) whether it is derived, stays a question for the lawyer, or becomes a reader output. For those that do: the guide row, six guide-test items, the note pack, the head, the proof that older outputs did not change, its own threshold; only then `readFromNote: true`. The scheme becomes 1.1. That specification holds this track's decision table.

**Rules track**, in the order of the table "Phase D: the deferred law work, step by step" of [law/README.md](law/README.md):

1. **The remaining Journal triage** (`PY triage.py plan` prints the runs: about 50 to 100 Sonnet runs, 8 at a time; **say the number first**); the special laws the records cite; `triage.py status`; `ledger.py build`.
2. **The texts before the scan's first year** (three labour texts, four Commercial Code texts), read from their Journal pages.
3. **The text pass:** `PY records.py inventory --out law\inventory.json` says which cited article needs which reading ([specs/article-store.md](specs/article-store.md), "When Codex reads a page"): every article that is not verified, or only `corpusChecked` under a deadline, a prior step, a section rule or an exit. Settlements by Opus on close-ups. Corrections go to the delta.
4. **Step 2's second pass:** `delta.py trim`; `law\REPORT.md`; the Journal PDFs deleted.
5. **`PY unit.py regrade --all`**, then `rules\reports\grades.md` ([rules/GRADES.md](rules/GRADES.md), last section).
6. **The Legal Review answers** ([prompts/legal-review-answer.md](prompts/legal-review-answer.md), [prompts/legal-review-attack.md](prompts/legal-review-attack.md)): the 22 questions in five groups, one answer by Opus and one by Sonnet (10 runs), each attacked by the other model (10 runs); you write `legal-review-answers.md` with the three marks, no typed grade.

**Done when.** Every added slot has its decision and the row that decided it; every slot to be read passed rows 2, 4, 7 and 8 of the specification, or is reported "read, below target". `triage-status.json` is complete for every code, or a code is listed as open with its reason; every cited article is verified or listed at grade D with its reason; `grades.md` is written after the regrade, with the proof labels and both planted-error rounds; the 22 answers each have their mark and their cite lines; `law\REPORT.md` gives the figures the brief asks for.

| Situation | Do |
|---|---|
| The triage finds a text that amends an article a final record cites | Fetch the amending text; add the wording to the delta (two readings); rebuild the store; a follow-up on each record that cites it, with the change as the attack; regrade. |
| A page reading differs from the store | A reading by the other family, then the settlement rule of [law/README.md](law/README.md). A difference of paragraph division only: a `paragraphDivision` correction, as for Civil Code 120. |
| A wording correction touches an article a final record cites | Re-quote. If `check-values` then differs, or the meaning changes: a follow-up, and a Codex support check of the changed statements. |
| A settled wording is not the Codex reading's | One more blind Codex reading of the close-up; still different: the entry's proof is `oneFamily` ([specs/article-store.md](specs/article-store.md)). |
| A reader copied the article's heading | The comparison already drops it (`reading.drop_heading`). |
| A text stays unverified after the pass | Its items stay grade D and are listed in the grade report, with the reason. |
| A triage answer stays `unsure` after a fresh re-read | The code's triage stays open; its items stay at B; the report names the issue. |
| The scan misses a known amendment | Say the scan is not trusted; every `ledgerClean` false; grades fall to B. Do not patch the scan by hand. |
| A slot needs a value type the scheme lacks | Stop point. |

**Traps.** `ledger.py` reads triage answers two folders deep: use the work folder the table names. An article of the two labour laws is read by Codex only: a Sonnet reading adds nothing there. The labour ministry's site is not an allowed source: the Journal's pages are. A slot pack is training material: Codex writes it, the local filter decides, and no Claude model judges whether its notes "state the slot".

**Report to the owner:** the commit id; the figures the brief asks for (cited articles checked by script, by a reading, corrections found); the corrections for the corpus and for the library; the final grades and how many items may block; the planted-error rates; the slots' decisions; the Legal Review answers' file; the sentences of section 5b (the triage, the settlements and the Legal Review rest on Claude models).

### Phase E. The mirror set, hidden v3 and the freeze (about a week)

Specifications: [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md) (every command, the loop, its own decision table), [specs/scorer.md](specs/scorer.md), [prompts/freeze-rebuild.md](prompts/freeze-rebuild.md).

**Inputs.** The at most two final candidates with their files; the probe pool; everything the generation method needs, to be frozen. **Outputs:** the mirror set (`<work>\mirror`, `<work>\mirror-relabel`, `<work>\mirror-results`); `docs\research\core-logic\results\mirror\` and `results\hidden-v3\` (`PRECOMMIT.md`, `RUNS.md`, the summaries); `docs\research\core-logic\FREEZE.md`.

1. **Freeze the generation method** (commit id recorded).
2. **Stop point "Before the mirror's planner":** the Workflow opt-in and the permission rules ([specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), sections 1 and 6).
3. **The mirror set** (about 440 kept notes in v3's proportions), with the v3 roles, prompts and commands: isolated Sonnet agents write; isolated Opus agents relabel, two known-truth probes in every batch; `v3 audit-relabel`; a fresh Sonnet is the third judge by the question-sheet route. It is the rehearsal of every v3 command.
4. **The mirror's `PRECOMMIT.md`; then the at most two candidates are scored on the mirror, once.**
5. **Stop point "Before the v3 planner":** the rehearsal's result and its figures.
6. **Hidden v3:** `v3 init`, planner, writers, validate, the relabel loop, compare, probes, refills (three rounds in all), finalize.
7. **v3's `PRECOMMIT.md`; push. The owner runs the one scoring command, once.** No commit between that file and the run.
8. **Step 6:** `docs\research\core-logic\FREEZE.md`; a fresh Sonnet rebuilds from it in a clean clone ([prompts/freeze-rebuild.md](prompts/freeze-rebuild.md)).

**Done when.** The ten points of [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), "Done when", hold; `RUNS.md` and the summaries are committed; the rebuild's figures equal the freeze document's, the mirror and v3 figures being checked from their committed summaries; your context holds no v3 text (if it ever did, that is reported).

| Situation | Do |
|---|---|
| A reply from a v3 planner, writer or relabeller holds text | Stop, discard the batch, tell the owner in one line without the text, wait. (On the mirror: discard, a fresh agent, count it, go on.) |
| The memory folder changed after a hidden-set agent ran | Stop, tell the owner in one line, start no new session before the owner has looked. |
| A quota cell is short after three rounds (the first and two refills) | Stop point: the cells and their counts. |
| A relabel batch is under the probe bar (two or more wrong fields on its two probes) | Its notes are relabelled by a fresh Opus; a second time for the same notes: stop point, with the per-field rates. |
| The mirror's label noise is over 5 % of notes, or more than half of its notes were dropped, or the pooled probe rate is under 95 % | Bring it to the stop point "Before the v3 planner"; v3 waits for the owner. |
| The rehearsal finds a defect after the freeze of the generation method | [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), section 6's table: fix, version bump, an amendment of the frozen method, that mirror stage again. |
| The mirror score is more than 3 points under the Sonnet half of dev | Say so in `PRECOMMIT.md`. Nothing is tuned; the mirror chooses nothing. |
| A rebuilt figure differs from the freeze document | Fix the document or its command and run the step again, never the figure. At most two full rebuilds. |

**Traps.** The mirror set chooses nothing. A hidden-set command is debugged on fixtures or on a marked copy of the mirror, never by looking. Never change the access rights of a hidden folder. Never open a relabel prompt file "to check it". Balanced sets are not realistic sets; say so beside every score. After the v3 run nothing is tuned on what the summary shows. With 100 notes a 90 % target is "met" only from 96 right; with 200 notes a 95 % target only from 197: several v3 targets will most likely come out "inconclusive". Say this to the owner before the run.

**Report to the owner:** the commit id; each target as met, not met or inconclusive, the worst test set of at least 200 notes as the headline; the mirror's prediction beside the v3 result; the probe rate and the third judge's figure; the sentences of section 5b on the one model family and on the relabeller's unchecked tool use; what the reviewer needs to integrate.

## 8. Stop points: when you must wait for the owner

At each of these: commit and push what is done; update the state file; make sure nothing unapproved is running; send a short plain report with the exact question; **wait**. Quote the answer in the state file before you continue.

| Stop point | What you bring | What the owner decides |
|---|---|---|
| **The pilot** (phase A) | the pilot report, the calendar, the runs and calls phase B needs | go on to volume; the pace |
| **More than 30 % of a call's notes fail the code checks, after one fix of the prompt** | ten failures described, the fix tried | change the writer prompt, or stop |
| **Label noise over 10 %** (any time) | the figure, its interval, the kinds of wrong label | change the filter, the writer, or stop |
| **The learning curve** (phase B) | the curve, the style gap, the recommendation | more notes, fine-tuning, or go on |
| **Opus cannot get a training run working** (phase C) | what was tried, the errors | the Codex training backup, or not |
| **Training beyond two overnight runs** | the time of one epoch, what was tried | yes or no; a free GPU or not |
| **A change of meaning of an existing label** (scheme 2.0), or a slot that needs a value type the contract lacks | the label or the slot, why, what must be relabelled or changed | yes or no |
| **Before the mirror's planner** (phase E) | the generation method's commit; how many agents the mirror and v3 need | the opt-in to a workflow; the permission rules, in the owner's Claude Code settings, for agents under the mirror folder, `C:\dz-hidden-v3` and the `-relabel` folder beside each (which role may read and write what: [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), section 1) |
| **Before the v3 planner** (phase E) | the mirror rehearsal's result: its label noise, drop rate, probe rate, the audit of its relabellers | go on to v3, or not |
| **The relabeller is under the probe bar twice** for the same notes | the probes' per-field rates | what to do |
| **A shortfall in v3's quotas** | the short cells with their counts | accept, or more rounds |
| **Running hidden v3** | the pre-commitment commit, the one command, what the sizes can prove | the owner runs it, once |
| **The hidden v2 comparison** (optional) | the question of [specs/v2-adapter.md](specs/v2-adapter.md) | accept the adapter, or drop the comparison |
| **Codex fails its bar** (two support checks in a row fail the seeded test; or waits over 10 minutes become usual) | the figures | try the local models as the second view, or go on within Claude |
| **Any account, any tool or download not listed in section 3, any spending** | what and why | yes or no |
| **A suspected exposure of a hidden set** | what happened, in one line, without the text | what to do |
| **Fine-tuning a generative model** (step 5) | the evidence | yes or no |
| **Committing a note set** | which set, its size | yes or no |

Between stop points, after each phase or batch: push, give the commit id and the short summary, **and continue**.

## 9. The budget of runs, and the forecast

State the count before every fan-out, and keep the ledger ([STATE-FORMAT.md](STATE-FORMAT.md)).

| | Opus runs | Sonnet runs | Codex calls | Main items |
|---|---:|---:|---:|---|
| Step 1 (32 units, two planted-error rounds) | about 165 | about 150 | about 75 | per unit: A, reconcile, adversary, verify (Opus); B, boundary cases, attack, and for a module C (Sonnet); the support check and the boundary view (Codex). Planted rounds: 22 attacks, 22 verifier runs, 22 support calls. Arabic checks of new slots' questions: about 11 |
| Deferred law work (phase D) | about 25 | about 150 | about 30 | triage 50 to 100 (the plan command prints the number); first page readings about 60; second readings only where the inventory says; settlements; Legal Review: 10 answers and 10 attacks |
| Step 3 | about 175 | about 235 | about 415 | Codex: pilot 25 to 30, training about 300, its halves of dev and challenge about 46, slot packs about 25 a slot. Sonnet: its halves 46, relabelling Codex's halves, the clean splits and the probe pool about 60, mirror and v3 planners and writers about 93, the third judge 18, realism 6, guide test and cells 5. Opus: relabelling Sonnet's halves 38, the mirror 24 and v3 about 70, the noise measurements about 30, realism 3, sorting 5, guide test and cells 2 |
| Steps 4 to 6 | about 12 | about 17 | 0 | step 5's judges (12 each); the Arabic checks of its templates (2); the rebuild (2) |
| **Total** | **about 375** | **about 550** | **about 520** | |

**These figures were recomputed on 14 November 2026 from the detailed specifications, after the cold read, and they are higher than the plan the owner approved the same day (345 Opus, 410 Sonnet, 435 Codex).** The plan had not counted: the writers and relabellers of the mirror set and of hidden v3 in full (about 190 runs between them), the Sonnet-written halves of dev and of the challenge set, the two planted-error rounds' own runs and support calls, the slot packs, and the triage's real number of issues. Nothing was added that the plan did not describe. Tell the owner the running totals against this table in every report.

**The laptop:** about 17 nights or free-day sessions: about 7 for the local filter (every note it must read: pilot 500, trial 400, training about 5,100, clean 1,300, 4,500 short phrase notes, at an estimated 25 to 30 seconds a note), 2 for writing the clean notes, 2 for the phrases, 3 for training runs, 2 for step 5's writers, 1 spare. The plan said 11: the filter's sheet is longer than first estimated, and the phrases and step 5's writing had not been counted. **The pilot report recomputes this from measured seconds per note**; more than 8 filter nights is a question for the owner at that stop point.

**Is the note generation feasible on this laptop?** On these estimates, yes: about 17 sessions before 10 December, with 23 nights available from 17 November. It is not measured: nobody has timed a local model writing or filtering a whole note. The pilot measures both, and its report gives the answer as a plain yes or no. A fallback without Codex (4,000 clean training notes written locally) would take about 12 more nights.

**Priority when a limit bites** (the first is protected most; the last gives way first): hidden v3 and the mirror set; dev and the challenge set; the rule checks; training volume. Do **not** save by skipping a blind relabel, the third judge of the mirror set, the seeded pairs or the planted errors.

**Forecast** (made on 14 November 2026; assumptions: about 10 working hours a day from 17 November; Codex's limits allow about 300 writing calls within a week; the laptop every night and on free days; the owner's answers within a day; no Claude usage limit stops the work):

| Milestone | About |
|---|---|
| Formal-notice closed; scheme frozen; pilot report (stop point) | 20 November |
| Four high-harm modules final; planted-error round 1 | 23 November |
| Learning curve at 4,000 notes (stop point) | 27 November |
| Step 5 result | 4 December |
| All 33 units final (texts still "to check") | 6 December |
| Deferred law work done; the reader in .NET, calibrated | 10 December |
| The owner's hidden v3 run | 12 December |
| The freeze rebuilt | 15 December |

Keep a line in each report: where the work stands against this table, and why if it has moved.

## 10. Reports

Every report to the owner is short and in plain language (the owner is a developer, not a lawyer and not a machine-learning specialist): **what is done** (with the commit id), **what is uncertain**, **what is next**, and, at a stop point, **the one question**. Numbers come with their meaning. Never round a result up. Never present a model's agreement as verification. When a target is missed, say so in the first paragraph. Where a result rests on checks inside one model family, say so in the same sentence (section 5b).

Reports are also committed. Under `docs/research/core-logic/reports/`: `pilot.md`, `learning-curve.md`, `reader.md`, `step-5.md`, `step-1-tracking.md` (kept up to date by the driver), `open-legal-questions.md` (by `unit.py questions`), `questions/batch-<n>.md`, `legal-review/`. Under `docs/research/core-logic/rules/reports/`: `planted-errors.md` and `.json`, `grades.md`. Under `docs/research/core-logic/law/`: `REPORT.md`. Under `docs/research/core-logic/results/`: the pre-commitments, `RUNS.md` and the summaries.

**Which report carries which proof sentence** (the owner: "State it wherever a result appears"). Each of these says, in its first lines, what its figures rest on:

| Report | The sentence it must carry |
|---|---|
| the phase 0 report; every batch report of the rules track; `step-1-tracking.md` (header); `open-legal-questions.md`; `questions/batch-<n>.md`; `rules/reports/grades.md` | derivations, attack, adversary and verifier are Claude models; Codex's support check is the one reading by another family; a belief the Claude models share is not caught; the triage behind the amendment ledger is one Sonnet reading per issue |
| `notes/GUIDE-TEST-RESULTS.md`; the cells table of `notes/SCHEME.md`; `pilot.md` (its guide-test part) | decided by two Claude models |
| `learning-curve.md`; `reader.md`; the phase C report | the Codex halves and the clean splits rest on two families; the Sonnet halves on labels two Claude models agreed on, with a code-made scenario |
| `step-5.md` | faithfulness is a script's; both judges of usability are Claude models |
| `law/REPORT.md`; the phase D report | which texts are `mechanical`, `twoFamilies`, `oneFamily`; settlements are Opus's |
| `legal-review-answers.md` | answers and attacks are Claude models' only; no lawyer checked them |
| the mirror's and v3's `PRECOMMIT.md` and summaries; the phase E report; `FREEZE.md` | writer and relabeller are both Claude models and a case is its writer's own; the probe rate and the third judge's figure are floors; a relabeller's tool use is not checked |

## 11. What this handbook did not settle

Honest gaps, so that you do not take silence for a decision:

1. **The reader's own time and memory in .NET.** The encoders were timed in Python on 13 November; the .NET reader is yours to measure.
2. **How long Codex takes to write 20 notes, and how many calls a window allows.** Nobody has measured it; the pilot does.
3. **How well a 4-billion-parameter local model filters notes.** The filter trial measures it; the 10 % noise bar is the guard if it is poor.
4. **How a period is counted** is for the `periods` module to derive. Until it is final, the evaluator's counting is an engineering placeholder behind one function.
5. **The style gap** between Codex-written training notes and Sonnet-written tests is unknown until the first dev scores.
6. **The module list** is a plan (section 6).
7. **What v3 can prove** is limited by its size.
8. **OpenAI's terms were not read** (section 2). The owner decided on the owner's own reading.
9. **What a Claude model writes that the notes are made from** (the sampler's code, the style cards, the English seed lines, the prompts, the guide, the cells table, the fixed Arabic lines for the local writers): **settled by the owner on 15 November 2026**, allowed as ordinary software work ([notes/TERMS.md](notes/TERMS.md)).
10. **What may change the writer prompt, the style cards and the sampler:** only findings on notes written by Codex or a local model (**the owner, 15 November 2026**; [notes/TERMS.md](notes/TERMS.md)).
11. **Whether a v3 relabeller peeks** cannot be known (section 5b, point 5).
12. **This revision was checked on the night of 14 November** by cold readers and critics ([reports/handbook-check-2.md](reports/handbook-check-2.md)). Its phases A to E have never been run, and **the fixes made after that check were not read cold again**. The first unit through the driver, and the pilot, are their real test.
