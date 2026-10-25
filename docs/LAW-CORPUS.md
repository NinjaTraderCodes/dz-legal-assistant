# Separate official-law corpus: delivery record and integration contract

Research cut-off: **25 October 2026**. Branch: `codex/law-corpus`, isolated from the application engineer's default-branch work. This is a separate, additive corpus. The existing `data/law/codes`, claim mappings, loaders and application behavior have not been switched to it.

## Delivery status

**The complete six-code corpus is not delivered.** The initial priority package contains all ten requested priority identities plus four temporal provisions: **14 article identities and 15 version candidates**. The original and 2005 versions of Civil article 7 are separate records. This is an explicitly bounded priority subset, not an inventory of the complete Civil, Commercial or procedural codes.

| Legal text | Corpus article identities | Versions | Checked | Partly checked | Unsettled | Full code complete |
|---|---:|---:|---:|---:|---:|---|
| Civil Code | 9 | 10 | 0 | 8 | 2 | No |
| Commercial Code | 2 | 2 | 0 | 2 | 0 | No |
| CPCA | 3 | 3 | 0 | 3 | 0 | No |
| Family Code | 0 | 0 | 0 | 0 | 0 | No; not started |
| Labour 90-11 | 0 | 0 | 0 | 0 | 0 | No; not started |
| Labour 90-04 | 0 | 0 | 0 | 0 | 0 | No; not started |

The priority inventory has no missing target identity. Whole-code inventory and amendment-application certification have **not** passed. Accordingly no version is `checked`. Thirteen transcriptions have explicit, hash-bound image decisions and French structural checks, so are `partly-checked`. Two Civil version candidates remain `unsettled`: article 140 punctuation and amended article 7 orthography. They must not be presented as exact checked source text. These states are not lawyer verification, and the original `verified`/`verifiedBy` fields are not set or changed.

Priority coverage:

- Civil: 138, 139, 140, 308, 309, 379; temporal provisions 7 (two versions), 1002 and 1003.
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

Every non-checked version has reasons in its code's `report.json`. Scope-level gaps remain in `ledger.scope.unsettled`. Empty **priority** target gaps do not mean a complete code. The six full-code deliveries and special-law inventory remain outstanding.

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

The only existing-file change so far is `.gitignore`, adding ignores for `data/law/corpus/.cache/`, `tools/law-corpus/.cache/` and build-tool bytecode. No package/runtime dependency, project definition or application source has been changed.

**Build-cache issue awaiting a specific scope exception:** the existing `Directory.Build.targets` explicitly copies `data/**/*`, including ignored corpus cache files. MSBuild inspection found 652 cache items in app content at the initial check. A one-line exclusion was proposed in `research/build-cache-exclusion-proposal.patch`, but the shared targets file is outside the assignment's edit allowlist and is not changed without the requested exception. For validation, the task's ignored cache is temporarily held outside that glob and restored in `finally`; this matches a clean checkout containing only shipped data. Do not publish a developer build containing the source cache. There is no production need for PDFs, OCR tools or page images.

Whole-code 20-article random visual sampling has not occurred because no whole code is claimed complete. No full PDF/page image is committed. Priority crop methods/hashes are retained, and crops can be regenerated from pinned official PDFs. The prompt's later `docs/law-corpus-checks/<code>/` crop requirement will need an explicit documented interpretation against its earlier permitted-path/no-page-image limits before that whole-code delivery.

## Validation and next work

Final checkpoint validation: the full Release solution built with **zero warnings/errors**, and **685 .NET tests passed** (App114, Web24, Drafting369, Form67, Documents6, Law102, Storage3). The Python pipeline's **19 fictitious offline tests passed**. All corpus files pass JSON Schema validation and the typed .NET corpus validator; all 26 official PDF hashes replay offline. The final Git commit identifies the delivered snapshot. OCR success or a valid schema is not a legal-completeness claim.

Remaining work, in the user's required order:

1. Resolve or explicitly retain every priority transcription/temporal uncertainty, finish validation and push the checkpoint on `codex/law-corpus` only.
2. Complete Civil original/amendment ledger, all versioned articles, independent numbering inventory, paragraph/cross-reference processing and the required visual sample.
3. Complete CPCA, including all amendments and transition records.
4. Complete Family Code.
5. Complete Commercial Code, including the earlier unprocessed amendments and later changes.
6. Complete 90-11 and 90-04 histories.
7. Propose the special-law list with official references/reasons before building it.

The 81 existing article records are mechanically compared below. A missing entry in this priority subset remains an explicit unresolved reconciliation item; it is not silently called the same text or a different legal version.

<!-- RECONCILIATION:BEGIN -->
All **81** legacy records are accounted for: different-text: 1; not-imported: 79; same-text: 1.

Differences and raw hashes: [machine-readable reconciliation](../data/law/corpus/research/reconciliation.json).

| Legacy code | Article | Result |
|---|---|---|
| civil-code | 86 | not-imported |
| civil-code | 103 | not-imported |
| civil-code | 106 | not-imported |
| civil-code | 107 | not-imported |
| civil-code | 119 | not-imported |
| civil-code | 122 | not-imported |
| civil-code | 124 | not-imported |
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
| cpca | 8 | not-imported |
| cpca | 13 | not-imported |
| cpca | 14 | not-imported |
| cpca | 15 | not-imported |
| cpca | 17 | not-imported |
| cpca | 37 | not-imported |
| family-code | 3 مكرر | not-imported |
| family-code | 7 | not-imported |
| family-code | 37 | not-imported |
| family-code | 48 | not-imported |
| family-code | 49 | not-imported |
| family-code | 52 | not-imported |
| family-code | 53 | not-imported |
| family-code | 53 مكرر | not-imported |
| family-code | 54 | not-imported |
| family-code | 57 | not-imported |
| family-code | 57 مكرر | not-imported |
| family-code | 62 | not-imported |
| family-code | 64 | not-imported |
| family-code | 65 | not-imported |
| family-code | 66 | not-imported |
| family-code | 67 | not-imported |
| family-code | 68 | not-imported |
| family-code | 72 | not-imported |
| family-code | 74 | not-imported |
| family-code | 75 | not-imported |
| family-code | 76 | not-imported |
| family-code | 78 | not-imported |
| family-code | 79 | not-imported |
| family-code | 80 | not-imported |
| family-code | 87 | not-imported |
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

Compared with `civil-code:138@ordinance-75-58`; corpus transcription state `partly-checked`.

```diff
--- legacy
+++ civil-code:138@ordinance-75-58
@@ -1,2 +1,2 @@
-كل من تولى حراسة شيء وكانت له قدرة الاستعمال والتسيير، والرقابة، يعتبر مسؤولا عن الضرر الذي يحدثه ذلك الشيء.
-ويعفي من هذه المسؤولية الحارس للشيء إذا أثبت أن ذلك الضرر حدث بسبب لم يكن يتوقعه مثل عمل الضحية، أو عمل الغير، أو الحالة الطارئة، أو القوة القاهرة.
+كل من تولى حراسة شىء وكانت له قدرة الاستعمال والتسيير، والرقابة، يعتبر مسؤولا عن الضرر الذى يحدثه ذلك الشىء.
+ويعفى من هذه المسؤولية الحارس للشىء اذا أثبت أن ذلك الضرر حدث بسبب لم يكن يتوقعه مثل عمل الضحية، أو عمل الغير، أو الحالة الطارئة، أو القوة القاهرة.
```
<!-- RECONCILIATION:END -->
