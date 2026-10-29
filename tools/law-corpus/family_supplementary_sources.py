#!/usr/bin/env python3
"""Replay actually read Family approval/translation-rectification sources without inferring Arabic body changes."""
from __future__ import annotations

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def validate(review: dict, root=pipeline.ROOT, cache=pipeline.CACHE) -> dict:
    sources = {s["id"]: s for s in review["sources"]}
    if len(sources) != len(review["sources"]) or set(sources) != {"A2005043", "F2005043", "F1984031", "F1984041"}:
        raise ValueError("Supplementary Family source inventory is incomplete or duplicated")
    for source in sources.values():
        path = (cache / source["cacheFile"]).resolve()
        if not path.is_relative_to(cache.resolve()) or pipeline.hash_file(path) != source["sha256"]:
            raise ValueError("Supplementary Family PDF changed or escaped the source cache")
        if source["inspection"] != "actually read; not human/lawyer verification":
            raise ValueError("Supplementary source lacks an explicit actual reading")
    images = {i["image"]: i for i in review["imagesActuallyRead"]}
    if len(images) != len(review["imagesActuallyRead"]) or not {s["image"] for s in sources.values()} <= set(images):
        raise ValueError("Supplementary source-page reading proof is incomplete")
    for record in images.values():
        path = (root / record["image"]).resolve()
        if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != record["imageSha256"]:
            raise ValueError("Supplementary Family image proof changed or escaped the repository")
    for source in sources.values():
        if images[source["image"]]["imageSha256"] != source["imageSha256"]:
            raise ValueError("Source locator and actually read image disagree")
    approval = review["approval"]
    if (approval["arabicSourceId"] != "A2005043" or approval["frenchSourceId"] != "F2005043"
            or approval["arabicPdfPage"] != 4 or approval["frenchPdfPage"] != 3
            or sources["A2005043"]["inspectedPdfPage"] != 4 or sources["F2005043"]["inspectedPdfPage"] != 3):
        raise ValueError("Approval locator must identify Arabic law page4, not the continued contents on page3")
    if (approval["publicationId"] != "law-05-09" or approval["number"] != "05-09"
            or approval["date"] != "2005-05-04" or approval["joIssue"] != "43" or approval["joDate"] != "2005-06-22"
            or approval["approvesPublicationId"] != "ordinance-05-02" or approval["articleBodyEffect"] != "approval-only"):
        raise ValueError("Source-read approval identity/effect changed")
    if review["arabicConflictEffect"]["resolved"] is not False:
        raise ValueError("French rectifications cannot silently resolve the quarantined Arabic wording conflicts")
    return sources


def approval_publication(review: dict, sources: dict) -> dict:
    approval = review["approval"]
    download = lambda source: {k: source[k] for k in ("url", "sha256", "retrievedOn")}
    return {"id": approval["publicationId"], "kind": "law", "number": approval["number"], "date": approval["date"],
            "jo": {"issue": approval["joIssue"], "date": approval["joDate"], "pages": [approval["arabicPdfPage"]]},
            "arabic": download(sources[approval["arabicSourceId"]]), "french": download(sources[approval["frenchSourceId"]]),
            "articlesTouched": [], "commencementDate": None,
            "commencementBasis": "Article2 orders publication and supplies no calendar commencement date. Approval of05-02 is not a new Family body or a guessed commencement boundary.",
            "transitionalProvisions": [], "discovery": [{"url": sources["A2005043"]["url"], "accessedOn": review["reviewedOn"],
                "note": "Actual Arabic approval read on PDF4 (left column), French counterpart on PDF3 (right column). Article1 approves05-02, article2 orders publication; no code-body change. Both raw source pins and image readings replay."}]}


def main() -> dict:
    review = pipeline.read_json(WORK / "supplementary-source-review.json")
    sources = validate(review)
    print("Family approval and both French rectification source pins/readings replayed; no Arabic conflict waived")
    return approval_publication(review, sources)


if __name__ == "__main__":
    main()
