#!/usr/bin/env python3
"""Delivery 2 (26 Oct 2026): the CPCA (Law 08-09 as amended by Law 22-13), complete and current.

Steps (run in order; each writes under data/law/corpus/research/cpca-2026-10-26/):
  inputs   cleaned native texts to compare: the 1,065 Ministry HTML articles (text_clean) and the 90 bodies quoted by
           Law 22-13 (extract_22_13 / jo_layer);
  (check_native.py runs the OCR comparison of each list against its column crops)
  build    ledger, articles and report from the compared texts, the image decisions and the consolidator.
Run: tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/deliver_cpca.py inputs|build
"""
from __future__ import annotations

import argparse
import json
import re
from collections import Counter

import check_native
import consolidate
import extract_22_13
import native_check as nc
import pipeline
import review_items
import text_clean

ROOT = pipeline.ROOT
CORPUS = ROOT / "data/law/corpus"
WORK = CORPUS / "research/cpca-2026-10-26"
HTML_BATCH = CORPUS / "research/current-batches/cpca-official-html-2008.json"
MANIFEST = ROOT / "tools/law-corpus/cpca-current-manifest.json"
TODAY = "2026-10-27"
SOURCES = {"law-08-09": "A2008021", "law-22-13": "A2022048"}
INDEX = {"url": "https://droit.mjustice.gov.dz/fr/content/codes-0", "accessedOn": "2026-10-26",
         "sha256": "46e5a42bf31a5ebe981f8faef8c6300779e1bca204c1cf50010aad339ee24bfc"}
REVIEWER = "Claude (AI) visual reading of the official page images; not a lawyer or human verification"
BOOK2_T1 = ["الكتاب الثاني - في الإجراءات الخاصة بكل جهة قضائية",
            "الباب الأول - في الإجراءات الخاصة بالمحكمة وفي الإجراءات الخاصة ببعض الأقسام",
            "الفصل الرابع - في القسم التجاري والمحكمة التجارية المتخصصة"]
BOOK4 = ["الكتاب الرابع - في الإجراءات المتبعة أمام الجهات القضائية الإدارية"]
APPEAL_TITLE = BOOK4 + ["الباب الأول مكرر - في الإجراءات المتبعة أمام المحاكم الإدارية للاستئناف"]
# Headings as printed in Law 22-13 (JO 48/2022 pages 3-11, read from the page images; not OCR-checked text).
NEW_PATHS = {
    "531": BOOK2_T1 + ["القسم الأول - في القسم التجاري", "الفرع الأول - في الاختصاص النوعي"],
    "532": BOOK2_T1 + ["القسم الأول - في القسم التجاري", "الفرع الثاني - الاختصاص الإقليمي"],
    "533": BOOK2_T1 + ["القسم الأول - في القسم التجاري", "الفرع الثالث - في التشكيلة"],
    **{n: BOOK2_T1 + ["القسم الأول - في القسم التجاري", "الفرع الرابع - في الخصومة"] for n in ("534", "535", "536")},
    "536 مكرر": BOOK2_T1 + ["القسم الثاني - في المحكمة التجارية المتخصصة", "الفرع الأول - في الاختصاص النوعي"],
    "536 مكرر 1": BOOK2_T1 + ["القسم الثاني - في المحكمة التجارية المتخصصة", "الفرع الثاني - في الاختصاص الإقليمي"],
    **{f"536 مكرر {k}": BOOK2_T1 + ["القسم الثاني - في المحكمة التجارية المتخصصة",
                                    "الفرع الثالث - في تشكيلة المحكمة التجارية المتخصصة وتنظيمها"] for k in (2, 3)},
    **{f"536 مكرر {k}": BOOK2_T1 + ["القسم الثاني - في المحكمة التجارية المتخصصة", "الفرع الرابع - في الخصومة"] for k in (4, 5, 6, 7)},
    "814 مكرر": BOOK4 + ["الباب الأول - في الإجراءات المتبعة أمام المحاكم الإدارية", "الفصل الأول مكرر - في التشكيلة"],
    **{n: APPEAL_TITLE + ["الفصل الأول - في الاختصاص", "القسم الأول - في الاختصاص النوعي"]
       for n in ("900 مكرر", "900 مكرر 1", "900 مكرر 2", "900 مكرر 3")},
    "900 مكرر 4": APPEAL_TITLE + ["الفصل الأول - في الاختصاص", "القسم الثاني - في طبيعة الاختصاص"],
    "900 مكرر 5": APPEAL_TITLE + ["الفصل الثاني - في التشكيلة"],
    "900 مكرر 6": APPEAL_TITLE + ["الفصل الثالث - في رفع الدعوى"],
    "900 مكرر 7": APPEAL_TITLE + ["الفصل الثالث - في رفع الدعوى", "القسم الأول - في الآجال"],
    "900 مكرر 8": APPEAL_TITLE + ["الفصل الثالث - في رفع الدعوى", "القسم الثاني - في وقف التنفيذ"],
    "900 مكرر 9": APPEAL_TITLE + ["الفصل الرابع - في الفصل في القضية"],
}
# Law 22-13 art. 9 and art. 11 rename a section and a chapter of Book IV: (title containing it, old heading, new heading).
RENAMED = [("الباب الثالث - في الاستعجال", "القسم الثالث - في طرق الطعن", "القسم الثالث - في الطعن في الأوامر الاستعجالية"),
           ("الباب السادس - في تنفيذ أحكام الجهات القضائية الإدارية",
            "الفصل الثاني - في الأحكام المطبقة على المحاكم الإدارية و مجلس الدولة",
            "الفصل الثاني - في الأحكام المطبقة على المحاكم الإدارية والمحاكم الإدارية للاستئناف ومجلس الدولة")]


