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


def c_answer(rec: dict) -> dict:
    """Codex's answer in its own schema (module-derivation-c.schema.json), carrying the same content as the record."""
    stmts = rc.statements(rec)
    basis = lambda ids: [{"code": stmts[i]["cite"]["code"], "article": stmts[i]["cite"]["article"],  # noqa: E731
                          "paragraphs": stmts[i]["cite"].get("paragraphs") or []} for i in ids if i in stmts]
    return {"statements": [dict(x, says="", role="basis", interpretive=False) for x in basis(list(stmts))],
            "deadlines": [{"kind": d["kind"], "periodValue": d["period"]["value"], "periodUnit": d["period"]["unit"],
                           "startsFromEvent": "", "countingWords": "", "raisedBy": d.get("raisedBy", "unclear"),
                           "sanctionWords": "", "periodWords": "", "basis": basis(d["basis"]), "interpretive": False}
                          for d in rec.get("deadlines", [])],
            "priorSteps": [{"kind": p["kind"], "requiredWhen": "", "notRequiredWhen": [], "sanctionIfMissing": "",
                            "basis": [], "interpretive": False} for p in rec.get("priorSteps", [])],
            "sections": [], "territory": [], "exits": [], "openPoints": [], "missingTexts": []}


def test_compare_c_reads_codex_in_its_own_shape_and_finds_no_difference_on_the_same_content():
    a, b = record(), record()
    r = rc.compare_c(c_answer(a), a, b)
    assert r["attacks"] == []
    assert any(row["field"].startswith("cite [") and row["c"] for row in r["rows"])


def test_compare_c_turns_a_lone_difference_into_an_attack():
    a, b = record(), record()
    c = c_answer(a)
    c["deadlines"][0]["periodValue"], c["deadlines"][0]["periodUnit"] = 3, "month"
    r = rc.compare_c(c, a, b)
    assert len(r["attacks"]) == 1 and r["attacks"][0]["who"] == "derivationC"


def test_compare_c_holds_a_missing_cite_against_c_only_inside_the_texts_it_was_given():
    a, b = record(), record()
    c = c_answer(a)
    dropped = c["statements"].pop(0)
    unit = (dropped["code"], dropped["article"])
    assert [x["target"] for x in rc.compare_c(c, a, b)["attacks"]] != []
    assert rc.compare_c(c, a, b, given=set())["attacks"] == []
    assert rc.compare_c(c, a, b, given={unit})["attacks"] != []


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
    # the live file: Civil Code and CPCA triage complete since 14 Nov 2026; the Family Code not yet
    assert {"civil-code", "cpca"} <= rc.triage_complete() and "family-code" not in rc.triage_complete()


def test_unit_texts_takes_the_store_chapter_or_the_edition_heading():
    a = entry("19", ["أول"])
    a["path"] = ["الباب الأول"]
    b = entry("20", ["ثان"])
    b["path"] = ["الباب الأول"]
    c = entry("176", ["فقرة أولى", "فقرة ثانية"])
    c["path"] = []
    d = entry("177", ["فقرة"])
    d["path"] = []
    store = FakeStore([a, b, c, d])
    store.chapter = lambda code, path: [e for e in (a, b) if e["path"] == path]
    edition = {"cpca": {"176": ["الفصل الثاني"], "177": ["الفصل الثاني"], "178": ["الفصل الثاني"]}}
    doc = rc.unit_texts("u", [("cpca", "19"), ("cpca", "176")], store, edition)
    assert [(ch["from"], ch["articles"]) for ch in doc["chapters"]] == [("store", ["19", "20"]),
                                                                         ("consolidatedEdition", ["176", "177"])]
    assert "[cpca 176 ¶2] فقرة ثانية" in doc["rendered"]
    assert doc["chapters"][1]["missingFromStore"] == ["178"]  # in the edition, not in the store: named, to be fetched


