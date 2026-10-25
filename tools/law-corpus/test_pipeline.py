"""Offline tests use fictitious snippets and temporary evidence; no law from memory."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("law_corpus_pipeline", Path(__file__).with_name("pipeline.py"))
pipeline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pipeline)


class ConservativeComparisonTests(unittest.TestCase):
    def test_whitespace_and_canonical_composition_only(self):
        self.assertTrue(pipeline.compare("ألف\n  باء", "ا\u0654لف باء")["match"])

    def test_negation_cannot_disappear(self):
        self.assertFalse(pipeline.compare("لا يجوز", "يجوز")["match"])

    def test_digits_and_digit_scripts_remain_distinct(self):
        self.assertFalse(pipeline.compare("6", "5")["match"])
        self.assertFalse(pipeline.compare("6", "٦")["match"])

    def test_punctuation_remains_significant(self):
        self.assertFalse(pipeline.compare("ألف، باء", "ألف باء")["match"])

    def test_bad_legacy_fold_is_never_used(self):
        self.assertFalse(pipeline.compare("آ", "ك")["match"])

    def test_diacritics_remain_significant(self):
        self.assertFalse(pipeline.compare("عَلَم", "علم")["match"])

    def test_empty_outputs_are_not_a_pass(self):
        self.assertFalse(pipeline.compare("", "")["match"])


class OfficialSourceTests(unittest.TestCase):
    def test_https_exact_host_allowlist(self):
        self.assertEqual("https://www.joradp.dz/a.pdf", pipeline.official_url("https://www.joradp.dz/a.pdf"))
        for url in ("http://www.joradp.dz/a.pdf", "https://joradp.dz.evil.invalid/a.pdf",
                    "https://evil.invalid/joradp.dz/a.pdf", "https://user@joradp.dz/a.pdf",
                    "https://joradp.dz:444/a.pdf", "https://coursupreme.dz/a.pdf"):
            with self.subTest(url=url), self.assertRaises(ValueError):
                pipeline.official_url(url)

    def test_artifact_ids_cannot_escape_cache(self):
        for key in ("../article", "/absolute", "a/b", "a\\b", ""):
            with self.assertRaises(ValueError):
                pipeline.safe_id(key)

    def test_tampered_local_pdf_is_rejected_without_network(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            target = cache / "fixture.pdf"
            target.write_bytes(b"%PDF-fixture tampered")
            source = {"id": "fixture", "url": "https://www.joradp.dz/fixture.pdf",
                      "sha256": "0" * 64, "retrievedOn": "2026-10-25"}
            with self.assertRaisesRegex(ValueError, "hash mismatch"):
                pipeline.fetch_source(source, cache, True, None)
            self.assertEqual(b"%PDF-fixture tampered", target.read_bytes())

    def test_unknown_missing_pdf_is_not_filled_in(self):
        with tempfile.TemporaryDirectory() as temporary:
            source = {"id": "fixture", "url": "https://www.joradp.dz/fixture.pdf",
                      "sha256": "0" * 64, "retrievedOn": "2026-10-25"}
            with self.assertRaises(FileNotFoundError):
                pipeline.fetch_source(source, Path(temporary), True, None)


class CompositionTests(unittest.TestCase):
    def make_evidence(self, cache):
        folder = cache / "extractions" / ("a" * 64) / "page-0001" / "fixture"
        folder.mkdir(parents=True)
        for name, contents in (("text-layer.txt", "raw one"), ("ocr.txt", "raw two"), ("crop.png", "image fixture")):
            (folder / name).write_text(contents, encoding="utf-8")
        record = {"id": "fixture", "sourceId": "source", "sourceSha256": "a" * 64, "pdfPage": 1, "printedPage": "1",
                  "region": [0, 0, 1, 1], "artifacts": [
                      {"path": str(p.relative_to(cache)), "sha256": pipeline.hash_file(p)} for p in folder.iterdir()]}
        pipeline.write_json(folder / "evidence.json", record)
        ledger = {"id": "fixture-code", "scope": {"complete": False, "targetArticles": ["1"], "kind": "priority"},
                  "publications": [{"id": "pub", "arabic": {"sha256": "a" * 64}}]}
        version = {"versionId": "fixture-code:1:original", "text": "selected transcription", "paragraphs": ["selected transcription"],
                   "introducedBy": "pub", "evidenceIds": ["fixture"], "temporalStatus": "unsettled"}
        spec = {"generatedOn": "2026-10-25", "articles": [{"id": "fixture-code:1", "canonicalNumber": "1",
                                                         "number": "1", "versions": [version]}]}
        return spec, ledger, version, record

    def test_ocr_disagreement_without_image_review_stays_unsettled(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, _, _ = self.make_evidence(cache)
            report = pipeline.compose(spec, ledger, cache, cache / "out")
            self.assertEqual({"checked": 0, "partly-checked": 0, "unsettled": 1}, report["countsByState"])
            self.assertFalse(report["inventory"]["complete"])

    def test_review_requires_exact_image_and_text_hashes(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, version, _ = self.make_evidence(cache)
            version["imageReview"] = {"textSha256": "0" * 64, "imageSha256s": [], "reviewedOn": "2026-10-25",
                                       "decision": "transcription-resolved", "note": "Fictitious test"}
            with self.assertRaisesRegex(ValueError, "not bound"):
                pipeline.compose(spec, ledger, cache, cache / "out")

    def test_image_review_alone_never_claims_code_complete(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, version, record = self.make_evidence(cache)
            image_hash = next(a["sha256"] for a in record["artifacts"] if a["path"].endswith("crop.png"))
            version["imageReview"] = {"textSha256": pipeline.hash_text(version["text"]), "imageSha256s": [image_hash],
                                       "reviewedOn": "2026-10-25", "decision": "transcription-resolved", "note": "Fictitious test"}
            report = pipeline.compose(spec, ledger, cache, cache / "out")
            self.assertEqual({"checked": 0, "partly-checked": 1, "unsettled": 0}, report["countsByState"])
            self.assertFalse(report["inventory"]["complete"])
            built = pipeline.read_json(cache / "out/articles.json")["articles"][0]["versions"][0]
            check = next(c for c in built["verification"]["checks"] if c["check"] == "discrepancy-resolved-from-image")
            self.assertEqual(pipeline.hash_text(version["text"]), check["textSha256"])
            self.assertEqual([image_hash], check["imageSha256s"])

    def test_matching_stream_check_binds_exact_canonical_text_not_whitespace_fold(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, version, record = self.make_evidence(cache)
            folder = cache / "extractions" / ("a" * 64) / "page-0001" / "fixture"
            (folder / "ocr.txt").write_text("raw\tone", encoding="utf-8")
            for artifact in record["artifacts"]:
                artifact["sha256"] = pipeline.hash_file(cache / artifact["path"])
            pipeline.write_json(folder / "evidence.json", record)
            version["text"] = "raw\none"
            version["paragraphs"] = ["raw", "one"]
            pipeline.compose(spec, ledger, cache, cache / "out")
            built = pipeline.read_json(cache / "out/articles.json")["articles"][0]["versions"][0]
            check = next(c for c in built["verification"]["checks"] if c["check"] == "text-layer-matches-ocr")
            self.assertEqual(pipeline.hash_text("raw\none"), check["textSha256"])
            self.assertNotEqual(pipeline.hash_text("raw one"), check["textSha256"])
            self.assertNotEqual(pipeline.hash_text("raw\tone"), check["textSha256"])

    def test_evidence_must_belong_to_the_claimed_official_publication(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, _, _ = self.make_evidence(cache)
            ledger["publications"][0]["arabic"]["sha256"] = "b" * 64
            with self.assertRaisesRegex(ValueError, "does not match"):
                pipeline.compose(spec, ledger, cache, cache / "out")

    def test_every_crop_becomes_a_source_fragment(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, version, _ = self.make_evidence(cache)
            # Duplicate evidence is enough to exercise ordered fragment preservation;
            # this fixture makes no legal or image-review claim.
            version["evidenceIds"] = ["fixture", "fixture"]
            pipeline.compose(spec, ledger, cache, cache / "out")
            built = pipeline.read_json(cache / "out/articles.json")["articles"][0]["versions"][0]
            self.assertEqual(2, len(built["source"]["fragments"]))
            self.assertEqual("raw one\nraw one", built["source"]["textLayer"])

    def test_explicit_french_structure_check_is_preserved(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            spec, ledger, version, _ = self.make_evidence(cache)
            check = {"check": "french-structure-matches", "method": "Fictitious image comparison",
                     "date": "2026-10-25", "result": "passed", "note": "Fictitious page/paragraph evidence"}
            version["verification"] = {"checks": [check]}
            pipeline.compose(spec, ledger, cache, cache / "out")
            checks = pipeline.read_json(cache / "out/articles.json")["articles"][0]["versions"][0]["verification"]["checks"]
            self.assertIn(check, checks)


class OfflineSchemaTests(unittest.TestCase):
    def test_empty_corpus_is_not_reported_as_validated(self):
        with tempfile.TemporaryDirectory() as temporary:
            errors = pipeline.validate_corpus(Path(temporary))
            self.assertEqual(1, len(errors))
            self.assertIn("no corpus code folders", errors[0])


if __name__ == "__main__":
    unittest.main()
