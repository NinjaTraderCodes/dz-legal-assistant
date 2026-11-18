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


def test_a_view_rejected_twice_is_never_sent_again_and_its_cases_become_uncertain(root, tmp_path):
    u = through_stage_4(root)
    reports = root / "rules" / "reports"
    dump(reports / "proof.attacks.json", {"attacks": [{"id": "bc-bc1", "who": "boundaryCase", "target": "p1",
                                                       "verdict": "refuted", "verifierQuote": "q"}]})
    dump(Path(u.cases) / "boundaryFinal.json", [{"caseId": "bc1", "title": "t", "passed": False, "actual": "ask",
                                                 "differences": ["p1: declared ask"]}])
    ledger = tmp_path / "runs.jsonl"
    row = {"kind": "codex", "unit": "proof", "stage": "8b", "cases": ["bc1"], "status": "rejected"}
    write_ledger(ledger, [dict(row, id="r-1")])
    assert unit.second_view_due(u, str(ledger)) == (["bc1"], []) and unit.uncertain_entries(u, str(ledger)) == []
    write_ledger(ledger, [dict(row, id="r-1"), dict(row, id="r-2")])
    assert unit.views_set_aside(u, str(ledger)) == ["bc1"]
    assert unit.second_view_due(u, str(ledger)) == ([], [])
    assert [(e["id"], e["target"]) for e in unit.uncertain_entries(u, str(ledger))] == [("uncertain-bc1", "p1")]


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


def test_store_view_keeps_both_words_refuses_a_non_word_and_keeps_a_third_word_as_uncertain(root):
    u = through_stage_4(root)
    answer = root / "answer.json"
    dump(answer, {"cases": [{"id": "bc1", "answer": "maybe", "why": "w", "decidingWords": "d"}]})
    with pytest.raises(ValueError):  # not an outcome word at all: the call is refused
        unit.store_view(u, str(answer), {"bc1": "ask"})
    dump(answer, {"cases": [{"id": "bc1", "answer": "ask", "why": "w", "decidingWords": "d"}]})
    view = load(unit.store_view(u, str(answer), {"bc1": "ask"}))
    assert view["cases"] == ["bc1"] and view["writerExpected"] == {"bc1": "stepMissing"}
    assert view["recordActual"] == {"bc1": "ask"} and view["codex"]["cases"][0]["answer"] == "ask"
    # A third outcome word (neither the writer's stepMissing nor the record's ask) is kept, and the case is uncertain.
    reports = root / "rules" / "reports"
    dump(reports / "proof.attacks.json", {"attacks": [{"id": "bc-bc1", "who": "boundaryCase", "target": "p1",
                                                       "verdict": "refuted", "verifierQuote": "q"}]})
    dump(answer, {"cases": [{"id": "bc1", "answer": "inTime", "why": "w", "decidingWords": "d"}]})
    view = load(unit.store_view(u, str(answer), {"bc1": "ask"}))
    assert view["codex"]["cases"][0]["answer"] == "inTime"
    assert [e["id"] for e in unit.uncertain_entries(u, str(root / "no-ledger.jsonl"))] == ["uncertain-bc1"]


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


def test_the_adversary_file_is_read_as_its_prompt_writes_it_an_array_or_the_first_modules_object():
    assert unit.attack_list([{"id": "adv-1"}]) == [{"id": "adv-1"}]
    assert unit.attack_list({"attacks": [{"id": "adv-1"}]}) == [{"id": "adv-1"}]



# contract-rescission: a claim type that stays on the light or full route (loan-repayment, the first fixture here, has
# been on the catalogue route since 18 November 2026; the route tests below are unchanged).
def claim_through_stage_2(root, name="contract-rescission", blocking=False):
    u = unit.Unit(name, str(root))
    r = root / "rules"
    dump(r / "reports" / f"{name}.texts.json", {"rendered": ""})
    deadline = {"id": "d1", "effectWhenExpired": "blockCleanExport" if blocking else "warn"}
    for side in ("a", "b"):
        dump(r / "derivations" / side / f"{name}.json", {"deadlines": [deadline] if side == "a" else []})
        dump(r / "derivations" / side / f"{name}.checklist.json", {})
    return u


