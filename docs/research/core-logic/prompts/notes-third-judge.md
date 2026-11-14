# The third blind judge of the mirror set: closed question sheets

- **Model:** Sonnet 5.5, fresh agents, **no tool**, 25 notes per run (about 18 runs for the mirror's 440 kept notes; say the number before launching). **Mirror set only**: hidden v3 is never judged a third time.
- **Why Sonnet, and why this route.** The mirror's notes are written by Sonnet and relabelled by Opus. A third free labelling by either would repeat one of them. This judge differs from the relabeller **by model and by route**: it never labels; it answers closed questions that a script built from the scheme. **It is still a Claude model**: an error that writer, relabeller and judge share is not counted. Say so beside the figure ([../specs/hidden-v3-commands.md](../specs/hidden-v3-commands.md), section 7b).
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against the schema; a run with any other tool use is discarded and run again by a fresh agent. (The mirror set is open, and this judge is not part of the v3 chain, so its transcript may be read by the script.)
- **Placeholders:** `{{notes}}`: for each note, a call-local number, its four fields, then its numbered questions with lettered options, built by `notes third-judge-sheets --set mirror` from the scheme and the note's agreed claim types: the claim (the agreed type among three near ones and "none of these"); every element of those claim types and two decoy elements; each slot's own question; each request of those claim types and one decoy; the plaintiff's role where the claim type offers a choice; for each date and amount the plain-code parsers found, what it is the date or the amount of. **Never a label.** The question wordings are those of [notes-filter-local.md](notes-filter-local.md), in Arabic, with two more: «ما صفة المدعي في هذه الدعوى؟» (the roles' Arabic labels; «لا تذكر المذكرة ذلك») and «إلى ماذا يعود هذا التاريخ: «<the date as written>»؟» / «إلى ماذا يعود هذا المبلغ: «<the amount as written>»؟» (the candidate roles' Arabic labels; «لا شيء مما سبق»).
- **Output schema:** `schemas/third-judge.schema.json`: `{ "notes": [{ "n": integer, "answers": ["a" | "b" | "c" | "d" | "e" | "f" | "g" | "h"] }] }`.
- **What the script does:** it compares each letter with the agreed label of that field. The label-noise figure is the share of kept notes with at least one difference, per kind of field, with its interval; a note where a decoy is answered as present is counted apart, as the judge's own error ([../specs/probes.md](../specs/probes.md), section 4). Nothing is dropped or changed because of it: the mirror chooses nothing.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

You are given working notes written by Algerian lawyers before drafting a statement of claim. The notes are in formal Arabic and fictitious. After each note come closed questions about it, each with lettered options.

Answer every question **from the text of the note alone**. Do not assume what is likely, and do not complete what the note leaves unsaid: when the note does not say a thing, choose the option that says so. A fact the note reports as contested by the other party is "the parties disagree", not "it happened". A fact that is only intended, asked for or expected has not happened.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in any repository, not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

==== THE NOTES AND THEIR QUESTIONS ====

{{notes}}

Answer each note on its own. Do not let one note's answers influence another's. Answer every question of every note.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "notes": [ { "n": 1, "answers": ["a", "d", "b"] } ] }`

- One entry of "notes" per note, in the order given; "n" is the note's number as given.
- "answers": one letter per question of that note, in the order of its questions: the first letter answers question 1, the second question 2, and so on. Exactly as many letters as the note has questions.
- A letter is one of "a", "b", "c", "d", "e", "f", "g", "h", and for each question only a letter that is among its options.
