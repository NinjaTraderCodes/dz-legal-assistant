#!/usr/bin/env python3
"""Official-source replay, independent PDF/image extraction and conservative comparison.

All PDF/image/OCR artifacts are development evidence, never a production dependency.
No repair of corrupt source encodings, Arabic folding, guessed dates, or automatic
image-review claims is performed. See README.md for the manifest contract.
"""
from __future__ import annotations

import argparse
from datetime import date, datetime, timezone
import difflib
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / "data/law/corpus/.cache"
TOOLS = ROOT / "tools/law-corpus/.cache"
OFFICIAL_TEXT_HOSTS = frozenset({
    "joradp.dz", "www.joradp.dz", "mjustice.gov.dz", "www.mjustice.gov.dz",
    "droit.mjustice.gov.dz", "mtess.gov.dz", "www.mtess.gov.dz",
    "commerce.gov.dz", "www.commerce.gov.dz",
})
NORMALIZATIONS = ["Unicode NFC (canonical composition only)",
                  "Unicode whitespace runs replaced by one U+0020; leading/trailing whitespace removed"]
SAFE_ID = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,127}\Z")
SHA = re.compile(r"[0-9a-f]{64}\Z")


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", newline="\n", dir=path.parent,
                                         prefix=path.name + ".", suffix=".partial", delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(json.dumps(value, ensure_ascii=False, indent=2) + "\n")
            stream.flush()
            os.fsync(stream.fileno())
        temporary.replace(path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def hash_file(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def hash_text(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def hash_json(value) -> str:
    """Stable structured-data binding; never a normalization of legal text."""
    return hash_text(json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")))


def safe_id(value: str) -> str:
    if not isinstance(value, str) or not SAFE_ID.fullmatch(value):
        raise ValueError(f"Unsafe artifact/source id: {value!r}")
    return value


def official_url(value: str) -> str:
    parsed = urllib.parse.urlsplit(value)
    if (parsed.scheme != "https" or parsed.hostname not in OFFICIAL_TEXT_HOSTS
            or parsed.username is not None or parsed.password is not None
            or parsed.port not in (None, 443) or parsed.fragment):
        raise ValueError(f"Not an allowlisted official article-text HTTPS URL: {value!r}")
    return value


class OfficialRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        official_url(newurl)
        return super().redirect_request(req, fp, code, msg, headers, newurl)


def normalize_for_comparison(value: str) -> str:
    return " ".join(unicodedata.normalize("NFC", value).split())


def compare(layer: str, ocr: str) -> dict:
    left, right = normalize_for_comparison(layer), normalize_for_comparison(ocr)
    matcher = difflib.SequenceMatcher(a=left, b=right, autojunk=False)
    differences = [{"operation": op, "textLayerStart": a, "ocrStart": b,
                    "textLayer": left[a:end_a], "ocr": right[b:end_b]}
                   for op, a, end_a, b, end_b in matcher.get_opcodes() if op != "equal"]
    return {"normalizations": NORMALIZATIONS, "match": bool(left and right) and left == right,
            "textLayerMissing": not bool(left), "ocrMissing": not bool(right),
            "similarityDiagnosticOnly": matcher.ratio(), "differences": differences,
            "preserved": ["Arabic letters and diacritics", "negations", "digits and their script",
                          "article numbers", "punctuation", "tatweel", "bidi/control characters"],
            "paragraphAgreement": "not-assessed-by-whitespace-comparison"}


def load_manifest(path: Path) -> dict:
    manifest = read_json(path)
    if manifest.get("manifestVersion") != 1:
        raise ValueError("manifestVersion must be 1")
    ids = set()
    for source in manifest["sources"]:
        key = safe_id(source["id"])
        if key in ids:
            raise ValueError(f"Duplicate source: {key}")
        ids.add(key)
        official_url(source["url"])
        if not SHA.fullmatch(source["sha256"]):
            raise ValueError(f"Pinned lowercase SHA-256 required: {key}")
        date.fromisoformat(source["retrievedOn"])
    extracts = set()
    for extract in manifest.get("extracts", []):
        key = safe_id(extract["id"])
        if key in extracts or extract["sourceId"] not in ids:
            raise ValueError(f"Duplicate extract or unknown source: {key}")
        extracts.add(key)
        if type(extract["pdfPage"]) is not int or extract["pdfPage"] < 1:
            raise ValueError("pdfPage is a positive one-based PDF page")
        region = extract.get("region", [0, 0, 1, 1])
        if (len(region) != 4 or not all(isinstance(n, (int, float)) for n in region)
                or not 0 <= region[0] < region[2] <= 1
                or not 0 <= region[1] < region[3] <= 1):
            raise ValueError(f"Invalid normalized top-left region: {key}")
        if extract.get("language", "ara") not in ("ara", "fra", "ara+fra"):
            raise ValueError("Only ara/fra language models are configured")
        polygon = extract.get("clipPolygon")
        if polygon is not None and (len(polygon) < 3 or any(len(p) != 2 or any(type(c) not in (int, float) or not 0 <= c <= 1 for c in p) for p in polygon)):
            raise ValueError("clipPolygon must contain at least three normalized page-coordinate pairs")
    return manifest


def fetch_source(source: dict, cache: Path, offline: bool, import_dir: Path | None) -> dict:
    target = cache / f"{safe_id(source['id'])}.pdf"
    cache.mkdir(parents=True, exist_ok=True)
    method = "existing-cache"
    if not target.exists() and import_dir is not None:
        candidate = import_dir / target.name
        if candidate.exists():
            if hash_file(candidate) != source["sha256"]:
                raise ValueError(f"Import hash mismatch for {source['id']}")
            shutil.copyfile(candidate, target)
            method = "hash-validated-local-import"
    if not target.exists():
        if offline:
            raise FileNotFoundError(f"Offline cache missing: {target}")
        opener = urllib.request.build_opener(OfficialRedirect())
        request = urllib.request.Request(official_url(source["url"]),
                                         headers={"User-Agent": "DZLegal-LawCorpus/1.0"})
        temporary = target.with_suffix(".pdf.partial")
        try:
            with opener.open(request, timeout=120) as response, temporary.open("wb") as out:
                official_url(response.url)
                size = 0
                while block := response.read(1024 * 1024):
                    size += len(block)
                    if size > 150 * 1024 * 1024:
                        raise ValueError("Official PDF exceeds the 150 MiB development download limit")
                    out.write(block)
            if hash_file(temporary) != source["sha256"]:
                raise ValueError(f"Downloaded bytes differ from pinned hash: {source['id']}")
            temporary.replace(target)
        finally:
            temporary.unlink(missing_ok=True)
        method = "official-https-download"
    if hash_file(target) != source["sha256"]:
        raise ValueError(f"Cached PDF hash mismatch for {source['id']}; refusing to extract")
    with target.open("rb") as header:
        is_pdf = header.read(1024).lstrip().startswith(b"%PDF-")
    if not is_pdf:
        raise ValueError(f"Not a PDF: {target}")
    return {"sourceId": source["id"], "url": source["url"], "sha256": source["sha256"],
            "originalRetrievedOn": source["retrievedOn"], "replayedOn": date.today().isoformat(),
            "method": method, "byteLength": target.stat().st_size}


def fetch(manifest: dict, cache: Path, offline: bool, import_dir: Path | None) -> list:
    records = [fetch_source(s, cache, offline, import_dir) for s in manifest["sources"]]
    write_json(cache / "replay.json", records)
    return records


def default_tesseract() -> Path:
    local = TOOLS / "ocr/Library/bin/tesseract.exe"
    return local if local.exists() else Path(shutil.which("tesseract") or str(local))


def ocr_image(image_path: Path, output: Path, language: str, psm: int,
              tesseract: Path, tessdata: Path | None, dpi: int) -> dict:
    command = [str(tesseract.resolve()), str(image_path.resolve()), str(output.resolve()),
               "--dpi", str(dpi), "-l", language, "--oem", "1", "--psm", str(psm)]
    if tessdata:
        command += ["--tessdata-dir", str(tessdata.resolve())]
    # Explicit parameters avoid relying on distribution-specific config files.
    command += ["-c", "tessedit_create_txt=1", "-c", "tessedit_create_tsv=1"]
    env = os.environ.copy()
    env["PATH"] = str(tesseract.resolve().parent) + os.pathsep + env.get("PATH", "")
    try:
        result = subprocess.run(command, env=env, capture_output=True, timeout=240,
                                creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
        stdout, stderr, code = result.stdout, result.stderr, result.returncode
    except (OSError, subprocess.TimeoutExpired) as error:
        stdout, stderr, code = b"", str(error).encode("utf-8"), -1
    output.with_suffix(".stdout.bin").write_bytes(stdout)
    output.with_suffix(".stderr.bin").write_bytes(stderr)
    if code != 0:
        output.with_suffix(".txt").write_text("", encoding="utf-8")
    return {"command": command, "exitCode": code,
            "stderr": stderr.decode("utf-8", errors="replace"),
            "executableSha256": hash_file(tesseract) if tesseract.exists() else None,
            "models": [{"language": lang, "sha256": hash_file(tessdata / (lang + ".traineddata"))}
                       for lang in language.split("+") if tessdata and (tessdata / (lang + ".traineddata")).exists()]}


def extract(manifest: dict, cache: Path, dpi: int, tesseract: Path,
            tessdata: Path | None, selected: set[str] | None = None, resume: bool = False) -> list:
    import pypdf
    import pypdfium2 as pdfium

    sources = {source["id"]: source for source in manifest["sources"]}
    evidence = []
    open_docs = {}
    page_cache = {}
    executable_hash = hash_file(tesseract) if tesseract.exists() else None
    for item in manifest.get("extracts", []):
        if selected and item["id"] not in selected:
            continue
        source = sources[item["sourceId"]]
        path = cache / f"{source['id']}.pdf"
        if hash_file(path) != source["sha256"]:
            raise ValueError(f"Source changed before extraction: {path}")
        prior_folder = cache / "extractions" / source["sha256"] / f"page-{item['pdfPage']:04}" / item["id"]
        prior_path = prior_folder / "evidence.json"
        if resume and prior_path.exists():
            prior = read_json(prior_path)
            command = prior.get("ocrInvocation", {}).get("command", [])
            prior_psm = command[command.index("--psm") + 1] if "--psm" in command else None
            models = [{"language": lang, "sha256": hash_file(tessdata / (lang + ".traineddata"))}
                      for lang in item.get("language", "ara").split("+")
                      if tessdata and (tessdata / (lang + ".traineddata")).exists()]
            matches = (prior.get("sourceSha256") == source["sha256"] and prior.get("pdfPage") == item["pdfPage"]
                       and prior.get("region") == item.get("region", [0, 0, 1, 1])
                       and prior.get("clipPolygon") == item.get("clipPolygon")
                       and prior.get("language") == item.get("language", "ara") and prior.get("dpi") == dpi
                       and prior_psm == str(item.get("psm", 6))
                       and prior.get("ocrInvocation", {}).get("exitCode") == 0
                       and prior.get("ocrInvocation", {}).get("executableSha256") == executable_hash
                       and prior.get("ocrInvocation", {}).get("models") == models)
            artifact_names = {Path(a["path"]).name for a in prior.get("artifacts", [])}
            if not {"crop.png", "text-layer.txt", "ocr.txt", "ocr.tsv"} <= artifact_names:
                matches = False
            for artifact in prior.get("artifacts", []):
                candidate = (cache / artifact["path"]).resolve()
                if not candidate.is_relative_to(cache.resolve()) or not candidate.exists() or hash_file(candidate) != artifact["sha256"]:
                    matches = False
                    break
            if matches:
                evidence.append(prior)
                print(f"{item['id']}: resumed hash-validated evidence", flush=True)
                continue
        if source["id"] not in open_docs:
            open_docs[source["id"]] = (pdfium.PdfDocument(path), pypdf.PdfReader(path))
        document, reader = open_docs[source["id"]]
        number = item["pdfPage"]
        if number > len(document):
            raise ValueError(f"Page out of bounds: {item['id']}")
        page_key = (source["id"], number)
        page_folder = cache / "extractions" / source["sha256"] / f"page-{number:04}"
        page_folder.mkdir(parents=True, exist_ok=True)
        if page_key not in page_cache:
            # Consecutive column crops reuse a page; do not retain an entire code's
            # uncompressed page images in RAM on the CPU-only development laptop.
            if len(page_cache) >= 4:
                oldest = next(iter(page_cache))
                old_page, old_image = page_cache.pop(oldest)
                old_image.close()
                old_page.close()
            page = document[number - 1]
            page_image = page.render(scale=dpi / 72).to_pil()
            page_image.save(page_folder / f"page-{dpi}dpi.png")
            raw = reader.pages[number - 1].extract_text() or ""
            (page_folder / "page.text-layer-pypdf.txt").write_text(raw, encoding="utf-8")
            page_cache[page_key] = (page, page_image)
        page, page_image = page_cache[page_key]
        region = item.get("region", [0, 0, 1, 1])
        left, top, right, bottom = region
        pixel_box = [round(left * page_image.width), round(top * page_image.height),
                     round(right * page_image.width), round(bottom * page_image.height)]
        folder = page_folder / item["id"]
        folder.mkdir(exist_ok=True)
        if (folder / "evidence.json").exists():
            previous_hash = hash_file(folder / "evidence.json")
            archive = folder / "previous-attempts" / previous_hash
            archive.mkdir(parents=True, exist_ok=True)
            for previous in list(folder.iterdir()):
                if previous.is_file():
                    shutil.copyfile(previous, archive / previous.name)
        image_path = folder / "crop.png"
        cropped = page_image.crop(tuple(pixel_box))
        if item.get("clipPolygon"):
            from PIL import Image, ImageDraw
            mask = Image.new("L", cropped.size, 0)
            polygon = [(round(x * page_image.width) - pixel_box[0], round(y * page_image.height) - pixel_box[1])
                       for x, y in item["clipPolygon"]]
            ImageDraw.Draw(mask).polygon(polygon, fill=255)
            masked = Image.new(cropped.mode, cropped.size, "white")
            masked.paste(cropped, mask=mask)
            cropped.close()
            cropped = masked
        cropped.save(image_path)
        cropped.close()
        width, height = page.get_size()
        textpage = page.get_textpage()
        raw_layer = textpage.get_text_bounded(left * width, (1 - bottom) * height,
                                              right * width, (1 - top) * height)
        textpage.close()
        (folder / "text-layer.txt").write_text(raw_layer, encoding="utf-8")
        ocr_result = ocr_image(image_path, folder / "ocr", item.get("language", "ara"),
                               item.get("psm", 6), tesseract, tessdata, dpi)
        ocr_path = folder / "ocr.txt"
        raw_ocr = ocr_path.read_text(encoding="utf-8") if ocr_path.exists() else ""
        comparison = compare(raw_layer, raw_ocr)
        artifact_paths = [image_path, folder / "text-layer.txt", page_folder / "page.text-layer-pypdf.txt",
                          ocr_path, folder / "ocr.tsv", folder / "ocr.stderr.bin", folder / "ocr.stdout.bin"]
        record = {"id": item["id"], "sourceId": source["id"], "sourceUrl": source["url"],
                  "sourceSha256": source["sha256"], "sourceRetrievedOn": source["retrievedOn"],
                  "pdfPage": number, "printedPage": item.get("printedPage"), "region": region,
                  **({"clipPolygon": item["clipPolygon"]} if item.get("clipPolygon") else {}),
                  "regionCoordinateSystem": "normalized top-left [left,top,right,bottom]",
                  "pixelBox": pixel_box, "pagePixels": [page_image.width, page_image.height],
                  "dpi": dpi, "language": item.get("language", "ara"),
                  "textLayerMethod": "PDFium get_text_bounded of bounding rectangle; no encoding repair or letter folding; optional polygon mask affects OCR pixels only",
                  "pageTextLayerMethod": "pypdf extract_text; raw and independent of OCR",
                  "renderer": "pypdfium2/" + importlib.metadata.version("pypdfium2"),
                  "pdfReader": "pypdf/" + importlib.metadata.version("pypdf"),
                  "extractedOn": date.today().isoformat(), "ocrInvocation": ocr_result,
                  "comparison": comparison, "imageReviewed": False,
                  "artifacts": [{"path": str(p.relative_to(cache)).replace("\\", "/"),
                                 "sha256": hash_file(p)} for p in artifact_paths if p.exists()]}
        write_json(folder / "evidence.json", record)
        evidence.append(record)
        print(f"{item['id']}: layer={len(raw_layer)} OCR={len(raw_ocr)} "
              f"match={comparison['match']} exit={ocr_result['exitCode']}", flush=True)
    for page, page_image in page_cache.values():
        page_image.close()
        page.close()
    for document, _ in open_docs.values():
        document.close()
    manifest_ids = {item["id"] for item in manifest.get("extracts", [])}
    all_evidence = [record for record, _ in evidence_by_id(cache, manifest_ids).values()]
    write_json(cache / "extraction-report.json", {"generatedAt": datetime.now(timezone.utc).isoformat(),
                "normalizations": NORMALIZATIONS, "evidence": all_evidence,
                "unsettled": [{"id": item["id"], "reason": "Text layer/OCR mismatch or missing; image decision required"}
                              for item in all_evidence if not item["comparison"]["match"]]})
    return evidence


def evidence_by_id(cache: Path, selected_ids: set[str] | None = None) -> dict:
    evidence = {}
    for path in sorted((cache / "extractions").glob("*/page-*/*/evidence.json")):
        item = read_json(path)
        if selected_ids is not None and item["id"] not in selected_ids:
            continue
        if item["id"] in evidence:
            raise ValueError(f"Ambiguous extraction id: {item['id']}")
        for artifact in item["artifacts"]:
            artifact_path = (cache / artifact["path"]).resolve()
            if not artifact_path.is_relative_to(cache.resolve()) or hash_file(artifact_path) != artifact["sha256"]:
                raise ValueError(f"Evidence artifact changed or escaped cache: {item['id']}")
        evidence[item["id"]] = (item, path.parent)
    return evidence


def compose(spec: dict, ledger: dict, cache: Path, out: Path) -> dict:
    """Compose explicit transcriptions; no legal inference or automatic amendment application.

    A review is valid only when its SHA-256 binds the exact canonical transcription
    and rendered image. Declaring a review remains the operator's accountable act.
    """
    requested_ids = {key for article in spec["articles"] for version in article.get("versions", [])
                     for key in version.get("evidenceIds", [])}
    requested_ids.update(fragment["evidenceId"] for article in spec["articles"] for version in article.get("versions", [])
                         for fragment in version.get("evidenceFragments", []))
    evidence = evidence_by_id(cache, requested_ids)
    publications = {item["id"]: item for item in ledger["publications"]}
    inventory_review = validate_inventory_review(spec, ledger)
    articles, unsettled = [], []
    for source_article in spec["articles"]:
        article = {key: value for key, value in source_article.items() if key != "versions"}
        article["versions"] = []
        for source_version in source_article.get("versions", []):
            version = {key: value for key, value in source_version.items()
                       if key not in ("evidenceIds", "evidenceFragments", "imageReview", "verification", "source", "frenchStructure", "sourcePublicationId")}
            if version["introducedBy"] not in publications:
                raise ValueError("Version introducedBy does not exist in ledger")
            publication_id = source_version.get("sourcePublicationId", version["introducedBy"])
            if publication_id != version["introducedBy"]:
                raise ValueError("Top-level source must locate introducedBy; older text belongs in explicit paragraph fragments")
            fragments = source_version.get("evidenceFragments")
            if fragments is not None and "evidenceIds" in source_version:
                raise ValueError("Use evidenceFragments or evidenceIds, not both")
            if fragments is None:
                fragments = [{"evidenceId": key, "publicationId": publication_id}
                             for key in source_version["evidenceIds"]]
            records = []
            for fragment in fragments:
                record, folder = evidence[fragment["evidenceId"]]
                publication = publications.get(fragment["publicationId"])
                if publication is None or record.get("sourceSha256") != publication["arabic"]["sha256"]:
                    raise ValueError("Evidence PDF hash does not match its ledger Arabic publication")
                paragraph = fragment.get("paragraph")
                if fragment.get("role", "body") not in ("body", "heading", "repeal", "amendment-instruction"):
                    raise ValueError("Unknown evidence fragment role")
                if fragment.get("role", "body") != "body" and paragraph is not None:
                    raise ValueError("Only article-body fragments may map to a text paragraph")
                if paragraph is not None and (type(paragraph) is not int or not 1 <= paragraph <= len(version["paragraphs"])):
                    raise ValueError("Fragment paragraph is outside the current transcription")
                records.append((record, folder))
            if not records or not any(f["publicationId"] == publication_id for f in fragments):
                raise ValueError("Version needs evidence locating the introducing/amending publication")
            if len({f["publicationId"] for f in fragments}) > 1:
                if (any(f.get("paragraph") is None for f in fragments if f.get("role", "body") == "body")
                        or {f["paragraph"] for f in fragments if f.get("paragraph") is not None} != set(range(1, len(version["paragraphs"]) + 1))):
                    raise ValueError("Multi-publication consolidation requires explicit provenance for every paragraph")
            layers = [(folder / "text-layer.txt").read_text(encoding="utf-8") for _, folder in records]
            ocrs = [(folder / "ocr.txt").read_text(encoding="utf-8") for _, folder in records]
            comparison = compare("\n".join(layers), "\n".join(ocrs))
            checks, reasons = [], list(source_version.get("verification", {}).get("reasons", []))
            text_proven = comparison["match"] and normalize_for_comparison(version["text"]) == normalize_for_comparison("\n".join(layers))
            if text_proven:
                checks.append({"check": "text-layer-matches-ocr", "method": "; ".join(NORMALIZATIONS),
                               "date": spec["generatedOn"], "result": "passed", "note": "Paragraph structure assessed separately.",
                               "textSha256": hash_text(version["text"])})
            review = source_version.get("imageReview")
            if review:
                images = [next(a["sha256"] for a in r["artifacts"] if a["path"].replace("\\", "/").endswith("/crop.png")) for r, _ in records]
                if (review["textSha256"] != hash_text(version["text"]) or review["imageSha256s"] != images
                        or not review.get("note", "").strip() or review.get("decision") != "transcription-resolved"):
                    raise ValueError(f"Image review is not bound to exact text/images: {version['versionId']}")
                date.fromisoformat(review["reviewedOn"])
                text_proven = True
                checks.append({"check": "discrepancy-resolved-from-image", "method": "Explicit image review bound to text/image SHA-256",
                               "date": review["reviewedOn"], "result": "passed", "note": review["note"],
                               "textSha256": hash_text(version["text"]), "imageSha256s": images})
            for supplied in source_version.get("verification", {}).get("checks", []):
                if supplied["check"] not in ("french-structure-matches", "inventory-complete", "amendment-applied"):
                    raise ValueError("Text/image checks must be generated from actual evidence, not supplied")
                if not supplied.get("method", "").strip() or not supplied.get("note", "").strip():
                    raise ValueError("Explicit structural/inventory/amendment checks require method and evidence note")
                date.fromisoformat(supplied["date"])
                checks.append(supplied)
            if inventory_review:
                for name in ("inventory-complete", "amendment-applied"):
                    if not any(c["check"] == name for c in checks):
                        checks.append({"check": name, "method": inventory_review["method"],
                                       "date": inventory_review["reviewedOn"], "result": "passed",
                                       "note": inventory_review["note"]})
            if not text_proven:
                reasons.append("Raw text layer/OCR disagree, are missing, or do not equal the transcription; no bound image decision.")
            if not inventory_review:
                reasons.append("Independent scoped inventory and amendment-application review remain incomplete.")
            if version.get("temporalStatus") != "resolved":
                reasons.append("Temporal applicability is unsettled.")
            # Full corpus inventory/amendment proof cannot be inferred from a cropped
            # article; the independent ledger/inventory builder must supply those checks.
            state = "checked" if text_proven and inventory_review else "partly-checked" if text_proven else "unsettled"
            version["verification"] = {"state": state, "checks": checks, "reasons": reasons}
            primary_records = [r for (r, _), f in zip(records, fragments) if f["publicationId"] == publication_id]
            first = primary_records[0]
            primary_last_page = max(r["pdfPage"] for r in primary_records)
            version["source"] = {"publicationId": publication_id, "page": first["pdfPage"],
                "endPage": primary_last_page if primary_last_page > first["pdfPage"] else None,
                "printedPage": first.get("printedPage"), "textLayer": "\n".join(layers), "ocr": "\n".join(ocrs),
                "evidenceId": "+".join(r["id"] for r, _ in records), "region": first["region"],
                "fragments": [{"publicationId": f["publicationId"], "page": r["pdfPage"],
                               "textLayer": layer, "ocr": ocr, "evidenceId": r["id"], "region": r["region"],
                               "sourceSha256": r["sourceSha256"],
                               "role": f.get("role", "body"),
                               **({"paragraph": f["paragraph"], "paragraphTextSha256": hash_text(version["paragraphs"][f["paragraph"] - 1])}
                                  if f.get("paragraph") is not None else {})}
                              for (r, _), f, layer, ocr in zip(records, fragments, layers, ocrs)]}
            article["versions"].append(version)
            unsettled += [{"id": version["versionId"], "reason": reason} for reason in reasons]
        articles.append(article)
    expected = ledger["scope"]["targetArticles"]
    expected_uses_ids = bool(expected and ":" in expected[0])
    present = [a["id"] if expected_uses_ids else a["canonicalNumber"]
               for a in articles if a.get("availability", "text") == "text" and a["versions"]]
    gaps = sorted(set(expected) - set(present))
    report = {"schemaVersion": "1.0", "codeId": ledger["id"], "generatedOn": spec["generatedOn"],
              "inventory": {"scope": ledger["scope"]["kind"], "expected": expected, "present": present,
                            "gaps": gaps, "unappliedPublications": spec.get("unappliedPublications", []),
                            "complete": bool(inventory_review)},
              "coverage": "complete" if ledger["scope"]["complete"] else "partial",
              "coveredChapters": ledger["scope"].get("coveredChapters", []),
              "countsByState": {state: sum(v["verification"]["state"] == state for a in articles for v in a["versions"])
                                for state in ("checked", "partly-checked", "unsettled")},
              "unsettled": unsettled}
    write_json(out / "ledger.json", ledger)
    write_json(out / "articles.json", {"schemaVersion": "1.0", "codeId": ledger["id"], "articles": articles})
    write_json(out / "report.json", report)
    return report


def article_versions_digest(spec: dict) -> str:
    """Bind inventory certification to identities, text, paragraphs, paths and version boundaries."""
    return hash_json([{k: a[k] for k in ("id", "canonicalNumber", "number", "path") if k in a} | {
        "versions": [{k: v[k] for k in ("versionId", "text", "paragraphs", "path", "introducedBy", "status",
                                        "inForceFrom", "inForceTo", "temporalStatus", "evidenceIds", "evidenceFragments") if k in v}
                     for v in a.get("versions", [])]}
        for a in spec["articles"]])


def validate_inventory_review(spec: dict, ledger: dict) -> dict | None:
    review = spec.get("inventoryReview")
    if review is None:
        if ledger["scope"]["complete"]:
            raise ValueError("Complete-code declaration requires a bound independent inventory review")
        return None
    expected = ledger["scope"]["targetArticles"]
    present = [a["canonicalNumber"] for a in spec["articles"] if a.get("availability", "text") == "text" and a.get("versions")]
    applied = review.get("publicationsApplied", [])
    if (review.get("ledgerSha256") != hash_json(ledger)
            or review.get("articleVersionsSha256") != article_versions_digest(spec)
            or review.get("expectedArticles") != expected or len(expected) != len(set(expected))
            or set(present) != set(expected) or len(present) != len(expected)
            or set(applied) != {p["id"] for p in ledger["publications"]} or len(applied) != len(set(applied))
            or spec.get("unappliedPublications") or not review.get("method", "").strip()
            or not review.get("note", "").strip()):
        raise ValueError("Inventory review is incomplete or not bound to the current ledger, articles and all publications")
    date.fromisoformat(review["reviewedOn"])
    return review


def validate_corpus(corpus: Path) -> list[str]:
    """Validate committed JSON against local JSON Schema, without fetching schemas."""
    from jsonschema import Draft202012Validator, FormatChecker

    errors = []
    code_count = 0
    schema_dir = corpus / "schema"
    for folder in sorted(corpus.iterdir()):
        if not folder.is_dir() or folder.name.startswith(".") or folder.name in ("schema", "research"):
            continue
        code_count += 1
        for kind in ("ledger", "articles", "report"):
            document = folder / f"{kind}.json"
            schema = schema_dir / f"{kind}.schema.json"
            if not document.exists():
                errors.append(f"{document}: missing")
                continue
            definition = read_json(schema)
            pending = [definition]
            while pending:
                value = pending.pop()
                if isinstance(value, dict):
                    if "$ref" in value and not value["$ref"].startswith("#"):
                        raise ValueError("Offline schema validation rejects external $ref resources")
                    pending.extend(value.values())
                elif isinstance(value, list):
                    pending.extend(value)
            Draft202012Validator.check_schema(definition)
            validator = Draft202012Validator(definition, format_checker=FormatChecker())
            for error in sorted(validator.iter_errors(read_json(document)), key=lambda e: str(e.json_path)):
                errors.append(f"{document}:{error.json_path}: {error.message}")
    if not code_count:
        errors.append(f"{corpus}: no corpus code folders to validate")
    return errors


def export_evidence_index(manifest_path: Path, cache: Path, out: Path) -> dict:
    manifest = load_manifest(manifest_path)
    evidence = evidence_by_id(cache, {x["id"] for x in manifest["extracts"]})
    records = []
    for requested in manifest["extracts"]:
        item, _ = evidence[requested["id"]]
        if (item["sourceId"] != requested["sourceId"] or item["pdfPage"] != requested["pdfPage"]
                or item["region"] != requested.get("region", [0, 0, 1, 1])
                or item.get("clipPolygon") != requested.get("clipPolygon")):
            raise ValueError(f"Extraction differs from committed crop manifest: {requested['id']}")
        invocation = item["ocrInvocation"]
        records.append({key: item[key] for key in ("id", "sourceId", "sourceUrl", "sourceSha256", "sourceRetrievedOn",
                       "pdfPage", "printedPage", "region", "regionCoordinateSystem", "pixelBox", "pagePixels", "dpi",
                       "language", "textLayerMethod", "pageTextLayerMethod", "renderer", "pdfReader", "extractedOn",
                       "comparison", "imageReviewed", "artifacts")} | {
                           "ocr": {key: invocation.get(key) for key in ("exitCode", "executableSha256", "models", "stderr")},
                           **({"clipPolygon": item["clipPolygon"]} if item.get("clipPolygon") else {})})
    index = {"manifestSha256": hash_file(manifest_path), "evidenceCount": len(records), "evidence": records,
             "note": "Image review is never inferred from extraction. Explicit corpus image-review records are separate."}
    write_json(out, index)
    return index


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache", type=Path, default=CACHE)
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("fetch", "extract", "run"):
        command = sub.add_parser(name)
        command.add_argument("manifest", type=Path)
        command.add_argument("--offline", action="store_true")
        command.add_argument("--import-dir", type=Path)
        if name != "fetch":
            command.add_argument("--dpi", type=int, default=300)
            command.add_argument("--tesseract", type=Path, default=default_tesseract())
            command.add_argument("--tessdata", type=Path)
            command.add_argument("--only", action="append")
            command.add_argument("--resume", action="store_true", help="Reuse only hash-validated evidence with identical tools, models, DPI and crop parameters")
    command = sub.add_parser("compose")
    command.add_argument("spec", type=Path)
    command.add_argument("ledger", type=Path)
    command.add_argument("--out", type=Path, required=True)
    command = sub.add_parser("validate")
    command.add_argument("corpus", type=Path, default=ROOT / "data/law/corpus", nargs="?")
    command = sub.add_parser("index")
    command.add_argument("manifest", type=Path)
    command.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    cache = args.cache.resolve()
    if args.command == "index":
        index = export_evidence_index(args.manifest, cache, args.out)
        print(f"Exported {index['evidenceCount']} evidence records")
        return 0
    if args.command == "validate":
        errors = validate_corpus(args.corpus)
        for error in errors:
            print(error, file=sys.stderr)
        print(f"JSON Schema validation: {len(errors)} errors")
        return 1 if errors else 0
    if args.command == "compose":
        report = compose(read_json(args.spec), read_json(args.ledger), cache, args.out)
        print(json.dumps(report["countsByState"]))
        return 0
    manifest = load_manifest(args.manifest)
    fetch(manifest, cache, args.offline, args.import_dir)
    if args.command in ("extract", "run"):
        if not 72 <= args.dpi <= 600:
            parser.error("DPI must be between 72 and 600")
        result = extract(manifest, cache, args.dpi, args.tesseract, args.tessdata,
                         set(args.only) if args.only else None, args.resume)
        return 1 if any(item["ocrInvocation"]["exitCode"] for item in result) else 0
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.SubprocessError) as error:
        print(f"law-corpus: {error}", file=sys.stderr)
        raise SystemExit(2)
