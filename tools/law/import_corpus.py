#!/usr/bin/env python3
"""Builds data/law/codes/<id>.json from a complete code of the law corpus (branch codex/law-corpus, docs/LAW-CORPUS.md).

The corpus holds each article's versions, each with its official publication, page and check state. The app needs one
text per article: the version in force today. This script takes, for every article, the version marked in force with
no end date, refuses any version that is not 'checked', leaves repealed articles out, and records the publication that
introduced the text (the code's own law, or the amending one) with its official URL and SHA-256. The corpus files are
read, never changed. The corpus commit is written into the code's notes, so the import can be traced and repeated.

Usage:  python3 tools/law/import_corpus.py <corpus folder> <corpus commit> family-code cpca
        (the corpus folder is data/law/corpus of a checkout of codex/law-corpus)
"""
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / "data/law/sources.json"
OUT = ROOT / "data/law/codes"
MONTHS = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "غشت", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"]
KINDS = {"law": "القانون", "ordinance": "الأمر"}
# The publication that enacted each code; any other publication of a text is an amendment.
BASE = {"family-code": "law-84-11", "cpca": "law-08-09"}


def arabic_date(iso: str) -> str:
    y, m, d = iso.split("-")
    return f"{int(d)} {MONTHS[int(m) - 1]} {y}"


def instrument(pub: dict) -> str:
    """«الأمر رقم 05-02 المؤرخ في 27 فبراير 2005»."""
    return f"{KINDS.get(pub['kind'], pub['kind'])} رقم {pub['number']} المؤرخ في {arabic_date(pub['date'])}"


def current(article: dict) -> dict | None:
    """The version in force today: marked in force and without an end date. None for a repealed article."""
    live = [v for v in article["versions"] if v.get("status") == "in-force" and not v.get("inForceTo")]
    return live[-1] if live else None


def build(code_id: str, folder: Path, commit: str, manifest: dict) -> dict:
    ledger = json.loads((folder / code_id / "ledger.json").read_text(encoding="utf-8"))
    raw = json.loads((folder / code_id / "articles.json").read_text(encoding="utf-8"))
    raw = raw if isinstance(raw, list) else raw["articles"]
    pubs = {p["id"]: p for p in ledger["publications"]}
    base = pubs[BASE[code_id]]
    articles, repealed = [], 0
    for item in raw:
        if item.get("availability") != "text":
            continue
        version = current(item)
        if version is None:
            repealed += 1
            continue
        state = version.get("verification", {}).get("state") if isinstance(version.get("verification"), dict) else version.get("verification")
        if state != "checked":
            raise SystemExit(f"{code_id} {item['number']}: version {version['versionId']} is '{state}', not checked; nothing written.")
        article = {"number": item["number"], "path": version.get("path") or item.get("path", []), "text": version["text"]}
        source = version.get("source", {})
        if source.get("page"):
            article["page"] = source["page"]
        pub = pubs.get(version.get("introducedBy") or source.get("publicationId"))
        if pub and pub["id"] != base["id"]:
            article["amendedBy"] = instrument(pub)
            article["source"] = {"url": pub["arabic"]["url"], "sha256": pub["arabic"]["sha256"], "retrievedOn": pub["arabic"]["retrievedOn"]}
        articles.append(article)

    amendments = [p for p in ledger["publications"] if p["id"] != base["id"] and p["kind"] in KINDS and any(
        a.get("amendedBy") == instrument(p) for a in articles)]
    latest = max([base] + amendments, key=lambda p: p["date"])
    entry = manifest[code_id]
    edition = f"{instrument(base)} (الجريدة الرسمية العدد {base['jo']['issue']} لسنة {base['date'][:4]})" + "".join(
        f"، كما عدله {instrument(p)} (الجريدة الرسمية العدد {p['jo']['issue']} لسنة {p['date'][:4]})" for p in sorted(amendments, key=lambda p: p["date"]))
    return {
        "id": code_id,
        "title": entry["title"],
        "citationName": entry["citationName"],
        "reference": entry["reference"],
        "edition": edition,
        "consolidatedAsOf": latest["date"],
        "partial": False,
        "source": {"url": base["arabic"]["url"], "sha256": base["arabic"]["sha256"], "retrievedOn": base["arabic"]["retrievedOn"]},
        "verified": False,
        "transcription": "النص الساري لكل مادة مأخوذ من مدونة النصوص الرسمية (الفرع codex/law-corpus): قرئ كل نص على صفحة الجريدة الرسمية التي نشرته وقورن بها، وطبقت عليه التعديلات المنشورة. هذه مراجعة آلية وبصرية للمصدر، لا مراجعة محام.",
        "notes": [
            f"مستورد من مدونة النصوص (codex/law-corpus، الإيداع {commit}) بالأداة tools/law/import_corpus.py: {len(articles)} مادة سارية، كل نسخة منها في حالة checked.",
            f"المواد الملغاة ({repealed}) غير مدرجة.",
            "تواريخ بدء السريان المحلية والأنظمة القديمة غير محسومة في المدونة؛ النص المدرج هو النص الساري حاليا فقط.",
        ],
        "articles": articles,
    }


def main() -> int:
    if len(sys.argv) < 4:
        print(__doc__)
        return 2
    folder, commit, codes = Path(sys.argv[1]), sys.argv[2], sys.argv[3:]
    manifest = {c["id"]: c for c in json.loads(SOURCES.read_text(encoding="utf-8"))["codes"]}
    for code_id in codes:
        code = build(code_id, folder, commit, manifest)
        target = OUT / f"{code_id}.json"
        target.write_text(json.dumps(code, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{target.relative_to(ROOT)}: {len(code['articles'])} articles in force")
    return 0


if __name__ == "__main__":
    sys.exit(main())
