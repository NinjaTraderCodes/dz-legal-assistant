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

`gold` is the unchanged evaluator's projection: non-absent events with their statuses, present requests, and forbidden affirmations for every non-affirmed/absent proposition. All non-absent events are retained as decisive for this authored diagnostic key. It does not extend the evaluator's sparse absence/request checks. Report the existing critical-error result and supplementary dense correctness/completeness separately; a legacy pass is not automatically a dense pass. No original answer key, scoring rule or pass mark is changed.
