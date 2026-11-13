"""Runs the encoder measurement of Part 7 on mains power: for each of the four trained classifiers, export_measure.py
(export if needed, quantise if needed, accuracy on the 46 development cases, size, latency, memory), then ort_len.py for
the time per note by length. One model at a time, nothing else running. Writes runs/onnx-<name>.json and
runs/onnx-length-<name>.json, and runs/onnx-conditions.json with the power state before and after."""
import ctypes, json, os, subprocess, sys, time

ROOT = os.path.dirname(os.path.abspath(__file__))
NAMES = ["camelbert-mix", "dziribert", "mmbert-small", "e5-small"]
MAXLEN = {"camelbert-mix": 512, "dziribert": 512, "mmbert-small": 512, "e5-small": 512}


class SPS(ctypes.Structure):
    _fields_ = [("ACLineStatus", ctypes.c_byte), ("BatteryFlag", ctypes.c_byte), ("BatteryLifePercent", ctypes.c_byte),
                ("SystemStatusFlag", ctypes.c_byte), ("BatteryLifeTime", ctypes.c_ulong), ("BatteryFullLifeTime", ctypes.c_ulong)]


def power():
    s = SPS(); ctypes.windll.kernel32.GetSystemPowerStatus(ctypes.byref(s))
    return {"onMains": s.ACLineStatus == 1, "batteryPercent": int(s.BatteryLifePercent), "energySaverOn": s.SystemStatusFlag == 1}


env = {k: v for k, v in os.environ.items() if k not in ("OPENROUTER_API_KEY", "GOOGLE_API_KEY")}
cond = {"started": time.strftime("%Y-%m-%d %H:%M:%S"), "before": power(), "models": {}}
if not cond["before"]["onMains"] or cond["before"]["energySaverOn"]:
    raise SystemExit("not on mains power, or energy saver is on: no timing")
for name in NAMES:
    t = time.time()
    p = subprocess.run([sys.executable, os.path.join(ROOT, "export_measure.py"), name], capture_output=True, text=True, env=env)
    if p.returncode != 0:
        cond["models"][name] = {"error": p.stderr[-600:]}
        print(name, "FAILED", p.stderr[-300:]); continue
    out = os.path.join(ROOT, "onnx", name)
    length = {}
    for tag in ("fp32", "int8"):
        q = subprocess.run([sys.executable, os.path.join(ROOT, "ort_len.py"), os.path.join(out, "model-%s.onnx" % tag), out, "4", str(MAXLEN[name])],
                           capture_output=True, text=True, env=env)
        length[tag] = json.loads(q.stdout.strip().splitlines()[-1]) if q.returncode == 0 else {"error": q.stderr[-400:]}
    json.dump({"name": name, "threads": 4, **length}, open(os.path.join(ROOT, "runs", "onnx-length-%s.json" % name), "w"), indent=1)
    cond["models"][name] = {"seconds": round(time.time() - t), "powerAfter": power()}
    print(name, "done in", round(time.time() - t), "s")
cond["after"] = power(); cond["ended"] = time.strftime("%Y-%m-%d %H:%M:%S")
json.dump(cond, open(os.path.join(ROOT, "runs", "onnx-conditions.json"), "w"), indent=1)
print(json.dumps(cond))
