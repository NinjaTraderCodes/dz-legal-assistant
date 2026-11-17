# Reconciliation: module land-registry-publication

Record: `rules/reconciled/land-registry-publication.json` (stage reconciled, not verified). `rules check` OK; `rules examples` 16 of 16; `--boundary` 30 of 30 (16 examples and 14 boundary cases).

Statement ids in the reconciled record: b1 = 17 ¶3 (A b1, B b1); b2 = 519 ¶1 (A b2, B b2); b3 = 539 ¶3 (A b3); b4 = 16 ¶1-2 (A b4); b5 = 50 ¶1 (A b5); b6 = 68 ¶1 (A b6); b7 = 69 ¶1 (A b7); b8 = 1 ¶1 (A b8); b9 = 652 ¶2 (A b9, B b9); b10 = 762 ¶2 (A b10); b11 = 515 ¶1 (B b3); b12 = 511 ¶1 (B b4); b13 = 512 ¶2,3,5,10,11 (B b5); b14 = 518 ¶1 (B b6); b15 = 40 ¶2 (B b7); b16 = 15 ¶7 (B b8); b17 = 512 ¶7 (new). Boundaries: n1 = B n1 (property not published); n2 = A n1 + B n2 (seizure and adjudication entries). Open points: o1 = A o1 + B o5; o2 = A o2; o3 = A o3 + B o2; o4 = A o4 + B o3 (cure); o5 = A o5 + B o3 (before filing); o6 = A o6; o7 = A o7 + B o1; o8 = B o4.

## Differences

