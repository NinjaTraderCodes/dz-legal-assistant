#!/usr/bin/env python3
"""Collect the official current-edition PDF's article blocks without repairing its bad font encoding."""
from collections import Counter
from contextlib import closing
import re
import unicodedata
import pipeline

STRETCH='[\u0640\u0467 \t]*'
LABEL=STRETCH.join('المادة')
NUMBER=r'([0-9]+(?:[.,][0-9]{3})*|الأولى|الاولى)'
SUFFIX=r'(?:\s*('+STRETCH.join('مكرر')+r')(?:\s*([0-9]+))?)?'
BIS=STRETCH.join('مكرر')
LABEL_BODIES=[LABEL+r'\s*'+NUMBER+r'\s*('+BIS+r')\s*:([0-9]+)(?=\s*\()',
              LABEL+r'\s*'+NUMBER+SUFFIX+r'\s*:',
              LABEL+r'\s*:\s*'+NUMBER+SUFFIX]


def label_view(raw):
    # Compatibility decomposition is restricted to locating source labels. Never
    # copy this derived string into an article body or use it as a text check.
    output=[];offsets=[]
    for i,char in enumerate(raw):
        value=unicodedata.normalize('NFKC',char)
        output.append(value);offsets.extend([i]*len(value))
    return ''.join(output),offsets


def page_labels(raw, *, allow_inline=False):
    normalized,offsets=label_view(raw)
    found=[]
    anchor='' if allow_inline else r'(?m)^[ \t]*'
    patterns=[re.compile(anchor+body) for body in LABEL_BODIES]
    matches=sorted([m for pattern in patterns for m in pattern.finditer(normalized)],key=lambda m:(m.start(),-m.end()))
    previous_end=-1
    for match in matches:
        if match.start()<previous_end:continue
        previous_end=match.end()
        number='1' if match[1] in {'الأولى','الاولى'} else str(int(match[1].replace('.','').replace(',','')))
        if match[2]:number+=' مكرر'+(' '+match[3] if match[3] else '')
        start=offsets[match.start()];end=offsets[match.end()-1]+1
        found.append({'canonicalNumberCandidate':number,'rawLabel':raw[start:end],'start':start,'end':end})
    return found


def without_footnotes(raw):
    separator=re.search(r'_{4,}',raw)
    return raw[:separator.start()] if separator else raw


def main():
    import pypdfium2 as pdfium
    folder=pipeline.CACHE/'current-digital';pdf=folder/'civil-consolidated-ar.pdf'
    sources=pipeline.read_json(pipeline.ROOT/'data/law/corpus/research/current-digital-sources.json')
    source=next(s for s in sources['sources'] if s['id']=='civil-consolidated-ar')
    if pipeline.hash_file(pdf)!=source['sha256']:raise ValueError('Official Civil compilation differs from pin')
    pages=[];records=[];current=None;started=False
    with closing(pdfium.PdfDocument(pdf)) as document:
        for index in range(len(document)):
            page=document[index];textpage=page.get_textpage()
            raw=textpage.get_text_range(index=0,count=textpage.count_chars())
            textpage.close();page.close()
            (folder/'civil-native-pages').mkdir(exist_ok=True)
            (folder/'civil-native-pages'/f'{index+1:04}.txt').write_text(raw,encoding='utf-8',newline='')
            body=without_footnotes(raw);labels=page_labels(body)
            if not started:
                if not labels or labels[0]['canonicalNumberCandidate']!='1':continue
                started=True
            pages.append({'pdfPage':index+1,'rawTextSha256':pipeline.hash_text(raw),'currentBodyBeforeFootnotesSha256':pipeline.hash_text(body)})
            cursor=0;ends_code=False
            for label in labels:
                if current:
                    current['fragments'].append({'pdfPage':index+1,'start':cursor,'end':label['start'],'rawText':body[cursor:label['start']]})
                    records.append(current)
                number=label['canonicalNumberCandidate']
                current={'candidateId':'civil-native:'+number.replace(' مكرر','-bis').replace(' ','-')+f"@p{index+1}:{label['start']}",
                         'canonicalNumberCandidate':number,'rawPrintedLabel':label['rawLabel'],
                         'labelPdfPage':index+1,'fragments':[]}
                cursor=label['end']
                if number=='1003':ends_code=True
            if current:current['fragments'].append({'pdfPage':index+1,'start':cursor,'end':len(body),'rawText':body[cursor:]})
            if ends_code:break
    if current:records.append(current)
    counts=Counter(r['canonicalNumberCandidate'] for r in records)
    expected={str(n) for n in range(1,1004)}
    repeal_inventory=pipeline.read_json(pipeline.ROOT/'data/law/corpus/research/civil-repeal-discovery.json')
    repealed=set()
    for row in repeal_inventory['records']:
        if pipeline.hash_file(pipeline.CACHE/(row['sourceId']+'.pdf'))!=row['sha256']:
            raise ValueError('Independent repeal source differs from recorded publication')
        repealed.update(row['numbers'])
    result={'codeId':'civil-code','source':source,'edition':'SGG2007 compilation linked by Justice Ministry',
            'acquisitionState':'raw article blocks collected; font decoding, layout and current-amendment QA incomplete',
            'articleBlockCount':len(records),'distinctNumberCandidates':len(counts),
            'originalBaseNumbersObserved':len(expected&set(counts)),
            'baseNumbersNotObserved':sorted(expected-set(counts),key=int),
            'baseLabelsAbsentButInKnownRepealInventory':sorted((expected-set(counts))&repealed,key=int),
            'baseLabelsAbsentWithoutRepealExplanation':sorted(expected-set(counts)-repealed,key=int),
            'repealInventoryEvidence':'civil-repeal-discovery.json; absent labels are never inferred repealed',
            'duplicateNumberCandidates':{k:v for k,v in counts.items() if v>1},
            'labelProcessing':'NFKC only for label discovery with offsets back to raw text; recognizes published stretch glyphs. Body text is untouched.',
            'warnings':['The source font maps some glyphs to wrong Unicode characters. No guessed Arabic letter repair is applied.',
                        'Blocks may include headings or editorial amendment markers. These are raw candidates, not canonical paragraphs.',
                        'The source edition and complete amendment inventory must be reconciled before current-law use.',
                        'Unobserved numbers are not inferred repealed; duplicate local numbering must not overwrite an identity.',
                        'Historical footnotes after literal underscore separators are excluded from these current-body blocks; raw full pages remain cached.'],
            'currentConsolidationComplete':False,'pdfAndOcrChecksComplete':False,'pages':pages,'articles':records}
    pipeline.write_json(pipeline.ROOT/'data/law/corpus/research/current-batches/civil-official-native-2007.json',result)
    print('Civil source blocks:',len(records),'distinct numbers:',len(counts),'base labels:',result['originalBaseNumbersObserved'],'/1003; duplicates:',len(result['duplicateNumberCandidates']))


if __name__=='__main__':main()
