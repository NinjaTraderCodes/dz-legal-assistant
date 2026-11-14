# Adversary with web access

- **Model:** Opus 5.5, fresh agent with web search and fetch, one run per unit (33 runs).
- **Give it:** this prompt, filled; it reads the reconciled record **with its quotes**, the store and the amendment ledger, and nothing else of the repository. Not the derivations, not the reconciliation report, not the differences, not the other attacks: it should attack the record, not re-argue the reconciliation.
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`), `{{unit_id}}`, `{{unit_kind}}`, `{{record_path}}` (the quoted copy `rules/reconciled/quoted/{{unit_id}}.json`), `{{store_path}}`, `{{store_text}}` (its text views), `{{ledger_path}}` (`law/amendment-ledger.json`, as far as the scan has run; if the file does not exist yet, the prompt says so), `{{oj_fetch}}` (the absolute command line of `research/core-logic/scripts/oj_fetch.py` run with Python 3.12: the one script through which every download from joradp.dz goes; it holds a lock, waits between downloads, records the hash and refuses a second simultaneous download).
- **Output:** `rules/reports/{{unit_id}}.attacks.adversary.json`: an array of attacks, each `{ "id", "target": "<item id>", "kind": "amendment" | "specialLaw" | "exception" | "procedure" | "caseLaw" | "misreading" | "missing", "claim", "source": { "tier": 1 | 2 | 3, "url", "what", "retrievedOn" }, "quoteArabic", "consequence" }`.
- **Why the source tiers:** the web is full of confident, outdated and wrong statements of Algerian law. An attack is a **lead**. It changes a record only when the verifier finds the official text. The adversary is told so, so that it brings sources, not opinions.
- **Sites it may fetch:** `joradp.dz` (only through `oj-fetch`), the Supreme Court's own site, the Council of State's, ministries' official sites. Anything else it may read as a lead and must label tier 3.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

Your only job is to break one legal rule record of an Algerian civil-procedure assistant. No lawyer will check this record: if it is wrong and you do not find it, a lawyer may file a claim that is out of time, before the wrong court, or without a step the law requires. You succeed when you find something real that the record gets wrong or leaves out. You do not succeed by agreeing with it.

**The unit:** {{unit_kind}} `{{unit_id}}`. **The record:** `{{record_path}}`. Read it whole: every statement with its quoted text, every deadline (with how it is counted), prior step, section rule, exit, boundary, open point and checklist line.

## What you may open

- **Read only:** the record `{{record_path}}`; the article store `{{store_path}}` (and its text views, one file per code, in `{{store_text}}`: the same entries with each paragraph labelled as a cite counts it; read these, since the JSON file is too large to read whole) (to read the paragraphs and articles around a cited one); the amendment ledger `{{ledger_path}}`; and, on the web, the sources described below.
- **Write only:** `<CL>\rules\reports\{{unit_id}}.attacks.adversary.json`.
- **Run only:** `{{oj_fetch}} --wait <url>`, for every download from joradp.dz (never fetch joradp.dz any other way). If it fails, do not run it again in a loop: note the failure and go on. Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment.
- **Everything else is off limits**, in particular: the derivations (`<CL>\rules\derivations\`), the agents' own reconciled file outside its quoted copy, every other report under `<CL>\rules\reports\` (the reconciliation report, the differences, the other attacks), `<CL>\rules\boundary-cases\`, `<CL>\rules\final\`, `<CL>\rules\final-candidates\`, `<CL>\rules\examples\`; other git branches and any other repository; `C:\Users\TheShadow\dz-core-research\` except the store file and its text views; the state file, the run ledger and the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Do not run git. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## Hunt for, in this order

1. **An amendment.** Was any cited article amended, replaced or repealed after the wording quoted? Look in the ledger, then in the Official Journal itself (search the law's number and the code's name with «يعدل ويتمم», «modifiant et complétant», «abrogé»). Finance laws and omnibus laws amend codes without naming them in their title.
2. **A special law** that displaces the general rule for some cases of this claim (consumer law, insurance, transport, public procurement, agricultural land, rent laws, social security, bankruptcy, the laws on specific professions).
3. **An exception or a condition in the same code** that the record does not cite: the paragraph after, the article after, the chapter's general provisions, the transitional provisions of the amending law.
4. **A procedural rule** of the Code of Civil and Administrative Procedure that changes the answer: another section or court, a mandatory prior step, a different time limit or way of counting it, an inadmissibility raised by the court of its own motion.
5. **A principle of the Supreme Court** (or the Council of State, or the Tribunal of Conflicts) that reads the text differently from the record. Only from the court's own publications.
6. **A misreading:** a quoted paragraph that does not say what the item says; a period, a starting event, a way of counting or a sanction that is not in the words; a "must" where the text says "may".
7. **A missing case:** a common situation of this claim that the record's examples and boundaries do not cover and where its rules would give a wrong or silent answer. Build the case and say what the record would do.

**Rules of evidence.** Every attack names its source and its tier:

- **Tier 1:** a page of the Official Journal (give the issue, the year, the page, and the URL on joradp.dz). Quote the Arabic words.
- **Tier 2:** an official consolidated text, or a decision on the court's own website (give the URL, the decision's number and date). Quote the words.
- **Tier 3:** anything else: a law firm's page, a commentary, a forum, a legal database, your own memory. Say what it is. It is a lead only.

Download from joradp.dz only with `{{oj_fetch}} --wait`: it keeps to one download at a time with a pause between them, and records what was read. Do not use any service that needs an account, a key or a payment. Do not include any personal data in a search.

A tier-3 attack is welcome when it is specific enough to be checked ("Law 23-xx art. y is said to have shortened this period to z; not found on joradp.dz"). A vague doubt is not an attack.

**Do not** propose fixes to the record, and do not edit it. Write `<CL>\rules\reports\{{unit_id}}.attacks.adversary.json`, each attack with an `id` (`adv-1`, `adv-2` …).

**Your reply:** the number of attacks per kind and per tier, and the single attack you believe most likely to be right. If you found nothing after a serious search, say what you searched; do not invent an attack.
