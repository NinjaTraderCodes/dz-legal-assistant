"""Finding the page of an article in an Official Journal issue (law/README.md, "Reading an article", step 1).

From 2003 on, the Arabic text layer keeps the digits of the article headings (the letters may be garbled), and pdftotext
writes a heading «المادة 14 :» as ": 14" (right to left). The pages whose headings carry the number are the candidates;
the reading confirms (found / not found). Before 2003 there is no usable layer: the page comes from the corpus's anchors
(seed.py) or from a page map read on the images.

    python locate.py headings <pdf>             -> page: heading numbers
    python locate.py find <pdf> <n> [<n> ...]   -> n: candidate pages
"""
from __future__ import annotations

import json
import re
import subprocess
import sys

PDFTOTEXT = r"C:\Program Files\Git\mingw64\bin\pdftotext.exe"
PDFINFO = r"C:\Users\TheShadow\AppData\Local\Programs\MiKTeX\miktex\bin\x64\pdfinfo.exe"
HEADING = re.compile(r":\s*(\d{1,4})(?!\d)")


def pages(pdf: str) -> int:
    out = subprocess.run([PDFINFO, pdf], capture_output=True, text=True).stdout
    return int(re.search(r"Pages:\s+(\d+)", out).group(1))


def headings(pdf: str) -> dict[int, list[int]]:
    found = {}
    for p in range(1, pages(pdf) + 1):
        text = subprocess.run([PDFTOTEXT, "-f", str(p), "-l", str(p), pdf, "-"], capture_output=True).stdout.decode("utf-8", "replace")
        nums = [int(m.group(1)) for m in HEADING.finditer(text)]
        if nums:
            found[p] = nums
    return found


def find(pdf: str, numbers: list[int], hs: dict[int, list[int]] | None = None) -> dict[int, list[int]]:
    """Candidate pages per article number: the pages whose headings carry it. Several candidates are normal (an
    amending law's own articles and the code's articles it reprints share numbers); the reading decides."""
    hs = hs or headings(pdf)
    return {n: [p for p, ns in hs.items() if n in ns] for n in numbers}


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[0] == "headings":
        for p, ns in headings(argv[1]).items():
            print(p, ns)
    elif argv[0] == "find":
        print(json.dumps(find(argv[1], [int(x) for x in argv[2:]])))
    return 0


if __name__ == "__main__":
    sys.exit(main())
