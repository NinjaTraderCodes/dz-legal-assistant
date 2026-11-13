# Derivation A

- **Model:** Opus 5.5, fresh agent, one run per unit (21 claim types, 12 modules: 33 runs).
- **Give it:** this prompt, filled. It reads only the files the prompt's allow-list names. It never sees derivation B or C, any reconciled record, the boundary cases, any report, the example records, the units of 10 November, `rules/GRADES.md`, `slots.json` or `specs/rule-evaluator.md` (the last three state things a deriver is meant to find in the texts: it reads `rules/DERIVER-GUIDE.md` instead).
- **Placeholders:**
  - `{{worktree}}`: `C:\Users\TheShadow\dz-fable-core`.
  - `{{unit_kind}}` (claim | module), `{{unit_id}}`.
  - `{{unit_brief}}`: for a claim, its `id`, `label` and `description` from `data/law/claims/<area>.json`, its `area`, and its elements (claim, kind, what must be proved) and request kinds from `data/law/claim-elements.json`; **never** the catalogue's `articles`, `prerequisites`, `requiredDocuments` or `suggestedRequests` (they become checklist questions, `from: catalogue`, in the merged checklist). For a module, its row of the handbook's module table (all three columns).
  - `{{modules_table}}`: the 12 modules, id and "what it must answer", one line each.
  - `{{slots}}`: every slot of `slots.json` with its id, type, question (Arabic and English) and choices; never `help`, `askedFor` or `origin`. For the labour module and the two labour claim types, leave out the slots the seed list took from the hand-made examples (`origin` `labour-prelitigation` or `unpaid-wages`), unless a final module record has since adopted them.
  - `{{vocabulary}}`: the event kinds and request kinds of `FactVocabulary` (`src/DzLegal.Drafting/CaseFacts.cs`), pasted.
  - `{{store_path}}`: the article store file. `{{rules_tool}}`: the rules tool, built once by the executor (agents never build).
