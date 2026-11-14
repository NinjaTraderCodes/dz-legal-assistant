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
UNCHANGED = "بدون تغيير"  # «(الباقي بدون تغيير)», «(المطات من 1 إلى 6 بدون تغيير)»
ITEM = re.compile(r"^\s*(\d+)\s*[-–—ـ]")  # the 1975 Journal numbers its items «1 ـ», with a tatweel


def _blocks(paragraphs: list[str]) -> tuple[list[str], dict[str, list[str]], list[str]]:
    """(lead paragraphs, numbered item -> its paragraphs with the unnumbered ones that follow it, item order)."""
    lead, items, order, cur = [], {}, [], None
    for p in paragraphs:
        m = ITEM.match(p)
        if m:
            cur = m.group(1)
            items[cur] = [p]
            order.append(cur)
        elif cur is None:
            lead.append(p)
        else:
            items[cur].append(p)
    return lead, items, order


def compose(base: list[str], amending: list[str] | None = None, repealed: list[int] | None = None) -> list[str]:
    """The wording an amending text makes of a base article. `amending`: the amending text as printed, where
    «… بدون تغيير» stands for what it keeps; its lead paragraphs replace the base's first ones, one for one, and each
    numbered item replaces the base's item of the same number (with its sub-paragraphs). `repealed`: paragraph
    numbers (from 1) of the base that the amending text repeals."""
    out = [p for i, p in enumerate(base, 1) if i not in set(repealed or [])]
    if not amending:
        return out
    printed = [p for p in amending if UNCHANGED not in p]
    a_lead, a_items, _ = _blocks(printed)
    b_lead, b_items, b_order = _blocks(out)
    if len(a_lead) > len(b_lead):
        raise ValueError("the amending text has more lead paragraphs than the base")
    new_lead = a_lead + b_lead[len(a_lead):]
    unknown = [n for n in a_items if n not in b_items]
    if unknown:
        raise ValueError(f"the amending text adds items {unknown}: compose them by hand, with a recorded decision")
    return new_lead + [p for n in b_order for p in (a_items.get(n) or b_items[n])]


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
        import french  # the French edition numbers inserted articles «bis, ter, …», not «مكرر»
        return ", ".join("Art. " + " (or ".join(french.french_number(a)) + (")" if len(french.french_number(a)) > 1 else "")
                         for a in job["articles"])
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
    call_id, n = job["job"], 1
    while os.path.exists(os.path.join(codex_run.LOGS, "page-reading", job["batch"], call_id + ".jsonl")):
        n += 1  # a call sent afresh after a discarded one keeps the first call's log for the record
        call_id = f"{job['job']}-r{n}"
    r = codex_run.call(prompt(job, "codex"), "page-reading", job["batch"], call_id, SCHEMA,
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


def compare_article(store_paragraphs: list[str], reading: dict, images: int = 1) -> dict:
    """One article of a reading against the store's text: the level and the differences (law/README.md).
    `images` is the number of images in the run: an article that "continues" only counts as cut off when it sits on
    the last image (a reader may set the flag for an article it followed onto the next image of the same run)."""
    if not reading["found"]:
        return {"level": "none", "result": "notFound", "differences": []}
    reading, dropped = drop_heading(reading)
    text = "\n".join(reading["paragraphs"])
    if UNCHANGED in text:
        # An amending text that reprints only the replaced part: compared only once composed with the base text.
        return {"level": None, "result": "partialPrint", "differences": []}
    if UNREADABLE in text:  # an "unreadable" note without a [؟] in the text is a remark (punctuation), kept apart
        # A reading with a [؟] mark is not a reading: re-render at a higher resolution and read again.
        return {"level": "none", "result": "unreadableMark", "differences": []}
    if reading["continuesOnNextPage"] and reading["where"]["image"] >= images:
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
    out = {"level": lv, "result": "agrees" if lv in ("1", "2", "2o", "2s") else "unsettled", "differences": diffs}
    if dropped:
        out["headingDropped"] = dropped
    return out


def drop_heading(reading: dict) -> tuple[dict, str | None]:
    """The reading prompt asks for the article without its heading «المادة …». An amending text prints the amended
    article inside quotation marks with its heading («"المادة 121 : في العقود …»), and a reader may copy it (Civil Code
    121, 14 November 2026; the settler confirmed it is the heading, not the paragraph). Only the heading of the article
    asked is dropped, and only at the very start of the first paragraph; the result says what was dropped."""
    if not reading.get("paragraphs"):
        return reading, None
    number = re.escape(str(reading.get("article", "")).translate(textcmp.DIGITS)).replace(r"\ ", r"\s+")
    first = reading["paragraphs"][0].translate(textcmp.DIGITS)
    m = re.match(r'^[\s"«“]*المـ*ادة\s+' + number + r"\s*:?\s*", first)
    if not m or not number:
        return reading, None
    rest = reading["paragraphs"][0][m.end():] if len(first) == len(reading["paragraphs"][0]) else first[m.end():]
    return dict(reading, paragraphs=[rest] + reading["paragraphs"][1:]), m.group(0).strip()


def family_of(reader: str) -> str:
    return "codex" if reader.startswith("codex") else "claude"


SETTLEMENTS = os.path.join(JOBS, "settlements.jsonl")


def settlements() -> list[dict]:
    if not os.path.exists(SETTLEMENTS):
        return []
    with open(SETTLEMENTS, encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]


def settled_result(job: dict, art: str, cmp: dict, versions: tuple | None = None) -> str:
    """Apply the recorded settlements (law/README.md, "When one reading differs from the store's text"): every
    difference settled, sure, for the store's words -> agrees; settled, sure, for the reading and confirmed by the other
    family -> storeDiffers (a correction); anything else stays unsettled."""
    if cmp["result"] != "unsettled":
        return cmp["result"]
    versions = versions or (job["version"],)  # a composed wording also takes the settlements of its base page
    known = [s for s in settlements() if s["code"] == job["code"] and s["article"] == art and s["version"] in versions]
    verdicts = []
    for d in cmp["differences"]:
        if textcmp.level(d["store"] or "", d["reading"] or "") in ("1", "2", "2o", "2s"):
            verdicts.append("store")  # a spacing or hamza-seat difference left inside a longer one: orthographic
            continue
        s = next((s for s in reversed(known) if s["storeWords"] == d["store"] and s["readingWords"] == d["reading"]), None)  # the latest run wins
        if s:
            d.update(settled=s["settled"], by=s["by"], seen=s["seen"], sure=s["sure"])
        verdicts.append(s["verdict"] if s else "open")
    if verdicts and all(v == "store" for v in verdicts):
        return "agrees"
    if "reading" in verdicts and "open" not in verdicts:
        return "storeDiffers"
    return "unsettled"


# The results a re-reading records in law/readings.jsonl (law/README.md, "Re-readings").
RECORDED = ("agrees", "storeDiffers", "unsettled")


def ingest(job: dict, answer: dict, reader: str, tool_uses: int | None = None, date: str | None = None,
           codex_log: str | None = None, store: st.Store | None = None, write: bool = True,
           skip: dict | None = None, why: str | None = None) -> list[dict]:
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
        if art in (skip or {}):
            # A job that did not hold the whole article (for example a page record that misses its continuation):
            # recorded with the reason, never written as a reading.
            cmp = {"level": None, "result": f"skipped: {skip[art]}", "differences": []}
        elif reading is None:
            cmp = {"level": "none", "result": "notAnswered", "differences": []}
        elif e is None:
            cmp = {"level": None, "result": "noStoreText", "differences": []}
        else:
            cmp = compare_article(e["paragraphs"], reading, len(job["images"]))
        page = job["pages"][max(0, (reading or {}).get("where", {}).get("image", 1) - 1)] if reading else None
        rows.append({
            "code": job["code"], "article": art, "version": job["version"], "why": why or job["why"],
            "reader": reader, "family": family_of(reader), "date": date,
            "source": {"url": job["source"]["url"], "sha256": job["source"]["sha256"], "pdfPage": page, "printedPage": None},
            "imageSha256s": [im["sha256"] for im in job["images"]],
            "textSha256": sha_text("\n".join((reading or {}).get("paragraphs") or [])),
            "answerSha256": text_sha, "codexLog": codex_log,
            "comparedWith": f"store:{(e or {}).get('store')}@{st.CORPUS_COMMIT}" if e else None,
            "storeTextSha256": (e or {}).get("textSha256"),
            "level": cmp["level"], "differences": cmp["differences"], "result": settled_result(job, art, cmp),
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
                    if r["storeTextSha256"] and r["result"] in RECORDED:
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


# --- collecting a Claude agent's answer from its transcript ----------------------------------------------------------
TRANSCRIPTS = os.environ.get("DZ_AGENT_TRANSCRIPTS", r"C:\Users\TheShadow\.claude\projects\c--Users-TheShadow-dz-legal-assistant"
                             r"\cf0c5771-14d9-4f6e-9456-6f9e2f4bf0b0\subagents")
ALLOWED_TOOLS = {"Read", "SubagentHandback", "StructuredOutput"}


def strip_extra(answer: dict) -> tuple[dict, list[str]]:
    """Keep only the schema's keys; report what was removed (a remark the reader added beside its answer)."""
    removed = [f"top:{k}" for k in answer if k != "articles"]
    keys = ("article", "found", "paragraphs", "unreadable", "continuesOnNextPage", "numbersSeen", "where")
    where_keys = ("image", "column", "top", "bottom")
    arts = []
    for a in answer.get("articles", []):
        removed += [f"{a.get('article')}:{k}" for k in a if k not in keys]
        a2 = {k: a[k] for k in keys if k in a}
        if isinstance(a2.get("where"), dict):
            removed += [f"{a.get('article')}:where.{k}" for k in a2["where"] if k not in where_keys]
            a2["where"] = {k: a2["where"][k] for k in where_keys if k in a2["where"]}
        arts.append(a2)
    return {"articles": arts}, removed


def first_json(text: str) -> tuple[dict, str]:
    """The JSON object a reader returned, and any text it wrote after it (kept apart, never used as a reading)."""
    t = text.strip()
    m = re.match(r"```(?:json)?\s*", t)
    if m:
        t = t[m.end():]
    obj, end = json.JSONDecoder().raw_decode(t)
    rest = t[end:].strip()
    rest = re.sub(r"^```", "", rest).strip()
    return obj, rest


def collect(job: dict, agent_id: str, allowed_images: list[str] | None = None) -> dict:
    """The agent's final answer, copied by script from its transcript (never retyped), with the access check: every
    tool call is a Read of one of the job's images, or the final hand-back; at most twice as many as images."""
    path = os.path.join(TRANSCRIPTS, f"agent-{agent_id}.jsonl")
    allowed = {os.path.normcase(os.path.normpath(p)) for p in (allowed_images or [im["path"] for im in job["images"]])}
    calls, final = [], None
    with open(path, encoding="utf-8") as f:
        for line in f:
            d = json.loads(line)
            if d.get("type") != "assistant":
                continue
            for c in d.get("message", {}).get("content") or []:
                if c.get("type") != "tool_use":
                    continue
                calls.append(c)
                if c["name"] == "SubagentHandback":
                    final = c["input"]["message"]
    problems = []
    for c in calls:
        if c["name"] not in ALLOWED_TOOLS:
            problems.append(f"tool {c['name']}")
        elif c["name"] == "Read" and os.path.normcase(os.path.normpath(c["input"].get("file_path", ""))) not in allowed:
            problems.append(f"read outside the job: {c['input'].get('file_path')}")
    work = [c for c in calls if c["name"] not in ("SubagentHandback", "StructuredOutput")]
    if len(work) > 2 * len(allowed):
        problems.append(f"{len(work)} tool uses for {len(allowed)} images")
    if final is None:
        problems.append("no final answer")
    return {"answerText": final, "toolUses": len(work), "problems": problems, "transcript": path}


# --- settling a difference (prompts/law-settle-difference.md) --------------------------------------------------------
SETTLE_TEMPLATE = os.path.join(PROMPTS, "law-settle-difference.md")


def band(job: dict, where: dict, dpi: int = 600, margin: float = 0.03) -> str:
    """A band of the page around the article, cut at 600 dpi from where the first reader said it is."""
    page = job["pages"][max(0, where["image"] - 1)]
    pdf = job["source"]["localPdf"]
    stem = os.path.splitext(os.path.basename(pdf))[0]
    out_dir = os.path.join(PAGES, job["code"], "bands")
    os.makedirs(out_dir, exist_ok=True)
    top, bottom = max(0.0, where["top"] - margin), min(1.0, where["bottom"] + margin)
    # the page's size in points, so that the band is cut in pixels at this resolution
    info = subprocess.run([PDFTOPPM.replace("pdftoppm", "pdfinfo"), "-f", str(page), "-l", str(page), pdf],
                          capture_output=True, text=True).stdout
    m = re.search(r"Page\s+%d size:\s+([\d.]+) x ([\d.]+)" % page, info) or re.search(r"Page size:\s+([\d.]+) x ([\d.]+)", info)
    w_pt, h_pt = float(m.group(1)), float(m.group(2))
    W, H = int(w_pt / 72 * dpi), int(h_pt / 72 * dpi)
    x0, x1 = {"right": (0.48, 1.0), "left": (0.0, 0.52)}.get(where["column"], (0.0, 1.0))
    x, y, w, h = int(W * x0), int(H * top), int(W * (x1 - x0)), int(H * (bottom - top))
    prefix = os.path.join(out_dir, f"{stem}-p{page:03d}-{dpi}-{where['column']}-{int(top * 1000):03d}-{int(bottom * 1000):03d}")
    if not os.path.exists(prefix + ".png"):
        subprocess.run([PDFTOPPM, "-r", str(dpi), "-f", str(page), "-l", str(page), "-x", str(x), "-y", str(y),
                        "-W", str(w), "-H", str(h), "-png", "-singlefile", pdf, prefix], check=True, capture_output=True)
    return prefix + ".png"


def column_bands(job: dict, dpi: int = 600) -> list[str]:
    """Both columns of every page of the job at 600 dpi (the first settle runs showed that a band cut around the
    article's start misses a difference printed in the other column or on the next page)."""
    out = []
    for i in range(1, len(job["pages"]) + 1):
        for col in ("right", "left"):
            out.append(band(job, {"image": i, "column": col, "top": 0.0, "bottom": 1.0}, dpi=dpi, margin=0.0))
    return out


# The settler's image viewer reduces a large image before the model sees it: a 600-dpi column (2409 x 6533 pixels) or
# an 800-dpi one (3212 x 8711) reached the settler at about 185 dpi, no sharper than the 300-dpi page (13 November
# 2026, settle-cons-b-800). A close-up is therefore cut in tiles small enough to be shown at full size.
TILE_SIDE, TILE_AREA, TILE_OVERLAP = 1400, 1_100_000, 150


def tiles(job: dict, where: dict, dpi: int = 600) -> list[dict]:
    """Full-resolution tiles of the region where the first reader placed the article, with a margin, and of the next
    column too when the article reaches the foot of its column. Tiles overlap, so no printed line is only ever cut."""
    from PIL import Image
    cols = [where["column"]] if where["column"] in ("right", "left") else ["right", "left"]
    regions = [(where["image"], c, max(0.0, where["top"] - 0.08), min(1.0, where["bottom"] + 0.08)) for c in cols]
    if where["bottom"] > 0.85 or where.get("continues"):
        if cols[-1] == "right":
            regions.append((where["image"], "left", 0.0, 0.45))
        elif where["image"] < len(job["pages"]):
            regions.append((where["image"] + 1, "right", 0.0, 0.45))
    out = []
    for img, col, top, bottom in regions:
        if img < 1:
            continue
        path = band(job, {"image": img, "column": col, "top": 0.0, "bottom": 1.0}, dpi=dpi, margin=0.0)
        tdir = os.path.join(os.path.dirname(path), "tiles")
        os.makedirs(tdir, exist_ok=True)
        im = Image.open(path)
        w, h = im.size
        n_w = -(-w // TILE_SIDE)
        tw = min(w, -(-w // n_w) + (TILE_OVERLAP if n_w > 1 else 0))
        th = min(TILE_SIDE, TILE_AREA // tw)
        y, y1 = int(top * h), int(bottom * h)
        while True:
            ye = min(y1, y + th)
            for k in range(n_w):
                x = 0 if k == 0 else min(w - tw, k * (w // n_w) - TILE_OVERLAP // 2)
                tp = os.path.join(tdir, f"{os.path.splitext(os.path.basename(path))[0]}-y{y:05d}-{ye:05d}-x{k}.png")
                if not os.path.exists(tp):
                    im.crop((x, y, x + tw, ye)).save(tp)
                part = ("whole width" if n_w == 1 else "left part" if k == 0 else
                        "right part" if k == n_w - 1 else f"part {k + 1} from the left")
                out.append({"path": tp, "image": img, "column": col, "from": round(y / h, 3), "to": round(ye / h, 3),
                            "part": part})
            if ye >= y1:
                break
            y = ye - TILE_OVERLAP
    return out


def settle_images(entries: list[dict]) -> list[str]:
    """Every image a settle run may open: the pages, and the close-ups (or the older column bands)."""
    return sorted({b for e in entries for b in e.get("bands", [])} | {t["path"] for e in entries for t in e.get("tiles", [])}
                  | {im["path"] for e in entries for im in e["job"]["images"]})


def settle_items(entries: list[dict], seed: str) -> tuple[str, list[dict]]:
    """entries: {job, article, paragraph, kind, store, reading, before, after, bands}. Returns the {{items}} text and the
    key (which of X and Y is the store's or first wording), kept by the script and never shown to the settler."""
    lines, key = [], []
    for n, e in enumerate(entries, 1):
        flip = int(hashlib.sha256(f"{seed}|{n}|{e['article']}".encode()).hexdigest(), 16) % 2 == 1
        x, y = (e["reading"], e["store"]) if flip else (e["store"], e["reading"])
        pages = "\n".join(f"  - `{im['path']}`" for im in e["job"]["images"])
        if e.get("tiles"):
            close = "\n".join(f"  - `{t['path']}`: page {t['image']}, {t['column']} column, {t['part']}, from "
                              f"{round(t['from'] * 100)} % to {round(t['to'] * 100)} % of the page's height"
                              for t in e["tiles"])
            close = ("- Close-ups at full resolution (600 dpi; open only the one or two that hold the line, after "
                     f"finding it on the page):\n{close}\n")
        else:
            bands = "\n".join(f"  - `{b}`" for b in e["bands"])
            close = f"- Bands (600 dpi; each page's right column, then its left column):\n{bands}\n"
        lines.append(
            f"**Difference {n}.** Article {e['article']}, paragraph {e.get('paragraph') or '?'}, kind `{e['kind']}`.\n"
            f"- Pages (300 dpi), in order:\n{pages}\n{close}"
            f"- Ten words before: «{e.get('before', '')}»\n- Ten words after: «{e.get('after', '')}»\n"
            f"- X: «{x}»\n- Y: «{y}»")
        key.append({"n": n, "article": e["article"], "code": e["job"]["code"], "version": e["job"]["version"],
                    "X": "reading" if flip else "store", "Y": "store" if flip else "reading",
                    "storeWords": e["store"], "readingWords": e["reading"]})
    return "\n\n".join(lines), key


def settle_prompt(items: str) -> str:
    with open(SETTLE_TEMPLATE, encoding="utf-8") as f:
        text = f.read().split("--- PROMPT ---", 1)[1].strip("\n")
    text = text.replace("{{worktree}}", st.REPO).replace("{{items}}", items)
    if re.findall(r"\{\{\w+\}\}", text):
        raise ValueError("unfilled placeholders")
    return text


def apply_settlement(key: list[dict], answer: list[dict], other_family_agrees: dict) -> list[dict]:
    """The rule of law/README.md, "When one reading differs from the store's text": the store's text is held wrong only
    when the settler is sure for the reading AND the other family's reading has the reading's words."""
    out = []
    for k, a in zip(key, answer):
        side = k.get(a["printed"]) if a["printed"] in ("X", "Y") else "neither"
        out.append({"article": k["article"], "settled": side, "sure": bool(a["sure"]), "seen": a["seen"],
                    "words": a["words"],
                    "verdict": ("store" if side == "store" and a["sure"] else
                                "reading" if side == "reading" and a["sure"] and other_family_agrees.get(k["article"]) else
                                "open")})
    return out


def settle_record(name: str, agent_id: str, settler: str = "claude-opus-5-5") -> list[dict]:
    """Collect a settle run's answer from its transcript (access check on the key's images), apply the rule, and append
    the verdicts to jobs/settlements.jsonl, which `ingest` reads. The other family "agrees with the reading" when its
    own reading of the article has the same differing words against the store."""
    folder = os.path.join(JOBS, f"settle-{name}")
    key = load(os.path.join(folder, "key.json"))
    got = collect({"images": []}, agent_id, allowed_images=key["images"])
    if got["problems"]:
        raise ValueError(f"discard the settle run: {got['problems']}")
    with open(os.path.join(folder, "answer.raw.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(got["answerText"])
    t = got["answerText"].strip()
    m = re.match(r"```(?:json)?\s*", t)
    answer, _ = json.JSONDecoder().raw_decode(t[m.end():] if m else t)
    norm = [{"printed": a.get("printed", a.get("decision", a.get("choice"))), "sure": bool(a.get("sure")),
             "seen": a.get("seen", ""), "words": a.get("words", "")} for a in answer]
    with open(READINGS, encoding="utf-8") as f:
        rows = [json.loads(line) for line in f if line.strip()]
    other = {}
    for k in key["key"]:
        other[k["article"]] = any(r["family"] == "codex" and r["code"] == k["code"] and r["article"] == k["article"]
                                  and r["version"] == k["version"] and
                                  any(d["reading"] == k["readingWords"] for d in r["differences"]) for r in rows)
    verdicts = apply_settlement(key["key"], norm, other)
    with open(SETTLEMENTS, "a", encoding="utf-8", newline="\n") as f:
        for k, v in zip(key["key"], verdicts):
            f.write(json.dumps({"code": k["code"], "article": k["article"], "version": k["version"],
                                "storeWords": k["storeWords"], "readingWords": k["readingWords"], "settled": v["settled"],
                                "sure": v["sure"], "seen": v["seen"], "by": settler, "verdict": v["verdict"],
                                "run": f"settle-{name}", "agent": agent_id}, ensure_ascii=False) + "\n")
    return verdicts


def settle_prep(name: str, family: str = "claude", recheck: bool = False) -> dict:
    """Every unsettled difference of law/readings.jsonl from one family's readings: the close-ups are cut, the items
    are written with X and Y in random order, and the key is kept apart in jobs/settle-<name>/. `recheck`: the settled
    differences too (a look again at full resolution)."""
    entries, seen = [], set()
    with open(READINGS, encoding="utf-8") as f:
        rows = [json.loads(line) for line in f if line.strip()]
    for r in rows:
        if r["family"] != family or (r["code"], r["article"]) in seen or not r["differences"]:
            continue
        if r["result"] != "unsettled" and not recheck:
            continue
        seen.add((r["code"], r["article"]))
        batch, jname = r["job"].split("/", 1)
        job = load(job_path(batch, jname))
        ans = load(os.path.join(JOBS, batch, f"{jname}.{'sonnet' if family == 'claude' else 'codex'}.json"))
        a = next(x for x in ans["articles"] if x["article"] == r["article"])
        close = tiles(job, dict(a["where"], continues=a.get("continuesOnNextPage")))
        for d in r["differences"]:
            if textcmp.level(d["store"] or "", d["reading"] or "") in ("1", "2", "2o", "2s"):
                continue  # orthographic (spacing, hamza seat): nothing to settle
            entries.append({"job": job, "tiles": close, "article": r["article"], "paragraph": d.get("paragraph"),
                            "kind": d["kind"], "store": d["store"], "reading": d["reading"],
                            "before": (d.get("context") or {}).get("before", ""),
                            "after": (d.get("context") or {}).get("after", ""), "readingLine": r["job"]})
    items, key = settle_items(entries, name)
    out = os.path.join(JOBS, f"settle-{name}")
    os.makedirs(out, exist_ok=True)
    prompt_path = os.path.join(out, "prompt.txt")
    with open(prompt_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(settle_prompt(items))
    images = settle_images(entries) + [prompt_path]
    with open(os.path.join(out, "key.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"key": key, "images": images, "lines": [e["readingLine"] for e in entries]}, f, ensure_ascii=False, indent=1)
    return {"items": len(entries), "images": images, "folder": out}


def ingest_composite(article: str, version: str, base: tuple[dict, dict], amending: tuple[dict, dict] | None,
                     reader: str, repealed: list[int] | None = None, date: str | None = None,
                     store: st.Store | None = None, write: bool = True, record: bool = True) -> dict:
    """One reading of an article in the wording an amending text made of it: the base page's reading composed with
    the amending page's reading (or with the repealed paragraphs), compared with the store's text of `version`.
    base, amending: (job, answer); both readings are by the same family (`reader`)."""
    import datetime as dt
    store = store or st.Store()
    b_job, b_ans = base
    b = next(a for a in b_ans["articles"] if a["article"] == article)
    parts = [b_job]
    for r in [b] + ([next(a for a in amending[1]["articles"] if a["article"] == article)] if amending else []):
        # A [؟] inside the «بدون تغيير» line (a reader unsure how many dots the print puts around it) touches no
        # word: compose drops that line. Anywhere else it means the part is not a reading.
        if not r["found"] or UNREADABLE in "\n".join(p for p in r["paragraphs"] if UNCHANGED not in p):
            raise ValueError(f"{article}: a part is not found or holds [؟]: not a reading")
        if [u for u in r["unreadable"] if UNCHANGED not in "".join(r["paragraphs"][u["paragraph"] - 1:u["paragraph"]])]:
            raise ValueError(f"{article}: a part has an unreadable mark outside the «بدون تغيير» lines")
    a_pars = None
    if amending:
        parts.append(amending[0])
        a_pars = next(a for a in amending[1]["articles"] if a["article"] == article)["paragraphs"]
    composed = compose(b["paragraphs"], a_pars, repealed)
    e = store.get(b_job["code"], article, version)
    cmp = compare_article(e["paragraphs"], {**b, "paragraphs": composed, "continuesOnNextPage": False, "unreadable": []})
    main_job = parts[-1]
    row = {"code": b_job["code"], "article": article, "version": version, "why": "criticalUse", "reader": reader,
           "family": family_of(reader), "date": date or dt.date.today().isoformat(),
           "source": {"url": main_job["source"]["url"], "sha256": main_job["source"]["sha256"],
                      "pdfPage": main_job["pages"][0], "printedPage": None},
           "imageSha256s": [im["sha256"] for j in parts for im in j["images"]],
           "textSha256": sha_text("\n".join(composed)), "answerSha256": None, "codexLog": None,
           "comparedWith": f"store:{e['store']}@{st.CORPUS_COMMIT}", "storeTextSha256": e["textSha256"],
           "level": cmp["level"], "differences": cmp["differences"],
           "result": settled_result({"code": b_job["code"], "version": version}, article, cmp, (version, b_job["version"])),
           "job": " + ".join(f"{j['batch']}/{j['job']}" for j in parts), "pagesInRun": len(b_job["pages"]),
           "composedFrom": {"base": f"{b_job['batch']}/{b_job['job']}",
                            "amending": f"{parts[-1]['batch']}/{parts[-1]['job']}" if amending else None,
                            "repealedParagraphs": repealed}}
    if write and row["result"] in RECORDED:
        with open(READINGS, "a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
    if write and record and row["result"] in RECORDED:
        log = os.path.join(JOBS, "composites.jsonl")
        with open(log, "a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps({"article": article, "version": version, "reader": reader, "base": row["composedFrom"],
                                "date": row["date"]}, ensure_ascii=False) + "\n")
    return row


def rebuild() -> int:
    """Recompute law/readings.jsonl from every recorded comparison (after a change of text-compare or of the store):
    the same answers, readers, skips and logs, compared again. Returns the number of lines written."""
    if os.path.exists(READINGS):
        os.remove(READINGS)
    open(READINGS, "w").close()
    s = st.Store()
    for batch in sorted(os.listdir(JOBS)):
        folder = os.path.join(JOBS, batch)
        if not os.path.isdir(folder):
            continue
        for name in sorted(os.listdir(folder)):
            if not name.endswith(".cmp.json"):
                continue
            jname, fam = name.split(".")[0], name.split(".")[1]
            rows = load(os.path.join(folder, name))
            job = load(os.path.join(folder, jname + ".json"))
            if job["kind"] == "fetch":
                if not job.get("rereadArticles") or rows[0]["why"] != job.get("rereadWhy"):
                    continue  # the collector's own comparison of a fetch answer, not a re-reading
                # the store's articles that a fetch job's pages also show (fetch.reread_ingest)
                job = dict(job, kind="reread", why=job["rereadWhy"], articles=job["rereadArticles"], job=jname)
            answer = load(os.path.join(folder, f"{jname}.{'codex' if fam == 'codex' else 'sonnet'}.json"))
            skip = {r["article"]: r["result"].split(": ", 1)[1] for r in rows if str(r["result"]).startswith("skipped: ")}
            ingest(job, answer, rows[0]["reader"], None, date=rows[0]["date"], codex_log=rows[0].get("codexLog"),
                   store=s, skip=skip, why=rows[0]["why"])
    comp = os.path.join(JOBS, "composites.jsonl")
    if os.path.exists(comp):
        with open(comp, encoding="utf-8") as f:
            for line in f:
                c = json.loads(line)
                b_batch, b_job = c["base"]["base"].split("/", 1)
                bj = load(job_path(b_batch, b_job))
                fam = "codex" if c["reader"].startswith("codex") else "sonnet"
                base = (bj, load(os.path.join(JOBS, b_batch, f"{b_job}.{fam}.json")))
                amending = None
                if c["base"]["amending"]:
                    a_batch, a_job = c["base"]["amending"].split("/", 1)
                    amending = (load(job_path(a_batch, a_job)), load(os.path.join(JOBS, a_batch, f"{a_job}.{fam}.json")))
                ingest_composite(c["article"], c["version"], base, amending, c["reader"], c["base"]["repealedParagraphs"],
                                 date=c["date"], store=s, record=False)
    with open(READINGS, encoding="utf-8") as f:
        return sum(1 for line in f if line.strip())


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
    ig.add_argument("--skip", action="append", default=[], help="ARTICLE=reason: compared, never written")
    ig.add_argument("--why", choices=WHYS, help="the reason for this reading, if not the job's (a disagreement re-reading)")
    ca = sub.add_parser("calibration"); ca.add_argument("batch"); ca.add_argument("--exclude", default="")
    co = sub.add_parser("collect"); co.add_argument("job"); co.add_argument("agent")
    co.add_argument("--ingest", action="store_true", help="then compare and record, as `ingest` does")
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
        rows = ingest(load(a.job), answer, a.reader, a.tool_uses, codex_log=a.codex_log,
                      skip=dict(x.split("=", 1) for x in a.skip), why=a.why)
        for r in rows:
            print(r["article"], r["result"], r["level"], len(r["differences"]))
    elif a.cmd == "collect":
        job = load(a.job)
        got = collect(job, a.agent)
        base = os.path.join(JOBS, job["batch"], job["job"])
        if got["problems"] == ["no final answer"]:
            print("NOT FINISHED (no final answer yet): nothing recorded")
            return 2
        if got["problems"]:
            # Discarded, like a Codex call that ran a command: nothing of its answer is kept or used.
            with open(base + ".sonnet.discarded.txt", "a", encoding="utf-8", newline="\n") as f:
                f.write(json.dumps({"agent": a.agent, "problems": got["problems"]}) + "\n")
            print("DISCARDED", got["problems"])
            return 1
        with open(base + ".sonnet.raw.txt", "w", encoding="utf-8", newline="\n") as f:
            f.write(got["answerText"])
        obj, after = first_json(got["answerText"])
        clean, removed = strip_extra(obj)
        if after:
            removed.append("text after the JSON: " + after[:300])
        answer = parse_answer(json.dumps(clean, ensure_ascii=False))
        with open(base + ".sonnet.json", "w", encoding="utf-8", newline="\n") as f:
            json.dump(answer, f, ensure_ascii=False, indent=1)
        with open(base + ".sonnet.meta.json", "w", encoding="utf-8", newline="\n") as f:
            json.dump({"agent": a.agent, "toolUses": got["toolUses"], "removedKeys": removed,
                       "transcript": got["transcript"]}, f, indent=1)
        print("collected", got["toolUses"], "tool uses; removed", removed)
        if a.ingest:
            for r in ingest(job, answer, SONNET, got["toolUses"]):
                print(r["article"], r["result"], r["level"], len(r["differences"]))
    elif a.cmd == "calibration":
        print(json.dumps(calibration(a.batch, set(filter(None, a.exclude.split(",")))), ensure_ascii=False, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
