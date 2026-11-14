"""Stage 7 inputs and outputs: the record rendered for the Codex attack, the support pairs with their seeded false
pairs, and the split of the support answers (rules-attack-codex.md, rules-support-check-codex.md)."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.dirname(__file__))
import records as rc  # noqa: E402
import stage7  # noqa: E402
from test_records import STORE, record  # noqa: E402


def quoted():
    q, problems = rc.quote_record(record(priorSteps=[{
        "id": "p1", "kind": "formalNotice", "requiredWhen": {"slot": "requests", "includes": "damages"},
        "notRequiredWhen": [{"when": {"slot": "s1", "is": "yes"}, "basis": ["b2"], "note": {"en": "Two months passed."}}],
        "effectWhenMissing": "warn", "basis": ["b1"]}]), STORE)
    assert not problems
    return q


def test_items_cover_statements_deadlines_prior_steps_and_their_exemptions_with_their_texts():
    got = {i["item"]: i for i in stage7.items(quoted())}
    assert set(got) == {"b1", "b2", "d1", "p1", "p1.notRequiredWhen[1]"}
    assert "6 months" in got["d1"]["statement"] and "date of s1" in got["d1"]["statement"]
    assert got["p1"]["statement"].startswith("Before a claim in which the petition asks for damages")
    assert [t["article"] for t in got["p1.notRequiredWhen[1]"]["texts"]] == ["504"]


def test_render_shows_each_item_its_quote_and_an_uncited_exception_next_to_it():
    text = stage7.render(quoted(), STORE)
    assert "[cpca 504 ¶1] لا تقبل الدعوى" in text
    assert "[cpca 504 ¶2] (not cited) غير أنه" in text
    assert "- d1 (deadline)" in text and "- p1 (priorStep)" in text


def test_one_pair_in_five_is_seeded_and_never_marked_in_the_pairs_text():
    pairs, key = stage7.build_pairs([quoted()], seed=3)
    seeded = [p for p in key["pairs"] if p["seeded"]]
    assert len(seeded) == round(5 / 4) and len(key["pairs"]) == 5 + len(seeded)
    assert sorted(p["n"] for p in key["pairs"]) == list(range(1, len(pairs) + 1))
    text = stage7.render_pairs(pairs)
    assert "seeded" not in text.lower() and "wrongText" not in text


def test_a_mutation_changes_the_statement():
    import random
    kind, changed = stage7._mutate("The claim must be filed within 6 months.", random.Random(1))
    assert changed != "The claim must be filed within 6 months." and kind in ("changedNumber", "swappedWord")
    assert stage7._mutate("Nothing to change here.", random.Random(1)) is None


def key_of(seeded_ns, total):
    return {"pairs": [{"n": n, "unit": "u", "item": f"b{n}", "seeded": "wrongText" if n in seeded_ns else None,
                       "statement": "s", "texts": []} for n in range(1, total + 1)]}


def test_split_turns_no_and_partly_into_attacks_and_scores_the_seeded_pairs():
    key = key_of({5, 10}, 10)
    answers = {"answers": [{"n": n, "supports": "yes", "missingOrWrong": ""} for n in range(1, 11)]}
    answers["answers"][1]["supports"] = "no"       # pair 2, a real one
    answers["answers"][4]["supports"] = "no"       # pair 5, seeded: caught
    answers["answers"][9]["supports"] = "partly"   # pair 10, seeded: caught
    doc = stage7.split(answers, key)["u"]
    assert doc["seededAccepted"] == 0 and doc["yesTrusted"] is True
    assert [a["target"] for a in doc["attacks"]] == ["b2"] and len(doc["pairs"]) == 8


def test_more_than_one_seeded_pair_in_five_accepted_makes_every_yes_untrusted():
    key = key_of({5, 10}, 10)
    answers = {"answers": [{"n": n, "supports": "yes", "missingOrWrong": ""} for n in range(1, 11)]}
    doc = stage7.split(answers, key)["u"]
    assert doc["seededAccepted"] == 2 and doc["yesTrusted"] is False
    assert len(doc["attacks"]) == 8 and {a["kind"] for a in doc["attacks"]} == {"untrustedCall"}


def test_a_list_item_is_paired_with_its_lead_in_and_a_step_names_its_exemptions():
    q = quoted()
    q["basis"][1]["quote"] = [{"paragraph": 2, "text": "البند الثاني."}]
    q["basis"].append({"id": "b9", "role": "procedure", "cite": {"code": "cpca", "article": "504", "paragraphs": [1]},
                       "says": {"ar": "", "en": "Lead-in."}, "quote": [{"paragraph": 1, "text": "في الحالات الآتية :"}]})
    got = {i["item"]: i for i in stage7.items(q)}
    assert [t["paragraph"] for t in got["b2"]["texts"]] == [1, 2]
    assert got["p1"]["statement"].endswith("except when: Two months passed.")
