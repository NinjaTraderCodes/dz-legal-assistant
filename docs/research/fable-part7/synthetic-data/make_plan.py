import json, os, random, sys
sys.path.insert(0, r"C:\Users\TheShadow\dz-arch-research\tools")
import law

random.seed(20261111)
OUT = os.path.dirname(os.path.abspath(__file__))
claims = law.claims()
OTHER = [
    "inheritance partition between heirs", "administrative appeal against a decision of a commune or wilaya",
    "dispute between partners of a company (dissolution, accounts)", "correction of a civil-status record",
    "traffic accident compensation against an insurer", "possession action over a plot of land",
    "boundary or easement dispute between neighbours", "proof of an unregistered marriage",
    "divorce by mutual consent (joint request)", "criminal complaint for fraud or breach of trust",
    "recognition or denial of filiation", "kafala or guardianship authorisation for a minor",
    "pre-emption (chafaa) on a sale of land", "medical liability against a public hospital",
    "social security dispute (pension, work accident recognition)", "cancellation of a cheque or a bill of exchange dispute",
    "expropriation compensation", "intellectual property or trademark dispute", "bankruptcy or judicial settlement of a trader",
    "request for a payment order only (no full claim)", "collective labour dispute or strike", "tax dispute",
    "consumer complaint about a defective product against a manufacturer abroad", "maritime or air transport dispute",
]
LANGS = [("msa", 0.52), ("darija", 0.26), ("fr", 0.14), ("msa_fr_terms", 0.08)]
LENGTHS = [("one line, 8 to 18 words", 0.3), ("two or three short sentences", 0.45), ("four to six sentences with side details", 0.25)]
VOICES = ["telegraphic lawyer's notes, no verbs wasted", "plain narrative of the facts", "what the client told the lawyer, reported",
          "list of facts, one per line", "notes written in a hurry, with one irrelevant detail"]
HINTS = [("names the legal notion explicitly", 0.35), ("describes the facts only, with no legal term", 0.65)]
def pick(pairs):
    r = random.random(); acc = 0
    for v, p in pairs:
        acc += p
        if r <= acc:
            return v
    return pairs[-1][0]

rows = []
PER_CLASS = 62
for c in claims:
    for k in range(PER_CLASS):
        rows.append({"label": c["id"], "label_ar": c["label"]["ar"], "what": c.get("description", ""),
                     "lang": pick(LANGS), "length": pick(LENGTHS), "voice": random.choice(VOICES), "hint": pick(HINTS),
                     "values": random.choice(["no date or amount", "one amount", "one date and one amount", "two dates and an amount"])})
for k in range(240):
    rows.append({"label": "other", "label_ar": "غير ذلك", "what": OTHER[k % len(OTHER)],
                 "lang": pick(LANGS), "length": pick(LENGTHS), "voice": random.choice(VOICES), "hint": pick(HINTS),
                 "values": random.choice(["no date or amount", "one amount", "one date and one amount"])})
random.shuffle(rows)
for i, r in enumerate(rows):
    r["id"] = f"t{i+1:04d}"
N = 4
for a in range(N):
    part = rows[a::N]
    with open(os.path.join(OUT, f"plan-{a+1}.jsonl"), "w", encoding="utf-8") as f:
        for r in part:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print("plan", a + 1, len(part))
print("total", len(rows), "classes", len(claims) + 1)
