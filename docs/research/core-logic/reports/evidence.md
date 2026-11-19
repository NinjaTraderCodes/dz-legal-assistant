# Evidence that the work pays off (19 November 2026)

**What the owner asked:** "before any other work the owner needs hard evidence that this work is paying off. Run the three
tests below and report the numbers plainly; do not soften a bad result." The decision rules were fixed before any test
ran:
- **The reader** is worth continuing only if it beats Gemma 4 on claim type and on polarity beyond the interval, and the
  1,938-note result beats the 812-note result on at least one of the two.
- **The rules** are worth putting in the app if test 3 finds real errors of the kind that can lose a case.
- **A reader result inside its interval** is "not yet settled": it counts as neither a pass nor a fail, and the report
  gives the number of dev notes that would settle it.

## The verdicts

| Question | Result | Verdict by the fixed rule |
|---|---|---|
| Reader beats Gemma 4 (as the app runs it) on **claim type**, beyond the interval | best route at 1,938: 92 % against 90 %, +2 points (interval −9 to +13); composite 94 %, +4 (−8 to +16) | **not yet settled** (about 490 dev notes would settle the composite's +4, about 1,700 the route's +2, if the differences held) |
| Reader beats Gemma 4 on **polarity**, beyond the interval | best route at 1,938: 83 % against 81 %, +2 points (−6 to +10) | **not yet settled** (about 1,700 dev notes would settle it) |
| 1,938 notes beat 812 on claim type or polarity | polarity +13 points (+6 to +20) for the best route, +18 (+8 to +27) for the composite; claim type +12 (+2 to +24) for the composite | **yes** |
| **The reader** | Level with Gemma on claim type, polarity and requests; ahead on "outside" (+29 to +43 points) and on dates and amounts with their role (+45 to +60); milliseconds against about 76 s a note | **not yet settled: neither a pass nor a fail.** At 812 notes Gemma was ahead on polarity beyond the interval (−12); at 1,938 the gap is closed. The 62 dev notes cannot separate the two on claim type or polarity: a few hundred to about 1,700 notes would. |
| **The rules** find real errors of the kind that can lose a case | On cases the rule writers never saw: 2 forfeitures under CPCA 504 (a labour claim filed 978 and 218 days after its six-month limit), which the app does not see; on the records' own cases, 47 warnings the app lets through, 12 of them deadlines | **yes: worth putting in the app** |


## 0. The three resume tasks (they change the reader's scores, so they came first)

- **Stored outcomes:** checked against the code (commit 327ec27). Only the sampler writes a scenario's `outcome`. The
  writer's view and the noise sorter hide it, and no label, score, loader, export or training script reads it. The one
  planned use, `v3 finalize`, was written out of the spec before any code existed (68aa6ff).
- **«تنفيذ» against «تنفي»:** cue verbs now match only as whole words (و/ف, then ل/س, as prefixes), so «تنفيذ العقد» and
  «عدم التنفيذ» never make an event disputed. Tests cover «تنفيذ العقد», «عدم التنفيذ» and «وتنفي». On the tuning notes
  the votes are unchanged.
- **The five wrong votes:** their causes are in reports/cue-persons.md; none is about a verb's person. The cues were right
  on 0 of 2 disputed and 0 of 1 denied events, so they now vote "stated" only: 14 of 14 right, against 14 of 15 before.

## 1. The reader against today's app

**How.** The app today, as it runs: the claim type from its grounds analysis, `GroundsAnalyzer`, stage 1, which gives at
most two catalogue claim types; an empty answer is read as "outside". The facts come from its fact reader,
`ClaimFactReader` (the Alpha-01 reader), on each gold claim type. The model is Gemma 4 E4B, with the app's shipped Ollama
options: thinking off, temperature 0.2, on loopback (`research/core-logic/src/DzLegal.CoreLogic.AppReader`).

The comparison runs on the same 62 dev notes the reader is measured on: 32 written by Codex, 30 by Sonnet. Each note is
scored on its own by the unchanged scorer, and an abstention counts as not right. Intervals: 95 %, bootstrap over notes.
Comparisons: paired by note.

