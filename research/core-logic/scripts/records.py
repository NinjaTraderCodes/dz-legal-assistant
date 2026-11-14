"""The scripts that check a rule record (docs/research/core-logic/specs/record-checks.md).

Agents point, scripts quote and check. A file an agent wrote is never rewritten: every script writes beside it.

    python records.py quote <record.json> [--out <quoted.json>]          -> the quoted copy (quoted/ next to the record)
    python records.py check-values <quoted.json> --report <file>
    python records.py flag-neighbours <quoted.json> --report <file>
    python records.py merge-checklists <unit> --a <a.checklist.json> --b <b.checklist.json> [--codex <file>]
                      [--november <file>] [--claim <claim id>] --out <file>
    python records.py compare <a-quoted.json> <b-quoted.json> --differences <file> --idmap <file>
    python records.py add-cases <differences.json> [--aOnB f] [--bOnA f] [--boundaryA f] [--boundaryB f] [--boundaryAB f]
    python records.py compare-c <c.json> <a-quoted.json> <b-quoted.json> --out <file> [--texts <unit.texts.json>]
    python records.py flatten <claim.json> --modules <dir> --out <file>
    python records.py grade <quoted.json> [--idmap f] [--differences f] [--attacks f] --out <final.json> [--report f]
    python records.py check-checklist <record.json> --checklist <file> [--flags <file> ...] --report <file>
    python records.py inventory <final records...> --out <file>
    python records.py unit-texts <unit> <code:article>... --out <reports/unit.texts.json>
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import store as st  # noqa: E402
import textcmp  # noqa: E402

REPO = st.REPO
CLAIMS = os.path.join(REPO, "data", "law", "claims")


def load(path: str):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save(path: str, doc) -> None:
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=2)
        f.write("\n")


def sha(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


# --- the items of a record ---------------------------------------------------------------------------------------------
CRITICAL_KINDS = ("deadline", "priorStep", "exit", "section")


def items(record: dict):
    """(kind, item) for every item that can be graded, statements first."""
    for s in record.get("basis", []):
        yield "statement", s
    for kind, key in (("element", "elements"), ("request", "requests"), ("deadline", "deadlines"),
                      ("priorStep", "priorSteps"), ("document", "documents"), ("boundary", "boundaries")):
        for x in record.get(key, []):
            yield kind, x
    court = record.get("court", {})
    for x in court.get("territory", []):
        yield "territory", x
    for x in court.get("exits", []):
        yield "exit", x
    if "section" in court:
        yield "section", court["section"]


def item_id(kind: str, item: dict) -> str:
    return item.get("id") or kind


def basis_ids(item: dict) -> list[str]:
    """Every statement an item rests on: its basis, its counting's, its exemptions', its alternatives', its proofs'."""
    ids = list(item.get("basis", []))
    if isinstance(item.get("counting"), dict):
        ids += item["counting"].get("basis", [])
    for key in ("notRequiredWhen", "alternatives", "proof", "acceptedMeans", "readings"):
        for x in item.get(key, []) or []:
            if isinstance(x, dict):
                ids += x.get("basis", [])
    return list(dict.fromkeys(ids))


def statements(record: dict) -> dict:
    return {s["id"]: s for s in record.get("basis", [])}


def quoted_text(stmts: dict, ids: list[str]) -> str:
    return "\n".join(q["text"] for i in ids if i in stmts for q in stmts[i].get("quote", []))


# --- quote-records -----------------------------------------------------------------------------------------------------
def quote_record(record: dict, store: st.Store) -> tuple[dict, list[dict]]:
    """The quoted copy: `quote` and `source` written into every statement from the store. Fails (returns problems)
    for a cite that is not in the store, a version the store does not hold, a repealed article, or a paragraph beyond
    the article's last."""
    out = copy.deepcopy(record)
    problems = []
    for s in out.get("basis", []):
        c = s["cite"]
        e = store.get(c["code"], c["article"], c.get("version"))
        where = f"{s['id']}: {c['code']} {c['article']}" + (f" ({c['version']})" if c.get("version") else "")
        if e is None:
            problems.append({"statement": s["id"], "problem": "notInStore" if not c.get("version") else "versionNotInStore",
                             "cite": c, "message": f"{where} is not in the store"})
            continue
        if e["repealed"] or not e["inForce"] and not c.get("version"):
            problems.append({"statement": s["id"], "problem": "repealed", "cite": c, "message": f"{where} is repealed"})
            continue
        wanted = c.get("paragraphs") or list(range(1, len(e["paragraphs"]) + 1))
        beyond = [n for n in wanted if n < 1 or n > len(e["paragraphs"])]
        if beyond:
            problems.append({"statement": s["id"], "problem": "noSuchParagraph", "cite": c,
                             "message": f"{where} has {len(e['paragraphs'])} paragraph(s); cited {beyond}"})
            continue
        s["quote"] = [{"paragraph": n, "text": e["paragraphs"][n - 1], "sha256": sha(e["paragraphs"][n - 1])}
                      for n in wanted]
        source = {"tier": e["tier"], "store": e["store"]}
        if e.get("textAsOf"):
            source["textAsOf"] = e["textAsOf"]
        source["ledgerRefs"] = list(e.get("ledgerRefs") or [])
        source["ledgerClean"] = bool(e.get("ledgerClean"))
        s["source"] = source
    return out, problems


def quote_changes(old: dict | None, new: dict) -> list[str]:
    """Statements whose quoted text changed since the last quoted copy (a store change): reported, never silent."""
    if not old:
        return []
    before = {s["id"]: [q["sha256"] for q in s.get("quote", [])] for s in old.get("basis", [])}
    return [s["id"] for s in new.get("basis", []) if s["id"] in before and before[s["id"]]
            != [q["sha256"] for q in s.get("quote", [])]]


# --- reading periods from Arabic words ------------------------------------------------------------------------------
UNITS = {"يوم": ("day", 1), "ايام": ("day", None), "يوما": ("day", None), "يومين": ("day", 2), "يومان": ("day", 2),
         "شهر": ("month", 1), "اشهر": ("month", None), "شهور": ("month", None), "شهرا": ("month", None),
         "شهرين": ("month", 2), "شهران": ("month", 2),
         "سنه": ("year", 1), "سنوات": ("year", None), "سنين": ("year", None), "سنتين": ("year", 2), "سنتان": ("year", 2),
         "عام": ("year", 1), "اعوام": ("year", None), "عامين": ("year", 2)}
ONES = {"واحد": 1, "واحده": 1, "احد": 1, "احدي": 1, "اثنان": 2, "اثنين": 2, "اثنتان": 2, "اثنتين": 2, "اثنا": 2,
        "اثني": 2, "اثنتا": 2, "اثنتي": 2, "ثلاثه": 3, "ثلاث": 3, "اربعه": 4, "اربع": 4, "خمسه": 5, "خمس": 5,
        "سته": 6, "ست": 6, "سبعه": 7, "سبع": 7, "ثمانيه": 8, "ثماني": 8, "ثمان": 8, "تسعه": 9, "تسع": 9,
        "عشره": 10, "عشر": 10}
TENS = {"عشرون": 20, "عشرين": 20, "ثلاثون": 30, "ثلاثين": 30, "اربعون": 40, "اربعين": 40, "خمسون": 50, "خمسين": 50,
        "ستون": 60, "ستين": 60, "سبعون": 70, "سبعين": 70, "ثمانون": 80, "ثمانين": 80, "تسعون": 90, "تسعين": 90,
        "مائه": 100, "مئه": 100}
