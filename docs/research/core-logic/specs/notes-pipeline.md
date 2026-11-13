# Specification: the notes pipeline (step 3)

From a scenario to a note that may be trained on or tested on. Every stage is a script with a resumable state; every note carries who wrote it and who judged it, so that the training notes can be replaced later if the terms of a provider require it (owner's decision, 12 November 2026).

```
scenario ──► WRITE (Codex, ~20 per call) ──► build-spans ──► JUDGE (Sonnet 5.5, text only, no tool) ──► compare ──► kept / kept with blanks / dropped
                                                                                                             │
                                                     cross-check sample: Claude writes, Codex judges ◄───────┘
```

**The order of step 3** (the owner's stop points in bold): **the terms report**; the guide completed, the guide test, the scheme frozen; the code (sampler, `build-spans`, `notes compare`, loader, probes); **the pilot**; development, challenge and polarity-family notes, and the cross-check sample; training notes to 4,000; **the learning curve** on dev and challenge; clean notes and composed notes; **the generation method frozen**; then the mirror set; then hidden v3 ([hidden-v3-commands.md](hidden-v3-commands.md)). The mirror set comes after the freeze so that nothing in the training material can have been shaped by a set written the way v3 is written.

**Where things are while a set is built.** The scenarios, the writers' answers and labels, the judges' answers and the queue live in `C:\Users\TheShadow\dz-core-research\notes-work\`, outside the repository, until their set is finished, so that nothing a judge could open holds them. A finished set is `notes-work\sets\<set>.jsonl`; the repository holds its counts, statistics and SHA-256, and holds the notes themselves only if the owner says so at the terms stop point (HANDBOOK, section 3).

## 0. Before anything is generated: the terms report (owner stop point)

Read, on the day, the current terms that govern (a) the owner's ChatGPT plan used through Codex CLI and (b) the Claude plan used through Claude Code: OpenAI's Terms of Use (and its Service Terms where they apply to the plan), Anthropic's Consumer Terms (or its Commercial Terms, if the plan is a business plan) and its Usage Policy, each from the provider's own site; for the clean track, the Gemma terms and the Jais 3 licence. The terms differ by plan, so **ask the owner which ChatGPT and Claude plans are used**; `codex login status` shows a login, not a plan. Look for the clauses on **ownership of outputs** and on **using outputs to develop or train models**.

Write `docs/research/core-logic/notes/TERMS.md`: each clause **quoted verbatim**, with its link and the date read, then one line per provider and per use: "cleared for training a classifier the app ships" or "research and testing only", with the sentence that decides it. Answer three uses separately, because the terms may treat them differently: (1) training on notes the provider's model **wrote**; (2) training on **labels** the provider's model gave to a note written by someone else; (3) using the provider's model only as a **filter** that accepts or rejects notes. Do not paraphrase a clause into a permission it does not give; if a clause is ambiguous, the line is "research and testing only".

**Stop. Report to the owner. Generate nothing until the owner answers.** Ask also whether the note sets may be committed to the repository (it is public). The answer sets `trainingAllowed` in every note's provenance: **true only when every use the note rests on is cleared**: the writer's family for use (1); the family of each judge whose agreement made the gold, for use (2); the family of a filter, for use (3). So if Anthropic is not cleared for use (2), the Codex notes judged by Sonnet train research models only, and the classifier the app ships is trained on clean notes (section 8), whose filter is then the local models.

## 1. Freeze the label scheme

Before any volume: the claim ids; the list of `readFromNote` slots; the labelling guide with its version; the boundary table from the final rule records; `note.schema.json`. Commit them under a tag in `notes/SCHEME.md` (the commit id is the scheme's version, recorded in every note). After the freeze, a change to the guide means: bump the version, re-judge the notes the change touches, and say so.

Run the **guide test** first ([../prompts/notes-guide-test.md](../prompts/notes-guide-test.md)): Sonnet 5.5 (a fresh agent, no tool) and Codex each label the items of `notes/guide-test.json` from the guide alone. The first run, on 13 November 2026, is in [../notes/GUIDE-TEST-RESULTS.md](../notes/GUIDE-TEST-RESULTS.md); the guide changed after it, so **the test is run again on the changed guide before the scheme is frozen**, and again after every later change. Each rerun has two parts:

- the existing items, each of which must still give its expected answer;
- about 20 **new** items on the sections that changed, written with their expected answers by a fresh Sonnet 5.5 agent that is given the guide but not `guide-test.json`, then added to the file. Rerunning only items the guide was fixed on would pass by construction and test nothing new.

Every item where a tester and the expected answer differ is a guide defect to fix before freezing, with one exception: a tester's misreading of a clear rule, and only when **the other tester gave the expected answer and named the same rule**. The scheme is not frozen while an item is unexplained.

## 2. The pilot (500 notes) and the calendar (owner stop point)

Run the whole pipeline on 500 pilot scenarios before trusting it. Measure and report:

- Codex calls accepted, rejected for tool use, otherwise invalid, and refused for a usage limit; calls per usage window; minutes per call;
- **the writer's sag within a call:** quality by position in the batch of 20 (length, drop rate for notes 1-5 against 16-20). If the last notes sag, lower the batch size;
- **the judge at 25 and at 40 notes per run:** half of the pilot notes, drawn at random, are judged in runs of 25, the other half in runs of 40 (fresh agents, the same prompt). Report, for each run size and for positions 1-10, 11-25 and 26-40: agreement with the writer per field, the `unclear` rate, invalid quotes, and notes missing from the reply. "No sag" means that no reply of 40 left a note out, and that the 40-note runs are not worse than the 25-note runs on any of these beyond their 95 % intervals;
- the drop rate, the blank rate per slot and per polarity, and the reasons (from the sorting of section 5);
- invalid quotes (words not found in the note) per role;
- near-copies and leakage ([probes.md](probes.md)).

From these, the calendar and the budget: how many days, agent runs and Codex calls the 4,000 training notes, the development sets, the mirror set and hidden v3 take at the measured rates. The estimate before the pilot (HANDBOOK, section 9) is **about 320 agent runs at 40 notes per judge run and about 405 at 25, and about 440 Codex calls**: 320 already assumes the first saving below. **Report it to the owner and wait.** At this stop point the owner decides, with the pilot's measured drop rates and Codex's measured calls per window, **which of the four ways to spend less are taken**:

1. 40 notes per judge run, only if the pilot shows no sag;
2. a development set of 400;
3. 100 realism reviews per set;
4. a mirror set of about 330 kept notes in v3's proportions (12 per claim type, 60 near-miss, 18 for the two rarest slots).

Bring, for each of the four, what it saves in runs and calls and what it costs in precision. Priority if the limit bites: hidden v3, the mirror set, development and challenge notes, the rule reviews of step 1, training volume last.

**The pilot notes** keep `set: "pilot"` and are never a test set. They may join the training notes only if nothing they were made with changed after the pilot (the same scheme version and the same prompt versions); otherwise they are never trained on. The report says which.

## 3. Writing

`notes write --set <name> --batch <n>`: takes 20 scenarios of the set (never two of one family in a call; mixed claim types and style cards, so that a call's notes do not resemble each other), builds the prompt [../prompts/notes-write.md](../prompts/notes-write.md) with the labelling guide inside it, calls Codex under [codex-calls.md](codex-calls.md) with an output schema, and stores the answer.

The writer returns, per scenario, the four fields of the note, then a list of the scenario's facts, each with **one to three quotes**: the exact words of the note that carry it. Each fact is named by a reference:

| Reference | The fact |
|---|---|
| `claim` | the claim type (for an `outside` scenario: the telling fact the scenario names) |
| `event:<claim>/<kind>` | an event that is not `notMentioned`: the words that carry its polarity (for `disputed`, the contest, and the assertion when it is written) |
| `event-date:<claim>/<kind>`, `event-amount:<claim>/<kind>` | the event's date, its amount |
| `slot:<id>` | a slot whose value is not `unknown` |
| `request:<kind>`, `request-amount:<kind>` | a request, its amount |
| `value:<what>` | an extra value of the scenario (`<what>` as the scenario gives it) |

The writer adds no date and no amount that the scenario does not hold. It may leave out (`"expressed": false`) only a fact the scenario marks `optional` ([scenario-sampler.md](scenario-sampler.md)). Writing first and quoting after, in one answer, is deliberate: a writer that must point at its own words notices when the note does not say what the scenario says.

**The writer's labels** are the scenario's labels plus the writer's quotes; the writer chooses no label. The script `writer-labels` (with a test on a fixture of every reference kind) turns them into the `labels` of [../notes/note.schema.json](../notes/note.schema.json), field for field:

| `note.schema.json` | From |
|---|---|
| `claims`, `outsideKind` | the scenario's claims (or `["outside"]`) and its `outsideKind` |
| `claimEvidence` | the quotes of `claim` |
| `events` | one entry for every element of every scenario claim. `claim`, `kind`, `polarity`, `actor`, `disputedBy`: from the scenario. `evidence`: the quotes of `event:`; none for `notMentioned`. `date`: `{ kind: date, at: <first quote of event-date:>, normalized: <the scenario's date> }`; `amount`: likewise from `event-amount:`, with `amountDisputed: true` when the scenario's event has a contest of its amount |
| `slots` | one entry for every `readFromNote` slot: the scenario's value (`unknown` when it has none); `evidence`: the quotes of `slot:`; none for `unknown` |
| `requests` | one entry per scenario request: `kind`; `evidence`: the quotes of `request:`; `amount` from `request-amount:` as for an event |
| `values` | one entry **per place a value is written**: each quote of an `event-date:`, `event-amount:`, `request-amount:`, `value:` reference, or of a `slot:` reference whose slot holds a date, gives `{ kind, at: <that quote>, normalized: <the scenario's value> }`; quotes of the same words merge into one entry; `roles` = the event kinds, request kinds and slot ids of the references that quote it (empty for a `value:` reference); `amountDisputed` as for the event |

The scenarios hold only Gregorian dates and dinar amounts, so `relative`, `calendar` and `currency` never come from a writer.

`build-spans` then finds every quote in the note (letter for letter; `occurrence` when repeated) and writes `start` and `length`. A note with a quote that is not found, a normalised value that does not parse, or a scenario fact neither quoted nor declared `"expressed": false` is **invalid**: it goes back to the queue once (rewritten in a later call), then is dropped.

**A rejected Codex call: the one rule of [codex-calls.md](codex-calls.md), for every role.** Its answer is discarded: not parsed, not stored. The same prompt is never sent again automatically. Its scenarios (or, for a judging call, its notes) go back to the queue **once**, to be sent in a later call built afresh from the queue; an item whose second call is rejected too is set aside and reported, never queued a third time. A rejection for tool use is counted apart from the other rejections: for an open set, fix the prompt before the next batch; for a hidden set, stop and tell the owner. **A usage-limit refusal** is recognised by the Codex process's failure (a non-zero exit, an `error` or `turn.failed` event, or no `turn.completed`) with a message about a limit, a quota or a rate in its output or log. It is not a rejection: it stops the batch at once, its scenarios stay where they are in the queue (not written, never dropped), and the next batch resumes from there; never loop on a refusal. **Two consecutive failed calls of any kind** (refused, rejected, timed out, or failed for a reason the script does not know) **stop the batch**; the script records where it stopped and why, and the executor reads the reason before the next batch starts.

Checks by code on every note, before any judge: formal Arabic only (no Latin letters at all; no Darija markers from a small list: راني، كاين، بزاف، واش، تاع، باش ...); length within the style card's range; no sentence copied from the labelling guide or the prompts (exact match of any 6-word window); no cast name outside the scenario's.

## 4. Judging

`notes judge --set <name>`: Sonnet 5.5 agents, each given **the notes' text only** (no scenario, no writer labels, no style card) and the labelling guide ([../prompts/notes-judge.md](../prompts/notes-judge.md)). 25 notes per run, or 40 if the owner takes that saving after the pilot. The judge returns the full labels of `note.schema.json` with quotes in its reply, and may answer `unclear` for a field.

**A judge uses no tool.** Its inputs are in its prompt and its answer is its reply. The executor reads the run's reported tool-use count: **a run with any tool use is discarded and run again by a fresh agent**, exactly as a Codex call that ran a command is discarded. A run that does not return is run again whole; a reply that leaves notes out sends those notes back to the queue. Its answer converts to `note.schema.json` as it stands, minus `n` and `remarks` (the remarks are kept beside the note for the guide's list of defects); `build-spans` adds the positions.

Two sides label each note: the writer (the scenario's labels, made by code, with the writer's quotes) and one blind judge of the other family. The brief asks for notes "labelled independently by at least two judges of different model families"; the owner's decisions of 12 November speak of "writer and judge", and this pipeline follows them. Stated here as a departure from the brief's wording.

`notes compare`: field by field, as section 9 of the guide says.

| Field | Agreement means |
|---|---|
| claim types | the same set (and the same `outsideKind`) |
| each event's polarity | the same value; `unclear` is a disagreement |
| actor, disputedBy | the same, when both gave one (an `unknown` actor is not one) |
| an event's date, amount | the same normalised value, and the same `amountDisputed` |
| each slot | the same value |
| each value | matched by overlapping quotes: the same normalised value, the same `amountDisputed`, and at least one common role; a value one side missed is a disagreement on that value |
| requests | the same set of kinds; for each, the same amount |
| quotes | not compared for equality; each must be valid, and the two for one field must overlap or lie in the same sentence |

## 5. The disagreement rule (owner's decision, 12 November 2026)

The owner's words: "Blank the field, keep the note, in training notes only, with three limits": drop the whole note if the judges disagree on the claim type or on more than about a fifth of its fields; report the rate of blanked fields per slot and per polarity; write targeted notes to refill those cases. "In every test set (development, the mirror test, hidden v3), drop the whole note on any disagreement."

**Training notes only:** a field on which writer and judge disagree is **blanked** and the note is kept (`status: keptWithBlanks`). In the schema's terms:

- an element's polarity: `"polarity": "blanked"`, path `events/<claim>/<kind>/polarity`;
- an element's actor, disputedBy, date or amount: the property is left out, path `events/<claim>/<kind>/<property>`;
- a slot: `"value": "blanked"`, no evidence, path `slots/<id>`;
- a request kind one side gave: left out of `requests`, path `requests/<kind>`; a request's amount: left out, path `requests/<kind>/amount`;
- a value: left out of `values`, path `values/<field>:<start>` (the position of the quote that holds it).

Every path goes into `blanked`. The training loader computes no loss for a blanked field; it is never read as `notMentioned` or `unknown`.

1. **Drop the note** if they disagree on the **claim type**, or on **more than a fifth of the note's fields** (the script uses 20 %), counted over the fields that at least one side labelled with something other than `notMentioned` or `unknown`. A field is: the claim; each element (its polarity with its actor, disputedBy, date and amount: one field); each `readFromNote` slot; each request kind either side gave; each value either side gave. Counting the always-`unknown` slots and the always-`notMentioned` elements would let a note keep most of its hard content blank.
2. **Report the rate of blanked fields per slot and per polarity** (stated, denied, disputed, by the writer's label). Blanking is not neutral: if `disputed` is blanked five times as often as `stated`, the classifier loses exactly the cases it most needs.
3. **Refill.** Targeted notes refill the cases that blanking and dropping removed: every slot × polarity cell (each element × polarity, each slot × value) is brought back to its target count, the cells with the highest rates first. The sampler draws scenarios of those cells, and they go through the same pipeline. A cell still short after a round is refilled again in the next batch; the report lists every cell still short.

**Every test set built here** (dev, challenge, and the mirror set and hidden v3 by their commands): **any disagreement drops the whole note.** A test label must be one two families agreed on. Dropped test notes are refilled in the same way.

**The dropped and blanked notes are kept apart, not deleted.** Opus 5.5 sorts a random 150 to 200 of them ([../prompts/notes-sort-dropped.md](../prompts/notes-sort-dropped.md)), drawn from the pilot, train, dev and challenge sets only (never the mirror set, never hidden v3), into: the writer's fault (the note does not say what the scenario says), the judge's fault, an unclear note, **an unclear guide**. The last class goes back into the guide. Report the four shares. If the judge is at fault in more than about a quarter, the judging prompt or the judge is the problem, and "agreement" is hiding it: fix before generating more.

## 6. The cross-check sample

For 300 scenarios, reverse the roles: a Sonnet 5.5 agent writes ([../prompts/notes-write.md](../prompts/notes-write.md), same prompt, no tool), Codex judges ([../prompts/notes-judge.md](../prompts/notes-judge.md)). Compare the two directions: drop rate, blank rate per polarity, and the classifier's score on each direction's notes. A classifier that scores much better on notes written by one family has learned that family's manner. Report the gap.

These notes are a measurement, not a set to learn from: they are never trained on, they take the test-set rule (any disagreement drops the note), and they are not one of the test sets of section 7. Their `set` value is `crossCheck` (to be added to the schema's list).

## 7. Sets, splits and the loader

- Sets and sizes: [scenario-sampler.md](scenario-sampler.md).
- A split is made **by writing call and by scenario family**, never by note: all notes of a call, of a family, of a polarity family are on one side.
- **The test sets** are exactly: dev; challenge; the clean test split (section 8); the app's own cases; the mirror set (scored once); hidden v3. **The headline** of a report is the worst of those with at least 200 notes; smaller sets are reported beside it with their intervals.
- **The app's own cases** are `tests/fact-cases/cases.json` (61 labelled cases of the Alpha-01 work: claim type and event polarities; the two of its `robustness` partition are in Darija and are reported apart) and the formal-Arabic drafting cases among `tests/drafting-cases*` and `src/DzLegal.Web/Samples` (16 files; `02-debt-french-notes.json` and `03-rent-darija-notes.json` are French or Darija variants and are left out). They are scored with what their labels allow and reported beside every result. They are too few to carry a verdict.
- Every test note's id is in `notes/TEST-IDS.txt` (ids only). **The training loader refuses to start if any id it is given is in that file**, and the test proves it. Hidden v3 ids are in no file outside the hidden folder; the protection of v3 is the no-open rule (HANDBOOK, section 3): no script and no agent opens that folder, and no folder is locked. The loader also refuses any path under `C:\dz-hidden*` and any note whose `set` is `hiddenV3` or `mirror`, and, once v3 is finalised, any open set that has not passed `v3 probes --against` (the near-copy check of hidden v3 against an open set, [hidden-v3-commands.md](hidden-v3-commands.md)); a test proves each refusal.
- `notes stats`: per set, the counts per claim type, per element × polarity, per slot value, per style parameter; the length distribution; the writers and judges.
- **The queue** (`notes-work\queue.jsonl`, format in [../STATE-FORMAT.md](../STATE-FORMAT.md)) holds every scenario's state, so that a stop loses nothing and repeats nothing. A note enters its set only once compared.

## 8. Clean notes (owner's decision, 12 November 2026)

Notes whose licence is beyond doubt, written by a **local open model** on this laptop.

1. **Trial:** 20 scenarios; Jais-3-3.7B and Gemma 4 E4B each write the 20 notes ([../prompts/notes-clean-write.md](../prompts/notes-clean-write.md)). Sonnet 5.5 (a fresh agent, no tool) and Codex each rank the two notes of every scenario blind ([../prompts/notes-clean-rank.md](../prompts/notes-clean-rank.md)). **The writer kept is "the better writer of formal legal Arabic"** (the owner's words): the one preferred for formal Arabic over the pairs where both notes are faithful. Faithfulness is reported beside it. If the preferred writer's faithful rate is more than 20 points below the other's, tell the owner before writing the 1,000. Run probe 2a ([probes.md](probes.md)) on the 40 trial notes too: a small model given a polarity word tends to copy it.
2. **1,000 clean notes** from the winner. Their `set` is `clean`; `writer.family` is `local`. **200 of them, chosen by scenario family, are the clean test split**: their ids go into `TEST-IDS.txt`, and they are never trained on. **Their labels are the scenario's own** (made by code), and their evidence positions are found by code; a field whose evidence is not found is **blanked**, in the schema's terms (section 5), never kept as a label without words. The blind judge is used only as a filter that drops a note whose text does not say what its scenario says, and nothing the judge wrote is stored in the note (details in the prompt). This keeps the clean set free of any provider's output; whether a provider's model may be that filter is use (3) of `TERMS.md`.
3. **The comparison that decides shipping**, on dev, challenge and the clean test split (never on the mirror set). Report both: (a) the same classifier trained on 800 clean notes and on 800 Codex notes, three seeds: the quality of the notes at equal size; (b) the classifier trained on the 800 clean notes against the best Codex-trained classifier actually built: what shipping clean would cost the lawyer. **"Close" is judged on (b): within 2 points on claim type and on polarity.** The 2 points are this handbook's working value, not the owner's; say so. If close: the plan is that the shipped classifier is trained on clean notes only, and the Codex notes stay for development, judging and testing (the owner's decision). Say which case holds and give the table.

Local models write worse notes than Codex. Expect a higher drop rate; that is the cost of the licence, measured.

## 9. Notes composed by code (owner's change 6)

A second source: notes assembled by code from short Arabic phrases (per element and polarity: 20 to 40 phrases, with slots for names, dates and sums; sentence order and connectors drawn at random). They are cheap and their labels are exact, but they are templates, and a classifier can learn the template instead of the language.

**Who writes the phrases.** Composed notes are not free of a provider's terms if a Claude or Codex model writes their phrases. So the phrases are written by **the local model that won the clean-notes trial**, with the clean-note prompt, from one-event scenarios with the style "very short": code keeps the sentence that carries the event (found as for clean notes, section 8) as a phrase, with its names, dates and sums turned into slots. They are filtered like clean notes: the filter labels each short note blind, and a phrase whose label differs is dropped; the 6-word-window check against the guide and the prompts applies. The kept phrases are committed with the model's digest, so the source is reproducible.

So, exactly:
- every such note has `composedByCode: true` and `set: "composed"`; `writer.family` is `code`, and `writer.model` names the composer's commit and the local model with its digest;
- they are **at most a quarter of any training set**, enforced by the loader (it refuses a mix above 25 %);
- they are **never in a test set** of any kind;
- the classifier is trained and scored **with and without them**, at 1,000, 2,000 and 4,000 training notes, on dev and challenge, and both rows are reported. Composed notes come after the learning curve (they need the clean trial's winner), so these rows are added to the curve's table when they exist. If composed notes do not help on dev and challenge, they are dropped, and the report says so.

## 10. The learning curve (owner stop point)

Train the frozen-encoder classifier of step 4 on 1,000, 2,000 and 4,000 training notes (nested subsets, three seeds each). Report, **for dev and challenge**: claim-type accuracy, polarity accuracy per polarity, each slot, with the spread over seeds; and the slope from 2,000 to 4,000. **Stop and show the owner before generating more.** A flat curve means more notes will not help and the effort goes to the method; a rising one gives the owner the price of the next points in days of Codex calls.

## 11. Freezing the generation method, then the mirror set

When the clean and composed notes are done, **freeze the generation method**: the sampler's commit and seed, the style cards, the prompt versions, the scheme version, the Codex CLI version, the roles. Record the commit id. Nothing of it changes afterwards.

Then **the mirror set**, immediately before hidden v3, with the same roles, prompts and commands ([hidden-v3-commands.md](hidden-v3-commands.md)), in an open folder (`C:\Users\TheShadow\dz-core-research\mirror`):

- **Size and proportions:** those of v3 at about 40 %: about 440 kept notes = 16 per claim type (336) + 80 near-miss + about 24 for the two rarest slots, in about 22 writer batches (about 330 in the same proportions if the owner takes that saving: 12 per claim type, 60 near-miss, 18 for the two rarest slots).
- **A third blind judge** (Opus 5.5, no tool, [../prompts/notes-judge.md](../prompts/notes-judge.md)) labels every kept note, for the label-noise figure ([probes.md](probes.md), section 4).
- **It chooses nothing**: not when to stop climbing the ladder, not whether composed notes stay, not clean against Codex-trained, not the learning curve, not thresholds, not prompts. All of those are decided on dev and challenge (and the clean test split where it applies). The mirror set is scored **once, for the at most two final candidates**, just before the pre-commitment, to predict v3 and to give v3's result its error bars.
- The builder does not read mirror notes to improve the sampler or the prompts, and its dropped notes are not sorted into guide feedback.

## Done when

1. `TERMS.md` exists and the owner has answered.
2. The scheme is frozen with a version; the guide test, rerun on the changed guide with new items, has no unexplained item.
3. The pilot report and the calendar are with the owner, and the owner has chosen among the four ways to spend less.
4. Every kept note of the Codex and Claude tracks matches `note.schema.json`, has valid spans, a writer and a judge of different families, and a scheme version. Every kept clean or composed note has the scenario's labels, passed its filter, and has spans or blanks.
5. The reports exist: drop and blank rates per slot and polarity; the refills and the cells still short; the four-way sorting of dropped notes; the pilot's judge at 25 and 40; the cross-check gap; the probes; the clean-notes comparison; the learning curve with the composed rows.
6. The loader's refusal tests pass (a test id; a hidden or mirror note; more than 25 % composed notes; a split that cuts a family; after v3 is finalised, an open set that has not passed `v3 probes --against`).
7. The generation method is frozen with its commit id before the mirror set is written.

## Traps

- **Agreement between two models is not truth.** Two families can share a misreading of the guide. That is why the guide is tested first, why the dropped notes are sorted, and why the mirror set has a third judge.
- **Do not let the judge see the scenario "to save a call",** and do not let it open a file. A judge that sees the intended labels confirms them.
- **The mirror set chooses nothing.** It is the rehearsal of v3; if its notes or scores are used to adjust anything, it predicts nothing. Use dev and challenge.
- **Sonnet 5.5 both judges the training notes and writes hidden v3.** That can flatter the v3 score slightly (the classifier learns what Sonnet accepts; v3 is what Sonnet writes). State it wherever a v3 score is given, and report beside it the scores on the app's own cases (section 7) and on the clean test split.
- **A usage-limit refusal mid-batch** leaves half a call: the script must record the scenarios as not written, never as dropped.
