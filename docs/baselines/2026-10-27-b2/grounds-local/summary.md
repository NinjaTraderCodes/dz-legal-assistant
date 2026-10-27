# Grounds analysis bench

## ollama gemma3:4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 89 % | 89 % | 70 % | 66 % | 14 % | 50 % | 0 % | 2.5 | 0 | 0 | 103 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 89 % | 69 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 148s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 175s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 143s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 137s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 110s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 95s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 102s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 139s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 124s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 97s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 105s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 106s
- ✗ `things-water` expected liability-for-things → tort-damages · 0/1 core · 1 off-claim · 102s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 98s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 93s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 123s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 115s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 89s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 0/2 core · 0 off-claim · 150s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 103s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 0/2 core · 0 off-claim · 102s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 81s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 97s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 1/1 core · 0 off-claim · 74s
- ✓ `tatliq-harm` expected tatliq → tatliq · 1/1 core · 0 off-claim · 77s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 82s
- ✗ `khul-2` expected khul → nafaqa · 0/1 core · 3 off-claim · 93s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 2/2 core · 0 off-claim · 95s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 90s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 105s
- ✓ `custody-transfer` expected custody → custody · 2/2 core · 0 off-claim · 91s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 83s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 88s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 85s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 83s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 72s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 2/2 core · 0 off-claim · 124s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 0/2 core · 0 off-claim · 95s
- ✗ `none-inheritance` expected none → nafaqa · 0/0 core · 4 off-claim · 178s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✗ `none-commercial-company` expected none → commercial-debt · 0/0 core · 4 off-claim · 107s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 45s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 106s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 110s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 100 % | 0 | 130 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 2/2 core · 153s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 108s

### Where the time goes (gemma3:4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 46 | 0.3 | 1296 | 38.1 | 141 | 5.4 | 34.8 | 0 |
| grounds.selection | 44 | 0.0 | 2010 | 65.2 | 74 | 5.3 | 30.9 | 0 |