PREFIXES = ("ب", "ل", "و", "ك")


def _norm_word(w: str) -> str:
    return textcmp.norm2(w, keep_lines=False)


def _strip_prefix(w: str, table: dict) -> str | None:
    """The word as found in `table`, with or without a one-letter prefix (ب، ل، و، ك) and the article ال."""
    for cand in (w, w[1:] if w[:1] in PREFIXES else None):
        if not cand:
            continue
        for c in (cand, cand[2:] if cand.startswith("ال") else None):
            if c and c in table:
                return c
    return None


def read_periods(text: str, bare: bool = True) -> list[dict]:
    """Every period written in the text: {value, unit, words}. Number words in every case and gender, compound
    (خمسة عشر, واحد وعشرين), the digits in brackets («ستة (6) أشهر»: both must agree, or the period is marked
    `conflict`), the dual (شهرين), and, when `bare`, a bare unit (شهر = 1; not in running text, where «من يوم الطلب»
    is no period)."""
    words = [w for w in re.split(r"\s+", textcmp.norm2(text, keep_lines=False)) if w]
    out, i = [], 0
    while i < len(words):
        j, value, nums = i, None, []
        # a number in words: ONES [عشر|عشره] | ONES و TENS | TENS | digits
        while j < len(words):
            w = words[j]
            one = _strip_prefix(w, ONES)
            ten = _strip_prefix(w, TENS)
            if one and not nums:
                nums.append(ONES[one])
                j += 1
                if j < len(words) and words[j] in ("عشر", "عشره") and ONES[one] < 10:
                    nums[-1] += 10
                    j += 1
                continue
            if ten and (not nums or words[j].startswith("و")):
                nums.append(TENS[ten])
                j += 1
                continue
            break
        if nums:
            value = sum(nums)
        digits = None
        if j < len(words) and re.fullmatch(r"\d+", words[j]):
            digits = int(words[j])
            j += 1
        if j < len(words):
            unit = _strip_prefix(words[j], UNITS)
            if unit:
                name, implied = UNITS[unit]
                if value is None and digits is None and (bare or implied == 2):
                    value = implied
                if value is None:
                    value = digits
                if value is not None:
                    out.append({"value": value, "unit": name, "words": " ".join(words[i:j + 1]),
                                "conflict": digits is not None and value != digits})
                    i = j + 1
                    continue
        i += 1 if j == i else j - i
    return out


FORFEITURE_WORDS = ("سقوط", "تحت طائله", "عدم القبول", "لا تقبل", "يسقط", "رفض الدعوي شكلا")
LIMITATION_WORDS = ("تقادم", "يتقادم", "تسقط بالتقادم")
EN_NUMBERS = {"one": 1, "two": 2, "three": 3, "four": 4, "five": 5, "six": 6, "seven": 7, "eight": 8, "nine": 9,
              "ten": 10, "eleven": 11, "twelve": 12, "fifteen": 15, "twenty": 20, "thirty": 30, "sixty": 60}


def _contains(haystack: str, needle: str) -> bool:
    return textcmp.norm2(needle, keep_lines=False) in textcmp.norm2(haystack, keep_lines=False)


def numbers_in(text: str) -> set[int]:
    t = text.translate(textcmp.DIGITS)
    found = {int(x) for x in re.findall(r"\d+", t)}
    found |= {p["value"] for p in read_periods(text, bare=False)}
    for w, n in EN_NUMBERS.items():
        if re.search(rf"\b{w}\b", t, re.I):
            found.add(n)
    return found


def message_articles(text: str) -> set[str]:
    """Article numbers a message names: «المادة 504», «المادتين 19 و 37», «art. 504», «arts. 19 and 37»."""
    t = text.translate(textcmp.DIGITS)
    out = set()
    for m in re.finditer(r"(?:المادت(?:ين|ان)|المواد|المادة|arts?\.|articles?)\s+((?:\d+(?:\s*(?:مكرر)(?:\s*\d+)?)?"
                         r"(?:\s*(?:و|،|,|and|to|إلى|الى)\s*)?)+)", t, re.I):
        for n in re.finditer(r"\d+(?:\s*مكرر(?:\s*\d+)?)?", m.group(1)):
            out.add(re.sub(r"\s+", " ", n.group(0)).strip())
    return out


def check_values(record: dict) -> dict:
    """The numbers are read from the text (record-checks.md, `check-values`). Returns {problems, notes}."""
    stmts = statements(record)
    problems, notes = [], []
    for d in record.get("deadlines", []):
        text = quoted_text(stmts, d.get("basis", []))
        ex = d.get("extract")
        if not ex:
            problems.append({"item": d["id"], "problem": "noExtract"})
        else:
            if ex.get("period") and ex["period"] not in text:
                problems.append({"item": d["id"], "problem": "periodWordsNotInQuote", "words": ex["period"]})
            if ex.get("period"):
                read = read_periods(ex["period"])
                if not read:
                    problems.append({"item": d["id"], "problem": "periodNotRead", "words": ex["period"]})
                elif read[0]["conflict"]:
                    problems.append({"item": d["id"], "problem": "wordsAndDigitsDiffer", "words": ex["period"]})
                elif (read[0]["value"], read[0]["unit"]) != (d["period"]["value"], d["period"]["unit"]):
                    problems.append({"item": d["id"], "problem": "valueMismatch", "read": read[0], "field": d["period"]})
            if ex.get("sanction") and ex["sanction"] not in text:
                problems.append({"item": d["id"], "problem": "sanctionWordsNotInQuote", "words": ex["sanction"]})
        n2 = textcmp.norm2(text, keep_lines=False)
        has_forfeiture = any(textcmp.norm2(w, keep_lines=False) in n2 for w in FORFEITURE_WORDS)
        has_limitation = any(textcmp.norm2(w, keep_lines=False) in n2 for w in LIMITATION_WORDS)
        if d["kind"] == "forfeiture" and not has_forfeiture:
            problems.append({"item": d["id"], "problem": "kindNotInText", "kind": "forfeiture"})
        if d["kind"] == "limitation" and not has_limitation:
            problems.append({"item": d["id"], "problem": "kindNotInText", "kind": "limitation"})
        if not has_forfeiture and not has_limitation:
            notes.append({"item": d["id"], "note": "interpretive: the quoted text has no word of forfeiture or limitation"})
        if isinstance(d.get("counting"), dict):
            for b in d["counting"].get("basis", []):
                if b not in stmts or not stmts[b].get("quote"):
                    problems.append({"item": d["id"], "problem": "countingBasisNotQuoted", "statement": b})
    for s in record.get("basis", []):
        if not s.get("quote"):
            continue
        text = "\n".join(q["text"] for q in s["quote"])
        have = numbers_in(text)
        own = {int(x) for x in re.findall(r"\d+", s["cite"]["article"])} | set(s["cite"].get("paragraphs") or [])
        for lang in ("ar", "en"):
            said = numbers_in(s["says"].get(lang, ""))
            missing = sorted(n for n in said - have - own)
            if missing:
                problems.append({"item": s["id"], "problem": "numberNotInQuote", "lang": lang, "numbers": missing})
    for kind, x in items(record):
        if kind not in ("deadline", "priorStep", "exit", "section") or not x.get("message"):
            continue
        cited = {stmts[b]["cite"]["article"] for b in basis_ids(x) if b in stmts}
        for lang in ("ar", "en"):
            msg = x["message"].get(lang, "")
            named = message_articles(msg)
            stray = sorted(a for a in named if a not in cited)
            if stray:
                problems.append({"item": item_id(kind, x), "problem": "messageArticleNotInBasis", "lang": lang,
                                 "articles": stray})
            if kind == "deadline":
                periods = read_periods(msg, bare=False) if lang == "ar" else []
                wrong = [p for p in periods if (p["value"], p["unit"]) != (x["period"]["value"], x["period"]["unit"])]
                if wrong:
                    problems.append({"item": x["id"], "problem": "messagePeriodDiffers", "read": wrong})
    return {"problems": problems, "notes": notes}


