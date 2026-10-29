import copy
import tempfile
import unittest
from pathlib import Path

import apply_family_paths as review
import pipeline


class FamilyPathReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        proof = self.root / "proof.txt"
        proof.write_text("invented proof", encoding="utf-8")
        self.native = {"source": {"sha256": "a" * 64}, "articles": [
            {"number": "74", "path": ["Invented old chapter"]}, {"number": "75", "path": ["Invented old chapter"]}]}
        self.decisions = {"sourceSha256": "a" * 64, "decisions": [{"numbers": ["74", "75"],
            "previousPath": ["Invented old chapter"], "path": ["Invented corrected chapter"], "note": "Fictitious reading",
            "images": ["proof.txt"], "imageSha256s": [pipeline.hash_file(proof)]}]}

    def test_path_changes_cover_descendants_without_touching_bodies(self):
        before = copy.deepcopy(self.native)
        paths = review.apply(self.native, self.decisions, self.root)
        self.assertEqual(before, self.native)
        self.assertEqual(["Invented corrected chapter"], paths["75"])

    def test_missing_identity_cannot_pass_whole_path_review(self):
        self.decisions["decisions"][0]["numbers"] = ["74"]
        with self.assertRaisesRegex(ValueError, "every Family"):
            review.apply(self.native, self.decisions, self.root)

    def test_changed_proof_is_rejected(self):
        (self.root / "proof.txt").write_text("changed", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "proof changed"):
            review.apply(self.native, self.decisions, self.root)

    def test_stale_path_cannot_overwrite_another_review(self):
        self.native["articles"][0]["path"] = ["Another path"]
        with self.assertRaisesRegex(ValueError, "stale"):
            review.apply(self.native, self.decisions, self.root)


if __name__ == "__main__":
    unittest.main()
