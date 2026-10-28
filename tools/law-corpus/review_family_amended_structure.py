#!/usr/bin/env python3
"""Display the37 corrected Family amendment bodies against original-JO crops for paragraph/structure reading.

Generation grants no verdict. Each view keeps the final candidate hash, complete source region and crop hashes.
"""
from __future__ import annotations

from PIL import Image

import check_native
import pipeline
import review_sheets


def main() -> None:
    work = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
    articles = pipeline.read_json(work / "amended-corrected.json")
    results = {r["number"]: r for r in pipeline.read_json(work / "amended-comparison.json")["results"]}
    override_path = work / "amended-source-locator-overrides.json"
    if override_path.exists():
        for override in pipeline.read_json(override_path)["overrides"]:
            result = results[override["number"]]
            if result["regions"] != override["previousGeneratedRegions"]:
                raise ValueError("Generated locator changed; recheck the explicit override")
            results[override["number"]] = result | {"regions":override["regions"]}
    manifest = pipeline.load_manifest(pipeline.ROOT / "tools/law-corpus/family-current-manifest.json")
    folders = dict(check_native.evidence_folders(manifest,"A2005015"))
    out = pipeline.CACHE / "reviews/family-amended-structure"
    out.mkdir(parents=True,exist_ok=True)
    items = []
    for article in articles:
        number = article["number"]
        images, regions = [], []
        for k,region in enumerate(results[number]["regions"]):
            original = Image.open(folders[region["evidenceId"]] / "crop.png")
            box = [0,max(0,region["box"][1]-120),original.width,min(original.height,region["box"][3]+200)]
            path = out / (number.replace(" ","-") + f"-{k}.png")
            original.crop(box).save(path)
            images.append(str(path))
            regions.append({"evidenceId":region["evidenceId"],"box":box})
        if not images:
            raise ValueError("Amended article lacks source regions")
        items.append({"id":number,"number":number,"title":"Family " + number + " - paragraph/structure; NOT YET READ",
                      "text":article["text"],"paragraphs":article["paragraphs"],"images":images,"regions":regions,
                      "textSha256":pipeline.hash_text(article["text"]),"note":"Original amending JO; compare each paragraph, list boundary and continuation. "
                      + str(len(article["paragraphs"])) + " candidate paragraphs. Word corrections have a separate prior audit."})
    sheets = review_sheets.render(items,out,"family-amended-structure",per_sheet=2)
    pipeline.write_json(out / "index.json",{"items":items,"sheets":sheets,"inspection":"not-inferred-from-generation",
        "candidateSha256":pipeline.hash_file(work / "amended-corrected.json")})
    print(len(items),"amended article views;",len(sheets),"structure sheets; no decision inferred")


if __name__ == "__main__":
    main()
