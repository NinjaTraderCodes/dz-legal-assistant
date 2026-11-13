import os
import sys

from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import consolidated  # noqa: E402
import french  # noqa: E402
import reading as rd  # noqa: E402

JOB = {"batch": "b", "job": "j", "code": "civil-code", "version": "ordinance-75-58", "pages": [20, 21],
       "source": {"localPdf": "x.pdf"}, "images": []}


def fake_band(tmp_path, size):
    def band(job, where, dpi=600, margin=0.03):
        p = tmp_path / f"band-p{where['image']}-{where['column']}.png"
        if not p.exists():
            Image.new("L", size, 255).save(p)
        return str(p)
    return band


def test_tiles_are_small_enough_to_be_shown_at_full_size(tmp_path, monkeypatch):
    monkeypatch.setattr(rd, "band", fake_band(tmp_path, (2409, 6533)))  # a 600-dpi column of the 1975 Journal
    out = rd.tiles(JOB, {"image": 1, "column": "right", "top": 0.4, "bottom": 0.6})
    assert out
    for t in out:
        w, h = Image.open(t["path"]).size
        assert max(w, h) <= rd.TILE_SIDE and w * h <= rd.TILE_AREA
    # the tiles cover the article's region with its margin, with no gap between rows
    rows = sorted({(t["from"], t["to"]) for t in out})
    assert rows[0][0] <= 0.32 and rows[-1][1] >= 0.68
    assert all(a[1] > b[0] for a, b in zip(rows, rows[1:]))


def test_tiles_add_the_next_column_when_the_article_reaches_the_foot(tmp_path, monkeypatch):
    monkeypatch.setattr(rd, "band", fake_band(tmp_path, (2409, 6533)))
    cols = {(t["image"], t["column"]) for t in rd.tiles(JOB, {"image": 1, "column": "left", "top": 0.7, "bottom": 0.95})}
    assert cols == {(1, "left"), (2, "right")}
    cols = {(t["image"], t["column"]) for t in rd.tiles(JOB, {"image": 1, "column": "right", "top": 0.1, "bottom": 0.3,
                                                                "continues": True})}
    assert cols == {(1, "right"), (1, "left")}


def test_consolidated_word_differences(monkeypatch):
    ours = "تتقادم بأربع سنوات الضرائب\nويكون للصورة الرسمية الحجية"
    printed = consolidated.fold("تتقادم بأربع 4 سنوات الضرائب ويكون للصور الرسمية الحجية")
    monkeypatch.setattr(consolidated, "document", lambda code: ("ابجد" * 20 + printed + "هوز" * 20, (0,)))
    got = consolidated.word_differences("civil-code", ours)
    assert [(d["ours"], d["consolidated"]) for d in got] == [("بأربع", "بأربع 4"), ("للصورة", "للصور")]
    assert not any(d["orthographic"] for d in got)
    assert got[1]["before"].endswith("الضرائب ويكون")


def test_consolidated_hamza_seat_is_orthographic(monkeypatch):
    printed = consolidated.fold("ان يجزئ ما ورد فيها")
    monkeypatch.setattr(consolidated, "document", lambda code: ("ابجد" * 20 + printed + "هوز" * 20, (0,)))
    got = consolidated.word_differences("civil-code", "ان يجزىء ما ورد فيها")
    assert len(got) == 1 and got[0]["orthographic"]


def test_french_cut_ignores_accents():
    text = 'Art. 45. — L\'article 324 quinquiès est modifié.\n"Art. 324 quinquiès. — Les actes solennels (2).\nArt. 46. — x'
    assert "solennels (2)" in french.cut(text, "324 مكرر 3")


def test_store_corrections_are_recorded():
    assert "storeDiffers" in rd.RECORDED
