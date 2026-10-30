# Branches

## Rules

1. **The default branch is `claude/stoic-fermat-mi1gko`. It holds the app as it ships.** Only tested, finished work is merged into it: 0 build warnings, and every test passing.
2. **One branch per workstream**, named `<who>/<topic>`, for example `claude/alpha-01`, `codex/law-corpus` or `djihane/onboarding`.
3. **To bring the default branch into a workstream branch, merge it.** Never rebase or force-push a branch someone else works on.
4. **A finished experiment is tagged `archive/<topic>` and its branch is deleted.** The tag keeps every commit, so a piece can still be taken from it later.
5. **Nothing experimental goes into the default branch.** A trial stays on its own branch until its measurements say it should be kept (for example `docs/ALPHA-01.md`, "Decision rule").

## Branches (30 Oct 2026)

| Branch | Owner | State |
|---|---|---|
| `claude/stoic-fermat-mi1gko` | — | Default branch. Alpha-01 (harness only), the family claims and the onboarding merged on 30 Oct. |
| `claude/family-claims` | Claude | Merged on 30 Oct: the complete Family Code and CPCA, and the family claims (`docs/FAMILY-CLAIMS.md`). Can be deleted. |
| `claude/alpha-01` | Claude | Merged; kept until the hidden set is run on its frozen version. |
| `codex/law-corpus` | Law agent | Active: Family Code and CPCA delivered (in the app since 30 Oct); next the Civil Code. Its code areas are its own (see `AGENTS.md`). |
| `djihane/onboarding` | Djihane | Done: merged on 30 Oct through `claude/onboarding-fixes`, her 7 commits under her name. Can be deleted. |
| `claude/onboarding-fixes` | Claude | Merged on 30 Oct: the fixes to the onboarding before merging (`docs/ONBOARDING-RELEASE.md`). Can be deleted. |

## Archived

| Tag | What it holds | Why it stopped |
|---|---|---|
| `archive/evidence-reader-trial` | The JSON fact reader with an enforced answer format, on top of Codex's evidence framework. | Superseded by Alpha-01: 10 of 61 cases fully right, at 48–59 s per case. |
| `archive/evidence-core-framework` | Codex's evidence contracts, strict fact scorer and qualification gates. | Never merged. The strict scorer may still be taken from it. |

`codex/r3-workbench` was fully merged into the default branch, so it was deleted without a tag.
