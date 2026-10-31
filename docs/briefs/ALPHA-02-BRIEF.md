# Brief: reading a lawyer's facts reliably on unseen cases

**Written:** 30 October 2026, before any work on it started. The target in §2 is fixed from this date.
**For:** an independent AI agent, working on the project owner's laptop.
**This brief covers two reports only. Do not edit, create or delete any file in the repository, and make no commit.** What is built afterwards, and by whom, is decided after the owner has read both reports.

Read `AGENTS.md` and this brief completely before anything else.

## 1. The problem

DZ Legal Assistant drafts Algerian statements of claim (عريضة افتتاح دعوى) offline, with a small local model (`gemma3:4b` through Ollama, on an 8 GB laptop without a graphics card).

The next step of the product is to **read the lawyer's notes into facts that code can rely on**:
- the plaintiff's position;
- the events, each with its date, amount and actor, and whether it is affirmed, denied or disputed;
- the requests.

Code can only reason about facts that were read correctly.

Our current attempt, **Alpha-01** (`docs/ALPHA-01.md`), was tuned on our own cases, then measured once on 100 unseen cases written by an independent agent, with the claim type given as the lawyer confirms it in the app (`docs/baselines/2026-10-30-hidden-given/summary.md`):

| | Our development cases | **Unseen cases** |
|---|---:|---:|
| cases with no critical error | 37 / 40 | **32 / 90 (36 %)** |
| seconds per case | 21–26 | **63** |

- **Single items are mostly right:**
  - position 97 %;
  - decisive events found 88 %;
  - polarity 81 %;
  - dates on the right event 80 %;
  - requests 93 %.

  A case has several items, so whole cases fail.
- **The main errors:**
  - polarity, 45: 38 of them read as stated a fact the text disputes (26) or denies (12);
  - a date or amount bound to the wrong event, 37;
  - a decisive event missed, 16;
  - a position or actor reversed, 6.
- **Speed:** the prompt averages 1,175 tokens, read at 22 tokens per second.

We tuned our way to a score that did not hold. **We want you to find out why, and what would hold.**

## 2. The target and the limits

**The target (fixed now):**
- at least **80 % of the in-catalogue cases with no critical error** on a new unseen set;
- at most **about 90 seconds per case** on the owner's laptop.

You may argue that the target, or what counts as a critical error, is wrong for the product. Argue it in your report, and it will be decided **before** the new set is run, never after.

**The limits:**
- **Runtime:** `gemma3:4b`, offline, through Ollama on loopback, CPU only, in the shipped app. A larger or online model may help during development, never at run time.
- **Privacy:** case data never leaves the lawyer's computer.
- **The lawyer stays in charge:** the app proposes, and the lawyer confirms. The product's safety rules in `AGENTS.md` stay as they are.
- **Honest measurement:** a new unseen set, written by someone else, run once.

**Everything else is open:**
- the approach;
- whether "a reader" is even the right shape;
- the questions, the codes, the prompts;
- how much is done by code and how much by the model;
- what the lawyer is asked to confirm.

If you think we are solving the wrong problem, say so.

## 3. What you may and may not do

**You may:**
- read the whole repository, including `docs/baselines/2026-10-30-hidden-given/summary.md` (error types and case names only);
- compile the project and run the existing tests and benchmarks to support your findings, for example:

  ```powershell
  dotnet build -c Release
  dotnet test -c Release
  dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --partition development --models gemma3:4b --out <a folder outside the repository>
  dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --partition development --models gemma3:4b --case <name> --out <a folder outside the repository>
  ```

  Write run outputs only outside the repository. Run one benchmark at a time: they share one Ollama.

**You may not:**
- edit, create or delete any file in the repository, or commit;
- open, list or copy `C:\dz-hidden*` (any folder or archive of that name), `C:\dz-hidden-results*`, or anything the owner says belongs to an evaluation set;
- use the 15 validation cases of `tests/fact-cases` more than twice, as a check, never to tune.

## 4. Report 1: understanding and diagnosis

1. **The project in your own words:** what the product does, where the model is used today, where the fact reading fits, and what the lawyer sees and decides.
2. **The core issues:** for each error type above, its root cause, shown on development cases with evidence (an answer, a prompt, a run). Separate:
   - what the model cannot do;
   - what our design asks badly;
   - what the measurement itself gets wrong (answer-key errors, scoring choices).
3. **Why our development score did not hold** on unseen cases.
4. **What you are unsure of.**

Then stop and wait for the owner.

## 5. Report 2: the proposed solution, without editing anything

1. **The approach,** and the alternatives you considered and rejected, with why.
2. **The design:** what code does, what the model does, what the lawyer confirms, with the prompts' shape and the data flow.
3. **The expected result on unseen cases,** with your reasoning, and how confident you are. If 80 % is out of reach under the limits, say so, and say what is reachable.
4. **The speed budget,** per case on the owner's laptop.
5. **How to measure it honestly:** what to run on our cases while building, and what to run once on the new set.
6. **The plan:** the files to change, in order, with the size of each change. Mark what is risky.
7. **What you would change in the target or the scoring,** if anything, and why.

Each report goes in your reply to the owner, in plain words first, then the details.
