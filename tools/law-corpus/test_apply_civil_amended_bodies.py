import copy
import tempfile
import unittest
from pathlib import Path

import apply_civil_amended_bodies as wording
import pipeline


class CivilAmendedBodyTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.image = self.root / "fictitious-proof.txt"
        self.image.write_text("Invented image fixture", encoding="utf-8")
        self.sources = {"A2007031": "a" * 64}
        text = "نص وهمي على المستأجر. يتعين عل المستأجر حفظ اثنتين (2) من الوثائق.\nلا يحذف هذا النص الوهمي."
        self.body = {"number": "484", "text": text, "paragraphs": text.split("\n"), "fullArticleChecked": False}
        self.proof = {"image": self.image.name, "imageSha256": pipeline.hash_file(self.image),
                      "sourceId": "A2007031", "sourceSha256": "a" * 64, "pdfPage": 5}
        self.flag = {"kind": "unconfirmed", "at": 9, "canonical": "عل", "punctuationOnly": False}
        self.reading = {"sourceId": "A2007031", "number": "484", "flagIndex": 0,
            "inputTextSha256": pipeline.hash_text(text), "inspection": wording.INSPECTION,
            "proofs": [self.proof], "flag": self.flag, "note": "Fictitious source review",
            "finding": "source-read-JO-wording-restoration-proposed"}
        self.proposal = {k: copy.deepcopy(v) for k, v in self.reading.items()
                         if k not in {"flagIndex", "flag", "note", "finding"}}
        self.proposal.update(diagnosticFlagIndex=0, flagCanonical="عل", old="يتعين عل المستأجر",
                             new="يتعين على المستأجر", status="source-read proposal; NOT applied")
        self.diagnostic = {"number": "484", "inputTextSha256": pipeline.hash_text(text),
            "expectedPageOnly": True, "ocr": {"tesseract": text, "ppocr": text},
            "instructionPdfPages": [5], "strictWordFlags": [self.flag]}

    def decisions(self):
        return wording.validate_flags([self.reading], {("A2007031", "484"): self.diagnostic},
                                       {"484": self.body}, self.sources, self.root)

    def apply(self):
        return wording.project(self.body, [self.proposal], self.decisions(), self.diagnostic, self.sources, self.root)

    def test_contextual_repair_preserves_other_occurrence_digits_negation_and_paragraphs(self):
        before = copy.deepcopy(self.body)
        result = self.apply()
        self.assertEqual(before, self.body)
        self.assertIn("نص وهمي على المستأجر.", result["text"])
        self.assertIn("يتعين على المستأجر", result["text"])
        self.assertIn("اثنتين (2)", result["text"])
        self.assertEqual("لا يحذف هذا النص الوهمي.", result["paragraphs"][1])
        self.assertFalse(result["fullArticleChecked"])

    def test_global_substring_replacement_or_legal_mutation_is_rejected(self):
        for old, new in [("عل", "على"), ("لا", ""), ("(2)", "(3)"),
                         ("يتعين عل المستأجر", "يتعين على المستأجر\nنص زائد")]:
            with self.subTest(old=old, new=new):
                self.proposal.update(old=old, new=new)
                with self.assertRaisesRegex(ValueError, "closed list"):
                    self.apply()

    def test_repeated_context_cannot_be_guessed_and_stale_body_is_rejected(self):
        self.body["paragraphs"][0] += " يتعين عل المستأجر"
        self.body["text"] = "\n".join(self.body["paragraphs"])
        with self.assertRaisesRegex(ValueError, "another body"):
            self.apply()
        sha = pipeline.hash_text(self.body["text"])
        self.reading["inputTextSha256"] = self.proposal["inputTextSha256"] = self.diagnostic["inputTextSha256"] = sha
        with self.assertRaisesRegex(ValueError, "unique context"):
            self.apply()

    def test_missing_duplicate_or_changed_flag_is_rejected(self):
        diagnostics = {("A2007031", "484"): self.diagnostic}
        for readings in [[], [self.reading, self.reading]]:
            with self.assertRaisesRegex(ValueError, "exactly one"):
                wording.validate_flags(readings, diagnostics, {"484": self.body}, self.sources, self.root)
        self.reading["flag"] = dict(self.flag, canonical="على")
        with self.assertRaisesRegex(ValueError, "stale"):
            self.apply()

    def test_proposal_cannot_switch_to_another_flag_or_an_ocr_retention(self):
        self.proposal["diagnosticFlagIndex"] = 1
        with self.assertRaisesRegex(ValueError, "exact source-read"):
            self.apply()
        self.proposal["diagnosticFlagIndex"] = 0
        self.reading["finding"] = "source-read-compilation-words-retained"
        with self.assertRaisesRegex(ValueError, "exact source-read"):
            self.apply()

    def test_actual_unchanged_source_proof_cannot_be_substituted(self):
        for key, value in [("sourceId", "A2005044"), ("sourceSha256", "b" * 64),
                           ("pdfPage", 6), ("image", "../outside.txt")]:
            with self.subTest(key=key):
                prior = self.proof[key]
                self.proof[key] = value
                with self.assertRaisesRegex(ValueError, "source proof"):
                    self.apply()
                self.proof[key] = prior
        self.image.write_text("Changed proof fixture", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "source proof"):
            self.apply()

    def test_unread_or_wrong_comparison_cannot_authorize_a_repair(self):
        self.reading["inspection"] = "generated only"
        with self.assertRaisesRegex(ValueError, "actual two-reader"):
            self.apply()
        self.reading["inspection"] = wording.INSPECTION
        self.diagnostic["inputTextSha256"] = "b" * 64
        with self.assertRaisesRegex(ValueError, "another compared body"):
            self.apply()
        self.diagnostic["inputTextSha256"] = pipeline.hash_text(self.body["text"])
        del self.diagnostic["ocr"]["ppocr"]
        with self.assertRaisesRegex(ValueError, "actual two-reader"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
