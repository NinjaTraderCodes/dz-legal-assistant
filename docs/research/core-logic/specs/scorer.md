# Specification: the scorer

**Why it is written first.** A score is a definition. If the definition is written after the first model exists, it bends, without anyone meaning it to, towards what that model does well. So the scorer, its tests and the contents of `summary.md` are fixed and committed **before any model is trained** (step 4, item 1), and the scorer's hash is recorded with every result. Changing the scorer after a hidden run invalidates that run.

**The scorer's hash** is the SHA-256 of a list: the path and SHA-256 of each source file of the `DzLegal.CoreLogic.Scoring` project and of this specification, as committed (`git show <commit>:<path>`), sorted by path. A built program is not hashed: a build in another folder or with another SDK does not give the same bytes, and the step 6 rebuild must be able to check the hash.

**What it is.** Project `DzLegal.CoreLogic.Scoring` and the command `score --gold <notes.jsonl> --answers <readings.jsonl> --out <folder>`. It writes `summary.md` and `summary.json`. It reads nothing else and calls no model.

- **Gold:** one note per line, in the format of `notes/note.schema.json`, with the quotes' `start` and `length` written by script.
- **Answers:** one JSON object per line, `{ "id": "<note id>", "reading": <NoteReading> }`. The reading is the contract's `NoteReading` ([NoteReading.cs](../../../../research/core-logic/src/DzLegal.CoreLogic.Contracts/NoteReading.cs)), read with the contract's strict JSON options (`ContractJson.Options`: an unknown member, a wrongly cased name or an enum written as a number is an error). The line's wrapper is defined in the scorer's project.
- A note of the gold with no answer line is scored as a reader that abstained on everything, and counted (`answers missing`). An answer for an id that is not in the gold, or two answers for one id, is an error with a fixed code; the message never holds the id.
- **On a hidden set** the scorer refuses a gold file under `C:\dz-hidden*` unless it is given `--run <n>` and that run is recorded as `started` in `docs/research/core-logic/results/hidden-v3/RUNS.md` ([hidden-v3-commands.md](hidden-v3-commands.md), section 9). Every exception is caught at the top and printed as a fixed code.

## Two settings, always both

| Setting | The reader is given | What it measures | Notes scored |
|---|---|---|---|
| **Type given** | the note and the gold claim types (`confirmedClaimIds`) | events, slots, values and requests as the app will really use them: the lawyer confirms the claim type first | notes whose gold is inside the catalogue (an outside note has no type to give) |
| **End to end** | the note only | the claim type and outside; and everything else when the reader's own type is used | every note |

In "type given", the reader's `Claim` answer is not scored. In "end to end", for a gold element: if the reader answered that element's claim type, its event entry is scored (no entry is an abstention); if the reader abstained on the claim, the element counts as abstained; if the reader answered other types only, the element counts as wrong. Elements the reader gives for a claim type that is not in the gold are counted and reported as "elements of a wrong type".

**Each target has one headline setting:** "type given" for polarity, slots, dates and amounts, and requests; "end to end" for the claim type and outside. The other setting is always reported beside it.

## The measures

**Abstention first.** Every measure that allows abstention is reported three ways: accuracy on the answers given; **coverage** (the share answered); accuracy counting an abstention as wrong. A target is judged on the accuracy of the answers given, **and** requires the coverage floor committed before the hidden run. The floors proposed: 90 % for the claim type, 90 % for events, 90 % for slots; the executor may propose others from dev, never after seeing v3. Coverage is compared with its floor as a point estimate. Values are read by code and requests carry no abstention: their coverage is reported as "n/a".

**One rule for an abstention on an outside note, everywhere: it is not right, and it is counted apart.** "Outside recognised" is measured over every outside note; a note is right only when the reader answers `outside`. An abstention there is the safe failure (the app asks the lawyer), so the report gives its count separately from the notes called inside; but it is not counted as right, so a reader cannot abstain its way to the target. This measure therefore needs no coverage floor.

**Events are matched on (claim, kind).** A gold event is the pair (claim type, event kind); the reader's `EventReading` with the same `Claim` and `Kind` answers it. Several claim types share event kinds (`notice_served` occurs in eight), and in a note with two claim types one kind can carry two polarities, so the kind alone does not name an element.

