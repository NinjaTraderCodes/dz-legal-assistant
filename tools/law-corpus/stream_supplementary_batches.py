#!/usr/bin/env python3
"""Run existing hash-bound supplementary OCR in ready batches while the first recognizer is still extracting.

The original recognizer and its replay binding are unchanged. Batch manifests and progress are recoverable cache
artifacts. Missing evidence waits at most fifteen minutes per batch, then fails instead of fabricating a reading.
"""
from __future__ import annotations

import argparse
import time

import pipeline
import supplementary_ocr


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("--batch-size", type=int, default=24)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    if not 1 <= args.batch_size <= 64:
        raise ValueError("Batch size must be between1 and64")
    manifest_path = args.manifest.resolve()
    manifest = pipeline.load_manifest(manifest_path)
    requested = [e for e in manifest["extracts"] if e["language"] == "ara"]
    records = []
    cache = pipeline.TOOLS / "streaming-ocr" / pipeline.hash_file(manifest_path)
    cache.mkdir(parents=True, exist_ok=True)
    for start in range(0,len(requested),args.batch_size):
        batch = requested[start:start + args.batch_size]
        wanted = {e["id"] for e in batch}
        print("Waiting for first-recognizer evidence",start+1,"through",start+len(batch),flush=True)
        deadline = time.monotonic() + 900
        while set(pipeline.evidence_by_id(pipeline.CACHE,wanted)) != wanted:
            if time.monotonic() > deadline:
                raise TimeoutError("First recognizer did not deliver this batch; completed supplementary records are retained")
            time.sleep(2)
        batch_path = cache / f"batch-{start:03d}.json"
        pipeline.write_json(batch_path,manifest | {"extracts":batch})
        records.extend(supplementary_ocr.run(batch_path,cache / f"result-{start:03d}.json"))
        pipeline.write_json(args.out, {"manifest":str(manifest_path.relative_to(pipeline.ROOT)),
            "manifestSha256":pipeline.hash_file(manifest_path), "requested":len(requested), "completed":len(records),
            "records":records, "legalInventoryComplete":False, "verificationEffect":"none; supplementary OCR only"})
        print("Supplementary total",len(records),"/",len(requested),flush=True)


if __name__ == "__main__":
    main()
