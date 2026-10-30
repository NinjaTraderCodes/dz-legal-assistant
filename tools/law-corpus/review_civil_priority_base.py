#!/usr/bin/env python3
"""Prepare original1975JO whole-body views for the selected unchanged Civil articles; never infer readings."""
from __future__ import annotations

import argparse

from PIL import Image

import check_native
import pipeline
import review_sheets
from scope_inventory import number_key

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("offset", type=int)
    parser.add_argument("count", type=int)
    args = parser.parse_args()
    if args.offset < 0 or args.count < 1:
        raise ValueError("A nonnegative offset and positive body count are required")
    bodies = pipeline.read_json(WORK / "priority-collated-body-projection.json")["projectedCandidates"]
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    amended = {n for op in inventory["operations"] for n in op["numbers"]}
    prior_checked = {"138", "139", "140", "379"}
    pending = sorted([b for b in bodies if b["number"] not in amended | prior_checked], key=lambda a:number_key(a["number"]))
    if len(pending) != 169:
        raise ValueError("Unchanged-body inventory excluding four existing source-read pilots changed")
    chosen = pending[args.offset:args.offset+args.count]
    name = f"civil-priority-base-{args.offset:04}-{args.offset+len(chosen):04}"
    out = pipeline.CACHE / "reviews" / name
    if (out / "index.json").exists():
        raise ValueError("Whole-body views already prepared; reuse their hashes and actual decisions")
    out.mkdir(parents=True,exist_ok=True)
    comparison = {r["number"]:r for r in pipeline.read_json(WORK / "base-unannotated-comparison.json")["results"]}
    folders = dict(check_native.evidence_folders(pipeline.load_manifest(pipeline.ROOT / "tools/law-corpus/civil-full-manifest.json"), "A1975078"))
    items=[]
    for body in chosen:
        number=body["number"]
        row=comparison[number]
        images, regions=[],[]
        for k,region in enumerate(row["regions"]):
            source=folders[region["evidenceId"]]/"crop.png"
            image=Image.open(source)
            box=[0,max(0,region["box"][1]-100),image.width,min(image.height,region["box"][3]+150)]
            path=out/f'article-{number}-{k}.png'
            image.crop(box).save(path,optimize=True)
            images.append(str(path))
            regions.append({"evidenceId":region["evidenceId"],"sourceImageSha256":pipeline.hash_file(source),"box":box})
        items.append({"id":number,"number":number,"title":f'Civil {number} original JO/current body; NOT YET READ',
            "text":body["text"],"paragraphs":body["paragraphs"],"textSha256":pipeline.hash_text(body["text"]),
            "images":images,"imageSha256s":[pipeline.hash_file(pipeline.Path(p)) for p in images],
            "regions":regions,"note":"Original1975JO comparison; not historical archive work. Read every clause, figure, negative and paragraph; distinguish actual edition wording differences from OCR noise or clipping."})
    sheets=review_sheets.render(items,out,name,per_sheet=2)
    for item in items:
        item["images"]=[pipeline.Path(p).relative_to(pipeline.ROOT).as_posix() for p in item["images"]]
    for sheet in sheets:
        sheet["sheet"]=pipeline.Path(sheet["sheet"]).relative_to(pipeline.ROOT).as_posix()
    pipeline.write_json(out/"index.json",{"name":name,"offset":args.offset,"count":len(chosen),"population":169,
        "sourceId":"A1975078","sourceSha256":"ba213b5beef85fb30959320b817594324e043842446c953cd86e22a074630ee9",
        "inputSha256":pipeline.hash_file(WORK/"priority-collated-body-projection.json"),
        "comparisonSha256":pipeline.hash_file(WORK/"base-unannotated-comparison.json"),
        "items":items,"sheets":sheets,"inspection":"not inferred from generation"})
    print(len(items),"whole-body original JO views;",len(sheets),"sheets generated; no readings inferred")


if __name__ == "__main__":
    main()
