# Specification: calling Codex, and the event-log check

Codex (OpenAI's CLI, `codex exec`) is the second model family of this task. It is called from scripts only, never interactively. This file fixes how it is called and how every call is checked. The rules here are hard rules: a result that did not pass them is thrown away.

## Why these rules exist

The owner wants Codex kept away from the repositories and from every hidden test set. A read-only sandbox does not guarantee that by itself. Measured on this laptop on 12 November 2026 (codex-cli 0.167.0, logged in with ChatGPT):

| Test | Result |
|---|---|
| Codex asked to read a file under `C:\Users\TheShadow\...` (outside its working folder), 12 November | **Refused by Windows** ("Accès refusé"): the sandbox runs commands as another local user, who had no right to that folder then. |
| The same probe, 13 November (the executor's first session) | **Read successfully.** By then several folders of the owner's profile carried an explicit grant `CodexSandboxUsers:(OI)(CI)(RX)` (read and list, inherited by everything below): `dz-core-research`, `dz-fable-core`, `dz-arch-research`, `.claude`, `.codex`; and `(M)` (modify) on the main folder `dz-legal-assistant`. Nobody in this work set them: Codex's own Windows sandbox (`sandbox = "elevated"` in its configuration) adds such grants to folders it works in. The event-log check rejected the call, as it must. |
| Codex asked to read a file in a folder at the root of `C:` | **Read successfully.** Root folders inherit read access for every local user. |
| Codex asked for a one-word answer and told not to use tools | No command was run; the event log holds one message only. |
| `whoami` inside the sandbox (13 November) | `CodexSandboxOffline`: the model's commands run as that separate local account. |
| Codex given a scanned Official Journal page as an image | It transcribed article 19 of Law 90-04 with 65 of 66 words equal to the library's text. |

So: `C:\dz-hidden-v2` and `C:\dz-hidden-v3` are at the root of `C:`, and the working folders of this work (the store, the note sets while they are built, the scenarios and the writers' labels in `dz-core-research`; the worktree; Claude Code's own transcripts in `.claude`) all **could be read by a sandboxed Codex call that chose to look**. The sandbox is not a wall around any of them.

**The owner decided on 13 November 2026 that no folder is locked:** the access rights of the hidden folders are never changed. The protection is: the no-open rule for the builder and its agents; an empty scratch folder with a read-only sandbox for every Codex call; and **the event-log check below, which discards any call that opened a file or ran a command.** That check is therefore not a formality: it is the one thing that would catch Codex looking where it must not. Never weaken it.

## The call

```text
codex exec "<prompt, or - to read it from standard input>" ^
  -s read-only ^
  -C "<scratch folder>" ^
  --skip-git-repo-check ^
  --ephemeral ^
  --json ^
  --output-schema "<schema file>" ^
  -o "<file for the last message>" ^
  [-i "<image file>" ...]
```

- **Order matters.** `-i` takes a list of files and swallows everything after it. Put the prompt first and `-i` last. A call with `-i` before the prompt exits with "No prompt provided" and makes no model call.
- **Standard input.** Give the prompt on standard input (prompt argument `-`) when it is long or holds Arabic text; otherwise redirect standard input from the null device. Without either, Codex prints "Reading additional input from stdin..." and waits.
- **`--ephemeral` is mandatory:** no session file is kept on disk, so no later agent can read a transcript.
- **`--output-schema` is mandatory** for every call whose answer a script reads. The last message is then a JSON object that matches the schema. The same text is written to the `-o` file.
- **The scratch folder** is `C:\Users\TheShadow\dz-core-research\codex-scratch\<role>\`, outside every repository. It must be **empty** when the call starts, except for calls that are meant to read files placed there on purpose (none in the present plan: law texts are sent inside the prompt).
- **Environment.** Both keys are Windows user variables, so every process inherits them unless something removes them. Since the owner's decision of 14 November 2026 the removal is automatic, not an instruction: `scripts/clean_env.py` removes both from any process that imports it (every script of the folder does, directly or through `store.py`), the Codex call takes its environment from `clean_env()`, and a command started from a shell goes through `python clean_env.py <command>`. `tests/test_clean_env.py` checks, with dummy values, that each of those launches sees neither key. No key is ever passed to Codex; it uses the ChatGPT login.
- **Model.** Do not pass `-m`: the subscription's default is used. Record the CLI version (`codex --version`) with every batch.
- **Cost.** $0: the calls run under the owner's existing ChatGPT plan. A call carries about 17,000 input tokens of overhead before the prompt. Respect the plan's limits: see "Pacing" below.

## The event log

With `--json`, standard output is one JSON object per line:

| `type` | Content |
|---|---|
| `thread.started` | `thread_id` |
| `turn.started` | |
| `item.started`, `item.completed` | `item`: `{ "id", "type", ... }`. Seen types: `agent_message` (`text`), `command_execution` (`command`, `aggregated_output`, `exit_code`, `status`). |
| `turn.completed` | `usage`: `input_tokens`, `cached_input_tokens`, `output_tokens`, `reasoning_output_tokens` |

**The rule.** A call is accepted only if **every** `item` in its log has a type in the allowed list:

```text
agent_message, reasoning
```

Any other item type (`command_execution`, a file change, a web search, a tool call of any kind, or a type the script does not know) **rejects the whole call for tool use**. The script writes one line to its own log (call id, role, the offending item type, never the item's content) and counts the rejection.

Also rejected: a non-zero exit code; a log without `turn.completed`; a last message that does not match the schema; a `-o` file that differs from the last `agent_message`. (A usage-limit refusal is a failure of its own kind: see "Pacing".)

**A rejected call: one rule, for every role.** Its answer is discarded: not parsed, not stored. The same prompt is never sent again automatically. Its items (notes, articles, pairs, questions) go back to the queue **once**, to be sent in a later call built afresh from the queue; an item whose second call is rejected too is set aside and reported, never queued a third time. A rejection for tool use is counted apart from the other rejections: for an open set, fix the prompt before the next batch (many tool-use rejections mean the prompt invites tool use); for a hidden set, stop and tell the owner.

Every Codex prompt in `prompts/` ends with the same sentence: "Do not run any command and do not open any file." Prompts used by both families (the page reading) give the Codex call that sentence too: its image arrives with `-i`, so it has nothing to open. The check does not rely on the sentence.

## Where the logs go

| Kind of call | Event log and last message |
|---|---|
| Notes, reviews, rule checks, page readings | `C:\Users\TheShadow\dz-core-research\codex-logs\<role>\<batch>\<call>.jsonl` and `.json`. Outside the repository. Never committed. |
| Anything that touches hidden set v3 (blind relabelling) | **Inside the hidden folder only**, under `C:\dz-hidden-v3\_codex\`. The script prints counts only. See `specs/hidden-v3-commands.md`. |

For each call the script keeps a provenance record (committed for non-hidden work, as one line of a JSONL file under `docs/research/core-logic/notes/provenance/` or the step's own folder): call id, role, prompt template name and version, CLI version, date and time, token usage, accepted, rejected (and why: tool use or other) or refused, and the SHA-256 of the prompt and of the answer. Never the text of a hidden note.

## Pacing

- One call at a time. No parallel Codex calls.
- Work in batches across the day. After each batch, the script records how many calls were accepted, rejected (for tool use, and for other reasons, apart) and refused for a usage limit, and the time of the first refusal. After each day of batches, send the owner a two-line progress note: calls accepted, rejected and refused; what was produced; what is next.
- **How a usage-limit refusal is recognised:** the Codex process fails (a non-zero exit, an `error` or `turn.failed` event, or no `turn.completed`) and its standard output, standard error or event log holds a message about a limit, a quota or a rate (for example "usage limit", "quota", "rate limit", HTTP 429). The script reads the message only to classify the failure, and logs the class, never the text. A refused call is not a rejection: its items stay where they are in the queue.
- A refusal for a usage limit stops the batch at once: the script records where it stopped, and the next batch, later in the day or the next day, resumes from there. Never loop on a refusal.
- **Two consecutive failed calls of any kind stop the batch** (refused, rejected, timed out, or failed for a reason the script does not know). The script records where it stopped and why; the executor reads the reason before the next batch starts. This is what keeps an unknown failure from running through a whole queue.
- The pilot of step 3 measures how many calls a usage window allows; the calendar is derived from that and reported to the owner.
- If the limit bites, Codex's calls go first to hidden set v3 and the mirror test, then the development notes, then the rule reviews, and last to training volume.

## The owner's three safeguards (13 November 2026)

The owner accepted the read access described above, kept the event-log check as the protection, changed no permission, and added three safeguards. `codex_run.py` enforces the first two in code: `call()` refuses to run (`Gate`) unless both hold.

1. **A read probe at the start of every day's batch.** Probe 2 below is run first each day. **No real call is made that day unless the probe was rejected by the check** (`codex_run.py probe2` writes `C:\Users\TheShadow\dz-core-research\codex-daily-probe.json` with the day, the version and the result; `call()` reads it).
2. **Codex CLI stays at 0.167.0 for the whole task.** `call()` checks `codex --version` against `codex-cli 0.167.0`. If Codex updated itself, re-run both probes before any real call **and tell the owner**.
3. **Nothing from a discarded call is ever used, not even part of its output.** A rejected or refused call's answer is not parsed or stored, and its last-message file is deleted at once; only the event log stays, for the record, and it is read only to classify the call. Its items go back to the queue under the one rule above, to be sent afresh.

## Checks before the first real call of a session ("done when")

1. `codex --version` is recorded, and from `codex login status` only "logged in: yes" or "no" (never the account it names). If the version differs from 0.167.0, the two probes below are run again and their results compared with this file; any difference is reported to the owner before real calls start.
2. **Probe 1 (no tool):** a one-word answer under a schema. Accepted; the log holds `agent_message` only.
3. **Probe 2 (profile canary):** Codex is asked to read a canary file under `C:\Users\TheShadow\dz-core-research\canary\`. Expected: the event-log check **rejects** the call (it ran a command). The probe script, and only it, may print the probe command's own output. On 12 November that output was "access denied"; since 13 November it is the canary's content (the grant above). **The pass mark is the rejection**, not the denial: the rejection is the protection.
4. The rejection path is exercised: the script's test suite feeds it a stored log that contains a `command_execution` item and expects a rejection, and a stored log of a usage-limit failure (written by hand from the rule above until a real one is seen) and expects a refusal that stops the batch.

The stored logs of the probes are in `C:\Users\TheShadow\dz-core-research\codex-probes\`: `p1` (probe 1), `p2` (probe 2), `p4` (a page reading with `-i`: `agent_message` only, so a page reading passes the rule). `p3` (a read of a folder at the root of `C:`) and `p5` (a test of a folder lock) belong to the tests of the table above and to the lock the owner withdrew on 13 November: they are not run again.

## Traps

- **Do not weaken the allowed list** because a batch has many rejections. Many rejections mean the prompt invites tool use: fix the prompt.
- **Do not print Codex's answer to the console in a hidden-set script.** The console of the agent that launches the script is part of that agent's context.
- **Do not reuse a scratch folder that is not empty.** Codex reads what it finds there.
- **A sandbox is not a boundary for folders at the root of `C:`.** Never assume a folder is safe from Codex because the call was read-only.
- **No access right is changed to protect a hidden set** (owner's decision, 13 November 2026). Do not "add a lock to be safe": on this laptop the account and the computer share the name `TheShadow`, and a grant by account name shut the owner's own account out of a test folder.

Sources: Codex documentation, "Sandboxing" (https://learn.chatgpt.com/docs/sandboxing), read on 12 November 2026: "`read-only`: The agent can inspect files, but it can't edit files or run commands without approval." The rest of this file is measured on this laptop.
