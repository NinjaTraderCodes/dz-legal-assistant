# CPCA (Law 08-09 as amended by Law 22-13): code report

Delivery 2, 28 October 2026, branch `codex/law-corpus`. Image readings are Claude's (AI); no lawyer or human has verified them. `verified` / `verifiedBy` are untouched.

## Result

| | Count |
|---|---:|
| Articles | 1,084 (1,065 of Law 08-09 + 19 inserted by Law 22-13) |
| Versions | 1,162 |
| `checked` | **1,162** |
| `partly-checked` / `unsettled` | 0 / 0 |
| Inventory | complete: every base, inserted and repealed number accounted for; 0 gaps |
| Temporal status unsettled | 175 versions (see below) |

**Versions by origin:**

- 1,065 Law 08-09 versions, in force since 23 April 2009.
- 91 Law 22-13 versions:
  - 60 full replacements;
  - 9 partial restatements (600, 801, 834, 840, 848, 849, 878, 931, 976);
  - art. 32 losing paragraphs 7–10;
  - 532 and 536 restated «بدون تغيير» with new headings;
  - 19 insertions.
- 6 repeals: 826, 835, 836, 912, 913 and 914.

**Amendments after 22-13:** none listed. The Justice Ministry codes index (fetched 26 Oct 2026; it lists texts to 12 Oct 2026) names only Law 22-13 for the CPCA.

## How the text was checked

1. **Sources.**
   - **Law 08-09:** the Ministry HTML (1,065 articles), compared with the printed JO 21/2008 (pages 3–95).
   - **Law 22-13:** the Arabic text layer of JO 48/2022 (pages 3–11), cleaned by tested transforms, compared with its own print.
2. **Comparison with two OCR readings.** Both recognisers, Tesseract and PP-OCRv5, read every printed column. A targeted Tesseract re-read then covered the tokens neither confirmed. Words, digits and negations must match exactly.
3. **Agreement.** Without any image decision, **788 of 1,065** base articles agree (74.0%), and **74 of 90** Law 22-13 bodies (82.2%).
4. **Image review.** Every remaining difference in words, digits, article numbers or negations was read on the page image and recorded with its reason:
   - **Base.** 556 review items plus 7 whole-article second looks. The decisions file holds 561 decisions:
     - 428 where the print agrees with the HTML;
     - 131 corrections from the print;
     - 2 that are part of another correction.
   - **Law 22-13.** 42 decisions, including 12 corrections.

### Corrections taken from the print

- **The Ministry HTML has lost every colon.** Its 1,065 articles contain no «:», and some full stops are missing too. The review restored 80 colons and 40 full stops where the print shows them.
- **10 spacing and abbreviation fixes.** Examples: «المادة165» → «المادة 165», «من815» → «من 815», and «دج» → «د.ج» in art. 883.
- **Art. 1065.** The HTML adds the heading «أحكام انتقالية وختامية» and the signature block to the article; the print ends it at «…الشعبية.».
- **Law 22-13 text layer.**
  - The layer drops word spaces 8 times, e.g. «يختصالقسم» → «يختص القسم».
  - It splits a word 4 times, e.g. «ه ذا» → «هذا».
  - Its harakat are displaced, so they are not carried; nor does the 2008 HTML text carry harakat.

### Punctuation rule (28 Oct)

Punctuation-only differences do not block `checked`:

- the corrections above keep the printed punctuation that is known;
- punctuation that neither OCR nor the review settled is listed on the version, in its transcription check note (`punctuationUnverified: true`, with each mark).

This applies to 15 versions, with 16 marks.

**Differences left in words, digits or negations: 0.**

## Temporal limits (175 versions with `temporalStatus: unsettled`)

Law 22-13 sets no calendar commencement date. Its art. 13 ties the new subject-matter and territorial jurisdiction rules to the installation of the new administrative courts of appeal and specialised commercial courts. It also keeps earlier-filed cases with the former courts.

So the 97 Law 22-13 versions and the 78 Law 08-09 versions they replace have no date boundary. No date is inferred, and each version is listed in `report.json` with this reason.

## Spot-check

`docs/law-corpus-checks/cpca/` shows 24 current versions beside their printed crops. All 24 match word for word.

- **20 versions drawn at random** (seed 20261027) from all current versions.
  - On art. 829 the crop cuts off the last printed line. That line was confirmed by the OCR comparison, not by the inspection.
- **4 versions introduced by Law 22-13**, added because the random draw held none (same seed): 535, 600, 804, 812.
  - Art. 600 checks the consolidation itself: paragraph 1 and item 7 follow the 2022 print, and the rest the 2008 print.

## Evidence

`data/law/corpus/research/cpca-2026-10-26/`:

- comparison results and image decisions;
- the base review log;
- the consolidation plan and audit (hash-guarded operations).

The review sheets and strips are in the ignored cache `tools/law-corpus/.cache/evidence/reviews/`; they can be regenerated from the pinned PDFs.
