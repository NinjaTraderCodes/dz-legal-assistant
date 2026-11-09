"""Same-27-case interim report using only the authorised default grounds records."""
import argparse
import hashlib
import json
import time
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("run_root", type=Path)
parser.add_argument("previous_root", type=Path)
args = parser.parse_args()
previous = args.previous_root / "grounds-gemma3-4b"
current = args.run_root / "grounds-gemma4-e4b"
old_bytes = (previous / "measurement-cases.json").read_bytes()
assert hashlib.sha256(old_bytes).hexdigest() == "3e6be167ddcf39a228a67dc18dfa2af7baf11dcfe46ba5aeccf503fa9cbe4387"
old = json.loads(old_bytes.decode("utf-8-sig"))
for attempt in range(10):
    try:
        new = json.loads((current / "measurement-cases.json").read_text(encoding="utf-8-sig"))[:27]
        break
    except (OSError, json.JSONDecodeError):
        if attempt == 9:
            raise
        time.sleep(.025)
assert len(old) == len(new) == 27
assert [r["Name"] for r in old] == [r["Name"] for r in new]
before = json.loads((previous / "manifest.json").read_text(encoding="utf-8-sig"))
after = json.loads((current / "manifest.json").read_text(encoding="utf-8-sig"))
for key in ("cases", "law", "documentTypes"):
    assert before["Fingerprints"][key] == after["Fingerprints"][key], key
assert after["Options"]["think"] == "false"
assert all(r["Metrics"]["Partition"] in ("development", "robustness") for r in old + new)


def quality(rows):
    return dict(cases=len(rows), claimExact=sum(r["Metrics"]["ClaimExact"] for r in rows),
        coreFound=sum(r["Metrics"]["ExpectedFound"] for r in rows),
        coreExpected=sum(len(r["Metrics"]["ExpectedArticles"]) for r in rows),
        secondsPerCase=sum(r["Seconds"] for r in rows) / len(rows),
        failures=sum(r["Error"] is not None for r in rows))


result = dict(caseIds=[r["Name"] for r in old], purpose="interim only; old interrupted prefix, unchanged cases/law/form; not the final full-run comparison",
    results={"gemma3:4b partial 2026-11-08": dict(thinkSent="omitted", all=quality(old)),
             "gemma4:e4b first27": dict(thinkSent=False, all=quality(new))})
for label, rows in (("gemma3:4b partial 2026-11-08", old), ("gemma4:e4b first27", new)):
    result["results"][label]["byPartition"] = {p: quality([r for r in rows if r["Metrics"]["Partition"] == p]) for p in ("development", "robustness")}
target = args.run_root / "grounds-interim27-comparison.json"
if target.exists():
    raise ValueError("Refusing to overwrite an existing interim snapshot")
target.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print("Same 27 cases; 25 development and 2 robustness; no out-of-scope cases in this prefix.")
for label, values in result["results"].items():
    q = values["all"]
    print(f'{label}: claim exact {q["claimExact"]}/{q["cases"]}; core {q["coreFound"]}/{q["coreExpected"]}; {q["secondsPerCase"]:.6f} s/case; think={values["thinkSent"]}')
