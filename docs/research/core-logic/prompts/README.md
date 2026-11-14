# Prompt templates

One file per agent role and per Codex call. A script or the executor takes **everything after the line `--- PROMPT ---`**, fills the `{{placeholders}}`, and sends it. The part above that line is for the executor: who runs it, with what, what comes back, and why the prompt is written as it is. Do not send it.

**Rules for every prompt.**

- An agent gets **only** what its prompt names. Independence is the whole point of having two derivations, a blind judge and an adversary: an agent that is shown another's work confirms it. When in doubt, give less.
- Claude agents are started **fresh** with the Agent tool and the model named in the header (never a fork of the executor, which would carry its context). Their reply is their output; where a file is to be written, the prompt names the path.
- **Every Claude prompt opens by fixing the place:** the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`), absolute paths, and an **allow-list**: the paths the agent may read and the paths it may write. Everything else is off limits. An agent's shell may start in another folder on another branch; without this line it would write there.
- **Every fresh agent is handed the user's memory index and `AGENTS.md` automatically.** Prompts for independent or isolated roles therefore forbid reading or writing any memory file and opening the plan or the state file.
- **Derivers do not read `rules/GRADES.md`, `slots.json` or `specs/rule-evaluator.md`**: those files state things a deriver is supposed to find. They read `rules/DERIVER-GUIDE.md`, and get the slots as the placeholder `{{slots}}` (id, type, question; no help text).
- **No-tool agents** (the third derivation, the attack, every relabeller, judge, tester, reviewer, ranker and sorter) are launched with one sentence that names their filled prompt file; **reading that file is the one tool call they may make.** Their answer is their reply. `records.py ingest-reply` copies it from the transcript, checks it against its schema, and discards the run if any other tool was used; a discarded run is run again by a fresh agent. (It is never run on a transcript of a hidden-set agent.)
- Codex calls follow [../specs/codex-calls.md](../specs/codex-calls.md) exactly: an output schema, an empty scratch folder, `--ephemeral`, the event-log rule. Every Codex prompt ends with the sentence "Do not run any command and do not open any file." (a page reading adds: "other than the image you were given"). Law texts and notes are sent **inside the prompt**. A rejected call's answer is discarded and the same prompt is never sent again automatically; its items go back to the queue once.
- **Every prompt whose JSON answer is copied from a reply or returned by a model has a schema file in `prompts/schemas/`**, and `tests/test_prompts.py` checks that the prompt's **body** names every key and every closed value of that schema (three runs were lost on 14 November 2026 because a shape sat only in a header). Prompts whose agent writes a file say the file's shape in their body.
- **Codex is used in three roles only** (owner, 14 November 2026): it writes notes; it makes the support check, the blind view on disputed boundary cases and the page readings that need a second family; and it is the training backup, with the owner's approval. **No Claude model writes, labels or filters a training note.**
- Never put a hidden note, a key, a real name or the owner's email in a prompt.
- A prompt has a version (the commit that last changed it). Record it with every output.
- If a prompt produces bad output, **fix the prompt and say so in the report**; do not hand-edit the output.
- The planted-error rounds have no prompt of their own: they run the attack, support-check and verify prompts on copies outside the repository ([../specs/planted-errors.md](../specs/planted-errors.md)).

| File | Role | Model | Step |
|---|---|---|---|
| `rules-derive-a.md` | Derivation A | Opus 5.5 | 1 |
| `rules-derive-b.md` | Derivation B | Sonnet 5.5 | 1 |
| `rules-derive-c.md` | Third derivation of a module, by the flat route | Sonnet 5.5, no tool | 1 |
| `rules-boundary-cases.md` | Boundary-case writer | Sonnet 5.5 | 1 |
| `rules-reconcile.md` | Reconciler | Opus 5.5 | 1 |
| `rules-adversary.md` | Adversary with web access | Opus 5.5 | 1 |
| `rules-attack.md` | Attack on the finished record | Sonnet 5.5, no tool | 1 |
| `rules-support-check-codex.md` | Does the text support the statement? (blind pairs, seeded) | **Codex** | 1 |
| `rules-verify.md` | Verifier of every finding at once | Opus 5.5 | 1 |
| `boundary-second-view-codex.md` | Blind view on boundary cases the verifier set aside | **Codex** | 1 |
| `legal-review-answer.md` | Answers to the 22 questions | Opus 5.5 and Sonnet 5.5 | 1 (phase D) |
| `legal-review-attack.md` | Attack on an answer, by the other model | Opus 5.5 and Sonnet 5.5, no tool | 1 (phase D) |
| `law-read-page.md` | Reading an article from a page image | Sonnet 5.5; **Codex** only where a second family is needed | 2 |
| `law-settle-difference.md` | Settling two readings on the image | Opus 5.5 | 2 |
| `journal-hit-triage.md` | Triage of an Official Journal hit | Sonnet 5.5 | 2 |
| `notes-guide-items.md` | Writing new guide-test items | Sonnet 5.5, no tool | 3 |
| `notes-guide-test.md` | Answering the guide-test items from the guide alone; also the 75 cell items of the scheme | Sonnet 5.5 and Opus 5.5, no tool | 3 |
| `notes-write.md` | Note writer | **Codex** (training notes; half of dev and of challenge); Sonnet 5.5 (the other half) | 3 |
| `notes-filter-local.md` | The filter of training notes: a closed sheet | a local model (Gemma 4 E4B or Jais-3-3.7B) | 3 |
| `notes-noise-audit.md` | The noise measurement: a blind relabel of kept training notes that changes nothing | Opus 5.5, no tool | 3 |
| `notes-judge.md` | Blind relabeller of test notes | Sonnet 5.5 (notes by Codex or a local model); Opus 5.5 (notes by Sonnet); no tool | 3 |
| `notes-sort-dropped.md` | Sorting differing, dropped and blanked fields by cause | Opus 5.5, no tool | 3 |
| `notes-realism-review.md` | Realism review by a model that did not write the notes | Sonnet 5.5 or Opus 5.5, no tool | 3 |
| `notes-clean-write.md` | Clean-note writer; phrases of the composed notes | Jais-3-3.7B or Gemma 4 E4B (local) | 3 |
| `notes-clean-rank.md` | Ranking the two local writers | Sonnet 5.5 and Opus 5.5, no tool | 3 |
| `v3-planner.md` | Planner of hidden set v3 (also of the mirror set) | Sonnet 5.5, isolated | 3 |
| `v3-writer.md` | Writer of hidden set v3 (also of the mirror set) | Sonnet 5.5, isolated | 3 |
| `v3-blind-relabel.md` | Blind relabelling of the mirror set and of v3 | Opus 5.5, isolated | 3 |
| `notes-third-judge.md` | Third blind judge of the mirror set, by closed question sheets | Sonnet 5.5, no tool | 3 |
| `gen-apply-law.md` | Step 5, part A: the paragraph that applies the law to the facts | Jais-3-0.9B, Qwen3.5-2B, Jais-3-3.7B, Gemma 4 E4B (local) | 5 |
| `gen-facts.md` | Step 5, part B: the facts section | the same four local models | 5 |
| `gen-judge.md` | Blind judge of step 5's outputs | Sonnet 5.5 and Opus 5.5, no tool | 5 |
| `freeze-rebuild.md` | Rebuilding the results from the freeze document | Sonnet 5.5 | 6 |
