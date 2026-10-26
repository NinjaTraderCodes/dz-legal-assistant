#!/usr/bin/env python3
"""Merge explicit reviewed/candidate records without dropping history or replacing an old text."""
from copy import deepcopy
import argparse
import pipeline
from scope_inventory import number_key


def merge(spec, ledger, batch):
    result, updated = deepcopy(spec), deepcopy(ledger)
    result.pop("inventoryReview", None)
    updated["scope"]["complete"] = False
    publications = {p["id"]: p for p in updated["publications"]}
    articles = {a["id"]: a for a in result["articles"]}
    for incoming in batch["articles"]:
        if not incoming["id"].startswith(updated["id"] + ":"):
            raise ValueError("Batch article belongs to a different code")
        current = articles.get(incoming["id"])
        if current is None:
            current = deepcopy(incoming)
            current["versions"] = []
            articles[current["id"]] = current
            result["articles"].append(current)
        elif (current["canonicalNumber"], current["number"]) != (incoming["canonicalNumber"], incoming["number"]):
            raise ValueError("Existing identity/printed number cannot be silently changed")
        versions = {v["versionId"]: v for v in current["versions"]}
        for version in incoming["versions"]:
            if version["text"] != "\n".join(version["paragraphs"]):
                raise ValueError("Incoming paragraphs do not reconstruct their declared text")
            if version["introducedBy"] not in publications:
                raise ValueError("Incoming version publication is absent from ledger")
            existing = versions.get(version["versionId"])
            if existing and (existing["text"], existing["paragraphs"], existing["introducedBy"]) != (version["text"], version["paragraphs"], version["introducedBy"]):
                raise ValueError("Refusing to overwrite an existing historical text; use an explicit correction decision")
            if existing:
                current["versions"][current["versions"].index(existing)] = deepcopy(version)
            else:
                current["versions"].append(deepcopy(version))
            publication = publications[version["introducedBy"]]
            publication["articlesTouched"] = sorted(set(publication["articlesTouched"]) | {incoming["canonicalNumber"]}, key=number_key)
        current["versions"].sort(key=lambda v: (publications[v["introducedBy"]]["jo"]["date"], v["versionId"]))
    updated["scope"]["targetArticles"] = sorted(set(updated["scope"]["targetArticles"]) | {a["canonicalNumber"] for a in batch["articles"]}, key=number_key)
    result["articles"].sort(key=lambda a: number_key(a["canonicalNumber"]))
    result["generatedOn"] = batch["generatedOn"]
    return result, updated


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("spec", type=pipeline.Path)
    parser.add_argument("ledger", type=pipeline.Path)
    parser.add_argument("batch", type=pipeline.Path)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    args = parser.parse_args()
    spec, ledger = merge(pipeline.read_json(args.spec), pipeline.read_json(args.ledger), pipeline.read_json(args.batch))
    # Composition validates all actual image/text/PDF bindings before publishing
    # the three data files. No review is synthesized by this merger.
    pipeline.compose(spec, ledger, pipeline.CACHE, args.out)
    pipeline.write_json(args.spec, spec)
    print(f"Merged explicit batch; {len(spec['articles'])} article identities in this bounded delivery")


if __name__ == "__main__":
    main()
