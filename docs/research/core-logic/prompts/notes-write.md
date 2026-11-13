# Note writer

- **Model:** Codex, about 20 scenarios per call ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 3). On the cross-check sample: a fresh Sonnet 5.5 agent, same prompt, replying with the same JSON.
- **A Claude writer uses no tool.** Its inputs are pasted in the prompt and its answer is its reply; it needs no file path. The executor reads the run's reported tool-use count: a run with any tool use is discarded and run again by a fresh agent, exactly as a Codex call that ran a command is discarded. A rejected Codex call, and a usage-limit refusal, follow the one rule of the notes pipeline, section 3.
- **Placeholders:** `{{guide}}` (the whole labelling guide, at the frozen version), `{{elements}}` (for the claim types of this call: each element's kind and Arabic proposition; for an `outside` scenario, nothing), `{{requestKinds}}` (every kind of `FactVocabulary.Requests`, with an English gloss and the Arabic label where `claim-elements.json` gives one: all of them, so that an `outside` scenario's request has its kind), `{{scenarios}}` (the call's scenarios as JSON, **without** `family`, `outcome` and `id` beyond a call-local number; with the style card's text and parameters in place of its id).
- **Output schema:** `schemas/notes-write.schema.json`: `{ "notes": [{ "n": integer, "subject": string, "facts": string, "requests": string, "documents": string, "facts_of_scenario": [{ "ref": string, "expressed": boolean, "quotes": [{ "field": "subject" | "facts" | "requests" | "documents", "quote": string, "occurrence": integer }] }] }] }`. `ref` names the scenario's fact: `claim`, `event:<claim>/<kind>`, `event-date:<claim>/<kind>`, `event-amount:<claim>/<kind>`, `slot:<id>`, `request:<kind>`, `request-amount:<kind>`, `value:<what>`. One to three quotes for an expressed fact; none when `"expressed": false`, which is allowed only for an extra value marked `optional`.
- **What the answer becomes:** the writer chooses no label. Its labels are the scenario's (claims, polarity, actor, disputedBy, slot values, normalised values) plus its quotes; the script `writer-labels` turns them into `note.schema.json` field for field, as the pipeline's section 3 sets out.
- **Why "write first, then point":** asked for labels and text together, a model writes the label's words into the note. Asked to write a lawyer's note and afterwards to point at the words that carry each fact, it notices when its note does not say what the scenario says.
- **Why the scenario's facts are in English:** so that no Arabic phrasing comes from the sampler. Every Arabic word of a note is the writer's.

--- PROMPT ---

You write the working notes an Algerian lawyer jots down before drafting a statement of claim (عريضة افتتاح دعوى). The notes are in **formal Arabic only**: no dialect, no French, no English, no Latin letters at all. Everything is fictitious; use only the names given in each scenario.

These notes will be read by a program that must find the facts in them. So each note must say exactly what its scenario says, no more and no less, in the natural words of a lawyer, never in the words of a form.

==== HOW FACTS ARE LABELLED (read it all; your quotes are checked against it) ====

{{guide}}

==== THE ELEMENTS OF THE CLAIM TYPES IN THIS BATCH ====

{{elements}}

==== THE KINDS OF REQUEST ====

{{requestKinds}}

==== THE SCENARIOS ====

{{scenarios}}

==== WHAT TO DO, FOR EACH SCENARIO ====

**1. Write the note.** Four fields: "subject", "facts", "requests", "documents". Follow the scenario's style card for length, order, tidiness, which fields are used, how dates and amounts are written, how negation and dispute are worded, and which name to use for each act. Then:

- Every event marked `stated`, `denied` or `disputed` must be expressed with that polarity, in the sense of the guide's rules 1 to 3 and its hard cases. For `denied`, the note itself says the thing did not happen. For `disputed`, the note reports who contests it and how (use the scenario's "how" as the idea, in your own Arabic).
- An event with a "contest" stays `stated`: the other side admits it and contests only its amount (write both figures) or its cause (the other side gives its own reason for it).
- An event marked `notMentioned` must not be stated, denied, disputed or hinted at, and no document named after it may appear in "documents".
- A slot with value `unknown` must not be answerable from the note.
- Write every date and amount of the scenario exactly as given, in the format the style card asks. You may write one twice where a lawyer would (the sum owed in the facts, again in the requests). **Add no date and no amount that the scenario does not hold.**
- Mention the scenario's extra values and distractors as a lawyer would, in passing. You may leave out an extra value marked `optional`; nothing else may be left out.
- For a scenario with an "outsideFact", the note must say that fact plainly.
- Requests: only those listed, worded as a lawyer notes them.
- **Do not copy any example sentence from the guide, and do not reuse a sentence from another note of this batch.** Vary the vocabulary: a lawyer does not write «لم يدفع» every time.
- Do not use label words as labels: never write «واقعة منكرة», «متنازع فيه», «غير مذكور», or the name of a claim type as a heading unless the style card fills "subject".
- Do not explain the law, cite articles, or conclude ("الدعوى مؤسسة"). These are notes of facts and wishes.

**2. Then point at your own words.** In "facts_of_scenario", give one entry for every fact of the scenario, named by its "ref":

- `claim`: the words that best show what is being claimed (for a scenario with an "outsideFact", the words that say it);
- `event:<claim>/<kind>`: for each event that is not `notMentioned`, the words that carry its polarity (for `disputed`, the contest, and the assertion too when you wrote it; for an event with a "contest", the admission and the contest);
- `event-date:<claim>/<kind>` and `event-amount:<claim>/<kind>`: the event's date and amount, as written;
- `slot:<id>`: each slot that is not `unknown`;
- `request:<kind>` and `request-amount:<kind>`: each request and its amount;
- `value:<what>`: each extra value, with its "what" exactly as the scenario gives it.

For each, give one to three "quotes": the field and the **exact words of your note**, the shortest stretch that carries the fact by itself, 2 to 12 words for an event or a request, the value as written (with its unit) for a date or an amount, copied letter for letter. If you wrote a value twice, quote each place. If the same words occur twice in the field, give "occurrence" (1 for the first); otherwise give 1.

If you cannot quote words for a fact, your note does not say it: **go back and fix the note.** Use `"expressed": false`, with no quote, only for an extra value marked `optional` that you left out.

**3. Check each note once more** against its scenario before answering: polarity of every event; nothing extra that would state a `notMentioned` event; no date or amount that the scenario does not hold; values exact.

Return the notes in the order of the scenarios, each with its number "n".

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.
