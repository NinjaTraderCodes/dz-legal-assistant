# Blind judge of the petition sections (step 5)

- **Models:** Sonnet 5.5 and Opus 5.5, independently, fresh agents, 15 sheets per run. (Codex was the second judge until the owner's decision of 14 November 2026.)
- **No tool** except one read of the judge's own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against `schemas/gen-judge.schema.json`; **a run with any other tool use is discarded and run again by a fresh agent**. Both judges get the whole body.
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`; named only so that the judge knows it is off limits). `{{sheets}}`: for each sheet, the section to write and what it must contain, the facts, the articles (for the grounds section), and the candidates' texts ([../specs/step5-templates.md](../specs/step5-templates.md)) (with any Jais marker lines already stripped) labelled with letters **in random order per sheet** (the template's output is one of the candidates, unmarked; outputs that the faithfulness script already rejected are **still included**, so that the judges' ability to notice invented items can be measured against the script).
- **Output** (the agent's reply, checked against `schemas/gen-judge.schema.json`): `{ "sheets": [{ "n", "candidates": [{ "letter", "faithful": boolean, "invented": [string], "appliesTheText": 1-5, "legalArabic": 1-5, "usable": boolean }], "best": "<letter>", "templateGoodEnough": "<the letter you think is a fixed template, or none>" }] }`.
- **What is measured.** (1) The script's faithfulness rate per writer (the main measure). (2) Each judge's `usable` rate per writer, and where the two judges differ. (3) How often each writer is `best`. Every rate with its Wilson interval. Judged: 120 paragraphs of part A and 60 sheets of part B (24 runs).
- **The bars and the conclusion, fixed in advance:** [../specs/step5-templates.md](../specs/step5-templates.md), section 5. A writer passes a part when it is faithful in at least 98 % (the script) and usable in at least 90 % (**the lower of the two judges' rates**). Both judges are Claude models; say so beside every usable rate. **Nothing is fine-tuned without the owner's word.**
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in the repository (`{{worktree}}`), not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. Everything you need is below, and your answer is your reply.

For each case below you have: which section of a statement of claim (عريضة افتتاح دعوى) is to be written and what it must contain, the facts established in the file, the official Algerian legal texts when the section applies one, and several candidate texts for that section written by different authors, labelled with letters.

{{sheets}}

For each candidate:

- "faithful": true only if **every** date, sum, name, number and article reference in the paragraph is in the facts or the texts given, and the paragraph attributes to an article nothing that the article's text does not say. List anything else in "invented" (the invented date, sum, article, or the claim the article does not make). Check each number against the sheet; do not trust the overall impression.
- "appliesTheText" (1 to 5): for a paragraph of the legal discussion: does it connect the fact to the text and draw the consequence for this case, without merely restating the article or the facts? **For a facts section** (no legal text on the sheet) the same mark means: is every fact of the sheet told, once, in order, with nothing argued and nothing left out?
- "legalArabic" (1 to 5): correct formal legal Arabic as used in Algerian pleadings; natural; no padding.
- "usable": true if a lawyer could keep the paragraph with at most a light edit. A paragraph that is not faithful is never usable.

Then "best": the letter of the best usable candidate (or "none"). And "templateGoodEnough": if one candidate looks like a fixed formula filled with the case's details, give its letter; otherwise "none".

Judge each case on its own; the letters are shuffled from case to case.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "sheets": [ { "n": 1, "candidates": [ { "letter": "A", "faithful": true, "invented": ["…"], "appliesTheText": 4, "legalArabic": 4, "usable": true } ], "best": "A", "templateGoodEnough": "none" } ] }`

- One entry of "sheets" per sheet, "n" being the sheet's number as given; in it one entry of "candidates" per candidate, "letter" being its letter.
- "faithful": false as soon as the paragraph holds a fact, date, amount, name or article number that is not on the sheet. "invented": each such item, copied; an empty list when faithful.
- "appliesTheText" and "legalArabic": whole numbers from 1 (poor) to 5 (excellent).
- "usable": true only when a lawyer could keep the paragraph with light editing **and** it is faithful.
- "best": the letter of the best usable candidate of the sheet, or "none" when no candidate is usable.
- "templateGoodEnough": the letter of the candidate you think is a fixed template, or "none".
