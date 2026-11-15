"""The light checks of HANDBOOK section 3: which paths are allowed, when the research solution must be built."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import check_commit  # noqa: E402


def test_only_the_two_folders_are_allowed():
    paths = ["docs/research/core-logic/HANDBOOK.md", "research/core-logic/scripts/x.py", "src/DzLegal.App/App.axaml",
             "global.json", "docs/research/other.md", "research/core-logicX/y.py"]
    assert check_commit.outside(paths) == ["src/DzLegal.App/App.axaml", "global.json", "docs/research/other.md",
                                           "research/core-logicX/y.py"]


def test_the_build_runs_only_when_the_solution_builds_or_reads_what_changed():
    assert not check_commit.needs_build(["docs/research/core-logic/HANDBOOK.md", "research/core-logic/scripts/a.py"])
    assert check_commit.needs_build(["research/core-logic/src/DzLegal.CoreLogic.Rules/Evaluator.cs"])
    assert check_commit.needs_build(["research/core-logic/tests/X/Y.cs"])
    assert check_commit.needs_build(["docs/research/core-logic/rules/rule-record.schema.json"])
    assert check_commit.needs_build(["docs/research/core-logic/prompts/schemas/attacks.schema.json"])
    assert check_commit.needs_build(["docs/research/core-logic/slots.json"])
    assert check_commit.needs_build(["docs/research/core-logic/rules/examples/claim.unpaid-wages.example.json"])


def test_a_build_with_a_warning_fails():
    assert check_commit.zero_warnings("Build succeeded.\n    0 Warning(s)\n    0 Error(s)")
    assert not check_commit.zero_warnings("Build succeeded.\n    2 Warning(s)\n    0 Error(s)")
    assert not check_commit.zero_warnings("no summary at all")


def test_the_paths_of_a_real_commit_are_read():
    paths = check_commit.changed_paths("733de0f")
    assert paths == ["docs/research/core-logic/prompts/journal-hit-triage.md"]
