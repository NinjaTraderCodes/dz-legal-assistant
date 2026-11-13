# Handbook: the core logic of DZ Legal Assistant, steps 1 to 6

*Written by Fable 5.2 on 12 and 13 November 2026 for the agent that carries out the work (Opus 5.5). You have not seen the conversation in which this was planned. Everything you need is in this file and the files it links to. Read this file to the end before you do anything; then read the brief ([brief/FABLE-CORE-LOGIC-BRIEF.md](brief/FABLE-CORE-LOGIC-BRIEF.md)).*

**State of things when this handbook was handed over:** step 0 (closing Part 7 of the architecture report) is the author's job and is not yours. The worktree, the branch `fable/core-logic` and the research frame exist. **Step 1 has not been started. Nothing has been derived, generated or trained.**

**The brief and the owner's later decisions.** The brief is the owner's statement of the task. Where the owner's decisions of 12 and 13 November (section 2) differ from it, **the decisions win**. The known differences: hidden set v3 has about 1,100 notes, not "at least 10 per claim type and 30 near-miss"; the builder launches the v3 commands that print counts, and only the scoring run is the owner's; "a forfeiture blocks export" means the clean export only; a note's two labellers are its writer and one blind judge of another family; and a grade is given per item of a record, not per JSON field (an item's fields rest on the same texts).

## 0. How to use this handbook

| If you need | Read |
|---|---|
| what the task is and why it is built this way | sections 1 and 5 |
| what the owner decided and what is forbidden | sections 2 and 3 |
| which model plays which role | section 4 |
| what to do next, step by step, with "done when" | section 7 |
| when you must stop and ask the owner | section 8 |
| where you are after a stop | [STATE-FORMAT.md](STATE-FORMAT.md), then `C:\Users\TheShadow\dz-core-research\STATE.md` |
| the format of a rule record | [rules/rule-record.schema.json](rules/rule-record.schema.json), [rules/DERIVER-GUIDE.md](rules/DERIVER-GUIDE.md), section 6, [rules/examples/](rules/examples/README.md) |
| how sure a rule is, and what it may do | [rules/GRADES.md](rules/GRADES.md) |
| the missing articles and the corrections | [law/README.md](law/README.md), [law/law-delta.schema.json](law/law-delta.schema.json) |
| how a note is labelled | [notes/LABELLING-GUIDE.md](notes/LABELLING-GUIDE.md), [notes/note.schema.json](notes/note.schema.json) |
| the questions the app asks the lawyer | [slots.json](slots.json) |
| the exact prompt of an agent or a Codex call | [prompts/README.md](prompts/README.md) |
| what a script must do | [specs/](specs/): `article-store`, `official-journal-scan`, `record-checks`, `rule-evaluator`, `codex-calls`, `scenario-sampler`, `notes-pipeline`, `probes`, `scorer`, `reader`, `hidden-v3-commands`, `v2-adapter` |
| what the reviewer will integrate, and how | [INTEGRATION-CONTRACT.md](INTEGRATION-CONTRACT.md) |
| the code that already exists | `research/core-logic/README.md` |
| how this handbook was checked, and what the check changed | [reports/handbook-check.md](reports/handbook-check.md) |

**When the handbook is silent or wrong.** It will be, somewhere. Then: (1) go back to the reason given for the step (every step and check below has one) and choose what serves that reason; (2) choose the option that protects the lawyer and the hidden sets; (3) write what you decided, and why, in the state file and in your next report. If the choice touches a hard rule of section 3 or a stop point of section 8, do not choose: ask the owner. **Fix the handbook when you find it wrong**, in the same commit as the work that showed it, and say so in the report.

## 1. The goal, and the one fact that shapes everything

The app drafts an Algerian statement of claim (عريضة افتتاح دعوى) on an ordinary laptop, offline. The architecture report found that what can hurt a lawyer is not the prose: it is **the decisions**: the claim type, the articles, the deadlines, the steps required before filing, the competent court and section. Those are rules, not text generation. And Part 7 measured that no ready-made model under 1 GB reads a lawyer's notes well enough.

So this work builds the app's **core logic**:

1. **Rules** for the 21 claim types: complete, machine-readable, each resting on the official text (steps 1 and 2).
2. **Notes**: a large body of realistic lawyer's notes with exact answers, and a hidden test set (step 3).
3. **A classifier and a reader** that turn a note into the facts the rules need, showing the words behind each answer (step 4).
4. A measured answer to: is a generative model still needed, and must one be fine-tuned? (step 5)
5. A freeze from which every figure can be rebuilt (step 6).

**The one fact: there is no human reviewer.** No lawyer will check a rule or a note. A wrong deadline that looks right would reach a lawyer's desk. Therefore nothing here may rest on a model's say-so, including yours. Every product carries its own proof, and the proof is of three kinds, used together everywhere:

- **The official text**, quoted by a script, never typed by a model.
- **Independence**: two derivations that did not see each other; a writer and a judge of different model families; a hidden set written from another plan.
- **Attack**: an agent whose only job is to break the result, and planted errors that show whether the checkers can find anything.

And one rule for when proof runs out: **say so, and choose the reading that protects the lawyer.** Warn rather than stay silent; ask rather than guess; never block on a reading.

This is why the method is heavier than "derive the rules, generate notes, train a model". Each extra piece exists because, without it, a specific error would pass unseen. Section 5 lists those pieces with the error each one stops. **When you are tempted to skip one to save time, read its line there first.**

## 2. The owner's decisions

The owner is the product owner and developer. A reviewer (another Claude) writes briefs and integrates; the owner relays. These decisions are binding. Words in quotation marks are the owner's.

### 12 November 2026

**Money.** "No API and no spending at all: the hard rule is $0. Do not use OpenRouter or any paid service. Use what is already on this laptop, under existing subscriptions."

**The two model families.**
- Codex CLI (`codex exec`) writes notes, "about 20 notes per call, in an empty scratch folder outside every repository with a read-only sandbox".
- "Claude subagents of this session are the second model family: Codex writes, Claude judges, and the reverse for a cross-check sample."
- Codex also reviews the rule records in step 1 and is the second blind judge in step 5.
- "Before generating, quote the current OpenAI and Anthropic terms on using outputs to train a model, and record the writer and judge of every note, so the training notes can be replaced later if needed."
- "Respect Codex's usage limits: work in batches across the day and report progress; start with 4,000 training notes and show me the learning curve at 1,000, 2,000 and 4,000 before generating more."

