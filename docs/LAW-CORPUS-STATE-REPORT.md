# Law corpus branch: current state report

**Report date:** 26 October 2026

**Branch:** `codex/law-corpus`

**Substantive checkpoint reviewed:** `e6698050f6fdd30a6fc31b50da9145af97b777d3`

**Recorded amendment-discovery cut-off in the typed collections:** 25 October 2026

This report covers the corpus work on this branch, including earlier checkpoints. It does not announce completion of the legal library. This report revision adds documentation, not new article text or application behavior.

## 1. Overall position

We have built and tested the corpus infrastructure, established the agreed collection boundaries, collected substantial official source material, and demonstrated versioned consolidation on real articles. The latest improvement is bulk collection from native Ministry text instead of relying on scanned publications for every initial transcription.

**The full requested corpus is not finished. No complete code has passed the required legal-data checks.**

| Measure | Current result | What it means |
|---|---:|---|
| Native source article records / raw blocks collected | **2,355** | Material collected for Civil, CPCA and Family; includes source editions, editorial material and unresolved candidates. |
| Article identities in the typed corpus | **49** | Stable identities in the approved `ledger.json` / `articles.json` / `report.json` format. |
| Versions attached to those identities | **59** | Includes historical versions; not 59 additional articles. |
| Fully `checked` versions | **0** | Full transcription, inventory and amendment certification has not passed. |
| `partly-checked` versions | **42** | Some recorded checks passed; these are not fully certified articles. |
| `unsettled` versions | **17** | Unresolved transcription or other recorded issues. |
| Complete checked codes delivered | **0** | Civil, CPCA and Family remain incomplete deliveries. |

**These counts describe different stages and must not be added together.** The 2,355 source records are not 2,355 distinct, checked, current articles. A downloaded text, a parsed article block, a typed version and a checked legal provision are separate milestones.

**The application still uses its existing 81-article library.** The new corpus loader is additive and has not replaced the application's existing loader. This branch has not changed claim matching, fact-to-law analysis, drafting, RAG, the UI or export/review safeguards.

## 2. Agreed scope and delivery order

The latest user instruction is **complete current codes first**, with comprehensive historical reconstruction deferred. Existing historical work is retained. Older rules and transitions still needed for supported cases must remain available or explicitly unsettled; current text must not silently substitute for unavailable applicable older law.

| Collection | Agreed coverage | Frozen numbering / chapter boundary |
|---|---|---|
| Civil Code, Ordinance 75-58 | Complete code | Original base numbers 1–1,003, plus inserted identities and explicit repeals. |
| CPCA, Law 08-09 | Complete code, including Law 22-13 | Original base numbers 1–1,065, plus inserted identities and explicit repeals. |
| Family Code, Law 84-11 | Complete code, including Ordinance 05-02 | Original base numbers 1–224, plus inserted identities and explicit repeals. |
| Commercial Code | **Partial**: catalogue articles and whole covered units | Article 30's whole chapter; the entire commercial-lease title, all seven chapters, 169–202 plus 187 bis and 187 bis 1. Initial bounded set: **37 identities**. |
| Labour relations, Law 90-11 | **Partial**: catalogue articles and whole covered chapters | Title III / Chapter VI, 62–74 including 73-1…73-6; Title IV / Chapter I, 80–86; Title IV / Chapter III, 88–90. Initial bounded set: **29 identities**. |
| Labour disputes, Law 90-04 | **Partial**: catalogue article and its whole chapter | Title IV / Chapter I, conciliation-bureau competence. The whole chapter is article 19. Initial bounded set: **1 identity**. |

The three complete codes have **2,292 original base numbers**. The bounded partial collections initially contain **67 identities**. These are scope snapshots, not final totals after all insertions, repeals, historical boundaries and numbering reconciliation.

Commercial and labour must remain labelled **partial**, even after their selected chapters are complete. Special laws, including the original prompt's item 7, are excluded. No work depends on Djihane or an external legal reviewer.

Evidence: [frozen scope](../data/law/corpus/research/scope-plan.json) and [approved current-first order](../data/law/corpus/research/delivery-order-2026-10-26.json).

## 3. Position against the agreed seven-point plan

