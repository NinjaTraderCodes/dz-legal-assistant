"""The light checks after every commit (HANDBOOK section 3): the commit touches nothing outside the two folders of this
work; the scripts' tests pass; and, when the commit touches what the research solution builds or reads, that solution
builds with 0 warnings and its tests pass.

    python check_commit.py [<commit>]     -> exit 0 when every check passes (default: HEAD)

A failing check is fixed in a new commit; a failing head is never pushed. Every child process starts without the
owner's API keys (clean_env).
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import clean_env  # noqa: E402  (importing it removes the keys from this process)

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
ALLOWED = ("docs/research/core-logic/", "research/core-logic/")
SOLUTION = os.path.join(REPO, "research", "core-logic", "CoreLogic.sln")
TESTS = os.path.join(REPO, "research", "core-logic", "scripts", "tests")

# What the research solution compiles or its tests read: a change there needs the build and the .NET tests.
BUILD_PREFIXES = ("research/core-logic/src/", "research/core-logic/tests/", "docs/research/core-logic/rules/examples/")
BUILD_FILES = {"research/core-logic/CoreLogic.sln", "research/core-logic/Directory.Build.props",
               "research/core-logic/Directory.Packages.props", "docs/research/core-logic/slots.json",
               "docs/research/core-logic/notes/guide-test.json"}


def changed_paths(commit: str) -> list[str]:
    """The paths a commit adds, changes, renames or deletes (both sides of a rename)."""
    out = subprocess.run(["git", "-C", REPO, "show", "--no-renames", "--name-only", "--format=", commit],
                         capture_output=True, text=True, check=True).stdout
    return [line.strip() for line in out.splitlines() if line.strip()]


def outside(paths: list[str]) -> list[str]:
    return [p for p in paths if not p.startswith(ALLOWED)]


def needs_build(paths: list[str]) -> bool:
    return any(p.startswith(BUILD_PREFIXES) or p in BUILD_FILES
               or (p.startswith("docs/research/core-logic/") and p.endswith(".schema.json")) for p in paths)


def zero_warnings(build_output: str) -> bool:
    m = re.search(r"(\d+) Warning\(s\)", build_output)
    return bool(m) and m.group(1) == "0"


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="check_commit")
    p.add_argument("commit", nargs="?", default="HEAD")
    a = p.parse_args(argv)
    paths = changed_paths(a.commit)
    ok = True
    bad = outside(paths)
    if bad:
        ok = False
        print("FAIL outside the two folders:", ", ".join(bad))
    else:
        print(f"ok   {len(paths)} paths, all under the two folders")
    t = clean_env.run([sys.executable, "-X", "utf8", "-m", "pytest", "-q", TESTS], capture_output=True, text=True)
    last = (t.stdout.strip().splitlines() or ["(no output)"])[-1]
    print(("ok   " if t.returncode == 0 else "FAIL ") + "pytest: " + last)
    ok &= t.returncode == 0
    if needs_build(paths):
        b = clean_env.run(["dotnet", "build", "-c", "Release", SOLUTION], capture_output=True, text=True)
        good = b.returncode == 0 and zero_warnings(b.stdout)
        print(("ok   " if good else "FAIL ") + "research solution build (0 warnings required)")
        ok &= good
        if good:
            r = clean_env.run(["dotnet", "test", "-c", "Release", "--no-build", SOLUTION], capture_output=True, text=True)
            print(("ok   " if r.returncode == 0 else "FAIL ") + "research solution tests")
            ok &= r.returncode == 0
    else:
        print("skip research solution: the commit touches nothing it builds or reads")
    print("PASS" if ok else "FAIL: fix it in a new commit; never push a failing head")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
