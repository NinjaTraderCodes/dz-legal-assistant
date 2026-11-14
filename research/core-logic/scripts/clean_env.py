"""The one environment every launch of this work starts from: the owner's two API keys removed (owner's decision of
14 November 2026: "make the removal automatic rather than a prompt instruction"). Both are Windows user variables, so
every process inherits them unless something removes them; this module is that something, for every Codex call and
every command a script or the executor launches.

    python clean_env.py <command> [<arg> ...]     -> runs the command with both keys removed, returns its exit code
    python clean_env.py --check                   -> exit 0 when this process was started without either key

In Python, importing this module removes the keys from the importing process (scrub), so every subprocess it starts
inherits a clean environment; `clean_env()` and `run()` give the same guarantee for an explicit `env`.
"""
from __future__ import annotations

import os
import subprocess
import sys

KEYS = ("OPENROUTER_API_KEY", "GOOGLE_API_KEY")


def clean_env(base: dict | None = None) -> dict:
    """A copy of `base` (default: this process's environment) without the keys, whatever their case."""
    env = dict(os.environ if base is None else base)
    for name in list(env):
        if name.upper() in KEYS:
            del env[name]
    return env


def run(cmd, **kwargs) -> subprocess.CompletedProcess:
    """subprocess.run with the clean environment; an `env` given is cleaned too, never used as it is."""
    kwargs["env"] = clean_env(kwargs.get("env"))
    return subprocess.run(cmd, **kwargs)


def holds_a_key(env: dict | None = None) -> list[str]:
    env = os.environ if env is None else env
    return sorted(name for name in env if name.upper() in KEYS and env[name] != "")


def scrub() -> None:
    """Removes the keys from this process's own environment, so that everything it starts inherits that."""
    for name in list(os.environ):
        if name.upper() in KEYS:
            del os.environ[name]


INHERITED = holds_a_key()  # what this process was started with, for --check (names only)
scrub()  # importing this module is enough: every script of this folder imports it, directly or through store.py


def main(argv: list[str]) -> int:
    if argv == ["--check"]:
        found = INHERITED
        print("clean" if not found else "holds " + ", ".join(found))  # names only, never a value
        return 1 if found else 0
    if not argv:
        print(__doc__)
        return 2
    return run(argv).returncode


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
