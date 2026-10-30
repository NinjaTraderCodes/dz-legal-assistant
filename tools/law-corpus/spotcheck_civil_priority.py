#!/usr/bin/env python3
"""Draw20 Civil priority identities once, including repeals; render proof without inferring inspection."""
from __future__ import annotations

import argparse
import html
import random

from PIL import Image

import pipeline
import review_sheets
from scope_inventory import number_key

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
OUT = pipeline.ROOT / "docs/law-corpus-checks/civil-priority"
SEED = 20261030


def population() -> list[dict]:
    return sorted(pipeline.read_json(WORK / "priority-collated-body-projection.json")["projectedCandidates"],
                  key=lambda a: number_key(a["number"]))


def digest(rows: list[dict]) -> str:
    return pipeline.hash_json([{ "number": r["number"], "textSha256": pipeline.hash_text(r["text"]),
                                "paragraphs": r["paragraphs"], "kind": r["compilationBodyKind"]} for r in rows])


def report() -> None:
    selection = pipeline.read_json(OUT / "selection.json")
    inspection = pipeline.read_json(OUT / "inspection.json")
    rows = population()
    expected = [r["number"] for r in random.Random(SEED).sample(rows, 20)]
    if (selection["populationSha256"] != digest(rows) or selection["seed"] != SEED
            or [i["number"] for i in selection["items"]] != expected
            or inspection["selectionSha256"] != pipeline.hash_file(OUT / "selection.json")
            or [r["number"] for r in inspection["decisions"]] != expected):
        raise ValueError("Civil sample population, draw or explicit inspection changed")
    by_number = {r["number"]: r for r in rows}
    blocks = []
    for item, decision in zip(selection["items"], inspection["decisions"]):
        actual = by_number[item["number"]]
        if (item["text"] != actual["text"] or item["paragraphs"] != actual["paragraphs"]
                or decision["textSha256"] != pipeline.hash_text(actual["text"])
                or decision["imageSha256s"] != item["imageSha256s"]
                or decision["inspection"] != "actually read; not human/lawyer verification"
                or not decision["note"].strip() or decision["result"] not in {
                    "body-words-figures-negation-and-paragraphs-match-source", "explicit-repeal-target-matches-source"}):
            raise ValueError("Sample decision is not bound to the current text and actual proof")
        images = []
        for image, sha in zip(item["images"], item["imageSha256s"]):
            path = (pipeline.ROOT / image).resolve()
            if not path.is_relative_to(OUT.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Committed sample crop changed or escaped its output folder")
            images.append('<img src="' + html.escape(path.name) + '">')
        body = "".join('<p dir="rtl">' + html.escape(p) + '</p>' for p in item["paragraphs"])
        if not body:
            body = "<p>Current repeal marker; no former body is invented.</p>"
        blocks.append('<section><h2>Article ' + html.escape(item["number"]) + '</h2><p>'
                      + html.escape(item["sourceId"] + " | " + decision["note"])
                      + '</p><div class="pair"><div>' + ''.join(images) + '</div><div>' + body + '</div></div></section>')
    page = ('<!doctype html><meta charset="utf-8"><title>Civil priority sample</title><style>'
            'body{margin:24px;color:#111;background:white;font-family:serif}.pair{display:grid;grid-template-columns:1fr 1fr;gap:24px}'
            'img{display:block;max-width:100%}section{border-top:2px solid #555;padding:16px 0}p[dir]{font-size:25px;line-height:1.8}'
            '@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><h1>Civil priority source sample</h1>'
            '<p>20 fixed random identities from the274-identity partial package, seed20261030. Actual AI image readings; not lawyer verification. '
            '16 operative bodies and four explicit repeal targets. This sample is not a whole-Civil or historical-date certificate.</p>' + ''.join(blocks))
    (OUT / "comparison.html").write_text(page, encoding="utf-8")
    (OUT / "README.md").write_text('# Civil priority source sample\n\n20 identities drawn without replacement from the274-identity package, '
        'seed `20261030`. [Side-by-side source comparison](comparison.html). All20 decisions are explicit AI image readings; '
        '16 bodies and four repeal targets. No lawyer, whole-code, historical archive or date verification is claimed. '
        'Source/image/body hashes and the unchanged draw are in `selection.json` and `inspection.json`.\n', encoding="utf-8")
    print("20actual Civil sample decisions replayed; side-by-side report written")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report-only", action="store_true")
    if parser.parse_args().report_only:
        report()
        return
    if (OUT / "selection.json").exists():
        raise ValueError("Sample already drawn; preserve it rather than redraw after seeing results")
    OUT.mkdir(parents=True, exist_ok=True)
    rows = population()
    chosen = random.Random(SEED).sample(rows, 20)
    native = {a["number"]: a for a in pipeline.read_json(WORK / "native-candidates.json")["articles"] if a["numberScope"] == "main"}
    layout = {a["candidateKey"]: a for a in pipeline.read_json(WORK / "priority-layout-readings.json")["readings"]}
    operations = {n: op for op in pipeline.read_json(WORK / "priority-amendment-inventory.json")["operations"] for n in op["numbers"]}
    diagnostics = {source: {r["number"]: r for r in pipeline.read_json(WORK / f"priority-{source.lower()}-body-diagnostic-v2.json")["results"]}
                   for source in ["A2005044", "A2007031"]}
    wanted = {r["evidenceId"] for body in chosen if body["number"] in operations and operations[body["number"]]["operation"] != "repeal"
              for r in diagnostics[operations[body["number"]]["sourceId"]][body["number"]]["regions"]}
    records = pipeline.evidence_by_id(pipeline.CACHE, wanted)
    items = []
    for body in chosen:
        number = body["number"]
        op = operations.get(number)
        crops, proofs = [], []
        if op and op["operation"] == "repeal":
            # The fixed draw includes four07-05repeals; show its complete art8instruction,
            # including the printed508-to537range, without the following unrelated act.
            if op["sourceId"] != "A2007031":
                raise ValueError("A newly selected repeal needs its own explicit instruction crop")
            path = pipeline.CACHE / "extractions/4c0801865c086523c05c43ce7d533dad15df452a103a9411ca1bd3ec87f0b0cf/page-0006/civil-priority-07-05-006-left/crop.png"
            image = Image.open(path)
            crops = [(image.crop((0, 0, image.width, int(image.height * .47))), path, [0, 0, image.width, int(image.height * .47)])]
            source_id = "A2007031"
        elif op:
            source_id = op["sourceId"]
            for region in diagnostics[source_id][number]["regions"]:
                record, folder = records[region["evidenceId"]]
                path = folder / "crop.png"
                image = Image.open(path)
                box = [0, max(0, region["box"][1]-70), image.width, min(image.height, region["box"][3]+85)]
                crops.append((image.crop(box), path, box))
        else:
            source_id = "civil-consolidated-ar"
            raw = native[number]
            for proof in layout[raw["candidateKey"]]["proofs"]:
                path = pipeline.ROOT / proof["image"]
                if pipeline.hash_file(path) != proof["sha256"]:
                    raise ValueError("Read Ministry page image changed")
                image = Image.open(path)
                lines = [l for l in raw["lineEvidence"] if l["page"] == proof["pdfPage"]]
                scale = image.width / 595.0
                box = [65, max(0, int(image.height-max(l["box"][3] for l in lines)*scale)-30), image.width-60,
                       min(image.height, int(image.height-min(l["box"][1] for l in lines)*scale)+30)]
                crops.append((image.crop(box), path, box))
        images = []
        for index, (crop, source, box) in enumerate(crops):
            path = OUT / (number.replace(" مكرر", "-bis").replace(" ", "-") + f"-{index}.png")
            crop.save(path, optimize=True)
            images.append(path.relative_to(pipeline.ROOT).as_posix())
            proofs.append({"sourceImage": source.relative_to(pipeline.ROOT).as_posix(),
                           "sourceImageSha256": pipeline.hash_file(source), "pixelBox": box})
        items.append({"id": "civil-code:" + number.replace(" مكرر", "-bis").replace(" ", "-"), "number": number,
            "title": "Civil " + number + "; fixed random sample, not yet inspected", "text": body["text"],
            "paragraphs": body["paragraphs"], "textSha256": pipeline.hash_text(body["text"]),
            "bodyKind": body["compilationBodyKind"], "sourceId": source_id, "images": images,
            "imageSha256s": [pipeline.hash_file(pipeline.ROOT / p) for p in images], "proofs": proofs,
            "note": "Read all words, figures, negatives and paragraphs; for a repeal, verify its exact target or closed printed range."})
    render_items = [i | {"images": [str(pipeline.ROOT / p) for p in i["images"]]} for i in items]
    sheets = review_sheets.render(render_items, pipeline.CACHE / "reviews/civil-priority-random-20", "civil-priority", per_sheet=2)
    for sheet in sheets:
        sheet["sheet"] = pipeline.Path(sheet["sheet"]).relative_to(pipeline.ROOT).as_posix()
    pipeline.write_json(OUT / "selection.json", {"seed": SEED, "population": 274, "populationSha256": digest(rows),
        "inputProjectionSha256": pipeline.hash_file(WORK / "priority-collated-body-projection.json"),
        "items": items, "sheets": sheets, "inspection": "not inferred from generation"})
    print("20fixed sample identities and10sheets generated; no inspection verdict inferred")


if __name__ == "__main__":
    main()