def test_a_claim_type_with_no_blocking_item_takes_the_light_route(root):
    u = claim_through_stage_2(root)
    assert u.kind == "claim" and unit.blocking_items(u) == [] and unit.light(u)
    s = dict(unit.status(u))
    assert s["2c"] == "not for a claim type" and s["2d"] == unit.LIGHT
    assert s["7b"] == unit.LIGHT and s["7c"] == unit.LIGHT and s["8b"] == unit.LIGHT
    assert not unit.followup_allowed(u)
    # Before both derivations exist the route cannot be read, and the boundary cases wait for it.
    fresh = unit.Unit("unpaid-rent", str(root))
    assert unit.light(fresh) is None and dict(unit.status(fresh))["2d"].startswith("waiting for")


def test_a_claim_type_with_a_blocking_item_keeps_the_full_route_with_one_follow_up(root):
    u = claim_through_stage_2(root, blocking=True)
    assert unit.blocking_items(u) == ["d1"] and unit.light(u) is False
    s = dict(unit.status(u))
    assert s["2d"] == "due" and s["7b"] != unit.LIGHT and s["8b"] != unit.LIGHT
    assert unit.followup_allowed(u)
    dump(u.followup_input(1), {"attacks": []})
    assert not unit.followup_allowed(u)  # at most one follow-up run per unit


def test_findings_left_without_a_follow_up_are_recorded_open_and_count_as_decided(root):
    u = claim_through_stage_2(root)
    entry = {"id": "values-d1-1", "who": "values", "target": "d1", "kind": "wrongValue"}
    path = unit.write_open(u, [entry])
    assert path in u.registers()
    assert unit._decided(u, "values-d1-1")
    opened = [e for e in unit.register_entries(u) if e["id"] == "values-d1-1"][0]
    assert opened["verdict"] == "open" and "open, warn" in opened["note"]
    # The grader reads it as an open attack on d1.
    import records as rc
    assert "d1" in rc.attack_targets(opened) and opened["verdict"] not in ("refuted", "fixed")


def test_on_the_light_route_a_flag_recorded_open_passes_the_final_checklist_check_and_no_other(root, monkeypatch):
    # regrade --all of 18 November 2026: tatliq's flag f-b78-previous was recorded open at stage 9 and still failed.
    u = claim_through_stage_2(root)
    dump(u.r("grades.json"), {"items": []})
    dump(u.r("checklist-check.json"), {"unansweredLines": [], "unansweredFlags": ["f-b78-previous"], "refsNotInRecord": []})
    monkeypatch.setattr(unit, "_flat", lambda *a, **k: u.candidate_q)
    monkeypatch.setattr(unit, "file_sha", lambda *a, **k: "x")
    monkeypatch.setattr(unit, "tool", lambda: "rules.exe")
    monkeypatch.setattr(unit, "run", lambda *a, **k: (0, ""))
    monkeypatch.setattr(unit, "py", lambda *a, **k: (1, "unanswered") if a[1] == "check-checklist" else (0, ""))
    assert not unit.grade(u)  # not recorded anywhere: it fails
    unit.write_open(u, [{"id": "flag-f-b78-previous", "who": "neighbourFlag", "target": "b78", "kind": "exception"}])
    assert unit.grade(u)  # recorded open, warn: decided
    dump(u.r("checklist-check.json"), {"unansweredLines": ["k3"], "unansweredFlags": ["f-b78-previous"], "refsNotInRecord": []})
    assert not unit.grade(u)  # an unanswered checklist line still fails


