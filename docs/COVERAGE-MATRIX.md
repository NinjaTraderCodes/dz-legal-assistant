# Coverage matrix (started 26 October 2026)

What the app can honestly claim to support, claim by claim, and what is still missing. This is the plan's §4.2 coverage matrix (`docs/CORE-REBUILD-PLAN.md`). It is a working document: each row moves to "supported" only when every column has evidence.

## Status levels

1. **Engineering:** in the catalogue and in the engineering test cases. The articles are from the old 81-article library. The rules are not yet written as conditions.
2. **Rules:** written as an executable cause of action (conditions, exceptions, prerequisites, remedies, time rules), each tracing to a quoted clause.
3. **Supported:** rules, plus checked current text from the corpus, plus independent test cases (decision-based) passing in the validation partition.

**Today every row is level 1.** No row is level 2 or 3 yet.

## Claims in the catalogue (20)

| Claim type | Area | Remedies the claim covers today | Authorities in the library | Known gaps and missing distinctions | Prerequisites and deadlines not modelled yet | Test cases (dev / independent) |
|---|---|---|---|---|---|---|
| loan-repayment | civil | repayment | C 450, 106, 107, 323, 324 bis 5, 333, 454, 179, 186, 182 | prescription (C 308/309 absent); a debt acknowledgment is mistaken for a sale (local miss, `loan-acknowledgment`) | formal notice (C 180, absent) before damages | 5 / 0 |
| unpaid-sale-price | civil | payment of the price | C 351, 106, 164, 323, 179, 186 | prescription (C 308/309); boundary with commercial debt | formal notice | 2 / 0 |
| contract-rescission | civil | rescission, damages | C 119, 122, 107, 176, 182 | none known yet | **formal notice is a condition of art. 119** (C 180 absent) | 2 / 0 |
| sale-annulment-fraud | civil | annulment, restitution | C 86, 103, CPCA 17, C 324 bis 1 | limitation of the annulment action (article to identify); hidden defects are a different branch (C 379 absent) | publication of a claim on registered land (CPCA 17) | 1 / 0 |
| contract-damages | civil | damages | C 176, 179, 182, 107, 106 | none known yet | formal notice (C 180) | 2 / 0 |
| tort-damages | civil | damages | C 124, 131, 182 bis, 182 | prescription of the tort action (article to identify); special regimes are out of scope (traffic 74-15, medical, public bodies) | none | 3 / 0 |
| liability-for-things | civil | damages | C 138, 131, 182 | **must split:** things (138), animals (139, absent), fire and buildings (140, absent) | none | 3 / 0 |
| unpaid-rent | civil | payment, termination | C 498, 467, 106, 467 bis, 324 bis 1, 119 | the regime depends on the lease date (2007 amendments; older leases); boundary with commercial lease | formal notice | 2 / 0 |
| eviction-occupier-without-title | civil | eviction | C 674, 124 | the title and possession facts are not distinguished | none | 2 / 0 |
| commercial-debt | commercial | payment | Com 30, C 106, 164, 176, 179 | the merchant status test is not modelled; supplies are mistaken for a sale (local miss) | formal notice | 2 / 0 |
| commercial-lease-eviction | commercial | eviction, eviction indemnity | Com 173, 176, 187 bis | **must split:** arrears, expiry, eviction, indemnity; the regime depends on the lease date (Com 187 bis 1, absent) | notice to vacate (form and time limit) | 3 / 0 |
| sale-non-delivery | civil / commercial | delivery, rescission, damages | C 364, 367, 164, 174, 119, 122, 179, Com 30 | delivery, rescission and damages are different remedies with different conditions | formal notice | 1 / 0 |
| divorce-by-husband | family | divorce, wife's indemnity | F 48, 49 | the wife's indemnity for abusive divorce (F 52) is in the library but cannot be proposed (audit, probe D) | conciliation attempts (F 49) | 1 / 0 |
| tatliq | family | judicial divorce at the wife's request | F 53, 53 bis, 49 | the harm ground sits beyond the 180-character excerpt (audit, probe C) | conciliation attempts (F 49) | 2 / 0 |
| khul | family | khul' | F 54, 49 | none known yet | conciliation attempts | 2 / 0 |
| nafaqa | family | maintenance | F 74, 75, 78, 79, 80 | **must split:** the wife's maintenance and the children's maintenance | none | 2 / 0 |
| custody | family | custody, transfer of custody | F 62, 64, 65, 72 | none known yet | none | 2 / 0 |
| visitation | family | visiting rights | F 64 | none known yet | none | 1 / 0 |
| abusive-dismissal | social | compensation, reinstatement | L 90-11 73, 73-4; L 90-04 19 | none known yet | **conciliation before the bureau (90-04), and the six-month filing time limit from the non-conciliation report (CPCA 504, absent)** | 2 / 0 |
| unpaid-wages | social | payment | L 90-11 80, 88; L 90-04 19 | none known yet | conciliation (90-04); CPCA 504 time limit | 2 / 0 |

C = Civil Code, Com = Commercial Code, F = Family Code, L = labour laws.

The development counts are the engineering cases in `tests/grounds-cases/cases.json`. No independent (decision-based) case exists yet.

## Matters outside the catalogue (the app should decline them)

- inheritance;
- administrative matters;
- company law;
- civil status;
- road accidents (74-15, 88-31);
- insurance;
- medical liability;
- registration of real property;
- social-security disputes (08-08);
- collective labour disputes.

The engineering cases test four of these (inheritance, administrative, company law, civil status). All four were declined in the 26 October baseline.

## Next updates

- **When the corpus delivers its priority articles:** add C 139, 140, 308, 309, 379, CPCA 504 and Com 187 bis 1, and move the first modules to level 2: debts and roles; animal and building liability; the labour time limit.
- **When independent cases exist:** fill the "independent" column, by partition.
