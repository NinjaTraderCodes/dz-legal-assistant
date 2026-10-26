import unittest
from collect_family_native import arabic_column


class FamilyColumnTests(unittest.TestCase):
    def test_rotated_right_column_uses_unrotated_crop_coordinates(self):
        self.assertEqual(arabic_column([10,20,110,220],-90),(10,20,110,120))

    def test_changed_orientation_requires_new_layout_review(self):
        with self.assertRaises(ValueError):arabic_column([10,20,110,220],0)

    def test_invalid_crop_fails(self):
        with self.assertRaises(ValueError):arabic_column([110,20,10,220],270)


if __name__=='__main__':unittest.main()
