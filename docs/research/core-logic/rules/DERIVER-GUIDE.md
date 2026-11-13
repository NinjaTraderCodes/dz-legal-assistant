# Guide for a deriver: how to write a rule record

This guide is for the agents that derive a rule record (derivations A and B) and for the boundary-case writer. It says **how** a record is written, never **what** the law says: it holds no Algerian rule, no article number and no period. Its one worked example is invented, from a domain that has nothing to do with law (a library's lending rules), so that it cannot anchor you.

You are not given the grade rules, the help texts of the slot list, the evaluator's specification or the example records, because some of them state things you are supposed to find in the texts yourself. Everything you need to write a valid record is here and in the schema `rules/rule-record.schema.json`. When the two seem to differ, the schema is right; say so in your reply.

## 1. What a record is

One record per unit. A unit is either a **claim type** (`kind: "claim"`, one per claim of the app's catalogue) or a **shared module** (`kind: "module"`, a block of rules that several claim types use, written once). A claim record **imports** the modules that apply to it and never restates their rules.

A record is a JSON object. You write **pointers and structured values**. A script writes the words of the law (`quote`), where they come from (`source`) and how sure each item is (`grading`). You never type those three fields: a record that has them is refused.

## 2. The top-level fields

| Field | What you write |
|---|---|
| `schemaVersion` | `"1.0"` |
| `kind` | `"claim"` or `"module"` |
| `id` | the unit id you were given |
| `label` | `{ "ar": "...", "en": "..." }`: the unit's name |
| `verified` | always `false`: no lawyer verified anything |
| `stage` | `"derivationA"` or `"derivationB"`, as your prompt says |
| `derivedBy` | `{ "role": "derivationA", "model": "<your model>", "date": "yyyy-mm-dd" }` |
| `area` | claims only: `civil`, `commercial`, `family` or `social`, as your brief says |
| `appliesTo` | modules only: the claim ids the module applies to, or `["*"]` for all |
| `imports` | claims only: the module blocks that apply (section 9) |
| `checklist` | your phase-1 lines, each answered (section 8) |
| `basis` | the statements: one per text you rely on (section 3) |
| `elements`, `requests` | what must be proved, and what is usually asked (section 5) |
| `court` | `section`, `territory`, `exits` (section 5) |
| `deadlines`, `priorSteps`, `documents`, `boundaries` | section 5 |
| `slots` | every slot id any condition, deadline, prior step, element, boundary or example of this record reads, plus `requests` when a condition reads the requests |
| `proposedSlots` | the slots you need that the slot list does not have (section 7) |
| `examples` | your worked examples (section 10) |
| `openPoints` | what the texts do not settle (section 6) |

Leave out `history`. Ids are unique inside the record. Any short ids will do; the usual ones are `b1`, `b2` … for statements, `e` for elements, `r` for requests, `d` for deadlines, `p` for prior steps, `x` for exits, `t` for territory rules, `doc` for documents, `n` for boundaries, `ex` for examples, `o` for open points, `k` for checklist lines.

Every text field that the lawyer may read is `{ "ar": "...", "en": "..." }`: formal Arabic and plain English, saying the same thing.

## 3. Statements: you point, a script quotes

A statement says "this paragraph of this article establishes this".

```json
{ "id": "b1", "role": "limit",
  "cite": { "code": "<code id>", "article": "<number>", "paragraphs": [2] },
  "says": { "ar": "...", "en": "..." } }
```

- `cite.code`: a code id exactly as the article store writes it. `cite.article`: the article number exactly as the store writes it (the store's canonical form, including any word that follows the number). `cite.paragraphs`: paragraph numbers counted from 1, as the store lists the article's lines; a numbered item of a list is a paragraph; empty or absent means the whole article. `cite.version`: only when you mean a version that is not the one in force (the store's publication id of that version).
- **Cite the narrowest paragraphs that carry the rule.** When a paragraph states an exception to another, cite it as **its own** statement. Whole paragraphs only: there is no way to cite half of one.
- `role`: `basis` (grounds the claim), `proof` (who proves what, and how), `damages`, `procedure`, `limit` (a time limit, an exclusion, a boundary), `prerequisite` (a step before filing).
- `says`: one short sentence, in your own words, of what that paragraph establishes. **Do not put article or paragraph numbers in `says`**, and put a number (a period, an amount) in it only when the paragraph itself has it: a script checks every number in `says` against the quoted words.
- `required: true` when the article is always proposed for this claim type.
- `interpretive: true` when what you make the paragraph say is a reading and not its words; then write why in `interpretiveNote`.

**Only the article store is a source.** If a text you need is not in it, do not cite it from memory: add a checklist line with `"answer": "searchedNotFound"` saying what you searched, write a request line (your prompt names the file), and put what you believe from memory in an open point of grade D, never in a rule.

## 4. Conditions: a small language with three values

Rules fire on **slots**: the facts of the case the app knows or asks for (section 7). A condition is a JSON object with exactly one of `slot`, `all`, `any`, `not`.

| Form | True when | Unknown when |
|---|---|---|
| `{ "slot": s, "is": v }` | the slot's value is `v` | the slot is unknown |
| `{ "slot": s, "in": [v1, v2] }` | the value is one of the list | the slot is unknown |
| `{ "slot": "requests", "includes": k }` | the petition asks for request kind `k` | never (an empty list is known) |
| `{ "slot": s, "before": "yyyy-mm-dd" }` | the slot's date is strictly before that date | the slot is unknown |
| `{ "slot": s, "after": "yyyy-mm-dd" }` | the slot's date is on or after that date | the slot is unknown |
| `{ "slot": s, "known": true }` | the slot has a value other than `unknown` (`known: false`: the reverse) | never |
| `{ "all": [c1, c2] }` | every part is true | no part is false and at least one is unknown |
| `{ "any": [c1, c2] }` | at least one part is true | no part is true and at least one is unknown |
| `{ "not": c }` | `c` is false | `c` is unknown |

A slot that is absent from the case, or set to `unknown`, is **unknown, never false**. An unknown condition makes the item **ask** the lawyer for the slots that could still change the result. That is the most common error to avoid: a missing fact is not a "no".

There is no comparison of amounts. A rule that turns on a sum is written as a statement (and, where it matters, an element's `proof` note), not as a condition.

## 5. The items, and the effects each may declare

### The four effects

| Effect | What the app does |
|---|---|
| `blockCleanExport` | The clean export of the petition is blocked; the export with the «مسودة» (draft) mark stays allowed. Nothing harder than this exists. |
| `warn` | A warning the lawyer must see. |
| `ask` | A question to the lawyer, for the slots that are unknown. |
| `none` | Nothing is shown (a document is still listed). |

What you write is the **declared** effect: what the texts call for. A script later grades every item from facts it can check (is the text official and verified, did two derivations agree, did it survive attack, was the Official Journal searched for amendments). The grade can lower what the app shows (a declared block may be shown as a warning), never raise it. You never write a grade, except the grade of an open point.

### Which item may declare which effect

| Item | May declare |
|---|---|
| Deadline | `effectWhenExpired`: `blockCleanExport` only for a `forfeiture` that is not interpretive; otherwise `warn`. A limitation always declares `warn`. |
| Prior step | `effectWhenMissing`: `blockCleanExport` or `warn` (`warn` when interpretive, for example when the text states the duty and no sanction). `effectWhenUnknown`: `ask` or `warn`. |
| Exit | `effect`: always `warn`. |
| Section rule | none of its own: a warning when the lawyer's section differs from the one proposed. |
| Boundary | none of its own: a warning when its `when` is true. |
| Document | none of its own: the app lists it. |
| Territory rule, element, request, statement | no effect: data the app shows or uses. |

**Where the law is unclear, choose the reading that protects the lawyer**: warn rather than stay silent; ask rather than guess; never block on a reading.

### Deadline

```json
{ "id": "d1", "kind": "forfeiture" | "limitation" | "procedural",
  "period": { "value": 21, "unit": "day" | "month" | "year" },
  "startsFrom": { "slot": "<date slot>", "event": "<event kind, if one>", "description": { "ar": "...", "en": "..." } },
  "counting": { "method": "excludeFirstAndLastDay" | "excludeFirstDay" | "inclusive", "extendToWorkingDay": true, "basis": ["b3"] },
  "raisedBy": "courtOfOwnMotion" | "pleaOnly" | "unclear",
  "when": <condition, optional>,
  "effectWhenExpired": "blockCleanExport" | "warn",
  "warnBandDays": 15,
  "message": { "ar": "...", "en": "..." },
  "basis": ["b1", "b3"],
  "extract": { "period": "<the exact words of the period>", "sanction": "<the exact words of the sanction>" },
  "interpretive": false, "interpretiveNote": "...", "readings": [ ... ] }
```

- `extract`: the words of the period and of the sanction, **copied letter for letter** from the text of a statement in `basis`. A script checks that they are there and that it reads from them the same number and unit as `period`.
- `counting` is **derived from the texts like everything else**: how the period is counted. `excludeFirstAndLastDay`: neither the starting day nor the day the period ends counts, so the act may still be done on the following day. `excludeFirstDay`: the starting day does not count and the period is complete at the end of its last day. `inclusive`: the starting day counts. `extendToWorkingDay`: a period that ends on a non-working day runs to the next working day. `basis`: the statements that say so. **If the texts do not settle the counting, leave `counting` out and set the deadline `interpretive`**, with your readings.
- `raisedBy`: write `unclear` unless a text says who raises it.
- `warnBandDays`: inside this many days of the last day, before or after, the rule warns instead of deciding (the evaluator uses 15 when it is absent).

**How the app evaluates a deadline** (you need this to write examples): if `when` is false, `none`; if it is unknown, `ask`. If the `startsFrom` slot is unknown, `ask`. Otherwise it computes the last day on which the act is in time: the start date plus the period (for months and years, the same day number in the last month, or that month's last day when it is shorter), plus one day for `excludeFirstAndLastDay`, minus one day for `inclusive`, then moved to the next working day when `extendToWorkingDay` is true. `DaysLeft` = last day − the petition's date. If `|DaysLeft| ≤ warnBandDays`: `warn`. Else if `DaysLeft < 0`: `effectWhenExpired`. Else `none`.

### Prior step

```json
{ "id": "p1", "kind": "formalNotice" | "labourConciliation" | "commercialConciliation" | "landRegistryPublication" | "noticeToQuit" | "reconciliationAttempt" | "mediation" | "other",
  "slots": ["<was the step done?>", "<slots its conditions read>"],
  "requiredWhen": <condition, optional>,
  "notRequiredWhen": [ { "when": <condition>, "basis": ["b2"], "note": { "ar": "...", "en": "..." } } ],
  "acceptedMeans": [ { "id": "m1", "label": { "ar": "...", "en": "..." }, "basis": ["b4"] } ],
  "effectWhenMissing": "blockCleanExport" | "warn",
  "effectWhenUnknown": "ask" | "warn",
  "message": { ... }, "basis": [ ... ], "interpretive": false }
```

The **first** slot of `slots` is the step's own (was it done?). Evaluation: if `requiredWhen` is false, `none`; unknown, `ask`. If the step was done (`yes`), `none`. If it is unknown, `effectWhenUnknown` (and the step's slot is asked); the exemptions are not asked before the lawyer says the step was not done. If it was not done: any exemption true gives `none`; none true and one unknown gives `ask`; all false gives `effectWhenMissing`.

### Court: section, territory, exits

- `court.section`: `{ "value": "civil" | "realEstate" | "family" | "social" | "commercial" | "other", "basis": [...], "alternatives": [ { "when": <condition>, "value": ..., "basis": [...] } ], "message": {...} }`. The proposal is `value`, or the value of the first alternative whose `when` is true.
- `court.territory`: `[ { "id": "t1", "rule": {...}, "when": <optional>, "basis": [...] } ]`. Shown to the lawyer; no effect.
- `court.exits`: the cases that leave the ordinary courts. `{ "id": "x1", "when": <condition>, "goesTo": "administrativeCourt" | "specialisedCommercialCourt" | "paymentOrder" | "criminalCourt" | "otherSection" | "outsideCatalogue", "effect": "warn", "message": {...}, "basis": [...] }`. `when` true: `warn`; false: `none`; unknown: `ask`.

### Elements and requests (claim records)

- `elements`: what the plaintiff must prove. `{ "id", "toProve": {...}, "factsToState": [{...}], "events": [<event kinds>], "slots": [...], "burden": "plaintiff" | "defendant", "proof": [ { "means": "...", "basis": [...], "note": {...} } ], "basis": [...] }`. `events` uses the event kinds your prompt lists (`{{vocabulary}}`); every element of your brief must be carried by at least one element's `events`.
- `requests`: `{ "id", "kind": <request kind from your prompt's list>, "accessory": true | false, "text": { "ar": "<the usual request line, with (...) where the lawyer fills a value>", "en": "..." }, "amount": true | false, "when": <optional>, "basis": [...] }`. Every request kind of your brief must appear, or a checklist line says why not.

### Documents and boundaries

- `documents`: `{ "id", "name": {...}, "requiredWhen": <optional>, "why": {...}, "basis": [...] }`. Listed by the app unless `requiredWhen` is false.
- `boundaries`: what looks like this claim and is not. `{ "id", "looksLike": {...}, "isActually": "claim:<id>" | "outside:<short kind>", "tellingFacts": [{...}], "when": <optional>, "slots": [...], "message": {...}, "basis": [...] }`. With a `when`: true `warn`, false `none`, unknown `ask`. Without one, it is a note for the reader of lawyers' notes and produces nothing.

### Messages

Every `message` is short, in formal Arabic and in English, and names the article. It states the rule and what to check; **it never tells the lawyer what to conclude**. An article number in a message must be the article of a statement in the item's `basis`, and a period in it must equal the item's `period`: a script checks both.

## 6. Unclear law: interpretive items, readings, open points

- An item is `interpretive` when it rests on a reading: which article governs, whether a period is a limitation or a forfeiture, the starting event, the sanction of a duty the text states without one, anything that needs case law. An interpretive item never declares `blockCleanExport`.
- Put every reasonable reading in `readings`: `[ { "reading": {...}, "basis": [...] } ]`, and make the rule the protective one.
- Add an `openPoints` entry: `{ "id", "question": {...}, "why": {...}, "readings": [...], "protectiveReading": {...}, "grade": "C" | "D", "refs": [<item ids>] }`. Grade `C` when the texts are in the store and do not settle the point; `D` when it rests on a text that is not in the store or on memory.

## 7. Slots and proposed slots

Your prompt gives the slot list (`{{slots}}`): each slot's id, type and question. Types: `yesNoUnknown` (values `yes`, `no`, `unknown`), `date` (`yyyy-mm-dd`), `amount` (digits), `choice` (one of the slot's choice ids). `requests` is a reserved list slot: the request kinds the petition asks for.

**If you need a fact the list does not have**, declare it in `proposedSlots` (never anywhere else) and use its id like any other slot:

```json
{ "id": "<snake_case id>", "type": "yesNoUnknown" | "date" | "amount" | "choice",
  "question": { "ar": "...", "en": "..." },
  "choices": [ { "id": "...", "label": { "ar": "...", "en": "..." } } ],
  "why": "<which item reads it, and which text makes the fact matter>" }
```

Do not propose a slot for a fact an existing slot already holds. Another deriver will name its own slots differently; a person after you maps them, so choose a plain, descriptive id and say clearly what it holds.

## 8. The checklist

In phase 1 you list, from memory and before reading any text, what a careful lawyer must check for this unit. In the record, every line of that list becomes a checklist item, and **every one is answered**:

```json
{ "id": "k1", "item": "<the line as you wrote it>", "from": ["derivationA"],
  "answer": "applies" | "doesNotApply" | "searchedNotFound",
  "refs": ["<ids of the items that answer it>"], "note": "<the reason, or what was searched>" }
```

`applies` names the items that answer the line; `doesNotApply` says why; `searchedNotFound` says what was searched. `from` is your own role. If the article store carries a note on an article you cite (for example a note on the date from which a wording applies), add a checklist line that asks what the note raises, with your own role in `from`, and answer it like the others.

## 9. Imports (claim records)

```json
{ "module": "<module id>", "blocks": ["<item ids of the module>"], "how": { "ar": "...", "en": "..." } }
```

- `blocks` may name a module's deadlines, prior steps, exits, territory rules, documents, boundaries, statements and requests, and the word `section` (the module's whole section rule).
- Take the ids from the module's final record, which your prompt lets you read. A script copies the blocks into the final claim record, with ids prefixed `<module id>.`; the import line stays as the trace.
- **Do not restate a module's rule in the claim record.** If you believe the module is wrong or incomplete for this claim, say so in your reply and in an open point; do not write your own version.
- Some modules are mostly statements and requests (for example the ones on the court's sections, on proof, on the petition's formal elements, on accessory requests). Import their statements and requests by id. The claim record then writes **its own** `court.section`, whose `basis` names the imported statements as `<module id>.<statement id>`. A claim that imports the word `section` may not also write its own.

## 10. Worked examples

A worked example is a case and what the record must do with it.

```json
{ "id": "ex1", "by": "derivationA", "title": "<one line>", "petitionDate": "yyyy-mm-dd",
  "slots": { "<slot>": "<value>", "requests": ["<request kind>"] },
  "expect": { "section": "<optional>",
              "findings": [ { "ref": "<item id>", "effect": "<the DECLARED effect>" } ],
              "questions": ["<slot ids to ask for>"] } }
```

- `effect` is the **declared** effect: what your record calls for in this case, following the evaluation rules of section 5, before any grade is applied. Do not write `shownEffect` or `toCheck`: a script writes them later.
- `findings` is **partial**: each finding you list is checked; an item you do not list is not checked. List every item the case is about.
- `questions` is compared **as a set** (order does not matter). Leave the field out and the questions are not checked; write `[]` and nothing may be asked.
- Write at least six: a clean case; each deadline far inside, far outside and within a few days of its last day; each prior step done, not done and unknown; each exemption; each exit.

A tool checks your record and runs your examples through the evaluator; your prompt says how to run it. Fix what it reports; do not change an expectation to make a wrong rule pass.

## 11. A made-up example: a library's lending rules

Everything below is invented. The "code" `toy-library` does not exist, and the store will not find it: the example shows the shape of a record, not a rule of law. Its texts are in English to keep it readable; real texts are Arabic, and `extract` copies Arabic words.

The invented rules, article 4 of the "library rules":
- ¶1 "A book lent to a reader shall be returned within twenty-one days, or the reader loses the right to borrow for the year."
- ¶2 "However, a member of staff may keep a book for as long as the work requires."
- Article 5 ¶1: "The day of the loan is not counted. A period ending on a day the library is closed runs to the next opening day."
- Article 2 ¶1: "No book is lent to a reader whose card has not been renewed for the year."
- Article 9 ¶1: "Requests for the rare-book collection are made at the archive desk."

```json
{
  "schemaVersion": "1.0", "kind": "module", "id": "toy-lending", "verified": false, "stage": "derivationA",
  "label": { "ar": "الإعارة", "en": "Lending" },
  "derivedBy": { "role": "derivationA", "model": "<model>", "date": "2026-01-01" },
  "appliesTo": ["*"],
  "slots": ["loan_date", "reader_is_staff", "card_renewed", "rare_book"],
  "proposedSlots": [
    { "id": "rare_book", "type": "yesNoUnknown",
      "question": { "ar": "هل الكتاب من المجموعة النادرة؟", "en": "Is the book from the rare-book collection?" },
      "why": "Exit x1 reads it; article 9 sends these requests to another desk." } ],
  "basis": [
    { "id": "b1", "role": "limit", "cite": { "code": "toy-library", "article": "4", "paragraphs": [1] },
      "says": { "ar": "يعاد الكتاب في أجل واحد وعشرين يوما وإلا فقد القارئ حق الإعارة.", "en": "A book is returned within twenty-one days, or the reader loses the right to borrow." } },
    { "id": "b2", "role": "limit", "cite": { "code": "toy-library", "article": "4", "paragraphs": [2] },
      "says": { "ar": "يستثنى الموظف من الأجل.", "en": "A member of staff is exempt from the period." } },
    { "id": "b3", "role": "procedure", "cite": { "code": "toy-library", "article": "5", "paragraphs": [1] },
      "says": { "ar": "لا يحسب يوم الإعارة، ويمتد الأجل إلى يوم الفتح الموالي.", "en": "The day of the loan is not counted; a period ending on a closed day runs to the next opening day." } },
    { "id": "b4", "role": "prerequisite", "cite": { "code": "toy-library", "article": "2", "paragraphs": [1] },
      "says": { "ar": "لا إعارة دون تجديد البطاقة.", "en": "No loan without a renewed card." } },
    { "id": "b5", "role": "procedure", "cite": { "code": "toy-library", "article": "9", "paragraphs": [1] },
      "says": { "ar": "طلبات المجموعة النادرة تقدم في مكتب الأرشيف.", "en": "Rare-book requests go to the archive desk." } } ],
  "deadlines": [
    { "id": "d1", "kind": "forfeiture", "period": { "value": 21, "unit": "day" },
      "startsFrom": { "slot": "loan_date", "description": { "ar": "يوم الإعارة", "en": "The day of the loan" } },
      "counting": { "method": "excludeFirstDay", "extendToWorkingDay": true, "basis": ["b3"] },
      "raisedBy": "unclear",
      "when": { "slot": "reader_is_staff", "is": "no" },
      "effectWhenExpired": "blockCleanExport", "warnBandDays": 3,
      "message": { "ar": "...", "en": "Article 4: the book must be returned within twenty-one days of the loan. Check the date of the loan." },
      "basis": ["b1", "b2", "b3"],
      "extract": { "period": "twenty-one days", "sanction": "loses the right to borrow" } } ],
  "priorSteps": [
    { "id": "p1", "kind": "other", "slots": ["card_renewed"],
      "effectWhenMissing": "warn", "effectWhenUnknown": "ask",
      "interpretive": true, "interpretiveNote": "Article 2 forbids the loan; it says nothing of a loan made anyway.",
      "message": { "ar": "...", "en": "Article 2: no book is lent without a renewed card. Check the card." },
      "basis": ["b4"] } ],
  "court": { "exits": [
    { "id": "x1", "when": { "slot": "rare_book", "is": "yes" }, "goesTo": "otherSection", "effect": "warn",
      "message": { "ar": "...", "en": "Article 9: rare-book requests are made at the archive desk." }, "basis": ["b5"] } ] },
  "openPoints": [
    { "id": "o1", "question": { "ar": "...", "en": "What happens to a loan made without a renewed card?" },
      "why": { "ar": "...", "en": "Article 2 states the rule and no consequence." },
      "protectiveReading": { "ar": "...", "en": "Warn; do not block." }, "grade": "C", "refs": ["p1"] } ],
  "checklist": [
    { "id": "k1", "item": "How long a book may be kept, and what happens when it is late.", "from": ["derivationA"], "answer": "applies", "refs": ["d1", "b1"] },
    { "id": "k2", "item": "Readers exempt from the period.", "from": ["derivationA"], "answer": "applies", "refs": ["b2", "d1"] },
    { "id": "k3", "item": "Fines for a late book.", "from": ["derivationA"], "answer": "searchedNotFound", "note": "No article on fines in the rules given; searched articles 1 to 12." } ],
  "examples": [
    { "id": "ex1", "by": "derivationA", "title": "Returned in time.", "petitionDate": "2026-01-20",
      "slots": { "loan_date": "2026-01-05", "reader_is_staff": "no", "card_renewed": "yes", "rare_book": "no" },
      "expect": { "findings": [ { "ref": "d1", "effect": "none" }, { "ref": "p1", "effect": "none" }, { "ref": "x1", "effect": "none" } ], "questions": [] } },
    { "id": "ex2", "by": "derivationA", "title": "A week late, outside the band.", "petitionDate": "2026-02-02",
      "slots": { "loan_date": "2026-01-05", "reader_is_staff": "no", "card_renewed": "yes", "rare_book": "no" },
      "expect": { "findings": [ { "ref": "d1", "effect": "blockCleanExport" } ] } },
    { "id": "ex3", "by": "derivationA", "title": "One day late: inside the band, a warning.", "petitionDate": "2026-01-27",
      "slots": { "loan_date": "2026-01-05", "reader_is_staff": "no" },
      "expect": { "findings": [ { "ref": "d1", "effect": "warn" } ] } },
    { "id": "ex4", "by": "derivationA", "title": "Staff member, late: the exception.", "petitionDate": "2026-03-02",
      "slots": { "loan_date": "2026-01-05", "reader_is_staff": "yes" },
      "expect": { "findings": [ { "ref": "d1", "effect": "none" } ] } },
    { "id": "ex5", "by": "derivationA", "title": "Nothing known: the rules ask.", "petitionDate": "2026-01-20",
      "slots": {},
      "expect": { "findings": [ { "ref": "d1", "effect": "ask" }, { "ref": "p1", "effect": "ask" }, { "ref": "x1", "effect": "ask" } ],
                  "questions": ["reader_is_staff", "card_renewed", "rare_book"] } },
    { "id": "ex6", "by": "derivationA", "title": "Card not renewed: a warning, never a block.", "petitionDate": "2026-01-20",
      "slots": { "card_renewed": "no" },
      "expect": { "findings": [ { "ref": "p1", "effect": "warn" } ] } } ]
}
```

How the dates work out: the loan is on 5 January 2026; the day of the loan is not counted, so twenty-one days end on 26 January, a day the library is open, which is the last day. On 20 January, `DaysLeft` is 6: `none`. On 2 February it is −7, outside the band of 3: the declared effect is `effectWhenExpired`. On 2 March a staff member's `when` is false (¶2, cited as its own statement `b2`), so `d1` gives `none` however late the book is. On 27 January it is −1, inside the band: `warn`. In `ex5` nothing is known, so `d1` asks for `reader_is_staff` (its `when`) and stops there; `p1` asks for its own slot; `x1` asks for `rare_book`. In `ex6`, `p1` is interpretive, so it declares `warn`, not a block. (The "messages" in Arabic are left as "..." only to keep the example short; a real record writes them.)

## 12. What you never write

- `quote`, `source`, `grading`, `history`; `shownEffect` and `toCheck` in an example.
- A grade, except an open point's.
- A rule from memory, a commentary, a website or a court decision as a source.
- A change to the slot list itself: new slots go in `proposedSlots`.
