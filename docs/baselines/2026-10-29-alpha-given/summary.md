# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 5 | 95 % | 96 % | 88 % | 100 % | 87 % | 0 | 27 |
| validation | 14 | 5 | 93 % | 96 % | 71 % | 88 % | 83 % | 0 | 28 |
| robustness | 2 | 0 | 100 % | 100 % | n/a | 100 % | n/a | 0 | 28 |

Critical errors: plaintiff position 3, missing decisive event 3, polarity 3, request amount 1

Other differences: unreadable lines 32, missing request 6, date not given 5, amount not given 3, missing event 1

5 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `loan-basic` (loan-repayment) [development] 39s
- ✓ `loan-darija` (loan-repayment) [robustness] 31s
- ✓ `loan-acknowledgment` (loan-repayment) [development] 30s
- ✓ `loan-no-section` (loan-repayment) [development] 33s
- ✓ `sale-price` (unpaid-sale-price) [development] 31s
- ✓ `sale-land-price` (unpaid-sale-price) [development] 31s
- ✓ `rescission` (contract-rescission) [development] 32s
- ✓ `rescission-sale` (contract-rescission) [development] 31s
- ✗ `contract-damages` (contract-damages) [development] 34s · plaintiff position (seller instead of buyer|client|creditor|other)
- ✓ `contract-damages-2` (contract-damages) [development] 31s
- ✓ `tort-assault` (tort-damages) [development] 21s
- ✓ `tort-damage-property` (tort-damages) [development] 18s
- ✓ `tort-defamation` (tort-damages) [development] 19s
- ✓ `things-water` (liability-for-things) [development] 25s
- ✓ `things-dog` (liability-for-things) [development] 20s
- ✓ `things-wall` (liability-for-things) [development] 20s
- ✗ `rent-basic` (unpaid-rent) [development] 29s · missing decisive event (notice_served)
- ✓ `rent-darija` (unpaid-rent) [robustness] 25s
- ✓ `occupation` (eviction-occupier-without-title) [development] 19s
- ✓ `occupation-flat` (eviction-occupier-without-title) [development] 19s
- ✗ `commercial-invoices` (commercial-debt) [development] 27s · missing decisive event (notice_served)
- ✓ `commercial-supplies` (commercial-debt) [development] 25s
- ✗ `commercial-lease` (commercial-lease-eviction) [development] 27s · plaintiff position (lessee instead of lessor)
- ✓ `commercial-lease-indemnity` (commercial-lease-eviction) [development] 30s
- ✓ `divorce-husband` (divorce-by-husband) [development] 18s
- ✓ `tatliq-abandonment` (tatliq) [development] 25s
- ✓ `tatliq-harm` (tatliq) [development] 27s
- ✗ `khul` (khul) [development] 19s · request amount (khul: - instead of 300000)
- ✓ `khul-2` (khul) [development] 15s
- ✓ `nafaqa-children` (nafaqa) [development] 26s
- ✓ `nafaqa-wife` (nafaqa) [development] 26s
- ✓ `custody` (custody) [development] 31s
- ✓ `custody-transfer` (custody) [development] 32s
- ✓ `visitation` (visitation) [development] 24s
- ✓ `dismissal` (abusive-dismissal) [development] 36s
- ✓ `dismissal-grave-fault` (abusive-dismissal) [development] 30s
- ✓ `wages` (unpaid-wages) [development] 27s
- ✓ `wages-overtime` (unpaid-wages) [development] 22s
- ✓ `loan-with-section-mismatch-words` (loan-repayment) [development] 28s
- ✓ `rent-shop-civil-words` (commercial-lease-eviction) [development] 28s
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✓ `annulment-fraud-land` (sale-annulment-fraud) [development] 32s
- ✓ `non-delivery-machines` (sale-non-delivery) [development] 34s
- ✗ `lease-start-vs-signing` (unpaid-rent) [validation] 39s · polarity (notice_served: denied instead of affirmed)
- ✗ `lease-date-in-documents` (unpaid-rent) [validation] 26s · polarity (notice_served: denied instead of affirmed)
- ✓ `rent-no-notice` (unpaid-rent) [validation] 23s
- ✓ `tenant-plaintiff` (contract-damages) [validation] 32s
- ✓ `assault-no-animal` (tort-damages) [validation] 20s
- – `borrower-plaintiff` (out-of-scope) [validation] 0s
- ✗ `disputed-loan` (loan-repayment) [validation] 28s · polarity (money_lent: affirmed instead of disputed)
- ✓ `amount-in-requests` (unpaid-sale-price) [validation] 25s
- ✓ `loan-due-vs-notice` (loan-repayment) [validation] 30s
- ✗ `commercial-lease-1998` (commercial-lease-eviction) [validation] 32s · plaintiff position (lessee instead of lessor)
- ✓ `tort-no-conviction` (tort-damages) [validation] 22s
- ✓ `dismissal-no-conciliation` (abusive-dismissal) [validation] 26s
- ✓ `loan-family-section` (loan-repayment) [validation] 27s
- ✗ `buyer-plaintiff` (sale-non-delivery) [validation] 31s · missing decisive event (notice_served)
- ✓ `rent-monthly-vs-total` (unpaid-rent) [validation] 30s

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 56 | 0.0 | 631 | 32.9 | 45 | 5.7 | 19.3 | 0 |


