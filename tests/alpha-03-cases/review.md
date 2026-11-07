# Author review record

**Review completed: 144/144.** Codex authored and performed a separate semantic pass over all 36 families and all four contrasts, before any candidate inference. This is an author review, not a second human annotation or independent legal validation. Static checks additionally use the repository's actual catalogue, sentence splitter and value normalizers; they do not establish legal truth.

The review checked the complete common facts as well as each contrast: a denial must target the full asked proposition, a party contest is disputed, an absent target must not be asserted elsewhere, and every case must retain a realistic claim basis. Binding variants were checked against the requested remedy and its particular source, including document dates, prior third-party events, amounts not requested and rate/total distinctions. All case/source corrections occurred before the freeze, without predictions.

| Family | Cases | Kind | Semantic review focus |
|---|---|---|---|
| f01 | 001–004 | polarity | Notice differs from unrelated no-pledge/no-payment statements; implicit plaintiff assertion in party denial |
| f02 | 005–008 | polarity | Loan remains asserted; separate acknowledgment, cross-sentence contest and office receipt identifier |
| f03 | 009–012 | polarity | Seller delivery conceded while notice is contested; non-payment remains affirmed |
| f04 | 013–016 | polarity | Unquantified instalment and account balance; no fabricated monetary binding |
| f05 | 017–020 | polarity | Stopped works ground rescission independently of demand; irrelevant key handover negation |
| f06 | 021–024 | polarity | Work stoppage remains; restitution requested only for asserted/disputed advance |
| f07 | 025–028 | polarity | Fraud remains alleged in all cases; payment may be denied/absent without destroying annulment basis |
| f08 | 029–032 | binding | Actual payment versus total price, expenses and damages; no refund request in fourth case |
| f09 | 033–036 | polarity | Damaged cargo grounds damages; explicit on-time delivery differs from notice status |
| f10 | 037–040 | polarity | Nonconformity grounds claim even if delivery was on time; delay date is actual alleged delivery date |
| f11 | 041–044 | polarity | Civil injury remains asserted regardless of criminal conviction status |
| f12 | 045–048 | binding | Injury versus conviction/medical/copy dates; expenses/rates versus requested damages; no conviction assertion in fourth case |
| f13 | 049–052 | polarity | Cart harm remains; role text does not pre-assert the optional dog injury |
| f14 | 053–056 | polarity | Machine damage remains; alleged wall collapse separate from irrelevant no-fire statement |
| f15 | 057–060 | polarity | Named notice document alone affirms notice; party contest leaves lease-start date conceded |
| f16 | 061–064 | binding | Monthly rent, arrears and deposit; partial monetary claim in second, no monetary claim in fourth; request negation scoped correctly |
| f17 | 065–068 | polarity | Titleless occupation remains; notice to leave independently varies |
| f18 | 069–072 | binding | Title/occupation/service/copy dates and cadastral IDs; third case seeks eviction without damages |
| f19 | 073–076 | polarity | Supply and full unpaid value remain; shipment record is not a demand |
| f20 | 077–080 | polarity | Open-account instalment; defendant assertion contested by plaintiff is still disputed; no invented balance |
| f21 | 081–084 | polarity | Lessor arrears claim; eviction warning does not automatically assert renewal refusal |
| f22 | 085–088 | binding | Plaintiff is lessee; paid rent denies arrears, defendant sends warning; compensation differs from monthly rent/moving costs/expert estimate |
| f23 | 089–092 | polarity | Missing delivery after advance; no refund/termination request merely because payment occurred |
| f24 | 093–096 | polarity | Price payable on delivery permits a no-advance claim; delivery request remains regardless of advance status |
| f25 | 097–100 | binding | Marriage affirmed; separation, previous third-party divorce and copy dates not substituted for current marriage; visitation only where requested |
| f26 | 101–104 | polarity | Non-maintenance independently grounds dissolution; abandonment actor is defendant |
| f27 | 105–108 | polarity | Non-maintenance remains; harm denial scoped to harm, not marriage/named document |
| f28 | 109–112 | binding | Marriage affirmed; offer, historical dowry, waiting-period/child components, explicit aggregates and monthly housing; no invented aggregate |
| f29 | 113–116 | polarity | Child maintenance is realistic whether parents divorced, still married, contesting divorce, or divorce unstated |
| f30 | 117–120 | binding | Marriage/divorce/copy dates; arrears and housing amounts, including actual monthly requests; no imputed event amount where catalogue asks none |
| f31 | 121–124 | polarity | Present parental care differs from child residence in a centre, contest over grandmother's care, or unstated residence; mother can seek custody throughout |
| f32 | 125–128 | polarity | Independent alleged unfitness remains; remarriage with a non-mahram varies without dropping claim basis |
| f33 | 129–132 | polarity | Independent relinquishment remains; settlement abroad differs from a temporary visit |
| f34 | 133–136 | polarity | A first visitation request remains realistic without prior visitation judgment; divorce is a separate event |
| f35 | 137–140 | binding | Reason disputed → act affirmed; act disputed → disputed; document-only act, three distinct dates, wage rate versus damages |
| f36 | 141–144 | polarity | Unpaid wages remain; named conciliation document versus pending procedural record, party contest or silence; pending case is preparation notes |

