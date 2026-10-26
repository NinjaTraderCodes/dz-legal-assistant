# Grounds analysis bench

## ollama gemma3:4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 96 % | 96 % | 75 % | 66 % | 3 % | 100 % | 0 % | 2.7 | 0 | 0 | 113 |

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 2/2 core · 0 off-claim · 155s
- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 131s
- ✗ `loan-acknowledgment` expected loan-repayment → unpaid-sale-price · 0/2 core · 1 off-claim · 139s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 152s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 120s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 127s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 118s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 166s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 128s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 1/2 core · 0 off-claim · 174s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 144s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 142s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 120s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 99s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 116s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 116s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 123s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 132s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 88s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 105s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 117s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 133s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 91s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 113s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 81s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 82s
- ✓ `tatliq-harm` expected tatliq → tatliq · 1/1 core · 0 off-claim · 116s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 82s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 112s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 96s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 119s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 91s
- ✓ `custody-transfer` expected custody → custody · 2/2 core · 0 off-claim · 131s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 78s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 94s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 123s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 82s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 117s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 145s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 94s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 58s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✓ `none-commercial-company` expected none → — · 0/0 core · 0 off-claim · 59s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 96s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 160s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 152s

### Where the time goes (gemma3:4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 45 | 0.4 | 1269 | 23.8 | 126 | 5.4 | 53.9 | 0 |
| grounds.selection | 42 | 0.0 | 663 | 24.4 | 84 | 6.0 | 27.5 | 0 |

