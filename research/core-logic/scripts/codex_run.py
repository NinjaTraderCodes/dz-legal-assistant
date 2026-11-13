"""Calling Codex, and the event-log check (docs/research/core-logic/specs/codex-calls.md).

Every call: an empty scratch folder outside every repository, a read-only sandbox, --ephemeral, an output schema, the
prompt on standard input, images last. A call is accepted only if every item of its event log is an agent message or
reasoning; any other item (a command, a file change, a search, a tool of any kind, an unknown type) rejects the whole
call for tool use. A usage-limit refusal is recognised apart and stops the batch.

    python codex_run.py probe1        # a one-word answer under a schema: must be accepted
    python codex_run.py probe2        # asked to read a canary file: must be rejected for tool use
"""
from __future__ import annotations

import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys

WORK = os.environ.get("CODEX_WORK", r"C:\Users\TheShadow\dz-core-research")
SCRATCH = os.path.join(WORK, "codex-scratch")
LOGS = os.path.join(WORK, "codex-logs")
ALLOWED_ITEMS = {"agent_message", "reasoning"}
LIMIT_WORDS = re.compile(r"usage limit|quota|rate[ _-]?limit|too many requests|\b429\b", re.I)
TIMEOUT = 1800
NO_TOOLS = "Do not run any command and do not open any file."

ACCEPTED, REJECTED, REFUSED = "accepted", "rejected", "refused"


def sha(text: str | bytes) -> str:
    return hashlib.sha256(text.encode("utf-8") if isinstance(text, str) else text).hexdigest()


# --- a small validator for the output schemas (the subset the handbook's schemas use) ---------------------------------
def validate(schema: dict, value, path: str = "$") -> list[str]:
    errors = []
    t = schema.get("type")
    kinds = {"object": dict, "array": list, "string": str, "boolean": bool, "integer": int, "number": (int, float)}
    if t:
        ok = isinstance(value, kinds[t]) and not (t in ("integer", "number") and isinstance(value, bool))
        if not ok:
            return [f"{path}: expected {t}"]
    if "const" in schema and value != schema["const"]:
        errors.append(f"{path}: must be {schema['const']!r}")
    if "enum" in schema and value not in schema["enum"]:
        errors.append(f"{path}: {value!r} not in {schema['enum']}")
    if isinstance(value, dict):
        for name in schema.get("required", []):
            if name not in value:
                errors.append(f"{path}: {name} is missing")
        props = schema.get("properties", {})
        for k, v in value.items():
            if k in props:
                errors += validate(props[k], v, f"{path}.{k}")
            elif schema.get("additionalProperties") is False:
                errors.append(f"{path}: {k} is not allowed")
    if isinstance(value, list) and "items" in schema:
        for i, v in enumerate(value):
            errors += validate(schema["items"], v, f"{path}[{i}]")
    return errors


# --- the event-log check -----------------------------------------------------------------------------------------------
def check(events: list[dict], exit_code: int, stderr: str, last_file_text: str | None, schema: dict | None) -> dict:
    """Classifies one call. Returns {status, reason, offending, answer, usage}. Never returns any item's content."""
    types = [e.get("item", {}).get("type") for e in events if e.get("type", "").startswith("item.")]
    completed = [e for e in events if e.get("type") == "turn.completed"]
    failed_event = any(e.get("type") in ("error", "turn.failed") for e in events)
    failed = exit_code != 0 or failed_event or not completed
    if failed:
        blob = stderr + "\n" + "\n".join(json.dumps(e) for e in events if e.get("type") in ("error", "turn.failed"))
        if LIMIT_WORDS.search(blob):
            return {"status": REFUSED, "reason": "usageLimit", "offending": None, "answer": None, "usage": None}
    offending = sorted({t for t in types if t not in ALLOWED_ITEMS})
    if offending:
        return {"status": REJECTED, "reason": "toolUse", "offending": offending, "answer": None, "usage": None}
    if exit_code != 0:
        return {"status": REJECTED, "reason": "exitCode", "offending": None, "answer": None, "usage": None}
    if failed_event:
        # An error event (for example a dropped connection that Codex retried) discards the call even if the turn
        # completed afterwards: the safe side, and the items are sent afresh.
        return {"status": REJECTED, "reason": "errorEvent", "offending": None, "answer": None, "usage": None}
    if not completed:
        return {"status": REJECTED, "reason": "noTurnCompleted", "offending": None, "answer": None, "usage": None}
    messages = [e["item"].get("text", "") for e in events
                if e.get("type") == "item.completed" and e.get("item", {}).get("type") == "agent_message"]
    if not messages:
        return {"status": REJECTED, "reason": "noAnswer", "offending": None, "answer": None, "usage": None}
    last = messages[-1]
    if last_file_text is not None and last_file_text.strip() != last.strip():
        return {"status": REJECTED, "reason": "outputMismatch", "offending": None, "answer": None, "usage": None}
    try:
        answer = json.loads(last)
    except ValueError:
        return {"status": REJECTED, "reason": "notJson", "offending": None, "answer": None, "usage": None}
    if schema is not None and validate(schema, answer):
        return {"status": REJECTED, "reason": "schema", "offending": None, "answer": None, "usage": None}
    return {"status": ACCEPTED, "reason": None, "offending": None, "answer": answer, "usage": completed[-1].get("usage")}


