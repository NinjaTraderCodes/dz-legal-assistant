# The fact-reading step (first step of the rebuilt legal core)

**What it is.** The first step of the new pipeline (`docs/CORE-REBUILD-PLAN.md` §4). A reader turns what the lawyer wrote into short, coded facts:
- the relationship between the parties, and the plaintiff's position in it;
- the events, each with its date, amount, actor and polarity (affirmed, denied or disputed);
- the requests.

Every fact points to the numbered sentence it came from, and the program checks every date and amount against that sentence.

**What it is not.** The reader decides nothing legal. Choosing the claim type and the articles is the job of the rules that come next, which are not built yet.

**Status.** It is measured in the harness only; it is **not used by the app**. It replaces nothing until a reader passes the comparison below.

## Code

| Where | What |
|---|---|
| `src/DzLegal.Drafting/CaseFacts.cs` | **The reader's input and output:** <ul><li>`CaseText`: every field read (subject, facts, requests, documents) cut into numbered sentences s1, s2…, with the court section as a hint;</li><li>`FactVocabulary`: the closed lists of codes;</li><li>`CaseFacts`: the facts read;</li><li>`FactValues`: reading and checking dates and amounts.</li></ul> |
| `src/DzLegal.Drafting/FactReader.cs` | **The model reader:** one call; coded lines in, checked facts out. |
| `data/law/prompts/read-case-facts.ar.txt` | **The prompt:** the instructions, the codes and one worked example first (the same for every case, so a local server can keep them read), then the case. |
| `tests/fact-cases/cases.json` | **The answer key: 61 fictitious cases.** <ul><li>44 formal cases from `tests/grounds-cases` (development);</li><li>2 Darija cases (robustness);</li><li>15 new contrast cases (validation: do not tune on them): competing dates, a key date only in the documents list, a denied notice, a tenant or borrower as plaintiff, a disputed loan, an amount only in the requests, a misleading court section.</li></ul> |
| `src/DzLegal.Harness/FactsEvaluator.cs` | **The scoring:** runs a reader, or scores another tool's predictions, against the answer key. |

## What counts as a critical error

Each of these would make the legal decision or the draft wrong:
- the plaintiff's position reversed (lessor read as lessee, lender as borrower);
- a decisive event missing;
- a date or amount bound to the wrong event (the lease's start date given as its signing date);
- a denied or disputed fact read as affirmed, or anything the answer key forbids;
- the actor of an event swapped between plaintiff and defendant;
- a value invented (not written anywhere in the case);
- a wrong amount in a request.

**The headline number is the cases with at least one critical error.** It is never averaged away. Other differences (a missing request, a value cited from the wrong sentence, an unreadable line) are reported apart.

## Commands

Run from the repository root.

**Local models (Ollama):**
```powershell
dotnet run -c Release --project src/DzLegal.Harness -- facts --models gemma3:4b,qwen3.5:2b --out bench-facts-local
```

**Options:**
- `--partition development,validation,robustness` (the default);
- `--case <name>`, for one case;
- `--endpoint`.

**Another tool (e.g. an extraction encoder in Python):**
```powershell
dotnet run -c Release --project src/DzLegal.Harness -- facts export --out fact-cases-input.json
# ... the tool reads fact-cases-input.json and writes predictions.json ...
dotnet run -c Release --project src/DzLegal.Harness -- facts score --predictions predictions.json --reader gliner2.5 --out bench-facts-gliner
```

**`fact-cases-input.json`** is a list of cases:
```json
{ "name": "...", "partition": "...", "courtSection": "...", "sentences": [ { "id": "s1", "field": "notes.facts", "text": "..." } ] }
```

**`predictions.json`** maps each case name to either:
- `{ "answer": "<coded lines, as the model reader writes them>" }`, or
- an object of codes from `FactVocabulary`:
```json
{ "relation": "lease", "position": "lessor", "seconds": 0.4,
  "events":   [ { "kind": "rent_unpaid", "date": null, "amount": "240000", "actor": "defendant", "polarity": "affirmed", "sentences": ["s2"] } ],
  "requests": [ { "kind": "payment", "amount": null, "sentences": ["s4"] } ] }
```

The values are checked against the sentences exactly as for the model reader.

## How to decide

Keep a reader only if both hold:
- **almost no cases with a critical error** on development **and** validation;
- **clearly faster than `gemma3:4b`,** measured on the same laptop.

A reader that writes valid lines but reverses roles or reads denials as facts is rejected, however fast it is.
