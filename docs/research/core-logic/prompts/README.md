# Prompt templates

One file per agent role and per Codex call. A script or the executor takes **everything after the line `--- PROMPT ---`**, fills the `{{placeholders}}`, and sends it. The part above that line is for the executor: who runs it, with what, what comes back, and why the prompt is written as it is. Do not send it.

**Rules for every prompt.**

- An agent gets **only** what its prompt names. Independence is the whole point of having two derivations, a blind judge and an adversary: an agent that is shown another's work confirms it. When in doubt, give less.
- Claude agents are started **fresh** with the Agent tool and the model named in the header (never a fork of the executor, which would carry its context). Their reply is their output; where a file is to be written, the prompt names the path.
- **Every Claude prompt opens by fixing the place:** the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`), absolute paths, and an **allow-list**: the paths the agent may read and the paths it may write. Everything else is off limits. An agent's shell may start in another folder on another branch; without this line it would write there.
- **Every fresh agent is handed the user's memory index and `AGENTS.md` automatically.** Prompts for independent or isolated roles therefore forbid reading or writing any memory file and opening the plan or the state file.
- **Derivers do not read `rules/GRADES.md`, `slots.json` or `specs/rule-evaluator.md`**: those files state things a deriver is supposed to find. They read `rules/DERIVER-GUIDE.md`, and get the slots as the placeholder `{{slots}}` (id, type, question; no help text).
- **Judges, guide testers and realism reviewers use no tool.** Their inputs are pasted into the prompt; their answer is their reply. The executor reads the run's reported tool-use count; a run with any tool use is discarded and run again by a fresh agent.
- Codex calls follow [../specs/codex-calls.md](../specs/codex-calls.md) exactly: an output schema, an empty scratch folder, `--ephemeral`, the event-log rule. Every Codex prompt ends with the sentence "Do not run any command and do not open any file." (a page reading adds: "other than the image you were given"). Law texts and notes are sent **inside the prompt**. A rejected call's answer is discarded and the same prompt is never sent again automatically; its items go back to the queue once.
- Codex's output schemas live in `prompts/schemas/<name>.schema.json`. They are not written yet: write each from its prompt's header the first time the prompt is used, with a test that the two agree.
- Never put a hidden note, a key, a real name or the owner's email in a prompt.
- A prompt has a version (the commit that last changed it). Record it with every output.
- If a prompt produces bad output, **fix the prompt and say so in the report**; do not hand-edit the output.
- The planted-error test has no prompt of its own: it runs the attack, support-check and verify prompts on copies outside the repository, with those copies' paths ([../specs/record-checks.md](../specs/record-checks.md)).

| File | Role | Model | Step |
|---|---|---|---|
| `rules-derive-a.md` | Derivation A | Opus 5.5 | 1 |
| `rules-derive-b.md` | Derivation B | Sonnet 5.5 | 1 |
| `rules-derive-c-codex.md` | Third derivation, modules only | Codex | 1 |
| `rules-checklist-codex.md` | Checklist before, claim types | Codex | 1 |
| `rules-boundary-cases.md` | Boundary-case writer | Sonnet 5.5 | 1 |
| `rules-reconcile.md` | Reconciler | Opus 5.5 | 1 |
| `rules-adversary.md` | Adversary with web access | Opus 5.5 | 1 |
| `rules-attack-codex.md` | Attack on the finished record | Codex | 1 |
| `rules-support-check-codex.md` | Does the text support the field? (blind pairs) | Codex | 1 |
| `rules-verify.md` | Verifier of attacks | Opus 5.5 | 1 |
| `legal-review-answer.md` | Answers to the 22 questions | Opus 5.5 and Sonnet 5.5 | 1 |
| `legal-review-attack-codex.md` | Attack on the answers | Codex | 1 |
| `law-read-page.md` | Reading an article from a page image | Sonnet 5.5, then Codex | 2 |
| `law-settle-difference.md` | Settling two readings on the image | Opus 5.5 | 2 |
| `journal-hit-triage.md` | Triage of an Official Journal hit | Sonnet 5.5 | 2 |
| `notes-guide-test.md` | Labelling the guide-test items from the guide alone (45 items on 13 November) | Sonnet 5.5 and Codex | 3 |
| `notes-guide-items.md` | Writing about 20 new guide-test items for each rerun | Sonnet 5.5 | 3 |
| `notes-write.md` | Note writer | Codex (and Sonnet 5.5 on the cross-check sample) | 3 |
| `notes-judge.md` | Blind judge; also the mirror set's third judge | Sonnet 5.5 (Codex on the cross-check; Opus 5.5 as third judge) | 3 |
| `notes-sort-dropped.md` | Sorting dropped and blanked notes | Opus 5.5 | 3 |
| `notes-realism-review.md` | Realism review by the other family | Sonnet 5.5 and Codex | 3 |
| `notes-clean-write.md` | Clean-note writer | Jais-3-3.7B or Gemma 4 E4B (local) | 3 |
| `notes-clean-rank.md` | Ranking the two local writers | Sonnet 5.5 and Codex | 3 |
| `v3-planner.md` | Planner of hidden set v3 (also of the mirror set) | Sonnet 5.5, isolated | 3 |
| `v3-writer.md` | Writer of hidden set v3 (also of the mirror set) | Sonnet 5.5, isolated | 3 |
| `v3-blind-relabel.md` | Blind relabelling | Codex | 3 |
| `gen-apply-law.md` | The sentence that applies the law to the facts | Gemma 4 E4B, Jais-3-3.7B, Jais-3-0.9B, Qwen3.5-2B (local) | 5 |
| `gen-judge.md` | Blind judge of those sentences | Sonnet 5.5 and Codex | 5 |
| `freeze-rebuild.md` | Rebuilding the results from the freeze document | Sonnet 5.5 | 6 |
