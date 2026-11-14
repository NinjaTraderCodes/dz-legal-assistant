# Specification: planted errors, the proof that the checks catch mistakes

**What it is for.** A checker that says "fine" to everything looks exactly like a checker with nothing to find. Since 14 November 2026 most checks on a rule are made by Claude models (HANDBOOK section 5b), so their agreement proves less than it did. Planted errors are the measured answer to "would this chain have caught a mistake of that kind?". The owner's blocking rule rests on it: **a rule may block the clean export only if the planted-error rounds caught its kinds of error 90 % of the time.**

**What it cannot show.** It measures whether the chain finds an error that is *in the record against the text*. It cannot measure a belief that every model shares and no text contradicts in front of them, because nobody can plant what nobody knows. Say this beside every planted-error figure.

## When

| Round | When | On which records | Errors |
|---|---|---|---|
| 1 | as soon as `formal-notice` and the four high-harm modules are final | copies of those 5 final records | 5 of each kind, as far as the records allow |
| 2 | phase B, as soon as six claim types are final | copies of those 6 final claim types: the first six of HANDBOOK section 7 | 5 of each kind, as far as the records allow |

Neither round is an owner stop point: its rates go into the next report. **After each round, `unit.py regrade --all` runs**, because condition 7 of the grades reads the rates.

A kind that could be planted fewer than five times in a round (few records hold an item of the right sort) is planted as often as possible; the report gives the count. After round 2 each kind has about ten plants.

## Where: outside the repository

