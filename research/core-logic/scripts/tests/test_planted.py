"""Planted errors (specs/planted-errors.md, "Tests"): plant is deterministic, changes exactly the field each kind names
and never the store; a planted period fails check-values; score tells caught from flagged and counts the controls'
false alarms; grade reads the rates. Every record, text and slot here is a small fixture written for the test."""
import copy
import hashlib
import json
import os
import re
import subprocess
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import planted as pl  # noqa: E402
import records as rc  # noqa: E402
import store as st  # noqa: E402
import unit  # noqa: E402

PATH = ["الباب الأول", "الفصل الأول"]


def entry(article, paragraphs, in_force=True, repealed=False):
    return {"code": "cpca", "article": article, "version": "law-08-09", "inForce": in_force, "repealed": repealed,
            "paragraphs": paragraphs, "path": PATH, "tier": 1, "store": "library", "verified": True,
            "verifiedBy": "libraryProof", "frenchCheck": None, "ledgerClean": True, "ledgerReason": None,
            "ledgerRefs": [], "textAsOf": "2026-11-10"}


ENTRIES = [
    entry("10", ["يرفع الطلب خلال ستة (6) أشهر من تاريخ التبليغ، تحت طائلة سقوط الحق.",
                 "غير أنه يجوز تمديد هذا الأجل بأمر من القاضي.", "ويبلغ المحضر إلى الأطراف."]),
    entry("11", ["يتقادم الحق بمرور خمس عشرة سنة."]),
    entry("12", ["يجب الإعذار قبل رفع الدعوى.", "يكون الإعذار بعقد غير قضائي يبلغه محضر قضائي."]),
    entry("13", ["نص آخر من الفصل نفسه.", "فقرة ثانية من النص الآخر."]),
    entry("14", ["لا يلزم الإعذار إذا صرح المدين كتابة بأنه لا يريد تنفيذ الالتزام."]),
    entry("15", ["نص ثالث من الفصل نفسه."]),
    entry("16", ["نص رابع من الفصل نفسه.", "فقرته الثانية."]),
    entry("90", ["ملغاة."], in_force=False, repealed=True),
]

SLOTS = {"schemaVersion": "1.0", "slots": [
    {"id": "notice_date", "type": "date", "askedFor": ["*"]},
    {"id": "debt_due_date", "type": "date", "askedFor": ["*"]},
    {"id": "notice_served", "type": "yesNoUnknown", "askedFor": ["*"]},
    {"id": "debtor_refused_in_writing", "type": "yesNoUnknown", "askedFor": ["*"]},
    {"id": "default_on_due_date_agreed", "type": "yesNoUnknown", "askedFor": ["*"]},
    {"id": "public_body_party", "type": "yesNoUnknown", "askedFor": ["*"]}]}

TEXT = {"ar": "نص.", "en": "Text."}


def stmt(sid, article, paragraphs, says_en="It says so."):
    return {"id": sid, "role": "procedure", "cite": {"code": "cpca", "article": article, "paragraphs": paragraphs},
            "says": {"ar": "يقول ذلك.", "en": says_en},
            "quote": [{"paragraph": 1, "text": "x", "sha256": "0"}], "source": {"tier": 1, "store": "library"},
            "grading": {"grade": "D", "reasons": [], "toCheck": True}}


