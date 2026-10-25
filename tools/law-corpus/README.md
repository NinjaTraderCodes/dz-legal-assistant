# Law corpus development pipeline

These Python tools create reproducible evidence from official publications. The application reads committed JSON only. Nothing here changes the existing code loaders, source JSON or the legacy `verified` fields.

## Setup (Windows, Python 3.12)

Run from the repository root. `uv` may use any installed CPython 3.12 interpreter; override `--python 3.12` with its absolute path if the Windows launcher is unavailable.

```powershell
$env:UV_CACHE_DIR = Join-Path (Get-Location) 'tools/law-corpus/.cache/uv'
uv venv tools/law-corpus/.cache/venv --python 3.12
uv pip install --python tools/law-corpus/.cache/venv/Scripts/python.exe --require-hashes -r tools/law-corpus/requirements.lock
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/bootstrap_ocr.py
```

`requirements.txt` pins Python packages and transitive validator dependencies; the generated `requirements.lock` adds distribution SHA-256 hashes. `toolchain-windows-lock.json` pins exact conda-forge package URLs, SHA-256, versions, builds and licenses. `bootstrap_ocr.py` downloads them directly, validates each hash, then streams only runtime DLLs, Tesseract, Arabic/French `tessdata_fast` 4.1.0 models, configs and license/recipe notices. It does not run a global installer or a package solver. Tool archive downloads are removed after extraction by default; `--keep-archives` retains them for offline tool replay. The final runtime/model/license hashes and version output are recorded in `tools/law-corpus/.cache/toolchain.json`; the delivery records those in `data/law/corpus/research/ocr-toolchain.json`.

