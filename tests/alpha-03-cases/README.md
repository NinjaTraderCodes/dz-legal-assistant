# Alpha-03 fictitious development corpus

**Codex-authored, not independent or hidden.** This version contains 144 Arabic lawyer-note cases in 36 scenario families, covering all 21 catalogue claims. It was written from Report 1's taxonomy and the owner's item-7 and realism decisions. No inference on this corpus or new candidate reader/prompt/calibration preceded the freeze. Model/order probe findings were not used to choose the case texts or keys. The earlier authorised probes used only the original development cases and are recorded separately in `docs/ALPHA-03.md`.

## Contents and separation

- `cases.json`: complete static cases, the existing evaluator key and dense supplemental annotations. Only `inputs` plus the confirmed `claim` may enter a reader. Names, family/split/type/contrast metadata and both keys are scoring material.
- `family-plan.json`: family themes, polarity/binding kinds, targets, substitution reasons and preassigned groups. Its pre-authoring SHA-256 is `d780cea2fe8cbb9ea2ef6a9be7160d374f2ade161219ef119fee45d7845de10b`.
- `split-manifest.json`: assignments, plan/case-file fingerprints and a canonical SHA-256 per case. Canonical case encoding is UTF-8 JSON with sorted keys, no ASCII escaping and separators `,` / `:` without spaces.
- `source/authoring.py`: hand-authored Arabic source and annotations. Its helpers assemble fixtures; they never classify Arabic or call a model.
- `annotation-rules.md`, `review.md`, `static-check.txt`: rules, semantic review and static-check evidence.
- `freeze.json`: payload hashes and the combined content hash. It excludes itself to avoid circular hashing. Included file paths and the exact hash algorithm are recorded there; LF is pinned by `.gitattributes` for portable verification.

All files here stay separate from `tests/fact-cases`. Fitting cases use partition `development`; audit uses `alpha03-audit`, which the existing harness's default partition list does not select. Any future command must also select the manifest group deliberately. This is a guard against accidental use, not secrecy: the author has read the audit labels.

## Frozen grouping

| Group | Families | Cases | Polarity families / cases | Binding families / cases |
|---|---:|---:|---:|---:|
| Fitting | 24 | 96 | 18 / 72 | 6 / 24 |
| Internal audit | 12 | 48 | 9 / 36 | 3 / 12 |
| Each of three fitting folds | 8 | 32 | 6 / 24 | 2 / 8 |

Every claim's related scenarios, paraphrases, substitutions and role variations stay together, both between fitting/audit and between fitting folds. Thus no claim type crosses those groups, a stronger separation than a random family/row split. This also makes folds heterogeneous; it is not a population guarantee. The seeded deterministic allocation and whole-group capacities are recorded in the plan and independently reconstructed by the checker. Divorce and khul are reserved on opposite sides to retain binding examples in both; each fold has both family kinds.

Each polarity family has four target labels, one per status: 27 of each across the corpus, 18 of each on fitting and 9 of each on audit. Binding families do not manufacture denied/absent core facts. Their other event labels still count in full dense metrics. All asked events and requests, not just targets, are annotated; they have a different natural class distribution.

## Use after the owner authorises fitting

Select architecture/components/calibration only by the three grouped fitting folds. Fit parameters on the other two folds and score the held-out fold; aggregate out-of-fold results. Keep the grid small and global as Report 1 requires. Report whole cases, each error type, omissions, false affirmations, denied/disputed recall and evidence/value completeness, with separate polarity-family and binding-family results. Never select a design from the internal audit's case-level errors.

The owner requires two fitting variables: one fixed-order blind chain versus averaged forward/reverse question scores, with the actual cost of each; and every candidate on both `gemma3:4b` and `gemma4:e4b` with thinking off, side by side with time and memory. These are future fitting requirements, not findings from this corpus and not a runtime-model choice. Freeze the precise score-averaging/parameter-selection protocol before those runs. All timed runs require mains, Best performance and quiet conditions. No such run is part of this freeze.

Freeze the candidate and parameters before one aggregate audit inspection. Apply Report 1's audit stop rules and report both family kinds separately. A failed gate means stop/report; no per-case repair loop. If later authorised development uses audit errors, retire it as generalisation evidence. The owner's independent hidden v2 remains separate and was never accessed. The original 40 development cases remain a compatibility gate; original robustness and capped validation checks retain their existing roles.

## Check and freeze

From the repository root, without Ollama:

```powershell
dotnet build tools/alpha-03/CaseCheck/CaseCheck.csproj -c Release
dotnet tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll tests/alpha-03-cases data/law
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts check --cases tests/alpha-03-cases --law-root data/law
python -B tools/alpha-03/freeze_cases.py --verify
```

Before the first freeze only, `build_cases.py` assembles the fixed authoring source and `freeze_cases.py --freeze` runs the static checker and writes the one-way freeze record. Both refuse overwriting a frozen corpus. After freezing, **no case, source, family assignment or split is edited** except an owner-adjudicated annotation correction, explicitly disclosed with a new version/content hash. Do not regenerate cases from observed predictions. Review limitations are recorded, not disguised as independent legal annotation.