def parse_events(stdout: str) -> list[dict]:
    events = []
    for line in stdout.splitlines():
        line = line.strip()
        if line.startswith("{"):
            try:
                events.append(json.loads(line))
            except ValueError:
                pass
    return events


def version() -> str:
    exe = shutil.which("codex") or "codex"
    out = subprocess.run([exe, "--version"], capture_output=True, text=True, timeout=60)
    return out.stdout.strip()


# --- the owner's safeguards of 13 November 2026 ------------------------------------------------------------------------
PINNED_VERSION = "codex-cli 0.167.0"
GATE = os.path.join(WORK, "codex-daily-probe.json")


class Gate(RuntimeError):
    """No real call may be made: the CLI is not the pinned version, or today's read probe was not rejected by the check."""


def gate_open(today: str | None = None) -> None:
    """Raises Gate unless the CLI is the pinned version and today's read probe (probe 2) was rejected for tool use."""
    v = version()
    if v != PINNED_VERSION:
        raise Gate(f"Codex CLI is {v!r}, not {PINNED_VERSION!r}: re-run both probes and tell the owner before any real call")
    today = today or dt.date.today().isoformat()
    try:
        with open(GATE, encoding="utf-8") as f:
            g = json.load(f)
    except (OSError, ValueError):
        raise Gate("no read probe today: run `codex_run.py probe2` first")
    if g.get("date") != today or g.get("version") != PINNED_VERSION or not g.get("probe2Rejected"):
        raise Gate("today's read probe is missing, or it was not rejected by the check: no real call today")


