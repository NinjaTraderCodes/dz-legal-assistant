#!/usr/bin/env python3
"""Reviewed article489 demonstrates a real paragraph-only amendment, not full Civil completion."""
from datetime import date

import consolidate
import pipeline

ORIGINAL = [
    "اذا وجد بالعين المؤجرة عيب يتحقق معه الضمان، يجوز للمستأجر حسب الظروف أن يطلب فسخ عقد الايجار أو انقاص الثمن وله كذلك أن يطلب اصلاح العيب أو أن يقوم هو باصلاحه على نفقة المؤجر اذا كان الاصلاح لا يكون نفقة باهظة عليه.",
    "فاذا لحق المستأجر ضرر من العيب التزم المؤجر بتعويضه مالم يثبت أنه كان يجهل وجود العيب.",
]
REPLACEMENT = "إذا وجد بالعين المؤجرة عيب يتحقق معه الضمان، يجوز للمستأجر حسب الحالة أن يطلب فسخ الإيجار أو إنقاص بدل الإيجار. وله كذلك أن يطلب إصلاح العيب أو أن يقوم هو بإصلاحه على نفقة المؤجر إذا كان الإصلاح لا يشكل نفقة باهظة على المؤجر."


def main():
    research = pipeline.ROOT / "data/law/corpus/research"
    ledger_path = pipeline.ROOT / "data/law/corpus/civil-code/ledger.json"
    spec_path = research / "priority-specs/civil-code.json"
    ledger, spec = pipeline.read_json(ledger_path), pipeline.read_json(spec_path)
    if ledger["scope"]["kind"] != "priority-subset" or len(spec["articles"]) > 10:
        raise ValueError("Pilot refuses to replace an expanded whole-code delivery")
    today = date.today().isoformat()
    downloads = {s["id"]: s for s in pipeline.read_json(research / "source-downloads.json")}
    publication_id = "law-07-05"
    if "489" not in ledger["scope"]["targetArticles"]:
        ledger["scope"]["targetArticles"].insert(-2, "489")
        ledger["scope"]["unsettled"].append("Article489 is a reviewed consolidation pilot: Law07-05 replaces paragraph1 only; its old paragraph2 must be retained. Full Civil inventory, paths and version-chain certification remain incomplete.")
    original_pub = next(p for p in ledger["publications"] if p["id"] == "ordinance-75-58")
    if "489" not in original_pub["articlesTouched"]:
        original_pub["articlesTouched"].append("489")
    if not any(p["id"] == publication_id for p in ledger["publications"]):
        ledger["publications"].append({"id": publication_id, "kind": "law", "number": "07-05", "date": "2007-05-13",
            "jo": {"issue": "31", "date": "2007-05-13", "pages": [3, 4, 5, 6]},
            "arabic": {k: downloads["A2007031"][k] for k in ("url", "sha256", "retrievedOn")},
            "french": {k: downloads["F2007031"][k] for k in ("url", "sha256", "retrievedOn")},
            "articlesTouched": ["489"], "commencementDate": None,
            "commencementBasis": "No uniform national date inferred from publication; generic geographic rule remains unsettled. Art6 replaces489 paragraph1; the printed remainder-unchanged instruction preserves paragraph2.",
            "transitionalProvisions": ["civil-code:507-bis", "civil-code:507-bis-1"],
            "discovery": [{"url": downloads["A2007031"]["url"], "accessedOn": today,
                "note": "ArabicPDF4 art6 lists489; PDF5 prints its replacement first paragraph followed by (... الباقي بدون تغيير ...). FrenchPDF4 confirms the first-paragraph replacement and remainder. articlesTouched here is only the pilot-scope intersection, not the full amending-law inventory. Transition references identify further work, not imported text."}]})
    spec["articles"] = [a for a in spec["articles"] if a["id"] != "civil-code:489"]
    original_text = "\n".join(ORIGINAL)
    spec["articles"].append({"id": "civil-code:489", "number": "489", "canonicalNumber": "489", "path": [],
        "availability": "text", "versions": [{"versionId": "civil-code:489@ordinance-75-58",
            "text": original_text, "paragraphs": ORIGINAL, "inForceFrom": "1975-07-05", "inForceTo": None,
            "temporalStatus": "resolved", "introducedBy": "ordinance-75-58", "status": "in-force",
            "evidenceIds": ["civil-489-original-body"], "crossReferences": [],
            "verification": {"reasons": ["Historical Arabic heading path and complete amendment-chain/inventory certification pending."]}}]})
    plan = {"generatedOn": today, "operations": [{"id": "law-07-05-art6-civil489-paragraph1",
        "publicationId": publication_id, "number": "489", "action": "edit-paragraphs",
        "expectedBaseTextSha256": pipeline.hash_text(original_text), "effectiveDate": None,
        "temporalBasis": "Exact generic national commencement is unresolved; source publication date is not substituted for it.",
        "edits": [{"start": 1, "delete": 1, "paragraphs": [REPLACEMENT],
            "evidenceFragments": [{"evidenceId": "civil-489-2007-replacement", "publicationId": publication_id}]}],
        "instructionEvidence": [{"evidenceId": "civil-489-2007-instruction", "publicationId": publication_id}],
        "crossReferences": []}]}
    result, audit = consolidate.apply_plan(spec, ledger, plan)
    reviews = {d["versionId"]: d for d in pipeline.read_json(research / "civil-pilot-image-decisions.json")["decisions"]}
    french_evidence = pipeline.evidence_by_id(pipeline.CACHE, {r["frenchStructure"]["evidenceId"] for r in reviews.values()})
    for article in result["articles"]:
        if article["id"] != "civil-code:489":
            continue
        for version in article["versions"]:
            # These are explicit, already-recorded review bindings. Never rebind
            # a review to newly rendered, unseen pixels on an automatic rerun.
            review = reviews[version["versionId"]]
            if review["textSha256"] != pipeline.hash_text(version["text"]):
                raise ValueError("Pilot transcription changed; a new explicit image decision is required")
            version["imageReview"] = {k: v for k, v in review.items() if k not in ("versionId", "frenchStructure")}
            french = review["frenchStructure"]
            record = french_evidence[french["evidenceId"]][0]
            french_image_hash = next(a["sha256"] for a in record["artifacts"] if a["path"].replace("\\", "/").endswith("/crop.png"))
            if record["sourceSha256"] != french["sourceSha256"] or record["pdfPage"] != french["pdfPage"] or french_image_hash != french["imageSha256"]:
                raise ValueError("French structural evidence changed; explicit renewed review required")
            version.setdefault("verification", {}).setdefault("checks", []).append({
                "check": "french-structure-matches", "method": "Explicit Arabic/French crop image inspection", "date": review["reviewedOn"],
                "result": "passed", "imageSha256s": [french["imageSha256"]],
                "note": "Original FrenchPDF25 has489 in two paragraphs. French2007PDF4 replaces the first and explicitly keeps the remainder; the consolidated second paragraph comes from1975. Structure only, not translation certification."})
    pipeline.write_json(research / "civil-pilot-initial.json", spec)
    pipeline.write_json(research / "civil-pilot-amendments.json", plan)
    pipeline.write_json(research / "civil-pilot-consolidation-audit.json", audit)
    pipeline.write_json(spec_path, result)
    report = pipeline.compose(result, ledger, pipeline.CACHE, ledger_path.parent)
    print("Civil pilot composed; " + str(report["countsByState"]))


if __name__ == "__main__":
    main()
