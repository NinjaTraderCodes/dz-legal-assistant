import unittest

import jo_layer


class Clean(unittest.TestCase):
    def test_arabic_indic_digits_become_western(self):
        self.assertEqual("المادة 534", jo_layer.clean("المادة 53٤"))
        self.assertEqual("1429", jo_layer.clean("1٤29"))

    def test_kashida_runs_and_their_spaces_are_removed(self):
        self.assertEqual("يتم بطلب", jo_layer.clean("ي ــــ ت ــــ م ب ــــ ط ــــ لب"))
        self.assertEqual("المحكمة", jo_layer.clean("المـحكمة"))

    def test_detached_shadda_joins_its_word(self):
        self.assertEqual("المادة", jo_layer.clean("الماد ّة"))
        self.assertEqual("يحول", jo_layer.clean("يحو ّ ل"))
        self.assertEqual("ويبلغ", jo_layer.clean("ويبّلغ"))
        self.assertEqual("إلا", jo_layer.clean("إ ّلا"))

    def test_word_final_tanween_keeps_the_next_word_space(self):
        self.assertEqual("بصفة مدع أو مدعى", jo_layer.clean("بصفة مد ّع ٍ ٍ أو مد ّعى"))
        self.assertEqual("من قاض فرد", jo_layer.clean("من قا ٍض فرد"))

    def test_spaces_inside_words_are_not_guessed(self):
        self.assertEqual("يختصالقسم", jo_layer.clean("يختصالقسم"))
        self.assertEqual("ه ذا", jo_layer.clean("ه ذا"))


class Layout(unittest.TestCase):
    def test_classify_by_distance_from_column_edge(self):
        lines = [{"page": 3, "column": "left", "right": 338.0, "y": 700, "text": "wide centred title"},
                 {"page": 3, "column": "left", "right": 286.0, "y": 600, "text": "a"},
                 {"page": 3, "column": "left", "right": 275.0, "y": 580, "text": "b"},
                 {"page": 3, "column": "left", "right": 286.2, "y": 570, "text": "d"},
                 {"page": 3, "column": "left", "right": 200.0, "y": 560, "text": "c"}]
        kinds = [l["kind"] for l in jo_layer.classify(lines)]
        self.assertEqual(["continuation", "continuation", "start", "continuation", "heading"], kinds)

    def test_paragraphs_join_continuations_only(self):
        lines = [{"page": 3, "column": "left", "y": 600, "kind": "start", "text": "تفصل المحكمة"},
                 {"page": 3, "column": "left", "y": 590, "kind": "continuation", "text": "في جميع الدعاوى."},
                 {"page": 3, "column": "left", "y": 580, "kind": "heading", "text": "الفرع الأول"}]
        result = jo_layer.paragraphs(lines)
        self.assertEqual(["تفصل المحكمة في جميع الدعاوى.", "الفرع الأول"], [p["text"] for p in result])
        self.assertEqual(["paragraph", "heading"], [p["kind"] for p in result])


if __name__ == "__main__":
    unittest.main()
