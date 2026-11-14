# The check of the revised handbook (night of 14 November 2026)

**What was checked.** The revision of 14 November (commit `c528a4d`: the reader first, Codex in three roles) was read cold by ten fresh Opus 5.5 agents, read-only, none of which had seen the planning. Eight played the executor, one per part of the work, and listed every point where they would have had to guess or ask. Two were critics: one looked only for contradictions between files, the other checked the handbook line by line against the owner's own list of requirements and decisions.

**One model family.** All ten readers are Claude models, like the planner. They catch what is missing, unclear or contradictory on the page. They do not catch what every Claude model would get wrong in the same way, and they ran nothing: the first unit through the driver and the pilot remain the real test.

## What they found

| Reader | Findings | Marked blocking |
|---|---:|---:|
| Every session and phase 0 | 25 | 3 |
| Phase A: scheme, scorer, parsers | 30 | 3 |
| Phase A: notes code, pilot, filter trial | 30 | 3 |
| Rules track (a unit through the driver; planted errors) | 30 | 12 |
| Phase B: sets, training, learning curve | 28 | 6 |
| Phase C: the reader finished; step 5 | 30 | 2 |
| Phase D: slots; deferred law work | 30 | 6 |
| Phase E: mirror, hidden v3, freeze | 28 | 4 |
| Critic: contradictions between files | 29 | 3 |
| Critic: against the owner's list | 10 groups, and a verdict on each of 20 lines | – |

About 270 findings, about 40 of them marked blocking. Many were the same gap seen from two sides. The owner's-list critic found 12 of 20 lines met, 8 met in part, none contrary except one: the handbook let an agent start in a session that still held the API keys, which the owner had ruled out.

## The gaps that mattered most, and what was done

1. **Phase B could not start**: the reader's heads train on encoder outputs that only the .NET front end can make, and that front end was scheduled for phase C. It moved to phase A ("The front end" in the reader's specification), with its own "done when".
2. **Claim types could not go through the driver**: the prompt filler, the texts command and the French sources worked for a module only; follow-up verifier runs had no file names and would have overwritten earlier fixes; a value mismatch never reached the verifier; a build replaced the rules tool under running agents. All of this is now written down as the retool list of `specs/unit-driver.md` (13 items, each with its tests), with a published copy of the tool, follow-up files, two more sources of findings, and a stage for the French sources.
3. **The planted-error rounds could not be scored as written**: two kinds stop the chain before any agent; the copies collided with the real reports. They now have their own script-only copies, a root folder for every path, one Codex call per copy, each kind defined as a change of a named field, and a fixed shape for the rates file.
4. **"Tuned only on the Codex half" had holes**: three files named three different sets; the remedies for the style gap were triggered and judged by the Sonnet half; the tuning score could be computed several ways. There is now one pooled set of tune notes, one formula (in the scorer, under its hash), a rule that the style gap decides nothing, and the rule in every decision row that chooses something.
5. **"No Claude model touches a training note" had holes**: a realism review read the training set and fed the style cards; the executor could tighten the filter on Opus's findings. Realism reviews now read test sets only; the list of who may read a training note is short and closed; the 5 to 10 % rule changes the sheet only for notes filtered afterwards, and the terms file says so.
6. **The filter's sheet could not be built for every note** (five claim types have fewer than three neighbours; two-claim notes had one "right" answer), and it did not check the role or what a date is for. The claim question is now a yes or no per claim type; the sheet asks the role and each value's role.
7. **The noise figure was not defined** well enough for two people to get the same number. It now has a field list, a rule for what counts as wrong, and a per-note figure beside it.
8. **Hidden v3**: the permission was asked after the mirror needed it (now two stop points); the writer never labelled the role that the comparison checks; the run had no command that reads a whole set without printing; the probe bar's arithmetic was wrong; the risk that a relabeller opens the writers' labels was understated. Each is corrected, and the remaining risk is said plainly, with what narrows it.
9. **The owner's words**: the blocking rule's last sentence had been cut; it is quoted whole. The API-key rule is now a gate before any agent launch, and the wrapper that starts the editor was broken (it could not start a `.cmd`): it is fixed, with a test.
10. **Figures**: the budget of runs, calls and laptop nights was recomputed from the detailed design and is higher than the plan's; the handbook's section 9 says by how much and why.

## What was left, and why

- **Specified, not built.** The retool list, the commands of the notes tool, the reader tool, the tools of phase D and of hidden v3 are specifications with their tests named. Writing them is the executor's work.
- **Not addressed, small:** where `merge-checklists --november` finds its file; `store.py` clearing a guard flag after a guard reading; the scorer's sentence "reads nothing else" beside the two files it does read on a hidden run, and the exact counts of its size-table test; how to keep Codex CLI from updating itself (the rule stands: do not update, and tell the owner if the version changes).
- **Known limits, written into the files:** the two labour laws have no chapter data, so the "every chapter" rule gives their units only the articles the store holds; nobody has timed a local model writing or filtering a whole note; whether a v3 relabeller opened another file cannot be checked.
- **History, not edited:** `reports/step-2a.md` and `reports/handbook-check.md` describe earlier states.

**The fixes themselves were not read cold again.** They changed about 45 files in one pass. The mechanical checks were rerun after them (no dangling link; every prompt's body names every key and closed value of its schema; the three relabel prompts share one labelling text; the scripts' tests and the research solution's tests pass).
