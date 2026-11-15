"""The scorer's hash (specs/scorer.md): a list of committed files, sorted, never a built program."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import scorer_hash  # noqa: E402


def test_the_hash_covers_the_scorer_the_contract_the_loader_and_the_spec_as_committed():
    files = scorer_hash.covered("HEAD")
    assert "docs/research/core-logic/specs/scorer.md" in files
    assert "research/core-logic/src/DzLegal.CoreLogic.Notes/NotesLoader.cs" in files
    assert any(f.startswith("research/core-logic/src/DzLegal.CoreLogic.Contracts/") for f in files)
    assert not any("/bin/" in f or "/obj/" in f for f in files)
    assert files == sorted(files)
    h = scorer_hash.scorer_hash("HEAD")
    assert len(h) == 64 and h == scorer_hash.scorer_hash("HEAD")
