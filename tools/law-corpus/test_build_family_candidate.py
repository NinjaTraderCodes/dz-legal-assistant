import unittest

from build_family_candidate import checked_family_locator, identity, references


class FamilyReferenceTests(unittest.TestCase):
    def test_identical_sentence_in_neighboring_act_cannot_supply_family_locator(self):
        result = {"number": "3 مكرر", "regions": [{"evidenceId": "neighbor"}], "ocr": {}}
        records = {"neighbor": ({"sourceId": "A2005015", "pdfPage": 18}, None)}
        with self.assertRaisesRegex(ValueError, "outside the independently read act"):
            checked_family_locator(result, "ordinance-05-02", records, "نص اختباري.", {})
        records["neighbor"][0]["pdfPage"] = 19
        self.assertEqual(result, checked_family_locator(result, "ordinance-05-02", records, "نص اختباري.", {}))

    def test_shared_number_is_not_mapped_to_the_wrong_code(self):
        internal, external = references(["طبقا للمادة 40 من القانون المدني.", "تطبق المادة 64 من قانون الحالة المدنية."])
        self.assertEqual([], internal)
        self.assertEqual(["civil-code:40", "civil-status:64"], [r["targetId"] for r in external])
        self.assertEqual([1, 2], [r["paragraph"] for r in external])

    def test_plural_parenthesized_and_bis_references_keep_their_quote(self):
        paragraphs = ["طبقا للمادتين (12 و13) من هذا القانون.", "أحكام المادتين 9 و9 مكرر من هذا القانون."]
        internal, external = references(paragraphs)
        self.assertEqual([], external)
        self.assertEqual(["family-code:12", "family-code:13", "family-code:9", "family-code:9-bis"],
                         [r["targetId"] for r in internal])
        self.assertTrue(all(r["quote"] in paragraphs[r["paragraph"] - 1] for r in internal))

    def test_unseparated_plural_numbers_and_secondary_bis_index(self):
        internal, _ = references(["أحكام المواد 78و79و80 والمادة 8 مكرر 1 أعلاه."])
        self.assertEqual(["family-code:78", "family-code:79", "family-code:80", "family-code:8-bis-1"],
                         [r["targetId"] for r in internal])

    def test_unnumbered_self_reference_does_not_invent_an_article(self):
        self.assertEqual(([], []), references(["تحدد كيفية تطبيق هذه المادة عن طريق التنظيم."]))
        self.assertEqual("family-code:8-bis-1", identity("8 مكرر 1"))


if __name__ == "__main__":
    unittest.main()
