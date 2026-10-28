# Separate official-law corpus: delivery record and integration contract

Research cut-off: **25 October 2026**. Branch: `codex/law-corpus`, isolated from the application engineer's default-branch work. This is a separate, additive corpus. The existing `data/law/codes`, claim mappings, loaders and application behavior have not been switched to it.

For current branch counts, start with **Current coverage (28 October 2026)** below. The [state report dated 26 October 2026](LAW-CORPUS-STATE-REPORT.md) is a historical checkpoint, superseded for the CPCA by delivery 2. Older acquisition and execution notes below retain their original dates and are not current delivery counts.

## Recovery and Family preparation, 28 October 2026

**Resumed after the safe pause.** The [Family source-check report](LAW-CORPUS-FAMILY.md) records 37 replaced/inserted body comparisons settled against the amending JO (eight guarded corrections), plus all 190 unchanged bodies word-checked against the current Ministry print (128 OCR agreements and 62 image-resolved bodies). Original-JO collation is now finished: 14 strict agreements and 176 actual article-image readings, yielding 171 edition-orthography matches, one guarded correction (42), and four unresolved official-edition conflicts (135, 151, 202, 203). These conflicts are explicitly quarantined. The typed candidate contains **232 identities / 232 current snapshot records: 228 partly-checked, four unsettled**. All 27 French paragraph-count flags have image decisions; art.178's page continuation is joined, and 18 bodies retain punctuation notes. Final amendment, paths, temporal and delivery QA remain open. Inherited approximate agreement must not be described as uniform exact-word verification. No finished Family delivery is claimed.

Historical safe pause: [the recovery instructions](LAW-CORPUS-RESUME-2026-10-28.md) record the 480 hash-bound readings retained at that point. Those are OCR-call counts, not articles. The base reread has since finished; the Family report above supersedes its partial status.

Codex recovered Claude's local project transcript and uncommitted files after the session limit. The two requested additions were already committed and pushed: `9636344` adds the four Law 22-13 spot-checks, and `eb43a21` implements the glyph-specific Family/Civil decoder. Neither addition has been repeated or overwritten.

- Both saved Family OCR runs finished: 40 column crops in Tesseract and PP-OCRv5. All 40 Tesseract crop artifacts replayed their hashes. These are extraction results, not checked article bodies.
- The glyph cross-check also finished: 32 sampled Civil compilation pages and 10 Family pages. Its word-presence comparison is a diagnostic, not a position-aligned verification. Source-image inspection supports the three Civil changes for which that diagnostic preferred the raw reading; a nearby legitimate «آل» remains unchanged. Sampling includes Civil annex material for font testing only. [Bound source hashes, OCR tallies, image decisions and limitations](../data/law/corpus/research/family-2026-10-28/glyph-validation.json).
- Family preparation now recovers **232 distinct identities: 224 base labels and eight insertions**, with no duplicate or missing label relative to the independently collected source inventory. It previously treated «8 مكرر :1» as another «8 مكرر» and included the signature after art. 224; the printed pages settle both boundaries. A duplicate inventory now stops preparation.
- The prepared file is explicitly **source candidates only**. Heading paths, paragraph structure, amendment metadata and bodies still require checking. There is no new typed Family delivery or `checked` state yet.
- Initial comparison of the **190 apparently unchanged base bodies** with the two 1984 JO OCR streams finds 77 agreements under `native_check.compare_streams`, with 238 unconfirmed token groups and 46 extra-text groups across the others. Missing amended bodies can affect alignment; these are review tasks, not 113 proven legal-text errors. The 37 annotated replacement/insertion candidates and five repeal markers need separate 2005-source checks.
- The corresponding initial 2005 comparison agrees on 16 of the 37 annotated bodies; 36 unconfirmed groups and one extra-text group remain. Reproduce both runs with `tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/compare_family_current.py`. No result from this command grants a verification check.
- The inherited baseline passes: Release build 0 warnings/errors, all 687 .NET tests and all 116 Python tests (including seven new boundary/crop regressions). The Ministry index live re-fetch timed out; this recovery does not claim a new amendment-discovery cut-off beyond the cached index.

Files outside the corpus/tool directories changed in this recovery: this report only. No application code, old article library, claim catalogue, prompts, loaders or safety behavior was changed. The half-day font investigation has produced its bounded mapping; further work proceeds to body comparison rather than another general font project.

## Current-first delivery order, authorized26October2026

The user approved **complete current codes first**, with comprehensive historical reconstruction deferred. Article/chapter scope is unchanged. Preserve express transitions and older provisions that still govern supported cases; missing required historical law must remain explicitly unsettled. The already collected historical work is retained. [Recorded authorization and boundaries](../data/law/corpus/research/delivery-order-2026-10-26.json) supersede the earlier delivery order below.

Direct Ministry HTTPS downloads work even when browser retrieval times out. Bulk acquisition now uses official native text before OCR:

| Source acquisition | Records collected | Remaining before current-law delivery |
|---|---:|---|
| CPCA Arabic Ministry HTML,2008 base |1,065 articles;0 failures; every source hash replayed offline|Apply22-13 and inspect publication/OCR discrepancies and current inventory.|
| Civil Arabic SGG2007 compilation linked by the Ministry |1,058 raw blocks;1,049 distinct printed-number candidates|Decode the bad embedded font text, separate headings/annotations, resolve duplicate numbering and complete insertion/quality checks.|
| Family bilingual Ministry compilation incorporating05-02 |232 raw Arabic blocks; all224 base labels and8 inserted candidates; no duplicates|Decode the shared font errors, separate annotations/page labels and confirm current amendments, paragraphs and source accuracy.|

These are **2,355 source records/blocks, not2,355 distinct checked current articles**. The separate typed integration prototype still has49identities/59versions. No complete current code is certified. Batches and source metadata are in [research/current-batches](../data/law/corpus/research/current-batches/acquisition-summary.json); raw PDFs/HTML stay ignored.

