# The pilot report (phase A, stop point) — DRAFT, 17 November 2026

**Status of this draft.** Every measured section below is final. The **route experiment**, which the owner ordered on 17 November, still lacks its fine-tuned-encoder rows: that run needs a night on mains. The calendar's reader part waits for it (section 10). The contents follow `specs/notes-pipeline.md`, section 2, as changed by the owner's decisions of 16 and 17 November (HANDBOOK section 2: no local filter on every note; flagged notes are dropped, never judged by a Claude model; the route experiment).

## 1. What the pilot made

| | Count |
|---|---:|
| scenarios drawn (sampler 1, seed 1101, 10 polarity groups) | 500 |
| Codex writing calls (20 scenarios a call; the last for the retries) | 28, all accepted |
| notes written and passed by the code checks | 499 |
| dropped after two attempts (s-pilot-00207: a shared fact of two claims written as «بنفس المقابل والتاريخ», twice) | 1 |
| dropped by script for sampler 1's role defect (below), their scenarios drawn again and queued | 6 |
| **kept training notes (on the code checks, the owner's route of 16 November)** | **493** |

**Sampler 1's role defect.** A Sonnet writer of the dev set found it. Sampler 1 drew the role of a second claim type without regard to the plaintiff's sex, so a wife asking for khul and custody was given the custody role "father". It hit 6 of the pilot's 500 scenarios and 3 of dev's 100. The fix is sampler 1.1 (620b9cd), and the coherence test now checks it on 10,000 draws. On the owner's word, the notes were found by script from the scenario records alone and moved out of the kept notes, and their scenarios were drawn again by sampler 1.1 and queued (`notes refill`, 54acaef).

**Are the pilot notes trained on?** Yes, by the owner's decision of 16 November ("use them, as they passed the code checks"). By section 2's own rule they would not be: within the pilot the writing prompt changed (version 1 for calls 1 to 8, version 2 after), the sampler changed (1.1), and the filter changed (the local filter was dropped). Every score that uses them says so.

## 2. Codex's pace (the calendar's input)

| Pool | Calls | Minutes a call, median (range) | Accepted | Rejected for tool use | Otherwise invalid | Usage limit |
|---:|---:|---|---:|---:|---:|---:|
| 2 writers (calls 1 to 8) | 8 | 15.3 (11.6 to 17.6) | 8 | 0 | 0 | 0 |
| 3 writers (calls 9 to 16) | 8 | 13.9 (12.2 to 15.2) | 8 | 0 | 0 | 0 |
| 4 writers (calls 17 to 27) | 11 | 13.8 (9.7 to 18.3) | 11 | 0 | 0 | 0 |
| 1 writer (call 28, the retries) | 1 | 12.7 | 1 | 0 | 0 | 0 |
| 3 writers (dev, 3 calls) | 3 | – | 3 | 0 | 0 | 0 |

**Calls per usage window:** no limit was met in 31 calls over 15 and 16 November, so the window's ceiling is still unknown. **At 4 writers, a call took no longer than at 2.** Four in parallel give about 17 calls an hour: about 340 notes written an hour, or about 300 kept at the measured reject rate.

## 3. The code checks

| | Prompt version 1 (calls 1 to 8) | Prompt version 2 (calls 9 to 28) |
|---|---:|---:|
| notes asked | 160 | 371 |
| rejected at first ingest | 35 (22 %) | 42 (11 %) |

Rejections by reason, over the pilot: length 49, a value that does not parse to the scenario's (or an extra value) 46, a quote not found 20, another scenario's name 6, a six-word run copied from the guide or the prompt 2. A note can fail on more than one reason.

**Most "value" and "names" rejections were our side's,** and were fixed with tests, with the notes rechecked (`ingest --recheck`):
- the reader's parsers missed the Arabic thousands separator «٬», «واحدا», digits in brackets with spaces, «بـ» before a number, and same-day references («في اليوم نفسه»);
- the names check read «على» as the name «علي»;
- writers copied the «(optional)» mark into references.

Version 2 of the prompt (the length counted by code) halved the length failures.

**The writer's sag within a call:** none in the code checks. By position in the batch of 20, notes 1 to 5 were rejected 23 times in 140 (16 %), notes 6 to 15 38 in 261 (15 %), and notes 16 to 20 16 in 130 (12 %). The notes do get shorter after the first five: median 168 words for notes 1 to 5, against 100 and 95 for the rest. The batch stays at 20.

## 4. The filter trial, and the owner's decision

| Model | Answer constrained | Planted errors counted / caught | Unaltered notes dropped (bar: at most 15 of 100) | Decoys answered right | Seconds a note (median) | Passes |
|---|---|---|---:|---:|---:|---|
| Gemma 4 E4B (100 notes) | yes, per question | 81 / 44 (54 %) | **38** | 95.5 % | 43.1 | **no** |
| Jais-3-3.7B (10 notes, the owner's sample) | no (Ollama refuses its sampler) | 3 / 2 | **9 of 10** | 88.3 % | 53.2 | **no** |

**Conditions:** 31 of Gemma's 181 timed notes ran on battery. Between 20:19 and 21:06 on 16 November two trial processes ran at once, so the seconds of that hour are unreliable. Ollama is not fully deterministic: 3 of the 47 notes recorded twice differed. The trial now takes an exclusive lock (f8076b6).

**The owner's check of Gemma's drops** (`reports/filter-trial-opus-check.md`): of 89 disputed fields, Opus found Gemma right on 5 (6 %) and the writer on 79 (89 %). **Gemma is dropped as a filter for every field.** Before that, on 16 November, the owner had already decided that no local model checks every note; at 43 seconds a note that would have taken about 40 hours.

**So, the filter on all 500:** not run. The training notes are kept on the code checks. The reader will flag the notes it confidently disagrees with, and a flagged note is dropped and never judged by a Claude model.

## 5. The noise measurement

`reports/noise-pilot.md`. A random 100 of the 493 kept notes, relabelled blind by Opus, then sorted. **The label noise is 0.5 %**: 8 wrong kept labels in 1,474 compared fields. The notes with at least one wrong label are 7 of 100 (Wilson 95 %: 3 % to 14 %). 28 fields are unsettled because of gaps in the guide (section 9). **Under the owner's 10 % bar, and under 5 %: go on.**

The kinds of wrong label: element polarity 5, an element's amount 1, a request left out 1, a value's role 1. One kind recurs across both checks: a note saying the delivery took place, late or on time, whose label leaves `delivery_missing` not denied (case 13: 3 here, 2 in the Gemma check).

## 6. Near-copies and leakage (`specs/probes.md`; `train/probes.py`, 98fea24)

Run on the 493 pilot notes and the 54 kept dev notes.

| Probe | Result |
|---|---|
| 1. Near-copies (facts with dates, amounts and names replaced; Jaccard on character 5-grams; a run of 25 words) | **none**, and no pair even in the 0.60 to 0.80 band to look at |
| 2a. The claim type from word n-grams, cross-validated by family | 76.1 % (the most common class: 11.8 %). From character n-grams: 85.2 %. The top features are the claims' own words: that is the task |
| 2a. Polarity per element (15 kinds with 40 notes or more) | close to the most common class for every kind. The top features are words about the facts. No single verb carries a class (no «ينكر» shortcut) |
| 2b. The claim type from the style card | 8.5 %, under the most common class: no style leak |
| 2c. The claim type from length | 11.8 %, equal to the most common class |
| 2e. The writer, Codex or Sonnet, from the text (dev) | **90.7 %** (the most common class 55.6 %). Codex's habits: «شخصان»/«فردان» (its fixed way to state that no public body is a party), «بعد الظهر», «في مواجهة»; Sonnet's: «الموكل/الموكلة», «بتاريخ», «تذكير». Every claim type sampled in dev comes from both writers |

## 7. The development set (the owner's approval of 16 November)

| | Codex half | Sonnet half |
|---|---:|---:|
| scenarios (sampler 1, seed 1611, families alternating) | 50 | 50 |
| written and passed the code checks | 47 | 44 |
| blind relabel | Sonnet, 2 runs | Opus, 2 runs |
| **kept (every field agreed)** | **30** | **24** |

The test-set rule drops a note on any disagreement, so only **54 of 91** were kept. The differences: claim type 14, element 10, slots 6, requests 3, values 3, roles 2.
- Most claim-type differences come from one gap of the guide (section 9): a custody, maintenance or visitation request in a khul, tatliq or divorce note.
- A bug in the comparison counted the role of every one-role claim type (45 false differences). It was found and fixed (ee364e8), and the run repeated.
- The 3 role-defect dev notes had already been dropped by the relabel; they are queued again.
- **Format fixes on the way, each reported:**
  - the relabel schema was made to follow its own prompt and the guide: `occurrence`, evidence for notMentioned and unknown, `normalized` for a relative date, `actor` for a notMentioned element;
  - a value without a normal form is not compared;
  - a Sonnet writer's extra `warnings` field was stripped by script, its text kept apart.

## 8. The first reader and the route experiment

### 8a. The first reader (`reports/first-reader.md`)

Heads on the frozen 8-bit CAMeLBERT-mix, trained on the 493 pilot notes and scored on the 54 dev notes, three seeds.
- The spec's shared claim and request layer could not learn: it called every note `outside`. It was replaced by per-class weights.
- The batch and rate were chosen on the tune notes: 8 and 3e-3, with a tuning score of 0.531 against 0.337.

### 8b. The route experiment (the owner, 17 November)

**Done**, scored on the same dev notes. The tuning score is on the 30 Codex notes; the lines are type given, except claim type and outside, which are end to end.

Outside recognised: 7 of 7 Codex for every route; Sonnet 3 of 6 (heads, vectors) and **6 of 6 (n-grams)**. The two floors read a value's role from the words around it (a linear model), the heads from attention: that is why the floors' date and amount line is twice the heads'.

| Route | Tuning score | Claim type, Codex / Sonnet | Polarity (mentioned), Codex / Sonnet | Requests, Codex / Sonnet | Dates and amounts with role, Codex / Sonnet |
|---|---:|---|---|---|---|
| Heads on the frozen encoder (mean of 3 seeds) | 0.531 | 45 % / 26 % | 63 % / 53 % | 36 % / 24 % | 29 % / 31 % |
| Logistic regression on the encoder's vectors | 0.502 | 39 % / 28 % | 62 % / 56 % | 39 % / 25 % | 59 % / 61 % |
| **Word and character n-gram classifier** | **0.663** | **78 % / 78 %** | **69 % / 67 %** | **62.5 % / 47 %** | **69 % / 56 %** |
| Fine-tuned encoder (top two layers, rung 5) | *pending: a night on mains* | | | | |

**Dates and amounts with their role (type given, 150 gold values), on the heads reader, seed 33:**

| | Codex half | Sonnet half | Both | Contract-date slot | Notice-date slot |
|---|---:|---:|---:|---:|---:|
| the model alone | 24 of 78 | 23 of 72 | 47 (31 %) | 0 of 9 | 0 of 8 |
| the cue table and the parsers alone | 18 of 78 | 36 of 72 | 54 (36 %) | 6 of 9 | 6 of 8 |
| **hybrid: cues decide, the model fills the rest** | 28 of 78 | 44 of 72 | **72 (48 %)** | 6 of 9 | 6 of 8 |

**The same on the n-gram floor** (its value roles read from the words around each value):

| | Codex half | Sonnet half | Both | Contract-date slot | Notice-date slot |
|---|---:|---:|---:|---:|---:|
| the n-gram floor alone | 54 of 78 | 40 of 72 | 94 (63 %) | 0 of 9 | 8 of 8 |
| the cue table and the parsers alone | 18 of 78 | 36 of 72 | 54 (36 %) | 6 of 9 | 6 of 8 |
| **hybrid** | 52 of 78 | 50 of 72 | **102 (68 %)** | 6 of 9 | 7 of 8 |

**What the done part already says:** the plain n-gram classifier, trained on the same 493 notes, beats the heads on every line and shows **no writer gap on the claim type** (78 % and 78 %). The heads' gap (45 % against 26 %) is what the owner read as the reader learning Codex's style. On this data the deep part does not yet earn its place: whatever the fine-tuning gives is compared with 0.663, not with 0.531.

**Pending, one night on mains:** the fine-tuned encoder, two settings × three seeds, scored after the 8-bit export, the cache rebuild and the head refit (`exec\route_finetune.sh`). Then the hybrid applied to the winner, and the choice of route.

## 9. The guide's gaps found in the pilot

Each is a place where two careful readers of the guide label one note differently. The handbook's rule: fix the guide, bump its version, rerun the guide test on the items it touches. **Proposed rules, not yet applied:**
1. **Side requests in a family note.** A custody, maintenance, housing or visitation request asked alongside a divorce, khul or tatliq: section 3 ("each claim that has its own request") adds a claim type, but the catalogue lists these requests under the dissolution. *Proposal:* such a request belongs to the dissolution and adds no claim type unless the note gives it facts of its own (who holds the child, a divorce judgment). The sampler's labels already follow this; the guide must say it.
2. **A remedy request.** Rescission asked in a sale-non-delivery note. *Proposal:* the same rule: a request that is a remedy of a labelled claim type adds none.
3. **The same fact written twice.** Writer and relabeller quote different places, and the comparison's quote rule (overlap or the same sentence) counts a disagreement on agreed labels. *Proposal:* any place that carries the same label is a valid quote. The comparison checks only that each quote is valid, and the labels.
4. **An amount given by reference** («المبلغ نفسه»، «بالمقدار المبين أعلاه»). *Proposal:* it takes the figure it refers to, as the same-day rule does for dates.
5. **An amount written only in a request.** Is it also the unpaid event's amount? *Proposal:* no. An event's amount comes from the words that state the event.
6. **A request that presupposes the unpaid event.** *Proposal:* a request states no event, unless its own words say the sum is due («الباقي»، «المتأخرة»).
7. **"The whole price is still owed"** («ما زال الثمن في ذمته»). Does it deny `payment_made`? *Proposal:* only when the note says the whole sum remains or nothing was paid.
8. **A party who admits part of an event** (part of the goods, part of the premises). *Proposal:* the event is stated, and the contest is one of extent, like a contest of amount (case 5).
9. **The request kinds have no definitions** (restitution or other). *Proposal:* one line for each kind.

## 10. Feasibility on this laptop, and the calendar

**Feasibility.** The local filter is no longer on the route, so the laptop's nights go to training only.
- Encoding all 4,000 notes with the 8-bit encoder takes about 20 minutes (the pilot's 493 took 2 minutes 13 seconds).
- A frozen-encoder or n-gram training run takes minutes.
- A fine-tuning run is about one hour, to be measured in the route experiment.
- The clean notes, written by a local model, were not measured. The plan's 1,300 at about 50 seconds a note need about 18 hours, two or three nights.

**Can the plan's notes be made here by 10 December?** Yes for the Codex notes and the training. The clean notes add two or three nights that the pilot did not measure.

**Calendar** (from the measured pace; the reader's part waits for the route).

| Work | Calls and runs | Time |
|---|---|---|
| 4,000 training notes, sampler 1.1 with wider style (the owner, 17 November: more style cards, several Codex prompt variants, shorter and messier notes) | about 225 Codex calls, plus about 10 % retries | about 15 hours of Codex at 4 writers: 2 to 3 days |
| the retries and refills already queued (3 + 6 pilot + 3 dev; 6 Sonnet) | 1 Codex call, 1 Sonnet run, 2 relabel runs | half a day |
| development set to 600 kept, at the measured 59 % keep rate (the owner may take 400) | about 1,000 written: 25 Codex calls, 25 Sonnet runs, about 40 relabel runs (half Sonnet, half Opus) | 3 to 4 days |
| challenge set, 12 difficulties × 50, the same keep rate | about 1,000 written: 25 Codex calls, 25 Sonnet runs, about 40 relabel runs | 3 to 4 days |
| the guide's nine gaps, the guide test on the touched items | 2 runs (Sonnet, Opus) | half a day, before the volume |
| the noise measurement per 1,000 kept training notes | 4 relabel runs and 1 sorting run of Opus each | with the volume |
| the learning curve at 1,000, 2,000 and 4,000 (the next stop point) | local | two evenings |

**The keep rate of the test sets is the costly line.** With the guide's gaps 1 to 3 fixed, most of today's dev drops would not happen. The rate is measured again after the fix, before the volume.

## 11. Choices made without the owner, for review

- The 100 quote and source fields stripped by script (owner-approved, 15 November).
- The decoy count scored on the first sheet; the token budget grows with the sheet; each answer is constrained to its own question's letters (filter-sheet-1.1); a trial lock.
- The same-day date rule (guide version 1.2, checks); the parsers' gaps; the names check.
- `notes accept`: the pilot kept on the code checks, with the code checks named as its filter.
- The heads' design changed from the spec (per-class weights); batch and rate chosen on the tune notes; `train/test_train.py` skipped in the repository's pytest (no torch there), run under Part 7's environment.
- The relabel schema's four relaxations (section 7), each reading a left-out field as the comparison already did.
- The noise sorting in one run of 41 fields instead of two (40 and 1).
- check-values: «اليومين» alone in a message is not a period (the owner, 17 November).
- check-values read «اليومين» as a period before that and cost two follow-up runs in the rules track.

## 12. Spending of phase A's notes track

| | Count |
|---|---:|
| Codex calls | 31 (28 pilot, 3 dev), all accepted, no usage limit |
| Claude runs | 13: Opus 8 (the Gemma check 1, the dev relabel 2, the noise relabel 4, the noise sorting 1), Sonnet 5 (the dev writers 3, the dev relabel 2) |
| laptop | the filter trial about 2.5 hours (part on battery); training and floors under an hour; the fine-tuning still to run |
| paid service | none ($0) |

**The question at this stop point (after the route experiment):** go on to 4,000 training notes, with the route the experiment chooses, the wider style, and the guide's gaps fixed first?
