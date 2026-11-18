# Reconciliation: custody-forfeiture

Record: `rules/reconciled/custody-forfeiture.json` (stage `reconciled`, `verified: false`). `rules check --store` OK; `rules examples` 27 cases, 0 differences. No boundary-case file exists for this unit in `rules/boundary-cases/`, so `rules examples --boundary` was not run.

Counts: settled by the text 29, both right 19, not settled 23, needing a text 0. Boundary cases disputed: 0.

## Differences

difference x1: Settled by the text: civil-code 15 ¶1 says the law of the protected person governs the substantive conditions of guardianship "and the other systems for the protection of minors"; A's statement (b28) says that and is kept on the boundary note n10 for a foreign-element case.
difference x2: Settled by the text: civil-code 15 ¶2 applies Algerian law to urgent measures when the minor is in Algeria; A's statement b29 kept (also answers flag f-b28-p2).
difference x3: Settled by the text: civil-code 338 ¶1 calls the force of a final judgment "this presumption" that no evidence may rebut, following article 337 on legal presumptions; the role is B's `proof` (b21).
difference x4: Settled by the text: civil-code 338 ¶1 states in its words what the statement says (same parties, same capacities, same object and cause), so A's `interpretive: false` stands; applying it to a ground that arose after the custody judgment stays in boundary n2's message as a point to check.
difference x5: Settled by the text: civil-code 338 ¶2 "ولا يجوز للمحكمة أن تأخذ بهذه القرينة تلقائيا"; A's b22 kept on n2 and e1.
difference x6: Settled by the text: cpca 13 ¶1 requires standing and an interest recognised by law; B's statement kept as b31 (plaintiff's standing, doc7).
difference x7: Both are right: B's own cpca 15 ¶1 statement restated court-sections b27 (cpca 15 ¶1 and ¶2), which A imported; merged as the import of court-sections.b27.
difference x8: Not settled by the text: cpca 260 ¶6 lists "protection of persons lacking capacity" and does not name custody; kept as interpretive b30 with open point o6 (the protective reading: warn through p1, never block).
difference x9: Settled by the text: cpca 36 ¶1 makes only subject-matter incompetence a matter of public order, which answers k42; it is court-sections b28 word for word, so it is imported as court-sections.b28 instead of B's own copy.
difference x10: Settled by the text: cpca 40 ¶3 (item 2) sends custody claims to the court of the place where custody is exercised and ¶1 says "دون سواها"; A's b23 kept (territory note, o9).
difference x11: Settled by the text: cpca 419 ¶1 puts the costs on the losing party; B's statement kept as b42 for request r6.
difference x12: Both are right: A cited cpca 425 ¶1 and B ¶1 to ¶3 for the same inquiry power; ¶2 and ¶3 add the report and the counter-inquiry; merged as b19 citing ¶1, ¶2, ¶3.
difference x13: Both are right: same merge as x12 (b19).
difference x14: Settled by the text: cpca 439 ¶1 sits in the divorce procedure (branch "في الصلح" of "إجراءات الطلاق"); B's statement kept as b33, supporting k17 and k43 (no conciliation before a custody claim).
difference x15: Settled by the text: cpca 453 ¶1 prescribes how a request to end guardianship is made (by urgent action, by whom); the role is B's `procedure` (b26).
difference x16: Not settled by the text: cpca 454 ¶1 to ¶4 follow article 453 in the branch on guardianship over the minor's person; whether they reach a custody-forfeiture claim is a reading; kept as interpretive b38 with open point o14.
difference x17: Settled by the text: cpca 455 ¶1 governs the urgent order made under article 453; B's statement kept as b39 on boundary n7 (guardianship withdrawal).
difference x18: Settled by the text: cpca 458 ¶1 and ¶2 set the court and the chambers hearing for the article 453 request; kept as b40 on n7.
difference x19: Settled by the text: cpca 460 ¶2 lets the judge award custody provisionally inside the guardianship procedure; kept as b41 on boundary n6 (provisional measures).
difference x20: Settled by the text: cpca 47 ¶1 requires the plea of territorial incompetence before any defence on the merits; B's statement kept as b34 (k42, o9).
difference x21: Settled by the text: cpca 994 ¶1 excepts family cases from the mediation the judge must offer: an exclusion, so the role is B's `limit` (b27).
difference x22: Settled by the text: family-code 22 ¶1 proves a marriage by a civil-status extract or a judgment; B's statement kept as b37 (proof of e2, doc3).
difference x23: Settled by the text: family-code 67 ¶1 says in its words that custody falls when a condition of article 62 fails; the store has no French check on the article (`frenchCheck` null), so B's ground for `interpretive` is absent and A's `false` stands (b5).
difference x24: Not settled by the text: family-code 65 ¶1 says "the age of marriage" without a number and family-code 7 ¶1 sets capacity at 19 but lets the judge allow earlier; kept as interpretive b14 with open point o3.
difference x25: Not settled by the text: family-code 68 ¶1 states the year and the loss of the right but not its start, its counting or whether it binds a person seeking a transfer; A's d1 kept as the protective reading (interpretive, warn only, never blocks), B's readings added to d1 and to open point o1.
difference x26: Not settled by the text: family-code 65 ¶1 ends a boy's custody at ten with a possible extension to sixteen, and ¶2 requires the child's interest in ruling on the end; whether that ends the object of a transfer claim is a reading; d2 kept as interpretive warn-only, B's boundary n5 kept as n13, open point o15.
difference x27: Not settled by the text: as x24 and x26 for a girl; d3 kept as interpretive warn-only (o3).
difference x28: Not settled by the text: family-code 3 مكرر ¶1 (court-sections b34) makes the prosecutor a principal party but names no sanction and no one who brings it in; A's p1 kept as interpretive, warn when missing, ask when unknown (o6).
difference x29: Both are right: A's e1 and B's e1 are the same element (the earlier judgment awarding custody); merged with both events (custody_judgment, custody_held) and B's fact that the defendant still exercises custody.
difference x30: Both are right: A's e7 and B's e7 are the same element (the child's interest and the plaintiff's entitlement); merged with B's event custody_held.
difference x31: Both are right: same merge as x29.
difference x32: Both are right: same merge as x30.
difference x33: Settled by the text: cpca 419 ¶1; B's costs request kept as r6.
difference x34: Both are right: A's extra court-sections blocks (b27 on cpca 15, b58 on family-code 72) and B's (b42 on cpca 426, n5 on the family section) all apply to a custody claim; the import holds the union, plus b28 (x9).
difference x35: Settled by the text: civil-code 339 ¶1 (ordinary-court-exits b36) binds the civil judge to what a criminal judgment necessarily decided, which matters when unfitness rests on facts before a criminal court; the import is kept and A's boundary n5, which restated the module's exit x6, is replaced by the import of ordinary-court-exits.x6.
difference x36: Settled by the text: civil-code 3 ¶1 (periods b27) counts periods by the Gregorian calendar, which d1 to d3 need, so it is imported; civil-code 323 ¶1 (periods b42) speaks of a creditor proving an obligation, which a custody ground is not, so it is not imported and no element rests on it.
difference x37: Both are right: B's cause_predates_custody_judgment is the same fact as A's comparison of custody_ground_date with custody_judgment_date; mapped to A's dates (needed by d1 anyway), and n2 now also requires prior_custody_judgment = yes.
difference x38: Not settled by the text: child_birth_date serves only the interpretive d2 and d3 (o15, o3); kept for the protective warnings.
difference x39: Not settled by the text: child_sex serves only d2 and d3, as x38.
difference x40: Settled by the text: civil-code 339 ¶1 makes a criminal case on the same facts matter; criminal_case_on_same_facts is read by the imported exit ordinary-court-exits.x6.
difference x41: Both are right: custody_ground_date is half of the mapping of x37, and the start of d1.
difference x42: Both are right: custody_judgment_date is the other half of the mapping of x37.
difference x43: Not settled by the text: cpca 442 ¶1 and 445 ¶1 let the divorce judge take and amend provisional measures and family-code 57 مكرر ¶1 covers custody, but no text bars a separate claim; B's boundary kept as n11 (warn when a divorce action is pending), open point o13.
difference x44: Settled by the text: family-code 71 ¶1 returns the right to custody when the involuntary cause of its loss ceases; B's slot forfeiture_cause_ceased and boundary kept as n12.
difference x45: Settled by the text: family-code 66 ¶1 and 70 ¶1 tie the mahram condition to the marriage grounds only, and articles 66 to 70 set distinct grounds with distinct proof; A's forfeiture_ground kept (n3, doc3 to doc6).
difference x46: Settled by the text: family-code 66 ¶1 "بالتزوج بغير قريب محرم"; A's new_spouse_is_mahram kept (n3).
difference x47: Both are right: A's prior_custody_judgment and B's prior_custody_judgment_exists are the same fact; one id kept, prior_custody_judgment.
difference x48: Both are right: as x47.
difference x49: Not settled by the text: public_prosecutor_joined serves the interpretive p1 (o6).
difference x50: Settled by the text: family-code 72 ¶1 sets the housing duty "في حالة الطلاق"; B's spouses_divorced kept as r4's `when`, and r4's basis is court-sections.b58 (B had cited family-code 87 ¶3).
difference x51: Not settled by the text: A's clean case differs on B because B lacked d1 to d3, p1 and A's boundaries; the interpretive d1 to d3 and p1 are kept on the protective readings (o1, o3, o6, o15), the boundaries merged; the case now passes on the merged record with B's slots answered.
difference x52: Not settled by the text: the difference is d1 to d3 (o1, o15, o3); kept as warn-only.
difference x53: Not settled by the text: the difference is d1 (o1) and A's n3; d1 kept warn-only.
difference x54: Not settled by the text: the difference is d2 (o15); kept warn-only.
difference x55: Not settled by the text: the difference is d3 (o3); kept warn-only.
difference x56: Not settled by the text: the difference is p1 (o6); kept warn when missing.
difference x57: Not settled by the text: the difference is p1 asking (o6); kept.
difference x58: Both are right: the same boundary n1 under two slot ids; merged under prior_custody_judgment.
difference x59: Both are right: the same boundary n2 under two slot forms; merged (x37).
difference x60: Settled by the text: family-code 66 ¶1 and 70 ¶1 (mahram husband: no marriage ground); n3 kept, case passes (A's ex10, reconciler rx2 and rx5).
difference x61: Settled by the text: civil-code 339 ¶1; A's n5 is now the imported exit ordinary-court-exits.x6, which this stage's evaluator does not run (A's ex11 rewritten).
difference x62: Both are right: only the questions differed; the merged record asks both sides' slots.
difference x63: Not settled by the text: A's "nothing known" case differs by d1 to d3 and p1 (interpretive, kept) and by slot naming (merged); questions rewritten for the merged record.
difference x64: Not settled by the text: B's clean case differs on A by B's n3 (divorce pending, o13) and n4 (settled, x44) and slot naming; kept n11 and n12, mapped the rest.
difference x65: Not settled by the text: as x64.
difference x66: Both are right: n1 and n2 under two slot forms; merged.
difference x67: Both are right: as x66.
difference x68: Not settled by the text: B's n3 (divorce pending) has no counterpart in A; kept as n11 (o13).
difference x69: Settled by the text: family-code 71 ¶1; B's n4 kept as n12 (rx1).
difference x70: Both are right: n1 under two slot ids; merged.
difference x71: Not settled by the text: B's "nothing known" case differs by n3 (o13), n4 and slot naming; questions rewritten for the merged record.

## Proposed slots: mapping

| Fact | A | B | Kept |
|---|---|---|---|
| An earlier judgment awarded custody to the defendant | prior_custody_judgment | prior_custody_judgment_exists | prior_custody_judgment |
| The ground predates the earlier judgment | custody_ground_date `beforeSlot` custody_judgment_date | cause_predates_custody_judgment | A's two dates (n2 also requires prior_custody_judgment = yes) |
| The involuntary cause has ceased | (none) | forfeiture_cause_ceased | forfeiture_cause_ceased |
| The ground relied on | forfeiture_ground | (none) | forfeiture_ground |
| The new husband is a mahram | new_spouse_is_mahram | (none) | new_spouse_is_mahram |
| The child's birth date and sex | child_birth_date, child_sex | (none) | both |
| The prosecutor made a party | public_prosecutor_joined | (none) | public_prosecutor_joined |

Existing slots kept: court_has_section_for_matter (both), criminal_case_on_same_facts (A), divorce_action_pending and spouses_divorced (B); obligation_is_tort_compensation and damage_caused_by_offence are listed because the imported exit x6 reads them.

## Item ids in the reconciled record

Statements keep A's ids b1 to b29 (B's matches: b3→b1, b4→b2, b11→b3, b1→b4, b2→b5, b5→b6, b6→b7, b7→b8, b8→b9, b9→b10, b10→b11, b12, b13, b15→b16, b14→b15, b16→b17, b17→b18, b23→b19, b24→b20, b26→b21, b27→b24, b28→b25, b18→b26, b32→b27). B-only statements: b30 (260), b31 (13), b33 (439), b34 (47), b25→b37 (FC 22), b19→b38 (454), b20→b39 (455), b21→b40 (458), b22→b41 (460), b29→b42 (419). B's b35 and b36 are replaced by court-sections.b28 and court-sections.b27. Added by the reconciler: b43 (cpca 323 ¶2, flags f-b24-p2 and f-b27-p2), b44 (cpca 442 ¶1), b45 (cpca 445 ¶1). Boundaries: A's n1 to n4, n6 to n10 keep their ids (B's n1→n1, n2→n2, n6→n4, n7→n6); A's n5 is replaced by ordinary-court-exits.x6; B's n3→n11, n4→n12, n5→n13. Documents: A's ids (B's doc1→doc1, doc2→doc3, doc3→doc6, doc4→doc4, doc5→doc5, doc6→doc7 with doc2). Elements and requests keep their ids; r6 is B's. Examples: A's ex1 to ex13, B's as bx1 to bx8, reconciler's rx1 to rx6.

## Differences between this unit and the modules

- ordinary-court-exits x6 replaces A's boundary n5. A's n5 fired only on criminal_case_on_same_facts = yes; x6 also asks obligation_is_tort_compensation and damage_caused_by_offence when no criminal case is known, and its message speaks of the route for a compensation claim. For a custody claim neither fits: one more check of the module (its second branch and its message are written for compensation claims). The exit is not run by `rules examples` at this stage, so A's ex11 now tests only that nothing of the claim's own fires.
- B's statements on cpca 36 ¶1 and cpca 15 ¶1 were the same as court-sections b28 and b27 (the module cites cpca 15 ¶1 and ¶2): imported by id.
- periods b42 (civil-code 323 ¶1) was used by A as the general burden of proof; its words concern a creditor and an obligation, so it is not imported here. The proof module, when it exists, should say which text carries the burden in status claims.

## Boundary cases

None: there is no `rules/boundary-cases/custody-forfeiture.json` and no other boundary-case file names this unit.

## What I changed where A and B had agreed

- r4 (housing): B's `when` spouses_divorced = yes kept, and the basis corrected to court-sections.b58 (family-code 72 ¶1); B had cited family-code 87 ¶3, which is on guardianship.
- r2 (guardianship): B's basis cpca 453 dropped (it concerns ending guardianship, not granting it); family-code 87 ¶1 and ¶3 kept; o4 stays.
- r5 (provisional execution): cpca 323 ¶2 (b43) added to the basis; it names maintenance and the marital home for the custodian, not custody (o5).
- Elements e1 to e7: periods.b42 removed from every basis and proof (x36); e2's proof now rests on family-code 22 ¶1 (b37); e4 and e7 also cite cpca 454 as an interpretive proof means.
- Documents: doc3 is now the civil-status extract or the judgment establishing the marriage (family-code 22 ¶1), not "the marriage contract"; doc6 covers the inquiry reports of B's doc3; doc7 takes B's standing basis (cpca 13 ¶1).
- Boundary n2 now requires prior_custody_judgment = yes, so a claim without an earlier judgment is not asked for its date (A's ex9 now states the judgment; rx3, rx4).
- Re-read with neighbours and left unchanged: the section rule (cpca 423 ¶3, 32 ¶5), territory t4 (cpca 426 ¶5, consistent with cpca 40 ¶3), r3 visitation (family-code 64 ¶1, last clause), r1.

Examples corrected: A's ex1, ex2 to ex5 and ex13 now answer B's slots (and x6's are not referenced); A's ex11 no longer refers to the removed n5; A's ex9 states prior_custody_judgment = yes; B's examples were mapped to the kept slot ids and given A's slots. No derivation's expectation was contrary to the text.

## Questions left to the adversary

1. d1: does family-code 68 ¶1 bind a person seeking a transfer at all, and from which event? The record warns from the ground's date (earliest reading) and never blocks.
2. civil-code 338 ¶2 says the court does not apply res judicata of its own motion, while cpca 67 lists res judicata among pleas of inadmissibility and cpca 69 makes the judge raise those of public order; is a custody judgment (status of a child) such a case? Not cited in the record; n2 only warns.
3. d2 warns for any boy past ten, although family-code 65 ¶1 allows an extension to sixteen when the custodian is a mother who has not remarried; is a separate slot needed?
4. Is cpca 454 usable in this claim (o14), and is a custody claim a "protection of persons lacking capacity" case under cpca 260 ¶6 (o6)?
5. The imported exit x6's tort questions in a custody claim (above).
