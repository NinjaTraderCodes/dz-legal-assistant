# Answers to the Legal Review questions

- **Models:** the 22 questions of `docs/research/core-logic/brief/LEGAL-REVIEW-QUESTIONS.md`, by section (5 groups). Each group gets **two independent answers**: one by Opus 5.5, one by Sonnet 5.5 (10 runs), fresh agents that do not see each other's answers. Then Codex attacks the pair ([legal-review-attack-codex.md](legal-review-attack-codex.md), 5 calls). The executor (Opus 5.5) writes the final answer from the two and the attack, by the reconciler's rule: settled by a text, or marked uncertain.
- **When:** after all units are final (several questions concern claim types: tatliq, abusive dismissal, khul, rent and eviction, sale annulment, labour, nafaqa, custody, and the limitation periods of all 21), so that the answers can cite verified texts and final records.
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`), `{{questions}}` (the group's questions, copied whole from the brief's file), `{{store_path}}`, `{{ledger_path}}` (`law/amendment-ledger.json`: the question on later amendments turns on it), `{{final_records}}` (the paths of **every** final record, the 12 modules and the 21 claim types), `{{answer_path}}` (`legal-review/answers-<a|b>/<group>.md`), `{{other_answers}}` (the other answerer's folder, named so that it can be forbidden).
- **Final output (the executor's):** `docs/research/core-logic/legal-review-answers.md`: for each question, the option chosen among those offered (or "none of the options, because …"), the answer in two or three plain sentences, the text it rests on (quoted by script from the store, with code, article and paragraph), a grade A to D by the rule of `rules/GRADES.md` (agreement of the two answers stands for agreement of two derivations), what remains uncertain, and what the answer changes in the app.
- **Why the cite lines:** each answer names its texts in one fixed form, `[<code> <article> ¶<n>]`, so that a script can paste exactly those articles (with their uncited paragraphs) into the Codex attack.
- **Why this is not legal advice to the owner:** every answer stays "not verified by a lawyer", with its grade. Where the law does not settle the question, the answer is the protective option and says so.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

The owner of an Algerian legal-drafting application asked lawyers a set of questions about what the application writes in a statement of claim (عريضة افتتاح دعوى). No lawyer has answered. You answer from the official texts, and you say plainly where the texts do not settle a question. Another agent answers the same questions independently; do not look for its answers.

**The questions:**

{{questions}}

**Your sources:** the article store `{{store_path}}` (official texts, the only law you may cite); the amendment ledger `{{ledger_path}}` (what the Official Journal scan found, as far as it has run); and the final rule records {{final_records}} (the shared modules and the claim types, already derived, attacked and graded: you may rely on what they establish, and you must say so when you do, with the record's id and the item's id).

## What you may open

- **Read only:** the store, the ledger and the final records named above.
- **Write only:** `{{answer_path}}` (under `<CL>`).
- **Everything else is off limits**, in particular: the other answerer's folder {{other_answers}}; the derivations, reconciled records, reports and boundary cases under `<CL>\rules\`; the example records; other git branches and any other repository; `C:\Users\TheShadow\dz-core-research\` except the store file; the state file, the run ledger and the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Do not run git. Do not search the web. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## For each question, write

1. **The option** you choose among those offered, by its number; or "none", with why.
2. **The answer** in two or three plain sentences.
3. **The texts:** each article and paragraph the answer rests on, one per line, in exactly this form: `[<code> <article> ¶<n>]` (code and article as the store writes them, one line per paragraph), followed by what its words establish. Point; do not paste long quotes. If the question turns on wording the application writes in Arabic, say precisely which words of which article support or contradict that wording. Where you rely on a final record, name it and the item (`record unpaid-wages, item d1`), and give the texts that item cites in the same form.
4. **What the texts do not settle.** Court practice, drafting custom and style are not in the texts. Where a question is about custom ("is this how petitions are usually written?"), say that no official text answers it, give the reading that is safest for a lawyer (the one least likely to draw an objection in form), and mark the answer as resting on no text.
5. **What you would need** to be surer: a text that is not in the store (name it), or a lawyer's practice.

Be strict with yourself about the difference between "the text says" and "I believe lawyers do". Never cite an article for something it does not say: if an article is about standing and interest, do not let it support a statement about fees.

Write your answers to `{{answer_path}}`. **Your reply:** for each question, the option chosen and one of: "settled by a text", "partly", "no text".
