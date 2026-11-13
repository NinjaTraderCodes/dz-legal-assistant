"""Writing the missing articles into law/law-delta.json (law/README.md, "Reading an article", steps 6 to 8).

An article goes into the delta only when its two readings, by two families, agree (or a third reading settled each
difference), its French check is done, and, for the Civil and Commercial Codes, the consolidated-PDF proof is recorded.
An article whose French check is still pending is listed, never written with a placeholder: the store would count a note
as a done French check.

    python delta.py add <batch> <job> [<article> ...]      -> writes or replaces the entries, prints what was skipped
"""
from __future__ import annotations

import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import consolidated  # noqa: E402
import fetch  # noqa: E402
import french  # noqa: E402
import reading as rd  # noqa: E402
import store as st  # noqa: E402

DELTA = os.path.join(st.LAW, "law-delta.json")
INVENTORY = os.path.join(st.LAW, "seed-inventory.json")
EVIDENCE = os.path.join(st.LAW, "journal-scan")
FETCHES = r"C:\Users\TheShadow\dz-core-research\oj-fetch\fetches.jsonl"


def retrieved_on(url: str, local: str) -> str:
    """The date the issue was fetched (oj-fetch's record), or the date the repository's copy was committed."""
    if os.path.exists(FETCHES):
        with open(FETCHES, encoding="utf-8") as f:
            for line in f:
                r = json.loads(line)
                if r.get("url") == url and r.get("httpStatus") == 200:
                    return r["at"][:10]
    out = subprocess.run(["git", "-C", st.REPO, "log", "-1", "--format=%cs", "--", local], capture_output=True, text=True)
    return out.stdout.strip() or "unknown"


def issue_of(url: str) -> tuple[int, int] | None:
    m = re.search(r"/([AF])(\d{4})(\d{3})\.pdf$", url or "", re.I)
    return (int(m.group(2)), int(m.group(3))) if m else None


def french_evidence(year: int, issue: int) -> dict | None:
    path = os.path.join(EVIDENCE, f"issues-{year}.jsonl")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as f:
        for line in f:
            r = json.loads(line)
            if r["issue"] == issue:
                return r
    return None


def french_check(job: dict, article: str, arabic: str, french_number: str | None = None) -> dict:
    """From 2003 on: the French article cut from the scan's kept text, and the numbers compared."""
    iss = issue_of(job["source"]["url"])
    if not iss or iss[0] < 2003:
        return {"status": "pending", "why": "French page images to be read (issue before 2003)"}
    ev = french_evidence(*iss)
    if not ev or not ev.get("readable", True):
        return {"status": "pending", "why": "the French issue is not in the scan's evidence or is unread"}
    text = french.kept(*iss)
    fr = french.cut(text, french_number or article)
    if fr is None:
        return {"status": "noFrench", "why": f"article not found in the French issue {iss[1]}/{iss[0]}",
                "source": {"url": ev["url"], "sha256": ev["sha256"], "retrievedOn": ev["readOn"]}}
    pages = text.split("\f")
    idx = text.find(fr)
    page = text[:idx].count("\f") + 1 if idx >= 0 else None
    chk = french.check(arabic, fr)
    return {"status": "equal" if chk["equal"] else "mismatch", "check": chk, "frenchPdfPage": page,
            "source": {"url": ev["url"], "sha256": ev["sha256"], "retrievedOn": ev["readOn"]}, "pages": len(pages)}


def needed_by(code: str, article: str) -> list[str]:
    with open(INVENTORY, encoding="utf-8") as f:
        inv = json.load(f)
    for r in inv["items"]:
        if r["code"] == code and r["article"] == article:
            return [("seed" if s == "brief" else s.replace("pointer:", "module:")) for s in r["sources"]]
    return ["seed"]


def text_as_of(code: str, version: str) -> str:
    """The date of the version's publication (the second pass rewrites it from the scan's textAsOf)."""
    try:
        ledger = json.loads(st._git_show(f"data/law/corpus/{code}/ledger.json"))
        for p in ledger["publications"]:
            if p["id"] == version:
                return p["date"]
    except Exception:  # noqa: BLE001 - a code the corpus does not hold (a law of its own)
        pass
    m = re.search(r"(\d{2})-(\d{2})$", version)
    return f"19{m.group(1)}" if m and int(m.group(1)) > 50 else (f"20{m.group(1)}" if m else "unknown")


