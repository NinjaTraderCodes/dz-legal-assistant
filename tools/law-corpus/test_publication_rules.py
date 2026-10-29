import unittest

from publication_rules import after_complete_day


class PublicationRuleTests(unittest.TestCase):
    def test_publication_day_is_not_substituted_for_a_complete_intervening_day(self):
        self.assertEqual("1984-06-14", after_complete_day("1984-06-12", location="algiers"))
        self.assertEqual("2005-03-01", after_complete_day("2005-02-27", location="algiers"))

    def test_other_daira_needs_its_own_receipt_evidence(self):
        self.assertIsNone(after_complete_day("2005-02-27", location="other-daira"))
        self.assertEqual("2005-03-05", after_complete_day("2005-02-27", location="other-daira", local_receipt="2005-03-03"))

    def test_no_implicit_nationwide_or_mixed_geographical_rule(self):
        with self.assertRaisesRegex(ValueError, "no nationwide"):
            after_complete_day("2005-02-27", location="nationwide")
        with self.assertRaisesRegex(ValueError, "not a supplied"):
            after_complete_day("2005-02-27", location="algiers", local_receipt="2005-03-03")

    def test_receipt_before_publication_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "precede"):
            after_complete_day("2005-02-27", location="other-daira", local_receipt="2005-02-26")

    def test_leap_day_and_year_boundary_do_not_lose_the_complete_day(self):
        self.assertEqual("2024-03-01", after_complete_day("2024-02-28", location="algiers"))
        self.assertEqual("2025-01-02", after_complete_day("2024-12-31", location="algiers"))
