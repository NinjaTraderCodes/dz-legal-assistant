# Owner note — resident gemma3 memory

**Separate from the current Alpha-03 bounded probe; investigation deferred at the owner's instruction.**

The already accepted 7 November Alpha-01 gemma3 batch used one resident runner PID. Its mean model/server working set was about **4.0 GiB early** and **7.4 GiB in the last minute**, with sampled peak 7.852 GiB. These are observations from the previous record, not new measurements or a diagnosis.

Source: `docs/baselines/2026-11-07-alpha-03-rung-0-gemma3-4b/summary.md`, with the existing external resource log and `memory-growth.json` identified there. Cause and plateau are unknown. The current same-order/blind-history/thinking-off task does not investigate this behaviour.
