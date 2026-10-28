#!/usr/bin/env python3
"""Prepare Civil compilation bodies using object-bound glyph decoding and source line geometry; never certify law.

Local articles1-10 of the management-contract chapter have a separate research number scope. They must not
overwrite Civil general provisions1-10 or be assigned invented printed bis numbers. Annexes stay excluded.
"""
from __future__ import annotations

import re
import ctypes
from collections import Counter
from contextlib import closing

import pipeline
import sgg_glyphs
import text_clean
from collect_civil_native import page_labels

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
KEYWORDS = [("الكتاب", 0), ("الباب", 1), ("الفصل", 2), ("القسم", 3)]


def extract_lines(document, page_number: int, decisions: dict) -> tuple[list[dict], list[dict]]:
    import pypdfium2.raw as raw_api
    page = document[page_number - 1]
    textpage = page.get_textpage()
    raw = textpage.get_text_range(index=0, count=textpage.count_chars())
    typography = []
    for obj in page.get_objects():
        if obj.type != raw_api.FPDF_PAGEOBJ_TEXT:
            continue
        rgba = [ctypes.c_uint() for _ in range(4)]
        if not raw_api.FPDFPageObj_GetFillColor(obj.raw, *[ctypes.byref(v) for v in rgba]):
            raise ValueError("Civil text object lacks its fill color")
        font = raw_api.FPDFTextObj_GetFont(obj.raw)
        weight = raw_api.FPDFFont_GetWeight(font)
        colored = len({v.value for v in rgba[:3]}) > 1
        if colored or weight >= 600:
            typography.append({"box": obj.get_pos(), "colored": colored, "bold": weight >= 600})
    lines, notes, offset, footnote = [], [], 0, False
    for raw_line in raw.splitlines(keepends=True):
        positions = [i for i in range(offset, offset + len(raw_line)) if i < textpage.count_chars()]
        offset += len(raw_line)
        chars, boxes = [], []
        for i in positions:
            char = textpage.get_text_range(i, 1)
            if char == sgg_glyphs.ALEF_MADDA and decisions.get(i) is True:
                char = "ك"
            chars.append(char)
            if not char.isspace():
                boxes.append(textpage.get_charbox(i))
        text = "".join(chars).replace(sgg_glyphs.STRETCH, "").replace("ـ", "").strip()
        if not text or re.fullmatch(r"\d+", text):
            continue
        if re.search(r"_{4,}", text):
            footnote = True
        if footnote:
            notes.append({"page": page_number, "rawText": raw_line, "decodedText": text})
            continue
        if not boxes:
            raise ValueError("Nonempty line lacks geometry")
        left, right = min(b[0] for b in boxes), max(b[2] for b in boxes)
        bottom, top = min(b[1] for b in boxes), max(b[3] for b in boxes)
        spans = []
        for obj in typography:
            x0,y0,x1,y1 = obj["box"]
            if min(top,y1) - max(bottom,y0) > (top-bottom) / 2:
                spans.append(obj | {"width": max(0, min(right,x1) - max(left,x0))})
        colored_share = min(1, sum(o["width"] for o in spans if o["colored"]) / max(1,right-left))
        bold_share = min(1, sum(o["width"] for o in spans if o["bold"]) / max(1,right-left))
        label = next((l for l in page_labels(text) if l["start"] == 0), None)
        heading = not label and (any(text.startswith(k + " ") for k, _ in KEYWORDS)
                                 or colored_share > .6
                                 or bold_share > .6 and (left > 120 and right < 475 or re.match(r"^\d+\s*[-–]", text)))
        lines.append({"page": page_number, "rawText": raw_line, "text": text,
                      "kind": "heading" if heading else "start" if label or right < 502 else "continuation",
                      "box": [left, bottom, right, top], "coloredShare": colored_share, "boldShare": bold_share})
    textpage.close()
    page.close()
    return lines, notes


