"""Copying a no-tool agent's answer from its transcript (specs/unit-driver.md, "records.py ingest-reply").

An agent's answer is never retyped: a script copies it from the transcript Claude Code writes for each agent, checks
that the agent made no tool call but the one read of its own prompt file (and the harness's hand-back), checks the
answer against its schema, and writes it. Used through `records.py ingest-reply`.

Never run on the transcript of an agent that saw a hidden set or the mirror set.
"""
from __future__ import annotations

import json
import glob
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import codex_run  # noqa: E402  (its schema validator)

PROJECTS = os.environ.get("DZ_AGENT_PROJECTS", r"C:\Users\TheShadow\.claude\projects\c--Users-TheShadow-dz-legal-assistant")
HANDBACK = {"SubagentHandback", "StructuredOutput"}


class Refused(Exception):
    def __init__(self, code: int, message: str):
        super().__init__(message)
        self.code = code


def transcript_path(agent: str, session: str | None = None, projects: str = PROJECTS) -> str:
    """The agent's transcript: <projects>\\<session>\\subagents\\agent-<id>.jsonl, in the named session or, by default,
    the newest session that holds it."""
    sessions = [session] if session else sorted(
        (d for d in os.listdir(projects) if os.path.isdir(os.path.join(projects, d, "subagents"))),
        key=lambda d: os.path.getmtime(os.path.join(projects, d, "subagents")), reverse=True)
    for s in sessions:
        path = os.path.join(projects, s, "subagents", f"agent-{agent}.jsonl")
        if os.path.isfile(path):
            return path
        # An agent of a workflow keeps its transcript in the workflow run's folder.
        for run in glob.glob(os.path.join(projects, s, "subagents", "workflows", "*", f"agent-{agent}.jsonl")):
            return run
    raise Refused(2, f"no transcript for agent {agent} under {projects}")


def read_transcript(path: str) -> tuple[list[dict], str | None, str | None]:
    """The tool calls, the hand-back message (if any) and the last assistant text."""
    calls, handback, last_text = [], None, None
    with open(path, encoding="utf-8") as f:
        for line in f:
            d = json.loads(line)
            if d.get("type") != "assistant":
                continue
            for c in d.get("message", {}).get("content") or []:
                if c.get("type") == "tool_use":
                    calls.append(c)
                    if c["name"] == "SubagentHandback":
                        handback = c["input"].get("message")
                elif c.get("type") == "text" and c.get("text", "").strip():
                    last_text = c["text"]
    return calls, handback, last_text


def _same(a: str, b: str) -> bool:
    return os.path.normcase(os.path.normpath(a)) == os.path.normcase(os.path.normpath(b))


def check_calls(calls: list[dict], prompt: str) -> list[str]:
    """Problems: any tool call other than one Read of the prompt file and the hand-back."""
    problems, reads = [], 0
    for c in calls:
        if c["name"] in HANDBACK:
            continue
        if c["name"] == "Read" and _same(c["input"].get("file_path", ""), prompt):
            reads += 1
            continue
        target = c["input"].get("file_path") or c["input"].get("command") or c["input"].get("pattern") or ""
        problems.append(f"tool {c['name']} {str(target)[:120]}")
    if reads > 1:
        problems.append(f"{reads} reads of the prompt file")
    return problems


def parse_answer(text: str):
    """The whole text must be one JSON value; one surrounding ```json fence is allowed."""
    t = text.strip()
    m = re.fullmatch(r"```(?:json)?\s*(.*?)\s*```", t, flags=re.S)
    if m:
        t = m.group(1)
    return json.loads(t)


def ingest(agent: str, prompt: str, schema_path: str, out: str, ids: str | None = None, unit: str | None = None,
           append: bool = False, session: str | None = None, transcript: str | None = None) -> dict:
    path = transcript or transcript_path(agent, session)
    calls, handback, last_text = read_transcript(path)
    problems = check_calls(calls, prompt)
    if problems:
        raise Refused(3, "the run used tools beyond its one read: " + "; ".join(problems))
    text = handback if handback is not None else last_text
    if text is None:
        raise Refused(4, "the agent gave no answer")
    try:
        answer = parse_answer(text)
    except json.JSONDecodeError as e:
        raise Refused(4, f"the answer is not one JSON value: {e}")
    with open(schema_path, encoding="utf-8") as f:
        errors = codex_run.validate(json.load(f), answer)
    if errors:
        raise Refused(4, "the answer does not match its schema: " + "; ".join(errors[:10]))
    if ids:
        old = []
        if append and os.path.isfile(out):
            with open(out, encoding="utf-8") as f:
                old = json.load(f).get("attacks", [])
        start = len(old)
        for n, a in enumerate(answer.get("attacks", []), start + 1):
            a["id"] = f"{ids}-{n}"
        answer["attacks"] = old + answer.get("attacks", [])
    if unit:
        answer = {"unit": unit, **answer}
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(answer, f, ensure_ascii=False, indent=1)
        f.write("\n")
    return {"transcript": path, "toolCalls": len(calls), "out": out}
