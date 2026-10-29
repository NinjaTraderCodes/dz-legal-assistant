#!/usr/bin/env python3
"""Record the four actually cited external identities without importing or inventing their law texts.

Reuses the existing reference-only contract: no versions, dates or operative body, and VersionOn stays unsettled.
This is identity bookkeeping for Family citations, not expansion into the excluded special-law collection.
"""
from __future__ import annotations

import build_family_candidate as family
import pipeline

EXPECTED = {"family-code:82": "civil-code:42", "family-code:83": "civil-code:43",
            "family-code:86": "civil-code:40", "family-code:120": "civil-status:64"}


def collect(content: dict) -> list[dict]:
    rows = []
    for article in content["articles"]:
        for version in article["versions"]:
            _, external = family.references(version["paragraphs"])
            rows += [{"sourceArticleId": article["id"], "sourceVersionId": version["versionId"],
                      "sourceTextSha256": pipeline.hash_text(version["text"]), **ref} for ref in external]
    if len(rows) != 4 or {r["sourceArticleId"]: r["targetId"] for r in rows} != EXPECTED:
        raise ValueError("Family external-reference inventory changed; review before creating identities")
    return rows


def ensure(content: dict, target: str) -> bool:
    code_id,number = target.split(":",1)
    if code_id != content["codeId"] or not number.isdigit():
        raise ValueError("Reference identity is outside the intended numeric code namespace")
    existing = [a for a in content["articles"] if a["id"] == target]
    if existing:
        if len(existing) != 1 or existing[0]["canonicalNumber"] != number:
            raise ValueError("Conflicting reference target identity")
        return False
    content["articles"].append({"id":target,"number":number,"canonicalNumber":number,
                                "path":[],"availability":"reference-only","versions":[]})
    return True


def reference_catalogue(cutoff: str) -> tuple[dict,dict,dict]:
    reason = ("Reference-only identity cited by Family article120. No civil-status article text, version, dates or "
              "special-law collection is imported. No amendment inventory is audited; the shared research cut-off "
              "must not be read as complete discovery of this excluded code.")
    ledger = {"schemaVersion":"1.0","id":"civil-status","citationName":"قانون الحالة المدنية",
        "reference":"هوية مرجعية للمادة64 من قانون الحالة المدنية كما وردت في المادة120 من قانون الأسرة؛ لم يجمع نصها.",
        "scope":{"kind":"priority-subset","targetArticles":[],"amendmentDiscoveryThrough":cutoff,
                 "complete":False,"unsettled":[reason]},"publications":[]}
    content = {"schemaVersion":"1.0","codeId":"civil-status","articles":[]}
    ensure(content,"civil-status:64")
    report = {"schemaVersion":"1.0","codeId":"civil-status","generatedOn":"2026-10-29",
        "inventory":{"scope":"priority-subset","expected":[],"present":[],"gaps":[],"unappliedPublications":[],"complete":False},
        "countsByState":{"checked":0,"partly-checked":0,"unsettled":0},
        "unsettled":[{"id":"civil-status:64","reason":reason}],"coverage":"partial","coveredChapters":[]}
    return ledger,content,report


def main() -> None:
    root = pipeline.ROOT / "data/law/corpus"
    content = pipeline.read_json(root / "family-code/articles.json")
    rows = collect(content)
    civil_path = root / "civil-code/articles.json"
    civil = pipeline.read_json(civil_path)
    added = [r["targetId"] for r in rows if r["targetId"].startswith("civil-code:") and ensure(civil,r["targetId"])]
    if added:
        pipeline.write_json(civil_path,civil)
        report_path = root / "civil-code/report.json"
        report = pipeline.read_json(report_path)
        report["generatedOn"] = "2026-10-29"
        report["unsettled"] += [{"id":target,"reason":"Reference-only target of an explicit Family citation; no text/version/date imported."} for target in added]
        pipeline.write_json(report_path,report)
    catalogue_path = root / "civil-status"
    if not catalogue_path.exists():
        cutoff = pipeline.read_json(root / "family-code/ledger.json")["scope"]["amendmentDiscoveryThrough"]
        ledger,catalogue,report = reference_catalogue(cutoff)
        for name,data in [("ledger",ledger),("articles",catalogue),("report",report)]:
            pipeline.write_json(catalogue_path/(name+".json"),data)
    else:
        catalogue = pipeline.read_json(catalogue_path/"articles.json")
        if not any(a["id"]=="civil-status:64" for a in catalogue["articles"]):
            raise ValueError("Existing external catalogue lacks the required identity; do not overwrite it")
    pipeline.write_json(root / "research/family-2026-10-28/external-reference-targets.json",{
        "method":__doc__.strip(),"sources":rows,
        "civilReferenceOnlyTargets":[a["id"] for a in civil["articles"]
                                     if a.get("availability")=="reference-only" and a["id"] in EXPECTED.values()],
        "specialLawArticleBodiesCollected":0,"verificationEffect":"Identity links only; no article text, verification or temporal applicability inferred"})
    print("Four explicit external identities accounted for; no external article body or special-law text collected")


if __name__ == "__main__":
    main()
