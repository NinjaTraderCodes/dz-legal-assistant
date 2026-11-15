# A second family's view on disputed boundary cases (Codex)

- **Model:** Codex, one call per unit whose verifier (Opus 5.5) set aside boundary cases: stage 8b of [../specs/unit-driver.md](../specs/unit-driver.md) (owner's decision of 14 November 2026). It goes through the background queue of [../specs/codex-calls.md](../specs/codex-calls.md).
- **Why:** the boundary writer (Sonnet 5.5) wrote what a case's outcome should be from the texts alone; the reconciler disputed some of those outcomes and the verifier agreed with it, both Opus. A dispute settled inside one model family is not settled. Codex is given the texts and the cases, blind: neither the writer's expectation nor the record's answer, nor which side anyone took.
- **Placeholders:**
  - `{{subject}}`: what the disputed cases are about, in a few plain words, taken by the driver from the unit's row of the handbook's table (for `formal-notice`: "the formal notice (الإعذار) required before the claim is filed"). It names the subject, never an answer.
  - `{{texts}}`: the unit's `rules/reports/<unit>.texts.json` rendered `[<code> <article> ¶<n>] <text>`, plus every paragraph that an item matched by one of the cases cites and that file lacks, from the article store.
  - `{{cases}}`: each case's id, its title (the writer's one-line summary of the facts: it holds facts no slot can, such as a rescission clause in the contract), its petition date and the facts its slots state, in plain words; never its `why` or its expected outcome. Every fact a case does not state is said to be unknown.
  - `{{choices}}`: the outcome words in dispute for these cases (the writer's and the record's, in alphabetical order so that neither side shows), each with its meaning from the table in the body. Always at least two.
- **Output schema:** `schemas/boundary-view.schema.json`: `{ "cases": [{ "id": string, "answer": "ask" | "inTime" | "lastDay" | "late" | "leavesOrdinaryCourt" | "stepDone" | "stepExempt" | "stepMissing" | "unsettled", "why": string, "decidingWords": string }] }`. The driver rejects an answer that is not an outcome word at all, and queues the call again once; an outcome word other than the two offered is kept as a third answer, which makes the case uncertain (since 15 November 2026: specs/unit-driver.md, "Stage 8b").
- **How it is used:** a case on which Codex gives the boundary writer's outcome becomes **uncertain**: the record keeps both readings in an open point and warns (never settled by one family alone). A case on which Codex gives the record's answer stays as the verifier decided it: a counted failure with the two-family reason. An answer that is neither side's is treated as the writer's (the case becomes uncertain). The answer is kept as returned in `rules/reports/<unit>.boundary-view.json`.
- **The shape is repeated in the body** (the model sees only the body).

--- PROMPT ---

You are given official Algerian legal texts (Arabic, each paragraph numbered) and a few short cases. For each case, decide what a rule applying these texts should tell the lawyer about {{subject}}. Use only the texts below, not your memory of the law.

**The texts:**

{{texts}}

**The cases** (a fact a case does not state is unknown: nobody has asked the lawyer about it yet):

{{cases}}

**The possible answers for these cases:**

{{choices}}

Judge each case on its own.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "cases": [ { "id": "<the case's id as given>", "answer": "<one of the words offered above>", "why": "<two or three sentences>", "decidingWords": "<Arabic words copied from the texts>" } ] }`

- One entry per case, in the order given.
- "answer": exactly one of the words offered above under "The possible answers for these cases". The words a case can ever use are these nine; only those offered above are open here:
  - "inTime": the time limit has not passed on the petition date;
  - "late": the time limit has passed;
  - "lastDay": the petition date is the last day of the time limit;
  - "stepMissing": a step required before filing was not done, and the rule must say so;
  - "stepDone": the step was done as the texts require;
  - "stepExempt": the texts exempt this case from the step;
  - "leavesOrdinaryCourt": the case does not belong before the ordinary court;
  - "ask": the answer depends on a fact that is unknown, and the rule must ask the lawyer for it;
  - "unsettled": the texts do not settle the outcome, and the rule must warn without deciding.
- "why": which paragraphs decide it; and if you answer "ask", which unknown fact the answer depends on.
- "decidingWords": the Arabic words of the texts that decide it, copied letter for letter.

Do not run any command and do not open any file.
