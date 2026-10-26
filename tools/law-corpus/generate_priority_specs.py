#!/usr/bin/env python3
"""Bind the explicit priority research to extraction evidence; never infer legal text.

The transcription decisions remain data. Re-running this creates composition specs,
not a claim that unreviewed OCR or a whole code has been verified.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CORPUS = ROOT / "data/law/corpus"
RESEARCH = CORPUS / "research"
CACHE = ROOT / "tools/law-corpus/.cache/evidence"
DATE = "2026-10-25"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def identity(code, number):
    return code + ":" + number.replace(" مكرر", "-bis").replace(" ", "-")


def evidence_ids(item):
    code, number, publication = item["code"], item["number"], item["publicationId"]
    if code == "civil-code":
        if publication == "law-05-10":
            return ["civil-amendment-05-10-article-7"]
        if number == "140":
            return ["civil-140-original-a", "civil-140-original-b"]
        return ["civil-" + number + "-original"]
    if code == "commercial-code":
        return ["commercial-187-bis" + ("-1" if number.endswith(" 1") else "") + "-introduced"]
    return ["cpca-" + number + "-original"]


def main():
    candidates = read(RESEARCH / "priority-transcriptions.json")["items"]
    structures = read(RESEARCH / "french-structure-locators.json")["items"]
    decisions_path = RESEARCH / "priority-image-decisions.json"
    decisions = read(decisions_path)["decisions"] if decisions_path.exists() else []
    decisions = {(d["code"], d["number"], d["publicationId"]): d for d in decisions}
    evidence = {read(p)["id"]: (read(p), p.parent) for p in (CACHE / "extractions").glob("*/page-*/*/evidence.json")}
    outputs = []
    for code in ("civil-code", "commercial-code", "cpca"):
        ledger = read(CORPUS / code / "ledger.json")
        grouped = {}
        for item in candidates:
            if item["code"] != code:
                continue
            number, publication = item["number"], item["publicationId"]
            article_id = identity(code, number)
            article = grouped.setdefault(number, {
                "id": article_id,
                "number": "1." + number[1:] if number in ("1002", "1003") else number,
                "canonicalNumber": number, "path": item["path"], "availability": "text", "versions": [],
            })
            start = "1975-07-05" if code == "civil-code" and publication == "ordinance-75-58" else "2009-04-23" if code == "cpca" else None
            temporal = "unsettled" if start is None or (code == "civil-code" and number == "7") else "resolved"
            locators = [s for s in structures if (s["code"], s["number"], s["publicationId"]) == (code, number, publication)]
            if len(locators) != 1:
                raise ValueError(f"Unique structural locator required: {article_id}@{publication}")
            structure = locators[0]
            if structure["arabicParagraphCount"] != len(item["paragraphs"]) or structure["frenchParagraphCount"] != len(item["paragraphs"]):
                raise ValueError(f"Paragraph disagreement: {article_id}@{publication}")
            checks = [{"check": "french-structure-matches", "method": "Codex visual alignment of official Arabic/French page images; presence, numbering/alias and paragraph count only",
                       "date": DATE, "result": "passed", "note": json.dumps(structure, ensure_ascii=False, sort_keys=True)}]
            cross_refs = []
            if code == "commercial-code" and number == "187 مكرر 1":
                cross_refs = [{"targetId": "commercial-code:187-bis", "paragraph": 1, "quote": "المادة 187 مكرر أعلاه"}]
            version = {
                "versionId": article_id + "@" + publication, "text": item["text"], "paragraphs": item["paragraphs"],
                "inForceFrom": start, "inForceTo": None, "temporalStatus": temporal,
                "introducedBy": publication, "status": "in-force", "crossReferences": cross_refs,
                "evidenceIds": evidence_ids(item), "verification": {"checks": checks, "reasons": item.get("unresolvedGlyphNotes", [])},
            }
            if item["text"] != "\n".join(item["paragraphs"]):
                raise ValueError(f"Paragraphs do not reconstruct candidate text: {article_id}")
            decision = decisions.get((code, number, publication))
            if decision and decision["decision"] == "transcription-resolved":
                images = []
                for key in version["evidenceIds"]:
                    record, folder = evidence[key]
                    image_hash = hashlib.sha256((folder / "crop.png").read_bytes()).hexdigest()
                    matching = [a["sha256"] for a in record["artifacts"] if a["path"].endswith("/crop.png")]
                    if matching != [image_hash]:
                        raise ValueError(f"Crop hash does not match evidence: {key}")
                    images.append(image_hash)
                version["imageReview"] = {
                    "decision": "transcription-resolved", "reviewedOn": decision["reviewedOn"], "note": decision["note"],
                    "textSha256": hashlib.sha256(item["text"].encode("utf-8")).hexdigest(), "imageSha256s": images,
                }
            article["versions"].append(version)
        spec = {"schemaVersion": "1.0", "codeId": code, "generatedOn": DATE, "unappliedPublications": [], "articles": list(grouped.values())}
        path = RESEARCH / "priority-specs" / (code + ".json")
        write(path, spec)
        outputs.append({"codeId": code, "articles": len(grouped), "versions": sum(len(a["versions"]) for a in grouped.values())})
    print(json.dumps(outputs))


if __name__ == "__main__":
    main()
