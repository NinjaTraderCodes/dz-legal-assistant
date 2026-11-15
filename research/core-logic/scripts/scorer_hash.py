"""The scorer's hash (docs/research/core-logic/specs/scorer.md, "The scorer's hash"): the SHA-256 of a list, the path and
SHA-256 of each source file as committed (`git show <commit>:<path>`), sorted by path. A built program is not hashed.

    python scorer_hash.py [<commit>]        -> prints the hash and the list it was computed from

The files: every tracked file of DzLegal.CoreLogic.Scoring and DzLegal.CoreLogic.Contracts (the scorer parses readings
with the contract), the loader of DzLegal.CoreLogic.Notes, and the scorer's specification."""
from __future__ import annotations

import hashlib
import os
import subprocess
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
FOLDERS = ("research/core-logic/src/DzLegal.CoreLogic.Scoring/", "research/core-logic/src/DzLegal.CoreLogic.Contracts/")
FILES = ("research/core-logic/src/DzLegal.CoreLogic.Notes/NotesLoader.cs", "docs/research/core-logic/specs/scorer.md")


def _git(*args: str) -> bytes:
    return subprocess.run(["git", "-C", REPO, *args], capture_output=True, check=True).stdout


def covered(commit: str) -> list[str]:
    tracked = _git("ls-tree", "-r", "--name-only", commit).decode("utf-8").splitlines()
    return sorted(p for p in tracked if p.startswith(FOLDERS) or p in FILES)


def listing(commit: str) -> str:
    return "".join(f"{p} {hashlib.sha256(_git('show', f'{commit}:{p}')).hexdigest()}\n" for p in covered(commit))


def scorer_hash(commit: str = "HEAD") -> str:
    return hashlib.sha256(listing(commit).encode("utf-8")).hexdigest()


def main(argv=None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    commit = argv[0] if argv else "HEAD"
    sys.stdout.reconfigure(encoding="utf-8")
    print(scorer_hash(commit))
    print(listing(commit), end="")
    return 0


if __name__ == "__main__":
    sys.exit(main())
