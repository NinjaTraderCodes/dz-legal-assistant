# Evidence core: implemented engineering slice

This work is on `codex/evidence-core-framework`, based on `c0421f8`, in separate review commits. It implements the audit, evidence protocol, harness pipeline, review bundle and training preparation from the agreed plan. **The whole deployment plan is not complete:** there is no qualified replacement reader, completed legal review, adapted model, independent 300-scenario release evaluation, or physical 8 GB result. The desktop and web still use their existing paths. No quality or speed improvement to the running app is claimed.

Start with the [checked-in review artifacts](evidence-core/README.md), particularly the [Arabic petition](evidence-core/lease-review/evaluation/petition.ar.txt), [rule review package](evidence-core/lease-review/review-package.json) and [scoring reconciliation](evidence-core/scoring-reconciliation.md). These are small, explicitly selected copies; model weights, environments, caches and full benchmark folders are excluded.

## What can be run now

Run from the repository root; choose a fresh output directory each time. Existing artifacts are never overwritten by the new commands.

```powershell
dotnet run -c Release --project src/DzLegal.Harness -- evidence demo --out bench-evidence-core/lease-review

dotnet run -c Release --project src/DzLegal.Harness -- evidence audit --run bench-facts-local --out bench-evidence-core/strict-local

dotnet run -c Release --project src/DzLegal.Harness -- evidence audit --predictions C:\path\to\gliner-benchmark\predictions.json --reader gliner2.5 --out bench-evidence-core/strict-gliner
```

The demo uses the explicitly fictitious `tests/drafting-cases-evidence/civil-lease.input.json`. It produces a source snapshot, full and reference-format evidence, schemas, authority candidates, an unreviewed rule specification and a legal-review package. Its `evaluation/` folder contains an Arabic petition, formal-check results, review state and stage/whole-harness measurements. The petition preserves the original dates and both accounts about notice. Its grounds contain `(...)`, and its facts/grounds retain unreviewed authorship marks. It cannot serve as approval to export an unreviewed draft.

**The petition was not generated from a reader's output on raw text.** `EvidenceDemo.Create` constructs answer-key-style factual annotations for its one named, new fictitious fixture; it does not load the 61-case answer key or run Gemma, Qwen or GLiNER. Those known annotations are supplied to the harness as evidence. The petition's actual factual wording is copied from the fixture's original raw input passages by `EvidencePipeline.Plan`, with the existing templates providing the rest. It therefore demonstrates preservation and review controls with known inputs, not successful automatic reading, independently reviewed gold, or a legally complete petition. Its open legal grounds remain `(...)`.

The demo is **not an AI extraction benchmark**. No network call or model inference occurs. Its measured pipeline time must not be compared to the 13-second GLiNER result. The bundled source edition is recorded as supplied, not asserted current; candidate rules and quotes remain unreviewed and do not enter approved grounds.

Read or score another reader's evidence:

```powershell
dotnet run -c Release --project src/DzLegal.Harness -- evidence snapshot --input tests/drafting-cases-evidence/civil-lease.input.json --out bench-evidence-core/input

dotnet run -c Release --project src/DzLegal.Harness -- evidence evaluate --input bench-evidence-core/lease-review/input.json --predictions bench-evidence-core/lease-review/reference-target.json --reference-format --rules bench-evidence-core/lease-review/rules.for-review.json --out bench-evidence-core/reference-evaluation

dotnet run -c Release --project src/DzLegal.Harness -- evidence score --input bench-evidence-core/lease-review/input.json --gold bench-evidence-core/lease-review/evidence.json --predictions bench-evidence-core/lease-review/reference-target.json --reference-format --exhaustive-gold --out bench-evidence-core/protocol-self-check
```

The last command checks protocol round-tripping against the demo's own annotations, not generalization or real reader accuracy. Exit codes: 0 success/no critical scored differences, 1 invalid evidence or critical scored differences, 2 invocation/data failure. `audit` reports failures without a release gate and returns 0 on successful report generation.

