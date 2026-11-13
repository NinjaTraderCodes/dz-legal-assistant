# The law delta (step 2)

**The task in one sentence.** Every article a rule record cites must exist with its official text in force (or the older wording the rule names); where the app's library and Codex's corpus already give that, prove it by script and by fresh page readings; where they do not, read the Official Journal and write **only the difference**.

**Do not rebuild a corpus.** Do not change `data/law/`, `tools/law-corpus/`, `src/DzLegal.Law/Corpus/` or the branch `codex/law-corpus`. Corrections go to the owner, who passes them to Codex.

## Files of this folder

| File | What it is | Who writes it |
|---|---|---|
| `law-delta.schema.json` | The format of `law-delta.json` and `unused-articles.json`. | fixed (the author) |
| `law-delta.json` | The delta: missing articles, corrections for the corpus, corrections for the library. **One file from the start:** it grows during the first pass (it is an input of the store) and is trimmed in the second. | scripts, from the readings |
| `unused-articles.json` | Written in the second pass: the fetched articles that no final record cites, moved out of `law-delta.json`. Same schema as `law-delta.json`, with empty correction lists. The store still reads it, so trimming changes no store entry. | script |
| `readings.jsonl` | **Re-readings:** one line per fresh page reading of an article that the library or the corpus already holds. An input of the store; `verified` is computed from it. Format below. | the comparison script, never by hand |
| `requests.jsonl` | Articles an agent asked for and did not find in the store. One line each: `{"code","article","askedBy","unit","why"}`. Two derivers of one unit run at the same time, so a deriver writes its requests to its own file, `rules/derivations/<a|b>/<unit>.requests.jsonl`, and a script merges those into this one; the reconciler and the verifier (one at a time per unit) append here directly. | the merge script, `quote-records`, the reconciler and the verifier |
| `inventory.json` | Every cited article: where it is, its state, how it was verified. | `inventory-cited` |
| `amendment-ledger.json` | What the Official Journal scan found for each instrument. | the scan ([../specs/official-journal-scan.md](../specs/official-journal-scan.md)) |
| `journal-scan/issues-<YYYY>.jsonl`, `journal-scan/SUMMARY.md` | The evidence list: every issue read, with its number, date, hash and matches. | the scan |
| `pages/` | **Not committed.** Page images live in `C:\Users\TheShadow\dz-core-research\pages\`; the delta and `readings.jsonl` record their hashes. | — |
| `REPORT.md` | The figures the brief asks for, and the instructions for the integrator. | the executor |

## What is already at hand

- **Official Journal PDFs in the repository** (`docs/official-law-texts/`): `75-58_Original_Version.pdf` (JO 78 of 1975, the Civil Code; its SHA-256 equals the corpus ledger's `A1975078.pdf`), `84-11_Original_Version.pdf` (the Family Code of 1984), `A2005015.pdf` (Ordinance 05-02), `A2008021.pdf` (the CPCA, JO 21 of 2008), `90-04_Original_Version.pdf` and `90-11_Original_Version.pdf` with their French editions `90-04_Original_Version_FR.pdf` and `90-11_Original_Version_FR.pdf` (images, no text layer), `A1991068.pdf` (Laws 91-28 and 91-29), `A1996043.pdf` (Ordinance 96-21), and the two consolidated editions of 2007, `75-58_wipo_Version.pdf` and `75-59_wipo_Version.pdf`. Use them before downloading anything; record their SHA-256 like a download's.
- **Codex's corpus tools and notes, for method only** (how Codex found issues, aligned Arabic and French pages, recorded its checks): `git -C C:\Users\TheShadow\dz-fable-core show 9b60990:tools/law-corpus/README.md`, the other files of `tools/law-corpus/` at that commit, and `docs/LAW-CORPUS-*.md` at that commit (`LAW-CORPUS.md`, `LAW-CORPUS-CPCA.md`, `LAW-CORPUS-CIVIL.md`, `LAW-CORPUS-FAMILY.md`, `LAW-CORPUS-STATE-REPORT.md` and the dated notes). Read them; never run them against the repository, never copy their output into the store.
- **Installed tools:** `pdftotext` (`/mingw64/bin`), `pdftoppm` and `pdfinfo` (MiKTeX), Python 3.12 (`C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`). No OCR engine is installed, and none is needed: the pages are read by models.
- **One door to joradp.dz.** Every download from the Official Journal's site, by the scan, a triage agent, a page fetch or the adversary, goes through the script `oj-fetch` ([../specs/official-journal-scan.md](../specs/official-journal-scan.md), "One door to joradp.dz"). Nothing else downloads from that site.

## Order of work

Step 2 runs in two passes around step 1, because a rule may only quote a verified text, and the list of texts to verify comes from the rules.

**First pass (before any derivation).**
1. Build the article store ([../specs/article-store.md](../specs/article-store.md)) and its tests, with an empty `law-delta.json` and `readings.jsonl`.
2. Start the Official Journal scan. Its downloads take a few hours; the triage and readings of its hits run for days, in the background of the other work. It starts first because "Which version?" below needs it.
3. **Seed list:** the articles the brief names (Civil Code 101, 119, 133, 179 to 181, 308, 309, 312, 321; CPCA 17, 32, 37 to 40, 426, 512, 515, 536 مكرر and مكرر 4, 800, 801; Commercial Code 173, 187 مكرر and the commercial-lease chapter; Family Code 53 (item 3); Law 90-04; Law 90-11 arts. 73 to 73-4), the "where to start" pointers of HANDBOOK section 6, ranges included (for example Civil Code 308 to 322, which holds 314, the article the evaluator's provisional counting names), and every article in `data/law/claims/*.json`. Run the inventory on it. Then, **now**, so that derivers find the texts in the store:
   - fetch and read what is missing (below, "Reading an article");
   - re-read on the page every CPCA and Family Code seed article, so that the critical use rule rarely waits; the 17 labour articles of the library (Codex); and every Civil or Commercial article found in the corpus only (below, "Re-readings").
4. **French sources for derivation B** (outside the repository, `C:\Users\TheShadow\dz-core-research\french\<code>\`): for each instrument, the French Official Journal issue or issues, through `oj-fetch`, and the two French PDFs already in `docs/official-law-texts/` (Laws 90-04 and 90-11). Text by `pdftotext` where the PDF has a usable text layer (issues from 2003 on), page images otherwise. Record each file's SHA-256. Where no French text exists for a chapter, say so in the list given to derivation B.

**During step 1.** Each deriver writes a line in its own requests file, and the reconciler and the verifier in `requests.jsonl`, for a text they need and do not find; a script merges the derivers' files into `requests.jsonl`. Serve the requests in batches, between derivations of different units, so that a deriver is rarely blocked. Every article read in this time goes into `law-delta.json` at once.

**Second pass (after reconciliation).** Run the inventory on the final records. Verify what remains, including the fresh page readings that the critical use rule asks for. Trim `law-delta.json` to **what the final records cite**: a fetched article that no final record cites moves to `unused-articles.json`. Corrections stay in `law-delta.json` whatever cites them. Rewrite each remaining article's `neededBy` (the final records that cite it) and `textAsOf`.

## Reading an article from the Official Journal (a missing article)

1. **Find the issue.** From the corpus ledger when the code is in it; from the scan's evidence list; from the instrument's own reference otherwise. Look in `docs/official-law-texts/` first. Otherwise download it with `oj-fetch` (address pattern `https://www.joradp.dz/FTP/jo-arabe/<YYYY>/A<YYYY><NNN>.pdf`; French: `/FTP/jo-francais/<YYYY>/F<YYYY><NNN>.pdf`).
2. **Render the page** with `pdftoppm -r 300 -f <page> -l <page> -png <pdf> <prefix>`, to `C:\Users\TheShadow\dz-core-research\pages\<code>\`. Keep the full page. When the first reader reports where the article sits (`where`), the script also renders its column at 600 dpi (`pdftoppm -r 600 -x -y -W -H`) and cuts the region of the article, with a margin and the next column when the article reaches the foot of its column, into close-ups of at most 1400 pixels a side and 1.1 million pixels (`reading.tiles`); the settler gets them. A larger image is reduced before the model sees it: a whole 600-dpi column reached the settler at about 185 dpi, no sharper than the page (13 November 2026). Every settlement made before the close-ups was looked at again with them.
3. **First reading: Sonnet 5.5** (prompt [../prompts/law-read-page.md](../prompts/law-read-page.md)), given the image paths and the article numbers only. One reading may cover every needed article printed on the same page or pair of pages. It returns each text as printed, paragraph by paragraph, and marks any word it could not read with `[؟]`.
4. **Second reading: Codex**, same prompt, same images (`codex exec` with the prompt first and `-i <image>` last; the call must pass the event-log rule of [../specs/codex-calls.md](../specs/codex-calls.md)). Neither reader sees the other's text, the library's text or the corpus's.
5. **Compare by script** (level 2, or 2o). Same letters: done. Otherwise, for each difference (of words or of paragraph division), **Opus 5.5** is shown the image and the two versions in question, labelled X and Y in random order (not which reader wrote which), and says what the page prints and why ([../prompts/law-settle-difference.md](../prompts/law-settle-difference.md)). Record it under `proof.differences`.
6. **French check.** Find the same article in the French edition of the same issue. From 2003 on, take it from the French PDF's text layer (`pdftotext`). Before 2003 the French pages are images: one Sonnet 5.5 reading with the same prompt and `{{language}}` = French. A script compares every number, period, amount and article number of the Arabic with the French («ستة (6) أشهر» ↔ "six (6) mois"). French numbering can differ: Commercial Code art. 187 مكرر 1 is "Article 187 ter" in the French publication (the corpus's scope records it); take such aliases from the corpus. Record the result in `proof.frenchChecked`, `proof.french`, `proof.frenchPdfPage` and `proof.numbers`. **A mismatch is not settled by choosing:** the Arabic text is the official one, so the Arabic stands; `frenchChecked` is false, the mismatch is written in the article's `note`, and `grade-records` treats every deadline that rests on the article as **interpretive**. The same holds, with the reason in `note`, when the French edition does not hold the article.
7. **Civil and Commercial Code articles: the consolidated-PDF proof.** Only where the wording is in the 2007 consolidated PDF (`75-58_wipo_Version.pdf`, `75-59_wipo_Version.pdf`): its version is dated on or before the edition and was still in force at that date. Compare at **level 2** (the consolidated text modernises some spellings; `tools/law/build_codes.py` shows how the library finds an article's letters in that PDF: reuse its method, do not change the file). Record `proof.consolidatedPdf.status`: `sameLetters`, `differs` (each differing word goes to the settler with the Journal page, which decides; `note` says what the consolidated text prints), or `notInConsolidated` (a later wording, or an older one the edition no longer prints).
8. Write the article into `law-delta.json` with its `proof` (below, "The delta's article fields"). `textAsOf` comes from the scan.
9. Delete the issue's PDF once the article's readings and French check are recorded (the owner's rule: keep the hashes, not the PDFs). The page images stay outside the repository, with their hashes in the delta.

A reading with a `[؟]` mark is not a reading: re-render at a higher resolution and read again; if the word stays unreadable for two families, the article is not verified and every item citing it is grade D.

**Which version?** The wording in force today. If the scan or the corpus ledger shows a later amending text, read that text's own page (the article "as replaced by" it), set `version` to the amending publication, and name it in `amendedBy`.

**Two wordings of one article.** If the old wording still governs some cases (the commercial-lease reform of 2005 is the known case), write **two entries** in the delta, each with its `version` and its `governs` (which cases it governs, in Arabic and English, with `transitionalCite`, the transitional provision that says so). The older one has `supersededWording: true`. Rule records cite the version they mean by name (`cite.version`), and the store serves it. The older entry is **not merged into the app's code file**: the app's loader refuses a number that appears twice in a code. The reviewer decides how the library holds two wordings.

## The delta's article fields

As `law-delta.schema.json` has them now:

- `code`, `number` (canonical), `path`, `text` (as printed, paragraphs separated by a line break; modern spelling is **not** applied), `repealed`, `source` (`url`, `sha256`, `retrievedOn`), `note`.
- `amendedBy`: **one string**, as in the library, naming the amending text in Arabic (for example «القانون رقم 22-13 المؤرخ في 12 يوليو 2022»).
- `page`: as in the library, the page of the **source document** (for an Official Journal issue, the PDF page). The printed page of the Journal is `proof.printedPage`.
- `version`, `textAsOf`, `neededBy`. During the first pass, `neededBy` names what asked for the article (`seed`, `claims/<id>`, `request:<unit>`); the second pass rewrites it to the final records that cite it.
- `governs` and `supersededWording`, for two wordings of one article.
- `proof`: `pdfPage`, `printedPage`, `dpi`, `readings` (each with `by`, `model`, `date`, `imageSha256s`, `textSha256`, and `codexLog` for Codex), `agreement` (`twoAgree` or `settledByThird`), `differences`, the French fields of step 6, and `consolidatedPdf` (`status`, `note`) for the Civil and Commercial Codes.

For a Codex reading, `model` is `codex-cli <version> (default model)` unless the call's event log names a model.

## Re-readings: `readings.jsonl`

A fresh page reading of an article that the library or the corpus already holds does not go into the delta: it goes here, one line per reading, written by the comparison script. The store pins this file's hash and computes `verified` from it ([../specs/article-store.md](../specs/article-store.md)).

**Who reads, and why.**

| `why` | Which articles | Reader |
|---|---|---|
| `criticalUse` | a CPCA or Family Code article (`verifiedBy: corpusChecked`) cited by a deadline, a prior step, a section rule or an exit (the critical use rule of the store's specification); in the first pass, every CPCA and Family Code seed article | the family other than the corpus's checker (Sonnet 5.5, unless the version's checks name a Claude reading) |
| `corpusOnly` | a Civil or Commercial Code article the corpus has and the library does not | one reading, as above; two (one per family) when the corpus state is not `checked` |
| `labour` | an article of Law 90-11 or 90-04 in the library | Codex |
| `guard` | an article flagged by the guard (trap 3 of the store) | Sonnet 5.5, on the amending text's page |
| `disagreement` | a second reading asked because a first one differed from the store's text | the other family |

**One line:**

```json
{"code": "cpca", "article": "504", "version": "law-08-09", "why": "criticalUse",
 "reader": "claude-sonnet-5-5", "family": "claude", "date": "2026-11-15",
 "source": {"url": "docs/official-law-texts/A2008021.pdf", "sha256": "…", "pdfPage": 47, "printedPage": 45},
 "imageSha256s": ["…"], "textSha256": "…", "codexLog": null,
 "comparedWith": "corpus@9b60990", "storeTextSha256": "…",
 "level": "2",
 "differences": [],
 "result": "agrees"}
```

- `reader`: the exact model id (`claude-sonnet-5-5`; for Codex, `codex-cli <version> (default model)` unless the log names a model). `family`: `claude` or `codex`.
- `textSha256`: the SHA-256 of the text the reader returned, before any comparison. `codexLog`: for a Codex reading, the file name of the call's event log.
- `comparedWith` and `storeTextSha256`: the store text the reading was compared with. If that text changes, the line no longer counts and the comparison is run again.
- `level`: the level `text-compare` reached against the store's text: `"1"`, `"2"`, `"2o"`, `"2s"`, `"3"` or `"none"`.
- `differences`: one entry per difference, `{ "paragraph", "kind": "words" | "paragraphBreak", "store", "reading", "settled": "store" | "reading" | "neither", "by": "claude-opus-5-5", "seen", "sure" }`.
- `result`: `agrees` (level 1, 2, 2o or 2s; or every difference settled, sure, for the store's text); `storeDiffers` (a difference settled, sure, for the reading, and confirmed by the other family's reading: a correction goes into `law-delta.json`); `unsettled` (anything else: not verified, and the items citing the article are grade D).

**When one reading differs from the store's text.** Be modest: Codex's corpus was built with more care than any single reading here, and the library's texts were proved too. So: (1) a reading by the other family (`why: "disagreement"`), blind like the first; (2) each differing word goes to Opus 5.5 ([../prompts/law-settle-difference.md](../prompts/law-settle-difference.md)) with the store's words and the reading's words as X and Y in random order, unlabelled; (3) the store's text is held wrong only when the settler is sure for the reading **and** the other family's reading agrees with it on those words. Two families and the image, never one reader.

## Entry into force

Codex's corpus report records that **Law 22-13 art. 13 delays the new court rules (CPCA 32, 33, 531, 536 مكرر, 536 مكرر 4, 800, 801) until the new courts are installed**, and keeps cases filed earlier before the former courts (`git show 9b60990:data/law/corpus/cpca/report.json`, list `unsettled`). It is a lead, not a text:

- Read Law 22-13 art. 13 (and art. 15, which orders publication only) on its Official Journal page (JO 48 of 17 July 2022), by the readings above. The store's `inForceNote` for each article concerned quotes that reading; until then it carries the corpus's sentence, marked as a lead.
- The modules `court-sections` and `ordinary-court-exits` must answer it: their merged checklist has the line "Which courts and sections do CPCA 32, 33, 531, 536 مكرر, 536 مكرر 4, 800 and 801 as amended by Law 22-13 govern today, given Law 22-13 art. 13, and what applies to a court where the new courts are not installed?". The answer cites the page reading, or becomes an open point with the protective reading.
- Commercial Code 187 مكرر and 187 مكرر 1 carry a similar note (the date from which the 2005 rules apply); `commercial-lease-regimes` answers it the same way.

## A touched article found by the scan

When the scan's triage says a text touched an article that a record cites, the new wording is read **exactly as above**: Sonnet 5.5, then Codex, Opus 5.5 settles a difference; French check; a new delta entry with the new `version`. The triage only says where to look; it is not a reading.

## Corrections for the corpus

One entry of `corpusCorrections` per finding, in the shape of Codex's own conflict records so that Codex can apply it without translation. Give the exact wording the corpus should hold (the whole paragraph), the Official Journal source with the PDF page, and the readings that support it. Kinds: a wrong word, a version that is missing, a wrong status (in force / repealed), an article missing from a code the corpus calls complete, a wrong paragraph division, a state that is too high (`checked` for a text that differs from the page) or too low. For a kind without a new wording (`wrongStatus`, `stateTooHigh`, `stateTooLow`), `exactWording` repeats the paragraph as the page prints it, so that Codex sees what was compared. For `missingVersion`, `finding` names the version's introducing text, its date of entry into force as the text gives it, and its page.

A correction needs two families and the image ("Re-readings", above). A difference between one reading and a `checked` corpus text is **your reader's error until then**.

## Corrections for the library

Known already, confirmed on 12 November: **the library carries six CPCA articles that Law 22-13 repealed: 826, 835, 836, 912, 913, 914.** The store serves them as repealed. Cause: `tools/law/import_corpus.py` picks the version by `inForceTo` instead of taking the last version. For the integrator: the fix is in the importer, the regenerated `cpca.json` loses six articles, and the test that fixes the library's counts at (227, 1084) must be updated with the new counts.

For each correction and each delta article, `REPORT.md` says what the integrator must change and which tests guard it:

- A Civil or Commercial Code article goes through `data/law/curated-articles.json` and `tools/law/build_codes.py`, which writes its `sourceExtract` from the 2007 consolidated PDF. The delta's text is the Journal's spelling and `build_codes.py`'s fold keeps ى and ي apart, so the integrator types the consolidated PDF's wording there; an article whose `consolidatedPdf.status` is `notInConsolidated` cannot pass that proof as it stands, and the reviewer decides. A labour-law article goes through `data/law/transcribed-articles.json` and `build_transcribed.py`; CPCA and Family Code through the corpus import. **Articles are merged into the existing code file**; the library has one file per code id. An entry with `supersededWording: true` is not merged.
- The tests a correction can touch (`tests/DzLegal.Law.Tests/GroundsTests.cs`): the one that keeps Civil Code art. 139 out of the library; `EveryShippedTextIsProvedAgainstItsOfficialSource`, which checks the `sourceExtract` proof of every Civil and Commercial Code article **and pins the list of six code ids with their check methods**; the one that fixes the counts. Name the ones a correction touches.
- A new code id (a special law the rules need) needs `newCode` in the delta and, in the app: an entry in `data/law/sources.json` (`id`, `title` in Arabic and English, `citationName`, `reference`, `officialSite`, `joReference`); a code-level `check` and, for every check but `officialPdfText`, a `transcription` (the loader refuses a code without them); and a change to `EveryShippedTextIsProvedAgainstItsOfficialSource`, which pins the six ids. Unknown enum values break the app's loader; unknown extra keys do not.

## `REPORT.md`: the figures

- Cited articles in the final records: total, per code.
- Verified by script alone (agreement of independent sources), by a page reading (`readings.jsonl`), by a third reading; how many critical items waited for a page reading.
- Missing articles added; corrections for the corpus; corrections for the library; for each, the integrator's instructions (above).
- Articles still not verified, each with the items that cite it (all grade D).
- The entry-into-force notes and how the modules answered them.
- The scan: issues read, issues that could not be read ("blind"), the five known amendments found or missed, amendments found for cited articles.

## Done when

1. `law-delta.json` and `unused-articles.json` match the schema (the mini validator; add a test beside the frame's).
2. Every `cite` of every final record resolves in the store to a verified entry of the version it names (with a fresh page reading where the critical use rule asks for one), or its item is grade D and listed.
3. Every article in the delta has two readings by two families and a French check, and its page images' hashes are recorded; every line of `readings.jsonl` names its images and the store text it was compared with.
4. The scan re-found the five known amendments: Laws 05-02, 05-10, 07-05 and 22-13, and Ordinance 05-02 (and Codex's post-2003 ledger entries); if it missed one, the report says the scan is not trusted and every `ledgerClean` is false.
5. The PDFs are deleted and the evidence list is complete.

## Traps

- **The library's CPCA and Family Code are the corpus.** Their agreement is not a check. Which of their articles need a fresh page reading is the critical use rule of [../specs/article-store.md](../specs/article-store.md); do not restate it differently.
- **Consolidated texts on unofficial sites** (law portals, PDFs of commentaries) are tier 3: leads for finding the right issue, never a source.
- **An article number is not an identity across time.** Law 05-02 renumbered and replaced; «187 مكرر» exists only after it. Always state the version.
- **Arabic and French can differ.** The Arabic is official. Record the difference; never "correct" the Arabic from the French.
- **Old issues are scans.** A PDF "text layer" from the 1970s to 1990s is unreliable or absent, and the Arabic layers of 2005 and 2007 are garbled: read the image, never the layer.
- **Spelling is not a correction.** The Journal of 1975 and the 1990 laws print older forms; the consolidated edition and the library's labour texts use modern ones. A difference of hamza seats only is orthographic (level 2o) and is recorded, never corrected.
