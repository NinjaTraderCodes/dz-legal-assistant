# Planner of hidden set v3 (and of the mirror set)

- **Model:** Sonnet 5.5, **a fresh agent used only for this** (never a fork: a fork would carry the builder's context). One run per set. Specification: [../specs/hidden-v3-commands.md](../specs/hidden-v3-commands.md), sections 3, 4 and 6.
- **Route:** the Workflow tool with a reply schema of two integers, `{"batches": integer, "notes": integer}`, if the owner opted in (stop point "A workflow"); otherwise the Agent tool and the discard rule below. Either way the owner must first have allowed agents, in the owner's own Claude Code settings, to read and write under the set's root; no folder's access rights change.
- **Placeholders:** `{{root}}`: `C:\dz-hidden-v3` for v3; `C:\Users\TheShadow\dz-core-research\mirror` for the mirror set (the same prompt, the same role: that is what makes the mirror a rehearsal). `{{set}}`: `hiddenV3` or `mirror`. The number of batches and their size are in `{{root}}\inputs\quotas.json` (about 55 × 25 for v3; about 22 × 25 for the mirror).
- **What it may read:** only `{{root}}\inputs\` (put there by `v3 init`): `guide.md`, `scheme.md`, `boundaries.md`, `quotas.json`, `design.schema.json`. **It is given nothing about the sampler, the style cards, the training notes or the classifier.** What it may write: only `{{root}}\plan\design.json`.
- **What it writes: quotas, not cases.** One short line per batch: how many notes of which claim types, the near-miss notes and what they resemble, the denied and disputed events and on which elements, the slot values, the hard cases of the guide, a few themes. Each writer then invents its own 25 cases to its line. Formats: the specification, section 4.
- **Reply:** the two integers only. **Any other reply** (a description, an excerpt, an apology, an explanation): stop; do not answer the agent; run `v3 discard --plan`; tell the owner in one line without repeating the text; run the planner again, fresh.
- **After the run:** `v3 memory compare` (on `memory changed: yes`, stop and tell the owner; never open the memory folder), then `v3 validate --plan`.
- **Why its own plan:** a hidden set drawn by the builder's sampler would share the sampler's blind spots, and the classifier would look better on it than on real notes. The planner sets the quotas per batch from the guide and the law's boundaries alone, and the writers invent the cases.

--- PROMPT ---

**Where you work.** Your root folder is `{{root}}`. You may read and write only inside it, and nothing else on this computer. Your shell may start elsewhere: use absolute paths under `{{root}}`, and never write anywhere else.

You are planning a **hidden test set** of working notes that Algerian lawyers write before drafting a statement of claim. The set will measure a program that reads such notes. Its value depends on one thing: **nobody outside this task ever sees its contents.** That includes the agent that started you.

**Hard rules, without exception:**

1. Your final reply is exactly this and nothing else: `{"batches": <number of batch lines you wrote>, "notes": <the sum of their notes>}`. No description, no example, no excerpt, no comment, no apology, no summary. If something goes wrong, reply `{"batches": 0, "notes": 0}` and nothing else.
2. Read only these files: `{{root}}\inputs\guide.md`, `scheme.md`, `boundaries.md`, `quotas.json`, `design.schema.json`. Write only `{{root}}\plan\design.json`.
3. Everything else is off limits, and in particular: any repository or worktree; any memory file (read none, write none, save no memory); the files under `C:\Users\TheShadow\.claude\`, including any plan file; the state file `C:\Users\TheShadow\dz-core-research\STATE.md` and everything under `C:\Users\TheShadow\dz-core-research\` that is not inside `{{root}}`; every other folder whose name begins with `C:\dz-hidden`. Search nothing; fetch nothing from the web; run no command other than reading and writing those files.
4. Everything is fictitious.

**Read the five input files first, whole:** the labelling guide (how facts in a note are labelled, with its table of hard cases in section 4.2), the scheme (the 21 claim types with their elements and requests; the slots to label and their values; the table of element × polarity cells a lawyer would still write a note for), the boundary table (for each claim type, the cases that look like it and are not), the quotas, and the format of your file.

**Then write `{{root}}\plan\design.json`** in the format of `design.schema.json`, with `"set": "{{set}}"`: one line per batch, as many batches as `quotas.json` says, each of the batch size it gives. A line says, for its batch: how many notes of each claim type; which notes carry two claim types; how many near-miss notes outside the catalogue and which claim type each resembles; how many denied and how many disputed events, on which elements; which slot values, how many times; which hard cases of the guide (by row number); and two or three themes in a few English words. **Nothing else: no case, no story, no Arabic sentence.**

**Meet the first-round quotas of `quotas.json`, all of them together:**

- every claim type at least its quota of notes, and the extra labour notes on top of it, on the claim types the file names;
- the near-miss notes spread over all 21 claim types, using the boundary table;
- at least the required numbers of denied and of disputed events, each spread over at least the required number of different elements, and only on cells that the scheme's table allows;
- every slot with each of its values somewhere, and with a value other than "unknown" at least the required number of times;
- about one note in eight with two claim types;
- every hard case of the guide's table in several batches, and no batch made only of hard cases.

Check your arithmetic before you finish: each line's numbers add up to its batch size, and the lines together reach every quota.

**Make it a real test.** Spread the difficulties: no batch should be all of one kind, and two batches should not have the same line. Choose themes that vary who the parties are and what is at stake.

When `design.json` is written, count its lines and their notes and reply with the two numbers only.
