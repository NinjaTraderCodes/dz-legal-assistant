# Blind judge of notes

- **Model:** the blind relabeller of test notes. Sonnet 5.5 relabels notes written by Codex; Opus 5.5 relabels notes written by Sonnet. Fresh agent, no tool, 25 notes per run. Never used on training notes (their filter is [notes-filter-local.md](notes-filter-local.md)). The mirror set and hidden v3 use [v3-blind-relabel.md](v3-blind-relabel.md), which carries the same answer shape. In every case the relabeller gets the notes' text and the guide, and **nothing else**: no scenario, no writer's labels, no style card, no other relabeller's answer.
- **A relabeller uses no tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against `schemas/notes-judge.schema.json`. **A run with any other tool use is discarded and run again by a fresh agent.** A run that does not return is run again whole; a reply that leaves notes out sends those notes back to the queue.
- **Placeholders:** `{{guide}}` (the whole labelling guide, frozen version), `{{elements}}` (every claim type with its roles (relation and position, with the Arabic label), its elements' kinds and Arabic propositions; all 21, since the judge must choose the type), `{{requestKinds}}` (every kind of `FactVocabulary.Requests`, with an English gloss and the Arabic label where `claim-elements.json` gives one: the writer gets the same list), `{{slots}}` (the `readFromNote` slots: id, type, choices), `{{notes}}` (each note: a call-local number and its four fields; ids are meaningless).
- **Output** (the agent's reply, checked against `schemas/notes-judge.schema.json`): `{ "notes": [{ "n", "claims": [string], "outsideKind": string, "claimEvidence": [quote], "roles": [{ "claim", "relation", "position", "evidence": [quote] }], "events": [{ "claim", "kind", "polarity": "stated" | "denied" | "disputed" | "notMentioned" | "unclear", "actor", "disputedBy", "evidence": [quote], "date": value, "amount": value }], "slots": [{ "slot", "value", "evidence": [quote] }], "requests": [{ "kind", "evidence": [quote], "amount": value }], "values": [value], "remarks": string }] }`, with `quote` and `value` as in `notes/note.schema.json` (`roles` a list; `amountDisputed` where a party contests the figure; without `start` and `length`: a script adds them). **`unclear` has one form per field:** `"claims": ["unclear"]`; an event's `"polarity": "unclear"`; a slot's `"value": "unclear"`; a value's `"normalized": "unclear"`.
- **What the answer becomes:** the `labels` of `note.schema.json`, field for field, minus `n` and `remarks` (the remarks are kept beside the note for the guide's list of defects). `unclear` never reaches gold: it is a disagreement.
- **Why blind:** a judge shown the intended labels confirms them. Agreement is evidence only if the judge could have disagreed.
- **Why "unclear" is allowed:** forcing a choice turns the guide's gaps into random agreement. `unclear` sends the note to the disagreement rule and the sentence to the guide's list of defects.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

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
   **The plaintiff's role** (guide, section 3b): for each claim type you chose for which the list above gives more than one role, say which of those roles the plaintiff holds, and quote the words that show it; `unknown` when the note does not say.
2. **Events** (sections 4 and 5): for **every** element of every claim type you chose, give its polarity. For `stated`, `denied` and `disputed`, quote the words that carry it; for `disputed`, say who contests. Give the date and the amount when the element takes one and the note gives it for that event. `notMentioned` has no quote. Go through the hard cases of section 4.2 whenever a sentence has a negation, a restriction (لم … إلا / سوى), a party's contest, an intention, a demand, or a document named in "documents".
3. **Slots** (section 6): every slot listed above, by its own rule. Remember that a slot can differ from the event of the same name, and that the date of a record is not the date of its delivery.
4. **Requests** (section 7) and **every date and amount** (section 8), normalised, each with its roles; one entry for each place a value is written.
5. If, for a field, two rules of the guide seem to fit, or the sentence can honestly be read two ways, answer `unclear` for that field and say in "remarks" which sentence and which two readings. For a claim type, write `"claims": ["unclear"]`; for an event, `"polarity": "unclear"`; for a slot, `"value": "unclear"`; for a date or an amount, `"normalized": "unclear"`. Do not use `unclear` to avoid a decision the guide makes.

Every quote must be copied **letter for letter** from the field you name: it is checked by a program, and a quote that is not found makes your label invalid.

Label each note on its own. Do not let one note's answer influence another's. Answer for every note you were given.

Use no tool. Everything you need is in this prompt, and your answer is your reply. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "notes": [ { "n": 1, "claims": ["…"], "outsideKind": "…", "claimEvidence": [quote], "roles": [role], "events": [event], "slots": [slot], "requests": [request], "values": [value], "remarks": "…" } ] }`

One entry of "notes" per note, in the order given. In each entry:

- "n": the note's number as given above.
- "claims": the claim type ids you find (one to three), or `["outside"]`, or `["unclear"]`.
- "outsideKind": only when "claims" is `["outside"]`: the kind, as the guide lists them. Otherwise leave the key out.
- "claimEvidence": quotes that show the claim type; may be an empty list.
- "roles": one entry for each claim type you answered for which more than one role is listed above: `{ "claim": "<claim id>", "relation": "<relation>", "position": "<position>", "evidence": [quote] }`, the pair being one of the roles listed for that claim type; or `"relation": "unknown", "position": "unknown"` with an empty "evidence" when the note does not say which. An empty list when none of your claim types offers a choice.
- "events": **one entry for every element of every claim type you answered, mentioned or not**: `{ "claim": "<claim id>", "kind": "<event kind>", "polarity": "stated" | "denied" | "disputed" | "notMentioned" | "unclear", "actor": "plaintiff" | "defendant" | "other" | "unknown", "disputedBy": "plaintiff" | "defendant" | "other", "evidence": [quote], "date": value, "amount": value }`. Give "disputedBy" only when the polarity is "disputed"; give "date" and "amount" only when the note states them for this event; "evidence" has at least one quote unless the polarity is "notMentioned".
- "slots": one entry for every slot listed above: `{ "slot": "<slot id>", "value": "<yes, no or unknown; a choice id; a date; or unclear>", "evidence": [quote] }`. No evidence for "unknown".
- "requests": one entry per request the note makes: `{ "kind": "<request kind>", "evidence": [quote], "amount": value }` ("amount" only when stated).
- "values": every date and every amount written in the note, one entry for each place it is written.
- "remarks": what the guide did not let you decide, in a sentence; `""` when nothing.

A **quote** is `{ "field": "subject" | "facts" | "requests" | "documents", "quote": "<words copied exactly from that field>", "occurrence": 1 }`. "occurrence" counts from 1 and matters only when the same words appear more than once in the field. Give no positions: a script finds them.

A **value** is `{ "kind": "date" | "amount", "at": quote, "normalized": "<the guide's normal form, or unclear>", "roles": ["<what the value is for, as the guide's section on values names it>"] }`, with, only when they apply, `"relative": true`, `"calendar": "hijri"` (a date is "gregorian" unless you say otherwise, and you need not write it), `"currency": "<the currency>"`, `"amountDisputed": true`.

**"unclear" has one form per field:** `"claims": ["unclear"]`; an event's `"polarity": "unclear"`; a slot's `"value": "unclear"`; a value's `"normalized": "unclear"`. Use it when the guide does not settle the answer; do not guess.