def final_record(uid):
    """A final module with one item of every sort the twelve kinds need, and the script fields a final record has."""
    return {
        "schemaVersion": "1.0", "kind": "module", "id": uid, "label": TEXT, "verified": False, "stage": "final",
        "derivedBy": {"role": "reconciler", "model": "fixture", "date": "2026-11-15"}, "appliesTo": ["*"],
        "slots": ["notice_date", "debt_due_date", "notice_served", "debtor_refused_in_writing",
                  "default_on_due_date_agreed", "public_body_party"],
        "basis": [stmt("b1", "10", [1], "Six months, on pain of forfeiture."), stmt("b2", "10", [2]),
                  stmt("b3", "11", [1], "Fifteen years."), stmt("b4", "12", [1]), stmt("b5", "12", [2]),
                  stmt("b6", "14", [1]), stmt("b7", "10", [3])],
        "deadlines": [
            {"id": "d1", "kind": "forfeiture", "period": {"value": 6, "unit": "month"},
             "startsFrom": {"slot": "notice_date", "description": TEXT}, "raisedBy": "unclear",
             "counting": {"method": "excludeFirstDay", "extendToWorkingDay": True, "basis": ["b1"]},
             "effectWhenExpired": "blockCleanExport", "message": {"ar": "أجل ستة (6) أشهر.", "en": "Six months."},
             "basis": ["b1", "b2"], "extract": {"period": "ستة (6) أشهر", "sanction": "تحت طائلة سقوط الحق"},
             "grading": {"grade": "D", "reasons": [], "toCheck": True}},
            {"id": "d2", "kind": "limitation", "period": {"value": 15, "unit": "year"},
             "startsFrom": {"slot": "debt_due_date", "description": TEXT}, "raisedBy": "pleaOnly",
             "counting": {"method": "excludeFirstDay", "extendToWorkingDay": True, "basis": ["b3"]},
             "effectWhenExpired": "warn", "message": {"ar": "تقادم.", "en": "Limitation."}, "basis": ["b3"],
             "extract": {"period": "خمس عشرة سنة", "sanction": "يتقادم"}}],
        "priorSteps": [
            {"id": "p1", "kind": "formalNotice", "slots": ["notice_served", "debtor_refused_in_writing"],
             "requiredWhen": {"slot": "default_on_due_date_agreed", "is": "no"},
             "notRequiredWhen": [{"when": {"slot": "debtor_refused_in_writing", "is": "yes"}, "basis": ["b6"]}],
             "acceptedMeans": [{"id": "m1", "label": TEXT, "basis": ["b5"]}],
             "effectWhenMissing": "warn", "effectWhenUnknown": "ask", "message": TEXT, "basis": ["b4", "b5"]}],
        "court": {"exits": [{"id": "x1", "when": {"slot": "public_body_party", "is": "yes"},
                             "goesTo": "administrativeCourt", "effect": "warn", "message": TEXT, "basis": ["b4", "b7"]}]},
        "openPoints": [], "checklist": [{"id": "k1", "item": "Is there a period?", "from": ["derivationA"],
                                         "answer": "applies", "refs": ["d1", "b2"]}],
        "examples": [{"id": "ex1", "by": "derivationA", "title": "t", "petitionDate": "2026-01-01", "slots": {},
                      "expect": {"findings": [{"ref": "d1", "effect": "blockCleanExport", "shownEffect": "warn",
                                               "toCheck": True}]}}],
        "history": {"agreementBeforeReconciliation": "1 of 1 matched items"}}


def make_world(tmp_path, uids=("fx-one", "fx-two")):
    """A fixture rules\\ folder with final records and the unit reports plant copies, and a store."""
    rules = tmp_path / "rules"
    finals = []
    for uid in uids:
        rc.save(str(rules / "final" / f"{uid}.json"), final_record(uid))
        rc.save(str(rules / "reports" / f"{uid}.checklist.json"), [{"id": "k1", "item": "Is there a period?"}])
        rc.save(str(rules / "reports" / f"{uid}.idmap.json"),
                [{"kind": "deadline", "a": "d1", "b": "d1", "reconciled": "d1"}])
        rc.save(str(rules / "boundary-cases" / f"{uid}.json"), [])
        finals.append(str(rules / "final" / f"{uid}.json"))
    store_path = tmp_path / "store" / "articles.json"
    rc.save(str(store_path), {"schemaVersion": "1.0", "inputs": {}, "entries": ENTRIES})
    return {"tmp": tmp_path, "finals": finals, "store_path": str(store_path), "store": st.Store(str(store_path))}


@pytest.fixture
def world(tmp_path):
    return make_world(tmp_path)


def plant(world, name="round", seed=7, checker=None):
    root = str(world["tmp"] / name)
    key = pl.plant_round(world["finals"], 1, seed, root, world["store"], SLOTS, checker)
    return root, key


def files(root):
    out = {}
    for d, _, names in os.walk(root):
        for n in names:
            p = os.path.join(d, n)
            out[os.path.relpath(p, root)] = open(p, "rb").read()
    return out


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


# --- plant --------------------------------------------------------------------------------------------------------------
FIELD = {"periodChanged": r"period\.value", "paragraphShifted": r"cite", "exceptionUncited": r"basis",
         "limitationForForfeiture": r"kind, effectWhenExpired", "startingEventChanged": r"startsFrom\.slot",
         "wrongArticle": r"cite", "repealedCited": r"cite", "exemptionDropped": r"notRequiredWhen\[\d+\]",
         "raisedByChanged": r"raisedBy", "blockForLimitation": r"effectWhenExpired",
         "conditionInverted": r"(requiredWhen|notRequiredWhen\[\d+\]\.when|when|alternatives\[\d+\]\.when)(\.\w+\[\d+\])*\.is",
         "doneBesideOpen": r"acceptedMeans"}