def renamed(path: list[str]) -> list[str]:
    for title, old, new in RENAMED:
        if path[:1] == BOOK4 and title in path:
            path = [new if h == old else h for h in path]
    return path


REPEALED = ["826", "835", "836", "912", "913", "914"]
# Law 22-13 restates these only in part; the rest of the article is printed as «… (… بدون تغيير) …».
# number -> list of (first base paragraph, how many base paragraphs replaced, which 22-13 paragraphs replace them)
PARTIAL = {"600": [(1, 1, [0]), (9, 1, [2])], "801": [(1, 5, [0, 1, 2, 3, 4, 5])], "834": [(1, 1, [0])],
           "840": [(1, 1, [0])], "848": [(1, 1, [0])], "849": [(1, 1, [0])], "878": [(1, 1, [0])],
           "931": [(1, 2, [0, 1])], "976": [(5, 0, [1])]}
PLACEHOLDER = re.compile(r"بدون تغيير")


def label_for(number: str) -> str:
    return "المادة الأولى" if number == "1" else "المادة " + number


def inputs() -> None:
    batch = pipeline.read_json(HTML_BATCH)
    base = []
    for article in batch["articles"]:
        paragraphs = [text_clean.paragraph(p) for p in article["paragraphs"]]
        base.append({"number": article["number"], "label": label_for(article["number"]), "paragraphs": paragraphs,
                     "text": "\n".join(paragraphs), "path": article["path"], "url": article["url"],
                     "nativeTextSha256": pipeline.hash_text(article["text"])})
    pipeline.write_json(WORK / "base-2008-cleaned.json", base)
    layer = extract_22_13.extract(pipeline.CACHE / f"{extract_22_13.SOURCE}.pdf")
    quoted = [{"number": q["number"], "label": "”" + label_for(q["number"]), "paragraphs": q["paragraphs"],
               "text": "\n".join(q["paragraphs"]), "lawArticle": q["lawArticle"], "lines": q["lines"]}
              for q in layer["quotedArticles"]]
    pipeline.write_json(WORK / "law-22-13-layer.json", {k: v for k, v in layer.items() if k != "quotedArticles"} | {"quotedArticles": quoted})
    print(len(base), "base articles;", len(quoted), "quoted 22-13 bodies")


def apply_decisions(articles: list[dict], decisions: dict) -> list[dict]:
    """Apply the image decisions that found the print different from the native text, in three explicit forms:
    - from/to: replaces one exact string that must occur exactly once in the article;
    - insert: puts the printed mark right after body token `afterToken` (0-based, tokens as native_check counts them),
      which must still be the token `afterTokenText`; insertions are applied from the end so positions stay valid;
    - keepParagraphs: keeps only the first N paragraphs (text the print places outside the article).
    Anything that does not match exactly stops the build."""
    by_number = {a["number"]: a for a in articles}
    changes = [d for d in decisions["decisions"] if d["decision"] == "print-differs"]
    inserts = sorted((d for d in changes if "insert" in d), key=lambda d: (d["number"], -d["afterToken"]))
    for decision in inserts:
        article = by_number[decision["number"]]
        matches = list(nc.TOKEN.finditer(article["text"]))
        token = matches[decision["afterToken"]]
        if token.group() != decision["afterTokenText"]:
            raise ValueError(f"insertion {decision['id']}: token {decision['afterToken']} is «{token.group()}», not «{decision['afterTokenText']}»")
        article["text"] = article["text"][:token.end()] + decision["insert"] + article["text"][token.end():]
        article["paragraphs"] = article["text"].split("\n")
        article.setdefault("corrections", []).append(decision["id"])
    for decision in changes:
        if "insert" in decision:
            continue
        article = by_number[decision["number"]]
        if "keepParagraphs" in decision:
            article["paragraphs"] = article["paragraphs"][:decision["keepParagraphs"]]
        else:
            hits = sum(p.count(decision["from"]) for p in article["paragraphs"])
            if hits != 1:
                raise ValueError(f"correction {decision['id']} matches {hits} times in {decision['number']}")
            article["paragraphs"] = [p.replace(decision["from"], decision["to"]) for p in article["paragraphs"]]
        article["text"] = "\n".join(article["paragraphs"])
        article.setdefault("corrections", []).append(decision["id"])
    return articles


