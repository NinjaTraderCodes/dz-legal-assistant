# Core logic: the research frame

Research code for `docs/research/core-logic/HANDBOOK.md`. It is **not** part of the app: the root solution does not build it, and it changes nothing under `src/`, `data/` or `tests/`.

| Folder | What it holds |
|---|---|
| `src/DzLegal.CoreLogic.Contracts` | The contract only, no logic: the shape of a rule record (`RuleRecord.cs`), the rule evaluator's interface and findings (`RuleEvaluation.cs`), the note reader's interface and answers (`NoteReading.cs`), the slot list (`Slots.cs`). `docs/research/core-logic/INTEGRATION-CONTRACT.md` explains each one. |
| `tests/DzLegal.CoreLogic.Contracts.Tests` | Loads the handbook's example records, the schema and `slots.json` through the contract. They are the same checks every real record must pass. |

Steps 1 to 6 add their projects here (one project per concern, each with its tests), and add them to `CoreLogic.sln`.

## Commands

Run both solutions after every commit, on an idle CPU (one test of the root solution fails when the CPU is busy):

```
dotnet build -c Release                                   # root solution: 0 warnings, 0 errors
dotnet test  -c Release                                   # root solution: all green
dotnet build research/core-logic/CoreLogic.sln -c Release # 0 warnings, 0 errors
dotnet test  research/core-logic/CoreLogic.sln -c Release # all green
```

## Rules of this folder

- The root settings apply (`Directory.Build.props` imports the root file): warnings are errors, analysis level `latest-recommended`. Fix the cause; never suppress a warning.
- A new package goes in `research/core-logic/Directory.Packages.props`, never in a project file and never in the root list. Check first that its licence allows commercial redistribution, and record it in the handbook.
- Projects here may **read** the app's projects (a project reference). They never change them.
- No folder may be named `bench…`: the repository's `.gitignore` hides such folders.
- Nothing here loads anything from the internet at run time, and nothing reads a hidden test set.
