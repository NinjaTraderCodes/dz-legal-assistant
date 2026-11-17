"""The probes of docs/research/core-logic/specs/probes.md that the pilot report needs (sections 1, 2a, 2b, 2c, 2e), on the
open sets only (never a hidden set: the loader's exports are the only input).

1. Near-copies: the `facts` at the level-2 normalisation, every date, amount and cast name replaced by a placeholder;
   Jaccard on character 5-grams (exact: the sets are small) and the longest common run of words. A pair at 0.80 or more,
   or sharing 25 words or more in a row, is a near-copy; 0.60 to 0.80 is listed for a look.
2a. The shallow classifier: word 1-2-grams and character 3-5-grams, a linear model per label (the claim type; the
   polarity of each element with enough notes), cross-validated by scenario family; its accuracy against the majority
   class and the 20 features with the largest weights.
2b. Style against label: the claim type predicted from the style card alone (cross-validated), against the majority.
2c. Length: the claim type predicted from the note's length alone.
2e. Writer identity: Codex-written against Sonnet-written, from the text (dev only, where both write), and whether the
   writer predicts the claim type.

    python probes.py --export <export folder> [--export ...] --scenarios <scenarios.jsonl> [...] --out <report.json>
"""
from __future__ import annotations

import argparse
import collections
import itertools
import json
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "scripts"))
import textcmp  # noqa: E402

FIELDS = ["subject", "facts", "requests", "documents"]


def load(folder):
    notes = []
    with open(os.path.join(folder, "notes.jsonl"), encoding="utf-8") as f:
        notes += [json.loads(x) for x in f if x.strip()]
    values = {}
    path = os.path.join(folder, "values.jsonl")
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            for x in f:
                if x.strip():
                    v = json.loads(x)
                    values[v["id"]] = v["values"]
    return notes, values


def names_of(scenario):
    cast = scenario.get("cast") or {}
    out = []
    for k in ("plaintiff", "defendant"):
        n = (cast.get(k) or {}).get("name")
        if n:
            out += [n] + n.split()
    out += cast.get("children") or []
    return [x for x in out if len(x) > 2]


def placeholder_facts(note, values, names):
    """The facts with every parsed date and amount, every cast name and every digit run replaced, at level 2."""
    t = note["text"].get("facts") or ""
    spans = sorted((v["start"], v["start"] + v["length"], "<D>" if v["kind"] == "date" else "<A>")
                   for v in values if v["field"] == "facts")
    out, last = [], 0
    for a, b, ph in spans:
        if a >= last:
            out.append(t[last:a])
            out.append(f" {ph} ")
            last = b
    out.append(t[last:])
    t = "".join(out)
    for n in sorted(names, key=len, reverse=True):
        t = t.replace(n, " <N> ")
    t = re.sub(r"[0-9٠-٩]+", " <A> ", t)
    return textcmp.norm2(t, keep_lines=False)


def grams(text, n=5):
    t = re.sub(r"\s+", " ", text)
    return {t[i:i + n] for i in range(max(0, len(t) - n + 1))}


def longest_run(a, b):
    wa, wb = a.split(), b.split()
    best, prev = 0, [0] * (len(wb) + 1)
    for x in wa:
        cur = [0] * (len(wb) + 1)
        for j, y in enumerate(wb, 1):
            if x == y:
                cur[j] = prev[j - 1] + 1
                best = max(best, cur[j])
        prev = cur
    return best


def near_copies(items, high=0.80, low=0.60, run=25):
    """items: (id, set, normalised facts). Returns the near-copy pairs and the pairs to look at."""
    g = [(i, s, t, grams(t)) for i, s, t in items]
    found, look = [], []
    for (i1, s1, t1, g1), (i2, s2, t2, g2) in itertools.combinations(g, 2):
        if not g1 or not g2:
            continue
        j = len(g1 & g2) / len(g1 | g2)
        if j < low:
            continue
        r = longest_run(t1, t2)
        pair = {"a": i1, "b": i2, "sets": f"{s1}/{s2}", "jaccard": round(j, 3), "run": r}
        (found if j >= high or r >= run else look).append(pair)
    return found, look


def text_of(note):
    return " [SEP] ".join(note["text"].get(f) or "" for f in FIELDS)


def shallow(texts, labels, groups, analyzer, ngram):
    from sklearn.feature_extraction.text import TfidfVectorizer
    from sklearn.linear_model import LogisticRegression
    from sklearn.model_selection import GroupKFold, cross_val_predict
    vec = TfidfVectorizer(analyzer=analyzer, ngram_range=ngram, min_df=2, sublinear_tf=True, max_features=50000)
    x = vec.fit_transform(texts)
    model = LogisticRegression(C=10, max_iter=3000)
    k = min(5, len(set(groups)))
    pred = cross_val_predict(model, x, labels, groups=groups, cv=GroupKFold(n_splits=k))
    acc = sum(p == y for p, y in zip(pred, labels)) / len(labels)
    model.fit(x, labels)
    names = vec.get_feature_names_out()
    top = {}
    for ci, c in enumerate(model.classes_):
        w = model.coef_[ci] if len(model.classes_) > 2 else (model.coef_[0] if ci == 1 else -model.coef_[0])
        top[str(c)] = [names[i] for i in w.argsort()[::-1][:20]]
    return acc, top


