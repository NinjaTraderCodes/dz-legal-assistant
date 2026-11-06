# Brief: Alpha-03, a fact reader that holds on unseen cases

**Written:** 6 November 2026, before any work on it started. The pass mark in §4 is fixed from this date.

**For:** Codex, working on the project owner's laptop (Windows, 16 GB RAM, no graphics card, Ollama on loopback).

Read `AGENTS.md`, this brief and `docs/LOCAL-READER-SEED.md` completely before anything else.

## 0. The git rules, before anything else

**You never merge anything, anywhere.** In detail:

- **Your branch:** `codex/alpha-03`, created from `claude/stoic-fermat-mi1gko`. Commit and push only to it.
- **Never merge, rebase or cherry-pick into another branch:** not `claude/stoic-fermat-mi1gko` (the default branch, which deploys the website), not `codex/law-corpus`, not anyone's.
- **Never open, approve or merge a pull request.** Never force-push. Never delete a branch or a tag.
- **To take new work from the default branch,** merge *it into yours*, never the reverse.

The owner reads your reports and decides what, if anything, joins the default branch, and who moves it there.

## 1. The product, in three lines

DZ Legal Assistant drafts the Algerian statement of claim (عريضة افتتاح دعوى) in Arabic for lawyers, offline, with a small local model (`gemma3:4b` through Ollama). Code holds the law, the form and the checks; the model reads and writes; the lawyer confirms and signs. Case data never leaves the lawyer's computer.

The next step of the product is to **read the lawyer's notes into facts that code can rely on**:
- the plaintiff's position;
- each event, with its date, amount and actor, and whether the text **states, denies or reports it disputed**;
- the requests.

Code can only reason about facts that were read correctly.

## 2. The core issues

### 2.1 The number

On 100 unseen cases (90 within the catalogue), with the claim type given as the lawyer confirms it in the app, the current reader (Alpha-01, `docs/ALPHA-01.md`) gets:
- `gemma3:4b` on the laptop: **32 of 90 cases with no critical error (36%)**, 63 s per case;
- Nemotron 3 Ultra online, with the same reader: 64 of 90 (71%).

Sources: `docs/baselines/2026-10-30-hidden-given/summary.md` and `2026-10-31-hidden-nemotron-given/summary.md`.

A **critical error** is one that would make the legal decision or the draft wrong: a reversed role, a missed decisive event, a date or amount on the wrong event, a denied or disputed fact read as stated, a swapped actor, an invented value, a wrong requested amount (`docs/FACT-READING.md`).

### 2.2 Where it fails

| Error | Cases touched | Cases fully right if this alone were fixed |
|---|---:|---:|
| **Polarity:** a disputed (26) or denied (12) fact read as stated, and the reverse | 35 | 47 (52%) |
| **Binding:** a date (19) or amount (8) on the wrong event | 22 | 39 (43%) |
| A decisive event missed | 15 | 42 (47%) |
| A wrong requested amount (a monthly sum for the total) | 10 | 37 (41%) |
| Position or actor reversed | 4 | — |

**Polarity and binding together would give 64 of 90 (71%).** That is the online model's score.

### 2.3 Why it fails, as far as we know

1. **We tuned our way to a score that did not hold.**
   - Four rounds of prompt rules, templates and parser tolerance brought our development cases to about 4 errors in 56.
   - The unseen set then gave 58 in 90.
   - The rules fixed the cases they were written from.
   - **A disputed loan was read as stated in every run, whatever the prompt said.**
2. **The model leans one way.** Most polarity errors go the same direction, towards "stated". That looks like a standing bias, not random noise.
3. **The answer is one free-text block.** The model answers every question of the case in one go, one line each. Its status words and value numbers are parsed, not chosen.
4. **Prompt reading is the time budget.** About 1,175 prompt tokens read at **22 tokens/s** (54 s); 46 answer tokens written at 5.5 tokens/s (8 s).
   - Anything that sends more text per case pays at 22 tokens/s.
   - A prefix the server has already read costs almost nothing again.
5. **The measure may be part of the problem.** The online model still makes 20 polarity errors.
   - When a defendant contests a fact, the answer key may expect "disputed" where a careful reader could say "stated by the plaintiff".
   - Some polarity errors may be ambiguity, not weakness.

### 2.4 What was already tried, and what it gave

Section 2b of `docs/LOCAL-READER-SEED.md` is the full list. In short, these did not move the needle:
- one universal JSON fact sheet (10 of 61);
- Ollama's `format` schema for the claim type (valid answers, no better judgement);
- a model-written quote per value (present in the text and wrong);
- prompt rules for negation and "disputed";
- an embedding encoder (E5) used by similarity;
- `qwen2.5:3b` (weaker) and `qwen3:4b` (too slow).

