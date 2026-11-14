# Grades: how sure a rule is, and what it may do

**No lawyer checks the records.** So no agent may say how sure it is: a model's "high confidence" is worth nothing. A grade is **computed by a script** from facts that can be checked: is the text official, was it verified, did two independent derivations agree, did it survive attack, was the Official Journal searched for amendments. The grading script is `records.py grade` (see [../specs/record-checks.md](../specs/record-checks.md)); it writes the `grading` field of every graded item. No agent types a grade, except the grade of an open point (C or D).

Derivers do not read this file: they read [DERIVER-GUIDE.md](DERIVER-GUIDE.md), which says what they need without anything they are meant to find in the texts.

## The seven conditions

| # | Code in `reasons` when it fails | The condition | How the script knows |
|---|---|---|---|
| 1 | `noOfficialText` | The item rests on at least one statement whose quote comes from tier 1 (an Official Journal page) or tier 2 (an official consolidated text). | `basis` is not empty and every statement in it has a `quote` and `source.tier` of 1 or 2. |
| 2 | `textNotVerified` | Every text the item rests on is verified. | The article store marks the entry (code, article, version) `verified` (see [../specs/article-store.md](../specs/article-store.md): what counts differs per code). **Critical use**, applied exactly as article-store.md states it ("The critical use rule"): for an item that is a deadline, a prior step, a section rule or an exit, `corpusChecked` alone is not enough: every article in its basis needs a fresh page reading recorded in `law/readings.jsonl`, that is, any `verifiedBy` other than `corpusChecked`. For other items `corpusChecked` is enough. |
| 3 | `interpretive` | The item says what the words say; it is not a reading. | Neither the item nor any statement in its `basis` has `"interpretive": true`. A deadline without `counting` is interpretive (the texts did not settle how it is counted). A deadline resting on a store entry whose French check (`frenchCheck`) is `mismatch` or `noFrench` is interpretive. |
| 4 | `disagreedBeforeReconciliation` | Derivations A and B gave the same value for this item **before** the reconciler saw them. | The comparison report of A and B (`records.py compare`), through the id map of the unit (`reports/<unit>.idmap.json`). Always A against B: the third derivation of a module (Sonnet, by the flat route) never counts here. `singleDerivation` when there was only one; `addedAfterDerivation` when neither A nor B had the item (the reconciler or the verifier added it). |
| 5 | `attackOpen` | No attack on the item is open. | The attack register: every attack that names the item is `refuted` or `fixed`, with the verifier's quote. `notAttacked` when no attack pass ran. `supportCheckPending` while Codex's support check of the unit has not returned, or returned with its seeded pairs failed ([../specs/unit-driver.md](../specs/unit-driver.md), stage 8). |
| 6 | `ledgerNotClean` | The Official Journal scan found no later text that touches a cited article, up to `source.textAsOf`. | `source.ledgerClean` of every statement in `basis`. `scanNotRun` before the scan; `scanBlind` when an issue of the period could not be read; `triageOpen` while the triage of the scan's hits for a cited code is not complete (owner, 14 Nov 2026). |
| 7 | `checkerWeak: <kind>` | The planted-error rounds showed that the checks catch this item's kinds of error. | `rules/reports/planted-errors.json` ([../specs/planted-errors.md](../specs/planted-errors.md)): every kind that speaks for the item's sort was caught at least 90 % of the time over the rounds run so far (while only round 1 has run: every plant of the kind, with at least 3 plants). `plantedNotRun` before round 1: **no item is grade A before round 1.** |

For a deadline there is one more test inside condition 1: the words of `extract.period` and `extract.sanction` are in the quoted text, and the script reads the same number and unit from them as `period` holds (`valueMismatch` when not). A deadline without `extract` fails condition 1.

A code in `reasons` may be followed by the texts it concerns: `textNotVerified: <code> <article>, <code> <article>`.

**Items that the comparison never saw.** An item of a claim record copied from a module by `records.py flatten` keeps the module's grading. An item added by the reconciler or the verifier that neither A nor B had fails condition 4 with `addedAfterDerivation`, so it is graded B at most: it may warn, never block. A difference between the third derivation and a value on which A and B agreed does not touch condition 4: it becomes an attack in the unit's register, and the verifier decides it (condition 5).

## The owner's blocking rule (14 November 2026, evening)

In the owner's words: a rule may block the clean export "only when the text was proved mechanically or by two families, the value is matched by script, derivations A and B agreed, no attack is open, and the planted-error round caught that kind of error 90 % of the time; otherwise the rule warns. If Codex passes the speed trial, a Codex check counts as the second family for these conditions." The owner then said of the trial: "Today's figures already pass". So a Codex check counts.

| The owner's condition | Where it is tested |
|---|---|
| the text was proved mechanically or by two families | conditions 1 and 2, with the critical use rule: an item that may block never rests on a text whose proof label is `oneFamily` (below) |
| the value is matched by script | the deadline test inside condition 1 (`valueMismatch`). A prior step has no number to match: for it this condition is met by the script's quote of every cited paragraph |
| derivations A and B agreed | condition 4 |
| no attack is open | condition 5, `supportCheckPending` included |
| the planted-error round caught that kind of error 90 % of the time | condition 7 |

Conditions 3 and 6 are this handbook's and stay: a reading never blocks, and a text the Journal scan has not cleared never blocks.

**If Codex fails its bar** (HANDBOOK section 8) and the owner chooses the local models as the second view: a local model's check is **not** a second family for these conditions unless the owner says so in words. Until then the items it would cover keep `supportCheckPending` and warn.

