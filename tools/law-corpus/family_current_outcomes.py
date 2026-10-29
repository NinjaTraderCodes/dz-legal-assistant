#!/usr/bin/env python3
"""Audit the reviewed Family current snapshot against the independently read 2005 instruction inventory.

This checks present outcomes, not a historical consolidation run, missing rectifications or commencement dates.
An unchanged-body edition conflict remains unsettled even when its identity and current operation are correct.
"""
from __future__ import annotations

from collections import Counter

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/family-2026-10-28"


def audit(articles: list[dict], expected: set[str], operations: dict, bodies: dict, paths: dict) -> dict:
    numbers = [a["number"] for a in articles]
    if len(numbers) != len(set(numbers)) or set(numbers) != expected:
        raise ValueError("Current Family identities differ from the independent inventory")
    if set(bodies) != expected - {n for n, op in operations.items() if op["operation"] == "repeal"}:
        raise ValueError("Reviewed Family bodies do not match the independently expected operative set")
    rows = []
    for article in articles:
        number = article["number"]
        op = operations.get(number)
        operation = op["operation"] if op else "unchanged-body"
        if len(article["versions"]) != 1:
            raise ValueError("Current audit expects one explicit snapshot, not invented historical intervals")
        version = article["versions"][0]
        expected_publication = "ordinance-05-02" if op else "law-84-11"
        if version["introducedBy"] != expected_publication or version["source"]["publicationId"] != expected_publication:
            raise ValueError("Current operation is attributed to the wrong publication: " + number)
        if version["path"] != paths[number] or article["path"] != paths[number]:
            raise ValueError("Current Family path differs from the actual heading reading: " + number)
        repeal = operation == "repeal"
        if (version["status"] == "repealed") != repeal:
            raise ValueError("Current repeal state differs from the JO instruction: " + number)
        expected_text = "" if repeal else bodies[number]["text"]
        expected_paragraphs = [] if repeal else bodies[number]["paragraphs"]
        if version["text"] != expected_text or version["paragraphs"] != expected_paragraphs:
            raise ValueError("Current Family outcome differs from its reviewed body: " + number)
        rows.append({"number": number, "operation": operation, "introducedBy": expected_publication,
                     "textSha256": pipeline.hash_text(version["text"]), "paragraphs": len(version["paragraphs"]),
                     "verificationStateRetained": version["verification"]["state"]})
    return {"method": __doc__.strip(), "identities": len(rows), "operations": dict(Counter(r["operation"] for r in rows)),
            "currentIdentityAndOutcomeAuditPassed": True, "historicalConsolidationComplete": False,
            "wholeCodeDeliveryComplete": False, "results": rows}


def run(articles: list[dict], inventory: dict, bodies: dict, paths: dict) -> dict:
    operations = {}
    for op in inventory["operations"]:
        for number in op.get("numbers", []):
            if number in operations or op["operation"] not in {"replace", "insert", "repeal"}:
                raise ValueError("Duplicate or unknown independently read Family operation")
            operations[number] = op
    counts = Counter(op["operation"] for op in operations.values())
    if dict(counts) != inventory["counts"] or counts != Counter({"replace": 29, "insert": 8, "repeal": 5}):
        raise ValueError("The independently read Family operation counts changed")
    for image in inventory["images"]:
        path = (pipeline.ROOT / image["cachePath"]).resolve()
        if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != image["imageSha256"]:
            raise ValueError("Independent Family instruction-image proof changed")
    expected = set(map(str, range(1, 225))) | {n for n, op in operations.items() if op["operation"] == "insert"}
    if len(expected) != 232:
        raise ValueError("Inserted identities collide with the base inventory")
    result = audit(articles, expected, operations, bodies, paths)
    result |= {"instructionInventorySha256": pipeline.hash_file(WORK / "amendment-inventory.json"),
               "pathDecisionsSha256": pipeline.hash_file(WORK / "path-decisions.json"),
               "verificationEffect": "Current identity/operation checks only. No wording conflict, missing historical body or commencement uncertainty is waived."}
    pipeline.write_json(WORK / "current-outcome-audit.json", result)
    print("Current Family inventory/outcomes:", result["operations"], "; whole-code delivery remains incomplete")
    return result
