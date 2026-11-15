"""The tiny fixture encoder for the reader's plumbing tests (specs/reader.md, "Model files are never committed": the one
named exception). A BERT of random weights (hidden size 8, one layer, one head) over the tests' tiny vocabulary, exported
with the same inputs and output as the real encoder (input_ids, attention_mask, token_type_ids -> last_hidden_state). It
checks loading, shapes, windows and stitching, never accuracy.

    python make_fixture_encoder.py --vocab <tests\\...\\tiny-vocab.txt> --out <tests\\...\\tiny-encoder.onnx>
"""
from __future__ import annotations

import argparse
import sys


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="make_fixture_encoder")
    p.add_argument("--vocab", required=True)
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)

    import torch
    from transformers import BertConfig, BertModel

    torch.manual_seed(7)
    with open(a.vocab, encoding="utf-8") as f:
        size = sum(1 for line in f if line.strip())
    config = BertConfig(vocab_size=size, hidden_size=8, num_hidden_layers=1, num_attention_heads=1, intermediate_size=16,
                        max_position_embeddings=1024)
    model = BertModel(config).eval()

    class Encoder(torch.nn.Module):
        def __init__(self, inner):
            super().__init__()
            self.inner = inner

        def forward(self, input_ids, attention_mask, token_type_ids):
            return self.inner(input_ids=input_ids, attention_mask=attention_mask,
                              token_type_ids=token_type_ids).last_hidden_state

    ids = torch.ones((1, 6), dtype=torch.long)
    axes = {n: {0: "batch", 1: "sequence"} for n in ("input_ids", "attention_mask", "token_type_ids", "last_hidden_state")}
    with torch.no_grad():
        torch.onnx.export(Encoder(model), (ids, torch.ones_like(ids), torch.zeros_like(ids)), a.out,
                          input_names=["input_ids", "attention_mask", "token_type_ids"], output_names=["last_hidden_state"],
                          dynamic_axes=axes, opset_version=17, dynamo=False)
    print(a.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