## The proof label

Since 14 November 2026 most checks on a rule are made by Claude models. So every result says how it was proved, in one of three words:

| Label | Means |
|---|---|
| `mechanical` | proved by a script against an official file, with no model in the proof |
| `twoFamilies` | two model families (Claude and Codex) each checked it, blind to each other |
| `oneFamily` | checked only inside one model family |

- **A text** (`proof` on each store entry, from its `verifiedBy`): the table in [../specs/article-store.md](../specs/article-store.md), "The proof label".
- **A graded item** (`grading.textProof` and `grading.proof`, written by `records.py grade`):
  - `textProof`: the weakest label among the texts of its basis (`mechanical` is stronger than `twoFamilies`, which is stronger than `oneFamily`); `unverified` when one of them is not verified.
  - `proof`: `twoFamilies` when `textProof` is `mechanical` or `twoFamilies` **and** Codex's support check, with its seeded pairs passed, answered "yes" for every statement of the item's basis as the final record has it; otherwise `oneFamily`. A statement the verifier changed after the support check is `oneFamily` until a later support check reads it.
- **A report table** carries the label of what it counts, or the count per label.

The label is information for the reader of a report. It does not change a grade by itself: the grade's conditions do.

## The owner's decisions of 14 November 2026 on step 2a

Two inputs of `records.py grade` record decisions the owner took on the step 2a report:

- **A text kept with two readings** (`law/text-disputes.json`): a paragraph whose official wording is probably misprinted, or whose reading is not settled, and that the owner decided to keep with both readings. An item resting on such a paragraph is graded **C** with the reason `disputedText: <code> <article>`, even when the store does not verify the text (it would otherwise be D); the record keeps both readings (an open point with the two `readings`), and the warning carries them. The first one: Law 90-04 art. 19 ¶2 (the page prints «يقدم», the French edition and the library «يقيم»).
- **The triage of the Official Journal scan, per code** (`law/triage-status.json`): until the triage of the hits that touch a code is complete, an item resting on that code fails condition 6 with the reason `triageOpen: <code>`, so it cannot be A. The owner first set the order "the Civil Code and the CPCA first; the Commercial Code, the Family Code and the labour laws before the first module or record that relies on each". **The evening decision of 14 November replaced the second half:** the remaining triage is deferred to phase D, and until it is done every item resting on those codes carries `triageOpen` and warns. The triage itself is one Sonnet reading per Journal issue: condition 6 rests on one model family, and the grade report says so.

## The grade

Read the table from the top; the first line that fits gives the grade.

| Grade | When | What the item may do |
|---|---|---|
| **D** | Condition 1 or 2 fails: no official text, a text from memory or from an unofficial source, or a text not yet verified. | A grade D item that declares a block or a warning **warns, marked "to check"; it never blocks and is never silent**. |
| **C** | Condition 3 fails: a matter of reading (which article governs, limitation or forfeiture, the starting event, the counting, the sanction of a duty the text states without one, anything resting on case law). | Warns. The message carries the readings that were kept. |
| **B** | Conditions 1 to 3 hold and at least one of 4, 5, 6, 7 fails. | Warns. |
| **A** | All seven hold. | Does what the record declares, including blocking the clean export. |

**What goes in `reasons`.** For a grade D item: the failures of conditions 1 and 2 (and `valueMismatch`), which are what the lawyer must know; the conditions below them are not tested, because nothing they could add changes what the item may do. For grades C and B: every condition from 3 to 7 that fails.

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

- **Several process conditions fail at once** (4, 5, 6, 7). The item is still B: it warns, it never blocks. Counting the failures would add nothing the lawyer can use; the `reasons` list names them.
- **The text is not verified** (condition 2). The item is D: it warns, and the warning is marked "to check", so that the lawyer sees both the rule and that its text has not been verified. It cannot block. Since the new order of 14 November 2026 the re-readings of cited texts come in phase D, so **before phase D a grade D for an unverified text is expected**: the rule still warns, marked "to check". What stays unverified after phase D stays D, and the grade report lists it.

An open point does **not** lower the grade of the items it refers to, unless those items are flagged `interpretive`. An invented example: a library's rules say a book is returned within twenty-one days of the loan, or the reader loses the right to borrow; an open point asks whether the librarian applies that loss of their own motion or only when someone complains. The period, its start and its sanction are explicit in the words, and the protective reading (treat the loss as applied) is the same as the rule. The deadline keeps the grade its own conditions give.

## What the report on grades must contain (phase D, after the regrade)

Written to `rules/reports/grades.md`:

- The number of graded items per grade, per kind (statement, element, request, deadline, prior step, exit, section, territory, document, boundary), and how many carry the mark "to check".
- Every item that declares `blockCleanExport`, with its grade and its `reasons`. If none reaches A, say so plainly: then no rule blocks, and the report says which condition stops each one.
- The agreement rate before reconciliation (condition 4, A against B), per kind of field; beside it, and apart, the agreement of the third derivation with A and B on the mechanical fields of the 12 modules.
- The planted-error rates of both rounds, per kind and per checker, and every item that carries `checkerWeak`.
- The proof labels: per kind of item, how many are `twoFamilies` and how many `oneFamily`; per code, how many cited texts are `mechanical`, `twoFamilies`, `oneFamily`, unverified. And the sentence of HANDBOOK section 5b on what one model family cannot see.
