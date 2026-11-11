"""Tokens per Arabic word for each tokenizer, on the fictitious facts of the repository's 46 development cases
and its 12 drafting cases (formal Arabic and a little Darija). Only tokenizer files are downloaded."""
import json, glob, os, re, sys
from transformers import AutoTokenizer
REPO = r"C:\Users\TheShadow\dz-fable-arch"
texts = [c["facts"] for c in json.load(open(os.path.join(REPO, "tests", "grounds-cases", "cases.json"), encoding="utf-8-sig"))["cases"]]
for f in glob.glob(os.path.join(REPO, "tests", "drafting-cases*", "*.json")):
    d = json.load(open(f, encoding="utf-8-sig"))
    t = (d.get("inputs") or d).get("notes.facts")
    if t:
        texts.append(t)
texts = [t for t in texts if re.search(r"[\u0600-\u06FF]", t)]
words = sum(len(t.split()) for t in texts)
chars = sum(len(t) for t in texts)
print(f"sample: {len(texts)} notes, {words} whitespace words, {chars} characters")
MODELS = [
    ("gemma4:e4b (reference)", "unsloth/gemma-4-E4B-it"), ("gemma3:4b", "unsloth/gemma-3-4b-it"),
    ("Jais-3-0.9B", "inception42/Jais-3-0.9B"), ("Jais-3-3.7B", "inception42/Jais-3-3.7B"),
    ("Falcon-H1-1.5B-Deep", "tiiuae/Falcon-H1-1.5B-Deep-Instruct"), ("jais-family-590m", "inception42/jais-family-590m-chat"),
    ("Qwen3.5-2B", "Qwen/Qwen3.5-2B"),
    ("mmBERT-small", "jhu-clsp/mmBERT-small"), ("CAMeLBERT-mix", "CAMeL-Lab/bert-base-arabic-camelbert-mix"),
    ("DziriBERT", "alger-ia/dziribert"), ("multilingual-e5-small", "intfloat/multilingual-e5-small"),
    ("Multilingual-MiniLM-L12", "microsoft/Multilingual-MiniLM-L12-H384"),
]
out = []
for name, repo in MODELS:
    try:
        tok = AutoTokenizer.from_pretrained(repo, trust_remote_code=False)
        n = sum(len(tok(t, add_special_tokens=False)["input_ids"]) for t in texts)
        out.append({"model": name, "repo": repo, "tokens": n, "tokens_per_word": round(n / words, 3), "vocab": tok.vocab_size})
        print(f"{name}: {n} tokens, {n / words:.2f} per word, vocabulary {tok.vocab_size}")
    except Exception as e:
        print(f"{name}: FAILED {type(e).__name__}: {str(e)[:120]}")
json.dump({"notes": len(texts), "words": words, "results": out}, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "runs", "tokens-per-word.json"), "w"), indent=1)
