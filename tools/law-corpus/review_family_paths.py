#!/usr/bin/env python3
"""Prepare source strips for every distinct Family heading path; generation records no reading or verdict."""
from __future__ import annotations

from PIL import Image, ImageDraw

import check_native
import pipeline


def main() -> None:
    work = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
    native_path = work / "native.json"
    comparison_path = work / "ministry-raw-comparison.json"
    native = pipeline.read_json(native_path)
    results = {r["number"]: r for r in pipeline.read_json(comparison_path)["results"]}
    manifest = pipeline.load_manifest(pipeline.ROOT / "tools/law-corpus/family-ministry-manifest.json")
    folders = dict(check_native.evidence_folders(manifest, "family-consolidated-bilingual"))
    out = pipeline.CACHE / "reviews/family-paths"
    out.mkdir(parents=True, exist_ok=True)
    seen, items, previous = set(), [], None
    for article in native["articles"]:
        result = results[article["number"]]
        path = tuple(article["path"])
        if path not in seen:
            region = result["regions"][0]
            image_path = folders[region["evidenceId"]] / "crop.png"
            image = Image.open(image_path)
            top = 0
            if previous:
                ending = previous["regions"][-1]
                if ending["evidenceId"] == region["evidenceId"]:
                    top = max(0, min(ending["box"][3] - 25, region["box"][1] - 200))
            box = [0, top, image.width, min(image.height, region["box"][1] + 130)]
            strip = out / f"heading-{len(items) + 1:03d}.png"
            image.crop(box).save(strip)
            items.append({"number": article["number"], "path": article["path"],
                          "evidenceId": region["evidenceId"], "sourceCropSha256": pipeline.hash_file(image_path),
                          "box": box, "image": str(strip.relative_to(pipeline.ROOT)).replace("\\", "/"),
                          "imageSha256": pipeline.hash_file(strip)})
            seen.add(path)
        previous = result
    sheets = []
    for start in range(0, len(items), 4):
        chunk = items[start:start + 4]
        images = []
        for item in chunk:
            source = Image.open(pipeline.ROOT / item["image"])
            images.append(source.resize((1200, round(source.height * 1200 / source.width))))
        sheet = Image.new("RGB", (1200, sum(i.height + 45 for i in images)), "white")
        draw, y = ImageDraw.Draw(sheet), 0
        for item, image in zip(chunk, images):
            draw.text((12, y + 10), "Family heading before " + item["number"] + "; " + item["evidenceId"], fill="black")
            sheet.paste(image, (0, y + 35))
            y += image.height + 45
            draw.line((0, y - 2, 1200, y - 2), fill="black", width=3)
        target = out / f"paths-{start // 4 + 1:03d}.png"
        sheet.save(target)
        sheets.append({"image": str(target.relative_to(pipeline.ROOT)).replace("\\", "/"),
                       "imageSha256": pipeline.hash_file(target), "numbers": [i["number"] for i in chunk]})
    pipeline.write_json(out / "index.json", {"nativeSha256": pipeline.hash_file(native_path),
        "comparisonSha256": pipeline.hash_file(comparison_path), "source": native["source"],
        "items": items, "sheets": sheets, "inspection": "not inferred from generation"})
    print(len(items), "distinct heading paths on", len(sheets), "sheets; none marked inspected")


if __name__ == "__main__":
    main()
