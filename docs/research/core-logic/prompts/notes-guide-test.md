# The guide test

- **Models:** Sonnet 5.5 (fresh agent) and Codex (one call), each given the labelling guide and the same items. Run before the label scheme is frozen, and again after every change to the guide ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 1).
- **A Claude tester uses no tool.** The guide and the items are pasted in the prompt, it needs no file path, and its answer is its reply. The executor reads the run's reported tool-use count: a run with any tool use is discarded and run again by a fresh agent, exactly as a Codex call that ran a command is discarded. A rejected Codex call follows the one rule of the notes pipeline, section 3.
- **Placeholders:** `{{guide}}` (the whole of `notes/LABELLING-GUIDE.md`), `{{items}}` (from `notes/guide-test.json`: for each item only `id`, `claim`, `ask`, `text`; **never** `expected` or `tests`).
- **Output schema (Codex) / reply (Sonnet):** `schemas/guide-test.schema.json`: `{ "answers": [{ "id": integer, "answer": string, "rule": string, "unsure": boolean }] }`.
- **Each rerun has two parts:** the existing items, each of which must still give its expected answer; and about 20 new items on the sections of the guide that changed, written with their expected answers by a fresh Sonnet 5.5 agent that is given the guide but not `guide-test.json`, then added to the file. A rerun only on items the guide was fixed on would pass by construction.
- **Reading the result (script `guide-test compare`):** for each item, the two testers' answers and the expected one. Every item where the three are not equal is listed. For each, the executor decides: the guide is unclear (fix the guide, add or reword a case), the expected answer is wrong (fix the file, say so), or a tester misread a clear rule. "Misread" stands **only when the other tester gave the expected answer and named the same rule**; otherwise the guide is the problem. If the same tester misreads the same rule twice, the guide's wording of that rule is the problem after all. Items marked `unsure` are read too, even when the answer is right: they show where the guide leaves a choice. **The scheme is not frozen while an item is unexplained.**
- **Why:** the guide is the one document every writer and judge shares. If it lets two careful readers differ, agreement between them later measures the guide's gaps, not the notes' quality.
- First run (13 November 2026, on the handbook's first version): [../notes/GUIDE-TEST-RESULTS.md](../notes/GUIDE-TEST-RESULTS.md).

--- PROMPT ---

Below is a labelling guide for lawyers' notes in Arabic, and then a list of short extracts. Answer each item **from the guide alone**. Do not use any other knowledge of how such labels are usually given; where the guide does not decide, say so.

==== THE GUIDE ====

{{guide}}

==== THE ITEMS ====

Each item gives the claim type of the note it comes from, what is asked, and the extract (a line beginning «الوقائع:» is the note's facts field; «الوثائق:» is its documents field).

- When "ask" is `event:<kind>`: answer how the element of that kind, for that claim type, stands in the extract: `stated`, `denied`, `disputed` or `notMentioned`. Use the element's proposition exactly as the guide's table 5.2 gives it for that claim type.
- When "ask" is `slot:<id>`: answer the slot's value by the guide's section 6 (`yes`, `no`, `unknown`, or a date `yyyy-mm-dd`).
- When "ask" is `value`: answer the normalised form of the date or amount in the extract by the guide's section 8, or `none` if the guide gives it no normalised form (a relative date, a Hijri date, a foreign currency) or does not label it (a period).

{{items}}

For every item give: "id"; "answer"; "rule" (the rule, case number, note letter or table row of the guide you applied, in a few words); "unsure": true if the guide did not clearly decide and you had to choose.

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.
