# Third derivation of a module, by the flat route

- **Model:** Sonnet 5.5, a fresh agent with **no tool**, one run per module (12 runs). (Codex made this derivation for the first module; the owner's decision of 14 November 2026 moved it to Sonnet.)
- **Why it is still worth a run:** the 12 modules feed all 21 records, so an error in one is an error in many. C shares its model with derivation B, so it is not a second family. What makes it different is its **route**: it gets the module's texts printed in the prompt and nothing else (no store, no rules tool, no record format, no French edition, no checklist of its own), and it answers in closed values that a script compares. A mistake that A and B make because of the record format or the tool's messages, C cannot make. Say "by another route, within one model family" wherever a C result is reported (HANDBOOK section 5b).
- **No tool** except one read of its own prompt file. Its answer is its reply, copied by `records.py ingest-reply` and checked against the schema. A run with any other tool use is discarded and run again by a fresh agent.
- **Placeholders:**
  - `{{module_id}}`; `{{module_question}}` (the module's row of the handbook's table: what it must answer, and where to start).
  - `{{texts}}`: the module's `rules/reports/<unit>.texts.json`, fixed and committed by rule before any derivation of the module ran ([../specs/record-checks.md](../specs/record-checks.md), "The texts given to derivation C and to the boundary writer"), rendered `[<code> <article> ¶<n>] <text>`, every paragraph on its own line. Never a selection made after reading A or B.
  - `{{slots}}`: the slot list as derivers get it (id, type, question in English).
- **Output schema:** `schemas/module-derivation-c.schema.json`. Every closed value is an enum, so that a script can compare it:
  - `statements`: `[{ "code", "article", "paragraphs": [int], "says", "role": "basis" | "proof" | "damages" | "procedure" | "limit" | "prerequisite", "interpretive": bool }]`
  - `deadlines`: `[{ "kind": "forfeiture" | "limitation" | "procedural", "periodValue": int, "periodUnit": "day" | "month" | "year", "startsFromEvent", "countingWords", "raisedBy": "courtOfOwnMotion" | "pleaOnly" | "unclear", "sanctionWords", "periodWords", "basis": [{ "code", "article", "paragraphs" }], "interpretive": bool }]`
  - `priorSteps`: `[{ "kind": "formalNotice" | "labourConciliation" | "commercialConciliation" | "landRegistryPublication" | "noticeToQuit" | "reconciliationAttempt" | "mediation" | "other", "requiredWhen", "notRequiredWhen": [string], "sanctionIfMissing", "basis": [...], "interpretive": bool }]`
  - `sections`: `[{ "matter", "section": "civil" | "realEstate" | "family" | "social" | "commercial" | "other", "basis": [...] }]`; `territory`: `[{ "rule", "when", "basis": [...] }]`; `exits`: `[{ "when", "goesTo": "administrativeCourt" | "specialisedCommercialCourt" | "paymentOrder" | "criminalCourt" | "otherSection" | "outsideCatalogue", "basis": [...] }]`
  - `openPoints`: `[{ "question", "readings": [string] }]`; `missingTexts`: `[{ "code", "article", "why" }]`
- **What happens to the answer:** it is **kept as returned** in `rules/derivations/c/<unit>.json` and never turned into a record (nobody maps it by hand). `compare-c` compares its mechanical fields with A and B: the cited (code, article, paragraphs); each deadline's kind, period value and unit, period and sanction words, `raisedBy`; each prior step's kind; exits' destinations; sections. Its free-text fields are listed for the reconciler as "C says". C never counts in grade condition 4 (always A against B); a mechanical difference between C and a value on which A and B agreed becomes an attack in the unit's register (`who: "derivationC"`, ids `c-1` …), decided by the verifier.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

You are deriving one shared rule module for an Algerian civil-procedure assistant, from official Algerian legal texts given below. No lawyer will check the result; two other derivations exist and a script will compare yours with them. Use only the texts below. Do not rely on memory for any rule, number or period: if the texts below do not settle a point, say so in "openPoints" or "missingTexts".

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command: not in any repository, not in `C:\Users\TheShadow\dz-core-research`, not in any memory, plan or state file, and nowhere else. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

**The module:** `{{module_id}}`

**What it must answer:** {{module_question}}

**The facts a rule may test (slots):**

{{slots}}

**The official texts** (Arabic; each paragraph is numbered; cite by code, article and paragraph numbers exactly as shown):

{{texts}}

**What to produce:**

- "statements": every paragraph that carries a rule of this module. "says": one short English sentence of what that paragraph establishes. Cite the narrowest paragraphs. When a paragraph states an exception to another, cite it as its own statement.
- "deadlines": every time limit. "periodValue" and "periodUnit": the number and the unit. "periodWords" and "sanctionWords": the exact Arabic words of the text, copied letter for letter. "countingWords": the exact Arabic words that say how the period is counted (whether the first or last day counts, what happens on a non-working day), or an empty string if the texts above do not say. "startsFromEvent": the event the period runs from, in English, as the text states it. "raisedBy": write "unclear" unless a text below says who may raise it.
- "priorSteps": every step required before filing, its kind, when it is required, each exemption, and the sanction if it is missing ("none stated" when the text states the duty and no sanction).
- "sections", "territory", "exits": which section hears what; which court by place; which cases leave the ordinary courts and where they go.
- Set "interpretive": true wherever your answer is a reading and not the words themselves, and give every reasonable reading in "openPoints".
- "missingTexts": every article the texts above refer to, or that you would need, and that is not given.

Read every paragraph before and after the one you rely on. The exception is usually next to the rule.

## Your answer

Return only one JSON object, with no text before or after it. It has exactly these eight keys, each a list (an empty list when the module has nothing of that sort):

```text
{
  "statements":  [ { "code": "…", "article": "…", "paragraphs": [1], "says": "…", "role": "basis", "interpretive": false } ],
  "deadlines":   [ { "kind": "forfeiture", "periodValue": 6, "periodUnit": "month", "startsFromEvent": "…",
                     "countingWords": "…", "raisedBy": "unclear", "sanctionWords": "…", "periodWords": "…",
                     "basis": [ { "code": "…", "article": "…", "paragraphs": [1] } ], "interpretive": false } ],
  "priorSteps":  [ { "kind": "formalNotice", "requiredWhen": "…", "notRequiredWhen": ["…"], "sanctionIfMissing": "…",
                     "basis": [ { "code": "…", "article": "…", "paragraphs": [1] } ], "interpretive": false } ],
  "sections":    [ { "matter": "…", "section": "civil", "basis": [ { "code": "…", "article": "…", "paragraphs": [1] } ] } ],
  "territory":   [ { "rule": "…", "when": "…", "basis": [ { "code": "…", "article": "…", "paragraphs": [1] } ] } ],
  "exits":       [ { "when": "…", "goesTo": "administrativeCourt", "basis": [ { "code": "…", "article": "…", "paragraphs": [1] } ] } ],
  "openPoints":  [ { "question": "…", "readings": ["…", "…"] } ],
  "missingTexts": [ { "code": "…", "article": "…", "why": "…" } ]
}
```

- "code" and "article": exactly as the bracket before a paragraph shows them (for `[civil-code 119 ¶2]`: "code": "civil-code", "article": "119"). "paragraphs": the paragraph numbers as shown after ¶, as whole numbers.
- A statement's "role", one of: "basis" (the rule itself), "proof" (who must prove what, or how), "damages" (compensation and its conditions), "procedure" (how something is done or filed), "limit" (a period or a ceiling), "prerequisite" (something required beforehand).
- A deadline's "kind", one of: "forfeiture" (the right or the action is lost when the period ends), "limitation" (prescription: the claim can be met with a plea that time has run), "procedural" (a period for an act of procedure). "periodUnit", one of: "day", "month", "year". "raisedBy", one of: "courtOfOwnMotion" (a text says the court raises it itself), "pleaOnly" (a text says only a party may raise it), "unclear" (no text given here says).
- A prior step's "kind", one of: "formalNotice", "labourConciliation", "commercialConciliation", "landRegistryPublication", "noticeToQuit", "reconciliationAttempt", "mediation", "other".
- A section's "section", one of: "civil", "realEstate", "family", "social", "commercial", "other".
- An exit's "goesTo", one of: "administrativeCourt", "specialisedCommercialCourt", "paymentOrder" (a procedure that replaces a claim), "criminalCourt", "otherSection" (another section of the same court), "outsideCatalogue" (anywhere else).
- "interpretive": true or false, never left out.
