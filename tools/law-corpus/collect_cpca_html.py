#!/usr/bin/env python3
"""Bulk official Arabic article acquisition; the2008 edition still needs22-13 consolidation."""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from html.parser import HTMLParser
import json
import re
import urllib.request
import pipeline

BASE='https://droit.mjustice.gov.dz/sites/default/files/portail/legisl_fr_de_06_au_juil_08/code_proced_civ_et_adm/ar/'
OUT=pipeline.ROOT/'data/law/corpus/research/current-batches/cpca-official-html-2008.json'
FOLDER=pipeline.CACHE/'current-digital/cpca-html'


def printed_number(title):
    # The official348 navigation title prints two tatweels inside المــادة.
    # Recognize that label spelling; retain the raw title and every body glyph.
    match=re.fullmatch(r'المـ*ادة\s+(\d+|الأولى|الاولى)',title.strip())
    if not match:return None
    return '1' if not match[1].isdecimal() else str(int(match[1]))


def navigation(common, tree):
    # Parse data arrays only; never execute downloaded JavaScript.
    arrays={name:json.loads(value) for name,value in re.findall(r'var\s+(\w+)\s*=\s*(\[.*?\]);',common,re.S)}
    levels=json.loads(re.search(r'var\s+tl\s*=\s*(\[.*?\]);',tree,re.S)[1])
    if not len(arrays['ps'])==len(arrays['ts'])==len(levels):
        raise ValueError('Official navigation arrays disagree')
    records=[];stack=[]
    for filename,title,level in zip(arrays['ps'],arrays['ts'],levels):
        while stack and stack[-1][0]>=level:stack.pop()
        number=printed_number(title)
        if number:
            if not re.fullmatch(r'\d+\.html',filename):raise ValueError('Unexpected article filename')
            records.append({'number':number,'printedTitle':title,'url':BASE+'topics/'+filename,
                            'filename':filename,'path':[name for _,name in stack]})
        else:stack.append((level,title))
    numbers=[r['number'] for r in records]
    if len(set(numbers))!=len(numbers):raise ValueError('Duplicate article labels in official catalogue')
    return records


class ArticleHTML(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.area=None;self.depth=0;self.paragraph=None;self.paragraphs=[];self.title=[]
        self.uncollected_content=[]

    def handle_starttag(self,tag,attrs):
        attrs=dict(attrs)
        if tag=='div' and attrs.get('id') in ('content','title'):
            self.area=attrs['id'];self.depth=1
        elif tag=='div' and self.area:self.depth+=1
        if self.area=='content' and tag=='p':self.paragraph=[]
        if self.paragraph is not None and tag=='br':self.paragraph.append(' ')

    def handle_data(self,text):
        if self.area=='title':self.title.append(text)
        elif self.area=='content' and self.paragraph is not None:self.paragraph.append(text)
        elif self.area=='content' and text.strip():self.uncollected_content.append(text)

    def handle_endtag(self,tag):
        if tag=='p' and self.paragraph is not None:
            text=' '.join(''.join(self.paragraph).split())
            if text:self.paragraphs.append(text)
            self.paragraph=None
        if tag=='div' and self.area:
            self.depth-=1
            if self.depth==0:self.area=None


def fetch_article(item, offline):
    folder=FOLDER/item['number'];folder.mkdir(parents=True,exist_ok=True)
    pin_path=folder/'source.json';raw=folder/'page.html'
    try:
        if pin_path.exists():
            pin=pipeline.read_json(pin_path)
            if pin['url']!=item['url'] or not raw.exists() or pipeline.hash_file(raw)!=pin['sha256']:
                raise ValueError('Cached official article differs from its pin')
        else:
            if offline:raise ValueError('Article absent from offline cache')
            request=urllib.request.Request(pipeline.official_url(item['url']),headers={'User-Agent':'DZLegal-LawCorpus/1.0'})
            opener=urllib.request.build_opener(pipeline.OfficialRedirect())
            with opener.open(request,timeout=20) as response:
                data=response.read();pipeline.official_url(response.url)
            temporary=raw.with_suffix('.partial');temporary.write_bytes(data);temporary.replace(raw)
            pin={'url':item['url'],'sha256':pipeline.hash_file(raw),'retrievedOn':datetime.now(timezone.utc).date().isoformat(),
                 'encoding':'windows-1256, declared by the official static HTML'}
            pipeline.write_json(pin_path,pin)
        parser=ArticleHTML();parser.feed(raw.read_bytes().decode('windows-1256'))
        title=' '.join(''.join(parser.title).split())
        if printed_number(title)!=item['number'] or not parser.paragraphs or parser.uncollected_content:
            raise ValueError('Article title/body does not match its independent navigation label')
        return item|{'paragraphs':parser.paragraphs,'text':'\n'.join(parser.paragraphs),'source':pin,
                     'acquisitionState':'official-html-extracted','currentness':'2008 base; reconcile22-13 before current-law use',
                     'verification':'not a PDF/OCR or legal-applicability check'}
    except Exception as error:return item|{'acquisitionState':'failed','error':str(error)}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--offline',action='store_true')
    args=parser.parse_args();digital=pipeline.CACHE/'current-digital'
    common=digital/'cpca-common-data.js';tree=digital/'cpca-tree-data.js'
    items=navigation(common.read_bytes().decode('windows-1256'),tree.read_bytes().decode('windows-1256'))
    expected={str(n) for n in range(1,1066)}
    if {i['number'] for i in items}!=expected:raise ValueError('Official base catalogue is not exactly1-1065')
    rows=[]
    def checkpoint():
        failures=[r for r in rows if r['acquisitionState']=='failed']
        pipeline.write_json(OUT,{'codeId':'cpca','sourceEdition':'Law08-09, original2008 statutory text',
                 'catalogue':{'url':BASE+'scripts/HelpCommonData.js','sha256':pipeline.hash_file(common),
                              'treeUrl':BASE+'scripts/HelpTreeData.js','treeSha256':pipeline.hash_file(tree)},
                 'expectedArticles':1065,'acquiredArticles':len(rows)-len(failures),'attemptedArticles':len(rows),
                 'failures':[{'number':r['number'],'error':r['error']} for r in failures],
                 'normalizations':['HTML entities decoded; declaredWindows-1256 decoded','HTML presentation whitespace collapsed within each published paragraph; paragraph boundaries preserved','No Arabic letter/digit/punctuation folding'],
                 'currentConsolidationComplete':False,'pdfAndOcrChecksComplete':False,
                 'note':'Bulk source acquisition only. Amended/repealed2008 wording must not be presented as current without applying22-13 and independent inventory/quality checks.',
                 'articles':sorted(rows,key=lambda r:int(r['number']))})
    with ThreadPoolExecutor(max_workers=4) as pool:
        futures=[pool.submit(fetch_article,item,args.offline) for item in items]
        for future in as_completed(futures):
            rows.append(future.result())
            if len(rows)%25==0 or len(rows)==len(items):
                checkpoint();print('Acquired/attempted',sum(r['acquisitionState']!='failed' for r in rows),'/',len(rows),'of',len(items),flush=True)
    if any(r['acquisitionState']=='failed' for r in rows):raise SystemExit(2)


if __name__=='__main__':main()
