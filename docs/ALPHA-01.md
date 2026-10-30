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

## Results

### Run 1 (29 Oct 2026, local `gemma3:4b`, claim type given)

Kept in `docs/baselines/2026-10-29-alpha-given/`.

| | cases | with a critical error | seconds / case |
|---|---:|---:|---:|
| development | 40 | 5 | 27 |
| validation | 14 | 5 | 28 |
| robustness | 2 | 0 | 28 |

The structured trial before it had 32 of 44 and 12 of 15, at 48–59 s.

What the raw answers showed:
- **The model copied the prompt's worked example.**
  - Its first line, `p | 2`, chose the second role in every case. That accounts for 3 role errors: a seller in a delivery-delay case, and a lessee twice where the lessor had sent the eviction notice.
  - It also copied the example's question numbers in cases with fewer questions. That accounts for 32 unreadable lines, and for the khul amount being given to a question that does not exist.
- **«رغم إعذاره» was read as no notice.** The model took the «لم» of the same sentence for the notice. A «محضر إعذار» in the documents list was read the same way. That accounts for 5 notice errors.
- **A loan the defendant denies** («يدعي… ينكر») was read as affirmed.
- **The time is now mostly reading the prompt:** about 630 tokens at 33 tokens per second, against 45 tokens of answer.

### Changes for run 2

- **The worked example is gone.** The prompt ends with an answer template for the case («1 | ? | ?», one line per question, with a value slot only where the question takes one).
- **The notice questions say «وُجّه إلى المدعى عليه إعذار…».** «أعذر» can also be read as "excused".
- **The status rules now say:**
  - a negation covers only the negated act («لم يدفع رغم إعذاره»: both facts happened);
  - a document in the documents list proves the fact it is named after;
  - «disputed» means one side claims it and the other denies or contests it.

These rules were written after seeing failures in development and validation cases alike. **The validation cases are therefore no longer blind.** A fresh validation set, written by someone who has not seen the prompt, is needed before any number is taken as final.

### Run 2 (29 Oct 2026, local `gemma3:4b`, claim type given)

Kept in `docs/baselines/2026-10-29-alpha-given-2/`.

| | cases | with a critical error | of which the answer was cut off | seconds / case |
|---|---:|---:|---:|---:|
| development | 40 | 6 | 1 | 26 |
| validation | 14 | 2 | 1 | 31 |
| robustness | 2 | 0 | 0 | 29 |

What the raw answers showed:
- **The answer format slipped in a few cases:**
  - a status left out («1 | v1 | v2»);
  - a value copied out in full («300000» instead of its number);
  - a question numbered like a sentence («s1 yes»);
  - two answers cut off at the 160-token cap.
- **«no» was given for facts that are themselves negative.** «لم يتقاض أجوره» was read as "no" to «لم يدفع المدعى عليه الأجور», in two wage cases.
- **A seller was chosen as plaintiff** in the land-sale fraud case, where the defendant had sold.
- **The disputed loan was again read as affirmed.** This looks like a real limit of the model.

### Changes for run 3

- **The parser accepts the slips that are not ambiguous:**
  - with values but no status, the fact counts as stated;
  - a value written out in full counts if the program listed it;
  - yes and no are still read.

  Re-scoring run 2's own answers with it fixes khul and the land sale's missing contract.
- **The status words are now `stated`, `denied`, `disputed` and `absent`.** The prompt says that a negative fact stated as such («لم يدفع») is `stated`.
- **Role choices are sentences about the parties** («المدعي اشترى من المدعى عليه»), not bare titles.
- **The answer cap is 300 tokens.**

### Run 3 (29 Oct 2026, local `gemma3:4b`, claim type given)

Kept in `docs/baselines/2026-10-29-alpha-given-3/`.

| | cases | with a critical error | the same answers, read by the parser of run 4 | seconds / case |
|---|---:|---:|---:|---:|
| development | 40 | 4 | 2 | 21 |
| validation | 14 | 3 (1 cut off) | 3 | 28 |
| robustness | 2 | 0 | 0 | 24 |

What remains:
- **A role chosen among eight:** lessee for a buyer of furniture (contract damages).
- **Notices:** «رغم التنبيه» read as absent once, and «لم يوجه أي إعذار» read as disputed once.
- **The disputed loan read as affirmed,** in all three runs.
- **The tenant case was cut off** even at 300 tokens.

### Changes for run 4

These are changes to the parser only; the prompt is unchanged:
- **The lines written before a cut-off are kept.** The cut line is not read.
- **«s1» is read as question 1.** No answer line refers to a sentence.
- **A value number in the status's place** («6 | v6 | ?») means the fact is mentioned. A value number that does not exist is dropped, never resolved.

