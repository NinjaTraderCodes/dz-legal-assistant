## google/gemma-3-4b-it (OpenRouter)

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 96 % | 96 % | 75 % | 66 % | 2 % | 75 % | 2 % | 2.7 | 1 | 0 | 8 |

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 9s
- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 8s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 9s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 11s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 9s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 8s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 9s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 8s
- ✓ `contract-damages` expected contract-damages → contract-damages · 1/2 core · 0 off-claim · 8s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 1/2 core · 0 off-claim · 8s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 11s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 8s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 8s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 9s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 8s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 9s
- ✗ `rent-basic` expected unpaid-rent → — · 0/2 core · 0 off-claim · 4s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 9s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 9s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 8s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 9s
- ✓ `commercial-supplies` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 8s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 9s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 8s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 9s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 9s
- ✓ `tatliq-harm` expected tatliq → tatliq · 1/1 core · 0 off-claim · 10s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 8s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 8s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 9s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 8s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 12s
- ✓ `custody-transfer` expected custody → custody · 2/2 core · 0 off-claim · 10s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 10s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 9s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 8s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 9s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 11s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 8s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 10s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 4s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✗ `none-commercial-company` expected none → commercial-debt · 0/0 core · 3 off-claim · 8s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 6s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 8s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 10s