# --- flag-neighbours ---------------------------------------------------------------------------------------------------
EXCEPTION_WORDS = ("غير أنه", "غير أن", "إلا أنه", "إلا إذا", "ما لم", "على أنه", "ويستثنى", "باستثناء", "دون الإخلال",
                   "مع مراعاة", "لا تطبق", "لا يسري", "ويجوز")


def _opens_with_exception(paragraph: str) -> str | None:
    head = " ".join(textcmp.norm2(paragraph, keep_lines=False).split()[:10])
    for w in EXCEPTION_WORDS:
        n = textcmp.norm2(w, keep_lines=False)
        if re.search(rf"(^|\s){re.escape(n)}(\s|$)", head) or (n.startswith("و") and head.startswith(n)):
            return w
        if head.startswith("و" + n) or head.startswith("ف" + n):
            return w
    return None


def _neighbours(store: st.Store, code: str, article: str) -> tuple[dict | None, dict | None]:
    entries = sorted((e for (c, _), e in store.in_force.items() if c == code), key=lambda e: st._num_key(e["article"]))
    numbers = [e["article"] for e in entries]
    if article not in numbers:
        return None, None
    i = numbers.index(article)
    return (entries[i - 1] if i > 0 else None), (entries[i + 1] if i + 1 < len(entries) else None)


def flag_neighbours(record: dict, store: st.Store) -> list[dict]:
    flags = []
    for s in record.get("basis", []):
        c = s["cite"]
        e = store.get(c["code"], c["article"], c.get("version"))
        if e is None:
            continue
        cited = set(c.get("paragraphs") or range(1, len(e["paragraphs"]) + 1))
        for n, p in enumerate(e["paragraphs"], 1):
            if n in cited:
                continue
            word = _opens_with_exception(p)
            if word:
                flags.append({"id": f"f-{s['id']}-p{n}", "statement": s["id"], "code": c["code"], "article": c["article"],
                              "paragraph": n, "word": word, "text": p, "kind": "uncitedParagraph"})
        prev, nxt = _neighbours(store, c["code"], c["article"])
        for side, ne in (("previous", prev), ("next", nxt)):
            if ne is None or not ne["paragraphs"]:
                continue
            word = _opens_with_exception(ne["paragraphs"][0])
            refers = re.search(rf"(المادة|المادتين|المواد)\s+[^.]*?\b{re.escape(c['article'])}\b",
                               "\n".join(ne["paragraphs"]).translate(textcmp.DIGITS))
            if word or refers:
                flags.append({"id": f"f-{s['id']}-{side}", "statement": s["id"], "code": c["code"],
                              "article": ne["article"], "paragraph": 1, "word": word or f"refers to {c['article']}",
                              "text": ne["paragraphs"][0], "kind": f"{side}Article"})
    # one flag per (code, article, paragraph): two statements of the same article share it
    seen, out = set(), []
    for f in flags:
        k = (f["code"], f["article"], f["paragraph"], f["kind"])
        if k not in seen:
            seen.add(k)
            out.append(f)
    return out


# --- merge-checklists --------------------------------------------------------------------------------------------------
def catalogue_claim(claim_id: str) -> dict | None:
    for name in sorted(os.listdir(CLAIMS)):
        if name.endswith(".json"):
            for c in load(os.path.join(CLAIMS, name)).get("claims", []):
                if c["id"] == claim_id:
                    return c
    return None


def catalogue_questions(claim: dict) -> list[str]:
    """The catalogue's own answers turned into questions; the derivers never see them, the final record answers them."""
    out = []
    for a in claim.get("articles", []):
        out.append(f"Is {a['code']} art. {a['number']} the right basis for this claim ({a.get('role')}: "
                   f"{(a.get('why') or {}).get('en', '')}), and which paragraph?")
    for p in claim.get("prerequisites", []):
        out.append(f"Prerequisite in the catalogue: {p.get('en') or p.get('ar')}. Which text requires it, and when is it "
                   f"not required?")
    for d in claim.get("requiredDocuments", []):
        out.append(f"Required document in the catalogue: «{d}». Which text requires it?")
    for r in claim.get("suggestedRequests", []):
        out.append(f"Suggested request in the catalogue: «{r}». On which text does it rest?")
    return out


def merge_checklists(sources: dict) -> list[dict]:
    """sources: from-name -> list of items (strings or {item}). Exact duplicates merge their `from`; near-duplicates
    stay (a longer list costs nothing)."""
    merged, index = [], {}
    for origin, lines in sources.items():
        for line in lines or []:
            text = (line.get("item") if isinstance(line, dict) else line).strip()
            key = re.sub(r"\s+", " ", text).strip().lower()
            if key in index:
                if origin not in merged[index[key]]["from"]:
                    merged[index[key]]["from"].append(origin)
                continue
            index[key] = len(merged)
            merged.append({"id": f"k{len(merged) + 1}", "item": text, "from": [origin]})
    return merged


# --- compare-derivations -----------------------------------------------------------------------------------------------
def _stmt_key(s: dict) -> tuple:
    c = s["cite"]
    return (c["code"], c["article"], c.get("version") or "", tuple(sorted(c.get("paragraphs") or [])))


def _main_article(rec: dict, item: dict):
    stmts = statements(rec)
    for b in item.get("basis", []):
        if b in stmts:
            c = stmts[b]["cite"]
            return (c["code"], c["article"])
    return None


def _cited_articles(rec: dict, ids: list) -> list:
    stmts = statements(rec)
    return sorted({(stmts[i]["cite"]["code"], stmts[i]["cite"]["article"]) for i in ids if i in stmts})


def _canon(v) -> str:
    return json.dumps(v, ensure_ascii=False, sort_keys=True)


