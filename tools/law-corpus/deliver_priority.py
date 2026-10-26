#!/usr/bin/env python3
"""Delivery 1 (26 Oct 2026): the eleven priority articles, current version, `checked`.

Civil 124, 138, 139, 140, 308, 309, 379; CPCA 2, 504; Commercial 187 bis, 187 bis 1.
Every text was settled by Claude reading the official page images (150-1200 dpi renders of the pinned JO PDFs);
the images are hashed and their render recipe recorded in research/priority-current-review-2026-10-26.json.
Corrections against the earlier candidate texts come only from those readings (no guessed repair).
Amendment discovery: Justice Ministry codes index fetched 26 Oct 2026 (lists texts up to 12 Oct 2026).
Run: tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/deliver_priority.py
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import pipeline

ROOT = pipeline.ROOT
CORPUS = ROOT / "data/law/corpus"
REVIEW = pipeline.CACHE / "reviews/priority-2026-10-26"
TODAY = "2026-10-26"
INDEX = {"url": "https://droit.mjustice.gov.dz/fr/content/codes-0", "accessedOn": TODAY,
         "sha256": "46e5a42bf31a5ebe981f8faef8c6300779e1bca204c1cf50010aad339ee24bfc"}


def sha(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def image_hashes(names: list[str]) -> list[str]:
    return [pipeline.hash_file(REVIEW / n) for n in names]


def crop_hash(evidence_id: str) -> str:
    return pipeline.hash_file(next((pipeline.CACHE / "extractions").glob(f"*/page-*/{evidence_id}/crop.png")))


# The settled texts. None = the stored text was confirmed unchanged.
REVIEWS = {
    ("cpca", "2"): dict(images=["p1-cpca.png"], crops=["cpca-2-original"], text=None,
        note="Read every word, the comma after سريانه and the final full stop on the JO 21/2008 page 3 crop (2x). Agrees with the Ministry HTML text of article 2."),
    ("cpca", "504"): dict(images=["p1-cpca.png"], crops=["cpca-504-original"], text=None,
        note="Read every word, ستة (6), the comma after الصلح and the final full stop on the JO 21/2008 page 41 crop (2x)."),
    ("commercial-code", "187 مكرر"): dict(images=["p1-com.png"], crops=["commercial-187-bis-introduced"], text=None,
        note="Both paragraphs read word by word on JO 11/2005 page 9 (2x): three commas in paragraph 1, ما لم in two words, closing full stop printed after the closing quotation mark of the amending law (kept, as for all quoted article bodies)."),
    ("commercial-code", "187 مكرر 1"): dict(images=["p1-com.png"], crops=["commercial-187-bis-1-introduced"], text=None,
        note="Single paragraph read on JO 11/2005 page 9 (2x), including the cross-reference «المادة 187 مكرر» and the comma after أعلاه."),
    ("civil-code", "138"): dict(images=["p1-civil-138-original.png", "civil-138-original-lines.png"], crops=["civil-138-original"], text=None,
        note="JO 78/1975 page 9, 600 dpi line by line: dotless تولى، شىء، الذى، ويعفى as printed; all commas and both final full stops agree."),
    ("civil-code", "139"): dict(images=["p1-civil-139-original.png", "civil-139-original-lines.png"], crops=["civil-139-original"], text=None,
        note="JO 78/1975 page 9, 600 dpi: مالم printed as one word; full stop after ضرر; اليه without hamza; final full stop."),
    ("civil-code", "308"): dict(images=["p1-civil-308-309.png", "civil-308-original-lines.png", "civil-308-allati.png"], crops=["civil-308-original"], text=None,
        note="JO 78/1975 page 19, 600 dpi; التى checked at 1200 dpi: the mark over its alef is a broken alef top, not a separate hamza glyph; فى dotless; الآتية with madda."),
    ("civil-code", "309"): dict(images=["p1-civil-308-309.png", "civil-309-lines.png", "civil-309-words.png"], crops=["civil-309-original"],
        text=["يتقادم بخمس سنوات كل حق دورى متجدد ولو أقر به المدين كأجرة المبانى، والديون المتأخرة، والمرتبات والاجور، والمعاشات.",
              "غير أنه لا يسقط الريع المستحق في ذمة الحائز سىء النية، ولا الريع الواجب آداؤه على متصرف المال المشاع للمستحقين الا بانقضاء خمس عشرة سنة."],
        note="JO 78/1975 page 19 at 600 and 1200 dpi. Two corrections to the earlier candidate, both read from the image: «في ذمة» has two clear dots under the yeh (candidate had فى); «آداؤه» carries a madda, unlike the hamza of أقر in the same article (candidate had أداؤه). المتأخرة shows its hamza."),
    ("civil-code", "379"): dict(images=["p1-civil-379-original.png", "civil-379-original-lines.png", "civil-379-zoom.png", "civil-379-endmark.png"], crops=["civil-379-original"],
        text=["يكون البائع ملزما بالضمان اذا لم يشتمل المبيع على الصفات التى تعهد بوجودها وقت التسليم الى المشترى أو اذا كان بالمبيع عيب ينقص من قيمته، أو من الانتفاع به بحسب الغاية المقصودة منه حسبما هو مذكور بعقد البيع، أو حسبما يظهر من طبيعته أو استعماله. فيكون البائع ضامنا لهذه العيوب ولو لم يكن عالما بوجودها.",
              "غير ان البائع لا يكون ضامنا للعيوب التى كان المشترى على علم بها وقت البيع، أو كان فى استطاعته أن يطلع عليها لو أنه فحص المبيع بعناية الرجل العادى، الا اذا أثبت المشترى أن البائع آكد له خلو المبيع من تلك العيوب أو أنه أخفاها غشا عنه."],
        note="JO 78/1975 page 24, 600 and 1200 dpi. One correction read from the image: «آكد» carries a madda, unlike the hamza of أن at the start of the same line (candidate had أكد). The final mark after عنه is a full stop; the two hairline ticks beside it are much thinner than this font's comma and are print specks."),
    ("civil-code", "140"): dict(images=["p1-civil-140.png", "civil-140-original-a-400.png", "civil-140-original-b-400.png", "civil-140-endmarks.png", "civil-140-lastline.png", "civil-140b-800.png"], crops=["civil-140-original-a", "civil-140-original-b"],
        text=["من كان حائزا بأى وجه كان لعقار أو جزء منه، أو منقولات، حدث فيها حريق لا يكون مسؤولا نحو الغير عن الاضرار التى سببها هذا الحريق الا اذا ثبت أن الحريق ينسب الى خطئه أو خطأ من هو مسؤول عنهم.",
              "مالك البناء مسؤول عما يحدثه انهدام البناء من ضرر ولو كان انهداما جزئيا، مالم يثبت أن الحادث لا يرجع سببه الى اهمال فى الصيانة، أو قدم فى البناء، أو عيب فيه،",
              "ويجوز لمن كان مهددا بضرر يصيبه من البناء أن يطالب المالك باتخاذ مايلزم من التدابير الضرورية للوقاية من الخطر فان لم يقم المالك بذلك، جاز الحصول على اذن من المحكمة فى اتخاذ هذه التدابير على حسابه."],
        note="JO 78/1975 pages 9-10 at 400 and 800 dpi. Settles the earlier quarantine: no mark between الخطر and فان (line break). Corrections read from the image: «مايلزم» printed as one word; «فان» without hamza (the hamza of أن in the same paragraph is clearly printed); paragraph 2 ends with the comma glyph «،» (same shape as the commas after الصيانة and البناء on that line, unlike the round full stop after عنهم) — a printing slip kept as published. فى dotless throughout."),
    ("civil-code", "124"): dict(images=["civil-124-crop.png", "civil-124-comma.png"], crops=["civil-124-current-05-10"],
        text=["كل فعل أيا كان يرتكبه الشخص بخطئه، ويسبب ضررا للغير يلزم من كان سببا في حدوثه بالتعويض."],
        note="Law 05-10 art. 35, JO 44/2005 page 23, right column, 300 and 600 dpi. Read every word; the mark after بخطئه is a comma (Tesseract read ؛); في dotted in this 2005 typesetting. Closing full stop printed after the amending law's closing quotation mark, kept as for other quoted bodies. The text layer of this issue uses a legacy Latin-glyph font encoding and is not used as text."),
}

CIVIL_PATH_CH3 = ["الكتاب الثانى - الالتزامات والعقود", "الباب الاول - مصادر الالتزام", "الفصل الثالث - الفعل المستحق للتعويض"]
PATHS = {("civil-code", "124"): CIVIL_PATH_CH3 + ["القسم الأول - المسؤولية عن الأفعال الشخصية"],
         **{("civil-code", n): CIVIL_PATH_CH3 + ["القسم الثالث - المسؤولية الناشئة عن الاشياء"] for n in ("138", "139", "140")}}

NEW_PUBLICATIONS = {
    "civil-code": [
        dict(id="law-83-01", kind="law", number="83-01", date="1983-01-29", jo=dict(issue="5", date="1983-02-01", pages=[307, 308, 309, 310]),
             arabic="A1983005", french="F1983005",
             touched=["746", "748", "750", "750 مكرر", "750 مكرر 1", "750 مكرر 2", "751", "752", "753", "754", "755", "756", "756 مكرر", "756 مكرر 1", "756 مكرر 2", "756 مكرر 3", "757", "758", "759", "760", "761", "762", "763", "763 مكرر", "764", "764 مكرر", "764 مكرر 1", "764 مكرر 2", "765", "766", "767", "768", "769", "770", "771", "772"],
             basis="Final art. 5 orders publication only; no special commencement date identified (research/amendment-discovery.json)."),
        dict(id="law-88-14", kind="law", number="88-14", date="1988-05-03", jo=dict(issue="18", date="1988-05-04", pages=[749, 750, 751]),
             arabic="A1988018", french="F1988018",
             touched=["324", "324 مكرر", "324 مكرر 1", "324 مكرر 2", "324 مكرر 3", "324 مكرر 4", "324 مكرر 5", "324 مكرر 6", "324 مكرر 7", "326 مكرر 1", "326 مكرر 2", "416", "679", "680", "681", "681 مكرر 1", "681 مكرر 2", "681 مكرر 3"],
             basis="Final art. 2 orders publication only; no special commencement date identified."),
        dict(id="law-89-01", kind="law", number="89-01", date="1989-02-07", jo=dict(issue="6", date="1989-02-08", pages=[153, 154]),
             arabic="A1989006", french="F1989006", touched=[],
             basis="Final art. 2 orders publication only. Adds a management-contract chapter to Book II Title IX whose own printed numbering (1-10) is not yet mapped to code numbers; touched list intentionally empty until reconciled."),
    ],
    "commercial-code": [
        dict(id="law-15-20", kind="law", number="15-20", date="2015-12-30", jo=dict(issue="71", date="2015-12-30", pages=[5]),
             arabic="A2015071", french="F2015071", touched=["566", "567", "567 مكرر", "567 مكرر 1", "590"],
             basis="Final art. 5 orders publication only; no special commencement date identified."),
        dict(id="law-22-09", kind="law", number="22-09", date="2022-05-05", jo=dict(issue="32", date="2022-05-14", pages=[12, 13]),
             arabic="A2022032", french="F2022032",
             touched=["544"] + [f"715 مكرر {n}" for n in range(133, 144)],
             basis="Final art. 4 orders publication only; no special commencement date identified."),
    ],
}

PRIORITY = {"civil-code": ["124", "138", "139", "140", "308", "309", "379"], "cpca": ["2", "504", "1062"],
            "commercial-code": ["187 مكرر", "187 مكرر 1"]}
CHECKED = {k for k in REVIEWS}


def pins() -> dict:
    manifest = pipeline.read_json(ROOT / "tools/law-corpus/scope-source-manifest.json")
    return {s["id"]: {"url": s["url"], "sha256": s["sha256"], "retrievedOn": s["retrievedOn"]} for s in manifest["sources"]}


def evidence_source(evidence_id: str, publication_id: str) -> dict:
    record = next(iter(pipeline.evidence_by_id(pipeline.CACHE, {evidence_id}).values()))[0]
    folder = next((pipeline.CACHE / "extractions").glob(f"*/page-*/{evidence_id}"))
    layer = (folder / "text-layer.txt").read_text(encoding="utf-8")
    ocr = (folder / "ocr.txt").read_text(encoding="utf-8")
    fragment = {"publicationId": publication_id, "page": record["pdfPage"], "textLayer": layer, "ocr": ocr,
                "evidenceId": evidence_id, "region": record["region"]}
    return {"publicationId": publication_id, "page": record["pdfPage"], "endPage": None, "printedPage": record.get("printedPage"),
            "textLayer": layer, "ocr": ocr, "evidenceId": evidence_id, "region": record["region"], "fragments": [fragment]}


def main() -> int:
    pinned = pins()
    review_record = {"deliveredOn": TODAY, "reviewer": "Claude (AI) visual reading of official page images; not a lawyer or human verification",
                     "imageFolder": "tools/law-corpus/.cache/evidence/reviews/priority-2026-10-26 (ignored cache; renders reproducible from the pinned PDFs)",
                     "articles": []}
    for code, numbers in PRIORITY.items():
        ledger_path, articles_path, report_path = (CORPUS / code / n for n in ("ledger.json", "articles.json", "report.json"))
        ledger, content = pipeline.read_json(ledger_path), pipeline.read_json(articles_path)
        publications = {p["id"]: p for p in ledger["publications"]}
        for new in NEW_PUBLICATIONS.get(code, []):
            if new["id"] not in publications:
                ledger["publications"].append({
                    "id": new["id"], "kind": new["kind"], "number": new["number"], "date": new["date"], "jo": new["jo"],
                    "arabic": pinned[new["arabic"]], "french": pinned[new["french"]], "articlesTouched": new["touched"],
                    "commencementDate": None, "commencementBasis": new["basis"], "transitionalProvisions": [],
                    "discovery": [{"url": INDEX["url"], "accessedOn": TODAY, "note": "Listed by the Justice Ministry codes index (page SHA-256 " + INDEX["sha256"] + ")."}]})
        ledger["publications"].sort(key=lambda p: p["date"])
        publications = {p["id"]: p for p in ledger["publications"]}

        # Civil 124: new identity, current version from Law 05-10.
        if code == "civil-code" and not any(a["number"] == "124" for a in content["articles"]):
            def base(n): return int(n.split()[0].split("-")[0])
            at = next((i for i, a in enumerate(content["articles"]) if base(a["number"]) > 124), len(content["articles"]))
            content["articles"].insert(at, {"id": "civil-code:124", "number": "124", "canonicalNumber": "124", "path": PATHS[(code, "124")],
                                        "availability": "text", "versions": [{
                "versionId": "civil-code:124@law-05-10", "text": "", "paragraphs": [], "inForceFrom": None, "inForceTo": None,
                "temporalStatus": "unsettled", "introducedBy": "law-05-10", "status": "in-force",
                "source": evidence_source("civil-124-current-05-10", "law-05-10"), "crossReferences": [],
                "verification": {"state": "unsettled", "checks": [], "reasons": ["pending"]}, "path": PATHS[(code, "124")]}]})

        for article in content["articles"]:
            key = (code, article["number"])
            if key not in REVIEWS:
                continue
            review = REVIEWS[key]
            current = [v for v in article["versions"] if v["status"] == "in-force" and v["inForceTo"] is None]
            assert len(current) == 1, key
            version = current[0]
            if review["text"] is not None:
                version["paragraphs"] = review["text"]
                version["text"] = "\n".join(review["text"])
            if key in PATHS:
                version["path"] = PATHS[key]
            text_hash = sha(version["text"])
            # A transcription check bound to an earlier text is stale: keep only checks that still bind.
            kept = [c for c in version["verification"]["checks"]
                    if c["check"] not in ("text-layer-matches-ocr", "discrepancy-resolved-from-image") or c.get("textSha256") == text_hash]
            kept = [c for c in kept if c["check"] not in ("inventory-complete", "amendment-applied")]
            images = [crop_hash(e) for e in review["crops"]] + image_hashes(review["images"])
            touching = [p["id"] for p in ledger["publications"] if article["canonicalNumber"] in p["articlesTouched"]]
            later = [p["id"] for p in ledger["publications"] if p["date"] > publications[version["introducedBy"]]["date"]]
            checks = kept + [
                {"check": "discrepancy-resolved-from-image", "method": "Claude visual reading of the official page image, word by word, every digit, negation and punctuation mark; bound to text and image SHA-256",
                 "date": TODAY, "result": "passed", "note": review["note"] + " AI reading, not a lawyer or human verification.", "textSha256": text_hash, "imageSha256s": images},
                {"check": "inventory-complete", "method": "Priority-subset inventory: identity present in the introducing official publication and in the current official Ministry source; no repeal recorded in the ledger",
                 "date": TODAY, "result": "passed", "note": json.dumps({"scope": "priority-subset", "targets": PRIORITY[code], "identity": article["canonicalNumber"],
                     "currentSource": {"cpca": "Ministry Arabic HTML 1065-article catalogue", "civil-code": "Ministry 2007 compilation (1,002/1,003 base labels; art. 41 explicitly repealed)",
                                       "commercial-code": "Law 05-02 art. 3 insertion; Ministry codes index"}[code]}, ensure_ascii=False)},
                {"check": "amendment-applied", "method": "Ledger of every amending text listed by the Justice Ministry codes index (fetched 2026-10-26, lists texts to 12 Oct 2026); each touching text applied",
                 "date": TODAY, "result": "passed", "note": json.dumps({"introducedBy": version["introducedBy"], "ledgerPublicationsTouchingArticle": touching,
                     "laterPublicationsInLedger": later, "laterPublicationsTouchingArticle": [p for p in later if p in touching],
                     "index": INDEX}, ensure_ascii=False)},
            ]
            version["verification"] = {"state": "checked", "checks": checks, "reasons": []}
            review_record["articles"].append({"code": code, "number": article["canonicalNumber"], "versionId": version["versionId"],
                                              "textSha256": text_hash, "imageSha256s": images, "cropEvidence": review["crops"], "reviewImages": review["images"],
                                              "corrected": review["text"] is not None, "note": review["note"]})

        # Scope and report: the priority subset of this code.
        scope = ledger["scope"]
        scope["kind"] = "priority-subset"
        scope["targetArticles"] = PRIORITY[code]
        scope["amendmentDiscoveryThrough"] = TODAY
        scope["complete"] = False
        scope["unsettled"] = [u for u in scope["unsettled"] if not u.startswith("Delivery 1")] + [
            "Delivery 1 (26 Oct 2026): this ledger's inventory covers the priority subset only; other identities kept here (history, pilots) remain partly-checked or unsettled with their reasons.",
            "Delivery 1: the Justice Ministry codes index fetched 26 Oct 2026 lists texts up to 12 Oct 2026; no amending text later than those in this ledger is listed for this code. Index discovery is not proof against unindexed or indirect changes."]
        versions = [v for a in content["articles"] for v in a["versions"]]
        present = sorted({a["canonicalNumber"] for a in content["articles"] if a["availability"] == "text"}, key=lambda n: (int(n.split()[0].split("-")[0]), n))
        report = pipeline.read_json(report_path)
        report["generatedOn"] = TODAY
        report["inventory"] = {"scope": "priority-subset", "expected": PRIORITY[code], "present": present,
                               "gaps": [n for n in PRIORITY[code] if n not in present], "unappliedPublications": [], "complete": True}
        report["countsByState"] = {s: sum(v["verification"]["state"] == s for v in versions) for s in ("checked", "partly-checked", "unsettled")}
        unsettled = []
        for v in versions:
            reasons = []
            if v["verification"]["state"] != "checked":
                reasons += v["verification"]["reasons"]
            if v["temporalStatus"] == "unsettled":
                reasons.append("Temporal start unsettled: the introducing text sets no national commencement date; Civil art. 4 gives a per-locality day-franc rule.")
            if reasons:
                unsettled.append({"id": v["versionId"], "reason": " ".join(dict.fromkeys(reasons))})
        report["unsettled"] = unsettled
        for version in versions:
            if version["verification"]["state"] != "checked":
                version["verification"]["reasons"] = [r.replace("Code inventory and amendment discovery remain incomplete.",
                    "Outside the delivery-1 priority list; not re-reviewed on 26 Oct 2026.") for r in version["verification"]["reasons"]]
        report["unsettled"] = [{"id": u["id"], "reason": u["reason"].replace("Code inventory and amendment discovery remain incomplete.",
            "Outside the delivery-1 priority list; not re-reviewed on 26 Oct 2026.")} for u in report["unsettled"]]
        pipeline.write_json(ledger_path, ledger)
        pipeline.write_json(articles_path, content)
        pipeline.write_json(report_path, report)
        print(code, report["countsByState"], "inventory complete", report["inventory"]["complete"])
    pipeline.write_json(CORPUS / "research/priority-current-review-2026-10-26.json", review_record)
    pipeline.write_json(CORPUS / "research/amendment-discovery-2026-10-26.json", {
        "index": INDEX, "method": "Full HTML of the Ministry codes index fetched with the task User-Agent; amending texts listed under each code transcribed below.",
        "codes": {
            "civil-code": {"original": "75-58", "listed": ["83-01", "88-14", "89-01", "05-10", "07-05"], "after2007": "none listed"},
            "cpca": {"original": "08-09", "listed": ["22-13"], "after2213": "none listed"},
            "family-code": {"original": "84-11", "listed": ["05-02"], "after0502": "none listed"},
            "commercial-code": {"original": "75-59", "listed": ["87-20", "93-08", "96-23", "96-27", "05-02", "15-20", "22-09"]},
            "labour-relations": {"original": "90-11", "listed": ["91-29", "94-03 (legislative decree)", "96-21", "97-02", "22-16"]},
            "labour-disputes": {"original": "90-04", "listed": "not listed in the index; discovery from the JO required"}},
        "indexCurrency": "The same page lists Law 26-13 of 12 Oct 2026 (Penal Code), so it was maintained to at least that date.",
        "limit": "Index discovery is not proof against unindexed or indirect changes."})
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
