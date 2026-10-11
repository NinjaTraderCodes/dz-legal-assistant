# Law library

The grounds suggestions rest on a **local copy of the official Arabic texts**. Loaded today: 39 articles of the Civil Code, the CPCA (2008 original text) and the Commercial Code. **Not loaded yet:** the Family Code and the two labour laws; the catalog cites their articles by number only, and the app refuses any quotation of them. The app quotes an article only from this library and never from a model's memory: a 4-billion-parameter model cannot recall article numbers or wording reliably.

## How the grounds analysis works

Nothing is scraped, searched online or read from PDFs while the lawyer works. Everything runs on the lawyer's machine. It starts only when the lawyer clicks «حلّل الوقائع واقترح الأسانيد» in the Legal grounds step.

**Prepared in advance:**
- The official texts are imported once, at development time, from the official PDFs into `data/law/codes/*.json`, and shipped with the app. A script checks each text against its PDF page; a lawyer's review is still to come (none is marked verified).
- The claim catalog (`data/law/claims/*.json`) is curated, lawyer-verifiable knowledge. For each claim type it records its usual articles, requests, documents and prerequisites.

**At the lawyer's click** (`GroundsAnalyzer`):
1. **Reading the facts** (model call 1, `prompts/read-facts.ar.txt`). The model writes five labelled lines:
   - the legal relationship;
   - the obligation breached;
   - what is sought;
   - one claim type (a second only for an independent demand), as an id from the catalog list for the court section's area, each listed with a one-line description; the list ends with `other`, the named way out when nothing applies;
   - 5–12 search terms in the codes' vocabulary.

   The parser accepts ids or Arabic names and formatting variants, keeps catalog claim types only (`other` means none), and removes any article number.
2. **Searching the official texts** (no model). `ArticleIndex` is a BM25 full-text index over every in-force imported article, with its chapter headings weighted ×2. It is queried with the model's terms (weight 1) and the raw facts (weight 0.5). The catalog articles of the recognised claim types come first. At most 12 candidates are kept.
3. **Choosing articles** (model call 2, `prompts/select-articles.ar.txt`). The model sees the candidates as `key — citation — first 180 characters of the official text` and answers `key | role | reason`. The parser:
   - keeps candidate keys only, at most 6;
   - maps the roles (أساس/إثبات/تعويض/إجراء);
   - truncates a reason to 30 words;
   - drops a reason that names another article.

**The result** is the model's reading plus the proposed articles, each with its role, reason, official text, provenance and origin (catalog or search). It is saved with the draft. Nothing is used until the lawyer ticks it.

**Why there are no hand-written keywords:** there is no official mapping from facts to articles, so a keyword list can never be complete. Here the model translates the facts into legal vocabulary, and the full-text index covers every imported article without curation.

**Before any code is imported,** stage 2 can only offer the catalog articles of the recognised claim types, described by their catalog reason instead of their text.

**Measuring it:** run this on the lawyer's kind of machine:

```bash
dotnet run --project src/DzLegal.Harness -- grounds --models gemma3:4b --out bench-grounds
```

It runs the fact patterns in `tests/grounds-cases/` and writes `bench-grounds/summary.md`, plus one file per case with the model's raw answers at each stage. It reports:
- **claim exact:** the expected claim is ranked first, or nothing is proposed when nothing applies;
- **claim found:** the expected claim is among those proposed;
- **core recall:** the expected articles proposed;
- **off-claim share:** proposed articles outside the expected claim's catalog list, i.e. noise;
- articles per case, empty answers, and seconds per case.

This is reported, not gated, because the quality depends on the model and on the imported texts.

**First measurement** (gemma3:4b, 8 GB laptop, no official text imported, catalog-only, cases written and tuned by us):

| Measure | Result |
|---|---|
| Claim exact | 64% |
| Claim found | 75% |
| Core recall | 73% |
| Time per case | 91 s |

