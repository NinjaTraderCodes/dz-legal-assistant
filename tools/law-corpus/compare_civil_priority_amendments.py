#!/usr/bin/env python3
"""Diagnose current Civil amended bodies against pinned original-JO OCR; never grant checks or alter law.

The07-05 manifest preserves complete column endings. Quoted-body boundaries, instruction extras,
word/digit/negation differences and paragraph-only amendments still require explicit source decisions.
"""
from __future__ import annotations

import check_native
import family_literal
import native_check as nc
import pipeline
from scope_inventory import number_key

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def main() -> None:
    review = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    bodies = {a["number"]: a for a in pipeline.read_json(WORK / "priority-layout-projection.json")["projectedCandidates"]}
    for source, manifest_name in [("A2005044", "civil-full-manifest.json"),
                                  ("A2007031", "civil-priority-07-05-manifest.json")]:
        rows = []
        for op in review["operations"]:
            if op["sourceId"] != source or op["operation"] == "repeal":
                continue
            for number in op["numbers"]:
                body = bodies[number]
                text = body["paragraphs"][0] if number == "489" else body["text"]
                rows.append({"number": number, "label": "المادة " + number, "text": text, "path": [],
                             "inputTextSha256": pipeline.hash_text(text), "instructionPdfPages": op["pdfPages"]})
        rows.sort(key=lambda a: number_key(a["number"]))
        manifest = pipeline.ROOT / "tools/law-corpus" / manifest_name
        result = check_native.run(rows, manifest, source)
        folders = check_native.evidence_folders(pipeline.load_manifest(manifest), source)
        streams = {"tesseract": check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(folders))),
                   "ppocr": check_native.without_leaders(check_native.without_headers(check_native.paddle_stream(folders)))}
        source_pages = {ident: pipeline.read_json(folder / "evidence.json")["pdfPage"] for ident, folder in folders}
        inputs = {a["number"]: a for a in rows}
        for row in result["results"]:
            article = inputs[row["number"]]
            tokens = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
            offset = len(nc.tokens(article["label"] + " :"))
            words, marks = family_literal.compare(tokens, row["ocr"], list(range(offset, len(tokens))))
            row.update(inputTextSha256=article["inputTextSha256"], strictWordFlags=words, strictMarkFlags=marks,
                       instructionPdfPages=article["instructionPdfPages"],
                       strictWordAgrees=bool(row["ocr"]) and not words,
                       alignedEvidenceIds={reader: sorted({t["source"] for t in streams[reader][bounds[0]:bounds[1]]})
                                           for reader, bounds in row["located"].items()})
            row["alignedPdfPages"] = {reader: sorted({source_pages[i] for i in ids})
                                      for reader, ids in row["alignedEvidenceIds"].items()}
            row["expectedPageOnly"] = (set(row["alignedPdfPages"]) == {"tesseract", "ppocr"}
                and all(pages and set(pages) <= set(article["instructionPdfPages"]) for pages in row["alignedPdfPages"].values()))
        result.update(inputProjectionSha256=pipeline.hash_file(WORK / "priority-layout-projection.json"),
                      instructionInventorySha256=pipeline.hash_file(WORK / "priority-amendment-inventory.json"),
                      method=__doc__.strip(), verificationEffect="diagnostic only; no body/state/date/import changes")
        pipeline.write_json(WORK / f"priority-{source.lower()}-body-diagnostic-v2.json", result)
        print(source, len(rows), "bodies; streams", result["streams"], "strict-word diagnostic agreements",
              sum(r["strictWordAgrees"] for r in result["results"]), "flags",
              sum(len(r["strictWordFlags"]) for r in result["results"]), flush=True)


if __name__ == "__main__":
    main()
