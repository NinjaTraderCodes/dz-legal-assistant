# Specification: the notes pipeline (step 3)

From a scenario to a note that may be trained on or tested on. Every stage is a script with a resumable state. Every note carries who wrote it, what filtered it and who relabelled it, so that a writer's notes can be replaced later (owner's decisions, 12 and 14 November 2026).

**Revised on 14 November 2026** for the owner's decisions of 14 November: Codex writes the training notes, several calls at a time; **no Claude model writes, labels or filters a training note**; test sets are written half by Codex and half by Sonnet; nothing that changes the reader is chosen on a Sonnet-written note. The terms report is no longer a stop point: the decision is taken ([../notes/TERMS.md](../notes/TERMS.md)).

```
TRAINING   scenario ─► WRITE (Codex, 20 per call, 2 to 4 calls at once) ─► code checks ─► LOCAL FILTER (closed sheet) ─► kept / kept with blanks / dropped
                                                                                                │
                                                          Opus measures the noise on a sample ◄─┘   (changes no note)

TEST       scenario ─► WRITE (Codex for one half, Sonnet for the other) ─► code checks ─► BLIND RELABEL (Sonnet for Codex's notes, Opus for Sonnet's) ─► kept / dropped
```

**The order of step 3** (the owner's stop points in bold): the scheme frozen; the code; **the pilot**; dev, challenge and training notes to 4,000; **the learning curve**; clean notes and composed notes; slot packs (phase D); the generation method frozen; then the mirror set; then hidden v3 ([hidden-v3-commands.md](hidden-v3-commands.md)). The mirror set comes after the freeze so that nothing in the training material can have been shaped by a set written the way v3 is written.

**Where things are while a set is built.** The scenarios, the writers' answers, the filter's answers, the relabellers' answers and the queue live in `C:\Users\TheShadow\dz-core-research\notes-work\`, outside the repository. A finished set is `notes-work\sets\<set>.jsonl`; the repository holds its counts, statistics and SHA-256, and the notes themselves only if the owner says so (stop point "Committing a note set").

**The code** is the project `DzLegal.CoreLogic.Notes` (C#), tool `notes`, with one command per stage below. Calls to Codex go through `scripts/codex_run.py` ([codex-calls.md](codex-calls.md)); calls to a local model go to Ollama on `127.0.0.1` only.

## 0. Who may do what

The table of [../notes/TERMS.md](../notes/TERMS.md) is the rule. In one line: **training notes are written by Codex, by a local model or by code, and are kept, blanked or dropped by code and a local model; Claude writes and relabels test notes, and measures.** The loader enforces it (section 7).

## 1. Freeze the label scheme

[../notes/SCHEME.md](../notes/SCHEME.md), version 1.0, is written. It is **frozen** in phase A, step 1, by these runs (5 in all; say so first):

1. **30 new guide-test items** (one run), on every part of the guide changed since the test of 13 November: hard cases 27 to 36, note f, the rules of section 8, section 3b (the plaintiff's role), the public-body list, the edges of the eight slots. A fresh Sonnet 5.5 agent writes them with their expected answers ([../prompts/notes-guide-items.md](../prompts/notes-guide-items.md), `{{count}}` = 30, `{{sections}}` = that list); it is given the guide and never `guide-test.json`. `notes guide-items --ingest <reply>` appends them (ids 46 to 75).
2. **The guide test** (two runs; [../prompts/notes-guide-test.md](../prompts/notes-guide-test.md)): Sonnet 5.5 and Opus 5.5, fresh agents, no tool, each answer all 75 items from the guide alone. `notes guide-test --prepare` fills the prompt; `notes guide-test --compare <Sonnet's reply> <Opus's reply>` appends a dated section to [../notes/GUIDE-TEST-RESULTS.md](../notes/GUIDE-TEST-RESULTS.md).
3. **The cells table** of SCHEME.md, section 8 (two runs; [../prompts/notes-cells.md](../prompts/notes-cells.md)): the same two models answer the 75 cell items; `notes cells --prepare`, then `notes cells --write-table`.

Every item where a tester and the expected answer differ is a guide defect to fix before freezing, with one exception: a tester's misreading of a clear rule, and only when **the other tester gave the expected answer and named the same rule**. The scheme is not frozen while an item is unexplained (HANDBOOK, phase A decision table). An item that is still answered two ways after one fix of the guide is written into SCHEME.md's list of known gaps, with both answers, and goes into the owner's question batch (a labelling convention) or into `reports/open-legal-questions.md` (a reading of the law: never sent to the owner, standing rule of 15 November 2026): it then counts as explained. The cell items are outside this rule. **The freeze is two commits:** the freeze commit (the guide, the scheme with its cells table and hashes), then a stamp commit that writes the freeze commit's id into SCHEME.md's status line; a note's `schemeVersion` is `1.0@<the freeze commit>`. After the freeze, a change to the guide means: bump its version, rerun the items it touches plus six new ones, and say so.

## 2. The pilot (500 notes) and the calendar (owner stop point)

Run the whole training route on 500 pilot scenarios before trusting it. **25 Codex calls, plus at most 5 more for scenarios that come back to the queue**, the pool at 2 writers for the first 8 calls, 3 for the next 8, 4 for the last 9. Measure and report in `reports/pilot.md`:

- **Codex's pace:** for each pool size, seconds per call, calls accepted, rejected for tool use, otherwise invalid, refused for a usage limit; **calls per usage window**. This is the calendar's input.
- **The writer's sag within a call:** quality by position in the batch of 20 (length, code-check failures, filter drops for notes 1 to 5 against 16 to 20). If the last notes sag, the batch size goes down to 15 for the volume.
- **The code checks:** notes rejected, by reason.
- **The filter trial** (section 4, "The filter trial"): its table for both local models, and seconds per note.
- **The filter on all 500:** kept, kept with blanks, dropped; the blank rate per slot and per polarity; the decoy rate.
- **The noise measurement** (section 5): the share of wrong labels among kept notes, with its interval.
- Near-copies and leakage ([probes.md](probes.md), sections 1, 2a, 2b, 2c and 2e).
- **The feasibility of the notes on this laptop, as one paragraph:** the measured seconds per note of the local filter and (from the clean-writer trial, if it has run) of local writing; the nights each needs; and a plain yes or no to "can the plan's notes be made here by 10 December?". The estimates before any measure: the filter 25 to 30 seconds a note, about 7 nights for every note it must read (pilot 500, trial 400, training about 5,100, clean 1,300, phrases 4,500 short ones); local writing about 50 seconds a note; a clean-only replacement of the 4,000 Codex notes would take about 12 more nights. **If the filter alone would need more than 8 nights, that is a question for the owner at this stop point** (a shorter sheet, or the faster model).

This list is the pilot report's contents; HANDBOOK phase A points here.

From these, **the calendar**: days, Codex calls, Claude runs and laptop nights to 4,000 kept training notes, the dev and challenge sets, the clean notes. **Report it to the owner and wait.** The owner decides the pace, and may take any of these ways to spend less: a dev set of 400; a mirror set of about 330 kept notes.

**The pilot notes** keep `set: "pilot"` and are never a test set. They join the training notes only if nothing they were made with changed after the pilot (the same scheme version, prompt versions and filter); otherwise they are never trained on. The report says which.

## 3. Writing

`notes write --set <name> --calls <n> [--writer codex|sonnet]`: takes 20 scenarios of the set per call (never two of one family in a call, except the members of one polarity group, which are always written together; mixed claim types and style cards, so that a call's notes do not resemble each other), builds the prompt [../prompts/notes-write.md](../prompts/notes-write.md) with the labelling guide inside it, and

- **for Codex:** hands the calls to the writer pool of [codex-calls.md](codex-calls.md) (2 to 4 at once, each in its own empty scratch folder, with the output schema `prompts/schemas/notes-write.schema.json`);
- **for Sonnet** (its half of dev and of the challenge set only): writes one prompt file per call; the executor launches a fresh no-tool Sonnet 5.5 agent on each; `records.py ingest-reply` copies the reply and checks it against the same schema.

The writer returns, per scenario, the four fields of the note, then a list of the scenario's facts, each with **one to three quotes**: the exact words of the note that carry it. Each fact is named by a reference:

| Reference | The fact |
|---|---|
| `claim` | the claim type (for an `outside` scenario: the telling fact the scenario names) |
| `role:<claim>` | the plaintiff's role, only for a claim type that offers a choice and a scenario that fixes it |
| `event:<claim>/<kind>` | an event that is not `notMentioned`: the words that carry its polarity (for `disputed`, the contest, and the assertion when it is written) |
| `event-date:<claim>/<kind>`, `event-amount:<claim>/<kind>` | the event's date, its amount |
| `contest-amount:<claim>/<kind>` | the other side's figure, for an event whose amount is contested |
| `slot:<id>` | a slot whose value is not `unknown` |
| `request:<kind>`, `request-amount:<kind>` | a request, its amount |
| `value:<what>` | an extra value of the scenario (`<what>` as the scenario gives it) |

The writer adds no date and no amount that the scenario does not hold. It may leave out (`"expressed": false`) only a fact the scenario marks `optional` ([scenario-sampler.md](scenario-sampler.md)). Writing first and quoting after, in one answer, is deliberate: a writer that must point at its own words notices when the note does not say what the scenario says.

**The labels of a written note are the scenario's labels plus the writer's quotes; the writer chooses no label.** They are stored as `provenance.writerLabels`; the note's `gold` is written only after the check of section 4 (for a training note: the same labels less the blanked fields). The script `writer-labels` (with a test on a fixture of every reference kind) turns them into the `labels` of [../notes/note.schema.json](../notes/note.schema.json), field for field:

| `note.schema.json` | From |
|---|---|
| `claims`, `outsideKind` | the scenario's claims (or `["outside"]`) and its `outsideKind` |
| `claimEvidence` | the quotes of `claim` |
| `roles` | one entry per scenario claim: the scenario's role, or the claim type's only role, or `unknown`; `evidence`: the quotes of `role:` |
| `events` | one entry for every element of every scenario claim. `claim`, `kind`, `polarity`, `actor`, `disputedBy`: from the scenario. `evidence`: the quotes of `event:`; none for `notMentioned`. `date`: `{ kind: date, at: <first quote of event-date:>, normalized: <the scenario's date> }`; `amount`: likewise from `event-amount:`, with `amountDisputed: true` when the scenario's event has a contest of its amount |
| `slots` | one entry for every `readFromNote` slot: the scenario's value (`unknown` when it has none); `evidence`: the quotes of `slot:`; none for `unknown` |
| `requests` | one entry per scenario request: `kind`; `evidence`: the quotes of `request:`; `amount` from `request-amount:` as for an event |
| `values` | one entry **per place a value is written**: each quote of an `event-date:`, `event-amount:`, `request-amount:`, `value:` reference, or of a `slot:` reference whose slot holds a date, gives `{ kind, at: <that quote>, normalized: <the scenario's value> }`; quotes of the same words merge into one entry; `roles` = the event kinds, request kinds and slot ids of the references that quote it (empty for a `value:` reference); `amountDisputed` as for the event. A `contest-amount:` quote gives its own entry, with the event kind as its role and `amountDisputed: true` |

The scenarios hold only Gregorian dates and dinar amounts, so `relative`, `calendar` and `currency` never come from a writer.

`build-spans` then finds every quote in the note (letter for letter; `occurrence` when repeated) and writes `start` and `length`.

**The code checks**, on every note, before any model reads it. A note that fails one is **invalid**: its scenario goes back to the queue once (to be written in a later call), then is dropped and counted by reason. **A scenario has one retry in all** (`attempts` in the queue), whether its first attempt was lost to a rejected call or to a failed check.

1. Every quote is found; every scenario fact is quoted or declared `"expressed": false` where that is allowed.
2. **Every date and amount:** each quoted value parses, with the plain-code parsers of the reader ([reader.md](reader.md), rung 2), to the scenario's normal form; and the parsers find in the note **no date and no amount that the scenario does not hold**. (This checks that a value is there, not what it is the date of: the sheet asks that, section 4a.)
3. Formal Arabic only: no Latin letter at all; none of these Darija words as a whole word: راني، راه، راهي، كاين، كاينة، ماكانش، مكانش، بزاف، واش، علاش، كيفاش، شحال، وين، تاع، نتاع، ديال، باش، برك، درك، ضرك، هاذ، هاذي، هاذو، بصح، خاطر، قاع، كيما، نحب، حاب، خدام، دراهم. The list is extended only before the generation method is frozen.
4. Length within the style card's range, in words over the four fields: very short 15 to 60; short 60 to 150; medium 150 to 300; long 300 to 500.
5. No sentence copied from the labelling guide or the prompts (an exact match of any 6-word window).
6. No name from the cast lists other than this scenario's own (a full name, compared after the level-2 normalisation).

**A rejected Codex call: the one rule of [codex-calls.md](codex-calls.md).** Its answer is discarded: not parsed, not stored. The same prompt is never sent again automatically. Its scenarios go back to the queue **once**, to be sent in a later call built afresh from the queue; a scenario whose second call is rejected too is set aside and reported, never queued a third time. A rejection for tool use is counted apart. **A usage-limit refusal** is not a rejection: it stops the pool at once, its scenarios stay in the queue (not written, never dropped), and the next batch resumes from there; never loop on a refusal. **Two rejections in a row from one writer of the pool stop that writer**; the others go on, and the owner is told.

## 4. Checking a note

### 4a. Training notes: the local filter

After the code checks, a **local model** reads each note and answers a closed sheet ([../prompts/notes-filter-local.md](../prompts/notes-filter-local.md)): one call per note, on loopback, temperature 0, the answer constrained to the schema `prompts/schemas/filter-sheet.schema.json` (a list of letters, one per question). The filter never sees the scenario's answers, never writes a label, and stores nothing in the note but its model, its version and the note's status.

`notes filter-sheet` builds the sheet from the scenario, in Arabic, by code:

| Question | Options | Asked for |
|---|---|---|
| Is the note about this kind of claim? (yes or no) | yes; no | once for each claim type of the scenario (right answer: yes), and once for each of **three distractor claim types drawn by seed from other areas** (right answer: no). **An `outside` scenario gets no claim question**: for it the code check of the telling fact's quote stands alone, and the report says so |
| Which side is the plaintiff on? | the claim type's roles; "the note does not say" | each claim type of the scenario that offers a choice of role |
| What does the note say about «the element's Arabic proposition»? | it happened; it did not happen; the parties disagree about it; the note does not mention it | every element of the scenario's claim types, `notMentioned` ones included |
| the same question | the same | **two decoy elements**: an event kind that none of the scenario's claim types has (nor, for a near-miss, the claim type it resembles). Right answer: not mentioned |
| the slot's own question | yes; no; the note does not say (or the slot's choices and "the note does not say") | every `readFromNote` slot of type yes/no or choice that is asked for the claim type |
| What is this date the date of? What is this amount the amount of? | the roles that value could have in this scenario (its events that take such a value, its requests, its date slots), at most ten; "none of these" | each date and amount of the scenario that has a role |
| Does the note ask for «the request's Arabic label»? | yes; no | every request of the scenario, and **one decoy request**: a kind that has an Arabic label and is in none of those claim types' request lists |

The Arabic wordings are fixed in the prompt's header. A sheet has about 18 to 25 questions. **Not on the sheet:** each event's actor and who disputes it. They are never filtered; the noise measurement covers them, and the report says so.

Whether a date or an amount is in the note at all is not on the sheet: code check 2 already decided that.

`notes filter --set <name>` runs the model and applies this rule, in order:

1. **The answer does not fit the sheet** (wrong length, not the schema, a letter that is not among its question's options): the call is made once more. Again: the note is dropped, reason `filterInvalid`.
2. **A decoy is answered as present** (a decoy element not answered "not mentioned", the decoy request or a distractor claim type answered "yes"): the model's answers on this note are not trusted. The sheet is rebuilt with other decoys and asked once more. Again: the note is dropped, reason `filterUnsure`.
3. **A claim type of the scenario is answered "no":** the note is dropped (the owner's rule of 12 November: a disagreement on the claim type drops the note).
4. **Each other field** where the model's answer is not the scenario's label is **blanked** (section 5). If more than a fifth of the note's counted fields are blanked, the note is dropped.
5. Otherwise the note is `kept` or `keptWithBlanks`, with `provenance.filter = { family: "local", model, version: <the sheet's version and the model's digest>, date }`.

The filter resumes from its queue; it runs at night on mains and on free days, never while a timing or a training runs.

### The filter trial

Which local model filters is decided by measurement, on 100 pilot notes that passed the code checks, before any note is filtered for real. For each candidate (Gemma 4 E4B: `gemma4:e4b`; Jais-3-3.7B: `jais3-3.7b-low`):

1. **The 100 notes as they are.** Run the filter rule. Count the notes dropped and the fields blanked.
2. **The same 100 notes with one planted contradiction each.** Code picks one fact of the note that is `stated`, `denied` or `disputed` (or a slot with a known value, or a request) and **deletes from the note every sentence that holds one of the writer's quotes for it**. A sentence is the stretch between two of these marks: a full stop, «؛», «؟», «!», a line break; in a note written as dashes or numbered points, each point is a sentence. **A fact is planted only when none of its sentences holds a quote of another fact.** The note now does not mention that fact while its label still says it does: a contradiction made without any model.
   - **A plant counts only where the filter answered that field as the label says on the unaltered copy** (step 1): otherwise the field was already in doubt. At least 60 counted plants are needed per model; with fewer, more pilot notes are planted.
   - A counted plant is **caught** when the filter's final answer for that field is not the label, or when rules 1 to 3 drop the planted note.
3. Seconds per note, on mains, Best performance, idle CPU.

| Measure | Bar |
|---|---|
| counted plants caught | at least 80 % |
| unaltered notes dropped (an upper bound on good notes wrongly rejected: some of these notes are truly faulty) | at most 15 of 100 |
| decoys answered "not mentioned" or "no" | reported; under 95 % is a warning in the report |

**The winner** is the model that passes both bars and catches more plants; within 3 plants of each other, the faster one. The trial's table goes into the pilot report. If neither passes: HANDBOOK, phase A decision table (`filterWeak`, and the question goes to the owner); "the better one" is then the model that caught the larger share of its counted plants.

The 100 trial notes with plants are never trained on (the unaltered copies are ordinary pilot notes). Do not tune the sheet on the notes the noise measurement reads.

### 4b. Test notes: the blind relabel

`notes relabel --set <name>`: a fresh no-tool agent of **the other model** is given **the notes' text only** (no scenario, no writer labels, no style card) and the labelling guide ([../prompts/notes-judge.md](../prompts/notes-judge.md)), 25 notes per run:

| The note was written by | It is relabelled by | Proof label |
|---|---|---|
| Codex | Sonnet 5.5 | `twoFamilies` |
| a local model (the clean dev and test splits) | Sonnet 5.5 | `twoFamilies` |
| Sonnet 5.5 | Opus 5.5 | `oneFamily` |

The relabeller returns the full labels of `note.schema.json` with quotes, and may answer `unclear` for a field. **A relabeller uses no tool** except one read of its prompt file: a run with any other tool use is discarded and run again by a fresh agent. A run that does not return is run again whole; a reply that leaves notes out sends those notes back to the queue. `build-spans` adds the positions.

`notes compare`: the scenario's labels (which are the writer's) against the relabel, field by field:

| Field | Agreement means |
|---|---|
| claim types | the same set (and the same `outsideKind`) |
| the plaintiff's role, per claim | the same pair, or both `unknown` |
| each event's polarity | the same value; `unclear` is a disagreement |
| actor, disputedBy | the same, when both gave one (an `unknown` actor is not one) |
| an event's date, amount | the same normalised value, and the same `amountDisputed` |
| each slot | the same value |
| each value | matched by overlapping quotes: the same normalised value, the same `amountDisputed`, and at least one common role; a value one side missed is a disagreement on that value |
| requests | the same set of kinds; for each, the same amount |
| quotes | not compared for equality; each must be valid, and the two for one field must overlap or lie in the same sentence |

## 5. The disagreement rule (owner's decision, 12 November 2026)

The owner's words: "Blank the field, keep the note, in training notes only, with three limits": drop the whole note if they disagree on the claim type or on more than about a fifth of its fields; report the rate of blanked fields per slot and per polarity; write targeted notes to refill those cases. "In every test set (development, the mirror test, hidden v3), drop the whole note on any disagreement."

**Training notes only:** a field on which the scenario and the filter disagree is **blanked** and the note is kept (`status: keptWithBlanks`). In the schema's terms:

- an element's polarity: `"polarity": "blanked"`, path `events/<claim>/<kind>/polarity`; its actor, date and amount are left out with it;
- a slot: `"value": "blanked"`, no evidence, path `slots/<id>`;
- the plaintiff's role: the entry left out of `roles`, path `roles/<claim>`;
- what a value is for: the value's `roles` emptied of the disputed role, path `values/<field>:<start>/roles`;
- a request the filter did not find: left out of `requests`, path `requests/<kind>`;
- a value whose evidence code could not find (clean and composed notes): left out of `values`, path `values/<field>:<start>`.

Every path goes into `blanked`. The training loader computes no loss for a blanked field; it is never read as `notMentioned` or `unknown`.

1. **Drop the note** if the disagreement is on the **claim type**, or on **more than a fifth of the note's fields** (the script uses 20 %), counted, above and below the line, over the fields that **at least one side** gives something other than `notMentioned` or `unknown`. A field is: the claim; the role of a claim type with a choice; each element; each `readFromNote` slot on the sheet; each request; each date or amount with a role. Counting the always-`unknown` slots and the always-`notMentioned` elements would let a note keep most of its hard content blank.
2. **Report the rate of blanked fields per slot and per polarity** (stated, denied, disputed, by the scenario's label). Blanking is not neutral: if `disputed` is blanked five times as often as `stated`, the reader loses exactly the cases it most needs.
3. **Refill.** Targeted scenarios refill the cells that blanking and dropping emptied: every element × polarity cell and every slot × value cell is brought back to its target count, the cells with the highest rates first. A cell still short after a round is refilled in the next batch; the report lists every cell still short.

**Every test set** (dev, challenge, the clean dev and test splits, and the mirror set and hidden v3 by their commands): **any disagreement, and any `unclear`, drops the whole note.** Dropped test notes are refilled in the same way.

### The noise measurement (Opus measures; nothing changes)

The local filter is a small model. What it lets through is measured, on a random **100 of every 1,000 kept training notes** (and 100 of the pilot's kept notes), in 5 Opus runs per 100:

1. **Four runs of 25:** a fresh no-tool Opus 5.5 agent relabels the notes blind ([../prompts/notes-noise-audit.md](../prompts/notes-noise-audit.md): the relabeller's prompt and answer shape).
2. `notes compare` lists the fields where Opus and the note's kept labels differ (blanked fields are not compared).
3. **One sorting run** ([../prompts/notes-sort-dropped.md](../prompts/notes-sort-dropped.md)): a fresh Opus agent sees, for each differing field, the note, the kept label and Opus's label, and says which is right.

**The label noise, defined so that two people compute the same number.**

- **A field** is one of: the claim-type set (one field per note); the role of a claim type with a choice; per element, its polarity; and, each as its own field whenever either side gives one, its actor, who disputes it, its date, its amount; each `readFromNote` slot; each request kind either side gave; each request's amount; each value (its normal form and its roles together).
- **Compared fields:** those fields that either side gives something other than `notMentioned` or `unknown`, and that are not blanked. A field Opus answered `unclear` is not compared; such fields are counted apart. When the claim-type sets differ, the note counts as one differing field (the claim) and its events are not compared.
- **A wrong kept label:** a compared field on which the two sides differ and the sorter's `right` is `checker` or `neither`. `bothReadingsHold`, and every field whose cause is `guide`, are counted apart as unsettled. One sorting run per 40 differing fields.
- **The label noise** = wrong kept labels over compared fields. **The owner's 10 % bar is on this figure.** Beside it, always: the share of the 100 notes with at least one wrong kept label, with its Wilson interval (fields of one note are not independent, so the per-field interval is too narrow: say so); the count of unsettled and of `unclear` fields; the three most common kinds.

| Noise | Do |
|---|---|
| over 10 % | **Stop point** (the owner's bar). Show the figure, the per-note share and the kinds. |
| 5 % to 10 % | Go on. For the most common kind, change the sheet (a clearer question, one more option), bump its version, and use it **from the next notes on**; say so in the next report. Notes kept under the earlier sheet stay; each note records its sheet's version. The change is never tried on the 100 measured notes: its effect is measured on a fresh 100 of the next 1,000. |
| under 5 % | Go on. |

**The measured notes are neither corrected nor dropped because of what Opus found** (TERMS.md): Opus labels nothing that is trained on. Its answers are kept beside the report, never in a note.

### The sorting of dropped notes

The dropped and blanked notes are kept apart, not deleted. Opus 5.5 sorts a random 150 to 200 of them ([../prompts/notes-sort-dropped.md](../prompts/notes-sort-dropped.md), about 5 runs), drawn from the pilot, train, dev and challenge sets only (never the mirror set, never hidden v3), into: the writer's fault, the checker's fault (the local filter for a training note, the relabeller for a test note), an unclear note, **an unclear guide**. The last class goes back into the guide. Report the four shares. If the checker is at fault in more than about a quarter, the sheet, the relabel prompt or the checker is the problem: fix before generating more. Like the noise measurement, the sorting changes no training note.

**Who may read a training note, and what may follow.** A training note (pilot, train, clean train, composed) is read by: the local filter; the executor, when a decision table tells it to read failures in order to fix code or a prompt; the noise measurement's relabeller and sorter; the sorter of blanked fields. Nobody else: **no realism review and no other reading by a Claude model runs on a training set.** None of these readers changes, labels, keeps or drops a note. A dropped note is in no set, so sorting it is not a reading of training data.

**What may change the generation, and on what evidence** (the owner's decision of 15 November 2026: [../notes/TERMS.md](../notes/TERMS.md)): the writer prompt, the style cards, the sampler and the filter's sheet are changed only on evidence from notes written by Codex or by a local model (training and pilot notes, the Codex halves, the clean notes). A finding on a Sonnet-written note goes into the report and changes none of them. **A defect of the guide is fixed whatever note showed it**: the guide defines the labels of every set; it is not the reader.

## 6. The two halves of dev and of the challenge set

The first handbook had a "cross-check sample" with the roles reversed. The two halves do its job, on the sets that count.

- **Dev: 600 kept notes, 300 written by Codex and 300 by Sonnet 5.5**, from scenarios drawn by the same recipe and the same seed stream, then split by scenario family. **Challenge: 50 per difficulty, 25 and 25.**
- Every score is reported **per writer** as well as in all: the Codex half, the Sonnet half, and the gap.
- **Why two halves.** The training notes are Codex's. A reader that scores much better on the Codex half has learned Codex's manner. The mirror set and hidden v3 are written by Sonnet, so the Sonnet half is the early warning for them.
- **The Codex half tunes; the Sonnet half only measures** (owner, 14 November 2026). Nothing that changes the reader is chosen by looking at a Sonnet-written note's score: section 7.

## 7. Sets, splits and the loader

| Set (`set`) | Kept size | Writer | Checked by | Trained on | May tune | Measures |
|---|---:|---|---|---|---|---|
| `pilot` | 500 written | Codex | code, local filter | only under section 2's rule | no | – |
| `train` | 4,000 (first batch) | Codex | code, local filter | yes | no | – |
| `composed` | at most a quarter of any training mix | code, from phrases a local model wrote | code | yes | no | – |
| `clean`, split `train` | 700 | the better local writer | code, local filter | yes | no | – |
| `clean`, split `dev` | 100 | the same | code, local filter, Sonnet relabel | no | **yes** | yes |
| `clean`, split `test` | 200 | the same | the same | no | no | yes |
| `dev`, Codex half | 300 | Codex | code, Sonnet relabel | no | **yes** | yes |
| `dev`, Sonnet half | 300 | Sonnet | code, Opus relabel | no | **never** | yes |
| `challenge`, Codex half | 25 per difficulty | Codex | code, Sonnet relabel | no | **yes** | yes |
| `challenge`, Sonnet half | 25 per difficulty | Sonnet | code, Opus relabel | no | **never** | yes |
| a slot pack (`provenance.pack`) | about 400 train, about 100 dev | as train and as dev | as train and as dev | its train part | its Codex dev half | its dev part |
| `probe` | about 150 | code (composed) | code | never | no | the relabeller only ([hidden-v3-commands.md](hidden-v3-commands.md)) |
| `mirror` | about 440 | Sonnet (isolated) | Opus relabel; a third judge | never | **never** | once |
| `hiddenV3` | about 1,100 | Sonnet (isolated) | Opus relabel | never | **never** | once, by the owner |

- Scenario counts and how each set is drawn: [scenario-sampler.md](scenario-sampler.md). **Polarity families** (groups of 3 to 4 notes that differ in one element's polarity) sit **inside** the sets: about 200 groups in train (Codex), 50 in dev and 50 in challenge, half of each written by Codex and half by Sonnet. A group is written in one call by one writer and never split between sides. A group that loses a member (an invalid note, a filter drop, a relabel disagreement) is dropped whole and refilled whole. Two members of a group never go to the same relabel run: side by side, they would give the polarity away.
- **Each set is done when:**

  | Set | Done when |
  |---|---|
  | `train` | 4,000 kept; every drawn element × polarity cell and every slot × value cell has at least 60 kept notes. The cells are counted at 3,000 kept, so that the refills fall inside the 4,000; **nothing beyond 4,000 is written before the owner has seen the curve** |
  | `dev` | 300 kept in each half; at least 10 per claim type in each half; `outside` between 9 and 15 % of each half |
  | `challenge` | 25 kept per difficulty in each half |
  | `clean` | 700, 100 and 200 kept in its three splits |
  | a test set's refills | at most three rounds (the first and two refills); a cell still short is reported with its count, never filled by relaxing a rule |
- A split is made **by writing call and by scenario family**, never by note.
- **The test sets** are exactly: dev; challenge; the clean test split; the app's own cases; the mirror set (scored once); hidden v3. **The headline** of a report is the worst of those with at least 200 notes; smaller sets are reported beside it with their intervals.
- **The app's own cases** are `tests/fact-cases/cases.json` (61 labelled cases of the Alpha-01 work; the two of its `robustness` partition are in Darija and are reported apart) and the formal-Arabic drafting cases among `tests/drafting-cases*` and `src/DzLegal.Web/Samples` (16 files; `02-debt-french-notes.json` and `03-rent-darija-notes.json` are left out). They are scored with what their labels allow and reported beside every result. They are too few to carry a verdict. They only measure.
- Every test note's id is in `notes/TEST-IDS.txt` (ids only).
- `notes stats [--by-writer]`: per set, the counts per claim type, per element × polarity, per slot value, per style parameter; the length distribution; the writers, filters and relabellers.
- **The queue** (`notes-work\queue.jsonl`, format in [../STATE-FORMAT.md](../STATE-FORMAT.md)) holds every scenario's state, so that a stop loses nothing and repeats nothing. A note enters its set only once checked.

### The loader

Every script that reads notes for the reader goes through one loader (`NotesLoader`, in `DzLegal.CoreLogic.Notes`; the Python training code calls the tool `notes export --purpose <p>` and reads only what it writes). It takes a **purpose**, and refuses by rule:

| Purpose | Used for | Accepts | Refuses, with a test for each |
|---|---|---|---|
| `train` | computing a loss | `train`, `pilot` (when allowed), `composed`, `clean` split `train`, a pack's train part | a note with `trainingAllowed: false`; a note whose writer's family is `anthropic`; a note without a writer or without a filter in its provenance; an id of `TEST-IDS.txt`; a mix with more than 25 % composed notes; a split that cuts a family |
| `tune` | **every choice that changes the reader**: early stopping, the encoder choice, which rung or head is kept, temperature scaling, thresholds | the Codex half of `dev`, the Codex half of `challenge`, `clean` split `dev`, the Codex half of a pack's dev part | **any note whose writer's family is `anthropic`**; `mirror`; `hiddenV3`; a training note |
| `measure` | reporting a score | every open test set, per writer | `mirror`, unless the caller is the pre-commitment's scoring run (it passes `--mirror-run <n>`, a run number that the mirror's `RUNS.md` holds as started); `hiddenV3`, always |

Whatever the purpose, the loader refuses any path under `C:\dz-hidden*`, and, once v3 is finalised, any open set that has not passed `v3 probes --against` ([hidden-v3-commands.md](hidden-v3-commands.md)). `--exclude-writer <family>` removes a family's notes from a training mix (TERMS.md). **A slot pack's notes** (`provenance.pack`) are left out of `dev` and `train` unless the caller names the pack (`--pack <slot id>`); a pack's dev notes are scored for that slot only. For any note, a slot or output that the note's `schemeVersion` does not know is *unlabelled*: no loss, and not scored.

The training code has no other way to read a note. A script that opens a `.jsonl` set directly is a defect.

## 8. Clean notes (owner's decision, 12 November 2026)

Notes written by a **local open model** on this laptop: the replacement set if the Codex notes ever have to go.

1. **Trial:** 20 scenarios; Jais-3-3.7B and Gemma 4 E4B each write the 20 notes ([../prompts/notes-clean-write.md](../prompts/notes-clean-write.md)). Sonnet 5.5 and Opus 5.5 (fresh agents, no tool) each rank the two notes of every scenario blind ([../prompts/notes-clean-rank.md](../prompts/notes-clean-rank.md)). **The writer kept is "the better writer of formal legal Arabic"** (the owner's words), by the decision rule fixed in the ranking prompt's header and nowhere else: over the pairs where a ranker found both notes faithful, count for each writer the pairs where its note is the better formal legal Arabic; add the two rankers' counts; the writer with more is kept; equal counts keep Jais-3-3.7B. If the preferred writer's faithful rate is more than 20 points below the other's, tell the owner before writing the 1,000. The 40 trial notes are never trained on (Claude ranked them); run probe 2a ([probes.md](probes.md)) on them too. **The trial runs in phase B** (it is cheap, and the composed notes of section 9 need its winner).
2. **1,000 clean notes** from the winner, about 1,300 written (three nights). `set: clean`; `writer.family: local`. Split by scenario family: **700 `train`, 100 `dev`, 200 `test`**. **Their labels are the scenario's own**, and their evidence positions are found by code ([../prompts/notes-clean-write.md](../prompts/notes-clean-write.md)); a field whose evidence is not found is blanked, never kept as a label without words.
3. **Their filter.** The code checks, then the local filter of section 4a, run by **the local model that did not write them** when it passed the filter trial, otherwise by the trial's winner (say which). The `dev` and `test` splits are then relabelled blind by Sonnet under the test rule; the `train` split is never read by a Claude model except in one noise measurement (100 of its 700 notes).
4. **The comparison that decides shipping** ([reader.md](reader.md), rung 7), measured on dev, challenge and the clean test split, per writer (never on the mirror set): (a) the same reader trained on 700 clean notes and on 700 Codex notes (the first 700 of the nested order of [reader.md](reader.md), rung 4), three seeds; (b) the reader trained on the 700 clean notes against the best Codex-trained reader actually built. **"Close" is judged on (b), on the tune notes: within 2 points on each of four lines: claim type, outside recognised, polarity, and the slots' known values.** The 2 points are this handbook's working value, not the owner's; say so.
5. **If it is close, "clean only" means exactly this** (the owner's decision of 12 November plans the shipped reader on clean notes only): the reader trained on the 700 clean notes and on composed notes up to a quarter (their phrases are a local model's); on the **original** 8-bit encoder, never one whose top layers were trained on Codex notes; with its own thresholds file, export and parity run. More clean notes are written only if the owner asks, at about a night per 600. Both candidates then go to the mirror set.

Local models write worse notes than Codex. Expect a higher drop rate; that is the cost of the licence, measured.

## 9. Composed notes

Notes assembled by code from short Arabic phrases (per drawn element × polarity cell: 20 phrases, with places for names, dates and sums; sentence order and connectors drawn at random). About 4,500 one-event notes are written for the phrases (a night and a half of the laptop); **1,500 composed notes** are then made for training, and 150 more as probes. They are cheap and their labels are exact, but they are templates, and a reader can learn the template instead of the language.

**Who writes the phrases:** the local model that won the clean-notes trial, with the clean-note prompt, from one-event scenarios with the style "very short". Code keeps the sentence that carries the event as a phrase, with its names, dates and sums turned into places. A phrase is kept only if the **local filter** answers its one-event sheet as the scenario says; the 6-word-window check applies. The kept phrases are committed with the model's digest. No Claude model and no Codex call takes part.

So, exactly:

- every such note has `composedByCode: true` and `set: "composed"`; `writer.family` is `code`, and `writer.model` names the composer's commit and the local model with its digest;
- they are **at most a quarter of any training mix**, enforced by the loader;
- they are **never in a test set**; 150 are set apart as `probe` notes for the relabeller and are never trained on. **The probe pool is relabelled once, blind, by Sonnet 5.5** (the test-set route): a probe on which Sonnet differs from the composer's labels is dropped, so that a probe's label is exact by construction and confirmed by a second reader;
- the reader is trained and scored **with and without them**, at 1,000, 2,000 and 4,000 Codex-written notes: the "with" arm adds one composed note for every three Codex notes (333, 667, 1,333), which makes them a quarter of the mix; if the phrases are not ready when the curve is first drawn, their rows are added when they are. If composed notes lower the Codex-half score at two of the three points, they are dropped from the shipped recipe, and the report says so.

## 10. The learning curve (owner stop point)

Train the frozen-encoder reader ([reader.md](reader.md), rung 4) on 1,000, 2,000 and 4,000 training notes (nested subsets, three seeds each). Report in `reports/learning-curve.md`, **for the Codex half and for the Sonnet half of dev, and for the challenge set per difficulty**: each target of [scorer.md](scorer.md) with the spread over seeds; the slope from 2,000 to 4,000 on the Codex half; the style gap. **Stop and show the owner before generating more.** The handbook's phase B decision table says what the report recommends.

## 11. Freezing the generation method, then the mirror set

When the clean notes, the composed notes and the slot packs are done, **freeze the generation method**: the sampler's commit and seed, the style cards, the prompt versions, the scheme version, the filter's model and sheet version, the Codex CLI version, the roles. Record the commit id. Nothing of it changes afterwards.

Then **the mirror set**, immediately before hidden v3, with the same roles, prompts and commands ([hidden-v3-commands.md](hidden-v3-commands.md)), in an open folder (`C:\Users\TheShadow\dz-core-research\mirror`):

- **Size and proportions:** those of v3 at about 40 %: about 440 kept notes = 16 per claim type (336) + 80 near-miss + about 24 for the two rarest slots, in about 22 writer batches (about 330 if the owner takes that saving).
- **Writer:** isolated Sonnet 5.5 agents. **Relabeller:** isolated Opus 5.5 agents, with known-truth probes mixed in. **A third blind judge:** a fresh Sonnet 5.5 by the closed question-sheet route ([../prompts/notes-third-judge.md](../prompts/notes-third-judge.md)), for the label-noise figure ([probes.md](probes.md), section 4).
- **It chooses nothing.** It is scored **once, for the at most two final candidates**, just before the pre-commitment, to predict v3 and to give v3's result its error bars.
- The builder does not read mirror notes to improve the sampler or the prompts, and its dropped notes are not sorted into guide feedback.

## The commands of the notes tool

`notes` is the tool of `DzLegal.CoreLogic.Notes`. Every command takes `--work C:\Users\TheShadow\dz-core-research\notes-work` and is resumable from `queue.jsonl`, which **only this tool writes**.

| Command | Does |
|---|---|
| `notes sample --set <set> --count <n> --seed <n> [--half codex\|sonnet] [--difficulty <id>] [--cells <file>] [--pack <slot>]` | draws scenarios ([scenario-sampler.md](scenario-sampler.md)) into the queue, state `drawn` |
| `notes write --set <set> --calls <n> --writer codex\|sonnet` | builds the prompts; for Codex writes the pool's calls file (`calls-<date>.jsonl`), for Sonnet one prompt file per call |
| `notes ingest --call <id>` (Codex) / `notes ingest --reply <file> --call <id>` (Sonnet) | `writer-labels`, `build-spans`, the code checks; updates the queue |
| `notes filter-sheet --note <id>`; `notes filter --set <set> --model <ollama tag>` | builds a sheet; runs the filter with its five-step rule |
| `notes filter-trial --notes 100 --seed <n> --model <tag>` | section 4a's trial for one model: the unaltered run, the plants, the table |
| `notes relabel --set <set> --half codex\|sonnet --prepare` / `--collect <reply> --run <id>` | the relabel prompt files (25 notes each; never two members of a polarity group in one); copies an answer in |
| `notes compare --set <set>` | section 4b's comparison; keeps or drops |
| `notes noise --from <set> --sample 100 --seed <n> --prepare` / `--compare` / `--report` | the noise measurement's three steps |
| `notes sort --from <sets> --sample <n> --seed <n> --prepare` / `--collect <reply>` | the sorting of dropped and blanked fields |
| `notes guide-items --ingest <reply>`; `notes guide-test --prepare\|--compare`; `notes cells --prepare\|--write-table` | section 1 |
| `notes compose --count <n> --seed <n>` | composed notes from the kept phrases |
| `notes stats [--by-writer]` | section 7 |
| `notes export --purpose train\|tune\|measure --sets <sets> [--size <n> --subset-seed 7] [--exclude-writer <family>] [--pack <slot>] --out <folder>` | the loader's only door: writes `notes.jsonl` and `manifest.json` (the purpose, the ids, the counts per writer, the SHA-256). **Nested training subsets:** `--size` takes the first notes of one fixed order of writing calls (`--subset-seed 7`), whole calls and whole families, so that the 1,000 are inside the 2,000 and those inside the 4,000; pilot notes count when they are allowed |
| `notes import-app-cases` | the app's own cases as notes: `tests/fact-cases/cases.json` gives claim types and event polarities (`affirmed` is `stated`), scored on those only; a drafting case is scored on its claim type when its file names one, and otherwise serves the realism anchor only. Writer family `human` |
| `notes third-judge-sheets --set mirror`; `notes third-judge --compare <replies>` | [hidden-v3-commands.md](hidden-v3-commands.md), section 7b |
| `notes step5-sheets` | [step5-templates.md](step5-templates.md) |

## Decision table

| Situation | Do |
|---|---|
| A writer of the pool is rejected twice in a row | Stop that writer; the others go on; tell the owner at the next report. |
| A usage-limit refusal | Stop the pool; record the time and the calls made in the window; resume from the queue after the window the refusal names. |
| More than 30 % of a call's notes fail the code checks | Read ten failures. One cause explains more than half: fix the prompt once, bump its version, rerun two calls. Still over 30 %: stop point (HANDBOOK section 8). |
| The filter's decoy rate falls under 95 % on a batch | Report it. Under 90 %: stop filtering, rerun the filter trial on 50 fresh notes; if the model now fails a bar, switch to the other model if it passed, else the phase A row "neither passes". |
| A cell (element × polarity, slot × value) is under its share of 60 when 3,000 notes are kept | Targeted scenarios for it in the calls that lead to 4,000. A cell still under 60 at 4,000 is reported with its count in the curve report; nothing more is written before the owner's answer. |
| The relabel drops more than 35 % of a writer's test notes | Count the dropped fields by kind. One kind is more than half of them: if the guide is at fault, fix the guide (version bump) and relabel the test notes that rule touches; if a Codex-written batch shows a fault of the writer prompt, fix the prompt and write that batch's cells again from new scenarios. (A fault seen only on Sonnet-written notes changes no prompt: it is reported.) Otherwise go on; the rate is reported. |
| Two quotes for one field neither overlap nor lie in one sentence | A disagreement on that field. |
| You are about to choose something about the reader, the writer prompt, the style cards or the sampler from a score or a finding on a Sonnet-written note | Do not. Choose on the tune notes (the Codex halves of dev and of the challenge set, and the clean dev split); report the Sonnet-written figure beside it. |
| A Sonnet writer run or a relabel run used a tool other than its one read | Discard the run; a fresh agent runs it again. Twice for the same batch: read the prompt file for a path that invites opening; fix; version bump. |
| The laptop is on battery, or the owner needs it | The filter and the clean writer stop at the next note and resume from their queue. |
| A Claude model's output is about to enter a training note (a label, a fix, a keep-or-drop) | It must not. If it already did, mark those notes `trainingAllowed: false`, tell the owner in the next report, and rebuild the affected training mix. |

## Done when

1. The scheme is frozen with its commit; the guide test, rerun with the new items, has no unexplained item; the cells table is filled.
2. The pilot report and the calendar are with the owner, and the owner has answered.
3. Every kept note matches `note.schema.json`, has valid spans and a full provenance: a training note its writer and its filter; a test note its writer and its relabeller; the scheme version on all.
4. No note with `trainingAllowed: true` has a Claude model as writer, filter or relabeller (`notes stats --by-writer` shows it, and a test asserts it on the finished sets).
5. Each set meets its line of section 7's "done when" table. The reports exist: Codex's pace; drop and blank rates per slot and polarity; the refills and the cells still short; the filter trial; the noise measurements, one per 1,000; the four-way sorting of dropped notes; the per-writer scores and the style gap; the probes; the clean comparison; the learning curve with the composed rows.
6. The loader's refusal tests pass (every row of "The loader").
7. The generation method is frozen with its commit id before the mirror set is written.

## Traps

- **Agreement between two models is not truth.** That is why the guide is tested first, why the dropped notes are sorted, and why the mirror set has a third judge.
- **Do not let a checker see the scenario "to save a call".** The filter's sheet holds questions, never answers; the relabeller gets the text only.
- **Do not "fix" a training note with a Claude model**, however obvious the fix. Drop it by rule or leave it.
- **The mirror set chooses nothing.** Use the Codex halves of dev and of the challenge set.
- **The Sonnet halves, the mirror set and hidden v3 share a writer.** A reader chosen on the Sonnet half would be fitted to the hidden set's manner; that is why it only measures.
- **A usage-limit refusal mid-batch** leaves calls half done: the script must record their scenarios as not written, never as dropped.
