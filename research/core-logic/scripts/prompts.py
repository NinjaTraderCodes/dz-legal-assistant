"""Filling the agent and Codex prompts of step 1 (docs/research/core-logic/prompts/), so that every run of a role gets the
same inputs built the same way. Each prompt file holds its rules above «--- PROMPT ---» and the text given to the agent
below it; every «{{name}}» must be filled, and a prompt with a placeholder left is refused.

    python prompts.py fill rules-derive-a --unit formal-notice --out <file>
    python prompts.py fill rules-derive-c --unit formal-notice --out <file>
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
import unit  # noqa: E402

# Agents run the published copy of the rules tool (unit.py tool), never bin\Release: a build replaces that one while
# agents run it. The fallback only keeps the filler importable in a clone without the working folder; `fill` refuses it.
RULES_TOOL = unit.published_tool() or "<the rules tool is not published: run unit.py tool>"
RUNS = os.path.join(WORK, "runs", "step1")
PYTHON = r"C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe"
MARK = "--- PROMPT ---"
PLACEHOLDER = re.compile(r"\{\{(\w+)\}\}")
LABOUR_UNITS = {"labour-prelitigation", "abusive-dismissal", "unpaid-wages"}
# The slots the seed list took from the hand-made labour examples before any derivation: hidden from the labour units'
# derivers by id (never by origin: a slot a final module adds has that module as its origin and is shown to everyone).
SEED_SLOTS = {"wages_due_from", "defendant_resides_abroad", "employer_in_bankruptcy_or_settlement",
              "worker_is_public_servant", "work_accident_or_occupational_illness"}

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
        m = re.match(r"^\| `([a-z-]+)`(?: \((?:high-harm|done)\))? \| (.+?) \| (.+?) \|$", line)
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
        if unit in LABOUR_UNITS and s["id"] in SEED_SLOTS:
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


def oj_fetch_command(unit: str, role: str) -> str:
    """The one door to joradp.dz, complete but for the URL (oj_fetch.py: --caller, --wait <seconds>, --out). The
    first unit's prompts gave only the script and "--wait", and the adversary had to find the arguments itself."""
    out = os.path.join(RUNS, unit.replace("+", "-"), f"oj-{role}")
    script = os.path.join(os.path.dirname(os.path.abspath(__file__)), "oj_fetch.py")
    return f"{PYTHON} {script} --caller {role}-{unit} --wait 600 --out {out}"


FAMILY_UNITS = {"divorce-by-husband", "tatliq", "khul", "nafaqa", "custody", "custody-forfeiture", "visitation",
                "conciliation-mediation"}
LEAD_GROUPS = {"labour modules": LABOUR_UNITS, "family modules": FAMILY_UNITS}


def leads_for(unit: str) -> str:
    """The triage leads recorded for this unit (law/journal-scan/triage-other-wave.json, `leads`), one per line, or
    "none". A lead is addressed to a unit by its id or to a group of units ("labour modules", "family modules"). The
    untriaged hits of the instruments a record cites are added by the driver (specs/unit-driver.md, stage 7a)."""
    path = os.path.join(CL, "law", "journal-scan", "triage-other-wave.json")
    if not os.path.isfile(path):
        return "none"
    with open(path, encoding="utf-8") as f:
        leads = json.load(f).get("leads", [])
    mine = [x for x in leads if x.get("for") == unit or unit in LEAD_GROUPS.get(x.get("for", ""), ())]
    if not mine:
        return "none"
    return "\n".join(f"- Official Journal issue {x['issue']}: {x['text']}. {x['what']}" for x in mine)


def _run_file(unit: str, name: str) -> str:
    """A text the stage-7 script wrote for this unit (stage7.py render / pairs), in the working folder."""
    path = os.path.join(RUNS, unit, name)
    if not os.path.isfile(path):
        raise FileNotFoundError(f"{path}: write it with stage7.py first")
    with open(path, encoding="utf-8") as f:
        return f.read().strip("\n")


