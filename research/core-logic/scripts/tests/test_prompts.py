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
    shapes = header_shapes("rules-derive-c")
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


@pytest.mark.parametrize("name", ["rules-derive-a", "rules-derive-b", "rules-derive-c", "rules-boundary-cases",
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


@pytest.mark.parametrize("prompt,schema_file", [("rules-attack", "attacks.schema.json"),
                                                ("rules-support-check-codex", "support.schema.json"),
                                                ("boundary-second-view-codex", "boundary-view.schema.json")])
def test_codex_schema_matches_its_prompt_header(prompt, schema_file):
    with open(os.path.join(SCHEMAS, schema_file), encoding="utf-8") as f:
        same_shape(header_object_shape(prompt, schema_file), json.load(f))


@pytest.mark.parametrize("name,file", [("rules-attack", "record_text.txt"),
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


def schema_words(schema, keys: set, values: set) -> None:
    """Every property name and every string of an enum, at any depth."""
    for k, v in schema.get("properties", {}).items():
        keys.add(k)
        schema_words(v, keys, values)
    if isinstance(schema.get("items"), dict):
        schema_words(schema["items"], keys, values)
    values.update(v for v in schema.get("enum", []) if isinstance(v, str))


# Safeguard 33: an agent sees only the body. Three runs were lost on 14 November 2026 because a shape sat in a header.
BODY_SCHEMAS = [("rules-derive-c", "module-derivation-c.schema.json"), ("rules-attack", "attacks.schema.json"),
                ("rules-support-check-codex", "support.schema.json"),
                ("boundary-second-view-codex", "boundary-view.schema.json"), ("law-read-page", "page-reading.schema.json"),
                ("legal-review-attack", "legal-review-attack.schema.json"),
                ("notes-write", "notes-write.schema.json"), ("notes-judge", "notes-judge.schema.json"),
                ("v3-blind-relabel", "notes-judge.schema.json"), ("notes-third-judge", "third-judge.schema.json"),
                ("notes-filter-local", "filter-sheet.schema.json"), ("notes-noise-audit", "notes-judge.schema.json"),
                ("notes-guide-test", "guide-test.schema.json"), ("notes-realism-review", "realism.schema.json"),
                ("notes-clean-rank", "clean-rank.schema.json"), ("gen-judge", "gen-judge.schema.json"),
                ("notes-cells", "cells.schema.json"), ("arabic-check", "arabic-check.schema.json"),
                ("notes-guide-items", "guide-items.schema.json"), ("notes-sort-dropped", "sort-dropped.schema.json"),
                ("law-settle-difference", "settle-difference.schema.json"),
                ("rules-derive-catalogue", "catalogue-changes.schema.json"),
                ("rules-blocker-view", "blocker-view.schema.json")]


@pytest.mark.parametrize("prompt,schema_file", BODY_SCHEMAS)
def test_the_body_names_every_key_and_every_closed_value_of_its_schema(prompt, schema_file):
    with open(os.path.join(SCHEMAS, schema_file), encoding="utf-8") as f:
        schema = json.load(f)
    keys, values = set(), set()
    schema_words(schema, keys, values)
    _, body = prompts.template(prompt)
    missing = sorted(w for w in keys | values if f'"{w}"' not in body)
    assert not missing, f"{prompt}: the body does not name {missing}"


def test_every_schema_file_named_by_a_prompt_exists():
    for name in sorted(os.listdir(prompts.PROMPTS)):
        if name.endswith(".md"):
            with open(os.path.join(prompts.PROMPTS, name), encoding="utf-8") as f:
                for schema_file in set(re.findall(r"schemas[/\\]([\w-]+\.schema\.json)", f.read())):
                    assert os.path.isfile(os.path.join(SCHEMAS, schema_file)), (name, schema_file)


def test_no_prompt_names_a_prompt_file_that_does_not_exist():
    names = {n for n in os.listdir(prompts.PROMPTS) if n.endswith(".md")}
    for name in sorted(names):
        with open(os.path.join(prompts.PROMPTS, name), encoding="utf-8") as f:
            for link in set(re.findall(r"\]\(([\w-]+\.md)\)", f.read())):
                assert link in names, (name, link)


def test_the_relabel_prompts_share_the_relabellers_labelling_text_word_for_word():
    """The noise measurement and the hidden sets' relabeller must apply the guide exactly as the open sets' relabeller
    does: their bodies are notes-judge.md's, except where the answer goes."""
    _, judge = prompts.template("notes-judge")
    _, noise = prompts.template("notes-noise-audit")
    _, hidden = prompts.template("v3-blind-relabel")
    assert noise == judge
    start = judge.index("==== FOR EACH NOTE ====")
    labelling = judge[start:judge.index("Use no tool.")]
    shape = judge[judge.index('One entry of "notes" per note'):]
    assert labelling in hidden and shape.rstrip() in hidden
    assert "Use no tool." not in hidden and "{{labels_path}}" in hidden and '"relabelled"' in hidden


def test_the_adversary_gets_the_triage_leads_recorded_for_its_unit():
    assert prompts.leads_for("formal-notice") == "none"
    labour = prompts.leads_for("unpaid-wages")
    assert labour.startswith("- Official Journal issue ") and "free zone" in labour
    assert "Official Journal issue" in prompts.leads_for("ordinary-court-exits")


def test_a_follow_up_verifier_run_reads_the_last_candidate_and_writes_its_own_register(tmp_path, monkeypatch):
    """specs/unit-driver.md, "Follow-up verifier runs": never the reconciled record, never the first register."""
    values = prompts.values_for("rules-verify", "formal-notice", followup=2)
    assert "final-candidates" in values["record_paths"] and "quoted" in values["record_paths"]
    assert "reconciled" not in values["record_paths"]
    assert "formal-notice.attacks.followup2-input.json" in values["attack_paths"]
    assert "formal-notice.attacks.followup.json" in values["attack_paths"]  # an earlier register, to read only
    assert values["register_paths"].strip().endswith("formal-notice.attacks.followup2.json`")
    _, body = prompts.template("rules-verify")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values)


def test_seed_slots_are_hidden_by_id_and_a_final_modules_slots_are_shown(tmp_path, monkeypatch):
    """A slot that labour-prelitigation's final record adds has that module as its origin: the labour derivers see it."""
    doc = {"slots": [{"id": "wages_due_from", "type": "date", "origin": "unpaid-wages",
                      "question": {"ar": "س؟", "en": "q?"}},
                     {"id": "new_from_module", "type": "yesNoUnknown", "origin": "labour-prelitigation",
                      "question": {"ar": "س؟", "en": "q2?"}}]}
    cl = tmp_path / "cl"
    cl.mkdir()
    (cl / "slots.json").write_text(json.dumps(doc), encoding="utf-8")
    monkeypatch.setattr(prompts, "CL", str(cl))
    shown = prompts.slots("unpaid-wages")
    assert "new_from_module" in shown and "wages_due_from" not in shown
    assert "wages_due_from" in prompts.slots("loan-repayment")



# --- retool item 5: every step-1 prompt for a claim type (specs/unit-driver.md) --------------------------------------
MODULES = prompts.handbook_modules()


@pytest.fixture
def tmp_cl(tmp_path, monkeypatch):
    """A folder standing for <CL>: the real module table and slots, no record of any unit."""
    cl = tmp_path / "cl"
    for sub in ("rules/final", "rules/derivations/a", "rules/derivations/b", "rules/reports", "law"):
        (cl / sub).mkdir(parents=True)
    with open(os.path.join(prompts.CL, "slots.json"), encoding="utf-8") as f:
        (cl / "slots.json").write_text(f.read(), encoding="utf-8")
    monkeypatch.setattr(prompts, "CL", str(cl))
    monkeypatch.setattr(prompts, "handbook_modules", lambda: MODULES)
    return cl


def test_a_claim_type_is_briefed_from_the_catalogue_and_the_elements_table_without_the_catalogues_articles():
    values = prompts.values_for("rules-derive-a", "abusive-dismissal")
    brief = values["unit_brief"]
    assert values["unit_kind"] == "claim" and prompts.unit_kind("formal-notice") == "module"
    assert "`dismissal`" in brief and "`conciliation_failed`" in brief and "`reinstatement`" in brief
    _, entry = prompts.claims()["abusive-dismissal"]
    for hidden in [a["why"]["ar"] for a in entry["articles"]] + [x["ar"] for x in entry.get("prerequisites", [])]:
        assert hidden not in brief  # they become checklist questions, from: catalogue
    assert "wages_due_from" not in values["slots"]  # a labour claim type: the seed slots stay hidden


def test_every_placeholder_of_a_step_1_prompt_is_filled_for_a_claim_type(tmp_cl, monkeypatch):
    (tmp_cl / "law" / "french-sources.json").write_text(json.dumps({"units": {"loan-repayment": [
        {"what": "the Civil Code, articles on the loan", "paths": []}]}}), encoding="utf-8")
    monkeypatch.setattr(prompts, "FRENCH_SOURCES", str(tmp_cl / "law" / "french-sources.json"))
    monkeypatch.setattr(prompts, "unit_texts", lambda u: {"rendered": "[civil-code 450 ¶1] نص"})
    monkeypatch.setattr(prompts, "_run_file", lambda u, name: "the record as text")
    for name in ("rules-derive-a", "rules-derive-b", "rules-boundary-cases", "rules-reconcile", "rules-adversary",
                 "rules-attack", "rules-verify"):
        _, body = prompts.template(name)
        values = prompts.values_for(name, "loan-repayment")
        assert not set(prompts.PLACEHOLDER.findall(body)) - set(values), name
    assert "No French text was prepared for the Civil Code, articles on the loan: say so in your checklist." in \
        prompts.values_for("rules-derive-b", "loan-repayment")["french_sources"]
    assert prompts.values_for("rules-attack", "loan-repayment")["unit_label"].startswith("claim type loan-repayment: ")
    with pytest.raises(ValueError):
        prompts.values_for("rules-derive-c", "loan-repayment")
    with pytest.raises(KeyError):  # stage 0b has not run for it
        prompts.french_sources("unpaid-wages")


def test_module_finals_give_a_claim_every_final_module_and_a_unit_with_a_deadline_periods(tmp_cl):
    final, a = tmp_cl / "rules" / "final", tmp_cl / "rules" / "derivations" / "a"
    assert prompts.module_finals(["formal-notice"], strict=True) == "none"
    (a / "labour-prelitigation.json").write_text(json.dumps({"deadlines": [{"id": "d1"}]}), encoding="utf-8")
    with pytest.raises(ValueError):  # stage 5 waits for periods
        prompts.module_finals(["labour-prelitigation"], strict=True)
    assert prompts.module_finals(["labour-prelitigation"]) == "none"  # a verifier is not refused
    (final / "periods.json").write_text("{}", encoding="utf-8")
    assert prompts.module_finals(["labour-prelitigation"]).strip() == f"- `{final / 'periods.json'}`"
    (final / "formal-notice.json").write_text("{}", encoding="utf-8")
    (a / "loan-repayment.json").write_text(json.dumps({"imports": [{"module": "proof"}]}), encoding="utf-8")
    with pytest.raises(ValueError):  # it imports a module that is not final
        prompts.module_finals(["loan-repayment"], strict=True)
    listed = prompts.module_finals(["loan-repayment"])
    assert "formal-notice.json" in listed and "periods.json" in listed and "proof.json" not in listed
    assert prompts.values_for("rules-verify", "loan-repayment")["module_finals"] == listed


def test_the_verifier_reads_the_gathered_input_once_verify_input_has_written_it(tmp_cl):
    reports = tmp_cl / "rules" / "reports"
    assert "attacks.adversary.json" in prompts.values_for("rules-verify", "formal-notice")["attack_paths"]
    (reports / "formal-notice.attacks.input.json").write_text("{}", encoding="utf-8")
    paths = prompts.values_for("rules-verify", "formal-notice")["attack_paths"]
    assert "attacks.input.json" in paths and "adversary" not in paths


def test_the_outcome_meanings_are_those_of_the_second_views_body():
    _, body = prompts.template("boundary-second-view-codex")
    for word, meaning in prompts.OUTCOME_MEANINGS.items():
        assert f'"{word}": {meaning}' in body, word


def test_the_second_view_is_blind_to_the_expectation_and_offers_the_two_words_in_dispute(monkeypatch):
    """Stage 8b: the expected word and the record's `actual` word, alphabetical; never the case's `why`."""
    monkeypatch.setattr(prompts, "unit_texts", lambda u: {"rendered": "[civil-code 119 ¶1] في العقود الملزمة"})
    values = prompts.values_for("boundary-second-view-codex", "formal-notice", cases=["bc3"],
                                run=[{"caseId": "bc3", "actual": "ask"}])
    _, body = prompts.template("boundary-second-view-codex")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values)
    assert values["choices"].splitlines() == [f'- "ask": {prompts.OUTCOME_MEANINGS["ask"]}.',
                                              f'- "stepMissing": {prompts.OUTCOME_MEANINGS["stepMissing"]}.']
    with open(os.path.join(prompts.CL, "rules", "boundary-cases", "formal-notice.json"), encoding="utf-8") as f:
        bc3 = next(c for c in json.load(f) if c["id"] == "bc3")
    assert bc3["expect"]["why"] not in values["cases"] and "Nothing else is known about the case." in values["cases"]
    assert values["subject"].startswith("this subject: when a formal notice is required")
    assert "(Further paragraphs of the same texts:)" in values["texts"]  # the record cites more than 119 ¶1
    with pytest.raises(ValueError):  # the same word on both sides is not a choice
        prompts.values_for("boundary-second-view-codex", "formal-notice", cases=["bc3"],
                           run=[{"caseId": "bc3", "actual": "stepMissing"}])


# --- the catalogue route (specs/unit-driver.md, "The catalogue route for claim types") -------------------------------
def test_the_catalogue_deriver_gets_the_whole_catalogue_entry_its_check_and_the_texts(tmp_cl, tmp_path, monkeypatch):
    monkeypatch.setattr(prompts, "RUNS", str(tmp_path / "runs"))
    monkeypatch.setattr(prompts, "unit_texts", lambda u: {"rendered": "[civil-code 450 ¶1] نص"})
    with pytest.raises(FileNotFoundError):  # unit.py next writes the catalogue check before it fills this prompt
        prompts.values_for("rules-derive-catalogue", "loan-repayment")
    (tmp_cl / "rules" / "reports" / "loan-repayment.catalogue-check.json").write_text(
        '{"unit": "loan-repayment", "summary": {}}\n', encoding="utf-8")
    values = prompts.values_for("rules-derive-catalogue", "loan-repayment")
    _, body = prompts.template("rules-derive-catalogue")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values)
    filled = prompts.fill(body, values)
    _, entry = prompts.claims()["loan-repayment"]
    for line in [a["why"]["ar"] for a in entry["articles"]] + entry["suggestedRequests"] + entry["requiredDocuments"] \
            + entry["factsToState"]:
        assert line in filled  # unlike derivation A, the catalogue is given in full
    assert '{"unit": "loan-repayment", "summary": {}}' in filled
    with open(values["texts_path"], encoding="utf-8") as f:
        assert f.read() == "[civil-code 450 ¶1] نص\n"
    with pytest.raises(ValueError):
        prompts.values_for("rules-derive-catalogue", "formal-notice")


def test_the_blocker_view_gets_the_questions_and_the_texts_never_the_items(tmp_cl, monkeypatch):
    a = tmp_cl / "rules" / "derivations" / "a"
    record = {"basis": [{"id": "b1", "cite": {"code": "civil-code", "article": "308"}},
                        {"id": "b2", "cite": {"code": "civil-code", "article": "179"}}],
              "deadlines": [{"id": "d1", "period": {"value": 15, "unit": "year"}, "basis": ["b1"],
                             "effectWhenExpired": "blockCleanExport", "extract": {"period": "خمس عشرة سنة"},
                             "message": {"ar": "رسالة الأجل", "en": "the deadline's message"}}],
              "priorSteps": [{"id": "p1", "basis": ["b2"], "effectWhenMissing": "blockCleanExport",
                              "message": {"ar": "رسالة الإجراء", "en": "the step's message"}}]}
    questions = [{"item": "d1", "question": {"ar": "في أي أجل؟", "en": "Within what time must it be filed?"}},
                 {"item": "p1", "question": {"ar": "هل يجب الإعذار؟", "en": "Must notice be given first?"}}]
    (a / "loan-repayment.json").write_text(json.dumps(record), encoding="utf-8")
    (a / "loan-repayment.catalogue-changes.json").write_text(json.dumps({"wouldBlock": questions}), encoding="utf-8")
    monkeypatch.setattr(prompts, "unit_texts", lambda u: {"rendered": "[civil-code 179 ¶1] لا يستحق التعويض"})

    class Store:
        def get(self, code, article, version=None):
            return {"paragraphs": ["يتقادم الالتزام", "غير أنه"]} if (code, article) == ("civil-code", "308") else None
    monkeypatch.setattr(prompts, "blocker_store", Store)
    values = prompts.values_for("rules-blocker-view", "loan-repayment")
    _, body = prompts.template("rules-blocker-view")
    assert not set(prompts.PLACEHOLDER.findall(body)) - set(values)
    assert values["questions"].splitlines() == [
        "- **d1** (a time limit): Within what time must it be filed? «في أي أجل؟»",
        "- **p1** (a step before filing): Must notice be given first? «هل يجب الإعذار؟»"]
    # the cited article outside the unit's texts is added whole; 179 is already there
    assert values["texts"].endswith("(Further articles of the same codes:)\n[civil-code 308 ¶1] يتقادم الالتزام\n"
                                    "[civil-code 308 ¶2] غير أنه")
    filled = prompts.fill(body, values)
    for hidden in ("خمس عشرة سنة", "the deadline's message", "رسالة الإجراء", "blockCleanExport", "15"):
        assert hidden not in filled
    (a / "loan-repayment.catalogue-changes.json").write_text(json.dumps({"wouldBlock": []}), encoding="utf-8")
    with pytest.raises(ValueError):  # nothing would block: stage 2e is skipped, never launched
        prompts.values_for("rules-blocker-view", "loan-repayment")


def test_every_prompt_that_names_the_keys_removes_them_by_the_exact_unset_only():
    # The owner, 18 November 2026: no `env -u` (it runs any program after it, or prints the whole environment); only
    # the exact unset of the two keys, which the agent guard allows alone.
    exact = "`unset OPENROUTER_API_KEY GOOGLE_API_KEY`, alone"
    named = 0
    for name in sorted(os.listdir(prompts.PROMPTS)):
        if not name.endswith(".md"):
            continue
        with open(os.path.join(prompts.PROMPTS, name), encoding="utf-8") as f:
            text = f.read()
        if "OPENROUTER_API_KEY" not in text:
            continue
        named += 1
        assert exact in text, name
        assert "env -u" not in text and "Remove-Item Env:" not in text and "remove `OPENROUTER_API_KEY`" not in text, name
    assert named >= 8
