# Reconciliation of `nafaqa` (claim)

Reconciled record: `rules/reconciled/nafaqa.json` (stage `reconciled`, `verified: false`). `rules check --store` passes; `rules examples` runs 28 examples (11 from A, 10 from B, 7 of mine), 0 differences. There is no boundary-case file for this unit (`rules/boundary-cases/` holds none for `nafaqa`), so no boundary run was made and no case is disputed. No line was appended to `law/requests.jsonl`: no difference needed a text that is not in the store.

## The differences

difference x1: settled by the text: civil-code 14 ¶1 makes the debtor's national law govern maintenance between relatives, which reaches the children's maintenance claimed here; B's statement is kept (b30, interpretive, as B had it), and boundary n11 now also cites civil-code 12 ¶1 and 13 ¶1 because a wife's maintenance may be an effect of the marriage rather than maintenance between relatives (open point o9).
difference x2: settled by the text: civil-code 309 ¶1 lists rents, arrears, salaries, wages and pensions and does not name maintenance, while family-code 80 ¶1 caps the period before filing; applying 309 to maintenance arrears is a reading, so b29 is interpretive (B).
difference x3: settled by the text: cpca 13 ¶1 sets the standing every plaintiff needs, and boundary n9 (the mother claiming the children's maintenance during the marriage) rests on it; A's statement is kept (b23).
difference x4: settled by the text: cpca 13 ¶2 makes the judge raise a lack of standing of his own motion, which is what boundary n9 warns about; A's statement is kept (b24).
difference x5: settled by the text: cpca 14 ¶1 requires a written, signed and dated petition lodged by the plaintiff, his agent or his lawyer, and answers checklist lines k29, k56 and k57; B's statement is kept (b35), to be replaced by an import from the petition module when allowed (o7).
difference x6: settled by the text: cpca 15 ¶1, ¶6 and ¶7 require the facts, requests, means and supporting documents on pain of formal inadmissibility for every petition, this one included; B's statement is kept (b33).
difference x7: settled by the text: cpca 17 ¶1 makes registration of the petition depend on paying the fees, which answers k29 and k55; B's statement is kept (b34).
difference x8: settled by the text: cpca 323 ¶1 suspends enforcement during the ordinary appeal period, the rule that ¶2's provisional execution in maintenance matters displaces only when requested; B's statement is kept (b31) and added to the basis of request r6.
difference x9: settled by the text: cpca 324 ¶1 lets the appeal court's president stop provisional execution, which the lawyer asking for r6 must know; B's statement is kept (b32) as information, with no rule.
difference x10: not settled by the text: cpca 40 ¶1 keeps articles 37, 38 and 46 («فضلا عما ورد») beside «دون سواها», and cpca 426 ¶6 names the creditor's domicile without the exclusive words; b19 stays interpretive (A), both readings are in open point o2, and the protective rule proposes the creditor's court.
difference x11: settled by the text: cpca 414 ¶1 governs service on a person domiciled abroad, which answers k30; A's statement is kept (b25).
difference x12: settled by the text: cpca 415 ¶1 sends service through diplomatic channels where no convention exists; A's statement is kept (b26).
difference x13: settled by the text: cpca 777 ¶1 and ¶2 name maintenance of minors, parents and the wife expressly and cap the wage attachment at half; B's statement is kept (b36) as enforcement information.
difference x14: settled by the text: cpca 779 ¶3 gives maintenance debts priority in recovery; B's statement is kept (b37).
difference x15: settled by the text: cpca 781 ¶2 makes the transfer order for a maintenance debt continue monthly; B's statement is kept (b38).
difference x16: settled by the text: cpca 990 ¶1 allows settlement at any stage, the only conciliation the procedure code leaves to a family case; A's statement is kept (b22).
difference x17: settled by the text: cpca 994 ¶1 excepts family matters from the mediation the judge must offer, so no mediation step applies; A's statement is kept (b21).
difference x18: settled by the text: family-code 49 ¶1 says a divorce is established only by a judgment; in a maintenance claim it serves to prove the divorce (doc2, e3), while its reconciliation attempts belong to the divorce claim and are no step before this one, so the role is `proof` (B).
difference x19: settled by the text: family-code 53 ¶2 is item 1 of the list that ¶1 opens («يجوز للزوجة أن تطلب التطليق للأسباب الآتية»), and the rule needs both, so A's cite of ¶1 and ¶2 is kept (b16).
difference x20: settled by the text: as x19, family-code 53 ¶2 alone does not say who may ask for what; B's statement is merged into b16 citing ¶1 and ¶2.
difference x21: settled by the text: family-code 55 ¶1 attaches divorce and compensation to a spouse's breach and says nothing of maintenance; B's statement is kept as interpretive (b39) and grounds only a reading of open point o4, never a rule (A's k9 conclusion and B's flag agree on that).
difference x22: settled by the text: family-code 57 ¶1 leaves the financial aspects of divorce judgments open to appeal, which answers A's line k17 on maintenance decided inside a divorce; A's statement is kept (b28).
difference x23: settled by the text: family-code 61 ¶1 says «ولها الحق في النفقة في عدة الطلاق», a right that grounds request r3, so the role is `basis` (B).
difference x24: settled by the text: family-code 72 ¶2 gives the custodian the right to stay in the marital home until the housing judgment is carried out, a right and not a step of procedure, so the role is `basis` (B).
difference x25: settled by the text: family-code 76 ¶1 is the exception to 75 ¶1 that moves the children's maintenance to the mother when the father is unable, and the record uses it only for boundary n7, so the role is `limit` (A).
difference x26: settled by the text: family-code 77 ¶1 founds maintenance between ascendants and descendants on means, need and degree of kinship in inheritance, conditions unlike those of 74 ¶1 and 75 ¶1, and both records use it only to route that claim out (boundary n3), so the role is `limit` (A).
difference x27: settled by the text: family-code 87 ¶1 makes the father the guardian of his minor children, which is why boundary n9 asks in what capacity the mother claims for them; A's statement is kept (b13).
difference x28: settled by the text: family-code 87 ¶2 lets the mother act in urgent matters when the father is absent or prevented, the reading of open point o3; A's statement is kept (b14).
difference x29: settled by the text: family-code 87 ¶3 allocates guardianship (who acts for the children after divorce), not the maintenance right itself, so the role is `procedure` (A).
difference x30: settled by the text: family-code 74 ¶1 («تجب نفقة الزوجة على زوجها بالدخول بها أو دعوتها إليه ببينة») makes consummation or the call a condition of the duty, not a step taken before filing; it is carried as element e2 and the warning boundary n6 (A), B's prior step p1 is dropped and its slot merged into `consummation_or_invitation` (example ex22: unknown asks, ex15: no warns).
difference x31: both are right: A's t1 carried the article 47 rule in its text with b20, B made it a separate rule t2; kept as t2 (b20).
difference x32: both are right: B's e1 carried consummation or the call together with the marriage; kept as A's separate element e2.
difference x33: both are right: A's e3 and B's e2 are the same element (the divorce and, for housing, custody); merged as e3 with A's events.
difference x34: both are right: B's e3 included the means and condition of the parties; kept as A's separate element e6.
difference x35: both are right: B's e2 is A's e3 (see x33); merged.
difference x36: settled by the text: family-code 80 ¶1 awards the period before filing «بناء على بينة», evidence brought by the party asking for it, so the burden of element e5 is the plaintiff's (B); civil-code 323 ¶1 stays as a proof means: the debtor proves payment.
difference x37: settled by the text: family-code 61 ¶1 grants the divorced wife maintenance in her waiting period; A's request r3 is kept, conditioned on `spouses_divorced`.
difference x38: settled by the text: family-code 78 ¶1 lists housing or its rent among the components of maintenance; A's request r5 (housing during the marriage) is kept.
difference x39: settled by the text: family-code 80 ¶1 («يحكم باستحقاقها ... لمدة لا تتجاوز سنة قبل رفع الدعوى») awards the same maintenance entitlement for an earlier period, a principal head of the claim and not an accessory request, so r2 is `accessory: false` (B).
difference x40: settled by the text: civil-code 339 ¶1 (ordinary-court-exits b36) binds the civil judge to a criminal judgment on the facts it decided, a rule limited to no claim type, the module applies to every claim, and non-payment of maintenance can be the subject of a criminal case; B's import of x6, b36 and b37 is kept.
difference x41: both are right: A's `consummation_or_invitation` and B's `wife_entered_or_called_with_proof` hold the same fact; one id kept, `consummation_or_invitation`, with a question in the text's own words because the text does not say who makes the call (open point o10).
difference x42: settled by the text: follows x40 (civil-code 339 ¶1); `criminal_case_on_same_facts` is read by the imported exit x6.
difference x43: settled by the text: civil-code 14 ¶1 (with 12 ¶1 and 13 ¶1) chooses the applicable law by nationality; B's `debtor_is_foreign_national` is kept for boundary n11 (ex20, ex24).
difference x44: both are right: B's `maintenance_creditor` and A's `maintenance_for` hold the same fact; kept as `maintenance_for` with B's choice `ascendant_or_descendant` added, and B's choice `divorced_wife` expressed as `maintenance_for` wife plus `spouses_divorced` yes (ex25).
difference x45: both are right: see x44.
difference x46: both are right: B's `maintenance_judgment_date` and A's `maintenance_previously_judged` hold the same fact; the yes/no slot is kept because B's test `known: true` reads an unknown judgment as silence instead of asking (ex26), and no condition can add the year of family-code 79 ¶1 to a date; the question asks for the date in words.
difference x47: both are right: see x46.
difference x48: settled by the text: family-code 22 ¶1 proves an unregistered marriage only by a judgment; A's boundary n4 and its slot `marriage_registered` are kept (ex8, ex27).
difference x49: settled by the text: follows x40 (civil-code 339 ¶1); `obligation_is_tort_compensation` is read by the imported exit x6 (see the module difference below).
difference x50: both are right: B held the divorce in the choice `divorced_wife`; A's slot `spouses_divorced` is kept because requests r3, r4, r5, boundaries n5, n9 and documents doc2, doc10 read it apart from the creditor.
difference x51: both are right: see x41.
difference x52: settled by the text: A's ex1 on B was asked about the criminal case, the tort, the debtor's nationality and the creditor because B imported x6 (kept, civil-code 339 ¶1), had n4 on nationality (kept, civil-code 14 ¶1) and named the creditor slot differently (merged); A's boundaries without counterpart are kept (family-code 79 ¶1, 22 ¶1, 61 ¶1, 74 ¶1, 87 ¶1); ex1 now answers those slots and asks nothing.
difference x53: settled by the text: as x52 for the questions; d1 warns in both records (family-code 80 ¶1); ex2 answers the extra slots.
difference x54: settled by the text: as x52 for the questions; d1 warns inside the band in both (family-code 80 ¶1); ex3 answers the extra slots.
difference x55: settled by the text: the two question sets differ only by the slots of x30, x40, x43, x44 and x46 to x50, each settled above (family-code 74 ¶1, civil-code 339 ¶1, civil-code 14 ¶1, family-code 79 ¶1, 22 ¶1); ex4 now lists the merged questions.
difference x56: settled by the text: n5, n6 and n9 have no counterpart in B and are kept (family-code 61 ¶1, 74 ¶1, 87 ¶1 with cpca 13 ¶2); the extra questions are those of x52; ex5 passes on the merged record.
difference x57: settled by the text: n6 is kept as the carrier of family-code 74 ¶1 (x30); B's p1 gave the same warning; ex6 passes.
difference x58: settled by the text: n1 now reads the yes/no slot (family-code 79 ¶1, x46); ex7 warns.
difference x59: settled by the text: n4 is kept (family-code 22 ¶1, x48); ex8 warns.
difference x60: settled by the text: only the questions of x52 differed; the civil section where sections were not created rests on cpca 32 ¶5 (court-sections b4) in both; ex9 passes.
difference x61: settled by the text: n9 is kept (family-code 87 ¶1 and ¶2, cpca 13 ¶2); ex10 warns.
difference x62: settled by the text: d1's condition on the maintenance request is the same in both (family-code 80 ¶1); n5 and n9 are kept; ex11 passes.
difference x63: settled by the text: B's p1 is replaced by n6 (family-code 74 ¶1, x30), A's n1 to n4 read slots B's ex1 did not answer; mapped as ex12 (B's absence of a judgment date written as `maintenance_previously_judged` no), which passes.
difference x64: settled by the text: d1 warns in both (family-code 80 ¶1); the question sets differ by slot names only (x44, x50) and the imports (x40); mapped as ex13.
difference x65: settled by the text: as x64; mapped as ex14.
difference x66: settled by the text: B's p1 warning is given by n6 (family-code 74 ¶1, x30); mapped as ex15, n6 warns.
difference x67: settled by the text: B's p1 ask is given by n6's ask on `maintenance_for` and `consummation_or_invitation` (family-code 74 ¶1); n3 and n11 ask on the merged slots; mapped as ex16.
difference x68: settled by the text: B's n2 with its condition on the `tatliq` request is kept (family-code 53 ¶1 and ¶2), A's n2 had no condition; mapped as ex17, n2 warns.
difference x69: settled by the text: n1 reads `maintenance_previously_judged` (family-code 79 ¶1, x46); mapped as ex18 with yes, n1 warns.
difference x70: settled by the text: n3 warns on the merged choice `ascendant_or_descendant` (family-code 77 ¶1); B's p1 is replaced by n6, silent for that creditor; mapped as ex19.
difference x71: settled by the text: n11 warns on a foreign debtor (civil-code 14 ¶1, x1); the civil section where sections were not created (cpca 32 ¶5); mapped as ex20 with `maintenance_for` children and `spouses_divorced` yes.
difference x72: settled by the text: the imported exit x6 warns on a pending criminal case (civil-code 339 ¶1, x40); B's p1 is replaced by n6; mapped as ex21 (the exit's own finding cannot be named by an example of the claim record, since `rules check` refuses imported refs in examples).

