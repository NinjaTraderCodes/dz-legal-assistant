#!/usr/bin/env python3
"""Apply explicit, hash-guarded amendment instructions; never infer legal operations or dates."""
from __future__ import annotations

import argparse
from copy import deepcopy
from datetime import date

import pipeline


def paragraph_fragments(version):
    """Expand declared crop provenance to ordered paragraphs, without certifying text."""
    fragments = version.get("evidenceFragments")
    if fragments is None:
        fragments = [{"evidenceId": key, "publicationId": version.get("sourcePublicationId", version["introducedBy"])}
                     for key in version["evidenceIds"]]
    result = []
    for i, text in enumerate(version["paragraphs"], 1):
        mapped = [{k: v for k, v in fragment.items() if k != "paragraph"}
                  for fragment in fragments if fragment.get("role", "body") == "body"
                  and fragment.get("paragraph", i) == i]
        if not mapped:
            raise ValueError("Every paragraph requires source evidence before consolidation")
        result.append({"text": text, "fragments": mapped})
    return result


def new_paragraphs(texts, fragments, publication):
    if not texts or any(not isinstance(text, str) or not text.strip() for text in texts):
        raise ValueError("Replacement paragraphs must be nonempty explicit transcriptions")
    result = []
    for i, text in enumerate(texts, 1):
        mapped = [{k: v for k, v in f.items() if k != "paragraph"}
                  for f in fragments if f.get("paragraph", i) == i]
        if not mapped or any(f.get("publicationId") != publication or not f.get("evidenceId") for f in mapped):
            raise ValueError("New text must have evidence in the amending publication")
        result.append({"text": text, "fragments": mapped})
    return result


