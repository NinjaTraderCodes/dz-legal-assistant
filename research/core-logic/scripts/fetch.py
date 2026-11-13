"""Missing articles, read from the Official Journal by two families (law/README.md, "Reading an article").

The page plan (which pages of which issue hold which missing articles) is a recorded decision: from the corpus's anchors,
from the French edition's text layer, or from a look at low-resolution renders. Each entry becomes a job of kind
"fetch", read by Sonnet 5.5 and by Codex. Their two readings are compared with each other at level 2 (or 2o, 2s); a
difference goes to the settler. The store is not involved: these articles are not in it.

    python fetch.py jobs <plan.json>
    python fetch.py compare <batch> <job>
"""
from __future__ import annotations

import json
import re
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import consolidated  # noqa: E402
import reading as rd  # noqa: E402
import textcmp  # noqa: E402

AGREE = ("1", "2", "2o", "2s")


def make_jobs(plan_path: str) -> list[dict]:
    """plan: {"batch": ..., "jobs": [{"job", "code", "version", "pdf", "url", "pages", "articles", "critical",
    "rereadArticles"?}]}. `rereadArticles` are articles the store already holds that the same images also show
    (labour articles re-read by Codex): the same answer is compared with the store for them."""
    with open(plan_path, encoding="utf-8") as f:
        plan = json.load(f)
    made = []
    for j in plan["jobs"]:
        arts = j["articles"] + [a for a in j.get("rereadArticles", []) if a not in j["articles"]]
        doc = rd.make_job(plan["batch"], j["job"], j["code"], j["version"], j["pdf"], j["pages"], arts, "fetch",
                          "missing", source_url=j.get("url"), critical=j.get("critical", True), dpi=j.get("dpi", 300))
        doc["rereadArticles"] = j.get("rereadArticles", [])
        doc["rereadWhy"] = j.get("rereadWhy")
        doc["planNote"] = j.get("note")
        with open(rd.job_path(plan["batch"], j["job"]), "w", encoding="utf-8", newline="\n") as f:
            json.dump(doc, f, ensure_ascii=False, indent=1)
        made.append(doc)
    for j in plan.get("codexRereads", []):  # re-readings only, read by Codex alone (the labour laws)
        made.append(rd.make_job(plan["batch"], j["job"], j["code"], j["version"], j["pdf"], j["pages"], j["articles"],
                                "reread", "labour", source_url=j.get("url")))
    return made


def reread_ingest(batch: str, job_name: str, family: str) -> list[dict]:
    """The articles of a fetch job that the store already holds: the same answer, compared with the store and
    recorded in law/readings.jsonl, as a re-reading for the job's `rereadWhy`."""
    job = rd.load(rd.job_path(batch, job_name))
    if not job.get("rereadArticles"):
        return []
    sub = dict(job, kind="reread", why=job["rereadWhy"], articles=job["rereadArticles"], job=job_name)
    answer = rd.load(os.path.join(rd.JOBS, batch, f"{job_name}.{'codex' if family == 'codex' else 'sonnet'}.json"))
    reader = rd.SONNET if family != "codex" else f"{rd.codex_run.version()} (default model)"
    return rd.ingest(sub, answer, reader, None, codex_log=f"{job_name}.jsonl" if family == "codex" else None)


def compare_pair(a: dict, b: dict) -> dict:
    """Two readings of one article (first: Sonnet, second: Codex)."""
    if not (a["found"] and b["found"]):
        return {"result": "notFoundByBoth" if not (a["found"] or b["found"]) else "foundByOne", "level": None,
                "differences": []}
    for r in (a, b):
        t = "\n".join(r["paragraphs"])
        if rd.UNREADABLE in t:  # notes without a [؟] (punctuation doubts) do not void a reading
            return {"result": "unreadableMark", "level": None, "differences": []}
    ta, tb = "\n".join(a["paragraphs"]), "\n".join(b["paragraphs"])
    lv = textcmp.level(ta, tb)
    if lv in AGREE:
        return {"result": "twoAgree", "level": lv, "differences": []}
    diffs = []
    if lv == "3":
        diffs.append({"kind": "paragraphBreak", "first": " | ".join(a["paragraphs"]), "second": " | ".join(b["paragraphs"])})
    else:
        for d in textcmp.differences(ta, tb):
            diffs.append({"kind": "words", "first": d["a"], "second": d["b"], "before": d["before"], "after": d["after"]})
    return {"result": "differ", "level": lv, "differences": diffs}


