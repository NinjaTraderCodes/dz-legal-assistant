# Reconciliation: module `formal-notice`

Reconciled record: `rules/reconciled/formal-notice.json` (stage `reconciled`, not verified). `rules check`: OK. `rules examples`: 24 of 24 pass. `rules examples --boundary`: 3 of 12 boundary cases pass; the 9 failures are explained under "Boundary cases" below (4 disputed, 5 where the case's outcome is met by another item or the rule language cannot express it).

**Structure kept.** A split the notice into three prior steps by their text: p1 damages (179 ¶1), p2 rescission (119 ¶1, 120 ¶2, 392 ¶1), p3 performance (164 ¶1). B merged them into one step and added a step for "other means". The record keeps A's three steps, because each rests on its own paragraph and carries its own exemptions. Two fixes: p3 now also rests on 119 ¶1 (it covers «أن يطالب بتنفيذ العقد»), and B's step for "other means" is replaced by A's boundary n1. Statement ids follow A (b1 to b26). Prior-step ids follow A (p1, p2, p3).

Counts: **22 settled by the text, 35 both right, 18 not settled, 0 needing a text.** No request was appended to `law/requests.jsonl`: no difference turned on a missing text. The missing texts the derivations already reported (the postal way of 180, the bailiff law, Civil Code 168) stay in o2, k66, k67 and o7.

## 1. The differences, one line each

| Id | Outcome | The paragraph, or why it is open |
|---|---|---|
| x1 | Settled by the text, B | 119 ¶2: «يجوز له أن يرفض الفسخ إذا كان ما لم يوف به المدين قليل الأهمية» limits the right to rescind, so its role is `limit`. |
| x2 | Settled by the text, A | 120 ¶2 opens with «وهذا الشرط»: only ¶1 defines the clause, so ¶1 is cited (b11). |
| x3 | Settled by the text, A | 120 ¶2: «لا يعفي من الاعذار» keeps a step before the rescission claim, so its role is `prerequisite`. |
| x4 | Both right | 121 ¶1 exists (rescission by law when performance is impossible). It is kept for the note boundary n7. |
| x5 | Settled by the text, A | 181 ¶1 carries the operative words «لا ضرورة لاعذار المدين». ¶2 to ¶5 are only its list, so the item paragraphs need ¶1 (b6). |
| x6 | Both right | 323 ¶1 is kept as A wrote it: interpretive, the proof of the notice. |
| x7 | Both right | 553 ¶1 is kept, for the works-contract note boundary n5. |
| x8 | Both right | 553 ¶2 is kept. It also answers the neighbour flag f-b18-p2. |
| x9 | Settled by the text, B | 558 ¶1: the work is deemed delivered only «رغم دعوته إلى ذلك بإنذار رسمي». The warning is a step before that effect, so its role is `prerequisite`. |
| x10 | Both right | CPCA 306 ¶1 is kept, interpretive, for the note boundary n6. |
| x11 | Settled by the text, A | CPCA 310 ¶2's words carry exactly what b22 says, so b22 is not interpretive. The reading that such a warning is a إعذار sits in the interpretive prior steps (means m2). `says` is narrowed to ¶2's words. |
| x12 | Both right | CPCA 406 ¶1 and ¶2 are kept: official service is by a bailiff's record, and it may concern an extrajudicial act. |
| x13 | Both right | A's p2 is the rescission part of B's merged p1. It is kept as p2. |
| x14 | Both right | A's p3 is the performance part of B's merged p1. It is kept as p3, and 119 ¶1 is added to its basis. |
| x15 | Not settled | 180 ¶1 does not list what stands in the place of a warning («ما يقوم مقام الانذار»). Two readings: B lets the lawyer say the means is equivalent and then stays silent; A always warns. The protective reading is A's n1, and o8 keeps both readings. B's step p2 is dropped. |
| x16 | Settled by the text | 119 ¶1 covers «تنفيذ العقد أو فسخه», 164 ¶1 covers performance in kind, and 179 ¶1 covers damages. Each request goes to the step whose paragraph names it. Restitution stays in p3: 181 ¶4 exempts «رد شىء» in certain cases, which only makes sense if a return otherwise needs notice. Eviction is added (see section 4). |
| x17 | Settled by the text, A | The slot names are the same facts (section 2). 392 ¶1 applies to rescission in both records. A's rescission step leaves out 181 ¶3 and ¶4: under 119 ¶1 the breached obligation is a contractual one, so «تعويضا ترتب عن عمل مضر» and «شىء تسلمه دون حق» cannot be its object. |
| x18 | Both right | A's element e1 (proof of the notice, through 323, 180 and 406) is kept. A module's element cannot be imported, so its content also sits in doc1, doc2 and the statements. |
| x19 | Both right | B `debt_is_tort_compensation` = A `obligation_is_tort_compensation`. |
| x20 | Both right | B `debtor_declared_in_writing_refusal` = A `debtor_refused_in_writing`. |
| x21 | Both right | B `debtor_knew_thing_stolen_or_wrongly_received` = A `return_of_thing_known_stolen_or_unduly_received`. |
| x22 | Both right | Same fact as x20. |
| x23 | Both right | B `default_by_term_agreement` = A `default_on_due_date_agreed`. |
| x24 | Both right | Same fact as x23. |
| x25 | Both right | `notice_date` is read by A's e1, and B kept it unread. It is kept in `slots`. No condition can compare it with the petition date (o9). |
| x26 | Both right | Same fact as x19. |
| x27 | Not settled | `other_means_equivalent_to_notice` is dropped with B's p2 (x15). The fact it asks for is the unsettled legal question itself. |
| x28 | Both right | B `performance_impossible_by_debtor` = A `performance_impossible_or_useless_by_debtor`. |
| x29 | Both right | Same fact as x28. |
| x30 | Both right | Same fact as x21. |
| x31 | Both right | aOnB ex1: B has no counterpart for A's steps p2 and p3 or for n1 (x13, x14). A's ex1 passes on the record. |
| x32 | Both right | aOnB ex2: the gap comes only from the slot names. A's ex2 passes. |
| x33 | Both right | aOnB ex3: no counterpart (x13, x14). It passes. |
| x34 | Both right | aOnB ex4: slot names. It passes. |
| x35 | Both right | aOnB ex5: slot names. It passes. |
| x36 | Both right | aOnB ex6: no counterpart for p2. It passes. |
| x37 | Both right | aOnB ex7: slot names. It passes. |
| x38 | Both right | aOnB ex8: the same five questions under different names. |
| x39 | Not settled | aOnB ex9: B asks whether the other means is equivalent. As in x15, n1 warns and asks nothing (exR3). |
| x40 | Settled by the text, A | aOnB ex10: restitution was not in B's trigger. 164 ¶1 and 181 ¶4 put it in p3 (exR1). |
| x41 | Settled by the text, A | aOnB ex11: B's means step asks on a divorce petition because it does not read the requests. A notice matters only for the claims of 179 ¶1, 119 ¶1 and 164 ¶1, so n1 reads the requests. |
| x42 | Both right | aOnB ex12: slot names. It passes. |
| x43 | Not settled | bOnA ex1: B's p2 has no counterpart (x15). |
| x44 | Both right | bOnA ex2: slot names. It passes as exB2. |
| x45 | Both right | bOnA ex3: slot names. It passes as exB3. |
| x46 | Not settled | bOnA ex4: B's p2 has no counterpart (x15). A's ex3 is the same case. |
| x47 | Both right | bOnA ex5: B's rescission rule is A's p2, not A's p1. It passes as exB5 on p2. |
| x48 | Not settled | bOnA ex6: the equivalence question (x15). Not kept: exR3 replaces it. |
| x49 | Not settled | bOnA ex7: B expected silence on eviction. Eviction is now in p3 (o6, protective), so exB7 is corrected to expect `ask`. |
| x50 | Both right | bOnA ex9: slot names. It passes as exB9 (p3). |
| x51 | Settled by the text | Boundary case bc3 on A: 181 ¶1 to ¶5 make the step unnecessary on facts that the case leaves unknown, so the record asks. I dispute bc3. |
| x52 | Settled by the text | bc4 on A: as x51, with 180 ¶1 and 392 ¶1 for rescission. I dispute bc4. |
| x53 | Not settled | bc6 on A: 180 ¶1 does not say which other means qualify. n1 warns. p1 (step done) and p2 and p3 (not asked for) are silent under their own conditions. |
| x54 | Not settled | bc7 on A: the case expects `stepMissing`. 179 ¶1 makes compensation due after notice without naming the filing, and 119 ¶1 concerns claims for performance or rescission, which the case does not make. I dispute bc7 (o9). |
| x55 | Not settled | bc8 on A: a notice on the day of the petition is open (o9). The rule language cannot compare the two dates, so p1 is silent. The case is right, and this is a gap in the record. |
| x56 | Settled by the text | bc9 on A: as x51, with p2's exemptions (180 ¶1, 181 ¶2 and ¶5, 392 ¶1) unknown, so p2 asks. I dispute bc9. |
| x57 | Settled by the text | bc10 on A: 119 ¶1 covers «أن يطالب بتنفيذ العقد». p3 now rests on it and asks. n7 no longer cites 119. |
| x58 | Not settled | bc11 on A: eviction (o6). p3 now asks, the protective reading. |
| x59 | Settled by the text | bc12 on A: 180 ¶1 makes the means matter, and unknown means ask (n1). p1 to p3 give `none` under their own conditions. |
| x60 | Settled by the text | bc3 on B: as x51. |
| x61 | Settled by the text | bc4 on B: as x52. |
| x62 | Not settled | bc6 on B: as x53. B warned through its p2, and the record warns through n1. |
| x63 | Not settled | bc7 on B: as x54. |
| x64 | Not settled | bc8 on B: as x55. |
| x65 | Settled by the text, A | bc9 on B: «[civil-code 120 ¶2] matches no item». 120 ¶2 keeps the notice before rescission, so p2 rests on b12. |
| x66 | Not settled | bc11 on B: as x58. |
| x67 | Settled by the text | bc12 on B: as x59. |
| x68 | Both right | bc2 on A and B: three steps against two, all `none`. The structure follows x13 and x14. |
| x69 | Both right | bc3 on A and B: the same questions under different names. |
| x70 | Settled by the text, A | bc4 on A and B: B also asks tort and stolen-thing exemptions on rescission. 119 ¶1 with 181 ¶3 and ¶4 (x17). |
| x71 | Not settled | bc6 on A and B: n1 against B's p2 (x15). |
| x72 | Settled by the text, A | bc9 on A and B: as x65. |
| x73 | Settled by the text, B | bc10 on A and B: B asked on a payment claim and A was silent, because A's p3 did not cite 119 ¶1. 119 ¶1 «بتنفيذ العقد». |
| x74 | Not settled | bc11 on A and B: both were silent on eviction (o6). p3 now asks. |
| x75 | Not settled | bc12 on A and B: both ask for `notice_means`, through different items (x15). n1 is kept. |

**Codex (C).** The script sent three attacks: A and B cite 119 ¶2, 176 ¶1 and 186 ¶1, and C does not.
- The text settles two of them in A and B's favour:
  - 119 ¶2 limits the rescission that p2 is about.
  - 186 ¶1 bears on whether the damages of p1 need notice (o4).
- 176 ¶1 is background: the damages that 179 conditions. No item rests on it.

C's open point "does a 180 agreement satisfy the notice 120 ¶2 keeps?" is settled by 180 ¶1: «يجوز أن يكون [الاعذار] مترتبا على اتفاق». Such an agreement is itself a form of notice, and 120 ¶2 says only that the rescission clause does not dispense with notice. So p2 keeps that exemption, and n2 says the two clauses differ.

## 2. Proposed slots: the mapping

| Kept id (A's) | B's id | Why A's |
|---|---|---|
| `default_on_due_date_agreed` | `default_by_term_agreement` | The question follows 180 ¶1 («دون حاجة إلى أي إجراء آخر»). |
| `performance_impossible_or_useless_by_debtor` | `performance_impossible_by_debtor` | It names both words of 181 ¶2 (تعذر, غير مجد). |
| `obligation_is_tort_compensation` | `debt_is_tort_compensation` | Same fact. A's wording is kept. |
| `return_of_thing_known_stolen_or_unduly_received` | `debtor_knew_thing_stolen_or_wrongly_received` | Same fact. A's wording is kept. |
| `debtor_refused_in_writing` | `debtor_declared_in_writing_refusal` | Same fact. A's wording is kept. |
| `movable_sale_price_unpaid_at_term` | `movable_sale_price_unpaid_at_term` | Same id. A's question has the conditions of 392 ¶1 that B's lacks: a term for both price and delivery, and no contrary agreement. |
| (dropped) | `other_means_equivalent_to_notice` | Only B had it (x15, x27). It asks the lawyer for the open legal question. |

B's examples were carried over with these ids.

## 3. Boundary cases I dispute

- **bc3** (about 179 ¶1, expects `stepMissing`). Under 181 ¶1 to ¶5 the step is unnecessary when certain facts are true, and the case leaves those facts unknown. 180 ¶1 adds the default-on-due-date agreement. The text therefore gives "depends on facts not stated", and the record asks for them (`ask`), as A and B both did. The case's own reason says it assumed no exemption because no slot held one. Those slots are now proposed.
- **bc4** (about 119 ¶1, expects `stepMissing`). Same reason with p2's exemptions: 180 ¶1, 181 ¶2 and ¶5, and 392 ¶1. The text gives `ask`.
- **bc9** (about 120 ¶2, expects `stepMissing`). 120 ¶2 does settle that the rescission clause gives no exemption. The other exemptions of p2 are still unknown, so the text gives `ask`.
- **bc7** (about 119 ¶1, expects `stepMissing`). The notice is dated after the petition, and the case asks only for damages.
  - 119 ¶1 governs claims for performance or rescission, which the case does not make.
  - 179 ¶1 makes compensation due after notice («لا يستحق التعويض إلا بعد اعذار») without saying that the petition must come after the notice.
  - So the text gives `unsettled`. The record cannot express either outcome, because no condition can compare `notice_date` with the petition date. That is open point o9, and every message asks the lawyer to check that the notice came before the petition.

Not disputed, but failing in the boundary run:
- **bc8**: the case is right (`unsettled`). p1 is silent for the same reason as bc7, a gap in the record.
- **bc6** and **bc12**: the case's outcome is given by n1, which warns and asks. p1, p2 and p3 are matched only because their default-on-due-date exemption cites 180 ¶1. Each gives `none` under its own condition: the step was done, or the request is not one of its own.
- **bc10** and **bc11**: p3 asks, as the case wants. p2 (rescission) is matched through 119 ¶1 and is silent because rescission is not asked.

## 4. What I changed where A and B had agreed

1. **Eviction now triggers p3, and it was added to n1's request list.** Both derivations left eviction silent. Neither 164 ¶1 nor 119 ¶1 says whether returning leased premises is a claim for performance in kind (o6, readings kept, grade C). The protective reading is to ask and warn, never block (bc11; exB7 corrected, exR2 added).
2. **b12 (120 ¶2) `says`.** Both wrote that custom sets the notice's "delay" or "period". The paragraph says the notice «يحدد حسب العرف», which is determined by custom, without naming a delay. The new `says` keeps to those words.
3. **B's open point "does a fixed term alone put the debtor in default?" is closed** (k45). 180 ¶1 requires an agreement: «اتفاق يقضي بأن يكون المدين معذرا بمجرد حلول الاجل».
4. **"The petition as notice": B gave grade D, A gave grade C. Merged as o1, grade C.** Article 180 ¶1 is in the store and does not settle the point. No missing text is involved.
5. **k24 and k57 (limitation).** A wrote that the notice is not among the interrupting acts. 317 ¶1 does name «التنبيه». Whether a bailiff's إعذار is an تنبيه is left to the periods module.
6. **k23 (risk of loss).** A wrote that the rule is not in the store. For the works contract it is: 568 ¶2 and ¶3 put the loss on the party who was put on notice. The general rule (168) is still missing (o7, D).
7. **B's boundary "a record of non-conciliation is not a notice under 180".** The text does not say that. It is moved into n1's telling facts and into o8 as an open question.
8. **Documents.** B's four documents (the notice, the proof of service, the clause, the written refusal) are kept with A's basis. doc2 now cites CPCA 406.
9. **Note boundaries now cite only the article their message names.** n2 drops 180 and its message no longer mentions 180. n7 drops 119.
10. **Messages and doc1 now ask the lawyer to check that the notice is dated before the petition** (o9).
11. **New checklist lines.**
   - One line per article with a store note: 119, 120, 121, 392, 480, 553 and 558 (k68 to k74).
   - Neighbour flags 559 ¶1 and commercial 176 ¶1, both `doesNotApply` (k75, k76).
   - A's lines for texts not found, carried over: k66 and k67.

## 5. Questions left to the adversary

1. Eviction in p3 makes every eviction claim ask about a formal notice. Is that too broad given 469 bis 1, under which a lease ends at its term without a notice to quit, and given commercial-lease-regimes?
2. Do the exemptions of 181 apply to the notice of 119 (o3)? And is it right that p2 leaves out 181 ¶3 and ¶4?
3. Does a warning directed through the court president (CPCA 310 ¶2, means m2) count as a إنذار under 180?
4. Wages claims ask for `payment`, so p3 fires on them. Should the labour claims leave p3 out of their imports?
5. bc7 and bc8 show that the rule language cannot test "notice before petition". Is a comparison with the petition date needed in the evaluator?
6. 120 ¶2 «يحدد حسب العرف»: what does custom determine, the form or the delay?
