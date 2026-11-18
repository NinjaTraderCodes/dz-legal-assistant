# Reconciliation of `custody` (claim)

Record: `rules/reconciled/custody.json` (stage `reconciled`, `verified: false`). `rules check --store`: OK, 0 failed. `rules examples`: 23 cases, 0 with differences. No boundary-case file exists for this unit (`rules/boundary-cases/` holds no `custody.json`, and no other file there is this unit's), so `rules examples --boundary` was not run.

Kept ids: derivation A's statement ids b1 to b25 are kept; derivation B's statements that A did not have are renumbered b26 to b37 (B b19 -> b26, b20 -> b27, b21 -> b28, b24 -> b29, b25 -> b30, b28 -> b31, b29 -> b32, b30 -> b33, b31 -> b34, b32 -> b35, b33 -> b36, b37 -> b37). B's b23 (cpca 423 ¶1,2) is replaced by the import court-sections.b7 and B's b26 (cpca 426 ¶1,5) by court-sections.b42. Boundaries: A's n1 to n9 kept; B's n1 = n1, B's n2 = A's n2 + n3, B's n3 = n4, B's n4 -> n10, B's n5 = n6, B's n6 dropped (difference x42).

## Differences

difference x1: Settled by the text: cpca 13 ¶1 («لا يجوز لأي شخص، التقاضي ما لم تكن له صفة، وله مصلحة») binds every claimant, so B's statement goes in (b32); it stays until petition-formal-elements has a final record to import (o5).
difference x2: Settled by the text: cpca 14 ¶1 sets the written, signed, dated petition for every claim before the court; B's statement goes in (b33).
difference x3: Settled by the text: cpca 15 ¶1-¶4, ¶6 list the petition's mandatory elements «تحت طائلة عدم قبولها شكلا» for every petition; B's statement goes in (b34).
difference x4: Settled by the text: cpca 15 ¶7 (documents referred to «عند الاقتضاء») is the basis of the documents list; B's statement goes in (b35, used by doc2 and doc4).
difference x5: Settled by the text: cpca 17 ¶1 makes registration depend on paying the fees unless the law says otherwise; B's statement goes in (b36, checklist k62).
difference x6: Settled by the text: cpca 4 ¶1 lets the judge conciliate «في أية مادة كانت» during the proceedings, which shows conciliation is no step before filing; B's statement goes in (b37, k24, k46).
difference x7: Both are right: B's cpca 423 ¶1,2 statement is the same cite and sense as the module statement court-sections.b7, which A imported; merged as the import, B's own statement dropped.
difference x8: Settled by the text: cpca 424 ¶1 charges the family judge with protecting minors' interests, a statement the claim relies on for the child's interest; B's statement goes in (b29).
difference x9: Settled by the text: cpca 425 ¶1-¶3 give the family-section president the social-assistant or expert inquiry, the report and the counter-inquiry, the proof route of e2 and e3; B's statement goes in (b30). The module statement court-sections.b30 (¶1) says only the urgent-judge powers.
difference x10: Both are right: B's cpca 426 ¶1,5 statement is covered by the module statement court-sections.b42 (426 ¶1,4,5,6), which both imported; merged as the import.
difference x11: Settled by the text: cpca 442 ¶1 lets the divorce judge take provisional measures during the conciliation attempts by an order open to no remedy, the basis of boundary n2; A's statement goes in (b24).
difference x12: Settled by the text: cpca 47 ¶1 requires the territorial plea before any defence on the merits; B's statement goes in (b31), its "says" reduced to the paragraph's words.
difference x13: Settled by the text: cpca 605 ¶1 makes a foreign judgment enforceable only after exequatur, the basis of A's note boundary n9; A's statement goes in (b25).
difference x14: Both are right: cpca 994 ¶1 is one paragraph stating the judge's duty to offer mediation and its exclusion of family cases; for this claim it works as the exclusion, so the merged role is `limit`.
difference x15: Settled by the text: family-code 22 ¶1 says how the marriage is proved (civil-status extract, or a judgment if unregistered), the proof of e1's marriage fact and doc5; B's statement goes in (b28).
difference x16: Settled by the text: family-code 49 ¶1 («لا يثبت الطلاق إلا بحكم») makes the divorce judgment the proof of e1; B's statement goes in (b26).
difference x17: Settled by the text: family-code 49 ¶3 makes the entry of divorce judgments in the civil-status register mandatory, the second proof route of e1 and doc1; B's statement goes in (b27).
difference x18: Settled by the text: family-code 65 ¶1 says «سن الزواج» without a number and 7 ¶1 speaks of capacity to marry and lets the judge allow it earlier, so using 7 ¶1 to fix the end of a girl's custody is a reading: b6 stays `interpretive` (A), with o2.
difference x19: Settled by the text: family-code 87 ¶1 (the father is guardian of his minor children, the mother after his death) is the rule that ¶3 displaces on divorce, and explains request r2; A's statement goes in (b17).
difference x20: Not settled by the text: family-code 68 ¶1 makes the lapse depend on «بدون عذر» but says neither who shows the excuse nor what follows when it is unknown; d1 keeps A's condition (it fires unless an excuse is confirmed, so a late claim warns and an early one asks nothing), B's "ask the excuse first" is a reading of d1, open point o12.
difference x21: Not settled by the text: family-code 68 ¶1 names no starting event; d1 counts from the divorce judgment (A), the earliest day a divorced parent's right can arise (72 ¶1 and 87 ¶3 speak of the case of divorce), so it warns wherever B's later start would; B's start (the day the person entitled could claim and did not) is kept as a reading and in o1.
difference x22: Settled by the text: cpca 40 ¶3 sends housing claims to «مكان وجود السكن» while 426 ¶5 sends custody to the place it is exercised; A's t1 (shown when housing is requested) goes in.
difference x23: Both are right: B's t1 (the court of the place where custody is exercised) restates the module rule court-sections.t4 that both imported; merged as the import, and the exclusivity of cpca 40 ¶1 («دون سواها») is written into t1.
difference x24: Settled by the text: cpca 47 ¶1 exists and governs any territorial plea; B's t2 goes in, reworded to the paragraph's words (B's "not a rule of public order" is not in the text and was dropped).
difference x25: Both are right: A's e1 (the divorce and its date) and B's e1 (the divorce by judgment and its date) are one element; merged as e1 with both proof routes.
difference x26: Both are right: A's e3 and B's e3 (rank, fitness, child's interest) are one element; merged as e3 with A's facts for a father's claim and B's facts on remarriage and work.
difference x27: Both are right: B's e1 merged into e1 (see x25).
difference x28: Both are right: B's e3 merged into e3 (see x26).
difference x29: Both are right: B's e4 (children under the custody age, family-code 65 ¶1) is the fact A carried inside e2 and n4; kept as e4.
difference x30: Settled by the text: family-code 72 ¶1 makes the father's duty «لممارسة الحضانة» to provide housing or the rent the fact request r3 rests on; B's e5 goes in.
difference x31: Settled by the text: family-code 64 ¶1 («وعلى القاضي عندما يحكم بإسناد الحضانة أن يحكم بحق الزيارة») makes visitation part of the award; B's request r5 goes in.
difference x32: Both are right: the two import lists of court-sections differ only by statements each used (A: b7, b35, b55, n5; B: b1); merged as the union.
difference x33: Settled by the text: family-code 57 مكرر ¶1 lets the judge rule on custody and housing by order on petition, and cpca 310 ¶1 makes such an order provisional, so the module boundary ordinary-court-exits.n5 (with its statements b50, b51, b52, b59) applies; A's import goes in.
difference x34: Both are right: both left d1's counting unsettled; A's import of periods b11 and b27 only gives the basis of the counting readings, and I added b32 and b35 for the procedure-code reading.
difference x35: Both are right: child_past_custody_age (A) and child_past_custody_term (B) are the same fact; kept child_past_custody_age.
difference x36: Both are right: same pair as x35.
difference x37: Both are right: custody_already_decided (A) and custody_previously_judged (B) are the same fact; kept custody_already_decided.
difference x38: Both are right: custody_claim_delay_excused (A) and custody_delay_excused (B) are the same fact; kept custody_claim_delay_excused.
difference x39: Both are right: same pair as x38.
difference x40: Both are right: same pair as x37.
difference x41: Not settled by the text: B's custody_unclaimed_since is the start of B's reading of family-code 68 ¶1 (x21); not kept as a slot, kept as a reading of d1 and in o1.
difference x42: Settled by the text: family-code 69 ¶1 concerns «الشخص الموكول له حق الحضانة» who wants to settle abroad, not the defendant's residence, so B's boundary n6 on defendant_resides_abroad is not supported and is dropped; the international-jurisdiction question it aimed at has no stored text (cpca 41, 42 are contractual) and stays B's open point (o11, grade D) and checklist k65.
difference x43: Not settled by the text: divorce_judgment_date is the start of d1 under the protective reading of family-code 68 ¶1 (x21); kept.
difference x44: Settled by the text: family-code 64 ¶1 puts the mother before the father and 72 ¶1 puts the housing duty on the father towards «الحاضنة», so who the plaintiff is changes the claim; A's plaintiff_parent goes in (n8, r3).
difference x45: Settled by the text: family-code 66 ¶1 makes a woman custodian's right fall «بالتزوج بغير قريب محرم»; B's plaintiff_remarried_non_mahram and its boundary go in (n10).
difference x46: Not settled by the text: A's ex1 differs on B because of the start of d1 (x21) and the slot names of the excuse and the boundaries (mapped); under the record d1 gives none (ex1 passes).
difference x47: Not settled by the text: A's ex2 differs on B by d1's start (x21); under the record, from the divorce, d1 warns (ex2 passes).
difference x48: Not settled by the text: A's ex3 differs on B by d1's start (x21); under the record d1 warns in the band (ex3 passes).
difference x49: Both are right: A's ex4 differs on B only because B names the excuse slot differently; with the mapping both give none (ex4 passes).
difference x50: Both are right: A's ex5 asks different slot sets in each record because of slot names and items each found; the record asks the mapped union (ex5 passes with the merged questions).
difference x51: Not settled by the text: A's ex6 differs on B by d1's start (x21) and the slot names; n8 warns in both readings (ex6 passes).
difference x52: Not settled by the text: A's ex7 differs on B by d1's start (x21) and the name of the already-decided slot (mapped; n1 warns, ex7 passes).
difference x53: Both are right: A splits the pending-divorce and no-divorce boundaries (n2, n3), B joins them in one; same facts and same warning, A's split kept (ex8, exB7 pass).
difference x54: Not settled by the text: A's ex9 differs on B by d1's start (x21) and the child-age slot name (mapped; n4 warns, ex9 passes).
difference x55: Not settled by the text: A's ex10 differs on B by d1's start (x21); the civil section is proposed in both (ex10 passes).
difference x56: Not settled by the text: A's ex11 (excuse unknown, year long gone) warns under A and asks under B (x20); the record warns (ex11 passes).
difference x57: Not settled by the text: B's ex1 differs on A because A starts d1 from the divorce date, absent from B's case (x21); with the divorce date added (exB1) d1 gives none; the case that splits the readings is rc1.
difference x58: Not settled by the text: B's ex2 differs on A by d1's start (x21); with the divorce date as start, exB2 warns.
difference x59: Not settled by the text: B's ex3 differs on A by d1's start (x21); exB3 warns in the band.
difference x60: Both are right: B's ex4 differs on A only by the excuse slot's name; with the mapping the excuse makes d1 silent (exB4 passes).
difference x61: Both are right: B's ex5 asks different slot sets in each record because of slot names and items each found; the record asks the mapped union (A's ex5 kept for it).
difference x62: Not settled by the text: B's ex6 differs on A by d1's start (x21) and the already-decided slot name (mapped; n1 warns, exB6 passes).
difference x63: Not settled by the text: B's ex7 differs on A by d1's start (x21) and the joined boundary (x53); exB7 warns by n2 and n3.
difference x64: Not settled by the text: B's ex8 differs on A by d1's start (x21) and the child-age slot name (mapped; n4 warns, exB8 passes).
difference x65: Not settled by the text: B's ex9 differs on A by d1's start (x21) and by the remarriage boundary A lacked, which family-code 66 ¶1 supports (n10 warns, exB9 passes).
difference x66: Not settled by the text: B's ex10 differs on A by d1's start (x21); the civil section is proposed in both (A's ex10 kept for it).
difference x67: Not settled by the text: B's ex11 differs on A by d1's start (x21) and by B's defendant-abroad boundary, which family-code 69 ¶1 does not support (x42); the example is dropped (see below).

Counts: settled by the text 24; both are right 22; not settled by the text 21 (4 item differences, x20, x21, x41, x43, and 17 case differences that follow them); needing a text 0.

## Proposed slots: the mapping

| Record id | Derivation A | Derivation B | Note |
|---|---|---|---|
| `divorce_judgment_date` | `divorce_judgment_date` | (none) | d1's start under the protective reading |
| (not kept) | (none) | `custody_unclaimed_since` | a different fact, B's start of d1; kept as a reading of d1 and in o1 |
| `custody_claim_delay_excused` | `custody_claim_delay_excused` | `custody_delay_excused` | same fact |
| `custody_already_decided` | `custody_already_decided` | `custody_previously_judged` | same fact |
| `child_past_custody_age` | `child_past_custody_age` | `child_past_custody_term` | same fact |
| `plaintiff_parent` | `plaintiff_parent` | (none) | n8, r3 |
| `plaintiff_remarried_non_mahram` | (none) | `plaintiff_remarried_non_mahram` | n10 |

B also read the existing slot `defendant_resides_abroad` (boundary n6); no item of the record reads it now (x42).

## Boundary cases

No boundary-case file exists for `custody`; none disputed.

## Differences between this unit and the modules

- court-sections t4 (family territory) rests on cpca 426 only. Cpca 40 ¶1 adds that the courts it names hear these claims «دون سواها», and 40 ¶3 sends housing claims to «مكان وجود السكن», which neither t4 nor b42 carries. B restated t4 in its own t1; the record imports t4 and writes the exclusivity and the housing rule into its own t1 (shown when housing is requested).
- court-sections.b30 cites cpca 425 ¶1 but says only that the family-section president has the urgent judge's powers; the second half of ¶1 (the inquiry by a social assistant, a medical expert or a competent service) and ¶2-¶3 are carried by the claim's own b30.
- Both derivations cite family-code 72 ¶1 as their own basis statement (b14); court-sections holds the same paragraph as b58 for the family section of a housing request. Same cite, same sense; the claim keeps its own statement as the basis of r3 and e5.

## What I changed where A and B had agreed

- d1 (kind forfeiture, one year, raisedBy unclear, interpretive, warns, no counting): values kept after re-reading family-code 68 ¶1 and its neighbours 66 to 71. The value check's "kindNotInText" is answered in the interpretive note: the text says the right «سقط», not that it is time-barred, so the kind is a reading; the item never blocks. I added a counting reading under the procedure code's rule (periods.b32, b35, cpca 405 ¶1 and ¶4), which A and B did not have, beside A's reading under civil-code 314 ¶1; neither text reaches a Family Code period by its words, so `counting` stays absent.
- Section, no prior step, no exit: all re-read (cpca 32 ¶5, 423 ¶3; family-code 49 ¶1 and cpca 439 ¶1 on conciliation are divorce-only; cpca 994 ¶1 excludes family cases from mediation; the formal-notice module turns on requests this claim does not make). Unchanged.
- B's checklist line on the public prosecutor (k47) answered `doesNotApply`; family-code 3 مكرر ¶1 makes him a principal party in every case applying the Family Code, so it is now `applies` (no step follows from it).
- Examples: A's ex1 and ex5 now also carry the remarriage slot of n10. B's examples are kept with the mapped slot ids and, since d1 now starts from the divorce judgment, with a divorce date in place of `custody_unclaimed_since`; their expectations are unchanged. B's ex11 expected a warning for a defendant living abroad under family-code 69 ¶1, which that paragraph does not support: dropped. B's ex5 and ex10 duplicate A's ex5 and ex10 and are not kept. Reconciler examples rc1 to rc4 cover x21, x20, x44 and x45.
- Entry-into-force lines were added for every cited article whose store entry has a note (family-code 3 مكرر, 7, 22, 49, 57, 57 مكرر, 62, 64, 65, 66, 67, 68, 69, 71, 72, 87; cpca 32; civil-code 3); the merged checklist had none. Neighbour flags: cpca 40 ¶10 and family-code 48 ¶1 are answered by `doesNotApply` lines; every other flagged paragraph is cited by a statement.

## Questions left to the adversary

1. d1's start: is the divorce judgment the right protective start of family-code 68 ¶1's year, given that it warns in the common case of a mother who has had the children since the divorce and claims only when the father takes them (rc1)?
2. d1's condition: should the lapse warn when the excuse is unknown (the record), or ask the excuse first (B)?
3. n2 and n3: is a custody claim before any divorce really outside this claim, given family-code 57 مكرر ¶1 and cpca 460 ¶2 (provisional custody to a parent in the guardianship procedure)? Both warn only.
4. r3: is the housing request rightly limited to a mother plaintiff (family-code 72 ¶1 «للحاضنة»), or should it follow any female custodian?
5. The CPCA statements on standing, the petition and fees (b32 to b36) are cited directly because petition-formal-elements has no final record here (o5); should they wait for the module?
6. International jurisdiction when the defendant or the children are abroad (o11, grade D): no stored text; is there one to request?
