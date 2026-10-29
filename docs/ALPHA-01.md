# Alpha-01: read the facts against the confirmed claim type

**Status:** a trial in the harness only. The app does not use it.

## Why

The first fact reader (`docs/FACT-READING.md`) asked a 4B model to fill a universal fact sheet in one answer: 17 relations, 23 positions, 38 events and 22 requests. On the local `gemma3:4b` run of 29 Oct 2026 (structured trial), only 10 of 61 cases had no critical error, at 48–59 s per case:
- **The plaintiff's position was wrong in 25 cases.** The model often wrote `lender`, the first item of the list, even after reading a marriage (khul, custody) or an employment relationship.
- **A decisive event was missing in 28 cases.** Neighbouring codes were confused (`debt_acknowledged` for a promise to repay).
- **Most of the time went into writing the answer:** about 210 tokens of JSON at 5 tokens per second.

The model does well on narrow questions. Recognising the claim type is one: 42 of 44 formal cases, and the lawyer confirms it in the app with one click.

## How

1. **The claim type comes first.** In the app, the analysis proposes it and the lawyer confirms it. In the bench it is either:
   - given, from the answer key (`--claims given`): this measures the reading alone;
   - read by the analysis's first step (`--claims read`): this measures the whole chain before the lawyer confirms.
2. **The relationship and the plaintiff's position follow from the claim type, by code.** A khul means a marriage with the wife as plaintiff. When a claim type allows more than one role (custody: mother or father; commercial lease: lessor or lessee; contract damages: buyer, client, lessee…), the model chooses among those roles only.
3. **The model answers a few short questions,** one line each: the facts and requests of that claim type only (`data/law/claim-elements.json`). For each one it says:
   - whether the text states it (`yes`), denies it (`no`), reports it disputed (`disputed`) or does not mention it (`absent`);
   - which of the dates and amounts the program found belong to it, by number (`v1`, `v2`…).

   It cannot invent a value, and it is never asked about anything outside the claim type.
4. **Code turns the answer into the same coded facts as before** (`CaseFacts`). The answer key and the scoring do not change.

## Files

| Where | What |
|---|---|
| `data/law/claim-elements.json` | For each of the 20 claim types: its role(s), its facts (with their actor and whether they carry a date or an amount) and its requests. Drafted from each claim's `factsToState` and `suggestedRequests` in the catalog. |
| `data/law/prompts/read-claim-elements.ar.txt` | The prompt: the answer format first, then the claim type, the case, the numbered values and the questions. |
| `src/DzLegal.Drafting/ClaimFactReader.cs` | Builds the questions, reads the answer lines, resolves the role and the actors. |
| `src/DzLegal.Harness/AlphaFacts.cs` | The two bench modes. |
| `tests/fact-cases/cases.json` | Each case now names its `claim`, or `out-of-scope` (5 cases: inheritance, administrative, company, civil status, and a borrower asking for his debt note back). |

## Scoring

- **The same answer key and the same critical errors** as `docs/FACT-READING.md`.
- **With `--claims given`,** the 5 out-of-scope cases are not read: the app abstains on them. They are listed, but left out of the table.
- **With `--claims read`:**
  - a wrong claim type is a critical error, and the case is still read against it, so its consequences show;
  - a claim type not recognised is a critical error;
  - so is a claim type proposed for an out-of-scope case.
- **A test checks the ceiling** (`AlphaFactsTests`): answering every question exactly as the answer key reads the case gives no critical error. So every role, event, value and request the key needs can be expressed by the questions.

## Honesty notes

- The element lists were written after the answer key (development *and* validation cases) had been read. They come from the catalog's own lists, not from the cases. Two hints that matched specific validation cases were removed before any run. Validation is therefore no longer fully blind for the element lists. It still is for the model's reading.
- Removing the role question from the model fixes most role errors by construction. That is the point of the design, not a measurement of the model. The `read` mode shows what remains when the claim type itself is wrong.

## Decision rule

Keep Alpha-01 only if, on the local `gemma3:4b`:
- **almost no case has a critical error** on development and on validation, in `given` mode;
- **the `read` mode's extra errors are only wrong claim types,** which the lawyer's confirmation catches in the app;
- **it is clearly faster** than the 48–59 s per case of the structured trial.
