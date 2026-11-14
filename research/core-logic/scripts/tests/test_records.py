"""The planted fixtures of record-checks.md: a changed digit, a repealed article, an uncited «غير أنه» paragraph, a
period that the text does not say, a limitation without its word; and the shape of each script's output."""
import os
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import records as rc  # noqa: E402


class FakeStore:
    """The few lookups the scripts use, over a handful of fictitious-free entries (texts are short real-shaped lines)."""

    def __init__(self, entries):
        self.by_key = {(e["code"], e["article"], e["version"]): e for e in entries}
        self.in_force = {(e["code"], e["article"]): e for e in entries if e["inForce"]}

    def get(self, code, article, version=None):
        if version:
            return self.by_key.get((code, article, version))
        return self.in_force.get((code, article)) or next(
            (e for (c, a, _), e in self.by_key.items() if (c, a) == (code, article)), None)


def entry(article, paragraphs, verified=True, by="ojPageReread", in_force=True, repealed=False, french=None,
          clean=False, reason="scanNotRun", code="cpca"):
    return {"code": code, "article": article, "version": "law-08-09", "inForce": in_force, "repealed": repealed,
            "paragraphs": paragraphs, "tier": 1, "store": "library", "verified": verified, "verifiedBy": by,
            "frenchCheck": french, "ledgerClean": clean, "ledgerReason": reason, "ledgerRefs": [], "textAsOf": None}


STORE = FakeStore([
    entry("503", ["يرفع الطلب خلال ستة (6) أشهر من تاريخ التبليغ، تحت طائلة سقوط الحق."]),
    entry("504", ["لا تقبل الدعوى بعد شهرين من تاريخ الرفض.", "غير أنه يجوز تمديد هذا الأجل بأمر من القاضي."]),
    entry("505", ["يتقادم الحق بمرور خمس عشرة سنة."]),
    entry("506", ["مع مراعاة أحكام المادة 505، تطبق القواعد العامة."]),
    entry("900", ["نص ملغى."], in_force=False, repealed=True),
    entry("19", ["نص يكفي لقراءة الصفحة."], by="corpusChecked"),
])


def record(**over):
    r = {"id": "m-test", "kind": "module", "basis": [
        {"id": "b1", "role": "procedure", "cite": {"code": "cpca", "article": "503"},
         "says": {"ar": "أجل ستة أشهر.", "en": "Six months."}},
        {"id": "b2", "role": "procedure", "cite": {"code": "cpca", "article": "504", "paragraphs": [1]},
         "says": {"ar": "شهران.", "en": "Two months."}}],
         "deadlines": [{"id": "d1", "kind": "forfeiture", "period": {"value": 6, "unit": "month"},
                        "startsFrom": {"slot": "s1", "event": "e1"}, "raisedBy": "unclear",
                        "counting": {"method": "excludeFirstDay", "extendToWorkingDay": True, "basis": ["b1"]},
                        "effectWhenExpired": "blockCleanExport", "basis": ["b1"],
                        "extract": {"period": "ستة (6) أشهر", "sanction": "تحت طائلة سقوط الحق"},
                        "message": {"ar": "أجل ستة (6) أشهر (المادة 503).", "en": "Six months (art. 503)."}}],
         "slots": ["s1"], "examples": [{"id": "ex1", "by": "derivationA", "title": "t", "petitionDate": "2026-01-01",
                                        "slots": {}, "expect": {"findings": [{"ref": "d1", "effect": "blockCleanExport"}]}}]}
    r.update(over)
    return r


@pytest.mark.parametrize("text,value,unit", [
    ("ستة (6) أشهر", 6, "month"), ("خمس عشرة سنة", 15, "year"), ("بخمس سنوات", 5, "year"), ("شهرين", 2, "month"),
    ("ثلاثين يوما", 30, "day"), ("خلال واحد وعشرين يوما", 21, "day"), ("ثمانية (8) أيام", 8, "day"),
    ("مدة سنة", 1, "year"), ("لمدة ثلاث سنوات", 3, "year")])