def base_decisions() -> None:
    """research/.../image-decisions-base.json from Claude's review log of the 93 base sheets (base-review-log.txt).
    Under the 28 Oct rule, the 16 wrong-line items that are punctuation only are not decided: they stay open and are
    reported as unverified punctuation. The 7 wrong-line items involving words or digits were read on whole-article
    crops (reviews/cpca-base/recheck)."""
    index = pipeline.read_json(pipeline.CACHE / "reviews/cpca-base/cpca-base-items.json")
    items = index["items"]
    recheck_dir = pipeline.CACHE / "reviews/cpca-base/recheck"
    recheck = {11: ["26.png", "26b.png"], 37: ["130.png"], 221: ["540.png"], 326: ["693.png"], 341: ["722.png"],
               384: ["758.png"], 385: ["758.png"]}
    base = {a["number"]: a for a in pipeline.read_json(WORK / "base-2008-cleaned.json")}
    decisions = []
    for line in (WORK / "base-review-log.txt").read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        number_s, verdict, rest = (line.split(" ", 2) + [""])[:3]
        k = int(number_s)
        item = items[k]
        images = [str(pipeline.Path(p).resolve().relative_to(ROOT)).replace("\\", "/") for p in item["images"]]
        hashes = list(item["imageSha256s"])
        record = {"id": item["id"], "number": item["number"], "kind": item["kind"], "canonical": item["canonical"], "ocr": item.get("ocr")}
        if verdict == "RECHECK":
            if k not in recheck:
                continue
            paths = [recheck_dir / n for n in recheck[k]]
            images = [str(p.relative_to(ROOT)).replace("\\", "/") for p in paths]
            hashes = [pipeline.hash_file(p) for p in paths]
            record |= {"decision": "native-matches-print", "reason": f"Whole-article crop: the print shows «{item['canonical'] or item.get('ocr')}» as in the native text"
                       + (" (the extra reading is the next label «المادة 131 : يؤدي»)." if k == 37 else ".")}
        elif verdict in ("OK", "HDR"):
            record |= {"decision": "native-matches-print",
                       "reason": "Running page header between two lines of the article, not article text." if verdict == "HDR"
                       else (rest or "The printed page shows the native text here.")}
        elif verdict == "INS":
            label_tokens = len(nc.tokens(label_for(item["number"]) + " :"))
            after = item["at"] - label_tokens
            text_tokens = nc.TOKEN.findall(base[item["number"]]["text"])
            record |= {"decision": "print-differs", "insert": rest, "afterToken": after, "afterTokenText": text_tokens[after],
                       "reason": f"The print has «{rest.strip()}» after «{text_tokens[after]}»; the Ministry HTML lost it."}
        elif verdict == "FIX":
            old, new = [s.strip() for s in rest.split("=>")]
            record |= {"decision": "print-differs", "from": old, "to": new, "reason": f"The print reads «{new}»."}
        elif verdict == "STRUCT":
            if item["id"] != "1065#0":
                record |= {"decision": "settled-by-correction", "reason": "Part of the art. 1065 correction (1065#0)."}
            else:
                record |= {"decision": "print-differs", "keepParagraphs": 1,
                           "reason": "The print ends art. 1065 at «…الشعبية.»; the Ministry HTML adds the heading «أحكام انتقالية وختامية» "
                                     "and the signature block (place, date, signatory), which are not article text."}
        record |= {"images": images, "imageSha256s": hashes}
        decisions.append(record)
    # After the printed marks were inserted, the comparison was run again: these differences now span the inserted mark
    # too, so their text no longer equals the reviewed item's. They are the same printed places, already read.
    moved = {"187": [63, 64], "235": [83], "423": [180, 181], "429": [184, 187], "565": [239, 241], "724": [349, 351],
             "741": [366, 368], "749": [375, 377], "974": [492], "1060": [549, 550]}
    check = pipeline.read_json(WORK / "check-base-reread.json")
    for result in check["results"]:
        if result["number"] not in moved:
            continue
        refs = [items[k] for k in moved[result["number"]]]
        for d in result["disagreements"]:
            if d.get("reread", {}).get("confirmed"):
                continue
            decisions.append({"id": f"{result['number']}#after-correction-{d['at']}", "number": result["number"], "kind": d["kind"],
                              "canonical": d["canonical"], "ocr": d.get("ocr"), "decision": "native-matches-print",
                              "reason": "Same printed place as review items " + ", ".join(r["id"] for r in refs)
                                        + " (read on the page image); the difference now spans the inserted printed mark or the heading below.",
                              "images": [str(pipeline.Path(p).resolve().relative_to(ROOT)).replace("\\", "/") for r in refs for p in r["images"]],
                              "imageSha256s": [h for r in refs for h in r["imageSha256s"]]})
    out = {"source": "A2008021", "reviewedOn": "2026-10-27/28", "reviewer": REVIEWER,
           "rule": "28 Oct 2026: punctuation-only differences do not block `checked`; open ones are listed as unverified punctuation.",
           "sheets": [{"sheet": str(pipeline.Path(s["sheet"]).resolve().relative_to(ROOT)).replace("\\", "/"), "sheetSha256": s["sheetSha256"]}
                      for s in index["sheets"]],
           "decisions": decisions}
    pipeline.write_json(WORK / "image-decisions-base.json", out)
    print(Counter(d["decision"] for d in decisions))


