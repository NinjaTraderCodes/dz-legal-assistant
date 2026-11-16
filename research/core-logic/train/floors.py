"""The floors of the route experiment (the owner, 17 November 2026; specs/reader.md, rung 3): linear models with no deep
training, scored by the same scorer as the reader.

- `ngrams`: TF-IDF of word 1-2-grams and character 2-5-grams of the note's text (the level-2 normalisation is not applied:
  the scorer reads what the model answers, and the parsers find the values);
- `vectors`: the 8-bit encoder's cached note vector (and, for a value, the mean of the token vectors around it).

For each: the claim types one-vs-rest (decoded as reader.md's decoder: at 0.5 or more, at most three, `outside` alone);
each element's polarity by its own classifier, trained on the notes of its claim type; each request kind one-vs-rest; each
yes/no or choice slot; the role of each date and amount the parsers found, from the words around it (the role among the
candidates of the note's claim types). Regularisation C among 1, 10 and 100, chosen on the tune notes by the tuning score.

    python floors.py --features ngrams|vectors --cache <cache> --train <export> --tune <export> --measure <export>
                     --space <label-space.json> --scorer <score.exe> --out <folder>
"""
from __future__ import annotations

import argparse
import json
import os
import sys

import numpy as np
from scipy.sparse import hstack
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decode import answered_claims  # noqa: E402
from reader_data import FIELDS, OUTSIDE, POLARITIES, Space, load_export, value_pieces  # noqa: E402
from train_heads import purpose_of, score  # noqa: E402


def text_of(note) -> str:
    return " [SEP] ".join(note.text.get(f) or "" for f in FIELDS)


def context(note, v, width=60) -> str:
    t = note.text.get(v["field"]) or ""
    return t[max(0, v["start"] - width):v["start"]] + " [V] " + t[v["start"] + v["length"]:v["start"] + v["length"] + width]


class Features:
    def __init__(self, kind: str, train):
        self.kind = kind
        if kind == "ngrams":
            self.word = TfidfVectorizer(analyzer="word", ngram_range=(1, 2), min_df=2, sublinear_tf=True).fit([text_of(n) for n in train])
            self.char = TfidfVectorizer(analyzer="char_wb", ngram_range=(2, 5), min_df=2, sublinear_tf=True, max_features=60000).fit([text_of(n) for n in train])
            ctx = [context(n, v) for n in train for v in n.values] or ["x"]
            self.cword = TfidfVectorizer(analyzer="word", ngram_range=(1, 2), min_df=1, sublinear_tf=True).fit(ctx)
            self.cchar = TfidfVectorizer(analyzer="char_wb", ngram_range=(2, 4), min_df=2, sublinear_tf=True).fit(ctx)

    def notes(self, notes):
        if self.kind == "ngrams":
            t = [text_of(n) for n in notes]
            return hstack([self.word.transform(t), self.char.transform(t)]).tocsr()
        return np.stack([n.vector for n in notes])

    def values(self, pairs):
        """pairs: (note, value) -> rows."""
        if self.kind == "ngrams":
            c = [context(n, v) + (" DATE" if v["kind"] == "date" else " AMOUNT") for n, v in pairs]
            return hstack([self.cword.transform(c), self.cchar.transform(c)]).tocsr()
        rows = []
        for n, v in pairs:
            idx = value_pieces(n, v)
            a, b = (int(idx.min()), int(idx.max()) + 1) if len(idx) else (0, 1)
            around = np.concatenate([n.tokens[max(0, a - 16):a], n.tokens[b:b + 16]]) if len(n.tokens) else np.zeros((1, 768))
            rows.append(np.concatenate([n.tokens[a:b].mean(0), around.mean(0) if len(around) else np.zeros(768), [1.0 if v["kind"] == "date" else 0.0]]))
        return np.stack(rows)


class Constant:
    def __init__(self, label):
        self.label = label

    def predict_proba(self, x):
        return np.ones((x.shape[0], 1))

    @property
    def classes_(self):
        return np.array([self.label])


def fit(x, y, c):
    y = np.asarray(y)
    if len(set(y.tolist())) < 2:
        return Constant(y[0] if len(y) else 0)
    return LogisticRegression(C=c, max_iter=3000, class_weight="balanced").fit(x, y)