def test_period_reader(text, value, unit):
    got = rc.read_periods(text)[0]
    assert (got["value"], got["unit"]) == (value, unit) and not got["conflict"]


def test_period_reader_catches_words_and_digits_that_differ():
    assert rc.read_periods("ستة (8) أشهر")[0]["conflict"]


def test_running_text_is_not_a_period_unless_numbered():
    assert rc.read_periods("ابتداء من يوم الطلب", bare=False) == []


def test_quote_writes_quote_and_source_and_fails_a_repealed_or_missing_article():
    q, problems = rc.quote_record(record(), STORE)
    assert not problems
    b2 = q["basis"][1]
    assert [p["paragraph"] for p in b2["quote"]] == [1] and b2["source"] == {"tier": 1, "store": "library",
                                                                              "ledgerRefs": [], "ledgerClean": False}
    bad = record(basis=[{"id": "b9", "role": "basis", "cite": {"code": "cpca", "article": "900"}, "says": {"ar": "", "en": ""}},
                        {"id": "b8", "role": "basis", "cite": {"code": "cpca", "article": "777"}, "says": {"ar": "", "en": ""}},
                        {"id": "b7", "role": "basis", "cite": {"code": "cpca", "article": "504", "paragraphs": [3]},
                         "says": {"ar": "", "en": ""}}])
    _, problems = rc.quote_record(bad, STORE)
    assert sorted(p["problem"] for p in problems) == ["noSuchParagraph", "notInStore", "repealed"]


def test_check_values_catches_a_changed_digit_and_a_period_the_text_does_not_say():
    q, _ = rc.quote_record(record(), STORE)
    assert rc.check_values(q)["problems"] == []
    planted = record()
    planted["deadlines"][0]["period"] = {"value": 3, "unit": "month"}
    q, _ = rc.quote_record(planted, STORE)
    assert "valueMismatch" in {p["problem"] for p in rc.check_values(q)["problems"]}
    planted = record()
    planted["basis"][0]["says"] = {"ar": "أجل عشرة أيام.", "en": "Ten days."}
    q, _ = rc.quote_record(planted, STORE)
    assert "numberNotInQuote" in {p["problem"] for p in rc.check_values(q)["problems"]}


def test_check_values_wants_the_word_of_the_kind():
    planted = record()
    planted["deadlines"][0]["kind"] = "limitation"  # limitation and forfeiture swapped
    q, _ = rc.quote_record(planted, STORE)
    assert "kindNotInText" in {p["problem"] for p in rc.check_values(q)["problems"]}


def test_check_values_checks_the_articles_a_message_names():
    planted = record()
    planted["deadlines"][0]["message"]["en"] = "Six months (art. 504)."
    q, _ = rc.quote_record(planted, STORE)
    assert "messageArticleNotInBasis" in {p["problem"] for p in rc.check_values(q)["problems"]}


def test_flag_neighbours_finds_the_uncited_exception_and_the_next_article():
    flags = rc.flag_neighbours(record(), STORE)
    kinds = {(f["article"], f["paragraph"], f["kind"]) for f in flags}
    assert ("504", 2, "uncitedParagraph") in kinds  # «غير أنه» left out of b2
    assert ("505", 1, "nextArticle") not in kinds  # 505 neither opens with an exception nor names 504


def test_compare_derivations_matches_on_content_and_reports_the_period():
    a, b = record(), record()
    b["basis"][0]["id"], b["deadlines"][0]["basis"], b["deadlines"][0]["counting"]["basis"] = "s9", ["s9"], ["s9"]
    b["deadlines"][0]["id"] = "dd"
    r = rc.compare_derivations(a, b)
    assert r["differences"] == []
    assert {"kind": "deadline", "a": "d1", "b": "dd", "reconciled": None} in r["idmap"]
    b["deadlines"][0]["period"] = {"value": 2, "unit": "month"}
    r = rc.compare_derivations(a, b)
    assert [(d["kind"], d["field"]) for d in r["differences"]] == [("deadline", "period")]


