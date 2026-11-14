"""Stage 7 of step 1: what the two Codex checks of a reconciled record are given, and what is done with their answers
(prompts/rules-attack-codex.md, prompts/rules-support-check-codex.md, specs/record-checks.md).

    python stage7.py render <quoted reconciled.json> --out <file>        -> {{record_text}} of the Codex attack
    python stage7.py pairs <quoted reconciled.json>... --batch <name> --out <pairs.txt> [--seed n]
                                                                          -> {{pairs}}; the key goes to support-keys\\
    python stage7.py split <answer.json> --batch <name>                   -> rules/reports/<unit>.support.json

The key of the seeded false pairs is written outside the repository (support-keys\\<batch>.json) and never shown to the
checker. Sentences are built from the items' structured values and their `says`; nothing here states law of its own.
"""
from __future__ import annotations

import argparse
import json
import os
import random
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import records as rc  # noqa: E402
import store as st  # noqa: E402

KEYS = os.path.join(os.path.dirname(os.path.dirname(st.DEFAULT_OUT)), "support-keys")
REPORTS = os.path.join(st.REPO, "docs", "research", "core-logic", "rules", "reports")
SEEDED_SHARE = 5  # one pair in five is seeded
STEP_WORDS = {
    "formalNotice": "the debtor must first be put on notice (formal notice)",
    "labourConciliation": "a conciliation attempt before the labour conciliation office must have taken place",
    "commercialConciliation": "a conciliation attempt must have taken place",
    "landRegistryPublication": "the petition must be published at the land registry",
    "noticeToQuit": "a notice to quit must have been given",
    "reconciliationAttempt": "a reconciliation attempt must have taken place",
    "mediation": "a mediation must have taken place",
    "other": "a prior step must have been taken",
}


# --- conditions and items in English ------------------------------------------------------------------------------------
def slot_words(slot: str) -> str:
    return slot.replace("_", " ")


def cond_en(c) -> str:
    if c is None:
        return "always"
    if "all" in c:
        return " and ".join(f"({cond_en(x)})" if ("all" in x or "any" in x) else cond_en(x) for x in c["all"])
    if "any" in c:
        return " or ".join(f"({cond_en(x)})" if ("all" in x or "any" in x) else cond_en(x) for x in c["any"])
    if "not" in c:
        return f"not ({cond_en(c['not'])})"
    s = c.get("slot", "")
    if s == "requests" and "includes" in c:
        return f"the petition asks for {slot_words(c['includes'])}"
    if "is" in c:
        return f"{slot_words(s)} is {c['is']}"
    if "in" in c:
        return f"{slot_words(s)} is one of {', '.join(map(str, c['in']))}"
    if "known" in c:
        return f"{slot_words(s)} is {'known' if c['known'] else 'unknown'}"
    if "before" in c or "after" in c:
        return f"{slot_words(s)} is {'before' if 'before' in c else 'after'} {c.get('before') or c.get('after')}"
    return json.dumps(c, ensure_ascii=False)


def _lead_ins(stmts: dict) -> dict:
    """(code, article) -> the quoted paragraph 1 of that article when it introduces a list (ends with a colon): an item
    of the list says nothing without it («لا ضرورة لاعذار المدين فى الحالات الآتية :» before «ـ اذا كان محل الالتزام …»).
    Taken only from the record's own quotes; the first module's pairs lacked it and the checker rightly said "partly"."""
    out = {}
    for s in stmts.values():
        for q in s.get("quote", []):
            if q["paragraph"] == 1 and q["text"].rstrip().endswith((":", "：")):
                out[(s["cite"]["code"], s["cite"]["article"])] = q["text"]
    return out


def _texts(stmts: dict, ids: list) -> list[dict]:
    out, seen, leads = [], set(), _lead_ins(stmts)
    for i in ids:
        s = stmts.get(i)
        if not s:
            continue
        for q in s.get("quote", []):
            k = (s["cite"]["code"], s["cite"]["article"], q["paragraph"])
            lead = leads.get(k[:2])
            if q["paragraph"] > 1 and lead and k[:2] + (1,) not in seen:
                seen.add(k[:2] + (1,))
                out.append({"code": k[0], "article": k[1], "paragraph": 1, "text": lead})
            if k not in seen:
                seen.add(k)
                out.append({"code": k[0], "article": k[1], "paragraph": k[2], "text": q["text"]})
    return sorted(out, key=lambda t: (t["code"], st._num_key(t["article"]), t["paragraph"])) if out else out


def _start(d: dict) -> str:
    s = d.get("startsFrom") or ""
    return s.get("slot", "") if isinstance(s, dict) else s


