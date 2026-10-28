# Evidence core review artifacts

These small outputs are selected for code review. They contain fictitious case data only. No model weights, environments, caches, full benchmark runs or training outputs are included.

Start here:

1. [The implementation guide](../EVIDENCE-CORE.md): contracts, limits, unchanged gold labels, training status and the complete file inventory.
2. [Arabic petition](lease-review/evaluation/petition.ar.txt) and [original input](lease-review/input.json): compare the two dates, the amounts and the opposing notice accounts. This is a known-fixture demonstration, not a reader-output benchmark. The factual wording copies original input passages; legal grounds remain open and unreviewed.
3. [Known fixture evidence](lease-review/evidence.json), [rule specification](lease-review/rules.for-review.json) and [review package](lease-review/review-package.json): all require independent factual/legal review. Empty semantic findings do not certify meanings. Candidate source editions are not asserted current or applicable.
4. [Scoring reconciliation](scoring-reconciliation.md), then the [Gemma](strict-gemma.md), [Qwen](strict-qwen.md) and [GLiNER](strict-gliner.md) summaries: these re-score saved outputs without fresh inference.
5. [Artifact provenance](provenance.json): source commit and file hashes. [Protocol self-check](protocol-self-check.json) only checks the fixture against its own reference-format annotations.

The rest of `lease-review/` preserves the generated source snapshot, schemas, reference-format target, discovery candidates, formal checks, draft/review state and process measurements. `evaluation/end-to-end.json` measures supplied evidence through drafting/formal checks. Its time and memory exclude model inference and cannot establish app latency, extraction accuracy or physical 8 GB qualification.

The `strict-local.manifest.json` and `strict-gliner.manifest.json` retain source/case/scorer fingerprints. Full raw benchmark folders remain local under ignored `bench*/`; the summaries alone cannot reproduce a model run. The original 61-case gold and original `FactsEvaluator` remain unchanged.
