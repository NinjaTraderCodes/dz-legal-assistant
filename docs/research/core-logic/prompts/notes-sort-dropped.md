# Sorting the dropped and blanked notes

- **Model:** Opus 5.5, fresh agent. A random 150 to 200 notes among those dropped or blanked in the pilot, train, dev and challenge sets, about 25 per run. **Never hidden v3, and never the mirror set**: the mirror chooses nothing, and its notes come from v3's writers, not from scenarios.
- **The sorter uses no tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply`. A run with any other tool use is discarded and run again by a fresh agent.
- **Give it:** for each note: its text, the scenario, the writer's labels, the checker's labels (the local filter's answers for a training note, the relabeller's for a test note), and the fields on which they differ. This is the one role that sees both sides. It changes no note: it is a measurement.
- **Placeholders:** `{{guide}}`, `{{cases}}`.
- **Output:** JSON array: `{ "n", "field", "cause": "writer" | "checker" | "note" | "guide", "right": "writer" | "checker" | "neither" | "bothReadingsHold", "explanation", "guideDefect": "<the rule or case that is missing or unclear, when cause is guide>" }` (one entry per differing field).
- **How it is used:** the four shares are reported. `guide` entries are fixed in the guide (then the guide's version is bumped and the guide test runs again on the items it touches). If `checker` is above about a quarter, the filter's sheet, the relabelling prompt or the checker is the problem. `writer` entries of one kind (say, restriction read as negation) go into the writer's prompt as a warning in general terms, never as a copied example.
- **Why:** dropping disagreements silently removes exactly the hard notes, and hides whether the writer, the checker or the guide is at fault. This sorting is how the pipeline learns.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).
- **Output schema:** `schemas/sort-dropped.schema.json` (the array of the body). **Two uses.** (1) The sorting of dropped and blanked notes: the checker is the local filter (a training note) or the blind relabeller (a test note). (2) **The noise measurement**: the "writer's label" is the note's kept label, the "checker" is Opus's blind relabel; **a kept label counts as wrong when `right` is `checker` or `neither`**; `bothReadingsHold` and the cause `guide` are counted apart, as unsettled. One run per 40 differing fields.

--- PROMPT ---

A writer wrote lawyers' notes from scenarios, and the scenario gave each note its labels; a checker who saw only the notes then answered about them. On the fields below the two differ. For each difference, find the cause. Be an umpire: the guide decides, not the writer's intention and not the checker's confidence.

==== THE LABELLING GUIDE ====

{{guide}}

==== THE CASES ====

Each case gives the scenario (what the writer was asked to express), the note, and, for each field in dispute, the writer's label with its quote and the checker's label (with its quote when the checker gave one: a checker that answers closed questions gives none).

{{cases}}

For each field in dispute, decide the **cause**:

- **writer:** the note does not say what the scenario asked (the fact is missing, has another polarity, or the quoted words do not carry it), and the checker labelled the note correctly.
- **checker:** the note says what the scenario asked, clearly, by the guide; the checker misapplied a rule that the guide states.
- **note:** the note is honestly ambiguous on this point: a careful reader could take it either way, and the guide's rules, applied properly, do not choose. (The fix is a rewrite.)
- **guide:** the two labels follow from two readings of the **guide**: a rule is missing, two rules collide, or a term is not defined. Say exactly which, in "guideDefect", and propose the missing rule in one sentence.

Give "right" (whose label the guide supports), and a short "explanation" that names the rule or case of the guide and the words of the note.

Do not split the difference. If the guide clearly decides, the cause is the writer or the checker, even if the mistake is understandable.

Return only the JSON array.

Use no tool. Everything you need is in this prompt, and your answer is your reply. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file.

## Your answer

Return only one JSON array, with no text before or after it, **one entry per differing field**:

`[ { "n": 1, "field": "<the field that differs, as the case names it>", "cause": "writer" | "checker" | "note" | "guide", "right": "writer" | "checker" | "neither" | "bothReadingsHold", "explanation": "<one or two sentences>", "guideDefect": "<the rule or case that is missing or unclear>" } ]`

- "n": the case's number as given.
- "cause": who or what produced the difference: "writer" (the note does not say what the scenario asked for, or its labels are wrong), "checker" (the checker misread a note that is clear), "note" (the note itself is ambiguous), "guide" (the guide does not settle it).
- "right": whose label is right under the guide: "writer", "checker", "neither", or "bothReadingsHold".
- "guideDefect": only when "cause" is "guide"; otherwise `""`.
