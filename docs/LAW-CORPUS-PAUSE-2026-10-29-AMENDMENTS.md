# Safe pause: Civil layout checkpoint and source instruction inventory

**PAUSED at the user's latest explicit request on29October2026. Do not resume research/OCR/import until the user resumes.** Work only on `codex/law-corpus`, in `redteam-output/law-corpus-worktree`. No app, catalogue, prompts or legacy library changes. No new push is authorized.

## Saved checkpoints

- Latest approved/pushed/remote-confirmed checkpoint remains **9fa6a0fff82ce4507e9d577ee0e89cc08f54ef96**, including parent95914fb. Family392c0fb was also approved/pushed earlier.
- **598b325bb4085538a1240c7bc482c98e07289551**: earlier word-boundary/pause checkpoint. Its completed gate passed694 .NET/262 Python, zero build warnings/errors. It remains local, not approved for push.
- **9dbf7dda6eae8d9a1fb35e9e5c8582aeec159213**: resumed48-page Civil layout review. Full post-commit gate passed694 .NET/273 Python, zero warnings/errors; schema0. It remains local, not approved for push.
- The following local pause checkpoint saves the instruction inventory and completed recrop acquisition. Its actual commit hash and post-commit gate result are in `tools/law-corpus/.cache/recovery-20261029-amendment-pause/validation.json`; do not predict a hash or infer push approval from this file.

## What progressed after the previous pause

**All48 full Ministry pages covering274 selected identities were actually read for layout.** The guarded research projection has234 bodies and40 compilation repeal markers.101 printed status labels and101 footnote callouts are separated into metadata; all14 parenthesized legal quantities remain.95's following cause heading is excluded,140bis1's final compensation line is restored from the native fragment merged with the divider, and489 has its two printed paragraphs.365/378/544 continuations were read across both pages. Raw candidates/audits remain immutable. See `priority-layout-readings.json`, `priority-layout-projection.json` and `civil_priority_layout.py`. These records do not certify the complete amendment/body/date chain.

**22 further complete original JO pages were actually read for instructions**, covering83-01(PDF5–8),88-14(7–9),89-01(3–4),05-10(17–25) and07-05(3–6). The earlier84-21/PDF8 reading is reused and hash-replayed. `priority-amendment-inventory.json` independently lists every selected body operation and separates necessary legal heading operations. `civil_priority_amendments.py` reconciles the printed targets/actions with the compilation's source annotations:

| Publication | Selected targets | Current operation partition |
|---|---:|---|
|84-21|2|455and456 full replacements, instructions22/23|
|05-10|28|21full replacements,4insertions,3repeals(96,115,135)|
|07-05|71|25full replacements,1partial489,8insertions,37repeals|
|Total|101|48full replacements,1partial,12insertions,40repeals|

The other173 selected identities have no explicit body operation in these scoped amendment findings. This is not a claim that unindexed/indirect legal effects cannot exist.83-01's co-ownership operations,88-14's evidence/company/property-taking targets and89-01's separately numbered management-contract insertion do not directly replace/insert/repeal a body among these274. Their actual instruction pages and exclusion reasons are recorded. No broader special-law collection started.

**489 must preserve its original second paragraph.** Instruction07-05/art6 lists it among amendments, but the quotation replaces only the first paragraph and explicitly retains the remainder. The new inventory/tests reject a whole-article replacement or a missing second paragraph.507bis/507bis1 carry express earlier-lease and public-institution exceptions; no historical applicability or missing older body is guessed. The05-10/07-05 closing clauses merely order publication; dates remain separate.84-21's closing provision is still unestablished; the oldPDF45annex is not commencement authority.

## OCR crop defect and exactly what is unfinished

The first original-JO body diagnostic was run for25amended05-10 bodies and34amended07-05 bodies. It is **diagnostic only**, with no discrepancy decisions or new checked states. Its local files are preserved under `tools/law-corpus/.cache/evidence/reviews/civil-priority-amendments-20261029/diagnostic-first-pass/`. The generator is retained as `.cache/compare_priority_original_amendments.py`; it still uses the old manifest and must not silently overwrite these first-pass records.