def compare_derivations(a: dict, b: dict) -> dict:
    """A against B on content (record-checks.md): the difference list, the id map, the agreement rate per kind."""
    diffs, idmap, rate = [], [], {}

    def diff(kind, field_name, va, vb, ida, idb, matched_on):
        diffs.append({"id": f"x{len(diffs) + 1}", "kind": kind, "field": field_name, "a": va, "b": vb,
                      "aId": ida, "bId": idb, "matchedOn": matched_on})

    def tally(kind, equal):
        r = rate.setdefault(kind, [0, 0])
        r[0] += 1 if equal else 0
        r[1] += 1

    def field(kind, name, xa, xb, ida, idb, matched, va=None, vb=None, given=False):
        if not given:
            va, vb = xa.get(name), xb.get(name)
        equal = _canon(va) == _canon(vb)
        tally(kind, equal)
        if not equal:
            diff(kind, name, va, vb, ida, idb, matched)

    sa = {_stmt_key(s): s for s in a.get("basis", [])}
    sb = {_stmt_key(s): s for s in b.get("basis", [])}
    for k in sorted(set(sa) | set(sb), key=str):
        x, y = sa.get(k), sb.get(k)
        if x and y:
            idmap.append({"kind": "statement", "a": x["id"], "b": y["id"], "reconciled": None})
            field("statement", "role", x, y, x["id"], y["id"], "cite")
            field("statement", "interpretive", x, y, x["id"], y["id"], "cite",
                  bool(x.get("interpretive")), bool(y.get("interpretive")), given=True)
        else:
            tally("statement", False)
            diff("statement", "cite", x and x["cite"], y and y["cite"], x and x["id"], y and y["id"], "cite")
            idmap.append({"kind": "statement", "a": x and x["id"], "b": y and y["id"], "reconciled": None})

    def match(kind, la, lb, keys):
        """Pairs by the first key function that matches; unmatched items are differences."""
        left, right, pairs = list(la), list(lb), []
        for keyf, label in keys:
            for x in list(left):
                kx = keyf(a, x)
                if kx is None:
                    continue
                y = next((y for y in right if keyf(b, y) == kx), None)
                if y is not None:
                    pairs.append((x, y, label))
                    left.remove(x)
                    right.remove(y)
        for x in left:
            tally(kind, False)
            diff(kind, "present", x.get("id"), None, x.get("id"), None, "none")
            idmap.append({"kind": kind, "a": x.get("id"), "b": None, "reconciled": None})
        for y in right:
            tally(kind, False)
            diff(kind, "present", None, y.get("id"), None, y.get("id"), "none")
            idmap.append({"kind": kind, "a": None, "b": y.get("id"), "reconciled": None})
        for x, y, label in pairs:
            idmap.append({"kind": kind, "a": x.get("id"), "b": y.get("id"), "reconciled": None})
        return pairs

    def counting(rec, d):
        c = d.get("counting")
        return c and {"method": c.get("method"), "extendToWorkingDay": c.get("extendToWorkingDay"),
                      "articles": _cited_articles(rec, c.get("basis", []))}

    for x, y, m in match("deadline", a.get("deadlines", []), b.get("deadlines", []),
                         [(lambda r, d: _main_article(r, d), "mainArticle"),
                          (lambda r, d: (d["kind"], _canon(d["period"])), "kindAndPeriod")]):
        for name in ("kind", "period", "raisedBy", "effectWhenExpired", "when"):
            field("deadline", name, x, y, x["id"], y["id"], m)
        field("deadline", "startsFrom", x, y, x["id"], y["id"], m,
              {k: x["startsFrom"].get(k) for k in ("slot", "event")},
              {k: y["startsFrom"].get(k) for k in ("slot", "event")}, given=True)
        field("deadline", "counting", x, y, x["id"], y["id"], m, counting(a, x), counting(b, y), given=True)
    for x, y, m in match("priorStep", a.get("priorSteps", []), b.get("priorSteps", []), [(lambda r, p: p["kind"], "kind")]):
        for name in ("requiredWhen", "effectWhenMissing", "effectWhenUnknown"):
            field("priorStep", name, x, y, x["id"], y["id"], m)
        field("priorStep", "notRequiredWhen", x, y, x["id"], y["id"], m,
              sorted(_canon(e.get("when")) for e in x.get("notRequiredWhen", [])),
              sorted(_canon(e.get("when")) for e in y.get("notRequiredWhen", [])), given=True)
    ca, cb = a.get("court", {}), b.get("court", {})
    if "section" in ca or "section" in cb:
        xa, xb = ca.get("section", {}), cb.get("section", {})
        field("section", "value", xa, xb, "section", "section", "section")
        field("section", "alternatives", xa, xb, "section", "section", "section",
              sorted(_canon({"when": t.get("when"), "value": t.get("value")}) for t in xa.get("alternatives", [])),
              sorted(_canon({"when": t.get("when"), "value": t.get("value")}) for t in xb.get("alternatives", [])),
              given=True)
        idmap.append({"kind": "section", "a": "section" if xa else None, "b": "section" if xb else None,
                      "reconciled": None})
    for x, y, m in match("territory", ca.get("territory", []), cb.get("territory", []),
                         [(lambda r, t: _main_article(r, t), "mainArticle")]):
        field("territory", "when", x, y, x["id"], y["id"], m)
    for x, y, m in match("exit", ca.get("exits", []), cb.get("exits", []),
                         [(lambda r, e: (e["goesTo"], _main_article(r, e)), "goesToAndArticle"),
                          (lambda r, e: e["goesTo"], "goesTo")]):
        field("exit", "when", x, y, x["id"], y["id"], m)
    for x, y, m in match("element", a.get("elements", []), b.get("elements", []),
                         [(lambda r, e: tuple(sorted(e.get("events", []))), "events")]):
        field("element", "burden", x, y, x["id"], y["id"], m)
    for x, y, m in match("request", a.get("requests", []), b.get("requests", []), [(lambda r, q: q["kind"], "kind")]):
        field("request", "accessory", x, y, x["id"], y["id"], m)
    side = {"documents": {"a": a.get("documents", []), "b": b.get("documents", [])},
            "boundaries": {"a": a.get("boundaries", []), "b": b.get("boundaries", [])}}
    slots_a, slots_b = set(a.get("slots", [])), set(b.get("slots", []))
    for s_ in sorted(slots_a ^ slots_b):
        diff("slot", "used", s_ if s_ in slots_a else None, s_ if s_ in slots_b else None, None, None, "slotId")
    rate["slot"] = [len(slots_a & slots_b), len(slots_a | slots_b)]
    total = [sum(v[0] for v in rate.values()), sum(v[1] for v in rate.values())]
    return {"differences": diffs, "idmap": idmap, "sideBySide": side,
            "proposedSlots": {"a": a.get("proposedSlots", []), "b": b.get("proposedSlots", [])},
            "agreement": {k: {"equal": v[0], "compared": v[1]} for k, v in sorted(rate.items())},
            "agreementTotal": {"equal": total[0], "compared": total[1]}}


# --- the case-by-case half of compare-derivations ----------------------------------------------------------------------
CASE_RUNS = ("aOnB", "bOnA", "boundaryA", "boundaryB", "boundaryAB")


def case_differences(runs: dict[str, list]) -> list[dict]:
    """The `rules examples --json` results of a unit, as differences (record-checks.md, compare-derivations, "case by
    case"): A's examples run on B (`aOnB`) and B's on A (`bOnA`) through the id map; the boundary cases through A alone,
    B alone (a case the record decides against the writer's expectation) and through both together (`boundaryAB`: a
    case the two records decide differently). Only failed results are differences; a passed one is agreement."""
    out = []
    for label in CASE_RUNS:
        for r in runs.get(label) or []:
            if r["passed"]:
                continue
            if label == "boundaryAB" and " | " not in r["recordId"]:
                continue  # the two-record run repeats each record's own check; those come from boundaryA and boundaryB
            if label in ("aOnB", "bOnA") and " on " not in r["recordId"]:
                continue  # the record's own examples, run first by the tool: already checked by the deriver
            out.append({"kind": "case", "field": f"{label} {r['caseId']}", "case": r["caseId"], "run": label,
                        "title": r.get("title", ""), "detail": r["differences"], "matchedOn": "case"})
    return out


