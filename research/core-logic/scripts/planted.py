"""Planted errors: the proof that the checks catch mistakes (docs/research/core-logic/specs/planted-errors.md).

    python planted.py plant --round <n> --seed <n> [--root <folder>] [--store <articles.json>] [--dry-run]
                            <final records...>
    python planted.py score --round <n> [--root <folder>] [--store <articles.json>] [--reports <folder>]

`plant` copies each final record into the round's root as p<n>-NN (planted), c<n>-NN (an untouched control) and, for
the two kinds a script must catch by failing, s<n>-NN (one plant per copy), and writes the answer key. `--dry-run`
prints what it would plant and writes nothing. `score` reads the key and the checkers' outputs under the root and
writes rules/reports/planted-errors.md (rates only) and planted-errors.json (what `records.py grade --planted` reads).

The copies are laid out as `unit.py --root <root>` reads a unit: <root>\\rules\\reconciled\\<copy>.json,
<root>\\rules\\reports\\<copy>.*, <root>\\rules\\boundary-cases\\<copy>.json, <root>\\runs\\<copy>\\. The key is
<root>\\key\\key.json, beside rules\\ and never under it, so that no allow-list built from rules\\ reaches it.
"""
from __future__ import annotations

import argparse
import copy
import datetime as dt
import json
import math
import os
import random
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import records as rc  # noqa: E402
import store as st  # noqa: E402
import unit  # noqa: E402

KINDS = ("periodChanged", "paragraphShifted", "exceptionUncited", "limitationForForfeiture", "startingEventChanged",
         "wrongArticle", "repealedCited", "exemptionDropped", "raisedByChanged", "blockForLimitation",
         "conditionInverted", "doneBesideOpen")
# Their catching is a script's failure, which stops the chain before any agent: planted on s copies, scored by script.
SCRIPT_ONLY = ("repealedCited", "blockForLimitation")
STATEMENT_KINDS = ("paragraphShifted", "exceptionUncited", "wrongArticle", "repealedCited")
PER_KIND = 5
RAISED_BY = ("courtOfOwnMotion", "pleaOnly", "unclear")
# What a script writes and an agent never types (rules check, item 8): a reconciled copy must not hold any of them.
SCRIPT_FIELDS = {"quote", "source", "grading", "shownEffect", "toCheck", "agreementBeforeReconciliation"}
CONDITION_ROOTS = {"priorStep": ("requiredWhen", "notRequiredWhen"), "exit": ("when",), "section": ("alternatives",),
                   "boundary": ("when",)}
REPORTS = os.path.join(unit.CL, "rules", "reports")
SLOTS = os.path.join(unit.CL, "slots.json")
CANNOT_SHOW = ("What this cannot show: a planted error is one that is in the record against the text. A belief that "
               "every model shares and no text contradicts in front of them cannot be planted, so it is not measured.")


def default_root(round_: int) -> str:
    return os.path.join(unit.WORK, "planted", f"round-{round_}")


# --- where a copy's files are (as unit.Unit lays them out under --root) -----------------------------------------------
def rules_dir(root: str) -> str:
    return os.path.join(root, "rules")


def reconciled_path(root: str, cid: str) -> str:
    return os.path.join(rules_dir(root), "reconciled", f"{cid}.json")


def candidate_path(root: str, cid: str) -> str:
    return os.path.join(rules_dir(root), "final-candidates", f"{cid}.json")


def report_path(root: str, cid: str, suffix: str) -> str:
    return os.path.join(rules_dir(root), "reports", f"{cid}.{suffix}")


def boundary_path(root: str, cid: str) -> str:
    return os.path.join(rules_dir(root), "boundary-cases", f"{cid}.json")


def run_dir(root: str, cid: str) -> str:
    return os.path.join(root, "runs", cid)


def key_path(root: str) -> str:
    return os.path.join(root, "key", "key.json")


def canon(x) -> str:
    return json.dumps(x, ensure_ascii=False, sort_keys=True)


# --- fields inside an item ---------------------------------------------------------------------------------------------
def parse(path: str) -> list:
    """«notRequiredWhen[2].when.any[0].is» -> ["notRequiredWhen", 2, "when", "any", 0, "is"]."""
    return [m.group(1) if m.group(1) is not None else int(m.group(2))
            for m in re.finditer(r"([^.\[\]]+)|\[(\d+)\]", path)]


def get_at(node, path: str):
    for t in parse(path):
        try:
            node = node[t]
        except (KeyError, IndexError, TypeError):
            return None
    return node


def set_at(node, path: str, value) -> None:
    tokens = parse(path)
    for t in tokens[:-1]:
        node = node[t]
    node[tokens[-1]] = value


def overlaps(a: str, b: str) -> bool:
    """Two fields of one item overlap when one holds the other ("" is the whole item)."""
    if not a or not b or a == b:
        return True
    return any(y.startswith(x + ".") or y.startswith(x + "[") for x, y in ((a, b), (b, a)))


def strip_script_fields(node):
    if isinstance(node, dict):
        return {k: strip_script_fields(v) for k, v in node.items() if k not in SCRIPT_FIELDS}
    if isinstance(node, list):
        return [strip_script_fields(v) for v in node]
    return node


def reconciled_copy(final: dict) -> dict:
    out = strip_script_fields(copy.deepcopy(final))
    out["stage"] = "reconciled"
    return out


def find_item(record: dict, iid: str):
    for kind, x in rc.items(record):
        if rc.item_id(kind, x) == iid:
            return kind, x
    return None, None


def users(record: dict, sid: str) -> list[str]:
    """The items (not statements) whose basis, counting, exemptions, means or readings rest on a statement."""
    return [rc.item_id(k, x) for k, x in rc.items(record) if k != "statement" and sid in rc.basis_ids(x)]