def items(record: dict) -> list[dict]:
    """Every checkable item of a quoted record as (unit, item, English statement, the quoted texts it rests on)."""
    unit, stmts, out = record["id"], rc.statements(record), []
    for s in record.get("basis", []):
        out.append({"unit": unit, "item": s["id"], "statement": (s.get("says") or {}).get("en", ""),
                    "texts": _texts(stmts, [s["id"]])})
    for d in record.get("deadlines", []):
        p = d.get("period") or {}
        sentence = (f"The time limit is {p.get('value')} {p.get('unit')}{'s' if p.get('value') != 1 else ''}, running "
                    f"from the date of {slot_words(_start(d))}; it is a {d['kind']} period")
        if (d.get("counting") or {}).get("method"):
            sentence += f", counted by the method {d['counting']['method']}"
        if d.get("when"):
            sentence += f"; it applies when {cond_en(d['when'])}"
        out.append({"unit": unit, "item": d["id"], "statement": sentence + ".", "texts": _texts(stmts, d.get("basis", []))})
    for p in record.get("priorSteps", []):
        exemptions = [(x.get("note") or {}).get("en") or cond_en(x.get("when")) for x in p.get("notRequiredWhen", [])]
        sentence = (f"Before a claim in which {cond_en(p.get('requiredWhen'))}, "
                    f"{STEP_WORDS.get(p['kind'], STEP_WORDS['other'])}")
        if exemptions:  # without them the sentence reads as unconditional, and the checker rightly says "no"
            sentence += ", except when: " + "; ".join(e.rstrip(".") for e in exemptions)
        out.append({"unit": unit, "item": p["id"], "statement": sentence + ".",
                    "texts": _texts(stmts, p.get("basis", []))})
        for n, x in enumerate(p.get("notRequiredWhen", []), 1):
            note = (x.get("note") or {}).get("en") or cond_en(x.get("when"))
            out.append({"unit": unit, "item": f"{p['id']}.notRequiredWhen[{n}]",
                        "statement": f"This prior step is not required when: {note}",
                        "texts": _texts(stmts, x.get("basis", []))})
    court = record.get("court") or {}
    if court.get("section"):
        out.append({"unit": unit, "item": "section",
                    "statement": f"The {court['section']['value']} section of the court hears this claim.",
                    "texts": _texts(stmts, court["section"].get("basis", []))})
    for x in court.get("exits", []):
        out.append({"unit": unit, "item": x["id"],
                    "statement": f"When {cond_en(x.get('when'))}, the case leaves the ordinary courts "
                                 f"({x['goesTo']}).", "texts": _texts(stmts, x.get("basis", []))})
    return [i for i in out if i["texts"] and i["statement"]]


# --- the Codex attack's {{record_text}} ---------------------------------------------------------------------------------
def _label(t: dict) -> str:
    return f"[{t['code']} {t['article']} ¶{t['paragraph']}]"


def render(record: dict, store: st.Store) -> str:
    """The quoted record as plain text: each item, its rule in English with its structured values, the quoted Arabic
    under it, then the neighbouring paragraphs that open an exception or refer back and are not cited."""
    lines = [f"Unit: {record['id']} ({record.get('kind', '')})", ""]
    by_item = {i["item"]: i for i in items(record)}
    for s in record.get("basis", []):
        lines.append(f"- {s['id']} ({s.get('role', '')}{', interpretive' if s.get('interpretive') else ''}): "
                     f"{by_item.get(s['id'], {}).get('statement', '')}")
        lines += [f"    {_label(t)} {t['text']}" for t in by_item.get(s["id"], {}).get("texts", [])]
    for kind in ("deadlines", "priorSteps"):
        for x in record.get(kind, []):
            values = {k: v for k, v in x.items() if k not in ("message", "basis", "grading", "interpretiveNote")}
            lines.append(f"- {x['id']} ({kind[:-1]}): {by_item.get(x['id'], {}).get('statement', '')}")
            lines.append(f"    values: {json.dumps(values, ensure_ascii=False)}")
            lines.append(f"    rests on: {', '.join(x.get('basis', []))}")
    court = record.get("court") or {}
    if court:
        lines.append(f"- court: {json.dumps({k: v for k, v in court.items() if k != 'grading'}, ensure_ascii=False)}")
    for b in record.get("boundaries", []):
        lines.append(f"- {b['id']} (boundary): {json.dumps({k: v for k, v in b.items() if k != 'grading'}, ensure_ascii=False)}")
    flags = rc.flag_neighbours(record, store)
    if flags:
        lines += ["", "Not cited (next to cited paragraphs):"]
        lines += [f"    [{f['code']} {f['article']} ¶{f['paragraph']}] (not cited) {f['text']}" for f in flags]
    return "\n".join(lines) + "\n"


# --- the support check's {{pairs}} and its key --------------------------------------------------------------------------
SWAPS = [(r"\bmust\b", "may"), (r"\bonly\b", "also"), (r"\bnot required\b", "required"), (r"\bis due\b", "is not due"),
         (r"\bmay\b", "must"), (r"\bunless\b", "even if")]


def _mutate(statement: str, rnd: random.Random) -> tuple[str, str] | None:
    """A changed statement (a number, an obligation, a negation), or None when no change applies."""
    nums = list(re.finditer(r"\d+", statement))
    options = []
    if nums:
        m = rnd.choice(nums)
        options.append(("changedNumber", statement[:m.start()] + str(int(m.group()) + rnd.choice([1, 2, 3])) +
                        statement[m.end():]))
    for pattern, repl in SWAPS:
        if re.search(pattern, statement):
            options.append(("swappedWord", re.sub(pattern, repl, statement, count=1)))
    return rnd.choice(options) if options else None


