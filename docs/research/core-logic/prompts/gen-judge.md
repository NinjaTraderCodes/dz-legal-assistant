# Blind judge of the applied-law sentences (step 5)

- **Models:** Sonnet 5.5 (fresh agent, 15 sheets per run) and Codex (8 sheets per call), independently. This is Codex's second role as a blind judge (owner's decision of 12 November).
- **No tool.** The Sonnet judge uses no tool: its inputs are pasted into the prompt and its answer is its reply. The executor reads the run's reported tool-use count; **a run with any tool use is discarded and run again by a fresh agent**, as a Codex call that ran a command is discarded. The paragraph "Where you work" below is sent to Sonnet only; Codex gets the prompt without it (it runs in an empty scratch folder, and its prompt ends with the no-command sentence).
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`; named only so that the judge knows it is off limits). `{{sheets}}`: for each sheet, the articles, the facts, the element to argue, and the candidates' paragraphs (with any Jais marker lines already stripped) labelled with letters **in random order per sheet** (the template's output is one of the candidates, unmarked; outputs that the faithfulness script already rejected are **still included**, so that the judges' ability to notice invented items can be measured against the script).
- **Output** (Codex schema `schemas/gen-judge.schema.json`, or the agent's reply): `{ "sheets": [{ "n", "candidates": [{ "letter", "faithful": boolean, "invented": [string], "appliesTheText": 1-5, "legalArabic": 1-5, "usable": boolean }], "best": "<letter>", "templateGoodEnough": "<the letter you think is a fixed template, or none>" }] }`.
- **What is measured.** (1) The script's faithfulness rate per writer (the main measure): the share of outputs with no invented item ([gen-apply-law.md](gen-apply-law.md)). (2) Each judge's `usable` rate per writer, and where the two judges differ. (3) How often each writer is `best`. Every rate with its Wilson interval.
- **Conclusion rule, fixed in advance, in this order of precedence.** The usable rate that counts for a writer is **the lower of the two judges'**. "Faithful" is the script's rate. The rule compares point estimates; the intervals are reported beside them (with about 120 sheets: 98 % is 118 outputs or more, 90 % is 108 or more).
  1. If the template is usable in at least 90 % of sheets, and no model exceeds the template's usable rate by more than 10 points while being faithful in at least 98 %: **no generative model is needed** for this sentence.
  2. Otherwise, if a model is faithful in at least 98 % and usable in at least 90 %: **name it** (if several, the one with the highest usable rate); no fine-tuning is needed.
  3. Otherwise: **put the evidence to the owner**: each writer's rates, the kinds of error, and whether training could plausibly fix them (say which).

  **Nothing is fine-tuned without the owner's word.**

--- PROMPT ---

**Where you work** (Sonnet only). You work without tools. Do not open any file and run no command: not in the repository (`{{worktree}}`), not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. Everything you need is below, and your answer is your reply.

For each case below you have: official Algerian legal texts, the facts established in a file, what a paragraph of a statement of claim (عريضة افتتاح دعوى) is supposed to argue, and several candidate paragraphs written by different authors, labelled with letters.

{{sheets}}

For each candidate:

- "faithful": true only if **every** date, sum, name, number and article reference in the paragraph is in the facts or the texts given, and the paragraph attributes to an article nothing that the article's text does not say. List anything else in "invented" (the invented date, sum, article, or the claim the article does not make). Check each number against the sheet; do not trust the overall impression.
- "appliesTheText" (1 to 5): does it connect the fact to the text and draw the consequence for this case, without merely restating the article or the facts?
- "legalArabic" (1 to 5): correct formal legal Arabic as used in Algerian pleadings; natural; no padding.
- "usable": true if a lawyer could keep the paragraph with at most a light edit. A paragraph that is not faithful is never usable.

Then "best": the letter of the best usable candidate (or "none"). And "templateGoodEnough": if one candidate looks like a fixed formula filled with the case's details, give its letter; otherwise "none".

Judge each case on its own; the letters are shuffled from case to case.

Do not run any command and do not open any file.