def add_cases(doc: dict, runs: dict[str, list]) -> dict:
    """Adds the case differences to a differences report, replacing those of an earlier run; ids continue the list.
    The agreement rate (condition 4) stays the field comparison's: a case difference is agenda, not a field."""
    kept = [d for d in doc["differences"] if d.get("kind") != "case"]
    n = max([int(d["id"][1:]) for d in kept if str(d.get("id", "")).startswith("x") and d["id"][1:].isdigit()] or [0])
    cases = case_differences(runs)
    for i, d in enumerate(cases, n + 1):
        d["id"] = f"x{i}"
    return dict(doc, differences=kept + cases, caseRuns=sorted(k for k in runs if runs[k] is not None),
                caseDifferences=len(cases))


# --- compare-c ---------------------------------------------------------------------------------------------------------
def _units(cites) -> set:
    """Cites as (code, article, paragraph) units; a cite without paragraphs is the whole article (paragraph None)."""
    out = set()
    for c in cites:
        ps = c.get("paragraphs") or []
        out |= {(c["code"], c["article"], p) for p in ps} if ps else {(c["code"], c["article"], None)}
    return out


def _covers(units: set, unit: tuple) -> bool:
    return unit in units or (unit[0], unit[1], None) in units


def _mech_record(rec: dict) -> dict:
    out = {"cites": _units(s["cite"] for s in rec.get("basis", [])), "deadlines": {},
           "priorSteps": sorted({p["kind"] for p in rec.get("priorSteps", [])}),
           "exits": sorted({e["goesTo"] for e in (rec.get("court") or {}).get("exits", [])}),
           "sections": sorted({v for v in [((rec.get("court") or {}).get("section") or {}).get("value")] if v})}
    for d in rec.get("deadlines", []):
        words = (d.get("extract") or {}).get("period", "")
        read = read_periods(words) if words else []
        out["deadlines"][_canon(_main_article(rec, d))] = {
            "kind": d["kind"], "period": d["period"], "raisedBy": d.get("raisedBy"),
            "periodWordsRead": read[0] if read else None, "sanctionWords": (d.get("extract") or {}).get("sanction")}
    return out


def _mech_c(c: dict) -> dict:
    """Codex's answer in its own shape (prompts/schemas/module-derivation-c.schema.json), never a record."""
    out = {"cites": _units(c.get("statements", [])), "deadlines": {},
           "priorSteps": sorted({p["kind"] for p in c.get("priorSteps", [])}),
           "exits": sorted({e["goesTo"] for e in c.get("exits", [])}),
           "sections": sorted({s["section"] for s in c.get("sections", [])})}
    for d in c.get("deadlines", []):
        main = (d["basis"][0]["code"], d["basis"][0]["article"]) if d.get("basis") else None
        read = read_periods(d.get("periodWords", "")) if d.get("periodWords") else []
        out["deadlines"][_canon(main)] = {
            "kind": d["kind"], "period": {"value": d["periodValue"], "unit": d["periodUnit"]},
            "raisedBy": d.get("raisedBy"), "periodWordsRead": read[0] if read else None,
            "sanctionWords": d.get("sanctionWords")}
    return out


def compare_c(c: dict, a: dict, b: dict, given: set | None = None) -> dict:
    """Codex's derivation of a module, as returned in its own shape, against A and B on its mechanical fields only. A
    mechanical difference with a value on which A and B agree becomes an attack (who: derivationC). Cites are compared
    per paragraph, and only within the texts C was given (`given`: (code, article) pairs; None = no limit): C cannot
    cite what it was not shown, and its silence there says nothing."""
    mc, ma, mb = _mech_c(c), _mech_record(a), _mech_record(b)
    rows, attacks = [], []
    for key in ("priorSteps", "exits", "sections"):
        rows.append({"field": key, "c": mc[key], "a": ma[key], "b": mb[key]})
        if ma[key] == mb[key] and mc[key] != ma[key]:
            attacks.append({"who": "derivationC", "target": key, "kind": "mechanicalDifference",
                            "claim": f"C gives {mc[key]} where A and B agree on {ma[key]}"})
    for unit in sorted(mc["cites"] | ma["cites"] | mb["cites"], key=lambda u: (u[0], u[1], u[2] or 0)):
        ia, ib, ic = _covers(ma["cites"], unit), _covers(mb["cites"], unit), _covers(mc["cites"], unit)
        label = f"[{unit[0]} {unit[1]}" + (f" ¶{unit[2]}]" if unit[2] else "]")
        shown = given is None or (unit[0], unit[1]) in given
        rows.append({"field": f"cite {label}", "c": ic, "a": ia, "b": ib, "inTextsGivenToC": shown})
        if shown and ia == ib and ic != ia:
            attacks.append({"who": "derivationC", "target": f"cite {label}", "kind": "mechanicalDifference",
                            "claim": (f"C cites {label}, which neither A nor B cites" if ic
                                      else f"A and B both cite {label}; C does not")})
    for art in sorted(set(mc["deadlines"]) | set(ma["deadlines"]) | set(mb["deadlines"])):
        dc, da, db = mc["deadlines"].get(art), ma["deadlines"].get(art), mb["deadlines"].get(art)
        rows.append({"field": f"deadline {art}", "c": dc, "a": da, "b": db})
        for name in ("kind", "period", "raisedBy"):
            va, vb, vc = (da or {}).get(name), (db or {}).get(name), (dc or {}).get(name)
            if da and db and va == vb and vc != va:
                attacks.append({"who": "derivationC", "target": f"deadline {art}", "kind": "mechanicalDifference",
                                "claim": f"{name}: C gives {vc} where A and B agree on {va}"})
    return {"rows": rows, "attacks": attacks,
            "cSays": {k: c.get(k) for k in ("openPoints", "missingTexts") if c.get(k)}}


# --- flatten-imports ---------------------------------------------------------------------------------------------------
def _prefix_refs(node, prefix: str, ids: set) -> None:
    if isinstance(node, dict):
        for k, v in list(node.items()):
            if k in ("basis", "refs") and isinstance(v, list):
                node[k] = [prefix + x if x in ids else x for x in v]
            else:
                _prefix_refs(v, prefix, ids)
    elif isinstance(node, list):
        for v in node:
            _prefix_refs(v, prefix, ids)


def _collect_basis(node, found: set) -> None:
    if isinstance(node, dict):
        for k, v in node.items():
            if k == "basis" and isinstance(v, list):
                found.update(v)
            else:
                _collect_basis(v, found)
    elif isinstance(node, list):
        for v in node:
            _collect_basis(v, found)


def _slots_of(node, found: set) -> None:
    if isinstance(node, dict):
        for k, v in node.items():
            if k == "slot" and isinstance(v, str):
                found.add(v)
            elif k == "slots" and isinstance(v, list):
                found.update(x for x in v if isinstance(x, str))
            else:
                _slots_of(v, found)
    elif isinstance(node, list):
        for v in node:
            _slots_of(v, found)


