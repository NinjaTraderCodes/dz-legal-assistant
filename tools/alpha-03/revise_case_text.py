"""Bounded owner-adjudicated v2 text correction; no labels or source IDs may move."""
import copy
import hashlib
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "tests/alpha-03-cases"
BASE = "41a9a86e89fb0de6465d36609133f858f69e5754"
OLD_HASH = "a3eb985d8f3bafe6bbe43c6afcd742ca0debc09e5b106921c4d4cb352d3bac68"
RECORD = DATA / "revisions/v2-text-correction.json"


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def blob(path):
    return subprocess.check_output(["git", "show", BASE + ":" + path], cwd=ROOT)


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def old_record():
    frozen = json.loads(blob("tests/alpha-03-cases/freeze.json"))
    assert frozen["contentHash"] == OLD_HASH
    digest = hashlib.sha256()
    for path, expected in sorted(frozen["files"].items()):
        raw = blob(path)
        assert sha(raw) == expected, path
        digest.update((path + "\n").encode())
        digest.update(raw)
    assert digest.hexdigest() == OLD_HASH
    return frozen


def invariants(before, after):
    assert before["comment"] == after["comment"]
    assert len(before["cases"]) == len(after["cases"]) == 144
    changed, sentences = [], []
    numbers = r"[0-9]+(?:[.,/\-][0-9]+)*"
    for old, new in zip(before["cases"], after["cases"]):
        assert old["name"] == new["name"]
        a, b = copy.deepcopy(old), copy.deepcopy(new)
        a.pop("inputs")
        b.pop("inputs")
        old_sentences = a["annotation"].pop("sentences")
        new_sentences = b["annotation"].pop("sentences")
        assert a == b, old["name"] + ": a label, value, date, role, citation or split changed"
        assert old["inputs"].keys() == new["inputs"].keys()
        assert old["inputs"]["court.section"] == new["inputs"]["court.section"]
        assert len(old_sentences) == len(new_sentences)
        for os, ns in zip(old_sentences, new_sentences):
            assert (os["id"], os["field"]) == (ns["id"], ns["field"])
            assert re.findall(numbers, os["text"]) == re.findall(numbers, ns["text"]), old["name"] + "/" + os["id"] + ": numeric source tokens changed"
            assert ns["text"] and not ns["text"].endswith(("،", "؛"))
            if os["text"] != ns["text"]:
                sentences.append(dict(case=old["name"], sentence=os["id"], field=os["field"], before=os["text"], after=ns["text"]))
        if old["inputs"] != new["inputs"]:
            changed.append(old["name"])
    return changed, sentences


def apply_source(source, edits):
    for entry in edits:
        assert entry["before"] in source, "Source edit no longer matches: " + entry["before"]
        source = source.replace(entry["before"], entry["after"])
    return source


def prepare():
    frozen = old_record()
    archive = DATA / "revisions/freeze-v1.json"
    if (DATA / "freeze.json").exists():
        assert load(DATA / "freeze.json")["contentHash"] == OLD_HASH
        for path, expected in frozen["files"].items():
            assert sha((ROOT / path).read_bytes()) == expected, path + ": original payload already modified"
        assert not archive.exists()
    else:
        # Resume only this pending, owner-adjudicated correction, never a later freeze.
        assert archive.read_bytes() == blob("tests/alpha-03-cases/freeze.json")
        invariants(json.loads(blob("tests/alpha-03-cases/cases.json")), load(DATA / "cases.json"))
        assert (DATA / "family-plan.json").read_bytes() == blob("tests/alpha-03-cases/family-plan.json")
    correction = load(RECORD)
    before = json.loads(blob("tests/alpha-03-cases/cases.json"))
    source = apply_source(blob("tests/alpha-03-cases/source/authoring.py").decode(), correction["sourceEdits"])
    preview = Path(tempfile.mkdtemp(prefix="dzlegal-alpha03-text-correction-"))
    for relative in ("tools/alpha-03/build_cases.py", "data/law/claim-elements.json", "tests/alpha-03-cases/family-plan.json"):
        target = preview / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(blob(relative))
    draft = preview / "tests/alpha-03-cases"
    (draft / "source").mkdir()
    (draft / "source/authoring.py").write_text(source, encoding="utf-8", newline="\n")
    subprocess.run([sys.executable, "-B", str(preview / "tools/alpha-03/build_cases.py")], check=True, cwd=ROOT)
    after = load(draft / "cases.json")
    changed, edits = invariants(before, after)
    binary = ROOT / "tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll"
    subprocess.run(["dotnet", str(binary), str(draft), str(ROOT / "data/law")], check=True, cwd=ROOT)
    ambiguity = set()
    for rule in correction["sourceEdits"]:
        if rule.get("ambiguityAvoidingRewrite"):
            pattern = re.sub(r"\\\{[^}]+\\\}", ".+?", re.escape(rule["before"]))
            ambiguity.update(c["name"] for c in before["cases"] if any(re.search(pattern, s["text"]) for s in c["annotation"]["sentences"]))
    assert archive.resolve().is_relative_to(DATA.resolve())
    if (DATA / "freeze.json").exists():
        (DATA / "freeze.json").rename(archive)
    for relative in ("source/authoring.py", "cases.json", "split-manifest.json"):
        (DATA / relative).write_bytes((draft / relative).read_bytes())
    correction.update(changedCases=changed, changedSentenceCount=len(edits), sentenceEdits=edits,
                      ambiguityAvoidingRewriteCases=sorted(ambiguity),
                      unchangedCases=[c["name"] for c in before["cases"] if c["name"] not in changed],
                      invariantResult="All labels, values, dates, actors, source IDs/fields/citation edges, numeric tokens per sentence, families, splits and folds unchanged; only input text and sentence-snapshot text changed",
                      labelAndCitationHashBefore=sha(canonical([{k: v for k, v in c.items() if k != "inputs"} | {"annotation": {k: v for k, v in c["annotation"].items() if k != "sentences"}} for c in before["cases"]])))
    write(RECORD, correction)
    print("Preview retained:", preview)
    print("Changed cases:", len(changed), ", ".join(changed))
    print("Changed sentences:", len(edits))
    print("Natural rewrites preserving ambiguous attributions:", ", ".join(sorted(ambiguity)))
    print("Owner correction applied; old freeze archived. Run review/checks and explicitly refreeze; no inference.")


