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

1. **The order, by impact on the software:** "the reader of the lawyer's notes comes first, the high-harm rules in parallel, the law clean-up later." Steps 3 and 4 start now, on a label scheme frozen now. Step 1 runs beside them, "high-harm modules first (limitation and periods with CPCA 504, labour conciliation, courts and sections and exits from the ordinary courts), then the claim types". Step 5 once a few claim types are final. Deferred until before the freeze: step 2's second pass, text passes beyond what a record needs, the remaining Journal triage, the Legal Review answers, "with rules on unverified text staying grade D ("to check", never block) until then". Hidden v3 and the freeze last.
2. **Codex has three roles, and no other:**
   - "Note writer: the pilot, the training notes, and development and challenge notes alongside Claude's (20 notes per call, 2 to 4 calls in parallel, each in its own empty scratch folder, read-only sandbox, event-log check)."
   - "Second family for checks: the support check, the blind view on disputed boundary cases, and the second page readings where no other two-family or mechanical proof exists, in the background. No attack on every record."
   - "Training backup only: training is run by Opus on the laptop. If Opus cannot get a training run working, ask me; with my approval, Codex may write and run the training code in its own separate folder (not read-only for that job), using only the training notes, never near C:\dz-hidden*, any repository other than its own copy, or a hidden set's results."
3. **Codex's speed:** "Today's figures already pass": about 10 Codex minutes per unit, in the background. The local models are tried as a second view "only if Codex fails".
4. **Terms.** "Codex writes the training notes, on my own reading that a small offline classifier for Algerian legal notes does not compete with OpenAI. I take responsibility for that reading." The owner's ChatGPT plan is a personal plan. **OpenAI's terms were not quoted, because OpenAI's page refused the fetch.** "Keep the writer of every note recorded, so the Codex notes can be replaced if ever needed." ([notes/TERMS.md](notes/TERMS.md))
5. **The filter on training notes is not Claude:** "code checks and a local model (Gemma 4 E4B or Jais-3-3.7B) decide keep or drop; Opus only measures label noise on a sample and stops to show me if it passes 10 %."
6. **Claude never writes training notes:** "Claude (Opus or Sonnet) writes test notes, the mirror set and hidden v3, never training notes, unless Anthropic authorizes it."
7. **What may tune the reader:** "anything that changes the reader (encoder choice, early stopping, temperature scaling, thresholds, head selection) is tuned only on the Codex half of dev and on clean notes. The Sonnet half is used only to measure and report, never to choose or tune. Same for the challenge set."
8. **Blocking.** A rule may block the clean export "only when the text was proved mechanically or by two families, the value is matched by script, derivations A and B agreed, no attack is open, and the planted-error round caught that kind of error 90 % of the time; otherwise the rule warns." A Codex check counts as the second family.
9. **Party kinds** in the label scheme are the app's relation and plaintiff-position lists and each event's actor.
10. **The laptop** writes and filters notes every night on mains, and by day whenever the owner says it is free.
11. **Process.** Light checks after each commit, the full CI before every push; one driver for a unit's stages; the owner's questions bundled per batch of units.

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
- **The two API keys.** `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` are Windows user variables, so every process inherits them. `research/core-logic/scripts/clean_env.py` removes them from every script that imports it (all of them do), from every Codex call, and from a shell command run as `python clean_env.py <command>`. Agents started by the Agent tool inherit the session's environment, which no script can change: **the session itself should be started through the wrapper** (section 7, "Every session"). Until it is, every agent prompt keeps its line "remove both keys before your first command". Never print, log or commit a key.
- The owner's name and email address never go into a prompt, a search, a request header or a file sent anywhere.

**Who may write what (terms).** Training notes are written by Codex, by the local models, or by code. **A Claude model never writes a training note, never labels one, and never decides whether one is kept.** Claude writes and relabels test sets, and measures. Every note records its writer, its filter and its relabellers ([notes/TERMS.md](notes/TERMS.md)).

**What may tune the reader.** A choice that changes the reader (the encoder, an epoch count, a temperature, a threshold, which head or rung is kept) is made only on notes written by Codex or by a local model: the Codex half of dev, the Codex half of the challenge set, and the clean-dev notes. Notes written by Sonnet (the other halves, the mirror set, hidden v3) only measure. The loader enforces this ([specs/notes-pipeline.md](specs/notes-pipeline.md), "The loader").

**Tools and downloads that are allowed.** Anything not on this list: ask the owner first.
- Already on the laptop: .NET 10; Python 3.12 with pytest (`C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`); the Python environment `C:\Users\TheShadow\dz-arch-research\venv` (torch 2.15.0+cpu, transformers 5.22.2, onnxruntime 1.31.1), left from Part 7, used as it is; `pdftotext` and `pdftoppm`; Windows PowerShell 5.1 (PowerShell 7 is **not** installed: never write `pwsh`); Codex CLI 0.167.0; Ollama with `gemma4:e4b`, `jais3-3.7b-low`, `jais3-0.9b-low`, `hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M` and `gemma3:4b`.
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

