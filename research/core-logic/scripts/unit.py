"""The driver that takes one unit of step 1 through its stages (docs/research/core-logic/specs/unit-driver.md).

    python unit.py status <unit>                       one line per stage: done, due, waiting for <what>, blocked: <why>
    python unit.py next <unit>                         runs the script stages that are due; prints the agent launches
    python unit.py done <unit> <stage> <agent id>      checks an agent stage's outputs and records it
    python unit.py codex <unit>                        queues the unit's due Codex calls (the background queue)
    python unit.py verify-input <unit> [--followup]    the one file the verifier receives (stage 8)
    python unit.py questions <unit>...                 the units' sections of reports/open-legal-questions.md
    python unit.py tool                                publishes the rules tool (<work>\\tools\\rules\\<commit>\\rules.exe)
    python unit.py slots <unit> [--apply <reply>]      stage 9's slot step; --apply writes the Arabic checker's corrections
    python unit.py regrade --all                       rebuilds the store, re-quotes, regrades every final unit
    python unit.py tracking <unit>                     the unit's rows of reports/step-1-tracking.md
    --root <folder> on every command                   rules\\ and runs\\ are taken under that folder (planted rounds)

Every state is read from files: there is no hidden state. Every child process is started through clean_env.

Why a published tool: a build replaces bin\\Release\\rules.exe, and agents run the tool while the executor builds. A
published copy, one folder per commit, is never replaced; units in flight finish on the copy they started with.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import clean_env  # noqa: E402  (importing it removes the keys from this process)

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
WORK = r"C:\Users\TheShadow\dz-core-research"
CL = os.path.join(REPO, "docs", "research", "core-logic")
SCRIPTS = os.path.dirname(os.path.abspath(__file__))
PY = r"C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe"
STORE = os.path.join(WORK, "store", "articles.json")
TOOLS = os.path.join(WORK, "tools", "rules")
LEDGER = os.path.join(WORK, "ledger", "runs.jsonl")
TOOL_PROJECT = os.path.join(REPO, "research", "core-logic", "src", "DzLegal.CoreLogic.Rules.Tool",
                            "DzLegal.CoreLogic.Rules.Tool.csproj")
# What the published tool is built from: a change here that is not committed would be published under a wrong name.
TOOL_SOURCES = ("research/core-logic/src", "research/core-logic/Directory.Build.props",
                "research/core-logic/Directory.Packages.props")
SCHEMAS = os.path.join(CL, "prompts", "schemas")

STAGES = ("0", "0b", "2a", "2b", "2c", "2d", "3", "4", "5", "6", "7a", "7b", "7c", "8", "8b", "9")
# Agent stages: the prompt, the model, and whether the agent has no tool (its reply is copied by ingest-reply).
AGENTS = {"2a": ("rules-derive-a", "opus", False), "2b": ("rules-derive-b", "sonnet", False),
          "2c": ("rules-derive-c", "sonnet", True), "2d": ("rules-boundary-cases", "sonnet", False),
          "5": ("rules-reconcile", "opus", False), "7a": ("rules-adversary", "opus", False),
          "7b": ("rules-attack", "sonnet", True), "8": ("rules-verify", "opus", False)}
CODEX_STAGES = {"7c": "rules-support-check-codex", "8b": "boundary-second-view-codex"}
# The first module was made before the driver: its attack is Codex's and its follow-up 1 files carry no number.
LEGACY = {"formal-notice"}


# --- the published tool (retool item 2) -----------------------------------------------------------------------------
def published_tool(tools: str = TOOLS) -> str | None:
    """The rules.exe that current.txt names, if it exists."""
    pointer = os.path.join(tools, "current.txt")
    if not os.path.isfile(pointer):
        return None
    with open(pointer, encoding="utf-8") as f:
        path = f.read().strip()
    return path if os.path.isfile(path) else None


def ledger_rows(ledger: str = LEDGER) -> dict[str, dict]:
    """The last line of every run id (a run's lines share its id; the last one is its state)."""
    last: dict[str, dict] = {}
    if os.path.isfile(ledger):
        with open(ledger, encoding="utf-8") as f:
            for line in f:
                if line.strip():
                    row = json.loads(line)
                    last[row["id"]] = row
    return last


def running_agents(ledger: str = LEDGER) -> list[str]:
    return [i for i, row in ledger_rows(ledger).items() if row.get("kind") == "agent" and row.get("status") == "running"]


def _git(*args: str) -> str:
    return subprocess.run(["git", "-C", REPO, *args], capture_output=True, text=True, check=True).stdout.strip()


def publish(tools: str = TOOLS, ledger: str = LEDGER) -> str:
    busy = running_agents(ledger)
    if busy:
        raise RuntimeError(f"agent runs still marked running in the ledger: {', '.join(busy)}; publish when none is")
    dirty = _git("status", "--porcelain", "--", *TOOL_SOURCES)
    if dirty:
        raise RuntimeError("the tool's sources have uncommitted changes; commit them first:\n" + dirty)
    commit = _git("rev-parse", "--short=7", "HEAD")
    out = os.path.join(tools, commit)
    exe = os.path.join(out, "rules.exe")
    if not os.path.isfile(exe):
        r = clean_env.run(["dotnet", "publish", TOOL_PROJECT, "-c", "Release", "-o", out], capture_output=True, text=True)
        if r.returncode != 0 or not os.path.isfile(exe):
            raise RuntimeError("dotnet publish failed:\n" + r.stdout[-3000:] + r.stderr[-2000:])
    with open(os.path.join(out, "worktree.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(REPO + "\n")
    with open(os.path.join(tools, "current.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(exe + "\n")
    return exe


# --- files ----------------------------------------------------------------------------------------------------------
def load(path: str):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def save(path: str, value) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(value, ensure_ascii=False, indent=1) + "\n")


def file_sha(path: str) -> str:
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


class Unit:
    """Every path of one unit. With `root`, what is normally under <CL>\\rules\\ and <work>\\runs\\step1\\ is under it."""

    def __init__(self, unit: str, root: str | None = None):
        import prompts
        self.id, self.root = unit, root
        self.kind = prompts.unit_kind(unit)
        self.rules = os.path.join(root, "rules") if root else os.path.join(CL, "rules")
        self.runs = os.path.join(root, "runs") if root else os.path.join(WORK, "runs", "step1")
        self.run = os.path.join(self.runs, unit)
        self.cases = os.path.join(self.run, "cases")
        self.reports = os.path.join(self.rules, "reports")

    def r(self, suffix: str) -> str:
        return os.path.join(self.reports, f"{self.id}.{suffix}")

    def deriv(self, side: str, quoted: bool = False) -> str:
        return os.path.join(self.rules, "derivations", side, *(["quoted"] if quoted else []), f"{self.id}.json")

    def at(self, *parts: str) -> str:
        return os.path.join(self.rules, *parts[:-1], f"{self.id}{parts[-1]}")

    @property
    def reconciled(self):
        return self.at("reconciled", ".json")

    @property
    def reconciled_q(self):
        return self.at("reconciled", "quoted", ".json")

    @property
    def candidate(self):
        return self.at("final-candidates", ".json")

    @property
    def candidate_q(self):
        return self.at("final-candidates", "quoted", ".json")

    @property
    def final(self):
        return self.at("final", ".json")

    @property
    def boundary(self):
        return self.at("boundary-cases", ".json")

    def register(self, k: int) -> str:
        """The register of the first verifier run (k = 0) or of follow-up k. Follow-up 1 of the first module was named
        without its number; every later unit numbers it."""
        if k == 0:
            return self.r("attacks.json")
        if k == 1 and os.path.isfile(self.r("attacks.followup.json")):
            return self.r("attacks.followup.json")
        return self.r(f"attacks.followup{k}.json")

    def followup_input(self, k: int) -> str:
        if k == 1 and os.path.isfile(self.r("attacks.followup-input.json")):
            return self.r("attacks.followup-input.json")
        return self.r(f"attacks.followup{k}-input.json")

    def followups(self) -> int:
        """How many follow-up inputs exist (1, 2, … without a gap)."""
        k = 0
        while os.path.isfile(self.followup_input(k + 1)):
            k += 1
        return k

    def registers(self) -> list[str]:
        out = [self.register(0)] if os.path.isfile(self.register(0)) else []
        return out + [self.register(k) for k in range(1, self.followups() + 1) if os.path.isfile(self.register(k))]


def register_entries(u: Unit) -> list[dict]:
    """Every entry of every register, in order (a later verdict of the same id counts)."""
    out = []
    for path in u.registers():
        doc = load(path)
        out += doc["attacks"] if isinstance(doc, dict) else doc
    return out


def latest(entries: list[dict]) -> dict[str, dict]:
    last = {}
    for e in entries:
        last[e["id"]] = e
    return last


def input_entries(u: Unit) -> list[dict]:
    """Every entry ever given to the verifier: attacks.input.json and the follow-up inputs."""
    out = load(u.r("attacks.input.json"))["attacks"] if os.path.isfile(u.r("attacks.input.json")) else []
    for k in range(1, u.followups() + 1):
        out += load(u.followup_input(k))["attacks"]
    return out


# --- child processes ------------------------------------------------------------------------------------------------
def tool() -> str:
    exe = published_tool()
    if exe is None:
        raise RuntimeError("the rules tool is not published: run unit.py tool")
    return exe


def run(args: list[str], keep: str | None = None) -> tuple[int, str]:
    """A child process without either API key. `keep` saves its standard output (a --json run)."""
    p = clean_env.run(args, capture_output=True, cwd=SCRIPTS)
    out = p.stdout.decode("utf-8", "replace")
    if keep:
        os.makedirs(os.path.dirname(keep), exist_ok=True)
        with open(keep, "w", encoding="utf-8", newline="\n") as f:
            f.write(out)
    return p.returncode, (out + p.stderr.decode("utf-8", "replace")).strip()


def py(*args: str, keep: str | None = None) -> tuple[int, str]:
    return run([PY, "-X", "utf8", *args], keep)


def say(what: str, code: int, out: str, limit: int = 1500) -> None:
    print(f"== {what}: exit {code}")
    if out:
        print(out[-limit:])


# --- the boundary-case file's shape (stage 2d) -----------------------------------------------------------------------
def boundary_problems(cases) -> list[str]:
    import prompts
    if not isinstance(cases, list) or not cases:
        return ["not a non-empty JSON array"]
    out = []
    for n, c in enumerate(cases):
        if not isinstance(c, dict):
            out.append(f"case {n}: not an object")
            continue
        for key in ("id", "title", "petitionDate", "slots", "expect"):
            if key not in c:
                out.append(f"case {n}: no `{key}`")
        e = c.get("expect") or {}
        if e.get("outcome") not in prompts.OUTCOME_MEANINGS:
            out.append(f"case {c.get('id', n)}: outcome {e.get('outcome')!r} is not an outcome word")
        if not re.match(r"^\[\S+ .+ ¶\d+\]$", e.get("about", "")):
            out.append(f"case {c.get('id', n)}: `about` is not a «[code article ¶n]» label")
    return out


# --- what is decided, and what a follow-up must still take ----------------------------------------------------------
def _decided(u: Unit, entry_id: str, target: str | None = None) -> bool:
    """An entry is decided when a register holds it: the same id, or the id a follow-up gave it (<id>-f<k>)."""
    for e in register_entries(u):
        if e["id"].startswith(entry_id + "-f"):  # the id a follow-up gave it is unique by construction
            return True
        if e["id"] == entry_id and (target is None or e.get("target") in (None, target)):
            return True
    return False


def limits(u: Unit) -> dict[str, dict]:
    """reports\\<unit>.boundary-limits.json: cases that fail only by a limit of the evaluator (no official holiday) or
    of the condition language (no comparison of a date slot with the petition date), as a verifier found. They are
    recorded in the tracking file and the question batch; they are not attacks and not sent to Codex."""
    path = u.r("boundary-limits.json")
    return {x["case"]: x for x in load(path)["cases"]} if os.path.isfile(path) else {}


def _case_decided(u: Unit, cid: str) -> bool:
    return any(e.get("who") == "boundaryCase" and (e["id"] in (cid, f"bc-{cid}") or e["id"].startswith(f"bc-{cid}-"))
               for e in register_entries(u))


def _case_entry(c: dict, result: dict, eid: str) -> dict:
    targets = sorted({d.split(":")[0] for d in result["differences"] if ":" in d and not d.startswith("[")})
    return {"id": eid, "who": "boundaryCase", "target": ", ".join(targets) or "record", "kind": "wrongEffect",
            "claim": f"Blind boundary case {c['id']} ({c['title']}; petition {c['petitionDate']}; slots "
                     f"{json.dumps(c['slots'], ensure_ascii=False)}) expects «{c['expect']['outcome']}» about "
                     f"{c['expect']['about']}, because: {c['expect']['why']} The runner says: "
                     + " | ".join(result["differences"])}


def _flag_entry(f: dict) -> dict:
    return {"id": f"flag-{f['id']}", "who": "neighbourFlag", "target": f["statement"], "kind": "exception",
            "claim": f"[{f['code']} {f['article']} ¶{f['paragraph']}] is next to what {f['statement']} cites, opens with "
                     f"«{f['word']}», and the record neither cites it nor says why it does not apply: «{f['text']}»"}


def _support_entries(u: Unit) -> list[dict]:
    if not os.path.isfile(u.r("support.json")):
        return []
    return [dict(a, who="support") for a in load(u.r("support.json")).get("attacks", [])]


def _store_entries(check_output: str) -> list[dict]:
    """The cites `<T> check --store` refused: each problem line under a FAIL line."""
    out, n = [], 0
    for line in check_output.splitlines():
        if line.startswith("      ") and line.strip():
            n += 1
            text = line.strip()
            m = re.match(r"^([a-zA-Z]+\d+)\b", text)
            out.append({"id": f"store-{m.group(1) if m else n}", "who": "store", "target": m.group(1) if m else "record",
                        "kind": "misreading", "claim": f"The rules tool refuses the record against the store: {text}"})
    return out


def _owner_done_entries(results: list[dict]) -> list[dict]:
    """The owner's rule of 14 November 2026: a step may say "done" only when nothing about the same step is open."""
    out, seen = [], set()
    for r in results:
        for d in r.get("differences", []):
            m = re.match(r"^(p\d+): declared none", d)
            if m and "requiredWhen false" not in d and m.group(1) not in seen:
                seen.add(m.group(1))
                out.append({"id": f"done-{m.group(1)}", "who": "owner", "target": m.group(1), "kind": "wrongEffect",
                            "claim": "The owner's rule of 14 November 2026: a step may say \"done\" only when nothing "
                                     f"about the same step is still open. Case {r['caseId']}: {d}"})
    return out


def _values_entries(report: dict) -> list[dict]:
    out = []
    for p in report.get("problems", []):
        item = p.get("item") or p.get("id") or "record"
        out.append({"id": f"values-{item}", "who": "values", "target": item, "kind": "misreading",
                    "claim": "check-values: " + json.dumps(p, ensure_ascii=False)})
    return out


def _results(path: str) -> list[dict]:
    if not os.path.isfile(path):
        return []
    text = open(path, encoding="utf-8-sig").read().strip()
    return json.loads(text) if text.startswith("[") else []


def first_input(u: Unit) -> dict:
    """Stage 8: everything the verifier must decide, gathered from the seven sources at once."""
    attacks = []
    if os.path.isfile(u.r("attacks.adversary.json")):
        attacks += [dict(a, who="adversary") for a in load(u.r("attacks.adversary.json"))["attacks"]]
    sonnet = u.r("attacks.sonnet.json")
    if u.id in LEGACY and not os.path.isfile(sonnet):
        sonnet = u.r("attacks.codex.json")
    if os.path.isfile(sonnet):
        attacks += [dict(a, who="attack") for a in load(sonnet)["attacks"]]
    pending = [] if os.path.isfile(u.r("support.json")) else ["supportCheck"]
    attacks += _support_entries(u)
    if os.path.isfile(u.r("c-comparison.json")):
        attacks += [dict(a, id=a.get("id") or f"c-{n}", who="derivationC")
                    for n, a in enumerate(load(u.r("c-comparison.json")).get("attacks", []), 1)]
    if os.path.isfile(u.r("checklist-check.reconciled.json")):
        flags = {f["id"]: f for path in (u.r("flags.reconciled.json"),) if os.path.isfile(path) for f in load(path)}
        attacks += [_flag_entry(flags[f]) for f in load(u.r("checklist-check.reconciled.json"))["unansweredFlags"]
                    if f in flags]
    results = _results(os.path.join(u.cases, "boundaryReconciled.json"))
    cases = {c["id"]: c for c in load(u.boundary)} if os.path.isfile(u.boundary) else {}
    disputed = set()
    if os.path.isfile(u.r("reconciliation.md")):
        disputed = set(re.findall(r"^disputed (\S+):", open(u.r("reconciliation.md"), encoding="utf-8").read(), re.M))
    for res in results:
        cid = res["caseId"]
        if cid in cases and (not res["passed"] or cid in disputed):
            attacks.append(_case_entry(cases[cid], res, f"bc-{cid}"))
    attacks += _owner_done_entries(results)
    if os.path.isfile(u.r("values.reconciled.json")):
        attacks += _values_entries(load(u.r("values.reconciled.json")))
    check = os.path.join(u.run, "check.reconciled.txt")
    if os.path.isfile(check):
        attacks += _store_entries(open(check, encoding="utf-8").read())
    doc = {"unit": u.id, "attacks": attacks}
    if pending:
        doc["pending"] = pending
    return doc


def uncertain_entries(u: Unit) -> list[dict]:
    """Stage 8b: Codex gave the case writer's outcome, or neither side's: the case is uncertain. One owner entry: keep
    both readings in an open point and make the item warn (never settled by one family alone)."""
    if not os.path.isfile(u.r("boundary-view.json")):
        return []
    view = load(u.r("boundary-view.json"))
    expected = view.get("writerExpected", {})
    out = []
    for c in view.get("codex", {}).get("cases", []):
        actual = view.get("recordActual", {}).get(c["id"])
        if actual is not None and c["answer"] == actual:
            continue  # the verifier's answer: the case stays as decided, a counted failure with the two-family reason
        if actual is None and c["answer"] != expected.get(c["id"]):
            continue  # a view of 14 November kept no record word: only the writer's word counts as uncertain
        target = next((e.get("target") for e in register_entries(u) if e.get("who") == "boundaryCase" and
                       (e["id"] in (c["id"], f"bc-{c['id']}") or e["id"].startswith(f"bc-{c['id']}-"))), "record")
        out.append({"id": f"uncertain-{c['id']}", "who": "owner", "target": target, "kind": "wrongEffect",
                    "claim": f"Boundary case {c['id']} is uncertain: the verifier set it aside, and Codex, reading the "
                             f"texts blind, gave «{c['answer']}» (the case writer expected «{expected.get(c['id'])}»): "
                             f"{c['why']} Deciding words: «{c['decidingWords']}». Keep both readings in an open point and "
                             "make the item warn; do not settle it either way."})
    return out


def followup_entries(u: Unit) -> list[dict]:
    """What a follow-up must decide, from the checks on the last final candidate: unanswered flags, a "done" beside an
    open question, a value mismatch, a cite the store refuses, a boundary case failing that no register has decided,
    and the support answers no register has decided. Only entries no earlier input holds."""
    given = {e["id"] for e in input_entries(u)}
    out = []
    if os.path.isfile(u.r("checklist-check.json")) and os.path.isfile(u.r("flags.final.json")):
        flags = {f["id"]: f for f in load(u.r("flags.final.json"))}
        out += [_flag_entry(flags[f]) for f in load(u.r("checklist-check.json"))["unansweredFlags"] if f in flags]
    results = _results(os.path.join(u.cases, "boundaryFinal.json"))
    cases = {c["id"]: c for c in load(u.boundary)} if os.path.isfile(u.boundary) else {}
    for res in results:
        cid = res["caseId"]
        if cid in cases and not res["passed"] and not _case_decided(u, cid) and cid not in limits(u):
            out.append(_case_entry(cases[cid], res, f"bc-{cid}"))
    out += _owner_done_entries(results)
    out += uncertain_entries(u)
    if os.path.isfile(u.r("values.final.json")):
        out += _values_entries(load(u.r("values.final.json")))
    check = os.path.join(u.run, "check.candidate.txt")
    if os.path.isfile(check):
        out += _store_entries(open(check, encoding="utf-8").read())
    out = [e for e in out if e["id"] not in given and not _decided(u, e["id"])]
    k = u.followups() + 1
    for s in _support_entries(u):
        if _decided(u, s["id"], s.get("target")):
            continue
        # Codex numbers its answers by pair: a later check's sup-29 may be another item than an earlier one's.
        clash = s["id"] in given or any(e["id"] == s["id"] for e in register_entries(u))
        out.append(dict(s, id=f"{s['id']}-f{k}") if clash else s)
    return [e for e in out if e["id"] not in given]


def verify_input(u: Unit, followup: bool = False, owner: list[dict] | None = None) -> str:
    """With `owner`, the owner's decisions that touch the record join the follow-up as `owner` entries."""
    if not followup:
        save(u.r("attacks.input.json"), first_input(u))
        return u.r("attacks.input.json")
    entries = followup_entries(u) + [dict(o, who="owner", kind=o.get("kind", "wrongCondition")) for o in owner or []]
    if not entries:
        raise RuntimeError("nothing new for a follow-up: every finding is in an earlier input or a register")
    k = u.followups() + 1
    save(u.followup_input(k), {"unit": u.id, "attacks": entries})
    return u.followup_input(k)


def undecided_input(u: Unit) -> list[str]:
    """Ids given to the verifier that no register holds with a verdict, and verdicts without the verifier's quote."""
    last = latest(register_entries(u))
    out = []
    for e in input_entries(u):
        v = last.get(e["id"])
        if v is None and u.id in LEGACY and e["id"].endswith("-runner"):  # follow-up 1 of the first module
            v = last.get(e["id"][:-len("-runner")])
        if v is None or v.get("verdict") not in ("fixed", "refuted", "open"):
            out.append(e["id"])
        elif v["verdict"] in ("fixed", "refuted") and not v.get("verifierQuote"):
            out.append(e["id"] + " (no quote)")
    return out


def second_view_due(u: Unit) -> tuple[list[str], list[str]]:
    """Stage 8b: cases a register set aside (refuted or open) that the final record still fails and Codex has not seen.
    Returns (cases to send, cases that cannot be a choice of words: the same word, or no outcome word)."""
    import prompts
    seen = set(load(u.r("boundary-view.json")).get("cases", [])) if os.path.isfile(u.r("boundary-view.json")) else set()
    failing = {r["caseId"]: r for r in _results(os.path.join(u.cases, "boundaryFinal.json")) if not r["passed"]}
    cases = {c["id"]: c for c in load(u.boundary)} if os.path.isfile(u.boundary) else {}
    send, owner = [], []
    for e in latest(register_entries(u)).values():
        if e.get("who") != "boundaryCase" or e.get("verdict") not in ("refuted", "open"):
            continue
        cid = e["id"][3:] if e["id"].startswith("bc-") else e["id"]
        cid = cid.split("-")[0]
        if cid in seen or cid not in failing or cid not in cases or cid in limits(u):
            continue
        actual = failing[cid].get("actual")
        if actual in prompts.OUTCOME_MEANINGS and actual != cases[cid]["expect"]["outcome"]:
            send.append(cid)
        else:
            owner.append(cid)
    return sorted(set(send)), sorted(set(owner))


# --- status ---------------------------------------------------------------------------------------------------------
def _requests_appended(u: Unit) -> bool:
    have = set()
    path = os.path.join(CL, "law", "requests.jsonl")
    if os.path.isfile(path):
        have = {line.strip() for line in open(path, encoding="utf-8") if line.strip()}
    for side in ("a", "b"):
        own = os.path.join(u.rules, "derivations", side, f"{u.id}.requests.jsonl")
        if os.path.isfile(own) and any(line.strip() and line.strip() not in have
                                       for line in open(own, encoding="utf-8")):
            return False
    return True


def _committed(paths: list[str]) -> bool:
    if any(not p.startswith(REPO) for p in paths):
        return True  # a planted round's folder: nothing to commit
    rel = [os.path.relpath(p, REPO) for p in paths]
    if _git("status", "--porcelain", "--", *rel):
        return False
    return all(subprocess.run(["git", "-C", REPO, "ls-files", "--error-unmatch", r], capture_output=True).returncode == 0
               for r in rel)


def stage4_reports(u: Unit) -> list[str]:
    names = ["values.a.json", "values.b.json", "flags.a.json", "flags.b.json", "checklist.json", "differences.json",
             "idmap.json"] + (["c-comparison.json"] if u.kind == "module" else [])
    return [u.r(n) for n in names]


def status(u: Unit) -> list[tuple[str, str]]:
    import prompts
    s: dict[str, str] = {}

    def need(*stages):
        missing = [x for x in stages if s.get(x) not in ("done", "not for a claim type")]
        return f"waiting for {', '.join(missing)}" if missing else None

    s["0"] = "done" if os.path.isfile(u.r("texts.json")) else "due"
    try:
        prompts.french_sources(u.id)
        s["0b"] = "done"
    except KeyError:
        s["0b"] = need("0") or "due: prepare the French sources (stage 0b) and add the unit to law\\french-sources.json"
    except FileNotFoundError as e:
        s["0b"] = f"blocked: a French file listed for the unit is missing: {e}"
    for st_, side in (("2a", "a"), ("2b", "b")):
        done = os.path.isfile(u.deriv(side)) and os.path.isfile(u.deriv(side).replace(".json", ".checklist.json"))
        s[st_] = "done" if done else (need("0", "0b") if side == "b" else need("0")) or "due"
    if u.kind == "claim":
        s["2c"] = "not for a claim type"
    else:
        s["2c"] = "done" if os.path.isfile(u.deriv("c")) else need("0") or "due"
    if os.path.isfile(u.boundary):
        problems = boundary_problems(load(u.boundary))
        s["2d"] = "done" if not problems else "blocked: the file has the wrong shape (set it aside, rerun fresh): " + \
            "; ".join(problems[:3])
    else:
        s["2d"] = need("0") or "due"
    s["3"] = need("2a", "2b") or ("done" if _requests_appended(u) else "due")
    s["4"] = need("2a", "2b", "2c", "2d", "3") or ("done" if all(map(os.path.isfile, stage4_reports(u))) else "due")
    if os.path.isfile(u.reconciled) and os.path.isfile(u.r("reconciliation.md")):
        text = open(u.r("reconciliation.md"), encoding="utf-8").read()
        ids = [d["id"] for d in load(u.r("differences.json"))["differences"]] if os.path.isfile(
            u.r("differences.json")) else []
        # the first module's report predates the fixed lines: its outcomes are table rows «| x1 | … |»
        form = r"^(?:difference {0}:|\| {0} \|)" if u.id in LEGACY else r"^difference {0}:"
        missing = [i for i in ids if not re.search(form.format(re.escape(i)), text, re.M)]
        s["5"] = "done" if not missing else f"blocked: no «difference <id>:» line for {', '.join(missing[:8])}"
    else:
        s["5"] = need("4")
        if s["5"] is None:
            if not _committed(stage4_reports(u)):
                s["5"] = "blocked: commit stage 4's reports first"
            else:
                try:
                    prompts.module_finals([u.id], strict=True)
                    s["5"] = "due"
                except ValueError as e:
                    s["5"] = f"waiting for {e}"
    stage6 = [u.r("values.reconciled.json"), u.r("flags.reconciled.json"), u.reconciled_q,
              os.path.join(u.cases, "boundaryReconciled.json")]
    s["6"] = need("5") or ("done" if all(map(os.path.isfile, stage6)) else "due")
    s["7a"] = "done" if os.path.isfile(u.r("attacks.adversary.json")) else need("6") or "due"
    attack = os.path.isfile(u.r("attacks.sonnet.json")) or (u.id in LEGACY and os.path.isfile(u.r("attacks.codex.json")))
    s["7b"] = "done" if attack else need("6") or "due"
    s["7c"] = "done" if os.path.isfile(u.r("support.json")) else need("6") or "due (unit.py codex)"
    if need("7a", "7b"):
        s["8"] = need("7a", "7b")
    elif not os.path.isfile(u.register(0)) or not os.path.isfile(u.candidate):
        s["8"] = "due"
    else:
        open_runs = [k for k in range(1, u.followups() + 1) if not os.path.isfile(u.register(k))]
        undecided = undecided_input(u)
        new = followup_entries(u) if os.path.isfile(u.candidate_q) else []
        if open_runs:
            s["8"] = f"due: follow-up {open_runs[0]} (its input is written)"
        elif undecided:
            s["8"] = f"blocked: given to the verifier without a verdict: {', '.join(undecided[:8])}"
        elif new:
            s["8"] = f"due: a follow-up for {len(new)} new findings (unit.py verify-input {u.id} --followup)"
        else:
            s["8"] = "done"
    if not os.path.isfile(u.register(0)):
        s["8b"] = "waiting for 8"
    else:
        send, _ = second_view_due(u)
        s["8b"] = f"due (unit.py codex): {', '.join(send)}" if send else "done"
    if s["8"] != "done" or s["8b"] != "done":
        s["9"] = "waiting for 8" + ("" if s["8"] != "done" else "b")
    else:
        grades = u.r("grades.json")
        fresh = os.path.isfile(grades) and os.path.isfile(u.candidate_q) and \
            load(grades).get("input") == file_sha(u.candidate_q)
        ok = os.path.isfile(u.r("checklist-check.json")) and load(u.r("checklist-check.json")).get("ok")
        s["9"] = "done" if os.path.isfile(u.final) and fresh and ok else "due"
    return [(x, s[x]) for x in STAGES]


# --- the script stages ----------------------------------------------------------------------------------------------
def stage0(u: Unit) -> bool:
    code, out = py("records.py", "unit-texts", u.id, "--seed", os.path.join(CL, "law", "seed-inventory.json"),
                   "--out", u.r("texts.json"))
    say("unit-texts", code, out)
    return code == 0


def stage3(u: Unit) -> bool:
    path = os.path.join(CL, "law", "requests.jsonl")
    have = {line.strip() for line in open(path, encoding="utf-8") if line.strip()} if os.path.isfile(path) else set()
    with open(path, "a", encoding="utf-8", newline="\n") as f:
        for side in ("a", "b"):
            own = os.path.join(u.rules, "derivations", side, f"{u.id}.requests.jsonl")
            if not os.path.isfile(own):
                continue
            for line in open(own, encoding="utf-8"):
                if line.strip() and line.strip() not in have:
                    f.write(line.strip() + "\n")
                    have.add(line.strip())
                    r = json.loads(line)
                    print(f"request ({side}, need {r.get('need', '?')}): {r.get('code')} {r.get('article')}: "
                          f"{str(r.get('why', ''))[:160]}")
    return True


def _flat(u: Unit, record: str, name: str) -> str:
    """A claim record with its imported module blocks copied in (stage 4's case runs, stage 9's grade)."""
    if u.kind != "claim":
        return record
    out = os.path.join(u.run, "flat", f"{name}.json")
    code, text = py("records.py", "flatten", record, "--modules", os.path.join(u.rules, "final"), "--out", out)
    say(f"flatten {name}", code, text)
    if code:
        raise RuntimeError(f"flatten {name} failed")
    return out


def stage4(u: Unit) -> bool:
    t = tool()
    for side in ("a", "b"):
        code, out = run([t, "check", "--store", STORE, u.deriv(side)])
        say(f"rules check {side}", code, out)
        if code:
            print("STOP: the tool refuses a record an agent just wrote: delete it and rerun stage "
                  f"{'2a' if side == 'a' else '2b'} fresh (HANDBOOK, decision table)")
            return False
        for args, what in ((("quote", u.deriv(side)), "quote"),
                           (("check-values", u.deriv(side, True), "--report", u.r(f"values.{side}.json")), "check-values"),
                           (("flag-neighbours", u.deriv(side, True), "--report", u.r(f"flags.{side}.json")),
                            "flag-neighbours")):
            code, out = py("records.py", *args)
            say(f"{what} {side}", code, out)
        say(f"examples {side}", *run([t, "examples", u.deriv(side)]))
    code, out = py("records.py", "merge-checklists", u.id, "--a", u.deriv("a").replace(".json", ".checklist.json"),
                   "--b", u.deriv("b").replace(".json", ".checklist.json"), "--out", u.r("checklist.json"))
    say("merge-checklists", code, out)
    code, out = py("records.py", "compare", u.deriv("a", True), u.deriv("b", True), "--differences",
                   u.r("differences.json"), "--idmap", u.r("idmap.json"))
    say("compare A and B", code, out)
    if u.kind == "module":
        say("compare-c", *py("records.py", "compare-c", u.deriv("c"), u.deriv("a", True), u.deriv("b", True),
                              "--out", u.r("c-comparison.json"), "--texts", u.r("texts.json")))
    a, b = _flat(u, u.deriv("a"), "a"), _flat(u, u.deriv("b"), "b")
    runs = {"aOnB": [a, "--against", b, "--idmap", u.r("idmap.json")],
            "bOnA": [b, "--against", a, "--idmap", u.r("idmap.json")],
            "boundaryA": [a, "--boundary", u.boundary], "boundaryB": [b, "--boundary", u.boundary],
            "boundaryAB": [a, b, "--boundary", u.boundary]}
    for label, args in runs.items():
        code, _ = run([t, "examples", *args, "--json"], keep=os.path.join(u.cases, f"{label}.json"))
        print(f"== examples {label}: exit {code}")
    say("add-cases", *py("records.py", "add-cases", u.r("differences.json"),
                          *[x for label in runs for x in (f"--{label}", os.path.join(u.cases, f"{label}.json"))]))
    print(f"commit stage 4's reports before stage 5: {', '.join(os.path.relpath(p, REPO) for p in stage4_reports(u))}")
    return True


def checks_on(u: Unit, record: str, quoted: str, tag: str) -> bool:
    """The checks of stages 6 and 9 on one record (tag: reconciled or final)."""
    t = tool()
    code, out = run([t, "check", "--store", STORE, record])
    with open(os.path.join(u.run, f"check.{'reconciled' if tag == 'reconciled' else 'candidate'}.txt"), "w",
              encoding="utf-8", newline="\n") as f:
        f.write(out + "\n")
    say(f"rules check {tag}", code, out)
    if code and tag == "reconciled":
        print("STOP: the tool refuses the reconciled record: delete it and rerun stage 5 fresh (HANDBOOK, decision table)")
        return False
    say(f"quote {tag}", *py("records.py", "quote", record))
    say("check-values", *py("records.py", "check-values", quoted, "--report", u.r(f"values.{tag}.json")))
    say("flag-neighbours", *py("records.py", "flag-neighbours", quoted, "--report", u.r(f"flags.{tag}.json")))
    if tag == "reconciled":
        say("complete-idmap", *py("records.py", "complete-idmap", u.r("idmap.json"), u.deriv("a", True),
                                   u.deriv("b", True), quoted))
    flat = _flat(u, record, tag)
    say(f"examples {tag}", *run([t, "examples", flat]))
    name = "boundaryReconciled.json" if tag == "reconciled" else "boundaryFinal.json"
    code, _ = run([t, "examples", flat, "--boundary", u.boundary, "--json"], keep=os.path.join(u.cases, name))
    print(f"== boundary cases on the {tag} record: exit {code} (kept in {name})")
    report = u.r("checklist-check.reconciled.json" if tag == "reconciled" else "checklist-check.json")
    say("check-checklist", *py("records.py", "check-checklist", quoted, "--checklist", u.r("checklist.json"),
                                "--flags", u.r(f"flags.{tag}.json"), "--report", report))
    return True


def stage6(u: Unit) -> bool:
    os.makedirs(u.cases, exist_ok=True)
    return checks_on(u, u.reconciled, u.reconciled_q, "reconciled")


def grade(u: Unit, planted: str | None = None) -> bool:
    """Stage 9's grade: every register in order, the support check, the planted-error rates."""
    record = u.candidate_q
    if u.kind == "claim":
        record = _flat(u, u.candidate_q, "graded")
    args = ["records.py", "grade", record, "--out", u.final, "--report", u.r("grades.json"),
            "--idmap", u.r("idmap.json"), "--differences", u.r("differences.json"), "--support", u.r("support.json"),
            "--planted", planted or os.path.join(u.reports, "planted-errors.json")]
    for path in u.registers():
        args += ["--attacks", path]
    code, out = py(*args)
    say("grade", code, out)
    if code:
        return False
    rep = load(u.r("grades.json"))
    rep["input"] = file_sha(u.candidate_q)
    save(u.r("grades.json"), rep)
    t = tool()
    code, out = run([t, "check", "--store", STORE, u.final])
    say("rules check final", code, out)
    code, out = py("records.py", "check-checklist", u.final, "--checklist", u.r("checklist.json"),
                   "--flags", u.r("flags.final.json"), "--report", u.r("checklist-check.json"))
    say("check-checklist final", code, out)
    return code == 0


def stage9(u: Unit) -> bool:
    if not checks_fresh(u) and not run_final_checks(u):
        return False
    new = followup_entries(u)
    if new:
        print(f"a follow-up is due ({len(new)} findings): python unit.py verify-input {u.id} --followup")
        return False
    added = slots(u)
    if added:
        print(f"slots.json gained {', '.join(added)}: the Arabic check runs per batch of three units")
    return grade(u)


SCRIPT_STAGES = {"0": stage0, "3": stage3, "4": stage4, "6": stage6, "9": stage9}


# --- the agent stages -----------------------------------------------------------------------------------------------
def _run_id(ledger: str = LEDGER) -> str:
    n = max([int(i[5:]) for i in ledger_rows(ledger) if re.match(r"^r-s1-\d+$", i)] or [0])
    return f"r-s1-{n + 1:04d}"


def _write_ledger(row: dict, ledger: str = LEDGER) -> None:
    os.makedirs(os.path.dirname(ledger), exist_ok=True)
    with open(ledger, "a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(row, ensure_ascii=False) + "\n")


def _running(u: Unit, stage: str, ledger: str = LEDGER) -> dict | None:
    return next((r for r in ledger_rows(ledger).values() if r.get("unit") == u.id and r.get("stage") == stage
                 and r.get("status") == "running"), None)


def launch(u: Unit, stage: str, ledger: str = LEDGER) -> str:
    """Fills the stage's prompt into the run folder, writes the ledger's launch line, returns the launch text."""
    import prompts
    prompt, model, _ = AGENTS[stage]
    already = _running(u, stage, ledger)
    if already:
        return f"RUNNING stage {stage} {prompt} ({already['id']}): when it returns, python unit.py done {u.id} {stage} <agent id>"
    followup = u.followups() if stage == "8" and os.path.isfile(u.register(0)) else 0
    name = f"{prompt}{'-followup' + str(followup) if followup else ''}.prompt.md"
    path = os.path.join(u.run, name)
    if stage == "7b":
        say("render", *py("stage7.py", "render", u.reconciled_q, "--out", os.path.join(u.run, "record_text.txt")))
    os.makedirs(u.run, exist_ok=True)
    _, body = prompts.template(prompt)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(prompts.fill(body, prompts.values_for(prompt, u.id, followup)))
    commit = _git("rev-parse", "--short", "HEAD")
    rid = _run_id(ledger)
    _write_ledger({"id": rid, "kind": "agent", "role": prompt + (f" (follow-up {followup})" if followup else ""),
                   "model": f"claude-{model}-5-5", "unit": u.id, "stage": stage, "prompt": f"{prompt}.md@{commit}",
                   "started": now(), "status": "running", "outputs": []}, ledger)
    return (f"LAUNCH  stage {stage}  {prompt}  model: {model}  fresh agent, background  (run {rid})\n"
            "PROMPT  Your whole task is written in one file. Read it first with your Read tool, in one call with allow_large\n"
            "        set to true (a long file is otherwise cut short), and follow it exactly,\n"
            f"        including its list of what you may open: {path}. That file is the only\n"
            "        thing you may open before its own allow-list takes over. Your final reply is the short reply that "
            "file asks for.\n"
            f"THEN    python unit.py done {u.id} {stage} <agent id>")


def checks_fresh(u: Unit) -> bool:
    stamp = os.path.join(u.run, "checks.final.json")
    return os.path.isfile(stamp) and os.path.isfile(u.candidate) and load(stamp).get("candidate") == file_sha(u.candidate)


def run_final_checks(u: Unit) -> bool:
    os.makedirs(u.cases, exist_ok=True)
    ok = checks_on(u, u.candidate, u.candidate_q, "final")
    if ok:
        save(os.path.join(u.run, "checks.final.json"), {"candidate": file_sha(u.candidate), "at": now()})
    return ok


def next_(u: Unit, ledger: str = LEDGER) -> int:
    if os.path.isfile(u.candidate) and not checks_fresh(u):
        print("-- the stage 9 checks on the new final candidate")
        if not run_final_checks(u):
            return 1
    for stage, state in status(u):
        if stage in SCRIPT_STAGES and state == "due":
            print(f"-- stage {stage}")
            if not SCRIPT_STAGES[stage](u):
                return 1
    rows = dict(status(u))
    for stage, state in rows.items():
        if stage in AGENTS and state.startswith("due"):
            if stage == "8" and "new findings" in state:
                print("verify-input:", verify_input(u, followup=True))
            elif stage == "8" and not os.path.isfile(u.register(0)):
                print("verify-input:", verify_input(u))
            print(launch(u, stage, ledger))
        elif stage in CODEX_STAGES and state.startswith("due"):
            print(f"CODEX   stage {stage}: python unit.py codex {u.id}")
        elif state.startswith("blocked"):
            print(f"BLOCKED stage {stage}: {state}")
    return 0


def done(u: Unit, stage: str, agent: str, ledger: str = LEDGER, session: str | None = None,
         changed: str | None = None) -> int:
    row = _running(u, stage, ledger)
    if row is None:
        print(f"no running ledger line for {u.id} stage {stage}")
        return 1
    prompt, _, no_tool = AGENTS[stage]
    problems, outputs = [], []
    t = tool()
    prompt_file = os.path.join(u.run, row.get("role", prompt).split(" ")[0] + (
        f"-followup{u.followups()}" if "follow-up" in row.get("role", "") else "") + ".prompt.md")
    if no_tool:
        out, schema, extra = ((u.deriv("c"), "module-derivation-c.schema.json", []) if stage == "2c" else
                              (u.r("attacks.sonnet.json"), "attacks.schema.json",
                               ["--ids", "atk"] + (["--append"] if os.path.isfile(u.r("attacks.sonnet.json")) else [])))
        code, text = py("records.py", "ingest-reply", "--agent", agent, "--prompt", prompt_file, "--schema",
                        os.path.join(SCHEMAS, schema), "--out", out, *extra, *(["--session", session] if session else []))
        say("ingest-reply", code, text)
        if code:
            problems.append({3: "a tool other than its own prompt's read was used",
                             4: "the answer is not one JSON value passing its schema"}.get(code, text[-300:]))
        outputs.append(out)
    elif stage in ("2a", "2b"):
        rec = u.deriv(stage[1])
        for path in (rec, rec.replace(".json", ".checklist.json")):
            if not os.path.isfile(path):
                problems.append(f"missing {path}")
        if not problems:
            for args in (["check", "--store", STORE, rec], ["examples", rec]):
                code, text = run([t, *args])
                if code:
                    problems.append(f"rules {args[0]}: " + text[-600:])
        outputs += [rec, rec.replace(".json", ".checklist.json")]
    elif stage == "2d":
        if not os.path.isfile(u.boundary):
            problems.append(f"missing {u.boundary}")
        else:
            bad = boundary_problems(load(u.boundary))
            if bad:
                os.makedirs(os.path.join(u.run, "discarded"), exist_ok=True)
                shutil.move(u.boundary, os.path.join(u.run, "discarded", f"boundary-cases-{row['id']}.json"))
                problems.append("wrong shape, set aside in the run folder: " + "; ".join(bad[:4]))
        outputs.append(u.boundary)
    elif stage == "5":
        for path in (u.reconciled, u.r("reconciliation.md")):
            if not os.path.isfile(path):
                problems.append(f"missing {path}")
        if not problems:
            code, text = run([t, "check", "--store", STORE, u.reconciled])
            if code:
                problems.append("rules check: " + text[-600:])
        outputs += [u.reconciled, u.r("reconciliation.md")]
    elif stage == "7a":
        try:
            load(u.r("attacks.adversary.json"))["attacks"]
        except (OSError, KeyError, ValueError) as e:
            problems.append(f"attacks.adversary.json: {e}")
        outputs.append(u.r("attacks.adversary.json"))
    elif stage == "8":
        k = u.followups() if "follow-up" in row.get("role", "") else 0
        for path in (u.register(k), u.candidate):
            if not os.path.isfile(path):
                problems.append(f"missing {path}")
        if not problems:
            code, text = run([t, "check", "--store", STORE, u.candidate])
            if code:
                problems.append("rules check on the candidate: " + text[-600:])
        outputs += [u.register(k), u.candidate]
    status_ = "failed" if problems else "done"
    line = dict(row, ended=now(), status=status_, agentId=agent,
                outputs=[os.path.relpath(p, REPO).replace("\\", "/") if p.startswith(REPO) else p for p in outputs])
    if changed:
        line["changedExamples"] = changed
    if problems:
        line["reason"] = "; ".join(problems)[:1500]
    _write_ledger(line, ledger)
    if problems:
        print("FAILED:", *problems, sep="\n  ")
        print("The rule: delete the outputs and rerun the stage with a fresh agent; never repair them.")
        return 1
    print(f"stage {stage} done ({row['id']})")
    return 0


# --- the Codex stages (7c, 8b): through the background queue -------------------------------------------------------
def _support_batch(u: Unit) -> str:
    """A fresh batch name for each support check of a unit: the key of an earlier one is kept."""
    keys = os.path.join(WORK, "support-keys")
    batch, n = f"step1-{u.id}", 1
    while os.path.isfile(os.path.join(keys, f"{batch}.json")):
        n += 1
        batch = f"step1-{u.id}-r{n}"
    return batch


def codex(u: Unit, queue=None, ledger: str = LEDGER, start: bool = True) -> list[str]:
    """Adds the unit's due Codex calls to the background queue, writes their ledger lines, starts `queue run` in the
    background if it is not running, and returns at once. `status` shows the calls by their outputs."""
    import codex_run
    import prompts
    queue = queue or codex_run.Queue()
    rows = dict(status(u))
    added = []
    commit = _git("rev-parse", "--short", "HEAD")
    os.makedirs(u.run, exist_ok=True)
    if rows["7c"].startswith("due"):
        record = u.candidate_q if os.path.isfile(u.candidate_q) else u.reconciled_q
        batch = _support_batch(u)
        pairs = os.path.join(u.run, "support-pairs.txt")
        code, out = py("stage7.py", "pairs", record, "--batch", batch, "--out", pairs, "--unit", u.id)
        say("pairs", code, out)
        if code:
            raise RuntimeError("stage7.py pairs failed")
        role = CODEX_STAGES["7c"]
        prompt = os.path.join(u.run, f"{role}.prompt.md")
        _, body = prompts.template(role)
        with open(prompt, "w", encoding="utf-8", newline="\n") as f:
            f.write(prompts.fill(body, prompts.values_for(role, u.id)))
        answer = os.path.join(u.run, f"support-answer-{batch}.json")
        after = [[PY, "-X", "utf8", os.path.join(SCRIPTS, "stage7.py"), "split", answer, "--batch", batch]
                 + (["--root", u.rules] if u.root else [])]
        rid = _run_id(ledger)
        row = {"id": rid, "kind": "codex", "role": role, "unit": u.id, "stage": "7c", "prompt": f"{role}.md@{commit}",
               "started": now(), "status": "running", "outputs": []}
        queue.add(role, prompt, os.path.join(SCHEMAS, "support.schema.json"), answer, call_id=rid,
                  item=f"{u.id}:support", after=after, ledger=row, batch=f"step1-{u.id}")
        _write_ledger(row, ledger)
        added.append(f"7c {rid}")
    send, _ = second_view_due(u) if os.path.isfile(u.register(0)) else ([], [])
    if send:
        role = CODEX_STAGES["8b"]
        results = _results(os.path.join(u.cases, "boundaryFinal.json"))
        prompt = os.path.join(u.run, f"{role}-{'-'.join(send)}.prompt.md")
        _, body = prompts.template(role)
        with open(prompt, "w", encoding="utf-8", newline="\n") as f:
            f.write(prompts.fill(body, prompts.values_for(role, u.id, cases=send, run=results)))
        answer = os.path.join(u.run, f"boundary-view-{'-'.join(send)}.json")
        actual = {r["caseId"]: r.get("actual") for r in results if r["caseId"] in send}
        after = [[PY, "-X", "utf8", os.path.join(SCRIPTS, "unit.py"), *(["--root", u.root] if u.root else []),
                  "store-view", u.id, answer, "--actual", json.dumps(actual)]]
        rid = _run_id(ledger)
        row = {"id": rid, "kind": "codex", "role": role, "unit": u.id, "stage": "8b", "cases": send,
               "prompt": f"{role}.md@{commit}", "started": now(), "status": "running", "outputs": []}
        queue.add(role, prompt, os.path.join(SCHEMAS, "boundary-view.schema.json"), answer, call_id=rid,
                  item=f"{u.id}:view:{'-'.join(send)}", after=after, ledger=row, batch=f"step1-{u.id}")
        _write_ledger(row, ledger)
        added.append(f"8b {rid} ({', '.join(send)})")
    if added and start:
        print("queue run:", "started" if queue.start() else "already running")
    return added


def store_view(u: Unit, answer_path: str, actual: dict) -> str:
    """Stage 8b's answer, merged into reports\\<unit>.boundary-view.json with both words that were in dispute. An answer
    that is not one of the two words offered for its case is refused: the call is queued again once."""
    answer = load(answer_path)
    cases = {c["id"]: c for c in load(u.boundary)}
    for c in answer["cases"]:
        offered = {cases[c["id"]]["expect"]["outcome"], actual.get(c["id"])}
        if c["answer"] not in offered:
            raise ValueError(f"{c['id']}: «{c['answer']}» is not one of the words offered ({', '.join(sorted(offered))})")
    path = u.r("boundary-view.json")
    view = load(path) if os.path.isfile(path) else {"unit": u.id, "cases": [], "writerExpected": {}, "codex": {"cases": []}}
    for c in answer["cases"]:
        view["cases"] = [x for x in view["cases"] if x != c["id"]] + [c["id"]]
        view["writerExpected"][c["id"]] = cases[c["id"]]["expect"]["outcome"]
        view.setdefault("recordActual", {})[c["id"]] = actual.get(c["id"])
        view["codex"]["cases"] = [x for x in view["codex"]["cases"] if x["id"] != c["id"]] + [c]
    save(path, view)
    return path


# --- slots (stage 9) ------------------------------------------------------------------------------------------------
def slots(u: Unit, slots_path: str | None = None) -> list[str]:
    """Appends the final candidate's proposed slots that slots.json lacks, keeping the file's own format."""
    path = slots_path or os.path.join(CL, "slots.json")
    if not os.path.isfile(u.candidate):
        return []
    proposed = load(u.candidate).get("proposedSlots") or []
    raw = open(path, encoding="utf-8").read()
    doc = json.loads(raw)
    known = {s["id"] for s in doc["slots"]}
    added = []
    for p in proposed:
        if p["id"] in known:
            continue
        q = p.get("question") or {}
        if not str(q.get("ar", "")).rstrip().endswith("؟"):
            raise ValueError(f"slot {p['id']}: the Arabic question must end with «؟»")
        entry = {"id": p["id"], "type": p["type"], "question": q}
        if p.get("choices"):
            entry["choices"] = p["choices"]
        entry.update({"askedFor": [u.id] if u.kind == "claim" else ["*"], "readFromNote": False, "origin": u.id})
        if p.get("why"):
            entry["why"] = p["why"]
        doc["slots"].append(entry)
        added.append(p["id"])
    if added:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(doc, ensure_ascii=False, indent=2) + "\n")
    return added


def apply_arabic(reply_path: str, items_path: str, slots_path: str | None = None) -> list[str]:
    """Writes the Arabic checker's corrections: `items_path` maps each item's number to its slot id (written when the
    check's prompt was filled); a correction that drops a place ({date}, {amount}, {name}) is refused."""
    path = slots_path or os.path.join(CL, "slots.json")
    reply, numbers = load(reply_path), load(items_path)
    doc = load(path)
    by_id = {s["id"]: s for s in doc["slots"]}
    changed = []
    for it in reply["items"]:
        if it["ok"]:
            continue
        sid = numbers[str(it["n"])]
        old = by_id[sid]["question"]["ar"]
        if set(re.findall(r"\{\w+\}", old)) != set(re.findall(r"\{\w+\}", it["corrected"])):
            raise ValueError(f"item {it['n']} ({sid}): the correction changes the places of the question")
        if not it["corrected"].rstrip().endswith("؟"):
            raise ValueError(f"item {it['n']} ({sid}): the corrected question does not end with «؟»")
        by_id[sid]["question"]["ar"] = it["corrected"]
        changed.append(sid)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(doc, ensure_ascii=False, indent=2) + "\n")
    return changed


# --- the owner's question batch -------------------------------------------------------------------------------------
CRITICAL = {"deadlines", "priorSteps", "boundaries"}


def _effect_items(record: dict) -> set[str]:
    """Items that could block or that change what the lawyer is told: deadlines, prior steps, boundaries, exits and the
    section, and any item that declares a block."""
    ids = {x["id"] for k in CRITICAL for x in record.get(k, []) if x.get("id")}
    court = record.get("court", {})
    ids |= {x["id"] for x in court.get("exits", []) + court.get("territory", []) if x.get("id")}
    if "section" in court:
        ids.add(court["section"].get("id", "section"))
    for k, v in record.items():
        if isinstance(v, list):
            ids |= {x["id"] for x in v if isinstance(x, dict) and x.get("id") and "blockCleanExport" in json.dumps(x)}
    return ids


LEGAL_FILE = os.path.join(CL, "reports", "open-legal-questions.md")
LEGAL_HEAD = """# Open legal questions

**The owner's standing rule of 15 November 2026.** The owner is not the legal reviewer, and there is none. A question of
legal reading (which reading of the law is right) is never sent to the owner: the record keeps the protective behaviour
it has (it warns, keeps both readings, never blocks), the item stays at grade C or D, and the question is kept here with
its readings and what each would change, for a legal reviewer if the product ever has one. Each unit's section is
written by `unit.py questions <unit>`; entry 4 of each question is written by the executor from the readings only, and
kept while the readings do not change.

Every check behind these entries except Codex's support check was made by Claude models (HANDBOOK, section 5b).
"""
PLACEHOLDER4 = ("EXECUTOR: from the readings above only, what each item would then do (block, warn, ask, nothing).")


def _readings_key(o: dict) -> str:
    return hashlib.sha256(json.dumps([o["question"], o.get("readings", []), o.get("protectiveReading")],
                                     ensure_ascii=False, sort_keys=True).encode("utf-8")).hexdigest()[:12]


def _sections(text: str) -> dict[str, str]:
    """The file's unit sections, by unit id («## `<unit>`»)."""
    out = {}
    parts = re.split(r"(?m)^## `([^`]+)`\n", text)
    for i in range(1, len(parts) - 1, 2):
        out[parts[i]] = parts[i + 1]
    return out


def questions(units: list[Unit], path: str | None = None) -> str:
    """Writes each unit's section of open-legal-questions.md: one entry per open point of grade C or D that names an
    item which could block or changes what the lawyer is told; the boundary cases still failing; the limits. An entry 4
    already written is kept while the open point's question, readings and protective reading are unchanged."""
    path = path or LEGAL_FILE
    text = open(path, encoding="utf-8").read() if os.path.isfile(path) else LEGAL_HEAD
    sections = _sections(text)
    for u in units:
        old = sections.get(u.id, "")
        kept4 = {}
        for m in re.finditer(r"<!-- (o\d+) ([0-9a-f]{12}) -->\n((?:(?!<!-- o).*\n)*)", old):
            e4 = re.search(r"^4\. \*\*What would change with each answer:\*\* (.*)$", m.group(3), re.M)
            if e4 and e4.group(1) != PLACEHOLDER4:
                kept4[(m.group(1), m.group(2))] = e4.group(1)
        record = load(u.final) if os.path.isfile(u.final) else load(u.candidate_q)
        effect = _effect_items(record)
        rep = load(u.r("grades.json")) if os.path.isfile(u.r("grades.json")) else {"items": []}
        counts = {}
        for row in rep["items"]:
            counts[row["grade"]] = counts.get(row["grade"], 0) + 1
        lines = ["", "Grades of the items: " + (", ".join(f"{g} {n}" for g, n in sorted(counts.items())) or "not graded")
                 + ".", ""]
        n = 0
        for o in record.get("openPoints", []):
            if o.get("grade") not in ("C", "D") or not set(o.get("refs", [])) & effect:
                continue
            n += 1
            key = _readings_key(o)
            lines += [f"<!-- {o['id']} {key} -->",
                      f"### {u.id} {n}. {o['question']['en']} ({o['id']}, grade {o['grade']}; items {', '.join(o['refs'])})",
                      "", f"1. **The question:** {o['question']['en']} «{o['question']['ar']}»", "2. **The readings:**"]
            for r in o.get("readings", []):
                lines.append(f"   - {r['reading']['en']} (rests on {', '.join(r.get('basis', [])) or 'no statement'})")
            if not o.get("readings"):
                lines.append(f"   - (no readings recorded) {o['why']['en']}")
            lines += [f"3. **What the record does (the protective behaviour it keeps):** {o['protectiveReading']['en']}",
                      f"4. **What would change with each answer:** {kept4.get((o['id'], key), PLACEHOLDER4)}", ""]
        failing = [r for r in _results(os.path.join(u.cases, "boundaryFinal.json")) if not r["passed"]]
        if failing:
            lines += [f"### {u.id}: boundary cases the final record still fails", ""]
            lim = limits(u)
            for r in failing:
                note = f" **{lim[r['caseId']]['kind']}:** {lim[r['caseId']]['why']}" if r["caseId"] in lim else ""
                lines.append(f"- **{r['caseId']}** ({r['title']}): " + " | ".join(r["differences"]) + note)
            lines.append("")
        _, owner = second_view_due(u)
        if owner:
            lines += ["Cases that cannot be put to Codex as a choice of words (the same word on both sides, a last day "
                      f"only, or no outcome word): {', '.join(owner)}.", ""]
        sections[u.id] = "\n".join(lines).rstrip("\n") + "\n\n"
    head = re.split(r"(?m)^## `", text, maxsplit=1)[0].rstrip("\n") + "\n\n"
    body = "".join(f"## `{k}`\n{v}" for k, v in sections.items())
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write((head + body).rstrip("\n") + "\n")
    return path


def unfilled_questions(path: str | None = None) -> int:
    path = path or LEGAL_FILE
    return open(path, encoding="utf-8").read().count(PLACEHOLDER4) if os.path.isfile(path) else 0


# --- tracking -------------------------------------------------------------------------------------------------------
TRACK_HEAD = "## Rows written by the driver (unit.py tracking)"
TRACK_TABLE = ("| Unit | A-B agreement before reconciliation | Boundary cases passed: A / B / reconciled / final | Still "
               "failing (first reason) | Cited outside the texts given (A / B / both) | Attacks: fixed / refuted / open | "
               "Grades | Proof labels (twoFamilies / mechanical / oneFamily) |\n|---|---|---|---|---|---|---|---|")


def _passed(path: str) -> str:
    rs = [r for r in _results(path) if r["caseId"].startswith("bc")]
    return f"{sum(r['passed'] for r in rs)}" if rs else "–"


def tracking_row(u: Unit) -> str:
    diff = load(u.r("differences.json")) if os.path.isfile(u.r("differences.json")) else {}
    tot = diff.get("agreementTotal") or {}
    agree = f"{tot.get('equal', '–')} of {tot.get('compared', '–')}" if tot else "–"
    total = len([c for c in load(u.boundary)]) if os.path.isfile(u.boundary) else 0
    passed = " / ".join(_passed(os.path.join(u.cases, f)) for f in
                        ("boundaryA.json", "boundaryB.json", "boundaryReconciled.json", "boundaryFinal.json"))
    lim = limits(u)
    failing = [f"{r['caseId']}: " + (f"{lim[r['caseId']]['kind']}: {lim[r['caseId']]['why'][:90]}" if r["caseId"] in lim
                                     else r["differences"][0][:90])
               for r in _results(os.path.join(u.cases, "boundaryFinal.json")) if not r["passed"] and r["differences"]]
    o = diff.get("outsideTextsGiven") or {}
    outside = f"{o.get('a', '–')} / {o.get('b', '–')} / {o.get('both', '–')}" if o else "–"
    verdicts = [e.get("verdict") for e in latest(register_entries(u)).values()]
    attacks = f"{verdicts.count('fixed')} / {verdicts.count('refuted')} / {verdicts.count('open')}"
    rep = load(u.r("grades.json")) if os.path.isfile(u.r("grades.json")) else {"items": []}
    g, p = {}, {}
    for row in rep["items"]:
        g[row["grade"]] = g.get(row["grade"], 0) + 1
        p[row.get("proof")] = p.get(row.get("proof"), 0) + 1
    grades = ", ".join(f"{k} {v}" for k, v in sorted(g.items())) or "–"
    proof = f"{p.get('twoFamilies', 0)} / {sum(1 for r in rep['items'] if r.get('textProof') == 'mechanical')} / " \
            f"{p.get('oneFamily', 0)}" if rep["items"] else "–"
    return (f"| `{u.id}` | {agree} | {passed} of {total} | {'; '.join(failing) or 'none'} | {outside} | {attacks} | "
            f"{grades} | {proof} |")


def tracking(u: Unit, path: str | None = None) -> str:
    path = path or os.path.join(CL, "reports", "step-1-tracking.md")
    text = open(path, encoding="utf-8").read() if os.path.isfile(path) else "# Step 1: figures tracked for every unit\n"
    row = tracking_row(u)
    if TRACK_HEAD not in text:
        text = text.rstrip("\n") + "\n\n" + TRACK_HEAD + "\n\n" + \
            ("Every check behind these rows except Codex's support check was made by Claude models (HANDBOOK, section "
             "5b). The proof label `mechanical` counts the items whose texts were proved mechanically; the other two "
             "count the items' own `proof`.\n\n") + TRACK_TABLE + "\n"
    lines = text.split("\n")
    at = next((i for i, x in enumerate(lines) if x.startswith(f"| `{u.id}` |") and i > lines.index(TRACK_HEAD)), None)
    if at is None:
        end = len(lines) - 1
        while end > 0 and not lines[end].startswith("|"):
            end -= 1
        lines.insert(end + 1, row)
    else:
        lines[at] = row
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines).rstrip("\n") + "\n")
    return row


# --- regrade --------------------------------------------------------------------------------------------------------
def regrade_all(root: str | None = None, rebuild: bool = True) -> int:
    """Phase D and after each planted-error round: the store rebuilt, every final candidate re-quoted and checked, the
    modules graded, then the claim types (flattened against the new module finals); rules/reports/grades.md."""
    import prompts
    if rebuild and root is None:
        code, out = py("store.py", "build")
        say("store build", code, out)
        if code:
            return 1
        say("store text views", *py("store_text.py"))
    rules = os.path.join(root, "rules") if root else os.path.join(CL, "rules")
    found = sorted(n[:-5] for n in os.listdir(os.path.join(rules, "final-candidates")) if n.endswith(".json"))
    units = [Unit(x, root) for x in found]
    units.sort(key=lambda u: u.kind != "module")  # modules first: the claim types flatten against them
    failed = []
    table = ["# Grades of every final unit", "", "Written by `unit.py regrade --all`. Every check behind them except "
             "Codex's support check was made by Claude models (HANDBOOK, section 5b).", "",
             "| Unit | Kind | Grades | Proof labels (twoFamilies / oneFamily) |", "|---|---|---|---|"]
    for u in units:
        print(f"-- {u.id}")
        os.makedirs(u.cases, exist_ok=True)
        checks_on(u, u.candidate, u.candidate_q, "final")
        if not grade(u):
            failed.append(u.id)
        rep = load(u.r("grades.json")) if os.path.isfile(u.r("grades.json")) else {"items": []}
        g, p = {}, {}
        for row in rep["items"]:
            g[row["grade"]] = g.get(row["grade"], 0) + 1
            p[row.get("proof")] = p.get(row.get("proof"), 0) + 1
        table.append(f"| `{u.id}` | {u.kind} | {', '.join(f'{k} {v}' for k, v in sorted(g.items()))} | "
                     f"{p.get('twoFamilies', 0)} / {p.get('oneFamily', 0)} |")
    with open(os.path.join(rules, "reports", "grades.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(table) + "\n")
    if failed:
        print("grade or checklist check failed for:", ", ".join(failed))
    _ = prompts
    return 1 if failed else 0


# --- the command line -----------------------------------------------------------------------------------------------
def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="unit")
    p.add_argument("--root", help="rules\\ and runs\\ under this folder (the planted-error rounds)")
    sub = p.add_subparsers(dest="cmd", required=True)
    sub.add_parser("tool", help="publish the rules tool and make it current")
    for name in ("status", "next", "tracking", "codex"):
        sub.add_parser(name).add_argument("unit")
    sv = sub.add_parser("store-view", help="stage 8b's after-step (the background queue runs it)")
    sv.add_argument("unit")
    sv.add_argument("answer")
    sv.add_argument("--actual", required=True, help="JSON: case id -> the record's outcome word")
    d = sub.add_parser("done")
    d.add_argument("unit")
    d.add_argument("stage", choices=sorted(AGENTS))
    d.add_argument("agent")
    d.add_argument("--session")
    d.add_argument("--changed", help="the expectations the deriver says it changed, as its reply lists them")
    v = sub.add_parser("verify-input")
    v.add_argument("unit")
    v.add_argument("--followup", action="store_true")
    v.add_argument("--owner", help="a JSON file: a list of the owner's entries {id, target, claim} for the follow-up")
    q = sub.add_parser("questions", help="the units' sections of reports/open-legal-questions.md")
    q.add_argument("units", nargs="+")
    s = sub.add_parser("slots")
    s.add_argument("unit")
    s.add_argument("--apply", help="the Arabic checker's reply")
    s.add_argument("--items", help="with --apply: the file mapping the reply's item numbers to slot ids")
    r = sub.add_parser("regrade")
    r.add_argument("--all", action="store_true", required=True)
    r.add_argument("--no-build", action="store_true", help="do not rebuild the store first")
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    try:
        if a.cmd == "tool":
            print(publish())
        elif a.cmd == "status":
            for stage, state in status(Unit(a.unit, a.root)):
                print(f"{stage:<3} {state}")
        elif a.cmd == "next":
            return next_(Unit(a.unit, a.root))
        elif a.cmd == "done":
            return done(Unit(a.unit, a.root), a.stage, a.agent, session=a.session, changed=a.changed)
        elif a.cmd == "codex":
            added = codex(Unit(a.unit, a.root))
            print("queued:", "; ".join(added) if added else "nothing due")
        elif a.cmd == "store-view":
            print(store_view(Unit(a.unit, a.root), a.answer, json.loads(a.actual)))
        elif a.cmd == "verify-input":
            path = verify_input(Unit(a.unit, a.root), a.followup, load(a.owner) if a.owner else None)
            print(path, len(load(path)["attacks"]), "entries")
        elif a.cmd == "questions":
            path = questions([Unit(x, a.root) for x in a.units])
            print(path, f"({unfilled_questions(path)} entries still need their entry 4)")
        elif a.cmd == "slots":
            u = Unit(a.unit, a.root)
            if a.apply:
                print("corrected:", ", ".join(apply_arabic(a.apply, a.items)) or "none")
            else:
                print("added:", ", ".join(slots(u)) or "none")
        elif a.cmd == "regrade":
            return regrade_all(a.root, rebuild=not a.no_build)
        elif a.cmd == "tracking":
            print(tracking(Unit(a.unit, a.root)))
    except (RuntimeError, ValueError, KeyError) as e:
        print(e, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