def proba(model, x, label):
    p = model.predict_proba(x)
    classes = list(model.classes_)
    return p[:, classes.index(label)] if label in classes else np.zeros(x.shape[0])


def best_class(model, x):
    p = model.predict_proba(x)
    i = p.argmax(1)
    return [model.classes_[j] for j in i], p.max(1)


class Floor:
    def __init__(self, space: Space, kind: str, train, c: float):
        self.space, self.c = space, c
        self.f = Features(kind, train)
        x = self.f.notes(train)
        self.claim = {k: fit(x, [1 if k in n.gold["claims"] else 0 for n in train], c) for k in space.claims}
        self.request = {k: fit(x, [1 if any(r["kind"] == k for r in n.gold.get("requests", [])) else 0 for n in train], c) for k in space.requests}
        self.slot = {}
        for s in space.class_slots:
            rows = [(i, next((g["value"] for g in n.gold.get("slots", []) if g["slot"] == s["id"]), None)) for i, n in enumerate(train)]
            rows = [(i, v) for i, v in rows if v is not None and f"slots/{s['id']}" not in train[i].blanked]
            if rows:
                self.slot[s["id"]] = fit(x[[i for i, _ in rows]], [v for _, v in rows], c)
        self.polarity = {}
        for key in space.elements:
            rows = []
            for i, n in enumerate(train):
                if key[0] in n.gold["claims"] and f"events/{key[0]}/{key[1]}/polarity" not in n.blanked:
                    e = next((e for e in n.gold.get("events", []) if (e["claim"], e["kind"]) == key), None)
                    rows.append((i, e["polarity"] if e else "notMentioned"))
            if rows:
                self.polarity[key] = fit(x[[i for i, _ in rows]], [p for _, p in rows], c)
        pairs, labels = [], []
        for n in train:
            for v in n.values:
                g = [gv for gv in n.gold.get("values", []) if gv["kind"] == v["kind"] and gv.get("normalized") == v["normalized"]
                     and gv["at"].get("field") == v["field"] and gv["at"].get("start") is not None
                     and gv["at"]["start"] < v["start"] + v["length"] and v["start"] < gv["at"]["start"] + gv["at"]["length"]]
                if g:
                    pairs.append((n, v))
                    labels.append((g[0].get("roles") or ["none"])[0])
        self.role = fit(self.f.values(pairs), labels, c) if pairs else None

    def reading(self, note, given, info):
        sp = self.space
        x = self.f.notes([note])
        pc = np.array([proba(self.claim[k], x, 1)[0] for k in sp.claims])
        claims = list(given) if given is not None else [sp.claims[i] for i in answered_claims(sp, pc)]
        answers = []
        for c in claims:
            g = {"claimId": c, "confidence": float(pc[sp.claim_index[c]]) if c in sp.claim_index else 1.0, "evidence": []}
            roles = sp.roles_of.get(c, [])
            if len(roles) == 1:
                g["role"] = {"relation": roles[0][0], "position": roles[0][1], "confidence": 1.0, "abstained": False, "evidence": []}
            answers.append(g)
        inside = [c for c in claims if c != OUTSIDE]
        role_of = {}
        if self.role is not None and note.values:
            xs = self.f.values([(note, v) for v in note.values])
            p = self.role.predict_proba(xs)
            classes = list(self.role.classes_)
            for vi, v in enumerate(note.values):
                cands = {sp.role_name(r) for r in sp.candidate_roles(v["kind"], inside)} | {"none"}
                ranked = sorted(((p[vi][j], cl) for j, cl in enumerate(classes) if cl in cands), reverse=True)
                role_of[vi] = (ranked[0][1], float(ranked[0][0])) if ranked and ranked[0][1] != "none" else None
        values = [{"kind": v["kind"], "normalized": v["normalized"], "span": {"field": v["field"], "start": v["start"], "length": v["length"]},
                   "roles": [role_of[i][0]] if role_of.get(i) else [], "confidence": 1.0} for i, v in enumerate(note.values)]

        def value_for(role, kind):
            best = None
            for i, v in enumerate(note.values):
                if v["kind"] == kind and role_of.get(i) and role_of[i][0] == role and (best is None or role_of[i][1] > best[1]):
                    best = (i, role_of[i][1])
            return dict(values[best[0]], confidence=best[1]) if best else None

        events = []
        for c in inside:
            for key in sp.elements_of.get(c, []):
                m = self.polarity.get(key)
                pol, conf = (best_class(m, x)[0][0], float(best_class(m, x)[1][0])) if m is not None else ("notMentioned", 1.0)
                e = {"claim": key[0], "kind": key[1], "polarity": pol, "confidence": conf, "abstained": False, "actor": "unknown", "evidence": []}
                if pol == "disputed":
                    e["disputedBy"] = "defendant"
                if pol != "notMentioned":
                    takes_date, takes_amount = sp.element_takes[key]
                    if takes_date and (d := value_for(key[1], "date")):
                        e["date"] = d
                    if takes_amount and (am := value_for(key[1], "amount")):
                        e["amount"] = am
                events.append(e)
        slots = []
        for s in sp.class_slots:
            m = self.slot.get(s["id"])
            v, conf = (best_class(m, x)[0][0], float(best_class(m, x)[1][0])) if m is not None else ("unknown", 1.0)
            slots.append({"slot": s["id"], "value": v, "confidence": conf, "abstained": False, "evidence": []})
        for sid in sp.date_slots:
            d = value_for(sid, "date")
            slots.append({"slot": sid, "value": d["normalized"] if d else "unknown", "confidence": 1.0, "abstained": False, "evidence": [d["span"]] if d else []})
        requests = []
        for k in sp.requests:
            pr = float(proba(self.request[k], x, 1)[0])
            if pr >= 0.5:
                r = {"kind": k, "confidence": pr, "evidence": []}
                if (am := value_for(k, "amount")):
                    r["amount"] = am
                requests.append(r)
        return {"claim": {"answers": answers, "others": [], "abstained": False}, "events": events, "slots": slots,
                "requests": requests, "values": values, "info": info}


