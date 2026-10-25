#!/usr/bin/env python3
"""Freeze catalogue seeds and explicit official chapter boundaries, not corpus completion."""
from __future__ import annotations

import argparse
from datetime import date
from pathlib import Path
import re

import pipeline

RESEARCH = pipeline.ROOT / "data/law/corpus/research"


def number_key(value):
    numbers = [int(n) for n in re.findall(r"[0-9]+", value)]
    return (numbers[0], 2 if "مكرر" in value else 1 if "-" in value else 0,
            numbers[1] if len(numbers) > 1 else 0, value)


def catalogue_seeds(folder: Path):
    seeds = {}
    pins = []

    def visit(value):
        if isinstance(value, dict):
            if "code" in value and "number" in value:
                seeds.setdefault(value["code"], set()).add(value["number"])
            for child in value.values():
                visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)

    for path in sorted(folder.glob("*.json")):
        visit(pipeline.read_json(path))
        pins.append({"file": path.name, "sha256": pipeline.hash_file(path)})
    return {key: sorted(values, key=number_key) for key, values in seeds.items()}, pins


def chapter(id, path, articles, source, pages, note=""):
    return {"id": id, "path": path, "articles": articles, "sourceId": source,
            "pdfPages": pages, "note": note}


def make_scope():
    seeds, pins = catalogue_seeds(pipeline.ROOT / "data/law/claims")
    full = [("civil-code", 1003), ("cpca", 1065), ("family-code", 224)]
    codes = [{"id": code, "coverage": "complete-code", "catalogueSeeds": seeds.get(code, []),
              "originalBaseArticles": [str(i) for i in range(1, last + 1)],
              "coveredChapters": [], "historicalVersions": "all",
              "inventoryFinal": False,
              "remaining": ["Reconcile every insertion, repeal, renumbering, historical heading and amendment before final count."]}
             for code, last in full]
    commercial = [chapter("contracts-general", ["Livre I", "Titre IV: Des contrats commerciaux", "Chapitre I: Dispositions générales"],
                          ["30"], "commercial-official-2007", [17]),
                  chapter("commercial-leases-entire-title", ["Livre II", "Titre II: Des baux commerciaux (all seven chapters)"],
                          sorted([str(i) for i in range(169, 203)] + ["187 مكرر", "187 مكرر 1"], key=number_key),
                          "commercial-official-2007", list(range(50, 64)),
                          "The user's explicit complete commercial-lease example covers the entire title, not only the chapters containing the catalogue seeds. Article203 begins the next title. French187ter is Arabic187bis1, not a second identity.")]
    labour = [chapter("individual-relations-chapter-vi", ["Titre III: Relations individuelles de travail", "Chapitre VI: Modification, cessation et suspension de la relation de travail"],
                      sorted([str(i) for i in range(62, 75)] + [f"73-{i}" for i in range(1, 7)], key=number_key),
                      "A1990017", [7, 8, 9], "Includes all three sections; inserted73-1..73-6 retained across their historical versions."),
              chapter("remuneration-general", ["Titre IV: Rémunération du travail", "Chapitre I: Dispositions générales"],
                      [str(i) for i in range(80, 87)], "A1990017", [9]),
              chapter("remuneration-guarantees", ["Titre IV: Rémunération du travail", "Chapitre III: Privilèges et garanties"],
                      ["88", "89", "90"], "A1990017", [9, 10])]
    disputes = [chapter("conciliation-competence", ["الباب الرابع: في الاختصاص", "الفصل الأول: اختصاص مكاتب المصالحة"],
                        ["19"], "A1990006", [18],
                        "Article19 is the entire chapter. The next chapter starts at20; procedural articles26 onward are a different title and are not silently added.")]
    for code, chapters, explicit in [("commercial-code", commercial, ["187 مكرر 1"]),
                                     ("labour-relations", labour, []), ("labour-disputes", disputes, [])]:
        numbers = sorted({n for c in chapters for n in c["articles"]}, key=number_key)
        missing = set(seeds.get(code, [])) | set(explicit)
        missing -= set(numbers)
        if missing:
            raise ValueError(f"Catalogue/explicit seeds outside declared chapters: {code}: {missing}")
        codes.append({"id": code, "coverage": "chapter-subset", "catalogueSeeds": seeds.get(code, []),
                      "explicitAdditionalSeeds": explicit, "coveredChapters": chapters,
                      "targetArticles": numbers, "targetCount": len(numbers), "historicalVersions": "all",
                      "inventoryFinal": False,
                      "remaining": ["Check all amending publications for additional identities and changed chapter boundaries within this scope."]})
    registry = pipeline.read_json(RESEARCH / "scope-source-downloads.json")
    return {"scopeVersion": 1, "recordedOn": date.today().isoformat(), "status": "boundaries-frozen; historical-inventory-in-progress",
            "authorization": "User scope change: complete Civil, CPCA including22-13, Family; Commercial/90-11/90-04 catalogue articles plus full chapters with all versions; no special laws.",
            "branch": "codex/law-corpus", "externalTeamDependency": False, "specialLaws": "excluded; do not start item7",
            "claimCatalogue": pins, "codes": codes,
            "originalBaseCount": sum(last for _, last in full),
            "boundedTargetCount": sum(c.get("targetCount", 0) for c in codes),
            "countNote": "Base numbering is not the final unique-identity or historical-version count. No completeness check is inferred from these arrays.",
            "sources": [{k: item[k] for k in ("id", "url", "sha256", "retrievedOn")} for item in registry if "sha256" in item]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    result = make_scope()
    target = RESEARCH / "scope-plan.json"
    if args.check:
        saved = pipeline.read_json(target)
        for key in ("claimCatalogue", "codes", "originalBaseCount", "boundedTargetCount"):
            if saved[key] != result[key]:
                raise ValueError("Frozen scope has drifted: " + key)
    else:
        pipeline.write_json(target, result)
    print(f"Original bases={result['originalBaseCount']}; bounded identities={result['boundedTargetCount']}; amendment inventory not final")


if __name__ == "__main__":
    main()
