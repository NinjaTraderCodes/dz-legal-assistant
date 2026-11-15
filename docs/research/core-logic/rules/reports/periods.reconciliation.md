# Reconciliation of `periods` (module)

Record: `rules/reconciled/periods.json` (stage `reconciled`, `verified: false`). Reconciler: claude-opus-5-5, 2026-11-15.

Tool runs on the reconciled record (rules.exe 16c46f4): `check` OK (0 failed); `examples` 36 of 36 pass; `examples --boundary` 18 cases, 7 pass, 11 report differences (all explained below: 10 are matching effects, 1 is a disputed case).

Counts over the 103 differences: **settled by the text 72, both right 26, not settled 5, needing a text 0** (items and slots x1 to x36: 27 / 6 / 3 / 0; cases x37 to x103: 45 / 20 / 2 / 0).

## Id map (reconciled ← A / B)

- Statements keep A's ids (b1 to b44, see `periods.idmap.json` for B's); B's b36 (civil-code 311 ¶1) becomes **b45**.
- d1 ← A d1 / B d3 · d2 ← A d2 / B d4 · d3 ← A d3 / B d2 · d4 ← A d4 / B d7 (wages) · d5 ← A d5 / B d1 · **d6** ← B d5 · **d7** ← B d6 · **d8** ← B d7 (periodic rights other than wages).
- doc1 ← A doc1 · doc2 ← A doc2 + B doc2 · doc3 ← B doc1. B's boundaries n1 to n3 and A's elements e1 to e3 are dropped (x25 to x27, x32, x33, x36).
- Examples: A ex1 to ex16 keep their ids; B ex1 to ex14 become ex17 to ex30; B ex15 (boundaries only) is dropped; ex31 to ex36 are the reconciler's.

## The differences, one line each

### Items and slots (x1 to x36)