def apply_plan(initial: dict, ledger: dict, plan: dict):
    """Returns a composition spec plus an operation audit. Input objects stay untouched."""
    result = deepcopy(initial)
    result.pop("inventoryReview", None)  # Any change invalidates earlier inventory certification.
    publications = {p["id"]: p for p in ledger["publications"]}
    articles = {a["canonicalNumber"]: a for a in result["articles"]}
    if len(articles) != len(result["articles"]):
        raise ValueError("Duplicate initial article identity")
    operations_seen, publication_articles = set(), set()
    audit = []
    last_publication_date = None
    for operation in plan["operations"]:
        publication_id = operation["publicationId"]
        if publication_id not in publications:
            raise ValueError("Amending publication not in ledger")
        publication = publications[publication_id]
        publication_date = date.fromisoformat(publication["jo"]["date"])
        if last_publication_date is not None and publication_date < last_publication_date:
            raise ValueError("Amendments must be supplied in publication order; no silent reordering")
        last_publication_date = publication_date
        op_id, number, action = operation["id"], operation["number"], operation["action"]
        if op_id in operations_seen or (publication_id, number) in publication_articles:
            raise ValueError("Duplicate operation or repeated article/publication; group its edits in one instruction")
        operations_seen.add(op_id)
        publication_articles.add((publication_id, number))
        if number not in publication["articlesTouched"]:
            raise ValueError("Operation target is absent from publication's touched-article inventory")
        if not operation.get("temporalBasis", "").strip() or "effectiveDate" not in operation:
            raise ValueError("An explicit date or null with temporal basis is required")
        effective = operation["effectiveDate"]
        if effective is not None:
            date.fromisoformat(effective)
        previous = None
        if action == "insert":
            if number in articles:
                raise ValueError("Cannot insert an existing article")
            article = {"id": ledger["id"] + ":" + number.replace(" مكرر", "-bis").replace(" ", "-"),
                       "number": operation.get("printedNumber", number), "canonicalNumber": number,
                       "path": operation["path"], "availability": "text", "versions": []}
            articles[number] = article
            result["articles"].append(article)
            paragraphs = new_paragraphs(operation["paragraphs"], operation["evidenceFragments"], publication_id)
        else:
            if number not in articles or not articles[number].get("versions"):
                raise ValueError("Cannot amend an article without a prior version")
            article = articles[number]
            previous = article["versions"][-1]
            if previous["status"] == "repealed":
                raise ValueError("Cannot amend a repealed article without an explicit reinstatement operation")
            if operation.get("expectedBaseTextSha256") != pipeline.hash_text(previous["text"]):
                raise ValueError("Amendment base text hash mismatch; refusing the wrong historical text")
            if effective is not None and previous.get("inForceFrom") is not None and effective <= previous["inForceFrom"]:
                raise ValueError("Amendment boundary must follow the preceding known start")
            if action == "replace":
                paragraphs = new_paragraphs(operation["paragraphs"], operation["evidenceFragments"], publication_id)
            elif action in ("edit-paragraphs", "heading"):
                paragraphs = paragraph_fragments(previous)
                edits = sorted(operation.get("edits", []), key=lambda e: e["start"], reverse=True)
                end_of_next_edit = len(paragraphs) + 2
                for edit in edits:
                    start, remove = edit["start"], edit["delete"]
                    if (type(start) is not int or type(remove) is not int or start < 1 or remove < 0
                            or start - 1 + remove > len(paragraphs) or start >= end_of_next_edit
                            or start - 1 + remove >= end_of_next_edit):
                        raise ValueError("Paragraph edits overlap or address an invalid source range")
                    replacement = (new_paragraphs(edit["paragraphs"], edit["evidenceFragments"], publication_id)
                                   if edit.get("paragraphs") else [])
                    paragraphs[start - 1:start - 1 + remove] = replacement
                    end_of_next_edit = start
                if action == "edit-paragraphs" and not edits:
                    raise ValueError("Paragraph amendment has no explicit edits")
                if action == "heading" and (edits or not operation.get("path")):
                    raise ValueError("Heading-only operation must supply a path and cannot change body text")
            elif action in ("repeal", "renumber"):
                paragraphs = []
            else:
                raise ValueError("Unsupported explicit amendment action: " + action)
            previous["inForceTo"] = effective
            if effective is None:
                previous["temporalStatus"] = "unsettled"
                previous.pop("inventoryReview", None)
        mapped = [fragment | {"paragraph": i} for i, p in enumerate(paragraphs, 1) for fragment in p["fragments"]]
        instruction = operation.get("instructionEvidence", [])
        if instruction:
            if any(f.get("publicationId") != publication_id or f.get("paragraph") is not None for f in instruction):
                raise ValueError("Amendment instruction evidence must locate the amending publication, without a body paragraph")
            mapped.extend(f | {"role": "repeal" if action in ("repeal", "renumber") else "heading" if action == "heading" else "amendment-instruction"}
                          for f in instruction)
        if not mapped or not any(f["publicationId"] == publication_id for f in mapped):
            raise ValueError("Operation needs source evidence for its actual amending instruction")
        if "crossReferences" not in operation:
            raise ValueError("Explicit cross-reference extraction required for each resulting version")
        texts = [p["text"] for p in paragraphs]
        version = {"versionId": article["id"] + "@" + publication_id,
                   "text": "\n".join(texts), "paragraphs": texts,
                   "path": operation.get("path", previous.get("path", article["path"]) if previous else article["path"]),
                   "inForceFrom": effective, "inForceTo": None,
                   "temporalStatus": "resolved" if effective is not None else "unsettled",
                   "introducedBy": publication_id, "status": "repealed" if action in ("repeal", "renumber") else "in-force",
                   "evidenceFragments": mapped, "crossReferences": operation["crossReferences"],
                   "verification": {"reasons": ["Derived from explicit amendment instructions; resulting transcription/image checks pending.", operation["temporalBasis"]]}}
        if any(v["versionId"] == version["versionId"] for v in article["versions"]):
            raise ValueError("Duplicate resulting version identity")
        article["versions"].append(version)
        audit.append({"id": op_id, "publicationId": publication_id, "number": number, "action": action,
                      "beforeTextSha256": pipeline.hash_text(previous["text"]) if previous else None,
                      "afterTextSha256": pipeline.hash_text(version["text"]),
                      "effectiveDate": effective, "temporalBasis": operation["temporalBasis"],
                      "resultVersionId": version["versionId"]})
        if action == "renumber":
            new_number = operation["newNumber"]
            if new_number in articles or new_number not in publication["articlesTouched"]:
                raise ValueError("Renumbering destination must be absent and explicitly touched in ledger")
            if not instruction:
                raise ValueError("Renumbering needs the published renumbering instruction, not inferred numbering")
            old_paragraphs = paragraph_fragments(previous)
            new_article_id = ledger["id"] + ":" + new_number.replace(" مكرر", "-bis").replace(" ", "-")
            new_version = deepcopy(version)
            new_version.update({"versionId": new_article_id + "@" + publication_id,
                                "text": previous["text"], "paragraphs": list(previous["paragraphs"]), "status": "in-force",
                                "evidenceFragments": [fragment | {"paragraph": i}
                                                      for i, p in enumerate(old_paragraphs, 1) for fragment in p["fragments"]]
                                                     + [f | {"role": "amendment-instruction"} for f in instruction]})
            new_article = {"id": new_article_id, "number": operation.get("newPrintedNumber", new_number),
                           "canonicalNumber": new_number, "path": list(new_version["path"]),
                           "availability": "text", "versions": [new_version]}
            articles[new_number] = new_article
            result["articles"].append(new_article)
            publication_articles.add((publication_id, new_number))
            audit[-1].update({"renumberedTo": new_article_id, "resultNewVersionId": new_version["versionId"],
                              "newTextSha256": pipeline.hash_text(new_version["text"])})
    target = set(ledger["scope"]["targetArticles"])
    applications = []
    no_effect = {entry["publicationId"]: entry for entry in plan.get("noScopedEffect", [])}
    for publication in ledger["publications"]:
        pub_id = publication["id"]
        touched = set(publication["articlesTouched"]) & target
        applied = {number for pub, number in publication_articles if pub == pub_id}
        applied |= {a["canonicalNumber"] for a in initial["articles"]
                    if any(v["introducedBy"] == pub_id for v in a.get("versions", []))}
        justified_no_effect = pub_id in no_effect and not touched and bool(no_effect[pub_id].get("note", "").strip())
        complete = (bool(touched) and touched <= applied) or justified_no_effect
        applications.append({"publicationId": pub_id, "expectedScopedArticles": sorted(touched),
                             "appliedScopedArticles": sorted(applied), "missing": sorted(touched - applied),
                             "complete": complete, "note": no_effect.get(pub_id, {}).get("note")})
    result["generatedOn"] = plan["generatedOn"]
    result["unappliedPublications"] = [p["publicationId"] for p in applications if not p["complete"]]
    # This audit establishes reproducible operations, not exact source transcription,
    # inventory independence, legal applicability, or an automatic checked state.
    return result, {"generatedOn": plan["generatedOn"], "ledgerSha256": pipeline.hash_json(ledger),
                    "inputSpecSha256": pipeline.hash_json(initial), "planSha256": pipeline.hash_json(plan),
                    "resultArticleVersionsSha256": pipeline.article_versions_digest(result),
                    "operations": audit, "publications": applications,
                    "verificationNote": "No OCR/image, inventory-complete or legal-review check is inferred from successful application."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("initial", type=pipeline.Path)
    parser.add_argument("ledger", type=pipeline.Path)
    parser.add_argument("plan", type=pipeline.Path)
    parser.add_argument("--out", type=pipeline.Path, required=True)
    parser.add_argument("--audit", type=pipeline.Path, required=True)
    args = parser.parse_args()
    result, audit = apply_plan(pipeline.read_json(args.initial), pipeline.read_json(args.ledger), pipeline.read_json(args.plan))
    pipeline.write_json(args.out, result)
    pipeline.write_json(args.audit, audit)
    print(f"Applied {len(audit['operations'])} explicit operations; {len(result['unappliedPublications'])} publications incomplete")


if __name__ == "__main__":
    main()
