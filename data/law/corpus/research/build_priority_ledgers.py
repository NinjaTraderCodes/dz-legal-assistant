"""Build expressly scoped priority ledgers from pinned official-download metadata."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TODAY = '2026-10-25'
INDEX = 'https://droit.mjustice.gov.dz/fr/content/codes-0'
DOWNLOADS = {x['id']: x for x in json.loads((ROOT / 'research/source-downloads.json').read_text(encoding='utf-8'))}

def download(key):
    source = DOWNLOADS[key]
    return {field: source[field] for field in ('url', 'sha256', 'retrievedOn')}

def publication(id, kind, number, date, issue, jo_date, pages, arabic, french, touched, start, basis, transitions, note):
    return {
        'id': id, 'kind': kind, 'number': number, 'date': date,
        'jo': {'issue': issue, 'date': jo_date, 'pages': pages},
        'arabic': download(arabic), 'french': download(french) if french else None,
        'articlesTouched': touched, 'commencementDate': start,
        'commencementBasis': basis, 'transitionalProvisions': transitions,
        'discovery': [
            {'url': INDEX, 'accessedOn': TODAY, 'note': 'Justice Ministry amendment index consulted for discovery only; priority subset, not an exhaustive whole-code inventory.'},
            {'url': DOWNLOADS[arabic]['url'], 'accessedOn': TODAY, 'note': note}
        ]
    }

def ledger(code, citation, reference, targets, unsettled, publications):
    result = {'schemaVersion': '1.0', 'id': code, 'citationName': citation, 'reference': reference,
              'scope': {'kind': 'priority-subset', 'targetArticles': targets,
                        'amendmentDiscoveryThrough': TODAY, 'complete': False, 'unsettled': unsettled},
              'publications': publications}
    target = ROOT / code / 'ledger.json'
    target.parent.mkdir(exist_ok=True)
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

civil_targets = ['7', '138', '139', '140', '308', '309', '379', '1002', '1003']
civil_2005_touched = ['6','7','8','10','11','12','13 مكرر','13 مكرر 1','15','16','17','17 مكرر','18','19','21 مكرر','22','23','23 مكرر','23 مكرر 1','23 مكرر 2','24','25','36','38','39','41','42','43','49','52','54','72 مكرر','78','79','80','90','93','96','101','103','115','121','123 مكرر','123 مكرر 1','124','124 مكرر','125','126','129','131','132','133','134','135','136','137','140 مكرر','140 مكرر 1','182 مكرر','323 مكرر','323 مكرر 1','324 مكرر 4','327','333','334','335','336','553','558']

ledger('civil-code', 'القانون المدني', 'الأمر رقم 75-58 المؤرخ في 26 سبتمبر 1975 المتضمن القانون المدني، المعدل والمتمم', civil_targets,
       [
           'This is a priority subset; the rest of the Civil Code is outside its inventory.',
           'Law05-10 replaced art7. No special commencement date appears in its final art52; Civil4 provides a geographic day-franc rule, so one uniform national commencement date and the exact art7 version boundary remain unsettled.',
           'Justice Ministry index also lists83-01,88-14,89-01 and07-05. Official issues for these are cached and reviewed for priority impact, with findings in research/amendment-discovery.json; they are not represented as fully extracted whole-code amendments in this scoped ledger.',
           'The amendment index plus targeted official-domain search is evidence of discovery through the recorded date, not proof that no unindexed or indirect legal effect exists.'
       ], [
           publication('ordinance-75-58','ordinance','75-58','1975-09-26','78','1975-09-30',list(range(990,1056)),'A1975078','F1975078',civil_targets,'1975-07-05',
                       'Explicit original art1003, Arabic PDF67/printed1055: commencement from5July1975. French PDF52/printed868 agrees. This does not replace the separate art1002 publication trigger for prescription facts. Art7 preserves old limitation start/suspension/interruption rules for the earlier period. JO pages are printed page labels, not PDF indexes.',
                       ['civil-code:7','civil-code:1002','civil-code:1003'],
                       'Original title Arabic PDF2/printed990. Priority138,139,140 PDF9-10/997-998;308,309 PDF19/1007;379 PDF24/1012;7 PDF2/990;1002,1003 PDF67/1055. Original articlesTouched is expressly the scoped target intersection, not an inventory of all1003 original articles.'),
           publication('law-05-10','law','05-10','2005-06-20','44','2005-06-26',list(range(17,26)),'A2005044','F2005044',civil_2005_touched,None,
                       'No specific calendar commencement in examined Law05-10; final art52 (Arabic PDF25) orders publication only. Original Civil4 (Arabic PDF2/printed990; French PDF2/818) distinguishes Algiers after one clear day from other dairas after arrival and one clear day. A single nationwide start cannot be inferred from enactment or publication alone. Art7 replacement Arabic PDF18/FrenchPDF15.',
                       ['civil-code:7'],
                       'Law title Arabic PDF17. Art3 replaces Civil7 atPDF18. All operative amendment provisions through final52 reviewed using Arabic page images and official French edition; none replaces138,139,140,308,309,379. Art42 inserts140bis and140bis1, distinct from140. French140ter aliases Arabic140bis1; French324quinquies aliases Arabic324bis4.')
       ])

commercial_targets = ['187 مكرر', '187 مكرر 1']
commercial_2005_touched = ['146','169','170','187 مكرر','187 مكرر 1','192','252 مكرر','414','502'] + ['526 مكرر'] + [f'526 مكرر {i}' for i in range(1,17)] + ['538','539'] + [f'543 مكرر {i}' for i in range(19,25)]
ledger('commercial-code','القانون التجاري','الأمر رقم 75-59 المؤرخ في 26 سبتمبر 1975 المتضمن القانون التجاري، المعدل والمتمم', commercial_targets,
       [
           'This is a priority subset; the rest of the Commercial Code is outside its inventory.',
           'Law05-02 art3 inserts187bis and187bis1;187bis expressly applies to leases concluded from publication2005-02-09. The generic commencement date remains null because finalart10 only orders publication and the geographic Civil4 rule has not been reduced to one national date.',
           'Official French publication calls Arabic187bis1 Article187ter. Its identity/paragraph correspondence must retain this alias.',
           'The Ministry index lists87-20,93-08,96-23,96-27,05-02,15-20,22-09. The four pre2005 instruments have not been separately downloaded and their hashes are unknown. Official15-20 and22-09 are cached and their corporate-law amendments reviewed; no priority-article amendment was found in those texts.',
           'The amendment index plus targeted official-domain search is not an exhaustive proof against unindexed or indirect later changes.'
       ], [
           publication('ordinance-75-59','ordinance','75-59','1975-09-26','101','1975-12-19',[1306,1325],'A1975101','F1975101',[],None,
                       'Original commencement clause has not been inspected in this priority research pass; unknown date remains null. Original source is retained to identify the base Code, not to invent a version of later inserted187bis/187bis1.',[],
                       'Original title ArabicPDF2/printed1306. ArabicPDF21/printed1325 has187 immediately followed by the heading ofchapterIV and188; the target bis articles are inserted by05-02, not transcribed from this original publication.'),
           publication('law-05-02','law','05-02','2005-02-06','11','2005-02-09',list(range(8,13)),'A2005011','F2005011',commercial_2005_touched,None,
                       'Art3, ArabicPDF8-9, inserts187bis and187bis1.187bis expressly addresses leases concluded from publication of this law (JO9February2005), and187bis1 preserves the former legislation for renewal of prior leases. Generic national commencement unresolved: finalart10 ArabicPDF12 only orders publication; Civil4 is geographically conditional.',
                       ['commercial-code:187-bis','commercial-code:187-bis-1'],
                       'Official ArabicPDF9/printed9 contains both priority articles. FrenchPDF7/printed7 contains187bis(two paragraphs) and187ter(one paragraph), the latter matching Arabic187bis1. Source finalart10 atArabicPDF12/FrenchPDF9.')
       ])

cpca_2022_touched = ['32','33','531','532','533','534','535','536','536 مكرر']+[f'536 مكرر {i}' for i in range(1,8)]+['600','800','801','804','805','808','809','811','812','813','814','814 مكرر','815','826','828','832','833','834','835','836','837','840','848','849','851','852','875','877','878','882','883','886','891','892','899','900 مكرر']+[f'900 مكرر {i}' for i in range(1,10)]+['901','902','903','907','908','910','911','912','913','914','917','921','931','936','937','938','939','940','941','943','944','945','949','950','951','953','954','959','960','963','966','967','976','986','987','989']
ledger('cpca','قانون الإجراءات المدنية والإدارية','القانون رقم 08-09 المؤرخ في 25 فبراير 2008 المتضمن قانون الإجراءات المدنية والإدارية، المعدل والمتمم',['2','504','1062'],
       [
           'This is a priority subset; the rest of the CPCA is outside its inventory.',
           'Law22-13 is discovered and its operative touched-article list checked; it does not replace2,504 or1062. Its wider jurisdiction and court-installation effects are not represented as a complete consolidation.',
           'Law22-13 art13 defers specified subject-matter/territorial jurisdiction rules to the actual installation dates of the new courts and preserves jurisdiction for previously instituted proceedings. Those installation dates have not been resolved here.',
           'A Ministry narrative page says25February2009 for CPCA commencement; it conflicts with governing art1062, which states one year after publication. The primary JO date23April2008 therefore supports23April2009; the narrative discrepancy is retained in research/temporal-findings.json.',
           'The amendment index plus targeted official-domain search is not an exhaustive proof against unindexed or indirect later changes.'
       ], [
           publication('law-08-09','law','08-09','2008-02-25','21','2008-04-23',list(range(3,96)),'A2008021','F2008021',['2','504','1062'],'2009-04-23',
                       'Art1062 ArabicPDF95/printed95 explicitly states one year after publication. JO cover and page headers give23April2008; adding one calendar year gives23April2009. FrenchPDF83/printed83 agrees. Art2 atArabicPDF3 excepts periods that began under the former Code; this is separate from interval selection.',
                       ['cpca:2','cpca:1062','cpca:1063','cpca:1064'],
                       'Original title and2 ArabicPDF3;504 ArabicPDF41;1062-1065 ArabicPDF95. French2 PDF3,504 PDF36,1062-1065 PDF83. Original articlesTouched records the priority intersection only.'),
           publication('law-22-13','law','22-13','2022-07-12','48','2022-07-17',list(range(3,12)),'A2022048','F2022048',cpca_2022_touched,None,
                       'Art13 ArabicPDF11/FrenchPDF11 makes specified subject-matter/territorial jurisdiction rules begin on installation of the new courts and preserves existing cases. Actual installation dates are not resolved. Finalart15 only orders publication for the law generally; no generic calendar commencement inferred.',
                       ['law-22-13:13','law-22-13:14'],
                       'Justice Ministry index identifies22-13 as the amending law. Full operative text Arabic/FrenchPDF3-11 reviewed for affected-number inventory;2,504,1062 absent.32 only loses paragraphs7-10.532 and536 are restated as unchanged within rewritten chapter; recorded as touched, not as new wording.')
       ])
print('Wrote three scoped ledgers.')
