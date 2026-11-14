# Specification: the driver that takes one unit through its stages

**What it is for.** On 14 November 2026 the first module went through the pipeline by hand: stages were launched from memory, three scripts turned out to be missing, and the verifier had to run three times because findings reached it in pieces. The driver makes the order of a unit's stages a fact of the code. It runs every script that is due, tells the executor exactly which agent to launch next with which prompt, and records every run.

It lives in `research/core-logic/scripts/unit.py`, with tests in `scripts/tests/test_unit.py`. It replaces the working-folder scripts of 14 November (`stage4.py`, `stage6.py`, `run_codex.py`, `run_codex_c.py`, `run_boundary_view.py`, `followup_prep.py`, `text_pass.py`): after phase 0 nothing of the pipeline lives outside the repository.

## Fixed names

| Name | Value |
|---|---|
| `<repo>` | `C:\Users\TheShadow\dz-fable-core` |
| `<CL>` | `<repo>\docs\research\core-logic` |
| `<work>` | `C:\Users\TheShadow\dz-core-research` |
| Python | `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe -X utf8` |
| `<T>`, the rules tool | `<repo>\research\core-logic\src\DzLegal.CoreLogic.Rules.Tool\bin\Release\net10.0\rules.exe` |
| `<S>`, the store | `<work>\store\articles.json`; its text views in `<work>\store\text\` |
| A unit's run folder | `<work>\runs\step1\<unit>\` (filled prompts, raw tool output, Codex output) |
| The run ledger | `<work>\ledger\runs.jsonl` ([../STATE-FORMAT.md](../STATE-FORMAT.md)) |

The driver starts every child process through `clean_env` (no API key in any child's environment).

## The commands

| Command | What it does |
|---|---|
| `python unit.py status <unit>` | Prints one line per stage: `done`, `due`, `waiting for <what>`, or `blocked: <why>`. It reads only files: there is no hidden state. |
| `python unit.py next <unit>` | Runs every script stage that is due, in order, stopping at the first failure. Then prints every agent launch and Codex call that is due (below). Fills each prompt with `prompts.py` into the run folder and writes the ledger's launch line. |
| `python unit.py done <unit> <stage> <agent id>` | For an agent stage: checks that the outputs exist and pass their check; for a no-tool agent copies the reply from the transcript (`records.py ingest-reply`); writes the ledger's completion line. On failure it prints what is wrong and the rule to apply (always "delete, rerun fresh", never "repair"). |
| `python unit.py codex <unit>` | Runs the unit's due Codex calls through the background queue ([codex-calls.md](codex-calls.md)) and records them. Returns at once; `status` shows them. |
| `python unit.py verify-input <unit>` | Builds the one file the verifier receives (stage 8, below). |
| `python unit.py questions --batch <n> <unit> <unit> <unit>` | Writes `<CL>\reports\questions\batch-<n>.md` (below). |
| `python unit.py tracking <unit>` | Writes or updates the unit's rows in `<CL>\reports\step-1-tracking.md`. |

**What `next` prints for an agent launch** (the executor copies it into the Agent tool, changing nothing):

```text
LAUNCH  stage 5  rules-reconcile  model: opus  fresh agent, background
PROMPT  Your whole task is written in one file. Read it first with your Read tool and follow it exactly,
        including its list of what you may open: <run folder>\rules-reconcile.prompt.md. That file is the only
        thing you may open before its own allow-list takes over. Your final reply is the short reply that file asks for.