def test_compare_c_turns_a_lone_difference_into_an_attack():
    a, b, c = record(), record(), record()
    c["deadlines"][0]["period"] = {"value": 3, "unit": "month"}
    r = rc.compare_c(c, a, b)
    assert len(r["attacks"]) == 1 and r["attacks"][0]["who"] == "derivationC"


def test_grade_follows_the_table():
    q, _ = rc.quote_record(record(), STORE)
    final, report = rc.grade_record(q, STORE, disputes=[], triaged={"cpca"})
    g = {row["item"]: row for row in report["items"]}
    assert g["d1"]["grade"] == "B" and g["d1"]["reasons"] == ["singleDerivation", "notAttacked", "scanNotRun"]
    assert final["examples"][0]["expect"]["findings"][0]["shownEffect"] == "warn"
    assert final["stage"] == "final"
    # all six conditions hold: A, and the block stays
    clean = FakeStore([entry("503", STORE.get("cpca", "503")["paragraphs"], clean=True, reason=None),
                       entry("504", STORE.get("cpca", "504")["paragraphs"], clean=True, reason=None)])
    q, _ = rc.quote_record(record(), clean)
    idmap = [{"kind": "deadline", "a": "d1", "b": "dd", "reconciled": None},
             {"kind": "statement", "a": "b1", "b": "s1", "reconciled": None},
             {"kind": "statement", "a": "b2", "b": "s2", "reconciled": None}]
    final, report = rc.grade_record(q, clean, idmap=idmap, differences=[], attacks=[], disputes=[], triaged={"cpca"})
    assert {r["item"]: r["grade"] for r in report["items"]}["d1"] == "A"
    assert final["examples"][0]["expect"]["findings"][0]["shownEffect"] == "blockCleanExport"
    assert final["history"]["agreementBeforeReconciliation"] == "3 of 3 matched items"


def test_grade_d_for_an_unverified_text_and_corpus_checked_in_critical_use():
    r = record()
    r["basis"].append({"id": "b3", "role": "procedure", "cite": {"code": "cpca", "article": "19"},
                       "says": {"ar": "", "en": ""}})
    r["deadlines"][0]["basis"] = ["b1", "b3"]
    q, _ = rc.quote_record(r, STORE)
    _, report = rc.grade_record(q, STORE, disputes=[], triaged={"cpca"})
    g = {row["item"]: row for row in report["items"]}
    assert g["d1"]["grade"] == "D" and g["d1"]["reasons"] == ["textNotVerified: cpca 19"] and g["d1"]["toCheck"]
    assert g["b3"]["grade"] == "D"  # a statement in critical use: corpusChecked is not enough


def test_grade_c_for_a_deadline_without_counting_or_a_french_mismatch():
    r = record()
    del r["deadlines"][0]["counting"]
    q, _ = rc.quote_record(r, STORE)
    _, report = rc.grade_record(q, STORE, disputes=[], triaged={"cpca"})
    assert {x["item"]: x["grade"] for x in report["items"]}["d1"] == "C"


def test_flatten_copies_blocks_prefixes_ids_and_refuses_a_missing_block():
    module = record(id="labour-prelitigation")
    claim = {"id": "c-test", "kind": "claim", "basis": [], "slots": [],
             "imports": [{"module": "labour-prelitigation", "blocks": ["d1"]}]}
    flat = rc.flatten_imports(claim, {"labour-prelitigation": module})
    assert flat["deadlines"][0]["id"] == "labour-prelitigation.d1"
    assert flat["deadlines"][0]["basis"] == ["labour-prelitigation.b1"]
    assert [s["id"] for s in flat["basis"]] == ["labour-prelitigation.b1"] and flat["slots"] == ["s1"]
    claim["imports"][0]["blocks"] = ["d9"]
    with pytest.raises(ValueError):
        rc.flatten_imports(claim, {"labour-prelitigation": module})


