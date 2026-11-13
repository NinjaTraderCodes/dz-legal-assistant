# How this handbook was checked, and what the check changed

*13 November 2026. Written by the handbook's author (Fable 5.2).*

**In one paragraph.** Before it was handed over, the handbook was read by nine independent agents and tested once on its labelling guide. They confirmed its facts about the app and about Codex's corpus, and they confirmed that the owner's decisions were all in it. They also found about 280 defects, many of them the same defect seen from several sides. Every one was either fixed, shown to be already fixed, or refused for a stated reason. **The handbook has still never been used**: the first module taken through step 1 is its real test.

## What was run

| Check | Who | What it was given | What came back |
|---|---|---|---|
| Six cold readings, one per part of the work | Opus 5.5, fresh agents, read-only | the handbook and nothing else; told to plan their first actions and list everything that would stop them | 179 findings (29 or 30 each) |
| A consistency critic | Opus 5.5 | every file; told to find where two files disagree or point to something that does not exist | 68 findings |
| A completeness critic | Opus 5.5 | the owner's list of what the handbook must contain, the six changes of 12 November, the three decisions of 13 November, and the brief | 7 serious and 42 light gaps |
| The guide test | Sonnet 5.5 and Codex, each alone | the labelling guide and 45 hard extracts, without the expected answers | both gave the expected answer on all 45 and agreed with each other on all 45; Sonnet marked six as unsure and listed eleven places where the guide left it to choose ([../notes/GUIDE-TEST-RESULTS.md](../notes/GUIDE-TEST-RESULTS.md)) |

Then four Opus 5.5 agents applied the fixes, each owning a separate set of files and working from one file of the author's decisions; the author fixed the main handbook, the contract, the schemas and the example records. In all: 13 agent runs and 2 Codex calls (the plan had said about 12 and 2).

## What the readers confirmed

- **The owner's decisions.** The six changes of 12 November and the three decisions of 13 November are each present where the owner asked for them (no serious gap; nine small ones, since closed).
- **The facts about the app** in the integration contract: the fields of a claim type, the export blockers and the one the draft mark lifts, the two severities, the JSON options. Two claims were wrong and are corrected: the app has no check for a conciliation record (only the formal-notice word list), and a record cannot be mapped onto the claim catalogue "without breaking anything" before the missing articles are in the library.
- **The facts about the law data**, checked by script: the last version is the one in force in all four codes of the corpus; the six CPCA articles that Law 22-13 repealed are still in the app's library, and they are exactly the articles the importer's filter gets wrong; the guard rule flags exactly Civil Code 18, 19 and 22 to 25; the library's CPCA and Family Code equal the corpus word for word; every quoted paragraph and hash in the two example records is right.
- **The four effects.** After the owner's decision of 13 November, no file keeps a fifth effect or treats "to check" as weaker than a warning.

## The defects that mattered, and what was done