THEN    python unit.py done <unit> 5 <agent id>
```

**A no-tool agent** (the third derivation, the attack, every relabeller and judge) gets the same launch line. It is allowed exactly one tool call: reading its own prompt file. `ingest-reply` checks the transcript: any other tool call discards the run.

## The stages

Each row gives the stage, who does it, what must exist before it, and what the driver runs or asks for. Paths are under `<CL>\rules\` unless they start with `law\`.

| # | Stage | Who | Needs | Does | Output, and the check that makes it "done" |
|---|---|---|---|---|---|
| 0 | Texts | script | the store | `records.py unit-texts <unit> --seed <CL>\law\seed-inventory.json --out reports\<unit>.texts.json`. For a module the seed articles are those with source `pointer:<unit>`; for a claim type those with source `claims:<unit>`. | `reports\<unit>.texts.json`, committed before stage 2 |
| 2a | Derivation A | Opus 5.5 | stage 0; for a claim type, every imported module final | prompt `rules-derive-a` | `derivations\a\<unit>.checklist.json`, `<unit>.json`; `<T> check --store <S>` passes and `<T> examples` passes |
| 2b | Derivation B | Sonnet 5.5 | as 2a; the French sources listed in `prompts.py` (`FRENCH`) | prompt `rules-derive-b` | as 2a, under `derivations\b\` |
| 2c | Third derivation (modules only) | Sonnet 5.5, no tool | stage 0 | prompt `rules-derive-c`; reply checked against `prompts\schemas\module-derivation-c.schema.json` | `derivations\c\<unit>.json` |
| 2d | Boundary cases | Sonnet 5.5 | stage 0 | prompt `rules-boundary-cases` | `boundary-cases\<unit>.json`; the driver validates its shape (a JSON array; the keys and outcome words of [rule-evaluator.md](rule-evaluator.md)); a file of another shape is set aside in the run folder, and the stage runs again fresh |
| 3 | Requests | script | 2a and 2b | appends both `requests.jsonl` files to `law\requests.jsonl`; prints each as "a rule must cite it" (the requesting record has an item whose only basis would be that text) or "context" | the list; see the handbook's decision table |
| 4 | Checks and comparison | script | 2a, 2b, 2d (and 2c for a module) | for A and for B: `<T> check --store`, `records.py quote`, `check-values`, `flag-neighbours`, `<T> examples`; then `merge-checklists`, `compare`, the five example runs (`aOnB`, `bOnA`, `boundaryA`, `boundaryB`, `boundaryAB`), `add-cases`, and for a module `compare-c --texts reports\<unit>.texts.json` | `reports\<unit>.values.a|b.json`, `.flags.a|b.json`, `.checklist.json`, `.differences.json`, `.idmap.json`, `.c-comparison.json`. **The driver commits these before it will print the stage 5 launch.** |
| 5 | Reconcile | Opus 5.5 | stage 4 committed; **for a unit with a deadline, `final\periods.json` exists** | prompt `rules-reconcile` | `reconciled\<unit>.json`, `reports\<unit>.reconciliation.md`; `<T> check --store` and `<T> examples` pass |
| 6 | Checks on the reconciled record | script | stage 5 | `quote`, `check-values`, `flag-neighbours`, `complete-idmap`, `<T> examples`, `<T> examples --boundary … --json` (kept in the run folder), `check-checklist` against the merged checklist and the flags | `reports\<unit>.values.reconciled.json`, `.flags.reconciled.json`, the completed id map |
| 7a | Adversary | Opus 5.5, web | stage 6 | prompt `rules-adversary`; the driver adds to the prompt the triage leads recorded for the unit and every untriaged title or operative hit of an instrument the record cites | `reports\<unit>.attacks.adversary.json` |
| 7b | Attack | Sonnet 5.5, no tool | stage 6 | `stage7.py render`; prompt `rules-attack`; reply checked against `schemas\attacks.schema.json`; ids `atk-1` … | `reports\<unit>.attacks.sonnet.json` |
| 7c | Support check | **Codex** | stage 6 | `stage7.py pairs` (seeded pairs; the key stays in `<work>\support-keys\`); prompt `rules-support-check-codex`; `stage7.py split` | `reports\<unit>.support.json` with `seededAccepted`, `seededTotal`, `yesTrusted` |
| 8 | Verify | Opus 5.5 | 7a, 7b, and 7c **or** ten minutes of waiting for 7c | `unit.py verify-input`; prompt `rules-verify` | `reports\<unit>.attacks.json` (the register), `final-candidates\<unit>.json`; `<T> check --store` and `<T> examples` pass |
| 8b | Blind view on set-aside boundary cases | **Codex** | the register holds a `boundaryCase` entry with verdict `refuted` or `open` | prompt `boundary-second-view-codex` with those cases | `reports\<unit>.boundary-view.json` |
| 9 | Final | script | stage 8 (and 8b when due) | `quote`, `check-values`, `flag-neighbours`, `<T> examples`, the boundary run; the slots (below); for a claim type `flatten`; `grade` with every register of the unit; `check-checklist`; `tracking` | `final\<unit>.json`, `reports\<unit>.grades.json`, `.checklist-check.json` with `ok: true` |

**Runs per unit**, to state before launching: a module has 7 Claude runs of its own (A, B, C, boundary, reconcile, adversary, attack) plus the verifier (8, or one run for two units once the units are small), and 2 Codex calls; a claim type has no C.

## Stage 8: the verifier receives everything at once

`unit.py verify-input <unit>` writes `<CL>\rules\reports\<unit>.attacks.input.json`: `{ "unit": "<unit>", "attacks": [ … ] }`, each entry `{ "id", "who", "target", "kind", "claim" }`, gathered from:

| Source | `who` | `id` | What becomes the `claim` |
|---|---|---|---|
| `reports\<unit>.attacks.adversary.json` | `adversary` | as given (`adv-n`) | the attack, with its source |
| `reports\<unit>.attacks.sonnet.json` | `attack` | `atk-n` | the attack, with its evidence words |
| `reports\<unit>.support.json`, list `attacks` | `support` | `sup-n` | the statement, the answer and what is missing or wrong |
| `reports\<unit>.c-comparison.json`, list `attacks` | `derivationC` | `c-n` | the mechanical difference |
| stage 6's `check-checklist` report: every unanswered neighbour flag | `neighbourFlag` | `flag-<flag id>` | the flagged paragraph's label, its opening word and its text |
| stage 6's boundary run: every case the reconciled record fails, **and** every case the reconciliation report disputes | `boundaryCase` | `bc-<case id>` | the case (title, petition date, slots, expected outcome, the writer's reason) and the runner's message |
| the runner's message "declared none although it applies" on a prior step, when another matched item asks or warns | `owner` | `done-<item id>` | the owner's rule of 14 November 2026: a step may say "done" only when nothing about the same step is still open |

If the support check has not returned when the verifier is launched, the file says so in a top-level `"pending": ["supportCheck"]`; the driver then runs the verifier once more on the support check's attacks alone when they arrive, and until then the unit's items carry the grade reason `supportCheckPending`.

**After the verifier:** the driver runs stage 6's checks on the final candidate. A flag still unanswered, a "done beside open" message, or a cite the store refuses makes one more `verify-input` holding only those entries, and one more verifier run. More than two verifier runs on one unit is reported to the owner in the unit's batch.

## Stage 8b: the rule

For each case Codex was asked about, compare its answer with the case's own expected outcome:

- **Codex gives the verifier's answer:** the case stays as the verifier decided. If it fails the runner, it is a counted failure with the two-family reason, written into the tracking file. The case file is never edited.
- **Codex gives the case writer's answer:** the case is **uncertain**. The driver writes one attack (`who: "owner"`, `id: "uncertain-<case id>"`): "keep both readings in an open point and make the item warn". One more verifier run.

## Stage 9: the slots

For every entry of the final candidate's `proposedSlots` that `slots.json` does not hold: append `{ id, type, question, choices?, "askedFor": [...], "readFromNote": false, "origin": "<unit>" }`, keeping the file's two-space format. `askedFor` is `["*"]` for a module that applies to every claim, the claim ids it names otherwise, and `["<unit>"]` for a claim type. The Arabic question must end with «؟». A fresh Sonnet agent checks the Arabic questions of each batch of three units in one run (plain, correct, formal Arabic; it answers per slot `ok` or a corrected question, which the next verifier run of that unit applies).

`records.py grade` writes the final record **without** `proposedSlots` once every proposed id is in `slots.json`, and refuses otherwise: a final record never proposes a slot.

Whether a new slot becomes something the reader reads from the note is decided in phase D ([scheme-extension.md](scheme-extension.md)), never here.

## The owner's question batch

`unit.py questions --batch <n> <units>` reads the three units' final records and registers and writes `<CL>\reports\questions\batch-<n>.md`. One entry per open point whose `grade` is C or D **and** that names an item which could otherwise block or which changes what the lawyer is told. Each entry, in plain language:

1. the question in one sentence;
2. the readings, each with the paragraph it rests on;
3. **what the record does today** (the protective reading: it warns, or asks);
4. what would change with each answer.

The executor adds nothing of its own to the file except a first paragraph with the three units' grades. The work goes on; an answer from the owner becomes an attack (`who: "owner"`) in a verifier run.

## The tracking rows

`unit.py tracking <unit>` writes the unit's row in each table of `<CL>\reports\step-1-tracking.md`: agreement before reconciliation (equal and compared, in all and per kind); whether A or B changed an example after running the tool (from the ledger notes; the driver asks for it at `done`); boundary cases passed by A, by B, by the reconciled record and by the final record; the cases still failing, each with its reason in one line; cited paragraphs outside the texts given (`outsideTextsGiven`); attacks fixed, refuted and open; grades; the proof labels of its items (how many `twoFamilies`, `mechanical`, `oneFamily`).

## Tests the driver must have

- `status` on a folder of fixture files reports each stage correctly, and `next` refuses stage 5 while stage 4's reports are uncommitted, and refuses to reconcile a unit with a deadline while `final\periods.json` is missing.
- `verify-input` gathers one entry from each of the seven sources, with the ids above, and marks a missing support check as pending.
- A boundary-case file of the wrong shape is set aside, never read by the tool.
- `done` on a no-tool agent whose transcript shows a second tool call discards the run.
- The slots step appends in the file's own format (the diff is additions only) and refuses a question without «؟».
- Every child process is started without either API key.
