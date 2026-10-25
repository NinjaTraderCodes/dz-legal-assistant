#!/usr/bin/env python3
"""Independent OCR label observations. Missing OCR is never a legal repeal or invented article."""
from __future__ import annotations

import argparse
from collections import Counter
import re
import unicodedata

import pipeline

CONTROLS = "\u200e\u200f\u202a\u202b\u202c\u202d\u202e\u2066\u2067\u2068\u2069"
ARABIC = re.compile(r"^[\s" + CONTROLS + r"«»\"'(:.ـ]*المادة\s+([0-9٠-٩۰-۹]+(?:[.,][0-9٠-٩۰-۹]{3})*|الأولى|الاولى|الاولىٰ)(?=\s|[:.،؛\-])")
FRENCH = re.compile(r"^\s*(?:[«\"]\s*)?(?:Art[.,]?|Article)\s+([0-9]+(?:[.,][0-9]{3})*|1er)(?=\s|[.,\-—:])", re.I)


def labels(text, language):
    pattern = ARABIC if language == "ara" else FRENCH
    result, offset = [], 0
    for line in text.splitlines(keepends=True):
        match = pattern.match(line)
        if match:
            printed = match[1]
            if printed in ("الأولى", "الاولى", "الاولىٰ", "1er"):
                number = "1"
            else:
                number = "".join(str(unicodedata.digit(c)) for c in printed if c.isdecimal())
                number = str(int(number)) if number else ""
            result.append({"numberFromOcr": number, "ocrLabel": match[0], "offset": offset,
                           "labelEndOffset": offset + match.end(), "line": line.rstrip("\r\n")})
        offset += len(line)
    return result


def build_index(manifest, cache, source_id, last_number):
    source = next(s for s in manifest["sources"] if s["id"] == source_id)
    requested = [x for x in manifest["extracts"] if x["sourceId"] == source_id]
    observations, missing_extracts = [], []
    for extract in requested:
        folder = cache / "extractions" / source["sha256"] / f"page-{extract['pdfPage']:04}" / extract["id"]
        record_path = folder / "evidence.json"
        if not record_path.exists():
            missing_extracts.append(extract["id"])
            continue
        record = pipeline.read_json(record_path)
        ocr_path = folder / "ocr.txt"
        pins = {a["path"].replace("\\", "/"): a["sha256"] for a in record["artifacts"]}
        relative = str(ocr_path.relative_to(cache)).replace("\\", "/")
        if (pipeline.hash_file(ocr_path) != pins.get(relative) or record["sourceSha256"] != source["sha256"]
                or record["region"] != extract["region"] or record["pdfPage"] != extract["pdfPage"]):
            raise ValueError("Observation evidence differs from pinned extraction")
        found = labels(ocr_path.read_text(encoding="utf-8"), extract["language"])
        for item in found:
            observations.append(item | {"sourceId": source_id, "sourceSha256": source["sha256"],
                                       "pdfPage": extract["pdfPage"], "evidenceId": extract["id"],
                                       "ocrSha256": pipeline.hash_file(ocr_path), "region": extract["region"]})
    counts = Counter(o["numberFromOcr"] for o in observations)
    expected = {str(i) for i in range(1, last_number + 1)}
    jumps = []
    for before, after in zip(observations, observations[1:]):
        if int(after["numberFromOcr"]) != int(before["numberFromOcr"]) + 1:
            jumps.append({"before": before["numberFromOcr"], "after": after["numberFromOcr"],
                          "beforeEvidenceId": before["evidenceId"], "afterEvidenceId": after["evidenceId"]})
    return {"sourceId": source_id, "source": source, "lastOriginalNumber": last_number,
            "method": "Anchored article-label syntax only. Number canonicalization is restricted to identity observations; body text is untouched.",
            "warning": "OCR observations are candidates, not a certified inventory. No missing or misread number is inferred from neighbours. All anomalies require official-image checking.",
            "observations": observations, "observedDistinct": len(set(counts) & expected),
            "notObserved": sorted(expected - set(counts), key=int),
            "outOfRangeObservations": sorted(set(counts) - expected, key=int),
            "duplicateObservations": {key: value for key, value in counts.items() if value > 1},
            "sequenceAnomalies": jumps, "missingExtracts": missing_extracts, "inventoryComplete": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("source")
    parser.add_argument("last", type=int)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    result = build_index(pipeline.load_manifest(args.manifest), pipeline.CACHE, args.source, args.last)
    pipeline.write_json(args.out, result)
    print(f"{args.source}: {result['observedDistinct']}/{args.last} distinct original number candidates; "
          f"{len(result['sequenceAnomalies'])} sequence anomalies; {len(result['missingExtracts'])} pending extracts")


if __name__ == "__main__":
    main()
