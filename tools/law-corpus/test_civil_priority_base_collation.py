import copy
import tempfile
import unittest
from pathlib import Path

import civil_priority_base_collation as collation
import pipeline


class CivilPriorityBaseCollationTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        image = self.root / "fictitious-proof.txt"
        image.write_text("Invented source-image fixture",encoding="utf-8")
        text = "نص وهمي لا يحذف بعد عشرة (10) أيام."
        self.body = {"number":"999","text":text,"paragraphs":[text]}
        self.viewed = {"number":"999","text":text,"textSha256":pipeline.hash_text(text),
                       "images":[image.name],"imageSha256s":[pipeline.hash_file(image)],"regions":[{"evidenceId":"fictitious"}]}
        self.reading = {"number":"999","inputTextSha256":pipeline.hash_text(text),
                        "inspection":"actually read; not human/lawyer verification",
                        "finding":"same-clauses-edition-orthography","proposals":[],"printedBodyParagraphs":1,
                        "note":"Fictitious complete source reading",
                        **{k:copy.deepcopy(self.viewed[k]) for k in ["images","imageSha256s","regions"]}}

    def apply(self):
        return collation.apply_reading(self.body,self.reading,self.viewed,self.root)

    def test_edition_reading_does_not_rewrite_digits_negations_or_any_source_character(self):
        before=copy.deepcopy(self.body)
        result=self.apply()
        self.assertEqual(before,self.body)
        self.assertEqual(before["text"],result["text"])
        self.assertEqual(before["paragraphs"],result["paragraphs"])
        self.assertNotIn("verification",result)

    def test_a_word_change_or_paragraph_mismatch_cannot_pass_as_orthography(self):
        self.reading["proposals"]=[{"old":"لا","new":""}]
        with self.assertRaisesRegex(ValueError,"own guarded decision"):
            self.apply()
        self.reading["proposals"]=[]
        self.reading["printedBodyParagraphs"]=2
        with self.assertRaisesRegex(ValueError,"complete source reading"):
            self.apply()

    def test_stale_body_or_substituted_source_proof_is_rejected(self):
        self.body["text"]+=" نص زائد."
        with self.assertRaisesRegex(ValueError,"another current body"):
            self.apply()
        self.body["text"]=self.viewed["text"]
        self.reading["imageSha256s"]=["b"*64]
        with self.assertRaisesRegex(ValueError,"complete source reading"):
            self.apply()

    def test_empty_source_view_cannot_count_as_a_reading(self):
        for obj in [self.reading,self.viewed]:
            obj["images"]=[];obj["imageSha256s"]=[]
        with self.assertRaisesRegex(ValueError,"complete source reading"):
            self.apply()

    def test_81source_override_is_closed_and_keeps_81text_and_82proof(self):
        empty=self.viewed|{"number":"81","images":[],"imageSha256s":[],"regions":[]}
        donor=self.viewed|{"number":"82"}
        reading=self.reading|{"number":"81","sourceViewOverride":{
            "previousImages":[],"fromViewedArticle":"82","note":"Fictitious81body visible in82proof"}}
        result=collation.view_for(reading,{"81":empty,"82":donor})
        self.assertEqual("81",result["number"])
        self.assertEqual(empty["text"],result["text"])
        self.assertEqual(donor["images"],result["images"])
        reading["sourceViewOverride"]["fromViewedArticle"]="83"
        with self.assertRaisesRegex(ValueError,"missing81view"):
            collation.view_for(reading,{"81":empty,"82":donor})

    def reset_body(self,number,text):
        self.body.update(number=number,text=text,paragraphs=text.split("\n"))
        self.viewed.update(number=number,text=text,textSha256=pipeline.hash_text(text))
        self.reading.update(number=number,inputTextSha256=pipeline.hash_text(text),printedBodyParagraphs=len(self.body["paragraphs"]))

    def test_358separates_only_three_closed_numeric_glosses_and_keeps_all_quantity_words(self):
        self.reset_body("358","مثال وهمي عن (1/5) الخمس إلى أربعة (4/5) أخماس.\nيزيد عن الخمس (1/5) أن لا يحذف.")
        self.reading.update(finding="source-read-compiler-numeric-gloss-separation",proposals=collation.FRACTION_GLOSSES_358)
        before=copy.deepcopy(self.body)
        result=self.apply()
        self.assertEqual(before,self.body)
        self.assertEqual("مثال وهمي عن الخمس إلى أربعة أخماس.\nيزيد عن الخمس أن لا يحذف.",result["text"])
        self.assertEqual(2,len(result["paragraphs"]))

    def test_358exception_cannot_remove_a_legal_quantity_or_apply_to_another_article(self):
        self.reset_body("999","نص وهمي عن (1/5) الخمس إلى أربعة (4/5) أخماس. الخمس (1/5) أن لا يحذف.")
        self.reading.update(finding="source-read-compiler-numeric-gloss-separation",proposals=collation.FRACTION_GLOSSES_358)
        with self.assertRaisesRegex(ValueError,"closed source-read numeric glosses"):
            self.apply()
        self.reset_body("358",self.body["text"]+" مدة عشر (10) سنوات.")
        with self.assertRaisesRegex(ValueError,"cannot lose any Arabic quantity"):
            self.apply()

    def test_363conflict_is_preserved_and_can_never_be_a_passed_wording_change(self):
        self.reset_body("363","مثال وهمي وفقا للمادة184.")
        self.reading.update(finding="unresolved-original-JO-reference-conflict",proposals=[],
            referenceConflict={"ministry":"184","originalArabic":"224","originalFrench":"184"},
            additionalProofs=[{"sourceId":source,"sourceSha256":sha,"pdfPage":page,
                              "image":self.viewed["images"][0],"imageSha256":self.viewed["imageSha256s"][0]}
                             for source,(sha,page) in collation.CONFLICT_363_SOURCES.items()])
        result=self.apply()
        self.assertEqual(self.body["text"],result["text"])
        self.assertNotIn("verification",result)
        self.assertTrue(result["currentBaseJoCheck"]["result"].startswith("unresolved-"))
        self.reading["additionalProofs"][0]["pdfPage"]=24
        with self.assertRaisesRegex(ValueError,"must remain explicit"):
            self.apply()

    def test_386separates_only_source_absent_digit_and_keeps_the_six_month_quantity(self):
        self.reset_body("386","مثال وهمي مدة ستة (6) أشهر ولا يغير.")
        self.reading.update(finding="source-read-compiler-numeric-gloss-separation",proposals=collation.NUMERIC_GLOSSES["386"])
        self.assertEqual("مثال وهمي مدة ستة أشهر ولا يغير.",self.apply()["text"])
        self.reading["proposals"]=[{"old":"ستة (6) أشهر","new":"سبعة أشهر"}]
        with self.assertRaisesRegex(ValueError,"closed source-read numeric glosses"):
            self.apply()


if __name__ == "__main__":
    unittest.main()
