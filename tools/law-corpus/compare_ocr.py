#!/usr/bin/env python3
"""Compare recognizers on existing image-bound transcriptions; never alter corpus data."""
import argparse
from collections import Counter
from datetime import datetime, timezone
import importlib.metadata
import os
from pathlib import Path
import re
import time

import pipeline

OUTPUT = pipeline.TOOLS / "ocr-comparison"


def ordered_lines(boxes, texts):
    """Cluster word boxes into lines before reading each Arabic line RTL."""
    entries = []
    for box, text in zip(boxes, texts):
        xs, ys = zip(*box)
        entries.append((min(xs), min(ys), max(xs), max(ys), text))
    lines = []
    for entry in sorted(entries, key=lambda e: (e[1] + e[3]) / 2):
        center, height = (entry[1] + entry[3]) / 2, entry[3] - entry[1]
        matches = [(abs(center - line[0]), line) for line in lines
                   if abs(center - line[0]) <= 0.5 * min(height, line[1])]
        if matches:
            _, line = min(matches, key=lambda pair: pair[0])
            line[2].append(entry)
        else:
            lines.append([center, height, [entry]])
    return [" ".join(e[4] for e in sorted(line[2], key=lambda e: e[2], reverse=True)) for line in lines]


def body_alignment(reference, recognized):
    """Levenshtein against the best contiguous span, ignoring crop labels/margins.

    Every reference character must align or incur an edit, including omitted
    trailing lines. Leading/trailing OCR outside that span is unscored, NOT
    removed from evidence or claimed correct. No Arabic/punctuation folding.
    """
    reference = pipeline.normalize_for_comparison(reference)
    recognized = pipeline.normalize_for_comparison(recognized)
    if not reference:
        raise ValueError("Empty reference cannot establish accuracy")
    costs, starts = [0] * (len(recognized) + 1), list(range(len(recognized) + 1))
    for i, char in enumerate(reference, 1):
        next_costs, next_starts = [i], [0]
        for j, other in enumerate(recognized, 1):
            choices = [(costs[j - 1] + (char != other), starts[j - 1]),
                       (costs[j] + 1, starts[j]),
                       (next_costs[j - 1] + 1, next_starts[j - 1])]
            cost, start = min(choices)
            next_costs.append(cost)
            next_starts.append(start)
        costs, starts = next_costs, next_starts
    end = min(range(len(costs)), key=lambda j: (costs[j], j))
    span = recognized[starts[end]:end]
    # A narrow diagnostic, not a check that legal meaning is preserved.
    tokens = lambda text: Counter(re.findall(r"\d+|\b(?:لا|لم|لن|ليس|الا|إلا|دون)\b", text))
    return {"edits": costs[end], "referenceCharacters": len(reference),
            "characterErrorRate": costs[end] / len(reference), "alignedSpan": span,
            "criticalTokensMissing": list((tokens(reference) - tokens(span)).elements()),
            "criticalTokensExtra": list((tokens(span) - tokens(reference)).elements())}


def samples(holdout=False):
    name = "ocr-holdout-samples.json" if holdout else "ocr-comparison-samples.json"
    fixed = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research" / name)["samples"]
    fixed_ids = {s["versionId"] for s in fixed}
    evidence = pipeline.evidence_by_id(pipeline.CACHE, {s["evidenceId"] for s in fixed})
    result = []
    for code in ("civil-code", "commercial-code", "cpca"):
        spec = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/priority-specs" / (code + ".json"))
        for article in spec["articles"]:
            for version in article["versions"]:
                if version["versionId"] not in fixed_ids:
                    continue
                review, ids = version.get("imageReview"), version.get("evidenceIds", [])
                if not review or len(ids) != 1:
                    continue
                ev, _ = evidence[ids[0]]
                folder = pipeline.CACHE / "extractions" / ev["sourceSha256"] / f"page-{ev['pdfPage']:04}" / ev["id"]
                image = folder / "crop.png"
                if (review["textSha256"] != pipeline.hash_text(version["text"])
                        or review["imageSha256s"] != [pipeline.hash_file(image)]):
                    raise ValueError("Stale sample review: " + version["versionId"])
                row = {"versionId": version["versionId"], "reference": version["text"],
                               "evidenceId": ev["id"], "sourceSha256": ev["sourceSha256"],
                               "sourceUrl": ev["sourceUrl"], "pdfPage": ev["pdfPage"],
                               "imageSha256": review["imageSha256s"][0], "image": image,
                               "dpi": ev["dpi"], "psm": int(ev["ocrInvocation"]["command"][ev["ocrInvocation"]["command"].index("--psm") + 1]),
                               "language": ev["language"]}
                expected = next(s for s in fixed if s["versionId"] == version["versionId"])
                if any(row[k] != v for k, v in expected.items()):
                    raise ValueError("Fixed calibration sample changed: " + version["versionId"])
                result.append(row)
    if len(result) != len(fixed):
        raise ValueError("A fixed calibration reference is missing or no longer reviewed")
    return result


