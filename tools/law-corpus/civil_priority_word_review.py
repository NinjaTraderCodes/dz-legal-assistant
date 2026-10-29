#!/usr/bin/env python3
"""Replay saved image readings for a selected priority chapter; leave other findings explicitly unapplied.

Uses the original strict spacing guard. The only additional body-boundary operation is the separately
source-read trailing heading in360. This grants word-review coverage, not full legal/article certification.
"""
from __future__ import annotations

import apply_civil_word_review as legacy
import native_check as nc
import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"


def repair_388_context(body: dict, reading: dict) -> dict:
    old = "تسليم المبيع، م ا لم يوجد"
    new = "تسليم المبيع، ما لم يوجد"
    paragraphs = list(body["paragraphs"])
    text = body["text"]
    if (body["candidateKey"] != "main:388" or reading["canonical"] != "م ا"
            or reading["proposal"]["new"] != "ما" or text.count(old) != 1
            or sum(p.count(old) for p in paragraphs) != 1):
        raise ValueError("Ambiguous spacing needs the exact source-read388 context, not a global replacement")
    target = text.index(old) + len("تسليم المبيع، ")
    prefix = body["printedLabel"].rstrip(": ").strip() + " : " + text[:target]
    if len(nc.tokens(prefix)) != reading["at"]:
        raise ValueError("Context anchor does not cover the actual flagged token position")
    paragraphs = [p.replace(old, new) for p in paragraphs]
    return {"paragraphs": paragraphs, "text": "\n".join(paragraphs),
            "contextCorrection": {"oldContext": old, "newContext": new, "flagAt": reading["at"],
                                  "sourceReadNegationRestoration": True, "otherCharactersAndSpacesRetained": True}}


def apply(native: dict, audit: dict, saved: dict, selected: list[str], root=pipeline.ROOT) -> dict:
    bodies = {a["candidateKey"]: a for a in native["articles"]}
    if (not selected or len(selected) != len(set(selected)) or not set(selected) <= set(bodies)
            or native["source"]["sha256"] != saved["sourceSha256"]):
        raise ValueError("Priority selection is duplicated, absent or bound to another source")
    results = {r["number"]: r for r in audit["results"]}
    if not set(selected) <= set(results):
        raise ValueError("Priority chapter has an unaudited body")
    expected = {f"{number}#{index}": (number, flag) for number in selected
                for index, flag in enumerate(d for d in results[number]["disagreements"] if not d["punctuationOnly"])}
    readings = {r["itemId"]: r for r in saved["readings"] if r["candidateKey"] in selected}
    if len(readings) != sum(r["candidateKey"] in selected for r in saved["readings"]) or set(readings) != set(expected):
        raise ValueError("Priority chapter needs every source-read flag exactly once")
    repairs, contextual, exclusions, decisions = {}, {}, {}, []
    for ident, (number, flag) in expected.items():
        reading = readings[ident]
        if (reading["candidateKey"] != number or any(reading[k] != flag[k] for k in ("kind", "at", "canonical"))
                or reading["ocrExtra"] != flag.get("ocr")
                or reading["inputTextSha256"] != pipeline.hash_text(bodies[number]["text"])):
            raise ValueError("Priority reading belongs to a different body or flag")
        if (not reading["images"] or len(reading["images"]) != len(reading["imageSha256s"])
                or not reading["note"].strip() or reading["inspection"] != "actually read; not human/lawyer verification"):
            raise ValueError("Priority reading lacks actual source proof")
        for image, sha in zip(reading["images"], reading["imageSha256s"]):
            path = (root / image).resolve()
            if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Priority source proof changed or escaped the workspace")
        finding = reading["finding"]
        if finding == "source-read-spacing-restoration-proposed":
            proposal = reading["proposal"]
            if flag["kind"] != "unconfirmed" or proposal["old"] != flag["canonical"]:
                raise ValueError("Spacing projection does not address the actual source flag")
            correction = {"old": proposal["old"], "new": proposal["new"], "expectedOccurrences": 1}
            if bodies[number]["text"].count(proposal["old"]) != 1:
                repair_388_context(bodies[number], reading)
                contextual[number] = reading
                decisions.append({"itemId": ident, "finding": finding, "imageSha256s": reading["imageSha256s"],
                                  "projection": "unique source-read context bound to the flagged token; legacy global guard unchanged"})
                continue
            if correction["new"] in nc.NEGATIONS:
                correction["sourceReadJoinedNegation"] = True
            repairs.setdefault(number, []).append(correction)
        elif finding == "source-read-native-body-heading-contamination":
            proposal = reading["proposal"]
            heading = proposal["oldParagraph"]
            paragraphs = bodies[number]["paragraphs"]
            if (number != "main:360" or flag["kind"] != "unconfirmed" or flag["canonical"] != "2"
                    or heading != "-2 التزامات البائع" or len(paragraphs) < 2
                    or paragraphs[-1] != heading or paragraphs.count(heading) != 1):
                raise ValueError("Only the exact source-read trailing360 heading may leave the body")
            exclusions[number] = {"printedHeading": heading, "sourceItemId": ident,
                                   "images": reading["images"], "imageSha256s": reading["imageSha256s"]}
        elif finding in {"source-read-outside-heading-or-footer", "source-read-outside-next-article-label"}:
            if flag["kind"] != "extra-in-all-streams" or reading.get("proposal") is not None:
                raise ValueError("Outside-body finding cannot drop an unconfirmed native word")
        elif finding == "source-read-native-retained":
            if reading.get("proposal") is not None:
                raise ValueError("Retaining native text cannot carry a hidden correction")
        else:
            raise ValueError("Unsupported priority finding needs its own guarded source projection")
        decisions.append({"itemId": ident, "finding": finding, "imageSha256s": reading["imageSha256s"]})
    projected = []
    for number in selected:
        body = bodies[number]
        if not any(n == number for n, _ in expected.values()) and not results[number]["wordAgrees"]:
            raise ValueError("A body without source decisions still needs strict OCR agreement")
        value = legacy.repair(body, repairs.get(number, []))
        if number in contextual:
            update = repair_388_context(body | {"text": value["text"], "paragraphs": value["paragraphs"]}, contextual[number])
            value.update(update)
            value["textSha256"] = pipeline.hash_text(value["text"])
        if number in exclusions:
            value["paragraphs"] = value["paragraphs"][:-1]
            value["text"] = "\n".join(value["paragraphs"])
            value["textSha256"] = pipeline.hash_text(value["text"])
            value["excludedPrintedHeading"] = exclusions[number]
        value["qaState"] = "word-review complete for selected chapter; annotations/paragraph/amendment/date QA not certified"
        projected.append(value)
    return {"method": __doc__.strip(), "selectedCandidates": selected, "wordFlagsActuallyReadAndReplayed": len(readings),
        "spacingRepairsApplied": sum(map(len, repairs.values())) + len(contextual), "contextualSpacingRepairs": len(contextual),
        "bodyHeadingExclusions": len(exclusions),
        "sourceReadingsOutsideSelectionNotApplied": len(saved["readings"]) - len(readings),
        "mechanicalApplicationCompleteForSelection": True, "fullArticleChecked": False,
        "decisions": decisions, "projectedCandidates": projected}


