import json
import os
import re
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import reading as rd  # noqa: E402

JOB = {"batch": "b", "job": "j", "kind": "reread", "why": "criticalUse", "code": "cpca", "version": "law-08-09",
       "language": "Arabic", "critical": True, "dpi": 300,
       "source": {"url": "docs/official-law-texts/A2008021.pdf", "sha256": "x", "localPdf": "x"},
       "pages": [35, 36], "images": [{"path": r"C:\p\a-p035-300.png", "pdfPage": 35, "sha256": "s1"},
                                     {"path": r"C:\p\a-p036-300.png", "pdfPage": 36, "sha256": "s2"}],
       "articles": ["419", "423"]}


def reading(paragraphs, found=True, unreadable=(), cont=False):
    return {"article": "419", "found": found, "paragraphs": list(paragraphs), "unreadable": list(unreadable),
            "continuesOnNextPage": cont, "numbersSeen": [], "where": {"image": 1, "column": "right", "top": 0, "bottom": 1}}


def test_the_prompts_are_filled_and_differ_as_the_template_says():
    s, c = rd.prompt(JOB, "sonnet"), rd.prompt(JOB, "codex")
    assert not re.search(r"\{\{\w+\}\}", s + c)
    assert r"C:\p\a-p035-300.png" in s and r"C:\p\a-p035-300.png" not in c  # Codex gets the images with -i only
    assert "المادة 419، المادة 423" in s and "المادة 419، المادة 423" in c
    assert "Two images, in order: pages 35 and 36" in c
    assert c.rstrip().endswith("Do not run any command and do not open any file.")
    assert "What you may open" in s and "What you may open" not in c


def test_the_schema_file_has_the_shape_of_the_prompt_header():
    header = rd.template_parts()["header"]
    with open(rd.SCHEMA, encoding="utf-8") as f:
        schema = json.load(f)
    item = schema["properties"]["articles"]["items"]
    for key in item["properties"]:
        assert f'"{key}"' in header, key
    assert set(item["required"]) == set(item["properties"])  # strict output: every key required
    assert item["properties"]["where"]["properties"]["column"]["enum"] == ["right", "left", "full", "none"]
    assert set(re.findall(r'"(\w+)": (?:string|boolean|integer|number|\[|\{)', header)) <= \
        set(item["properties"]) | set(item["properties"]["where"]["properties"]) | {"articles", "paragraph", "about"}


@pytest.mark.parametrize("store,read,result,level", [
    (["يرفع الطلب خلال ستة (6) أشهر."], ["يرفع الطلب خلال ستة (6) أشهر."], "agrees", "1"),
    (["يرفع الطلب خلال ستة (6) أشهر."], ["يرفع الطلب خلال ستة (8) أشهر."], "unsettled", "none"),  # a changed digit
    (["لا يقبل الطلب إلا بعد الصلح."], ["يقبل الطلب إلا بعد الصلح."], "unsettled", "none"),  # a dropped لا
    (["الفقرة الأولى.", "الفقرة الثانية."], ["الفقرة الأولى. الفقرة الثانية."], "unsettled", "3"),  # a shifted paragraph
    (["مسؤولية"], ["مسئولية"], "agrees", "2o"),
])
def test_comparing_one_article(store, read, result, level):
    r = rd.compare_article(store, reading(read))
    assert (r["result"], r["level"]) == (result, level)
    if result == "unsettled":
        assert r["differences"] and r["differences"][0]["settled"] is None


def test_a_question_mark_or_a_missing_article_is_not_a_reading():
    assert rd.compare_article(["نص"], reading(["نص [؟]"]))["result"] == "unreadableMark"
    # a note without a [؟] in the text (a punctuation doubt) is a remark, not a gap
    assert rd.compare_article(["نص"], reading(["نص"], unreadable=[{"paragraph": 1, "about": "x"}]))["result"] == "agrees"
    assert rd.compare_article(["نص"], reading([], found=False))["result"] == "notFound"
    assert rd.compare_article(["نص"], reading(["نص"], cont=True))["result"] == "incomplete"
    # followed onto the next image of the same run: compared, not cut off
    assert rd.compare_article(["نص"], reading(["نص"], cont=True), images=2)["result"] == "agrees"


def test_critical_articles_are_read_one_page_or_a_pair_at_a_time():
    with pytest.raises(ValueError):
        rd.make_job("b", "j", "cpca", "v", "x.pdf", [1, 2, 3], ["1"], "reread")
    with pytest.raises(ValueError):
        rd.make_job("b", "j", "cpca", "v", "x.pdf", [1, 2, 3, 4, 5], ["1"], "reread", critical=False)


def test_an_answer_must_match_the_schema():
    ok = {"articles": [reading(["نص"])]}
    assert rd.parse_answer("```json\n" + json.dumps(ok, ensure_ascii=False) + "\n```") == ok
    with pytest.raises(ValueError):
        rd.parse_answer(json.dumps({"articles": [{"article": "1"}]}))


