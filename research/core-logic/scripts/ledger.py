"""The amendment ledger (specs/official-journal-scan.md, "From hits to a ledger"): docs/research/core-logic/law/amendment-ledger.json,
built from the triage answers of the scan's hits and Codex's corpus ledgers. The store reads it for textAsOf, ledgerRefs and
ledgerClean.

    python ledger.py build [--codes civil-code,cpca,law-22-13]     -> writes the ledger, prints the counts

Only the codes whose triage is complete (law/triage-status.json) are covered; an article of another code gets
ledgerClean false ("notCovered"), so it cannot be A (GRADES.md, condition 6).
"""
from __future__ import annotations

import argparse
import datetime as dt
import glob
import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import store as st  # noqa: E402

LEDGER = os.path.join(st.LAW, "amendment-ledger.json")
EVIDENCE = os.path.join(st.LAW, "journal-scan")
TRIAGE = r"C:\Users\TheShadow\dz-core-research\triage"
SCAN_FROM = "2003-01-01"
ARTICLE = re.compile(r"^\s*(\d+(?:\s*مكرر(?:\s*\d+)?)?)")


def canonical(article: str) -> tuple[str, str]:
    """The canonical number and whatever followed it («32 alinéas 7-10» -> ("32", "alinéas 7-10"))."""
    m = ARTICLE.match(article.translate(str.maketrans("٠١٢٣٤٥٦٧٨٩", "0123456789")))
    if not m:
        return article.strip(), ""
    number = re.sub(r"\s+", " ", m.group(1)).strip()
    return number, article[m.end():].strip(" ,;()")


def text_id(kind: str, number: str) -> str:
    return f"{kind}-{number.strip()}".lower().replace(" ", "")


def triage_answers() -> list[tuple[str, dict]]:
    out = []
    for path in sorted(glob.glob(os.path.join(TRIAGE, "*", "*", "triage-*.json"))):
        with open(path, encoding="utf-8-sig") as f:
            out.append((os.path.relpath(path, TRIAGE).replace("\\", "/"), json.load(f)))
    return out


def corpus_ledger(code: str) -> list[dict]:
    try:
        raw = subprocess.run(["git", "show", f"{st.CORPUS_COMMIT}:data/law/corpus/{code}/ledger.json"], cwd=st.REPO,
                             capture_output=True, check=True).stdout.decode("utf-8")
    except subprocess.CalledProcessError:
        return []
    import seed  # the corrections of Codex's ledger found in step 2 (seed.LEDGER_CORRECTIONS)
    return [seed.correct_ledger(code, p) for p in json.loads(raw)["publications"]]


def scan_issues() -> list[dict]:
    out = []
    for path in sorted(glob.glob(os.path.join(EVIDENCE, "issues-*.jsonl"))):
        with open(path, encoding="utf-8") as f:
            out += [json.loads(line) for line in f if line.strip()]
    return out


def text_as_of(issues: list[dict], answers: dict[tuple[int, int], dict]) -> str | None:
    """The latest issue date such that every issue up to it is read: no error, a known date, and a readable text or a
    triage answer read from its page images (the spec's definition)."""
    issues = sorted(issues, key=lambda i: (i["year"], i["issue"]))
    last = None
    for i in issues:
        a = answers.get((i["year"], i["issue"]))
        date = i.get("issueDate") or (a or {}).get("issueDate")
        if i.get("error") or not date or (i.get("readable") is False and a is None):
            break
        last = date if last is None or date > last else last
    return last


def build(codes: list[str]) -> dict:
    answers = triage_answers()
    by_issue = {}
    for name, a in answers:
        y, n = a["issue"].split("-")[0], a["issue"].split("-")[-1]
        by_issue[(int(y), int(n))] = dict(a, _file=name)
    instruments: dict[str, dict[str, dict]] = {c: {} for c in codes}
    # the triage answers: what the scan found
    for name, a in answers:
        for t in a.get("texts", []):
            code = t.get("amends")
            if code not in instruments:
                continue
            tid = text_id(t["kind"], t["number"])
            touched = []
            for x in t.get("articlesTouched", []):
                number, detail = canonical(x["article"])
                row = {"article": number, "how": x["how"]}
                if detail:
                    row["detail"] = detail
                touched.append(row)
            year, num = a["issue"].split("-")[0], a["issue"].split("-")[-1]
            sha = next((f["sha256"] for f in a.get("files", []) if "/jo-arabe/" in f.get("url", "")), None)
            entry = instruments[code].setdefault(tid, {
                "id": tid, "kind": t["kind"], "number": t["number"], "date": t.get("date"),
                "issue": {"year": int(year), "number": int(num), "date": a.get("issueDate"), "sha256": sha},
                "pages": t.get("pdfPages", []), "articlesTouched": [], "transitional": t.get("transitional", ""),
                "foundBy": [], "triage": name})
            known = {(r["article"], r["how"]) for r in entry["articlesTouched"]}
            entry["articlesTouched"] += [r for r in touched if (r["article"], r["how"]) not in known]
            if "scan" not in entry["foundBy"]:
                entry["foundBy"].append("scan")
    # Codex's ledgers: every amending publication, before and after 2003
    for code in codes:
        for p in corpus_ledger(code):
            if not p.get("articlesTouched") or p["id"] == st.ORIGINAL.get(code):
                continue  # the original text, which Codex's ledger lists with all its articles
            entry = instruments[code].setdefault(p["id"], {
                "id": p["id"], "kind": p["kind"], "number": p["number"], "date": p["date"],
                "issue": {"year": int(p["date"][:4]) if p["jo"].get("date") is None else int(p["jo"]["date"][:4]),
                          "number": int(p["jo"]["issue"]), "date": p["jo"].get("date"),
                          "sha256": (p.get("arabic") or {}).get("sha256")},
                "pages": p["jo"].get("pages", []), "articlesTouched": [], "transitional": "", "foundBy": [],
                "triage": None})
            known = {r["article"] for r in entry["articlesTouched"]}
            for a in p["articlesTouched"]:
                number, _ = canonical(a)
                if number not in known:
                    entry["articlesTouched"].append({"article": number, "how": "touched"})
                    known.add(number)
            if "codex-ledger" not in entry["foundBy"]:
                entry["foundBy"].append("codex-ledger")
    asof = text_as_of(scan_issues(), by_issue)
    doc = {"builtOn": dt.date.today().isoformat(), "scanFrom": SCAN_FROM, "textAsOf": asof, "patternsVersion": 1,
           "codes": codes,
           "instruments": {c: sorted(v.values(), key=lambda e: (e["date"] or "", e["id"])) for c, v in instruments.items()}}
    return doc


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="ledger")
    sub = p.add_subparsers(dest="cmd", required=True)
    b = sub.add_parser("build")
    b.add_argument("--codes")
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    if a.cmd == "build":
        if a.codes:
            codes = a.codes.split(",")
        else:
            with open(os.path.join(st.LAW, "triage-status.json"), encoding="utf-8") as f:
                codes = [c for c, v in json.load(f)["codes"].items() if v.get("complete")]
        doc = build(codes)
        with open(LEDGER, "w", encoding="utf-8", newline="\n") as f:
            json.dump(doc, f, ensure_ascii=False, indent=1)
            f.write("\n")
        for c, entries in doc["instruments"].items():
            print(c, [(e["id"], len(e["articlesTouched"]), "+".join(e["foundBy"])) for e in entries])
        print("textAsOf", doc["textAsOf"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
