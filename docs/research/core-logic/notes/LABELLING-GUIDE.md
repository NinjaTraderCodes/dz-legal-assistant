# Labelling guide for lawyer notes

**Who reads this.** Every agent that writes a note, labels a note, judges a label or scores an answer: the Codex writer, the Sonnet 5.5 judge, the isolated writers of hidden set v3, the blind relabeller, and the author of the scorer. Each of them gets this file whole, and nothing else about labelling. If two careful readers of this guide label one note differently, **the guide is wrong**, not the readers: report the sentence (step 3 keeps a list) and the guide is fixed before the label scheme is frozen.

**What a label is.** A label records **what the note says**, never what is true, likely or legally right. The note is a lawyer's working note before drafting. The reader built in step 4 must find in it the facts the rules need, and show the lawyer the words each one comes from. So every label carries **the exact words of the note that carry it**. A label with no words in the note is not allowed.

**Language.** Notes are formal Arabic only (the owner's decision for this stage): no Darija, no French, no mixed text. Names of months as Algerians write them (جانفي، فيفري، مارس، أفريل، ماي، جوان، جويلية، أوت، سبتمبر، أكتوبر، نوفمبر، ديسمبر) are formal usage here. All data is fictitious: no real person, company, case number or address.

---

## 1. The note

A note has the four fields the app gives the reader (`CaseText.From`), in this order:

| Field | App field | What a lawyer puts there |
|---|---|---|
| `subject` | `claim.subject` | a short title of the claim; often empty |
| `facts` | `notes.facts` | the story, in the lawyer's own order; the only field that is always filled |
| `requests` | `notes.requests` | what to ask the court; may be empty when the requests are inside `facts` |
| `documents` | `documents.list` | the documents to attach, one per line; may be empty |

## 2. What is labelled

| Label | Values | Section |
|---|---|---|
| Claim type | one or more of the 21 ids of `data/law/claims`, or `outside` | 3 |
| Events | for each element of each labelled claim type: a polarity, the words, and the date, amount and actor when the note gives them | 4, 5 |
| Slots | each slot of [`../slots.json`](../slots.json) with `readFromNote: true` | 6 |
| Requests | the request kinds of `FactVocabulary.Requests` the note asks | 7 |
| Dates and amounts | every one in the note, normalised, with what it is for | 8 |

The file format is [`note.schema.json`](note.schema.json). Writers and judges give **quotes** (exact words copied from the note); a script turns each quote into a position (`field`, `start`, `length`, counted in UTF-16 units of the field as written). A quote that is not found letter for letter in the field it names makes the label invalid. Quote the shortest stretch that carries the fact by itself, normally 2 to 12 words; never a whole paragraph. When the same words occur twice in a field, add `occurrence` (1 for the first).

## 3. The claim type

- Label the claim the lawyer wants to bring, as the note shows it, by the **facts and the requests together**. The same facts can carry different claims: an unpaid price with a request to pay is `unpaid-sale-price`; with a request to undo the sale it is `contract-rescission`.
- **Several claims in one note:** label each one that has its own request (a worker who asks for his unpaid wages and for reinstatement after dismissal: `unpaid-wages` and `abusive-dismissal`). At most three. A fact mentioned in passing, with no request, is not a claim.
- **`outside`:** the matter is not one of the 21 types. Give `outsideKind`, one of: `administrative` (a claim against the State, a wilaya, a commune or a public administrative body; annulment of an administrative decision), `criminal` (a complaint, a civil party before the criminal judge), `family-other` (inheritance, kafala, proof of marriage or of descent, guardianship of an adult), `civil-status` (correction of a civil-status record), `property-other` (partition, boundary, pre-emption, possession), `payment-order` (a request for an order to pay), `urgent` (a request to the judge of urgent matters), `enforcement`, `company`, `other`. `outside` is never combined with a claim id.
- **Near the border:** the rule records list, for each type, the cases that look like it and are not (`boundaries`). The frozen label scheme carries that list as a table "looks like / is / what tells them apart". Use it; do not decide a border from your own knowledge of the law.

## 4. Events and polarity

Each claim type has **elements** (`data/law/claim-elements.json`): 75 in all, listed in section 5. An element is a **proposition**, written in Arabic, for example «وُجّه إلى المدعى عليه إعذار أو تنبيه أو مطالبة بالدفع». For each element of each labelled claim type, say how the proposition stands in the note:

| Polarity | Name in the app | When |
|---|---|---|
| `stated` | `affirmed` | The note asserts the proposition. |
| `denied` | `denied` | The note itself says the proposition did not happen. |
| `disputed` | `disputed` | One side claims the proposition and the other denies or contests it. |
| `notMentioned` | (absent) | None of the three. No words are quoted. |

These are the definitions of `docs/HIDDEN-SET.md` with the four rules the owner agreed on 6 November 2026. They are applied **exactly**; hidden set v2 was built on them.

**Rule 1. Stated.** The notes assert the proposition, **including a proposition that is itself negative**: «لم يتقاض المدعي أجوره» states `wages_unpaid`. A document named in `documents` states the event it is named after («محضر عدم المصالحة» states `conciliation_failed`; «عقد الإيجار» states `contract_concluded`). This records an assertion in the notes, not proof.

**Rule 2. Disputed.** A party denies or contests **the same fact** the other relies on. The plaintiff's assertion may be implicit in bringing the claim. «ينكر المدعى عليه استلام القرض» is `disputed` for `money_lent`, not denied and not absent. The contest attaches to **the proposition the element states**, not to every event in the sentence: the dismissal element is the act alone («سرّح المدعى عليه المدعي أو فصله من العمل»). An employer who contests only its reason («يدعي المدعى عليه أن الفصل كان بسبب خطأ جسيم») leaves the dismissal `stated`; an employer who contests the act («ينكر أنه فصله ويدعي أنه ترك منصبه») makes it `disputed`.

**Rule 3. Denied.** The notes themselves say the proposition did not happen, rather than reporting a party's opposition. «لم يوجه أي إعذار» is `denied` for `notice_served`. **Negation is read against the whole element:** «لم يدفع» states non-payment (`debt_unpaid`, `price_unpaid` ...) and denies payment (`payment_made`); in «لم يدفع رغم إعذاره» the notice is `stated`.

**Rule 4. Not mentioned.** No assertion, no denial by the notes, no party's contest and no named document for that proposition.

Label each event at the granularity it describes. **A named document does not override an explicit contest** of that event. If a sentence seems to fit two rules, **do not invent a tie rule**: a writer rewrites the sentence; a judge marks the field `unclear` (the note is then handled by the disagreement rule).

### 4.1 With each event

- **`evidence`:** the quotes that carry the polarity. For `disputed`, quote the contest (and the assertion too when it is written).
- **`actor`:** who did it, as the note says: `plaintiff`, `defendant`, `other`, `unknown`.
- **`disputedBy`:** for `disputed`, who contests: `plaintiff`, `defendant` or `other`.
- **`date`, `amount`:** when the element takes one (columns D and A in section 5) and the note gives it **for this event**. Section 8 says how to normalise. A date or an amount is labelled also when the polarity is `disputed` («ينكر استلام مبلغ 500.000 دج»: the amount belongs to the disputed event). A `denied` event has no date and no amount.

### 4.2 The hard cases, decided

Learn these; most disagreements between judges come from them. Their sentences illustrate a rule and are **not stock phrases** (section 5): a writer must not reuse them, and a judge must not expect them.

| # | The note says | Label | Why |
|---|---|---|---|
| 1 | «لم يدفع المدعى عليه أي مبلغ» | `payment_made` denied; `debt_unpaid` (or `price_unpaid`, `rent_unpaid`) stated | Rule 3, the whole element. |
| 2 | «دفع 200.000 دج وبقي في ذمته 300.000 دج» | `payment_made` stated, amount 200000; `debt_unpaid` stated, amount 300000 | A partial payment states both. Each amount goes to its own event. |
| 3 | «لم يدفع إلا 200.000 دج» ، «لم يسدد سوى قسطين» | `payment_made` stated (amount 200000 in the first); `debt_unpaid` stated, with an amount only if the note gives the remainder | «لم … إلا» and «لم … سوى» are a **restriction, not a negation**: the thing after إلا happened. |
| 4 | «يدعي المدعى عليه أنه سدد كامل المبلغ» (the plaintiff claims the debt) | `payment_made` disputed, `disputedBy: plaintiff`; `debt_unpaid` disputed, `disputedBy: defendant` | Each proposition is claimed by one side and contested by the other. |
| 5 | «ينازع المدعى عليه في مقدار الدين دون أصله» | `debt_unpaid` stated; the amount is labelled with `amountDisputed: true` | The contest is on the amount, not on the proposition. |
| 6 | «لم يوجه أي إعذار كتابي واكتفى المدعي بمطالبته شفويا» | `notice_served` **stated** (the element includes «مطالبة»), evidence «بمطالبته شفويا»; slot `notice_served` = **no** | The event's proposition is wider than the slot (section 6). |
| 7 | «سيوجه إعذار قبل رفع الدعوى» ، «يعتزم المدعي إعذاره» | `notice_served` notMentioned; slot `notice_served` = unknown | An intended act is not an event. With an explicit «لم يوجه بعد»: denied, slot = no. |
| 8 | «لا يعلم المدعي إن كان الإعذار قد بلغ» ، «يتحقق من وجود محضر» | notMentioned; slot = unknown | A fact said to be unknown is not stated and not denied. |
| 9 | «هل وجه إعذار؟» (a question the lawyer notes for himself) | notMentioned | A question asserts nothing. |
| 10 | «لم ينكر المدعى عليه الدين» | `debt_acknowledged` notMentioned | Not denying is not acknowledging. |
| 11 | «أقر المدعى عليه شفويا بالدين أمام شاهدين» | `debt_acknowledged` stated | The element says «كتابة أو بإقرار». |
| 12 | «لم يسلم المبيع في الموعد المتفق عليه» ، «لم تسلم البضاعة في التاريخ المحدد» (nothing says it came later) | `delivery_missing` stated; `delivery_late` notMentioned; `deadline_passed` stated | Not delivered, as far as the note goes. The note need not say whether a delivery came later: unless it says so (case 13), the thing was not delivered. |
| 13 | «سلم المبيع بعد شهرين من الموعد» | `delivery_late` stated; `delivery_missing` denied | The note says the delivery took place. |
| 14 | «ينكر المدعى عليه توصله بالإعذار» | `notice_served` disputed, `disputedBy: defendant`; slot `notice_served` = yes | The plaintiff relies on the notice; the contest is in the polarity, not in the slot. |
| 15 | «أعذر عن طريق محضر قضائي بتاريخ 12 مارس 2025» and, in `documents`, nothing | stated, date 2025-03-12; slot yes, means `bailiff` | Words in `facts` are enough. |
| 16 | `documents`: «محضر تبليغ إعذار»; `facts` silent | stated (rule 1); slot yes | A named document states its event. |
| 17 | `documents`: «محضر عدم المصالحة»; `facts`: «ينكر المدعى عليه حضوره جلسة الصلح» | `conciliation_failed` stated | The contest is on attendance, not on the record. |
| 18 | «حسب أقوال المدعي، سلمه المبلغ نقدا دون وصل» | `money_lent` stated | A fact reported from the client is asserted by the notes. «دون وصل» is about proof, not about the fact. |
| 19 | «يزعم المدعى عليه أن المبلغ كان هبة لا قرضا» | `money_lent` disputed | The handing over is admitted, but the element is «على سبيل القرض»: the proposition is contested. |
| 20 | «تزوج الطرفان سنة 2015 دون عقد مسجل» | `marriage` stated, date 2015 | The element is the marriage, not its registration. |
| 21 | «فصل المدعي شفويا دون قرار مكتوب» | `dismissal` stated | The act is asserted; the form is another matter. |
| 22 | «استقال المدعي تحت الضغط» | `dismissal` notMentioned (and the note is near the border of `abusive-dismissal`) | A resignation is not the element's act. Do not label what the law might later call a dismissal. |
| 23 | «دفع المدعى عليه الأجور إلى غاية جوان 2025 ثم توقف» | `wages_unpaid` stated | The proposition is that wages due were not paid; a period paid does not deny it. |
| 24 | «تقاضى المدعي جميع أجوره وإنما يطالب بمنحة المردودية» | `wages_unpaid` stated (the element says «الأجور أو المستحقات») | A bonus is a sum due. |
| 25 | A fact stated in one sentence and corrected later («بتاريخ 3 ماي، والصحيح 13 ماي») | the corrected value; evidence = the corrected words | The lawyer's last word stands. |
| 26 | The same event twice with different polarity and no correction («لم يوجه إعذار» … «بعد إعذاره») | writer: rewrite. Judge: `unclear` | No tie rule. |
| 27 | In `contract-rescission`, `contract-damages` or `sale-non-delivery` (their notice element has no «مطالبة»): «اقتصر الموكل على تذكير البائع هاتفيا بموعد التسليم»; and «لم يوجه إليه أي إعذار، واكتفى بتذكيره هاتفيا» | first: `notice_served` notMentioned, slot = unknown. Second: `notice_served` **denied**, slot = no | In these three types the element is a formal notice only. An informal demand alone says nothing of it. Only words that name the formal notice and say it was not sent deny it (rule 3), whatever demand the note also mentions. |
| 28 | «ذكّر الموكل المدين برسالة نصية بأن أجل الرد قد حل» ، «طالبه بالدفع عبر الهاتف» | `notice_served` stated where the element includes «مطالبة» (case 6); slot `notice_served` = unknown when nothing is said of a formal notice | A demand by word of mouth, by phone, by text message, or a reminder that payment or performance is due, is a «مطالبة». It is never a formal notice for the slot (section 6). |
| 29 | «لم يرسل الموكل إلى المدين سوى رسالة نصية يذكره فيها بحلول الأجل» (`loan-repayment`) | `notice_served` stated; slot `notice_served` = **unknown** | The restriction states what follows سوى (case 3), and a reminder is a «مطالبة» (case 28). The slot is `no` only when the note names the formal notice and says it was not sent (cases 6, 27); this sentence does not name it. The app then asks, which costs one question and states nothing the note did not. |
| 30 | «لا ينكر المدعى عليه أنه تسلم البضاعة كاملة» (`commercial-debt`) | `goods_delivered` stated | «لا ينكر المدعى عليه X» asserts X and reports no contest (rule 1). It never states `debt_acknowledged`: not denying is not acknowledging (case 10). |
| 31 | «بلغت البدلات المتأخرة حسب الموكل 240.000 دج، ويقر المستأجر بالتأخر لكنه يحصرها في 120.000 دج» (`unpaid-rent`) | `rent_unpaid` stated (case 5); the event's amount is `240000` with `amountDisputed: true`; in `values`, `240000` and `120000` both with role `rent_unpaid` and `amountDisputed: true` | The event takes the figure of the side that relies on the element; the other side's figure belongs to the same event and is labelled too, so the reader can show both. |
| 32 | «يعترف المقاول بأنه أوقف البناء، ويعزو ذلك إلى امتناع الموكل عن دفع القسط الثالث» (`contract-rescission`, the client sues) | `work_stopped` stated (note a); `payment_made` notMentioned | The plaintiff's payment element («دفع المدعي الثمن أو تسبيقا منه») is met by any part. A note that says only that one part was not paid neither asserts nor denies that any part was paid (section 10). It is stated when the note says something was paid (case 3), denied when it says nothing was (case 1). |
| 33 | `documents`: «نسخة من محضر عدم التصالح»; `facts`: «تغيب ممثل المستخدمة عن جلسة المصالحة» | `conciliation_failed` stated; slot `nonconciliation_record` = yes | A document is known by what it is, not by one spelling of its name: «محضر عدم الصلح»، «محضر عدم المصالحة»، «محضر عدم التصالح» name the same record (rule 1). A party's absence is not a contest of the record (case 17). |
| 34 | «لا تدين المؤسسة للعامل بأي أجر متأخر، وطلبه يقتصر على التعويض» (`unpaid-wages`) | `wages_unpaid` **denied** | Read the meaning against the whole element (rule 3), not the grammatical form. The proposition is that wages due were not paid; a sentence that says nothing remains due says it is false. Rule 1's negative propositions are those that say the element's own content («لم يتقاض أجوره»). |
| 35 | `facts`: «ينفي المشتري أن يكون قد وقع عقد البيع المحتج به»; `documents`: «عقد البيع» (`unpaid-sale-price`) | `contract_concluded` disputed, `disputedBy: defendant` | To deny one's signature on the contract is to contest that one concluded it. The named document does not override the contest (section 4). |
| 36 | «تعتبر المستخدمة أن تسريح العامل كان جزاء لتأخره المتكرر» | `dismissal` stated | An employer who gives a reason for ending the work relation admits the act (rule 2). Every name of the act is the act: فصل، تسريح، عزل، إنهاء علاقة العمل. |

## 5. The 75 elements

**How to read the tables.** 5.1 gives, for every event kind, the proposition's core and one worked example of each polarity. 5.2 lists the 75 elements by claim type, with the exact proposition of `claim-elements.json`, whether it takes a date (D) or an amount (A), and what differs from the kind's general row. The examples are illustrations, **not stock phrases**: a writer must not reuse them, and a judge must not expect them.

### 5.1 By event kind

| Kind | Stated | Denied | Disputed |
|---|---|---|---|
| `money_lent` | «أقرض المدعي المدعى عليه مبلغ 800.000 دج بتاريخ 4 جانفي 2024» | «لم يسلم المدعي أي مبلغ للمدعى عليه، وإنما كفل دينه لدى الغير» | «ينكر المدعى عليه أنه تسلم المبلغ» |
| `debt_acknowledged` | «حرر المدعى عليه اعترافا بدين مؤرخا في 2 فيفري 2024» | «لم يوقع المدعى عليه أي اعتراف بالدين» | «يطعن المدعى عليه في التوقيع المنسوب إليه على وثيقة الاعتراف بالدين» |
| `deadline_passed` | «حل أجل الرد في 30 جوان 2024 دون وفاء» | «لم يحل بعد أجل الرد المتفق عليه» | «يتمسك المدعى عليه بأن الأجل مدد باتفاق لاحق ولم يحل» |
| `payment_made` | «سدد المدعى عليه 150.000 دج في أوت 2024» | «لم يسدد المدعى عليه أي قسط» | «يدعي المدعى عليه أنه دفع نصف المبلغ نقدا، وينفي المدعي ذلك» |
| `debt_unpaid` | «ما زال المبلغ في ذمة المدعى عليه» | «رد المدعى عليه المبلغ كاملا، والنزاع ينحصر في التعويض» | «يدعي المدعى عليه أنه وفى بالدين كله» |
| `price_unpaid` | «لم يدفع المشتري باقي الثمن وقدره 2.400.000 دج» | «قبض البائع الثمن كاملا يوم العقد» | «يتمسك المدعى عليه بأنه سدد الثمن بموجب تحويل بنكي» |
| `rent_unpaid` | «تخلف المستأجر عن دفع بدلات الإيجار منذ مارس 2025» | «المستأجر منتظم في دفع بدل الإيجار، والنزاع حول انتهاء المدة» | «يدعي المستأجر أنه دفع البدلات نقدا دون وصولات» |
| `wages_unpaid` | «لم يتقاض العامل أجور ثلاثة أشهر» | «تقاضى العامل كل أجوره ومستحقاته» | «تدعي المستخدمة أنها حولت الأجور إلى حسابه البريدي» |
| `maintenance_unpaid` | «امتنع الزوج عن الإنفاق على زوجته وأبنائه منذ سنة» | «الزوج يدفع النفقة بانتظام» | «يدعي الزوج أنه يرسل النفقة شهريا، وتنفي الزوجة توصلها بها» |
| `notice_served` | «أعذر المدعى عليه بموجب محضر قضائي بتاريخ 12 مارس 2025» | «لم يوجه أي إعذار إلى المدعى عليه» | «ينكر المدعى عليه توصله بأي إعذار» |
| `contract_concluded` | «أبرم الطرفان عقد بيع بتاريخ 9 سبتمبر 2023» | «لم يبرم أي عقد بين الطرفين، واقتصر الأمر على مفاوضات» | «ينكر المدعى عليه وجود أي عقد بينه وبين المدعي» |
| `goods_delivered` | «سلم المدعي البضاعة بموجب وصل تسليم مؤرخ في 5 ماي 2025» | «لم يسلم المدعي البضاعة بعد» | «ينكر المدعى عليه استلام البضاعة» |
| `delivery_missing` | «لم يسلم البائع السيارة إلى اليوم» | «سلم البائع المبيع في موعده» | «يدعي البائع أنه وضع المبيع تحت تصرف المشتري» |
| `delivery_late` | «سلمت الأشغال بعد خمسة أشهر من الموعد المتفق عليه» | «تم التسليم في الأجل المتفق عليه» | «يدعي المقاول أن التسليم تم في الأجل بعد تمديده كتابة» |
| `work_stopped` | «توقف المقاول عن الأشغال منذ أكتوبر 2024» | «الأشغال متواصلة ولم تتوقف» | «يدعي المقاول أن التوقف مصدره صاحب المشروع الذي منعه من دخول الورشة» → see note a |
| `concealment` | «كتم البائع أن العقار مثقل برهن» | «لم يخف البائع شيئا عن المشتري، وإنما يتعلق الأمر بغلط» | «يدعي البائع أنه أعلم المشتري بالرهن قبل العقد» |
| `other` (contract-damages) | «أخل المدعى عليه بالتزامه بعدم المنافسة» | «نفذ المدعى عليه التزاماته الأخرى كلها» | «ينفي المدعى عليه أي إخلال بالتزاماته» |
| `harm_by_person` | «اعتدى المدعى عليه على المدعي بالضرب يوم 3 أفريل 2025» | «لم يصدر عن المدعى عليه أي فعل، والضرر سببه الغير» | «ينكر المدعى عليه أنه اعتدى على المدعي» |
| `criminal_conviction` | «أدين المدعى عليه بحكم جزائي صادر في 18 جوان 2025» | «لم يصدر أي حكم جزائي، وحفظت الشكوى» | «يتمسك المدعى عليه بأن الحكم الجزائي غير نهائي وأنه استأنفه» → see note b |
| `harm_by_thing` | «تسربت المياه من شقة المدعى عليه فأتلفت أثاث المدعي» | «لم يكن للشيء دخل في الضرر» | «ينفي المدعى عليه أن التسرب مصدره شقته» |
| `harm_by_animal` | «عض كلب المدعى عليه ابن المدعي» | «لم يكن الحيوان ملكا للمدعى عليه ولا في حراسته» → see note c | «ينكر المدعى عليه أن كلبه هو الذي عض الطفل» |
| `harm_by_building` | «انهار جدار ملك المدعى عليه على سيارة المدعي» | «لم ينهدم أي جزء من البناء» | «يدعي المدعى عليه أن الجدار المنهار ليس ملكه» |
| `lease_started` | «شغل المستأجر العين منذ أول جانفي 2022» | «لم يتسلم المستأجر العين قط» | «ينكر المدعى عليه أنه شغل المحل» |
| `occupation_without_title` | «يشغل المدعى عليه المسكن دون أي سند» | «يشغل المدعى عليه العقار بموجب عقد إيجار ساري» | «يتمسك المدعى عليه بأن له عقد إيجار شفويا مع المالك السابق» |
| `eviction_notice` | «وجه المؤجر تنبيها بالإخلاء بتاريخ 2 فيفري 2025» | «لم يوجه المؤجر أي تنبيه بالإخلاء» | «ينكر المستأجر توصله بالتنبيه بالإخلاء» |
| `lease_renewal_refused` | «رفض المؤجر تجديد عقد الإيجار» | «لم يرفض المؤجر التجديد، وإنما اشترط مراجعة البدل» | «يدعي المؤجر أنه لم يرفض التجديد وأن المستأجر هو الذي غادر» |
| `marriage` | «تزوج الطرفان بعقد مؤرخ في 14 أوت 2016» | «لم يبرم عقد زواج بين الطرفين» | «ينكر المدعى عليه قيام العلاقة الزوجية» |
| `divorce` | «صدر حكم بالطلاق بين الطرفين في 7 ماي 2023» | «لم يقع الطلاق بعد، والزوجية قائمة» | «يدعي الزوج وقوع طلاق عرفي تنكره الزوجة» |
| `abandonment` | «هجر الزوج بيت الزوجية منذ جانفي 2024» | «لم يغادر الزوج بيت الزوجية» | «يدعي الزوج أن الزوجة هي التي غادرت البيت» |
| `spouse_harm` | «تعرضت الزوجة للضرب من زوجها مرارا» | «لم يثبت أي إيذاء، والدعوى مؤسسة على عدم الإنفاق وحده» → see note d | «ينكر الزوج أنه ضرب زوجته» |
| `custody_held` | «يقيم الأبناء مع أمهم منذ الطلاق» | «لا يقيم الأبناء عند أي من الوالدين، بل عند الجدة» | «يدعي كل من الوالدين أن الأبناء يقيمون عنده» |
| `custody_judgment` | «أسندت الحضانة للأم بحكم صادر في 11 مارس 2022» | «لم يصدر أي حكم في الحضانة» | «يتمسك المدعى عليه بأن الحكم ألغي في الاستئناف» |
| `custodian_remarried` | «تزوجت الحاضنة بأجنبي عن المحضون في 2024» | «لم تتزوج الحاضنة بعد طلاقها» | «تنكر الحاضنة زواجها وتقول إنها مخطوبة فقط» |
| `custody_renounced` | «تنازلت الأم عن الحضانة كتابة» | «لم تتنازل الحاضنة عن الحضانة» | «تدعي الأم أن التنازل انتزع منها بالإكراه» → see note e |
| `custodian_unfit` | «أهملت الحاضنة تمدرس الطفل وعلاجه» | «الحاضنة قائمة بشؤون المحضون على أحسن وجه» | «تنفي الحاضنة أي إهمال وتدلي بشهادات مدرسية» |
| `custody_unclaimed` | «لم يطالب الأب بالحضانة مدة ثلاث سنوات دون عذر» | «طالب الأب بالحضانة فور علمه بزواج الحاضنة» | «يدعي الأب أنه كان يجهل سبب السقوط» → see note f |
| `custodian_abroad` | «استقرت الحاضنة بالمحضون في بلد أجنبي» | «لم تغادر الحاضنة التراب الوطني» | «تدعي الحاضنة أن سفرها مؤقت للعلاج» |
| `visitation_refused` | «تمنع الأم الأب من رؤية أبنائه منذ ستة أشهر» | «لم يمنع المدعي من الزيارة، وإنما يطلب توسيعها» | «تدعي الأم أن الأب هو الذي لا يحضر في مواعيد الزيارة» |
| `employment_started` | «التحق المدعي بمنصبه في 3 سبتمبر 2019» | «لم يباشر المدعي العمل قط لدى المدعى عليها» | «تنكر المدعى عليها وجود علاقة عمل وتقول إنه مقاول مستقل» |
| `dismissal` | «فصل المدعي من منصبه بقرار مؤرخ في 20 جانفي 2026» | «لم يفصل المدعي، وما زال في منصبه» | «تنكر المستخدمة أنها فصلته وتدعي أنه ترك منصبه» |
| `conciliation_failed` | «حرر مكتب المصالحة محضر عدم المصالحة في 15 فيفري 2026» | «لم يعرض النزاع على مكتب المصالحة» | «تدعي المستخدمة أن الصلح تم وأن المحضر محضر مصالحة» |

Notes to the table:

- **a.** `work_stopped` reads «توقف … عن الأشغال أو أخل بتنفيذها». A contractor who admits the stop and blames the client contests the cause, not the stop: **stated**. It is disputed only when he says the works did not stop («يدعي المقاول أن الأشغال لم تتوقف»). The cell above shows the trap on purpose.
- **b.** The element is «صدر حكم جزائي بإدانة». Contesting that the judgment is final does not contest that it was given: **stated**. Disputed: «ينكر المدعى عليه صدور أي حكم بإدانته».
- **c.** For the three "harm by" elements of `liability-for-things`, the proposition includes the custody («في حراسة المدعى عليه»). A note that says the thing was not in his custody denies the proposition.
- **d.** «لم يثبت» in a lawyer's note about his own client's case means the note does not rely on the fact; label **denied** only when the note says it did not happen («لم يتعرض لها بأي إيذاء»). «لم يثبت» alone: `notMentioned`. The cell above shows the trap. When the note asserts the fact and adds a remark on its proof («ولا يملك الموكل شاهدا على ذلك», «دون وصل» in case 18), the fact stays **stated**: a remark on proof changes no polarity (section 10).
- **e.** A renunciation said to be forced is still a renunciation: **stated**; the vice is a legal argument. Disputed: «تنكر الأم أنها تنازلت».
- **f.** The proposition includes «دون عذر». An excuse claimed by the other side contests the proposition: **disputed**. This is the one element where a reason is part of the proposition. "The other side" is the right-holder whose silence the proposition states: an excuse given by him or for him makes the element disputed, whoever writes it and whichever party he is. `disputedBy` is his side as the note shows it; when the note does not show it, give no `disputedBy`.

### 5.2 By claim type

`D` = takes a date; `A` = takes an amount. "Actor" is the usual one; label the actor the note gives.

| Claim type | Kind | The element's proposition (`claim-elements.json`) | D | A | Note |
|---|---|---|---|---|---|
| `loan-repayment` | `money_lent` | سلّم المدعي المدعى عليه مبلغا على سبيل القرض أو السلفة | D | A | case 19 |
| | `debt_acknowledged` | اعترف المدعى عليه بالدين كتابة أو بإقرار | D | A | cases 10, 11 |
| | `deadline_passed` | حلّ أجل الرد المتفق عليه | D | | the date is the due date |
| | `payment_made` | دفع المدعى عليه جزءا من المبلغ | D | A | cases 1 to 4 |
| | `debt_unpaid` | لم يرد المدعى عليه المبلغ أو باقيه | | A | amount = what is still owed |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه أو مطالبة بالرد | D | | cases 6, 7, 14 to 16 |
| `unpaid-sale-price` | `contract_concluded` | إبرام البيع بين الطرفين | D | A | amount = the price; a denied signature: case 35 |
| | `goods_delivered` | سلّم المدعي المبيع للمدعى عليه | D | | |
| | `payment_made` | دفع المدعى عليه جزءا من الثمن | D | A | |
| | `price_unpaid` | لم يدفع المدعى عليه الثمن أو باقيه | | A | |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه أو مطالبة بالدفع | D | | |
| `contract-rescission` | `contract_concluded` | إبرام العقد بين الطرفين | D | A | |
| | `payment_made` | دفع المدعي الثمن أو تسبيقا منه | D | A | **the plaintiff pays**; one part said unpaid: case 32 |
| | `delivery_missing` | لم يسلم المدعى عليه الشيء محل العقد | | | cases 12, 13 |
| | `work_stopped` | توقف المدعى عليه عن الأشغال أو أخل بتنفيذها | | | note a |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه بالتنفيذ | D | | no «مطالبة» here: an oral demand does not state it (case 27) |
| `sale-annulment-fraud` | `contract_concluded` | إبرام العقد بين الطرفين | D | A | |
| | `concealment` | كتم المدعى عليه واقعة عمدا أو استعمل حيلا دفعت المدعي إلى التعاقد | | | |
| | `payment_made` | دفع المدعي الثمن | D | A | the plaintiff pays |
| `contract-damages` | `contract_concluded` | إبرام العقد بين الطرفين | D | A | |
| | `delivery_late` | تأخر المدعى عليه في التسليم أو التنفيذ عن الموعد المتفق عليه | D | | date = the day it was delivered |
| | `delivery_missing` | لم يسلم المدعى عليه ما التزم بتسليمه | | | |
| | `work_stopped` | توقف المدعى عليه عن الأشغال أو أخل بتنفيذها | | | note a |
| | `other` | أخل المدعى عليه بالتزام تعاقدي آخر | | | only a breach that is none of the three above |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه بالتنفيذ | D | | no «مطالبة» (case 27) |
| `tort-damages` | `harm_by_person` | ارتكب المدعى عليه فعلا ضارا (اعتداء، إهمال، تشهير، إتلاف) | D | | |
| | `criminal_conviction` | صدر حكم جزائي بإدانة المدعى عليه | D | | note b |
| `liability-for-things` | `harm_by_thing` | أحدث شيء في حراسة المدعى عليه ضررا (تسرب مياه، آلة، سيارة…) | D | | note c |
| | `harm_by_animal` | أحدث حيوان في حراسة المدعى عليه ضررا | D | | note c |
| | `harm_by_building` | انهدم بناء أو جزء منه في حراسة المدعى عليه فأحدث ضررا | D | | note c |
| `unpaid-rent` | `contract_concluded` | إبرام عقد الإيجار وبدله | D | A | amount = the rent per period |
| | `lease_started` | بداية الانتفاع بالعين المؤجرة | D | | |
| | `rent_unpaid` | لم يدفع المدعى عليه بدلات الإيجار المستحقة | | A | amount = the arrears; two figures: case 31 |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه أو مطالبة بالدفع | D | | |
| `eviction-occupier-without-title` | `occupation_without_title` | يشغل المدعى عليه عقار المدعي دون عقد أو سند | D | | date = since when |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو مطالبة بالخروج | D | | |
| `commercial-debt` | `goods_delivered` | سلّم المدعي للمدعى عليه بضائع أو خدمات | D | A | amount = the invoiced value; «لا ينكر»: case 30 |
| | `payment_made` | دفع المدعى عليه جزءا من المبلغ | D | A | |
| | `price_unpaid` | لم يسدد المدعى عليه قيمة البضائع أو الفواتير أو باقيها | | A | |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه أو مطالبة بالدفع | D | | |
| `commercial-lease-eviction` | `contract_concluded` | إبرام عقد الإيجار التجاري | D | A | the date also fills slot `contract_date` |
| | `rent_unpaid` | لم يدفع المستأجر بدلات الإيجار | | A | **actor = the lessee, who may be the plaintiff or the defendant** |
| | `eviction_notice` | وجه المؤجر تنبيها بالإخلاء | D | | actor = the lessor |
| | `lease_renewal_refused` | رفض المؤجر تجديد الإيجار | | | actor = the lessor |
| `sale-non-delivery` | `contract_concluded` | إبرام البيع بين الطرفين | D | A | |
| | `payment_made` | دفع المدعي الثمن أو جزءا منه | D | A | the plaintiff pays |
| | `deadline_passed` | حلّ أجل التسليم المتفق عليه | D | | |
| | `delivery_missing` | لم يسلم المدعى عليه المبيع | | | |
| | `notice_served` | وُجّه إلى المدعى عليه إعذار أو تنبيه بالتسليم | D | | no «مطالبة» (case 27) |
| `divorce-by-husband` | `marriage` | انعقاد الزواج بين الطرفين | D | | case 20 |
| `tatliq` | `marriage` | انعقاد الزواج بين الطرفين | D | | |
| | `abandonment` | هجر المدعى عليه المدعية | | | |
| | `maintenance_unpaid` | امتنع المدعى عليه عن الإنفاق | | | |
| | `spouse_harm` | أضر المدعى عليه بالمدعية (ضرب أو إيذاء) | | | note d |
| `khul` | `marriage` | انعقاد الزواج بين الطرفين | D | | |
| `nafaqa` | `marriage` | انعقاد الزواج بين الطرفين | D | | |
| | `divorce` | وقع الطلاق بين الطرفين | D | | |
| | `maintenance_unpaid` | امتنع المدعى عليه عن الإنفاق | | | |
| `custody` | `divorce` | وقع الطلاق بين الطرفين | D | | |
| | `custody_held` | يقيم الأبناء حاليا عند أحد الوالدين | | | with a third person: denied |
| `custody-forfeiture` | `custody_judgment` | صدر حكم سابق أسند الحضانة للمدعى عليه | D | | |
| | `custodian_remarried` | تزوجت الحاضنة بغير قريب محرم للمحضون، أي بأجنبي عنه | D | | a marriage to a close relative of the child: denied; a marriage with nothing said of kinship: **stated** (the exception counts only when the note says it) |
| | `custody_renounced` | تنازل الحاضن عن الحضانة | D | | note e |
| | `custodian_unfit` | اختل لدى الحاضن شرط من شروط الحضانة (الرعاية، التعليم، الحماية، الصحة أو الخلق) | | | |
| | `custody_unclaimed` | لم يطلب صاحب الحق الحضانة مدة تزيد عن سنة دون عذر | | | note f |
| | `custodian_abroad` | استوطن الحاضن أو يريد الاستيطان في بلد أجنبي | | | an intention to settle abroad **states** it (the proposition says «أو يريد»); so does an intention to go and live, with the child, beside someone who resides abroad («تعتزم اللحاق بزوجها المقيم في الخارج رفقة الطفل») |
| `visitation` | `divorce` | وقع الطلاق بين الطرفين | D | | |
| | `custody_judgment` | صدر حكم سابق حدد حق الزيارة | D | | |
| | `visitation_refused` | منع المدعى عليه المدعي من رؤية الأبناء | | | |
| `abusive-dismissal` | `employment_started` | التحق المدعي بالعمل لدى المدعى عليه | D | | |
| | `dismissal` | سرّح المدعى عليه المدعي أو فصله من العمل | D | | rule 2; cases 21, 22, 36 |
| | `conciliation_failed` | حرر محضر عدم المصالحة | D | | date = the date of the record, **not** of its delivery (section 6); other names of the record: case 33 |
| `unpaid-wages` | `employment_started` | التحق المدعي بالعمل لدى المدعى عليه | D | | |
| | `wages_unpaid` | لم يدفع المدعى عليه الأجور أو المستحقات | | A | cases 23, 24, 34 |
| | `conciliation_failed` | حرر محضر عدم المصالحة | D | | |

(75 rows. A test in the notes project counts them against `claim-elements.json`, so the table cannot fall behind the app.)

## 6. Slots

A slot is a question the app will ask the lawyer ([`../slots.json`](../slots.json)). The reader proposes an answer when the note gives one. **A slot is not a copy of an event:** its question may be narrower. Label each slot with `readFromNote: true`, for every note, by these rules. `unknown` carries no quote.

| Slot | `yes` / value when | `no` when | `unknown` when |
|---|---|---|---|
| `notice_served` | the note states a **formal** notice: إعذار، إنذار، تنبيه رسمي، محضر تبليغ, a notice by bailiff or by registered letter; also when the notice is stated and the defendant contests receiving it (case 14) | the note names a notice, or the formal notice, and says it was not sent (cases 6, 27) | nothing on a notice; an intended notice; only an informal demand (by word of mouth, phone, text message, a reminder) with no word on a formal one, also when worded with «اكتفى» or a restriction (cases 27 to 29) |
| `notice_date` | the date the note gives for that formal notice | — | no date, or a relative one («منذ شهرين») |
| `notice_means` | `bailiff` (محضر قضائي), `registered_letter` (رسالة مضمونة / موصى عليها), `other` (any other means the note names) | — | the means is not said |
| `nonconciliation_record` | the note states the record, names it in `documents`, or says the conciliation before the office failed | the note says the dispute was not put to the conciliation office, or that no record was drawn up | nothing on conciliation; an attempt whose outcome is not said |
| `nonconciliation_record_delivered_on` | the date the note gives for the **delivery** of the record («سلم بتاريخ», «استلم المحضر في», «تسلم نسخة منه يوم»), also when given as the same day as another date of the note (section 8) | — | the note gives only the date of the record or of the hearing («محضر مؤرخ في», «جلسة») → that date goes to the event, and this slot stays unknown |
| `contract_date` | the date of the contract named by the claim type's `contract_concluded` element (the sale, the lease, the contract of section 5.2) | — | not given, or a relative one; and always for a claim type without a `contract_concluded` element, even when the note dates another contract (a loan, a marriage, an employment: those dates go to their own events) |
| `public_body_party` | a party is named as one of the bodies in the list frozen with the label scheme (from the rule module on cases that leave the ordinary courts: the State, a ministry, a wilaya, a commune, a public body of an administrative character) | every party is a private person or a company, in the note's own words | a party's nature is not clear from the note (a "مؤسسة" with nothing more) |
| `property_registered` | the note says the property or its title is published or has a land book (مشهر بالمحافظة العقارية، دفتر عقاري، عقد رسمي مشهر) | the note says it is not (غير مشهر، عقد عرفي غير مشهر) | nothing on publication; or no property in the case |

The trap that matters most: **the date of the non-conciliation record is not the date of its delivery.** The six-month period of CPCA art. 504 runs from delivery. A reader that copies the record's date into the delivery slot would make the app compute a wrong last day. When the note has one date and does not say which it is («محضر عدم المصالحة بتاريخ 15 فيفري 2026»), it is the record's date.

**`notice_date` and `notice_means` follow `notice_served`.** They are `unknown` whenever `notice_served` is not `yes`, even when the note gives the date or the means of an intended notice or of an informal demand («ينوي الموكل إعذاره بواسطة محضر قضائي»: `notice_means` unknown). The app asks them only after a formal notice (`askedWhen` in `slots.json`).

Slots with `readFromNote: false` are not labelled; the app asks the lawyer. The label-scheme freeze of step 3 fixes the final list (a slot is read from the note only if lawyers usually write the fact, and if a deadline, a prior step or an exit depends on it). A slot that becomes `readFromNote: true` at the freeze gets its row in the table above, and its items in the guide test, before the scheme is frozen.

## 7. Requests

Label each request kind of `FactVocabulary.Requests` that the note **asks the court for**, in `requests` or in `facts` («يلتمس المدعي …», «نطلب …», «المطلوب: …»). Quote the words. An amount asked goes with the request. Do not label what the lawyer could ask and did not. `provisional_execution` only when asked in words. A request that fits no kind: `other`.

## 8. Dates and amounts

Label **every** date and amount in the note, also those that belong to no element (a date of birth, a salary mentioned in passing): the reader must find them all and link the ones that matter.

- **`quote`:** the value as written, with its unit and nothing else («12 مارس 2025», «٥٠٠ ألف دج», «1.200.000,00 دج»).
- **`normalized`, dates:** `yyyy-mm-dd`; `yyyy-mm` when the day is missing; `yyyy` for a lone year. Day before month, always («03/04/2025» is 3 April). Arabic-Indic and Western digits alike. Two-digit years: do not guess; a writer must not write them, a judge marks `unclear`. Hijri dates: not normalised (leave `normalized` out; `calendar: "hijri"`). Relative dates («منذ ثلاثة أشهر», «الشهر الماضي», «في اليوم الموالي»): leave `normalized` out; `relative: true`. «منذ» with a length of time points to a moment, so it is a relative date and is labelled; a length of time alone is a period (below) and is not. Where the guide test asks for the value of either, the answer is `none`.
- **A date given as the same day as another date of the note** («في اليوم نفسه», «في التاريخ ذاته») takes that date. Nothing new is added to `values`: the date written once carries both roles, and the evidence of the event or slot quotes both the date and the words that refer to it («حرر المحضر في 10 فيفري 2026 وسلمت نسخة منه للعامل في اليوم نفسه»: the delivery slot is `2026-02-10`). Any other reference («في اليوم الموالي», «بعد أسبوع») is a relative date.
- **`normalized`, amounts:** digits only, in **dinars**, no separators. «500.000 دج» → `500000`; «مليون ونصف دينار» → `1500000`; «ونصف» after a unit word (ألف، مليون، مليار) is half of that unit, whether or not the unit is repeated («مليون ونصف مليون دينار» → `1500000`; «ألف ونصف دينار» → `1500`); «50 مليون سنتيم» → `500000` (100 centimes = 1 dinar: divide by 100); «3 ملايير سنتيم» → `30000000`. Decimals are dropped only when zero («1.200.000,00» → `1200000`); a real fraction keeps two decimals, written with a point (`1250.50`). An amount in words and digits («خمسمائة ألف (500.000) دينار») is one amount, quoted whole. A foreign currency: leave `normalized` out; `currency` as written. When a party contests the amount and gives its own figure, both figures are labelled with the event's role and `amountDisputed: true` (case 31).
- **`roles`:** the event kinds or request kinds the value belongs to (`payment_made`, `damages` ...), and the slot ids it answers (`notice_date`, `nonconciliation_record_delivered_on`, `contract_date`); an empty list when it belongs to none. One written value may carry several roles (the contract's date is `contract_concluded` and `contract_date`).
- **One entry per place a value is written.** A value the note writes twice (the sum owed in `facts`, again in `requests`) is labelled twice, each entry with its own quote and the roles of that place.
- A period («مدة ستة أشهر», «ثلاث سنوات») is not a date and is not labelled.
- A number that is not money (an invoice number, an area, an article number, a count of children) is not an amount.

## 9. How each role uses this guide

- **Writer (training and development notes).** You receive a scenario with its facts and a style card. Write the note as a lawyer would; then give, for each fact of the scenario, the exact words of your note that carry it. If you cannot quote it, your note does not say it: fix the note, do not stretch the quote. Never add a date or an amount the scenario does not hold. Never copy an example of this guide.
- **Judge.** You receive the note only. Label it from the text, quote the words, and mark `unclear` where two rules seem to fit. You do not see the writer's labels or the scenario.
- **The script** compares writer and judge field by field (claim types as sets; polarity per element; slot values; normalised values and their roles; request kinds as sets). Quotes need not be equal, but each must be found in the note, and two quotes for the same field must overlap or lie in the same sentence.
- **Disagreement** (owner's decision, 12 November 2026). In **training notes only**: the disputed field is blanked and the note kept, unless the two sides differ on the claim type or on more than about a fifth of the note's fields (counted over the fields that at least one side labelled with something other than `notMentioned` or `unknown`), in which case the note is dropped. A blanked field carries no label: it is never read as `notMentioned` or `unknown`. In **every test set** (development, challenge, mirror, hidden v3): any disagreement drops the whole note. Blanked fields are counted per slot and per polarity, and targeted notes refill every cell that blanking and dropping emptied ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 5).

## 10. What is never labelled

- What the law says the facts amount to ("abusive", "justified", "void", "time-barred").
- What is likely but not written. No inference of more than the direct opposite (case 13).
- The lawyer's opinion of the evidence («الإثبات ضعيف»): it changes no polarity.
- Anything from outside the note: the scenario, the style card, another note.
