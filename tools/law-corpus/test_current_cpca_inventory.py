import unittest
from current_cpca_inventory import quoted_targets


class QuotedLabelTests(unittest.TestCase):
    def test_dot_variant_and_bis_are_labels_not_body_normalizations(self):
        self.assertEqual(quoted_targets('« Art. 11. — text » « Art 12 bis 1. — text »'),['11','12 bis 1'])

    def test_unquoted_cross_reference_is_not_a_published_target(self):
        self.assertEqual(quoted_targets('les articles 11 et 12; Art. 4. — instruction'),[])


if __name__=='__main__':unittest.main()
