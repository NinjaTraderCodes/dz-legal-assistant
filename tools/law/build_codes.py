#!/usr/bin/env python3
"""Builds data/law/codes/*.json from the clean texts in data/law/curated-articles.json and the official PDFs.

Every clean text is proved against its PDF: with spaces, punctuation and tatweel removed (and the letter ك, which these
PDFs encode as آ, folded with آ), its letters and digits must appear in the PDF's text in the same order, as one
stretch (or a few long stretches where the PDF splits an article across columns). The folded stretches are stored with
the article as `sourceExtract`, and the test suite checks the stored text against them, so an edited article that no
longer matches its source fails the build.

Usage (needs poppler-utils for pdftotext):  python3 tools/law/build_codes.py
"""
import hashlib, json, re, subprocess, sys, unicodedata
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CURATED = ROOT / "data/law/curated-articles.json"
SOURCES = ROOT / "data/law/sources.json"
OUT = ROOT / "data/law/codes"
MIN_FRAGMENT = 12
# Articles the PDF prints across two columns, so their text comes in several stretches: searched on their own page
# (and the next) only, since their sentences also occur elsewhere in the code.
MULTI_FRAGMENT = {("cpca", "8"): 4, ("cpca", "15"): 4}

# The 2008 Official Journal PDF uses a font whose ligatures extract as Latin characters.
CPCA_GLYPHS = [("خمل", "لمخ"), ("جمل", "لمج"), ("J", "لم"), ("~", "بم"), ("•", "يم"), ("X", "ين"), ("s", "،"), ("–", "مم")]


def fold(text: str) -> str:
    """Letters and digits only, with آ and ك folded together. Must match LawTextFold in DzLegal.Law."""
    text = unicodedata.normalize("NFKC", text).replace("آ", "ك")
    return "".join(c for c in text if ("\u0621" <= c <= "\u064a" and c != "\u0640") or c.isdigit())


def page_texts(pdf: Path, decode: bool) -> list[str]:
    pages = int(re.search(r"Pages:\s+(\d+)", subprocess.run(["pdfinfo", pdf], capture_output=True, text=True).stdout).group(1))
    out = []
    for page in range(1, pages + 1):
        text = subprocess.run(["pdftotext", "-f", str(page), "-l", str(page), pdf, "-"], capture_output=True, text=True).stdout
        text = re.sub("[‪-‮‎‏]", "", unicodedata.normalize("NFKC", text))
        if decode:
            for a, b in CPCA_GLYPHS:
                text = text.replace(a, b)
        out.append(fold(text))
    return out


def fragments(target: str, document: str) -> list[tuple[int, str]]:
    """Splits the target into the longest stretches found in the document, in order of the target."""
    found, i = [], 0
    while i < len(target):
        j = i + MIN_FRAGMENT
        if document.find(target[i:j]) < 0:
            raise ValueError(f"not found in the source: …{target[i:i + 40]}…")
        while j < len(target) and document.find(target[i:j + 1]) >= 0:
            j += 1
        found.append((document.find(target[i:j]), target[i:j]))
        i = j
    return found


def main() -> int:
    curated = json.loads(CURATED.read_text(encoding="utf-8"))
    sources = {c["id"]: c for c in json.loads(SOURCES.read_text(encoding="utf-8"))["codes"]}
    OUT.mkdir(parents=True, exist_ok=True)
    failures = 0
    for code_id, spec in curated["codes"].items():
        pdf = ROOT / spec["file"]
        pages = page_texts(pdf, decode=code_id == "cpca")
        document = "".join(pages)
        starts, offset = [], 0
        for text in pages:
            starts.append(offset)
            offset += len(text)
        articles = []
        for number, text in spec["articles"].items():
            window, base = document, 0
            if (code_id, number) in MULTI_FRAGMENT:
                page_index = MULTI_FRAGMENT[(code_id, number)] - 1
                window, base = pages[page_index] + pages[page_index + 1], starts[page_index]
            try:
                parts = [(base + at, part) for at, part in fragments(fold(text), window)]
            except ValueError as error:
                print(f"{code_id} {number}: {error}", file=sys.stderr)
                failures += 1
                continue
            if len(parts) > 1 and (code_id, number) not in MULTI_FRAGMENT:
                print(f"{code_id} {number}: matches {len(parts)} separate stretches of the PDF, expected one", file=sys.stderr)
                failures += 1
                continue
            page = max(i for i, s in enumerate(starts) if s <= parts[0][0]) + 1
            article = {"number": number, "text": text, "page": page, "sourceExtract": "".join(p for _, p in parts)}
            if note := spec.get("notes", {}).get(number):
                article["note"] = note
            articles.append(article)
        manifest = sources[code_id]
        code = {
            "id": code_id,
            "title": manifest["title"],
            "citationName": manifest["citationName"],
            "reference": manifest["reference"],
            "edition": spec["edition"],
            "consolidatedAsOf": spec.get("consolidatedAsOf"),
            "partial": True,
            "source": {
                "url": spec["file"],
                "sha256": hashlib.sha256(pdf.read_bytes()).hexdigest(),
                "retrievedOn": date.today().isoformat(),
            },
            "verified": False,
            "notes": spec.get("codeNotes", []),
            "articles": articles,
        }
        target = OUT / f"{code_id}.json"
        target.write_text(json.dumps(code, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{target.relative_to(ROOT)}: {len(articles)} articles")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