def covers(cite: dict, code: str, article: str, paragraph: int | None) -> bool:
    return (cite.get("code"), cite.get("article")) == (code, article) and (
        paragraph is None or not cite.get("paragraphs") or paragraph in cite["paragraphs"])


def covers_all(have: dict, want: dict) -> bool:
    """A cite that cites at least what another cites: the same article, every paragraph of it."""
    if (have.get("code"), have.get("article")) != (want.get("code"), want.get("article")):
        return False
    if not have.get("paragraphs"):
        return True
    return bool(want.get("paragraphs")) and set(want["paragraphs"]) <= set(have["paragraphs"])


def same_cite(a: dict | None, b: dict | None) -> bool:
    if not a or not b:
        return False
    return (a.get("code"), a.get("article"), a.get("version"), sorted(a.get("paragraphs") or [])) == \
        (b.get("code"), b.get("article"), b.get("version"), sorted(b.get("paragraphs") or []))


# --- where each kind can be planted -----------------------------------------------------------------------------------
class Context:
    """What the candidates read besides the record: the store (never written), slots.json, the unit's texts."""

    def __init__(self, store: st.Store, slots_doc: dict, seed: int, texts: dict[str, dict] | None = None):
        self.store, self.seed, self.texts = store, seed, texts or {}
        self.slot_types = {s["id"]: s.get("type") for s in slots_doc.get("slots", [])}
        self.slot_asked = {s["id"]: s.get("askedFor", []) for s in slots_doc.get("slots", [])}
        self.repealed_only: dict[str, list[dict]] = {}
        for (code, article), entries in sorted(store.any_version.items(), key=lambda kv: (kv[0][0], st._num_key(kv[0][1]))):
            if (code, article) not in store.in_force and any(e["repealed"] for e in entries):
                self.repealed_only.setdefault(code, []).append(next(e for e in entries if e["repealed"]))

    def rng(self, *parts) -> random.Random:
        # One generator per place: a place's choice does not depend on which other places were looked at first.
        return random.Random("|".join(str(p) for p in (self.seed, *parts)))

    def chapter_mates(self, unit_id: str, cite: dict) -> list[dict]:
        """The other in-force articles of the cited article's chapter: the store's path, else the unit's texts."""
        e = self.store.get(cite["code"], cite["article"], cite.get("version"))
        mates = []
        if e and e.get("path"):
            mates = self.store.chapter(cite["code"], e["path"])
        else:
            for ch in self.texts.get(unit_id, {}).get("chapters", []):
                if ch.get("code") == cite["code"] and cite["article"] in ch.get("articles", []):
                    mates = [self.store.in_force[(cite["code"], a)] for a in ch["articles"]
                             if (cite["code"], a) in self.store.in_force]
                    break
        return sorted((m for m in mates if m["article"] != cite["article"] and m.get("paragraphs")),
                      key=lambda m: st._num_key(m["article"]))


def _place(kind, rec, unit_id, item, field, before, after, **extra) -> dict:
    return {"kind": kind, "rec": rec, "unit": unit_id, "item": item, "field": field, "before": before, "after": after,
            **extra}


def _kept_paragraphs(cite: dict, count: int) -> dict:
    out = {"code": cite["code"]}
    if cite.get("paragraphs"):
        out["paragraphs"] = [p for p in cite["paragraphs"] if p <= count] or [1]
    return out


def _conditions(cond, path: str):
    """Every leaf of a condition whose `is` is yes or no, with its path."""
    if not isinstance(cond, dict):
        return
    if "slot" in cond and cond.get("is") in ("yes", "no"):
        yield path + ".is"
    for key in ("all", "any"):
        for i, c in enumerate(cond.get(key) or []):
            yield from _conditions(c, f"{path}.{key}[{i}]")
    if "not" in cond:
        yield from _conditions(cond["not"], path + ".not")


