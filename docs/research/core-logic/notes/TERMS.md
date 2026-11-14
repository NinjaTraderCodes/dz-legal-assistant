# Terms: who may write, filter and train on which notes

**What this file is.** The record of the owner's decision of 14 November 2026 on the providers' terms, with the one clause that was read and quoted, and a plain statement of what was **not** read. It is not legal advice, and nobody qualified has reviewed it.

## The owner's decision (14 November 2026)

In the owner's words:

> "Codex writes the training notes, on my own reading that a small offline classifier for Algerian legal notes does not compete with OpenAI. I take responsibility for that reading."

> "The filter on training notes is not Claude: code checks and a local model (Gemma 4 E4B or Jais-3-3.7B) decide keep or drop; Opus only measures label noise on a sample and stops to show me if it passes 10 %."

> "Claude (Opus or Sonnet) writes test notes, the mirror set and hidden v3, never training notes, unless Anthropic authorizes it."

> "Keep the writer of every note recorded, so the Codex notes can be replaced if ever needed."

The owner's ChatGPT plan, used through Codex CLI, is a **personal** plan.

## What was read, and what was not

**OpenAI's terms were not quoted, because they could not be read.** On 14 November 2026 OpenAI's own page refused the fetch (HTTP 403), and the owner then said to stop looking. No clause of OpenAI's terms is quoted or paraphrased here. The decision that Codex writes the training notes rests on **the owner's own reading**, stated above, and on nothing in this file.

**Anthropic's terms were read on Anthropic's own site on 14 November 2026.**

- Usage Policy (<https://www.anthropic.com/legal/aup>, effective 12 November 2026, for all users), under "Do Not Abuse Our Platform", forbids to: «Use outputs to train an AI model (e.g., "model scraping" or "model distillation") without prior authorization from Anthropic».
- Consumer Terms (<https://www.anthropic.com/legal/consumer-terms>, effective 8 October 2025), section 3, forbid using the services: «To develop any products or services that compete with our Services, including to develop or train any artificial intelligence or machine learning algorithms or models».

The first clause is why no Claude output enters the training of the reader. No authorization from Anthropic has been asked for or given.

**The local models.** Gemma 4 E4B and Jais-3-3.7B are both under the Apache 2.0 licence (read for Part 7 of the architecture review, 13 November 2026). The two encoders the reader starts from are under Apache 2.0 (HANDBOOK section 3).

## The rule that follows, for every note

| Use | Codex (OpenAI) | Claude (Anthropic) | Local models | Code |
|---|---|---|---|---|
| **Write a training note** | yes (the owner's decision) | **never** | yes | yes (composed notes) |
| **Decide whether a training note is kept, or blank one of its fields** | no (not one of Codex's three roles) | **never** | yes (the filter) | yes (the code checks) |
| **Label a training note** | no: the labels are the code-made scenario's | **never** | no: the filter only removes | yes (the scenario) |
| **Measure a sample of training notes, changing nothing** | no | yes (the noise measurement, the sorting of dropped notes) | – | – |
| **Write a test note** (dev, challenge, mirror, hidden v3) | yes (half of dev and of the challenge set) | yes | yes (the clean test split) | never (composed notes are never test notes) |
| **Relabel a test note blind, and so decide whether it is kept** | no | yes | no | – |

"Measure, changing nothing" is meant strictly: a training note that the noise measurement finds wrong is **neither corrected nor dropped because of it**. The measurement can only stop the work or make the owner change the filter.

## How the rule is enforced

1. **Provenance on every note** ([note.schema.json](note.schema.json)): the writer (family, model, date, call), the filter (family, model, version), the relabellers, the scenario, the scheme version, `trainingAllowed`.
2. **`trainingAllowed` is true only when** the writer's family is `openai`, `local` or `code`, the filter's family is `local` or `code`, and the note is in no test set. It is false for every note a Claude model wrote.
3. **The loader** ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), "The loader") refuses, for training, a note with `trainingAllowed: false`, a note whose writer's family is `anthropic`, and a note without provenance. A test proves each refusal.
4. **Replacing a writer.** `notes stats --by-writer` counts the kept notes per writer family and model. Training takes `--exclude-writer <family>`: with `openai` excluded, the reader is trained on the clean and composed notes alone, which is the owner's fallback of 12 November 2026 ("plan for the shipped classifier to be trained on clean notes only"). The comparison "clean against Codex-trained" ([../specs/reader.md](../specs/reader.md), rung 7) measures what that would cost.

## If something changes

- **OpenAI's terms are read later and forbid the use:** the Codex-written notes leave training with one flag (point 4), and stay as development and test material. Tell the owner the measured cost.
- **Anthropic authorizes training on outputs:** the owner decides whether Claude-written notes may then be trained on; until the owner says so, nothing changes.
- **The Codex training backup** (HANDBOOK section 4) would have Codex write training code, not notes: it changes nothing here.
