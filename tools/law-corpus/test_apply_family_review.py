import tempfile
import unittest
from pathlib import Path

import apply_family_review as review
import pipeline


class FamilyReviewGuards(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "proof.png").write_bytes(b"fictitious image")
        self.articles = [{"number": "8", "paragraphs": ["نص اختباري مختلف."], "text": "نص اختباري مختلف."}]
        self.audit = {"results": [{"number": "8", "disagreements": [{"kind": "unconfirmed", "at": 2, "canonical": "مختلف"}]}]}
        self.record = {"decisions": [{"id": "8#0", "number": "8", "kind": "unconfirmed", "at": 2, "canonical": "مختلف",
                                      "decision": "print-differs", "from": "مختلف", "to": "صحيح",
                                      "images": ["proof.png"], "imageSha256s": [pipeline.hash_file(self.root / "proof.png")]}]}

    def test_missing_word_decision_cannot_pass(self):
        with self.assertRaises(ValueError):
            review.apply(self.articles, self.audit, {"decisions": []}, self.root)

    def test_modified_proof_cannot_pass(self):
        (self.root / "proof.png").write_bytes(b"different fictitious image")
        with self.assertRaises(ValueError):
            review.apply(self.articles, self.audit, self.record, self.root)

    def test_source_text_change_stops_correction(self):
        self.articles[0]["paragraphs"] = ["نص اختباري جديد."]
        with self.assertRaises(ValueError):
            review.apply(self.articles, self.audit, self.record, self.root)

    def test_corrected_text_is_bound_without_claiming_whole_code_check(self):
        out = review.apply(self.articles, self.audit, self.record, self.root)
        self.assertEqual("نص اختباري صحيح.", out[0]["text"])
        self.assertEqual(pipeline.hash_text(out[0]["text"]), out[0]["bodyCheck"]["textSha256"])
        self.assertNotIn("verification", out[0])


if __name__ == "__main__":
    unittest.main()
