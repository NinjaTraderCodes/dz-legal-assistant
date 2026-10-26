#!/usr/bin/env python3
"""Collect the Ministry's visible Arabic Family column; preserve raw font errors."""
from collections import Counter
from contextlib import closing
import pipeline
from collect_civil_native import page_labels


def arabic_column(cropbox, rotation):
    # This publication is a -90-degree rotated, bilingual landscape document.
    # Its visible right column is the lower half in unrotated PDF coordinates.
    if rotation % 360 != 270:
        raise ValueError('Family source rotation changed; inspect the layout again')
    left,bottom,right,top=map(float,cropbox)
    if left>=right or bottom>=top:
        raise ValueError('Invalid Family crop box')
    return left,bottom,right,(bottom+top)/2


def main():
    import pypdfium2 as pdfium
    import pypdfium2.raw as raw_api
    from pypdf import PdfReader
    folder=pipeline.CACHE/'current-digital'
    source=next(s for s in pipeline.read_json(pipeline.ROOT/'data/law/corpus/research/current-digital-sources.json')['sources']
                if s['id']=='family-consolidated-bilingual')
    pdf=folder/(source['id']+'.pdf')
    if pipeline.hash_file(pdf)!=source['sha256']:
        raise ValueError('Family compilation differs from pin')
    reader=PdfReader(pdf)
    pages=[];records=[];current=None;started=False;ended=False
    cache=folder/'family-native-pages';cache.mkdir(exist_ok=True)
    with closing(pdfium.PdfDocument(pdf)) as document:
        for index in range(len(document)):
            left,bottom,right,top=arabic_column(reader.pages[index].cropbox,reader.pages[index].rotation)
            page=document[index];textpage=page.get_textpage()
            # Fail rather than silently clipping any Arabic glyph out of the column.
            for i in range(textpage.count_chars()):
                cp=raw_api.FPDFText_GetUnicode(textpage.raw,i)
                if 0x600<=cp<=0x6ff or 0xfb50<=cp<=0xfeff:
                    x0,y0,x1,y1=textpage.get_charbox(i)
                    if not (left-1<=x0<=x1<=right+1 and bottom-1<=y0<=y1<=top+1):
                        raise ValueError(f'Arabic glyph outside reviewed column on PDF page{index+1}')
            arabic=textpage.get_text_bounded(left=left,bottom=bottom,right=right,top=top)
            french=textpage.get_text_bounded(left=left,bottom=top,right=right,top=float(reader.pages[index].cropbox.top))
            textpage.close();page.close()
            (cache/f'{index+1:04}-ar.txt').write_text(arabic,encoding='utf-8',newline='')
            (cache/f'{index+1:04}-fr.txt').write_text(french,encoding='utf-8',newline='')
            labels=page_labels(arabic,allow_inline=True)
            if not started:
                if not labels or labels[0]['canonicalNumberCandidate']!='1':continue
                started=True
            pages.append({'pdfPage':index+1,'arabicColumnUnrotatedBox':[left,bottom,right,top],
                          'rawArabicSha256':pipeline.hash_text(arabic),'rawFrenchSha256':pipeline.hash_text(french),
                          'arabicGlyphCoverage':'all source Arabic glyph boxes within the reviewed column'})
            cursor=0
            for label in labels:
                if current:
                    current['fragments'].append({'pdfPage':index+1,'start':cursor,'end':label['start'],'rawText':arabic[cursor:label['start']]})
                    records.append(current)
                number=label['canonicalNumberCandidate']
                current={'candidateId':'family-native:'+number.replace(' مكرر','-bis').replace(' ','-')+f"@p{index+1}:{label['start']}",
                         'canonicalNumberCandidate':number,'rawPrintedLabel':label['rawLabel'],
                         'labelPdfPage':index+1,'fragments':[]}
                cursor=label['end']
                if number=='224':ended=True
            if current:current['fragments'].append({'pdfPage':index+1,'start':cursor,'end':len(arabic),'rawText':arabic[cursor:]})
            if ended:break
    if not ended:raise ValueError('Family main code ending224 not found; do not import annexes')
    if current:records.append(current)
    counts=Counter(r['canonicalNumberCandidate'] for r in records)
    result={'codeId':'family-code','source':source,'edition':'Bilingual Ministry compilation incorporating05-02',
            'acquisitionState':'raw Arabic column blocks; font decoding, paragraphs and latest-amendment QA incomplete',
            'articleBlockCount':len(records),'distinctNumberCandidates':len(counts),
            'originalBaseNumbersObserved':len(set(map(str,range(1,225)))&set(counts)),
            'baseNumbersNotObserved':[str(n) for n in range(1,225) if str(n) not in counts],
            'insertedNumberCandidates':[n for n in counts if 'مكرر' in n],
            'duplicateNumberCandidates':{k:v for k,v in counts.items() if v>1},
            'warnings':['Raw bodies retain corrupt font Unicode, headings, amendment annotations and page labels.',
                        'Inline labels require a printed colon; a prose reference without that delimiter is not a label.',
                        'Suffix index preceding an amendment annotation may appear after the colon in PDF extraction; candidate numbering is not independent identity verification.',
                        'The French column is separately cached; presence, aliases and paragraph alignment remain unchecked.',
                        'Pages after the first224 are excluded: related annexes do not expand the authorized scope.'],
            'currentConsolidationComplete':False,'pdfAndOcrChecksComplete':False,'pages':pages,'articles':records}
    pipeline.write_json(pipeline.ROOT/'data/law/corpus/research/current-batches/family-official-native-2005.json',result)
    print('Family source blocks:',len(records),'distinct:',len(counts),'base labels:',result['originalBaseNumbersObserved'],'/224')


if __name__=='__main__':main()
