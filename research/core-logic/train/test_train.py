"""Tests of the heads' plumbing on made-up vectors (no model file): targets, one training step, the decoder's shape.
Part 7's environment has no pytest, so this runs as a script (`python test_train.py`); pytest also collects it.
"""
from __future__ import annotations

import json
import os
import sys
import tempfile

try:
    import numpy as np
    import torch
except ImportError:  # the pytest Python has neither: this file runs under Part 7's environment (reader.md, rung 4)
    import pytest

    pytest.skip("needs Part 7's environment (numpy, torch): run it there as a script", allow_module_level=True)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decode import best_stretch, reading, value_index_lists  # noqa: E402
from heads import Heads  # noqa: E402
from reader_data import Note, Space, load_export, targets  # noqa: E402
from train_heads import note_loss  # noqa: E402

SPACE = {
    "claims": [
        {"id": "nafaqa", "roles": [{"relation": "marriage", "position": "wife"}],
         "elements": [{"kind": "marriage", "date": True, "amount": False}, {"kind": "maintenance_unpaid", "date": False, "amount": True}]},
        {"id": "custody", "roles": [{"relation": "marriage", "position": "father"}, {"relation": "marriage", "position": "mother"}],
         "elements": [{"kind": "divorce", "date": True, "amount": False}]},
    ],
    "requests": ["maintenance", "custody", "other"],
    "slots": [{"id": "public_body_party", "type": "yesNoUnknown", "choices": [], "askedFor": ["*"]},
              {"id": "contract_date", "type": "date", "choices": [], "askedFor": ["*"]}],
}

FACTS = "تزوج الطرفان في 3 أفريل 2019. لم يدفع النفقة."


def fixture(folder: str) -> tuple[str, str, str]:
    space_path = os.path.join(folder, "space.json")
    with open(space_path, "w", encoding="utf-8") as f:
        json.dump(SPACE, f)
    export, cache = os.path.join(folder, "export"), os.path.join(folder, "cache")
    os.makedirs(export)
    os.makedirs(cache)
    words = [(0, 4), (5, 7), (13, 2), (16, 1), (18, 5), (24, 4), (30, 3), (34, 4), (39, 6)]
    pieces = [{"Id": 3, "field": "subject", "Start": 0, "Length": 0}] + [{"Id": 100 + i, "field": "facts", "Start": s, "Length": n} for i, (s, n) in enumerate(words)]
    rng = np.random.default_rng(1)
    np.savez(os.path.join(cache, "n1.npz"), tokens=rng.standard_normal((len(pieces), 768)).astype(np.float16),
             note=rng.standard_normal(768).astype(np.float16))
    with open(os.path.join(cache, "n1.json"), "w", encoding="utf-8") as f:
        json.dump({"id": "n1", "pieces": pieces}, f)
    q = lambda s, n: {"field": "facts", "quote": FACTS[s:s + n], "occurrence": 1, "start": s, "length": n}  # noqa: E731
    gold = {"claims": ["nafaqa"], "claimEvidence": [q(30, 15)], "roles": [],
            "events": [{"claim": "nafaqa", "kind": "marriage", "polarity": "stated", "actor": "unknown", "evidence": [q(0, 12)],
                        "date": {"kind": "date", "at": q(16, 12), "normalized": "2019-04-03"}},
                       {"claim": "nafaqa", "kind": "maintenance_unpaid", "polarity": "stated", "actor": "defendant", "evidence": [q(30, 15)]}],
            "slots": [{"slot": "public_body_party", "value": "no", "evidence": []}],
            "requests": [{"kind": "maintenance", "evidence": [q(34, 11)]}],
            "values": [{"kind": "date", "at": q(16, 12), "normalized": "2019-04-03", "roles": ["marriage"]}]}
    with open(os.path.join(export, "notes.jsonl"), "w", encoding="utf-8") as f:
        f.write(json.dumps({"id": "n1", "text": {"subject": "", "facts": FACTS, "requests": "", "documents": ""}, "gold": gold,
                            "blanked": [], "provenance": {"writer": {"family": "openai"}}}, ensure_ascii=False) + "\n")
    with open(os.path.join(export, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump({"purpose": "train"}, f)
    with open(os.path.join(export, "values.jsonl"), "w", encoding="utf-8") as f:
        f.write(json.dumps({"id": "n1", "values": [{"kind": "date", "normalized": "2019-04-03", "field": "facts", "start": 16, "length": 12}]}) + "\n")
    return space_path, export, cache


def test_targets_step_and_decoding():
    torch.manual_seed(0)
    with tempfile.TemporaryDirectory() as folder:
        space_path, export, cache = fixture(folder)
        space = Space(space_path)
        assert space.claims == ["nafaqa", "custody", "outside"]
        assert space.party_claims == ["custody"]
        notes, manifest = load_export(export, cache)
        assert manifest["purpose"] == "train" and len(notes) == 1
        note = notes[0]
        t = targets(space, note)
        assert t.claims.tolist() == [1.0, 0.0, 0.0]
        assert [(e[1], e[2]) for e in t.elements] == [(1, -1), (1, 1)]  # stated; actor unknown carries no loss
        assert t.requests.tolist() == [1.0, 0.0, 0.0]
        assert t.value_roles and all(space.value_roles[r] in ("event:marriage", "event:maintenance_unpaid") or space.value_roles[r].startswith("slot:")
                                     for _, r, _ in t.value_roles)
        assert any(y == 1.0 and space.value_roles[r] == "event:marriage" for _, r, y in t.value_roles)
        model = Heads(space)
        loss = note_loss(model, space, note, t, "focal", None)
        loss.backward()
        assert torch.isfinite(loss) and model.claim_q.grad is not None
        model.eval()
        out = model(torch.from_numpy(note.tokens), torch.from_numpy(note.fields), torch.from_numpy(note.vector), value_index_lists(note))
        r = reading(space, note, out, model, ["nafaqa"], {"name": "test", "version": "0", "files": {}})
        assert [a["claimId"] for a in r["claim"]["answers"]] == ["nafaqa"]
        assert r["claim"]["answers"][0]["role"]["position"] == "wife"  # one role: from the catalogue
        assert {(e["claim"], e["kind"]) for e in r["events"]} == {("nafaqa", "marriage"), ("nafaqa", "maintenance_unpaid")}
        assert {s["slot"] for s in r["slots"]} == {"public_body_party", "contract_date"}
        assert r["values"][0]["span"] == {"field": "facts", "start": 16, "length": 12}
        json.dumps(r)


def test_best_stretch_stays_in_one_field():
    note = Note(id="x", tokens=np.zeros((5, 768), np.float32), vector=np.zeros(768, np.float32),
                fields=np.array([0, 1, 1, 2, 2]), starts=np.array([0, 0, 4, 0, 3]), lengths=np.array([0, 3, 2, 2, 2]),
                text={}, values=[])
    span = best_stretch(note, np.array([0.0, 3.0, 3.0, 3.0, 3.0]))
    assert len(span) == 1 and span[0]["field"] in ("facts", "requests") and span[0]["start"] == 0
    assert best_stretch(note, np.array([-5.0] * 5)) == []


if __name__ == "__main__":
    test_targets_step_and_decoding()
    test_best_stretch_stays_in_one_field()
    print("2 passed")
