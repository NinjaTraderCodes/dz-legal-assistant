import copy
import unittest
from unittest.mock import patch

import build_civil_priority as builder
import pipeline


class BuildCivilPriorityTests(unittest.TestCase):
    def setUp(self):
        self.version = {"versionId":"fictitious:1@invented","text":"نص وهمي لا يغير.",
            "paragraphs":["نص وهمي لا يغير."],"inForceFrom":"2020-01-01","inForceTo":None,
            "temporalStatus":"resolved","source":{"publicationId":"invented","page":1},
            "verification":{"state":"checked","checks":[],"reasons":[]}}
        self.article = {"id":"fictitious:1","versions":[copy.deepcopy(self.version)]}

    def test_reuses_checked_version_without_adding_unknown_duplicate_or_changing_dates(self):
        candidate = copy.deepcopy(self.version)
        candidate.update(inForceFrom=None,temporalStatus="unsettled")
        before = copy.deepcopy(self.article)
        builder.merge_current(self.article,candidate)
        self.assertEqual(before,self.article)
        self.assertEqual(1,len(self.article["versions"]))

    def test_existing_legal_body_cannot_be_silently_replaced(self):
        candidate = copy.deepcopy(self.version)
        candidate.update(text="نص وهمي يغير.",paragraphs=["نص وهمي يغير."])
        with self.assertRaisesRegex(ValueError,"silently rewrite"):
            builder.merge_current(self.article,candidate)

    def test_current_pilot_check_augmentation_keeps_exact_body_dates_and_provenance(self):
        old = self.article["versions"][0]
        old["verification"] = {"state":"partly-checked","checks":[{"method":"Earlier fictitious check"}],"reasons":["Fictitious pending check"]}
        candidate = copy.deepcopy(self.version)
        candidate["verification"]["checks"]=[{"method":builder.PREFIX+"Fictitious new source check"}]
        protected = copy.deepcopy(builder.protected_version(old))
        with patch.object(builder,"CURRENT_489",old["versionId"]):
            builder.merge_current(self.article,candidate)
            builder.merge_current(self.article,candidate)
        self.assertEqual(protected,builder.protected_version(old))
        self.assertEqual("checked",old["verification"]["state"])
        self.assertEqual(2,len(old["verification"]["checks"]))
        self.assertEqual(1,len(self.article["versions"]))

    def test_baseline_rejects_lost_version_or_changed_negation_date_or_evidence(self):
        baseline={"articleIds":[self.article["id"]],"versions":[{"versionId":self.version["versionId"],
            "fullSha256":pipeline.hash_json(self.version),
            "protectedFieldsSha256":pipeline.hash_json(builder.protected_version(self.version))}]}
        builder.verify_preserved({"articles":[self.article]},baseline)
        for field,value in [("text","نص وهمي يغير."),("inForceFrom","2021-01-01"),("source",{"page":2})]:
            with self.subTest(field=field):
                changed=copy.deepcopy(self.article)
                changed["versions"][0][field]=value
                with self.assertRaisesRegex(ValueError,"prior Civil version"):
                    builder.verify_preserved({"articles":[changed]},baseline)
        changed=copy.deepcopy(self.article)
        changed["versions"]=[]
        with self.assertRaisesRegex(ValueError,"prior Civil version"):
            builder.verify_preserved({"articles":[changed]},baseline)


if __name__ == "__main__":
    unittest.main()
