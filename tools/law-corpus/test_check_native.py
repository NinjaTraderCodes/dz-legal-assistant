import unittest

import check_native
import native_check as nc


class Leaders(unittest.TestCase):
    def test_dot_leaders_are_dropped_from_ocr_but_single_stops_kept(self):
        stream = [{"token": t} for t in ["تنفيذي", ".", ".", ".", ".", "(", "الباقي", ")", "الدولة", "."]]
        self.assertEqual(["تنفيذي", "(", "الباقي", ")", "الدولة", "."], [t["token"] for t in check_native.without_leaders(stream)])

    def test_dot_leaders_are_dropped_from_native_text(self):
        result = check_native.comparable("تنفيذي. ..........(الباقي بدون تغيير).....")
        self.assertEqual(nc.tokens("تنفيذي. (الباقي بدون تغيير)"), nc.tokens(result))

    def test_search_window_grows_with_the_article(self):
        self.assertLess(check_native.search_window(["a"] * 10), check_native.search_window(["a"] * 100))


class Boundaries(unittest.TestCase):
    def test_running_header_lines_are_dropped_only_at_the_top(self):
        rows = [("الجريدة الرسمية للجمهورية مزائريةالعدد", 0), ("23 أبريل", 1), ("تفصل المحكمة", 2), ("في", 3), ("الدعاوى", 4),
                ("بعد", 5), ("الجريدة الرسمية", 9)]
        stream = [{"token": t, "source": "p3", "line": line} for text, line in rows for t in nc.tokens(text)]
        kept = [t["token"] for t in check_native.without_headers(stream)]
        self.assertEqual(["تفصل", "المحكمة", "في", "الدعاوى", "بعد", "الجريدة", "الرسمية"], kept)

    def test_only_labels_and_headings_count_as_a_structural_tail(self):
        self.assertTrue(check_native.structural_tail(nc.tokens("المادة 12 : الفصل الثاني في")))
        self.assertFalse(check_native.structural_tail(nc.tokens("المادة يجوز")))

    def test_a_heading_opened_by_the_next_article_is_a_structural_tail(self):
        this = {"path": ["الكتاب الأول"]}
        following = {"path": ["الكتاب الأول", "الفصل الخامس - في الخبرة"]}
        words = check_native.opening_headings(this, following)
        self.assertTrue(check_native.structural_tail(nc.tokens("الفصل الخامس في الخبرة المادة"), words))
        self.assertFalse(check_native.structural_tail(nc.tokens("الفصل الخامس في الخبرة المادة"), set()))

    def test_span_is_cut_at_the_next_label_but_not_at_a_reference(self):
        span = nc.tokens("المادة 804 : خلافا لأحكام المادة 803 أعلاه، ترفع الدعاوى. ”المادة 805 : تكون")
        trimmed = check_native.trim_at_next_label(span, 2, "805")
        self.assertEqual(nc.tokens("المادة 804 : خلافا لأحكام المادة 803 أعلاه، ترفع الدعاوى."), trimmed)


if __name__ == "__main__":
    unittest.main()


class GlobalSpans(unittest.TestCase):
    def test_articles_are_placed_in_order_despite_junk_and_a_repeated_phrase(self):
        a = nc.tokens("المادة 1 : تطبق أحكام هذا القانون على الدعاوى المرفوعة أمام الجهات القضائية.")
        b = nc.tokens("المادة 2 : يجوز لكل شخص يدعي حقا رفع دعوى أمام القضاء.")
        c = nc.tokens("المادة 3 : تطبق أحكام هذا القانون فور سريانه.")
        stream = nc.tokens("الجريدة الرسمية") + a + nc.tokens("| ؛ ،") + b[:6] + b[8:] + c
        spans = check_native.global_spans([a, b, c], stream)
        self.assertEqual(a, stream[spans[0][0]:spans[0][1]][:len(a)])
        self.assertLessEqual(spans[0][1] - spans[0][0], len(a) + check_native.SLACK)
        self.assertEqual(c, stream[spans[2][0]:spans[2][1]])
        self.assertLess(spans[0][1], spans[1][0] + 1)
        self.assertLessEqual(spans[1][1], spans[2][0])

    def test_unplaceable_article_is_not_located(self):
        a = nc.tokens("المادة 1 : تطبق أحكام هذا القانون على الدعاوى المرفوعة.")
        b = nc.tokens("المادة 2 : نص لا يوجد في القراءة إطلاقا هنا.")
        self.assertIsNone(check_native.global_spans([a, b], a)[1])
