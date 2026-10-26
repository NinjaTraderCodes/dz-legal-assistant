#!/usr/bin/env python3
"""Apply only hash-bound, explicit visual transcription findings; retain uncertainty."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RESEARCH = ROOT / "data/law/corpus/research"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    findings = read(RESEARCH / "civil-image-review-findings.json")
    candidates = read(RESEARCH / "priority-transcriptions.json")
    decisions = read(RESEARCH / "priority-image-decisions.json")
    for finding in findings["findings"]:
        matches = [i for i in candidates["items"] if i["code"] == "civil-code" and i["number"] == finding["number"]
                   and i["publicationId"] == finding["publicationId"]]
        if len(matches) != 1:
            raise ValueError("Unique candidate required")
        item = matches[0]
        if digest(item["text"]) not in (finding["originalCandidateTextSha256"], finding["correctedTextSha256"]):
            raise ValueError("Candidate changed since the visual finding")
        corrected = finding["correctedText"] if finding["correctedText"] is not None else item["text"]
        paragraphs = finding["correctedParagraphs"] if finding["correctedParagraphs"] is not None else item["paragraphs"]
        expected_hash = finding["correctedTextSha256"] or finding["originalCandidateTextSha256"]
        if digest(corrected) != expected_hash or corrected != "\n".join(paragraphs):
            raise ValueError("Corrected transcription hash/paragraph mismatch")
        actual_hashes = []
        for image in finding["imagePaths"]:
            path = (ROOT / image).resolve()
            if not path.is_relative_to((ROOT / "tools/law-corpus/.cache/evidence").resolve()):
                raise ValueError("Image path escapes corpus cache")
            actual_hashes.append(hashlib.sha256(path.read_bytes()).hexdigest())
        if actual_hashes != finding["imageSha256"]:
            raise ValueError("Image bytes changed after the finding")
        item["text"], item["paragraphs"] = corrected, paragraphs
        certain = finding["correctedTextCertain"]
        item["imageDecision"] = "resolved" if certain else "unsettled"
        item["unresolvedGlyphNotes"] = finding["unresolvedGlyphNotes"]
        item["reviewFinding"] = finding["versionId"]
        if certain:
            key = ("civil-code", finding["number"], finding["publicationId"])
            decisions["decisions"] = [d for d in decisions["decisions"] if (d["code"], d["number"], d["publicationId"]) != key]
            decisions["decisions"].append({"code": key[0], "number": key[1], "publicationId": key[2],
                "decision": "transcription-resolved", "reviewedOn": findings["reviewedOn"],
                "note": "Codex visual review, not a human or lawyer verification. " + finding["wordingReview"]
                    + " Exact before/after text, differences and image hashes are retained in research/civil-image-review-findings.json."
                    + " Article labels and physical line wrapping are publication layout, not body paragraphs."})
    decisions["unresolvedPolicy"] = "Civil140 and amended Civil7 retain explicit glyph/punctuation uncertainty; no bound resolved-text claim is generated for them. Full-code inventory/amendment certification remains incomplete for all versions."
    write(RESEARCH / "priority-transcriptions.json", candidates)
    write(RESEARCH / "priority-image-decisions.json", decisions)
    print(json.dumps({"findings": len(findings["findings"]), "resolvedCivilTranscriptions": sum(f["correctedTextCertain"] for f in findings["findings"])}))


if __name__ == "__main__":
    main()
