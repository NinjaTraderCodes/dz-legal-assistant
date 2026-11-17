"""The near-copy probe's own test (specs/probes.md, section 1): 20 copies with changed names and dates are all flagged, and 20
paraphrases at about half overlap none. Made-up texts; plain Python (runs under the repository's pytest)."""
from __future__ import annotations

import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from probes import near_copies, placeholder_facts  # noqa: E402

WORDS = ("المدعي المدعى عليه العقد البيع الثمن التسليم الموعد الإعذار المحضر القضائي الشركة المؤسسة المبلغ الأجر "
         "العمل الفصل المصالحة المحكمة القسم الطلب التعويض الضرر الإيجار المحل الشهر السنة الدفع القسط الباقي الذمة "
         "الزوج الزوجة الحضانة الطفل النفقة السكن الولاية الطلاق الخلع المقابل الشاهد الوصل الكشف الملف الوثيقة").split()


def text(rng, n=60):
    return " ".join(rng.choice(WORDS) for _ in range(n))


def note(i, facts, name, date_text):
    full = f"{name} {facts} في {date_text}"
    return ({"id": f"n{i}", "set": "t", "text": {"facts": full}},
            [{"kind": "date", "field": "facts", "start": full.index(date_text), "length": len(date_text)}], [name])


def test_copies_with_new_names_and_dates_are_flagged_and_half_paraphrases_are_not():
    rng = random.Random(5)
    items, copies, paraphrases = [], set(), set()
    for k in range(20):
        base = text(rng)
        a = note(f"{k}a", base, "سليم بن عيسى", "12 مارس 2021")
        b = note(f"{k}b", base, "كريمة مسعودي", "3 جوان 2024")  # the same note, other name and date
        words = base.split()
        half = " ".join(w if i % 2 == 0 else rng.choice(WORDS) for i, w in enumerate(words))
        c = note(f"{k}c", half, "نادية بوعلام", "8 أوت 2022")  # about half the words kept
        for n, v, names in (a, b, c):
            items.append((n["id"], "t", placeholder_facts(n, v, names)))
        copies.add(frozenset((f"n{k}a", f"n{k}b")))
        paraphrases |= {frozenset((f"n{k}a", f"n{k}c")), frozenset((f"n{k}b", f"n{k}c"))}
    found, _ = near_copies(items)
    pairs = {frozenset((p["a"], p["b"])) for p in found}
    assert copies <= pairs
    assert not (pairs & paraphrases)
