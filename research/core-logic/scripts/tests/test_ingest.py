"""records.py ingest-reply: an agent's answer copied from its transcript, never retyped (specs/unit-driver.md)."""
import json
import os
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import ingest  # noqa: E402

SCHEMA = {"type": "object", "additionalProperties": False, "required": ["attacks", "nothingFound"],
          "properties": {"attacks": {"type": "array", "items": {"type": "object", "required": ["target"],
                                                               "properties": {"target": {"type": "string"}}}},
                         "nothingFound": {"type": "boolean"}}}


def transcript(tmp_path, prompt, answer_text, extra_calls=()):
    rows = [{"type": "assistant", "message": {"content": [
        {"type": "tool_use", "name": "Read", "input": {"file_path": prompt}}]}}]
    for c in extra_calls:
        rows.append({"type": "assistant", "message": {"content": [{"type": "tool_use", **c}]}})
    rows.append({"type": "assistant", "message": {"content": [
        {"type": "tool_use", "name": "SubagentHandback", "input": {"message": answer_text}}]}})
    path = tmp_path / "agent-x.jsonl"
    path.write_text("\n".join(json.dumps(r) for r in rows) + "\n", encoding="utf-8")
    schema = tmp_path / "s.json"
    schema.write_text(json.dumps(SCHEMA), encoding="utf-8")
    return str(path), str(schema)


def test_a_clean_reply_is_written_with_ids_and_the_unit(tmp_path):
    prompt = str(tmp_path / "p.md")
    t, s = transcript(tmp_path, prompt, '```json\n{"attacks": [{"target": "d1"}, {"target": "p2"}], "nothingFound": false}\n```')
    out = tmp_path / "out.json"
    ingest.ingest("x", prompt, s, str(out), ids="atk", unit="periods", transcript=t)
    doc = json.loads(out.read_text(encoding="utf-8"))
    assert doc["unit"] == "periods" and [a["id"] for a in doc["attacks"]] == ["atk-1", "atk-2"]


def test_append_continues_the_numbering(tmp_path):
    prompt = str(tmp_path / "p.md")
    out = tmp_path / "out.json"
    out.write_text(json.dumps({"unit": "u", "attacks": [{"target": "a", "id": "atk-1"}], "nothingFound": False}),
                   encoding="utf-8")
    t, s = transcript(tmp_path, prompt, '{"attacks": [{"target": "b"}], "nothingFound": false}')
    ingest.ingest("x", prompt, s, str(out), ids="atk", unit="u", append=True, transcript=t)
    assert [a["id"] for a in json.loads(out.read_text(encoding="utf-8"))["attacks"]] == ["atk-1", "atk-2"]


def test_a_second_tool_call_discards_the_run(tmp_path):
    prompt = str(tmp_path / "p.md")
    t, s = transcript(tmp_path, prompt, '{"attacks": [], "nothingFound": true}',
                      extra_calls=[{"name": "Read", "input": {"file_path": str(tmp_path / "other.json")}}])
    with pytest.raises(ingest.Refused) as e:
        ingest.ingest("x", prompt, s, str(tmp_path / "out.json"), transcript=t)
    assert e.value.code == 3 and not (tmp_path / "out.json").exists()


def test_text_around_the_json_or_a_wrong_shape_is_refused(tmp_path):
    prompt = str(tmp_path / "p.md")
    for text in ('Here it is: {"attacks": [], "nothingFound": true}', '{"attacks": []}'):
        t, s = transcript(tmp_path, prompt, text)
        with pytest.raises(ingest.Refused) as e:
            ingest.ingest("x", prompt, s, str(tmp_path / "out.json"), transcript=t)
        assert e.value.code == 4


def test_the_transcript_is_found_in_the_newest_session_that_holds_it(tmp_path):
    for s in ("old", "new"):
        (tmp_path / s / "subagents").mkdir(parents=True)
    (tmp_path / "old" / "subagents" / "agent-a1.jsonl").write_text("", encoding="utf-8")
    assert ingest.transcript_path("a1", projects=str(tmp_path)).endswith(os.path.join("old", "subagents", "agent-a1.jsonl"))
    with pytest.raises(ingest.Refused):
        ingest.transcript_path("zz", projects=str(tmp_path))


def test_the_validator_accepts_a_required_key_that_may_be_null():
    import codex_run
    s = {"type": "object", "required": ["p"], "properties": {"p": {"type": ["integer", "null"]}}}
    assert codex_run.validate(s, {"p": None}) == [] and codex_run.validate(s, {"p": 2}) == []
    assert codex_run.validate(s, {"p": "2"}) and codex_run.validate(s, {})
