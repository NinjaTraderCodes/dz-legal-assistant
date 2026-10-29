import unittest

import prepare_family as family


def line(text, kind="start", page=1):
    return {"text": text, "kind": kind, "page": page}


class FamilyBoundaries(unittest.TestCase):
    def test_index_after_colon_is_identity_not_body(self):
        articles, _ = family.assemble([
            line("المادة 8 مكرر: نص اختباري."),
            line("المادة 8 مكرر :1 (الأمر رقم 02-05) لا يجوز إجراء اختباري."),
        ])
        self.assertEqual(["8 مكرر", "8 مكرر 1"], [a["number"] for a in articles])
        self.assertEqual("لا يجوز إجراء اختباري.", articles[1]["text"])

    def test_stretched_source_label_retains_number(self):
        self.assertEqual("12", family.article_label("المѧѧادة 12: نص اختباري.")["canonicalNumberCandidate"])

    def test_prose_reference_is_not_an_article(self):
        articles, _ = family.assemble([
            line("المادة 10: نص اختباري."),
            line("المادة 11 أعلاه لا تجيز الإجراء الاختباري."),
        ])
        self.assertEqual(1, len(articles))
        self.assertIn("لا تجيز", articles[0]["text"])

    def test_signature_is_evidence_outside_final_article(self):
        articles, notes = family.assemble([
            line("المادة 224: ينشر هذا النص الاختباري."),
            line("حرر بالجزائر في تاريخ اختباري."),
            line("اسم توقيع اختباري", "heading"),
        ])
        self.assertEqual("ينشر هذا النص الاختباري.", articles[0]["text"])
        self.assertEqual(2, len(notes))

    def test_duplicate_label_cannot_pass_inventory(self):
        with self.assertRaises(ValueError):
            family.validate_inventory([{"number": "8 مكرر"}, {"number": "8 مكرر"}], ["8 مكرر", "8 مكرر 1"])

    def test_flush_right_chapter_is_not_repealed_article_text(self):
        articles, _ = family.assemble([
            line("المادة 39: (ملغاة بالأمر رقم 02-05)."),
            line("الفصل الخامس", "continuation"),
            line("النسب", "heading"),
            line("المادة 40: نص اختباري."),
        ])
        self.assertEqual("", articles[0]["text"])
        self.assertEqual(["الفصل الخامس - النسب"], articles[1]["path"])

    def test_unnumbered_sections_replace_siblings(self):
        articles, _ = family.assemble([
            line("الفصل الثاني", "heading"),
            line("آثار الطلاق", "heading"),
            line("العدة", "heading"),
            line("المادة 58: نص اختباري."),
            line("الحضانة", "heading"),
            line("المادة 62: نص اختباري آخر."),
        ])
        self.assertEqual(["الفصل الثاني - آثار الطلاق", "العدة"], articles[0]["path"])
        self.assertEqual(["الفصل الثاني - آثار الطلاق", "الحضانة"], articles[1]["path"])

    def test_footnoted_chapter_is_not_discarded_with_publisher_note(self):
        articles, notes = family.assemble([
            line("الفصل الثاني", "heading"), line("موضوع اختباري سابق", "heading"),
            line("المادة 73: نص اختباري سابق."),
            line("* الفصل الثالث", "continuation"), line("النفقة", "heading"),
            line("المادة 74: نص اختباري لاحق."),
            line("* ملاحظة ناشر اختبارية.", "continuation"),
        ])
        self.assertEqual(["الفصل الثالث - النفقة"], articles[1]["path"])
        self.assertEqual("نص اختباري لاحق.", articles[1]["text"])
        self.assertEqual(2, len(notes))


if __name__ == "__main__":
    unittest.main()
