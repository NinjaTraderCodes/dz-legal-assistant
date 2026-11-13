# Third derivation of a module (Codex)

- **Model:** Codex (`codex exec`), one call per module (12 calls). Rules of [../specs/codex-calls.md](../specs/codex-calls.md).
- **Why Codex here:** the 12 modules feed all 21 records, so an error in one is an error in many. A third derivation by another model family is more independent than a second run of a Claude model.
- **Placeholders:**
  - `{{module_id}}`; `{{module_question}}` (the module's row of the handbook's table: what it must answer, and where to start).
  - `{{texts}}`: the module's `rules/reports/<unit>.texts.json`, fixed and committed by rule before any derivation of the module ran ([../specs/record-checks.md](../specs/record-checks.md), "The texts given to Codex and to the boundary writer"), rendered `[<code> <article> ¶<n>] <text>`, every paragraph on its own line. Never a selection made after reading A or B.
  - `{{slots}}`: the slot list as derivers get it (id, type, question in English).
- **Output schema:** `schemas/module-derivation-c.schema.json`. Every closed value is an enum, so that a script can compare it:
  - `statements`: `[{ "code", "article", "paragraphs": [int], "says", "role": "basis" | "proof" | "damages" | "procedure" | "limit" | "prerequisite", "interpretive": bool }]`
  - `deadlines`: `[{ "kind": "forfeiture" | "limitation" | "procedural", "periodValue": int, "periodUnit": "day" | "month" | "year", "startsFromEvent", "countingWords", "raisedBy": "courtOfOwnMotion" | "pleaOnly" | "unclear", "sanctionWords", "periodWords", "basis": [{ "code", "article", "paragraphs" }], "interpretive": bool }]`
  - `priorSteps`: `[{ "kind": "formalNotice" | "labourConciliation" | "commercialConciliation" | "landRegistryPublication" | "noticeToQuit" | "reconciliationAttempt" | "mediation" | "other", "requiredWhen", "notRequiredWhen": [string], "sanctionIfMissing", "basis": [...], "interpretive": bool }]`
  - `sections`: `[{ "matter", "section": "civil" | "realEstate" | "family" | "social" | "commercial" | "other", "basis": [...] }]`; `territory`: `[{ "rule", "when", "basis": [...] }]`; `exits`: `[{ "when", "goesTo": "administrativeCourt" | "specialisedCommercialCourt" | "paymentOrder" | "criminalCourt" | "otherSection" | "outsideCatalogue", "basis": [...] }]`
  - `openPoints`: `[{ "question", "readings": [string] }]`; `missingTexts`: `[{ "code", "article", "why" }]`
- **What happens to the answer:** it is **kept as returned** in `rules/derivations/c/<unit>.json` and never turned into a record (nobody maps it by hand). `compare-c` compares its mechanical fields with A and B: the cited (code, article, paragraphs); each deadline's kind, period value and unit, period and sanction words, `raisedBy`; each prior step's kind; exits' destinations; sections. Its free-text fields are listed for the reconciler as "C says". C never counts in grade condition 4 (always A against B); a mechanical difference between C and a value on which A and B agreed becomes an attack in the unit's register, decided by the verifier.
- Codex cannot open the store or the schema, which is why its answer is flatter than a record.

--- PROMPT ---

You are deriving one shared rule module for an Algerian civil-procedure assistant, from official Algerian legal texts given below. No lawyer will check the result; two other derivations exist and a script will compare yours with them. Use only the texts below. Do not rely on memory for any rule, number or period: if the texts below do not settle a point, say so in "openPoints" or "missingTexts".

**The module:** `{{module_id}}`

**What it must answer:** {{module_question}}

**The facts a rule may test (slots):**

{{slots}}

**The official texts** (Arabic; each paragraph is numbered; cite by code, article and paragraph numbers exactly as shown):

{{texts}}

**What to produce** (JSON matching the schema you were given):

- "statements": every paragraph that carries a rule of this module. "says": one short English sentence of what that paragraph establishes. "role": basis, proof, damages, procedure, limit or prerequisite. Cite the narrowest paragraphs. When a paragraph states an exception to another, cite it as its own statement.
- "deadlines": every time limit. "kind": forfeiture, limitation or procedural. "periodValue" and "periodUnit": the number and the unit (day, month or year). "periodWords" and "sanctionWords": the exact Arabic words of the text, copied letter for letter. "countingWords": the exact Arabic words that say how the period is counted (whether the first or last day counts, what happens on a non-working day), or an empty string if the texts above do not say. "startsFromEvent": the event the period runs from, in English, as the text states it. "raisedBy": courtOfOwnMotion, pleaOnly or unclear; write unclear unless a text below says it.
- "priorSteps": every step required before filing, its kind, when it is required, each exemption, and the sanction if it is missing ("none stated" when the text states the duty and no sanction).
- "sections", "territory", "exits": which section hears what; which court by place; which cases leave the ordinary courts and where they go.
- Set "interpretive": true wherever your answer is a reading and not the words themselves, and give every reasonable reading in "openPoints".
- "missingTexts": every article the texts above refer to, or that you would need, and that is not given.

Read every paragraph before and after the one you rely on. The exception is usually next to the rule.

Do not run any command and do not open any file.
