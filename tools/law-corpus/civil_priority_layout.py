#!/usr/bin/env python3
"""Replay source-read Civil body/paragraph boundaries and separate compilation annotations.

This projection preserves raw candidates and every legal word/number. Compilation repeal labels are
not operative-law certificates: current amending instructions still need independent reconciliation.
"""
from __future__ import annotations

import re
from collections import Counter

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
SOURCE_SHA = "17a555bf81d5b99bf67417f3d42c157fd5df69499d6190cffbf182bc3566b573"
ANNOTATIONS = {"(معدلة)", "(جديدة)", "(ملغاة)"}
HEADING = "2 مكرر – السبب (جديدة) (1)"
RESTORED_LINE = "بالتعويض عن هذا الضرر.(4)"
SPLIT_BEFORE = "فإذا لحق المستأجر ضرر من العيب"


def project(body: dict, reading: dict, footnotes: list[dict], source_sha: str,
            root=pipeline.ROOT) -> dict:
    if (source_sha != SOURCE_SHA or reading["candidateKey"] != body["candidateKey"]
            or reading["inputTextSha256"] != pipeline.hash_text(body["text"])
            or body["text"] != "\n".join(body["paragraphs"])):
        raise ValueError("Layout reading belongs to another source/body/paragraph input")
    if (reading["inspection"] != "actually read; not human/lawyer verification"
            or not reading["note"].strip() or not reading["proofs"]):
        raise ValueError("Layout projection requires an actual source reading")
    proofs = reading["proofs"]
    if (len({p["pdfPage"] for p in proofs}) != len(proofs)
            or {p["pdfPage"] for p in proofs} != set(body["pages"])):
        raise ValueError("Layout proof must cover every article page exactly once")
    for proof in proofs:
        path = (root / proof["image"]).resolve()
        if not path.is_relative_to(root.resolve()) or pipeline.hash_file(path) != proof["sha256"]:
            raise ValueError("Layout image proof changed or escaped its evidence root")

    paragraphs = list(body["paragraphs"])
    separated = []
    heading = reading["excludedFollowingHeading"]
    if heading is not None:
        if (body["candidateKey"] != "main:95" or heading != HEADING
                or paragraphs[-1] != heading or paragraphs.count(heading) != 1):
            raise ValueError("Only the explicitly source-read trailing95 heading may be separated")
        paragraphs.pop()
        separated.append({"kind": "following-heading", "text": heading, "pdfPage": 24})

    restoration = reading["restoredDividerLine"]
    if restoration is not None:
        if (body["candidateKey"] != "main:140 مكرر 1" or body["pages"] != [32]
                or restoration != RESTORED_LINE or len(paragraphs) != 1
                or not paragraphs[0].endswith("تتكفل الدولة")
                or restoration in paragraphs[0]
                or not any(n["page"] == 32 and n["decodedText"] == "____________ " + restoration
                           for n in footnotes)):
            raise ValueError("Only the source-read140bis1 line merged with the divider may be restored")
        paragraphs[0] += " " + restoration

    annotation = reading["sourceStatusAnnotation"]
    actual = next((a for a in ANNOTATIONS if paragraphs[0].startswith(a)), None)
    if annotation != actual:
        raise ValueError("Compilation annotation partition changed")
    if annotation is not None:
        paragraphs[0] = paragraphs[0][len(annotation):].lstrip()
        separated.append({"kind": "compilation-status-label", "text": annotation})

    callout = reading["trailingFootnoteCallout"]
    if callout is not None:
        if (annotation is None or not re.fullmatch(r"\([1-9][0-9]*\)", callout["text"])
                or not paragraphs[-1].endswith(callout["text"])
                or callout["pdfPage"] not in body["pages"]
                or not callout["footnoteText"].startswith(callout["text"])
                or not any(n["page"] == callout["pdfPage"] and n["decodedText"] == callout["footnoteText"]
                           for n in footnotes)):
            raise ValueError("A footnote callout needs its exact trailing position and source note")
        paragraphs[-1] = paragraphs[-1][:-len(callout["text"])].rstrip()
        separated.append({"kind": "footnote-callout", **callout})
    elif annotation is not None:
        raise ValueError("Every selected status annotation has a source-read trailing footnote")

    is_repeal_marker = annotation == "(ملغاة)"
    if is_repeal_marker:
        if any(paragraphs):
            raise ValueError("A repeal marker cannot discard an operative or former body")
        paragraphs = []
    elif not all(paragraphs):
        raise ValueError("An operative body cannot become empty")

    split = reading["paragraphSplitBefore"]
    if split is not None:
        if (body["candidateKey"] != "main:489" or split != SPLIT_BEFORE or len(paragraphs) != 1
                or paragraphs[0].count(" " + split) != 1):
            raise ValueError("Only the explicitly source-read489 paragraph split may be projected")
        at = paragraphs[0].index(" " + split)
        paragraphs = [paragraphs[0][:at], paragraphs[0][at + 1:]]
    if len(paragraphs) != reading["printedBodyParagraphs"]:
        raise ValueError("Projected paragraphs disagree with the source-read paragraph inventory")
    retained_numbers = re.findall(r"\([0-9]+\)", "\n".join(paragraphs))
    if retained_numbers != reading["retainedParenthesizedLegalNumbers"]:
        raise ValueError("A legal number or its position changed during editorial separation")
    text = "\n".join(paragraphs)
    return {"candidateKey": body["candidateKey"], "number": body["number"], "numberScope": "main",
            "text": text, "paragraphs": paragraphs, "textSha256": pipeline.hash_text(text),
            "compilationBodyKind": "repeal-marker" if is_repeal_marker else "text",
            "sourceStatusAnnotation": annotation, "separatedCompilationMetadata": separated,
            "restoredDividerLine": restoration, "paragraphSplitBefore": split,
            "amendmentVerified": False, "fullArticleChecked": False}