SORT = {"periodChanged": {"deadline"}, "limitationForForfeiture": {"deadline"}, "startingEventChanged": {"deadline"},
        "raisedByChanged": {"deadline"}, "blockForLimitation": {"deadline"}, "exemptionDropped": {"priorStep"},
        "doneBesideOpen": {"priorStep"}, "conditionInverted": {"priorStep", "exit", "section", "boundary"},
        "paragraphShifted": {"statement"}, "wrongArticle": {"statement"}, "repealedCited": {"statement"},
        "exceptionUncited": {"statement"}}


def undo(record, p):
    """The planted copy with only this plant taken back."""
    out = copy.deepcopy(record)
    _, x = pl.find_item(out, p["item"])
    if p["kind"] == "limitationForForfeiture":
        x.update(p["before"])
    elif p["kind"] == "exemptionDropped":
        x["notRequiredWhen"].insert(pl.parse(p["field"])[1], p["before"])
    elif p["kind"] == "doneBesideOpen":
        x["acceptedMeans"] = p["before"]
    else:
        pl.set_at(x, p["field"], p["before"])
    return out


def test_plant_is_deterministic_changes_only_the_named_field_and_never_the_store(world):
    before = sha(world["store_path"])
    root1, key1 = plant(world, "one")
    root2, key2 = plant(world, "two")
    assert sha(world["store_path"]) == before
    assert sorted(os.listdir(os.path.dirname(world["store_path"]))) == ["articles.json"]
    assert {k: v for k, v in key1.items() if k != "made"} == {k: v for k, v in key2.items() if k != "made"}
    one, two = files(root1), files(root2)
    one.pop(os.path.join("key", "key.json"))
    two.pop(os.path.join("key", "key.json"))
    assert one == two
    assert {p["kind"] for p in key1["plants"]} == set(pl.KINDS)  # the fixture holds a place for every kind

    copies = {c["copy"]: c for c in key1["copies"]}
    for c in key1["copies"]:
        rec = rc.load(pl.reconciled_path(root1, c["copy"]))
        assert rec["stage"] == "reconciled" and rec["id"] == c["unit"]  # the record's own id is kept
        assert not re.search(r'"(quote|source|grading|shownEffect|toCheck|agreementBeforeReconciliation)"',
                             json.dumps(rec)), c["copy"]
        if c["role"] != "scriptOnly":
            for suffix in ("checklist.json", "idmap.json"):
                assert os.path.isfile(pl.report_path(root1, c["copy"], suffix))
            assert os.path.isfile(pl.boundary_path(root1, c["copy"]))
    for unit_id in ("fx-one", "fx-two"):
        control = next(c["copy"] for c in key1["copies"] if c["unit"] == unit_id and c["role"] == "control")
        base = rc.load(pl.reconciled_path(root1, control))
        assert base == pl.reconciled_copy(final_record(unit_id))  # the control is untouched
        for cid in [c["copy"] for c in key1["copies"] if c["unit"] == unit_id and c["role"] != "control"]:
            mine = [p for p in key1["plants"] if p["copy"] == cid]
            rec = rc.load(pl.reconciled_path(root1, cid))
            if copies[cid]["role"] == "scriptOnly":
                assert len(mine) == 1 and mine[0]["kind"] in pl.SCRIPT_ONLY
            for p in mine:
                assert re.fullmatch(FIELD[p["kind"]], p["field"]), p
                kind, _ = pl.find_item(base, p["item"])
                assert kind in SORT[p["kind"]], p
                assert p["before"] != p["after"]
            taken_back, expect = rec, copy.deepcopy(base)
            for p in mine:
                if p["kind"] != "exceptionUncited":
                    taken_back = undo(taken_back, p)
                    continue
                # the statement and every reference to it go; nothing else
                assert f'"{p["item"]}"' not in json.dumps(rec)
                assert p["cite"] == p["before"]["cite"] and p["from"]
                expect["basis"] = [s for s in expect["basis"] if s["id"] != p["item"]]
                for key in ("deadlines", "priorSteps", "court", "checklist"):
                    pl._drop_ref(expect[key], p["item"])
            assert taken_back == expect, cid