def marks(semicolon_sample: int = 24, seed: int = 20261027) -> None:
    """The Ministry HTML lost every «:» (0 in 1,065 articles), and an extra mark counts only when every OCR stream
    reads it, which PP-OCRv5 rarely does for «:». So, besides the stream comparison, every place where a colon may
    be missing becomes a review item: each paragraph ending without a mark, and each «:» Tesseract reads where the
    native text has none. «؛» is also absent from the HTML; Tesseract reads it often, mostly for «،»: a fixed random
    sample of those readings is reviewed to establish whether the print uses it. Writes check-base-marks.json."""
    import random
    check = pipeline.read_json(WORK / "check-base-reread.json")
    base = {a["number"]: a for a in pipeline.read_json(WORK / "base-2008-corrected.json")}
    semicolons = []
    for result in check["results"]:
        article = base[result["number"]]
        canonical = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        skip = len(nc.tokens(article["label"] + " :"))
        gaps, start = set(), skip
        for paragraph in article["paragraphs"]:
            start += len(nc.tokens(check_native.comparable(paragraph)))
            if paragraph and paragraph[-1] not in ".،:؛":
                gaps.add(start)
        tess = result["ocr"].get("tesseract", "").split(" ") if result["ocr"].get("tesseract") else []
        _, inserted = nc.token_statuses(canonical, tess)
        for gap, tokens in inserted.items():
            if gap > skip and ":" in tokens:
                gaps.add(gap)
            if gap > skip and "؛" in tokens:
                semicolons.append((result["number"], gap))
        for gap in sorted(gaps):
            result["disagreements"].append({"kind": "mark-candidate", "canonical": canonical[gap - 1], "at": gap - 1, "ocr": [":"]})
        result["agreesAfterReread"] = False if gaps else result.get("agreesAfterReread", result["agrees"])
    for number, gap in random.Random(seed).sample(semicolons, min(semicolon_sample, len(semicolons))):
        result = next(r for r in check["results"] if r["number"] == number)
        article = base[number]
        canonical = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        result["disagreements"].append({"kind": "semicolon-sample", "canonical": canonical[gap - 1], "at": gap - 1, "ocr": ["؛"]})
        result["agreesAfterReread"] = False
    check["marks"] = {"rule": marks.__doc__.strip(), "semicolonReadings": len(semicolons), "semicolonSample": semicolon_sample, "seed": seed}
    pipeline.write_json(WORK / "check-base-marks.json", check)
    print(sum(1 for r in check["results"] for d in r["disagreements"] if d["kind"] == "mark-candidate"), "colon candidates;",
          len(semicolons), "semicolon readings, sample", semicolon_sample)


def corrected() -> None:
    layer = pipeline.read_json(WORK / "law-22-13-layer.json")
    layer["quotedArticles"] = apply_decisions(layer["quotedArticles"], pipeline.read_json(WORK / "image-decisions-22-13.json"))
    pipeline.write_json(WORK / "law-22-13-corrected.json", layer)
    base = pipeline.read_json(WORK / "base-2008-cleaned.json")
    decisions = WORK / "image-decisions-base.json"
    if decisions.exists():
        base = apply_decisions(base, pipeline.read_json(decisions))
    pipeline.write_json(WORK / "base-2008-corrected.json", base)
    print("corrected:", sum(len(a.get("corrections", [])) for a in layer["quotedArticles"]), "in 22-13;",
          sum(len(a.get("corrections", [])) for a in base), "in the base")


def number_key(number: str) -> tuple:
    parts = number.split()
    return (int(parts[0]), 1 if len(parts) > 1 else 0, int(parts[2]) if len(parts) > 2 else 0)


def punctuation_only(d: dict) -> bool:
    """28 Oct 2026 rule: a difference made only of punctuation marks (colon, full stop, comma, semicolon, dash, brackets,
    quotation marks; spacing around them) does not block `checked`. Words, digits, article numbers and negations do."""
    if d["kind"] in ("mark-candidate", "semicolon-sample"):
        return True
    tokens = nc.tokens(d.get("canonical") or "") + [t for t in (d.get("ocr") or [])]
    return bool(tokens) and all(len(t) == 1 and t in nc.PUNCT for t in tokens)


class Verdicts:
    """Per article and source: how each token of the compared text was settled (streams, targeted re-read, image)."""

    def __init__(self, articles: list[dict], check: dict, decisions: dict, source: str):
        self.articles = {a["number"]: a for a in articles}
        self.results = {r["number"]: r for r in check["results"]}
        self.decisions = decisions
        self.source = source
        manifest = pipeline.load_manifest(MANIFEST)
        self.folders = dict(check_native.evidence_folders(manifest, source))
        self.stream = check_native.without_leaders(check_native.without_headers(check_native.tesseract_stream(list(self.folders.items()))))
        self.records = {k: v[0] for k, v in pipeline.evidence_by_id(pipeline.CACHE, set(self.folders)).items()}
        pool = Counter((d["number"], d.get("kind"), d.get("canonical"), tuple(d.get("ocr") or []))
                       for d in decisions["decisions"] if d["decision"] == "native-matches-print")
        self.by_article: dict[str, dict] = {}
        for number, result in self.results.items():
            image, open_items, punctuation, reread = [], [], [], 0
            for d in result["disagreements"]:
                if d.get("reread", {}).get("confirmed"):
                    reread += 1
                    continue
                key = (number, d["kind"], d.get("canonical"), tuple(d.get("ocr") or []))
                if pool[key] > 0:
                    pool[key] -= 1
                    image.append(next(x for x in decisions["decisions"] if (x["number"], x.get("kind"), x.get("canonical"), tuple(x.get("ocr") or [])) == key))
                elif punctuation_only(d):
                    punctuation.append(d)
                else:
                    open_items.append(d)
            image += [x for x in decisions["decisions"] if x["number"] == number and x["decision"] in ("print-differs", "settled-by-correction")]
            self.by_article[number] = {"image": image, "open": open_items, "punctuation": punctuation, "reread": reread, "result": result}

    def paragraph_sources(self, number: str) -> list[list[str]]:
        """Evidence crops of each paragraph, from the OCR words aligned to it (nearest aligned word if none)."""
        article, result = self.articles[number], self.results[number]
        canonical = nc.tokens(article["label"] + " : " + check_native.comparable(article["text"]))
        span = result["located"].get("tesseract")
        ocr = self.stream[span[0]:span[1]] if span else []
        pairs = review_items.matched_pairs(canonical, ocr)
        start = len(nc.tokens(article["label"] + " :"))
        fallback = [r["evidenceId"] for r in result["regions"]] or [next(iter(self.folders))]
        out = []
        for paragraph in article["paragraphs"]:
            end = start + len(nc.tokens(check_native.comparable(paragraph)))
            inside = [ocr[b]["source"] for a, b in pairs if start <= a < end]
            if not inside:
                near = sorted(pairs, key=lambda p: min(abs(p[0] - start), abs(p[0] - end)))
                inside = [ocr[near[0][1]]["source"]] if near else fallback[:1]
            out.append(list(dict.fromkeys(inside)))
            start = end
        return out

    def ocr_text(self, number: str) -> str:
        span = self.results[number]["located"].get("tesseract")
        return " ".join(t["token"] for t in self.stream[span[0]:span[1]]) if span else ""


