# Specification: the rule evaluator

**What it is.** One pure function that applies rule records to one case: `IRuleEvaluator.Evaluate(records, slots, petitionDate)` in `research/core-logic/src/DzLegal.CoreLogic.Contracts/RuleEvaluation.cs`. No file, no clock, no network: the same inputs give the same outcome.

**Why there is exactly one.** The same code must (1) run the worked examples of both derivations through both records in step 1, (2) decide, in the scenario sampler of step 3, what a generated case should produce, (3) compute "blocks" and "warns" from the gold answers of hidden set v3, and (4) later run in the app. Two implementations would disagree on an edge, and nobody would see it. So: one project (`DzLegal.CoreLogic.Rules`), no second copy in Python or anywhere else. A script that needs an evaluation calls the .NET tool `rules eval` (below).

**Who writes it.** Opus 5.5 (the executor), in step 2's first pass (HANDBOOK §7, item 3), before any derivation is compared. It needs only this file, [../rules/GRADES.md](../rules/GRADES.md) and the contract.

**Derivers do not read this file.** The deriver's guide ([../rules/DERIVER-GUIDE.md](../rules/DERIVER-GUIDE.md)) describes the evaluation they need in neutral words; keep the two in agreement when either changes.

## Inputs

- `records`: the **final** records of the claim types the lawyer confirmed, with their modules already copied in. The evaluator runs **every record it is given**; choosing the confirmed claim types is the caller's job (`CaseSlots.ClaimIds` is for the caller and for the trace). For step 1's comparison the tool also accepts a derivation-stage or reconciled record; items then have no `grading` and count as grade D for `Effect`, which does not matter there, because comparisons use `DeclaredEffect`.
- `slots`: `CaseSlots`. A slot absent from `Values`, or present with the value `unknown`, is **unknown**. `Requests` is the list of request kinds the petition asks; conditions read it under the reserved slot name `requests`. A slot declared in a record's `proposedSlots` is read like any other.
- `petitionDate`: the date written on the petition.

## Conditions: three values

A condition is **true**, **false** or **unknown**.

| Form | True when | Unknown when |
|---|---|---|
| `{slot, is}` | the slot's value equals `is` | the slot is unknown |
| `{slot, in}` | the value is one of the list | the slot is unknown |
| `{slot: "requests", includes}` | the list holds the kind | never (an empty list is known) |
| `{slot, before}` | the slot's date is strictly before the date | the slot is unknown |
| `{slot, after}` | the slot's date is on or after the date | the slot is unknown |
| `{slot, known}` | `known: true` and the slot has a value other than `unknown` (or `known: false` and it has not) | never |
| `{all: [...]}` | every part is true | no part is false and at least one is unknown |
| `{any: [...]}` | at least one part is true | no part is true and at least one is unknown |
| `{not: c}` | `c` is false | `c` is unknown |

A condition with none or more than one of `all`, `any`, `not`, `slot` set is an error in the record: `rules check` refuses the record.

**The slots to ask for** when a condition is unknown: the unknown slots that could still change the result, in the order they are written. For `all`, skip the parts that are already true; for `any`, skip the parts already false.

## What each kind of item produces

Every item below produces **one finding** (also when its effect is `none`), so that a worked example can state it. `Ref` is the item's id; `RecordId` is the record it sits in.

**Deadline.**
1. If `when` is present and false: `none`. If unknown: `ask`, with questions for the slots of `when`. Stop.
2. If the slot of `startsFrom` is unknown: `ask`, with a question for it. Stop.
3. Compute the last day (see "Counting a period") and `DaysLeft = lastDay − petitionDate` in days.
4. If `|DaysLeft| ≤ warnBandDays` (default 15 when absent): declared effect `warn`.
5. Else if `DaysLeft < 0`: declared effect `effectWhenExpired`.
6. Else: `none`.

`LastDay` and `DaysLeft` are set on the finding whenever step 3 ran.

**Prior step.** The first id in `slots` is the step's own slot (was the step done?); the others are read by its conditions.
1. If `requiredWhen` is present and false: `none`. If unknown: `ask` for its slots. Stop.
2. If the step's slot is `yes`: `none`.
3. If the step's slot is unknown: `effectWhenUnknown`, and in both cases (`ask` or `warn`) the step's slot is asked. Stop: the exemptions are not asked about before the lawyer has said the step was not done.
4. The step's slot is `no`. If any `notRequiredWhen` condition is true: `none`. If none is true and at least one is unknown: `ask` for those slots. If all are false (or there are none): `effectWhenMissing`.

**Exit.** `when` true: `warn` (an exit always warns). False: `none`. Unknown: `ask`.

