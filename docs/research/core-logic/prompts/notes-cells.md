# The cells table: which "denied" cells of the scheme are drawn

- **Models:** Sonnet 5.5 and Opus 5.5, independently, fresh agents, **no tool**, one run each, all 75 items in one run. Run once, before the freeze of scheme 1.0 ([../notes/SCHEME.md](../notes/SCHEME.md), section 8).
- **One family:** both are Claude models. The table decides only which scenarios are drawn, not a label; say so beside it.
- **Placeholders:** `{{items}}`: 75 numbered items (ids 1001 to 1075), one per element of the scheme, built by `notes cells --prepare` from `claim-elements.json`: the claim type's Arabic label and the element's Arabic proposition. They live in `notes/cells-test.json`, apart from `guide-test.json`, and have no expected answer.
- **Output schema:** `schemas/cells.schema.json`: `{ "answers": [{ "id": integer, "answer": "yes" | "no", "why": string }] }`.
- **How it is used:** `notes cells --write-table <Sonnet's reply> <Opus's reply>` writes the table of SCHEME.md, section 8. A `denied` cell is **not drawn only when both answer "no"**; on any other outcome it is drawn. These items are outside the rule "no unexplained item before the freeze": a split answer simply leaves the cell drawn.
- **No tool** except one read of its own prompt file; the answer is the reply, copied by `records.py ingest-reply`.

--- PROMPT ---

A tool helps Algerian lawyers draft a statement of claim (عريضة افتتاح دعوى) from their working notes. To train it, fictitious notes are written from cases drawn by a program. For each kind of claim the program knows a few facts; in a drawn case each fact is stated, denied, disputed or not mentioned.

Some combinations make no sense: a note for a claim to recover a loan in which the lawyer himself writes that no money was ever lent is not a note any lawyer would write for that claim. You decide, fact by fact, whether such a note could still exist.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

**For each item:** the note is about the kind of claim named. Its own author (the plaintiff's lawyer) writes that the fact given **did not happen**. Would a lawyer still write this note in order to bring **this** claim?

- Answer "yes" when the claim can still be brought, or is even brought because of it (for example: the claim exists precisely because a payment was not made).
- Answer "no" when, with that fact denied by the plaintiff's own lawyer, there is nothing left of this claim.
- This is a question about what notes exist, not about the law's fine points. When in doubt, answer "yes".

{{items}}

## Your answer

Return only one JSON object, with no text before or after it:

`{ "answers": [ { "id": 1001, "answer": "yes", "why": "<a few words>" } ] }`

One entry per item, "id" being the item's own number. "answer" is "yes" or "no". "why": a few words.