Everything happens under `C:\Users\TheShadow\dz-core-research\planted\round-<n>\`, so that nothing can be compared with `rules/final/` and no real register or candidate is overwritten.

- Each record is copied **twice** under new unit ids: `p<n>-01`, `p<n>-02` … carry planted errors; `c<n>-01`, `c<n>-02` … are untouched controls; and once more as `s<n>-01` … for the two kinds a script must catch by failing (below). The ids are used in file names, report names and the prompts' placeholders; the record's own `id` field is unchanged, because `rules check` needs it. That is why `stage7.py`, `prompts.py` and `unit.py` take the unit id and `--root` as arguments and never read them from the record ([unit-driver.md](unit-driver.md), retool list).
- The planted and the control copy of one record never go to the same agent run.
- The answer key is `planted\round-<n>\key\key.json`. No agent is given it, and no prompt names its folder.

## The twelve kinds

`scripts/planted.py plant --round <n> --seed <n> <final records…>` plants them. It changes the **record**, never the store, and writes for each plant: the copy, the item, the kind, the field before and after.

| # | Kind | How the script plants it | Must be caught by | Item sorts it speaks for |
|---|---|---|---|---|
| 1 | `periodChanged` | a deadline's `period.value` changed (6 → 3), `extract` left as it is | `check-values` | deadline |
| 2 | `paragraphShifted` | one cite's paragraph number moved by one, to a paragraph that exists | the support check, or the verifier | every sort |
| 3 | `exceptionUncited` | the statement that cites an exception paragraph removed, with its uses | `flag-neighbours` | every sort |
| 4 | `limitationForForfeiture` | a forfeiture's `kind` set to `limitation` and its `effectWhenExpired` to `warn` (so that `rules check` still passes), its `extract` kept | `check-values` (sanction words) | deadline |
| 5 | `startingEventChanged` | a deadline's `startsFrom` slot replaced by another slot of type date that the same record uses (if it uses none, another date slot of `slots.json` asked for the unit) | the support check, the attack, a boundary case | deadline |
| 6 | `wrongArticle` | a statement's cite replaced by another real article of the same chapter, its `says` kept | the support check | every sort |
| 7 | `repealedCited` | a cite replaced by a repealed article | `quote` | every sort |
| 8 | `exemptionDropped` | one entry of a prior step's `notRequiredWhen` removed | the attack, a boundary case, a worked example | prior step |
| 9 | `raisedByChanged` | a deadline's `raisedBy` changed | the attack | deadline |
| 10 | `blockForLimitation` | a limitation's `effectWhenExpired` set to `blockCleanExport` | `rules check` | deadline |
| 11 | `conditionInverted` | one condition's `is` value flipped (`yes` ↔ `no`), every `says` and message kept | a worked example, a boundary case, the attack | prior step, exit, section, boundary |
| 12 | `doneBesideOpen` | a prior step's `acceptedMeans` removed, so that any means makes it say "done" while a boundary item still asks about the means | the boundary runner | prior step |

After planting, the script runs `rules check` on each planted copy. A plant that makes the record fail the schema (and so is caught before any checker) is replaced by another place of the same kind.

**Kinds 7 and 10 are scored by script alone.** Their catching **is** a script's failure (`quote` refuses a repealed article; `rules check` refuses a block on a limitation), which stops the chain before any agent. So they are planted on the `s` copies, one plant per copy; `planted.py score` runs `quote` and `rules check` on each and counts the plant as caught when the script fails on that item. No agent runs on an `s` copy.

**What `planted.py plant` writes under the root** (`planted\round-<n>\`), for each `p` and `c` copy of a final record `<unit>`:

| File under the root | Made from |
|---|---|
| `reconciled\<copy id>.json` | the final record with its script-written fields removed (`quote`, `source`, `grading`) and `stage` set back to `reconciled`; for a `p` copy, with its plants |
| `reports\<copy id>.checklist.json`, `.texts.json`, `.idmap.json` | copies of the real unit's (the id map already complete: `complete-idmap` is not run) |
| `boundary-cases\<copy id>.json` | a copy of the real unit's |
| `key\key.json` | for each plant: the copy, the item, the kind, the field, the value before and after; for kind 3 the removed cite |

## The run

The chain of [unit-driver.md](unit-driver.md) on each `p` and `c` copy, with **the same prompts** and the same models: stage 6 (`quote`, `check-values`, `flag-neighbours`, the examples and the boundary run; no `complete-idmap`), stage 7b (the Sonnet attack), stage 7c (the Codex support check: **one call per copy**, so that a planted and a control copy never meet), stage 8 (the Opus verifier, whose `verify-input` holds the attack, the support answers, the value mismatches, the store's refusals, the flags and the failed boundary cases), then stage 6's scripts once more on the corrected copy. No adversary (it searches the web and has nothing to find in a planted record; its absence is stated), no stage 8b, no slots, no grade, no tracking. The driver takes `--root C:\Users\TheShadow\dz-core-research\planted\round-<n>`: every path placeholder then points there, and the verifier's allow-list holds no path of the real `rules/`.

The agents are told only "check this record". No prompt ever says that errors were planted, and the handbook's own prompts say truthfully, as they always do, that some statements may be wrong.

State the runs before launching. Round 1: 10 copies (5 planted, 5 controls): 10 Sonnet attacks, 10 verifier runs, 10 Codex support calls. Round 2: 12 copies: 12, 12 and 12. A kind fixed and replanted (under 80 %) adds its copies' runs again, once.

## The score

`scripts/planted.py score --round <n>` reads the key and the copies' outputs and writes `docs/research/core-logic/rules/reports/planted-errors.md` (**only the rates are committed**; the copies and the key stay outside).

For each plant, in order:

1. **Flagged** by which checkers: a script's report names the item; an attack or a support answer names the item or its statement; the boundary runner fails a case on it.
2. **Caught** = flagged by at least one checker **and** the verifier's corrected record has the original value back (or an open point that names the difference). A plant the verifier "fixed" into a third value counts as flagged, not caught. For kind 3 (a removed statement) "the original value back" means: the removed cite (code, article, paragraph) is again in the basis of the item it was taken from.

The report holds, per kind: planted, flagged (per checker), caught, the rate with its interval over both rounds; and for the controls: how many attacks the verifier marked `fixed` on an untouched record (false alarms), with the items changed.

## What the rates decide

| Rate for a kind | Effect |
|---|---|
| Round 1 only: every plant of the kind caught, with at least 3 plants | the kind counts as passed until round 2 (with fewer than 3 plants it stays not passed: `checkerWeak`) |
| Both rounds: 90 % or more caught | passed |
| Under 90 % | **`checkerWeak: <kind>`** on every item of the sorts the kind speaks for: those items cannot be grade A ([../rules/GRADES.md](../rules/GRADES.md)) |
| Under 80 % (the first handbook's bar: a checker that misses more than one error in five) | also: the checker named in the table, or its prompt, is fixed once (version bump); new plants of that kind on the same copies; run again; the report gives the rate before and after |
| A control record changed by the verifier | every such change is listed; three or more in a round: the verifier's prompt is reviewed before the next units, and the owner is told |

`records.py grade --planted` reads `rules/reports/planted-errors.json`, the machine copy of the report, which `planted.py score` rewrites whole after each round:

```json
{
  "rounds": [ { "round": 1, "date": "2026-11-23", "copies": { "planted": 5, "controls": 5, "scriptOnly": 2 },
                "kinds": { "periodChanged": { "planted": 5, "flagged": { "check-values": 5 }, "caught": 5 } },
                "controls": { "changedByVerifier": 0, "items": [] } } ],
  "status": { "periodChanged": "passed", "paragraphShifted": "weak", "repealedCited": "notRun" }
}
```

`status` holds one of `passed`, `weak`, `notRun` for each of the twelve kinds, by the table above over all rounds. A missing file, or `notRun`, gives the grade reason `plantedNotRun`; `weak` gives `checkerWeak: <kind>` on the sorts of item the kind speaks for. **No item is grade A before round 1.**

## Tests

- `plant` with a fixed seed is deterministic; each kind changes exactly the field the table names; a plant never touches the store.
- A planted copy of a fixture record with kind 1 fails `check-values`; with kind 12 fails the boundary runner's "declared none although it applies".
- `score` on a fixture run: a plant flagged and restored is caught; flagged and changed to a third value is not; a control change is a false alarm.
- `grade` gives `checkerWeak` to a deadline when `periodChanged` is under 90 %, and not to a statement.
