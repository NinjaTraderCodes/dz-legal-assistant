import unittest

from collect_civil_native import page_labels
from prepare_civil import assemble
from make_civil_ministry_manifest import checked_region


def line(text, kind="start", page=1):
    return {"text": text, "kind": kind, "page": page}


class CivilPreparationTests(unittest.TestCase):
    def test_source_first_alias_does_not_rewrite_body(self):
        text = "المادة الاولى : نص اختباري باللفظ الاولى."
        label = page_labels(text)[0]
        self.assertEqual("1", label["canonicalNumberCandidate"])
        self.assertEqual(" نص اختباري باللفظ الاولى.", text[label["end"]:])

    def test_local_numbers_cannot_overwrite_general_provisions(self):
        result, _ = assemble([line("المادة 1 : نص اختباري عام."),
                              line("الفصل الأول مكرر", "heading"), line("عقد التسيير", "heading"),
                              line("المادة الاولى : نص اختباري محلي."),
                              line("الفصل الثاني", "heading"), line("موضوع اختباري", "heading"),
                              line("المادة 571 : نص اختباري لاحق.")])
        self.assertEqual(["main:1", "management-contract:1", "main:571"], [a["candidateKey"] for a in result])
        self.assertNotIn("عقد التسيير", result[0]["text"])
        self.assertNotIn("الفصل الثاني", result[1]["text"])

    def test_duplicate_local_numbers_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "Duplicate qualified"):
            assemble([line("المادة 1 : نص اختباري."), line("المادة 1 : نص اختباري آخر.")])

    def test_signature_excluded_and_page_continuation_preserved(self):
        result, notes = assemble([line("المادة 1003 : نص اختباري."),
                                  line("وتتمة اختباريّة.", "continuation", 2),
                                  line("وحرر بالجزائر في تاريخ اختباري."), line("توقيع اختباري.")])
        self.assertEqual([1, 2], result[0]["pages"])
        self.assertIn("وتتمة", result[0]["text"])
        self.assertNotIn("حرر", result[0]["text"])
        self.assertEqual(2, len(notes))

    def test_body_crop_cannot_hide_a_line_below_the_footnote_divider(self):
        self.assertLess(checked_region(600, 840, 200, [[90,300,510,320]])[3], .94)
        with self.assertRaisesRegex(ValueError, "clip a prepared body line"):
            checked_region(600, 840, 200, [[90,190,510,210]])


if __name__ == "__main__":
    unittest.main()
