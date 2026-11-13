"""The seed inventory of step 2's first pass (docs/research/core-logic/law/README.md, "First pass", item 3).

The seed is every article the brief names, the "where to start" pointers of HANDBOOK section 6 (ranges included), and
every article of data/law/claims/*.json. For each article the inventory says what the store holds, whether the article
is in critical use, what must be done now (nothing, a fetch of a missing article, or a page re-reading and why), and
where it is printed (the issue and the PDF page, or how the page is still to be found).

    python seed.py inventory [--store <articles.json>] [--out <seed-inventory.json>]

The output is evidence and is committed: docs/research/core-logic/law/seed-inventory.json.
"""
from __future__ import annotations

import argparse
import glob
import hashlib
import json
import os
import re
import sys
from collections import Counter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import store as st  # noqa: E402

REPO = st.REPO
OUT = os.path.join(st.LAW, "seed-inventory.json")
LOCAL_PDFS = os.path.join(REPO, "docs", "official-law-texts")
FETCHED = r"C:\Users\TheShadow\dz-core-research\oj-pdf"

# --- the seed ---------------------------------------------------------------------------------------------------------
# Items: (code, spec, source). A spec is one article number, "a-b" (every article from a to b that a source holds, plus
# the integers in between), or "prefix:<p>" (every article of the code whose number starts with <p>).
BRIEF = [
    ("civil-code", ["101", "119", "133", "179", "180", "181", "308", "309", "312", "321"]),
    ("cpca", ["17", "32", "37-40", "426", "512", "515", "536 مكرر", "536 مكرر 4", "800", "801"]),
    ("commercial-code", ["173", "187 مكرر", "169-202"]),  # 169-202: the commercial-lease chapter
    ("family-code", ["53"]),
    ("labour-disputes", ["1-40"]),  # Law 90-04, the whole law (its last article is confirmed by the reading)
    ("labour-relations", ["73", "73-1", "73-2", "73-3", "73-4"]),
    # Law 22-13 art. 13 (entry into force of the new court rules), art. 14 (repeals) and art. 15: law/README.md.
    ("law-22-13", ["13", "14", "15"]),  # 14 repeals CPCA 32 paras 7-10 (read 13 Nov)
]

# HANDBOOK section 6, "where to start". A "?" pointer is from memory: it is seeded all the same, and marked.
POINTERS = {
    "court-sections": [("cpca", "32"), ("cpca", "423?"), ("cpca", "500"), ("cpca", "512"), ("cpca", "515"),
                       ("cpca", "531?"), ("cpca", "536 مكرر")],
    "territorial-jurisdiction": [("cpca", "37-40"), ("cpca", "426"), ("cpca", "501"), ("cpca", "45-47?")],
    "ordinary-court-exits": [("cpca", "800"), ("cpca", "801"), ("cpca", "prefix:536 مكرر"), ("cpca", "306-309?")],
    "periods": [("civil-code", "308-322"), ("cpca", "405"), ("cpca", "67-69")],
    "formal-notice": [("civil-code", "119"), ("civil-code", "179"), ("civil-code", "180"), ("civil-code", "181")],
    "labour-prelitigation": [("labour-disputes", "1-40"), ("cpca", "500-510")],
    "land-registry-publication": [("cpca", "17"), ("cpca", "519?")],
    "conciliation-mediation": [("cpca", "536 مكرر 4"), ("cpca", "439-449?"), ("cpca", "990-1005?"), ("family-code", "49?")],
    "petition-formal-elements": [("cpca", "13"), ("cpca", "14"), ("cpca", "15"), ("cpca", "17"), ("cpca", "426")],
    "commercial-lease-regimes": [("commercial-code", "169-202"), ("commercial-code", "173"), ("commercial-code", "187 مكرر"),
                                 ],  # Law 05-02: its lease articles are in 169-202 (bis numbers from the ledger)
    "proof": [("civil-code", "323-350"), ("commercial-code", "30?")],
    "accessory-requests": [("civil-code", "182"), ("civil-code", "186"), ("civil-code", "174?"), ("civil-code", "175?"),
                           ("cpca", "419"), ("cpca", "323"), ("cpca", "625?")],
}

