import unittest
from compare_ocr import body_alignment, ordered_lines


class ComparisonTests(unittest.TestCase):
    def test_rtl_words_grouped_despite_small_y_offsets(self):
        boxes = [[[0, 2], [9, 2], [9, 12], [0, 12]],
                 [[20, 0], [30, 0], [30, 10], [20, 10]],
                 [[0, 25], [9, 25], [9, 35], [0, 35]]]
        self.assertEqual(ordered_lines(boxes, ["left", "right", "next"]), ["right left", "next"])

    def test_crop_label_not_part_of_body(self):
        self.assertEqual(body_alignment("alpha beta", "Article 3: alpha beta trailing margin")["edits"], 0)

    def test_missing_final_line_is_penalized(self):
        self.assertGreater(body_alignment("alpha beta gamma", "alpha beta")["edits"], 0)

    def test_negations_and_digits_and_punctuation_are_not_folded(self):
        result = body_alignment("لا 1975.", "1976")
        self.assertGreater(result["edits"], 0)
        self.assertIn("لا", result["criticalTokensMissing"])
        self.assertIn("1975", result["criticalTokensMissing"])

    def test_empty_prediction_cannot_pass(self):
        self.assertEqual(body_alignment("abcd", "")["characterErrorRate"], 1)


if __name__ == "__main__":
    unittest.main()
