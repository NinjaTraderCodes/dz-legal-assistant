# Offline evidence training pilot

The .NET exporter and Python preflight are implemented. No trained reader, performance improvement or legal qualification is claimed.

1. Create reviewed, fictitious `EvidenceDatasetCase` records, partitioned by source decision and scenario family. Review the actual factual labels; do not rename unreviewed labels to make export succeed. Keep calibration and release material separate from training.
2. Run `dotnet run -c Release --project src/DzLegal.Harness -- evidence dataset --dataset <reviewed-cases.json> --out <fresh-external-folder>`.
3. Run `python tools/evidence/train_reader.py --dataset <fresh-external-folder>` for the dependency-free integrity/leakage preflight. This creates no model and starts no paid resource.
4. After dataset review, checkpoint selection and separate GPU/spending approval, install compatible official PyTorch CUDA, Transformers and PEFT packages in an external development venv. Record exact versions, model revision/digest, license and hardware. The reference implementation was checked against Transformers 5.21's Trainer API; an end-to-end GPU training compatibility run remains required.
5. Download the selected supported text-only causal checkpoint explicitly beforehand. The runner accepts only a local directory, uses `local_files_only=True` and `trust_remote_code=False`, and disables Hub/W&B telemetry. It does not manage accounts, rent a machine, download weights or transmit examples.
6. On the separately approved machine: `python tools/evidence/train_reader.py --dataset <export> --model <local-hf-checkpoint> --out <fresh-external-adapter-folder> --train`.

The fixed first pilot uses LoRA rank 8/alpha 16/dropout .05, all linear modules, FP32, one example per device, accumulation 8, learning rate 0.0001, seed 42, at most 100 optimizer steps, and a cooperative one-hour stop checked after steps. Full examples above 4096 tokens fail instead of truncating. Only assistant output tokens are supervised. Some model families may not fit this memory profile or `AutoModelForCausalLM`; such a failure requires a new explicit profile, not an unreported fallback.

The time cap is **not a billing cap** and does not shut down a rented machine. GPU rental approval, rate selection and shutdown are external responsibilities; no rental has been authorized or started. Training loss is not semantic accuracy. The adapter status explicitly remains unqualified. Native/quantized CPU output parity, family-held-out evaluation, legal review and physical 8 GB testing are still mandatory.

`python -m unittest discover -s tools/evidence -p test_train_reader.py` checks preflight acceptance/rejection using fictitious protocol records without loading Torch, downloading a model or training. The shipped desktop does not depend on Python or these tools.

Primary API references: [Transformers Trainer](https://huggingface.co/docs/transformers/main/en/main_classes/trainer), [PEFT](https://github.com/huggingface/peft). Their interfaces and a particular checkpoint's compatibility must be verified in the approved development environment before the paid run.