# Modules whose rules are deadlines, prior steps, court and section rules or exits: the critical use rule.
CRITICAL_MODULES = {"court-sections", "territorial-jurisdiction", "ordinary-court-exits", "periods", "formal-notice",
                    "labour-prelitigation", "land-registry-publication", "conciliation-mediation", "commercial-lease-regimes"}
CRITICAL_ROLES = {"prerequisite", "limit", "procedure"}

# Where each code's own texts are published (Arabic issue), for codes the corpus ledger does not hold.
OTHER_SOURCES = {
    "labour-disputes": {"law-90-04": {"url": "docs/official-law-texts/90-04_Original_Version.pdf", "issue": "6/1990"}},
    "labour-relations": {"law-90-11": {"url": "docs/official-law-texts/90-11_Original_Version.pdf", "issue": "17/1990"}},
    "law-22-13": {"law-22-13": {"url": "https://www.joradp.dz/FTP/jo-arabe/2022/A2022048.pdf", "issue": "48/2022"}},
}


def num_key(n: str):
    m = re.match(r"(\d+)(?:-(\d+))?\s*(.*)", n)
    if not m:
        return (10 ** 9, 0, n)
    return (int(m.group(1)), int(m.group(2) or 0), m.group(3))


def base_int(n: str) -> int | None:
    m = re.match(r"(\d+)", n)
    return int(m.group(1)) if m else None