def majority(labels):
    return collections.Counter(labels).most_common(1)[0][1] / len(labels)


def cv_simple(features, labels, groups):
    from sklearn.linear_model import LogisticRegression
    from sklearn.model_selection import GroupKFold, cross_val_predict
    pred = cross_val_predict(LogisticRegression(max_iter=3000), features, labels, groups=groups, cv=GroupKFold(n_splits=min(5, len(set(groups)))))
    return sum(p == y for p, y in zip(pred, labels)) / len(labels)


def main(argv=None):
    p = argparse.ArgumentParser()
    p.add_argument("--export", action="append", required=True)
    p.add_argument("--scenarios", action="append", required=True)
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)
    scen = {}
    for path in a.scenarios:
        with open(path, encoding="utf-8") as f:
            for x in f:
                if x.strip():
                    s = json.loads(x)
                    scen[s["id"]] = s
    notes, values = [], {}
    for folder in a.export:
        n, v = load(folder)
        notes += n
        values.update(v)
    report = {"notes": len(notes)}
    items = [(n["id"], n["set"], placeholder_facts(n, values.get(n["id"], []), names_of(scen[n["provenance"]["scenario"]]))) for n in notes]
    found, look = near_copies(items)
    report["nearCopies"] = {"pairs": found, "toLookAt": len(look), "highestToLookAt": max((x["jaccard"] for x in look), default=None)}
    train = [n for n in notes if n["set"] in ("pilot", "train")]
    texts = [text_of(n) for n in train]
    groups = [n["provenance"]["scenarioFamily"] for n in train]
    claims = [n["gold"]["claims"][0] for n in train]
    report["2a"] = {"claimType": {}}
    for name, analyzer, ngram in (("words", "word", (1, 2)), ("chars", "char_wb", (3, 5))):
        acc, top = shallow(texts, claims, groups, analyzer, ngram)
        report["2a"]["claimType"][name] = {"accuracy": round(acc, 3), "majority": round(majority(claims), 3), "topFeatures": top}
    by_element = collections.defaultdict(list)
    for i, n in enumerate(train):
        for e in n["gold"].get("events", []):
            by_element[e["kind"]].append((i, e["polarity"]))
    report["2a"]["polarity"] = {}
    for kind, rows in sorted(by_element.items(), key=lambda kv: -len(kv[1])):
        labels = [r[1] for r in rows]
        if len(rows) < 40 or len(set(labels)) < 2 or collections.Counter(labels).most_common()[-1][1] < 3:
            continue
        acc, top = shallow([texts[i] for i, _ in rows], labels, [groups[i] for i, _ in rows], "word", (1, 2))
        report["2a"]["polarity"][kind] = {"n": len(rows), "accuracy": round(acc, 3), "majority": round(majority(labels), 3),
                                          "topFeatures": {k: v[:10] for k, v in top.items()}}
    import numpy as np
    styles = sorted({scen[n["provenance"]["scenario"]]["style"] for n in train})
    style_x = np.array([[1.0 if scen[n["provenance"]["scenario"]]["style"] == s else 0.0 for s in styles] for n in train])
    report["2b"] = {"claimFromStyleCard": round(cv_simple(style_x, claims, groups), 3), "majority": round(majority(claims), 3)}
    length_x = np.array([[len(text_of(n).split()) / 100.0] for n in train])
    report["2c"] = {"claimFromLength": round(cv_simple(length_x, claims, groups), 3), "majority": round(majority(claims), 3)}
    dev = [n for n in notes if n["set"] == "dev"]
    if dev and len({n["provenance"]["writer"]["family"] for n in dev}) == 2:
        writers = [n["provenance"]["writer"]["family"] for n in dev]
        acc, top = shallow([text_of(n) for n in dev], writers, [n["provenance"]["scenarioFamily"] for n in dev], "word", (1, 2))
        report["2e"] = {"writerFromText": round(acc, 3), "majority": round(majority(writers), 3), "topFeatures": {k: v[:15] for k, v in top.items()},
                        "claimsPerWriter": {w: dict(collections.Counter(n["gold"]["claims"][0] for n in dev if n["provenance"]["writer"]["family"] == w)) for w in set(writers)}}
    with open(a.out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(report, f, ensure_ascii=False, indent=1)
    print(json.dumps({k: (v if k != "2a" else {"claimType": {m: {x: y for x, y in r.items() if x != "topFeatures"} for m, r in v["claimType"].items()},
                                                "polarityKinds": len(v["polarity"])}) for k, v in report.items() if k != "nearCopies"}, ensure_ascii=False))
    print("near-copies:", len(found), "to look at:", len(look))
    return 0


if __name__ == "__main__":
    sys.exit(main())