def flatten_imports(claim: dict, modules: dict) -> dict:
    """Copies each imported block of a claim record from the module's final record (record-checks.md,
    `flatten-imports`). Fails on a block that does not exist or an id that collides."""
    claim = copy.deepcopy(claim)
    for imp in claim.get("imports", []):
        if imp["module"] not in modules:
            raise ValueError(f"module {imp['module']} is not given")
        module = copy.deepcopy(modules[imp["module"]])
        prefix = module["id"] + "."
        statement_ids = {s["id"] for s in module.get("basis", [])}
        court_m = module.get("court", {})
        pools = {"deadlines": module.get("deadlines", []), "priorSteps": module.get("priorSteps", []),
                 "documents": module.get("documents", []), "boundaries": module.get("boundaries", []),
                 "requests": module.get("requests", []), "territory": court_m.get("territory", []),
                 "exits": court_m.get("exits", [])}
        ids = set(statement_ids)
        for pool in pools.values():
            ids.update(x["id"] for x in pool)
        taken, needed = [], set()
        court = claim.setdefault("court", {})
        for block in imp["blocks"]:
            if block == "section":
                if "section" in court:
                    raise ValueError(f"the claim has its own section rule and imports {module['id']}'s")
                court["section"] = court_m["section"]
                taken.append(court["section"])
                continue
            if block in statement_ids:
                needed.add(block)
                continue
            for name, pool in pools.items():
                hit = [x for x in pool if x["id"] == block]
                if hit:
                    target = court.setdefault(name, []) if name in ("territory", "exits") else claim.setdefault(name, [])
                    target.append(hit[0])
                    taken.append(hit[0])
                    break
            else:
                raise ValueError(f"block {block} does not exist in {module['id']}")
        _collect_basis(taken, needed)
        taken_ids = {x.get("id") for x in taken}
        for point in module.get("openPoints", []):
            if set(point.get("refs", [])) & (taken_ids | needed):
                claim.setdefault("openPoints", []).append(point)
                taken.append(point)
                needed.update(r for r in point.get("refs", []) if r in statement_ids)
        _collect_basis(taken, needed)
        stmts = [s for s in module.get("basis", []) if s["id"] in needed]
        claim.setdefault("basis", []).extend(stmts)
        taken.extend(stmts)
        _prefix_refs(taken, prefix, ids)
        for x in taken:
            if "id" in x:
                x["id"] = prefix + x["id"]
        read = set()
        _slots_of(taken, read)
        for slot in module.get("slots", []):
            if slot in read and slot not in claim.setdefault("slots", []):
                claim["slots"].append(slot)
    all_ids = [x["id"] for k, x in items(claim) if x.get("id")] + [p["id"] for p in claim.get("openPoints", [])]
    dup = sorted({i for i in all_ids if all_ids.count(i) > 1})
    if dup:
        raise ValueError(f"ids collide after flattening: {dup}")
    return claim


# --- grade-records -----------------------------------------------------------------------------------------------------
def _verified(store: st.Store, cite: dict, critical: bool) -> bool:
    e = store.get(cite["code"], cite["article"], cite.get("version"))
    if e is None or not e["verified"]:
        return False
    return not (critical and e.get("verifiedBy") == "corpusChecked")


DISPUTES = os.path.join(st.LAW, "text-disputes.json")
TRIAGE = os.path.join(st.LAW, "triage-status.json")


def text_disputes() -> list[dict]:
    """The open disputes of law/text-disputes.json (owner's decisions of 14 Nov 2026)."""
    return [t for t in (load(DISPUTES).get("texts", []) if os.path.exists(DISPUTES) else []) if t.get("status") == "open"]


def triage_complete() -> set[str]:
    """The codes whose triage of the scan's hits is complete; a missing file means none is."""
    doc = load(TRIAGE) if os.path.exists(TRIAGE) else {"codes": {}}
    return {c for c, v in doc.get("codes", {}).items() if v.get("complete")}


def disputed(cite: dict, disputes: list[dict]) -> dict | None:
    for t in disputes:
        if (t["code"], t["article"]) != (cite["code"], cite["article"]):
            continue
        if cite.get("version") and t.get("version") and cite["version"] != t["version"]:
            continue
        cited = set(cite.get("paragraphs") or [])
        if not cited or not t.get("paragraphs") or cited & set(t["paragraphs"]):
            return t
    return None


def grade_record(record: dict, store: st.Store, idmap=None, differences=None, attacks=None, disputes=None,
                 triaged=None) -> tuple:
    """GRADES.md exactly, with the owner's decisions of 14 Nov 2026: a paragraph kept with two readings
    (law/text-disputes.json) gives grade C, never D, and the mark to check where the owner asked; a code whose triage is
    not complete (law/triage-status.json) keeps every item resting on it below A. Returns (record, grades report)."""
    disputes = text_disputes() if disputes is None else disputes
    triaged = triage_complete() if triaged is None else triaged
    rec = copy.deepcopy(record)
    stmts = statements(rec)
    values = {p["item"] for p in check_values(rec)["problems"] if p["problem"] in
              ("valueMismatch", "periodWordsNotInQuote", "noExtract", "periodNotRead", "wordsAndDigitsDiffer")}
    critical_statements = set()
    for kind, x in items(rec):
        if kind in CRITICAL_KINDS:
            critical_statements.update(basis_ids(x))
    traced = {}
    for row in idmap or []:
        target = row.get("reconciled") or row.get("a")
        if target:
            traced[target] = row
    disagreed = {(side[0], d[side]) for d in differences or [] for side in ("aId", "bId") if d.get(side)}
    report = []
    for kind, x in items(rec):
        iid = item_id(kind, x)
        if "." in iid and x.get("grading"):
            report.append({"item": iid, "kind": kind, **x["grading"], "imported": True})
            continue  # copied from a module by flatten-imports: keeps the module's grading
        basis = [x] if kind == "statement" else [stmts[b] for b in basis_ids(x) if b in stmts]
        critical = kind in CRITICAL_KINDS or (kind == "statement" and x["id"] in critical_statements)
        reasons_d = []
        if not basis or any(not s.get("quote") or s.get("source", {}).get("tier") not in (1, 2) for s in basis):
            reasons_d.append("noOfficialText")
        if kind == "deadline" and iid in values:
            reasons_d.append("valueMismatch")
        in_dispute = [(s, disputed(s["cite"], disputes)) for s in basis]
        in_dispute = [(s, t) for s, t in in_dispute if t]
        unverified = sorted({f"{s['cite']['code']} {s['cite']['article']}" for s in basis
                             if not _verified(store, s["cite"], critical) and not disputed(s["cite"], disputes)})
        if unverified:
            reasons_d.append("textNotVerified: " + ", ".join(unverified))
        if reasons_d:
            g = {"grade": "D", "reasons": reasons_d, "toCheck": True}
        else:
            reasons = []
            interpretive = bool(x.get("interpretive") or any(s.get("interpretive") for s in basis))
            if in_dispute:
                interpretive = True
                reasons.append("disputedText: " + ", ".join(sorted({f"{t['code']} {t['article']}" for _, t in in_dispute})))
            if kind == "deadline" and not x.get("counting"):
                interpretive = True
            entries = [store.get(s["cite"]["code"], s["cite"]["article"], s["cite"].get("version")) for s in basis]
            if kind == "deadline" and any(e and e.get("frenchCheck") in ("mismatch", "noFrench") for e in entries):
                interpretive = True
            if interpretive and not in_dispute:
                reasons.append("interpretive")
            if idmap is None:
                reasons.append("singleDerivation")
            else:
                row = traced.get(iid)
                if not row or not row.get("a") or not row.get("b"):
                    reasons.append("addedAfterDerivation")
                elif ("a", row["a"]) in disagreed or ("b", row["b"]) in disagreed:
                    reasons.append("disagreedBeforeReconciliation")
            if attacks is None:
                reasons.append("notAttacked")
            elif any(t.get("target") == iid and t.get("verdict") not in ("refuted", "fixed") for t in attacks):
                reasons.append("attackOpen")
            if not all(s.get("source", {}).get("ledgerClean") for s in basis):
                why = {(e or {}).get("ledgerReason") for e in entries}
                reasons.append("scanBlind" if "scanBlind" in why else "scanNotRun" if "scanNotRun" in why
                               else "ledgerNotClean")
            open_codes = sorted({s["cite"]["code"] for s in basis} - triaged)
            if open_codes:
                reasons.append("triageOpen: " + ", ".join(open_codes))
            g = {"grade": "C" if interpretive else ("B" if reasons else "A"), "reasons": reasons,
                 "toCheck": any(t.get("toCheck") for _, t in in_dispute)}
        x["grading"] = g
        report.append({"item": iid, "kind": kind, **g})
    graded = {item_id(k, x): x["grading"] for k, x in items(rec)}
    for ex in rec.get("examples", []):
        for f in ex["expect"].get("findings", []):
            g = graded.get(f["ref"])
            if g is None:
                continue
            f["shownEffect"] = "warn" if f["effect"] == "blockCleanExport" and g["grade"] != "A" else f["effect"]
            f["toCheck"] = bool(g["toCheck"])
    if idmap is not None and differences is not None:
        pairs = [r for r in idmap if r.get("a") and r.get("b")]
        bad = {("a", d["aId"]) for d in differences if d.get("aId")} | {("b", d["bId"]) for d in differences if d.get("bId")}
        same = sum(1 for r in pairs if ("a", r["a"]) not in bad and ("b", r["b"]) not in bad)
        rec.setdefault("history", {})["agreementBeforeReconciliation"] = f"{same} of {len(pairs)} matched items"
    rec["stage"] = "final"
    return rec, {"items": report}


