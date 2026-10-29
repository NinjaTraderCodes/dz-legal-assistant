import copy
import tempfile
import unittest
from pathlib import Path

import civil_flag_spacing
import civil_source_nouns as nouns
import native_check as nc
import pipeline


class SourceNounTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        image = self.root / "fictitious-proof.txt"
        image.write_text("Fictitious source image fixture", encoding="utf-8")
        self.body = {"candidateKey": "main:456", "printedLabel": "المادة 456 :",
                     "text": "نص وهمي الا قتصادي فقط.", "paragraphs": ["نص وهمي الا قتصادي فقط."]}
        self.reading = {"candidateKey": "main:456", "proposal": {"old": "الا قتصادي", "new": "الاقتصادي"},
            "sourceReadNounNotNegation": True, "sourceReading": "الاقتصادي", "finding": "source-read-joined-word-proposed",
            "inspection": "actually read; not human/lawyer verification", "inputTextSha256": pipeline.hash_text(self.body["text"]),
            "images": [image.name], "imageSha256s": [pipeline.hash_file(image)], "at": 5}

    def apply(self):
        return nouns.project(self.body, self.reading, nouns.SOURCE_SHA, self.root)

    def test_source_bound_noun_exception_does_not_relax_the_general_negation_guard(self):
        before = copy.deepcopy(self.body)
        with self.assertRaisesRegex(ValueError, "Independent negation"):
            civil_flag_spacing.project(self.body, [{"old": "الا قتصادي", "new": "الاقتصادي", "at": 5}])
        self.assertEqual("نص وهمي الاقتصادي فقط.", self.apply()["text"])
        self.assertEqual(before, self.body)

    def test_unknown_article_or_arbitrary_nonnegative_word_cannot_use_the_exception(self):
        self.body["candidateKey"] = self.reading["candidateKey"] = "main:457"
        with self.assertRaisesRegex(ValueError, "two explicitly source-read"):
            self.apply()
        self.setUp()
        self.reading["proposal"] = {"old": "لا ضرر", "new": "لاضرر"}
        with self.assertRaisesRegex(ValueError, "two explicitly source-read"):
            self.apply()

    def test_wrong_source_or_missing_actual_noun_decision_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "two explicitly source-read"):
            nouns.project(self.body, self.reading, "b" * 64, self.root)
        self.reading["sourceReadNounNotNegation"] = False
        with self.assertRaisesRegex(ValueError, "two explicitly source-read"):
            self.apply()

    def test_stale_image_or_body_cannot_inherit_a_source_decision(self):
        (self.root / "fictitious-proof.txt").write_text("changed", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "image proof changed"):
            self.apply()
        self.setUp()
        self.body["text"] += " لا"
        with self.assertRaisesRegex(ValueError, "another raw body"):
            self.apply()

    def test_source_noun_requires_its_exact_token_position(self):
        self.reading["at"] = 4
        with self.assertRaisesRegex(ValueError, "exact flagged source span"):
            self.apply()
