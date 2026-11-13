# Scripts (Python 3.12)

Run with `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`; tests with `<that python> -m pytest research/core-logic/scripts/tests`. Only the standard library is used. Working files go to `C:\Users\TheShadow\dz-core-research\`, never into the repository, except the committed evidence and law files named below.

| Script | What it does | Specification |
|---|---|---|
| `oj_fetch.py` | The one door to joradp.dz: one download at a time, a pause of 3 to 6 s, a lock, a record of every fetch, a robots check. | `docs/research/core-logic/specs/official-journal-scan.md` |
| `journal_scan.py` | The Official Journal scan: every French issue since 2003, the kept text outside the repository, the evidence list (`docs/research/core-logic/law/journal-scan/`). | same |
| `textcmp.py` | Comparing two Arabic texts at levels 1, 2, 2o and 3. | `docs/research/core-logic/specs/article-store.md` |
| `store.py` | The article store: library, corpus at `9b60990`, delta, readings, ledger, keyed by (code, article, version). | same |
