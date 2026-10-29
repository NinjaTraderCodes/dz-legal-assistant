import unittest

from review_items import file_tag


class ReviewFileIdentityTests(unittest.TestCase):
    def test_qualified_same_printed_number_cannot_share_proof_filename(self):
        self.assertNotEqual(file_tag("main:1"), file_tag("management-contract:1"))

    def test_qualified_tag_has_no_windows_reserved_delimiters(self):
        self.assertRegex(file_tag("management-contract:1"), r"^article-[a-f0-9]{20}$")

    def test_preexisting_numeric_filenames_are_retained(self):
        self.assertEqual("187_مكرر_1", file_tag("187 مكرر 1"))


if __name__ == "__main__":
    unittest.main()
