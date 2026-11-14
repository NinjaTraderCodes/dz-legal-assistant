"""The article store (docs/research/core-logic/specs/article-store.md).

One read-only view over the texts a rule may quote: the app's library, Codex's corpus at a pinned commit, the law delta,
the fresh page readings and the amendment ledger. Keyed by (code, article, version). Built by script, outside the
repository, from pinned inputs, so that it can be rebuilt to the same hash.

    python store.py build [--out C:\\Users\\TheShadow\\dz-core-research\\store\\articles.json]
    python store.py get <code> <article> [<version>]
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import textcmp  # noqa: E402

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
LAW = os.path.join(REPO, "docs", "research", "core-logic", "law")
CORPUS_COMMIT = "9b60990"
CORPUS_CODES = ("civil-code", "commercial-code", "cpca", "family-code", "civil-status")
LIBRARY_CODES = ("civil-code", "commercial-code", "cpca", "family-code", "labour-relations", "labour-disputes")
ORIGINAL = {"civil-code": "ordinance-75-58", "commercial-code": "ordinance-75-59", "cpca": "law-08-09",
            "family-code": "law-84-11", "labour-relations": "law-90-11", "labour-disputes": "law-90-04"}
DEFAULT_OUT = r"C:\Users\TheShadow\dz-core-research\store\articles.json"
CLAUDE, CODEX = "claude", "codex"


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def sha256_file(path: str) -> str | None:
    if not os.path.exists(path):
        return None
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def _git_show(path: str) -> str:
    out = subprocess.run(["git", "-C", REPO, "show", f"{CORPUS_COMMIT}:{path}"], capture_output=True, check=True)
    return out.stdout.decode("utf-8")


def _json(path: str, default):
    if not os.path.exists(path):
        return default
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def _jsonl(path: str) -> list[dict]:
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]


class Inputs:
    """Everything the store is built from. The tests give fixtures; `load()` reads the real, pinned inputs."""

    def __init__(self, library: dict, corpus: dict, delta: dict, unused: dict, readings: list[dict], ledger: dict | None,
                 hashes: dict):
        self.library, self.corpus, self.delta, self.unused = library, corpus, delta, unused
        self.readings, self.ledger, self.hashes = readings, ledger, hashes

    @staticmethod
    def load() -> "Inputs":
        library, lib_hash = {}, hashlib.sha256()
        for code in LIBRARY_CODES:
            path = os.path.join(REPO, "data", "law", "codes", f"{code}.json")
            with open(path, "rb") as f:
                raw = f.read()
            lib_hash.update(raw)
            library[code] = json.loads(raw.decode("utf-8"))
        corpus = {}
        for code in CORPUS_CODES:
            corpus[code] = {
                "articles": json.loads(_git_show(f"data/law/corpus/{code}/articles.json")),
                "ledger": json.loads(_git_show(f"data/law/corpus/{code}/ledger.json")),
                "report": json.loads(_git_show(f"data/law/corpus/{code}/report.json")),
            }
        paths = {k: os.path.join(LAW, f) for k, f in
                 (("delta", "law-delta.json"), ("unused", "unused-articles.json"), ("readings", "readings.jsonl"),
                  ("ledger", "amendment-ledger.json"))}
        hashes = {"library": lib_hash.hexdigest(), "corpus": CORPUS_COMMIT}
        hashes.update({k: sha256_file(p) for k, p in paths.items()})
        empty = {"articles": [], "corpusCorrections": [], "libraryCorrections": []}
        return Inputs(library, corpus, _json(paths["delta"], empty), _json(paths["unused"], empty),
                      _jsonl(paths["readings"]), _json(paths["ledger"], None), hashes)


def _version_from_amended_by(code: str, amended_by: str) -> str:
    """'القانون رقم 91-29 ...' -> 'law-91-29'; 'الأمر رقم 96-21 ...' -> 'ordinance-96-21'."""
    m = re.search(r"(\d{2}-\d{2,3})", amended_by or "")
    if not m:
        return ORIGINAL[code]
    kind = "ordinance" if "الأمر" in amended_by or "أمر" in amended_by else \
        "legislative-decree" if "مرسوم تشريعي" in amended_by else "law"
    return f"{kind}-{m.group(1)}"


def _touched(pub: dict) -> set[str]:
    return {t if isinstance(t, str) else t.get("article", "") for t in pub.get("articlesTouched", [])}


class Builder:
    def __init__(self, inputs: Inputs):
        self.i = inputs
        self.ledgers = {c: sorted(v["ledger"]["publications"], key=lambda p: p["date"]) for c, v in inputs.corpus.items()}
        self.pub_date = {(c, p["id"]): p["date"] for c, pubs in self.ledgers.items() for p in pubs}
        self.unsettled = {u["id"]: u["reason"] for c, v in inputs.corpus.items() for u in v["report"].get("unsettled", [])}

    # --- the library's one wording: which version is it? -----------------------------------------------------------
    def library_version(self, code: str, article: dict, corpus_article: dict | None) -> str:
        if code in ("cpca", "family-code") and corpus_article and corpus_article.get("versions"):
            return corpus_article["versions"][-1]["introducedBy"]
        if code in ("labour-relations", "labour-disputes"):
            return _version_from_amended_by(code, article.get("amendedBy", ""))
        lib = self.i.library[code]
        cutoff = lib.get("consolidatedAsOf") or "2007-01-01"
        best = ORIGINAL[code]
        for pub in self.ledgers.get(code, []):
            if pub["date"] <= cutoff and article["number"] in _touched(pub) and pub["kind"] != "rectification":
                best = pub["id"]
        return best

    def corpus_page(self, code: str, version: dict) -> dict | None:
        """Where the version is printed: the Arabic issue of its publication and the PDF page (from the corpus's source)."""
        src = version.get("source") or {}
        pub = next((p for p in self.ledgers.get(code, []) if p["id"] == src.get("publicationId")), None)
        if not pub:
            return None
        arabic = pub.get("arabic") or {}
        return {"url": arabic.get("url"), "sha256": arabic.get("sha256"), "pdfPage": src.get("page"), "endPage": src.get("endPage"),
                "issue": (pub.get("jo") or {}).get("issue"), "issueDate": (pub.get("jo") or {}).get("date")}

    def library_page(self, code: str, article: dict) -> dict:
        """The library's own source: the code's document and the article's page in it (labour laws: the local extract)."""
        lib = self.i.library[code]
        src = article.get("source") or lib.get("source") or {}
        return {"url": src.get("url"), "sha256": src.get("sha256"), "pdfPage": article.get("page"), "endPage": None}

    def guard(self, code: str, number: str, version: dict) -> bool:
        start = self.pub_date.get((code, version["introducedBy"]), "")
        return any(p["date"] > start and p["kind"] != "rectification" and number in _touched(p)
                   for p in self.ledgers.get(code, []))

    # --- readings -------------------------------------------------------------------------------------------------
    def readings_for(self, code: str, article: str, version: str, text_sha: str) -> list[dict]:
        return [r for r in self.i.readings
                if r["code"] == code and r["article"] == article and r["version"] == version
                and r.get("storeTextSha256") == text_sha and r.get("result") == "agrees"]

    def verify(self, e: dict, readings: list[dict], corpus_checker: str) -> tuple[bool, str | None]:
        code, store = e["code"], e["store"]
        families = {r["family"] for r in readings}
        other = CODEX if corpus_checker == CLAUDE else CLAUDE
        if e["repealed"]:
            return False, None
        if store == "delta":
            return e.get("_deltaVerified", False), ("twoFamilies" if e.get("_deltaVerified") else None)
        if code in ("civil-code", "commercial-code"):
            if store == "library":
                if e["_libraryProof"]:
                    if e["agreement"] is None:
                        return True, "libraryProof"
                    if e["agreement"] in ("identical", "level2", "level2o", "level2s") and e["corpusState"] == "checked":
                        return True, "libraryProofAndCorpus"
                    if readings:
                        return True, "libraryProofAndReread"
                return False, None
            if e["guardFlag"]:
                return False, None
            if e["corpusState"] == "checked" and other in families:
                return True, "corpusAndReread"
            if e["corpusState"] in ("partly-checked", "unsettled") and {CLAUDE, CODEX} <= families:
                return True, "corpusAndTwoReadings"
            return False, None
        if code in ("cpca", "family-code"):
            if e["guardFlag"] or e["corpusState"] != "checked":
                return False, None
            return True, ("ojPageReread" if other in families else "corpusChecked")
        if code in ("labour-relations", "labour-disputes"):
            return (CODEX in families), ("libraryAndCodexReread" if CODEX in families else None)
        return False, None

    # --- the entries ----------------------------------------------------------------------------------------------
    def build(self) -> list[dict]:
        entries: dict[tuple, dict] = {}
        lib_by = {c: {a["number"]: a for a in d["articles"]} for c, d in self.i.library.items()}
        cor_by = {c: {a["canonicalNumber"]: a for a in v["articles"]["articles"]} for c, v in self.i.corpus.items()}

        for code, arts in cor_by.items():
            for number, art in arts.items():
                versions = art.get("versions") or []
                if not versions:
                    continue  # reference-only: no text and no status here
                last = versions[-1]
                lib_art = lib_by.get(code, {}).get(number)
                lib_version = self.library_version(code, lib_art, art) if lib_art else None
                for v in versions:
                    is_last = v is last
                    text_from_library = lib_art is not None and lib_version == v["introducedBy"] and v["status"] != "repealed"
                    paragraphs = lib_art["text"].split("\n") if text_from_library else list(v.get("paragraphs") or [])
                    agreement = None
                    if text_from_library:
                        lv = textcmp.level("\n".join(paragraphs), "\n".join(v.get("paragraphs") or []))
                        agreement = {"1": "identical", "2": "level2", "2o": "level2o", "2s": "level2s", "3": "level3"}.get(lv, "differs")
                    e = {
                        "code": code, "article": number, "version": v["introducedBy"],
                        "inForce": is_last and v["status"] != "repealed",
                        "repealed": v["status"] == "repealed",
                        "statusFrom": "corpus",
                        "store": "library" if text_from_library else "corpus",
                        "tier": 2 if text_from_library and code in ("civil-code", "commercial-code") else 1,
                        "path": (lib_art or {}).get("path") or v.get("path") or art.get("path") or [],
                        "paragraphs": [] if v["status"] == "repealed" else paragraphs,
                        "alsoIn": ["corpus"] if text_from_library else (["library"] if lib_art and lib_version == v["introducedBy"] else []),
                        "agreement": agreement,
                        "corpusState": (v.get("verification") or {}).get("state"),
                        "guardFlag": is_last and self.guard(code, number, v),
                        "inForceNote": ({"lead": self.unsettled[v["versionId"]], "source": f"corpus report @ {CORPUS_COMMIT}"}
                                        if v.get("versionId") in self.unsettled else None),
                        "frenchCheck": None,
                        "page": self.corpus_page(code, v),
                        "_libraryProof": text_from_library and self.i.library[code].get("check") == "officialPdfText",
                        # The corpus's checker is taken to be Codex; when a version's checks name a Claude reading, Claude.
                        "_checker": CLAUDE if any("Claude" in (c.get("method") or "")
                                                  for c in (v.get("verification") or {}).get("checks", [])) else CODEX,
                    }
                    entries[(code, number, e["version"])] = e

        # library articles the corpus does not hold with text (labour laws, library-only Civil and Commercial articles)
        for code, arts in lib_by.items():
            for number, lib_art in arts.items():
                cor = cor_by.get(code, {}).get(number)
                if cor and cor.get("versions"):
                    continue
                version = self.library_version(code, lib_art, cor)
                entries[(code, number, version)] = {
                    "code": code, "article": number, "version": version, "inForce": True, "repealed": False,
                    "statusFrom": "library", "store": "library",
                    "tier": 2 if code in ("civil-code", "commercial-code") else 1,
                    "path": lib_art.get("path") or (cor or {}).get("path") or [],
                    "paragraphs": lib_art["text"].split("\n"), "alsoIn": [], "agreement": None,
                    "corpusState": None, "guardFlag": False, "inForceNote": None, "frenchCheck": None,
                    "page": self.library_page(code, lib_art),
                    "_libraryProof": self.i.library[code].get("check") == "officialPdfText", "_checker": CLAUDE,
                }

        # the delta (then the articles moved out of it, which the store still serves)
        for source in (self.i.delta, self.i.unused):
            for d in source.get("articles", []):
                proof = d.get("proof") or {}
                readings = proof.get("readings") or []
                two = len({("codex" if "codex" in (r.get("model") or "") else "claude") for r in readings}) >= 2
                french_ok = bool(proof.get("french")) or bool(d.get("note"))
                pdf_ok = d["code"] not in ("civil-code", "commercial-code") or bool(proof.get("consolidatedPdf"))
                fc = "equal" if proof.get("frenchChecked") else ("mismatch" if proof.get("french") else "noFrench")
                key = (d["code"], d["number"], d["version"])
                entries[key] = {
                    "code": d["code"], "article": d["number"], "version": d["version"],
                    "inForce": not d.get("supersededWording") and not d.get("repealed"),
                    "repealed": bool(d.get("repealed")), "statusFrom": "delta", "store": "delta", "tier": 1,
                    "path": d.get("path") or [], "paragraphs": d["text"].split("\n"), "alsoIn": [], "agreement": None,
                    "corpusState": None, "guardFlag": False, "inForceNote": None, "frenchCheck": fc,
                    "page": {"url": (d.get("source") or {}).get("url"), "sha256": (d.get("source") or {}).get("sha256"),
                             "pdfPage": d.get("page"), "endPage": None},
                    "_deltaVerified": two and french_ok and pdf_ok and proof.get("agreement") in ("twoAgree", "settledByThird"),
                    "_libraryProof": False, "_checker": CLAUDE,
                }
                if not d.get("supersededWording"):
                    for k, e in entries.items():  # the delta's wording is the one in force
                        if k[:2] == key[:2] and k != key and e["store"] != "delta":
                            e["inForce"] = False
        # corrections of status
        for c in self.i.delta.get("libraryCorrections", []) + self.i.delta.get("corpusCorrections", []):
            if c.get("kind") == "wrongStatus":
                # The schema does not yet say which status the correction sets: refuse rather than ignore it.
                raise NotImplementedError("a wrongStatus correction exists: extend the store and the schema first")

        # corrections of wording: the library's text is not what the page prints (settled from the page, confirmed by
        # the other family and the French edition); the store serves the page's wording, so that quotes are right and
        # the re-readings are compared with it again
        for c in self.i.delta.get("libraryCorrections", []):
            if c.get("kind") != "wording":
                continue
            hits = [e for e in entries.values() if e["code"] == c["code"] and e["article"] == c["article"]
                    and e["store"] == "library"]
            if not hits or not c.get("exactWording"):
                raise ValueError(f"a wording correction of {c['code']} {c['article']} has no library entry or no wording")
            for e in hits:
                e["paragraphs"] = c["exactWording"].split("\n")
                e["correctedFromLibrary"] = True

        # verification, the ledger, and the final shape
        out = []
        for key in sorted(entries, key=lambda k: (k[0], _num_key(k[1]), k[2])):
            e = entries[key]
            text_sha = sha256_text("\n".join(e["paragraphs"]))
            readings = self.readings_for(e["code"], e["article"], e["version"], text_sha)
            e["verified"], e["verifiedBy"] = self.verify(e, readings, e["_checker"])
            e["readings"] = len(readings)
            e.update(self.ledger_fields(e))
            e["textSha256"] = text_sha
            for k in [k for k in e if k.startswith("_")]:
                del e[k]
            out.append(e)
        return out

    def ledger_fields(self, e: dict) -> dict:
        led = self.i.ledger
        if not led:
            return {"textAsOf": None, "ledgerRefs": [], "ledgerClean": False, "ledgerReason": "scanNotRun"}
        refs = [f"{e['code']}/{x['id']}" for x in led["instruments"].get(e["code"], [])
                if any(t["article"] == e["article"] for t in x.get("articlesTouched", []))]
        latest = refs[-1].split("/", 1)[1] if refs else None
        clean = (latest is None or latest == e["version"]) and e["code"] in led["instruments"]
        return {"textAsOf": led.get("textAsOf"), "ledgerRefs": refs, "ledgerClean": clean,
                "ledgerReason": None if clean else ("touchedLater" if refs else "notCovered")}


def _num_key(number: str):
    m = re.match(r"(\d+)(.*)", number)
    return (int(m.group(1)), m.group(2)) if m else (10 ** 9, number)


def build(out: str = DEFAULT_OUT, inputs: Inputs | None = None) -> dict:
    inputs = inputs or Inputs.load()
    entries = Builder(inputs).build()
    doc = {"schemaVersion": "1.0", "inputs": inputs.hashes, "entries": entries}
    data = json.dumps(doc, ensure_ascii=False, indent=1, sort_keys=False)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(data)
    return {"path": out, "sha256": sha256_text(data), "entries": len(entries)}


class Store:
    """Lookups for the other scripts."""

    def __init__(self, path: str = DEFAULT_OUT):
        with open(path, encoding="utf-8") as f:
            doc = json.load(f)
        self.inputs = doc["inputs"]
        self.by_key = {(e["code"], e["article"], e["version"]): e for e in doc["entries"]}
        self.in_force = {}
        for e in doc["entries"]:
            if e["inForce"]:
                self.in_force[(e["code"], e["article"])] = e
        self.any_version = {}
        for e in doc["entries"]:
            self.any_version.setdefault((e["code"], e["article"]), []).append(e)

    def get(self, code: str, article: str, version: str | None = None) -> dict | None:
        if version:
            return self.by_key.get((code, article, version))
        e = self.in_force.get((code, article))
        if e:
            return e
        repealed = [x for x in self.any_version.get((code, article)) or [] if x["repealed"]]
        return repealed[0] if repealed else None

    def chapter(self, code: str, path: list[str]) -> list[dict]:
        """Every in-force entry of the code whose path equals `path` (the whole chapter of an article)."""
        return [e for e in self.in_force.values() if e["code"] == code and e["path"] == path]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="store")
    sub = parser.add_subparsers(dest="cmd", required=True)
    b = sub.add_parser("build"); b.add_argument("--out", default=DEFAULT_OUT)
    g = sub.add_parser("get"); g.add_argument("code"); g.add_argument("article"); g.add_argument("version", nargs="?")
    g.add_argument("--store", default=DEFAULT_OUT)
    args = parser.parse_args(argv)
    if args.cmd == "build":
        print(json.dumps(build(args.out)))
        return 0
    e = Store(args.store).get(args.code, args.article, args.version)
    sys.stdout.reconfigure(encoding="utf-8")
    print(json.dumps(e, ensure_ascii=False, indent=1) if e else "not in the store")
    return 0 if e else 1


if __name__ == "__main__":
    sys.exit(main())
