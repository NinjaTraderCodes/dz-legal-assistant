"""Fine-tune one encoder as a claim-type classifier (21 types + other) on synthetic fictitious notes,
then test it once on the repository's 46 development cases. Fixed recipe for every encoder; nothing is tuned on the 46.
Usage: python train_cls.py <hf model id> <short name> [epochs]"""
import json, os, sys, time, glob, random, re
import numpy as np, torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification, get_linear_schedule_with_warmup

MODEL, NAME = sys.argv[1], sys.argv[2]
EPOCHS = int(sys.argv[3]) if len(sys.argv) > 3 else 5
ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = r"C:\Users\TheShadow\dz-fable-arch"
SEED, MAXLEN, BATCH = 20261111, 160, 16
# Defaults are the fixed recipe. CLS_LR and CLS_OTHER_CAP exist only for the clearly labelled second runs.
LR = float(os.environ.get("CLS_LR", "3e-5"))
OTHER_CAP = int(os.environ.get("CLS_OTHER_CAP", "0"))
random.seed(SEED); np.random.seed(SEED); torch.manual_seed(SEED)

plan = {}
for f in glob.glob(os.path.join(ROOT, "data", "plan-*.jsonl")):
    for line in open(f, encoding="utf-8"):
        r = json.loads(line); plan[r["id"]] = r
rows = []
for f in sorted(glob.glob(os.path.join(ROOT, "data", "notes-[0-9].jsonl"))):
    for line in open(f, encoding="utf-8"):
        line = line.strip()
        if not line:
            continue
        r = json.loads(line)
        if r["id"] in plan and r.get("text", "").strip():
            rows.append((r["text"].strip(), plan[r["id"]]["label"], plan[r["id"]]["lang"]))
labels = sorted({l for _, l, _ in rows})
L = {l: i for i, l in enumerate(labels)}
random.shuffle(rows)
by = {}
for r in rows:
    by.setdefault(r[1], []).append(r)
train, dev = [], []
for l, rs in by.items():
    k = max(2, len(rs) // 10)
    dev += rs[:k]; train += rs[k:]
if OTHER_CAP:
    keep_other = [r for r in train if r[1] == "other"][:OTHER_CAP]
    train = [r for r in train if r[1] != "other"] + keep_other
print(f"{NAME}: notes {len(rows)}, train {len(train)}, synthetic dev {len(dev)}, classes {len(labels)}", flush=True)

# The 46 development cases of the repository (test; never trained on).
cases = json.load(open(os.path.join(REPO, "tests", "grounds-cases", "cases.json"), encoding="utf-8-sig"))["cases"]
def area_of(section):
    s = section or ""
    if "إداري" in s or "ادار" in s: return "administrative"
    if "الأسرة" in s or "شؤون" in s or "الاسرة" in s: return "family"
    if "تجاري" in s: return "commercial"
    if "اجتماعي" in s: return "social"
    if "مدني" in s: return "civil"
    return None
sys.path.insert(0, r"C:\Users\TheShadow\dz-arch-research\tools")
import law
AREA = {c["id"]: c["_file"] for c in law.claims()}
test = [(c["facts"], c["expect"] if c.get("expect") else "other", c.get("partition"), area_of(c.get("courtSection")), c["name"]) for c in cases]

tok = AutoTokenizer.from_pretrained(MODEL)
model = AutoModelForSequenceClassification.from_pretrained(MODEL, num_labels=len(labels))
nparams = sum(p.numel() for p in model.parameters())
def enc(texts):
    return tok(texts, truncation=True, max_length=MAXLEN, padding=True, return_tensors="pt")
def predict(texts, bs=32):
    model.eval(); out = []
    with torch.no_grad():
        for i in range(0, len(texts), bs):
            out.append(model(**enc(texts[i:i + bs])).logits)
    return torch.cat(out)

opt = torch.optim.AdamW(model.parameters(), lr=LR, weight_decay=0.01)
steps = (len(train) + BATCH - 1) // BATCH * EPOCHS
sched = get_linear_schedule_with_warmup(opt, int(0.06 * steps), steps)
t0 = time.time(); tokens = 0
for ep in range(EPOCHS):
    model.train(); random.shuffle(train); tot = 0.0
    for i in range(0, len(train), BATCH):
        b = train[i:i + BATCH]
        x = enc([t for t, _, _ in b]); y = torch.tensor([L[l] for _, l, _ in b])
        tokens += int(x["attention_mask"].sum())
        loss = model(**x, labels=y).loss
        loss.backward(); torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        opt.step(); sched.step(); opt.zero_grad(); tot += float(loss)
    lg = predict([t for t, _, _ in dev])
    acc = float((lg.argmax(-1) == torch.tensor([L[l] for _, l, _ in dev])).float().mean())
    print(f"  epoch {ep + 1}: loss {tot / (len(train) / BATCH):.3f}, synthetic dev accuracy {acc:.3f}, elapsed {time.time() - t0:.0f} s", flush=True)
train_s = time.time() - t0

lg = predict([t for t, *_ in test])
pred = [labels[i] for i in lg.argmax(-1).tolist()]
# Section-masked variant: as the app does for the local model, only the types of the section's area (plus other) are allowed.
masked = []
for (t, gold, part, area, name), row in zip(test, lg):
    if area == "administrative":
        masked.append("other"); continue
    row = row.clone()
    if area:
        for l, i in L.items():
            if l != "other" and AREA.get(l) != area:
                row[i] = -1e9
    masked.append(labels[int(row.argmax())])
def score(p):
    ok = [a == g for a, (_, g, *_r) in zip(p, test)]
    dv = [o for o, (_, _, part, *_r) in zip(ok, test) if part == "development"]
    rb = [o for o, (_, _, part, *_r) in zip(ok, test) if part == "robustness"]
    oos = [o for o, (_, g, *_r) in zip(ok, test) if g == "other"]
    return sum(ok), len(ok), sum(dv), len(dv), sum(rb), len(rb), sum(oos), len(oos)
res = {"model": MODEL, "name": NAME, "parameters": nparams, "epochs": EPOCHS, "lr": LR, "other_cap": OTHER_CAP, "train_notes": len(train), "train_seconds": round(train_s),
       "train_tokens": tokens, "synthetic_dev_accuracy": round(acc, 4),
       "test_plain": score(pred), "test_section_masked": score(masked),
       "errors_plain": [(n, g, p) for p, (_, g, _, _, n) in zip(pred, test) if p != g],
       "errors_masked": [(n, g, p) for p, (_, g, _, _, n) in zip(masked, test) if p != g],
       "torch_threads": torch.get_num_threads()}
os.makedirs(os.path.join(ROOT, "runs"), exist_ok=True)
json.dump(res, open(os.path.join(ROOT, "runs", f"cls-{NAME}.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
out = os.path.join(ROOT, "models", NAME)
model.save_pretrained(out); tok.save_pretrained(out)
json.dump(labels, open(os.path.join(out, "labels.json"), "w"))
print(json.dumps({k: v for k, v in res.items() if not k.startswith("errors")}, ensure_ascii=False))
print("errors (plain):", res["errors_plain"])
print("errors (section-masked):", res["errors_masked"])
