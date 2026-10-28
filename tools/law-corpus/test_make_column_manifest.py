import unittest

import numpy as np

import make_column_manifest as columns


class ColumnSeparation(unittest.TestCase):
    def image(self):
        gray = np.full((500, 1000), 255, dtype=np.uint8)
        for y in range(60, 460, 20):
            gray[y:y + 4, 40:530] = 0
            gray[y:y + 4, 570:960] = 0
        return gray

    def test_no_rule_uses_displaced_white_gutter(self):
        result = columns.page_columns(self.image())
        split = result["right"][0]
        self.assertGreaterEqual(split, 0.530)
        self.assertLess(split, 0.570)
        self.assertEqual(split, result["left"][2])
        self.assertIn("least-inked", result["note"])

    def test_column_rule_is_preserved(self):
        gray = self.image()
        gray[50:490, 550:552] = 0
        result = columns.page_columns(gray)
        self.assertIsNone(result["note"])
        self.assertLess(result["left"][2], 0.550)
        self.assertGreater(result["right"][0], 0.552)


if __name__ == "__main__":
    unittest.main()