# --- check-checklist and inventory-cited -------------------------------------------------------------------------------
def check_checklist(record: dict, checklist: list, flags: list) -> dict:
    """Every line of the merged checklist and every neighbour flag must be answered in the record."""
    lines = {re.sub(r"\s+", " ", k["item"]).strip().lower(): k for k in record.get("checklist", [])}
    ids = {item_id(kind, x) for kind, x in items(record)} | {p["id"] for p in record.get("openPoints", [])}
    unanswered, bad_refs = [], []
    for line in checklist:
        k = lines.get(re.sub(r"\s+", " ", line["item"]).strip().lower())
        if not k or k.get("answer") not in ("applies", "doesNotApply", "searchedNotFound"):
            unanswered.append(line["id"])
        elif k["answer"] == "applies" and not k.get("refs"):
            unanswered.append(line["id"])
        else:
            bad_refs += [[line["id"], r] for r in k.get("refs", []) if r not in ids]
    stmts = statements(record)
    cited = {(s["cite"]["code"], s["cite"]["article"], p) for s in stmts.values() for p in (s["cite"].get("paragraphs") or [])}
    whole = {(s["cite"]["code"], s["cite"]["article"]) for s in stmts.values() if not s["cite"].get("paragraphs")}
    said = " ".join((k.get("note") or "") + " " + k["item"] for k in record.get("checklist", [])
                    if k.get("answer") == "doesNotApply")
    open_flags = [f["id"] for f in flags
                  if (f["code"], f["article"], f["paragraph"]) not in cited and (f["code"], f["article"]) not in whole
                  and not re.search(rf"(?<!\d){re.escape(f['article'])}(?!\d)", said)]
    return {"unansweredLines": unanswered, "refsNotInRecord": bad_refs, "unansweredFlags": open_flags,
            "ok": not unanswered and not bad_refs and not open_flags}


def inventory_cited(records: list) -> list:
    out = {}
    for r in records:
        for s in r.get("basis", []):
            c = s["cite"]
            k = (c["code"], c["article"], c.get("version") or "")
            out.setdefault(k, {"code": c["code"], "article": c["article"], "version": c.get("version"),
                               "units": set()})["units"].add(r["id"])
    rows = sorted(out.items(), key=lambda kv: (kv[0][0], st._num_key(kv[0][1]), kv[0][2]))
    return [dict(v, units=sorted(v["units"])) for _, v in rows]


# --- the texts of a unit (record-checks.md, "The texts given to Codex and to the boundary writer") ----------------------
HEADING = re.compile(r"^(الكتاب|الباب|الفصل|القسم)")
LEVELS = ("الكتاب", "الباب", "الفصل", "القسم")


def consolidated_chapters(code: str) -> dict:
    """Article -> its headings, read from the official consolidated edition (tier 2) where the store has no path: the
    library carries no headings, and the corpus lacks some articles (Civil Code 160 to 188). Only headings are taken
    from the edition, never a word of an article."""
    import consolidated
    import subprocess
    import unicodedata
    pdf = consolidated.PDFS[code]
    n = int(re.search(r"Pages:\s+(\d+)", subprocess.run([consolidated.PDFINFO, pdf], capture_output=True,
                                                          text=True).stdout).group(1))
    path = ["", "", "", ""]
    out, last = {}, 0
    for p in range(1, n + 1):
        t = subprocess.run([consolidated.PDFTOTEXT, "-enc", "UTF-8", "-f", str(p), "-l", str(p), pdf, "-"],
                           capture_output=True).stdout.decode("utf-8", "replace")
        t = unicodedata.normalize("NFKC", re.sub("[‪-‮‎‏]", "", t)).translate(textcmp.DIGITS)
        for line in t.split("\n"):
            joined = line.replace(" ", "")
            h = HEADING.match(joined)
            if h and len(joined) < 70:
                level = LEVELS.index(h.group(1))
                path[level] = re.sub(r"\s+", " ", line).strip(" ()0123456789")
                for k in range(level + 1, 4):
                    path[k] = ""
                continue
            m = re.match(r"^المادة:?(\d+)(مكرر\d*)?", joined)
            if m and int(m.group(1)) >= last and int(m.group(1)) <= last + 20:  # articles come in order; a mention elsewhere does not
                last = int(m.group(1))
                number = m.group(1) + ((" مكرر" + (" " + m.group(2)[4:] if m.group(2)[4:] else "")) if m.group(2) else "")
                out.setdefault(number, [x for x in path if x])
    return out


def unit_texts(unit: str, starts: list[tuple[str, str]], store: st.Store, edition_paths: dict | None = None) -> dict:
    """Every article of each chapter that holds a starting article, whole, each paragraph numbered «[code article ¶n]».
    A chapter is the store's path when it has one; otherwise the deepest heading of the official consolidated edition."""
    chapters, seen = [], set()
    for code, article in starts:
        e = store.get(code, article)
        if e is None:
            chapters.append({"code": code, "start": article, "path": None, "from": "notInStore", "articles": []})
            continue
        missing = []
        if e.get("path"):
            path, source = e["path"], "store"
            members = [x["article"] for x in store.chapter(code, path)]
        else:
            paths = (edition_paths or {}).get(code) or consolidated_chapters(code)
            path = paths.get(article)
            source = "consolidatedEdition" if path else "noHeadingFound"
            members = [a for a, pth in paths.items() if pth == path and store.get(code, a)] if path else [article]
            missing = [a for a, pth in paths.items() if path and pth == path and not store.get(code, a)]
        key = (code, tuple(path or [article]))
        if key in seen:
            continue
        seen.add(key)
        members = sorted(set(members), key=st._num_key)
        chapters.append({"code": code, "start": article, "path": path, "from": source, "articles": members,
                         "missingFromStore": sorted(missing, key=st._num_key)})  # fetch these before committing
    lines = []
    for ch in chapters:
        lines.append(f"## {ch['code']}: {' / '.join(ch['path'] or [ch['start']])}")
        for a in ch["articles"]:
            e = store.get(ch["code"], a)
            for n, p in enumerate(e["paragraphs"], 1):
                lines.append(f"[{ch['code']} {a} ¶{n}] {p}")
    return {"unit": unit, "rule": "every article of each chapter that holds a starting article (record-checks.md); "
                                  "the chapter is the store's path, or the deepest heading of the official consolidated "
                                  "edition when the store has none",
            "starts": [{"code": c, "article": a} for c, a in starts], "chapters": chapters,
            "rendered": "\n".join(lines), "renderedSha256": sha("\n".join(lines))}



