#!/usr/bin/env python3
"""Replay the twelve source-read JO restorations without changing raw, historical or typed law.

The saved reading record remains immutable. This separate research projection binds each edit
to its original diagnostic flag, complete body, official publication and actually inspected image.
"""
from __future__ import annotations

import copy
import re
from collections import defaultdict

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
INSPECTION = "actually read; not human/lawyer verification"
FINDINGS = {"source-read-JO-wording-restoration-proposed", "outside-following-instruction-or-article-label",
            "source-read-compilation-words-retained", "paired-wording-conflict-extra"}
CORRECTIONS = {
    ("A2005044", "72 مكرر"): [("فقدة", "فقده")],
    ("A2005044", "101"): [("عشرة", "عشر")],
    ("A2005044", "124 مكرر"): [("لاسيما", "لا سيما"), ("الاضرار", "الإضرار")],
    ("A2005044", "140 مكرر"): [("مسؤول", "مسؤولا")],
    ("A2007031", "468"): [("الا", "إلا"), ("ايجارا", "إيجارا")],
    ("A2007031", "484"): [("يتعين عل المستأجر", "يتعين على المستأجر")],
    ("A2007031", "490"): [("الاعفاء", "الإعفاء")],
    ("A2007031", "492"): [("بحلاف", "بخلاف")],
    ("A2007031", "499"): [("بالاقساط", "بالأقساط")],
    ("A2007031", "507"): [("الا", "إلا")],
}


def check_reading(record: dict, body: dict, diagnostic: dict, sources: dict,
                  root=pipeline.ROOT) -> None:
    if (record["number"] != body["number"] or record["inputTextSha256"] != pipeline.hash_text(body["text"])
            or body["text"] != "\n".join(body["paragraphs"])):
        raise ValueError("JO reading belongs to another body or paragraph projection")
    if (record["inspection"] != INSPECTION or not record["proofs"]
            or record["sourceId"] not in sources or diagnostic["number"] != record["number"]
            or not diagnostic["expectedPageOnly"] or set(diagnostic["ocr"]) != {"tesseract", "ppocr"}):
        raise ValueError("JO decision needs an actual two-reader source-page reading")
    compared = body["paragraphs"][0] if body["number"] == "489" else body["text"]
    if diagnostic["inputTextSha256"] != pipeline.hash_text(compared):
        raise ValueError("JO diagnostic belongs to another compared body")
    seen = set()
    for proof in record["proofs"]:
        image = (root / proof["image"]).resolve()
        if (proof["sourceId"] != record["sourceId"] or proof["sourceSha256"] != sources[record["sourceId"]]
                or proof["pdfPage"] not in diagnostic["instructionPdfPages"]
                or proof["image"] in seen or not image.is_relative_to(root.resolve())
                or pipeline.hash_file(image) != proof["imageSha256"]):
            raise ValueError("JO source proof changed, escaped its root or belongs to another publication/page")
        seen.add(proof["image"])


def validate_flags(readings: list[dict], diagnostics: dict, bodies: dict, sources: dict,
                   root=pipeline.ROOT) -> dict:
    expected = {(source, number, index): flag for (source, number), row in diagnostics.items()
                for index, flag in enumerate(row["strictWordFlags"])}
    actual = {(r["sourceId"], r["number"], r["flagIndex"]): r for r in readings}
    if len(actual) != len(readings) or set(actual) != set(expected):
        raise ValueError("Every JO word flag needs exactly one source-bound decision")
    for key, reading in actual.items():
        if (reading["flag"] != expected[key] or reading["finding"] not in FINDINGS
                or not reading["note"].strip()):
            raise ValueError("JO flag decision is stale, unknown or lacks a finding")
        check_reading(reading, bodies[key[1]], diagnostics[key[:2]], sources, root)
    return actual


