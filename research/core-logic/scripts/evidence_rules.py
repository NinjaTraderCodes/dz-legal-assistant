"""The owner's evidence test 3 (19 November 2026): "on 30 fictitious cases across the 9 final claim types, list every legal
error the current app (its catalogue and formal checks) would let through and the final rules catch: a missed deadline, a
missing prior step, a wrong court or section, a missing document. Count them by kind and severity, with the article for
each."

    python evidence_rules.py --out <folder>

The cases are the final records' own worked examples (fictitious, with their facts and petition date), 30 chosen round
robin over the 9 claim types, each with at least one finding that warns or blocks; within a claim type the example that
adds the most findings of the owner's four kinds not yet seen comes first (boundaries break ties). Each case is run through the published rules tool on the final record
(`rules eval`). What the app does is read from its code and data, not guessed: its 15 formal checks (definition.json,
`elements`) test only that a part of the petition is present (CPCA 15, 14, 17, 8); its catalogue shows the claim type's
`prerequisites` as one fixed "⚠" line whatever the facts, offers `requiredDocuments` as chips to insert, and cites
`articles` in the grounds. No part of the app computes a deadline, tests a prior step against the facts, or checks a
court or section against the case.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from collections import Counter, defaultdict

sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FINAL = os.path.join(REPO, "docs", "research", "core-logic", "rules", "final")
CLAIMS = ["unpaid-wages", "abusive-dismissal", "unpaid-rent", "nafaqa", "divorce-by-husband", "tatliq", "khul", "custody",
          "custody-forfeiture"]
KINDS = {"deadline": "missed deadline", "priorStep": "missing prior step", "courtSection": "wrong court or section",
         "exit": "wrong court or section", "boundary": "wrong claim or court", "document": "missing document"}


def tool() -> str:
    with open(os.path.join(r"C:\Users\TheShadow\dz-core-research\tools\rules", "current.txt"), encoding="utf-8") as f:
        return f.read().strip()


def catalogue() -> dict:
    out = {}
    for name in os.listdir(os.path.join(REPO, "data", "law", "claims")):
        with open(os.path.join(REPO, "data", "law", "claims", name), encoding="utf-8-sig") as f:
            text = re.sub(r"(?m)^\s*//.*$", "", f.read())
        doc = json.loads(text)
        for c in doc if isinstance(doc, list) else doc.get("claims", doc.get("claimTypes", [])):
            out[c["id"]] = c
    return out


def items_of(record: dict) -> dict:
    found = {}
    for key in ("deadlines", "priorSteps", "documents", "boundaries"):
        for x in record.get(key, []):
            found[x["id"]] = x
    for key in ("territory", "exits"):
        for x in record.get("court", {}).get(key, []):
            found[x["id"]] = x
    if "section" in record.get("court", {}):
        found["section"] = record["court"]["section"]
    return found


def severity(finding: dict, item: dict | None) -> str:
    kind, declared = finding["kind"], finding["declaredEffect"]
    if kind == "deadline":
        dk = (item or {}).get("kind", "")
        if declared == "blockCleanExport":
            return "A: the right is lost (forfeiture, the text's sanction)"
        if dk == "forfeiture":
            return "A-: the right may be lost (forfeiture, reading not settled)"
        return "B: time-barred if the defendant pleads it (limitation)"
    if kind == "priorStep":
        return ("A: the claim is inadmissible (the text's sanction)" if declared == "blockCleanExport"
                else "B: the claim may be inadmissible (sanction not settled by the text)")
    if kind in ("exit", "boundary"):
        return "B: wrong court or regime: dismissed or sent elsewhere, time lost"
    if kind == "courtSection":
        return "C: wrong section: referred within the court"
    if kind == "document":
        return "C: a document to attach"
    return "C: other"


def app_view(claim: str, finding: dict, cat: dict) -> str:
    entry = cat.get(claim, {})
    cites = {f"{a['code']} {a['number']}" for a in entry.get("articles", [])}
    basis = {f"{b.get('code', '')} {b.get('article', '')}".strip() for b in finding.get("basis", [])}
    if finding["kind"] == "priorStep" and entry.get("prerequisites") and any(b.startswith("labour-disputes") for b in basis):
        return "fixed reminder (same text for every case)"
    if any(b in ("cpca 17", "cpca 519") for b in basis):
        return "app formal check art17.publication (asks that the publication be mentioned; presence only)"
    if finding["kind"] == "document" and entry.get("requiredDocuments"):
        return "document chips offered (no check)"
    if basis & cites:
        return "article cited in the grounds (no check)"
    return "nothing"


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--cases", type=int, default=30)
    a = p.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    rules, cat = tool(), catalogue()
    per_claim = {}
    for claim in CLAIMS:
        with open(os.path.join(FINAL, claim + ".json"), encoding="utf-8") as f:
            record = json.load(f)
        items = items_of(record)
        runs = []
        for ex in record.get("examples", []):
            case = os.path.join(a.out, f"case-{claim}-{ex['id']}.json")
            with open(case, "w", encoding="utf-8") as f:
                json.dump({"petitionDate": ex["petitionDate"], "slots": ex["slots"], "claimIds": [claim]}, f, ensure_ascii=False)
            r = subprocess.run([rules, "eval", "--records", os.path.join(FINAL, claim + ".json"), "--case", case],
                               capture_output=True, text=True, encoding="utf-8")
            if r.returncode != 0:
                raise RuntimeError(f"{claim} {ex['id']}: {r.stderr[-300:]}")
            out = json.loads(r.stdout)
            hits = [f for f in out["findings"] if f["effect"] in ("warn", "blockCleanExport") and f["kind"] in KINDS]
            if hits:
                runs.append({"claim": claim, "example": ex["id"], "title": ex.get("title", ""), "petitionDate": ex["petitionDate"],
                             "findings": [{"ref": f["ref"], "kind": f["kind"], "declared": f["declaredEffect"],
                                           "shown": f["effect"], "grade": f.get("grade"),
                                           "articles": sorted({f"{b.get('code', '')} {b.get('article', '')}".strip()
                                                               for b in f.get("basis", [])}),
                                           "message": f["message"]["en"], "daysLeft": f.get("daysLeft"),
                                           "lastDay": f.get("lastDay"),
                                           "severity": severity(f, items.get(f["ref"].split(".")[-1] if "." in f["ref"] else f["ref"])),
                                           "app": app_view(claim, f, cat)} for f in hits]})
        per_claim[claim] = runs
    # 30 cases, round robin over the claim types; inside one, the case adding the most unseen findings first.
    chosen, seen = [], defaultdict(set)
    while len(chosen) < a.cases and any(per_claim.values()):
        for claim in CLAIMS:
            if len(chosen) >= a.cases or not per_claim[claim]:
                continue
            # the owner's four kinds first (deadline, prior step, court or section, document); boundaries break ties
            best = max(per_claim[claim], key=lambda r: (
                len({f["ref"] for f in r["findings"] if f["kind"] != "boundary"} - seen[claim]),
                len({f["ref"] for f in r["findings"]} - seen[claim])))
            per_claim[claim].remove(best)
            seen[claim] |= {f["ref"] for f in best["findings"]}
            chosen.append(best)
    with open(os.path.join(a.out, "cases.json"), "w", encoding="utf-8") as f:
        json.dump(chosen, f, ensure_ascii=False, indent=1)
    findings = [dict(fd, claim=c["claim"], example=c["example"]) for c in chosen for fd in c["findings"]]
    through = [fd for fd in findings if fd["app"] == "nothing" or fd["app"].startswith("article cited")]
    by_kind = Counter(KINDS[fd["kind"]] for fd in through)
    by_sev = Counter(fd["severity"][:2].strip(":") for fd in through)
    print(f"{len(chosen)} cases, {len(findings)} findings (warn or block); let through by the app (no case-specific check): "
          f"{len(through)}; met by an app reminder or presence check: {len(findings) - len(through)}")
    print("by kind:", dict(by_kind))
    print("by severity:", dict(sorted(by_sev.items())))
    print("app view:", dict(Counter(fd["app"] for fd in findings)))
    with open(os.path.join(a.out, "summary.json"), "w", encoding="utf-8") as f:
        json.dump({"cases": len(chosen), "findings": len(findings), "letThrough": len(through), "byKind": by_kind,
                   "bySeverity": by_sev, "app": Counter(fd["app"] for fd in findings)}, f, ensure_ascii=False, indent=1)
    return 0


if __name__ == "__main__":
    sys.exit(main())
