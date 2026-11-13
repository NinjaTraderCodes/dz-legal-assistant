"""The French check of a missing article (law/README.md, "Reading an article", step 6).

Every number of the Arabic text (periods, amounts, article numbers, items) must appear in the French text of the same
article in the same issue. From 2003 on, the French text is cut from the scan's kept text layer of the French issue;
before 2003 it is a Sonnet reading of the French page (same prompt, language French). A mismatch is never settled by
choosing: the Arabic is official and stands; the mismatch is written in the article's note and its deadlines become
interpretive (grade-records).

    python french.py cut <year> <issue> <article>     -> the French article from the kept text
"""
from __future__ import annotations

import os
import re
import sys
import unicodedata
from collections import Counter

TEXT = r"C:\Users\TheShadow\dz-core-research\journal-text"
DIGITS = str.maketrans("٠١٢٣٤٥٦٧٨٩", "0123456789")
# French numbering of inserted articles, as the Journal prints it
BIS = {"مكرر": "bis", "مكرر 1": "ter", "مكرر 2": "quater", "مكرر 3": "quinquies", "مكرر 4": "sexies",
       "مكرر 5": "septies", "مكرر 6": "octies", "مكرر 7": "nonies"}


def french_number(article: str) -> list[str]:
    """Candidate French headings for an Arabic number: «187 مكرر 1» -> ["187 ter", "187 bis 1"]."""
    m = re.fullmatch(r"(\d+)(?:\s+(مكرر(?:\s+\d+)?))?", article.strip())
    if not m:
        return [article]
    n, suffix = m.group(1), m.group(2)
    if not suffix:
        return [n]
    out = []
    if suffix in BIS:
        out.append(f"{n} {BIS[suffix]}")
    k = re.search(r"\d+", suffix)
    out.append(f"{n} bis {k.group(0)}" if k else f"{n} bis")
    out = list(dict.fromkeys(out))
    return out


def cut(text: str, article: str) -> str | None:
    """The text of «Art. N.» up to the next «Art.» heading, in a French issue's text layer. Accents are dropped first:
    the Journal writes «quinquiès» as well as «quinquies» (Law 05-10, art. 45)."""
    text = "".join(c for c in unicodedata.normalize("NFD", text) if not unicodedata.combining(c))
    for head in french_number(article):
        pat = re.compile(r"(?:^|\n)\s*[«\"“]?\s*Art(?:icle)?\.?\s*" + re.escape(head).replace(r"\ ", r"\s+") + r"(?![\w])\s*[.\-—–:]",
                         re.I)
        m = pat.search(text)
        if m:
            rest = text[m.end():]
            nxt = re.search(r"\n\s*[«\"“]?\s*Art(?:icle)?\.?\s*\d", rest)
            return rest[: nxt.start()] if nxt else rest[:4000]
    return None


def numbers(text: str) -> Counter:
    """The integers written in digits (Arabic or Western), as a multiset. Years of dates count too. Thousands
    separators are joined («100.000» is one number); the k of «N مكرر k» is dropped, since the French writes it as a
    word (ter, quater, …)."""
    t = text.translate(DIGITS)
    # Hijri dates («22 جمادى الثانية عام 1411») are printed in the Arabic only; the French gives the Gregorian date.
    t = re.sub(r"\d+\s+(?:محرم|صفر|ربيع الأول|ربيع الثاني|ربيع الآخر|جمادى الأولى|جمادى الثانية|جمادى الآخرة|رجب|شعبان|"
               r"رمضان|شوال|ذي القعدة|ذو القعدة|ذي الحجة|ذو الحجة)\s+عام\s+\d+", " ", t)
    t = re.sub(r"(\d+)\s*مكرر\s*\d+", r"\1", t)
    t = re.sub(r"\b(\d{1,3}(?:[.\s ]\d{3})+)\b", lambda m: re.sub(r"[.\s ]", "", m.group(1)), t)
    return Counter(int(x) for x in re.findall(r"\d+", t))


def check(arabic: str, french: str) -> dict:
    a, f = numbers(arabic), numbers(french)
    missing = sorted((a - f).elements())
    extra = sorted((f - a).elements())
    return {"equal": not missing, "arabicNumbers": sorted(a.elements()), "frenchNumbers": sorted(f.elements()),
            "missingInFrench": missing, "onlyInFrench": extra}


def kept(year: int, issue: int) -> str:
    with open(os.path.join(TEXT, str(year), f"F{year}{issue:03d}.txt"), encoding="utf-8") as f:
        return f.read()


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[0] == "cut":
        print(cut(kept(int(argv[1]), int(argv[2])), argv[3]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
