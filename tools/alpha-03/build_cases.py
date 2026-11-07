"""Assemble hand-authored fixtures, never predictions. Refuse changes after freeze."""
import hashlib
import json
import re
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "tests/alpha-03-cases"
PLAN_HASH = "d780cea2fe8cbb9ea2ef6a9be7160d374f2ade161219ef119fee45d7845de10b"
FIELDS = ("claim.subject", "notes.facts", "notes.requests", "documents.list")
STATUSES = ("affirmed", "denied", "disputed", "absent")


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def line(key, text, field="notes.facts"):
    return {"key": key, "text": text, "field": field}


def event(kind, cites, *, polarity="affirmed", actor="unknown", date=None, amount=None,
          date_cites=None, amount_cites=None):
    return dict(kind=kind, polarity=polarity, actor=actor, date=date, amount=amount,
                evidence=cites, dateEvidence=(date_cites or cites) if date else [],
                amountEvidence=(amount_cites or cites) if amount else [])


def request(kind, cites, amount=None):
    return dict(kind=kind, present=True, amount=amount, evidence=cites,
                amountEvidence=cites if amount else [])


def contrast(text, *, actor="unknown", date=None, amount=None, field="notes.facts",
             extra=(), events=(), requests=(), note=""):
    texts = [] if text is None else [text] if isinstance(text, str) else text
    keys = ["target" + str(i + 1) for i in range(len(texts))]
    return dict(lines=[line(k, t, field) for k, t in zip(keys, texts)] + list(extra),
                target=dict(actor=actor, date=date, amount=amount, evidence=keys,
                            dateEvidence=keys[:1] if date else [], amountEvidence=keys[:1] if amount else []),
                events=list(events), requests=list(requests), note=note)


