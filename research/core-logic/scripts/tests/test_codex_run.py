import json
import os
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import codex_run as cr  # noqa: E402

SCHEMA = {"type": "object", "additionalProperties": False, "required": ["word"], "properties": {"word": {"type": "string"}}}
PROBES = r"C:\Users\TheShadow\dz-core-research\codex-probes"


def ev(*items, completed=True):
    events = [{"type": "thread.started"}, {"type": "turn.started"}]
    events += [{"type": "item.completed", "item": {"id": f"i{n}", **it}} for n, it in enumerate(items)]
    if completed:
        events.append({"type": "turn.completed", "usage": {"input_tokens": 1, "output_tokens": 1}})
    return events


def test_an_answer_alone_is_accepted():
    r = cr.check(ev({"type": "reasoning", "text": "…"}, {"type": "agent_message", "text": '{"word": "ready"}'}), 0, "", '{"word": "ready"}', SCHEMA)
    assert r["status"] == "accepted" and r["answer"] == {"word": "ready"}


@pytest.mark.parametrize("kind", ["command_execution", "file_change", "web_search", "mcp_tool_call", "something_new"])
def test_any_other_item_rejects_the_whole_call_for_tool_use(kind):
    r = cr.check(ev({"type": kind}, {"type": "agent_message", "text": '{"word": "x"}'}), 0, "", '{"word": "x"}', SCHEMA)
    assert r["status"] == "rejected" and r["reason"] == "toolUse" and r["answer"] is None


def test_a_started_but_unfinished_command_also_rejects():
    events = ev({"type": "agent_message", "text": '{"word": "x"}'})
    events.insert(2, {"type": "item.started", "item": {"id": "c", "type": "command_execution"}})
    assert cr.check(events, 0, "", '{"word": "x"}', SCHEMA)["reason"] == "toolUse"


def test_other_rejections():
    ok = {"type": "agent_message", "text": '{"word": "x"}'}
    assert cr.check(ev(ok), 1, "boom", '{"word": "x"}', SCHEMA)["reason"] == "exitCode"
    assert cr.check(ev(ok, completed=False), 0, "", '{"word": "x"}', SCHEMA)["reason"] == "noTurnCompleted"
    assert cr.check(ev(ok), 0, "", '{"word": "y"}', SCHEMA)["reason"] == "outputMismatch"
    assert cr.check(ev({"type": "agent_message", "text": '{"other": 1}'}), 0, "", None, SCHEMA)["reason"] == "schema"
    assert cr.check(ev({"type": "agent_message", "text": "plain"}), 0, "", None, SCHEMA)["reason"] == "notJson"


def test_a_usage_limit_is_a_refusal_not_a_rejection():
    events = ev(completed=False) + [{"type": "turn.failed", "error": {"message": "You've hit your usage limit"}}]
    r = cr.check(events, 1, "", None, SCHEMA)
    assert r["status"] == "refused" and r["reason"] == "usageLimit"
    assert cr.check(ev(completed=False), 1, "HTTP 429 Too Many Requests", None, SCHEMA)["status"] == "refused"


def test_the_batch_stops_on_a_refusal_and_after_two_failures_in_a_row():
    b = cr.Batch()
    b.record({"status": "accepted"}); b.record({"status": "rejected", "reason": "schema"}); b.record({"status": "accepted"})
    assert not b.stopped
    b.record({"status": "rejected", "reason": "schema"}); b.record({"status": "rejected", "reason": "exitCode"})
    assert b.stopped and "two failed calls" in b.why
    c = cr.Batch(); c.record({"status": "refused", "reason": "usageLimit"})
    assert c.stopped and c.why == "usage limit"


@pytest.mark.skipif(not os.path.isdir(PROBES), reason="needs the stored probes of 12 November")
@pytest.mark.parametrize("name,expected", [("p1", "accepted"), ("p2", "rejected"), ("p4", None)])
def test_the_stored_probe_logs(name, expected):
    with open(os.path.join(PROBES, f"{name}.jsonl"), encoding="utf-8", errors="replace") as f:
        events = cr.parse_events(f.read())
    r = cr.check(events, 0, "", None, None)
    if name == "p4":  # a page reading with -i: only an agent message, so it passes the rule
        assert r["reason"] != "toolUse"
    else:
        assert r["status"] == expected
        if expected == "rejected":
            assert r["reason"] == "toolUse"


def test_the_validator_subset():
    schema = {"type": "object", "required": ["a"], "additionalProperties": False,
              "properties": {"a": {"type": "array", "items": {"type": "integer"}}, "b": {"enum": ["x"]}}}
    assert cr.validate(schema, {"a": [1, 2]}) == []
    assert cr.validate(schema, {"a": [1, True]})
    assert cr.validate(schema, {"a": [], "c": 1})
    assert cr.validate(schema, {"a": [], "b": "y"})


def test_the_gate_needs_the_pinned_version_and_todays_rejected_probe(tmp_path, monkeypatch):
    gate = tmp_path / "gate.json"
    monkeypatch.setattr(cr, "GATE", str(gate))
    monkeypatch.setattr(cr, "version", lambda: "codex-cli 0.167.0")
    with pytest.raises(cr.Gate):
        cr.gate_open("2026-11-14")  # no probe recorded
    gate.write_text(json.dumps({"date": "2026-11-14", "version": "codex-cli 0.167.0", "probe2Rejected": True}))
    cr.gate_open("2026-11-14")
    with pytest.raises(cr.Gate):
        cr.gate_open("2026-11-15")  # yesterday's probe does not count
    gate.write_text(json.dumps({"date": "2026-11-14", "version": "codex-cli 0.167.0", "probe2Rejected": False}))
    with pytest.raises(cr.Gate):
        cr.gate_open("2026-11-14")  # the probe was not rejected
    gate.write_text(json.dumps({"date": "2026-11-14", "version": "codex-cli 0.167.0", "probe2Rejected": True}))
    monkeypatch.setattr(cr, "version", lambda: "codex-cli 0.168.0")
    with pytest.raises(cr.Gate):
        cr.gate_open("2026-11-14")  # Codex updated itself


def test_an_error_event_discards_the_call_even_when_the_turn_completes():
    ok = {"type": "agent_message", "text": '{"word": "x"}'}
    events = ev(ok)
    events.insert(2, {"type": "error", "message": "failed to connect to Responses WebSocket"})
    r = cr.check(events, 0, "", '{"word": "x"}', SCHEMA)
    assert r["status"] == "rejected" and r["reason"] == "errorEvent" and r["answer"] is None
