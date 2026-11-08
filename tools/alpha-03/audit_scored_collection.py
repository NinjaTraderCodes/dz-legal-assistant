"""Audit completed capability/fitting wire records; no inference or semantic selection."""
import argparse
import hashlib
import json
import math
import statistics
from datetime import datetime
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def stats(values):
    values = sorted(values)
    return dict(n=len(values), mean=statistics.mean(values), median=statistics.median(values),
                p95=values[math.ceil(.95 * len(values)) - 1], max=values[-1])


def time(value):
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("run_root", type=Path)
parser.add_argument("--mode", choices=("capability", "fitting"), required=True)
parser.add_argument("--models", nargs="+", choices=("gemma3:4b", "gemma4:e4b"), default=("gemma3:4b", "gemma4:e4b"))
args = parser.parse_args()
workspace = Path(__file__).resolve().parents[2]
manifest = read(workspace / "tests/alpha-03-cases/split-manifest.json")
names = [m["name"] for m in manifest["members"] if m["split"] == "fitting"]
if args.mode == "capability":
    names = names[:1]
cases = {c["name"]: c for c in read(workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in names}
catalog = {c["claim"]: c for c in read(workspace / "data/law/claim-elements.json")["claims"]}
prompt = (workspace / "tools/alpha-03/scored-status.ar.txt").read_bytes().decode("utf-8-sig")
resources = [json.loads(line) for line in (args.run_root / "resources.jsonl").read_text(encoding="utf-8-sig").splitlines()]
labels = dict(zip(("claim.subject", "notes.facts", "notes.requests", "documents.list"), ("الموضوع", "الوقائع", "الطلبات", "المستندات")))
best = "ded574b5-45a0-4f42-8737-46345c09c238"
output = {}
for model in args.models:
    folder = args.run_root / (args.mode + "-" + model.replace(":", "-"))
    completion, rows, loads = (read(folder / name) for name in ("completion.json", "case-results.json", "loads.json"))
    assert completion["Complete"] and completion["Failure"] is None
    assert completion["Planned"] == completion["Completed"] == len(names)
    assert [r["Name"] for r in rows] == names
    assert [r["FirstCase"] for r in loads] == names[::10]
    ordinal = extensions = 0
    first_cache, extension_cache, prompt_tokens = [], [], []
    for row in rows:
        case = cases[row["Name"]]
        elements = catalog[case["claim"]]["elements"]
        render = "\n".join(f'{s["id"]} [{labels[s["field"]]}] {s["text"]}' for s in case["annotation"]["sentences"])
        common = prompt.replace("{{ case }}", render).replace("{{ claim }}", case["claim"])
        assert row["FirstCall"] == ordinal + 1
        assert len(row["Chains"]) == 2
        for reverse, chain in zip((False, True), row["Chains"]):
            assert chain["Reverse"] is reverse
            asked = list(reversed(elements)) if reverse else elements
            assert [q["Event"] for q in chain["Questions"]] == [e["event"] for e in asked]
            history = [dict(role="system", content="")]
            for index, (element, saved) in enumerate(zip(asked, chain["Questions"])):
                question = "\nالسؤال: " + element["label"] + "\nالجواب:"
                history.append(dict(role="user", content=(common if index == 0 else "") + question))
                ordinal += 1
                req = read(folder / f"call-{ordinal:04}-request.json")
                resp = read(folder / f"call-{ordinal:04}-response.json")
                assert req["messages"] == history, (model, row["Name"], reverse, index)
                assert req["model"] == model and req["stream"] is False and req["keep_alive"] == "15m"
                assert (req.get("think") is False) if model == "gemma4:e4b" else "think" not in req
                assert req["format"] == dict(type="integer", enum=[0])
                assert req["logprobs"] is True and req["top_logprobs"] == 20
                assert req["options"] == dict(num_ctx=4096, num_gpu=0, num_predict=1, temperature=.2, repeat_penalty=1.1, seed=0)
                assert resp["done"] and resp["eval_count"] == 1 and resp["message"]["content"] == "0"
                records = resp["logprobs"]
                assert len(records) == 1 and records[0]["token"] == "0" and records[0]["bytes"] == [48]
                scores = []
                for code in "1234":
                    found = [a for a in records[0]["top_logprobs"] if a["token"] == code and a["bytes"] == [ord(code)]]
                    assert len(found) == 1 and math.isfinite(found[0]["logprob"])
                    scores.append(found[0]["logprob"])
                assert saved["Scores"] == scores
                cached, count = resp["prompt_eval_cached_count"], resp["prompt_eval_count"]
                assert 0 <= cached < 4096 and 0 <= count < 4096
                assert saved["CachedTokens"] == cached and saved["PromptTokens"] == count
                if index:
                    assert cached > 0
                    extensions += 1
                    extension_cache.append(cached)
                else:
                    first_cache.append(cached)
                prompt_tokens.append(count)
                history.append(dict(role="assistant", content=resp["message"]["content"]))
        assert row["LastCall"] == ordinal
    assert completion["Calls"] == ordinal == 2 * sum(len(catalog[cases[name]["claim"]]["elements"]) for name in names)
    assert len(list(folder.glob("call-*-request.json"))) == ordinal
    assert len(list(folder.glob("call-*-response.json"))) == ordinal
    for i in range(1, len(loads) + 1):
        assert read(folder / f"batch-{i}-unload-request.json") == dict(model=model, keep_alive=0)
        assert read(folder / f"batch-{i}-warmup-request.json") == dict(model=model, keep_alive="15m", options=dict(num_ctx=4096, num_gpu=0))
    samples = [s for s in resources if time(completion["StartedUtc"]) <= time(s["utc"]) <= time(completion["FinishedUtc"])]
    assert samples
    for sample in samples:
        p = sample["power"]
        assert p["acLineStatus"] == 1 and p["energySaver"] == 0
        assert all(p[k] == best for k in ("actualOverlayGuid", "effectiveOverlayGuid", "configuredAcGuid"))
        assert sample["appWorkingSetBytes"] > 0 and sample["availablePhysicalBytes"] >= 512 * 1024**2
        assert sample["modelPrivateBytes"] + sample["appWorkingSetBytes"] <= sample["totalPhysicalBytes"]
        assert not any(p["name"] in ("brave", "chrome", "msedge", "firefox", "Code") for p in sample["topOther"])
    output[model] = dict(cases=len(rows), positions=ordinal, fourFiniteScoreSets=ordinal, extensionReuse=extensions,
        exactPromptAndPrefixChecks=ordinal, firstPositionCachedTokens=first_cache, extensionCachedTokens=stats(extension_cache),
        promptTokens=stats(prompt_tokens), forwardSeconds=stats([r["Chains"][0]["WallSeconds"] for r in rows]),
        reverseSeconds=stats([r["Chains"][1]["WallSeconds"] for r in rows]),
        caseSeconds=stats([r["Seconds"] for r in rows]), batchSeconds=completion["BatchSeconds"],
        amortizedSeconds=completion["BatchSeconds"] / len(rows), loads=loads,
        resources=dict(samples=len(samples), powerValidSamples=len(samples), modelWorkingSetPeak=max(s["modelWorkingSetBytes"] for s in samples),
            modelPrivatePeak=max(s["modelPrivateBytes"] for s in samples), modelAppWorkingSetPeak=max(s["modelWorkingSetBytes"] + s["appWorkingSetBytes"] for s in samples),
            availableMinimum=min(s["availablePhysicalBytes"] for s in samples), otherCores=stats([s["otherCores"] for s in samples])))
assert not (args.run_root / "precondition-failure.json").exists()
(args.run_root / (args.mode + "-audit.json")).write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
hashes = {str(p.relative_to(args.run_root)): hashlib.sha256(p.read_bytes()).hexdigest()
          for p in sorted(args.run_root.rglob("*")) if p.is_file() and p.name not in ("artifact-hashes.json", "audit-stdout.txt")}
(args.run_root / "artifact-hashes.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
print(json.dumps(output, indent=2))
