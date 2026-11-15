"""The unit driver (specs/unit-driver.md), command by command as each is written in phase 0."""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import unit  # noqa: E402


def write_ledger(path, rows):
    with open(path, "w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r) + "\n")


def test_the_last_ledger_line_of_a_run_is_its_state(tmp_path):
    ledger = tmp_path / "runs.jsonl"
    write_ledger(ledger, [
        {"id": "r-1", "kind": "agent", "status": "running"}, {"id": "r-1", "kind": "agent", "status": "done"},
        {"id": "r-2", "kind": "agent", "status": "running"},
        {"id": "r-3", "kind": "codex", "status": "running"},
    ])
    assert unit.running_agents(str(ledger)) == ["r-2"]
    assert unit.running_agents(str(tmp_path / "none.jsonl")) == []


def test_publishing_is_refused_while_an_agent_is_running(tmp_path):
    ledger = tmp_path / "runs.jsonl"
    write_ledger(ledger, [{"id": "r-9", "kind": "agent", "status": "running"}])
    try:
        unit.publish(tools=str(tmp_path / "tools"), ledger=str(ledger))
    except RuntimeError as e:
        assert "r-9" in str(e)
    else:
        raise AssertionError("publish ran while an agent was marked running")


def test_the_current_tool_is_the_one_current_txt_names_if_it_exists(tmp_path):
    assert unit.published_tool(str(tmp_path)) is None
    exe = tmp_path / "abc1234" / "rules.exe"
    exe.parent.mkdir()
    (tmp_path / "current.txt").write_text(str(exe) + "\n", encoding="utf-8")
    assert unit.published_tool(str(tmp_path)) is None  # named, but not there
    exe.write_bytes(b"")
    assert unit.published_tool(str(tmp_path)) == str(exe)


# --- retool item 3: the driver's commands (specs/unit-driver.md, "Tests the driver must have") -----------------------
import difflib  # noqa: E402
from pathlib import Path  # noqa: E402

import pytest  # noqa: E402

import prompts  # noqa: E402

CASE = {"id": "bc1", "title": "t", "petitionDate": "2026-06-15", "slots": {"notice_served": "no"},
        "expect": {"about": "[civil-code 179 ¶1]", "outcome": "stepMissing", "why": "w"}}


def dump(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False), encoding="utf-8")


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


@pytest.fixture
def root(tmp_path, monkeypatch):
    """A planted-round style root: rules\\ and runs\\ under one folder; the French sources list holds the modules."""
    french = tmp_path / "french-sources.json"
    dump(french, {"units": {"periods": [{"what": "the Civil Code", "paths": []}],
                            "proof": [{"what": "the Civil Code", "paths": []}]}})
    monkeypatch.setattr(prompts, "FRENCH_SOURCES", str(french))
    monkeypatch.setattr(unit, "_requests_appended", lambda u: True)
    return tmp_path / "round"


def through_stage_4(root, name="proof", deadline=False):
    u = unit.Unit(name, str(root))
    r = root / "rules"
    dump(r / "reports" / f"{name}.texts.json", {"rendered": ""})
    for side in ("a", "b"):
        dump(r / "derivations" / side / f"{name}.json", {"deadlines": [{"id": "d1"}] if deadline else []})
        dump(r / "derivations" / side / f"{name}.checklist.json", {})
    dump(r / "derivations" / "c" / f"{name}.json", {})
    dump(r / "boundary-cases" / f"{name}.json", [CASE])
    for path in unit.stage4_reports(u):
        dump(path, {"differences": [{"id": "x1"}]} if path.endswith("differences.json") else {})
    return u


def test_status_reads_each_stage_from_the_files_alone(root):
    u = unit.Unit("proof", str(root))
    s = dict(unit.status(u))
    assert s["0"] == "due" and s["2a"] == "waiting for 0" and s["0b"] == "done"
    u = through_stage_4(root)
    s = dict(unit.status(u))
    assert [s[x] for x in ("0", "2a", "2b", "2c", "2d", "3", "4")] == ["done"] * 7
    assert s["5"] == "due" and s["6"] == "waiting for 5" and s["9"].startswith("waiting for 8")


