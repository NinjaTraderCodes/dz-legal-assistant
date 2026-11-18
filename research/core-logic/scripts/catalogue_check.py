"""The catalogue check of a claim type (specs/unit-driver.md, "The catalogue route for claim types").

    python catalogue_check.py <claim> [--store <articles.json>] [--final <rules\\final>] [--texts <texts.json>]
                                      [--out <rules\\reports\\<claim>.catalogue-check.json>]

The owner's decision of 18 November 2026: for the remaining claim types, compare the app's catalogue (data/law/claims)
with the official text and the final modules by script, and derive only what is missing or wrong. This script is the
"by script" half: for each article the catalogue names, what the store holds (present, in force or repealed, verified
and its proof label, how many paragraphs) and which final modules already rest items on it; for each prerequisite and
required document, which final module items already carry it. It reads facts and copies them; it never decides what
the law says. Deterministic: the same catalogue, store and finals give the same file.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import records as rc  # noqa: E402
import store as st  # noqa: E402

CL = os.path.join(st.REPO, "docs", "research", "core-logic")

# Words that carry no subject: two documents that share only these are not the same document.
_STOP = {"من", "في", "على", "او", "عن", "الي", "ان", "ما", "مع", "قبل", "بعد", "عدم", "التي", "الذي", "اذا", "ذلك", "هذا",
         "كل", "اي", "عند", "به", "بها", "له", "لها", "ثم", "وان", "ولا", "لا", "غير"}
_DIACRITICS = re.compile("[ً-ْـ]")


def words(text: str) -> set[str]:
    """The content words of an Arabic phrase, normalised so that «الإعذار» and «إعذار» meet: no diacritics, one alif,
    the article and a leading conjunction or preposition stripped, stop words and words under three letters dropped."""
    text = _DIACRITICS.sub("", text or "")
    text = re.sub("[أإآ]", "ا", text).replace("ة", "ه").replace("ى", "ي")
    out = set()
    for w in re.findall(r"[ء-ي]+", text):
        for pre in ("وال", "بال", "كال", "فال", "لل", "ال"):
            if w.startswith(pre) and len(w) - len(pre) >= 3:
                w = w[len(pre):]
                break
        if len(w) >= 3 and w not in _STOP:
            out.add(w)
    return out


def shared(a: str, b: str) -> tuple[list[str], str | None]:
    """The content words two phrases share, and how strongly: "strong" when there are at least two, or every content
    word of the shorter phrase when it has only one; "weak" for a single shared word («محضر الإعذار» against a module's
    «الإعذار نفسه» shares only «اعذار», and is still worth showing the deriver); None when nothing is shared."""
    wa, wb = words(a), words(b)
    common = sorted(wa & wb)
    if not common:
        return [], None
    return common, "strong" if len(common) >= min(2, len(wa), len(wb)) else "weak"


def _status(store, code: str, article: str) -> dict:
    e = store.get(code, article)
    if e is None:
        others = (getattr(store, "any_version", {}) or {}).get((code, article)) or []
        if others:  # only a superseded wording: the article is in the store but no version is in force
            return {"inStore": True, "status": "notInForce", "versions": sorted(x["version"] for x in others)}
        return {"inStore": False, "status": "notInStore"}
    return {"inStore": True, "status": "repealed" if e.get("repealed") else ("inForce" if e.get("inForce") else "notInForce"),
            "version": e.get("version"), "verified": bool(e.get("verified")), "verifiedBy": e.get("verifiedBy"),
            "proof": st.proof_of(e), "paragraphs": len(e.get("paragraphs") or []), "tier": e.get("tier"),
            "frenchCheck": e.get("frenchCheck"), "ledgerClean": e.get("ledgerClean"),
            "ledgerReason": e.get("ledgerReason")}


def module_citations(finals: dict[str, dict]) -> dict[tuple[str, str], list[dict]]:
    """(code, article) -> the final module items that rest on it: [{module, statements, items}]."""
    out: dict[tuple[str, str], dict[str, dict]] = {}
    for mid in sorted(finals):
        rec = finals[mid]
        stmts = rc.statements(rec)
        for s in stmts.values():
            key = (s["cite"]["code"], s["cite"]["article"])
            row = out.setdefault(key, {}).setdefault(mid, {"module": mid, "statements": [], "items": []})
            row["statements"].append(s["id"])
        for kind, x in rc.items(rec):
            if kind == "statement":
                continue
            for b in rc.basis_ids(x):
                if b in stmts:
                    key = (stmts[b]["cite"]["code"], stmts[b]["cite"]["article"])
                    row = out[key][mid]
                    label = f"{kind}:{rc.item_id(kind, x)}"
                    if label not in row["items"]:
                        row["items"].append(label)
    return {k: [dict(r, statements=sorted(r["statements"]), items=sorted(r["items"])) for r in v.values()]
            for k, v in out.items()}


def _text_of(x: dict, *keys: str) -> str:
    for k in keys:
        v = x.get(k)
        if isinstance(v, dict) and v.get("ar"):
            return v["ar"]
    return ""


def module_matches(text: str, finals: dict[str, dict], pools: tuple[str, ...]) -> list[dict]:
    """The final module items of the given pools (priorSteps, documents) whose Arabic name or message shares content
    words with `text`, with the words shared and the strength (so the deriver can judge; a lead, not a finding)."""
    out = []
    for mid in sorted(finals):
        for pool in pools:
            for x in finals[mid].get(pool, []):
                common, strength = shared(text, _text_of(x, "name", "message"))
                if strength:
                    out.append({"module": mid, "item": f"{pool}:{x['id']}", "sharedWords": common, "strength": strength})
    return out


def check(claim: dict, store, finals: dict[str, dict], texts: dict | None = None) -> dict:
    """The catalogue entry against the store and the final modules. `texts` is the unit's texts file (stage 0)."""
    cites = module_citations(finals)
    given = {(ch["code"], a) for ch in (texts or {}).get("chapters", []) for a in ch.get("articles", [])}
    articles = []
    for a in claim.get("articles", []):
        key = (a["code"], a["number"])
        row = {"code": a["code"], "number": a["number"], "role": a.get("role"), "required": bool(a.get("required")),
               **_status(store, *key), "inModules": cites.get(key, [])}
        if texts is not None:
            row["inUnitTexts"] = key in given
        articles.append(row)
    prerequisites = []
    for p in claim.get("prerequisites", []):
        prerequisites.append({"from": "prerequisites", "text": p, "inModules": module_matches(
            p.get("ar", ""), finals, ("priorSteps",))})
    for a in claim.get("articles", []):
        if a.get("role") == "prerequisite":
            steps = [r for r in cites.get((a["code"], a["number"]), []) if any(i.startswith("priorStep:") for i in r["items"])]
            prerequisites.append({"from": "articles", "text": a.get("why"), "article": f"{a['code']} {a['number']}",
                                  "inModules": [{"module": r["module"], "items": [i for i in r["items"]
                                                                                  if i.startswith("priorStep:")]}
                                                for r in steps]})
    documents = [{"document": d, "inModules": module_matches(d, finals, ("documents",))}
                 for d in claim.get("requiredDocuments", [])]
    summary = {"articles": len(articles),
               "notInStore": [f"{r['code']} {r['number']}" for r in articles if not r["inStore"]],
               "repealedOrNotInForce": [f"{r['code']} {r['number']}" for r in articles
                                        if r["status"] in ("repealed", "notInForce")],
               "notVerified": [f"{r['code']} {r['number']}" for r in articles
                               if r["status"] == "inForce" and not r.get("verified")],
               "proofLabels": {lab: sum(1 for r in articles if r.get("proof") == lab)
                               for lab in ("mechanical", "twoFamilies", "oneFamily")},
               "carriedByModules": [f"{r['code']} {r['number']}" for r in articles if r["inModules"]],
               "documentsMatchedInModules": sum(1 for d in documents
                                                if any(m["strength"] == "strong" for m in d["inModules"])),
               "prerequisitesMatchedInModules": sum(1 for p in prerequisites if any(
                   m.get("strength", "strong") == "strong" for m in p["inModules"]))}
    if texts is not None:
        summary["outsideUnitTexts"] = [f"{r['code']} {r['number']}" for r in articles if not r["inUnitTexts"]]
    return {"unit": claim["id"], "finalModules": sorted(finals), "articles": articles, "prerequisites": prerequisites,
            "documents": documents, "summary": summary,
            "rule": "facts read by script from the article store and the final module records; a module match by shared "
                    "words is a lead for the deriver, never a finding"}


