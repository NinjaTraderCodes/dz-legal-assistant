# Specification: the optional comparison on hidden set v2

**Status: optional, and an owner stop point.** Do not build or run any of this before the owner says yes to the question in section 3. If the owner says no, drop the comparison and write one line in the final report: "The new reader was not run on hidden v2."

## Why one would want it

Part 7 measured six generative models on hidden v2 with the Alpha-01 reader: Gemma 4 E4B 58 of 90 cases fully right, Jais-3-3.7B 50, down to 2. The new reader's v3 score is on a new scale. One number on the old scale ("the classifier-reader gets N of 90 on the same sheet") would tie the two together.

## The owner's condition (change 5 of 12 November)

The run counts **only if the answers are scored by the unchanged Alpha-01 scorer, through the owner's command.** Two shortcuts are therefore ruled out:

- **`facts score --predictions`** exists in the harness and scores a reader from outside .NET, but it scores by its own rules (sentence-numbered predictions), not by the Alpha-01 path. A figure from it is not comparable with the 58 and the 50.
- **Converting `NoteReading` to `CaseFacts` in new code** and calling the scorer directly would skip the frozen parser: the scorer would see facts that no model's answer could have produced through the sheet.

**Two departures remain, and both go to the owner (section 3):**
- **The adapter is new mapping code.** It turns the reader's answers into the sheet's lines. The scorer and the sheet's parser are unchanged, and the adapter's lines go through that parser exactly as a model's answer does; but the mapping itself is new code between the reader and the yardstick, tested by round trip on the open cases (section 1), not reviewed by anyone else.
- **The run does not go through the owner's frozen command.** `Run-OwnerAlpha01.ps1` accepts four model names only; the run uses Part 7's runner and its scratch harness instead (section 3).

## 1. The adapter: new mapping code, behind an unchanged sheet

The Alpha-01 reader (`ClaimFactReader`, `src/DzLegal.Drafting/ClaimFactReader.cs`) asks its model through the Ollama provider: `POST /api/chat` on a loopback address. The prompt holds the case as text, the dates and amounts the program found (numbered), and the elements and requests of the confirmed claim type (numbered). The model answers **one short line per element and per request**: stated, denied or disputed, and the numbers of the values that belong to it. `ClaimFactReader.Parse` reads those lines; the Alpha-01 scorer scores the result.

So the adapter is **a small loopback server that speaks Ollama's protocol and is, to the harness, just another model**. Project `research/core-logic/src/DzLegal.CoreLogic.V2Adapter`, tests in `research/core-logic/tests/DzLegal.CoreLogic.V2Adapter.Tests`.

| Request | The adapter answers |
|---|---|
| `GET /api/tags` | one model, named `dz-reader:<version>`, with a `digest` equal to the reader's hash (the SHA-256 of its files, as in the pre-commitment), so the harness's digest check pins the reader exactly |
| `POST /api/generate` with an empty prompt (load, unload, `keep_alive`) | the empty success reply Ollama gives |
| `POST /api/chat` | parses the prompt back into: the case text, the numbered values, the numbered elements and requests; calls `INoteReader.Read(note, [claim])` in process; writes the answer **in the exact line format `ClaimFactReader.Parse` expects**; returns it in Ollama's reply shape, with token counts and durations filled with measured or zero values |
| anything else the frozen harness calls (`/api/ps`, `/api/show`) | the minimal valid reply; found by reading the frozen harness, not guessed |

Rules for the adapter:

- It binds to `127.0.0.1` only, on a free port; the harness's loopback guard must accept it unchanged.
- It **keeps nothing**: no log of prompts, no file. The prompt is hidden v2 text passing through; it lives in memory for one request. An exception is answered with a fixed error and no text.
- **The mapping, in full** (this is the new code the owner is asked about):
  - polarity: `Stated` → the sheet's word for stated; `Denied`; `Disputed`; `NotMentioned` → the sheet's "absent" answer;
  - an **abstained** event is written as absent (the sheet has no abstention), and the count of such cases is reported, because it makes the comparison slightly unfair to the new reader;
  - values: the new reader finds values by span; the sheet wants the **number** the program gave each value. A value is matched by normalised value and sentence; an unmatched value is left out, and counted;
  - the reader's event entries are taken for the claim type the sheet gives (`EventReading.Claim`).
