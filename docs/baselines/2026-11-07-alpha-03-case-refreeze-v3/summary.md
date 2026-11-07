# Alpha-03 owner-adjudicated decisive flags: v3 freeze

**144 cases re-frozen; only decisive flags change.** Old content hash `86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642`; new `9fc0eb6404903071aeaee4721f228a621cc71b772ea4afa45fb0230256fda3a5`. Case-file SHA-256 `415fc5c2f0bcec7dbb41b0e12df7dcd06ccd3d03fb3d6feb8c2dee907275ab67`; split-manifest SHA-256 `1a9b132b481c7e3719f5f66b8f955c5aee56a2c16fc0e76b945520ef033e762f`. Plan hash unchanged.

Authority: the owner corrected the blanket default to `docs/HIDDEN-SET.md`: decisive when a reader missing the fact would get the case wrong. Targets stay decisive; distinct foundations/material grounds, remedy prerequisites and procedural/prior-adjudication facts remain decisive. Redundant negative alternatives and ancillary history are not. The complete rule and every event/reason are in `tests/alpha-03-cases/revisions/v3-decisiveness.json`; every changed gold event is tabulated in `docs/ALPHA-03.md` and the corpus review.

548 explicit dense flags: 422 true, 126 false. Of 428 non-absent gold events, 33 change true to false, 395 stay true. All 108 target decisions, including 27 absent targets, stay true. Non-target absent rows have false flags without changing their dense status or forbidden checks. Dense denied-versus-absent errors remain errors. All other fields, texts, labels, dates, values, sources, requests and groups are identical to v2, proved against predecessor commit `0511de639194298da9b94036ffc3ffb814eba226`. The archived v2 freeze preserves its original 17-file record; v3 adds the archive, inventory and bounded correction verifier to the 20-file payload.

```powershell
python -B tools/alpha-03/revise_decisiveness.py --prepare # bounded v2-only correction, already completed
dotnet build tools/alpha-03/CaseCheck/CaseCheck.csproj -c Release
dotnet tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll tests/alpha-03-cases data/law
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts check --cases tests/alpha-03-cases --law-root data/law
python -B tools/alpha-03/freeze_cases.py --check
python -B tools/alpha-03/freeze_cases.py --freeze # one-way, already completed; do not repeat
python -B tools/alpha-03/freeze_cases.py --verify
```

Results: checker build zero warnings/errors; 144 cases, 548 dense events, 368 requests, 1,939 citation checks, zero problems; existing harness no problems. V2 historical correction proof and v3 flags-only proof pass. Structure retains 36 families, all 21 claims, fitting 96/audit 48, 27 polarity/9 binding families, three fitting folds and whole-claim separation. The audit's sparse disputed gate remains indicative only. Required post-commit Release build/full-test results are reported in the handoff/checkpoint.

The 25 interrupted v2 reference calls are excluded from all comparisons and are not rescored or reused. No v3 inference, candidate/audit inference or original validation check preceded freeze. The owner authorised continued references from case 1 for both models, under the new ten-case reload protocol, then fitting/candidate freeze/locked audit as previously agreed. No hidden/evaluation access. No unit test changed, deleted, skipped or weakened; the static checker adds explicit-flag/target/projection assertions.
