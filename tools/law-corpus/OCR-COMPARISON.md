# Arabic OCR comparison,26October2026

The user authorized resuming corpus work and adopting useful OCR improvements. All tools and weights here are development-only. The app's dependencies and existing81-article library are unchanged.

| Recognizer | Selected18 body-alignment character error | OCR time for18 crops |
|---|---:|---:|
| Tesseract5.5.2 / Arabic fast4.1.0 |8.25%|4.94s|
| Tesseract5.5.2 / Arabic best4.1.0 |7.93%|12.58s|
| Arabic PP-OCRv5 ONNX / RapidOCR3.9.2 |2.94%|18.41s|

On12 additional original article bodies unused during parameter tuning, fast Tesseract measured7.58% and PP-OCRv5 measured3.09%. Those12 bodies share three column crops from two original pages. This is a small follow-up within the same publication, not an independent human-reviewed or random whole-code test. The initial18 references were used to choose the geometry profile. Both recognizers still make errors; no metric confers a legal verification check.

Fixed references, exact recognized strings, official source and image hashes, critical-token differences and method limitations are in `data/law/corpus/research/ocr-comparison-*.json` and `ocr-holdout-*.json`. Only NFC and whitespace normalization are applied. Each complete reference body aligns to the best contiguous OCR span so labels and surrounding articles do not score as body errors. Omissions inside the body, including final lines, incur edits. The metric does not certify paragraphs or legal meaning.

Times are CPU observations during a charging/power-state transition and other corpus activity, not controlled throughput measurements. The follow-up repeatedly recognizes shared column images and is unsuitable for throughput comparison. The improvement is in transcription/correction effort, not faster model inference. Actual total manual correction effort is not measured.

An initial PP-OCRv5 attempt used inappropriate crop scaling and sorted word boxes by y alone, scrambling Arabic word order. The adopted profile limits the long side, groups words into lines and reads each Arabic line right to left. Raw engine strings, scores, boxes and the failed initial result remain cached separately. RapidOCR's own Arabic bidi conversion is retained with pinned `python-bidi`. No extra character reversal, letter folding, textual voting or silent canonical correction is applied. These are geometry choices, not learned legal-text repairs.

Decision: retain fast Tesseract as the first stream, add PP-OCRv5 as a separate second Arabic stream for disagreement review, and use Tesseract best selectively. The small local best-model gain does not justify replacing the fast pass. The PP-OCRv5 recognizer is also supported by Docling; Docling itself has not been installed or benchmarked. Explicit column polygons already solve the examined source layouts. EasyOCR remains an unmeasured fallback. Official digital text is preferable when traceable to the correct legal version. No unofficial collection supplies corpus text.

Primary technical sources:

- [Tesseract fast/best model documentation](https://tesseract-ocr.github.io/tessdoc/Data-Files.html).
- [PaddleOCR Arabic PP-OCRv5 model documentation](https://www.paddleocr.ai/main/en/version3.x/pipeline_usage/OCR.html).
- [Docling backend/language support](https://docling-project.github.io/docling/concepts/OCR_native/).
- [RapidOCR3.9.2 model routes and hashes](https://github.com/RapidAI/RapidOCR/blob/v3.9.2/python/rapidocr/default_models.yaml).
- [RapidOCR3.9.2 bidi conversion](https://github.com/RapidAI/RapidOCR/blob/v3.9.2/python/rapidocr/utils/utils.py).
- [EasyOCR upstream implementation](https://github.com/JaidedAI/EasyOCR).

## Reproduction

Run from the isolated worktree. Models and optional distributions are separately pinned. Install only into the existing ignored corpus virtual environment:

```powershell
$env:UV_CACHE_DIR = Join-Path (Get-Location) 'tools/law-corpus/.cache/uv'
uv pip install --python tools/law-corpus/.cache/venv/Scripts/python.exe --require-hashes -r tools/law-corpus/requirements-ocr-candidates.lock
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/bootstrap_ocr_candidates.py
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/bootstrap_ocr_candidates.py --offline
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/compare_ocr.py tesseract-fast
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/compare_ocr.py tesseract-best
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/compare_ocr.py paddle-onnx
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/compare_ocr.py tesseract-fast --holdout
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/compare_ocr.py paddle-onnx --holdout
```

For full Civil second-stream extraction, after the original manifest's pinned PDFs and first-stream evidence exist:

```powershell
& tools/law-corpus/.cache/venv/Scripts/python.exe tools/law-corpus/supplementary_ocr.py tools/law-corpus/civil-full-manifest.json --out tools/law-corpus/.cache/civil-full-paddle-extractions.json
```

Each crop is checkpointed atomically and bound to existing source/crop evidence, model/runtime settings, implementation hashes and raw artifacts. Resume refuses changed artifacts. Records never replace the first OCR, text layer or an image decision and never import article bodies automatically. Keep all source files/implementation revisions used for an extraction when replaying exact historical evidence; a newer implementation causes a new conservative cache binding even if its OCR helper is unchanged.

Tesseract/PaddleOCR/RapidOCR use Apache-2.0, ONNX Runtime MIT, and python-bidi LGPL. All remain development-only, with license metadata in the ignored environment. No weights, PDFs, full-page PNGs or runtime network calls are shipped.
