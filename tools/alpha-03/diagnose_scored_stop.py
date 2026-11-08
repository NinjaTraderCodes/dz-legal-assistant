"""Reproduce the recorded gemma4 missing-score stop, without scoring semantic answers."""
import argparse
import json
import math
from datetime import datetime
from pathlib import Path
from summarize_fitting_references import resource_stats


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def time(value):
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("run_root", type=Path)
args = parser.parse_args()
workspace = Path(__file__).resolve().parents[2]
root = args.run_root
observed = read(root / "observed-stop.json")
assert observed["model"] == "gemma4:e4b"
folder = root / "fitting-gemma4-e4b"
requests = sorted(folder.glob("call-*-request.json"))
assert len(requests) == len(list(folder.glob("call-*-response.json"))) == 47
assert not (folder / "completion.json").exists(), "Preserve the original collector crash; never fabricate completion"
members = [m for m in read(workspace / "tests/alpha-03-cases/split-manifest.json")["members"] if m["split"] == "fitting"]
names = [m["name"] for m in members]
cases = {c["name"]: c for c in read(workspace / "tests/alpha-03-cases/cases.json")["cases"] if c["name"] in names}
catalog = {c["claim"]: c for c in read(workspace / "data/law/claim-elements.json")["claims"]}
prompt = (workspace / "tools/alpha-03/scored-status.ar.txt").read_bytes().decode("utf-8-sig")
fields = dict(zip(("claim.subject", "notes.facts", "notes.requests", "documents.list"), ("الموضوع", "الوقائع", "الطلبات", "المستندات")))
ordinal = valid = reused = whole_chains = 0
first_cache, failed, prompt_counts = [], None, []
last_response = None
for name in names:
    case = cases[name]
    render = "\n".join(f'{s["id"]} [{fields[s["field"]]}] {s["text"]}' for s in case["annotation"]["sentences"])
    common = prompt.replace("{{ case }}", render).replace("{{ claim }}", case["claim"])
    for reverse in (False, True):
        elements = list(catalog[case["claim"]]["elements"])
        if reverse:
            elements.reverse()
        history = [dict(role="system", content="")]
        for index, element in enumerate(elements):
            if ordinal == len(requests):
                break
            ordinal += 1
            req, resp = (read(folder / f"call-{ordinal:04}-{suffix}.json") for suffix in ("request", "response"))
            last_response = resp
            question = "\nالسؤال: " + element["label"] + "\nالجواب:"
            history.append(dict(role="user", content=(common if index == 0 else "") + question))
            assert req["messages"] == history
            assert req["model"] == "gemma4:e4b" and req["think"] is False and req["stream"] is False
            assert req["format"] == dict(type="integer", enum=[0]) and req["logprobs"] is True and req["top_logprobs"] == 20
            assert req["options"] == dict(num_ctx=4096, num_gpu=0, num_predict=1, temperature=.2, repeat_penalty=1.1, seed=0)
            assert resp["done"] and resp["eval_count"] == 1 and resp["message"]["content"] == "0"
            record = resp["logprobs"]
            assert len(record) == 1 and record[0]["token"] == "0" and record[0]["bytes"] == [48]
            alternatives = record[0]["top_logprobs"]
            present = {}
            for code in "1234":
                found = [a for a in alternatives if a["token"] == code and a["bytes"] == [ord(code)]]
                assert len(found) <= 1
                if found:
                    assert math.isfinite(found[0]["logprob"])
                    present[code] = found[0]["logprob"]
            assert 0 <= resp["prompt_eval_count"] < 4096 and 0 <= resp["prompt_eval_cached_count"] < 4096
            prompt_counts.append(resp["prompt_eval_count"])
            if index:
                assert resp["prompt_eval_cached_count"] > 0
                reused += 1
            else:
                first_cache.append(resp["prompt_eval_cached_count"])
            if len(present) == 4:
                valid += 1
                if index == len(elements) - 1:
                    whole_chains += 1
            else:
                assert ordinal == len(requests) and set(present) == {"1", "2", "3"} and len(alternatives) == 20
                failed = dict(case=name, order="reverse" if reverse else "forward", question=index + 1,
                    event=element["event"], nativeCall=ordinal, missing=["4"], presentScores=present,
                    lastReturnedLogprob=alternatives[-1]["logprob"], cachedTokens=resp["prompt_eval_cached_count"], promptTokens=resp["prompt_eval_count"])
            history.append(dict(role="assistant", content=resp["message"]["content"]))
        if ordinal == len(requests):
            break
    if ordinal == len(requests):
        break
rows = read(folder / "case-results.json")
assert [r["Name"] for r in rows] == names[:3] and rows[-1]["LastCall"] == 36
assert valid == 46 and reused == 39 and whole_chains == 7
assert failed["case"] == observed["case"] and failed["event"] == observed["event"]
loads = read(folder / "loads.json")
start, finish = time(loads[0]["StartedUtc"]), time(last_response["created_at"])
samples = [json.loads(line) for line in (root / "resources.jsonl").read_text(encoding="utf-8-sig").splitlines()]
samples = [s for s in samples if start <= time(s["utc"]) <= finish]
assert samples
best = "ded574b5-45a0-4f42-8737-46345c09c238"
for sample in samples:
    power = sample["power"]
    assert power["acLineStatus"] == 1 and power["energySaver"] == 0
    assert all(power[k] == best for k in ("actualOverlayGuid", "effectiveOverlayGuid", "configuredAcGuid"))
    assert sample["appWorkingSetBytes"] > 0 and sample["availablePhysicalBytes"] >= 512 * 1024**2
    assert sample["modelPrivateBytes"] + sample["appWorkingSetBytes"] <= sample["totalPhysicalBytes"]
    assert not any(p["name"] in ("brave", "chrome", "msedge", "firefox", "Code") for p in sample["topOther"])
result = dict(stageComplete=False, semanticEvaluationRun=False, failed=failed, nativeRequests=47,
    fourFinitePositions=valid, missingScorePositions=1, plannedPositions=744, notRequestedPositions=697,
    completedWholeCases=3, partialCase="a03-004", untouchedCases=92, completedWholeChains=whole_chains,
    positiveCacheExtensions=reused, firstPositionCachedTokens=first_cache, maxPromptTokens=max(prompt_counts),
    exactWireAndPromptChecks=47, completionFileAbsent=True, loads=loads, resources=resource_stats(samples),
    resourceWindow=dict(startUtc=start.isoformat(), endUtc=finish.isoformat(), rule="recorded first load to last native response created_at; not a fabricated process/batch time"),
    completedPrefixTimes=[dict(name=r["Name"], seconds=r["Seconds"], forward=r["Chains"][0]["WallSeconds"], reverse=r["Chains"][1]["WallSeconds"]) for r in rows],
    disposition="entire stage stopped; no retry, fallback, score imputation, corpus calibration, binding, compatibility, audit or validation")
(root / "stopped-diagnostic.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: result[k] for k in ("stageComplete", "failed", "nativeRequests", "fourFinitePositions", "positiveCacheExtensions", "notRequestedPositions")}, indent=2))
