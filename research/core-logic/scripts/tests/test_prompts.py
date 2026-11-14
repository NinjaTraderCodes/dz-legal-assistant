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
                                  "rules-reconcile"])
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
