# Attack on the finished record (Codex)

- **Model:** Codex, one call per unit (33 calls; a long record is split into two calls by the script: deadlines, prior steps and court in one, the rest in the other).
- **Placeholders:** `{{unit_label}}`, `{{record_text}}` (the quoted copy of the reconciled record, `rules/reconciled/quoted/<unit>.json`, rendered as plain text by a script: for each item its id, its English `says` or rule, its structured values (a deadline's counting included), and under it the quoted Arabic paragraphs with their code, article and paragraph numbers; plus the neighbouring paragraphs that are **not** cited, marked "not cited"), `{{slots}}` (id, type, English question).
- **Output schema:** `schemas/attacks.schema.json`: `{ "attacks": [{ "target": string, "kind": "misreading" | "exception" | "missing" | "amendmentSuspected" | "specialLawSuspected" | "wrongEffect" | "wrongCondition", "claim": string, "evidenceWords": string, "fromMemory": boolean }], "nothingFound": boolean }`.
- **Output file:** the script stores the answer as `rules/reports/<unit>.attacks.codex.json`, giving each attack an id (`cdx-1` …).
- **Why Codex also attacks:** another family reads the same Arabic differently. It has no web access here: its strength is close reading of the quoted text against the item, and remembering things that the verifier must then check. `fromMemory: true` marks the second kind, which is a lead only.

--- PROMPT ---

Below is a set of legal rules for one kind of claim before Algerian courts, each with the official Arabic text it rests on. No lawyer will check these rules. Find what is wrong with them. Agreement is not useful; a specific, checkable defect is.

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
6. **From your memory only:** an amendment or a special law you believe changes one of these texts. Set "fromMemory" to true, and say as exactly as you can what to look for (the law's number and year, the article). Do not present memory as fact.

For every attack give "target" (the item's id), "kind", "claim" (one or two sentences), and "evidenceWords" (Arabic words copied from the texts above; empty only for attacks from memory). Up to 15 attacks, the most serious first. If you find nothing after reading every item against its text, set "nothingFound" to true and return an empty list; do not invent defects.

Do not run any command and do not open any file.
