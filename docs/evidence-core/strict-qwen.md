# Strict fact audit: qwen

facts.strict/1

| reader | partition | cases | critical cases | dates correct / all expected | polarity correct / all expected |
|---|---|---:|---:|---:|---:|
| ollama_qwen3.5_2b___127.0.0.1_11434 | development | 44 | 44 | 2/8 | 2/54 |
| ollama_qwen3.5_2b___127.0.0.1_11434 | robustness | 2 | 2 | 0/0 | 0/5 |
| ollama_qwen3.5_2b___127.0.0.1_11434 | validation | 15 | 15 | 3/15 | 2/30 |

Re-scored saved outputs; no fresh model inference. Missing decisive values and unreadable output are critical. Unannotated additions are noncritical under the incomplete legacy gold. See scoring-reconciliation.md and strict-local.manifest.json.
