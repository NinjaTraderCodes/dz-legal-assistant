# Reconciliation: abusive-dismissal

Record: `rules/reconciled/abusive-dismissal.json` (stage `reconciled`, `verified: false`). `rules check` with the store: OK. `rules examples`: 17 cases, 0 differences. No boundary-case file exists for this unit (`rules/boundary-cases/abusive-dismissal.json` is absent and no boundary-case file names the claim), so the `--boundary` run could not be made.

Id map used below: own statements b1 labour-relations 1 ¶1; b2 73 ¶1; b3 73 ¶2-9; b4 73-1 ¶1; b5 73-2 ¶1; b6 73-2 ¶2; b7 73-3 ¶1; b8 73-4 ¶1; b9 73-4 ¶2; b10 73-4 ¶3; b11 73-4 ¶4; b12 cpca 13 ¶1; b13 cpca 15 ¶1-7; b14 civil-code 124 ¶1 (interpretive). Elements e1 relation, e2 dismissal, e3 breach (A e3 = B e4), e4 conciliation (A e4 = B e3). Requests r1 reinstatement, r2 six-month compensation, r3 further damages (A r3), r4 annulment, r5 procedural compensation (A r5 = B r3), r6 certificates and pay slips (B r6).

## Differences

difference x1: Not settled by the text: labour-relations 73-4 ¶3 reserves «التعويضات المحتملة» without naming their ground; that civil-code 124 ¶1 is that ground is a reading. Kept as b14 (interpretive), referenced only by open point o6 with both readings; no rule rests on it.
difference x2: Both are right: B's b16 and A's periods.b42 cite the same paragraph (civil-code 323 ¶1); merged into the import periods.b42 (a module statement on proof, imported by id).
difference x3: Settled by the text: cpca 13 ¶1 conditions every claim on standing and an interest the law recognises; kept as b12 (B), basis of e1.
difference x4: Settled by the text: cpca 15 ¶1 requires the listed particulars (¶2-¶7) on pain of formal inadmissibility; kept as b13 (B). No final petition-formal-elements record exists to import from; ¶1-¶2 overlap court-sections.b27.
difference x5: Settled by the text: cpca 503 ¶1 (claim before the social section by statement of claim) supports B's statement; it restates labour-prelitigation.b34, so it is imported by id instead.
difference x6: Both are right: B's b15 (labour-disputes 21 ¶1-2) and A's labour-prelitigation.b41 (21 ¶1-3, basis of A's n2) cite the same paragraphs; merged into the import labour-prelitigation.b41.
difference x7: Both are right: B's b13 (labour-disputes 21 ¶1, ¶3) is inside A's labour-prelitigation.b41 (21 ¶1-3); merged into that import, which now grounds r6 and doc3.
difference x8: Settled by the text: labour-disputes 22 ¶5 lets the court order provisional execution without bond beyond six months; B's b14 restates labour-prelitigation.b58, imported by id.
difference x9: Settled by the text: labour-relations 1 ¶1 makes the law govern individual relations between salaried workers and employers; kept as b1 (A), basis of e1.
difference x10: Both are right: B's b1 and A's labour-prelitigation.b48 cite labour-relations 2 ¶1; merged into the import labour-prelitigation.b48.
difference x11: Settled by the text: labour-relations 73 ¶1 «يتم التسريح التأديبي في حالة ارتكاب العامل أخطاء جسيمة» says exactly what both statements say; the store holds it as the law-91-29 version in force, verified, with no French check against it, so the statement is not a reading (A: not interpretive). B's open point o5 on the 1990 French wording is dropped for the same reason.
difference x12: Settled by the text: 73 ¶2 ends «الأفعال الآتية:» and the acts are ¶3-¶9 (a list item is a paragraph); A's cite ¶2-¶9 carries its statement. Kept as b3.
difference x13: Settled by the text: same paragraph as x12; B's says names refusal of instructions, disclosure, violence, damage and alcohol, which are only in ¶3-¶9, so B's cite of ¶2 alone does not carry it. A's cite kept.
difference x14: Settled by the text: 73-1 ¶1 states what the employer must weigh when it describes the fault, and 73-3 ¶1 puts on the employer the proof that the dismissal was lawful; the paragraph sets no step of a procedure (those are 73-2 ¶1-¶2). Role `proof` (A).
difference x15: Settled by the text: 73-2 ¶1 binds the employer in pronouncing the dismissal; it is no step the worker takes before filing (that step is labour-disputes 19 ¶1, imported p1). Role `procedure` (A).
difference x16: Settled by the text: 73-2 ¶2 fixes the content of the employer's procedure (written notification, hearing); same reason as x15. Role `procedure` (A).
difference x17: Both are right: A's d1 (fifteen years, civil-code 308 ¶1 / 133 ¶1) restates the periods module, which B imported (periods.d1); merged by importing periods.d1 and periods.d2 (the two fifteen-year readings A kept), start event per civil-code 315 ¶1 (debt_due_date) or 133 ¶1 (harmful_act_date). A's one-year reading (312 ¶3) is kept in open point o1.
difference x18: Both are right: A's e3 and B's e4 are the same element (the dismissal breached 73 or the procedure; employer proves the contrary under 73-3 ¶1); merged as e3.
difference x19: Both are right: same as x18.
difference x20: Both are right: A's r5 and B's r3 are the same accessory request for the 73-4 ¶1 compensation; merged as r5.
difference x21: Settled by the text: labour-disputes 22 ¶5 supports B's provisional-execution request; it restates labour-prelitigation.r2, imported by id.
difference x22: Settled by the text: labour-disputes 21 ¶3 makes delivery of work certificates and pay slips a claim the social court decides (cpca 510 ¶1 admits it as an additional request); kept as r6 (B).
difference x23: Settled by the text: 73-4 ¶1 makes annulment of the decision the court's primary ruling for a procedural breach, the order and the compensation following it; r4 is not accessory (A), r5 is.
difference x24: Both are right: every block either side imported from court-sections exists and bears on this claim (A's n7 on civil servants; B's b3, b5 on listing and referral, b13, b15 on composition, b46 on additional requests, n1); the union is imported.
difference x25: Both are right: the common blocks plus A's statements (b1-b4, b7, b21-b28, b41, b48, b51, b55, n4) and B's (b12, b15, b46) all exist and are used by elements or checklist lines; union imported, with b34, b58 and r2 from x5, x8, x21.
difference x26: Not settled by the text: cpca 536 مكرر ¶4 gives judicial settlement and bankruptcy to the specialised commercial court (from its installation, law-22-13 13 ¶1), while cpca 500 ¶3 gives termination of employment contracts exclusively to the social section and labour-disputes 19 ¶2 only makes conciliation optional; protective reading: import ordinary-court-exits.x5 (interpretive, warns), keep the social section as proposal, open point o3.
difference x27: Both are right: A's periods statements and wage limitations (d3, d4, which act only when wages are requested) and B's d1 and doc3 all exist; union imported, plus d2 (x17).
difference x28: Both are right: claim_is_periodic_renewable_right is read by periods.d1, which both relied on (A restated it); the slot comes with the import.
difference x29: Both are right: claim_is_professional_fees, read by periods.d1 (as x28).
difference x30: Both are right: claim_is_trader_supply_or_hotel, read by periods.d1 (as x28).
difference x31: Both are right: conciliation_agreement_reached is read by labour-prelitigation.n1, which both imported; A did not list it in `slots`.
difference x32: Both are right: court_has_section_for_matter is read by court-sections.x3, which both imported.
difference x33: Both are right: criminal_case_on_same_facts is read by ordinary-court-exits.x6, which both imported.
difference x34: Both are right: damage_caused_by_offence, read by ordinary-court-exits.x6 (as x33).
difference x35: Both are right: debt_due_date is the start of periods.d1 (civil-code 315 ¶1, imported periods.b12), replacing A's dismissal_date start; follows x17.
difference x36: Both are right: defendant_resides_abroad is read by labour-prelitigation.p1, which both imported.
difference x37: Settled by the text: 73-2 ¶2 requires the procedure to provide for written notification, and 73-4 ¶1 attaches its own remedy to a procedural breach, so whether notification was done decides which request fits; slot kept (B), read by n3, e2, e3.
difference x38: Both are right: earlier_nonconciliation_record is read by labour-prelitigation.d3, which both imported.
difference x39: Both are right: earlier_nonconciliation_record_delivered_on, start of labour-prelitigation.d3 (as x38).
difference x40: Both are right: employer_in_bankruptcy_or_settlement is read by labour-prelitigation.p1 (both) and by the imported x5 (x26).
difference x41: Settled by the text: no paragraph in the store attaches any effect to the date the work began (labour-relations 2 ¶1 defines the worker without a date; 73-4 ¶1 and ¶3 measure compensation on pay, not seniority); B's proposed slot employment_start_date is not kept (A), the date stays a fact to state in e1.
difference x42: Both are right: forfeiture_force_majeure_invoked is read by labour-prelitigation.d1 and d2, which both imported.
difference x43: Both are right: forfeiture_lifted_by_order, read by labour-prelitigation.d1-d3 (as x42).
difference x44: Both are right: internal_grievance_exhausted is read by labour-prelitigation.p2, which both imported.
difference x45: Both are right: obligation_is_tort_compensation is read by periods.d1/d2 (x17) and by ordinary-court-exits.x6, which both imported.
difference x46: Both are right: public_body_party is read by labour-prelitigation.x1, which both imported.
difference x47: Both are right: work_accident_or_occupational_illness is read by labour-prelitigation.t2, which both imported.
difference x48: Settled by the text: 73-2 ¶2 requires the procedure to provide for the employer to hear the worker, and 73-4 ¶1 gives a breach of the procedure its own remedy; slot kept (B), read by n3, e2, e3.
difference x49: Not settled by the text: B's run asks employer_in_bankruptcy_or_settlement through x5, kept on the protective reading of x26 (o3); the other part of the difference is settled (n3's two slots by 73-2 ¶2, A's d1 merged into periods.d1). A's ex1 kept, its question set corrected.
difference x50: Not settled by the text: as x49 (A's ex2, which tested A's own d1; dropped, see below).
difference x51: Not settled by the text: as x49 (A's ex3; dropped with A's d1).
difference x52: Not settled by the text: as x49 (A's ex4; dropped with A's d1).
difference x53: Not settled by the text: as x49 (A's ex5; dropped with A's d1).
difference x54: Not settled by the text: as x49 (A's ex6; kept, now the due date of periods.d1 is what is asked).
difference x55: Not settled by the text: as x49 (A's ex7; kept, section other).
difference x56: Not settled by the text: as x49 (A's ex8; kept, the status asked).
difference x57: Settled by the text: the difference is n3, which A lacked; 73-2 ¶2 makes notification and hearing facts the claim turns on, so n3 is kept and decides B's ex1 (now ex9) as B expected.
difference x58: Settled by the text: as x57; the extra dismissal_date question came from A's d1, now merged into periods.d1 (x17), so it is no longer asked.
difference x59: Settled by the text: as x58 (B's ex3, now ex11: n3 warns).
difference x60: Not settled by the text: B's run asks employer_in_bankruptcy_or_settlement through x5 (x26, o3); the n3 part is settled by 73-2 ¶2 and A's dismissal_date question disappears with x17.
difference x61: Settled by the text: as x58 (B's ex5, now ex13).
difference x62: Settled by the text: as x58 (B's ex6, now ex14: only the hearing is asked).
difference x63: Settled by the text: as x58 (B's ex7, now ex15).

Counts: settled by the text 23; both are right 29; not settled by the text 11; needs a text that is not in the store 0.

## Proposed slots

- A `dismissal_date` = B `dismissal_date` → `dismissal_date` (A's fuller question: the date of notification, or the date the worker was in fact kept from work).
- B `dismissal_notified_in_writing` (A none) → kept, same id.
- B `worker_heard_before_dismissal` (A none) → kept, same id.
- B `employment_start_date` (A none) → not kept (x41).

## Boundary cases

No boundary-case file exists for this unit; nothing is disputed.

## What I changed where A and B had agreed

- Labour-relations 3 ¶1 (A b11 = B b2, agreed) is a restatement of court-sections.b52 (and labour-prelitigation.b38): replaced by the import court-sections.b52; A's own statement removed from the section alternative's basis.
- Requests matched by the id map as equal (A r3 ↔ B r3) are not the same request: A r3 is further damages under 73-4 ¶3, B r3 the procedural compensation of 73-4 ¶1 (= A r5). Both requests are kept (r3, r5).
- B's boundary n3 (now n3) kept with its condition, but `isActually` changed from `outside:lawful-disciplinary-dismissal` to `outside:procedure-followed` and its message rewritten: 73-4 ¶2 makes a dismissal in breach of 73 abusive whatever the procedure, so "lawful" is not supported by the text.
- Re-read and agreed, no change: the imported labour-prelitigation d1 (cpca 504 ¶1: six months from delivery of the record, forfeiture; counting cpca 405 ¶1-¶4), p1 (labour-disputes 19 ¶1-¶3, 37 ¶1; exemptions ¶2 and ¶3 as written), section social (cpca 500 ¶1-¶3, labour-disputes 20 ¶1-¶2, cpca 32 ¶4-¶5; both wordings of 32 hold the same ¶1-¶6).
- Not importing the formal-notice module (both agreed) is kept, but it is not settled: see o2 and the questions below.
- Examples: A's ex2-ex5 tested A's own limitation d1, now the imported periods.d1, which this record's examples cannot name (the tool accepts only the record's own items); dropped. A's ex1, ex6, ex7, ex8 kept with complete facts (A's ex1 expected no question while its own imports asked several: corrected). B's ex1-ex7 kept as ex9-ex15; B's ex1 lost the dropped employment_start_date, and B's cases except ex4 got the facts the imported items read (conciliation_agreement_reached, criminal_case_on_same_facts, damage_caused_by_offence, obligation_is_tort_compensation, debt_due_date), without which their expected empty question sets were wrong under B's own imports (x57). Reconciler cases ex16-ex21. Expected questions for module items once flattened (not checkable here): ex6 debt_due_date; ex17 harmful_act_date; ex18 obligation_is_tort_compensation; ex19 wages_due_from.
- Entry-into-force lines added (k62-k67: civil-code 124, 133, 3; cpca 32, 536 مكرر, 800) and both neighbour flags answered (k68 labour-disputes 20 ¶1, cited by court-sections.b14; k69 labour-disputes 21 ¶1, cited by labour-prelitigation.b41).

## Differences between this unit and the modules

- labour-prelitigation declares `appliesTo: ["unpaid-wages"]`, yet both derivations and this record import it for abusive-dismissal (labour-disputes 19 ¶1 covers every individual labour dispute); the module's `appliesTo` looks incomplete.
- A's own limitation ran from the dismissal date with a 15-day band; periods.d1 runs from debt_due_date (19-day band) and is silent whenever the requests include wages (its `when` excludes them). So a dismissal petition that adds a wage request loses any limitation warning on the dismissal compensation itself unless it is characterised as for a harmful act (periods.d2). And no module item carries the one-year reading of civil-code 312 ¶3 for the compensation (only o1 here).

## Questions left to the adversary

1. Formal notice: is labour-relations 73-4 a "contrary text" under civil-code 179, or does the 179 notice (with the 181 ¶3 exemption for a harmful act) apply to r2, r3, r5? Neither derivation imported formal-notice.p1; if it applies, it should be imported (it only warns).
2. x26: does a dismissal claim against an employer in bankruptcy or judicial settlement leave the social section (cpca 536 مكرر ¶4 against cpca 500 ¶3)?
3. o1: is the dismissal compensation a debt under civil-code 308/315, compensation for a harmful act under 133, or a sum owed for work under 312 ¶3; and is its due date the dismissal or the judgment?
4. x14: is 73-1 ¶1 better read as a ground of the claim (role `basis`) than as proof?
5. Boundary n3 warns in an ordinary abusive-dismissal case where the procedure was followed and the fault is contested: is that warning useful or noise?
