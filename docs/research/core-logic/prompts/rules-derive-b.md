# Derivation B

- **Model:** Sonnet 5.5, fresh agent, one run per unit (33 runs).
- **Give it:** this prompt, filled, and the French editions (`{{french_sources}}`). Like A, it reads only its allow-list: never derivation A or C, any reconciled or final claim record, the boundary cases, any report, the example records, the units of 10 November, `rules/GRADES.md`, `slots.json` or `specs/rule-evaluator.md`.
- **Placeholders:** as derivation A (`{{worktree}}`, `{{unit_kind}}`, `{{unit_id}}`, `{{unit_brief}}`, `{{modules_table}}`, `{{slots}}`, `{{vocabulary}}`, `{{store_path}}`, `{{store_text}}`, `{{rules_tool}}`, filled the same way), plus `{{french_sources}}`: for each instrument the unit concerns, the paths of the French text prepared in step 2's first pass, outside the repository (`C:\Users\TheShadow\dz-core-research\french\`): the French Official Journal issue(s), as text by `pdftotext` where the PDF has a usable text layer, as page images otherwise (B can read images), and the two French PDFs already in the repository (`docs/official-law-texts/90-04_Original_Version_FR.pdf`, `90-11_Original_Version_FR.pdf`). Where no French text exists for a chapter, say so in the placeholder.
- **Output:** `rules/derivations/b/{{unit_id}}.checklist.json`, then `rules/derivations/b/{{unit_id}}.json` (stage `derivationB`), and `rules/derivations/b/{{unit_id}}.requests.jsonl` when needed.
- **Why a different route:** two agents that read the same Arabic article in the same order make the same mistake. B starts from the claim as a lawyer meets it and from the **French** edition, and only then finds the Arabic text. A disagreement between A and B is then informative; an agreement is worth more.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

You are deriving the legal rules for one unit of an Algerian civil-procedure assistant. No lawyer will check your work. Another agent derives the same unit by another route; a script compares the two field by field. Do not try to guess what the other will write: work from the texts.

**The unit:** {{unit_kind}} `{{unit_id}}`

{{unit_brief}}

**Shared modules** (do not restate their rules in a claim record; import them):

{{modules_table}}

**The facts a rule may test (slots):**

{{slots}}

**The event kinds and request kinds you may use:**

{{vocabulary}}

**The French editions:** {{french_sources}}

## What you may open

- **Read only:** `<CL>\rules\DERIVER-GUIDE.md`; `<CL>\rules\rule-record.schema.json`; the article store `{{store_path}}` (the **only** source of law you may cite) and its text views, one file per code, in `{{store_text}}` (the same entries, each paragraph labelled as a cite counts it: read these, since the JSON file is too large to read whole); the French files listed above; for a claim, the final module records `<CL>\rules\final\<module>.json` of the 12 modules; and the files you write yourself.
- **Write only:** `<CL>\rules\derivations\b\{{unit_id}}.checklist.json`, `<CL>\rules\derivations\b\{{unit_id}}.json`, `<CL>\rules\derivations\b\{{unit_id}}.requests.jsonl`.
- **Run only:** `{{rules_tool}}` as shown at the end. Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment. Do not run git.
- **Everything else is off limits**, in particular: the other derivations (`<CL>\rules\derivations\a\`, `\c\`, `\codex\`) and the `quoted\` copies; `<CL>\rules\reports\`; `<CL>\rules\boundary-cases\`; `<CL>\rules\reconciled\`, `\final-candidates\`, and any final record of a claim type; `<CL>\rules\examples\`; every other file under `<CL>` and the repository; `<CL>\law\requests.jsonl`; other git branches; any other repository or worktree; `C:\Users\TheShadow\dz-core-research\` except the store file, its text views and the French files; the state file and the run ledger; the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## Phase 1: the checklist, from memory, before you open any text

Write `<CL>\rules\derivations\b\{{unit_id}}.checklist.json`: a JSON array of strings, each one thing a careful Algerian lawyer must check before filing this kind of claim (or one question the module must answer). 15 to 40 lines. Do not open the store or the French files before this file is written, and do not edit it later.

## Phase 2: the record, by this route

1. **Start from the case, not from the code.** Write down (for yourself) three concrete, different cases of this claim as a client would tell them, and one case that looks like it and is not. For each, ask in order: who may sue and whom; before which section and where; what must have been done before filing; by when, and how the days are counted; what must be proved, by whom, with what; what can be asked.
2. **Answer each question in the French edition first.** Find the article and the exact alinéa. Note the article number and the paragraph. **Where no French text exists for a chapter,** still start from your cases, and read the Arabic chapter from its end backwards (the last article first), so that you do not follow the order another reader follows; say so in your reply.
3. **Then find the same article in the article store** (Arabic, the only source you may cite) and check that the Arabic paragraph says the same thing: the same number, the same period, the same condition. **If the Arabic and the French differ, the Arabic is the official text:** follow it, set the item `interpretive`, and describe the difference in `interpretiveNote`.
4. Read `<CL>\rules\DERIVER-GUIDE.md` and the schema, and write the record, stage `derivationB`, `"verified": false`.

**Rules of the record** (the same for every deriver; the guide gives the detail)

- **You point; you do not quote.** `cite` = code, article, paragraphs counted from 1 as the store lists them; `says` = one short sentence in your own words, without article or paragraph numbers. Never write `quote`, `source` or `grading`.
- Cite the narrowest paragraphs that carry the rule; cite an exception paragraph as its own statement.
- A text you need that is not in the store: do not cite it. Add a `checklist` line with `"answer": "searchedNotFound"`, and append to `<CL>\rules\derivations\b\{{unit_id}}.requests.jsonl` one line `{"code": "...", "article": "...", "askedBy": "derivationB", "unit": "{{unit_id}}", "why": "..."}`. A belief from memory goes in `openPoints` with grade D.
- Answer everything the brief lists: basis with the exact part; elements to prove and facts to state (with the event kinds above); usual requests with their articles, accessory ones included; section, territory, exits from the ordinary courts; every limitation or forfeiture with its starting event, how it is counted, and who raises it; prior steps; documents; boundaries.
- Deadlines carry `extract` (the Arabic words of the period and of the sanction) and `counting` (left out, and the deadline `interpretive`, when the texts do not settle it). A forfeiture declares `blockCleanExport`; a limitation declares `warn`; a duty without a stated sanction is `interpretive` and declares `warn`.
- Conditions test the slots above; a fact that is missing goes in `proposedSlots`.
- Unclear law: `interpretive`, every reading in `readings`, the protective reading as the rule, an `openPoints` entry.
- At least six worked examples, with the declared effect of each item the case is about and the set of slots to ask for. Use your three cases and the near-miss among them.
- A claim record imports the modules that apply (block ids from the module's final record) and does not restate them; modules made mostly of statements and requests are imported by those ids, and the claim writes its own `court.section` citing them.
- Messages for the lawyer: formal Arabic and English, short, naming the article; they say what to check, never what to conclude.
- Do not search the web; do not cite a commentary, a website or a court decision.

Run `{{rules_tool}} check --store {{store_path}} <CL>\rules\derivations\b\{{unit_id}}.json` and `{{rules_tool}} examples <CL>\rules\derivations\b\{{unit_id}}.json`, and fix what they report. Never change an expectation to make a wrong rule pass.

**Your reply:** at most fifteen lines: every place where the Arabic and the French differ; every chapter you read without a French text; what you could not settle; the texts you requested; the slots you proposed.
