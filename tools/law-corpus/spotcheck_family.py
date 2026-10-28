#!/usr/bin/env python3
"""Draw 20 Family snapshot articles without replacement; generate proof, never an inspection verdict.

Uses the introducing JO for amended bodies and the explicitly identified Ministry witness for unchanged bodies.
The known clipped art178 closing line gets its own small next-page crop. No whole-code verification is granted.
"""
from __future__ import annotations

import argparse
import html
import random

from PIL import Image

import check_native
import pipeline
import review_sheets

ROOT = pipeline.ROOT
WORK = ROOT / "data/law/corpus/research/family-2026-10-28"
OUT = ROOT / "docs/law-corpus-checks/family-code"
SEED = 20261028


def report() -> None:
    selection = pipeline.read_json(OUT / "selection.json")
    inspection = pipeline.read_json(OUT / "inspection.json")
    current = pipeline.read_json(ROOT / "data/law/corpus/family-code/articles.json")["articles"]
    population = [{"id": a["id"], "textSha256": pipeline.hash_text(a["versions"][0]["text"])} for a in current]
    expected = [a["id"] for a in random.Random(SEED).sample(current, 20)]
    if (inspection["selectionSha256"] != pipeline.hash_file(OUT / "selection.json")
            or selection["populationSha256"] != pipeline.hash_json(population)
            or [i["id"] for i in selection["items"]] != expected
            or [d["id"] for d in inspection["decisions"]] != expected):
        raise ValueError("Sample or inspection is stale; draw/inspect explicitly rather than reusing a verdict")
    decisions = {d["id"]: d for d in inspection["decisions"]}
    blocks = []
    lines = ["# Family current-candidate visual sample", "",
             "20 articles drawn without replacement from 232 identities, seed `20261028`. "
             "[Open the side-by-side comparison](comparison.html). Source crops are included beside this file.", "",
             "All ten generated review sheets were actually read by Codex (AI) on 28 October 2026. "
             "The 20 sampled bodies' words, figures, negation and paragraphing match their identified source. "
             "This is not human/lawyer verification or a claim of whole-code completion.", "",
             "15 unchanged bodies use the current Ministry print; five amended bodies use the amending JO. "
             "The known art. 178 continuation is included. Source selection and printed-vocalization limits are "
             "in `inspection.json`; the other Family QA remains open.", ""]
    if selection.get("populationUpdates"):
        lines += ["The sample was retained after the guarded correction to unsampled article 42: all 20 selected texts and "
                  "paragraphs replay unchanged. The population update and prior selection hash remain recorded. "
                  "The Ministry-source readings of sampled articles 135, 151 and 203 do not settle their separately "
                  "recorded original-JO edition conflicts; those candidates are now explicitly unsettled.", ""]
    for item in selection["items"]:
        decision = decisions[item["id"]]
        actual = next(a["versions"][0] for a in current if a["id"] == item["id"])
        if (any(decision[k] != item[k] for k in ("versionId", "textSha256", "images", "imageSha256s"))
                or item["text"] != actual["text"] or item["paragraphs"] != actual["paragraphs"]
                or pipeline.hash_text(item["text"]) != item["textSha256"]
                or not item["images"] or len(item["images"]) != len(item["imageSha256s"])
                or decision["result"] != "body-words-figures-negation-and-paragraphs-match-displayed-source"):
            raise ValueError("Decision belongs to different text/proof")
        images = []
        for image, sha in zip(item["images"], item["imageSha256s"]):
            path = (ROOT / image).resolve()
            if not path.is_relative_to(ROOT.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Sample proof changed or escaped workspace")
            images.append(f'<img src="{html.escape(path.name)}" alt="Official source for {html.escape(item["number"])}">')
        text = "".join('<p dir="rtl">' + html.escape(p) + "</p>" for p in item["paragraphs"])
        blocks.append('<section><h2>Article ' + html.escape(item["number"]) + '</h2><p class="meta">'
                      + html.escape(item["sourceId"] + " | " + decision["note"]) + '</p><div class="pair"><div>'
                      + "".join(images) + '</div><div class="body">' + text + '</div></div></section>')
        lines += ["## Article " + item["number"], "", "Source: `" + item["sourceId"] + "`. " + decision["note"], ""]
        for image in item["images"]:
            name = pipeline.Path(image).name
            lines += ["![Official source](" + name + ")", ""]
        lines += ["> " + "\n>\n> ".join(item["paragraphs"]), ""]
    document = ("<!doctype html><html lang='en'><meta charset='utf-8'><title>Family source sample</title><style>"
                "body{margin:24px;background:#fff;color:#111;font-family:serif}h1,h2,.meta{font-family:sans-serif}"
                "section{border-top:2px solid #555;padding:16px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:24px}"
                "img{display:block;max-width:100%;height:auto}.body{font-size:26px;line-height:1.9}.meta{font-size:14px}"
                "@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><h1>Family current-candidate sample</h1>"
                "<p>20 actual AI image readings; source and canonical body. Remaining whole-code QA is open.</p>"
                + "".join(blocks) + "</html>")
    (OUT / "comparison.html").write_text(document, encoding="utf-8", newline="\n")
    (OUT / "README.md").write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print("20 sample decisions replayed against current population, text and proof hashes; report written")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report-only", action="store_true", help="Validate already recorded inspections and write the report")
    if parser.parse_args().report_only:
        report()
        return
    content = pipeline.read_json(ROOT / "data/law/corpus/family-code/articles.json")
    population = content["articles"]
    selected = random.Random(SEED).sample(population, 20)
    source_ids = {"law-84-11": "family-consolidated-bilingual", "ordinance-05-02": "A2005015"}
    comparisons = {"law-84-11": pipeline.read_json(WORK / "ministry-raw-comparison.json"),
                   "ordinance-05-02": pipeline.read_json(WORK / "amended-comparison.json")}
    results = {publication: {r["number"]: r for r in check["results"]} for publication, check in comparisons.items()}
    manifests = {"law-84-11": "family-ministry-manifest.json", "ordinance-05-02": "family-current-manifest.json"}
    folders = {publication: dict(check_native.evidence_folders(pipeline.load_manifest(ROOT / "tools/law-corpus" / manifest),
                                                             source_ids[publication])) for publication, manifest in manifests.items()}
    OUT.mkdir(parents=True, exist_ok=True)
    items = []
    for article in selected:
        version = article["versions"][0]
        publication = version["introducedBy"]
        if version["status"] != "in-force":
            raise ValueError("This seeded sample includes a repeal; add explicit instruction views rather than drawing again")
        number = article["number"]
        images, evidence = [], []
        for index, region in enumerate(results[publication][number]["regions"]):
            folder = folders[publication][region["evidenceId"]]
            image = Image.open(folder / "crop.png")
            box = [0, max(0, region["box"][1] - 90), image.width, min(image.height, region["box"][3] + 100)]
            path = OUT / (article["id"].split(":")[1] + f"-{index}.png")
            image.crop(box).save(path, optimize=True)
            images.append(str(path))
            evidence.append({"evidenceId": region["evidenceId"], "sourceCropSha256": pipeline.hash_file(folder / "crop.png"), "pixelBox": box})
        if number == "178":
            folder = folders[publication]["family-ministry-037-right"]
            image = Image.open(folder / "crop.png")
            box = [0, 0, image.width, 180]
            path = OUT / "178-continuation.png"
            image.crop(box).save(path, optimize=True)
            images.append(str(path))
            evidence.append({"evidenceId": "family-ministry-037-right", "sourceCropSha256": pipeline.hash_file(folder / "crop.png"), "pixelBox": box})
        items.append({"id": article["id"], "number": number, "versionId": version["versionId"],
                      "title": "Family " + number + " — random sample; NOT YET INSPECTED", "text": version["text"],
                      "textSha256": pipeline.hash_text(version["text"]), "paragraphs": version["paragraphs"],
                      "images": images, "imageSha256s": [pipeline.hash_file(pipeline.Path(p)) for p in images],
                      "sourceId": source_ids[publication], "evidence": evidence,
                      "note": "Check all words, figures, negation, body boundaries and paragraphing; source " + source_ids[publication]})
    review_folder = pipeline.CACHE / "reviews/family-random-20"
    sheets = review_sheets.render(items, review_folder, "family-random-20", per_sheet=2)
    # Committed selection uses repository-relative paths, never this machine's absolute workspace path.
    for item in items:
        item["images"] = [str(pipeline.Path(p).relative_to(ROOT)).replace("\\", "/") for p in item["images"]]
    for sheet in sheets:
        sheet["sheet"] = str(pipeline.Path(sheet["sheet"]).relative_to(ROOT)).replace("\\", "/")
    pipeline.write_json(OUT / "selection.json", {"seed": SEED, "count": 20, "population": len(population),
        "populationSha256": pipeline.hash_json([{ "id": a["id"], "textSha256": pipeline.hash_text(a["versions"][0]["text"])} for a in population]),
        "items": items, "sheets": sheets, "inspection": "not-inferred-from-generation"})
    print("20 seeded articles, 10 sheets generated; no inspection verdict inferred")


if __name__ == "__main__":
    main()
