# Checklist before, for a claim type (Codex)

- **Model:** Codex, one call per claim type (21 calls), **before** the derivations of that type are reconciled. It sees nothing but the claim's name and description.
- **Placeholders:** `{{claim_label_ar}}`, `{{claim_label_en}}`, `{{claim_description_ar}}` (from `data/law/claims/<area>.json`; never the catalogue's articles, prerequisites, documents or requests).
- **Output schema:** `schemas/checklist.schema.json`: `{ "items": [{ "item": string, "kind": "basis" | "element" | "proof" | "court" | "deadline" | "priorStep" | "specialLaw" | "exception" | "boundary" | "document" | "amendment" | "other" }] }`.
- **Output file:** `docs/research/core-logic/rules/derivations/codex/<unit>.checklist.json` (the answer as returned). The derivers never see it.
- **Why:** a list written by another family, from memory, with no text in view. `merge-checklists` merges it with the two derivers' lists, the November unit's questions and the catalogue's questions into `rules/reports/<unit>.checklist.json` (lines `from: codex`); the final record must answer every line (`applies`, `doesNotApply` with a reason, `searchedNotFound`). What Codex "remembers" is **never** used as law: it is only a question that must be answered from the texts.

--- PROMPT ---

You are helping to build a checklist for Algerian lawyers. Do not state the law; list what must be checked.

**The kind of claim** (before an Algerian court of first instance):

- Arabic name: {{claim_label_ar}}
- English name: {{claim_label_en}}
- Description: {{claim_description_ar}}

List every point a careful Algerian lawyer must check before filing a statement of claim (عريضة افتتاح دعوى) of this kind. Think of: the texts the claim rests on; each fact that must be proved, and by whom; the means of proof the law requires or excludes; the competent court, section and place, and the cases where the matter belongs to another court (administrative, specialised commercial, criminal) or another procedure; every time limit (limitation, forfeiture, procedural), what starts it and what stops it; every step that must come before filing (notice, conciliation, publication, prior complaint); special laws that displace the general one; amendments in recent years; the neighbouring kinds of claim that are easily confused with this one; the documents to attach.

Write each point as a question or a short instruction, in English, one idea per item. Name an article or a law only as a pointer ("check Civil Code art. 119 on formal notice"), and say "if it exists" when you are not sure it does. Between 20 and 50 items. Do not answer the questions.

Do not run any command and do not open any file.
