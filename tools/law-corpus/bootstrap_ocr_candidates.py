#!/usr/bin/env python3
"""Fetch optional build-only OCR weights from committed upstream URL/SHA pins."""
import argparse
import urllib.request
from urllib.parse import urlsplit
import pipeline


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--offline", action="store_true")
    args = parser.parse_args()
    for filename, folder, name_key in [
        ("ocr-best-model-pins.json", pipeline.TOOLS / "ocr-best", "language"),
        ("ocr-paddle-model-pins.json", pipeline.TOOLS / "ocr-comparison/models", "file")]:
        pins = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research" / filename)
        for pin in pins["models"]:
            name = pin[name_key] + (".traineddata" if name_key == "language" else "")
            if pipeline.Path(name).name != name:
                raise ValueError("Unsafe model filename")
            destination = folder / name
            if destination.exists():
                if pipeline.hash_file(destination) != pin["sha256"]:
                    raise ValueError("Existing optional model differs from pin: " + name)
                print(name, "hash replay passed")
                continue
            if args.offline:
                raise ValueError("Missing offline model: " + name)
            url = urlsplit(pin["url"])
            if url.scheme != "https" or url.hostname not in ("raw.githubusercontent.com", "www.modelscope.cn"):
                raise ValueError("Unapproved software-model origin")
            folder.mkdir(parents=True, exist_ok=True)
            temporary = destination.with_suffix(destination.suffix + ".partial")
            try:
                with urllib.request.urlopen(pin["url"], timeout=60) as response, temporary.open("wb") as output:
                    total = 0
                    while chunk := response.read(1024 * 1024):
                        total += len(chunk)
                        if total > pin["bytes"]:
                            raise ValueError("Optional model exceeded pinned size")
                        output.write(chunk)
                if total != pin["bytes"] or pipeline.hash_file(temporary) != pin["sha256"]:
                    raise ValueError("Downloaded optional model differs from pin: " + name)
                temporary.replace(destination)
            finally:
                temporary.unlink(missing_ok=True)
            print(name, "download and hash replay passed")


if __name__ == "__main__":
    main()
