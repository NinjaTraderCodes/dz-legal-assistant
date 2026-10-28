# Law corpus: progress report, 28 October 2026

Branch `codex/law-corpus`. This report covers what Claude did on this branch under the law-corpus brief: the cache move, delivery 1 and the CPCA delivery in progress. Everything listed as "on disk" is not yet committed or pushed. Image readings are Claude's (AI); no lawyer or human has verified any text, and `verified` / `verifiedBy` are untouched.

## Where things stand

| Step of the brief | State |
|---|---|
| 1. Priority articles `checked` | **Done**: committed and pushed (`1dfc02d`) |
| 2. CPCA, complete and current | **In progress**: about 85% of the work done, nothing committed yet |
| 3. Family Code | Not started |
| 4. Civil Code (font decoding first) | Not started |
| 5. Commercial (37), Labour 90-11 (29), 90-04 (1) | Not started |
| 6. Reconcile the 81 legacy articles | Not started |

The local branch is level with `origin/codex/law-corpus`. Delivery 2 work is in 16 untracked paths.

## Committed and pushed

### Cache move (`41fa46d`)

The OCR evidence cache moved from `data/` to `tools/law-corpus/.cache/evidence`, so an ordinary build can no longer copy PDFs, page images or OCR files into the build output. Proof: a Release build outputs 0 cache files.

### Delivery 1: priority articles (`1dfc02d`)

All 11 priority articles are `checked` in their current version:

- Civil 124, 138, 139, 140, 308, 309, 379
- CPCA 2, 504
- Commercial 187 bis, 187 bis 1

What the work involved:

- **Reading.** Each text was read word by word from the official JO page image (150–1,200 dpi), with every digit, negation and punctuation mark checked. Each reading is bound to the text and image SHA-256.
- **Corrections from the images.** The earlier candidate texts had modernised the 1975 spelling:
  - Civil 309: «في ذمة», «آداؤه»
  - Civil 379: «آكد»
  - Civil 140: «مايلزم», «فان», and the printed comma at the end of paragraph 2
- **New article.** Civil 124 was added from Law 05-10 (JO 44/2005 p. 23).
- **Amendment discovery.** The Justice Ministry codes index (fetched 26 Oct 2026; it lists texts up to 12 Oct 2026) gives:

  | Code | Amending texts listed |
  |---|---|
  | CPCA | 22-13 only |
  | Family | 05-02 only |
  | Civil | Nothing after 07-05 |
  | Labour 90-11 | Adds 22-16 (JO 49/2022) |

- **Temporal.** Civil 124 and Commercial 187 bis and bis 1 are `checked` text with `temporalStatus: unsettled`, because their laws set no national commencement date.
- **Gate.** Build with 0 warnings, 687 .NET tests, 67 Python tests, schema 0 errors, loader OK. A spot-check is in `docs/law-corpus-checks/priority/`.

## Delivery 2 (CPCA): done so far, on disk

### OCR of the official pages

Both recognisers have read all 204 column crops: JO 21/2008 pages 3–95 (Law 08-09) and JO 48/2022 pages 3–11 (Law 22-13). The two recognisers are Tesseract (best model) and PP-OCRv5. Column boundaries are detected from the page pixels.

### Law 22-13 text (the current wording of 90 articles)

The Arabic text comes from the JO 48/2022 text layer. It is cleaned only by documented, tested transforms (`jo_layer.py`):

- lines reordered from glyph geometry, because the layer's blocks are out of order;
- Arabic-Indic digits mapped to the printed Western digits;
- kashida runs removed;
- harakat dropped, because the layer displaces them.

The result is 90 quoted bodies: 69 replacements, 19 insertions, and 532 and 536 restated «بدون تغيير».

After comparison with both OCR streams and a targeted re-read, **74 of 90 agree** with no image decision (82.2%). I read every remaining difference on the page image:

- **12 corrections.** The layer dropped a word space eight times (e.g. «يختصالقسم» → «يختص القسم») and inserted one four times (e.g. «ه ذا» → «هذا»).
- **27 confirmations.** These were full stops, digits and dashes that OCR missed.
- **The art. 14 repeal, read on both prints.** Law 22-13 repeals paragraphs 7–10 of art. 32, and the 2008 print shows ten paragraphs, matching the Ministry HTML.

Decisions are in `research/cpca-2026-10-26/image-decisions-22-13.json`.

### Law 08-09 base text (1,065 articles, Ministry HTML)

- **Clean-up.** Tatweel, harakat and stray spaces before punctuation are removed (`text_clean.py`). Nothing is added or replaced.
- **Comparison.** Every article was compared with both OCR streams. All 1,065 were located in the printed sequence. **555 agree** outright; **786 agree (73.8%)** after the targeted re-read.
- **Finding: the Ministry HTML has lost every colon.** It has 0 «:» in 1,065 articles, and it also lacks some printed full stops. Every place where a mark may be missing was added to the review.
- **Image review: all 93 sheets read** (556 items). Decisions:

  | Decision | Items |
  |---|---:|
  | Print agrees with the HTML | 379 |
  | Running page header, not text | 21 |
  | Colon or full stop to insert | 120 (80 colons, 40 full stops) |
  | Word-spacing or abbreviation fix | 10 (e.g. «المادة165» → «المادة 165», «دج» → «د.ج» in art. 883) |
  | Strip showed the wrong line; needs a second look | 23 |
  | Structural fix (art. 1065) | 3 |

  The structural fix is needed because the HTML body of art. 1065 also carries the heading «أحكام انتقالية وختامية» and the signature block, which the print places outside the article.

- **Semicolons.** I sampled 24 of the 259 places where Tesseract read «؛». None is printed; all were commas misread.

### Tools written (37 new Python tests, all passing)

| File | What it does |
|---|---|
| `jo_layer.py` | Cleans the JO 48/2022 text layer |
| `extract_22_13.py` | Cuts out the quoted bodies |
| `text_clean.py` | Cleans the Ministry HTML |
| `native_check.py` | The comparison rule: digits, punctuation and negations exact. Dash widths count as one mark, and a lone letter is never treated as OCR spacing. |
| `check_native.py` | Places all articles in reading order at once, drops page headers and dot leaders, and cuts each span at the next article's label |
| `reread.py` | Third, targeted Tesseract reading for unconfirmed tokens |
| `review_items.py`, `review_sheets.py` | Strip images and review sheets |
| `deliver_cpca.py` | Inputs, corrections, mark candidates; the build and spot-check steps are written but not yet run |

## What remains for the CPCA delivery

1. Look again at the 23 items whose strip showed the wrong line, then record all base decisions in `image-decisions-base.json`. The corrections step also needs to support insertions (it supports replacements only today).
2. Apply the corrections, re-run the comparison, and confirm that every remaining difference has a recorded decision.
3. Run the build:
   - the consolidator applies Law 22-13: 69 replacements, 9 partial edits (e.g. art. 600 item 7), 19 insertions, 6 repeals and the art. 32 partial repeal;
   - paths and headings are updated;
   - ledger, articles and report are written.
4. Pass the gate: build, .NET and Python tests, schema, typed loader.
5. Spot-check 20 random articles beside their crops in `docs/law-corpus-checks/cpca/`, update `LAW-CORPUS.md`, then commit, push and send a short report.

**Expected result.** The 1,065 base and 90 Law 22-13 texts should all become `checked` once the remaining decisions are recorded.

The 22-13 versions will carry `temporalStatus: unsettled`, because the law gives no calendar commencement date. Its art. 13 also ties the new jurisdiction rules to the installation of the new courts and keeps earlier-filed cases under the former rules. That limit will be stated in the report.
