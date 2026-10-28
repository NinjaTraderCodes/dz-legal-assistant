#!/usr/bin/env python3
"""Review sheets for image decisions: each item shows the official page crop beside the native text, with the
unconfirmed tokens highlighted, rendered by headless Microsoft Edge (correct Arabic shaping) to PNG.

The sheets only help a person look; they decide nothing. Usage (library):
    render(items, out_folder, prefix, per_sheet=6)
where each item is {"id", "title", "images": [png paths], "text", "highlight": [token indices], "note"}.
Returns the list of PNG paths with the SHA-256 of every source image, so decisions can be bound to them.
"""
from __future__ import annotations

import html
import subprocess
from pathlib import Path

from PIL import Image

import native_check as nc
import pipeline

EDGE = Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")
FONT = pipeline.ROOT / "assets/fonts/NotoNaskhArabic-Regular.ttf"


def _text_html(text: str, highlight: set[int]) -> str:
    out, i = [], 0
    for paragraph in text.split("\n"):
        parts = []
        for tok in nc.tokens(paragraph):
            esc = html.escape(tok)
            parts.append(f'<mark>{esc}</mark>' if i in highlight else esc)
            i += 1
        out.append("<p>" + " ".join(parts) + "</p>")
    return "".join(out)


def render(items: list[dict], out: Path, prefix: str, per_sheet: int = 6) -> list[dict]:
    out.mkdir(parents=True, exist_ok=True)
    sheets = []
    for start in range(0, len(items), per_sheet):
        chunk = items[start:start + per_sheet]
        blocks = []
        for item in chunk:
            imgs = "".join(f'<img src="{Path(p).resolve().as_uri()}">' for p in item["images"])
            blocks.append(f'<section><h2>{html.escape(item["title"])}</h2><div class="img">{imgs}</div>'
                          f'<div class="txt" dir="rtl">{_text_html(item["text"], set(item.get("highlight", [])))}</div>'
                          f'<div class="note" dir="ltr">{html.escape(item.get("note", ""))}</div></section>')
        page = ("<!doctype html><html><head><meta charset='utf-8'><style>"
                f"@font-face{{font-family:Naskh;src:url('{FONT.resolve().as_uri()}')}}"
                "body{margin:8px;background:#fff;font-family:Naskh,serif} section{border-bottom:3px solid #555;padding:6px 0}"
                "h2{font:bold 18px sans-serif;margin:2px 0}"
                ".img img{display:block;margin-bottom:4px;border:1px solid #999}"
                ".txt{font-size:24px;line-height:1.6;width:1054px} .txt p{margin:0} mark{background:#ffd23f}"
                ".note{font:12px sans-serif;color:#333;margin-top:4px}</style></head><body>"
                + "".join(blocks) + "</body></html>")
        name = f"{prefix}-{start // per_sheet + 1:03}"
        html_path = out / f"{name}.html"
        html_path.write_text(page, encoding="utf-8")
        png = out / f"{name}.png"
        height = 60 + sum(120 + sum(Image.open(p).height for p in i["images"]) + 40 * (1 + len(i["text"]) // 70) for i in chunk)
        subprocess.run([str(EDGE), "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
                        f"--window-size=1080,{height}", f"--screenshot={png.resolve()}", html_path.resolve().as_uri()],
                       capture_output=True, timeout=120)
        sheets.append({"sheet": str(png), "sheetSha256": pipeline.hash_file(png) if png.exists() else None,
                       "items": [{"id": i["id"], "imageSha256s": [pipeline.hash_file(Path(p)) for p in i["images"]]} for i in chunk]})
    return sheets
