## google/gemma-3-4b-it (OpenRouter)

**Stopped early after 23 of 44 cases**: the engine failed several cases in a row. The figures below cover only those cases.

| claim exact | claim found | core recall | model recall | off-claim share | out-of-scope abstained | false abstention | articles / case | empty (should not be) | failures | seconds / case |
|---|---|---|---|---|---|---|---|---|---|---|
| 78 % | 78 % | 59 % | 54 % | 7 % | n/a | 0 % | 3.0 | 3 | 3 | 11 |

Legal reference (the authorities the law requires, which may differ from the catalog's answer):

| claim acceptable | legal recall (authorities in the library) | required but not in the library | excluded authorities proposed |
|---|---|---|---|
| 78 % | 57 % | 2 | 0 |

- Coverage gap: `civil-code:139` (needed by things-dog)
- Coverage gap: `civil-code:140` (needed by things-wall)

- ✓ `loan-basic` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 15s
- ✓ `loan-acknowledgment` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 10s
- ✓ `loan-no-section` expected loan-repayment → loan-repayment · 1/2 core · 0 off-claim · 12s
- ✓ `sale-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 11s
- ✓ `sale-land-price` expected unpaid-sale-price → unpaid-sale-price · 1/2 core · 0 off-claim · 12s
- ✓ `rescission` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 10s
- ✓ `rescission-sale` expected contract-rescission → contract-rescission · 2/2 core · 0 off-claim · 12s
- ✓ `contract-damages` expected contract-damages → contract-damages · 2/2 core · 0 off-claim · 10s
- ✗ `contract-damages-2` expected contract-damages → tort-damages · 0/2 core · 2 off-claim · 11s
- ✓ `tort-assault` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 12s
- ✓ `tort-damage-property` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 12s
- ✓ `tort-defamation` expected tort-damages → tort-damages · 1/1 core · 0 off-claim · 11s
- ✓ `things-water` expected liability-for-things → liability-for-things · 1/1 core · 0 off-claim · 10s
- ✓ `things-dog` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 10s
- ✓ `things-wall` expected liability-for-things → liability-for-things · 1/1 core · legal 0/0 (+1 not in library) · 0 off-claim · 11s
- ✓ `rent-basic` expected unpaid-rent → unpaid-rent · 2/2 core · 0 off-claim · 13s
- ✓ `occupation` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 10s
- ✓ `occupation-flat` expected eviction-occupier-without-title → eviction-occupier-without-title · 1/1 core · 0 off-claim · 10s
- ✓ `commercial-invoices` expected commercial-debt → commercial-debt · 1/2 core · 0 off-claim · 10s
- ✗ `commercial-supplies` expected commercial-debt → unpaid-sale-price · 0/2 core · 3 off-claim · 11s
- ✗ `commercial-lease` expected commercial-lease-eviction → — · 0/2 core · 0 off-claim · 9s
- ✗ `commercial-lease-indemnity` expected commercial-lease-eviction → — · 0/2 core · 0 off-claim · 8s
- ✗ `divorce-husband` expected divorce-by-husband → — · 0/1 core · 0 off-claim · 3s

### Robustness: facts in Darija or French (not counted above)

| cases | claim exact | core recall | failures | seconds / case |
|---|---|---|---|---|
| 2 | 100 % | 75 % | 0 | 10 |

- ✓ `loan-darija` expected loan-repayment → loan-repayment · 1/2 core · 10s
- ✓ `rent-darija` expected unpaid-rent → unpaid-rent · 2/2 core · 10s

