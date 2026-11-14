# Attack on the finished record

- **Model:** Sonnet 5.5, a fresh agent with **no tool**, one run per unit (33 runs; a long record is split into two runs by the script: deadlines, prior steps and court in one, the rest in the other). The record is Opus's (the reconciler's), so the attacker is the other Claude model. (Codex made this attack for the first module; the owner's decision of 14 November 2026 moved it to Sonnet. Codex still reads every statement against its text in the support check.)
- **One family:** attacker, derivers and verifier are all Claude models. Say so wherever attack counts are reported (HANDBOOK section 5b).
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against the schema. A run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:** `{{unit_label}}`, `{{record_text}}` (the quoted copy of the reconciled record, `rules/reconciled/quoted/<unit>.json`, rendered as plain text by `stage7.py render`: for each item its id, its English `says` or rule, its structured values (a deadline's counting included), and under it the quoted Arabic paragraphs with their code, article and paragraph numbers; plus the neighbouring paragraphs that are **not** cited, marked "not cited"), `{{slots}}` (id, type, English question).
- **Output schema:** `schemas/attacks.schema.json`: `{ "attacks": [{ "target": string, "kind": "misreading" | "exception" | "missing" | "amendmentSuspected" | "specialLawSuspected" | "wrongEffect" | "wrongCondition", "claim": string, "evidenceWords": string, "fromMemory": boolean }], "nothingFound": boolean }`.
- **Output file:** `records.py ingest-reply` stores the answer as `rules/reports/<unit>.attacks.sonnet.json`, giving each attack an id (`atk-1` …). (The first module's file is `formal-notice.attacks.codex.json`, with ids `cdx-…`.)
- **What it adds:** it has no web access and no store: its strength is close reading of the quoted text against the item, and remembering things that the verifier must then check. `fromMemory: true` marks the second kind, which is a lead only.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

Below is a set of legal rules for one kind of claim before Algerian courts, each with the official Arabic text it rests on. No lawyer will check these rules. Find what is wrong with them. Agreement is not useful; a specific, checkable defect is.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in any repository, not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

**The unit:** {{unit_label}}

**The facts a rule may test (slots):**

{{slots}}

**The rules and their texts** (paragraphs marked "not cited" are next to cited ones and were left out by the authors):

{{record_text}}

Look for:

1. **Misreading.** An item says something its quoted paragraphs do not say: a period, a starting event, a sanction, a condition, a "must" for a "may", "and" for "or", who must act. Copy the Arabic words that show it into "evidenceWords".
2. **A dropped exception.** A paragraph marked "not cited" changes the rule for some cases. Copy its words.
3. **A wrong effect.** An item blocks or warns when its own text supports less, or stays silent when its text supports a warning.
4. **A wrong condition.** A rule fires, or fails to fire, on a case its text decides the other way. Describe the case with the slots.
5. **Something missing.** A situation that is common for this kind of claim and that these rules do not handle. Describe the case.
6. **"Done" beside an open question.** A step required before filing that the rules treat as done while another item still asks or warns about the same step (its means, its date).
7. **From your memory only:** an amendment or a special law you believe changes one of these texts. Set "fromMemory" to true, and say as exactly as you can what to look for (the law's number and year, the article). Do not present memory as fact.

Up to 15 attacks, the most serious first. If you find nothing after reading every item against its text, do not invent defects.

## Your answer

Return only one JSON object, with no text before or after it:

`{ "attacks": [ { "target": "<the id of the item you attack>", "kind": "misreading", "claim": "<one or two sentences>", "evidenceWords": "<Arabic words copied from the texts above>", "fromMemory": false } ], "nothingFound": false }`

- "target": the item's id exactly as printed above. One item per attack.
- "kind", one of:
  - "misreading": the item says what its quoted text does not say (point 1);
  - "exception": a paragraph that is not cited changes the rule (point 2);
  - "wrongEffect": the item blocks, warns or stays silent against its own text, or says "done" beside an open question (points 3 and 6);
  - "wrongCondition": the rule fires or fails to fire on a case its text decides the other way (point 4);
  - "missing": a common situation the rules do not handle (point 5);
  - "amendmentSuspected": from memory, a later text changed a cited article (point 7);
  - "specialLawSuspected": from memory, another law governs some of these cases (point 7).
- "claim": one or two sentences that a reader can check against the text.
- "evidenceWords": Arabic words copied letter for letter from the texts above; `""` only when "fromMemory" is true.
- "fromMemory": true only for the two "…Suspected" kinds.
- "nothingFound": true, with `"attacks": []`, when you read every item against its text and found no defect; otherwise false.
