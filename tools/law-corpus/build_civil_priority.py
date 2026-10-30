#!/usr/bin/env python3
"""Import the source-collated current Civil chapters, retaining the article363 conflict and all older versions.

This is a partial current-text delivery at the existing discovery cut-off. New snapshot dates stay
unsettled; the five earlier checked current versions are reused without duplicating their intervals.
Only489's current pilot receives additional checks. Its body, dates and mixed-source provenance stay exact.
"""
from __future__ import annotations

import copy
from collections import Counter

import apply_civil_amended_bodies
import civil_priority_base_collation
import civil_priority_collation
import civil_priority_final_audit
import civil_priority_outcomes as outcomes
import civil_priority_references as references
import pipeline
from scope_inventory import number_key

WORK = outcomes.WORK
OUT = pipeline.ROOT / "data/law/corpus/civil-code"
TODAY = "2026-10-30"
BASELINE = WORK / "priority-preimport-baseline.json"
CURRENT_489 = "civil-code:489@law-07-05"
PREFIX = "Civil priority current snapshot: "
TEMPORAL = ("Current text snapshot only at the2026-10-26 discovery cut-off. New snapshot bounds are unknown, "
            "not unlimited intervals. Original and amending publication dates are not substituted for commencement. "
            "Earlier checked resolved intervals remain untouched; full historical regimes/local receipt dates are deferred.")


def protected_version(version: dict) -> dict:
    return {k:v for k,v in version.items() if k != "verification"}


def preserve_baseline(content: dict) -> dict:
    if not BASELINE.exists():
        versions = [v for a in content["articles"] for v in a["versions"]]
        if len(content["articles"]) != 48 or len(versions) != 55:
            raise ValueError("Initial Civil import must freeze the original48identities/55versions")
        pipeline.write_json(BASELINE,{
            "sourceCheckpoint":"693842e","inputArticlesSha256":pipeline.hash_file(OUT / "articles.json"),
            "inputLedgerSha256":pipeline.hash_file(OUT / "ledger.json"),"inputReportSha256":pipeline.hash_file(OUT / "report.json"),
            "articleIds":[a["id"] for a in content["articles"]],
            "versions":[{"versionId":v["versionId"],"fullSha256":pipeline.hash_json(v),
                         "protectedFieldsSha256":pipeline.hash_json(protected_version(v))} for v in versions],
            "permittedCurrentVerificationAugmentation":[CURRENT_489],
            "limit":"All55body/paragraph/date/source/reference/path fields retained. Only current489verification is augmented; no duplicate edition-version is created."})
    return pipeline.read_json(BASELINE)


def verify_preserved(content: dict, baseline: dict) -> None:
    versions = {v["versionId"]:v for a in content["articles"] for v in a["versions"]}
    if not set(baseline["articleIds"]) <= {a["id"] for a in content["articles"]}:
        raise ValueError("A prior Civil identity was lost")
    for old in baseline["versions"]:
        version = versions.get(old["versionId"])
        if version is None or pipeline.hash_json(protected_version(version)) != old["protectedFieldsSha256"]:
            raise ValueError("A prior Civil version body/date/source/reference/path changed")
        if old["versionId"] != CURRENT_489 and pipeline.hash_json(version) != old["fullSha256"]:
            raise ValueError("A prior Civil version verification changed outside the current489pilot")


def merge_current(article: dict, version: dict) -> None:
    previous = [v for v in article["versions"] if v["versionId"] == version["versionId"]]
    if not previous:
        article["versions"].append(copy.deepcopy(version))
        return
    if len(previous) != 1 or previous[0]["text"] != version["text"] or previous[0]["paragraphs"] != version["paragraphs"]:
        raise ValueError("A current import cannot silently rewrite an existing legal version")
    old = previous[0]
    if old["verification"]["state"] == "checked":
        return
    if old["versionId"] != CURRENT_489:
        if old != version:
            raise ValueError("An existing unsettled snapshot changed; requires its own reviewed migration")
        return
    kept = [c for c in old["verification"]["checks"] if not c["method"].startswith(PREFIX)]
    old["verification"] = copy.deepcopy(version["verification"])
    old["verification"]["checks"] = kept + old["verification"]["checks"]