`evaluate` can accept a separate `--findings findings.json` from an assessor and a `--rules rules.json` module. These are trusted engineering/review inputs, not model self-certification. The default assessor supplies no supported findings. The output includes rule conditions and open points. Missing form/temporal checks and exceptions remain unknown; their legal implementations await review rather than guessed rules. `check` performs only structural/source/coverage validation and does not draft a petition or load legal modules.

## Contracts and safety boundaries

The implementation lives in `DzLegal.Drafting.Evidence`:

- `CaseSnapshot` owns immutable copies of **all** fields, original sentences and UTF-16 source offsets. Its fingerprint includes case identity, revision and all field values. `NormalizedEvidenceText` maps search-normalized text back to original spans, including diacritics, ligatures and Arabic digits. It never supplies normalized text to the draft.
- `CaseEvidence` separates relationships, events, participants, assertion source, polarity, dispute grouping, event-owned values, requests, listed documents, coverage and unresolved points. It remains a bounded first contract: additional individually identified third parties, multiple-contract resolution and more complex event identities need qualification before those scenarios are declared supported.
- `EvidenceReferences` is the compact generative wire format. Models select sentence IDs and numeric/date value IDs. The host resolves offsets, original values and revision identity. Candidate extraction is lexical; it does not decide which event owns a value. Native encoders may use the full span contract instead.
- `EvidenceValidator` checks references, vocabulary, calendar-valid dates, conservative monetary formats, participants, coverage and internal conflicts. It cannot prove semantic ownership. Invalid structures, unresolved coverage and stale snapshots cannot produce supported assessments.
- `IEvidenceReader` and `IEvidenceAssessor` are separate interfaces. Meaning cannot be certified by an extra model-output property: unknown and null properties/records are rejected. The default `UnqualifiedEvidenceAssessor` leaves meaning unknown. A refuted extraction does not prove the inverse event occurred.
- `StructuredEvidenceReader` uses opt-in `ModelRequest.OutputSchema` and `IStructuredModelProvider`, implemented by the Ollama transport. It requires an actual input-token counter covering the runtime's complete chat template and checks the budget against the provider's configured context/output limits. Oversized or mismatched requests fail before inference; no substring is silently used. A character-count estimate is not an acceptable production counter. No qualified tokenizer/chunking adapter has been installed here. Other providers must demonstrate schema support before implementing this capability.
- `AnalysisIdentity` covers snapshot, schema, reader digest/options, corpus, rules and assessment version. Harness identities include actual evidence/finding/rule/corpus and assembly fingerprints. Late results are labeled stale and the renderer refuses a stale identity.
- `EvidenceLogic` preserves supported/refuted/unknown/conflicting conditions. Reviewed authorities require nonempty identities, a known version, quote, review state and a SHA-256-shaped source hash. These are consistency checks of supplied review records, not automatic proof of legal correctness or a trusted signature.
- `CivilLeaseConditions` is a limited factual condition extractor. The demo's payment module remains a candidate specification. Form, applicable temporal regime and payment-satisfied exceptions are intentionally unresolved until their legal meaning is reviewed and implemented.
- `EvidenceAuthorityCandidates` unions catalogue seeds, explicit numbered references, raw-field BM25 hits and mandatory dependencies. The court section is not a filter. Full article/exception context is retained; missing dependencies and unresolved versions are recorded. Numbers in dates/money are not treated as explicit article references. This is discovery, never applicability.
- `EvidenceDraftPlan` and `EvidenceDraftRenderer` preserve complete original factual passages with neutral attribution, requests and document-list descriptions. Only reviewed, supported authority records can contribute quotes. They do not turn conflicting assertions into an adjudicated fact, claim document contents were inspected, or accept arbitrary model prose. The complete petition uses existing templates and existing formal checks.

Visible draft strings are in both Presentation `Strings.resx` files and read through `Loc`. Harness documents always instantiate the Arabic locale. No UI flows, export blockers, recovery behavior, corpus files, legal data or shipped prompts were changed.

