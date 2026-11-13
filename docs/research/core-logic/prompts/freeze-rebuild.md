# Rebuilding the results from the freeze document (step 6)

- **Model:** Sonnet 5.5, fresh agent, in a **clean clone** of the branch at the freeze commit (`git clone` then `git checkout <commit>`), in a new folder outside the worktree: `C:\Users\TheShadow\dz-core-research\rebuild\clone`.
- **Why Sonnet and not Opus:** the freeze document must be followable by someone who did not do the work. If the lighter model can follow it to the same figures, it is clear enough; if only its author can, it is not a freeze.
- **Placeholders:** `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`: the original, named so that the agent knows it is off limits), `{{clone_path}}` (`C:\Users\TheShadow\dz-core-research\rebuild\clone`), `{{freeze_doc}}` (`docs/research/core-logic/FREEZE.md` in the clone), `{{report_path}}` (`C:\Users\TheShadow\dz-core-research\rebuild\report.md`, outside the clone), `{{inputs}}`: the absolute paths `FREEZE.md` names for what the clone does not hold, each with its SHA-256: the open note sets; the model files, token vectors and checkpoints (`C:\Users\TheShadow\dz-core-research\models\…`, never committed); the Python environment (`C:\Users\TheShadow\dz-arch-research\venv`, used as it is); the local NuGet cache holding the listed package versions.
- **What `FREEZE.md` must make possible, because the clone has no model file:**
  - `dotnet build` and `dotnet test` green in the clone without any model file (unit tests use the committed fixture model);
  - each model file found at its path with the SHA-256 the document gives, and each score rebuilt by re-scoring with it;
  - the shipped model retrained once from its command, its scores within the stated spread over seeds (other trained figures are checked from the stored files, not retrained);
  - the scorer's hash and the reader's source hash, recomputed over the committed sources as [../specs/scorer.md](../specs/scorer.md) defines them (built programs are not hashed);
  - timings rebuilt only on mains with Best performance; otherwise "not rebuilt: conditions".
- **It never touches:** the hidden sets (the v3 figures are checked only by comparing the committed `summary.md` with what `FREEZE.md` quotes), the original worktree, any paid service, any download not listed in `FREEZE.md`.
- **Output:** `{{report_path}}`: for every figure of `FREEZE.md` that can be rebuilt from open data: the command run, the figure obtained, equal or not; every step where the document was unclear, wrong or silent; every file whose hash differs.
- **Done when:** every open figure is rebuilt equal (identical for deterministic steps; within the stated spread over seeds for training), or the difference is explained and `FREEZE.md` is corrected and the rebuild run again from the top.

--- PROMPT ---

**Where you work.** Your clean copy of the repository is `{{clone_path}}`. Your shell may start elsewhere: use absolute paths. You may change files only inside `{{clone_path}}` (and only by running the document's commands) and write one file, your report, at `{{report_path}}`. You may read, without changing them, only these paths outside the clone: {{inputs}}. Everything else is off limits, and in particular: the original worktree `{{worktree}}`; any memory file (read none, write none); the files under `C:\Users\TheShadow\.claude\`, including any plan file; the state file `C:\Users\TheShadow\dz-core-research\STATE.md` and everything under `C:\Users\TheShadow\dz-core-research\` not named above; other git branches; and anything under `C:\dz-hidden*`.

You are checking that a piece of research can be reproduced by someone who did not do it. You have a clean copy of the repository and one document: `{{freeze_doc}}`. Follow that document, and only that document, and see whether you reach the figures it states.

**Rules:**

1. Do exactly what the document says, in its order, with its commands. Do not use anything you know about the project from elsewhere, do not read the handbook or other reports to fill a gap, and do not fix a broken step by guessing: when the document is unclear, wrong or silent, **write that down and stop that branch of the work**, then continue with the steps that do not depend on it.
2. Do not open anything under `C:\dz-hidden*`. Figures that come from a hidden set are checked only by comparing the committed `summary.md` files with what the document quotes from them.
3. No network, except a download the document lists with its URL and hash. No account, no key, no payment. Remove `OPENROUTER_API_KEY` and `GOOGLE_API_KEY` from the environment of every shell you start.
4. Run builds and tests on an idle machine, and never while a training or a timing runs; run timings only if the document's conditions (mains power, Best performance) hold, and otherwise mark them "not rebuilt: conditions".
5. Change nothing in the clone except what the commands themselves write. Copy no model file into the clone.

**For each figure the document states** (a score, a count, a hash, a size, a time): the command you ran; what you obtained; "equal", "within the stated spread", or "different" with both values.

**Also check:** every file hash the document lists, including each model file outside the clone; that the build has 0 warnings and every test passes for both solutions in the clone, with no model file in it; that each data set has the number of notes stated; that the scorer's hash matches.

Write your report to `{{report_path}}`: first the table of figures; then, in order, every place where the document failed you, quoting the sentence and saying what was missing. Be exact and unsparing: the purpose is to find where the document is not enough.

**Your reply:** the number of figures rebuilt equal, within spread, different and not rebuilt; and the three most serious gaps in the document.
