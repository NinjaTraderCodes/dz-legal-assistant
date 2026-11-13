# Specification: the scenario sampler and the style cards

**Why a sampler.** If a model is asked for "a lawyer's note about unpaid wages", it writes the same tidy note a thousand times, and it decides the facts itself, so nobody knows the right labels. The sampler turns this round: **code decides the case** (the claim type, each element's polarity, each slot, the dates, the sums, what is asked), checks it is legally coherent with the one rule evaluator, and gives the writer a scenario to turn into a note. The gold answer exists before the note does; the writer's job is only the wording, and the judge checks that the wording says what the scenario says.

**Who writes it.** Opus 5.5, in step 3, after the final rule records exist and the label scheme is frozen. Project `DzLegal.CoreLogic.Notes` (C#, so that it calls the evaluator in process), tool `notes sample`.

## A scenario

```json
{
  "id": "s-004211",
  "family": "f-0931",
  "claims": ["unpaid-wages"],
  "outsideKind": null,
  "events": [
    { "claim": "unpaid-wages", "kind": "employment_started", "polarity": "stated", "actor": "unknown", "date": "2019-09-03" },
    { "claim": "unpaid-wages", "kind": "wages_unpaid", "polarity": "disputed", "actor": "defendant", "disputedBy": "defendant", "amount": 384000,
      "how": "the employer says the wages were transferred to the worker's postal account" },
    { "claim": "unpaid-wages", "kind": "conciliation_failed", "polarity": "stated", "actor": "unknown", "date": "2026-01-22" }
  ],
  "slots": { "nonconciliation_record": "yes", "nonconciliation_record_delivered_on": "2026-02-02", "notice_served": "unknown" },
  "requests": [ { "kind": "wages", "amount": 384000 }, { "kind": "damages", "amount": 100000 } ],
  "extraValues": [ { "kind": "amount", "value": 64000, "what": "the monthly wage", "optional": true } ],
  "cast": { "plaintiff": "…", "defendant": "…", "place": "…" },
  "distractors": ["a detail about the worker's seniority bonus in another company"],
  "petitionDate": "2026-03-10",
  "outcome": { "findings": [ { "ref": "labour-prelitigation.d1", "effect": "none" } ], "questions": ["notice_served"] },
  "style": "c-17"
}
```

- `family`: scenarios that share a skeleton (same claim, same polarity pattern, values changed) share a family. **A family never sits on both sides of a split.**
- `actor`: who did it, from the element's usual actor in `claim-elements.json` (`unknown` where it names none), or drawn where the guide says the actor varies (`commercial-lease-eviction/rent_unpaid`). The writer's labels take it from here: the writer chooses no label.
- `how`: for a `denied` or `disputed` event, a short English line on the manner (who contests, with what story), drawn from a list per element written with the rule records' boundaries. It is a seed, not wording: the writer never sees Arabic from the sampler.
- `contest`: on a `stated` event only, a contest that leaves the proposition standing: `{ "of": "amount" | "cause", "by": "plaintiff" | "defendant", "how": "<English line>", "amount": <the other side's figure, for an amount> }`. The guide decides these (cases 5 and 31; note a): the polarity stays `stated`; a contest of the amount gives `amountDisputed: true` to both figures. Without this field the writer, told to write no more than the scenario, could not write the challenge set's contests of amount and of cause.
- `extraValues`: each has a `what` unique within the scenario (the writer's reference is `value:<what>`). `optional: true` is allowed **only** here: a lawyer does not note every figure, so the writer may leave such a value out. Every other fact of the scenario must be in the note.
- `distractors`: English lines for details that are not facts of the case. They hold no date and no amount (an amount the note should carry is an extra value), and the writer gives them no reference.
- `outsideFact` (for a near-miss): the one telling fact, in English, that makes the case fall outside the type it resembles. The writer must express it and quote it under `claim`, so the script can check the note says it.
- `outcome`: what the evaluator returns for these slots, in the shape of a worked example's expected findings (`ref` and the declared `effect`). It is not given to the writer; it lets the sampler balance cases that block, warn, ask and pass.

## How a scenario is drawn

1. **Claim.** Uniform over the 21 types for the base sets; about 12 % two-claim notes (pairs that the rule records' boundaries name as occurring together); about 12 % `outside` with an `outsideKind`, of which at least half are **near-misses built from a real type** (take a type's scenario and change the one telling fact its boundary names; that fact is the scenario's `outsideFact`).
2. **Polarity pattern.** For each element of the claim: draw a polarity from a table tuned so that, over a set, **every element appears with every polarity**, and `denied` and `disputed` are far more frequent than in life (about 15 % each of mentioned events): polarity is the hard part, and a set that mirrors life would hold too few to learn or to measure. Some `stated` events also carry a `contest` of their amount or their cause. Forbidden patterns are removed by rule, not by hand: a pattern is kept only if it is **a case a lawyer would still write a note about** (a loan note with `money_lent` denied by the notes themselves is no loan claim: drop it or turn it into `outside`).
3. **Slots.** Each `readFromNote` slot gets a value consistent with the events (section 6 of the labelling guide): the generator applies the guide's slot table in reverse. Deliberately include the divergent cases: an oral demand with no formal notice; a record with its date and no delivery date; a delivery date alone.
4. **Dates.** Drawn so that events are in a possible order, and so that deadlines land, across the set, far inside, far outside and **inside the warning band** on both sides. The evaluator computes the last day; the sampler places the petition date around it.
5. **Sums.** Plausible for the claim (a monthly wage between the legal minimum and a senior salary; a commercial invoice from tens of thousands to tens of millions of dinars), with remainders that add up when a partial payment is drawn (paid + owed = total), and with **one unrelated amount** in about a third of notes.
6. **Requests.** From the record's usual requests, each with a probability; sometimes a request that belongs to a neighbouring claim (the boundary cases).
7. **Cast.** Names, companies, communes, streets from **fictitious lists** built once by code: given names × family names recombined, company names from invented roots; a check refuses any full name that exactly equals an entry of a small list of well-known public figures and companies. No real person, no real case number. Lists of at least 400 given names, 600 family names, 300 company roots, all 58 wilayas' communes by name only.
8. **Style card** (below), drawn independently of everything above, so that no style correlates with a label.
9. **Coherence.** Run the evaluator. Reject a scenario whose slots contradict each other; keep the outcome.

## Style cards

A style card says **how** the note is written, never what it says. About 40 cards, each a short English paragraph plus parameters. The writer gets one card per note.

| Parameter | Values |
|---|---|
| Length | very short (2 to 4 lines), short, medium, long (a page) |
| Order | chronological; requests first; the problem first, history after; documents first |
| Tidiness | drafted sentences; telegraphic notes with dashes; numbered points; one block without paragraphs |
| Fields used | everything in `facts`; `facts` + `requests`; all four; `subject` filled or empty; `documents` as a list or absent |
| Voice | third person («المدعي»); the client by name; first person plural of the office («نطلب»); nominal fragments |
| Dates | «12/03/2025»; «2025/03/12» (year first); «12 مارس 2025»; «في 12 من شهر مارس 2025»; Arabic-Indic digits; month names جانفي… ; mixed in one note. Never two-digit years (the guide forbids them to writers; a known gap in realism, stated in the report) |
| Amounts | «500.000 دج»; «500 000 دينار»; words and digits; «خمسون مليون سنتيم»; Arabic-Indic digits |
| Negation manner | لم + verb; عدم + noun; دون / بدون; لا وجود لـ; restriction (لم … إلا / سوى); refusal verbs (امتنع، رفض، تخلف، تقاعس) |
| Dispute manner | ينكر؛ ينازع في؛ يدعي؛ يتمسك بـ؛ يزعم؛ يدفع بـ؛ reported in the plaintiff's voice («حسب المدعى عليه …») |
| Extras | a correction mid-note; an irrelevant detail; a question the lawyer notes to himself; a reminder («يطلب من الموكل وصل التسليم»); none |
| Terms | the element's usual words; a synonym set (per element: at least four ways to name the act that share no content word with the element's label) |

**The last line matters most.** If every unpaid-wages note says «الأجور غير المدفوعة», the classifier learns that phrase and fails on «مستحقاته المالية المتأخرة». For each element the sampler carries a list of concept names in English ("wages / back pay / salary arrears / monthly dues"), and the card tells the writer which to express; the Arabic is the writer's.

A card never names a claim type, an element or a polarity. A probe checks that no card predicts a label (see [probes.md](probes.md)).

## Sets drawn

| Set | Size | Drawn how | Used for |
|---|---:|---|---|
| pilot | 500 | the base recipe | measuring the pipeline before freezing it |
| train | 4,000 (first batch) | base recipe | training; learning curve at 1,000, 2,000, 4,000 |
| dev | 600 (400 if the owner takes that saving) | base recipe, families disjoint from train | choosing models and thresholds |
| challenge | at least 50 per difficulty: restriction (لم … إلا / سوى); double negation; contest of the amount only; contest of the cause only; an oral or informal demand; record date against delivery date; correction mid-note; «رغم» (despite); an intention; a named document against an explicit contest; two claims; near-miss. **This is the one list**; the reader's specification reports results per difficulty from it | targeted recipes | polarity as its own problem |
| cross-check | 300 | base recipe | the cross-check gap (notes pipeline, section 6); never trained on |
| clean | 1,000, of which 200 (by family) are the clean test split | base recipe | the clean-notes track (notes pipeline, section 8) |
| mirror | about 440 kept (about 330 with the saving) | **not drawn by this sampler**: the v3 roles (planner, writers, Codex relabeller), with v3's prompts and commands, after the generation method is frozen, in v3's proportions (16 per claim type, 80 near-miss, about 24 for the two rarest slots) | predicting v3, once, for the final candidates; rehearsing the v3 commands. It chooses nothing |
| polarity families | 300 groups of 3 to 4 | one scenario, each polarity of one element, everything else equal. **Inside the sets above, not in addition:** about 200 groups in train, 100 in dev and challenge | training and testing polarity; a group is never split |

Hidden set v3 and its mirror set do **not** use this sampler: a planner writes quotas, and each writer invents its own cases to them ([hidden-v3-commands.md](hidden-v3-commands.md)). That is the point of a hidden set: it must not share the sampler's blind spots.

## Tests

- Every scenario passes the evaluator without contradiction; rejected draws are counted, by reason.
- Over 10,000 draws: every element × polarity cell is non-empty; every `readFromNote` slot takes every value; the deadline outcomes cover none, warn (band), block; no style parameter has a mutual information with any label above a small threshold (report the largest).
- The same seed gives the same scenarios. The seed and the sampler's commit are recorded with every set, and in every note's provenance once the schema has the fields.
- No name of the cast equals an entry of the refusal list.

## Traps

- **Balanced is not realistic, and that is deliberate.** Say so in every report that gives a score: the sets over-represent denied and disputed events and near-misses. The clean test split and the app's own cases ([notes-pipeline.md](notes-pipeline.md), section 7) are reported beside every score as the counterweight.
- **Do not let the sampler write Arabic.** The moment code emits Arabic phrases, the notes carry a template the classifier can learn. The only code-composed notes are the separate, marked `composed` set (see [notes-pipeline.md](notes-pipeline.md)).
- **A scenario the evaluator cannot evaluate** (a slot the records never read) is a sign the label scheme and the records have drifted: stop and reconcile them, do not patch the sampler.
