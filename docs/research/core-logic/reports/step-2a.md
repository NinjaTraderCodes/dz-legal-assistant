# Step 2a report: the law texts before the rules (first pass)

13 and 14 November 2026. Branch `fable/core-logic`. Executor: Opus 5.5, following `HANDBOOK.md`.

**In one paragraph.** The articles the rules will cite are now in one store (1,873 entries). The seed list holds 273
articles, 186 of them in critical use. 270 of them are verified; the three that are not (in Law 90-04 and Law 91-29) are listed
below with the reason. The articles missing from the library were read on the Official Journal pages by two model families
and are in `law/law-delta.json` (122 entries). One finding changes how the work is done: the image viewer of the
agents shrinks large images, so the "higher-resolution" second looks were not sharper at all. All 57 earlier decisions
were looked at again at full resolution, and two of them changed. Of the things the texts themselves showed, two
matter for the rules: the official Arabic of **Law 90-04 art. 19** (labour conciliation) most likely carries a
misprint, and the library's text of **Law 91-29 art. 73** (serious misconduct) has one word that changes the meaning.
**The one question** is at the end: the go for the triage of the scan's hits.

The app was not run: nothing in it changed.

## 1. What is done

| Commit | What |
|---|---|
| `41c841b` | The rule evaluator, the `rules` tool and their 192 tests (pushed earlier). |
| `ad512b0` … `700752a` | Text comparison fixes; the settled readings of the missing 1975 and 1988 articles; Law 22-13 arts 13-15. |
| `4cfd65f` | Law 90-04 arts 10-40 and Commercial 182-202. |
| `da0afcb` | Settler close-ups at full resolution; every earlier settlement looked at again; the consolidated-PDF differences settled. |
| `203077f` | Two wordings of Commercial 169, 170 and 192 (1975, and as Law 05-02 rewrote them). |
| `641a505` | The record scripts of step 1 (`records.py`) and their tests. |
| `6282bf3`, `25c83b0` | Codex re-reads at 400 dpi; CPCA articles in critical use re-read by Codex; the seed inventory; scan years. |
| `aa0d9b2` | The Law 22-13 CPCA articles re-read by Codex; CPCA 39, 443 and 600 settled; 270 of 273 seed articles verified. |
| the commit of this report | This report; the scan to its end (`law/journal-scan/`). |

### The store and the seed list

- **The article store** (`research/core-logic/scripts/store.py`): 1,873 entries, keyed by code, article and version,
  built from the app's library, Codex's corpus at `9b60990`, and the delta. It builds twice to the same hash.
- **The seed list** (`law/seed-inventory.json`): every article the brief names, the pointers of the handbook's module
  table, and every article of `data/law/claims/*.json`: **273 articles, 186 in critical use** (a deadline, a prior
  step, a court or section rule, an exit from the ordinary courts). **270 of the 273 are verified**: 119 read on the page by both families (the delta), 101 re-read on the page by the family that had not checked them, 33 proved against the official consolidated edition (with or without the corpus), 11 labour articles confirmed by Codex against the library, 5 corpus articles re-read; one article outside critical use rests on the corpus check alone, which is allowed there.
- **Not verified, with the reason** (they stay grade D, "to check", in any rule that cites them):
  - Law 90-04 art. 19 ¶2: see finding 1.
  - Law 91-29 art. 73: see finding 2 (a correction of the library, not yet applied).
  - Law 91-29 art. 73-2: the page prints «لتصطحبه», the library «ليصطحبه», Codex read «لاصطحابه»; the settler is sure
    of «لتصطحبه». Same meaning; the library's word is not what the page prints, and the other family does not confirm
    the page's word.

### The articles that were missing (`law/law-delta.json`)

