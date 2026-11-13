import copy
import json
import os
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import store  # noqa: E402


def version(vid, text, status="in-force", state="checked", path=None, method="Codex alignment"):
    return {"versionId": vid, "introducedBy": vid.split("@")[1], "status": status, "text": text,
            "paragraphs": text.split("\n") if text else [], "path": path or ["باب"],
            "verification": {"state": state, "checks": [{"check": "x", "method": method}]}}


def corpus_code(articles, publications, unsettled=()):
    return {"articles": {"articles": articles}, "ledger": {"publications": publications},
            "report": {"unsettled": list(unsettled)}}


def article(code, number, versions):
    return {"id": f"{code}:{number}", "number": number, "canonicalNumber": number, "path": ["باب"],
            "availability": "text" if versions else "reference-only", "versions": versions}


CPCA_PUBS = [{"id": "law-08-09", "kind": "law", "date": "2008-02-25", "articlesTouched": ["504", "826"]},
             {"id": "law-22-13", "kind": "law", "date": "2022-07-12", "articlesTouched": ["32", "826"]}]
CIVIL_PUBS = [{"id": "ordinance-75-58", "kind": "ordinance", "date": "1975-09-26", "articlesTouched": ["18", "309"]},
              {"id": "law-05-10", "kind": "law", "date": "2005-06-20", "articlesTouched": ["18"]}]


def inputs(readings=(), delta=None):
    library = {
        "cpca": {"id": "cpca", "check": "officialJournalPage", "articles": [
            {"number": "504", "text": "يجب رفع الدعوى في أجل ستة (6) أشهر.", "path": ["باب"]},
            {"number": "826", "text": "نص ملغى ما زال في المكتبة.", "path": ["باب"]}]},
        "civil-code": {"id": "civil-code", "check": "officialPdfText", "consolidatedAsOf": "2007-05-13", "articles": [
            {"number": "182", "text": "إذا لم يكن التعويض مقدرا.\nغير أنه إذا كان الالتزام مصدره العقد.", "path": ["باب"]}]},
        "labour-disputes": {"id": "labour-disputes", "check": "pageImagesReadTwice", "articles": [
            {"number": "19", "text": "يجب أن يكون كل خلاف فردي موضوع محاولة للصلح.", "path": ["باب"]}]},
        "commercial-code": {"id": "commercial-code", "check": "officialPdfText", "articles": []},
        "family-code": {"id": "family-code", "check": "officialJournalPage", "articles": []},
        "labour-relations": {"id": "labour-relations", "check": "pageImagesReadTwice", "articles": []},
    }
    corpus = {
        "cpca": corpus_code([
            article("cpca", "504", [version("cpca:504@law-08-09", "يجب رفع الدعوى في أجل ستة (6) أشهر.")]),
            article("cpca", "826", [version("cpca:826@law-08-09", "نص قديم."), version("cpca:826@law-22-13", "", status="repealed")]),
            article("cpca", "32", [version("cpca:32@law-08-09", "النص الأول."), version("cpca:32@law-22-13", "النص المعدل.")]),
        ], CPCA_PUBS, [{"id": "cpca:32@law-22-13", "reason": "Law 22-13 art. 13 defers the new rules."}]),
        "civil-code": corpus_code([
            article("civil-code", "18", [version("civil-code:18@ordinance-75-58", "نص المادة 18.")]),
            article("civil-code", "182", []),
            article("civil-code", "309", [version("civil-code:309@ordinance-75-58", "يتقادم بخمس سنوات.\nغير أنه لا يسقط الريع.",
                                                  method="Claude visual reading")]),
        ], CIVIL_PUBS),
    }
    empty = {"articles": [], "corpusCorrections": [], "libraryCorrections": []}
    return store.Inputs(library, corpus, delta or empty, empty, list(readings), None, {"library": "x", "corpus": "9b60990"})


def build(tmp_path, **kw):
    out = str(tmp_path / "articles.json")
    store.build(out, inputs(**kw))
    return store.Store(out)


def test_the_version_in_force_is_the_last_one(tmp_path):
    s = build(tmp_path)
    assert s.get("cpca", "32")["version"] == "law-22-13"
    assert s.get("cpca", "32")["paragraphs"] == ["النص المعدل."]
    assert s.get("cpca", "32", "law-08-09")["paragraphs"] == ["النص الأول."]
    assert s.get("cpca", "32", "law-08-09")["inForce"] is False


def test_a_repealed_article_is_served_as_repealed_whatever_the_library_holds(tmp_path):
    e = build(tmp_path).get("cpca", "826")
    assert e["repealed"] is True and e["paragraphs"] == [] and e["statusFrom"] == "corpus"


def test_the_guard_flags_an_article_touched_later_without_a_new_version(tmp_path):
    s = build(tmp_path)
    assert s.get("civil-code", "18")["guardFlag"] is True
    assert s.get("civil-code", "18")["verified"] is False
    assert s.get("civil-code", "309")["guardFlag"] is False


