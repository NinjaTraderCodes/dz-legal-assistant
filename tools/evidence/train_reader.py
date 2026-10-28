"""Offline LoRA pilot. Default is a dataset-only preflight; no rentals, downloads or uploads.

Only consume reviewed exports from `evidence dataset`. GPU execution requires an
explicit --train and a separately arranged, approved machine. This script cannot
enforce a cloud bill; its wall-clock cap is an additional local stop condition.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import time

for key in ("HF_HUB_OFFLINE", "TRANSFORMERS_OFFLINE", "HF_HUB_DISABLE_TELEMETRY", "DO_NOT_TRACK"):
    os.environ[key] = "1"
os.environ["WANDB_DISABLED"] = "true"
os.environ["TOKENIZERS_PARALLELISM"] = "false"


def preflight(folder):
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8-sig"))
    raw = (folder / "training.jsonl").read_bytes()
    if manifest.get("schema") != "dzlegal.evidence/1":
        raise ValueError("Unsupported evidence contract")
    if hashlib.sha256(raw).hexdigest() != manifest.get("trainingSha256"):
        raise ValueError("Training file differs from reviewed export")
    cases = manifest["cases"]
    if any(c["labelReview"] != "reviewed" or c.get("fictitious") is not True for c in cases):
        raise ValueError("Only reviewed fictitious labels are allowed")
    if len({c["caseId"] for c in cases}) != len(cases) or any(c["partition"] not in {"train", "calibration", "release"} for c in cases):
        raise ValueError("Invalid dataset identities or partitions")
    for key in ("scenarioFamily", "sourceDecision"):
        partitions = {}
        for case in cases:
            value = case[key]
            if not value:
                raise ValueError("Missing scenario/source identity")
            partitions.setdefault(value, set()).add(case["partition"])
        if any(len(values) != 1 for values in partitions.values()):
            raise ValueError("Dataset partition leakage")
    rows = [json.loads(line) for line in raw.decode("utf-8-sig").splitlines() if line.strip()]
    expected = {c["caseId"] for c in cases if c["partition"] == "train"}
    if not rows or len({r["id"] for r in rows}) != len(rows) or {r["id"] for r in rows} != expected:
        raise ValueError("Training identities do not match export manifest")
    for row in rows:
        if row["input"]["fingerprint"] != row["output"]["snapshotFingerprint"]:
            raise ValueError("Stale target evidence")
        if row["referenceInput"]["fingerprint"] != row["input"]["fingerprint"] or set(row["referenceOutput"]) != {"relations", "events", "requests", "documents", "coverage", "unresolved"}:
            raise ValueError("Missing or stale reference-format target")
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--model", type=Path, help="An already downloaded, supported text-only HF causal-LM directory")
    parser.add_argument("--out", type=Path)
    parser.add_argument("--train", action="store_true")
    args = parser.parse_args()
    try:
        rows = preflight(args.dataset)
    except (OSError, ValueError, KeyError) as error:
        parser.error(str(error))
    print(json.dumps({"preflight": "passed", "training_examples": len(rows), "paid_resources_started": False}))
    if not args.train:
        return
    if args.model is None or not args.model.is_dir() or args.out is None or args.out.exists():
        raise ValueError("Training requires a local model and a fresh output directory")

    import torch
    from transformers import AutoModelForCausalLM, AutoTokenizer, Trainer, TrainingArguments, TrainerCallback
    from peft import LoraConfig, TaskType, get_peft_model

    if not torch.cuda.is_available():
        raise RuntimeError("The bounded pilot requires an approved CUDA development machine; shipped inference remains CPU-only")
    tokenizer = AutoTokenizer.from_pretrained(args.model, local_files_only=True, trust_remote_code=False)
    if not tokenizer.chat_template or tokenizer.eos_token_id is None:
        raise ValueError("The checkpoint needs an explicit supported chat template and EOS token")
    if tokenizer.pad_token_id is None:
        tokenizer.pad_token = tokenizer.eos_token
    tokenizer.padding_side = "right"
    examples = []
    for row in rows:
        prompt = [
            {"role": "system", "content": "Extract attributed facts using dzlegal.evidence/1 reference format. Return JSON only. Select existing sentence and value IDs; never count offsets or retype values. Preserve denials and conflicts; never infer legal applicability. Use empty arrays and unresolved points for unstated information."},
            {"role": "user", "content": json.dumps(row["referenceInput"], ensure_ascii=False, separators=(",", ":"))},
        ]
        prefix = tokenizer.apply_chat_template(prompt, tokenize=True, add_generation_prompt=True, return_dict=False)
        complete = tokenizer.apply_chat_template(prompt + [{"role": "assistant", "content": json.dumps(row["referenceOutput"], ensure_ascii=False, separators=(",", ":"))}], tokenize=True, add_generation_prompt=False, return_dict=False)
        if complete[:len(prefix)] != prefix:
            raise ValueError("Template prefix mismatch; supervise only actual assistant tokens")
        if len(complete) > 4096:
            raise ValueError("Whole example exceeds pilot context; refusing silent truncation")
        examples.append({"input_ids": complete, "attention_mask": [1] * len(complete), "labels": [-100] * len(prefix) + complete[len(prefix):]})

    class Dataset(torch.utils.data.Dataset):
        def __len__(self):
            return len(examples)

        def __getitem__(self, index):
            return examples[index]

    def collate(batch):
        padded = tokenizer.pad([{k: v for k, v in example.items() if k != "labels"} for example in batch], return_tensors="pt")
        width = padded["input_ids"].shape[1]
        padded["labels"] = torch.tensor([example["labels"] + [-100] * (width-len(example["labels"])) for example in batch])
        return padded

    class TimeCap(TrainerCallback):
        def __init__(self):
            self.started = time.monotonic()

        def on_step_end(self, training_args, state, control, **kwargs):
            if time.monotonic() - self.started >= 3600:
                control.should_training_stop = True
            return control

    model = AutoModelForCausalLM.from_pretrained(args.model, local_files_only=True, trust_remote_code=False, dtype=torch.float32)
    model = get_peft_model(model, LoraConfig(task_type=TaskType.CAUSAL_LM, r=8, lora_alpha=16, lora_dropout=0.05, target_modules="all-linear"))
    arguments = TrainingArguments(output_dir=str(args.out), max_steps=100, per_device_train_batch_size=1,
        gradient_accumulation_steps=8, learning_rate=1e-4, seed=42, data_seed=42, report_to=[],
        save_strategy="no", logging_steps=10, remove_unused_columns=False, bf16=False, fp16=False)
    trainer = Trainer(model=model, args=arguments, train_dataset=Dataset(), data_collator=collate, callbacks=[TimeCap()])
    trainer.train()
    model.save_pretrained(args.out)
    tokenizer.save_pretrained(args.out)
    (args.out / "pilot-status.json").write_text(json.dumps({"qualified_for_app": False, "steps": trainer.state.global_step,
        "dataset_sha256": hashlib.sha256((args.dataset/"training.jsonl").read_bytes()).hexdigest(),
        "needs": ["held-out semantic evaluation", "CPU output parity", "legal review", "physical 8 GB qualification"]}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
