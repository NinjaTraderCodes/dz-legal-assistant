# The state file, and how to resume after a stop

**Why.** This work runs for weeks, through hundreds of agent runs and Codex calls, in a conversation whose memory is summarised and sometimes lost. After any stop (a full context, a closed laptop, a usage limit, a crash), the executor must be able to continue **from files alone**, without redoing work and without contaminating what must stay independent. The state file is the first thing read after a stop and the last thing written before one.

## Where

`C:\Users\TheShadow\dz-core-research\STATE.md`, **outside every repository** (it names local paths and is not a product). It was started on 12 November 2026 by the author of this handbook; keep its history and append.

Beside it, also outside the repository: `store\` (the article store), `pages\` (page images), `french\` (the French sources of derivation B), `codex-scratch\`, `codex-logs\`, `journal\` (the scan's working folder and the download lock), `notes-work\` (the queue, the scenarios, the writers' labels, and `sets\<set>.jsonl`), `models\` (weights and ONNX files, never committed), `planted\` (the planted-error test), `ledger\runs.jsonl` (below).

**The file as handed over** was restructured into the four parts below on 13 November 2026; its older log entries are kept as they were written. Some of them record things that were later reversed (a folder lock that the owner cancelled; a grade rule that the owner changed): **where an old log entry and the handbook differ, the handbook is right.**

## Format of `STATE.md`

Plain Markdown, four parts, in this order:

```markdown
# Core-logic task: state (read this first after any stop)

## 1. Where things are
Handbook: C:\Users\TheShadow\dz-fable-core\docs\research\core-logic\HANDBOOK.md  (branch fable/core-logic)
Last pushed commit: <id>   Last local commit: <id>   Working tree: clean | <what is uncommitted and why>

## 2. Now
Step: <1..6>   Stage: <name from the handbook's step table>
Next action: <one sentence, exact enough to do without thinking>
Waiting for the owner: <nothing | the question, asked on <date>, in the owner's words if answered>
Running in the background: <nothing | what, started when, where its output goes>

## 3. Counters
Claude agent runs: step 1-2: <n>/230   step 3: <n>/320 (405 at 25 notes per judge run)   steps 4-6: <n>/12
Codex calls: step 1-2: <n>/165   step 3: <n>/440   steps 4-6: <n>/15; accepted <n>, rejected for tool use <n>, invalid <n>, refused <n>; today <n>; first refusal today at <time>
Units: final <n>/33   reconciled <n>   derived A <n>  B <n>  C <n>/12
Notes: train <kept>/<written>  dev …  challenge …  mirror …  clean …  composed …
Hidden v3: <not started | planned | written <count> | relabelled | finalized sha <…> | run on <date>>

## 4. Log (newest last; never rewrite an entry)
- <date time>: <what was done, with paths and commit ids>. <what was learned that the handbook does not say>.
```

Rules:

- **Append to the log at every milestone and before every stop point**, and whenever something is learned that a successor would otherwise have to rediscover (a tool's quirk, a decision made while writing, a trap).
- **Part 2 is rewritten each time**, so that it is always true. "Next action" is one concrete action.
- Never write a key, the owner's email, a hidden note, or a count table that could identify a hidden note.
- When the owner decides something, quote the decision with its date in the log. **A decision recorded here is still the owner's: do not reinterpret it later.**

## The run ledger

`ledger\runs.jsonl`: one line per agent run and per Codex call, written by the script or the executor **when the run is launched** and completed when it returns:

```json
{"id": "r-0412", "kind": "agent" | "codex", "role": "rules-derive-b", "model": "claude-sonnet-5-5", "unit": "unpaid-rent", "prompt": "rules-derive-b.md@<commit>", "started": "…", "ended": "…", "status": "running" | "done" | "failed" | "rejected" | "discarded", "outputs": ["docs/research/core-logic/rules/derivations/b/unpaid-rent.json"], "sawOtherDerivation": false}
```

It answers, after a stop: what was running, what finished, and **which agent saw what** (needed to prove independence in the report).

## The notes queue

`notes-work\queue.jsonl`: one line per scenario, rewritten by the scripts as the note moves on, so that a stop in the middle of step 3 loses nothing and repeats nothing:

```json
{"scenario": "s-004211", "set": "train", "family": "f-0931", "state": "drawn" | "written" | "spansBuilt" | "judged" | "kept" | "keptWithBlanks" | "dropped" | "invalid", "writeCall": "<call id>", "judgeRun": "<run id>", "attempts": 1, "note": "n-000123"}
```

A note counts for a set only in the states `kept` and `keptWithBlanks`. A note that is `written` and not yet judged belongs to no set and is never trained on. The scenarios and the writers' labels stay in `notes-work\` (outside the repository) until their set is finished, so that no judge can open them.

## Resuming after a stop

1. **Read `STATE.md`,** then the handbook's section for the current step. Do not act from memory of the conversation.
2. **`git status` and `git log -3`** in the worktree: compare with part 1. Uncommitted files are either finished outputs (check them, commit them) or half-written ones (see 4).
3. **Check nothing is still running:** background shells, agents (list them; do not start new ones before you know), a Codex batch, the Official Journal scan (resumable by design: it continues from its evidence list). If a run in the ledger says `running` and nothing is running, mark it `failed`.
4. **Half-finished outputs.** Each kind has its rule:
   - a derivation or a reconciled record that fails `rules check`, or whose agent did not return: **delete it and run the unit again with a fresh agent**. Never complete another agent's half-record yourself, and never show a fresh deriver the half-record;
   - a Codex batch: accepted answers are kept, the rest of the batch is queued again (scenarios "not written", never "dropped"). A rejected call's answer is discarded and the same prompt is never sent again automatically; its items go back to the queue once. Two consecutive failed calls of any kind stop the batch ([specs/codex-calls.md](specs/codex-calls.md));
   - a judge run that did not return, or that used a tool: its notes go back to the state `spansBuilt` and are judged by a fresh agent; nothing of its partial answer is kept;
   - a v3 batch whose writer did not return its numbers: `v3 discard --batch <n>`, then a new writer;
   - a training run: restart from its last checkpoint, or from the start if it kept none; the result must name its seed either way.
5. **Re-run the cheap checks** before building on old outputs: both solutions build and test; `rules check` on every record of the current stage; the store rebuilds to the recorded hash.
6. **Independence check.** Before launching any agent that must be independent (a deriver, a judge, a v3 writer), confirm from the ledger that its inputs exclude what it must not see. If your own context now holds something a fresh agent must not get (you read derivation A; you are about to brief derivation B), that is fine, **as long as the prompt template is used unchanged** and you add nothing to it.
7. **If you cannot tell whether a hidden set was exposed** (a command printed something unexpected, a reply held text), stop and tell the owner. Do not decide alone that it was harmless.
8. Update part 2, then continue with "Next action".

## At every stop point

Before sending the owner a report that waits for an answer: commit and push what is finished; update `STATE.md` (part 2: "Waiting for the owner: …"); make sure nothing is running that the owner did not approve; then send the report. After the answer: quote it in the log, then continue.
