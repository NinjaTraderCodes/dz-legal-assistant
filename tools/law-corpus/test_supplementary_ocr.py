import tempfile
import unittest
from pathlib import Path
import pipeline
from supplementary_ocr import binding_for, replay_record


class ReplayTests(unittest.TestCase):
    def test_binding_changes_for_crop_runtime_and_implementation(self):
        first = binding_for({'crop':'one'}, {'model':'a'}, {'script':'a'})
        self.assertNotEqual(first, binding_for({'crop':'two'}, {'model':'a'}, {'script':'a'}))
        self.assertNotEqual(first, binding_for({'crop':'one'}, {'model':'b'}, {'script':'a'}))
        self.assertNotEqual(first, binding_for({'crop':'one'}, {'model':'a'}, {'script':'b'}))

    def test_missing_or_mutated_raw_output_refuses_resume(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            for name in ['raw.json','ocr.txt']:
                (folder/name).write_text('fictitious',encoding='utf-8')
            row={'binding':'test','status':'extracted','artifacts':[{'file':n,'sha256':pipeline.hash_file(folder/n)} for n in ['raw.json','ocr.txt']]}
            self.assertTrue(replay_record(row, folder, 'test'))
            (folder/'ocr.txt').write_text('changed',encoding='utf-8')
            with self.assertRaises(ValueError):replay_record(row, folder, 'test')
            self.assertFalse(replay_record(row, folder, 'different'))


if __name__ == '__main__':
    unittest.main()
