# Corpus restart record - 29 October 2026

The user resumed after the second safe pause. Work remains on `codex/law-corpus` in `redteam-output/law-corpus-worktree`. This record supersedes the earlier pause files as to activity and Civil OCR progress. The agreed scope remains full current Civil/CPCA/Family and bounded Commercial/labour chapters; full history is deferred except legally needed regimes. No special laws.

## Saved checkpoint

`0380e3ff06360015dcb48d88d306f5919e81f317` is committed and pushed. It contains the Family heading/current-outcome work and initial Civil strict-audit tool. It passed zero-warning/error Release build, all 692 .NET tests, 182 Python tests, schema validation, all four typed packages, scope inventory and the existing Family sample. All 232 Family texts/paragraphs/sources/references/dates/states were compared with the preceding commit and remain unchanged; 48 metadata paths changed.

Automatic approval review initially rejected the push because authorization for the external destination was not established. The user then explicitly approved pushing this checkpoint to `https://github.com/NinjaTraderCodes/dz-legal-assistant.git`, branch `codex/law-corpus`. The push succeeded and the remote hash was verified. Raw PDFs and OCR cache were not uploaded.

## Civil OCR and comparison

- Both readers are complete: **164/164 Tesseract and 164/164 PP-OCR**, including the four final pages 168–171. Do not repeat these readings.
- The final aggregate `.cache/civil-ministry-paddle.json` contains all 164 distinct records. Source/crop/artifact hashes replayed through `civil_ministry_audit.py`. PowerShell treated the OCR logger's informational stderr as a `NativeCommandError`; the complete aggregate, final completion marker and independent artifact replay establish actual completion. No OCR exception was found.
- `ministry-exact-audit.json`: original global diagnostic, **510/1,059 strict word agreements**, 1,000 word/digit/extra groups, 216 punctuation groups. It includes 279 neighbouring-page alignment flags.
- `ministry-page-bounded-exact-audit.json`: additional diagnostic, **673/1,059 strict word agreements**, 702 word/digit/extra groups, 216 punctuation groups. `bound_civil_alignment.py` removes only tokens on pages outside a candidate's recorded source pages. It removes no native word and no same-page OCR token, refuses disjoint projections and keeps the original comparison. This is not looser spelling acceptance.
- All **58 word/figure/extra flags among source-order candidates 0–149** were actually inspected on ten sheets plus five wider heading/footer views. The curated decisions are `image-decisions-ministry-words.json` (0–50) and `image-decisions-ministry-words-0050-0150.json` (50–150).
- Their guarded application produces **26 spacing repairs in 25 separate derived research bodies**: `ministry-word-corrected.json` and `ministry-word-corrected-0050-0150.json`. Raw native/prepared/typed records remain intact. Two repairs restore source-visible `لم` from extracted `ل م` in 108 and 119; all figures and letters are preserved. Correctly printed words, comma OCR artifacts, headings, labels and page numbers have explicit decisions.
- No typed Civil delivery or new checked state is claimed. The package remains 45 identities/55 versions. The 1,059 native records include repeal annotations and ten locally numbered management-contract records; they are not 1,059 verified operative articles.

## Exact continuation

1. Check the latest commit and post-commit validation in the local recovery metadata/final handoff before editing. New checkpoint changes after `0380e3f` comprise the completed Civil comparison, word review tools/decisions and these reports.
2. Continue Civil source-image review at **source-order offset150** (not printed article150). Reuse the strict diagnostics and both completed readers:

   ```powershell
   .\tools\law-corpus\.cache\venv\Scripts\python.exe -B tools/law-corpus/review_civil_items.py --start 150 --count 100 --per-sheet 6
   ```

   Generation records no reading. Inspect every emitted sheet and wider source views wherever the strip clips relevant context. Store explicit decisions with original body/audit/image hashes; use separate per-batch files. `apply_civil_word_review.py --decisions <batch.json> --out <derived.json>` replays the targeted reviews without replacing raw data or typed law. Keep critical digits/negations strict. Punctuation-only items remain in the audit, outside the word batches.
3. Complete guarded editorial-marker/callout projection, paragraph/path checks (known 489 paragraph merge remains), all JO amendments/repeals/insertions and qualified local management-contract1–10 identities. Preserve general1–10 and the earlier checked/historical pilot. Then full current import, independent inventory/French structure, sample, legacy reconciliation and gates.
4. Family current headings and 2005 outcomes pass; do not repeat their body/heading readings. Remaining: conflicts135/151/202/203; official French rectifications and structure; local approval05-09 pin; latest discovery; references82→Civil42,83→Civil43,86→Civil40,120→civil-status64; temporal limits and legally needed older regimes. Direct source acquisition still timed out and the browser host was unavailable. Do not infer amendments/dates or start the special law to fill that reference.
5. Bounded collections still required: Commercial37 targets, Labour90-11's29 and90-04's one whole-chapter target. Mark them partial and list covered chapters. No comprehensive historical expansion or special-law work.

Raw evidence and generated views remain under the ignored cache, with committed pins and curated decisions. The app, legacy law library, claim catalogue, model prompts and safety behavior remain unchanged. Files outside corpus/tool areas in this checkpoint: this record, `docs/LAW-CORPUS.md` and `docs/LAW-CORPUS-CIVIL.md`. Tests add page-bound alignment, qualified proof filenames and guarded word-repair regressions; no existing tests were removed or weakened.
