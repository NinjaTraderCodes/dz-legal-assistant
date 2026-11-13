# Core logic: the research frame

Research code for `docs/research/core-logic/HANDBOOK.md`. It is **not** part of the app: the root solution does not build it, and it changes nothing under `src/`, `data/` or `tests/`.

| Folder | What it holds |
|---|---|
| `src/DzLegal.CoreLogic.Contracts` | The contract only, no logic: the shape of a rule record (`RuleRecord.cs`), the rule evaluator's interface and findings (`RuleEvaluation.cs`), the note reader's interface and answers (`NoteReading.cs`), the slot list (`Slots.cs`). `docs/research/core-logic/INTEGRATION-CONTRACT.md` explains each one. |
| `src/DzLegal.CoreLogic.Rules` | The one rule evaluator (`RuleEvaluator`, `docs/research/core-logic/specs/rule-evaluator.md`): conditions, period counting with its unsourced placeholders (`Periods`), the grade table, the worked-example, `--against` and boundary-case runners; and under `Checks/` the one copy of the record checks (`RecordChecks`, `MiniSchema`, `specs/record-checks.md`). |
| `src/DzLegal.CoreLogic.Rules.Tool` | The `rules` command (`check`, `eval`, `examples`). It reads `slots.json`, the schema, `data/law` and `rules/final` from the worktree at run time, so the built tool stays valid when they change. |
| `tests/DzLegal.CoreLogic.Contracts.Tests` | Loads the handbook's example records, the schema and `slots.json` through the contract, and holds the examples to every check of `RecordChecks`. |
| `tests/DzLegal.CoreLogic.Rules.Tests` | The evaluator's tests (truth table, every branch, the grade table, counting, determinism, both examples) and a planted error for each record check. |

Steps 1 to 6 add their projects here (one project per concern, each with its tests), and add them to `CoreLogic.sln`.

## Commands

Run both solutions after every commit, on an idle CPU (one test of the root solution fails when the CPU is busy):

```
dotnet build -c Release                                   # root solution: 0 warnings, 0 errors
dotnet test  -c Release                                   # root solution: all green
dotnet build research/core-logic/CoreLogic.sln -c Release # 0 warnings, 0 errors
dotnet test  research/core-logic/CoreLogic.sln -c Release # all green
```

The `rules` tool, once built (agents are given this path and never build):

```
research\core-logic\src\DzLegal.CoreLogic.Rules.Tool\bin\Release\net10.0\rules.exe check    [--quoted] [--store <articles.json>] [--modules <dir>] <record.json>...
research\core-logic\src\DzLegal.CoreLogic.Rules.Tool\bin\Release\net10.0\rules.exe eval     --records <record.json>... --case <case.json>
research\core-logic\src\DzLegal.CoreLogic.Rules.Tool\bin\Release\net10.0\rules.exe examples <record.json>... [--against <other.json> --idmap <unit.idmap.json>] [--boundary <cases.json>] [--json]
```

Exit code 0: everything passed; 1: a check failed or a case differed; 2: the command could not run. Every command takes `--root <worktree>` (default: the worktree the tool was built in). An id map is a JSON array of `{ "kind", "a", "b", "reconciled" }`; a record's column is taken from its stage (a final record uses the reconciled column).

## Rules of this folder

- The root settings apply (`Directory.Build.props` imports the root file): warnings are errors, analysis level `latest-recommended`. Fix the cause; never suppress a warning.
- A new package goes in `research/core-logic/Directory.Packages.props`, never in a project file and never in the root list. Check first that its licence allows commercial redistribution, and record it in the handbook.
- Projects here may **read** the app's projects (a project reference). They never change them.
- No folder may be named `bench…`: the repository's `.gitignore` hides such folders.
- Nothing the reader or the evaluator runs loads anything from the internet, and nothing reads a hidden test set. The one exception is a research tool, not part of what the app would ship: `scripts/oj_fetch.py`, the single door to `joradp.dz` for the Official Journal scan.
