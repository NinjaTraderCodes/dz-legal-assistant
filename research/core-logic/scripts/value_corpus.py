"""The code-made value corpus (docs/research/core-logic/specs/reader.md, rung 2): dates and amounts rendered in every
written form of the labelling guide's section 8, inside carrier sentences, each with its normal form and its exact span.
Written from the guide and from reader.md's span rule, never from the parsers: the parsers must read 98 % of it exactly.

    python value_corpus.py --dates 2000 --amounts 2000 --seed 11 --out <file.jsonl>

Each line: {"text", "kind": "date"|"amount", "normalized", "start", "length"} with start and length in UTF-16 units
(every character used here is one UTF-16 unit)."""
from __future__ import annotations

import argparse
import calendar
import json
import random
import sys

INDIC = str.maketrans("0123456789", "٠١٢٣٤٥٦٧٨٩")

# The month names: Algerian (the guide's), Egyptian, Levantine.
MONTHS = {
    1: ["جانفي", "يناير", "كانون الثاني"], 2: ["فيفري", "فبراير", "شباط"], 3: ["مارس", "آذار"],
    4: ["أفريل", "أبريل", "نيسان"], 5: ["ماي", "مايو", "أيار"], 6: ["جوان", "يونيو", "حزيران"],
    7: ["جويلية", "يوليو", "تموز"], 8: ["أوت", "أغسطس", "آب"], 9: ["سبتمبر", "أيلول"],
    10: ["أكتوبر", "تشرين الأول"], 11: ["نوفمبر", "تشرين الثاني"], 12: ["ديسمبر", "كانون الأول"],
}

DATE_CARRIERS = [
    "أبرم العقد {lead}{value} بين الطرفين.", "وجه الموكل إعذارا {lead}{value} عن طريق محضر قضائي.",
    "تسلم العامل نسخة من المحضر {lead}{value}، ولم يتم الصلح.", "حل أجل الوفاء {lead}{value} دون أن يدفع المدين شيئا.",
    "{lead}{value} توقف المستأجر عن دفع بدل الإيجار.", "صدر الحكم {lead}{value} وأصبح نهائيا.",
]
DATE_LEADS = ["بتاريخ ", "في ", "يوم ", ""]

AMOUNT_CARRIERS = [
    "اقترض المدعى عليه من الموكل مبلغ {value} ولم يرده.", "بقي في ذمة المشتري {value} من ثمن البيع.",
    "يطالب العامل بأجور غير مدفوعة قدرها {value}.", "حدد التعويض المطلوب بـ {value} عن الضرر اللاحق.",
    "تبلغ قيمة الفواتير غير المسددة {value}، حسب كشف الحساب.", "دفع المدين جزءا من الدين قدره {value} فقط.",
]

