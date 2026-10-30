# Grounds analysis bench

## ollama gemma3:4b @ 127.0.0.1:11434

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 98 % | 98 % | 78 % | 66 % | 2 % | 100 % | 0 % | 3.0 | 0 | 0 | 121 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 98 % | 77 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 2/2 core · 0 off-claim · 139s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 176s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 172s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 123s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 169s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 146s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 157s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 112s
- ✓ `contract-damages-2` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 142s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 108s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 138s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 143s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 104s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 131s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 142s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 123s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 97s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 126s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 97s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 133s
- ✓ `commercial-lease` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 101s
- ✓ `commercial-lease-indemnity` expected commercial-lease-eviction → commercial-lease-eviction · 1/2 core · 0 off-claim · 128s
- ✓ `divorce-husband` expected divorce-by-husband → divorce-by-husband · 1/1 core · 0 off-claim · 112s
- ✓ `tatliq-abandonment` expected tatliq → tatliq · 0/1 core · 0 off-claim · 130s
- ✓ `tatliq-harm` expected tatliq → tatliq · 1/1 core · 0 off-claim · 168s
- ✓ `khul` expected khul → khul · 1/1 core · 0 off-claim · 127s
- ✓ `khul-2` expected khul → khul · 1/1 core · 0 off-claim · 160s
- ✓ `nafaqa-children` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 110s
- ✓ `nafaqa-wife` expected nafaqa → nafaqa · 1/2 core · 0 off-claim · 149s
- ✓ `custody` expected custody → custody · 2/2 core · 0 off-claim · 126s
- ✓ `custody-transfer` expected custody-forfeiture → custody-forfeiture · 0/2 core · 0 off-claim · 158s
- ✓ `visitation` expected visitation → visitation · 1/1 core · 0 off-claim · 100s
- ✓ `dismissal` expected abusive-dismissal → abusive-dismissal · 0/1 core · 0 off-claim · 86s
- ✓ `dismissal-grave-fault` expected abusive-dismissal → abusive-dismissal · 1/1 core · 0 off-claim · 136s
- ✓ `wages` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 83s
- ✓ `wages-overtime` expected unpaid-wages → unpaid-wages · 2/2 core · 0 off-claim · 123s
- ✓ `loan-with-section-mismatch-words` expected loan-repayment → loan-repayment · 2/2 core · 0 off-claim · 125s
- ✓ `rent-shop-civil-words` expected commercial-lease-eviction → commercial-lease-eviction · 2/2 core · 0 off-claim · 91s
- ✓ `none-inheritance` expected none → — · 0/0 core · 0 off-claim · 59s
- ✓ `none-administrative` expected none → — · 0/0 core · 0 off-claim · 0s
- ✓ `none-commercial-company` expected none → — · 0/0 core · 0 off-claim · 55s
- ✓ `none-civil-status` expected none → — · 0/0 core · 0 off-claim · 93s
- ✓ `annulment-fraud-land` expected sale-annulment-fraud → sale-annulment-fraud · 2/2 core · 0 off-claim · 119s
- ✓ `non-delivery-machines` expected sale-non-delivery → sale-non-delivery · 2/2 core · 0 off-claim · 117s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 75 % | 0 | 144 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 140s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 149s

### Where the time goes (gemma3:4b)

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds.reading | 45 | 0.0 | 1375 | 23.5 | 131 | 6.0 | 58.6 | 0 |
| grounds.selection | 42 | 0.0 | 778 | 24.1 | 97 | 6.3 | 32.6 | 0 |