**The readers compared.** At each point the best route by the handbook's rule is the fine-tuned encoder with the highest
tuning score, seed 33 (0.683 at 812 notes, 0.839 at 1,938). Beside it are its composite and the n-grams, at 812 and at
1,938 training notes.

Notes: 62 (Codex 32, Sonnet 30). Rates in %, with the 95 % bootstrap interval over notes; an abstention counts as not right.

| System | Writer | claim type | outside | polarity | requests | dates and amounts |
|---|---|---|---|---|---|---|
| gemma-app | all | 90 (80–98; n 48) | 57 (30–83; n 14) | 81 (74–88; n 129) | 76 (65–84; n 94) | 16 (6–29; n 170) |
| gemma-app | Codex | 92 (79–100; n 24) | 50 (14–86; n 8) | 80 (68–89; n 59) | 74 (59–86; n 47) | 23 (4–44; n 78) |
| gemma-app | Sonnet | 88 (72–100; n 24) | 67 (25–100; n 6) | 83 (72–92; n 70) | 77 (61–89; n 47) | 11 (2–21; n 92) |
| finetuned33-1938 | all | 92 (83–98; n 48) | 86 (64–100; n 14) | 83 (76–89; n 129) | 72 (61–82; n 93) | 62 (54–70; n 170) |
| finetuned33-1938 | Codex | 92 (78–100; n 24) | 88 (60–100; n 8) | 80 (69–90; n 59) | 68 (54–81; n 47) | 62 (49–75; n 78) |
| finetuned33-1938 | Sonnet | 92 (79–100; n 24) | 83 (50–100; n 6) | 86 (78–93; n 70) | 76 (59–89; n 46) | 62 (51–72; n 92) |
| composite33-1938 | all | 94 (86–100; n 48) | 100 (100–100; n 14) | 83 (76–89; n 129) | 72 (61–82; n 93) | 76 (69–83; n 170) |
| composite33-1938 | Codex | 92 (79–100; n 24) | 100 (100–100; n 8) | 80 (69–90; n 59) | 68 (54–81; n 47) | 79 (67–89; n 78) |
| composite33-1938 | Sonnet | 96 (86–100; n 24) | 100 (100–100; n 6) | 86 (78–93; n 70) | 76 (59–89; n 46) | 74 (64–82; n 92) |
| ngrams-1938 | all | 94 (86–100; n 48) | 100 (100–100; n 14) | 67 (59–75; n 129) | 69 (58–79; n 94) | 76 (69–83; n 170) |
| ngrams-1938 | Codex | 92 (79–100; n 24) | 100 (100–100; n 8) | 71 (62–80; n 59) | 65 (50–79; n 46) | 79 (67–89; n 78) |
| ngrams-1938 | Sonnet | 96 (86–100; n 24) | 100 (100–100; n 6) | 63 (52–74; n 70) | 73 (58–85; n 48) | 74 (64–82; n 92) |
| finetuned33-812 | all | 83 (72–93; n 48) | 86 (64–100; n 14) | 70 (63–77; n 129) | 64 (53–73; n 102) | 55 (47–63; n 170) |
| finetuned33-812 | Codex | 75 (57–91; n 24) | 88 (60–100; n 8) | 68 (57–78; n 59) | 56 (43–67; n 52) | 56 (44–70; n 78) |
| finetuned33-812 | Sonnet | 92 (79–100; n 24) | 83 (50–100; n 6) | 71 (62–81; n 70) | 72 (55–85; n 50) | 53 (43–64; n 92) |
| composite33-812 | all | 81 (69–92; n 48) | 79 (54–100; n 14) | 65 (57–73; n 129) | 59 (49–69; n 96) | 70 (61–78; n 170) |
| composite33-812 | Codex | 79 (62–95; n 24) | 88 (57–100; n 8) | 68 (58–78; n 59) | 57 (42–72; n 47) | 65 (54–77; n 78) |
| composite33-812 | Sonnet | 83 (67–96; n 24) | 67 (25–100; n 6) | 63 (51–74; n 70) | 61 (47–76; n 49) | 74 (61–85; n 92) |
| ngrams-812 | all | 81 (69–92; n 48) | 79 (54–100; n 14) | 65 (57–73; n 129) | 59 (49–69; n 96) | 69 (61–77; n 170) |
| ngrams-812 | Codex | 79 (62–95; n 24) | 88 (57–100; n 8) | 68 (58–78; n 59) | 57 (42–72; n 47) | 67 (54–79; n 78) |
| ngrams-812 | Sonnet | 83 (67–96; n 24) | 67 (25–100; n 6) | 63 (51–74; n 70) | 61 (47–76; n 49) | 72 (61–80; n 92) |

