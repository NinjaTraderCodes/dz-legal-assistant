# Reconciler

- **Model:** Opus 5.5, fresh agent, one run per unit (33 runs). A claim type is reconciled only after the **final** records of the modules it imports exist.
- **Give it:** this prompt, filled. It is the first agent that sees both derivations, and it reads only its allow-list.
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`), `{{unit_id}}`, `{{unit_kind}}`, `{{store_path}}`, `{{store_text}}` (its text views, `scripts/store_text.py`), `{{rules_tool}}`, and `{{module_finals}}` (for a claim: the paths of `rules/final/<module>.json` of the modules it imports, or may import; for a module: "none"). The report paths are fixed by [../specs/record-checks.md](../specs/record-checks.md), "Where the reports go".
- **Before it runs** (the executor, by script, committed): both derivations quoted (`quote-records`, into the `quoted/` copies), `check-values`, `flag-neighbours`, `merge-checklists`, `compare-derivations` (with the id map), `compare-c` for a module, `rules examples` with both derivations' examples and the boundary cases. **The comparison report is committed before the reconciler starts**, so the agreement rate cannot be touched by it.
- **Output:** `rules/reconciled/{{unit_id}}.json` (stage `reconciled`) and `rules/reports/{{unit_id}}.reconciliation.md`.
- **Why "from the text, or uncertain":** a reconciler tends to pick the better-argued side or to average. Neither is evidence. Only the words of the text settle a difference; when they do not, the honest result is an open point and the protective rule.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`, and `<R>` means `<CL>\rules\reports\{{unit_id}}`.

Two agents derived the legal rules for the same unit independently, by different routes. You reconcile them. No lawyer will check the result; after you, an adversary will attack it and a script will grade each item by whether the two derivations agreed **before** you saw them. You cannot raise a grade by smoothing a difference over, and an item that neither derivation had is graded lower for that reason alone: you can only make the record right.

**The unit:** {{unit_kind}} `{{unit_id}}`

## What you may open

- **Read only:**
  1. `<CL>\rules\rule-record.schema.json`, `<CL>\rules\DERIVER-GUIDE.md`, `<CL>\rules\GRADES.md`, `<CL>\specs\rule-evaluator.md`, `<CL>\slots.json`.
  2. The difference list `<R>.differences.json` and the id map `<R>.idmap.json`.
  3. The two derivations as quoted by the script: `<CL>\rules\derivations\a\quoted\{{unit_id}}.json` and `<CL>\rules\derivations\b\quoted\{{unit_id}}.json`. For a module, also Codex's derivation as it returned it, `<CL>\rules\derivations\c\{{unit_id}}.json`, and its comparison `<R>.c-comparison.json`.
  4. The merged checklist `<R>.checklist.json`, the neighbour flags `<R>.flags.a.json` and `<R>.flags.b.json`, the value reports `<R>.values.a.json` and `<R>.values.b.json`.
  5. The boundary cases `<CL>\rules\boundary-cases\{{unit_id}}.json` (or the file of the run that held this unit).
  6. The article store `{{store_path}}` (and its text views, one file per code, in `{{store_text}}`: the same entries with each paragraph labelled as a cite counts it; read these, since the JSON file is too large to read whole): the only source of law.
  7. For a claim: the final module records {{module_finals}}.
- **Write only:** `<CL>\rules\reconciled\{{unit_id}}.json`, `<CL>\rules\reports\{{unit_id}}.reconciliation.md`, and new lines appended to `<CL>\law\requests.jsonl`.
- **Run only:** `{{rules_tool}}` as shown below. Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment. Do not run git.
- **Everything else is off limits**, in particular: the agents' own files outside the `quoted\` copies (the quoted copies hold the same content); the reports of other units; `<CL>\rules\final\` except the module records named above; `<CL>\rules\final-candidates\`; `<CL>\rules\examples\`; other git branches and any other repository; `C:\Users\TheShadow\dz-core-research\` except the store file and its text views; the state file, the run ledger and the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Do not search the web. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## The differences

Each entry of the difference list is one difference between derivation A and derivation B: a statement cited by one only; a deadline's period, kind, starting event, counting, who raises it; a prior step's conditions; a section; an exit; a proposed slot; a case that the two records decide differently (by effect, by questions, or by last day).

