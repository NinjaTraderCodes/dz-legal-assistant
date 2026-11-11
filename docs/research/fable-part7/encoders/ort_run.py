"""Child process: load one ONNX classifier with ONNX Runtime (CPU), classify the 46 development notes one at a time,
print JSON: predictions (logits argmax and section-masked), latency per note, peak memory of this process.
Usage: python ort_run.py <model.onnx> <tokenizer dir> <threads>"""
import ctypes, ctypes.wintypes as wt, json, os, sys, time
import numpy as np, onnxruntime as ort
from tokenizers import Tokenizer

class PMC(ctypes.Structure):
    _fields_ = [("cb", wt.DWORD), ("PageFaultCount", wt.DWORD), ("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
                ("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t), ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t),
                ("QuotaNonPagedPoolUsage", ctypes.c_size_t), ("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t), ("PrivateUsage", ctypes.c_size_t)]
def mem():
    c = PMC(); c.cb = ctypes.sizeof(PMC)
    h = ctypes.windll.kernel32.GetCurrentProcess()
    ctypes.windll.kernel32.K32GetProcessMemoryInfo.argtypes = [wt.HANDLE, ctypes.POINTER(PMC), wt.DWORD]
    ctypes.windll.kernel32.K32GetProcessMemoryInfo(h, ctypes.byref(c), c.cb)
    return c.PeakWorkingSetSize, c.PeakPagefileUsage

model, tokdir, threads = sys.argv[1], sys.argv[2], int(sys.argv[3])
REPO = r"C:\Users\TheShadow\dz-fable-arch"
cases = json.load(open(os.path.join(REPO, "tests", "grounds-cases", "cases.json"), encoding="utf-8-sig"))["cases"]
labels = json.load(open(os.path.join(tokdir, "labels.json")))
t0 = time.perf_counter()
tok = Tokenizer.from_file(os.path.join(tokdir, "tokenizer.json")); tok.enable_truncation(160)
so = ort.SessionOptions(); so.intra_op_num_threads = threads
sess = ort.InferenceSession(model, so, providers=["CPUExecutionProvider"])
names = [i.name for i in sess.get_inputs()]
load_s = time.perf_counter() - t0
lat, logits = [], []
for rep in range(2):  # first pass warms up; the second is timed
    lat, logits = [], []
    for c in cases:
        t = time.perf_counter()
        e = tok.encode(c["facts"])
        feed = {"input_ids": np.array([e.ids], dtype=np.int64), "attention_mask": np.array([e.attention_mask], dtype=np.int64)}
        if "token_type_ids" in names:
            feed["token_type_ids"] = np.zeros((1, len(e.ids)), dtype=np.int64)
        out = sess.run(None, {k: v for k, v in feed.items() if k in names})[0][0]
        lat.append((time.perf_counter() - t) * 1000); logits.append(out.tolist())
ws, pf = mem()
print(json.dumps({"load_s": round(load_s, 2), "ms_median": round(float(np.median(lat)), 1), "ms_max": round(max(lat), 1),
                  "peak_working_set_mb": round(ws / 2**20), "peak_private_mb": round(pf / 2**20),
                  "tokens_mean": round(float(np.mean([len(tok.encode(c["facts"]).ids) for c in cases])), 1), "logits": logits, "labels": labels}))
