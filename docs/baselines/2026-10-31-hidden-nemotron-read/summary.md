# Fact reading bench

## nvidia/nemotron-3-ultra-550b-a55b (OpenRouter) (alpha-01, claims read)

Run on commit `140a353` (the manifest's `Commit` is empty because the run used a git worktree, whose `.git` is a file
the harness does not follow yet; its `endpoint` line is the harness default and was not used by the OpenRouter engine).

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 100 | 30 | 98 % | 99 % | 94 % | 94 % | 97 % | 0 | 8 |

Critical errors: polarity 19, date on wrong event 9, request amount 6, wrong claim type 5, forbidden reading 4, amount on wrong event 4, plaintiff position 2, missing decisive event 2

Other differences: missing request 6, missing event 3, date not given 1

The 10 out-of-scope cases were all declined. Claim type right on 85 of the 90 in-catalogue cases.

The per-case lines stay in the owner's local results folder; only this summary is shared (docs/HIDDEN-SET.md).
