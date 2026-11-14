"""The Official Journal scan (docs/research/core-logic/specs/official-journal-scan.md).

Reads every French issue of the Official Journal from 2003 to today, keeps its text outside the repository, lists every
sentence that may announce a change to one of the instruments the rules rest on, and writes the evidence list (one line
per issue, committed). Downloads go through oj_fetch, one at a time; a run resumes where the last one stopped.

    python journal_scan.py run [--years 2005,2007,2022] [--to-year 2026]
    python journal_scan.py search          # search the kept text again with the current patterns (no download)
    python journal_scan.py known           # the test the scan must pass: the five known amendments
    python journal_scan.py summary         # writes journal-scan/SUMMARY.md
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import oj_fetch  # noqa: E402
import clean_env  # noqa: E402,F401  (removes the owner's two API keys from this process: every launch inherits that)

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
EVIDENCE = os.path.join(REPO, "docs", "research", "core-logic", "law", "journal-scan")
WORK = os.environ.get("JOURNAL_WORK", r"C:\Users\TheShadow\dz-core-research")
TEXT = os.path.join(WORK, "journal-text")
STATE = os.path.join(WORK, "journal-scan-state.json")
PDFTOTEXT = os.environ.get("PDFTOTEXT") or shutil.which("pdftotext") or r"C:\Program Files\Git\mingw64\bin\pdftotext.exe"
FIRST_YEAR = 2003
URL = "https://www.joradp.dz/FTP/jo-francais/{y}/F{y}{n:03d}.pdf"

# The patterns. Raise PATTERNS_VERSION whenever a pattern changes; `search` then runs over the kept text again.
PATTERNS_VERSION = 1
INSTRUMENTS = {
    "civil-code": {"numbers": ["75-58"], "names": ["code civil"]},
    "commercial-code": {"numbers": ["75-59"], "names": ["code de commerce"]},
    "cpca": {"numbers": ["08-09"], "names": ["code de procedure civile et administrative"]},
    "family-code": {"numbers": ["84-11"], "names": ["code de la famille"]},
    "labour-relations": {"numbers": ["90-11"], "names": ["relations de travail"]},
    "labour-disputes": {"numbers": ["90-04"], "names": ["conflits individuels de travail"]},
}
# Participles are matched in every gender and number (modifie, modifiee, modifies, modifiees).
_PP = r"(?:e|s|es)?"
TITLE_RE = re.compile(r"(?<![a-z])(?:modifiant|completant|abrogeant|portant|rectificatif|(?:modifie|complete|abroge)" + _PP + r")(?![a-z])")
OPERATIVE_RE = re.compile(r"(?<![a-z])(?:(?:est|sont)\s(?:modifie|complete|abroge|remplace|insere|redige)" + _PP +
                          r"|il est insere)(?![a-z])")
NEAR_NAME_RE = re.compile(r"(?<![a-z])(?:modifie|complete|abroge)" + _PP + r"(?![a-z])")
WINDOW = 150

KNOWN = [  # the five known amendments: (year, issue, instrument, text)
    (2005, 11, "commercial-code", "Law 05-02 of 6 February 2005"),
    (2005, 15, "family-code", "Ordinance 05-02 of 27 February 2005"),
    (2005, 44, "civil-code", "Law 05-10 of 20 June 2005"),
    (2007, 31, "civil-code", "Law 07-05 of 13 May 2007"),
    (2022, 48, "cpca", "Law 22-13 of 12 July 2022"),
]

MONTHS = {"janvier": 1, "fevrier": 2, "mars": 3, "avril": 4, "mai": 5, "juin": 6, "juillet": 7, "aout": 8,
          "septembre": 9, "octobre": 10, "novembre": 11, "decembre": 12}


def fold(text: str) -> str:
    """Lower case, accents removed, every hyphen-like character as '-', runs of white space as one space."""
    text = unicodedata.normalize("NFKD", text)
    text = "".join(c for c in text if not unicodedata.combining(c))
    text = text.replace("º", "°").replace("ᵒ", "°")
    text = re.sub(r"[\u2010-\u2015\u2212\u00ad]", "-", text)
    return re.sub(r"\s+", " ", text.lower())


def _number_regex(number: str) -> re.Pattern:
    return re.compile(r"(?<![a-z0-9])(?:n\s?°|no|numero)[\s.:]{0,4}" + re.escape(number) + r"(?![0-9])")


NUMBER_RE = {k: [_number_regex(n) for n in v["numbers"]] for k, v in INSTRUMENTS.items()}
NAME_RE = {k: [re.compile(r"(?<![a-z])" + re.escape(n) + r"(?![a-z])") for n in v["names"]] for k, v in INSTRUMENTS.items()}
ALL_NAMES = [r for regs in NAME_RE.values() for r in regs]
SENTENCE_END = re.compile(r"[.;:](?=\s)|\n\s*\n")


def _sentence(page: str, start: int, end: int) -> str:
    before = [m.end() for m in SENTENCE_END.finditer(page, 0, start)]
    s = before[-1] if before else 0
    after = SENTENCE_END.search(page, end)
    e = after.start() + 1 if after else len(page)
    return page[s:e].strip()


def classify(folded_page: str, start: int, end: int) -> str:
    sentence = _sentence(folded_page, start, end)
    if re.match(r"vu\s", sentence):
        return "visa"
    window = folded_page[max(0, start - WINDOW): end + WINDOW]
    operative = bool(OPERATIVE_RE.search(sentence))
    if not operative:
        for name in ALL_NAMES:
            for m in name.finditer(folded_page, max(0, start - 2 * WINDOW), end + 2 * WINDOW):
                near = folded_page[max(0, m.start() - WINDOW): m.end() + WINDOW]
                if NEAR_NAME_RE.search(near) and not re.match(r"vu\s", _sentence(folded_page, m.start(), m.end())):
                    operative = True
                    break
            if operative:
                break
    if operative:
        return "operative"
    if TITLE_RE.search(window):
        return "title"
    return "other"


def search(pages: list[str]) -> tuple[list[dict], dict]:
    hits, visas = [], {}
    for p, raw in enumerate(pages, start=1):
        page = fold(raw)
        seen = set()
        for instrument in INSTRUMENTS:
            for kind, regs in (("number", NUMBER_RE[instrument]), ("name", NAME_RE[instrument])):
                for reg in regs:
                    for m in reg.finditer(page):
                        cls = classify(page, m.start(), m.end())
                        if cls == "visa":
                            visas[instrument] = visas.get(instrument, 0) + 1
                            continue
                        sentence = _sentence(page, m.start(), m.end())
                        key = (instrument, sentence[:120])
                        if key in seen:
                            continue
                        seen.add(key)
                        snippet = sentence if len(sentence) <= 300 else page[max(0, m.start() - 140): m.end() + 140]
                        pattern = INSTRUMENTS[instrument]["numbers" if kind == "number" else "names"][0]
                        hits.append({"instrument": instrument, "pattern": pattern, "page": p, "class": cls, "sentence": snippet[:300]})
    return hits, visas


def masthead_date(first_page: str) -> str | None:
    head = fold(first_page)[:900]
    m = re.search(r"(?<![0-9])(\d{1,2})(?:er)?\s(" + "|".join(MONTHS) + r")\s(\d{4})(?![0-9])", head)
    if not m:
        return None
    try:
        return dt.date(int(m.group(3)), MONTHS[m.group(2)], int(m.group(1))).isoformat()
    except ValueError:
        return None


# Some issues (seen in 2005) print part of their text in a font whose text layer is shifted: every character is
# written 29 code points lower ("'LPDQFKH" for "Dimanche"), spaces as \x03, digits as control characters, and the
# accented letters as plain ones (p for é, q for è ...). Such text passes the "blind" test (it has characters) but no
# pattern can ever match it. The test of French is a letter-pair model learnt from issues whose text layer is clean
# (`french_bigrams.json`, built by `journal_scan.py model`): a line is decoded when its decoded form is clearly more
# French than its raw form; a page is unreadable when, after that repair, it is less French than the clean pages ever
# are. Names, tables and headings read as French enough by this measure; shifted text does not. Raise TEXT_VERSION
# when this changes.
TEXT_VERSION = 3
SHIFT = 29
ACCENTS = {"p": "é", "q": "è", "r": "ê", "s": "ë", "j": "à", "k": "â", "d": "ç", "v": "î", "w": "ï", "{": "ô", "x": "ù", "y": "û"}
MODEL_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "french_bigrams.json")
MIN_LETTERS = 12       # shorter lines are left as they are
MIN_PAGE_LETTERS = 300  # pages with fewer letters (tables, signatures) are not judged
MARGIN = 0.8           # bits per letter pair by which the decoded line must be more French
SHIFT_MARK = re.compile(r"[\x03-\x08\x0e-\x1f]")
_MODEL = None


def _letters(text: str) -> str:
    return re.sub(r" +", " ", re.sub(r"[^a-z]", " ", fold(text))).strip()


def model() -> dict:
    global _MODEL
    if _MODEL is None:
        with open(MODEL_PATH, encoding="utf-8") as f:
            _MODEL = json.load(f)
    return _MODEL


def plausibility(text: str, min_letters: int = MIN_LETTERS) -> float | None:
    """Average log2 probability of the text's letter pairs under the model; None for too little text."""
    t = _letters(text)
    if sum(c != " " for c in t) < min_letters:
        return None
    lp, alphabet = model()["logprob"], model()["alphabet"]
    pairs = [lp[alphabet.index(a)][alphabet.index(b)] for a, b in zip(" " + t, t + " ")]
    return sum(pairs) / len(pairs)