def test_stage_5_is_not_done_while_a_difference_has_no_line_in_the_report(root):
    u = through_stage_4(root)
    dump(u.reconciled, {})
    Path(u.r("reconciliation.md")).write_text("Nothing in the fixed form.\n", encoding="utf-8")
    assert dict(unit.status(u))["5"].startswith("blocked: no «difference <id>:» line for x1")
    Path(u.r("reconciliation.md")).write_text("difference x1: settled by the text\n", encoding="utf-8")
    assert dict(unit.status(u))["5"] == "done"


def test_stage_5_waits_for_stage_4_to_be_committed(root, monkeypatch):
    u = through_stage_4(root)
    monkeypatch.setattr(unit, "_committed", lambda paths: False)
    assert dict(unit.status(u))["5"] == "blocked: commit stage 4's reports first"


def test_a_unit_with_a_deadline_waits_for_periods_and_periods_itself_does_not(root, monkeypatch):
    modules = prompts.handbook_modules()
    monkeypatch.setattr(prompts, "CL", str(root))  # module_finals and has_deadline read <CL>\rules
    monkeypatch.setattr(prompts, "handbook_modules", lambda: modules)
    u = through_stage_4(root, "proof", deadline=True)
    assert dict(unit.status(u))["5"].startswith("waiting for proof has a deadline and periods is not final")
    p = through_stage_4(root, "periods", deadline=True)
    assert dict(unit.status(p))["5"] == "due"
    dump(root / "rules" / "final" / "periods.json", {})
    assert dict(unit.status(u))["5"] == "due"


def test_a_boundary_file_of_the_wrong_shape_is_blocked_and_done_sets_it_aside(root, tmp_path, monkeypatch):
    u = through_stage_4(root)
    dump(u.boundary, {"cases": [CASE]})  # an object, not an array
    assert dict(unit.status(u))["2d"].startswith("blocked: the file has the wrong shape")
    ledger = tmp_path / "ledger.jsonl"
    write_ledger(ledger, [{"id": "r-s1-0001", "kind": "agent", "unit": "proof", "stage": "2d", "status": "running",
                           "role": "rules-boundary-cases"}])
    monkeypatch.setattr(unit, "tool", lambda: "rules.exe")
    assert unit.done(u, "2d", "agent-1", ledger=str(ledger)) == 1
    assert not os.path.isfile(u.boundary)
    assert os.path.isfile(os.path.join(u.run, "discarded", "boundary-cases-r-s1-0001.json"))
    assert unit.ledger_rows(str(ledger))["r-s1-0001"]["status"] == "failed"


def test_done_discards_a_no_tool_agent_that_used_another_tool(root, tmp_path, monkeypatch):
    u = through_stage_4(root)
    ledger = tmp_path / "ledger.jsonl"
    write_ledger(ledger, [{"id": "r-s1-0002", "kind": "agent", "unit": "proof", "stage": "7b", "status": "running",
                           "role": "rules-attack"}])
    monkeypatch.setattr(unit, "tool", lambda: "rules.exe")
    calls = []
    monkeypatch.setattr(unit, "py", lambda *a, **k: calls.append(a) or (3, "tool used"))
    assert unit.done(u, "7b", "agent-2", ledger=str(ledger)) == 1
    assert calls[0][:2] == ("records.py", "ingest-reply") and "--ids" in calls[0]
    row = unit.ledger_rows(str(ledger))["r-s1-0002"]
    assert row["status"] == "failed" and "a tool other than its own prompt's read" in row["reason"]


