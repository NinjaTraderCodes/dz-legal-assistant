# Law library

The grounds suggestions rest on a **local copy of the official Arabic texts**: Civil Code, CPCA, Commercial Code, Family Code and the two labour laws. The app quotes an article only from this library and never from a model's memory. A 4-billion-parameter model cannot recall article numbers or wording reliably.

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

## Status

The container used for development cannot reach joradp.dz or mjustice.dz yet. **No official text has been imported, so `data/law/codes/` is empty.** Tests run on clearly fictitious fixtures. Once the network allows those sites, or the PDFs are supplied as files, each code is imported with the commands above.
