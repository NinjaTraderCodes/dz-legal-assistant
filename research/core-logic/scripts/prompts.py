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

# The French texts for derivation B, by unit: <CL>\law\french-sources.json (stage 0b of specs/unit-driver.md).
FRENCH_SOURCES = os.path.join(CL, "law", "french-sources.json")

# The outcome words of a boundary case, as the body of boundary-second-view-codex.md gives them (a test holds the two
# word for word).
OUTCOME_MEANINGS = {
    "inTime": "the time limit has not passed on the petition date",
    "late": "the time limit has passed",
    "lastDay": "the petition date is the last day of the time limit",
    "stepMissing": "a step required before filing was not done, and the rule must say so",
    "stepDone": "the step was done as the texts require",
    "stepExempt": "the texts exempt this case from the step",
    "leavesOrdinaryCourt": "the case does not belong before the ordinary court",
    "ask": "the answer depends on a fact that is unknown, and the rule must ask the lawyer for it",
    "unsettled": "the texts do not settle the outcome, and the rule must warn without deciding",
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


def claims() -> dict:
    """The claim types of the catalogue: id -> (area, the catalogue entry)."""
    out = {}
    folder = os.path.join(st.REPO, "data", "law", "claims")
    for name in sorted(os.listdir(folder)):
        with open(os.path.join(folder, name), encoding="utf-8") as f:
            doc = json.load(f)
        for c in doc["claims"]:
            out[c["id"]] = (doc["area"], c)
    return out


def claim_elements(unit: str) -> dict:
    with open(os.path.join(st.REPO, "data", "law", "claim-elements.json"), encoding="utf-8") as f:
        doc = json.load(f)
    row = next((c for c in doc["claims"] if c["claim"] == unit), None)
    if row is None:
        raise KeyError(f"{unit} has no row in claim-elements.json")
    return row


def unit_kind(unit: str) -> str:
    """"module" or "claim", the record's own `kind` (rule-record.schema.json)."""
    if any(r["id"] == unit for r in handbook_modules()):
        return "module"
    if unit in claims():
        return "claim"
    raise KeyError(f"{unit} is neither a module of the handbook's table nor a claim type of the catalogue")


def unit_brief_claim(unit: str) -> str:
    """The claim as the catalogue and the elements table describe it, never the catalogue's articles, prerequisites,
    documents or suggested requests: those become checklist questions (from: catalogue) in the merged checklist."""
    area, c = claims()[unit]
    el = claim_elements(unit)
    lines = [f"- **The claim:** `{unit}`, {c['label']['ar']} / {c['label']['en']} (area: {area}).",
             f"- **What the lawyer brings:** {c['description']}"]
    if el.get("roles"):
        lines.append("- **Who the plaintiff is:** " + "; ".join(
            f"{r['label']} (relation `{r['relation']}`, position `{r['position']}`)" for r in el["roles"]))
    lines.append("- **The elements** (what must be proved, each an event kind of the vocabulary):")
    for e in el["elements"]:
        extra = [f"by the {e['actor']}"] if e.get("actor") else []
        extra += ["with a date"] if e.get("date") else []
        extra += ["with an amount"] if e.get("amount") else []
        lines.append(f"  - `{e['event']}`" + (f" ({', '.join(extra)})" if extra else "") + f": {e['label']}")
    lines.append("- **The requests** (request kinds of the vocabulary):")
    for q in el.get("requests", []):
        lines.append(f"  - `{q['kind']}`" + (" (with an amount)" if q.get("amount") else "") + f": {q['label']}")
    return "\n".join(lines)


def unit_brief(unit: str) -> str:
    return unit_brief_module(unit) if unit_kind(unit) == "module" else unit_brief_claim(unit)


def unit_line(unit: str) -> str:
    """One line naming the unit and its subject (`{{units}}`)."""
    if unit_kind(unit) == "module":
        return f"- `{unit}`: {module_row(unit)['question']}"
    area, c = claims()[unit]
    return f"- `{unit}`: claim type, {c['label']['en']} ({c['label']['ar']}): {c['description']}"


def unit_label(unit: str) -> str:
    if unit_kind(unit) == "module":
        return f"module {unit}: {module_row(unit)['question']}"
    area, c = claims()[unit]
    return f"claim type {unit}: {c['label']['en']} ({c['label']['ar']}): {c['description']}"


def subject(unit: str) -> str:
    """What disputed boundary cases are about, for the second view: the subject, never an answer."""
    if unit_kind(unit) == "module":
        q = module_row(unit)["question"].rstrip(".")
        return f"this subject: {q[0].lower()}{q[1:]}"
    area, c = claims()[unit]
    return f"this claim: {c['label']['en']} ({c['description'].rstrip('.')})"


def _records_of(unit: str) -> list[dict]:
    """The records of a unit written so far (A, B, reconciled, final candidate), to see whether it has a deadline."""
    out = []
    for sub_ in (("derivations", "a"), ("derivations", "b"), ("reconciled",), ("final-candidates",)):
        path = os.path.join(CL, "rules", *sub_, f"{unit}.json")
        if os.path.isfile(path):
            with open(path, encoding="utf-8") as f:
                out.append(json.load(f))
    return out


def has_deadline(unit: str) -> bool:
    return any(r.get("deadlines") for r in _records_of(unit))


def module_finals(units: list[str], strict: bool = False) -> str:
    """The final module records a reconciler or a verifier may read (stage 5): for a claim every final module (it
    imports or may import any); for a unit with a deadline, other than periods itself, final/periods.json. With
    `strict` (the reconciler), a claim whose derivations import a module that is not final, or a unit with a deadline
    while periods is not final, is refused: stage 5 waits for them."""
    final = os.path.join(CL, "rules", "final")
    paths = []
    for u in units:
        if unit_kind(u) == "claim":
            finals = [r["id"] for r in handbook_modules() if os.path.isfile(os.path.join(final, f"{r['id']}.json"))]
            if strict:
                imported = {i["module"] for r in _records_of(u) for i in r.get("imports", [])}
                missing = sorted(imported - set(finals))
                if missing:
                    raise ValueError(f"{u} imports modules that are not final: {', '.join(missing)}")
            paths += [os.path.join(final, f"{m}.json") for m in finals]
        if u != "periods" and has_deadline(u):
            periods = os.path.join(final, "periods.json")
            if not os.path.isfile(periods):
                if strict:
                    raise ValueError(f"{u} has a deadline and periods is not final: stage 5 waits for it")
                continue
            paths.append(periods)
    paths = list(dict.fromkeys(paths))
    return "\n" + "\n".join(f"- `{x}`" for x in paths) if paths else "none"


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
    """The French texts of stage 0b. A unit stage 0b has not reached is refused; an instrument for which no French
    text could be prepared becomes a sentence for the deriver (the filler never throws for a missing chapter)."""
    with open(FRENCH_SOURCES, encoding="utf-8") as f:
        units = json.load(f)["units"]
    if unit not in units:
        raise KeyError(f"stage 0b has not run for {unit}: law/french-sources.json has no entry")
    lines = []
    for e in units[unit]:
        if not e.get("paths"):
            lines.append(f"- No French text was prepared for {e['what']}: say so in your checklist.")
            continue
        paths = [os.path.join(WORK, *x.split("/")) for x in e["paths"]]
        for x in paths:
            if not os.path.isfile(x):
                raise FileNotFoundError(x)
        lines.append(f"- {e['what']}: " + ", ".join(f"`{x}`" for x in paths))
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


def _slot_fact(slot_id: str, value, slots_by_id: dict) -> str:
    if slot_id == "requests":
        return "the petition asks for: " + ", ".join(f"`{k}`" for k in value)
    s = slots_by_id.get(slot_id)
    if s is None:
        return f"`{slot_id}`: {value}"
    shown = value
    for c in s.get("choices") or []:
        if c["id"] == value:
            shown = c["label"]["en"]
    return f"{s['question']['en']} {shown}"


def second_view(unit: str, case_ids: list[str], run: list[dict]) -> dict[str, str]:
    """The placeholders of boundary-second-view-codex (stage 8b): the cases without their `why` or expected outcome,
    the two words in dispute for each (expected and actual, alphabetical), and the texts with every paragraph that an
    item matched by a case cites and the unit's texts lack."""
    with open(os.path.join(CL, "rules", "boundary-cases", f"{unit}.json"), encoding="utf-8") as f:
        cases = {c["id"]: c for c in json.load(f)}
    actual = {r["caseId"]: r.get("actual") for r in run}
    with open(os.path.join(CL, "slots.json"), encoding="utf-8") as f:
        slots_by_id = {s["id"]: s for s in json.load(f)["slots"]}
    with open(os.path.join(CL, "rules", "final-candidates", "quoted", f"{unit}.json"), encoding="utf-8") as f:
        record = json.load(f)
    import records as rc
    import stage7
    stmts = rc.statements(record)
    words, case_lines, cited = set(), [], []
    for cid in case_ids:
        c = cases[cid]
        expected, got = c["expect"]["outcome"], actual.get(cid)
        if got is None:
            raise ValueError(f"{cid}: the run has no `actual` outcome (run the published tool with --json)")
        if got == expected:
            raise ValueError(f"{cid}: expected and actual are both {got}: not a choice of words (a last-day "
                             "difference goes to the owner's question batch)")
        words |= {expected, got}
        facts = [f"the petition is dated {c['petitionDate']}"] + [_slot_fact(k, v, slots_by_id)
                                                                   for k, v in c["slots"].items()]
        case_lines.append(f"- **{cid}** ({c['title']}): " + "; ".join(facts) + ". Nothing else is known about the case.")
        m = re.match(r"\[(\S+) (.+?) ¶(\d+)\]", c["expect"].get("about", ""))
        if m:
            code, article, para = m.group(1), m.group(2), int(m.group(3))
            hit = {i for i, s in stmts.items() if s["cite"]["code"] == code and s["cite"]["article"] == article
                   and any(q["paragraph"] == para for q in s.get("quote", []))}
            for _, x in rc.items(record):
                ids = rc.basis_ids(x) if "basis" in x or "counting" in x else [x.get("id")]
                if hit & set(ids):
                    cited += ids
    rendered = unit_texts(unit)["rendered"]
    further = [t_ for t_ in stage7._texts(stmts, list(dict.fromkeys(cited)))
               if stage7._label(t_) not in rendered]
    texts = rendered + ("\n\n(Further paragraphs of the same texts:)\n" +
                        "\n".join(f"{stage7._label(t_)} {t_['text']}" for t_ in further) if further else "")
    choices = "\n".join(f"- \"{w}\": {OUTCOME_MEANINGS[w]}." for w in sorted(words))
    return {"subject": subject(unit), "texts": texts, "cases": "\n".join(case_lines), "choices": choices}


def _run_file(unit: str, name: str) -> str:
    """A text the stage-7 script wrote for this unit (stage7.py render / pairs), in the working folder."""
    path = os.path.join(RUNS, unit, name)
    if not os.path.isfile(path):
        raise FileNotFoundError(f"{path}: write it with stage7.py first")
    with open(path, encoding="utf-8") as f:
        return f.read().strip("\n")


def values_for(prompt: str, unit: str, followup: int = 0, cases: list[str] | None = None,
               run: list[dict] | None = None) -> dict[str, str]:
    kind = unit_kind(unit.split("+")[0])
    base = {"worktree": st.REPO, "unit_id": unit, "module_id": unit, "unit_kind": kind,
            "store_path": STORE_JSON, "store_text": STORE_TEXT, "rules_tool": RULES_TOOL}
    if prompt in ("rules-derive-a", "rules-derive-b"):
        base.update(unit_brief=unit_brief(unit), modules_table=modules_table(), slots=slots(unit),
                    vocabulary=vocabulary())
        if prompt == "rules-derive-b":
            base["french_sources"] = french_sources(unit)
    elif prompt == "rules-derive-c":
        if kind != "module":
            raise ValueError("derivation C is for modules only (HANDBOOK, stage 2c)")
        r = module_row(unit)
        base.update(module_question=f"{r['question']} Where to start (pointers, not statements of the law): {r['start']}",
                    slots=slots(unit, english_only=True), texts=unit_texts(unit)["rendered"])
    elif prompt == "rules-boundary-cases":
        base.update(units=unit_line(unit), slots=slots(unit), request_kinds=request_kinds(),
                    texts=f"### {unit}\n\n" + unit_texts(unit)["rendered"])
    elif prompt == "rules-reconcile":
        base["module_finals"] = module_finals([unit], strict=True)
    elif prompt == "rules-adversary":
        ledger = os.path.join(CL, "law", "amendment-ledger.json")
        base.update(record_path=os.path.join(CL, "rules", "reconciled", "quoted", f"{unit}.json"),
                    ledger_path=ledger if os.path.isfile(ledger) else "(none yet: the amendment ledger does not exist)",
                    oj_fetch=oj_fetch_command(unit, 'adversary'), leads=leads_for(unit))
    elif prompt == "rules-attack":
        base.update(unit_label=unit_label(unit), slots=slots(unit, english_only=True),
                    record_text=_run_file(unit, "record_text.txt"))
    elif prompt == "rules-support-check-codex":
        base["pairs"] = _run_file(unit, "support-pairs.txt")
    elif prompt == "boundary-second-view-codex":
        if not cases or run is None:
            raise ValueError("the second view needs --cases and --run (the boundary run printed with --json)")
        base.update(second_view(unit, cases, run))
    elif prompt == "rules-verify":
        # one unit, or two joined by "+" (the verifier takes units two by two, modules first)
        units = unit.split("+")
        rules_dir, reports = os.path.join(CL, "rules"), os.path.join(CL, "rules", "reports")

        def listing(paths_of):
            def one(p):  # a path, or (path, note): the note stays outside the backticks
                return f"`{p[0]}` {p[1]}" if isinstance(p, tuple) else f"`{p}`"
            return "\n" + "\n".join(f"- `{u}`: " + ", ".join(one(p) for p in paths_of(u)) for u in units)

        def attacks_of(u):
            gathered = os.path.join(reports, f"{u}.attacks.input.json")  # unit.py verify-input (stage 8)
            if os.path.isfile(gathered):
                return [gathered]
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
                        oj_fetch=oj_fetch_command(u, 'verify'), module_finals=module_finals([u]))
            return base
        base.update(units=", ".join(f"`{u}`" for u in units),
                    record_paths=listing(lambda u: [os.path.join(rules_dir, "reconciled", "quoted", f"{u}.json")]),
                    attack_paths=listing(attacks_of),
                    reconciliation_paths=listing(lambda u: [os.path.join(reports, f"{u}.reconciliation.md")]),
                    register_paths=listing(lambda u: [os.path.join(reports, f"{u}.attacks.json")]),
                    candidate_paths=listing(lambda u: [os.path.join(rules_dir, "final-candidates", f"{u}.json")]),
                    requests_path=os.path.join(CL, "law", "requests.jsonl"),
                    ledger_path=ledger if os.path.isfile(ledger) else "(none yet: the amendment ledger does not exist)",
                    oj_fetch=oj_fetch_command(unit, 'verify'), module_finals=module_finals(units))
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
    f.add_argument("--cases", help="boundary-second-view-codex only: the case ids, comma-separated")
    f.add_argument("--run", help="boundary-second-view-codex only: the boundary run printed with --json")
    a = p.parse_args(argv)
    if a.cmd == "fill":
        if unit.published_tool() is None:
            print("the rules tool is not published: run unit.py tool first", file=sys.stderr)
            return 1
        _, body = template(a.prompt)
        run = None
        if a.run:
            with open(a.run, encoding="utf-8-sig") as fh:
                run = json.load(fh)
        text = fill(body, values_for(a.prompt, a.unit, a.followup, a.cases.split(",") if a.cases else None, run))
        with open(a.out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print(a.out, len(text), "characters")
    return 0


if __name__ == "__main__":
    sys.exit(main())