def fragment(verdicts: Verdicts, evidence_id: str, publication_id: str, paragraph: int | None, text: str | None, ocr: str = "",
             role: str = "body") -> dict:
    record = verdicts.records[evidence_id]
    out = {"publicationId": publication_id, "page": record["pdfPage"], "textLayer": "", "ocr": ocr, "evidenceId": evidence_id,
           "region": record["region"], "sourceSha256": record["sourceSha256"], "role": role}
    if paragraph is not None:
        out |= {"paragraph": paragraph, "paragraphTextSha256": pipeline.hash_text(text)}
    return out


def transcription_check(parts: list[tuple[str, dict, Verdicts]], text: str, extra_images: list[str] = ()) -> tuple[dict | None, list[str]]:
    """parts: (label, verdict of one compared text, its Verdicts). Returns the check (or None) and open reasons."""
    reasons = [f"{label}: OCR disagreement not settled: «{d.get('canonical') or d.get('ocr')}» ({d['kind']})"
               for label, v, _ in parts for d in v["open"]]
    if reasons:
        return None, reasons
    images = [h for _, v, _ in parts for d in v["image"] for h in d["imageSha256s"]] + list(extra_images)
    crops = [pipeline.hash_file(vs.folders[r["evidenceId"]] / "crop.png") for _, v, vs in parts for r in v["result"]["regions"]]
    note = {"compared": [{"text": label, "source": vs.source, "evidence": [r["evidenceId"] for r in v["result"]["regions"]],
                          "rule": "native_check.compare_streams (Tesseract best psm 4 whole column + PP-OCRv5), then reread.py for unconfirmed tokens",
                          "confirmedTokens": v["result"]["confirmedBy"], "canonicalTokens": v["result"]["canonicalTokens"],
                          "settledByTargetedReread": v["reread"],
                          "punctuationUnverified": bool(v["punctuation"]),
                          "unverifiedPunctuation": [{"kind": d["kind"], "native": d.get("canonical"), "ocr": d.get("ocr"), "atToken": d["at"]}
                                                    for d in v["punctuation"]],
                          "imageDecisions": [{"id": d["id"], "decision": d["decision"], **({"from": d["from"], "to": d["to"]} if "from" in d else {}),
                                              "reason": d["reason"]} for d in v["image"]]} for label, v, vs in parts]}
    if images:
        return {"check": "discrepancy-resolved-from-image",
                "method": "Native text compared with two OCR readings of the official page and a targeted re-read; every remaining "
                          "difference in words, digits or negations read on the page image (strip images hashed) and recorded with its "
                          "reason; punctuation not settled is listed as unverified (28 Oct 2026 rule). " + REVIEWER,
                "date": TODAY, "result": "passed", "note": json.dumps(note, ensure_ascii=False),
                "textSha256": pipeline.hash_text(text), "imageSha256s": list(dict.fromkeys(images + crops))}, []
    return {"check": "text-layer-matches-ocr",
            "method": "Native text agrees token by token with independent OCR of the official printed page (rule: native_check; words, "
                      "digits and negations exact; punctuation not settled by OCR is listed as unverified, 28 Oct 2026 rule)",
            "date": TODAY, "result": "passed", "note": json.dumps(note, ensure_ascii=False), "textSha256": pipeline.hash_text(text)}, []