def paths_from_review(native: dict) -> dict:
    review = pipeline.read_json(WORK / "priority-necessary-heading-review.json")
    if (review["nativeCandidatesSha256"] != pipeline.hash_file(WORK / "native-candidates.json")
            or review["sourceId"] != "A2005044"
            or review["sourceSha256"] != pipeline.hash_file(pipeline.CACHE / "A2005044.pdf")):
        raise ValueError("Necessary heading review belongs to another source or path inventory")
    for proof in review["imagesActuallyRead"]:
        image = (pipeline.ROOT / proof["image"]).resolve()
        if (proof["pdfPage"] not in {22,23,24} or not image.is_relative_to(pipeline.CACHE.resolve())
                or pipeline.hash_file(image) != proof["imageSha256"]
                or proof["inspection"] != "actually read; not human/lawyer verification"):
            raise ValueError("Necessary legal heading image changed")
    expected = {"96","97","98"} | set(map(str,range(124,141))) | outcomes.INSERTIONS["liability"]
    corrections = {r["number"]:r for r in review["pathCorrections"]}
    if set(corrections) != expected or len(corrections) != len(review["pathCorrections"]):
        raise ValueError("Necessary heading target inventory changed")
    paths = {n:list(a["path"]) for n,a in native.items()}
    for number,row in corrections.items():
        if paths[number] != row["oldPath"]:
            raise ValueError("Necessary heading correction has a stale original path")
        wanted = list(row["oldPath"])
        if number in {"96","97","98"}:
            wanted[-1] = "2 مكرر - السبب"
            instruction = 28
        else:
            wanted[2] = "الفصل الثالث - الفعل المستحق للتعويض"
            instruction = 34
        if wanted != row["currentPath"] or instruction != row["instructionArticle"]:
            raise ValueError("Necessary heading correction exceeds the independently read instruction")
        paths[number] = wanted
    return paths


def source_from(regions: list[dict], publication: str, records: dict, role="body") -> dict:
    fragments = []
    for region in regions:
        record,folder = records[region["evidenceId"]]
        fragments.append({"publicationId":publication,"page":record["pdfPage"],
            "textLayer":(folder / "text-layer.txt").read_text(encoding="utf-8"),
            "ocr":(folder / "ocr.txt").read_text(encoding="utf-8"),"evidenceId":record["id"],
            "region":record["region"],"sourceSha256":record["sourceSha256"],"role":role})
    if not fragments:
        raise ValueError("No legislative locator for a current Civil record")
    return {"publicationId":publication,"page":min(f["page"] for f in fragments),
        "endPage":max(f["page"] for f in fragments),"printedPage":records[regions[0]["evidenceId"]][0].get("printedPage"),
        "textLayer":"\n".join(f["textLayer"] for f in fragments),"ocr":"\n".join(f["ocr"] for f in fragments),
        "evidenceId":fragments[0]["evidenceId"],"region":fragments[0]["region"],"fragments":fragments}


