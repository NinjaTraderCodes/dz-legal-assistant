"""The consolidated-PDF proof of a Civil or Commercial Code article (law/README.md, "Reading an article", step 7).

The method of tools/law/build_codes.py, reused without changing that file: letters and digits only (the consolidated
PDFs encode ك as آ, so the two are folded), searched in order in the PDF's text, as one stretch or a few long ones. The
consolidated edition of 2007 modernises some spellings, so the comparison is at level 2 (the hamza forms of alef, ى and
ي, final ة and ه are folded too, as text-compare's level 2 does).

    python consolidated.py check <code> <text-file>
Status: sameLetters | differs (with the first stretch that is not found) | notInConsolidated (most of it absent).
word_differences gives each differing place, word by word, for the settler (fetch.consolidated_prep).
"""
from __future__ import annotations

import difflib
import functools
import os
import re
import subprocess
import sys
import unicodedata

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import textcmp  # noqa: E402

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


def word_differences(code: str, text: str, slack: int = 40) -> list[dict]:
    """The places where the consolidated edition's letters differ from the text, word by word: our words there, the
    same words with the consolidated letters put in (spaces are lost in the consolidated letters, so a word it adds may
    be joined to its neighbour), ten words of context on each side, and whether the difference is orthographic only
    (text-compare level 2 or 2o: a hamza seat, ى and ي, which the 2007 edition modernises)."""
    words = list(re.finditer(r"\S+", text))
    target, pos = "", []  # pos[k]: (word index, offset in text) of folded letter k
    for wi, m in enumerate(words):
        for ci, ch in enumerate(m.group(0)):
            f = fold(ch)
            if f:
                target += f
                pos.append((wi, m.start() + ci))
    doc, _ = document(code)
    parts, _ = stretches(target, doc)
    if not parts:
        return []
    seg = doc[max(0, parts[0][0] - slack): parts[-1][0] + len(parts[-1][1]) + slack]
    ops = []  # (first word, last word, start offset, end offset, consolidated letters)
    for op, i1, i2, j1, j2 in difflib.SequenceMatcher(a=target, b=seg, autojunk=False).get_opcodes():
        if op == "equal" or (op == "insert" and i1 in (0, len(target))):
            continue  # an insert at either end is the slack around the article
        if op == "insert":
            w = pos[i1 - 1][0]
            ops.append((w, w, pos[i1 - 1][1] + 1, pos[i1 - 1][1] + 1, seg[j1:j2]))
        else:
            ops.append((pos[i1][0], pos[i2 - 1][0], pos[i1][1], pos[i2 - 1][1] + 1, seg[j1:j2]))
    groups: list[list[tuple]] = []
    for o in ops:  # differences that fall on the same words are one place
        if groups and o[0] <= max(x[1] for x in groups[-1]) + 1:
            groups[-1].append(o)
        else:
            groups.append([o])
    out = []
    for g in groups:
        w_lo, w_hi = g[0][0], max(o[1] for o in g)
        s, e = words[w_lo].start(), words[w_hi].end()
        theirs = text[s:e]
        for o in sorted(g, key=lambda o: o[2], reverse=True):
            theirs = theirs[: o[2] - s] + o[4] + theirs[o[3] - s:]
        ours = text[s:e]
        theirs = re.sub(r"(?<=\d)(?=[^\d\s])|(?<=[^\d\s])(?=\d)", " ", theirs).strip()
        out.append({"ours": ours, "consolidated": theirs,
                    "orthographic": textcmp.level(ours, theirs) in ("1", "2", "2o"),
                    "before": " ".join(m.group(0) for m in words[max(0, w_lo - 10):w_lo]),
                    "after": " ".join(m.group(0) for m in words[w_hi + 1:w_hi + 11])})
    return out


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    with open(argv[2], encoding="utf-8") as f:
        print(check(argv[1], f.read()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