def assemble():
    if (DATA / "freeze.json").exists():
        raise SystemExit("Frozen corpus: no regeneration. Owner-adjudicated annotation changes require a new version/hash.")
    assert hashlib.sha256((DATA / "family-plan.json").read_bytes()).hexdigest() == PLAN_HASH
    plan = json.loads((DATA / "family-plan.json").read_text(encoding="utf-8"))
    catalog = {c["claim"]: c for c in json.loads((ROOT / "data/law/claim-elements.json").read_text(encoding="utf-8"))["claims"]}
    authored = {}

    def polarity(fid, role, lines, events, requests, contrasts, *, role_cites=("role",), court="القسم المدني"):
        assert len(contrasts) == 4
        authored[fid] = dict(role=role, roleEvidence=list(role_cites), court=court,
                             variants=[dict(lines=lines + c["lines"], events=events + c["events"], requests=requests + c["requests"],
                                            target=c["target"], variant=s, note=c["note"])
                                       for s, c in zip(STATUSES, contrasts)])

    def binding(fid, role, variants, *, role_cites=("role",), court="القسم المدني"):
        assert len(variants) == 4
        authored[fid] = dict(role=role, roleEvidence=list(role_cites), court=court,
                             variants=[dict(v, variant="binding-" + str(i + 1)) for i, v in enumerate(variants)])

    runpy.run_path(str(DATA / "source/authoring.py"), init_globals={
        "P": polarity, "B": binding, "L": line, "E": event, "R": request, "C": contrast,
        "Q": lambda k, t: line(k, t, "notes.requests"),
        "D": lambda k, t: line(k, t, "documents.list"),
    })
    assert set(authored) == {f["id"] for f in plan["families"]}
    cases, members = [], []
    for family in plan["families"]:
        a = authored[family["id"]]
        claim = catalog[family["claim"]]
        for variant in a["variants"]:
            keys, sentences, inputs = {}, [], {"court.section": a["court"]}
            for field in FIELDS:
                pieces = [x for x in variant["lines"] if x["field"] == field]
                if pieces:
                    inputs[field] = "\n".join(x["text"] for x in pieces)
                for x in pieces:
                    assert x["key"] not in keys, x["key"]
                    ss = [s.strip() for ln in x["text"].replace("\r", "").split("\n") for s in re.split(r"(?<=[.؟!])\s+", ln) if s.strip()]
                    keys[x["key"]] = ["s" + str(len(sentences) + i + 1) for i in range(len(ss))]
                    sentences.extend(dict(id=sid, field=field, text=t) for sid, t in zip(keys[x["key"]], ss))
            def resolve(cites):
                return list(dict.fromkeys(sid for key in cites for sid in keys[key]))
            rows = {e["kind"]: dict(e) for e in variant["events"]}
            if family["kind"] == "polarity" and variant["variant"] != "absent":
                rows[family["target"]] = dict(kind=family["target"], polarity=variant["variant"], **variant["target"])
            assert len(rows) == len(variant["events"]) + (family["kind"] == "polarity" and variant["variant"] != "absent")
            dense_events = []
            for element in claim["elements"]:
                row = rows.pop(element["event"], dict(kind=element["event"], polarity="absent", actor=None, date=None, amount=None, evidence=[], dateEvidence=[], amountEvidence=[]))
                for k in ("evidence", "dateEvidence", "amountEvidence"):
                    row[k] = resolve(row[k])
                row["documentsOnly"] = bool(row["evidence"]) and all(sentences[int(s[1:])-1]["field"] == "documents.list" for s in row["evidence"])
                dense_events.append(row)
            assert not rows, rows
            requests = {r["kind"]: dict(r) for r in variant["requests"]}
            assert len(requests) == len(variant["requests"])
            dense_requests = []
            for remedy in claim["requests"]:
                row = requests.pop(remedy["kind"], dict(kind=remedy["kind"], present=False, amount=None, evidence=[], amountEvidence=[]))
                for k in ("evidence", "amountEvidence"):
                    row[k] = resolve(row[k])
                dense_requests.append(row)
            assert not requests, requests
            gold = dict(relation=a["role"][0], position=a["role"][1],
                        events=[dict({k: e[k] for k in ("kind", "polarity", "date", "amount") if e[k] is not None}, decisive=True,
                                     **({"actor": e["actor"]} if e["actor"] in ("plaintiff", "defendant") else {})) for e in dense_events if e["polarity"] != "absent"],
                        forbidden=[dict(kind=e["kind"]) for e in dense_events if e["polarity"] != "affirmed"],
                        requests=[{k: r[k] for k in ("kind", "amount") if r[k] is not None} for r in dense_requests if r["present"]])
            value_notes = [dict(v, evidence=resolve(v["evidence"])) for v in variant.get("valueNotes", [])]
            c = dict(name="a03-" + str(len(cases) + 1).zfill(3), partition="development" if family["split"] == "fitting" else "alpha03-audit",
                     family=family["id"], claim=family["claim"], inputs=inputs, gold=gold,
                     annotation=dict(familyKind=family["kind"], split=family["split"], fold=family["fold"], contrast=variant["variant"], target=family["target"],
                                     note=variant.get("note", ""), roleEvidence=resolve(a["roleEvidence"]), events=dense_events, requests=dense_requests, valueNotes=value_notes, sentences=sentences))
            cases.append(c)
            members.append(dict(name=c["name"], family=family["id"], split=family["split"], fold=family["fold"], familyKind=family["kind"],
                                caseSha256=hashlib.sha256(json.dumps(c, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()).hexdigest()))
    write(DATA / "cases.json", dict(comment="Codex-authored FICTITIOUS development contrasts. Audit is author-known, never hidden. Dense annotations are scoring data, never model inputs.", cases=cases))
    write(DATA / "split-manifest.json", dict(schemaVersion=1, planSha256=PLAN_HASH, casesFileSha256=hashlib.sha256((DATA / "cases.json").read_bytes()).hexdigest(),
                                            families=plan["families"], members=members,
                                            policy="Fitting families only for grouped selection. Freeze candidate before one audit aggregate inspection; no repairs from audit errors. Both family kinds reported separately."))
    print("Assembled 144 cases from 36 preassigned families; no inference.")


if __name__ == "__main__":
    assemble()
