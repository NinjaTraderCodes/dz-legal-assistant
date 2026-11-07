# Fact reading bench

## ollama gemma4:e4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 40 | n/a | n/a | n/a | n/a | n/a | 40 | 39 |

Critical errors: none

Other differences: none

4 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✗ failed `loan-basic` [development] 56s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `loan-acknowledgment` [development] 42s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `loan-no-section` [development] 41s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `sale-price` [development] 41s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `sale-land-price` [development] 40s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `rescission` [development] 43s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `rescission-sale` [development] 44s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `contract-damages` [development] 43s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `contract-damages-2` [development] 40s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `tort-assault` [development] 36s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `tort-damage-property` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `tort-defamation` [development] 34s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `things-water` [development] 37s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `things-dog` [development] 36s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `things-wall` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `rent-basic` [development] 42s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `occupation` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `occupation-flat` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `commercial-invoices` [development] 41s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `commercial-supplies` [development] 37s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `commercial-lease` [development] 41s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `commercial-lease-indemnity` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `divorce-husband` [development] 34s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `tatliq-abandonment` [development] 37s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `tatliq-harm` [development] 39s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `khul` [development] 37s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `khul-2` [development] 36s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `nafaqa-children` [development] 36s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `nafaqa-wife` [development] 34s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `custody` [development] 39s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `custody-transfer` [development] 40s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `visitation` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `dismissal` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `dismissal-grave-fault` [development] 39s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `wages` [development] 40s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `wages-overtime` [development] 37s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `loan-with-section-mismatch-words` [development] 42s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `rent-shop-civil-words` [development] 41s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✗ failed `annulment-fraud-land` [development] 40s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.
- ✗ failed `non-delivery-machines` [development] 38s · ollama gemma4:e4b @ 127.0.0.1:11434 stopped at its answer length limit: the answer was cut off.

### Where the time goes (ollama gemma4:e4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 40 | 0.2 | 721 | 102.6 | 300 | 9.4 | 39.2 | 0 |



Measurement conditions: mains power; Best performance. Actual/effective/configured AC mode GUID: ded574b5-45a0-4f42-8737-46345c09c238. Companion power records and continuous CPU/memory/paging samples are retained outside the repository.

## Alpha-03 rung-0 metadata and limits

7 November 2026. **Mains; Best performance throughout**, with no sampled mode violation. VS Code/heavy applications were closed; no other model benchmark, build or test ran concurrently. CPU-only, context 4,096, temperature 0.2, repeat penalty 1.1, keep-alive 15 minutes, existing reader cap 300 tokens. The Alpha-01 reader, parser, prompt and catalogue were unchanged. Four out-of-catalogue development cases were excluded; 40 inference cases ran once, claims given.

Installed model `gemma4:e4b`: digest **`dc35e8d9c6061baa6f0fa870975ab6932e2542b579b13ea0f199fa4bb7300c9c`**, nominal metadata 7.5B / Q4_K_M, **6,583,656,505 disk bytes**. Installed `/api/show` licence text: **Apache License 2.0**. This identifies the particular installed asset; no weights are redistributed by this change.

Harness/reader commit: `5dc52cbf24b670f6f9dad39d294676e29bd2b1ff`; Ollama 0.40.15; .NET 10.0.403; Windows 11 build 26100; i5-1335U, 10 cores / 12 logical processors, 16,836,857,856 visible physical bytes. The Balanced base plan and Best performance mode are recorded separately. App residence was maintained for the whole batch.

**All 40 model calls hit the existing 300-token answer limit.** The unchanged provider raised its cutoff exception before returning a fact reading to the evaluator. Therefore there are **zero successful readings**, and semantic error types/accuracy are **not assessed**. The generated summary's “Critical errors: none” means no parsed semantic-error occurrences were available; it does not mean successful cases. The 40 failures remain in the primary denominator and time calculation.

Thinking was left **unset**, exactly as the current harness does for this model. Its metadata advertises thinking capability, but the failure artifacts do not retain content/thinking streams, so the cause of the token expenditure was not isolated. No thinking-off, longer-cap or prompt variant was run. This result establishes failure of the unchanged configuration, not a semantic ranking of the model under other settings.

The original bench mean is **39 s/case**. Rounded per-case rows give approximate mean **39.225**, median **38.5**, nearest-rank p95 **43**, maximum **56** seconds. Full CLI wall time was **1,569.814 s**, or **39.245 s/inference case**. The first case includes cold model loading; later cases use the existing server's normal cache behaviour. All failed calls are included; there were no retries or substitutions. The native timing table reports 300 answer tokens per call and zero prompt-truncation flags.

Sampled simultaneous model/server working-set peak: **9.942 GiB**; model plus app peak: **10.042 GiB**. Model private-byte peak: **11.197 GiB**. App at the model peak: 102.527 MiB. Minimum whole-machine available physical memory: **0.396 GiB**, about 405 MiB. The single runner PID remained 14912: early (seconds 20–80) model working-set mean **6.607 GiB**, last-minute mean **9.840 GiB**. Growth and little remaining headroom need further investigation before any runtime proposal. Maximum machine commit was **18.996 GiB**, above physical RAM. Updated paging samples had median page-read rate zero and zero observed page outputs, with read spikes; these samples do not certify long-session memory stability or absence of all paging.

Reproduction command (after the already documented unloaded-model start):

```powershell
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts --alpha --claims given --models gemma4:e4b --partition development --cases tests/fact-cases --law-root data/law --out $out
```

Raw artifacts: `%TEMP%\dzlegal-alpha03-gate2-clean-20261107-165004\rung0-gemma4-e4b`; global resource, licence and digest records are in its parent directory. No raw notes or model responses were copied into this summary. External original harness `summary.md` SHA-256: `4973783d9dcb16c7483cefb0b536ab0b0ea5280b6d1b5a0c87f0a35d528bc86f`; `manifest.json`: `8e6f7b1b8c36c134a92c5d9e3f9c73d8fe0a08eab8e1d39efe498c9ec96bcdc4`.

The two references have identical prompt, element and case-folder fingerprints (listed in the gemma3 summary). Their model-specific digests and original default thinking behaviour are preserved. This is a reference, not a runtime switch. No validation/hidden cases were run. The authorised stop is before new case writing; post-commit build/test results are reported in the handoff.
