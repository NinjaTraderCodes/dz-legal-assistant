import copy
import unittest

import ensure_family_reference_targets as targets


class FamilyExternalIdentityTests(unittest.TestCase):
    def test_reference_only_contains_no_operative_text_or_version(self):
        content={"codeId":"fictitious","articles":[]}
        self.assertTrue(targets.ensure(content,"fictitious:40"))
        article=content["articles"][0]
        self.assertEqual("reference-only",article["availability"])
        self.assertEqual([],article["versions"])
        self.assertEqual([],article["path"])

    def test_existing_real_article_is_never_overwritten(self):
        content={"codeId":"fictitious","articles":[{"id":"fictitious:40","canonicalNumber":"40","versions":[{"text":"Invented fixture"}]}]}
        before=copy.deepcopy(content)
        self.assertFalse(targets.ensure(content,"fictitious:40"))
        self.assertEqual(before,content)

    def test_repeated_reference_creation_is_idempotent(self):
        content={"codeId":"fictitious","articles":[]}
        targets.ensure(content,"fictitious:40")
        before=copy.deepcopy(content)
        self.assertFalse(targets.ensure(content,"fictitious:40"))
        self.assertEqual(before,content)

    def test_namespace_mismatch_is_rejected(self):
        with self.assertRaisesRegex(ValueError,"namespace"):
            targets.ensure({"codeId":"fictitious","articles":[]},"another-code:40")

    def test_external_catalogue_cannot_claim_collected_special_law(self):
        ledger,content,report=targets.reference_catalogue("2026-10-26")
        self.assertFalse(ledger["scope"]["complete"])
        self.assertEqual([],ledger["publications"])
        self.assertEqual([],content["articles"][0]["versions"])
        self.assertEqual([],report["inventory"]["present"])
        self.assertEqual(0,sum(report["countsByState"].values()))


if __name__ == "__main__":
    unittest.main()
