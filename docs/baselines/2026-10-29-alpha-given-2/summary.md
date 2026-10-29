# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 6 | 97 % | 96 % | 83 % | 96 % | 79 % | 1 | 26 |
| validation | 14 | 2 | 100 % | 100 % | 86 % | 96 % | 82 % | 1 | 31 |
| robustness | 2 | 0 | 100 % | 100 % | n/a | 100 % | n/a | 0 | 29 |

Critical errors: polarity 3, missing decisive event 2, request amount 1, plaintiff position 1

Other differences: unreadable lines 28, missing request 8, date not given 3

5 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `loan-basic` (loan-repayment) [development] 48s
- ✓ `loan-darija` (loan-repayment) [robustness] 32s
- ✓ `loan-acknowledgment` (loan-repayment) [development] 31s
- ✓ `loan-no-section` (loan-repayment) [development] 30s
- ✓ `sale-price` (unpaid-sale-price) [development] 30s
- ✓ `sale-land-price` (unpaid-sale-price) [development] 28s
- ✓ `rescission` (contract-rescission) [development] 32s
- ✓ `rescission-sale` (contract-rescission) [development] 30s
- ✗ failed `contract-damages` [development] 54s · ollama gemma3:4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✓ `contract-damages-2` (contract-damages) [development] 50s
- ✓ `tort-assault` (tort-damages) [development] 18s
- ✓ `tort-damage-property` (tort-damages) [development] 17s
- ✓ `tort-defamation` (tort-damages) [development] 17s
- ✓ `things-water` (liability-for-things) [development] 20s
- ✓ `things-dog` (liability-for-things) [development] 19s
- ✓ `things-wall` (liability-for-things) [development] 18s
- ✓ `rent-basic` (unpaid-rent) [development] 26s
- ✓ `rent-darija` (unpaid-rent) [robustness] 26s
- ✓ `occupation` (eviction-occupier-without-title) [development] 19s
- ✓ `occupation-flat` (eviction-occupier-without-title) [development] 17s
- ✓ `commercial-invoices` (commercial-debt) [development] 26s
- ✓ `commercial-supplies` (commercial-debt) [development] 23s
- ✓ `commercial-lease` (commercial-lease-eviction) [development] 28s
- ✓ `commercial-lease-indemnity` (commercial-lease-eviction) [development] 28s
- ✗ `divorce-husband` (divorce-by-husband) [development] 13s · missing decisive event (marriage)
- ✓ `tatliq-abandonment` (tatliq) [development] 22s
- ✓ `tatliq-harm` (tatliq) [development] 22s
- ✗ `khul` (khul) [development] 15s · request amount (khul: - instead of 300000)
- ✓ `khul-2` (khul) [development] 29s
- ✓ `nafaqa-children` (nafaqa) [development] 20s
- ✓ `nafaqa-wife` (nafaqa) [development] 19s
- ✓ `custody` (custody) [development] 21s
- ✓ `custody-transfer` (custody) [development] 21s
- ✓ `visitation` (visitation) [development] 17s
- ✓ `dismissal` (abusive-dismissal) [development] 23s
- ✓ `dismissal-grave-fault` (abusive-dismissal) [development] 21s
- ✗ `wages` (unpaid-wages) [development] 22s · polarity (wages_unpaid: denied instead of affirmed)
- ✗ `wages-overtime` (unpaid-wages) [development] 21s · polarity (wages_unpaid: denied instead of affirmed)
- ✓ `loan-with-section-mismatch-words` (loan-repayment) [development] 34s
- ✓ `rent-shop-civil-words` (commercial-lease-eviction) [development] 33s
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✗ `annulment-fraud-land` (sale-annulment-fraud) [development] 38s · plaintiff position (seller instead of buyer); missing decisive event (contract_concluded)
- ✓ `non-delivery-machines` (sale-non-delivery) [development] 43s
- ✓ `lease-start-vs-signing` (unpaid-rent) [validation] 46s
- ✓ `lease-date-in-documents` (unpaid-rent) [validation] 29s
- ✓ `rent-no-notice` (unpaid-rent) [validation] 27s
- ✗ failed `tenant-plaintiff` [validation] 54s · ollama gemma3:4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✓ `assault-no-animal` (tort-damages) [validation] 19s
- – `borrower-plaintiff` (out-of-scope) [validation] 0s
- ✗ `disputed-loan` (loan-repayment) [validation] 30s · polarity (money_lent: affirmed instead of disputed)
- ✓ `amount-in-requests` (unpaid-sale-price) [validation] 29s
- ✓ `loan-due-vs-notice` (loan-repayment) [validation] 40s
- ✓ `commercial-lease-1998` (commercial-lease-eviction) [validation] 31s
- ✓ `tort-no-conviction` (tort-damages) [validation] 21s
- ✓ `dismissal-no-conciliation` (abusive-dismissal) [validation] 25s
- ✓ `loan-family-section` (loan-repayment) [validation] 32s
- ✓ `buyer-plaintiff` (sale-non-delivery) [validation] 29s
- ✓ `rent-monthly-vs-total` (unpaid-rent) [validation] 27s

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 56 | 0.0 | 712 | 37.3 | 46 | 5.7 | 19.3 | 0 |