**Paired against Gemma (the app):**

| Comparison | Writer | Line | Difference (points) | 95 % interval | Verdict |
|---|---|---|---:|---|---|
| finetuned33-1938 vs gemma-app | all | claim type | +2 | -9 to +13 | not yet settled (about 1677 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | all | outside | +29 | +7 to +55 | finetuned33-1938 beats gemma-app |
| finetuned33-1938 vs gemma-app | all | polarity | +2 | -6 to +10 | not yet settled (about 1657 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | all | requests | -3 | -14 to +7 | not yet settled (about 583 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | all | dates and amounts | +45 | +29 to +60 | finetuned33-1938 beats gemma-app |
| finetuned33-1938 vs gemma-app | Codex | claim type | +0 | -17 to +17 | not yet settled (no difference seen) |
| finetuned33-1938 vs gemma-app | Codex | outside | +38 | +0 to +75 | not yet settled (about 33 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Codex | polarity | +0 | -10 to +10 | not yet settled (no difference seen) |
| finetuned33-1938 vs gemma-app | Codex | requests | -6 | -19 to +9 | not yet settled (about 155 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Codex | dates and amounts | +38 | +9 to +67 | finetuned33-1938 beats gemma-app |
| finetuned33-1938 vs gemma-app | Sonnet | claim type | +4 | -9 to +19 | not yet settled (about 343 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Sonnet | outside | +17 | +0 to +50 | not yet settled (about 68 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Sonnet | polarity | +3 | -8 to +14 | not yet settled (about 466 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Sonnet | requests | -1 | -17 to +15 | not yet settled (about 28874 notes would settle it if the difference held) |
| finetuned33-1938 vs gemma-app | Sonnet | dates and amounts | +51 | +38 to +63 | finetuned33-1938 beats gemma-app |
| composite33-1938 vs gemma-app | all | claim type | +4 | -8 to +16 | not yet settled (about 488 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | all | outside | +43 | +17 to +70 | composite33-1938 beats gemma-app |
| composite33-1938 vs gemma-app | all | polarity | +2 | -6 to +10 | not yet settled (about 1657 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | all | requests | -3 | -14 to +7 | not yet settled (about 583 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | all | dates and amounts | +60 | +46 to +72 | composite33-1938 beats gemma-app |
| composite33-1938 vs gemma-app | Codex | claim type | +0 | -17 to +17 | not yet settled (no difference seen) |
| composite33-1938 vs gemma-app | Codex | outside | +50 | +14 to +86 | composite33-1938 beats gemma-app |
| composite33-1938 vs gemma-app | Codex | polarity | +0 | -10 to +10 | not yet settled (no difference seen) |
| composite33-1938 vs gemma-app | Codex | requests | -6 | -19 to +9 | not yet settled (about 155 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | Codex | dates and amounts | +56 | +31 to +81 | composite33-1938 beats gemma-app |
| composite33-1938 vs gemma-app | Sonnet | claim type | +8 | -8 to +25 | not yet settled (about 116 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | Sonnet | outside | +33 | +0 to +75 | not yet settled (about 38 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | Sonnet | polarity | +3 | -8 to +14 | not yet settled (about 466 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | Sonnet | requests | -1 | -17 to +15 | not yet settled (about 28874 notes would settle it if the difference held) |
| composite33-1938 vs gemma-app | Sonnet | dates and amounts | +63 | +49 to +74 | composite33-1938 beats gemma-app |
| ngrams-1938 vs gemma-app | all | claim type | +4 | -8 to +16 | not yet settled (about 488 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | all | outside | +43 | +17 to +70 | ngrams-1938 beats gemma-app |
| ngrams-1938 vs gemma-app | all | polarity | -15 | -23 to -6 | ngrams-1938 behind gemma-app |
| ngrams-1938 vs gemma-app | all | requests | -6 | -16 to +4 | not yet settled (about 153 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | all | dates and amounts | +60 | +46 to +72 | ngrams-1938 beats gemma-app |
| ngrams-1938 vs gemma-app | Codex | claim type | +0 | -17 to +17 | not yet settled (no difference seen) |
| ngrams-1938 vs gemma-app | Codex | outside | +50 | +14 to +86 | ngrams-1938 beats gemma-app |
| ngrams-1938 vs gemma-app | Codex | polarity | -8 | -21 to +4 | not yet settled (about 68 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | Codex | requests | -9 | -24 to +6 | not yet settled (about 87 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | Codex | dates and amounts | +56 | +31 to +81 | ngrams-1938 beats gemma-app |
| ngrams-1938 vs gemma-app | Sonnet | claim type | +8 | -8 to +25 | not yet settled (about 116 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | Sonnet | outside | +33 | +0 to +75 | not yet settled (about 38 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | Sonnet | polarity | -20 | -30 to -9 | ngrams-1938 behind gemma-app |
| ngrams-1938 vs gemma-app | Sonnet | requests | -4 | -17 to +9 | not yet settled (about 363 notes would settle it if the difference held) |
| ngrams-1938 vs gemma-app | Sonnet | dates and amounts | +63 | +49 to +74 | ngrams-1938 beats gemma-app |

**One thing tilts the comparison towards the reader.** Everything the reader chose was chosen on the Codex half of these dev
notes, by the handbook's rule: its route, its epochs and its composite's parts. Gemma was never tuned on them. So the
reader's figures on the Codex half are optimistic. The **Sonnet half** (30 notes, written the way hidden set v3 will be)
is clean for both, and there the reader against Gemma at 1,938 notes reads:
- claim type: +4 points for the route (interval −9 to +19), +8 for the composite (−8 to +25);
- polarity: +3 (−8 to +14);
- requests: −1 (−17 to +15).

Every one is "not yet settled": the same picture, slightly in the reader's favour, but unproven.

**Plainly, at 1,938 notes (the reader as it stands):**
- **Level with Gemma, within the interval, on the three lines that matter most.**
  - Claim type: 92 % against 90 % for the best route, +2 points (interval −9 to +13); the composite has 94 %, +4 (−8 to +16).
  - Polarity: 83 % against 81 %, +2 (−6 to +10).
  - Requests: 72 % against 76 %, −3 (−14 to +7).

  All three are "not yet settled". On the 62 dev notes no difference of this size can be told apart: it would take about
  500 to 1,700 notes.
- **Ahead of Gemma, beyond the interval,** on "outside" and on dates and amounts with their role.
  - Outside: 86 % (route) and 100 % (composite), against 57 %.
  - Dates and amounts: 62 % (route) and 76 % (composite), against 16 %.

**Plainly, at 812 notes (the reader before test 2):**
- **Gemma, as the app runs it, is better than the 812-note reader at polarity and at requests, beyond the interval.**
  - Polarity: 81 % against 70 % for the best route, −12 points (interval −20 to −3).
  - Requests: 76 % against 64 %, −12 points (interval −23 to −1).
- **On claim type, Gemma is ahead too, but not beyond the interval:** 90 % against 83 %, −6 points (interval −20 to +7):
  "not yet settled", about 290 dev notes would settle it.
- **The reader is better at "outside"** (86 % against 57 %, +29 points, interval +7 to +55) **and at dates and amounts
  with their role** (55 % against 16 % for the best route; 70 % for the composite).
  - Gemma's dates and amounts are low partly because of the app's own code, not the model: its date parser reads
    «2022/11/23» as the year 2022 alone.
- **Speed, on this laptop:** Gemma takes about 76 seconds per note (median 57 s for the claim type, 19 s for the facts).
  The reader takes milliseconds.

## 2. The reader with more notes: 1,938 against 812

Same 62 dev notes, same scorer, paired by note. The 1,938 kept training notes include the 812 (a nested subset, seed 7). The fine-tuning was capped at 10 epochs, against 20 at 812 (the owner's cap). Tuning scores on the 8-bit files, by the handbook's rule (Codex half of dev): fine-tuned seeds 0.810, 0.804, **0.839** (812: 0.652, 0.665, 0.683); n-grams 0.723 (812: 0.662). The best route is now the fine-tuned encoder (seed 33), ahead of the n-grams beyond the seeds' spread. The composite at 1,938 takes claim type and values from the n-grams, polarity and requests from the fine-tuned encoder, by the tuning-notes rule.

| Comparison | Writer | Line | Difference (points) | 95 % interval | Verdict |
|---|---|---|---:|---|---|
| finetuned33-1938 vs finetuned33-812 | all | claim type | +8 | +0 to +19 | not yet settled (about 79 notes would settle it if the difference held) |
| finetuned33-1938 vs finetuned33-812 | all | outside | +0 | +0 to +0 | not yet settled (no difference seen) |
| finetuned33-1938 vs finetuned33-812 | all | polarity | +13 | +6 to +20 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | all | requests | +8 | +3 to +15 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | all | dates and amounts | +7 | +1 to +13 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | Codex | claim type | +17 | +4 to +33 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | Codex | outside | +0 | +0 to +0 | not yet settled (no difference seen) |
| finetuned33-1938 vs finetuned33-812 | Codex | polarity | +12 | +2 to +22 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | Codex | requests | +12 | +4 to +21 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | Codex | dates and amounts | +5 | -1 to +12 | not yet settled (about 55 notes would settle it if the difference held) |
| finetuned33-1938 vs finetuned33-812 | Sonnet | claim type | +0 | -12 to +12 | not yet settled (no difference seen) |
| finetuned33-1938 vs finetuned33-812 | Sonnet | outside | +0 | +0 to +0 | not yet settled (no difference seen) |
| finetuned33-1938 vs finetuned33-812 | Sonnet | polarity | +14 | +4 to +24 | finetuned33-1938 beats finetuned33-812 |
| finetuned33-1938 vs finetuned33-812 | Sonnet | requests | +4 | -3 to +13 | not yet settled (about 116 notes would settle it if the difference held) |
| finetuned33-1938 vs finetuned33-812 | Sonnet | dates and amounts | +9 | -1 to +19 | not yet settled (about 38 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | all | claim type | +12 | +2 to +24 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | all | outside | +21 | +0 to +46 | not yet settled (about 72 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | all | polarity | +18 | +8 to +27 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | all | requests | +13 | +5 to +21 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | all | dates and amounts | +6 | +0 to +13 | not yet settled (about 63 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Codex | claim type | +12 | +0 to +27 | not yet settled (about 38 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Codex | outside | +12 | +0 to +43 | not yet settled (about 95 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Codex | polarity | +12 | -3 to +25 | not yet settled (about 46 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Codex | requests | +11 | +2 to +21 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | Codex | dates and amounts | +14 | +6 to +21 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | Sonnet | claim type | +12 | -5 to +31 | not yet settled (about 60 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Sonnet | outside | +33 | +0 to +75 | not yet settled (about 38 notes would settle it if the difference held) |
| composite33-1938 vs composite33-812 | Sonnet | polarity | +23 | +10 to +35 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | Sonnet | requests | +15 | +1 to +27 | composite33-1938 beats composite33-812 |
| composite33-1938 vs composite33-812 | Sonnet | dates and amounts | +0 | -9 to +9 | not yet settled (no difference seen) |
| ngrams-1938 vs ngrams-812 | all | claim type | +12 | +2 to +24 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | all | outside | +21 | +0 to +46 | not yet settled (about 72 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | all | polarity | +2 | -3 to +7 | not yet settled (about 682 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | all | requests | +10 | +4 to +15 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | all | dates and amounts | +7 | +2 to +13 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | Codex | claim type | +12 | +0 to +27 | not yet settled (about 38 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | Codex | outside | +12 | +0 to +43 | not yet settled (about 95 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | Codex | polarity | +3 | -4 to +12 | not yet settled (about 172 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | Codex | requests | +8 | +1 to +15 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | Codex | dates and amounts | +13 | +6 to +19 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | Sonnet | claim type | +12 | -5 to +31 | not yet settled (about 60 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | Sonnet | outside | +33 | +0 to +75 | not yet settled (about 38 notes would settle it if the difference held) |
| ngrams-1938 vs ngrams-812 | Sonnet | polarity | +0 | -7 to +7 | not yet settled (no difference seen) |
| ngrams-1938 vs ngrams-812 | Sonnet | requests | +12 | +3 to +19 | ngrams-1938 beats ngrams-812 |
| ngrams-1938 vs ngrams-812 | Sonnet | dates and amounts | +2 | -4 to +10 | not yet settled (about 302 notes would settle it if the difference held) |

**How the run went.** The first launch (02:33) stopped at once: the encoding step refuses a note already in its cache, and 1,125 of the 1,938 notes were new. Those 1,125 were encoded on their own, and the run was started again at 02:35. The n-grams finished at 02:41 and the three fine-tuning seeds at 04:14, 05:46 and 07:18, on mains. Logs: `dz-core-research\reader\runs\curve-1938.log` and `curve-1938-failed-encode.log`.

## 3. The rules against the app today

**How.** Every case goes through the published rules tool on the final records (`scripts/evidence_rules.py`). What the app
does with each warning was read from its code and data, not guessed:
- **Its 15 formal checks** (definition.json) test only that a part of the petition is present: CPCA 15, 14, 17 and 8, and
  three of its own.
- **Its catalogue:**
  - shows a claim type's prerequisites as one fixed "⚠" line, whatever the facts;
  - offers the required documents as chips to insert;
  - cites articles in the grounds.
- **Nothing in the app** computes a deadline, tests a prior step against the facts, or checks a court or section against
  the case.

**Severity:**
- **A:** the rule's text loses the right or makes the claim inadmissible (a declared block; shown as a warning until the
  rule reaches grade A).
- **A-:** a forfeiture whose reading is not settled.
- **B:** can lose the case if it applies: a limitation the defendant pleads, a possible inadmissibility, the wrong court or
  regime.
- **C:** curable.

**Two sets of 30 fictitious cases.** Set 1 is the final records' own worked examples, which were written to trigger the
rules. Set 2, added by the owner, is dev and pilot scenarios for the nine claim types, drawn by the sampler, which the rule
writers never saw. Each Set 2 case is built as the sampler builds its own evaluator case: the known facts, the requests
and the petition date.

### Set 1: the final records' own worked examples (30 cases)

Warnings raised by the final rules: **48**. 47 let through; 1 checked by the app (presence only).

| Kind | Count |
|---|---:|
| missed deadline | 12 |
| missing prior step | 5 |
| wrong claim type or court | 22 |
| wrong court or section | 9 |

| Severity | Count |
|---|---:|
| A- | 5 |
| B | 43 |

#### Every warning, with its articles

| # | Case | Claim type | Kind | Severity | Deadline | Articles | The app |
|---:|---|---|---|---|---|---|---|
| 1 | bex6 | unpaid-wages | wrong court or section | B |  | cpca 800, labour-disputes 19, labour-relations 3 | article cited in the grounds |
| 2 | bex6 | unpaid-wages | wrong court or section | B |  | cpca 36, cpca 800, cpca 804, labour-relations 3 | nothing |
| 3 | bex6 | unpaid-wages | wrong claim type or court | B |  | labour-relations 73-4, labour-relations 80 | article cited in the grounds |
| 4 | bex6 | unpaid-wages | wrong claim type or court | B |  | cpca 500, cpca 800, labour-relations 3 | nothing |
| 5 | ex10 | abusive-dismissal | wrong court or section | B |  | cpca 500, cpca 800 | nothing |
| 6 | ex10 | abusive-dismissal | wrong court or section | B |  | cpca 800, labour-disputes 19, labour-relations 3 | article cited in the grounds |
| 7 | ex10 | abusive-dismissal | wrong claim type or court | B |  | cpca 500, cpca 800, labour-relations 3 | nothing |
| 8 | exA5 | unpaid-rent | missed deadline | B | 10 days left | civil-code 3, civil-code 314, civil-code 321, commercial-code 169 | nothing |
| 9 | exA5 | unpaid-rent | wrong claim type or court | B |  | cpca 534 | nothing |
| 10 | exA5 | unpaid-rent | wrong claim type or court | B |  | commercial-code 187 مكرر 1, commercial-code 195, cpca 40, cpca 512 | nothing |
| 11 | ex2 | nafaqa | missed deadline | B | passed 627 days ago | family-code 80 | article cited in the grounds |
| 12 | ex19 | divorce-by-husband | wrong claim type or court | B |  | family-code 48, family-code 53, family-code 54 | article cited in the grounds |
| 13 | ex19 | divorce-by-husband | wrong claim type or court | B |  | family-code 30, family-code 32, family-code 33, family-code 51 | nothing |
| 14 | ex19 | divorce-by-husband | wrong claim type or court | B |  | civil-code 12, civil-code 13, civil-code 22, civil-code 24 | nothing |
| 15 | ex2 | tatliq | missed deadline | B | passed 629 days ago | family-code 80 | nothing |
| 16 | ex3 | khul | missed deadline | B | passed 627 days ago | family-code 80 | nothing |
| 17 | vf1 | custody | missed deadline | A- | passed 2305 days ago | family-code 68 | nothing |
| 18 | vf1 | custody | wrong claim type or court | B |  | family-code 62, family-code 64, family-code 68 | article cited in the grounds |
| 19 | ex2 | custody-forfeiture | missed deadline | A- | passed 677 days ago | civil-code 3, family-code 68 | article cited in the grounds |
| 20 | ex2 | custody-forfeiture | wrong claim type or court | B |  | family-code 72 | nothing |
| 21 | vex6 | unpaid-wages | missed deadline | B | passed 2058 days ago | civil-code 312, civil-code 313, civil-code 314, civil-code 315 | nothing |
| 22 | vex6 | unpaid-wages | missed deadline | B | passed 597 days ago | civil-code 309, civil-code 313, civil-code 314, civil-code 315 | nothing |
| 23 | ex16 | abusive-dismissal | wrong court or section | B |  | cpca 536 مكرر, law-22-13 13 | nothing |
| 24 | exA2 | unpaid-rent | missed deadline | B | passed 931 days ago | civil-code 309, civil-code 313, civil-code 314, civil-code 315 | nothing |
| 25 | ex21 | nafaqa | wrong court or section | B |  | civil-code 339, cpca 39 | nothing |
| 26 | ex2 | divorce-by-husband | wrong claim type or court | B |  | cpca 423, cpca 426, family-code 22 | article cited in the grounds |
| 27 | ex5 | tatliq | missing prior step | B |  | family-code 53 | article cited in the grounds |
| 28 | ex5 | khul | missing prior step | B |  | family-code 22 | nothing |
| 29 | ex7 | custody | wrong claim type or court | B |  | family-code 66, family-code 67, family-code 69, family-code 71 | nothing |
| 30 | ex7 | custody | wrong claim type or court | B |  | family-code 64, family-code 66, family-code 67, family-code 68 | article cited in the grounds |
| 31 | ex4 | custody-forfeiture | missed deadline | A- | passed 992 days ago | civil-code 3, family-code 65 | nothing |
| 32 | vex3 | unpaid-wages | wrong court or section | B |  | cpca 536 مكرر, law-22-13 13 | nothing |
| 33 | ex11 | abusive-dismissal | wrong claim type or court | B |  | labour-relations 73, labour-relations 73-2, labour-relations 73-4 | article cited in the grounds |
| 34 | exA14 | unpaid-rent | missing prior step | B |  | commercial-code 191 | nothing |
| 35 | exA14 | unpaid-rent | wrong claim type or court | B |  | cpca 534 | nothing |
| 36 | exA14 | unpaid-rent | wrong claim type or court | B |  | commercial-code 187 مكرر 1, commercial-code 195, cpca 40, cpca 512 | nothing |
| 37 | ex5 | nafaqa | wrong claim type or court | B |  | family-code 58, family-code 60, family-code 61 | nothing |
| 38 | ex4 | divorce-by-husband | wrong claim type or court | B |  | cpca 53, cpca 54, cpca 55, cpca 56 | nothing |
| 39 | ex6 | tatliq | missing prior step | B |  | family-code 22 | nothing |
| 40 | ex27 | khul | missed deadline | A- | passed 64 days ago | family-code 68 | nothing |
| 41 | ex8 | custody | wrong claim type or court | B |  | cpca 442, family-code 57 مكرر | nothing |
| 42 | ex8 | custody | wrong claim type or court | B |  | cpca 453, cpca 455, cpca 456, cpca 458 | article cited in the grounds |
| 43 | ex5 | custody-forfeiture | missed deadline | A- | 13 days left | civil-code 3, family-code 65, family-code 7 | nothing |
| 44 | ex5 | custody-forfeiture | wrong claim type or court | B |  | family-code 72 | nothing |
| 45 | vex4 | unpaid-wages | wrong court or section | B |  | civil-code 339, cpca 39 | nothing |
| 46 | ex7 | abusive-dismissal | wrong court or section | B |  | cpca 800, labour-disputes 19, labour-relations 3 | article cited in the grounds |
| 47 | ex7 | abusive-dismissal | wrong claim type or court | B |  | cpca 500, cpca 800, labour-relations 3 | nothing |
| 48 | exA17 | unpaid-rent | missing prior step | B |  | cpca 17, cpca 515, cpca 519 | app formal check art17.publication |

### Set 2: dev and pilot scenarios the rule writers never saw (30 cases)

Warnings raised by the final rules: **5**. 5 let through.

| Kind | Count |
|---|---:|
| missed deadline | 2 |
| wrong claim type or court | 3 |

| Severity | Count |
|---|---:|
| A | 2 |
| B | 3 |

#### Every warning, with its articles

| # | Case | Claim type | Kind | Severity | Deadline | Articles | The app |
|---:|---|---|---|---|---|---|---|
| 1 | s-dev-00061 | nafaqa | wrong claim type or court | B |  | family-code 53 | nothing |
| 2 | s-dev-b2-00041 | unpaid-wages | missed deadline | A | passed 978 days ago | cpca 322, cpca 405, cpca 504, cpca 67 | nothing |
| 3 | s-dev-b2-00041 | unpaid-wages | wrong claim type or court | B |  | labour-relations 73-4, labour-relations 80 | article cited in the grounds |
| 4 | s-pilot-00206 | unpaid-wages | missed deadline | A | passed 218 days ago | cpca 322, cpca 405, cpca 504, cpca 67 | nothing |
| 5 | s-pilot-00206 | unpaid-wages | wrong claim type or court | B |  | labour-relations 73-4, labour-relations 80 | article cited in the grounds |



**Plainly:**
- **On the worked examples,** the final rules raise 48 warnings and the app checks one of them, the land-registry
  publication, and only for presence.
- **On the scenarios the rule writers never saw,** the rules raise 5 warnings, in 3 of the 30 cases, and the app misses
  all 5. Two of them are the errors that lose a case outright: the six-month time limit of CPCA 504 for a labour claim,
  passed by 978 and 218 days.
- **On those same cases the rules mostly ask:** 402 questions, about 13 per case, because the facts leave what the rules
  need unknown (dates of notification, whether conciliation took place). How many real errors the rules catch therefore
  depends on the lawyer answering the questions.
