#!/usr/bin/env python3
"""Strict transcription diagnostic for all qualified Civil compilation candidates.

Printed editorial annotations and footnote callouts are retained, not guessed away. Agreement certifies neither
their legal status nor paragraphing, headings, amendment history or current applicability. Arabic-letter folding
is used only to locate spans; every body word/digit/negation uses family_literal's exact acceptance rule.
"""
from __future__ import annotations

import re

import check_native
import family_literal
import native_check as nc
import pipeline
import supplementary_ocr

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
MANIFEST = pipeline.ROOT / "tools/law-corpus/civil-ministry-manifest.json"


def inputs_for(articles: list[dict]) -> list[dict]:
    keys = [a["candidateKey"] for a in articles]
    if len(set(keys)) != len(keys):
        raise ValueError("Duplicate qualified Civil key")
    return [a | {"printedNumber": a["number"], "number": a["candidateKey"],
                 "label": a["printedLabel"].rstrip(": ").strip()} for a in articles]


def audit(articles: list[dict], comparison: dict) -> dict:
    by_key = {r["number"]: r for r in comparison["results"]}
    if len(comparison["results"]) != len(articles) or len(by_key) != len(articles) or set(by_key) != {a["candidateKey"] for a in articles}:
        raise ValueError("Comparison does not cover each qualified Civil identity exactly once")
    results = []
    for article in articles:
        result = by_key[article["candidateKey"]]
        tokens = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        offset = len(nc.tokens(article["label"] + " :"))
        words, marks = family_literal.compare(tokens, result["ocr"], list(range(offset, len(tokens))))
        two_readers = set(result["ocr"]) == {"tesseract", "ppocr"} and all(result["ocr"].values())
        if not two_readers:
            words.append({"kind": "missing-ocr-reader", "at": offset, "canonical": "",
                          "punctuationOnly": False})
        aligned = result.get("alignedEvidenceIds", {})
        page_sets = {}
        for reader, ids in aligned.items():
            matches = [re.fullmatch(r"civil-ministry-(\d+)-body", ident) for ident in ids]
            page_sets[reader] = {int(m[1]) for m in matches if m}
            if any(m is None for m in matches):
                raise ValueError("Civil alignment refers to another extraction set")
        if (set(page_sets) != {"tesseract", "ppocr"} or any(
                not pages or not pages <= set(article["pages"]) for pages in page_sets.values())):
            words.append({"kind": "source-page-mismatch", "at": offset, "canonical": "",
                          "punctuationOnly": False, "expectedPages": article["pages"],
                          "alignedPages": {reader: sorted(pages) for reader, pages in page_sets.items()}})
        results.append(result | {"candidateKey": article["candidateKey"], "printedNumber": article["printedNumber"],
            "numberScope": article["numberScope"], "pages": article["pages"], "textSha256": article["textSha256"],
            "wordAgrees": len(tokens) > offset and not words, "agrees": len(tokens) > offset and not words and not marks,
            "disagreements": words + marks})
    return {"method": __doc__.strip(), "articles": len(results),
            "wordAgreed": sum(r["wordAgrees"] for r in results), "agreed": sum(r["agrees"] for r in results),
            "wordReviewGroups": sum(not d["punctuationOnly"] for r in results for d in r["disagreements"]),
            "punctuationReviewGroups": sum(d["punctuationOnly"] for r in results for d in r["disagreements"]),
            "results": results, "verificationEffect": "none; research transcription comparison only"}


def verify_evidence(manifest: dict) -> None:
    source = manifest["sources"][0]
    source_pdf = pipeline.CACHE / "current-digital/civil-consolidated-ar.pdf"
    if pipeline.hash_file(source_pdf) != source["sha256"]:
        raise ValueError("Civil compilation differs from source pin")
    records = pipeline.evidence_by_id(pipeline.CACHE, {e["id"] for e in manifest["extracts"]})
    for extract in manifest["extracts"]:
        record, folder = records[extract["id"]]
        if record["sourceSha256"] != source["sha256"] or any(
                record.get(k) != extract.get(k) for k in ("sourceId", "pdfPage", "region", "clipPolygon")):
            raise ValueError("Civil extraction is bound to another source or crop")
        for artifact in record["artifacts"]:
            path = (pipeline.CACHE / artifact["path"]).resolve()
            if not path.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(path) != artifact["sha256"]:
                raise ValueError("Civil first-reader artifact changed")
        crop_sha = pipeline.hash_file(folder / "crop.png")
        candidates = [(p, pipeline.read_json(p)) for p in
                      (pipeline.TOOLS / "supplementary-ocr" / extract["id"]).glob("*/record.json")]
        candidates = [(p, r) for p, r in candidates if r.get("imageSha256") == crop_sha and r.get("status") == "extracted"]
        if not candidates:
            raise ValueError("Civil second-reader evidence is missing: " + extract["id"])
        path, second = max(candidates, key=lambda item: item[1]["recordedAt"])
        if (second["sourceSha256"] != source["sha256"] or second["evidenceId"] != extract["id"]
                or second["ocr"] != (path.parent / "ocr.txt").read_text(encoding="utf-8")
                or not supplementary_ocr.replay_record(second, path.parent, second["binding"])):
            raise ValueError("Civil second-reader artifact changed")


def main() -> None:
    native_path = WORK / "native-candidates.json"
    native = pipeline.read_json(native_path)
    manifest = pipeline.load_manifest(MANIFEST)
    verify_evidence(manifest)
    articles = inputs_for(native["articles"])
    input_path = WORK / "ministry-raw-input.json"
    pipeline.write_json(input_path, articles)
    print("Aligning", len(articles), "qualified Civil candidates with both modern-edition OCR streams", flush=True)
    comparison = check_native.run(articles, MANIFEST, "civil-consolidated-ar")
    folders = check_native.evidence_folders(manifest, "civil-consolidated-ar")
    streams = {"tesseract": check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(folders))),
               "ppocr": check_native.without_leaders(check_native.without_headers(check_native.paddle_stream(folders)))}
    for row in comparison["results"]:
        row["alignedEvidenceIds"] = {reader: sorted({t["source"] for t in streams[reader][bounds[0]:bounds[1]]})
                                     for reader, bounds in row["located"].items()}
    comparison["verificationEffect"] = "none; approximate alignment retained only as diagnostic evidence"
    comparison_path = WORK / "ministry-raw-comparison.json"
    pipeline.write_json(comparison_path, comparison)
    result = audit(articles, comparison)
    result |= {"source": native["source"], "nativeSha256": pipeline.hash_file(native_path),
               "inputSha256": pipeline.hash_file(input_path), "manifestSha256": pipeline.hash_file(MANIFEST),
               "comparisonSha256": pipeline.hash_file(comparison_path)}
    pipeline.write_json(WORK / "ministry-exact-audit.json", result)
    print(result["wordAgreed"], "/", len(articles), "strict word agreements;", result["wordReviewGroups"],
          "word/digit/extra groups;", result["punctuationReviewGroups"], "punctuation groups", flush=True)


if __name__ == "__main__":
    main()