def sources(root, u):
    r = root / "rules" / "reports"
    dump(r / "proof.attacks.adversary.json", {"attacks": [{"id": "adv-1", "target": "b1", "claim": "c"}]})
    dump(r / "proof.attacks.sonnet.json", {"attacks": [{"id": "atk-1", "target": "b1", "claim": "c"}]})
    dump(r / "proof.c-comparison.json", {"attacks": [{"target": "cite x", "claim": "c"}]})
    dump(r / "proof.flags.reconciled.json", [{"id": "f-b1-next", "statement": "b1", "code": "civil-code",
                                               "article": "2", "paragraph": 1, "word": "غير", "text": "نص"}])
    dump(r / "proof.checklist-check.reconciled.json", {"unansweredFlags": ["f-b1-next"], "ok": False})
    dump(r / "proof.values.reconciled.json", {"problems": [{"item": "d1", "field": "period"}]})
    dump(Path(u.cases) / "boundaryReconciled.json", [
        {"caseId": "bc1", "title": "t", "passed": False, "differences": ["p1: declared none, the outcome StepMissing "
                                                                         "needs warn (p1: asks x -> none)"]}])
    os.makedirs(u.run, exist_ok=True)
    with open(os.path.join(u.run, "check.reconciled.txt"), "w", encoding="utf-8") as f:
        f.write("FAIL  x.json (1 problems)\n      b2: cite [civil-code 9 ¶3]: no such paragraph in the store\n")


def test_verify_input_gathers_every_source_with_its_ids_and_marks_a_missing_support_check(root):
    u = through_stage_4(root)
    sources(root, u)
    doc = unit.first_input(u)
    who = {e["who"]: e["id"] for e in doc["attacks"]}
    assert who == {"adversary": "adv-1", "attack": "atk-1", "derivationC": "c-1", "neighbourFlag": "flag-f-b1-next",
                   "boundaryCase": "bc-bc1", "owner": "done-p1", "values": "values-d1", "store": "store-b2"}
    assert doc["pending"] == ["supportCheck"]
    dump(root / "rules" / "reports" / "proof.support.json", {"attacks": [{"id": "sup-3", "who": "supportCheck",
                                                                         "target": "b1", "claim": "partly"}]})
    doc = unit.first_input(u)
    assert "pending" not in doc and {"id": "sup-3", "who": "support", "target": "b1", "claim": "partly"} in doc["attacks"]


def test_a_follow_up_numbers_its_file_and_renames_a_support_id_an_earlier_register_used(root):
    u = through_stage_4(root)
    reports = root / "rules" / "reports"
    dump(reports / "proof.attacks.json", {"attacks": [{"id": "sup-3", "target": "b9", "verdict": "refuted"},
                                                      {"id": "bc-bc12", "who": "boundaryCase", "verdict": "open"}]})
    dump(reports / "proof.support.json", {"attacks": [{"id": "sup-3", "target": "b1", "claim": "partly"},
                                                      {"id": "sup-4", "target": "b2", "claim": "partly"}]})
    dump(Path(u.cases) / "boundaryFinal.json", [
        {"caseId": "bc1", "title": "t", "passed": False, "differences": ["p1: declared ask (p1: asks y -> ask)"]}])
    first = unit.verify_input(u, followup=True)
    assert first.endswith("proof.attacks.followup1-input.json")
    ids = [e["id"] for e in load(first)["attacks"]]
    assert ids == ["bc-bc1", "sup-3-f1", "sup-4"]  # bc1 is not decided because bc12 is
    dump(reports / "proof.attacks.followup1.json", {"attacks": [{"id": i, "verdict": "refuted", "verifierQuote": "q"}
                                                                for i in ids]})
    with pytest.raises(RuntimeError):  # nothing new
        unit.verify_input(u, followup=True)
    assert unit.undecided_input(u) == []


