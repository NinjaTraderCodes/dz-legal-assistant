# The guide test

- **Models:** Sonnet 5.5 and Opus 5.5, independently, fresh agents, each given the labelling guide and the same items. Run before the label scheme is frozen, and again after every change to the guide ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 1).
- **A tester uses no tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against `schemas/guide-test.schema.json`; a run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:** `{{guide}}` (the whole of `notes/LABELLING-GUIDE.md`), `{{items}}` (from `notes/guide-test.json`: for each item only `id`, `claim`, `ask`, `text`; **never** `expected` or `tests`).
- **Output** (the agent's reply), `schemas/guide-test.schema.json`: `{ "answers": [{ "id": integer, "answer": string, "rule": string, "unsure": boolean }] }`.
- **Each rerun has two parts:** the existing items, each of which must still give its expected answer; and about 20 new items on the sections of the guide that changed, written with their expected answers by a fresh Sonnet 5.5 agent that is given the guide but not `guide-test.json`, then added to the file. A rerun only on items the guide was fixed on would pass by construction.
- **Reading the result (script `guide-test compare`):** for each item, the two testers' answers and the expected one. Every item where the three are not equal is listed. For each, the executor decides: the guide is unclear (fix the guide, add or reword a case), the expected answer is wrong (fix the file, say so), or a tester misread a clear rule. "Misread" stands **only when the other tester gave the expected answer and named the same rule**; otherwise the guide is the problem. If the same tester misreads the same rule twice, the guide's wording of that rule is the problem after all. Items marked `unsure` are read too, even when the answer is right: they show where the guide leaves a choice. **The scheme is not frozen while an item is unexplained.**
- **Why:** the guide is the one document every writer and judge shares. If it lets two careful readers differ, agreement between them later measures the guide's gaps, not the notes' quality.
- First run (13 November 2026, on the handbook's first version): [../notes/GUIDE-TEST-RESULTS.md](../notes/GUIDE-TEST-RESULTS.md).
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

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

Use no tool. Everything you need is in this prompt, and your answer is your reply. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "answers": [ { "id": 12, "answer": "<your answer to the item's question>", "rule": "<the section, rule or case number of the guide you applied>", "unsure": false } ] }`

One entry per item, "id" being the item's own number. "unsure": true when the guide left you a choice, even if you think your answer is right.
