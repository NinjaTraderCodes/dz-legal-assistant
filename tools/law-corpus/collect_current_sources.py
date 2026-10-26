#!/usr/bin/env python3
"""Acquire linked official digital editions before resorting to source-scan OCR."""
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path
import urllib.parse
import urllib.request
import pipeline


class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links=[]

    def handle_starttag(self, tag, attrs):
        values=dict(attrs)
        for key in ('href','src'):
            if values.get(key):self.links.append(values[key])


def main():
    root_url='https://droit.mjustice.gov.dz/ar'
    folder=pipeline.CACHE/'current-digital'
    folder.mkdir(parents=True,exist_ok=True)
    registry=pipeline.ROOT/'data/law/corpus/research/current-digital-sources.json'
    previous={s['id']:s for s in pipeline.read_json(registry)['sources']} if registry.exists() else {}
    def acquire(item):
        sid,url,kind,found=item
        pipeline.official_url(url)
        path=folder/(sid+('.pdf' if kind=='pdf' else '.js' if kind=='js' else '.html'))
        if sid in previous:
            old=previous[sid]
            if old['url']!=url or not path.exists() or pipeline.hash_file(path)!=old['sha256']:
                raise ValueError('Current-source cache differs from pin: '+sid)
            return old
        opener=urllib.request.build_opener(pipeline.OfficialRedirect())
        with opener.open(urllib.request.Request(url,headers={'User-Agent':'DZLegal-LawCorpus/1.0'}),timeout=30) as response:
            data=response.read();final=response.url;content_type=response.headers.get('Content-Type','')
        pipeline.official_url(final)
        if kind=='pdf' and not data.startswith(b'%PDF-'):
            raise ValueError('Official URL did not return a PDF: '+url)
        temporary=path.with_suffix(path.suffix+'.partial');temporary.write_bytes(data);temporary.replace(path)
        return {'id':sid,'url':url,'finalUrl':final,'sha256':pipeline.hash_file(path),'bytes':len(data),
                'retrievedOn':datetime.now(timezone.utc).date().isoformat(),'contentType':content_type,
                'kind':kind,'discoveredFrom':found,'versionStatus':'edition and latest-amendment reconciliation pending'}
    root=acquire(('ministry-root-ar',root_url,'html',root_url))
    html=(folder/'ministry-root-ar.html').read_text(encoding='utf-8')
    parser=Links();parser.feed(html)
    items=[]
    for link in parser.links:
        url=urllib.parse.urljoin(root_url,link)
        if url.endswith('/ACivil.pdf'):items.append(('civil-consolidated-ar',url,'pdf',root_url))
        if url.endswith('/code_famille.pdf'):items.append(('family-consolidated-bilingual',url,'pdf',root_url))
    items=list(dict.fromkeys(items))
    items.append(('cpca-html-index','https://droit.mjustice.gov.dz/sites/default/files/portail/legisl_fr_de_06_au_juil_08/code_proced_civ_et_adm/ar/index.html','html','Official CPCA article HTML links to ../index.html'))
    cpca='https://droit.mjustice.gov.dz/sites/default/files/portail/legisl_fr_de_06_au_juil_08/code_proced_civ_et_adm/ar/'
    items.extend([('cpca-common-data',cpca+'scripts/HelpCommonData.js','js',cpca+'HelpTree.html'),
                  ('cpca-tree-data',cpca+'scripts/HelpTreeData.js','js',cpca+'HelpTree.html'),
                  ('family-original-84-11','https://droit.mjustice.gov.dz/sites/default/files/84-11_.pdf','pdf','Official Family page discovery-links.json: Arabic link is the1984 original, not a consolidated code')])
    with ThreadPoolExecutor(max_workers=3) as pool:rows=[root]+list(pool.map(acquire,items))
    for row in rows:
        if row['id']=='family-consolidated-bilingual':
            row['language']='ara+fra'
            row['versionStatus']='Visible Arabic/French compilation incorporating05-02. Latest-amendment reconciliation pending; related annexes excluded.'
    pipeline.write_json(registry,{'recordedOn':root['retrievedOn'],'note':'Official linked digital editions. Acquisition is not certification that consolidation covers every later amendment. Raw PDFs/HTML remain ignored.','sources':rows})
    for row in rows:print(row['id'],row['bytes'],row['sha256'],flush=True)
    index=(folder/'cpca-html-index.html').read_bytes().decode('windows-1256')
    parser=Links();parser.feed(index)
    pipeline.write_json(folder/'cpca-index-links.json',{'links':parser.links})


if __name__=='__main__':main()
