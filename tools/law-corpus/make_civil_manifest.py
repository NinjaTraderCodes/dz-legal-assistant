#!/usr/bin/env python3
"""Declare full Civil source windows; OCR windows are not checked article transcriptions."""
import pipeline

RANGES = {
    "A1975078": (2, 67), "F1975078": (2, 52),
    "A1983005": (5, 8), "F1983005": (3, 5),
    "A1988018": (7, 9), "F1988018": (7, 8),
    "A1989006": (3, 4), "F1989006": (3, 4),
    "A2005044": (17, 25), "F2005044": (15, 21),
    "A2007031": (3, 6), "F2007031": (3, 5),
}


def main():
    output = pipeline.ROOT / "tools/law-corpus/civil-full-manifest.json"
    prior = {x["id"]: x for x in pipeline.read_json(output)["extracts"]} if output.exists() else {}
    sources = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/source-downloads.json")
    extracts = []
    for source_id, (first, last) in RANGES.items():
        # Arabic columns read right then left; French columns left then right.
        columns = [("right", [0.50, 0.045, 0.995, 0.985]), ("left", [0.015, 0.045, 0.505, 0.985])]
        if source_id.startswith("F"):
            columns.reverse()
        for page in range(first, last + 1):
            for side, region in columns:
                extracts.append({"id": f"civil-full-{source_id.lower()}-{page:03}-{side}",
                                 "sourceId": source_id, "pdfPage": page,
                                 "printedPage": str(page + 988) if source_id == "A1975078" else None,
                                 "region": region, "language": "ara" if source_id.startswith("A") else "fra", "psm": 6})
                old = prior.get(extracts[-1]["id"])
                if old and old.get("clipPolygon") and old["sourceId"] == source_id and old["pdfPage"] == page:
                    extracts[-1].update(region=old["region"], clipPolygon=old["clipPolygon"])
    manifest = {"manifestVersion": 1,
                "sources": [{key: s[key] for key in ("id", "url", "sha256", "retrievedOn")}
                            for s in sources if s["id"] in RANGES], "extracts": extracts}
    pipeline.write_json(output, manifest)
    print(f"{len(extracts)} independent column crops; source windows require article-boundary review")


if __name__ == "__main__":
    main()