def project(body: dict, proposals: list[dict], decisions: dict, diagnostic: dict, sources: dict,
            root=pipeline.ROOT) -> dict:
    if not proposals:
        return copy.deepcopy(body)
    source = proposals[0]["sourceId"]
    if [(p["old"], p["new"]) for p in proposals] != CORRECTIONS.get((source, body["number"])):
        raise ValueError("Only the closed list of actual JO word restorations may be applied")
    paragraphs = list(body["paragraphs"])
    for proposal in proposals:
        check_reading(proposal, body, diagnostic, sources, root)
        key = (proposal["sourceId"], proposal["number"], proposal["diagnosticFlagIndex"])
        reading = decisions.get(key)
        if (proposal["sourceId"] != source or reading is None
                or reading["finding"] != "source-read-JO-wording-restoration-proposed"
                or reading["flag"]["canonical"] != proposal["flagCanonical"]
                or reading["proofs"] != proposal["proofs"]
                or proposal["status"] != "source-read proposal; NOT applied"):
            raise ValueError("JO proposal must address its exact source-read diagnostic flag")
        old, new = proposal["old"], proposal["new"]
        text = "\n".join(paragraphs)
        if (text.count(old) != 1 or "\n" in old + new
                or re.findall(r"\d+", old) != re.findall(r"\d+", new)):
            raise ValueError("A JO restoration needs one unique context and cannot change digits or paragraphs")
        paragraphs = [p.replace(old, new, 1) if old in p else p for p in paragraphs]
    result = copy.deepcopy(body)
    text = "\n".join(paragraphs)
    if len(paragraphs) != len(body["paragraphs"]) or re.findall(r"\d+", text) != re.findall(r"\d+", body["text"]):
        raise ValueError("JO word restorations cannot change paragraph or numeric inventories")
    result.update(text=text, paragraphs=paragraphs, textSha256=pipeline.hash_text(text),
                  joWordingSourceId=source, joWordCorrections=copy.deepcopy(proposals),
                  fullArticleChecked=False)
    return result


def main() -> None:
    path = WORK / "priority-jo-word-readings.json"
    review = pipeline.read_json(path)
    for name, sha in review["inputFiles"].items():
        item = (WORK / name).resolve()
        if not item.is_relative_to(WORK.resolve()) or pipeline.hash_file(item) != sha:
            raise ValueError("JO wording review input changed")
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    sources = {}
    for source in inventory["sources"]:
        if source["id"] not in {"A2005044", "A2007031"}:
            continue
        pdf = (pipeline.ROOT / source["cachePath"]).resolve()
        if not pdf.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(pdf) != source["sha256"]:
            raise ValueError("Official JO publication pin changed")
        sources[source["id"]] = source["sha256"]
    bodies = {a["number"]: a for a in pipeline.read_json(WORK / "priority-layout-projection.json")["projectedCandidates"]}
    diagnostics = {}
    for source in sources:
        diagnostic = pipeline.read_json(WORK / f"priority-{source.lower()}-body-diagnostic-v2.json")
        if (diagnostic["inputProjectionSha256"] != review["inputFiles"]["priority-layout-projection.json"]
                or diagnostic["instructionInventorySha256"] != review["inputFiles"]["priority-amendment-inventory.json"]):
            raise ValueError("JO comparison projection/instruction binding changed")
        for row in diagnostic["results"]:
            key = (source, row["number"])
            if key in diagnostics:
                raise ValueError("Duplicate JO diagnostic body")
            diagnostics[key] = row
    decisions = validate_flags(review["wordFlagReadings"], diagnostics, bodies, sources)
    proposals = defaultdict(list)
    for proposal in review["correctionProposals"]:
        proposals[(proposal["sourceId"], proposal["number"])].append(proposal)
    if (set(proposals) != set(CORRECTIONS) or len(decisions) != 36 or len(diagnostics) != 59
            or sum(map(len, proposals.values())) != 12):
        raise ValueError("The saved JO correction/reading inventory changed")
    output = copy.deepcopy(bodies)
    for key, items in proposals.items():
        output[key[1]] = project(bodies[key[1]], items, decisions, diagnostics[key], sources)
    if output["469 مكرر 4"]["text"] != bodies["469 مكرر 4"]["text"] or "لا يجوز" not in output["469 مكرر 4"]["text"]:
        raise ValueError("The explicitly source-retained independent negative cannot be changed")
    pipeline.write_json(WORK / "priority-amended-body-projection.json", {
        "method": __doc__.strip(), "reviewSha256": pipeline.hash_file(path),
        "inputProjectionSha256": review["inputFiles"]["priority-layout-projection.json"],
        "diagnosticBodies": 59, "wordFlagsActuallyReadAndReplayed": len(decisions),
        "sourceWordCorrectionsApplied": 12, "distinctBodiesCorrected": 10,
        "mechanicalApplicationComplete": True, "projectedCandidates": list(output.values()),
        "punctuationFlagsPending": len(review["punctuationQueue"]),
        "typedCivilChanged": False, "fullArticleChecked": False,
        "remaining": "84-21 body collation, retained489paragraph provenance, punctuation, current operation replay, references/inventory/sample and typed partial import; dates remain separate."})
    print("36 source-bound word decisions replayed;12 JO restorations applied in10 research bodies; typed Civil unchanged")


if __name__ == "__main__":
    main()
