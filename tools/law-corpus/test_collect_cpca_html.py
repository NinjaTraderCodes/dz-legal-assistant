import json
import unittest
from collect_cpca_html import navigation, ArticleHTML, printed_number


class NativeArticleTests(unittest.TestCase):
    def test_unstructured_statutory_text_is_not_silently_dropped(self):
        parser=ArticleHTML();parser.feed('<div id="content">important outside<p>inside</p></div>')
        self.assertEqual(parser.uncollected_content,['important outside'])

    def test_stretched_official_label_is_identity_only(self):
        self.assertEqual(printed_number('المــادة 348'),'348')

    def test_title_and_paragraphs_exclude_navigation(self):
        parser=ArticleHTML()
        parser.feed('<div id="title">المادة 7</div><div id="content"><p>لفظ &amp; آخر</p><p>لا 123.</p></div><div id="related"><p>خارج</p></div>')
        self.assertEqual(parser.paragraphs,['لفظ & آخر','لا 123.'])
        self.assertEqual(printed_number(''.join(parser.title)),'7')

    def test_navigation_does_not_execute_javascript_and_preserves_heading_path(self):
        common='var ps = '+json.dumps(['index.html','2.html'])+'; var ts = '+json.dumps(['heading','المادة 1'])+'; throw "never execute";'
        records=navigation(common,'var tl = [1,2];')
        self.assertEqual(records[0]['path'],['heading'])
        self.assertEqual(records[0]['number'],'1')

    def test_duplicate_or_disagreeing_catalogue_refused(self):
        common='var ps = ["2.html","3.html"]; var ts = ["المادة 1","المادة 1"];'
        with self.assertRaises(ValueError):navigation(common,'var tl = [1,1];')


if __name__=='__main__':unittest.main()
