# Family Code: preparation and source-check record

Updated 28 October 2026, branch `codex/law-corpus`. This is **not a completed Family delivery**. The typed current candidate contains **232 identities / 232 snapshot records: 0 checked, 228 partly-checked, four unsettled**. Image readings are Codex's (AI), never human or lawyer verification.

## Latest checkpoint: original-JO body collation finished

All **176 flagged unchanged-body article views** were actually read against JO24/1984, including wider original-page images when alignment clipped words or closing lines. Together with the 14 strict OCR agreements, this accounts for all 190 unchanged bodies: **171 image-collated matches with disclosed edition orthography, one guarded correction, four unresolved official-edition conflicts**. This does not certify identical printed spelling, vocalization or punctuation between editions.

Art. **42** loses the compilation's added `(06)` after `ستة`; the original prints the six-month minimum in words, while the maximum `عشرة (10)` is retained. Initial visual notes proposed changes to 69 and 210, but the exact old-text guards established that both the displayed input and current candidate already had `الموكول` and `يحوز`. Those proposals were rejected; the superseded findings and reasons remain recorded. No extra article correction is claimed.

| Article | Original JO reading | Ministry candidate | Disposition |
|---|---|---|---|
| 135 | `عمدا أو عدوانا` | `عمدا وعدوانا` | Critical OR/AND disagreement; original image and both OCR streams support `أو`. |
| 151 | `عاصب غيره` | `عاصب مع غيره` | Original list omits `مع`, although its later heading includes it. |
| 202 | `يتوقف على إنجاز الشرط` | `يتوقف تمامها على إنجاز الشرط` | Compilation adds `تمامها`. |
| 203 | `وغير محجوز عليه` | `وغير محجور عليه` | Original final letter has an upper dot; a suspected print typo is not silently repaired. |

These four current candidates retain the Ministry reading **only as an unsettled hypothesis**, with both exact alternatives in their verification reasons and research record. They receive no passed original-JO check. The 20-article sample includes Ministry witnesses for 135, 151 and 203: matching those source images never resolved their disagreement with the original JO.

`family_jo_collation.py` records explicit readings; `apply_family_jo_collation.py` replays all decisions, proof/input/text hashes, unique correction guards and a complete partition of wording differences into fixes or unresolved conflicts. `build_family_candidate.py` replays that operation before assembly and keeps the original Ministry text hash separate from the final JO-corrected text. Seven new regression tests cover missing/stale proof, wrong source, duplicate decisions, incomplete crops, correction guards and conflict quarantine. All **156 Python tests** pass.

Official discovery identified French rectifications in JO31/1984 p. 801 and JO41/1984 p. 1018. The Ministry amendment list does not enumerate them. Official image acquisition/reading remains pending; direct download timed out. Secondary descriptions are discovery clues only and supply no canonical wording or legal effect. No rectification is assumed to settle article 135. The separate Law05-09 approval-source acquisition remains open.

## Amended paragraph review and source-locator corrections

All **37 amended-body paragraph layouts** have now been actually read on19 generated source-review sheets, with a separate correct-source reading for3bis. No paragraph text or division needed changing. `amended-structure-decisions.json` binds the final candidate texts, paragraph counts and actual proof hashes; `apply_family_amended_structure.py` refuses missing/duplicate readings, reflowed paragraphs or changed proof. All **165 Python tests** now pass, including three new paragraph-proof guards and a wrong-act locator regression.

The review corrected two provenance errors without changing wording:

- **3bis:** automatic alignment had selected the identical prosecution sentence in nationality article37 on PDF18. The Family insertion is actually below Ordinance05-02 article2 on PDF19 right. That passage was read, and both raw OCR streams and the primary locator now come from the correct column. The old generated comparison remains diagnostic evidence.
- **57bis:** the automatic span included a second region on PDF22 containing the next amending instruction/header. Its complete one-paragraph body ends on PDF21 left. The extra body locator is removed.

`amended-source-locator-overrides.json` records the old/generated regions, guarded final body hashes, correct source regions, actual image proof and raw OCR. The builder rejects a changed old locator or body and enforces the independently read Family act's PDF-page ranges. Identical wording in another act in the same journal cannot satisfy that guard. This Family-specific PDF-range check does not reinterpret the older codes' journal-page metadata.