# --- the catalogue route (the owner, 18 November 2026) ----------------------------------------------------------------
CAT_RECORD = {
    "id": "loan-repayment", "kind": "claim", "stage": "derivationA", "verified": False,
    "derivedBy": {"role": "derivationA", "model": "claude-opus-5-5", "date": "2026-11-18"},
    "basis": [{"id": "b1", "role": "limit", "cite": {"code": "civil-code", "article": "308", "paragraphs": [1]},
               "says": {"ar": "أجل", "en": "a period"}},
              {"id": "b2", "role": "prerequisite", "cite": {"code": "civil-code", "article": "179"},
               "says": {"ar": "إعذار", "en": "a notice"}}],
    "deadlines": [{"id": "d1", "kind": "forfeiture", "period": {"value": 15, "unit": "year"}, "basis": ["b1"],
                   "effectWhenExpired": "blockCleanExport",
                   "extract": {"period": "خمس عشرة سنة", "sanction": "يسقط"}}],
    "priorSteps": [{"id": "p1", "kind": "formalNotice", "basis": ["b2"], "effectWhenMissing": "blockCleanExport",
                    "effectWhenUnknown": "ask"}],
    "examples": [{"id": "ex1", "expect": {"findings": [{"ref": "d1", "effect": "blockCleanExport"},
                                                       {"ref": "p1", "effect": "blockCleanExport"}]}}]}
QUESTIONS = [{"item": "d1", "question": {"ar": "في أي أجل ترفع الدعوى؟", "en": "Within what time must it be filed?"}},
             {"item": "p1", "question": {"ar": "هل يجب إعذار المدين قبل رفع الدعوى؟",
                                         "en": "Must the debtor be given notice before filing?"}}]


def view_answer(item, require="yes", value=None, unit_="none", consequence="inadmissibleOrForfeited", basis=()):
    return {"item": item, "textsRequire": require, "periodValue": value, "periodUnit": unit_,
            "consequence": consequence, "basis": list(basis), "decidingWords": "نص", "why": "w"}


@pytest.fixture
def cat(root, monkeypatch):
    monkeypatch.setattr(prompts, "module_finals", lambda units, strict=False: "none")
    return root


def catalogue_through_stage_2a(root, blocking=True):
    u = unit.Unit("loan-repayment", str(root))
    r = root / "rules"
    dump(r / "reports" / "loan-repayment.texts.json", {"rendered": ""})
    record = json.loads(json.dumps(CAT_RECORD))
    if not blocking:
        record["deadlines"][0]["effectWhenExpired"] = "warn"
        record["priorSteps"][0]["effectWhenMissing"] = "warn"
    dump(u.deriv("a"), record)
    dump(u.deriv("a").replace(".json", ".checklist.json"), [])
    dump(unit.changes_path(u), {"unit": "loan-repayment", "changes": [], "wouldBlock": QUESTIONS if blocking else []})
    return u


def test_the_catalogue_route_holds_the_ten_claim_types_and_its_own_agents_and_scripts(root):
    assert len(unit.CATALOGUE_ROUTE) == 10 and all(prompts.unit_kind(c) == "claim" for c in unit.CATALOGUE_ROUTE)
    u = unit.Unit("loan-repayment", str(root))
    assert unit.catalogue_route(u) and unit.agents_of(u)["2a"] == ("rules-derive-catalogue", "opus", False)
    assert unit.agents_of(u)["2e"] == ("rules-blocker-view", "sonnet", True)
    assert set(unit.agents_of(u)) == {"2a", "2e", "7a", "8"}  # no B, no boundary writer, no reconciler, no attack
    assert unit.script_stages(u)["5"] is unit.stage5_catalogue and unit.script_stages(u)["4"] is unit.stage4_catalogue
    # every other unit keeps its route, and its status has no stage 2e
    for other in ("contract-rescission", "proof"):
        o = unit.Unit(other, str(root))
        assert not unit.catalogue_route(o) and unit.agents_of(o) is unit.AGENTS
        assert unit.script_stages(o) is unit.SCRIPT_STAGES and "2e" not in dict(unit.status(o))


