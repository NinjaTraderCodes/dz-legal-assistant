"""The owner's composite route (18 November 2026: "Keep both routes and add the composite (each line from the route that
wins it)"). The composite takes each part of a reading (the claim block, the events, the requests, the values, the slots)
from the route whose score on that part is higher on the **tuning** notes (the Codex half of dev: the handbook's rule
that only those notes choose anything about the reader), never on the measured ones.

    python composite.py --tune-summaries ngrams=<folder> finetuned=<folder> --sources ngrams=<folder> finetuned=<folder>
                        --gold <notes.jsonl> --scorer <score.exe> --out <folder>

Each source folder holds `answers.jsonl` (end to end) and `given.jsonl` (claim types given) for the measured notes, and
each tune folder the scorer's `summary.json` for the tuning notes. The end-to-end claim block comes from the route that
wins the claim type; the other parts of both files come from the route that wins each part. A part read for claim types
other than the end-to-end answer's keeps working because each source's end-to-end reading already holds the parts
for its own claim answer: the combination route (n-grams' claim type, the fine-tuned rest) is a source like any other.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys

# The part of a reading each scorer line measures; the first line of each part decides it.
PARTS = {
    "claim": ("claim", [("claim type", "in-catalogue notes"), ("outside recognised", "outside notes")]),
    "events": ("events", [("polarity", "mentioned")]),
    "requests": ("requests", [("requests", "pairs")]),
    "values": ("values", [("dates and amounts", "recall")]),
}


def rate(summary: dict, measure: str, part: str) -> float | None:
    for line in summary.get("lines", []):
        if line.get("measure") == measure and line.get("line", line.get("part")) == part:
            n = line.get("answered") or line.get("n") or 0
            return (line.get("right", 0) / n) if n else None
    return None


def read_jsonl(path: str) -> dict:
    with open(path, encoding="utf-8") as f:
        return {x["id"]: x["reading"] for x in (json.loads(line) for line in f if line.strip())}


def choose(tune: dict[str, dict]) -> dict[str, str]:
    """Per part, the route with the higher tuning rate (the first listed route on a tie or a missing line)."""
    out = {}
    names = list(tune)
    for part, (_, lines) in PARTS.items():
        best, best_rate = names[0], -1.0
        for name in names:
            rates = [r for m, p in lines if (r := rate(tune[name], m, p)) is not None]
            score = sum(rates) / len(rates) if rates else -1.0
            if score > best_rate:
                best, best_rate = name, score
        out[part] = best
    return out


def compose(choice: dict[str, str], sources: dict[str, dict], kind: str) -> dict:
    """One file (answers or given): for each note, each part from its chosen source; slots go with the values (the
    date slots are value roles)."""
    base = sources[choice["claim"]][kind] if kind == "answers" else sources[choice["events"]][kind]
    out = {}
    for note_id, reading in base.items():
        r = dict(reading)
        for part, (key, _) in PARTS.items():
            if part == "claim" and kind == "given":
                continue
            src = sources[choice[part]][kind].get(note_id)
            if src is not None and key in src:
                r[key] = src[key]
        src = sources[choice["values"]][kind].get(note_id)
        if src is not None and "slots" in src:
            r["slots"] = src["slots"]
        r["info"] = {"name": "dz-reader-composite", "version": "+".join(f"{p}={s}" for p, s in choice.items()), "files": {}}
        out[note_id] = r
    return out


def pairs(values: list[str]) -> dict[str, str]:
    return dict(v.split("=", 1) for v in values)


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--tune-summaries", nargs="+", required=True)
    p.add_argument("--sources", nargs="+", required=True)
    for name in ("gold", "scorer", "out"):
        p.add_argument(f"--{name}", required=True)
    a = p.parse_args(argv)
    tune = {}
    for name, folder in pairs(a.tune_summaries).items():
        with open(os.path.join(folder, "summary.json"), encoding="utf-8") as f:
            tune[name] = json.load(f)
    sources = {name: {"answers": read_jsonl(os.path.join(folder, "answers.jsonl")),
                      "given": read_jsonl(os.path.join(folder, "given.jsonl"))}
               for name, folder in pairs(a.sources).items()}
    choice = choose({n: tune[n] for n in sources if n in tune})
    os.makedirs(a.out, exist_ok=True)
    for kind in ("answers", "given"):
        with open(os.path.join(a.out, kind + ".jsonl"), "w", encoding="utf-8", newline="\n") as f:
            for note_id, reading in compose(choice, sources, kind).items():
                f.write(json.dumps({"id": note_id, "reading": reading}, ensure_ascii=False) + "\n")
    with open(os.path.join(a.out, "choice.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"choice": choice, "rule": "per part, the higher rate on the tuning notes (Codex half of dev)"}, f, indent=1)
    subprocess.run([a.scorer, "--gold", a.gold, "--answers", os.path.join(a.out, "answers.jsonl"),
                    "--given", os.path.join(a.out, "given.jsonl"), "--out", a.out, "--purpose", "measure"],
                   check=True, capture_output=True)
    print(f"composite {choice} -> {a.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
