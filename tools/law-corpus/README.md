# Law corpus development pipeline

These Python tools create reproducible evidence from official publications. The application reads committed JSON only. Nothing here changes the existing code loaders, source JSON or the legacy `verified` fields.

The optional Arabic second recognizer, measured comparison, caveats and hash-locked setup are documented in [OCR-COMPARISON.md](OCR-COMPARISON.md). It writes separate evidence and never upgrades a legal verification state or replaces canonical text. `merge_transcriptions.py` merges explicit reviewed/candidate batches while preserving later history and refusing implicit historical-text corrections.

`validate_checkpoint.ps1` now imports `validation-cache-filter.targets` only for its build command. This excludes ignored development cache items without moving live OCR evidence or modifying shared project targets. Run it through process-scoped `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/law-corpus/validate_checkpoint.ps1`. The ordinary developer build is not permanently changed.

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

A spec has `generatedOn` and `articles`. Article/version fields follow the corpus JSON contract. A version has either ordered `evidenceIds` from its introducing publication, or explicit `evidenceFragments` with `evidenceId`, `publicationId` and a one-based `paragraph`. Each crop must match its own ledger publication's Arabic PDF hash. Mixed publications require a source for every body paragraph. Non-body fragments use `role: heading`, `repeal` or `amendment-instruction` and cannot claim a paragraph. The top-level source locates `introducedBy`; older words retain their own fragment publications rather than being attributed to the new law. Fragment PDF hashes and mapped paragraph text hashes are emitted and checked by the typed loader. Optional version-level `path` preserves the headings for that version.

An optional `imageReview` contains `decision: "transcription-resolved"`, `reviewedOn`, a specific `note`, `textSha256` (UTF-8 bytes of the exact canonical version text) and ordered `imageSha256s` (each `crop.png`). The operator must actually inspect every referenced image and settle every glyph, digit, diacritic and punctuation mark before declaring that review. A hash mismatch rejects the claim. Partial reading, plausible wording or OCR similarity does not justify this field. The pipeline itself never declares an image reviewed.

Generated transcription checks retain `textSha256` for the exact canonical UTF-8 text, even when comparison permits whitespace differences. Image-resolution checks also retain the ordered actual `imageSha256s`. The typed loader rejects a passed transcription check whose text hash no longer matches, so editing canonical text requires renewed evidence rather than retaining a stale passed check.

Explicit independently observed French structure, inventory and amendment checks can be supplied in a version's `verification.checks`, with the permitted check name, method, date, result and an evidence note. They are retained. Supplied text-layer/image checks are rejected: those require actual matching streams or a bound image review. French structural alignment is not translation validation. Cross-references, explicit paragraphs, temporal bounds/status and other declared version fields are preserved; no dates or legal application rules are inferred.

Composition leaves the inventory incomplete unless an explicit independent `inventoryReview` is supplied. That review must name the exact expected article set and every applied publication, contain a method/evidence note/date, and match `hash_json(ledger)` and `article_versions_digest(spec)`. Gaps, unapplied publications or changed text/boundaries reject it. A valid review can supply the inventory/amendment checks; a version reaches `checked` only if its transcription also passes. A bounded chapter inventory may be complete while the collection remains `coverage: partial`; it never becomes a complete code. This binding prevents stale certificates, but does not manufacture independent source observation or prove that an unlisted amendment does not exist.

Without that review, a resolved transcription stays `partly-checked`; missing/disagreeing text without a bound image decision stays `unsettled`. All three state counts include zeros. JSON Schema validation refuses external `$ref` resources; C# validation adds identity/date/ledger/cross-reference and paragraph-provenance checks.

## Revised scope and full Civil extraction

The user requested complete Civil, CPCA including Law22-13, and Family; Commercial/90-11/90-04 cover only catalogue articles plus whole chapters and all versions. **No special laws.** The frozen boundaries and catalogue file hashes are in `research/scope-plan.json`. Run `scope_inventory.py --check` to detect drift. The initial bounded sets contain37,29 and1 identities respectively; historical insertions and chapter changes still need reconciliation. `chapter-subset` and covered-chapter arrays make partial coverage explicit in ledgers and reports.

`collect_scope_sources.py` pins first official acquisitions and replays known hashes. It reuses the old research downloads when available, records failures, and never treats a404 response as a publication. `scope-source-manifest.json` replays the36 successfully pinned PDFs currently used for source/discovery work. The old2007 Commercial compilation supplies structural discovery, not a claim of current law.