def test_catalogue_route_status_skips_b_boundary_cases_reconciler_attack_and_codex(cat):
    u = unit.Unit("loan-repayment", str(cat))
    s = dict(unit.status(u))
    assert s["0"] == "due" and s["2a"] == "waiting for 0" and s["0b"].startswith("skipped: catalogue route")
    u = catalogue_through_stage_2a(cat, blocking=False)
    os.remove(unit.changes_path(u))
    assert dict(unit.status(u))["2a"] == "due"  # the record without its side file of changes is not done
    u = catalogue_through_stage_2a(cat, blocking=False)
    s = dict(unit.status(u))
    assert s["2a"] == "done" and s["2b"] == unit.CATALOGUE and s["2c"] == "not for a claim type"
    assert s["2d"] == unit.CATALOGUE and s["2e"].startswith("skipped: catalogue route (no item")
    assert s["3"] == "done" and s["4"] == "due" and s["5"] == "waiting for 4"
    for path in unit.catalogue_stage4_reports(u):
        dump(path, {})
    s = dict(unit.status(u))
    assert s["4"] == "done" and s["5"] == "due" and s["6"] == "waiting for 5"
    assert s["7b"] == s["7c"] == s["8b"] == unit.CATALOGUE
    assert not unit.followup_allowed(u)  # as on the light route: open findings are recorded open, warn


def test_catalogue_route_stage_5_waits_for_the_imported_modules(cat, monkeypatch):
    u = catalogue_through_stage_2a(cat, blocking=False)
    for path in unit.catalogue_stage4_reports(u):
        dump(path, {})

    def not_final(units, strict=False):
        raise ValueError("loan-repayment imports modules that are not final: proof")
    monkeypatch.setattr(prompts, "module_finals", not_final)
    assert dict(unit.status(u))["5"].startswith("waiting for loan-repayment imports modules that are not final")


def test_the_blocker_view_is_due_only_with_a_would_be_blocking_item_and_promotion_waits_for_it(cat):
    u = catalogue_through_stage_2a(cat, blocking=True)
    for path in unit.catalogue_stage4_reports(u):
        dump(path, {})
    s = dict(unit.status(u))
    assert s["2e"] == "due" and s["5"] == "waiting for 2e"
    dump(u.r("blocker-view.json"), {"unit": u.id, "items": []})
    s = dict(unit.status(u))
    assert s["2e"] == "done" and s["5"] == "due"


def test_promotion_keeps_the_blocks_the_blind_reader_agrees_with_and_sets_the_others_to_warn(cat):
    u = catalogue_through_stage_2a(cat, blocking=True)
    for path in unit.catalogue_stage4_reports(u):
        dump(path, {})
    dump(u.r("blocker-view.json"), {"unit": u.id, "items": [
        view_answer("d1", value=15, unit_="year", basis=["[civil-code 308 ¶1]"]),
        view_answer("p1", consequence="onlyIfRaised", basis=["[civil-code 179 ¶1]"])]})
    assert unit.stage5_catalogue(u)
    rec = load(u.reconciled)
    assert rec["stage"] == "reconciled" and rec["verified"] is False and "catalogue route" in rec["derivedBy"]["role"]
    assert rec["deadlines"][0]["effectWhenExpired"] == "blockCleanExport"
    assert rec["priorSteps"][0]["effectWhenMissing"] == "warn" and rec["priorSteps"][0]["effectWhenUnknown"] == "ask"
    assert rec["examples"][0]["expect"]["findings"] == [{"ref": "d1", "effect": "blockCleanExport"},
                                                       {"ref": "p1", "effect": "warn"}]
    assert "onlyIfRaised" in rec["history"]["reconciliation"]
    assert load(u.deriv("a"))["priorSteps"][0]["effectWhenMissing"] == "blockCleanExport"  # A itself is untouched
    rows = {r["reconciled"]: r for r in load(u.r("idmap.json"))}
    assert rows["d1"]["b"] == "blocker-view" and not rows["d1"]["single"]
    assert rows["p1"]["b"] is None and rows["p1"]["single"] and rows["b1"]["single"]
    assert load(u.r("differences.json"))["agreementTotal"] == {"equal": 1, "compared": 2}
    check = load(u.r("blocker-check.json"))
    assert check["kept"] == ["d1"] and check["setToWarn"] == ["p1"]
    report = open(u.r("reconciliation.md"), encoding="utf-8").read()
    assert "blocker d1: keeps blockCleanExport" in report and "blocker p1: set to warn" in report
    s = dict(unit.status(u))
    assert s["5"] == unit.PROMOTED and s["6"] == "due"
    found = unit.blocking_items(u)  # read from the promoted record now
    assert "d1" in found and "p1" not in found


