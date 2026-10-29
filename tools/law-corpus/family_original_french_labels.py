#!/usr/bin/env python3
"""Replay the actually read 1984 French label inventory, separately from OCR and paragraph/translation checks."""
from __future__ import annotations

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/family-french-original-manifest.json"


def label_inventory(review: dict) -> list[str]:
    rows = review["columnsActuallyRead"]
    keys = [(r["pdfPage"], r["column"]) for r in rows]
    expected_keys = [(p, side) for p in range(2, 16) for side in ("left", "right")]
    if keys != expected_keys:
        raise ValueError("French label reading must cover each source column once in reading order")
    labels = []
    for row in rows:
        if row["inspection"] != "actually read; labels/presence only; not full-word or paragraph certification":
            raise ValueError("French source column lacks an actual label reading")
        if row["printedPage"] != row["pdfPage"] + 610 or not row["labels"]:
            raise ValueError("French source page/label reading is incomplete")
        if row["evidenceId"] != f"family-fr-original-{row['pdfPage']:03d}-{row['column']}":
            raise ValueError("French reading points to the wrong evidence column")
        labels.extend(row["labels"])
    expected = [str(n) for n in range(1, 225)]
    if labels != expected:
        raise ValueError("Actually read French labels have a missing, duplicate, unexpected or reordered identity")
    return labels


def replay(review: dict, root=pipeline.ROOT, cache=pipeline.CACHE) -> dict:
    labels = label_inventory(review)
    manifest_path = root / review["manifest"]
    if pipeline.hash_file(manifest_path) != review["manifestSha256"]:
        raise ValueError("French reading manifest changed")
    manifest = pipeline.load_manifest(manifest_path)
    pin = manifest["sources"][0]
    if len(manifest["sources"]) != 1 or pin["id"] != "F1984024" or pin["sha256"] != review["sourceSha256"]:
        raise ValueError("French label reading source changed")
    if pipeline.hash_file(cache / "F1984024.pdf") != pin["sha256"]:
        raise ValueError("French source PDF changed")
    expected = {r["evidenceId"]: r for r in review["columnsActuallyRead"]}
    if {e["id"] for e in manifest["extracts"]} != set(expected) or len(manifest["extracts"]) != 28:
        raise ValueError("French OCR column inventory differs from the actual reading")
    records = pipeline.evidence_by_id(cache, set(expected))
    if set(records) != set(expected):
        raise ValueError("French OCR column evidence is incomplete")
    images = {r["image"]: r for r in review["imagesActuallyRead"]}
    if len(images) != len(review["imagesActuallyRead"]):
        raise ValueError("Duplicate French source-image proof")
    for image in images.values():
        path = (root / image["image"]).resolve()
        if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != image["imageSha256"]:
            raise ValueError("French source-image proof changed or escaped the repository")
    for ident, row in expected.items():
        record, _ = records[ident]
        if (record["sourceSha256"] != pin["sha256"] or record["pdfPage"] != row["pdfPage"]
                or record["region"] != next(e["region"] for e in manifest["extracts"] if e["id"] == ident)
                or record["ocrInvocation"]["exitCode"] != 0):
            raise ValueError("French column OCR source, page, region or process differs from its reading")
        for artifact in record["artifacts"]:
            path = (cache / artifact["path"]).resolve()
            if not path.is_relative_to(cache.resolve()) or pipeline.hash_file(path) != artifact["sha256"]:
                raise ValueError("French OCR artifact changed or escaped the cache")
        proof = images.get(row["image"])
        if proof is None or proof["pdfPage"] != row["pdfPage"] or proof["imageSha256"] != row["imageSha256"]:
            raise ValueError("French column lacks its actually read page-image proof")
    if review["verificationEffect"] != "Base numbering/presence only; no paragraph, translation, Arabic conflict, date or checked-state inference":
        raise ValueError("French label review must not claim an unrelated verification")
    return {"originalFrenchLabelsRead": len(labels), "missingBaseLabels": [], "duplicateBaseLabels": [],
            "ocrColumnsReplayed": len(records), "paragraphComparisonComplete": False,
            "arabicConflictResolved": False, "checkedStatesGranted": 0}


def main() -> dict:
    review = pipeline.read_json(WORK / "original-french-label-review.json")
    result = replay(review)
    pipeline.write_json(WORK / "original-french-label-audit.json", {"method": __doc__.strip(), **result})
    print("Original French inventory: all224 printed labels actually read; 28 OCR columns replayed; paragraph QA remains separate")
    return result


if __name__ == "__main__":
    main()
