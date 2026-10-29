#!/usr/bin/env python3
"""Account for accepted OCR joins independently of the original Civil word-discrepancy review.

An exact separate-word witness is retained; otherwise the boundary needs an actual source reading.
All edits are separate research projections, with no typed corpus or temporal certification.
"""
from __future__ import annotations

import copy
import difflib

import check_native
import civil_flag_spacing
import civil_ministry_audit
import civil_source_nouns
import family_literal
import native_check as nc
import pipeline

WORK = civil_ministry_audit.WORK


def joined_events(body: dict, streams: dict[str, str]) -> list[dict]:
    tokens = nc.tokens(body["printedLabel"].rstrip(": ").strip() + " : " + check_native.comparable(body["text"]))
    canonical, owners = family_literal.expand(tokens)
    expanded = {r: family_literal.expand(nc.tokens(text))[0] for r, text in streams.items()}
    statuses = {r: nc.token_statuses(canonical, words, strict=True)[0] for r, words in expanded.items()}
    events = {}
    for reader, words in expanded.items():
        matcher = difflib.SequenceMatcher(a=[nc.fold(t) for t in canonical], b=[nc.fold(t) for t in words], autojunk=False)
        for op, a1, a2, b1, b2 in matcher.get_opcodes():
            left, right = canonical[a1:a2], words[b1:b2]
            if (op != "replace" or len(left) <= len(right) or not right
                    or any(not t.isalpha() or len(nc.exact_form(t)) < 2 for t in left)
                    or any(any(c.isdigit() for c in t) for t in left + right)
                    or "".join(nc.exact_form(t) for t in left) != "".join(nc.exact_form(t) for t in right)):
                continue
            at, last = owners[a1], owners[a2 - 1]
            event = events.setdefault((at, last), {"candidateKey": body["candidateKey"], "at": at, "last": last,
                "canonical": " ".join(tokens[at:last + 1]), "readers": {}, "separatelyConfirmedBy":
                [r for r, states in statuses.items() if all(states[i] == "exact" for i in range(a1, a2))]})
            event["readers"][reader] = " ".join(right)
    return list(events.values())


def rebuild() -> dict:
    bodies = {a["candidateKey"]: copy.deepcopy(a) for a in pipeline.read_json(WORK / "native-candidates.json")["articles"]}
    for name in ["ministry-word-corrected.json", "ministry-word-corrected-0050-0150.json",
                 "ministry-word-corrected-0150-0250.json", "ministry-word-corrected-0250-0350.json"]:
        for row in pipeline.read_json(WORK / name)["correctedCandidates"]:
            body = bodies[row["candidateKey"]]
            if row["rawCandidateTextSha256"] != pipeline.hash_text(body["text"]):
                raise ValueError("Prior Civil spacing projection belongs to another raw body")
            body.update(text=row["text"], paragraphs=row["paragraphs"])
    for name in ["priority-sale-word-projection.json", "priority-remaining-word-projection.json"]:
        for row in pipeline.read_json(WORK / name)["projectedCandidates"]:
            bodies[row["candidateKey"]].update(text=row["text"], paragraphs=row["paragraphs"])
    return bodies


def main() -> None:
    review_path = WORK / "priority-word-boundary-readings.json"
    review = pipeline.read_json(review_path)
    for name, sha in review["inputFiles"].items():
        path = (WORK / name).resolve()
        if not path.is_relative_to(WORK.resolve()) or pipeline.hash_file(path) != sha:
            raise ValueError("Word-boundary review input/projection changed")
    native = pipeline.read_json(WORK / "native-candidates.json")
    if review["sourceSha256"] != native["source"]["sha256"]:
        raise ValueError("Word-boundary review belongs to another source")
    bodies = rebuild()
    inventory = pipeline.read_json(WORK / "priority-chapter-inventory.json")
    selected = {"main:" + n for chapter in inventory["chapters"] for n in chapter["articles"]}
    results = {r["number"]: r for r in pipeline.read_json(WORK / "ministry-page-bounded-exact-audit.json")["results"]}
    events = [event for key in selected if not bodies[key]["text"].startswith("(ملغاة)")
              for event in joined_events(bodies[key], results[key]["ocr"])]
    pending = {(e["candidateKey"], e["at"], e["canonical"]): e for e in events if not e["separatelyConfirmedBy"]}
    readings = {(r["candidateKey"], r["at"], r["canonical"]): r for r in review["readings"]}
    if len(events) != 172 or len(pending) != 28 or set(pending) != set(readings) or len(readings) != len(review["readings"]):
        raise ValueError("Every word-boundary case lacking an exact separate witness needs its own source reading")
    patches = {}
    retained = 0
    for key, reading in readings.items():
        body = bodies[reading["candidateKey"]]
        if (pipeline.hash_text(body["text"]) != reading["inputTextSha256"] or not reading["note"].strip()
                or reading["inspection"] != "actually read; not human/lawyer verification"
                or not reading["images"] or len(reading["images"]) != len(reading["imageSha256s"])):
            raise ValueError("Word-boundary source reading is stale or incomplete")
        for image, sha in zip(reading["images"], reading["imageSha256s"]):
            path = (pipeline.ROOT / image).resolve()
            if not path.is_relative_to(pipeline.ROOT.resolve()) or pipeline.hash_file(path) != sha:
                raise ValueError("Word-boundary source proof changed")
        proposal = reading["proposal"]
        if proposal is None:
            if reading["finding"] != "source-read-separate-words-retained" or reading["sourceReading"] != reading["canonical"]:
                raise ValueError("Retaining a boundary cannot silently change a word")
            retained += 1
            continue
        if reading["finding"] != "source-read-joined-word-proposed" or proposal["old"] != reading["canonical"]:
            raise ValueError("Boundary correction does not address its source-read phrase")
        if body["candidateKey"] in patches:
            raise ValueError("Multiple edits in one source noun body need an explicit simultaneous-span plan")
        if reading["sourceReadNounNotNegation"]:
            value = civil_source_nouns.project(body, reading, review["sourceSha256"])
        else:
            value = civil_flag_spacing.project(body, [{"old": proposal["old"], "new": proposal["new"], "at": reading["at"]}])
        patches[body["candidateKey"]] = value
    if len(patches) != 19 or retained != 9:
        raise ValueError("The independently read correction/retention partition changed")
    output = []
    for key in sorted(selected):
        body = bodies[key]
        value = patches.get(key, {"text": body["text"], "paragraphs": body["paragraphs"], "textSha256": pipeline.hash_text(body["text"])})
        output.append({"candidateKey": key, "number": body["number"], "numberScope": "main", **value})
    pipeline.write_json(WORK / "priority-word-boundary-projection.json", {"method": __doc__.strip(),
        "reviewSha256": pipeline.hash_file(review_path), "priorityIdentities": len(selected),
        "diagnosticJoinEvents": len(events), "separatelyConfirmedByExactOcr": len(events) - len(pending),
        "sourceCasesReadAndReplayed": len(readings), "joinedWordsRestored": len(patches), "nativeBoundariesRetained": retained,
        "known456WordBoundaryFollowupResolved": True, "projectedCandidates": output,
        "typedCivilChanged": False, "fullArticleChecked": False,
        "remaining": "Status/callout and paragraph QA, current amendments/repeals, references/inventory/sample and typed import; no broader historical/French work inferred."})
    print("172 join events accounted for:144 exact separate witnesses,28 source readings;19 word restores,9 retentions; typed Civil unchanged")


if __name__ == "__main__":
    main()
