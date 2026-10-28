import tempfile
import unittest
from pathlib import Path

import pipeline
from apply_family_amended_structure import replay


class AmendedStructureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "proof.png").write_bytes(b"fictitious proof")
        self.article = {"number":"1", "text":"test one\ntest two", "paragraphs":["test one","test two"]}
        self.decision = {"number":"1", "outcome":"paragraphs-and-body-boundaries-match", "note":"Actual test reading",
                         "paragraphCount":2, "textSha256":pipeline.hash_text(self.article["text"]),
                         "images":["proof.png"], "imageSha256s":[pipeline.hash_file(self.root / "proof.png")]}

    def test_missing_or_duplicate_reading_cannot_complete(self):
        for decisions in [[],[self.decision,self.decision]]:
            with self.assertRaisesRegex(ValueError,"Missing, duplicate"):
                replay([self.article],{"decisions":decisions},self.root)

    def test_paragraph_reflow_invalidates_otherwise_identical_words(self):
        self.article.update(text="test one test two",paragraphs=["test one test two"])
        with self.assertRaisesRegex(ValueError,"stale amended paragraph"):
            replay([self.article],{"decisions":[self.decision]},self.root)

    def test_mutated_source_proof_invalidates_decision(self):
        replay([self.article],{"decisions":[self.decision]},self.root)
        (self.root / "proof.png").write_bytes(b"changed test proof")
        with self.assertRaisesRegex(ValueError,"source image changed"):
            replay([self.article],{"decisions":[self.decision]},self.root)


if __name__ == "__main__":
    unittest.main()
