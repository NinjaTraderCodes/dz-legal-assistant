# Alpha-03 second case freeze — owner-adjudicated text correction

**123 of 144 case texts / 219 sentence texts corrected; all annotations, numeric tokens, source identities and groups unchanged. No inference.** The owner accepted the first freeze's hashes, checks, coverage, splits and sampled labels, then authorised removing unnatural explanatory language that made binding too easy.

| Record | Content SHA-256 |
|---|---|
| First freeze, commit `41a9a86e89fb0de6465d36609133f858f69e5754` | `a3eb985d8f3bafe6bbe43c6afcd742ca0debc09e5b106921c4d4cb352d3bac68` |
| Second freeze, 7 November 2026 19:55:30 UTC | `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642` |

The old record is archived byte-for-byte in `tests/alpha-03-cases/revisions/freeze-v1.json`; current `freeze.json` records its predecessor and the owner's correction. `revisions/v2-text-correction.json` is the complete case/sentence before-and-after inventory, source-edit list and invariant record. The current hash covers 17 files, including history and the correction tool. The git commit and required post-commit build/tests are reported in the handoff.

## Correction and unchanged material

All 144 cases / 381 distinct sentences were reviewed. The changes remove answer-key emphasis (`صراحة`, `بمجموع صريح`, “independent of rent”), instructions not to include deposits/expenses, declarations that a number is not an amount, document/source exclusion explanations and missing-value announcements. Ordinary factual acts, document names, remedy nouns and time units remain. Natural restrictive request wording such as `فقط` remains where appropriate. No model answer influenced a change.

Changed: **85 fitting cases and 38 audit cases**; 21 case objects unchanged. The complete 123-case list is in `docs/ALPHA-03.md` and the correction inventory. Natural rewrites preserving attribution/scope rather than indiscriminate deletion: `a03-061`–`a03-064`, `a03-098`, `a03-109`, `a03-110`, `a03-112`, `a03-117`, `a03-119`, `a03-137`, `a03-140`. Their reasons and sentence-level text are recorded. No unresolved label ambiguity is retained.

An old-versus-current comparison verifies **144/144** records: every gold/dense label, value, date, actor, role, source ID/field/citation edge, document marker, family, split and fold is identical. Numeric tokens, including unlabelled competing figures/identifiers, remain in the same sentence and in the same order. No sentence is inserted, removed, split, merged or moved. Only input text, its exact sentence snapshot and derived hashes change. The family-plan file remains byte-identical. The label/citation projection hash is unchanged: `8676499900ce8e8b012a54bb68537d51f1ee9d8609361297e0d739b07142819c`.

Coverage remains **144 cases, 36 families, 21 claims, 96 fitting / 48 audit**, with the same three fitting folds and 27 polarity / 9 binding families. Dense labels remain **548 events / 368 requests**. Current text lengths: facts 93–761 characters (six cases at least 700); all read fields together 157–868. No time or model-memory cost was measured in this correction.

## Checks and interpretation

- Existing `facts check`: **144 cases; no problem found**.
- Unchanged static dense checker: **548 event labels, 368 request labels, 1,939 citation checks, zero problems**.
- Structural checker: original membership, split, fold, family-kind, vocabulary/calendar/value and duplicate/template checks pass.
- New correction checker: pinned original Git blobs, old freeze digest and archive verified; all annotations/source identities/numeric tokens unchanged; current authored source exactly equals the recorded edits; manifest changes limited to derived hashes.
- The owner-adjudicated correction was previewed and checked outside the repository before applying it. Final preview: `%TEMP%\dzlegal-alpha03-text-correction-ibo5mvac`. All material is the author's fictitious corpus, with no hidden/private-history access.

**Audit disputed recall is indicative only:** nine disputed targets, ten disputed event labels overall in 48 audit cases. A single target answer moves target recall by 11.1 percentage points. Keep the reference threshold and exact counts visible; do not infer precise population recall. No label, split or threshold was retrospectively changed from a model result.

## Verify

Requires a local checkout containing predecessor commit `41a9a86` and the existing .NET 10 tools; no Ollama/model calls:

```powershell
dotnet build tools/alpha-03/CaseCheck/CaseCheck.csproj -c Release
dotnet tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll tests/alpha-03-cases data/law
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts check --cases tests/alpha-03-cases --law-root data/law
python -B tools/alpha-03/revise_case_text.py --verify
python -B tools/alpha-03/freeze_cases.py --verify
```

Current file hashes: `cases.json` **`bd2ebc0c649c92375100f18f8eb533f03d0e4f3de102652c9084a1ab2f191204`**; `split-manifest.json` **`5942c710bb971b170e5149e243c36100bf1bebe99bb36c16b52542d46009edf5`**; correction inventory **`ba08974a24e98d5ad2a700f189736b253f06af177bc932719912ce4ef4bea21a`**. The original plan hash remains `d780cea2fe8cbb9ea2ef6a9be7160d374f2ade161219ef119fee45d7845de10b`. The combined hash algorithm is unchanged; current `freeze.json` supplies the complete file list.

Production readers/providers, prompts, law data and original fact cases are untouched. Tests added/changed/skipped/weakened: none. Candidate/fitting/audit/validation inference on the corpus: **0**. Original validation model checks: **0**. Future order/model fitting variables remain as authorised, unrun. **Stopped after the second freeze.**
