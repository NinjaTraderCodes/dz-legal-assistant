# Annotation rules — Alpha-03 authored development material

These are the owner's item-7 rules, applied before inference. They do not decide whether a legal claim will succeed. All notes, identifiers, dates and amounts in this corpus are fictitious.

1. **Affirmed:** the notes assert the complete element proposition, including a proposition that is itself negative. “لم يدفع” affirms non-payment and denies payment. A document named in `documents.list` affirms the event it is named after: notes-level assertion, not judicial proof.
2. **Disputed:** a party denies or contests the fact on which the plaintiff relies, even if the plaintiff's assertion is implicit in bringing the claim. “The defendant denies receiving the loan” is disputed, never notes-level denied or absent. Scope the contest to its proposition.
3. **Denied:** the notes themselves say the element did not occur, for example “لم يوجه أي إعذار”. A report of a party's denial is disputed. An unrelated negation does not reverse another event in the same sentence.
4. **Absent:** none of the four read fields asserts, denies, contests or names a document asserting the proposition. A failed/invalid model decision will be unresolved, never a default absent label.

The dismissal element describes the act alone: a contested reason leaves the dismissal affirmed; a contested act makes it disputed. A named document does not override an explicit contest of that same event. This corpus contains no unadjudicated contradictory assertion/notes-level-denial pair.

The owner replaced unrealistic polarity contrasts with binding/request contrasts. Divorce-by-husband and khul keep marriage affirmed in all eight cases. Nine binding families are identified in the family plan and split manifest, each with its substitution reason. The remaining 27 families each contain affirmed/denied/disputed/absent target contrasts; other facts retain the statuses the complete file supports. An ancillary fact may be denied while another asserted fact continues to ground the claim. Pending procedural information may appear in realistic preparation notes; it does not deny the underlying wage claim.

## Evidence and values

`annotation.sentences` is an exact snapshot of existing `CaseText.From`: field order `claim.subject`, `notes.facts`, `notes.requests`, `documents.list`, with `s1...` IDs. Each non-absent event and present request has source IDs. Every non-null date/amount has its own cited source, contained in that item's evidence. A disputed event's date/amount records the allegation, not a finding of truth. `roleEvidence` supports the stated relation/position.

`documentsOnly` is information about an event supported only by the annotated document assertions; the semantic review includes other assertions in the notes. It never changes polarity. For marriage files whose role text already asserts a marital relationship, role text is included as evidence and the flag is false even when its date comes only from a document.

Null dates/amounts mean no uniquely specified explicit binding or no value applicable to that question. They are not zero, fabricated values, a licence for arithmetic, or automatically forbidden values. In `f04` and `f20`, unquantified account/payment statements deliberately stay unquantified. Do not substitute the original sale/supply value for an unstated balance. Non-null expected values remain required by supplementary completeness checks; omissions must not improve a score.

`valueNotes` names competing values with their source sentence: rates, expressly written maintenance components, document-issue dates, prior events involving third parties, expenses, expert estimates and identifiers. Amounts requested monthly stay monthly if that is the actual request; do not invent a total. Explicit requested aggregates take precedence over component/rate numbers. A requested amount need not equal the entire underlying debt: `a03-062` asks for a stated part. Requests are annotated only when actually sought, including explicit exclusions and omissions.

`gold` is the unchanged evaluator's projection: non-absent events with their statuses, present requests, and forbidden affirmations for every non-affirmed/absent proposition. It does not extend the evaluator's sparse absence/request checks. Report the existing critical-error result and supplementary dense correctness/completeness separately; a legacy pass is not automatically a dense pass. No original answer key, evaluator implementation or fixed pass mark is changed.

## Revision 3: explicit decisiveness (owner-adjudicated)

Apply `docs/HIDDEN-SET.md`: decisive is true **when a reader that misses this fact would get the case wrong**, rather than true for every annotated event. Every event has an explicit boolean in the dense key; every non-absent projected gold event has the same explicit boolean. All four states of a family's contrast target remain decisive by owner instruction. A non-target absent question is false; absent targets stay true in the dense key but remain excluded from legacy `gold.events` as before. Their forbidden affirmations and dense status measure still apply.

Keep distinct claim foundations, material independent grounds, remedy prerequisites and material procedural/prior-adjudication facts decisive. In particular, separate harmful acts are not duplicates merely because either can support damages; a prior ruling is a distinct adjudication fact, and the lease's performance/accrual start is distinct from contract formation. Denied arrears in the lessee's indemnity file is a separate compliance assertion, not redundant payment negation.

Non-decisive backgrounds: denied payment already carried by affirmed unpaid debt/price in f01/f02/f03/f19; denied alternative delivery breaches beside the actual damage/nonconformity ground in f09/f10; a03-032's historical payment where only annulment/damages are sought; f18's ancillary prior demands where titleless occupation grounds eviction/damages. Contrast-target override always applies. No polarity is demoted by default. The exact 548 decisions/reasons and 33 true-to-false gold changes are inventoried in `revisions/v3-decisiveness.json`.

This affects omission criticality only through the existing evaluator. Wrong returned polarity/forbidden affirmation/value errors retain their original scoring; all dense statuses, known-value omissions and source measures retain their original denominators. The interrupted v2 partial run is preserved historically and excluded from every future comparison; references restart on v3 from case 1.