class Inventory:
    def __init__(self, store: st.Store, corpus: dict, ledgers: dict, claims: list[dict]):
        self.s, self.corpus, self.ledgers, self.claims = store, corpus, ledgers, claims
        self.known: dict[str, set[str]] = {}
        for (code, art) in store.any_version:
            self.known.setdefault(code, set()).add(art)
        for code, arts in corpus.items():  # reference-only corpus articles count as known numbers too
            self.known.setdefault(code, set()).update(a["canonicalNumber"] for a in arts)
        for code, pubs in ledgers.items():  # and every number an amending text of the ledger touched
            for p in pubs:
                self.known.setdefault(code, set()).update(
                    t if isinstance(t, str) else t.get("article") for t in p.get("articlesTouched", []))

    # --- expanding specs ------------------------------------------------------------------------------------------
    def expand(self, code: str, spec: str) -> list[tuple[str, bool]]:
        """-> [(article, fromMemory)]"""
        memory = spec.endswith("?")
        spec = spec.rstrip("?")
        known = self.known.get(code, set())
        if spec.startswith("prefix:"):
            p = spec.split(":", 1)[1]
            return [(a, memory) for a in sorted((a for a in known if a == p or a.startswith(p + " ")), key=num_key)]
        if spec.startswith("touched:"):
            pub = spec.split(":", 1)[1]
            arts = {t if isinstance(t, str) else t.get("article") for p in self.ledgers.get(code, []) if p["id"] == pub
                    for t in p.get("articlesTouched", [])}
            return [(a, memory) for a in sorted(arts, key=num_key)]
        m = re.fullmatch(r"(\d+)-(\d+)", spec)
        if m and code not in ("labour-relations",):  # labour-relations numbers "73-1" are numbers, not ranges
            lo, hi = int(m.group(1)), int(m.group(2))
            arts = {str(i) for i in range(lo, hi + 1)}
            arts |= {a for a in known if (base_int(a) or -1) in range(lo, hi + 1)}
            return [(a, memory) for a in sorted(arts, key=num_key)]
        return [(spec, memory)]

    def seed(self) -> dict[tuple[str, str], dict]:
        items: dict[tuple[str, str], dict] = {}

        def add(code, art, source, memory, critical, why, explicit=True):
            it = items.setdefault((code, art), {"code": code, "article": art, "sources": [], "fromMemoryOnly": True,
                                                "critical": False, "criticalWhy": [], "explicit": False})
            if source not in it["sources"]:
                it["sources"].append(source)
            it["fromMemoryOnly"] = it["fromMemoryOnly"] and memory
            it["explicit"] = it["explicit"] or explicit
            if critical:
                it["critical"] = True
                if why not in it["criticalWhy"]:
                    it["criticalWhy"].append(why)

        def is_range(code, spec):
            spec = spec.rstrip("?")
            return spec.startswith(("prefix:", "touched:")) or (
                bool(re.fullmatch(r"\d+-\d+", spec)) and code != "labour-relations")

        for code, specs in BRIEF:
            for spec in specs:
                for art, memory in self.expand(code, spec):
                    # Every article the brief names is a deadline, a prior step, a court or section rule, or an exit.
                    add(code, art, "brief", memory, True, "brief", not is_range(code, spec))
        for module, pointers in POINTERS.items():
            for code, spec in pointers:
                for art, memory in self.expand(code, spec):
                    add(code, art, f"pointer:{module}", memory, module in CRITICAL_MODULES, f"module:{module}",
                        not is_range(code, spec))
        for cl in self.claims:
            for a in cl["articles"]:
                add(a["code"], a["number"], f"claims:{cl['id']}", False, a["role"] in CRITICAL_ROLES, f"role:{a['role']}")
        return items

    # --- what the store holds, and what to do ---------------------------------------------------------------------
    def state(self, it: dict) -> dict:
        code, art = it["code"], it["article"]
        e = self.s.get(code, art)
        if e is None:
            ref = any(a["canonicalNumber"] == art and not a.get("versions") for a in self.corpus.get(code, []))
            return {"state": "missing", "referenceOnlyInCorpus": ref}
        return {"state": "repealed" if e["repealed"] else "inForce", "store": e["store"], "version": e["version"],
                "verified": e["verified"], "verifiedBy": e["verifiedBy"], "corpusState": e["corpusState"],
                "guardFlag": e["guardFlag"], "inForceNote": bool(e.get("inForceNote")), "page": e.get("page")}

    @staticmethod
    def action(it: dict, s: dict) -> dict:
        code = it["code"]
        if s["state"] == "repealed":
            return {"action": "none", "why": "repealed", "readers": []}
        if s["state"] == "missing":
            return {"action": "fetch", "why": "missing", "readers": ["claude-sonnet-5-5", "codex"]}
        if s["guardFlag"]:
            return {"action": "reread", "why": "guard", "readers": ["claude-sonnet-5-5"]}
        if code in ("cpca", "family-code"):
            return {"action": "reread", "why": "criticalUse", "readers": ["claude-sonnet-5-5"]}
        if code in ("labour-relations", "labour-disputes"):
            return {"action": "reread", "why": "labour", "readers": ["codex"]}
        if code in ("civil-code", "commercial-code") and s["store"] == "corpus":
            two = s["corpusState"] != "checked"
            return {"action": "reread", "why": "corpusOnly",
                    "readers": ["claude-sonnet-5-5", "codex"] if two else ["claude-sonnet-5-5"]}
        if s["verified"]:
            return {"action": "none", "why": s["verifiedBy"], "readers": []}
        return {"action": "reread", "why": "notVerified", "readers": ["claude-sonnet-5-5", "codex"]}

    # --- where it is printed ----------------------------------------------------------------------------------------
    def version_for_missing(self, code: str, art: str) -> str | None:
        pubs = self.ledgers.get(code, [])
        best = pubs[0]["id"] if pubs else None  # the ledger's first publication is the code itself
        for p in pubs:
            if p["kind"] == "rectification":
                continue
            if art in {t if isinstance(t, str) else t.get("article") for t in p.get("articlesTouched", [])}:
                best = p["id"]
        return best

    def anchors(self, code: str, pub: str) -> list[tuple[int, int]]:
        out = []
        for a in self.corpus.get(code, []):
            for v in a.get("versions") or []:
                src = v.get("source") or {}
                n = base_int(a["canonicalNumber"])
                if src.get("publicationId") == pub and src.get("page") and n is not None:
                    out.append((n, int(src["page"])))
        return sorted(set(out))

    def locate(self, it: dict, s: dict) -> dict | None:
        code, art = it["code"], it["article"]
        if s["state"] == "inForce":
            p = s.get("page") or {}
            return {"version": s["version"], "url": p.get("url"), "pdfPage": p.get("pdfPage"), "endPage": p.get("endPage"),
                    "method": "store"}
        if s["state"] != "missing":
            return None
        if code in OTHER_SOURCES:
            version, src = next(iter(OTHER_SOURCES[code].items()))
            lib = [(base_int(a), (self.s.get(code, a) or {}).get("page", {}).get("pdfPage"))
                   for a in self.known.get(code, set())]
            anchors = sorted((n, p) for n, p in lib if n is not None and p)
            guess = interpolate(anchors, base_int(art))
            return {"version": version, "url": src["url"], "issue": src["issue"], "pdfPage": guess, "endPage": None,
                    "method": "anchors" if guess else "textLayer"}
        version = self.version_for_missing(code, art)
        pub = next((p for p in self.ledgers.get(code, []) if p["id"] == version), None)
        url = ((pub or {}).get("arabic") or {}).get("url")
        anchors = self.anchors(code, version) if version else []
        guess = interpolate(anchors, base_int(art)) if len(anchors) >= 3 else None
        year = int(((pub or {}).get("date") or "0000")[:4])
        method = "anchors" if guess else ("textLayer" if year >= 2003 else "pageMap")
        return {"version": version, "url": url, "issue": ((pub or {}).get("jo") or {}).get("issue"), "pdfPage": guess,
                "endPage": None, "method": method, "anchorCount": len(anchors)}


