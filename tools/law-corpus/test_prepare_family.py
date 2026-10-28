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


if __name__ == "__main__":
    unittest.main()
