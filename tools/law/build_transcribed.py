#!/usr/bin/env python3
"""Builds data/law/codes/<id>.json for the codes whose official PDFs have no usable text layer.

The Family Code and labour-law PDFs are scans, or use a font that extracts as other characters, so build_codes.py cannot
prove them letter by letter. Their texts are kept in data/law/transcribed-articles.json: transcribed from the page
images, then read a second time word by word on the same images. This script only records where each article comes
from (file, SHA-256, page) and marks the code as transcribed; a lawyer then checks each article against its page.

Usage:  python3 tools/law/build_transcribed.py
"""
import hashlib, json, sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TRANSCRIBED = ROOT / "data/law/transcribed-articles.json"
SOURCES = ROOT / "data/law/sources.json"
OUT = ROOT / "data/law/codes"


def source(file: str) -> dict:
    path = ROOT / file
    return {"url": file, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "retrievedOn": date.today().isoformat()}


def main() -> int:
    data = json.loads(TRANSCRIBED.read_text(encoding="utf-8"))
    manifest = {c["id"]: c for c in json.loads(SOURCES.read_text(encoding="utf-8"))["codes"]}
    for code_id, spec in data["codes"].items():
        articles = []
        for item in spec["articles"]:
            article = {"number": item["number"], "text": item["text"], "page": item["page"]}
            if "amendedBy" in item:
                article["amendedBy"] = item["amendedBy"]
            if "source" in item:
                article["source"] = source(item["source"])
            if "note" in item:
                article["note"] = item["note"]
            articles.append(article)
        entry = manifest[code_id]
        code = {
            "id": code_id,
            "title": entry["title"],
            "citationName": entry["citationName"],
            "reference": entry["reference"],
            "edition": spec["edition"],
            "consolidatedAsOf": spec.get("consolidatedAsOf"),
            "partial": True,
            "source": source(spec["file"]),
            "verified": False,
            "transcription": spec["transcription"],
            "notes": spec.get("notes", []),
            "articles": articles,
        }
        target = OUT / f"{code_id}.json"
        target.write_text(json.dumps(code, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{target.relative_to(ROOT)}: {len(articles)} articles")
    return 0


if __name__ == "__main__":
    sys.exit(main())
