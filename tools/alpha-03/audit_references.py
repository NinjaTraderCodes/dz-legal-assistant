"""Audit completed v3 reference wire/reload records; no inference or raw-data edits."""
import argparse
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("run_root", type=Path)
parser.add_argument("--models", nargs="+", choices=("gemma3:4b", "gemma4:e4b"), default=("gemma3:4b", "gemma4:e4b"))
args = parser.parse_args()
workspace = Path(__file__).resolve().parents[2]
manifest = read(workspace / "tests/alpha-03-cases/split-manifest.json")
names = [m["name"] for m in manifest["members"] if m["split"] == "fitting"]
cases = {c["name"]: c for c in read(workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in names}
resources = [json.loads(line) for line in (args.run_root / "resources.jsonl").read_text(encoding="utf-8-sig").splitlines()]
result = {}
for model in args.models:
    folder = args.run_root / ("reference-" + model.replace(":", "-"))
    rows, loads, completion = read(folder / "case-results.json"), read(folder / "loads.json"), read(folder / "completion.json")
    assert [r["Name"] for r in rows] == names and len(names) == 96
    assert completion["Complete"] and completion["LoadCount"] == 10 and completion["Unattempted"] == 0
    assert len(loads) == 10 and [r["FirstCase"] for r in loads] == names[::10]
    assert len(list(folder.glob("call-*-request.json"))) == 96
    for i, row in enumerate(rows, 1):
        request = read(folder / f"call-{i:03}-request.json")
        opts = request["options"]
        assert request["model"] == model and request["stream"] is True
        assert request["keep_alive"] == "15m" and row["Batch"] == (i - 1) // 10 + 1
        assert opts["num_ctx"] == 4096 and opts["num_gpu"] == 0 and opts["num_predict"] == 300
        assert opts["temperature"] == .2 and opts["repeat_penalty"] == 1.1
        assert (request.get("think") is False) if model == "gemma4:e4b" else "think" not in request
        assert "format" not in request and "logprobs" not in request
        assert row["Failure"] is None and row["Timings"] is not None and not row["Timings"]["PromptMayBeTruncated"]
        text = "\n".join(m["content"] for m in request["messages"])
        assert all(s["text"] in text for s in cases[row["Name"]]["annotation"]["sentences"])
    for i in range(1, 11):
        prefix = folder / f"batch-{i:02}"
        unload = read(Path(str(prefix) + "-unload-request.json"))
        warm = read(Path(str(prefix) + "-warmup-request.json"))
        response = read(Path(str(prefix) + "-warmup-response.json"))
        assert unload == {"model": model, "keep_alive": 0}
        assert warm == {"model": model, "keep_alive": "15m", "options": {"num_ctx": 4096, "num_gpu": 0}}
        assert response["done"] and response.get("eval_count", 0) == 0 and not response.get("response")
    samples = [s for s in resources if completion["StartedUtc"][:23] <= s["utc"][:23] <= completion["FinishedUtc"][:23]]
    browser = [s for s in samples if any(p["name"] in ("brave", "chrome", "msedge", "firefox", "Code") for p in s["topOther"])]
    result[model] = dict(requests=96, reloads=10, sourceTextChecks="all complete source sentences present",
                         wireMismatches=0, caps=[r["Name"] for r in rows if r["ReachedCap"]],
                         observedBrowserSamples=len(browser),
                         firstBrowserUtc=browser[0]["utc"] if browser else None,
                         lastBrowserUtc=browser[-1]["utc"] if browser else None,
                         browserPhases=sorted({s["phase"] for s in browser}),
                         timingQualification="UNQUALIFIED: browser reopened during timing" if browser else "no active browser observed; see complete resource record",
                         observationLimit="old monitor records top-three CPU processes, not an exhaustive browser inventory")
(args.run_root / "reference-wire-audit.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, indent=2))
