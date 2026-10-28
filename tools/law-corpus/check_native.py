#!/usr/bin/env python3
"""Compare a code's cleaned native official text with two independent OCR readings of the official printed pages.

Input articles (JSON list, reading order): {"number", "label", "text"} where label is the printed article label used
only to align («المادة 504»). OCR streams come from pipeline evidence of a column manifest (make_column_manifest.py):
Tesseract TSV (lines re-ordered top to bottom) and the PP-OCRv5 supplementary stream bound to the same crop hash.
Rule: native_check.compare_streams (tested in test_native_check.py).

Usage: check_native.py ARTICLES.json MANIFEST.json SOURCE_ID --out RESULT.json
"""
from __future__ import annotations

import argparse
from collections import defaultdict

import re

import native_check as nc
import pipeline

LEADERS = re.compile(r"\.{3,}")
SLACK = 3  # tokens past an article's expected end: a closing quotation mark or OCR junk before its final full stop
HEADER_PARTS = [nc.fold(w) for w in ("الجريدة", "الرسمي", "للجمهوري", "الجزائري", "العدد", "ربيع", "أبريل", "يوليو", "الحجة",
                                     "1429", "1443", "2008", "2022")]
STRUCTURAL = {nc.fold(w) for w in ("المادة", "الكتاب", "الباب", "الفصل", "القسم", "الفرع", "في", "مكرر", "الأول", "الثاني",
                                   "الثالث", "الرابع", "الخامس", "السادس", "السابع", "الثامن", "التاسع", "العاشر", ":")}


def evidence_folders(manifest: dict, source_id: str, partial: bool = False) -> list[tuple[str, pipeline.Path]]:
    extracts = sorted((e for e in manifest["extracts"] if e["sourceId"] == source_id),
                      key=lambda e: (e["pdfPage"], 0 if e["id"].endswith("right") else 1))
    found = pipeline.evidence_by_id(pipeline.CACHE, {e["id"] for e in extracts})
    out = []
    for e in extracts:
        if e["id"] not in found:
            if partial:
                break
            raise FileNotFoundError("missing evidence " + e["id"])
        out.append((e["id"], found[e["id"]][1]))
    return out


def tesseract_stream(folders) -> list[dict]:
    stream = []
    for ident, folder in folders:
        stream += nc.ocr_tokens_from_tsv((folder / "ocr.tsv").read_text(encoding="utf-8"), source=ident)
    return stream


def paddle_stream(folders) -> list[dict]:
    stream = []
    for ident, folder in folders:
        crop_sha = pipeline.hash_file(folder / "crop.png")
        records = [pipeline.read_json(p) for p in (pipeline.CACHE.parent / "supplementary-ocr" / ident).glob("*/record.json")]
        records = [r for r in records if r.get("imageSha256") == crop_sha and r.get("status") == "extracted"]
        if not records:
            continue
        record = max(records, key=lambda r: r["recordedAt"])
        for line_no, line in enumerate(record["ocr"].split("\n")):
            stream += [{"token": t, "source": ident, "line": line_no} for t in nc.tokens(line)]
    return stream


def without_leaders(stream: list[dict]) -> list[dict]:
    """Drop dot leaders (runs of 3+ full-stop tokens, as in «....(الباقي بدون تغيير)....»): layout, not text."""
    out, run = [], []
    for tok in stream + [{"token": ""}]:
        if tok["token"] == ".":
            run.append(tok)
            continue
        if len(run) < 3:
            out += run
        run = []
        if tok["token"]:
            out.append(tok)
    return out


def without_headers(stream: list[dict], top_lines: int = 5) -> list[dict]:
    """Drop the running page header (JO title, issue number, Hijri and Gregorian dates) from the first lines of each
    crop. OCR often merges its words («مزايريهالعدد»), so header words are matched inside tokens: a top line is a
    header line with two or more header words, or one header word in a line of at most three tokens. Dropping can
    only create disagreements in article text, never hide one, because header lines are not article text."""
    seen: dict[str, list] = {}
    for tok in stream:
        lines = seen.setdefault(tok["source"], [])
        if tok["line"] not in lines:
            lines.append(tok["line"])
    header_lines = set()
    for source, lines in seen.items():
        for line in lines[:top_lines]:
            words = [nc.fold(t["token"]) for t in stream if t["source"] == source and t["line"] == line]
            hits = sum(any(h in w for w in words) for h in HEADER_PARTS)
            if hits >= 2 or (hits == 1 and len(words) <= 3):
                header_lines.add((source, line))
    return [t for t in stream if (t["source"], t["line"]) not in header_lines]


def structural_tail(tokens: list[str], headings: set[str] = frozenset()) -> bool:
    """True when extra tokens found past an article's end are only the next label or a heading: label and heading
    words, numbers, or words of the headings that open before the next article (`headings`, folded)."""
    return all(nc.fold(t) in STRUCTURAL or nc.fold(t) in headings or nc.exact_form(t).isdigit() for t in tokens)