Prefix reuse already exists in the app's analysis (Slice B, `ModelRequest.Earlier`).

**Do not spend your work repeating these.** If you believe one of them failed for a reason that no longer holds, argue it in Report 1.

### 2.5 What exists for you to build on

- **The reader:** `src/DzLegal.Drafting/ClaimFactReader.cs`, `CaseFacts.cs` (sentences `s1…`, values `v1…` found by code, closed vocabularies), `data/law/claim-elements.json` (what each of the 21 claim types is read for).
- **The prompt:** `data/law/prompts/read-claim-elements.ar.txt`.
- **The provider:** `src/DzLegal.ModelProviders.Ollama/OllamaModelProvider.cs`. Loopback only, CPU only, `num_ctx` 4096. No `format`, no logprobs today; several turns are possible through `ModelRequest.Earlier`.
- **The bench:**
  - `src/DzLegal.Harness`: `facts --alpha --claims given|read` runs a reader.
  - `facts export` and `facts score` score **any outside tool** (a Python prototype, an encoder) against the same answer key and critical errors.
  - The run manifest records the machine state.
- **The cases:** `tests/fact-cases/cases.json`, 44 development, 15 validation, 2 robustness; all fictitious.

## 3. What we ask: be creative, then prove it

**Lift the local reader's whole-case score on unseen cases, within the limits of §5.** How is yours to propose. You may rethink:
- the questions and their order;
- the split between code and model;
- how the model is asked and how its answer is read;
- a second, small model;
- what the lawyer is asked to confirm;
- the scoring itself, argued in advance.

### Our leading hypothesis

The one experiment we would run first is **polarity as a scored choice**:
1. Read the case once, then ask one short question per fact over that cached prefix.
2. The model answers with one token among four (1 stated, 2 denied, 3 disputed, 4 absent).
3. Read the four tokens' log-probabilities rather than the written word.
4. Correct the model's lean towards "stated" with a calibration (contextual or batch calibration) fitted on development and checked on validation.
5. Keep the binding as Alpha-01 does it.

It targets the largest error, it was never tried, and it fits the time budget: the case read once, about 2 to 3 s and one token per extra question.

**It is a hypothesis, not an instruction.** If you see a better path, or a better combination, take it and say why. `docs/LOCAL-READER-SEED.md` §4 lists twelve others with their costs and what was tried before. Use it as a starting point, not as a menu.

**One condition:** whatever you build is measured by the same bench, on the same laptop, against the same critical errors, unless a change to the scoring was agreed before the unseen run (§6).

## 4. The pass mark (fixed now)

On the **new unseen set (hidden v2)**, with the claim type given, run once:

| | Required | Stretch |
|---|---|---|
| In-catalogue cases with no critical error | **at least 50%** (Alpha-01: 36%) | 71% (the online model) |
| Seconds per case, owner's laptop, plugged in | **at most 90** | at most 63 (today) |
| Peak memory of the model server and any second model | **fits 16 GB** with the app running | — |
| Development cases | **at most 3 of 40 with a critical error**, as Alpha-01 run 4 (`docs/baselines/2026-10-30-alpha-given-4/`) | — |

**Reporting:**
- Report every error type separately, with seconds per case and peak memory. Never fold them into one figure.
- If you add an "unsure" outcome that the lawyer confirms, report two scores:
  - fully right **without** asking;
  - fully right **after at most N confirmations**.

  The first is the pass mark; the second is information.

You may argue that the pass mark or a critical error is wrong for the product. Argue it in Report 1. It is decided **before** hidden v2 is run, never after.

## 5. The limits

**At run time** (what the app would ship):
- the model is local, through Ollama on `127.0.0.1`, CPU only;
- no network, no download, no telemetry;
- a second model is allowed if it is small, runs on CPU and is licensed for redistribution (licence file beside it);
- the model never invents a value: dates and amounts stay found and checked by code, and every fact cites its sentence;
- `gemma3:4b` is the baseline. You may propose another local model (Gemma 4 E4B, Qwen3.5 4B, a fine-tune…), but every candidate goes through the same bench on the same laptop. Its licence, size and speed are part of the result.

**During development:**
- **The data:** fictitious only. You may write new fictitious development cases. Keep them apart from `tests/fact-cases`, labelled as yours.
- **Online models:** a larger or online model may help, for example to label synthetic training data, never at run time.
- **Keys:** stay in the environment. Never print, log, commit or show them in a screenshot.
- **Bench runs:** one at a time (they share one Ollama). Run outputs go outside the repository.

