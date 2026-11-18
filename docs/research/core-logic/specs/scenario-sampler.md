# Specification: the scenario sampler and the style cards

**Why a sampler.** If a model is asked for "a lawyer's note about unpaid wages", it writes the same tidy note a thousand times, and it decides the facts itself, so nobody knows the right labels. The sampler turns this round: **code decides the case** (the claim type, each element's polarity, each slot, the dates, the sums, what is asked), checks it is legally coherent with the one rule evaluator, and gives the writer a scenario to turn into a note. The gold answer exists before the note does; the writer's job is only the wording, and a checker (the local filter for a training note, a blind relabeller for a test note) checks that the wording says what the scenario says.

**Who writes it.** Opus 5.5, in phase A, as soon as the label scheme is written ([../notes/SCHEME.md](../notes/SCHEME.md)). Project `DzLegal.CoreLogic.Notes` (C#, so that it calls the evaluator in process), tool `notes sample`.

## Version 1: drawing without final rule records

The first handbook built the sampler after every rule record was final. Since 14 November 2026 the reader comes first, so **version 1 draws from what exists**, and each place that needed a record has a rule:

| What the first design took from the rule records | Version 1 takes it from |
|---|---|
| the claim types, their elements, actors, date and amount flags, requests | `data/law/claim-elements.json` (read only) |
| the plaintiff's role, for the seven claim types that offer a choice | the claim type's `roles` in the same file: drawn uniformly, written into the scenario as `"roles": { "<claim>": { "relation", "position" } }`, and always to be expressed by the note (no scenario leaves the role unsaid) |
| which polarity of which element is a note a lawyer would write | the cells table of SCHEME.md, section 8 |
| which two claim types occur together | the pairs listed in SCHEME.md, section 9 |
| near-misses and their telling fact | SCHEME.md, section 9, table B: the outside kind's English seed lines, drawn for the claim types it names |
| the `how` line of a denied or disputed event | a list of two to four English lines per element, written by the executor from the element's proposition (who contests, with what story); seeds, never wording, and never a statement of the law |
| coherence, and the `outcome` of a scenario | the rule evaluator **on the records that are final when the set is drawn** (`rules/final/*.json`: `formal-notice` at first). A scenario gets an `outcome` only for those; with no final record that applies, `outcome` is absent and coherence is the slot table of the guide applied in reverse |
| dates placed around a deadline's last day | only for a deadline of a final record; otherwise dates are drawn in a possible order and nothing more |

When more records are final (phase B onwards), new sets are drawn with their outcomes; **sets already drawn are not redrawn**. The sampler's commit and seed are in every note's provenance, so each set says which version drew it. A change of the sampler after the generation method is frozen is not allowed ([notes-pipeline.md](notes-pipeline.md), section 11).

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
- `how`: for a `denied` or `disputed` event, a short English line on the manner (who contests, with what story), drawn from a list per element (version 1, above). It is a seed, not wording: the writer never sees Arabic from the sampler.
- `contest`: on a `stated` event only, a contest that leaves the proposition standing: `{ "of": "amount" | "cause", "by": "plaintiff" | "defendant", "how": "<English line>", "amount": <the other side's figure, for an amount> }`. The guide decides these (cases 5 and 31; note a): the polarity stays `stated`; a contest of the amount gives `amountDisputed: true` to both figures. Without this field the writer, told to write no more than the scenario, could not write the challenge set's contests of amount and of cause.
- `extraValues`: each has a `what` unique within the scenario (the writer's reference is `value:<what>`). `optional: true` is allowed **only** here: a lawyer does not note every figure, so the writer may leave such a value out. Every other fact of the scenario must be in the note.
- `distractors`: English lines for details that are not facts of the case. They hold no date and no amount (an amount the note should carry is an extra value), and the writer gives them no reference.
- `outsideFact` (for a near-miss): the one telling fact, in English, that makes the case fall outside the type it resembles. The writer must express it and quote it under `claim`, so the script can check the note says it.
- `outcome`: what the evaluator returns for these slots, in the shape of a worked example's expected findings (`ref` and the declared `effect`). It is not given to the writer; it lets the sampler balance cases that block, warn, ask and pass. **It records the final records that existed when the set was drawn, nothing more** (the owner, 18 November 2026): no label, no score and no check uses a stored outcome; a score that needs what the rules say about a case recomputes it from the note's gold slots and the final records at scoring time. Checked that day: the writer's view hides it, ingest stores only the scenario's id, labels come from the writer's quotes and the scenario's facts, and the scorer, the loader, the export, the training scripts and the noise sorter never read it.

## How a scenario is drawn

1. **Claim.** Uniform over the 21 types for the base sets; about 12 % two-claim notes (the pairs of SCHEME.md, section 9); about 12 % `outside` with an `outsideKind`, of which at least half are **near-misses built from a real type** (take a type's scenario and add the one telling fact of SCHEME.md, section 9, table B; that fact is the scenario's `outsideFact`).
2. **Polarity pattern.** For each element of the claim: draw a polarity from a table tuned so that, over a set, **every element appears with every polarity**, and `denied` and `disputed` are far more frequent than in life (about 15 % each of mentioned events): polarity is the hard part, and a set that mirrors life would hold too few to learn or to measure. Some `stated` events also carry a `contest` of their amount or their cause. Forbidden patterns are removed by rule, not by hand: a pattern is kept only if it is **a case a lawyer would still write a note about** (a loan note with `money_lent` denied by the notes themselves is no loan claim: drop it or turn it into `outside`).
3. **Slots.** Each `readFromNote` slot gets a value consistent with the events (section 6 of the labelling guide): the generator applies the guide's slot table in reverse. Deliberately include the divergent cases: an oral demand with no formal notice; a record with its date and no delivery date; a delivery date alone.
4. **Dates.** Drawn so that events are in a possible order, and so that deadlines land, across the set, far inside, far outside and **inside the warning band** on both sides. For a deadline of a final record, the evaluator computes the last day and the sampler places the petition date around it.
5. **Sums.** Plausible for the claim (a monthly wage between the legal minimum and a senior salary; a commercial invoice from tens of thousands to tens of millions of dinars), with remainders that add up when a partial payment is drawn (paid + owed = total), and with **one unrelated amount** in about a third of notes.
6. **Requests.** From the claim type's requests in `claim-elements.json`, each with a probability; sometimes a request that belongs to a neighbouring claim (the boundary cases).
7. **Cast.** Names, companies, places, streets from **fictitious lists** built once by code (places: the names of the 58 wilayas, typed once into the lists; no download): given names × family names recombined, company names from invented roots; a check refuses any full name that exactly equals an entry of a small list of well-known public figures and companies. No real person, no real case number. Lists of at least 400 given names, 600 family names, 300 company roots, the 58 wilayas' names.
8. **Style card** (below), drawn independently of everything above, so that no style correlates with a label.
9. **Coherence.** Run the evaluator on the final records that exist (version 1, above). Reject a scenario whose slots contradict each other; keep the outcome when there is one. **Since sampler 1.3 (the owner, 17 November 2026):** a dissolution claim (`khul`, `tatliq`, and `divorce-by-husband`, the same contradiction) never stands beside another claim whose `divorce` event is stated or disputed: the note would ask to dissolve a marriage it says was dissolved before (found in two dev notes). A pair that breaks the rule keeps its two claims and draws its facts again, so the share of two-claim notes stays the recipe's (`ScenarioSampler.DissolutionBesideEarlierDivorce`; the coherence test checks it on 10,000 draws).

## Style cards

A style card says **how** the note is written, never what it says. About 40 cards, each a short English paragraph plus parameters. The writer gets one card per note.

**Version 2 of the cards (sampler 1.2, 17 November 2026; the owner: "in the 4,000 training notes, widen the style variety: more style cards, several Codex prompt variants, shorter and messier notes").** 80 more cards (`s2-01` to `s2-80`) beside the first 40, which keep their text. On them the lengths lean short: very short or short on 50 of the 80 cards, medium on 20, long on 10. Two parameters are added: `mess` (clean, typing slips never in a name, date or sum, bare fragments, run-on sentences, shorthand) and `setPhrases` (free, or no set formula for the parties: the writer-identity probe found Codex's «شخصان طبيعيان»). The first 40 cards take the neutral values (`clean`, `free`). Every set drawn from sampler 1.2 draws from all 120. The writer prompt's version 3 turns four framings by call (`WriteCommand.Framings`: at the desk, during the visit, a handover to a colleague, an end-of-day reminder).

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
| dev | 600 kept (400 if the owner takes that saving): **300 written by Codex, 300 by Sonnet 5.5** | base recipe, families disjoint from train; the two halves from the same seed stream, split by family | the Codex half tunes and measures; the Sonnet half only measures ([notes-pipeline.md](notes-pipeline.md), sections 6 and 7) |
| challenge | 50 per difficulty, **25 by each writer**: restriction (لم … إلا / سوى); double negation; contest of the amount only; contest of the cause only; an oral or informal demand; record date against delivery date; correction mid-note; «رغم» (despite); an intention; a named document against an explicit contest; two claims; near-miss. **This is the one list**; the reader's specification reports results per difficulty from it | targeted recipes | polarity as its own problem; the same rule for the two halves |
| clean | 1,000 kept, split by family: 700 `train`, 100 `dev`, 200 `test` | base recipe, without the parts given only in English | the clean-notes track (notes pipeline, section 8) |
| a slot pack | about 400 train and about 100 dev (half by each writer) per slot the reader is to read | scenarios that fix the slot's values | [scheme-extension.md](scheme-extension.md) |
| probe | about 150 composed notes, set apart | the composer | the relabeller's known-truth probes ([hidden-v3-commands.md](hidden-v3-commands.md), section 7b); never trained on |
| step5 | 120 sheets, 20 for each of six final claim types | the base recipe with the constraints of [step5-templates.md](step5-templates.md) | step 5's fact sheets; not notes |
| mirror | about 440 kept (about 330 with the saving) | **not drawn by this sampler**: the v3 roles (planner, writers, isolated Opus relabellers), with v3's prompts and commands, after the generation method is frozen, in v3's proportions (16 per claim type, 80 near-miss, about 24 for the two rarest slots) | predicting v3, once, for the final candidates; rehearsing the v3 commands. It chooses nothing |
| polarity families | 300 groups of 3 to 4 | one scenario, each polarity of one element, everything else equal. **Inside the sets above, not in addition:** about 200 groups in train, 100 in dev and challenge. A group is written in one call by one writer | training and testing polarity; a group is never split |

Hidden set v3 and its mirror set do **not** use this sampler: a planner writes quotas, and each writer invents its own cases to them ([hidden-v3-commands.md](hidden-v3-commands.md)). That is the point of a hidden set: it must not share the sampler's blind spots.

## The challenge set's recipes

One row per difficulty. The sampler sets the scenario's `difficulty` (copied into the note's `provenance.difficulty`), forces the constraint and the style value, and the note must pass the code check or it is invalid (rewritten once, like any note).

| Difficulty (`difficulty`) | The scenario must have | Style value forced | Code check on the note |
|---|---|---|---|
| `restriction` | an event that is partly true: a payment made for less than what is owed (both amounts in the scenario) | negation manner: restriction | «إلا», «سوى» or «غير» after a negation particle, in the sentence of that event's quote |
| `doubleNegation` | a `stated` event | negation manner: double negation («لا ينكر», «لم يمتنع عن») | two negation cues of the cue table in the sentence of that event's quote |
| `contestOfAmount` | a `stated` event with a `contest` of its amount | – | both figures found by the parsers |
| `contestOfCause` | a `stated` event with a `contest` of its cause | – | a quote for the contest |
| `informalDemand` | `notice_served` = `unknown`, and a distractor line "the client asked for payment by word of mouth, by phone or by message" | – | none of the formal-notice words of the guide's slot table in the note |
| `recordAgainstDelivery` | a labour scenario with the record's date and a different delivery date, or only one of the two | – | exactly the scenario's dates found by the parsers |
| `correction` | any scenario; one extra value marked as first written wrong | extras: a correction mid-note | a correction marker («بل», «والصحيح», «تصحيح», «عفوا») |
| `despite` | a `stated` event to be told inside a concession | – | «رغم», «بالرغم من» or «مع أن» in the sentence of that event's quote |
| `intention` | an event `notMentioned`, and a distractor line "the client intends to do it" for that same event | – | a cue of intention («سوف», «يعتزم», «ينوي», «سـ») and no quote for that event |
| `documentAgainstContest` | a `disputed` event and a document named after it | fields: all four, documents as a list | a non-empty `documents` field and a quote for the contest |
| `twoClaims` | a two-claim scenario | – | a `claim` quote for each |
| `nearMiss` | an `outside` near-miss | – | the quote of the telling fact |

**Implemented: four rows (sampler 1.4, 18 November 2026).** The owner kept the challenge set "for polarity only (negation, restriction, partial admission, contested facts): about 100 kept notes, written and relabelled like the dev set". One row per word: `doubleNegation` (negation), `restriction` (restriction), `contestOfAmount` (partial admission), `documentAgainstContest` (contested facts); `notes sample --set challenge --difficulty <id>` refuses the other eight by name. `doubleNegation` and `documentAgainstContest` filter base draws (the document's event is one a paper records); `restriction` and `contestOfAmount` force their element in the draw (a defendant's part payment beside a larger stated sum of the debt; a stated amount with its contest). The scenario names its event (`target`) and its forced style (`styleForced`, which takes the card's own value and sentence in the writer's view); the code check (`ChallengeChecks`) takes its words from the reader's cue table and its figures from the parsers. The base sets draw the same bytes as with sampler 1.3 (a test holds their hashes).

## Tests

- Every scenario passes the evaluator without contradiction; rejected draws are counted, by reason.
- Over 10,000 draws: every element × polarity cell **that the cells table of SCHEME.md (section 8) marks as drawn** is non-empty, and no other cell is; every `readFromNote` slot takes every value; **when a final record with a deadline exists**, the deadline outcomes cover none, warn (band) and, where an item may block, block; no style parameter has a mutual information with any label above 0.02 bits (report the largest).
- The same seed gives the same scenarios. The seed and the sampler's commit are recorded with every set, and in every note's provenance once the schema has the fields.
- No name of the cast equals an entry of the refusal list.

## Traps

- **Balanced is not realistic, and that is deliberate.** Say so in every report that gives a score: the sets over-represent denied and disputed events and near-misses. The clean test split and the app's own cases ([notes-pipeline.md](notes-pipeline.md), section 7) are reported beside every score as the counterweight.
- **Do not let the sampler write Arabic.** The moment code emits Arabic phrases, the notes carry a template the classifier can learn. The only code-composed notes are the separate, marked `composed` set (see [notes-pipeline.md](notes-pipeline.md)).
- **A scenario the evaluator cannot evaluate** with the final records that exist is expected in version 1 when no final record reads its slots: it simply has no `outcome`. If a **final** record reads a slot the scheme does not have, that is [scheme-extension.md](scheme-extension.md)'s case: the slot starts as a question for the lawyer; do not patch the sampler.