def verify():
    old_record()
    assert (DATA / "revisions/freeze-v1.json").read_bytes() == blob("tests/alpha-03-cases/freeze.json")
    correction = load(RECORD)
    assert correction["revision"] == 2 and correction["predecessorCommit"] == BASE and correction["predecessorContentHash"] == OLD_HASH
    before = json.loads(blob("tests/alpha-03-cases/cases.json"))
    successor = (DATA / "revisions/v3-decisiveness.json").exists()
    # Preserve the original v2 proof on its exact historical snapshot. The v3
    # verifier separately proves current content differs only in decisive flags.
    after = json.loads(subprocess.check_output(["git", "show", "0511de639194298da9b94036ffc3ffb814eba226:tests/alpha-03-cases/cases.json"], cwd=ROOT)) if successor else load(DATA / "cases.json")
    changed, edits = invariants(before, after)
    assert changed == correction["changedCases"] and edits == correction["sentenceEdits"]
    assert len(edits) == correction["changedSentenceCount"]
    label_hash = sha(canonical([{k: v for k, v in c.items() if k != "inputs"} | {"annotation": {k: v for k, v in c["annotation"].items() if k != "sentences"}} for c in after["cases"]]))
    assert label_hash == correction["labelAndCitationHashBefore"]
    ambiguity = set()
    for rule in correction["sourceEdits"]:
        if rule.get("ambiguityAvoidingRewrite"):
            pattern = re.sub(r"\\\{[^}]+\\\}", ".+?", re.escape(rule["before"]))
            ambiguity.update(c["name"] for c in before["cases"] if any(re.search(pattern, s["text"]) for s in c["annotation"]["sentences"]))
    assert sorted(ambiguity) == correction["ambiguityAvoidingRewriteCases"]
    expected_source = apply_source(blob("tests/alpha-03-cases/source/authoring.py").decode(), correction["sourceEdits"])
    assert (DATA / "source/authoring.py").read_text(encoding="utf-8") == expected_source
    old_manifest, new_manifest = json.loads(blob("tests/alpha-03-cases/split-manifest.json")), load(DATA / "split-manifest.json")
    for manifest in (old_manifest, new_manifest):
        manifest.pop("casesFileSha256")
        for member in manifest["members"]:
            member.pop("caseSha256")
    assert old_manifest == new_manifest, "Assignments or split policy changed"
    assert (DATA / "family-plan.json").read_bytes() == blob("tests/alpha-03-cases/family-plan.json")
    print(f"V2 correction invariants{' (historical snapshot)' if successor else ''}: 144/144 label/value/date/source/split records unchanged; {len(changed)} case texts and {len(edits)} sentence texts changed.")


if __name__ == "__main__":
    if sys.argv[1:] == ["--prepare"]:
        prepare()
    elif sys.argv[1:] == ["--verify"]:
        verify()
    else:
        raise SystemExit("Usage: revise_case_text.py --prepare (bounded v1 only) | --verify")