ONES = ["", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة"]
TEENS = {10: "عشرة", 11: "أحد عشر", 12: "اثنا عشر", 13: "ثلاثة عشر", 14: "أربعة عشر", 15: "خمسة عشر",
         16: "ستة عشر", 17: "سبعة عشر", 18: "ثمانية عشر", 19: "تسعة عشر"}
TENS = ["", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"]
HUNDREDS = ["", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"]
SCALES = [(1_000_000_000, "مليار", "ملايير", "ملياران"), (1_000_000, "مليون", "ملايين", "مليونان"),
          (1_000, "ألف", "آلاف", "ألفان")]


def words_below_1000(n: int) -> str:
    parts = []
    if n >= 100:
        parts.append(HUNDREDS[n // 100])
        n %= 100
    if 10 <= n <= 19:
        parts.append(TEENS[n])
    elif n:
        ones, tens = n % 10, n // 10
        if ones and tens:
            parts.append(f"{ONES[ones]} و{TENS[tens]}")
        elif tens:
            parts.append(TENS[tens])
        else:
            parts.append(ONES[ones])
    return " و".join(parts)


def words(n: int) -> str:
    """Formal Arabic words for a whole number, scale by scale («ثلاثمائة وخمسون ألف»)."""
    parts = []
    for value, one, many, two in SCALES:
        count, n = divmod(n, value)
        if count == 1:
            parts.append(one)
        elif count == 2:
            parts.append(two)
        elif 3 <= count <= 10:
            parts.append(f"{words_below_1000(count)} {many}")
        elif count:
            parts.append(f"{words_below_1000(count)} {one}")
    if n:
        parts.append(words_below_1000(n))
    return " و".join(parts)


def thousands(n: int, sep: str) -> str:
    s = str(n)
    groups = []
    while s:
        groups.insert(0, s[-3:])
        s = s[:-3]
    return sep.join(groups)


def date_item(rng: random.Random) -> dict:
    year = rng.randint(1990, 2026)
    month = rng.randint(1, 12)
    day = rng.randint(1, calendar.monthrange(year, month)[1])
    form = rng.choice(["numeric", "numeric", "iso", "named", "named", "named-min", "month-year", "year"])
    lead = rng.choice(DATE_LEADS)
    if form == "numeric":
        sep = rng.choice("/-.")
        dd = f"{day:02d}" if rng.random() < 0.7 else str(day)
        mm = f"{month:02d}" if rng.random() < 0.7 else str(month)
        value, normal = f"{dd}{sep}{mm}{sep}{year}", f"{year:04d}-{month:02d}-{day:02d}"
    elif form == "iso":
        sep = rng.choice("/-.")
        value, normal = f"{year}{sep}{month:02d}{sep}{day:02d}", f"{year:04d}-{month:02d}-{day:02d}"
    elif form in ("named", "named-min"):
        name = rng.choice(MONTHS[month])
        middle = " من شهر " if form == "named-min" else " "
        value, normal = f"{day}{middle}{name} {year}", f"{year:04d}-{month:02d}-{day:02d}"
    elif form == "month-year":
        name = rng.choice(MONTHS[month])
        lead = rng.choice(["في شهر ", "خلال ", "في "])
        value, normal = f"{name} {year}", f"{year:04d}-{month:02d}"
    else:
        lead = rng.choice(["سنة ", "عام ", "في سنة "])
        value, normal = str(year), f"{year:04d}"
    if rng.random() < 0.2:
        value = value.translate(INDIC)
    return render(rng.choice(DATE_CARRIERS), lead, value, "date", normal)


def amount_item(rng: random.Random) -> dict:
    form = rng.choice(["dots", "dots", "space", "comma", "plain", "cents", "fraction", "words", "words+digits",
                       "digits+scale", "half", "centimes"])
    unit = rng.choice(["دج", "دينار", "د.ج", "دينار جزائري"])
    if form in ("dots", "space", "comma", "plain", "cents"):
        n = rng.choice([rng.randint(1, 999) * 1000, rng.randint(1000, 99_999_999)])
        sep = {"dots": ".", "space": " ", "comma": ",", "plain": "", "cents": "."}[form]
        text = thousands(n, sep) if sep else str(n)
        if form == "cents":
            text += ",00"
        value, normal = f"{text} {unit}", str(n)
    elif form == "fraction":
        n, cents = rng.randint(100, 99_999), rng.randint(1, 99)
        sep_dec = rng.choice([",", "."])
        value, normal = f"{n}{sep_dec}{cents:02d} {unit}", f"{n}.{cents:02d}"
    elif form in ("words", "words+digits"):
        n = rng.choice([rng.randint(1, 999) * 1000, rng.randint(1, 9) * 1_000_000 + rng.randint(0, 9) * 100_000,
                        rng.randint(21, 999)])
        text = words(n)
        if form == "words+digits":
            text += f" ({thousands(n, '.')})"
        value, normal = f"{text} {unit}", str(n)
    elif form == "digits+scale":
        count = rng.randint(2, 900)
        scale, one, many, _ = rng.choice(SCALES[1:])
        word = many if 3 <= count <= 10 else one
        value, normal = f"{count} {word} {unit}", str(count * scale)
    elif form == "half":
        scale, one, _, _ = rng.choice(SCALES)
        repeat = rng.random() < 0.4
        value = f"{one} ونصف {one + ' ' if repeat else ''}{unit}"
        normal = str(scale + scale // 2)
    else:  # centimes: 100 centimes = 1 dinar
        count = rng.randint(2, 900)
        scale, one, many, _ = rng.choice(SCALES[:2])
        word = many if 3 <= count <= 10 else one
        value, normal = f"{count} {word} سنتيم", str(count * scale // 100)
    if rng.random() < 0.15 and form not in ("words", "words+digits", "half"):
        value = value.translate(INDIC)
    return render(rng.choice(AMOUNT_CARRIERS), "", value, "amount", normal)


def render(carrier: str, lead: str, value: str, kind: str, normal: str) -> dict:
    """The carrier sentence with the value in place; the span is the value alone (reader.md, the span rule)."""
    head, tail = carrier.split("{value}")
    head = head.replace("{lead}", lead)
    text = head + value + tail
    return {"text": text, "kind": kind, "normalized": normal, "start": len(head), "length": len(value)}


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="value_corpus")
    p.add_argument("--dates", type=int, default=2000)
    p.add_argument("--amounts", type=int, default=2000)
    p.add_argument("--seed", type=int, default=11)
    p.add_argument("--out", required=True)
    a = p.parse_args(argv)
    rng = random.Random(a.seed)
    items = [date_item(rng) for _ in range(a.dates)] + [amount_item(rng) for _ in range(a.amounts)]
    with open(a.out, "w", encoding="utf-8", newline="\n") as f:
        for item in items:
            f.write(json.dumps(item, ensure_ascii=False) + "\n")
    print(len(items), "items ->", a.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
