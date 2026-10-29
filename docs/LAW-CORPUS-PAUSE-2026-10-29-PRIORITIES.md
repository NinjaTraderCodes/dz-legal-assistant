# Safe pause — current Family delivery and priority Civil chapters

**Paused at the user's explicit request on29October2026. Do not resume research/OCR/import work until the user resumes.** Branch:`codex/law-corpus`; isolated workspace:`redteam-output/law-corpus-worktree`. No app, catalogue, prompts or legacy law changes are authorized by this checkpoint. Push only with the user's specific approval.

## Committed and pushed with approval

- **392c0fb4a4af8c26b7f2c3cd7659a3dd21379313:** complete checked **current Family text**,232 records comprising227 bodies and five repeal markers. The newly acquired Arabic JO31/1984 PDF12–13 explicitly resolves135/151/202/203. Its17 targets were accounted for;167's canonical noun order now follows the rectification. Original print/conflict evidence remains preserved. The source-read location-dependent commencement rule is documented; all version bounds remain null and historical/date lookup remains fail-closed. This is a current-text delivery, not a complete dated historical archive.
- **95914fbacd07722de26312cf90fbfdfc175d4f00**, then **9fa6a0fff82ce4507e9d577ee0e89cc08f54ef96:** Civil priority inventory, source readings and guarded research projections. Both were explicitly approved and pushed; the latest remote hash was verified as9fa6a0f. The latter passed694 .NET/257 Python tests and0 Release warnings/errors. Family's checkpoint passed694 .NET/241 Python tests.

## Additional work saved in this local pause checkpoint

The Civil word-boundary follow-up accounts for172 accepted OCR-join events:144 have an exact separate-word witness; the other**28 cases were actually read on five source sheets**, with enlarged64/561/568 strips. **Nineteen broken words are restored; nine valid word boundaries are retained.** Source561/568 visibly print`خطإ`; no preferred-spelling or letter correction was made. The source noun in64 retains the print's`الايجاب` spelling.

`priority-word-boundary-readings.json` binds every actual reading and all earlier input hashes. `civil_boundary_projection.py` independently rebuilds the previous projections, replays the172-event partition and produces`priority-word-boundary-projection.json` for all274 selected identities. It does not import typed Civil data or grant full-article certification. The two source nouns64/456 are handled in a closed, image/body/token-bound registry; the general independent-negation guard remains strict. Five new regressions bring Python to**262**. The262 tests and JSON schema validation pass before the pause commit; the commit receives the mandatory full post-commit .NET gate.

Research spacing repairs now total**111**:58 earlier,13 sale,21 remaining priority,19 word-boundary restorations. Article360's trailing source heading is separately excluded from its derived body and preserved as metadata. The original80-reading350–450 file stays immutable:33 unselected findings, including333's difficult numeric/context proposals, remain unapplied/deferred. No raw native/audit source is overwritten.

The **typed Civil package is unchanged:**48 identities (45 with text plus three reference-only),55 versions (seven checked,32 partly-checked,16 unsettled). **The274-identity checked partial package is not yet delivered.** Raw PDFs, OCR, manifests, rendered views and rejected/provisional locators remain cached locally. No OCR, Tesseract or review-rendering job remains running; completed command sessions were collected. Shared .NET build servers and unrelated apps must not be killed.

## Exact next work after resumption

1. Read this file and the latest sections of`LAW-CORPUS-CIVIL.md` and`LAW-CORPUS-FAMILY.md`; check branch, local HEAD, remote and the ignored recovery record. The pause checkpoint is local only and is not approved for push. Do not repeat the completed28/63/47 source readings.
2. Start from`priority-word-boundary-projection.json`. Guard the removal of printed status annotations and footnote callouts into metadata; retain actual legal digits, list numbers and negations. Complete Arabic paragraph/body-boundary QA, especially the known two-paragraph489 issue. Broad editorial heading cleanup and French paragraph expansion remain postponed.
3. Audit and apply current amendments for the selected chapters, including84-21,88-14,05-10,07-05 and any other actually cited relevant source. Arabic JO72/1984 is already cached/pinned:Law84-21 dated24December1984,published31December1984; PDF8/printed2544 instructions22→455 and23→456. Both were actually read; OCR456 misses a visible line, so strict agreement was never claimed. PDF45 is an annex table, not a commencement clause. Do not start a separate Finance Act/special-law collection.
4. Verify current repeal targets from the amending instructions. The four later priority chapters contain37 excluded current repeal markers; also account for any repeals in the other priority chapters. No empty marker may stand in for a missing operative or former body.
5. Check explicit references, independent scoped identity/amendment inventory, all published paragraph projections and20 seeded source-image samples. Import the checked **partial** current package while preserving the old pilots/history and honest temporal limits; list the seven covered chapters and pass schema/typed loader/full Release gates. No fabricated nationwide date or unavailable reference body.
6. Only after that partial delivery, continue remaining Civil in source order. Then build bounded Commercial37, Labour90-11's29 and90-04's one whole-chapter article19. Keep those collections explicitly partial. No special-law collection; older regimes only when a conflict/applicability issue actually requires them.

## Frozen Civil priority scope

| Chapter | Identities | Main numbering |
|---|---:|---|
|General contract|71|54–123 plus72bis; separate123bis/unilateral chapter excluded|
|Sale|62|351–412|
|Lease|79|467–537, including insertions/repeal markers|
|Consumption loan|9|450–458|
|Loan for use|11|538–548|
|Works|22|549–570; locally numbered management-contract insertion excluded|
|Liability|20|124–140bis1, including the middle vicarious-liability section|

Total274. Fifteen boundary/successor page images were actually read. See`priority-chapter-inventory.json` for every target and proof. Boundary reading alone never grants body certification.

## Recovery and validation

The actual pause-commit hash, post-commit gate results and local-only status are stored in`tools/law-corpus/.cache/recovery-20261029-priority-pause/validation.json` and reported to the user. This file intentionally identifies the earlier approved remote hash rather than predicting its own commit hash. Latest task status is also at the top of`tools/law-corpus/.cache/RESUME.md`; older ACTIVE/PAUSED headers are historical.

No tests were removed, skipped or weakened. New tests use fictitious fixtures. No manual app run was performed for these data/tool-only checkpoints; automated app/web regression remains part of each full gate. Files changed outside corpus/tools in this pause checkpoint: this pause record, Civil/restart/coverage reports. The Family delivery earlier also updated its report, existing sample metadata/report and the authorized corpus loader test fixture; no production application/engine behavior changed.
