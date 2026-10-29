import copy
import unittest

import family_original_french_labels as labels


class OriginalFrenchLabelTests(unittest.TestCase):
    def setUp(self):
        self.review = {"columnsActuallyRead": []}
        for index, (page, side) in enumerate((p,s) for p in range(2,16) for s in ("left","right")):
            self.review["columnsActuallyRead"].append({"pdfPage":page,"column":side,"printedPage":page+610,
                "evidenceId":f"family-fr-original-{page:03d}-{side}","labels":[str(n) for n in range(index*8+1,index*8+9)],
                "inspection":"actually read; labels/presence only; not full-word or paragraph certification"})

    def test_all_labels_required_without_promoting_full_text(self):
        self.assertEqual([str(n) for n in range(1,225)],labels.label_inventory(self.review))

    def test_ocr_missing_number_cannot_be_filled_by_continuity(self):
        self.review["columnsActuallyRead"][0]["labels"].remove("4")
        with self.assertRaisesRegex(ValueError,"missing, duplicate"):
            labels.label_inventory(self.review)

    def test_duplicate_or_reordered_column_fails(self):
        self.review["columnsActuallyRead"][1]=copy.deepcopy(self.review["columnsActuallyRead"][0])
        with self.assertRaisesRegex(ValueError,"each source column"):
            labels.label_inventory(self.review)

    def test_generated_view_is_not_an_actual_reading(self):
        self.review["columnsActuallyRead"][0]["inspection"]="generated but unread"
        with self.assertRaisesRegex(ValueError,"actual label reading"):
            labels.label_inventory(self.review)

    def test_wrong_evidence_column_fails(self):
        self.review["columnsActuallyRead"][0]["evidenceId"]="family-fr-original-002-right"
        with self.assertRaisesRegex(ValueError,"wrong evidence"):
            labels.label_inventory(self.review)


if __name__ == "__main__":
    unittest.main()