Three weaknesses showed up:
- **Empty answers:** five cases came back empty, probably from the answer format.
- **Over-inclusion:** the model nearly always added a second, generic claim type and accepted 6–8 articles.
- **Confusions** between neighbouring claim types: liability for things vs tort, taṭlīq vs divorce, commercial lease vs commercial debt.

**What we changed in response:**
- The parser now accepts the claim type's Arabic name and formatting variants.
- Each claim type has a one-line `description` that tells it apart from its neighbours.
- The prompts ask for one claim type and 2–5 articles.
- The bench now saves the raw answers and measures the noise.

**Second measurement**, after those changes:

| Measure | Run 1 | Run 2 | Run 3 |
|---|---|---|---|
| Claim exact | 64% | 80% | **84%** |
| Claim found | 75% | 80% | 84% |
| Core recall | 73% | 61% | 67% |
| Off-claim share | not measured | 13% | 10% |
| Articles per case | about 7 | 2.4 | 2.5 |
| Empty answers | 5 | 0 | 0 |
| Seconds per case | 91 | 84 | 82 |

The lists are now short and focused. Core recall fell mostly because fewer articles are proposed, and because our "core" articles are a guess. For example, the model picks 107 rather than 106 for a loan, which is defensible.

Remaining errors:
- liability for things still read as tort (3 of 3);
- 3 of the 4 cases outside the catalog were still given a claim type;
- one taṭlīq read as divorce by the husband.

Two last generic changes followed:
- a named way out (`other`) at the end of the claim list;
- descriptions that say what each claim type is *not*.

**Run 3** came after those changes. Liability for things improved (0/3 → 2/3), and the commercial supplies case became right. The model **never chose `other`**, so all four out-of-catalog cases still got the closest claim type: inheritance was read as divorce, the administrative case as rescission, company dissolution as commercial debt, and civil status as a sale. Three single confusions remain: a dog bite read as tort, one taṭlīq read as divorce by the husband, and one loan read as a sale.

**Conclusion and decision:**
- A 4B model does not use an abstain option, so the lawyer judges the proposed claim type. This is acceptable because the model's reading and claim type are shown, and nothing is used unless the lawyer ticks it.
- `other` stays: it costs nothing and may work with other models.
- **Tuning on our own 44 cases stops here, to avoid overfitting.** The next measurements are:
  - the legal reviewer's hold-out cases, with her own core articles;
  - a run after the official texts are imported;
  - optionally, a larger local model if the hardware allows.

**Why there is no live scraping or web search:**
- The case facts would leave the machine.
- The sources are mostly scanned Journal Officiel PDFs, which a small local model cannot read reliably.
- It would take minutes per case.
- Nobody could verify or reproduce a suggestion afterwards.

**Provenance shown under each suggested article:**
- the official reference of the text, plus the Journal Officiel issue once a lawyer fills `joReference` in `sources.json`;
- the article's place in the code (book › title › chapter) once imported;
- a link the lawyer can click: the exact official document the text was imported from, or the official site (`officialSite`) before import;
- a tag: "Catalog proposal — not verified by a lawyer" until both the claim and the imported code are marked verified.

## Rules

1. **Article text only comes from an official publication.** Every code file records the URL it was imported from, the SHA-256 of the downloaded file, and the date. Nobody types article text by hand, and no model writes it.
2. **Every import is reviewed.** The importer writes a report listing gaps in the numbering, duplicates, and articles that are suspiciously short or long. A lawyer compares a sample of articles with the Journal Officiel before the code is marked `"verified": true`.
3. **Amendments show up as a diff.** Re-importing a newer consolidated text reports added, removed, changed and newly repealed articles.
4. **The app never downloads anything.** Importing is a maintainer task done at development time. The shipped app reads `data/law/codes/*.json` offline.

## Files

| Path | What it is |
|---|---|
| `data/law/sources.json` | The texts to import: id, titles, the citation name used in requêtes, and the official reference (law number and date). |
| `data/law/codes/<id>.json` | One imported code: metadata, source, and its articles (number, book/title/chapter path, text, repealed flag). |
| `data/law/reports/<id>.md` | The review report from the last import. |

