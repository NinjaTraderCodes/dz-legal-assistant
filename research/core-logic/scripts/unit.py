"""The driver that takes one unit of step 1 through its stages (docs/research/core-logic/specs/unit-driver.md).

Written command by command in phase 0 (HANDBOOK section 7); each command lands with its tests.

    python unit.py tool        -> publishes the rules tool outside the worktree and makes it the one every script and
                                  agent runs (<work>\\tools\\rules\\<commit>\\rules.exe; current.txt names it)

Why a published copy: a build replaces bin\\Release\\rules.exe, and agents run the tool while the executor builds. A
published copy, one folder per commit, is never replaced; units in flight finish on the copy they started with.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import clean_env  # noqa: E402  (importing it removes the keys from this process)

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
WORK = r"C:\Users\TheShadow\dz-core-research"
TOOLS = os.path.join(WORK, "tools", "rules")
LEDGER = os.path.join(WORK, "ledger", "runs.jsonl")
TOOL_PROJECT = os.path.join(REPO, "research", "core-logic", "src", "DzLegal.CoreLogic.Rules.Tool",
                            "DzLegal.CoreLogic.Rules.Tool.csproj")
# What the published tool is built from: a change here that is not committed would be published under a wrong name.
TOOL_SOURCES = ("research/core-logic/src", "research/core-logic/Directory.Build.props",
                "research/core-logic/Directory.Packages.props")


def published_tool(tools: str = TOOLS) -> str | None:
    """The rules.exe that current.txt names, if it exists."""
    pointer = os.path.join(tools, "current.txt")
    if not os.path.isfile(pointer):
        return None
    with open(pointer, encoding="utf-8") as f:
        path = f.read().strip()
    return path if os.path.isfile(path) else None


def running_agents(ledger: str = LEDGER) -> list[str]:
    """Agent runs whose last ledger line says "running" (a run's lines share its id; the last one is its state)."""
    last: dict[str, dict] = {}
    if os.path.isfile(ledger):
        with open(ledger, encoding="utf-8") as f:
            for line in f:
                if line.strip():
                    row = json.loads(line)
                    last[row["id"]] = row
    return [i for i, row in last.items() if row.get("kind") == "agent" and row.get("status") == "running"]


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


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="unit")
    sub = p.add_subparsers(dest="cmd", required=True)
    sub.add_parser("tool", help="publish the rules tool and make it current")
    a = p.parse_args(argv)
    if a.cmd == "tool":
        try:
            print(publish())
        except RuntimeError as e:
            print(e, file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
