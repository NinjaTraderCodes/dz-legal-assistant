"""The code-made value corpus (specs/reader.md, rung 2): exact spans, every form, the same corpus for the same seed."""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import value_corpus as vc  # noqa: E402


def test_each_span_holds_the_value_and_a_seed_gives_the_same_corpus(tmp_path):
    a, b = tmp_path / "a.jsonl", tmp_path / "b.jsonl"
    vc.main(["--dates", "300", "--amounts", "300", "--seed", "5", "--out", str(a)])
    vc.main(["--dates", "300", "--amounts", "300", "--seed", "5", "--out", str(b)])
    assert a.read_bytes() == b.read_bytes()
    for line in a.read_text(encoding="utf-8").splitlines():
        item = json.loads(line)
        value = item["text"][item["start"]:item["start"] + item["length"]]
        assert value and value == value.strip()
        assert value.split()[0] not in ("في", "بتاريخ", "يوم", "شهر", "سنة", "عام", "خلال")  # lead words stay outside


def test_number_words_follow_the_guide():
    assert vc.words(350000) == "ثلاثمائة وخمسون ألف"
    assert vc.words(768) == "سبعمائة وثمانية وستون"
    assert vc.words(2000) == "ألفان"
    assert vc.words(3_500_000) == "ثلاثة ملايين وخمسمائة ألف"


def test_the_committed_corpus_is_the_seed_11_corpus(tmp_path):
    out = tmp_path / "c.jsonl"
    vc.main(["--out", str(out)])
    committed = os.path.join(os.path.dirname(__file__), "..", "..", "tests", "DzLegal.CoreLogic.Reader.Tests", "value-corpus.jsonl")
    assert out.read_bytes() == open(committed, "rb").read()
