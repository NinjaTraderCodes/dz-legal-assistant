# Blind judge of notes

- **Models:** Sonnet 5.5, fresh agent, 25 notes per run, or 40 if the owner takes that saving after the pilot (the pilot judges half its notes in runs of 25 and half in runs of 40: [../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 2). Used on training, dev, challenge and pilot notes, and as the filter of clean notes and composed phrases. Codex on the cross-check sample (10 to 20 notes per call, with an output schema of the same shape). **Opus 5.5** as the third judge of the mirror set. In every case the judge gets the notes' text and the guide, and **nothing else**: no scenario, no writer's labels, no style card, no other judge's answer.
- **A Claude judge uses no tool.** Everything it needs is pasted in the prompt, it needs no file path, and its answer is its reply. The executor reads the run's reported tool-use count: **a run with any tool use is discarded and run again by a fresh agent**, exactly as a Codex call that ran a command is discarded. A run that does not return is run again whole; a reply that leaves notes out sends those notes back to the queue. A rejected Codex call follows the one rule of the notes pipeline, section 3.
- **Placeholders:** `{{guide}}` (the whole labelling guide, frozen version), `{{elements}}` (every claim type with its elements' kinds and Arabic propositions; all 21, since the judge must choose the type), `{{requestKinds}}` (every kind of `FactVocabulary.Requests`, with an English gloss and the Arabic label where `claim-elements.json` gives one: the writer gets the same list), `{{slots}}` (the `readFromNote` slots: id, type, choices), `{{notes}}` (each note: a call-local number and its four fields; ids are meaningless).
- **Output** (reply of a Claude agent, or Codex schema `schemas/notes-judge.schema.json`): `{ "notes": [{ "n", "claims": [string], "outsideKind": string, "claimEvidence": [quote], "events": [{ "claim", "kind", "polarity": "stated" | "denied" | "disputed" | "notMentioned" | "unclear", "actor", "disputedBy", "evidence": [quote], "date": value, "amount": value }], "slots": [{ "slot", "value", "evidence": [quote] }], "requests": [{ "kind", "evidence": [quote], "amount": value }], "values": [value], "remarks": string }] }`, with `quote` and `value` as in `notes/note.schema.json` (`roles` a list; `amountDisputed` where a party contests the figure; without `start` and `length`: a script adds them). **`unclear` has one form per field:** `"claims": ["unclear"]`; an event's `"polarity": "unclear"`; a slot's `"value": "unclear"`; a value's `"normalized": "unclear"`.
- **What the answer becomes:** the `labels` of `note.schema.json`, field for field, minus `n` and `remarks` (the remarks are kept beside the note for the guide's list of defects). `unclear` never reaches gold: it is a disagreement.
- **Why blind:** a judge shown the intended labels confirms them. Agreement is evidence only if the judge could have disagreed.
- **Why "unclear" is allowed:** forcing a choice turns the guide's gaps into random agreement. `unclear` sends the note to the disagreement rule and the sentence to the guide's list of defects.

--- PROMPT ---

You label working notes written by Algerian lawyers before drafting a statement of claim. The notes are in formal Arabic and fictitious. You label **what each note says**, by the guide below, and you quote the exact words behind every label. You have no other information about the notes, and you must not guess what their author meant to write.

==== THE LABELLING GUIDE (apply it exactly) ====

{{guide}}

==== THE 21 CLAIM TYPES AND THEIR ELEMENTS ====

{{elements}}

==== THE KINDS OF REQUEST ====

{{requestKinds}}

==== THE SLOTS TO LABEL ====

{{slots}}

==== THE NOTES ====

{{notes}}

==== FOR EACH NOTE ====

1. **Claim type(s)** (guide, section 3): one to three claim ids, or `outside` with its kind. Quote the words that show it.
2. **Events** (sections 4 and 5): for **every** element of every claim type you chose, give its polarity. For `stated`, `denied` and `disputed`, quote the words that carry it; for `disputed`, say who contests. Give the date and the amount when the element takes one and the note gives it for that event. `notMentioned` has no quote. Go through the hard cases of section 4.2 whenever a sentence has a negation, a restriction (لم … إلا / سوى), a party's contest, an intention, a demand, or a document named in "documents".
3. **Slots** (section 6): every slot listed above, by its own rule. Remember that a slot can differ from the event of the same name, and that the date of a record is not the date of its delivery.
4. **Requests** (section 7) and **every date and amount** (section 8), normalised, each with its roles; one entry for each place a value is written.
5. If, for a field, two rules of the guide seem to fit, or the sentence can honestly be read two ways, answer `unclear` for that field and say in "remarks" which sentence and which two readings. For a claim type, write `"claims": ["unclear"]`; for an event, `"polarity": "unclear"`; for a slot, `"value": "unclear"`; for a date or an amount, `"normalized": "unclear"`. Do not use `unclear` to avoid a decision the guide makes.

Every quote must be copied **letter for letter** from the field you name: it is checked by a program, and a quote that is not found makes your label invalid.

Label each note on its own. Do not let one note's answer influence another's. Answer for every note you were given.

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.
