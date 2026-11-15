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
