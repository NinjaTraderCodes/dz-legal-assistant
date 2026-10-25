"""Fictitious page geometry only."""
from PIL import Image, ImageDraw
import unittest
import civil_layout


class GeometryTests(unittest.TestCase):
    def test_tracks_shifted_slanted_separator_without_using_ocr(self):
        image = Image.new('L', (600, 800), 255)
        draw = ImageDraw.Draw(image)
        draw.line([(315, 20), (325, 799)], fill=0, width=2)
        for y in range(90, 750, 20):
            draw.rectangle((60, y, 290, y + 4), fill=0)
            draw.rectangle((345, y, 570, y + 4), fill=0)
        result = civil_layout.separator(image)
        self.assertGreater(result['darkPixelCoverage'], .9)
        self.assertAlmostEqual(320 / 600, result['centerAtHalfHeight'], delta=.004)
        self.assertGreater(result['deltaAcrossHeight'], 0)

    def test_blank_page_never_has_a_confident_separator(self):
        result = civil_layout.separator(Image.new('L', (600, 800), 255))
        self.assertEqual(0, result['darkPixelCoverage'])


if __name__ == '__main__':
    unittest.main()
