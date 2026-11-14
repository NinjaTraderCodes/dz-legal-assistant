# The label scheme of lawyer notes

**Version: 1.0. Status: written on 14 November 2026, not yet frozen.** The executor freezes it in phase A, step 1 of the handbook, when the guide test has no unexplained item and the cells table of section 8 is filled: **the freeze commit** holds everything; **a second, stamp commit** then changes only this line to `frozen at <the freeze commit's id>` and fills section 12. A note's `schemeVersion` is `1.0@<the freeze commit's id>`. From that commit on, the rules of [../specs/scheme-extension.md](../specs/scheme-extension.md) apply: **the scheme only grows**.

**What this file is.** The complete list of what a note is labelled with, and therefore of what the reader outputs. The owner decided on 14 November 2026 that the reader comes first, so the scheme is frozen **from what exists today** (the app's claim catalogue, its 75 elements, its vocabulary, the 8 slots already read from the note), not from final rule records. The rules add slots later; section 10 says where they go.

**What decides a label** is the labelling guide ([LABELLING-GUIDE.md](LABELLING-GUIDE.md)); this file decides **which labels exist**. Where they differ, the guide is fixed, its version bumped, and the guide test rerun.

The frozen inputs, with the hash the executor records here at the freeze:

| File | What the scheme takes from it | SHA-256 at the freeze |
|---|---|---|
| `data/law/claim-elements.json` | the 21 claim types, each one's roles, its elements (event kind, usual actor, Arabic proposition, whether it takes a date and an amount), its requests | *(filled at the freeze)* |
| `src/DzLegal.Drafting/CaseFacts.cs`, `FactVocabulary` | the relations, the positions, the request kinds, the actors | *(filled at the freeze)* |
| `docs/research/core-logic/slots.json` | the eight slots of section 5, by their ids, types and choices as that section lists them (the file itself grows with every rule record, so its hash is not recorded) | – |
| `notes/LABELLING-GUIDE.md` | the rules, the 36 hard cases, the tables of section 5 | *(filled at the freeze; a later version of the guide is recorded with its hash in section 12)* |

Both files outside `docs/research/core-logic/` are read, never changed. If one of them changes on the default branch after the freeze, the scheme keeps the frozen copy's content, and the difference is reported to the owner.

## 1. The outputs, in one table

| # | Output | Values | Counted |
|---|---|---|---|
| 1 | **Claim type** | up to three of the 21 ids, or `outside` | 22 classes. (`outsideKind`, one of 10 kinds, is a label of the notes only: it is used to draw near-misses and to report by kind; the reader does not output it and it is not compared) |
| 2 | **The plaintiff's role**, per claim type | one of the roles the claim type allows, or `unknown` | 7 claim types offer a choice |
| 3 | **Elements**, per claim type | for each element: polarity `stated`, `denied`, `disputed` or `notMentioned`; the actor; who disputes; its date and its amount where the element takes one | 75 elements, 41 event kinds |
| 4 | **Slots read from the note** | per slot: `yes`, `no`, `unknown`; a choice id; or a date | 8 slots |
| 5 | **Requests** | the kinds the note asks for, each with its amount when stated | 24 kinds |
| 6 | **Dates and amounts** | every one written in the note, normalised, with what it is for | roles: event kinds, request kinds, slot ids |
| 7 | **The words behind every answer** | quotes of the note (field, start, length) | on every label except `notMentioned` and `unknown` |

A judge may also answer `unclear`, and a training note may carry `blanked`; neither is a label the reader outputs ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), section 5).

## 2. Claim types

The 21 ids: `loan-repayment`, `unpaid-sale-price`, `contract-rescission`, `sale-annulment-fraud`, `contract-damages`, `tort-damages`, `liability-for-things`, `unpaid-rent`, `eviction-occupier-without-title`, `commercial-debt`, `commercial-lease-eviction`, `sale-non-delivery`, `divorce-by-husband`, `tatliq`, `khul`, `nafaqa`, `custody`, `custody-forfeiture`, `visitation`, `abusive-dismissal`, `unpaid-wages`.

`outside`, never combined with an id, with `outsideKind` one of: `administrative`, `criminal`, `family-other`, `civil-status`, `property-other`, `payment-order`, `urgent`, `enforcement`, `company`, `other` (the guide, section 3, defines each).

A note has one to three claim types (the guide, section 3: each needs its own request).

## 3. The plaintiff's role (the "party kinds" of the owner's list)

The owner's decision of 14 November 2026: party kinds are "the app's relation and plaintiff-position lists and each event's actor". The app's lists are `FactVocabulary.Relations` (17 values) and `FactVocabulary.Positions` (23 values); `claim-elements.json` says, per claim type, which (relation, position) pairs are possible.

- **The label:** for each claim type of the note, one entry `{ "claim", "relation", "position", "evidence": [quote] }`, where (relation, position) is one of that claim type's `roles`. A relabeller who cannot tell answers `"relation": "unknown", "position": "unknown"` with no evidence; since every generated note is written to show its role, that is a disagreement. The reader has no "unknown" answer: under its threshold it abstains.
- **Fourteen claim types have exactly one role.** For them the label is that role, written by code, without evidence; nobody labels it, the comparison ignores it, and the reader does not read it.
- **What this reading leaves out, said plainly for the owner:** only the pairs that `claim-elements.json` lists are ever output. Five relations of the app's list (`debt_acknowledgment`, `inheritance`, `company`, `administrative`, `civil_status`) and six positions (`borrower`, `debtor`, `occupant`, `employer`, `heir`, `partner`) never appear, and a note outside the catalogue has no party kind.
- **Seven claim types offer a choice**, and only these are labelled and read: `contract-rescission` (5 roles), `sale-annulment-fraud` (3), `contract-damages` (8), `commercial-lease-eviction` (2), `custody` (2), `custody-forfeiture` (3), `visitation` (2).
- **Each event's actor** is already part of output 3 (`plaintiff`, `defendant`, `other`, `unknown`).
- **In the reader's contract** the role is an optional part of the claim reading: an integrator that ignores it loses nothing ([../specs/reader.md](../specs/reader.md), "The parties head").

The guide's rule for the role is in its section 3b (added with this scheme).

## 4. Elements and polarity

The 75 elements are the (claim type, event kind) pairs of `claim-elements.json`; the guide's section 5 holds, for each, its Arabic proposition, what counts as each polarity, and whether it takes a date (D) and an amount (A). 41 of the 44 event kinds of `FactVocabulary.Events` are used.

| Part | Values |
|---|---|
| polarity | `stated`, `denied`, `disputed`, `notMentioned` |
| actor | `plaintiff`, `defendant`, `other`, `unknown` |
| disputedBy (only when `disputed`) | `plaintiff`, `defendant`, `other` |
| date, amount | a value (section 7), only where the element takes one and the note gives it for that event |

An element is labelled for **every** claim type of the note, `notMentioned` included.

## 5. Slots read from the note

The 8 slots of `slots.json` with `readFromNote: true` on 14 November 2026. The guide's section 6 gives each one's rule.

| Slot | Type | Asked for | Note |
|---|---|---|---|
| `notice_served` | yes / no / unknown | every claim type | follows the event `notice_served` only in part: the guide's table |
| `notice_date` | date | every claim type | a value role |
| `notice_means` | choice: `bailiff`, `registered_letter`, `other` | every claim type | |
| `nonconciliation_record` | yes / no / unknown | `abusive-dismissal`, `unpaid-wages` | |
| `nonconciliation_record_delivered_on` | date | `abusive-dismissal`, `unpaid-wages` | a value role; the date of delivery, not of the record |
| `contract_date` | date | every claim type | a value role |
| `public_body_party` | yes / no / unknown | every claim type | the list below |
| `property_registered` | yes / no / unknown | every claim type | |

**The public-body list, frozen with this version** (it was to come from a rule module; it is fixed now from the slot's own question and the guide's definition of `administrative`). The same three lines are in the guide's sections 3 and 6:

- **`yes`:** the note names a party as **the State (الدولة), a ministry (وزارة), a wilaya (ولاية), a commune (بلدية), or a public institution of an administrative character (مؤسسة عمومية ذات صبغة إدارية)**.
- **`no`:** every party is a private person or a company; or the note itself names a party as a public company («شركة عمومية», «مؤسسة عمومية اقتصادية») or as a public institution of an industrial and commercial character: neither is on the list.
- **`unknown`:** a party's nature is not clear from the note: «مؤسسة» or «مؤسسة عمومية» with nothing more.

The slot is about **any** party. A public body as plaintiff makes the slot `yes`; it does not by itself make the note `outside` (the guide's `administrative` kind is a claim **against** such a body, or the annulment of an administrative decision). If the module `ordinary-court-exits` later shows that the list should be wider, that is a new slot or a change of meaning (scheme 2.0, a stop point), never a silent edit.

The other 14 slots of `slots.json` have `readFromNote: false`: the app asks the lawyer. Nine of them came from the module `formal-notice`. Section 10 says how one of them can become a reader output.

## 6. Requests

The 24 kinds of `FactVocabulary.Requests`: `payment`, `eviction`, `rescission`, `damages`, `annulment`, `restitution`, `delivery`, `divorce`, `tatliq`, `khul`, `maintenance`, `custody`, `custody_transfer`, `visitation`, `housing`, `reinstatement`, `wages`, `partition`, `cancel_decision`, `correction`, `dissolution`, `guardianship`, `provisional_execution`, `other`. Each with its evidence, and its amount when the note states one (the guide, section 7).

## 7. Dates and amounts

Every date and every amount written in the note, one entry for each place it is written (the guide, section 8): `kind` (`date` or `amount`), the quote, the normal form (`yyyy-mm-dd`, `yyyy-mm` or `yyyy`; digits in dinars), and `roles`: the event kinds and request kinds the value belongs to and the slot ids it answers (`notice_date`, `nonconciliation_record_delivered_on`, `contract_date`); an empty list when it belongs to none. With, where they apply, `relative`, `calendar: hijri`, `currency`, `amountDisputed`.

## 8. The cells that are drawn (element × polarity)

Not every polarity of every element is a note a lawyer would write: a loan note whose own author says no money was lent is not a loan claim. The sampler, the quotas of the mirror set and of hidden v3, and the "every element with every polarity" tests all read this table.

**The rule.** For every element: `stated`, `disputed` and `notMentioned` are drawn. `denied` is drawn **unless** the note, with that element denied by its own author, would no longer be a note for that claim type.

**How the 75 `denied` cells are decided, once, before the freeze** (two runs, stated to the owner with the guide test's). Both deciders are Claude models: the table rests on one model family. It decides only which scenarios are drawn, never a label, and its safe side is "drawn".

1. `notes cells --prepare` writes 75 items (ids 1001 to 1075), one per element, into `notes/cells-test.json` (apart from `guide-test.json`; no expected answer): the claim type's Arabic label and the element's Arabic proposition.
2. Sonnet 5.5 and Opus 5.5 each answer all 75 ([../prompts/notes-cells.md](../prompts/notes-cells.md), no tool).
3. A cell is **not drawn** only when both answer "no". On any other outcome it is drawn: more coverage is the safe side. These items are outside the rule "no unexplained item before the freeze".
4. `notes cells --write-table` writes the table below from the two replies; the executor edits nothing in it. The sampler's tests, the quotas of the mirror set and of v3, and the "every cell" checks read this table.

| Claim type | Event kind | `denied` drawn | Sonnet | Opus |
|---|---|---|---|---|
| *(75 rows, written by the script at the freeze)* | | | | |

## 9. The boundary table, version 1

What looks like a claim type and is labelled otherwise. It is built **only from the guide's own definitions** (section 3): it adds no statement of the law. The rule records' `boundaries` add rows later (a minor version); a row that would contradict one of these is a change of meaning.

**A. Claim types that look alike.** The request decides, as the guide says.

| Looks like | Is, when | What tells them apart |
|---|---|---|
| `unpaid-sale-price` | `contract-rescission` | the note asks to undo the sale, not to be paid |
| `unpaid-sale-price` | `commercial-debt` | the seller is a supplier and the note speaks of invoices between traders |
| `contract-damages` | `contract-rescission` | the note asks to end the contract; with both requests, both types |
| `sale-non-delivery` | `contract-rescission` | the buyer asks to undo the sale instead of delivery |
| `tort-damages` | `liability-for-things` | the harm came from a thing, an animal or a building, not from a person's act |
| `unpaid-rent` | `commercial-lease-eviction` | the lease is commercial and the note asks for eviction |
| `unpaid-rent` | `eviction-occupier-without-title` | there is no lease: the occupier has no title |
| `divorce-by-husband` | `tatliq`, `khul` | who asks, and how: the wife asks the judge for divorce on a ground she states (`tatliq`); the wife offers payment for it (`khul`) |
| `custody` | `custody-forfeiture` | custody was already awarded and the note asks to take it away |
| `abusive-dismissal` | `unpaid-wages` | the note asks for wages, not for reinstatement or damages for the dismissal; with both requests, both types |
| `loan-repayment` | `commercial-debt` | the debt comes from supplies between traders, not from a loan |

**B. Near-misses that fall `outside`.** A near-miss scenario takes a claim type's story and adds the one telling fact. The label follows from the guide's definition of the kind.

| Outside kind | The telling fact (English seed lines for the sampler; the Arabic is the writer's) | Drawn near |
|---|---|---|
| `administrative` | the other party is the State, a ministry, a wilaya, a commune or a public institution of an administrative character; or the note asks to annul a decision of an administration | every civil, commercial and labour type |
| `criminal` | the note asks to file a criminal complaint, or to join a criminal case as a civil party, instead of bringing a civil claim | `tort-damages`, `sale-annulment-fraud`, `loan-repayment`, `nafaqa` |
| `payment-order` | the note asks for an order to pay (أمر أداء), not for a claim | `loan-repayment`, `unpaid-sale-price`, `unpaid-rent`, `commercial-debt` |
| `urgent` | the note asks the judge of urgent matters for a provisional measure | `eviction-occupier-without-title`, `unpaid-rent`, `custody`, `visitation`, `unpaid-wages` |
| `enforcement` | a final judgment already exists and the note asks to have it enforced | `nafaqa`, `custody`, `visitation`, `loan-repayment`, `unpaid-wages` |
| `family-other` | the matter is inheritance, kafala, proof of marriage or of descent, or guardianship of an adult | the seven family types |
| `civil-status` | the note asks to correct a civil-status record | the seven family types |
| `property-other` | the matter is partition, a boundary, pre-emption or possession | `eviction-occupier-without-title`, `unpaid-rent`, `sale-non-delivery` |
| `company` | the dispute is between partners or about the life of a company | `commercial-debt`, `contract-damages`, `loan-repayment` |
| `other` | none of the above, and none of the 21 types | any |

**Two claim types in one note** (about one note in eight): the pairs the sampler draws in version 1 are `unpaid-wages` + `abusive-dismissal`; `contract-rescission` + `contract-damages`; `unpaid-sale-price` + `contract-damages`; `sale-non-delivery` + `contract-damages`; `tatliq` + `nafaqa`; `tatliq` + `custody`; `divorce-by-husband` + `custody`; `khul` + `custody`; `nafaqa` + `custody`; `custody` + `visitation`; `unpaid-rent` + `contract-damages`. They are drawing choices, not statements of the law.

## 10. Slots added after 1.0

A slot a rule record adds starts as a question the app asks the lawyer (`readFromNote: false`). In phase D each is decided by the table of [../specs/scheme-extension.md](../specs/scheme-extension.md): **Derived** (computed from an event the scheme already has), **Asked** (stays a question), or **Read** (a new reader output, with its note pack and its head).

| Slot | From the record | Decision | The row of the table that decided | Scheme version |
|---|---|---|---|---|
| *(none yet)* | | | | |

## 11. Known gaps of the guide

An item of the guide test that is still answered two ways after one fix of the guide is written here, with both answers, and goes into the owner's question batch. A note that turns on such a sentence is labelled as the guide's protective reading says, and the gap is named in the report of any score it could touch.

| Guide-test item | The two answers | Sent to the owner in |
|---|---|---|
| *(none yet)* | | |

## 12. Versions

| Version | Date | What it added | Commit |
|---|---|---|---|
| 1.0 | 14 November 2026 (written); frozen in phase A | everything above; guide version 1.1 | *(the freeze commit; the guide's hash)* |

**A minor version (1.1, 1.2 …) adds** an output: a slot, an element, a request kind, a value role, a boundary row. It never changes what an existing label means, never removes one, never renames one. Every note records the version it was labelled under (`provenance.schemeVersion`); for an output added later, an older note is *unlabelled*, which is not `unknown` and not `notMentioned`.

**A change of meaning is version 2.0 and a stop point for the owner** (HANDBOOK section 8): it forces relabelling and retraining.
