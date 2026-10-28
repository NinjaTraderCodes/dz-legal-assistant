"""Preflight tests use fictitious protocol records; they do not train or download a model."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

from train_reader import preflight


class PreflightTests(unittest.TestCase):
    def test_review_integrity_and_separation_are_enforced(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            row = {"id": "fictional", "input": {"fingerprint": "test-hash"}, "output": {"snapshotFingerprint": "test-hash"}, "referenceInput": {"fingerprint": "test-hash"}, "referenceOutput": {key: [] for key in ["relations", "events", "requests", "documents", "coverage", "unresolved"]}}
            raw = (json.dumps(row)+"\n").encode()
            (root/"training.jsonl").write_bytes(raw)
            case = {"caseId": "fictional", "labelReview": "reviewed", "fictitious": True, "scenarioFamily": "fixture", "sourceDecision": "synthetic:fixture", "partition": "train"}
            manifest = {"schema": "dzlegal.evidence/1", "trainingSha256": hashlib.sha256(raw).hexdigest(), "cases": [case]}

            def write():
                (root/"manifest.json").write_text(json.dumps(manifest), encoding="utf-8")

            write()
            self.assertEqual(len(preflight(root)), 1)
            case["labelReview"] = "unreviewed"
            write()
            with self.assertRaises(ValueError):
                preflight(root)
            case["labelReview"] = "reviewed"
            manifest["cases"].append(dict(case, caseId="sealed", partition="release"))
            write()
            with self.assertRaises(ValueError):
                preflight(root)
            manifest["cases"] = [case]
            write()
            (root/"training.jsonl").write_bytes(raw+b" ")
            with self.assertRaises(ValueError):
                preflight(root)


if __name__ == "__main__":
    unittest.main()
