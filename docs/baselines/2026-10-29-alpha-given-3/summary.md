# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 4 | 97 % | 94 % | 86 % | 100 % | 87 % | 0 | 21 |
| validation | 14 | 3 | 100 % | 100 % | 93 % | 93 % | 91 % | 1 | 28 |
| robustness | 2 | 0 | 100 % | 100 % | n/a | 100 % | n/a | 0 | 24 |

Critical errors: missing decisive event 3, polarity 2, plaintiff position 1

Other differences: unreadable lines 5, missing request 5, date not given 2, amount not given 1, relation 1

5 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✗ `loan-basic` (loan-repayment) [development] 44s · missing decisive event (notice_served)
- ✓ `loan-darija` (loan-repayment) [robustness] 28s
- ✓ `loan-acknowledgment` (loan-repayment) [development] 28s
- ✓ `loan-no-section` (loan-repayment) [development] 27s
- ✓ `sale-price` (unpaid-sale-price) [development] 27s
- ✓ `sale-land-price` (unpaid-sale-price) [development] 26s
- ✓ `rescission` (contract-rescission) [development] 31s
- ✓ `rescission-sale` (contract-rescission) [development] 29s
- ✗ `contract-damages` (contract-damages) [development] 33s · plaintiff position (lessee instead of buyer|client|creditor|other)
- ✓ `contract-damages-2` (contract-damages) [development] 27s
- ✓ `tort-assault` (tort-damages) [development] 16s
- ✓ `tort-damage-property` (tort-damages) [development] 14s
- ✓ `tort-defamation` (tort-damages) [development] 13s
- ✓ `things-water` (liability-for-things) [development] 15s
- ✓ `things-dog` (liability-for-things) [development] 16s
- ✓ `things-wall` (liability-for-things) [development] 16s
- ✗ `rent-basic` (unpaid-rent) [development] 22s · missing decisive event (notice_served)
- ✓ `rent-darija` (unpaid-rent) [robustness] 20s
- ✓ `occupation` (eviction-occupier-without-title) [development] 15s
- ✓ `occupation-flat` (eviction-occupier-without-title) [development] 14s
- ✓ `commercial-invoices` (commercial-debt) [development] 21s
- ✓ `commercial-supplies` (commercial-debt) [development] 20s
- ✓ `commercial-lease` (commercial-lease-eviction) [development] 24s
- ✓ `commercial-lease-indemnity` (commercial-lease-eviction) [development] 22s
- ✗ `divorce-husband` (divorce-by-husband) [development] 11s · missing decisive event (marriage)
- ✓ `tatliq-abandonment` (tatliq) [development] 19s
- ✓ `tatliq-harm` (tatliq) [development] 19s
- ✓ `khul` (khul) [development] 13s
- ✓ `khul-2` (khul) [development] 12s
- ✓ `nafaqa-children` (nafaqa) [development] 18s
- ✓ `nafaqa-wife` (nafaqa) [development] 16s
- ✓ `custody` (custody) [development] 19s
- ✓ `custody-transfer` (custody) [development] 18s
- ✓ `visitation` (visitation) [development] 15s
- ✓ `dismissal` (abusive-dismissal) [development] 21s
- ✓ `dismissal-grave-fault` (abusive-dismissal) [development] 17s
- ✓ `wages` (unpaid-wages) [development] 18s
- ✓ `wages-overtime` (unpaid-wages) [development] 17s
- ✓ `loan-with-section-mismatch-words` (loan-repayment) [development] 23s
- ✓ `rent-shop-civil-words` (commercial-lease-eviction) [development] 22s
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✓ `annulment-fraud-land` (sale-annulment-fraud) [development] 23s
- ✓ `non-delivery-machines` (sale-non-delivery) [development] 26s
- ✓ `lease-start-vs-signing` (unpaid-rent) [validation] 34s
- ✓ `lease-date-in-documents` (unpaid-rent) [validation] 22s
- ✗ `rent-no-notice` (unpaid-rent) [validation] 21s · polarity (notice_served: disputed instead of denied)
- ✗ failed `tenant-plaintiff` [validation] 73s · ollama gemma3:4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✓ `assault-no-animal` (tort-damages) [validation] 19s
- – `borrower-plaintiff` (out-of-scope) [validation] 0s
- ✗ `disputed-loan` (loan-repayment) [validation] 30s · polarity (money_lent: affirmed instead of disputed)
- ✓ `amount-in-requests` (unpaid-sale-price) [validation] 27s
- ✓ `loan-due-vs-notice` (loan-repayment) [validation] 32s
- ✓ `commercial-lease-1998` (commercial-lease-eviction) [validation] 28s
- ✓ `tort-no-conviction` (tort-damages) [validation] 17s
- ✓ `dismissal-no-conciliation` (abusive-dismissal) [validation] 19s
- ✓ `loan-family-section` (loan-repayment) [validation] 23s
- ✓ `buyer-plaintiff` (sale-non-delivery) [validation] 25s
- ✓ `rent-monthly-vs-total` (unpaid-rent) [validation] 29s

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 56 | 0.0 | 735 | 45.5 | 44 | 6.8 | 16.3 | 0 |


