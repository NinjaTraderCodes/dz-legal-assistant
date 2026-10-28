import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import reread


class RereadRecovery(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.image = self.root / "strip.png"
        self.image.write_bytes(b"fictitious image")
        self.binary = self.root / "tesseract.exe"
        self.binary.write_bytes(b"fictitious executable")
        (self.root / "ara.traineddata").write_bytes(b"fictitious model")
        self.pins = patch.multiple(reread, TESSERACT=self.binary, TESSDATA=self.root)
        self.pins.start()
        self.addCleanup(self.pins.stop)

    def response(self):
        return subprocess.CompletedProcess([], 0, stdout=b"fictitious reading")

    def test_completed_reading_survives_restart(self):
        with patch("reread.subprocess.run", return_value=self.response()) as run:
            self.assertEqual("fictitious reading", reread.read_strip(self.image))
            reread.tool_hash.cache_clear()
            self.assertEqual("fictitious reading", reread.read_strip(self.image))
            self.assertEqual(1, run.call_count)

    def test_changed_image_and_model_force_new_readings(self):
        with patch("reread.subprocess.run", return_value=self.response()) as run:
            reread.read_strip(self.image)
            self.image.write_bytes(b"different fictitious image")
            reread.read_strip(self.image)
            (self.root / "ara.traineddata").write_bytes(b"different fictitious model")
            reread.read_strip(self.image)
            self.assertEqual(3, run.call_count)

    def test_failed_process_never_creates_reusable_evidence(self):
        with patch("reread.subprocess.run", side_effect=subprocess.CalledProcessError(1, [])):
            with self.assertRaises(subprocess.CalledProcessError):
                reread.read_strip(self.image)
        self.assertFalse(self.image.with_suffix(".ocr.json").exists())

    def test_inserted_article_paths_are_ascii_and_distinct(self):
        numbers = ("8", "8 مكرر", "8 مكرر 1", "الأولى")
        names = [reread.strip_tag(n) for n in numbers]
        self.assertTrue(all(n.isascii() for n in names))
        self.assertEqual(len(numbers), len(set(names)))


if __name__ == "__main__":
    unittest.main()
