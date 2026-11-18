# Agent definitions (the owner, 18 November 2026)

"Make each agent's allowed tools a setting it cannot override." Every agent of the research runs under one of these
definitions, installed in the user's agents folder (`C:\Users\TheShadow\.claude\agents\`; Claude Code reads them when a
session starts). Each lists its tools in `tools:` and runs `scripts/agent_guard.py <role>` as a PreToolUse hook on every
tool call; the hook refuses (exit 2) what the role may not do.

| Definition | Tools | May run | May write |
|---|---|---|---|
| dz-reader | Read | nothing | nothing |
| dz-rules | Read, Write, Edit, Glob, Grep, Bash | the published rules tool only | `docs/research/core-logic/rules/`, `law/requests.jsonl`, `dz-core-research\runs\` |
| dz-checker | the same, plus WebSearch, WebFetch | also oj_fetch.py and pdftotext / pdftoppm on run-folder files | the same |
| dz-builder | Read, Write, Edit, Glob, Grep, Bash | anything but a git command that changes the repository | `research/core-logic/`, `docs/research/core-logic/` |

Every role: no path of the hidden sets, `dz-eval`, `dz-fixed`, `%TEMP%\dzlegal-*`, or the Claude settings, memory and
agents folders; for dz-reader, dz-rules and dz-checker, one plain command at a time (no `;`, `&`, `|`, `>`, `<`,
backquote, `$(`). `scripts/unit.py` prints the agent type with every launch. After an edit here, copy the files to the
agents folder and restart the session.
