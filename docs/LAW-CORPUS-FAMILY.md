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

These are different verification rules on the same prepared material, not evidence that the law itself changed. Many base disagreements may be OCR errors or orthographic variants; they still require resolution before an exact-word claim.

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

1. Resolve the base exact-word backlog from the official images, retaining genuinely unreadable items as unsettled. Improve or add OCR only where it reduces this work without lowering the acceptance rule.
2. Check paragraph boundaries, paths, French structural alignment and cross-references.
3. Build the typed current package with the amendment audit, source fragments, honest temporal limits and legally needed older regimes.
4. Reconcile the old Family records, draw and actually inspect the 20-article sample, and complete schema, typed-loader, full build and regression checks.
5. Recheck latest amendment discovery; the previous live index attempt timed out, so the cached cut-off is not silently advanced.

Evidence is in `data/law/corpus/research/family-2026-10-28/`; raw PDFs, images and recognizer outputs remain in the ignored cache. Reproduce the audit with `family_exact_audit.py`, and apply the recorded amended-body decisions with `apply_family_review.py`. Do not overwrite reviewed/corrected material with preparation output without reapplying those decisions.

The shared comparison's default behavior and existing CPCA corpus have not been changed. The CPCA's existing `checked` state reflects its recorded method; an independent exact-word overlay for that code remains a separate QA task. No new CPCA failure count is inferred from the Family findings.

Files changed outside the corpus/tool directories at this checkpoint: this report, `docs/LAW-CORPUS.md`, and the QA-limitation note in `docs/LAW-CORPUS-CPCA.md`. The eight new regressions cover exact-word/digit acceptance and missing/changed image decisions, correction guards and final text binding. All 130 Python tests pass; no test was weakened.
