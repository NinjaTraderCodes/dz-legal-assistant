#!/usr/bin/env python3
"""Assemble the reviewed current Family snapshot as explicitly incomplete typed data, not a finished delivery.

No historical body, commencement date, JO collation decision, paragraph certification or legal applicability is
invented. The Ministry witness keeps its own PDF identity. All versions stay partly-checked until final QA.
"""
from __future__ import annotations

import re

import pipeline

ROOT = pipeline.ROOT
WORK = ROOT / "data/law/corpus/research/family-2026-10-28"
OUT = ROOT / "data/law/corpus/family-code"
TODAY = "2026-10-28"
TEMPORAL = ("Exact commencement is unresolved: enactment/publication is not substituted for the applicable "
            "commencement rule and local receipt/publication evidence. Null bounds do not authorize historical use.")
NUMBER = r"\d+(?:\s+مكرر(?:\s+\d+)?)?"
REFERENCE = re.compile(r"(?P<label>(?:ال|لل)(?:مادة|مادتين|مادتان|مواد))\s*\(?\s*(?P<numbers>" + NUMBER + r"(?:\s*و\s*" + NUMBER + r")*)\s*\)?")


def identity(number: str) -> str:
    return "family-code:" + number.replace(" مكرر", "-bis").replace(" ", "-")


def references(paragraphs: list[str]) -> tuple[list[dict], list[dict]]:
    internal, external = [], []
    for index, paragraph in enumerate(paragraphs, 1):
        for match in REFERENCE.finditer(paragraph):
            tail = paragraph[match.end():]
            # Do not relabel a Civil Code or civil-status citation as a Family article with the same number.
            foreign = ("civil-code" if re.match(r"\s*من القانون المدني", tail)
                       else "civil-status" if re.match(r"\s*من قانون الحالة المدنية", tail) else None)
            for number in re.split(r"\s*و\s*", match["numbers"]):
                target = foreign + ":" + number if foreign else identity(number)
                item = {"targetId": target, "paragraph": index, "quote": match.group()}
                (external if foreign else internal).append(item)
    return internal, external


def download(source: dict) -> dict:
    return {k: source[k] for k in ("url", "sha256", "retrievedOn")}


def source_from(result: dict, publication: str, records: dict) -> dict:
    if not result["regions"]:
        raise ValueError("No JO locator for " + result["number"])
    fragments = []
    for region in result["regions"]:
        record, folder = records[region["evidenceId"]]
        fragments.append({"publicationId": publication, "page": record["pdfPage"],
                          "textLayer": (folder / "text-layer.txt").read_text(encoding="utf-8"),
                          "ocr": (folder / "ocr.txt").read_text(encoding="utf-8"),
                          "evidenceId": record["id"], "region": record["region"],
                          "sourceSha256": record["sourceSha256"], "role": "body"})
    return {"publicationId": publication, "page": min(f["page"] for f in fragments),
            "endPage": max(f["page"] for f in fragments),
            "textLayer": "\n".join(f["textLayer"] for f in fragments),
            "ocr": "\n".join(name + ":\n" + text for name, text in result["ocr"].items()),
            "evidenceId": fragments[0]["evidenceId"], "region": fragments[0]["region"], "fragments": fragments}