def test_a_reference_only_corpus_article_is_served_from_the_library(tmp_path):
    e = build(tmp_path).get("civil-code", "182")
    assert e["store"] == "library" and e["statusFrom"] == "library" and len(e["paragraphs"]) == 2
    assert e["verified"] is True and e["verifiedBy"] == "libraryProof" and e["tier"] == 2


def test_cpca_is_corpus_checked_and_a_reading_makes_it_oj_page_reread(tmp_path):
    s = build(tmp_path)
    e = s.get("cpca", "504")
    assert e["store"] == "library" and e["agreement"] == "identical" and e["verifiedBy"] == "corpusChecked"
    reading = {"code": "cpca", "article": "504", "version": "law-08-09", "family": "claude", "result": "agrees",
               "storeTextSha256": e["textSha256"]}
    assert build(tmp_path, readings=[reading]).get("cpca", "504")["verifiedBy"] == "ojPageReread"
    stale = dict(reading, storeTextSha256="something else")
    assert build(tmp_path, readings=[stale]).get("cpca", "504")["verifiedBy"] == "corpusChecked"


def test_a_corpus_only_civil_article_needs_a_reading_by_the_other_family(tmp_path):
    s = build(tmp_path)
    e = s.get("civil-code", "309")
    assert e["verified"] is False
    by_claude = {"code": "civil-code", "article": "309", "version": "ordinance-75-58", "family": "claude", "result": "agrees",
                 "storeTextSha256": e["textSha256"]}
    assert build(tmp_path, readings=[by_claude]).get("civil-code", "309")["verified"] is False  # its checker was Claude
    by_codex = dict(by_claude, family="codex")
    assert build(tmp_path, readings=[by_codex]).get("civil-code", "309")["verifiedBy"] == "corpusAndReread"


def test_the_labour_laws_need_a_codex_reading(tmp_path):
    e = build(tmp_path).get("labour-disputes", "19")
    assert e["verified"] is False and e["version"] == "law-90-04"
    r = {"code": "labour-disputes", "article": "19", "version": "law-90-04", "family": "codex", "result": "agrees",
         "storeTextSha256": e["textSha256"]}
    assert build(tmp_path, readings=[r]).get("labour-disputes", "19")["verifiedBy"] == "libraryAndCodexReread"


def test_the_in_force_note_is_carried_as_a_lead(tmp_path):
    e = build(tmp_path).get("cpca", "32")
    assert "defers" in e["inForceNote"]["lead"]


def test_a_delta_article_and_two_wordings(tmp_path):
    proof = {"readings": [{"model": "claude-sonnet-5-5"}, {"model": "codex-cli 0.167.0 (default model)"}], "agreement": "twoAgree",
             "french": {"url": "u", "sha256": "s", "retrievedOn": "d"}, "frenchChecked": True,
             "consolidatedPdf": {"status": "sameLetters"}}
    delta = {"articles": [
        {"code": "civil-code", "number": "312", "text": "نص جديد.", "version": "ordinance-75-58", "proof": proof, "path": ["باب"]},
        {"code": "civil-code", "number": "312", "text": "نص أقدم.", "version": "law-00-00", "proof": proof,
         "supersededWording": True, "path": ["باب"]}],
        "corpusCorrections": [], "libraryCorrections": []}
    s = build(tmp_path, delta=delta)
    assert s.get("civil-code", "312")["paragraphs"] == ["نص جديد."]
    assert s.get("civil-code", "312")["verifiedBy"] == "twoFamilies" and s.get("civil-code", "312")["frenchCheck"] == "equal"
    assert s.get("civil-code", "312", "law-00-00")["inForce"] is False


def test_the_store_is_deterministic(tmp_path):
    a = store.build(str(tmp_path / "a.json"), inputs())
    b = store.build(str(tmp_path / "b.json"), inputs())
    assert a["sha256"] == b["sha256"]


@pytest.mark.skipif(not os.path.isdir(os.path.join(store.REPO, "data", "law", "codes")), reason="needs the repository")
def test_the_real_store_builds_twice_to_the_same_hash_with_the_known_facts(tmp_path):
    real = store.Inputs.load()
    a = store.build(str(tmp_path / "a.json"), real)
    b = store.build(str(tmp_path / "b.json"), copy.deepcopy(real))
    assert a["sha256"] == b["sha256"]
    s = store.Store(str(tmp_path / "a.json"))
    for n in ("826", "835", "836", "912", "913", "914"):
        assert s.get("cpca", n)["repealed"] is True, n
    for n in ("18", "19", "22", "23", "24", "25"):
        assert s.get("civil-code", n)["guardFlag"] is True, n
    assert s.get("civil-code", "182")["store"] == "library"