def build() -> None:
    batch = pipeline.read_json(HTML_BATCH)
    html = {a["number"]: a for a in batch["articles"]}
    base = pipeline.read_json(WORK / "base-2008-corrected.json")
    law = pipeline.read_json(WORK / "law-22-13-corrected.json")
    quoted = {q["number"]: q for q in law["quotedArticles"]}
    vb = Verdicts(base, pipeline.read_json(WORK / "check-base-reread.json"), pipeline.read_json(WORK / "image-decisions-base.json"), "A2008021")
    vl = Verdicts(law["quotedArticles"], pipeline.read_json(WORK / "check-22-13-reread.json"),
                  pipeline.read_json(WORK / "image-decisions-22-13.json"), "A2022048")
    law_decisions = pipeline.read_json(WORK / "image-decisions-22-13.json")
    not_located = [n for n, r in vb.results.items() if not r["located"]]
    if not_located:
        raise ValueError(f"base articles not located in the printed sequence: {not_located[:10]}")

    ledger = pipeline.read_json(CORPUS / "cpca/ledger.json")
    publications = {p["id"]: p for p in ledger["publications"]}
    base_numbers = [a["number"] for a in base]
    inserted = [n for n in quoted if n not in html]
    touched_2213 = sorted(set(quoted) | set(REPEALED) | {"32"}, key=number_key)
    if set(touched_2213) != set(publications["law-22-13"]["articlesTouched"]):
        raise ValueError("Law 22-13 touched list differs from the ledger inventory")
    publications["law-08-09"]["articlesTouched"] = base_numbers
    publications["law-22-13"]["articlesTouched"] = touched_2213

    # Initial spec: the 1,065 base versions, each paragraph bound to its JO 21/2008 crops.
    spec = {"generatedOn": TODAY, "articles": []}
    for article in base:
        sources = vb.paragraph_sources(article["number"])
        spec["articles"].append({
            "id": "cpca:" + article["number"], "number": article["number"], "canonicalNumber": article["number"],
            "path": [text_clean.paragraph(p) for p in article["path"]], "availability": "text",
            "versions": [{"versionId": f"cpca:{article['number']}@law-08-09", "text": article["text"], "paragraphs": article["paragraphs"],
                          "path": [text_clean.paragraph(p) for p in article["path"]], "inForceFrom": "2009-04-23", "inForceTo": None,
                          "temporalStatus": "resolved", "introducedBy": "law-08-09", "status": "in-force", "crossReferences": [],
                          "evidenceFragments": [{"evidenceId": e, "publicationId": "law-08-09", "paragraph": i}
                                                for i, ids in enumerate(sources, 1) for e in ids]}]})
    base_text = {a["number"]: a["text"] for a in base}

    def new_fragments(number: str, indexes: list[int]) -> tuple[list[str], list[dict]]:
        q, sources = quoted[number], vl.paragraph_sources(number)
        texts = [q["paragraphs"][i] for i in indexes]
        if any(PLACEHOLDER.search(t) for t in texts):
            raise ValueError(f"placeholder text selected as new wording in {number}")
        return texts, [{"evidenceId": e, "publicationId": "law-22-13", "paragraph": k} for k, i in enumerate(indexes, 1) for e in sources[i]]

    basis = ("Law 22-13 (JO 48/2022 of 17 July 2022) sets no calendar commencement (final art. 15 orders publication only); "
             "art. 13 defers the new subject-matter and territorial jurisdiction rules to the installation of the new courts and "
             "keeps earlier-filed cases with the former courts. No date is inferred.")
    instruction = {"14": [{"evidenceId": "a2022048-011-left", "publicationId": "law-22-13"}],
                   "9": [{"evidenceId": "a2022048-009-right", "publicationId": "law-22-13"}],
                   "11": [{"evidenceId": "a2022048-011-right", "publicationId": "law-22-13"}]}
    operations = []
    for number in sorted(quoted, key=number_key):
        q = quoted[number]
        op = {"id": "22-13:" + number, "publicationId": "law-22-13", "number": number, "effectiveDate": None,
              "temporalBasis": basis, "crossReferences": []}
        if number in NEW_PATHS:
            op["path"] = NEW_PATHS[number]
        if number in ("532", "536"):
            if [p for p in q["paragraphs"] if not PLACEHOLDER.search(p)]:
                raise ValueError(f"{number} expected «(بدون تغيير)» only")
            op |= {"action": "heading", "expectedBaseTextSha256": pipeline.hash_text(base_text[number]),
                   "instructionEvidence": [{"evidenceId": e, "publicationId": "law-22-13"} for e in vl.paragraph_sources(number)[0]]}
        elif number in PARTIAL:
            edits = []
            for first, remove, indexes in PARTIAL[number]:
                texts, fragments = new_fragments(number, indexes)
                edits.append({"start": first, "delete": remove, "paragraphs": texts, "evidenceFragments": fragments})
            op |= {"action": "edit-paragraphs", "expectedBaseTextSha256": pipeline.hash_text(base_text[number]), "edits": edits}
        else:
            if any(PLACEHOLDER.search(p) for p in q["paragraphs"]):
                raise ValueError(f"{number}: unexpected partial restatement")
            texts, fragments = new_fragments(number, list(range(len(q["paragraphs"]))))
            op |= {"action": "insert" if number in inserted else "replace", "paragraphs": texts, "evidenceFragments": fragments}
            if number not in inserted:
                op["expectedBaseTextSha256"] = pipeline.hash_text(base_text[number])
        operations.append(op)
    operations.append({"id": "22-13:32", "publicationId": "law-22-13", "number": "32", "action": "edit-paragraphs",
                       "expectedBaseTextSha256": pipeline.hash_text(base_text["32"]), "edits": [{"start": 7, "delete": 4}],
                       "instructionEvidence": instruction["14"], "effectiveDate": None, "temporalBasis": basis, "crossReferences": []})
    for number in REPEALED:
        operations.append({"id": "22-13:" + number, "publicationId": "law-22-13", "number": number, "action": "repeal",
                           "expectedBaseTextSha256": pipeline.hash_text(base_text[number]), "instructionEvidence": instruction["14"],
                           "effectiveDate": None, "temporalBasis": basis, "crossReferences": []})
    ledger["scope"] = {"kind": "complete-code", "targetArticles": sorted(base_numbers + inserted, key=number_key),
                       "amendmentDiscoveryThrough": "2026-10-26", "complete": True, "unsettled": []}
    result, audit = consolidate.apply_plan(spec, ledger, {"generatedOn": TODAY, "operations": operations})
    if result["unappliedPublications"]:
        raise ValueError(f"unapplied publications: {result['unappliedPublications']}")
    pipeline.write_json(WORK / "consolidation-audit.json", audit)
    pipeline.write_json(WORK / "consolidation-plan.json", {"generatedOn": TODAY, "operations": operations})

    # Repeal instruction (law art. 14) and renamed headings were read on the page image.
    repeal_decision = next(d for d in law_decisions["decisions"] if d["id"] == "law-14#repeal")
    articles, unsettled = [], []
    for article in sorted(result["articles"], key=lambda a: number_key(a["canonicalNumber"])):
        number = article["canonicalNumber"]
        final_versions = []
        for version in article["versions"]:
            publication = version["introducedBy"]
            fragments = []
            for f in version["evidenceFragments"]:
                vs = vb if f["publicationId"] == "law-08-09" else vl
                paragraph = f.get("paragraph")
                fragments.append(fragment(vs, f["evidenceId"], f["publicationId"], paragraph,
                                          version["paragraphs"][paragraph - 1] if paragraph else None, role=f.get("role", "body")))
            primary = [f for f in fragments if f["publicationId"] == publication]
            base_part = ("Law 08-09 text (Ministry HTML vs JO 21/2008)", vb.by_article[number], vb) if number in vb.by_article else None
            law_part = ("Law 22-13 text (JO 48/2022 text layer vs its print)", vl.by_article[number], vl) if number in vl.by_article else None
            extra = []
            if publication == "law-08-09":
                parts = [base_part]
            elif version["status"] == "repealed":
                parts, extra = [], repeal_decision["imageSha256s"]
            elif number == "32":
                parts, extra = [base_part], repeal_decision["imageSha256s"]
            elif number in ("532", "536"):
                parts = [base_part]
            elif number in PARTIAL:
                parts = [base_part, law_part]
            else:
                parts = [law_part]
            check, reasons = transcription_check(parts, version["text"], extra)
            touching = [p["id"] for p in ledger["publications"] if number in p["articlesTouched"]]
            checks = [check] if check else []
            checks += [
                {"check": "inventory-complete", "method": "Complete-code inventory: the 1,065 numbers of Law 08-09 (Ministry HTML catalogue), each found "
                 "in sequence in the OCR of JO 21/2008; the 19 insertions and 6 whole repeals of Law 22-13 read from JO 48/2022",
                 "date": TODAY, "result": "passed",
                 "note": json.dumps({"scope": "complete-code", "identities": len(ledger["scope"]["targetArticles"]), "base": len(base_numbers),
                                     "inserted": inserted, "repealed": REPEALED, "partlyRepealed": {"32": [7, 8, 9, 10]}, "identity": number},
                                    ensure_ascii=False)},
                {"check": "amendment-applied", "method": "Every amending text of the ledger applied by consolidate.py (hash-guarded operations, audit in "
                 "research/cpca-2026-10-26/consolidation-audit.json); ledger from the Justice Ministry codes index (texts to 12 Oct 2026)",
                 "date": TODAY, "result": "passed",
                 "note": json.dumps({"introducedBy": publication, "ledgerPublicationsTouchingArticle": touching,
                                     "isCurrent": version["inForceTo"] is None and version is article["versions"][-1], "index": INDEX}, ensure_ascii=False)}]
            state = "checked" if check else "partly-checked"
            path = version.get("path") or article["path"]
            if publication == "law-22-13":
                path = renamed(path)
            final = {"versionId": version["versionId"], "text": version["text"], "paragraphs": version["paragraphs"],
                     "inForceFrom": version["inForceFrom"], "inForceTo": version["inForceTo"], "temporalStatus": version["temporalStatus"],
                     "introducedBy": publication, "status": version["status"],
                     "source": {"publicationId": publication, "page": min(f["page"] for f in primary),
                                "endPage": (max(f["page"] for f in primary) if max(f["page"] for f in primary) > min(f["page"] for f in primary) else None),
                                "printedPage": None, "textLayer": "",
                                "ocr": (vb if publication == "law-08-09" else vl).ocr_text(number) if number in ((vb if publication == "law-08-09" else vl).results) else "",
                                "evidenceId": "+".join(dict.fromkeys(f["evidenceId"] for f in primary)), "region": primary[0]["region"],
                                "fragments": fragments},
                     "crossReferences": [], "verification": {"state": state, "checks": checks, "reasons": reasons}, "path": path}
            final_versions.append(final)
            why = list(reasons)
            if final["temporalStatus"] == "unsettled":
                why.append("Temporal boundary unsettled: " + basis)
            if why:
                unsettled.append({"id": final["versionId"], "reason": " ".join(why)})
        current_path = final_versions[-1]["path"]
        article_path = renamed(current_path)
        articles.append({"id": article["id"], "number": article["number"], "canonicalNumber": number, "path": article_path,
                         "availability": "text", "versions": final_versions})

    for publication in ledger["publications"]:
        publication["discovery"] = [d for d in publication["discovery"] if "priority subset" not in d["note"]] + [{
            "url": INDEX["url"], "accessedOn": INDEX["accessedOn"],
            "note": "Justice Ministry codes index (page SHA-256 " + INDEX["sha256"] + ", lists texts to 12 Oct 2026): Law 22-13 is the only "
                    "amending text listed for the CPCA. Index discovery is not proof against unindexed or indirect changes."}]
    versions = [v for a in articles for v in a["versions"]]
    present = [a["canonicalNumber"] for a in articles]
    report = {"schemaVersion": "1.0", "codeId": "cpca", "generatedOn": TODAY,
              "inventory": {"scope": "complete-code", "expected": ledger["scope"]["targetArticles"], "present": present,
                            "gaps": sorted(set(ledger["scope"]["targetArticles"]) - set(present), key=number_key),
                            "unappliedPublications": [], "complete": True},
              "coverage": "complete",
              "countsByState": {s: sum(v["verification"]["state"] == s for v in versions) for s in ("checked", "partly-checked", "unsettled")},
              "unsettled": unsettled}
    pipeline.write_json(CORPUS / "cpca/ledger.json", ledger)
    pipeline.write_json(CORPUS / "cpca/articles.json", {"schemaVersion": "1.0", "codeId": "cpca", "articles": articles})
    pipeline.write_json(CORPUS / "cpca/report.json", report)
    print(report["countsByState"], len(articles), "identities;", len(unsettled), "unsettled entries")


