# Triage of an Official Journal hit

- **Model:** Sonnet 5.5, fresh agent. One run handles the hits of one issue (or of a few issues of the same month), or one blind issue. Specification: [../specs/official-journal-scan.md](../specs/official-journal-scan.md), sections "From hits to a ledger" and "Blind issues".
- **Placeholders:** `{{worktree}}`; `{{issue}}` (year, number, date if known, the French and Arabic URLs); `{{hits}}` (each hit: its class (title, operative or other), the page, and the sentence the scan matched; or the words "blind issue: no text layer"); `{{instruments}}` (the instruments the rules rest on, with their numbers and code names); `{{cited_articles}}` (for each instrument, the articles the rule records cite today); `{{work_folder}}` (an absolute path outside the repository, empty, for example `C:\Users\TheShadow\dz-core-research\triage\2022-048\`); `{{oj_fetch}}` (the absolute command line of `oj-fetch`: `C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe C:\Users\TheShadow\dz-fable-core\research\core-logic\scripts\oj_fetch.py`).
- **Output:** `{{work_folder}}\triage-<year>-<number>.json`: `{ "issue", "issueDate": "yyyy-mm-dd" | null, "files": [{ "url", "sha256" }], "texts": [{ "kind": "law" | "ordinance" | "decree" | "rectification" | "other", "number", "date", "title", "amends": "<instrument id or none>", "pdfPages": [int], "articlesTouched": [{ "article", "how": "replaced" | "amended" | "completed" | "inserted" | "repealed", "citedByARecord": boolean }], "transitional": "<a sentence, or empty>", "confidence": "read" | "unsure" }], "falseHits": [{ "page", "why" }] }`. The executor's script merges these into `law/amendment-ledger.json`.
- **Why an agent and not a pattern:** the scan finds sentences; whether "modifiant et complétant la loi n° …" introduces a text that touches article 187 مكرر takes reading the operative articles. The triage only says where to look: every touched article that a record cites is then read as [../law/README.md](../law/README.md) says (Sonnet 5.5, then Codex; Opus 5.5 settles a difference).
- **Downloads:** only through `oj-fetch`, the one door to joradp.dz, which holds the lock, pauses and records the hash.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else, except in the work folder this prompt names, which is outside it.

**What you may use, and nothing else:**
- **Run:** `{{oj_fetch}} <url> --out {{work_folder}} --caller triage --wait 600`, to download the issue; `pdftotext`, `pdftoppm` and `pdfinfo` on the files in `{{work_folder}}`; and, at the end, a delete of the PDFs in `{{work_folder}}`. No other command; no other download, from joradp.dz or anywhere else; no web search.
- **Read:** the files in `{{work_folder}}` (the PDFs `oj-fetch` put there, the text and the page images you make from them), with your Read tool.
- **Write:** page images and text files under `{{work_folder}}`, and your output file. **Delete:** the PDFs in `{{work_folder}}`, at the end.

Everything else is off limits, in particular: every file of the repository (you run `oj-fetch` but do not read or change any file of the repository); the rule records, `data/law/`, `docs/research/core-logic/law/` and the article store; Codex's corpus and every other git branch; everything under `C:\Users\TheShadow\dz-core-research` beyond `{{work_folder}}`; the plan file and the state file (`C:\Users\TheShadow\dz-core-research\STATE.md`); any memory file (do not read or write anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`.

Before your first command, remove the two keys from your shell, and never print a key: in PowerShell `Remove-Item Env:OPENROUTER_API_KEY,Env:GOOGLE_API_KEY -ErrorAction SilentlyContinue`; in bash `unset OPENROUTER_API_KEY GOOGLE_API_KEY`.

A scan of the Official Journal of the Algerian Republic found sentences in one issue that may announce an amendment to a law this project relies on. Read the issue and say exactly what it changes.

**The issue:** {{issue}}

**The sentences the scan matched:**

{{hits}}

**The instruments that matter:**

{{instruments}}

**The articles of those instruments that our rules cite today:**

{{cited_articles}}

Do this:

1. Download the French edition of the issue, then the Arabic one, with `oj-fetch` as above (it waits its turn and pauses by itself). If it answers `busy` after its wait, or fails, say so in your reply and stop: do not retry in a loop. Record each file's SHA-256 (`oj-fetch` prints it) in "files".
2. Find the pages you need with the French text layer (`pdftotext -f <page> -l <page> -layout`). Then **render those pages as images** (`pdftoppm -r 200 -f <page> -l <page> -png <pdf> <prefix>`, in `{{work_folder}}`) and read the images with your Read tool. The Arabic text layers of 2005 and 2007 are garbled, and older issues are scans: never trust a text layer for a word or a number.
3. If the hits say "blind issue", read the issue's first pages (the masthead and the table of contents) from the images instead: give the issue's date in "issueDate", and treat every title that names one of the instruments as a matched sentence. Otherwise give the date printed on the masthead.
4. For each matched sentence, find the text it belongs to (its kind, number, date and title) and read its operative articles: which articles of which instrument it replaces, amends, completes, inserts or repeals. List each one in "articlesTouched". Read the Arabic edition for the article numbers (the Arabic numbering is the official one: «مكرر», «مكرر 1»).
5. Note any transitional provision: the date of entry into force, and whether the old wording keeps governing existing contracts or pending cases. Copy that sentence.
6. A matched sentence that turns out to be only a citation in a decree's visa («Vu la loi n° …»), an application decree that changes no article of the instrument, or another law with a similar number, goes in "falseHits" with the reason.
7. "confidence": "read" when you read the operative articles on the page image; "unsure" when the page is a poor scan, the list of touched articles may be incomplete, or the Arabic and French disagree. Say which.

Do not summarise the law, and do not judge whether an amendment matters for a rule: list what the text does. When the output file is written, delete the PDFs in `{{work_folder}}`; keep the hashes.

**Your reply:** one line per text found (kind, number, the instrument it amends, how many articles touched, how many of them cited by a record), the count of false hits, and any download that failed.
