# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 58 | 97 % | 88 % | 80 % | 81 % | 93 % | 0 | 63 |

Critical errors: polarity 45, date on wrong event 19, missing decisive event 16, forbidden reading 15, request amount 10, amount on wrong event 8, plaintiff position 3, actor swapped 3

Other differences: missing event 50, amount not given 19, date not given 14, missing request 13, unreadable lines 4

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✗ `h066` (liability-for-things) [hidden] 81s · request amount (damages: 67000 instead of 50000)
- ✗ `h053` (contract-damages) [hidden] 73s · date on wrong event (delivery_late: 10/06/2024 instead of 18/06/2024); polarity (delivery_missing: affirmed instead of denied); forbidden reading (delivery_missing affirmed)
- ✓ `h081` (nafaqa) [hidden] 58s
- ✗ `h006` (nafaqa) [hidden] 72s · polarity (maintenance_unpaid: affirmed instead of disputed); request amount (maintenance: 22000 instead of 16000)
- ✗ `h059` (custody-forfeiture) [hidden] 71s · polarity (custody_renounced: affirmed instead of denied); polarity (custodian_abroad: affirmed instead of denied); forbidden reading (custody_renounced affirmed); forbidden reading (custodian_abroad affirmed)
- ✓ `h025` (abusive-dismissal) [hidden] 67s
- ✓ `h047` (tatliq) [hidden] 63s
- ✗ `h041` (abusive-dismissal) [hidden] 56s · polarity (dismissal: affirmed instead of disputed); polarity (conciliation_failed: disputed instead of affirmed)
- ✓ `h079` (sale-non-delivery) [hidden] 63s
- ✓ `h082` (unpaid-wages) [hidden] 56s
- ✗ `h056` (contract-damages) [hidden] 75s · date on wrong event (contract_concluded: 01/09/2023 instead of 16/08/2023)
- ✗ `h022` (unpaid-rent) [hidden] 71s · date on wrong event (lease_started: 09/09/2017 instead of 30/10/2023); polarity (rent_unpaid: disputed instead of affirmed)
- ✓ `h087` (tort-damages) [hidden] 47s
- ✗ `h096` (commercial-lease-eviction) [hidden] 71s · date on wrong event (eviction_notice: 30/04/2024 instead of 18/10/2023)
- ✓ `h014` (unpaid-sale-price) [hidden] 63s
- – `h058` (out-of-scope) [hidden] 0s
- ✗ `h090` (unpaid-rent) [hidden] 72s · amount on wrong event (rent_unpaid: 246000 instead of 164000); forbidden reading (rent_unpaid with 246000)
- – `h018` (out-of-scope) [hidden] 0s
- ✗ `h016` (commercial-debt) [hidden] 62s · polarity (goods_delivered: affirmed instead of disputed); date on wrong event (goods_delivered: 04/01/2024 instead of 20/01/2024)
- ✓ `h071` (divorce-by-husband) [hidden] 56s
- ✗ `h013` (liability-for-things) [hidden] 53s · missing decisive event (harm_by_thing); polarity (harm_by_building: affirmed instead of denied); forbidden reading (harm_by_building affirmed)
- ✗ `h034` (unpaid-sale-price) [hidden] 62s · polarity (goods_delivered: affirmed instead of disputed)
- ✗ `h089` (custody) [hidden] 57s · missing decisive event (custody_held)
- ✗ `h039` (sale-non-delivery) [hidden] 68s · date on wrong event (contract_concluded: 04/04/2022 instead of 28/03/2022); polarity (deadline_passed: disputed instead of affirmed); polarity (delivery_missing: affirmed instead of disputed); date on wrong event (notice_served: 01/04/2022 instead of 03/05/2022)
- – `h031` (out-of-scope) [hidden] 0s
- ✗ `h098` (unpaid-wages) [hidden] 72s · polarity (wages_unpaid: denied instead of affirmed)
- ✗ `h054` (khul) [hidden] 64s · request amount (maintenance: 90000 instead of 11000)
- ✓ `h028` (khul) [hidden] 59s
- ✗ `h033` (liability-for-things) [hidden] 51s · polarity (harm_by_animal: affirmed instead of denied); forbidden reading (harm_by_animal affirmed)
- ✓ `h023` (unpaid-rent) [hidden] 64s
- ✓ `h063` (custody) [hidden] 50s
- ✓ `h003` (visitation) [hidden] 44s
- ✓ `h070` (divorce-by-husband) [hidden] 48s
- ✗ `h049` (tatliq) [hidden] 67s · polarity (abandonment: affirmed instead of disputed); polarity (spouse_harm: affirmed instead of denied); forbidden reading (spouse_harm affirmed)
- ✗ `h100` (tort-damages) [hidden] 53s · missing decisive event (harm_by_person); request amount (damages: - instead of 240000)
- ✗ `h043` (nafaqa) [hidden] 65s · request amount (maintenance: 180000 instead of 15000)
- ✗ `h002` (loan-repayment) [hidden] 61s · missing decisive event (debt_unpaid)
- ✗ `h077` (unpaid-wages) [hidden] 61s · request amount (damages: 96000 instead of 18000)
- ✗ `h097` (loan-repayment) [hidden] 77s · amount on wrong event (debt_acknowledged: 500000 instead of 720000); polarity (deadline_passed: disputed instead of affirmed); polarity (payment_made: affirmed instead of disputed); amount on wrong event (payment_made: 220000 instead of 500000)
- ✗ `h036` (loan-repayment) [hidden] 72s · polarity (debt_unpaid: affirmed instead of disputed); polarity (notice_served: disputed instead of affirmed); date on wrong event (notice_served: 08/08/2022 instead of 12/09/2022)
- ✓ `h050` (abusive-dismissal) [hidden] 65s
- ✓ `h024` (eviction-occupier-without-title) [hidden] 55s
- – `h065` (out-of-scope) [hidden] 0s
- ✗ `h052` (custody) [hidden] 58s · missing decisive event (custody_held)
- ✗ `h086` (liability-for-things) [hidden] 57s · polarity (harm_by_building: affirmed instead of disputed)
- ✗ `h055` (tort-damages) [hidden] 51s · request amount (damages: 36000 instead of 90000)
- ✗ `h040` (sale-non-delivery) [hidden] 77s · polarity (payment_made: affirmed instead of disputed); amount on wrong event (payment_made: 210000 instead of 300000)
- ✗ `h051` (tort-damages) [hidden] 56s · polarity (harm_by_person: affirmed instead of disputed); request amount (damages: 54000 instead of 30000)
- – `h032` (out-of-scope) [hidden] 0s
- ✓ `h017` (sale-annulment-fraud) [hidden] 63s
- ✓ `h030` (khul) [hidden] 74s
- ✗ `h076` (loan-repayment) [hidden] 62s · missing decisive event (debt_acknowledged)
- – `h060` (out-of-scope) [hidden] 0s
- ✓ `h027` (visitation) [hidden] 33s
- ✓ `h038` (unpaid-rent) [hidden] 68s
- ✗ `h073` (eviction-occupier-without-title) [hidden] 49s · missing decisive event (occupation_without_title)
- ✓ `h074` (commercial-debt) [hidden] 67s
- ✓ `h021` (abusive-dismissal) [hidden] 57s
- ✗ `h026` (custody-forfeiture) [hidden] 64s · polarity (custody_renounced: affirmed instead of denied); forbidden reading (custody_renounced affirmed)
- ✗ `h057` (tatliq) [hidden] 69s · polarity (maintenance_unpaid: affirmed instead of denied); forbidden reading (maintenance_unpaid affirmed)
- – `h008` (out-of-scope) [hidden] 0s
- – `h084` (out-of-scope) [hidden] 0s
- ✗ `h005` (unpaid-sale-price) [hidden] 70s · date on wrong event (payment_made: 20/11/2021 instead of 18/11/2021); polarity (notice_served: affirmed instead of disputed); date on wrong event (notice_served: 17/11/2021 instead of 12/02/2022)
- ✗ `h044` (visitation) [hidden] 35s · missing decisive event (visitation_refused)
- ✓ `h007` (sale-annulment-fraud) [hidden] 62s
- ✓ `h020` (nafaqa) [hidden] 60s
- ✗ `h091` (commercial-lease-eviction) [hidden] 69s · plaintiff position (lessee instead of lessor); date on wrong event (contract_concluded: 01/06/2004 instead of 17/11/2001); polarity (eviction_notice: affirmed instead of disputed); actor swapped (eviction_notice: defendant instead of plaintiff); actor swapped (lease_renewal_refused: defendant instead of plaintiff); actor swapped (rent_unpaid: plaintiff instead of defendant); forbidden reading (contract_concluded with 01/06/2004)
- ✗ `h067` (contract-rescission) [hidden] 69s · plaintiff position (client instead of contractor); polarity (payment_made: affirmed instead of denied); polarity (delivery_missing: affirmed instead of disputed); polarity (notice_served: affirmed instead of disputed); forbidden reading (payment_made with 130000)
- ✗ `h010` (commercial-lease-eviction) [hidden] 76s · polarity (rent_unpaid: affirmed instead of denied); forbidden reading (rent_unpaid affirmed)
- ✓ `h092` (contract-damages) [hidden] 78s
- ✗ `h072` (unpaid-rent) [hidden] 70s · polarity (rent_unpaid: affirmed instead of disputed); amount on wrong event (rent_unpaid: 36000 instead of 180000); forbidden reading (rent_unpaid with 36000)
- ✗ `h099` (commercial-lease-eviction) [hidden] 71s · polarity (lease_renewal_refused: affirmed instead of disputed); polarity (rent_unpaid: disputed instead of affirmed)
- – `h009` (out-of-scope) [hidden] 0s
- ✗ `h068` (contract-damages) [hidden] 75s · polarity (other: affirmed instead of disputed); forbidden reading (work_stopped affirmed)
- ✗ `h062` (custody) [hidden] 57s · missing decisive event (custody_held)
- ✗ `h095` (sale-annulment-fraud) [hidden] 66s · missing decisive event (concealment)
- ✗ `h045` (khul) [hidden] 60s · date on wrong event (marriage: 2018 instead of 04/04/2019); forbidden reading (marriage with 2018)
- ✓ `h046` (sale-non-delivery) [hidden] 68s
- ✓ `h004` (loan-repayment) [hidden] 73s
- ✗ `h037` (contract-rescission) [hidden] 72s · date on wrong event (notice_served: 12/02/2024 instead of 09/04/2024)
- ✗ `h069` (custody-forfeiture) [hidden] 68s · date on wrong event (custodian_remarried: 09/09/2023 instead of 30/08/2023)
- ✗ `h064` (sale-annulment-fraud) [hidden] 70s · amount on wrong event (contract_concluded: 120000 instead of 320000); date on wrong event (payment_made: 02/08/2023 instead of 24/07/2023)
- ✓ `h078` (commercial-debt) [hidden] 61s
- ✓ `h001` (tatliq) [hidden] 58s
- ✓ `h019` (divorce-by-husband) [hidden] 53s
- – `h011` (out-of-scope) [hidden] 0s
- ✗ `h042` (unpaid-sale-price) [hidden] 83s · date on wrong event (payment_made: 10/03/2022 instead of 06/02/2022); polarity (payment_made: affirmed instead of disputed); amount on wrong event (payment_made: 800000 instead of 480000); missing decisive event (price_unpaid)
- ✓ `h048` (nafaqa) [hidden] 77s
- ✗ `h029` (custody-forfeiture) [hidden] 67s · polarity (custodian_abroad: affirmed instead of disputed)
- ✗ `h012` (contract-rescission) [hidden] 68s · polarity (delivery_missing: affirmed instead of disputed)
- ✗ `h093` (contract-damages) [hidden] 72s · plaintiff position (buyer instead of seller); polarity (other: affirmed instead of disputed)
- ✗ `h083` (commercial-debt) [hidden] 79s · missing decisive event (goods_delivered); polarity (payment_made: affirmed instead of disputed); date on wrong event (payment_made: 15/05/2022 instead of 03/06/2022); amount on wrong event (payment_made: 650000 instead of 1500000); missing decisive event (price_unpaid); request amount (damages: 1500000 instead of 180000)
- ✗ `h035` (contract-rescission) [hidden] 70s · missing decisive event (delivery_missing)
- ✗ `h075` (unpaid-sale-price) [hidden] 60s · polarity (payment_made: affirmed instead of denied)
- ✓ `h080` (visitation) [hidden] 34s
- ✓ `h094` (divorce-by-husband) [hidden] 44s
- ✗ `h088` (tort-damages) [hidden] 53s · polarity (criminal_conviction: affirmed instead of disputed); polarity (harm_by_person: affirmed instead of denied); date on wrong event (harm_by_person: 16/06/2021 instead of 03/05/2021)
- ✗ `h061` (eviction-occupier-without-title) [hidden] 38s · missing decisive event (occupation_without_title)
- ✗ `h015` (eviction-occupier-without-title) [hidden] 42s · missing decisive event (occupation_without_title); polarity (notice_served: affirmed instead of disputed)
- ✗ `h085` (unpaid-wages) [hidden] 64s · polarity (wages_unpaid: affirmed instead of disputed); date on wrong event (conciliation_failed: 15/08/2018 instead of 24/07/2024); request amount (damages: 292000 instead of 35000)

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.3 | 1175 | 22.0 | 46 | 5.5 | 54.2 | 0 |


