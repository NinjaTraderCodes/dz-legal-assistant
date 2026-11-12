> Copy of the run's `summary.md` for review. The 100 per-case lines (hidden case names with their error types) were removed before pushing, as in the committed hidden v2 comparison of 9 November; everything else is unchanged. Run folder: `battery-facts-1`.

# Alpha-01 owner run

Model: jais3-0.9b-low:latest; digest: 8c192071bb3691b6ff92d9d61c74548592ddc3520454a3f069e118894a70b37a; think value sent: omitted.

## ollama jais3-0.9b-low:latest @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 76 | 82 % | 52 % | 70 % | 76 % | 49 % | 0 | 17 |

Critical errors: missing decisive event 64, polarity 34, plaintiff position 16, forbidden reading 11, date on wrong event 9, amount on wrong event 6, actor swapped 2, request amount 1

Other differences: unreadable lines 90, missing request 90, missing event 66, date not given 16, amount not given 14, relation 8

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

### Where the time goes (ollama jais3-0.9b-low:latest @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 872 | 134.9 | 178 | 17.2 | 6.5 | 0 |

## Measurement

Battery run: on battery throughout, Windows power mode as found (not Best performance); reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 1557.574 s; per attempted case: 15.576 s. Loads: 10.

Sampled model/server working-set peak: 1.440 GiB; private peak: 1.504 GiB; concurrent model+app working-set peak: 1.698 GiB; minimum machine available: 9.116 GiB. Resource samples: 725; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
