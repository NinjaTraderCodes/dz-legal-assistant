#!/usr/bin/env python3
"""Constrain existing Civil OCR spans to the article's source pages without deleting any token on those pages.

The global comparison remains unchanged. Only OCR tokens on neighbouring pages are removed from the new derived
comparison; no native body token, same-page heading, editorial annotation, digit or negation is excluded. Empty or
wrong-page readings still fail. Strict word acceptance is unchanged and no legal verification state is granted.
"""
from __future__ import annotations

import check_native
import civil_ministry_audit as civil
import pipeline


def bound(article: dict, result: dict, streams: dict) -> dict:
    allowed = {f"civil-ministry-{page:03d}-body" for page in article["pages"]}
    ocr, located, ids, discarded = {}, {}, {}, {}
    for reader, bounds in result["located"].items():
        indexed = list(enumerate(streams[reader][bounds[0]:bounds[1]], bounds[0]))
        selected = [(i, t) for i, t in indexed if t["source"] in allowed]
        discarded[reader] = sorted({t["source"] for _, t in indexed if t["source"] not in allowed})
        if selected:
            positions = [i for i, _ in selected]
            if positions != list(range(positions[0], positions[-1] + 1)):
                raise ValueError("Non-contiguous article-page projection; inspect the source rather than join disjoint readings")
            located[reader] = [positions[0], positions[-1] + 1]
            ocr[reader] = " ".join(t["token"] for _, t in selected)
            ids[reader] = sorted({t["source"] for _, t in selected})
    return result | {"ocr": ocr, "located": located, "alignedEvidenceIds": ids,
                     "regions": [r for r in result["regions"] if r["evidenceId"] in allowed],
                     "unboundedLocated": result["located"], "discardedNeighbourPageEvidenceIds": discarded}


def main() -> None:
    manifest = pipeline.load_manifest(civil.MANIFEST)
    civil.verify_evidence(manifest)
    input_path = civil.WORK / "ministry-raw-input.json"
    raw_path = civil.WORK / "ministry-raw-comparison.json"
    initial_path = civil.WORK / "ministry-exact-audit.json"
    initial = pipeline.read_json(initial_path)
    if (initial["inputSha256"] != pipeline.hash_file(input_path)
            or initial["comparisonSha256"] != pipeline.hash_file(raw_path)
            or initial["manifestSha256"] != pipeline.hash_file(civil.MANIFEST)):
        raise ValueError("Unbounded comparison/input/manifest changed")
    articles = pipeline.read_json(input_path)
    by_key = {a["candidateKey"]: a for a in articles}
    folders = check_native.evidence_folders(manifest, "civil-consolidated-ar")
    streams = {"tesseract": check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(folders))),
               "ppocr": check_native.without_leaders(check_native.without_headers(check_native.paddle_stream(folders)))}
    raw = pipeline.read_json(raw_path)
    if any(len(streams[reader]) != count for reader, count in raw["streams"].items()):
        raise ValueError("OCR stream length changed since global alignment")
    rows = [bound(by_key[row["number"]], row, streams) for row in raw["results"]]
    result = civil.audit(articles, {"results": rows})
    result |= {"alignmentProjection": __doc__.strip(), "inputSha256": pipeline.hash_file(input_path),
               "comparisonSha256": pipeline.hash_file(raw_path), "manifestSha256": pipeline.hash_file(civil.MANIFEST),
               "unboundedAuditSha256": pipeline.hash_file(initial_path), "source": initial["source"],
               "unboundedWordAgreed": initial["wordAgreed"],
               "articlesWithNeighbourPageTokensRemoved": sum(any(r["discardedNeighbourPageEvidenceIds"].values()) for r in rows)}
    pipeline.write_json(civil.WORK / "ministry-page-bounded-exact-audit.json", result)
    print(result["wordAgreed"], "/", len(articles), "strict word agreements;", result["wordReviewGroups"],
          "word/digit/extra groups;", result["punctuationReviewGroups"], "punctuation groups. No corpus states changed.")


if __name__ == "__main__":
    main()
