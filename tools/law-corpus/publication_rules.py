#!/usr/bin/env python3
"""Civil article4's location-dependent publication rule; never a guessed nationwide commencement date."""
from __future__ import annotations

from datetime import date, timedelta

import pipeline


def after_complete_day(publication: str, *, location: str, local_receipt: str | None = None) -> str | None:
    published = date.fromisoformat(publication)
    if location == "algiers":
        if local_receipt is not None:
            raise ValueError("Algiers uses publication, not a supplied local receipt")
        anchor = published
    elif location == "other-daira":
        if local_receipt is None:
            return None
        anchor = date.fromisoformat(local_receipt)
        if anchor < published:
            raise ValueError("Local receipt cannot precede the publication")
    else:
        raise ValueError("Location must be explicit; no nationwide default")
    # The anchor day is excluded; the following complete calendar day must elapse.
    return (anchor + timedelta(days=2)).isoformat()


def replay_family_review(root=pipeline.ROOT, cache=pipeline.CACHE) -> dict:
    work = root / "data/law/corpus/research/family-2026-10-28"
    review = pipeline.read_json(work / "temporal-rule-review.json")
    expected = {"A1975078": "ba213b5beef85fb30959320b817594324e043842446c953cd86e22a074630ee9",
                "A1984024": "72a9290493fe6b94b7b4ab6547cfebafb6d4f85c7530a2c80a9fcad8ca378c9b",
                "A2005015": "8cd7385ea8c9a560849841728d91d7344f804ba5ea9e8c35f9e7f365af15a399",
                "A2005043": "ec671d5e6fd9eb0b8c463e28aa9853f0b83266948aa125ff80f453a5af204d2a"}
    sources = {s["id"]: s for s in review["sources"]}
    if len(sources) != len(review["sources"]) or set(sources) != set(expected):
        raise ValueError("Family commencement review source inventory changed")
    for ident, sha in expected.items():
        if sources[ident]["sha256"] != sha or pipeline.hash_file(cache / (ident + ".pdf")) != sha:
            raise ValueError("Family commencement source PDF changed")
    for item in review["imagesActuallyRead"]:
        path = (root / item["image"]).resolve()
        if (not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != item["imageSha256"]
                or item["inspection"] != "actually read; Codex AI, not human/lawyer verification"):
            raise ValueError("Family commencement source-image proof changed")
    if (len(review["imagesActuallyRead"]) != 3 or review["sourceReadRule"]["article"] != "civil-code:4"
            or review["sourceReadRule"]["pdfPage"] != 2 or review["historicalLookupPermitted"]
            or review["nationalDateInvented"]):
        raise ValueError("A publication rule cannot authorize historical lookup or a guessed nationwide date")
    expected_dates = {"law-84-11": "1984-06-12", "ordinance-05-02": "2005-02-27", "law-05-09": "2005-06-22"}
    rows = {r["publicationId"]: r for r in review["publicationRules"]}
    if set(rows) != set(expected_dates) or len(rows) != len(review["publicationRules"]):
        raise ValueError("Family publication-clause inventory changed")
    for ident, published in expected_dates.items():
        row = rows[ident]
        if (row["publishedOn"] != published or row["otherDairaCommencement"] is not None
                or row["algiersRuleCalendarCalculation"] != after_complete_day(published, location="algiers")):
            raise ValueError("Conditional publication calculation cannot become a nationwide bound")
    return review
