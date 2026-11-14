# Step 1: figures tracked for every module

The owner asked on 14 November 2026 that the step 1 report track, for every module, the A-B agreement rate before reconciliation and whether a deriver changed its own examples after running the tool, and state how many cites fell outside the chapters given to derivation C and the boundary writer. **The blind boundary cases stay the test that counts.**

This file is filled in as each module passes stage 4 and stage 9. The step 1 report takes its tables from it.

## Per module

| Module | A-B agreement before reconciliation (fields) | By kind | A changed its own examples | B changed its own examples | Blind boundary cases passed, before reconciliation | Cited paragraphs outside the texts given (A / B / both, of cited by A or B) | Chapter rule |
|---|---|---|---|---|---|---|---|
| `formal-notice` | 38 of 68 (56 %) | statements 32/44, prior steps 2/7, slots 4/16, elements 0/1 | yes: ex3, the expected questions and one finding (the means check also asks `notice_means` when `notice_served` is unknown); the rule was not changed | yes: ex4, the expected questions (same cause); the rule was not changed | A 3 of 12, B 4 of 12; 8 cases decided differently by A and B | 12 / 6 / 6 of 27 | the pointer's own chapters (before the owner's rule); under the rule of 14 November it would have been 11 / 5 / 5 |

**Reading the first row.** The low slot agreement (4 of 16) is mostly naming: A and B each proposed six or seven slots for the exemptions of Civil Code 181 under different ids, which the comparison cannot match and the reconciler maps. The boundary cases fail mostly where the writer marked an outcome `unsettled` (the texts do not settle it) and a record stays silent, and where an exemption has no slot yet. Final figures for the module come after stage 9.

## After reconciliation and verification

| Module | Blind boundary cases passed: A / B / reconciled / final candidate | Disputed by the reconciler | Verifier on those | Attacks: fixed / refuted / open | Grades (items) | Why D |
|---|---|---|---|---|---|---|
| `formal-notice` | 3 / 4 / 3 / 3 of 12 | 4 (bc3, bc4, bc7, bc9) | bc3, bc4, bc9: the case's expectation refuted (the exemption facts are unknown, so the text gives "ask", not "step missing"); bc7 open (both readings kept) | 25 / 15 / 7 of 47 | A 10, B 16, C 4, D 24 (first grading, before step 2's second pass) | 24 items cite one of 13 articles not yet verified: Civil Code 120, 121, 392, 469 مكرر 1, 480, 502, 507 مكرر, 507 مكرر 1, 553, 558 (corpus only: one fresh page reading each) and CPCA 310, 311, 406 (critical use: a fresh page reading) |

**On the blind cases.** The reconciler and the verifier are both Opus 5.5, and the verifier sided with the record on three of the four disputed cases. The other five failures are of one kind: a case names a paragraph, and every item citing that paragraph must meet the case's expectation, so an item that rightly stays silent (a step already taken) fails a case another item answers. Whether an `unsettled` or `ask` case should need every matched item or at least one to respond is a question about the boundary-case runner, not about this record, and is put to the owner.

## After the text pass, the runner change and the follow-up (14 November 2026, owner's decisions)

| Module | Blind boundary cases passed, final record: old runner / owner's runner | Still failing, and why | Second family on the disputed cases | Grades |
|---|---|---|---|---|
| `formal-notice` | 3 / 5 of 12 | bc3, bc4, bc9: the writer expects "step missing" where the exemption facts are unknown; the verifier (Opus) and Codex, blind, both answer "ask", so the case files' expectations are held wrong by two families (they stay unedited and counted as failures). bc7: open, both readings kept. bc6, bc8, bc12: the notice step says "done" once a notice was sent, while the boundary items n1, n11 and n12 ask or warn about its means or its date; under the owner's runner rule that silence contradicts the open case, while the follow-up verifier holds that the step answers a different question (was a notice sent?) | Codex: "ask" on bc3, bc4, bc9 (none becomes uncertain) | A 12, B 32, C 6, D 5: every D on Civil Code 120, whose printed single paragraph the store splits in two (the record cites 120 ¶2) |

## Case differences by run (before reconciliation)

| Module | A's examples on B | B's examples on A | Boundary cases on A | on B | decided differently by A and B |
|---|---|---|---|---|---|
| `formal-notice` | 12 | 8 | 9 | 8 | 8 |
