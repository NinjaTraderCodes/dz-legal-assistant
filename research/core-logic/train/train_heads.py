"""Trains the heads on the cached 8-bit encoder outputs (docs/research/core-logic/specs/reader.md, rung 4, "The recipe for
the heads"), with early stopping on the scorer's tuning score over the tune notes, then writes the readings.

    python train_heads.py --cache <cache folder> --train <export folder> --tune <export folder> --space <label-space.json>
                          --scorer <score.exe> --seed <n> --loss focal|weighted --out <run folder>
                          [--measure <export folder> ...] [--epochs 20] [--patience 3]

Each export folder is written by `notes export` (with values.jsonl from `reader values`); a train or tune folder whose
manifest's purpose is `measure` is refused, and the tune folder must be `tune` (no Claude-written note in it). A
--measure folder is only read after training, to write readings for the report: nothing is chosen on it.

Settings (the spec's table): AdamW 1e-3, weight decay 0.01, 5 % linear warm-up then cosine, gradient norm 1.0; batch 32
notes (--batch; fewer on a small set); at most 20 epochs, patience 3 on the tuning score; dropout 0.1; loss weights claim 1, polarity 2, evidence 1,
actor and disputedBy 0.5, value roles 1, requests 1, slots 1, parties 0.5; focal loss (gamma 2) on polarity, or plain
cross-entropy with inverse-frequency class weights (`--loss weighted`); 4 threads.
"""
from __future__ import annotations

import argparse
import json
import math
import os
import subprocess
import sys

import numpy as np
import torch
from torch import nn

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decode import reading, value_index_lists  # noqa: E402
from heads import Heads  # noqa: E402
from reader_data import POLARITIES, Space, load_export, targets  # noqa: E402

WEIGHTS = {"claim": 1.0, "polarity": 2.0, "evidence": 1.0, "actor": 0.5, "disputer": 0.5, "roles": 1.0,
           "requests": 1.0, "slots": 1.0, "parties": 0.5}


def focal(logits: torch.Tensor, target: torch.Tensor, gamma: float = 2.0) -> torch.Tensor:
    logp = torch.log_softmax(logits, -1)
    lp = logp.gather(-1, target.unsqueeze(-1)).squeeze(-1)
    return (-(1 - lp.exp()) ** gamma * lp).mean()


def note_loss(model: Heads, space: Space, note, t, loss_kind: str, pol_weights: torch.Tensor | None, out: dict | None = None) -> torch.Tensor:
    if out is None:
        out = model(torch.from_numpy(note.tokens), torch.from_numpy(note.fields), torch.from_numpy(note.vector), value_index_lists(note))
    bce = nn.functional.binary_cross_entropy_with_logits
    total = WEIGHTS["claim"] * bce(out["claim"], torch.from_numpy(t.claims))
    evidence = []
    if t.claim_evidence is not None:
        for i in np.nonzero(t.claims)[0]:
            evidence.append(bce(out["claim_u"][i], torch.from_numpy(t.claim_evidence)))
    if t.elements:
        idx = torch.tensor([e[0] for e in t.elements])
        pol = torch.tensor([e[1] for e in t.elements])
        if loss_kind == "focal":
            total = total + WEIGHTS["polarity"] * focal(out["polarity"][idx], pol)
        else:
            total = total + WEIGHTS["polarity"] * nn.functional.cross_entropy(out["polarity"][idx], pol, weight=pol_weights)
        act = [(e[0], e[2]) for e in t.elements if e[2] >= 0]
        if act:
            total = total + WEIGHTS["actor"] * nn.functional.cross_entropy(out["actor"][torch.tensor([a[0] for a in act])], torch.tensor([a[1] for a in act]))
        dis = [(e[0], e[3]) for e in t.elements if e[3] >= 0]
        if dis:
            total = total + WEIGHTS["disputer"] * nn.functional.cross_entropy(out["disputer"][torch.tensor([d[0] for d in dis])], torch.tensor([d[1] for d in dis]))
        for e in t.elements:
            if e[4] is not None and e[4].sum() > 0:
                evidence.append(bce(out["element_u"][e[0]], torch.from_numpy(e[4])))
    mask = torch.from_numpy(t.requests >= 0)
    if mask.any():
        total = total + WEIGHTS["requests"] * bce(out["request"][mask], torch.from_numpy(np.clip(t.requests, 0, 1))[mask])
    for i, m in t.request_evidence.items():
        if m.sum() > 0:
            evidence.append(bce(out["request_u"][i], torch.from_numpy(m)))
    for k, cls, ev in t.slots:
        total = total + WEIGHTS["slots"] * nn.functional.cross_entropy(out["slot"][k].unsqueeze(0), torch.tensor([cls])) / max(1, len(t.slots))
        if ev is not None and ev.sum() > 0:
            evidence.append(bce(out["slot_u"][k], torch.from_numpy(ev)))
    for k, cls, ev in t.parties:
        total = total + WEIGHTS["parties"] * nn.functional.cross_entropy(out["party"][k].unsqueeze(0), torch.tensor([cls]))
        if ev is not None and ev.sum() > 0:
            evidence.append(bce(out["party_u"][k], torch.from_numpy(ev)))
    if t.value_roles:
        losses = []
        by_value = {}
        for vi, ri, y in t.value_roles:
            by_value.setdefault(vi, []).append((ri, y))
        for vi, pairs in by_value.items():
            ctx = out["value_ctx"][vi]
            if ctx is None:
                continue
            s = model.role_scores(ctx, torch.tensor([p[0] for p in pairs]))
            losses.append(bce(s, torch.tensor([p[1] for p in pairs], dtype=torch.float32)))
        if losses:
            total = total + WEIGHTS["roles"] * torch.stack(losses).mean()
    if evidence:
        total = total + WEIGHTS["evidence"] * torch.stack(evidence).mean()
    return total


