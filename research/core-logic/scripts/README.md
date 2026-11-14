# Scripts (Python 3.12)

Run with `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe`; tests with `<that python> -m pytest research/core-logic/scripts/tests`. Only the standard library is used. Working files go to `C:\Users\TheShadow\dz-core-research\`, never into the repository, except the committed evidence and law files named below.

| Script | What it does | Specification |
|---|---|---|
| `oj_fetch.py` | The one door to joradp.dz: one download at a time, a pause of 3 to 6 s, a lock, a record of every fetch, a robots check. | `docs/research/core-logic/specs/official-journal-scan.md` |
| `journal_scan.py` | The Official Journal scan: every French issue since 2003, the kept text outside the repository, the evidence list (`docs/research/core-logic/law/journal-scan/`). | same |
| `textcmp.py` | Comparing two Arabic texts at levels 1, 2, 2o, 2s and 3. | `docs/research/core-logic/specs/article-store.md` |
| `store.py` | The article store: library, corpus at `9b60990`, delta, readings, ledger, keyed by (code, article, version). | same |
| `codex_run.py` | Every Codex call: the event-log check, the stopping rules, the owner's safeguards (pinned CLI, today's read probe, nothing kept from a discarded call). | `docs/research/core-logic/specs/codex-calls.md` |
| `seed.py` | The seed inventory of step 2's first pass: what the store holds for each seed article, whether it is in critical use, what to do, and where it is printed (`docs/research/core-logic/law/seed-inventory.json`). | `docs/research/core-logic/law/README.md` |
| `reading.py` | Page readings: jobs (one page or a pair for critical use, up to four otherwise), the prompt, the Codex call, the comparison with the store into `law/readings.jsonl`, the calibration rates. | `docs/research/core-logic/prompts/law-read-page.md`, `law/README.md` |
| `fetch.py` | Missing articles read from the Official Journal by two families, their comparison, the settler path (close-ups at full resolution), and the consolidated-PDF differences. | `docs/research/core-logic/law/README.md` |
| `french.py`, `consolidated.py`, `delta.py` | The French check, the consolidated-PDF proof (and its word differences), and the entries of `law/law-delta.json` (two wordings of one article included). | same |
| `records.py` | The record checks: quote, check-values (the period reader), flag-neighbours, merge-checklists, compare (A against B), compare-c, flatten, grade, check-checklist, inventory. | `docs/research/core-logic/specs/record-checks.md`, `rules/GRADES.md` |
| `ledger.py` | The amendment ledger from the triage answers and Codex's corpus ledgers (`law/amendment-ledger.json`). | `docs/research/core-logic/specs/official-journal-scan.md` |
| `store_text.py` | The store's text views, one file per code, each paragraph labelled as a cite counts it: what the derivers read. | `docs/research/core-logic/specs/article-store.md` |
| `prompts.py` | Fills the prompts of step 1 (derivations A, B and C, boundary cases) from the handbook's module table, `slots.json`, `FactVocabulary`, the unit's `texts.json` and the French index; refuses a placeholder left empty. | `docs/research/core-logic/prompts/` |
| `stage7.py` | Stage 7 of step 1: the reconciled record rendered for the Codex attack; the support pairs, one in five a seeded false pair whose key stays outside the repository; the support answers split back per unit, with the seeded rate and the attacks. | `docs/research/core-logic/prompts/rules-attack-codex.md`, `rules-support-check-codex.md` |
| `clean_env.py` | The one environment every launch starts from: the owner's two API keys removed from any process that imports it, from every Codex call, and from a command run as `python clean_env.py <command>`; `--check` says whether a process was started with either key (names only). | `docs/research/core-logic/specs/codex-calls.md` |