Counts: settled by the text 59, both are right 12, not settled by the text 1, needs a text that is not in the store 0.

## Proposed slots: the mapping

| Fact | Derivation A | Derivation B | Kept |
|---|---|---|---|
| First unpaid day | `maintenance_unpaid_since` | `maintenance_unpaid_since` | `maintenance_unpaid_since` |
| For whom maintenance is claimed | `maintenance_for` (wife, children, wife_and_children) | `maintenance_creditor` (wife, divorced_wife, child, ascendant_or_descendant) | `maintenance_for` (wife, children, wife_and_children, ascendant_or_descendant); B's `divorced_wife` = `maintenance_for` wife + `spouses_divorced` yes; B's `child` = children |
| Consummation or the call, on evidence | `consummation_or_invitation` | `wife_entered_or_called_with_proof` | `consummation_or_invitation` (question in the text's words) |
| Divorce pronounced | `spouses_divorced` | inside `maintenance_creditor` (`divorced_wife`) | `spouses_divorced` |
| Marriage registered | `marriage_registered` | none | `marriage_registered` |
| Earlier maintenance judgment | `maintenance_previously_judged` (yes/no) | `maintenance_judgment_date` (date, tested with `known`) | `maintenance_previously_judged` (the question asks for the date in words) |
| Debtor's nationality | none | `debtor_is_foreign_national` | `debtor_is_foreign_national` |

The imported exit x6 also reads the existing slots `criminal_case_on_same_facts`, `obligation_is_tort_compensation` and `damage_caused_by_offence`.

## Boundary cases

No boundary-case file exists for `nafaqa`; none is disputed.

## What I changed where A and B had agreed

- **Territory t1** (both had it): it now states both what B saw (housing claims go to the place of the housing, cpca 40 ¶3) and what A saw (the reading of «فضلا عما ورد ... دون سواها»), on the interpretive statement b19.
- **Boundaries n1, n2, n3** (both had them): n1 reads the merged yes/no slot (B's `known` test never asked); n2 takes B's condition on the `tatliq` request (A's had none); n3 takes B's condition on the merged `ascendant_or_descendant` choice (A's had none). B's n4 (foreign debtor) is now n11, and B's n5 (interim maintenance) is merged into A's n8 (both family-code 57 مكرر ¶1).
- **Section rule**: the value and alternative are unchanged; it is marked interpretive (B, and the court-sections module's own note) because cpca 32 ¶5 does not say whether it reaches a court that has sections but lacks the family one.
- **d1** (agreed on every compared field): kept, with A's `extract.sanction` and both sides' readings (ceiling, discretion, five-year limitation). I re-read family-code 80 ¶1 and its neighbours: the paragraph has no word of limitation or forfeiture (the value check flags `kindNotInText`); since d1 is interpretive it declares `warn` whatever its kind, so `limitation` was kept and the question is left to the adversary. `counting` stays absent: civil-code 314 ¶1 counts limitation periods and cpca 405 counts the procedure code's periods, so neither text settles how this Family Code year is counted; the evaluator's fallback (`excludeFirstDay`, no extension) equals the periods module's method for limitation periods.
- **No import of `periods`** (agreed): re-checked; its general, periodic-rights and other limitation periods are all longer than the one-year cap of family-code 80 ¶1 on the arrears a petition can claim, so they would only add questions. No import of `formal-notice` (its steps fire only on damages, rescission, payment, delivery, restitution or eviction requests) or `labour-prelitigation` (applies to unpaid-wages only).
- **B's open points o3 (prior step) and o4 (divorce element)** were dropped as settled: cpca 994 ¶1 and family-code 49 ¶1 leave no step before a maintenance claim, and family-code 61 ¶1 and 72 ¶1 make the divorce an element only of the waiting-period and custodian-housing requests (element e3).
- **Added by me** (graded `addedAfterDerivation`): statements b40 (civil-code 12 ¶1) and b41 (civil-code 13 ¶1) in the basis of boundary n11, because B's message ("the debtor's national law governs") would mislead for an Algerian wife married to a foreign husband; open points o9 (conflict rule) and o10 (who makes the call of family-code 74 ¶1); documents doc9 to doc11 split from B's doc3, doc4 and doc6; checklist lines k62 to k76 (entry-into-force notes, the age of majority, and the neighbour flags cpca 40 ¶10, family-code 48 ¶1, civil-code 309 ¶2 and cpca 325 ¶1).

## Differences between this unit and the modules

- **court-sections t4 against the claim's t1.** The module sends maintenance to the creditor's domicile on cpca 426 ¶6. It says nothing of cpca 40 ¶3, which adds «دون سواها» and sends housing claims to the place of the housing. The claim keeps its own t1 for those two points; the module's rule is not contradicted.
- **court-sections b58 (family-code 72 ¶1)** cites the same paragraph as the claim's own b8. The claim keeps b8 because its requests r4 and doc lists rest on it.
- **ordinary-court-exits x6.** Its condition asks `obligation_is_tort_compensation` and `damage_caused_by_offence` whenever `criminal_case_on_same_facts` is unknown or no. For a maintenance claim these two questions are always beside the point. That is a cost of the module's trigger, not an error of this record. Also, `rules examples` and `rules eval` do not resolve imports, so the exit's finding cannot be tested from this record.

## Questions left to the adversary

1. **x30.** Is consummation or the call of family-code 74 ¶1 rightly a condition of the right (element e2, boundary n6), and not a prior step (B's p1)? And who makes the call («دعوتها إليه»): the wife or the husband (open point o10)?
2. **d1.** Should family-code 80 ¶1 be a deadline at all, and of which kind? The text has no word of limitation or forfeiture, and the cap is addressed to the judge. Is the warning from the first unpaid day the right trigger?
3. **x10 / t1.** Does cpca 40 ¶1 make the creditor's court exclusive, or does it leave the defendant's court open? Which court hears a joint maintenance and housing claim? Should the claim's t1 be folded into the module's t4?
4. **x36.** Does the plaintiff carry the burden for non-payment and its start date, or only for the period before filing?
5. **x39.** Is the request for past maintenance principal or accessory?
6. **n11.** For a wife's maintenance from a foreign husband, does civil-code 12 and 13 govern, or civil-code 14? The store holds 13 and 14 only in their 1975 wording, without certifying that wording is in force.
