"""Fictitious amendments only; no fixture asserts Algerian law."""
from copy import deepcopy
import unittest

import consolidate
import pipeline


class AmendmentTests(unittest.TestCase):
    def fixture(self):
        version = {"versionId": "fiction:1@original", "introducedBy": "original", "status": "in-force",
                   "text": "old first\nunchanged second\nold third", "paragraphs": ["old first", "unchanged second", "old third"],
                   "inForceFrom": "2000-01-01", "inForceTo": None, "temporalStatus": "resolved",
                   "evidenceIds": ["old-crop"], "crossReferences": []}
        initial = {"generatedOn": "2026-10-25", "articles": [{"id": "fiction:1", "number": "1", "canonicalNumber": "1",
                   "path": ["Invented chapter"], "versions": [version]}]}
        ledger = {"id": "fiction", "scope": {"targetArticles": ["1"]}, "publications": [
            {"id": "original", "jo": {"date": "2000-01-01"}, "articlesTouched": ["1"]},
            {"id": "amendment", "jo": {"date": "2002-01-01"}, "articlesTouched": ["1"]}]}
        operation = {"id": "op-1", "publicationId": "amendment", "number": "1", "action": "edit-paragraphs",
                     "expectedBaseTextSha256": pipeline.hash_text(version["text"]), "effectiveDate": None,
                     "temporalBasis": "Invented unknown boundary; publication is not guessed as commencement.",
                     "edits": [{"start": 1, "delete": 1, "paragraphs": ["new first"],
                                "evidenceFragments": [{"evidenceId": "new-crop", "publicationId": "amendment"}]}],
                     "crossReferences": []}
        plan = {"generatedOn": "2026-10-25", "operations": [operation]}
        return initial, ledger, plan, operation

    def test_partial_replacement_preserves_old_paragraph_sources_and_unknown_boundary(self):
        initial, ledger, plan, _ = self.fixture()
        before = deepcopy(initial)
        result, audit = consolidate.apply_plan(initial, ledger, plan)
        self.assertEqual(before, initial)
        versions = result["articles"][0]["versions"]
        self.assertEqual("new first\nunchanged second\nold third", versions[-1]["text"])
        self.assertEqual(["amendment", "original", "original"], [f["publicationId"] for f in versions[-1]["evidenceFragments"]])
        self.assertEqual("unsettled", versions[0]["temporalStatus"])
        self.assertIsNone(versions[0]["inForceTo"])
        self.assertIsNone(versions[-1]["inForceFrom"])
        self.assertEqual([], result["unappliedPublications"])
        self.assertEqual(1, len(audit["operations"]))
        self.assertNotIn("inventoryReview", result)
        self.assertNotIn("checks", versions[-1]["verification"])

    def test_known_boundary_is_exclusive_and_full_text_hash_guards_the_base(self):
        initial, ledger, plan, op = self.fixture()
        op["effectiveDate"] = "2002-02-01"
        result, _ = consolidate.apply_plan(initial, ledger, plan)
        self.assertEqual("2002-02-01", result["articles"][0]["versions"][0]["inForceTo"])
        op["expectedBaseTextSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "base text hash"):
            consolidate.apply_plan(initial, ledger, plan)

    def test_multiple_edits_use_old_paragraph_positions_and_keep_order(self):
        initial, ledger, plan, op = self.fixture()
        op["edits"].append({"start": 3, "delete": 1, "paragraphs": ["new third", "new fourth"],
                             "evidenceFragments": [{"evidenceId": "third-crop", "publicationId": "amendment"}]})
        result, _ = consolidate.apply_plan(initial, ledger, plan)
        self.assertEqual(["new first", "unchanged second", "new third", "new fourth"], result["articles"][0]["versions"][-1]["paragraphs"])

    def test_overlapping_paragraph_edits_are_rejected(self):
        initial, ledger, plan, op = self.fixture()
        op["edits"].append({"start": 1, "delete": 2, "paragraphs": []})
        with self.assertRaisesRegex(ValueError, "overlap"):
            consolidate.apply_plan(initial, ledger, plan)

    def test_insertion_and_repeal_need_published_evidence(self):
        initial, ledger, plan, op = self.fixture()
        op["action"] = "repeal"
        with self.assertRaisesRegex(ValueError, "actual amending instruction"):
            consolidate.apply_plan(initial, ledger, plan)
        op["instructionEvidence"] = [{"evidenceId": "repeal-crop", "publicationId": "amendment"}]
        result, _ = consolidate.apply_plan(initial, ledger, plan)
        last = result["articles"][0]["versions"][-1]
        self.assertEqual(("repealed", "", []), (last["status"], last["text"], last["paragraphs"]))

    def test_heading_change_preserves_body_and_has_its_own_publication_locator(self):
        initial, ledger, plan, op = self.fixture()
        op.update(action="heading", edits=[], path=["New invented heading"],
                  instructionEvidence=[{"evidenceId": "heading-crop", "publicationId": "amendment"}])
        result, _ = consolidate.apply_plan(initial, ledger, plan)
        last = result["articles"][0]["versions"][-1]
        self.assertEqual(initial["articles"][0]["versions"][0]["text"], last["text"])
        self.assertEqual(["New invented heading"], last["path"])
        self.assertEqual("heading", last["evidenceFragments"][-1]["role"])

    def test_explicit_renumbering_retains_old_identity_and_creates_new_one(self):
        initial, ledger, plan, op = self.fixture()
        ledger["scope"]["targetArticles"].append("2")
        ledger["publications"][1]["articlesTouched"].append("2")
        op.update(action="renumber", newNumber="2", instructionEvidence=[{"evidenceId": "renumber-crop", "publicationId": "amendment"}])
        result, audit = consolidate.apply_plan(initial, ledger, plan)
        self.assertEqual("repealed", result["articles"][0]["versions"][-1]["status"])
        new = result["articles"][1]
        self.assertEqual("fiction:2", new["id"])
        self.assertEqual(initial["articles"][0]["versions"][0]["text"], new["versions"][0]["text"])
        self.assertEqual("fiction:2", audit["operations"][0]["renumberedTo"])

    def test_unapplied_touched_article_is_never_a_complete_publication(self):
        initial, ledger, plan, _ = self.fixture()
        ledger["scope"]["targetArticles"].append("2")
        ledger["publications"][1]["articlesTouched"].append("2")
        result, audit = consolidate.apply_plan(initial, ledger, plan)
        self.assertEqual(["amendment"], result["unappliedPublications"])
        self.assertEqual(["2"], audit["publications"][1]["missing"])

    def test_duplicate_instruction_or_missing_temporal_basis_is_rejected(self):
        initial, ledger, plan, op = self.fixture()
        plan["operations"].append(deepcopy(op))
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            consolidate.apply_plan(initial, ledger, plan)
        plan["operations"].pop()
        op["temporalBasis"] = ""
        with self.assertRaisesRegex(ValueError, "temporal basis"):
            consolidate.apply_plan(initial, ledger, plan)


if __name__ == "__main__":
    unittest.main()
