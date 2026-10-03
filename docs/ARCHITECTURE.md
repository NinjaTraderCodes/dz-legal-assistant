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
  - `DraftOptions.KeepNarrative` keeps sections the lawyer edited; `DraftSectionAsync` redrafts one section; `OnPartial` streams text as the model writes it (`IModelProvider.StreamAsync`, which Ollama implements with `stream: true`).
  - `SectionReviser.ReviseAsync` asks the model to edit one section following the lawyer's instruction (`prompts/revise.ar.txt`). The proposal goes through the same cleaning, corrections and `NarrativeChecks` as a draft (numbers in the instruction count as known), is retried once on a hard failure, and is flagged as a refusal when the model returns the fixed «خارج نطاق التعديل» line. `TextDiff` produces the word-level diff shown for review. Nothing is applied until the lawyer accepts.
  - **Selected articles:** `DraftOptions.LegalTexts`, `DraftSectionAsync(..., legalTexts)` and `SectionReviser.ReviseAsync(..., legalTexts)` carry the official texts of the articles the lawyer ticked. Prompts receive them as `legal_texts`. `NarrativeChecks` reports `UncitedArticle` (an article neither in the notes nor ticked) and `MisquotedArticle` (a «…» quote that isn't in the ticked article's text). Both count as invented law, which earns one retry.
  - `Refresh` re-applies form changes without calling the model and keeps the lawyer's narrative edits.
  - `BuildRequest` exposes the exact prompt for inspection and tests.
- **NarrativeChecks** (in Drafting) checks model text against the lawyer's material:
  - foreign-script characters, a hard failure;
  - numbers not found in any input, such as an invented amount, date or article;
  - numbers from the notes that the text dropped;
  - Latin-script words found in no input (for example «caso»);
  - text that is mostly not Arabic (for example English reasoning), a hard failure.

  Month names (جانفي، يناير، janvier…) count as their numbers, so «15 جانفي 2025» matches `15/01/2025`. Wording that appears in the prompt's instructions (for example «في الشكل: قبول الدعوى شكلا») never counts as copying the example.

  Paragraphs that copy the prompt's worked example are removed first. Detection uses the example's placeholder tokens such as «(س)», or near-verbatim overlap. `DraftAsync` then retries an empty or hard-failing answer once, falls back to the notes verbatim, and records each step in `DraftSection.DraftingNotes`. The app re-runs the checks on every edit and shows them as review hints under the section. They cannot detect meaning-level errors such as swapped roles, which remain the lawyer's review.
- **ModelProviders**: `StubModelProvider` (template mode) and `OllamaModelProvider` (loopback-only HTTP to a local Ollama, streaming NDJSON, and `GetInstalledModelsAsync` over `/api/tags` for the status dot). Adding llama.cpp or another runtime means one new class implementing `IModelProvider`.
- **Documents**: `DocumentLayoutBuilder` turns a draft into print blocks, using the per-section `layout` data.
  - `PdfRenderer` (QuestPDF) produces an A4 right-to-left PDF with the embedded Naskh font, and the page images used by the in-app preview, so the preview is exactly the PDF.
  - `DocxRenderer` (Open XML SDK) produces an editable, schema-valid right-to-left `.docx`.
  - Fonts live in `assets/fonts/` and are shared by the app and the PDF engine.
- **Law** (`DzLegal.Law`): the offline law library. `LawLibraryLoader` validates `data/law/codes/*.json`. `LawTextParser` splits an official text into articles (headings, «مكرر» numbers, reversed PDF text, repealed markers) and reports gaps for review. `LawDiff` compares two imports. `ArabicScript.Normalize` and `ArabicScript.Tokens` in Domain give the shared Arabic normalisation used for matching. The importer itself (`law import` in the Harness) is the only code that downloads anything, and only at development time. See [LAW-LIBRARY.md](LAW-LIBRARY.md).
- **Grounds suggestions** (`DzLegal.Law`): `LegalKnowledgeLoader` loads the source manifest, the imported codes and the claim catalog, and checks them against each other. `GroundsSuggester` ranks claim types from the facts, filtered by the area named in the court section. `ArticleSearch` finds articles by number or words. `ClaimPicker` (in Drafting) is the model fallback. It sends the facts and a closed list of claim ids to the local model and keeps only exact ids from that list. `LegalKnowledge.ProvenanceOf` supplies the official reference, the location and the source link shown under each article. Links are restricted to `OfficialLinks` hosts and opened only on click, through `IExternalLinks`. In the app, `MainWindowViewModel.Grounds.cs` keeps the ticked articles. They are saved in `DraftDocument.SelectedArticles` and turned into `LegalText`s for drafting.
- **Storage**: drafts (`*.dzdraft.json`) and evaluation records as local JSON under the user's local app-data folder.
- **Harness** has two modes:
  - Its default mode loads the corpus, runs the Form Engine, and prints per-element accuracy, precision and recall. The same logic runs as a test.
  - The `draft` mode is a model-comparison bench over `tests/drafting-cases/`.

## Data flow

0. Since rules v0.3.0, **facts are a generated section**. Each line of the lawyer's facts notes becomes «حيث إن …» or «وحيث إن(ه) …» through the `whereas` template helper. Benchmarks showed 3–4B models reversing who lent and who borrowed.
1. The case form is generated from `fields` in `definition.json`. Values are strings keyed by field id (for example `plaintiff.domicile`). Visibility follows each field's `visibleWhen` conditions.
2. On every form change, `Refresh` re-renders the **generated** sections (court, parties, subject, publication, opening, documents, date, signature). Sections with an `includeWhen` condition, such as land-registry publication, appear or disappear.
3. **Draft narrative sections** sends one request per narrative section. Each request is the system prompt (rules: no invented facts, amounts or citations; formal Arabic; text only) plus the section prompt with the lawyer's notes embedded. Output is cleaned (`<think>` blocks, Markdown, a repeated heading). Empty notes mean no model call, so the section stays empty and gets flagged.
4. Narrative sections are freely editable, and the section assistant proposes edits as a diff to accept or reject; each section keeps an undo history. Re-drafting skips sections the lawyer edited. Generated sections are read-only in the editor and change through the form. This keeps what the lawyer sees identical to what the Form Engine checks.
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
- `DzLegal.Drafting.Tests`: template model; grounding (prompt contents, no call without notes); refresh semantics; output cleaning; keep-edits and single-section redraft; the reviser (checks, retry, refusal) and word diff; Ollama streaming and model listing; `LoopbackGuard`; the Ollama client against a fake HTTP handler.
- `DzLegal.App.Tests`: the whole loop through the view model; headless Skia rendering for the RTL layout, language switch and bidi ordering. Set `DZLEGAL_SCREENSHOTS=<folder>` to save screenshots.
