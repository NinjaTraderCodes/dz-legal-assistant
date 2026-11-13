# Ranking the two local writers (the 20-note trial)

- **Models:** Sonnet 5.5 (fresh agent) and Codex (two calls of 10 scenarios), independently. Each sees, for each of the 20 scenarios, the scenario's facts and two notes labelled **K and L in random order per scenario** (the mapping to Jais-3-3.7B and Gemma 4 E4B is kept by the script).
- **A Claude ranker uses no tool.** The pairs are pasted in the prompt, it needs no file path, and its answer is its reply. The executor reads the run's reported tool-use count: a run with any tool use is discarded and run again by a fresh agent, exactly as a Codex call that ran a command is discarded. A rejected Codex call follows the one rule of the notes pipeline, section 3.
- **Placeholders:** `{{pairs}}`.
- **Output schema (Codex) / reply (Sonnet):** `schemas/clean-rank.schema.json`: `{ "pairs": [{ "n", "kFaithful": boolean, "lFaithful": boolean, "kErrors": [string], "lErrors": [string], "betterArabic": "K" | "L" | "same" }] }`.
- **Decision rule (fixed before the trial is run), by the owner's criterion:** the writer kept is "the better writer of formal legal Arabic" (the owner's words). For each judge, over the pairs where that judge found **both** notes faithful (no fact missing or added; every polarity, date and amount right), count the pairs where each writer's note is the better formal legal Arabic; add the two judges' counts. The writer with more is kept; if they are equal, Jais-3-3.7B (an Arabic-first model, and the candidate default of Part 7). **Faithfulness is reported beside it**: each writer's faithful rate, the share of its 20 notes found faithful, averaged over the two judges. **If the preferred writer's faithful rate is more than 20 points below the other's**, or if no pair has two faithful notes, tell the owner before the 1,000 are written. Report the counts from both judges and where they differ.
- **Why this order:** the owner asked for the better writer of formal legal Arabic. Comparing Arabic only where both notes are faithful keeps an unfaithful but fluent note from winning; the 20-point warning keeps a writer that is often unfaithful from being chosen without the owner seeing it, since a note that changes a polarity is worse than useless as training data.

--- PROMPT ---

For each case below you have the facts a lawyer's working note was supposed to express, and two notes, K and L, written from those facts by two different writers. The notes should be in formal legal Arabic as Algerian lawyers write it.

{{pairs}}

For each case:

- "kFaithful" and "lFaithful": true if the note expresses the facts exactly: every fact present, none added; each fact with the right status (happened / said not to have happened / contested, and by whom); each date and amount exact. List each note's errors in "kErrors" and "lErrors", each in a few words ("omits the delivery date", "says the notice was served; the facts say it was not", "adds an article of law"). A note with any error is not faithful.
- "betterArabic": which note is better formal legal Arabic: correct grammar, the profession's usual terms in Algeria, natural phrasing, no dialect and no foreign words, and the manner of a working note and not of a judgment or an essay. Judge the Arabic for every pair, whether or not the notes are faithful.

Answer "same" only when you truly see no difference. Judge each case on its own; K and L are shuffled from case to case.

Use no tool. Everything you need is in this message, and your answer is your reply. Do not read or write any memory file, do not open the plan file or the state file, and do not look for any other file. Do not run any command and do not open any file.
