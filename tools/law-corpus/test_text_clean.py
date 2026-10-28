import unittest

import text_clean


class Paragraph(unittest.TestCase):
    def test_tatweel_and_harakat_go(self):
        self.assertEqual("لا يجوز التنفيذ الجبري إلا بسند تنفيذي.", text_clean.paragraph("لا يجوز التنفيذ الجبري إلا بسند تنفيـــذي."))
        self.assertEqual("إن", text_clean.paragraph("إنّ"))

    def test_space_before_comma_and_inside_parentheses(self):
        self.assertEqual("يتشكل القسم الاجتماعي، تحت", text_clean.paragraph("يتشكل القسم الاجتماعي ، تحت"))
        self.assertEqual("خمسة عشر (15) يوما", text_clean.paragraph("خمسة عشر ( 15 ) يوما"))

    def test_nothing_is_added(self):
        self.assertEqual("والسندات التنفيذية هي", text_clean.paragraph("والسنـدات التنفيذيـة هـي"))
        self.assertEqual("1 - أحكام المحاكم", text_clean.paragraph("1 - أحكام المحاكم"))


if __name__ == "__main__":
    unittest.main()
