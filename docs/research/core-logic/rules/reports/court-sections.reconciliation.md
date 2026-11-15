# Reconciliation of court-sections

Reconciler: claude-opus-5-5, 15 November 2026. Record: `rules/reconciled/court-sections.json` (stage `reconciled`, `verified: false`). Inputs: derivations A and B (quoted copies), derivation C and its comparison, the difference list (118 entries), the id map, the merged checklist, the neighbour flags, the value reports, the boundary cases, and the article store (text views).

Counts: 74 settled by the text, 25 both are right, 19 not settled by the text, 0 needing a text that is not in the store. Two boundary cases disputed (bc7, bc13). Three text requests appended to `law/requests.jsonl` for the open points of grade D and for the definition of commercial acts.

## The differences

difference x1: settled by the text: civil-code 317 ¶1 says a court claim interrupts limitation «ولو رفعت الدعوى الى محكمة غير مختصة»; B's statement is kept as b49 (interpretive as to a wrong section of a competent court, open point o8) and answers checklist k42.
difference x2: settled by the text: cpca 15 ¶1-¶2 require the petition to name «الجهة القضائية التي ترفع أمامها الدعوى», the court and not the section; A's statement is kept as b27, which shows that a wrong section is no formal defect of the petition.
difference x3: settled by the text: cpca 299 ¶1 brings urgent matters by petition before the court where the problem or measure arises; A's statement is kept as b29, basis of boundary n2.
difference x4: settled by the text: cpca 32 ¶1 sits under the heading «في الاختصاص النوعي للمحاكم» and describes how the court is made up, a procedural rule that grounds no claim; role procedure (A).
difference x5: settled by the text: cpca 32 ¶2 («يمكن أيضا أن تتشكل من أقطاب متخصصة») describes the court's make-up, a procedural rule; role procedure (A).
difference x6: settled by the text: cpca 32 ¶3 states the court's general jurisdiction, a rule of procedure that grounds no claim; role procedure (A).
difference x7: settled by the text: cpca 36 ¶1's words are exactly what both statements say; whether the split between sections is subject-matter jurisdiction is the reading of open point o1, not of the statement, so the statement is not interpretive (A).
difference x8: settled by the text: cpca 40 ¶2 sends «دعاوى الإيجارات بما فيها التجارية المتعلقة بالعقارات» to the court of the property; B's statement is kept as b48, with the chapeau ¶1 («ترفع الدعاوى ... دون سواها») that the list item needs; basis of t3, n3 and of a reading in o2.
difference x9: both are right: A's 423 ¶1-¶2 and B's ¶1-¶3 say the same thing; A's narrower cuts are kept (b7 for ¶1-¶2, b8 for ¶1 and ¶3).
difference x10: both are right: B's 423 ¶1-¶3 equals A's b7 plus b8; merged into them.
difference x11: both are right: A's 423 ¶1 and ¶3 (maintenance, custody, visitation) is kept as b8; it is the middle of B's b7.
difference x12: both are right: A's 423 ¶1, ¶4, ¶5 is kept as b9; it carries items 3 and 4 of B's b8 with their chapeau.
difference x13: both are right: A's 423 ¶1 and ¶6 is kept as b10; it carries item 5 of B's b8 with its chapeau.
difference x14: settled by the text: 423 ¶4-¶6 are list items whose subject («ينظر قسم شؤون الأسرة») stands only in ¶1, so B's cut without ¶1 does not carry the rule; merged into A's b9 and b10, which cite ¶1.
difference x15: settled by the text: 426 ¶4-¶6 fix the family courts by place only under the chapeau ¶1 («تكون المحكمة مختصة إقليميا»); B's statement is kept as b42 with ¶1 added, basis of t4.
difference x16: both are right: A's 500 ¶1-¶3 and ¶1, ¶4-¶8 together equal B's ¶1-¶8; A's narrower cuts are kept (b11, b12).
difference x17: both are right: B's 500 ¶1-¶8 is merged into b11 and b12.
difference x18: both are right: A's 500 ¶1, ¶4-¶8 is kept as b12.
difference x19: settled by the text: 501 ¶1 gives the social dispute to the court where the employment contract was made or performed or where the defendant lives; B's statement is kept as b40, basis of t1.
difference x20: settled by the text: 501 ¶2 («غير أنه ... بسبب حادث عمل أو مرض مهني») is the exception to ¶1; B's statement is kept as b41, basis of t2; it also answers neighbour flag f-b10-p2.
difference x21: settled by the text: 502 ¶1 sets the social section's composition «تحت طائلة البطلان»; A's statement is kept as b13 (checklist k10, k24).
difference x22: settled by the text: 510 ¶1 admits additional requests before the social section at every stage; B's statement is kept as b46 (checklist k45, open point o3).
difference x23: both are right: A's 512 ¶1 and ¶10 (partition) is kept as b18; it is the tail of B's b13.
difference x24: both are right: A's 512 ¶1, ¶11, ¶12 and B's ¶11 and ¶12 say the same; split as B did into b19 (¶1, ¶11) and b36 (¶1, ¶12), each with A's chapeau, because only ¶11 bears on the commercial-lease reading.
difference x25: both are right: A's 512 ¶1-¶9 is kept as b17; it is the head of B's b13.
difference x26: both are right: B's 512 ¶1-¶10 is merged into b17 and b18.
difference x27: settled by the text: 512 ¶11 is a list item whose subject («ينظر القسم العقاري») is in ¶1; kept as b19 with ¶1.
difference x28: settled by the text: 512 ¶12 is a list item whose subject is in ¶1; kept as b36 with ¶1.
difference x29: settled by the text: 513 ¶1 gives the real-estate section disputes on agricultural land of the national domain; B's statement is kept as b37 (checklist k12, k33).
difference x30: settled by the text: 515 ¶1's words list «إبطال أو فسخ أو تعديل أو نقض» and the statement says exactly that; the store records no French mismatch for 515, so the statement is not interpretive (A).
difference x31: settled by the text: 518 ¶1 gives real-property matters to the court where the property is; B's statement is kept as b39, basis of t3.
difference x32: settled by the text: 519 ¶1 brings the action before the real-estate section subject to the rules on publishing actions on published deeds; B's statement is kept as b38, basis of boundary n6.
difference x33: settled by the text: the law-08-09 wording of 531 ¶1 gave the commercial section commercial and, where relevant, maritime disputes; with law-22-13 13 ¶2 it bears on the time before installation; A's statement is kept as b22 (checklist k15, k28, k51; open point o4).
difference x34: settled by the text: 532 ¶1 applies the code's and the special territorial rules to the commercial section; B's statement is kept as b43, basis of t5.
difference x35: settled by the text: 536 bis 1 ¶1 applies the code's territorial rules to the specialised commercial court; B's statement is kept as b44, basis of t5.
difference x36: settled by the text: 536 bis 6 ¶2 gives a section president of the specialised commercial court urgent provisional or protective powers; B's statement is kept as b45 and added to boundary n2.
difference x37: settled by the text: 536 bis ¶1 attributes disputes («تختص المحكمة التجارية المتخصصة بالنظر في المنازعات») as 423 ¶1 and 500 ¶1 do, which both derivations call basis; the exclusion («باستثناء») is in 531 ¶1; role basis (B).
difference x38: settled by the text: cpca 7 ¶1 makes hearings public unless publicity harms public order, morals or «حرمة الأسرة»; A's statement is kept as b35 (checklist k8).
difference x39: settled by the text: 800 ¶2 gives the administrative courts every case in which the State, a wilaya, a commune or a public administrative body is a party; B's statement is kept as b47, basis of exit x1.
difference x40: settled by the text: family-code 3 مكرر ¶1 makes the public prosecutor a principal party in every case applying the Family Code; A's statement is kept as b34 (checklist k8); its store note gets line k53.
difference x41: settled by the text: labour-disputes 20 ¶1-¶2 give the courts sitting in social matters the individual labour disputes; A's statement is kept as b14, basis of the social alternative.
difference x42: settled by the text: labour-disputes 8 ¶1-¶3 set the social composition and the replacement of absent assessors; A's statement is kept as b15 (checklist k10).
difference x43: settled by the text: law-22-13 13 ¶1 runs the specialised commercial courts' jurisdiction rules from their installation; A's statement is kept as b24 and added to the basis of exit x2.
difference x44: settled by the text: law-22-13 13 ¶2 keeps cases filed before installation with the commercial sections; A's statement is kept as b25 (exit x2, section alternative, open point o4).
difference x45: settled by the text: law-22-13 14 ¶2 repeals 32 ¶7-¶10 on the specialised poles; A's statement is kept as b26 (checklist k16, k28).
difference x46: not settled by the text: 32 ¶4 lists cases by the nature of the dispute, but no paragraph names a residual section in a court that has sections; civil is kept as the residual proposal (A, supported by C), the section rule is interpretive, and B's choice to propose nothing is kept as a reading of open point o5.
difference x47: not settled by the text: 32 ¶4-¶5, 423 ¶2-¶3, 500 ¶3, 512 ¶10 and 531 ¶1 carry most alternatives, but the commercial-lease alternative (512 ¶11 against 531 ¶1, o2), housing and partition (o5) and the reach of 32 ¶5 (o7) are readings; A's alternatives are kept with the slot of x58, the rule stays interpretive, and B's warnings (n3, x3) stand beside it.
difference x48: settled by the text: 501 ¶1 states the rule of t1; B's territory rule is kept.
difference x49: settled by the text: 501 ¶2 states the rule of t2; B's territory rule is kept.
difference x50: settled by the text: 518 ¶1 and 40 ¶1-¶2 state the rule of t3; B's territory rule is kept, its wording now also naming leases of immovables, commercial ones included.
difference x51: settled by the text: 426 ¶1, ¶4-¶6 state the rule of t4; B's territory rule is kept.
difference x52: settled by the text: 532 ¶1 and 536 bis 1 ¶1 state the rule of t5; B's territory rule is kept.
difference x53: settled by the text: 800 ¶2 gives the administrative courts the cases in which a public administrative body is a party, in both wordings for the four bodies the slot names; B's exit x1 is kept, with 802 ¶1-¶3 (the ordinary courts keep road offences and liability for damage by a public vehicle) added to its basis and message, and 32 ¶3 (the general jurisdiction it carves out) added to its basis.
difference x54: settled by the text: 531 ¶1 excepts from the commercial section the disputes of 536 bis ¶1-¶7; B's exit x2 is kept (A had only a boundary without a condition, n2, now dropped as a duplicate), interpretive because law-22-13 13 ¶1 defers it to an installation the store does not record (open point o4, grade D); its message now names only article 531, which clears B's value-report problem on «536 bis».
difference x55: settled by the text: 32 ¶5 keeps every dispute «باستثناء القضايا الاجتماعية» with the civil section, so for non-social cases B's exit says what A's section alternative already proposes, and for social cases the paragraph names no section; x3 is kept, narrowed to social requests, and interpretive (open point o7).
difference x56: both are right: 32 ¶6 (referral through the registry) is carried by A's section rule, whose warning fires when the lawyer's section differs from the one proposed, and by boundary n1; B's exit x4 says the same thing through a slot asking the lawyer to judge the very section the rule computes; merged into the section rule's message and n1.
difference x57: both are right: B's case_listed_before_other_section was read only by x4, which is merged into the section rule (x56); the slot is dropped.
difference x58: both are right: B's court_has_section_for_matter and A's court_has_sections hold the same fact for 32 ¶5; B's id is kept with a wording that covers both readings of «لم تنشأ فيها الأقسام» (open point o7).
difference x59: both are right: the mapping of x58; A's court_has_sections is replaced by court_has_section_for_matter in the section rule and in A's examples.
difference x60: settled by the text: 511 ¶1 and 512 give the real-estate section the disputes about real property and no existing slot holds that fact; A's proposed slot dispute_concerns_real_property is kept.
difference x61: settled by the text: 531 ¶1 gives the commercial section commercial disputes and no existing slot holds that fact; A's proposed slot dispute_is_commercial is kept.
difference x62: settled by the text: 800 ¶2 turns on whether a public body is a party; the existing slot public_body_party is kept with exit x1.
difference x63: settled by the text: 423 ¶2-¶3, 500 ¶3 and 512 ¶10 tie sections to kinds of claim, which the record reads from the requests; requests is kept (section rule, x3, n7).
difference x64: settled by the text: 536 bis ¶1-¶7 list the specialised court's subjects and no existing slot holds that fact; B's proposed slot specialised_commercial_subject is kept with exit x2.
difference x65: settled by the text: 501 ¶2 turns on a work accident or occupational illness; the existing slot work_accident_or_occupational_illness is kept with t1 and t2.
difference x66: not settled by the text: follows x46 and x47; the reconciled record proposes the civil section for this case (A's ex1 kept), where B proposed none.
difference x67: not settled by the text: follows x47; the social section is proposed (500 ¶3), and x3 now warns because 32 ¶5 names no section for social cases (A's ex2 updated).
difference x68: not settled by the text: follows x47; 32 ¶5 gives the civil section a divorce in a court without the section (A's ex3 kept, x3 silent).
difference x69: not settled by the text: follows x47; the family section is proposed (423 ¶3), A's ex4 kept.
difference x70: not settled by the text: follows x47 and o2; the commercial section is proposed and n3 warns with both readings (A's ex5 updated).
difference x71: not settled by the text: follows x47; the real-estate section is proposed (512 ¶11), A's ex6 kept.
difference x72: not settled by the text: follows x47; the commercial section is proposed (531 ¶1), A's ex7 kept.
difference x73: not settled by the text: follows x47 and o5; the real-estate section is proposed for a partition (512 ¶10), A's ex8 kept.
difference x74: not settled by the text: follows x46; the residual civil section is proposed, A's ex9 kept.
difference x75: settled by the text: follows x53-x56; B's ex1 is kept as ex10, with dispute_is_commercial yes and dispute_concerns_real_property no so that the commercial section is proposed (531 ¶1) and nothing is asked; x4 is gone (x56).
difference x76: settled by the text: follows x54; B's ex2 is kept as ex11, x2 warns.
difference x77: settled by the text: follows x55; B's ex3 is kept as ex12 with a wage request, and x3 warns under 32 ¶5's social exception.
difference x78: both are right: follows x56; B's ex4 is rewritten as ex13: the family section is proposed (423 ¶3), and a listing before the civil section is the referral case of 32 ¶6 carried by the section rule's warning.
difference x79: settled by the text: follows x53; B's ex5 is kept as ex14, x1 warns (800 ¶2).
difference x80: not settled by the text: follows x47 and o2; B's ex6 is kept as ex15, n3 warns and the commercial section is proposed.
difference x81: settled by the text: follows x55 and x56; B's ex7 is kept as ex16: x3 no longer asks when no social request is made (32 ¶5's exception concerns social cases only), x4 is gone, and the section rule's questions are added.
difference x82: settled by the text: 536 bis 4 ¶3 requires the record of non-conciliation with a petition before the specialised commercial court on pain of formal inadmissibility, so bc1's outcome is right; A and B both left this conciliation step to the conciliation module and the reconciled record still holds no prior step for it (C's priorSteps attack is with the verifier).
difference x83: settled by the text: 536 bis 4 ¶3 makes the missing record a ground of formal inadmissibility, so bc2's outcome is right; same scope as x82, no prior step in this record.
difference x84: settled by the text: 536 bis 4 ¶3 makes the record a condition of the petition, so an unknown record calls for a question (bc3); same scope as x82.
difference x85: settled by the text: 536 bis 4 ¶3 requires the petition to be filed «مرفقة ... بمحضر عدم الصلح», so a record delivered after the petition date cannot be attached (bc4); same scope as x82.
difference x86: settled by the text: 536 bis 4 ¶3 asks only that the record accompany the petition, so a record delivered on the petition day suffices (bc5); same scope as x82.
difference x87: settled by the text: 536 bis 4 ¶3 makes the step complete only when the record can be attached, so an unknown delivery date calls for a question (bc6); same scope as x82.
difference x88: not settled by the text: bc7 (536 bis ¶3): A had only a boundary without a condition; the reconciled exit x2 asks, because whether the company is commercial (536 bis ¶3 names «الشركات التجارية») and whether the court is installed (law-22-13 13 ¶1) are not known; bc7 is disputed below.
difference x89: settled by the text: 439 ¶1 makes reconciliation compulsory in divorce, but 436 ¶1 and 440 ¶1 place it before the judge after the petition is filed, so no step precedes filing; both records rightly hold none, and bc8's outcome agrees with the text.
difference x90: settled by the text: 423 ¶3 gives maintenance and custody to the family section and no text in the store requires a record before filing them; both records rightly hold no step, and bc9's outcome agrees with the text.
difference x91: not settled by the text: bc10 sets 500 ¶1, ¶3 (exclusive social jurisdiction) against 536 bis ¶4 (bankruptcy); exit x2 (kept from B) asks, and open point o11 records both readings.
difference x92: not settled by the text: bc11, as x91; exit x2 asks when the specialised subject is not known.
difference x93: not settled by the text: bc12: 500 ¶2 covers employment contracts, not the public statute, and 800 ¶2 gives the administrative courts cases with a public administrative body; the new boundary n7 warns (open point o10).
difference x94: settled by the text: bc13: 800 ¶2 sends cases in which a public administrative body is a party to the administrative courts; exit x1, whose basis now includes 32 ¶3, warns; bc13's outcome is disputed below.
difference x95: settled by the text: as x82 (bc1 on B's record).
difference x96: settled by the text: as x83 (bc2 on B's record).
difference x97: settled by the text: as x84 (bc3 on B's record).
difference x98: settled by the text: as x85 (bc4 on B's record).
difference x99: settled by the text: as x86 (bc5 on B's record).
difference x100: settled by the text: as x87 (bc6 on B's record).
difference x101: not settled by the text: as x88: B's exit x2 asks where bc7 wants a warning, because law-22-13 13 ¶1 defers the rule to an installation the store does not record.
difference x102: settled by the text: as x89 (bc8 on B's record).
difference x103: settled by the text: as x90: bc9 matches only the family boundary n5 (B's n3), which is no prior step, rightly.
difference x104: not settled by the text: as x93 (bc12 on B's record): B's wage boundary had no condition; the new n7 warns.
difference x105: settled by the text: as x94 (bc13 on B's record): x1 cited only 800, not 32 ¶3; its basis now includes 32 ¶3.
difference x106: both are right: bc1, both records: the different questions come from A's section rule and B's exits, and the reconciled record asks the union under the slot mapping of x58 (x47, x53-x56).
difference x107: both are right: bc2, both records: as x106.
difference x108: both are right: bc3, both records: as x106.
difference x109: both are right: bc4, both records: as x106.
difference x110: both are right: bc5, both records: as x106.
difference x111: both are right: bc6, both records: as x106.
difference x112: settled by the text: bc7, both records: only B had the exit for 536 bis; kept as x2 (x54).
difference x113: both are right: bc8, both records: the questions differ only as the section rule and the exits differ; the reconciled record asks the union (x106).
difference x114: both are right: bc9, both records: as x113.
difference x115: settled by the text: bc10, both records: only B's x2 matched 536 bis ¶4 and asked; kept (x54, x91).
difference x116: settled by the text: bc11, both records: as x115.
difference x117: not settled by the text: bc12, both records: neither had a rule for a civil servant; n7 added (x93, o10).
difference x118: settled by the text: bc13, both records: only B had the administrative-court exit; kept as x1 (x53, x94).

## Mapping of proposed slots

| Fact | Derivation A | Derivation B | Kept id |
|---|---|---|---|
| The court has (not) created the section concerned, or any sections (cpca 32 ¶5) | court_has_sections | court_has_section_for_matter | court_has_section_for_matter (question widened: "no" also when the court has no sections) |
| The dispute concerns real property (511, 512, 515) | dispute_concerns_real_property | none | dispute_concerns_real_property |
| The dispute is commercial (531) | dispute_is_commercial | none | dispute_is_commercial |
| The dispute is a subject of the specialised commercial court (536 bis) | none | specialised_commercial_subject | specialised_commercial_subject |
| The case is or will be listed before the wrong section (32 ¶6) | none | case_listed_before_other_section | dropped with exit x4 (merged into the section rule, x56) |

Existing slots now read: requests, commercial_lease, public_body_party, work_accident_or_occupational_illness, and worker_is_public_servant (new boundary n7).

## Boundary cases

disputed bc7: law-22-13 13 ¶1 makes the jurisdiction rules of the specialised commercial courts run only from their installation, which no text in the store records, and 536 bis ¶3 covers «الشركات التجارية» only while the case gives nothing but a dissolution request; the text gives a question (exit x2 asks), not leavesOrdinaryCourt.
disputed bc13: 800 ¶2 gives the administrative courts «جميع القضايا» in which a public administrative body is a party (the exceptions of 802 ¶1-¶3, road offences and public-vehicle damage, do not reach a rescission claim), so the text settles the case: leavesOrdinaryCourt, not unsettled; exit x1 warns either way, so the case passes.

Not disputed, but they still fail on the reconciled record: bc1 to bc6 (536 bis 4 ¶3) match no item because the specialised commercial court's pre-filing conciliation is not held here (see x82 and the first question below); their outcomes agree with the text. bc8 (439 ¶1) and bc9 (423 ¶3) agree with the text: no step precedes filing a divorce or a custody and maintenance claim. The record has no prior step, so the matcher finds no step to test. bc10 to bc13 pass.

## What changed where A and B had agreed

- Both wrote the fifth paragraph of article 32 their own way: A "no sections", B "some sections". The words are «المحاكم التي لم تنشأ فيها الأقسام»; b4 now says "the sections" and the two readings went to open point o7.
- Both left the record without deadlines and prior steps. I re-read the paragraphs around the section chapters: 442 ¶2 (three months of reconciliation, after filing), 504 ¶1 (six months after delivery of the non-conciliation record, labour-prelitigation's), 524 ¶2 (possession actions within a year, a claim's), 536 bis 4 ¶1 and ¶3 (conciliation before registration at the specialised commercial court). None of them is a rule on sections, so the record still has none. C's attack on prior steps and on the 536 bis 4 periods is the verifier's. The text does set a pre-filing step with an inadmissibility sanction in 536 bis 4 ¶3. It is in scope only if this module is meant to hold the specialised court's conciliation.
- Both cited 425 ¶1. C did not. The paragraph gives the president of the family section the powers of the urgent-matters judge, so A and B are right (b30).
- Both derivations' statements on 32, 36, 425, 506, 511, 515, 521, 531, 536 and 536 bis were re-read against their paragraphs and neighbours. They hold. The in-force 32 ¶2 (poles) stays although ¶7 to ¶10 were repealed (law-22-13 14 ¶2).
- Added items that neither A nor B had, so each fails condition 4 with addedAfterDerivation:
  - statement b50 (802 ¶1-¶3) in exit x1;
  - boundary n7 (civil servant's wage or reinstatement claim);
  - open points o10 and o11;
  - checklist lines k51 to k58: the three store-note lines (k51 to k53), B's seats line under a new number (k54), and the four neighbour-flag lines (k55 to k58).
- Neighbour flags:
  - f-b1-p5 (32 ¶5): cited by b4.
  - f-b10-p2 (501 ¶2): cited by b41.
  - f-b22-previous (531 ¶1): cited by b21.
  - f-b24-previous (425 ¶1): cited by b30.
  - f-b14-next (labour-disputes 21 ¶1): answered by k55.
  - f-b35-previous (cpca 6 ¶1): answered by k56.
  - f-b34-previous (civil-code 316 ¶1): answered by k57.
  - f-b27-p10 (cpca 40 ¶10): answered by k58.
- Examples:
  - A's ex1 to ex9 are kept with the slot renamed. ex2, ex3, ex5, ex6 and ex9 now also state findings of x3, n3 and n7.
  - B's ex1 to ex7 are kept as ex10 to ex16 with these corrections. ex10 gains two slots and the commercial section. ex12 gains a wage request, which x3 needs. ex13 replaces the x4 case (exit merged). In ex16, x3 gives none instead of ask: with no social request, 32 ¶5's exception is not in play. ex16's questions also drop B's dropped slot and add the section rule's slots.
  - Reconciler examples ex17 to ex21 cover the settled differences: the narrowed x3, the new n7 (warn and ask), x1 with 802, and x2 asking.

## C's mechanical differences that the text settles

- C cites the general territorial rules 37 to 47 and all of 426. The texts support them, but they are the territorial-jurisdiction module's. This record keeps only the territorial rules written in the section chapters (t1 to t5).
- C's 536 bis 4 ¶1 periods (five days, three months) bind the president and the conciliating judge, not the lawyer's petition.
- C's 442 ¶2 period runs after filing.
- C's administrative-court gap (800 not given to C) is filled by x1.

## Questions left to the adversary

1. Should this module hold the specialised commercial court's pre-filing conciliation (536 bis 4 ¶1 and ¶3, inadmissibility) as a prior step? Six boundary cases and derivation C say so. A and B left it to conciliation-mediation, and the existing slot nonconciliation_record is worded for the labour conciliation office.
2. Is proposing the commercial section for a lease of business premises (o2) the better proposal? Boundary n3 warns in every such case, but the section rule still proposes one side.
3. Exit x1 and territory rules t1 to t5 may duplicate the ordinary-court-exits and territorial-jurisdiction modules. Should a claim import them from here or from there?
4. The social alternative and x3 fire only on the request kinds wages and reinstatement. A dismissal claim that asks only for damages gets the residual civil proposal. Should the labour claims write their own section rule?
5. Open point o7: does 32 ¶5 reach a court that lacks only the section concerned? The merged slot assumes it may.
