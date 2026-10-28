# Strict fact audit: gemma

facts.strict/1

| reader | partition | cases | critical cases | dates correct / all expected | polarity correct / all expected |
|---|---|---:|---:|---:|---:|
| ollama_gemma3_4b___127.0.0.1_11434 | development | 44 | 40 | 7/8 | 36/54 |
| ollama_gemma3_4b___127.0.0.1_11434 | robustness | 2 | 2 | 0/0 | 3/5 |
| ollama_gemma3_4b___127.0.0.1_11434 | validation | 15 | 14 | 9/15 | 20/30 |

Re-scored saved outputs; no fresh model inference. Missing decisive values and unreadable output are critical. Unannotated additions are noncritical under the incomplete legacy gold. See scoring-reconciliation.md and strict-local.manifest.json.
