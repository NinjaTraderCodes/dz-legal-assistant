#!/usr/bin/env python3
"""Render one-based PDF pages for direct source inspection (no text repair)."""
import argparse
from pathlib import Path
import pypdfium2 as pdfium

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("pdf", type=Path)
parser.add_argument("pages", type=int, nargs="+")
parser.add_argument("--dpi", type=int, default=150)
parser.add_argument("--out", type=Path, required=True)
args = parser.parse_args()
args.out.mkdir(parents=True, exist_ok=True)
document = pdfium.PdfDocument(args.pdf)
try:
    for number in args.pages:
        if not 1 <= number <= len(document):
            parser.error(f"PDF page out of bounds: {number}")
        page = document[number - 1]
        page.render(scale=args.dpi / 72).to_pil().save(args.out / f"{args.pdf.stem}-p{number}.png")
        page.close()
finally:
    document.close()
