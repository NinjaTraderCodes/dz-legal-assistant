# Reconciliation of the saved fact-reading scores

No models were rerun, trained or adapted for this audit. The 61 cases and their gold labels in `tests/fact-cases` are unchanged. The original parser, `FactsEvaluator` and `facts` commands remain available; the new `evidence audit` command applies `StrictFactsEvaluator` (`facts.strict/1`) to the saved outputs.

| Reader | Partition | Original critical cases | Strict critical cases | Cases |
|---|---|---:|---:|---:|
| Gemma3:4b | development | 34 | 40 | 44 |
| Gemma3:4b | validation | 14 | 14 | 15 |
| Gemma3:4b | robustness (Darija) | 2 | 2 | 2 |
| **Gemma3:4b** | **all** | **50** | **56** | **61** |
| Qwen3.5:2b | development | 43 | 44 | 44 |
| Qwen3.5:2b | validation | 15 | 15 | 15 |
| Qwen3.5:2b | robustness (Darija) | 2 | 2 | 2 |
| **Qwen3.5:2b** | **all** | **60** | **61** | **61** |
| **GLiNER2.5 adapter** | **all** | **57** | **61** | **61** |

The earlier **48/61** Gemma figure mixed scopes: 34 development plus 14 validation is **48/59 formal-Arabic cases**. Including the two Darija failures yields the original **50/61**. The strict all-case total is **40 + 14 + 2 = 56/61**, not an eight-case model regression.

All 50 previously failing Gemma cases still fail. Six previously passing development cases now fail:

| Case | Newly critical differences |
|---|---|
| `things-water` | One unreadable output line |
| `divorce-husband` | Missing marriage date; missing divorce source citation; two unreadable lines |
| `tatliq-abandonment` | Missing tatliq and damages requests; missing divorce source citation |
| `khul-2` | Missing khul request; one unreadable line |
| `custody-transfer` | One unreadable line |
| `none-commercial-company` | One unreadable line |

The strict scorer promotes omitted decisive values, missing requests, wrong/missing sources and rejected lines to critical errors; it also checks required actors against `unknown`, uses one-to-one event matching without consulting gold values to choose a duplicate, and flags contradictory duplicates. Missing events remain in date/polarity denominators. Omitted explicit polarity in external JSON is flagged. Unannotated extra events/requests remain noncritical because the legacy answer key is not exhaustive.

These are stricter acceptance and output-contract criteria, not a claim that 56 cases received independent legal adjudication. A schema error can fail a case before its legal meaning can be assessed. The old logs do not prove every runtime/template setting was honored. Legacy per-case times are unavailable at exact precision in Markdown, so replays report null seconds rather than claiming a new latency measurement.

See the complete strict summaries for [Gemma](strict-gemma.md), [Qwen](strict-qwen.md) and [GLiNER](strict-gliner.md), and the [implementation guide](../EVIDENCE-CORE.md#evaluation-and-measured-audit) for exact scoring behavior and qualification limits.
