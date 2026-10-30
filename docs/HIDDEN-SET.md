# The hidden evaluation set: how to write it and how to run it

**Purpose:** to measure the fact reading on cases its developers have never seen.

The 61 cases in `tests/fact-cases` were used to build and tune the reader, so its score on them is optimistic. The hidden set is the honest measure.

## Rules

1. **Who writes it.** Someone who has not worked on the reader: an independent agent, or a person.
2. **What the author may read:**
   - this file;
   - the claim catalogue `data/law/claims/*.json`, for what each claim type is;
   - `data/law/claim-elements.json`, for the facts and requests each claim type is read for;
   - the codes in `src/DzLegal.Drafting/CaseFacts.cs` (`FactVocabulary`).
3. **What the author must not read:**
   - the prompts in `data/law/prompts/`;
   - the cases in `tests/fact-cases/` and `tests/grounds-cases/`;
   - the benchmark results.
4. **Where it lives.** Never in the repository and never sent to the developers. It stays in a folder outside the repository, for example `C:\dz-hidden\`.
5. **Fictitious data only.** Names, places, dates and amounts are invented. Formal Arabic (الفصحى) only.
6. **Runs.**
   - It is run once per frozen version. The commit is recorded in `manifest.json`.
   - Only `summary.md` is shared.
   - **The `--out` folder holds every case's text: do not share it.**
7. **After use.** Once its failures are opened to improve the reader, the set has been seen. It then joins development, and the next measure needs a new hidden set.

## Content (about 100 cases)

**Coverage:**
- **4 or 5 cases for each of the 20 claim types** in the catalogue, about 90 in all.
- **10 cases outside the catalogue** (`"claim": "out-of-scope"`), for example an inheritance partition, an administrative appeal, a company dispute or a correction of civil status.

**Make them read like a lawyer's notes, not like an exercise:**
- 3 to 10 sentences;
- side details that do not matter;
- several dates and amounts, some of them belonging to different facts;
- a key date given only in the documents list;
- facts that did not happen («لم يوجه أي إعذار»);
- facts one party denies («ينكر المدعى عليه…»);
- the plaintiff on either side when the claim type allows it (the lessee in a commercial lease, the father in a custody case);
- a monthly amount next to a total;
- a court section that does not match the facts.

## Format

One file, for example `hidden-cases.json`. It holds an object with a `cases` list, and each case is:

```json
{
  "name": "h001",
  "partition": "hidden",
  "claim": "unpaid-rent",
  "inputs": {
    "claim.subject": "…",
    "notes.facts": "one fact per line\n…",
    "notes.requests": "…",
    "documents.list": "…",
    "court.section": "القسم المدني"
  },
  "gold": {
    "relation": "lease",
    "position": "lessor",
    "events": [
      { "kind": "contract_concluded", "date": "15/02/2024", "polarity": "affirmed", "decisive": true },
      { "kind": "rent_unpaid", "amount": "400000", "actor": "defendant", "polarity": "affirmed", "decisive": true },
      { "kind": "notice_served", "polarity": "denied", "decisive": false }
    ],
    "forbidden": [ { "kind": "rent_unpaid", "amount": "50000" } ],
    "requests": [ { "kind": "payment", "amount": "400000" }, { "kind": "eviction" } ]
  }
}
```

### The fields

- **`name`:** neutral (h001, h002…). It appears in the shared summary, so it must say nothing about the case.
- **`claim`:** a claim `id` from `data/law/claims/*.json`, or `out-of-scope`.
- **`inputs`:** only these keys. Any of them may be left out except `notes.facts`.
- **`relation`, `position`, event `kind`, request `kind`:** codes from `FactVocabulary`. Several accepted answers are separated by `|` (`"lender|creditor"`).
  - The position must be one of the roles `claim-elements.json` lists for the claim type.
  - Every event and request must be one that claim type is read for.
- **`date`:**
  - `dd/mm/yyyy`, or a year (`2019`);
  - the same date must be written in the case text.
- **`amount`:**
  - digits only (`400000` for «400.000 دج»);
  - the same amount must be written in the case text.
- **`actor`:** `plaintiff` or `defendant`, only when who did it matters.
- **`polarity`:** `affirmed`, `denied` (the text says it did not happen) or `disputed` (one side claims it, the other denies it).
- **`decisive`:** `true` when a reader that misses this fact would get the case wrong.
- **`forbidden`:** a reading that must never be made:
  - an event that is absent or denied, read as affirmed;
  - or a value bound to the wrong event (the monthly rent given as the total owed).
- **Out-of-scope cases:** fill in `relation` and `position` as well as possible, and leave `events` and `requests` empty.

### Check the file without showing it to anyone

```powershell
dotnet run -c Release --project src/DzLegal.Harness -- facts check --cases C:\dz-hidden
```

The check names each problem by case name only, never by text. It reports:
- unknown codes or claim types;
- dates and amounts that are not written in the case;
- answers the reader is never asked for.

Fix them until it prints «No problem found.»

## Running it

```powershell
dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --cases C:\dz-hidden --partition hidden --models gemma3:4b --out C:\dz-hidden-results
```

Share only `C:\dz-hidden-results\summary.md`.

For the whole chain, with the claim type read by the analysis instead of given, add `--claims read`.