def candidates(rec: int, record: dict, ctx: Context) -> dict[str, list[dict]]:
    uid = record["id"]
    out = {k: [] for k in KINDS}
    cited = {(s["cite"]["code"], s["cite"]["article"]) for s in record.get("basis", [])}
    for s in record.get("basis", []):
        c, sid, used = s["cite"], s["id"], users(record, s["id"])
        e = ctx.store.get(c["code"], c["article"], c.get("version"))
        if e and not e["repealed"] and e.get("paragraphs"):
            n = len(e["paragraphs"])
            moves = [(p, p + d) for p in c.get("paragraphs") or [] for d in (-1, 1)
                     if 1 <= p + d <= n and p + d not in c["paragraphs"]]
            if moves:
                old, new = ctx.rng("paragraphShifted", uid, sid).choice(moves)
                after = dict(c, paragraphs=sorted(new if p == old else p for p in c["paragraphs"]))
                out["paragraphShifted"].append(_place("paragraphShifted", rec, uid, sid, "cite", c, after, **{"from": used}))
            cited_paras = c.get("paragraphs") or list(range(1, n + 1))
            exceptions = [p for p in cited_paras if p <= n and rc._opens_with_exception(e["paragraphs"][p - 1])]
            # Removing it must leave every list that held it non-empty: an empty basis is no longer the same error.
            if exceptions and used and all(len(lst) > 1 for lst in _lists_holding(record, sid)):
                out["exceptionUncited"].append(_place("exceptionUncited", rec, uid, sid, "basis", s, None, cite=c,
                                                      exceptionParagraphs=exceptions, **{"from": used}))
        mates = [m for m in ctx.chapter_mates(uid, c) if (m["code"], m["article"]) not in cited]
        if mates:
            m = ctx.rng("wrongArticle", uid, sid).choice(mates)
            after = dict(_kept_paragraphs(c, len(m["paragraphs"])), article=m["article"])
            out["wrongArticle"].append(_place("wrongArticle", rec, uid, sid, "cite", c,
                                              {k: after[k] for k in ("code", "article", "paragraphs") if k in after},
                                              **{"from": used}))
        gone = ctx.repealed_only.get(c["code"], [])
        if gone:
            near = [g for g in gone if e and e.get("path") and g.get("path") == e["path"]] or gone
            g = ctx.rng("repealedCited", uid, sid).choice(near)
            after = dict(_kept_paragraphs(c, max(len(g.get("paragraphs") or []), 1)), article=g["article"])
            out["repealedCited"].append(_place("repealedCited", rec, uid, sid, "cite", c,
                                               {k: after[k] for k in ("code", "article", "paragraphs") if k in after},
                                               **{"from": used}))
    dates = [x for x in record.get("slots", []) if ctx.slot_types.get(x) == "date"]
    for d in record.get("deadlines", []):
        did, v = d["id"], d["period"]["value"]
        out["periodChanged"].append(_place("periodChanged", rec, uid, did, "period.value", v, v // 2 if v >= 2 else v + 1))
        if d["kind"] == "forfeiture":
            out["limitationForForfeiture"].append(_place(
                "limitationForForfeiture", rec, uid, did, "kind, effectWhenExpired",
                {"kind": d["kind"], "effectWhenExpired": d["effectWhenExpired"]},
                {"kind": "limitation", "effectWhenExpired": "warn"}))
        if d["kind"] == "limitation" and d["effectWhenExpired"] != "blockCleanExport":
            out["blockForLimitation"].append(_place("blockForLimitation", rec, uid, did, "effectWhenExpired",
                                                    d["effectWhenExpired"], "blockCleanExport"))
        start = d["startsFrom"]["slot"]
        others, added = [x for x in dates if x != start], False
        if not others:
            others = sorted(x for x, t in ctx.slot_types.items() if t == "date" and x != start and
                            ("*" in ctx.slot_asked.get(x, []) or uid in ctx.slot_asked.get(x, [])))
            added = True
        if others:
            extra = {"addedSlot": True} if added else {}
            out["startingEventChanged"].append(_place("startingEventChanged", rec, uid, did, "startsFrom.slot", start,
                                                      ctx.rng("startingEventChanged", uid, did).choice(others), **extra))
        other = [x for x in RAISED_BY if x != d["raisedBy"]]
        out["raisedByChanged"].append(_place("raisedByChanged", rec, uid, did, "raisedBy", d["raisedBy"],
                                             ctx.rng("raisedByChanged", uid, did).choice(other)))
    for p in record.get("priorSteps", []):
        for i, ex in enumerate(p.get("notRequiredWhen") or []):
            out["exemptionDropped"].append(_place("exemptionDropped", rec, uid, p["id"], f"notRequiredWhen[{i}]", ex, None))
        if p.get("acceptedMeans"):
            out["doneBesideOpen"].append(_place("doneBesideOpen", rec, uid, p["id"], "acceptedMeans", p["acceptedMeans"],
                                                None))
    for kind, x in rc.items(record):
        for root in CONDITION_ROOTS.get(kind, ()):
            value = x.get(root)
            if isinstance(value, dict):
                paths = list(_conditions(value, root))
            elif isinstance(value, list):
                paths = [p for i, entry in enumerate(value) for p in _conditions(entry.get("when"), f"{root}[{i}].when")]
            else:
                continue
            for path in paths:
                was = get_at(x, path)
                out["conditionInverted"].append(_place("conditionInverted", rec, uid, rc.item_id(kind, x), path, was,
                                                       "no" if was == "yes" else "yes"))
    return out


def _lists_holding(record: dict, sid: str) -> list[list]:
    found = []

    def walk(node):
        if isinstance(node, dict):
            for k, v in node.items():
                if k == "basis" and isinstance(v, list) and sid in v:
                    found.append(v)
                else:
                    walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)

    for key, value in record.items():
        if key != "basis":  # the record's own list of statements is not a reference to one
            walk(value)
    return found


# --- choosing the places ----------------------------------------------------------------------------------------------
def place_id(p: dict) -> tuple:
    return p["rec"], p["kind"], p["item"], p["field"]


def touches(p: dict) -> list[tuple]:
    if p["kind"] == "exceptionUncited":
        return [(p["item"], "")]
    if p["kind"] == "limitationForForfeiture":
        return [(p["item"], "kind"), (p["item"], "effectWhenExpired")]
    return [(p["item"], p["field"])]


def conflicts(p: dict, chosen: list[dict]) -> bool:
    if p["kind"] in SCRIPT_ONLY:  # each on its own s copy: only the same place twice is a conflict
        return any(q["kind"] == p["kind"] and (q["rec"], q["item"]) == (p["rec"], p["item"]) for q in chosen)
    for q in chosen:
        if q["rec"] != p["rec"] or q["kind"] in SCRIPT_ONLY:
            continue
        if any(a == b and overlaps(f, g) for a, f in touches(p) for b, g in touches(q)):
            return True
        # two statements removed from one item could leave it with no basis at all
        if p["kind"] == q["kind"] == "exceptionUncited" and set(p["from"]) & set(q["from"]):
            return True
    return False


def pick(kind: str, pool: list[dict], chosen: list[dict], want: int, bad: set) -> list[dict]:
    """Up to `want` places of one kind, spread: the copy with fewest plants of the kind first, then an item no other
    plant touches (so that a flag on the item is about this plant), then the copy with fewest plants, then the seed."""
    picked = []
    order = {place_id(p): i for i, p in enumerate(pool)}
    while len(picked) < want:
        same = [q for q in chosen + picked if q["kind"] == kind]
        free = [p for p in pool if place_id(p) not in bad and place_id(p) not in {place_id(q) for q in chosen + picked}
                and not conflicts(p, chosen + picked)]
        if not free:
            break

        def rank(p):
            others = [q for q in chosen + picked if q["rec"] == p["rec"] and q["kind"] not in SCRIPT_ONLY]
            return (sum(1 for q in same if q["rec"] == p["rec"]), any(q["item"] == p["item"] for q in others),
                    len(others), order[place_id(p)])

        picked.append(min(free, key=rank))
    return picked


def select(places: dict[str, list[dict]], rng: random.Random, bad: set | None = None) -> list[dict]:
    """The scarcest kinds choose first, so that they get items of their own."""
    chosen = []
    for kind in sorted(KINDS, key=lambda k: (len(places[k]), KINDS.index(k))):
        pool = sorted(places[kind], key=lambda p: (p["rec"], p["item"], p["field"]))
        rng.shuffle(pool)
        chosen += pick(kind, pool, chosen, PER_KIND, bad or set())
    return chosen


# --- applying plants --------------------------------------------------------------------------------------------------
def _drop_ref(node, sid: str) -> None:
    if isinstance(node, dict):
        for k, v in node.items():
            if k in ("basis", "refs") and isinstance(v, list) and all(isinstance(x, str) for x in v):
                v[:] = [x for x in v if x != sid]
            else:
                _drop_ref(v, sid)
    elif isinstance(node, list):
        for v in node:
            _drop_ref(v, sid)


def apply(record: dict, plants: list[dict]) -> dict:
    """The record with its plants, changes first and removals after (list entries from the last), so that every
    plant's field, written against the untouched record, still names the right place."""
    out = copy.deepcopy(record)

    def order(p):
        if p["kind"] == "exemptionDropped":
            return 1, -parse(p["field"])[1]
        return (1, 0) if p["kind"] in ("doneBesideOpen", "exceptionUncited") else (0, 0)

    for p in sorted(plants, key=order):
        _, x = find_item(out, p["item"])
        if x is None:
            raise KeyError(f"{record['id']}: no item {p['item']} for {p['kind']}")
        if p["kind"] == "limitationForForfeiture":
            x.update(p["after"])
        elif p["kind"] == "exemptionDropped":
            del x["notRequiredWhen"][parse(p["field"])[1]]
        elif p["kind"] == "doneBesideOpen":
            del x["acceptedMeans"]
        elif p["kind"] == "exceptionUncited":
            out["basis"] = [s for s in out["basis"] if s["id"] != p["item"]]
            for key, value in out.items():
                if key != "basis":
                    _drop_ref(value, p["item"])
        else:
            set_at(x, p["field"], copy.deepcopy(p["after"]))
            if p.get("addedSlot") and p["after"] not in out.setdefault("slots", []):
                out["slots"].append(p["after"])
    return out


def key_entry(p: dict, copy_id: str) -> dict:
    return {"copy": copy_id, **{k: v for k, v in p.items() if k != "rec"}}


# --- rules check: a plant that the schema refuses is replaced ----------------------------------------------------------
def problem_lines(output: str) -> list[str]:
    return [line.strip() for line in output.splitlines() if line.startswith("      ") and line.strip()]


def _norm(line: str) -> str:
    return re.sub(r"\[\d+\]", "[]", line)  # a removed statement shifts the indices of the others


def tool_checker(tool: str, store_path: str):
    def check(path: str) -> list[str]:
        code, out = unit.run([tool, "check", "--store", store_path, path])
        if code not in (0, 1):
            raise RuntimeError(f"rules check could not run on {path}:\n{out[-1500:]}")
        return problem_lines(out)
    return check


def new_problems(checker, path: str, record: dict, control: set[str]) -> list[str]:
    rc.save(path, record)
    return sorted({_norm(x) for x in checker(path)} - control)


def failing(base: dict, mine: list[dict], checker, path: str, control: set[str]) -> list[tuple[dict, list[str]]]:
    """The plants of one copy that make it fail where its control passes: all at once first, then one by one."""
    if not new_problems(checker, path, apply(base, mine), control):
        return []
    kept, out = [], []
    for p in mine:
        found = new_problems(checker, path, apply(base, kept + [p]), control)
        if found:
            out.append((p, found))
        else:
            kept.append(p)
    return out


# --- plant ------------------------------------------------------------------------------------------------------------
def plant_round(finals: list[str], round_: int, seed: int, root: str, store: st.Store, slots_doc: dict,
                checker=None, dry: bool = False) -> dict:
    records = [rc.load(p) for p in finals]
    ids = [r["id"] for r in records]
    if len(set(ids)) != len(ids):
        raise ValueError(f"a record is given twice: {ids}")
    texts = {}
    for path, r in zip(finals, records):
        t = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(path))), "reports", f"{r['id']}.texts.json")
        if os.path.isfile(t):
            texts[r["id"]] = rc.load(t)
    ctx = Context(store, slots_doc, seed, texts)
    bases = [reconciled_copy(r) for r in records]
    places = {k: [] for k in KINDS}
    for i, b in enumerate(bases):
        for k, found in candidates(i, b, ctx).items():
            places[k] += found
    rng = random.Random(seed)
    plants = select(places, rng)
    p_ids = [f"p{round_}-{i + 1:02d}" for i in range(len(records))]
    order = list(range(len(records)))
    rng.shuffle(order)  # the control of p-01 is not c-01: the pairing is in the key only
    c_ids = {rec: f"c{round_}-{n + 1:02d}" for n, rec in enumerate(order)}
    replaced = []
    if not dry:
        if os.path.isdir(root) and os.listdir(root):
            raise FileExistsError(f"{root} is not empty: a round is planted once; move the old one away first")
        for i, b in enumerate(bases):
            rc.save(reconciled_path(root, c_ids[i]), b)
        if checker is not None:
            bad = set()
            while True:
                failed = []
                for i, b in enumerate(bases):
                    mine = [p for p in plants if p["rec"] == i and p["kind"] not in SCRIPT_ONLY]
                    if mine:
                        control = {_norm(x) for x in checker(reconciled_path(root, c_ids[i]))}
                        failed += failing(b, mine, checker, reconciled_path(root, p_ids[i]), control)
                if not failed:
                    break
                for p, why in failed:
                    bad.add(place_id(p))
                    plants.remove(p)
                for p, why in failed:
                    pool = sorted(places[p["kind"]], key=lambda q: (q["rec"], q["item"], q["field"]))
                    random.Random(f"{seed}|replace|{len(bad)}").shuffle(pool)
                    new = pick(p["kind"], pool, plants, 1, bad)
                    plants += new
                    replaced.append({"kind": p["kind"], "copy": p_ids[p["rec"]], "item": p["item"], "field": p["field"],
                                     "problems": why, "by": [key_entry(q, p_ids[q["rec"]]) for q in new]})
    s_plants = sorted((p for p in plants if p["kind"] in SCRIPT_ONLY),
                      key=lambda p: (KINDS.index(p["kind"]), p["rec"], p["item"]))
    s_ids = [f"s{round_}-{n + 1:02d}" for n in range(len(s_plants))]
    sources = [os.path.abspath(x) for x in finals]
    key = {"round": round_, "seed": seed, "made": unit.now(),
           "copies": [{"copy": p_ids[i], "unit": ids[i], "role": "planted", "from": sources[i]} for i in range(len(ids))]
           + [{"copy": c_ids[i], "unit": ids[i], "role": "control", "from": sources[i]} for i in range(len(ids))]
           + [{"copy": s_ids[n], "unit": ids[p["rec"]], "role": "scriptOnly", "from": sources[p["rec"]]}
              for n, p in enumerate(s_plants)],
           "plants": [key_entry(p, p_ids[p["rec"]]) for p in sorted(
               (p for p in plants if p["kind"] not in SCRIPT_ONLY), key=lambda p: (p["rec"], KINDS.index(p["kind"]),
                                                                                  p["item"], p["field"]))]
           + [key_entry(p, s_ids[n]) for n, p in enumerate(s_plants)],
           "replaced": replaced,
           "counts": {k: {"places": len(places[k]), "planted": sum(1 for p in plants if p["kind"] == k)} for k in KINDS}}
    if dry:
        return key
    for i, (path, b) in enumerate(zip(finals, bases)):
        rc.save(reconciled_path(root, p_ids[i]), apply(b, [p for p in plants if p["rec"] == i and
                                                           p["kind"] not in SCRIPT_ONLY]))
        rc.save(reconciled_path(root, c_ids[i]), b)
        real = os.path.dirname(os.path.dirname(os.path.abspath(path)))  # the real unit's rules\ folder
        for cid in (p_ids[i], c_ids[i]):
            for suffix in ("checklist.json", "texts.json", "idmap.json"):
                src = os.path.join(real, "reports", f"{ids[i]}.{suffix}")
                if os.path.isfile(src):
                    os.makedirs(os.path.dirname(report_path(root, cid, suffix)), exist_ok=True)
                    shutil.copyfile(src, report_path(root, cid, suffix))
            src = os.path.join(real, "boundary-cases", f"{ids[i]}.json")
            if os.path.isfile(src):
                os.makedirs(os.path.dirname(boundary_path(root, cid)), exist_ok=True)
                shutil.copyfile(src, boundary_path(root, cid))
    for n, p in enumerate(s_plants):
        rc.save(reconciled_path(root, s_ids[n]), apply(bases[p["rec"]], [p]))
    rc.save(key_path(root), key)
    return key


