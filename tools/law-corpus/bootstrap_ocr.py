#!/usr/bin/env python3
"""Replay the frozen conda-forge Windows OCR runtime without a package solver.

Downloads are SHA-256 pinned. Only DLLs, tesseract.exe, Arabic/French models,
output configs, package recipes and license files are extracted. No installer,
registry change, shell activation, or production dependency is introduced.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import tarfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CACHE = ROOT / "tools/law-corpus/.cache"


def sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def selected(name: str) -> bool:
    path = PurePosixPath(name)
    return ((name.startswith("Library/bin/") and path.suffix.lower() == ".dll")
            or name == "Library/bin/tesseract.exe"
            or (path.suffix == ".traineddata" and path.stem in ("ara", "fra"))
            or "/tessdata/configs/" in name or "/tessdata/tessconfigs/" in name
            or name.startswith("info/licenses/") or name.startswith("info/recipe/"))


def extract_tar(stream, prefix: Path, package: str) -> None:
    with tarfile.open(fileobj=stream, mode="r|") as bundle:
        for member in bundle:
            if not member.isfile() or not selected(member.name):
                continue
            relative = PurePosixPath(member.name)
            if relative.is_absolute() or ".." in relative.parts:
                raise ValueError("Unsafe archive path")
            target = prefix.joinpath(*relative.parts)
            if member.name.startswith("info/"):
                target = prefix / "package-notices" / package / Path(*relative.parts[1:])
            target.parent.mkdir(parents=True, exist_ok=True)
            source = bundle.extractfile(member)
            if source is None:
                raise ValueError("Missing archive stream")
            with target.open("wb") as output:
                shutil.copyfileobj(source, output, length=1024 * 1024)


def main() -> int:
    import zstandard

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache", type=Path, default=DEFAULT_CACHE)
    parser.add_argument("--lock", type=Path, default=Path(__file__).with_name("toolchain-windows-lock.json"))
    parser.add_argument("--reuse-packages", type=Path)
    parser.add_argument("--offline", action="store_true")
    parser.add_argument("--keep-archives", action="store_true")
    parser.add_argument("--only-package", action="append", help="Repair an interrupted replay; full lock still recorded")
    args = parser.parse_args()
    cache = args.cache.resolve()
    downloads, prefix = cache / "tool-downloads", cache / "ocr"
    downloads.mkdir(parents=True, exist_ok=True)
    lock = json.loads(args.lock.read_text(encoding="utf-8-sig"))
    if lock["platform"] != "win-64" or os.name != "nt":
        parser.error("This lock is for Windows x86-64; elsewhere use installed open-source Tesseract")
    for package in lock["packages"]:
        if args.only_package and package["name"] not in args.only_package:
            continue
        url = package["url"]
        if not url.startswith(("https://conda.anaconda.org/conda-forge/win-64/",
                               "https://conda.anaconda.org/conda-forge/noarch/")):
            raise ValueError("Tool archive URL is not frozen conda-forge HTTPS")
        name = url.rsplit("/", 1)[1]
        archive = downloads / name
        reused = args.reuse_packages / name if args.reuse_packages else None
        if reused and reused.exists():
            archive = reused
        if not archive.exists():
            if args.offline:
                raise FileNotFoundError(name)
            temporary = archive.with_suffix(archive.suffix + ".partial")
            with urllib.request.urlopen(url, timeout=180) as response, temporary.open("wb") as output:
                shutil.copyfileobj(response, output, length=1024 * 1024)
            temporary.replace(archive)
        if sha256(archive) != package["sha256"]:
            raise ValueError(f"Tool archive hash mismatch: {name}")
        with zipfile.ZipFile(archive) as conda:
            for member in conda.namelist():
                if member.endswith(".tar.zst"):
                    with conda.open(member) as compressed, zstandard.ZstdDecompressor().stream_reader(compressed) as stream:
                        extract_tar(stream, prefix, package["name"])
        print(f"Extracted pinned {package['name']} {package['version']}", flush=True)
        if not args.keep_archives and archive.parent == downloads:
            archive.unlink()
    tool = prefix / "Library/bin/tesseract.exe"
    env = os.environ.copy()
    env["PATH"] = str(tool.parent) + os.pathsep + env.get("PATH", "")
    version = subprocess.run([str(tool), "--version"], env=env, check=True, capture_output=True,
                             text=True, creationflags=subprocess.CREATE_NO_WINDOW).stdout.strip()
    models = [{"file": str(path.relative_to(prefix)).replace("\\", "/"), "sha256": sha256(path)}
              for path in sorted(prefix.rglob("*.traineddata")) if path.stem in ("ara", "fra")]
    record = {"lockSha256": sha256(args.lock), "tesseractVersionOutput": version,
              "tesseractExecutableSha256": sha256(tool), "models": models, "packages": lock["packages"],
              "licenseFiles": [{"path": str(path.relative_to(prefix)).replace("\\", "/"), "sha256": sha256(path)}
                               for path in sorted((prefix / "package-notices").rglob("*")) if path.is_file()]}
    (cache / "toolchain.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(tool)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
