"""Numerical/protocol fixtures only, not additional legal development cases."""
import unittest
from fit_status import choose, fit, matrix, objective_gradient, project
from select_status import reductions, safe_omissions


class OffsetTests(unittest.TestCase):
    def test_projection_respects_zero_sum_and_hard_bounds(self):
        result = project([100, -100, 0, 0])
        self.assertAlmostEqual(sum(result), 0)
        self.assertTrue(all(-3 <= x <= 3 for x in result))
        self.assertAlmostEqual(result[0], 3)
        self.assertAlmostEqual(result[1], -3)
        self.assertAlmostEqual(result[2], result[3])

    def test_global_bias_fit_lowers_the_declared_objective(self):
        samples = [dict(label=i, scores=[4.0, 0.0, 0.0, 0.0]) for i in range(4)]
        fitted = fit(samples, .1)
        original = objective_gradient(samples, [0.0] * 4, .1)[0]
        self.assertLess(fitted["objective"], original)
        self.assertLess(fitted["offsets"][0], 0)
        self.assertAlmostEqual(fitted["offsets"][1], fitted["offsets"][2])
        self.assertAlmostEqual(fitted["offsets"][2], fitted["offsets"][3])
        self.assertTrue(fitted["converged"])
        self.assertLessEqual(fitted["iterations"], 1000)

    def test_balancing_is_invariant_to_replicating_one_class(self):
        samples = [dict(label=i, scores=[4.0, 0.0, 0.0, 0.0]) for i in range(4)]
        a = fit(samples, .1)["offsets"]
        b = fit(samples + [samples[0]] * 19, .1)["offsets"]
        for x, y in zip(a, b):
            self.assertAlmostEqual(x, y, places=8)

    def test_missing_training_class_is_a_failure(self):
        with self.assertRaises(ValueError):
            fit([dict(label=0, scores=[0.0] * 4)], .1)

    def test_numeric_ties_match_the_csharp_rule(self):
        self.assertEqual(1, choose([0.0] * 4, [0.0] * 4))
        self.assertEqual(3, choose([0.0] * 4, [0.0, 0.0, 1.0, 0.0]))

    def test_reversed_questions_align_by_event_before_averaging(self):
        rows = [dict(Name="numeric", Fold=1, Kind="polarity", Chains=[
            dict(Questions=[dict(Event="x", Scores=[0.0] * 4), dict(Event="y", Scores=[2.0, 0.0, 0.0, 0.0])]),
            dict(Questions=[dict(Event="y", Scores=[0.0, 2.0, 0.0, 0.0]), dict(Event="x", Scores=[0.0, 0.0, 2.0, 0.0])]),
        ])]
        cases = {"numeric": dict(annotation=dict(events=[dict(kind="x", polarity="affirmed"), dict(kind="y", polarity="denied")]))}
        members = {"numeric": dict(fold=1, familyKind="polarity")}
        averaged = matrix(rows, cases, members, "averaged")
        self.assertEqual([0.0, 0.0, 1.0, 0.0], averaged[0]["scores"])
        self.assertEqual([1.0, 1.0, 0.0, 0.0], averaged[1]["scores"])
        self.assertEqual(1, choose(matrix(rows, cases, members, "fixed")[0]["scores"], [0.0] * 4))
        self.assertEqual(3, choose(averaged[0]["scores"], [0.0] * 4))

    def test_more_false_affirmations_or_omissions_cannot_pass(self):
        reference = dict(counts=dict(criticalCases=5, nonAffirmedErrors=10, falseAffirmations=1, dateMissing=1, decisiveOmissions=1))
        more_false = dict(counts=dict(criticalCases=4, nonAffirmedErrors=9, falseAffirmations=2))
        more_missing = dict(counts=dict(criticalCases=4, nonAffirmedErrors=9, falseAffirmations=1, dateMissing=2))
        self.assertFalse(reductions(more_false, reference))
        self.assertTrue(reductions(more_missing, reference))
        self.assertFalse(safe_omissions(more_missing, reference))


if __name__ == "__main__":
    unittest.main()
