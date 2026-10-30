# Slice B, first attempt (commit `48ec080`): faster, but less accurate

**What changed against the 26 October baseline:**
- the whole claim catalogue was offered whatever the court section (the section became a hint);
- the instructions came before the case, and the case came last, so the shared beginning could be reused;
- the choice of articles continued the reading conversation.

**Results:**

| | Baseline (26 Oct), local | This run, local | Baseline, online (3 runs) | This run, online (3 runs) |
|---|---|---|---|---|
| First claim type exact | 96% | **76%** | 96 / 100 / 100% | 93 / 91 / 93% |
| Core recall | 75% | 60% | 75–79% | 66% |
| Seconds per case | 113 | **80** | 8–9 | 9–10 |
| Reading call: time to first word | 53.9 s | **22.4 s** | | |
| Selection call: time to first word | 27.5 s | 26.9 s | | |

**What this shows:**
- **The reuse of the shared beginning works.** The reading call reaches its first word 2.4 times sooner. The selection call kept its time although its prompt grew from 663 to 2,419 tokens, so the conversation is reused.
- **Accuracy dropped, mostly from two things:**
  - **Confusions inside one area** (khul → nafaqa, tatliq → nafaqa, visitation → custody, contract damages → tort). These point to the instructions having moved before the case.
  - **Confusions across areas** (khul → loan, a company case → rescission). These point to the whole catalogue being offered.

**Corrected in the next commit:**
- **The court section's claims are offered again.** If the model answers «nothing applies», it is asked once more with every claim, so a wrong section cannot hide the right claim.
- **The instructions go back after the case,** where the small model follows them best.
- **Kept:** the task and the claim list still come first, identical for every case of a section, and the article choice still continues the conversation.

The drafting baseline (`draft --models gemma3:4b`, 5 cases) is in `docs/baselines/2026-10-26/draft-local/`.
