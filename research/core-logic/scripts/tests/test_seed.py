import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import seed  # noqa: E402


def test_interpolation_gives_a_page_or_a_pair():
    anchors = [(300, 19), (310, 19), (330, 21)]
    assert seed.interpolate(anchors, 305) == [19]
    assert seed.interpolate(anchors, 320) == [20]
    assert seed.interpolate(anchors, 325) == [20, 21]
    assert seed.interpolate(anchors, 299) is None  # outside the anchors: found another way
    assert seed.interpolate([], 5) is None


class FakeStore:
    any_version = {("civil-code", "308"): [], ("civil-code", "309"): []}


def test_ranges_take_the_bis_numbers_the_sources_know():
    ledgers = {"civil-code": [{"id": "ordinance-75-58", "date": "1975-09-26", "kind": "ordinance", "articlesTouched": []},
                              {"id": "law-05-10", "date": "2005-06-20", "kind": "law", "articlesTouched": ["323 مكرر"]}]}
    inv = seed.Inventory(FakeStore(), {"civil-code": [{"canonicalNumber": "324 مكرر 1"}]}, ledgers, [])
    got = [a for a, _ in inv.expand("civil-code", "322-324?")]
    assert got == ["322", "323", "323 مكرر", "324", "324 مكرر 1"]
    assert all(m for _, m in inv.expand("civil-code", "322-324?"))
    assert inv.expand("labour-relations", "73-1") == [("73-1", False)]  # a number, not a range
    assert inv.version_for_missing("civil-code", "323 مكرر") == "law-05-10"
    assert inv.version_for_missing("civil-code", "322") == "ordinance-75-58"


def test_the_actions_follow_the_law_readme():
    act = seed.Inventory.action
    assert act({"code": "civil-code"}, {"state": "missing"})["action"] == "fetch"
    assert act({"code": "cpca"}, {"state": "repealed"})["action"] == "none"
    cp = {"state": "inForce", "guardFlag": False, "store": "library", "verified": True, "verifiedBy": "corpusChecked",
          "corpusState": "checked"}
    assert act({"code": "cpca"}, cp) == {"action": "reread", "why": "criticalUse", "readers": ["claude-sonnet-5-5"]}
    assert act({"code": "labour-disputes"}, cp)["readers"] == ["codex"]
    co = dict(cp, store="corpus", verified=False, corpusState="partly-checked")
    assert act({"code": "civil-code"}, co)["readers"] == ["claude-sonnet-5-5", "codex"]
    assert act({"code": "civil-code"}, dict(co, guardFlag=True))["why"] == "guard"
    lib = dict(cp, verifiedBy="libraryProof")
    assert act({"code": "civil-code"}, lib)["action"] == "none"


def test_page_pairs_never_cut_an_article_that_runs_on():
    items = [{"article": "306", "page": 26}, {"article": "323", "page": 27, "end": 28}, {"article": "312", "page": 27},
             {"article": "405", "page": 34}]
    runs = seed.plan_pairs(items)
    assert runs[0] == {"pages": [26, 27], "articles": ["306", "312"]}
    assert runs[1] == {"pages": [27, 28], "articles": ["323"]}
    assert runs[2] == {"pages": [34, 35], "articles": ["405"]}


def test_the_french_check_compares_numbers_not_their_writing():
    import french
    assert french.check("ستة (6) أشهر من المادة 323 مكرر 1", "six (6) mois de l'article 323 ter")["equal"]
    assert french.check("على 100.000 دينار", "à 100.000 DA")["equal"]
    assert not french.check("ستة (6) أشهر", "huit (8) mois")["equal"]
    assert french.french_number("187 مكرر 1")[0] == "187 ter"
    text = "Art. 2. — L'article 169 est modifié :\n« Art. 169. — Les dispositions.\n« Art. 170. — Autre."
    assert french.cut(text, "169").strip().startswith("— Les dispositions")
