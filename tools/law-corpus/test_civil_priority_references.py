import unittest

import civil_priority_references as references


class CivilPriorityReferenceTests(unittest.TestCase):
    def test_bis_plural_and_source_paragraphs_are_preserved(self):
        result = references.extract(["نص وهمي وفقا للمادتين 12 و12 مكرر.", "يراجع المادة 15 مكرر 1."])
        self.assertEqual(["civil-code:12", "civil-code:12-bis", "civil-code:15-bis-1"], [r["targetId"] for r in result])
        self.assertEqual([1, 1, 2], [r["paragraph"] for r in result])
        self.assertEqual("للمادتين 12 و12 مكرر", result[0]["quote"])

    def test_bad_source_spacing_and_punctuation_are_parsed_without_rewriting_text(self):
        text = "مثال وهمي المادة.12 والمادة،13."
        self.assertEqual(["المادة.12", "المادة،13"], [r["quote"] for r in references.extract([text])])
        self.assertEqual("مثال وهمي المادة.12 والمادة،13.", text)

    def test_open_ended_anchor_does_not_invent_an_end(self):
        result = references.extract(["تطبق المادة 12 وما بعدها في هذا المثال الوهمي."])
        self.assertEqual(1, len(result))
        self.assertEqual("المادة 12 وما بعدها", result[0]["quote"])

    def test_material_nouns_and_anonymous_self_references_are_not_numeric_targets(self):
        self.assertEqual([], references.extract(["المادة المستخدمة في العمل. تطبق هذه المادة على المثال الوهمي."]))

    def test_explicit_foreign_namespace_cannot_be_assigned_to_civil(self):
        with self.assertRaisesRegex(ValueError, "foreign-law"):
            references.extract(["مثال وهمي تطبق المادة 12 من قانون الأسرة."])


if __name__ == "__main__":
    unittest.main()
