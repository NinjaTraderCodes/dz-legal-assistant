# The route at the first point of the learning curve: 812 training notes (18 November 2026)

**What the owner asked** (17 November, at the pilot's stop point): decide the route at the 1,000-note point; "train the n-gram classifier, the fine-tuned encoder (small setting only, three seeds, one evening on mains), and a combination where the n-grams decide claim type and outside and the fine-tuned model the rest. Report every line per writer." And: run it on whatever is kept once the window's calls are in, if at least 800. **812 kept training notes** (sampler 1.3, the wider style, guide 1.3, Codex prompt v3 with four framings) were in at 01:38.

**The noise of those notes:** 0.3 % (reports/noise-train-812.md), far under the 10 % bar: the results below stand.

**How it was run** (`exec\curve_point.sh 812`, on mains, 01:41 to 04:42): nested export of the first 812 kept training notes; tuning on the Codex half of dev (32 notes), measuring on all of dev (62 notes: 32 Codex, 30 Sonnet). The fine-tuned encoder: the top two layers and the heads, 32-bit, then the 8-bit export, the .NET cache and a 5-epoch head refit; **only the 8-bit scores count**. The combination: the n-grams' claim answer (and outside) with the fine-tuned reader's reading for those claim types. The cue hybrid: the cue table decides a value's role where it can, the model fills the rest.

## The selection rule (tuning score, Codex dev)

| Route | Tuning score |
|---|---:|
| n-gram classifier (C = 10) | 0.662 |
| fine-tuned encoder, mean of 3 seeds (0.652, 0.665, 0.683) | **0.667** (spread ± 0.016) |

**The fine-tuned encoder is not ahead beyond the spread of its seeds: the rule chooses no route on the tuning score.** At 493 pilot notes it was 0.742 on one seed; with three seeds on 812 notes it is level with the n-grams.

## Every line per writer (dev, 62 notes; Codex half / Sonnet half; mean of 3 seeds with half the range)

| Route | Claim type | Outside recognised | Polarity (mentioned) | Requests | Dates and amounts with role |
|---|---|---|---|---|---|
| n-gram classifier | 79 / 83 | 88 / 67 | 68 / 63 | 57 / 61 | 67 / 72 |
| n-grams + cue hybrid | 79 / 83 | 88 / 67 | 68 / 63 | 57 / 61 | 65 / 74 |
| fine-tuned encoder | 72 / 89 (± 2 / 4) | 92 / 78 (± 6 / 8) | 69 / 77 (± 2 / 6) | 53 / 69 (± 2 / 3) | 50 / 48 (± 5 / 4) |
| fine-tuned + cue hybrid | 72 / 89 | 92 / 78 | 69 / 77 | 53 / 69 | 53 / 60 (± 4 / 2) |
| combination (n-grams' claim type, fine-tuned rest) | 79 / 83 | 88 / 67 | 69 / 77 | 53 / 69 | 50 / 48 |
| combination + cue hybrid | 79 / 83 | 88 / 67 | 69 / 77 | 53 / 69 | 53 / 60 |

Outside: 8 Codex and 6 Sonnet outside notes, so one note moves a figure by 12 to 17 points.

## What it says

1. **The writer gap has turned.** The fine-tuned encoder is now better on the **Sonnet** half, the half written like the hidden set: claim type 89 against 83, polarity 77 against 63, requests 69 against 61. The wider style of the 4,000 (sampler 1.3, four framings) is the likely reason, since the pilot's heads were 26 % on Sonnet claim types.
2. **The n-grams keep two lines:** the claim type on the Codex half (79 against 72), and dates and amounts with their role, by a wide margin (65 / 74 against 53 / 60 with the cues).
3. **No single route wins every line.** The pieces that win each line are: the claim type from either (the n-grams on Codex notes, the fine-tuned model on Sonnet notes); polarity and requests from the fine-tuned model; value roles from the n-grams with the cue table. That composite was not run; it is the obvious next measurement.
4. **Three seeds at one point decide nothing for good.** The curve's next points (2,000 and 4,000) will show which route grows with data.

**Proposal for the owner** (not decided here): keep both routes to the 2,000-note point, and add the composite of point 3 there (no new model: a different way of joining the two readers' outputs).

Files (outside the repository): `reader\runs\curve-812\` (every summary), `reader\runs\curve-812.log`; the first two attempts' logs (`curve-812-failed-*.log`, `curve-812-run1.log`) record a tune-export option, a cache path and a re-encoding fault of the script, each fixed before the run that counts.
