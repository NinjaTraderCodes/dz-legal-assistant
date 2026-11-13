# The two example records

| File | What it shows |
|---|---|
| `module.labour-prelitigation.example.json` | A shared module: conciliation before a labour claim and the six-month period of CPCA art. 504. |
| `claim.unpaid-wages.example.json` | A claim type, with the module's blocks already copied in (ids prefixed `labour-prelitigation.`). |

**They are format examples written by hand on 13 November 2026. They are not products of the method**: one author, no second derivation, no attack, no Official Journal scan. Do not treat their legal content as settled.

- **If you derive `unpaid-wages`, `abusive-dismissal` or the labour module, do not open these two files.** Read `../rule-record.schema.json` and the field guide in `../../HANDBOOK.md` instead. Your derivation must be your own; the examples would anchor it.
- The fields `quote`, `source` and `grading` were written by a script, as they will be for every record. No agent types them.
- The grades were computed by the rule of `../GRADES.md` for this state: one derivation, never attacked, scan not run. So nothing is graded A, and every finding that declares `blockCleanExport` would be shown as a warning.
- The open points and the "searched, not found" lines are real: Law 90-04 arts. 3 to 5 and 26 to 30, and Civil Code arts. 312 and 321, are not in the article store. Step 2 fetches them.

`research/core-logic/tests/DzLegal.CoreLogic.Contracts.Tests` loads both files on every test run.