def write_readings(model: Heads, space: Space, notes, path_answers: str, path_given: str, info: dict) -> None:
    model.eval()
    with torch.no_grad(), open(path_answers, "w", encoding="utf-8", newline="\n") as fa, \
            open(path_given, "w", encoding="utf-8", newline="\n") as fg:
        for note in notes:
            out = model(torch.from_numpy(note.tokens), torch.from_numpy(note.fields), torch.from_numpy(note.vector), value_index_lists(note))
            fa.write(json.dumps({"id": note.id, "reading": reading(space, note, out, model, None, info)}, ensure_ascii=False) + "\n")
            gold = [c for c in (note.gold or {}).get("claims", []) if c != "outside"]
            if gold:
                fg.write(json.dumps({"id": note.id, "reading": reading(space, note, out, model, gold, info)}, ensure_ascii=False) + "\n")
    model.train()


def score(scorer: str, gold: str, answers: str, given: str, out: str, purpose: str, tuning: bool) -> dict:
    os.makedirs(out, exist_ok=True)
    cmd = [scorer, "--gold", gold, "--answers", answers, "--given", given, "--out", out, "--purpose", purpose]
    if tuning:
        cmd.append("--tuning")
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0:
        raise RuntimeError(f"scorer: {r.stdout}\n{r.stderr}")
    with open(os.path.join(out, "summary.json"), encoding="utf-8") as f:
        return json.load(f)


