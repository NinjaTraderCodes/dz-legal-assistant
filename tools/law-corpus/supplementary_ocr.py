#!/usr/bin/env python3
"""A hash-bound second Arabic OCR stream, separate from canonical text and reviews."""
import argparse
from datetime import datetime, timezone
import time
import pipeline
from compare_ocr import paddle_engine, ordered_lines


def binding_for(evidence, runtime, implementation):
    return pipeline.hash_json({"evidence": evidence, "runtime": runtime, "implementation": implementation})


def replay_record(record, folder, expected_binding):
    if record.get("binding") != expected_binding or record.get("status") != "extracted":
        return False
    for artifact in record.get("artifacts", []):
        path = (folder / artifact["file"]).resolve()
        if not path.is_relative_to(folder.resolve()) or not path.is_file() or pipeline.hash_file(path) != artifact["sha256"]:
            raise ValueError("Supplementary artifact changed or escaped its evidence folder")
    return {a["file"] for a in record.get("artifacts", [])} == {"raw.json", "ocr.txt"}


def run(manifest_path, out):
    manifest = pipeline.load_manifest(manifest_path)
    requested = [e for e in manifest["extracts"] if e.get("language") == "ara"]
    # Verify only this manifest's evidence; no races with other extraction sets.
    evidence = pipeline.evidence_by_id(pipeline.CACHE, {e["id"] for e in requested})
    engine, runtime = paddle_engine()
    implementation = {name: pipeline.hash_file(pipeline.Path(__file__).parent / name)
                      for name in ("supplementary_ocr.py", "compare_ocr.py")}
    records=[]
    for index, item in enumerate(requested, 1):
        ev, source_folder = evidence[item["id"]]
        # The existing raw evidence retains source hash, page/crop and PNG hash.
        if any(ev.get(k) != item.get(k) for k in ("sourceId", "pdfPage", "region", "clipPolygon")):
            raise ValueError("Manifest and source extraction differ: " + item["id"])
        binding = binding_for(ev, runtime, implementation)
        folder = pipeline.TOOLS / "supplementary-ocr" / item["id"] / binding
        record_path = folder / "record.json"
        if record_path.exists() and replay_record(pipeline.read_json(record_path), folder, binding):
            record = pipeline.read_json(record_path)
            print(index, '/', len(requested), item["id"], 'resumed', flush=True)
        else:
            folder.mkdir(parents=True, exist_ok=True)
            start = time.perf_counter()
            output = engine(source_folder / "crop.png")
            elapsed = time.perf_counter() - start
            raw = output.to_json() or []
            boxes = [] if output.boxes is None else output.boxes.tolist()
            text = "\n".join(ordered_lines(boxes, output.txts or []))
            pipeline.write_json(folder / "raw.json", raw)
            (folder / "ocr.txt").write_text(text, encoding="utf-8")
            record = {"evidenceId": item["id"], "binding": binding, "sourceId": ev["sourceId"],
                      "sourceUrl": ev["sourceUrl"], "sourceSha256": ev["sourceSha256"],
                      "pdfPage": ev["pdfPage"], "region": ev["region"],
                      "imageSha256": pipeline.hash_file(source_folder / "crop.png"),
                      "runtime": runtime, "implementation": implementation,
                      "recordedAt": datetime.now(timezone.utc).isoformat(), "seconds": elapsed,
                      "status": "extracted" if text.strip() else "empty-unsettled", "ocr": text,
                      "textLayerComparison": pipeline.compare((source_folder / "text-layer.txt").read_text(encoding="utf-8"), text),
                      "firstOcrComparison": pipeline.compare((source_folder / "ocr.txt").read_text(encoding="utf-8-sig"), text),
                      "verificationEffect": "none; recognizer agreement does not establish a legal text check",
                      "artifacts": [{"file": name, "sha256": pipeline.hash_file(folder / name)} for name in ("raw.json", "ocr.txt")]}
            pipeline.write_json(record_path, record)
            print(index, '/', len(requested), item["id"], len(text), round(elapsed, 2), flush=True)
        records.append(record)
        pipeline.write_json(out, {"manifest": str(manifest_path.relative_to(pipeline.ROOT)),
                                  "manifestSha256": pipeline.hash_file(manifest_path),
                                  "requested": len(requested), "completed": len(records),
                                  "records": records, "legalInventoryComplete": False,
                                  "note": "Secondary Arabic OCR only; no body transcription, paragraph or amendment verification is inferred."})
    return records


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    run(args.manifest.resolve(), args.out)
