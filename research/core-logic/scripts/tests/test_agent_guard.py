import json
import subprocess
import sys
import os

import agent_guard as g

RULES = r"C:\Users\TheShadow\dz-core-research\tools\rules\6b439d4\rules.exe"
PY = r"C:\Users\TheShadow\AppData\Local\Programs\Python\Python312\python.exe"
OJ = r"C:\Users\TheShadow\dz-fable-core\research\core-logic\scripts\oj_fetch.py"
RUN = r"C:\Users\TheShadow\dz-core-research\runs\step1\tatliq\oj-verify"
CL = r"C:\Users\TheShadow\dz-fable-core\docs\research\core-logic"


def bash(cmd):
    return {"tool_name": "Bash", "tool_input": {"command": cmd}}


def test_every_role_is_kept_out_of_the_hidden_sets_and_the_settings():
    for role in ("reader", "rules", "checker", "builder"):
        assert g.check(role, {"tool_name": "Read", "tool_input": {"file_path": r"C:\dz-hidden-v3\notes.jsonl"}})
        assert g.check(role, {"tool_name": "Grep", "tool_input": {"pattern": "x", "path": "C:/dz-hidden-v2"}})
        assert g.check(role, {"tool_name": "Read", "tool_input": {"file_path": r"C:\Users\TheShadow\.claude\settings.json"}})
        assert g.check(role, {"tool_name": "Read", "tool_input": {"file_path": r"C:\Users\TheShadow\dz-eval\a.json"}})
        assert g.check(role, bash("cat /c/dz-hidden-v3/x"))
        assert g.check(role, bash("ls /c/Users/TheShadow/dz-eval"))
    assert g.denied_in("cat /c/Users/TheShadow/.claude/settings.json") == "/.claude/"


def test_a_reader_reads_and_does_nothing_else():
    assert g.check("reader", {"tool_name": "Read", "tool_input": {"file_path": CL + r"\notes\LABELLING-GUIDE.md"}}) is None
    assert g.check("reader", bash("echo hi"))
    assert g.check("reader", {"tool_name": "Write", "tool_input": {"file_path": CL + r"\rules\x.json", "content": ""}})


def test_the_rules_role_runs_the_published_rules_tool_only():
    assert g.check("rules", bash(f'"{RULES}" check --store C:/Users/TheShadow/dz-core-research/store/articles.json {CL}\\rules\\a.json')) is None
    assert g.check("rules", bash(RULES.replace("\\", "/") + " examples x.json")) is None
    assert g.check("rules", bash("/c/Users/TheShadow/dz-core-research/tools/rules/6b439d4/rules.exe examples x.json")) is None
    # the tatliq verifier's three commands of 18 November 2026
    assert g.check("rules", bash("awk '{print}' a.json > b.json"))
    assert g.check("rules", bash("sed -i 's/a/b/' x.json"))
    assert g.check("rules", bash("echo '{}' >> requests.jsonl"))
    # the tool chained with something else, or a build copy of the tool
    assert g.check("rules", bash(f"{RULES.replace(chr(92), '/')} examples x.json && rm y"))
    assert g.check("rules", bash(r"C:\Users\TheShadow\dz-fable-core\research\core-logic\src\DzLegal.CoreLogic.Rules.Tool\bin\Release\net10.0\rules.exe x"))
    assert g.check("rules", bash(f'{PY} {OJ} --out {RUN} https://www.joradp.dz/x.pdf'))


def test_the_checker_may_also_fetch_and_convert_journal_pages_in_the_run_folder():
    assert g.check("checker", bash(f'{PY} {OJ} --caller verify-tatliq --wait 600 --out {RUN} https://www.joradp.dz/FTP/jo-arabe/2008/A2008021.pdf')) is None
    assert g.check("checker", bash(f"pdftoppm -r 300 -f 9 -l 9 -png {RUN}\\A2008021.pdf {RUN}\\p9")) is None
    assert g.check("checker", bash(f"pdftotext {RUN}\\A2008021.pdf {RUN}\\a.txt")) is None
    assert g.check("checker", bash(r"pdftotext C:\Users\TheShadow\Desktop\x.pdf C:\Users\TheShadow\x.txt"))
    assert g.check("checker", bash(f"{PY} -c 'import os'"))
    assert g.check("checker", bash("curl https://www.joradp.dz/x.pdf"))


def test_writes_stay_in_the_role_folders():
    ok = {"tool_name": "Write", "tool_input": {"file_path": CL + r"\rules\final-candidates\tatliq.json", "content": ""}}
    assert g.check("rules", ok) is None and g.check("checker", ok) is None
    assert g.check("rules", {"tool_name": "Edit", "tool_input": {"file_path": CL + r"\law\requests.jsonl"}}) is None
    assert g.check("rules", {"tool_name": "Write", "tool_input": {"file_path": CL + r"\HANDBOOK.md", "content": ""}})
    assert g.check("rules", {"tool_name": "Write", "tool_input": {"file_path": CL + r"\law\law-delta.json", "content": ""}})
    assert g.check("rules", {"tool_name": "Write", "tool_input": {"file_path": r"C:\Users\TheShadow\dz-legal-assistant\x.md"}})
    assert g.check("builder", {"tool_name": "Write", "tool_input": {"file_path": r"C:\Users\TheShadow\dz-fable-core\research\core-logic\scripts\x.py"}}) is None
    assert g.check("builder", {"tool_name": "Write", "tool_input": {"file_path": r"C:\Users\TheShadow\dz-fable-core\src\DzLegal.App\x.cs"}})


def test_the_builder_may_chain_commands_but_not_change_the_repository():
    assert g.check("builder", bash("cd /c/Users/TheShadow/dz-fable-core && dotnet test -c Release | tail -3")) is None
    assert g.check("builder", bash("git status --short && git diff --stat")) is None
    for cmd in ("git commit -m x", "git push origin x", "git stash", "git checkout -- a", "git reset --hard",
                "cd x && git -C y commit -q -m z"):
        assert g.check("builder", bash(cmd)), cmd


def test_the_hook_exits_2_with_a_reason_and_0_when_allowed():
    script = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "agent_guard.py")
    run = lambda role, ev: subprocess.run([sys.executable, script, role], input=json.dumps(ev), capture_output=True, text=True)
    r = run("rules", bash("sed -i s/a/b/ x"))
    assert r.returncode == 2 and "Refused by the owner's tool settings" in r.stderr
    assert run("reader", {"tool_name": "Read", "tool_input": {"file_path": CL + r"\HANDBOOK.md"}}).returncode == 0
    assert run("nobody", {"tool_name": "Read", "tool_input": {}}).returncode == 2
