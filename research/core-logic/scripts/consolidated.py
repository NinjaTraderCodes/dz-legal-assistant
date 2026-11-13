"""The consolidated-PDF proof of a Civil or Commercial Code article (law/README.md, "Reading an article", step 7).

The method of tools/law/build_codes.py, reused without changing that file: letters and digits only (the consolidated
PDFs encode ك as آ, so the two are folded), searched in order in the PDF's text, as one stretch or a few long ones. The
consolidated edition of 2007 modernises some spellings, so the comparison is at level 2 (the hamza forms of alef, ى and
ي, final ة and ه are folded too, as text-compare's level 2 does).

    python consolidated.py check <code> <text-file>
Status: sameLetters | differs (with the first stretch that is not found) | notInConsolidated (most of it absent).
"""
from __future__ import annotations

import functools
import os
import re
import subprocess
import sys
import unicodedata

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
PDFS = {"civil-code": os.path.join(REPO, "docs", "official-law-texts", "75-58_wipo_Version.pdf"),
        "commercial-code": os.path.join(REPO, "docs", "official-law-texts", "75-59_wipo_Version.pdf")}
PDFTOTEXT = r"C:\Program Files\Git\mingw64\bin\pdftotext.exe"
PDFINFO = r"C:\Users\TheShadow\AppData\Local\Programs\MiKTeX\miktex\bin\x64\pdfinfo.exe"
MIN_FRAGMENT = 12
MAX_STRETCHES = 4  # an article split across columns or pages comes in a few stretches


def fold(text: str) -> str:
    text = unicodedata.normalize("NFKC", text).replace("آ", "ك")
    text = text.translate(str.maketrans({"أ": "ا", "إ": "ا", "ٱ": "ا", "ى": "ي", "ة": "ه"}))
    return "".join(c for c in text if ("\u0621" <= c <= "\u064a" and c != "\u0640") or c.isdigit())


@functools.lru_cache(maxsize=4)
def document(code: str) -> tuple[str, tuple[int, ...]]:
    pdf = PDFS[code]
    n = int(re.search(r"Pages:\s+(\d+)", subprocess.run([PDFINFO, pdf], capture_output=True, text=True).stdout).group(1))
    parts, starts, offset = [], [], 0
    for p in range(1, n + 1):
        t = subprocess.run([PDFTOTEXT, "-enc", "UTF-8", "-f", str(p), "-l", str(p), pdf, "-"], capture_output=True).stdout.decode("utf-8", "replace")
        t = fold(re.sub("[\u202a-\u202e\u200e\u200f]", "", t))
        starts.append(offset)
        offset += len(t)
        parts.append(t)
    return "".join(parts), tuple(starts)


def stretches(target: str, doc: str) -> tuple[list[tuple[int, str]], int]:
    """The longest stretches of the target found in the document, in order; and how many letters were found."""
    found, i, covered = [], 0, 0
    while i < len(target):
        j = i + MIN_FRAGMENT
        if j > len(target):
            j = len(target)
        if doc.find(target[i:j]) < 0:
            i += 1  # skip a letter that is not found (a difference), keep looking
            continue
        while j < len(target) and doc.find(target[i:j + 1]) >= 0:
            j += 1
        found.append((doc.find(target[i:j]), target[i:j]))
        covered += j - i
        i = j
    return found, covered


def check(code: str, text: str) -> dict:
    target = fold(text)
    doc, starts = document(code)
    parts, covered = stretches(target, doc)
    share = covered / len(target) if target else 0
    page = (max(k for k, s in enumerate(starts) if s <= parts[0][0]) + 1) if parts else None
    if share == 1.0 and len(parts) <= MAX_STRETCHES:
        status = "sameLetters"
    elif share >= 0.6:
        status = "differs"
    else:
        status = "notInConsolidated"
    return {"status": status, "coverage": round(share, 3), "stretches": len(parts), "pdfPage": page,
            "pdf": os.path.relpath(PDFS[code], REPO).replace("\\", "/")}


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    with open(argv[2], encoding="utf-8") as f:
        print(check(argv[1], f.read()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
