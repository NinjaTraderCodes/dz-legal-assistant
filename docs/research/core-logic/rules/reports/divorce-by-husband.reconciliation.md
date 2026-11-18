# Reconciliation of divorce-by-husband (claim)

Record: `rules/reconciled/divorce-by-husband.json` (stage `reconciled`, `verified: false`). `rules check --store`: OK. `rules examples`: 21 cases, 0 with differences. Request appended to `law/requests.jsonl`: civil-code 13.

Outcomes: 45 settled by the text, 16 both are right, 0 not settled by the text, 1 needs a text that is not in the store. Boundary cases disputed: 0 (no boundary-case file exists for this unit; see below).

## Differences

difference x1: settled by the text: civil-code 12 ¶2 submits "انحلال الزواج" to the husband's national law "وقت رفع الدعوى", which is this claim's own question when the husband is foreign; A's statement kept (b43, interpretive because the store marks the article unverified with commencement unsettled).
difference x2: needs a text that is not in the store: civil-code 13 is held only in its 1975 wording, whose note records that the Arabic refers to articles 11 and 12 and the French to 12 and 13; the wording in force after law 05-10 decides whether Algerian law alone governs the dissolution. Not decided: A's statement kept as interpretive (b44), open point o5 grade D, checklist k66 searchedNotFound, request appended.
difference x3: settled by the text: civil-code 338 ¶1 makes a final judgment binding "بين الخصوم أنفسهم ... نفس المحل والسبب", exactly the case of an earlier divorce judgment between the same spouses (boundary n6); A's b35 kept.
difference x4: settled by the text: civil-code 338 ¶2 "ولا يجوز للمحكمة أن تأخذ بهذه القرينة تلقائيا" is a separate rule the lawyer must know for n6; A's b36 kept.
difference x5: settled by the text: cpca 13 ¶1 conditions every claim on standing and interest, which is the standing line of the checklist (k6); B's statement kept (b46).
difference x6: settled by the text: cpca 436 ¶1 files the divorce petition "وفقا للأشكال المقررة لرفع الدعوى", and those forms are set by cpca 14 ¶1 (written, signed, dated, copies per party); B's statement kept (b47).
difference x7: settled by the text: the same reference of cpca 436 ¶1 reaches cpca 15 ¶1, which lists the petition's elements "تحت طائلة عدم قبولها شكلا"; B's statement kept (b48); the petition-formal-elements module has no final record to import.
difference x8: settled by the text: cpca 16 ¶3 (twenty days) and ¶4 (three months for a person abroad) govern the summons the plaintiff must serve under cpca 438 ¶1; B's statement kept, split into b49 (¶3) and b57 (¶4) because ¶4 extends ¶3.
difference x9: settled by the text: cpca 17 ¶1 "لا تقيد العريضة إلا بعد دفع الرسوم" applies to every petition, so the fees receipt is listed (doc5); B's statement kept (b50), A's checklist line k32 changed to applies.
difference x10: settled by the text: cpca 428 ¶1 states the single joint petition signed by both spouses, the telling fact both derivations wrote for the mutual-consent boundary; A's citation added to n3 (b29).
difference x11: settled by the text: cpca 440 ¶1 says how the judge holds the attempt (each spouse alone, then together), which answers checklist lines k11 and k42; B's statement kept (b51).
difference x12: settled by the text: cpca 442 ¶1 gives the reflection period and provisional measures "بموجب أمر غير قابل لأي طعن", answering k13 and k41; B's statement kept (b52).
difference x13: settled by the text: cpca 442 ¶2 "يجب ألا تتجاوز محاولات الصلح ثلاثة (3) أشهر" is a time limit (a ceiling on the judge's attempts), so its role is limit (B); it creates no deadline item, since it runs from the filing.
difference x14: settled by the text: cpca 450 ¶1 says what the judge does ("يتأكد القاضي ... ويأمر"), not the right that grounds the claim (family-code 48 ¶1), so its role is procedure (B); kept as required.
difference x15: settled by the text: cpca 452 ¶1 says cassation does not suspend execution of the divorce judgments of 450, answering k19 and k53; B's statement kept (b53).
difference x16: settled by the text: cpca 40 ¶1 opens "فضلا عما ورد في المواد 37 و38 و46", naming article 46 beside the exclusive rule, so cpca 46 ¶1 belongs to the territory rule; A's statement kept (b24, in t1).
difference x17: both are right: B's cpca 54 ¶1,2 and A's separate ¶1 and ¶2 say the same; merged as two statements (b33, b34) because ¶2 is a distinct rule (the judge's own motion).
difference x18: both are right: as x17; A's cpca 54 ¶1 is b33.
difference x19: both are right: as x17; A's cpca 54 ¶2 is b34.
difference x20: settled by the text: cpca 55 ¶1 defines connexity between cases before different formations or courts, the other reading of a wife's pending action (open point o9); B's statement kept (b54), and cpca 56 ¶1 added (b55) because the giving-up for connexity is in 56, not 55.
difference x21: settled by the text: cpca 994 ¶1 excludes "قضايا شؤون الأسرة" from the mediation the judge must offer, answering k15; A's statement kept (b22).
difference x22: settled by the text: family-code 16 ¶1 gives half the dowry "عند الطلاق قبل الدخول", a direct consequence of this claim (k22, k63); A's statement kept (b37).
difference x23: settled by the text: family-code 22 ¶2 says the judgment validating a marriage is registered at the prosecutor's initiative, the neighbour paragraph of the proof rule both cite and the document of the unregistered case (doc2, n1); A's statement kept (b45).
difference x24: settled by the text: family-code 32 ¶1 "يبطل الزواج" is a ground of nullity, a different action from divorce under 48, which the annulment boundary n4 needs; A's statement kept (b30).
difference x25: settled by the text: family-code 33 ¶1 (void for lack of consent) as x24; A's statement kept (b31), and 33 ¶2 (rescission before consummation) added to n4 (b56).
difference x26: settled by the text: family-code 47 ¶1 is the general rule that a marriage ends by divorce or death, which 48 ¶1 then details; A's statement kept (b2).
difference x27: settled by the text: family-code 50 ¶1 "ومن راجعها بعد صدور الحكم بالطلاق يحتاج إلى عقد جديد" marks the judgment as the point after which the marriage no longer subsists, which is how both records use it (the already-divorced boundary), so its role is limit (B).
difference x28: settled by the text: family-code 53 ¶1 "يجوز للزوجة أن تطلب التطليق" gives the action to the wife, so for the husband's claim it only marks a boundary: role limit (A), not basis.
difference x29: settled by the text: family-code 54 ¶1 "يجوز للزوجة ... أن تخالع نفسها" likewise: role limit (A).
difference x30: both are right: B's family-code 57 ¶1,2 and A's separate paragraphs say the same; merged as A's two statements (b9, b10) because ¶2 (custody judgments appealable) carves custody out of ¶1.
difference x31: both are right: as x30; A's 57 ¶1 is b9.
difference x32: both are right: as x30; A's 57 ¶2 is b10.
difference x33: settled by the text: family-code 58 ¶1 runs the idda "من تاريخ التصريح بالطلاق", a consequence of this judgment on which idda maintenance (61 ¶1) depends; A's statement kept (b38, role damages); B's point that it conditions nothing in the petition is kept in k55 (no item reads it).
difference x34: settled by the text: family-code 61 ¶1 "ولها الحق في النفقة في عدة الطلاق" is a consequence of this divorce (k17); A's statement kept (b39).
difference x35: settled by the text: family-code 65 ¶1 sets when custody ends, which defines the children for whom visitation is asked (merged slot minor_children); A's statement kept (b40).
difference x36: settled by the text: family-code 87 ¶3 "وفي حالة الطلاق، يمنح القاضي الولاية لمن أسندت له حضانة الأولاد" is a ruling of the divorce judgment (k17, k46); A's statement kept (b41).
difference x37: both are right: B's territory rule t2 (cpca 47 ¶1) is the same rule A put inside its t1; kept as t2, and t1 now carries only cpca 40 and 46.
difference x38: both are right: A's e2 (the husband's will, maintained after the attempts) and B's e2 (the husband himself asks by his own will) are the same element, unmatched by the id map; merged as e2 on family-code 48 ¶1, 49 ¶1 and cpca 450 ¶1.
difference x39: both are right: as x38.
difference x40: settled by the text: family-code 64 ¶1 "وعلى القاضي عندما يحكم بإسناد الحضانة أن يحكم بحق الزيارة" ties the visitation request to the children and their custody, facts the petition states; B's element kept (e3), reading the merged slot.
difference x41: settled by the text: family-code 49 ¶3 "تسجل أحكام الطلاق وجوبا في الحالة المدنية بسعي من النيابة العامة" is what A's accessory request asks the judgment to order; A's request kept (r3).
difference x42: settled by the text: A's extra blocks rest on paragraphs that reach this claim: court-sections.b9 (cpca 423 ¶4, proof of marriage, used by n1), b55 (cpca 426 ¶3, its territory), b35 (cpca 7 ¶1, publicity limited by "حرمة الأسرة"), b58 (family-code 72 ¶1 "في حالة الطلاق" the custodian's housing), and n2 (urgent measures, cpca 425 ¶1); A's blocks kept.
difference x43: settled by the text: civil-code 12 ¶2 makes the husband's nationality at filing decide the law of dissolution, so the fact is needed (boundary n8); A's proposed slot kept.
difference x44: both are right: A's minor_children and B's minor_children_of_marriage ask the same fact; one id kept (minor_children), its question narrowed to "children of this marriage still within the age of custody" by family-code 65 ¶1.
difference x45: both are right: as x44.
difference x46: settled by the text: A's ex1 on B fails because the id map holds no boundaries; the boundaries B has are the same under other ids (both right), and the two B lacks exist: n4 by family-code 32 ¶1 and 33 ¶1, n8 by civil-code 12 ¶2. The reconciled record gives A's outcome (all none, no question).
difference x47: both are right: A's ex2 on B differs only by ids (A n1 = B n2, A n5 = B n4, A n6 = B n3); the same boundaries give the same effects.
difference x48: settled by the text: A's ex3 on B differs by the ids and by the question husband_is_foreign_national, which civil-code 12 ¶2 makes necessary; the reconciled record asks it (A's outcome).
difference x49: both are right: A's ex4 on B differs only by ids (pending action, registration, earlier judgment).
difference x50: both are right: A's ex5 on B differs only by ids (earlier judgment, pending action).
difference x51: settled by the text: A's ex6 on B differs by ids and by n4, which B lacks; family-code 32 ¶1 and 33 ¶1 make annulment a separate action, so n4 exists and is silent here (no annulment request).
difference x52: settled by the text: A's ex7 on B: the annulment request warns through n4, resting on family-code 32 ¶1 and 33 ¶1; B had no item for it.
difference x53: both are right: A's ex8 on B differs only by the id of the unregistered-marriage boundary.
difference x54: settled by the text: A's ex9 on B: the foreign husband warns through n8, resting on civil-code 12 ¶2; B had no item for it.
difference x55: both are right: A's ex10 on B differs only by the id of the unregistered-marriage boundary.
difference x56: settled by the text: B's ex1 on A asks husband_is_foreign_national, which B expected not to; civil-code 12 ¶2 makes that fact matter, so B's expectation is corrected in the record (ex11 asks it; ex20 is the same case with the fact given and asks nothing).
difference x57: settled by the text: B's ex2 on A: same extra question, required by civil-code 12 ¶2; the boundary ids differ only by name.
difference x58: settled by the text: B's ex3 on A: same extra question (civil-code 12 ¶2); B's split of the marriage documents (extract or judgment) kept as doc1 and doc2 on family-code 22 ¶1.
difference x59: settled by the text: B's ex4 on A: same extra question (civil-code 12 ¶2); boundary ids differ only by name.
difference x60: settled by the text: B's ex5 on A: same extra question (civil-code 12 ¶2); boundary ids differ only by name.
difference x61: settled by the text: B's ex6 on A: the extra question husband_is_foreign_national follows civil-code 12 ¶2; B's expected question set is corrected in ex16.
difference x62: settled by the text: B's ex7 on A: same extra question (civil-code 12 ¶2).