**Untouched, whatever you build:**
- the app's current analysis and drafting path: `GroundsAnalyzer`, `read-facts.ar.txt`, `select-articles.ar.txt`, `ArguedGrounds`, the formal check;
- `src/DzLegal.Law`, `src/DzLegal.Domain`, `src/DzLegal.FormEngine`, `src/DzLegal.Documents`;
- the law corpus (`data/law/corpus/`, `tools/law-corpus/`, `src/DzLegal.Law/Corpus/`, all of `codex/law-corpus`);
- `src/DzLegal.App`, `src/DzLegal.Presentation`, `src/DzLegal.Web`, `site/`;
- the lawyer-safety behaviour in `AGENTS.md`.

Alpha-03 lives in the harness until the owner decides otherwise.

## 6. Honest measurement

- **Hidden v2:** written by someone else, under `docs/HIDDEN-SET.md`.
  - The owner arranges it, from an author who has seen neither the prompts, nor the cases, nor `docs/LOCAL-READER-SEED.md`'s error list.
  - **You never write, open, list or copy it,** nor `C:\dz-hidden*` or `C:\dz-hidden-results*`, nor anything the owner says belongs to an evaluation set.
- **The run:** the owner runs it once, on your frozen commit, and shares only `summary.md`.
- **Development:** develop on the development partition and your own fictitious cases.
- **Validation:** use the 15 validation cases **at most three times**, as a check, never to tune. Each use is recorded in your report.
- **Ablations:** each component you add is measured on and off, on the same laptop, so its share of the result is known.
- **Answer-key changes:** if you find the answer key wrong, or the definition of "disputed" ambiguous, show it on development cases. Propose the change in Report 1 with an exact rule. The owner decides before hidden v2 is written, so its author uses the same rule.

## 7. What you may change on your branch

You may add or change:
- **`src/DzLegal.Drafting`:** new reader files (for example `ScoredFactReader.cs`). Changes to `ClaimFactReader.cs`, `CaseFacts.cs` or `IModelProvider.cs` only if Alpha-01 still runs and scores exactly as before.
- **`src/DzLegal.ModelProviders.Ollama`:** logprobs, schema or grammar options, several turns over one prefix. The app's existing requests must behave exactly as before.
- **`src/DzLegal.Harness`:** new readers, bench modes, scoring columns ("unsure", time, memory), ablation switches.
- **`data/law/prompts/` and `data/law/claim-elements.json`:**
  - new files, or new fields the app does not read;
  - existing wording only if Alpha-01's runs are reproduced with the old wording kept under its own name.
- **`tests/`:** new tests, and your own fictitious cases in a new file. Never delete, skip or weaken a test.
- **Experiments outside C#** (Python, an encoder, training scripts): in `tools/alpha-03/`, with a README saying how to run them. No model weights in the repository; say where they come from, with their licence.

After every commit:
- `dotnet build -c Release` → 0 warnings, 0 errors;
- `dotnet test -c Release` → all green.

## 8. The work, in three stops

### Stop 1: Report 1, before any code

Commit it as `docs/ALPHA-03.md` on your branch: **plain words first, then the details**.

1. **Your understanding** of the product, of where fact reading fits, and of what the lawyer sees and decides.
2. **Your diagnosis of the core issues.**
   - For each error type in §2.2: its root cause, shown on development cases with evidence (an answer, a prompt, a run).
   - Separate three things: what the model cannot do; what our design asks badly; what the measure gets wrong.
3. **Why the development score did not hold.**
4. **The approach you propose.** Include the alternatives you weighed, our leading hypothesis among them, and why you chose as you did.
5. **The expected result on unseen cases,** with your reasoning and how confident you are; and **the speed budget** per case.
6. **The plan:** files to change, in order, with the size of each change; what is risky; **your stop rules** (what result would make you abandon a path).
7. **Any change you propose to the pass mark, the critical errors or the answer key.**

Then **stop and wait for the owner.**

### Stop 2: build and measure

Once the owner approves Report 1, build on `codex/alpha-03` in small commits. Measure each step on development.

Add to `docs/ALPHA-03.md` as you go:
- each run, its command and summary, kept in `docs/baselines/<date>-alpha-03-…/`;
- what you learnt from it;
- what you changed next.

**Stop early and report** if:
- a precondition fails. For example, the installed Ollama returns no log-probabilities for `gemma3:4b`: say so, say what you would do instead, and wait;
- a path misses its stop rule.

### Stop 3: Report 2 and the freeze

When development and the validation checks say the reader is ready:
1. **Freeze a commit,** and give the owner the exact command to run on hidden v2.
2. **Report 2,** added to `docs/ALPHA-03.md`:
   - what you built;
   - the results per error type, with time and memory;
   - the ablations;
   - what you are unsure of;
   - every file you changed outside `src/DzLegal.Harness` and `tools/alpha-03/`;
   - the tests you added or updated.

**Then stop. Do not merge, do not open a pull request.** The owner runs hidden v2, reads both reports and decides.