def main() -> None:
    native_path = WORK / "native-candidates.json"
    audit_path = WORK / "ministry-page-bounded-exact-audit.json"
    saved_path = WORK / "image-readings-ministry-words-0350-0450.json"
    inventory_path = WORK / "priority-chapter-inventory.json"
    native, audit, saved = (pipeline.read_json(p) for p in (native_path, audit_path, saved_path))
    inventory = pipeline.read_json(inventory_path)
    if (saved["nativeSha256"] != pipeline.hash_file(native_path) or saved["auditSha256"] != pipeline.hash_file(audit_path)
            or inventory["nativeSha256"] != pipeline.hash_file(native_path)
            or inventory["source"]["sha256"] != native["source"]["sha256"]):
        raise ValueError("Saved priority/native/audit evidence changed")
    chapter = next(c for c in inventory["chapters"] if c["id"] == "sale")
    if pipeline.hash_file(pipeline.CACHE / "current-digital/civil-consolidated-ar.pdf") != inventory["source"]["sha256"]:
        raise ValueError("Priority boundary source PDF changed")
    for image in inventory["boundaryImagesActuallyRead"]:
        path = (pipeline.ROOT / image["image"]).resolve()
        if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != image["imageSha256"]:
            raise ValueError("Priority chapter-boundary reading proof changed")
    if chapter["articles"] != list(map(str, range(351, 413))):
        raise ValueError("Sale chapter selection differs from the independently read whole chapter")
    output = apply(native, audit, saved, ["main:" + n for n in chapter["articles"]])
    output |= {"sourceSha256": native["source"]["sha256"], "nativeSha256": pipeline.hash_file(native_path),
               "auditSha256": pipeline.hash_file(audit_path), "savedReadingSha256": pipeline.hash_file(saved_path),
               "priorityInventorySha256": pipeline.hash_file(inventory_path)}
    pipeline.write_json(WORK / "priority-sale-word-projection.json", output)
    print(len(output["projectedCandidates"]), "sale bodies;", output["wordFlagsActuallyReadAndReplayed"], "saved readings;",
          output["spacingRepairsApplied"], "strict spacing repairs; one guarded360 body-boundary exclusion; no typed import")


if __name__ == "__main__":
    main()
