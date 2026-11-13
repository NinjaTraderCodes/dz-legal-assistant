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
    assert rd.compare_article(["نص"], reading(["نص"], unreadable=[{"paragraph": 1, "about": "x"}]))["result"] == "unreadableMark"
    assert rd.compare_article(["نص"], reading([], found=False))["result"] == "notFound"
    assert rd.compare_article(["نص"], reading(["نص"], cont=True))["result"] == "incomplete"


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
