# Attack on the Legal Review answers (Codex)

- **Model:** Codex, one call per group of questions (5 calls).
- **Placeholders:** `{{questions}}` (the group's questions, whole), `{{answer_a}}`, `{{answer_b}}` (the two independent answers, labelled only "first" and "second"), `{{texts}}` (every article either answer cites, found by a script from the answers' cite lines `[<code> <article> ¶<n>]` and pasted from the store, rendered the same way, with every paragraph of the same articles, the uncited ones included).
- **Output schema:** `schemas/legal-review-attack.schema.json`: `{ "questions": [{ "id": string, "agree": boolean, "betterSupported": "first" | "second" | "neither" | "same", "defects": [{ "in": "first" | "second" | "both", "claim": string, "evidenceWords": string }], "notSettledByTheseTexts": boolean }] }`.
- **How it is used:** the executor writes the final answer. A defect with `evidenceWords` from the texts is checked against the store and, when real, changes the answer. `notSettledByTheseTexts: true` on a question both answers called "settled" lowers its grade to C.

--- PROMPT ---

Two people answered, independently, questions about what an Algerian statement of claim (عريضة افتتاح دعوى) should say. No lawyer has checked either answer. You check both against the official texts below. Do not use your memory of Algerian law to decide: use it only to say what else should be looked up.

**The questions:**

{{questions}}

**The first answer:**

{{answer_a}}

**The second answer:**

{{answer_b}}

**The official texts they cite** (Arabic, paragraphs numbered; the paragraphs they did not cite are included):

{{texts}}

For each question:

- "agree": do the two answers choose the same option and give the same rule?
- "defects": every place where an answer says a text establishes something the text does not say; cites an article for a matter it does not cover; overlooks a paragraph of the texts below that changes the answer; or presents custom or belief as if a text said it. Copy the Arabic words that show the defect into "evidenceWords".
- "betterSupported": which answer the texts support better; "neither" if neither is supported; "same" if they are equally supported.
- "notSettledByTheseTexts": true if, on your reading, the texts below do not decide the question (it is then a matter of practice or of texts not given).

Be specific. "The second answer is clearer" is not a defect.

Do not run any command and do not open any file.
