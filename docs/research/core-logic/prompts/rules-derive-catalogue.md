# Derivation from the catalogue (the catalogue route for claim types)

- **Model:** Opus 5.5, fresh agent, one run per claim type on the catalogue route (`CATALOGUE_ROUTE` in `scripts/unit.py`: ten claim types, ten runs). It is stage 2a of that route and takes the place of derivation A: the record it writes is a derivation A record, in the same files.
- **Why it exists:** the owner, 18 November 2026: "Claim types, since Claude's limit is now the bottleneck: no full derivation for the remaining 17. For each, compare the app's current catalogue (data/law/claims) with the official text and the final modules by script, and derive only what is missing or wrong, on the light route." The script half is `scripts/catalogue_check.py`, run by `unit.py next` before this launch; this prompt is the other half. Unlike `rules-derive-a.md`, the deriver **is** given the catalogue's articles, requests, documents and facts, in full: it starts from them.
- **Give it:** this prompt, filled. It reads only the files the prompt's allow-list names. It never sees another derivation, any reconciled record, the boundary cases, any report other than its own catalogue check (which is pasted in), the example records, `rules/GRADES.md`, `slots.json` or `specs/rule-evaluator.md` (it reads `rules/DERIVER-GUIDE.md` instead).
- **Placeholders:**
  - `{{worktree}}`: `C:\Users\TheShadow\dz-fable-core`. `{{unit_kind}}` (always `claim`), `{{unit_id}}`.
  - `{{unit_brief}}`: as for `rules-derive-a` (the claim's id, label, description, area, elements and request kinds from `claim-elements.json`).
  - `{{catalogue_entry}}`: the claim's entry of `data/law/claims/<area>.json`, **whole**, as JSON (articles with code, number, role, why, required; `suggestedRequests`; `requiredDocuments`; `factsToState`; `prerequisites` where it has them).
  - `{{catalogue_check}}`: `rules/reports/<unit>.catalogue-check.json` as JSON (`catalogue_check.py`: each catalogue article's state in the store, its proof label and paragraph count, whether it is in the unit's texts, the final module items resting on it; the module items the prerequisites and documents may point to, each with its shared words and a strength).
  - `{{texts_path}}`: the unit's texts (stage 0, `reports/<unit>.texts.json`, field `rendered`), written by the filler as plain text into the unit's run folder as `texts.txt`.
  - `{{module_finals}}`: the paths of the final module records (`prompts.module_finals`).
  - `{{modules_table}}`, `{{slots}}`, `{{vocabulary}}`, `{{store_path}}`, `{{store_text}}`, `{{rules_tool}}`: as for `rules-derive-a`.
- **Output:** files it writes: `rules/derivations/a/{{unit_id}}.checklist.json` (written **before** it opens any text), `rules/derivations/a/{{unit_id}}.json` (the record, stage `derivationA`, the schema of every record), `rules/derivations/a/{{unit_id}}.requests.jsonl` when it needs a text the store lacks, and `rules/derivations/a/{{unit_id}}.catalogue-changes.json`, checked by `unit.py done` against `schemas/catalogue-changes.schema.json`: `{ "unit", "changes": [{ "part": "article" | "request" | "document" | "fact" | "prerequisite" | "element" | "deadline" | "priorStep" | "court" | "boundary" | "import" | "other", "catalogueSays", "what": "kept" | "corrected" | "added", "items": [...], "reason" }], "wouldBlock": [{ "item", "question": { "ar", "en" } }] }`. `unit.py done` also checks that every catalogue article, request, document, fact and prerequisite is named in some `catalogueSays`, that `wouldBlock` lists exactly the record's own items that declare `blockCleanExport`, and that no question holds a digit or the words of the item's `extract`. Its reply: a short list of what it could not settle, and the items it would make block.
- **Why `wouldBlock` carries a question:** stage 2e gives each such question, and only the texts, to a fresh Sonnet reader (`rules-blocker-view.md`). The question must let that reader find the answer in the texts without being handed it.
- **The shape is repeated in the body** (agents see only the body; fault of 14 November 2026).

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

You are writing the legal rules for one claim type of an Algerian civil-procedure assistant. No lawyer will check your work. The assistant already has a catalogue entry for this claim type, written by hand and never checked against the official texts. Your task is to **start from that entry**: keep what the official texts support, correct what they contradict, and add what the entry lacks. No second deriver works on this claim type; a fresh reader will check, from the texts alone, every item you make block the clean export, and an adversary and a verifier will attack the result. Work so that your record is right on its own.

**The unit:** {{unit_kind}} `{{unit_id}}`

{{unit_brief}}

**The catalogue entry, in full** (the starting point; nothing in it is verified):

```json
{{catalogue_entry}}
```

**The catalogue check** (facts read by a script: for each catalogue article, whether the article store has it, in force or repealed, verified or not and how, how many paragraphs, whether it is among the unit's texts, and which final module items already rest on it; for each prerequisite and document, the final module items whose wording shares words with it: a "strong" match shares two words or more, a "weak" one a single word, and both are leads to check, not findings):

```json
{{catalogue_check}}
```

**Shared modules** (do not restate their rules in a claim record; import them):

{{modules_table}}

**The final module records** you may read, to choose what to import and the ids of the blocks:
{{module_finals}}

**The facts a rule may test (slots):**

{{slots}}

**The event kinds and request kinds you may use:**

{{vocabulary}}

## What you may open

- **Read only:** the unit's texts, `{{texts_path}}` (every article of each chapter the claim type's seed articles touch, each paragraph labelled `[code article ¶n]` as a cite counts it); `<CL>\rules\DERIVER-GUIDE.md` (how a record is written); `<CL>\rules\rule-record.schema.json` (the format); the article store `{{store_path}}` (the **only** source of law you may cite) and its text views, one file per code, in `{{store_text}}` (read these for any article outside the unit's texts: the JSON file is too large to read whole); the final module records listed above; and the files you write yourself.
- **Write only:** `<CL>\rules\derivations\a\{{unit_id}}.checklist.json`, `<CL>\rules\derivations\a\{{unit_id}}.json`, `<CL>\rules\derivations\a\{{unit_id}}.requests.jsonl`, `<CL>\rules\derivations\a\{{unit_id}}.catalogue-changes.json`.
- **Run only:** `{{rules_tool}}` as shown at the end. Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment. Do not run git.
- **Everything else is off limits**, in particular: the other derivations (`<CL>\rules\derivations\b\`, `\c\`, `\codex\`) and the `quoted\` copies; `<CL>\rules\reports\` (the catalogue check you need is pasted above); `<CL>\rules\boundary-cases\`; `<CL>\rules\reconciled\`, `\final-candidates\`, and any final record of a claim type; `<CL>\rules\examples\`; `data\law\claims\` (the entry you need is pasted above); every other file under `<CL>` and the repository; `<CL>\law\requests.jsonl`; other git branches; any other repository or worktree; `C:\Users\TheShadow\dz-core-research\` except the store file, its text views and the texts file above; the state file and the run ledger; the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## Phase 1: the checklist, before you open any text

Write `<CL>\rules\derivations\a\{{unit_id}}.checklist.json`: a JSON array of strings. Each string is one thing a careful Algerian lawyer must check before filing this kind of claim: the text that grounds the claim; each element to prove; who bears the proof; the competent section and court; every time limit, its starting event, how it is counted, and its sanction; every step required before filing; special laws and exceptions; cases that look like this one and are not; amendments you remember. Use the catalogue entry above, and go beyond it: a hand-written catalogue usually lacks the time limits, the steps before filing and the boundaries. 15 to 40 lines. Do not open the texts file, the store or a module record before this file is written. Do not go back and edit it later.

## Phase 2: the record, from the catalogue and the official texts

Read `<CL>\rules\DERIVER-GUIDE.md` and the schema. Then write `<CL>\rules\derivations\a\{{unit_id}}.json`, a record with `"stage": "derivationA"`, `"verified": false`, and `"derivedBy": { "role": "derivationA", "model": "<your model>", "date": "yyyy-mm-dd" }`.

**Start from the catalogue, item by item**

- **Each catalogue article:** open it in the texts and read the article before it and the article after it. If the text supports what the catalogue's `why` says, keep it as a statement with the narrowest paragraphs that carry it. If the text says something else, write what the text says (a corrected statement). If the article is repealed, not in the store, or does not bear on this claim, leave it out and say why. Where the catalogue check shows a final module item resting on the same article, import that module block instead of restating it.
- **Each suggested request:** keep it as a request tied to the article it rests on, correct it, or leave it out with the reason. Every request kind of the brief must appear, or a checklist line says why not.
- **Each required document, each fact to state, each prerequisite:** keep it as a document, an element (with the event kinds above) or a prior step, tied to its text; or correct it; or leave it out with the reason. A document or a prerequisite that a final module already carries is imported from it.
- **What the catalogue lacks:** the deadlines (limitation and forfeiture), the steps before filing, the competent section, the territory and the exits from the ordinary courts, the boundaries (what looks like this claim and is not), the accessory requests, and the module blocks that apply. Add each from the texts.

**How to work**

- Start from the Arabic texts. Read whole chapters, not single articles: the exception is usually in the next paragraph or the next article.
- **You point; you do not quote.** A statement has a `cite` and a short `says` in your own words, without article or paragraph numbers. Never write `quote`, `source` or `grading`.
- Cite the **narrowest paragraphs that carry the rule**, and cite the exception paragraph as its own statement when there is one.
- **If a text you need is not in the store,** do not cite it from memory. Add a line to the record's `checklist` (in the record file, not in the phase-1 list, which you never edit) with `"answer": "searchedNotFound"` and what you searched, and append one line to `<CL>\rules\derivations\a\{{unit_id}}.requests.jsonl`: `{"code": "...", "article": "...", "askedBy": "derivationA", "unit": "{{unit_id}}", "need": "basis" | "context", "why": "..."}` (`"need": "basis"` when a rule of your record would have to cite that text; `"context"` when you only wanted to read around it). Anything you believe from memory goes in an `openPoints` entry with grade D, never in a rule.
- **Everything the brief lists must be answered:** basis (with the exact paragraph or item); elements to prove and the facts to state for each; usual requests, accessory ones included, each tied to its article; the competent section, the territory, and the cases that leave the ordinary courts; every limitation or forfeiture, its starting event, how it is counted, and whether the court raises it of its own motion or only on a plea; prior steps; required documents; boundaries.
- **Deadlines.** For each: `kind`, `period`, `startsFrom` (a date slot), `counting` (derived from the texts; left out, and the deadline `interpretive`, when the texts do not settle it), `raisedBy`, the declared effect, and `extract`: the exact words of the text that give the period and the sanction. A forfeiture declares `blockCleanExport`; a limitation always declares `warn`. If the text states a duty and no sanction, the item is `interpretive` and declares `warn`.
- **A prior step** declares `blockCleanExport` when it is missing only if the text makes the claim inadmissible without it; otherwise `warn`.
- **Conditions** test the slots above. If you need a fact that is not there, declare it in `proposedSlots` (the guide says how) and use its id; do not propose a slot for something an existing one covers.
- **Where the law is unclear,** say so: set `interpretive`, keep every reasonable reading in `readings`, and choose as the rule the reading that protects the lawyer (warn rather than stay silent). Add an `openPoints` entry.
- **Worked examples.** At least six, covering: a clean case; each deadline far inside, far outside and within a few days of its last day; each prior step done, not done, and unknown; each exemption; each exit. State the **declared** effect you expect for each item the case is about, and the set of slots the evaluator should ask for, as the guide explains.
- **Imports:** `imports` lists the module id, the block ids from the module's final record, and `how`. Do not restate an imported rule. Where a module consists mostly of statements and requests, import those by id, and write the claim's own `court.section` citing the imported statements.
- Messages shown to the lawyer (`message`) are in formal Arabic and in English, short, and name the article. They state the rule and what to check; they never tell the lawyer what to conclude.
- Do not search the web. Do not cite a commentary, a website or a court decision.

## Phase 3: the list of changes, and the items that would block

Write `<CL>\rules\derivations\a\{{unit_id}}.catalogue-changes.json`, one JSON object of exactly this shape:

```json
{ "unit": "{{unit_id}}",
  "changes": [
    { "part": "article", "catalogueSays": "<what the catalogue says>", "what": "kept", "items": ["<ids of your record's items>"], "reason": "<one or two sentences naming the paragraph>" }
  ],
  "wouldBlock": [
    { "item": "<id of an item of your record>", "question": { "ar": "<the question in Arabic>", "en": "<the same question in English>" } }
  ] }
```

- **"changes":** one entry for **every** part of the catalogue entry, and one for every item you added.
  - "part", one of: "article", "request", "document", "fact", "prerequisite" (a part of the catalogue entry); "element", "deadline", "priorStep", "court", "boundary", "import", "other" (what you added).
  - "catalogueSays": for a catalogue article, its code and number exactly as the catalogue writes them, separated by one space (for example `civil-code 450`), then what its `why` says; for a request, a document, a fact or a prerequisite, its Arabic text copied letter for letter from the entry above; `""` for an item you added.
  - "what": "kept" (the texts support it as the catalogue has it), "corrected" (the texts say something else, or it is left out: say which in "reason"), or "added" (the catalogue lacks it).
  - "items": the ids of the record's items that carry it (`[]` when it is left out); an imported block as `<module>:<block id>`.
  - "reason": one or two sentences that name the paragraph, as `[code article ¶n]`.
- **"wouldBlock":** one entry for **every** item of your own record that declares `blockCleanExport` (in `effectWhenExpired` or `effectWhenMissing`), and no other. `[]` when none does.
  - "item": the item's id.
  - "question": the question that the item answers, written so that a reader who has only the texts can answer it **without being handed your answer**. Name the claim and the step or the time limit's subject (for example: before filing this claim, must the plaintiff take a certain step, and what follows if it was not taken; or: within what time must this claim be filed, from which event, and what follows if it is filed late). Never put in it a number, a period, an article or paragraph number, the starting event you chose, the sanction you chose, or the words of your `extract`. A script refuses a question with a digit or with the words of the item's `extract`.

When the files are written, run `{{rules_tool}} check --store {{store_path}} <CL>\rules\derivations\a\{{unit_id}}.json` and `{{rules_tool}} examples <CL>\rules\derivations\a\{{unit_id}}.json`, and fix what they report. Never change an expectation to make a wrong rule pass.

**Your reply:** at most twenty lines: what you could not settle, the texts you requested, the slots you proposed, **the items you would make block the clean export** (their ids, one line each, or the words "no item would block"), and **every worked example whose expected findings you changed after you first ran the tool on it** (its id, and what changed), or the words "no example changed". No summary of the record.
