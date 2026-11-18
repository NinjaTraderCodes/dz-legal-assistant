# contract-rescission: reconciliation

Reconciled record: `rules/reconciled/contract-rescission.json` (stage `reconciled`, not verified). `rules check`: OK. `rules examples`: 35 cases, 0 differences. `rules examples --boundary`: bc1 to bc8 pass; bc9 to bc23 match only imported formal-notice and exit items, which the one-record run shown in the brief does not load (each says "matches no item" or matches only n12/n13); their outcomes with the modules are those of the script's boundaryA/boundaryB runs (differences x130 to x134, x144 to x148), discussed below.

Ids: items kept from derivation A keep A's ids; B's examples are renamed exB1 to exB13; B-only statements are b50 to b59 (B b54→b50, b55→b51, b8→b52, b24→b53, b21→b54, b22→b55, b25→b56, b26→b57, b33→b58, b44→b59); B's n3 (article 123) is n14; B's n2 merges into A's n3, B's n1 into A's n5, B's n4 into A's n6, B's n5 into A's n1, B's n6 into A's n12; B's p1 merges into A's p2; B's d1 is d1. New: b60 (101 ¶2), b61 (333 ¶2), d2, d3, exR1 to exR7, o13.

## Differences

difference x1: both are right: civil-code 101 ¶1 (A) and 102 ¶1 (B, x2) are different paragraphs of the same nullity boundary; both kept in the merged boundary n5.
difference x2: both are right: civil-code 102 ¶1 (B) states who may rely on absolute nullity; kept beside A's paragraphs in boundary n5.
difference x3: both are right: civil-code 102 ¶2 (A) exists and now also carries the added nullity deadline d2; B reached nullity through 102 ¶1 and 103 ¶1.
difference x4: both are right: civil-code 103 ¶1 (B) states the effect of nullity, which the boundary n5 contrasts with rescission; kept.
difference x5: settled by the text: civil-code 107 ¶1 («يجب تنفيذ العقد طبقا لما اشتمل عليه») is the duty whose breach founds the claim; B's statement kept (b52, element e3).
difference x6: both are right: civil-code 120 ¶1 is carried by the formal-notice module (b11, b12, n2) that A imported; B's own statement is replaced by the import.
difference x7: both are right: civil-code 121 ¶1 is formal-notice.b26 (note n7, imported by both); B's own statement replaced by the import.
difference x8: settled by the text: civil-code 122 ¶1 first states that on rescission the parties are put back where they were, the basis of the restitution request; compensation is only its fallback; role basis (A).
difference x9: settled by the text: civil-code 164 ¶1 compels performance in kind after notice, the alternative remedy of 119 ¶1; kept only as basis of boundary n13 through the import formal-notice.b4.
difference x10: settled by the text: civil-code 174 ¶1 orders performance in kind by the debtor himself under a penalty payment, which a rescission request gives up; B's statement dropped (checklist k85).
difference x11: settled by the text: civil-code 175 ¶1 fixes compensation once performance in kind is obtained or refused, a sequel of 174; dropped (k85).
difference x12: both are right: civil-code 179 ¶1 is formal-notice.b1, the basis of formal-notice.p1 that A imported; B's own statement replaced by the import (now in r3 and e6).
difference x13: settled by the text: civil-code 184 ¶2 («ويجوز للقاضى أن يخفض») is a rule of its own beside ¶1, so it is cited apart (A's b20/b21), not merged with ¶1 as B did.
difference x14: settled by the text: civil-code 184 ¶1 cited alone (b20) for the same reason as x13.
difference x15: settled by the text: civil-code 184 ¶2 cited alone (b21) for the same reason as x13.
difference x16: settled by the text: civil-code 186 ¶1 gives damages for delay in paying a sum fixed when the claim is filed, which the sum to be returned is; B's request kept (r5), its basis imported as formal-notice.b15.
difference x17: both are right: civil-code 308 ¶1 is periods.b1, the basis of periods.d1 imported by both; B's own statement replaced by the import.
difference x18: both are right: civil-code 314 ¶1 is periods.b11; imported, and now the counting basis of d1, d2, d3.
difference x19: both are right: civil-code 315 ¶1 is periods.b12; imported.
difference x20: settled by the text: civil-code 324 مكرر 1 ¶1 states a form required «تحت طائلة البطلان», a condition of the contract, not a step to take before filing; role limit (A).
difference x21: settled by the text: civil-code 324 مكرر 5 ¶1 makes an official deed proof until shown forged, relevant to proving the contract; B's statement kept (b53, e1).
difference x22: settled by the text: civil-code 334 ¶2 is a list item whose verb is in ¶1 («لا يجوز الإثبات بالشهود ولو لم تزد القيمة»); B's statement kept, cited as ¶1 and ¶2 (b54).
difference x23: settled by the text: civil-code 335 ¶1 allows witnesses with a beginning of written proof; B's statement kept (b55, e1 and e2 proof).
difference x24: settled by the text: civil-code 351 ¶1 defines the seller's obligation whose breach founds rescission of a sale; B's statement kept (b56).
difference x25: settled by the text: civil-code 361 ¶1 states the seller's duty to transfer the right sold; B's statement kept (b57).
difference x26: settled by the text: civil-code 365 ¶1 says the buyer may ask «فسخ العقد لنقص» only for a serious shortfall: a condition of this claim; A's statement kept, now in d1's basis.
difference x27: settled by the text: civil-code 370 ¶1 lets the buyer ask rescission for serious loss of value before delivery, a rescission ground of this claim; B's statement kept (b58).
difference x28: settled by the text: civil-code 386 ¶1 sets the functioning-guarantee action and its periods, a different action; A's boundary n9 kept.
difference x29: settled by the text: civil-code 554 ¶1 sets the ten-year guarantee, a different action; A's boundary n7 kept.
difference x30: settled by the text: civil-code 557 ¶1 sets that guarantee's three-year limit; kept with n7.
difference x31: both are right: civil-code 558 ¶1 is formal-notice.b20, the basis of formal-notice.n5 imported by both; B's own statement dropped in favour of the import.
difference x32: settled by the text: civil-code 567 ¶1 ends a works contract when the work becomes impossible, the works counterpart of 369; B's statement kept in boundary n1.
difference x33: settled by the text: commercial-code 30 lists the free means of proving a commercial contract, the exception 333 ¶1 names («في غير المواد التجارية»); A's statement kept.
difference x34: settled by the text: cpca 13 ¶1 conditions every claim on standing and interest; no final module carries it; A's statement kept.
difference x35: settled by the text: cpca 15 ¶1 and ¶6 require facts, requests and grounds on pain of formal inadmissibility; no final module carries them; A's statement kept.
difference x36: settled by the text: cpca 17 ¶3 attaches «عدم قبولها شكلا» to a step the plaintiff must take (publishing the petition): role prerequisite (A), carried by prior step p1.
difference x37: settled by the text: cpca 323 ¶3 («في جميع الأحوال الأخرى») is a rule of its own beside ¶2, so the two are cited apart (A's b44/b45), not together as in B's b52.
difference x38: settled by the text: cpca 323 ¶2 cited alone (b44), as x37.
difference x39: settled by the text: cpca 323 ¶3 cited alone (b45), as x37.
difference x40: settled by the text: cpca 39 ¶4 has its own verb («يؤول الاختصاص»), so it carries the rule alone; B's narrower citation [4] kept for b29.
difference x41: settled by the text: cpca 39 ¶5 has no verb of its own and reads only with the chapeau ¶1 («ترفع الدعاوى ... أمام»); A's citation [1, 5] kept for b30.
difference x42: settled by the text: same item as x40; cpca 39 ¶4 cited alone.
difference x43: settled by the text: same item as x41; cpca 39 ¶5 needs ¶1.
difference x44: settled by the text: cpca 45 ¶1 voids a jurisdiction clause in the contract unless between traders; A's statement kept with territory rule t6.
difference x45: both are right: cpca 515 ¶1 is court-sections.b20, imported by A; B's own statement replaced by the import.
difference x46: both are right: cpca 519 ¶1 is court-sections.b38 (basis of court-sections.n6, imported by A); imported in place of B's own statement.
difference x47: settled by the text: cpca 534 ¶1 puts the mediation on the commercial section's president, not on the plaintiff before filing; A's statement kept as information (o8).
difference x48: settled by the text: civil-code 366 ¶1 times out «حق المشتري في طلب ... فسخ العقد» for a shortfall one year after actual delivery, so it is a deadline of this claim; B's d1 kept and A's boundary n4 (which called it outside) folded into it.
difference x49: settled by the text: cpca 17 ¶3 requires publication of a petition concerning a published property on pain of formal inadmissibility; A's prior step p1 kept (B left it as an open point).
difference x50: settled by the text: civil-code 553 ¶1 triggers the warning when «المقاول يقوم به على وجه معيب أو مناف لشروط العقد» while the work is in progress, not for every works contract; A's slot kept, with B's «requests includes rescission» (the article's «أن يطلب ... فسخ العقد») added.
difference x51: both are right: works_correction_impossible (A) and defect_correction_impossible (B) hold the same fact of civil-code 553 ¶2; A's id kept.
difference x52: both are right: B put cpca 38 inside its t1; A wrote it as t2; kept separate.
difference x53: settled by the text: cpca 45 ¶1 exists and reaches jurisdiction clauses in contracts; A's t6 kept.
difference x54: settled by the text: cpca 39 ¶4 names supplies and the hire of services besides works, so a rule limited to works contracts is narrower than the text; shown without a condition (A).
difference x55: settled by the text: cpca 40 ¶2 also covers «الأشغال المتعلقة بالعقار», which dispute_concerns_real_property may not catch; shown without a condition, its words carrying the scope (A).
difference x56: both are right: A's e2 and B's e2 are the same element (the plaintiff performed or paid); merged.
difference x57: settled by the text: civil-code 323 ¶1 («وعلى المدين اثبات التخلص منه») puts the proof of delivery on the seller, so the non-delivery element's burden is the defendant's (A); merged with B's e3.
difference x58: settled by the text: civil-code 119 ¶1 gives rescission to either party, and 388 ¶1 and 559 ¶1 fix when the price falls due; A's e5 (seller or contractor plaintiff) kept.
difference x59: both are right: same element as x56, merged.
difference x60: both are right: B's e3 is A's e3 (non-delivery), merged; burden per x57.
difference x61: settled by the text: civil-code 186 ¶1 founds damages for late payment of a sum fixed at filing; B's request kept as r5.
difference x62: both are right: cpca 419 ¶1 makes costs follow the loss without a request (A's note), and nothing bars asking for them (B's request); kept as accessory r6.
difference x63: settled by the text: civil-code 119 ¶1 grants damages «مع» performance or rescission, «إذا اقتضى الحال ذلك»: accessory to the main request (A).
difference x64: both are right: the court-sections blocks each side imported exist and apply; union kept (b1 to b5, b16, b17, b20, b21, b28, b38, t3, t5, x4, n1, n6).
difference x65: settled by the text: civil-code 122 ¶1 makes restitution the effect of rescission, so the rescission notice rule (formal-notice.p2) covers it; formal-notice.p3 (B), which would warn a seller of 392 ¶1 rescinded «دون سابق انذار», is not imported; A's doc3 and doc4 kept.
difference x66: both are right: the ordinary-court-exits blocks of both exist; union kept (x1, x4, x6, t2, n6, n7, b29, b30, b53, b54, b55).
difference x67: not settled by the text: whether civil-code 310 and 312 (periods d6, d7, A only) reach a rescission claim by a professional or a trader is not said; the protective reading imports them (they only warn); open point o4.
difference x68: not settled by the text: claim_is_professional_fees follows periods.d6 (x67); kept with open point o4.
difference x69: not settled by the text: claim_is_trader_supply_or_hotel follows periods.d7 (x67); kept with open point o4.
difference x70: settled by the text: contract_kind is not needed once civil-code 553 ¶1 (x50) and cpca 39 ¶4 (x54) are read; B's proposed slot dropped.
difference x71: both are right: contract_sets_notice_terms is read by formal-notice.n10, imported by both; B only omitted it from its slot list.
difference x72: both are right: criminal_case_on_same_facts is read by ordinary-court-exits.x6, imported by both.
difference x73: both are right: damage_caused_by_offence, same exit as x72.
difference x74: both are right: debt_due_date starts periods.d1, imported by both.
difference x75: both are right: defect_correction_impossible maps to works_correction_impossible (same fact, x51).
difference x76: settled by the text: civil-code 553 ¶1 allows rescission only «إذا انقضى هذا الأجل»; B's defect_correction_notice_given (warning given) would call the step done before the time ran out; mapped to A's works_correction_warning_expired.
difference x77: settled by the text: civil-code 379 ¶1 and 383 ¶1 make delivered-but-defective goods the warranty action; B's slot kept for boundary n3.
difference x78: settled by the text: civil-code 366 ¶1 runs «من وقت تسليم المبيع تسليما فعليا»; B's slot kept as d1's start.
difference x79: both are right: national_public_body_party is read by court-sections.x4 (A's import, kept in the union).
difference x80: both are right: notice_before_petition_day is read by formal-notice p1, p2 and n12, imported by both.
difference x81: both are right: obligation_is_tort_compensation is read by periods.d1 and ordinary-court-exits.x6, imported by both.
difference x82: settled by the text: petition_lodged_for_publication is p1's own slot (cpca 17 ¶3 «ما لم يثبت إيداعها للإشهار»), x49.
difference x83: settled by the text: civil-code 123 ¶1 lets the defendant withhold performance while the plaintiff's due obligation is unperformed; B's slot kept for boundary n14.
difference x84: settled by the text: cpca 17 ¶3 reaches a property «مشهر»; property_registered kept for p1 and doc4.
difference x85: both are right: public_body_party is read by ordinary-court-exits.x1, imported by both.
difference x86: both are right: public_vehicle_damage, same exit as x85.
difference x87: settled by the text: civil-code 366 ¶1 and 365 ¶1 concern only a shortfall in quantity; B's slot kept as d1's condition.
difference x88: both are right: return_of_thing_known_stolen_or_unduly_received is read by formal-notice.p1, imported by both.
difference x89: both are right: specialised_commercial_matter is read by ordinary-court-exits x4 and t2, imported by both.
difference x90: both are right: works_correction_impossible, same fact as x51.
difference x91: settled by the text: civil-code 553 ¶1 («فإذا انقضى هذا الأجل»), as x76; A's slot kept.
difference x92: settled by the text: civil-code 553 ¶1, as x50; A's slot kept.
difference x93: settled by the text: on A's ex1, B's record asked contract_kind (dropped, 553 ¶1) and three facts that 366 ¶1, 379 ¶1 and 123 ¶1 make matter; ex1 now states them and asks nothing.
difference x94: settled by the text: A's ex2 now expects the nine questions the merged own items ask (p1, p2, d1, n2, n3, n8, n14 and the section), each resting on its paragraph.
difference x95: settled by the text: ex7 as x93; p2 is silent because works_performed_defectively is no (553 ¶1), and the B-only facts are given.
difference x96: settled by the text: ex8 as x95.
difference x97: settled by the text: ex9 as x95.
difference x98: settled by the text: ex19 as x95.
difference x99: settled by the text: ex20 as x95.
difference x100: settled by the text: ex10, defective works, no warning, correction possible: civil-code 553 ¶1 and ¶2 give warn (A); B's record asked contract_kind.
difference x101: settled by the text: ex11, correction impossible: 553 ¶2 exempts: none (A).
difference x102: settled by the text: ex12, warning unknown: the step's own slot is asked (A).
difference x103: settled by the text: ex21, warning given and time passed: done (A), 553 ¶1.
difference x104: settled by the text: ex22, exemption unknown: asked (A), 553 ¶2.
difference x105: settled by the text: ex13, 392 ¶1 boundary n2 warns; p2 silent (553 ¶1); B-only facts given.
difference x106: settled by the text: ex14 as x95.
difference x107: settled by the text: ex18 as x95.
difference x108: settled by the text: on B's ex1, A's record had no d1 (366 ¶1 supplies it), asked works_performed_defectively (now given, 553 ¶1) and the d6/d7 slots (x67, imported); exB1 updated.
difference x109: settled by the text: exB2, shortfall a year gone: civil-code 366 ¶1 gives d1 warn (B).
difference x110: settled by the text: exB3, a few days late: d1 warns inside its band (366 ¶1).
difference x111: settled by the text: exB4, well within the year: d1 none (366 ¶1).
difference x112: settled by the text: exB5, delivery date unknown: d1 asks goods_delivery_date (366 ¶1).
difference x113: settled by the text: exB6, defective works and no warning: p2 warns (553 ¶1); the case now states works_performed_defectively instead of contract_kind.
difference x114: settled by the text: exB7, warning given and time passed: done (553 ¶1, the merged slot holds both).
difference x115: settled by the text: exB8, correction impossible: exempt (553 ¶2).
difference x116: settled by the text: exB9, warning unknown: asked (553 ¶1).
difference x117: settled by the text: exB10, defective goods delivered: n3 warns (379 ¶1, 383 ¶1).
difference x118: settled by the text: exB11, plaintiff has not performed: n14 warns (123 ¶1).
difference x119: settled by the text: exB12, restitution only: p2 and d1 silent (553 ¶1 and 366 ¶1 concern a rescission request).
difference x120: settled by the text: exB13, nothing known: the reconciled set of nine questions follows the merged own items; contract_kind dropped (553 ¶1).
difference x121: settled by the text: A's ex2 under the boundary run expected too few questions; the reconciled ex2 lists the record's own questions.
difference x122: settled by the text: civil-code 102 ¶2 («وتسقط دعوى البطلان بمضي خمسة عشرة سنة من وقت إبرام العقد») gives a deadline when a nullity request is added; d2 added; bc1 passes (last day 2026-06-15).
difference x123: settled by the text: bc2 as x122; d2 warns inside its band.
difference x124: settled by the text: bc3 as x122.
difference x125: settled by the text: bc4 as x122.
difference x126: settled by the text: bc5 as x122.
difference x127: settled by the text: bc6 as x122 (last day 2026-06-12, no extension).
difference x128: settled by the text: civil-code 101 ¶2 («إذا انقضت عشر (10) سنوات من وقت تمام العقد»); d3 added; bc7 passes (last day 2026-02-28).
difference x129: settled by the text: bc8 as x122: the period runs from the contract, last day 2025-12-01.
difference x130: settled by the text: bc13: civil-code 392 ¶1 is a further case «دون سابق انذار» that the case leaves unknown, so formal-notice.p2 rightly asks; case disputed below.
difference x131: settled by the text: bc18: civil-code 181 ¶3 exempts when «محل الالتزام تعويضا ترتب عن عمل مضر», which a rescission for an unperformed contract term (119 ¶1) is not; the module's p2 does not cite 181 ¶3, so nothing matches; case disputed below.
difference x132: settled by the text: bc20: civil-code 119 ¶1 («بعد اعذاره») supports the case's stepMissing; formal-notice.p2 returns none and leaves the warning to formal-notice.n12, a module design reported below.
difference x133: settled by the text: bc22: civil-code 120 ¶1 lets the contract fix the notice, so the step is not done while formal-notice.n10 asks contract_sets_notice_terms; the module's p2 says done (owner's rule of 14 November); reported as a module check.
difference x134: settled by the text: bc23: cpca 800 (ordinary-court-exits.x1, imported) sends the case to the administrative court; the case's «about» (civil-code 119 ¶1) does not govern jurisdiction; case disputed below.
difference x135: settled by the text: B's ex13 under the boundary run; same as x120.
difference x136: settled by the text: bc1 on B, as x122.
difference x137: settled by the text: bc2 on B, as x122.
difference x138: settled by the text: bc3 on B, as x122.
difference x139: settled by the text: bc4 on B, as x122.
difference x140: settled by the text: bc5 on B, as x122.
difference x141: settled by the text: bc6 on B, as x122.
difference x142: settled by the text: bc7 on B, as x128.
difference x143: settled by the text: bc8 on B, as x122.
difference x144: settled by the text: bc13 on B, as x130; B's p3 is not imported (x65, 122 ¶1).
difference x145: settled by the text: bc18 on B, as x131.
difference x146: settled by the text: bc20 on B, as x132; p3 not imported.
difference x147: settled by the text: bc22 on B, as x133.
difference x148: settled by the text: bc23 on B, as x134.
difference x149: settled by the text: bc1, A against B: neither had a nullity deadline (102 ¶2, d2 added); their questions differed by contract_kind (dropped, 553 ¶1) and the B-only facts (kept, 366 ¶1, 379 ¶1, 123 ¶1).
difference x150: settled by the text: bc2, as x149.
difference x151: settled by the text: bc3, as x149.
difference x152: settled by the text: bc4, as x149.
difference x153: settled by the text: bc5, as x149.
difference x154: settled by the text: bc6, as x149.
difference x155: settled by the text: bc7, as x149 with 101 ¶2 (d3).
difference x156: settled by the text: bc8, as x149.
difference x157: settled by the text: bc9, A against B: B's extra finding is formal-notice.p3 (not imported, 122 ¶1); question sets as x149.
difference x158: settled by the text: bc10, as x157.
difference x159: settled by the text: bc11, as x157.
difference x160: settled by the text: bc12, as x157.
difference x161: settled by the text: bc13, as x157 and x130.
difference x162: settled by the text: bc14, as x157.
difference x163: settled by the text: bc15, as x157.
difference x164: settled by the text: bc16, as x157.
difference x165: settled by the text: bc17, as x157.
difference x166: settled by the text: bc18, as x157 and x131.
difference x167: settled by the text: bc19, as x157.
difference x168: settled by the text: bc20, as x157 and x132.
difference x169: settled by the text: bc21, as x157.
difference x170: settled by the text: bc22, as x157 and x133.
difference x171: settled by the text: bc23, as x157 and x134.

Counts: settled by the text 134; both are right 34; not settled by the text 3 (x67, x68, x69); needs a text not in the store 0 (no line appended to `law/requests.jsonl`).

## Proposed slots: mapping

- `works_performed_defectively` (A): kept. B's `contract_kind = works_contract` is not the same fact (553 ¶1 turns on defective performance in progress); B's `contract_kind` dropped (x70).
- `works_correction_warning_expired` (A) = `defect_correction_notice_given` (B): same step; A's id kept because it also holds the expiry of the time fixed (553 ¶1).
- `works_correction_impossible` (A) = `defect_correction_impossible` (B): same fact; A's id kept.
- `goods_delivery_date`, `quantity_shortfall_claimed`, `plaintiff_performed_or_offered`, `delivered_goods_defective` (B only): kept.

## Disputed boundary cases

disputed bc13: civil-code 392 ¶1 makes the sale of movables unpaid at the term rescinded «دون سابق انذار»; the case leaves movable_sale_price_unpaid_at_term unknown, so the text gives ask, not stepMissing.
disputed bc18: civil-code 181 ¶3 exempts only when the object of the obligation is compensation for a harmful act; a rescission rests on an unperformed contract term (119 ¶1), so the text gives the step missing (subject to the other exemptions), not unsettled.
disputed bc23: cpca 800 (in the store, ordinary-court-exits.x1) gives the administrative courts the cases to which a public administrative body is a party; the text gives leavesOrdinaryCourt, and civil-code 119 ¶1, the case's «about», does not govern jurisdiction.

## What I changed where A and B had agreed, and other changes

- d1 (B): counting basis aligned with the periods module (periods.b11, periods.b27 instead of an own 314 statement), band widened to 16 days as the module's one-year deadlines, and the module's two counting readings carried.
- p2 (553 step): both had it warn/ask and interpretive; its own slot now means "warned and the time ran out" (owner's rule on "done").
- Boundary n5 (nullity, both had it as a note) now warns when the requests include `annulment`; deadlines d2 (102 ¶2) and d3 (101 ¶2) added for bc1 to bc8. Neither derivation had them: graded addedAfterDerivation, they only warn.
- A's boundary n4 (quantity shortfall "outside") removed: 366 ¶1 makes it a rescission of this claim (d1).
- Statements of B that restate module statements replaced by imports: 120, 121, 164, 179, 186, 558 (formal-notice), 308, 314, 315 (periods), 515, 519 (court-sections). Imported formal-notice.b5 (180 ¶1) and periods.b13 (315 ¶2) to answer neighbour flags.
- Added b61 (333 ¶2) to answer the flag on 333 ¶2.
- Corrected examples: A's examples now state the three B-only facts (ex1 otherwise asked them); A's ex2 and B's ex13 now list the nine questions; B's examples use works_performed_defectively and the merged slot names.

## Differences between this unit and the modules (checks of the modules)

- formal-notice.p2 says the step is done while formal-notice.n10 still asks whether the contract sets the notice terms (bc22): against the owner's rule of 14 November on "done".
- formal-notice.p2 neither cites nor reads civil-code 181 ¶3, so bc18 matches nothing; the protective result (ask or warn) is reached only by its other exemptions.
- formal-notice.p2 returns none for a notice dated after the petition (bc20) and leaves the warning to n12; the case's stepMissing is what 119 ¶1 says.
- formal-notice.p3 is not imported: for this claim, restitution is the effect of rescission (122 ¶1), and p3 lacks the 392 ¶1 exemption that p2 has.
- court-sections.t3 (real-estate territory) does not mention works relating to real property (cpca 40 ¶2); the own rule t5 is kept.
- B's d1 counted under 314 alone; the periods module adds civil-code 3 and keeps two readings; aligned to the module.

## Questions left to the adversary

1. Do the nullity deadlines d2 and d3 belong in this claim record, keyed on the request kind `annulment` (no slot holds the ground of nullity, so d3 may warn on an incapacity case where 101 ¶2's ten-year bar does not apply)?
2. p1 declares blockCleanExport, non-interpretive, on cpca 17 ¶3 for a petition that is necessarily lodged after it is drafted; does «مشهر» qualify the property as well as the real right?
3. Should periods d6 and d7 (310, 312) be imported for a rescission claim (x67)?
4. Is excluding formal-notice.p3 for the restitution request right on 122 ¶1?
5. Does the 553 warning reach a contractor who stopped work altogether (o5)?
