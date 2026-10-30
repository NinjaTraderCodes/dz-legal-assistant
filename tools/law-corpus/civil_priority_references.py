#!/usr/bin/env python3
"""Extract explicit numeric Civil citations; preserve source spelling, punctuation and open-ended anchors."""
from __future__ import annotations

import re

import pipeline

WORK = pipeline.ROOT / "data/law/corpus/research/civil-2026-10-28"
NUMBER = r"\d+(?:\s+مكرر(?:\s+\d+)?)?"
REFERENCE = re.compile(r"(?:ال|لل|بال)(?:مادة|مادتين|مادتان|مواد)[\s.:،]*"
                       r"(?P<numbers>" + NUMBER + r"(?:\s*و\s*" + NUMBER + r")*)")
# Independently read from the completed current bodies, not generated from this expression.
EXPECTED = {("83", 1): ["81", "82"], ("91", 1): ["90"], ("131", 1): ["182", "182 مكرر"],
            ("363", 2): ["184"], ("375", 5): ["373"], ("376", 1): ["375"], ("381", 1): ["376"],
            ("390", 2): ["212"], ("401", 1): ["400"], ("409", 1): ["408"], ("412", 1): ["410", "411"],
            ("452", 1): ["538"], ("553", 1): ["170"], ("569", 1): ["552"]}


def identity(number: str) -> str:
    return "civil-code:" + number.replace(" مكرر", "-bis").replace(" ", "-")


def extract(paragraphs: list[str]) -> list[dict]:
    result = []
    for index, paragraph in enumerate(paragraphs, 1):
        for match in REFERENCE.finditer(paragraph):
            tail = paragraph[match.end():]
            if re.match(r"\s*من\s+(?:ال)?(?:قانون|أمر|مرسوم)", tail) and not re.match(r"\s*من القانون المدني", tail):
                raise ValueError("An explicit foreign-law citation needs a separately reviewed namespace")
            quote = match.group()
            if re.match(r"\s+وما بعدها", tail):
                quote += re.match(r"\s+وما بعدها", tail).group()
            for number in re.split(r"\s*و\s*", match["numbers"]):
                result.append({"targetId": identity(" ".join(number.split())), "paragraph": index, "quote": quote})
    return result


def main() -> None:
    path = WORK / "priority-collated-body-projection.json"
    bodies = pipeline.read_json(path)["projectedCandidates"]
    found, rows = {}, []
    for body in bodies:
        refs = extract(body["paragraphs"])
        for ref in refs:
            found.setdefault((body["number"], ref["paragraph"]), []).append(
                ref["targetId"].split(":")[1].replace("-bis", " مكرر").replace("-", " "))
        if refs:
            rows.append({"number": body["number"], "inputTextSha256": pipeline.hash_text(body["text"]), "references": refs})
    if found != EXPECTED:
        raise ValueError("Extracted numeric references differ from the independently read17-target inventory")
    selected = {identity(a["number"]) for a in bodies}
    outside = sorted({r["targetId"] for row in rows for r in row["references"]} - selected)
    pipeline.write_json(WORK / "priority-reference-audit.json", {
        "method": __doc__.strip(), "inputSha256": pipeline.hash_file(path), "reviewedOn": "2026-10-30",
        "sourceParagraphsActuallyRead": len(EXPECTED), "explicitTargetOccurrences": sum(map(len, EXPECTED.values())),
        "articles": rows, "outsidePackageTargetIds": outside,
        "notes": ["All numeric citation paragraphs were actually read in the completed bodies; article550/551/552/568/570 material nouns and anonymous self-references are not numeric citations.",
                  "Article452 retains the explicit538and-following anchor; no invented end to the range or claim that only538is relevant.",
                  "Source punctuation in المادة.184 / للمادة،373 / المادة.212 is retained literally; target parsing does not rewrite legal text.",
                  "The current schema records the source paragraph. Mentioned target-paragraph qualifiers remain in the body; no unsupported target-paragraph field is invented."],
        "typedCivilChanged": False, "fullArticleChecked": False})
    print("17 explicit target occurrences in14source paragraphs; outside-package identities:", ", ".join(outside))


if __name__ == "__main__":
    main()