def interpolate(anchors: list[tuple[int, int]], n: int | None) -> list[int] | None:
    """The page or pair of pages where article n should be, from (article, page) anchors of the same issue."""
    if n is None or not anchors:
        return None
    below = [a for a in anchors if a[0] <= n]
    above = [a for a in anchors if a[0] >= n]
    if not below or not above:
        return None
    lo, hi = below[-1], above[0]
    if lo[1] == hi[1]:
        return [lo[1]]
    if lo[0] == hi[0]:
        return [lo[1]]
    p = lo[1] + (hi[1] - lo[1]) * (n - lo[0]) / (hi[0] - lo[0])
    a = int(p)
    return [a] if a == p or a + 1 > hi[1] else [a, a + 1]


def local_pdf(url: str | None, sha_index: dict) -> str | None:
    """The copy in docs/official-law-texts/ of an issue, matched by the corpus ledger's SHA-256."""
    if not url:
        return None
    if url.startswith("docs/"):
        return url
    if url in sha_index:
        return sha_index[url]
    # an issue downloaded through oj-fetch for the seed (kept until its readings are recorded, then deleted)
    fetched = os.path.join(FETCHED, os.path.basename(url))
    return fetched if os.path.exists(fetched) else None


# Errors found in the corpus ledger's touched lists (13 Nov 2026), settled in the official consolidated Civil Code of
# 2007 (tier 2, docs/official-law-texts/75-58_wipo_Version.pdf pp 57-58, footnotes): the seed uses the corrected lists;
# the corpus itself is not changed (a corpus correction for the owner to pass on).
LEDGER_CORRECTIONS = {
    ("civil-code", "law-88-14"): {"remove": ["324 مكرر"],
                                  "why": "no article «324 مكرر» exists: Law 88-14 added 324 مكرر 1 to 324 مكرر 7"},
    ("civil-code", "law-05-10"): {"replace": {"324 مكرر 4": "324 مكرر 3"},
                                  "why": "Law 05-10 amended 324 مكرر 3 (footnote 2 p58); 324 مكرر 4 is Law 88-14's"},
}