**Boundary.** With a `when`: true gives `warn`, false `none`, unknown `ask`. Without a `when`, a boundary is documentation for the classifier and the sampler, and produces no finding.

**Section.** The proposed section is `value`, or the `value` of the first alternative whose `when` is true (unknown alternatives are skipped, and their slots asked). If `CaseSlots.Section` is set and differs: one finding of kind `CourtSection`, `Ref` = `"section"`, declared effect `warn`, with the section rule's message. `RuleOutcome.Section` always carries the proposal.

**Document.** With a `requiredWhen` that is false: nothing. Otherwise one finding of kind `Document` with effect `none` and `Message` = the document's `name`: **the app lists the document** (with the mark "to check" when the document is grade D). Whether a document is attached is checked by the app's own form checks, not here.

**Territory rules and requests** produce no finding and ask no question. They are data the app shows.


## From the declared effect to the effect

Apply, in this order: the grade table of [../rules/GRADES.md](../rules/GRADES.md) (an item without `grading` is grade D); then the origin rule (if any slot in `SlotsUsed` has `Origin = Proposed`, `blockCleanExport` becomes `warn`). `ask` and `none` are never changed.

**"To check" is a mark, never a weaker effect** (owner, 13 November 2026). Set `ToCheck = true` on **every** finding of a grade D item, whatever its effect: a warning, a question (`ask`) or a listed document (`none`) carries the mark. A grade D item that declares a block or a warning always comes out as `Warn` with the mark: it never blocks and is never silent.

`Message` is the item's `message` (a document's `name`). For a grade C item with `readings`, the app shows the readings under the message; it looks them up in the record by the finding's `Ref` (the evaluator does not build text). `Trace` says in one English line which branch was taken, with the dates: `d1: start 2025-06-01, last day 2025-12-02, petition 2026-03-01, 89 days late, outside band 15 -> blockCleanExport; grade B -> warn`.

## Questions

`RuleOutcome.Questions` holds each slot to ask for once, with every item that waits for it in `NeededBy`, ordered by first appearance (records in the order given; inside a record: exits, section, prior steps, deadlines, boundaries). The order is tested in the evaluator's unit tests only.

## How a worked example is checked

- **Findings are partial.** Each finding the example lists is checked: its `effect` against the evaluator's `DeclaredEffect`; in a final record, its script-written `shownEffect` against `Effect` (all slots treated as confirmed) and `toCheck` against `ToCheck`. An item the example does not list is not checked.
- **Questions are a set.** Compared without order. Absent: not checked. An empty list: nothing may be asked.
- `section`, when present, must equal `RuleOutcome.Section`.

## Boundary cases

The boundary-case writer's files (`rules/boundary-cases/<unit>.json`, format in [../prompts/rules-boundary-cases.md](../prompts/rules-boundary-cases.md)) are never edited after they are written. They name no item; `rules examples --boundary` matches each case to items and runs it:

1. **Matching.** The case's `expect.about` (`[<code> <article> ¶<n>]`) matches every deadline, prior step, exit and boundary whose `basis` holds a statement citing that article (and that paragraph, when one is given), and also every deadline whose `counting.basis` and every prior step whose exemptions (`notRequiredWhen[].basis`) cite it (executor's decision of 13 November 2026, from the evaluator's build: an exemption paragraph would otherwise match nothing). A case that matches no item is reported: a rule the record may have missed.
2. **Expected declared effect**, by `expect.outcome`:

| Outcome | The matched item must declare |
|---|---|
| `inTime` | a deadline: `none` outside its band, `warn` inside it |
| `late` | a deadline: `effectWhenExpired` outside its band, `warn` inside it |
| `lastDay` | a deadline: `warn` (`DaysLeft` = 0) |
| `stepMissing` | a prior step: `effectWhenMissing` |
| `stepDone` | a prior step: `none` (the step was taken) |
| `stepExempt` | a prior step: `none` |
| `leavesOrdinaryCourt` | an exit: `warn` |
| `ask` | the item: `ask` |
| `unsettled` | the item is `interpretive`, or declares `warn` or `ask`: never `blockCleanExport` and never silent |

3. **Last day.** For a deadline, when the case has `expect.lastDay`, the evaluator's `LastDay` must equal it.
4. **Between two records.** Run on records A and B (and on the reconciled record), the two runs are compared on **`LastDay` and `DaysLeft`** of matched deadlines, as well as on declared effects and questions, so that a difference in counting shows even inside the warning band. Every difference goes into `reports/<unit>.differences.json`.

A case whose expectation is wrong under the text is never corrected: the reconciler records it, with the paragraph, in the reconciliation report.

