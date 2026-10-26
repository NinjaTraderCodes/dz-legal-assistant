import unittest
from collect_civil_native import page_labels,without_footnotes


class NativeBlockTests(unittest.TestCase):
    def test_pdf_bidi_colon_and_font_spacing_are_label_variants(self):
        self.assertEqual(page_labels('المادة :745 body')[0]['canonicalNumberCandidate'],'745')
        self.assertEqual(page_labels('المѧ ادة 909 : body')[0]['canonicalNumberCandidate'],'909')
        self.assertEqual(page_labels('المادة41 : body')[0]['canonicalNumberCandidate'],'41')

    def test_label_decomposition_preserves_raw_body(self):
        raw='المادة 4 : لا 123.\nالمادة 5 : كلام'
        labels=page_labels(raw)
        self.assertEqual([l['canonicalNumberCandidate'] for l in labels],['4','5'])
        self.assertEqual(raw[labels[0]['end']:labels[1]['start']],' لا 123.\n')

    def test_font_stretch_in_label_and_bis_suffix(self):
        label=page_labels('المѧادة 13 مكرر 1 : body')[0]
        self.assertEqual(label['canonicalNumberCandidate'],'13 مكرر 1')
        self.assertEqual(page_labels('المادة 13 مكرر1 : body')[0]['canonicalNumberCandidate'],'13 مكرر 1')

    def test_historical_footnote_is_not_current_body(self):
        self.assertEqual(without_footnotes('current\n_________\nold'),'current\n')

    def test_reversed_suffix_index_is_kept_out_of_body(self):
        raw='المادة 8 مكرر :1 (source annotation) body'
        labels=page_labels(raw)
        self.assertEqual(len(labels),1)
        self.assertEqual(labels[0]['canonicalNumberCandidate'],'8 مكرر 1')
        self.assertEqual(raw[labels[0]['end']:],' (source annotation) body')

    def test_joined_pdf_lines_need_explicit_inline_label_mode(self):
        raw='المادة :130 first.المادة :131 second. Reference المادة 132.'
        self.assertEqual(len(page_labels(raw)),1)
        self.assertEqual([x['canonicalNumberCandidate'] for x in page_labels(raw,allow_inline=True)],['130','131'])


if __name__=='__main__':unittest.main()
