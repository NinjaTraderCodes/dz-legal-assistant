# Safe pause: law corpus, 28 October 2026

Paused at the user's request. **Do not resume extraction or review until the user asks.** This checkpoint is on `codex/law-corpus`, in `redteam-output/law-corpus-worktree`; the default branch and application are untouched.

## What was recovered

The local Claude project session `88248e4a-c351-4e3d-b4a6-76f4dd9d774b` was readable. Its actual final state was recovered, including the uncommitted Family preparation and completed background OCR. The transcript itself was not copied into the repository.

- `ec5b96c`: CPCA delivery, 1,084 identities / 1,162 checked transcription versions; 175 temporal boundaries remain unsettled as already documented.
- `9636344`: the requested additional CPCA spot-checks were already complete: 535, 600, 804 and 812. Do not repeat them as unfinished work.
- `eb43a21`: glyph-specific decoding for Family and Civil was already committed. Its background OCR validation also finished and is now recorded in `research/family-2026-10-28/glyph-validation.json`.
- `5ab86c5`: recovered Family preparation, both initial comparisons, source records and seven boundary/crop regression tests; committed, pushed, and validated with 0 build warnings/errors, 687 .NET tests and 116 Python tests.

## Progress since takeover

1. All **40 Family official-publication column crops** finished in both Tesseract and PP-OCRv5. Hash replay accepted all 40 Tesseract crop records. No download or original OCR batch needs repeating.
2. Family preparation now has **232 distinct identities**: all 224 base numbers plus eight insertions. The `8 bis 1` identity is no longer confused with `8 bis`; duplicate/missing inventories stop preparation. The signature is excluded from article 224 and retained as evidence.
3. A second boundary correction prevents the flush-right heading `الفصل الخامس` from becoming the body of repealed article 39. Unnumbered subheadings now replace sibling subheadings instead of accumulating inside a chapter title. The updated preparation and comparisons were replayed for the pause. Paths and paragraphs still need substantive source QA.
4. The independent **Ordinance 05-02 target inventory** was read on JO15/2005 pages 19–22: **29 replacements, eight insertions, five repeals**, plus the section restructuring and publication clause. Its targets match the compilation's annotations. The inventory and image hashes are committed; article-body and consolidation certification are not inferred.
5. Targeted rereading exposed a Windows Tesseract defect: Arabic filenames fail, while the identical image at an ASCII filename succeeds. Temporary reread names are now ASCII. Nonzero OCR exits raise an error, and successful per-strip readings are cached against the image, executable and Arabic traineddata hashes. An interrupted batch can reuse its completed OCR calls.
6. The **amended-candidate reread completed**: 25/37 bodies agree under the inherited comparison rule, leaving 20 discrepancy groups. This record, `amended-reread-pre-heading.json`, belongs to the input snapshot at `5ab86c5`, before the subsequent heading correction. It remains evidence, not a final certification.
7. The **base-candidate reread was stopped** for this pause before it wrote its aggregate. **408 base readings and 72 amended readings** survive as valid per-strip cache records: 480 total, zero damaged records at the pause. They are OCR-call counts, not article counts or checked versions. `pause-progress.json` indexes their hashes.

There is **no typed Family delivery and no newly certified Family article yet**. The current typed corpus remains CPCA + the existing Civil/Commercial subsets. The progress is the recovery, clean preparation, independent amendment inventory and discrepancy-reduction work needed for that delivery.

## Exact next actions after resumption

From this worktree, use `tools/law-corpus/.cache/venv/Scripts/python.exe`; keep `OMP_THREAD_LIMIT=1` for the recorded reread configuration.

1. Read this file, `docs/LAW-CORPUS.md`, and `research/family-2026-10-28/preparation-summary.json`. Confirm the branch and working tree. Do not start from the obsolete 26 October resume counts.
2. Preparation replay command, if a new replay is needed:

   ```powershell
   tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/compare_family_current.py
   ```

   This reconstructs 232 source candidates and comparisons against JO24/1984 and JO15/2005. It grants no verification state. Do not overwrite later image corrections without integrating their explicit hash-guarded decisions.
3. Resume the base targeted reread, which reuses intact per-strip cached readings:

   ```powershell
   $env:OMP_THREAD_LIMIT='1'
   tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/reread.py data/law/corpus/research/family-2026-10-28/base-unchanged-input.json data/law/corpus/research/family-2026-10-28/base-unchanged-comparison.json tools/law-corpus/family-current-manifest.json A1984024 --name family-base --out data/law/corpus/research/family-2026-10-28/base-unchanged-reread.json
   ```

4. Replay the completed amended reread against the latest preparation, reusing the same cache:

   ```powershell
   tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/reread.py data/law/corpus/research/family-2026-10-28/amended-input.json data/law/corpus/research/family-2026-10-28/amended-comparison.json tools/law-corpus/family-current-manifest.json A2005015 --name family-amended --out data/law/corpus/research/family-2026-10-28/amended-reread.json
   ```

5. Review remaining word, digit, negation and boundary differences against the official images. Five amended review sheets were generated under the ignored `tools/law-corpus/.cache/evidence/reviews/family-amended/`; **they have not been visually reviewed by Codex**. Regenerate after replay if item positions change. The full amendment instruction pages 19–22 were inspected for the operation inventory, not as a substitute for every body comparison. Unconfirmed punctuation follows the approved disclosure rule; it is never silently called confirmed.
6. Check heading paths, published paragraph boundaries, French structure and reference targets. Apply explicit corrections with text/image hashes. Then build the typed current Family corpus, retain legally required temporal qualifications and older regimes, reconcile the legacy Family records, generate and inspect the 20-article random sample, and run all required validation before a completed delivery claim.
7. Continue the agreed scope afterward: full Civil; only the frozen Commercial/90-11/90-04 chapters, explicitly partial. Comprehensive history is deferred except legally needed older provisions/transitions. **No special laws, no unrelated application changes and no dependence on an external reviewer.**

## Evidence, validation and limitations

- Research outputs: `data/law/corpus/research/family-2026-10-28/`.
- Raw PDFs, images, two original OCR streams, reread records and pre-takeover backups: ignored `tools/law-corpus/.cache/`; preserve them.
- The Ministry index re-fetch timed out. There is no new live amendment-discovery cut-off beyond the cached index; close that discovery check before final delivery.
- The half-day font investigation has yielded its bounded mapping. Do not restart an unbounded font project. The Civil corpus bodies themselves have not yet been rebuilt from it.
- No new background OCR job should remain running at the pause. The interrupted base shell session ended after its owned Python/Tesseract processes were stopped. All other started validation/rendering sessions completed.
- The latest Python suite passes **122 tests**. The pause commit must also pass the required full Release build and all 687 .NET regressions after commit; the validation logs are `tools/law-corpus/.cache/resumed-full-build.log` and `resumed-full-tests.log`.
- Changes outside the corpus/tool directories: `docs/LAW-CORPUS.md` and this resume document only. No application/old loader behavior, old legal library, claim catalogue, prompts or safeguards changed.

This is a recoverable intermediate checkpoint, not completion of the Family Code or the whole corpus assignment.