def values_for(prompt: str, unit: str, followup: int = 0) -> dict[str, str]:
    base = {"worktree": st.REPO, "unit_id": unit, "module_id": unit, "unit_kind": "module",
            "store_path": STORE_JSON, "store_text": STORE_TEXT, "rules_tool": RULES_TOOL}
    if prompt in ("rules-derive-a", "rules-derive-b"):
        base.update(unit_brief=unit_brief_module(unit), modules_table=modules_table(), slots=slots(unit),
                    vocabulary=vocabulary())
        if prompt == "rules-derive-b":
            base["french_sources"] = french_sources(unit)
    elif prompt == "rules-derive-c":
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
                    oj_fetch=oj_fetch_command(unit, 'adversary'), leads=leads_for(unit))
    elif prompt == "rules-attack":
        base.update(unit_label=f"module {unit}: {module_row(unit)['question']}", slots=slots(unit, english_only=True),
                    record_text=_run_file(unit, "record_text.txt"))
    elif prompt == "rules-support-check-codex":
        base["pairs"] = _run_file(unit, "support-pairs.txt")
    elif prompt == "rules-verify":
        # one unit, or two joined by "+" (the verifier takes units two by two, modules first)
        units = unit.split("+")
        rules_dir, reports = os.path.join(CL, "rules"), os.path.join(CL, "rules", "reports")

        def listing(paths_of):
            def one(p):  # a path, or (path, note): the note stays outside the backticks
                return f"`{p[0]}` {p[1]}" if isinstance(p, tuple) else f"`{p}`"
            return "\n" + "\n".join(f"- `{u}`: " + ", ".join(one(p) for p in paths_of(u)) for u in units)

        def attacks_of(u):
            attack = os.path.join(reports, f"{u}.attacks.sonnet.json")
            if not os.path.isfile(attack) and os.path.isfile(os.path.join(reports, f"{u}.attacks.codex.json")):
                attack = os.path.join(reports, f"{u}.attacks.codex.json")  # the first module's attack was Codex's
            paths = [os.path.join(reports, f"{u}.attacks.adversary.json"), attack,
                     (os.path.join(reports, f"{u}.support.json"), "(its `attacks` list: the \"no\" and \"partly\" answers)")]
            if os.path.isfile(os.path.join(reports, f"{u}.c-comparison.json")):
                paths.append((os.path.join(reports, f"{u}.c-comparison.json"), "(its `attacks` list only)"))
            return paths

        def register(u, k):  # the register of the first run (k = 0) or of follow-up k; follow-up 1 was named without "1"
            return os.path.join(reports, f"{u}.attacks.json" if k == 0 else
                                f"{u}.attacks.followup.json" if k == 1 else f"{u}.attacks.followup{k}.json")

        ledger = os.path.join(CL, "law", "amendment-ledger.json")
        if followup:  # specs/unit-driver.md, "Follow-up verifier runs"
            if len(units) != 1:
                raise ValueError("a follow-up is for one unit")
            u = units[0]
            earlier = [register(u, k) for k in range(followup) if os.path.isfile(register(u, k))]
            base.update(units=f"`{u}`",
                        record_paths=listing(lambda u: [(os.path.join(rules_dir, "final-candidates", "quoted", f"{u}.json"),
                                                         "(the record as the earlier verification runs left it, with "
                                                         "its quotes)")]),
                        attack_paths=listing(lambda u: [(os.path.join(reports, f"{u}.attacks.followup{followup}-input.json"),
                                                         "(only these entries are to be decided now; the earlier "
                                                         "registers " + " and ".join(f"`{e}`" for e in earlier) +
                                                         " may be read for what was already decided, and are not to be "
                                                         "edited)")]),
                        reconciliation_paths=listing(lambda u: [os.path.join(reports, f"{u}.reconciliation.md")]),
                        register_paths=listing(lambda u: [register(u, followup)]),
                        candidate_paths=listing(lambda u: [(os.path.join(rules_dir, "final-candidates", f"{u}.json"),
                                                            "(the corrected record replaces it)")]),
                        requests_path=os.path.join(CL, "law", "requests.jsonl"),
                        ledger_path=ledger if os.path.isfile(ledger) else "(none yet: the amendment ledger does not exist)",
                        oj_fetch=oj_fetch_command(u, 'verify'))
            return base
        base.update(units=", ".join(f"`{u}`" for u in units),
                    record_paths=listing(lambda u: [os.path.join(rules_dir, "reconciled", "quoted", f"{u}.json")]),
                    attack_paths=listing(attacks_of),
                    reconciliation_paths=listing(lambda u: [os.path.join(reports, f"{u}.reconciliation.md")]),
                    register_paths=listing(lambda u: [os.path.join(reports, f"{u}.attacks.json")]),
                    candidate_paths=listing(lambda u: [os.path.join(rules_dir, "final-candidates", f"{u}.json")]),
                    requests_path=os.path.join(CL, "law", "requests.jsonl"),
                    ledger_path=ledger if os.path.isfile(ledger) else "(none yet: the amendment ledger does not exist)",
                    oj_fetch=oj_fetch_command(unit, 'verify'))
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
    f.add_argument("--followup", type=int, default=0, help="rules-verify only: the number of the follow-up run")
    a = p.parse_args(argv)
    if a.cmd == "fill":
        if unit.published_tool() is None:
            print("the rules tool is not published: run unit.py tool first", file=sys.stderr)
            return 1
        _, body = template(a.prompt)
        text = fill(body, values_for(a.prompt, a.unit, a.followup))
        with open(a.out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(a.out, len(text), "characters")
    return 0


if __name__ == "__main__":
    sys.exit(main())