def load_finals(folder: str) -> dict[str, dict]:
    """The final module records of the folder (claim types' finals are left out: a claim imports only modules)."""
    import prompts
    modules = {r["id"] for r in prompts.handbook_modules()}
    out = {}
    if os.path.isdir(folder):
        for name in sorted(os.listdir(folder)):
            if name.endswith(".json") and name[:-5] in modules:
                out[name[:-5]] = rc.load(os.path.join(folder, name))
    return out


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="catalogue_check")
    p.add_argument("claim")
    p.add_argument("--store", help="the article store (default: the working folder's)")
    p.add_argument("--final", default=os.path.join(CL, "rules", "final"))
    p.add_argument("--texts", help="the unit's texts file (default: rules\\reports\\<claim>.texts.json if it exists)")
    p.add_argument("--out")
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    claim = rc.catalogue_claim(a.claim)
    if claim is None:
        print(f"{a.claim} is not a claim type of the catalogue", file=sys.stderr)
        return 2
    texts_path = a.texts or os.path.join(CL, "rules", "reports", f"{a.claim}.texts.json")
    texts = rc.load(texts_path) if os.path.isfile(texts_path) else None
    doc = check(claim, st.Store(a.store) if a.store else st.Store(), load_finals(a.final), texts)
    out = a.out or os.path.join(CL, "rules", "reports", f"{a.claim}.catalogue-check.json")
    rc.save(out, doc)
    s = doc["summary"]
    print(f"{out}: {s['articles']} article(s); not in the store {s['notInStore'] or 'none'}; repealed or not in force "
          f"{s['repealedOrNotInForce'] or 'none'}; not verified {s['notVerified'] or 'none'}; carried by a final module "
          f"{s['carriedByModules'] or 'none'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