```powershell
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/make_civil_manifest.py
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/pipeline.py run tools/law-corpus/civil-full-manifest.json --offline --dpi 150 --resume --tessdata tools/law-corpus/.cache/ocr/share/tessdata
# On a fresh cache, inspect the page layouts, then record separator proposals:
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/civil_layout.py
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/pipeline.py run tools/law-corpus/civil-full-manifest.json --offline --dpi 150 --resume --tessdata tools/law-corpus/.cache/ocr/share/tessdata
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/index_article_labels.py tools/law-corpus/civil-full-manifest.json A1975078 1003 --out data/law/corpus/research/civil-original-ar-number-observations.json
```

The312 column windows cover original Arabic/French Civil sources and five indexed amendments. The original1975 scans have shifted/slanted column separators. `civil_layout.py` records pixel-derived polygons with source/page-image hashes; low confidence refuses automatic adoption. `clipPolygon` is expressed in normalized page coordinates inside its bounding `region`. It masks the OCR image only; the independently retained PDFium text layer remains the raw bounding rectangle. No text is repaired by the mask. These are layout proposals, not image-transcription decisions. The old rectangular attempts remain in `previous-attempts`.

`--resume` reuses evidence only when source/crop/polygon/DPI/PSM/engine/models and all necessary artifact hashes agree. It does not rebind image reviews. Extraction retains at most four uncompressed page images. JSON checkpoints are written through flushed temporary files and atomic replacement. Distinct manifests validate only their requested evidence, so unrelated active extraction cannot invalidate their final index. Simultaneous writes to the same evidence remain an error.

`index_article_labels.py` observes anchored article labels and reports missing/duplicate/out-of-order OCR numbers. Its number canonicalization never changes body text. A missed label is **not** a repeal or a legal inventory gap. The current number observations remain candidates, not `inventory-complete` proof or imported article text.

## Explicit amendment application and article489 pilot

`consolidate.py INITIAL LEDGER PLAN --out SPEC --audit AUDIT` applies declared operations in publication order. Each mutation checks the preceding exact text hash. Supported operations are insertion, full replacement, paragraph edits, repeal, heading change and explicit renumbering. Paragraph edits use the old paragraph positions; overlapping edits fail. Unchanged paragraphs retain their earlier evidence. Repeals remain explicit empty-text versions. Renumbering keeps the old numeric identity as repealed and creates the new numeric identity, linked in the operation audit; existing references are not silently rewritten. No commencement date is inferred from a publication date. The output invalidates prior inventory certification and lists unapplied touched articles/publications.

The real489 pilot uses `civil-consolidation-pilot-manifest.json` at200DPI. Arabic1975 PDF31/printed1019 has two paragraphs. Law07-05 art6 names489 and its PDF5 replaces paragraph1 while explicitly keeping the remainder. The pipeline preserves original paragraph2 and its source; it does not import the ellipsis as article text. French originalPDF25 and2007PDF4 independently show that structure. Exact generic commencement and historical Arabic headings remain unresolved.

```powershell
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/pipeline.py run tools/law-corpus/civil-consolidation-pilot-manifest.json --offline --dpi 200 --resume --tessdata tools/law-corpus/.cache/ocr/share/tessdata
& tools/law-corpus/.cache/venv/Scripts/python.exe -B tools/law-corpus/build_civil_pilot.py
```

The pilot builder reads **previously recorded** text/image review hashes from `research/civil-pilot-image-decisions.json`. Changed pixels or text require a renewed explicit review; the builder never regenerates a passed review against unseen images. Original orthography remains in the inherited paragraph. The pilot is not a whole-code delivery. To regenerate after `generate_priority_specs.py`, rerun the pilot builder; it refuses to overwrite a larger whole-code spec.

## Checkpoint validation on this Windows laptop

The shared `Directory.Build.targets` remains unchanged. Because it explicitly copies `data/**/*`, run full validation only after extraction/composition stops. `validate_checkpoint.ps1` temporarily moves only this task's ignored source cache outside that glob, checks the resolved paths, runs the full Release build/tests using SDK hard-link copy options, and restores the source cache in `finally`. It also removes only stale source-cache copies from this worktree's development `bin` outputs. Do not run a second extractor while the cache is held. A process-scoped `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/law-corpus/validate_checkpoint.ps1` runs this local reviewed script without changing the machine's execution-policy setting.

## Observed setup failures

The default Windows Python/uv-cache paths were unavailable in the restricted environment. A dedicated cache/interpreter fixed that. MiKTeX `pdftoppm` attempted initialization outside the workspace; PDFium avoided that dependency. Tesseract was absent. An initial micromamba solve produced about 484 MB of repository metadata and the installation ran out of disk; retrying the solver ran out of memory. Only that newly created, resolved task cache was removed. The final bootstrap has no solver. PE import inspection found `libcurl.dll` required by the Tesseract binary but absent from its declared runtime dependencies; the frozen lock explicitly includes libcurl, Kerberos and libssh2. All tool downloads required development-time network access; legal text replay remains offline after the pinned PDFs are cached.
