#!/usr/bin/env python3
"""Acquire official discovery sources; first acquisition is pinned, never silently refreshed."""
from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor
from datetime import date
import shutil
import urllib.request

import pipeline

SOURCES = [
    ("A1984024", "1984", "arabe"), ("F1984024", "1984", "francais"),
    ("A2005015", "2005", "arabe"), ("F2005015", "2005", "francais"),
    ("A1990006", "1990", "arabe"), ("F1990006", "1990", "francais"),
    ("A1990017", "1990", "arabe"), ("F1990017", "1990", "francais"),
    ("F1991068", "1991", "francais"),
]
PRIOR = pipeline.ROOT.parent.parent / "legal-research/downloads/joradp"
REGISTRY = pipeline.ROOT / "data/law/corpus/research/scope-source-downloads.json"


def acquire(request):
    key, url = request
    target = pipeline.CACHE / (key + ".pdf")
    try:
        method = "existing-task-cache"
        if not target.exists():
            prior = PRIOR / target.name
            if prior.exists():
                shutil.copyfile(prior, target)
                method = "existing-official-research-cache; acquisition date not re-established"
            else:
                temporary = target.with_suffix(".pdf.partial")
                opener = urllib.request.build_opener(pipeline.OfficialRedirect())
                try:
                    req = urllib.request.Request(pipeline.official_url(url), headers={"User-Agent": "DZLegal-LawCorpus/1.0"})
                    with opener.open(req, timeout=45) as response, temporary.open("wb") as stream:
                        pipeline.official_url(response.url)
                        length = 0
                        while block := response.read(1024 * 1024):
                            length += len(block)
                            if length > 150 * 1024 * 1024:
                                raise ValueError("PDF exceeds 150 MiB")
                            stream.write(block)
                    if not temporary.read_bytes()[:1024].lstrip().startswith(b"%PDF-"):
                        raise ValueError("Response is not a PDF")
                    temporary.replace(target)
                finally:
                    temporary.unlink(missing_ok=True)
                method = "official-https-first-acquisition"
        result = {"id": key, "url": url, "sha256": pipeline.hash_file(target),
                  "retrievedOn": date.today().isoformat(), "method": method}
        print(key + ": " + method, flush=True)
        return result
    except Exception as error:
        print(key + ": FAILED " + str(error), flush=True)
        return {"id": key, "url": url, "attemptedOn": date.today().isoformat(), "error": str(error)}


def main():
    pipeline.CACHE.mkdir(parents=True, exist_ok=True)
    requests = [(key, f"https://www.joradp.dz/FTP/jo-{language}/{year}/{key}.pdf")
                for key, year, language in SOURCES]
    requests.append(("commercial-official-2007", "https://droit.mjustice.gov.dz/sites/default/files/Code_de_Commerce_2007.pdf"))
    requests.append(("civil-official-2007", "https://droit.mjustice.gov.dz/sites/default/files/Code_civil_2007.pdf"))
    prior = {s["id"]: s for s in pipeline.read_json(REGISTRY)} if REGISTRY.exists() else {}
    known = {s["id"]: s for s in pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/source-downloads.json")}
    results = []
    pending = []
    for key, url in requests:
        pinned = prior.get(key) or known.get(key)
        if pinned and "sha256" in pinned:
            pipeline.fetch_source(pinned, pipeline.CACHE, False, PRIOR)
            results.append(pinned)
        else:
            pending.append((key, url))
    with ThreadPoolExecutor(max_workers=3) as pool:
        results.extend(pool.map(acquire, pending))
    pipeline.write_json(REGISTRY, sorted(results, key=lambda s: s["id"]))


if __name__ == "__main__":
    main()
