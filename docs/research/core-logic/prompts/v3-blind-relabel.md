# Blind relabelling of the mirror set and of hidden set v3 (isolated Opus agents)

- **Model:** Opus 5.5, **one fresh, isolated agent per relabel batch** (25 texts, two of them known-truth probes), launched by the route the owner chose for hidden-set agents ([../specs/hidden-v3-commands.md](../specs/hidden-v3-commands.md), section 6: the Workflow tool with a reply schema of integers only, or its fallback). Codex was the relabeller until the owner's decision of 14 November 2026 (evening): relabelling is not one of Codex's three roles, and **no text of the mirror set or of v3 is ever sent to Codex**.
- **One family.** The writers are Sonnet, the relabellers Opus, and a v3 case is its writer's own: no voice outside the Claude family checks a label of these sets. The known-truth probes and the mirror's third judge are the compensation (the specification, section 7b). Say so beside every mirror and v3 figure.
- **What the agent is launched with,** and nothing more: "Your whole task is written in the file `<root>-relabel\batch-<nnn>.prompt.md`. Read it with your Read tool and follow it exactly. Reply with the JSON object it asks for and nothing else."
- **The prompt file is built by `v3 relabel prepare`** from the body below, with everything pasted in. **The builder never opens a built prompt file**: it holds hidden notes. The command is tested on fixtures and on the mirror.
- **Placeholders:** those of [notes-judge.md](notes-judge.md) (`{{guide}}`, `{{elements}}`, `{{requestKinds}}`, `{{slots}}`, `{{notes}}`), and `{{prompt_path}}` (this batch's own prompt file), `{{labels_path}}` (`<root>-relabel\batch-<nnn>.labels.json`), `{{batch}}` (the relabel batch's number). `{{notes}}` holds the four text fields of each note and a call-local number. **Never** the writer's labels, the plan, a note's id, the path of `<root>`, or which texts are probes.
- **The labelling text is [notes-judge.md](notes-judge.md)'s, word for word** (a test asserts it). The hidden set's relabeller must apply the same guide in the same words as the open sets', or the two measures would not be comparable.
- **Output:** the labels file, in the answer shape of the body (`schemas/notes-judge.schema.json`), read only by `v3 relabel collect`; and the reply `{"batch": <n>, "relabelled": <n>}`: two integers.
- **Checks by `v3 relabel collect`:** the schema; every quote found in its note; every element of the answered claim types answered; one entry per text; **the probe bar** (the two probes' counted fields at least 95 % right). A file that fails a check is set aside and its notes go back to the queue once, for a fresh agent.
- **A reply that holds anything but the two integers is a leak:** stop, `v3 discard --relabel <nnn>`, tell the owner in one line without the text. `records.py ingest-reply` is never run on a relabeller's transcript, and nobody reads it.
- **What the builder sees:** `batches valid`, `batches invalid`, `batches under the probe bar`, `notes relabelled`, `probe fields`, `probe fields right`. Nothing else.
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

**Where you work.** Your whole task is this file, `{{prompt_path}}`. You may read **this file and no other**, and you may write **one file and no other**: `{{labels_path}}`. Open nothing else: no other file of this folder or of any other folder, no repository, no memory file, no plan file and no state file. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Run no command. Save nothing to any memory. Never copy a note, a label or a quote into your reply.

## Your answer

Write one JSON object, and nothing else, to the file `{{labels_path}}`:

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

**Your reply** is exactly this JSON object, with no text before or after it: `{"batch": {{batch}}, "relabelled": <the number of notes you labelled>}`. Two whole numbers and nothing else. A reply that holds any other text discards your whole batch.
