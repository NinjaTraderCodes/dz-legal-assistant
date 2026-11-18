# The tatliq verifier's example edits: were expected answers changed to match the record? (18 November 2026)

**Why this check:** the owner's first task on resume. The tatliq verifier (stage 8, run r-s1-0205) changed worked
examples with `sed`, and its task file did not allow that. The question: did it change any expected answer to make the
record pass?

**How:** every example of the reconciled record before the verifier's run (`rules/reconciled/tatliq.json` at f5ad5bb)
compared field by field with the same example in the verifier's final candidate (`rules/final-candidates/tatliq.json`,
committed e1718a0). The verifier changed 15 of the 27 examples and added 9 new ones (exv1 to exv8, exv4b). No example
was removed.

**Answer: no expected answer was changed to fit the record.** No expected finding or question was removed or altered
in any of the 15 examples. Two examples gained expected answers, and only for items the verifier added: the boundaries
n12 (plaintiff without full capacity) and n13 (marital home outside Algeria), and the three new slots behind them. Every
answer they expected before still holds.

| Example | Expected answer changed? | What changed |
|---|---|---|
| ex1, ex2, ex3, ex4, ex5, ex6, ex9, ex10, exr1, exr2, exr3, exr4, exr5 | **No** | Only two facts added: `plaintiff_lacks_full_capacity` = no, `marital_home_in_algeria` = yes (the case as written, before the new questions existed). Expected findings and questions unchanged. |
| ex7 ("Nothing known…") | **Yes, additions only** | See diff 1. |
| ex8 ("Every boundary true…") | **Yes, additions only** | See diff 2. |

**Diff 1, ex7.** Facts: unchanged.

```
expect.findings  + {"ref": "n12", "effect": "ask"}
                 + {"ref": "n13", "effect": "ask"}
expect.questions + either_spouse_algerian_at_marriage
                 + marital_home_in_algeria
                 + plaintiff_lacks_full_capacity
(removed: none)
```

**Diff 2, ex8.**

```
slots            + either_spouse_algerian_at_marriage = no
                 + plaintiff_lacks_full_capacity = yes
                 + marital_home_in_algeria = no
expect.findings  + {"ref": "n12", "effect": "warn"}
                 + {"ref": "n13", "effect": "warn"}
(removed: none; n1 to n5 still expected to warn)
```

One point to know about ex8. The verifier narrowed boundary n3 (adv-4): it now warns only when neither spouse was
Algerian at the marriage. To keep n3's expected warning in this example, the verifier added the fact
`either_spouse_algerian_at_marriage` = no. So the expected answer for n3 did not change, but the case behind it gained a
fact. That fits the example's title, "every boundary true".

**Also checked:** the unquoted final candidate that the verifier made with `awk` equals the quoted one minus its
`quote` and `source` fields, exactly. `rules examples` on the final candidate: 36 cases, 0 differences.

Nothing was restored, because nothing needed restoring.
