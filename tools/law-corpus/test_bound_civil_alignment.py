import unittest

from bound_civil_alignment import bound


def token(text, page):
    return {"token": text, "source": f"civil-ministry-{page:03d}-body"}


class CivilPageProjectionTests(unittest.TestCase):
    def project(self, words, pages):
        result = {"located": {"test": [0, len(words)]}, "regions": [
            {"evidenceId": t["source"], "box": [0, 0, 1, 1]} for t in words]}
        return bound({"pages": pages}, result, {"test": words})

    def test_removes_only_tokens_on_neighbour_pages(self):
        result = self.project([token("سابق", 7), token("لا", 8), token("19", 8), token("لاحق", 9)], [8])
        self.assertEqual("لا 19", result["ocr"]["test"])
        self.assertEqual([1, 3], result["located"]["test"])
        self.assertEqual(["civil-ministry-008-body"], result["alignedEvidenceIds"]["test"])

    def test_same_page_extra_words_and_headings_remain_visible(self):
        result = self.project([token("نص", 8), token("المادة", 8), token("الفصل", 8)], [8])
        self.assertEqual("نص المادة الفصل", result["ocr"]["test"])

    def test_two_page_body_is_kept_whole(self):
        result = self.project([token("نص", 8), token("ولا", 9), token("زيادة", 10)], [8, 9])
        self.assertEqual("نص ولا", result["ocr"]["test"])

    def test_wrong_page_only_leaves_no_reading(self):
        result = self.project([token("نص مماثل", 9)], [8])
        self.assertEqual({}, result["ocr"])
        self.assertEqual([], result["regions"])

    def test_disjoint_readings_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "Non-contiguous"):
            self.project([token("أول", 8), token("خارج", 9), token("آخر", 8)], [8])


if __name__ == "__main__":
    unittest.main()
