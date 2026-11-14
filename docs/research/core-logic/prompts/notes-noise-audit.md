# The noise measurement: a blind relabel of kept training notes, by Opus, that changes nothing

- **Model:** Opus 5.5, fresh agents, **no tool**, 25 notes per run. For each random 100 of every 1,000 kept training notes (and 100 of the pilot's): **four runs of this prompt, then one sorting run** ([notes-sort-dropped.md](notes-sort-dropped.md)) on the fields that differ. Five Opus runs per 100 notes; say so before launching.
- **It measures; it changes no note** (owner, 14 November 2026; [../notes/TERMS.md](../notes/TERMS.md)): "Opus only measures label noise on a sample and stops to show me if it passes 10 %." A note this measurement finds wrong is **neither corrected nor dropped because of it**. Its answers are kept in `C:\Users\TheShadow\dz-core-research\notes-work\noise\<date>\`, never in a note, and never reach the training loader.
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against `schemas/notes-judge.schema.json`. A run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:** those of [notes-judge.md](notes-judge.md). `{{notes}}` holds the four text fields of each sampled note and a call-local number: **never** the scenario, the note's labels, its blanks, the filter's answers or its id.
- **The body is [notes-judge.md](notes-judge.md)'s, word for word** (a test asserts it): the measure must apply the guide exactly as the relabellers of the test sets do.
- **What the script does** ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), "The noise measurement"): `notes compare` lists the fields where this answer and the note's kept labels differ (blanked fields are not compared); the sorter says which side is right; the label noise is the share of compared fields whose kept label is wrong, with its interval. **Over 10 %: stop and show the owner. 5 % to 10 %: tighten the filter's sheet and measure again on the next 1,000.**
- **The shape is in the body** (agents see only the body).

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
