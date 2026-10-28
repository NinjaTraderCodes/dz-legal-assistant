import unittest

import family_french_structure as french


class PrintedFrenchAliases(unittest.TestCase):
    def test_publisher_dot_before_bis_keeps_inserted_identity(self):
        self.assertEqual("53 مكرر", french.number("Art. 53. bis – Fictitious text.")[0])

    def test_suffix_one_is_not_collapsed_into_bis(self):
        self.assertEqual("8 مكرر 1", french.number("Art. 8 bis 1. – Fictitious text.")[0])

    def test_cross_reference_in_prose_is_not_a_label(self):
        self.assertIsNone(french.number("Selon Art. 53. bis – fictitious text."))


if __name__ == "__main__":
    unittest.main()
