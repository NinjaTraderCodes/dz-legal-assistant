# The guide test: first run (13 November 2026)

**What was tested.** The labelling guide as it stood in the handbook's first version, on 13 November 2026. Each tester got the whole guide and the 45 items of `notes/guide-test.json` with only `id`, `claim`, `ask` and `text`: no expected answer. The prompt was [../prompts/notes-guide-test.md](../prompts/notes-guide-test.md).

**The testers.**
- Claude Sonnet 5.5, a fresh agent.
- Codex CLI 0.167.0, in one call. The event-log check accepted it: one agent message, no command, no file opened.

## Result

| | Sonnet 5.5 | Codex |
|---|---:|---:|
| Expected answer given | 45 of 45 | 45 of 45 |
| Marked "unsure" | 6 (items 10, 19, 23, 33, 36, 45) | 0 |

The two testers agreed with each other on all 45 items.

**What this shows, and what it does not.** On these 45 items the guide decides, and two families read it the same way. But the items were written by the guide's author, who knew the answers. Sonnet marked six items unsure and wrote eleven remarks on places where the guide left it to choose; it chose the expected answer each time, but a judge in step 3 might choose the other way. So a perfect score here does not prove that the guide decides every sentence.

## What changed in the guide

Each remark became one rule or one hard case. None changes the answer of any of the 45 items, the four polarity rules of 6 November 2026, or the propositions of table 5.2.

| Item(s) | Where the guide left a choice | What the guide now says | Where |
|---|---|---|---|
| 10 | An oral demand with «اكتفى», and no word of a formal notice, in a claim type whose notice element has no «مطالبة»: denied or not mentioned? | Not mentioned (slot unknown). Only words that name the formal notice and say it was not sent deny it. | 4.2, case 27; 5.2, notes of the three notice rows |
| 19 | Does a variant name of a document («محضر عدم الصلح») count as the named document? | A document is known by what it is; the variants name the same record. A party's absence is not a contest of the record. | 4.2, case 33; 5.2, note of `conciliation_failed` |
| 23 | A sentence negative in form that says nothing is owed: stated by rule 1, or denied by rule 3? | Read the meaning against the whole element: it denies `wages_unpaid`. | 4.2, case 34 |
| 33 | Note f's "the other side" for `custody_unclaimed`, when the excuse comes from the right-holder himself. | "The other side" is the right-holder whose silence the element states; his excuse makes it disputed; `disputedBy` is his side when the note shows it. | 5.1, note f |
| 36 | A contest of one's signature on the contract itself, with the contract named in `documents`. | It contests the conclusion: disputed; the named document does not override it. | 4.2, case 35; 5.2, note of `unpaid-sale-price/contract_concluded` |
| 45 | «منذ ثلاثة أشهر»: a relative date or a period? And what is the answer when a value is not normalised? | «منذ» with a length of time is a relative date (labelled, no normalised value); a length of time alone is a period (not labelled). Either is answered `none` in the guide test. `normalized` is left out, never `null`. | 8, dates |
| 42 | «مليون ونصف مليون دينار», beside the guide's «مليون ونصف دينار». | «ونصف» after a unit word is half of that unit, whether or not the unit is repeated. | 8, amounts |
| 26 | «لم يتم التسليم في الأجل المتفق عليه» does not say whether delivery came later. | Unless the note says it came later, the thing was not delivered. | 4.2, case 12 (wording extended; label unchanged) |
| 16 | Is an employer's reason for "ending the work relation" an admission of the act? | Yes: a reason given admits the act; every name of the act is the act. | 4.2, case 36 |
| 31 | Note d and section 10 (an opinion on proof) were not linked. | A fact asserted with a remark on its proof stays stated. | 5.1, note d |
| 8, 9 | Case 6 speaks of an oral demand, not of a demand by phone. | A demand by word of mouth, phone or text message, or a reminder, is a «مطالبة»; never a formal notice for the slot. | 4.2, case 28 |

**Changed at the same time, from a cold reading of the handbook** (reader 4, check 3a: ten edge sentences labelled from the guide alone, of which seven had two answers):

| Sentence | What the guide now says | Where |
|---|---|---|
| «لا ينكر المدعى عليه أنه تسلم البضاعة كاملة» | «لا ينكر X» states X; it never states `debt_acknowledged`. | 4.2, case 30 |
| A restriction that names only an informal demand («لم يوجه … سوى رسالة نصية») | Event stated where the element includes «مطالبة»; slot `notice_served` unknown. | 4.2, case 29; 6, `notice_served` row |
| A contest of an amount with two figures | The event takes the figure of the side that relies on it; both figures are labelled with the same role and `amountDisputed`. | 4.2, case 31; 8, amounts |
| An instalment said to be unpaid, given as the cause of a stop | `payment_made` not mentioned when the element is met by any part. | 4.2, case 32 |
| A remarriage with nothing said of kinship; an intention to join someone abroad with the children | `custodian_remarried` stated; `custodian_abroad` stated. | 5.2, the two rows' notes |
| The means of an intended notice | `notice_date` and `notice_means` are unknown whenever `notice_served` is not yes. | 6, paragraph after the table |
| «في اليوم نفسه» after the record's date | The same-day date takes the date it refers to. | 8, dates; 6, delivery row |

The other three sentences already had one answer (case 7; rule 2; the section 4 sentence on a named document against an explicit contest).

Also made precise: `contract_date` (only the contract of the claim type's `contract_concluded` element); one value entry per place a value is written, with a list of `roles`; a slot that becomes `readFromNote: true` at the freeze gets its row and its guide-test items first.

## What remains

**The test must be run again on the changed guide before the scheme is frozen** ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 1): the 45 items again, each of which must still give its expected answer, and new items on the changed sections, written by an agent that has not seen `guide-test.json`. A rerun on the same 45 items alone would pass by construction and prove nothing about the new rules.
