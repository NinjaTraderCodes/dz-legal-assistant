# Reconciliation of unpaid-rent (claim)

Reconciled record: `rules/reconciled/unpaid-rent.json` (stage `reconciled`, `verified: false`). `rules check --store` passes; `rules examples` passes 34 of 34 (17 examples of derivation A, 9 of derivation B, 8 of the reconciler). No boundary-case file exists for this unit (`rules/boundary-cases/` holds none for unpaid-rent), so `rules examples --boundary` was not run and no case is disputed.

Counts: 52 settled by the text, 41 both are right, 10 not settled by the text, 0 needs a text that is not in the store.

## The differences

difference x1: settled by the text: civil-code 106 ¶1 makes the contract the law of the parties; it grounds the payment request, so A's statement b7 is kept.
difference x2: both are right: civil-code 120 ¶1 both permits the rescission clause (A, basis) and keeps the notice (B, limit); kept as A's b10 (basis), the notice half is carried by the imported formal-notice.b12.
difference x3: settled by the text: civil-code 122 ¶1 puts the parties back where they were after rescission; it is the ground of the eviction and occupation requests, so A's b11 is kept.
difference x4: not settled by the text: civil-code 174 ¶1 allows the coercive fine only where performance needs the debtor's own act; whether paying rent or leaving the premises is such an act is a reading. b17 kept as interpretive, request r6 kept, open point o10.
difference x5: settled by the text: civil-code 182 ¶1 and ¶2 set how damages are assessed and limit contract damages to the foreseeable harm; B's statement kept, split into b48 (¶1) and b49 (¶2, the exception) as the guide requires.
difference x6: settled by the text: civil-code 324 مكرر 1 ¶1 requires the official form for commercial leases on pain of nullity; A's b21 kept (boundary n2, element e1).
difference x7: settled by the text: civil-code 328 ¶1 to ¶5 say when a private contract's date becomes fixed, the term 467 مكرر ¶1 uses; A's b34 kept.
difference x8: settled by the text: civil-code 467 مكرر ¶1 «ينعقد الإيجار كتابة» is a condition of the contract's formation, not an act done before filing; role limit (A).
difference x9: settled by the text: civil-code 467 مكرر ¶1; A's says repeats the words of the paragraph (writing, fixed date, nullity); the readings on its effect belong to boundary n1 and open points o4 and o8, so the statement is not interpretive (A).
difference x10: both are right: civil-code 467 ¶1 and ¶2 say the same definition of the lease and its rent; merged as b1 (¶1, A) and b46 (¶2, B's content).
difference x11: both are right: civil-code 467 ¶1 is cited by both (B within ¶1 and ¶2); merged as b1.
difference x12: settled by the text: civil-code 469 مكرر 1 ¶1 ends the lease at its term without a notice to quit; A's b30 kept (boundary n3).
difference x13: settled by the text: civil-code 469 مكرر 2 ¶2 keeps the lease running to its term after the tenant's death; it bears on who is sued; A's b44 kept.
difference x14: settled by the text: civil-code 469 مكرر 3 ¶1 makes the lease binding on a new owner; it bears on the lessor's standing; A's b45 kept.
difference x15: both are right: civil-code 476 ¶1 and ¶3 (B) and ¶1, ¶2 (A) are three distinct rules of the same article; merged as b13 (¶1), b14 (¶2), b47 (¶3).
difference x16: both are right: civil-code 476 ¶1 is cited by both; merged as b13.
difference x17: settled by the text: civil-code 476 ¶2 calls for the inspection record attached to the lease; A's b14 kept (element e2, document doc3).
difference x18: settled by the text: civil-code 480 ¶1 is the tenant's own claim after notice; A's b42 kept for the note boundary n5.
difference x19: settled by the text: civil-code 498 ¶2 says in the official Arabic «في موطن المستأجر»; the store holds no French check of 498, so B's French reading is kept only as open point o7 (grade D, request appended); the statement is not interpretive (A).
difference x20: settled by the text: civil-code 501 ¶1 gives the retention «ضمانا لحقوقه الناشئة عن الإيجار», a security beside the claim, not its ground; role procedure (A).
difference x21: settled by the text: civil-code 502 ¶1 speaks of «انتهاء مدة الإيجار» (the end of the term); using it after a judicial rescission is a reading, so b12 is interpretive (B) and its says now follows the words.
difference x22: both are right: civil-code 507 مكرر 1 ¶1 is A's own b33 and B's imported formal-notice.b29; both kept.
difference x23: both are right: civil-code 507 مكرر ¶1 is A's own b31 and part of B's imported formal-notice.b30; both kept.
difference x24: both are right: civil-code 507 مكرر ¶2 is A's own b32 and part of B's imported formal-notice.b30; both kept.
difference x25: settled by the text: civil-code 507 ¶1 binds the sub-tenant directly to the lessor from the lessor's warning; A's b40 kept for note boundary n7.
difference x26: settled by the text: commercial-code 169 ¶1 sets which leases the commercial-lease provisions govern, the scope of d1; A's b29 kept.
difference x27: settled by the text: commercial-code 187 مكرر 1 ¶1 keeps earlier leases' renewal under the old law; A's b24 kept (n2, n3).
difference x28: settled by the text: commercial-code 187 مكرر ¶1 «تحرر ... في الشكل الرسمي ... تحت طائلة البطلان» is a condition of how the contract is drawn up, not an act before filing; role limit (A).
difference x29: settled by the text: commercial-code 187 مكرر ¶1; A's says repeats the words (leases made from the publication, official form, nullity); the unsettled commencement is the store's note, answered by checklist k64 and open point o4, so the statement is not interpretive (A).
difference x30: settled by the text: commercial-code 187 مكرر ¶2 makes the tenant leave at the term without notice to quit; A's b23 kept (n3).
difference x31: settled by the text: commercial-code 191 ¶1 gives a rescission clause for unpaid rent effect only one month after an unanswered demand to pay; A's b25 kept (p1).
difference x32: settled by the text: commercial-code 191 ¶2 requires the demand to mention the period; A's b26 kept (p1).
difference x33: settled by the text: commercial-code 191 ¶3 lets judges suspend the clause when granting time; A's b27 kept (p1).
difference x34: settled by the text: commercial-code 192 ¶1 opens the revision of a commercial rent at a party's request; A's b43 kept for note boundary n6.
difference x35: settled by the text: commercial-code 198 ¶1; A's says repeats the words (actions under the title, court of the building, two years); the reach of the period over rent arrears is the reading carried by d1 and o1, so the statement is not interpretive (A).
difference x36: settled by the text: cpca 17 ¶3; A's says repeats the words; whether a lease petition falls under it is the reading carried by p2 and o3, so the statement is not interpretive (A).
difference x37: both are right: cpca 323 ¶2 and ¶3 say the same in B's one statement and A's two; merged as A's b18 and b19 (narrowest paragraphs).
difference x38: both are right: cpca 323 ¶2 is part of B's b19; kept as b18.
difference x39: both are right: cpca 323 ¶3 is part of B's b19; kept as b19.
difference x40: both are right: cpca 40 ¶1 and ¶2 are B's own b23 and the imported court-sections.b48 that both derivations import; kept as the import.
difference x41: settled by the text: cpca 419 ¶1 speaks of «المصاريف المترتبة عليها», the costs of the action, not compensation for harm; role procedure (A).
difference x42: settled by the text: cpca 515 ¶1 speaks of rights arising from published deeds; it carries the narrow reading of p2; A's b37 kept.
difference x43: settled by the text: cpca 519 ¶1 reserves the rules on publishing actions on published deeds; it carries the narrow reading of p2; A's b38 kept.
difference x44: not settled by the text: commercial-code 198 ¶1 reaches «جميع الدعاوى الممارسة وفقا لما ورد في هذا الباب»; whether a claim for rent arrears resting on civil-code 498 is such an action is not said (and the store's French check of 198 is a mismatch). A's d1 kept as an interpretive limitation that warns, both readings in its message and readings, open point o1 (B's own o5 asked the same and chose the same protective reading).
difference x45: not settled by the text: commercial-code 191 ¶1 and ¶2 deprive the clause of effect before the month, but do not say whether the month must have run before a petition for judicial rescission (civil-code 119 ¶1). A's p1 kept, interpretive, warns, open point o2.
difference x46: settled by the text: civil-code 467 مكرر ¶1 makes writing with a fixed date a condition of the lease's formation, not a step before filing; B's prior step p1 is replaced by A's boundary n1 (same warning, same question through lease_form).
difference x47: settled by the text: commercial-code 187 مكرر ¶1 (with civil-code 324 مكرر 1 ¶1) sets the official form as a condition of the commercial lease, not a step before filing; B's prior step p2 is replaced by A's boundary n2.
difference x48: both are right: the two conditions are the same test written in another order; merged, then widened (see the changes below).
difference x49: settled by the text: civil-code 323 ¶1 puts the obligation on the creditor and the release on the debtor; the merged element e3 has the lessor prove that the rent fell due (A's burden) and states in its proof that the tenant proves payment (B's point).
difference x50: settled by the text: civil-code 323 ¶1, as for x49; B's element (burden on the defendant to show non-payment) merged into e3.
difference x51: settled by the text: civil-code 502 ¶1 gives compensation by the rental value for holding the property without right; A's request r5 kept, with the reading of b12 for its use after rescission.
difference x52: both are right: B's r6 is the same costs request on cpca 419 ¶1 (the id map paired it with A's coercive-fine request by kind); kept as r8.
difference x53: both are right: every court-sections block either side named exists and applies (the section statements, b5 on referral, b36 cited by checklist k29, b39 behind t3, t5 for the commercial alternative, n2 and n3); the union is imported.
difference x54: both are right: every formal-notice block either side named exists and applies; the union is imported, and B's own boundary on contractual notice terms is replaced by the module's n10 (see below).
difference x55: both are right: every ordinary-court-exits block either side named exists and applies (A's order-to-pay periods d3, d4 and notes, B's statements); the union is imported.
difference x56: both are right: every periods block either side named exists and applies; the union is imported.
difference x57: both are right: claim_is_professional_fees is read by the imported periods d1 that both import; B's list left it out.
difference x58: both are right: claim_is_trader_supply_or_hotel is read by the imported periods d1 that both import.
difference x59: both are right: commercial_lease_in_official_form (B) is the same fact as lease_form = official (A); mapped to lease_form.
difference x60: both are right: debtor_refused_in_writing is read by the imported formal-notice steps that both import.
difference x61: settled by the text: cpca 512 ¶11 already names leases of dwellings and professional premises, so the section proposal needs no separate fact on real property; dispute_concerns_real_property is dropped (no item of B read it).
difference x62: both are right: lease_form (A) holds B's two form facts; kept.
difference x63: settled by the text: commercial-code 191 ¶1 turns on «كل اشتراط مدرج فى الايجار ينص على الفسخ بقوة القانون»; lease_has_rescission_clause kept for p1.
difference x64: both are right: lease_in_writing_with_certain_date (B) is lease_form in official or private_fixed_date (A); mapped to lease_form.
difference x65: settled by the text: civil-code 469 مكرر 1 ¶1 and 502 ¶1 make the end of the term change rent into compensation; lease_term_expired kept for n3.
difference x66: settled by the text: civil-code 507 مكرر 1 ¶1 keeps residential leases with public institutions under their own provisions; lease_with_public_housing_body kept for n4.
difference x67: both are right: movable_sale_price_unpaid_at_term is read by the imported formal-notice p2.
difference x68: both are right: notice_before_petition_day is read by the imported formal-notice steps and n12.
difference x69: both are right: payment_order_date is read by the imported ordinary-court-exits d3, kept in the union of x55.
difference x70: both are right: payment_order_issued is read by the imported ordinary-court-exits d3 and d4, kept in the union of x55.
difference x71: both are right: payment_order_notified_on is read by the imported ordinary-court-exits d4, kept in the union of x55.
difference x72: both are right: performance_impossible_or_useless_by_debtor is read by the imported formal-notice steps.
difference x73: both are right: petition_lodged_for_publication (A) and petition_published_at_land_registry (B) are one fact; A's id kept, since its question matches «ما لم يثبت إيداعها للإشهار» of cpca 17 ¶3.
difference x74: both are right: as x73.
difference x75: both are right: public_vehicle_damage is read by the imported ordinary-court-exits x1 that both import.
difference x76: settled by the text: commercial-code 191 ¶1 and ¶2 (a demand mentioning the month, then one month without payment); rescission_clause_notice_month_elapsed kept as p1's own slot, its question now asks that the month ran before the petition's date.
difference x77: both are right: return_of_thing_known_stolen_or_unduly_received is read by the imported formal-notice steps.
difference x78: both are right: A's clean case differs on B's record only by B's slot name for the lease's form and by items B lacked; the reconciled record gives every item none and asks nothing (exA1).
difference x79: settled by the text: commercial-code 169 ¶1 and 198 ¶1 concern commercial leases only, so a residential case gives d1 none (exA2); the other differences are slot names.
difference x80: settled by the text: commercial-code 187 مكرر ¶1 is met by an official deed and the arrears are within 198 ¶1's two years, so n2 and d1 give none (exA4).
difference x81: not settled by the text: the warning near the two-year day rests on the reach of commercial-code 198 ¶1 (o1); kept as warn (exA5).
difference x82: not settled by the text: as x81; kept as warn, never a block (exA6).
difference x83: settled by the text: civil-code 467 مكرر ¶1 voids an oral lease; n1 warns (exA12); B warned through its own step.
difference x84: settled by the text: commercial-code 187 مكرر ¶1 and civil-code 324 مكرر 1 ¶1 require an official deed; n2 warns (exA13).
difference x85: not settled by the text: whether the month of commercial-code 191 ¶1 must run before the petition (o2); p1 warns (exA14).
difference x86: settled by the text: commercial-code 191 ¶1 and ¶2 are met when the demand mentioned the month and it ran; p1 gives none (exA15).
difference x87: not settled by the text: as x85; p1 asks when the demand is unknown (exA16).
difference x88: both are right: A's and B's land-registry steps are the same rule under two slot names; with one slot the case warns (exA17).
difference x89: not settled by the text: cpca 17 ¶3 does not limit publication to rescission and eviction; the reconciled p2 asks for a payment-only claim on a published building, and exA18 is corrected from none to ask.
difference x90: both are right: the section is civil on both records; only slot names differ (exA19).
difference x91: settled by the text: civil-code 469 مكرر 1 ¶1 and 502 ¶1; rent claimed after the term ended warns through n3 (exA20).
difference x92: settled by the text: civil-code 507 مكرر 1 ¶1; a lease with a public housing institution warns through n4 (exA21).
difference x93: not settled by the text: with nothing known the case turns on the reach of commercial-code 198 ¶1 (d1 asks for commercial_lease) and on the form boundaries; the reconciled record asks each fact (exA22).
difference x94: both are right: same land-registry step and same document (A's doc5, B's doc6) under two slot names; with the petition lodged, p2 gives none (exA23).
difference x95: settled by the text: civil-code 467 مكرر ¶1 is carried by boundary n1, not a prior step; B's clean case gives n1 and n2 none (exB1).
difference x96: settled by the text: commercial-code 187 مكرر ¶1 is met by an official deed; n1, n2 and p2 give none (exB2).
difference x97: settled by the text: civil-code 467 مكرر ¶1; B's oral-lease case warns through n1 (exB3).
difference x98: settled by the text: civil-code 467 مكرر ¶1 and commercial-code 187 مكرر ¶1 make the boundaries ask the lease's form when nothing is known; p2 now asks too (cpca 17 ¶3, o3), and exB4 is corrected accordingly.
difference x99: settled by the text: civil-code 507 مكرر ¶2 keeps the right to stay of persons sixty at the publication, under leases made under the earlier legislation; B's warning on eviction under such a lease is kept by giving A's n8 B's condition (exB5, exR4, exR5).
difference x100: both are right: B's and A's land-registry steps are the same rule; the case warns (exB6).
difference x101: not settled by the text: as x89; exB7 is corrected from none to ask.
difference x102: both are right: B's own boundary on contractual notice terms restates the imported formal-notice n10 that A imported; kept as the import (exB8 no longer lists it, since `rules examples` does not run imported blocks).
difference x103: both are right: the section is civil on both records; only slot names differ (exB9).

## Proposed slots: the mapping

| Kept id | Derivation A | Derivation B | Mapping |
|---|---|---|---|
| lease_form | lease_form | lease_in_writing_with_certain_date | B yes = official or private_fixed_date; B no = private_no_fixed_date or oral |
| lease_form | lease_form | commercial_lease_in_official_form | B yes = official; B no = private_fixed_date, private_no_fixed_date or oral |
| petition_lodged_for_publication | petition_lodged_for_publication | petition_published_at_land_registry | same fact |
| lease_has_rescission_clause | lease_has_rescission_clause | (none) | A only (191 ¶1) |
| rescission_clause_notice_month_elapsed | rescission_clause_notice_month_elapsed | (none) | A only; question now says the month ran before the petition's date |
| lease_term_expired | lease_term_expired | (none) | A only (469 مكرر 1 ¶1, 502 ¶1) |
| lease_with_public_housing_body | lease_with_public_housing_body | (none) | A only (507 مكرر 1 ¶1) |

B's examples were translated with this mapping (and B's item ids: its p1 to n1, p2 to n2, p3 to p2, n1 to formal-notice.n10, n2 to n8, n3 to n1).

## Boundary cases

No boundary-case file exists for this unit; none disputed.

## Differences between this unit and the modules

- Derivation B's boundary n1 (warn when the lease sets its own notice terms) restated formal-notice n10. The module fires only when a listed request is made and the notice was served; B's version fired whatever the notice. The module's block is imported instead.
- Derivation B's note boundary n4 (end or refusal of renewal of a commercial lease) restated formal-notice n3 and court-sections n3; it is dropped and those blocks are imported.
- Derivation A's d1 counted with periods.b11 and periods.b27; the evaluator spec says every record writes its own counting with its own statements, so d1 now cites own statements b50 (civil-code 314 ¶1) and b51 (civil-code 3 ¶1), with the same method as periods d8 (excludeFirstDay, no extension).
- The land-registry step p2 is the claim's own (no land-registry module is final); it is wider than either derivation (below).

## What I changed where A and B had agreed

- **Land-registry publication (p2, doc5).** Both limited the step to rescission and eviction requests. cpca 17 ¶3 says «إذا تعلقت بعقار و/أو حق عيني عقاري مشهر»; cpca 40 ¶2 itself calls lease actions «دعاوى الإيجارات ... المتعلقة بالعقارات»; cpca 515 ¶1 and 519 ¶1 speak of rights based on published deeds. No paragraph supports the cut at rescission and eviction: the broad reading reaches every lease petition on a published immovable, the narrow one only a published lease. The step now applies whenever property_registered is yes, stays interpretive, warns and never blocks; doc5 follows; o3 holds three readings. exA18, exB4 and exB7 are corrected (none to ask).
- **Statement on 502 (b12).** Both had it as a basis of eviction; it is now interpretive with says that follow «انتهاء مدة الإيجار» (x21).
- **Element e3.** Both had an element on the unpaid rent with opposite burdens; merged on 323 ¶1 (x49, x50).
- **Open point on commencement (o4).** A graded it D because "the store does not carry those dates". The store's page records the Official Journal issues (2007 no. 31 of 13 May 2007 for law 07-05; 2005 no. 11 of 9 February 2005 for law 05-02), while its notes say publication dates are not substituted for commencement. It is now grade C with two readings; n8 compares the contract date with 14 May 2007 (the day after the issue), as B's boundary did.
- **Re-read with no change.** The section rule (cpca 512 ¶11, 32 ¶5, 531 ¶1; the commercial alternative stays interpretive as in court-sections o2), the imported exits x1, x4, x7, the imported formal-notice steps p1 to p3, the request kinds, and the agreed statements (498, 499, 323, 333, 476 ¶1, 186, 176, 119, 120, 324 مكرر 1, 501, 500, 323 cpca, 419).
- **Entry-into-force notes.** Checklist lines k64 (187 مكرر, 187 مكرر 1), k65 (the civil-code lease articles and 106, 119, 120, 122) and k66 (civil-code 3) added, with the derivations that cite the articles in `from`.
- **Neighbour flags.** Cited: 119 ¶2 (b9), 476 ¶3 (b47), 498 ¶2 (b4), 507 مكرر ¶2 (b32), 187 مكرر 1 (b24), cpca 323 ¶2 (b18). Answered by doesNotApply lines k67 to k73: commercial-code 188 ¶1, 170 ¶1, civil-code 469 مكرر 1 ¶2, 328 ¶6, 333 ¶2, commercial-code 192 ¶4, cpca 40 ¶10.
- **Requests appended to law/requests.jsonl:** civil-code 498 (the French edition B relied on; open point o7).

## Questions left to the adversary

1. Is widening p2 to payment-only claims the protective reading of cpca 17 ¶3, or does «عريضة رفع الدعوى ... إذا تعلقت بعقار» only reach petitions that touch the registry?
2. n8 uses 14 May 2007 (the day after the Official Journal issue that published law 07-05, from the store's page record). Civil Code article 4 may put the start a few days later in some localities: should the cutoff move, and on which text?
3. The store's French check of commercial-code 198 is a mismatch. The statement b28 is kept non-interpretive (it repeats the Arabic words) while d1 is interpretive; does the mismatch touch the words the statement repeats?
4. Is the demand of commercial-code 191 (p1) the same act as the formal notice of the imported formal-notice steps? If so, under the owner's rule of 14 November 2026, p1 should not say "done" while formal-notice n1, n11 or n12 still warn about that notice's means or date.
5. Is civil-code 174 ¶1 available to compel a tenant to leave (o10)?
6. The value report flags the section message for naming articles 32, 512 and 531, which are carried by imported court-sections statements; `rules check` accepts it. Is a claim's own section message expected to name only its own statements' articles?