# --- score: what each checker wrote ------------------------------------------------------------------------------------
def _load(path: str):
    return unit.load(path) if os.path.isfile(path) else None


def names(p: dict) -> set[str]:
    out = {p["item"], *p.get("from", [])}
    return out | {"court.section"} if p["item"] == "section" else out


def _cites(p: dict) -> list[dict]:
    """The cites a statement plant moved: the one the record had, and the one planted."""
    if p["kind"] not in STATEMENT_KINDS:
        return []
    return [c for c in (p.get("cite") or p["before"], p["after"]) if isinstance(c, dict) and c.get("code")]


LABEL = re.compile(r"^\[\s*(?P<code>\S+)\s+(?P<article>.+?)(?:\s*¶\s*(?P<p>\d+))?\s*\]$")


def script_findings(root: str, cid: str, p: dict) -> dict[str, set[str]]:
    """What the stage 6 scripts of one copy say about one plant's item: check-values, the unanswered neighbour flags,
    the store's refusals, the boundary runner's failed cases."""
    who, out = names(p), {}
    values = _load(report_path(root, cid, "values.reconciled.json"))
    out["check-values"] = {canon(x) for x in (values or {}).get("problems", []) if x.get("item") in who}
    flags = {f["id"]: f for f in _load(report_path(root, cid, "flags.reconciled.json")) or []}
    answered = _load(report_path(root, cid, "checklist-check.reconciled.json")) or {}
    out["flag-neighbours"] = {f"{f['code']} {f['article']} ¶{f['paragraph']} {f['statement']}"
                              for fid in answered.get("unansweredFlags", []) if (f := flags.get(fid))
                              and (f["statement"] in who or any(covers(c, f["code"], f["article"], f["paragraph"])
                                                                 for c in _cites(p)))}
    check = os.path.join(run_dir(root, cid), "check.reconciled.txt")
    lines = problem_lines(open(check, encoding="utf-8").read()) if os.path.isfile(check) else []
    out["rules check"] = {_norm(x) for x in lines if any(f"({n})" in x for n in who)}
    cases = {c["id"]: c for c in _load(boundary_path(root, cid)) or [] if isinstance(c, dict)}
    out["boundary"] = set()
    for r in unit._results(os.path.join(run_dir(root, cid), "cases", "boundaryReconciled.json")):
        if r.get("passed"):
            continue
        about = LABEL.match(((cases.get(r["caseId"]) or {}).get("expect") or {}).get("about", ""))
        on_cite = bool(about) and any(covers(c, about["code"], about["article"].strip(),
                                             int(about["p"]) if about["p"] else None) for c in _cites(p))
        out["boundary"] |= {f"{r['caseId']}: {d}" for d in r.get("differences", [])
                            if on_cite or any(d.startswith(f"{n}:") for n in who)}
    return out