## Evaluation and measured audit

**`tests/fact-cases` and `FactsEvaluator.cs` are unchanged from `c0421f8`.** The existing `facts` commands and `FactReader` parser are also preserved. `StrictFactsEvaluator` adds the separate `evidence audit` command and version `facts.strict/1`; the reported changes are a re-scoring of saved answers, not retraining or a rerun of the models.

Compared with `FactsEvaluator.Score`, the strict scorer:

- Makes wrong relationship codes, omitted decisive dates/amounts, missing requested relief, wrong source sentences, missing citations and rejected/unreadable output critical. The original scorer treated most of these as minor or did not check them.
- Counts an unknown/missing actor as wrong when the answer key requires plaintiff or defendant; the original actor check only caught a swap between the two explicit parties.
- Matches each event once by kind and prediction order and flags conflicting duplicate events. The original could reuse an event and select the duplicate closest to the gold date, amount and polarity.
- Keeps missing events in date and polarity denominators. The original percentages considered only events that were found, which could hide omissions.
- For external predictions, flags omitted explicit polarity rather than relying silently on the legacy parser's affirmed default.

Legacy annotations are not exhaustive: unannotated extra events/requests remain disclosed noncritical differences, unless another check independently finds a critical problem. No event/source labels have been added to the old gold to make the new scoring easier. Ambiguity under legacy annotations is a qualification problem, not proof that every extra event is false.

Replay of the saved local outputs and external GLiNER predictions:

| Reader | Previous critical cases | Strict audit critical cases |
|---|---:|---:|
| Gemma3:4b | 50/61 | 56/61 |
| Qwen3.5:2b | 60/61 | 61/61 |
| GLiNER2.5 adapter | 57/61 | 61/61 |

**Why 56/61 differs from the earlier 48/61 for Gemma:** the saved original summary has 34/44 development failures, 14/15 validation failures and 2/2 Darija robustness failures. Thus 48 is the formal-Arabic subtotal out of 59 cases, and the original all-partition total is 50/61. Reporting that subtotal as 48/61 mixed the numerator's scope with the denominator. The strict scorer changes development from 34 to 40, leaves validation at 14 and robustness at 2, and totals **40 + 14 + 2 = 56/61**. All 50 previously failing cases still fail. The six newly failing cases are:

| Case | Newly critical differences |
|---|---|
| `things-water` | One unreadable/rejected output line |
| `divorce-husband` | Missing marriage date, missing divorce source citation, two unreadable lines |
| `tatliq-abandonment` | Missing tatliq and damages requests, missing divorce source citation |
| `khul-2` | Missing khul request, one unreadable line |
| `custody-transfer` | One unreadable line |
| `none-commercial-company` | One unreadable line |

The [checked-in reconciliation](evidence-core/scoring-reconciliation.md) includes the partition counts and links to all three strict summaries. These strict failures can include output-contract failures as well as semantic errors; 56/61 must not be described as 56 independently adjudicated legal mistakes.

These are **rescored saved outputs**, not new model runs or a measured app regression. The old Markdown lacks exact per-case timing/termination metadata: audited seconds are null, not zero-duration claims. The source run, cases and current scorer/drafting assemblies are fingerprinted. Qwen's `loan-basic` raw answer uses `plaintiff` where a plaintiff-position code is required, an unsupported event code, and a date absent from the case; this is both output-contract failure and factual error. Old artifacts cannot establish whether every runtime/template option was honored.

For the richer contract, `EvidenceEvaluator` (`evidence.score/1`) matches assertion identity by kind, attribution and original source spans. Model-assigned IDs and event order may change. It checks participants, polarity, dispute grouping and event-owned source values, and handles unsupported additions explicitly when the author declares the gold exhaustive. Fine-grained repeated events sharing the same sentence/attribution require a sufficiently specific source/identity annotation; the current sentence-reference adapter must not be advertised as qualified for all such cases. Missing or contradictory predictions cannot be averaged away by entity scores.