Actual inspection of the old07-05/PDF4right crop proved that it clips line-ending letters. The old left crops also include fragments from the right column. Native-line geometry places body text at left-column x≤266.11points and right-column x≥292.403on a595point page; the old boundary at0.5/0.505cuts/includes the right text. New `civil-priority-07-05-manifest.json` uses the intervening gutter atx279.65(0.47), excludes the shared page3large heading above the article body, and retains all old sources/crops/manifests.

**All eight new Tesseract crops and eight PP-OCR outputs completed and their hashes replayed. ZERO new crop images have been inspected. The34-body comparison has not been rerun with them.** `priority-07-05-recrop-state.json` records the exact manifest/crop/evidence/secondary hashes, the inspected old crop and the remaining steps. Generation/OCR is never an image verdict. Do not treat the old134word flags as134legal errors.

Potential wording/orthography differences remain uncurated, including72bis,101,124bis,140bis,484and492. Several old2007flags are crop artifacts. Do not apply proposed letter changes or certify body outcomes until each is bound to an actual authoritative image reading. No original-JO letter correction was applied during this continuation.

## Exact restart steps after explicit resumption

1. Read this file and the latest Civil report; check branch/HEAD/clean worktree and ignored validation records. Local598b325/9dbf7dd/this pause commit are not approved for push. Do not repeat the finished48layout/22instruction page readings or earlier47/63/28word findings.
2. Inspect the8new07-05crops against their already-read complete original pages. Replay the saved Tesseract/PP-OCR records; no full312-crop or164-page rerun. Update the body-diagnostic generator's07-05manifest argument to the new scoped manifest and save the new diagnostic separately.05-10continues using its existing original-JO evidence. Audit actual quoted-body bounds, page attribution and extra instruction/placeholder tokens before counting agreements.
3. Resolve every remaining word/digit/negation conflict from the authoritative page, preserving separate Ministry witnesses and original diagnostics. Apply guarded current outcomes from the101-target inventory, with489's second paragraph retained and all40source-read repeals accounted for. Handle84-21's two clauses and unknown commencement honestly. Never use a repeal marker as a missing operative/former body.
4. Check explicit references and independent274current identity/outcome inventory,20seeded actual source-image samples, then typed partial import. Preserve existing Civil pilot/history and clear temporal limits. The typed partial must list the seven covered chapters and remain explicitly partial; current text is not a dated historical archive.
5. Only after that package is delivered: remaining Civil source order, then bounded Commercial37/Labour90-11's29/Labour90-04's article19wholechapter. French expansion, general editorial heading cleanup and older regimes remain deferred unless a conflict/applicability issue needs them. No special-law collection.

## Validation and recovery

The six new inventory regressions bring Python to**279**. All279 tests and JSON schema0 pass before the pause commit; its mandatory full post-commit Release gate is saved in the ignored recovery record. New layout tests were11; no test was removed, skipped or weakened. Tests use fictitious fixtures.

The preserved9dbf7dd gate logs/metadata are in `.cache/recovery-20261029-civil-layout/`;598b325's logs/metadata are in `.cache/recovery-20261029-priority-pause/`. Latest pause metadata is `.cache/recovery-20261029-amendment-pause/validation.json`; `.cache/RESUME.md` starts with PAUSED. Older ACTIVE/PAUSED sections are historical. PDFs, raw OCR, crop images, rejected locators and first-pass diagnostics remain local/ignored. The two OCR process sessions finished normally and were collected; no task OCR/review worker remains running. Do not kill unrelated apps or shared idle.NET servers.

**Typed Civil remains48identities/55versions; the274identity checked partial is NOT delivered.** Family remains the complete232checked current-text delivery, with its disclosed historical/geographic date limits. No app, catalogue, prompts, legacy data or production engine/loader behavior changed. No manual app run for this research-only continuation; automated app/web tests remain included in every full gate. Files outside corpus/tools in this pause checkpoint: this pause record and the Civil/restart/coverage reports.