The user identified the already present `docs/official-law-texts/75-58_wipo_Version.pdf`. It had not supplied this bulk pipeline before this checkpoint. A raw PDFium comparison of all268pages now finds identical extracted Unicode to the Ministry's Civil PDF, despite different file hashes. The WIPO file is a useful alternate copy of the same text, not an independent witness or a solution to the common font errors: PDF8 visually prints `كامل`/`كل`, while extraction emits `آامل`/`آل`. [WIPO's catalogue](https://www.wipo.int/wipolex/en/legislation/details/14774) identifies the edition as amended through07-05 of13May2007. Local and Ministry source hashes, per-page text hashes and the bound image review are retained in [civil-wipo-comparison.json](../data/law/corpus/research/current-batches/civil-wipo-comparison.json). No global letter substitution has been applied.

The Family compilation was initially misclassified as French only from an incomplete text sample. Visible pages4,5and25 establish that it is bilingual: French on the left, Arabic on the right. The corrected source ID is `family-consolidated-bilingual`; PDF bytes and hash are unchanged. The collector uses unrotated crop coordinates for the rotated document, checks that all Arabic glyph boxes remain inside its selected column, and stops at the first224 on PDF43. It excludes the later annexes. Source-image inspection resolves the candidate8bis1 label and confirms131, which PDF extraction joins to the preceding line. These are acquisition/layout checks, not passed article-transcription checks.

The CPCA overlay inventory isolates69 replacement/partial-replacement targets,19 insertions,6 whole-article repeals and the partial repeal of article32. The989 base bodies outside those direct text changes still need the normal source/quality checks. HTML paragraph elements are not assumed to equal legal alinea numbers. Law22-13 article13 preserves earlier jurisdiction rules for earlier-filed cases and ties new jurisdiction provisions to installation; this is a concrete historical exception retained in phaseOne. See [the source-bound overlay plan](../data/law/corpus/research/current-batches/cpca-22-13-current-overlay-plan.json).

Civil label discovery accounts for1,002 original base labels; the absent41 has a separate explicit repeal instruction. That is number evidence, not body verification. Duplicate candidates are retained, never overwritten. Compatibility decomposition is used only to locate PDF labels and map back to raw offsets. No broad Arabic letter repair is applied to the extracted body. Historical footnotes are separated where the source provides a literal rule; full raw pages are retained for review.

## Revised scope and resumed execution

The article scope remains **complete Civil Code, complete CPCA including Law22-13, and complete Family Code**; Commercial and Laws90-11/90-04 cover **only claim-catalogue articles and their full chapters**. Complete history is now deferred to phaseTwo, with legally applicable exceptions retained in phaseOne. Special laws are excluded. There is no dependency on Djihane or an external legal reviewer.

The reproducible scope snapshot is [research/scope-plan.json](../data/law/corpus/research/scope-plan.json); `tools/law-corpus/scope_inventory.py --check` checks the frozen catalogue hashes and chapter inventory. The full-code original base numbering totals 2,292. This is not the final count after insertions, repeals and historical identity reconciliation.

| Partial collection | Catalogue seeds | Entire covered units | Initial bounded identities |
|---|---|---|---:|
| Commercial |30,173,176,187bis;187bis1 explicitly required|BookI/TitleIV/ChapterI (30); BookII/TitleII, **all seven commercial-lease chapters** (169–202,187bis,187bis1)|37|
|90-11|73,73-4,80,88|TitleIII/ChapterVI (62–74 and73-1…73-6); TitleIV/ChapterI (80–86); TitleIV/ChapterIII (88–90)|29|
|90-04|19|TitleIV/ChapterI, conciliation-bureau competence; **the whole chapter consists of article19**. Article20 begins the next chapter.|1|

These 67 targets are a boundary snapshot, not a certificate that no historical amendment inserted another article within a covered unit. Historical numbering/boundary changes must be reconciled before the inventory becomes final. The commercial-lease unit deliberately includes the whole title as the user's explicit example requires; it is not limited to the chapters containing173/176. The partial designation remains even after every selected chapter is delivered.

Source boundaries were inspected against official Arabic1990 publications and the Ministry's dated French Commercial2007 compilation. That old compilation supplies structure and discovery only; it is not asserted to be current law. All acquired files and failures are recorded in [scope-source-downloads.json](../data/law/corpus/research/scope-source-downloads.json). A guessed Civil2007 filename returned404 and supplies no publication or article text.

The new consolidation machinery checks the prior text hash before applying explicit full or paragraph replacements, paragraph insertion/deletion, article insertions, repeals, heading changes and renumbering. It preserves ordered paragraph provenance across publications, retains unknown commencement dates and produces an operation audit. It does not infer legal operations, change the old application loader, or grant a verification check merely because an operation executed.

`chapter-subset`, typed covered-chapter lists and explicit partial/complete reports are now supported. A multi-publication version needs provenance for every body paragraph; source PDF and paragraph text hashes are checked. A bound independent inventory review is required before composition can produce `checked`; changing its ledger, article text or version boundaries invalidates that review. French structure remains a separate check.

Full Civil source extraction uses `civil-full-manifest.json`: original Arabic/French publications plus all five indexed amendments,312 column windows. The first full pass exposed shifted/slanted1975 column separators and inaccurate initial French amendment-window hints. Pixel-derived polygons now preserve each column, with proposals/hash records in `research/civil-layout-decisions.json`. Image inspection corrected French83-01 toPDF3–5,88-14 toPDF7–8, and89-01 toPDF3–4. The correction pass and hash replay are complete, and all312 evidence records are exported in `research/civil-full-extractions.json`. Anchored original-number OCR observes883Arabic and798French candidates, with19numbers absent from both streams; these are recognition issues, not declared legal gaps. Geometry/OCR success does not establish article accuracy.

Historical execution checkpoint, 26 October (superseded by deliveries 1 and 2 below): scope boundaries and consolidation infrastructure had progressed, but no complete code was delivered. The typed prototype then contained 49 identities and 59 versions: 42 partly checked, 17 unsettled, 0 checked. Civil included originals1–32, priority/temporal records and the489 pilot; most later versions still needed consolidation. The current coverage table below replaces these historical counts.

The earlier489 pilot inspected original ArabicPDF31 and2007 ArabicPDF5, with FrenchPDF25/2007PDF4 for structure. Law07-05 article6 replaces the first paragraph and explicitly keeps the remainder; the new source fragments retain the original second paragraph. Historical Arabic headings and the exact generic commencement boundary remain unresolved. The data does not convert the amendment placeholder into an article paragraph. See `research/civil-pilot-amendments.json` and `research/civil-pilot-consolidation-audit.json`.

## Delivery status

### Current coverage (28 October 2026)

| Legal text | Article identities | Versions | Checked | Partly checked | Unsettled | Scope | Full code complete |
|---|---:|---:|---:|---:|---:|---|---|
| CPCA | 1,084 | 1,162 | 1,162 | 0 | 0 | complete code, Law 08-09 + Law 22-13 | **Yes** (175 versions with an unsettled temporal boundary, see below) |
| Civil Code | 45 | 55 | 7 | 32 | 16 | priority subset (7 targets) | No |
| Commercial Code | 2 | 2 | 2 | 0 | 0 | priority subset (2 targets) | No |
| Family Code | 232 | 232 | 0 | 228 | 4 | current snapshot candidate; five repeals retained; four edition conflicts; history deferred | No; final QA and all temporal boundaries remain open |
| Labour 90-11 | 0 | 0 | 0 | 0 | 0 | — | No; not started |
| Labour 90-04 | 0 | 0 | 0 | 0 | 0 | — | No; not started |

What remains: final Family QA, Civil Code, Commercial (37), Labour 90-11 (29) and 90-04 (1). All 81 legacy records are accounted for in the reconciliation below; 47 still lack imported corpus text. The [Family 20-article image sample](law-corpus-checks/family-code/README.md) is complete for the current candidate, without promoting its verification states.

The [Civil preparation report](LAW-CORPUS-CIVIL.md) now records 1,059 decoded source candidates across164 code pages, with no undecided glyph assignments. Ten locally numbered management-contract articles are kept separate from general provisions1–10; original JO89-01 image inspection confirms that insertion structure. These candidates are not a replacement typed delivery. Current-edition OCR is running on164 guarded body crops; the old1975 OCR diagnostic and its many unresolved groups remain retained separately.

The Family amended-body paragraph review is also complete for all37 bodies. It corrected the source locator for3bis (a duplicate sentence had matched a neighboring nationality provision) and removed an extraneous next-page locator from57bis. Wording and verification counts are unchanged; the [Family report](LAW-CORPUS-FAMILY.md) records both guarded provenance fixes and the remaining delivery tasks.

### Delivery 2 — CPCA, complete and current (28 October 2026)

All 1,162 versions of the CPCA are `checked`. Details are in the [code report](LAW-CORPUS-CPCA.md); the spot-check is in `docs/law-corpus-checks/cpca/`.

**Text sources.** Law 08-09 comes from the Ministry HTML (1,065 articles), compared with two OCR readings of JO 21/2008. Law 22-13 comes from the JO 48/2022 Arabic text layer, cleaned by tested transforms and compared with its own print. Without any image decision, 788 of 1,065 and 74 of 90 agree. Every other difference in words, digits or negations was read on the page image: 561 base decisions and 42 decisions for Law 22-13.

**Main finding: the Ministry HTML has lost every colon.** The review restored 80 colons and 40 full stops from the print, plus 10 spacing/abbreviation fixes. In art. 1065 the HTML includes a heading and the signature block, which were removed.

**Punctuation rule (28 Oct).** Punctuation-only differences do not block `checked`. Unsettled marks are listed in the version's transcription note: 15 versions, 16 marks.

**Law 22-13 consolidation.** `consolidate.py` applied every change with a hash guard and an audit: 60 replacements, 10 paragraph edits (the 9 partial restatements and art. 32 losing paragraphs 7–10), 19 insertions, 6 repeals and 2 heading-only changes (532, 536). The renamed headings of law arts. 9 and 11 are applied to the affected paths. No amending text later than 22-13 is listed by the Ministry index.

**Temporal limit.** Law 22-13 has no calendar commencement date, and its art. 13 ties the new jurisdiction rules to the installation of the new courts. Its 97 versions and the 78 they replace therefore carry `temporalStatus: unsettled`, each listed in the report.

**Gate.** Release build with 0 warnings, all .NET tests and the Python corpus tests pass, the schema validates and the typed loader loads the complete code.

### Delivery 1 — priority articles, current version, `checked` (26 October 2026)

All eleven priority articles are `checked` in their current version: Civil 124, 138, 139, 140, 308, 309, 379; CPCA 2, 504; Commercial 187 مكرر, 187 مكرر 1. Each passes the four checks the loader requires and the brief's six conditions:

1. **Pinned source:** the introducing JO issue (official URL and SHA-256 in the ledger), page and crop evidence id.
2. **Text:** read by Claude from the official page image (150–1,200 dpi renders of the pinned PDF), word by word, every digit, negation and punctuation mark, recorded as `discrepancy-resolved-from-image` bound to the text and image SHA-256s. This is an AI reading, not lawyer verification; `verified`/`verifiedBy` are untouched.
3. **Inventory:** priority-subset inventory, identity present in the introducing publication and the current Ministry source, no repeal in the ledger.
4. **Amendments:** every amending text listed by the Justice Ministry codes index (fetched 26 Oct 2026, page SHA-256 in `research/amendment-discovery-2026-10-26.json`; the index lists texts up to 12 Oct 2026) is in the ledger; the check lists the texts touching the article and confirms none later than its introducing text does.

**Corrections found by re-reading the images** (the earlier candidate texts had silently modernised the 1975 spelling): Civil 309 «في ذمة» (dotted) and «آداؤه» (madda); 379 «آكد» (madda); 140 «مايلزم» (one word), «فان» (no hamza), and paragraph 2 ending with the printed comma «،» (kept as published; the shape matches the line's other commas, not the full stop). Civil 140's earlier quarantine (punctuation between الخطر and فان) is settled: none, a line break. Civil 124 is new: Law 05-10 art. 35, JO 44/2005 p. 23.

**Paths:** Law 05-10 art. 34 renamed Chapter III «الفعل المستحق للتعويض» and Section I «المسؤولية عن الأفعال الشخصية»; current versions of 124 and 138–140 carry these headings (Section III «المسؤولية الناشئة عن الاشياء» is unchanged).

**Temporal:** Civil 124 (Law 05-10) and Commercial 187 مكرر / 187 مكرر 1 (Law 05-02) are `checked` text with `temporalStatus: unsettled`: these laws set no national commencement date (Civil art. 4 day-franc rule per locality); listed in each report.

Evidence: `research/priority-current-review-2026-10-26.json` (image hashes and render notes), visual spot-check `docs/law-corpus-checks/priority/`, builder `tools/law-corpus/deliver_priority.py`.

State after delivery 1 (superseded for the CPCA by delivery 2):

| Legal text | Article identities | Versions | Checked | Partly checked | Unsettled | Scope | Full code complete |
|---|---:|---:|---:|---:|---:|---|---|
| Civil Code | 45 | 55 | 7 | 32 | 16 | priority subset (7 targets) | No |
| Commercial Code | 2 | 2 | 2 | 0 | 0 | priority subset (2 targets) | No |
| CPCA | 3 | 3 | 2 | 1 | 0 | priority subset (3 targets; 1062 not re-reviewed) | No |
| Family Code | 0 | 0 | 0 | 0 | 0 | — | No; not started |
| Labour 90-11 | 0 | 0 | 0 | 0 | 0 | — | No; not started |
| Labour 90-04 | 0 | 0 | 0 | 0 | 0 | — | No; not started |

The other Civil identities (originals 1–32, 489 pilot, 1002/1003) keep their earlier states and reasons; they are outside delivery 1.

**What remains:** CPCA complete and current (22-13 overlay), Family complete, Civil complete (font decoding of the 2007 compilation first), the bounded Commercial/90-11/90-04 collections, and the 81-article legacy reconciliation.

### Earlier history (before 26 October 2026)


**The revised corpus scope is not delivered.** The initial priority package contained all ten requested priority identities plus four temporal provisions: **14 article identities and 15 version candidates**. The original and2005 versions of Civil article7 are separate records. This is a delivery-history count; subsequent checkpoint counts are reported separately. It is an explicitly bounded priority subset, not an inventory of a complete code.

| Legal text | Corpus article identities | Versions | Checked | Partly checked | Unsettled | Full code complete |
|---|---:|---:|---:|---:|---:|---|
| Civil Code | 44 | 54 | 0 | 37 | 17 | No |
| Commercial Code | 2 | 2 | 0 | 2 | 0 | No |
| CPCA | 3 | 3 | 0 | 3 | 0 | No |
| Family Code | 0 | 0 | 0 | 0 | 0 | No; not started |
| Labour 90-11 | 0 | 0 | 0 | 0 | 0 | No; not started |
| Labour 90-04 | 0 | 0 | 0 | 0 | 0 | No; not started |

(Superseded by delivery 1 above.) The priority inventory has no missing target identity. Whole-code inventory and amendment-application certification have **not** passed. Accordingly no version is `checked`. Passed transcription checks are bound to exact text and reviewed images. The17 unsettled Civil candidates are4,6,8,10,12,13,15,16,17,20,22,23,25,26,28,140 and2005 article17bis. The former2005 article7 uncertainty has now been resolved from a renewed source-image reading. They must not be presented as exact checked source text. These states are not lawyer verification, and the original `verified`/`verifiedBy` fields are not set or changed.

The French comparison preserves two discrepancies: original10 has different paragraph blocks, and original13 cites11/12 in Arabic but12/13 in French. The actual Arabic cross-references are retained; neither publication is silently corrected. Article33 continues on another page and is deliberately excluded from this batch. `research/civil-original-6-32-structure.json` retains the observations.

The83-01 insertion inventory has been corrected from official source images:11 new identities, removing phantom750bis3 and restoring756bis and764bis. The earlier discovery list must not be reused. The law also has a separate heading-scoped terminology operation with an exception. Its source tokens and original743-772 heading boundaries have now been read, but paragraph-level replacements remain unapplied.

Priority coverage:

- Civil: 138, 139, 140, 308, 309, 379; temporal provisions 7 (two versions), 1002 and 1003. The resumed checkpoint adds489 with its original and2007 versions as a real paragraph-consolidation pilot.
- Commercial: 187 مكرر and 187 مكرر 1, inserted by Law 05-02.
- CPCA: 2, 504, and the commencement provision 1062.

The priority package is an integration checkpoint with explicit unresolved items. It is not permission to describe the larger corpus assignment, every historical version chain, or mechanical text verification as finished. The next requested delivery is the **complete Civil Code**, after the priority checkpoint's remaining verification work is addressed or explicitly accepted as unsettled data.

## Files and reproducible pipeline

Each loaded text has `ledger.json`, `articles.json` and `report.json` under `data/law/corpus/<id>/`. JSON Schemas are in `data/law/corpus/schema/`. The offline typed reader is `src/DzLegal.Law/Corpus/`; its unit fixtures are entirely fictitious and live in `tests/DzLegal.Law.Tests/Corpus/`.

The build-time tools are documented in [tools/law-corpus/README.md](../tools/law-corpus/README.md). They provide official-host allowlisted downloads with pinned hashes, separate PDF text-layer and image OCR extraction, conservative comparison, explicit transcription composition and JSON Schema validation. The .NET validator also checks corpus identities, intervals, references, publications and report counts.

Typical commands from the repository root:

```powershell
# Bootstrap pinned build-only tooling; see README for the isolated environment.
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py fetch tools/law-corpus/priority-manifest.json

# All priority crops; existing image decisions are bound to the recorded final run.
# Re-extraction can change image hashes: review/rebind decisions before composing.
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py run tools/law-corpus/priority-manifest.json --offline --dpi 150 --tessdata tools/law-corpus/.cache/ocr/share/tessdata

& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/apply_review_findings.py
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/generate_priority_specs.py
foreach ($code in 'civil-code','commercial-code','cpca') {
  & tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py compose "data/law/corpus/research/priority-specs/$code.json" "data/law/corpus/$code/ledger.json" --out "data/law/corpus/$code"
}
dotnet run -c Release --project tools/law-corpus/Validate -- data/law/corpus
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/reconcile_existing.py
```

`research/source-downloads.json` pins 26 official PDFs. The extraction manifest includes these sources and 30 targeted Arabic/French crops. PDF bytes, images, raw process streams and OCR tools stay in ignored caches. The final article records themselves retain both raw text-layer and OCR text. `research/ocr-priority-extractions.json` and `research/ocr-toolchain.json` preserve the crop/toolchain methods, versions and artifact hashes without committing PDFs or page images.

Re-extraction does not silently settle a discrepancy. It invalidates a review whose image/text hashes no longer match. Updating a downloaded publication's hash also requires an explicit provenance review, rather than accepting changed bytes under an old pin.

## Comparison and text quality

The comparison transformations are only Unicode NFC and collapsing whitespace runs for comparison. Raw strings are preserved. There is no `آ`→`ك` fold, Arabic letter replacement, digit-script conversion, diacritic deletion, negation deletion or punctuation removal in text verification. Canonicalizing article identities is a separate operation and does not alter article text.

At 300 dpi the Arabic OCR model unexpectedly skipped body lines in some small crops despite returning exit code zero. Re-running at 150 dpi recovered substantially more text. Both attempts were retained, with the final image profile recorded. An OCR success exit code is not a fidelity check. The final 30-crop evidence set still contains missing/disagreeing text-layer/OCR streams; the thirteen image decisions are separate recorded resolutions, not fabricated OCR agreement.

Original 1975 Arabic/French PDFs are image-only. Several later Arabic PDFs have corrupt font extraction. The two remaining Civil candidates are quarantined because exact glyph/punctuation agreement has not been established. Independent image checks resolved eight other Civil transcriptions, including visible dotless-yeh spellings and a hamza. A material phrase in amended article 7 was corrected to the printed wording while a narrower orthographic uncertainty remains explicitly unsettled. The before/after text and exact hashes are retained in `research/civil-image-review-findings.json`. Article 140 spans two PDF pages, and the punctuation after `الخطر` remains ambiguous. Its third paragraph is not silently omitted. Multi-crop source fragments retain their separate page, region, raw text and evidence identity.

The text field contains the article body and paragraph breaks, not its publication's outer quotation delimiters or article-label layout. Every such boundary decision is explicit in the image review record. `paragraphs` must reconstruct `text` exactly with LF separators.

French alignment checks presence, printed numbering/alias and paragraph count. It does **not** establish translation equivalence. In particular:

- Arabic Commercial `187 مكرر 1` corresponds to printed French `187 ter`; no missing French article or extra Arabic article is invented.
- Arabic CPCA 504 refers to delivery of the non-conciliation report, while the French wording refers to the report's date. The structural match must not be misrepresented as identical legal meaning.
- Arabic printed `1.002` and `1.003` have canonical identities `1002` and `1003`. The new corpus canonicalizer handles this documented grouping format without changing the legacy `ArticleNumbers` behavior.

## Temporal data and integration with the legal-core rebuild

**Do not substitute enactment or publication for commencement.** The original Civil Code's article 1003 expressly sets effect from **5 July 1975**, before enactment/publication. Article 1002 separately addresses the publication trigger for prescription periods. CPCA article 1062 prescribes one year after publication: the recorded anniversary is **23 April 2009**. A ministry narrative giving 25 February 2009 is recorded as conflicting secondary material; the publication's own operative provision takes precedence in this data record.

For laws without an inspected special commencement clause, the generic geographic rule in Civil article 4 has not been converted into a guessed uniform national date. Consequently the precise 2005 Civil article 7 boundary and the Commercial insertion's generic commencement remain null/unsettled. Commercial 187 مكرر separately supplies its express contract-publication applicability trigger. These distinctions are preserved in `research/temporal-findings.json` and the ledger's `commencementBasis`.

The `.NET` integration entry points are:

```csharp
using DzLegal.Law.Corpus;

LawCorpus corpus = CorpusLoader.Load(corpusFolder);
CorpusVersionResolution result = corpus.VersionOn("cpca:504", requestedDate);
```

`VersionOn` uses half-open recorded intervals `[inForceFrom, inForceTo)`. Its states are `missing`, `not-in-force`, `unsettled`, `partly-checked`, `resolved`, and `repealed`. A null start is unknown, not minus infinity. Unknown temporal bounds, an unsettled transcription, or a requested date after the discovery cut-off produce `unsettled` with no usable version. An interval-resolved but only partly checked transcription is returned as `partly-checked`, never as fully checked. Repeal is explicit; it is not silently replaced by the prior operative text.

The lookup identifies a recorded version; it does not decide which event controls temporal applicability to a case. The application engineer must inspect verification, transition records and unresolved reasons. Raw archive records remain accessible for research through `Codes`; that is not a bypass authorizing unsettled text for drafting.

Extensions beyond the requested minimum format are intentionally documented: `schemaVersion`, version IDs, `temporalStatus`, explicit corpus scope/cut-off, source fragments, and `availability: reference-only` for a future explicitly cited target whose text is not imported. A reference-only record contains no invented article text and cannot resolve to an operative version. The priority package does not need any such stubs because its explicit cross-reference target is already present.

## Amendment discovery and gaps

The Justice Ministry index was used for discovery and the official JO for text. Research records distinguish a whole-code amendment from an amendment that actually touches a priority article. None of the six originally requested Civil article texts was found replaced in the inspected listed amendments; Civil article 7 was replaced in 2005 and is recorded twice. Original chapter headings were changed by the 2005 reform even where article words were unchanged, so historical source headings must not be advertised as a newly reconstructed current code organization.

The complete amendment histories and complete inventories are not certified. `research/amendment-discovery.json` lists the indexed amendments, checked priority impact, uncached instruments and later official-domain discoveries. Four older Commercial amending instruments were not downloaded in the priority pass; their unknown hashes are not invented. The 2015 and 2022 corporate-law changes were inspected for priority impact. CPCA 22-13 is represented in its ledger, without implying that the 2008 edition alone is consolidated current law.

A 2026 implementing decree concerning specialized commercial-court territorial organization was discovered and recorded as outside this article-text subset. It must not be treated as an amendment to 187 مكرر, nor does this corpus claim that a 2022 jurisdiction description is current merely because a statutory amendment was indexed.

Every non-checked version has reasons in its code's `report.json`. Scope-level gaps remain in `ledger.scope.unsettled`. Empty **priority** target gaps do not mean a complete code. The three complete-code deliveries and three bounded partial collections remain outstanding. The user has excluded the special-law inventory and build.

## Official sources and copyright

Primary Arabic publications:

- [Civil Code, JO 78/1975](https://www.joradp.dz/FTP/jo-arabe/1975/A1975078.pdf), especially PDF pages 2, 9–10, 19, 24 and 67; [05-10 amendment, JO 44/2005](https://www.joradp.dz/FTP/jo-arabe/2005/A2005044.pdf), PDF page 18 for article 7.
- [Commercial Code, JO 101/1975](https://www.joradp.dz/FTP/jo-arabe/1975/A1975101.pdf); [05-02 insertion, JO 11/2005](https://www.joradp.dz/FTP/jo-arabe/2005/A2005011.pdf), PDF pages 8–9.
- [CPCA, JO 21/2008](https://www.joradp.dz/FTP/jo-arabe/2008/A2008021.pdf), PDF pages 3, 41 and 95; [22-13 amendment, JO 48/2022](https://www.joradp.dz/FTP/jo-arabe/2022/A2022048.pdf).
- [Justice Ministry code/amendment index](https://droit.mjustice.gov.dz/fr/content/codes-0), used as an amendment-discovery aid, not substituted for published article text.

**Copyright confirmation:** [Ordinance 03-05, official Arabic JO 44/2003](https://www.joradp.dz/FTP/jo-arabe/2003/A2003044.pdf), **article 11 on PDF page 5**, excludes the listed official legal texts and their official translations from the copyright protection provided by that ordinance. The corresponding [official French publication](https://www.joradp.dz/FTP/jo-francais/2003/F2003044.pdf), PDF page 4, was cross-checked. This supports the official statutory text corpus; it is not a blanket license for private annotations, commercial databases or build tools. Exact provenance and the provision's wording are recorded in `research/temporal-findings.json`.

No unofficial source supplies an article in this corpus. The existing application's old PDF provenance is reconciled as legacy data, not adopted as a new allowed corpus source.

## Reuse, edits outside the new folders, and build boundaries

Reused: the official downloads already held in the earlier research cache, after hash checks; `ArticleNumbers` for canonical identity handling; the existing law project and test project through SDK automatic source inclusion. The legacy `LawTextParser`, `LawDiff` and special `LawTextFold` were inspected, but their folded-text behavior was deliberately not used to establish exact corpus transcription. The existing Harness `law` command and all old loaders are unchanged. The new development validator is a separate tool.

The only existing-file change so far is `.gitignore`, adding ignores for `tools/law-corpus/.cache/evidence/`, `tools/law-corpus/.cache/` and build-tool bytecode. No package/runtime dependency, project definition or application source has been changed.

**Build-cache issue: fixed 26 October 2026.** The shared `Directory.Build.targets` copies `data/**/*`, and the ignored evidence cache used to sit in `data/law/corpus/.cache/`, so ordinary developer builds picked it up (652 cache items at the initial check). The cache now lives in `tools/law-corpus/.cache/evidence/`, outside `data/`; the tools and docs were updated, and an ordinary `dotnet build -c Release` output contains no cache file (checked after the move). Shared targets remain unchanged. Do not publish source-cache files, PDFs, OCR tools or page images in product output.

Whole-code 20-article random visual sampling has not occurred because no whole code is claimed complete. Priority crop methods/hashes are retained, and crops can be regenerated from pinned official PDFs. The brief explicitly requests small article crops in `docs/law-corpus-checks/<code>/` for that later delivery; those remain to be produced and inspected. Full PDFs and full-page images remain in ignored caches.

## OCR comparison and resumed work

The latest consolidation batch replays11 explicit operations from Law05-10: replacements6,8,10,11,12,15,16,17 and insertions13bis,13bis1,17bis. It retains every prior version, separates full replacements from the independent article7 transcription correction, and leaves unknown commencement dates unknown. Article10 spans two pages and has four new paragraphs. The six-paragraph17bis text remains unsettled because exact vocalization needs another source review. Replay inputs, source-image decisions and the operation audit are under `research/civil-2005-conflict-*`; run `build_civil_conflict_batch.py`. Its correction guard refuses a different old text or attempts to change legal identity/date metadata.

The user authorized resumption on26October2026. On18 fixed selected body references, measured character error was8.25% for fast Tesseract,7.93% for best Tesseract and2.94% for Arabic PP-OCRv5 through RapidOCR/ONNX. On12 further bodies unused during tuning, fast/Paddle measured7.58%/3.09%. These are small source-image-reviewed samples, not independent human ground truth or legal-accuracy guarantees. The follow-up shares three column crops. See [the full method, caveats, primary sources and replay commands](../tools/law-corpus/OCR-COMPARISON.md).

Fast Tesseract remains the first stream. The new recognizer supplies an independent second Arabic OCR stream for discrepancy review, with raw boxes/text, crop/source/model/runtime/implementation hashes and per-crop recovery. It does not modify canonical text, paragraph structure, dates or verification. Best models are available selectively. Neither Docling itself nor EasyOCR has been benchmarked; no performance claim is made for either. Official digital text remains preferable when traceable to the correct version. Optional locked dependencies and model downloads remain in the ignored development environment, outside product dependencies.

The full-pass number-label failure and critical-token omissions mean PP-OCRv5 is a **wording aid only**. Its lower body character error is insufficient for legal numbers, dates, negations or article identities. It missed11 critical tokens on the calibration bodies versus6 for fast and2 for best Tesseract. `research/ocr-critical-coverage.json` records the limitation; the second stream cannot certify or automatically rewrite corpus data.

The full Civil second pass covers176 Arabic column windows, including original and amending publications. All176 windows completed and their raw artifacts passed hash replay; the compact source index and number observations are committed. The original-publication numeric-label parser recognized only1/1003 through this second stream, so it is unsuitable for article indexing. Existing Tesseract/French/manual number observations remain authoritative evidence candidates. This is source extraction, not completion of1003 article bodies or their historical versions.

## Validation and next work

Current checkpoint validation: the full Release solution built with **zero warnings/errors**, and **687 .NET tests passed** (App114, Web24, Drafting369, Form67, Documents6, Law104, Storage3). The Python pipeline's **67 fictitious offline tests passed**, including conservative merge, missing-line/critical-token scoring, Arabic word ordering, native-source label/layout guards and supplementary-artifact tamper checks. All corpus files pass JSON Schema and the typed .NET validator. The prior checkpoint's36 official PDF pins replayed offline and matched a fresh HTTPS comparison; `research/official-live-replay.json` retains that source audit. No pin was replaced. OCR success or a valid schema is not a legal-completeness claim.

Validation runs the ordinary Release build and tests. The earlier opt-in `validation-cache-filter.targets` import is no longer needed for correctness, because the cache is outside `data/`; `validate_checkpoint.ps1` keeps only the removal of stale cache copies from earlier `bin` outputs. No full PDF, page image or OCR dependency belongs in product output.

Remaining work under the user's approved current-first order:

1. Resolve Civil font/layout and current inventory, finish applicable consolidation and quality checks, and retain explicit priority uncertainties.
2. Complete current CPCA, applying22-13 and preserving installation/earlier-filed-case transitions.
3. Complete current Family Code from the acquired bilingual source and applicable amendments.
4. Complete only the frozen Commercial/90-11/90-04 units; report all three collections as partial.
5. Validate each delivery, finish source sampling and legacy reconciliation, and push recoverable checkpoints to `codex/law-corpus` only.
6. Preserve legally applicable older provisions in phaseOne; defer the comprehensive historical archive to phaseTwo.
7. **Do not start special laws.** See the [state report](LAW-CORPUS-STATE-REPORT.md) for the detailed backlog. The checkpoint's existing build/tests and push are complete; whole-code legal-data certification is not.

The 81 existing article records are mechanically compared below. A missing entry in this priority subset remains an explicit unresolved reconciliation item; it is not silently called the same text or a different legal version.

<!-- RECONCILIATION:BEGIN -->
All **81** legacy records are accounted for: different-text: 9; not-imported: 47; same-text: 25.

Differences and raw hashes: [machine-readable reconciliation](../data/law/corpus/research/reconciliation.json).

| Legacy code | Article | Result |
|---|---|---|
| civil-code | 86 | not-imported |
| civil-code | 103 | not-imported |
| civil-code | 106 | not-imported |
| civil-code | 107 | not-imported |
| civil-code | 119 | not-imported |
| civil-code | 122 | not-imported |
| civil-code | 124 | same-text |
| civil-code | 131 | not-imported |
| civil-code | 138 | different-text |
| civil-code | 164 | not-imported |
| civil-code | 174 | not-imported |
| civil-code | 176 | not-imported |
| civil-code | 179 | not-imported |
| civil-code | 182 | not-imported |
| civil-code | 182 مكرر | not-imported |
| civil-code | 186 | not-imported |
| civil-code | 323 | not-imported |
| civil-code | 324 مكرر 1 | not-imported |
| civil-code | 324 مكرر 5 | not-imported |
| civil-code | 333 | not-imported |
| civil-code | 351 | not-imported |
| civil-code | 364 | not-imported |
| civil-code | 367 | not-imported |
| civil-code | 450 | not-imported |
| civil-code | 454 | not-imported |
| civil-code | 467 | not-imported |
| civil-code | 467 مكرر | not-imported |
| civil-code | 498 | not-imported |
| civil-code | 674 | not-imported |
| commercial-code | 30 | not-imported |
| commercial-code | 173 | not-imported |
| commercial-code | 176 | not-imported |
| commercial-code | 187 مكرر | same-text |
| cpca | 8 | same-text |
| cpca | 13 | same-text |
| cpca | 14 | same-text |
| cpca | 15 | different-text |
| cpca | 17 | different-text |
| cpca | 37 | same-text |
| family-code | 3 مكرر | same-text |
| family-code | 7 | same-text |
| family-code | 37 | same-text |
| family-code | 48 | different-text |
| family-code | 49 | same-text |
| family-code | 52 | same-text |
| family-code | 53 | different-text |
| family-code | 53 مكرر | same-text |
| family-code | 54 | same-text |
| family-code | 57 | same-text |
| family-code | 57 مكرر | same-text |
| family-code | 62 | same-text |
| family-code | 64 | same-text |
| family-code | 65 | same-text |
| family-code | 66 | different-text |
| family-code | 67 | same-text |
| family-code | 68 | same-text |
| family-code | 72 | same-text |
| family-code | 74 | different-text |
| family-code | 75 | different-text |
| family-code | 76 | same-text |
| family-code | 78 | same-text |
| family-code | 79 | different-text |
| family-code | 80 | same-text |
| family-code | 87 | same-text |
| labour-disputes | 1 | not-imported |
| labour-disputes | 2 | not-imported |
| labour-disputes | 19 | not-imported |
| labour-disputes | 31 | not-imported |
| labour-disputes | 32 | not-imported |
| labour-disputes | 36 | not-imported |
| labour-disputes | 37 | not-imported |
| labour-relations | 1 | not-imported |
| labour-relations | 2 | not-imported |
| labour-relations | 3 | not-imported |
| labour-relations | 73 | not-imported |
| labour-relations | 73-1 | not-imported |
| labour-relations | 73-2 | not-imported |
| labour-relations | 73-3 | not-imported |
| labour-relations | 73-4 | not-imported |
| labour-relations | 80 | not-imported |
| labour-relations | 88 | not-imported |

### Text difference: civil-code 138

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `civil-code:138@ordinance-75-58`; corpus transcription state `checked`.

```diff
--- legacy
+++ civil-code:138@ordinance-75-58
@@ -1,2 +1,2 @@
-كل من تولى حراسة شيء وكانت له قدرة الاستعمال والتسيير، والرقابة، يعتبر مسؤولا عن الضرر الذي يحدثه ذلك الشيء.
-ويعفي من هذه المسؤولية الحارس للشيء إذا أثبت أن ذلك الضرر حدث بسبب لم يكن يتوقعه مثل عمل الضحية، أو عمل الغير، أو الحالة الطارئة، أو القوة القاهرة.
+كل من تولى حراسة شىء وكانت له قدرة الاستعمال والتسيير، والرقابة، يعتبر مسؤولا عن الضرر الذى يحدثه ذلك الشىء.
+ويعفى من هذه المسؤولية الحارس للشىء اذا أثبت أن ذلك الضرر حدث بسبب لم يكن يتوقعه مثل عمل الضحية، أو عمل الغير، أو الحالة الطارئة، أو القوة القاهرة.
```

### Text difference: cpca 15

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `cpca:15@law-08-09`; corpus transcription state `checked`.

```diff
--- legacy
+++ cpca:15@law-08-09
@@ -1,7 +1,7 @@
-يجب أن تتضمن عريضة افتتاح الدعوى، تحت طائلة عدم قبولها شكلا، البيانات الآتية:
+يجب أن تتضمن عريضة افتتاح الدعوى، تحت طائلة عدم قبولها شكلا، البيانات الآتية :
 1 - الجهة القضائية التي ترفع أمامها الدعوى،
-2 - اسم ولقب المدعي وموطنه،
-3 - اسم ولقب وموطن المدعى عليه، فإن لم يكن له موطن معلوم، فآخر موطن له،
-4 - الإشارة إلى تسمية وطبيعة الشخص المعنوي، ومقره الاجتماعي وصفة ممثله القانوني أو الاتفاقي،
-5 - عرضا موجزا للوقائع والطلبات والوسائل التي تؤسس عليها الدعوى،
-6 - الإشارة، عند الاقتضاء، إلى المستندات والوثائق المؤيدة للدعوى.
+2- اسم ولقب المدعي وموطنه،
+3- اسم ولقب وموطن المدعى عليه، فإن لم يكن له موطن معلوم، فآخر موطن له،
+4- الإشارة إلى تسمية وطبيعة الشخص المعنوي، ومقره الاجتماعي وصفة ممثله القانوني أو الاتفاقي،
+5- عرضا موجزا للوقائع والطلبات والوسائل التي تؤسس عليها الدعوى،
+6- الإشارة، عند الاقتضاء، إلى المستندات والوثائق المؤيدة للدعوى.
```

### Text difference: cpca 17

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `cpca:17@law-08-09`; corpus transcription state `checked`.

```diff
--- legacy
+++ cpca:17@law-08-09
@@ -1,3 +1,3 @@
-لا تقيد العريضة إلا بعد دفع الرسوم المحددة قانونا، مالم ينص القانون على خلاف ذلك.
+لا تقيد العريضة إلا بعد دفع الرسوم المحددة قانونا، ما لم ينص القانون على خلاف ذلك.
 يفصل رئيس الجهة القضائية في كل نزاع يعرض عليه حول دفع الرسوم، بأمر غير قابل لأي طعن.
-يجب إشهار عريضة رفع الدعوى لدى المحافظة العقارية، إذا تعلقت بعقار و/أو حق عيني عقاري مشهر طبقا للقانون، وتقديمها في أول جلسة ينادى فيها على القضية، تحت طائلة عدم قبولها شكلا، مالم يثبت إيداعها للإشهار.
+يجب إشهار عريضة رفع الدعوى لدى المحافظة العقارية، إذا تعلقت بعقار و/أو حق عيني عقاري مشهر طبقا للقانون، وتقديمها في أول جلسة ينادى فيها على القضية، تحت طائلة عدم قبولها شكلا، ما لم يثبت إيداعها للإشهار.
```

### Text difference: family-code 48

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:48@ordinance-05-02`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:48@ordinance-05-02
@@ -1 +1 @@
-مع مراعاة أحكام المادة 49 أدناه، يحل عقد الزواج بالطلاق الذي يتم بإرادة الزوج أو بتراضي الزوجين أو بطلب من الزوجة في حدود ما ورد في المادتين 53 و 54 من هذا القانون.
+مع مراعاة أحكام المادة 49 أدناه، يحل عقد الزواج بالطلاق الذي يتم بإرادة الزوج أو بتراضي الزوجين أو بطلب من الزوجة في حدود ما ورد في المادتين 53 و54 من هذا القانون.
```

### Text difference: family-code 53

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:53@ordinance-05-02`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:53@ordinance-05-02
@@ -1,5 +1,5 @@
 يجوز للزوجة أن تطلب التطليق للأسباب الآتية:
-1 – عدم الإنفاق بعد صدور الحكم بوجوبه ما لم تكن عالمة بإعساره وقت الزواج، مع مراعاة المواد 78 و 79 و 80 من هذا القانون،
+1 – عدم الإنفاق بعد صدور الحكم بوجوبه ما لم تكن عالمة بإعساره وقت الزواج، مع مراعاة المواد 78 و79 و80 من هذا القانون،
 2 – العيوب التي تحول دون تحقيق الهدف من الزواج،
 3 – الهجر في المضجع فوق أربعة أشهر،
 4 – الحكم على الزوج عن جريمة فيها مساس بشرف الأسرة وتستحيل معها مواصلة العشرة والحياة الزوجية،
```

### Text difference: family-code 66

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:66@law-84-11`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:66@law-84-11
@@ -1 +1 @@
-يسقط حق الحاضنة بالتزوج بغير قريب محرم وبالتنازل ما لم يضر بمصلحة المحضون.
+يسقط حق الحاضنة بالتزوج بغير قريب محرم، وبالتنازل ما لم يضر بمصلحة المحضون.
```

### Text difference: family-code 74

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:74@law-84-11`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:74@law-84-11
@@ -1 +1 @@
-تجب نفقة الزوجة على زوجها بالدخول بها أو دعوتها إليه ببينة مع مراعاة أحكام المواد 78 و 79 و 80 من هذا القانون.
+تجب نفقة الزوجة على زوجها بالدخول بها أو دعوتها إليه ببينة مع مراعاة أحكام المواد 78 و79 و80 من هذا القانون.
```

### Text difference: family-code 75

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:75@law-84-11`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:75@law-84-11
@@ -1 +1 @@
-تجب نفقة الولد على الأب ما لم يكن له مال، فبالنسبة للذكور إلى سن الرشد والإناث إلى الدخول وتستمر في حالة ما إذا كان الولد عاجزا لآفة عقلية أو بدنية أو مزاولا للدراسة وتسقط بالاستغناء عنها بالكسب.
+تجب نفقة الولد على الأب ما لم يكن له مال، فبالنسبة للذكور إلى سن الرشد والإناث إلى الدخول وتستمر في حالة ما إذا كان الولد عاجزا لآفة عقلية أو بدنية أو مزاولا للدراسة وتسقط بالإستغناء عنها بالكسب.
```

### Text difference: family-code 79

Text differs; inspect the stored diff. No legal amendment or correction is inferred from orthographic/punctuation differences.

Compared with `family-code:79@law-84-11`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ family-code:79@law-84-11
@@ -1 +1 @@
-يراعي القاضي في تقدير النفقة حال الطرفين وظروف المعاش ولا يراجع تقديره قبل مضي سنة من الحكم.
+يراعى القاضي في تقدير النفقة حال الطرفين وظروف المعاش ولا يراجع تقديره قبل مضي سنة من الحكم.
```
<!-- RECONCILIATION:END -->
