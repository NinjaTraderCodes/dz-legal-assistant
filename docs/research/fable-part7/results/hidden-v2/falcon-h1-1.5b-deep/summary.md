> Copy of the run's `summary.md` for review. The 100 per-case lines (hidden case names with their error types) were removed before pushing, as in the committed hidden v2 comparison of 9 November; everything else is unchanged. Run folder: `mains-facts-1`.

# Alpha-01 owner run

Model: hf.co/tiiuae/Falcon-H1-1.5B-Deep-Instruct-GGUF:Q4_K_M; digest: c634578f683ad854e8834e9e895c6cf3b719cafd669db1216a3b2bd29fd927fd; think value sent: omitted.

## ollama hf.co/tiiuae/Falcon-H1-1.5B-Deep-Instruct-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 65 | 86 % | 86 % | 58 % | 86 % | 79 % | 0 | 16 |

Critical errors: polarity 32, date on wrong event 27, missing decisive event 19, amount on wrong event 17, plaintiff position 13, forbidden reading 9, request amount 8, actor swapped 3

Other differences: missing request 37, date not given 35, unreadable lines 25, missing event 23, amount not given 7, relation 5

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

### Where the time goes (ollama hf.co/tiiuae/Falcon-H1-1.5B-Deep-Instruct-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 1104 | 87.7 | 52 | 17.4 | 12.7 | 0 |

## Measurement

Mains / Best performance required; reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 1440.058 s; per attempted case: 14.401 s. Loads: 10.

Sampled model/server working-set peak: 5.352 GiB; private peak: 5.559 GiB; concurrent model+app working-set peak: 5.520 GiB; minimum machine available: 5.996 GiB. Resource samples: 680; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