difference x1: settled by the text: cpca 1 ¶1 applies the code to ordinary and administrative courts, and article 17 sits in Book I, "provisions common to all courts"; A's b8 is kept as the basis of o6 (k20).
difference x2: settled by the text: cpca 15 ¶7 has the petition mention its supporting documents, which fits doc2 (proof of the property's position, cited with the petition); doc1 is produced at the first hearing under 17 ¶3, so 15 ¶7 is kept as b16 for doc2 only.
difference x3: settled by the text: cpca 16 ¶1-2 registers and numbers the petition on filing and returns the copies for service, while 17 ¶3 fixes production at the first hearing; A's b4 is kept, interpretive, as the basis of o5, and left out of p1's basis.
difference x4: not settled by the text: 17 ¶3 «بعقار و/أو حق عيني عقاري مشهر طبقا للقانون» does not say whether «مشهر» qualifies the property, the right or both; b1 is set interpretive, both readings are in o7, the rule is the protective one (p1 asks about the property; n1 warns when it is not published). B's mention of the French edition is dropped: no French text is in the store (frenchCheck null).
difference x5: settled by the text: cpca 40 ¶2 sends real-property matters, works on property and leases of property to the court of the property's place; kept as b15, basis of t1 (its "only" removed from says: «دون سواها» is in ¶1, not cited).
difference x6: settled by the text: cpca 50 ¶1 says pleas of form are raised before the merits on pain of inadmissibility; kept as b5, the basis of o3's second reading, since «عدم قبولها شكلا» in 17 ¶3 falls between a plea of form and a plea of inadmissibility.
difference x7: settled by the text: cpca 511 ¶1 gives the real-property section disputes about immovables; kept as b12 (checklist k29, k45), not in p1's basis.
difference x8: settled by the text: cpca 512 ¶2, ¶3, ¶5, ¶10, ¶11 list ownership and real rights, possession, co-ownership, partition and dwelling leases among real-property disputes, which is what «تعلقت بعقار» in 17 ¶3 reaches on the protective reading (o1); kept as b13 in p1's basis, its says corrected to the cited paragraphs.
difference x9: settled by the text: cpca 515 ¶1 names the claims to annul, rescind, amend or undo rights from published deeds, the same claims 519 ¶1 subjects to special publication provisions; kept as b11 in p1's basis.
difference x10: settled by the text: cpca 518 ¶1 gives territorial jurisdiction in real-property cases to the court of the property's area; kept as b14, basis of t1.
difference x11: settled by the text: cpca 539 ¶3 registers the appeal petition "subject to article 17"; A's b3 is kept, interpretive as to which paragraph of 17 it reaches (o6). B's k14 and k21 (here k42, k49) had missed it.
difference x12: settled by the text: cpca 68 ¶1 allows a plea of inadmissibility at any stage; kept as b6, basis of o3's first reading.
difference x13: settled by the text: cpca 69 ¶1 makes the judge raise a plea of inadmissibility of public order; kept as b7, basis of o3's first reading.
difference x14: settled by the text: cpca 762 ¶2 has the bailiff enter the adjudication judgment at the land registry for publication, an enforcement act distinct from the petition's publication; kept as b10, with b9, in boundary n2.
difference x15: both are right: A's {property_registered is yes} and B's {not {property_registered is no}} give the same value in all three cases (yes: true, no: false, unknown: unknown, asking the same slot); A's simpler form is kept.
difference x16: settled by the text: cpca 511 ¶1 gives the real-property section only disputes about immovables, so B's unconditional realEstate in a module that applies to every claim proposes it for wages or maintenance claims (B's ex4); the conditional proposal is the court-sections module's (slot dispute_concerns_real_property); no section is written here.
difference x17: settled by the text: cpca 40 ¶2 and 518 ¶1 state the rule of t1 (court of the property's place); t1 is kept as data with `when` dispute_concerns_real_property yes. It may duplicate the territorial-jurisdiction module: left to the adversary.
difference x18: both are right: B's petition_deposited_for_publication and A's petition_published_at_land_registry hold the same fact (was the step of 17 ¶3 done by an accepted means); merged under petition_deposited_for_publication, whose question names both means.
difference x19: both are right: same fact as x18; A's id is mapped to petition_deposited_for_publication.
difference x20: both are right: under the slot mapping (x18) A's ex1 gives p1 none and no question on both records; the remaining section part follows x16.
difference x21: both are right: under the slot mapping A's ex2 gives p1 warn on both records; the section part follows x16.
difference x22: both are right: under the slot mapping both records ask the same slot in A's ex3; the section part follows x16.
difference x23: settled by the text: only the section differs; cpca 511 ¶1 (see x16), no section in this module.
difference x24: settled by the text: only the section differs; cpca 511 ¶1 (see x16): A's ex5 is a non-real-property claim for which B proposed realEstate.
difference x25: settled by the text: only the section differs; cpca 511 ¶1 (see x16).
difference x26: settled by the text: only the section differs; cpca 511 ¶1 (see x16): with nothing known B still proposed realEstate.
difference x27: both are right: A's ex8 has the same slots as ex1; under the slot mapping both records give p1 none; ex8 is dropped as a duplicate.
difference x28: both are right: under the slot mapping B's ex1 gives p1 none on both; B's n1 is kept (x32) and gives none here; the section expectation of B's ex1 is removed (x16).
difference x29: both are right: under the slot mapping B's ex2 gives p1 warn on both; n1 none.
difference x30: both are right: under the slot mapping both records ask the same slot in B's ex3.
difference x31: settled by the text: B's n1 is kept on 17 ¶3 «و/أو» (x32) and gives none in a non-real-property claim; the section part follows x16 (511 ¶1).
difference x32: settled by the text: 17 ¶3 «بعقار و/أو حق عيني عقاري مشهر» brings a published real right within the duty even where the property is not published, and the slot asks only about the property; so a "no" does not by itself exclude the duty, and B's n1 warning stands (A's o7 said the same as an open point).
difference x33: settled by the text: as x32 (17 ¶3 «و/أو»); with nothing known n1 asks the same two slots as p1.
difference x34: settled by the text: as x32 (17 ¶3 «و/أو»); with the property's registration unknown n1 asks it.
difference x35: settled by the text: cpca 519 ¶1 subjects real-property claims to the special provisions on publishing rescission, annulment, amendment and undoing claims; B had it in p1's basis and it is kept there, so bc6 now matches p1 (ask).
difference x36: settled by the text: cpca 515 ¶1 names the claims on rights from published deeds that 519 ¶1 refers to; b11 is now in p1's basis, so bc7 matches p1.
difference x37: settled by the text: cpca 519 ¶1, as x35 (bc8).
difference x38: settled by the text: cpca 512 ¶7 lists pre-emption among real-property disputes; added as b17 in p1's basis, so bc9 matches p1 (ask).
difference x39: settled by the text: cpca 512 ¶11 lists dwelling leases among real-property disputes (b13 in p1's basis); bc10 now matches p1 (none, property not published).
difference x40: settled by the text: cpca 512 ¶11, as x39; bc11 matches p1 (ask property_registered).
difference x41: settled by the text: cpca 512 ¶10 lists partition (b13 in p1's basis); bc12 matches p1 (ask).
difference x42: settled by the text: cpca 512 ¶5 lists co-ownership and undivided ownership (b13 in p1's basis); bc13 matches p1 (ask).
difference x43: settled by the text: cpca 515 ¶1, as x36 (bc7 on B).
difference x44: settled by the text: cpca 512 ¶7, as x38 (bc9 on B).
difference x45: settled by the text: cpca 512 ¶11, as x39 (bc10 on B).
difference x46: settled by the text: cpca 512 ¶11, as x40 (bc11 on B).
difference x47: settled by the text: cpca 512 ¶10, as x41 (bc12 on B).
difference x48: settled by the text: cpca 512 ¶5, as x42 (bc13 on B).
difference x49: both are right: both ask whether the step was done in bc1, under two names of the same slot (x18); B's n1 is silent there.
difference x50: both are right: both ask property_registered in bc2; B's n1 also asks it.
difference x51: both are right: both ask dispute_concerns_real_property in bc3.
difference x52: settled by the text: cpca 519 ¶1 (as x35): in bc6 B's p1 asks, A matched nothing; the reconciled p1 asks.
difference x53: both are right: in bc7 neither matched an item and the questions differ only by the slot's name (x18); the match itself is settled in x36.
difference x54: settled by the text: cpca 519 ¶1 (as x35): in bc8 B's p1 asks, A matched nothing; the reconciled p1 asks.
difference x55: both are right: in bc9 the questions differ only by the slot's name (x18); the match is settled in x38.
difference x56: both are right: in bc12 the questions differ only by the slot's name (x18); the match is settled in x41.
difference x57: both are right: in bc13 the questions differ only by the slot's name (x18); the match is settled in x42.
difference x58: both are right: in bc14 both ask whether the step was done, under two names of the same slot; B's n1 is silent there.

Counts: settled by the text 39; both are right 18; not settled 1; needing a text 0.

## Proposed slots

- A `petition_published_at_land_registry` = B `petition_deposited_for_publication` → kept `petition_deposited_for_publication` (deposit is the act common to both accepted means of 17 ¶3; the question names publication and proven deposit). No other proposed slot.

## Boundary cases

disputed bc4: 17 ¶3 «بعقار و/أو حق عيني عقاري مشهر» covers a published real right even on a property that is not published, and «مشهر» may qualify the right alone (o7); a "no" on the property's publication does not settle that the duty does not arise, so the text gives unsettled (the record warns through n1), not stepExempt. The run passes because p1 is silent and n1 is not judged for stepExempt.
disputed bc10: same paragraph and reason as bc4 (lease of a dwelling on a property that is not published): unsettled, not stepExempt.

## Where A and B agreed, re-read

- p1 (kind, warn when missing, ask when unknown, interpretive): confirmed on 17 ¶3. The sanction is explicit, but the moment is the first hearing and the scope is a reading (x4, o1), so warn stays. Its basis changed: A's 16 ¶1-2 left out (registration, not the step), and 515 ¶1, 512 and 512 ¶7 added (x8, x9, x38).
- p1 acceptedMeans: A's two means (published petition produced at the first hearing; proven deposit) kept, both in 17 ¶3's words; B's single means is m2.
- No deadline: confirmed. 17 ¶3 fixes no period in days, only the first hearing (C agrees).
- doc1 and doc2: kept; doc1 rests on 17 ¶3 only (x2).
- Boundary on seizure and adjudication entries (A n1, B n2): kept as n2, with 762 ¶2 added to B's 652 ¶2. B's says of 652 kept "a step of the seizure".
- B's 512 says named "proof of real-property ownership" (¶6, not cited) and omitted co-ownership (¶5, cited): corrected.
- Checklist: A's k15 (pre-emption) changed from searchedNotFound to applies (512 ¶7); B's k14 and k21 (k42, k49) changed to applies (539 ¶3); A's k24 changed from applies to searchedNotFound (17 ¶3 names no one), like B's k34.
- Examples: B's ex1 expected section realEstate, which this record no longer proposes (x16): removed. A's ex8 dropped as a duplicate of ex1; B's ex3 dropped as a duplicate of A's ex3. All other examples of both kept with the merged slot id.
- Neighbour flags answered by k56 (539 ¶2), k57 (2 ¶1), k58 (40 ¶10). No cited article has an inForceNote.
- Third derivation: C marks the prior step not interpretive and proposes realEstate; its attacks list is empty. The text does not settle C's "not interpretive" (x4); C's section is settled against this module by 511 ¶1 (x16).
- Request appended to law/requests.jsonl for Decree 76-63 art. 85 (the provisions 519 ¶1 refers to; A's k29 says it was requested, which I could not check).

## Questions left to the adversary

1. Is b1 rightly interpretive (x4), or do the words settle that «مشهر» qualifies both nouns? If they do, n1 and the disputes of bc4 and bc10 fall.
2. Does putting 512 (leases, possession, partition, pre-emption) in p1's basis overstate 17 ¶3, whose scope o1 leaves open?
3. Should t1 stay here, or does it restate the territorial-jurisdiction module?
4. Is «عدم قبولها شكلا» a plea of form (50 ¶1) or a plea of inadmissibility (67 to 69)? It decides whether the defect can still be raised after the merits.
5. Should p1 stay a warning? The sanction is explicit; the record warns because of the first-hearing moment and the open scope.
