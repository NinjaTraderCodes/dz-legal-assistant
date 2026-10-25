"""Invented source-label and catalogue fixtures; no inferred legal numbering."""
from pathlib import Path
import tempfile
import unittest

import index_article_labels
import pipeline
import scope_inventory


class LabelTests(unittest.TestCase):
    def test_number_order_has_no_hash_seed_dependent_ties_between_base_and_bis(self):
        numbers = ['187 مكرر 1', '187 مكرر', '188', '187']
        self.assertEqual(['187', '187 مكرر', '187 مكرر 1', '188'], sorted(numbers, key=scope_inventory.number_key))
        self.assertEqual(len(numbers), len({scope_inventory.number_key(n) for n in numbers}))

    def test_only_article_headers_count_not_references_in_body_sentences(self):
        text = 'Article 1er. — Invented.\nSee Art. 2 above.\nArt, 3. — Another invented heading.\nArt. $. — Unreadable number.\n'
        self.assertEqual(['1', '3'], [x['numberFromOcr'] for x in index_article_labels.labels(text, 'fra')])

    def test_printed_number_observation_never_rewrites_ocr_body(self):
        text = '\u200fالمادة ١.٠٠٢ : نص وهمي.\nوبمقتضى المادة 7 أعلاه.\n'
        found = index_article_labels.labels(text, 'ara')
        self.assertEqual(['1002'], [x['numberFromOcr'] for x in found])
        self.assertIn('١.٠٠٢', found[0]['ocrLabel'])
        self.assertIn('١.٠٠٢', text)

    def test_catalogue_seed_deduplication_preserves_file_hashes_and_all_references(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'invented.json'
            pipeline.write_json(path, {'claims': [{'articles': [{'code': 'fiction', 'number': '9'},
                        {'code': 'fiction', 'number': '9'}, {'code': 'fiction', 'number': '10'}]}]})
            seeds, pins = scope_inventory.catalogue_seeds(path.parent)
            self.assertEqual({'fiction': ['9', '10']}, seeds)
            self.assertEqual(pipeline.hash_file(path), pins[0]['sha256'])


if __name__ == '__main__':
    unittest.main()