def test_the_slots_step_appends_in_the_files_own_format_and_refuses_a_question_without_its_mark(root, tmp_path):
    with open(os.path.join(prompts.CL, "slots.json"), encoding="utf-8") as f:
        before = f.read()
    path = tmp_path / "slots.json"
    path.write_text(before, encoding="utf-8")
    u = through_stage_4(root)
    new = {"id": "proof_in_writing", "type": "yesNoUnknown", "question": {"ar": "هل الإثبات مكتوب؟", "en": "Written?"},
           "why": "w"}
    dump(u.candidate, {"proposedSlots": [{"id": "notice_served"}, new]})
    assert unit.slots(u, str(path)) == ["proof_in_writing"]
    after = path.read_text(encoding="utf-8")
    removed = [x for x in difflib.unified_diff(before.splitlines(), after.splitlines(), lineterm="", n=0)
               if x.startswith("-") and not x.startswith("---")]
    assert removed == []  # the diff is additions only
    added = json.loads(after)["slots"][-1]
    assert added["askedFor"] == ["*"] and added["origin"] == "proof" and added["readFromNote"] is False
    dump(u.candidate, {"proposedSlots": [dict(new, id="other", question={"ar": "سؤال", "en": "q"})]})
    with pytest.raises(ValueError):
        unit.slots(u, str(path))


def test_every_child_process_starts_without_either_api_key(monkeypatch):
    monkeypatch.setenv("OPENROUTER_API_KEY", "x")
    monkeypatch.setenv("GOOGLE_API_KEY", "y")
    code, out = unit.py("-c", "import os; print(sorted(k for k in os.environ if k.endswith('_API_KEY')))")
    assert code == 0 and "OPENROUTER" not in out and "GOOGLE" not in out


def test_formal_notice_shows_its_stages_done_through_the_legacy_map():
    """The first module was made before the driver (its attack is Codex's, its report has no fixed lines)."""
    s = dict(unit.status(unit.Unit("formal-notice")))
    assert [s[x] for x in ("0", "0b", "2a", "2b", "2c", "2d", "3", "4", "5", "6", "7a", "7b", "7c")] == ["done"] * 13


def test_a_case_codex_reads_as_the_writer_did_becomes_uncertain(root):
    u = through_stage_4(root)
    reports = root / "rules" / "reports"
    dump(reports / "proof.attacks.json", {"attacks": [{"id": "bc7", "who": "boundaryCase", "target": "p1",
                                                       "verdict": "open"}]})
    dump(reports / "proof.boundary-view.json", {
        "cases": ["bc3", "bc7"], "writerExpected": {"bc3": "stepMissing", "bc7": "stepMissing"},
        "recordActual": {"bc7": "stepDone"},
        "codex": {"cases": [{"id": "bc3", "answer": "ask", "why": "w", "decidingWords": "d"},
                            {"id": "bc7", "answer": "stepMissing", "why": "w", "decidingWords": "d"}]}})
    assert [(e["id"], e["target"]) for e in unit.uncertain_entries(u)] == [("uncertain-bc7", "p1")]


# --- unit.py codex (stages 7c and 8b through the background queue) -----------------------------------------------------
class FakeQueue:
    def __init__(self):
        self.added, self.started = [], 0

    def add(self, role, prompt, schema, out, **kw):
        self.added.append(dict(kw, role=role, prompt=prompt, schema=schema, out=out))
        return self.added[-1]

    def start(self):
        self.started += 1
        return True


def test_codex_queues_the_second_view_with_its_after_step_and_writes_its_ledger_line(tmp_path, monkeypatch):
    u = unit.Unit("formal-notice")
    if unit.second_view_due(u)[0] != ["bc7"]:
        pytest.skip("formal-notice's bc7 is no longer due for a second view")
    u.run = str(tmp_path / "run")  # the filled prompt goes here, not into the working folder
    ledger, q = tmp_path / "ledger.jsonl", FakeQueue()
    added = unit.codex(u, queue=q, ledger=str(ledger))
    assert added and added[0].startswith("8b ") and q.started == 1
    call = q.added[0]
    assert call["role"] == "boundary-second-view-codex" and call["item"] == "formal-notice:view:bc7"
    assert call["after"][0][-5:-2] == ["store-view", "formal-notice", call["out"]]
    text = open(call["prompt"], encoding="utf-8").read()
    assert "**bc7**" in text and "{{" not in text
    row = unit.ledger_rows(str(ledger))[call["call_id"]]
    assert row["status"] == "running" and row["stage"] == "8b" and row["cases"] == ["bc7"]


