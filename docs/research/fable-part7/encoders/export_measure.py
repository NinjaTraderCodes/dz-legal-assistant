"""For one fine-tuned classifier in models/<name>: export to ONNX (fp32), quantise to int8 (dynamic), then for each file
run ort_run.py in a fresh process and record accuracy on the 46 development cases, file size, latency and peak memory.
Usage: python export_measure.py <name>"""
import json, os, subprocess, sys
import torch
from transformers import AutoModelForSequenceClassification, AutoTokenizer
from onnxruntime.quantization import quantize_dynamic, QuantType

NAME = sys.argv[1]
ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = r"C:\Users\TheShadow\dz-fable-arch"
src = os.path.join(ROOT, "models", NAME)
out = os.path.join(ROOT, "onnx", NAME); os.makedirs(out, exist_ok=True)
fp32, int8 = os.path.join(out, "model-fp32.onnx"), os.path.join(out, "model-int8.onnx")
if not os.path.exists(fp32):
    tok = AutoTokenizer.from_pretrained(src)
    model = AutoModelForSequenceClassification.from_pretrained(src).eval()
    enc = tok(["نص تجريبي قصير", "نص ثان أطول قليلا من الأول"], padding=True, return_tensors="pt")
    args = (enc["input_ids"], enc["attention_mask"])
    torch.onnx.export(model, args, fp32, input_names=["input_ids", "attention_mask"], output_names=["logits"],
                      dynamic_axes={"input_ids": {0: "b", 1: "s"}, "attention_mask": {0: "b", 1: "s"}, "logits": {0: "b"}},
                      opset_version=17, dynamo=False)
    tok.save_pretrained(out)
    import shutil; shutil.copy(os.path.join(src, "labels.json"), os.path.join(out, "labels.json"))
if not os.path.exists(int8):
    quantize_dynamic(fp32, int8, weight_type=QuantType.QInt8)

sys.path.insert(0, r"C:\Users\TheShadow\dz-arch-research\tools")
import law
AREA = {c["id"]: c["_file"] for c in law.claims()}
cases = json.load(open(os.path.join(REPO, "tests", "grounds-cases", "cases.json"), encoding="utf-8-sig"))["cases"]
def area_of(s):
    s = s or ""
    if "إداري" in s or "ادار" in s: return "administrative"
    if "الأسرة" in s or "شؤون" in s or "الاسرة" in s: return "family"
    if "تجاري" in s: return "commercial"
    if "اجتماعي" in s: return "social"
    if "مدني" in s: return "civil"
    return None
def score(logits, labels):
    plain = masked = 0
    for c, row in zip(cases, logits):
        gold = c["expect"] if c.get("expect") else "other"
        p = labels[max(range(len(row)), key=lambda i: row[i])]
        a = area_of(c.get("courtSection"))
        if a == "administrative":
            m = "other"
        else:
            r2 = [(-1e9 if (a and l != "other" and AREA.get(l) != a) else v) for l, v in zip(labels, row)]
            m = labels[max(range(len(r2)), key=lambda i: r2[i])]
        plain += p == gold; masked += m == gold
    return plain, masked
res = {"name": NAME}
for tag, path in (("fp32", fp32), ("int8", int8)):
    extra = sum(os.path.getsize(os.path.join(out, f)) for f in os.listdir(out) if f.startswith(os.path.basename(path)) and f != os.path.basename(path))
    for threads in (4,):
        r = json.loads(subprocess.run([sys.executable, os.path.join(ROOT, "ort_run.py"), path, out, str(threads)], capture_output=True, text=True, check=True).stdout.strip().splitlines()[-1])
        plain, masked = score(r.pop("logits"), r.pop("labels"))
        res[tag] = dict(r, file_mb=round((os.path.getsize(path) + extra) / 2**20, 1), correct_plain=plain, correct_section_masked=masked, of=len(cases), threads=threads)
tk = os.path.join(out, "tokenizer.json")
res["tokenizer_mb"] = round(os.path.getsize(tk) / 2**20, 1) if os.path.exists(tk) else None
json.dump(res, open(os.path.join(ROOT, "runs", f"onnx-{NAME}.json"), "w"), indent=1)
print(json.dumps(res))