- **Output:** files it writes: `rules/derivations/a/{{unit_id}}.checklist.json` (written **before** it reads any text), `rules/derivations/a/{{unit_id}}.json` (the record, stage `derivationA`), and `rules/derivations/a/{{unit_id}}.requests.jsonl` when it needs a text the store lacks (the executor's script appends those lines to `law/requests.jsonl` once A and B have both returned). Its reply: a short list of what it could not settle.
- **Why two phases:** the checklist from memory is written before the texts narrow the mind; it becomes lines the final record must answer.
- **Why "pointers, not quotes":** a script writes the words (`quote-records`), so a misquote is impossible and a wrong pointer fails loudly.
- **Why an allow-list:** a fresh agent has read access to the whole laptop and receives the user's memory index and `AGENTS.md` automatically. A list of forbidden places always has holes; a list of the only places it may open does not.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

You are deriving the legal rules for one unit of an Algerian civil-procedure assistant. No lawyer will check your work. Another agent is deriving the same unit independently, and a script will compare the two field by field; a third agent will attack the result. Work so that your record is right on its own.

**The unit:** {{unit_kind}} `{{unit_id}}`

{{unit_brief}}

**Shared modules** (do not restate their rules in a claim record; import them):

{{modules_table}}

**The facts a rule may test (slots):**

{{slots}}

**The event kinds and request kinds you may use:**

{{vocabulary}}

## What you may open

- **Read only:** `<CL>\rules\DERIVER-GUIDE.md` (how a record is written); `<CL>\rules\rule-record.schema.json` (the format); the article store `{{store_path}}` (the **only** source of law you may cite); for a claim, the final module records `<CL>\rules\final\<module>.json` of the 12 modules (to choose what to import, and the ids of the blocks); and the files you write yourself.
- **Write only:** `<CL>\rules\derivations\a\{{unit_id}}.checklist.json`, `<CL>\rules\derivations\a\{{unit_id}}.json`, `<CL>\rules\derivations\a\{{unit_id}}.requests.jsonl`.
- **Run only:** `{{rules_tool}}` as shown at the end. Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment. Do not run git.
- **Everything else is off limits**, in particular: the other derivations (`<CL>\rules\derivations\b\`, `\c\`, `\codex\`) and the `quoted\` copies; `<CL>\rules\reports\`; `<CL>\rules\boundary-cases\`; `<CL>\rules\reconciled\`, `\final-candidates\`, and any final record of a claim type; `<CL>\rules\examples\`; every other file under `<CL>` and the repository; `<CL>\law\requests.jsonl`; other git branches; any other repository or worktree; `C:\Users\TheShadow\dz-core-research\` except the store file; the state file and the run ledger; the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## Phase 1: the checklist, from memory, before you open any text

Write `<CL>\rules\derivations\a\{{unit_id}}.checklist.json`: a JSON array of strings. Each string is one thing a careful Algerian lawyer must check before filing this kind of claim (or, for a module, one question the module must answer): the text that grounds the claim; each element to prove; who bears the proof; the competent section and court; every time limit, its starting event, how it is counted, and its sanction; every step required before filing; special laws and exceptions; cases that look like this one and are not; amendments you remember. Be generous: 15 to 40 lines. Do not open the store before this file is written. Do not go back and edit it later.

## Phase 2: the record, from the official texts

Read `<CL>\rules\DERIVER-GUIDE.md` and the schema. Then write `<CL>\rules\derivations\a\{{unit_id}}.json`, a record with `"stage": "derivationA"`, `"verified": false`.

**How to work**

- Start from the Arabic texts in the store. Read whole chapters, not single articles: the exception is usually in the next paragraph or the next article. For every article you rely on, read the article before it and the article after it.
- **You point; you do not quote.** A statement has a `cite` and a short `says` in your own words, without article or paragraph numbers. Never write `quote`, `source` or `grading`.
- Cite the **narrowest paragraphs that carry the rule**, and cite the exception paragraph as its own statement when there is one.
- **If a text you need is not in the store,** do not cite it from memory. Add a checklist line with `"answer": "searchedNotFound"` and what you searched, and append one line to `<CL>\rules\derivations\a\{{unit_id}}.requests.jsonl`: `{"code": "...", "article": "...", "askedBy": "derivationA", "unit": "{{unit_id}}", "why": "..."}`. Anything you believe from memory goes in an `openPoints` entry with grade D, never in a rule.
- **Everything the brief lists must be answered:** basis (with the exact paragraph or item); elements to prove and the facts to state for each (with the event kinds above); usual requests, accessory ones included, each tied to its article; the competent section, the territory, and the cases that leave the ordinary courts; every limitation or forfeiture, its starting event, how it is counted, and whether the court raises it of its own motion or only on a plea; prior steps; required documents; boundaries (what looks like this claim and is not, and what it is).
- **Deadlines.** For each: `kind`, `period`, `startsFrom` (a date slot), `counting` (derived from the texts; left out, and the deadline `interpretive`, when the texts do not settle it), `raisedBy`, the declared effect, and `extract`: the exact words of the text that give the period and the sanction. A forfeiture declares `blockCleanExport`; a limitation always declares `warn`. If the text states a duty and no sanction, the item is `interpretive` and declares `warn`.
- **Conditions** test the slots above. If you need a fact that is not there, declare it in `proposedSlots` (the guide says how) and use its id; do not propose a slot for something an existing one covers.
- **Where the law is unclear,** say so: set `interpretive`, keep every reasonable reading in `readings`, and choose as the rule the reading that protects the lawyer (warn rather than stay silent). Add an `openPoints` entry.
- **Worked examples.** At least six, covering: a clean case; each deadline far inside, far outside and within a few days of its last day; each prior step done, not done, and unknown; each exemption; each exit. State the **declared** effect you expect for each item the case is about, and the set of slots the evaluator should ask for, as the guide explains.
- **For a claim record:** import the modules that apply (`imports`: module id, the block ids from the module's final record, and `how`), and do not restate their rules. Where a module consists mostly of statements and requests (sections, proof, the petition's formal elements, accessory requests), import those by id, and write the claim's own `court.section` citing the imported statements.
- Messages shown to the lawyer (`message`) are in formal Arabic and in English, short, and name the article. They state the rule and what to check; they never tell the lawyer what to conclude.
- Do not search the web. Do not cite a commentary, a website or a court decision.

When the record is written, run `{{rules_tool}} check --store {{store_path}} <CL>\rules\derivations\a\{{unit_id}}.json` and `{{rules_tool}} examples <CL>\rules\derivations\a\{{unit_id}}.json`, and fix what they report. Never change an expectation to make a wrong rule pass.

**Your reply:** at most fifteen lines: what you could not settle, the texts you requested, the slots you proposed. No summary of the record.