def paddle_engine():
    """Require pinned local weights and packages; downloads are a separate step."""
    from rapidocr import RapidOCR, OCRVersion, ModelType, LangDet, LangRec
    pins = pipeline.read_json(pipeline.ROOT / "data/law/corpus/research/ocr-paddle-model-pins.json")
    for name in ("rapidocr", "onnxruntime", "python-bidi"):
        if importlib.metadata.version(name) != pins[name]:
            raise ValueError("Optional OCR package differs from the comparison lock: " + name)
    models = OUTPUT / "models"
    for pin in pins["models"]:
        if pipeline.hash_file(models / pin["file"]) != pin["sha256"]:
            raise ValueError("Optional OCR model differs from pin: " + pin["file"])
    params = {"Global.model_root_dir": str(models.resolve()), "Global.use_cls": False,
              "Det.ocr_version": OCRVersion.PPOCRV5, "Det.model_type": ModelType.MOBILE, "Det.lang_type": LangDet.CH,
              "Det.limit_type": "max", "Det.limit_side_len": 1600,
              "Rec.ocr_version": OCRVersion.PPOCRV5, "Rec.model_type": ModelType.MOBILE, "Rec.lang_type": LangRec.ARABIC,
              "EngineConfig.onnxruntime.intra_op_num_threads": 2,
              "EngineConfig.onnxruntime.inter_op_num_threads": 1}
    metadata = {"params": {k: getattr(v, "value", v) for k, v in params.items()},
                "versions": {k: pins[k] for k in ("rapidocr", "onnxruntime", "python-bidi")},
                "models": pins["models"],
                "order": "word boxes clustered by y-overlap then x descending; raw engine boxes/order retained; engine's Arabic bidi conversion retained without further character reversal"}
    return RapidOCR(params=params), metadata


def run(engine_name, holdout=False):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    os.environ["OMP_THREAD_LIMIT"] = "2"
    engine, initialization = None, 0
    params = None
    if engine_name == "paddle-onnx":
        start = time.perf_counter()
        engine, params = paddle_engine()
        initialization = time.perf_counter() - start
    rows = []
    for sample in samples(holdout):
        dest = OUTPUT / engine_name / sample["evidenceId"]
        dest.mkdir(parents=True, exist_ok=True)
        start = time.perf_counter()
        if engine is None:
            models = (pipeline.TOOLS / "ocr-best" if engine_name == "tesseract-best"
                      else pipeline.TOOLS / "ocr/share/tessdata")
            invocation = pipeline.ocr_image(sample["image"], dest / "ocr", sample["language"],
                                            sample["psm"], pipeline.default_tesseract(), models, sample["dpi"])
            if invocation["exitCode"] != 0:
                raise ValueError("OCR failed: " + str(invocation))
            text = (dest / "ocr.txt").read_text(encoding="utf-8-sig")
        else:
            out = engine(sample["image"])
            raw = out.to_json() or []
            pipeline.write_json(dest / "raw.json", raw)
            # Single-column crops; preserve original model order and boxes in
            # raw.json. Never reorder Arabic characters inside a recognized line.
            boxes = [] if out.boxes is None else out.boxes.tolist()
            text = "\n".join(ordered_lines(boxes, out.txts or []))
            (dest / "ocr.txt").write_text(text, encoding="utf-8")
            invocation = params
        elapsed = time.perf_counter() - start
        row = {k: v for k, v in sample.items() if k != "image"}
        row.update({"engine": engine_name, "seconds": elapsed, "recognized": text,
                    "ocrSha256": pipeline.hash_text(text), "invocation": invocation,
                    "metric": body_alignment(sample["reference"], text)})
        rows.append(row)
        # Recoverable after every sample, including a model/engine failure later.
        pipeline.write_json(OUTPUT / (engine_name + ".json"), rows)
        print(engine_name, sample["evidenceId"], round(elapsed, 3), round(row["metric"]["characterErrorRate"], 4), flush=True)
    result = {"engine": engine_name, "recordedAt": datetime.now(timezone.utc).isoformat(),
              "normalizations": pipeline.NORMALIZATIONS, "initializationSeconds": initialization,
              "ocrSeconds": sum(r["seconds"] for r in rows), "sampleCount": len(rows),
              "bodyCharacterErrorRate": sum(r["metric"]["edits"] for r in rows) / sum(r["metric"]["referenceCharacters"] for r in rows),
              "limitations": ["Selected image-reviewed integration samples, not independent human ground truth or a random code-wide test.",
                              "Best-contiguous-span CER ignores crop labels and surrounding text; every reference character still incurs alignment/edit cost.",
                              "Times include subprocess startup or first inference; downloads/initialization are separate. Battery CPU, two threads; no throughput guarantee.",
                              "No engine agreement or metric upgrades legal verification; paragraph layout and semantic accuracy need source inspection."],
              "models": [{"file": p.name, "sha256": pipeline.hash_file(p), "bytes": p.stat().st_size}
                         for p in sorted((OUTPUT / "models").glob("*.onnx"))] if engine else [],
              "samples": rows}
    prefix = "ocr-holdout-" if holdout else "ocr-comparison-"
    result["sampleSet"] = "new12 untreated original-page samples; not used for parameter tuning" if holdout else "initial18 selected calibration samples"
    pipeline.write_json(pipeline.ROOT / "data/law/corpus/research" / (prefix + engine_name + ".json"), result)
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("engine", choices=["tesseract-fast", "tesseract-best", "paddle-onnx"])
    parser.add_argument("--holdout", action="store_true", help="Evaluate the fixed new12 samples without tuning on them")
    args = parser.parse_args()
    result = run(args.engine, args.holdout)
    print({k: v for k, v in result.items() if k not in ("samples", "models")})
