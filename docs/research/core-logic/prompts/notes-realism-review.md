# Realism review by the other family

- **Model:** one reviewer per note, a model that did not write it: **Sonnet 5.5 for notes written by Codex or by a local model** (another family), **Opus 5.5 for notes written by Sonnet** (the same family: say so beside the figure). Fresh agents, 40 notes per run. 200 notes per set, or 100 if the owner takes that saving ([../specs/probes.md](../specs/probes.md), section 3).
- **A reviewer uses no tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against `schemas/realism.schema.json`; a run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:** `{{notes}}` (text only, numbered; for the odd-one-out variant, 90 generated notes with 10 of the app's formal-Arabic drafting cases mixed in at random positions, and the extra question at the end).
- **Output** (the agent's reply, checked against `schemas/realism.schema.json`): `{ "notes": [{ "n", "workingNote": 1-5, "formalArabic": 1-5, "coherent": 1-5, "labelWords": boolean, "defects": [string] }], "patterns": [string], "otherHand": [integer] }`.
- **Why another reader:** a model finds its own manner natural. Another model, and better another family, notices it.
- **What it is not:** a judgment of legal correctness. Reviewers are told not to assess the law.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

Below are short working notes in Arabic, of the kind an Algerian lawyer writes for himself before drafting a statement of claim: facts as the client told them, what to ask the court, which documents exist. Some may be good imitations and some poor ones. Review each as writing; do not assess whether the law is right.

{{notes}}

For each note, give:

- "workingNote" (1 to 5): does it read like a practitioner's own notes (uneven, concrete, selective), and not like a judgment, a textbook, a form being filled in, or a story told for a reader? 5 = I would believe a lawyer wrote it for himself.
- "formalArabic" (1 to 5): is it correct formal Arabic of the kind Algerian legal professionals write (their usual terms, month names such as جانفي and أوت, amounts in دج)? Lower it for dialect, for foreign words, for unnatural or translated-sounding phrasing, for the vocabulary of another country's legal system.
- "coherent" (1 to 5): do the dates, sums and events hang together (order of dates, sums that add up, parties that keep their roles)?
- "labelWords": true if the note uses classification words that no lawyer would write in his notes (for example announcing that a fact is "disputed" or "not mentioned" as a category, or naming a claim type like a heading from a catalogue).
- "defects": the specific things that give it away or weaken it, each in a few words ("every sentence starts with حيث", "the amount is repeated three times", "ends with a summary").

Then, in "patterns": habits you see **across** the notes (the same opening, the same order, the same verb for the same act, the same length, a stock closing sentence). Look in particular at the facts a note says did not happen or that a party contests: is the same wording, or the same place in the note, used for them each time? These habits matter most.

Finally, in "otherHand": the numbers of the notes, if any, that you believe were written by a different hand from the rest. Leave it empty if you see no such difference.

Use no tool. Everything you need is in this prompt, and your answer is your reply. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "notes": [ { "n": 1, "workingNote": 4, "formalArabic": 5, "coherent": 4, "labelWords": false, "defects": ["…"] } ], "patterns": ["…"], "otherHand": [3, 17] }`

- One entry of "notes" per note, "n" being its number as given. "workingNote", "formalArabic" and "coherent" are whole numbers from 1 (poor) to 5 (excellent). "labelWords": true when the note uses the words of a classification instead of telling what happened. "defects": each defect in a few words; an empty list when none.
- "patterns": turns of phrase, openings or structures that come back across the notes; an empty list when none.
- "otherHand": the numbers of the notes you think were written by another hand than the rest; an empty list when you were not asked the question or see none.
