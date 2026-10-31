# Fact reading bench

## nvidia/nemotron-3-ultra-550b-a55b (OpenRouter) (alpha-01, claims given)

Run on commit `140a353` (the manifest's `Commit` is empty because the run used a git worktree, whose `.git` is a file
the harness does not follow yet; its `endpoint` line is the harness default and was not used by the OpenRouter engine).

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 26 | 100 % | 99 % | 95 % | 93 % | 99 % | 0 | 4 |

Critical errors: polarity 20, date on wrong event 9, request amount 6, forbidden reading 4, amount on wrong event 4, missing decisive event 1

Other differences: missing event 2, missing request 2

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

The per-case lines stay in the owner's local results folder; only this summary is shared (docs/HIDDEN-SET.md).
