# Specification: the driver that takes one unit through its stages

**What it is for.** On 14 November 2026 the first module went through the pipeline by hand: stages were launched from memory, three scripts turned out to be missing, and the verifier had to run three times because findings reached it in pieces. The driver makes the order of a unit's stages a fact of the code. It runs every script that is due, tells the executor exactly which agent to launch next with which prompt, and records every run.

It lives in `research/core-logic/scripts/unit.py`, with tests in `scripts/tests/test_unit.py`. After phase 0 nothing of the pipeline lives outside the repository. **What becomes of each working-folder script of 14 November** (`C:\Users\TheShadow\dz-core-research\`):

| Script of 14 November | Becomes |
|---|---|
| `stage4.py`, `stage6.py` | stages 4 and 6 of `unit.py next` |
| `run_codex.py` (the support check) | `unit.py codex <unit>`, through the background queue |
| `run_boundary_view.py` | stage 8b of `unit.py codex <unit>` |
| `followup_prep.py` | `unit.py verify-input <unit> --followup` |
| `run_codex_c.py` (Codex as derivation C) | **retired**: C is Sonnet's since 14 November; do not absorb it |
| `text_pass.py`, `collect_batch.py`, `codex_queue.py`, `ingest_queue.py` | page readings: `reading.py` and `codex_run.py queue` (phase D) |
| every other file there (`check_collect`, `triage_prep`, `triage_counts`, `recheck_compare`, `calib_dry`, `peek`, `patch_*`) | one-off helpers of the first module: left where they are, unused |

`stage7.py`, `records.py` and `prompts.py` are already in the repository and stay; the retool list below says what each gains.

## Fixed names

| Name | Value |
|---|---|
| `<repo>` | `C:\Users\TheShadow\dz-fable-core` |
| `<CL>` | `<repo>\docs\research\core-logic` |
| `<work>` | `C:\Users\TheShadow\dz-core-research` |
| `PY`, the Python of every command | `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe -X utf8`. **Never bare `python`**: on this laptop it is Python 3.14 without pytest (measured, 14 November 2026). Every `python …` in the handbook and the specifications means `PY …` |
| `<T>`, the rules tool | **a published copy**, `<work>\tools\rules\<short commit>\rules.exe`, made by `PY unit.py tool` (`dotnet publish` of `DzLegal.CoreLogic.Rules.Tool`, Release) and used by every script and every agent (`prompts.py`'s `RULES_TOOL` points at the newest). **Nobody runs the `bin\Release` copy**: a build replaces it, and builds happen while agents work. Publish again after any change of the Rules projects, never while an agent of a unit is running; units in flight finish on the copy they started with |
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
| `python unit.py verify-input <unit> [--followup]` | Builds the one file the verifier receives (stage 8, below); with `--followup`, the next follow-up file. |
| `python unit.py questions --batch <n> <unit> [<unit> <unit>]` | Writes `<CL>\reports\questions\batch-<n>.md` for one to three units (below). |
| `python unit.py tool` | Publishes the rules tool (fixed names, above). |
| `python unit.py slots <unit> [--apply <reply file>]` | Stage 9's slot step; with `--apply`, writes the Arabic checker's corrected questions into `slots.json`. |
| `python unit.py regrade --all` | Phase D, and after each planted-error round: rebuilds the store, re-quotes every final candidate, runs `check-values`, grades the modules, re-flattens and grades the claim types, rewrites `rules/reports/grades.md`. |
| `--root <folder>` on every command | Every path that is normally under `<CL>\rules\` and `<work>\runs\` is taken under that folder instead (the planted-error rounds). |
| `python unit.py tracking <unit>` | Writes or updates the unit's rows in `<CL>\reports\step-1-tracking.md`. |

**What `next` prints for an agent launch** (the executor copies it into the Agent tool, changing nothing):

```text
LAUNCH  stage 5  rules-reconcile  model: opus  fresh agent, background
PROMPT  Your whole task is written in one file. Read it first with your Read tool and follow it exactly,
        including its list of what you may open: <run folder>\rules-reconcile.prompt.md. That file is the only
        thing you may open before its own allow-list takes over. Your final reply is the short reply that file asks for.
THEN    python unit.py done <unit> 5 <agent id>
```

**A no-tool agent** (the third derivation, the attack, every relabeller and judge of an open set) gets the same launch line. It is allowed exactly one tool call of its own: reading its own prompt file. (The isolated relabellers of the mirror set and of hidden v3 are the one exception: they read one file and write one file, and their transcripts are never read: [hidden-v3-commands.md](hidden-v3-commands.md).)

### `records.py ingest-reply`: copying a no-tool agent's answer

```text
PY records.py ingest-reply --agent <agent id> --prompt <the filled prompt file> --schema <schema file> --out <output file>
                           [--ids atk] [--unit <unit>] [--append] [--session <session id>]
```

- **Where the transcript is.** Claude Code writes one JSONL file per agent under the current session's folder: `C:\Users\TheShadow\.claude\projects\c--Users-TheShadow-dz-legal-assistant\<session id>\subagents\`. The command takes the newest session folder unless `--session` names one, and finds the file whose name holds the agent id. `reading.py` already has this collector (`reading.collect`, written for the page readings with a session folder typed in): move it into a shared function and reuse it. This is the executor's own agents' output; it is the only reason a script of this work opens that folder, and it never does so for an agent that saw a hidden set or the mirror set.
- **The tool check.** The transcript's tool calls must be exactly: one `Read` whose path is `--prompt`, and the harness's own hand-back call that returns the reply, if the transcript shows one. Any other tool call: exit 3, nothing written, and the run is discarded.
- **The answer.** The agent's last assistant text, with one surrounding ` ```json ` fence removed if present, must parse as one JSON value and pass `--schema` (the validator the frame's tests already use). Otherwise exit 4, nothing written.
- **What it adds.** With `--ids atk`: each entry of `attacks` gets `"id": "atk-<n>"`, numbered from 1, or from the last id of the output file with `--append` (the second run of a long record's attack). With `--unit`: the answer is wrapped as `{ "unit": "<unit>", … }`.
- Tests: a stored transcript with a second tool call is refused; a reply with a fence parses; `--append` continues the numbering.

## The stages

Each row gives the stage, who does it, what must exist before it, and what the driver runs or asks for. Paths are under `<CL>\rules\` unless they start with `law\`.

| # | Stage | Who | Needs | Does | Output, and the check that makes it "done" |
|---|---|---|---|---|---|
| 0 | Texts | script | the store | `records.py unit-texts <unit> --seed <CL>\law\seed-inventory.json --out reports\<unit>.texts.json`. For a module the seed articles are those with source `pointer:<unit>`; for a claim type those with source `claims:<unit>`. **An empty start list is an error** (exit 2), never an empty file. A chapter member the store lacks is listed as `missingFromStore` (exit 0): the handbook's decision table says which are fetched. For the two labour laws the store has no chapter data: the texts are every article of those laws the store holds, and the tracking file records this as a known limit of the chapter rule. | `reports\<unit>.texts.json`, committed before stage 2 |
| 0b | French sources of derivation B | script; a Sonnet page reading only for pages before 2003 | stage 0 | For each instrument of the unit's texts: the French issue (from the ledger or the corpus), fetched through `oj_fetch.py`; from 2003 on the chapter's text by `pdftotext`, before 2003 the chapter's pages as 300-dpi images. The entry goes into `<CL>\law\french-sources.json` (`{ "<unit>": [ { "what": "…", "paths": ["…"] } ] }`; the list `FRENCH` leaves `prompts.py`) and into `<work>\french\INDEX.md`. **Where no French text can be prepared** (an instrument with no French issue found, a fetch refused), the entry says so, and the prompt's placeholder becomes the sentence "No French text was prepared for <instrument>: say so in your checklist." The filler never throws for a missing chapter. | `law\french-sources.json` holds the unit |
| 2a | Derivation A | Opus 5.5 | stage 0; for a claim type, every imported module final | prompt `rules-derive-a` | `derivations\a\<unit>.checklist.json`, `<unit>.json`; `<T> check --store <S>` passes and `<T> examples` passes |
| 2b | Derivation B | Sonnet 5.5 | as 2a; the unit's entry in `law\french-sources.json` (stage 0b) | prompt `rules-derive-b` | as 2a, under `derivations\b\` |
| 2c | Third derivation (modules only) | Sonnet 5.5, no tool | stage 0 | prompt `rules-derive-c`; reply checked against `prompts\schemas\module-derivation-c.schema.json` | `derivations\c\<unit>.json` |
| 2d | Boundary cases | Sonnet 5.5 | stage 0 | prompt `rules-boundary-cases` | `boundary-cases\<unit>.json`; the driver validates its shape (a JSON array; the keys and outcome words of [rule-evaluator.md](rule-evaluator.md)); a file of another shape is set aside in the run folder, and the stage runs again fresh |
| 3 | Requests | script | 2a and 2b | appends both `requests.jsonl` files to `law\requests.jsonl`; prints each by its `need` field, which the derivers write: `basis` (a rule of the record must cite the text) or `context` | the list; see the handbook's decision table |
| 4 | Checks and comparison | script | 2a, 2b, 2d (and 2c for a module) | for A and for B: `<T> check --store`, `records.py quote`, `check-values`, `flag-neighbours`, `<T> examples`; then `merge-checklists`, `compare` (which, for a claim type, also compares the imported (module, block) sets), the five example runs (`aOnB`, `bOnA`, `boundaryA`, `boundaryB`, `boundaryAB`; **for a claim type on a flattened temporary copy** in the run folder, so that a case about a module's paragraph matches the imported item), `add-cases`, and for a module `compare-c --texts reports\<unit>.texts.json` | `reports\<unit>.values.a|b.json`, `.flags.a|b.json`, `.checklist.json`, `.differences.json`, `.idmap.json`, `.c-comparison.json`. **The driver commits these before it will print the stage 5 launch.** |
| 5 | Reconcile | Opus 5.5 | stage 4 committed; **for a unit with a deadline other than `periods` itself, `final\periods.json` exists**, and its path is in the reconciler's and the verifier's allow-list (`{{module_finals}}`): each deadline's `counting` cites the same statements as `periods`' counting rules, and the reconciliation report names the rule applied | prompt `rules-reconcile` | `reconciled\<unit>.json`, `reports\<unit>.reconciliation.md`; `<T> check --store` and `<T> examples` pass |
| 6 | Checks on the reconciled record | script | stage 5 | `quote`, `check-values`, `flag-neighbours`, `complete-idmap`, `<T> examples`, `<T> examples --boundary … --json` (kept in the run folder), `check-checklist` against the merged checklist and the flags | `reports\<unit>.values.reconciled.json`, `.flags.reconciled.json`, the completed id map |
| 7a | Adversary | Opus 5.5, web | stage 6 | prompt `rules-adversary`; its placeholder `{{leads}}` is filled by the driver with the triage leads recorded for the unit (`law\journal-scan\triage-other-wave.json`, `leads`) and every untriaged title or operative hit of an instrument the record cites (issue, date, the hit's sentence), or "none" | `reports\<unit>.attacks.adversary.json` |
| 7b | Attack | Sonnet 5.5, no tool | stage 6 | `stage7.py render`; prompt `rules-attack`; reply checked against `schemas\attacks.schema.json`; ids `atk-1` … | `reports\<unit>.attacks.sonnet.json` |
| 7c | Support check | **Codex** | stage 6 | `stage7.py pairs` (seeded pairs; the key stays in `<work>\support-keys\`); prompt `rules-support-check-codex`; `stage7.py split` | `reports\<unit>.support.json` with `seededAccepted`, `seededTotal`, `yesTrusted` |
| 8 | Verify | Opus 5.5 | 7a, 7b, and 7c **or** ten minutes of waiting for 7c | `unit.py verify-input`; prompt `rules-verify` | `reports\<unit>.attacks.json` (the register), `final-candidates\<unit>.json`; `<T> check --store` and `<T> examples` pass |
| 8b | Blind view on set-aside boundary cases | **Codex** | the register holds a `boundaryCase` entry with verdict `refuted` or `open` | prompt `boundary-second-view-codex` with those cases | `reports\<unit>.boundary-view.json` |
| 9 | Final | script | stage 8 (and 8b when due) | `quote`, `check-values`, `flag-neighbours`, `<T> examples`, the boundary run; the slots (below); for a claim type `flatten`; `grade` with every register of the unit; `check-checklist`; `tracking` | `final\<unit>.json`, `reports\<unit>.grades.json`, `.checklist-check.json` with `ok: true` |

**Runs per unit**, to state before launching: a module has 8 Claude runs (A, B, C, boundary cases, reconcile, adversary, attack, verify) and 2 Codex calls; a claim type has 7 (no C). **Every agent stage is one run per unit**: the boundary writer and the verifier are never given two units (the first handbook grouped them; the prompts' headers say so no longer). The attack alone may take two runs, when `stage7.py render` gives more than 60,000 characters: deadlines, prior steps and court in the first, the rest in the second, the ids continuing.

**Which script results stop `next`, and which are findings.** `next` stops only when `<T> check --store` refuses a record an agent just wrote at stage 2 or 5 (the handbook's rule then applies: delete, rerun fresh). Everywhere else a script's problem is **a finding, not a stop**: a `check-values` mismatch, a cite `quote` could not fill, a neighbour flag, a failed example or boundary case are written to their report and reach the verifier through `verify-input` (below).

**Fixed lines the reconciler writes** in `reports\<unit>.reconciliation.md`, so that a script can check them: one line `difference <id>: <outcome>` for every entry of `reports\<unit>.differences.json`, and one line `disputed <case id>: <reason>` for every boundary case whose expectation it holds wrong. `unit.py status` shows stage 5 as done only when every difference id has its line; and stage 8 only when every id of `attacks.input.json` is in the register with a verdict, and every `fixed` or `refuted` verdict has a `verifierQuote`.

## Stage 8: the verifier receives everything at once

`unit.py verify-input <unit>` writes `<CL>\rules\reports\<unit>.attacks.input.json`: `{ "unit": "<unit>", "attacks": [ … ] }`, each entry `{ "id", "who", "target", "kind", "claim" }`, gathered from the sources below. (`stage7.py split` writes `"who": "supportCheck"` in `support.json`; `verify-input` renames it `support`, the one value the register uses.)

| Source | `who` | `id` | What becomes the `claim` |
|---|---|---|---|
| `reports\<unit>.attacks.adversary.json` | `adversary` | as given (`adv-n`) | the attack, with its source |
| `reports\<unit>.attacks.sonnet.json` | `attack` | `atk-n` | the attack, with its evidence words |
| `reports\<unit>.support.json`, list `attacks` | `support` | `sup-n` | the statement, the answer and what is missing or wrong |
| `reports\<unit>.c-comparison.json`, list `attacks` | `derivationC` | `c-n` | the mechanical difference |
| stage 6's `check-checklist` report: every unanswered neighbour flag | `neighbourFlag` | `flag-<flag id>` | the flagged paragraph's label, its opening word and its text |
| stage 6's boundary run: every case the reconciled record fails, **and** every case the reconciliation report disputes | `boundaryCase` | `bc-<case id>` | the case (title, petition date, slots, expected outcome, the writer's reason) and the runner's message |
| the runner's message "declared none although it applies" on a prior step, when another matched item asks or warns | `owner` | `done-<item id>` | the owner's rule of 14 November 2026: a step may say "done" only when nothing about the same step is still open |
| stage 6's `check-values` report: every item whose number, unit or sanction words do not match its quoted text | `values` | `values-<item id>` | the field, the value in the record and the words the script read |
| stage 6's `quote` and `<T> check --store` output: every cite the store refuses (no such paragraph, a repealed article) | `store` | `store-<item id>` | the cite and the store's message |

If the support check has not returned when the verifier is launched, the file says so in a top-level `"pending": ["supportCheck"]`; the driver then runs the verifier once more on the support check's attacks alone when they arrive, and until then the unit's items carry the grade reason `supportCheckPending`.

**After the verifier:** the driver runs stage 6's checks on the final candidate. A flag still unanswered, a "done beside open" message, a value mismatch or a cite the store refuses makes a **follow-up**.

**Follow-up verifier runs** (also the late support check, stage 8b's uncertain cases, an owner's answer, a store correction):

- `unit.py verify-input <unit> --followup` writes `reports\<unit>.attacks.followup<n>-input.json` (n from 1), holding only the new entries.
- The prompt is `rules-verify`, filled for a follow-up: its record is `final-candidates\quoted\<unit>.json` (the last candidate, quoted), **not** the reconciled record; its register is `reports\<unit>.attacks.followup<n>.json`; it rewrites `final-candidates\<unit>.json` (git keeps the earlier one).
- `records.py grade` is given every register of the unit in order (`--attacks` repeated) and takes **the latest verdict per attack id**.
- An attack on an item a claim record **imports from a module** is not decided in the claim's run: the driver moves it into the module's next follow-up input, runs the module's follow-up, regrades the module, then re-flattens and regrades the claim types that import it.
- More than two verifier runs on one unit is reported to the owner in the unit's batch.

**The first module.** `formal-notice` was made before the driver existed: its attack file is `formal-notice.attacks.codex.json` (Codex's, with ids `cdx-…`), its third derivation is Codex's, and its registers are `attacks.json`, `attacks.followup.json`, `attacks.followup2.json`. `unit.py` carries a small legacy map for this one unit (stage 7b done by the `.codex` file; stage 8 done by the chain of registers), with a test, so that `status formal-notice` shows every stage done.

## Stage 8b: the rule

**What Codex is offered.** The boundary runner prints, for every case, an `actual` outcome word beside the expected one (`<T> examples --boundary … --json`): the first word, in the order of the table of [rule-evaluator.md](rule-evaluator.md), whose condition the matched items meet. The two words offered for a case are its expected word and the record's `actual` word, in alphabetical order. A case that differs **only by its last day** (the same outcome word, another date) cannot be put as a choice of words: it is not sent to Codex; it goes into the owner's question batch with both dates, and counts as a failure meanwhile. A case that fails only because the evaluator knows no official holiday or weekly rest day is **an evaluator limit**: it is recorded as such in the tracking file and is not an attack. `prompts.py` fills the prompt (`subject` from the unit's row, `texts`, `cases`, `choices`).

For each case Codex was asked about, compare its answer with the case's own expected outcome:

- **Codex gives the verifier's answer:** the case stays as the verifier decided. If it fails the runner, it is a counted failure with the two-family reason, written into the tracking file. The case file is never edited.
- **Codex gives the case writer's answer:** the case is **uncertain**. The driver writes one attack (`who: "owner"`, `id: "uncertain-<case id>"`): "keep both readings in an open point and make the item warn". One more verifier run.

## Stage 9: the slots

For every entry of the final candidate's `proposedSlots` that `slots.json` does not hold: append `{ id, type, question, choices?, "askedFor": [...], "readFromNote": false, "origin": "<unit>" }`, keeping the file's two-space format and the proposer's `why` as it is (no `help` text: that is the reviewer's to write). `askedFor` is `["<unit>"]` for a claim type and `["*"]` for a module; when the claim types are flattened, the driver narrows a module slot's `askedFor` to the claim types whose records hold an item that tests it. The Arabic question must end with «؟». **The Arabic check:** a fresh Sonnet agent, no tool, checks the new questions of each batch of three units in one run ([../prompts/arabic-check.md](../prompts/arabic-check.md)); `unit.py slots <unit> --apply <reply>` writes each corrected question into `slots.json` by script. No verifier run is needed for it.

**Hiding the hand-made seed slots.** The derivers of the labour units are not shown the seed slots that were written by hand before any derivation (`prompts.py`): they are hidden **by a frozen list of ids** in `prompts.py` (`SEED_SLOTS`), never by `origin`. A slot that a final module added is shown to every later deriver.

`records.py grade` writes the final record **without** `proposedSlots` once every proposed id is in `slots.json`, and refuses otherwise: a final record never proposes a slot.

Whether a new slot becomes something the reader reads from the note is decided in phase D ([scheme-extension.md](scheme-extension.md)), never here.

## The owner's question batch

`unit.py questions --batch <n> <units>` reads the three units' final records and registers and writes `<CL>\reports\questions\batch-<n>.md`. One entry per open point whose `grade` is C or D **and** that names an item which could otherwise block or which changes what the lawyer is told (that selection is the driver's; no record field marks a point "for the owner"). Each entry, in plain language:

1. the question in one sentence;
2. the readings, each with the paragraph it rests on;
3. **what the record does today** (the protective reading: it warns, or asks);
4. what would change with each answer.

Entries 1 to 3 are copied from the record. **Entry 4 is written by the executor, from the open point's `readings` only**: for each reading, what the item would then do (block, warn, ask, nothing). The executor adds nothing else of its own except a first paragraph with the units' grades and the sentence that the checks behind them were made inside one model family, apart from Codex's support check (HANDBOOK section 5b). The work goes on; an answer from the owner becomes an attack (`who: "owner"`) in a verifier run.

## The tracking rows

`unit.py tracking <unit>` writes the unit's row in each table of `<CL>\reports\step-1-tracking.md` (whose header carries the one-family sentence of HANDBOOK section 5b): agreement before reconciliation (equal and compared, in all and per kind); whether A or B changed an example after running the tool (each deriver's reply lists every expectation it changed, as its prompt asks; `done` copies that list into the ledger); boundary cases passed by A, by B, by the reconciled record and by the final record; the cases still failing, each with its reason in one line; cited paragraphs outside the texts given (`outsideTextsGiven`); attacks fixed, refuted and open; grades; the proof labels of its items (how many `twoFamilies`, `mechanical`, `oneFamily`).

## Tests the driver must have

- `status` on a folder of fixture files reports each stage correctly, and `next` refuses stage 5 while stage 4's reports are uncommitted, and refuses to reconcile a unit with a deadline while `final\periods.json` is missing.
- `verify-input` gathers one entry from each of the seven sources, with the ids above, and marks a missing support check as pending.
- A boundary-case file of the wrong shape is set aside, never read by the tool.
- `done` on a no-tool agent whose transcript shows a second tool call discards the run.
- The slots step appends in the file's own format (the diff is additions only) and refuses a question without «؟».
- Every child process is started without either API key.
- A unit with a deadline is refused at stage 5 while `final\periods.json` is missing, and `periods` itself is not.
- `verify-input --followup` numbers its files, and `grade` takes the latest verdict of an attack id from several registers.
- `status` refuses "stage 5 done" when a difference id has no line in the reconciliation report.

## The retool list (phase 0, item 4): what must exist before the second unit

Each line is code with tests, committed with the light checks. The handbook's phase 0 gives the order.

| # | What | Where | Why (found on) |
|---|---|---|---|
| 1 | `check_commit.py [<commit>]`: every path of the commit is under the two folders; pytest; and, when the commit touches `research/core-logic/src`, `research/core-logic/tests`, a `*.schema.json` or an example the .NET tests read, the research solution's build (0 warnings) and tests. On failure: fix in a new commit; never push a failing head | `scripts/` | the light checks |
| 2 | `unit.py tool`: the published rules tool; `RULES_TOOL` in `prompts.py` points at it | `scripts/` | a build replaced the tool under running agents |
| 3 | `unit.py` with every command of this file, `--root`, the legacy map of `formal-notice` | `scripts/` | this file |
| 4 | `records.py ingest-reply` | `scripts/` | this file |
| 5 | `prompts.py` fills **every step-1 prompt for a claim type**: `unit_kind` ("claim type"); `unit_brief` (the claim's id, label, description, elements and requests, from `data/law/claims` and `claim-elements.json`, **without** the catalogue's articles, prerequisites and documents: those become checklist questions); `units` and `unit_label`; `module_finals` (the paths of the final modules, and for a module with a deadline `final\periods.json`); `leads`; the follow-up form of `rules-verify`; `boundary-second-view-codex`; the French sources from `law\french-sources.json` | `scripts/` | the first module was a module; a claim type's prompts could not be filled |
| 6 | `records.py unit-texts`: the `claims:<unit>` seeds; an empty start list refused; `missingFromStore` and `notInStore` listed without a crash | `scripts/` | stage 0 |
| 7 | `records.py compare` compares the imports of claim records; the case runs use a flattened temporary copy | `scripts/` | stage 4 |
| 8 | `stage7.py` takes `--unit` and `--root` instead of the record's own `id` and the fixed reports folder; one support batch per unit | `scripts/` | the planted copies share their record's `id` |
| 9 | `records.py grade`: `--support <reports\<unit>.support.json>` and `--planted <rules\reports\planted-errors.json>` (a missing file means `plantedNotRun`); condition 7, `supportCheckPending`, the proof labels; the latest verdict per attack id; it writes the final record without `proposedSlots` and refuses while a proposed id is missing from `slots.json` | `scripts/` | [../rules/GRADES.md](../rules/GRADES.md) |
| 10 | `grading.textProof` and `grading.proof` in `rules/rule-record.schema.json` and in the contract's C# record, with the frame's tests; `proof` on store entries (`store.py build`) | schema, `src/DzLegal.CoreLogic.Contracts`, `scripts/` | `rules check` refuses an unknown field |
| 11 | `<T> examples --boundary … --json` prints `actual` for each case | `src/DzLegal.CoreLogic.Rules.Tool` | stage 8b |
| 12 | The writer pool and the background queue of `codex_run.py` | `scripts/` | [codex-calls.md](codex-calls.md) |
| 13 | `SEED_SLOTS` in `prompts.py` | `scripts/` | stage 9 |

**After `periods` is final** (one more item, not part of phase 0): the evaluator's counting of a period is an engineering placeholder behind one function ([rule-evaluator.md](rule-evaluator.md)); replace it by what `final\periods.json` establishes, with tests from `periods`' own examples; publish the tool again; rerun the examples and boundary cases of every final unit; a result that changes goes through a follow-up.
