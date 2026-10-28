import tempfile
import unittest
from pathlib import Path

import apply_family_ministry_review as review
import pipeline


class WholeArticleReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "source.png").write_bytes(b"test proof")
        self.article = {"number": "178", "text": "one\ntwo", "paragraphs": ["one", "two"],
                        "amendedBy0502": False, "repealedBy0502": False}
        self.audit = {"results": [{"number": "178", "wordAgrees": False,
                                   "disagreements": [{"punctuationOnly": False, "canonical": "two"}]}]}
        self.decision = {"number": "178", "textSha256": pipeline.hash_text("one\ntwo"),
                         "decision": "paragraph-break-differs", "joinAfter": 1,
                         "images": ["source.png"], "imageSha256s": [pipeline.hash_file(self.root / "source.png")]}
        self.structure = {"paragraphCountDifferences": [{"number": "178"}]}

    def apply(self, decisions=None):
        return review.apply([self.article], self.audit, {"decisions": decisions if decisions is not None else [self.decision]},
                            self.structure, self.root)[0]

    def test_page_break_join_preserves_all_words_and_final_hash(self):
        result = self.apply()
        self.assertEqual(["one two"], result["paragraphs"])
        self.assertEqual(pipeline.hash_text("one two"), result["bodyCheck"]["textSha256"])
        self.assertEqual("one\ntwo", self.article["text"])

    def test_missing_word_or_structural_review_cannot_pass(self):
        with self.assertRaisesRegex(ValueError, "still needs image review"):
            self.apply([])
        self.audit["results"][0].update(wordAgrees=True, disagreements=[])
        with self.assertRaisesRegex(ValueError, "still needs image review"):
            self.apply([])

    def test_changed_body_or_proof_cannot_reuse_a_decision(self):
        self.article["text"] = "different"
        with self.assertRaisesRegex(ValueError, "different body"):
            self.apply()
        self.article["text"] = "one\ntwo"
        (self.root / "source.png").write_bytes(b"changed proof")
        with self.assertRaisesRegex(ValueError, "image changed"):
            self.apply()

    def test_punctuation_only_disagreement_is_disclosed_without_invented_image_review(self):
        mark = {"punctuationOnly": True, "canonical": ":"}
        self.audit["results"][0].update(wordAgrees=True, disagreements=[mark])
        self.structure["paragraphCountDifferences"] = []
        result = self.apply([])
        self.assertEqual([mark], result["bodyCheck"]["unconfirmedPunctuation"])
        self.assertEqual("strict-ocr-word-agreement", result["bodyCheck"]["result"])

    def test_contradictory_acceptance_and_amended_scope_are_rejected(self):
        self.audit["results"][0]["wordAgrees"] = True
        with self.assertRaisesRegex(ValueError, "contradicts"):
            self.apply()
        self.article["amendedBy0502"] = True
        with self.assertRaisesRegex(ValueError, "outside unchanged scope"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
