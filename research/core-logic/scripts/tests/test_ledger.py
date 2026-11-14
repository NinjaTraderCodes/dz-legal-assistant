import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import ledger  # noqa: E402

SCHEMA = os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", "docs", "research", "core-logic", "law",
                      "amendment-ledger.schema.json")


def test_canonical_numbers():
    assert ledger.canonical("32 alinéas 7-10") == ("32", "alinéas 7-10")
    assert ledger.canonical("536 مكرر 4") == ("536 مكرر 4", "")
    assert ledger.canonical("٤٦٧ مكرر") == ("467 مكرر", "")
    assert ledger.text_id("law", "05-10") == "law-05-10"


def test_text_as_of_stops_before_the_first_unread_issue():
    issues = [{"year": 2005, "issue": 1, "issueDate": "2005-01-05", "readable": True},
              {"year": 2005, "issue": 2, "issueDate": "2005-01-09", "readable": False},
              {"year": 2005, "issue": 3, "issueDate": "2005-01-12", "readable": True}]
    assert ledger.text_as_of(issues, {}) == "2005-01-05"
    assert ledger.text_as_of(issues, {(2005, 2): {"issueDate": "2005-01-09"}}) == "2005-01-12"
    issues[2]["error"] = "Failed"
    assert ledger.text_as_of(issues, {(2005, 2): {}}) == "2005-01-09"


def test_a_date_read_by_the_triage_counts():
    issues = [{"year": 2006, "issue": 58, "issueDate": None, "readable": True}]
    assert ledger.text_as_of(issues, {}) is None
    assert ledger.text_as_of(issues, {(2006, 58): {"issueDate": "2006-09-20"}}) == "2006-09-20"


def _check(node, schema, defs, path="$"):
    """A small validator for the keywords the project's schemas use (type, properties, required,
    additionalProperties false, items, enum, $ref)."""
    if "$ref" in schema:
        return _check(node, defs[schema["$ref"].split("/")[-1]], defs, path)
    types = {"object": dict, "array": list, "string": str, "integer": int, "boolean": bool, "null": type(None)}
    allowed = schema.get("type")
    if allowed:
        allowed = allowed if isinstance(allowed, list) else [allowed]
        assert any(isinstance(node, types[t]) and not (t == "integer" and isinstance(node, bool)) for t in allowed), path
    if "enum" in schema:
        assert node in schema["enum"], path
    if isinstance(node, dict):
        for k in schema.get("required", []):
            assert k in node, f"{path}.{k} missing"
        props = schema.get("properties", {})
        if schema.get("additionalProperties") is False:
            assert set(node) <= set(props), f"{path}: {set(node) - set(props)}"
        for k, v in node.items():
            if k in props:
                _check(v, props[k], defs, f"{path}.{k}")
            elif isinstance(schema.get("additionalProperties"), dict):
                _check(v, schema["additionalProperties"], defs, f"{path}.{k}")
    if isinstance(node, list) and "items" in schema:
        for i, v in enumerate(node):
            _check(v, schema["items"], defs, f"{path}[{i}]")


def test_the_ledger_file_matches_its_schema():
    with open(SCHEMA, encoding="utf-8") as f:
        schema = json.load(f)
    if not os.path.exists(ledger.LEDGER):
        return
    with open(ledger.LEDGER, encoding="utf-8") as f:
        doc = json.load(f)
    _check(doc, schema, schema.get("$defs", {}))
    for code, entries in doc["instruments"].items():
        dates = [e["date"] or "" for e in entries]
        assert dates == sorted(dates), code  # the store takes the last touching entry as the latest