**Training.** "Laptop first, never rent." If training time becomes the bottleneck, a free GPU only (Kaggle Notebooks, or Google Colab's free tier). "Upload only the fictitious notes and the code, never anything from a hidden set, and no payment details. Ask me before creating any account."

**Hidden set v3.** Writer: isolated Claude subagents, used only for this, write v3 from their own plan straight into `C:\dz-hidden-v3` and return counts only ("no note, no label, no excerpt in its reply"). Checker: "a fresh Codex session relabels every note blind; your script checks each label against the text; disagreements are dropped and refilled. Only counts come out." "Do not use Gemma or Jais as a v3 judge." Size: about 1,100 notes (40 per claim type; 200 near-miss; about 60 more for the two rarest slots; every slot at least 100 times; at least 150 denied and 150 disputed events). "Every slot" is read as: every slot the reader reads from the note, with a value other than "unknown".

**Clean notes.** "Yes, 1,000 clean notes. First write 20 with each of Jais-3-3.7B and Gemma 4 E4B and keep the better writer of formal legal Arabic. If the classifier trained on the clean notes comes close to the one trained on Codex notes, plan for the shipped classifier to be trained on clean notes only, and keep the Codex notes for development, judging and testing."

**The Official Journal scan** from 2003 to today, approved with four additions: search the code names as well as the law numbers ("code civil", "code de commerce", "code de procédure civile et administrative", "code de la famille") with "modifié", "complété", "abrogé"; "be polite to joradp.dz: respect its robots.txt, one download at a time with a pause of a few seconds, and resume where you stopped rather than restart"; "keep a list of every issue read (number, date, hash, matches found) as evidence, and delete the PDFs after reading"; "the check must re-find the known amendments (Laws 05-02, 05-10, 07-05, 22-13); if it misses one, the scan is not trusted and you say so." (The number 05-02 covers two texts of 2005: Law 05-02 on the Commercial Code and Ordinance 05-02 on the Family Code. The scan must re-find all five texts.)

**Disagreements between writer and judge.** "Blank the field, keep the note, in training notes only, with three limits": drop the whole note if the judges disagree on the claim type or on more than about a fifth of its fields; report the rate of blanked fields per slot and per polarity; write targeted notes to refill those cases. "In every test set (development, the mirror test, hidden v3), drop the whole note on any disagreement."

**Depth of step 1.** One unit per claim type.

**The six changes made when the plan was approved:**
1. The author of this handbook does step 0, the worktree and the handbook, and stops. **You carry out steps 1 to 6.**
2. A model is named for every role (section 4).
3. The integration contract is defined now ([INTEGRATION-CONTRACT.md](INTEGRATION-CONTRACT.md)).
4. **"Block" means what it means in the app today:** the clean export is blocked and the export with the «مسودة» mark stays allowed. Never a hard stop.
5. The optional run of the finished reader on hidden v2 counts only if its answers are scored by the unchanged Alpha-01 scorer through the owner's command ([specs/v2-adapter.md](specs/v2-adapter.md)); otherwise the comparison is dropped.
6. Notes composed by code from short phrases: marked, capped at a quarter of any training set, never in a test set, and the classifier is measured with and without them.

### 13 November 2026

1. **No folder locks.** "Do not change the permissions of C:\dz-hidden-v2, and do not lock C:\dz-hidden-v3 either." "The protection for both hidden sets is the no-open rule, Codex's empty scratch folder with a read-only sandbox, and the Codex event-log check that discards any call that opened a file or ran a command."
2. **Step 3's count** ("about 320 agent runs and 440 Codex calls"): "accepted for now. Make the pilot a stop point where the four ways to spend less are decided with its measured drop rates and Codex's measured calls per window."
3. **The grade rule.** "A rule resting on an unverified text must still warn, and is also marked "to check"; it can never block the clean export and never be silent. "To check" is never weaker than a warning."

**The owner watches usage.** Before any fan-out of agents, say how many runs it is (section 9 has the budget). Prefer the lean way when two ways serve the purpose equally.

## 3. Hard rules

Breaking one of these is not a judgment call. If a rule stops you, stop and tell the owner.

**Hidden sets.**
- Never open, list or search `C:\dz-hidden*` (v1, v2, the future `C:\dz-hidden-v3`), their results folders, or the zip files of the same name. Never read a transcript of an agent that wrote hidden notes. Only `summary.md` leaves a hidden run, through the owner's command.
- Nothing is tuned on v1, v2 or v3, ever.
- **No folder is locked** (owner's decision, 13 November 2026): the access rights of the hidden folders are never changed. The protection of both hidden sets is exactly three things: **this no-open rule**; **Codex's empty scratch folder with a read-only sandbox**; and **the Codex event-log check, which discards any call that opened a file or ran a command**. You and all your agents run under the owner's account, so nothing technical stops you: the rule is the protection.
- Never open `dz-eval`, `dz-fixed`, `%TEMP%\dzlegal-*`, or the untracked files of the main folder `C:\Users\TheShadow\dz-legal-assistant`.

**Money, keys, accounts.**
- $0. No paid API, service or rented machine. No new account without asking.
- The owner's shell has `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` set. **Unset both in every shell before running any tool**, and never print, log or commit a key.
- The owner's name and email address never go into a prompt, a search, a request header or a file sent anywhere.

**Tools and downloads that are allowed.** Anything not on this list: ask the owner first.
- Already on the laptop: .NET 10; Python 3.12 with pytest; the Python environment `C:\Users\TheShadow\dz-arch-research\venv` (torch 2.15.0+cpu, transformers 5.22.2, onnxruntime 1.31.1), left from Part 7; `pdftotext` and `pdftoppm`; Windows PowerShell 5.1 (PowerShell 7 is **not** installed: never write `pwsh`); Codex CLI; Ollama with the local models of Part 7 (Gemma 4 E4B, the Jais 3 copies, Qwen3.5-2B, Gemma 3 4B).
- **The encoders step 4 starts from, and no other:** the two original pre-trained models, each downloaded from its official page, in the revision named here (both Apache 2.0, read from the pages on 13 November 2026):
  - `CAMeL-Lab/bert-base-arabic-camelbert-mix` (https://huggingface.co/CAMeL-Lab/bert-base-arabic-camelbert-mix, revision `9be352797bdf28a9ae21e2ae582aaaca7abdb22d`). **Why:** it is the encoder Part 7 measured on this very task, and the best of six there (37 of 46 development cases alone, 40 with the section filter). It was trained on formal Arabic, dialects and classical Arabic together.
  - `CAMeL-Lab/bert-base-arabic-camelbert-msa` (https://huggingface.co/CAMeL-Lab/bert-base-arabic-camelbert-msa, revision `277069fd3645fedb22b746caf38d111aadee0241`). **Why:** the notes of this stage are formal Arabic only, and this variant was trained on formal Arabic only. Part 7 did not measure it.
  - Both have the same shape and size, so either fits the 150 MB limit in its 8-bit form. Train the same heads on both, compare them on dev and challenge, and **keep one**: the reader ships a single encoder. If they are within a point of each other, keep CAMeLBERT-mix, the one with measured evidence.
  - For each download record the size and SHA-256 of every file, the revision, the licence link and the date, and put the files in `C:\Users\TheShadow\dz-core-research\models\pretrained\`. No account is needed; if the page asks for one, stop and ask the owner.
- **Never start from a Part 7 fine-tuned copy.** The classifiers Part 7 trained (CAMeLBERT-mix among them) were fine-tuned on notes written by Claude models, which are usable for a measurement and **not for anything that ships**. They were removed from the laptop on 13 November 2026; their result files remain as evidence (`git show fable/architecture-review:docs/research/fable-part7/encoders/results/...`). If such a copy ever turns up again, do not use it as a starting point, as a teacher, or as a source of weights.
- Downloads: issues of the Official Journal from `joradp.dz` (Arabic and French), through the one fetching script; the two pre-trained encoders named above; the NuGet packages `Microsoft.ML.OnnxRuntime` and `Microsoft.ML.Tokenizers` (MIT) when step 4 starts; a small pure-Python package from PyPI when a script needs one, installed into an environment of your own under `C:\Users\TheShadow\dz-core-research\`, never into Part 7's, which is used as it is (record the package's name, version and licence in the step report).
- The Official Journal PDFs already in the repository (`docs/official-law-texts/`) are read where they are.

**Branches and folders.**
- Work only in the worktree `C:\Users\TheShadow\dz-fable-core`, on `fable/core-logic`. Push only to `fable/core-logic`. Never merge, rebase or force-push; never commit to another branch.
- **Create and edit files only under `docs/research/core-logic/` and `research/core-logic/`; never change a file outside them.** Editing your own files there (this handbook, the examples, `slots.json`, the solution file) is expected. Outside them, nothing: not the app, `src/`, `data/`, `tests/`, the prompts, the harness, Codex's corpus (`data/law/corpus/`, `tools/law-corpus/`, `src/DzLegal.Law/Corpus/`) or its branch `codex/law-corpus` (read it with `git show 9b60990:<path>`).
- Working files that are not products (the store, page images, queues, logs, the state file, the note sets while they are being built) live outside every repository, under `C:\Users\TheShadow\dz-core-research\`.
- **Model files are never committed**: an 8-bit encoder is over GitHub's 100 MB limit, and a commit that holds one blocks every later push. Weights and ONNX files live in `C:\Users\TheShadow\dz-core-research\models\`; the repository holds their hashes, their sizes and the commands that rebuild them.
- **Note sets are not committed until the owner says so** at the terms stop point (the repository is public, and the notes are a provider's output). Until then they live in `C:\Users\TheShadow\dz-core-research\notes-work\sets\<set>.jsonl`, and the repository holds their counts, statistics and SHA-256.
- Never name a folder `bench…` (the repository ignores such folders).

**After every commit.** `dotnet build -c Release` with 0 warnings and `dotnet test -c Release` green, for the root solution **and** for `research/core-logic/CoreLogic.sln`, on an idle CPU (the root test `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemory…` fails when the CPU is busy; it passes alone). Python scripts have their tests too (`python -m pytest research/core-logic/scripts/tests`). Never delete, skip or weaken a test; never suppress a warning. Small commits, clear messages, ending with the `Co-Authored-By:` line of the model that made them. Agents never build or commit while you do, and nothing is built, tested or committed while a training or a timing runs.

**Data.** Fictitious only: no real person, company or case. Notes are **formal Arabic only** (no Darija, no French, no mixed text).

**Measurements.** Time and memory are measured only on mains power, Windows power mode Best performance, CPU idle. Otherwise the figure is not reported.

**Codex.** Every call follows [specs/codex-calls.md](specs/codex-calls.md): empty scratch folder outside every repository, read-only sandbox, `--ephemeral`, an output schema, and **the event-log rule**: a call in which Codex ran a command or opened a file is thrown away. One call at a time; batches across the day; a usage-limit refusal stops the batch, never loops.

**The app's safety behaviour** (export blockers, AI-authorship marks, review before export, the recovery copy) is not yours to touch. No rule may have an effect stronger than `blockCleanExport`, and no rule that fires may be shown as less than a warning.

**A built-in safety check** may refuse an action (for example deleting a folder at the root of `C:`). Never work around such a refusal; tell the owner.

**Legal honesty.** Every record keeps `"verified": false`. Never write, in a record, a report or a message for the lawyer, that a rule was verified by a lawyer or is certain. Every legal statement has its official source and date; every claim about a model has its link and date. This handbook's own article numbers (section 6) are pointers, not statements of the law.

## 4. Who does what

| Role | Model | Why this one |
|---|---|---|
| **You: the executor.** All code, scripts and tests; the classifier and reader; orchestration; reports | Opus 5.5 | |
| Derivation A; reconciler; verifier of attacks; planted-error test; sorting of dropped notes | Opus 5.5 (fresh agents) | the roles that need the deepest reading of a text |
| Derivation B; boundary-case writer; note judges; mirror and hidden-v3 planners and writers; Official Journal triage; first page reading; the clean-notes filter; checking a new slot's Arabic question | Sonnet 5.5 (fresh agents) | a different model from A, so that A and B fail differently; and the volume roles |
| Third derivation of the 12 modules; checklists before; attack on finished records; support check; second page reading; note writer; judge on the cross-check sample; blind relabelling of v3 and mirror; second judge in step 5 | Codex | the other family |
| Adversary with web access | Opus 5.5 | hunting an exception needs depth; the owner's list had no adversary, so the strongest model takes it |
| Third blind judge of the mirror set | Opus 5.5 | must differ from the Sonnet writer and the Codex checker |
| Settling a difference between two page readings | Opus 5.5 | sees the image and both readings, blind to their source |
| Legal Review answers | Opus 5.5 and Sonnet 5.5, independently; Codex attacks the pair | two answers by two models, one attack by the other family |
| Guide test; realism review; ranking of the two clean writers | Sonnet 5.5 and Codex, independently | each family checks what the other cannot see in itself |
| Clean notes, and the phrases of the composed notes | the better of Jais-3-3.7B and Gemma 4 E4B (local) | the owner's decision; a licence beyond doubt |
| Applied-law sentences (step 5) | a template, Gemma 4 E4B, Jais-3-3.7B, Jais-3-0.9B, Qwen3.5-2B (local) | the candidates Part 7 measured |
| Rebuild from the freeze document (step 6) | Sonnet 5.5 | if the lighter model can follow it, it is clear enough |

Six rules about agents:

1. **Fresh, never forked.** An agent that must be independent is started with the Agent tool, the model named above, and the prompt template **unchanged** with its placeholders filled. A fork carries your context, and your context holds what the agent must not see.
2. **You orchestrate; you do not take a role that must be independent of you.** By the time B runs, you have read A. So you never "finish" a derivation, "fix" a judge's label, map Codex's free text onto a record by hand, or "complete" a v3 batch yourself. A failed run is run again by a fresh agent.
3. **Absolute paths.** An agent's shell may start in another folder, on another branch. Every Claude prompt for an agent that reads or writes files opens by naming the worktree (`{{worktree}}` = `C:\Users\TheShadow\dz-fable-core`) and says where the agent may read and write. An agent started without that line can write its work into the wrong branch.
4. **Allow-lists.** A prompt says which paths the agent may read. Everything else is off limits, and the prompts name the tempting places: the other derivation, the reports, the memory folder, the plan and state files, other git branches, `C:\dz-hidden*`. Every fresh agent is handed the user's memory index and `AGENTS.md` automatically; the prompts for independent and isolated roles forbid reading or writing memory, and for hidden-set writers a script checks afterwards that the memory folder did not change ([specs/hidden-v3-commands.md](specs/hidden-v3-commands.md)).
5. **Judges use no tool.** A judge, a guide tester, a realism reviewer, the sorter of dropped notes, the ranker of the clean writers or a cross-check writer gets its inputs pasted in the prompt and answers in its reply; its prompt names no path at all. A run that reports any tool use is discarded and run again, exactly as a Codex call that ran a command is discarded. This is the Claude side of the event-log rule.
6. **A known bias, to state wherever a v3 score appears:** Sonnet 5.5 both judges the training notes and writes hidden v3. The classifier learns what Sonnet accepts and is tested on what Sonnet writes, which can flatter the v3 score slightly. The scores on the app's own cases and on the clean notes are reported beside it for that reason.

## 5. What each safeguard is for

The plan was attacked by a reviewer agent before it was approved; the changes that attack forced are marked **(review)**. Each line names the error that would pass unseen without it.

| # | Safeguard | The weakness it fixes |
|---|---|---|
| 1 | **Texts are verified before rules are built on them** (review): step 2's first pass runs before step 1. | A rule derived from an unverified or outdated wording is wrong however carefully it is reasoned. |
| 2 | **Agents point, a script quotes** (review): a record names article, version, paragraph; the script inserts the words. Whole paragraphs only. | A model misquotes, or "quotes" from memory a text that says something else. Two derivers can share the same false quote. |
| 3 | **The exception-neighbour flag**: an uncited paragraph that opens an exception next to a cited one must be answered. | The rule is cited and its exception, one paragraph down, is silently dropped. |
| 4 | **Numbers are read from the quoted text by script** and must equal the field. | "Ten days" in the field beside "fifteen" in the text; limitation written as forfeiture. |
| 5 | **Agreement between the corpus and the library counts only where their sources differ** (review); CPCA and Family Code articles behind a deadline, a prior step, a section rule or an exit are re-read on the Official Journal page (the "critical use" rule of [specs/article-store.md](specs/article-store.md)). | The library's CPCA and Family Code were imported from the corpus: comparing a text with its own copy proves nothing. |
| 6 | **Missing articles are read twice by two families, a third settles, and every number is checked against the French edition** (review). | One reader's plausible misreading of a scan (measured once: one word in sixty-six). |
| 7 | **Amendments are searched by machine** in every Official Journal issue since 2003; the scan is not trusted if it misses a known amendment. | Relying on models' memory of what was amended. |
| 8 | **Two derivations by different routes** (A from the Arabic text; B from the case and the French edition), and **a third reading of the 12 modules by Codex** (review), compared on its mechanical fields. | Two agents reading the same article in the same order make the same mistake. A module's error is copied into many records. |
| 9 | **Checklists written from memory before any text is read**, merged, and answered line by line. | What nobody found in the texts ("is there a special law?") leaves no trace unless it was written down first as a question. |
| 10 | **One rule evaluator, and comparison case by case**: both derivations' examples and blind boundary cases run through both records, comparing the last day computed and not only the effect. | Two records with the same fields and different conditions look equal in a field comparison. Derivers write examples their own record passes. A difference in counting hides inside the warning band. |
| 11 | **Reconciliation from the text, or "uncertain"**; the comparison report is committed before the reconciler runs. | A reconciler picks the better-argued side, or smooths a difference to raise the agreement figure. |
| 12 | **Source tiers**: Official Journal page; official consolidated text; everything else is a lead. A court decision counts only from the court's own site. | Confident, outdated statements of Algerian law on the web taken as law. |
| 13 | **Confidence is computed, with an effect** (review): grades A to D from checkable facts; only grade A may block; a deadline warns inside a band; a proposed value never blocks; a rule on an unverified text still warns, marked "to check", and is never silent. | A model's "high confidence" means nothing; a block resting on a reading or on an uncertain count of days would harm the lawyer; a silent rule would too. |
| 14 | **Seeded false pairs in the support check, and planted errors in finished records.** | A checker that says "fine" to everything looks exactly like a checker with nothing to find. |
| 15 | **A polarity table for each of the 75 elements, frozen before any volume** (review), and **the guide test**. | The event list names some facts twice, positively and negatively (`payment_made` beside `price_unpaid`), and did not say what «لم يدفع الثمن» makes of each. Without it, judges disagree because the guide is silent, and their "agreement" measures nothing. |
| 16 | **Code decides the case, a model writes the wording**: the sampler, with style cards drawn independently of the labels. | A model asked for notes writes one tidy note a thousand times and chooses the facts itself, so the right labels are unknown and the style predicts the label. |
| 17 | **A pilot of 500 notes before the method is frozen.** | Committing weeks of Codex budget to a pipeline whose drop rate, sag and cost were never measured. |
| 18 | **Dropped notes are kept, counted per polarity, sorted by cause, and refilled.** | Dropping disagreements silently removes the hard cases (denied, disputed) the classifier most needs, and hides whether the writer, the judge or the guide is at fault. |
| 19 | **The mirror set**, built exactly like v3 but open, with a third blind judge; **it chooses nothing** and is scored once. | Nobody could otherwise say how many wrong labels survive in v3, or how much dropping hard notes flatters its score. A rehearsal that is used to choose models stops predicting anything. |
| 20 | **Splits by writing call and scenario family; a loader that refuses test notes; near-copy and leakage probes.** | The same note, slightly changed, in training and test; labels guessable from a stock phrase. |
| 21 | **The event-log rule**: a Codex call that opened a file or ran a command is discarded whole; and its Claude twin: a judge run with any tool use is discarded. (No folder is locked: the owner's decision of 13 November.) | A read-only sandbox stops Codex from writing, not from reading: measured on this laptop, it can read folders at the root of `C:`. A Claude judge keeps its file tools. The checks are what would catch either looking. |
| 22 | **The scorer first; three verdicts with intervals; the worst test set as the headline; a commitment before the hidden run; at most two candidates.** | A yardstick written after the model bends to it; 95.4 % reported as "target met"; a hidden set used as a tuning set. |
| 23 | **Cheap experiments first on a laptop without a GPU** (review): a frozen encoder with light heads before any night of training; every score on the 8-bit model. | Nights of training spent before knowing they are needed; a 32-bit score reported for an 8-bit product. |
| 24 | **The .NET tokenizer is the reference; training uses the numbers it produces** (review). | Python and .NET tokenise one word differently and every highlighted span is shifted. |
| 25 | **Step 5's main measure is mechanical** (review): no fact, number, date or article that is not on the sheet. | Model judges prefer fluent prose, including fluent inventions. |
| 26 | **The freeze is rebuilt by a lighter model in a clean clone.** | A freeze document only its author can follow. |
| 27 | **Derivers read a neutral guide, not the files that hold answers**; allow-lists; absolute paths. (Added by the check of 13 November.) | The grade table, the slot list's help texts and the evaluator's provisional counting stated things the derivers were supposed to find; a "do not open" list always has holes; an agent started in the main folder writes into another branch. |

## 6. The units, and a rule record in brief

**33 units:** the 21 claim types of `data/law/claims/*.json`, and 12 shared modules written once and copied into the claim records by script.

**The 21 claim types:** `loan-repayment`, `unpaid-sale-price`, `contract-rescission`, `sale-annulment-fraud`, `contract-damages`, `tort-damages`, `liability-for-things`, `unpaid-rent`, `eviction-occupier-without-title`, `commercial-debt`, `commercial-lease-eviction`, `sale-non-delivery`, `divorce-by-husband`, `tatliq`, `khul`, `nafaqa`, `custody`, `custody-forfeiture`, `visitation`, `abusive-dismissal`, `unpaid-wages`. Their areas, labels, descriptions and elements are in `data/law/claims/` and `data/law/claim-elements.json` (read; never change).

**The 12 modules.** Article numbers in the last column are **pointers to start from** (those the brief names, and a few from memory marked "?"): the derivers find the texts in the store, and nothing here is a statement of the law.

| Module id | What it must answer | Where to start |
|---|---|---|
| `court-sections` | Which section of the court hears which matter; what happens when a court has no sections or a case is listed before the wrong one. | CPCA 32, 423?, 500, 512, 515, 531?, 536 مكرر |
| `territorial-jurisdiction` | Which court by place, for each kind of matter; the special rules (real property, family, labour, contracts, torts); whether the rule is of public order. | CPCA 37 to 40, 426, 501, 45 to 47? |
| `ordinary-court-exits` | Which cases leave the ordinary courts: administrative courts; the specialised commercial courts; the criminal judge; and procedures that replace a claim (order to pay). What tells them apart. | CPCA 800, 801, 536 مكرر and following, 306? |
| `periods` | Limitation and forfeiture: the general periods, **how a period is counted**, what starts, suspends and interrupts it, who may raise it, whether the court raises it of its own motion; the counting of procedural periods. | Civil Code 308 to 322 (308, 309, 312, 321 named by the brief); CPCA 405, 67 to 69 |
| `formal-notice` | When a formal notice is required before a claim or before damages, its form and means, when it is not needed, and the effect of its absence. | Civil Code 119, 179, 180, 181 |
| `labour-prelitigation` | Conciliation before a labour claim, its exceptions, the record, the time limit to file and its sanction, the section and the court. | Law 90-04 arts. 19 and following (and its first articles), 36, 37; CPCA 500 to 510 |
| `land-registry-publication` | When a petition about real property must be published at the land registry, how, and the sanction. | CPCA 17, 519? |
| `conciliation-mediation` | Conciliation and mediation offered or required by the court; the reconciliation attempts in family matters; the conciliation required before the specialised commercial court. | CPCA 536 مكرر 4, 439 and following?, 990 and following?; Family Code 49? |
| `petition-formal-elements` | The mandatory elements of the petition and what their absence costs; standing, interest; fees. (Feeds the 22 Legal Review questions.) | CPCA 13, 14, 15, 17, 426 |
| `commercial-lease-regimes` | The two regimes of commercial leases (before and after the 2005 reform): which contracts fall under which, by what date; notice to quit, its form and period; renewal, refusal, eviction compensation. | Commercial Code 169 to 202, 173, 187 مكرر; Law 05-02 |
| `proof` | Who must prove what; when writing is required and above what sum; freedom of proof in commercial matters; the force of an official and of a private document; admission, oath. | Civil Code 323 and following; Commercial Code 30? |
| `accessory-requests` | Damages for delay and their conditions, interest, the penalty payment, costs, provisional execution: the text behind each accessory request. | Civil Code 182, 186, 174?, 175?; CPCA 419, 323, 625? |

Three things about the modules:

- **A lead on the court rules, from Codex's corpus report:** Law 22-13 (art. 13) is recorded there as delaying its new court rules (CPCA 32, 33, 531, 536 مكرر, 536 مكرر 4, 800, 801) until the new courts are installed. The modules `court-sections` and `ordinary-court-exits` must read that text on the Official Journal page and answer it in their checklist: a rule that sends a case to a court that does not exist yet would harm the lawyer.
- **Notes the store carries.** Whenever the store gives a cited article an `inForceNote`, or more than one wording (the commercial-lease articles around 187 مكرر are the known case), the record must answer it in its checklist: which wording governs which cases, from which date, by which transitional text.
- **Modules made of statements.** `court-sections`, `proof`, `petition-formal-elements` and `accessory-requests` consist mostly of statements and requests. A claim record imports those by id, and writes its own `court.section` citing the imported statements.
- If a derivation shows that a module should be split, merged or added, do it, record why in the state file and tell the owner in the step report. The number 12 is a plan, not a rule.

**A rule record in brief** (the schema describes every field; [rules/DERIVER-GUIDE.md](rules/DERIVER-GUIDE.md) explains it for derivers, with a toy example that holds no law):

- `basis`: statements. Each is a **pointer** (`cite`: code, article, paragraph numbers counted from 1, optionally a version) and a short `says`. A script adds `quote`, `source`, `grading`.
- `elements`: what must be proved, with the events of `claim-elements.json` that carry each, the burden, the usual proof.
- `requests`: the usual requests with their request kind and the statements that ground them.
- `court`: the section rule, the territory rules, the exits from the ordinary courts.
- `deadlines`: kind (forfeiture, limitation, procedural), period, the date slot it runs from, `counting` (the method, with the statements it comes from; absent when the texts do not settle it), who raises it, the declared effect, and `extract`: the exact words of the text for the period and the sanction.
- `priorSteps`: the step, when it is required, each exemption, the effect when missing and when unknown.
- `documents`, `boundaries` (what looks like this claim and is not).
- **Conditions** test `slots` ([slots.json](slots.json)); an unknown slot makes a condition unknown, and the rule asks. A deriver that needs a new slot declares it in `proposedSlots`.
- `examples`: worked cases with the **declared** effect expected for the items the author lists, and the set of questions expected.
- `openPoints`: what the texts do not settle, every reading, and the protective one chosen.
- `checklist`: every line of the merged checklist, answered.
- A claim record `imports` blocks of modules; it never restates a module's rule.

Folders under `rules/`: `derivations/a|b/` (stage records and their from-memory checklists, as the agents wrote them), `derivations/a|b/quoted/` (the scripts' copies with quotes), `derivations/c/` (Codex's answers on the modules, as returned), `derivations/codex/` (Codex's checklists), `boundary-cases/`, `reports/`, `reconciled/`, `final-candidates/` (the verifier's corrected records, still at stage `reconciled`), `final/`. Only `final/` is a product; the rest is the proof, and is committed too.

## 7. The steps

Steps 1 and 2 are one loop: the texts first, then the rules, then the delta of texts. Of step 3, only the terms report and the code (sampler, pipeline, probes, on the two example records) can start before step 1 ends: the label scheme, and so every note, needs **all** final records. Step 4's scorer and plain-code rung can be written during step 3. Steps 5 and 6 come last.

Expected length: three to four weeks, paced mostly by Codex's usage limits and by the laptop.

---

### Step 2, first pass. The texts before the rules (2 to 3 days, then in the background)

**Why first:** safeguard 1.

**Inputs.** The library (`data/law/codes/`), the corpus at `9b60990` (through `git show`), the Official Journal PDFs in `docs/official-law-texts/`, the brief's list of expected missing articles.

**Do, in this order.**
1. Record the remote heads (`git ls-remote origin`, into the working folder) and build and test both solutions once, as your green baseline.
2. Write the **text comparison** and the **article store** with their tests ([specs/article-store.md](specs/article-store.md)), and the one fetching script for the Official Journal.
3. Write the **rule evaluator**, the `rules` tool and `rules check` ([specs/rule-evaluator.md](specs/rule-evaluator.md), [specs/record-checks.md](specs/record-checks.md)); move the frame's record checks into the tool so there is one copy. Run both example records through it. Build the tool once and give agents its absolute command line (the prompts' `{{rules_tool}}`); agents never build.
4. Write `quote-records`, `check-values`, `flag-neighbours`, `compare-derivations`, `flatten-imports`, `grade-records`, each with tests on small fixtures. **Rebuild the two example records with your scripts** from their sources and compare with the committed ones (they were built by a throwaway script that did the same by hand; a difference is a defect in one of the two: find which). The example sources and that script are in `C:\Users\TheShadow\dz-core-research\examples-src\`.
5. Run the first **Codex probes** of a session ([specs/codex-calls.md](specs/codex-calls.md), "Checks before the first real call").
6. Start the **Official Journal scan** ([specs/official-journal-scan.md](specs/official-journal-scan.md)), before the seed fetches, so that the two never compete for the one download at a time. The downloads take a few hours, politely; the triage of its hits ([prompts/journal-hit-triage.md](prompts/journal-hit-triage.md)) runs for days beside the other work.
7. **Seed inventory:** every article the brief names (step 1's list, Family Code art. 53 included, and step 2's list), the pointers of section 6 with their ranges, and every article of `data/law/claims/*.json`. Fetch and read what is missing ([law/README.md](law/README.md)), so derivers find it in the store. Record every re-reading of a text that is already present in `law/readings.jsonl`.
8. **Prepare the French sources** that derivation B starts from, outside the repository: the French Official Journal issues of each instrument (and the two French PDFs already in `docs/official-law-texts/`), as text where the PDF has a usable layer and as page images otherwise.

**In parallel:** 2 to 4 are code and can overlap; the scan runs in the background of everything; page readings (Sonnet, then Codex, one Codex call at a time) run beside the coding. Every download from `joradp.dz` goes through the one fetching script, so that there is never more than one at a time.

**Done when.**
- Both solutions build with 0 warnings and pass; the store builds twice to the same hash; the three corpus traps have tests.
- Every example of both example records passes `rules examples` (declared effects for the listed findings; the question set), or the example's source was corrected and the correction is reported.
- The planted fixtures of each script are caught (a changed digit, a dropped لا, a shifted paragraph, a repealed article, an uncited «غير أنه» paragraph).
- The seed articles are in the store, verified, or listed as not yet verified with the reason.
- The French sources exist for the chapters the first modules need.
- The scan has re-found the known amendments in the years it has covered so far.

**Traps.** The corpus's version in force is the **last** version, never a filter on `inForceTo`; and whether an article is repealed always comes from the corpus. The library's CPCA and Family Code are the corpus. Old issues are scans: read the image, not the text layer. Do not improve the store's rules for one article by hand: a special case belongs in a recorded decision.

**Report to the owner:** the commit id; what the store holds (articles per code, verified or not); the articles fetched; the scan's progress; what is uncertain.

---

### Step 1. The rule records (5 to 7 days with step 2's second pass)

**Why this shape:** safeguards 2 to 4, 8 to 14 and 27.

**Inputs.** The store; the schema; the deriver's guide; the slot list; the prompts `rules-*.md`; `data/law/claims`, `data/law/claim-elements.json`. The author's legal units of 10 November (on `fable/architecture-review`, in the report's Part 5) and the catalogue's own articles and prerequisites may be used **only as checklist questions** (`from: novemberUnit`, `from: catalogue`), never as a derivation and never shown to a deriver.

**Order.** **Take one module through the whole pipeline first** (`formal-notice` is small and self-contained), and fix the prompts and scripts on what it shows, before launching anything else: the pipeline has never run. Then the other modules, because claim records import them: `periods`, `court-sections`, `territorial-jurisdiction`, `ordinary-court-exits`, `petition-formal-elements`, `proof`, `accessory-requests`, `labour-prelitigation`, `land-registry-publication`, `conciliation-mediation`, `commercial-lease-regimes`. Then the claim types, area by area. A claim type is not derived before the modules it will import are **final**.

**For each unit, the pipeline:**

| # | Stage | Who | Prompt or script | Output |
|---|---|---|---|---|
| 0 | Fix the texts of the unit (whole chapters) and commit the list | script | [specs/record-checks.md](specs/record-checks.md) | `reports/<unit>.texts.json` |
| 1 | Checklist before (claim types only) | Codex | `rules-checklist-codex.md` | `derivations/codex/<unit>.checklist.json` |
| 2 | Derivation A and derivation B, at the same time, blind to each other | Opus 5.5; Sonnet 5.5 | `rules-derive-a.md`, `rules-derive-b.md` | `derivations/a|b/<unit>.checklist.json`, `<unit>.json` |
| 2c | Third reading (modules only), kept as returned | Codex | `rules-derive-c-codex.md` | `derivations/c/<unit>.json` |
| 2d | Boundary cases, blind to all records; never edited afterwards | Sonnet 5.5 | `rules-boundary-cases.md` | `boundary-cases/<unit>.json` |
| 3 | Merge the derivers' request files into `law/requests.jsonl` and serve the requests; if a derivation was stopped by a missing text, **rerun it fresh** once the text is in the store | you, step 2's readers | [law/README.md](law/README.md) | the store |
| 4 | `rules check`; `quote-records` (into `quoted/` copies); `check-values`; `flag-neighbours`; merge the checklists; `compare-derivations` (A against B); `compare-c` (Codex's answer, on its mechanical fields); `rules examples` across records. **Commit these reports now.** | scripts | [specs/record-checks.md](specs/record-checks.md) | `reports/<unit>.*` |
| 5 | Reconcile | Opus 5.5 | `rules-reconcile.md` | `reconciled/<unit>.json`, `reports/<unit>.reconciliation.md` |
| 6 | Scripts again on the reconciled record | scripts | | |
| 7 | Adversary (web); attack (Codex); support check with seeded pairs (Codex, in batches of 40 pairs across units) | Opus 5.5; Codex | `rules-adversary.md`, `rules-attack-codex.md`, `rules-support-check-codex.md` | `reports/<unit>.attacks.*.json`, `.support.json` |
| 8 | Verify every attack against the official text | Opus 5.5 | `rules-verify.md` | `reports/<unit>.attacks.json`, `final-candidates/<unit>.json` |
| 9 | Scripts again; move the reconciled `proposedSlots` into `slots.json`; `flatten-imports` (claim records); `grade-records`; `check-checklist` | scripts, you | | `final/<unit>.json` |

**In parallel:** stages 2, 2c and 2d of one unit; and the same stage across several units (a few agents at a time; say how many before you launch them). Codex calls are one at a time: queue them. A unit's stage 5 waits for its stage 4 commit.

**After all units:**
- **The planted-error test** ([specs/record-checks.md](specs/record-checks.md)): 8 records and 50 planted errors (each of the ten kinds five times), on copies outside the repository under new ids, the whole chain; a checker that misses a kind of error too often is fixed and re-tested.
- **The Legal Review answers** (`legal-review-answer.md`, `legal-review-attack-codex.md`), once every record is final: the 22 questions of [brief/LEGAL-REVIEW-QUESTIONS.md](brief/LEGAL-REVIEW-QUESTIONS.md) by section, two independent answers and a Codex attack each; you write `docs/research/core-logic/legal-review-answers.md`.
- **`slots.json` from the final records:** every slot they read, with its question in Arabic and English; `askedFor` by script. A new slot's question is written by you and checked by one fresh Sonnet agent for plain, correct Arabic.
- **The boundary table** for the labelling guide: from every record's `boundaries`, one row "looks like / is / what tells them apart".

**Done when.**
1. 33 final records pass `rules check`; every worked example and boundary case gives the expected result; `check-checklist` finds no unanswered line or flag; no final record has a `proposedSlots` entry.
2. Every statement has a quote, a source and a grade written by script; every cited article is verified in the store, or its items are grade D (they warn, marked "to check") and are listed.
3. Every difference between A and B has one of the four outcomes, with the paragraph that settled it or the open point.
4. Every attack has a verdict with a quote. No attack is "open" without an open point in the record.
5. The seeded-pair rate and the planted-error rates are reported, and each checker met its bar (or was fixed and re-tested).
6. The grade report exists ([rules/GRADES.md](rules/GRADES.md), last section), including the plain sentence on how many items may block.
7. `legal-review-answers.md` answers all 22 questions, each with its texts, its grade and what remains uncertain.

**Traps.**
- **A deriver that cannot find a text will write the rule from memory** unless the rule is enforced: `quote-records` refuses a cite that is not in the store, and an item without a cite is grade D. Read the "searched, not found" lines; they are the work list of step 2.
- **Deriving a claim type before its modules are final** bakes a module's unsettled point into 21 records.
- **Sonnet's B is not a lesser A.** When they differ, B is right about as often on plain reading; do not let the reconciler treat A as the default. The prompt forbids it; check the reconciliation reports for "A's reasoning" used as a reason.
- **The adversary will bring many tier-3 leads.** That is its job. The verifier decides; an unconfirmed specific lead becomes an open point, not a dismissal.
- **Case law is not in the store.** A rule that needs a Supreme Court principle to hold is `interpretive`, grade C at best, and says so.
- **Your own legal knowledge is a lead too.** If you "know" a record is wrong, raise an attack with a source and send it through the verifier like any other.
- **An agent launched without the worktree line** writes into the folder its shell started in.

**Report to the owner** (plain language): commit id; units final; the agreement rate before reconciliation; attacks made, fixed, refuted, open; planted errors found; how many items of each grade, and how many may block; the open points that matter most to a lawyer, in a short list; what you are unsure of; what is next.

---

### Step 2, second pass. The delta (1 to 2 days)

**Inputs.** The final records; the store; `law/readings.jsonl`; the scan's ledger.

**Do.** `inventory-cited` on the final records. Verify what remains (page readings, French check, the consolidated-PDF comparison where it applies). Finish and triage the scan; set `textAsOf` and `ledgerClean`; rerun `quote-records` and `grade-records`. Trim `law/law-delta.json` to what the final records cite (fetched articles that no final record cites move to `law/unused-articles.json`); write `law/amendment-ledger.json`, the evidence lists, and `law/REPORT.md`. Delete the downloaded PDFs.

**Done when:** the points of [law/README.md](law/README.md), "Done when". A test validates `law-delta.json` against its schema.

**Traps.** If the scan missed a known amendment, **say the scan is not trusted**, set every `ledgerClean` to false, and let the grades fall to B: do not patch the scan's result by hand. Be modest about corrections to Codex's corpus: a difference between one reading and a `checked` text is your reader's error until two families and the image agree.

**Report to the owner:** commit id; the figures the brief asks for (cited articles checked by script, by a reading, corrections found); the corrections for the corpus, for the owner to pass to Codex; the corrections for the library, with the six repealed CPCA articles; what stays unverified.

---

### Step 3. The notes and hidden set v3 (5 to 8 days of work, spread by Codex's limits)

**Why this shape:** safeguards 15 to 21. Full specification: [specs/notes-pipeline.md](specs/notes-pipeline.md), [specs/scenario-sampler.md](specs/scenario-sampler.md), [specs/probes.md](specs/probes.md), [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md).

**Inputs.** The final records, the slot list, the boundary table, the labelling guide, the evaluator.

**Order.**

| # | Stage | Stop point |
|---|---|---|
| 1 | **The terms report** (`notes/TERMS.md`): the providers' current terms quoted, the uses answered separately; and the question whether note sets may be committed | **Owner.** Generate nothing before the answer. |
| 2 | Complete the labelling guide from the final records (the boundary table; the public-body list; any new slot's rule); run **the guide test** again; **freeze the scheme** (`notes/SCHEME.md`) | |
| 3 | The sampler, the style cards, the fictitious cast lists, `build-spans`, `notes compare`, the loader with its refusals, the probes: code with tests | |
| 4 | **The pilot** (500 notes): the whole pipeline, measured, including the judge at 25 and at 40 notes per run | **Owner:** the pilot report, the calendar, the budget of runs, and **the decision on the four ways to spend less**. |
| 5 | Development, challenge and polarity-family notes; the cross-check sample (300) | |
| 6 | Training notes to 4,000, in batches across days; sorting of dropped notes; refills | |
| 7 | The clean-notes trial (20 notes from each local model; the better writer is kept) and the **composed notes**, whose phrases that writer supplies | |
| 8 | **The learning curve** at 1,000, 2,000, 4,000, on dev and challenge (needs step 4's rungs 1 to 4), with and without composed notes | **Owner.** No more notes before the answer. |
| 9 | The 1,000 clean notes (200 held out as a test split) | |
| 10 | **Freeze the generation method** (commit id recorded) | |
| 11 | **The mirror set** (about 440 kept notes in v3's proportions: 16 per claim type, 80 near-miss, about 24 for the two rarest slots), with the v3 roles, prompts and commands, in an open folder; the third judge; the label-noise and drop-flattery figures. It is the rehearsal of every v3 command | **Owner**, before any writer runs: the opt-in to a workflow for the writers (replies of integers only), and the permission, in the owner's Claude Code settings, for agents to read and write under the mirror folder and `C:\dz-hidden-v3` (no folder's access rights change). |
| 12 | **Hidden set v3**: `v3 init`, planner, writers, validate, relabel, compare, refills, probes, finalize | |

**In parallel.** Codex is one call at a time, so writing is a queue; while it runs, Sonnet judges the batches already written, and you write step 4's scorer and plain-code rung. Never run two Codex batches at once.

**Done when:** the "Done when" lists of [specs/notes-pipeline.md](specs/notes-pipeline.md) and of [specs/hidden-v3-commands.md](specs/hidden-v3-commands.md), the latter up to "finalized".

**Traps** (the specifications list more).
- **The mirror set chooses nothing.** No model, threshold, prompt or number of notes is decided on it. It is built after the method is frozen and scored once, for the final candidates.
- **The judge never sees the scenario**, and uses no tool.
- **A hidden-set command is debugged on the mirror set**, never by looking.
- **Never change the access rights of a hidden folder.** No folder is locked; the event-log check is what guards Codex.
- **A reply from a v3 writer that holds text**: stop, discard the batch, tell the owner.
- **A Codex call rejected for tool use while it handled hidden notes**: stop, tell the owner.
- **Balanced sets are not realistic sets**; say so beside every score.

**Reports to the owner:** at each stop point above, and at the end: commit id; notes kept per set; drop and blank rates per slot and polarity; the four-way sorting; the cross-check gap; the probes; the mirror set's two figures; v3's counts (from the commands) and its SHA-256; what is uncertain.

---

### Step 4. The classifier and the reader (7 to 10 days on the laptop)

**Why this shape:** safeguards 22 to 24. Full specification: [specs/reader.md](specs/reader.md), [specs/scorer.md](specs/scorer.md).

**Inputs.** The notes; the contract `INoteReader`; the Part 7 encoder results; the Python environment and the two original pre-trained encoders named in section 3 (never a Part 7 fine-tuned copy).

> **The encoders' size, time and memory were measured on the owner's laptop on 13 November 2026**, on mains power with Best performance: the 8-bit CAMeLBERT-mix is 104.8 MB, takes about 9 ms on a short note and 0.28 s on a window of 512 tokens, in about 0.51 GiB of memory, and lost 2 of 46 development cases against its 32-bit form. The table is in [specs/reader.md](specs/reader.md) (rung 4). Time is not the constraint; size forces the 8-bit form, and the 8-bit form must be the one that is scored.

**Order.** The ladder of [specs/reader.md](specs/reader.md): the scorer first; plain code; the linear model; the frozen encoder with heads; (only if needed) training the encoder; polarity throughout; one bounded from-scratch experiment; calibration and abstention; clean against Codex-trained; the .NET reader with parity tests and laptop measurements. Then the mirror set's one scoring of the final candidates, the **pre-commitment**, and the owner's **one run** on hidden v3.

**In parallel.** Training runs are sequential on one laptop. Do not run a build, a test suite or a Codex batch during a timing.

**Done when.**
1. The scorer's tests pass and its hash is recorded before the first model is trained.
2. Every rung is reported on dev, challenge, the clean test split and the app's own cases, in both settings, with intervals. The mirror set is scored once, for the at most two final candidates.
3. The .NET reader implements `INoteReader`, passes the parity run, and is measured on the laptop (time, memory, bytes) against the brief's limits. `dotnet test` is green in a clean clone without any model file (unit tests use a tiny fixture model).
4. The probes of step 4 (masking, writer identity) are reported.
5. `PRECOMMIT.md` is pushed; the owner has run the command once; the `summary.md` files are committed.
6. The report states each target as **met, not met or inconclusive**, with **the worst test set of at least 200 notes as the headline**, and the mirror set's prediction and error bars beside the v3 result.

**Traps.** Choosing a model on the mirror set. Reporting a 32-bit score. Evidence that is right by accident. A reader that abstains its way to a target (the coverage floor is committed in advance). Treating "outside the catalogue" as low confidence instead of a class. Committing a model file. **After the v3 run nothing is tuned on what the summary shows.**

**What the sizes can prove.** With 100 notes, a 90 % target is "met" only from 96 right of 100; with 200 notes, a 95 % target only from 197 of 200 ([specs/scorer.md](specs/scorer.md) has the table). Several v3 targets will therefore most likely come out "inconclusive" unless the reader is nearly perfect. Say this to the owner before the run, not after.

**Stop points:** any training beyond two overnight runs; any free-GPU account; any download not listed in section 3; the v3 run; the optional v2 comparison.

**Report to the owner:** commit id; the ladder table; the three-verdict table on v3; size, time, memory; what did not work; what a lawyer would notice (the kinds of sentence the reader still misreads, described in general terms, never quoted from a hidden set); what is next.

---

### Step 5. Is a generative model still needed? (about 1 day)

**Why:** safeguard 25. With steps 1 to 4, the model only writes the sentence that applies the law to confirmed facts.

**Do.** About 120 fact sheets from the final records and the sampler. Five writers ([prompts/gen-apply-law.md](prompts/gen-apply-law.md)): a template in code, Gemma 4 E4B, Jais-3-3.7B, Jais-3-0.9B, Qwen3.5-2B, all local, with their Part 7 digests. First the script: no fact, number, date or article that is not on the sheet. Then two blind judges ([prompts/gen-judge.md](prompts/gen-judge.md)): Sonnet 5.5 and Codex.

**Done when:** the faithfulness rate per writer with its interval; the judges' usable rate and their disagreements; the conclusion by the rule fixed, in its order of precedence, in the judge prompt's header: no model needed, or which model without fine-tuning, or the evidence **put to the owner**. Time per sentence on the laptop for each model, under the measurement rule.

**Trap.** A fluent paragraph with one invented date is a failure, whatever the judges prefer. **Nothing is fine-tuned without the owner's word.**

**Report to the owner:** commit id; the table; the conclusion in one sentence; what it means for the app's default model (Part 7 named Jais-3-3.7B as the candidate to replace Gemma 3 4B, with two caveats: 50 against 39 of 90 on one pass is a gap chance produces about one time in ten, and Jais 3 needs a prepared copy to run in Ollama).

---

### Step 6. Freeze (half a day)

**Why:** safeguard 26.

**Do.** Write `docs/research/core-logic/FREEZE.md`: every final score with the reader's file hashes and each model's digest; the data versions (the scheme's commit, each set's size and SHA-256, the v3 SHA-256); where the model files and the note sets live and how each is rebuilt; the notes' generation method (sampler commit and seed, prompt versions, Codex CLI version, writer and judge of each set); the store's inputs; **the exact commands**, in order, from a clean clone to each figure. Then a fresh Sonnet 5.5 agent rebuilds from it in a clean clone ([prompts/freeze-rebuild.md](prompts/freeze-rebuild.md)); fix the document until every open figure is rebuilt.

**Done when:** the rebuild report shows every open figure equal (or within the stated spread over seeds), both solutions green in the clone, and the hidden figures equal to the committed summaries.

**Report to the owner:** commit id; the final table; what the reviewer needs to integrate ([INTEGRATION-CONTRACT.md](INTEGRATION-CONTRACT.md)), with anything that changed in the contract since this handbook and why.

## 8. Stop points: when you must wait for the owner

At each of these: commit and push what is done; update the state file; make sure nothing unapproved is running; send a short plain report with the exact question; **wait**. Quote the answer in the state file before you continue.

| Stop point | What you bring | What the owner decides |
|---|---|---|
| **The terms report** (step 3) | the quoted clauses, the uses answered separately, a line per provider | whether each family's notes may train what the app ships; whether note sets may be committed |
| **The pilot's calendar and budget** (step 3) | the measured drop and blank rates, Codex's measured calls per window, the judge at 25 and at 40 notes per run, days, runs and calls needed (section 9), and for each of the four ways to spend less what it saves and what it costs | how the Codex budget is spent; the batch sizes; **which of the four ways to spend less are taken** |
| **The learning curve** (step 3) | the curve at 1,000, 2,000, 4,000 on dev and challenge, with and without composed notes | whether more notes are generated |
| **Before the v3 writers** (step 3) | the mirror rehearsal's result; what a workflow would run and how many agents | the opt-in to a workflow, in the owner's own words; the permission, in the owner's Claude Code settings, for agents to read and write under the mirror folder and `C:\dz-hidden-v3` (no folder's access rights change) |
| **A shortfall in v3's quotas** (step 3) | the cells that are short after three refill rounds, with their counts | accept the shortfall, or more rounds |
| **Running hidden v3** (step 4) | the pre-commitment commit, the one command, and what the sizes can prove | the owner runs it, once |
| **The hidden v2 comparison** (after step 4, optional) | the question of [specs/v2-adapter.md](specs/v2-adapter.md) | accept the adapter and the one added line of the scratch harness, or drop the comparison |
| **Training beyond the plan** | the time of one epoch, what was tried, why more is needed | yes or no; a free GPU or not |
| **Any account, any tool or download not listed in section 3, any spending** | what and why | yes or no (spending: the rule is $0) |
| **Plugging in the laptop** for a timing | which timing, how long | when |
| **A suspected exposure of a hidden set** (a reply with text; a Codex call rejected for tool use while it handled hidden notes; a memory folder that changed after a writer ran; a command that printed something unexpected) | what happened, in one line, without the text | what to do |
| **Fine-tuning a generative model** (step 5) | the evidence | yes or no |

Between stop points, after each step: push, give the commit id and the short summary, **and continue** (the brief: "Then continue unless the owner says otherwise").

## 9. The budget of runs

The owner watches usage. State the count before every fan-out, and keep the ledger ([STATE-FORMAT.md](STATE-FORMAT.md)).

| | Claude agent runs | Codex calls | Main items |
|---|---:|---:|---|
| Steps 1 and 2 | about 230 | about 165 to 195 | 33 A, 33 B, 11 boundary, 33 reconcile, 33 adversary, 17 verify, 8 planted, 10 legal review, about 50 page readings, settlements and triage; Codex: 12 + 21 + 33 attacks + about 26 support batches + 8 + 5, and 60 to 90 page readings (one reading may cover several articles of a page; the first pass also re-reads the CPCA and Family Code seed articles in critical use and the 17 labour-law articles) |
| Step 3 | **about 320 to 405** | **about 440** | see below |
| Steps 4 to 6 | about 12 | about 15 | step 5's judges; the rebuild |

**Step 3 costs more than the plan of 12 November said (135 runs and 315 calls).** The plan had under-counted the blind judging of every note and the relabelling of v3 and the mirror set. The owner accepted "about 320 agent runs and 440 Codex calls" on 13 November "for now", and made the pilot the stop point where the ways to spend less are decided. Be exact with the owner about one thing: **320 already assumes 40 notes per judge run**, which is itself one of the four savings and is allowed only if the pilot shows no sag; at 25 notes per run the figure is about 405.

- **Codex:** writing 5,600 notes at 20 per call (pilot 500, train 4,000, dev 600, challenge about 500) = 280 calls, about 310 with refills; relabelling v3 (about 1,375 notes before drops) and the mirror set (about 525) at 20 per call = about 95; judging the cross-check sample = 15 to 30; realism, guide tests, ranking = about 15.
- **Claude:** judging about 6,000 notes at 40 per run = 150 (240 at 25 per run); the clean-notes filter = 25; cross-check writers = 15; v3 planner, writers and refills = about 66; the mirror's = about 22; the mirror's third judge = 14; sorting dropped notes = 8; realism reviews = about 15.

**The four ways to spend less, decided by the owner at the pilot stop point** (bring each with what it saves and what it costs): (1) judge 40 notes per run, only if the pilot shows no sag; (2) a development set of 400; (3) 100 realism reviews per set; (4) a mirror set of about 330 kept notes in v3's proportions (12 per claim type, 60 near-miss, 18 for the rarest slots; wider error bars, stated). **Do not** save by skipping the blind judge, the third judge of the mirror set, or the v3 relabelling.

Small additions the check of 13 November brought: one more Sonnet run for each rerun of the guide test (it writes the new items); a challenge set of about 600 notes (12 difficulties of at least 50), about 5 more Codex calls; the composed notes' phrases cost local-model calls and a few filter runs.

If Codex's limit bites: hidden v3 first, then the mirror set, then development and challenge notes, then the rule reviews, and training volume last.

## 10. Reports

Every report to the owner is short and in plain language (the owner is a developer, not a lawyer and not a machine-learning specialist): **what is done** (with the commit id), **what is uncertain**, **what is next**, and, at a stop point, **the one question**. Numbers come with their meaning ("the two derivations agreed on 82 of 100 fields before reconciliation; that is the share of the rules that two independent readings reached without help"). Never round a result up. Never present a model's agreement as verification. When a target is missed, say so in the first paragraph.

Step reports are also committed, as `docs/research/core-logic/reports/step-<n>.md` (`step-2a.md` and `step-2b.md` for the two passes of step 2).

## 11. What this handbook did not settle

Honest gaps, so that you do not take silence for a decision:

1. **The reader's own time and memory in .NET.** The encoders were timed in Python on 13 November ([specs/reader.md](specs/reader.md), rung 4); the .NET reader, with its heads and its windows, is yours to measure.
2. **The two example records are hand-made** and their legal content is unverified. They are graded with the real state of 13 November, when no text had been re-read for this work: most of their items are grade D, so their rules warn with the mark "to check". Their worked examples were written before any evaluator existed; one reader worked them by hand against the specification and they held, but step 2's first pass is their real test.
3. **How a period is counted** is for the `periods` module to derive. Until it is final, the evaluator's counting is an engineering placeholder behind one function, not a statement of the law.
4. **The slot list is a seed**: the brief's slots and the examples'. The final records will add slots; which ones the reader reads from the note is decided at the scheme freeze.
5. **Codex's output schemas** (`prompts/schemas/*.json`) are described in each prompt's header and not yet written as files. Write each from its header when you first use the prompt, with a test that the header's shape and the file agree.
6. **The module list** is a plan (section 6).
7. **What v3 can prove** is limited by its size (step 4, "What the sizes can prove"). The owner knows; the sizes are the owner's decision.
8. **The handbook was checked on 13 November** by six cold readers, two critics and a guide test, and corrected on what they found ([reports/handbook-check.md](reports/handbook-check.md)). It has never been used. The first module you take through step 1 is its real test.
