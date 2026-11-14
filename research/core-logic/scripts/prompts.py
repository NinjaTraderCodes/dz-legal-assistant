"""Filling the agent and Codex prompts of step 1 (docs/research/core-logic/prompts/), so that every run of a role gets the
same inputs built the same way. Each prompt file holds its rules above «--- PROMPT ---» and the text given to the agent
below it; every «{{name}}» must be filled, and a prompt with a placeholder left is refused.

    python prompts.py fill rules-derive-a --unit formal-notice --out <file>
    python prompts.py fill rules-derive-c-codex --unit formal-notice --out <file>
    python prompts.py fill rules-boundary-cases --unit formal-notice --out <file>

Sources: the handbook's module table (section 6), slots.json, FactVocabulary in src/DzLegal.Drafting/CaseFacts.cs, the
unit's committed rules/reports/<unit>.texts.json, the French index in the working folder. Nothing here is legal content:
the script copies, it never writes a word of law.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import store as st  # noqa: E402

CL = os.path.join(st.REPO, "docs", "research", "core-logic")
PROMPTS = os.path.join(CL, "prompts")
WORK = r"C:\Users\TheShadow\dz-core-research"
STORE_JSON = os.path.join(WORK, "store", "articles.json")
STORE_TEXT = os.path.join(WORK, "store", "text")
RULES_TOOL = os.path.join(st.REPO, "research", "core-logic", "src", "DzLegal.CoreLogic.Rules.Tool", "bin", "Release",
                          "net10.0", "rules.exe")
RUNS = os.path.join(WORK, "runs", "step1")
PYTHON = r"C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe"
MARK = "--- PROMPT ---"
PLACEHOLDER = re.compile(r"\{\{(\w+)\}\}")
LABOUR_UNITS = {"labour-prelitigation", "abusive-dismissal", "unpaid-wages"}
SEED_ORIGINS = {"labour-prelitigation", "unpaid-wages"}

# The French texts for derivation B, by module (french/INDEX.md in the working folder lists what exists). A unit not
# listed here cannot be filled for B until its French chapter is prepared.
FRENCH = {
    "formal-notice": [
        ("Civil Code (Ordinance 75-58), French Official Journal 78 of 1975, pages 6 to 10 of the PDF, images: the chapters "
         "on the effects of obligations and the formal notice (art. 175 is on pages 8 and 9, arts. 180 and 181 on pages 9 "
         "and 10)", [os.path.join(WORK, "french", "civil-code", f"F1975078-p{p:03d}-300.png") for p in range(6, 11)]),
        ("Law 05-10 of 2005 amending the Civil Code, French Official Journal 44 of 2005, text",
         [os.path.join(WORK, "french", "civil-code", "F2005044-law-05-10.txt")]),
    ],
}


def template(name: str) -> tuple[str, str]:
    """The prompt file's rules (above the mark) and the text given to the agent (below it)."""
    with open(os.path.join(PROMPTS, name + ".md"), encoding="utf-8") as f:
        text = f.read()
    head, sep, body = text.partition(MARK)
    if not sep:
        raise ValueError(f"{name}: no «{MARK}» line")
    return head, body.strip("\n") + "\n"


def fill(body: str, values: dict[str, str]) -> str:
    missing = sorted({m for m in PLACEHOLDER.findall(body) if m not in values})
    if missing:
        raise KeyError(f"placeholders not filled: {', '.join(missing)}")
    out = PLACEHOLDER.sub(lambda m: values[m.group(1)], body)
    left = PLACEHOLDER.findall(out)
    if left:  # a value that itself held «{{…}}»
        raise ValueError(f"placeholders left after filling: {left}")
    return out


def handbook_modules() -> list[dict]:
    """The handbook's module table (section 6): id, what it must answer, where to start."""
    with open(os.path.join(CL, "HANDBOOK.md"), encoding="utf-8") as f:
        text = f.read()
    rows = []
    for line in text.split("\n"):
        m = re.match(r"^\| `([a-z-]+)` \| (.+?) \| (.+?) \|$", line)
        if m and m.group(1) not in {r["id"] for r in rows}:
            rows.append({"id": m.group(1), "question": m.group(2).strip(), "start": m.group(3).strip()})
    if len(rows) != 12:
        raise ValueError(f"the handbook's module table has {len(rows)} rows, not 12")
    return rows


def module_row(unit: str) -> dict:
    row = next((r for r in handbook_modules() if r["id"] == unit), None)
    if row is None:
        raise KeyError(f"{unit} is not a module of the handbook's table")
    return row


def modules_table() -> str:
    return "\n".join(f"- `{r['id']}`: {r['question']}" for r in handbook_modules())


def unit_brief_module(unit: str) -> str:
    r = module_row(unit)
    return (f"- **What it must answer:** {r['question']}\n"
            f"- **Where to start** (pointers to start from, not statements of the law; a «?» marks one from memory): "
            f"{r['start']}")


