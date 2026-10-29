import copy
import tempfile
import unittest
from pathlib import Path

import apply_civil_word_review as review
import pipeline


class CivilWordRepairTests(unittest.TestCase):
    def candidate(self, text="لف ظ اختباري."):
        return {"candidateKey": "main:1", "number": "1", "numberScope": "main", "text": text, "paragraphs": [text]}

    def patch(self, old="لف ظ", new="لفظ"):
        return {"old": old, "new": new, "expectedOccurrences": 1}

    def test_repair_changes_only_the_derived_body(self):
        candidate = self.candidate()
        before = copy.deepcopy(candidate)
        result = review.repair(candidate, [self.patch()])
        self.assertEqual(before, candidate)
        self.assertEqual("لفظ اختباري.", result["text"])
        self.assertEqual(result["text"], "\n".join(result["paragraphs"]))

    def test_ambiguous_or_absent_source_word_fails(self):
        for text in ["نص اختباري.", "لف ظ لف ظ اختباري."]:
            with self.subTest(text=text), self.assertRaisesRegex(ValueError, "absent, ambiguous"):
                review.repair(self.candidate(text), [self.patch()])

    def test_digit_join_negation_join_and_letter_substitution_fail(self):
        for old,new in [("1 9", "19"), ("لا يجوز", "لايجوز"), ("اختبار", "اختيار")]:
            with self.subTest(old=old), self.assertRaisesRegex(ValueError, "allowed"):
                review.repair(self.candidate(old), [self.patch(old,new)])

    def test_repairs_cannot_join_across_paragraphs(self):
        candidate = self.candidate("لف\nظ اختباري.")
        candidate["paragraphs"] = ["لف", "ظ اختباري."]
        with self.assertRaises(ValueError):
            review.repair(candidate, [self.patch()])

    def fixture(self, root):
        article = self.candidate()
        proof = root / "proof.txt"
        proof.write_text("invented source proof", encoding="utf-8")
        native = {"source": {"sha256": "a"*64}, "articles": [article]}
        flag = {"kind":"unconfirmed", "at":3, "canonical":"لف ظ", "punctuationOnly":False}
        audit = {"results":[{"number":"main:1", "disagreements":[flag]}]}
        decision = {"itemId":"main:1#0", "candidateKey":"main:1", "kind":"unconfirmed", "at":3,
            "canonical":"لف ظ", "ocrExtra":None, "inputTextSha256":pipeline.hash_text(article["text"]),
            "images":["proof.txt"], "imageSha256s":[pipeline.hash_file(proof)], "note":"Fictitious source reading",
            "verdict":"native-spacing-correction", "correction":self.patch()}
        data = {"sourceSha256":"a"*64, "sourceOrderStart":0, "sourceOrderEnd":1, "decisions":[decision]}
        return native,audit,data

    def test_complete_proof_replays_without_changing_native(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            native,audit,data = self.fixture(root)
            self.assertEqual(1,review.apply(native,audit,data,root)["correctionsApplied"])

    def test_missing_and_stale_decisions_are_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            native,audit,data = self.fixture(root)
            data["decisions"][0]["inputTextSha256"] = "b"*64
            with self.assertRaisesRegex(ValueError,"no longer matches"):
                review.apply(native,audit,data,root)
            data["decisions"] = []
            with self.assertRaisesRegex(ValueError,"missing"):
                review.apply(native,audit,data,root)

    def test_changed_source_proof_is_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            native,audit,data = self.fixture(root)
            (root/"proof.txt").write_text("changed",encoding="utf-8")
            with self.assertRaisesRegex(ValueError,"proof changed"):
                review.apply(native,audit,data,root)

    def test_an_unconfirmed_word_cannot_be_dismissed_as_a_heading(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            native,audit,data = self.fixture(root)
            data["decisions"][0]["verdict"] = "outside-article-heading-or-page-label"
            data["decisions"][0]["correction"] = None
            with self.assertRaisesRegex(ValueError,"silently drop"):
                review.apply(native,audit,data,root)


if __name__ == "__main__":
    unittest.main()