def build_pairs(records: list[dict], seed: int = 0) -> tuple[list[dict], dict]:
    """Real pairs from every item, plus one seeded false pair for every four real ones (so one in five of the batch):
    a statement with another article's text, or a changed statement with its own text. Shuffled together."""
    rnd = random.Random(seed)
    real = [dict(i, seeded=None) for r in records for i in items(r)]
    n_seeded = max(1, round(len(real) / (SEEDED_SHARE - 1)))
    seeded = []
    for k in range(n_seeded):
        base = real[k % len(real)]
        mutated = _mutate(base["statement"], rnd) if k % 2 else None
        if mutated:
            seeded.append(dict(base, statement=mutated[1], seeded=mutated[0]))
            continue
        own = {(t["code"], t["article"]) for t in base["texts"]}
        others = [i for i in real if not ({(t["code"], t["article"]) for t in i["texts"]} & own)]
        if others:
            seeded.append(dict(base, texts=rnd.choice(others)["texts"], seeded="wrongText"))
    pairs = real + seeded
    rnd.shuffle(pairs)
    for n, p in enumerate(pairs, 1):
        p["n"] = n
    key = {"seed": seed, "pairs": [{"n": p["n"], "unit": p["unit"], "item": p["item"], "seeded": p["seeded"],
                                   "statement": p["statement"], "texts": [_label(t) for t in p["texts"]]} for p in pairs]}
    return pairs, key


def render_pairs(pairs: list[dict]) -> str:
    out = []
    for p in pairs:
        text = "\n".join(f"{_label(t)} {t['text']}" for t in p["texts"])
        out.append(f"{p['n']}. STATEMENT: {p['statement']}\nTEXT:\n{text}")
    return "\n\n".join(out)


def split(answer: dict, key: dict) -> dict[str, dict]:
    """The answers back per unit; the seeded pairs are scored against the key. More than one seeded false pair in five
    accepted means the call's "yes" answers are not trusted (rules-support-check-codex.md)."""
    by_n = {a["n"]: a for a in answer["answers"]}
    seeded = [p for p in key["pairs"] if p["seeded"]]
    accepted = [p["n"] for p in seeded if (by_n.get(p["n"]) or {}).get("supports") == "yes"]
    trusted = len(accepted) * SEEDED_SHARE <= len(seeded)
    units: dict[str, dict] = {}
    for p in key["pairs"]:
        if p["seeded"]:
            continue
        a = by_n.get(p["n"]) or {"supports": "noAnswer", "missingOrWrong": ""}
        u = units.setdefault(p["unit"], {"unit": p["unit"], "seededTotal": len(seeded), "seededAccepted": len(accepted),
                                         "yesTrusted": trusted, "pairs": [], "attacks": []})
        u["pairs"].append({"n": p["n"], "item": p["item"], "statement": p["statement"], "texts": p["texts"],
                           "supports": a["supports"], "missingOrWrong": a.get("missingOrWrong", "")})
        if a["supports"] != "yes" or not trusted:
            u["attacks"].append({"id": f"sup-{p['n']}", "who": "supportCheck", "target": p["item"].split(".")[0],
                                 "kind": "notSupported" if a["supports"] == "no" else
                                 ("partlySupported" if a["supports"] == "partly" else "untrustedCall"),
                                 "claim": f"{p['statement']} | {a['supports']}: {a.get('missingOrWrong', '')}"})
    return units


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="stage7")
    sub = p.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("render"); r.add_argument("record"); r.add_argument("--out", required=True)
    r.add_argument("--store")
    q = sub.add_parser("pairs"); q.add_argument("records", nargs="+"); q.add_argument("--batch", required=True)
    q.add_argument("--out", required=True); q.add_argument("--seed", type=int, default=0)
    s = sub.add_parser("split"); s.add_argument("answer"); s.add_argument("--batch", required=True)
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    if a.cmd == "render":
        text = render(rc.load(a.record), st.Store(a.store) if a.store else st.Store())
        with open(a.out, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print(a.out, len(text), "characters")
    elif a.cmd == "pairs":
        pairs, key = build_pairs([rc.load(x) for x in a.records], a.seed)
        os.makedirs(KEYS, exist_ok=True)
        rc.save(os.path.join(KEYS, f"{a.batch}.json"), key)
        with open(a.out, "w", encoding="utf-8", newline="\n") as f:
            f.write(render_pairs(pairs) + "\n")
        print(len(pairs), "pairs,", sum(1 for x in pairs if x["seeded"]), "seeded; key in", KEYS)
    elif a.cmd == "split":
        key = rc.load(os.path.join(KEYS, f"{a.batch}.json"))
        for unit, doc in split(rc.load(a.answer), key).items():
            rc.save(os.path.join(REPORTS, f"{unit}.support.json"), doc)
            print(unit, len(doc["pairs"]), "pairs,", len(doc["attacks"]), "attacks; seeded accepted",
                  doc["seededAccepted"], "of", doc["seededTotal"], "; yes trusted:", doc["yesTrusted"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