| Measure | Over | An answer is right when | Target |
|---|---|---|---|
| **Claim type** | notes whose gold is inside the catalogue | the set of `Answers` equals the gold set | 95 % |
| **Outside recognised** | notes whose gold is `outside` | the answer is `outside` (an abstention is not right, and is counted apart) | 95 % |
| Outside, false alarms | notes inside the catalogue | (reported) the share answered `outside` | — |
| **Polarity, mentioned** | every gold element (claim, kind) whose gold polarity is stated, denied or disputed | the reader's polarity equals gold | 95 % |
| **Polarity, absent** | every gold element whose gold polarity is `notMentioned` | the reader says `NotMentioned` | 95 % |
| Polarity, per class | each of stated, denied, disputed | recall and precision, with intervals | reported; **the report flags any class whose rate is under 90 %** (point estimate) |
| Critical flips | mentioned elements | count of stated↔denied confusions, and of disputed read as stated | reported as counts |
| **Each slot** (`readFromNote`) | all notes; and separately the notes whose gold value is not `unknown` | the value equals gold (dates and digits after normalisation; `unknown` equals `unknown`); a slot missing from the reading is an abstention | 90 % on **both** |
| **Dates and amounts** | every gold value with a normalised form | a predicted value of the same kind overlaps its span, has the same normalised form (a date as `yyyy-mm-dd`, `yyyy-mm` or `yyyy`), and its roles share one with gold's (or both are empty) | 90 % (recall); precision reported |
| **Requests** | every (note, request kind) pair asked in gold **or** answered | the pair is both asked in gold and answered: an accuracy over pairs, a proportion | 90 % |
| Request amounts | requests asked and answered with a gold amount | same normalised amount | reported |
| Actor, disputedBy | mentioned elements where gold gives one | equal | reported |
| **Evidence** | every right answer that carries words | at least one predicted span overlaps a gold span of the same field, or lies in the same sentence (sentences end at `.`, `؟`, `?`, `!` and a line break) | reported; under 90 % is flagged: right answers for the wrong reason. A claim answer with no evidence is allowed and counts only here. |
| Calibration | claim answers; event answers | reliability table in 10 bins and the expected calibration error, on the answers given | reported |

Elements whose gold polarity is `blanked` (training notes only) are never scored; a test set has none.

## Three verdicts, and how they combine

For each line of a target, with the Wilson 95 % interval of its rate:

- **met:** the lower bound is at or above the target;
- **not met:** the upper bound is below the target;
- **inconclusive:** the target lies inside the interval. The report then says how many more notes would be needed to decide, and does **not** round up.

**A target is met only if every one of its lines is met, in its headline setting, and the coverage floor holds.** Below the floor its verdict is **"not met (coverage)"**, whatever its accuracy. Otherwise it is "not met" if any line is not met, and "inconclusive" in every other case. The lines of a target: polarity has two (mentioned, absent); each slot has two (all notes, known values); the others have one. A line with n = 0 is "not measurable", never 100 %, and makes its target inconclusive.

## What each size can prove