def _attacks(path: str) -> list[dict]:
    doc = _load(path)
    if doc is None:
        return []
    return doc if isinstance(doc, list) else doc.get("attacks", [])


def agent_flags(root: str, cid: str, p: dict) -> dict[str, bool]:
    who = names(p)
    return {who_: any(rc.attack_targets(a) & who for a in _attacks(report_path(root, cid, suffix)))
            for who_, suffix in (("attack", "attacks.sonnet.json"), ("adversary", "attacks.adversary.json"),
                                 ("support", "support.json"))}


def value_now(record: dict, p: dict):
    _, x = find_item(record, p["item"])
    if x is None:
        return None
    if p["kind"] == "limitationForForfeiture":
        return {k: x.get(k) for k in p["after"]}
    return get_at(x, p["field"])


def back_in_basis(record: dict, cite: dict, items: list[str]) -> bool:
    """The cite is again in the basis of every item it was taken from."""
    stmts = rc.statements(record)
    for iid in items:
        _, x = find_item(record, iid)
        if x is None or not any(covers_all(stmts[b]["cite"], cite) for b in rc.basis_ids(x) if b in stmts):
            return False
    return bool(items)


def _condition_root(path: str) -> str:
    tokens = parse(path)
    n = 3 if tokens[0] in ("notRequiredWhen", "alternatives") else 1
    return "".join(f"[{t}]" if isinstance(t, int) else (f".{t}" if i else t) for i, t in enumerate(tokens[:n]))


