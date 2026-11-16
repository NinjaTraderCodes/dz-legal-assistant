"""Rung 5 of docs/research/core-logic/specs/reader.md: training the encoder's top layers with the heads.

The lower layers stay frozen: their outputs are computed once (32-bit) for every window of every note, from the .NET
tokenizer's piece ids kept in the 8-bit cache's piece files, windowed exactly as `NoteSequence.Cut` and `Ids` do. The top
`--top` layers and the heads are trained on them, with early stopping on the scorer's tuning score (purpose `tune`). The
result is saved as a full encoder folder (the pre-trained model with the trained top layers) and the heads. **Its score here
is not the reader's** (spec, rung 5, step 2): `export_encoder.py` makes the 8-bit file, `reader encode` rebuilds the cache,
and `train_heads.py --init` refits the heads on it for at most 5 epochs; only that is scored.

    python finetune_top.py --model <models\\pretrained\\camelbert-mix> --cache <8-bit cache folder> --train <export> --tune <export>
                           --space <label-space.json> --scorer <score.exe> --seed <n> --out <run folder>
                           [--top 2] [--batch 16] [--lr-encoder 2e-5] [--lr-heads 5e-4] [--epochs 3] [--patience 3]
"""
from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import subprocess
import sys

import numpy as np
import torch
from torch import nn

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decode import reading, value_index_lists  # noqa: E402
from heads import Heads  # noqa: E402
from reader_data import POLARITIES, Space, load_export, targets  # noqa: E402
from train_heads import note_loss, purpose_of, score  # noqa: E402

WINDOW, OVERLAP, MAX_WINDOWS = 512, 128, 3


def cut(pieces: int):
    """NoteSequence.Cut: one window when [CLS] and the pieces fit; otherwise 510 pieces every 382, at most three."""
    if pieces + 1 <= WINDOW:
        return [(0, pieces)]
    body, windows, first = WINDOW - 2, [], 0
    step = body - OVERLAP
    while first < pieces and len(windows) < MAX_WINDOWS:
        windows.append((first, min(body, pieces - first)))
        if first + body >= pieces:
            break
        first += step
    return windows


def stitch(pieces: int, windows):
    chosen, distance = [-1] * pieces, [0] * pieces
    for w, (first, count) in enumerate(windows):
        for i in range(first, min(first + count, pieces)):
            d = min(i - first, first + count - 1 - i)
            if chosen[i] < 0 or d > distance[i]:
                chosen[i], distance[i] = w, d
    return chosen


def window_ids(ids, first, count, cls, sep):
    """NoteSequence.Ids: [CLS], the window's pieces, and [SEP] unless its last piece is the note's closing [SEP]."""
    out = [cls] + ids[first:first + count]
    if first + count < len(ids) or ids[-1] != sep:
        out.append(sep)
    return out


class Lower:
    """The frozen lower layers' output of every window of a note (32-bit), with the windows and the stitching."""

    def __init__(self, bert, cache: str, note_id: str, lower: int, cls: int, sep: int):
        with open(os.path.join(cache, note_id + ".json"), encoding="utf-8") as f:
            meta = json.load(f)
        if meta.get("cutOff"):
            raise ValueError(f"{note_id}: cut after three windows; not handled here")
        ids = [p["Id"] for p in meta["pieces"]]
        self.windows = cut(len(ids))
        self.chosen = stitch(len(ids), self.windows)
        self.states, self.masks = [], []
        with torch.no_grad():
            for first, count in self.windows:
                x = torch.tensor([window_ids(ids, first, count, cls, sep)])
                out = bert(input_ids=x, attention_mask=torch.ones_like(x), token_type_ids=torch.zeros_like(x), output_hidden_states=True)
                self.states.append(out.hidden_states[lower][0].clone())
        self.pieces = len(ids)


class Top(nn.Module):
    def __init__(self, bert, lower: int):
        super().__init__()
        self.layers = nn.ModuleList([bert.encoder.layer[i] for i in range(lower, len(bert.encoder.layer))])

    def forward(self, low: Lower):
        outs = []
        for h in low.states:
            x = h.unsqueeze(0)
            for layer in self.layers:
                y = layer(x)
                x = y[0] if isinstance(y, tuple) else y
            outs.append(x[0])
        rows = [outs[w][1 + i - low.windows[w][0]] for i, w in enumerate(low.chosen)]
        tokens = torch.stack(rows)
        note = torch.stack([o[0] for o in outs]).mean(0)
        return tokens, note