def test_check_checklist_wants_every_line_and_flag_answered():
    r = record(checklist=[{"id": "k1", "item": "Which period?", "from": ["derivationA"], "answer": "applies", "refs": ["d1"]}])
    flags = rc.flag_neighbours(r, STORE)
    res = rc.check_checklist(r, [{"id": "k1", "item": "Which period?"}, {"id": "k2", "item": "Is there an exception?"}], flags)
    assert res["unansweredLines"] == ["k2"] and res["unansweredFlags"] and not res["ok"]
    r["checklist"].append({"id": "k2", "item": "Is there an exception?", "answer": "doesNotApply",
                           "note": "Art. 504 ¶2 extends the period by order of the judge: not for this claim."})
    assert rc.check_checklist(r, [{"id": "k1", "item": "Which period?"}, {"id": "k2", "item": "Is there an exception?"}],
                              flags)["ok"]


def test_merge_checklists_merges_exact_duplicates_only():
    m = rc.merge_checklists({"derivationA": ["Which period?", "Who raises it?"], "codex": ["which period?  ", "Another"]})
    assert [(x["item"], x["from"]) for x in m] == [("Which period?", ["derivationA", "codex"]),
                                                   ("Who raises it?", ["derivationA"]), ("Another", ["codex"])]


def test_inventory_lists_each_cited_article_once():
    inv = rc.inventory_cited([record(), record(id="m-two")])
    assert [(x["article"], x["units"]) for x in inv] == [("503", ["m-test", "m-two"]), ("504", ["m-test", "m-two"])]


def test_an_untriaged_code_keeps_every_item_below_a():
    clean = FakeStore([entry("503", STORE.get("cpca", "503")["paragraphs"], clean=True, reason=None),
                       entry("504", STORE.get("cpca", "504")["paragraphs"], clean=True, reason=None)])
    q, _ = rc.quote_record(record(), clean)
    idmap = [{"kind": "deadline", "a": "d1", "b": "dd", "reconciled": None},
             {"kind": "statement", "a": "b1", "b": "s1", "reconciled": None}]
    final, report = rc.grade_record(q, clean, idmap=idmap, differences=[], attacks=[], disputes=[], triaged=set())
    d1 = {r["item"]: r for r in report["items"]}["d1"]
    assert d1["grade"] == "B" and d1["reasons"] == ["triageOpen: cpca"]
    assert final["examples"][0]["expect"]["findings"][0]["shownEffect"] == "warn"


def test_a_disputed_paragraph_gives_c_even_when_the_text_is_not_verified():
    store = FakeStore([entry("503", STORE.get("cpca", "503")["paragraphs"], verified=False, by=None),
                       entry("504", STORE.get("cpca", "504")["paragraphs"])])
    q, _ = rc.quote_record(record(), store)
    dispute = [{"code": "cpca", "article": "503", "version": "law-08-09", "paragraphs": [1], "status": "open",
                "toCheck": False, "readings": [{"ar": "x", "en": "x"}, {"ar": "y", "en": "y"}]}]
    final, report = rc.grade_record(q, store, disputes=dispute, triaged={"cpca"})
    g = {r["item"]: r for r in report["items"]}
    assert g["d1"]["grade"] == "C" and g["d1"]["reasons"][0] == "disputedText: cpca 503" and not g["d1"]["toCheck"]
    assert g["b1"]["grade"] == "C"
    _, report = rc.grade_record(q, store, disputes=[], triaged={"cpca"})
    assert {r["item"]: r["grade"] for r in report["items"]}["d1"] == "D"  # without the owner's decision: unverified
    dispute[0]["paragraphs"] = [2]  # a dispute on another paragraph does not touch a cite of paragraph 1 only
    q["basis"][0]["cite"]["paragraphs"] = [1]
    _, report = rc.grade_record(q, store, disputes=dispute, triaged={"cpca"})
    assert {r["item"]: r["grade"] for r in report["items"]}["d1"] == "D"


def test_the_owners_files_load():
    assert any(t["article"] == "19" for t in rc.text_disputes())
    assert "civil-code" not in rc.triage_complete()
