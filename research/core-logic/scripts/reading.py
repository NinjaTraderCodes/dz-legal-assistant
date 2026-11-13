"""Page readings of Official Journal articles (docs/research/core-logic/law/README.md, "Reading an article" and
"Re-readings"; prompt docs/research/core-logic/prompts/law-read-page.md).

A job is one reading assignment: the page images of one issue (one page or a pair; up to four only for articles that
are not in critical use) and the articles asked for. The same job is given to each reader. Answers are compared by
script with the store's text (a re-reading) or with the other family's reading (a missing article).

    python reading.py job <batch> <job> --code C --version V --pdf P --pages 35[,36] --articles "419,423" --kind K [--why W]
    python reading.py prompt <job.json> sonnet|codex
    python reading.py codex <job.json>
    python reading.py ingest <job.json> <answer.json> --reader claude-sonnet-5-5 [--tool-uses N]
    python reading.py calibration <batch>

Working files live outside the repository: C:\\Users\\TheShadow\\dz-core-research\\pages\\ (images) and \\jobs\\ (jobs,
answers, comparisons). The only file of the repository written here is law/readings.jsonl.
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
import codex_run  # noqa: E402
import store as st  # noqa: E402
import textcmp  # noqa: E402

WORK = r"C:\Users\TheShadow\dz-core-research"
PAGES = os.path.join(WORK, "pages")
JOBS = os.path.join(WORK, "jobs")
PROMPTS = os.path.join(st.REPO, "docs", "research", "core-logic", "prompts")
TEMPLATE = os.path.join(PROMPTS, "law-read-page.md")
SCHEMA = os.path.join(PROMPTS, "schemas", "page-reading.schema.json")
READINGS = os.path.join(st.LAW, "readings.jsonl")
PDFTOPPM = r"C:\Users\TheShadow\AppData\Local\Programs\MiKTeX\miktex\bin\x64\pdftoppm.exe"
SONNET, CODEX_MODEL = "claude-sonnet-5-5", "codex"
KINDS = ("reread", "fetch", "calibration")
WHYS = ("criticalUse", "corpusOnly", "labour", "guard", "disagreement", "missing", "calibration")
UNREADABLE = "[؟]"


def sha_file(path: str) -> str:
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def sha_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


# --- images ----------------------------------------------------------------------------------------------------------
def render(pdf: str, page: int, code: str, dpi: int = 300) -> str:
    """The full page as PNG, rendered once and kept."""
    out_dir = os.path.join(PAGES, code)
    os.makedirs(out_dir, exist_ok=True)
    stem = os.path.splitext(os.path.basename(pdf))[0]
    prefix = os.path.join(out_dir, f"{stem}-p{page:03d}-{dpi}")
    path = prefix + ".png"
    if not os.path.exists(path):
        exe = PDFTOPPM if os.path.exists(PDFTOPPM) else "pdftoppm"
        subprocess.run([exe, "-r", str(dpi), "-f", str(page), "-l", str(page), "-png", "-singlefile", pdf, prefix],
                       check=True, capture_output=True)
    return path


# --- jobs ------------------------------------------------------------------------------------------------------------
def job_path(batch: str, job: str) -> str:
    return os.path.join(JOBS, batch, f"{job}.json")


def make_job(batch: str, job: str, code: str, version: str, pdf: str, pages: list[int], articles: list[str], kind: str,
             why: str | None = None, language: str = "Arabic", source_url: str | None = None, dpi: int = 300,
             critical: bool = True) -> dict:
    if kind not in KINDS:
        raise ValueError(f"kind must be one of {KINDS}")
    if critical and len(pages) > 2:
        raise ValueError("an article in critical use is read on one page or a pair of pages per run (owner, 13 Nov)")
    if len(pages) > 4:
        raise ValueError("at most four pages of one issue per run")
    pdf_abs = pdf if os.path.isabs(pdf) else os.path.join(st.REPO, pdf)
    images = [{"path": render(pdf_abs, p, code, dpi), "pdfPage": p} for p in pages]
    for im in images:
        im["sha256"] = sha_file(im["path"])
    doc = {"batch": batch, "job": job, "kind": kind, "why": why or kind, "code": code, "version": version,
           "language": language, "critical": critical, "dpi": dpi,
           "source": {"url": source_url or pdf.replace("\\", "/"), "sha256": sha_file(pdf_abs), "localPdf": pdf_abs},
           "pages": pages, "images": images, "articles": articles}
    os.makedirs(os.path.dirname(job_path(batch, job)), exist_ok=True)
    with open(job_path(batch, job), "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
    return doc


def load(path: str) -> dict:
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# --- the prompt ------------------------------------------------------------------------------------------------------
def template_parts() -> dict:
    with open(TEMPLATE, encoding="utf-8") as f:
        text = f.read()
    parts = re.split(r"^--- (.+?) ---$", text, flags=re.M)
    out = {"header": parts[0]}
    for i in range(1, len(parts), 2):
        out[parts[i].strip()] = parts[i + 1].strip("\n")
    return out


def template_version() -> str:
    return sha_file(TEMPLATE)[:12]


def image_note(job: dict) -> str:
    pages = job["pages"]
    if len(pages) == 1:
        return f"One image: page {pages[0]} of the issue's PDF."
    words = {2: "Two", 3: "Three", 4: "Four"}[len(pages)]
    return f"{words} images, in order: pages {', '.join(map(str, pages[:-1]))} and {pages[-1]} of the issue's PDF."


def article_numbers(job: dict) -> str:
    if job["language"] == "French":
        return ", ".join(f"Art. {a}" for a in job["articles"])
    return "، ".join(f"المادة {a}" for a in job["articles"])


def prompt(job: dict, who: str) -> str:
    p = template_parts()
    body = p["PROMPT"]
    fills = {"language": job["language"], "image_note": image_note(job), "article_numbers": article_numbers(job),
             "worktree": st.REPO, "image_paths": "\n".join(f"- `{im['path']}`" for im in job["images"])}
    if who == "sonnet":
        text = "\n\n".join([p["PREAMBLE (Sonnet agent only)"], body, p["LAST LINE (Sonnet agent)"]])
    elif who == "codex":
        text = "\n\n".join([body, p["LAST LINE (Codex)"]])
    else:
        raise ValueError(who)
    for k, v in fills.items():
        text = text.replace("{{" + k + "}}", v)
    left = re.findall(r"\{\{\w+\}\}", text)
    if left:
        raise ValueError(f"unfilled placeholders: {left}")
    return text


# --- Codex -----------------------------------------------------------------------------------------------------------
def run_codex(job: dict) -> dict:
    out_dir = os.path.join(JOBS, job["batch"])
    r = codex_run.call(prompt(job, "codex"), "page-reading", job["batch"], job["job"], SCHEMA,
                       images=[im["path"] for im in job["images"]], provenance=os.path.join(out_dir, "codex-calls.jsonl"),
                       template="law-read-page.md", template_version=template_version())
    if r["status"] == codex_run.ACCEPTED:
        with open(os.path.join(out_dir, f"{job['job']}.codex.json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(r["answer"], f, ensure_ascii=False, indent=1)
    return r


# --- comparing a reading ---------------------------------------------------------------------------------------------
def parse_answer(text: str) -> dict:
    """A Sonnet agent replies with the JSON object alone; tolerate a code fence around it, nothing else."""
    t = text.strip()
    m = re.fullmatch(r"```(?:json)?\s*(.*?)\s*```", t, flags=re.S)
    if m:
        t = m.group(1)
    answer = json.loads(t)
    with open(SCHEMA, encoding="utf-8") as f:
        errors = codex_run.validate(json.load(f), answer)
    if errors:
        raise ValueError(f"the answer does not match the schema: {errors[:3]}")
    return answer


def compare_article(store_paragraphs: list[str], reading: dict) -> dict:
    """One article of a reading against the store's text: the level and the differences (law/README.md)."""
    if not reading["found"]:
        return {"level": "none", "result": "notFound", "differences": []}
    text = "\n".join(reading["paragraphs"])
    if UNREADABLE in text or reading["unreadable"]:
        # A reading with a [؟] mark is not a reading: re-render at a higher resolution and read again.
        return {"level": "none", "result": "unreadableMark", "differences": []}
    if reading["continuesOnNextPage"]:
        return {"level": "none", "result": "incomplete", "differences": []}
    store_text = "\n".join(store_paragraphs)
    lv = textcmp.level(store_text, text)
    diffs = []
    pending = {"settled": None, "by": None, "seen": None, "sure": None}
    if lv == "3":  # the same words in order, divided into paragraphs differently
        diffs.append({"paragraph": None, "kind": "paragraphBreak", "store": " | ".join(store_paragraphs),
                      "reading": " | ".join(reading["paragraphs"]), **pending})
    elif lv == "none":
        for d in textcmp.differences(store_text, text):
            diffs.append({"paragraph": None, "kind": "words", "store": d["a"], "reading": d["b"],
                          "context": {"before": d["before"], "after": d["after"]}, **pending})
    return {"level": lv, "result": "agrees" if lv in ("1", "2", "2o") else "unsettled", "differences": diffs}


