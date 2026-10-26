#!/usr/bin/env python3
"""Visual spot-check page for a delivery: each selected version's text beside its official page crop.

Usage: make_spotcheck.py NAME CODE[:VERSION_ID,...] [--random N --seed S]
Writes docs/law-corpus-checks/NAME/README.md and the crops it shows (PNG copies of the hashed evidence crops).
With --random, N versions are drawn from the code's in-force current versions with a fixed seed, so the sample is
reproducible. The page records who inspected each crop; the generator never claims an inspection by itself.
"""
from __future__ import annotations

import argparse
import random
import shutil

import pipeline

DOCS = pipeline.ROOT / "docs/law-corpus-checks"


def crop_for(version: dict):
    evidence_id = version["source"].get("evidenceId")
    ids = [f["evidenceId"] for f in version["source"].get("fragments", []) if f.get("evidenceId")] or ([evidence_id] if evidence_id else [])
    out = []
    for i in dict.fromkeys(ids):
        for part in i.split("+"):
            found = list((pipeline.CACHE / "extractions").glob(f"*/page-*/{part}/crop.png"))
            if found:
                out.append((part, found[0]))
    return out


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("name")
    parser.add_argument("selection", nargs="+", help="CODE or CODE:VERSION_ID,VERSION_ID")
    parser.add_argument("--random", type=int, default=0)
    parser.add_argument("--seed", type=int, default=20261026)
    parser.add_argument("--inspected-by", default="Claude (AI), visual reading; not a lawyer or human verification")
    args = parser.parse_args()
    folder = DOCS / args.name
    folder.mkdir(parents=True, exist_ok=True)
    rows = []
    for selection in args.selection:
        code, _, ids = selection.partition(":")
        content = pipeline.read_json(pipeline.ROOT / "data/law/corpus" / code / "articles.json")
        versions = [(a, v) for a in content["articles"] for v in a["versions"]]
        if ids:
            wanted = ids.split(",")
            chosen = [(a, v) for a, v in versions if v["versionId"] in wanted]
        else:
            current = [(a, v) for a, v in versions if v["status"] == "in-force" and v["inForceTo"] is None]
            chosen = random.Random(args.seed).sample(current, min(args.random or 20, len(current)))
        rows += [(code, a, v) for a, v in chosen]
    lines = [f"# Visual spot-check: {args.name}", "",
             f"Each article's canonical text beside the official page crop it was checked against. Inspected by: {args.inspected_by}.",
             "Crops are copies of hashed evidence images (`tools/law-corpus/.cache/evidence/extractions`); hashes in each row.", ""]
    for code, article, version in rows:
        lines += [f"## {code} {article['number']} — `{version['versionId']}` — {version['verification']['state']}", ""]
        for evidence_id, path in crop_for(version):
            target = folder / f"{evidence_id}.png"
            shutil.copyfile(path, target)
            lines += [f"![{evidence_id}]({target.name})", "", f"Crop `{evidence_id}`, SHA-256 `{pipeline.hash_file(path)}`", ""]
        lines += ["Canonical text:", "", "> " + "\n>\n> ".join(version["paragraphs"]), ""]
        image_checks = [c for c in version["verification"]["checks"] if c["check"] in ("discrepancy-resolved-from-image", "text-layer-matches-ocr")]
        if image_checks:
            lines += ["Decision: " + image_checks[-1].get("note", ""), ""]
    (folder / "README.md").write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(folder, len(rows))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
