# Safe pause — 28 October 2026

The user explicitly requested this pause. **Do not restart OCR, corpus expansion or research until the user resumes.** The user subsequently confirmed that disk space is no longer an issue; that does not cancel the pause.

Branch: `codex/law-corpus`. Worktree: `redteam-output/law-corpus-worktree`. The application/default branch and legacy law library remain untouched.

## Commits and preservation

Two completed checkpoints were committed, fully validated and pushed before this pause:

- `c9c3480` — original-JO Family collation, guarded article42 correction and explicit quarantine of four official-edition conflicts.
- `61b293e` — 1,059 decoded Civil source candidates, qualified local numbering, source-bound findings and recoverable OCR pipeline.

The later Family paragraph/source-locator work and this handoff are being saved as the pause checkpoint; its hash is available in `git log -1`. Earlier attempts to stage/commit that work failed during a disk-space error and created no commit. The final handoff states whether the new attempt and post-commit gate succeeded. No source or evidence was deleted to recover space.

Raw PDFs, page images, OCR records and generated review sheets remain in the ignored `tools/law-corpus/.cache/` tree. They are not pushed. Committed source pins and research records identify them. A local recovery copy of the pending patch/untracked files and a hash audit are saved under `.cache/recovery-20261028-final-pause/`.

## Achieved: Family Code

- Typed current candidate: **232 identities / 232 snapshot records**, comprising227 body candidates and five repeal markers. Verification states remain **0 checked, 228 partly-checked, four unsettled**; all232 temporal boundaries remain unresolved.
- Original1984 JO collation now accounts for all190 unchanged bodies: 14 strict OCR agreements, 171 actual image-collated matches with disclosed edition orthography, one guarded correction and four source conflicts. All176 flagged article views were actually read.
- Article42: remove the compilation's editorial `(06)` after `ستة`; retain the printed maximum `عشرة (10)`. Suspected edits to69 and210 were rejected because exact replay established that the existing text already had the correct readings. Superseded notes are preserved.
- All37 replaced/inserted bodies retain the earlier eight guarded word/punctuation corrections. Their paragraph/body boundaries have now also been actually read on19 sheets plus the corrected3bis source view; no paragraph text or division changed.
- Corrected two source locators:3bis now points to the actual Family insertion on PDF19 right, rather than an identical sentence in nationality article37 on PDF18;57bis ends on PDF21 left, excluding the next-page amendment header previously included by automatic alignment.
- New guards refuse missing/stale paragraph proof and a Family locator outside the independently read act's PDF pages. Old generated comparisons remain diagnostic evidence.
- The existing20-article sample and25-record legacy Family reconciliation are retained. The sample's selected bodies are unchanged by the unsampled42 correction; its explicit population update remains recorded. Source matching of sampled Ministry articles135/151/203 does not settle their JO conflicts.

Four unsettled official-edition conflicts:

| Article | Original JO | Ministry candidate |
|---|---|---|
|135|`عمدا أو عدوانا`|`عمدا وعدوانا`|
|151|`عاصب غيره`|`عاصب مع غيره`|
|202|`يتوقف على إنجاز الشرط`|`يتوقف تمامها على إنجاز الشرط`|
|203|`وغير محجوز عليه`|`وغير محجور عليه`|

Both readings remain explicit. In particular, the OR/AND conflict in135 is not silently resolved. French rectifications were discovered in JO31/1984 p801 and JO41/1984 p1018; official local acquisition/reading remains open. Law05-09's approval of Ordinance05-02 was read through the official French research source, but its local source pin is still missing. No amendment or commencement date was invented.

Details: [Family report](LAW-CORPUS-FAMILY.md). Research: `data/law/corpus/research/family-2026-10-28/`.

## Achieved: Civil Code

- Decoded all164 code pages, PDF8–171 of the pinned Ministry2007 compilation, with **zero undecided glyph assignments** and no fresh source/model download.
- Prepared **1,059 qualified source candidates**:1,049 main-number records and ten local management-contract articles. These include editorial repeal markers and are **not a verified current Civil delivery**. The existing typed Civil package remains45 identities /55 versions.
- Recovered the missed local first article `المادة الاولى`. Original JO89-01 images confirm a locally numbered1–10 chapter inserted between Civil570 and571; it must not overwrite general provisions1–10 or be assigned invented printed bis numbers. The typed identity representation still needs support for that scope.
- Corrected preparation boundaries so the black bold subsection before505 is excluded from504 and the signature after1003 remains paratext. Paragraph boundaries remain provisional:489 visibly has two body paragraphs but preparation currently merges them. Editorial labels and footnote callouts also need guarded removal.
- Preserved the diagnostic comparison of828 unannotated base-number candidates with the old1975 OCR:15 strict word agreements,5,209 word/digit/extra groups and1,276 punctuation groups. These groups are not proven legal errors; lack of an editorial label does not establish amendment history.
- Generated164 modern-edition body crops with a containment guard for every prepared body line, excluding historical footnotes at their actual separator geometry.