def decode_shifted(line: str) -> str:
    out = []
    for c in line:
        o = ord(c)
        if 3 <= o <= 0x5D:
            out.append(chr(o + SHIFT))
        else:
            out.append(ACCENTS.get(c, c))
    return "".join(out)


def repair_line(line: str) -> tuple[str, bool]:
    # Only a line that carries the shifted font's own marks (spaces and digits come out as control characters) is ever
    # decoded: short clean lines ("de l'article 5.") can look less French than their "decoded" form by chance.
    if not SHIFT_MARK.search(line):
        return line, False
    raw = plausibility(line)
    decoded = decode_shifted(line)
    dec = plausibility(decoded)
    if dec is None:
        return line, False
    if raw is None:
        # Shifted capitals come out as digits and signs ("-2851$/2)),&,(/" for "JOURNAL OFFICIEL"), so the raw line
        # has almost no letters: decode it when the decoded line reads as French as a clean page does.
        return (decoded, True) if dec > model()["pageFloor"] + MARGIN else (line, False)
    if dec > raw + MARGIN:
        return decoded, True
    return line, False


def unreadable_pages(pages: list[str]) -> list[int]:
    """The readability check: pages (1-based) with enough letters whose text is less French than clean pages ever are."""
    floor = model()["pageFloor"]
    bad = []
    for n, page in enumerate(pages, start=1):
        score = plausibility(page, MIN_PAGE_LETTERS)
        if score is not None and score < floor:
            bad.append(n)
    return bad


