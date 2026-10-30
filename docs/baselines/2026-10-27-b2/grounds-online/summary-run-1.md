## google/gemma-3-4b-it (OpenRouter)

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 91 % | 91 % | 70 % | 66 % | 7 % | 75 % | 0 % | 2.7 | 0 | 0 | 11 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 91 % | 69 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 12s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 10s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 17s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 10s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 10s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 10s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 9s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 11s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 1/2 core · 0 off-claim · 10s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 11s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 10s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 10s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 10s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 13s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 10s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 11s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 10s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 14s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 11s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 10s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 10s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 10s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 10s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 12s
- ✓ `tatliq-harm` expected tatliq → tatliq · 0/1 core · 0 off-claim · 10s
- ✗ `khul` expected khul → tatliq · 0/1 core · 1 off-claim · 9s
- ✗ `khul-2` expected khul → tatliq · 0/1 core · 0 off-claim · 10s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 10s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 10s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 12s
- ✓ `custody-transfer` expected custody → custody · 1/2 core · 0 off-claim · 10s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 12s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 10s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 10s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 10s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 10s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 10s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 10s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 17s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✗ `none-commercial-company` expected none → contract-rescission · 0/0 core · 4 off-claim · 21s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 5s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 11s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 10s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 75 % | 0 | 11 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 10s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 11s