def purpose_of(folder: str) -> str:
    with open(os.path.join(folder, "manifest.json"), encoding="utf-8") as f:
        return json.load(f)["purpose"]


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--cache", required=True)
    p.add_argument("--train", required=True)
    p.add_argument("--tune", required=True)
    p.add_argument("--measure", action="append", default=[])
    p.add_argument("--space", required=True)
    p.add_argument("--scorer", required=True)
    p.add_argument("--seed", type=int, required=True)
    p.add_argument("--loss", choices=["focal", "weighted"], default="focal")
    p.add_argument("--epochs", type=int, default=20)
    p.add_argument("--patience", type=int, default=3)
    p.add_argument("--batch", type=int, default=32, help="notes per step (the spec's 32; fewer on a small training set)")
    p.add_argument("--lr", type=float, default=1e-3)
    p.add_argument("--init", help="heads.pt to start from (rung 5: refit the heads of a fine-tuned encoder on its 8-bit cache)")
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)
    if purpose_of(a.train) != "train" or purpose_of(a.tune) != "tune":
        raise SystemExit("the train folder must be exported for train and the tune folder for tune (a measure folder never trains or tunes)")
    torch.set_num_threads(4)
    torch.manual_seed(a.seed)
    np.random.seed(a.seed)
    os.makedirs(a.out, exist_ok=True)
    space = Space(a.space)
    train, _ = load_export(a.train, a.cache)
    tune, _ = load_export(a.tune, a.cache)
    train_t = [targets(space, n) for n in train]
    counts = np.ones(len(POLARITIES))
    for t in train_t:
        for e in t.elements:
            counts[e[1]] += 1
    pol_weights = torch.tensor(counts.sum() / (len(counts) * counts), dtype=torch.float32)
    model = Heads(space)
    if a.init:
        model.load_state_dict(torch.load(a.init))
    opt = torch.optim.AdamW(model.parameters(), lr=a.lr, weight_decay=0.01)
    batch = a.batch
    steps = a.epochs * math.ceil(len(train) / batch)
    warm = max(1, int(0.05 * steps))
    sched = torch.optim.lr_scheduler.LambdaLR(opt, lambda s: (s + 1) / warm if s < warm else 0.5 * (1 + math.cos(math.pi * (s - warm) / max(1, steps - warm))))
    info = {"name": "dz-reader-heads", "version": f"rung4-seed{a.seed}-{a.loss}-b{a.batch}-lr{a.lr:g}", "files": {}}
    rng = np.random.default_rng(a.seed)
    best, best_epoch, history, waited = -1.0, -1, [], 0
    for epoch in range(a.epochs):
        order = rng.permutation(len(train))
        total = 0.0
        for start in range(0, len(order), batch):
            opt.zero_grad()
            loss = torch.stack([note_loss(model, space, train[i], train_t[i], a.loss, pol_weights) for i in order[start:start + batch]]).mean()
            loss.backward()
            nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            opt.step()
            sched.step()
            total += float(loss.detach()) * len(order[start:start + batch])
        folder = os.path.join(a.out, f"epoch-{epoch + 1:02d}")
        os.makedirs(folder, exist_ok=True)
        write_readings(model, space, tune, os.path.join(folder, "answers.jsonl"), os.path.join(folder, "given.jsonl"), info)
        summary = score(a.scorer, os.path.join(a.tune, "notes.jsonl"), os.path.join(folder, "answers.jsonl"),
                        os.path.join(folder, "given.jsonl"), folder, "tune", True)
        tuning = summary.get("tuning")
        history.append({"epoch": epoch + 1, "loss": total / len(train), "tuning": tuning})
        print(f"epoch {epoch + 1}: loss {total / len(train):.4f}, tuning {tuning}", flush=True)
        if tuning is not None and tuning > best:
            best, best_epoch, waited = tuning, epoch + 1, 0
            torch.save(model.state_dict(), os.path.join(a.out, "heads.pt"))
        else:
            waited += 1
            if waited >= a.patience:
                break
    model.load_state_dict(torch.load(os.path.join(a.out, "heads.pt")))
    for folder in a.measure:
        if purpose_of(folder) != "measure":
            raise SystemExit(f"{folder}: a --measure folder must be exported for measure")
        notes, _ = load_export(folder, a.cache)
        name = os.path.basename(os.path.normpath(folder))
        out = os.path.join(a.out, "measure-" + name)
        os.makedirs(out, exist_ok=True)
        write_readings(model, space, notes, os.path.join(out, "answers.jsonl"), os.path.join(out, "given.jsonl"), info)
        score(a.scorer, os.path.join(folder, "notes.jsonl"), os.path.join(out, "answers.jsonl"), os.path.join(out, "given.jsonl"), out, "measure", False)
    packages = subprocess.run([sys.executable, "-m", "pip", "freeze"], capture_output=True, text=True).stdout.split()
    with open(os.path.join(a.out, "run.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"seed": a.seed, "loss": a.loss, "batch": a.batch, "lr": a.lr, "init": a.init, "train": a.train, "tune": a.tune, "measure": a.measure, "cache": a.cache,
                   "trainNotes": len(train), "tuneNotes": len(tune), "bestEpoch": best_epoch, "bestTuning": best,
                   "history": history, "packages": packages}, f, indent=1)
    print(f"best epoch {best_epoch}: tuning {best} -> {a.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