Pre-freeze corrections found through these checks included a missing abandonment actor, a phrase that could have implied animal harm in all four variants, a notice-only source marker despite other marital assertions, and request wording whose negation could have scoped over eviction. They were corrected in source and regenerated before freezing. No original cases were changed and no model answer motivated a correction.

The cases intentionally include concise and longer notes, ordinary affirmed controls, inverse/negative propositions, multiple events in a sentence, cross-sentence disputes, document-only assertions, role variants, identifiers, rates beside totals and more than one legitimate value in a cited sentence. Longer notes are still synthetic and do not certify coverage of real multi-page files. All post-freeze annotation concerns go to the owner for adjudication and a disclosed new hash; no silent repairs.

## Revision 2 author review — owner-adjudicated correction

The owner accepted the first freeze's hashes, coverage, groups and sampled labels, and adjudicated removal of artificial explanatory case-text hints. This is the authorised exception to the mutation policy, before any inference. All 144 cases / 381 distinct sentences were reviewed. The old record is preserved in `revisions/freeze-v1.json`; the exact edit inventory and unchanged-record checks are in `revisions/v2-text-correction.json`.

123 cases / 219 sentence texts change. Every gold/dense label and value, date, actor, case/family/split/fold field, source ID, source field and citation edge is identical to the first commit. All numeric tokens, including identifiers and unlabeled competing figures, remain in their original sentence and original order. No sentence is added, removed, merged, split or moved. The complete text snapshot is refreshed to the rewritten input, as required by `CaseText` checks.

Natural rewrites preserving attribution/scope rather than simply deleting a clause: `a03-061`, `a03-062`, `a03-063`, `a03-064` (rent and actual deposit payment); `a03-098` (prior divorce from another wife); `a03-109`, `a03-110`, `a03-112` (current relief versus maintenance components, prior maintenance and housing units); `a03-117`, `a03-119` (periodic entitlement versus accrued debt); `a03-137`, `a03-140` (a disputed reason without denying the dismissal act). No unresolved label ambiguity is retained.

Removed wording includes total/independent-amount emphasis, instructions not to count deposits or expenses, declarations that a number is not an amount, absent-value announcements and document-only/source-exclusion explanations. Basic factual units and legal nouns remain: a note still has to state a payment, rent obligation, security deposit, expense or actual remedy, without telling the reader which competing candidate to select. Ordinary `فقط` in a request can remain. No model answer motivated the review.

The audit's disputed evidence is sparse: nine target propositions, ten disputed event labels overall. The disputed-recall gate is indicative only; the count and original threshold remain reported. This revision changes neither the split nor the gate to compensate for that limitation.

## Revision 3 author review — owner-adjudicated decisive flags

Review completed: 144/144 for omission criticality against the unchanged case text, requested remedies and claim catalogue. This is author review under the owner's corrected definition, not independent legal validation. The 25 interrupted reference answers were not used to select individual flags; the decision is a corpus-wide counterfactual omission rule. That partial run is excluded from all comparisons, without retrospective rescoring.

548 dense events now carry explicit flags: 422 true / 126 false. Their 428 non-absent gold projections have 395 true / 33 false. All 108 polarity-family target decisions, including 27 absent targets, remain decisive. Non-target absence stays densely measured. No other annotation or input changes.

Gold true-to-false changes (every affected event):

| Cases | Event | Reason |
|---|---|---|
| a03-001–a03-008 | payment_made | Denied payment duplicates affirmed unpaid loan/debt |
| a03-009–a03-012 | payment_made | Denied payment duplicates affirmed unpaid sale price |
| a03-032 | payment_made | Historical payment without a restitution request; annulment/damages grounds remain |
| a03-033–a03-036 | delivery_late, delivery_missing | Negative alternative breaches; actual broken-goods breach is affirmed other |
| a03-037–a03-040 | delivery_missing | Delivered but nonconforming goods; affirmed other carries the claimed breach |
| a03-069–a03-072 | notice_served | Prior demand is ancillary to titleless-occupation eviction/damages |
| a03-073–a03-076 | payment_made | Denied payment duplicates affirmed unpaid commercial price |

All remaining present events stay true for distinct grounds, performance/relationship facts, procedural context or the owner's target override. In particular, independently harmful acts stay separate, prior criminal rulings retain their adjudication context, lease start differs from formation/accrual, and the indemnity-seeking lessee's no-arrears assertion is not duplicated by an affirmed payment event. The full per-event inventory is `revisions/v3-decisiveness.json`; its verifier enforces exactly these 33 flag changes and every other field's identity against v2.