| Agreed point | State | What remains |
|---|---|---|
| 1. Freeze the precise article and chapter inventory | **Boundaries frozen; final legal inventory incomplete** | Reconcile every insertion, repeal and numbering change. Catalogue hashes detect scope drift. |
| 2. Finish the machinery for consolidation | **Implemented and tested for the supported operations** | Apply it across the full scope; complete-code certification has not been demonstrated. |
| 3. Complete Civil first | **Source collection advanced; typed delivery partial** | Font/layout cleanup, current consolidation, inventory, structure and quality checks. |
| 4. Complete CPCA, then Family | **Bulk native text collected** | Apply 22-13 to CPCA; finish Family cleanup, amendment reconciliation and both codes' quality checks. |
| 5. Build bounded Commercial and labour collections | **Scope frozen; Commercial priority pilot only** | Build and validate all selected chapters, with explicit partial reports. |
| 6. Validate before calling a delivery finished | **Engineering checks pass; whole-code legal-data checks open** | Independent complete inventory, all applicable amendments, paragraph/cross-reference checks and 20-article visual sampling per code. |
| 7. Deliver small, recoverable checkpoints | **In place** | Continue saving tested deliveries to this branch only. |

## 4. Engineering work achieved

### Corpus format and offline loader

Implemented JSON Schemas for ledgers, article/version records and reports, together with typed C# models and `CorpusLoader` / `LawCorpus.VersionOn`.

The loader validates identities, ledger references, dates and overlapping intervals, paragraph reconstruction, cross-reference targets and quoted references, source fragments, verification evidence, and declared coverage. Partial chapter inventories cannot be presented as complete codes. Stale image-review hashes cannot validate changed text. Multi-publication article bodies require paragraph-level provenance.

Date lookup distinguishes missing, not-in-force, unsettled, partly-checked, resolved and repealed results. Unknown commencement is not treated as an unlimited interval. Requests after the recorded discovery cut-off remain unsettled. A date lookup does not itself decide which event controls legal applicability to a particular case.

Evidence: [typed models and lookup](../src/DzLegal.Law/Corpus/CorpusModel.cs), [loader](../src/DzLegal.Law/Corpus/CorpusLoader.cs), [schemas](../data/law/corpus/schema/) and [loader tests](../tests/DzLegal.Law.Tests/Corpus/CorpusLoaderTests.cs).

### Reproducible acquisition and extraction

Built development tools for official-source downloads, SHA-256 pinning, redirect checks, offline replay, rendered crops, separate PDF-text and OCR streams, conservative comparison, source-image decisions and resumable extraction. Raw evidence binds the source page, crop, tool/model settings and artifact hashes.

The earlier source/discovery manifest contains **36 pinned PDFs** with a recorded fresh official-byte comparison. The initial priority download list and later source lists overlap; their counts must not be added as distinct publications. The current digital registry and the 1,065 per-article CPCA HTML pins are additional evidence collections.

PDFs, images, OCR runtimes and model weights remain in ignored development caches. The product has gained no OCR dependency, runtime download or network requirement.

Reused existing official downloads after hash checks, the existing article-number identity handling, and the law/test projects' automatic source inclusion. The legacy parser/diff/folding tools were inspected; their special Arabic letter folding was not used to establish transcription accuracy. The Harness `law` command was not changed.

### Amendment consolidation

Implemented explicit, hash-guarded operations for full replacement, paragraph replacement/insertion/deletion, article insertion, repeal, heading changes and renumbering. The machinery retains prior versions and inherited paragraph sources, audits operations, and invalidates outdated inventory certification. It does not guess legal instructions or commencement dates.

Real demonstrations include the Civil article 489 partial replacement, which retains the untouched earlier paragraph, and **11 Law 05-10 operations**: replacements of 6, 8, 10, 11, 12, 15, 16 and 17; insertions of 13 bis, 13 bis 1 and 17 bis. The separately reviewed article 7 transcription correction is not misrepresented as another amendment operation. Replaying the Civil checkpoint reproduced its canonical files byte for byte.

Evidence: [pipeline guide](../tools/law-corpus/README.md), [consolidator](../tools/law-corpus/consolidate.py), [489 audit](../data/law/corpus/research/civil-pilot-consolidation-audit.json) and [2005 batch audit](../data/law/corpus/research/civil-2005-conflict-consolidation-audit.json).

## 5. Legal text collected and represented

### Native source acquisition

| Collection | Collected | Outstanding qualification |
|---|---|---|
| CPCA Ministry Arabic HTML, 2008 base | **1,065 articles**, zero acquisition failures; all page pins replayed offline | This is the 2008 base. The 2022 overlay is not yet applied to produce a complete current code. HTML paragraphs are not automatically legal alinea boundaries. |
| Civil Ministry-linked Arabic 2007 compilation | **1,058 raw blocks**, **1,049 distinct number candidates**; 1,002 of 1,003 original base labels observed | Absent 41 has independent explicit repeal evidence. Repeated local numbers 2–10 are retained separately. Font errors, headings, annotations, inserted-number inventory and currentness checks remain. |
| Family Ministry bilingual compilation incorporating 05-02 | **232 raw Arabic blocks**: all 224 base labels plus eight inserted candidates; no duplicate candidates | Source-font errors, paragraphs, annotations, amendment coverage and French alignment remain to be checked. Annexes after the main code are excluded. |
| **Total** | **2,355 source records / blocks** | **Not a checked-current-article total.** |

