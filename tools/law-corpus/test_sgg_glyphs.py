import unittest

import sgg_glyphs


class DecodeObject(unittest.TestCase):
    def test_kaf_glyph_read_as_madda_becomes_kaf(self):
        # «آان» is logical; glyphs are visual order: alef-final, alef, kaf-form 987.
        self.assertEqual(("كان", None), sgg_glyphs.decode_object("آان", [1015, 910, 987]))

    def test_real_madda_is_kept(self):
        self.assertEqual(("الآخر", None), sgg_glyphs.decode_object("الآخر", [942, 964, 897, 991, 909]))

    def test_order_pairs_mixed_madda(self):
        # Two «آ» characters: the visually first glyph pairs with the logically last character.
        fixed, reason = sgg_glyphs.decode_object("آآ", [897, 987])
        self.assertEqual("كآ", fixed)

    def test_counts_that_do_not_pair_are_flagged(self):
        fixed, reason = sgg_glyphs.decode_object("الآخر", [942, 964, 991, 909])
        self.assertIsNone(fixed)
        self.assertIn("«آ»", reason)

    def test_stretch_glyph_character_becomes_tatweel(self):
        self.assertEqual(("تعتمـد", None), sgg_glyphs.decode_object("تعتمѧد", [1, 2, 1127, 3, 4]))


if __name__ == "__main__":
    unittest.main()
