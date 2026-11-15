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

## Rows written by the driver (unit.py tracking)

Every check behind these rows except Codex's support check was made by Claude models (HANDBOOK, section 5b). The proof label `mechanical` counts the items whose texts were proved mechanically; the other two count the items' own `proof`.

| Unit | A-B agreement before reconciliation | Boundary cases passed: A / B / reconciled / final | Still failing (first reason) | Cited outside the texts given (A / B / both) | Attacks: fixed / refuted / open | Grades | Proof labels (twoFamilies / mechanical / oneFamily) |
|---|---|---|---|---|---|---|---|
| `formal-notice` | 38 of 68 | 3 / 4 / 3 / 8 of 12 | bc3: p1: declared ask, the outcome StepMissing needs warn (p1: notice_served no, exemptions unk; bc4: p2: declared ask, the outcome StepMissing needs warn (p2: notice_served no, exemptions unk; bc7: p2: declared none, the outcome StepMissing needs warn (p2: requiredWhen false -> none; gra; bc9: [civil-code 120 ¶2] matches no item: a rule the record may have missed | – | 32 / 24 / 11 | B 49, C 7 | 45 / 12 / 11 |
| `periods` | 97 of 133 | 9 / 9 / 7 / 7 of 18 | bc2: d1: declared none without a last day, the outcome InTime needs one (d1: when false -> none; bc5: d1: last day none, the case says 2026-02-28; bc6: d8: declared none without a last day, the outcome Late needs one (d8: when false -> none; ; bc7: d8: declared none without a last day, the outcome Late needs one (d8: when false -> none; ; bc8: d4: last day 2026-06-15, the case says 2022-06-15; bc9: d2: last day none, the case says 2026-06-12; bc10: d2: last day none, the case says 2026-07-05; bc11: d1: last day none, the case says 2026-03-01; bc12: d1: last day none, the case says 2026-03-01; bc16: d1: declared ask without a last day, the outcome InTime needs one (d1: when unknown, asks ; bc18: d1: declared none without a last day, the outcome InTime needs one (d1: when false -> none | – | 18 / 32 / 10 | B 41, C 3, D 12 | 38 / 1 / 18 |
| `court-sections` | 25 of 90 | 0 / 2 / 4 / 6 of 13 | bc1: [cpca 536 مكرر 4 ¶3] matches no PriorStep (it matches x2); bc2: [cpca 536 مكرر 4 ¶3] matches no PriorStep (it matches x2); bc4: [cpca 536 مكرر 4 ¶3] matches no PriorStep (it matches x2); bc5: [cpca 536 مكرر 4 ¶3] matches no PriorStep (it matches x2); bc7: x2: declared ask, the outcome LeavesOrdinaryCourt needs warn (x2: when unknown, asks speci; bc8: [cpca 439 ¶1] matches no item: a rule the record may have missed; bc9: [cpca 423 ¶3] matches no PriorStep (it matches n5) | – | 34 / 38 / 4 | B 67, C 4, D 6 | 37 / 0 / 40 |
| `ordinary-court-exits` | 41 of 130 | 3 / 2 / 9 / 9 of 10 | bc9: x4: declared ask, the outcome LeavesOrdinaryCourt needs warn (x4: when unknown, asks speci | – | 24 / 26 / 7 | B 55, C 8, D 24 | 31 / 0 / 56 |
