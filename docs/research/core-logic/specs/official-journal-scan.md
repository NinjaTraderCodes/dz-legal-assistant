# Specification: the Official Journal scan (amendments, by machine)

## Why

A rule record quotes an article as printed in some edition. A later law may have changed that article, and a web search can only show that someone looked. The scan reads **every issue** of the Official Journal over a stated period and lists every text that touches one of the instruments the rules rest on. It turns "we searched and found nothing" into "these 1,900 issues were read; here is the list".

The owner approved it on 12 November 2026 with four conditions, all built in below: code names as well as law numbers; polite and resumable; a list of every issue read; not trusted if it misses a known amendment.

## Scope

- **Period:** from issue 1 of 2003 to the last issue published on the day of the scan. Before 2003 the French PDFs are scanned images without text; that period is covered differently (see "Before 2003").
- **Edition scanned:** French (`jo-francais`), because its text layer is readable. The Arabic edition's text layers of 2005 and 2007 are garbled (Codex's corpus documents this). The **text of an article is always taken from the Arabic page**; the French edition is used to *find*, never to quote.
- **The six instruments and what to search for:**

| Instrument | Numbers | Names |
|---|---|---|
| Civil Code | `75-58` | `code civil` |
| Commercial Code | `75-59` | `code de commerce` |
| Code of civil and administrative procedure (CPCA) | `08-09` | `code de procédure civile et administrative` |
| Family Code | `84-11` | `code de la famille` |
| Law on labour relations | `90-11` | `relations de travail` |
| Law on individual labour disputes | `90-04` | `conflits individuels de travail` |

Matching is case-insensitive, with accents folded, runs of white space treated as one space, and every hyphen-like character treated as `-`. A number matches only when it follows `n°`, `no`, `nº` or `numéro` within 4 characters, so that `75-58` inside a page range or a date is not counted.

