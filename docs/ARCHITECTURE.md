# Architecture

## Modules and dependencies

```
                 ┌──────────────┐
                 │ DzLegal.App  │  Avalonia shell, MVVM (CommunityToolkit.Mvvm), .resx strings
                 └──────┬───────┘
      ┌─────────────┬───┴──────────┬───────────────┬──────────────┐
      ▼             ▼              ▼               ▼              ▼
 FormEngine     Drafting   ModelProviders.*     Storage     (data/ folder)
      │             │        Stub   Ollama         │
      │             │◄─────────┴──────┘            │
      └──────┬──────┘                              │
             ▼                                     │
       DzLegal.Domain  ◄───────────────────────────┘
```

- **Domain** holds the records (`DocumentTypeDefinition`, `RequiredElement`, `RuleCheck`, `DraftDocument`, `ComplianceReport`) and `DefinitionLoader`, which parses and **validates** `definition.json`. It checks for unknown fields or sections, duplicate ids, bad regexes and missing template files, so a broken data edit fails at load time and in CI.
- **FormEngine** is a single pure function, `ComplianceChecker.Check(definition, draft) → report`. It has no I/O, no AI and no clock, which makes it deterministic and fast enough to run on every keystroke.
- **Drafting**:
  - `DraftingService.DraftAsync` renders the *generated* sections from Scriban templates and asks an `IModelProvider` to write the *narrative* sections.
  - `Refresh` re-applies form changes without calling the model and keeps the lawyer's narrative edits.
  - `BuildRequest` exposes the exact prompt for inspection and tests.
- **ModelProviders**: `StubModelProvider` (template mode) and `OllamaModelProvider` (loopback-only HTTP to a local Ollama). Adding llama.cpp or another runtime means one new class implementing `IModelProvider`.
- **Storage**: drafts (`*.dzdraft.json`) and evaluation records as local JSON under the user's local app-data folder.
- **Harness**: loads the corpus, runs the Form Engine, and prints per-element accuracy, precision and recall. The same logic runs as a test.

## Data flow

1. The case form is generated from `fields` in `definition.json`. Values are strings keyed by field id (for example `plaintiff.domicile`). Visibility follows each field's `visibleWhen` conditions.
2. On every form change, `Refresh` re-renders the **generated** sections (court, parties, subject, publication, opening, documents, date, signature). Sections with an `includeWhen` condition, such as land-registry publication, appear or disappear.
3. **Draft narrative sections** sends one request per narrative section. Each request is the system prompt (rules: no invented facts, amounts or citations; formal Arabic; text only) plus the section prompt with the lawyer's notes embedded. Output is cleaned (`<think>` blocks, Markdown, a repeated heading). Empty notes mean no model call, so the section stays empty and gets flagged.
4. Narrative sections are freely editable. Generated sections are read-only in the editor and change through the form. This keeps what the lawyer sees identical to what the Form Engine checks.
5. After every change the Form Engine produces a report. Missing mandatory elements are listed first, then warnings, then present and not-applicable elements.

## The rule model

Each `RequiredElement` has:
- an `id` and an `article` (the legal reference);
- a `severity`: `inadmissible` or `warning`;
- `appliesWhen` conditions;
- bilingual `label` and `missingMessage`;
- `verified` and `sourceNote`;
- a `check` built from a closed set of kinds:

| kind | passes when |
|---|---|
| `fieldsPresent` | every listed input has a non-blank value |
| `sectionNotEmpty` | the section exists and has text |
| `sectionMatches` | the section text matches a regex (for example a date) |
| `sectionOrder` | the listed sections that are present appear in this order |
| `predominantlyArabic` | at least `minRatio` of the letters are Arabic script (CPCA art. 8), so inline French terms and Latin names are tolerated |
| `allOf` / `anyOf` | composition (for example domicile **or** last known domicile) |

The check set is deliberately small. Anything new should be a new kind with unit tests, never logic hidden in the UI.

## Key decisions

| Decision | Why |
|---|---|
| .NET 10 LTS + Avalonia 12 | Matches the product owner's C# skills. Cross-platform, and builds and tests headlessly on Linux CI. .NET 8 support ends Nov 2026. |
| Generated vs narrative sections | Legally critical identification data comes straight from the form, deterministically. The model only rephrases notes, which limits hallucination. |
| JSON files, not SQLite | Drafts and evaluations are small documents; JSON is human-readable, diff-able and easy to back up. SQLite can come later if querying is needed. |
| Scriban templates | Readable by non-developers, with conditionals for legal-person or real-property variants, and offline. |
| Bundled Noto Naskh Arabic | Consistent Arabic shaping on every machine with no font download. |
| Document and inputs always RTL | Even with the English UI, Arabic content with embedded names and numbers must lay out correctly. This is covered by a bidi hit-test test. |

## Testing

- `DzLegal.FormEngine.Tests`: every check kind; definition validation; one test per corpus case; whole-corpus 100%; every element exercised as missing at least once; the harness detects a wrong expectation.
- `DzLegal.Drafting.Tests`: template model; grounding (prompt contents, no call without notes); refresh semantics; output cleaning; `LoopbackGuard`; the Ollama client against a fake HTTP handler.
- `DzLegal.App.Tests`: the whole loop through the view model; headless Skia rendering for the RTL layout, language switch and bidi ordering. Set `DZLEGAL_SCREENSHOTS=<folder>` to save screenshots.