def test_a_plant_the_rules_check_refuses_is_replaced_by_another_place_of_the_same_kind(tmp_path):
    world = make_world(tmp_path, ("fx-one", "fx-two", "fx-three"))  # three records leave a spare place
    _, first = plant(world, "free")
    refused = next(p for p in first["plants"] if p["kind"] == "paragraphShifted")

    def checker(path):  # refuses exactly that plant
        rec = rc.load(path)
        _, x = pl.find_item(rec, refused["item"])
        hit = os.path.basename(path) == f"{refused['copy']}.json" and x and x["cite"] == refused["after"]
        return [f"      basis[0] ({refused['item']}): refused for the test"] if hit else []

    _, key = plant(world, "checked", checker=lambda path: [x.strip() for x in checker(path)])
    mine = [p for p in key["plants"] if p["kind"] == "paragraphShifted"]
    assert not any((p["copy"], p["item"]) == (refused["copy"], refused["item"]) for p in mine)
    assert len(mine) == len([p for p in first["plants"] if p["kind"] == "paragraphShifted"])
    r = key["replaced"][0]
    assert (r["kind"], r["copy"], r["item"]) == ("paragraphShifted", refused["copy"], refused["item"])
    assert [q["kind"] for q in r["by"]] == ["paragraphShifted"]


def test_a_root_that_holds_a_round_is_never_planted_over(world):
    root, _ = plant(world, "once")
    with pytest.raises(FileExistsError):
        pl.plant_round(world["finals"], 1, 7, root, world["store"], SLOTS)


# --- the planted copies fail their checkers -----------------------------------------------------------------------------
def test_a_planted_period_fails_check_values(world):
    root, key = plant(world)
    p = next(p for p in key["plants"] if p["kind"] == "periodChanged")
    quoted, problems = rc.quote_record(rc.load(pl.reconciled_path(root, p["copy"])), world["store"])
    assert not problems
    found = rc.check_values(quoted)["problems"]
    assert any(x["item"] == p["item"] and x["problem"] == "valueMismatch" for x in found)
    control = next(c["copy"] for c in key["copies"] if c["unit"] == p["unit"] and c["role"] == "control")
    quoted, _ = rc.quote_record(rc.load(pl.reconciled_path(root, control)), world["store"])
    assert not [x for x in rc.check_values(quoted)["problems"] if x["item"] == p["item"]]


TOOL = unit.published_tool()


def boundary_run(record_path, cases_path):
    out = subprocess.run([TOOL, "examples", record_path, "--boundary", cases_path, "--json"], capture_output=True)
    text = out.stdout.decode("utf-8")
    return json.loads(text[text.index("["):])


def means_case(world):
    """A blind case about the paragraph of the accepted means: the step was served, but by which means is open."""
    path = world["tmp"] / "means-case.json"
    rc.save(str(path), [{"id": "bc1", "title": "Served, by an unknown means", "petitionDate": "2026-06-01",
                         "slots": {"notice_served": "yes", "default_on_due_date_agreed": "no"},
                         "expect": {"about": "[cpca 12 ¶2]", "outcome": "ask", "why": "The means decides it."}}])
    return str(path)


def declared_none(results):
    return [d for r in results if not r["passed"] for d in r["differences"] if "declared none although it applies" in d]


def means_alone(world):
    """The kind 12 plant alone on its record, and the untouched control."""
    root, key = plant(world)
    p = next(p for p in key["plants"] if p["kind"] == "doneBesideOpen")
    control = next(c["copy"] for c in key["copies"] if c["unit"] == p["unit"] and c["role"] == "control")
    alone = str(world["tmp"] / "alone.json")
    rc.save(alone, pl.apply(rc.load(pl.reconciled_path(root, control)), [p]))
    return alone, pl.reconciled_path(root, control)


@pytest.mark.skipif(TOOL is None, reason="needs the published rules tool (unit.py tool)")
def test_a_step_without_its_accepted_means_fails_the_boundary_runner(world):
    alone, _ = means_alone(world)
    assert declared_none(boundary_run(alone, means_case(world)))


@pytest.mark.xfail(strict=True, reason="the evaluator never reads acceptedMeans (RuleEvaluator.PriorStep): the control "
                                       "fails the same case, so the boundary runner cannot tell this plant apart")
@pytest.mark.skipif(TOOL is None, reason="needs the published rules tool (unit.py tool)")
def test_the_control_of_that_step_passes_the_boundary_runner(world):
    _, control = means_alone(world)
    assert not declared_none(boundary_run(control, means_case(world)))


