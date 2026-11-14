"""The prompt filler, the store's text views, and the Codex output schemas against their prompts' headers (handbook
§11.5: a schema is written from its prompt's header, with a test that the two agree)."""
import json
import os
import re
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import prompts  # noqa: E402
import store_text  # noqa: E402

SCHEMAS = os.path.join(prompts.PROMPTS, "schemas")


def header_shapes(prompt: str) -> dict[str, dict[str, list[str] | None]]:
    """From a prompt header's bullets «`name`: `[{ "a", "b": "x" | "y", "c": [...] }]`»: each top-level array's fields,
    with the enum values where the header lists them (None for an open value)."""
    head, _ = prompts.template(prompt)
    shapes = {}
    for name, body in re.findall(r"`(\w+)`: `\[\{(.*?)\}\]`", head):
        body = re.sub(r"\[\{.*?\}\]", "[]", body)  # a nested list of objects: its fields belong to it, not here
        fields, current = {}, None
        for m in re.finditer(r'([:|,{]?)\s*"(\w+)"', body):
            before, word = m.group(1), m.group(2)
            if before in (":", "|") and current is not None:
                fields[current] = (fields[current] or []) + [word]
            else:
                current = word
                fields[word] = None
        shapes[name] = fields
    return shapes


def test_derivation_c_schema_matches_the_prompt_header():
    with open(os.path.join(SCHEMAS, "module-derivation-c.schema.json"), encoding="utf-8") as f:
        schema = json.load(f)
    shapes = header_shapes("rules-derive-c-codex")
    assert set(shapes) == set(schema["properties"]) == set(schema["required"])
    for name, fields in shapes.items():
        item = schema["properties"][name]["items"]
        assert item["additionalProperties"] is False
        assert set(fields) == set(item["properties"]) == set(item["required"]), name
        for field, values in fields.items():
            assert item["properties"][field].get("enum") == values, (name, field)


def test_fill_refuses_a_placeholder_left_empty():
    with pytest.raises(KeyError):
        prompts.fill("a {{one}} and {{two}}", {"one": "1"})
    assert prompts.fill("a {{one}}", {"one": "1"}) == "a 1"


@pytest.mark.parametrize("name", ["rules-derive-a", "rules-derive-b", "rules-derive-c-codex", "rules-boundary-cases",
                                  "rules-reconcile", "rules-adversary", "rules-verify"])
def test_every_placeholder_of_a_step_1_prompt_has_a_filling_rule(name):
    _, body = prompts.template(name)
    values = prompts.values_for(name, "formal-notice")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values)


def test_the_module_table_has_twelve_rows():
    rows = prompts.handbook_modules()
    assert len(rows) == 12 and rows[4]["id"] == "formal-notice"


def test_slots_leave_out_help_and_the_seed_slots_of_the_labour_units():
    all_slots = prompts.slots("formal-notice")
    labour = prompts.slots("labour-prelitigation")
    assert "`worker_is_public_servant`" in all_slots and "`worker_is_public_servant`" not in labour
    assert "`wages_due_from`" not in prompts.slots("unpaid-wages")
    assert "الإعذار: إنذار رسمي" not in all_slots  # the help text of notice_served


def test_vocabulary_has_the_event_and_request_kinds():
    v = prompts.vocabulary()
    assert "`notice_served`" in v and "`provisional_execution`" in v


def test_text_view_labels_paragraphs_as_a_cite_counts_them():
    entries = [{"code": "x", "article": "2", "version": "v1", "inForce": True, "repealed": False, "path": ["A"],
                "paragraphs": ["p1", "p2"]},
               {"code": "x", "article": "1", "version": "v1", "inForce": True, "repealed": False, "path": [],
                "paragraphs": ["q"]},
               {"code": "x", "article": "3", "version": "v2", "inForce": False, "repealed": True, "path": ["A"],
                "paragraphs": [], "inForceNote": {"lead": "a note"}}]
    text = store_text.render("x", entries, {"1": ["E"]})
    lines = text.split("\n")
    assert lines.index("[x 1 ¶1] q") < lines.index("[x 2 ¶1] p1") < lines.index("[x 2 ¶2] p2")
    assert "## E [headings from the official consolidated edition]" in lines and "## A" in lines
    assert "### x 3 (repealed; version v2; note: a note)" in lines


def header_object_shape(prompt: str, schema_file: str):
    """A header's «`schemas/x.schema.json`: `{ … }`» as a JSON shape: string/boolean/integer as type names, «"a" | "b"»
    as a list of the values."""
    head, _ = prompts.template(prompt)
    m = re.search(r"`schemas/" + re.escape(schema_file) + r"`: `(\{.*?\})`", head)
    text = re.sub(r'"[^"]+"(?:\s*\|\s*"[^"]+")+', lambda x: json.dumps(re.findall(r'"([^"]+)"', x.group())), m.group(1))
    text = re.sub(r"\b(string|boolean|integer)\b", r'"\1"', text)
    return json.loads(text)


def same_shape(shape, schema) -> None:
    if isinstance(shape, dict):
        assert schema["type"] == "object" and schema["additionalProperties"] is False
        assert set(shape) == set(schema["properties"]) == set(schema["required"])
        for k, v in shape.items():
            same_shape(v, schema["properties"][k])
    elif isinstance(shape, list) and shape and all(isinstance(x, str) for x in shape) and shape[0] not in (
            "string", "boolean", "integer"):
        assert schema.get("enum") == shape
    elif isinstance(shape, list):
        assert schema["type"] == "array"
        same_shape(shape[0], schema["items"])
    else:
        assert schema["type"] == shape and "enum" not in schema


@pytest.mark.parametrize("prompt,schema_file", [("rules-attack-codex", "attacks.schema.json"),
                                                ("rules-support-check-codex", "support.schema.json")])
def test_codex_schema_matches_its_prompt_header(prompt, schema_file):
    with open(os.path.join(SCHEMAS, schema_file), encoding="utf-8") as f:
        same_shape(header_object_shape(prompt, schema_file), json.load(f))


@pytest.mark.parametrize("name,file", [("rules-attack-codex", "record_text.txt"),
                                       ("rules-support-check-codex", "support-pairs.txt")])
def test_codex_stage_7_prompts_take_their_text_from_the_stage_7_files(name, file, tmp_path, monkeypatch):
    monkeypatch.setattr(prompts, "RUNS", str(tmp_path))
    with pytest.raises(FileNotFoundError):
        prompts.values_for(name, "formal-notice")
    (tmp_path / "formal-notice").mkdir()
    (tmp_path / "formal-notice" / file).write_text("the text" + chr(10), encoding="utf-8")
    _, body = prompts.template(name)
    values = prompts.values_for(name, "formal-notice")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values) and "the text" in values.values()