122 entries: Civil Code 51, Commercial Code 35, Law 90-04 33, Law 22-13 3 (a code new to the library). Each was read
by Sonnet 5.5 and by Codex on the Official Journal page; 96 agreed at once and 26 were settled by a third reading
(Opus 5.5, from the page). 118 pass the French check (every number of the Arabic is in the French edition of the same
issue); the 4 others are explained in their notes (findings 3, 5 and 6). For the Civil and Commercial Codes, every
entry was also compared with the official consolidated edition of 2007: 66 have the same letters, 17 differ, and at
every place where they differ the Journal page was read again and holds (the 2007 edition modernises spellings, adds
digits, or misprints: finding 8). Three entries are older wordings that the consolidated edition no longer prints.

Commercial 169, 170 and 192 have **two wordings** in the delta: the 1975 one (`supersededWording`) and the one Law
05-02 made of it, composed by script from the paragraphs 05-02 prints and the rest of the 1975 text («الباقي بدون
تغيير»). Law 05-02's art. 187 bis 1 keeps the renewal of a lease concluded before it under the law in force when the
lease was made; both entries say which cases they govern.

### Re-readings of the texts the library and the corpus already held (`law/readings.jsonl`)

`law/readings.jsonl` holds **206 re-readings**: 204 agree with the store (at the level of letters, hamza seats or spacing), 1 shows the store wrong (Law 91-29 art. 73, finding 2) and 1 stays open (art. 73-2). Every difference was settled from the page by Opus 5.5 at full resolution; the store was never changed by a single reader. The rule: a text in critical use needs a fresh reading of the page by a model family other than the one
that checked it before. For the CPCA and the Family Code that means Codex, because Claude checked Codex's corpus.

### The calibration: how many pages per run

The owner allowed up to 4 pages per run if a calibration showed that 4 pages do no worse than 1. On CPCA pages 35-38,
each family read the same 14 articles once page by page and once as 4 pages: **1 article in 14 differed in both
conditions, for both families** (art. 441, a matter of spacing between words), and 2 words in 498 were misread. So 4
pages are allowed for articles that are not in critical use; articles in critical use are always read on one page or
a pair. Caveat: one clean 2008 issue; the one real failure seen in practice was a page cut (art. 419 continues on the
next page), not a misreading.

### The finding about images (it changed the method)