**Other instruments.** A rule may come to rest on a special law (a request in `law/requests.jsonl`, an adversary's find). Its number and name are added to the patterns, the patterns' version (`patternsVersion`, an integer) goes up by one, and the hit search is run again over the kept text (below): no issue is downloaded again. Until that is done, the articles of that instrument have `ledgerClean: false` with the reason `scanNotRun`.

## Where the issues are

`https://www.joradp.dz/FTP/jo-francais/<YYYY>/F<YYYY><NNN>.pdf` (Arabic: `/FTP/jo-arabe/<YYYY>/A<YYYY><NNN>.pdf`), `NNN` from `001`, three digits. Checked on 12 November 2026: both hosts answer; the path is not case-sensitive; issue 1 of 2003, issue 31 of 2007 and issue 48 of 2022 exist. **The site has no robots file** (HTTP 404 on `www.joradp.dz/robots.txt` and `joradp.dz/robots.txt` that day). Check it again before the scan (`oj-fetch --check-robots`); if one exists by then, obey it and tell the owner what it forbids.

The issues of a year are numbered without gaps. The script counts up from 001 and stops a year after 5 "not found" answers in a row.

## One door to joradp.dz: `oj-fetch`

The owner asked for "one download at a time with a pause of a few seconds". The scan, the triage agents, the page fetches of `law/README.md` and the adversary all download from the same site, at times at once. So **every download from joradp.dz, by anyone, goes through one script**, and nothing else downloads from that site.

- **Where:** `research/core-logic/scripts/oj_fetch.py`, run with Python 3.12 (`C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`), with its tests in `research/core-logic/scripts/tests`. Called `oj-fetch` in this handbook.
- **Use:** `oj-fetch <url> --out <folder> --caller <name> [--wait <seconds>]`. It accepts only `https://www.joradp.dz/...` and `https://joradp.dz/...` addresses, and refuses any other.
- **The lock.** It creates `C:\Users\TheShadow\dz-core-research\oj-fetch\joradp.lock` exclusively (it fails if the file exists), writing its process id, caller, address and time. It holds the lock during the download **and** the random pause of 3 to 6 seconds after it, then deletes it. **A second simultaneous download is refused:** a call made while the lock is held downloads nothing; it waits for the lock at most `--wait` seconds (default 0), checking every few seconds, and otherwise exits with code 3 and prints `busy`. A lock whose process no longer runs, or older than 10 minutes, is removed, with a line in the log. (This is a turn-taking file for downloads; it changes no access right of any folder.)
- **The record.** Each download appends one line to `C:\Users\TheShadow\dz-core-research\oj-fetch\fetches.jsonl`: `{ "at", "caller", "url", "httpStatus", "bytes", "sha256", "file" }`. The script prints the file's path, SHA-256 and size.
- **Failures.** "Not found" exits with code 4 (`notFound`); any other failure exits with code 5 and the error. It never retries by itself; the caller decides, and never loops.
- **Robots.** `oj-fetch --check-robots` fetches `robots.txt` from both hosts, through the same lock, and records the answer in `C:\Users\TheShadow\dz-core-research\oj-fetch\robots.json`. `oj-fetch` refuses to download when no check is recorded in the last 7 days, or when a recorded robots file forbids the path.
- **Client name:** a generic one, with no personal detail: `dz-law-scan/1.0 (research; one request at a time)`.

The scan calls the same function for each issue, so it releases the lock between issues and a triage agent or a page fetch can pass (with a `--wait` of a few minutes).

## Politeness (the owner's rule)

- One download at a time, through `oj-fetch`, with its pause.
- Stop the whole scan after 5 failures in a row that are not "not found" (time-outs, 5xx). Do not hammer.
- **Resume, never restart:** an issue already in the evidence list with its hash is not downloaded again. A change of patterns is handled by searching the kept text again, not by downloading.
- Expected load: about 1,900 issues of 70 to 450 KB. The downloads take about 3 hours in all; the triage of the hits and the readings it calls for run for days.

## What the script does with one issue

1. Download it through `oj-fetch` to a temporary folder outside the repository. Note its SHA-256 and size.
2. Extract the text page by page (`pdftotext`, installed in `/mingw64/bin`) and **keep the text** in `C:\Users\TheShadow\dz-core-research\journal-text\<YYYY>\F<YYYY><NNN>.txt` (public law text, a few hundred KB per issue; outside the repository, never committed). If the issue has fewer than 200 characters of text per page on average, mark it **blind**.
   - **The shifted font (found on 13 November 2026).** Some issues of 2005 print part of their text in a font whose text layer is shifted: each character 29 code points lower (`'LPDQFKH` for «Dimanche»), spaces and digits as control characters, accented letters as plain ones. Such text has characters, so it is not blind, but no pattern can match it. Every line that carries those control characters is decoded when its decoded form is clearly more French, by a letter-pair model learnt from clean pages (`research/core-logic/scripts/french_bigrams.json`, built by `journal_scan.py model` from the kept text of 2007). The evidence line records `textVersion` and `decodedLines`.
   - **The readability check (owner, 13 November 2026).** After that repair, every page with at least 300 letters must read as French by the same model (no less French than the least French clean page seen, with half a bit of margin; tables of names and figures pass). A page that fails is listed in `unreadablePages`. **An issue that is blind or has an unreadable page is `readable: false`: it is listed as unread, never as "no match"**, its hits do not count as a search, and its table of contents is read from page images like a blind issue's (below). The summary gives the number of unread issues per year.
3. Read the issue's date from the masthead of page 1 (French, for example « Dimanche 17 juillet 2022 »).
4. Search every page with the current patterns. For each hit, record the instrument, the pattern, the page, and the sentence around it (at most 300 characters).
5. Classify each hit by rule:
   - **visa:** the sentence starts with `Vu ` (the list of texts a law refers to at its head). Almost every law and decree cites the codes this way, so visas are counted but not examined.
   - **title:** one of `modifiant`, `complétant`, `modifie`, `complète`, `abrogeant`, `abroge` or `portant` is within 150 characters of the matched number or name, wherever the sentence is on the page.
   - **operative:** the sentence holds `est modifié`, `sont modifiés`, `est complété`, `sont complétés`, `est abrogé`, `sont abrogés`, `est remplacé`, `est inséré`, `il est inséré`, `est rédigé`; or `modifié`, `complété`, `abrogé` within 150 characters of a code name (the owner's condition, for finance laws that amend a code without naming the original law).
   - **other:** anything else.
   A hit that fits both title and operative is operative.
6. Append one line to the evidence list and **delete the PDF**.

Steps 4 and 5 are a separate pass over the kept text, so that they can be run again after a change of patterns; each line records the `patternsVersion` it was searched with.

## The evidence list (committed)

`docs/research/core-logic/law/journal-scan/issues-<YYYY>.jsonl`, one line per issue:

```json
{"year": 2022, "issue": 48, "issueDate": "2022-07-17", "dateSource": "masthead",
 "url": "https://www.joradp.dz/FTP/jo-francais/2022/F2022048.pdf",
 "sha256": "…", "bytes": 295824, "pages": 32, "textChars": 98211, "textSha256": "…", "blind": false,
 "readOn": "2026-11-20", "patternsVersion": 1, "error": null,
 "hits": [{"instrument": "cpca", "pattern": "08-09", "page": 4, "class": "title",
           "sentence": "Loi n° 22-13 du … modifiant et complétant la loi n° 08-09 …"}],
 "visaHits": {"civil-code": 3, "cpca": 5}}
```

- `issueDate`: the date printed on the issue (the owner's "date"). `dateSource`: `masthead`; `triage` when a blind issue's date was read from its page image; `unread` (with `issueDate: null`) until then. `readOn` is only the day the scan read it.
- `error`: for an issue that could not be fetched or read, the error, with `sha256` null; otherwise null.
- `readable`, `unreadablePages`, `textVersion`, `decodedLines`: the readability check and the repair above.
- **`textAsOf`** (used by the store and the grades) is the latest `issueDate` such that every issue up to it is in the evidence list with no `error`, no `unread` date, and either `readable: true` or a triage answer read from its page images.

A summary file `journal-scan/SUMMARY.md` gives, per year: issues fetched, **unread issues** (blind or failing the readability check), issues in error, issues with decoded lines, hits by class, the patterns' version, and the scan's start and end dates.

## The test the scan must pass before it is trusted

It must re-find, as **title** or **operative** hits, each of these known amendments:

| Text | Amends | Issue |
|---|---|---|
| Law 05-02 of 6 February 2005 | Commercial Code | 11 of 2005 |
| Ordinance 05-02 of 27 February 2005 | Family Code | 15 of 2005 |
| Law 05-10 of 20 June 2005 | Civil Code | 44 of 2005 |
| Law 07-05 of 13 May 2007 | Civil Code | 31 of 2007 |
| Law 22-13 of 12 July 2022 | CPCA | 48 of 2022 |

The owner named four laws (05-02, 05-10, 07-05, 22-13); "05-02" names two texts of 2005, so all five are required. The scan must also re-find every publication dated 2003 or later that Codex's ledgers list (`data/law/corpus/<code>/ledger.json` on `codex/law-corpus`, for example Laws 15-20 and 22-09 for the Commercial Code). Run the years 2005, 2007 and 2022 first, so that the test can pass early. The pattern tests use the kept text of these five issues as fixtures.

**If one is missed, the scan is not trusted.** Say so to the owner, fix the patterns, and search the kept text of the years concerned again. Do not report "clean" for any article until the test passes.

## From hits to a ledger

Every **title** and **operative** hit, and every **other** hit that is not plainly a decree applying a law, is examined by an agent (Sonnet 5.5, prompt [../prompts/journal-hit-triage.md](../prompts/journal-hit-triage.md)). It fetches that issue (French and Arabic) through `oj-fetch`, renders the pages it needs as images, reads them, and answers: which instrument, which articles are amended, inserted or repealed, any transitional provision, and the text's number, date and pages. The triage only says where to look. **Every touched article that a rule record cites is then read as `law/README.md` says ("A touched article found by the scan"): Sonnet 5.5, then Codex, and Opus 5.5 settles a difference.**

The result is `docs/research/core-logic/law/amendment-ledger.json`, built by script from the triage answers and Codex's ledgers:

```json
{ "builtOn": "2026-11-25", "scanFrom": "2003-01-01", "textAsOf": "2026-11-20", "patternsVersion": 1,
  "instruments": {
    "cpca": [
      { "id": "law-22-13", "kind": "law", "number": "22-13", "date": "2022-07-12",
        "issue": { "year": 2022, "number": 48, "date": "2022-07-17", "sha256": "…" },
        "pages": [3, 4, 5],
        "articlesTouched": [ { "article": "32", "how": "replaced" } ],
        "transitional": "<the sentence, or empty>",
        "foundBy": ["scan", "codex-ledger"],
        "triage": "<file name of the triage answer>" } ] } }
```

`foundBy` holds one or more of `scan`, `codex-ledger`, `consolidated-edition`. Write the JSON schema of this file with a test when the script is written.

For every entry of the article store, the store build then computes:
- `textAsOf`: as defined under the evidence list;
- `ledgerRefs`: the ledger entries that touch it (`<instrument>/<id>`, the same name the rule record's `source.ledgerRefs` uses);
- `ledgerClean`: true when the entry's version is the one introduced by the latest of those entries (or by the original text when there is none) and the instrument is covered by the patterns, false otherwise. False sends the article to the hand check of step 2.

## Blind and unread issues

An issue without a text layer is listed as blind; an issue that fails the readability check is listed as unread in the same way. Its first pages (the table of contents, and the masthead with the date) are rendered as images and read by a triage agent (Sonnet 5.5, the same prompt, told that the issue is blind), which lists the titles and the issue's date; titles that name one of the instruments are treated as title hits. If more than a handful of issues of a year are blind, say so in the summary: that year is weaker evidence.

## Before 2003

The scan does not cover it. The basis there is, per instrument:
- **Civil Code:** the official consolidated edition of 2007 (`docs/official-law-texts/75-58_wipo_Version.pdf`) and Codex's ledger (Laws 83-01, 84-21, 88-14, 89-01).
- **Commercial Code:** the official consolidated edition (`75-59_wipo_Version.pdf`) and Codex's ledger, which also names four earlier texts it never downloaded (87-20, 93-08, 96-23, 96-27): read their pages for every commercial-lease article cited.
- **Family Code:** Law 84-11 as published, its 1984 rectification, and Ordinance 05-02 (inside the scan).
- **CPCA:** published in 2008; nothing before.
- **Laws 90-11 and 90-04:** the repository holds the 1990 originals and the issues of 1991 and 1996 that carry Laws 91-28 and 91-29 and Ordinance 96-21. Other texts between 1990 and 2002 (for example Legislative Decree 94-03 and Ordinances 97-02 and 97-03) must be listed from two official sources (the labour ministry's consolidated text and the Official Journal pages themselves) and read. Until that is done, an article of these two laws cannot have a clean ledger, and its grade is at most B.

State the cut-off date and these limits in the step 2 report.

## "Done when"

1. Every issue from 2003 to the scan date is in the evidence list with its hash and its date, or with its `error`.
2. The five known amendments and Codex's post-2003 ledger entries are all re-found.
3. Every title and operative hit has a triage answer; every touched article that a record cites has two readings (Sonnet 5.5 and Codex), settled by Opus 5.5 where they differ.
4. `amendment-ledger.json` exists; every entry of the store has `textAsOf`, `ledgerRefs` and `ledgerClean`.
5. No PDF remains on disk (the extracted text is kept outside the repository); the summary states the period, the counts, the blind issues and the limits before 2003.

## Traps

- **Visas will swamp the hits** if the `Vu` rule is missing: every law cites the Civil Code at its head.
- **Finance laws** (`loi de finances`, `loi de finances complémentaire`) amend codes in a single article deep in the text. They are why the operative class exists; never skip an issue because its title looks unrelated.
- **Rectifications** (`rectificatif`) correct a published text weeks later. Search that word next to the instrument numbers and treat a hit as a title hit.
- **Do not quote the French text** in a rule record. Arabic is the official text; where the two differ, say so in the record's open points.
- **Never keep the PDFs,** and never commit them or the kept text.
- **Never download from joradp.dz except through `oj-fetch`**, in a script or an agent.
