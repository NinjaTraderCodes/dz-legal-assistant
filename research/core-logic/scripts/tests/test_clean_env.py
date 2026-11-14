"""The owner's decision of 14 Nov 2026: every launch starts in an environment with neither API key. The tests set dummy
values (never a real key) and check what the launched processes see."""
import os
import subprocess
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import clean_env  # noqa: E402
import codex_run  # noqa: E402

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DUMMY = {"OPENROUTER_API_KEY": "dummy-not-a-key", "GOOGLE_API_KEY": "dummy-not-a-key", "Google_Api_Key": "dummy"}


def with_keys() -> dict:
    return dict(os.environ, **DUMMY)


def test_clean_env_removes_both_keys_whatever_their_case_and_keeps_the_rest():
    env = clean_env.clean_env(dict(DUMMY, PATH="p", OTHER="x"))
    assert clean_env.holds_a_key(env) == [] and env["OTHER"] == "x" and env["PATH"] == "p"


def test_the_command_wrapper_starts_its_command_without_the_keys():
    check = [sys.executable, os.path.join(HERE, "clean_env.py"), "--check"]
    assert subprocess.run(check, env=with_keys(), capture_output=True).returncode == 1  # the check itself sees them
    wrapped = [sys.executable, os.path.join(HERE, "clean_env.py"), *check]
    p = subprocess.run(wrapped, env=with_keys(), capture_output=True, text=True)
    assert p.returncode == 0 and p.stdout.strip() == "clean"


def test_a_script_that_imports_the_store_launches_clean_processes():
    code = ("import subprocess, sys; import store; "
            f"sys.exit(subprocess.run([sys.executable, {os.path.join(HERE, 'clean_env.py')!r}, '--check']).returncode)")
    p = subprocess.run([sys.executable, "-c", code], env=with_keys(), cwd=HERE, capture_output=True, text=True)
    assert p.returncode == 0, p.stdout + p.stderr


def test_a_codex_call_runs_in_an_environment_without_the_keys(tmp_path, monkeypatch):
    for k, v in DUMMY.items():
        monkeypatch.setenv(k, v)
    monkeypatch.setattr(codex_run, "SCRATCH", str(tmp_path / "scratch"))
    monkeypatch.setattr(codex_run, "LOGS", str(tmp_path / "logs"))
    seen = {}

    class Done:
        stdout, stderr, returncode = b"", b"", 0

    def fake_run(cmd, **kw):
        seen["env"] = kw["env"]
        return Done()

    monkeypatch.setattr(codex_run.subprocess, "run", fake_run)
    monkeypatch.setattr(codex_run, "version", lambda: "codex-cli test")
    schema = tmp_path / "s.json"
    schema.write_text('{"type": "object"}', encoding="utf-8")
    codex_run.call("p", "role", "batch", "c1", str(schema), _probe=True)
    assert clean_env.holds_a_key(seen["env"]) == []
