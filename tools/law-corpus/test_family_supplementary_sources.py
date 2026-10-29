import tempfile
import unittest
from pathlib import Path

import family_supplementary_sources as sources
import pipeline


class SupplementaryFamilySourceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.cache = self.root / "cache"
        self.cache.mkdir()
        entries,images = [],[]
        for ident,page in [("A2005043",4),("F2005043",3),("F1984031",9),("F1984041",2)]:
            pdf = self.cache / (ident+".pdf")
            image = self.root / (ident+".txt")
            pdf.write_text("Fictitious source fixture "+ident,encoding="utf-8")
            image.write_text("Fictitious image fixture "+ident,encoding="utf-8")
            entries.append({"id":ident,"cacheFile":pdf.name,"sha256":pipeline.hash_file(pdf),"inspectedPdfPage":page,
                "image":image.name,"imageSha256":pipeline.hash_file(image),"inspection":"actually read; not human/lawyer verification",
                "url":"https://joradp.dz/fixture/"+ident+".pdf","retrievedOn":"2026-10-29"})
            images.append({"image":image.name,"imageSha256":pipeline.hash_file(image)})
        self.review={"reviewedOn":"2026-10-29","sources":entries,"imagesActuallyRead":images,
            "approval":{"publicationId":"law-05-09","number":"05-09","date":"2005-05-04","joIssue":"43","joDate":"2005-06-22",
                        "arabicSourceId":"A2005043","arabicPdfPage":4,"frenchSourceId":"F2005043","frenchPdfPage":3,
                        "approvesPublicationId":"ordinance-05-02","articleBodyEffect":"approval-only"},
            "arabicConflictEffect":{"resolved":False}}

    def test_approval_does_not_create_article_changes_or_a_date(self):
        pins=sources.validate(self.review,self.root,self.cache)
        publication=sources.approval_publication(self.review,pins)
        self.assertEqual([],publication["articlesTouched"])
        self.assertIsNone(publication["commencementDate"])
        self.assertEqual([4],publication["jo"]["pages"])

    def test_french_page_number_cannot_be_copied_to_arabic_locator(self):
        self.review["approval"]["arabicPdfPage"]=3
        with self.assertRaisesRegex(ValueError,"continued contents"):
            sources.validate(self.review,self.root,self.cache)

    def test_changed_pdf_or_image_is_rejected(self):
        (self.cache/"A2005043.pdf").write_text("changed",encoding="utf-8")
        with self.assertRaisesRegex(ValueError,"PDF changed"):
            sources.validate(self.review,self.root,self.cache)

    def test_missing_source_page_reading_is_rejected(self):
        self.review["imagesActuallyRead"].pop()
        with self.assertRaisesRegex(ValueError,"proof is incomplete"):
            sources.validate(self.review,self.root,self.cache)

    def test_french_rectification_cannot_waive_arabic_conflicts(self):
        self.review["arabicConflictEffect"]["resolved"]=True
        with self.assertRaisesRegex(ValueError,"Arabic wording conflicts"):
            sources.validate(self.review,self.root,self.cache)


if __name__ == "__main__":
    unittest.main()
