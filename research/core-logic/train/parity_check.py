"""Smoke check of the reader's front end: NumPy reads the cache; the Python tokenizer's ids equal the .NET ones."""
import json, sys, glob, os
import numpy as np
from transformers import AutoTokenizer
work = sys.argv[1]
tok = AutoTokenizer.from_pretrained(r"C:\Users\TheShadow\dz-core-research\models\pretrained\camelbert-mix")
notes = {json.loads(l)["id"]: json.loads(l) for l in open(os.path.join(work, "notes.jsonl"), encoding="utf-8")}
same = 0
for line in open(os.path.join(work, "ids.jsonl"), encoding="utf-8"):
    row = json.loads(line)
    text = notes[row["id"]]["text"]
    py = [tok.cls_token_id]
    for f in ("subject", "facts", "requests", "documents"):
        py += tok(text.get(f, ""), add_special_tokens=False)["input_ids"] + [tok.sep_token_id]
    print(row["id"], "pieces", len(row["ids"]), "python == .NET:", py == row["ids"])
    same += py == row["ids"]
for f in sorted(glob.glob(os.path.join(work, "cache", "*", "*.npz"))):
    z = np.load(f)
    print(os.path.basename(f), z["tokens"].dtype, z["tokens"].shape, z["note"].shape, "finite:", bool(np.isfinite(z["tokens"]).all()))
