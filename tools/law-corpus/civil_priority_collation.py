#!/usr/bin/env python3
"""Replay final Civil source readings and audit current outcomes, without inventing historical versions.

All34 disputed marks are visibly retained. The two loan clauses have direct JO body readings;
489 keeps its original second paragraph rather than treating the2007 placeholder as article text.
"""
from __future__ import annotations

import copy
from collections import Counter

import apply_civil_amended_bodies as wording
import civil_priority_amendments as amendments
import pipeline

WORK = wording.WORK


def check_proofs(reading: dict, source: str, pages: set[int], pins: dict,
                 root=pipeline.ROOT) -> None:
    if reading["inspection"] != wording.INSPECTION or not reading["note"].strip() or not reading["proofs"]:
        raise ValueError("Final collation requires an actual documented source reading")
    for proof in reading["proofs"]:
        path = (root / proof["image"]).resolve()
        if (proof["sourceId"] != source or proof["sourceSha256"] != pins[source]
                or proof["pdfPage"] not in pages or not path.is_relative_to(root.resolve())
                or pipeline.hash_file(path) != proof["imageSha256"]):
            raise ValueError("Final collation source image changed or belongs to another publication/page")


def replay(review: dict, bodies: dict, layout: dict, queue: list[dict], operations: list[dict],
           pins: dict, original489: str, root=pipeline.ROOT) -> dict:
    expected = {(r["sourceId"], r["number"], r["flagIndex"]): r["flag"] for r in queue}
    actual = {(r["sourceId"], r["number"], r["flagIndex"]): r for r in review["punctuationDecisions"]}
    if len(actual) != len(review["punctuationDecisions"]) or set(actual) != set(expected):
        raise ValueError("Every punctuation flag needs exactly one explicit source decision")
    targets = amendments.target_map(operations, set(bodies))
    for key, reading in actual.items():
        number = key[1]
        if (reading["flag"] != expected[key] or not reading["flag"]["punctuationOnly"]
                or reading["flag"]["canonical"] not in {".", ":"}
                or reading["finding"] != "source-visible-mark-retained"
                or reading["inputTextSha256"] != pipeline.hash_text(layout[number]["text"])
                or reading["projectedTextSha256"] != pipeline.hash_text(bodies[number]["text"])
                or targets[number]["sourceId"] != key[0]):
            raise ValueError("Mark decision is stale or attempts an unreviewed punctuation change")
        check_proofs(reading, key[0], set(targets[number]["pdfPages"]), pins, root)
    loans = {r["number"]: r for r in review["loanBodyReadings"]}
    if set(loans) != {"455", "456"} or len(loans) != len(review["loanBodyReadings"]):
        raise ValueError("Both scoped84-21 loan bodies need separate source collations")
    for number, instruction in [("455", 22), ("456", 23)]:
        reading, body = loans[number], bodies[number]
        if (reading["sourceId"] != "A1984072" or reading["instructionArticle"] != instruction
                or reading["inputTextSha256"] != pipeline.hash_text(body["text"])
                or reading["sourceTranscription"] != body["text"]
                or body["paragraphs"] != [body["text"]] or reading["printedBodyParagraphs"] != 1):
            raise ValueError("Loan source transcription/instruction differs from its complete projected body")
        check_proofs(reading, "A1984072", {8}, pins, root)
    retained = review["retained489Paragraph"]
    body = bodies["489"]
    if (retained["number"] != "489" or retained["sourceId"] != "A1975078"
            or retained["inputTextSha256"] != pipeline.hash_text(body["text"])
            or len(body["paragraphs"]) != 2 or retained["oldCompilationParagraph"] != body["paragraphs"][1]
            or retained["originalParagraph"] != original489
            or retained["originalParagraphSha256"] != pipeline.hash_text(original489)
            or not original489.strip() or "\n" in original489 or "الباقي بدون تغيير" in original489):
        raise ValueError("489must retain the exact proven original second paragraph")
    check_proofs(retained, "A1975078", {31}, pins, root)
    result = copy.deepcopy(bodies)
    result["489"]["paragraphs"][1] = original489
    result["489"].update(text="\n".join(result["489"]["paragraphs"]),
                         retainedOriginalParagraphSource="A1975078:PDF31:paragraph2")
    result["489"]["textSha256"] = pipeline.hash_text(result["489"]["text"])
    for number, row in result.items():
        row["punctuationDecisions"] = [r for r in actual.values() if r["number"] == number]
        if number in loans:
            row["loanJoBodyCollation"] = loans[number]
        row["fullArticleChecked"] = False
    return result


def main() -> None:
    # Reuse the independently source-read instruction inventory and its publication/image pins.
    amendments.main()
    path = WORK / "priority-final-collation-readings.json"
    review = pipeline.read_json(path)
    for name, sha in review["inputFiles"].items():
        item = (WORK / name).resolve()
        if not item.is_relative_to(WORK.resolve()) or pipeline.hash_file(item) != sha:
            raise ValueError("Final collation input changed")
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    pins = {s["id"]: s["sha256"] for s in inventory["sources"]}
    pins["A1975078"] = "ba213b5beef85fb30959320b817594324e043842446c953cd86e22a074630ee9"
    if pipeline.hash_file(pipeline.CACHE / "A1975078.pdf") != pins["A1975078"]:
        raise ValueError("Original489publication pin changed")
    retained = review["retained489Paragraph"]
    pilot_path = (pipeline.ROOT / retained["pilotInput"]).resolve()
    if not pilot_path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(pilot_path) != retained["pilotInputSha256"]:
        raise ValueError("Previously source-read489pilot changed")
    pilot = next(a for a in pipeline.read_json(pilot_path)["articles"] if a["number"] == "489")
    original489 = next(v for v in pilot["versions"] if v["introducedBy"] == "ordinance-75-58")["paragraphs"][1]
    bodies = {a["number"]: a for a in pipeline.read_json(WORK / "priority-amended-body-projection.json")["projectedCandidates"]}
    layout = {a["number"]: a for a in pipeline.read_json(WORK / "priority-layout-projection.json")["projectedCandidates"]}
    queue = pipeline.read_json(WORK / "priority-jo-word-readings.json")["punctuationQueue"]
    output = replay(review, bodies, layout, queue, inventory["operations"], pins, original489)
    audit = amendments.reconcile(inventory["operations"], list(output.values()), set(output))
    if len(queue) != 34 or len(output) != 274:
        raise ValueError("Final source-read Civil priority inventory changed")
    counts = Counter(row["compilationBodyKind"] for row in output.values())
    pipeline.write_json(WORK / "priority-collated-body-projection.json", {
        "method": __doc__.strip(), "reviewSha256": pipeline.hash_file(path),
        "priorityIdentities": 274, "bodyKinds": dict(counts), "joLoanBodiesCollated": 2,
        "punctuationFlagsReadAndReplayed": 34, "punctuationChanges": 0, "unconfirmedPunctuationFlags": 0,
        "retained489ParagraphProvenance": retained, "currentInstructionInventory": audit,
        "projectedCandidates": list(output.values()), "typedCivilChanged": False,
        "historicalConsolidationComplete": False, "fullArticleChecked": False,
        "remaining": "References, independent identity/outcome inventory,20seeded source-image samples and typed partial import; preserve prior pilots/history and fail-closed dates."})
    print("34 visible marks retained;455/456 JO bodies collated;489original paragraph2 retained;274research outcomes, typed Civil unchanged")


if __name__ == "__main__":
    main()