**An "independent" agent could see what it must not.**
- The files every deriver had to read (the grade table, the slot list's help texts, the evaluator's provisional counting) stated things the derivers were supposed to find. → A neutral [deriver's guide](../rules/DERIVER-GUIDE.md) with a toy example that holds no law; the slots reach a deriver without their help texts.
- The prompts forbade some folders by name, and such lists always have holes (the reports, the boundary cases, Codex's checklists, the memory folder, other branches). → Every prompt now says which paths the agent may read and write; everything else is off limits.
- Every fresh agent is handed the user's memory index automatically. → Independent and isolated roles are forbidden to read or write memory; for hidden-set writers a script checks that the memory folder did not change.
- A Claude judge keeps its file tools, and nothing would show that it had looked at the scenario. → Judges, guide testers and reviewers get their inputs pasted in the prompt and use no tool; a run that reports any tool use is discarded, as a Codex call that ran a command is.
- The prompts used relative paths; an agent started in the main folder would have written its work into another branch (this check's own agents started there). → Every prompt that touches files opens by naming the worktree, with absolute paths.

**The mirror set was being used to decide things.** Three decisions (when to stop improving the model, whether composed notes stay, which notes train the shipped classifier) were taken on it in two specifications, while the handbook elsewhere forbade tuning on it. → The mirror set now chooses nothing: it is built after the generation method is frozen, in v3's proportions, and scored once for the final candidates.

**Formats that did not fit each other.**
- The reader's answer had no claim type on an event, though eight claim types share one event kind; a value had one role where the notes give several. → Both fixed in the contract.
- The writer's answer could not be turned into a note record (no way to point at an event's date or amount). → Fixed in the writer's prompt.
- A deriver was told to add a new slot and then to pass a check that refuses unknown slots. → A record now declares the slots it proposes.
- "Counting" was a rule id in the schema and a statement id in the examples. → It is now a small object the evaluator can run, derived from the texts.
- Codex's third derivation of a module could not be "mapped by script" onto a record. → It is kept as returned and compared on its mechanical fields only; nobody maps it by hand.
- A note could not hold a blanked field. → It can.

**Things nobody was assigned to do, or that could not be done as written.**
- The French editions that derivation B starts from. → Prepared in step 2's first pass.
- Re-readings of texts that already exist had no file to live in, so those texts could never count as verified. → `law/readings.jsonl`.
- One planner run could not write a plan of 1,375 cases for hidden v3. → The planner writes quotas per batch; each writer invents its own cases.
- The owner's v3 command was written for PowerShell 7, which this laptop does not have. → Windows PowerShell 5.1.
- There was no list of allowed tools and downloads, so "no download not listed" could not be applied; and no rule for model files, which are over GitHub's size limit. → Both in the hard rules (section 3).
- "Only new files", read literally, forbade edits the handbook itself asks for. → "Create and edit files only under the two folders."

**Things stated more favourably than the facts allow.**
- The example records were graded as if their texts had been verified. → Regraded with the real state: most of their items are grade D, so a declared block is shown as a warning marked "to check". This is also the clearest worked example of the owner's rule of 13 November.
- Notes composed by code were called licence-free while a Claude model was to write their phrases. → Their phrases come from the local model that wins the clean-notes trial.
- The step 3 budget of about 320 agent runs already assumed one of the savings (40 notes per judge run). → Said plainly: about 405 at 25 notes per run.
- **What hidden v3 can prove.** With 100 notes a 90 % target is "met" only from 96 right of 100; with 200 notes a 95 % target only from 197 of 200. Several targets will most likely come out "inconclusive" unless the reader is nearly perfect. This was always true of the sizes chosen; it is now written where the owner and the executor will see it before the run.

**A legal point the check surfaced.** Codex's corpus report records that Law 22-13 (art. 13) delays its new court rules until the new courts are installed. The article store now carries such notes, and the two court modules must answer that one in their checklist. It is a lead to be read on the Official Journal page, not a finding.

**The labelling guide.** Both testers agreed on every item, so no answer changed. The eleven places where one tester had to choose, and seven further edge sentences from a cold reader, became ten new hard cases (27 to 36) and several clarified rules. The four polarity rules of 6 November and the 75 propositions are unchanged. The test must run again on the changed guide before the label scheme is frozen, with new items written for the changed parts ([../prompts/notes-guide-items.md](../prompts/notes-guide-items.md)).

## What was not applied, and why

- **A lock on the hidden folders.** A reader proposed it; the owner had decided against it the same day. The protection is the no-open rule, Codex's empty scratch folder with a read-only sandbox, and the event-log check.
- **Extending the "critical use" re-reading to territory rules.** The decision names four kinds of item (deadline, prior step, section rule, exit); a territory rule never blocks.
- **Having the app's own cases relabelled for every slot.** They are scored with what their existing labels allow, and are too few to carry a verdict either way.
- **Letting a judge write its answer to a file.** A judge that uses no tool cannot; a truncated reply sends its notes back to the queue.
- **Three claim types per note, two-digit years, reasons for each threshold of step 5:** left as they were, each with its reason in the fixers' logs.

## What remains uncertain

1. The handbook is long (about 60 files). The check found contradictions between files and they were fixed, but a second consistency pass was not run after the fixes. The frame's tests, a link check (182 links, all resolving) and a search for known leftovers were.
2. Four agents edited separate files from one list of decisions. Where two of them had to agree on a detail (a script's name, a figure), the author aligned what the logs showed; a detail the logs did not show may still differ.
3. The clean notes hold no "outside the catalogue" case, because code cannot turn an English telling fact into Arabic without writing the wording itself. A classifier trained on clean notes alone would therefore not learn that class from them. The owner has been told.
4. The encoder timings are still a marked gap (the laptop was on battery).

The nine reports, the author's decisions and the four fixers' logs are kept outside the repository, in `C:\Users\TheShadow\dz-core-research\handbook-check\`.
