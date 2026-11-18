# Reconciliation: unpaid-wages (claim)

Reconciler: claude-opus-5-5, 18 November 2026. Record: `rules/reconciled/unpaid-wages.json` (stage `reconciled`, `verified: false`). `rules check --store`: OK. `rules examples`: 13 cases, 0 differences. No boundary-case file exists for this unit (`rules/boundary-cases/` holds only module files), so `rules examples --boundary` could not be run. This is a claim, so there is no third derivation.

Counts: 39 settled by the text, 41 both are right, 2 not settled by the text, 1 needs a text that is not in the store (83 differences).

## Differences

difference x1: both are right: civil-code 176 ¶1 is cited by B as its own b13 and by A through the import of formal-notice.b14 (same paragraph); merged as the import formal-notice.b14.
difference x2: both are right: civil-code 179 ¶1 is B's b15 and A's import formal-notice.b1 (same paragraph); merged as the import formal-notice.b1.
difference x3: both are right: civil-code 186 ¶1 is B's b12 and A's import formal-notice.b15 (same paragraph); merged as the import formal-notice.b15.
difference x4: settled by the text: civil-code 313 ¶1 runs limitation "في الحقوق المذكورة في المادتين 309 و311", and 309 ¶1 lists «المرتبات والاجور», so the paragraph bears on when the five-year wage limitation starts; B's citation is kept as the import periods.b9 (interpretive in the module) and the starting event stays open point o7.
difference x5: both are right: civil-code 321 ¶1 is B's b29, and A carries it through periods.d3 and d4 (raisedBy pleaOnly, message citing article 321); merged as the imports periods.b23, with periods.b24 for ¶2.
difference x6: needs a text that is not in the store: civil-code 333 ¶1 bars witnesses for a legal act above the threshold "ما لم يوجد نص يقضي بغير ذلك", and whether Law 90-11 article 10 (proof of the relationship by any means) is that text cannot be read in the store; b10 stays interpretive, request appended (labour-relations 10), checklist k55 searchedNotFound, open point o8 grade D.
difference x7: settled by the text: civil-code 565 ¶1 gives the workers of a contractor a direct claim against the owner of the work, a different defendant and ground, so A's note-only boundary n3 stays; A's `says` ("up to what the owner owes") is corrected because the words read «بما يجاوز القدر», and b8 is made interpretive (open point o10).
difference x8: settled by the text: cpca 10 ¶1 makes a lawyer compulsory only before the courts of appeal and cassation, which with cpca 14 ¶1 answers checklist line k62; B's statement kept.
difference x9: settled by the text: cpca 13 ¶1 and ¶2 require standing and an interest and let the judge raise lack of standing of his own motion, answering k63 and neighbour flag f-b28-previous; B's statement kept.
difference x10: settled by the text: cpca 14 ¶1 says the claim is brought by a written, signed, dated petition lodged by the plaintiff, his agent or his lawyer; B's statement kept.
difference x11: settled by the text: cpca 15 ¶1 makes the particulars of ¶2 to ¶7 a condition of formal admissibility of every statement of claim; with no final petition-formal-elements record to import, B's statement is kept and A's k30 answer (doesNotApply) is corrected to applies.
difference x12: settled by the text: cpca 17 ¶1 registers a petition only after the fees are paid "ما لم ينص القانون على خلاف ذلك", the fee that legal aid relieves; kept as the import labour-prelitigation.b49 (same paragraph).
difference x13: settled by the text: cpca 418 ¶1 and ¶2 define court costs, lawyer's fees included, the content of request r3; B's statement kept.
difference x14: settled by the text: cpca 419 ¶1 puts the costs on the losing party, the ground of request r3; B's statement kept.
difference x15: settled by the text: cpca 503 ¶1 says a claim before the social section is brought by a statement of claim; kept as the import labour-prelitigation.b34 (same paragraph), also in the section rule's basis.
difference x16: both are right: cpca 510 ¶1 is B's b20 and A's import labour-prelitigation.b35 (same paragraph); merged as the import.
difference x17: both are right: labour-disputes 2 ¶1 is B's b4 and the basis of the step labour-prelitigation.p2 that A imports (labour-prelitigation.b4); merged as the import labour-prelitigation.b4.
difference x18: both are right: labour-disputes 20 ¶1 and ¶2 are B's b5 and A's import court-sections.b14 (same paragraphs); merged as the import.
difference x19: both are right: labour-disputes 21 ¶1 and ¶3 are B's b18 and part of A's import labour-prelitigation.b41 (¶1 to ¶3); merged as the import, which also answers neighbour flag f-b5-next.
difference x20: settled by the text: labour-disputes 25 ¶1 grants legal aid "بكامل الحقوق" to every worker whose pay is (below) twice the guaranteed minimum wage, and cpca 417 ¶2 exempts a beneficiary from court costs; kept as the import labour-prelitigation.b46 with document doc3, the printed «يقبل» kept as open point o11.
difference x21: both are right: labour-disputes 30 ¶1 is B's b7 and the basis of means m2 of the step labour-prelitigation.p1 that A imports (labour-prelitigation.b12); merged as the import.
difference x22: both are right: labour-disputes 31 ¶2 is B's b6 and the basis of means m1 of labour-prelitigation.p1 (labour-prelitigation.b15); merged as the import.
difference x23: settled by the text: labour-disputes 32 ¶1 makes the record of conciliation proof unless challenged as forged, which bears on the partial-conciliation check (labour-prelitigation.n1, k11, k39); kept as the import labour-prelitigation.b16, which also answers neighbour flag f-b6-next.
difference x24: both are right: labour-disputes 37 ¶1 is B's b8 and A's import labour-prelitigation.b20 (same paragraph); merged as the import.
difference x25: not settled by the text: labour-disputes 39 ¶1 fixes "الغرامة التهديدية اليومية المنصوص عليها في المادتين 34 – 35", and 34 ¶1 and 35 ¶1 concern the execution of a conciliation agreement and of a collective agreement, so whether it reaches a judgment for wages is not in the words; b19 stays interpretive, both readings in open point o9 (grade C).
difference x26: settled by the text: labour-relations 73-4 ¶1 grants a worker dismissed in breach of procedure a compensation "لا يقل عن الأجر الذي يتقاضاه كما لو استمر في عمله", so pay for a period after a dismissal is that compensation, not the wage of 80 ¶1; A's b6 is kept in boundary n1.
difference x27: both are right: A's e1 and B's e1 are the same element (an employment relationship with the defendant), unmatched only by their bases; merged into one e1 with both proofs.
difference x28: both are right: the same merge as x27, seen from B's side.
difference x29: settled by the text: cpca 419 ¶1 (the loser bears the costs) with 418 ¶1 and ¶2 grounds the usual request r3; B's request kept.
difference x30: settled by the text: labour-disputes 21 ¶3 names the claim to deliver work certificates and pay statements among those the social court decides, and cpca 510 ¶1 admits additional requests that follow from the original one even if not put to conciliation; B's r4 kept, whether it "follows from" the wage request left in open point o3, and A's note-only boundary n2 kept for the case where delivery is the main request.
difference x31: not settled by the text: r5 rests only on labour-disputes 39 ¶1 (see x25); kept as an optional accessory request whose only basis is the interpretive b19, open point o9.
difference x32: both are right: every block either side imported from court-sections exists and applies: A's x4 (cpca 800 ¶2 in the law-22-13 wording names national public bodies; interpretive in the module), B's b3, b4, b5 (cpca 32 ¶4 to ¶6), n4 (note) and n7 (a civil servant's wage claim); the union is imported.
difference x33: both are right: A's extra formal-notice blocks are b1, b14, b15 (the paragraphs B restated as its own b15, b13, b12) and doc3, doc4, which document two exemptions of formal-notice.p1 (civil-code 180 ¶1 agreement, 181 ¶5 refusal in writing); the union is imported, with formal-notice.b5 added for neighbour flag f-b15-next (civil-code 180 ¶1).
difference x34: both are right: A imported the labour-prelitigation statements B restated as its own, and B imported the exits x1 (cpca 800 ¶2 against 500) and x2 (labour-disputes 19 ¶3, labour-relations 3 ¶1) that A had not; both sets apply and are merged.
difference x35: settled by the text: cpca 802 ¶3 confines the exception that distinguishes ordinary-court-exits.x1 to liability for damage caused by a public body's vehicle, which no wage claim is, and the exit's trigger (a public body as party, cpca 800 ¶2) is carried by labour-prelitigation.x1; ordinary-court-exits.x2 is kept because cpca 804 ¶5 sends disputes on civil servants and staff of public administrative bodies to the administrative courts; imported: x2, x5, x6.
difference x36: both are right: B's extra periods.doc3 (proof of the day a period starts: the due date, the delivery of the non-conciliation record) applies to this claim; the union is imported.
difference x37: both are right: conciliation_agreement_reached is read by labour-prelitigation.n1, which both imported; B left it out of its slot list.
difference x38: both are right: contract_sets_notice_terms is read by formal-notice.n10, which both imported.
difference x39: both are right: court_has_section_for_matter is read by court-sections.x3, which both imported.
difference x40: both are right: criminal_case_on_same_facts is read by ordinary-court-exits.x6, which both imported.
difference x41: both are right: damage_caused_by_offence is read by ordinary-court-exits.x6, which both imported.
difference x42: both are right: debtor_refused_in_writing is read by formal-notice.p1, which both imported.
difference x43: both are right: default_on_due_date_agreed is read by formal-notice.p1, which both imported.
difference x44: both are right: defendant_resides_abroad is read by labour-prelitigation.p1, which both imported (B even proposed it; it is already in slots.json).
difference x45: both are right: earlier_nonconciliation_record is read by labour-prelitigation.d3, which both imported.
difference x46: both are right: earlier_nonconciliation_record_delivered_on is read by labour-prelitigation.d3, which both imported.
difference x47: both are right: employer_in_bankruptcy_or_settlement is read by labour-prelitigation.p1 and ordinary-court-exits.x5, which both imported.
difference x48: both are right: forfeiture_force_majeure_invoked is read by labour-prelitigation.d1 and d2, which both imported.
difference x49: both are right: forfeiture_lifted_by_order is read by labour-prelitigation.d1, d2 and d3, which both imported.
difference x50: both are right: internal_grievance_exhausted is read by labour-prelitigation.p2, which both imported.
difference x51: settled by the text: national_public_body_party is read by court-sections.x4, kept because cpca 800 ¶2 (law-22-13 wording) gives the administrative courts the cases in which a national public body is a party (see x32).
difference x52: both are right: notice_before_petition_day is read by formal-notice.p1 and n12, which both imported.
difference x53: both are right: notice_date is read by formal-notice.p1 and n12, which both imported.
difference x54: both are right: notice_means is read by formal-notice.p1, n1 and n11, which both imported.
difference x55: both are right: notice_served is read by formal-notice.p1 and its boundaries, which both imported.
difference x56: both are right: obligation_is_tort_compensation is read by formal-notice.p1 and ordinary-court-exits.x6, which both imported.
difference x57: both are right: performance_impossible_or_useless_by_debtor is read by formal-notice.p1, which both imported.
difference x58: both are right: public_body_party is read by an exit in both records (A's ordinary-court-exits.x1, B's labour-prelitigation.x1); the reconciled record keeps labour-prelitigation.x1 (x35).
difference x59: settled by the text: public_vehicle_damage was read only by ordinary-court-exits.x1, whose vehicle exception cpca 802 ¶3 confines to liability for vehicle damage; with that exit not imported (x35) the slot is not used.
difference x60: both are right: return_of_thing_known_stolen_or_unduly_received is read by formal-notice.p1, which both imported.
difference x61: settled by the text: labour-relations 80 ¶1 ties the wage to "العمل المؤدى" and 73-4 ¶1 makes pay for the period after an irregular dismissal a compensation; A's slot wages_claimed_after_dismissal and n1's `when` (that slot, or a reinstatement request) are kept, so B's n1, firing on reinstatement only, missed the case.
difference x62: both are right: work_accident_or_occupational_illness is read by labour-prelitigation.t2, which both imported (B proposed it; it is already in slots.json).
difference x63: settled by the text: labour-disputes 25 ¶1 makes legal aid turn on pay below twice the guaranteed minimum wage and cpca 417 ¶2 exempts the beneficiary from costs, so B's slot worker_pay_below_twice_snmg and document doc3 are kept.
difference x64: both are right: A's ex1 gives n1 `none` under both records (no reinstatement, no post-dismissal pay); the difference is only that the id map has no n1 pair.
difference x65: settled by the text: A's ex2 asks national_public_body_party (court-sections.x4, cpca 800 ¶2 law-22-13 wording) and wages_claimed_after_dismissal (n1, labour-relations 80 ¶1 and 73-4 ¶1); the reconciled record asks both, as A's record did.
difference x66: settled by the text: A's ex3, same two questions as x65 on the same paragraphs; A's side.
difference x67: settled by the text: A's ex4, same two questions as x65 on the same paragraphs; A's side.
difference x68: settled by the text: A's ex5, n1 asks under A and is silent under B, and the same two questions as x65; labour-relations 73-4 ¶1 makes the post-dismissal fact matter, so n1 asks; A's side.
difference x69: settled by the text: A's ex6, same two questions as x65 on the same paragraphs; A's side.
difference x70: settled by the text: A's ex7, same two questions as x65 on the same paragraphs; A's side.
difference x71: settled by the text: A's ex8, same two questions as x65 on the same paragraphs; A's side.
difference x72: settled by the text: A's ex9 differs on three questions: national_public_body_party and wages_claimed_after_dismissal are asked (cpca 800 ¶2; labour-relations 73-4 ¶1), public_vehicle_damage is not (cpca 802 ¶3, x35); mixed.
difference x73: settled by the text: A's ex10, same two questions as x65 on the same paragraphs; A's side.
difference x74: settled by the text: A's ex11, n1 warns under A (pay after dismissal) and is silent under B; labour-relations 73-4 ¶1 settles it for A; national_public_body_party as x65.
difference x75: settled by the text: A's ex12, same two questions as x65 on the same paragraphs; A's side.
difference x76: settled by the text: A's ex13, as x72: national_public_body_party and wages_claimed_after_dismissal asked, public_vehicle_damage not (cpca 802 ¶3).
difference x77: settled by the text: B's ex1 expected no question, but the imported items ask on paragraphs that make the facts matter (labour-disputes 19 ¶1 and 5 ¶1, cpca 504 ¶1, civil-code 179 ¶1 for the damages requested, cpca 32 ¶5, 800 ¶2); between the two records, national_public_body_party and wages_claimed_after_dismissal are asked and public_vehicle_damage is not (as x72); B's example corrected (n1 asks, no question list).
difference x78: settled by the text: B's ex2, as x77 (no damages requested, so no notice questions); documents merged, the legal-aid document correctly absent when pay is not below the threshold (labour-disputes 25 ¶1); B's example corrected.
difference x79: settled by the text: B's ex3, as x77 for the questions; n1 warns under both on the reinstatement request (labour-relations 73-4 ¶3); B's example kept without its empty question list.
difference x80: settled by the text: B's ex4, as x77 for the questions; section other under both (labour-relations 3 ¶1, cpca 800 ¶2); B's example kept without its empty question list.
difference x81: settled by the text: B's ex5 expected n1 `none` and only worker_is_public_servant asked; with n1 settled (x61, labour-relations 73-4 ¶1) n1 asks, and the imported items ask the rest; B's example corrected.
difference x82: settled by the text: B's ex6, as x77 for the questions; n1 warns and the section moves under both; doc3 listed (labour-disputes 25 ¶1); B's example kept without its empty question list.
difference x83: settled by the text: B's ex7 expected only worker_is_public_servant; n1 also asks (x61) and the imported items ask the rest (as x77); B's example dropped as covered by bex5 and rex3.

## Proposed slots

| A | B | Kept id | Note |
|---|---|---|---|
| wages_claimed_after_dismissal | (none) | wages_claimed_after_dismissal | A only; settled by labour-relations 80 ¶1 and 73-4 ¶1 (x61). |
| (none) | worker_pay_below_twice_snmg | worker_pay_below_twice_snmg | B only; settled by labour-disputes 25 ¶1 and cpca 417 ¶2 (x63). |
| worker_is_public_servant (used, in slots.json) | worker_is_public_servant (proposed) | worker_is_public_servant | Already in slots.json (origin labour-prelitigation); not proposed. |
| wages_due_from (used, in slots.json) | wages_due_from (proposed) | wages_due_from | Already in slots.json; not proposed. |
| defendant_resides_abroad (used) | defendant_resides_abroad (proposed) | defendant_resides_abroad | Already in slots.json; not proposed. |
| employer_in_bankruptcy_or_settlement (used) | employer_in_bankruptcy_or_settlement (proposed) | employer_in_bankruptcy_or_settlement | Already in slots.json; not proposed. |
| work_accident_or_occupational_illness (used) | work_accident_or_occupational_illness (proposed) | work_accident_or_occupational_illness | Already in slots.json; not proposed. |

The record's `proposedSlots` holds the first two only.

## Boundary cases

No boundary-case file for unpaid-wages exists in `rules/boundary-cases/`. No case disputed.

## What I changed where A and B had agreed

- **Section rule.** Both wrote social with the public-servant alternative, citing court-sections.b11 and b14 while the message names articles 500, 20 and 32 (the value reports flagged it). I added court-sections.b3 (cpca 32 ¶4) and labour-prelitigation.b34 (cpca 503 ¶1) to the basis. I re-read cpca 500 ¶1 to ¶3 and labour-disputes 20 ¶1 and ¶2: the value stands. The value check seems not to resolve imported statement ids for 500 and 20; `rules check` passes.
- **Deadlines (imported, both agreed).** I re-read cpca 504 ¶1, 405 ¶1 to ¶4 and 322 ¶1 and ¶2 for labour-prelitigation.d1 to d3, and civil-code 309 ¶1, 312 ¶3 and ¶4, 314 ¶1, 315 ¶1 and 321 ¶1 and ¶2 for periods.d3 and d4. Their periods, kinds, counting and who raises them follow the words. One gap is noted under the module checks below (313 ¶1).
- **Prior steps (imported, both agreed).** labour-prelitigation.p1 rests on labour-disputes 19 ¶1 to ¶3 and 37 ¶1, and p2 on 2, 4 and 5. formal-notice.p1 rests on civil-code 179 ¶1, 180 ¶1 and 181. I re-read all of them and found nothing to change. Under the owner's rule on "done", p1 says done only when the record exists and was delivered before the petition (n5 covers the rest), and formal-notice.p1 is silent when the means are not a bailiff's (its boundaries warn).
- **Exits.** Both imported ordinary-court-exits.x5 and x6, and these stand (cpca 536 مكرر; civil-code 339 and cpca 39 through the module). Both imported court-sections.x3, which stands (cpca 32 ¶5).
- **Examples.** A's ex2, ex3, ex4, ex6, ex8, ex9, ex10, ex12 and ex13 came through the quoted copy with empty findings: they named only imported items, so at this stage they check nothing. I did not carry them over. A's ex5 and ex7 now state the n1 slot and its finding, and ex1 now lists the documents. Under the settled n1, B's ex1 and ex5 now expect n1 `ask`, not `none`. B's empty or single question lists are removed, because the imported items ask more once the record is flattened. B's ex1 title said "wages claimed only" although damages were requested, so I corrected it. The examples are renamed bex1 to bex6.
- **Statements kept as both wrote them.** labour-relations 2 ¶1 (b3) and civil-code 323 ¶1 (b4) stay as own statements because A and B agreed on them, although labour-prelitigation.b48 and periods.b42 cite the same paragraphs.
- **Added by the reconciler (graded `addedAfterDerivation`).**
  - b31: civil-code 182 ¶2 answers neighbour flags f-b5-p2 and f-b14-p2, and limits the delay damages of r2.
  - b32: cpca 417 ¶2 is the cost exemption that doc3's `why` already named.
  - Imports formal-notice.b5 (flag f-b15-next), periods.b24 (flag f-b29-p2) and labour-prelitigation.b16 (flag f-b6-next).
  - Checklist lines k67 to k74: the in-force notes on cpca 800, 804 and 536 مكرر and on civil-code 565, 3, 106, 119, 120 and 133, plus flag f-b10-p2.
  - Examples rex1 to rex3.
  - Open points o10 (565 wording) and o11 (25 wording).

## Differences between this unit and its modules (checks of the modules)

1. **labour-prelitigation section rule.** The module's rule (social; b27, b28, b50) has no alternative. A civil servant's wage claim is therefore proposed to the social section, while its own x2 and ordinary-court-exits.x2 send that claim out. The claim keeps its own rule, which has the public-servant alternative.
2. **periods.d4.** The deadline runs from the due date (civil-code 315 ¶1) and does not take up 313 ¶1. That paragraph starts limitation for the rights of article 309 (wages among them) "من الوقت الذى يتم فيه الدائنون تقديم خدماتهم". The claim records this as o7. The reference "309 و 311" in 313 ¶1 looks misnumbered, since 311 is taxes, but the store's French check reads "equal".
3. **labour-prelitigation.b46.** It reads labour-disputes 25 ¶1 as "below twice" without flagging the printed «يقبل». B flagged it, and the claim keeps it as o11.
4. **Two exit pairs on the same facts.**
   - Public party: ordinary-court-exits.x1 and labour-prelitigation.x1 fire on the same slot.
   - Civil servant: ordinary-court-exits.x2 (administrativeCourt, cpca 804 ¶5) and labour-prelitigation.x2 (outsideCatalogue) fire on the same slot but name different destinations. court-sections.n7 also fires on the same fact, so a civil servant's wage claim gets three warnings.
   - 804 ¶5 supports administrativeCourt.
5. **periods.d5.** It repeats labour-prelitigation.d1 (cpca 504) and is not imported.

## Questions left to the adversary

- Should r4 (work certificate and pay statements) and r5 (penalty payment) be proposed at all? r5 rests only on the interpretive b19, and r4's admissibility without conciliation is o3.
- Does dropping ordinary-court-exits.x1 lose its message on special texts (social-security disputes) for a public employer? Should labour-prelitigation.x2 or ordinary-court-exits.x2 be the only civil-servant exit?
- Is `claim:abusive-dismissal` the right target for n1? 73-4 ¶1 concerns a dismissal in breach of procedure, which ¶2 distinguishes from an abusive one.
- Civil-code 565 ¶1 «بما يجاوز» (o10): is it a misprint for «بما لا يجاوز»? The store holds no French edition of the article.
- Does civil-code 333 reach the employment relationship (o8)? The answer depends on labour-relations 10, which has been requested.
- Labour-disputes 25 ¶1 «يقبل» (o11).
