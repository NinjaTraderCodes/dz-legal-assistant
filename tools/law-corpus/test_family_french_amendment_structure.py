import copy
import unittest

import family_french_amendment_structure as structure
import pipeline


class FrenchAmendmentStructureTests(unittest.TestCase):
    def setUp(self):
        text = "Fictitious structure fixture."
        self.article = {"number":"4","versions":[{"text":text,"paragraphs":[text],"introducedBy":"ordinance-05-02","status":"in-force"}]}
        self.inventory = {"operations":[{"operation":"replace","numbers":["4"]},{"operation":"repeal","numbers":["12"]}]}
        self.row = {"number":"4","frenchPrintedLabel":"4","arabicTextSha256":pipeline.hash_text(text),
                    "arabicParagraphSha256s":[pipeline.hash_text(text)],"arabicParagraphs":1,"frenchParagraphsActuallyRead":1,
                    "frenchPdfPages":[17],"verdict":"same printed paragraph count",
                    "inspection":"actually read on original French page images; structural only; no translation equivalence"}
        self.review = {"articles":[self.row],"repeals":{"numbers":["12"],"lawArticle":18,"pdfPage":20},
                       "publicationClause":{"calendarCommencementDate":None}}

    def test_structure_match_grants_no_checked_state_or_translation_claim(self):
        result=structure.compare(self.review,[self.article],self.inventory)
        self.assertEqual(1,result["paragraphCountMatches"])
        self.assertEqual(0,result["checkedStatesGranted"])
        self.assertFalse(result["translationEquivalenceCertified"])

    def test_changed_arabic_body_invalidates_structure_record(self):
        self.article["versions"][0]["text"] += " changed"
        with self.assertRaisesRegex(ValueError,"Arabic body/paragraphs changed"):
            structure.compare(self.review,[self.article],self.inventory)

    def test_missing_or_duplicate_quoted_label_fails(self):
        self.review["articles"].append(copy.deepcopy(self.row))
        with self.assertRaisesRegex(ValueError,"inventory differs"):
            structure.compare(self.review,[self.article],self.inventory)

    def test_layout_difference_must_remain_explicit(self):
        self.row["frenchParagraphsActuallyRead"]=2
        with self.assertRaisesRegex(ValueError,"silently treated as a match"):
            structure.compare(self.review,[self.article],self.inventory)
        self.row["verdict"]="edition layout difference; independently read Arabic paragraphing retained"
        self.assertEqual(["4"],structure.compare(self.review,[self.article],self.inventory)["paragraphDifferences"])

    def test_publication_clause_cannot_supply_a_date(self):
        self.review["publicationClause"]["calendarCommencementDate"]="2005-02-27"
        with self.assertRaisesRegex(ValueError,"invent a calendar"):
            structure.compare(self.review,[self.article],self.inventory)


if __name__ == "__main__":
    unittest.main()
