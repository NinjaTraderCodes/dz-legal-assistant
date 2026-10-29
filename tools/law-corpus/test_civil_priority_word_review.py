import copy
import tempfile
import unittest
from pathlib import Path

import civil_priority_word_review as review
import native_check as nc
import pipeline


class CivilPriorityWordReviewTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        image = self.root / "fictitious-proof.txt"
        image.write_text("Fictitious proof fixture, never an actual source image", encoding="utf-8")
        self.selected = ["main:357", "main:360", "main:388"]
        paragraphs = {"main:357": ["نص وهمي يحد د فقط."],
                      "main:360": ["نص وهمي للاختبار.", "-2 التزامات البائع"],
                      "main:388": ["نص وهمي فيه تسليم المبيع، م ا لم يوجد اتفاق."]}
        self.native = {"source": {"sha256": "a" * 64}, "articles": []}
        self.audit = {"results": []}
        self.saved = {"sourceSha256": "a" * 64, "readings": []}
        for key in self.selected:
            number = key.split(":")[1]
            text = "\n".join(paragraphs[key])
            body = {"candidateKey": key, "number": number, "numberScope": "main",
                    "printedLabel": "المادة " + number + " :", "paragraphs": paragraphs[key], "text": text}
            self.native["articles"].append(body)
            canonical = {"main:357": "يحد د", "main:360": "2", "main:388": "م ا"}[key]
            at = (len(nc.tokens(body["printedLabel"] + " " + text[:text.index("م ا لم يوجد")]))
                  if number == "388" else 5)
            flag = {"kind": "unconfirmed", "at": at, "canonical": canonical, "punctuationOnly": False}
            self.audit["results"].append({"number": key, "wordAgrees": False, "disagreements": [flag]})
            self.saved["readings"].append({"itemId": key + "#0", "candidateKey": key,
                "kind": flag["kind"], "at": at, "canonical": canonical, "ocrExtra": None,
                "inputTextSha256": pipeline.hash_text(text),
                "finding": "source-read-native-body-heading-contamination" if number == "360" else "source-read-spacing-restoration-proposed",
                "proposal": {"oldParagraph": "-2 التزامات البائع"} if number == "360" else
                            {"old": canonical, "new": "يحدد" if number == "357" else "ما"},
                "note": "Fictitious test reading; no real law or source certification",
                "images": [image.name], "imageSha256s": [pipeline.hash_file(image)],
                "inspection": "actually read; not human/lawyer verification"})

    def apply(self):
        return review.apply(self.native, self.audit, self.saved, self.selected, self.root)

    def test_selected_projection_preserves_raw_text_and_the_valid_neighboring_word_boundary(self):
        original = copy.deepcopy(self.native)
        result = self.apply()
        self.assertEqual(original, self.native)
        self.assertFalse(result["fullArticleChecked"])
        self.assertEqual(2, result["spacingRepairsApplied"])
        self.assertEqual(1, result["contextualSpacingRepairs"])
        self.assertEqual(1, result["bodyHeadingExclusions"])
        self.assertIn("تسليم المبيع، ما لم يوجد", result["projectedCandidates"][2]["text"])
        self.assertEqual(["نص وهمي للاختبار."], result["projectedCandidates"][1]["paragraphs"])

    def test_missing_or_duplicate_selected_flag_cannot_be_treated_as_complete(self):
        self.saved["readings"].pop()
        with self.assertRaisesRegex(ValueError, "every source-read flag"):
            self.apply()
        self.setUp()
        self.saved["readings"].append(copy.deepcopy(self.saved["readings"][0]))
        with self.assertRaisesRegex(ValueError, "every source-read flag"):
            self.apply()

    def test_stale_source_proof_fails(self):
        (self.root / "fictitious-proof.txt").write_text("changed", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "proof changed"):
            self.apply()

    def test_stale_body_cannot_reuse_an_actual_reading(self):
        self.native["articles"][0]["text"] += " لا"
        with self.assertRaisesRegex(ValueError, "different body"):
            self.apply()

    def test_context_must_cover_the_flagged_token_rather_than_another_identical_substring(self):
        self.saved["readings"][2]["at"] += 1
        self.audit["results"][2]["disagreements"][0]["at"] += 1
        with self.assertRaisesRegex(ValueError, "flagged token position"):
            self.apply()

    def test_only_the_exact_trailing_heading_can_be_excluded(self):
        self.saved["readings"][1]["proposal"]["oldParagraph"] = "نص وهمي للاختبار."
        with self.assertRaisesRegex(ValueError, "exact source-read trailing"):
            self.apply()

    def test_repeated_digit_spacing_is_not_legalized_by_the_context_exception(self):
        body = self.native["articles"][0]
        body["paragraphs"] = ["قيمة وهمية 1 9 فقط."]
        body["text"] = body["paragraphs"][0]
        self.saved["readings"][0] |= {"canonical": "1 9", "inputTextSha256": pipeline.hash_text(body["text"]),
                                     "proposal": {"old": "1 9", "new": "19"}}
        self.audit["results"][0]["disagreements"][0]["canonical"] = "1 9"
        with self.assertRaisesRegex(ValueError, "allowed source-read spacing"):
            self.apply()

    def test_separate_negation_cannot_be_absorbed_into_an_ordinary_word(self):
        body = self.native["articles"][0]
        body["paragraphs"] = ["لا ضرر وهمي."]
        body["text"] = body["paragraphs"][0]
        self.saved["readings"][0] |= {"canonical": "لا ضرر", "inputTextSha256": pipeline.hash_text(body["text"]),
                                     "proposal": {"old": "لا ضرر", "new": "لاضرر"}}
        self.audit["results"][0]["disagreements"][0]["canonical"] = "لا ضرر"
        with self.assertRaisesRegex(ValueError, "allowed source-read spacing"):
            self.apply()