def through_stage_6(u):
    for path in (u.r("values.reconciled.json"), u.r("flags.reconciled.json"), u.reconciled_q,
                 os.path.join(u.cases, "boundaryReconciled.json")):
        dump(path, [])


def test_the_support_check_runs_when_a_block_survived_the_blind_reading(cat):
    u = catalogue_through_stage_2a(cat, blocking=True)
    dump(u.r("blocker-view.json"), {"unit": u.id, "items": [
        view_answer("d1", value=15, unit_="year", basis=["[civil-code 308 ¶1]"]),
        view_answer("p1", require="textsSilent", consequence="textsSilent")]})
    unit.promote(u)
    assert load(u.r("blocker-check.json"))["kept"] == ["d1"]
    assert dict(unit.status(u))["7c"] == "waiting for 6"
    through_stage_6(u)
    s = dict(unit.status(u))
    assert s["7c"] == "due (unit.py codex)" and s["7b"] == s["8b"] == unit.CATALOGUE
    dump(u.r("support.json"), {"attacks": []})
    assert dict(unit.status(u))["7c"] == "done"


def test_the_support_check_stays_skipped_when_no_block_survived(cat):
    u = catalogue_through_stage_2a(cat, blocking=True)
    dump(u.r("blocker-view.json"), {"unit": u.id, "items": [view_answer("d1", consequence="onlyIfRaised"),
                                                            view_answer("p1", consequence="noSanction")]})
    unit.promote(u)
    assert load(u.r("blocker-check.json"))["kept"] == []
    through_stage_6(u)
    assert dict(unit.status(u))["7c"] == unit.CATALOGUE


def test_promotion_without_a_would_be_blocking_item_needs_no_blind_reading(cat):
    u = catalogue_through_stage_2a(cat, blocking=False)
    unit.promote(u)
    rec = load(u.reconciled)
    assert rec["stage"] == "reconciled" and not unit.own_blocking(rec)
    assert all(r["single"] and r["b"] is None for r in load(u.r("idmap.json")))
    assert "No item of the derivation would block" in open(u.r("reconciliation.md"), encoding="utf-8").read()


def test_the_blocker_comparison_needs_requirement_inadmissibility_the_period_and_a_common_article():
    def compare(answer):
        return unit.blocker_compare(CAT_RECORD, QUESTIONS[:1], {"items": [answer]})[0]
    good = view_answer("d1", value=15, unit_="year", basis=["[civil-code 308 ¶1]", "[civil-code 309 ¶2]"])
    assert compare(good)["agreed"] and compare(good)["reasons"] == []
    assert "do not require" in compare(dict(good, textsRequire="textsSilent"))["reasons"][0]
    assert "«noSanction»" in compare(dict(good, consequence="noSanction"))["reasons"][0]
    assert "a period of 10 year, the record 15 year" in compare(dict(good, periodValue=10))["reasons"][0]
    assert "none of the articles the item rests on (civil-code 308)" in \
        compare(dict(good, basis=["[civil-code 310 ¶1]"]))["reasons"][0]
    missing = unit.blocker_compare(CAT_RECORD, QUESTIONS, {"items": [good]})[1]
    assert missing["item"] == "p1" and not missing["agreed"] and "no answer" in missing["reasons"][0]
    # a prior step has no period to compare; an article written with words after its number still matches
    rec = json.loads(json.dumps(CAT_RECORD))
    rec["basis"][1]["cite"]["article"] = "324 مكرر 5"
    step = unit.blocker_compare(rec, QUESTIONS[1:], {"items": [view_answer("p1", basis=["[civil-code 324 مكرر 5 ¶1]"])]})
    assert step[0]["agreed"]


