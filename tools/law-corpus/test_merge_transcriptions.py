"""Invented version chains only."""
from copy import deepcopy
import unittest
import merge_transcriptions


class MergeTests(unittest.TestCase):
    def fixture(self):
        original = {"versionId": "fiction:1@original", "text": "old", "paragraphs": ["old"], "introducedBy": "original"}
        later = {"versionId": "fiction:1@later", "text": "new", "paragraphs": ["new"], "introducedBy": "later"}
        article = {"id": "fiction:1", "number": "1", "canonicalNumber": "1", "versions": [original, later]}
        spec = {"articles": [article], "inventoryReview": {"stale": True}}
        ledger = {"id": "fiction", "scope": {"complete": True, "targetArticles": ["1"]}, "publications": [
            {"id": "original", "articlesTouched": ["1"], "jo": {"date": "2000-01-01"}},
            {"id": "later", "articlesTouched": ["1"], "jo": {"date": "2001-01-01"}}]}
        batch = {"generatedOn": "2026-10-25", "articles": [deepcopy(article)]}
        batch["articles"][0]["versions"] = [deepcopy(original)]
        return spec, ledger, batch

    def test_partial_batch_retains_later_history_and_invalidates_inventory_certificate(self):
        spec, ledger, batch = self.fixture()
        result, updated = merge_transcriptions.merge(spec, ledger, batch)
        self.assertEqual(["old", "new"], [v["text"] for v in result["articles"][0]["versions"]])
        self.assertNotIn("inventoryReview", result)
        self.assertFalse(updated["scope"]["complete"])
        self.assertTrue(ledger["scope"]["complete"])

    def test_conflicting_old_transcription_is_not_an_implicit_correction(self):
        spec, ledger, batch = self.fixture()
        batch["articles"][0]["versions"][0].update(text="silent edit", paragraphs=["silent edit"])
        with self.assertRaisesRegex(ValueError, "overwrite"):
            merge_transcriptions.merge(spec, ledger, batch)

    def test_unknown_publication_or_wrong_paragraph_reconstruction_is_rejected(self):
        spec, ledger, batch = self.fixture()
        batch["articles"][0]["versions"][0]["introducedBy"] = "missing"
        with self.assertRaisesRegex(ValueError, "absent"):
            merge_transcriptions.merge(spec, ledger, batch)


if __name__ == '__main__':
    unittest.main()