def verifier_view(corrected: dict | None, planted: dict, p: dict) -> tuple[bool, bool]:
    """(changed, restored): did the verifier touch the planted field, and is the original value back."""
    if corrected is None:
        return False, False
    k = p["kind"]
    if k == "exceptionUncited":
        back = back_in_basis(corrected, p["cite"], p["from"])
        return back or p["item"] in rc.statements(corrected), back
    if k == "exemptionDropped":
        _, x = find_item(corrected, p["item"])
        _, was = find_item(planted, p["item"])
        now = (x or {}).get("notRequiredWhen") or []
        back = any(canon(e.get("when")) == canon(p["before"]["when"]) for e in now)
        return back or len(now) != len((was or {}).get("notRequiredWhen") or []), back
    if k == "doneBesideOpen":
        _, x = find_item(corrected, p["item"])
        means = (x or {}).get("acceptedMeans") or []
        back = bool(means) and all(any(m.get("id") == o["id"] or m.get("label") == o.get("label") for m in means)
                                   for o in p["before"])
        return bool(means), back
    now = value_now(corrected, p)
    if k in ("paragraphShifted", "wrongArticle"):
        back = same_cite(now, p["before"]) or back_in_basis(corrected, p["before"], p.get("from") or [])
        return back or not same_cite(now, p["after"]), back
    back = canon(now) == canon(p["before"])
    if not back and k == "conditionInverted":  # rewritten around the leaf, but the condition as it was
        root = _condition_root(p["field"])
        _, was = find_item(planted, p["item"])
        original = copy.deepcopy(get_at(was, root))
        set_at({"r": original}, "r" + p["field"][len(root):], p["before"])
        _, x = find_item(corrected, p["item"])
        back = x is not None and canon(get_at(x, root)) == canon(original)
    return back or canon(now) != canon(p["after"]), back


def new_open_points(corrected: dict | None, planted: dict, p: dict) -> bool:
    """An open point the verifier added (or rewrote) that names the plant's item."""
    if corrected is None:
        return False
    before = {canon(o) for o in planted.get("openPoints", [])}
    return any(set(o.get("refs") or []) & names(p) for o in corrected.get("openPoints", []) if canon(o) not in before)


def score_plant(root: str, p: dict, control: str | None) -> dict:
    cid = p["copy"]
    planted = rc.load(reconciled_path(root, cid))
    corrected = _load(candidate_path(root, cid))
    flagged = []
    mine = script_findings(root, cid, p)
    theirs = script_findings(root, control, p) if control else {}
    for checker, found in mine.items():
        if found - theirs.get(checker, set()):  # a finding the untouched control also shows is not about the plant
            flagged.append(checker)
    flagged += [w for w, hit in agent_flags(root, cid, p).items() if hit]
    changed, restored = verifier_view(corrected, planted, p)
    opened = new_open_points(corrected, planted, p)
    if changed or opened:
        flagged.append("verifier")
    return {"kind": p["kind"], "flagged": flagged, "caught": bool(flagged) and (restored or opened),
            "byOpenPoint": opened and not restored}