def spotcheck(count: int = 20, seed: int = 20261027, extra_2213: int = 4) -> None:
    """docs/law-corpus-checks/cpca: `count` random current versions, each beside the printed article cropped from the
    hashed column crops (the OCR-located region, two lines of margin). The inspection result is written by hand into
    inspection.json and rendered into the README; the generator never claims an inspection."""
    import random
    from PIL import Image
    content = pipeline.read_json(CORPUS / "cpca/articles.json")
    current = [(a, a["versions"][-1]) for a in content["articles"] if a["versions"][-1]["status"] == "in-force"]
    chosen = random.Random(seed).sample(current, count)
    # Addition of 28 Oct: the random draw held no Law 22-13 text, so 4 are drawn from those with the same seed.
    amended = [(a, v) for a, v in current if v["introducedBy"] == "law-22-13" and (a, v) not in chosen]
    chosen = sorted(chosen + random.Random(seed).sample(amended, extra_2213),
                    key=lambda av: number_key(av[0]["canonicalNumber"]))
    out = ROOT / "docs/law-corpus-checks/cpca"
    out.mkdir(parents=True, exist_ok=True)
    checks = {"law-08-09": pipeline.read_json(WORK / "check-base-reread.json"), "law-22-13": pipeline.read_json(WORK / "check-22-13-reread.json")}
    folders = {}
    for source in SOURCES.values():
        folders.update(check_native.evidence_folders(pipeline.load_manifest(MANIFEST), source))
    inspection_path = out / "inspection.json"
    inspection = pipeline.read_json(inspection_path) if inspection_path.exists() else {}
    lines = ["# Visual spot-check: CPCA (delivery 2)", "",
             f"{count} current versions drawn at random (seed {seed}) from `data/law/corpus/cpca/articles.json`, each beside the "
             "official printed article cropped from the hashed evidence column crops (the region OCR located, with a margin). "
             f"Because that draw held no Law 22-13 text, {extra_2213} versions introduced by Law 22-13 were added, drawn with the same seed. "
             "For a version amended by Law 22-13 in part, both the 2008 page (kept paragraphs) and the 2022 page are shown.",
             "", "Inspected by: " + REVIEWER + ".", ""]
    for article, version in chosen:
        number = article["canonicalNumber"]
        lines += [f"## Article {number} — `{version['versionId']}` — {version['verification']['state']}", ""]
        publications = list(dict.fromkeys(f["publicationId"] for f in version["source"]["fragments"] if f["role"] == "body"))
        for publication in publications:
            result = next((r for r in checks[publication]["results"] if r["number"] == number), None)
            for k, region in enumerate(result["regions"] if result else []):
                crop = Image.open(folders[region["evidenceId"]] / "crop.png")
                x0, y0, x1, y1 = region["box"]
                image = crop.crop((0, max(0, y0 - 60), crop.width, min(crop.height, y1 + 90)))
                name = f"{number.replace(' مكرر', '-bis').replace(' ', '-')}-{publication}-{k}.png"
                image.save(out / name, optimize=True)
                lines += [f"![{name}]({name})", "",
                          f"Cut from crop `{region['evidenceId']}` (SHA-256 `{pipeline.hash_file(folders[region['evidenceId']] / 'crop.png')}`); "
                          f"this image SHA-256 `{pipeline.hash_file(out / name)}`.", ""]
        lines += ["Canonical text:", "", "> " + "\n>\n> ".join(version["paragraphs"]), ""]
        verdict = inspection.get(version["versionId"])
        lines += ["Inspection: " + (verdict if verdict else "**not yet inspected**"), ""]
    (out / "README.md").write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(out, len(chosen), "articles;", sum(1 for a, v in chosen if v["versionId"] in inspection), "inspected")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("step", choices=["inputs", "decisions", "corrected", "marks", "build", "spotcheck"])
    args = parser.parse_args()
    if args.step == "inputs":
        inputs()
    elif args.step == "corrected":
        corrected()
    elif args.step == "build":
        build()
    elif args.step == "decisions":
        base_decisions()
    elif args.step == "marks":
        marks()
    elif args.step == "spotcheck":
        spotcheck()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
