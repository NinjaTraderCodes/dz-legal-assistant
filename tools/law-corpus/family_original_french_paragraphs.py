#!/usr/bin/env python3
"""Replay all current unchanged-body French JO paragraph readings; preserve every Arabic edition layout difference."""
from __future__ import annotations

import family_original_french_labels as labels
import pipeline

WORK = labels.WORK


def compare(review: dict, label_review: dict, articles: list[dict], inventory: dict) -> dict:
    changed = {n for op in inventory["operations"] for n in op.get("numbers", [])}
    expected = {a["number"] for a in articles if a["number"] not in changed}
    rows = {r["number"]:r for r in review["articles"]}
    if set(rows) != expected or len(rows) != len(review["articles"]):
        raise ValueError("Original French paragraph review has missing, duplicate or unexpected current bodies")
    by_number = {a["number"]:a for a in articles}
    flags = []
    for number,row in rows.items():
        version = by_number[number]["versions"][0]
        if (version["introducedBy"] != "law-84-11" or version["status"] != "in-force"
                or pipeline.hash_text(version["text"]) != row["arabicTextSha256"]
                or [pipeline.hash_text(p) for p in version["paragraphs"]] != row["arabicParagraphSha256s"]
                or len(version["paragraphs"]) != row["arabicParagraphs"]):
            raise ValueError("Arabic body/paragraphs changed after the original French reading")
        printed = [r for r in label_review["columnsActuallyRead"] if number in r["labels"] or r["continuationOf"] == number]
        if (not printed or row["frenchPdfPages"] != list(dict.fromkeys(r["pdfPage"] for r in printed))
                or row["frenchSourceColumns"] != [r["evidenceId"] for r in printed]):
            raise ValueError("French paragraph reading omits or changes a source/page continuation")
        if row["inspection"] != "actually read all relevant French paragraph starts/continuations on source-page images; structural only; not full-word/translation certification":
            raise ValueError("Original French paragraph comparison lacks an actual image reading")
        count = row["frenchParagraphsActuallyRead"]
        if type(count) is not int or count < 1:
            raise ValueError("French paragraph count must be an explicit positive integer")
        same = count == row["arabicParagraphs"]
        verdict = "same printed paragraph count" if same else "edition layout difference; independently source-read Arabic paragraphing retained"
        if row["verdict"] != verdict:
            raise ValueError("French paragraph difference cannot be silently called a match")
        if not same:
            flags.append({"number":number,"arabicParagraphs":row["arabicParagraphs"],"frenchParagraphs":count,
                          "action":"Retain independently source-read Arabic paragraphs; no translation or Arabic wording conflict resolution"})
    return {"unchangedBodiesCompared":len(rows),"paragraphCountMatches":len(rows)-len(flags),"paragraphDifferences":flags,
            "arabicBodiesOrParagraphsChanged":0,"arabicConflictsResolved":0,"checkedStatesGranted":0}


def main(articles: list[dict] | None = None) -> dict:
    review = pipeline.read_json(WORK / "original-french-paragraph-review.json")
    label_path = WORK / "original-french-label-review.json"
    label_review = pipeline.read_json(label_path)
    labels.replay(label_review)
    if (pipeline.hash_file(label_path) != review["labelReviewSha256"] or review["sourceSha256"] != label_review["sourceSha256"]
            or review["imagesActuallyRead"] != label_review["imagesActuallyRead"]):
        raise ValueError("Original French paragraph source/image bindings changed")
    result = compare(review, label_review,
                     articles if articles is not None else pipeline.read_json(pipeline.ROOT / "data/law/corpus/family-code/articles.json")["articles"],
                     pipeline.read_json(WORK / "amendment-inventory.json"))
    pipeline.write_json(WORK / "original-french-paragraph-audit.json", {"method":__doc__.strip(), **result})
    print("190 original French paragraph readings replayed;",len(result["paragraphDifferences"]),"edition layout flags; Arabic retained")
    return result


if __name__ == "__main__":
    main()
