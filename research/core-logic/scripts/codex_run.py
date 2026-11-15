"""Calling Codex, and the event-log check (docs/research/core-logic/specs/codex-calls.md).

Every call: an empty scratch folder outside every repository, a read-only sandbox, --ephemeral, an output schema, the
prompt on standard input, images last. A call is accepted only if every item of its event log is an agent message or
reasoning; any other item (a command, a file change, a search, a tool of any kind, an unknown type) rejects the whole
call for tool use. A usage-limit refusal is recognised apart and stops the batch.

    python codex_run.py probe1        # a one-word answer under a schema: must be accepted
    python codex_run.py probe2        # asked to read a canary file: must be rejected for tool use
    python codex_run.py queue add <role> --prompt <f> --schema <f> --out <f> [--image <f> ...] [--id <call id>]
    python codex_run.py queue run     # the background queue (role 2): one call at a time, in order
    python codex_run.py queue status
    python codex_run.py pool --queue <calls file> --writers <2..4>   # the writer pool (role 1)

Each call runs in its own empty scratch folder, <work>\\codex-scratch\\<role>\\<call id>, removed afterwards: two calls
running at once never share one.
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
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import clean_env  # noqa: E402,F401  (removes the owner's two API keys from this process: every launch inherits that)

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
        types = t if isinstance(t, list) else [t]  # a list of types: a key required but nullable ("null" among them)
        ok = any((value is None) if x == "null" else
                 (isinstance(value, kinds[x]) and not (x in ("integer", "number") and isinstance(value, bool)))
                 for x in types)
        if not ok:
            return [f"{path}: expected {' or '.join(types)}"]
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
    scratch = os.path.join(SCRATCH, role, call_id)
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
    env = clean_env.clean_env()
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
    shutil.rmtree(scratch, ignore_errors=True)
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


# --- the background queue (role 2) and the writer pool (role 1): specs/codex-calls.md -------------------------------
QUEUE_DIR = os.path.join(WORK, "codex-queue")
NOTES_PROVENANCE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "docs", "research",
                                "core-logic", "notes", "provenance", "codex-calls.jsonl")
POOL_DIR = os.path.join(LOGS, "write")
PY = r"C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe"


def pid_alive(pid: int) -> bool:
    """Whether a process lives, without signalling it (os.kill on Windows would terminate it)."""
    if pid <= 0:
        return False
    if os.name == "nt":
        import ctypes
        handle = ctypes.windll.kernel32.OpenProcess(0x1000, False, pid)  # PROCESS_QUERY_LIMITED_INFORMATION
        if not handle:
            return False
        code = ctypes.c_ulong()
        ok = ctypes.windll.kernel32.GetExitCodeProcess(handle, ctypes.byref(code))
        ctypes.windll.kernel32.CloseHandle(handle)
        return bool(ok) and code.value == 259  # STILL_ACTIVE
    try:
        os.kill(pid, 0)
        return True
    except OSError:
        return False


def lock_alive(path: str) -> bool:
    try:
        with open(path, encoding="utf-8") as f:
            return pid_alive(int(f.read().strip() or 0))
    except (OSError, ValueError):
        return False


def _now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


class Queue:
    """<work>\\codex-queue\\queue.jsonl: one line per call with its state (waiting, running, accepted, rejected,
    refused, failed); run.lock holds the process id of `queue run`."""

    def __init__(self, folder: str = QUEUE_DIR):
        self.folder = folder
        self.path = os.path.join(folder, "queue.jsonl")
        self.lock = os.path.join(folder, "run.lock")

    def read(self) -> list[dict]:
        if not os.path.isfile(self.path):
            return []
        with open(self.path, encoding="utf-8") as f:
            return [json.loads(x) for x in f if x.strip()]

    def write(self, rows: list[dict]) -> None:
        os.makedirs(self.folder, exist_ok=True)
        tmp = self.path + ".tmp"
        with open(tmp, "w", encoding="utf-8", newline="\n") as f:
            for r in rows:
                f.write(json.dumps(r, ensure_ascii=False) + "\n")
        os.replace(tmp, self.path)

    def add(self, role: str, prompt: str, schema: str, out: str, images: list[str] | None = None,
            call_id: str | None = None, item: str | None = None, after: list[list[str]] | None = None,
            ledger: dict | None = None, batch: str | None = None) -> dict:
        """`item` names what the call is about (a unit's support check, a page): a rejected item is queued once more,
        never a third time (the one rule of a rejected call)."""
        rows = self.read()
        if item:
            mine = [r for r in rows if r.get("item") == item]
            if any(r["state"] in ("waiting", "running") for r in mine):
                raise RuntimeError(f"{item} is already in the queue")
            if sum(r["state"] == "rejected" for r in mine) >= 2:
                raise RuntimeError(f"{item} was rejected twice: set aside, report it; never a third call")
        row = {"id": call_id or f"q-{len(rows) + 1:04d}", "role": role, "prompt": prompt, "schema": schema, "out": out,
               "images": images or [], "item": item, "after": after or [], "ledger": ledger, "batch": batch or role,
               "state": "waiting", "added": _now()}
        if any(r["id"] == row["id"] for r in rows):
            raise RuntimeError(f"call id {row['id']} is already in the queue")
        self.write(rows + [row])
        return row

    def set(self, call_id: str, **changes) -> None:
        rows = self.read()
        for r in rows:
            if r["id"] == call_id:
                r.update(changes)
        self.write(rows)

    def running(self) -> bool:
        return lock_alive(self.lock)

    def start(self) -> bool:
        """Starts `queue run` detached unless it already runs. Returns whether it was started now."""
        if self.running():
            return False
        flags = 0x00000008 | 0x00000200 if os.name == "nt" else 0  # DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP
        os.makedirs(self.folder, exist_ok=True)
        log = open(os.path.join(self.folder, "run.log"), "a", encoding="utf-8")
        subprocess.Popen([PY, "-X", "utf8", os.path.abspath(__file__), "queue", "run"], creationflags=flags,
                         stdout=log, stderr=log, stdin=subprocess.DEVNULL, close_fds=True, env=clean_env.clean_env())
        return True

    def run(self, call_fn=None, ledger_path: str | None = None) -> str:
        """One call at a time, in the order added. Stops on a refusal, on two failed calls in a row, or when the gate
        refuses (the day changed, the CLI changed): running calls finish, nothing new starts, waiting calls stay."""
        call_fn = call_fn or call
        os.makedirs(self.folder, exist_ok=True)
        if self.running():
            return "already running"
        with open(self.lock, "w", encoding="utf-8") as f:
            f.write(str(os.getpid()))
        b = Batch()
        try:
            while True:
                waiting = [r for r in self.read() if r["state"] == "waiting"]
                if not waiting:
                    return "empty"
                row = waiting[0]
                try:
                    gate_open()
                except Gate as e:
                    return f"gate: {e}"
                self.set(row["id"], state="running", started=_now())
                with open(row["prompt"], encoding="utf-8") as f:
                    prompt = f.read()
                r = call_fn(prompt, row["role"], row["batch"], row["id"], row["schema"], images=row["images"] or None,
                            provenance=os.path.join(WORK, "runs", "step1", "codex-calls.jsonl"),
                            template=f"{row['role']}.md")
                state = {ACCEPTED: "accepted", REJECTED: "rejected", REFUSED: "refused"}.get(r["status"], "failed")
                if state == "refused":  # the call did not happen: it waits for the next window, as it was
                    self.set(row["id"], state="waiting", refusedAt=_now(), reason=r.get("reason"))
                else:
                    if state == "accepted":
                        os.makedirs(os.path.dirname(row["out"]), exist_ok=True)
                        with open(row["out"], "w", encoding="utf-8", newline="\n") as f:
                            f.write(json.dumps(r["answer"], ensure_ascii=False, indent=1) + "\n")
                        for argv in row["after"]:
                            done = clean_env.run(argv, capture_output=True)
                            if done.returncode:  # an answer the driver refuses (a word not offered): not used
                                state = "rejected"
                                r["reason"] = "refusedByDriver: " + done.stdout.decode("utf-8", "replace")[-300:] +                                     done.stderr.decode("utf-8", "replace")[-300:]
                                os.remove(row["out"])
                                break
                    self.set(row["id"], state=state, ended=_now(), reason=r.get("reason"), usage=r.get("usage"))
                if row.get("ledger"):
                    line = dict(row["ledger"], ended=_now(), usage=r.get("usage"), reason=r.get("reason"),
                                status={"accepted": "done", "refused": "refused"}.get(state, state),
                                outputs=[row["out"]] if state == "accepted" else [])
                    with open(ledger_path or os.path.join(WORK, "ledger", "runs.jsonl"), "a", encoding="utf-8",
                              newline="\n") as f:
                        f.write(json.dumps(line, ensure_ascii=False) + "\n")
                b.record(r)
                if b.stopped:
                    return f"stopped: {b.why}"
        finally:
            try:
                os.remove(self.lock)
            except OSError:
                pass


def pool(calls_file: str, writers: int, call_fn=None, log_dir: str = POOL_DIR) -> dict:
    """The writer pool (role 1): `writers` calls at once from a calls file written by `notes write` (one JSON line per
    call). A call already in the pool's logs is not run again: a rejected call's scenarios go back through the notes
    tool, as a new call. Two rejections in a row stop one writer; a refusal or a closed gate stops new calls."""
    import threading
    if not 2 <= writers <= 4:
        raise ValueError("the pool runs 2, 3 or 4 writers")
    call_fn = call_fn or call
    os.makedirs(log_dir, exist_ok=True)
    lock_path = os.path.join(log_dir, "pool.lock")
    if lock_alive(lock_path):
        raise RuntimeError("a pool is already running")
    with open(lock_path, "w", encoding="utf-8") as f:
        f.write(str(os.getpid()))
    log_path = os.path.join(log_dir, f"pool-{dt.date.today().isoformat()}.jsonl")
    seen = set()
    for name in os.listdir(log_dir):
        if name.startswith("pool-") and name.endswith(".jsonl"):
            with open(os.path.join(log_dir, name), encoding="utf-8") as f:
                seen |= {json.loads(x)["call"] for x in f if x.strip()}
    with open(calls_file, encoding="utf-8") as f:
        todo = [json.loads(x) for x in f if x.strip()]
    todo = [c for c in todo if c["call"] not in seen]
    guard, state = threading.Lock(), {"stop": None, "stopped": []}

    def take():
        with guard:
            if state["stop"] or not todo:
                return None
            return todo.pop(0)

    def writer(n: int):
        rejected_in_a_row = 0
        while True:
            c = take()
            if c is None:
                return
            try:
                gate_open()
            except Gate as e:
                with guard:
                    state["stop"] = state["stop"] or f"gate: {e}"
                    todo.insert(0, c)
                return
            started = _now()
            with open(c["prompt"], encoding="utf-8") as f:
                prompt = f.read()
            r = call_fn(prompt, "write", c.get("set", "write"), c["call"], c["schema"],
                        provenance=NOTES_PROVENANCE, template="notes-write.md")
            kind = {ACCEPTED: "accepted", REFUSED: "refused"}.get(r["status"]) or \
                ("rejectedToolUse" if r.get("reason") == "toolUse" else "rejectedOther")
            if kind == "accepted":
                os.makedirs(os.path.dirname(c["out"]), exist_ok=True)
                with open(c["out"], "w", encoding="utf-8", newline="\n") as f:
                    f.write(json.dumps(r["answer"], ensure_ascii=False, indent=1) + "\n")
            with guard:
                if kind != "refused":  # a refused call did not happen: it stays for the next pool run
                    with open(log_path, "a", encoding="utf-8", newline="\n") as f:
                        f.write(json.dumps({"call": c["call"], "writer": n, "start": started, "end": _now(),
                                            "result": kind}) + "\n")
                else:
                    state["stop"] = state["stop"] or "usage limit"
                    return
            rejected_in_a_row = rejected_in_a_row + 1 if kind.startswith("rejected") else 0
            if rejected_in_a_row >= 2:
                with guard:
                    state["stopped"].append(n)
                return

    threads = [threading.Thread(target=writer, args=(n,), name=f"writer-{n}") for n in range(1, writers + 1)]
    try:
        for th in threads:
            th.start()
        for th in threads:
            th.join()
    finally:
        try:
            os.remove(lock_path)
        except OSError:
            pass
    return {"stop": state["stop"], "writersStopped": state["stopped"], "left": len(todo)}


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
    if sys.argv[1:] in (["probe1"], ["probe2"]):
        sys.exit(_probe(1 if sys.argv[1:] == ["probe1"] else 2))
    import argparse
    ap = argparse.ArgumentParser(prog="codex_run")
    sp = ap.add_subparsers(dest="cmd", required=True)
    qa = sp.add_parser("queue")
    qa.add_argument("action", choices=["add", "run", "status"])
    qa.add_argument("role", nargs="?")
    for name in ("prompt", "schema", "out", "id", "item"):
        qa.add_argument(f"--{name}")
    qa.add_argument("--image", action="append", default=[])
    pa = sp.add_parser("pool")
    pa.add_argument("--queue", required=True)
    pa.add_argument("--writers", type=int, required=True)
    args = ap.parse_args()
    if args.cmd == "pool":
        print(json.dumps(pool(args.queue, args.writers)))
        sys.exit(0)
    q = Queue()
    if args.action == "add":
        print(json.dumps(q.add(args.role, args.prompt, args.schema, args.out, args.image, args.id, args.item)))
    elif args.action == "run":
        print(q.run())
    else:
        print("queue run:", "running" if q.running() else "not running")
        for r in q.read():
            print(f"{r['id']:<12} {r['state']:<9} {r['role']:<28} {r.get('item') or ''} {r.get('reason') or ''}")
    sys.exit(0)
