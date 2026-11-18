"""The guard on every agent's tool calls (the owner, 18 November 2026: "make each agent's allowed tools a setting it
cannot override"). Each agent definition in the user's agents folder (`dz-reader`, `dz-rules`, `dz-checker`,
`dz-builder`) names its tools in `tools:` and runs this script as a PreToolUse hook with its role:

    python agent_guard.py <reader|rules|checker|builder>      (the hook's JSON on stdin)

Exit 0 lets the call run; exit 2 refuses it, and the reason on stderr goes back to the agent. Why a script and not the
`tools:` list alone: the list cannot narrow Bash to one program, and the tatliq verifier of 18 November ran awk, sed
and a shell append that its prompt did not allow.

The rules, for every role: no path of the hidden sets, of the app's evaluation folders, of the Claude settings,
memory or agents folders; no shell construct (`;`, `&`, `|`, `>`, `<`, a backquote, `$(`, a line break) in a command,
except for the builder, who may pipe and chain but never runs git commands that change the repository. Writes go only
under the role's folders. A command runs only the role's programs (rules: the published rules tool; checker: the
rules tool, oj_fetch.py, pdftotext and pdftoppm on files under the run folders).
"""
from __future__ import annotations

import json
import re
import shlex
import sys

WORKTREE = "c:/users/theshadow/dz-fable-core/"
RESEARCH = "c:/users/theshadow/dz-core-research/"
PYTHON = "c:/users/theshadow/appdata/local/programs/python/python312/python.exe"

DENIED = ("dz-hidden", "/dz-eval", "/dz-fixed", "/temp/dzlegal-", "/.claude/", "/.config/watch")
WRITE_ROOTS = {
    "reader": (),
    "rules": (WORKTREE + "docs/research/core-logic/rules/", WORKTREE + "docs/research/core-logic/law/requests.jsonl",
              RESEARCH + "runs/"),
    "checker": (WORKTREE + "docs/research/core-logic/rules/", WORKTREE + "docs/research/core-logic/law/requests.jsonl",
                RESEARCH + "runs/"),
    "builder": (WORKTREE + "research/core-logic/", WORKTREE + "docs/research/core-logic/"),
}
RULES_TOOL = re.compile(r"^" + re.escape(RESEARCH) + r"tools/rules/[0-9a-f]{7,40}/rules\.exe$")
OJ_FETCH = WORKTREE + "research/core-logic/scripts/oj_fetch.py"
KEY_UNSET = re.compile(r"^unset( (OPENROUTER_API_KEY|GOOGLE_API_KEY)){1,2}$")
SHELL_CONSTRUCT = re.compile(r"[;&|<>`\n]|\$\(")
GIT_CHANGES = re.compile(r"\bgit\b[^|;&]*\b(commit|push|reset|checkout|switch|stash|rebase|merge|cherry-pick|revert|"
                         r"clean|branch\s+-[dD]|worktree|tag|config|update-ref|gc|filter-branch)\b")
READ_TOOLS = {"Read", "Glob", "Grep", "LS", "NotebookRead"}
WRITE_TOOLS = {"Write", "Edit", "MultiEdit", "NotebookEdit"}
SHELL_TOOLS = {"Bash", "PowerShell"}


def norm(path: str) -> str:
    """One spelling for a Windows path: lower case, forward slashes, `C:/` for `/c/`."""
    p = path.strip().strip('"').strip("'").replace("\\", "/").lower()
    p = re.sub(r"/+", "/", p)
    if re.match(r"^/[a-z]/", p):
        p = p[1] + ":" + p[2:]
    return p


def denied_in(text: str) -> str | None:
    t = re.sub(r"(^|[\s'\"=(])/([a-z])/", r"\1\2:/", norm(text))
    return next((d for d in DENIED if d in t), None)


def paths_of(tool_input: dict) -> list[str]:
    return [str(tool_input[k]) for k in ("file_path", "path", "notebook_path", "pattern") if tool_input.get(k)]


def check_command(role: str, command: str) -> str | None:
    if role == "reader":
        return "this agent may not run commands"
    if role == "builder":
        if GIT_CHANGES.search(command):
            return "this agent may not run a git command that changes the repository"
        return None
    if KEY_UNSET.match(command.strip()):  # the prompts' first step (the keys are absent when the gate is clean)
        return None
    if SHELL_CONSTRUCT.search(command):
        return "one plain command only: no ;, &, |, >, <, backquote, $( or line break"
    try:
        words = shlex.split(command.replace("\\", "/"), posix=True)
    except ValueError as e:
        return f"the command does not parse ({e})"
    if not words:
        return "empty command"
    program = norm(words[0])
    if RULES_TOOL.match(program):
        return None
    if role == "checker":
        if program == PYTHON and len(words) > 1 and norm(words[1]) == OJ_FETCH:
            return None
        if re.search(r"(^|/)(pdftotext|pdftoppm)(\.exe)?$", program):
            files = [w for w in words[1:] if not w.startswith("-") and ("/" in w or "." in w) and not w.isdigit()]
            outside = [w for w in files if not norm(w).startswith(RESEARCH + "runs/")]
            return f"pdftotext and pdftoppm work only on files under {RESEARCH}runs/: {outside}" if outside else None
        allowed = "the rules tool, oj_fetch.py (with the Python above), pdftotext, pdftoppm"
    else:
        allowed = "the published rules tool"
    return f"this agent may run only {allowed}; not {words[0]!r}"


def check(role: str, event: dict) -> str | None:
    """The reason to refuse the call, or None to let it run."""
    if role not in WRITE_ROOTS:
        return f"unknown role {role!r}"
    tool, tool_input = event.get("tool_name", ""), event.get("tool_input") or {}
    for p in paths_of(tool_input):
        d = denied_in(p)
        if d:
            return f"no agent may open {d!r} (the owner's rule)"
    if tool in SHELL_TOOLS:
        command = str(tool_input.get("command", ""))
        d = denied_in(command)
        if d:
            return f"no agent may touch {d!r} (the owner's rule)"
        return check_command(role, command)
    if tool in WRITE_TOOLS:
        target = norm(str(tool_input.get("file_path") or tool_input.get("notebook_path") or ""))
        roots = WRITE_ROOTS[role]
        if not any(target.startswith(r) or target == r for r in roots):
            return f"this agent may write only under {list(roots) or 'nothing'}; not {target!r}"
    return None


def main() -> int:
    role = sys.argv[1] if len(sys.argv) > 1 else ""
    try:
        event = json.loads(sys.stdin.read() or "{}")
    except ValueError:
        print("agent_guard: unreadable hook input: refused", file=sys.stderr)
        return 2
    why = check(role, event)
    if why:
        print(f"Refused by the owner's tool settings ({role}): {why}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