def build() -> None:
    native = pipeline.read_json(WORK / "native.json")
    source = native["source"]
    pdf = pipeline.CACHE / "current-digital/family-consolidated-bilingual.pdf"
    if pipeline.hash_file(pdf) != source["sha256"]:
        raise ValueError("Ministry PDF changed")
    unchanged = pipeline.read_json(WORK / "unchanged-ministry-reviewed.json")
    amended = pipeline.read_json(WORK / "amended-corrected.json")
    bodies = {a["number"]: a for a in unchanged + amended}
    inventory = pipeline.read_json(WORK / "amendment-inventory.json")
    actions = {number: op for op in inventory["operations"] for number in op.get("numbers", [])}
    expected_amended = {n for n, op in actions.items() if op["operation"] in {"replace", "insert"}}
    if expected_amended != {a["number"] for a in amended} or len(bodies) != 227:
        raise ValueError("Reviewed body set disagrees with independently read amendment targets")
    targets = [a["number"] for a in native["articles"]]
    if len(targets) != len(set(targets)) or len(targets) != 232:
        raise ValueError("Current inventory is not 232 distinct identities")
    manifests = [pipeline.load_manifest(ROOT / "tools/law-corpus" / name)
                 for name in ("family-current-manifest.json", "family-ministry-manifest.json")]
    source_pins = {s["id"]: s for m in manifests for s in m["sources"]}
    for ident, pin in source_pins.items():
        if pipeline.hash_file(pipeline.CACHE / (ident + ".pdf")) != pin["sha256"]:
            raise ValueError("Source PDF changed: " + ident)
    wanted = {e["id"] for m in manifests for e in m["extracts"]}
    records = pipeline.evidence_by_id(pipeline.CACHE, wanted)
    if set(records) != wanted:
        raise ValueError("Source evidence inventory is incomplete")
    for record, _ in records.values():
        for artifact in record["artifacts"]:
            path = (pipeline.CACHE / artifact["path"]).resolve()
            if not path.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(path) != artifact["sha256"]:
                raise ValueError("Source artifact changed or escaped the cache")
    jo_base = {r["number"]: r for r in pipeline.read_json(WORK / "base-unchanged-comparison.json")["results"]}
    jo_amended = {r["number"]: r for r in pipeline.read_json(WORK / "amended-comparison.json")["results"]}
    ministry = {r["number"]: r for r in pipeline.read_json(WORK / "ministry-raw-comparison.json")["results"]}
    french_pins = {s["id"]: s for s in pipeline.read_json(ROOT / "data/law/corpus/research/scope-source-downloads.json") if "sha256" in s}
    discovery = {"url": "https://droit.mjustice.gov.dz/fr/content/codes-0", "accessedOn": "2026-10-26",
                 "note": "Cached Ministry index, SHA256 46e5a42bf31a5ebe981f8faef8c6300779e1bca204c1cf50010aad339ee24bfc, "
                         "lists Ordinance05-02 as the Family amendment. A later live open timed out; cut-off not advanced."}
    common_reasons = ["Current snapshot candidate: final independent inventory/amendment/heading and 20-article delivery QA are pending.",
                      "French compilation numbering aligns; JO French structural check is not yet complete.",
                      "Complete historical archive deferred by user; missing former bodies are not represented as unchanged law.",
                      "Body text uses the documented text_clean normalization (including removal of harakat/tatweel); raw witnesses are retained. Printed vocalization is not certified.",
                      TEMPORAL]
    ledger = {"schemaVersion": "1.0", "id": "family-code", "citationName": "قانون الأسرة",
              "reference": "القانون رقم 84-11 المؤرخ في 9 يونيو 1984 المتضمن قانون الأسرة، المعدل والمتمم بالأمر رقم 05-02",
              "scope": {"kind": "complete-code", "targetArticles": targets, "amendmentDiscoveryThrough": "2026-10-26",
                        "complete": False, "unsettled": common_reasons + [
                            "Law05-09 approval (JO43/2005 p3) is discovered from the official French JO; Arabic acquisition/hash pending. No new Family wording is inferred."]},
              "publications": []}
    for ident, kind, number, date, issue, jo_date, pages, pin_id, french_id, touched in [
        ("law-84-11", "law", "84-11", "1984-06-09", "24", "1984-06-12", list(range(2, 17)), "A1984024", "F1984024", [str(n) for n in range(1, 225)]),
        ("ordinance-05-02", "ordinance", "05-02", "2005-02-27", "15", "2005-02-27", [19, 20, 21, 22], "A2005015", "F2005015", list(actions)),
    ]:
        ledger["publications"].append({"id": ident, "kind": kind, "number": number, "date": date,
            "jo": {"issue": issue, "date": jo_date, "pages": pages}, "arabic": download(source_pins[pin_id]),
            "french": download(french_pins[french_id]), "articlesTouched": touched,
            "commencementDate": None, "commencementBasis": TEMPORAL, "transitionalProvisions": [], "discovery": [discovery]})
    articles, unresolved, assembly = [], [], []
    for raw in native["articles"]:
        number = raw["number"]
        op = actions.get(number)
        repeal = raw["repealedBy0502"]
        if repeal != bool(op and op["operation"] == "repeal"):
            raise ValueError("Repeal annotation disagrees with JO inventory: " + number)
        publication = "ordinance-05-02" if op else "law-84-11"
        article_id = identity(number)
        version_id = article_id + "@" + publication
        paragraphs = [] if repeal else bodies[number]["paragraphs"]
        text = "\n".join(paragraphs)
        checks = []
        reasons = list(common_reasons)
        internal, external = references(paragraphs)
        if external:
            reasons.append("External article targets await in-scope corpus/import support; not remapped to Family numbers: "
                           + ", ".join(r["targetId"] for r in external))
        if repeal:
            proof = next(i for i in inventory["images"] if i["pdfPage"] == op["pages"][0])
            if pipeline.hash_file(ROOT / proof["cachePath"]) != proof["imageSha256"]:
                raise ValueError("Repeal image changed")
            evidence_ids = [e["id"] for e in manifests[0]["extracts"]
                            if e["sourceId"] == "A2005015" and e["pdfPage"] in op["pages"]]
            locator = {"number": number, "regions": [{"evidenceId": ident} for ident in evidence_ids], "ocr": {}}
            src = source_from(locator, publication, records)
            src["ocr"] = "\n".join(f["ocr"] for f in src["fragments"])
            src["fragments"] = [f | {"role": "repeal"} for f in src["fragments"]]
            reasons.append("Current repeal marker from independently read Ordinance05-02 art18; former article body is deferred, not replaced by empty historical text.")
        else:
            body = bodies[number]
            if pipeline.hash_text(text) != body["bodyCheck"]["textSha256"]:
                raise ValueError("Reviewed body changed: " + number)
            comparison = jo_amended[number] if op else jo_base[number]
            src = source_from(comparison, publication, records)
            src["transcriptionWitnesses"] = [{"document": download(source), "pages": raw["pages"],
                "textLayer": "\n".join(raw["rawParagraphs"]),
                "ocr": "\n".join(name + ":\n" + stream for name, stream in ministry[number]["ocr"].items()),
                "evidenceIds": ["family-ministry-" + str(p).zfill(3) + "-right" for p in raw["pages"]],
                "textSha256": pipeline.hash_text(text),
                "note": "Official Ministry compilation is the native-text witness, not the legislative publication. "
                        + ("Amending JO controls the corrected body; compilation discrepancies remain visible in raw text." if op
                           else "Word transcription checked against Ministry print; independent 1984 JO exact collation remains separate.")}]
            check = "discrepancy-resolved-from-image" if body["bodyCheck"]["imageSha256s"] else "text-layer-matches-ocr"
            checks.append({"check": check, "method": body["bodyCheck"]["rule"], "date": TODAY, "result": "passed",
                           "note": "Body word comparison only, with the named normalization; no printed-vocalization or whole-code certification.",
                           "textSha256": pipeline.hash_text(text), "imageSha256s": body["bodyCheck"]["imageSha256s"]})
            if body["bodyCheck"].get("unconfirmedPunctuation"):
                reasons.append("Unconfirmed punctuation: " + str(body["bodyCheck"]["unconfirmedPunctuation"]))
            if not op:
                reasons.append("Ministry transcription is settled; edition differences with the 1984 JO are not silently resolved.")
        version = {"versionId": version_id, "text": text, "paragraphs": paragraphs, "path": raw["path"],
                   "inForceFrom": None, "inForceTo": None, "temporalStatus": "unsettled", "introducedBy": publication,
                   "status": "repealed" if repeal else "in-force", "source": src, "crossReferences": internal,
                   "verification": {"state": "partly-checked", "checks": checks, "reasons": reasons}}
        articles.append({"id": article_id, "number": number, "canonicalNumber": number, "path": raw["path"],
                         "availability": "text", "versions": [version]})
        unresolved.append({"id": version_id, "reason": " ".join(reasons)})
        assembly.append({"number": number, "versionId": version_id, "textSha256": pipeline.hash_text(text),
                         "currentOperation": op["operation"] if op else "unchanged-body", "externalReferencesPending": external})
    if any(ref["targetId"] not in {a["id"] for a in articles} for a in articles for v in a["versions"] for ref in v["crossReferences"]):
        raise ValueError("Extracted internal reference is absent")
    report = {"schemaVersion": "1.0", "codeId": "family-code", "generatedOn": TODAY,
              "inventory": {"scope": "complete-code", "expected": targets, "present": targets, "gaps": [],
                            "unappliedPublications": ["ordinance-05-02"], "complete": False},
              "countsByState": {"checked": 0, "partly-checked": 232, "unsettled": 0},
              "unsettled": unresolved, "coverage": "partial", "coveredChapters": []}
    pipeline.write_json(OUT / "ledger.json", ledger)
    pipeline.write_json(OUT / "articles.json", {"schemaVersion": "1.0", "codeId": "family-code", "articles": articles})
    pipeline.write_json(OUT / "report.json", report)
    pipeline.write_json(WORK / "current-candidate-assembly.json", {"method": __doc__.strip(), "articles": assembly,
        "verificationEffect": "none; this is a current-snapshot assembly, not successful execution of a historical consolidation plan"})
    print("232 current-snapshot candidates; 0 checked; exact temporal applicability remains unresolved")


if __name__ == "__main__":
    build()
