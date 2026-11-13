# Grades: how sure a rule is, and what it may do

**No lawyer checks the records.** So no agent may say how sure it is: a model's "high confidence" is worth nothing. A grade is **computed by a script** from facts that can be checked: is the text official, was it verified, did two independent derivations agree, did it survive attack, was the Official Journal searched for amendments. The grading script is `grade-records` (see [../specs/record-checks.md](../specs/record-checks.md)); it writes the `grading` field of every graded item. No agent types a grade, except the grade of an open point (C or D).

Derivers do not read this file: they read [DERIVER-GUIDE.md](DERIVER-GUIDE.md), which says what they need without anything they are meant to find in the texts.

## The six conditions

| # | Code in `reasons` when it fails | The condition | How the script knows |
|---|---|---|---|
| 1 | `noOfficialText` | The item rests on at least one statement whose quote comes from tier 1 (an Official Journal page) or tier 2 (an official consolidated text). | `basis` is not empty and every statement in it has a `quote` and `source.tier` of 1 or 2. |
| 2 | `textNotVerified` | Every text the item rests on is verified. | The article store marks the entry (code, article, version) `verified` (see [../specs/article-store.md](../specs/article-store.md): what counts differs per code). **Critical use**, applied exactly as article-store.md states it ("The critical use rule"): for an item that is a deadline, a prior step, a section rule or an exit, `corpusChecked` alone is not enough: every article in its basis needs a fresh page reading recorded in `law/readings.jsonl`, that is, any `verifiedBy` other than `corpusChecked`. For other items `corpusChecked` is enough. |
| 3 | `interpretive` | The item says what the words say; it is not a reading. | Neither the item nor any statement in its `basis` has `"interpretive": true`. A deadline without `counting` is interpretive (the texts did not settle how it is counted). A deadline resting on a store entry whose French check (`frenchCheck`) is `mismatch` or `noFrench` is interpretive. |
| 4 | `disagreedBeforeReconciliation` | Derivations A and B gave the same value for this item **before** the reconciler saw them. | The comparison report of A and B (`compare-derivations`), through the id map of the unit (`reports/<unit>.idmap.json`). Always A against B: the third derivation of a module (Codex) never counts here. `singleDerivation` when there was only one; `addedAfterDerivation` when neither A nor B had the item (the reconciler or the verifier added it). |
| 5 | `attackOpen` | No attack on the item is open. | The attack register: every attack that names the item is `refuted` or `fixed`, with the verifier's quote. `notAttacked` when no attack pass ran. |
| 6 | `ledgerNotClean` | The Official Journal scan found no later text that touches a cited article, up to `source.textAsOf`. | `source.ledgerClean` of every statement in `basis`. `scanNotRun` before the scan; `scanBlind` when an issue of the period could not be read. |

For a deadline there is one more test inside condition 1: the words of `extract.period` and `extract.sanction` are in the quoted text, and the script reads the same number and unit from them as `period` holds (`valueMismatch` when not). A deadline without `extract` fails condition 1.

A code in `reasons` may be followed by the texts it concerns: `textNotVerified: <code> <article>, <code> <article>`.

**Items that the comparison never saw.** An item of a claim record copied from a module by `flatten-imports` keeps the module's grading. An item added by the reconciler or the verifier that neither A nor B had fails condition 4 with `addedAfterDerivation`, so it is graded B at most: it may warn, never block. A difference between the Codex derivation and a value on which A and B agreed does not touch condition 4: it becomes an attack in the unit's register, and the verifier decides it (condition 5).

## The grade

Read the table from the top; the first line that fits gives the grade.

| Grade | When | What the item may do |
|---|---|---|
| **D** | Condition 1 or 2 fails: no official text, a text from memory or from an unofficial source, or a text not yet verified. | A grade D item that declares a block or a warning **warns, marked "to check"; it never blocks and is never silent**. |
| **C** | Condition 3 fails: a matter of reading (which article governs, limitation or forfeiture, the starting event, the counting, the sanction of a duty the text states without one, anything resting on case law). | Warns. The message carries the readings that were kept. |
| **B** | Conditions 1 to 3 hold and at least one of 4, 5, 6 fails. | Warns. |
| **A** | All six hold. | Does what the record declares, including blocking the clean export. |