The Family inserted candidates are 3 bis, 7 bis, 8 bis, 8 bis 1, 9 bis, 45 bis, 53 bis and 57 bis. Observing labels establishes acquisition coverage, not independent completeness certification.

The Family PDF was initially misclassified as French only. Visible-page inspection established its Arabic/French columns, and metadata was corrected without changing source bytes. The collector now handles the rotated PDF's coordinates, checks coverage of all Arabic glyph boxes in the selected column, and stops at article 224 on PDF page 43, excluding the later annexes. The separate Arabic `84-11_.pdf` is the original 1984 publication, not a current consolidation.

Evidence: [acquisition summary](../data/law/corpus/research/current-batches/acquisition-summary.json), [digital-source registry](../data/law/corpus/research/current-digital-sources.json), [CPCA batch](../data/law/corpus/research/current-batches/cpca-official-html-2008.json), [Civil batch](../data/law/corpus/research/current-batches/civil-official-native-2007.json) and [Family batch](../data/law/corpus/research/current-batches/family-official-native-2005.json).

### Typed corpus, separate from the raw batches

Verification counts below are **version counts**.

| Collection | Article identities | Versions | Checked | Partly checked | Unsettled |
|---|---:|---:|---:|---:|---:|
| Civil | 44 | 54 | 0 | 37 | 17 |
| Commercial | 2 | 2 | 0 | 2 | 0 |
| CPCA | 3 | 3 | 0 | 3 | 0 |
| Family | 0 | 0 | 0 | 0 | 0 |
| Labour 90-11 | 0 | 0 | 0 | 0 | 0 |
| Labour 90-04 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **49** | **59** | **0** | **42** | **17** |

The initial priority-format milestone contained 14 identities and 15 version candidates. Subsequent Civil work expanded that to the present totals. Civil now includes original bodies 1–32, additional priority/temporal records, inserted identities and the 489 pilot. Commercial contains 187 bis and 187 bis 1; CPCA contains 2, 504 and 1062.

The **17 unsettled Civil versions** are original 4, 6, 8, 10, 12, 13, 15, 16, 17, 20, 22, 23, 25, 26, 28 and 140, plus the 2005 version of 17 bis. Unknown temporal endpoints also restrict usability of other historical records; a partly-checked transcription is not automatically a resolved current version.

No article is marked as verified by a lawyer. Existing `verified` / `verifiedBy` fields were not changed.

## 6. OCR and source-quality findings

Completed **312 Civil extraction windows** across Arabic/French original and amending publications. Column geometry was corrected for the slanted/shifted original scan, with decisions and hashes retained. These are crop counts, not checked-article counts.

Tested fast and best Tesseract models and a separate Arabic PP-OCRv5 stream through RapidOCR/ONNX. The latter completed **176 Arabic Civil windows** with artifact-hash replay.

| Recognizer | Character error on 18 selected body references | OCR time for those 18 crops |
|---|---:|---:|
| Tesseract fast | 8.25% | 4.94 seconds |
| Tesseract best | 7.93% | 12.58 seconds |
| Arabic PP-OCRv5 / ONNX | 2.94% | 18.41 seconds |

On 12 further bodies unused during tuning, fast Tesseract measured 7.58% and PP-OCRv5 3.09%. These are small, locally reviewed samples from the same publication, not independent human ground truth, random whole-code quality measurements or controlled throughput benchmarks.

The lower body error hid a serious weakness: the supplementary original-publication label parser recognized only **1 of 1,003 article numbers**. PP-OCRv5 also missed 11 critical numeric/negation tokens on the 18-body sample, versus six for fast Tesseract and two for best. It is therefore retained as a **wording aid**, with no automatic text fusion or legal-data promotion. Fast Tesseract remains the first OCR stream; best is selective.

Docling itself and EasyOCR were not installed or benchmarked. No accuracy or speed result is claimed for either. The implemented ONNX route uses a PaddleOCR model without installing the full Paddle runtime.

The strongest coverage improvement has been discovering usable native official text. Direct Ministry HTTP requests succeed with the task User-Agent even when browser retrieval times out. Native text is now the first collection route, with image/OCR evidence retained for checking.

