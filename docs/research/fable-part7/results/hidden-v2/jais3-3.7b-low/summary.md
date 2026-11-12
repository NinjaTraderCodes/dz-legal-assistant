> Copy of the run's `summary.md` for review. The 100 per-case lines (hidden case names with their error types) were removed before pushing, as in the committed hidden v2 comparison of 9 November; everything else is unchanged. Run folder: `mains-facts-1`.

# Alpha-01 owner run

Model: jais3-3.7b-low:latest; digest: 83990c9ca4b9b92e79fbd24c97d744125a9617667b848cb837ce2bacc971bc86; think value sent: omitted.

## ollama jais3-3.7b-low:latest @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 40 | 94 % | 97 % | 80 % | 90 % | 98 % | 0 | 20 |

Critical errors: polarity 23, amount on wrong event 8, forbidden reading 6, date on wrong event 6, plaintiff position 5, missing decisive event 4, request amount 3, actor swapped 2

Other differences: unreadable lines 90, missing event 31, date not given 25, amount not given 11, relation 3, missing request 3

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

### Where the time goes (ollama jais3-3.7b-low:latest @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 759 | 51.7 | 41 | 8.4 | 14.7 | 0 |

## Measurement

Mains / Best performance required; reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 1814.799 s; per attempted case: 18.148 s. Loads: 10.

Sampled model/server working-set peak: 4.597 GiB; private peak: 4.683 GiB; concurrent model+app working-set peak: 4.763 GiB; minimum machine available: 6.924 GiB. Resource samples: 859; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
