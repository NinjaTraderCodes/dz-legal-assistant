"""The catalogue check of the catalogue route (specs/unit-driver.md, "The catalogue route for claim types"), on a fixture
claim: what the store says of each catalogue article, and which final module items already carry its prerequisites and
documents. The claim, the store entries and the module are fixtures; the Arabic lines are short and real-shaped."""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import catalogue_check as cc  # noqa: E402


class FakeStore:
    def __init__(self, entries):
        self.in_force = {(e["code"], e["article"]): e for e in entries if e["inForce"]}
        self.any_version = {}
        for e in entries:
            self.any_version.setdefault((e["code"], e["article"]), []).append(e)

    def get(self, code, article, version=None):
        e = self.in_force.get((code, article))
        if e:
            return e
        return next((x for x in self.any_version.get((code, article), []) if x["repealed"]), None)


def entry(article, paragraphs=1, verified=True, by="libraryProof", in_force=True, repealed=False, version="v1"):
    return {"code": "civil-code", "article": article, "version": version, "inForce": in_force, "repealed": repealed,
            "paragraphs": ["نص"] * paragraphs, "tier": 2, "verified": verified, "verifiedBy": by, "frenchCheck": None,
            "ledgerClean": True, "ledgerReason": None}


STORE = FakeStore([entry("10", paragraphs=2), entry("11", verified=False, by=None), entry("12", in_force=False,
                                                                                           repealed=True),
                   entry("13", in_force=False, version="old"), entry("14", by="corpusAndReread")])

CLAIM = {"id": "fixture-claim", "articles": [
    {"code": "civil-code", "number": "10", "role": "basis", "required": True, "why": {"ar": "أساس", "en": "basis"}},
    {"code": "civil-code", "number": "11", "role": "proof", "why": {"ar": "إثبات", "en": "proof"}},
    {"code": "civil-code", "number": "12", "role": "basis", "why": {"ar": "ملغاة", "en": "repealed"}},
    {"code": "civil-code", "number": "13", "role": "basis", "why": {"ar": "قديمة", "en": "superseded"}},
    {"code": "civil-code", "number": "14", "role": "prerequisite", "why": {"ar": "الإعذار", "en": "notice"}},
    {"code": "civil-code", "number": "99", "role": "damages", "why": {"ar": "غائبة", "en": "absent"}}],
    "prerequisites": [{"ar": "توجيه الإعذار إلى المدين قبل رفع الدعوى", "en": "notice before filing"}],
    "requiredDocuments": ["محضر تبليغ الإعذار", "عقد البيع", "وصل الدفع"]}

MODULE = {"id": "notice-module", "basis": [
    {"id": "b1", "cite": {"code": "civil-code", "article": "14"}},
    {"id": "b2", "cite": {"code": "civil-code", "article": "10", "paragraphs": [2]}}],
    "priorSteps": [{"id": "p1", "basis": ["b1"], "message": {"ar": "يجب إعذار المدين قبل رفع الدعوى", "en": "x"}}],
    "documents": [{"id": "doc1", "name": {"ar": "محضر التبليغ الرسمي للإعذار", "en": "x"}, "basis": ["b1"]},
                  {"id": "doc2", "name": {"ar": "وصل الإيداع", "en": "x"}}]}


def test_each_catalogue_article_gets_its_store_state_and_the_module_items_resting_on_it():
    texts = {"chapters": [{"code": "civil-code", "articles": ["10", "11", "14"]}]}
    doc = cc.check(CLAIM, STORE, {"notice-module": MODULE}, texts)
    a = {r["number"]: r for r in doc["articles"]}
    assert (a["10"]["status"], a["10"]["verified"], a["10"]["proof"], a["10"]["paragraphs"]) == ("inForce", True,
                                                                                                "mechanical", 2)
    assert a["11"]["verified"] is False and a["11"]["proof"] is None
    assert a["12"]["status"] == "repealed"
    assert a["13"]["status"] == "notInForce" and a["13"]["versions"] == ["old"]
    assert a["14"]["proof"] == "twoFamilies"
    assert a["99"] == {"code": "civil-code", "number": "99", "role": "damages", "required": False, "inStore": False,
                       "status": "notInStore", "inModules": [], "inUnitTexts": False}
    assert a["14"]["inModules"] == [{"module": "notice-module", "statements": ["b1"],
                                     "items": ["document:doc1", "priorStep:p1"]}]
    assert a["10"]["inModules"] == [{"module": "notice-module", "statements": ["b2"], "items": []}]
    s = doc["summary"]
    assert s["notInStore"] == ["civil-code 99"] and s["repealedOrNotInForce"] == ["civil-code 12", "civil-code 13"]
    assert s["notVerified"] == ["civil-code 11"] and s["outsideUnitTexts"] == ["civil-code 12", "civil-code 13",
                                                                               "civil-code 99"]


def test_prerequisites_and_documents_point_to_the_module_items_that_carry_them():
    doc = cc.check(CLAIM, STORE, {"notice-module": MODULE})
    pre = {p["from"]: p for p in doc["prerequisites"]}
    assert pre["prerequisites"]["inModules"][0]["item"] == "priorSteps:p1"
    assert pre["prerequisites"]["inModules"][0]["strength"] == "strong"
    assert pre["articles"]["inModules"] == [{"module": "notice-module", "items": ["priorStep:p1"]}]
    docs = {d["document"]: d["inModules"] for d in doc["documents"]}
    assert docs["محضر تبليغ الإعذار"][0]["item"] == "documents:doc1"
    assert docs["محضر تبليغ الإعذار"][0]["strength"] == "strong"
    assert docs["عقد البيع"] == []
    assert [(m["item"], m["strength"]) for m in docs["وصل الدفع"]] == [("documents:doc2", "weak")]
    assert doc["summary"]["documentsMatchedInModules"] == 1  # a weak match is shown, never counted
    assert "inUnitTexts" not in doc["articles"][0]  # no texts file given


def test_the_check_is_deterministic():
    one = json.dumps(cc.check(CLAIM, STORE, {"notice-module": MODULE}), ensure_ascii=False, sort_keys=True)
    two = json.dumps(cc.check(CLAIM, STORE, {"notice-module": MODULE}), ensure_ascii=False, sort_keys=True)
    assert one == two


def test_words_meet_across_the_article_hamza_and_diacritics():
    assert cc.words("الإعذارُ") == cc.words("إعذار") == {"اعذار"}
    assert cc.shared("محضر الإعذار", "الإعذار نفسه") == (["اعذار"], "weak")
    assert cc.shared("عقد البيع", "بيع العقار") == (["بيع"], "weak")
    assert cc.shared("عقد البيع", "العقد المتضمن البيع") == (["بيع", "عقد"], "strong")
    assert cc.shared("محضر", "عقد") == ([], None)
