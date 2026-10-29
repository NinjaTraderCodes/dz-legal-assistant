import unittest

import civil_flag_spacing as spacing
import native_check as nc


class CivilFlagSpacingTests(unittest.TestCase):
    def body(self, text):
        return {"printedLabel": "المادة 900 :", "text": text, "paragraphs": text.split("\n")}

    def at(self, text, phrase):
        return len(nc.tokens("المادة 900 : " + text[:text.index(phrase)]))

    def test_repeated_substring_is_bound_to_its_own_complete_tokens(self):
        text = "نص وهمي: تسليم المبيع، م ا لم يوجد اتفاق."
        correction = {"old": "م ا", "new": "ما", "at": self.at(text, "م ا لم"), "sourceReadJoinedNegation": True}
        result = spacing.project(self.body(text), [correction])
        self.assertEqual("نص وهمي: تسليم المبيع، ما لم يوجد اتفاق.", result["text"])
        correction["at"] -= 1
        with self.assertRaisesRegex(ValueError, "token position"):
            spacing.project(self.body(text), [correction])

    def test_multiword_projection_keeps_true_word_boundaries(self):
        text = "نص وهمي ال مؤجرة جزئيا، فقط."
        result = spacing.project(self.body(text), [{"old": "ال مؤجرة جزئيا،", "new": "المؤجرة جزئيا،",
                                                    "at": self.at(text, "ال مؤجرة")}])
        self.assertEqual("نص وهمي المؤجرة جزئيا، فقط.", result["text"])

    def test_multiple_original_token_positions_are_applied_without_index_drift(self):
        text = "نص وهمي ا لعمل وإ ليه فقط."
        changes = [{"old": "ا لعمل", "new": "العمل", "at": self.at(text, "ا لعمل")},
                   {"old": "وإ ليه", "new": "وإليه", "at": self.at(text, "وإ ليه")}]
        self.assertEqual("نص وهمي العمل وإليه فقط.", spacing.project(self.body(text), changes)["text"])

    def test_digits_letters_and_relocated_spaces_are_rejected(self):
        for old, new in [("1 9", "19"), ("ب ا", "بأ"), ("م ا لم", "ما ل م")]:
            with self.subTest(old=old), self.assertRaisesRegex(ValueError, "only delete spaces"):
                spacing.project(self.body(old), [{"old": old, "new": new, "at": 3}])

    def test_independent_negation_cannot_disappear_into_an_ordinary_word(self):
        text = "نص وهمي لا ضرر."
        with self.assertRaisesRegex(ValueError, "Independent negation"):
            spacing.project(self.body(text), [{"old": "لا ضرر", "new": "لاضرر", "at": 5}])

    def test_split_negation_needs_an_explicit_source_read_marker(self):
        text = "نص وهمي إ لا ضرر."
        change = {"old": "إ لا", "new": "إلا", "at": self.at(text, "إ لا")}
        with self.assertRaisesRegex(ValueError, "own source-read marker"):
            spacing.project(self.body(text), [change])
        self.assertIn("إلا", spacing.project(self.body(text), [change | {"sourceReadJoinedNegation": True}])["text"])

    def test_overlapping_projection_cannot_delete_twice(self):
        text = "نص وهمي ا لعمل."
        change = {"old": "ا لعمل", "new": "العمل", "at": self.at(text, "ا لعمل")}
        with self.assertRaisesRegex(ValueError, "overlapping"):
            spacing.project(self.body(text), [change, change])

    def test_projection_cannot_cross_a_paragraph_boundary(self):
        with self.assertRaisesRegex(ValueError, "only delete spaces"):
            spacing.project(self.body("نص وهمي ا\nلعمل."), [{"old": "ا\nلعمل", "new": "العمل", "at": 5}])
