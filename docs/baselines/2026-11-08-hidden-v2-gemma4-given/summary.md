# Alpha-01 owner run

## ollama gemma4:e4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 32 | 100 % | 98 % | 92 % | 91 % | 95 % | 0 | 21 |

Critical errors: polarity 24, forbidden reading 4, date on wrong event 4, amount on wrong event 4, missing decisive event 3

Other differences: missing event 14, unreadable lines 11, missing request 9, date not given 8, amount not given 6

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `n001` (loan-repayment) [hidden] 23s
- ✓ `n002` (contract-damages) [hidden] 30s
- ✗ `n003` (liability-for-things) [hidden] 20s · forbidden reading (harm_by_thing affirmed)
- ✓ `n004` (unpaid-rent) [hidden] 23s
- ✓ `n005` (divorce-by-husband) [hidden] 18s
- ✓ `n006` (unpaid-wages) [hidden] 21s
- ✓ `n007` (unpaid-rent) [hidden] 25s
- ✗ `n008` (sale-annulment-fraud) [hidden] 26s · date on wrong event (payment_made: 21/05/2025 instead of 04/06/2025); amount on wrong event (payment_made: 1000000 instead of 650000)
- – `n009` (out-of-scope) [hidden] 0s
- ✗ `n010` (contract-rescission) [hidden] 32s · amount on wrong event (payment_made: 1000000 instead of 800000)
- ✓ `n011` (abusive-dismissal) [hidden] 20s
- ✗ `n012` (unpaid-wages) [hidden] 21s · polarity (wages_unpaid: affirmed instead of disputed)
- ✓ `n013` (commercial-debt) [hidden] 26s
- ✓ `n014` (unpaid-rent) [hidden] 24s
- ✓ `n015` (custody-forfeiture) [hidden] 24s
- ✗ `n016` (contract-rescission) [hidden] 29s · polarity (work_stopped: affirmed instead of disputed)
- ✓ `n017` (contract-damages) [hidden] 29s
- – `n018` (out-of-scope) [hidden] 0s
- – `n019` (out-of-scope) [hidden] 0s
- ✓ `n020` (divorce-by-husband) [hidden] 17s
- ✓ `n021` (custody-forfeiture) [hidden] 23s
- ✓ `n022` (khul) [hidden] 21s
- ✓ `n023` (unpaid-sale-price) [hidden] 26s
- ✓ `n024` (liability-for-things) [hidden] 22s
- ✓ `n025` (unpaid-rent) [hidden] 25s
- ✗ `n026` (visitation) [hidden] 19s · missing decisive event (visitation_refused)
- – `n027` (out-of-scope) [hidden] 0s
- ✗ `n028` (tatliq) [hidden] 20s · forbidden reading (abandonment affirmed)
- ✗ `n029` (contract-rescission) [hidden] 22s · polarity (delivery_missing: affirmed instead of disputed)
- ✗ `n030` (sale-annulment-fraud) [hidden] 19s · polarity (concealment: affirmed instead of disputed); missing decisive event (payment_made)
- ✓ `n031` (commercial-lease-eviction) [hidden] 25s
- ✓ `n032` (nafaqa) [hidden] 22s
- ✓ `n033` (loan-repayment) [hidden] 29s
- – `n034` (out-of-scope) [hidden] 0s
- ✓ `n035` (custody) [hidden] 20s
- ✗ `n036` (nafaqa) [hidden] 21s · polarity (divorce: affirmed instead of denied); forbidden reading (divorce affirmed)
- ✗ `n037` (sale-annulment-fraud) [hidden] 25s · polarity (concealment: affirmed instead of disputed)
- ✗ `n038` (unpaid-sale-price) [hidden] 23s · polarity (contract_concluded: affirmed instead of disputed)
- ✗ `n039` (unpaid-wages) [hidden] 22s · polarity (employment_started: affirmed instead of disputed)
- ✓ `n040` (sale-annulment-fraud) [hidden] 25s
- ✓ `n041` (visitation) [hidden] 17s
- ✓ `n042` (contract-damages) [hidden] 24s
- ✓ `n043` (nafaqa) [hidden] 15s
- ✓ `n044` (eviction-occupier-without-title) [hidden] 14s
- ✓ `n045` (eviction-occupier-without-title) [hidden] 12s
- ✗ `n046` (liability-for-things) [hidden] 14s · missing decisive event (harm_by_thing); forbidden reading (harm_by_building affirmed)
- ✗ `n047` (unpaid-sale-price) [hidden] 22s · polarity (goods_delivered: affirmed instead of disputed)
- ✗ `n048` (contract-damages) [hidden] 24s · polarity (delivery_missing: affirmed instead of disputed)
- ✗ `n049` (liability-for-things) [hidden] 14s · polarity (harm_by_animal: affirmed instead of disputed)
- ✓ `n050` (sale-non-delivery) [hidden] 20s
- – `n051` (out-of-scope) [hidden] 0s
- ✓ `n052` (khul) [hidden] 21s
- ✓ `n053` (tatliq) [hidden] 24s
- ✓ `n054` (abusive-dismissal) [hidden] 21s
- ✗ `n055` (eviction-occupier-without-title) [hidden] 20s · polarity (occupation_without_title: affirmed instead of disputed)
- – `n056` (out-of-scope) [hidden] 0s
- ✓ `n057` (custody) [hidden] 20s
- ✗ `n058` (commercial-debt) [hidden] 19s · polarity (notice_served: affirmed instead of denied)
- ✗ `n059` (abusive-dismissal) [hidden] 21s · polarity (dismissal: affirmed instead of disputed)
- ✗ `n060` (commercial-lease-eviction) [hidden] 20s · polarity (lease_renewal_refused: denied instead of affirmed)
- ✓ `n061` (tatliq) [hidden] 27s
- ✓ `n062` (loan-repayment) [hidden] 26s
- ✓ `n063` (eviction-occupier-without-title) [hidden] 20s
- ✗ `n064` (tort-damages) [hidden] 20s · polarity (harm_by_person: affirmed instead of disputed)
- ✓ `n065` (abusive-dismissal) [hidden] 22s
- ✓ `n066` (custody) [hidden] 22s
- ✓ `n067` (nafaqa) [hidden] 21s
- ✓ `n068` (khul) [hidden] 25s
- – `n069` (out-of-scope) [hidden] 0s
- ✗ `n070` (commercial-debt) [hidden] 25s · polarity (goods_delivered: affirmed instead of disputed); date on wrong event (goods_delivered: 05/06/2025 instead of 19/06/2025); amount on wrong event (goods_delivered: 620000 instead of 540000)
- ✓ `n071` (divorce-by-husband) [hidden] 18s
- ✓ `n072` (tort-damages) [hidden] 16s
- ✓ `n073` (custody-forfeiture) [hidden] 18s
- – `n074` (out-of-scope) [hidden] 0s
- ✓ `n075` (visitation) [hidden] 11s
- ✓ `n076` (contract-rescission) [hidden] 24s
- ✗ `n077` (sale-non-delivery) [hidden] 20s · date on wrong event (payment_made: 04/11/2024 instead of 18/11/2024)
- ✓ `n078` (visitation) [hidden] 12s
- ✗ `n079` (tatliq) [hidden] 19s · polarity (abandonment: affirmed instead of disputed)
- ✓ `n080` (contract-damages) [hidden] 23s
- ✓ `n081` (khul) [hidden] 18s
- ✗ `n082` (sale-non-delivery) [hidden] 19s · polarity (payment_made: affirmed instead of disputed)
- ✗ `n083` (unpaid-sale-price) [hidden] 21s · polarity (notice_served: affirmed instead of denied)
- ✓ `n084` (tatliq) [hidden] 21s
- ✓ `n085` (tort-damages) [hidden] 15s
- ✗ `n086` (loan-repayment) [hidden] 19s · polarity (money_lent: affirmed instead of disputed)
- ✗ `n087` (custody-forfeiture) [hidden] 19s · polarity (custodian_unfit: affirmed instead of disputed)
- ✓ `n088` (tort-damages) [hidden] 15s
- ✗ `n089` (loan-repayment) [hidden] 24s · date on wrong event (money_lent: 10/02/2024 instead of 25/03/2024); amount on wrong event (money_lent: 300000 instead of 200000)
- ✓ `n090` (commercial-debt) [hidden] 19s
- ✓ `n091` (custody) [hidden] 19s
- ✓ `n092` (unpaid-rent) [hidden] 25s
- ✓ `n093` (sale-non-delivery) [hidden] 22s
- ✓ `n094` (unpaid-wages) [hidden] 22s
- – `n095` (out-of-scope) [hidden] 0s
- ✗ `n096` (commercial-lease-eviction) [hidden] 26s · polarity (rent_unpaid: denied instead of disputed)
- ✓ `n097` (divorce-by-husband) [hidden] 17s
- ✗ `n098` (commercial-debt) [hidden] 18s · polarity (price_unpaid: affirmed instead of disputed)
- ✓ `n099` (abusive-dismissal) [hidden] 16s
- ✓ `n100` (commercial-lease-eviction) [hidden] 19s

### Where the time goes (ollama gemma4:e4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 1085 | 63.1 | 43 | 11.0 | 17.2 | 0 |




## Measurement

Mains / Best performance required; reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 2001.927 s; per attempted case: 20.019 s. Loads: 10.

Sampled model/server working-set peak: 7.244 GiB; private peak: 7.968 GiB; concurrent model+app working-set peak: 7.424 GiB; minimum machine available: 3.684 GiB. Resource samples: 944; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