def test_the_side_file_of_changes_must_name_every_catalogue_part_and_keep_its_questions_blind():
    entry = {"articles": [{"code": "civil-code", "number": "450"}], "suggestedRequests": ["إلزام المدعى عليه"],
             "requiredDocuments": ["سند القرض"], "factsToState": ["تاريخ القرض"]}
    changes = [{"part": "article", "catalogueSays": "civil-code 450: the loan", "what": "kept", "items": ["b1"],
                "reason": "[civil-code 450 ¶1]"},
               {"part": "request", "catalogueSays": "إلزام المدعى عليه", "what": "kept", "items": ["r1"], "reason": "r"},
               {"part": "document", "catalogueSays": "سند القرض", "what": "kept", "items": ["doc1"], "reason": "r"},
               {"part": "fact", "catalogueSays": "تاريخ القرض", "what": "corrected", "items": ["e1"], "reason": "r"},
               {"part": "deadline", "catalogueSays": "", "what": "added", "items": ["d1"], "reason": "r"}]
    doc = {"unit": "loan-repayment", "changes": changes, "wouldBlock": QUESTIONS}
    assert unit.catalogue_changes_problems(doc, CAT_RECORD, entry) == []
    assert unit.catalogue_changes_problems(dict(doc, changes=changes[1:]), CAT_RECORD, entry) == [
        "no change names the catalogue's «civil-code 450»"]
    assert unit.catalogue_changes_problems(dict(doc, wouldBlock=QUESTIONS[:1]), CAT_RECORD, entry) == [
        "wouldBlock lists ['d1'], the record's own blocking items are ['d1', 'p1']"]
    leaky = [{"item": "d1", "question": {"ar": "هل ترفع خلال خمس عشرة سنة؟", "en": "Within 15 years?"}}, QUESTIONS[1]]
    assert unit.catalogue_changes_problems(dict(doc, wouldBlock=leaky), CAT_RECORD, entry) == [
        "wouldBlock d1: the ar question holds the extract «خمس عشرة سنة»", "wouldBlock d1: the en question holds a digit"]
    assert unit.catalogue_changes_problems({"unit": "x", "changes": [{"part": "nonsense"}]}, CAT_RECORD, entry)


def test_done_sets_aside_a_blocker_view_that_skips_an_item(cat, tmp_path, monkeypatch):
    u = catalogue_through_stage_2a(cat, blocking=True)
    ledger = tmp_path / "ledger.jsonl"
    write_ledger(ledger, [{"id": "r-s1-0003", "kind": "agent", "unit": u.id, "stage": "2e", "status": "running",
                           "role": "rules-blocker-view"}])
    monkeypatch.setattr(unit, "tool", lambda: "rules.exe")
    calls = []

    def ingest(*args, **kw):  # the reply passes its schema but answers d1 only
        calls.append(args)
        dump(args[args.index("--out") + 1], {"unit": u.id, "items": [view_answer("d1")]})
        return 0, "ok"
    monkeypatch.setattr(unit, "py", ingest)
    assert unit.done(u, "2e", "agent-3", ledger=str(ledger)) == 1
    assert calls[0][:2] == ("records.py", "ingest-reply") and calls[0][calls[0].index("--schema") + 1].endswith(
        "blocker-view.schema.json")
    assert not os.path.isfile(u.r("blocker-view.json"))
    assert os.path.isfile(os.path.join(u.run, "discarded", "blocker-view-r-s1-0003.json"))
    row = unit.ledger_rows(str(ledger))["r-s1-0003"]
    assert row["status"] == "failed" and "no answer for p1" in row["reason"]
    assert dict(unit.status(u))["2e"] == "due"  # run again, fresh
