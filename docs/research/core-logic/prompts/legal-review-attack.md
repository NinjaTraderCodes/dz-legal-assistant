# Attack on a Legal Review answer, by the other model

- **Models:** each group of questions has two independent answers ([legal-review-answer.md](legal-review-answer.md)): one by Opus 5.5, one by Sonnet 5.5. **A fresh Opus 5.5 agent attacks Sonnet's answer, and a fresh Sonnet 5.5 agent attacks Opus's** (5 groups, 10 runs). The attacker is never the agent that answered. (Codex attacked both answers in one call until the owner's decision of 14 November 2026.)
- **One family:** answerers and attackers are all Claude models. A belief about Algerian practice that both models share passes unseen. Say so at the head of `legal-review-answers.md` (HANDBOOK section 5b). What is checked mechanically: every cite line of an answer exists in the store, and every `evidenceWords` of an attack is found in the texts given.
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against the schema. A run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:** `{{questions}}` (the group's questions, whole, each with its id); `{{answer}}` (the one answer under attack, whole, with nothing that says which model wrote it); `{{texts}}` (every article the answer cites, found by a script from the answer's cite lines `[<code> <article> ¶<n>]` and pasted from the store, rendered the same way, with **every paragraph of the same articles**, the uncited ones included). The script is `records.py review-texts <answer file> --out <file>`, written in phase D with one test: every cite line of a fixture answer is either rendered from the store or listed as `notInStore`. A cite the store does not hold is itself reported to the executor as a defect of the answer.
- **Output schema:** `schemas/legal-review-attack.schema.json`: `{ "questions": [{ "id": string, "supported": "yes" | "partly" | "no", "defects": [{ "claim": string, "evidenceWords": string }], "notSettledByTheseTexts": boolean, "toLookUp": string }] }`.
- **Output file:** `docs/research/core-logic/reports/legal-review/<group>.attack-on-<opus|sonnet>.json`.
- **How it is used:** the executor writes the final answer from the two answers and the two attacks. A defect whose `evidenceWords` a script finds in the texts is checked against the store and, when real, changes the answer. Where the two answers chose different options, the final answer takes the one whose attack left it `supported: "yes"`; if neither or both, the question is marked **uncertain** with both readings and the reading that is safest for the lawyer. `notSettledByTheseTexts: true` on a question the answer called "settled by a text" lowers that question to "partly".
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

Someone answered questions about what an Algerian statement of claim (عريضة افتتاح دعوى) should say. No lawyer has checked the answer. You check it against the official texts below. Do not use your memory of Algerian law to decide: use it only to say what else should be looked up.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in any repository, not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

**The questions:**

{{questions}}

**The answer you check:**

{{answer}}

**The official texts it cites** (Arabic, paragraphs numbered; the paragraphs it did not cite are included):

{{texts}}

For each question, look for every place where the answer:

- says a text establishes something the text does not say;
- cites an article for a matter the article does not cover;
- overlooks a paragraph of the texts below that changes the answer;
- presents custom or belief as if a text said it.

Be specific. "The answer is unclear" is not a defect. Agreement is not useful; a checkable defect is. But do not invent one: an answer the texts support has an empty list of defects.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "questions": [ { "id": "<the question's id as given>", "supported": "partly", "defects": [ { "claim": "<one or two sentences: what the answer says and what the text says>", "evidenceWords": "<Arabic words copied from the texts above>" } ], "notSettledByTheseTexts": false, "toLookUp": "" } ] }`

- One entry per question, in the order given.
- "supported", one of: "yes" (the texts below establish what the answer says), "partly" (they establish some of it), "no" (they do not establish it, or they contradict it).
- "defects": one entry per defect; an empty list when you found none. "evidenceWords": the Arabic words of the texts above that show the defect, copied letter for letter; never words from memory.
- "notSettledByTheseTexts": true when, on your reading, the texts below do not decide the question (it is then a matter of practice, or of texts not given).
- "toLookUp": from your memory only, a text that is not given and that you believe matters (its name, number, year, article), in one sentence; `""` when none. It is a lead, never a fact.