def correct_ledger(code: str, pub: dict) -> dict:
    fix = LEDGER_CORRECTIONS.get((code, pub["id"]))
    if not fix:
        return pub
    touched = []
    for t in pub.get("articlesTouched", []):
        a = t if isinstance(t, str) else t.get("article")
        if a in fix.get("remove", []):
            continue
        touched.append(fix.get("replace", {}).get(a, a))
    return dict(pub, articlesTouched=touched)


def load_all(store_path: str):
    s = st.Store(store_path)
    corpus, ledgers, sha_index = {}, {}, {}
    files = {}
    for f in glob.glob(os.path.join(LOCAL_PDFS, "*.pdf")):
        with open(f, "rb") as fh:
            files[hashlib.sha256(fh.read()).hexdigest()] = "docs/official-law-texts/" + os.path.basename(f)
    for code in st.CORPUS_CODES:
        corpus[code] = json.loads(st._git_show(f"data/law/corpus/{code}/articles.json"))["articles"]
        ledgers[code] = sorted((correct_ledger(code, p) for p in
                                json.loads(st._git_show(f"data/law/corpus/{code}/ledger.json"))["publications"]),
                               key=lambda p: p["date"])
        for p in ledgers[code]:
            a = p.get("arabic") or {}
            if a.get("sha256") in files:
                sha_index[a["url"]] = files[a["sha256"]]
    claims = []
    for f in sorted(glob.glob(os.path.join(REPO, "data", "law", "claims", "*.json"))):
        with open(f, encoding="utf-8") as fh:
            claims += json.load(fh)["claims"]
    return s, corpus, ledgers, claims, sha_index


def inventory(store_path: str = st.DEFAULT_OUT, out: str = OUT) -> dict:
    s, corpus, ledgers, claims, sha_index = load_all(store_path)
    inv = Inventory(s, corpus, ledgers, claims)
    items = inv.seed()
    result = []
    for key in sorted(items, key=lambda k: (k[0], num_key(k[1]))):
        it = items[key]
        s_ = inv.state(it)
        act = inv.action(it, s_)
        loc = inv.locate(it, s_)
        s_.pop("page", None)  # given under "where"
        if loc:
            loc["localPdf"] = local_pdf(loc.get("url"), sha_index)
        # A range fills in numbers that no source holds: such a missing number may not exist at all (the reading says).
        may_not_exist = s_["state"] == "missing" and not it.pop("explicit")
        it.pop("explicit", None)
        older = None
        if it["code"] == "commercial-code" and (base_int(it["article"]) or 0) in range(169, 203) and loc                 and loc.get("version") == "law-05-02" and "مكرر" not in it["article"]:
            # The 2005 reform did not reach leases made before it: their 1975 wording is read too (two wordings).
            older = "ordinance-75-59"
        result.append({**it, **s_, **act, "mayNotExist": may_not_exist, "olderWording": older, "where": loc})
    counts = Counter((r["code"], r["action"], r["why"]) for r in result)
    doc = {
        "schemaVersion": "1.0",
        "builtFrom": {"store": s.inputs, "storeFile": os.path.basename(store_path), "claimsFiles": "data/law/claims/*.json",
                      "corpusCommit": st.CORPUS_COMMIT},
        "counts": {"articles": len(result),
                   "byAction": dict(Counter(r["action"] for r in result)),
                   "byCodeActionWhy": {f"{c}|{a}|{w}": n for (c, a, w), n in sorted(counts.items())},
                   "critical": sum(1 for r in result if r["critical"]),
                   "mayNotExist": sum(1 for r in result if r["mayNotExist"])},
        "items": result,
    }
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
        f.write("\n")
    return doc