## Prepared scope and source inventory

The Ministry's bilingual compilation yields 232 distinct identities: 224 base numbers and eight insertions. Duplicate numbering, the `8 bis 1` label, the chapter heading after repealed art. 39 and the signature after art. 224 have explicit boundary fixes. All five repealed identities remain in the inventory. Annexes outside the main code remain excluded.

The independently inspected Ordinance 05-02 instructions (JO15/2005, pp. 19–22) account for 29 replacements, eight insertions and five repeals, plus the section reorganization and publication clause. The 190 other base bodies are compared with JO24/1984. An enactment or publication date is not silently treated as commencement.

## Quality finding: inherited agreement is approximate

The previous `native_check` comparison accepts ordinary words after Arabic-letter folding, and can accept a one-letter substitution as OCR noise. Digits and negations receive stronger treatment. That behavior is explicitly present in the tool; it is **not an exact-word transcription test**.

This matters in practice: Family art. 8's Ministry compilation says `وأثبت الزواج المبرر الشرعي`, while the 2005 print reads `وأثبت الزوج المبرر الشرعي`. The inherited test could accept this difference as noise. The new exact-word overlay flags it. The older results remain available with their original rule; their scores are not relabelled as exact agreement.

`family_exact_audit.py` uses folding only for alignment, requires `native_check.exact_form` equality to accept words, disallows digit joins, and uses saved targeted readings only on their original disputed ranges. Its named forms still normalize NFC, tatweel, harakat and digit script; it does not claim to certify printed vocalization. Any retained unconfirmed punctuation must be disclosed under the approved rule.

| Stage | Unchanged base bodies | Replaced/inserted bodies |
|---|---:|---:|
| Initial inherited comparison agreement | 77/190 | 16/37 |
| After cached targeted rereads, inherited rule | 96/190 | 25/37 |
| Exact-word overlay before image decisions | 14/190 | 22/37 |
| Exact overlay outstanding word/digit/extra groups | 761 | 24 |
| Exact overlay punctuation groups | 59 | 4 |

These are different verification rules on the same prepared material, not evidence that the law itself changed. Many base disagreements are OCR errors or edition spelling differences. The JO comparison remains available; its unresolved differences are not silently waived by checking another edition.

## Current Ministry edition: unchanged bodies

