# Baseline of the current analysis path (26 October 2026)

This is the reference the rebuilt legal core (`docs/CORE-REBUILD-PLAN.md`) must beat. It was measured on the code as of `8f52e03`; the analysis code has not changed since `543a518`.

## What was run

On the team's Windows laptop:
- **Local:** `grounds --models gemma3:4b` on the 46 fact patterns of `tests/grounds-cases/cases.json`, one run, Ollama on loopback, CPU only.
- **Online:** `grounds --models openrouter --runs 3`, the same model family (Gemma 3 4B) served online. Fast, but not the product's runtime.

The drafting baseline (`draft --models gemma3:4b`) has **not** been run yet. It is needed before slice E changes the drafting.

## Results

| | Local `gemma3:4b` | Online, run 1 / 2 / 3 |
|---|---|---|
| First claim type exact | 44/46 (96%) | 96% / 100% / 100% |
| Expected articles proposed (core recall) | 75% | 75% / 78% / 79% |
| Expected articles the model chose itself | 66% | 66% / 69% / 71% |
| Out-of-scope cases declined | 4/4 | 3/4 / 4/4 / 4/4 |
| Seconds per case | **113** | 8 / 8 / 9 |

**Local misses:**
- `loan-acknowledgment` → unpaid-sale-price;
- `commercial-supplies` → unpaid-sale-price.

**Where the local time goes** (averages per call):

| Call | Prompt tokens | Prompt read | Answer tokens | Writing | Time to first word |
|---|---:|---:|---:|---:|---:|
| reading | 1,269 | 23.8 tok/s | 126 | 5.4 tok/s | 53.9 s |
| selection | 663 | 24.4 tok/s | 84 | 6.0 tok/s | 27.5 s |

**About 80 of the 113 seconds are spent reading the prompt before the first word.** Most of the reading prompt is the same in every case (instructions and catalogue). A stable prefix that Ollama can reuse is the first speed lever.

## How to read these numbers

- **The 46 cases were written by the team and used to tune the analysis.** They measure progress against ourselves, not legal accuracy on unseen cases (see the audit, §6).
- **"Core recall" is measured against the catalogue's own answers.** Two of them are now known to be incomplete:
  - the dog case should cite Civil art. 139, not 138;
  - the wall case should cite art. 140.

  Since this baseline, the cases carry a separate legal reference (`required`), and the bench reports such articles as **coverage gaps** when the library lacks them. The figures above use the old reference only.
- **Runs from now on write `manifest.json`** (commit, fingerprints of the law data, prompts and cases, options). Only runs with comparable manifests should be compared.

## Files

- `grounds-local/summary.md`, and `grounds-local/cases/` (one trace per case);
- `grounds-online/summary.md` (the stability table over 3 runs), and `summary-run-1..3.md`.
