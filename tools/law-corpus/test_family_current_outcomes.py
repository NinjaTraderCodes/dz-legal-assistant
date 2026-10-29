import copy
import unittest

import family_current_outcomes as audit


class FamilyCurrentOutcomeTests(unittest.TestCase):
    def setUp(self):
        self.operations = {"2": {"operation": "repeal"}, "3": {"operation": "replace"}, "3 مكرر": {"operation": "insert"}}
        self.bodies = {n: {"text": "نص اختباري " + n, "paragraphs": ["نص اختباري " + n]} for n in ["1", "3", "3 مكرر"]}
        self.paths = {n: ["عنوان اختباري"] for n in ["1", "2", "3", "3 مكرر"]}
        self.articles = []
        for number in self.paths:
            publication = "law-84-11" if number == "1" else "ordinance-05-02"
            self.articles.append({"number": number, "path": self.paths[number], "versions": [{
                "introducedBy": publication, "source": {"publicationId": publication}, "path": self.paths[number],
                "status": "repealed" if number == "2" else "in-force",
                "text": self.bodies[number]["text"] if number in self.bodies else "",
                "paragraphs": self.bodies[number]["paragraphs"] if number in self.bodies else [],
                "verification": {"state": "partly-checked"}}]})

    def run_audit(self):
        return audit.audit(self.articles, set(self.paths), self.operations, self.bodies, self.paths)

    def test_valid_current_outcomes_do_not_claim_historical_or_whole_code_completion(self):
        before = copy.deepcopy(self.articles)
        result = self.run_audit()
        self.assertTrue(result["currentIdentityAndOutcomeAuditPassed"])
        self.assertFalse(result["historicalConsolidationComplete"])
        self.assertFalse(result["wholeCodeDeliveryComplete"])
        self.assertEqual(before, self.articles)

    def test_missing_inserted_identity_is_rejected(self):
        self.articles.pop()
        with self.assertRaisesRegex(ValueError, "independent inventory"):
            self.run_audit()

    def test_stale_word_or_paragraph_cannot_pass(self):
        self.articles[0]["versions"][0]["text"] = "نص اختباري مختلف"
        with self.assertRaisesRegex(ValueError, "reviewed body"):
            self.run_audit()

    def test_repeal_cannot_be_exposed_as_an_operative_article(self):
        self.articles[1]["versions"][0]["status"] = "in-force"
        with self.assertRaisesRegex(ValueError, "repeal state"):
            self.run_audit()

    def test_replacement_cannot_be_attributed_to_base_publication(self):
        self.articles[2]["versions"][0]["introducedBy"] = "law-84-11"
        with self.assertRaisesRegex(ValueError, "wrong publication"):
            self.run_audit()


if __name__ == "__main__":
    unittest.main()
