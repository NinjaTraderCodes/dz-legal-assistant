# Fact reading bench: Alpha-01, claim type read by the analysis (local gemma3:4b)

The run's own summary was not written (the run stopped after its last case). This table is rebuilt from its 61 case files, which are complete. No time per case: those files do not record it; the first cases took 108–129 s, the analysis's claim reading being most of it.

| partition | cases | fully right | with a critical error |
|---|---:|---:|---:|
| development | 44 | 38 | 6 |
| validation | 15 | 9 | 6 |
| robustness | 2 | 2 | 0 |

Claim type read by the analysis: 55 of 61 right (90 %). Of the 5 cases outside the catalogue, the analysis declined 4.

Wrong claim type (6 cases), which the lawyer's confirmation in the app is there to catch:
- `loan-acknowledgment` [development]: unpaid-sale-price instead of loan-repayment
- `commercial-supplies` [development]: unpaid-sale-price instead of commercial-debt
- `assault-no-animal` [validation]: liability-for-things instead of tort-damages
- `loan-family-section` [validation]: nafaqa instead of loan-repayment (the family court section misled it)
- `tenant-plaintiff` [validation]: contract-rescission instead of contract-damages
- `borrower-plaintiff` [validation]: loan-repayment proposed for a case outside the catalogue

Errors with the right claim type (6 cases), the same kinds as with the claim type given:
- `contract-damages` [development]: plaintiff position lessee instead of buyer
- `divorce-husband` [development]: marriage missing
- `loan-basic` [development]: notice missing
- `rent-basic` [development]: notice missing
- `rent-no-notice` [validation]: notice disputed instead of denied
- `disputed-loan` [validation]: loan affirmed instead of disputed
