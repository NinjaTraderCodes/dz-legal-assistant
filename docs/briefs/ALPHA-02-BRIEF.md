# Brief: Alpha-02, a fact reader that holds on unseen cases

**Written:** 30 October 2026, before any work on Alpha-02 started. The pass mark below is fixed from this date.
**For:** an independent AI coding agent, working on the project owner's laptop.
**Branch:** `fable/alpha-02`, created from the latest `claude/stoic-fermat-mi1gko`.
**Freeze:** Saturday 31 October 2026, 17:00 Algiers time.

Read `AGENTS.md`, then this brief, completely, before writing any code.

## 1. The problem

DZ Legal Assistant drafts Algerian statements of claim offline, with a small local model (`gemma3:4b` through Ollama, on an 8 GB laptop without a graphics card). The **fact reader** turns the lawyer's notes into coded facts: the plaintiff's position, the events (with date, amount, actor, and whether each is affirmed, denied or disputed) and the requests. It is the foundation of the next legal core: code can only reason about facts it has read correctly.

The current reader, **Alpha-01** (`docs/ALPHA-01.md`), was tuned on our own cases and then measured once on 100 unseen cases written by an independent agent. The claim type was given, as the lawyer confirms it in the app. The result (`docs/baselines/2026-10-30-hidden-given/summary.md`):

| | Our development cases | **Unseen cases** |
|---|---:|---:|
| cases with no critical error | 37 / 40 | **32 / 90 (36 %)** |
| seconds per case | 21–26 | **63** |

Each item is mostly right (position 97 %, decisive events found 88 %, polarity 81 %, dates on the right event 80 %, requests 93 %), but a case has several items, so whole cases fail. The main errors:
- **Polarity, 45:** 38 of them read as stated a fact the text disputes (26) or denies (12).
- **A date, an amount or a requested amount bound to the wrong event, 37.**
- **A decisive event missed, 16.**
- **Position or actor reversed, 6.**

Speed: the unseen cases are longer; the prompt averages 1,175 tokens, read at 22 tokens per second, so 54 s pass before the first word.

## 2. The goal and the pass mark (fixed now, not changed later)

Build **Alpha-02**, a new reader, so that on a **new** unseen set of about 100 cases (written by another agent, never seen by you or by us), with the claim type given:

1. **At least 80 % of the in-catalogue cases have no critical error**, as the existing scorer counts them;
2. **at most 90 seconds per case on average** on the owner's laptop, `gemma3:4b`;
3. **no regression** on the development cases against Alpha-01.

The new set is run **once**, by the owner, on your frozen commit. Whatever it gives is reported as it is.

## 3. What is fixed

- **The runtime.** `gemma3:4b` through Ollama on loopback, CPU only. No larger model, no online model, no GPU reliance, no fixed seed shipped. The model is the limit; the design around it is the work.
- **The scoring.** `src/DzLegal.Harness/FactsEvaluator.cs` and what counts as a critical error do not change. If you find an answer-key error in `tests/fact-cases/cases.json`, write it in your report with the evidence; do not change the key without the owner's approval, and never to raise a score.
- **The codes.** The codes in `FactVocabulary` (`src/DzLegal.Drafting/CaseFacts.cs`) and the claim types and their elements' codes in `data/law/claim-elements.json` stay as they are: the new unseen set is written against them. You may reword questions and add guidance, not rename or remove codes.
- **Alpha-01 stays runnable** (`facts --alpha`), for comparison. Add Alpha-02 as its own mode.

## 4. What you may and may not read

**You may read** the whole repository, including `docs/baselines/2026-10-30-hidden-given/summary.md` (error types and case names only).

**You must never open, list or copy:**
- `C:\dz-hidden\`, `C:\dz-hidden-2\` or any `C:\dz-hidden*` folder or zip;
- `C:\dz-hidden-results\` or any other results folder of a hidden run;
- any file the owner tells you is part of an evaluation set.

If a command would read them, do not run it.

## 5. How to work

- **Tune only on the development partition** of `tests/fact-cases` (44 cases). Use the validation partition (15 cases) **at most three times**, as a check, never to tune. The two robustness cases (Darija) are not scored.
- **Diagnose before you build.** First write the "Diagnosis" section of `docs/ALPHA-02.md`: for each error type, its root cause, shown on development cases.
- **Prefer code to the model** wherever a decision can be checked. Directions worth testing (not instructions):
  - **dates and amounts:** the model points to the sentence that states an event (the case is already cut into numbered sentences, `CaseText`); code takes the value from that sentence;
  - **denials and disputes:** code marks the sentences that carry a denial or a dispute («لم», «لا», «ما», «ينكر», «ينفي», «يدعي», «يزعم», «يعترض», «رفض»…), and the polarity follows from the sentence the model points to;
  - **smaller questions:** one short question per event, with the case text first in the prompt so Ollama reuses it between calls instead of reading it again;
  - **a check pass:** a second short question only where the first answer and the code disagree.
- **Measure every change** on the development cases, with seconds per case. Use `--case <name>` for quick checks; run the full development partition before keeping a change.
- **Generality.** No rule may name a case, a person, a place or an amount from any test set. Every rule must make sense for a lawyer's notes you have never seen.

Commands (from the repository root):

```powershell
dotnet build -c Release
dotnet test -c Release
dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --partition development --models gemma3:4b --out bench-alpha01-dev
dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --partition development --models gemma3:4b --case <name>
```

## 6. Rules of the repository

- `AGENTS.md` applies: 0 build warnings, every test green, never delete, skip or weaken a test, fictitious data only, offline, loopback only.
- **You may change, for this work:** `src/DzLegal.Drafting` (the reader and its helpers), `src/DzLegal.Harness`, `data/law/claim-elements.json` (wording only, see §3), the reader prompts in `data/law/prompts/`, and their tests.
- **You must not change:** the claim catalogue (`data/law/claims/`), the law texts (`data/law/codes/`), the form definition and templates, the desktop or web interface, or the scorer.
- Small commits with clear messages, pushed to `fable/alpha-02` only. No model names or identifiers in commits, code or docs.

## 7. What to deliver at the freeze

1. **The code**, with tests, on `fable/alpha-02`; build and tests green.
2. **`docs/ALPHA-02.md`:**
   - the diagnosis;
   - the design, and why;
   - development results, the validation checks, seconds per case, against Alpha-01 on the same laptop;
   - what you are unsure of.
3. **The frozen commit** and the exact command the owner runs once on the new unseen set.
4. **A summary for the owner** in plain words: what changed, what the development numbers say, and what you expect on unseen cases, with your reasons.

If the pass mark looks out of reach, say so early, with the best result you can reach and why. An honest 65 % is worth more than a 90 % that does not hold.