def test_settle_items_hide_the_source_and_the_key_maps_back():
    job = dict(JOB)
    entries = [{"job": job, "bands": ["b1.png", "b2.png"], "article": str(400 + i), "paragraph": 1,
                "kind": "words", "store": "أو حدث", "reading": "أوحدث", "before": "", "after": ""} for i in range(8)]
    text, key = rd.settle_items(entries, "seed")
    assert "store" not in text.lower() and "reading" not in text.lower() and "corpus" not in text.lower()
    assert {k["X"] for k in key} == {"store", "reading"}  # both orders occur over eight items
    for k in key:
        assert {k["X"], k["Y"]} == {"store", "reading"}
    assert rd.settle_items(entries, "seed")[1] == key  # reproducible
    assert "{{" not in rd.settle_prompt(text)


def test_the_store_is_held_wrong_only_with_a_sure_settler_and_the_other_family():
    key = [{"article": "441", "X": "reading", "Y": "store"}]
    sure_reading = [{"printed": "X", "sure": True, "seen": "s", "words": "w"}]
    assert rd.apply_settlement(key, sure_reading, {"441": True})[0]["verdict"] == "reading"
    assert rd.apply_settlement(key, sure_reading, {"441": False})[0]["verdict"] == "open"
    assert rd.apply_settlement(key, [{"printed": "Y", "sure": True, "seen": "", "words": ""}], {})[0]["verdict"] == "store"
    assert rd.apply_settlement(key, [{"printed": "Y", "sure": False, "seen": "", "words": ""}], {})[0]["verdict"] == "open"
    assert rd.apply_settlement(key, [{"printed": "neither", "sure": True, "seen": "", "words": ""}], {})[0]["verdict"] == "open"


def test_a_reader_remark_is_kept_apart_never_read_as_text():
    obj, rest = rd.first_json('```json\n{"articles": []}\n```\n\nNote: the article is not here.')
    assert obj == {"articles": []} and rest.startswith("Note:")
    a = reading(["نص"])
    a["where"]["note"] = "x"
    a["whereNote"] = "y"
    clean, removed = rd.strip_extra({"articles": [a], "note": "z"})
    assert "note" not in clean["articles"][0]["where"] and "whereNote" not in clean["articles"][0]
    assert sorted(removed) == ["419:where.note", "419:whereNote", "top:note"]


def test_an_amending_text_printed_in_part_is_composed_with_the_base():
    base = ["تختص المحاكم الإدارية كذلك بالفصل في :", "1 - دعاوى الإلغاء عن :", "- الولاية،", "- البلدية،",
            "2 - دعاوى القضاء الكامل،", "3 - القضايا المخولة لها بموجب نصوص خاصة."]
    amending = ["تختص المحاكم الإدارية كذلك بالفصل في :", "1– دعاوى إلغاء وتفسير عن :", "- الولاية،",
                "- المنظمات المهنية الجهوية،", "............... (الباقي بدون تغيير)...........\"."]
    assert rd.compose(base, amending) == amending[:4] + base[4:]
    # 600: a lead line, a marker for items 1 to 6, item 7 replaced, the rest kept
    b600 = ["لا يجوز التنفيذ الجبري إلا بسند تنفيذي.", "والسندات التنفيذية هي:", "6 - قرارات المجالس،",
            "7 - أحكام المحاكم الإدارية وقرارات مجلس الدولة،", "8 - محاضر الصلح،", "وتعتبر أيضا سندات تنفيذية كل العقود."]
    a600 = ["لا يجوز التنفيذ الجبري إلاّ بسند تنفيذي.", "....(المطات من 1 إلى 6 بدون تغيير)....",
            "7– أحكام المحاكم الإدارية وقرارات المحاكم الإدارية للاستئناف ومجلس الدولة.", "...(الباقي بدون تغيير)...\"."]
    assert rd.compose(b600, a600) == [a600[0], b600[1], b600[2], a600[2], b600[4], b600[5]]
    # 32: paragraphs repealed by number
    assert rd.compose(["a", "b", "c", "d"], repealed=[3, 4]) == ["a", "b"]
    assert rd.compare_article(["x"], reading(["1– جديد", "(الباقي بدون تغيير)"]))["result"] == "partialPrint"
    with pytest.raises(ValueError):
        rd.compose(base, ["4 - بند جديد،"])


def test_a_heading_the_reader_copied_from_an_amending_text_is_dropped_before_comparing():
    reading = {"article": "121", "found": True, "continuesOnNextPage": False, "where": {"image": 1},
               "paragraphs": ['"المادة 121 : في العقود الملزمة للجانبين إذا انقضى التزام"']}
    out = rd.compare_article(["في العقود الملزمة للجانبين إذا انقضى التزام"], reading)
    assert out["result"] == "agrees" and out["headingDropped"].startswith('"المادة 121')
    other = dict(reading, article="122")  # only the heading of the article asked
    assert rd.compare_article(["في العقود الملزمة للجانبين إذا انقضى التزام"], other)["result"] == "unsettled"
    mid = dict(reading, paragraphs=["نص يذكر المادة 121 في وسطه"])
    assert rd.drop_heading(mid)[1] is None
