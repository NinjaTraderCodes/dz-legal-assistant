import copy
import unittest

import civil_priority_amendments as amendments


class CivilPriorityAmendmentTests(unittest.TestCase):
    def setUp(self):
        self.selected = {"467", "467 مكرر", "470", "489", "471"}
        self.operations = []
        self.bodies = []
        for instruction, action, number in [(2, "replace", "467"), (3, "insert", "467 مكرر"),
                                             (8, "repeal", "470"), (6, "replace-paragraphs", "489")]:
            self.operations.append({"id": "fictitious-" + number, "publicationId": "law-07-05",
                "sourceId": "A2007031", "instructionArticle": instruction, "operation": action,
                "numbers": [number], "pdfPages": [1],
                **({"replacedParagraphs": [1], "retainedParagraphs": [2]} if number == "489" else {})})
            paragraphs = [] if action == "repeal" else ["نص وهمي."] + (["لا يحذف هذا النص الوهمي."] if number == "489" else [])
            self.bodies.append({"number": number, "sourceStatusAnnotation": amendments.ANNOTATION[action],
                "text": "\n".join(paragraphs), "paragraphs": paragraphs,
                "compilationBodyKind": "repeal-marker" if action == "repeal" else "text",
                "separatedCompilationMetadata": [{"kind": "footnote-callout", "footnoteText": "(1) عدلت بالقانون رقم 05-07 نص وهمي."}]})
        self.bodies.append({"number": "471", "sourceStatusAnnotation": None})

    def apply(self):
        return amendments.reconcile(self.operations, self.bodies, self.selected)

    def test_matching_inventory_is_not_a_body_or_legal_applicability_certificate(self):
        before = copy.deepcopy(self.bodies)
        audit = self.apply()
        self.assertEqual(4, audit["explicitCurrentBodyTargets"])
        self.assertEqual(1, audit["untargetedSelectedIdentities"])
        self.assertTrue(audit["bodyCollationPending"])
        self.assertFalse(audit["fullArticleChecked"])
        self.assertEqual(before, self.bodies)

    def test_partial_489_cannot_be_promoted_to_whole_replacement_or_drop_remainder(self):
        op = self.operations[-1]
        op["operation"] = "replace"
        with self.assertRaisesRegex(ValueError, "replaced as a whole"):
            self.apply()
        op["operation"] = "replace-paragraphs"
        op["retainedParagraphs"] = []
        with self.assertRaisesRegex(ValueError, "preserve its source-read"):
            self.apply()
        op["retainedParagraphs"] = [2]
        self.bodies[-2]["paragraphs"].pop()
        with self.assertRaisesRegex(ValueError, "second489paragraph"):
            self.apply()

    def test_duplicate_targets_and_out_of_scope_local_numbers_are_rejected(self):
        self.operations[0]["numbers"].append("467")
        with self.assertRaisesRegex(ValueError, "duplicated or outside"):
            self.apply()
        self.operations[0]["numbers"] = ["1"]
        with self.assertRaisesRegex(ValueError, "duplicated or outside"):
            self.apply()

    def test_missing_or_misclassified_annotation_is_not_silently_ignored(self):
        self.bodies[0]["sourceStatusAnnotation"] = None
        with self.assertRaisesRegex(ValueError, "annotations disagree"):
            self.apply()
        self.bodies[0]["sourceStatusAnnotation"] = "(ملغاة)"
        with self.assertRaisesRegex(ValueError, "status label"):
            self.apply()

    def test_repeal_cannot_destroy_a_body_and_unknown_publication_cannot_supply_a_clause(self):
        self.bodies[2]["text"] = "نص وهمي لا يحذف."
        with self.assertRaisesRegex(ValueError, "cannot substitute"):
            self.apply()
        self.bodies[2]["text"] = ""
        self.operations[0]["sourceId"] = "A1988018"
        with self.assertRaisesRegex(ValueError, "another official publication"):
            self.apply()

    def test_citation_to_another_amendment_cannot_confirm_the_inventory(self):
        self.bodies[0]["separatedCompilationMetadata"][0]["footnoteText"] = "(1) بالقانون رقم 10-05 نص وهمي."
        with self.assertRaisesRegex(ValueError, "another amending publication"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
