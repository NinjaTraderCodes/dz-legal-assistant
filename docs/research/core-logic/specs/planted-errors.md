# Specification: planted errors, the proof that the checks catch mistakes

**What it is for.** A checker that says "fine" to everything looks exactly like a checker with nothing to find. Since 14 November 2026 most checks on a rule are made by Claude models (HANDBOOK section 5b), so their agreement proves less than it did. Planted errors are the measured answer to "would this chain have caught a mistake of that kind?". The owner's blocking rule rests on it: **a rule may block the clean export only if the planted-error rounds caught its kinds of error 90 % of the time.**

**What it cannot show.** It measures whether the chain finds an error that is *in the record against the text*. It cannot measure a belief that every model shares and no text contradicts in front of them, because nobody can plant what nobody knows. Say this beside every planted-error figure.

## When

| Round | When | On which records | Errors |
|---|---|---|---|
| 1 | as soon as `formal-notice` and the four high-harm modules are final | copies of those 5 final records | 5 of each kind, as far as the records allow |
| 2 | phase D, after the regrade | copies of 6 final claim types: the first six of HANDBOOK section 7 | 5 of each kind, as far as the records allow |

A kind that could be planted fewer than five times in a round (few records hold an item of the right sort) is planted as often as possible; the report gives the count. After round 2 each kind has about ten plants.

## Where: outside the repository

Everything happens under `C:\Users\TheShadow\dz-core-research\planted\round-<n>\`, so that nothing can be compared with `rules/final/` and no real register or candidate is overwritten.

- Each record is copied **twice** under new unit ids: `p<n>-01`, `p<n>-02` … carry planted errors; `c<n>-01`, `c<n>-02` … are untouched controls. The ids are used in file names, report names and the prompts' placeholders; the record's own `id` field is unchanged, because `rules check` needs it.
- The planted and the control copy of one record never go to the same agent run.
- The answer key is `planted\round-<n>\key\key.json`. No agent is given it, and no prompt names its folder.

## The twelve kinds

`scripts/planted.py plant --round <n> --seed <n> <final records…>` plants them. It changes the **record**, never the store, and writes for each plant: the copy, the item, the kind, the field before and after.

| # | Kind | How the script plants it | Must be caught by | Item sorts it speaks for |
|---|---|---|---|---|
| 1 | `periodChanged` | a deadline's `period.value` changed (6 → 3), `extract` left as it is | `check-values` | deadline |
| 2 | `paragraphShifted` | one cite's paragraph number moved by one, to a paragraph that exists | the support check, or the verifier | every sort |
| 3 | `exceptionUncited` | the statement that cites an exception paragraph removed, with its uses | `flag-neighbours` | every sort |
| 4 | `limitationForForfeiture` | a deadline's `kind` swapped, its `extract` kept | `check-values` (sanction words) | deadline |
| 5 | `startingEventChanged` | a deadline's `startsFrom` slot replaced by a neighbouring date slot | the support check, the attack, a boundary case | deadline |
| 6 | `wrongArticle` | a statement's cite replaced by another real article of the same chapter, its `says` kept | the support check | every sort |
| 7 | `repealedCited` | a cite replaced by a repealed article | `quote` | every sort |
| 8 | `exemptionDropped` | one entry of a prior step's `notRequiredWhen` removed | the attack, a boundary case, a worked example | prior step |
| 9 | `raisedByChanged` | a deadline's `raisedBy` changed | the attack | deadline |
| 10 | `blockForLimitation` | a limitation's `effectWhenExpired` set to `blockCleanExport` | `rules check` | deadline |
| 11 | `conditionInverted` | one condition's `is` value flipped (`yes` ↔ `no`), every `says` and message kept | a worked example, a boundary case, the attack | prior step, exit, section, boundary |
| 12 | `doneBesideOpen` | a prior step's own check of its means or timing removed, so that it says "done" while a boundary item still asks | the boundary runner | prior step |

After planting, the script runs `rules check` on each planted copy. A plant that makes the record fail the schema (and so is caught before any checker) is replaced by another place of the same kind, except for kinds 7 and 10, whose catching **is** that failure.

## The run

The whole chain of [unit-driver.md](unit-driver.md) from stage 6 to stage 9, on each copy, with **the same prompts** and the same models (Sonnet attack, Codex support check, Opus verifier; no adversary: it searches the web and has nothing to find in a planted record, and its absence is stated). The driver takes `--root C:\Users\TheShadow\dz-core-research\planted\round-<n>`: every path placeholder then points there, and the verifier's allow-list holds no path of the real `rules/`.

The agents are told only "check this record". No prompt ever says that errors were planted, and the handbook's own prompts say truthfully, as they always do, that some statements may be wrong.

State the runs before launching. Round 1: 10 copies (5 planted, 5 controls): 10 Sonnet attacks, 10 verifier runs, 1 or 2 Codex support batches. Round 2: 12 copies.

## The score

`scripts/planted.py score --round <n>` reads the key and the copies' outputs and writes `docs/research/core-logic/rules/reports/planted-errors.md` (**only the rates are committed**; the copies and the key stay outside).

For each plant, in order:

1. **Flagged** by which checkers: a script's report names the item; an attack or a support answer names the item or its statement; the boundary runner fails a case on it.
2. **Caught** = flagged by at least one checker **and** the verifier's corrected record has the original value back (or an open point that names the difference). A plant the verifier "fixed" into a third value counts as flagged, not caught.

The report holds, per kind: planted, flagged (per checker), caught, the rate with its interval over both rounds; and for the controls: how many attacks the verifier marked `fixed` on an untouched record (false alarms), with the items changed.

## What the rates decide

| Rate for a kind | Effect |
|---|---|
| Round 1 only: 5 of 5 caught (or all of fewer plants, at least 3) | the kind counts as passed until round 2 |
| Both rounds: 90 % or more caught | passed |
| Under 90 % | **`checkerWeak: <kind>`** on every item of the sorts the kind speaks for: those items cannot be grade A ([../rules/GRADES.md](../rules/GRADES.md)) |
| Under 80 % (the first handbook's bar: a checker that misses more than one error in five) | also: the checker named in the table, or its prompt, is fixed once (version bump); new plants of that kind on the same copies; run again; the report gives the rate before and after |
| A control record changed by the verifier | every such change is listed; three or more in a round: the verifier's prompt is reviewed before the next units, and the owner is told |

`records.py grade` reads the last line of each kind from `rules/reports/planted-errors.json` (the machine copy of the report). Before round 1 has run, every kind counts as not passed: **no item is grade A before round 1.**

## Tests

- `plant` with a fixed seed is deterministic; each kind changes exactly the field the table names; a plant never touches the store.
- A planted copy of a fixture record with kind 1 fails `check-values`; with kind 12 fails the boundary runner's "declared none although it applies".
- `score` on a fixture run: a plant flagged and restored is caught; flagged and changed to a third value is not; a control change is a false alarm.
- `grade` gives `checkerWeak` to a deadline when `periodChanged` is under 90 %, and not to a statement.
