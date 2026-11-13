import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import textcmp  # noqa: E402

TEXT = "يجب رفع الدعوى أمام القسم الاجتماعي في أجل لا يتجاوز ستة (6) أشهر من تاريخ تسليم محضر عدم الصلح."


def test_identical_is_level_1():
    assert textcmp.level(TEXT, "  " + TEXT + " ") == "1"


def test_hamza_of_alef_alef_maqsura_ta_marbuta_digits_tatweel_and_diacritics_are_level_2():
    variant = ("يجـب رفعُ الدعوي امام القسم الاجتماعى في اجل لا يتجاوز ستة (٦) اشهر من تاريخ تسليم محضر عدم الصلح")
    assert textcmp.level(TEXT, variant) == "2"


def test_the_1975_spelling_passes_level_2():
    assert textcmp.level("كل شىء", "كل شيء") == "2"


def test_a_changed_digit_is_a_difference_at_every_level():
    changed = TEXT.replace("(6)", "(8)")
    assert textcmp.level(TEXT, changed) == "none"
    assert textcmp.compare(TEXT, changed)["differences"][0]["a"] == "(6)"


def test_a_dropped_negation_is_a_difference():
    assert textcmp.level(TEXT, TEXT.replace(" لا يتجاوز", " يتجاوز")) == "none"


def test_a_swapped_exception_word_is_a_difference():
    a = "لا يستحق التعويض إلا بعد اعذار المدين"
    b = "لا يستحق التعويض بعد اعذار المدين إلا"
    assert textcmp.level(a, b) == "none"


def test_hamza_seats_only_are_orthographic():
    assert textcmp.level("مسؤول عن الشؤون", "مسئول عن الشئون") == "2o"


def test_paragraph_division_only_is_level_3():
    a = "الفقرة الأولى من النص.\nالفقرة الثانية."
    b = "الفقرة الأولى من النص. الفقرة الثانية."
    assert textcmp.level(a, b) == "3"


def test_differences_give_ten_words_of_context_and_the_original_words():
    a = " ".join(f"كلمة{i}" for i in range(30))
    b = a.replace("كلمة15", "تغيير")
    d = textcmp.compare(a, b)["differences"]
    assert len(d) == 1 and d[0]["a"] == "كلمة15" and d[0]["b"] == "تغيير"
    assert len(d[0]["before"].split()) == 10 and len(d[0]["after"].split()) == 10