**After every commit: the light checks.** `python research/core-logic/scripts/check_commit.py` (about a minute): the commit touches nothing outside the two folders; `research/core-logic/CoreLogic.sln` builds with 0 warnings and its tests pass; `python -m pytest research/core-logic/scripts/tests` passes. **Before every push: the full CI**, on a CPU as idle as you can get: the root solution's build and tests, the research solution's, pytest, and one build of both solutions with SDK 10.0.115 through a temporary `global.json`, restored afterwards. Never delete, skip or weaken a test; never suppress a warning. Small commits, clear messages, ending with the `Co-Authored-By:` line of the model that made them. Nothing is built while an agent is running the rules tool, and nothing is built, tested or committed while a training or a timing runs.

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
| Derivation B; **third derivation of a module, by the flat route**; **attack on the finished record**; boundary-case writer; Official Journal triage; first page reading; checking a new slot's Arabic question | Sonnet 5.5 (fresh agents) | a different model from A; the record is Opus's, so Sonnet attacks it |
| **Note writer:** the pilot, the training notes, half of dev, half of the challenge set, the polarity families | **Codex** | the owner's decision of 14 November |
| **Second family for checks:** the support check; the blind view on disputed boundary cases; a second page reading where no other two-family or mechanical proof exists | **Codex** | the owner's decision of 14 November |
| Filter of training notes (keep, blank or drop) | code, then a local model: the better of Gemma 4 E4B and Jais-3-3.7B at the filter trial | never Claude (terms) |
| Label-noise measurement on training notes | Opus 5.5 (no tool) | measures only; changes no note |
| The other half of dev and of the challenge set; planners and writers of the mirror set and hidden v3 | Sonnet 5.5 (fresh, isolated for v3) | Claude writes test sets only |
| Blind relabelling: Codex-written test notes | Sonnet 5.5 (no tool) | two families |
| Blind relabelling: Sonnet-written test notes, the mirror set, hidden v3 | Opus 5.5 (no tool; isolated for v3) | one family: see section 5b |
| Third blind judge of the mirror set | Sonnet 5.5, by the closed question-sheet route | differs from the relabeller by model and by route |
| Legal Review answers | Opus 5.5 and Sonnet 5.5, independently; each then attacks the other's answer | two answers by two models |
| Guide test (and the scheme's 75 cell items); ranking of the two clean writers; the two judges of step 5 | Sonnet 5.5 and Opus 5.5, independently | |
| Realism review | Sonnet 5.5 for notes written by Codex or a local model; Opus 5.5 for notes written by Sonnet | a model that did not write the note |
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
5. **Judges use no tool.** A relabeller, a guide tester, a realism reviewer, the noise measurer, the sorter of dropped notes, a ranker or a page reader gets its inputs in the prompt (or as the listed image files) and answers in its reply. A script copies the answer from the agent's transcript and checks that no other tool was used; a run with any other tool use is discarded and run again.
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
| 29 | **Known-truth probes for the relabeller**: code-composed notes with certain labels, mixed blind into what Opus relabels. | The relabeller's own error rate would otherwise be unknown. |
| 30 | **The local filter is tested with decoys and planted contradictions, and Opus measures the label noise it leaves.** | A small local model that answers "yes" to everything would keep every wrong note. |
| 31 | **One driver for a unit's stages; the verifier receives every kind of finding at once** (first module). | Stages run by hand were skipped or run out of order; the verifier had to be run three times. |
| 32 | **"Done" never beside an open question** about the same step (first module, owner's rule). | A lawyer reads "notice: done" and misses the warning that its means is doubtful. |
| 33 | **Every prompt's output shape is in its body** (first module). | Three runs produced unusable output because the format was only in the header. |

## 5b. Where the proof is weaker than the first handbook's, and what compensates

The first handbook used a second model family against every model-made product. The owner's decision of 14 November keeps Codex for the closed checks and the note writing, and moves five roles to Claude models. Say what follows plainly **wherever a result of these roles appears** (a report table, a grade summary, the freeze document):

1. **The rules.** Derivations A, B and C, the attack, the adversary and the verifier are all Claude models. A legal belief they share (for example a French-law habit that Algerian law does not have) is invisible to all of them. Planted errors cannot measure this: nobody can plant what nobody knows. What still guards a rule: the quotes and the values come from scripts; **Codex's support check still reads every statement against its text**; the adversary must bring official sources; the Journal triage brings special laws as leads.
2. **The mirror set and hidden v3.** Their writer (Sonnet) and relabeller (Opus) are both Claude models, and **a case of these sets is its writer's own** (the owner's decision of 12 November: "from their own plan"), not made by code. So nothing outside the family checks one of their labels: a label error both models make survives. A note is kept only when the writer's labels and the blind relabel are equal on every field.
3. **The Sonnet-written halves of dev and of the challenge set** are relabelled by Opus: one family again, on sets that only measure. There the scenario is made by code, so a note is kept only when the code-made labels and the blind relabel are equal: one voice outside the family remains.

**Compensations, each with its own specification:**

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
| `periods` (high-harm) | Limitation and forfeiture: the general periods, **how a period is counted**, what starts, suspends and interrupts it, who may raise it, whether the court raises it of its own motion; the counting of procedural periods. | Civil Code 308 to 322 (308, 309, 312, 321 named by the brief); CPCA 405, 67 to 69 |
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
| **A** | Scheme 1.0 frozen; scorer; parsers; sampler; the pilot and the filter trial. **Stop point.** | The four high-harm modules |
| **B** | Dev and challenge sets; training notes to 4,000; the first rungs; the learning curve. **Stop point.** | Planted-error round 1; the other 7 modules; the 21 claim types |
| **C** | Fine-tuning if needed; clean notes; calibration; the .NET reader | Step 5, once 6 claim types are final |
| **D** | Slot packs and heads for the slots the records added | The deferred law work; planted-error round 2 |
| **E** | The mirror set; the pre-commitment; hidden v3; the freeze (step 6) | – |

Expected end: about 15 December 2026 (section 9).

### Every session

1. Read `C:\Users\TheShadow\dz-core-research\STATE.md` as [STATE-FORMAT.md](STATE-FORMAT.md) says. Do not act from memory of a conversation.
2. `python research\core-logic\scripts\clean_env.py --check` in your shell. It prints `clean`, or the names of the keys it was started with.
3. `git status`, `git log -3` in the worktree; read the last lines of `ledger\runs.jsonl`.
4. If Codex will be used today: `codex --version` must print `0.167.0`; then `python research\core-logic\scripts\codex_run.py probe2` must be **rejected** by the check.

| Situation | Do |
|---|---|
| The key check prints key names | Tell the owner once that day: "start VS Code with `python C:\Users\TheShadow\dz-fable-core\research\core-logic\scripts\clean_env.py code C:\Users\TheShadow\dz-legal-assistant` after closing every VS Code window". Go on; agent prompts keep their key line; every script and Codex call is clean anyway. |
| The ledger shows a run still "running" and nothing is running | Mark it `failed`. A derivation, a reconciled record or a verifier output from that run is deleted and the run is launched again fresh. A Codex batch: accepted answers are kept, the rest is queued again once. |
| Codex's version is not 0.167.0 | No Codex call. Run both probes, tell the owner, wait. |
| The probe is not rejected | No Codex call that day. Tell the owner. |
| The worktree has uncommitted files | Finished outputs: check them (schema, tests) and commit. Half-written agent outputs: delete, rerun. Never complete one by hand. |

### Phase 0. Close formal-notice and retool (about one day)

**Why first:** the module is one run from done, and every later unit needs the tools the first module showed were missing.

**Inputs.** `C:\Users\TheShadow\dz-core-research\runs\step1\formal-notice\rules-verify-followup2.prompt.md` (the prepared prompt); `rules/reports/formal-notice.attacks.followup2-input.json`; the working-folder scripts of 14 November (`stage4.py`, `stage6.py`, `run_codex.py`, `run_codex_c.py`, `run_boundary_view.py`, `followup_prep.py`, `text_pass.py`, `collect_batch.py`, `codex_queue.py`, `ingest_queue.py`).

**Do, in this order.**

1. **Launch the prepared run** (Opus 5.5, fresh): its prompt is the file above, unchanged. It applies the owner's rule 7 of 14 November to the three notice steps and changes the cite of statement `b12` from «civil-code 120 ¶2» to ¶1. Outputs: `rules/reports/formal-notice.attacks.followup2.json`, and a new `rules/final-candidates/formal-notice.json`.
2. **Run the scripts on the result** (from `research\core-logic\scripts`, with the Python of section 3; `<T>` is the rules tool, `<S>` the store file, both named in [specs/unit-driver.md](specs/unit-driver.md)):
   - `<T> check --store <S> ..\..\..\docs\research\core-logic\rules\final-candidates\formal-notice.json`
   - `python records.py quote <the same file>`
   - `python records.py check-values <quoted> --report <reports>\formal-notice.values.final.json`
   - `python records.py flag-neighbours <quoted> --report <reports>\formal-notice.flags.final.json`
   - `<T> examples <file>` and `<T> examples <file> --boundary ..\..\..\docs\research\core-logic\rules\boundary-cases\formal-notice.json --json`
   - move any new `proposedSlots` into `slots.json` (the rule of [specs/unit-driver.md](specs/unit-driver.md), stage 9)
   - `python records.py grade <quoted> --idmap … --differences … --attacks <reports>\formal-notice.attacks.json --attacks …attacks.followup.json --attacks …attacks.followup2.json --out …\final\formal-notice.json --report …formal-notice.grades.json`
   - `python records.py check-checklist …\final\formal-notice.json --checklist …formal-notice.checklist.json --flags …formal-notice.flags.final.json --report …formal-notice.checklist-check.json`
3. Update the module's row in [reports/step-1-tracking.md](reports/step-1-tracking.md): the grades, and the boundary count under the owner's runner.
4. **Retool** (code with tests, in `research/core-logic/scripts/` unless said):
   - `check_commit.py`: the light checks of section 3.
   - `unit.py`: the driver of [specs/unit-driver.md](specs/unit-driver.md). It absorbs the working-folder scripts listed above; after it, nothing of the pipeline lives outside the repository.
   - `codex_run.py`: the writer pool and the background queue of [specs/codex-calls.md](specs/codex-calls.md).
   - `records.py grade` and `store.py`: the proof label and the owner's blocking rule ([rules/GRADES.md](rules/GRADES.md), [specs/article-store.md](specs/article-store.md)).
   - `records.py ingest-reply`: copies a no-tool agent's JSON reply from its transcript, checks it against a schema file and writes it to its output path. Used for derivation C, the attack, the relabellers and every judge.
5. Commit after each item with the light checks; then the full CI; push.

**Done when.**
- `rules check` passes on `final/formal-notice.json`; `check-checklist` reports `ok: true`; no item is grade D for an unverified text; boundary cases bc6, bc8 and bc12 pass (expected 8 of 12; the other four are bc3, bc4, bc9, which two families hold wrong, and bc7, which is open).
- `unit.py status formal-notice` shows every stage done from the files alone.
- `check_commit.py` passes on its own commit; `tests/test_codex_run.py` covers the pool (two calls never share a scratch folder; a discarded call's notes go back to the queue once).
- Nothing of the pipeline remains only under `C:\Users\TheShadow\dz-core-research\` except working data.

| Situation | Do |
|---|---|
| bc6, bc8 or bc12 still fails after the run | Read the runner's message. If a step still says "done" beside an open boundary item, run the same prompt once more with that message added as an attack. If it fails again, put it in the owner's question batch and go on. |
| The run changed an example's expected outcome | Allowed only where the owner's rule requires it (a step that said "done" now stays open). List each in the tracking file. Any other changed expectation: discard the run, rerun fresh. |
| `rules check --store` fails on a cite | The store has no such paragraph. Do not edit the record: one more verifier run with the failure as an attack. |
| The full CI fails only on `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemory…` | Run that test alone three times. If it passes three times, push and say so in the report. Once a day, at a pause, run the full CI on an idle CPU until it is green. |
| Any other CI failure | Do not push. Fix the cause. |

**Report to the owner:** the commit id; formal-notice's final grades and boundary count; the tools added; anything uncertain.

### Phase A, reader track. The scheme, the yardstick and the pilot (about three days)

**Why this shape:** safeguards 15 to 17, 22 and 30. Specifications: [notes/SCHEME.md](notes/SCHEME.md), [specs/scorer.md](specs/scorer.md), [specs/scenario-sampler.md](specs/scenario-sampler.md), [specs/notes-pipeline.md](specs/notes-pipeline.md), [specs/reader.md](specs/reader.md) (rungs 1 and 2).

**Inputs.** `data/law/claim-elements.json`, `data/law/claims/*.json`, `slots.json`, the labelling guide, `notes/guide-test.json`, the contract `INoteReader`.

**Do, in this order.**

1. **Freeze scheme 1.0** ([specs/notes-pipeline.md](specs/notes-pipeline.md), section 1; 5 runs, say so first). Run [prompts/notes-guide-items.md](prompts/notes-guide-items.md) (Sonnet, one run) for 20 new guide-test items on what the revision added (the plaintiff's role, the public-body list, the eight slots' edge cases); then the guide test ([prompts/notes-guide-test.md](prompts/notes-guide-test.md)) with Sonnet and with Opus, no tool, on all 65 items; then the same two models on the 75 cell items that fill the table of `notes/SCHEME.md`, section 8. When no item is unexplained and the cells table is written, fill the hashes of `notes/SCHEME.md`, set its status line to `frozen at <commit id>`, commit, and write the commit id in the state file.
2. **The scorer** (`DzLegal.CoreLogic.Scoring`) with its six tests; record its hash in the state file **before any model exists**.
3. **The plain code of rung 2** (`DzLegal.CoreLogic.Reader`): the date and amount parsers and the polarity cue table, with their tests.
4. **The notes code** (`DzLegal.CoreLogic.Notes`): sampler v1, style cards, fictitious cast lists, `build-spans`, the code checks, the loader with its refusals.
5. **The pilot:** 500 scenarios, 25 Codex calls of 20 notes, with the pool at 2 writers, then 3, then 4 ([specs/codex-calls.md](specs/codex-calls.md)). State "25 Codex calls" to the owner before the first.
6. **The filter trial** on 100 pilot notes ([specs/notes-pipeline.md](specs/notes-pipeline.md), "The filter trial"); then the winning filter on all 500, at night or on a free day.
7. **The noise measurement:** Opus relabels 100 kept notes blind ([prompts/notes-noise-audit.md](prompts/notes-noise-audit.md), 4 runs of 25), a script lists the fields that differ from the kept labels, and one sorting run says which side is right ([specs/notes-pipeline.md](specs/notes-pipeline.md), "The noise measurement"). 5 Opus runs. It changes no note.
8. **The pilot report** `reports/pilot.md` and the calendar. **Stop point.**

**Done when.**
- `notes/SCHEME.md` is frozen, with its hashes and its cells table filled; the guide-test results file shows both testers on all 65 items.
- The scorer's six tests pass; its hash is in the state file.
- The parsers reach 98 % on the code-made value corpus (every written form in [specs/reader.md](specs/reader.md), rung 2).
- The loader's refusal tests pass (a test-set note in training; a Sonnet-written note for tuning; a note without provenance).
- The pilot report gives: seconds per Codex call and calls per window at 2, 3 and 4 writers; notes rejected by the code checks, by reason; the filter trial's table; notes kept, blanked, dropped, per polarity; the measured label noise with its interval; the calendar to 4,000 notes.

| Situation | Do |
|---|---|
| A guide-test item is answered differently by the two testers, or against its key | Fix the guide's wording, bump its version, rerun that item with both. Still different: the item's label becomes `unclear` (blank in training, dropped in tests) and it goes in the question batch. Do not freeze with an unexplained item. |
| A Codex call is rejected by the event-log check | Its notes are discarded and its scenarios go back to the queue once. Two rejections in a row from one writer: stop that writer, keep the others, tell the owner. |
| A usage-limit refusal | Stop the pool. Record the time. Do not retry before the window the refusal names. Report the calls made in the window: that figure is the calendar's input. |
| More than 30 % of a call's notes fail the code checks | Read ten failures. If one cause explains most (a format, a date written otherwise), fix the prompt once, bump its version, rerun two calls. Still over 30 %: stop point. |
| Neither local model passes the filter trial (catches under 80 % of planted contradictions, or rejects over 15 % of good notes) | Keep the better one for the pilot, mark every pilot note `filterWeak`, and put the question to the owner at the pilot stop point with the table. Do not start volume. |
| The noise measurement is over 10 % | Stop. Show the owner the figure, the interval and the three most common kinds of wrong label (described, with one fictitious example each). |
| The noise is between 5 % and 10 % | Go on; tighten the filter's sheet for the most common kind; measure again on the next 1,000. |
| The laptop is on battery, or the owner needs it | The filter does not run. It resumes from its queue; nothing is lost. |

**Traps.** The pilot's notes are training notes: no Claude model reads them except the noise measurer, and its answers change no note. Do not tune the filter on the notes the noise measurer read. Do not freeze the scheme after the pilot: its labels would then follow the notes.

**Report to the owner (stop point):** the pilot report; the calendar; the count of Codex calls and Claude runs needed for phase B; the one question: "go on to 4,000 training notes with this filter and this pace?"

### Phases A and B, rules track. One unit through the driver

**Why this shape:** safeguards 2 to 4, 8 to 14, 27, 31 and 32. Specifications: [specs/unit-driver.md](specs/unit-driver.md), [specs/record-checks.md](specs/record-checks.md), [rules/GRADES.md](rules/GRADES.md).

**The order of units.**

1. **Phase A, the high-harm modules:** `periods`, then `labour-prelitigation`, `court-sections` and `ordinary-court-exits`. `periods` goes first and alone, because it fixes how a period is counted and every deadline cites it; the other three may run their stages 0 to 4 beside it, but **no unit with a deadline is reconciled before `periods` is final**.
2. **Phase B:** planted-error round 1 on the five final modules. Then the other seven modules, three in flight: `territorial-jurisdiction`, `petition-formal-elements`, `proof`, `accessory-requests`, `land-registry-publication`, `conciliation-mediation`, `commercial-lease-regimes`.
3. **Then the claim types**, three in flight, the first six chosen so that step 5 can start: `unpaid-wages`, `abusive-dismissal`, `loan-repayment`, `unpaid-rent`, `commercial-debt`, `nafaqa`; then the remaining fifteen, area by area. A claim type is not reconciled before every module it imports is **final**.

**The stages of a unit.** `python unit.py next <unit>` runs the scripts that are due and prints the agent launches that are due, each with its model and its filled prompt file; `python unit.py done <unit> <stage> <agent id>` records a returned run. The driver writes the run ledger.

| # | Stage | Who | Prompt or script | Output |
|---|---|---|---|---|
| 0 | The unit's texts: every chapter its seed articles touch, and one heading up | script | `records.py unit-texts <unit> --seed` | `reports/<unit>.texts.json` |
| 2 | Derivation A and derivation B, blind to each other | Opus 5.5; Sonnet 5.5 | `rules-derive-a.md`, `rules-derive-b.md` | `derivations/a|b/<unit>.*` |
| 2c | Third derivation (modules only), flat | Sonnet 5.5, no tool | `rules-derive-c.md` | `derivations/c/<unit>.json` |
| 2d | Boundary cases, blind to all records; never edited afterwards | Sonnet 5.5 | `rules-boundary-cases.md` | `boundary-cases/<unit>.json` |
| 3 | The derivers' text requests | script; readers only for a text a rule must cite | [law/README.md](law/README.md) | `law/requests.jsonl`, the store |
| 4 | Check, quote, values, flags, merged checklist, A against B by field and by case, C's comparison. **Commit.** | scripts | [specs/record-checks.md](specs/record-checks.md) | `reports/<unit>.*` |
| 5 | Reconcile | Opus 5.5 | `rules-reconcile.md` | `reconciled/<unit>.json`, `reports/<unit>.reconciliation.md` |
| 6 | Scripts again; the id map completed; the boundary cases run | scripts | | `reports/<unit>.*` |
| 7 | Adversary (web); attack; support check with seeded pairs | Opus 5.5; Sonnet 5.5; **Codex** (queue) | `rules-adversary.md`, `rules-attack.md`, `rules-support-check-codex.md` | `reports/<unit>.attacks.*.json`, `.support.json` |
| 8 | Verify **everything at once**: the three attack lists, C's differences, the unanswered neighbour flags, the boundary cases the record fails | Opus 5.5 | `rules-verify.md` | `reports/<unit>.attacks.json`, `final-candidates/<unit>.json` |
| 8b | Blind view on boundary cases the verifier set aside | **Codex** (queue) | `boundary-second-view-codex.md` | `reports/<unit>.boundary-view.json` |
| 9 | Scripts again; slots; flatten (claim types); grade; checklist check; the tracking row | scripts | | `final/<unit>.json` |

**In parallel:** stages 2, 2c and 2d of one unit; the same stage across up to three units. Say the number of runs before each launch (a module: 8 Claude runs and 2 Codex calls; a claim type: 7 and 2; one more verifier run whenever stage 8b, a leftover flag or a late support check asks for it).

**The owner's questions, bundled.** Each unit's reconciler and verifier leave open points. The driver collects those marked "for the owner" into `reports/questions/batch-<n>.md`, one file per three units: each question in one plain sentence, the readings, **the protective reading the record already applies**, and what would change with each answer. The work never waits for a batch.

**Done when, for a unit.** `rules check` passes on the final record; every worked example passes; `check-checklist` is `ok`; no final record has a `proposedSlots` entry; every statement has a quote, a source, a grade and a proof label written by script; every difference between A and B has an outcome; every attack has a verdict with a quote; the unit's row in the tracking file is filled (agreement before reconciliation, examples changed by a deriver, boundary cases passed, cites outside the texts given).

| Situation | Do |
|---|---|
| A deriver's run ends without its record, or its record fails `rules check` | Delete what it wrote. Launch the same prompt fresh. Never repair it. Two failures: read the tool's message; if the prompt or the guide is at fault, fix it (version bump), then a third run. |
| A deriver requests a text the store lacks | If a rule of the unit must cite it: fetch and read it now (first reading Sonnet, second Codex, mechanical proofs; [law/README.md](law/README.md)), then rerun **that derivation** fresh. If it is only context: list it; it waits for phase D. |
| A deriver changed one of its own examples after running the tool | Allowed when only the expected questions changed and the rule did not. Record it in the tracking file. If an expected effect changed: keep the run, and add the example as a case difference for the reconciler. |
| Stage 0 lists chapter members missing from the store | Fetch only those in a chapter that holds one of the unit's own pointer articles. The others are listed in `texts.json` as `missingFromStore` and wait for phase D. |
| The unit cites an instrument whose triage is open (Commercial Code, Family Code, labour laws) | Go on. Its items cannot be grade A (`triageOpen`). The driver gives the adversary every untriaged title or operative hit that names the instrument, as leads to read in the Journal. |
| A and B agree on under 40 % of compared fields | Go on to the reconciler; say so in the tracking file. Under 25 % on two units running: stop and read both prompts for a shared misunderstanding before the next unit. |
| The reconciler adds an item neither derivation had | Kept; graded `addedAfterDerivation` (never A). The verifier is told to test it first. |
| The support check accepts more than one seeded false pair in five | Its "yes" answers in that call do not count. Run the call once more with fresh pairs. Again: stop Codex for this role and tell the owner (the owner's condition for trying the local models). |
| A unit waits more than 10 minutes for the Codex queue, or Codex refuses for a usage limit | Record it. The unit goes on without the Codex answer; its items carry `supportCheckPending` and cannot be A until the call returns. Tell the owner at the next report. |
| The verifier sets a boundary case aside | Stage 8b. Codex agrees with the verifier: the case stays a counted failure with the two-family reason. Codex agrees with the case's writer: the case becomes **uncertain**, and one more verifier run makes the record keep both readings and warn. |
| A step says "done" while another item still asks or warns about the same step | The runner reports it. One more verifier run with the owner's rule 7 of 14 November as the attack. |
| After stage 9 a neighbour flag is unanswered | It comes from a statement the verifier added. One more verifier run with the flags as attacks. This should be rare: stage 8 already hands the verifier every flag. |
| A store correction changes the paragraph numbers of a cited article | One verifier run changes the cites. Never by script. |
| A unit's item is grade D for an unverified text | Expected before phase D. It warns, marked "to check". List it; do not start a text pass for it. |
| You "know" a record is wrong | Raise an attack with a source and send it through the verifier like any other. |

**Traps.** Deriving a claim type before its modules are final bakes a module's unsettled point into many records. Sonnet's B is not a lesser A: check the reconciliation reports for "A's reasoning" used as a reason. The adversary will bring many tier-3 leads; an unconfirmed specific lead becomes an open point, not a dismissal. Case law is not in the store: a rule that needs a Supreme Court principle is `interpretive`, grade C at best. Building the research solution replaces the rules tool: never while an agent that runs it is working.

**Planted-error rounds** ([specs/planted-errors.md](specs/planted-errors.md)). Round 1 after the fifth final module; round 2 after the sixth final claim type. Until round 2 a kind of error counts as caught only at 5 of 5.

**Report to the owner, after each batch of three units:** the commit id; units final; for each: agreement before reconciliation, attacks fixed, refuted and open, grades with how many may block, boundary cases passed; the proof sentence of section 5b; the question batch file; what is next.

### Phase B, reader track. The sets, the volume and the first rungs (about a week)

**Why this shape:** safeguards 16, 18, 20 and 23. Specifications: [specs/notes-pipeline.md](specs/notes-pipeline.md), [specs/reader.md](specs/reader.md), [specs/probes.md](specs/probes.md).

**Do.**

1. **Dev and challenge sets.** Dev: 600 notes, 300 written by Codex and 300 by Sonnet, from scenarios drawn the same way. Challenge: 12 difficulties of 50, half and half. Polarity families (about 300 groups of 3 to 4) sit inside the sets: about 200 groups in train, 100 in dev and challenge, each group written in one call by one writer. Relabel blind: Codex-written notes by Sonnet, Sonnet-written notes by Opus ([prompts/notes-judge.md](prompts/notes-judge.md), 25 notes a run, no tool). A test note is kept only if the scenario's labels (which are the writer's) and the blind relabel are equal on every field.
2. **Training notes to 4,000 kept.** Codex writes; the code checks and the local filter decide; blank-and-keep under the owner's rule; refills for the polarities and slots that lose most. Opus measures the noise on 100 of every 1,000 kept.
3. **Composed notes.** The better local writer supplies the phrases ([specs/notes-pipeline.md](specs/notes-pipeline.md), "Composed notes"); code composes; at most a quarter of any training set.
4. **Rungs 3 and 4** of [specs/reader.md](specs/reader.md): the linear baseline; the encoder choice at 1,000 notes; the frozen encoder with its heads at 1,000, 2,000 and 4,000, three seeds, with and without composed notes. Every choice on the Codex half of dev.
5. **The learning-curve report** `reports/learning-curve.md`. **Stop point.**

**Done when.** Each set meets its "done when" in [specs/notes-pipeline.md](specs/notes-pipeline.md); every kept note is schema-valid with its provenance; the curve report shows, per point and per seed, each target of [specs/scorer.md](specs/scorer.md) on the Codex half and on the Sonnet half of dev, and the challenge set per difficulty; the probes of [specs/probes.md](specs/probes.md) are reported.

| Situation | Do |
|---|---|
| The Sonnet half of dev scores more than 5 points under the Codex half | Report it as the style gap. Do **not** tune on the Sonnet half. Allowed answers, in order: more style cards for the Codex writer; a larger composed share up to its cap; then fine-tuning (phase C). |
| A polarity or a slot has under 60 known examples at 4,000 | Targeted scenarios for it in the next Codex calls, before the curve report. |
| Composed notes lower the Codex-half score at two of the three points | Drop them from the shipped recipe; say so. |
| The curve still rises by more than 1.5 points from 2,000 to 4,000 and a target is missed | The report recommends more notes, with the Codex calls and nights they cost. The owner decides. |
| The curve is flat and a target is missed by more than 2 points | The report recommends fine-tuning (phase C), not more notes. |
| MSA and mix are within a point | Keep mix. |
| The relabel drops more than 35 % of a writer's test notes | Read the dropped fields' counts by kind. One kind dominates: fix the guide or the writer prompt, rerun that batch. Otherwise go on; the rate is reported. |

**Report to the owner (stop point):** the curve; what it means in one paragraph; the style gap; the recommendation; the one question: "more notes, fine-tuning, or go on as it is?"

### Phase C. The reader finished, and step 5 (about a week)

**Reader track** ([specs/reader.md](specs/reader.md), rungs 5 to 9).

1. **Fine-tuning**, only under the rule of [specs/reader.md](specs/reader.md) ("Rung 5"); two overnight runs at most.
2. **Clean notes:** the trial (20 notes from each local model, the same scenarios), then 1,000 by the better writer: 700 for training, 100 clean-dev, 200 as the clean test split. Then the comparison "clean against Codex-trained" at equal size.
3. **Calibration and abstention** on the Codex half of dev and on clean-dev.
4. **The .NET reader:** `DzLegal.CoreLogic.Reader` implements `INoteReader`; parity on 200 dev notes; time, memory and bytes on mains.

| Situation | Do |
|---|---|
| A training run fails (a crash, a loss that does not fall, an out-of-memory error) | Two more attempts, each with one named change. After the third failure or one lost day: stop and ask the owner. Only with the owner's approval does the Codex training backup start ([specs/codex-calls.md](specs/codex-calls.md)). |
| The 8-bit model loses more than a point against the 32-bit one | Static quantisation with 200 training notes for calibration; then, if needed, one fine-tuning epoch with the quantised weights simulated. Report both figures. |
| The reader in .NET differs from Python by more than 0.01 on a confidence | A tokenizer or offset fault. Find the first differing token on the parity notes; do not relax the bar. |
| A note takes more than one second | Measure per stage. Usual cause: more than two windows. Cap the windows at three and report the notes it affects. |
| The clean-trained reader is within 2 points of the Codex-trained one on claim type and polarity | Tell the owner: the owner's decision of 12 November then plans the shipped reader on clean notes only. Both candidates go to the mirror set. |
| Training would need more than two overnight runs | Stop point. |

**Step 5** ([specs/step5-templates.md](specs/step5-templates.md)), once six claim types are final. In the app today, thirteen of the petition's fourteen sections are already written by templates, and one by the model: the legal discussion. So step 5 measures the two sections a model could still be needed for: **the legal discussion** (one paragraph per element, from the record's articles and the confirmed facts) and **the facts**. 120 fact sheets; each part written by a template and by the four local models; the faithfulness script on every output; two blind judges (Sonnet and Opus) on a sample, 24 runs; time, memory and the bytes of each set-up; about two laptop nights. The requests stay the lawyer's own lines, as the app's own audit decided. **Done when** `reports/step-5.md` holds the one table (for each section, the smallest set-up that is faithful 98 % of the time and usable 90 % of the time) and the two answers: how much templates can write, and whether the app can work under 1 GB. **Nothing is fine-tuned without the owner's word.**

**Report to the owner:** the commit id; the ladder table with intervals; size, time and memory; the step 5 table and its one-sentence conclusion; what did not work.

### Phase D. The slots the records added, and the deferred law work (about four days)

**Reader track.** For each slot the final records added, decide by the table of [specs/scheme-extension.md](specs/scheme-extension.md) whether it stays a question for the lawyer or becomes a reader output. For those that do: the guide row, the six guide-test items, the note pack, the head, the proof that older outputs did not change. The scheme becomes 1.1.

**Rules track**, in this order:

1. **The remaining Journal triage:** the title and operative hits of the Commercial Code, the Family Code and the labour laws ([prompts/journal-hit-triage.md](prompts/journal-hit-triage.md); about 67 Sonnet runs, 8 at a time; say the number first). Then `python ledger.py build` and `law/triage-status.json`.
2. **The text pass:** `python records.py inventory` on every final record; a page reading for every cited article that is not verified, grouped by issue (an article in critical use on one page or a pair of pages per run). First reading Sonnet. **A second reading by Codex only where the proof needs one:** the labour laws, an article added to the delta, a corpus text whose own check was a Claude reading. Settlements by Opus on full-resolution tiles. Corrections go to the delta.
3. **Step 2's second pass:** the delta trimmed to what the final records cite; `law/amendment-ledger.json`; `law/REPORT.md`; the downloaded PDFs deleted ([law/README.md](law/README.md), "Done when").
4. **Regrade every record;** planted-error round 2; the grade report ([rules/GRADES.md](rules/GRADES.md), last section).
5. **The Legal Review answers** ([prompts/legal-review-answer.md](prompts/legal-review-answer.md), [prompts/legal-review-attack.md](prompts/legal-review-attack.md)): the 22 questions by section, one answer by Opus and one by Sonnet, each attacked by the other; you write `docs/research/core-logic/legal-review-answers.md`.

| Situation | Do |
|---|---|
| The triage finds a text that amends an article a final record cites | Fetch the amending text; add the wording to the delta; rebuild the store; one verifier run on each record that cites it, with the change as the attack; regrade. |
| A page reading differs from the store | The settlement rule of [law/README.md](law/README.md). A difference of paragraph division only: a `paragraphDivision` correction, as for Civil Code 120. |
| A reader copied the article's heading | The comparison already drops it (`reading.drop_heading`). |
| A text stays unverified after the pass | Its items stay grade D and are listed in the grade report, with the reason. |
| The scan misses a known amendment | Say the scan is not trusted; every `ledgerClean` false; grades fall to B. Do not patch the scan by hand. |

**Report to the owner:** the commit id; the figures the brief asks for (cited articles checked by script, by a reading, corrections found); the corrections for the corpus and for the library; the final grades and how many items may block; the planted-error rates; the Legal Review answers' file.

### Phase E. The mirror set, hidden v3 and the freeze (about a week)

Specifications: [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), [specs/scorer.md](specs/scorer.md), [prompts/freeze-rebuild.md](prompts/freeze-rebuild.md).

1. **Freeze the generation method** (commit id recorded).
2. **The mirror set** (about 440 kept notes in v3's proportions), with the v3 roles, prompts and commands, in an open folder: Sonnet writes, Opus relabels with the known-truth probes mixed in, a fresh Sonnet is the third judge by the question-sheet route. It is the rehearsal of every v3 command. **Stop point before any v3 writer runs.**
3. **Score the at most two final candidates on the mirror set, once.** Write `PRECOMMIT.md`; push.
4. **Hidden v3:** `v3 init`, planner, writers, validate, relabel, compare, refills, probes, finalize. **The owner runs the one scoring command, once.**
5. **Step 6:** `docs/research/core-logic/FREEZE.md`; a fresh Sonnet rebuilds from it in a clean clone.

| Situation | Do |
|---|---|
| A reply from a v3 writer or relabeller holds text | Stop, discard the batch, tell the owner. |
| The memory folder changed after a hidden-set agent ran | Stop, tell the owner in one line, without any text. |
| A quota cell is short after three refill rounds | Stop point: the cells and their counts. |
| The relabeller is under 95 % on the known-truth probes | Its labels are not trusted for that batch: rerun the batch with a fresh Opus; under 95 % again: stop point, with the per-field rates. |
| The mirror score is more than 3 points under the Sonnet half of dev | Say so in `PRECOMMIT.md`. Nothing is tuned; the mirror chooses nothing. |
| A rebuilt figure differs from the freeze document | Fix the document or the command, never the figure. |

**Traps.** The mirror set chooses nothing. A hidden-set command is debugged on the mirror set, never by looking. Never change the access rights of a hidden folder. Balanced sets are not realistic sets; say so beside every score. After the v3 run nothing is tuned on what the summary shows. With 100 notes a 90 % target is "met" only from 96 right; with 200 notes a 95 % target only from 197: several v3 targets will most likely come out "inconclusive". Say this to the owner before the run.

**Report to the owner:** the commit id; each target as met, not met or inconclusive, the worst test set of at least 200 notes as the headline; the mirror's prediction beside the v3 result; the bias sentence of section 4, rule 7; what the reviewer needs to integrate.

## 8. Stop points: when you must wait for the owner

At each of these: commit and push what is done; update the state file; make sure nothing unapproved is running; send a short plain report with the exact question; **wait**. Quote the answer in the state file before you continue.

| Stop point | What you bring | What the owner decides |
|---|---|---|
| **The pilot** (phase A) | the pilot report, the calendar, the runs and calls phase B needs | go on to volume; the pace |
| **Label noise over 10 %** (any time) | the figure, its interval, the kinds of wrong label | change the filter, the writer, or stop |
| **The learning curve** (phase B) | the curve, the style gap, the recommendation | more notes, fine-tuning, or go on |
| **Opus cannot get a training run working** (phase C) | what was tried, the errors | the Codex training backup, or not |
| **Training beyond two overnight runs** | the time of one epoch, what was tried | yes or no; a free GPU or not |
| **A change of meaning of an existing label** (scheme 2.0) | the label, why, what must be relabelled | yes or no |
| **Before the v3 writers** (phase E) | the mirror rehearsal's result; how many agents | the opt-in to a workflow; the permission, in the owner's Claude Code settings, for agents to read and write under the mirror folder, `C:\dz-hidden-v3` and the `-relabel` folder beside each |
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
| Step 1 (32 units, two planted-error rounds) | about 165 | about 140 | about 55 | per unit: A, reconcile, adversary, verify (Opus); B, boundary cases, attack, and for a module C (Sonnet); support check and boundary view (Codex). Planted rounds: 22 attacks, 22 verifier runs |
| Deferred law work (phase D) | about 25 | about 135 | about 30 | triage 67; first page readings about 60; second readings only where needed; settlements; Legal Review: 10 answers and 10 attacks |
| Step 3 | about 175 | about 240 | about 415 | Codex: pilot 25, training about 300, its halves of dev and challenge about 46, slot packs about 45. Sonnet: its halves 46, relabelling Codex's and the clean splits about 54, mirror and v3 planners and writers about 93, the third judge 18, realism, guide test. Opus: relabelling Sonnet's halves 38, the mirror 24 and v3 about 70, the noise measurements 25, realism, sorting |
| Steps 4 to 6 | about 12 | about 15 | 0 | step 5's judges (12 each); the Arabic checks of its templates; the rebuild |
| **Total** | **about 375** | **about 530** | **about 500** | |

**These figures were recomputed on 14 November 2026 from the detailed specifications, and they are higher than the plan the owner approved the same day (345 Opus, 410 Sonnet, 435 Codex).** The plan had not counted: the writers and relabellers of the mirror set and of hidden v3 in full (about 190 runs between them), the Sonnet-written halves of dev and of the challenge set, the two planted-error rounds' own runs, and the slot packs. Nothing was added that the plan did not describe. Tell the owner the running totals against this table in every report.

**The laptop:** about 14 nights or free-day sessions (4 for the filter, 3 for the clean notes, 3 for training runs, 2 for step 5's writers, 2 spare). The plan said 11: the filter's sheet is longer than first estimated (about 20 to 25 seconds a note, to be measured at the filter trial), and step 5's writing had not been counted.

**If something must give:** hidden v3 and the mirror set first, then dev and the challenge set, then the rule checks, and training volume last. Do **not** save by skipping a blind relabel, the third judge of the mirror set, the seeded pairs or the planted errors.

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

Reports are also committed under `docs/research/core-logic/reports/`: `pilot.md`, `learning-curve.md`, `step-1-tracking.md` (one row per unit, kept up to date by the driver), `questions/batch-<n>.md`, `planted-errors.md`, `step-5.md`, `step-2b.md`, `reader.md`, `legal-review/`.

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
9. **This revision was checked on the night of 14 November** by cold readers and critics ([reports/handbook-check-2.md](reports/handbook-check-2.md)). Its phases A to E have never been run. The first unit through the driver, and the pilot, are their real test.
