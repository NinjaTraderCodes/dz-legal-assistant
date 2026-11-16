# The label noise of the pilot's kept notes (17 November 2026)

**What it measures.** How often a kept training note's label (the writer's, from the scenario) is wrong. The notes were kept on the code checks alone: the owner's route of 16 November, with no local filter. A random 100 of the 493 kept pilot notes (seed 1701) were relabelled blind by Opus 5.5, 25 a run, with the noise-audit prompt and no tool. Then one Opus run per 40 differing fields sorted each difference against the guide (`prompts/notes-sort-dropped.md`). **Nothing was changed:** no note was corrected, dropped or kept because of it (TERMS.md).

## The figure

| | |
|---|---:|
| compared fields (either side gives something other than notMentioned or unknown; not blanked; Opus's `unclear` excluded) | 1474 |
| fields where the kept label differs from Opus's and the sorter says the kept label is wrong (`checker` or `neither`) | 8 |
| **label noise** (wrong kept labels over compared fields) | **0.5%** (field interval 0.3% to 1.1%, too narrow: fields of one note are not independent) |
| notes with at least one wrong kept label | 7 of 100 (7%; Wilson 95 % 3% to 14%) |
| unsettled (both readings hold, or the guide does not decide), counted apart | 28 |
| fields Opus answered `unclear`, counted apart | 3 |
| differences in the quotes only (the same label), not label fields | 0 |
| differing fields where the sorter found the kept label right | 5 |

**Against the owner's bar of 10 %:** under 5 %: go on.

## Where the wrong kept labels are

| Field type | Compared | Wrong kept labels | Share |
|---|---:|---:|---:|
| element polarity | 262 | 5 | 1.9% |
| element amount | 75 | 1 | 1.3% |
| request kind | 145 | 1 | 0.7% |
| what a date or amount is for | 286 | 1 | 0.3% |
| claim type | 100 | 0 | 0.0% |
| plaintiff's role | 33 | 0 | 0.0% |
| element actor | 195 | 0 | 0.0% |
| slot public_body_party | 67 | 0 | 0.0% |
| request amount | 62 | 0 | 0.0% |
| element disputedBy | 50 | 0 | 0.0% |
| element date | 123 | 0 | 0.0% |
| slot contract_date | 18 | 0 | 0.0% |
| slot notice_date | 13 | 0 | 0.0% |
| slot notice_means | 14 | 0 | 0.0% |
| slot notice_served | 20 | 0 | 0.0% |
| slot property_registered | 7 | 0 | 0.0% |
| slot nonconciliation_record | 6 | 0 | 0.0% |
| slot nonconciliation_record_delivered_on | 1 | 0 | 0.0% |

The most common kinds: element polarity (payment_made) (2); element polarity (delivery_missing) (2); element polarity (delivery_late) (1).

## Each wrong kept label

- `n-pilot-00050`, `events/unpaid-sale-price/payment_made/polarity` (cause: writer): «تخلف نجاة بلعيد عن أداء الثمن» is a «لم يدفع», and by rule 3 and case 1 it denies payment_made. The note adds that the sum owed is the whole price, ١٤.١٠٠.٠٠٠ دج.
- `n-pilot-00070`, `events/contract-rescission/contract_concluded/amount` (cause: writer): One agreement carries both claims, and the note contests its amount: «تحصر مقابله في ٩.٨٤٠.٠٠٠ دينار». By case 31 the amount is labelled amountDisputed for every claim that rests on that contract. The scenario's contest on contract-damages alone cannot be read from the note.
- `n-pilot-00070`, `values/facts:186` (cause: writer): Case 31 and section 8 say that when the other side gives its own figure, the plaintiff's figure «ستة عشر مليونا وخمسمائة ألف (١٦.٥٠٠.٠٠٠) دينار» carries amountDisputed: true. The note contests it in the very next line.
- `n-pilot-00117`, `events/commercial-debt/payment_made/polarity` (cause: writer): The note reports the defendant's claim «أنها سوت كامل الثمن نقدا لدى وكيل مبيعات» against the note's own «عدم دفع أي قسط». Case 4 makes payment_made disputed (disputedBy plaintiff), not denied.
- `n-pilot-00125`, `events/contract-damages/delivery_missing/polarity` (cause: writer): «التنفيذ الذي قامت به توررون جاء بعد موعده» says the performance took place, late. Case 13 makes that delivery_late stated and delivery_missing denied; the unfinished work belongs to work_stopped.
- `n-pilot-00159`, `requests/other` (cause: writer): The requests field asks the court «قبول تأسيس عبد الحكيم طاهري طرفا مدنيا في القضية الجزائية المفتوحة». Section 7 labels every request the note asks, and one that fits no kind is other. The writer left it out.
- `n-pilot-00479`, `events/contract-damages/delivery_late/polarity` (cause: writer): «سلمت المؤسسة محل العقد في موعده دون نقص» is the denied form of delivery_late in table 5.1 («تم التسليم في الأجل المتفق عليه»). A note that says the delivery came on time cannot leave delivery_late notMentioned.
- `n-pilot-00488`, `events/contract-damages/delivery_missing/polarity` (cause: writer): «وكان التنفيذ المتأخر في 2021/04/01» says the performance took place, late. Case 13 makes delivery_late stated and delivery_missing denied.

## Unsettled (the guide does not decide, or both readings hold)

- `n-pilot-00007`, `events/unpaid-sale-price/price_unpaid/amount`: The facts say «عدم سداد الباقي» with no figure (case 3: an amount only if the note gives the remainder). The only figure is in the request «إلزامه بأداء 2 607 000 دينار». The guide does not say whether a request's figure gives the remainder. *Guide defect:* Same as case 9: no rule on whether an amount asked in a request is also the amount of the matching unpaid element.
- `n-pilot-00022`, `roles/custody`: «أمهما جازية بوبكر» and «بصفتها أما طالبة للحضانة» both carry marriage/mother (section 3b). Only the place quoted differs. *Guide defect:* Same as case 2: no rule on which of several places to quote, and the script's overlap/same-sentence test rejects two valid quotes.
- `n-pilot-00067`, `events/unpaid-rent/rent_unpaid/polarity`: The facts say nothing of unpaid rent. Only the request «إلزامها ببدلات 1.269.600 دج» presupposes it. The guide does not say whether a request states the event it rests on (rule 1 against section 10). *Guide defect:* Same as case 4: no rule on whether a payment request states the matching unpaid element. Rule: a request states no event unless its own words say the sum is due or unpaid.
- `n-pilot-00104`, `requests/payment/amount`: The request gives the amount only by reference, «بأداء ثمن المبيع بالمقدار المحدد أعلاه»; the figure 3.020.000 is in the facts. The guide has no reference rule for amounts. *Guide defect:* Same as case 7: no rule for amounts given by reference. Rule: they take the figure they refer to, as the same-day test does for dates.
- `n-pilot-00117`, `requests/payment/amount`: The request gives its amount only by reference: «بأداء قيمة البضائع بالمقدار المبين أعلاه». Section 7 attaches an amount asked to its request, but section 8 has a reference rule only for dates (the same-day test), not for amounts. *Guide defect:* No rule for an amount given by reference («بالمقدار المبين أعلاه», «المبلغ نفسه»). Rule: such words take the figure they refer to, as the same-day test does for dates, with no new entry in values; the quote holds the figure and the reference words.
- `n-pilot-00135`, `claims`: The note never names a sale. It speaks of «المقابل المستحق عن البضاعة», goods delivered and a sales agent, which fits unpaid-sale-price and commercial-debt equally. The border table is not in the guide, so nothing decides it. A rewrite that names the sale fixes it.
- `n-pilot-00137`, `events/unpaid-wages/wages_unpaid/amount`: The facts state the unpaid wages with no figure. The amount appears only in the request «أداء أجوره، وقدرها مائتان وستة وسبعون مليون سنتيم». The guide does not say whether a request's figure is the event's amount. *Guide defect:* Same as case 9: no rule on whether an amount asked in a request is also the amount of the matching unpaid element.
- `n-pilot-00138`, `roles/contract-damages`: «تعويض فينكام، بصفتها مشترية في البيع الآخر» (requests) and «ومشترية في البيع الذي نطلب التعويض عن الإخلال بتنفيذه» (facts) both carry sale/buyer. The difference is only the place quoted. *Guide defect:* Same as case 2: no rule on which of several places to quote, and the script rejects two valid quotes that do not overlap or share a sentence.
- `n-pilot-00182`, `roles/custody`: «أمها، موكلتنا شريفة بن زين الدين» (item 1) and «موكلتنا هي الأم التي تطلب إسناد الحضانة إليها» (item 4) both carry marriage/mother (section 3b). Only the place quoted differs. *Guide defect:* Same as case 2: no rule on which of several places to quote, and the script's overlap/same-sentence test turns two valid quotes into a disagreement. Rule: any place that carries the same label is a valid quote.
- `n-pilot-00192`, `requests/damages`: Both quotes carry the damages request: «التعويض الذي يريد ثامر سويسي طلبه» (item 2) and «والحكم له بالتعويض المحدد أعلاه» (the closing المطلوب). The note asks it in several places. *Guide defect:* Same as case 2: no rule on which of several places to quote when a request is stated more than once.
- `n-pilot-00198`, `roles/custody`: Both quotes carry marriage/mother (section 3b): «أمهما ضاوية قدور» and «موكلتنا هي أم نادية ووردة». They differ only because the note states the role twice, in two sentences. *Guide defect:* No rule says which place to quote when a note states the same fact more than once, yet the script (section 9) counts two valid quotes in different sentences as a disagreement. Rule: any place that carries the same label is a valid quote, and only the labels are compared (or: always quote the first place that carries the fact).
- `n-pilot-00222`, `claims`: «إسناد حضانة محمد الطيب إلى المدعية» is a request asked with a khul. Under the literal 'own request' rule it is a custody claim; as an accessory of the khul it is not. The guide does not decide. *Guide defect:* Same as case 12: no rule on accessory family requests asked with a dissolution.
- `n-pilot-00262`, `roles/visitation`: «نوارة بن عمر، الأم» and «صفة نوارة بن عمر في هذا الطلب صفة الأم» both carry marriage/mother (section 3b). Only the place quoted differs. *Guide defect:* Same as case 2: no rule on which of several places to quote, and the script's overlap/same-sentence test rejects two valid quotes.
- `n-pilot-00273`, `requests/other`: «الفصل في استحقاق نور الدين سحنون للعربون بعد إلغاء الحجز» asks for a ruling on the right to a deposit the client wants back. Section 7 names the kinds of FactVocabulary.Requests but defines none, so restitution and other cannot be told apart from the guide. *Guide defect:* Section 7 does not define the request kinds. Rule: add a one-line definition for each kind (for example, restitution = the return of a sum or thing handed over, including a deposit).
- `n-pilot-00273`, `requests/restitution`: This is the same request as the field above. Whether a ruling on the right to a deposit is restitution or other depends on a definition of the kinds that the guide does not give. *Guide defect:* Section 7 does not define the request kinds; add a one-line definition for each kind of FactVocabulary.Requests.
- `n-pilot-00311`, `claims`: The khul note also asks «بنفقة ١٠٩٠٠ دج» and «إسناد حضانة عائدة وصهيب». The literal 'own request' rule (section 3) adds nafaqa and custody; reading them as accessories of the khul does not. The guide does not decide. *Guide defect:* Same as case 12: no rule on whether requests accompanying a dissolution (custody, maintenance, visitation) are separate claims or accessories.
- `n-pilot-00327`, `claims`: The note asks «تسليم المبيع أو فسخ البيع». Section 3 says a request to undo a sale makes contract-rescission and that each claim with its own request is labelled. But rescission may also be a remedy of sale-non-delivery itself, and the guide does not say which applies. *Guide defect:* No rule on a request that is one of the labelled claim type's own remedies (rescission asked in a sale-non-delivery note, also as an alternative to delivery). Rule: a request that is a remedy of a labelled claim type adds no claim type unless it rests on facts of its own.
- `n-pilot-00350`, `claims`: Section 3 says to label each claim 'that has its own request', and «نفقة قدرها 36 800 دينار» is a request. But maintenance asked alongside a khul may be an accessory of that claim. The guide does not say which, and it sends borders to a SCHEME table that is not part of the guide. *Guide defect:* No rule on accessory family requests: whether custody, maintenance, housing or visitation asked alongside a divorce, khul or tatliq adds its own claim type. Rule: requests that accompany the dissolution are accessory requests of that claim and add no claim type unless the note gives them facts of their own.
- `n-pilot-00355`, `events/unpaid-sale-price/payment_made/polarity`: «ثمن عقد البيع ... هو 11 900 000 دينار، وما زال في ذمة الشركة» asserts that the price is still owed, not that nothing was paid. Case 32 denies payment_made only 'when it says nothing was', and the guide does not say whether 'the whole price is still owed' says that. *Guide defect:* Rule 3 and cases 1 and 32 do not say whether an assertion that the whole price or sum is still owed («ما زال الثمن في ذمته») denies payment_made. Rule: it denies payment_made only when the note says the whole sum remains or no part was paid; otherwise payment_made is notMentioned.
- `n-pilot-00360`, `events/unpaid-rent/rent_unpaid/amount`: The facts state the arrears with no figure. 630000 appears only in the request «بأداء 630.000 دج من بدلات الكراء». The guide does not say whether a request's figure is the event's amount. *Guide defect:* Same as case 9: no rule on whether an amount asked in a request is also the amount of the matching unpaid element.
- `n-pilot-00368`, `claims`: The note asks for delivery and also «تضمين طلب فسخ البيع أيضا». Section 3 ties a request to undo a sale to contract-rescission, but rescission may also be a remedy of sale-non-delivery. The guide does not decide. *Guide defect:* Same as case 17: no rule on whether rescission asked in a sale-non-delivery note adds contract-rescission.
- `n-pilot-00380`, `claims`: The note asks «بتسليم المبيع، وفسخ العقد» in a sale-non-delivery and contract-damages note. Section 3's literal rule adds contract-rescission; reading rescission as a remedy of sale-non-delivery does not. The guide does not decide. *Guide defect:* Same as case 17: no rule on whether a request that is a remedy of a labelled claim type (rescission in sale-non-delivery) adds a claim type.
- `n-pilot-00393`, `events/unpaid-wages/wages_unpaid/amount`: The facts say «أجور أشهر محبوسة» with no figure. 5520000 appears only in the request «بدفع أجور قدرها 5 520 000 دينار». The guide does not say whether a request's figure is the event's amount. *Guide defect:* Same as case 9: no rule on whether an amount asked in a request is also the amount of the matching unpaid element.
- `n-pilot-00422`, `claims`: «مع إسناد حضانة منير وقاسم إليها» is asked together with the khul. The literal 'own request' rule (section 3) adds custody; the accessory reading does not. The guide does not decide. *Guide defect:* Same as case 12: no rule on accessory family requests asked with a dissolution.
- `n-pilot-00447`, `events/loan-repayment/debt_unpaid/amount`: The facts give the outstanding sum only by reference, «بقاء المبلغ نفسه في ذمته»; the figure is written in the loan sentence and in the request. The guide has no reference rule for amounts, and it does not say whether a request's figure is the event's amount. *Guide defect:* No rule for amounts given by reference («المبلغ نفسه», «بالمقدار المبين أعلاه»). Rule: they take the figure referred to, as the same-day test does for dates, quoting the figure and the reference words.
- `n-pilot-00458`, `events/unpaid-wages/wages_unpaid/amount`: The facts state the unpaid wages with no figure. 344000 appears only in the request «بأداء ٣٤٤٠٠٠ دينار ... عن مستحقات عمله». Section 4.1 ('the note gives it for this event') and section 8 (one value, several roles) do not say whether an amount asked is also the event's amount. *Guide defect:* No rule says whether an amount that appears only in a request is also the amount of the matching unpaid element. Rule: an event's amount comes from the words that state the event; a figure that appears only in a request takes the request's role only.
- `n-pilot-00482`, `events/unpaid-sale-price/price_unpaid/polarity`: The facts say only that a «شطرا من المقابل» was paid. The checker reads the request «بأداء ٥.٥٤٠.٠٠٠ دج من ثمن البيع» as stating that the price is unpaid. Rules 1 to 4 never say whether a request states the event it rests on, and section 10 forbids inference. *Guide defect:* No rule says whether a request to be paid a sum states the matching unpaid element (price_unpaid, rent_unpaid ...). Rule: a request asks and states no event; the unpaid element is stated only by words in facts or documents, or by a request whose own words say the sum is still due («الباقي», «المتأخرة»).
- `n-pilot-00492`, `claims`: «تمكينه من حق الزيارة» is asked alongside the divorce. Under the literal 'own request' rule it adds visitation; as an accessory of the divorce it does not. The guide does not decide. *Guide defect:* Same as case 12: no rule on accessory family requests (visitation, custody, maintenance) asked with a dissolution.

Files (outside the repository, never in a note): `C:/Users/TheShadow/dz-core-research/notes-work/noise/pilot-1701` (the prompts, the blind labels, `differences.json`, `sort-key.json`, the sorting replies); this page by `exec\noise_report.py`.