def family_of(reader: str) -> str:
    return "codex" if reader.startswith("codex") else "claude"


def ingest(job: dict, answer: dict, reader: str, tool_uses: int | None = None, date: str | None = None,
           codex_log: str | None = None, store: st.Store | None = None, write: bool = True) -> list[dict]:
    """Compare every asked article with the store. Writes the job's comparison file and, for a re-reading that counts,
    one line per article in law/readings.jsonl."""
    import datetime as dt
    if tool_uses is not None and tool_uses > 2 * len(job["images"]):
        raise ValueError(f"discard: {tool_uses} tool uses for {len(job['images'])} images (law-read-page.md)")
    store = store or st.Store()
    date = date or dt.date.today().isoformat()
    by_article = {a["article"]: a for a in answer["articles"]}
    text_sha = sha_text(json.dumps(answer, ensure_ascii=False, sort_keys=True))
    rows = []
    for art in job["articles"]:
        reading = by_article.get(art)
        e = store.get(job["code"], art, job["version"])
        if reading is None:
            cmp = {"level": "none", "result": "notAnswered", "differences": []}
        elif e is None:
            cmp = {"level": None, "result": "noStoreText", "differences": []}
        else:
            cmp = compare_article(e["paragraphs"], reading)
        page = job["pages"][max(0, (reading or {}).get("where", {}).get("image", 1) - 1)] if reading else None
        rows.append({
            "code": job["code"], "article": art, "version": job["version"], "why": job["why"],
            "reader": reader, "family": family_of(reader), "date": date,
            "source": {"url": job["source"]["url"], "sha256": job["source"]["sha256"], "pdfPage": page, "printedPage": None},
            "imageSha256s": [im["sha256"] for im in job["images"]],
            "textSha256": sha_text("\n".join((reading or {}).get("paragraphs") or [])),
            "answerSha256": text_sha, "codexLog": codex_log,
            "comparedWith": f"store:{(e or {}).get('store')}@{st.CORPUS_COMMIT}" if e else None,
            "storeTextSha256": (e or {}).get("textSha256"),
            "level": cmp["level"], "differences": cmp["differences"], "result": cmp["result"],
            "job": f"{job['batch']}/{job['job']}", "pagesInRun": len(job["pages"]),
        })
    if write:
        out = os.path.join(JOBS, job["batch"], f"{job['job']}.{family_of(reader)}.cmp.json")
        with open(out, "w", encoding="utf-8", newline="\n") as f:
            json.dump(rows, f, ensure_ascii=False, indent=1)
        counts = job["kind"] == "reread" or (job["kind"] == "calibration" and job["critical"] and len(job["pages"]) <= 2)
        if counts:
            with open(READINGS, "a", encoding="utf-8", newline="\n") as f:
                for r in rows:
                    if r["storeTextSha256"] and r["result"] in ("agrees", "unsettled"):
                        f.write(json.dumps(r, ensure_ascii=False) + "\n")
    return rows