**For each difference, do exactly one of these, and record which:**

- **Settled by the text.** Read the cited paragraphs, the paragraphs around them, and the articles around the article. Name the paragraph whose words settle the difference and say, in one or two sentences, how. The side the text supports goes into the record. "A's reasoning is more convincing" is not a settlement; a paragraph is. Derivation A is not the default: B is right about as often.
- **Both are right** (they say the same thing in two ways, or each found a rule the other missed and both rules exist). Merge; say so.
- **Not settled by the text.** Keep both readings: set the item `interpretive`, put every reading in `readings`, make the rule the reading that protects the lawyer (warn rather than stay silent; never `blockCleanExport` on an interpretive item), and add an `openPoints` entry with the question, why the text does not settle it, and the protective reading.
- **Needs a text that is not in the store.** Do not decide. Add the `searchedNotFound` checklist line, append the request to `<CL>\law\requests.jsonl` (`"askedBy": "reconciler"`), and make the item an open point of grade D.

**Proposed slots.** The two derivers named new facts independently, with different ids. Map them: decide which proposed slots of A and B are the same fact, keep **one** id for each in the record's `proposedSlots` (with its question in Arabic and English), and write the mapping in your report. A fact that only one side needed is a difference like any other.

**Codex's derivation (modules).** Read what "C says" beside the items. Use it as evidence; it is never a reason by itself. Where C differs mechanically from a value A and B agreed on, the script has already sent that difference to the verifier as an attack: you need not settle it, but say in your report if the text settles it.

## Then, beyond the differences

- **Where A and B agree, do not assume they are right.** For every deadline (its counting included), prior step, section rule and exit, re-read the cited paragraph yourself and its neighbours. Two derivations can share a misreading.
- **Answer every line of the merged checklist** in the record's `checklist`: `applies` with the ids of the items that answer it; `doesNotApply` with the reason; `searchedNotFound` with what was searched. Keep each line's `from`. Lines `from: catalogue` are the app's catalogue (its articles, prerequisites, documents, requests) turned into questions: answer them from the texts like the others. `catalogue` is used for nothing else.
- **Entry-into-force notes.** For every article the record cites whose store entry has an `inForceNote`, the checklist must hold a line asking what the note raises (from which date the wording applies, and what that changes for this unit). If neither derivation wrote one, add it, with `from` naming the derivation(s) whose statements cite the article (`derivationA`, `derivationB`), and answer it from the texts.
- **Answer every neighbour flag:** the flagged paragraph is cited by a statement, or a `doesNotApply` checklist line names it and says why.
- **Keep the best worked examples of both derivations** (marked with their author) and add your own for every difference you settled: the case that distinguishes the two sides, with the outcome the text gives. If a derivation's example expects something the text does not support, correct it in your record and say so in the report.
- **The boundary cases are never edited.** When a boundary case's expectation (its outcome or its last day) is wrong under the text, write in your report which case, which paragraph shows it, and what the text gives. The verifier sees it there.
- **For a claim record:** replace any rule that restates a shared module by an import of the module's block (ids from its final record). If the claim's own version differed from the module's, list that as a difference between this unit and the module, in your report: it is one more check of the module. Statements and requests of the modules on sections, proof, the petition's formal elements and accessory requests are imported by id; the claim writes its own `court.section` citing them.
- You point; you do not quote. Never write `quote`, `source` or `grading`.

**Write** `<CL>\rules\reconciled\{{unit_id}}.json` (stage `reconciled`, `"verified": false`), and `<CL>\rules\reports\{{unit_id}}.reconciliation.md`: one line per difference (its id in the difference list, the outcome among the four above, the paragraph that settled it or the reason it is open), the mapping of proposed slots, the boundary cases you dispute, then what you changed where A and B had agreed, then the questions you leave to the adversary.

Run `{{rules_tool}} check --store {{store_path}} <CL>\rules\reconciled\{{unit_id}}.json`, `{{rules_tool}} examples <CL>\rules\reconciled\{{unit_id}}.json` and `{{rules_tool}} examples <CL>\rules\reconciled\{{unit_id}}.json --boundary <the boundary-case file>`, and fix the record where the text supports the case.

**Your reply:** the counts (differences settled by the text / both right / not settled / needing a text), the number of boundary cases you dispute, and the three points you are least sure of.
