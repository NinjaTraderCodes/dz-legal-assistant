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


def compare(batch: str, job_name: str) -> list[dict]:
    job = rd.load(rd.job_path(batch, job_name))
    folder = os.path.join(rd.JOBS, batch)
    s = rd.load(os.path.join(folder, f"{job_name}.sonnet.json"))
    c = rd.load(os.path.join(folder, f"{job_name}.codex.json"))
    # A reader may echo the heading («المادة 324 مكرر 3» for «324 مكرر 3»): the key is the number alone.
    by_s = {re.sub(r"^المادة\s+", "", x["article"]).strip(): x for x in s["articles"]}
    by_c = {re.sub(r"^المادة\s+", "", x["article"]).strip(): x for x in c["articles"]}
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


if __name__ == "__main__":
    sys.exit(main())
