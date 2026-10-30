#!/usr/bin/env python3
"""Replay actual original-JO body readings and preserve four earlier checked Civil transcriptions.

Unreviewed bodies remain explicitly pending; neither spelling folds nor OCR similarity grant checks.
Raw compilations, prior law versions and the fixed sample remain unchanged.
"""
from __future__ import annotations

import copy

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
PRIOR = {"138", "139", "140", "379"}


def view_for(reading: dict, views: dict) -> dict:
    viewed = views[reading["number"]]
    override = reading.get("sourceViewOverride")
    if override is None:
        return viewed
    if (reading["number"] != "81" or override["fromViewedArticle"] != "82"
            or override["previousImages"] != viewed["images"] or viewed["images"] or viewed["regions"]
            or not override["note"].strip()):
        raise ValueError("Only the actually read missing81view may reuse its complete82neighbour proof")
    donor = views["82"]
    if (not donor["images"] or reading["images"] != donor["images"]
            or reading["imageSha256s"] != donor["imageSha256s"] or reading["regions"] != donor["regions"]):
        raise ValueError("The81locator restoration must retain the actual donor source-image bindings")
    return viewed | {key: donor[key] for key in ["images", "imageSha256s", "regions"]}


def apply_reading(body: dict, reading: dict, viewed: dict, root=pipeline.ROOT) -> dict:
    if (reading["number"] != body["number"] or reading["inputTextSha256"] != pipeline.hash_text(body["text"])
            or viewed["textSha256"] != reading["inputTextSha256"] or viewed["text"] != body["text"]
            or body["text"] != "\n".join(body["paragraphs"])):
        raise ValueError("Original-JO reading belongs to another current body")
    if (reading["inspection"] != "actually read; not human/lawyer verification"
            or reading["finding"] != "same-clauses-edition-orthography" or reading["proposals"]
            or reading["printedBodyParagraphs"] != len(body["paragraphs"]) or not reading["note"].strip()
            or reading["images"] != viewed["images"] or reading["imageSha256s"] != viewed["imageSha256s"]
            or reading["regions"] != viewed["regions"] or not reading["images"]
            or len(reading["images"]) != len(reading["imageSha256s"])):
        raise ValueError("An actual complete source reading is required; a wording change needs its own guarded decision")
    for image,sha in zip(reading["images"],reading["imageSha256s"]):
        path = (root / image).resolve()
        if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
            raise ValueError("Original-JO body proof changed or escaped its root")
    result = copy.deepcopy(body)
    result["currentBaseJoCheck"] = {"result": reading["finding"], "textSha256":pipeline.hash_text(body["text"]),
        "imageSha256s":reading["imageSha256s"],"note":reading["note"],"verificationEffect":"Current unchanged-body collation only; no historical/date certificate"}
    return result