def test_store_view_keeps_both_words_and_refuses_a_word_not_offered(root):
    u = through_stage_4(root)
    answer = root / "answer.json"
    dump(answer, {"cases": [{"id": "bc1", "answer": "inTime", "why": "w", "decidingWords": "d"}]})
    with pytest.raises(ValueError):
        unit.store_view(u, str(answer), {"bc1": "ask"})
    dump(answer, {"cases": [{"id": "bc1", "answer": "ask", "why": "w", "decidingWords": "d"}]})
    view = load(unit.store_view(u, str(answer), {"bc1": "ask"}))
    assert view["cases"] == ["bc1"] and view["writerExpected"] == {"bc1": "stepMissing"}
    assert view["recordActual"] == {"bc1": "ask"} and view["codex"]["cases"][0]["answer"] == "ask"


def test_a_case_failing_by_a_known_limit_is_neither_a_follow_up_nor_sent_to_codex(root):
    u = through_stage_4(root)
    reports = root / "rules" / "reports"
    dump(reports / "proof.attacks.json", {"attacks": [{"id": "bc-bc1", "who": "boundaryCase", "target": "p1",
                                                       "verdict": "refuted", "verifierQuote": "q"}]})
    dump(Path(u.cases) / "boundaryFinal.json", [{"caseId": "bc1", "title": "t", "passed": False, "actual": "ask",
                                                 "differences": ["p1: declared ask (p1: asks y -> ask)"]}])
    assert unit.second_view_due(u) == (["bc1"], [])
    dump(reports / "proof.boundary-limits.json", {"cases": [{"case": "bc1", "kind": "languageLimit",
                                                            "why": "no comparison of a date slot with the petition date"}]})
    assert unit.second_view_due(u) == ([], []) and unit.followup_entries(u) == []
    assert "languageLimit" in unit.tracking_row(u)


def test_legal_questions_keep_entry_4_while_the_readings_do_not_change(root, tmp_path):
    u = through_stage_4(root)
    point = {"id": "o1", "grade": "C", "refs": ["p1"], "question": {"en": "Q?", "ar": "س؟"}, "why": {"en": "w"},
             "readings": [{"reading": {"en": "R1"}, "basis": ["b1"]}], "protectiveReading": {"en": "warn"}}
    dump(u.final, {"priorSteps": [{"id": "p1"}], "openPoints": [point]})
    path = str(tmp_path / "open-legal-questions.md")
    unit.questions([u], path)
    assert unit.unfilled_questions(path) == 1
    text = open(path, encoding="utf-8").read().replace(unit.PLACEHOLDER4, "If R1: p1 warns.")
    open(path, "w", encoding="utf-8").write(text)
    unit.questions([u], path)
    assert unit.unfilled_questions(path) == 0 and "If R1: p1 warns." in open(path, encoding="utf-8").read()
    point["readings"].append({"reading": {"en": "R2"}, "basis": ["b2"]})
    dump(u.final, {"priorSteps": [{"id": "p1"}], "openPoints": [point]})
    unit.questions([u], path)
    assert unit.unfilled_questions(path) == 1  # a new reading: the executor writes entry 4 again


def test_an_owner_decision_makes_a_follow_up_of_its_own(root):
    u = through_stage_4(root)
    path = unit.verify_input(u, followup=True, owner=[{"id": "owner-x", "target": "p1", "claim": "use the new form"}])
    entry = load(path)["attacks"][0]
    assert (entry["id"], entry["who"], entry["kind"]) == ("owner-x", "owner", "wrongCondition")

