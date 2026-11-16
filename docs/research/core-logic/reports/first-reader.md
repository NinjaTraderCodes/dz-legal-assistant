# The first rough reader (16 to 17 November 2026)

**What the owner asked (16 November):** "Use [the pilot notes], as they passed the code checks, to train a first rough reader now, and score it on development notes it never saw (never the hidden sets). The first score comes before the 4,000 notes are written."

## What it was trained and scored on

- **Training:** the 493 pilot notes kept on the code checks (`notes accept`), written by Codex. 499 were written; the 6 hit by sampler 1's role defect were dropped by script (`notes refill`).
- **Tuning (early stopping, and the one setting chosen):** the Codex half of the development notes, **30 notes**.
- **Measured:** all **54** kept development notes, 30 written by Codex and 24 by Sonnet. 100 were drawn and 91 written; the blind relabel dropped 34 and the role defect 3. The Sonnet half only measures.
- **Reader:** CAMeLBERT-mix, 8-bit, frozen, with its vectors cached through the .NET tokenizer (`reader encode`). The heads of `specs/reader.md`, rung 4, with the correction below; the phase B decoder (no abstention); focal loss; three seeds (11, 22, 33).

## Two things that went wrong first, and what was changed

1. **The spec's claim-type and request heads could not learn.** With every setting of the spec, the reader called all 54 notes `outside` and predicted no request.
   - The cause is in the design: each class was scored by **one shared** layer on `[v; q; c]`. The note vector `c` is the same for every class, and the query `q` adds only a fixed bias.
   - The cached vectors were not at fault. A plain logistic regression on the note vectors reached 60 to 67 % on the 30 tune notes. The spec's heads reached 12 to 20 % on their own training notes, even with four times more steps.
   - **Changed:** each claim type and each request kind now has its own weights on `[v; c]`. Trained on the claim loss alone, these reached 88 % on the training notes. `specs/reader.md` says so in the two rows.
2. **Batch and rate.** 493 notes at 32 a batch give 16 steps an epoch. The two settings were compared as the spec requires: three seeds each, on the tune notes, means compared beyond the spread.

   | Setting | Tuning score, seeds 11 / 22 / 33 | Mean | Spread |
   |---|---|---:|---:|
   | batch 32, rate 1e-3 (the spec's) | 0.365 / 0.270 / 0.376 | 0.337 | 0.106 |
   | **batch 8, rate 3e-3 (chosen)** | 0.522 / 0.533 / 0.537 | **0.531** | 0.015 |

## The first score

The mean of the three seeds of the chosen setting, on the 54 development notes. "Type given" means the reader is told the claim type, as the app does after the lawyer confirms it.

| Line | Codex half (30) | Sonnet half (24) | Both | Target |
|---|---:|---:|---:|---:|
| claim type, in-catalogue notes (end to end) | 10.3 of 23 (45 %) | 4.7 of 18 (26 %) | 15.0 of 41 (37 %) | 95 % |
| outside recognised | 7 of 7 (100 %) | 3 of 6 (50 %) | 10 of 13 (77 %) | 95 % |
| polarity, mentioned elements (type given) | 36.3 of 58 (63 %) | 27.3 of 52 (53 %) | 63.7 of 110 (58 %) | 95 % |
| polarity, absent elements (type given) | 13.3 of 26 (51 %) | 5.7 of 13 (44 %) | 19.0 of 39 (49 %) | 95 % |
| requests (type given) | 18.0 of 51 (36 %) | 8.3 of 31 (24 %) | 26.3 of 82 (31 %) | 90 % |
| dates and amounts with their role (type given) | 22.7 of 78 (29 %) | 22.0 of 72 (31 %) | 44.7 of 150 (30 %) | 90 % |
| slot notice_served, known values | 6 of 6 | 5 of 6 | 11 of 12 (92 %) | 90 % |
| slot public_body_party, known values | 20.7 of 21 | 17 of 17 | 37.7 of 38 (99 %) | 90 % |
| slot contract_date, known values | 0.3 of 5 | 0 of 4 | 0.3 of 9 (4 %) | 90 % |
| slot notice_date, known values | 0 of 5 | 0 of 3 | 0 of 8 (0 %) | 90 % |

**Read it as a first measurement, not a verdict.**
- The training set is an eighth of the planned 4,000 notes.
- The tune set is 30 notes, so each line's interval is wide: the scorer's own intervals are in each run's `summary.md`.
- The reader still trails the plain logistic regression on the claim type: 45 % against 60 to 67 % on the same 30 notes.
- The roles of dates and amounts are weak, and the date slots, which come from those roles, are at zero.
- The Sonnet half scores below the Codex half on most lines. That is the style gap the handbook asks to report; it decides nothing.

## Not done yet, in order

1. The reader's own flags (the owner's route): cross-validation on the training notes, and the notes where the reader confidently disagrees with the writer's label are dropped. That waits for a reader whose answers mean something; this one is too weak to flag with.
2. The guide's three gaps found on 16 November: words that only presuppose an element; a party who admits part of an event; a side request in a family note. Each is fixed in the guide, the guide's version is bumped, and the guide test reruns on the items it touches.
3. The retries and refills: one Codex call (3 development retries, 6 pilot and 3 development refills), and one Sonnet run (6 retries), with their relabel.
4. The noise measurement and the pilot report with the calendar: the stop point.

Runs (outside the repository): `C:\Users\TheShadow\dz-core-research\reader\runs\` (`first-pilot493`, `diag-20epochs`, `perclass-b32`, `perclass-b8`). Each holds `run.json` (settings, history, package list) and, per epoch, the readings and the scorer's summary.