def plan_pairs(items: list[dict]) -> list[dict]:
    """Group articles of one issue into runs of a page pair [p, p+1]. Each article is given with its first page; the
    pair also holds a continuation onto the next page, which a page record may miss (CPCA 419 on 13 November)."""
    by_page: dict[int, list[dict]] = {}
    for it in items:
        by_page.setdefault(it["page"], []).append(it)
    runs, left = [], {p: list(v) for p, v in by_page.items()}
    for p in sorted(by_page):
        here = left.get(p) or []
        if not here:
            continue
        arts = [it["article"] for it in here]
        left[p] = []
        # the next page's articles join this pair only if they end on it; one that runs on starts its own pair
        nxt = left.get(p + 1) or []
        fits = [it for it in nxt if (it.get("end") or it["page"]) <= p + 1]
        arts += [it["article"] for it in fits]
        left[p + 1] = [it for it in nxt if it not in fits]
        runs.append({"pages": [p, p + 1], "articles": sorted(set(arts), key=num_key)})
    return runs


def page_count(pdf: str) -> int:
    import subprocess
    exe = r"C:\Users\TheShadow\AppData\Local\Programs\MiKTeX\miktex\bin\x64\pdfinfo.exe"
    out = subprocess.run([exe, pdf], capture_output=True, text=True).stdout
    return int(re.search(r"Pages:\s+(\d+)", out).group(1))


def jobs(inv_path: str, batch: str, why: str, codes: list[str], skip: set | None = None) -> list[dict]:
    """Page-pair jobs for the re-readings of one kind, from the inventory (articles with a known page only)."""
    import reading
    with open(inv_path, encoding="utf-8") as f:
        inv = json.load(f)
    groups: dict[tuple, list[dict]] = {}
    for r in inv["items"]:
        if r["action"] != "reread" or r["why"] != why or r["code"] not in codes:
            continue
        if (r["code"], r["article"]) in (skip or set()):
            continue
        w = r["where"] or {}
        page = w.get("pdfPage")
        if isinstance(page, list):
            page = page[0]
        if not page or not w.get("localPdf"):
            continue
        groups.setdefault((r["code"], w["version"], w["localPdf"], w.get("url")), []).append(
            {"article": r["article"], "page": int(page), "end": w.get("endPage")})
    made = []
    for (code, version, pdf, url), items in sorted(groups.items()):
        pdf_abs = pdf if os.path.isabs(pdf) else os.path.join(REPO, pdf)
        last = page_count(pdf_abs)
        for run in plan_pairs(items):
            pages = [p for p in run["pages"] if p <= last]
            stem = os.path.splitext(os.path.basename(pdf))[0]
            name = f"{code}-{stem}-p{pages[0]:03d}"
            made.append(reading.make_job(batch, name, code, version, pdf, pages, run["articles"], "reread", why,
                                         source_url=url))
    return made


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(prog="seed")
    sub = p.add_subparsers(dest="cmd", required=True)
    i = sub.add_parser("inventory")
    i.add_argument("--store", default=st.DEFAULT_OUT)
    i.add_argument("--out", default=OUT)
    j = sub.add_parser("jobs")
    j.add_argument("batch"); j.add_argument("--why", required=True); j.add_argument("--codes", required=True)
    j.add_argument("--inventory", default=OUT)
    j.add_argument("--skip", default="", help="code:article,... already read")
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    if a.cmd == "jobs":
        skip = {tuple(x.split(":", 1)) for x in a.skip.split(",") if x}
        for d in jobs(a.inventory, a.batch, a.why, a.codes.split(","), skip):
            print(d["job"], d["pages"], ",".join(d["articles"]))
        return 0
    doc = inventory(a.store, a.out)
    print(json.dumps(doc["counts"], ensure_ascii=False, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
