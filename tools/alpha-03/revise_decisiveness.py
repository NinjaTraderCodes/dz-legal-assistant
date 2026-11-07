"""Owner-adjudicated v3 decisive flags only; exact predecessor and invariant checks."""
import copy
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "tests/alpha-03-cases"
BASE = "0511de639194298da9b94036ffc3ffb814eba226"
OLD_HASH = "86a4e3701871184fcab58bdeb94b2a27426f067c95e094380f5045823db1e642"
RECORD = DATA / "revisions/v3-decisiveness.json"
RULE = ('Decisive means a reader missing this fact would get the case wrong. '
        'Every family contrast target is decisive in all four states. Retain distinct '
        'claim foundations, material independent grounds, remedy prerequisites and '
        'material procedural/prior-adjudication facts. A redundant negative alternative '
        'to an affirmed ground, or ancillary history not needed for a sought remedy, '
        'is not decisive. Polarity never by itself determines decisiveness. '
        'Non-target absent questions are not decisive; absence is still scored densely '
        'and forbidden affirmations remain unchanged.')


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def blob(path):
    return subprocess.check_output(["git", "show", BASE + ":" + path], cwd=ROOT)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def decision(case, event):
    kind, family = event["kind"], case["family"]
    if kind == case["annotation"]["target"]:
        return True, "Family contrast target: retained under explicit owner instruction, including absent target."
    if event["polarity"] == "absent":
        return False, "Non-target absent proposition; no missing positive/negative assertion to lose. Dense absence and forbidden affirmation remain scored."
    if family in ("f01", "f02", "f03", "f19") and kind == "payment_made":
        assert event["polarity"] == "denied"
        return False, "No-payment background is already carried by the affirmed unpaid-debt/price event and its outstanding amount."
    if (family == "f09" and kind in ("delivery_late", "delivery_missing")) or (family == "f10" and kind == "delivery_missing"):
        assert event["polarity"] == "denied"
        return False, "Delivered goods' damage/nonconformity is carried by affirmed other; denied alternative breaches do not ground this damages request."
    if case["name"] == "a03-032" and kind == "payment_made":
        return False, "Historical part-payment; only annulment and damages are sought, with no restitution request depending on payment."
    if family == "f18" and kind == "notice_served":
        return False, "Ancillary demand to leave; the titleless-occupation eviction/damages file rests on ownership, occupation and lack of title, not a prior demand."
    return True, "Distinct claim/relationship/performance foundation, material independent ground, sought-remedy or procedural/prior-adjudication fact; not a redundant alternative."


def strip_flags(value):
    result = copy.deepcopy(value)
    for case in result["cases"]:
        for event in case["gold"]["events"] + case["annotation"]["events"]:
            event.pop("decisive", None)
    return result


def verify():
    before = json.loads(blob("tests/alpha-03-cases/cases.json"))
    after = read(DATA / "cases.json")
    assert strip_flags(before) == strip_flags(after), "Non-decisiveness content changed"
    old = json.loads(blob("tests/alpha-03-cases/freeze.json"))
    assert old["contentHash"] == OLD_HASH
    assert (DATA / "revisions/freeze-v2.json").read_bytes() == blob("tests/alpha-03-cases/freeze.json")
    record = read(RECORD)
    assert record["predecessorCommit"] == BASE and record["predecessorContentHash"] == OLD_HASH
    inventory, changes = [], []
    for old_case, case in zip(before["cases"], after["cases"]):
        old_events = {e["kind"]: e for e in old_case["gold"]["events"]}
        gold = {e["kind"]: e for e in case["gold"]["events"]}
        for event in case["annotation"]["events"]:
            flag, reason = decision(case, event)
            assert type(event["decisive"]) is bool and event["decisive"] == flag
            if event["kind"] in gold:
                assert type(gold[event["kind"]]["decisive"]) is bool and gold[event["kind"]]["decisive"] == flag
                if old_events[event["kind"]]["decisive"] != flag:
                    changes.append(dict(case=case["name"], family=case["family"], event=event["kind"], before=True, after=flag, reason=reason))
            inventory.append(dict(case=case["name"], family=case["family"], event=event["kind"], decisive=flag, reason=reason))
    assert inventory == record["allEventDecisions"] and changes == record["changedGoldEvents"]
    assert len(inventory) == 548 and len(changes) == 33
    for name in ("family-plan.json", "source/authoring.py"):
        assert (DATA / name).read_bytes() == blob("tests/alpha-03-cases/" + name), name
    old_manifest = json.loads(blob("tests/alpha-03-cases/split-manifest.json"))
    manifest = read(DATA / "split-manifest.json")
    for item in (old_manifest, manifest):
        item.pop("casesFileSha256")
        for member in item["members"]:
            member.pop("caseSha256")
    assert old_manifest == manifest, "Family/split/fold changed"
    print("V3 invariants: only explicit decisive flags changed; all 144 texts/polarities/values/dates/actors/sources/requests/groups unchanged; 548 explicit dense flags, 33 gold true-to-false changes; all contrast targets decisive.")


def prepare():
    frozen = read(DATA / "freeze.json")
    assert frozen["revision"] == 2 and frozen["contentHash"] == OLD_HASH
    for path, expected in frozen["files"].items():
        assert sha((ROOT / path).read_bytes()) == expected, path
    assert not RECORD.exists() and not (DATA / "revisions/freeze-v2.json").exists()
    before = read(DATA / "cases.json")
    after = copy.deepcopy(before)
    inventory, changes = [], []
    for case in after["cases"]:
        gold = {e["kind"]: e for e in case["gold"]["events"]}
        for event in case["annotation"]["events"]:
            flag, reason = decision(case, event)
            event["decisive"] = flag
            if event["kind"] in gold:
                if gold[event["kind"]]["decisive"] != flag:
                    changes.append(dict(case=case["name"], family=case["family"], event=event["kind"], before=True, after=flag, reason=reason))
                gold[event["kind"]]["decisive"] = flag
            inventory.append(dict(case=case["name"], family=case["family"], event=event["kind"], decisive=flag, reason=reason))
    assert strip_flags(before) == strip_flags(after) and len(changes) == 33
    (DATA / "freeze.json").rename(DATA / "revisions/freeze-v2.json")
    write(DATA / "cases.json", after)
    manifest = read(DATA / "split-manifest.json")
    manifest["casesFileSha256"] = sha((DATA / "cases.json").read_bytes())
    for member, case in zip(manifest["members"], after["cases"]):
        assert member["name"] == case["name"]
        member["caseSha256"] = sha(canonical(case))
    write(DATA / "split-manifest.json", manifest)
    write(RECORD, dict(revision=3, predecessorCommit=BASE, predecessorContentHash=OLD_HASH,
                      ownerDecision="Owner-adjudicated decisive-only correction after interrupted v2 reference; restart all comparisons on v3, do not reuse the partial run.",
                      rule=RULE, priorInference="25 v2 fitting reference calls only, discarded from all future comparisons; no candidate/audit/validation inference",
                      unchangedContentExcludingDecisiveHash=sha(canonical(strip_flags(after))),
                      changedGoldEvents=changes, allEventDecisions=inventory))
    verify()


if __name__ == "__main__":
    if sys.argv[1:] == ["--prepare"]:
        prepare()
    elif sys.argv[1:] == ["--verify"]:
        verify()
    else:
        raise SystemExit("Usage: revise_decisiveness.py --prepare (v2 only) | --verify")
