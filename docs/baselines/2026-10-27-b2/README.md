# Slice B, second attempt (commit `c889ec7`), and the decision to return to the baseline analysis

**What changed against the first attempt (`48ec080`):**
- the court section's claims were offered again, with a retry over every claim on «nothing applies»;
- the instructions came after the case;
- formal Arabic only in the scores (44 cases; the two Darija cases were reported apart).

**Results.** The baseline row is the 26 October baseline counted on the same 44 formal-Arabic cases.

| | Baseline, local | B1, local | **B2, local** | B2, online (runs 1 / 2) |
|---|---|---|---|---|
| First claim type exact | 42/44 (95%) | 76% (46 cases) | **39/44 (89%)** | 91% / 93% |
| Out-of-scope cases declined | 4/4 | 3/4 | **2/4** | 3/4 / 3/4 |
| Seconds per case | 113 | 80 | **103** | 11 |
| Reading: time to first word | 53.9 s | 22.4 s | 34.8 s | |
| Selection: time to first word | 27.5 s | 26.9 s | 30.9 s | |

Online run 3 is left out: the service failed three cases in a row and the run stopped early.

**What this shows:**
- **Order matters to the 4B model.**
  - With the claim list before the case (needed so the beginning can be reused), its claim recognition is worse than with the case first (the baseline), whatever we put after it.
  - Most of the speed gain came from exactly that order, so on this prompt speed and accuracy pull against each other.
- **The retry over every claim hurts out-of-scope cases.** An inheritance case was retried and then misread as maintenance, taking 178 s.
- **Continuing the conversation for the choice of articles saved no time.** The selection's new part (candidates and instructions) is as long as the old prompt was.
- **Both Darija cases were right** (robustness group).

**Decision (27 October):**
- **The analysis returns to the baseline prompts and logic** (the case first, the court section's claims, no retry).
- **Kept:** the conversation support in the providers (`ModelRequest.Earlier`), the run manifests, the legal reference in the cases, and the formal-Arabic scope.
- **The reuse of a shared beginning will be designed into the new pipeline from the start** (fact extraction, then condition checking). There, the shared part can be the instructions and examples rather than the claim list, and each step is measured for accuracy and speed together.