The official Ministry bilingual PDF is an additional transcription witness: [code_famille.pdf](https://droit.mjustice.gov.dz/sites/default/files/code_famille.pdf), SHA-256 `662d81783f6c0547a0e20b1387199a63dd8ddf5a21fe50d26fe9b0cc0d6f96d8`, retrieved 26 October 2026. Its Arabic column on PDF pages 2–43 has now been read by Tesseract (best Arabic model) and PP-OCRv5 on 42 hash-bound crops each. These are two recognizers of the same source, not two independent legal authorities.

`family_literal.py` separates Arabic punctuation from words without changing the legacy shared tokenizer or its recorded token indices. Acceptance requires exact word/number forms under the documented normalization. A comma cannot hide a missing negation; a separator between digits is treated as critical rather than deferrable punctuation. Annotations remain in raw alignment but are excluded from the body only with an exact projection guard.

- **128/190 unchanged bodies** have strict word agreement with this edition's OCR.
- The remaining **62 bodies** were read on source images. Together with the paragraph-only flags, **78 whole articles** were inspected on 40 sheets. No further word corrections to those Ministry candidates were found.
- All **27 French/Arabic paragraph-count flags** were inspected against the Arabic print. Twenty-six retain the printed Arabic structure. Art. **178** needed one page-break continuation joined: its final distribution line on PDF page 37 had been split into a second paragraph. The first generated article view omitted that line, so the full next-page Arabic crop was also inspected and bound to the decision.
- **18 bodies retain unconfirmed punctuation notes** under the approved rule. These are recorded explicitly, not called image-confirmed.

`image-decisions-ministry-whole.json` records the actual readings with text/image hashes. `apply_family_ministry_review.py` refuses a changed body/proof, missing word review, an unreviewed paragraph-count flag, contradictory agreement, or a decision outside the unchanged scope. Its `unchanged-ministry-reviewed.json` contains all **190 Ministry-edition body comparisons**, with the paragraph correction and remaining punctuation notes. It does **not** establish word-for-word identity with the 1984 JO, legal applicability or whole-code completion. The 37 amending-JO bodies below retain precedence, including the substantive art. 8 correction.

French labels independently enumerate **all 232 identities**, with no missing, duplicate or unexpected label. Printed forms `Art. 10 –` and `Art. 53. bis –` have explicit parsing regressions. This checks numbering and structural layout, not translation equivalence. Arabic paragraphs have not been forced to match French formatting.

The old 1984 crop sometimes clipped the rightmost letters (observed in art. 34). Crop generation now preserves an outer page edge when no printed frame exists; 30 full-edge Tesseract crops have finished under new IDs. The original evidence is preserved. This is an alternate crop of the same source/recognizer, not independent agreement. Their aggregate JO comparison has not yet been rebuilt.

## The 37 replaced/inserted bodies

All 28 exact-overlay items for these bodies were inspected on seven source-image review sheets, with a full-page second look for the clipped references in art. 48. The 22 bodies without an exact-overlay discrepancy plus the 15 image-reviewed bodies account for all 37.

Eight hash-guarded string corrections follow the official print:

- Art. 8: `الزواج` → `الزوج` at the reviewed location.
- Art. 30: a colon at the second list introduction.
- Art. 36: remove the extra conjunction before `بالحسنى` and restore the printed final full stop there.
- Art. 49: `إبتداء` → `ابتداء`.
- Art. 53: `التالية` → `الآتية`, and `إرتكاب` → `ارتكاب`.
- Art. 57: `للإستئناف` → `للاستئناف` in both reviewed places.

`image-decisions-amended-exact.json` binds every decision to its image hashes. `apply_family_review.py` refuses missing decisions, changed proof, a wrong old text or a linked correction belonging to another article. Its `amended-corrected.json` records **body comparison only**, with final text hashes. It does not assign whole-code verification or assert that paragraph and path QA are finished.

## Remaining before delivery

1. Resolve the four now-isolated official-edition conflicts above, acquiring/reading any applicable official rectification. The 190-body original-JO collation itself is complete; do not repeat its OCR or the 176 finished image readings.
2. Finish paths and cross-references. All37 amended-body paragraph layouts and all27 flagged unchanged-body paragraph counts have actual source-image decisions; French compilation numbering is complete.
3. Finish the typed candidate's amendment audit, honest temporal limits and legally needed older regimes. Source fragments and separate Ministry transcription witnesses are now present. Record Law 05-09's approval of Ordinance 05-02 without inventing article changes; local acquisition of its Arabic publication timed out.
4. Finish the remaining content QA, then repeat the schema, typed-loader, full build and regression gate for final delivery. The 20-article sample and legacy Family reconciliation below are now complete for this candidate.
5. Recheck latest amendment discovery; the previous live index attempt timed out, so the cached cut-off is not silently advanced.

Evidence is in `data/law/corpus/research/family-2026-10-28/`; raw PDFs, images and recognizer outputs remain in the ignored cache. Reproduce the audit with `family_exact_audit.py`, and apply the recorded amended-body decisions with `apply_family_review.py`. Do not overwrite reviewed/corrected material with preparation output without reapplying those decisions.

## Typed candidate and integration boundary

`build_family_candidate.py` assembles `data/law/corpus/family-code/`: **227 current body candidates and five repeal markers**, with the expected 232 identities and no numbering gap. It checks pinned PDFs and cached artifact hashes before assembling; it binds body checks to final text hashes. This is a current-snapshot assembly, **not** execution of a complete historical consolidation plan. The amendment inventory remains unapplied in the report until final reconciliation and heading QA are finished. `scope.complete` and `inventory.complete` remain false.

The schema/model has one optional addition, `source.transcriptionWitnesses`. Each official compilation witness carries its **own** document URL, SHA-256, retrieval date, PDF pages, raw native/OCR streams, evidence IDs, current text hash and explanatory note. The legislative `source.publicationId` and introducing publication remain separate and retain their JO locators and raw streams. The typed loader rejects unofficial witnesses, stale text hashes, missing raw streams and duplicate pages/evidence. Adding a witness grants no verification check and cannot substitute for a missing legislative source.

All 232 current records have unresolved temporal bounds; no national commencement date or older body was guessed. `VersionOn` must therefore continue to return an unsettled result. **Do not integrate this candidate as a completed or date-applicable legal library.** The app and legacy loaders have not been switched to it. Consumers of the new optional field must use the updated corpus reader.

The candidate records internal explicit cross-references, with their original quote and paragraph. Four external targets remain disclosed in the assembly audit/report rather than being falsely mapped to Family numbers: art. 82 → Civil art. 42, art. 83 → Civil art. 43, art. 86 → Civil art. 40, and art. 120 → civil-status art. 64. Their texts/typed links need the appropriate in-scope import or explicit reference handling; no special-law text was started. Reference tests caught and fixed an extractor omission for attached `للمادة` / `للمادتين` forms.

## Random source sample and legacy comparison

The [20-article proof set](law-corpus-checks/family-code/README.md) is drawn without replacement from all 232 identities using seed `20261028`. All ten review sheets were actually inspected. The draw contains 15 unchanged Ministry-witness bodies and five amended-JO bodies; no repeal happened to be selected. It independently includes art. 8, whose corrected `الزوج` is confirmed again, and art. 178, including the final line on the next page and the joined paragraph. All sampled words, figures, negations and Arabic paragraph breaks match the displayed source under the named normalization. This is a current snapshot sample, not a certification of every 1984 JO word or printed vocalization.

The 22 small source crops (two articles span pages), selection, actual inspection record and offline [side-by-side HTML](law-corpus-checks/family-code/comparison.html) are included. `spotcheck_family.py --report-only` refuses a stale population, changed body/paragraphs, changed proof, unmatched selection or nonmatching verdict. Generation alone never records an inspection. The HTML was rendered and visually checked.

Reused `reconcile_existing.py` without changing its comparison rule (NFC and collapsed whitespace only). All **25 legacy Family records** are compared: **19 same-text**, **six different-text** (48, 53, 66, 74, 75, 79), with raw hashes and full diffs. A text difference is not automatically a legal-version change. Across all 81 legacy records the current counts are 25 same-text, nine different-text and 47 not yet imported; every absence is listed. Legacy texts and verification flags remain untouched.

[Law 05-09, official French JO43/2005 p. 3](https://www.joradp.dz/jo2000/2005/043/FP3.pdf) approves Ordinance 05-02 and provides for publication; it supplies no replacement Family body. The French publication was accessible through the research browser, but direct local downloads of the Arabic issue and the small French page timed out. The candidate explicitly discloses the missing local approval-source pin instead of inventing a hash or claiming its ledger is final. Latest-amendment discovery still uses the pinned 26 October Ministry index; a search-cache result is not treated as a newly acquired index.

The shared comparison's default behavior and existing CPCA corpus have not been changed. The CPCA's existing `checked` state reflects its recorded method; an independent exact-word overlay for that code remains a separate QA task. No new CPCA failure count is inferred from the Family findings.

Files changed outside the corpus/tool directories in this continuation: this report, `docs/LAW-CORPUS.md`, the permitted sample directory `docs/law-corpus-checks/family-code/`, the corpus model/loader under `src/DzLegal.Law/Corpus/` and their tests under `tests/DzLegal.Law.Tests/Corpus/` (all within the brief's approved areas except the additional named report). The previous checkpoint also added the QA-limitation note in `docs/LAW-CORPUS-CPCA.md`. New regressions cover source edges, annotation projection, Arabic punctuation/negation/numeric separators, French label aliases, stale/missing image evidence, paragraph continuation, disclosed punctuation, compilation provenance and cross-code/bis references. All **149 Python tests** pass. The typed loader accepts all four code packages and schema validation has 0 errors. Checkpoint `40dbaf0` passed the expanded full gate: **692 .NET tests, 0 build warnings/errors**. No test was weakened.