def assemble(lines: list[dict]) -> tuple[list[dict], list[dict]]:
    articles, paratext, path, buffer, current = [], [], {}, [], None
    signature = False
    for line in lines:
        text = line["text"]
        if current and current["numberScope"] == "main" and current["number"] == "1003" and (
                signature or text.startswith(("حرر بالجزائر", "وحرر بالجزائر"))):
            signature = True
            paratext.append(line)
            continue
        if line["kind"] == "heading":
            buffer.append(text)
            continue
        if buffer:
            new_level = None
            for heading in buffer:
                heading = re.sub(r"\s*\(\d+\)\s*", " ", heading).strip()
                level = next((v for k, v in KEYWORDS if heading.startswith(k + " ")), None)
                if level is not None:
                    path = {k: v for k, v in path.items() if k < level}
                    path[level] = heading
                    new_level = level
                elif new_level is not None:
                    path[new_level] += " - " + heading
                    new_level = None
                elif current is not None:
                    path[4] = heading
            buffer = []
        label = next((l for l in page_labels(text) if l["start"] == 0), None)
        if label:
            number = label["canonicalNumberCandidate"]
            local = any("الفصل الأول مكرر" in h and "عقد التسيير" in h for h in path.values())
            scope = "management-contract" if local else "main"
            if local and number not in {str(n) for n in range(1, 11)}:
                raise ValueError("Unexpected numbering within management-contract scope")
            current = {"number": number, "numberScope": scope, "candidateKey": scope + ":" + number,
                       "printedLabel": label["rawLabel"], "path": [path[k] for k in sorted(path)],
                       "pages": [line["page"]], "rawParagraphs": [text[label["end"]:].strip()], "lineEvidence": [line]}
            articles.append(current)
        elif current:
            if line["kind"] == "start":
                current["rawParagraphs"].append(text)
            else:
                current["rawParagraphs"][-1] += " " + text
            if line["page"] not in current["pages"]:
                current["pages"].append(line["page"])
            current["lineEvidence"].append(line)
        else:
            paratext.append(line)
    for article in articles:
        article["paragraphs"] = [p for raw in article["rawParagraphs"] if (p := text_clean.paragraph(raw))]
        article["text"] = "\n".join(article["paragraphs"])
        article["textSha256"] = pipeline.hash_text(article["text"])
        article["qaState"] = "source-candidate-only"
    keys = [a["candidateKey"] for a in articles]
    if len(keys) != len(set(keys)):
        raise ValueError("Duplicate qualified Civil article identity; never overwrite")
    return articles, paratext


def main() -> None:
    import pypdfium2 as pdfium
    batch = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/current-batches/civil-official-native-2007.json")
    pdf = pipeline.CACHE / "current-digital/civil-consolidated-ar.pdf"
    if pipeline.hash_file(pdf) != batch["source"]["sha256"]:
        raise ValueError("Civil compilation differs from its pin")
    pages = [p["pdfPage"] for p in batch["pages"]]
    cache = pipeline.CACHE / "civil-native-glyph-decisions.json"
    if cache.exists():
        saved = pipeline.read_json(cache)
        if saved["sourceSha256"] != batch["source"]["sha256"] or saved["pages"] != pages or saved["decoderSha256"] != pipeline.hash_file(pipeline.ROOT / "tools/law-corpus/sgg_glyphs.py"):
            raise ValueError("Cached glyph decisions are stale")
        decisions = {int(k): v for k,v in saved["decisions"].items()}
    else:
        print("Decoding", len(pages), "Civil source pages; no fresh OCR", flush=True)
        decisions = sgg_glyphs.page_decisions(pdf, [p - 1 for p in pages])
        pipeline.write_json(cache, {"sourceSha256": batch["source"]["sha256"], "pages": pages,
            "decoderSha256": pipeline.hash_file(pipeline.ROOT / "tools/law-corpus/sgg_glyphs.py"), "decisions": decisions})
    if set(decisions) != set(pages) or any(d is None for d in decisions.values()):
        raise ValueError("Civil glyph objects failed to align on a source page")
    undecided = {p: [k for k,v in d.items() if v is None] for p,d in decisions.items() if any(v is None for v in d.values())}
    lines, footnotes = [], []
    with closing(pdfium.PdfDocument(str(pdf))) as document:
        for p in pages:
            page, notes = extract_lines(document, p, {int(k): v for k,v in decisions[p].items()})
            lines.extend(page)
            footnotes.extend(notes)
    articles, paratext = assemble(lines)
    pipeline.write_json(WORK / "native-candidates.json", {
        "source": batch["source"], "pages": pages, "glyphUndecided": undecided,
        "glyphDecisionCacheSha256": pipeline.hash_file(cache), "articles": articles,
        "footnotes": footnotes, "paratext": paratext,
        "countsByNumberScope": dict(Counter(a["numberScope"] for a in articles)),
        "preparationState": "Source candidates only; headings/paragraphs, local numbering, amendments and current applicability require JO checks.",
        "verificationEffect": "none; no typed Civil delivery replaced or checked state granted"})
    print("Prepared", len(articles), "qualified Civil candidates:", dict(Counter(a["numberScope"] for a in articles)),
          "; undecided glyph pages:", len(undecided))


if __name__ == "__main__":
    main()
