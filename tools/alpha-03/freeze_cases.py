"""Read-only checks by default; one-way freeze after static and author review."""
import collections
import datetime
import functools
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "tests/alpha-03-cases"
PAYLOAD = ["tests/alpha-03-cases/" + p for p in (
    ".gitattributes", "README.md", "annotation-rules.md", "family-plan.json",
    "source/authoring.py", "cases.json", "split-manifest.json", "review.md", "static-check.txt")]
PAYLOAD += ["tools/alpha-03/" + p for p in (
    ".gitattributes", "build_cases.py", "freeze_cases.py", "CaseCheck/CaseCheck.csproj", "CaseCheck/Program.cs")]
PAYLOAD += ["tests/alpha-03-cases/revisions/freeze-v1.json",
            "tests/alpha-03-cases/revisions/v2-text-correction.json",
            "tools/alpha-03/revise_case_text.py"]


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()


def load(name):
    return json.loads((DATA / name).read_text(encoding="utf-8"))


def check():
    subprocess.run([sys.executable, "-B", str(ROOT / "tools/alpha-03/revise_case_text.py"), "--verify"], check=True, cwd=ROOT)
    plan, manifest, cases = load("family-plan.json"), load("split-manifest.json"), load("cases.json")["cases"]
    families = {f["id"]: f for f in plan["families"]}
    groups = collections.defaultdict(list)
    for f in families.values():
        groups[f["claim"]].append(f)
    reserved = {"divorce-by-husband": 0, "khul": 1}
    order = sorted((c for c in groups if c not in reserved), key=lambda c: sha((plan["splitSeed"] + "\n" + c).encode()))
    capacities, binding_capacities = [11, 7, 8, 8], [2, 1, 2, 2]
    @functools.lru_cache(None)
    def allocate(i, cap, bind):
        if i == len(order):
            return () if not any(cap + bind) else None
        fs = groups[order[i]]
        n, nb = len(fs), sum(f["kind"] == "binding" for f in fs)
        for bucket in range(4):
            if cap[bucket] < n or bind[bucket] < nb or cap[bucket] - n < bind[bucket] - nb:
                continue
            cc, bb = list(cap), list(bind)
            cc[bucket] -= n
            bb[bucket] -= nb
            tail = allocate(i + 1, tuple(cc), tuple(bb))
            if tail is not None:
                return (bucket,) + tail
        return None
    assignment = reserved | dict(zip(order, allocate(0, tuple(capacities), tuple(binding_capacities))))
    for f in families.values():
        expected = assignment[f["claim"]]
        assert (f["split"], f["fold"]) == (("audit", None) if expected == 0 else ("fitting", expected))
    assert len(families) == 36 and len(cases) == 144
    assert manifest["planSha256"] == sha((DATA / "family-plan.json").read_bytes())
    assert manifest["casesFileSha256"] == sha((DATA / "cases.json").read_bytes())
    assert manifest["families"] == plan["families"]
    members = {m["name"]: m for m in manifest["members"]}
    assert len(members) == 144 and set(members) == {c["name"] for c in cases}
    assert len({c["claim"] for c in cases}) == 21
    assert collections.Counter(c["annotation"]["split"] for c in cases) == {"fitting": 96, "audit": 48}
    assert collections.Counter(f["kind"] for f in families.values()) == {"polarity": 27, "binding": 9}
    for f in families.values():
        rows = [c for c in cases if c["family"] == f["id"]]
        assert len(rows) == 4
        assert {c["annotation"]["contrast"] for c in rows} == (set(("affirmed", "denied", "disputed", "absent")) if f["kind"] == "polarity" else {"binding-1", "binding-2", "binding-3", "binding-4"})
        for c in rows:
            a, m = c["annotation"], members[c["name"]]
            assert (a["split"], a["fold"], a["familyKind"]) == (f["split"], f["fold"], f["kind"])
            assert (m["split"], m["fold"], m["familyKind"], m["family"]) == (f["split"], f["fold"], f["kind"], f["id"])
            assert m["caseSha256"] == sha(canonical(c))
            assert set(c["inputs"]) <= {"claim.subject", "notes.facts", "notes.requests", "documents.list", "court.section"}
            assert c["inputs"].get("notes.facts")
            assert c["partition"] == ("development" if f["split"] == "fitting" else "alpha03-audit")
            if f["kind"] == "polarity":
                assert next(e["polarity"] for e in a["events"] if e["kind"] == f["target"]) == a["contrast"]
            if c["claim"] in ("divorce-by-husband", "khul"):
                assert next(e["polarity"] for e in a["events"] if e["kind"] == "marriage") == "affirmed"
            for e in a["events"]:
                if e["date"]:
                    datetime.datetime.strptime(e["date"], "%d/%m/%Y")
                assert not e["amount"] or re.fullmatch(r"[0-9]+", e["amount"])
            for v in a["valueNotes"]:
                assert v["kind"] in ("date", "amount", "identifier")
                if v["kind"] == "date":
                    datetime.datetime.strptime(v["value"], "%d/%m/%Y")
    for claim in {f["claim"] for f in families.values()}:
        assert len({(f["split"], f["fold"]) for f in families.values() if f["claim"] == claim}) == 1
    for fold in (1, 2, 3):
        fs = [f for f in families.values() if f["fold"] == fold]
        assert len(fs) == 8 and collections.Counter(f["kind"] for f in fs) == {"polarity": 6, "binding": 2}
    audit = [f for f in families.values() if f["split"] == "audit"]
    assert collections.Counter(f["kind"] for f in audit) == {"polarity": 9, "binding": 3}
    assert len({canonical(c["inputs"]) for c in cases}) == 144
    normalized_sentences = collections.defaultdict(set)
    for c in cases:
        for s in c["annotation"]["sentences"]:
            normalized = re.sub(r"[0-9]+", "#", s["text"])
            if len(normalized) >= 70:
                normalized_sentences[normalized].add(c["annotation"]["split"])
    assert not any(len(splits) > 1 for splits in normalized_sentences.values()), "Substantial numeric-substitution sentence crosses splits"
    print("Structure: 144 cases; 36 families; 21 claims; 96 fitting / 48 audit; claim groups disjoint; 3 x 8 fitting families; no duplicate inputs or substantial cross-split numeric templates.")
    print("Kinds: fitting 18 polarity / 6 binding families; audit 9 polarity / 3 binding families; each fold 6 polarity / 2 binding.")
    return cases


