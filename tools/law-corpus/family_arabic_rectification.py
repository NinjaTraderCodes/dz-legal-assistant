#!/usr/bin/env python3
"""Replay the Arabic JO31/1984 rectification independently of the French translation notices.

Original collation decisions remain immutable. Only an explicitly source-read correction can resolve
their conflict. This operation changes no historical intervals and does not certify local commencement.
"""
from __future__ import annotations

import copy

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"
SOURCE_SHA = "e925d9cb539288b7bd57b908976810944adb952ac0c6852622d7df6cfb67966a"
PAGES = {**dict.fromkeys(["19", "30", "36", "107", "111", "135"], 12),
         **dict.fromkeys(["144", "151", "153", "165", "167", "168", "185", "202", "203", "207", "211"], 13)}
CONFLICTS = {"135", "151", "202", "203"}
SUPERSEDED = {"19", "30", "36"}
ORDER_OLD = "لم تستغرق فروض التركة"
ORDER_NEW = "لم تستغرق الفروض التركة"


def apply(bodies: dict, review: dict, conflicts: dict, inventory: dict) -> dict:
    rows = {r["number"]: r for r in review["currentOutcomes"]}
    disputed = {r["number"]: r for r in conflicts["conflicts"]}
    if len(rows) != len(review["currentOutcomes"]) or set(rows) != set(PAGES):
        raise ValueError("Arabic rectification needs all17 distinct independently read targets")
    if set(review["resolvedConflictArticles"]) != CONFLICTS or set(disputed) != CONFLICTS:
        raise ValueError("Arabic conflict partition changed")
    replacements = {n for op in inventory["operations"] if op["operation"] == "replace" for n in op["numbers"]}
    final = copy.deepcopy(bodies)
    for number, row in rows.items():
        if row["rectificationPdfPage"] != PAGES[number] or not row["note"].strip():
            raise ValueError("Arabic rectification locator or source reading changed")
        body = final[number]
        if (pipeline.hash_text(body["text"]) != row["currentTextSha256"]
                or body["text"] != "\n".join(body["paragraphs"])):
            raise ValueError("Arabic rectification belongs to a different current body")
        superseded = number in SUPERSEDED
        expected_outcome = ("superseded-by-05-02" if superseded else "source-order-correction-required" if number == "167"
                            else "already-reflected-in-current-body")
        if (number in replacements) != superseded or row["outcome"] != expected_outcome:
            raise ValueError("Historical rectification cannot overwrite a later replacement")
        if superseded:
            if row["clauses"]:
                raise ValueError("Deferred historical clause cannot be applied to current replacement")
            continue
        if not row["clauses"]:
            raise ValueError("Current rectification lacks its exact source-read clause")
        for clause in row["clauses"]:
            quote = clause["currentQuote"]
            if not quote or body["text"].count(quote) != clause["expectedOccurrences"]:
                raise ValueError("Corrected clause is absent or ambiguous in the bound current body")
            if not clause["oldPrinted"] or not clause["correctedPrinted"]:
                raise ValueError("Rectification must retain both actual printed readings")
        prior = copy.deepcopy(body["joCheck"])
        if number in CONFLICTS:
            conflict = disputed[number]
            if (prior.get("conflict") != conflict or body["text"].count(conflict["ministryQuote"]) != 1
                    or not any(conflict["ministryQuote"] in c["currentQuote"] for c in row["clauses"])):
                raise ValueError("Correction does not resolve the exact quarantined source conflict: " + number)
        if number == "167":
            # Retain the rectification's printed order even though a later compilation reorders the nouns.
            if (body["text"].count(ORDER_OLD) != 1 or ORDER_NEW in body["text"]
                    or row["clauses"][0]["correctedPrinted"] != ORDER_NEW):
                raise ValueError("Article167 source-order correction is not uniquely guarded")
            body["paragraphs"] = [p.replace(ORDER_OLD, ORDER_NEW) for p in body["paragraphs"]]
            body["text"] = "\n".join(body["paragraphs"])
        hashes = [i["imageSha256"] for i in review["imagesActuallyRead"]
                  if i["image"].endswith(f"A1984031-p{PAGES[number]}.png")]
        if len(hashes) != 1:
            raise ValueError("Rectification lacks the actual Arabic page reading")
        body["joCheck"] |= {"result": "original-JO-with-Arabic-rectification", "conflict": None,
            "priorCollation": prior, "textSha256": pipeline.hash_text(body["text"]),
            "imageSha256s": list(dict.fromkeys(prior["imageSha256s"] + hashes)),
            "note": "Original Arabic JO31/1984 rectification, PDF" + str(PAGES[number]) + ": " + row["note"],
            "rectificationPublicationId": "rectification-ar-1984-31", "rectificationPdfPage": PAGES[number],
            "rectificationSourceSha256": SOURCE_SHA}
    return final


