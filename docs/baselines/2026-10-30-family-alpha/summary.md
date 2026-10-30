# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 5 | 97 % | 92 % | 86 % | 100 % | 83 % | 0 | 28 |
| validation | 14 | 3 | 93 % | 96 % | 93 % | 93 % | 83 % | 0 | 33 |
| robustness | 2 | 0 | 100 % | 100 % | n/a | 100 % | n/a | 0 | 30 |

Critical errors: missing decisive event 5, plaintiff position 2, polarity 2

Other differences: missing request 7, unreadable lines 4, date not given 2, amount not given 2, relation 2

5 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `loan-basic` (loan-repayment) [development] 63s
- ✓ `loan-darija` (loan-repayment) [robustness] 34s
- ✓ `loan-acknowledgment` (loan-repayment) [development] 31s
- ✓ `loan-no-section` (loan-repayment) [development] 35s
- ✓ `sale-price` (unpaid-sale-price) [development] 43s
- ✓ `sale-land-price` (unpaid-sale-price) [development] 32s
- ✓ `rescission` (contract-rescission) [development] 36s
- ✓ `rescission-sale` (contract-rescission) [development] 32s
- ✗ `contract-damages` (contract-damages) [development] 36s · plaintiff position (lessee instead of buyer|client|creditor|other)
- ✓ `contract-damages-2` (contract-damages) [development] 33s
- ✓ `tort-assault` (tort-damages) [development] 18s
- ✓ `tort-damage-property` (tort-damages) [development] 15s
- ✓ `tort-defamation` (tort-damages) [development] 15s
- ✓ `things-water` (liability-for-things) [development] 19s
- ✓ `things-dog` (liability-for-things) [development] 18s
- ✓ `things-wall` (liability-for-things) [development] 19s
- ✗ `rent-basic` (unpaid-rent) [development] 27s · missing decisive event (notice_served)
- ✓ `rent-darija` (unpaid-rent) [robustness] 26s
- ✓ `occupation` (eviction-occupier-without-title) [development] 21s
- ✓ `occupation-flat` (eviction-occupier-without-title) [development] 20s
- ✓ `commercial-invoices` (commercial-debt) [development] 30s
- ✓ `commercial-supplies` (commercial-debt) [development] 29s
- ✓ `commercial-lease` (commercial-lease-eviction) [development] 36s
- ✓ `commercial-lease-indemnity` (commercial-lease-eviction) [development] 35s
- ✗ `divorce-husband` (divorce-by-husband) [development] 57s · missing decisive event (marriage)
- ✓ `tatliq-abandonment` (tatliq) [development] 40s
- ✓ `tatliq-harm` (tatliq) [development] 29s
- ✓ `khul` (khul) [development] 23s
- ✓ `khul-2` (khul) [development] 21s
- ✓ `nafaqa-children` (nafaqa) [development] 22s
- ✓ `nafaqa-wife` (nafaqa) [development] 25s
- ✓ `custody` (custody) [development] 31s
- ✗ `custody-transfer` (custody-forfeiture) [development] 35s · missing decisive event (custodian_remarried)
- ✓ `visitation` (visitation) [development] 18s
- ✓ `dismissal` (abusive-dismissal) [development] 22s
- ✓ `dismissal-grave-fault` (abusive-dismissal) [development] 19s
- ✗ `wages` (unpaid-wages) [development] 20s · missing decisive event (conciliation_failed)
- ✓ `wages-overtime` (unpaid-wages) [development] 18s
- ✓ `loan-with-section-mismatch-words` (loan-repayment) [development] 27s
- ✓ `rent-shop-civil-words` (commercial-lease-eviction) [development] 25s
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✓ `annulment-fraud-land` (sale-annulment-fraud) [development] 28s
- ✓ `non-delivery-machines` (sale-non-delivery) [development] 30s
- ✓ `lease-start-vs-signing` (unpaid-rent) [validation] 40s
- ✓ `lease-date-in-documents` (unpaid-rent) [validation] 26s
- ✗ `rent-no-notice` (unpaid-rent) [validation] 24s · polarity (notice_served: disputed instead of denied)
- ✗ `tenant-plaintiff` (contract-damages) [validation] 88s · plaintiff position (buyer instead of lessee); missing decisive event (harm_by_person|other)
- ✓ `assault-no-animal` (tort-damages) [validation] 22s
- – `borrower-plaintiff` (out-of-scope) [validation] 0s
- ✗ `disputed-loan` (loan-repayment) [validation] 36s · polarity (money_lent: affirmed instead of disputed)
- ✓ `amount-in-requests` (unpaid-sale-price) [validation] 30s
- ✓ `loan-due-vs-notice` (loan-repayment) [validation] 37s
- ✓ `commercial-lease-1998` (commercial-lease-eviction) [validation] 31s
- ✓ `tort-no-conviction` (tort-damages) [validation] 19s
- ✓ `dismissal-no-conciliation` (abusive-dismissal) [validation] 23s
- ✓ `loan-family-section` (loan-repayment) [validation] 28s
- ✓ `buyer-plaintiff` (sale-non-delivery) [validation] 30s
- ✓ `rent-monthly-vs-total` (unpaid-rent) [validation] 33s

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 56 | 0.2 | 745 | 38.1 | 49 | 5.1 | 19.9 | 0 |


