# Realism review by the other family

- **Models:** the family that did **not** write the notes: Sonnet 5.5 (fresh agent, 40 notes per run) for Codex-written and local-model notes; Codex (20 notes per call) for Claude-written notes. 200 notes per set, or 100 if the owner takes that saving ([../specs/probes.md](../specs/probes.md), section 3).
- **A Claude reviewer uses no tool.** The notes are pasted in the prompt, it needs no file path, and its answer is its reply. The executor reads the run's reported tool-use count: a run with any tool use is discarded and run again by a fresh agent, exactly as a Codex call that ran a command is discarded. A rejected Codex call follows the one rule of the notes pipeline, section 3.
- **Placeholders:** `{{notes}}` (text only, numbered; for the odd-one-out variant, 90 generated notes with 10 of the app's formal-Arabic drafting cases mixed in at random positions, and the extra question at the end).
- **Output** (Codex schema `schemas/realism.schema.json`, or the agent's reply): `{ "notes": [{ "n", "workingNote": 1-5, "formalArabic": 1-5, "coherent": 1-5, "labelWords": boolean, "defects": [string] }], "patterns": [string], "otherHand": [integer] }`.
- **Why the other family:** a model finds its own manner natural. The other family notices it.
- **What it is not:** a judgment of legal correctness. Reviewers are told not to assess the law.

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

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.
