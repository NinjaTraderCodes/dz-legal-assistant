> Copy of the run's `summary.md` for review. The 100 per-case lines (hidden case names with their error types) were removed before pushing, as in the committed hidden v2 comparison of 9 November; everything else is unchanged. Run folder: `battery-facts-2`.

# Alpha-01 owner run

Model: hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M; digest: 8572a8510e8517742777c4a2b49accaaf3d19a99aa08bf09bce216de82b76233; think value sent: false.

## ollama hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 69 | 90 % | 93 % | 58 % | 73 % | 77 % | 0 | 16 |

Critical errors: polarity 70, request amount 23, amount on wrong event 16, forbidden reading 14, date on wrong event 10, missing decisive event 10, plaintiff position 9, actor swapped 1

Other differences: date not given 55, missing request 40, unreadable lines 15, amount not given 10, missing event 5, relation 4

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

### Where the time goes (ollama hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 982 | 76.6 | 46 | 13.6 | 12.9 | 0 |

## Measurement

Battery run: on battery throughout, Windows power mode as found (not Best performance); reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 1533.801 s; per attempted case: 15.338 s. Loads: 10.

Sampled model/server working-set peak: 2.780 GiB; private peak: 3.594 GiB; concurrent model+app working-set peak: 3.031 GiB; minimum machine available: 8.484 GiB. Resource samples: 716; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