### Run 4 (30 Oct 2026, local `gemma3:4b`, claim type given)

Kept in `docs/baselines/2026-10-30-alpha-given-4/`.

| | cases | with a critical error | seconds / case |
|---|---:|---:|---:|
| development | 40 | 3 | 21 |
| validation | 14 | 3 | 26 |
| robustness | 2 | 0 | 25 |

There were no cut-off answers and no format slips. The six errors are the kinds seen in every run:
- a role chosen among eight;
- a notice missed, or read as disputed;
- the disputed loan;
- a conciliation report missed;
- the tenant's "other breach".

The results have levelled off.

### The whole chain (30 Oct 2026, claim type read by the analysis)

Kept in `docs/baselines/2026-10-30-alpha-read/`. The table is rebuilt from the case files, because the run's own summary was not written.

| | cases | fully right |
|---|---:|---:|
| development | 44 | 38 |
| validation | 15 | 9 |
| robustness | 2 | 2 |

- **The analysis read the right claim type in 55 of 61 cases.** It declined 4 of the 5 out-of-scope cases.
- **Every extra error comes from a wrong claim type.** Those are the errors the lawyer's one-click confirmation is there to catch.
- **Time:** about 110–130 s per case, most of it in the analysis's claim reading.

## Decision rule

Keep Alpha-01 only if, on the local `gemma3:4b`:
- **almost no case has a critical error** on development and on validation, in `given` mode;
- **the `read` mode's extra errors are only wrong claim types,** which the lawyer's confirmation catches in the app;
- **it is clearly faster** than the 48–59 s per case of the structured trial.

### Verdict (30 Oct 2026)

The first two conditions are met on the development set.
- **Critical errors:** with the claim type given, 37 of 40 development and 11 of 14 validation cases are fully right. Before Alpha-01, it was 12 of 44 and 3 of 15.
- **The `read` mode:** its extra errors are only wrong claim types.
- **Speed:** the reading step is twice as fast (21–26 s against 48–59 s).

These numbers are optimistic, because the cases were seen while tuning. The real measure is the hidden set (`docs/HIDDEN-SET.md`).

### The hidden set (30 Oct 2026): the honest measure

100 cases written by an independent agent, run once on the frozen version `83cd421`, claim type given (`docs/baselines/2026-10-30-hidden-given/`). The 10 cases outside the catalogue are left out, as the app abstains on them.

| | Development | Validation | **Hidden** |
|---|---:|---:|---:|
| cases fully right | 37 / 40 | 11 / 14 | **32 / 90 (36 %)** |
| seconds per case | 21–26 | 21–26 | **63** (33–83) |

**Each item is mostly read right; whole cases are not.** On the hidden set:
- the plaintiff's position is right in 97 % of cases;
- 88 % of decisive events are found;
- 81 % of polarities are right;
- 80 % of dates are bound to the right event;
- 93 % of requests are found.

A case has several events, so these small error rates add up. Of the 58 cases with a critical error, 25 have exactly one.

**What goes wrong, by type of error** (critical errors, a case can have several):
- **Polarity, 45.** 38 of them read as stated a fact the text disputes (26) or denies (12). The model takes the plaintiff's version and misses the other side's denial or the «لم…».
- **A date, an amount or a requested amount on the wrong event, 37** (19 + 8 + 10).
- **A decisive event missed, 16.**
- **The plaintiff's position or who acted reversed, 6.**

**By area:** family 15 / 29 fully right, labour 4 / 8, civil and commercial 13 / 53.

**Speed.** The hidden cases read like a lawyer's notes and are longer: the prompt averages 1,175 tokens, read at 22 tokens per second, so 54 s pass before the first word. Speed now depends on the length of the case, not on the answer.

**Verdict:** condition 1 is **not met** on unseen cases, and condition 3 is not met on cases of realistic length. The development score came largely from tuning on cases we had seen. **Alpha-01 stays in the harness and does not go into the app.** The app is unchanged: its analysis proposes the claim type and articles, and the lawyer confirms them.

**What this says about the way forward:**
- Most failures are one or two wrong items in an otherwise right reading. A reader whose answers the lawyer confirms item by item is a more realistic goal than a fully automatic one.
- Polarity is the main weakness of a 4B model. The text's own markers of denial («لم», «ينكر», «ينفي», «يدعي») could be pointed out by code before the model answers.
- Any change is measured on a **new** hidden set: this one's results have now been seen, by error type and case name (`docs/HIDDEN-SET.md`, rule 7).