def replay(review: dict, root=pipeline.ROOT, cache=pipeline.CACHE) -> None:
    source = review["source"]
    if (source["id"] != "A1984031" or source["sha256"] != SOURCE_SHA
            or source["url"] != "https://joradp.dz/FTP/jo-arabe/1984/A1984031.pdf"):
        raise ValueError("Arabic conflict resolution requires the Arabic rectification, not a French notice")
    path = (root / source["cachePath"]).resolve()
    if not path.is_relative_to(cache.resolve()) or pipeline.hash_file(path) != SOURCE_SHA:
        raise ValueError("Arabic rectification PDF changed or escaped the cache")
    for item in review["imagesActuallyRead"]:
        path = (root / item["image"]).resolve()
        if (not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != item["imageSha256"]
                or item["inspection"] != "actually read; Codex AI, not human/lawyer verification"):
            raise ValueError("Arabic rectification image reading is missing or stale")
    for key, name in [("originalConflictsSha256", "jo-collation-conflicts.json"),
                      ("amendmentInventorySha256", "amendment-inventory.json")]:
        if review[key] != pipeline.hash_file(root / "data/law/corpus/research/family-2026-10-28" / name):
            raise ValueError("Arabic rectification prior conflict/instruction evidence changed")


def publication(review: dict) -> dict:
    source = review["source"]
    return {"id": "rectification-ar-1984-31", "kind": "rectification", "number": "84-11",
        "date": "1984-07-31", "jo": {"issue": "31", "date": "1984-07-31", "pages": [12, 13]},
        "arabic": {k: source[k] for k in ("url", "sha256", "retrievedOn")}, "french": None,
        "articlesTouched": list(PAGES), "commencementDate": None,
        "commencementBasis": "Dated by publication of the Arabic rectification notice. No independent enactment date or retrospective/commencement boundary is inferred.",
        "transitionalProvisions": [], "discovery": [{"url": source["url"], "accessedOn": review["reviewedOn"],
            "note": "Arabic issue cover explicitly lists the Family rectification at printed1184; actually read PDF12-13 (printed1184-1185), including all17 target articles. Separate from the French notices."}]}


def main(bodies: dict) -> tuple[dict, dict]:
    review = pipeline.read_json(WORK / "arabic-rectification-review.json")
    replay(review)
    result = apply(bodies, review, pipeline.read_json(WORK / "jo-collation-conflicts.json"),
                   pipeline.read_json(WORK / "amendment-inventory.json"))
    pipeline.write_json(WORK / "arabic-rectification-audit.json", {
        "reviewSha256": pipeline.hash_file(WORK / "arabic-rectification-review.json"),
        "currentTargets": len(PAGES) - len(SUPERSEDED), "supersededTargets": sorted(SUPERSEDED),
        "wordingConflictsResolved": sorted(CONFLICTS), "bodyTextChanges": ["167"],
        "temporalBoundsChanged": 0, "method": __doc__.strip(),
        "results": [{"number": n, "beforeSha256": pipeline.hash_text(bodies[n]["text"]),
                     "afterSha256": pipeline.hash_text(result[n]["text"])} for n in PAGES]})
    print("Arabic rectification replay: four conflicts resolved;14 current targets; three superseded;167 printed order restored")
    return result, publication(review)
