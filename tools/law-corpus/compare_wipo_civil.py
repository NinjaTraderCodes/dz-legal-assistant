#!/usr/bin/env python3
"""Replay a page-by-page raw text comparison of the local WIPO and Ministry PDFs."""
from contextlib import closing
import importlib.metadata
import pipeline

WIPO_SHA='ef532014f87a448ec1cd3d5b47eaa499c9ff813ae09c0bf00f8f5370811b7050'
MINISTRY_SHA='17a555bf81d5b99bf67417f3d42c157fd5df69499d6190cffbf182bc3566b573'
REVIEWED_IMAGE_SHA='71b1e5756475ad2ea15d02adb558d0f09063a8b1edfaa78e0263f589ff3b6a50'


def main():
    import pypdfium2 as pdfium
    wipo=pipeline.ROOT/'docs/official-law-texts/75-58_wipo_Version.pdf'
    ministry=pipeline.CACHE/'current-digital/civil-consolidated-ar.pdf'
    if pipeline.hash_file(wipo)!=WIPO_SHA or pipeline.hash_file(ministry)!=MINISTRY_SHA:
        raise ValueError('Civil comparison input differs from pinned bytes')
    rows=[]
    with closing(pdfium.PdfDocument(wipo)) as a,closing(pdfium.PdfDocument(ministry)) as b:
        if len(a)!=len(b):raise ValueError('Different page counts')
        page=a[7]
        bitmap=page.render(scale=1.6)
        image=bitmap.to_pil()
        image_path=pipeline.TOOLS/'wipo-civil-page8.png'
        image_path.parent.mkdir(parents=True,exist_ok=True)
        image.save(image_path)
        image.close();bitmap.close();page.close()
        if pipeline.hash_file(image_path)!=REVIEWED_IMAGE_SHA:
            raise ValueError('Rendered WIPO image differs from the recorded visual review; inspect it again')
        for index in range(len(a)):
            strings=[]
            for document in (a,b):
                page=document[index];textpage=page.get_textpage()
                strings.append(textpage.get_text_range(index=0,count=textpage.count_chars()))
                textpage.close();page.close()
            rows.append({'pdfPage':index+1,'wipoRawTextSha256':pipeline.hash_text(strings[0]),
                         'ministryRawTextSha256':pipeline.hash_text(strings[1]),'identical':strings[0]==strings[1]})
    result={'comparedOn':'2026-10-26','wipo':{'localPath':wipo.relative_to(pipeline.ROOT).as_posix(),
            'sha256':WIPO_SHA,'bytes':wipo.stat().st_size,'catalogueUrl':'https://www.wipo.int/wipolex/en/legislation/details/14774',
            'resourceUrl':'https://wipolex-res.wipo.int/edocs/lexdocs/laws/ar/dz/dz029ar.pdf',
            'acquisition':'User-supplied repository file; no claim of a new byte-identical online download',
            'catalogueVersion':'2007; amended through07-05 of13May2007'},
            'ministry':{'url':'https://droit.mjustice.gov.dz/sites/default/files/ACivil.pdf','sha256':MINISTRY_SHA},
            'method':{'engine':'pypdfium2','version':importlib.metadata.version('pypdfium2'),
                      'extraction':'Full raw character range; no normalization or Arabic repair'},
            'pageCount':len(rows),'identicalRawTextPages':sum(r['identical'] for r in rows),
            'interpretation':'Different PDF bytes, but identical extracted Unicode on every page with this engine. They are not independent text witnesses.',
            'visualCheck':{'pdfPage':8,'finding':'Rendered article4 prints كامل and كل; raw text contains آامل and آل. U+0467 is also emitted as a font stretch glyph.',
                           'imageSha256':REVIEWED_IMAGE_SHA,
                           'renderScale':1.6,'reviewedOn':'2026-10-26'},
            'effect':'Use native extraction for bulk collection; this mirror does not resolve the shared font-encoding issue. Canonical article verification is unchanged.',
            'pages':rows}
    pipeline.write_json(pipeline.ROOT/'data/law/corpus/research/current-batches/civil-wipo-comparison.json',result)
    print(f"WIPO/Ministry: {result['identicalRawTextPages']}/{len(rows)} pages have identical raw text")


if __name__=='__main__':main()