FILLS = os.path.join(rd.JOBS, "fills.jsonl")


def fills() -> list[dict]:
    """Marked gaps settled from another source on the owner's instruction (a blotted digit read in the official
    consolidated edition, tier 2): applied to the readings before comparison, and named in the delta's note."""
    if not os.path.exists(FILLS):
        return []
    with open(FILLS, encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]


def apply_fills(job: dict, reading: dict) -> tuple[dict, list[dict]]:
    used = []
    paras = list(reading["paragraphs"])
    for fl in fills():
        if (fl["code"], fl["article"], fl["version"]) != (job["code"], reading["article"], job["version"]):
            continue
        for i, p in enumerate(paras):
            if fl["marked"] in p:
                paras[i] = p.replace(fl["marked"], fl["filled"])
                used.append(fl)
    if not used:
        return reading, []
    return dict(reading, paragraphs=paras, unreadable=[]), used


def article_key(answer: dict) -> str:
    """A reader may echo the heading («المادة 324 مكرر 3» for «324 مكرر 3»): the key is the number alone."""
    return re.sub(r"^المادة\s+", "", answer["article"]).strip()


def first_reading_job(batch: str, job_name: str, job: dict) -> tuple[str, dict]:
    """A Codex-only re-read at a higher resolution (`firstReadingFrom`) is paired with the Sonnet reading of the same
    pages at 300 dpi: that job's answer, with its own images, is the first reading."""
    first = job.get("firstReadingFrom")
    return (first, rd.load(rd.job_path(batch, first))) if first else (job_name, job)


def first_reading_path(batch: str, job_name: str, job: dict) -> str:
    return os.path.join(rd.JOBS, batch, f"{first_reading_job(batch, job_name, job)[0]}.sonnet.json")