# --- one call ----------------------------------------------------------------------------------------------------------
def call(prompt: str, role: str, batch: str, call_id: str, schema_path: str, images: list[str] | None = None,
         provenance: str | None = None, template: str = "", template_version: str = "", _probe: bool = False) -> dict:
    if not _probe:
        gate_open()
    scratch = os.path.join(SCRATCH, role)
    os.makedirs(scratch, exist_ok=True)
    if os.listdir(scratch):
        raise RuntimeError(f"the scratch folder is not empty: {scratch}")
    log_dir = os.path.join(LOGS, role, batch)
    os.makedirs(log_dir, exist_ok=True)
    base = os.path.join(log_dir, call_id)
    last_file = base + ".json"
    exe = shutil.which("codex") or "codex"
    cmd = [exe, "exec", "-", "-s", "read-only", "-C", scratch, "--skip-git-repo-check", "--ephemeral", "--json",
           "--output-schema", schema_path, "-o", last_file]
    if images:
        cmd += ["-i", *images]  # -i takes a list and must come last
    env = {k: v for k, v in os.environ.items() if k not in ("OPENROUTER_API_KEY", "GOOGLE_API_KEY")}
    started = dt.datetime.now(dt.timezone.utc)
    try:
        p = subprocess.run(cmd, input=prompt.encode("utf-8"), capture_output=True, env=env, timeout=TIMEOUT, cwd=scratch)
        stdout, stderr, code = p.stdout.decode("utf-8", "replace"), p.stderr.decode("utf-8", "replace"), p.returncode
    except subprocess.TimeoutExpired as e:
        stdout = (e.stdout or b"").decode("utf-8", "replace")
        stderr, code = "timeout", -1
    with open(base + ".jsonl", "w", encoding="utf-8", newline="\n") as f:
        f.write(stdout)
    with open(base + ".err", "w", encoding="utf-8", newline="\n") as f:
        f.write(stderr)
    last_text = open(last_file, encoding="utf-8").read() if os.path.exists(last_file) else None
    with open(schema_path, encoding="utf-8") as f:
        schema = json.load(f)
    events = parse_events(stdout)
    result = check(events, code, stderr, last_text, schema)
    leftovers = os.listdir(scratch)
    if leftovers:  # a read-only sandbox should leave nothing; anything there is reported and removed
        result.update(status=REJECTED, reason="scratchNotEmpty")
        for name in leftovers:
            path = os.path.join(scratch, name)
            shutil.rmtree(path) if os.path.isdir(path) else os.remove(path)
    if result["status"] != ACCEPTED and os.path.exists(last_file):
        # Nothing from a discarded call is ever used, not even part of its output: its last message is removed. The
        # event log stays for the record and is read only to classify the call (and, for the probes, to show it).
        os.remove(last_file)
    result.update(callId=call_id, role=role, batch=batch, log=base + ".jsonl")
    if provenance:
        line = {"callId": call_id, "role": role, "batch": batch, "template": template, "templateVersion": template_version,
                "cli": version(), "at": started.isoformat(timespec="seconds"), "usage": result.get("usage"),
                "status": result["status"], "reason": result["reason"], "promptSha256": sha(prompt),
                "answerSha256": sha(json.dumps(result["answer"], ensure_ascii=False, sort_keys=True)) if result["answer"] is not None else None,
                "images": [sha(open(i, "rb").read()) for i in images or []]}
        os.makedirs(os.path.dirname(provenance), exist_ok=True)
        with open(provenance, "a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(line, ensure_ascii=False) + "\n")
    return result


class Batch:
    """The pacing rules: one call at a time; a usage-limit refusal stops the batch; two failed calls in a row stop it."""

    def __init__(self):
        self.failures_in_a_row, self.stopped, self.why, self.counts = 0, False, None, {ACCEPTED: 0, REJECTED: 0, REFUSED: 0, "toolUse": 0}

    def record(self, result: dict) -> None:
        self.counts[result["status"]] += 1
        if result.get("reason") == "toolUse":
            self.counts["toolUse"] += 1
        if result["status"] == REFUSED:
            self.stopped, self.why = True, "usage limit"
            return
        self.failures_in_a_row = 0 if result["status"] == ACCEPTED else self.failures_in_a_row + 1
        if self.failures_in_a_row >= 2:
            self.stopped, self.why = True, f"two failed calls in a row ({result.get('reason')})"


PROBE_SCHEMA = {"type": "object", "additionalProperties": False, "required": ["word"], "properties": {"word": {"type": "string"}}}


def _probe(n: int) -> int:
    path = os.path.join(WORK, "codex-probes", "schema-word.json")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(PROBE_SCHEMA, f)
    stamp = dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    if n == 1:
        prompt = 'Answer with the single English word "ready" in the field "word". ' + NO_TOOLS
    else:
        canary = os.path.join(WORK, "canary", "canary.txt")
        prompt = f'Run a shell command that prints the file {canary}, then put its first word in the field "word".'
    r = call(prompt, f"probe{n}", stamp, f"probe{n}", path, _probe=True)
    print(f"cli: {version()}")
    print(f"probe {n}: status={r['status']} reason={r['reason']} offending={r['offending']}")
    if n == 2:  # the probe script, and only it, may print the probe command's own output, to show the read was refused
        for e in parse_events(open(r["log"], encoding="utf-8").read()):
            item = e.get("item", {})
            if e.get("type") == "item.completed" and item.get("type") == "command_execution":
                print("  command output:", (item.get("aggregated_output") or "")[:200].replace("\n", " | "))
        rejected = r["status"] == REJECTED and r["reason"] == "toolUse"
        with open(GATE, "w", encoding="utf-8") as f:
            json.dump({"date": dt.date.today().isoformat(), "version": version(), "probe2Rejected": rejected,
                       "at": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")}, f)
        print("gate:", "open for today" if rejected and version() == PINNED_VERSION else "CLOSED: no real call today")
        return 0 if rejected else 1
    return 0 if r["status"] == ACCEPTED else 1


if __name__ == "__main__":
    for key in ("OPENROUTER_API_KEY", "GOOGLE_API_KEY"):
        os.environ.pop(key, None)
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(_probe(1 if sys.argv[1:] == ["probe1"] else 2) if sys.argv[1:] in (["probe1"], ["probe2"]) else 2)