def slots(unit: str, english_only: bool = False) -> str:
    """The slots as derivers get them: id, type, question, choices; never help, askedFor, askedWhen, origin. The labour
    units do not get the slots the seed list took from the hand-made examples (prompts/rules-derive-a.md)."""
    with open(os.path.join(CL, "slots.json"), encoding="utf-8") as f:
        doc = json.load(f)
    lines = []
    for s in doc["slots"]:
        if unit in LABOUR_UNITS and s.get("origin") in SEED_ORIGINS:
            continue
        q = s["question"]["en"] if english_only else f"{s['question']['ar']} / {s['question']['en']}"
        line = f"- `{s['id']}` ({s['type']}): {q}"
        if s.get("choices"):
            line += " Choices: " + "; ".join(
                f"`{c['id']}` ({c['label']['en'] if english_only else c['label']['ar'] + ' / ' + c['label']['en']})"
                for c in s["choices"])
        lines.append(line)
    return "\n".join(lines)


def vocabulary() -> str:
    with open(os.path.join(st.REPO, "src", "DzLegal.Drafting", "CaseFacts.cs"), encoding="utf-8") as f:
        text = f.read()
    out = []
    for name in ("Events", "Requests"):
        m = re.search(r"IReadOnlyList<string> " + name + r"\s*=\s*\[(.*?)\];", text, re.S)
        if not m:
            raise ValueError(f"FactVocabulary.{name} not found")
        kinds = re.findall(r'"([a-z_]+)"', m.group(1))
        out.append(f"- **{'Event' if name == 'Events' else 'Request'} kinds:** " + ", ".join(f"`{k}`" for k in kinds))
    return "\n".join(out)


def request_kinds() -> str:
    """The request kinds alone (the values of the reserved `requests` slot), for the boundary writer."""
    line = vocabulary().splitlines()[1]
    return line.split(":** ", 1)[1]


def unit_texts(unit: str) -> dict:
    with open(os.path.join(CL, "rules", "reports", f"{unit}.texts.json"), encoding="utf-8") as f:
        return json.load(f)


def french_sources(unit: str) -> str:
    if unit not in FRENCH:
        raise KeyError(f"no French texts prepared for {unit}: add them to french/INDEX.md and to FRENCH first")
    lines = []
    for what, paths in FRENCH[unit]:
        for p in paths:
            if not os.path.isfile(p):
                raise FileNotFoundError(p)
        lines.append(f"- {what}: " + ", ".join(f"`{p}`" for p in paths))
    return "\n" + "\n".join(lines)


def _run_file(unit: str, name: str) -> str:
    """A text the stage-7 script wrote for this unit (stage7.py render / pairs), in the working folder."""
    path = os.path.join(RUNS, unit, name)
    if not os.path.isfile(path):
        raise FileNotFoundError(f"{path}: write it with stage7.py first")
    with open(path, encoding="utf-8") as f:
        return f.read().strip("\n")


def values_for(prompt: str, unit: str) -> dict[str, str]:
    base = {"worktree": st.REPO, "unit_id": unit, "module_id": unit, "unit_kind": "module",
            "store_path": STORE_JSON, "store_text": STORE_TEXT, "rules_tool": RULES_TOOL}
    if prompt in ("rules-derive-a", "rules-derive-b"):
        base.update(unit_brief=unit_brief_module(unit), modules_table=modules_table(), slots=slots(unit),
                    vocabulary=vocabulary())
        if prompt == "rules-derive-b":
            base["french_sources"] = french_sources(unit)
    elif prompt == "rules-derive-c-codex":
        r = module_row(unit)
        base.update(module_question=f"{r['question']} Where to start (pointers, not statements of the law): {r['start']}",
                    slots=slots(unit, english_only=True), texts=unit_texts(unit)["rendered"])
    elif prompt == "rules-boundary-cases":
        base.update(units=f"- `{unit}`: {module_row(unit)['question']}", slots=slots(unit), request_kinds=request_kinds(),
                    texts=f"### {unit}\n\n" + unit_texts(unit)["rendered"])
    elif prompt == "rules-reconcile":
        base["module_finals"] = "none"  # a module imports nothing; claim records get their modules' final paths
    elif prompt == "rules-adversary":
        ledger = os.path.join(CL, "law", "amendment-ledger.json")
        base.update(record_path=os.path.join(CL, "rules", "reconciled", "quoted", f"{unit}.json"),
                    ledger_path=ledger if os.path.isfile(ledger) else "(none yet: the amendment ledger does not exist)",
                    oj_fetch=f"{PYTHON} {os.path.join(os.path.dirname(os.path.abspath(__file__)), 'oj_fetch.py')}")
    elif prompt == "rules-attack-codex":
        base.update(unit_label=f"module {unit}: {module_row(unit)['question']}", slots=slots(unit, english_only=True),
                    record_text=_run_file(unit, "record_text.txt"))
    elif prompt == "rules-support-check-codex":
        base["pairs"] = _run_file(unit, "support-pairs.txt")
    else:
        raise KeyError(f"no filling rule for {prompt}")
    return base


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="prompts")
    sub = p.add_subparsers(dest="cmd", required=True)
    f = sub.add_parser("fill")
    f.add_argument("prompt")
    f.add_argument("--unit", required=True)
    f.add_argument("--out", required=True)
    a = p.parse_args(argv)
    if a.cmd == "fill":
        _, body = template(a.prompt)
        text = fill(body, values_for(a.prompt, a.unit))
        with open(a.out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(a.out, len(text), "characters")
    return 0


if __name__ == "__main__":
    sys.exit(main())