def compare(batch: str, job_name: str) -> list[dict]:
    job = rd.load(rd.job_path(batch, job_name))
    folder = os.path.join(rd.JOBS, batch)
    s = rd.load(first_reading_path(batch, job_name, job))
    c = rd.load(os.path.join(folder, f"{job_name}.codex.json"))
    by_s = {article_key(x): x for x in s["articles"]}
    by_c = {article_key(x): x for x in c["articles"]}
    rows = []
    for art in job["articles"]:
        if art in job.get("rereadArticles", []):
            continue
        if art not in by_s or art not in by_c:
            rows.append({"article": art, "result": "notAnswered", "level": None, "differences": []})
            continue
        s_read, s_fills = apply_fills(job, by_s[art])
        c_read, c_fills = apply_fills(job, by_c[art])
        row = {"article": art, **compare_pair(s_read, c_read),
               "where": by_s[art]["where"], "continues": [by_s[art]["continuesOnNextPage"], by_c[art]["continuesOnNextPage"]]}
        if s_fills or c_fills:
            row["filled"] = {"first": [f["filled"] for f in s_fills], "second": [f["filled"] for f in c_fills],
                             "settledBy": (s_fills or c_fills)[0]["settledBy"]}
        if row["result"] == "differ":
            apply_settlements(f"{job['batch']}/{job_name}", row, s_read)
        rows.append(row)
    with open(os.path.join(folder, f"{job_name}.fetch.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(rows, f, ensure_ascii=False, indent=1)
    return rows


def main(argv: list[str] | None = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[0] == "jobs":
        for d in make_jobs(argv[1]):
            print(d["job"], d["pages"], ",".join(d["articles"]))
    elif argv[0] == "compare":
        for r in compare(argv[1], argv[2]):
            print(r["article"], r["result"], r["level"], [(d.get("first", "")[:40], d.get("second", "")[:40]) for d in r["differences"]])
    return 0



# --- settling the differences between the two readings of a missing article -------------------------------------
SETTLEMENTS = os.path.join(rd.JOBS, "fetch-settlements.jsonl")


def orthographic(a: str, b: str) -> bool:
    return textcmp.level(a or "", b or "") in AGREE


def settle_prep(name: str, jobs: list[tuple[str, str]], dpi: int = 600, only: dict | None = None,
                recheck: bool = False) -> dict:
    """Every non-orthographic difference of the given (batch, job) comparisons goes to one settle run: the two words as X
    and Y in random order (which reader wrote which is not shown), both columns of every page at 600 dpi."""
    entries = []
    for batch, job_name in jobs:
        job = rd.load(rd.job_path(batch, job_name))
        _, first_job = first_reading_job(batch, job_name, job)
        for r in rd.load(os.path.join(rd.JOBS, batch, f"{job_name}.fetch.json")):
            if r["result"] not in (("differ", "settled") if recheck else ("differ",)):
                continue
            if only is not None and r["article"] not in only.get(job_name, []):
                continue  # a second look covers only the articles still open
            for d in r["differences"]:
                if d["kind"] == "words" and orthographic(d["first"], d["second"]):
                    continue
                if not recheck and settled_words(f"{batch}/{job_name}", r["article"], d) is not None:
                    continue  # already settled, sure
                close = rd.tiles(first_job, dict(r["where"], continues=any(r.get("continues") or [])), dpi=dpi)
                entries.append({"job": first_job, "tiles": close, "fetchJob": f"{batch}/{job_name}",
                                "article": r["article"], "paragraph": None,
                                "kind": d["kind"], "store": d["first"], "reading": d["second"],
                                "before": d.get("before", ""), "after": d.get("after", "")})
    items, key = rd.settle_items(entries, name)
    for k, e in zip(key, entries):  # in this path "store" means the first reading and "reading" the second
        k.update(job=e["fetchJob"], first=k.pop("storeWords"), second=k.pop("readingWords"))
        k["X"] = {"store": "first", "reading": "second"}[k["X"]]
        k["Y"] = {"store": "first", "reading": "second"}[k["Y"]]
    out = os.path.join(rd.JOBS, f"settle-{name}")
    os.makedirs(out, exist_ok=True)
    prompt_path = os.path.join(out, "prompt.txt")
    with open(prompt_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(rd.settle_prompt(items))
    images = rd.settle_images(entries) + [prompt_path]
    with open(os.path.join(out, "key.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"key": key, "images": images}, f, ensure_ascii=False, indent=1)
    return {"items": len(entries), "images": len(images), "folder": out}


def settle_record(name: str, agent_id: str, settler: str = "claude-opus-5-5") -> list[dict]:
    folder = os.path.join(rd.JOBS, f"settle-{name}")
    key = rd.load(os.path.join(folder, "key.json"))
    got = rd.collect({"images": []}, agent_id, allowed_images=key["images"])
    if got["problems"]:
        raise ValueError(f"discard the settle run: {got['problems']}")
    with open(os.path.join(folder, "answer.raw.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(got["answerText"])
    answer, _ = rd.first_json(got["answerText"])
    out = []
    with open(SETTLEMENTS, "a", encoding="utf-8", newline="\n") as f:
        for k, a in zip(key["key"], answer):
            printed = a.get("printed", a.get("decision", a.get("choice")))
            side = k[printed] if printed in ("X", "Y") else "neither"
            row = {"job": k["job"], "article": k["article"], "first": k["first"], "second": k["second"], "printed": side,
                   "words": a.get("words", ""), "sure": bool(a.get("sure")), "seen": a.get("seen", ""), "by": settler,
                   "run": f"settle-{name}", "agent": agent_id}
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
            out.append(row)
    return out


def settled_words(job_key: str, article: str, d: dict) -> str | None:
    """The words the page prints for one difference, if a sure settlement exists (the latest run wins)."""
    if not os.path.exists(SETTLEMENTS):
        return None
    with open(SETTLEMENTS, encoding="utf-8") as f:
        rows = [json.loads(line) for line in f if line.strip()]
    for s in reversed(rows):
        if s["job"] == job_key and s["article"] == article and s["first"] == d["first"] and s["second"] == d["second"]:
            if not s["sure"]:
                return None
            return {"first": d["first"], "second": d["second"]}.get(s["printed"], s["words"])
    return None


def apply_settlements(job_key: str, row: dict, first: dict) -> None:
    """Every real difference settled, sure -> the article is settled by a third reading; its text is the first reading
    with each difference replaced by what the page prints. Orthographic differences keep the first reading's spelling."""
    paras = list(first["paragraphs"])
    for d in row["differences"]:
        if d["kind"] == "words" and orthographic(d["first"], d["second"]):
            continue
        words = settled_words(job_key, row["article"], d)
        if words is None:
            return
        if d["kind"] != "words":
            return  # a paragraph-division difference is settled by hand, not by replacement
        if d["first"]:
            hit = next((i for i, p in enumerate(paras) if d["first"] in p), None)
            if hit is None:
                return
            paras[hit] = paras[hit].replace(d["first"], words, 1)
        else:  # the first reading lacks words the page prints: insert after the context
            ctx = (d.get("before") or "").split()[-3:]
            anchor = " ".join(ctx)
            hit = next((i for i, p in enumerate(paras) if anchor and anchor in p), None)
            if hit is None:
                return
            paras[hit] = paras[hit].replace(anchor, f"{anchor} {words}", 1)
        d["settled"] = words
    row["result"] = "settled"
    row["settledParagraphs"] = paras



# --- the consolidated-PDF differences (law/README.md, "Reading an article", step 7) --------------------------------
# Each word where the 2007 consolidated edition differs from the agreed reading goes to the settler with the Journal
# page, which decides; the consolidated edition is tier 2 and never overrules the Journal page by itself.
CONSOLIDATED_SETTLEMENTS = os.path.join(rd.JOBS, "consolidated-settlements.jsonl")


def job_of_entry(e: dict) -> tuple[str, str]:
    """The fetch job whose readings made a delta entry: the Codex reading's event log is named after the job."""
    job_name = e["proof"]["readings"][1]["codexLog"].removesuffix(".jsonl")
    for batch in sorted(os.listdir(rd.JOBS)):
        if batch.startswith("fetch-") and os.path.exists(rd.job_path(batch, job_name)):
            return batch, job_name
    raise FileNotFoundError(job_name)


def consolidated_settled(code: str, version: str, article: str, d: dict) -> dict | None:
    """The latest settlement of one consolidated difference, if any."""
    if not os.path.exists(CONSOLIDATED_SETTLEMENTS):
        return None
    with open(CONSOLIDATED_SETTLEMENTS, encoding="utf-8") as f:
        rows = [json.loads(line) for line in f if line.strip()]
    for s in reversed(rows):
        if (s["code"], s["version"], s["article"], s["ours"], s["consolidated"]) == (code, version, article, d["ours"],
                                                                                     d["consolidated"]):
            return s
    return None


def page_holds(code: str, versions: tuple[str, ...], article: str, ours: str, other: str) -> str | None:
    """The run of the latest settlement between the two readers that is sure for `ours` against words equal to `other`
    at level 2, if that settlement is the latest for its difference: the Journal page was looked at for these words."""
    if not os.path.exists(SETTLEMENTS):
        return None
    with open(SETTLEMENTS, encoding="utf-8") as f:
        rows = [json.loads(line) for line in f if line.strip()]
    latest = {}
    for s in rows:
        latest[(s["job"], s["article"], s["first"], s["second"])] = s
    for (job_key, art, first, second), s in latest.items():
        if art != article or not s["sure"]:
            continue
        job = rd.load(rd.job_path(*job_key.split("/", 1)))
        if job["code"] != code or job["version"] not in versions:
            continue
        printed = {"first": first, "second": second}.get(s["printed"], s["words"])
        alternative = second if printed == first else first
        if textcmp.level(printed, ours) in ("1", "2") and textcmp.level(alternative, other) in ("1", "2", "2s"):
            return s["run"]
    return None


def consolidated_prep(name: str, delta_path: str, dpi: int = 600, recheck: bool = False) -> dict:
    with open(delta_path, encoding="utf-8") as f:
        delta = json.load(f)
    entries = []
    for e in delta["articles"]:
        cp = e["proof"].get("consolidatedPdf")
        if not cp or cp["status"] != "differs":
            continue
        batch, job_name = job_of_entry(e)
        job = rd.load(rd.job_path(batch, job_name))
        _, first_job = first_reading_job(batch, job_name, job)
        a = next(x for x in rd.load(first_reading_path(batch, job_name, job))["articles"] if article_key(x) == e["number"])
        close = rd.tiles(first_job, dict(a["where"], continues=a.get("continuesOnNextPage")), dpi=dpi)
        for d in consolidated.word_differences(e["code"], e["text"]):
            if d["orthographic"]:
                continue
            s = consolidated_settled(e["code"], e["version"], e["number"], d)
            if s is not None and s["sure"] and not recheck:
                continue
            entries.append({"job": first_job, "tiles": close, "fetchJob": f"{batch}/{job_name}", "article": e["number"],
                            "paragraph": None, "kind": "words",
                            "store": d["ours"], "reading": d["consolidated"], "before": d["before"], "after": d["after"]})
    items, key = rd.settle_items(entries, name)
    for k, e in zip(key, entries):  # here "store" is the agreed reading and "reading" the consolidated edition's
        k.update(job=e["fetchJob"], ours=k.pop("storeWords"),
                 consolidated=k.pop("readingWords"))
        k["X"] = {"store": "ours", "reading": "consolidated"}[k["X"]]
        k["Y"] = {"store": "ours", "reading": "consolidated"}[k["Y"]]
    out = os.path.join(rd.JOBS, f"settle-{name}")
    os.makedirs(out, exist_ok=True)
    prompt = os.path.join(out, "prompt.txt")
    with open(prompt, "w", encoding="utf-8", newline="\n") as f:
        f.write(rd.settle_prompt(items))
    images = rd.settle_images(entries)
    with open(os.path.join(out, "key.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"key": key, "images": images + [prompt]}, f, ensure_ascii=False, indent=1)
    return {"items": len(entries), "images": len(images), "folder": out}


def consolidated_record(name: str, agent_id: str, settler: str = "claude-opus-5-5") -> list[dict]:
    folder = os.path.join(rd.JOBS, f"settle-{name}")
    key = rd.load(os.path.join(folder, "key.json"))
    got = rd.collect({"images": []}, agent_id, allowed_images=key["images"])
    if got["problems"]:
        raise ValueError(f"discard the settle run: {got['problems']}")
    with open(os.path.join(folder, "answer.raw.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(got["answerText"])
    answer, _ = rd.first_json(got["answerText"])
    if len(answer) != len(key["key"]):
        raise ValueError(f"discard the settle run: {len(answer)} answers for {len(key['key'])} differences")
    out = []
    with open(CONSOLIDATED_SETTLEMENTS, "a", encoding="utf-8", newline="\n") as f:
        for k, a in zip(key["key"], answer):
            printed = a.get("printed", a.get("decision", a.get("choice")))
            row = {"code": k["code"], "version": k["version"], "article": k["article"], "job": k["job"],
                   "ours": k["ours"], "consolidated": k["consolidated"],
                   "printed": k[printed] if printed in ("X", "Y") else "neither", "words": a.get("words", ""),
                   "sure": bool(a.get("sure")), "seen": a.get("seen", ""), "by": settler, "run": f"settle-{name}",
                   "agent": agent_id}
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
            out.append(row)
    return out


if __name__ == "__main__":
    sys.exit(main())
