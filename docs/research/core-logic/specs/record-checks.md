# Specification: the scripts that check a rule record

**Why scripts.** An agent that derives a rule can misquote, quote a real article that says something else, drop the paragraph that carries the exception, or copy a period wrongly. A second agent often repeats the first one's error. So everything that can be checked mechanically is checked by code, and the agents are left with what code cannot do: reading.

All scripts live in `research/core-logic/` (C# for anything the evaluator shares; Python is allowed for the text scripts, run with Python 3.12, `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`, which has pytest; tests by `<that python> -m pytest research/core-logic/scripts/tests`). Each has tests on small fixtures **before** it is used on a record. Each prints a summary and writes its report to the path of the table "Where the reports go" below.

**A file an agent wrote is never rewritten by a script.** Scripts write beside it: `quote-records` writes a quoted copy in a `quoted/` folder next to the agent's file (for example `rules/derivations/a/quoted/<unit>.json`, `rules/reconciled/quoted/<unit>.json`), and every later stage reads the quoted copy. So the derivations stay exactly as the agents returned them.

| Script | Reads | Writes | Fails the record when |
|---|---|---|---|
| `rules check` | a record | nothing | it breaks the schema, the contract, a reference, the slot list, an effect rule, or (with `--store`) cites what the store does not hold |
| `quote-records` | a record, the article store | the quoted copy: `quote`, `source` in every statement | a cited article, version or paragraph does not exist, or is repealed |
| `check-values` | a quoted record | a report | a period, an article number or a sanction word in a field is not what the quoted text says |
| `flag-neighbours` | a quoted record, the store | a report | (never fails; lists what a reader must look at) |
| `merge-checklists` | the from-memory lists, the catalogue entry, the November unit | the merged checklist | (never fails) |
| `compare-derivations` | records A and B of one unit (quoted) | the difference list, the id map | (never fails; its list is the reconciler's agenda) |
| `compare-c` | Codex's derivation of a module, as returned, and A and B | the C comparison | (never fails) |
| `flatten-imports` | a claim record, its final modules | the flattened claim record | a block does not exist, or an id collides |
| `grade-records` | a quoted record, the id map, the difference list, the attack register, the amendment ledger | `grading` in every graded item; `shownEffect`, `toCheck` in examples | (never fails) |
| `check-checklist` | a record, the merged checklist, the neighbour flags | a report | a checklist line or a flag has no answer |

## `rules check`

The tests of `research/core-logic/tests/DzLegal.CoreLogic.Contracts.Tests`, made to run on any record file (move the checks from the test class into the `Rules` project and let the tests call them, so there is one copy):

1. The record matches `rule-record.schema.json` (the mini validator; the schema may use only the keywords it supports).
2. It loads through `ContractJson.Options` (an unknown member, a wrong-case name or an enum written as a number is an error).
3. Ids are unique in the record. Every `basis`, `refs`, `counting.basis` and expected-finding `ref` points to an id that exists. In a claim record that is not yet flattened, a reference written `<module>.<id>` resolves against `rules/final/<module>.json` when the record imports that id from that module.
4. Every slot used in a condition, a `startsFrom`, a prior step, an element, a boundary or a worked example is in the record's `slots`; every slot of `slots` is in `slots.json`, or is declared in the record's `proposedSlots`, or is the reserved `requests`. A final record has no `proposedSlots` (the executor moves the reconciled ones into `slots.json` first).
5. Every event kind and request kind is in `FactVocabulary`; every claim id is in `data/law/claims`.
6. Effect rules: only a forfeiture that is not interpretive may declare `blockCleanExport` for a deadline; an interpretive prior step declares `warn`; an exit declares `warn`.
7. Each condition has exactly one of `all`, `any`, `not`, `slot`.
8. Script fields. A file written by an agent (derivation, reconciled, final-candidate) must **not** contain `quote`, `source`, `grading`, `history.agreementBeforeReconciliation` or an example's `shownEffect` / `toCheck`. With `--quoted`, the script fields `quote` and `source` are accepted (a quoted copy). A final record must contain `quote`, `source` and `grading` for every statement.
9. For a claim record: every element of the claim in `data/law/claim-elements.json` is carried by at least one `elements[].events`, and every request kind listed there appears in `requests` (or is imported). A missing one is allowed only with a checklist line that says why.
10. With `--store <articles.json>`: every `cite` is in the store (code, article, the version when given, each paragraph), and none is repealed. This is the same lookup as `quote-records`, run early so that a deriver who cites a missing text is told at once.

## `quote-records`: agents point, the script quotes

For every statement: look the `cite` up in the store, which is keyed by (code, article, version) (the version in force when `cite.version` is absent; the version it names otherwise, and the script fails when that version is not in the store); take the paragraphs listed (all of them when `paragraphs` is empty or absent); write, in the quoted copy, `quote` (paragraph number, exact text, SHA-256 of the UTF-8 text) and `source` (`tier`, `store`, and later `textAsOf`, `ledgerRefs`, `ledgerClean` from the amendment ledger).

- **Whole paragraphs only.** There is no way to cite half a paragraph, on purpose: a cut sentence is how a quote is made to say what it does not.
- A cite of a repealed article, of a version the store does not hold, of a paragraph number beyond the article's last, or of an article that is not in the store **fails**. The answer to "not in the store" is a line in the checklist with `searchedNotFound`, and a request to step 2 (`law/requests.jsonl`, one line per article: code, article, who asked, unit, why). Derivers write their requests in their own folder (`rules/derivations/<a|b>/<unit>.requests.jsonl`), so that neither sees the other's; a script appends them to `law/requests.jsonl` once both have returned. The reconciler and the verifier append directly.
- The script is the only writer of `quote`. Run it again whenever the store changes; a changed hash is reported.

## `check-values`: the numbers are read from the text

For every deadline: the words of `extract.period` must be inside the quoted text of its `basis` (level 1 of the text comparison), and the script must read from those words the same value and unit as `period`. The reader of periods is a small table, tested: number words in every case and gender (ثلاثة، ثلاث، خمسة عشر، خمس عشرة، ستين ...), **with or without a one-letter prefix attached** (ب، ل، و، ك: «بخمس سنوات»، «لمدة» are read as «خمس سنوات»، «مدة»), the digits in brackets when the text has them («ستة (6) أشهر»: both must agree), the dual (شهرين، سنتين، يومين) and the singular without a number (شهر، سنة = 1), the units يوم، أيام، شهر، أشهر، شهور، سنة، سنوات. `extract.sanction` must be inside the quoted text, and must fit `kind`: a `forfeiture` needs a word of forfeiture or inadmissibility in the text («سقوط»، «تحت طائلة»، «عدم القبول»، «لا تقبل»); a `limitation` needs «تقادم» or «يتقادم» or «تسقط بالتقادم». A deadline whose text has neither is `interpretive`, and the script says so.

For every deadline with `counting`: each statement of `counting.basis` exists and is quoted (the script cannot read a counting method from words; the support check and the attack do).

For every statement: each number the English or Arabic `says` contains (a period, an amount) must be in the quote, in words or digits. The cited article's own number and its paragraph numbers are exempt (derivers are told not to write them in `says`). This catches "ten days" written beside a text that says fifteen.

For every message shown to the lawyer (`message` of deadlines, prior steps, exits, section rules): each article number it names must be the article of a statement in the item's `basis`, and each period it names must equal `period`.

## `flag-neighbours`: the exception next door

For every statement that cites some paragraphs of an article and not all: list each paragraph **not** cited that begins with, or contains in its first ten words, a word of exception or condition: «غير أنه»، «غير أن»، «إلا أنه»، «إلا إذا»، «ما لم»، «على أنه»، «ويستثنى»، «باستثناء»، «دون الإخلال»، «مع مراعاة»، «لا تطبق»، «لا يسري»، «ويجوز». Also list the article that follows and the one that precedes when they begin with such a word or refer to the cited article by number.

Each flag must be **answered in the record**: the paragraph is cited by another statement, or a checklist line with `doesNotApply` names it and says why. `check-checklist` refuses a final record with an unanswered flag. In the example module, paragraph 2 of CPCA art. 501 («غير أنه في حالة ...») is cited by its own statement, `b11`; in the example claim, the uncited second paragraph of Civil Code art. 309 is answered by checklist line `k8`. Those are the two patterns.

## `compare-derivations`: A against B, field by field, then case by case

Two derivations do not use the same ids, so the comparison works on **content** (on the quoted copies):

1. **Statements** are matched on (code, article, version, set of paragraphs). Reported: cited by A only, by B only, by both with different `role`, with different `interpretive`.
2. **Deadlines** are matched on the article of their main basis; when that fails, on (kind, period). Reported, each as a separate difference: kind, period, starting slot and event, `counting` (method, `extendToWorkingDay`, its cited articles; present in one only), `raisedBy`, declared effect, `when`.
3. **Prior steps** on `kind`; then required-when, exemptions, effects.
4. **Section** on `value` and alternatives; **territory** on the cited article; **exits** on `goesTo` and the cited article.
5. **Elements** on their set of events; **requests** on `kind`; **documents** and **boundaries** are listed side by side (free text cannot be matched by code; the reconciler reads them).
6. **Slots**: used by A only, by B only. **Proposed slots are not matched by id** (two independent derivers name the same fact differently): both lists are shown side by side, and every condition on a proposed slot is a difference the reconciler must map. The reconciler writes the mapping in its report and uses one id; the executor adds the reconciled slots to `slots.json`.

Then **case by case**: `rules examples` runs A's worked examples, B's, and the boundary-case writer's through both records (rule-evaluator.md, "Boundary cases"). Every case on which the two records give a different declared effect for matched items, a different `LastDay` or `DaysLeft`, a different section or a different set of questions is a difference, even when the fields look equal. This catches what the field comparison cannot: two records with the same period and different conditions, or a different counting inside the warning band.

Outputs: `reports/<unit>.differences.json` and `reports/<unit>.idmap.json` (the item ids of A, B and, once it exists, the reconciled record, matched by content; `rules examples --against` and `grade-records` read it). The **agreement rate before reconciliation**: matched and equal fields over all compared fields, per kind of field. That rate is condition 4 of the grades, item by item, **always A against B**, and a headline figure of the step 1 report. **The comparison runs before the reconciler sees anything**, and its reports are committed before the reconciled record.

## `compare-c`: the third derivation of a module, kept as returned

Codex's derivation of a module (`rules/derivations/c/<unit>.json`) is **not** turned into a record; nobody maps it by hand. `compare-c` compares only its mechanical fields with A and B: the cited (code, article, paragraphs); each deadline's kind, period value and unit, the period and sanction words (read by the `check-values` reader), `raisedBy`; each prior step's kind; each exit's destination; the sections. Its free-text fields (starting events, conditions, exemptions, matters, open points) are listed as "C says" beside the matching items of A and B, for the reconciler to read.

Output: `reports/<unit>.c-comparison.json`. C never counts in condition 4. **A mechanical difference between C and a value on which A and B agreed is marked as an attack** (`who: derivationC`), goes to the verifier with the other attacks, and enters the unit's register like any other. C's agreement with A and B is reported apart from the A-against-B rate.

## `flatten-imports`

For each `imports` entry of a claim record, in order:

1. Copy each block named in `blocks` from the module's final record into the claim record's matching list (`basis` for statements, `requests`, `deadlines`, `priorSteps`, `documents`, `boundaries`, `court.territory`, `court.exits`); the word `section` copies `court.section` and fails if the claim has its own.
2. Copy every statement the copied blocks cite (`basis`, `counting.basis`, `readings[].basis`, exemptions' and proof's `basis`).
3. Copy every open point of the module whose `refs` touch a copied item, with the statements it refers to.
4. Prefix every copied id, and every reference to one, with `<module id>.`. References the claim already wrote as `<module id>.<id>` (for example its own `court.section` citing imported statements) now resolve inside the record.
5. Add the module's `slots` that the copied blocks read to the claim's.

Copied items keep the module's `grading`. The module's worked examples and checklist are not copied; they are run against the module itself. The import line stays in the claim record as the trace. The hand-made builder of the two examples did exactly this (the frame's test `TheClaimExampleCarriesTheModuleBlocksItImports` checks the result).

**Modules made mostly of statements and requests** (`court-sections`, `proof`, `petition-formal-elements`, `accessory-requests`) are imported by the ids of their statements and requests. A claim record writes **its own** `court.section`, whose `basis` cites the imported `court-sections` statements.

**A claim record never restates a module's rule in its own words.** If a derivation writes its own labour-conciliation deadline inside `unpaid-wages`, the reconciler replaces it by the import and reports a difference between the claim's version and the module's, which is one more independent check of the module.

## `grade-records`

Implements [../rules/GRADES.md](../rules/GRADES.md) exactly, from its inputs: the quoted record; `reports/<unit>.idmap.json` and `reports/<unit>.differences.json` (an item of the record that the id map does not trace to both A and B gets `addedAfterDerivation`; an imported item keeps the module's grading); the attack register `reports/<unit>.attacks.json`; the amendment ledger of step 2; the store's `verified`, `verifiedBy` and `frenchCheck` for each cited (code, article, version). It applies the critical-use rule of condition 2 exactly as [article-store.md](article-store.md) states it ("The critical use rule": for a deadline, a prior step, a section rule or an exit, every cited article needs a `verifiedBy` other than `corpusChecked`), and treats a deadline resting on an entry whose `frenchCheck` is `mismatch` or `noFrench` as interpretive (condition 3). It writes `grading` everywhere the schema allows it, `history.agreementBeforeReconciliation`, and, in every worked example's expected findings, `shownEffect` and `toCheck` (what the app shows once the grade is applied, all slots confirmed). It sets `"stage": "final"` and writes `rules/final/<unit>.json`. It also reads the owner's two decision files of 14 November 2026, `law/text-disputes.json` (a paragraph kept with two readings: grade C, reason `disputedText`) and `law/triage-status.json` (a code whose triage is not complete: reason `triageOpen`, never A); see [../rules/GRADES.md](../rules/GRADES.md).

## The merged checklist and `check-checklist`

Before reading any text, each deriver lists from memory what a lawyer must check for the unit; Codex does the same for each claim type without seeing anything (`rules/derivations/codex/<unit>.checklist.json`). `merge-checklists` merges, into `reports/<unit>.checklist.json` (`[{ "id", "item", "from": [...] }]`):

- the derivers' lists (`derivationA`, `derivationB`) and Codex's (`codex`);
- the questions of the author's November unit for this unit, copied word for word by the executor (`novemberUnit`);
- for a claim type, **the catalogue's own answers turned into questions** (`catalogue`): each article of its `data/law/claims` entry ("Is art. N the right basis for …, and which paragraph?"), each prerequisite, each required document, each suggested request. The derivers are not shown these (their brief is the claim's id, label, description, elements and requests), so that their agreement is not anchored on the catalogue; but the final record must answer each of them.

Exact duplicates are merged; near-duplicates are left, a longer list costs nothing. The final record must answer **every** line: `applies` with the items that answer it, `doesNotApply` with a reason, or `searchedNotFound` with what was searched. The same holds for every flag of `flag-neighbours`.

Why: the checklist is written before the text narrows the mind. A point nobody found in the text ("is there a special law for this?") stays visible as a line that had to be answered, not as a silence.

## The texts given to Codex and to the boundary writer

Codex's derivation C and the boundary-case writer must not get texts chosen by someone who has read A and B. Their texts are fixed by a rule, written to `reports/<unit>.texts.json` and committed **before any derivation of the unit runs**:

- every article of each chapter (the library's `path`, or the corpus's) that contains an article named in the unit's "where to start" line of the handbook's module table, or requested in `law/requests.jsonl` for the unit (by an earlier run); for a claim type, the articles of its catalogue entry stand for the "where to start" line;
- whole chapters, every paragraph numbered, rendered as `[<code> <article> ¶<n>] <text>`.
- where the store has no chapter for an article (the library carries no headings, and the corpus lacks some articles: Civil Code 160 to 188), the chapter is the deepest heading of the **official consolidated edition** (tier 2), read by `records.py unit-texts` in the order the edition prints its articles; only the headings are taken from the edition, never a word of an article. An article whose heading is not found stands alone, marked `noHeadingFound`. A chapter member missing from the store is fetched and read (law/README.md) **before** the texts are committed: formal-notice's chapter needed Civil Code 177, 178, 183 to 185 and 187 (14 November 2026).

## Where the reports go

All under `docs/research/core-logic/rules/reports/`, committed:

| File | Written by | When |
|---|---|---|
| `<unit>.texts.json` | the executor's script, by the rule above | before any derivation of the unit |
| `<unit>.checklist.json` | `merge-checklists` | stage 4 |
| `<unit>.flags.<a\|b\|reconciled>.json` | `flag-neighbours` | stages 4 and 6 |
| `<unit>.values.<a\|b\|reconciled\|final>.json` | `check-values` | stages 4, 6 and 9 |
| `<unit>.differences.json` | `compare-derivations` | stage 4, committed before the reconciler runs |
| `<unit>.idmap.json` | `compare-derivations` (A, B), completed for the reconciled record at stage 6 | stages 4 and 6 |
| `<unit>.c-comparison.json` | `compare-c` (modules only) | stage 4 |
| `<unit>.reconciliation.md` | the reconciler | stage 5 |
| `<unit>.attacks.adversary.json` | the adversary | stage 7 |
| `<unit>.attacks.codex.json` | the Codex attack, through its script | stage 7 |
| `<unit>.support.json` | the support check, split back per unit by its script | stage 7 |
| `<unit>.attacks.json` | the verifier: the register of every attack and its verdict | stage 8 |
| `<unit>.grades.json` | `grade-records` (the grades and reasons per item) | stage 9 |
| `grades.md` | the executor: the grade report of [../rules/GRADES.md](../rules/GRADES.md) | end of step 1 |
| `planted-errors.md` | the executor: the rates of the planted-error test | after all units |

**Shapes.** Each report gets a JSON schema with a test at its first use (as the Codex output schemas do). At least: a difference is `{ "id", "kind", "a", "b", "field", "matchedOn" }`; an id-map entry is `{ "kind", "a", "b", "reconciled" }`; an attack in the register is `{ "id", "who": "adversary" | "codex" | "support" | "derivationC" | "boundaryCase", "target", "kind", "claim", "evidence", "verdict": "fixed" | "refuted" | "open", "verifierQuote", "change" }`.

## The planted-error test

Proof that the checkers work, before their silence is believed. It runs on copies **outside the repository**, under `C:\Users\TheShadow\dz-core-research\planted\`, so that nothing can be diffed against `rules/final/` and no real register or candidate is overwritten.

Take 8 final records and copy them there under new unit ids (`p-01` … `p-08`, in file names, report names and the prompts' placeholders; the record's own `id` field is unchanged, because `rules check` needs it). A script plants **50 errors, each of the ten kinds below five times**, spread over the 8 copies, and keeps the answer key in `planted\key\`, which no agent is given:

| Planted error | Must be caught by |
|---|---|
| a period changed (6 months → 3) | `check-values` |
| a paragraph number shifted by one | the support check (Codex) or the verifier |
| the exception paragraph removed from the cites | `flag-neighbours` |
| limitation and forfeiture swapped | `check-values` (sanction words) |
| the starting event changed (delivery → date of the record) | the support check, the attack |
| a real article cited for something it does not say | the support check |
| a repealed article cited | `quote-records` |
| an exemption dropped from a prior step | the attack, `rules examples` (the boundary cases) |
| `raisedBy` changed | the attack |
| a block declared for a limitation | `rules check` |

Then run the whole chain on the copies: the scripts, the Codex support check, the Codex attack, the verifier, with **the same prompts**, their path placeholders pointing into `planted\` (the verifier's allow-list then holds no path of the real `rules/final/` or `rules/reports/`). Outputs stay there. Report, per kind of error, planted and found, in `rules/reports/planted-errors.md`: only the rates are committed. **If a kind is found less than four times in five, its checker is not trusted** (a working bar: below it, a checker misses more than one error in five, and its silence on real records means little): fix the checker or the prompt, plant new errors, run again, and state the final rates. The agents are told only "check this record"; never that errors were planted.

## The support check

Run in batches of 40 pairs **across units** (modules first, then the claim types by area): about 26 Codex calls in all ([../prompts/rules-support-check-codex.md](../prompts/rules-support-check-codex.md)). The script splits each batch's answers back into `reports/<unit>.support.json`. The first module, taken end to end alone (HANDBOOK §11.8), gets a smaller batch of its own.

## Traps

- **A paraphrase in `says` is not shown to the lawyer and is not a quote.** The app shows `message` and the quoted paragraphs. Keep `says` short and literal; it exists so that the support check can ask "does this text say this?".
- **The same article, two numbering styles.** Always canonical numbers (`187 مكرر`, `536 مكرر 4`, `73-4`); the store refuses anything else.
- **A record that passes every script can still be wrong** in what it leaves out. That is what the checklist, the adversary and the boundary cases are for; do not report "all checks pass" as "the record is right".
