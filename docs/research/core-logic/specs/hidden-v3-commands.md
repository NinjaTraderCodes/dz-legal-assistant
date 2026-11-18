# Specification: hidden set v3 and its mirror set: building them, protecting them, running v3 once

**What it is for.** Hidden set v3 is the only measure of the reader that nobody could tune on: not the builder, not its agents, not its scripts' authors. Its value is exactly its isolation. Every rule below protects that; none may be traded for convenience.

**Owner's decisions (12 November 2026), binding:**
- **Writer:** isolated Claude subagents, used only for this, write v3 **from their own plan** straight into `C:\dz-hidden-v3`, and reply with **counts only** (no note, no label, no excerpt).
- **Checker:** every note is **relabelled blind**; a script checks each label against the text; disagreements are **dropped and refilled**. Only counts come out. The owner first gave this role to Codex; **since the owner's decision of 14 November 2026 (evening) the relabeller is an isolated Opus 5.5 agent**, because relabelling is not one of Codex's three roles. Writer (Sonnet) and relabeller (Opus) are therefore both Claude models, and **the cases are the writers' own, not code-made**: no voice outside the family checks a v3 label. What compensates is in section 7b.
- No Gemma or Jais model judges v3.
- **Size:** about 1,100 notes: 40 per claim type (840), 200 near-miss notes outside the catalogue, about 60 more for the two rarest slots. Every `readFromNote` slot at least 100 times with a value other than `unknown`; at least 150 denied and 150 disputed events.
- Its gold answers hold **only what is in the note**; "blocks" and "warns" are computed from them by the rule evaluator **at scoring time, on the final records the pre-commitment names**, never stored with the notes (the owner, 18 November 2026).
- The builder never opens the folder, lists it, searches it, or reads the writers' transcripts. Only `summary.md` leaves, after the owner's one run.