`EvidenceQualification` enforces a minimum 300 independent source/scenario groups, no overlap with development/calibration, reviewed gold, zero observed critical-error cases, at least 90% complete coverage, reviewed rules and a physical-8-GB pass. It reports a zero-error one-sided binomial upper bound using independent completed groups, conditional on independence and trustworthy labels. Caller-supplied review/hardware metadata must be backed by real artifacts; unit-test metadata is not a qualification corpus. No real sealed 300-case pack was created or passed by this work.

## Annotation changes

**No existing gold labels were changed.** `tests/fact-cases`, existing drafting/grounds fixtures, and the legal data/catalogue remain unchanged from `c0421f8`. No expected event, actor, polarity, date, amount, request, article or partition in those sets was corrected or relaxed. The only new case is `tests/drafting-cases-evidence/civil-lease.input.json`; its fixture-specific evidence is constructed by `EvidenceDemo` and explicitly remains unreviewed. Small fictitious annotations in unit tests are test inputs, not new reviewed training or release cases.

## Training preparation and remaining gates

`evidence dataset --dataset <reviewed-cases.json> --out <fresh-folder>` validates fictitious labels, source spans, review status and family/source partition separation. Its cases have `caseId`, `scenarioFamily`, `sourceDecision` (including an explicit synthetic source ID where applicable), `partition` (`train`, `calibration`, `release`), `fictitious`, `labelReview`, `inputs` and `target` (full `CaseEvidence`, revision zero). Reviewed is an annotation attestation; it must not be set automatically just because models agree.

Only `train` cases enter `training.jsonl`. Calibration/release identities enter the manifest, **not their answers**. The training file has an integrity hash and both native and reference-format targets. The unreviewed demo is deliberately ineligible for training export. See `tools/evidence/README.md` for the offline, bounded LoRA preparation tool. It defaults to preflight, never rents hardware and does not download or upload models/data.

**Nothing was trained: no gradient updates, adapter, learned assessor or new checkpoint were produced.** Only the dependency-free preflight and its regression test were run. Dataset annotation/review, .NET export, Python preflight, scoring and the fictitious petition demo do not need a GPU. The implemented training runner requires an approved CUDA machine for `--train`: loading the selected local causal model, LoRA forward/backward passes and optimization, then saving the adapter. Its GPU memory fit and end-to-end package/checkpoint compatibility are still untested. Any future learned semantic assessor would need its own training plan; this work supplies no trained assessor. CPU output parity, quantization/packaging validation and the physical 8 GB release evaluation remain subsequent work, with final inference measured on CPU.

The following require additional evidence or external participation, and are not claimed complete:

1. An Algerian legal reviewer must adjudicate the candidate lease module, applicable authority versions/transitions, exceptions and representative expected outcomes. The bundle `review-package.json`, `rules.for-review.json`, original fixture and candidates is ready; no reviewer was contacted or paid.
2. Independently reviewed extraction labels and new scenario families must be collected. The existing 61 cases are regressions, not a sealed release set. No hundreds of automatically labeled variants have been presented as independent reviewed cases.
3. Select a supported local training checkpoint after the corrected runtime/schema comparison; arrange GPU access and approve an actual spending ceiling. US$100 remains a proposal. No training was executed, no checkpoint was installed for this training pilot, and no rented resource was started or spending authorized.
4. Qualify the adapted reader and any semantic assessor on held-out cases; calibrate selective acceptance and report deferred coverage. No learned verifier or calibrated acceptance model is being treated as proven by this implementation.
5. Implement and evaluate the reviewed legal conditions, time/version resolution and supported case-family boundaries. Then perform the actual physical 8 GB test and controlled end-to-end model comparison. The 10–20 second target remains unproven.
6. Integrate a passing slice only after all gates pass. Current app freshness behavior is preserved; the new all-input identity and stale-result guard are exercised in the harness, not wired into the failing old app analysis path.