def score_script_only(root: str, p: dict, store: st.Store, checker) -> dict:
    path = reconciled_path(root, p["copy"])
    flagged = []
    _, problems = rc.quote_record(rc.load(path), store)
    if any(x["statement"] == p["item"] for x in problems):
        flagged.append("quote")
    if any(f"({p['item']})" in line for line in checker(path)):
        flagged.append("rules check")
    return {"kind": p["kind"], "flagged": flagged, "caught": bool(flagged), "byOpenPoint": False}


def control_changes(root: str, cid: str, unit_id: str) -> tuple[int, list[dict]]:
    """False alarms on an untouched copy: the attacks the verifier marked fixed, and the items it changed."""
    registers = [report_path(root, cid, "attacks.json")]
    k = 1
    while os.path.isfile(report_path(root, cid, f"attacks.followup{k}.json")):
        registers.append(report_path(root, cid, f"attacks.followup{k}.json"))
        k += 1
    entries = rc.latest_verdicts([a for path in registers for a in _attacks(path)]) or []
    fixed = [a for a in entries if a.get("verdict") == "fixed"]
    items = {}
    for a in fixed:
        for t in rc.attack_targets(a):
            items.setdefault(t, set()).add(a.get("id", "?"))
    corrected = _load(candidate_path(root, cid))
    if corrected is not None:
        was = {rc.item_id(k_, x): canon(x) for k_, x in rc.items(rc.load(reconciled_path(root, cid)))}
        now = {rc.item_id(k_, x): canon(x) for k_, x in rc.items(corrected)}
        for iid in sorted(set(was) | set(now)):
            if was.get(iid) != now.get(iid):
                items.setdefault(iid, set())
    return len(fixed), [{"copy": cid, "unit": unit_id, "item": i, "attacks": sorted(a)} for i, a in sorted(items.items())]


def score_round(round_: int, root: str, store: st.Store | None, checker, date: str | None = None) -> dict:
    key = unit.load(key_path(root))
    copies = {c["copy"]: c for c in key["copies"]}
    controls = {c["unit"]: c["copy"] for c in key["copies"] if c["role"] == "control"}
    kinds = {k: {"planted": 0, "flagged": {}, "caught": 0} for k in KINDS}
    by_open_point = {k: 0 for k in KINDS}
    for p in key["plants"]:
        if copies[p["copy"]]["role"] == "scriptOnly":
            if checker is None or store is None:
                raise RuntimeError("the script-only copies need the rules tool and the store")
            r = score_script_only(root, p, store, checker)
        else:
            r = score_plant(root, p, controls.get(copies[p["copy"]]["unit"]))
        k = kinds[p["kind"]]
        k["planted"] += 1
        for w in r["flagged"]:
            k["flagged"][w] = k["flagged"].get(w, 0) + 1
        k["caught"] += int(r["caught"])
        by_open_point[p["kind"]] += int(r["byOpenPoint"])
    changed, items = 0, []
    for unit_id, cid in sorted(controls.items(), key=lambda kv: kv[1]):
        n, found = control_changes(root, cid, unit_id)
        changed += n
        items += found
    count = {role: sum(1 for c in key["copies"] if c["role"] == role) for role in ("planted", "control", "scriptOnly")}
    return {"round": round_, "date": date or dt.date.today().isoformat(),
            "copies": {"planted": count["planted"], "controls": count["control"], "scriptOnly": count["scriptOnly"]},
            "kinds": kinds, "controls": {"changedByVerifier": changed, "items": items},
            "_byOpenPoint": by_open_point}


# --- the rates and what they decide ------------------------------------------------------------------------------------
def status(rounds: list[dict]) -> dict[str, str]:
    """The table "What the rates decide": round 1 alone needs every plant caught and at least 3 plants; once round 2
    has run, 90 % over both rounds."""
    last = max((r["round"] for r in rounds), default=0)
    out = {}
    for k in KINDS:
        planted = sum(r["kinds"].get(k, {}).get("planted", 0) for r in rounds)
        caught = sum(r["kinds"].get(k, {}).get("caught", 0) for r in rounds)
        if planted == 0:
            out[k] = "notRun"
        elif last <= 1:
            out[k] = "passed" if planted >= 3 and caught == planted else "weak"
        else:
            out[k] = "passed" if caught >= 0.9 * planted else "weak"
    return out


