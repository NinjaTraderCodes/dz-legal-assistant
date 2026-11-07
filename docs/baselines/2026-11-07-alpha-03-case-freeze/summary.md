# Alpha-03: authored-case freeze, no candidate runs

**144 fictitious cases, 36 families, all 21 claims; checked and frozen before candidate inference or new reader/prompt code.** Owner-approved realism substitutions are explicit. This is authored development/audit material, not an independent evaluation set.

Combined content SHA-256:

`a3eb985d8f3bafe6bbe43c6afcd742ca0debc09e5b106921c4d4cb352d3bac68`

Freeze time: **2026-11-07 19:24:14 UTC**. Source base: `05c0f398b0de0913f07508d4cf51216bd3bd166a`. The resulting git commit and required post-commit build/tests are reported in the handoff. This freezes data, not an Alpha-03 reader for hidden v2.

## Split and coverage

| Group | Families | Cases | Polarity families / cases | Binding families / cases | Claims |
|---|---:|---:|---:|---:|---:|
| Fitting | 24 | 96 | 18 / 72 | 6 / 24 | 13 |
| Internal audit | 12 | 48 | 9 / 36 | 3 / 12 | 8 |
| Each fitting fold | 8 | 32 | 6 / 24 | 2 / 8 | varies |

The family plan was assigned and hashed before case prose; pre-authoring hash **`d780cea2fe8cbb9ea2ef6a9be7160d374f2ade161219ef119fee45d7845de10b`**. Assignment uses a published seed/hash order, fixed capacities and whole-claim groups. The checker reconstructs the allocation independently. All related scenarios, role variants and substitutions stay on one side and in one fitting fold. No case text, inference result, model identity or order-probe outcome chooses a split. This claim-disjoint design is stronger than random row/family splitting and creates heterogeneous folds; generalisation claims must respect that limit.

Every polarity family contains four target statuses: **27 affirmed, 27 denied, 27 disputed, 27 absent** overall; 18 each in fitting, 9 each in audit. The nine binding families (`f08`, `f12`, `f16`, `f18`, `f22`, `f25`, `f28`, `f30`, `f35`) substitute realistic value/request contrasts for denial/absence of their scenario's core act. Reasons are in both plan and split manifest. Divorce and khul keep marriage affirmed in all eight cases. Dismissal contains the settled reason-versus-act distinction, including one realistic disputed act.

Dense coverage: **548 event labels** (340 affirmed, 60 denied, 28 disputed, 120 absent); **368 request labels**, 214 present and 154 absent; **78 competing-value notes** and **32 document-only event annotations**. Non-target facts retain their natural distribution; the full set is not class-balanced merely because its polarity targets are. Each event/request/value has its own source where present; null values remain unspecified, not fabricated. The current evaluator's projection and supplemental dense key are kept distinct.

Notes span **93–1,007 characters**, with 12 cases at least 700 characters; all read fields together span **186–1,126 characters**. These are varied synthetic notes, not real multi-page-file qualification. They cover negative propositions, irrelevant negation, cross-sentence contests, multiple events per sentence, source-only document assertions, roles, identifiers and competing rates/totals. No test is designed around a model's current answer or the order-probe findings.

## Checks and review

- Existing harness `facts check`: **144 cases; no problem found**.
- New static checker, using unchanged `CaseText`, `FactValues`, `FactVocabulary`, catalogue and `FactCaseCheck`: **548 event labels, 368 request labels, 1,939 citation checks, zero problems**. It verifies exact sentence snapshots, per-item/value evidence, actor/role mapping, document-source markers and dense-to-existing-key projection.
- Structural/hash checker: exact 36 × 4 membership, 96/48 separation, three 32-case folds, both family kinds in every group, all 21 claims, no duplicate inputs, no claim-group leakage and no substantial numeric-substitution sentence crossing the fitting/audit boundary. Calendar dates and normalized amounts are checked as well.
- Author semantic review: **144/144**, common facts plus all contrasts, recorded by family in `review.md`. Several actor/source/scope issues were corrected before freeze, without predictions. This is one author's review, not independent legal annotation; no unresolved contradictory training scenario was retained.
- The post-freeze builder guard was exercised: it refused regeneration, and the subsequent hash verification remained unchanged. No frozen case/source/split was edited after that point.

The new checker built in Release with **0 warnings/errors** before the freeze. No source, parser, prompt, catalogue or original case was altered. Original validation model checks remain **0**; hidden/evaluation access remains **none**. The internal audit is author-known and never described as hidden.

## Verify and next-phase limits

```powershell
dotnet build tools/alpha-03/CaseCheck/CaseCheck.csproj -c Release
dotnet tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll tests/alpha-03-cases data/law
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts check --cases tests/alpha-03-cases --law-root data/law
python -B tools/alpha-03/freeze_cases.py --verify
```

`freeze.json` records 14 payload-file hashes and the exact combined algorithm: concatenate sorted repository-relative UTF-8 paths plus LF and each file's raw bytes, then SHA-256. It excludes itself to avoid self-reference. Byte-level hashes: `cases.json` **`867b6ac35c4502158e7a575cfd4abb4804cdfb364c636255a0d18019980a1516`**; `split-manifest.json` **`40f51b59fd59f0207b7273425d5647e86a6e0d9644acdd472c0b4652a0438ecd`**. The manifest also records each canonical case hash. LF rules preserve these bytes on Windows checkout. The sole root formatting change pins `tools/alpha-03/.gitattributes` itself to LF; it leaves the frozen payload hash unchanged.

After owner authorisation, carry two variables into the same grouped **fitting-only** comparisons: fixed-order blind chain versus averaged forward/reverse scores per question, with the full cost of each; every candidate on both gemma3:4b and gemma4:e4b with thinking off, with time/memory side by side. The exact averaging/selection protocol must be fixed before those runs. Neither model becomes the runtime model from this development evidence. Report polarity and binding families separately, preserving all Report 1 gates.

No fitting, calibration, ablation, audit inference, validation inference or candidate code followed the freeze. The next stage must freeze its candidate/parameters before one aggregate audit inspection; no repairs from audit cases. Any later annotation correction needs owner adjudication and a disclosed new hash. **Stopped after the case freeze.**