def entry(batch: str, job_name: str, article: str, text_paragraphs: list[str] | None = None,
          composed_note: str | None = None) -> tuple[dict | None, str]:
    job = rd.load(rd.job_path(batch, job_name))
    folder = os.path.join(rd.JOBS, batch)
    rows = {r["article"]: r for r in rd.load(os.path.join(folder, f"{job_name}.fetch.json"))}
    row = rows.get(article)
    if not row or row["result"] not in ("twoAgree", "settled"):
        return None, f"readings: {row and row['result']}"
    s = next(a for a in rd.load(os.path.join(folder, f"{job_name}.sonnet.json"))["articles"] if a["article"] == article)
    c = next(a for a in rd.load(os.path.join(folder, f"{job_name}.codex.json"))["articles"] if a["article"] == article)
    paragraphs = text_paragraphs or s["paragraphs"]
    text = "\n".join(paragraphs)
    fr = french_check(job, article, text)
    if fr["status"] == "pending":
        return None, f"French: {fr['why']}"
    page = job["pages"][max(0, s["where"]["image"] - 1)]
    code, version = job["code"], job["version"]
    imgs = [im["sha256"] for im in job["images"]]
    meta = os.path.join(folder, f"{job_name}.sonnet.meta.json")
    proof = {
        "pdfPage": page, "dpi": job.get("dpi", 300),
        "readings": [
            {"by": "first", "model": rd.SONNET, "date": job.get("date") or "2026-11-13", "imageSha256s": imgs,
             "textSha256": rd.sha_text("\n".join(s["paragraphs"]))},
            {"by": "second", "model": f"{rd.codex_run.PINNED_VERSION} (default model)", "date": "2026-11-13",
             "imageSha256s": imgs, "textSha256": rd.sha_text("\n".join(c["paragraphs"])), "codexLog": f"{job_name}.jsonl"},
        ],
        "agreement": "twoAgree" if row["result"] == "twoAgree" else "settledByThird",
        "differences": [],
        "frenchChecked": fr["status"] == "equal",
    }
    note = []
    if fr.get("source"):
        proof["french"] = fr["source"]
    if fr.get("frenchPdfPage"):
        proof["frenchPdfPage"] = fr["frenchPdfPage"]
    if fr["status"] == "equal":
        proof["numbers"] = [str(n) for n in fr["check"]["arabicNumbers"]]
    elif fr["status"] == "mismatch":
        note.append(f"French check: numbers of the Arabic missing in the French: {fr['check']['missingInFrench']}; only in "
                    f"the French: {fr['check']['onlyInFrench']}. The Arabic stands; deadlines resting on it are interpretive.")
    else:
        note.append(f"French check: {fr['why']}.")
    if code in ("civil-code", "commercial-code"):
        cp = consolidated.check(code, text)
        proof["consolidatedPdf"] = {"status": cp["status"],
                                    "note": f"{cp['pdf']} p{cp['pdfPage']}: coverage {cp['coverage']}, {cp['stretches']} stretch(es)"}
    if composed_note:
        note.append(composed_note)
    e = {"code": code, "number": article, "text": text, "page": page,
         "source": {"url": job["source"]["url"], "sha256": job["source"]["sha256"],
                    "retrievedOn": retrieved_on(job["source"]["url"], job["source"]["localPdf"])},
         "version": version, "textAsOf": text_as_of(code, version), "proof": proof,
         "neededBy": needed_by(code, article)}
    if note:
        e["note"] = " ".join(note)
    return e, "ok"


def write(entries: list[dict]) -> None:
    with open(DELTA, encoding="utf-8") as f:
        doc = json.load(f)
    keep = [a for a in doc["articles"] if (a["code"], a["number"], a["version"]) not in
            {(e["code"], e["number"], e["version"]) for e in entries}]
    doc["articles"] = sorted(keep + entries, key=lambda a: (a["code"], st._num_key(a["number"]), a["version"]))
    with open(DELTA, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
        f.write("\n")


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[0] == "add":
        batch, job_name, arts = argv[1], argv[2], argv[3:]
        if not arts:
            arts = [r["article"] for r in fetch.compare(batch, job_name)]
        made = []
        for a in arts:
            e, why = entry(batch, job_name, a)
            print(a, why if e is None else f"written ({e['proof']['frenchChecked'] and 'French equal' or 'French: see note'}"
                  f"{', consolidated ' + e['proof']['consolidatedPdf']['status'] if 'consolidatedPdf' in e['proof'] else ''})")
            if e:
                made.append(e)
        if made:
            write(made)
    return 0


if __name__ == "__main__":
    sys.exit(main())