- **x1** civil-code 3 ¶1 (A only): **settled**, A. ¶1 «تحسب الآجال بالتقويم الميلادي» makes month and year periods run by the calendar, which is what the counting of every limitation needs; kept as b27.
- **x2** civil-code 311 ¶1 (B only): **both right**. The paragraph exists and is the article 313 ¶1 names; neither side makes a deadline of it (taxes owed to the State are not a claim of this app, A k30). Kept as b45, statement only.
- **x3** b7 interpretive (A yes, B no): **settled**, B. 312 ¶1 and ¶3 say in words that sums owed to workers for their work prescribe in one year; the conflict with 309 ¶1 is a matter of the deadline d3, which stays interpretive.
- **x4** b8 role (A proof, B procedure): **settled**, A. 312 ¶4 says who must swear and to what (payment): a rule of proof.
- **x5** b9 interpretive (A yes, B no): **not settled**. 313 ¶1 names 309 and 311, but its words («يتم فيه الدائنون تقديم خدماتهم») do not fit taxes, which 311 ¶1 starts on its own days, and fit the professionals and one-year creditors. Kept interpretive, both readings in o2, protective reading: count from the earlier of the due date and the completion of the work.
- **x6** b23 role (A procedure, B limit): **settled**, A. 321 ¶1 says who may ask the court to apply limitation.
- **x7** civil-code 323 ¶1 (A only): **settled**, A. ¶1 puts the proof of release on the debtor, and 320 ¶1 makes limitation an extinction; kept as b42 (k35).
- **x8** cpca 226 ¶1 (A only): **not settled**. The paragraph exists; whether «عدم الاحتجاج بأي إجراء» removes the interruption of 317 ¶1 is not said. Kept interpretive with o8 (protective: do not rely on a lapsed claim).
- **x9, x10** cpca 322 ¶1 and ¶2 (A only): **settled**, A. ¶1 covers «كل الآجال المقررة في هذا القانون من أجل ممارسة حق», which includes the period of 504 ¶1; ¶2 says how the forfeiture is lifted. A's o6 is dropped as settled.
- **x11** labour-disputes 30 ¶2 (A only): **settled**, A. ¶2 hands the copy of the record to the plaintiff at the meeting, which dates the start of d5 in that case; kept in doc1 (not in d5's basis).
- **x12** labour-disputes 37 ¶1 (A only): **settled**, A. ¶1 requires the copy of the record with the petition; kept in doc1.
- **x13** deadline of 310 ¶1 (B only): **settled**, B. ¶1 «تتقادم بسنتين حقوق ...» sets two years; 315 ¶1 starts it on the due date, since 313 ¶1 does not name 310 on its words. Kept as d6.
- **x14** deadline of 312 ¶2 (B only): **settled**, B. ¶1 and ¶2 set one year for traders' supplies and hotel and restaurant bills. Kept as d7.
- **x15** d1 `when` (A excludes wage claims): **settled**, A. 308 ¶1 «فيما عدا الحالات التى ورد فيها نص خاص ... وفيما عدا الاستثناءات الآتية» displaces the fifteen years where a special period applies, as 309 and 312 do for wages.
- **x16** d1 `startsFrom` (obligation_due_date / debt_due_date): **both right**: the same fact (315 ¶1), one id `debt_due_date`, A's event `debt_unpaid`.
- **x17, x19, x20, x23** counting basis (A cites civil-code 3 and 314, B 314 only): **settled**, A. Method and extension agree; civil-code 3 ¶1 is the text for calendar months and years.
- **x18** d2 `raisedBy` (A unclear, B pleaOnly): **not settled**. 321 ¶1 (plea only) governs limitation, and 133 ¶1 says «تسقط» outside the limitation section; whether it is a limitation is the open reading o3. Kept `unclear`, both readings.
- **x21** d4 `when` (A wages, B any periodic right): **both right**. 309 ¶1 names «المرتبات والاجور» and also «كل حق دورى متجدد» such as rent and pensions. Merged as d4 (wages, A) and d8 (other periodic rights, B, slot question narrowed to "other than wages").
- **x22** d4 `startsFrom` (wages_due_from / debt_due_date): **both right**. Wages run from the existing slot wages_due_from (d4); other periodic rights from debt_due_date (d8).
- **x24** d5 `when` (A adds the lifted forfeiture): **settled**, A. 322 ¶1 excepts force majeure and events affecting the courts, and ¶2 lifts the forfeiture by the president's order; unknown is not asked, so the deadline still applies.
- **x25, x26, x27** A's elements e1 to e3: **settled**, B (none). 504 ¶1 makes the six months a deadline (d5), not a fact to prove; 321 ¶1 leaves limitation to the debtor's plea, so e3 is not the plaintiff's element; the interruption of e2 answers that plea and is carried by doc2, b18, b19 and b42. A module's elements cannot be imported (the schema's import blocks).
- **x28** claim_is_periodic_renewable_right (B): **settled**, B. 309 ¶1 (d8).
- **x29** claim_is_professional_fees (B): **settled**, B. 310 ¶1 (d6).
- **x30** claim_is_trader_supply_or_hotel (B): **settled**, B. 312 ¶2 (d7).
- **x31, x35** debt_due_date / obligation_due_date: **both right**, same fact (see slots).
- **x32** defendant_resides_abroad (B's n1): **settled**, A. 404 ¶1 extends only opposition, appeal, review and cassation; no deadline of this module moves, so the slot only added a question to every case (k23, k43).
- **x33** employer_in_bankruptcy_or_settlement (B's n2): **settled**, A. 317 ¶1 makes a filing in the debtor's bankruptcy interrupt limitation for every debtor; it is in b18 and doc2, and the employer's bankruptcy changes no period.
- **x34** forfeiture_lifted_by_order (A): **settled**, A. 322 ¶1 and ¶2.
- **x36** worker_is_public_servant (B's n3): **settled**, A. 309 ¶1 names «المرتبات», so the five years reach a public servant's pay on the words, and d3 warns on the one-year reading anyway; labour-disputes 19 ¶3 keeps public servants out of conciliation offices, so d5 never runs for them. Who sues where is for the exits module (labour-relations 3 ¶1).

### Cases: A's examples on B (x37 to x52), B's on A (x53 to x67)

- **x37** aOnB ex1: **settled**. Kinds-of-debt questions stay (310 ¶1, 312 ¶2, 309 ¶1); boundary questions go (404 ¶1, 317 ¶1). ex1 now answers the three kinds "no" and passes.
- **x38, x39** aOnB ex2, ex3: **both right** (x16, x21): same due-date fact; d4 is wages-only.
- **x40, x41, x43** aOnB ex4, ex5, ex7: **both right** (x21).
- **x42** aOnB ex6: **settled** (x24, cpca 322 ¶2): a lifted forfeiture gives none.
- **x44 to x48** aOnB ex8 to ex12: **settled** (x32, x33, x36, 404 ¶1 and 317 ¶1; kinds of debt not asked in wage or tort claims). ex9 now lists the three kinds-of-debt questions.
- **x49** aOnB ex13: **both right** (x31): it asks debt_due_date.
- **x50, x51** aOnB ex14, ex15: **both right** (x21): d4 is the wage five-year rule.
- **x52** aOnB ex16: **settled** (x15, x32, x33, x36).
- **x53** bOnA ex1: **settled** (x15, 308 ¶1; n1 dropped under 404 ¶1).
- **x54, x55, x57** bOnA ex2, ex3, ex5: **both right** (x21).
- **x56** bOnA ex4: **both right** (x31).
- **x58, x59, x60** bOnA ex6 to ex8: **settled** (x15, 308 ¶1): d1 is silent in wage claims.
- **x61, x62, x63** bOnA ex9 to ex11: **both right** (x21).
- **x64** bOnA ex12: **settled** (x13, 310 ¶1): d6.
- **x65** bOnA ex13: **both right** (x21): rent arrears now warn through d8.
- **x66** bOnA ex14: **settled** (x14, 312 ¶2): d7.
- **x67** bOnA ex15: **settled** (x32, x33, x36): the example is dropped with the boundaries.

### Boundary-case runs (x68 to x103)

- **x68, x76** boundaryA bc2, bc18: **settled**. 314 ¶1 and civil-code 3 ¶1 give 2026-06-15 / 2026-06-16 for the one-year reading (d3 passes); the other matched deadlines govern other periods (matching effect).
- **x69, x78, x90** bc5 (A, B, AB): **settled**. civil-code 3 ¶1 counts by the calendar: five years from 2021-02-28 end on 2026-02-28 (d4 passes). The writer's doubt (calendar years or 1825 days) is answered by that paragraph; the outcome "unsettled" still stands because of o1.
- **x70, x81, x93** bc8 (A, B, AB): **settled** by 309 ¶1 with 314 ¶1 (last day 2026-06-15); the case is disputed, see below.
- **x71, x82, x94** bc9: **settled**. cpca 405 ¶1 counts «الآجال المنصوص عليها في هذا القانون»: the civil one-year period is counted by 314 ¶1 alone and is not extended; last day 2026-06-12, as the case says.
- **x72, x83, x95** bc10: **settled**. 405 ¶3 now sits in d5's counting basis (both had left it out), so the case matches d5; 405 ¶3 «بمفهوم هذا القانون» does not reach the civil period, last day 2026-07-05 as the case says.
- **x73, x74** boundaryA bc11, bc12: **not settled**. 317 ¶1 names «التنبيه» without saying whether a formal notice (by bailiff or letter) is one; o8, protective reading: do not rely on it. d3 warns, last day 2026-03-01.
- **x75, x84, x101** bc16: **settled**. 315 ¶1 starts the period on the due date, which the record asks for; it does not read contract_date (see below).
- **x77, x85, x87, x103** bc2, bc18 (B, AB): **settled** (x15; d6 to d8 silent in wage claims).
- **x79, x80, x91, x92** bc6, bc7 (B, AB): **both right** (x21): d4 gives the wage five-year last day (2025-03-01, 2026-06-15).
- **x86, x88, x89, x96, x97, x98, x99, x100, x102** bc1, bc3, bc4, bc11, bc12, bc13, bc14, bc15, bc17 (AB): **settled**. The question sets differed by B's boundary slots (dropped, 404 ¶1, 317 ¶1, 309 ¶1) and by the kinds of debt (kept where no wage or tort is claimed, 310 ¶1, 312 ¶2, 309 ¶1).

## Proposed slots: the mapping

| Reconciled id | A | B | Note |
|---|---|---|---|
| `debt_due_date` | `obligation_due_date` | `debt_due_date` | same fact (315 ¶1); B's "oldest sum" wording kept |
| `harmful_act_date` | `harmful_act_date` | `harmful_act_date` | same id |
| `forfeiture_lifted_by_order` | yes | none | cpca 322 ¶1 and ¶2 |
| `claim_is_professional_fees` | none | yes | 310 ¶1 |
| `claim_is_trader_supply_or_hotel` | none | yes | 312 ¶2 |
| `claim_is_periodic_renewable_right` | none | yes | 309 ¶1; question narrowed to rights other than the wages claimed, which d4 carries |

B's existing slots defendant_resides_abroad, employer_in_bankruptcy_or_settlement and worker_is_public_servant are no longer read (x32, x33, x36).

## Boundary cases disputed (1)

- **bc8**: about [civil-code 309 ¶1], `lastDay` 2022-06-15. Under 309 ¶1, counted by 314 ¶1 and civil-code 3 ¶1, five years from 2021-06-15 end on **2026-06-15**; 2022-06-15 is the last day of the one-year reading of 312 ¶3, another paragraph. The outcome "unsettled" is right.

Not disputed, but failing for a matching reason the verifier should know: a case with `lastDay` is matched to every deadline citing its paragraph, so bc2, bc5, bc6, bc7, bc11, bc12 and bc18 fail on deadlines that govern other periods (d1, d2, d6 to d8 are silent or asking), while the deadline the case means gives its last day. bc9 and bc10 now match only d5 (through 405 ¶3 and ¶4), while their last days belong to d3; their premise that 405 might extend a civil limitation is settled against it by 405 ¶1 («في هذا القانون»), and their last days are right. bc16 expects "in time" from a contract date; the record asks for the due date (315 ¶1) and cannot compare dates with the petition date by arithmetic.

## What I changed where A and B agreed

- d5: counting basis and basis now include **405 ¶3** (b34), which defines the non-working days that ¶4's extension depends on; both had left it out (cause of x72, x83).
- d3: basis now includes **317 ¶1** (b18) and **321 ¶1** (b23): `raisedBy: pleaOnly` rests on 321 ¶1, and the message names the interrupting acts.
- d2: second reading now also rests on cpca 69 ¶1 (b31); message points to the two readings.
- d6 to d8 (B's): `when` also requires no tort and no wage request, so tort and wage claims are not asked the kinds of debt (B had only the kind slot).
- b16: both paraphrased 316 ¶2 differently (A "no more than", B "less than"); the print reads «تنقضى مدته عن خمس سنوات», kept as "falls short of"; see the question below.
- b8, b10, b21 English `says` reworded without "one" (the value check could not find it in the quote); B's d2 message no longer names a five-year period (`messagePeriodDiffers`).
- Examples: A ex1 and ex13 now answer the three kinds of debt "no"; A ex1, ex4 and ex9 list d6 to d8; ex9's questions include the three kinds. B's examples lost their boundary slots and n1 finding; ex28 to ex30 answer tort "no". Under the reconciled rules the original expectations would fail only because of these new questions, not because a rule changed.
- Checklist: k66 and k67 (store notes on civil-code 3 and 133) answer from which date the wording applies; k69 answers the neighbour flag on civil-code 2 ¶1. One request was appended to `law/requests.jsonl` for the 1975 wording of civil-code 133.

## The third derivation (C)

- C cites 311 ¶2 and ¶3: the text settles that they add nothing here (a refund of taxes paid without right, and the saving of special laws: neither is a claim of this app); named in k30.
- C's 309 "fifteen years" is the exception of 309 ¶2 (b3); ¶1 gives five years: both rules exist.
- C's missing 133 and 504 values: C was not given those texts; 133 ¶1 and 504 ¶1 settle fifteen years and six months; the kind of 133 stays open (o3).

## Questions left to the adversary

1. d5 now rests on cpca 322 (verified only `corpusChecked`): under the critical-use rule d5 cannot reach grade A, so it cannot block, until 322 is read again on the page. Should the lifted-forfeiture exception stay in d5, and does 322 ¶1, placed in the general rules on remedies, reach 504 as its words say?
2. Every limitation deadline cites civil-code 3 ¶1 in its counting, and the store marks that entry unverified: those deadlines will carry "to check". d5 counts months too but does not cite it; should it?
3. d4 and d8 start on the due date and are not interpretive, though 313 ¶1 (interpretive, o2) names article 309 and would start them when the services are completed. The due date is the earlier date for wages and for rent paid in advance. Is that enough?
4. 316 ¶2 reads «تنقضى مدته عن خمس سنوات» and ¶3 «تزيد مدته على خمس سنوات»: on the printed words a period of exactly five years fits neither. Is this a misprint the dispute register should hold?
5. cpca 630 ¶1 (fifteen full years for the rights in an enforceable title) was found and not used: it concerns enforcement, not the filing of a claim.
6. bc16: should the record read contract_date to settle a limitation that cannot have run?