def opening_headings(article: dict, following: dict | None) -> set[str]:
    """Folded words of the headings the next article opens (those in its path but not in this article's)."""
    if not following:
        return set()
    new = [h for h in following.get("path", []) if h not in article.get("path", [])]
    return {nc.fold(t) for h in new for t in nc.tokens(h)}


def trim_at_next_label(span: list[str], label_length: int, next_number: str | None) -> list[str]:
    """Cut a located span where the next article's printed label begins («المادة» followed within four tokens by «:»
    or by the next article's number), so the next label is not counted as extra text of this article."""
    label = nc.fold("المادة")
    following = nc.tokens(next_number) if next_number else []
    for k in range(label_length + 1, len(span)):
        if nc.fold(span[k].lstrip("”“\"")) != label:
            continue
        ahead = span[k + 1:k + 5]
        if ":" in ahead or (following and [nc.exact_form(t) for t in ahead[:len(following)]] == following):
            start = k - 1 if k > 0 and span[k - 1] in ("”", "“", '"') else k
            return span[:start]
    return span


def comparable(text: str) -> str:
    """The same rule on the native side: dot leaders are removed before comparison."""
    return LEADERS.sub(" ", text)


def global_spans(articles: list[list[str]], stream: list[str], n: int = 4) -> list[tuple[int, int] | None]:
    """Order-preserving location of every article in one OCR stream at once.

    Anchors are n-grams (folded tokens) that occur exactly once in the whole native text and once in the stream; the
    longest chain of anchors increasing on both sides is kept (so a phrase repeated elsewhere, or one wrong match,
    cannot pull an article out of order). An article's span runs from its first anchor, moved back by the anchor's
    offset into the article, to its last anchor, moved forward by the rest of the article, clamped between the
    neighbouring articles' anchors. An article with no anchor gets the gap between its neighbours when that gap is
    plausible for its length; otherwise it is not located."""
    folded = [nc.fold(t) for t in stream]
    starts, flat = [], []
    for tokens in articles:
        starts.append(len(flat))
        flat += [nc.fold(t) for t in tokens]
    ends = starts[1:] + [len(flat)]
    owner = [k for k, tokens in enumerate(articles) for _ in tokens]

    def unique_grams(seq):
        seen: dict[tuple, int] = {}
        for i in range(len(seq) - n + 1):
            gram = tuple(seq[i:i + n])
            seen[gram] = -1 if gram in seen else i
        return {g: i for g, i in seen.items() if i >= 0}

    native, ocr = unique_grams(flat), unique_grams(folded)
    pairs = sorted((native[g], ocr[g]) for g in native.keys() & ocr.keys() if owner[native[g]] == owner[native[g] + n - 1])
    # Longest chain increasing in both coordinates (patience sorting on the stream position).
    tails, previous, links = [], [], [-1] * len(pairs)
    for k, (_, b) in enumerate(pairs):
        lo, hi = 0, len(tails)
        while lo < hi:
            mid = (lo + hi) // 2
            if pairs[tails[mid]][1] < b:
                lo = mid + 1
            else:
                hi = mid
        links[k] = tails[lo - 1] if lo else -1
        if lo == len(tails):
            tails.append(k)
        else:
            tails[lo] = k
    chain, k = [], tails[-1] if tails else -1
    while k >= 0:
        chain.append(pairs[k])
        k = links[k]
    chain.reverse()
    anchors: dict[int, list[tuple[int, int]]] = {}
    for a, b in chain:
        anchors.setdefault(owner[a], []).append((a, b))
    first = {k: v[0][1] for k, v in anchors.items()}
    last = {k: v[-1][1] + n for k, v in anchors.items()}
    out: list[tuple[int, int] | None] = []
    for k in range(len(articles)):
        floor = next((last[j] for j in range(k - 1, -1, -1) if j in last), 0)
        ceiling = next((first[j] for j in range(k + 1, len(articles)) if j in first), len(stream))
        if k in anchors:
            (a0, b0), (a1, b1) = anchors[k][0], anchors[k][-1]
            begin = max(floor, b0 - (a0 - starts[k]))
            end = min(ceiling, b1 + n + (ends[k] - (a1 + n)) + SLACK)
            out.append((begin, max(begin, end)))
        elif 0 < ceiling - floor <= 3 * (ends[k] - starts[k]) + 50:
            out.append((floor, ceiling))
        else:
            out.append(None)
    return out


def search_window(canonical_tokens: list[str]) -> int:
    """How far past the cursor an article may start: proportional to its length, so one wrong anchor cannot skip
    the rest of the issue."""
    return 3 * len(canonical_tokens) + 600


