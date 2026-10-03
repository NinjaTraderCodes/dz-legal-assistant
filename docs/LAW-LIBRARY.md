# Law library

The grounds suggestions rest on a **local copy of the official Arabic texts**: Civil Code, CPCA, Commercial Code, Family Code and the two labour laws. The app quotes an article only from this library and never from a model's memory. A 4-billion-parameter model cannot recall article numbers or wording reliably.

## How suggestions are built

Nothing is scraped, searched online or read from PDFs while the lawyer works.

1. **The claim catalog is prepared in advance** (`data/law/claims/*.json`). It lists claim types and the articles each one cites. It was drafted by engineering and is waiting for a lawyer's review.
2. **The official texts are imported once, at development time,** from joradp.dz / mjustice.dz into `data/law/codes/*.json`, reviewed by a lawyer, committed, and shipped with the app.
3. **While the lawyer types the facts,** keyword matching inside the local catalog picks up to three claim types, instantly and offline.
4. **The model only writes.** It turns the articles the lawyer ticked into grounds paragraphs, using their official texts.

**Model fallback, on the lawyer's click only.** «اسأل النموذج عن نوع الدعوى» lets the local model choose among the catalog's claim types, for example when no keyword matched.
- The model sees the facts and a closed list of `id — label` lines, limited to the court section's area. It may only answer with ids from that list (prompt: `data/law/prompts/pick-claims.ar.txt`).
- `ClaimPicker` keeps only exact catalog ids, at most three. Anything else is discarded: explanations, invented ids, reasoning blocks.
- The chosen claims appear tagged «اختاره النموذج من فهرس الدعاوى», with the catalog's own articles. **The model never introduces an article number.**
- It takes about 20–60 s on a CPU-only laptop and can be cancelled. Template mode chooses nothing.
- `dotnet run --project src/DzLegal.Harness -- grounds --models gemma3:4b` measures how well a local model chooses, on the same fact patterns. This is reported only; there is no gate.

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

`data/law/claims/<area>.json` (areas: civil, commercial, family, social) lists **claim types** the way a lawyer thinks of them, for example «استرداد مبلغ قرض», «الخلع» or «التسريح التعسفي». Each claim type has:

- **triggers:** groups of words and phrases. A group adds its weight once if any of its words appears in the facts. A `required` group must appear, or the claim is not suggested.
- **exclude:** words that point to another claim type. For example, «إيجار» rules out a loan.
- **articles:** code, number, role (`basis`, `proof`, `damages`, `procedure`) and a short "why".
- **prerequisites:** for example, prior conciliation for labour claims.
- **suggestedRequests**, **requiredDocuments** and **factsToState**, offered to the lawyer as one-click inserts and a checklist.

### Loader checks
- Every claim cites a code listed in `sources.json`.
- **Once a code is imported, every cited article must exist and be in force.** A wrong or repealed number fails at load time and in CI.
- Until the code is imported, the citation is shown with "text not yet available".

### How the suggester works
`GroundsSuggester` reads the facts after Arabic normalisation:
- Diacritics are ignored, and alef, yā' and tā' marbūṭa forms are unified.
- «ال» and a leading «و ف ب ل» are allowed, and so are common endings («ا», «ه», «ها», «هم», «ات», «ين»…).
- Only claims of the area named by the court section are considered («القسم الاجتماعي» → social). An unclear section means all areas.

It returns at most three claim types, each with the words that matched. It is deterministic, and no model is involved.

### Quality gate
`dotnet run --project src/DzLegal.Harness -- grounds` runs the fictitious fact patterns in `tests/grounds-cases/`. Each case names the claim a lawyer would start from, or expects nothing for a case the catalog doesn't cover. CI requires the expected claim in the top 3 for at least 90% of cases, and every claim type must be exercised.

**Limitation:** the triggers were tuned on these same cases, so today's 100% is a development score. A lawyer-written hold-out set, never used for tuning, is needed before trusting the number.

## Status

The container used for development cannot reach joradp.dz or mjustice.dz yet. **No official text has been imported, so `data/law/codes/` is empty.** Tests run on clearly fictitious fixtures. The claim catalog (18 claim types) is complete but `verified: false`. Its article numbers are the engineering team's proposals, and the loader will check them automatically against the official texts once they are imported. Once the network allows those sites, or the PDFs are supplied as files, each code is imported with the commands above.
