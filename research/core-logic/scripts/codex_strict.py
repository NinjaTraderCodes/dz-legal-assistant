"""Codex's structured output needs a strict schema: every object lists all its properties as required (19 November 2026:
"'schema.properties.notes.items.required' is required to include every key in properties. Missing 'outsideKind'"). This
makes the strict twin of a schema (an optional property becomes required and may be null) and strips those nulls from an
answer, so the answer is checked against the original schema as before.

    python codex_strict.py make <schema.json> <strict.json>
    python codex_strict.py strip <answer.json>          (in place)
"""
from __future__ import annotations

import json
import sys


def strict(node):
    if isinstance(node, list):
        return [strict(x) for x in node]
    if not isinstance(node, dict):
        return node
    out = {k: strict(v) for k, v in node.items()}
    props = out.get("properties")
    if isinstance(props, dict):
        required = set(out.get("required", []))
        for key, sub in props.items():
            if key not in required:
                props[key] = nullable(sub)
        out["required"] = list(props)
        out.setdefault("additionalProperties", False)
    return out


def nullable(sub: dict) -> dict:
    sub = dict(sub)
    t = sub.get("type")
    if isinstance(t, str):
        sub["type"] = [t, "null"]
    elif isinstance(t, list) and "null" not in t:
        sub["type"] = t + ["null"]
    elif t is None:
        return {"anyOf": [sub, {"type": "null"}]}
    if "enum" in sub and None not in sub["enum"]:
        sub["enum"] = list(sub["enum"]) + [None]
    return sub


def strip(node):
    if isinstance(node, list):
        return [strip(x) for x in node]
    if isinstance(node, dict):
        return {k: strip(v) for k, v in node.items() if v is not None}
    return node


def main(argv=None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    if argv[0] == "make":
        with open(argv[1], encoding="utf-8") as f:
            schema = json.load(f)
        with open(argv[2], "w", encoding="utf-8", newline="\n") as f:
            json.dump(strict(schema), f, ensure_ascii=False, indent=1)
            f.write("\n")
    elif argv[0] == "strip":
        with open(argv[1], encoding="utf-8") as f:
            answer = json.load(f)
        with open(argv[1], "w", encoding="utf-8", newline="\n") as f:
            json.dump(strip(answer), f, ensure_ascii=False, indent=1)
            f.write("\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