# --- calibration: one page against four ------------------------------------------------------------------------------
def word_errors(store_paragraphs: list[str], reading: dict) -> tuple[int, int]:
    """(differing words, words in the store's text) at level 2."""
    a = textcmp.norm2("\n".join(store_paragraphs), keep_lines=False).split()
    b = textcmp.norm2("\n".join(reading.get("paragraphs") or []), keep_lines=False).split()
    import difflib
    sm = difflib.SequenceMatcher(a=a, b=b, autojunk=False)
    same = sum(blk.size for blk in sm.get_matching_blocks())
    return max(len(a), len(b)) - same, len(a)


def calibration(batch: str, exclude: set[str] | None = None) -> dict:
    exclude = exclude or set()
    folder = os.path.join(JOBS, batch)
    out = {}
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".cmp.json"):
            continue
        job = load(os.path.join(folder, name.split(".")[0] + ".json"))
        fam = name.split(".")[1]
        cond = f"{len(job['pages'])}page"
        rows = load(os.path.join(folder, name))
        answer = load(os.path.join(folder, f"{job['job']}.{'codex' if fam == 'codex' else 'sonnet'}.json"))
        by = {a["article"]: a for a in answer["articles"]}
        s = st.Store()
        for r in rows:
            if r["article"] in exclude:
                continue
            k = out.setdefault(f"{fam}|{cond}", {"articles": 0, "disagree": 0, "words": 0, "wordErrors": 0, "notOk": []})
            k["articles"] += 1
            if r["result"] != "agrees":
                k["disagree"] += 1
                k["notOk"].append(f"{r['article']}:{r['result']}:{r['level']}")
            e = s.get(job["code"], r["article"], job["version"])
            if e and r["article"] in by:
                we, n = word_errors(e["paragraphs"], by[r["article"]])
                k["wordErrors"] += we
                k["words"] += n
    for k in out.values():
        k["disagreementRate"] = round(k["disagree"] / k["articles"], 4) if k["articles"] else None
        k["wordErrorRate"] = round(k["wordErrors"] / k["words"], 5) if k["words"] else None
    return out


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(prog="reading")
    sub = p.add_subparsers(dest="cmd", required=True)
    j = sub.add_parser("job")
    for a in ("batch", "job"):
        j.add_argument(a)
    j.add_argument("--code", required=True); j.add_argument("--version", required=True); j.add_argument("--pdf", required=True)
    j.add_argument("--pages", required=True); j.add_argument("--articles", required=True)
    j.add_argument("--kind", required=True, choices=KINDS); j.add_argument("--why", choices=WHYS)
    j.add_argument("--language", default="Arabic"); j.add_argument("--url"); j.add_argument("--dpi", type=int, default=300)
    j.add_argument("--not-critical", action="store_true")
    pr = sub.add_parser("prompt"); pr.add_argument("job"); pr.add_argument("who", choices=("sonnet", "codex"))
    cx = sub.add_parser("codex"); cx.add_argument("job")
    ig = sub.add_parser("ingest"); ig.add_argument("job"); ig.add_argument("answer"); ig.add_argument("--reader", required=True)
    ig.add_argument("--tool-uses", type=int); ig.add_argument("--codex-log")
    ca = sub.add_parser("calibration"); ca.add_argument("batch"); ca.add_argument("--exclude", default="")
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    if a.cmd == "job":
        doc = make_job(a.batch, a.job, a.code, a.version, a.pdf, [int(x) for x in a.pages.split(",")],
                       [x.strip() for x in a.articles.split(",") if x.strip()], a.kind, a.why, a.language, a.url, a.dpi,
                       critical=not a.not_critical)
        print(job_path(a.batch, a.job), [im["path"] for im in doc["images"]])
    elif a.cmd == "prompt":
        print(prompt(load(a.job), a.who))
    elif a.cmd == "codex":
        r = run_codex(load(a.job))
        print(json.dumps({k: r[k] for k in ("status", "reason", "callId", "log")}, ensure_ascii=False))
        return 0 if r["status"] == codex_run.ACCEPTED else 1
    elif a.cmd == "ingest":
        with open(a.answer, encoding="utf-8") as f:
            answer = parse_answer(f.read())
        rows = ingest(load(a.job), answer, a.reader, a.tool_uses, codex_log=a.codex_log)
        for r in rows:
            print(r["article"], r["result"], r["level"], len(r["differences"]))
    elif a.cmd == "calibration":
        print(json.dumps(calibration(a.batch, set(filter(None, a.exclude.split(",")))), ensure_ascii=False, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