def span_boxes(stream: list[dict], begin: int, end: int) -> list[dict]:
    """Union box of the matched OCR lines per evidence crop (pixel coordinates of that crop)."""
    boxes: dict[str, list[int]] = {}
    for tok in stream[begin:end]:
        b = tok.get("box")
        if not b:
            continue
        cur = boxes.setdefault(tok["source"], list(b))
        boxes[tok["source"]] = [min(cur[0], b[0]), min(cur[1], b[1]), max(cur[2], b[2]), max(cur[3], b[3])]
    return [{"evidenceId": k, "box": v} for k, v in boxes.items()]


def run(articles: list[dict], manifest_path: pipeline.Path, source_id: str, partial: bool = False) -> dict:
    """partial: stop at the first crop without evidence (calibration runs only; never for a delivery)."""
    manifest = pipeline.load_manifest(manifest_path)
    folders = evidence_folders(manifest, source_id, partial)
    streams = {"tesseract": without_leaders(without_headers(tesseract_stream(folders))),
               "ppocr": without_leaders(without_headers(paddle_stream(folders)))}
    tokens = {n: [t["token"] for t in s] for n, s in streams.items()}
    canonicals = [nc.tokens(a["label"] + " : " + comparable(a["text"])) for a in articles]
    placed = {n: global_spans(canonicals, tokens[n]) if tokens[n] else [None] * len(articles) for n in streams}
    results = []
    for position, article in enumerate(articles):
        next_number = articles[position + 1]["number"] if position + 1 < len(articles) else None
        label = nc.tokens(article["label"])
        full = article["label"] + " : " + comparable(article["text"])
        canonical = nc.tokens(full)
        spans, located = {}, {}
        for name in streams:
            if not tokens[name]:
                continue
            found = placed[name][position]
            if found and found[1] > found[0]:
                span = trim_at_next_label(tokens[name][found[0]:found[1]], len(label), next_number)
                found = (found[0], found[0] + len(span))
                located[name] = found
                spans[name] = span
        comparison = nc.compare_streams(full, spans, skip=len(nc.tokens(article["label"] + " :")))
        boundary = [d for d in comparison["disagreements"]
                    if d["kind"] == "extra-in-all-streams" and d["at"] >= len(canonical) - 1
                    and structural_tail(d["ocr"], opening_headings(article, articles[position + 1] if position + 1 < len(articles) else None))]
        if boundary:
            comparison["disagreements"] = [d for d in comparison["disagreements"] if d not in boundary]
            comparison["agrees"] = not comparison["disagreements"] and bool(spans)
            comparison["boundaryExtrasDropped"] = [d["ocr"] for d in boundary]
        if not spans:
            comparison["disagreements"].append({"kind": "not-located", "canonical": article["text"][:80], "at": 0})
            comparison["agrees"] = False
        results.append({
            "number": article["number"], "agrees": comparison["agrees"], "canonicalTokens": comparison["canonicalTokens"],
            "confirmedBy": comparison.get("confirmedBy", {}), "disagreements": comparison["disagreements"],
            "located": {n: list(v) for n, v in located.items()},
            "ocr": {n: " ".join(v) for n, v in spans.items()},
            "regions": span_boxes(streams["tesseract"], *located["tesseract"]) if "tesseract" in located else [],
            "labelTokens": len(label), "boundaryExtrasDropped": comparison.get("boundaryExtrasDropped", []),
        })
    agreed = sum(r["agrees"] for r in results)
    return {"source": source_id, "manifest": str(manifest_path.relative_to(pipeline.ROOT)).replace("\\", "/"),
            "manifestSha256": pipeline.hash_file(manifest_path), "rule": "native_check.compare_streams",
            "streams": {n: len(s) for n, s in streams.items()}, "articles": len(results), "agreed": agreed,
            "agreementRate": round(agreed / len(results), 4) if results else 0, "results": results}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("articles", type=pipeline.Path)
    parser.add_argument("manifest", type=pipeline.Path)
    parser.add_argument("source")
    parser.add_argument("--out", type=pipeline.Path, required=True)
    parser.add_argument("--partial", type=int, default=0, help="calibration: compare only the first N articles over the crops OCRed so far")
    args = parser.parse_args()
    articles = pipeline.read_json(args.articles)
    if isinstance(articles, dict):
        articles = articles["quotedArticles"]
    if args.partial:
        articles = articles[:args.partial]
    result = run(articles, args.manifest.resolve(), args.source, partial=bool(args.partial))
    pipeline.write_json(args.out, result)
    kinds = defaultdict(int)
    for r in result["results"]:
        for d in r["disagreements"]:
            kinds[d["kind"]] += 1
    print(f"{result['agreed']}/{result['articles']} agree ({result['agreementRate']:.1%}); disagreements {dict(kinds)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
