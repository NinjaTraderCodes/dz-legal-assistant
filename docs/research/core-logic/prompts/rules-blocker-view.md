# Blocker view: a second, independent reading of the items that would block

- **Model:** Sonnet 5.5, a fresh agent with **no tool**, one run per claim type on the catalogue route, and only when its derivation has at least one item that would block the clean export (stage 2e of `scripts/unit.py`).
- **Why it exists:** the owner, 18 November 2026, on the catalogue route: "the few items that would block get a second, independent derivation (only those items), so they can still block." The catalogue route has no derivation B, so without this view no item could pass condition 4 of `rules/GRADES.md` (derivations A and B agreed). This is that second derivation, narrowed to those items.
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` (through `unit.py done <unit> 2e <agent id>`) into `rules/reports/<unit>.blocker-view.json` and checked against `schemas/blocker-view.schema.json`. A run with any other tool use is discarded and run again by a fresh agent.
- **Blind:** it is given **only** the texts and, for each would-be blocking item, the question the item answers (written by the deriver in `derivations/a/<unit>.catalogue-changes.json`, `wouldBlock`; `unit.py done` refused a question holding a digit or the item's extract). Never the record, the item's period, its sanction, its cites or its message.
- **Placeholders:** `{{unit_label}}` (`prompts.unit_label`); `{{texts}}` (the unit's texts, `reports/<unit>.texts.json`, field `rendered`; then, under a line of its own, every article the questioned items cite that those texts lack, whole, from the store, labelled the same way); `{{questions}}` (one line per item: its id, whether it is a time limit or a step before filing, and the question in English and Arabic).
- **Output schema:** `schemas/blocker-view.schema.json`: `{ "items": [{ "item", "textsRequire": "yes" | "no" | "textsSilent", "periodValue": integer | null, "periodUnit": "day" | "month" | "year" | "none", "consequence": "inadmissibleOrForfeited" | "onlyIfRaised" | "noSanction" | "textsSilent", "basis": [ "[code article ¶n]" ], "decidingWords", "why" }] }`.
- **What the script does with it** (`unit.py`, stage 5 of the catalogue route, before stage 6): an item keeps `blockCleanExport`, and counts as "derivations A and B agreed", only when the reader answers `textsRequire` "yes", `consequence` "inadmissibleOrForfeited", the same period as the record for a deadline, and cites at least one article the item rests on. Any other answer sets the item to warn, with the reason, in the promoted record.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

Below are official Algerian legal texts and a few questions about one kind of claim. Answer each question from these texts alone. No lawyer will check your answer, and another reader answered the same questions separately: an honest "the texts are silent" is worth more than a confident guess.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in any repository, not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

**The claim:** {{unit_label}}

**The texts** (each paragraph labelled `[code article ¶n]`):

{{texts}}

**The questions** (each is about a time limit for filing the claim, or a step to take before filing it):

{{questions}}

For each question:

1. Find the paragraphs that answer it. Read the paragraph before and after each: an exception often sits there.
2. Decide whether the texts **require** it: for a time limit, whether a text sets a time within which this claim must be brought; for a step, whether a text requires the step before this claim is filed.
3. For a time limit, the period the text states, as a number and a unit.
4. Decide what the texts say follows when it is missing (the step was not taken, or the claim was filed late): the claim is inadmissible or the right is lost, and a court must so hold; or it counts only if the other side raises it; or the texts state no consequence.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "items": [ { "item": "<the id printed before the question>", "textsRequire": "<one of its words below>", "periodValue": <an integer, or null>, "periodUnit": "<one of its words below>", "consequence": "<one of its words below>", "basis": ["[code article ¶n]"], "decidingWords": "<Arabic words copied from the texts>", "why": "<one or two sentences>" } ] }`

- One entry per question, in the order given, and no other entry.
- "item": the id printed before the question, exactly.
- "textsRequire", one of: "yes" (a text requires the step, or sets the time limit, for this claim); "no" (the texts show it is not required for this claim); "textsSilent" (the texts given do not say).
- "periodValue": for a time limit, the number of the period the text states (an integer); `null` for a step, or when the texts state no period.
- "periodUnit", one of: "day", "month", "year"; "none" for a step, or when the texts state no period.
- "consequence", one of:
  - "inadmissibleOrForfeited": when it is missing, the claim is not accepted or the right is lost, and the court must so hold;
  - "onlyIfRaised": it counts only when the other side raises it;
  - "noSanction": the texts state the duty and no consequence;
  - "textsSilent": the texts given do not say.
- "basis": the labels of the paragraphs your answer rests on, exactly as printed above (for example `[civil-code 179 ¶1]`); `[]` only with "textsSilent".
- "decidingWords": the Arabic words that decide, copied letter for letter from the texts above; `""` only with "textsSilent".
- "why": one or two sentences a reader can check against the texts.
