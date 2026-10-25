#!/usr/bin/env python3
"""Build the initial crop manifest from the independent official-download ledger.

Coordinates are manually selected from rendered official pages. This declares
extraction windows, not confirmation of a transcription or completeness of law.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EXTRACTS = [
    ("civil-7-original", "A1975078", 2, "990", [0.02, 0.46, 0.50, 0.57]),
    ("civil-138-original", "A1975078", 9, "997", [0.024, 0.65, 0.502, 0.771]),
    ("civil-139-original", "A1975078", 9, "997", [0.023, 0.772, 0.50, 0.836]),
    ("civil-140-original-a", "A1975078", 9, "997", [0.023, 0.838, 0.50, 0.982]),
    ("civil-140-original-b", "A1975078", 10, "998", [0.51, 0.057, 0.985, 0.139]),
    ("civil-308-original", "A1975078", 19, "1007", [0.025, 0.568, 0.507, 0.627]),
    ("civil-309-original", "A1975078", 19, "1007", [0.025, 0.635, 0.507, 0.760]),
    ("civil-379-original", "A1975078", 24, "1012", [0.02, 0.485, 0.495, 0.71]),
    ("civil-1002-original", "A1975078", 67, "1055", [0.02, 0.765, 0.49, 0.82]),
    ("civil-1003-original", "A1975078", 67, "1055", [0.02, 0.82, 0.49, 0.905]),
    ("civil-amendment-05-10-article-7", "A2005044", 18, "18", [0.075, 0.298, 0.485, 0.569]),
    ("commercial-187-bis-introduced", "A2005011", 9, "9", [0.50, 0.10, 0.92, 0.30]),
    ("commercial-187-bis-1-introduced", "A2005011", 9, "9", [0.50, 0.305, 0.92, 0.38]),
    ("cpca-2-original", "A2008021", 3, "3", [0.05, 0.585, 0.455, 0.642]),
    ("cpca-504-original", "A2008021", 41, "41", [0.048, 0.728, 0.46, 0.812]),
    ("cpca-1062-original", "A2008021", 95, "95", [0.50, 0.255, 0.91, 0.294]),
    ("cpca-1063-original", "A2008021", 95, "95", [0.50, 0.304, 0.91, 0.38]),
    ("cpca-1064-original", "A2008021", 95, "95", [0.05, 0.105, 0.47, 0.193]),
    ("civil-138-fr-a", "F1975078", 7, "823", [0.51, 0.925, 0.99, 0.985]),
    ("civil-138-fr-b", "F1975078", 8, "824", [0.07, 0.06, 0.51, 0.115]),
    ("civil-139-fr", "F1975078", 8, "824", [0.07, 0.118, 0.51, 0.175]),
    ("civil-140-fr", "F1975078", 8, "824", [0.07, 0.177, 0.51, 0.355]),
    ("civil-308-fr", "F1975078", 16, "832", [0.08, 0.395, 0.52, 0.435]),
    ("civil-309-fr", "F1975078", 16, "832", [0.08, 0.442, 0.52, 0.53]),
    ("civil-379-fr", "F1975078", 20, "836", [0.08, 0.20, 0.52, 0.36]),
    ("civil-1002-1003-fr", "F1975078", 52, "868", [0.51, 0.854, 0.99, 0.945]),
    ("commercial-187-bis-fr", "F2005011", 7, "7", [0.515, 0.149, 0.92, 0.314]),
    ("commercial-187-ter-fr", "F2005011", 7, "7", [0.515, 0.324, 0.92, 0.38]),
    ("cpca-504-fr", "F2008021", 36, "36", [0.51, 0.435, 0.91, 0.49]),
    ("cpca-2-fr", "F2008021", 3, "3", [0.49, 0.177, 0.90, 0.238]),
]

sources = json.loads((ROOT / "data/law/corpus/research/source-downloads.json").read_text(encoding="utf-8-sig"))
manifest = {"manifestVersion": 1,
            "sources": [{key: item[key] for key in ("id", "url", "sha256", "retrievedOn")}
                        for item in sources],
            "extracts": [{"id": item_id, "sourceId": source, "pdfPage": page, "printedPage": printed,
                          "region": region, "language": "fra" if source.startswith("F") else "ara", "psm": 6}
                         for item_id, source, page, printed, region in EXTRACTS]}
out = ROOT / "tools/law-corpus/priority-manifest.json"
out.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(out)
