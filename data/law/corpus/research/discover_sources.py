"""Research helper; download only allowlisted official sources into ignored cache."""
import concurrent.futures
import hashlib
import json
from pathlib import Path
import shutil
import urllib.request
import fitz

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / '.cache'
PRIOR = Path(r'C:\Users\TheShadow\dz-legal-assistant\legal-research\downloads\joradp')
CACHE.mkdir(exist_ok=True)
SOURCES = [
    ('A1975078', 'https://www.joradp.dz/FTP/jo-arabe/1975/A1975078.pdf'),
    ('F1975078', 'https://www.joradp.dz/FTP/jo-francais/1975/F1975078.pdf'),
    ('A1983005', 'https://www.joradp.dz/FTP/jo-arabe/1983/A1983005.pdf'),
    ('F1983005', 'https://www.joradp.dz/FTP/jo-francais/1983/F1983005.pdf'),
    ('A1988018', 'https://www.joradp.dz/FTP/jo-arabe/1988/A1988018.pdf'),
    ('F1988018', 'https://www.joradp.dz/FTP/jo-francais/1988/F1988018.pdf'),
    ('A1989006', 'https://www.joradp.dz/FTP/jo-arabe/1989/A1989006.pdf'),
    ('F1989006', 'https://www.joradp.dz/FTP/jo-francais/1989/F1989006.pdf'),
    ('A2005044', 'https://www.joradp.dz/FTP/jo-arabe/2005/A2005044.pdf'),
    ('F2005044', 'https://www.joradp.dz/FTP/jo-francais/2005/F2005044.pdf'),
    ('A2007031', 'https://www.joradp.dz/FTP/jo-arabe/2007/A2007031.PDF'),
    ('F2007031', 'https://www.joradp.dz/FTP/jo-francais/2007/F2007031.pdf'),
    ('A1975101', 'https://www.joradp.dz/FTP/jo-arabe/1975/A1975101.pdf'),
    ('F1975101', 'https://www.joradp.dz/FTP/jo-francais/1975/F1975101.pdf'),
    ('A2005011', 'https://www.joradp.dz/FTP/jo-arabe/2005/A2005011.pdf'),
    ('F2005011', 'https://www.joradp.dz/FTP/jo-francais/2005/F2005011.pdf'),
    ('A2015071', 'https://www.joradp.dz/FTP/jo-arabe/2015/A2015071.pdf'),
    ('F2015071', 'https://www.joradp.dz/FTP/jo-francais/2015/F2015071.pdf'),
    ('A2022032', 'https://www.joradp.dz/FTP/jo-arabe/2022/A2022032.pdf'),
    ('F2022032', 'https://www.joradp.dz/FTP/jo-francais/2022/F2022032.pdf'),
    ('A2008021', 'https://www.joradp.dz/FTP/jo-arabe/2008/A2008021.pdf'),
    ('F2008021', 'https://www.joradp.dz/FTP/jo-francais/2008/F2008021.pdf'),
    ('A2022048', 'https://www.joradp.dz/FTP/jo-arabe/2022/A2022048.pdf'),
    ('F2022048', 'https://www.joradp.dz/FTP/jo-francais/2022/F2022048.pdf'),
    ('A2003044', 'https://www.joradp.dz/FTP/jo-arabe/2003/A2003044.pdf'),
    ('F2003044', 'https://www.joradp.dz/FTP/jo-francais/2003/F2003044.pdf'),
]

def obtain(source):
    key, url = source
    target = CACHE / f'{key}.pdf'
    try:
        reused = (PRIOR / target.name).exists()
        if not target.exists():
            if reused:
                shutil.copyfile(PRIOR / target.name, target)
            else:
                req = urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0'})
                with urllib.request.urlopen(req, timeout=45) as response:
                    target.write_bytes(response.read())
        pdf = fitz.open(target)
        pages = [page.get_text(sort=True) for page in pdf]
        (CACHE / f'{key}.pages.json').write_text(json.dumps(pages, ensure_ascii=False), encoding='utf-8')
        return {'id':key,'url':url,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
                'retrievedOn':'2026-10-16' if reused else '2026-10-25',
                'reusedPriorDownload':reused,'pageCount':len(pdf)}
    except Exception as exc:
        return {'id':key,'url':url,'error':str(exc)}

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        results = list(pool.map(obtain, SOURCES))
    (ROOT / 'research' / 'source-downloads.json').write_text(json.dumps(results, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for result in results:
        print(json.dumps(result, ensure_ascii=False))