**What goes in `reasons`.** For a grade D item: the failures of conditions 1 and 2 (and `valueMismatch`), which are what the lawyer must know; the conditions below them are not tested, because nothing they could add changes what the item may do. For grades C and B: every condition from 3 to 6 that fails.

**What "block" means, always:** the app's existing blocker that the draft mark lifts. The clean export is blocked; the export with the «مسودة» mark stays allowed. There is no harder stop (owner's decision, 12 November 2026).

## From the declared effect to the effect in the app

A record declares what should happen (`effectWhenExpired`, `effectWhenMissing`, an exit's `effect`). The evaluator works out the declared effect for the case, then lowers it by the grade, and returns both (`DeclaredEffect`, `Effect`).

| Declared | Grade A | Grade B | Grade C | Grade D or no grading |
|---|---|---|---|---|
| `blockCleanExport` | `blockCleanExport` | `warn` | `warn` | `warn`, marked "to check" |
| `warn` | `warn` | `warn` | `warn` | `warn`, marked "to check" |
| `ask`, `none` | unchanged | unchanged | unchanged | unchanged, marked "to check" (a question, or a listed item such as a document, is shown with the mark) |

**"To check" is a mark, not an effect** (owner's decision, 13 November 2026). There are four effects: `blockCleanExport`, `warn`, `ask`, `none`. "To check" is `grading.toCheck` in the record and `ToCheck` on the evaluator's finding, true exactly for grade D. **A grade D item that declares a block or a warning warns, marked "to check"; it never blocks and is never silent; "to check" is never weaker than a warning.** Wherever warnings are shown, counted or required to be read, a marked warning is among them.

One more lowering, whatever the grade: **a value the lawyer has not confirmed never blocks.** If any slot the finding used was proposed by the reader and not confirmed, `blockCleanExport` becomes `warn`.

## What a record may declare

These limits act on the **declared** effect, before any grade:

1. **Only a forfeiture or a prior step may declare `blockCleanExport`.** A limitation always declares `warn`: it is for the defendant to raise, and it may have been interrupted or suspended. An interpretive item always declares `warn`. An exit always declares `warn`. (The frame's tests and `rules check` enforce these.)
2. **A deadline declares `warn` inside its band.** Within `warnBandDays` of the last day, before or after, the evaluator's declared effect is `warn`, not `effectWhenExpired`: the counting of the last day depends on holidays and on the act that started the period, which the app cannot see.

## Why this differs slightly from the plan's table

The plan of 12 November said "B: as A, with one condition weaker". Writing the rule exactly showed two cases that sentence left open, and I closed both in the safe direction:

- **Several process conditions fail at once** (4, 5, 6). The item is still B: it warns, it never blocks. Counting the failures would add nothing the lawyer can use; the `reasons` list names them.
- **The text is not verified** (condition 2). The item is D: it warns, and the warning is marked "to check", so that the lawyer sees both the rule and that its text has not been verified. It cannot block. Step 2 verifies the cited texts; what stays unverified stays D, and the grade report lists it.

An open point does **not** lower the grade of the items it refers to, unless those items are flagged `interpretive`. An invented example: a library's rules say a book is returned within twenty-one days of the loan, or the reader loses the right to borrow; an open point asks whether the librarian applies that loss of their own motion or only when someone complains. The period, its start and its sanction are explicit in the words, and the protective reading (treat the loss as applied) is the same as the rule. The deadline keeps the grade its own conditions give.

## What the report on grades must contain (end of step 1)

Written to `rules/reports/grades.md`:

- The number of graded items per grade, per kind (statement, element, request, deadline, prior step, exit, section, territory, document, boundary), and how many carry the mark "to check".
- Every item that declares `blockCleanExport`, with its grade and its `reasons`. If none reaches A, say so plainly: then no rule blocks, and the report says which condition stops each one.
- The agreement rate before reconciliation (condition 4, A against B), per kind of field; beside it, and apart, the agreement of the Codex derivation with A and B on the mechanical fields of the 12 modules.
- The result of the planted-error test: of the errors planted, how many each checker found.
