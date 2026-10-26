#!/usr/bin/env python3
"""Freeze the22-13 overlay inventory; do not serve the old affected bodies as current."""
from pathlib import Path
import re
from pypdf import PdfReader
import pipeline

GROUPS={
 '2':[33], '3':[531,533,534,535],
 '4':[600,800,801,804,805,808,809,811,812,813,814],
 '6':[815,828,832,833,834,837,840,848,849,851,852,875,877,878,882,883,886,891,892,899],
 '8':[901,902,903,907,908,910,911,917,921,931],
 '10':[936,937,938,939,940,941,943,944,945,949,950,951,953,954,959,960,963,966,967,976,986],
 '12':[987,989]}


def quoted_targets(text):
    # Source900bis1 prints "Art" without the usual dot; label recognition only.
    return [m[1] for m in re.finditer(r'«\s*Art\.?\s*([0-9]+(?:\s+bis(?:\s+[0-9]+)?)?)\.',text)]


def main():
    manifest=pipeline.read_json(pipeline.ROOT/'tools/law-corpus/scope-source-manifest.json')
    pins={s['id']:s for s in manifest['sources']}
    for sid in ['A2022048','F2022048']:
        if pipeline.hash_file(pipeline.CACHE/(sid+'.pdf'))!=pins[sid]['sha256']:
            raise ValueError('Amending law differs from official source pin')
    reader=PdfReader(pipeline.CACHE/'F2022048.pdf');quoted=[]
    for page in range(3,12):
        quoted.extend({'numberFrench':n,'pdfPage':page} for n in quoted_targets(reader.pages[page-1].extract_text()))
    replaced={str(n) for group in GROUPS.values() for n in group}
    repealed={'826','835','836','912','913','914'}
    inserted=['536 bis']+['536 bis '+str(n) for n in range(1,8)]+['814 bis','900 bis']+['900 bis '+str(n) for n in range(1,10)]
    if not replaced|set(inserted)<={q['numberFrench'] for q in quoted}:
        raise ValueError('Explicit overlay inventory and quoted official targets disagree')
    pipeline.write_json(pipeline.ROOT/'data/law/corpus/research/current-batches/cpca-22-13-current-overlay-plan.json',{
      'law':'22-13','sourceArabic':pins['A2022048'],'sourceFrench':pins['F2022048'],'pdfPages':list(range(3,12)),
      'method':'All French operative instructions read; each replacement/insertion also occurs as a quoted target in the official law. Arabic body/paragraph checks and exact partial amendment composition remain pending.',
      'replacementOrPartialReplacementGroups':GROUPS,'replacementTargetCount':len(replaced),
      'knownUnchangedWithinReorganizedChapter':['532','536'],
      'partialRepeal':{'lawArticle':'14','cpcaArticle':'32','paragraphs':[7,8,9,10],
                      'warning':'HTML p element positions are not assumed to equal statutory alinea numbers.'},
      'wholeArticleRepeals':sorted(repealed,key=int),'insertionsFrenchNumbering':inserted,
      'headingChanges':['Law3:BookII/TitleI/ChapterIV commercial section and specialized tribunal.',
                        'Law5:BookIV/TitleI, newChapterIbis.','Law7:BookIV, newTitleIbis.',
                        'Law9:BookIV/TitleIII/ChapterII/SectionIII.','Law11:BookIV/TitleVI/ChapterII.'],
      'legallyRequiredTransition':{'lawArticle':'13',
        'rule':'Subject-matter/territorial provisions for new administrative jurisdictions and specialized commercial courts start at installation. Earlier-filed cases remain with the former administrative jurisdictions/commercial sections.',
        'effect':'Retain applicable former rules for these cases; current text alone cannot resolve temporal jurisdiction.'},
      'externalRepealNotExpandedIntoScope':'Law91-02 repealed by Law14; no new special-law collection started.',
      'baseArticlesBlockedPendingOverlay':sorted(replaced|repealed|{'32'},key=int),
      'baseArticlesNotDirectlyTextAmendedInThisInventory':1065-len(replaced|repealed|{'32'}),
      'currentConsolidationComplete':False,'quotedTargetObservations':quoted})
    print('CPCA overlay:',len(replaced),'replacement/partial targets,6 whole repeals,1 partial repeal,19 insertions;989 base bodies outside these direct text changes')


if __name__=='__main__':main()
