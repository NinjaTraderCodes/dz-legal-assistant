# The owner's check of Gemma's dropped notes (16 November 2026)

**What was asked (the owner, 16 November):** "take 20 of the notes Gemma dropped for too many blanks and, in one Opus run, judge each disputed field against the note: who is right, Gemma or the writer's label? Report it per field."

**How.** `exec\gemma_disputes.py` drew 20 of the trial's notes dropped for too many blanks (seed 7). For each one it rebuilt the sheet Gemma had answered, and listed every disputed field: the question, the writer's label with its quotes, and Gemma's answer. One fresh Opus 5.5 run judged them with the sorting prompt (`prompts/notes-sort-dropped.md`, the checker being Gemma), with no tool except reading its prompt. The answer was checked against `schemas/sort-dropped.schema.json`: 89 fields in, 89 answered.

## Per field

| Field type | Disputed fields | Writer's label right | Gemma right | Both readings hold | Neither |
|---|---:|---:|---:|---:|---:|
| element polarity | 42 | 32 (76%) | 5 (12%) | 5 | 0 |
| slot | 18 | 18 (100%) | 0 (0%) | 0 | 0 |
| what a value is for | 18 | 18 (100%) | 0 (0%) | 0 | 0 |
| request | 9 | 9 (100%) | 0 (0%) | 0 | 0 |
| plaintiff's role | 2 | 2 (100%) | 0 (0%) | 0 | 0 |
| **all** | **89** | **79 (89%)** | **5 (6%)** | **5** | **0** |

Slots in detail:

| Slot | Disputed | Writer right | Gemma right |
|---|---:|---:|---:|
| notice_means | 3 | 3 | 0 |
| notice_served | 1 | 1 | 0 |
| property_registered | 9 | 9 | 0 |
| public_body_party | 5 | 5 | 0 |

Causes as Opus gave them: Gemma (the checker) 74, the guide 9, the writer 5, the note 1.

## What it means, by the owner's rule

**Gemma is mostly wrong.** Opus found Gemma right on 5 of 89 disputed fields (6%). On slots, value roles, requests and roles it was right on none (0 of 47). Its best type is element polarity, at 5 of 42. There is no field type on which it agrees reliably, so **Gemma is dropped as a filter for every field**. The owner's route of 16 November had already stopped it checking every note. This result also rules it out for the flagged notes: those notes are dropped, and no model judges them. The code checks, the reader's own flags and the noise measurement carry the filtering.

Gemma's typical mistakes:
- it answers `no` instead of `unknown` for `property_registered` (9 times) and for `public_body_party` (5);
- it answers `stated` for an element whose receipt or title is contested (case 14), instead of `disputed`;
- it gives a date or an amount to the wrong role where the note writes it twice.

**The writer's labels are not often wrong.** Opus found the writer wrong on 5 of the 89 fields, in 4 of the 20 notes, all of them element polarity:
- case 5, `events/contract-damages/delivery_missing/polarity`: «البضاعة دون تسليم» is immediately contested by «تدعي إنجاز العمل وتسليمه»: the defendant says it delivered. That contest is of the same fact, which rule 2 labels disputed.
- case 9, `events/commercial-debt/price_unpaid/polarity`: «رفضت بيلرون تقديم أي قسط، ولم تؤد أي دفعة جزئية قط» states non-payment. Under case 1 and rule 3, 'paid nothing' denies payment_made and states price_unpaid, so the scenario's notMentioned cannot hold for this sentence.
- case 14, `events/contract-damages/delivery_late/polarity`: The element's proposition includes «عن الموعد المتفق عليه», and the defendant says «الاتفاق لم يعين أصلا يوما» and «ينكر ... حلول الموعد». Contesting part of the proposition makes it disputed, as with «على سبيل القرض» (case 19) and custody (note c).
- case 18, `events/contract-rescission/delivery_missing/polarity`: «تسليم الأشغال في الموعد المتفق عليه دون تأخر» says the delivery took place, which denies non-delivery under case 13 and the 5.1 example «سلم البائع المبيع في موعده». It is not notMentioned.
- case 18, `events/contract-damages/delivery_missing/polarity`: «تسليم الأشغال في الموعد المتفق عليه» says delivery took place, so delivery_missing is denied (case 13).

At this rate the owner's rule sends no change to the writing prompt or the ingest checks. The kinds are kept for the noise measurement, which measures label noise on kept notes directly. If that noise rises, these are the first kinds to put into the writer's prompt as warnings in general terms.

**Defects of the guide (9 fields, three gaps):**
- No rule says whether words that only presuppose an element (a request to return a sum, the 'price' of goods or invoices) state it. Proposed rule: an element is stated only by words that assert it, and a request or a word that merely presupposes it leaves it notMentioned.
- Section 7 does not define the request kinds, and the sheet's wording narrows 'payment' to a loan. Proposed rule: 'payment' is any request that the defendant be ordered to pay a sum owed (loan, price, rent, invoices), other than wages, restitution and damages.
- No rule covers a party who admits the event for a part and denies it for the rest (part of the premises, part of the goods). Proposed rule: such a contest leaves the event stated and is recorded as a contest of extent, like a contest of amount (case 5).
- Section 7 defines 'other' as a request that fits no kind, but the request kinds' wording on the sheet glosses 'other' as performance of an obligation. Proposed rule: the 'other' kind is worded as 'any other request that fits none of the listed kinds', with performance of an obligation given its own kind or listed as one example only.

The request-kind gap is a defect of Gemma's sheet, not of the guide. The sheet showed the catalogue's Arabic example as the option's text: «إلزام المدعى عليه بدفع مبلغ القرض» for `payment`, and «تنفيذ الالتزام أو تمكين المدعي من حقه» for `other`. The sheet is no longer used. The writer's and the relabeller's prompts give the English gloss ("payment of a sum owed", "any other request") and the Arabic only as an example.

The other two are real gaps in the guide: words that only presuppose an element, and a party who admits part of an event and denies the rest. The handbook's decision table fixes them in the guide, bumps its version, and reruns the guide test on the items they touch. That is listed for after the first score, in the pilot report.

Files (outside the repository): `C:\Users\TheShadow\dz-core-research\runs\notes\gemma-disputes\` (`sort.prompt.md`, `key.json`, `reply.json`); this page by `exec\gemma_check_report.py`.