Earlier work spent substantial time on small historical transcription batches and evidence tooling. It did not produce complete-code coverage. The approved current-first order now prioritizes bulk collection and current consolidation. No reliable end-to-end completion percentage or remaining-time estimate has been established; the main unfinished work is consolidation and quality control, not simply downloading more articles.

### WIPO comparison

The user-supplied local `75-58_wipo_Version.pdf` was tested after the user's suggestion. Its file bytes differ from the Ministry copy, but **all 268 pages have identical raw PDFium-extracted text**. It is an alternate copy of the same extracted text, not an independent text witness. WIPO identifies its edition as amended through May 2007.

Both copies display Arabic correctly while extracting some glyphs incorrectly: for example, printed `كامل` becomes `آامل`, and a font stretch is emitted as U+0467. No global Arabic-letter replacement has been applied. A possible font/CID-specific decoding approach remains investigative, not a completed correction.

Evidence: [OCR comparison and limitations](../tools/law-corpus/OCR-COMPARISON.md), [312-window index](../data/law/corpus/research/civil-full-extractions.json), [176-window index](../data/law/corpus/research/civil-full-paddle-extractions.json), [critical-token findings](../data/law/corpus/research/ocr-critical-coverage.json) and [WIPO comparison](../data/law/corpus/research/current-batches/civil-wipo-comparison.json).

## 7. Legal-source research and reconciliation achieved

Amendment discovery, source-page findings and transition records are retained separately from transcription claims. The branch records why enactment, publication, commencement and case applicability cannot be treated as interchangeable dates. Unknown dates remain unknown.

Historical exceptions already identified for the current-first delivery include Civil prescription transitions, older lease regimes preserved by Commercial 187 bis 1 / Civil 507 bis, and the Law 22-13 installation/earlier-filed-case transition. Actual installation dates and case-specific applicability are not established merely by recording the transition text.

Source discrepancies are preserved rather than silently repaired. Examples include the Arabic/French cross-reference mismatch in original Civil 13, paragraph differences in original Civil 10, and the printed French alias `187 ter` for Arabic `187 مكرر 1`.

The 83-01 discovery inventory was corrected to 11 inserted identities and its actual terminology instructions were read from source images. Complete application of those substitutions and all affected versions remains outstanding. This earlier research is retained for current consolidation where necessary and for the deferred historical archive.

The prior copyright review located Ordinance 03-05 article 11 in the official Arabic/French publications and recorded the statutory basis concerning official legal texts and official translations. Its findings and page/hash references are in [temporal and copyright findings](../data/law/corpus/research/temporal-findings.json); this is not a blanket licence for private annotations or OCR software.

All **81 legacy article records** have a mechanical reconciliation entry:

| Recorded reconciliation result | Count |
|---|---:|
| Same under the named NFC/whitespace comparison | 1 |
| Different text, with a stored difference | 1 |
| Not imported into the typed subset; comparison unresolved | 79 |

This is accounting for every legacy record, not completed substantive reconciliation of all 81. The 79 entries may have source material in raw batches; they are not yet represented in the typed subset used for this comparison. A textual difference alone does not establish a different legal version.

Evidence: [amendment discovery](../data/law/corpus/research/amendment-discovery.json), [reconciliation](../data/law/corpus/research/reconciliation.json) and [technical delivery record](LAW-CORPUS.md).

## 8. Validation and application boundaries

At the substantive checkpoint reviewed, both the pre-commit and required post-commit checks passed:

| Check | Result |
|---|---|
| Full Release solution build | **0 warnings, 0 errors** |
| Full .NET suite | **687 passed, 0 failed** |
| Python corpus pipeline suite | **67 passed** |
| JSON Schema validation | **0 errors** |
| Typed .NET corpus validation | Passed |
| Frozen scope / catalogue-hash check | Passed |
| Civil canonical replay | Byte-identical |
| CPCA native-source offline replay | All 1,065 article pins passed |
| WIPO / Ministry raw text comparison | All 268 pages matched |

The .NET total comprises App 114, Web 24, Drafting 369, Form Engine 67, Documents 6, Law 104 and Storage 3. These engineering checks do not certify legal completeness or article accuracy. Whole-code independent inventory certification and 20-article random visual QA have not been completed.

New tests cover invalid/overlapping version intervals, missing cross-references, false verification claims, stale review evidence, partial chapter scope, mixed-publication paragraph provenance, consolidation guards, native-source parsing/layout, and OCR/resume artifact integrity. No existing test was deleted, skipped or weakened for this work.

