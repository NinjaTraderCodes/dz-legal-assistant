# Family claims: what the official texts confirm (30 Oct 2026)

Legal answers on family procedure were received on 29 Oct 2026. They are **input, not verification**. Every article they cite was checked against the official texts:
- the **complete Family Code** and the **complete CPCA** of the law corpus (`docs/LAW-CORPUS.md`);
- in the version in force, as loaded in the app (`docs/LAW-LIBRARY.md`).

**Rule:** where a point and the official text differ, the official text decides. Where the official texts settle nothing, the point is left out and recorded below as unsettled. No question is sent to anyone.

## The articles checked

| Article | Point received | Official text | Verdict |
|---|---|---|---|
| Family 3 bis | The public prosecutor is a principal party in family cases | «تعد النيابة العامة طرفا أصليا في جميع القضايا الرامية إلى تطبيق أحكام هذا القانون» | Matches |
| Family 57 | Custody judgments can be appealed | Yes. Divorce, tatliq and khul judgments cannot, except for their financial aspects | Matches |
| Family 62 | Conditions for the custodian | The custodian must be fit to care for, educate and protect the child | Matches |
| Family 64 | Visitation follows custody; the child's interest | Order of custodians, in the child's interest; the judge who awards custody rules on visitation | Matches |
| Family 66 | Custody is lost when the custodian marries a non-mahram | The same, and also by renouncing it, unless that harms the child | Matches |
| Family 67 | Custody is lost when a condition fails | The same. **A woman's work is not a reason to lose custody** | Matches |
| Family 68 | Custody is lost after more than a year without claiming it | The same, «بدون عذر» | Matches |
| Family 69 | Moving abroad | The judge decides whether to keep or remove custody | Matches |
| Family 72 | Housing for the custodian, or rent | The same. The custodian stays in the marital home until the housing judgment is carried out | Matches |
| Family 87 | Guardianship goes with custody | «في حالة الطلاق، يمنح القاضي الولاية لمن أسندت له حضانة الأولاد» | Matches |
| CPCA 13 | Standing | Capacity and interest; the judge raises their absence on his own motion | Matches |
| CPCA 15 | Content of the petition | The 6 mandatory items | Matches |
| CPCA 17 | Court fees | The petition is registered only once the fees are paid | Matches |
| CPCA 426 | Territorial jurisdiction | Divorce: the marital home. Custody and visitation: where custody is exercised. Nafaqa: the creditor's home. Guardianship: where it is exercised | Matches |
| CPCA 438 | Serve the defendant and the prosecutor | In a **divorce suit**, the petition is served on the defendant and on the public prosecutor | Narrower: it is certain only for divorce suits |
| CPCA 451 | Cited for the documents proving a tatliq, and for the khul compensation | The judge examines and qualifies the facts of a tatliq or khul request, and may order an inquiry or a medical expert report | Does not support that use. **The khul compensation rests on Family Code art. 54** |
| CPCA 600 | (supports) A visitation judgment is enforced, not sued for again | Final judgments and summary orders are enforceable titles | Supports the point |
| CPCA 323 | (found while checking) | Provisional execution is ordered, on request, in nafaqa and for the custodian's housing | Applied |

## What changed in the claim catalogue (`data/law/claims/family.json`)

- **Custody is split in two claim types:**
  - `custody`, awarding custody: arts. 62, 64, 65, 72 and 87. It now also suggests guardianship and provisional execution for housing.
  - `custody-forfeiture`, losing or transferring custody: a separate suit, based on an event after the judgment (arts. 66–69, 62, 64, 87). Art. 67 says that a woman's work is not a reason.
- **Visitation:**
  - the claim type covers fixing or changing visitation;
  - CPCA 600 is a required "limit" article. So whenever visitation is confirmed, the lawyer sees that refusing an existing judgment is a matter of enforcement, not a new suit.
- **Every family claim cites Family 3 bis** (the prosecutor) and **CPCA 426** (the court with territorial jurisdiction).
- **Divorce by the husband cites CPCA 438.**
- **Tatliq and khul cite CPCA 451.** Khul keeps art. 54 for the compensation.
- **Nafaqa cites CPCA 323** and suggests provisional execution.
- **Usual documents** were added for each family claim. They are documents usually attached, not legal requirements: no article requires them beyond CPCA 15 ("when appropriate").
- **In the reader** (`data/law/claim-elements.json`, `FactVocabulary`):
  - new facts: a prior custody judgment, the custodian's remarriage, renunciation, unfitness, not claiming custody, settling abroad;
  - new requests: guardianship and provisional execution;
  - test case `custody-transfer` now belongs to `custody-forfeiture`, in both answer keys.

## Unsettled, left out

- **A criminal complaint for refusing to hand over a child.** It is in the Penal Code, which the library does not hold.
- **Whether CPCA 438 also covers tatliq and khul.** The CPCA places them in the same divorce provisions (451), but 438 says "divorce suit". The prosecutor is still cited in those claims, through Family 3 bis.
- **Lists of documents beyond what the law requires.** Several are administrative practice («ممارسة إدارية»). They are shown as usual documents.
- **Checking the court chosen against CPCA 426, and stating the prosecutor in the petition itself.** Both need a change to the form definition, which `AGENTS.md` protects. They are proposed separately, and not applied.
