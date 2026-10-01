# Updating legal rules, templates and prompts

Everything legal lives in `data/document-types/<id>/`. **No code change is needed** for the tasks below. After any edit, run:

```bash
dotnet test && dotnet run --project src/DzLegal.Harness
```

The loader validates the definition: unknown fields or sections, duplicate ids, bad regexes and missing files all fail with a precise message.

## Change a required element

Edit its entry under `elements` in `definition.json`:

```jsonc
{
  "id": "art15.3.defendant",                 // stable id, used by the corpus
  "article": "CPCA art. 15-3",               // shown to the lawyer
  "severity": "inadmissible",                // or "warning"
  "label":          { "ar": "…", "en": "…" },
  "missingMessage": { "ar": "…", "en": "…" },
  "appliesWhen": [ { "field": "defendant.isLegalEntity", "isNot": "true" } ],
  "check": { "kind": "allOf", "checks": [
    { "kind": "fieldsPresent", "fields": [ "defendant.lastName", "defendant.firstName" ] },
    { "kind": "anyOf", "checks": [
      { "kind": "fieldsPresent", "fields": [ "defendant.domicile" ] },
      { "kind": "fieldsPresent", "fields": [ "defendant.lastKnownDomicile" ] } ] } ] },
  "verified": false,                         // set to true once a lawyer has confirmed it
  "sourceNote": "What the rule is based on and what remains to confirm."
}
```

Check kinds are listed in [ARCHITECTURE.md](ARCHITECTURE.md#the-rule-model). When a lawyer confirms an element, set `"verified": true` and put the confirmation (who, when, which text) in `sourceNote`. Bump `version` whenever rules change.

## Add an input field

Add it to `fields`, with an `id`, a `group`, a bilingual `label` and a `kind` of `text`, `multilineText`, `boolean`, `date` or `choice`. A `choice` field also needs `options`: a list of `{ "value": "...", "label": { "ar": "...", "en": "..." } }`. It appears in the form automatically. Use it in templates as `{{ group.name }}`, for example `{{ plaintiff.profession }}`.

## Change the document wording

- **Generated sections:** edit `templates/<section>.scriban` (Scriban syntax).
  - Blank inputs are `null`, so `{{ if x }}` hides them.
  - Boolean fields are real booleans.
  - `lines x` splits a multi-line field into items.
  - `gendered plaintiff.gender "السيد" "السيدة"` picks the agreeing form, and shows `السيد/السيدة` while no gender is chosen.
- **Narrative sections:** edit `prompts/<section>.ar.txt`.
  - `{{ section_notes }}` is the lawyer's notes for that section.
  - Any field is available by id, for example `{{ notes.requests }}` or `{{ plaintiff.lastName }}`. The shared drafting rules are in `prompts/system.ar.txt`. Keep the "do not invent facts, amounts or citations" rules.
- **Order of sections:** the order of `sections` in `definition.json`. Also update the `form.order` element if it should be enforced.

After any prompt change, re-run the drafting bench (see the README) on the same models and compare `summary.md` and the outputs. Add a case to `tests/drafting-cases/` for every new kind of failure you see.

## Add a corpus case (quality harness)

Add `tests/corpus/<document-type>/<name>.case.json`. Cases usually extend a complete base document and introduce one defect:

```json
{
  "name": "defendant with neither domicile nor last known domicile",
  "extends": "_base-natural-person.json",
  "setInputs": { "defendant.domicile": "", "defendant.lastKnownDomicile": "" },
  "setSections": { "facts": "…" },
  "removeSections": [ "grounds" ],
  "sectionOrder": [ "header", "…" ],
  "expectedMissing": [ "art15.3.defendant" ],
  "expectedNotApplicable": [ "art15.4.plaintiff", "art15.4.defendant", "art17.publication" ]
}
```

Every element not listed as missing or not applicable is expected to be present. `expectedNotApplicable` is inherited from the extended file unless overridden. Every element must be exercised as missing by at least one case; a test enforces this.

When a lawyer finds a real document that the engine judges wrongly, turn it into a corpus case **first**, then fix the rule.

## Add a document type (after Target 1)

Create `data/document-types/<new-id>/` with its own `definition.json`, templates and prompts, plus a corpus folder. The engine, drafting layer and harness need no change. The app currently opens `requete-introductive` directly (see `Composition.cs`); a document-type picker is the only UI work needed.