OCR state at pause:

| Reader | Completed source crops | Remaining |
|---|---:|---:|
|Tesseract Arabic best|164/164|0|
|PP-OCRv5 Arabic|119/164|45, PDF127–171|

The Paddle aggregate file records96 completed crops;23 more individually completed, hash-bound records survive through `civil-ministry-126-body`. Resume must reuse those records, not redo119 readings. The next missing crop is `civil-ministry-127-body`.

The first OCR process completed. The second stopped after a write failure; **no OCR or rendering worker remains running**. The batch runner was changed to isolate each native model worker in a separate subprocess, releasing its allocations between batches. That isolation change has not yet been exercised after the interruption and needs a controlled resume check.

Details: [Civil report](LAW-CORPUS-CIVIL.md). Research: `data/law/corpus/research/civil-2026-10-28/`. Main crop manifest: `tools/law-corpus/civil-ministry-manifest.json`.

## Validation

-165 Python tests pass after the Family paragraph/source-locator changes.
-JSON Schema validation:0 errors.
-The last completed full Release gate before pause preparation was for `61b293e`:692 .NET tests passed,0 build warnings/errors.
-The failed commit attempt did not complete a new Release gate. The pause checkpoint must receive the required post-commit gate before it is described as fully validated; the final handoff records that result. The logs are `.cache/resumed-full-build.log` and `.cache/resumed-full-tests.log`.
-No tests were removed, skipped or weakened. No app UI/behavior, legacy texts, claim mappings, prompts or safety rules changed.

Additional files outside the corpus/tool directories in this continuation: `docs/LAW-CORPUS-FAMILY.md`, `docs/LAW-CORPUS-CIVIL.md`, `docs/LAW-CORPUS.md`, this handoff, and the previously permitted Family sample report/selection/inspection metadata under `docs/law-corpus-checks/family-code/`.

## Exact next work after explicit resume

1. Read this handoff, confirm the pause commit/gate and storage headroom. Keep existing source pins, raw evidence and recorded decisions. Do not repeat the font investigation or completed Family image reviews.
2. Resume only the45 missing Civil supplementary crops, reusing119 completed records. The batch wrapper can recheck/resume earlier batches without repeating valid OCR. Command: `.\tools\law-corpus\.cache\venv\Scripts\python.exe -B tools/law-corpus/stream_supplementary_batches.py tools/law-corpus/civil-ministry-manifest.json --out tools/law-corpus/.cache/civil-ministry-paddle.json`. Check the isolated-worker change on resume; avoid concurrent full builds until its resource behavior is confirmed.
3. Build the modern Civil edition's exact body comparison against the two OCR streams. Retain the original1975 diagnostic separately. Project editorial labels/footnotes out with exact guards, resolve real word/digit/negation flags, and validate paragraphs/headings.
4. Finish Family's four source conflicts, paths, current amendment-outcome audit and external-reference handling. Pending references:82→Civil42,83→Civil43,86→Civil40,120→civil-status64. Do not start the civil-status special law merely to fill that reference; use honest reference-only handling as needed.
5. Complete Civil amendment/insertion/repeal reconciliation, qualified local numbering, typed current import, independent inventory/structure review, reproducible20-article sample and legacy reconciliation. Preserve existing historical pilot records and disclose unresolved dates/needed older regimes.
6. Complete the frozen bounded collections: Commercial37 targets, Labour90-11's29 targets and Labour90-04's one whole-chapter target. Commercial currently has two priority articles, not the entire37-target delivery. Keep Commercial/labour coverage explicitly partial and list chapters.
7. Run the required schema/typed-loader/full Release gates at each delivery. Comprehensive historical reconstruction remains deferred; **no special laws**.

The broad completion estimate given before pause was12–24 additional focused working hours for the agreed current-law scope, with substantial source-discrepancy uncertainty. It was not a commitment that the work could finish unattended during this pause.