Tesseract is Apache-2.0 ([upstream](https://github.com/tesseract-ocr/tesseract/blob/main/LICENSE)); the models are Apache-2.0 ([upstream tag](https://github.com/tesseract-ocr/tessdata_fast/tree/4.1.0)). Dependency licenses are retained individually, including libarchive's GPL build and the platform's redistributable runtime licenses. The engine/model package recipe is retained with the tool cache. Linux/macOS users can supply their installed open-source engine with `--tesseract` and Arabic/French models with `--tessdata`; the Windows package lock is platform-specific.

## Replay and extraction

```powershell
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py fetch tools/law-corpus/priority-manifest.json
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py run tools/law-corpus/priority-manifest.json --offline --dpi 150 --tessdata tools/law-corpus/.cache/ocr/share/tessdata
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py index tools/law-corpus/priority-manifest.json --out data/law/corpus/research/ocr-priority-extractions.json
```

`fetch` downloads only from the exact official HTTPS host allowlist in `pipeline.py`, checks every redirect, and verifies the committed SHA-256 before adopting the bytes. Changed PDFs are refused. `--offline` requires matching cached files. `--import-dir PATH` permits hash-validated reuse of earlier official downloads. Neither unavailable pages nor hash changes are filled in. Trusted software package downloads use a separate fixed conda-forge allowlist; they cannot supply article text.

The manifest contains `manifestVersion: 1`, `sources` (`id`, official `url`, pinned lowercase `sha256`, `retrievedOn`) and `extracts` (`id`, `sourceId`, one-based `pdfPage`, optional `printedPage`, `region`, `language`, `psm`). All 26 ledger/research source PDFs are hash-replayed, including publications without an extraction crop yet. The initial manifest has 30 Arabic/French crops. A `region` is normalized **top-left** `[left, top, right, bottom]`, each coordinate in `[0,1]`. Arabic and French crops are explicit independent entries. An article spanning pages has multiple ordered extraction IDs. Page numbers are PDF indices; printed journal labels are separate strings. `index` exports current crop provenance, raw artifact/model hashes and conservative differences without committing the underlying PDFs/images.

`make_priority_manifest.py` regenerates the reviewed crop coordinates and source metadata from the independent official download list in `research/source-downloads.json`. That list does not establish amendment completeness. `render_pages.py PDF 1 2 --out DIRECTORY` renders specified PDF pages for inspection. `--only EVIDENCE_ID` can limit a replay to selected crops without changing the other artifacts.

For each crop the extraction directory is:

```text
data/law/corpus/.cache/extractions/<PDF-SHA256>/page-<0001>/<evidence-id>/
  crop.png
  text-layer.txt
  ocr.txt
  ocr.tsv
  ocr.stdout.bin
  ocr.stderr.bin
  evidence.json
```

The parent page directory also contains the rendered full-page PNG and `page.text-layer-pypdf.txt`. PDFium independently reads the crop's bounded text layer; `pypdf` preserves the full page's raw text layer. Tesseract sees rendered pixels only. No text-layer output is fed into OCR. The TSV retains recognized word coordinates/confidence; coordinates are relative to the cropped image. `evidence.json` binds official URL/hash/retrieval date, PDF/printed page, normalized/pixel crop box, DPI, engine/model hashes, raw artifact hashes, invocation, failures and comparison differences. Reruns archive previous raw files in `previous-attempts/<previous-evidence-json-sha256>/` before replacing the current attempt. A nonzero exit/missing OCR remains failed/unsettled evidence. OCR success is not text accuracy.

The priority extraction used 150 DPI after observing that the 300 DPI Arabic pass missed whole lines. Both attempts remain in the ignored evidence cache. There are real OCR mistakes and corrupt/absent text layers; none are silently repaired. Raw evidence is reproducible but PDFs, tool binaries and PNGs are not committed.

## Conservative comparison

Only these named operations are used for text comparison:

1. Unicode NFC canonical composition.
2. Replace Unicode whitespace runs with one U+0020; trim leading/trailing whitespace.

Arabic letters, diacritics, tatweel, digit scripts, article numbers, negations, punctuation and bidi/control characters remain significant. There is no `آ` to `ك` folding, ligature repair, punctuation removal, diacritic removal, translation or guessed correction. Empty outputs never constitute agreement. Paragraph structure is not proved by collapsing whitespace. A similarity ratio is diagnostic only; it never establishes a check.

The legacy `tools/law/build_codes.py` was inspected, but its letter/punctuation/diacritic folds and font repair were deliberately not reused. Existing numbering/typed validation remain separate application responsibilities.

## Compose explicit transcriptions

```powershell
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py compose <spec.json> <ledger.json> --out <code-output-folder>
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/pipeline.py validate data/law/corpus
& tools/law-corpus/.cache/venv/Scripts/python.exe -m unittest discover -s tools/law-corpus -p 'test_*.py' -v
```

A spec has `generatedOn` and `articles`. Article/version fields follow the corpus JSON contract. A version additionally has ordered `evidenceIds`; `sourcePublicationId` is optional when different from `introducedBy`. Each crop must match the SHA-256 of the ledger's Arabic source publication. The builder inserts raw `source.textLayer`, `source.ocr` and individual `source.fragments` for every page/crop.

An optional `imageReview` contains `decision: "transcription-resolved"`, `reviewedOn`, a specific `note`, `textSha256` (UTF-8 bytes of the exact canonical version text) and ordered `imageSha256s` (each `crop.png`). The operator must actually inspect every referenced image and settle every glyph, digit, diacritic and punctuation mark before declaring that review. A hash mismatch rejects the claim. Partial reading, plausible wording or OCR similarity does not justify this field. The pipeline itself never declares an image reviewed.

Generated transcription checks retain `textSha256` for the exact canonical UTF-8 text, even when comparison permits whitespace differences. Image-resolution checks also retain the ordered actual `imageSha256s`. The typed loader rejects a passed transcription check whose text hash no longer matches, so editing canonical text requires renewed evidence rather than retaining a stale passed check.

Explicit independently observed French structure, inventory and amendment checks can be supplied in a version's `verification.checks`, with the permitted check name, method, date, result and an evidence note. They are retained. Supplied text-layer/image checks are rejected: those require actual matching streams or a bound image review. French structural alignment is not translation validation. Cross-references, explicit paragraphs, temporal bounds/status and other declared version fields are preserved; no dates or legal application rules are inferred.

Composition always leaves the independent code inventory incomplete, and never promotes a cropped subset to `checked`. A resolved text can be `partly-checked`; missing/disagreeing text without a bound review is `unsettled`. Full inventory/amendment consolidation is a separate required stage. All three verification-state counts are emitted, including zeros. Local JSON Schema validation refuses external `$ref` resources; C# semantic validation supplies additional id/date/ledger/cross-reference checks.

## Observed setup failures

The default Windows Python/uv-cache paths were unavailable in the restricted environment. A dedicated cache/interpreter fixed that. MiKTeX `pdftoppm` attempted initialization outside the workspace; PDFium avoided that dependency. Tesseract was absent. An initial micromamba solve produced about 484 MB of repository metadata and the installation ran out of disk; retrying the solver ran out of memory. Only that newly created, resolved task cache was removed. The final bootstrap has no solver. PE import inspection found `libcurl.dll` required by the Tesseract binary but absent from its declared runtime dependencies; the frozen lock explicitly includes libcurl, Kerberos and libssh2. All tool downloads required development-time network access; legal text replay remains offline after the pinned PDFs are cached.
