#!/usr/bin/env python3
"""Trace scanned column separators in pixels; preserves page geometry and source bytes."""
from PIL import Image
import pipeline


def separator(image):
    # Fixed analysis grid; tests use invented slanted rules. This finds geometry,
    # never article text or article-number corrections.
    small = image.convert("L").resize((600, 800))
    pixels = small.tobytes()
    rows = list(range(80, 770, 4))
    best = (0, 0, 0)
    for delta in range(-20, 21):
        for x in range(258, 360):
            score = sum(pixels[y * 600 + x + round(delta * (y - 400) / 800)] < 150 for y in rows)
            if score > best[0]:
                best = (score, x, delta)
    return {"centerAtHalfHeight": best[1] / 600, "deltaAcrossHeight": best[2] / 600,
            "darkPixelCoverage": best[0] / len(rows), "grid": [600, 800]}


def main():
    path = pipeline.ROOT / "tools/law-corpus/civil-full-manifest.json"
    manifest = pipeline.load_manifest(path)
    sources = {s["id"]: s for s in manifest["sources"]}
    decisions = []
    for source_id in ("A1975078", "F1975078"):
        source = sources[source_id]
        pages = sorted({x["pdfPage"] for x in manifest["extracts"] if x["sourceId"] == source_id})
        for page in pages:
            image_path = pipeline.CACHE / "extractions" / source["sha256"] / f"page-{page:04}" / "page-150dpi.png"
            with Image.open(image_path) as image:
                found = separator(image)
            if found["darkPixelCoverage"] < 0.55:
                raise ValueError(f"No reliable scanned separator; manual page-layout decision needed: {source_id}/{page}")
            center, delta = found["centerAtHalfHeight"], found["deltaAcrossHeight"]
            top, bottom = 0.045, 0.985
            at_top, at_bottom = center + delta * (top - .5), center + delta * (bottom - .5)
            for extract in manifest["extracts"]:
                if extract["sourceId"] != source_id or extract["pdfPage"] != page:
                    continue
                if extract["id"].endswith("-left"):
                    polygon = [[.015, top], [at_top - .002, top], [at_bottom - .002, bottom], [.015, bottom]]
                else:
                    polygon = [[at_top + .002, top], [.995, top], [.995, bottom], [at_bottom + .002, bottom]]
                extract["clipPolygon"] = [[round(c, 7) for c in point] for point in polygon]
                extract["region"] = [min(x for x, _ in polygon), top, max(x for x, _ in polygon), bottom]
            decisions.append({"sourceId": source_id, "pdfPage": page, "sourceSha256": source["sha256"],
                              "pageImageSha256": pipeline.hash_file(image_path), **found,
                              "method": "Discrete straight-rule pixel search; slanted column polygon masks; no OCR/body text changed",
                              "status": "mechanical-layout-proposal; article text and boundary review still required"})
    pipeline.write_json(path, manifest)
    pipeline.write_json(pipeline.ROOT / "data/law/corpus/research/civil-layout-decisions.json", {"pages": decisions})
    print(f"Recorded {len(decisions)} scanned-page separator proposals; no article verification inferred")


if __name__ == "__main__":
    main()
