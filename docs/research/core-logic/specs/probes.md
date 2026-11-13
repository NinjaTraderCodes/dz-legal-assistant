# Specification: the probes for near-copies, leakage and realism

**Why.** A test set made by models can flatter a classifier in three ways that no accuracy figure shows: the same note sits, slightly changed, in training and in test; the label leaks into the wording (every disputed note contains «ينكر»; every unpaid-wages note opens with the same phrase); the notes do not look like what a lawyer writes. Each probe below turns one of those into a number, reported with every score.

All probes are scripts in `DzLegal.CoreLogic.Notes` (or Python beside it), with tests on small made-up sets where the right answer is known. They run on pilot, train, dev, challenge, cross-check, clean and composed, and on the mirror set once it is built. **They never run on hidden v3 from outside**: the same code runs inside the `v3 probes` command, which prints near-copy counts only ([hidden-v3-commands.md](hidden-v3-commands.md)). Every open set written after v3 is finalised passes `v3 probes --against` before it is used.

## 1. Near-copies

- **Normalise** each note for comparison: level 2 of the text comparison ([article-store.md](article-store.md)), then replace every date, amount and cast name by a placeholder (`<D>`, `<A>`, `<N>`), so that two notes that differ only in their values count as the same.
- **Measure:** Jaccard similarity on character 5-grams of the normalised `facts` (MinHash with 128 permutations for the all-pairs search; exact Jaccard on the candidates). Also the longest common run of words.
- **Thresholds:** a pair at 0.80 or more, or sharing a run of 25 words or more, is a **near-copy**. Between 0.60 and 0.80: listed for a look. These are working values: change them only on the evidence of the probe's own test or of the pilot, and report the change.
- **Rules:** within a set, keep one note of each near-copy group. **Across a split, a near-copy is a leak**: remove it from the training side, and count it. Report, per pair of sets, the number of near-copy pairs before and after, and the highest similarity that remains.
- **Test of the probe:** plant 20 copies with changed names and dates, and 20 paraphrases at about half overlap; the probe must flag all of the first and none of the second.

## 2. Leakage of the label into the wording

A model that can guess the label from surface features, without reading, shows that the notes carry a shortcut.

**2a. The shallow classifier.** Train a linear model on word unigrams and bigrams (and, separately, on character 3- to 5-grams) for each label: claim type; for each element, its polarity. Cross-validate by scenario family. This model is also step 4's baseline, so it costs nothing extra. On hidden v3 it is trained only by cross-validation inside v3, and its accuracy there appears only in the run's `summary.md`: no accuracy of any model on v3 is printed before the run. Report its accuracy, and for each label **the 20 features with the largest weights**. Read them:
- content words of the claim («أجور», «إيجار») are expected for the claim type: that is the task;
- for **polarity**, the top features should be the many forms of negation and dispute. If one verb carries a class (say «ينكر» alone gives 70 % of `disputed`), the style cards' "dispute manner" is not doing its work: fix the sampler, regenerate, and show the before and after;
- any feature that is **not language about the fact** is a leak: a name, a place, a formatting habit, a position ("the first sentence"), the note's length.

**2b. Style against label.** For each style parameter and each label, the mutual information (or simply the table). A style that predicts a label is a sampler bug.

**2c. Length and position.** The label predicted from the note's length alone, and from the index of the sentence that carries the evidence. Report both accuracies against the majority-class rate.

**2d. Masking.** Remove the quoted evidence of one event from the note (replace by a neutral placeholder) and ask the trained classifier for that event's polarity. It should fall to `notMentioned`. If it still answers `disputed` at a high rate, it is reading something else. Report the rate per polarity. (This is a probe of the classifier and of the data together; it runs in step 4 on dev.)

**2e. Writer identity.** A shallow model that tells Codex-written from Claude-written from local-written notes. It will succeed to a degree (models have manners). Report its accuracy, and check that **the writer does not predict the label**: every set must hold every label from every writer it uses.

## 3. Realism

No lawyer reads the notes, so realism is judged by the other family, blind, against a short description of the target, and by comparison with the few notes written for the app before this work.

- **Cross-family review:** 200 notes per set (100 if the owner takes that saving), each read by the family that did not write it ([../prompts/notes-realism-review.md](../prompts/notes-realism-review.md)). The reviewer scores 1 to 5 on: reads like a working note, not like a judgment or a textbook; formal Arabic a lawyer would write; no label word used as a label («الواقعة المنكرة»); details hang together. And lists the defects it sees. Report the distribution and the 20 most frequent defects; the defects feed the style cards.
- **The anchor:** the app's own fictitious cases, written for the app before this work and not by this pipeline: the 14 formal-Arabic drafting cases among `tests/drafting-cases*` and `src/DzLegal.Web/Samples` (16 files, less `02-debt-french-notes.json` and `03-rent-darija-notes.json`), and the facts of `tests/fact-cases/cases.json` (less the two Darija cases of its `robustness` partition). Compare simple statistics of the generated notes with them: length, sentences per note, share of nominal sentences, digits per 100 words, share of notes with a `documents` list. Large gaps are reported, not hidden. These cases are also one of the test sets ([notes-pipeline.md](notes-pipeline.md), section 7), scored with what their labels allow and reported beside every synthetic score. They are too few to carry a verdict.
- **Blind odd-one-out:** mix 10 of the drafting cases' notes among 90 generated ones and ask each family to pick the 10 not written by the same hand as the rest. If they are picked far above chance, the generated notes have a recognisable manner; report the hit rate.

## 4. The label-noise estimate (mirror set only)

The mirror set is built like v3, in v3's proportions, after the generation method is frozen: a planner writes quotas, isolated Sonnet writers invent their cases and write the notes, Codex relabels them blind, disagreements are dropped ([hidden-v3-commands.md](hidden-v3-commands.md), section 7). On it, and only on it, a **third blind judge (Opus 5.5, no tool)** labels every kept note. Two figures come from it, each a plain proportion with its Wilson 95 % interval:

- **The label-noise figure**, when the set is built: the share of kept notes where the third judge differs from the agreed label, per field. An `unclear` from the third judge counts as a difference, and is also reported apart. It estimates the wrong labels that survive agreement. If it is 3 %, a 95 % target on v3 is measured with a ruler whose error is of the size of the gap, and the report must say so.
- **The drop-flattery figure**, once, at the pre-commitment, for the at most two final candidates: their score on the dropped mirror notes against the kept ones. A dropped note has no agreed gold, so it is scored on the writer's labels, on the fields where writer and Codex agreed. The gap says how much dropping the hard notes flatters the score.

The mirror set chooses nothing ([notes-pipeline.md](notes-pipeline.md), section 11). Both figures are quoted beside the v3 result.

## What goes in every score table

Beside each accuracy: the number of notes; the 95 % interval (Wilson); the near-copy count across the split; the shallow model's accuracy on the same set (the "no reading" floor; for v3, only in `summary.md`); and, for v3, the two mirror figures above. The headline is the worst test set among those with at least 200 notes; smaller sets are reported beside it with their intervals.

## Traps

- **A clean probe is not proof of a clean set.** The probes find the leaks someone thought of. Say what was probed, not "no leakage".
- **Do not fix a leak by deleting the notes that show it** and keeping the generator: the next batch will leak again. Fix the sampler or the prompt, regenerate, re-probe.
- **Never copy a hidden note's text out to "probe it by hand".** Counts only.