def build_model(paths: list[str]) -> dict:
    """Letter-pair log probabilities from pages without any control character (a clean text layer)."""
    alphabet = " abcdefghijklmnopqrstuvwxyz"
    counts = [[1] * len(alphabet) for _ in alphabet]  # add-one smoothing
    clean_pages = []
    for path in paths:
        with open(path, encoding="utf-8") as f:
            for page in f.read().split("\f"):
                if re.search(r"[\x03-\x08\x0e-\x1f]", page):
                    continue
                t = _letters(page)
                if sum(c != " " for c in t) < MIN_PAGE_LETTERS:
                    continue
                clean_pages.append(page)
                for a, b in zip(" " + t, t + " "):
                    counts[alphabet.index(a)][alphabet.index(b)] += 1
    import math
    logprob = [[round(math.log2(c / sum(row)), 4) for c in row] for row in counts]
    global _MODEL
    _MODEL = {"alphabet": alphabet, "logprob": logprob, "pageFloor": 0.0, "pages": len(clean_pages)}
    scores = sorted(plausibility(p, MIN_PAGE_LETTERS) for p in clean_pages)
    # The floor: half a bit below the least French clean page seen (tables of names and figures included).
    _MODEL["pageFloor"] = round(scores[0] - 0.5, 4)
    _MODEL["cleanPageScores"] = {"min": scores[0], "p01": scores[len(scores) // 100], "median": scores[len(scores) // 2]}
    return _MODEL


def repair(pages: list[str]) -> tuple[list[str], int]:
    """Decodes the shifted lines of every page. Returns the pages and the number of lines decoded."""
    fixed, count = [], 0
    for page in pages:
        lines = []
        for line in page.split("\n"):
            new, changed = repair_line(line)
            lines.append(new)
            count += changed
        fixed.append("\n".join(lines))
    return fixed, count


def extract_pages(pdf: str) -> list[str]:
    out = subprocess.run([PDFTOTEXT, "-enc", "UTF-8", pdf, "-"], capture_output=True, timeout=300)
    if out.returncode != 0:
        raise RuntimeError(f"pdftotext exit {out.returncode}")
    text = out.stdout.decode("utf-8", "replace")
    pages = text.split("\f")
    if pages and not pages[-1].strip():
        pages = pages[:-1]
    return pages


def _evidence_path(year: int) -> str:
    return os.path.join(EVIDENCE, f"issues-{year}.jsonl")


def load_evidence(year: int) -> dict[int, dict]:
    lines = {}
    path = _evidence_path(year)
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            for raw in f:
                if raw.strip():
                    line = json.loads(raw)
                    lines[line["issue"]] = line
    return lines


def save_evidence(year: int, lines: dict[int, dict]) -> None:
    os.makedirs(EVIDENCE, exist_ok=True)
    with open(_evidence_path(year), "w", encoding="utf-8", newline="\n") as f:
        for issue in sorted(lines):
            f.write(json.dumps(lines[issue], ensure_ascii=False) + "\n")


def _kept_text_path(year: int, issue: int) -> str:
    return os.path.join(TEXT, str(year), f"F{year}{issue:03d}.txt")


def read_issue(year: int, issue: int, tmp: str) -> dict:
    url = URL.format(y=year, n=issue)
    line = {"year": year, "issue": issue, "issueDate": None, "dateSource": "unread", "url": url, "sha256": None,
            "bytes": None, "pages": None, "textChars": None, "textSha256": None, "blind": None,
            "readOn": dt.date.today().isoformat(), "patternsVersion": PATTERNS_VERSION, "error": None, "hits": [], "visaHits": {}}
    fetched = oj_fetch.fetch(url, tmp, "scan", wait=600)
    pdf = fetched["file"]
    try:
        line["sha256"], line["bytes"] = fetched["sha256"], fetched["bytes"]
        pages, decoded_lines = repair(extract_pages(pdf))
        line["textVersion"], line["decodedLines"] = TEXT_VERSION, decoded_lines
        text = "\f".join(pages)
        os.makedirs(os.path.dirname(_kept_text_path(year, issue)), exist_ok=True)
        with open(_kept_text_path(year, issue), "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        chars = sum(len(p.strip()) for p in pages)
        line.update(pages=len(pages), textChars=chars, textSha256=hashlib.sha256(text.encode("utf-8")).hexdigest(),
                    blind=(chars / max(1, len(pages))) < 200)
        bad = unreadable_pages(pages)
        line["unreadablePages"] = bad
        line["readable"] = not line["blind"] and not bad
        if pages:
            date = masthead_date(pages[0])
            if date:
                line["issueDate"], line["dateSource"] = date, "masthead"
        line["hits"], line["visaHits"] = search(pages)
    finally:
        os.remove(pdf)  # the owner's rule: keep the hash, never the PDF
    return line


def _load_state() -> dict:
    try:
        with open(STATE, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return {"years": {}}


def _save_state(state: dict) -> None:
    os.makedirs(os.path.dirname(STATE), exist_ok=True)
    with open(STATE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=1)


def run(years: list[int]) -> int:
    state = _load_state()
    failures_in_a_row = 0
    with tempfile.TemporaryDirectory(prefix="jo-scan-") as tmp:
        for year in years:
            lines = load_evidence(year)
            ystate = state["years"].setdefault(str(year), {"complete": False, "lastIssue": 0})
            if ystate.get("complete"):
                continue
            issue, misses = 1, 0
            while misses < 5:
                if issue in lines and lines[issue].get("sha256") and not lines[issue].get("error"):
                    issue += 1
                    misses = 0
                    continue
                try:
                    lines[issue] = read_issue(year, issue, tmp)
                    misses, failures_in_a_row = 0, 0
                    print(f"{year} {issue:03d} {lines[issue]['pages']}p hits={len(lines[issue]['hits'])} "
                          f"date={lines[issue]['issueDate']}", flush=True)
                except oj_fetch.NotFound:
                    misses += 1
                except oj_fetch.Busy:
                    print(f"{year} {issue:03d} busy: stopping; resume later", flush=True)
                    save_evidence(year, lines); _save_state(state)
                    return 3
                except oj_fetch.Refused as e:
                    print(f"refused: {e}", flush=True)
                    save_evidence(year, lines); _save_state(state)
                    return 6
                except (oj_fetch.Failed, RuntimeError, OSError, subprocess.TimeoutExpired) as e:
                    failures_in_a_row += 1
                    entry = lines.get(issue) or {"year": year, "issue": issue, "issueDate": None, "dateSource": "unread",
                                                 "url": URL.format(y=year, n=issue), "sha256": None, "bytes": None,
                                                 "pages": None, "textChars": None, "textSha256": None, "blind": None,
                                                 "readOn": dt.date.today().isoformat(), "patternsVersion": PATTERNS_VERSION,
                                                 "hits": [], "visaHits": {}}
                    entry["error"] = f"{type(e).__name__}: {str(e)[:160]}"
                    lines[issue] = entry
                    print(f"{year} {issue:03d} error {entry['error']}", flush=True)
                    if failures_in_a_row >= 5:
                        print("5 failures in a row: the scan stops (politeness rule)", flush=True)
                        save_evidence(year, lines); _save_state(state)
                        return 5
                ystate["lastIssue"] = issue
                if issue % 10 == 0:
                    save_evidence(year, lines); _save_state(state)
                issue += 1
            ystate["complete"] = True
            ystate["issues"] = max(lines) if lines else 0
            save_evidence(year, lines)
            _save_state(state)
            print(f"{year} complete: {len(lines)} issues", flush=True)
    return 0


def research() -> None:
    """Searches the kept text again with the current patterns. No download."""
    for name in sorted(os.listdir(EVIDENCE)) if os.path.isdir(EVIDENCE) else []:
        m = re.match(r"issues-(\d{4})\.jsonl$", name)
        if not m:
            continue
        year = int(m.group(1))
        lines = load_evidence(year)
        for issue, line in lines.items():
            path = _kept_text_path(year, issue)
            if line.get("sha256") and os.path.exists(path):
                with open(path, encoding="utf-8") as f:
                    pages = f.read().split("\f")
                if line.get("textVersion", 1) < TEXT_VERSION:
                    pages, decoded_lines = repair(pages)
                    text = "\f".join(pages)
                    with open(path, "w", encoding="utf-8", newline="\n") as f:
                        f.write(text)
                    chars = sum(len(p.strip()) for p in pages)
                    line.update(textVersion=TEXT_VERSION, decodedLines=decoded_lines, textChars=chars,
                                textSha256=hashlib.sha256(text.encode("utf-8")).hexdigest(),
                                blind=(chars / max(1, len(pages))) < 200)
                    if pages and not line.get("issueDate"):
                        date = masthead_date(pages[0])
                        if date:
                            line["issueDate"], line["dateSource"] = date, "masthead"
                bad = unreadable_pages(pages)
                line["unreadablePages"] = bad
                line["readable"] = not line.get("blind") and not bad
                line["hits"], line["visaHits"] = search(pages)
                line["patternsVersion"] = PATTERNS_VERSION
        save_evidence(year, lines)
        print(f"{year}: searched again with patterns version {PATTERNS_VERSION}")


def known() -> int:
    ok = True
    for year, issue, instrument, label in KNOWN:
        line = load_evidence(year).get(issue)
        if not line or not line.get("sha256"):
            print(f"NOT READ YET  {label} (issue {issue} of {year})")
            ok = False
            continue
        if line.get("readable") is False:
            print(f"UNREAD  {label} (issue {issue} of {year}): the text does not read as French")
            ok = False
            continue
        found = [h for h in line["hits"] if h["instrument"] == instrument and h["class"] in ("title", "operative")]
        print(f"{'FOUND' if found else 'MISSED'}  {label} (issue {issue} of {year}): {len(found)} title/operative hit(s)")
        ok &= bool(found)
    print("known-amendment test:", "PASSED" if ok else "NOT PASSED")
    return 0 if ok else 1


def summary() -> None:
    rows = []
    for name in sorted(os.listdir(EVIDENCE)) if os.path.isdir(EVIDENCE) else []:
        m = re.match(r"issues-(\d{4})\.jsonl$", name)
        if not m:
            continue
        lines = load_evidence(int(m.group(1)))
        read = [l for l in lines.values() if l.get("sha256") and not l.get("error")]
        counts = {}
        for l in read:
            for h in l["hits"]:
                counts[(h["class"], h["instrument"])] = counts.get((h["class"], h["instrument"]), 0) + 1
        rows.append((m.group(1), len(read), sum(1 for l in read if l.get("blind")), sum(1 for l in lines.values() if l.get("error")),
                     sum(1 for l in read if l.get("dateSource") == "unread"), sum(1 for l in read if l.get("decodedLines")),
                     sum(1 for l in read if l.get("readable") is False), counts,
                     sorted({l.get("patternsVersion") for l in read})))
    out = ["# The Official Journal scan: summary", "",
           "French edition, from issue 1 of 2003. Specification: `../../specs/official-journal-scan.md`. "
           "One line per issue in `issues-<year>.jsonl`.", "",
           "An issue with **decoded lines** printed part of its text in a font whose text layer is shifted; those lines were "
           "decoded before the search (see `TEXT_VERSION` in the script).", "",
           "**Unread** issues are those whose text could not be read: blind (no text layer) or failing the readability check "
           "(a page of enough words that does not read as French). They are listed as unread, never as \"no match\", and "
           "their tables of contents are read from page images by the triage.", "",
           "| Year | Issues fetched | Unread | of which blind | In error | Date unread | With decoded lines | Title hits | Operative hits | Other hits | Patterns |",
           "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|"]
    for year, n, blind, err, unread, dec, unr, counts, pv in rows:
        by = lambda cls: sum(v for (c, _), v in counts.items() if c == cls)  # noqa: E731
        out.append(f"| {year} | {n} | {unr} | {blind} | {err} | {unread} | {dec} | {by('title')} | {by('operative')} | {by('other')} | {','.join(map(str, pv))} |")
    with open(os.path.join(EVIDENCE, "SUMMARY.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out) + "\n")
    print("\n".join(out))


def main(argv: list[str] | None = None) -> int:
    for key in ("OPENROUTER_API_KEY", "GOOGLE_API_KEY"):
        os.environ.pop(key, None)
    parser = argparse.ArgumentParser(prog="journal_scan")
    sub = parser.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("run")
    r.add_argument("--years", help="comma-separated years, in this order")
    r.add_argument("--to-year", type=int, default=dt.date.today().year)
    sub.add_parser("search")
    sub.add_parser("known")
    sub.add_parser("summary")
    mo = sub.add_parser("model")
    mo.add_argument("--years", default="2007", help="years whose kept text trains the letter-pair model")
    args = parser.parse_args(argv)
    if args.cmd == "model":
        import glob as _glob
        paths = [p for y in args.years.split(",") for p in sorted(_glob.glob(os.path.join(TEXT, y, "*.txt")))]
        m = build_model(paths)
        with open(MODEL_PATH, "w", encoding="utf-8", newline="\n") as f:
            json.dump(m, f, indent=0)
        print(json.dumps({k: m[k] for k in ("pages", "pageFloor", "cleanPageScores")}))
        return 0
    if args.cmd == "run":
        years = [int(y) for y in args.years.split(",")] if args.years else \
            [2005, 2007, 2022] + [y for y in range(FIRST_YEAR, args.to_year + 1) if y not in (2005, 2007, 2022)]
        return run(years)
    if args.cmd == "search":
        research(); return 0
    if args.cmd == "known":
        return known()
    summary()
    return 0


if __name__ == "__main__":
    sys.exit(main())