Each line has its own n, and with the Wilson interval at 95 % a small n cannot prove much. The table gives, for the sizes this work will have, the smallest count that is "met" and the largest that is "not met"; everything between is "inconclusive". (Computed with z = 1.96; checked by the scorer's test 4.)

| Set and line | n | Target | "Met" from | "Not met" up to |
|---|---:|---:|---|---|
| v3, claim type (in-catalogue notes) | about 900 | 95 % | 868 (96.4 %) | 842 (93.6 %) |
| v3, outside recognised | 200 | 95 % | **197 (98.5 %)** | 183 (91.5 %) |
| v3, one slot, known values | 100 | 90 % | **96 (96 %)** | 84 (84 %) |
| v3, one slot, all notes | about 1,100 | 90 % | 1,010 (91.8 %) | 970 (88.2 %) |
| v3, polarity, mentioned (or absent) | about 2,000 | 95 % | 1,920 (96.0 %) | 1,880 (94.0 %) |
| v3, requests (pairs), or dates and amounts | about 2,000 | 90 % | 1,827 (91.4 %) | 1,773 (88.7 %) |
| dev, claim type | 600 | 95 % | 581 (96.8 %) | 559 (93.2 %) |
| challenge, one difficulty | 50 | 95 % | never (50 of 50 has a lower bound of 92.9 %) | 44 (88 %) |
| clean test split | 200 | 95 % | 197 (98.5 %) | 183 (91.5 %) |
| mirror, claim type (in-catalogue notes) | about 360 | 95 % | 351 (97.5 %) | 333 (92.5 %) |
| mirror, outside recognised | 80 | 95 % | only 80 of 80 | 72 (90 %) |
| mirror, one slot, known values | 40 | 90 % | only 40 of 40 | 32 (80 %) |
| the app's own fact cases | 59 | 95 % | never | 52 (88.1 %) |

**Said plainly:** with 100 notes, a 90 % target is "met" only from 96 of 100; with 200 notes, a 95 % target only from 197 of 200. A slot read right 95 times in 100 is "inconclusive", not "met". So **several v3 targets (outside recognised and each slot's known-value line) will most likely come out "inconclusive" unless the reader is nearly perfect.** This goes to the owner in the step 4 report, before the run, so that "inconclusive" is not read as a failure. Rung 8 of [reader.md](reader.md) aims its thresholds with this table in mind.

## The test sets and the headline

The scorer is run on these test sets, and only these:

- **dev** and **challenge**;
- **the clean test split**: the 200 clean notes held out by scenario family and never trained on;
- **the app's own cases** (below);
- **the mirror set** (kept notes): **once**, for the at most two final candidates, at the pre-commitment;
- **hidden v3**: once, by the owner's command.

**The headline is the worst test set among those with at least 200 notes.** For each target, the headline verdict is that of the set (of 200 notes or more) whose interval has the lowest lower bound, named with its verdict. Smaller sets are reported beside it, each with its interval. Averages over sets are not reported.

**The app's own cases** are:
- `tests/fact-cases/cases.json`: 61 fictitious cases of the Alpha-01 work labelled with the claim type, the events' polarities and some dates and amounts. The two Darija cases of its robustness partition are left out (the notes are formal Arabic only): 59 are scored, for claim type, polarity of the labelled events, and the dates and amounts they give.
- The drafting cases in `tests/drafting-cases` and `tests/drafting-cases-more` (12 files) and `src/DzLegal.Web/Samples` (4 files): 16 files, of which `02-debt-french-notes.json` and `03-rent-darija-notes.json` are left out. Their labels are form fields only (for example `claim.concernsRegisteredRealProperty` for the slot `property_registered`); what those fields allow is scored, and nothing else.

They are scored with what their labels allow, reported beside every result, and too few to carry a verdict. They are written by people: if the reader does well on v3 and badly on them, believe them.

## `summary.md` for a hidden set: the exact contents

Fixed here, so that nothing else can leave the hidden folder. No note text, no quote, no note id, no cast name, no per-note line. **Every table merges cells under 3 into "other"**, so that no single note can be reconstructed.

1. Header: the set's name; the run number from `RUNS.md`; the number of notes; the scorer's hash; the reader's `ReaderInfo` (name, version, file hashes); the thresholds file's hash; the date; the command line.
2. The table of targets: measure, line, setting, n, rate, interval, coverage, verdict; for both settings, the headline setting marked.
3. Claim type: accuracy per claim type (21 rows and `outside`), and the confusion counts between types.
4. Polarity: the 4 × 4 confusion counts overall; recall per element (claim, kind) for elements with 20 instances or more.
5. Slots, values, requests: one row each.
6. Abstention: coverage per measure; abstentions on outside notes; accuracy of the answers the reader was least sure of (lowest decile).
7. Calibration tables (a bin with fewer than 3 answers is merged with its neighbour).
8. The probes' figures for the set: near-copies inside it, and the shallow model's accuracy by cross-validation inside it (written by `v3 probes` into the folder, never printed before the run).

`summary.json` holds the same numbers. Both are written inside the results folder; **the owner's command copies only `summary.md`** to where the builder may read it.

## Tests the scorer must have before any model exists

1. A perfect reader (gold turned into answers) scores 100 % everywhere; an empty reader scores 0 with full abstention counted.
2. One planted error of each kind moves exactly the measure it should and no other: a flipped polarity; a missed event; an invented event; an event answered under the wrong claim type in a two-claim note; a wrong slot; a right value with a wrong role; a right answer with a span elsewhere; a missed request; `outside` called inside; an abstention on an outside note.
3. Abstention: an abstained answer leaves the accuracy on answers unchanged, lowers coverage, and is wrong in the third figure; under the floor the verdict is "not met (coverage)".
4. The interval and the three verdicts on hand-computed cases, including n = 0 ("not measurable") and every row of the size table above.
5. `summary.md` produced from a set whose notes hold a marker string never contains the marker (the no-text guarantee is tested, not assumed).
6. The same inputs give byte-identical outputs (ordering is fixed; no timestamps inside the tables); dates in all three forms (`yyyy-mm-dd`, `yyyy-mm`, `yyyy`) are read.

## Traps

- **Do not score `notMentioned` together with the mentioned polarities in one accuracy.** Most elements of most notes are absent; a 4-class accuracy is dominated by the easy class and hides the denied and disputed errors. Two lines, both with a target.
- **A slot's accuracy over all notes is mostly `unknown` equals `unknown`.** That is why each slot has two lines.
- **Small cells identify notes.** Keep the merge rule in every table.
- **This scorer is not the Alpha-01 scorer.** Hidden v2 results from the old question sheet are comparable only through [v2-adapter.md](v2-adapter.md); never put a v2 figure and a v3 figure in one column.
