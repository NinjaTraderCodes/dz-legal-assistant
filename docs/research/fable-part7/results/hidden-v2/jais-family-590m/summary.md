> Copy of the run's `summary.md` for review. The 100 per-case lines (hidden case names with their error types) were removed before pushing, as in the committed hidden v2 comparison of 9 November; everything else is unchanged. Run folder: `battery-facts-1`.

# Alpha-01 owner run

Model: hf.co/mradermacher/jais-family-590m-chat-GGUF:Q4_K_M; digest: fefa705799575cad4b5fbc07105baab7d837965357526fa25c266ab6fb4666e2; think value sent: omitted.

## ollama hf.co/mradermacher/jais-family-590m-chat-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| hidden | 90 | 88 | 81 % | 1 % | 100 % | 50 % | 0 % | 0 | 10 |

Critical errors: missing decisive event 133, plaintiff position 17, polarity 1

Other differences: missing request 176, missing event 135, unreadable lines 83, relation 8

10 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

### Where the time goes (ollama hf.co/mradermacher/jais-family-590m-chat-GGUF:Q4_K_M @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 90 | 0.0 | 806 | 249.6 | 210 | 31.4 | 3.3 | 0 |

## Measurement

Battery run: on battery throughout, Windows power mode as found (not Best performance); reload every10 cases;512MiB guard; app resident.

Full batch including loads/waits: 941.805 s; per attempted case: 9.418 s. Loads: 10.

Sampled model/server working-set peak: 1.682 GiB; private peak: 1.745 GiB; concurrent model+app working-set peak: 1.904 GiB; minimum machine available: 8.928 GiB. Resource samples: 434; invalid power/headroom samples: 0. Sampling can miss short peaks.

Run complete.
