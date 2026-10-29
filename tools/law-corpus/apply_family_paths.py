#!/usr/bin/env python3
"""Replay actual heading-only readings without regenerating reviewed Family article bodies."""
from __future__ import annotations

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def apply(native: dict, decisions: dict, root=pipeline.ROOT) -> dict[str, list[str]]:
    if decisions["sourceSha256"] != native["source"]["sha256"]:
        raise ValueError("Path review belongs to another Ministry source")
    expected = {a["number"]: a for a in native["articles"]}
    if len(expected) != len(native["articles"]):
        raise ValueError("Duplicate Family identity")
    paths = {}
    for decision in decisions["decisions"]:
        if not decision["note"].strip() or not decision["path"] or not decision["images"]:
            raise ValueError("Path reading lacks a verdict, path or proof")
        if len(decision["images"]) != len(decision["imageSha256s"]):
            raise ValueError("Path image hashes are incomplete")
        for image, sha in zip(decision["images"], decision["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Path proof changed or escaped the repository")
        for number in decision["numbers"]:
            if number in paths or number not in expected or expected[number]["path"] != decision["previousPath"]:
                raise ValueError("Duplicate, absent or stale Family path")
            paths[number] = decision["path"]
    if set(paths) != set(expected):
        raise ValueError("Path readings do not cover every Family identity")
    return paths


def main() -> dict[str, list[str]]:
    native_path = WORK / "native.json"
    decisions = pipeline.read_json(WORK / "path-decisions.json")
    if decisions["nativeSha256"] != pipeline.hash_file(native_path):
        raise ValueError("Path preparation changed since source reading")
    native = pipeline.read_json(native_path)
    paths = apply(native, decisions)
    changed = [a["number"] for a in native["articles"] if paths[a["number"]] != a["path"]]
    pipeline.write_json(WORK / "path-review-summary.json", {
        "nativeSha256": pipeline.hash_file(native_path), "decisionsSha256": pipeline.hash_file(WORK / "path-decisions.json"),
        "groupsRead": len(decisions["decisions"]), "identitiesCovered": len(paths), "changedPaths": changed,
        "method": __doc__.strip(), "verificationEffect": "heading paths only; no article word/date/amendment certification"})
    print(len(paths), "Family paths accounted for;", len(changed), "metadata paths corrected")
    return paths


if __name__ == "__main__":
    main()