The brief said v3 is "read only by the owner's command". The owner's decisions above added the checker: an isolated relabeller reads every note's text (never the writer's labels), and the builder's `v3` commands read the notes and print counts only. Nothing else reads the folder. **No text of v3 or of the mirror set is ever sent to Codex.**

## Who may do what

| Actor | May | May never |
|---|---|---|
| The builder (the executor, Opus 5.5) | write and test the `v3` scripts on made-up fixtures and on the mirror set; launch `v3` commands that print counts; read `summary.md` after the run | open, list or search `C:\dz-hidden-v3*`; read a writer's transcript; print or log a note; send a message to a planner or a writer; read the mirror's notes to improve the sampler or the prompts |
| The planner and the writers (isolated Sonnet 5.5 agents) | read `<root>\inputs\`; the planner writes `<root>\plan\design.json`; a writer writes its own `plan\batch-<nnn>.cases.json` and `notes\batch-<nnn>.jsonl` | open anything outside `<root>`: the repository, the training, dev or mirror notes, the sampler, the classifier, a memory file, the plan or state file; reply with anything but integers |
| The relabellers (isolated Opus 5.5 agents, one fresh agent per relabel batch) | read **one file**: their own prompt file in `<root>-relabel\`, which holds the guide and 25 note texts; write **one file**: their labels file beside it | open `<root>` or anything else; see the writer's labels, the plan or a note's id; reply with anything but integers |
| The third judge (fresh Sonnet 5.5 agents, **mirror set only**) | answer the closed sheets a script builds from the mirror's kept notes | see the agreed labels; judge anything of v3 |
| The scripts | read and write inside the set's folder | print anything but counts, fixed status words and the SHA-256 that `v3 finalize` prints |
| The owner | everything; runs the one scoring command | — |

`<root>` is `C:\dz-hidden-v3` for v3 and `C:\Users\TheShadow\dz-core-research\mirror` for the mirror set. `<root>-relabel` is the folder beside it with `-relabel` added to its name (`C:\dz-hidden-v3-relabel`, `…\mirror-relabel`). The no-open rule covers `C:\dz-hidden-v3-relabel` like every `C:\dz-hidden*` folder.

## 1. What protects the hidden folders (owner's decision, 13 November 2026)

**No folder is locked.** The access rights of `C:\dz-hidden-v2` and of `C:\dz-hidden-v3` are never changed, by anyone, for this work. The owner decided this on 13 November 2026, after a test on a throwaway folder showed that a lock rewrites the permission entry of every file inside and that a lock granted by account name shuts the owner's own account out on this laptop.

The protection of both hidden sets is exactly these three things, and nothing else:

1. **The no-open rule.** The builder and every agent it starts never open, list or search a hidden folder, its results folder, or a hidden-set writer's transcript. They all run under the owner's account, so nothing technical stops them: the rule is the protection, and it has no exception.
2. **Codex's empty scratch folder and read-only sandbox.** Every Codex call starts in an empty folder outside every repository, with `-s read-only` and `--ephemeral`. Codex is given text in its prompt; it is never pointed at a file.
3. **The Codex event-log check.** Every call's own event log is read by the script, and **a call in which the model ran a command or opened a file is discarded whole**: its answer is not parsed, not stored, not used ([codex-calls.md](codex-calls.md)). The check sees what the model does with its tools; the CLI's own reading of its configuration at start-up leaves no event and is not what the check is for.

**Know what the sandbox does not do.** Measured on this laptop on 12 November: a read-only Codex sandbox **can read a folder at the root of `C:`** (the model's commands run as the local account `CodexSandboxOffline`, and root folders give every local account read access). So the sandbox is not a wall around the hidden folders. That is exactly why check 3 is mandatory and may never be weakened: if Codex ever tried to look, the call would be thrown away, counted apart, and the work would stop until the owner decides (HANDBOOK section 8, "A suspected exposure of a hidden set").

`v3 init` creates `C:\dz-hidden-v3`, `C:\dz-hidden-v3-relabel` and `C:\dz-hidden-v3-results` if they do not exist. It changes no access right.

**Since 14 November 2026 no Codex call carries any text of a hidden set or of the mirror set.** Points 2 and 3 above still matter: they are what keeps the Codex calls of the other roles (note writing, rule checks) from reading a hidden folder.

**What keeps the relabel blind, and what does not.** The relabellers are Claude agents under the owner's account: nothing technical stops one from opening a place that holds the writers' labels. Those places are: `<root>\notes\`, `<root>\plan\`, `<root>\work\` (the compare results hold both sets of labels; `map.json` says which text is which note), `<root>\final\`, and the writers' transcripts in Claude Code's own folders. The protection is the structure and the rule: a relabeller's whole task is one file in **another folder**, which holds texts only; its prompt names no other path; it may open nothing else; it replies with two integers. **The builder cannot check a v3 relabeller's tool calls**, because it never reads a transcript of an agent that saw hidden notes.

A relabeller that copied the writer's labels would raise the kept rate and hide label errors, and **nothing would show it for certain**: the third judge of the mirror cannot tell a copied label from an error all three models share, and it never runs on v3. Three things narrow the risk; each is asked for, or run, as said:

1. **Narrower permission rules, asked of the owner at the stop point "Before the mirror's planner"** (the owner's own Claude Code settings; no folder's access right changes): writers may read `<root>\inputs\` and `<root>\plan\design.json` and write under `<root>\plan\` and `<root>\notes\`; relabellers may read and write only under `<root>-relabel\`; reading `<root>\notes\`, `<root>\plan\batch-*`, `<root>\work\` and `<root>\final\` is denied to agents. A background agent cannot answer a permission prompt, so a denied read simply fails. The same rules stop a slip of the builder's own.
2. **On the mirror only** (an open set): after its relabel, `v3 audit-relabel --root <mirror root>` reads the relabellers' transcripts and prints two counts: runs with exactly one Read and one Write on their own two files, and runs with anything else. It prints nothing else. A run of the second kind is a defect of the prompt to fix before v3.
3. **On v3:** `v3 compare` counts the relabel batches whose share of kept notes is more than three standard deviations above the mirror's mean; the count is printed and goes into the pre-commitment.

This risk is stated, in one sentence, in HANDBOOK section 5b, in `PRECOMMIT.md` and in `FREEZE.md`.

**Two more paths a leak could take, closed by rules:**
- **Auto-loaded context.** Every fresh subagent receives the user's memory index and the repository's `AGENTS.md`. So the planner and writer prompts forbid reading or writing any memory file, the plan file (`C:\Users\TheShadow\.claude\plans\reactive-squishing-blossom.md`) and the state file (`C:\Users\TheShadow\dz-core-research\STATE.md`), and anything outside `<root>`. And after each planner or writer run, `v3 memory compare` (section 5) compares the memory folder's file list, sizes and times with a snapshot taken before the run, and prints only `memory changed: yes` or `memory changed: no`. The memory folder is `C:\Users\TheShadow\.claude\projects\c--Users-TheShadow-dz-legal-assistant\memory\` (the script lists names, sizes and times; it never opens a file). **The builder itself writes no memory while a fan-out of hidden-set agents is under way** (its own write would read as "yes"). **On yes: stop, do not read the memory folder, tell the owner, and start no new session before the owner has looked at the memory**: an entry a sub-agent saved is loaded into every later session. A writer that saved a memory would put hidden content into every later session of the builder.
- **Messages and logs.** Every exception is caught at the top of every command and of the run script and printed as a fixed code; its detail, and the standard error of every child process (the reader, the scorer), go to a file **inside** `<root>` or the results folder. Nothing a hidden set holds may reach a console, `%TEMP%`, the Windows event log or a file outside those two folders.

## 2. The layout inside the folder

```
<root>\
  inputs\        put there by v3 init; no note (the list below)
  plan\          design.json (the planner's quotas per batch); batch-<nnn>.cases.json (each writer's own cases)
  notes\         batch-<nnn>.jsonl, written by the writers (note.schema.json, set = hiddenV3 or mirror)
  work\          compare results, drops, refill quotas, error details; relabel\map.json (which relabel batch and
                 number is which note, and where the probes are)
  final\         v3.jsonl (the kept notes with gold), v3.sha256, probes.json, RUN-STARTED
<root>-relabel\  batch-<nnn>.prompt.md (the whole prompt of one relabel batch: the guide, the scheme's lists, 25 note
                 texts with call-local numbers) and batch-<nnn>.labels.json (written by that batch's relabeller).
                 No id, no label of a writer, no plan.
C:\dz-hidden-v3-results\<run>\   readings, standard error of each child, summary.json, summary.md
```

For the mirror set, `<root>` is `C:\Users\TheShadow\dz-core-research\mirror` and its results folder `C:\Users\TheShadow\dz-core-research\mirror-results`. The files keep the same names (`final\v3.jsonl` included), so that the same code runs on both.

**What `v3 init` copies into `inputs\`,** each file named here because the planner and writer prompts name it:

| File | What it holds | Copied from |
|---|---|---|
| `guide.md` | the labelling guide | `notes/LABELLING-GUIDE.md` **at the generation-freeze commit** (every input is taken at that one commit: after phase D the scheme may be 1.1 and the guide bumped) |
| `scheme.md` | the scheme, **rendered by `v3 init`** as one file a planner can read alone: the 21 claim types, each with its roles, its elements (kind, Arabic proposition, date and amount) and its requests, from `claim-elements.json`; the `readFromNote` slots with their values; the cells table; no link into the repository | `notes/SCHEME.md` and `data/law/claim-elements.json` at the generation-freeze commit |
| `boundaries.md` | the boundary table: for each claim type, what looks like it and is not, and what tells them apart | `notes/SCHEME.md`, section 9, at the scheme version frozen with the generation method |
| `quotas.json` | the set's quotas (format below) | written by `v3 init` from `--set` |
| `note.schema.json` | the format of a note | `notes/note.schema.json` |
| `design.schema.json`, `cases.schema.json` | the formats of the two plan files (below) | the `DzLegal.CoreLogic.Hidden` project, where a test checks them against this section |
| `writing-rules.json` | the value of `set`, the id rule, the provenance values and the status every note carries (below) | written by `v3 init` from `--set` |

`writing-rules.json`, for v3 (for the mirror: `"set": "mirror"`, ids `mr-<batch>-<nn>`):

```json
{
  "set": "hiddenV3",
  "id": "v3-<batch>-<nn>: <batch> is the batch's three digits, <nn> the note's two-digit number in its batch, from 01",
  "provenance": {
    "writer": { "family": "anthropic", "model": "Sonnet 5.5", "date": "<the day of writing, yyyy-mm-dd>", "call": "batch-<batch>" },
    "trainingAllowed": false,
    "composedByCode": false,
    "guideVersion": "<the guide's version, filled in by v3 init>",
    "schemeVersion": "<the scheme's version and freeze commit, filled in by v3 init>"
  },
  "status": "kept"
}
```

The status is provisional: `v3 compare` sets `dropped` and the drop reason, and moves the writer's labels to `provenance.writerLabels` and the relabeller's to `provenance.judgeLabels`. Batch numbers are unique in a set: refill batches continue the numbering. So no two notes can share an id, and no id can collide with an open note's (`n-…`).

## 3. The quotas, stated so that they can be met together

**v3 (kept notes):**
- **40 notes per claim type** (840), each a note of that type; a note with two claim types counts once for each.
- **200 near-miss notes** outside the catalogue, each resembling a named claim type (the boundary table), spread over the 21 types.
- **About 60 more for the two rarest slots.** The two rarest slots are the two the frozen scheme can fill least often; with today's slots they are `nonconciliation_record` and `nonconciliation_record_delivered_on`, which only the two labour types carry. At 40 per type that is at most 80 notes, fewer than the 100 the slot minimum needs. So the 60 are **extra labour notes** (`abusive-dismissal` or `unpaid-wages`), counted apart from the 40 per type: "every claim type equally often" means the 40, and these 60 come on top.
- **Every `readFromNote` slot at least 100 times with a value other than `unknown`**, and each of its values at least once.
- **At least 150 denied and 150 disputed events** in total, the denied ones spread over at least 25 different elements, the disputed ones over at least 25.
- **Every element with every polarity** applies only to the cells of the scheme's table: a cell where no lawyer would write the note (for example the loan itself denied in a loan claim) is not asked for.
- Notes with two claim types: about one in eight, as in the open sets.

**The mirror set (kept notes), in v3's proportions at about 40 % of its size:** 16 per claim type (336); 80 near-miss; about 24 extra labour notes; every `readFromNote` slot at least 40 times with a known value; at least 60 denied and 60 disputed events, each spread over at least 25 elements. That is **about 440 kept notes**. (If the owner takes that saving at the pilot: about 330 kept notes: 12 per claim type, 60 near-miss, 18 for the two rarest slots.)

**Each drawn cell has a minimum:** every element × polarity cell that the scheme's table marks as drawn holds at least 3 kept notes in v3 and at least 1 in the mirror. A line that reports a shortfall ("cell, short by k") is exempt from the rule that merges counts under 3: the owner must see it.

**The rare slots are computed, not typed:** `v3 init` reads the frozen scheme, finds every `readFromNote` slot that 40 notes per claim type cannot bring to 100 known values (today the two labour slots), and sets the extra notes they need; it refuses when no plan can meet the minimum.

**The first round writes more than it keeps.** The quotas above are for kept notes. `v3 init --set <set> --first-round-factor <f>` writes, beside them, the first round's targets: each kept target times `f`, rounded up. For the mirror, `f` = 1 / (1 − the drop rate of the **Sonnet half** of the development set, the half written and relabelled by the same models as the mirror). For v3, `f` comes from the mirror's measured loss of every kind (relabel disagreements, invalid notes, near-copies, batches left under the probe bar). That is the one number the mirror sets for v3, and it is a count of runs: it changes no model, note, prompt or threshold. Refills cover what the first round misses.

`quotas.json`:

```json
{
  "set": "hiddenV3",
  "batchSize": 25,
  "kept":       { "perClaimType": 40, "nearMiss": 200, "extraLabour": 60, "slotKnownMin": 100,
                  "deniedMin": 150, "deniedElementsMin": 25, "disputedMin": 150, "disputedElementsMin": 25 },
  "firstRound": { "perClaimType": 50, "nearMiss": 250, "extraLabour": 75, "slotKnownMin": 125,
                  "deniedMin": 188, "deniedElementsMin": 25, "disputedMin": 188, "disputedElementsMin": 25 },
  "batches": 55,
  "extraLabourClaims": ["abusive-dismissal", "unpaid-wages"],
  "rareSlots": ["nonconciliation_record", "nonconciliation_record_delivered_on"],
  "cells": "the element × polarity table of scheme.md"
}
```

(The `firstRound` figures shown are for `f` = 1.25.)

**If a quota cannot be met** after the last refill round, `v3 quotas` reports the cells still short as counts, and the owner decides. If the owner accepts them, the builder writes the accepted list (cell names and counts, no note) to a file in the repository, and `v3 finalize --accept-shortfall <file>` accepts exactly those cells; the file is named in the pre-commitment. Over-full cells are kept: nothing is trimmed.

## 4. The two plan files

**`plan\design.json`, written by the one planner run: quotas, not cases.** One short entry per batch (about 55 for v3, about 22 for the mirror), and nothing else:

```json
{
  "set": "hiddenV3",
  "batches": [
    {
      "batch": "001",
      "notes": 25,
      "claims":    { "loan-repayment": 2, "unpaid-rent": 1, "khul": 1 },
      "twoClaims": [ ["abusive-dismissal", "unpaid-wages"] ],
      "outside":   [ { "resembles": "unpaid-rent", "n": 1 } ],
      "denied":    [ { "claim": "unpaid-sale-price", "kind": "goods_delivered", "n": 1 } ],
      "disputed":  [ { "claim": "tort-damages", "kind": "harm_by_person", "n": 1 } ],
      "slots":     [ { "slot": "notice_served", "value": "yes", "n": 4 } ],
      "hardCases": [ 3, 11 ],
      "themes":    [ "family workshop", "rented shop", "village land" ]
    }
  ]
}
```

`notes` equals the sum of `claims`, `twoClaims` and `outside` and the batch size. `hardCases` are row numbers of the guide's table of hard cases (section 4.2). `themes` are a few English words to spread the stories; no Arabic sentence, no case.

**`plan\batch-<nnn>.cases.json`, written by each writer before its notes:** the writer's own 25 cases, invented to its batch's line.

```json
{
  "batch": "001",
  "cases": [
    {
      "n": 1,
      "claims": ["loan-repayment"],
      "outside": null,
      "events":   [ { "claim": "loan-repayment", "kind": "money_lent", "polarity": "stated" },
                    { "claim": "loan-repayment", "kind": "debt_unpaid", "polarity": "disputed", "disputedBy": "defendant" } ],
      "slots":    [ { "slot": "notice_served", "value": "unknown" } ],
      "requests": [ "payment" ],
      "hardCase": 4,
      "writing":  { "length": "short", "fields": ["facts", "requests"], "dates": "dd/mm/yyyy", "amounts": "words", "extras": ["correction"] },
      "story": "one or two English sentences"
    }
  ]
}
```

A case of a claim type that offers a choice of role also carries `"roles": [ { "claim": "<claim id>", "relation": "…", "position": "…" } ]`, and its note's gold carries the same with the words that show it: `v3 compare` compares the role like every other field.

An outside case has `"claims": ["outside"]` and `"outside": { "resembles": "<claim id>", "kind": "<outside kind>", "tellingFact": "<the one fact that puts it outside, in English>" }`.

**`v3 validate --plan`** checks the plan files **without the builder reading them**: `design.json` against its schema; each line's counts against its batch size; the lines' totals against the first-round quotas; every denied or disputed cell against the scheme's table; every slot value against the scheme; each cases file against its schema and against its own line. It prints counts only: `design lines`, `design lines invalid`, `design cells short`, `cases files`, `cases files invalid`, `cases off their line`. A `design.json` that fails is not repaired: the planner is run again, fresh, once; what is still short after that is left to the refill rounds.

## 5. The commands (project `DzLegal.CoreLogic.Hidden`, tool `v3`)

Every command prints **only** lines of the form `name: integer` or a fixed status word, and exits non-zero on failure with a fixed error code, never with data. Every command that touches a set takes `--root <folder>`, which has **no default**. Each is developed on made-up fixture notes in temporary folders, then run for real on the mirror set; a test puts a marker string in fixture notes and asserts that the marker appears in **no** console output and in no file outside `--root` and its results folder: the named places are the console, `%TEMP%`, `%USERPROFILE%\.codex\`, `C:\Users\TheShadow\dz-core-research\codex-scratch\`, `C:\Users\TheShadow\dz-core-research\codex-logs\`, the worktree, the rest of `C:\Users\TheShadow\dz-core-research\`, and the Windows Application event log. (`--root`'s own `-relabel` folder is, with the results folder, the only other place a note's text may be.) The same marker test runs once on the mirror before the first v3 command. No test ever runs a command on a hidden folder: the tests use temporary folders only, and the refusals that protect a hidden path (the scorer's, section 9) are tested with a protected prefix the test chooses.

| Command | What it does | Prints |
|---|---|---|
| `v3 init --root <root> --set hiddenV3\|mirror --first-round-factor <f>` | creates the folders if needed, the `-relabel` folder included (it changes no access right); copies `inputs\` (section 2); writes `quotas.json` and `writing-rules.json` | `ready` |
| `v3 memory snapshot --memory <folder>` / `v3 memory compare --memory <folder>` | records, then compares, the memory folder's file names, sizes and times; never opens a file in it. The snapshot is kept in `C:\Users\TheShadow\dz-core-research\memory-snapshot.json` | `memory changed: yes` or `memory changed: no` |
| `v3 validate --plan` | the plan files (section 4) | counts |
| `v3 validate` | each batch file: schema; ids by the rule; `set` and provenance as `writing-rules.json` says; quotes found; formal Arabic; no cast name from the refusal list; no 6-word window from the guide or the prompts. It then writes each quote's `start` and `length` (the code of `build-spans`) | counts: notes valid, invalid per reason |
| `v3 relabel prepare --probes <probe set file>` | cuts the valid notes not yet relabelled into relabel batches of 25 texts, **two of them known-truth probes** at random positions (section 7b); for each batch writes `<root>-relabel\batch-<nnn>.prompt.md` from [../prompts/v3-blind-relabel.md](../prompts/v3-blind-relabel.md) (the guide, the scheme's lists and the 25 texts with call-local numbers: **never** an id, a writer's label or anything of the plan) and records the mapping in `<root>\work\relabel\map.json`; resumable | counts: `relabel batches`, `notes queued`, `probes mixed in` |
| `v3 relabel collect` | for each `batch-<nnn>.labels.json`: the schema (`prompts/schemas/notes-judge.schema.json`); every quote found in its note; every element of the answered claim types answered; one entry per text. An invalid file is set aside and its notes go back to the queue **once**. It scores each batch's two probes against their certain labels; **a batch under the probe bar is set aside whole and its notes queued again once** (section 7b). It writes the spans | counts: `batches valid`, `batches invalid`, `batches under the probe bar`, `notes relabelled`, `probe fields`, `probe fields right` |
| `v3 compare` | writer's labels against the relabeller's, by the rules of the notes pipeline (section 4b); **any disagreement, and any `unclear`, drops the note** | counts: kept, dropped; kept per claim type, per element × polarity, per slot value |
| `v3 probes --open <folder> …` | near-copies inside the set and against the open sets (train, dev, challenge, clean, composed, pilot, and the mirror when the set is v3), read from the folders the state file lists for them; drops set notes that are near-copies of an open note. It also trains the shallow leakage model **by cross-validation inside the set only** and writes its accuracy to `final\probes.json` **without printing it**: no accuracy of any model on v3 is printed before the run; it appears only in `summary.md` | near-copy counts only |
| `v3 probes --against <open set file>` | after `finalize`: near-copies between a newly written open set and the finalized set. Every open set written after v3 is finalized (more training notes, new open notes after a missed target) passes it before it is used | near-copy counts only |
| `v3 quotas` | compares the kept counts with the kept targets; writes the refill to `work\refill.json`, already split into numbered refill batches, each with a line in the format of a `design.json` entry | counts: cells short, notes to write, refill batches |
| `v3 discard --batch <nnn>` / `v3 discard --plan` / `v3 discard --relabel <nnn>` | deletes that batch's cases and notes (or `design.json`) inside the folder; or that relabel batch's labels file, queuing its notes again | `discarded` |
| `v3 finalize [--accept-shortfall <file>]` | writes `final\v3.jsonl` and its SHA-256, **without any rule outcome** (the owner, 18 November 2026: an outcome depends on the final records that exist when it is computed, so any "blocks" or "warns" a score uses is recomputed from the gold slots and the final records at scoring time, and those records' hashes go into the pre-commitment); refuses if a kept target is not met, except the cells the file lists | `final`, counts, the SHA-256 |
| `v3 stats` | the same counts again, at any time | counts |

**Counts can leak too, in the limit.** A table with a cell of one note tells what that note is. Every command, and the run, merges cells under 3 into "other".

### Notes on the commands

- **Every command takes `--root <folder>`**; the table writes it once. `v3 init` creates only the folders of its own root (for the mirror: the mirror folder, its `-relabel` and its results folder).
- **The probe pool** is `C:\Users\TheShadow\dz-core-research\notes-work\sets\probe.jsonl`; `v3 probes --open` takes the open sets' files by path (the state file lists them).
- **`v3 compare` also writes**, inside the folder: each kept note's `provenance.judges`; and `work\dropped-agreed.jsonl`, the dropped notes with only the fields on which writer and relabeller agreed, for the drop-flattery figure (the scorer's `--agreed-only`).
- **`v3 audit-relabel`** exists for the mirror only (section 1) and refuses a root under `C:\dz-hidden`.
- **The marker tests** run on fixtures and on **a marked copy of the mirror folder** (never on the mirror itself, whose notes must not change, and never on v3). The places checked also include `%LOCALAPPDATA%\CrashDumps` and the Windows Error Reporting folders: a native crash of ONNX Runtime does not pass through a top-level catch.
- **A run of the mirror that wrote no score** (a crash of the run script) may be repeated once; the repeat is a line of the mirror's `RUNS.md`.

## 6. The planner and the writers

The planner and the writers are **Sonnet 5.5** agents, the relabellers **Opus 5.5** agents, all started fresh (no fork: a fork would inherit the builder's context, and the builder's context must not travel either way). Prompts: [../prompts/v3-planner.md](../prompts/v3-planner.md), [../prompts/v3-writer.md](../prompts/v3-writer.md), with `{{set}}` = `hiddenV3` or `mirror`.

**How their replies come back: the owner decides, before the mirror's planner runs** (stop point "Before the mirror's planner", HANDBOOK section 8; a second stop point, "Before the v3 planner", brings the rehearsal's result). Ask the owner, in one message, for two things together, and state the fan-out (mirror: 1 planner, about 22 writers, about 24 relabellers, refills, one planted rehearsal run, and about 18 third-judge runs; v3: 1 planner, about 55 writers, about 60 relabellers, refills):

1. **The owner's opt-in, in the owner's own words, to the Workflow tool** for the planners, writers and relabellers of the mirror and of v3. The Workflow runs them with a reply schema of **integers only** (`{"batches", "notes"}` for the planner, `{"batch", "written"}` for a writer, `{"batch", "relabelled"}` for a relabeller): a reply that holds text is rejected by the schema and **cannot** reach the builder. This is the default route.
2. **The owner's permission, in the owner's own Claude Code settings, for agents to read and write under `C:\Users\TheShadow\dz-core-research\mirror`, `…\mirror-relabel`, `C:\dz-hidden-v3` and `C:\dz-hidden-v3-relabel`.** These folders are outside the session's working folders, and a background agent cannot answer a permission prompt. This is a setting of Claude Code, not an access right of a folder: no folder's access rights change. Either route needs it.

**The Workflow route, concretely.** Load the `workflow-authoring` skill before writing the script. Each agent is one `agent(prompt, { model: 'sonnet' or 'opus', schema })` call, the schema written in the script (`{ type: 'object', properties: { batch: { type: 'integer' }, written: { type: 'integer' } }, required: [...] }` and its two siblings). The script never passes an agent's output to `log()` and returns only the integers.

**The rehearsal.** The mirror set is built with exactly the route, prompts and commands v3 will use, so building it rehearses the whole chain. On the Workflow route, add one planted run: a writer prompt that asks for a reply holding a marker sentence; the builder must receive the schema's rejection without the marker. No v3 planner, writer or relabeller runs before the mirror chain has run to `v3 finalize` with no reply that held text, no batch left under the probe bar and `memory changed: no` after every run.

**Without the owner's opt-in:** the Agent tool, the numbers-only rule of the prompts, and **the discard rule**: any reply that is not exactly the two integers is treated as a leak, whatever it holds. On this route a text reply reaches the builder's context before anyone can judge it; that is why it is the fallback, and the report names the route that ran.

**The order, for a set:**

1. `v3 init`; `v3 memory snapshot`.
2. **One planner run.** Reads `inputs\`; writes `plan\design.json`; replies with two integers. Then `v3 memory compare` and `v3 validate --plan`. The planner invents the quota lines from the guide, the scheme, the boundary table and the quotas: **it is not drawn by the sampler**, and it is told nothing about how the training notes were made.
3. **Writer runs,** one per batch, a few in parallel (say how many before launching). Each reads its line of `design.json` and `inputs\`, **invents its own 25 cases and writes them first to `plan\batch-<nnn>.cases.json`**, then writes `notes\batch-<nnn>.jsonl`, and replies `{"batch": <n>, "written": <n>}`. A writer that cannot write its file replies `written: 0` and nothing else. After each run returns: `v3 memory compare`.
4. **If a reply ever holds anything but the integers** (a note, a label, an excerpt, an explanation): stop. Do not continue the conversation with that agent. Run `v3 discard --batch <nnn>` (or `--plan`), and tell the owner what happened in one line, without repeating the text. The batch is written again by a fresh writer with the same batch number and line, who invents new cases.
5. `v3 validate --plan`, `v3 validate`, `v3 relabel prepare`; then **one fresh relabeller per relabel batch**, a few in parallel (say how many first), each launched with one sentence that names only its prompt file (`<root>-relabel\batch-<nnn>.prompt.md`) and replying `{"batch": <n>, "relabelled": <n>}`; after each run `v3 memory compare`; a reply that holds anything but the integers is a leak (point 4: `v3 discard --relabel <nnn>`, a fresh relabeller). Then `v3 relabel collect`, `v3 compare`, `v3 probes`. `records.py ingest-reply` is **never** run on a planner's, a writer's or a relabeller's transcript.
6. **Refills:** `v3 quotas`, then one fresh refill writer per refill batch (`{{round}}` = `refill 2`, then `refill 3`; `{{batch}}` = the refill batch's number), then steps 5 and 6 again. **At most three rounds in all: the first and two refill rounds.** The probes run in every round before `v3 quotas`, so that a near-copy dropped late is refilled like any other loss. A cell still short after the third round goes to the owner (section 3).
7. `v3 finalize`.

**Relabelling is a loop.** `v3 relabel prepare`, the relabellers, `v3 relabel collect`: collect may put notes back in the queue (an invalid file, a batch under the probe bar), so the three are repeated until `prepare` prints `notes queued: 0`. A note whose second relabel is also set aside is dropped as "relabel invalid" and refilled like any other loss. Only then `v3 compare`.

| Situation | Do |
|---|---|
| A reply holds anything but its integers, **on v3** | Stop. Discard that batch (`v3 discard`). Tell the owner in one line, without the text. **Wait** for the owner (stop point "A suspected exposure of a hidden set"). |
| The same **on the mirror** (an open set) | Discard the batch, run it again with a fresh agent, count it, go on; say it at the stop point before v3. Its cause is looked for (next row) before v3. |
| The rehearsal finds a defect after the generation method is frozen (a prompt that invites a tool or a sentence in the reply, a high drop rate from a fault of a prompt, a failed marker test) | Fix it; bump the prompt's version; record it as an amendment of the frozen method (commit id and one line in the report); run that stage of the mirror again. "The mirror chain ran clean" means: the **last** full pass of each stage had no reply that held text and no batch left under the probe bar. |
| A writer replies `written: 0`, writes fewer notes than its line, or never returns | `v3 discard --batch <nnn>`; one fresh writer for that batch; a second failure leaves the gap to the refill rounds. |
| `v3 validate` finds more than half of a batch invalid | The same: discard, one fresh writer, then the refills. |
| `cases off their line` is not zero | Reported as a count. The quotas are checked on what was written (`v3 quotas`), so the refills correct it. |
| A relabel file is invalid, or its batch is under the probe bar | Its notes go back to the queue once (the loop above). Under the bar a second time, for the same notes: stop point, with the probes' per-field rates. |

The writers' transcripts stay on disk in Claude Code's own folders. **The builder never opens them**, and never uses a search that could match their content.

## 7. The mirror set

**What it is for:** to predict v3 and to give v3's result its error bars. **It chooses nothing.** It is never used to decide when to stop climbing the ladder, whether composed notes stay, clean against Codex-trained, the learning curve, thresholds or prompts: all of those are decided on dev and challenge (and the clean test split where relevant). The builder does not read mirror notes to improve the sampler or the prompts, and its dropped notes are not sorted into guide feedback.

**When:** after the generation method is frozen (notes pipeline, section 11: commit id recorded), immediately before v3, with the same roles, prompts and commands. Building it earlier would let several hundred notes written by v3's planner and writer prompts shape the training material for weeks.

**What is measured on it:**
- When it is built: the third blind judge and the probes (section 7b).
- **Once, at the pre-commitment, for the at most two final candidates:** their scores on the mirror (the prediction of v3, with intervals), and the drop-flattery figure (their score on the dropped mirror notes against the kept ones). The mirror is scored by the run script of section 9 with its mirror paths, which rehearses that script too. Its pre-commitment is `docs/research/core-logic/results/mirror/PRECOMMIT.md` and its trace `docs/research/core-logic/results/mirror/RUNS.md`.

## 7b. What stands in for a second family: the probes and the third judge

Writer and relabeller are both Claude models, and a v3 case is its writer's own. So two measures are added, and their figures go into the pre-commitment beside every mirror and v3 score.

### Probes for the relabeller

**What they are.** 150 notes composed by code ([notes-pipeline.md](notes-pipeline.md), section 9), set apart as `set: "probe"` and never trained on. Their labels are exact by construction (code chose the facts and placed the phrases), **and each probe was confirmed once by a blind Sonnet relabel**: a probe on which Sonnet differed was dropped. (The phrases come from a local model and were kept by the local filter, so "by construction" alone would claim too much.)

**How they are used.** `v3 relabel prepare --probes <file>` puts **two probes in every relabel batch**, at random positions, drawn without replacement until the pool is used up and then again (a fresh relabeller has no memory of another batch). The relabeller cannot tell that a text is a probe from anything it is told. `v3 relabel collect` compares its labels for the two probes with their certain labels, field by field, by the rules of the notes pipeline (section 4b).

**The bar.** The counted fields of a probe are those of the noise measurement ([notes-pipeline.md](notes-pipeline.md), section 5); a composed probe has about 6 to 8 of them. An `unclear` on a probe's field is a wrong field. **A batch is under the probe bar when two or more counted fields of its two probes are wrong** (one wrong field is allowed: with about 14 fields, "95 %" would mean none). The pooled rate over all batches must be at least 95 %; it is reported. Its labels are set aside and its notes are relabelled by a fresh Opus agent. Under the bar again: **stop point**, with the per-field rates (probes are open notes, so their rates may be shown).

**What is reported:** over all batches, the probe fields right over the probe fields, with its interval, per kind of field. **And its limit, in the same sentence:** composed notes are simpler than written ones, so this rate is an upper bound on the relabeller's accuracy on real notes.

Probes never enter `final\v3.jsonl` and are never scored as test notes.

### The third judge, on the mirror set only

After the mirror is finalised, every kept note is judged once more, **by another route**: not a free labelling, but a sheet of closed questions.

1. `notes third-judge-sheets --set mirror` builds, for each kept note, a numbered list of questions with lettered options, from the scheme and the note's agreed claim types (never from its other labels): one yes/no question for each agreed claim type and for three other claim types (for an outside note: the claim type it resembles and two others, all expected "no"); for every element of those claim types and for two decoy elements, what the note says about it (happened; did not happen; the parties disagree; not mentioned); each slot's own question; for each request of those claim types and one decoy, whether the note asks for it; for a claim type with a choice of role, which role; for each date and amount the plain-code parsers found, what it is the date or the amount of (the candidate roles and "none of these").
2. Fresh **Sonnet 5.5** agents, no tool, 25 notes per run (about 18 runs for 440 notes), answer the sheets ([../prompts/notes-third-judge.md](../prompts/notes-third-judge.md)). They never see a label.
3. `notes third-judge --compare <replies>` compares each answer with the agreed label. **The label-noise figure** of the mirror is the share of kept notes on which the third judge differs from the agreed label on at least one field; beside it, the share of differing answers per kind of field; each with its interval ([probes.md](probes.md), section 4). A note where a decoy is answered as present is counted apart (the judge's own error).

**Say beside the figure:** the third judge is a Claude model, like the writer and the relabeller. An error all three share is not counted, so the figure is a floor, not an estimate of every wrong label. It stands for v3 as well: v3 is never judged a third time.

Also on the mirror, as a mechanical cross-check that needs no model: the share of labelled dates and amounts whose normal form differs from what the plain-code parsers read at the same place. It is reported, and nothing is dropped because of it (dropping by the reader's own parser would bias the set towards what the reader can read).

### When the mirror says "do not build v3 yet"

Before the v3 planner runs, at the stop point "Before the v3 planner", any of these goes to the owner with its figure, and v3 waits for the answer: the mirror's label-noise figure is over 5 % of kept notes; more than half of the mirror's written notes were dropped; the relabeller's pooled probe rate is under 95 %; `v3 audit-relabel` counted a run that used another tool. (With 3 % of wrong labels a 95 % target is already measured with a ruler whose error is the size of the gap: [probes.md](probes.md), section 4.)

## 8. Before the run: the commitment (step 4, last item before v3)

**Two commitments, in this order.** (1) **The mirror's**, `docs/research/core-logic/results/mirror/PRECOMMIT.md`, committed before the mirror is scored: the hashes of every file each candidate loads, the reader's source hash, the scorer's hash, the thresholds and coverage floors, the targets and the three-verdict rule, the candidates' names, the SHA-256 of the mirror's `final\v3.jsonl`, and the mirror's label-noise and probe figures. (2) Then the mirror is scored, once. (3) Then the whole v3 chain is built. (4) **Then v3's**, below, which adds what only exists by then: v3's SHA-256, the relabeller's probe rate on v3, the count of relabel batches kept far above the mirror's rate, and the mirror's scores. (5) Push; the owner's command. **No commit is made between v3's `PRECOMMIT.md` and the run**: the run script refuses unless the worktree's head is that commit.

One commit, pushed, whose id the owner receives with the command. It holds, under `docs/research/core-logic/results/hidden-v3/PRECOMMIT.md`:

- the SHA-256 of every file the reader loads (model, vocabulary, thresholds, rule tables), and the reader's source hash (below);
- the scorer's hash ([scorer.md](scorer.md)) and the SHA-256 of this specification;
- the thresholds and the coverage floors;
- the targets, the two settings, the three-verdict rule and the headline rule (copied from [scorer.md](scorer.md));
- the exact contents of `summary.md`;
- the SHA-256 of `final\v3.jsonl` as `v3 finalize` printed it, and the accepted-shortfall file if there is one;
- the mirror set's results for the same candidates, scored once: the prediction of the v3 score, the label-noise and drop-flattery figures, each with its interval, stated **before** the run; the relabeller's probe rate on the mirror and on v3; and the sentence of section 7b on what one model family cannot see;
- how many candidates are run. **At most two** (for example the main reader and the clean-notes-trained one), both named here. More candidates would turn a hidden set into a tuning set.

**Hashes are of sources and model files, not of built programs.** A program built in another folder or with another SDK does not hash the same, so the step 6 rebuild could never match it. The reader's source hash and the scorer's hash are each the SHA-256 of a list: the path and SHA-256 of each source file of the project, as committed (`git show <commit>:<path>`, so a clone's line-end settings do not change it), sorted by path.

## 9. The run (owner stop point: the owner runs it, once)

The owner's laptop has Windows PowerShell 5.1 only (PowerShell 7 is not installed). From the worktree's root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File research\core-logic\scripts\Run-HiddenV3.ps1 -Commit <precommit id>
```

With `-Set mirror`, the builder runs the same script once on the mirror set, at the pre-commitment (section 7): it then uses the mirror's folders and `docs/research/core-logic/results/mirror/` instead of the paths below.

The script, in order:

1. Refuses unless the working tree is clean at that commit.
2. Refuses if `final\RUN-STARTED` exists or if `docs/research/core-logic/results/hidden-v3/RUNS.md` already has a run line, unless the owner adds `-SecondRun` (the owner does so only after deciding a second run, and the line records it).
3. **Before it runs anything:** writes `final\RUN-STARTED` (date, commit) and appends to `RUNS.md` one line: run number, date and time, commit, candidates, `started`. A run that crashes or is stopped still leaves this trace outside the folder, and a second start is visible there.
4. Removes `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from its own environment, and sets `DOTNET_DbgEnableMiniDump=0` there (no crash dump of a process that holds notes; no system setting is changed).
5. Recomputes every hash of `PRECOMMIT.md`; on any difference, completes the line with `refused: hashes` and stops.
6. Refuses unless the laptop is offline: the owner turns the network off before running (the reader needs none). The script checks; it changes no system setting.
7. Runs each committed candidate over `final\v3.jsonl` in both settings (`reader read --notes … --setting typeGiven` and `typeUnknown`, `--out` a file under `C:\dz-hidden-v3-results\<run>\`; the command prints nothing of a note), with each child's standard error redirected to a file there. The programs were built before the network was turned off (`dotnet build` of the research solution at that commit); the script builds nothing.
8. Runs the scorer with `--run <run number>`. The scorer refuses a gold file under `C:\dz-hidden*` without a run number that `RUNS.md` holds as `started`, so no other path scores v3 without a trace.
9. Copies **only** the summaries (`summary-<candidate>.md`, one per candidate) to `docs/research/core-logic/results/hidden-v3/`, and completes the `RUNS.md` line: `finished`, or `failed: <fixed code>`. With `-Set mirror` it also scores `work\dropped-agreed.jsonl` (the drop-flattery figure).

It prints the path of the summaries and fixed codes only, never a message from a child. The builder commits `RUNS.md` and the summaries, an incomplete line as it is.

**Write the script for Windows PowerShell 5.1, and test it with `powershell.exe` (never `pwsh`) on fixtures and on the mirror.** The 5.1 pitfalls met in Part 7 and since:
- No `&&` or `||` between commands, no ternary `? :`, no `??`: use `if`.
- `$PSScriptRoot` is empty in `param()` defaults: compute paths in the script's body.
- `ConvertFrom-Json` emits a JSON array as one object: assign the result to a variable first, then wrap it in `@()`.
- `Out-File` writes UTF-16 and `Set-Content` the ANSI code page by default, and `-Encoding UTF8` adds a byte-order mark: append to `RUNS.md` with `[System.IO.File]::AppendAllText` and a UTF-8 encoding without a mark.
- `2>&1` on a native program wraps each line of its standard error in an error record and sets `$?` to false even on success: redirect a child's standard error to a file and test `$LASTEXITCODE`.
- Set `$ErrorActionPreference = 'Stop'` and wrap the body in one `try`/`catch` that prints a fixed code only.

The builder then reads `summary.md` and reports: each target as met, not met (coverage), not met or inconclusive; the headline by the rule of [scorer.md](scorer.md); the mirror's prediction against the result; the mirror's label-noise and drop-flattery figures beside it, with their intervals.

**After the run, nothing is tuned.** If a target is missed, the work continues on dev, challenge and new open notes (each passing `v3 probes --against`); v3 can be run again **only** if the owner decides so and the report names it a second run. A second run after changes made in the knowledge of the first summary is weaker evidence, and the report says that too.

## 10. Done when

1. No text of the mirror set or of v3 was sent to Codex; no access right of any hidden folder was changed.
2. Every `v3` command, and `Run-HiddenV3.ps1` under Windows PowerShell 5.1, passed its marker test on fixtures, then on the mirror set.
3. The mirror set is built after the generation freeze, by the route the owner chose: no reply that held text, no batch left under the probe bar, `memory changed: no` after every run; its counts, its SHA-256, the probe rate and the label-noise figure are reported.
4. `v3 finalize` printed `final` with every kept target met (or the owner accepted the named cells of an accepted-shortfall file).
5. `PRECOMMIT.md` is pushed and its commit id is with the owner.
6. After the run: `RUNS.md` and the `summary.md` files are committed; the builder's context holds no v3 text (if it ever did, that is reported).

## Traps

- **"Just a quick look to debug."** There is no debugging of v3 by looking. A v3 command that fails is debugged on fixtures or on the mirror set, where the same code runs on open notes. If the failure cannot be reproduced there, report it to the owner with the error code.
- **Exception messages carry data.** A stack trace with a note's text in it is a leak. Catch at the top, print a fixed code, keep the detail inside the folder (section 1).
- **A relabeller's reply that holds text is a leak, not a retry.** Stop, discard that relabel batch, tell the owner in one line without the text ("A suspected exposure of a hidden set", HANDBOOK section 8).
- **Never put a writer's label, a note's id or a plan line into the `-relabel` folder,** and never name `<root>` in a relabel prompt. The relabel is blind only as long as that folder holds texts alone.
- **Do not read a relabel prompt file "to check it".** It holds hidden notes. Its builder is tested on fixtures and on the mirror.
- **Do not let the planner or a writer see the sampler, the style cards or the training notes.** Shared blind spots are what a hidden set is there to expose.
- **Do not tune anything on the mirror set.** It is the rehearsal and the forecast of v3, not a development set.