def main() -> None:
    initial = WORK / "priority-collated-body-projection.json"
    output = {a["number"]:copy.deepcopy(a) for a in pipeline.read_json(initial)["projectedCandidates"]}
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    amended = {n for op in inventory["operations"] for n in op["numbers"]}
    pending = set(output) - amended - PRIOR
    if len(pending) != 169:
        raise ValueError("Unchanged priority Civil body set changed")
    source_sha = "ba213b5beef85fb30959320b817594324e043842446c953cd86e22a074630ee9"
    if pipeline.hash_file(pipeline.CACHE / "A1975078.pdf") != source_sha:
        raise ValueError("Original Civil JO source changed")
    covered, inputs = set(), {}
    for path in sorted(WORK.glob("priority-base-readings-*.json")):
        review = pipeline.read_json(path)
        view_path = (pipeline.ROOT / review["viewPreparation"]).resolve()
        if (review["inputProjectionSha256"] != pipeline.hash_file(initial)
                or review["sourceId"] != "A1975078" or review["sourceSha256"] != source_sha
                or not view_path.is_relative_to(pipeline.CACHE.resolve())
                or pipeline.hash_file(view_path) != review["viewPreparationSha256"]):
            raise ValueError("Original-JO batch belongs to another source/projection/view")
        views = {r["number"]:r for r in pipeline.read_json(view_path)["items"]}
        rows = {r["number"]:r for r in review["readings"]}
        if len(rows) != len(review["readings"]) or set(rows) != set(views) or not set(rows) <= pending - covered:
            raise ValueError("Original-JO batches are duplicate, incomplete or outside the selected current bodies")
        for number,reading in rows.items():
            output[number] = apply_reading(output[number],reading,view_for(reading,views))
        covered.update(rows)
        inputs[path.name] = pipeline.hash_file(path)
    pilot_path = WORK / "priority-pilot-reconciliation.json"
    pilots = pipeline.read_json(pilot_path)
    saved_path = WORK / "priority-preserved-pilot-input.json"
    if (pilots["inputProjectionSha256"] != pipeline.hash_file(initial)
            or pilots["preservedInputSha256"] != pipeline.hash_file(saved_path)):
        raise ValueError("Prior checked Civil pilot bindings changed")
    saved = {a["number"]:a for a in pipeline.read_json(saved_path)["articles"]}
    decisions = {r["number"]:r for r in pilots["decisions"]}
    if set(saved) != PRIOR or set(decisions) != PRIOR or len(decisions) != len(pilots["decisions"]):
        raise ValueError("Only the four separately preserved checked JO pilots may supply these bodies")
    for number,decision in decisions.items():
        body = output[number]
        version = next(v for v in saved[number]["versions"] if v["versionId"] == decision["versionId"])
        if (decision["inputTextSha256"] != pipeline.hash_text(body["text"])
                or decision["checkedTextSha256"] != pipeline.hash_text(version["text"])
                or version["verification"]["state"] != "checked"
                or version["introducedBy"] != "ordinance-75-58"
                or version["text"] != "\n".join(version["paragraphs"])
                or decision["priorReview"]["textSha256"] != decision["checkedTextSha256"]
                or [p["imageSha256"] for p in decision["proofs"]] != decision["priorReview"]["imageSha256s"]):
            raise ValueError("Preserved JO pilot no longer has its exact checked text/proof binding")
        for proof in decision["proofs"]:
            image = (pipeline.ROOT / proof["image"]).resolve()
            if not image.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(image) != proof["imageSha256"]:
                raise ValueError("Earlier checked original-JO image proof changed")
        body.update(text=version["text"],paragraphs=version["paragraphs"],textSha256=decision["checkedTextSha256"],
            currentBaseJoCheck={"result":"prior-checked-original-JO-transcription-retained",
                "textSha256":decision["checkedTextSha256"],"imageSha256s":decision["priorReview"]["imageSha256s"],
                "note":decision["note"],"priorVersionId":decision["versionId"]})
    pipeline.write_json(WORK / "priority-base-collation-projection.json", {
        "method":__doc__.strip(),"inputProjectionSha256":pipeline.hash_file(initial),
        "batchInputs":inputs,"pilotReconciliationSha256":pipeline.hash_file(pilot_path),
        "newOriginalBodiesActuallyReadAndReplayed":len(covered),"priorCheckedOriginalBodiesRetained":4,
        "originalUnchangedBodiesAccountedFor":len(covered)+4,
        "pendingOriginalUnchangedBodies":sorted(pending-covered,key=lambda n:int(n)),
        "all173OriginalUnchangedBodiesAccountedFor":covered==pending,
        "projectedCandidates":list(output.values()),"typedCivilChanged":False,"fullArticleChecked":False,
        "remaining":"Complete original-current body conflicts, retain/update fixed sample without redraw and import validated typed partial; history/dates stay separate."})
    print(len(covered),"new original bodies replayed;4prior JO transcriptions retained;",len(pending-covered),"base bodies pending; typed Civil unchanged")


if __name__ == "__main__":
    main()
