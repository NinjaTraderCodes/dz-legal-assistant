# Drafting bench

Rules v0.9.5, 5 cases, run 2026-10-27 03:32.

Automatic columns cannot see swapped roles or invented reasoning: read each output too.

| model | case | run | total s | slowest section s | hard issues | number issues | stray Latin words | ungrounded terms | wrong law (misquoted or from memory) | wrong-law paragraphs removed | legal points | example copies removed | retried | fell back | formally complete | error |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| gemma3:4b | 01-debt-arabic | 1 | 88 | 88 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | yes | |
| gemma3:4b | 02-debt-french-notes | 1 | 67 | 67 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | NO | |
| gemma3:4b | 03-rent-darija-notes | 1 | 71 | 71 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 1 | 0 | yes | |
| gemma3:4b | 04-company-defendant | 1 | 59 | 59 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |
| gemma3:4b | 05-real-property | 1 | 55 | 55 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | |

## Where the time goes

Load = reading the model from disk (should be ~0 after the first call). Prompt read = how fast the model reads what we send; write = how fast it writes.

### gemma3:4b

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| grounds-argued | 6 | 1.4 | 1164 | 29.7 | 106 | 6.7 | 40.7 | 0 |

