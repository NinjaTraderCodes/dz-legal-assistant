# Verifier of attacks

- **Model:** Opus 5.5, fresh agent with web fetch (official sites only). **One run per unit** (33 first runs), and one follow-up run whenever [../specs/unit-driver.md](../specs/unit-driver.md) asks for one ("Follow-up verifier runs": then the record it corrects is the last final candidate, quoted, and its register is `<unit>.attacks.followup<n>.json`).
- **Give it:** this prompt, filled. It reads only its allow-list: the quoted reconciled records of its units, their attack lists, the reconciliation reports (for the boundary cases the reconciler disputes), the store and the ledger. Never the derivations, the final records or any other report.
- **Placeholders:**
  - `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`); `{{units}}`.
  - `{{record_paths}}`: the quoted reconciled records (`rules/reconciled/quoted/<unit>.json`).
  - `{{attack_paths}}`: per unit, `rules/reports/<unit>.attacks.adversary.json`, `.attacks.sonnet.json` (the first module: `.attacks.codex.json`), the "no" and "partly" answers of `.support.json`, and, for a module, the entries of `.c-comparison.json` marked as attacks (the third derivation differing from a value A and B agreed on). **Once `unit.py` exists (phase 0), the placeholder holds one file per unit instead: `rules/reports/<unit>.attacks.input.json`, built by `unit.py verify-input`, which gathers all of these with the unanswered neighbour flags, the failed boundary cases and the owner's "done" rule** ([../specs/unit-driver.md](../specs/unit-driver.md), stage 8).
  - `{{reconciliation_paths}}`: `rules/reports/<unit>.reconciliation.md`.
  - `{{register_paths}}`: where the register goes, `rules/reports/<unit>.attacks.json`.
  - `{{candidate_paths}}`: where the corrected record goes, `rules/final-candidates/<unit>.json`.
  - `{{requests_path}}`: `law/requests.jsonl`.
  - `{{store_path}}`, `{{store_text}}` (its text views), `{{ledger_path}}` (`law/amendment-ledger.json` as far as the scan has run), `{{rules_tool}}`, `{{oj_fetch}}` (the absolute command line of `research/core-logic/scripts/oj_fetch.py` run with Python 3.12, complete but for the URL: `--caller`, `--wait <seconds>` and `--out <a folder of the run in the working folder>`; the first unit's prompt lacked them: the one script through which every download from joradp.dz goes).
- **The planted-error rounds** ([../specs/planted-errors.md](../specs/planted-errors.md)) use this prompt unchanged, with every path placeholder pointing into `C:\Users\TheShadow\dz-core-research\planted\round-<n>\` and the units named `p<n>-01` … and `c<n>-01` …: nothing of the real `rules/final/` or `rules/reports/` is then in its allow-list, and nothing real is overwritten.
- **Output:** the register (the file `records.py grade` reads; its shape is in the prompt's body since the first module, whose verifier never saw this header): every attack with `"verdict": "fixed" | "refuted" | "open"`, `"verifierQuote"` (the official words that decide it, with code, article, paragraph or the Official Journal reference), `"change"` (what was changed in the record, when fixed). And the corrected record, which keeps `"stage": "reconciled"` (only `records.py grade` sets `final`).
- **Why a separate verifier:** an attacker is paid to doubt and a reconciler has a record to defend. The verifier decides each attack on one question only: what does the official text say?
- **After it:** the scripts run again on the corrected record (`records.py quote`, `check-values`, `flag-neighbours`, `rules check`, `rules examples`), then `records.py flatten` and `records.py grade` produce `rules/final/<unit>.json`.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it and under the other folders this prompt names, and never write anywhere else.

Attacks were made on legal rule records of an Algerian civil-procedure assistant. You decide each attack, on one question only: **what does the official text say?** No lawyer will check after you.

**The units:** {{units}}
**The records:** {{record_paths}}
**The attacks:** {{attack_paths}}
**The reconciler's reports** (read them only for the boundary cases the reconciler disputes): {{reconciliation_paths}}
**The article store** (the texts already verified): `{{store_path}}` (and its text views, one file per code, in `{{store_text}}`: the same entries with each paragraph labelled as a cite counts it; read these, since the JSON file is too large to read whole). **The amendment ledger:** `{{ledger_path}}`.

## What you may open

- **Read only:** the files named just above; `{{worktree}}\docs\research\core-logic\rules\rule-record.schema.json` and `{{worktree}}\docs\research\core-logic\rules\DERIVER-GUIDE.md` (the format); and, on the web, official sources only, as described below.
- **Write only:** the registers {{register_paths}}, the corrected records {{candidate_paths}}, and new lines appended to `{{requests_path}}`.
- **Run only:** `{{rules_tool}}`, and `{{oj_fetch}} <url>` (the command saves each file in the folder it names after `--out`; you may read the files there and convert them with `pdftotext` or `pdftoppm` into that same folder) for every download from joradp.dz (never fetch joradp.dz any other way; if it fails, do not run it again in a loop: note the failure and go on). Before your first command, remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from your shell's environment. Do not run git.
- **Everything else is off limits**, in particular: the derivations; every final record and final candidate other than the ones you write; every report other than the ones named above; the boundary-case files; the example records; other git branches and any other repository; `C:\Users\TheShadow\dz-core-research\` except the paths named above; the state file, the run ledger and the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## For each attack, in order

1. Read the attacked item and its quoted paragraphs, and the paragraphs around them in the store.
2. **If the attack rests on a text** (an amendment, a special law, another article): find that text in an official source. In the store first. Otherwise on the Official Journal's site (joradp.dz, through `{{oj_fetch}} <url>`; keep the issue number, the page and the file's SHA-256), or, for a decision, on the court's own site. A commentary, a law portal or a search engine's summary is not a source: use it only to find the official one.
3. **Decide:**
   - **fixed:** the official text shows the record is wrong or incomplete. Correct the record (pointers only: `cite`, structured values, `counting`, conditions, messages; never `quote`, `source`, `grading`). If the text needed is not in the store, append it to `{{requests_path}}` (`"askedBy": "verifier"`) with the source you found, and make the item an open point of grade D until step 2 has read it twice.
   - **refuted:** the official text shows the record is right. Quote the words that show it.
   - **open:** you could not find an official text that decides it, or the texts allow both readings. Then the item becomes `interpretive` with both readings kept, the protective reading as the rule, and an open point. An attack from memory or from a tier-3 source that you could not confirm in an official text is **open**, not refuted, when it is specific; say what you searched.
4. Write the verdict with `verifierQuote`: the official Arabic words, with their reference. A verdict without a quote is not a verdict.

A decision of the Supreme Court changes a rule only if you fetched its text from the court's own site and quote it; then the item it touches is `interpretive` (it rests on case law) and cites the statute it reads.

**Boundary cases the reconciler disputes** (in its report): decide each the same way, from the text, and record it in the register as an attack on the item the case tests (`who: "boundaryCase"`). Never edit a boundary-case file.

**Do not** decide an attack by which side argues better, by majority, or by what is usual. **Do not** soften a rule to make an attack go away: if the text supports the rule, the rule stays. An item you add that the record did not have is graded lower for that reason alone; add it when the text requires it all the same.

**The register** of a unit is one JSON object: `{ "unit": "<unit id>", "attacks": [ … ] }`, one entry per attack, each `{ "id", "who": "adversary" | "attack" | "codex" | "support" | "derivationC" | "boundaryCase" | "neighbourFlag" | "values" | "owner" | "store", "target": "<item id, or several separated by commas>", "kind", "claim", "verdict": "fixed" | "refuted" | "open", "verifierQuote": "<the official words that decide it>", "quoteRef": "<code, article, paragraph, or the Official Journal issue and page>", "change": "<what you changed in the record, when fixed>" }`. Keep the attack's own id (`adv-…`, `cdx-…`, `sup-…`), and give a boundary case the id `bc-<case id>`.

Write every register (every attack, none dropped) and every corrected record (stage `reconciled`, `"verified": false`). Run `{{rules_tool}} check --store {{store_path}}` and `{{rules_tool}} examples` on each corrected record; add a worked example for every attack you marked fixed (the case the attack described, with the corrected outcome; `"by": "verifier"`).

**Your reply:** per unit, the counts fixed / refuted / open, and each fixed attack in one line.

## Ids, and the two kinds of finding that are not attacks

- **A value mismatch** (`who: "values"`, id `values-…`) is a number, a unit or a sanction word of an item that a script could not find in the item's quoted text. **A store refusal** (`who: "store"`, id `store-…`) is a cite the store does not hold as written. Decide each from the text, like an attack.
- **Keep each entry's own id** as its file gives it: `adv-…` (the adversary), `atk-…` or `cdx-…` (the attack), `sup-…` (the support check), `c-…` (the third derivation), `bc-…` (a boundary case), `flag-…` (a neighbour flag), `done-…`, `uncertain-…`, `owner-…`, `store-…`.
- **A neighbour flag** (`who: "neighbourFlag"`) is a paragraph next to a cited one that opens an exception or refers back, and that no line of the record answers. Decide it from the text: either cite it where it changes a rule, or add a line to the record's `checklist` with `"answer": "doesNotApply"` and a note that names the article and says why. Its verdict is `fixed` in the first case, `refuted` in the second.
- **A boundary case** (`who: "boundaryCase"`) is a case written from the texts alone by an agent that saw no record, with the outcome it expects and the tool's message for this record. Decide from the text whether the record or the case is right. **Never edit a boundary-case file.**
- **A line appended to the requests file** has this shape: `{"code": "…", "article": "…", "askedBy": "verifier", "unit": "<unit id>", "why": "…"}`.
- **The owner's rule on "done" (14 November 2026):** a prior step may say it was done only when nothing about that same step is still asked or warned about by another item (its means, its date). Otherwise the step itself stays open and leaves the answer to those items. The lawyer must never see "done" beside an open question about the same step.