## Proposed slots

- `minor_children` (kept) = A's `minor_children` = B's `minor_children_of_marriage`. Question: «هل للزوجين من هذا الزواج أولاد لم تنقض مدة حضانتهم؟» / "Do the spouses have children of this marriage still within the age of custody?" Read by r2, e3, doc4.
- `husband_is_foreign_national` (kept) = A's only; B had none. Read by n8.

## Id map (reconciled from A / B)

Statements: b1 = A b1 / B b1; b2 = A b2; b3 = A b3 / B b2; b4 = A b4 / B b3; b5 = A b5 / B b4; b6 = A b6 / B b31; b7 = A b7 / B b5; b8 = A b8 / B b9; b9 = A b9 / B b6 (¶1); b10 = A b10 / B b6 (¶2); b11 = A b11 / B b7; b12 = A b12 / B b8; b13 = A b13 / B b10; b14 = A b14 / B b11; b15 = A b15 / B b12; b16 = A b16 / B b13; b17 = A b17 / B b14; b18 = A b18 / B b16; b19 = A b19 / B b18; b20 = A b20 / B b19; b21 = A b21 / B b20; b22 = A b22; b23 = A b23 / B b22; b24 = A b24; b25 = A b25 / B b23; b26 = A b26 / B b29; b27 = A b27 / B b30; b28 = A b28 / B b35; b29 = A b29; b30 = A b30; b31 = A b31; b32 = A b32 / B b32; b33 = A b33 / B b33 (¶1); b34 = A b34 / B b33 (¶2); b35 = A b35; b36 = A b36; b37 = A b37; b38 = A b38; b39 = A b39; b40 = A b40; b41 = A b41; b42 = A b42 / B b36; b43 = A b43; b44 = A b44; b45 = A b45; b46 = B b28; b47 = B b24; b48 = B b25; b49 = B b26 (¶3); b57 = B b26 (¶4); b50 = B b27; b51 = B b15; b52 = B b17; b53 = B b21; b54 = B b34; added by the reconciler: b55 (cpca 56 ¶1), b56 (family-code 33 ¶2), b58 (cpca 441 ¶1).
Elements: e1 = A e1 / B e1; e2 = A e2 / B e2; e3 = B e3. Requests: r1, r2 = A / B r1, r2; r3 = A r3. Territory: t1 = A t1 / B t1; t2 = B t2 (A's t1 in part). Documents: doc1 = A doc1 (extract part) / B doc1; doc2 = A doc1 (judgment part) / B doc2; doc3 = A doc2 / B doc3; doc4 = B doc4; doc5 = B doc5. Boundaries: n1 = A n1 / B n2; n2 = A n2 / B n1; n3 = A n3 / B n5; n4 = A n4; n5 = A n5 / B n4; n6 = A n6 / B n3; n7 = A n7 / B n6; n8 = A n8. Open points: o1-o5 = A o1-o5; o6-o11 = B o1-o6; o12 added. Examples: ex1-ex10 = A ex1-ex10; ex11-ex17 = B ex1-ex7; ex18-ex21 by the reconciler.

## Boundary cases

No boundary-case file exists for this unit (`rules/boundary-cases/divorce-by-husband.json` is absent), so none is disputed. For information I ran the divorce cases of other units' files against this record: conciliation-mediation bc1-bc5 ([cpca 442 ¶2], expecting a warning on the three-month ceiling), bc15 and bc16 ([cpca 439 ¶1]), court-sections bc8 ([cpca 439 ¶1], stepExempt) and territorial-jurisdiction bc5 ([cpca 32 ¶5]) all "match no item". That is by design: the record has no deadline, prior step or exit, and its section rule is not matched by `about`. bc15 and court-sections bc8 agree with the record's reading (attempts are the judge's, after filing). bc1-bc5 expect a warning on a ceiling that cpca 442 ¶2 runs "من تاريخ رفع دعوى الطلاق": its last day always falls three months after the petition, so a petition can never be late under it; if a conciliation-mediation module carries that item, a divorce record that imports it will show its warning, which this record does not restate.

## Differences between this unit and the imported module (court-sections)

- Territory: the module's t4 rests on cpca 426 only and does not say the family rules are exclusive. cpca 40 ¶1,¶3 makes the divorce rule exclusive ("دون سواها"), subject to articles 37, 38 and 46. This record keeps that in its own t1 (not a restatement of t4); the module may want the same.
- B's boundary on the unregistered marriage cited court-sections.b7 (cpca 423 ¶1-2) for proof of marriage; the module's statement for it is b9 (cpca 423 ¶4). Corrected here; no change asked of the module.
- The module's section rule and this record's own section give the same proposals for a divorce (family; civil when the court lacks the section).

## What I changed where A and B agreed

- Re-read family-code 49 ¶1, cpca 439 ¶1, 440 ¶1, 442 ¶2: no deadline and no prior step, confirmed (the three months start at the filing and bind the judge).
- Re-read cpca 32 ¶5, 423 ¶2, 436 ¶1 for the section: confirmed (family; civil where the sections are not created). Checklist k8 now notes that the text names the civil section, not an "ordinary composition".
- b32 (cpca 53 ¶1): English `says` no longer writes "two" (the value report read it as a number not in the quote).
- n5: B's message ascribed the giving-up for connexity to cpca 55; the text puts it in cpca 56 ¶1, now cited (b55).
- n6 (A n6 / B n3): one basis (family-code 49 ¶1, 50 ¶1, civil-code 338 ¶1, ¶2).
- n4: family-code 33 ¶2 (rescission before consummation for want of witnesses, dowry or a required guardian) added.
- Documents: A's single marriage document split into B's extract (doc1) and judgment (doc2); A's proof of service and B's copies merged into doc3.
- t1 rewritten so that it does not restate the module's t4.
- Checklist: k12 now cites cpca 441 ¶1 (impediment: later date or delegated judge), added as b58; k29 notes that family-code 50 speaks of taking back during the conciliation attempt, not during the idda; B's k55 (idda) and k63 (dowry) changed from doesNotApply to applies; A's k32 (fees) changed to applies. Entry-into-force lines added for the family-code articles (k64), civil-code 12 (k65), civil-code 13 (k66) and cpca 32 through court-sections.b4 (k67); neighbour-flag lines k68 (cpca 40 ¶10), k69 (family-code 53 ¶2), k70 (family-code 66 ¶1).
- The section message names articles 423 and 32, which sit in imported statements (court-sections.b7, b4); `rules check` accepts it, but the value report of both derivations flagged it as "messageArticleNotInBasis". Left as is: a message must name its article, and restating the module's statements locally is forbidden.

## Questions left to the adversary

1. n8 reads only the husband's nationality at filing; civil-code 13 (stored 1975 wording) turns on whether either spouse was Algerian at the marriage, and its wording in force is not in the store. Is the boundary too wide, and does the amended article 13 still reach the dissolution of marriage?
2. r3 asks the court to order the registration of the judgment, which family-code 49 ¶3 makes obligatory at the prosecutor's initiative anyway: is the request proper or redundant?
3. n6 rests on civil-code 338 (res judicata, not raised by the court of its own motion). Is the better frame the absence of a subsisting marriage (no object or interest, cpca 13 ¶1, which the judge raises of his own motion under ¶2)?
4. doc3 lists the proof of service, which can exist only after registration (cpca 16 ¶2) and is produced at the first hearing, not with the petition.
5. Does the exclusivity of cpca 40 ¶1,¶3 make a petition to another court a case for a warning item (it is now only shown as territory data and open point o2)?