- The claim type is **given** on this sheet (Alpha-01 "claims given"): the comparison says nothing about the classifier, only about the reader.
- **Tests, on the open cases only:** the prompt parser on the open Alpha-01 development cases (prompt in, structure out, for every case); the line writer by round trip: gold `CaseFacts` → lines → `ClaimFactReader.Parse` → equal; and the whole mapping by round trip: a reading built from an open case's gold → lines → parse → the same facts, with the abstention and unmatched-value counts at zero.

## 2. Rehearsal on open cases

Before any hidden run: run the adapter through the unchanged harness on the open development cases (`facts --alpha --claims given --models dz-reader:<version>` against the adapter's port). The harness must run to the end without a single change to it. The open cases have gold in the Alpha-01 format (`tests/fact-cases/cases.json`), so no new labelling is needed: the harness's result must equal, case by case and element by element, what the reader's own answers give when compared directly with that gold, except for the counted abstentions and unmatched values. A difference means the adapter distorts; fix it before thinking of v2.

## 3. The one question the owner must answer

The owner's frozen command (`Run-OwnerAlpha01.ps1`) accepts four model names only. A fifth name cannot pass through it. The Part 7 runs solved the same problem with:

- the runner `C:\Users\TheShadow\dz-arch-research\F\run\Run-ArabicModels.ps1` (copy on the branch `fable/architecture-review`: `docs/research/fable-part7/run/Run-ArabicModels.ps1`; reviewed by the reviewer on 8 to 11 November), written for Windows PowerShell 5.1;
- a scratch copy of the harness at commit `480d4a4` with **one added line in the model lock** (`FactRunMeasurement.ResolveDigest`: any other model runs only with its recorded 64-character digest). The same copy also carries Part 7's battery switch, which stays off unless the environment variable `DZLEGAL_PART7_POWER` is `battery`; this comparison runs on mains, with it off. The whole difference is `docs/research/fable-part7/run/harness-480d4a4.diff` on `fable/architecture-review`. **No line of the reader, the parser or the scorer differs.**

The comparison would use the same runner and the same scratch harness, with a `models.json` entry for `dz-reader:<version>`, its digest, and the adapter's endpoint.

**Ask the owner, in these words:** "The v2 comparison needs three things that are not your frozen command. (1) Part 7's runner instead of `Run-OwnerAlpha01.ps1`. (2) Its scratch harness, which differs from the frozen one by one added line in the model lock (and by Part 7's battery switch, left off). The scorer and the sheet's parser are unchanged. (3) An adapter that is new mapping code: it turns the reader's answers into the sheet's lines, writes an abstention as absent and drops a value it cannot match, and counts both; it is tested by round trip on the open cases. Do you accept these for this comparison, or do I drop the comparison?" Wait for the answer.

## 4. If the owner says yes

1. Commit the adapter, its tests, the reader's digest and the `models.json` entry; give the owner the commit id.
2. The owner starts the adapter and runs the runner on the hidden stage only (`powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\TheShadow\dz-arch-research\F\run\Run-ArabicModels.ps1 -RunRoot <new folder> -Stages "facts"`), on mains power with Best performance, as for Part 7's mains runs.
3. Only `summary.md` is read. Report: N of 90 fully right, with its 95 % interval, beside the six Part 7 figures, **in a table of its own** (never in a v3 table), with four notes: the claim type was given; abstentions were written as absent (count); unmatched values were dropped (count); time per case is not comparable (a classifier against generative models) but is given.
4. Nothing is tuned afterwards on what the summary shows. Hidden v2 has now been used by six models and one reader; say so.

## Traps

- **The adapter sees hidden text.** It is code the builder wrote, run by the owner, logging nothing. Treat any debugging need as for v3: reproduce on open cases.
- **Do not "improve" the line format.** The parser is frozen; an answer it does not understand is scored as wrong, exactly as for a model.
- **A good v2 number proves little about v3's targets** (v2 has 90 cases, no slots, the old polarity sheet). It is a bridge between two scales, not a result.
