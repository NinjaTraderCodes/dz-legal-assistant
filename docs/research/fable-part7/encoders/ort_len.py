"""Child process: time one ONNX classifier on inputs of fixed token lengths, one note at a time (CPU, 4 threads).
The 46 development notes are short (about 27 tokens); a lawyer's real note is several hundred. This gives the time per
note by length. The input is a real development note's tokens repeated to the wanted length.
Usage: python ort_len.py <model.onnx> <tokenizer dir> <threads> <max length>"""
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

model, tokdir, threads, max_len = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
REPO = r"C:\Users\TheShadow\dz-fable-arch"
cases = json.load(open(os.path.join(REPO, "tests", "grounds-cases", "cases.json"), encoding="utf-8-sig"))["cases"]
tok = Tokenizer.from_file(os.path.join(tokdir, "tokenizer.json"))
so = ort.SessionOptions(); so.intra_op_num_threads = threads
sess = ort.InferenceSession(model, so, providers=["CPUExecutionProvider"])
names = [i.name for i in sess.get_inputs()]
ids = tok.encode(" ".join(c["facts"] for c in cases[:20])).ids   # special tokens at both ends, real words between
first, last, body = ids[0], ids[-1], ids[1:-1]
res = {}
for n in (32, 128, 256, 512):
    if n > max_len:
        continue
    row = [first] + (body * (n // len(body) + 1))[: n - 2] + [last]
    feed = {"input_ids": np.array([row], dtype=np.int64), "attention_mask": np.ones((1, n), dtype=np.int64)}
    if "token_type_ids" in names:
        feed["token_type_ids"] = np.zeros((1, n), dtype=np.int64)
    feed = {k: v for k, v in feed.items() if k in names}
    for _ in range(3):
        sess.run(None, feed)
    lat = []
    for _ in range(20):
        t = time.perf_counter(); sess.run(None, feed); lat.append((time.perf_counter() - t) * 1000)
    res[str(n)] = {"ms_median": round(float(np.median(lat)), 1), "ms_max": round(max(lat), 1)}
ws, pf = mem()
print(json.dumps({"by_tokens": res, "peak_working_set_mb": round(ws / 2**20), "peak_private_mb": round(pf / 2**20)}))
