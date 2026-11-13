# The two example records

| File | What it shows |
|---|---|
| `module.labour-prelitigation.example.json` | A shared module: conciliation before a labour claim, its exemptions, the time limit to file after it fails (a forfeiture that declares `blockCleanExport`), the section, the territory and one exit. |
| `claim.unpaid-wages.example.json` | A claim type, with the module's blocks already copied in by the import (ids prefixed `labour-prelitigation.`), plus its own statements, elements, requests, a limitation, a formal-notice step, documents and a boundary. |

**They are format examples written by hand on 13 November 2026. They are not products of the method**: one author, no second derivation, no attack, no Official Journal scan. Do not treat their legal content as settled.

- **Derivers never open these two files** (their prompts give them [../DERIVER-GUIDE.md](../DERIVER-GUIDE.md) instead, which has its own made-up example). For the two labour claim types and the labour module the rule is absolute: the examples would anchor the derivation.
- Both say `"stage": "final"` to show the shape of a final record (quotes, sources, grades, the script-written `shownEffect` and `toCheck`). **They are not final by the method's own rules**: nothing in them was derived twice or attacked, and the module's checklist line `k5` (the articles of Law 90-04 that are not in the store) is still `searchedNotFound` with a note that it "must be answered before the module is final".
- The fields `quote`, `source` and `grading` were written by a script, as they will be for every record. No agent types them.

## Their grades: the real state of 13 November

The examples were regraded with the verification state of 13 November: a text counts as verified only by the rule of [../../specs/article-store.md](../../specs/article-store.md), and for a deadline, a prior step, a section rule or an exit only after a fresh page reading. No such reading existed. So:

- **Most items are grade D**, with `"toCheck": true` and `reasons` such as `textNotVerified: cpca 504`. In the module: every statement on Law 90-04 and on CPCA arts. 504, 405, 500 and 32; the deadline `d1`, the prior step `p1`, the section, the exit `x1`, the document `doc1` and the boundary `n1`. In the claim: its statements on Law 90-11 and Civil Code art. 309, its elements, the request `r1`, its deadline `d1`, and the documents `doc1`, `doc2` and the boundary `n1`, which rest on no official text at all (`noOfficialText`). Imported items keep the module's grades.
- **Grade B**: the module's statements on CPCA arts. 501, 67 and 69 and the two territory rules (one derivation, never attacked, scan not run); in the claim, the Civil Code art. 179 statement and the formal-notice step `p1`.
- **The `reasons` of a grade D item** name only what failed in conditions 1 and 2, with the texts concerned; those of grades C and B name every failed condition from 3 to 6 ([../GRADES.md](../GRADES.md)).
- **Grade C**: in the claim, the Civil Code art. 186 statement and the damages request `r2`, which rest on a reading.
- **Nothing is grade A**, so no finding blocks the clean export.

## What their worked examples show

Each example lists the findings it is about (the list is partial: an item not listed is not checked) with the **declared** effect, and, written by script, the `shownEffect` once the grade is applied and the `toCheck` mark. Questions are compared as a set.

- **A declared block shown as a warning marked "to check".** Module `ex2` (the non-conciliation record delivered nine months before the petition): `d1` declares `blockCleanExport`; because `d1` is grade D, the app shows `warn` with `toCheck: true`. Claim `ex5` shows the same for the imported deadline `labour-prelitigation.d1`. This is the owner's rule of 13 November in action: a grade D item that declares a block warns, marked "to check"; it never blocks and is never silent.
- **Grade D findings carry the mark whatever their effect.** In module `ex1` the deadline and the step give `none` with `toCheck: true`; in module `ex4` (nothing known) four items ask, each with the mark. A grade B item carries no mark (claim `ex3` and `ex4`: the formal-notice step warns or asks with `toCheck: false`).
- **The civil servant.** The module's prior step `p1` has `requiredWhen`: the plaintiff is not a civil servant (Law 90-04 art. 19 para. 3 takes those disputes out of the conciliation offices). In module `ex8` (a civil servant with no record) `p1` gives `none`, while the exit `x1` and the boundary `n1` warn.
- **The uncited paragraph.** The claim cites only paragraph 1 of Civil Code art. 309; its checklist line `k8` answers the second paragraph («غير أنه ...») as `doesNotApply`, with the reason.
- **A limitation with no counting.** The claim's own deadline `d1` (five years from the day the oldest wage fell due) has no `counting` object, because the texts in the store do not settle how it is counted; it is `interpretive`, with two readings (art. 309 against art. 312, which is not in the store yet).

The open points and the "searched, not found" lines are real: Law 90-04 arts. 3 to 5 and 26 to 30, and Civil Code arts. 312 and 321, were not in the article store on 13 November. Step 2 fetches them.

`research/core-logic/tests/DzLegal.CoreLogic.Contracts.Tests` loads both files on every test run.
