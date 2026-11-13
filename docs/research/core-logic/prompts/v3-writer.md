# Writer of hidden set v3 (and of the mirror set)

- **Model:** Sonnet 5.5, **a fresh agent used only for this**, one run per batch (never a fork; never continued with a follow-up message). A few in parallel; say how many before launching. Specification: [../specs/hidden-v3-commands.md](../specs/hidden-v3-commands.md), sections 2, 4 and 6.
- **Route:** the Workflow tool with a reply schema of two integers, `{"batch": integer, "written": integer}`, if the owner opted in (stop point "A workflow"): text then **cannot** come back. Otherwise the Agent tool and the discard rule below. Either way the owner must first have allowed agents, in the owner's own Claude Code settings, to read and write under the set's root; no folder's access rights change. Before any v3 writer runs, the whole chain has been rehearsed on the mirror set.
- **Placeholders:** `{{root}}` (`C:\dz-hidden-v3`, or the mirror's open folder `C:\Users\TheShadow\dz-core-research\mirror`); `{{set}}` (`hiddenV3` or `mirror`); `{{batch}}` (the batch number, three digits); `{{round}}` (`first`: the batch's line is in `plan\design.json`; `refill 2` or `refill 3`: the line is the entry `{{batch}}` of `work\refill.json`, written by `v3 quotas`, in the same format, cells and numbers only).
- **What `{{root}}\inputs\` holds** (put there by `v3 init`): `guide.md`, `scheme.md`, `boundaries.md`, `note.schema.json`, `cases.schema.json`, `writing-rules.json` (the value of `set`, the id rule, the provenance values, the status). The prompt names these files; if one is missing, the writer replies `written: 0`.
- **What it writes:** first its own 25 cases, invented to its line, in `plan\batch-{{batch}}.cases.json`; then the notes, in `notes\batch-{{batch}}.jsonl`. Nothing else.
- **Reply:** numbers only. A writer that cannot write its file replies `written: 0` and nothing else.
- **If a reply ever holds anything else** (a note, a label, an excerpt, an explanation of any kind): stop; do not answer the agent; run `v3 discard --batch {{batch}}`; tell the owner in one line without repeating the text. A fresh writer then writes the batch again, with the same number and line.
- **After each run returns:** `v3 memory compare`. On `memory changed: yes`: stop, do not open the memory folder, tell the owner.
- **The builder never opens this agent's transcript.**
- **Why the writer also labels:** the gold answer of a hidden note is the writer's labels **confirmed by a blind relabelling** by another family (Codex). A note on which the two differ in any field is dropped. So the writer must label by the guide, exactly, and quote its own words.

--- PROMPT ---

**Where you work.** Your root folder is `{{root}}`. You may read and write only inside it, and nothing else on this computer. Your shell may start elsewhere: use absolute paths under `{{root}}`, and never write anywhere else.

You are writing one batch of a **hidden test set**: working notes that Algerian lawyers write before drafting a statement of claim, each with its labels. The set will measure a program that reads such notes. Its value depends on one thing: **nobody outside this task ever sees its contents.** That includes the agent that started you.

**Hard rules, without exception:**

1. Your final reply is exactly this and nothing else: `{"batch": <your batch number as a plain integer, without leading zeros>, "written": <number of notes you wrote>}`. No note, no label, no excerpt, no description of what you wrote, no comment on difficulties. If something goes wrong, or you cannot write your files, reply with `"written": 0` and nothing else.
2. Read only: the files of `{{root}}\inputs\`; and your batch's line, which is the entry `"batch": "{{batch}}"` of `{{root}}\plan\design.json` (round: {{round}}; for a refill round, the entry `"batch": "{{batch}}"` of `{{root}}\work\refill.json` instead). Use no other entry of those files.
3. Write only two files: `{{root}}\plan\batch-{{batch}}.cases.json`, then `{{root}}\notes\batch-{{batch}}.jsonl`.
4. Everything else is off limits, and in particular: other batches' cases and notes; any repository or worktree; any memory file (read none, write none, save no memory); the files under `C:\Users\TheShadow\.claude\`, including any plan file; the state file `C:\Users\TheShadow\dz-core-research\STATE.md` and everything under `C:\Users\TheShadow\dz-core-research\` that is not inside `{{root}}`; every other folder whose name begins with `C:\dz-hidden`. Search nothing; fetch nothing from the web.
5. Formal Arabic only in the notes: no dialect, no French, no English, no Latin letters except in «دج». Everything fictitious: no real person, company, case number or address.
6. Do not print a case, a note or a label in any message while you work. Write them to the files only.

**Read `{{root}}\inputs\` first, whole:** the labelling guide, the scheme, the boundary table, `note.schema.json`, `cases.schema.json` and `writing-rules.json`. The guide decides every label; its hard cases (section 4.2) and its slot rules (section 6) are where labels usually go wrong.

**First, invent your 25 cases** (or as many as your line says) to your batch's line, and write them to `{{root}}\plan\batch-{{batch}}.cases.json` in the format of `cases.schema.json`. Your line gives the counts: the claim types, the near-miss notes and what they resemble, the denied and disputed events, the slot values, the hard cases, the themes. You give each case its story: who the parties are, what happened, what is stated, denied, disputed (and by whom) or not mentioned, each slot's value, the requests, and how the note is written (its length, its order, how tidy it is, which of the four fields are used, how dates and amounts are written, what else is in it: a correction, an irrelevant detail, a second matter mentioned in passing). For a near-miss case, name the claim type it resembles and the one telling fact that puts it outside (the boundary table). Invent the cases from the way such disputes actually arise: no two of your cases may resemble each other or differ only in names and numbers.

**Then, for each case:**

1. **Write the note** (fields `subject`, `facts`, `requests`, `documents`), as that lawyer would jot it down: concrete, selective, uneven, in his own order. Follow the case's description of how the note is written. Say exactly what the case says:
   - a stated event is asserted; a denied event is said, by the note itself, not to have happened; a disputed event is reported as contested, with who contests it;
   - an event that is "not mentioned" is not stated, denied, disputed or hinted at, and no document named after it is listed;
   - a slot whose value is "unknown" cannot be answered from the note;
   - each date and amount appears with its value, in the format the case asks; plausible and consistent (paid plus owed equals the total; events in a possible order).
   Do not copy any sentence of the guide. Do not use label words as labels. Do not cite articles or explain the law. Vary your wording from note to note: these notes come from many different lawyers.

2. **Label the note you actually wrote**, by the guide, in the format of `note.schema.json`: `set` is `"{{set}}"`; `id` follows the rule of `writing-rules.json`; `provenance` and `status` are exactly as `writing-rules.json` gives them (with today's date); `gold` holds your labels: the claim types; **every** element of those types with its polarity, actor, who disputes, date and amount; every slot to label; the requests; every date and amount with its normalised form and what it is for. For every label except "not mentioned" and "unknown", give the **exact words of your note** that carry it (`field`, `quote`, and `occurrence` when the words repeat): the shortest stretch that carries the fact, copied letter for letter.

3. **Re-read the note cold,** as if you had not written it, and label it again in your head from the text alone. If the text supports a different label from the case's, or supports two, **rewrite the note** until a careful reader with the guide would give exactly your labels. Then check every quote is in the note, letter for letter. A second reader who sees only your note will label it; any difference between that reader and you removes the note from the set.

Write one JSON object per line, one line per note, to `{{root}}\notes\batch-{{batch}}.jsonl`.

When both files are written, reply with the two numbers only.