# --- score --------------------------------------------------------------------------------------------------------------
def fake_rules_check(path):
    """What `rules check` says of the two script-only kinds: a limitation may not block."""
    rec = rc.load(path)
    return [f"deadlines[{i}] ({d['id']}): only a forfeiture may declare blockCleanExport; a limitation declares warn"
            for i, d in enumerate(rec.get("deadlines", []))
            if d["kind"] == "limitation" and d["effectWhenExpired"] == "blockCleanExport"]


def run_copies(root, key):
    """A fixture run: one plant restored after an attack, one changed to a third value after an attack, and on one
    control the verifier fixes an attack that was wrong."""
    first = next(p for p in key["plants"] if p["kind"] == "periodChanged")
    third = next(p for p in key["plants"] if p["kind"] == "raisedByChanged")
    for p, value in ((first, first["before"]), (third, next(x for x in pl.RAISED_BY if x not in
                                                            (third["before"], third["after"])))):
        rec = rc.load(pl.candidate_path(root, p["copy"])) if os.path.isfile(pl.candidate_path(root, p["copy"])) \
            else rc.load(pl.reconciled_path(root, p["copy"]))
        _, x = pl.find_item(rec, p["item"])
        pl.set_at(x, p["field"], value)
        rc.save(pl.candidate_path(root, p["copy"]), rec)
        path = pl.report_path(root, p["copy"], "attacks.sonnet.json")
        old = rc.load(path)["attacks"] if os.path.isfile(path) else []
        rc.save(path, {"attacks": old + [{"id": f"atk-{len(old) + 1}", "target": p["item"], "kind": "wrongValue",
                                          "claim": "The text says otherwise."}], "nothingFound": False})
    control = next(c["copy"] for c in key["copies"] if c["role"] == "control")
    rec = rc.load(pl.reconciled_path(root, control))
    rec["deadlines"][1]["message"] = {"ar": "تقادم مسقط.", "en": "A changed message."}
    rc.save(pl.candidate_path(root, control), rec)
    rc.save(pl.report_path(root, control, "attacks.json"),
            {"unit": "fx", "attacks": [{"id": "atk-1", "who": "attack", "target": "d2", "verdict": "fixed",
                                        "verifierQuote": "يتقادم"},
                                       {"id": "atk-2", "who": "attack", "target": "d1", "verdict": "refuted",
                                        "verifierQuote": "سقوط"}]})
    return first, third


def test_score_tells_caught_from_flagged_and_counts_false_alarms(world):
    root, key = plant(world)
    first, third = run_copies(root, key)
    entry = pl.score_round(1, root, world["store"], fake_rules_check, date="2026-11-23")
    kinds = entry["kinds"]
    n = {k: sum(1 for p in key["plants"] if p["kind"] == k) for k in pl.KINDS}
    assert kinds["periodChanged"]["planted"] == n["periodChanged"]
    assert kinds["periodChanged"]["caught"] == 1  # flagged and restored
    assert kinds["periodChanged"]["flagged"] == {"attack": 1, "verifier": 1}
    assert kinds["raisedByChanged"]["caught"] == 0  # flagged, but a third value
    assert kinds["raisedByChanged"]["flagged"] == {"attack": 1, "verifier": 1}
    assert kinds["exemptionDropped"]["flagged"] == {} and kinds["exemptionDropped"]["caught"] == 0
    # the script-only kinds: quote refuses the repealed article, rules check the block on a limitation
    assert kinds["repealedCited"]["caught"] == kinds["repealedCited"]["planted"] == n["repealedCited"] > 0
    assert kinds["repealedCited"]["flagged"]["quote"] == n["repealedCited"]
    assert kinds["blockForLimitation"]["caught"] == n["blockForLimitation"] > 0
    assert entry["controls"]["changedByVerifier"] == 1
    assert [(x["item"], x["attacks"]) for x in entry["controls"]["items"]] == [("d2", ["atk-1"])]
    assert entry["copies"] == {"planted": 2, "controls": 2, "scriptOnly": n["repealedCited"] + n["blockForLimitation"]}


