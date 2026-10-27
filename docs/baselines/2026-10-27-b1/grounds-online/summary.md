# Grounds analysis bench

## Stability over 3 runs — openrouter

Claim agreement: share of runs giving the most common claim type. Article agreement: mean overlap (Jaccard) of the proposed sets between runs; 100% = identical every time. Model: the articles the model chose itself.

| case | most common claim | claim agreement | claim exact | article agreement | model article agreement | model recall | failures |
|---|---|---:|---:|---:|---:|---:|---:|
| loan-basic | loan-repayment | 100 % | 100 % | 69 % | 63 % | 50 % | 0 |
| loan-darija | loan-repayment | 100 % | 100 % | 78 % | 51 % | 33 % | 0 |
| loan-acknowledgment | loan-repayment | 100 % | 100 % | 89 % | 70 % | 17 % | 0 |
| loan-no-section | loan-repayment | 100 % | 100 % | 89 % | 83 % | 0 % | 0 |
| sale-price | unpaid-sale-price | 100 % | 100 % | 83 % | 67 % | 50 % | 0 |
| sale-land-price | unpaid-sale-price | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| rescission | contract-rescission | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| rescission-sale | sale-annulment-fraud | 100 % | 0 % | 100 % | 100 % | 0 % | 0 |
| contract-damages | contract-damages | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| contract-damages-2 | contract-damages | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| tort-assault | tort-damages | 100 % | 100 % | 83 % | 83 % | 100 % | 0 |
| tort-damage-property | tort-damages | 100 % | 100 % | 83 % | 83 % | 100 % | 0 |
| tort-defamation | tort-damages | 100 % | 100 % | 83 % | 83 % | 100 % | 0 |
| things-water | liability-for-things | 100 % | 100 % | 78 % | 78 % | 100 % | 0 |
| things-dog | liability-for-things | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| things-wall | tort-damages | 100 % | 0 % | 100 % | 100 % | 0 % | 0 |
| rent-basic | unpaid-rent | 100 % | 100 % | 83 % | 67 % | 83 % | 0 |
| rent-darija | unpaid-rent | 100 % | 100 % | 83 % | 67 % | 83 % | 0 |
| occupation | eviction-occupier-without-title | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| occupation-flat | eviction-occupier-without-title | 100 % | 100 % | 67 % | 67 % | 100 % | 0 |
| commercial-invoices | commercial-debt | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| commercial-supplies | commercial-debt | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| commercial-lease | commercial-lease-eviction | 100 % | 100 % | 78 % | 78 % | 67 % | 0 |
| commercial-lease-indemnity | commercial-lease-eviction | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| divorce-husband | divorce-by-husband | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| tatliq-abandonment | tatliq | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| tatliq-harm | tatliq | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| khul | khul | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| khul-2 | khul | 67 % | 67 % | 33 % | 33 % | 67 % | 0 |
| nafaqa-children | nafaqa | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| nafaqa-wife | nafaqa | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| custody | custody | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| custody-transfer | custody | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| visitation | visitation | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| dismissal | abusive-dismissal | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| dismissal-grave-fault | abusive-dismissal | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| wages | unpaid-wages | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| wages-overtime | unpaid-wages | 100 % | 100 % | 100 % | 100 % | 100 % | 0 |
| loan-with-section-mismatch-words | loan-repayment | 100 % | 100 % | 78 % | 73 % | 50 % | 0 |
| rent-shop-civil-words | commercial-lease-eviction | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| none-inheritance | — | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| none-administrative | — | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| none-commercial-company | commercial-debt | 67 % | 0 % | 44 % | 44 % | 0 % | 0 |
| none-civil-status | — | 100 % | 100 % | 100 % | 100 % | 0 % | 0 |
| annulment-fraud-land | sale-annulment-fraud | 100 % | 100 % | 100 % | 100 % | 50 % | 0 |
| non-delivery-machines | sale-non-delivery | 100 % | 100 % | 76 % | 60 % | 67 % | 0 |
| **all** | | 99 % | 93 % | 91 % | 88 % | 56 % | 0 |

### Where the time goes (openrouter)

No timings reported (template mode, or a server that does not report them).

