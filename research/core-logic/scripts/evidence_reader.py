"""The owner's evidence tests 1 and 2 (19 November 2026): readers compared on the same dev notes, per line, per writer,
paired by note, with intervals.

    python evidence_reader.py --gold <notes.jsonl> --system name=<folder> ... --compare a:b ... --out <folder>

Each system folder holds `answers.jsonl` (end to end) and `given.jsonl` (type given). Every note is scored on its own by
the scorer itself (`score.exe`, one note at a time), so the lines mean exactly what the scorer means; a note's result is
(right, total) per line, an abstention counting as not right (total, not answered, in the denominator):

- claim type: in-catalogue notes, end to end;
- outside recognised: outside notes, end to end;
- polarity: the mentioned elements, type given;
- requests: the (claim, kind) pairs, type given;
- dates and amounts with role: recall, type given.

A rate's interval: the 2.5th and 97.5th percentiles of 4,000 bootstrap resamples of the notes (a note's items stay
together). A comparison a:b: the difference of the rates, on the same resampled notes, paired; "beats" when the whole
interval is above zero, "behind" when below, else "not yet settled", with the number of dev notes that would settle
it if the difference held (the interval narrows as one over the square root of the notes).
"""
from __future__ import annotations

import argparse
import json
import os
import random
import subprocess
import sys
import tempfile

sys.stdout.reconfigure(encoding="utf-8")
LINES = {
    "claim type": ("claim type", "in-catalogue notes", "end to end"),
    "outside": ("outside recognised", "outside notes", "end to end"),
    "polarity": ("polarity", "mentioned", "type given"),
    "requests": ("requests", "pairs", "type given"),
    "dates and amounts": ("dates and amounts", "recall", "type given"),
}
WRITERS = {"openai": "Codex", "anthropic": "Sonnet"}


def read_jsonl(path: str) -> dict:
    with open(path, encoding="utf-8") as f:
        return {x["id"]: x for x in (json.loads(line) for line in f if line.strip())}


def per_note(score: str, gold: dict, folder: str) -> dict:
    """{note id: {line: (right, total)}}, cached in the system's folder."""
    cache = os.path.join(folder, "per-note.json")
    if os.path.isfile(cache):
        with open(cache, encoding="utf-8") as f:
            return json.load(f)
    answers, given = read_jsonl(os.path.join(folder, "answers.jsonl")), read_jsonl(os.path.join(folder, "given.jsonl"))
    out = {}
    with tempfile.TemporaryDirectory() as tmp:
        for nid, note in gold.items():
            paths = {}
            for name, row in (("gold", note), ("answers", answers.get(nid)), ("given", given.get(nid))):
                paths[name] = os.path.join(tmp, name + ".jsonl")
                with open(paths[name], "w", encoding="utf-8", newline="\n") as f:
                    if row is not None:
                        f.write(json.dumps(row, ensure_ascii=False) + "\n")
            res = os.path.join(tmp, "out")
            subprocess.run([score, "--gold", paths["gold"], "--answers", paths["answers"], "--given", paths["given"],
                            "--out", res, "--purpose", "measure", "--no-by-writer"], check=True, capture_output=True)
            with open(os.path.join(res, "summary.json"), encoding="utf-8") as f:
                lines = json.load(f)["lines"]
            row = {}
            for key, (measure, line, setting) in LINES.items():
                hit = next((x for x in lines if x.get("measure") == measure and x.get("line") == line and x.get("setting") == setting), None)
                row[key] = (hit["right"], hit["total"]) if hit else (0, 0)
            out[nid] = row
    with open(cache, "w", encoding="utf-8") as f:
        json.dump(out, f)
    return out


def rate(notes: list, data: dict, line: str) -> float | None:
    right = sum(data[n][line][0] for n in notes)
    total = sum(data[n][line][1] for n in notes)
    return right / total if total else None


def boot(notes: list, fn, reps: int = 4000, seed: int = 20261119) -> tuple:
    rng = random.Random(seed)
    vals = []
    for _ in range(reps):
        sample = [rng.choice(notes) for _ in notes]
        v = fn(sample)
        if v is not None:
            vals.append(v)
    vals.sort()
    return (vals[int(0.025 * len(vals))], vals[int(0.975 * len(vals)) - 1]) if vals else (None, None)


def fmt(x: float | None) -> str:
    return "–" if x is None else f"{100 * x:.0f}"


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--gold", required=True)
    p.add_argument("--system", nargs="+", required=True)
    p.add_argument("--compare", nargs="*", default=[])
    p.add_argument("--score", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src",
                                                   "DzLegal.CoreLogic.Scoring", "bin", "Release", "net10.0", "score.exe"))
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    gold = read_jsonl(a.gold)
    writer = {nid: (n.get("provenance", {}).get("writer", {}).get("family", "")) for nid, n in gold.items()}
    systems = dict(s.split("=", 1) for s in a.system)
    data = {name: per_note(a.score, gold, folder) for name, folder in systems.items()}
    groups = {"all": list(gold)} | {label: [n for n in gold if writer[n] == fam] for fam, label in WRITERS.items()}
    md = [f"Notes: {len(gold)} ({', '.join(f'{k} {len(v)}' for k, v in groups.items() if k != 'all')}). Rates in %, with "
          "the 95 % bootstrap interval over notes; an abstention counts as not right.", "",
          "| System | Writer | " + " | ".join(LINES) + " |", "|---|---|" + "---|" * len(LINES)]
    results = {}
    for name in systems:
        for g, notes in groups.items():
            cells = []
            for line in LINES:
                r = rate(notes, data[name], line)
                lo, hi = boot(notes, lambda s, line=line, name=name: rate(s, data[name], line))
                n = sum(data[name][x][line][1] for x in notes)
                results[(name, g, line)] = (r, lo, hi, n)
                cells.append(f"{fmt(r)} ({fmt(lo)}–{fmt(hi)}; n {n})" if r is not None else "–")
            md.append(f"| {name} | {g} | " + " | ".join(cells) + " |")
    md += ["", "| Comparison | Writer | Line | Difference (points) | 95 % interval | Verdict |", "|---|---|---|---:|---|---|"]
    verdicts = {}
    for pair in a.compare:
        x, y = pair.split(":")
        for g, notes in groups.items():
            for line in LINES:
                def diff(s, line=line):
                    rx, ry = rate(s, data[x], line), rate(s, data[y], line)
                    return None if rx is None or ry is None else rx - ry
                d = diff(notes)
                if d is None:
                    continue
                lo, hi = boot(notes, diff)
                if lo is None:
                    continue
                if lo > 0:
                    verdict = f"{x} beats {y}"
                elif hi < 0:
                    verdict = f"{x} behind {y}"
                elif d == 0:
                    verdict = "not yet settled (no difference seen)"
                else:
                    half = (hi - lo) / 2
                    need = int(len(notes) * (half / abs(d)) ** 2) + 1
                    verdict = f"not yet settled (about {need} notes would settle it if the difference held)"
                verdicts[(pair, g, line)] = verdict
                md.append(f"| {x} vs {y} | {g} | {line} | {100 * d:+.0f} | {100 * lo:+.0f} to {100 * hi:+.0f} | {verdict} |")
    with open(os.path.join(a.out, "evidence.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(md) + "\n")
    print("\n".join(md))
    return 0


if __name__ == "__main__":
    sys.exit(main())
