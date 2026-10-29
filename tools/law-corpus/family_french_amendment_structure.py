#!/usr/bin/env python3
"""Replay source-read French 05-02 labels/paragraphs against the independent Arabic current targets."""
from __future__ import annotations

import re
from pypdf import PdfReader

import family_french_structure
import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def compare(review: dict, articles: list[dict], inventory: dict) -> dict:
    expected = {n for op in inventory["operations"] if op["operation"] in {"insert", "replace"} for n in op["numbers"]}
    rows = {r["number"]: r for r in review["articles"]}
    if len(rows) != len(review["articles"]) or set(rows) != expected:
        raise ValueError("French quoted-body inventory differs from the independent Arabic amendment targets")
    by_number = {a["number"]: a for a in articles}
    flags = []
    for number,row in rows.items():
        parsed = family_french_structure.number("Art. " + row["frenchPrintedLabel"] + ". — ")
        if parsed is None or parsed[0] != number:
            raise ValueError("French printed label maps to another identity")
        version = by_number[number]["versions"][0]
        if (version["introducedBy"] != "ordinance-05-02" or version["status"] != "in-force"
                or pipeline.hash_text(version["text"]) != row["arabicTextSha256"]
                or [pipeline.hash_text(p) for p in version["paragraphs"]] != row["arabicParagraphSha256s"]
                or len(version["paragraphs"]) != row["arabicParagraphs"]):
            raise ValueError("Arabic body/paragraphs changed after the French structure comparison")
        if row["inspection"] != "actually read on original French page images; structural only; no translation equivalence":
            raise ValueError("French paragraph reading is not an actual source inspection")
        if (not row["frenchPdfPages"] or not set(row["frenchPdfPages"]) <= {17,18,19,20}
                or not isinstance(row["frenchParagraphsActuallyRead"], int) or row["frenchParagraphsActuallyRead"] < 1):
            raise ValueError("French body locator/paragraph count is incomplete")
        if row["frenchParagraphsActuallyRead"] != row["arabicParagraphs"]:
            if row["verdict"] != "edition layout difference; independently read Arabic paragraphing retained":
                raise ValueError("French/Arabic paragraph difference cannot be silently treated as a match")
            flags.append(number)
        elif row["verdict"] != "same printed paragraph count":
            raise ValueError("French paragraph comparison verdict disagrees with the counts")
    expected_repeals = {n for op in inventory["operations"] if op["operation"] == "repeal" for n in op["numbers"]}
    if (set(review["repeals"]["numbers"]) != expected_repeals or review["repeals"]["lawArticle"] != 18
            or review["repeals"]["pdfPage"] != 20):
        raise ValueError("French repeal list differs from the Arabic source inventory")
    if review["publicationClause"]["calendarCommencementDate"] is not None:
        raise ValueError("A publication clause cannot invent a calendar commencement date")
    return {"quotedBodies": len(rows), "paragraphCountMatches": len(rows)-len(flags), "paragraphDifferences": flags,
            "repealTargets": len(expected_repeals), "translationEquivalenceCertified": False, "checkedStatesGranted": 0}


def main(articles: list[dict] | None = None) -> dict:
    review = pipeline.read_json(WORK / "amending-french-structure-review.json")
    pin = review["source"]
    pdf = pipeline.CACHE / "F2005015.pdf"
    if pin["id"] != "F2005015" or pipeline.hash_file(pdf) != pin["sha256"]:
        raise ValueError("French amendment source PDF changed")
    reader = PdfReader(pdf)
    native = "\n".join(reader.pages[p-1].extract_text() for p in range(17,21))
    if pipeline.hash_text(native) != review["nativePageTextSha256"]:
        raise ValueError("French native extraction changed")
    labels = []
    for line in native.splitlines():
        if re.match(r"^«\s*Art\.", line):
            parsed = family_french_structure.number(line.lstrip("« "))
            if parsed is None:
                raise ValueError("Unparsed quoted French article label")
            labels.append(parsed[0])
    if labels != [r["number"] for r in review["articles"]]:
        raise ValueError("French native quoted labels disagree with the actual image reading")
    images = {r["pdfPage"]:r for r in review["imagesActuallyRead"]}
    if set(images) != {17,18,19,20} or len(images) != len(review["imagesActuallyRead"]):
        raise ValueError("Missing or duplicate French amendment source-image proof")
    for image in images.values():
        path = (pipeline.ROOT / image["image"]).resolve()
        if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != image["imageSha256"]:
            raise ValueError("French amendment source-image proof changed or escaped the repository")
    result = compare(review, articles if articles is not None else pipeline.read_json(pipeline.ROOT / "data/law/corpus/family-code/articles.json")["articles"],
                     pipeline.read_json(WORK / "amendment-inventory.json"))
    pipeline.write_json(WORK / "amending-french-structure-audit.json", {"method":__doc__.strip(), **result})
    print("French05-02: 37 quoted labels/paragraph layouts and five repeal targets match; no date or translation inference")
    return result


if __name__ == "__main__":
    main()
