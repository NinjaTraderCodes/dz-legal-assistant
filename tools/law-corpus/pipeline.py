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
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def hash_file(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def hash_text(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


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
            tessdata: Path | None, selected: set[str] | None = None) -> list:
    import pypdf
    import pypdfium2 as pdfium

    sources = {source["id"]: source for source in manifest["sources"]}
    evidence = []
    open_docs = {}
    page_cache = {}
    for item in manifest.get("extracts", []):
        if selected and item["id"] not in selected:
            continue
        source = sources[item["sourceId"]]
        path = cache / f"{source['id']}.pdf"
        if hash_file(path) != source["sha256"]:
            raise ValueError(f"Source changed before extraction: {path}")
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
        page_image.crop(tuple(pixel_box)).save(image_path)
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
                  "regionCoordinateSystem": "normalized top-left [left,top,right,bottom]",
                  "pixelBox": pixel_box, "pagePixels": [page_image.width, page_image.height],
                  "dpi": dpi, "language": item.get("language", "ara"),
                  "textLayerMethod": "PDFium get_text_bounded; no encoding repair or letter folding",
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
    for document, _ in open_docs.values():
        document.close()
    manifest_ids = {item["id"] for item in manifest.get("extracts", [])}
    all_evidence = [record for record, _ in evidence_by_id(cache).values() if record["id"] in manifest_ids]
    write_json(cache / "extraction-report.json", {"generatedAt": datetime.now(timezone.utc).isoformat(),
                "normalizations": NORMALIZATIONS, "evidence": all_evidence,
                "unsettled": [{"id": item["id"], "reason": "Text layer/OCR mismatch or missing; image decision required"}
                              for item in all_evidence if not item["comparison"]["match"]]})
    return evidence


def evidence_by_id(cache: Path) -> dict:
    evidence = {}
    for path in sorted((cache / "extractions").glob("*/page-*/*/evidence.json")):
        item = read_json(path)
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
    evidence = evidence_by_id(cache)
    publications = {item["id"]: item for item in ledger["publications"]}
    articles, unsettled = [], []
    for source_article in spec["articles"]:
        article = {key: value for key, value in source_article.items() if key != "versions"}
        article["versions"] = []
        for source_version in source_article.get("versions", []):
            version = {key: value for key, value in source_version.items()
                       if key not in ("evidenceIds", "imageReview", "verification", "source", "frenchStructure", "sourcePublicationId")}
            records = [evidence[key] for key in source_version["evidenceIds"]]
            if not records or len({r[0]["sourceId"] for r in records}) != 1:
                raise ValueError("Each version requires evidence crops from exactly one source PDF")
            if version["introducedBy"] not in publications:
                raise ValueError("Version introducedBy does not exist in ledger")
            publication_id = source_version.get("sourcePublicationId", version["introducedBy"])
            if publication_id not in publications:
                raise ValueError("Evidence publication is not in ledger")
            publication = publications[publication_id]
            if any(r.get("sourceSha256") != publication["arabic"]["sha256"] for r, _ in records):
                raise ValueError("Evidence PDF hash does not match its ledger Arabic publication")
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
            if not text_proven:
                reasons.append("Raw text layer/OCR disagree, are missing, or do not equal the transcription; no bound image decision.")
            if not ledger["scope"]["complete"]:
                reasons.append("Code inventory and amendment discovery remain incomplete.")
            if version.get("temporalStatus") != "resolved":
                reasons.append("Temporal applicability is unsettled.")
            # Full corpus inventory/amendment proof cannot be inferred from a cropped
            # article; the independent ledger/inventory builder must supply those checks.
            state = "partly-checked" if text_proven else "unsettled"
            version["verification"] = {"state": state, "checks": checks, "reasons": reasons}
            first = records[0][0]
            version["source"] = {"publicationId": publication_id, "page": first["pdfPage"],
                "endPage": records[-1][0]["pdfPage"] if len(records) > 1 else None,
                "printedPage": first.get("printedPage"), "textLayer": "\n".join(layers), "ocr": "\n".join(ocrs),
                "evidenceId": "+".join(r["id"] for r, _ in records), "region": first["region"],
                "fragments": [{"publicationId": publication_id, "page": r["pdfPage"],
                               "textLayer": layer, "ocr": ocr, "evidenceId": r["id"], "region": r["region"]}
                              for (r, _), layer, ocr in zip(records, layers, ocrs)]}
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
                            "complete": False},
              "countsByState": {state: sum(v["verification"]["state"] == state for a in articles for v in a["versions"])
                                for state in ("checked", "partly-checked", "unsettled")},
              "unsettled": unsettled}
    write_json(out / "ledger.json", ledger)
    write_json(out / "articles.json", {"schemaVersion": "1.0", "codeId": ledger["id"], "articles": articles})
    write_json(out / "report.json", report)
    return report


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
    evidence = evidence_by_id(cache)
    records = []
    for requested in manifest["extracts"]:
        item, _ = evidence[requested["id"]]
        if (item["sourceId"] != requested["sourceId"] or item["pdfPage"] != requested["pdfPage"]
                or item["region"] != requested.get("region", [0, 0, 1, 1])):
            raise ValueError(f"Extraction differs from committed crop manifest: {requested['id']}")
        invocation = item["ocrInvocation"]
        records.append({key: item[key] for key in ("id", "sourceId", "sourceUrl", "sourceSha256", "sourceRetrievedOn",
                       "pdfPage", "printedPage", "region", "regionCoordinateSystem", "pixelBox", "pagePixels", "dpi",
                       "language", "textLayerMethod", "pageTextLayerMethod", "renderer", "pdfReader", "extractedOn",
                       "comparison", "imageReviewed", "artifacts")} | {
                           "ocr": {key: invocation.get(key) for key in ("exitCode", "executableSha256", "models", "stderr")}})
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
                         set(args.only) if args.only else None)
        return 1 if any(item["ocrInvocation"]["exitCode"] for item in result) else 0
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.SubprocessError) as error:
        print(f"law-corpus: {error}", file=sys.stderr)
        raise SystemExit(2)
