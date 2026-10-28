# Family Code: preparation and source-check record

Updated 28 October 2026, branch `codex/law-corpus`. This is **not a completed Family delivery**. There is no typed Family package yet. Image readings are Codex's (AI), never human or lawyer verification.

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

1. Finish collation of unchanged Ministry bodies against the 1984 JO, distinguishing edition spelling/punctuation from substantive wording. The completed Ministry transcription check above cannot automatically settle a JO disagreement.
2. Finish paths, amendment paragraph checks and cross-references. French numbering is complete and all 27 flagged unchanged-body paragraph counts have image decisions.
3. Build the typed current package with the amendment audit, source fragments, honest temporal limits and legally needed older regimes. Represent the Ministry transcription witness explicitly; do not attribute its text/PDF hash to the 1984 legislative publication. Record Law 05-09's approval of Ordinance 05-02 without inventing article changes.
4. Reconcile the old Family records, draw and actually inspect the 20-article sample, and complete schema, typed-loader, full build and regression checks.
5. Recheck latest amendment discovery; the previous live index attempt timed out, so the cached cut-off is not silently advanced.

Evidence is in `data/law/corpus/research/family-2026-10-28/`; raw PDFs, images and recognizer outputs remain in the ignored cache. Reproduce the audit with `family_exact_audit.py`, and apply the recorded amended-body decisions with `apply_family_review.py`. Do not overwrite reviewed/corrected material with preparation output without reapplying those decisions.

The shared comparison's default behavior and existing CPCA corpus have not been changed. The CPCA's existing `checked` state reflects its recorded method; an independent exact-word overlay for that code remains a separate QA task. No new CPCA failure count is inferred from the Family findings.

Files changed outside the corpus/tool directories in this continuation: this report and `docs/LAW-CORPUS.md`. The previous checkpoint also added the QA-limitation note in `docs/LAW-CORPUS-CPCA.md`. The new regressions cover outer-edge clipping, annotation projection, Arabic punctuation/negation/numeric separators, French label aliases, stale/missing whole-article evidence, scope, paragraph continuation and disclosed punctuation. All **145 Python tests** pass; no test was weakened. The last pushed full .NET gate (`8a30604`) passed all 687 tests with 0 build warnings/errors; a new full gate follows the next commit.
