import unittest

import civil_ministry_audit as audit
import pipeline


def candidate(key="main:1", text="لا يجوز فعل اختباري (19)."):
    return {"number": "1", "numberScope": key.split(":")[0], "candidateKey": key,
            "printedLabel": "المادة الأولى :", "text": text, "textSha256": pipeline.hash_text(text), "pages": [8]}


def comparison(article, reading):
    return {"results": [{"number": article["candidateKey"], "ocr": {
        "tesseract": reading, "ppocr": reading}, "disagreements": [],
        "alignedEvidenceIds": {"tesseract": ["civil-ministry-008-body"], "ppocr": ["civil-ministry-008-body"]}}]}


class CivilStrictAuditTests(unittest.TestCase):
    def test_local_and_general_numbers_remain_distinct(self):
        articles = audit.inputs_for([candidate(), candidate("management-contract:1")])
        result = audit.audit(articles, {"results": [comparison(a, a["text"])["results"][0] for a in articles]})
        self.assertEqual(2, result["wordAgreed"])
        self.assertEqual(["main:1", "management-contract:1"], [r["candidateKey"] for r in result["results"]])

    def test_editorial_or_normative_numbers_are_not_silently_removed(self):
        article = audit.inputs_for([candidate()])[0]
        result = audit.audit([article], comparison(article, "يجوز فعل اختباري."))["results"][0]
        self.assertFalse(result["wordAgrees"])
        self.assertIn("لا", [d["canonical"] for d in result["disagreements"]])
        self.assertTrue(any("19" in d["canonical"] for d in result["disagreements"]))

    def test_matching_arabic_letter_fold_cannot_pass(self):
        article = audit.inputs_for([candidate(text="هذه مسؤولية اختباريّة.")])[0]
        result = audit.audit([article], comparison(article, "هذه مسؤوليه اختباريّة."))["results"][0]
        self.assertFalse(result["wordAgrees"])

    def test_missing_reader_is_not_agreement(self):
        article = audit.inputs_for([candidate()])[0]
        check = comparison(article, article["text"])
        del check["results"][0]["ocr"]["ppocr"]
        result = audit.audit([article], check)["results"][0]
        self.assertFalse(result["wordAgrees"])

    def test_a_missing_local_identity_rejects_aggregate(self):
        articles = audit.inputs_for([candidate(), candidate("management-contract:1")])
        with self.assertRaisesRegex(ValueError, "each qualified"):
            audit.audit(articles, comparison(articles[0], articles[0]["text"]))

    def test_a_duplicate_comparison_row_cannot_pass(self):
        article = audit.inputs_for([candidate()])[0]
        check = comparison(article, article["text"])
        check["results"] *= 2
        with self.assertRaisesRegex(ValueError, "exactly once"):
            audit.audit([article], check)

    def test_identical_words_on_another_page_are_not_a_source_match(self):
        article = audit.inputs_for([candidate()])[0]
        check = comparison(article, article["text"])
        check["results"][0]["alignedEvidenceIds"]["ppocr"] = ["civil-ministry-018-body"]
        result = audit.audit([article], check)["results"][0]
        self.assertFalse(result["wordAgrees"])
        self.assertIn("source-page-mismatch", [d["kind"] for d in result["disagreements"]])


if __name__ == "__main__":
    unittest.main()
