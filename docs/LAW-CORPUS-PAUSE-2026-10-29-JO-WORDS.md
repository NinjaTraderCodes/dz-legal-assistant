# Safe pause: corrected Civil JO crops and saved wording decisions

**PAUSED at the user's latest explicit request. No research/OCR/import resumes until the user resumes.** Branch `codex/law-corpus`; isolated workspace `redteam-output/law-corpus-worktree`. No app, catalogue, prompts or legacy library changes. No new push is approved or performed.

## Saved work

Earlier local checkpoints598b325,9dbf7ddand38ca401remain unpushed.38ca401passed694 .NET/279 Python tests, zero build warnings/errors and schema0. Latest approved/pushed checkpoint remains9fa6a0f. The actual new pause commit and its mandatory post-commit gate result are in `.cache/recovery-20261029-jo-word-pause/validation.json`.

- **All eight corrected07-05column crops were actually inspected.** Their line endings and cross-column/page continuations are intact; no neighbouring-column fragments appear. Both saved OCR readers were hash-replayed. `priority-07-05-crop-readings.json` records the new readings; the old pause-state file remains unchanged as history.
- `compare_civil_priority_amendments.py` reran the scoped original-JO comparisons. All59body spans use both readers and stay on the expected instruction pages.05-10:25bodies,19strict-word diagnostic agreements,15word flags.07-05:34bodies,21agreements,21flags. The older cropped07-05diagnostic had134flags; the reduction is an extraction improvement, not a legal-quality score. Both new outputs are `priority-a2005044-body-diagnostic-v2.json` and `priority-a2007031-body-diagnostic-v2.json`. They grant no checks or states.
- **All36remaining word flags received actual source readings.** `priority-jo-word-readings.json` records each original flag, body hash, source-image proof and finding. Some are OCR/spacing artifacts or following instruction/label text.469bis4expressly retains the independent negative `لا يجوز`; the OCR-side joined form and a missing Tesseract opening never create positive permission.
- **12source-word restorations are proposed in10bodies; ZERO are applied.** The immutable reading-only record says `mechanicalApplicationComplete:false` and `sourceWordCorrectionsApplied:0`. No new corrected body projection, typed Civil update or checked state is claimed.
- **34punctuation flags remain separately pending.** No period/colon was silently deleted, changed or certified from word agreement. Quote boundaries and printed punctuation require explicit treatment, or individual disclosed notes under the established delivery rule.

## Exact unapplied proposals

| Source | Article | Compilation → actual JO proposal |
|---|---|---|
|05-10|72bis|فقدة → فقده|
|05-10|101|عشرة → عشر; printed numeric10remains unchanged|
|05-10|124bis|لاسيما → لا سيما; الاضرار → الإضرار|
|05-10|140bis|مسؤول → مسؤولا|
|07-05|468|الا → إلا; ايجارا → إيجارا|
|07-05|484|يتعين عل المستأجر → يتعين على المستأجر|
|07-05|490|الاعفاء → الإعفاء|
|07-05|492|بحلاف → بخلاف|
|07-05|499|بالاقساط → بالأقساط|
|07-05|507|الا → إلا|

These are source-specific readings, not general spelling rules.484requires the recorded unique phrase: standalone`عل`also occurs inside valid`على`, so a global substring replacement is unsafe. Its guard correctly refused that ambiguity during recording. Retain the exact diagnostic flag separately from the contextual replacement.

## Restart after explicit resumption

1. Check branch/HEAD/clean worktree and the latest ignored validation metadata. Read this file before older ACTIVE/PAUSED sections. No local pending commit is approved for push. Do not redo the completed48Ministry layout pages,22instruction pages,8new crops or36word readings.
2. Implement and test a source/body/image/flag-bound applier for the12saved proposals, including484's unique context and the explicit negation/spacing exceptions. A preliminary, **incomplete and untested** prototype is preserved locally as `.cache/wip-apply-civil-amended-bodies.py`; it expects an older review schema and has no valid production invocation. Do not run it as-is or count its constants as applied data. The authoritative decisions are the committed reading-only JSON.
3. Complete source/current-body collation for the84-21loan clauses and explicit original provenance for489's retained second paragraph. The original84-21page was viewed again, but no new automatic/body verdict was saved. The25+34comparison covers only489's replacement first paragraph. Its second paragraph must not be overwritten or sourced from the2007placeholder.
4. Reconcile34punctuation/quotation flags individually; preserve source typography and disclose any residual mark uncertainty. Replay the101-target current instruction inventory (48full replacements,1partial,12insertions,40repeals), then explicit references and independent274identity/outcome inventory,20seeded source-image samples and typed partial import. Preserve existing pilots/history and honest date limits. Do not advertise a completed Civil partial before those gates.
5. Remaining Civil source order follows that partial delivery; then bounded Commercial/labour. French expansion, general editorial cleanup and older regimes remain deferred unless needed for an actual conflict/applicability issue. No special-law collection.

## Recovery and validation

PDFs, all crop/OCR records and old first-pass diagnostics remain local/ignored. The first-pass diagnostics are under `.cache/evidence/reviews/civil-priority-amendments-20261029/diagnostic-first-pass/`; newv2diagnostics do not overwrite them. The reading recorder `.cache/save_jo_word_pause.py` saves source hashes and checks unique correction contexts/proof hashes. `.cache/RESUME.md` starts with PAUSED; all older headers are historical.

The comparison tool was exercised on all59amended-body inputs. No tests were deleted, skipped or weakened; the existing279Python tests/schema and mandatory post-commit Release gate are recorded in recovery metadata. No manual app run for this research-only checkpoint; full automated app/web regressions remain included. Files outside corpus/tools changed: this handoff and Civil/restart/coverage reports.

**Family remains the complete232checked current-text delivery with disclosed temporal limits. Typed Civil remains48identities/55versions; its274identity checked partial is unfinished.** No task worker remains running. The task is paused, not complete.
