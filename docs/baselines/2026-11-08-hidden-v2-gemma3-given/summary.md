# Alpha-01 owner run

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 51 | 94 % | 89 % | 83 % | 86 % | 92 % | 0 | 28 |

Critical errors: polarity 31, missing decisive event 15, forbidden reading 11, amount on wrong event 10, date on wrong event 7, plaintiff position 5, request amount 2, actor swapped 2

Other differences: missing event 30, date not given 18, missing request 14, amount not given 10, unreadable lines 3, relation 2

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `n001` (loan-repayment) [hidden] 24s
- ✗ `n002` (contract-damages) [hidden] 27s · plaintiff position (lessee instead of creditor); missing decisive event (other)
- ✗ `n003` (liability-for-things) [hidden] 20s · forbidden reading (harm_by_thing affirmed)
- ✓ `n004` (unpaid-rent) [hidden] 24s
- ✓ `n005` (divorce-by-husband) [hidden] 19s
- ✓ `n006` (unpaid-wages) [hidden] 24s
- ✓ `n007` (unpaid-rent) [hidden] 30s
- ✗ `n008` (sale-annulment-fraud) [hidden] 34s · date on wrong event (payment_made: 21/05/2025 instead of 04/06/2025); amount on wrong event (payment_made: 1000000 instead of 650000); request amount (damages: 240000 instead of 200000)
- – `n009` (out-of-scope) [hidden] 0s
- ✗ `n010` (contract-rescission) [hidden] 42s · date on wrong event (payment_made: 20/09/2023 instead of 09/05/2023); amount on wrong event (payment_made: 1000000 instead of 800000)
- ✗ `n011` (abusive-dismissal) [hidden] 27s · polarity (dismissal: affirmed instead of disputed)
- ✗ `n012` (unpaid-wages) [hidden] 28s · polarity (wages_unpaid: affirmed instead of disputed)
- ✗ `n013` (commercial-debt) [hidden] 34s · amount on wrong event (payment_made: 2940000 instead of 1000000)
- ✗ `n014` (unpaid-rent) [hidden] 32s · polarity (rent_unpaid: affirmed instead of disputed)
- ✗ `n015` (custody-forfeiture) [hidden] 30s · polarity (custody_renounced: affirmed instead of denied); forbidden reading (custody_renounced affirmed)
- ✗ `n016` (contract-rescission) [hidden] 40s · amount on wrong event (contract_concluded: 750000 instead of 1500000); polarity (work_stopped: affirmed instead of disputed)
- ✓ `n017` (contract-damages) [hidden] 39s
- – `n018` (out-of-scope) [hidden] 0s
- – `n019` (out-of-scope) [hidden] 0s
- ✓ `n020` (divorce-by-husband) [hidden] 22s
- ✓ `n021` (custody-forfeiture) [hidden] 31s
- ✓ `n022` (khul) [hidden] 29s
- ✓ `n023` (unpaid-sale-price) [hidden] 37s
- ✗ `n024` (liability-for-things) [hidden] 29s · missing decisive event (harm_by_thing)
- ✓ `n025` (unpaid-rent) [hidden] 34s
- ✗ `n026` (visitation) [hidden] 27s · polarity (custody_judgment: affirmed instead of denied); missing decisive event (visitation_refused)
- – `n027` (out-of-scope) [hidden] 0s
- ✓ `n028` (tatliq) [hidden] 36s
- ✗ `n029` (contract-rescission) [hidden] 40s · polarity (delivery_missing: affirmed instead of disputed)
- ✗ `n030` (sale-annulment-fraud) [hidden] 35s · polarity (concealment: affirmed instead of disputed)
- ✗ `n031` (commercial-lease-eviction) [hidden] 38s · polarity (rent_unpaid: affirmed instead of denied); forbidden reading (rent_unpaid affirmed)
- ✓ `n032` (nafaqa) [hidden] 28s
- ✗ `n033` (loan-repayment) [hidden] 38s · polarity (deadline_passed: affirmed instead of disputed)
- – `n034` (out-of-scope) [hidden] 0s
- ✓ `n035` (custody) [hidden] 27s
- ✗ `n036` (nafaqa) [hidden] 30s · polarity (maintenance_unpaid: affirmed instead of disputed)
- ✗ `n037` (sale-annulment-fraud) [hidden] 34s · plaintiff position (buyer instead of seller); polarity (concealment: affirmed instead of disputed)
- ✗ `n038` (unpaid-sale-price) [hidden] 31s · polarity (contract_concluded: affirmed instead of disputed); amount on wrong event (contract_concluded: 45000 instead of 1800000); missing decisive event (goods_delivered); forbidden reading (contract_concluded with 45000)
- ✗ `n039` (unpaid-wages) [hidden] 31s · polarity (employment_started: affirmed instead of disputed)
- ✗ `n040` (sale-annulment-fraud) [hidden] 32s · plaintiff position (buyer instead of other); polarity (concealment: disputed instead of affirmed)
- ✗ `n041` (visitation) [hidden] 25s · polarity (visitation_refused: affirmed instead of disputed)
- ✗ `n042` (contract-damages) [hidden] 31s · plaintiff position (buyer instead of seller); missing decisive event (other)
- ✗ `n043` (nafaqa) [hidden] 20s · polarity (maintenance_unpaid: affirmed instead of denied); forbidden reading (maintenance_unpaid affirmed)
- ✓ `n044` (eviction-occupier-without-title) [hidden] 20s
- ✓ `n045` (eviction-occupier-without-title) [hidden] 16s
- ✗ `n046` (liability-for-things) [hidden] 18s · missing decisive event (harm_by_thing); forbidden reading (harm_by_building affirmed)
- ✗ `n047` (unpaid-sale-price) [hidden] 29s · date on wrong event (payment_made: 01/03/2025 instead of 28/02/2025); polarity (goods_delivered: affirmed instead of disputed)
- ✗ `n048` (contract-damages) [hidden] 33s · polarity (delivery_missing: affirmed instead of disputed)
- ✗ `n049` (liability-for-things) [hidden] 19s · missing decisive event (harm_by_animal)
- ✓ `n050` (sale-non-delivery) [hidden] 28s
- – `n051` (out-of-scope) [hidden] 0s
- ✓ `n052` (khul) [hidden] 27s
- ✗ `n053` (tatliq) [hidden] 30s · polarity (spouse_harm: affirmed instead of disputed); polarity (maintenance_unpaid: affirmed instead of denied); forbidden reading (maintenance_unpaid affirmed)
- ✓ `n054` (abusive-dismissal) [hidden] 32s
- ✗ `n055` (eviction-occupier-without-title) [hidden] 26s · missing decisive event (occupation_without_title)
- – `n056` (out-of-scope) [hidden] 0s
- ✓ `n057` (custody) [hidden] 25s
- ✗ `n058` (commercial-debt) [hidden] 24s · amount on wrong event (goods_delivered: 700000 instead of 900000)
- ✗ `n059` (abusive-dismissal) [hidden] 31s · polarity (dismissal: affirmed instead of disputed)
- ✗ `n060` (commercial-lease-eviction) [hidden] 26s · plaintiff position (lessor instead of lessee); actor swapped (eviction_notice: plaintiff instead of defendant); actor swapped (lease_renewal_refused: plaintiff instead of defendant)
- ✗ `n061` (tatliq) [hidden] 33s · missing decisive event (abandonment)
- ✓ `n062` (loan-repayment) [hidden] 35s
- ✗ `n063` (eviction-occupier-without-title) [hidden] 26s · missing decisive event (occupation_without_title)
- ✗ `n064` (tort-damages) [hidden] 25s · polarity (harm_by_person: affirmed instead of disputed)
- ✓ `n065` (abusive-dismissal) [hidden] 29s
- ✓ `n066` (custody) [hidden] 27s
- ✓ `n067` (nafaqa) [hidden] 27s
- ✓ `n068` (khul) [hidden] 32s
- – `n069` (out-of-scope) [hidden] 0s
- ✗ `n070` (commercial-debt) [hidden] 32s · polarity (goods_delivered: affirmed instead of disputed); date on wrong event (goods_delivered: 05/06/2025 instead of 19/06/2025); amount on wrong event (goods_delivered: 620000 instead of 540000)
- ✓ `n071` (divorce-by-husband) [hidden] 22s
- ✗ `n072` (tort-damages) [hidden] 21s · date on wrong event (harm_by_person: 16/02/2025 instead of 14/02/2025)
- ✓ `n073` (custody-forfeiture) [hidden] 23s
- – `n074` (out-of-scope) [hidden] 0s
- ✓ `n075` (visitation) [hidden] 15s
- ✗ `n076` (contract-rescission) [hidden] 32s · missing decisive event (delivery_missing); polarity (notice_served: affirmed instead of denied); forbidden reading (notice_served affirmed)
- ✓ `n077` (sale-non-delivery) [hidden] 26s
- ✓ `n078` (visitation) [hidden] 15s
- ✗ `n079` (tatliq) [hidden] 25s · polarity (abandonment: affirmed instead of disputed)
- ✗ `n080` (contract-damages) [hidden] 30s · missing decisive event (other)
- ✓ `n081` (khul) [hidden] 23s
- ✗ `n082` (sale-non-delivery) [hidden] 24s · missing decisive event (payment_made)
- ✗ `n083` (unpaid-sale-price) [hidden] 27s · missing decisive event (goods_delivered)
- ✓ `n084` (tatliq) [hidden] 28s
- ✗ `n085` (tort-damages) [hidden] 20s · date on wrong event (criminal_conviction: 12/10/2023 instead of 28/04/2024); forbidden reading (criminal_conviction with 12/10/2023)
- ✗ `n086` (loan-repayment) [hidden] 25s · polarity (money_lent: affirmed instead of disputed)
- ✗ `n087` (custody-forfeiture) [hidden] 24s · missing decisive event (custodian_unfit)
- ✓ `n088` (tort-damages) [hidden] 21s
- ✗ `n089` (loan-repayment) [hidden] 31s · amount on wrong event (money_lent: 300000 instead of 200000); date on wrong event (deadline_passed: 19/01/2025 instead of 2024)
- ✗ `n090` (commercial-debt) [hidden] 26s · amount on wrong event (goods_delivered: 300 instead of 1380000); request amount (payment: 100000 instead of 1380000)
- ✓ `n091` (custody) [hidden] 27s
- ✗ `n092` (unpaid-rent) [hidden] 33s · polarity (notice_served: disputed instead of affirmed)
- ✓ `n093` (sale-non-delivery) [hidden] 29s
- ✓ `n094` (unpaid-wages) [hidden] 30s
- – `n095` (out-of-scope) [hidden] 0s
- ✓ `n096` (commercial-lease-eviction) [hidden] 35s
- ✓ `n097` (divorce-by-husband) [hidden] 23s
- ✗ `n098` (commercial-debt) [hidden] 24s · polarity (payment_made: affirmed instead of disputed); polarity (price_unpaid: affirmed instead of disputed)
- ✓ `n099` (abusive-dismissal) [hidden] 26s
- ✗ `n100` (commercial-lease-eviction) [hidden] 37s · amount on wrong event (rent_unpaid: 70000 instead of 420000); polarity (eviction_notice: affirmed instead of denied); forbidden reading (rent_unpaid with 70000); forbidden reading (eviction_notice affirmed)

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 1089 | 47.1 | 48 | 9.7 | 23.2 | 0 |




## Measurement

Mains / Best performance required; reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 2591.863 s; per attempted case: 25.919 s. Loads: 10.

Sampled model/server working-set peak: 6.237 GiB; private peak: 6.620 GiB; concurrent model+app working-set peak: 6.484 GiB; minimum machine available: 3.684 GiB. Resource samples: 1219; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
