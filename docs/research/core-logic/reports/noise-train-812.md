# The label noise of the first 812 kept training notes (18 November 2026)

**What it measures.** How often a kept training note's label (the writer's, from the scenario) is wrong. The notes were kept on the code checks alone: the owner's route of 16 November, with no local filter. A random 100 of the 812 kept training notes, sampler 1.3 with the wider style (seed 1801) were relabelled blind by Opus 5.5, 25 a run, with the noise-audit prompt and no tool. Then one Opus run per 40 differing fields sorted each difference against the guide (`prompts/notes-sort-dropped.md`). **Nothing was changed:** no note was corrected, dropped or kept because of it (TERMS.md).

## The figure

| | |
|---|---:|
| compared fields (either side gives something other than notMentioned or unknown; not blanked; Opus's `unclear` excluded) | 1468 |
| fields where the kept label differs from Opus's and the sorter says the kept label is wrong (`checker` or `neither`) | 5 |
| **label noise** (wrong kept labels over compared fields) | **0.3%** (field interval 0.1% to 0.8%, too narrow: fields of one note are not independent) |
| notes with at least one wrong kept label | 5 of 100 (5%; Wilson 95 % 2% to 11%) |
| unsettled (both readings hold, or the guide does not decide), counted apart | 11 |
| fields Opus answered `unclear`, counted apart | 1 |
| differences in the quotes only (the same label), not label fields | 0 |
| differing fields where the sorter found the kept label right | 0 |

**Against the owner's bar of 10 %:** under 5 %: go on.

## Where the wrong kept labels are

| Field type | Compared | Wrong kept labels | Share |
|---|---:|---:|---:|
| element polarity | 243 | 3 | 1.2% |
| claim type | 100 | 1 | 1.0% |
| request kind | 179 | 1 | 0.6% |
| element date | 107 | 0 | 0.0% |
| element amount | 55 | 0 | 0.0% |
| element actor | 175 | 0 | 0.0% |
| element disputedBy | 45 | 0 | 0.0% |
| slot contract_date | 16 | 0 | 0.0% |
| slot public_body_party | 81 | 0 | 0.0% |
| request amount | 68 | 0 | 0.0% |
| what a date or amount is for | 284 | 0 | 0.0% |
| plaintiff's role | 47 | 0 | 0.0% |
| slot property_registered | 14 | 0 | 0.0% |
| slot notice_date | 12 | 0 | 0.0% |
| slot notice_served | 17 | 0 | 0.0% |
| slot nonconciliation_record | 10 | 0 | 0.0% |
| slot notice_means | 10 | 0 | 0.0% |
| slot nonconciliation_record_delivered_on | 6 | 0 | 0.0% |

The most common kinds: element polarity (payment_made) (2); request kind (1); element polarity (custody_renounced) (1).

## Each wrong kept label

- `n-train-00141`, `events/custody-forfeiture/custody_renounced/polarity` (cause: writer): Note e decides this. A renunciation said to be forced is still a renunciation and is stated. «وقعت التنازل تحت الضغط وتعترض عليه» admits that she signed and contests only its validity. It would be disputed only if she denied renouncing.
- `n-train-00293`, `claims` (cause: writer): Section 3 says the custody request in a divorce-by-husband note belongs to the dissolution. It adds custody only when the note gives facts of its own: who holds the child now, a refusal of visits, or an earlier judgment. The note gives none: «الزوجية قائمة», and «في طلب قائم بذاته» is only the request's wording. So only divorce-by-husband.
- `n-train-00487`, `requests/cancel_decision` (cause: writer): Section 7 counts requests asked in facts. The note says «موضوع الدعوى التي يريدها عثمان بن نجيب هو إلغاء قرار صادر عن الإدارة», which asks to annul an administrative decision, so cancel_decision is asked. The scenario's request list left it out.
- `n-train-00684`, `events/unpaid-sale-price/payment_made/polarity` (cause: writer): Case 4 decides this. The defendant says it paid in full («تقول إن الثمن تمت تسويته نقدا لدى وكيل بيع») and the plaintiff says the price is unpaid («غير مؤدى بحسب مبروك»), so payment_made is disputed (disputedBy: plaintiff). The scenario's notMentioned is wrong.
- `n-train-00775`, `events/loan-repayment/payment_made/polarity` (cause: writer): Case 4 decides this. «وحسب ميدرون، فقد ردت المال كله» is the defendant's claim of full repayment, and «حسب زانديا، المبلغ غير المعاد» is the plaintiff's opposing claim. So payment_made is disputed, not notMentioned.

## Unsettled (the guide does not decide, or both readings hold)

- `n-train-00510`, `events/sale-annulment-fraud/contract_concluded/date`: Section 8 says 'day before month, always', which read literally makes «2022/09/03» 9 March. The usual year-first order reads it as 3 September. The guide does not address dates written year first, so the checker's unclear and the writer's 2022-09-03 both follow from it. *Guide defect:* Section 8's 'day before month, always' does not cover dates written year first (yyyy/mm/dd). Proposed rule: a date written with the year first is read year, month, day; 'day before month' applies only to dates written with the year last.
- `n-train-00510`, `values/facts:231`: This is the same value as the previous field. Under section 8's 'day before month, always', «2022/09/03» can be read as 2022-03-09, or in year-month-day order as 2022-09-03. The guide does not say which. *Guide defect:* Section 8 has no rule for dates written year first (yyyy/mm/dd). Proposed rule: a year-first date is read year, month, day.
- `n-train-00580`, `events/commercial-debt/payment_made/polarity`: Rule 3 says a bare «لم يدفع» denies payment_made. Case 39 says a sum still owed leaves payment_made notMentioned unless the note says nothing was paid or the whole sum remains. «الفواتير ... لم تسو بعد وقيمتها العالقة ١٢.٨٠٠.٠٠٠ دج» falls between the two. Its outstanding figure equals the delivered value only when the reader compares the figures. *Guide defect:* Rule 3 (bare «لم يدفع» denies payment_made) collides with case 39 (owed sum without «كاملا»/«أي مبلغ» leaves it notMentioned), and the guide does not say whether an outstanding figure equal to the full delivered value counts as 'the whole sum remains'. Proposed rule: payment_made is denied only when words say nothing was paid or the whole sum remains; an unpaid or unsettled statement without such words, even with equal figures, leaves it notMentioned.
- `n-train-00734`, `values/facts:402`: The note is outside (company). «توريدات سلمت في ٢٤/٠٨/٢٠٢٢» describes a delivery, but no commercial-debt claim is labelled, so there is no goods_delivered event. Section 8 does not say whether the value still takes that role. *Guide defect:* Section 8 does not say whether values in an outside note carry event-kind roles. Proposed rule: in an outside note, event-kind roles are left empty.
- `n-train-00734`, `values/facts:442`: Same gap. «ما زال ثمنها مستحقا بمقدار ٣٤ ٣٠٠ ٠٠٠ دينار» describes price_unpaid, but the note is outside and no event is labelled. The guide does not decide the role. *Guide defect:* Section 8 does not say whether values in an outside note carry event-kind roles. Proposed rule: in an outside note, event-kind roles are left empty.
- `n-train-00742`, `values/facts:304`: The note is outside (criminal). Sections 2 and 4 label events only for the elements of labelled claim types, while section 8 gives a value the roles of 'the event kinds ... the value belongs to'. The guide never says whether a date in an outside note («23 جانفي 2021» of the acknowledgment) takes an event-kind role. Request roles do apply in outside notes. *Guide defect:* Section 8 does not say whether values in an outside note carry event-kind roles when no claim type, and so no event, is labelled. Proposed rule: in an outside note, values carry only request-kind roles; event-kind roles stay empty.
- `n-train-00742`, `values/facts:326`: Same gap as facts:304. The acknowledgment amount «4.170.000 دج» describes a debt_acknowledged event, but the note is outside, so no such event is labelled, and section 8 does not say whether the role still applies. *Guide defect:* Section 8 does not say whether values in an outside note take event-kind roles. Proposed rule: in an outside note, event-kind roles are left empty.
- `n-train-00742`, `values/facts:449`: Same gap. «المبلغ الذي لا يزال في ذمة الشركة هو 4.170.000 دج» describes debt_unpaid, but in an outside note no event is labelled, and the guide does not settle the value's role. *Guide defect:* Section 8 does not say whether values in an outside note take event-kind roles. Proposed rule: in an outside note, event-kind roles are left empty.
- `n-train-00742`, `values/facts:552`: Same gap. «إعذار رسمي بتاريخ 01/09/2024» dates a notice, but the note is outside: section 6 makes its slots unknown and unscored, and section 8 says nothing about event-kind roles for such notes. *Guide defect:* Section 8 does not say whether values in an outside note take event-kind roles. Proposed rule: in an outside note, event-kind roles are left empty.
- `n-train-00811`, `events/abusive-dismissal/employment_started/polarity`: «ينكر مستخدمه عادل رابية» names the defendant as the plaintiff's employer, which presupposes the work relation. The guide does not say whether a noun naming a party's relation states the element «التحق المدعي بالعمل لدى المدعى عليه» (rule 1) or is an unwritten inference (section 10). *Guide defect:* The guide does not say whether a relation noun that presupposes an element (مستخدمه, المؤجر, المشتري) states that element. Proposed rule: a noun that names a party by the relation the element creates states that element (stated, no date).
- `n-train-00811`, `events/unpaid-wages/employment_started/polarity`: Same words, «مستخدمه عادل رابية», and the same gap: rule 1 against section 10, with no rule on whether a relation noun states employment_started. *Guide defect:* The guide does not say whether a relation noun that presupposes an element states that element. Proposed rule: a noun that names a party by the relation the element creates states that element.

Files (outside the repository, never in a note): `C:/Users/TheShadow/dz-core-research/notes-work/noise/train-1801` (the prompts, the blind labels, `differences.json`, `sort-key.json`, the sorting replies); this page by `exec\noise_report.py`.
