# A second family's view on disputed boundary cases (Codex)

- **Model:** Codex, one call per module whose reconciler and verifier (both Opus 5.5) set aside boundary cases (owner's decision of 14 November 2026). Rules of [../specs/codex-calls.md](../specs/codex-calls.md).
- **Why:** the boundary writer (Sonnet 5.5) wrote what a case's outcome should be from the texts alone; the reconciler disputed some of those outcomes and the verifier agreed with it, both Opus. A dispute settled by one model family against another is not settled. Codex is given the texts and the cases, blind: neither the writer's expectation nor the record's answer, nor which side anyone took.
- **Placeholders:**
  - `{{texts}}`: the module's `rules/reports/<unit>.texts.json` rendered `[<code> <article> ¶<n>] <text>`, plus every paragraph that an item matched by one of the cases cites and that file lacks, from the article store.
  - `{{cases}}`: each case's id, its title (the writer's one-line summary of the facts: it holds facts no slot can, such as a rescission clause in the contract), its petition date and the facts its slots state, in plain words; never its `why` or its expected outcome. Every fact a case does not state is said to be unknown.
  - `{{choices}}`: the two answers in dispute, each with what it means.
- **Output schema:** `schemas/boundary-view.schema.json`: `{ "cases": [{ "id": string, "answer": "ask" | "stepMissing", "why": string, "decidingWords": string }] }`.
- **How it is used:** a case on which Codex gives the boundary writer's outcome becomes **uncertain**: the record keeps both readings in an open point and warns (never settled by one family alone). A case on which Codex gives the record's answer stays as the verifier decided it. The answer is kept as returned in `rules/reports/<unit>.boundary-view.json`.

--- PROMPT ---

You are given official Algerian legal texts (Arabic, each paragraph numbered) and a few short cases. For each case, decide what a rule applying these texts should tell the lawyer about the formal notice (الإعذار) before the claim is filed. Use only the texts below, not your memory of the law.

**The texts:**

{{texts}}

**The cases** (a fact a case does not state is unknown: nobody has asked the lawyer about it yet):

{{cases}}

**The two possible answers:**

{{choices}}

For each case give "id", "answer" (one of the two), "why" (two or three sentences: which paragraphs decide it, and if you answer "ask", which unknown fact the answer depends on), and "decidingWords" (the Arabic words of the text that decide it, copied exactly). Judge each case on its own.

Do not run any command and do not open any file.
