import copy
import tempfile
import unittest
from pathlib import Path

import civil_priority_layout as layout
import pipeline


class CivilPriorityLayoutTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.image = self.root / "fictitious-proof.txt"
        self.image.write_text("Invented source image fixture", encoding="utf-8")
        self.body = {"candidateKey": "main:999", "number": "999", "pages": [1],
                     "text": "(معدلة) نص وهمي بعد ستة (6) أشهر لا يسقط. (1)",
                     "paragraphs": ["(معدلة) نص وهمي بعد ستة (6) أشهر لا يسقط. (1)"]}
        self.notes = [{"page": 1, "decodedText": "(1) عدلت بالقانون رقم اختباري."}]
        self.reading = {"candidateKey": "main:999", "inputTextSha256": pipeline.hash_text(self.body["text"]),
            "inspection": "actually read; not human/lawyer verification", "note": "Fictitious review fixture",
            "proofs": [{"pdfPage": 1, "image": self.image.name, "sha256": pipeline.hash_file(self.image)}],
            "excludedFollowingHeading": None, "restoredDividerLine": None, "sourceStatusAnnotation": "(معدلة)",
            "trailingFootnoteCallout": {"text": "(1)", "pdfPage": 1, "footnoteText": self.notes[0]["decodedText"]},
            "paragraphSplitBefore": None, "printedBodyParagraphs": 1, "retainedParenthesizedLegalNumbers": ["(6)"]}

    def apply(self):
        return layout.project(self.body, self.reading, self.notes, layout.SOURCE_SHA, self.root)

    def body_text(self, text):
        self.body.update(text=text, paragraphs=[text])
        self.reading["inputTextSha256"] = pipeline.hash_text(text)

    def test_separates_only_editorial_markers_and_preserves_legal_numbers_and_negation(self):
        before = copy.deepcopy(self.body)
        result = self.apply()
        self.assertEqual("نص وهمي بعد ستة (6) أشهر لا يسقط.", result["text"])
        self.assertEqual(before, self.body)
        self.assertFalse(result["amendmentVerified"])
        self.assertFalse(result["fullArticleChecked"])

    def test_internal_quantity_cannot_be_removed_as_a_trailing_callout(self):
        self.reading["trailingFootnoteCallout"].update(text="(6)", footnoteText="(6) نص وهمي.")
        self.notes.append({"page": 1, "decodedText": "(6) نص وهمي."})
        with self.assertRaisesRegex(ValueError, "exact trailing position"):
            self.apply()

    def test_repeal_label_cannot_hide_an_operative_body(self):
        self.body_text("(ملغاة) نص وهمي لا يحذف. (1)")
        self.reading["sourceStatusAnnotation"] = "(ملغاة)"
        with self.assertRaisesRegex(ValueError, "cannot discard"):
            self.apply()

    def test_empty_compilation_repeal_marker_does_not_certify_legal_repeal(self):
        self.body_text("(ملغاة) (1)")
        self.reading.update(sourceStatusAnnotation="(ملغاة)", printedBodyParagraphs=0,
                            retainedParenthesizedLegalNumbers=[])
        result = self.apply()
        self.assertEqual([], result["paragraphs"])
        self.assertEqual("repeal-marker", result["compilationBodyKind"])
        self.assertFalse(result["amendmentVerified"])

    def test_arbitrary_heading_or_negation_cannot_be_excluded(self):
        self.reading["excludedFollowingHeading"] = "لا يسقط."
        with self.assertRaisesRegex(ValueError, "trailing95 heading"):
            self.apply()

    def test_stale_body_wrong_source_and_missing_actual_review_are_rejected(self):
        self.body["text"] += " لا"
        with self.assertRaisesRegex(ValueError, "another source/body"):
            self.apply()
        self.setUp()
        with self.assertRaisesRegex(ValueError, "another source/body"):
            layout.project(self.body, self.reading, self.notes, "b" * 64, self.root)
        self.reading["inspection"] = "generated only"
        with self.assertRaisesRegex(ValueError, "actual source reading"):
            self.apply()

    def test_every_page_needs_unchanged_unique_source_proof(self):
        self.body["pages"].append(2)
        with self.assertRaisesRegex(ValueError, "every article page"):
            self.apply()
        self.body["pages"] = [1]
        self.reading["proofs"] *= 2
        with self.assertRaisesRegex(ValueError, "every article page"):
            self.apply()
        self.reading["proofs"] = self.reading["proofs"][:1]
        self.image.write_text("Altered source fixture", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "image proof changed"):
            self.apply()

    def test_source_footnote_and_legal_number_inventory_cannot_be_guessed(self):
        self.notes = []
        with self.assertRaisesRegex(ValueError, "source note"):
            self.apply()
        self.setUp()
        self.reading["retainedParenthesizedLegalNumbers"] = []
        with self.assertRaisesRegex(ValueError, "legal number"):
            self.apply()

    def test_exact_489_split_preserves_every_word_and_only_changes_paragraph_boundary(self):
        self.body.update(candidateKey="main:489", number="489")
        self.body_text("(معدلة) نص وهمي على المؤجر. " + layout.SPLIT_BEFORE + " لا يحذف. (1)")
        self.reading.update(candidateKey="main:489", paragraphSplitBefore=layout.SPLIT_BEFORE,
                            printedBodyParagraphs=2, retainedParenthesizedLegalNumbers=[])
        result = self.apply()
        self.assertEqual(["نص وهمي على المؤجر.", layout.SPLIT_BEFORE + " لا يحذف."], result["paragraphs"])
        self.body["candidateKey"] = self.reading["candidateKey"] = "main:490"
        with self.assertRaisesRegex(ValueError, "source-read489"):
            self.apply()

    def test_compensation_line_restoration_needs_exact_divider_and_source_article(self):
        self.body.update(candidateKey="main:140 مكرر 1", number="140 مكرر 1", pages=[32])
        self.body_text("(جديدة) نص وهمي تتكفل الدولة")
        self.notes = [{"page": 32, "decodedText": "____________ " + layout.RESTORED_LINE},
                      {"page": 32, "decodedText": "(4) أضيفت بالقانون رقم اختباري."}]
        self.reading.update(candidateKey=self.body["candidateKey"], sourceStatusAnnotation="(جديدة)",
            restoredDividerLine=layout.RESTORED_LINE, retainedParenthesizedLegalNumbers=[],
            trailingFootnoteCallout={"text": "(4)", "pdfPage": 32, "footnoteText": self.notes[1]["decodedText"]})
        self.reading["proofs"][0]["pdfPage"] = 32
        self.assertEqual("نص وهمي تتكفل الدولة بالتعويض عن هذا الضرر.", self.apply()["text"])
        self.notes.pop(0)
        with self.assertRaisesRegex(ValueError, "merged with the divider"):
            self.apply()

    def test_annotation_absence_and_paragraph_count_cannot_silently_change(self):
        self.reading["sourceStatusAnnotation"] = None
        with self.assertRaisesRegex(ValueError, "annotation partition"):
            self.apply()
        self.reading["sourceStatusAnnotation"] = "(معدلة)"
        self.reading["printedBodyParagraphs"] = 2
        with self.assertRaisesRegex(ValueError, "paragraph inventory"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