def test_add_cases_keeps_only_failed_cross_record_results_and_replaces_an_earlier_run():
    doc = {"differences": [{"id": "x1", "kind": "statement"}], "agreementTotal": {"equal": 1, "compared": 2}}
    r = lambda rid, case, ok: {"recordId": rid, "caseId": case, "title": "", "differences": [] if ok else ["d"], "passed": ok}  # noqa: E731
    runs = {"aOnB": [r("u", "ex1", False), r("u on u", "ex1", False), r("u on u", "ex2", True)],
            "boundaryAB": [r("u", "bc1", False), r("u (A) | u (B)", "bc1", False)],
            "boundaryA": [r("u", "bc2", False)]}
    out = rc.add_cases(doc, runs)
    assert [(d["id"], d["field"]) for d in out["differences"][1:]] == [("x2", "aOnB ex1"), ("x3", "boundaryA bc2"),
                                                                       ("x4", "boundaryAB bc1")]
    assert out["agreementTotal"] == doc["agreementTotal"] and out["caseDifferences"] == 3
    again = rc.add_cases(out, {"bOnA": [r("u on u", "ex9", False)]})
    assert [d["field"] for d in again["differences"] if d["kind"] == "case"] == ["bOnA ex9"]


class PathStore:
    """Only what seed_starts reads: an entry's path."""

    def __init__(self, paths):
        self.paths = paths

    def get(self, code, article, version=None):
        return {"path": self.paths.get(article)} if article in self.paths else None


def test_seed_starts_go_one_heading_up_from_a_pointer_and_take_the_chapters_holding_seed_articles():
    book, title = "الكتاب الثاني", "الباب الثاني - آثار الالتزام"
    store = PathStore({"179": [book, title, "الفصل الثاني"], "164": [book, title, "الفصل الأول"],
                       "189": [book, title, "الفصل الثالث", "القسم الأول"], "124": [book, "الباب الأول", "الفصل الثالث"]})
    seed = [{"code": "civil-code", "article": "179", "sources": ["pointer:formal-notice"]},
            {"code": "civil-code", "article": "164", "sources": ["claims:x"]},
            {"code": "civil-code", "article": "189", "sources": ["claims:y"]},
            {"code": "civil-code", "article": "124", "sources": ["claims:z"]},
            {"code": "cpca", "article": "164", "sources": ["claims:x"]}]
    got = rc.seed_starts("formal-notice", seed, store, edition_paths={"civil-code": {}})
    assert got == [("civil-code", "164"), ("civil-code", "179"), ("civil-code", "189")]


def test_heading_paths_compare_across_the_store_and_the_edition_spellings():
    assert rc._norm_path(["الكتاب الثانى - الالتزامات", "الباب الاول - مصادر"]) == rc._norm_path(
        ["الكتاب الثاني الالتزامات", "الباب الأول مصادر"])


def test_compare_c_counts_the_cites_outside_the_texts_given():
    a, b = record(), record()
    r = rc.compare_c(c_answer(a), a, b, given=set())
    o = r["outsideTextsGiven"]
    assert o["both"] == o["a"] == o["b"] == o["citedByAOrB"] == 2 and o["paragraphs"]
    assert rc.compare_c(c_answer(a), a, b)["outsideTextsGiven"] is None


def test_complete_idmap_traces_reconciled_items_by_content_and_adds_new_ones():
    a, b = record(), record()
    b["basis"][0]["id"], b["deadlines"][0]["basis"], b["deadlines"][0]["counting"]["basis"] = "s9", ["s9"], ["s9"]
    idmap = rc.compare_derivations(a, b)["idmap"]
    rec = record()
    rec["basis"][1]["id"] = "r2"
    rec["basis"].append({"id": "r3", "role": "procedure", "cite": {"code": "cpca", "article": "505"},
                         "says": {"ar": "", "en": ""}})
    rows = rc.complete_idmap(idmap, a, b, rec)
    by_rec = {r["reconciled"]: r for r in rows if r["reconciled"]}
    assert (by_rec["b1"]["a"], by_rec["b1"]["b"]) == ("b1", "s9")
    assert (by_rec["r2"]["a"], by_rec["r2"]["b"]) == ("b2", "b2")
    assert (by_rec["r3"]["a"], by_rec["r3"]["b"]) == (None, None)
    assert by_rec["d1"]["a"] == by_rec["d1"]["b"] == "d1"


def test_prior_steps_of_one_kind_are_matched_on_their_condition_first():
    steps = lambda ids_and_requests: [{"id": i, "kind": "formalNotice", "requiredWhen": {"slot": "requests", "includes": q},  # noqa: E731
                                       "basis": []} for i, q in ids_and_requests]
    x = record(priorSteps=steps([("p1", "damages"), ("p2", "rescission")]))
    y = record(priorSteps=steps([("q2", "rescission"), ("q1", "damages")]))
    pairs = rc.content_pairs(x, y)
    assert pairs[("priorStep", "p1")] == "q1" and pairs[("priorStep", "p2")] == "q2"
