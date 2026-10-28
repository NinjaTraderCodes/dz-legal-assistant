import copy
import tempfile
import unittest
from pathlib import Path

import apply_family_jo_collation as review
import pipeline


class OriginalJoCollationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "proof.png").write_bytes(b"fictitious test proof")
        self.body = {"number": "1", "text": "test old word", "paragraphs": ["test old word"]}
        self.audit = {"results": [{"number": "1", "agrees": False, "regions": [{"evidenceId": "test"}]}]}
        self.decision = {"number": "1", "outcome": "wording-differs", "note": "Explicit test image reading",
                         "candidateTextSha256": pipeline.hash_text(self.body["text"]),
                         "viewTextSha256": pipeline.hash_text(self.body["text"]),
                         "images": ["proof.png"], "imageSha256s": [pipeline.hash_file(self.root / "proof.png")],
                         "regions": self.audit["results"][0]["regions"]}
        self.record = {"sourceId": "A1984024", "sourceSha256": review.SOURCE_SHA, "decisions": [self.decision]}
        self.fixes = {"corrections": [{"number": "1", "from": "old", "to": "new", "reason": "Test fix"}]}
        self.conflicts = {"sourceId": "A1984024", "sourceSha256": review.SOURCE_SHA, "conflicts": []}

    def apply(self):
        return review.apply([self.body], [copy.deepcopy(self.body)], self.audit, self.record,
                            self.fixes, self.conflicts, self.root)[0]

    def test_correction_binds_final_text_without_rewriting_ministry_check(self):
        result = self.apply()
        self.assertEqual("test new word", result["text"])
        self.assertEqual(pipeline.hash_text(result["text"]), result["joCheck"]["textSha256"])
        self.assertEqual("test old word", self.body["text"])
        self.assertNotIn("verification", result)

    def test_source_conflict_retains_candidate_and_never_passes(self):
        self.fixes["corrections"] = []
        self.conflicts["conflicts"] = [{"number": "1", "ministryQuote": "old", "joQuote": "different", "reason": "Disputed"}]
        result = self.apply()
        self.assertEqual(self.body["text"], result["text"])
        self.assertEqual("unresolved-source-conflict", result["joCheck"]["result"])

    def test_missing_image_decision_cannot_pass(self):
        self.record["decisions"] = []
        with self.assertRaisesRegex(ValueError, "Missing or out-of-scope"):
            self.apply()

    def test_changed_proof_or_body_invalidates_reading(self):
        self.decision["candidateTextSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "Stale"):
            self.apply()
        self.decision["candidateTextSha256"] = pipeline.hash_text(self.body["text"])
        (self.root / "proof.png").write_bytes(b"changed proof")
        with self.assertRaisesRegex(ValueError, "Changed or unsafe"):
            self.apply()

    def test_fix_guard_must_match_exactly_once(self):
        self.fixes["corrections"][0]["from"] = "missing"
        with self.assertRaisesRegex(ValueError, "uniquely match"):
            self.apply()

    def test_difference_cannot_be_waived_or_both_fixed_and_disputed(self):
        self.fixes["corrections"] = []
        with self.assertRaisesRegex(ValueError, "exactly one"):
            self.apply()
        self.fixes["corrections"] = [{"number": "1", "from": "old", "to": "new", "reason": "test"}]
        self.conflicts["conflicts"] = [{"number": "1"}]
        with self.assertRaisesRegex(ValueError, "exactly one"):
            self.apply()

    def test_wrong_source_duplicate_and_incomplete_crop_are_rejected(self):
        self.record["sourceSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "Wrong legislative"):
            self.apply()
        self.record["sourceSha256"] = review.SOURCE_SHA
        self.record["decisions"].append(self.decision)
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            self.apply()
        self.record["decisions"].pop()
        self.decision["outcome"] = "needs-full-page"
        self.fixes["corrections"] = []
        with self.assertRaisesRegex(ValueError, "Unresolved"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
