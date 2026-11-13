# New items for a rerun of the guide test

- **Model:** Sonnet 5.5, fresh agent, one run per rerun of the guide test (after every change to the labelling guide; [notes-guide-test.md](notes-guide-test.md)).
- **No tool.** The guide and the list of changed sections are pasted in the prompt; the answer is the reply. A run with any tool use is discarded and run again by a fresh agent. In particular the agent never sees `notes/guide-test.json`: items written by someone who knows the existing ones would only repeat them.
- **Placeholders:** `{{guide}}` (the whole of `notes/LABELLING-GUIDE.md`, as changed), `{{sections}}` (the sections, rules, hard cases and table rows that changed since the last run, each named as the guide numbers it).
- **Output** (the agent's reply): `{ "items": [{ "claim": string, "ask": "event:<kind>" | "slot:<id>" | "value", "text": string, "expected": string, "tests": string }] }`. A script gives each item the next free id and appends it to `notes/guide-test.json`.
- **Why:** a rerun only on the old items would pass by construction, because the guide was fixed on exactly those. New items on the changed sections test whether the fix made the guide clear or only made it agree with the old answers. The new items then go to both testers like the old ones; an item on which the two testers and the expected answer do not agree is a defect of the guide **or of the item**, and the executor decides which.

--- PROMPT ---

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command.

Below is a labelling guide for working notes that Algerian lawyers write in formal Arabic before drafting a statement of claim. Some parts of it were changed recently. You write test items for those parts: short extracts whose label a careful reader must be able to give from the guide alone.

==== THE GUIDE ====

{{guide}}

==== THE PARTS THAT CHANGED ====

{{sections}}

==== WHAT TO WRITE ====

Write 20 items, at least one for each changed rule, hard case or table row, and the rest on the borders between a changed part and the rules next to it.

Each item has:

- "claim": the claim type of the note the extract comes from (an id of the guide's table 5.2).
- "ask": what is asked: `event:<kind>` (how the element of that kind, for that claim type, stands in the extract), `slot:<id>` (the slot's value by the guide's section 6), or `value` (the normalised form of the date or amount in the extract by section 8).
- "text": one or two sentences of a lawyer's note, in **formal Arabic only**, fictitious, as a lawyer would jot them down. When the documents field matters, write «الوقائع: … الوثائق: …».
- "expected": the one answer the guide gives: `stated`, `denied`, `disputed` or `notMentioned` for an event; `yes`, `no`, `unknown`, a date or a choice id for a slot; the normalised form, or `none`, for a value.
- "tests": the rule, case number, note letter or table row of the guide that decides it, in a few words.

Rules for your items:

- **Never reuse a sentence of the guide**, and do not make an item a light rewording of one of its examples. Write a new situation that the same rule decides.
- Each item must have exactly one answer under the guide. If, while writing it, you find that the guide allows two, do not include the item: report the sentence and the two readings in "problems" instead (a list of strings beside "items"). Those reports are as useful as the items.
- Make the items hard in the way real notes are hard: the fact sits inside a longer sentence; the negation is of something else; a party contests one thing and admits another; a date belongs to a different event.
- No real person, company, place of business or case.

Return only the JSON object: `{ "items": [...], "problems": [...] }`.
