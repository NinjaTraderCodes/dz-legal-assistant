"""Text views of the article store, one file per code, so that a deriver can read whole chapters (the store's JSON file
is too large to read whole): <working folder>\\store\\text\\<code>.txt. The same entries as the store; every paragraph is
labelled as a cite counts it, «[civil-code 179 ¶1] …». Headings: the store's path, or where it has none, the official
consolidated edition's (records.consolidated_chapters), marked as such. Only headings come from the edition.

    python store_text.py [--store <articles.json>] [--out <folder>]
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import store as st  # noqa: E402

DEFAULT_TEXT = os.path.join(os.path.dirname(st.DEFAULT_OUT), "text")
EDITION_CODES = ("civil-code", "commercial-code")  # the codes with an official consolidated edition (consolidated.PDFS)


def render(code: str, entries: list[dict], edition: dict, store_sha: str = "") -> str:
    entries = sorted(entries, key=lambda e: (st._num_key(e["article"]), not e["inForce"], e["version"]))
    lines = [f"# {code}: the article store as text (store sha256 {store_sha[:16]}). Cite as the brackets show: code, "
             "article, paragraph number.",
             "# Each article's line says its version; «not in force» marks an older wording, «repealed» a repealed "
             "article.", ""]
    last = None
    for e in entries:
        path = e.get("path") or []
        if path:
            head = " / ".join(path)
        elif edition.get(e["article"]):
            head = " / ".join(edition[e["article"]]) + " [headings from the official consolidated edition]"
        else:
            head = ""
        if head and head != last:
            lines += ["", f"## {head}"]
            last = head
        tag = []
        if e["repealed"]:
            tag.append("repealed")
        elif not e["inForce"]:
            tag.append("not in force: an older wording")
        tag.append(f"version {e['version']}")
        note = e.get("inForceNote")
        if note:
            tag.append("note: " + (note.get("lead", "") if isinstance(note, dict) else str(note)))
        lines.append(f"### {code} {e['article']} ({'; '.join(tag)})")
        if e["repealed"]:
            lines.append("(repealed: no text to cite)")
            continue
        lines += [f"[{code} {e['article']} ¶{n}] {p}" for n, p in enumerate(e["paragraphs"], 1)]
    return "\n".join(lines) + "\n"


def main(argv=None) -> int:
    p = argparse.ArgumentParser(prog="store_text")
    p.add_argument("--store", default=st.DEFAULT_OUT)
    p.add_argument("--out", default=DEFAULT_TEXT)
    a = p.parse_args(argv)
    sys.stdout.reconfigure(encoding="utf-8")
    with open(a.store, "rb") as f:
        raw = f.read()
    doc, store_sha = json.loads(raw.decode("utf-8")), hashlib.sha256(raw).hexdigest()
    os.makedirs(a.out, exist_ok=True)
    import records as rc
    for code in sorted({e["code"] for e in doc["entries"]}):
        entries = [e for e in doc["entries"] if e["code"] == code]
        edition = rc.consolidated_chapters(code) if code in EDITION_CODES else {}
        with open(os.path.join(a.out, f"{code}.txt"), "w", encoding="utf-8", newline="\n") as f:
            f.write(render(code, entries, edition, store_sha))
        print(code, len(entries), "entries")
    return 0


if __name__ == "__main__":
    sys.exit(main())