The agents' image viewer reduces a large image before the model sees it. A whole 600-dpi column (2,409 × 6,533
pixels) or 800-dpi column reached the settler at about 185 dpi: no sharper than the 300-dpi page. So every "closer
look" before 13 November at 23:00 was not closer. The settler now gets **close-ups cut in tiles of at most 1,400
pixels a side** (`reading.tiles`), which it sees at full size, and opens only the one or two that hold the line.
All 57 earlier decisions were looked at again that way (9 Opus runs): 2 changed (Commercial 193 reads «استثمارات»,
confirmed by a third look at 800 dpi; CPCA 801 reads «الممركزة», the store's word, as Codex had read it), Commercial
191 became readable, and the rest held. Codex's own image handling seems to have a similar limit: at 400 dpi it still
could not read three words that the full-resolution close-ups show plainly.

### Scripts written for step 1

`research/core-logic/scripts/records.py` implements every script of `specs/record-checks.md`: `quote` (the script
quotes, agents only point), `check-values` (periods read from the Arabic words, with a tested table), `flag-neighbours`
(the uncited «غير أنه» paragraph), `merge-checklists`, `compare` (derivation A against B, with the id map and the
agreement rate), `compare-c`, `flatten`, `grade` (the grade table of `rules/GRADES.md`), `check-checklist` and
`inventory`. Their 25 tests plant the errors the handbook lists (a changed digit, a period the text does not say,
limitation and forfeiture swapped, a repealed article, an uncited exception paragraph) and all are caught. The two
example records were rebuilt with these scripts: same items, same quotes, same content; the grades rose from D to B
for the texts re-read since the examples were written, which is the store changing, not the scripts. The rebuild also
showed that CPCA art. 32 as of Law 22-13 still needed a reading by the other family; it now has it.

### The French sources for derivation B

`C:\Users\TheShadow\dz-core-research\french\` (outside the repository, with `INDEX.md`): the Civil Code of 1975
(pages 4-12, images; the PDF has no text layer) and Law 05-10 (text), for the formal-notice module that step 1 starts
with; the commercial-lease chapter of 1975 and Law 05-02; the two French labour laws. The CPCA, the Family Code and
Law 22-13 are listed as still to copy before their units.

### The Official Journal scan

The French edition of every issue from 2003 to the issue of 10 November 2026: **1,944 issues**, downloaded one at a time through the one fetching script, with a pause between downloads. It **re-found all five known amendments** (Laws 05-02, 05-10, 07-05 and 22-13, and Ordinance 05-02), the test the specification sets before the scan is trusted. Eleven downloads failed on the network the first time (2003: 7; 2004, 2012, 2013 and 2026: 1 each) and were read on a retry.

**Unread issues, by year: 2005: 2 (nos 27 and 67); 2025: 1 (no 66); every other year: 0.** They are listed as unread, never as "no match", and are read from page images by the triage. The masthead date of two readable issues (one of 2006, one of 2014) could not be read either. Until those five are read, the date up to which a text counts as searched for amendments (`textAsOf`) stops before the first of them, in spring 2005.

The hits: 131 in titles, 414 in operative articles, 601 others. **163 issues have a title or operative hit**, and 161 more have only other hits. The title and operative hits fall on the labour relations law (191), the Commercial Code (123), the Civil Code (100), the CPCA (81), the Family Code (31) and Law 90-04 (19). The evidence is in `law/journal-scan/` (`SUMMARY.md`, and one line per issue in `issues-<year>.jsonl`).

## 2. Findings in the texts

1. **Law 90-04 art. 19 ¶2** (labour conciliation is optional when the defendant is abroad, or the employer bankrupt).
   The Arabic page prints «عندما **يقدم** المدعى عليه خارج التراب الوطني»; the French edition of the same law prints
   «lorsque le défendeur **réside** en dehors du territoire national»; the library has «يقيم» (resides). The settler
   is sure of «يقدم» at full resolution; Codex could not read the word at 300 or 400 dpi. Most likely a misprint in
   the official Arabic, which the library silently corrected. The article stays unverified, so the conciliation rule
   resting on it warns with the mark "to check" and never blocks.
2. **Law 91-29 art. 73** (amending Law 90-11; the list of serious misconduct): the page prints «… بالتزاماته المهنية
   **أو التي** قد تلحق أضرارا بالمؤسسة»; the library has «**والتي**». With «و» the refusal must both concern the work
   and be harmful; with «أو» either is enough. Settled from the page (sure, at full resolution), and Codex reads the
   same. A correction of the library, for the second pass of step 2.
3. **Law 90-04 art. 28**: the Arabic has a second paragraph («يبلغ المدعي خلال ثمانية (8) أيام من تاريخ اتخاذ
   القرار»); the French edition has only the first. The Arabic stands; the 8-day period is not confirmed by the French.
4. **Law 90-04 art. 25** prints «ومتدرب **يقبل** مرتبه عن ضعف الأجر الأدنى المضمون»; the French reads «dont le salaire
   est **inférieur** au double du SNMG», which needs «يقل». A likely misprint; the delta keeps the print and says so.
5. **Commercial Code art. 198** cites «المادتين 196 و 197» in the Arabic and «194» in the French; the 2007 consolidated
   edition prints «المواد من 194 الى». **Art. 189** prints «المادة 88» where the French has «article 188».
6. **Commercial Code art. 178** (French): a digit is illegible even at 400 dpi; the Arabic stands, unchecked against
   the French. One word of the Arabic that Codex could not read («للاجراء») is taken from the official consolidated
   edition, as the owner decided for art. 171, and the note says so. **Art. 171** (a blotted digit, «169») likewise.
7. **Codex's corpus ledger**: Law 88-14 is listed as touching a «324 مكرر» that does not exist; Law 05-10 is listed as
   touching «324 مكرر 4» where it amended «324 مكرر 3» (the consolidated Civil Code's footnotes say so). The
   catalogue cites no article that does not exist.
8. **The 2007 consolidated editions** differ from the Journal pages in 17 articles (Civil 311, 312, 324 bis 2, 326, 330,
   337, 341; Commercial 170, 175, 182, 187, 191, 193, 196, 198, …): added digits, modernised spellings, and some
   misprints (for example «طرف» for «طرق», «مفعول» for «معقول»). In every place the page was read again; the delta's
   text is the page's, and each entry's note says what the consolidated edition prints.
9. **Law 22-13**: art. 13 says the new jurisdiction rules of the administrative and specialised commercial courts
   apply from the installation of those courts (cases filed before stay where they are); art. 14 repeals CPCA art. 32
   paras 7-10 and arts 826, 835, 836, 912-914. The library still carries those six repealed CPCA articles.

## 3. What is uncertain

- The two misprints (findings 1 and 4) are readings of the official page against the French; a lawyer may know of a
  rectification published later. The triage of the scan's hits would find one.
- The calibration rests on one clean issue (see above).
- The 185-dpi figure is the settler's own estimate, consistent with the image sizes; the fix does not depend on it.
- One run of the script tests failed once (1 of 92) and never again in seven runs; the test was not identified. The
  load-sensitive app test `WaitingWorkflowTests` failed once under load and passes alone and on an idle CPU.

## 4. Counts

- **Codex:** 50 page readings, 49 accepted, 1 discarded for an error event and sent again. The daily read probe ran before each day's first call (13 and 14 November) and was rejected by the event-log check, as it must be; the Codex CLI stayed at 0.167.0. Nothing from a discarded call was used.
- **Claude:** 70 page-reading runs (Sonnet 5.5; Arabic and French pages; the calibration's 5 included) and 24 settle runs (Opus 5.5), of which 9 were the full-resolution re-check.
- **Tests after the last commit:** root solution 1,038, research solution 33 and 192, scripts 117; both solutions also build with SDK 10.0.115.

## 5. Next

The second pass of step 2 waits for step 1's records (it delivers only what the final records cite). Step 1 starts
with the formal-notice module alone, as decided; the terms report is written during step 1 and is the next stop point.

## The question

**May I start the triage of the scan's hits?** By the specification, each issue with a title or operative hit, and each unread issue, is read by a Sonnet 5.5 agent, which downloads the French and the Arabic issue through the one fetching script and says exactly what the text changes. One run takes one issue, or a few issues of the same month.

- **All of it:** 163 issues in 125 runs, plus 5 short runs for the three unread issues and the two unread dates: **about 130 runs**. I estimate 60,000 to 100,000 tokens a run, so roughly 8 to 13 million in all, spread over days in the background of step 1. No Codex call.
- **A smaller first wave:** the Civil Code and the CPCA only (the formal-notice module and the court modules come first): 65 issues in **58 runs**, plus the 5 short runs above; the rest after step 1's first records.

Either way, the 161 issues with only other hits are first looked through by me from the sentences the scan matched, and only those that are not plainly decrees applying a law go to triage; I will give you that number before they run.

## Addendum: the owner's decisions of 14 November 2026, applied

- **Law 91-29 art. 73 is settled**: the page (Opus 5.5, full resolution), Codex and the French edition of the same issue
  («liées à ses obligations professionnelles **ou** celles dont l'inexécution pourrait porter préjudice», JO 68 of 1991,
  p. 7 of the PDF) all give «أو التي»: the conditions are alternative. The library correction is in
  `law/law-delta.json` (`libraryCorrections`), the store serves the corrected wording, and the article is verified.
  The seed list now has **271 of 273** verified.
- **Law 90-04 art. 19 ¶2** stays as printed, with the probable misprint recorded: `law/text-disputes.json` makes every
  rule item resting on that paragraph grade C, with both readings kept.
- **Triage**: the first wave (Civil Code and CPCA, 58 runs, and the 5 short runs for the unread issues and dates) is
  approved; the other codes are triaged before the first module or record that relies on each. `law/triage-status.json`
  keeps every item resting on a code whose triage is not complete below grade A (reason `triageOpen`).
- **Art. 73-2** stays open: the French («se faire assister d'un travailleur de son choix») does not decide between the
  two Arabic forms, which mean the same.