def wilson(k: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return 0.0, 1.0
    p = k / n
    centre = (p + z * z / (2 * n)) / (1 + z * z / n)
    half = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / (1 + z * z / n)
    return max(0.0, centre - half), min(1.0, centre + half)


def pct(x: float) -> str:
    return f"{100 * x:.0f} %"


def report_md(doc: dict, extra: dict[int, dict]) -> str:
    lines = ["# Planted errors: the rates", "",
             "Written by `scripts/planted.py score` ([../../specs/planted-errors.md](../../specs/planted-errors.md)); "
             "the machine copy is `planted-errors.json`. Only the rates are here: the copies and the key stay outside the "
             "repository.", "", f"> {CANNOT_SHOW}", ""]
    for r in doc["rounds"]:
        bop = extra.get(r["round"], {})
        c = r["copies"]
        lines += [f"## Round {r['round']} ({r['date']})", "",
                  f"Copies: {c['planted']} planted, {c['controls']} controls, {c['scriptOnly']} script-only. No adversary "
                  "ran on the copies (it searches the web and has nothing to find in a planted record).", "",
                  "| Kind | Planted | Flagged by | Caught | Rate |", "|---|---|---|---|---|"]
        for k in KINDS:
            x = r["kinds"].get(k, {"planted": 0, "flagged": {}, "caught": 0})
            flagged = ", ".join(f"{w} {n}" for w, n in sorted(x["flagged"].items())) or "none"
            note = f" ({bop[k]} by an open point)" if bop.get(k) else ""
            rate = pct(x["caught"] / x["planted"]) if x["planted"] else "not planted"
            lines.append(f"| `{k}` | {x['planted']} | {flagged} | {x['caught']}{note} | {rate} |")
        ctl = r["controls"]
        lines += ["", f"Controls: the verifier marked {ctl['changedByVerifier']} attack(s) `fixed` on an untouched record "
                      "(false alarms)."]
        for it in ctl["items"]:
            lines.append(f"- `{it['unit']}` item `{it['item']}`" + (f" (attacks {', '.join(it['attacks'])})"
                                                                     if it["attacks"] else " (changed, no fixed attack)"))
        if ctl["changedByVerifier"] >= 3:
            lines.append("- Three or more in this round: the verifier's prompt is reviewed before the next units, and the "
                         "owner is told.")
        lines.append("")
    lines += ["## Over the rounds run so far", "", f"> {CANNOT_SHOW}", "",
              "| Kind | Planted | Caught | Rate | 95 % interval (Wilson) | Status |", "|---|---|---|---|---|---|"]
    for k in KINDS:
        n = sum(r["kinds"].get(k, {}).get("planted", 0) for r in doc["rounds"])
        c = sum(r["kinds"].get(k, {}).get("caught", 0) for r in doc["rounds"])
        lo, hi = wilson(c, n)
        lines.append(f"| `{k}` | {n} | {c} | {pct(c / n) if n else 'none'} | "
                     f"{pct(lo) + ' to ' + pct(hi) if n else 'none'} | {doc['status'][k]} |")
    under = [k for k in KINDS if (n := sum(r["kinds"].get(k, {}).get("planted", 0) for r in doc["rounds"]))
             and sum(r["kinds"].get(k, {}).get("caught", 0) for r in doc["rounds"]) < 0.8 * n]
    lines += ["", "A kind that could be planted fewer than five times in a round is planted as often as the records allow; "
                  "the planted column gives the count. `weak` puts `checkerWeak: <kind>` on every item of the sorts the "
                  "kind speaks for; `notRun` gives `plantedNotRun` (rules/GRADES.md, condition 7)."]
    if under:
        lines += ["", "Under 80 % (the checker named for the kind, or its prompt, is fixed once and the kind replanted): "
                  + ", ".join(f"`{k}`" for k in under) + "."]
    return "\n".join(lines) + "\n"


def write_reports(entry: dict, reports: str) -> dict:
    """planted-errors.json is rewritten whole: the rounds already scored, with this one replaced."""
    path = os.path.join(reports, "planted-errors.json")
    old = _load(path) or {"rounds": []}
    extra = {entry["round"]: entry.pop("_byOpenPoint", {})}
    rounds = sorted([r for r in old["rounds"] if r["round"] != entry["round"]] + [entry], key=lambda r: r["round"])
    doc = {"rounds": rounds, "status": status(rounds)}
    rc.save(path, doc)
    with open(os.path.join(reports, "planted-errors.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write(report_md(doc, extra))
    return doc


# --- command line -------------------------------------------------------------------------------------------------------
def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="planted")
    sub = p.add_subparsers(dest="cmd", required=True)
    pl = sub.add_parser("plant")
    pl.add_argument("records", nargs="+", help="rules/final/<unit>.json")
    sc = sub.add_parser("score")
    for s in (pl, sc):
        s.add_argument("--round", type=int, required=True)
        s.add_argument("--root", help="default <work>\\planted\\round-<n>")
        s.add_argument("--store", default=unit.STORE)
    pl.add_argument("--seed", type=int, required=True)
    pl.add_argument("--slots", default=SLOTS)
    pl.add_argument("--dry-run", action="store_true", help="print what would be planted; write nothing")
    sc.add_argument("--reports", default=REPORTS)
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    root = a.root or default_root(a.round)
    store = st.Store(a.store)
    if a.cmd == "plant":
        checker = None
        if not a.dry_run:
            tool = unit.published_tool()
            if tool is None:
                print("the rules tool is not published: run unit.py tool")
                return 2
            checker = tool_checker(tool, a.store)
        try:
            key = plant_round(a.records, a.round, a.seed, root, store, rc.load(a.slots), checker, a.dry_run)
        except FileExistsError as e:
            print("REFUSED", e)
            return 2
        for k in KINDS:
            c = key["counts"][k]
            where = sorted({x["copy"] for x in key["plants"] if x["kind"] == k})
            print(f"{k:24} places {c['places']:4}  planted {c['planted']}" + (f"  ({', '.join(where)})" if where else ""))
        for r in key["replaced"]:
            print(f"replaced {r['kind']} {r['copy']} {r['item']} {r['field']}: {r['problems'][0]}"
                  + ("" if r["by"] else " (no other place left)"))
        print("dry run: nothing written" if a.dry_run else f"key -> {key_path(root)}")
        return 0
    tool = unit.published_tool()
    checker = tool_checker(tool, a.store) if tool else None
    entry = score_round(a.round, root, store, checker)
    doc = write_reports(entry, a.reports)
    for k in KINDS:
        x = entry["kinds"][k]
        print(f"{k:24} planted {x['planted']}  caught {x['caught']}  {doc['status'][k]}")
    print(f"controls: {entry['controls']['changedByVerifier']} false alarm(s) -> {a.reports}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
