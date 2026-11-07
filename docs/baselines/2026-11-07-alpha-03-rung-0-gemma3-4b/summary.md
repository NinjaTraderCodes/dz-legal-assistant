# Fact reading bench

## ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given)

A critical error would make the legal decision or the draft wrong: a reversed role, a missing decisive event, a date or amount bound to the wrong event, a denial read as a fact, an invented value, a wrong requested amount.

| partition | cases | cases with a critical error | plaintiff position | decisive events found | dates on the right event | polarity right | requests found | failures | seconds / case |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| development | 40 | 2 | 97 % | 98 % | 75 % | 100 % | 87 % | 0 | 13 |

Critical errors: plaintiff position 1, missing decisive event 1

Other differences: missing request 4, unreadable lines 3, date not given 2, amount not given 1, relation 1

4 case(s) outside the claim catalogue were not read and are left out of the table: with the claim type given, the app abstains on them.

- ✓ `loan-basic` (loan-repayment) [development] 22s
- ✓ `loan-acknowledgment` (loan-repayment) [development] 14s
- ✓ `loan-no-section` (loan-repayment) [development] 14s
- ✓ `sale-price` (unpaid-sale-price) [development] 14s
- ✓ `sale-land-price` (unpaid-sale-price) [development] 13s
- ✓ `rescission` (contract-rescission) [development] 16s
- ✓ `rescission-sale` (contract-rescission) [development] 15s
- ✗ `contract-damages` (contract-damages) [development] 18s · plaintiff position (lessee instead of buyer|client|creditor|other)
- ✓ `contract-damages-2` (contract-damages) [development] 17s
- ✓ `tort-assault` (tort-damages) [development] 9s
- ✓ `tort-damage-property` (tort-damages) [development] 8s
- ✓ `tort-defamation` (tort-damages) [development] 8s
- ✓ `things-water` (liability-for-things) [development] 10s
- ✓ `things-dog` (liability-for-things) [development] 10s
- ✓ `things-wall` (liability-for-things) [development] 9s
- ✗ `rent-basic` (unpaid-rent) [development] 14s · missing decisive event (notice_served)
- ✓ `occupation` (eviction-occupier-without-title) [development] 10s
- ✓ `occupation-flat` (eviction-occupier-without-title) [development] 9s
- ✓ `commercial-invoices` (commercial-debt) [development] 13s
- ✓ `commercial-supplies` (commercial-debt) [development] 12s
- ✓ `commercial-lease` (commercial-lease-eviction) [development] 15s
- ✓ `commercial-lease-indemnity` (commercial-lease-eviction) [development] 14s
- ✓ `divorce-husband` (divorce-by-husband) [development] 20s
- ✓ `tatliq-abandonment` (tatliq) [development] 16s
- ✓ `tatliq-harm` (tatliq) [development] 16s
- ✓ `khul` (khul) [development] 12s
- ✓ `khul-2` (khul) [development] 11s
- ✓ `nafaqa-children` (nafaqa) [development] 12s
- ✓ `nafaqa-wife` (nafaqa) [development] 11s
- ✓ `custody` (custody) [development] 13s
- ✓ `custody-transfer` (custody-forfeiture) [development] 16s
- ✓ `visitation` (visitation) [development] 10s
- ✓ `dismissal` (abusive-dismissal) [development] 13s
- ✓ `dismissal-grave-fault` (abusive-dismissal) [development] 10s
- ✓ `wages` (unpaid-wages) [development] 11s
- ✓ `wages-overtime` (unpaid-wages) [development] 10s
- ✓ `loan-with-section-mismatch-words` (loan-repayment) [development] 14s
- ✓ `rent-shop-civil-words` (commercial-lease-eviction) [development] 14s
- – `none-inheritance` (out-of-scope) [development] 0s
- – `none-administrative` (out-of-scope) [development] 0s
- – `none-commercial-company` (out-of-scope) [development] 0s
- – `none-civil-status` (out-of-scope) [development] 0s
- ✓ `annulment-fraud-land` (sale-annulment-fraud) [development] 15s
- ✓ `non-delivery-machines` (sale-non-delivery) [development] 17s

### Where the time goes (ollama gemma3:4b @ 127.0.0.1:11434 (alpha-01, claims given))

| call | calls | load s (avg) | prompt tokens (avg) | prompt read tok/s | answer tokens (avg) | write tok/s | first word s (avg) | prompt may be truncated |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| facts.claim | 40 | 0.1 | 723 | 84.1 | 44 | 10.1 | 8.7 | 0 |



