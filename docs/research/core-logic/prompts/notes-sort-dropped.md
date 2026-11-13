# Sorting the dropped and blanked notes

- **Model:** Opus 5.5, fresh agent. A random 150 to 200 notes among those dropped or blanked in the pilot, train, dev and challenge sets, about 25 per run. **Never hidden v3, and never the mirror set**: the mirror chooses nothing, and its notes come from v3's writers, not from scenarios.
- **The sorter uses no tool.** The guide and the cases are pasted in the prompt, it needs no file path, and its answer is its reply. The executor reads the run's reported tool-use count: a run with any tool use is discarded and run again by a fresh agent.
- **Give it:** for each note: its text, the scenario, the writer's labels, the judge's labels, and the fields on which they differ. This is the one role that sees both sides.
- **Placeholders:** `{{guide}}`, `{{cases}}`.
- **Output:** JSON array: `{ "n", "field", "cause": "writer" | "judge" | "note" | "guide", "right": "writer" | "judge" | "neither" | "both readings hold", "explanation", "guideDefect": "<the rule or case that is missing or unclear, when cause is guide>" }` (one entry per differing field).
- **How it is used:** the four shares are reported. `guide` entries are fixed in the guide (then the guide test runs again and the scheme version changes). If `judge` is above about a quarter, the judging prompt or the judge is the problem. `writer` entries of one kind (say, restriction read as negation) go into the writer's prompt as a warning in general terms, never as a copied example.
- **Why:** dropping disagreements silently removes exactly the hard notes, and hides whether the writer, the judge or the guide is at fault. This sorting is how the pipeline learns.

--- PROMPT ---

A writer wrote lawyers' notes from scenarios and labelled them; a judge who saw only the notes labelled them again. On the fields below they disagree. For each disagreement, find the cause. Be an umpire: the guide decides, not the writer's intention and not the judge's confidence.

==== THE LABELLING GUIDE ====

{{guide}}

==== THE CASES ====

Each case gives the scenario (what the writer was asked to express), the note, and, for each field in dispute, the writer's label with its quote and the judge's label with its quote.

{{cases}}

For each field in dispute, decide the **cause**:

- **writer:** the note does not say what the scenario asked (the fact is missing, has another polarity, or the quoted words do not carry it), and the judge labelled the note correctly.
- **judge:** the note says what the scenario asked, clearly, by the guide; the judge misapplied a rule that the guide states.
- **note:** the note is honestly ambiguous on this point: a careful reader could take it either way, and the guide's rules, applied properly, do not choose. (The fix is a rewrite.)
- **guide:** the two labels follow from two readings of the **guide**: a rule is missing, two rules collide, or a term is not defined. Say exactly which, in "guideDefect", and propose the missing rule in one sentence.

Give "right" (whose label the guide supports), and a short "explanation" that names the rule or case of the guide and the words of the note.

Do not split the difference. If the guide clearly decides, the cause is the writer or the judge, even if the mistake is understandable.

Return only the JSON array.

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file.