The branch's changes are confined to the corpus/data tools, typed corpus implementation and tests, corpus documentation, and three `.gitignore` cache patterns. This requested state report is an additional documentation file. No existing application loader, legal-engine behavior, claim catalogue, legacy law JSON, prompt, shared project target or UI source was changed by the corpus implementation. Upstream documentation was brought in by merge, not rebase.

One real build limitation remains: shared `Directory.Build.targets` includes `data/**/*`, so an ordinary build from a developer checkout can pick up the ignored source cache. The task's opt-in validation filter excludes that cache without moving evidence or editing shared targets. That validation command is safe for this checkout; it is not a permanent packaging fix for every developer build. Build-time corpus caches must not be shipped.

## 9. Work still required

| Area | Remaining work before calling it delivered |
|---|---|
| Civil current code | Resolve the source-font decoding problem with evidence; remove editorial/layout material; reconcile local numbering, all inserted identities and explicit repeals; finish applicable amendment/heading/paragraph changes; import current versions with provenance; complete independent inventory and quality checks. |
| CPCA current code | Apply the source-bound 22-13 plan: **69 replacement/partial-replacement targets, 19 insertions, six whole-article repeals and one partial repeal affecting article 32**. Check Arabic wording and paragraph composition, heading changes and transitions. The 989 base bodies outside those direct text changes still need normal QA. |
| Family current code | Clean the 232 raw candidates without guessed letter repair; finish amendment and repeal inventory, paragraph/headings/cross-references, French structural alignment and typed import; perform full delivery checks. |
| Commercial partial collection | Complete the frozen 37-identity chapter/title boundary, reconcile amendments and retained older lease rules, and publish an explicit partial-coverage report. Only two priority identities currently exist in the typed corpus. |
| Labour 90-11 partial collection | Build the three frozen chapters, reconcile their amended numbering and wording, import and validate; publish an explicit partial report. No typed articles delivered yet. |
| Labour 90-04 partial collection | Build article 19's whole chapter, reconcile amendments, import and validate; publish an explicit partial report. No typed articles delivered yet. |
| Shared legal-data quality | Complete source-text/OCR/image discrepancy decisions, published paragraph structure, Arabic/French numbering/presence/paragraph checks, cross-reference targets, independent inventories and amendment application evidence. |
| Sampling and handoff | Produce and inspect the required 20 random article/source comparisons per completed code, finish reconciliation against the 81 legacy records, document all remaining gaps and supply tested recoverable deliveries. |
| Full historical archive | Deferred, except for provisions/transitions necessary for supported cases. Preserve all existing versions and evidence; do not discard earlier work. |
| Application integration | Separate downstream task after adequate corpus delivery; the new raw batches must not be wired directly into drafting as checked law. |

The CPCA overlay is an **instruction inventory**, not an already completed consolidation. The French publication helped identify operation targets; that does not supply checked Arabic article bodies. See [22-13 overlay plan](../data/law/corpus/research/current-batches/cpca-22-13-current-overlay-plan.json).

The next Civil milestone should resolve font/layout issues and establish a defensible current inventory. CPCA's structured base and bounded overlay then provide a clear consolidation route; Family has a bounded native source ready for cleanup. Commercial/labour remain restricted to the agreed chapters. Each code must pass its delivery gates before being called complete.

## 10. Recoverable checkpoints and where to resume

| Checkpoint | Contribution |
|---|---|
| `06273fb` | Initial versioned priority corpus, schemas, loader and reproducible source checks. |
| `04d638c` | Revised scope and Civil consolidation evidence. |
| `1323ea3` | Measured Arabic OCR alternative and expanded Civil source readings. |
| `5cdd5f0` | Merge of upstream documentation into the corpus branch. |
| `e669805` | Current-first native collection, WIPO comparison, Family correction and further Civil consolidation. |

The substantive checkpoint was pushed to `origin/codex/law-corpus`; local and remote tracking hashes matched when this report was prepared. No PR, force-push or default-branch corpus commit was made. No extraction or OCR job was left running at that checkpoint.

Committed source URLs, hashes, manifests, raw-text indexes, review decisions and audits support recovery. The ignored local caches retain the larger PDFs, images, recognizers and raw process artifacts. A fresh checkout needs the pinned source/tool downloads to regenerate those caches; it does not contain the caches themselves.

Start from [current progress](../data/law/corpus/research/resume-progress-2026-10-26.json), [acquisition summary](../data/law/corpus/research/current-batches/acquisition-summary.json), [delivery order](../data/law/corpus/research/delivery-order-2026-10-26.json) and [pipeline commands](../tools/law-corpus/README.md). Earlier pause notes and earlier full-history delivery ordering are historical records; the approved current-first order governs the remaining work.