## Counting a period

`LastDay(start, period, counting)` is the last day on which the act is in time. It lives behind that one function, covered by table tests.

- **What the record says.** A deadline's `counting` is an object derived from the texts: `method` (`excludeFirstAndLastDay`: neither the starting day nor the end day counts, so the act may still be done on the following day; `excludeFirstDay`: the starting day does not count and the period is complete at the end of its last day; `inclusive`: the starting day counts), `extendToWorkingDay` (a period ending on a non-working day runs to the next working day), `basis` (the statements that say so). Then: the end of the period is `start + period` (`excludeFirstDay`), plus one day (`excludeFirstAndLastDay`), minus one day (`inclusive`); moved to the next working day when `extendToWorkingDay` is true.
- **Engineering placeholders, not statements of the law.** Three things the evaluator needs have no source in the store today. They are written as named constants, marked unsourced in the code, and replaced as soon as a final record cites a text for them; record each replacement in the step 1 report:
  1. Calendar arithmetic for months and years: the same day number in the last month, or that month's last day when it is shorter.
  2. The non-working days used by `extendToWorkingDay`: a constant list of weekly rest days (Friday and Saturday), unsourced until the `periods` module cites the text that sets them. Official holidays are **not** known to the evaluator, which is one reason for the warning band.
  3. A deadline **without** `counting` (the texts did not settle it; the deadline is interpretive and so warns at most): counted as `excludeFirstDay` without extension, only so that a date can be shown.
- The `periods` module is finalised before any unit with a deadline is reconciled (HANDBOOK §7); every record writes its own `counting`, citing its own statements.

## The command-line tool

`research/core-logic/src/DzLegal.CoreLogic.Rules.Tool` (a console project), built once by the executor; agents are given the built executable's path (`{{rules_tool}}`) so that parallel agents never build at the same time.

```
rules check   [--quoted] [--store <articles.json>] <record.json>...   # see record-checks.md
rules eval    --records <file>... --case <case.json>                     # prints the RuleOutcome as JSON
rules examples <record.json>... [--against <other-record.json> --idmap <unit.idmap.json>] [--boundary <cases.json>]
```

`rules examples` runs every worked example (exit code 1 on any difference). `--against` runs the examples of record X on record Y, after mapping item ids through `reports/<unit>.idmap.json` (written by `compare-derivations`); it is how step 1 finds that two derivations give different outcomes on the same case. `--boundary` runs a boundary-case file as above.

`case.json` has its own shape: `{ "claimIds": [...], "petitionDate": "yyyy-mm-dd", "slots": { "<slot>": "<value>", "requests": ["..."] }, "section": "social" }` (`claimIds` and `section` are optional; a worked example has neither).

## Tests it must have before any record is compared

1. The truth table above, every row, including `all`/`any` with unknown parts.
2. Each kind of item, each branch (a table of small records written in the test code, not the examples), including a prior step whose `effectWhenUnknown` is `warn` (it also asks).
3. The effect table of GRADES.md, every cell, and the origin rule; that a grade D finding that declares a block or a warning is `Warn` with `ToCheck = true`, never weaker and never absent; and that a grade D `ask` or document finding keeps its effect and carries `ToCheck = true`.
4. `LastDay`: each `method`, with and without `extendToWorkingDay`; no `counting`; end of month, leap year, a period ending on each placeholder rest day; the band's two edges.
5. Both handbook examples: every worked example gives its listed declared effects (and, being final, its `shownEffect` and `toCheck`) and its questions as a set. If one is wrong under this specification, fix the example in `rules/examples/` and say so in the report; do not bend the evaluator.
6. Determinism: the same call twice gives equal outcomes; no `DateTime.Now`, no culture-dependent parsing (dates are parsed with the invariant culture).

## Traps

- **Unknown is not false.** The commonest error is to treat a missing slot as "no" and to warn that a notice is missing when the note simply did not mention it. Unknown asks.
- **Never run rules on a proposed claim type.** `CaseSlots.ClaimIds` holds confirmed types only, and the caller passes only their records. (The app today lets unconfirmed claim ids drive some warnings; that is flagged to the reviewer in the integration contract, not copied.)
- **Do not add an effect.** `FindingEffect` has four values and a test fixes them. A harder stop than `blockCleanExport` is forbidden, and so is an effect weaker than a warning for a rule that fires: "to check" is a mark.
- **Messages come from the record**, in Arabic and English. The evaluator never builds a sentence, so that every word shown to the lawyer was reviewed with the record.
- **A placeholder is not law.** Never cite the placeholders above as a rule, in a record, a message or a report.
