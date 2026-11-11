import json, re, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
ROOT = r"C:\Users\TheShadow\dz-fable-arch\data\law"

def load(path):
    raw = open(path, encoding="utf-8-sig").read()
    lines = [l for l in raw.split("\n") if not l.lstrip().startswith("//")]
    return json.loads("\n".join(lines))

def code(name):
    return load(os.path.join(ROOT, "codes", name + ".json"))

def claims():
    out = []
    for f in ["civil", "commercial", "family", "social"]:
        d = load(os.path.join(ROOT, "claims", f + ".json"))
        items = d["claims"] if isinstance(d, dict) and "claims" in d else d
        for c in items:
            c["_file"] = f
            out.append(c)
    return out

if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "keys":
        d = code(sys.argv[2])
        print(type(d), list(d.keys())[:20] if isinstance(d, dict) else len(d))
        arts = d.get("articles", [])
        print(len(arts), json.dumps(arts[0], ensure_ascii=False)[:600])
        for k, v in d.items():
            if k != "articles":
                print(k, "=", json.dumps(v, ensure_ascii=False)[:300])
    elif cmd == "art":
        d = code(sys.argv[2])
        for n in sys.argv[3:]:
            for a in d["articles"]:
                if str(a.get("number")) == n:
                    print("==", sys.argv[2], n, "==")
                    print(json.dumps(a, ensure_ascii=False, indent=1))
    elif cmd == "why":
        want = set(sys.argv[2:])
        for c in claims():
            for a in c.get("articles", []):
                key = a.get("code", "") + ":" + str(a.get("number"))
                if key in want or str(a.get("number")) in want:
                    print(c["id"], "|", key, "|", a.get("role"), "| required" if a.get("required") else "|", "|", json.dumps(a.get("why"), ensure_ascii=False))
    elif cmd == "claim":
        for c in claims():
            if c["id"] in sys.argv[2:]:
                print(json.dumps(c, ensure_ascii=False, indent=1))
    elif cmd == "list":
        for c in claims():
            arts = c.get("articles", [])
            print(c["_file"], "|", c["id"], "|", c.get("label", {}).get("ar"), "| arts", len(arts), "| req", sum(1 for a in arts if a.get("required")), "| verified", c.get("verified"), "| contractual", c.get("contractual"))
