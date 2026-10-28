import unittest

import family_ministry_audit as audit
import family_literal
import native_check as nc


class MinistryBodyProjection(unittest.TestCase):
    def article(self):
        return {"number": "8", "rawParagraphs": ["(الأمر رقم 05-02) لا يجوز فعل اختباري."],
                "text": "لا يجوز فعل اختباري.", "paragraphs": ["لا يجوز فعل اختباري."]}

    def test_annotation_date_cannot_count_as_a_body_token(self):
        item = audit.input_for(self.article())
        full = nc.tokens(item["label"] + " : " + item["text"])
        body = [full[i] for i in item["bodyTokenIndices"]]
        self.assertEqual(nc.tokens(item["preparedBody"]), body)
        self.assertNotIn("05", body)

    def test_matching_annotation_cannot_hide_a_missing_negation(self):
        item = audit.input_for(self.article())
        check = {"results": [{"number": "8", "ocr": {"test": "المادة 8 : (الأمر رقم 05-02) يجوز فعل اختباري."},
                              "disagreements": []}]}
        result = audit.audit([item], check)
        self.assertFalse(result["results"][0]["agrees"])
        self.assertIn("لا", [d["canonical"] for d in result["results"][0]["disagreements"]])

    def test_an_annotation_rule_cannot_drop_unaccounted_body_words(self):
        article = self.article()
        article["text"] = "يجوز فعل اختباري."
        with self.assertRaises(ValueError):
            audit.input_for(article)

    def test_arabic_comma_is_disclosed_without_turning_into_a_word_error(self):
        words, marks = family_literal.compare(["اختبار،"], {"test": "اختبار"}, [0])
        self.assertEqual([], words)
        self.assertEqual(1, len(marks))

    def test_comma_attachment_never_hides_a_missing_negation(self):
        words, _ = family_literal.compare(["لا،", "يجوز"], {"test": "يجوز"}, [0, 1])
        self.assertTrue(any(d["canonical"] == "لا،" for d in words))

    def test_numeric_separator_is_not_deferred_as_ordinary_punctuation(self):
        words, _ = family_literal.compare(["3", ".", "50"], {"test": "3 50"}, [0, 1, 2])
        self.assertTrue(any(d["canonical"] == "." for d in words))


if __name__ == "__main__":
    unittest.main()
