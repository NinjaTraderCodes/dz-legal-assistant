"""The owner's combination route (17 November 2026, at the pilot's stop point): "a combination where the n-grams decide
claim type and outside and the fine-tuned model the rest".

End to end, the n-gram floor's claim answer (or its `outside`, or its abstention) stands, and the fine-tuned reader reads
the events, slots, requests and values for the claim types the n-grams chose; the plaintiff's role comes from the
fine-tuned reader too, the claim confidences from the n-grams. With the gold claim types given, nothing of the n-grams is
used, so `given.jsonl` is the fine-tuned reader's own.

  python combine.py --ngrams <folder with answers.jsonl> --finetuned <folder with given.jsonl> --heads <heads.pt>
                    --cache <the fine-tuned encoder's cache> --measure <export folder> --space <label-space.json>
                    --scorer <score.exe> --out <folder>
"""
from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys

import torch

from decode import reading, value_index_lists
from heads import Heads
from reader_data import Space, load_export

OUTSIDE = "outside"


def read_jsonl(path: str) -> dict:
    out = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.strip():
                x = json.loads(line)
                out[x["id"]] = x["reading"]
    return out


def combined(ngram: dict, tuned: dict) -> dict:
    """The fine-tuned reading made for the n-grams' claim types, under the n-grams' claim block."""
    claim = ngram["claim"]
    chosen = [a["claimId"] for a in claim.get("answers", [])]
    if claim.get("abstained") or not chosen or OUTSIDE in chosen:
        return ngram
    confidence = {a["claimId"]: a["confidence"] for a in claim["answers"]}
    answers = [dict(a, confidence=confidence.get(a["claimId"], a["confidence"])) for a in tuned["claim"]["answers"]]
    return dict(tuned, claim={"answers": answers, "others": claim.get("others", []), "abstained": False})


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    for name in ("ngrams", "finetuned", "heads", "cache", "measure", "space", "scorer", "out"):
        p.add_argument(f"--{name}", required=True)
    a = p.parse_args(argv)
    space = Space(a.space)
    model = Heads(space)
    model.load_state_dict(torch.load(a.heads))
    model.eval()
    ngrams = read_jsonl(os.path.join(a.ngrams, "answers.jsonl"))
    notes, _ = load_export(a.measure, a.cache)
    info = {"name": "dz-reader-combination", "version": "ngrams-claims+finetuned-rest", "files": {}}
    os.makedirs(a.out, exist_ok=True)
    with torch.no_grad(), open(os.path.join(a.out, "answers.jsonl"), "w", encoding="utf-8", newline="\n") as f:
        for note in notes:
            ng = ngrams[note.id]
            chosen = [x["claimId"] for x in ng["claim"].get("answers", []) if x["claimId"] != OUTSIDE]
            out = model(torch.from_numpy(note.tokens), torch.from_numpy(note.fields), torch.from_numpy(note.vector), value_index_lists(note))
            tuned = reading(space, note, out, model, chosen, info) if chosen else ng
            f.write(json.dumps({"id": note.id, "reading": dict(combined(ng, tuned), info=info)}, ensure_ascii=False) + "\n")
    shutil.copyfile(os.path.join(a.finetuned, "given.jsonl"), os.path.join(a.out, "given.jsonl"))
    cmd = [a.scorer, "--gold", os.path.join(a.measure, "notes.jsonl"), "--answers", os.path.join(a.out, "answers.jsonl"),
           "--given", os.path.join(a.out, "given.jsonl"), "--out", a.out, "--purpose", "measure"]
    subprocess.run(cmd, check=True, capture_output=True)
    print(f"combination -> {a.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