def write(floor, notes, folder, info):
    os.makedirs(folder, exist_ok=True)
    with open(os.path.join(folder, "answers.jsonl"), "w", encoding="utf-8", newline="\n") as fa, \
            open(os.path.join(folder, "given.jsonl"), "w", encoding="utf-8", newline="\n") as fg:
        for n in notes:
            fa.write(json.dumps({"id": n.id, "reading": floor.reading(n, None, info)}, ensure_ascii=False) + "\n")
            gold = [c for c in n.gold.get("claims", []) if c != OUTSIDE]
            if gold:
                fg.write(json.dumps({"id": n.id, "reading": floor.reading(n, gold, info)}, ensure_ascii=False) + "\n")


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    for name in ("cache", "train", "tune", "measure", "space", "scorer", "out"):
        p.add_argument(f"--{name}", required=True)
    p.add_argument("--features", choices=["ngrams", "vectors"], required=True)
    a = p.parse_args(argv)
    if purpose_of(a.train) != "train" or purpose_of(a.tune) != "tune" or purpose_of(a.measure) != "measure":
        raise SystemExit("train, tune and measure folders must be exported for those purposes")
    space = Space(a.space)
    train, _ = load_export(a.train, a.cache)
    tune, _ = load_export(a.tune, a.cache)
    measure, _ = load_export(a.measure, a.cache)
    results = {}
    for c in (1.0, 10.0, 100.0):
        floor = Floor(space, a.features, train, c)
        info = {"name": f"floor-{a.features}", "version": f"C{c:g}", "files": {}}
        folder = os.path.join(a.out, f"tune-C{c:g}")
        write(floor, tune, folder, info)
        results[c] = (score(a.scorer, os.path.join(a.tune, "notes.jsonl"), os.path.join(folder, "answers.jsonl"),
                            os.path.join(folder, "given.jsonl"), folder, "tune", True).get("tuning"), floor, info)
        print(f"{a.features} C={c:g}: tuning {results[c][0]}", flush=True)
    c = max(results, key=lambda k: results[k][0] or -1)
    _, floor, info = results[c]
    folder = os.path.join(a.out, "measure")
    write(floor, measure, folder, info)
    score(a.scorer, os.path.join(a.measure, "notes.jsonl"), os.path.join(folder, "answers.jsonl"), os.path.join(folder, "given.jsonl"), folder, "measure", False)
    with open(os.path.join(a.out, "run.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"features": a.features, "C": c, "tuning": {str(k): v[0] for k, v in results.items()}}, f, indent=1)
    print(f"chosen C={c:g} -> {folder}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
