import unittest

import family_exact_audit as audit
import native_check as nc


class ExactWords(unittest.TestCase):
    def test_party_word_cannot_pass_as_one_letter_noise(self):
        old, _ = nc.token_statuses(["الزواج"], ["الزوج"])
        strict, _ = nc.token_statuses(["الزواج"], ["الزوج"], strict=True)
        self.assertEqual(["noise"], old)
        self.assertEqual(["miss"], strict)

    def test_hamza_fold_is_alignment_only_in_strict_mode(self):
        states, _ = nc.token_statuses(["أمر"], ["امر"], strict=True)
        self.assertEqual(["miss"], states)

    def test_digit_join_cannot_invent_a_single_number(self):
        states, _ = nc.token_statuses(["20", "21"], ["2021"], strict=True)
        self.assertTrue(all(s == "miss" for s in states))

    def test_ocr_join_preserves_the_negation_letters(self):
        states, _ = nc.token_statuses(["لا", "يجوز"], ["لايجوز"], strict=True)
        self.assertEqual(["merge", "merge"], states)


if __name__ == "__main__":
    unittest.main()