def main(argv=None) -> int:
    from transformers import AutoModel

    p = argparse.ArgumentParser()
    for name in ("model", "cache", "train", "tune", "space", "scorer", "out"):
        p.add_argument(f"--{name}", required=True)
    p.add_argument("--seed", type=int, required=True)
    p.add_argument("--top", type=int, default=2)
    p.add_argument("--batch", type=int, default=16)
    p.add_argument("--lr-encoder", type=float, default=2e-5)
    p.add_argument("--lr-heads", type=float, default=5e-4)
    p.add_argument("--epochs", type=int, default=3)
    p.add_argument("--patience", type=int, default=3)
    p.add_argument("--loss", choices=["focal", "weighted"], default="focal")
    a = p.parse_args(argv)
    if purpose_of(a.train) != "train" or purpose_of(a.tune) != "tune":
        raise SystemExit("the train folder must be exported for train and the tune folder for tune")
    torch.set_num_threads(4)
    torch.manual_seed(a.seed)
    os.makedirs(a.out, exist_ok=True)
    vocab = open(os.path.join(a.model, "vocab.txt"), encoding="utf-8").read().splitlines()
    cls, sep = vocab.index("[CLS]"), vocab.index("[SEP]")
    bert = AutoModel.from_pretrained(a.model)
    bert.eval()
    lower = len(bert.encoder.layer) - a.top
    space = Space(a.space)
    train, _ = load_export(a.train, a.cache)
    tune, _ = load_export(a.tune, a.cache)
    low_train = [Lower(bert, a.cache, n.id, lower, cls, sep) for n in train]
    low_tune = [Lower(bert, a.cache, n.id, lower, cls, sep) for n in tune]
    print(f"lower {lower} layers computed for {len(train)} + {len(tune)} notes", flush=True)
    top = Top(bert, lower)
    top.train()
    heads = Heads(space)
    train_t = [targets(space, n) for n in train]
    counts = np.ones(len(POLARITIES))
    for t in train_t:
        for e in t.elements:
            counts[e[1]] += 1
    pol_weights = torch.tensor(counts.sum() / (len(counts) * counts), dtype=torch.float32)
    opt = torch.optim.AdamW([{"params": top.parameters(), "lr": a.lr_encoder}, {"params": heads.parameters(), "lr": a.lr_heads}], weight_decay=0.01)
    steps = a.epochs * math.ceil(len(train) / a.batch)
    warm = max(1, int(0.05 * steps))
    sched = torch.optim.lr_scheduler.LambdaLR(opt, lambda s: (s + 1) / warm if s < warm else 0.5 * (1 + math.cos(math.pi * (s - warm) / max(1, steps - warm))))
    info = {"name": "dz-reader-finetune-top", "version": f"rung5-top{a.top}-seed{a.seed}", "files": {}}
    rng = np.random.default_rng(a.seed)
    best, best_epoch, waited, history = -1.0, -1, 0, []

    def forward(low, note):
        tokens, vec = top(low)
        return heads(tokens, torch.from_numpy(note.fields), vec, value_index_lists(note))

    for epoch in range(a.epochs):
        order = rng.permutation(len(train))
        total = 0.0
        for s in range(0, len(order), a.batch):
            opt.zero_grad()
            part = order[s:s + a.batch]
            loss = torch.stack([note_loss(heads, space, train[i], train_t[i], a.loss, pol_weights, forward(low_train[i], train[i])) for i in part]).mean()
            loss.backward()
            nn.utils.clip_grad_norm_(list(top.parameters()) + list(heads.parameters()), 1.0)
            opt.step()
            sched.step()
            total += float(loss.detach()) * len(part)
        folder = os.path.join(a.out, f"epoch-{epoch + 1:02d}")
        os.makedirs(folder, exist_ok=True)
        top.eval()
        heads.eval()
        with torch.no_grad(), open(os.path.join(folder, "answers.jsonl"), "w", encoding="utf-8", newline="\n") as fa, \
                open(os.path.join(folder, "given.jsonl"), "w", encoding="utf-8", newline="\n") as fg:
            for low, note in zip(low_tune, tune):
                out = forward(low, note)
                fa.write(json.dumps({"id": note.id, "reading": reading(space, note, out, heads, None, info)}, ensure_ascii=False) + "\n")
                gold = [c for c in note.gold.get("claims", []) if c != "outside"]
                if gold:
                    fg.write(json.dumps({"id": note.id, "reading": reading(space, note, out, heads, gold, info)}, ensure_ascii=False) + "\n")
        top.train()
        heads.train()
        tuning = score(a.scorer, os.path.join(a.tune, "notes.jsonl"), os.path.join(folder, "answers.jsonl"), os.path.join(folder, "given.jsonl"),
                       folder, "tune", True).get("tuning")
        history.append({"epoch": epoch + 1, "loss": total / len(train), "tuning32": tuning})
        print(f"epoch {epoch + 1}: loss {total / len(train):.4f}, tuning (32-bit, not the reader's) {tuning}", flush=True)
        if tuning is not None and tuning > best:
            best, best_epoch, waited = tuning, epoch + 1, 0
            torch.save(top.state_dict(), os.path.join(a.out, "top.pt"))
            torch.save(heads.state_dict(), os.path.join(a.out, "heads.pt"))
        else:
            waited += 1
            if waited >= a.patience:
                break
    # The full encoder folder: the pre-trained model with the best epoch's top layers.
    top.load_state_dict(torch.load(os.path.join(a.out, "top.pt")))
    enc = os.path.join(a.out, "encoder")
    bert.save_pretrained(enc)
    for f in ("vocab.txt", "tokenizer_config.json", "special_tokens_map.json"):
        if os.path.exists(os.path.join(a.model, f)):
            shutil.copy(os.path.join(a.model, f), enc)
    # The source record export_encoder.py reads, marked as fine-tuned here (the weights are no longer the download's).
    manifest = json.load(open(os.path.join(a.model, "MANIFEST.json"), encoding="utf-8"))
    manifest["fineTuned"] = {"script": "train/finetune_top.py", "top": a.top, "seed": a.seed, "train": a.train, "bestEpoch": best_epoch}
    with open(os.path.join(enc, "MANIFEST.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, indent=1)
    packages = subprocess.run([sys.executable, "-m", "pip", "freeze"], capture_output=True, text=True).stdout.split()
    with open(os.path.join(a.out, "run.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"seed": a.seed, "top": a.top, "batch": a.batch, "lrEncoder": a.lr_encoder, "lrHeads": a.lr_heads, "epochs": a.epochs,
                   "loss": a.loss, "train": a.train, "tune": a.tune, "bestEpoch": best_epoch, "bestTuning32": best, "history": history,
                   "note": "32-bit scores; the reader's score is the refit on the 8-bit cache", "packages": packages}, f, indent=1)
    print(f"best epoch {best_epoch}: tuning (32-bit) {best} -> {enc}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