Article numbers are canonical, for example `106`, `124 مكرر` and `124 مكرر 1`. «المادة الأولى» is stored as `1`.

## Importing a text

You need the .NET SDK, plus `pdftotext` (poppler-utils) for PDF sources. Run from the repository root:

```bash
# from the official URL
dotnet run --project src/DzLegal.Harness -- law import civil-code --url "<official PDF or HTML URL>" --as-of 2026-01-01
# or from a file downloaded by hand
dotnet run --project src/DzLegal.Harness -- law import civil-code --file ~/Downloads/code-civil.pdf --as-of 2026-01-01
```

`--as-of` records the date up to which the consolidated text includes amendments.

What the importer does:
- It extracts the text: PDF via `pdftotext`, HTML with its tags removed, or plain UTF-8 text.
- It turns PDF presentation forms into ordinary letters.
- It detects text extracted in reversed visual order and flips it.
- It splits the text on «المادة N» headings and keeps each article's text exactly as published.
- It re-joins lines that the PDF layout wrapped.

The code file is written only when it validates: canonical and unique numbers, and no empty text unless the article is marked repealed.

**Sources:** use the official publications, which are the Journal Officiel (joradp.dz) and the consolidated codes published by the Ministry of Justice (mjustice.dz). Algerian copyright law excludes official legal texts from protection. That should be confirmed as part of the legal review.

## Verifying an import (for the lawyer)

1. Read `data/law/reports/<id>.md`. Every gap must be explained, usually by a repealed article or an extraction problem. Explanations go in the code's `notes`.
2. Compare at least 20 articles chosen across the whole code, plus every article the claim catalog cites, with the official text.
3. If everything matches, set `"verified": true` in `data/law/codes/<id>.json` and commit.

## Claim catalog and suggestions

`data/law/claims/<area>.json` (areas: civil, commercial, family, social) lists **claim types** the way a lawyer thinks of them (with no keywords: the model recognises them from their label and a one-line `description` of when the claim applies and how it differs from its neighbours), for example «استرداد مبلغ قرض», «الخلع» or «التسريح التعسفي». Each claim type has:

- **articles:** code, number, role (`basis`, `proof`, `damages`, `procedure`) and a short "why".
- **prerequisites:** for example, prior conciliation for labour claims.
- **suggestedRequests**, **requiredDocuments** and **factsToState**, offered to the lawyer as one-click inserts and a checklist.

### Loader checks
- Every claim cites a code listed in `sources.json`.
- **Once a code is imported, every cited article must exist and be in force.** A wrong or repealed number fails at load time and in CI.
- Until the code is imported, the citation is shown with "text not yet available".

## Status

**39 articles are loaded**: those the four sample cases rely on, from the PDFs in `docs/official-law-texts`.
- Civil Code: SGG 2007 consolidated edition.
- Commercial Code: SGG 2007 consolidated edition.
- CPCA: Official Journal no. 21 of 2008, the original text. The 2022 amendment (Law 22-13) still has to be checked.

How they were loaded:
- **Typed.** `data/law/curated-articles.json` holds the clean text of each article. Only spacing, punctuation and the letter ك (which these PDFs encode as آ) were corrected.
- **Proved.** `python3 tools/law/build_codes.py` checks every text against its PDF: the same letters and digits in the same order, from the same page.
- **Written.** The builder writes `data/law/codes/*.json` with the page, the edition and the source extract. The loader rejects any later edit that changes a letter.
- **Verified by a lawyer.** Each article is `verified: false` until a lawyer compares it with the PDF page. The review sheet records her verdicts, which then set `verified`, `verifiedBy` and `verifiedOn`.

The codes are partial (`partial: true`). Every article the claim catalog cites in those codes must be loaded, and the
loader enforces this. The full-text importer (`law import`) remains for loading whole codes later.
