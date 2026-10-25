#!/usr/bin/env python3
"""Compare pinned cache identities with fresh official HTTPS bytes; never replace a pin."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
import argparse
import hashlib
import urllib.request
import pipeline


def verify(source):
    record = {"sourceId": source["id"], "url": source["url"], "expectedSha256": source["sha256"],
              "checkedAt": datetime.now(timezone.utc).isoformat()}
    try:
        opener = urllib.request.build_opener(pipeline.OfficialRedirect())
        request = urllib.request.Request(pipeline.official_url(source["url"]), headers={"User-Agent": "DZLegal-LawCorpus/1.0"})
        digest, length, header = hashlib.sha256(), 0, b""
        with opener.open(request, timeout=45) as response:
            record["resolvedUrl"] = pipeline.official_url(response.url)
            while block := response.read(1024 * 1024):
                if not header:
                    header = block[:1024]
                length += len(block)
                if length > 150 * 1024 * 1024:
                    raise ValueError("Official PDF exceeds150MiB")
                digest.update(block)
        if not header.lstrip().startswith(b"%PDF-"):
            raise ValueError("Official response is not a PDF")
        record.update(observedSha256=digest.hexdigest(), byteLength=length,
                      result="matches-pin" if digest.hexdigest() == source["sha256"] else "changed-official-bytes; pin-not-replaced")
    except Exception as error:
        record.update(result="unavailable", error=str(error))
    print(source["id"] + ": " + record["result"], flush=True)
    return record


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    manifest = pipeline.load_manifest(args.manifest)
    results = []
    with ThreadPoolExecutor(max_workers=3) as pool:
        for future in as_completed([pool.submit(verify, source) for source in manifest["sources"]]):
            results.append(future.result())
            pipeline.write_json(args.out, {"manifestSha256": pipeline.hash_file(args.manifest),
                "note": "Fresh official bytes checked against existing pins. No cached source or expected hash is replaced.",
                "records": sorted(results, key=lambda r: r["sourceId"])})
    return 0 if all(r["result"] == "matches-pin" for r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
