import copy
import tempfile
import unittest
from pathlib import Path

import civil_priority_collation as collation
import pipeline


class CivilPriorityCollationTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.image = self.root / "fictitious-proof.txt"
        self.image.write_text("Invented image fixture", encoding="utf-8")
        self.pins = {s: "a" * 64 for s in ["A1975078", "A1984072", "A2007031"]}
        self.original = "نص وهمي مالم يثبت العكس."
        self.bodies = {n: {"number": n, "paragraphs": ["نص وهمي لا يحذف."], "text": "نص وهمي لا يحذف."}
                       for n in ["455", "456", "484", "489"]}
        self.bodies["489"].update(paragraphs=["نص وهمي لا يحذف.", "نص وهمي ما لم يثبت العكس."],
                                   text="نص وهمي لا يحذف.\nنص وهمي ما لم يثبت العكس.")
        self.layout = copy.deepcopy(self.bodies)
        self.ops = [{"id": "fictitious-" + n, "publicationId": pub, "sourceId": source,
                     "instructionArticle": article, "numbers": [n], "pdfPages": [page], "operation": action,
                     **({"replacedParagraphs": [1], "retainedParagraphs": [2]} if n == "489" else {})}
                    for n, pub, source, article, page, action in [
                        ("455", "law-84-21", "A1984072", 22, 8, "replace"),
                        ("456", "law-84-21", "A1984072", 23, 8, "replace"),
                        ("484", "law-07-05", "A2007031", 6, 5, "replace"),
                        ("489", "law-07-05", "A2007031", 6, 5, "replace-paragraphs")]]
        flag = {"kind": "unconfirmed", "at": 3, "canonical": ".", "punctuationOnly": True}
        self.queue = [{"sourceId": "A2007031", "number": "484", "flagIndex": 0, "flag": flag}]
        mark = {**self.queue[0], **self.reading("484", "A2007031", 5),
                "projectedTextSha256": pipeline.hash_text(self.bodies["484"]["text"]),
                "finding": "source-visible-mark-retained"}
        retained = {**self.reading("489", "A1975078", 31),
                    "oldCompilationParagraph": self.bodies["489"]["paragraphs"][1],
                    "originalParagraph": self.original, "originalParagraphSha256": pipeline.hash_text(self.original)}
        loans = [{**self.reading(n, "A1984072", 8), "sourceId": "A1984072", "instructionArticle": i,
                  "sourceTranscription": self.bodies[n]["text"], "printedBodyParagraphs": 1}
                 for n, i in [("455", 22), ("456", 23)]]
        self.review = {"punctuationDecisions": [mark], "loanBodyReadings": loans, "retained489Paragraph": retained}

    def reading(self, number, source, page):
        return {"number": number, "sourceId": source, "inputTextSha256": pipeline.hash_text(self.bodies[number]["text"]),
                "inspection": "actually read; not human/lawyer verification", "note": "Fictitious source reading",
                "proofs": [{"image": self.image.name, "imageSha256": pipeline.hash_file(self.image),
                            "sourceId": source, "sourceSha256": self.pins[source], "pdfPage": page}]}

    def apply(self):
        return collation.replay(self.review, self.bodies, self.layout, self.queue, self.ops,
                                self.pins, self.original, self.root)

    def test_retains_original_remainder_and_leaves_inputs_first_paragraph_and_marks_intact(self):
        before = copy.deepcopy(self.bodies)
        result = self.apply()
        self.assertEqual(before, self.bodies)
        self.assertEqual(before["489"]["paragraphs"][0], result["489"]["paragraphs"][0])
        self.assertEqual(self.original, result["489"]["paragraphs"][1])
        self.assertEqual(before["484"]["text"], result["484"]["text"])
        self.assertFalse(result["489"]["fullArticleChecked"])

    def test_remainder_cannot_be_missing_replaced_or_supplied_by_placeholder(self):
        for wrong in ["", "(... الباقي بدون تغيير ...)", "نص بديل لا أصل له."]:
            self.review["retained489Paragraph"]["originalParagraph"] = wrong
            with self.assertRaisesRegex(ValueError, "exact proven original"):
                self.apply()

    def test_complete_loan_body_and_exact_instruction_are_required(self):
        self.review["loanBodyReadings"][0]["sourceTranscription"] += " نص زائد."
        with self.assertRaisesRegex(ValueError, "Loan source"):
            self.apply()
        self.review["loanBodyReadings"][0]["sourceTranscription"] = self.bodies["455"]["text"]
        self.review["loanBodyReadings"][0]["instructionArticle"] = 23
        with self.assertRaisesRegex(ValueError, "Loan source"):
            self.apply()

    def test_punctuation_coverage_and_body_bindings_cannot_be_waived(self):
        mark = self.review["punctuationDecisions"].pop()
        with self.assertRaisesRegex(ValueError, "exactly one"):
            self.apply()
        self.review["punctuationDecisions"] = [mark]
        mark["projectedTextSha256"] = "b" * 64
        with self.assertRaisesRegex(ValueError, "Mark decision is stale"):
            self.apply()

    def test_changed_source_or_different_page_cannot_prove_retained_text(self):
        proof = self.review["retained489Paragraph"]["proofs"][0]
        proof["pdfPage"] = 32
        with self.assertRaisesRegex(ValueError, "another publication/page"):
            self.apply()
        proof["pdfPage"] = 31
        self.image.write_text("Changed fixture", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "source image changed"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
