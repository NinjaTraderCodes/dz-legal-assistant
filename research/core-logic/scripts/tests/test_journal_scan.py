import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import journal_scan as js  # noqa: E402


def classes(page):
    hits, visas = js.search([page])
    return [(h["instrument"], h["class"]) for h in hits], visas


def test_a_visa_is_counted_not_examined():
    hits, visas = classes("Vu l'ordonnance n° 75-58 du 26 septembre 1975, modifiée et complétée, portant code civil ;\n")
    assert hits == [] and visas["civil-code"] >= 1


def test_a_title_hit():
    hits, _ = classes("Loi n° 22-13 du 12 juillet 2022 modifiant et complétant la loi n° 08-09 du 25 février 2008 "
                      "portant code de procédure civile et administrative.")
    assert ("cpca", "title") in hits


def test_an_operative_hit_in_a_finance_law():
    page = ("Art. 45. — Les dispositions de l'article 124 du code de commerce sont modifiées et rédigées comme suit :")
    hits, _ = classes(page)
    assert ("commercial-code", "operative") in hits


def test_an_operative_phrase():
    hits, _ = classes("Art. 2. — L'article 32 de la loi n° 08-09 susvisée est modifié comme suit :")
    assert ("cpca", "operative") in hits


def test_a_number_inside_a_page_range_or_a_date_does_not_match():
    hits, _ = classes("Pages 75-58 et suivantes ; le 08-09-2008 ; numéros 90-111.")
    assert hits == []


def test_matching_folds_accents_case_hyphens_and_spaces():
    hits, _ = classes("ORDONNANCE N° 84‑11 DU 9 JUIN 1984 PORTANT CODE   DE LA FAMILLE, modifiée")
    assert any(i == "family-code" for i, _ in hits)


def test_a_rectification_is_a_title_hit():
    hits, _ = classes("Rectificatif à la loi n° 05-10 du 20 juin 2005 modifiant et complétant l'ordonnance n° 75-58")
    assert ("civil-code", "title") in hits


def test_the_masthead_date():
    assert js.masthead_date("JOURNAL OFFICIEL\nDimanche 17 juillet 2022\nN° 48") == "2022-07-17"
    assert js.masthead_date("1er janvier 2003") == "2003-01-01"
    assert js.masthead_date("no date here") is None


def test_unrelated_text_gives_nothing():
    hits, visas = classes("Décret exécutif portant organisation de l'administration centrale du ministère.")
    assert hits == [] and visas == {}
