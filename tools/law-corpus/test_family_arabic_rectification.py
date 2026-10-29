import copy
import unittest

import family_arabic_rectification as rect
import pipeline


class ArabicFamilyRectificationTests(unittest.TestCase):
    def setUp(self):
        self.review = pipeline.read_json(rect.WORK / "arabic-rectification-review.json")
        self.conflicts = pipeline.read_json(rect.WORK / "jo-collation-conflicts.json")
        self.inventory = pipeline.read_json(rect.WORK / "amendment-inventory.json")
        self.bodies = {a["number"]: a for a in pipeline.read_json(rect.WORK / "unchanged-jo-collated.json")
                       + pipeline.read_json(rect.WORK / "amended-corrected.json")}

    def apply(self):
        return rect.apply(self.bodies, self.review, self.conflicts, self.inventory)

    def test_explicit_arabic_corrections_resolve_four_conflicts_without_erasing_the_original_print(self):
        before = copy.deepcopy(self.bodies)
        final = self.apply()
        self.assertEqual(before, self.bodies)
        for number in rect.CONFLICTS:
            self.assertIsNone(final[number]["joCheck"]["conflict"])
            self.assertEqual(before[number]["joCheck"], final[number]["joCheck"]["priorCollation"])
            self.assertEqual(before[number]["text"], final[number]["text"])
        self.assertEqual(["167"], [n for n in final if final[n]["text"] != before[n]["text"]])
        self.assertIn(rect.ORDER_NEW, final["167"]["text"])

    def test_missing_or_duplicate_rectification_target_fails(self):
        self.review["currentOutcomes"].pop()
        with self.assertRaisesRegex(ValueError, "all17"):
            self.apply()
        self.setUp()
        self.review["currentOutcomes"].append(copy.deepcopy(self.review["currentOutcomes"][0]))
        with self.assertRaisesRegex(ValueError, "all17"):
            self.apply()

    def test_changed_current_body_is_rejected(self):
        self.bodies["135"]["text"] += " لا"
        with self.assertRaisesRegex(ValueError, "different current body"):
            self.apply()

    def test_arabic_locator_cannot_be_replaced_by_french_page9(self):
        self.review["currentOutcomes"][0]["rectificationPdfPage"] = 9
        with self.assertRaisesRegex(ValueError, "locator"):
            self.apply()

    def test_historical_correction_cannot_overwrite_a_2005_replacement(self):
        self.review["currentOutcomes"][0]["clauses"] = [{"currentQuote": "لا"}]
        with self.assertRaisesRegex(ValueError, "Deferred historical clause"):
            self.apply()

    def test_absent_corrected_clause_is_not_accepted_as_a_resolved_conflict(self):
        row = next(r for r in self.review["currentOutcomes"] if r["number"] == "135")
        row["clauses"][0]["currentQuote"] = "عمدا أو عدوانا"
        with self.assertRaisesRegex(ValueError, "absent or ambiguous"):
            self.apply()

    def test_unrelated_original_conflict_cannot_be_marked_resolved(self):
        self.bodies["151"]["joCheck"]["conflict"]["joQuote"] += " لا"
        with self.assertRaisesRegex(ValueError, "exact quarantined source conflict"):
            self.apply()

    def test_ministry_word_order_cannot_silently_override_the_printed_rectification(self):
        row = next(r for r in self.review["currentOutcomes"] if r["number"] == "167")
        row["clauses"][0]["correctedPrinted"] = rect.ORDER_OLD
        with self.assertRaisesRegex(ValueError, "Article167"):
            self.apply()

    def test_missing_actual_arabic_page_reading_is_rejected(self):
        self.review["imagesActuallyRead"] = [i for i in self.review["imagesActuallyRead"]
                                             if not i["image"].endswith("A1984031-p13.png")]
        with self.assertRaisesRegex(ValueError, "actual Arabic page"):
            self.apply()

    def test_french_translation_notice_cannot_supply_arabic_correction_authority(self):
        self.review["source"]["id"] = "F1984031"
        with self.assertRaisesRegex(ValueError, "not a French notice"):
            rect.replay(self.review)
