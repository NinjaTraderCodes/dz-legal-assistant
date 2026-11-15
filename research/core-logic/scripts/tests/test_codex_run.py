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


# --- retool item 12: the background queue and the writer pool (specs/codex-calls.md) ---------------------------------
import threading  # noqa: E402


def fake_result(status, reason=None):
    return {"status": status, "reason": reason, "answer": {"ok": True} if status == cr.ACCEPTED else None,
            "usage": None}


def queue_with(tmp_path, n, monkeypatch):
    monkeypatch.setattr(cr, "gate_open", lambda today=None: None)
    q = cr.Queue(str(tmp_path / "queue"))
    for i in range(n):
        prompt = tmp_path / f"p{i}.md"
        prompt.write_text("prompt", encoding="utf-8")
        q.add("rules-support-check-codex", str(prompt), "schema.json", str(tmp_path / f"out{i}.json"), item=f"u{i}")
    return q


def test_two_calls_at_once_never_share_a_scratch_folder_and_each_starts_empty(tmp_path, monkeypatch):
    monkeypatch.setattr(cr, "SCRATCH", str(tmp_path / "scratch"))
    monkeypatch.setattr(cr, "LOGS", str(tmp_path / "logs"))
    monkeypatch.setattr(cr, "gate_open", lambda today=None: None)
    schema = tmp_path / "schema.json"
    schema.write_text('{"type": "object"}', encoding="utf-8")
    seen, barrier = [], threading.Barrier(2)

    class Done:
        returncode, stdout, stderr = 0, b"", b""

    def fake_run(cmd, cwd=None, **kw):
        seen.append((cwd, os.listdir(cwd)))
        barrier.wait(timeout=10)  # both calls are inside their folders at the same time
        return Done()

    monkeypatch.setattr(cr.subprocess, "run", fake_run)
    threads = [threading.Thread(target=cr.call, args=("p", "write", "b", f"c{i}", str(schema))) for i in (1, 2)]
    for th in threads:
        th.start()
    for th in threads:
        th.join()
    assert len({cwd for cwd, _ in seen}) == 2 and all(files == [] for _, files in seen)
    assert not any(os.path.exists(cwd) for cwd, _ in seen)  # removed after the call


def test_a_refusal_stops_the_queue_and_leaves_the_refused_call_waiting(tmp_path, monkeypatch):
    q = queue_with(tmp_path, 2, monkeypatch)
    calls = []
    out = q.run(call_fn=lambda *a, **k: calls.append(a[3]) or fake_result(cr.REFUSED, "usageLimit"))
    assert out == "stopped: usage limit" and calls == ["q-0001"]
    assert [r["state"] for r in q.read()] == ["waiting", "waiting"]
    assert not os.path.exists(q.lock)


def test_the_queue_runs_in_order_writes_the_answer_and_stops_after_two_failures(tmp_path, monkeypatch):
    q = queue_with(tmp_path, 3, monkeypatch)
    q.set("q-0001", state="waiting")
    results = iter([fake_result(cr.ACCEPTED), fake_result(cr.REJECTED, "toolUse"), fake_result(cr.REJECTED, "other")])
    assert q.run(call_fn=lambda *a, **k: next(results)).startswith("stopped: two failed calls in a row")
    assert [r["state"] for r in q.read()] == ["accepted", "rejected", "rejected"]
    assert json.load(open(tmp_path / "out0.json", encoding="utf-8")) == {"ok": True}
    assert not os.path.exists(tmp_path / "out1.json")  # nothing from a discarded call


def test_a_rejected_item_is_queued_once_more_and_never_a_third_time(tmp_path, monkeypatch):
    q = queue_with(tmp_path, 1, monkeypatch)
    with pytest.raises(RuntimeError):  # still waiting
        q.add("r", "p", "s", "o", item="u0")
    q.set("q-0001", state="rejected")
    q.add("r", "p", "s", "o", item="u0")
    q.set("q-0002", state="rejected")
    with pytest.raises(RuntimeError):
        q.add("r", "p", "s", "o", item="u0")


def test_the_gate_refuses_every_call_of_the_queue_and_the_pool(tmp_path, monkeypatch):
    q = queue_with(tmp_path, 1, monkeypatch)

    def closed(today=None):
        raise cr.Gate("no read probe today")

    monkeypatch.setattr(cr, "gate_open", closed)
    calls = []
    assert q.run(call_fn=lambda *a, **k: calls.append(1)).startswith("gate:")
    calls_file = pool_calls(tmp_path, 4)
    out = cr.pool(str(calls_file), 2, call_fn=lambda *a, **k: calls.append(1), log_dir=str(tmp_path / "pool"))
    assert calls == [] and out["stop"].startswith("gate:") and out["left"] == 4
    assert [r["state"] for r in q.read()] == ["waiting"]


def pool_calls(tmp_path, n):
    path = tmp_path / "calls.jsonl"
    prompt = tmp_path / "write.md"
    prompt.write_text("write", encoding="utf-8")
    with open(path, "w", encoding="utf-8") as f:
        for i in range(n):
            f.write(json.dumps({"call": f"w{i}", "role": "write", "set": "train", "prompt": str(prompt),
                                "schema": "s.json", "out": str(tmp_path / "notes" / f"w{i}.json"),
                                "scenarios": [f"s-{i}"]}) + "\n")
    return path


def test_two_rejections_in_a_row_stop_one_writer_and_not_the_others(tmp_path, monkeypatch):
    monkeypatch.setattr(cr, "gate_open", lambda today=None: None)
    calls_file = pool_calls(tmp_path, 8)

    def fake(prompt, role, batch, call_id, schema, **kw):
        if threading.current_thread().name == "writer-1":
            return fake_result(cr.REJECTED, "toolUse")
        return fake_result(cr.ACCEPTED)

    out = cr.pool(str(calls_file), 2, call_fn=fake, log_dir=str(tmp_path / "pool"))
    assert out["writersStopped"] == [1] and out["left"] == 0 and out["stop"] is None
    log = [json.loads(x) for name in os.listdir(tmp_path / "pool") if name.endswith(".jsonl")
           for x in open(tmp_path / "pool" / name, encoding="utf-8")]
    assert sum(r["result"] == "rejectedToolUse" for r in log) == 2 and len(log) == 8
    again = cr.pool(str(calls_file), 2, call_fn=fake, log_dir=str(tmp_path / "pool"))
    assert again["left"] == 0  # a call in the pool's log is never run again: a retry is a new call of the notes tool


def test_a_refusal_stops_the_pool_and_keeps_the_refused_call_for_the_next_run(tmp_path, monkeypatch):
    monkeypatch.setattr(cr, "gate_open", lambda today=None: None)
    calls_file = pool_calls(tmp_path, 3)
    out = cr.pool(str(calls_file), 2, call_fn=lambda *a, **k: fake_result(cr.REFUSED, "usageLimit"),
                  log_dir=str(tmp_path / "pool"))
    assert out["stop"] == "usage limit"
    logs = [n for n in os.listdir(tmp_path / "pool") if n.endswith(".jsonl")]
    assert all(os.path.getsize(tmp_path / "pool" / n) == 0 for n in logs)  # no refused call is logged as done
