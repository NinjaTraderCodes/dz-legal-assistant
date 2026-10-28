import unittest

import numpy as np

import native_check as nc


class CompareStreams(unittest.TestCase):
    def check(self, canonical, *streams, skip=0):
        return nc.compare_streams(canonical, {f"s{i}": nc.tokens(s) for i, s in enumerate(streams)}, skip=skip)

    def test_identical_text_agrees(self):
        self.assertTrue(self.check("يجب رفع الدعوى في أجل ستة (6) أشهر.", "يجب رفع الدعوى في أجل ستة (6) أشهر.")["agrees"])

    def test_digit_difference_is_never_tolerated(self):
        result = self.check("في أجل ستة (6) أشهر.", "في أجل ستة (8) أشهر.")
        self.assertFalse(result["agrees"])
        self.assertEqual("6", result["disagreements"][0]["canonical"])

    def test_negation_must_be_read(self):
        self.assertFalse(self.check("لا يجوز الطعن فيه.", "يجوز الطعن فيه.")["agrees"])

    def test_comma_versus_full_stop_counts(self):
        self.assertFalse(self.check("الصلح، وذلك", "الصلح. وذلك")["agrees"])

    def test_one_letter_noise_on_long_ordinary_word_is_tolerated(self):
        self.assertTrue(self.check("حسب الإجراءات العادية.", "حسب الإجراءاث العادية.")["agrees"])

    def test_short_word_substitution_is_not_noise(self):
        self.assertFalse(self.check("أو تسرب.", "أر تسرب.")["agrees"])

    def test_ocr_merge_is_tolerated_but_canonical_merge_is_not(self):
        self.assertTrue(self.check("رفع الدعوى أمام القسم.", "رفعالدعوى أمام القسم.")["agrees"])
        self.assertFalse(self.check("رفعالدعوى أمام القسم.", "رفع الدعوى أمام القسم.")["agrees"])

    def test_lone_letter_in_native_text_is_not_ocr_spacing(self):
        self.assertFalse(self.check("المادة 244 من ه ذا القانون.", "المادة 244 من هذا القانون.")["agrees"])

    def test_negation_spacing_merge_is_exact_letters(self):
        self.assertTrue(self.check("ما لم ينص القانون.", "مالم ينص القانون.")["agrees"])

    def test_second_stream_confirms_what_the_first_missed(self):
        self.assertTrue(self.check("الصلح، وذلك تحت طائلة.", "الصلح. وذلك تحت طائلة.", "الصلح، وذلك تحت طائلة.")["agrees"])

    def test_extra_word_in_every_stream_is_a_disagreement(self):
        result = self.check("يجب رفع الدعوى.", "يجب رفع الدعوى فورا.", "يجب رفع الدعوى فورا.")
        self.assertEqual("extra-in-all-streams", result["disagreements"][-1]["kind"])

    def test_junk_in_one_stream_only_is_ignored(self):
        self.assertTrue(self.check("يجب رفع الدعوى.", "يجب | رفع الدعوى.", "يجب رفع الدعوى.")["agrees"])

    def test_empty_ocr_never_agrees(self):
        self.assertFalse(self.check("يجب رفع الدعوى.", "")["agrees"])

    def test_dash_width_is_not_a_difference_but_a_missing_dash_is(self):
        self.assertTrue(self.check("1– الطعن أمام جهة.", "1 - الطعن أمام جهة.")["agrees"])
        self.assertFalse(self.check("1– الطعن أمام جهة.", "1 الطعن أمام جهة.")["agrees"])

    def test_label_tokens_are_skipped(self):
        canonical = "المادة 504 : يجب رفع الدعوى."
        self.assertTrue(self.check(canonical, "المادة 50 : يجب رفع الدعوى.", skip=len(nc.tokens("المادة 504 :")))["agrees"])


class Locate(unittest.TestCase):
    def test_locates_article_inside_longer_stream(self):
        stream = nc.tokens("المادة 503 : ترفع الدعوى بعريضة. المادة 504 : يجب رفع الدعوى أمام القسم الاجتماعي. المادة 505 : تحدد أول جلسة.")
        canonical = nc.tokens("المادة 504 : يجب رفع الدعوى أمام القسم الاجتماعي.")
        begin, end = nc.locate_span(canonical, stream)
        self.assertEqual(canonical, stream[begin:end])


class Layout(unittest.TestCase):
    def test_tsv_lines_are_read_top_to_bottom(self):
        header = "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext"
        rows = [header,
                "5\t1\t2\t1\t1\t1\t10\t300\t50\t20\t90\tثانيا",
                "5\t1\t1\t1\t1\t1\t10\t100\t50\t20\t90\tأولا"]
        lines = nc.tsv_lines("\n".join(rows))
        self.assertEqual(["أولا", "ثانيا"], [l["words"][0]["text"] for l in lines])

    def test_line_bands(self):
        image = np.full((100, 50), 255, dtype=np.uint8)
        image[10:20, 5:45] = 0
        image[40:55, 5:45] = 0
        self.assertEqual(2, len(nc.line_bands(image, min_gap=4, min_height=5, pad=0)))


if __name__ == "__main__":
    unittest.main()
