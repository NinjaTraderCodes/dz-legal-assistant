# Specification: how a slot added later becomes a reader output without retraining from zero

**The problem.** The reader comes first now (owner's decision, 14 November 2026), so its label scheme is frozen before most rule records exist. The records will add slots: `formal-notice` alone added nine. The first handbook assumed one freeze after all records were final. This file says how the scheme grows without throwing away the notes, the encoder or the scores.

## The three rules of growth

1. **The scheme only grows.** `notes/SCHEME.md` has a version: 1.0, then 1.1, 1.2 … Each minor version **adds** outputs (a slot, an element, a request kind, a value role). It never changes what an existing label means, never removes one, never renames one.
2. **A change of meaning is a major version** (2.0) and a stop point for the owner: it would force relabelling and retraining.
3. **Every note records the scheme version it was labelled under** (`provenance.schemeVersion`). A note labelled under 1.0 says nothing about a slot added in 1.1: for that slot it is *unlabelled*, which is not the same as `unknown`.

## Step 1: a new slot starts as a question

The day a record is final, its new slots are in `slots.json` with `readFromNote: false` ([unit-driver.md](unit-driver.md), stage 9). The app asks the lawyer. The rules work. **Nothing about the reader changes, and nothing waits for it.**

## Step 2: decide, in phase D, whether the reader should read it

For each slot with `readFromNote: false` whose `origin` is a final record, apply this table. The first row that fits decides.

| The slot | Decision | Why |
|---|---|---|
| is filled by something the reader or the app already has: an event (its polarity or its date), another slot with the petition date, or the claim type | **Derived**: set `derivedFrom`; no head | nothing new has to be read |
| is a fact lawyers do not usually write in a first note (a contract clause's exact wording, a fact about the other party's internal state, a procedural detail learned later) | **Asked**: stays a question | the reader would abstain almost always |
| can be asked in one short question and is needed by fewer than two claim types | **Asked** | a question costs the lawyer seconds; a head costs a note pack and a night |
| is the kind of fact a note states plainly, and a rule that warns or blocks reads it, for two claim types or more (counted by `records.py slot-use`: the claim records whose flattened items with a warn or block effect test the slot) | **Read**: a new reader output | this is where the reader saves the lawyer work |
| none of the above | **Asked**, and listed in the phase report | the cheap choice, reversible later |

Write the decisions in `notes/SCHEME.md` (the table "Slots added after 1.0"), one line per slot with the row that decided. The owner sees the table in the phase D report; an "Asked" can become "Read" in a later version at no cost.

**Derived slots.** `slots.json` gets `"derivedFrom": { "event": "<kind>" }`, `{ "slot": "<id>", "with": "petitionDate" }` or `{ "claim": true }`, and `notes/SCHEME.md` the mapping (which polarity gives `yes`, `no`, `unknown`; which value gives the date; which claim types give which choice). `readFromNote` stays false and the scheme's version does not change: the slot is computed in plain code, by the reader when it comes from an event and by the app's own code otherwise. A test checks each mapping on five made-up readings. (`fromEvent`, which some slots already carry, keeps its old meaning, the event a slot's question is about; it does not mark a derived slot.)

## Step 3: for a slot that is to be read

Do these in order. Nothing here touches the encoder.

| # | Do | Check |
|---|---|---|
| 1 | Add the slot's row to the labelling guide, section 6: what makes it `yes`, `no`, `unknown`; what does **not** count; two worked sentences for each value. Bump the guide's version. | – |
| 2 | Six new guide-test items for it ([../prompts/notes-guide-items.md](../prompts/notes-guide-items.md), `{{count}}` = 6), run by Sonnet and Opus ([../prompts/notes-guide-test.md](../prompts/notes-guide-test.md)). | Both testers agree with the key on all six, or the guide is fixed and the items rerun |
| 3 | Add the slot to the sampler: the claim types it is drawn for (the record's `askedFor`), its values' shares (a third each unless the record says a value is rare), and which other facts must hold for a value to make sense. | The sampler's tests |
| 4 | **The slot pack.** About 400 training notes whose scenarios fix the slot (Codex writes; the code checks and the local filter decide, as for any training note), of which at most a quarter composed by code; and about 160 dev notes, 80 by Codex and 80 by Sonnet, relabelled blind as dev notes are. State the calls before launching: about 25 Codex calls and 12 Claude runs per slot; several slots share packs when one scenario can fix them together. **A date slot:** the pack fixes whether the note gives the date and how it is written. | A yes/no or choice slot: at least 60 kept training notes for each value, and at least 15 kept dev notes for each value in each half. A date slot: at least 150 kept training notes with the date and 100 without, and 40 kept dev notes with the date in each half; its score is the share with the exact normal form |
| 5 | Set the scheme to the next minor version (the slot enters `notes/SCHEME.md`, section 10, as "Read"); commit. `readFromNote` stays false until row 8 has passed. | `SlotCatalogueTests` |
| 6 | **Train the head** on the stored encoder outputs ([reader.md](reader.md), "A new head"). Only notes whose `schemeVersion` knows the slot carry a loss for it. Minutes on the laptop. | The head's score on the Codex half of the slot's dev notes, with its interval |
| 7 | **Prove nothing else moved.** Export the heads file; run the reader on the whole of dev with the old and the new heads file. | Every output that existed before is identical, value and confidence; the test `OlderOutputsUnchanged` |
| 8 | Fit the new head's own temperature and threshold on the Codex half of its dev pack (no clean note knows a later slot). Then set `readFromNote: true` in `slots.json`. | Coverage at least 90 % at the slot's target; otherwise the slot stays `readFromNote: false` (the app keeps asking), its head ships switched off, and the report says "read, below target" |

## Why older outputs cannot move

- The encoder file (`reader-encoder.onnx`, about 105 MB) and the heads file (`reader-heads.onnx`, a few megabytes) are separate. A new head adds weights to the heads file only.
- Every head reads the encoder's outputs and nothing from another head. Adding one changes no input of the others.
- `ReaderInfo.Files` holds the SHA-256 of both files: after a new head the encoder's hash is the same and the heads' hash changes. The reader's version goes from `1.0.x` to `1.1.0`.
- The contract does not change: a slot reading is already a generic `{ slot, value, confidence, abstained, evidence }` ([../INTEGRATION-CONTRACT.md](../INTEGRATION-CONTRACT.md)).

**If the encoder itself was fine-tuned** (reader.md, rung 5), every head was trained on that fine-tuned encoder, and a later head is trained on it too, frozen. The encoder is never fine-tuned again for a new slot. If a later, larger change needs the encoder retrained, that is "training beyond the plan": a stop point.

## The stored encoder outputs

`<work>\reader\cache\<encoder hash>\<note id>.npy`: the 8-bit encoder's token vectors for each note (768 values per token, 16-bit floats), written once by the .NET tokenizer's ids through ONNX Runtime ([reader.md](reader.md), rung 4). A new pack's notes are added to the cache; nothing is recomputed. The cache is keyed by the encoder's hash, so a changed encoder can never be mixed with old vectors.

## What a slot pack must not do

- It never enters a test set that already has scores: the dev notes of a pack form their own slice (`dev`, `pack: <slot id>`), scored for that slot only.
- It is not written to make the head look good: the scenarios come from the sampler with the slot's values drawn independently of the note's style card, like every other note.
- Notes of the mirror set and hidden v3 are written under the scheme version that is frozen when the generation method is frozen (phase E). A slot read after that date is not in v3, and the report says which slots v3 does not measure.

## Decision table

| Situation | Do |
|---|---|
| Two final records propose the same fact under two ids | The verifier of the later record maps its id to the earlier one. One slot. |
| A read slot's head is under its target on the Codex half of its dev pack | `readFromNote` stays false: the app keeps asking. Report the head's score and coverage. Do not write a second pack without telling the owner the cost. |
| A pack's dev half keeps fewer than 15 notes for a value after its relabel | One refill call for that value; still short: the slot's figures are reported with their counts and marked "too few to conclude". |
| The pack's notes rarely state the slot although the scenario fixed it (measured by code and by the local filter, never by reading: the filter answers "the note does not say" for more than half of the pack's first 40 notes) | The scenario line for that slot is unclear: fix the line, bump the prompt version, rewrite those 40 notes; if still over half, the slot becomes "asked". |
| A slot needs a value type the scheme lacks (a list, a number that is not an amount) | Stop point (HANDBOOK section 8): it changes the contract. |
| `OlderOutputsUnchanged` fails | A head read something outside the encoder's outputs, or the encoder file changed. Find which; never relax the test. |
