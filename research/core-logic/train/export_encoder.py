"""The 8-bit encoder of the reader's front end (docs/research/core-logic/specs/reader.md, "The front end", item 3).

Loads the original pre-trained encoder (AutoModel: no head) from its downloaded folder at the pinned revision, exports it to
ONNX with the inputs input_ids, attention_mask and token_type_ids (all zeros) and the output last_hidden_state, batch and
sequence as dynamic axes, then quantises the weights to 8-bit integers (onnxruntime.quantization.quantize_dynamic, the step
Part 7's export_measure.py used). Records the files' sizes and SHA-256 in export.json beside them. Model files are never
committed: they stay in C:\\Users\\TheShadow\\dz-core-research\\models\\.

Run with the Part 7 environment (C:\\Users\\TheShadow\\dz-arch-research\\venv), through clean_env:
    python export_encoder.py --model <models\\pretrained\\camelbert-mix> --out <models\\encoders\\camelbert-mix>
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import sys

OPSET = 17  # read by ONNX Runtime 1.31.1; the opset Part 7 exported with


def sha(path: str) -> str:
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="export_encoder")
    p.add_argument("--model", required=True, help="the downloaded pre-trained model folder (with MANIFEST.json)")
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)

    import torch
    from onnxruntime.quantization import QuantType, quantize_dynamic
    from transformers import AutoModel

    os.makedirs(a.out, exist_ok=True)
    model = AutoModel.from_pretrained(a.model)  # the base encoder, no head
    model.eval()
    ids = torch.ones((1, 16), dtype=torch.long)
    args = (ids, torch.ones_like(ids), torch.zeros_like(ids))
    fp32 = os.path.join(a.out, "encoder.fp32.onnx")
    int8 = os.path.join(a.out, "encoder.int8.onnx")
    axes = {"input_ids": {0: "batch", 1: "sequence"}, "attention_mask": {0: "batch", 1: "sequence"},
            "token_type_ids": {0: "batch", 1: "sequence"}, "last_hidden_state": {0: "batch", 1: "sequence"}}

    class Encoder(torch.nn.Module):
        def __init__(self, inner):
            super().__init__()
            self.inner = inner

        def forward(self, input_ids, attention_mask, token_type_ids):
            return self.inner(input_ids=input_ids, attention_mask=attention_mask,
                              token_type_ids=token_type_ids).last_hidden_state

    with torch.no_grad():
        torch.onnx.export(Encoder(model), args, fp32, input_names=["input_ids", "attention_mask", "token_type_ids"],
                          output_names=["last_hidden_state"], dynamic_axes=axes, opset_version=OPSET, dynamo=False)
    quantize_dynamic(fp32, int8, weight_type=QuantType.QInt8)

    manifest = json.load(open(os.path.join(a.model, "MANIFEST.json"), encoding="utf-8"))
    record = {
        "source": {"model": manifest["model"], "revision": manifest["revision"], "licence": manifest["licence"]},
        "opset": OPSET, "quantisation": "quantize_dynamic, QInt8 weights",
        "made": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"),
        "files": {os.path.basename(f): {"bytes": os.path.getsize(f), "sha256": sha(f)} for f in (fp32, int8)},
        "vocab": {"sha256": manifest["files"]["vocab.txt"]["sha256"]},
    }
    with open(os.path.join(a.out, "export.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(record, f, indent=1)
        f.write("\n")
    print(json.dumps(record, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