def main() -> None:
    review_path = WORK / "priority-layout-readings.json"
    review = pipeline.read_json(review_path)
    for name, sha in review["inputFiles"].items():
        path = (WORK / name).resolve()
        if not path.is_relative_to(WORK.resolve()) or pipeline.hash_file(path) != sha:
            raise ValueError("Layout source/projection/inventory input changed")
    native = pipeline.read_json(WORK / "native-candidates.json")
    if native["source"]["sha256"] != review["sourceSha256"]:
        raise ValueError("Layout review source pin changed")
    raw = {a["candidateKey"]: a for a in native["articles"]}
    bodies = {a["candidateKey"]: a | {"pages": raw[a["candidateKey"]]["pages"]}
              for a in pipeline.read_json(WORK / "priority-word-boundary-projection.json")["projectedCandidates"]}
    inventory = pipeline.read_json(WORK / "priority-chapter-inventory.json")
    selected = {"main:" + n for c in inventory["chapters"] for n in c["articles"]}
    readings = {r["candidateKey"]: r for r in review["readings"]}
    if (len(selected) != 274 or selected != set(bodies) or selected != set(readings)
            or len(readings) != len(review["readings"])):
        raise ValueError("Every scoped Civil identity needs exactly one body/paragraph reading")
    output = [project(bodies[key], readings[key], native["footnotes"], review["sourceSha256"])
              for key in sorted(selected)]
    counts = Counter(m["kind"] for a in output for m in a["separatedCompilationMetadata"])
    kinds = Counter(a["compilationBodyKind"] for a in output)
    if counts != {"compilation-status-label": 101, "footnote-callout": 101, "following-heading": 1} or kinds != {"text":234,"repeal-marker":40}:
        raise ValueError("Source-read annotation/body-kind inventory changed")
    pipeline.write_json(WORK / "priority-layout-projection.json", {
        "method": __doc__.strip(), "reviewSha256": pipeline.hash_file(review_path),
        "priorityIdentities": len(output), "sourcePagesActuallyRead": 48,
        "compilationBodyKinds": dict(kinds), "separatedMetadataCounts": dict(counts),
        "projectedCandidates": output, "typedCivilChanged": False, "fullArticleChecked": False,
        "remaining": "Current amendment/repeal reconciliation, references/independent inventory/sample and typed partial import. No temporal certification inferred."})
    print("274 source-read paragraph/body records;234 bodies,40 compilation repeal markers;101 notes separated;140bis1 restored,489 split")


if __name__ == "__main__":
    main()