No paid execution or reviewer-engagement approval is inferred from implementing or publishing these review commits. Only `codex/evidence-core-framework` is the publication target; no other branch is merged, rebased or force-pushed.

## Validation and file scope

New tests exercise source-offset round trips, unchanged raw text, all-field freshness, surrogate pairs, Arabic normalization maps, impossible dates, ambiguous amounts, overflow, attribution and conflicts, unknown propagation, rejected semantic self-certification, context overflow before inference, stale results, ID-based value selection, retrieval unions, duplicate-resistant scoring, training leakage/integrity, release gates and the complete fictitious petition path. Existing tests are retained unchanged apart from adding an opt-in schema transport test.

Outside the engine/harness portion of the brief, changes are limited to `Directory.Build.targets` (copy the new fictitious fixture into harness/test output), the two Presentation string resources, additional tests/fixture, these docs, and `tools/evidence/`. No app views, legal corpus/data/prompts or storage formats changed. Visual desktop inspection could not be performed because the native UI helper is unavailable; the harness demo and automated desktop/web regression tests are the available checks. The machine is a 16 GB development host, not an 8 GB certification target.

Final verification on 28 October 2026: `dotnet build -c Release` completed with zero warnings/errors; `dotnet test -c Release --no-build` passed all 855 tests, with none skipped. The dependency-free Python preflight regression passed. The final fictitious bundle is in `bench-evidence-core/final-lease-review/`; its reference-format self-check has zero critical differences. That self-check uses the fixture's own annotations and does not establish model accuracy. A command-level negative check also confirmed that unreviewed labels return exit code 2 without creating a training-export directory.

For publication, the bundle and summaries were regenerated from implementation commit `79a6ee6` and copied into [docs/evidence-core](evidence-core/README.md), with provenance and hashes. All implementation commits were checked from their own committed-source snapshots. One validation attempt exhausted temporary disk space; only generated validation copies were removed, and the affected build/tests passed on retry. Temporary source/build copies are excluded from Git; validation logs remain local.

### Every changed file outside the new folders

The following **eight pre-existing files** were modified:

- `Directory.Build.targets`
- `src/DzLegal.Drafting/IModelProvider.cs`
- `src/DzLegal.Harness/DzLegal.Harness.csproj`
- `src/DzLegal.Harness/Program.cs`
- `src/DzLegal.ModelProviders.Ollama/OllamaModelProvider.cs`
- `src/DzLegal.Presentation/Resources/Strings.resx`
- `src/DzLegal.Presentation/Resources/Strings.en.resx`
- `tests/DzLegal.Drafting.Tests/OllamaProviderTests.cs` (one additional test)

These **12 new files** were added directly inside pre-existing folders:

- `src/DzLegal.Harness/EvidenceCommands.cs`
- `src/DzLegal.Harness/EvidenceDataset.cs`
- `src/DzLegal.Harness/EvidenceDemo.cs`
- `src/DzLegal.Harness/EvidenceEvaluator.cs`
- `src/DzLegal.Harness/EvidenceQualification.cs`
- `src/DzLegal.Harness/StrictFactsEvaluator.cs`
- `tests/DzLegal.Drafting.Tests/EvidenceTests.cs`
- `tests/DzLegal.Drafting.Tests/EvidenceReferenceTests.cs`
- `tests/DzLegal.Drafting.Tests/EvidenceCandidateTests.cs`
- `tests/DzLegal.Law.Tests/EvidenceAuditTests.cs`
- `tests/DzLegal.Law.Tests/EvidenceEvaluatorTests.cs`
- `docs/EVIDENCE-CORE.md`

All other additions are contained in four new folders: `src/DzLegal.Drafting/Evidence/`, `tests/drafting-cases-evidence/`, `tools/evidence/`, and `docs/evidence-core/`. Existing unrelated untracked reports, archives and design files are excluded from these commits.
