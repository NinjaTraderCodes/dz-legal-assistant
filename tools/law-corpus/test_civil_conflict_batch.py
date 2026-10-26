import unittest
import pipeline
from build_civil_conflict_batch import correct_transcription


class CorrectionTests(unittest.TestCase):
    def test_correction_retains_dates_and_other_historical_fields(self):
        version={'versionId':'fiction:1@old','text':'wrong','inForceFrom':'1900-01-01','introducedBy':'old'}
        decision={'versionId':version['versionId'],'beforeTextSha256':pipeline.hash_text('wrong'),
                  'replacement':{'text':'correct','paragraphs':['correct']}}
        changed=correct_transcription(version, decision)
        self.assertEqual(changed['text'],'correct')
        self.assertEqual(changed['inForceFrom'],'1900-01-01')
        self.assertEqual(version['text'],'wrong')

    def test_wrong_historical_body_is_rejected(self):
        with self.assertRaises(ValueError):
            correct_transcription({'versionId':'fiction:1@old','text':'another'},
                {'versionId':'fiction:1@old','beforeTextSha256':pipeline.hash_text('wrong'),
                 'replacement':{'text':'correct','paragraphs':['correct']}})

    def test_correction_cannot_smuggle_a_new_commencement_date(self):
        with self.assertRaises(ValueError):
            correct_transcription({'versionId':'fiction:1@old','text':'wrong'},
                {'versionId':'fiction:1@old','beforeTextSha256':pipeline.hash_text('wrong'),
                 'replacement':{'text':'correct','paragraphs':['correct'],'inForceFrom':'2000-01-01'}})


if __name__ == '__main__':
    unittest.main()
