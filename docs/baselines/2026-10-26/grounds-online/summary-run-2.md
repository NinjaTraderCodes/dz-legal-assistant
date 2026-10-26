## google/gemma-3-4b-it (OpenRouter)

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 100 % | 100 % | 78 % | 69 % | 0 % | 100 % | 0 % | 2.7 | 0 | 0 | 8 |

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 11s
- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 10s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 8s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 9s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 8s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 8s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 10s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 10s
- ✓ `contract-damages` expected contract-damages → contract-damages · 1/2 core · 0 off-claim · 9s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 9s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 9s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 8s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 9s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 11s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 9s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 8s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 8s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 10s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 10s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 12s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 9s
- ✓ `commercial-supplies` expected commercial-debt → commercial-debt · 0/2 core · 0 off-claim · 9s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 9s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 9s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 9s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 8s
- ✓ `tatliq-harm` expected tatliq → tatliq · 1/1 core · 0 off-claim · 9s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 8s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 8s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 8s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 9s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 10s
- ✓ `custody-transfer` expected custody → custody · 2/2 core · 0 off-claim · 9s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 9s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 8s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 8s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 8s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 8s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 8s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 9s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 5s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✓ `none-commercial-company` expected none → — · 0/0 core · 0 off-claim · 5s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 5s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 10s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 9s