# --- command line --------------------------------------------------------------------------------------------------------
def quoted_path(record_path: str) -> str:
    return os.path.join(os.path.dirname(os.path.abspath(record_path)), "quoted", os.path.basename(record_path))


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="records")
    sub = p.add_subparsers(dest="cmd", required=True)
    q = sub.add_parser("quote"); q.add_argument("record"); q.add_argument("--out"); q.add_argument("--store")
    v = sub.add_parser("check-values"); v.add_argument("record"); v.add_argument("--report", required=True)
    f = sub.add_parser("flag-neighbours"); f.add_argument("record"); f.add_argument("--report", required=True)
    f.add_argument("--store")
    m = sub.add_parser("merge-checklists"); m.add_argument("unit"); m.add_argument("--out", required=True)
    for name in ("a", "b", "codex", "november", "claim"):
        m.add_argument(f"--{name}")
    c = sub.add_parser("compare"); c.add_argument("a"); c.add_argument("b")
    c.add_argument("--differences", required=True); c.add_argument("--idmap", required=True)
    ac = sub.add_parser("add-cases"); ac.add_argument("differences")
    for name in CASE_RUNS:
        ac.add_argument(f"--{name}")
    cc = sub.add_parser("compare-c"); cc.add_argument("c"); cc.add_argument("a"); cc.add_argument("b")
    cc.add_argument("--out", required=True)
    cc.add_argument("--texts", help="the unit's reports/<unit>.texts.json: cites outside it are not held against C")
    fl = sub.add_parser("flatten"); fl.add_argument("claim"); fl.add_argument("--modules", required=True)
    fl.add_argument("--out", required=True)
    g = sub.add_parser("grade"); g.add_argument("record"); g.add_argument("--out", required=True)
    for name in ("idmap", "differences", "attacks", "report", "store"):
        g.add_argument(f"--{name}")
    k = sub.add_parser("check-checklist"); k.add_argument("record"); k.add_argument("--checklist", required=True)
    k.add_argument("--flags", action="append", default=[]); k.add_argument("--report", required=True)
    i = sub.add_parser("inventory"); i.add_argument("records", nargs="+"); i.add_argument("--out", required=True)
    ut = sub.add_parser("unit-texts"); ut.add_argument("unit"); ut.add_argument("starts", nargs="+", help="code:article")
    ut.add_argument("--out", required=True)
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")

    def the_store(path):
        return st.Store(path) if path else st.Store()

    if a.cmd == "quote":
        out = a.out or quoted_path(a.record)
        old = load(out) if os.path.exists(out) else None
        quoted, problems = quote_record(load(a.record), the_store(a.store))
        for pr in problems:
            print("FAIL", pr["message"])
        changed = quote_changes(old, quoted)
        if changed:
            print("quoted text changed since the last run:", ", ".join(changed))
        if problems:
            return 1
        save(out, quoted)
        print(f"quoted {len(quoted.get('basis', []))} statements -> {out}")
    elif a.cmd == "check-values":
        r = check_values(load(a.record))
        save(a.report, r)
        print(f"{len(r['problems'])} problem(s), {len(r['notes'])} note(s)")
        return 1 if r["problems"] else 0
    elif a.cmd == "flag-neighbours":
        flags = flag_neighbours(load(a.record), the_store(a.store))
        save(a.report, flags)
        print(f"{len(flags)} flag(s)")
    elif a.cmd == "merge-checklists":
        sources = {}
        for name, origin in (("a", "derivationA"), ("b", "derivationB"), ("codex", "codex"), ("november", "novemberUnit")):
            path = getattr(a, name)
            if path:
                doc = load(path)
                sources[origin] = doc.get("checklist", doc) if isinstance(doc, dict) else doc
        if a.claim:
            claim = catalogue_claim(a.claim)
            if claim is None:
                raise SystemExit(f"claim {a.claim} is not in the catalogue")
            sources["catalogue"] = catalogue_questions(claim)
        merged = merge_checklists(sources)
        save(a.out, merged)
        print(f"{len(merged)} line(s)")
    elif a.cmd == "compare":
        r = compare_derivations(load(a.a), load(a.b))
        save(a.differences, {k: r[k] for k in ("differences", "sideBySide", "proposedSlots", "agreement", "agreementTotal")})
        save(a.idmap, r["idmap"])
        t = r["agreementTotal"]
        print(f"{len(r['differences'])} difference(s); agreement {t['equal']} of {t['compared']}")
    elif a.cmd == "add-cases":
        runs = {name: load(getattr(a, name)) for name in CASE_RUNS if getattr(a, name)}
        doc = add_cases(load(a.differences), runs)
        save(a.differences, doc)
        print(f"{doc['caseDifferences']} case difference(s) from {', '.join(doc['caseRuns'])}")
    elif a.cmd == "compare-c":
        given = None
        if a.texts:
            given = {(ch["code"], art) for ch in load(a.texts)["chapters"] for art in ch["articles"]}
        r = compare_c(load(a.c), load(a.a), load(a.b), given)
        save(a.out, r)
        print(f"{len(r['attacks'])} attack(s) from C")
    elif a.cmd == "flatten":
        claim = load(a.claim)
        mods = {imp["module"]: load(os.path.join(a.modules, f"{imp['module']}.json")) for imp in claim.get("imports", [])}
        save(a.out, flatten_imports(claim, mods))
    elif a.cmd == "grade":
        rec, report = grade_record(load(a.record), the_store(a.store), load(a.idmap) if a.idmap else None,
                                   load(a.differences)["differences"] if a.differences else None,
                                   load(a.attacks) if a.attacks else None)
        save(a.out, rec)
        if a.report:
            save(a.report, report)
        counts = {}
        for row in report["items"]:
            counts[row["grade"]] = counts.get(row["grade"], 0) + 1
        print(" ".join(f"{g}:{n}" for g, n in sorted(counts.items())))
    elif a.cmd == "check-checklist":
        flags = [x for path in a.flags for x in load(path)]
        r = check_checklist(load(a.record), load(a.checklist), flags)
        save(a.report, r)
        print("ok" if r["ok"] else f"unanswered lines {r['unansweredLines']}, flags {r['unansweredFlags']}, "
                                    f"refs {r['refsNotInRecord']}")
        return 0 if r["ok"] else 1
    elif a.cmd == "unit-texts":
        doc = unit_texts(a.unit, [tuple(x.split(":", 1)) for x in a.starts], the_store(None))
        save(a.out, doc)
        missing = [m for ch in doc["chapters"] for m in ch["missingFromStore"]]
        print(f"{len(doc['chapters'])} chapter(s); missing from the store: {missing or 'none'}")
        return 1 if missing else 0
    elif a.cmd == "inventory":
        inv = inventory_cited([load(x) for x in a.records])
        save(a.out, inv)
        print(f"{len(inv)} cited article(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