def build() -> None:
    apply_civil_amended_bodies.main()
    civil_priority_collation.main()
    civil_priority_base_collation.main()
    civil_priority_final_audit.main()
    content = pipeline.read_json(OUT / "articles.json")
    baseline = preserve_baseline(content)
    verify_preserved(content,baseline)
    ledger = pipeline.read_json(OUT / "ledger.json")
    projection = pipeline.read_json(WORK / "priority-base-collation-projection.json")
    bodies = {a["number"]:a for a in projection["projectedCandidates"]}
    native_file = pipeline.read_json(WORK / "native-candidates.json")
    native = {a["number"]:a for a in native_file["articles"] if a["numberScope"] == "main"}
    paths = paths_from_review(native)
    inventory = pipeline.read_json(WORK / "priority-amendment-inventory.json")
    ops = {n:op for op in inventory["operations"] for n in op["numbers"]}
    diagnostics = {source:{a["number"]:a for a in pipeline.read_json(WORK / filename)["results"]}
        for source,filename in [("A2005044","priority-a2005044-body-diagnostic-v2.json"),
                                ("A2007031","priority-a2007031-body-diagnostic-v2.json")]}
    base_rows = {r["number"]:r for file in sorted(WORK.glob("priority-base-readings-*.json"))
                 for r in pipeline.read_json(file)["readings"]}
    manifests = [pipeline.read_json(pipeline.ROOT / "tools/law-corpus" / name)
                 for name in ["civil-full-manifest.json","civil-priority-07-05-manifest.json","civil-ministry-manifest.json"]]
    extracts = [e for m in manifests for e in m["extracts"]]
    regions_by_number = {}
    for number in bodies:
        op = ops.get(number)
        if op and op["operation"] == "repeal":
            prefix = "civil-full-" if op["sourceId"] == "A2005044" else "civil-priority-07-05-"
            regions = [{"evidenceId":e["id"]} for e in extracts
                       if e["id"].startswith(prefix) and e["sourceId"] == op["sourceId"] and e["pdfPage"] in op["pdfPages"]]
        elif op and op["sourceId"] == "A1984072":
            regions = [{"evidenceId":"civil-84-21-art"+number}]
        elif op:
            regions = diagnostics[op["sourceId"]][number]["regions"]
        elif number in base_rows:
            regions = base_rows[number]["regions"]
        else:
            continue  # The four exact checked originals keep their existing source objects.
        regions_by_number[number] = regions
    wanted = {r["evidenceId"] for regions in regions_by_number.values() for r in regions}
    wanted.update("civil-ministry-"+str(p).zfill(3)+"-body" for n in bodies for p in native[n]["pages"])
    records = pipeline.evidence_by_id(pipeline.CACHE,wanted)
    if set(records) != wanted:
        raise ValueError("Missing raw source evidence for current Civil delivery")
    for record,_ in records.values():
        for artifact in record["artifacts"]:
            path = (pipeline.CACHE / artifact["path"]).resolve()
            if not path.is_relative_to(pipeline.CACHE.resolve()) or pipeline.hash_file(path) != artifact["sha256"]:
                raise ValueError("Raw Civil evidence artifact changed")
    publications = {p["id"]:p for p in ledger["publications"]}
    loan = pipeline.read_json(WORK / "law-84-21-discovery-review.json")
    if "law-84-21" not in publications:
        p,s = loan["publication"],loan["source"]
        ledger["publications"].append({"id":"law-84-21","kind":"law","number":p["number"],"date":p["date"],
            "jo":{"issue":p["joIssue"],"date":p["joDate"],"pages":[2544]},
            "arabic":{k:s[k] for k in ["url","sha256","retrievedOn"]},"french":None,
            "articlesTouched":["455","456"],"commencementDate":None,"commencementBasis":TEMPORAL,
            "transitionalProvisions":[],"discovery":[{"url":s["url"],"accessedOn":"2026-10-29",
                "note":"Compilation footnotes led to actual Arabic instructions22/23 and issue-cover reading; omitted from the short Ministry index. Bounded Civil loan amendments only."}]})
    publications = {p["id"]:p for p in ledger["publications"]}
    for number,body in bodies.items():
        publication = ops[number]["publicationId"] if number in ops else "ordinance-75-58"
        p = publications[publication]
        p["articlesTouched"] = sorted(set(p["articlesTouched"])|{number},key=number_key)
    for p in publications.values():
        if pipeline.hash_file(pipeline.CACHE / ({"ordinance-75-58":"A1975078","law-83-01":"A1983005",
            "law-84-21":"A1984072","law-88-14":"A1988018","law-89-01":"A1989006",
            "law-05-10":"A2005044","law-07-05":"A2007031"}[p["id"]]+".pdf")) != p["arabic"]["sha256"]:
            raise ValueError("Civil legislative PDF differs from its ledger")
    existing = {a["number"]:a for a in content["articles"]}
    assembly = []
    for number,body in bodies.items():
        op = ops.get(number)
        publication = op["publicationId"] if op else "ordinance-75-58"
        article_id = references.identity(number)
        article = existing.get(number)
        if article is None:
            article = {"id":article_id,"number":number,"canonicalNumber":number,"path":paths[number],"availability":"text","versions":[]}
            content["articles"].append(article)
            existing[number] = article
        article["path"] = paths[number]
        version_id = article_id+"@"+publication
        if number in civil_priority_base_collation.PRIOR or number == "124":
            prior = next(v for v in article["versions"] if v["versionId"] == version_id)
            if prior["text"] != body["text"] or prior["verification"]["state"] != "checked":
                raise ValueError("Prior checked current body cannot be replaced by a compilation edition")
            assembly.append({"number":number,"versionId":version_id,"action":"reused-exact-checked-version","textSha256":pipeline.hash_text(body["text"])})
            continue
        repeal = body["compilationBodyKind"] == "repeal-marker"
        source = source_from(regions_by_number[number],publication,records,"repeal" if repeal else "body")
        if any(f["sourceSha256"] != publications[publication]["arabic"]["sha256"] for f in source["fragments"]):
            raise ValueError("Civil current source belongs to another legislative publication")
        source["transcriptionWitnesses"] = [{"document":{k:native_file["source"][k] for k in ["url","sha256","retrievedOn"]},
            "pages":native[number]["pages"],"textLayer":"\n".join(native[number]["rawParagraphs"]),
            "ocr":"\n".join(records["civil-ministry-"+str(p).zfill(3)+"-body"][1].joinpath("ocr.txt").read_text(encoding="utf-8")
                              for p in native[number]["pages"]),
            "evidenceIds":["civil-ministry-"+str(p).zfill(3)+"-body" for p in native[number]["pages"]],
            "textSha256":pipeline.hash_text(body["text"]),
            "note":"Official compilation witness: native article block before corrections, and separately retained full-page raw OCR (including neighbouring text). Canonical body follows the separately source-read corrections. Native font recovery, harakat/tatweel cleanup and edition orthography do not certify uniform printed glyph equality."}]
        checks = []
        reasons = [TEMPORAL,"Full historical archive and broad French paragraph alignment deferred by user; no lawyer verification."]
        conflict = number == "363"
        if conflict:
            reasons.append("UNRESOLVED: original ArabicJO78/1975 PDF23 prints reference224; current Ministry ArabicPDF65 and original FrenchPDF19 print184. Ministry184is retained as an unsettled witness, not an authoritative correction. No passed body check; no verified Arabic rectification located in the limited search.")
        else:
            if op:
                proofs = [p["sha256"] for p in inventory["imagesActuallyRead"] if p["sourceId"] == op["sourceId"] and p["pdfPage"] in op["pdfPages"]]
                note = "Source-read amendment instructions and complete current-body diagnostics; explicit wording/mark decisions and loan clauses replayed."
            else:
                proofs = body["currentBaseJoCheck"]["imageSha256s"]
                note = body["currentBaseJoCheck"]["note"]
            if not proofs:
                raise ValueError("A current checked record lacks its actual source image proof")
            checks.append({"check":"discrepancy-resolved-from-image","method":PREFIX+"actual source collation with guarded corrections; documented edition spelling retained",
                "date":TODAY,"result":"passed","textSha256":pipeline.hash_text(body["text"]),"imageSha256s":proofs,"note":note})
        checks += [{"check":"inventory-complete","method":PREFIX+"independent274-identity whole-chapter inventory and retained fixed sample",
                    "date":TODAY,"result":"passed","note":"Identity/outcome inventory only; article363wording remains unresolved."},
                   {"check":"amendment-applied","method":PREFIX+"101current operations:48replacements, one partial489,12insertions,40repeals",
                    "date":TODAY,"result":"passed","note":"Current outcomes of84-21/05-10/07-05, including all source-read exclusions83-01/88-14/89-01. Not a complete historical consolidation archive."}]
        if repeal:
            reasons.append("Empty checked current repeal marker only; former operative body and repeal commencement remain deferred.")
        version = {"versionId":version_id,"text":body["text"],"paragraphs":body["paragraphs"],"path":paths[number],
            "inForceFrom":None,"inForceTo":None,"temporalStatus":"unsettled","introducedBy":publication,
            "status":"repealed" if repeal else "in-force","source":source,"crossReferences":references.extract(body["paragraphs"]),
            "verification":{"state":"unsettled" if conflict else "checked","checks":checks,"reasons":reasons}}
        #489already has precise two-publication paragraph provenance and exact current text. Keep that object.
        merge_current(article,version)
        assembly.append({"number":number,"versionId":version_id,"action":"current489verification-augmented" if number == "489" else "current-record-imported",
                         "state":version["verification"]["state"],"textSha256":pipeline.hash_text(body["text"])})
    for target in {r["targetId"] for a in content["articles"] for v in a["versions"] for r in v["crossReferences"]}:
        if target not in {a["id"] for a in content["articles"]}:
            number = target.split(":",1)[1].replace("-bis"," مكرر").replace("-"," ")
            if target not in {references.identity(n) for n in ["170","182","182 مكرر","184","212"]}:
                raise ValueError("An unreviewed reference target needs separate identity review")
            content["articles"].append({"id":target,"number":number,"canonicalNumber":number,"path":[],"availability":"reference-only","versions":[]})
    content["articles"].sort(key=lambda a:number_key(a["number"]))
    chapters = copy.deepcopy(pipeline.read_json(WORK / "priority-current-outcome-audit.json")["coveredChapters"])
    for chapter in chapters:
        chapter["path"] = paths[chapter["articles"][0]][:3]
    targets = sorted(bodies,key=number_key)
    ledger["scope"] = {"kind":"chapter-subset","targetArticles":targets,"amendmentDiscoveryThrough":"2026-10-26",
        "complete":False,"coveredChapters":chapters,"unsettled":["Partial Civil delivery: seven selected chapters. Earlier308/309checked pilots and other prior records remain outside the chapter inventory; not the whole code.",
        "Article363reference conflict remains unresolved;273of274current chapter records checked. No full historical/date certificate.",TEMPORAL]}
    ledger["publications"].sort(key=lambda p:(p["date"],p["id"]))
    versions = [v for a in content["articles"] for v in a["versions"]]
    old_report = pipeline.read_json(OUT / "report.json")
    previous_reasons = {u["id"]:u["reason"] for u in old_report["unsettled"]}
    unsettled = [{"id":v["versionId"],"reason":" ".join(v["verification"]["reasons"]) or previous_reasons.get(v["versionId"],TEMPORAL)}
                 for v in versions if v["verification"]["state"] != "checked" or v["temporalStatus"] == "unsettled"]
    report = {"schemaVersion":"1.0","codeId":"civil-code","generatedOn":TODAY,
        "inventory":{"scope":"chapter-subset","expected":targets,"present":[a["canonicalNumber"] for a in content["articles"] if a["availability"] == "text"],
                     "gaps":[],"unappliedPublications":[],"complete":True},
        "countsByState":{state:sum(v["verification"]["state"] == state for v in versions) for state in ["checked","partly-checked","unsettled"]},
        "unsettled":unsettled,"coverage":"partial","coveredChapters":chapters}
    verify_preserved(content,baseline)
    current = [next(v for a in content["articles"] if a["number"] == n for v in a["versions"] if v["versionId"] == row["versionId"]) for n,row in {r["number"]:r for r in assembly}.items()]
    counts = dict(Counter(v["verification"]["state"] for v in current))
    if counts != {"checked":273,"unsettled":1} or len(current) != 274:
        raise ValueError("Civil partial must retain precisely273checked current records and the363conflict")
    for filename,value in [("ledger.json",ledger),("articles.json",content),("report.json",report)]:
        pipeline.write_json(OUT / filename,value)
    pipeline.write_json(WORK / "priority-typed-delivery-audit.json",{
        "method":__doc__.strip(),"generatedOn":TODAY,"baselineSha256":pipeline.hash_file(BASELINE),
        "finalPopulationAuditSha256":pipeline.hash_file(WORK / "priority-final-population-audit.json"),
        "headingReviewSha256":pipeline.hash_file(WORK / "priority-necessary-heading-review.json"),
        "currentPackageRecords":274,"currentPackageStates":counts,"preservedEarlierVersions":55,
        "allEarlierProtectedVersionFieldsUnchanged":True,"olderVersionsEntirelyUnchanged":54,
        "onlyCurrentVerificationAugmentation":CURRENT_489,"currentRecords":assembly,
        "typedFiles":{name:pipeline.hash_file(OUT / name) for name in ["ledger.json","articles.json","report.json"]},
        "fullCivilComplete":False,"entirePartialChecked":False,"unresolvedWordingConflicts":["363"]})
    print("Typed Civil:",len(content["articles"]),"identities;",len(versions),"versions;",report["countsByState"],
          "; current274package:",counts,"; all55prior version bodies/dates/provenance retained")


if __name__ == "__main__":
    build()