def test_a_script_finding_the_control_also_shows_is_not_a_flag(world):
    root, key = plant(world)
    p = next(p for p in key["plants"] if p["kind"] == "periodChanged")
    control = next(c["copy"] for c in key["copies"] if c["unit"] == p["unit"] and c["role"] == "control")
    old = {"item": p["item"], "problem": "messagePeriodDiffers", "read": []}
    rc.save(pl.report_path(root, control, "values.reconciled.json"), {"problems": [old], "notes": []})
    rc.save(pl.report_path(root, p["copy"], "values.reconciled.json"), {"problems": [old], "notes": []})
    assert "check-values" not in pl.score_plant(root, p, control)["flagged"]
    new = {"item": p["item"], "problem": "valueMismatch", "read": {}, "field": {}}
    rc.save(pl.report_path(root, p["copy"], "values.reconciled.json"), {"problems": [old, new], "notes": []})
    assert "check-values" in pl.score_plant(root, p, control)["flagged"]


def test_score_writes_the_json_grade_reads_and_keeps_the_other_rounds(world, tmp_path):
    root, key = plant(world)
    run_copies(root, key)
    reports = tmp_path / "reports"
    reports.mkdir()
    doc = pl.write_reports(pl.score_round(1, root, world["store"], fake_rules_check, date="2026-11-23"), str(reports))
    assert set(doc) == {"rounds", "status"} and set(doc["status"]) == set(pl.KINDS)
    r = doc["rounds"][0]
    assert set(r) == {"round", "date", "copies", "kinds", "controls"}
    assert set(r["kinds"]["periodChanged"]) == {"planted", "flagged", "caught"}
    assert doc["status"]["periodChanged"] == "weak"  # round 1 alone: every plant must be caught
    md = (reports / "planted-errors.md").read_text(encoding="utf-8")
    assert pl.CANNOT_SHOW in md and "d1" not in md.split("Controls:")[0]  # rates only above the controls
    second = copy.deepcopy(r)
    second["round"] = 2
    second["_byOpenPoint"] = {}
    doc = pl.write_reports(second, str(reports))
    assert [x["round"] for x in rc.load(str(reports / "planted-errors.json"))["rounds"]] == [1, 2]


def test_status_follows_the_table_of_what_the_rates_decide():
    def rnd(n, kinds):
        return {"round": n, "kinds": {k: {"planted": a, "flagged": {}, "caught": b} for k, (a, b) in kinds.items()}}
    one = pl.status([rnd(1, {"periodChanged": (3, 3), "paragraphShifted": (2, 2), "wrongArticle": (5, 4)})])
    assert one["periodChanged"] == "passed" and one["paragraphShifted"] == "weak" and one["wrongArticle"] == "weak"
    assert one["repealedCited"] == "notRun"
    both = pl.status([rnd(1, {"periodChanged": (5, 5), "wrongArticle": (5, 4)}),
                      rnd(2, {"periodChanged": (5, 4), "wrongArticle": (5, 5)})])
    assert both["periodChanged"] == "passed" and both["wrongArticle"] == "passed"  # 9 of 10
    assert pl.status([rnd(1, {"periodChanged": (5, 4)}), rnd(2, {"periodChanged": (5, 4)})])["periodChanged"] == "weak"


# --- grade reads the rates ----------------------------------------------------------------------------------------------
def test_grade_gives_checker_weak_to_a_deadline_and_not_to_a_statement(world, tmp_path):
    def rnd(n):
        kinds = {k: {"planted": 5, "flagged": {}, "caught": 5} for k in pl.KINDS}
        kinds["periodChanged"] = {"planted": 5, "flagged": {"check-values": 5}, "caught": 4}
        return {"round": n, "date": "2026-11-23", "copies": {"planted": 5, "controls": 5, "scriptOnly": 10},
                "kinds": kinds, "controls": {"changedByVerifier": 0, "items": []}, "_byOpenPoint": {}}
    reports = tmp_path / "reports"
    reports.mkdir()
    pl.write_reports(rnd(1), str(reports))
    doc = pl.write_reports(rnd(2), str(reports))
    assert doc["status"]["periodChanged"] == "weak"  # 8 of 10, under 90 %
    planted = rc.load(str(reports / "planted-errors.json"))
    quoted, problems = rc.quote_record(pl.reconciled_copy(final_record("fx-one")), world["store"])
    assert not problems
    _, report = rc.grade_record(quoted, world["store"], planted=planted, disputes=[], triaged={"cpca"})
    reasons = {row["item"]: row["reasons"] for row in report["items"]}
    assert "checkerWeak: periodChanged" in reasons["d1"]
    assert not any(r.startswith("checkerWeak") for r in reasons["b1"])
    assert not any(r.startswith("checkerWeak") or r == "plantedNotRun" for r in reasons["b1"])