def fingerprints():
    return {p: sha((ROOT / p).read_bytes()) for p in sorted(PAYLOAD)}


def digest():
    value = hashlib.sha256()
    for p in sorted(PAYLOAD):
        value.update((p + "\n").encode())
        value.update((ROOT / p).read_bytes())
    return value.hexdigest()


def main():
    mode = sys.argv[1] if len(sys.argv) == 2 else "--check"
    assert mode in ("--check", "--freeze", "--verify")
    check()
    if mode == "--freeze":
        assert not (DATA / "freeze.json").exists(), "Already frozen; no overwrite permitted"
        review = (DATA / "review.md").read_text(encoding="utf-8")
        assert "Review completed: 144/144" in review
        result = subprocess.run(["dotnet", str(ROOT / "tools/alpha-03/CaseCheck/bin/Release/net10.0/CaseCheck.dll"), str(DATA), str(ROOT / "data/law")], check=True, capture_output=True, text=True)
        (DATA / "static-check.txt").write_text(result.stdout, encoding="utf-8", newline="\n")
        previous = load("revisions/freeze-v1.json")
        correction = load("revisions/v2-text-correction.json")
        value = dict(schemaVersion=1, revision=2, caseCount=144, familyCount=36, createdUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                     sourceBaseCommit=correction["predecessorCommit"], predecessorContentHash=previous["contentHash"],
                     correctionAuthority=correction["ownerDecision"], unchangedLabelAndCitationHash=correction["labelAndCitationHashBefore"], corpusInferenceBeforeFreeze=0,
                     contentHash=digest(), algorithm="SHA256 of sorted repository-relative UTF8 path + LF + raw file bytes, concatenated; freeze.json excluded to avoid self-reference",
                     files=fingerprints(), mutationPolicy="No case, source or split edit after freeze except owner-adjudicated annotation correction, disclosed with a new hash")
        with (DATA / "freeze.json").open("x", encoding="utf-8", newline="\n") as output:
            json.dump(value, output, ensure_ascii=False, indent=2)
            output.write("\n")
        print(result.stdout.strip())
        print("FROZEN content hash:", value["contentHash"])
    elif mode == "--verify":
        frozen = load("freeze.json")
        assert frozen["files"] == fingerprints() and frozen["contentHash"] == digest(), "Frozen content changed"
        print("Verified frozen content hash:", frozen["contentHash"])


if __name__ == "__main__":
    main()