Measurement conditions: mains power; Best performance. Actual/effective/configured AC mode GUID: ded574b5-45a0-4f42-8737-46345c09c238. Companion power records and continuous CPU/memory/paging samples are retained outside the repository.

## Alpha-03 rung-0 metadata and limits

7 November 2026. **Mains; Best performance throughout**, with no sampled mode violation. VS Code/heavy applications were closed; no other model benchmark, build or test ran concurrently. CPU-only, context 4,096, temperature 0.2, repeat penalty 1.1, keep-alive 15 minutes, existing reader cap 300 tokens. The Alpha-01 reader, parser, prompt and catalogue were unchanged. Four out-of-catalogue development cases were excluded; 40 inference cases ran once, claims given.

Installed model `gemma3:4b`: digest **`c30276f7dffec93a536d283e15088990454b8d21cf180a3d6eea4959e4147291`**, nominal metadata 4.3B / Q4_K_M, **3,349,593,375 disk bytes**. Installed `/api/show` licence text: **Gemma Terms of Use** (21 February 2024 text). No weights are redistributed by this change.

Harness/reader commit: `5dc52cbf24b670f6f9dad39d294676e29bd2b1ff`; Ollama 0.40.15; .NET 10.0.403; Windows 11 build 26100; i5-1335U, 10 cores / 12 logical processors, 16,836,857,856 visible physical bytes. The Balanced base plan and Best performance mode are recorded separately. App residence was maintained for the whole batch.

The original bench mean is **13 s/case**, including all semantic failures. Rounded per-case rows give approximate mean **13.125**, median **13**, nearest-rank p95 **18**, maximum **22** seconds. Full CLI wall time was **526.951 s**, or **13.174 s/inference case**, including startup, exclusions and report overhead. The first case includes cold model loading; later cases use the existing server's normal cache behaviour. Its current provider does not expose cached-token counts, so the displayed prompt rate must not be treated as a guaranteed fresh-prefill rate.

Sampled simultaneous model/server working-set peak: **7.852 GiB**; model plus app peak: **7.954 GiB**. Model private-byte peak: **8.239 GiB**. App at the model peak: 104.973 MiB. Minimum whole-machine available physical memory: **1.808 GiB**. The single runner PID remained 8692: early (seconds 20–80) model working-set mean **4.001 GiB**, last-minute mean **7.377 GiB**. This is observed resident growth, with no cause or plateau established. It must not be replaced in reporting by a reset-per-case footprint. Maximum machine commit was 16.406 GiB. Updated paging samples had median page-read rate zero and zero observed page outputs, with read spikes; sampling cannot prove absence of every paging event.

The score is **38/40 without a critical error under the existing key**, with the two critical occurrences shown above. Critical categories not observed in this run: wrong date binding, wrong amount binding, polarity, actor swap, invented date/amount and requested-amount error (all zero). The four missing requests, two missing dates and one missing amount are still reported as non-critical by the unchanged evaluator. The original development/robustness data contain no denied/disputed gold events, so the polarity percentage does not establish non-affirmed reading competence. No score or answer-key definition was changed.

Reproduction command (after the already documented unloaded-model start):

```powershell
dotnet src/DzLegal.Harness/bin/Release/net10.0/DzLegal.Harness.dll facts --alpha --claims given --models gemma3:4b --partition development --cases tests/fact-cases --law-root data/law --out $out
```

Raw artifacts: `%TEMP%\dzlegal-alpha03-gate2-clean-20261107-165004\rung0-gemma3-4b`; global resource, licence and digest records are in its parent directory. No raw notes or model responses were copied into this summary. External original harness `summary.md` SHA-256: `08add6412362bd589acebd1a16cdde8a1b756f055a7ae32d607133cf17ba8646`; `manifest.json`: `8db608c4ca689e606ed6ceab00d65648c790152e92345f210a7996ac73e2bf03`.

Shared fingerprints for both references: prompt `60e6f29368b4e77711f89697281137a5dc760f54133c4f85694ea7bc6dc68154`; elements `aa40f534266d6a5cb9f76b2de463b757005e8a861a4271b577964359ae1cfe33`; original case folder `425da3236a75bffbfbbbee07c076c4c9b5fddb71a899a073ef2c19b6ce712f18`.

This is a reference, not a new reader or a runtime model switch. No validation/hidden cases were run. See the gate-2 summary for the separate status-chain cost, order dependence and combined projection. The authorised stop is before new case writing; post-commit build/test results are reported in the handoff.
