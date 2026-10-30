#!/usr/bin/env python3
"""Replay the final Civil research population without redrawing its fixed source sample.

The original population and actual inspection remain immutable. A discovered source conflict
is carried into the final audit and never converted to a passed transcription check.
"""
from __future__ import annotations

import random
from collections import Counter

import civil_priority_outcomes as outcomes
import civil_priority_references as references
import pipeline
import spotcheck_civil_priority as sample
from scope_inventory import number_key

WORK = outcomes.WORK


def main() -> None:
    path = WORK / "priority-base-collation-projection.json"
    final = pipeline.read_json(path)
    if (final["pendingOriginalUnchangedBodies"] or not final["all173OriginalUnchangedBodiesAccountedFor"]
            or final["newOriginalBodiesActuallyReadAndReplayed"] != 169
            or final["priorCheckedOriginalBodiesRetained"] != 4
            or final["unresolvedWordingConflicts"] != ["363"]):
        raise ValueError("Final research inventory must retain all source readings and the unresolved363conflict")
    rows = sorted(final["projectedCandidates"],key=lambda a:number_key(a["number"]))
    native = {a["number"]:a for a in pipeline.read_json(WORK / "native-candidates.json")["articles"]
              if a["numberScope"] == "main"}
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    outcome = outcomes.audit(rows,inventory["operations"],native)
    found,ref_rows = {},[]
    for body in rows:
        refs = references.extract(body["paragraphs"])
        for ref in refs:
            found.setdefault((body["number"],ref["paragraph"]),[]).append(
                ref["targetId"].split(":")[1].replace("-bis"," مكرر").replace("-"," "))
        if refs:
            ref_rows.append({"number":body["number"],"textSha256":pipeline.hash_text(body["text"]),
                             "references":refs,"unresolvedSourceConflict":body["number"] == "363"})
    if found != references.EXPECTED:
        raise ValueError("Final numeric references differ from the independently read inventory")
    selected = pipeline.read_json(sample.OUT / "selection.json")
    inspected = pipeline.read_json(sample.OUT / "inspection.json")
    initial = sample.population()
    expected = [a["number"] for a in random.Random(sample.SEED).sample(rows,20)]
    if (selected["seed"] != sample.SEED or selected["populationSha256"] != sample.digest(initial)
            or selected["inputProjectionSha256"] != pipeline.hash_file(WORK / "priority-collated-body-projection.json")
            or inspected["selectionSha256"] != pipeline.hash_file(sample.OUT / "selection.json")
            or [a["number"] for a in selected["items"]] != expected
            or [a["number"] for a in inspected["decisions"]] != expected):
        raise ValueError("Final sample must retain the original draw, population and actual decisions")
    indexed = {a["number"]:a for a in rows}
    for item,decision in zip(selected["items"],inspected["decisions"]):
        actual = indexed[item["number"]]
        if (item["text"] != actual["text"] or item["paragraphs"] != actual["paragraphs"]
                or decision["textSha256"] != pipeline.hash_text(actual["text"])
                or decision["imageSha256s"] != item["imageSha256s"]
                or decision["inspection"] != "actually read; not human/lawyer verification"
                or decision["result"] not in {"body-words-figures-negation-and-paragraphs-match-source",
                                               "explicit-repeal-target-matches-source"}):
            raise ValueError("A selected body changed; never carry an old inspection over changed words")
        for image,sha in zip(item["images"],item["imageSha256s"]):
            image_path = (pipeline.ROOT / image).resolve()
            if not image_path.is_relative_to(sample.OUT.resolve()) or pipeline.hash_file(image_path) != sha:
                raise ValueError("Final sample source crop changed")
    old = {a["number"]:a for a in initial}
    changed = [{"number":a["number"],"beforeSha256":pipeline.hash_text(old[a["number"]]["text"]),
                "afterSha256":pipeline.hash_text(a["text"])} for a in rows if old[a["number"]]["text"] != a["text"]]
    pipeline.write_json(WORK / "priority-final-population-audit.json",{
        "method":__doc__.strip(),"reviewedOn":"2026-10-30","finalProjectionSha256":pipeline.hash_file(path),
        "initialPopulationSha256":sample.digest(initial),"finalPopulationSha256":sample.digest(rows),
        "selectionSha256":pipeline.hash_file(sample.OUT / "selection.json"),
        "inspectionSha256":pipeline.hash_file(sample.OUT / "inspection.json"),
        "fixedDrawRetained":expected,"selectedBodiesAndProofsUnchanged":True,"populationChanges":changed,
        "outcomeInventory":outcome,
        "outcomePathWitness":"Native Ministry heading paths; necessary current legal heading corrections are separately source-read and applied by build_civil_priority.py, not inferred by this body audit.",
        "necessaryHeadingReviewSha256":pipeline.hash_file(WORK / "priority-necessary-heading-review.json"),
        "numericReferences":ref_rows,"explicitReferenceOccurrences":sum(map(len,found.values())),
        "unresolvedWordingConflicts":["363"],"baseReadingResults":dict(Counter(
            a["currentBaseJoCheck"]["result"] for a in rows if "currentBaseJoCheck" in a)),
        "typedCivilChanged":False,"partialPackageChecked":False,
        "limit":"Article363reference184is only the retained Ministry witness. Original Arabic224remains unresolved. No checked-state, full-code or historical/date certificate."})
    print("274 final identities; 173 original bodies accounted; fixed20draw/texts/proofs unchanged; 17 references;363unresolved")


if __name__ == "__main__":
    main()
