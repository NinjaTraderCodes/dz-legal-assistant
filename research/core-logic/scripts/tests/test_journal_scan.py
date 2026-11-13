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


def _shift(text):
    """The broken font of some 2005 issues, applied to plain text: 29 code points lower, accents as plain letters."""
    back = {v: k for k, v in js.ACCENTS.items()}
    return "".join(back.get(c, chr(ord(c) - js.SHIFT) if 0x20 <= ord(c) <= 0x7A else c) for c in text)


def test_a_shifted_line_is_decoded_and_a_plain_line_is_left_alone():
    plain = "La présente loi modifie et complète la loi n° 75-59 portant code de commerce."
    pages, count = js.repair([_shift(plain) + "\n" + plain])
    assert count == 1
    assert pages[0].split("\n") == [plain, plain]


def test_the_search_finds_an_amendment_in_shifted_text():
    pages, _ = js.repair([_shift("Art. 2. — L'article 32 de la loi n° 08-09 susvisée est modifié comme suit :")])
    hits, _ = js.search(pages)
    assert ("cpca", "operative") in [(h["instrument"], h["class"]) for h in hits]


def test_a_list_of_names_is_readable_and_shifted_names_are_decoded():
    names = "Hannachi Bouazziz Boutebba Ziani Guellati Achouri Assami Menidjel Djaider Benabdelkader Baroud Maizi"
    assert js.repair_line(names) == (names, False)
    decoded, changed = js.repair_line(_shift(names))
    assert changed and decoded == names


def test_the_readability_check_flags_a_page_that_does_not_read_as_french():
    french = "La présente loi a pour objet de modifier et compléter la loi relative aux relations de travail. " * 6
    garbled = _shift(french).replace("\x03", "\x03")  # what pdftotext gives for the broken font, before any repair
    assert js.unreadable_pages([french]) == []
    assert js.unreadable_pages([french, garbled]) == [2]
    assert js.unreadable_pages(["12 345 678"]) == []  # a table without words is not judged
