import copy
import unittest

import family_original_french_paragraphs as structure
import pipeline


class OriginalFrenchParagraphTests(unittest.TestCase):
    def setUp(self):
        text="Fictitious paragraph fixture."
        self.article={"number":"16","versions":[{"text":text,"paragraphs":[text],"introducedBy":"law-84-11","status":"in-force"}]}
        self.inventory={"operations":[]}
        self.labels={"columnsActuallyRead":[{"pdfPage":2,"labels":["16"],"continuationOf":None,"evidenceId":"fixture-2"},
                                           {"pdfPage":3,"labels":["17"],"continuationOf":"16","evidenceId":"fixture-3"}]}
        self.row={"number":"16","arabicTextSha256":pipeline.hash_text(text),"arabicParagraphSha256s":[pipeline.hash_text(text)],
                  "arabicParagraphs":1,"frenchParagraphsActuallyRead":2,"frenchPdfPages":[2,3],"frenchSourceColumns":["fixture-2","fixture-3"],
                  "inspection":"actually read all relevant French paragraph starts/continuations on source-page images; structural only; not full-word/translation certification",
                  "verdict":"edition layout difference; independently source-read Arabic paragraphing retained"}
        self.review={"articles":[self.row]}

    def test_edition_layout_difference_never_changes_arabic_or_resolves_a_conflict(self):
        before=copy.deepcopy(self.article)
        result=structure.compare(self.review,self.labels,[self.article],self.inventory)
        self.assertEqual(1,len(result["paragraphDifferences"]))
        self.assertEqual(0,result["arabicConflictsResolved"])
        self.assertEqual(before,self.article)

    def test_omitted_page_continuation_is_rejected(self):
        self.row["frenchPdfPages"]=[2]
        with self.assertRaisesRegex(ValueError,"continuation"):
            structure.compare(self.review,self.labels,[self.article],self.inventory)

    def test_stale_body_or_paragraphs_invalidates_the_reading(self):
        self.article["versions"][0]["paragraphs"].append("Another fictitious paragraph.")
        with self.assertRaisesRegex(ValueError,"Arabic body/paragraphs changed"):
            structure.compare(self.review,self.labels,[self.article],self.inventory)

    def test_missing_or_duplicate_current_body_fails(self):
        self.review["articles"].append(copy.deepcopy(self.row))
        with self.assertRaisesRegex(ValueError,"missing, duplicate"):
            structure.compare(self.review,self.labels,[self.article],self.inventory)

    def test_mismatch_cannot_be_reported_as_matching(self):
        self.row["verdict"]="same printed paragraph count"
        with self.assertRaisesRegex(ValueError,"silently called a match"):
            structure.compare(self.review,self.labels,[self.article],self.inventory)


if __name__ == "__main__":
    unittest.main()
