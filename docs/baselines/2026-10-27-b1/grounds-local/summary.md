# Grounds analysis bench

## ollama gemma3:4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 76 % | 76 % | 60 % | 54 % | 23 % | 75 % | 2 % | 2.6 | 1 | 0 | 80 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 76 % | 61 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 193s
- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 118s
- ✗ `loan-acknowledgment` expected loan-repayment → unpaid-sale-price · 0/2 core · 1 off-claim · 82s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 95s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 85s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 83s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 94s
- ✗ `rescission-sale` expected contract-rescission → sale-annulment-fraud · 0/2 core · 4 off-claim · 82s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 86s
- ✗ `contract-damages-2` expected contract-damages → tort-damages · 0/2 core · 2 off-claim · 84s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 83s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 94s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 79s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 79s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 72s
- ✗ `things-wall` expected liability-for-things → tort-damages · 0/1 core · legal 0/0 (+1 not in library) · 2 off-claim · 88s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 85s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 96s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 63s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 67s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 83s
- ✗ `commercial-supplies` expected commercial-debt → sale-non-delivery · 1/2 core · 3 off-claim · 104s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 83s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 80s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 61s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 70s
- ✗ `tatliq-harm` expected tatliq → nafaqa · 0/1 core · 3 off-claim · 76s
- ✗ `khul` expected khul → loan-repayment · 0/1 core · 5 off-claim · 108s
- ✗ `khul-2` expected khul → nafaqa · 0/1 core · 3 off-claim · 88s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 93s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 74s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 92s
- ✓ `custody-transfer` expected custody → custody · 2/2 core · 0 off-claim · 88s
- ✗ `visitation` expected visitation → custody · 1/1 core · 2 off-claim · 81s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 86s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 82s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 1/2 core · 0 off-claim · 73s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 69s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 98s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 71s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 42s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✗ `none-commercial-company` expected none → contract-rescission · 0/0 core · 3 off-claim · 65s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 23s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 64s
- ✗ `non-delivery-machines` expected sale-non-delivery → — · 0/2 core · 0 off-claim · 28s

### Where the time goes (gemma3:4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 45 | 0.3 | 1701 | 77.1 | 138 | 6.2 | 22.4 | 0 |
| grounds.selection | 42 | 0.0 | 2419 | 90.1 | 79 | 6.1 | 26.9 | 0 |

