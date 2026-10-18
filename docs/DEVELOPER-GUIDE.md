# Developer guide

This guide is for working on the code by hand. It covers:
- which file does what;
- where every prompt lives and which code fills it;
- how the features call each other at runtime.

For the *why* behind the design, see [ARCHITECTURE.md](ARCHITECTURE.md). For changing the legal rules without touching code, see [UPDATING-LEGAL-RULES.md](UPDATING-LEGAL-RULES.md). For the law library, see [LAW-LIBRARY.md](LAW-LIBRARY.md).

Contents:
1. [Solution map](#1-solution-map)
2. [Where to change what](#2-where-to-change-what)
3. [The data folder](#3-the-data-folder)
4. [All prompts](#4-all-prompts)
5. [Runtime flows](#5-runtime-flows)
6. [File reference](#6-file-reference)
7. [Guardrails on model output](#7-guardrails-on-model-output)
8. [Offline and security boundaries](#8-offline-and-security-boundaries)
9. [Persistence](#9-persistence)
10. [Localization and UI](#10-localization-and-ui)
11. [Tests, harness and CI](#11-tests-harness-and-ci)
12. [Recipes](#12-recipes)
13. [Gotchas](#13-gotchas)

---

## 1. Solution map

```
DzLegal.App  (Avalonia desktop, offline)         DzLegal.Web  (Blazor Server web demo)
 │   └─► ModelProviders.Ollama                     └─► ModelProviders.Hosted (Google AI Studio, OpenAI-compatible)
 └──────────────┬──────────────────────────────────────┘
                ▼
DzLegal.Presentation  (view models, strings: shared by both shells)
 ├─► DzLegal.Drafting ──► DzLegal.Law ──► DzLegal.Domain
 │        ▲                                   ▲
 ├─► DzLegal.ModelProviders.Ollama ─┘ (implements IModelProvider)
 ├─► DzLegal.ModelProviders.Stub ───┘
 ├─► DzLegal.FormEngine ──────────────────────┤
 ├─► DzLegal.Documents (PDF, Word) ───────────┤
 └─► DzLegal.Storage (files on disk) ─────────┘

DzLegal.Harness (console) ─► FormEngine, Drafting, Law, Storage, both providers
```

| Project | Responsibility | External packages |
|---|---|---|
| `DzLegal.Domain` | Records only: the document-type definition, drafts, the compliance report, Arabic text helpers, the definition loader and validator. No I/O except reading the definition. | — |
| `DzLegal.FormEngine` | **Deterministic** CPCA compliance check: definition + draft → report. No AI. | — |
| `DzLegal.Law` | The law library (imported codes), BM25 article search, the claim catalog, official-link rules, and the loader for `data/law`. | — |
| `DzLegal.Drafting` | Everything that talks to a model: drafting, section revision, grounds analysis, Scriban templates, and the checks on model output. | Scriban |
| `DzLegal.ModelProviders.Ollama` | HTTP client for a **loopback** Ollama server (`/api/chat`, `/api/tags`). | — |
| `DzLegal.ModelProviders.Hosted` | Hosted engines for the **web demo only**: Gemini API (Gemma 3), OpenAI-compatible APIs, a fallback chain. Never referenced by the desktop app. | — |
| `DzLegal.Web` | The public web demo: Blazor Server pages over the shared view models, a queue in front of the hosted model, sample cases, health endpoint. See [WEB-DEMO.md](WEB-DEMO.md). | ASP.NET Core |
| `DzLegal.ModelProviders.Stub` | "Template mode": returns the lawyer's notes unchanged. Used without AI, in tests and in CI. | — |
| `DzLegal.Documents` | Layout model → PDF (QuestPDF) and Word (OpenXML), RTL, Arabic fonts. | QuestPDF, DocumentFormat.OpenXml |
| `DzLegal.Storage` | `.dzdraft.json` files, autosave, evaluation records, per-user folders. | — |
| `DzLegal.Presentation` | The screens' logic, shared by the desktop app and the web demo: view models, UI strings (`Resources/Strings*.resx`), file and link service interfaces. No UI framework. | CommunityToolkit.Mvvm |
| `DzLegal.App` | The desktop app: composition, XAML views, converters, Avalonia services, fonts. | Avalonia 12, FluentIcons |
| `DzLegal.Harness` | Console tool: the Form Engine quality harness, the drafting bench, the grounds bench, law import. | — |

Each project has a matching `tests/<Project>.Tests` (xunit v3), except Domain, Harness and the providers. Their tests live in FormEngine.Tests and Drafting.Tests.

**Build-wide settings** (repository root):
- `Directory.Build.props`: net10.0, nullable, `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`. An analyzer warning breaks the build.
- `Directory.Packages.props`: central package versions. Change versions here, never in a `.csproj`.
- `Directory.Build.targets`: copies the data into the build output.
  - `IncludeLegalData=true` copies `data/**` next to the binaries, under `data/`.
  - `IncludeQualityCorpus=true` copies `tests/corpus`, `tests/drafting-cases` and `tests/grounds-cases`.
  - **At runtime, the app and the harness read the copies in `bin/…/data`, not the repository.** A rebuild is needed after editing a data file.
- `global.json`: pins the SDK.

**Commands:**

```bash
dotnet build                      # warnings are errors
dotnet test                       # all unit + headless UI tests
dotnet run --project src/DzLegal.App
dotnet run --project src/DzLegal.Harness                 # Form Engine corpus: must be 100%
dotnet run --project src/DzLegal.Harness -- draft   --models gemma3:4b --out bench
dotnet run --project src/DzLegal.Harness -- grounds --models gemma3:4b --out bench-grounds
dotnet run --project src/DzLegal.Harness -- law import civil-code --file <pdf> --as-of 2026-01-01
DZLEGAL_SCREENSHOTS=$PWD/docs/images dotnet test tests/DzLegal.App.Tests   # refresh screenshots
```

---

## 2. Where to change what

| I want to… | Edit | Notes |
|---|---|---|
| Change how the model writes the **legal grounds** | `data/document-types/requete-introductive/prompts/grounds.ar.txt` | Scriban. See §4. |
| Change how the model writes the **requests** | `…/prompts/requests.ar.txt` | Scriban. |
| Change the drafting rules shared by every section | `…/prompts/system.ar.txt` | Sent as the system message. |
| Change the **section assistant** («المساعد») | `…/prompts/revise.ar.txt`; shortcuts in `definition.json` → `revisionShortcuts` | |
| Change how the facts are **read** (grounds analysis, stage 1) | `data/law/prompts/read-facts.ar.txt`; parser `GroundsAnalyzer.ParseReading` | Plain text replacement, not Scriban. |
| Change how **articles are chosen** (stage 3) | `data/law/prompts/select-articles.ar.txt`; parser `GroundsAnalyzer.ParseSelection` | Plain text replacement. |
| Change the **candidate search** (stage 2) | `GroundsAnalyzer.Candidates`, `src/DzLegal.Law/ArticleIndex.cs` | The weights and the limit of 12 are there. |
| Add or edit a **claim type** | `data/law/claims/<area>.json` | Validated by `LegalKnowledgeLoader.Validate`. |
| Change fixed wording (header, parties, opening, signature…) | `…/templates/*.scriban` | Generated sections: no model involved. |
| Add an **input field** | `definition.json` → `fields` (+ `fieldGroups`) | The UI is generated from it. |
| Add or change a **CPCA rule** | `definition.json` → `elements` | Add corpus cases (§11). |
| Fix a recurring model **grammar mistake** | `definition.json` → `corrections` | Applied to model text only. |
| Flag an **invented legal qualification** | `definition.json` → `groundedTerms` | |
| Section order, alignment, bold, spacing | `definition.json` → `sections[].layout` | Used by the preview, PDF and Word. |
| Change the default **model** or its parameters | `src/DzLegal.App/appsettings.json`, or a local `appsettings.local.json` | |
| UI text | `src/DzLegal.Presentation/Resources/Strings.resx` (Arabic) **and** `Strings.en.resx` | Keep the keys in sync. |
| UI layout | `src/DzLegal.App/Views/MainWindow.axaml` | Compiled bindings. |
| Add an official code to import | `data/law/sources.json`, then `law import` | |
| PDF or Word look | `src/DzLegal.Documents/PdfRenderer.cs`, `DocxRenderer.cs` | |

---

## 3. The data folder

```
data/
├─ document-types/
│  └─ requete-introductive/
│     ├─ definition.json        ← THE document type: fields, sections, rules, prompt paths
│     ├─ prompts/
│     │  ├─ system.ar.txt       ← system message for drafting and revision
│     │  ├─ grounds.ar.txt      ← section "grounds" (narrative)
│     │  ├─ requests.ar.txt     ← section "requests" (narrative)
│     │  └─ revise.ar.txt       ← section assistant
│     └─ templates/*.scriban    ← one per generated section
└─ law/
   ├─ sources.json              ← the codes: id, titles, citation name, official site, JO reference
   ├─ codes/<id>.json           ← imported official texts (empty until imported)
   ├─ reports/<id>.md           ← import review reports
   ├─ claims/{civil,commercial,family,social}.json   ← claim catalog (18 claim types)
   └─ prompts/
      ├─ read-facts.ar.txt      ← grounds analysis, stage 1
      └─ select-articles.ar.txt ← grounds analysis, stage 3
```

### `definition.json` (JSON with `//` comments)

Loaded and validated by `src/DzLegal.Domain/Serialization/DefinitionLoader.cs` into `DocumentTypeDefinition` (`src/DzLegal.Domain/DocumentTypeDefinition.cs`). A validation error stops the app at startup, and the error is shown in a window.

| Key | Type in code | Used by |
|---|---|---|
| `id`, `version`, `name`, `description` | `DocumentTypeDefinition` | Saved in every draft (`DefinitionVersion`). **Bump `version` when you change rules.** |
| `systemPrompt`, `revisionPrompt` | relative paths | `DraftingService.BuildRequest`, `SectionReviser.BuildRequest` |
| `revisionShortcuts` | `RevisionShortcut` | assistant buttons (`MainWindowViewModel.Assistant.cs` → `Shortcuts`) |
| `corrections` | `TextCorrection` | `DraftingService.ApplyCorrections` (model text only) |
| `groundedTerms` | `string[]` | `NarrativeChecks.Check` → `UngroundedTerm` |
| `fieldGroups` | `FieldGroupDefinition` | the wizard steps (9): court, plaintiff, defendant, claim, facts, grounds, requests, documents, signature |
| `fields` | `FieldDefinition` (`kind`: text, multilineText, boolean, choice, date; `visibleWhen`, `default`, `hint`, `options`) | `FieldViewModel`, `CaseInputs.CreateDefaults`, templates (as `plaintiff.lastName`…) |
| `sections` | `SectionDefinition` (`kind`: generated or narrative; `template` or `prompt`; `sourceField`; `formGroup`; `layout`; `includeWhen`; `hints`; `showTitle`) | `DraftingService`, `DocumentLayoutBuilder`, the editor |
| `elements` | `RequiredElement` + a `RuleCheck` tree | `ComplianceChecker` |

The narrative sections are `grounds` (source `notes.grounds`) and `requests` (source `notes.requests`). Everything else is generated from templates. **The facts are a generated section**: `templates/facts.scriban` inserts `notes.facts` as written, and no model rewrites them.

The rule ids are `art15.1.court`, `art15.2.plaintiff`, `art15.3.defendant`, `art15.4.plaintiff`, `art15.4.defendant`, `art15.5.facts`, `art15.5.grounds`, `art15.5.requests`, `art15.6.documents`, `art14.date`, `art14.signature`, `art17.publication` and `art8.arabic`. The check kinds (`src/DzLegal.Domain/RequiredElement.cs`) are `fieldsPresent`, `sectionNotEmpty`, `sectionMatches`, `sectionOrder`, `predominantlyArabic`, `allOf` and `anyOf`.

### Templates (Scriban)

`TemplateRenderer.BuildModel` exposes:
- every field as a nested object (`plaintiff.firstName`); booleans are real booleans, and empty text is `null`;
- three helpers: `lines(text)`, `gendered(gender, masc, fem)` and `whereas(index, line)`;
- the `extra` variables passed by the caller (the prompts use them, see §4).

`TemplateRenderer.Normalize` tidies the result: blank lines and spaces.

### Law data

| File | Loaded by | Model |
|---|---|---|
| `sources.json` | `LawSourceManifest.Load` (`LawSources.cs`) | `LawSourceEntry`: id, titles, `citationName`, `officialSite`, `joReference` |
| `codes/*.json` | `LawLibraryLoader.Load` / `Validate` | `LawCode`, `LawArticle` (`LawModel.cs`) |
| `claims/*.json` | `LegalKnowledgeLoader.Load` / `Validate` | `ClaimCatalogFile` → `ClaimType`, `ClaimArticle` (`ClaimCatalog.cs`) |
| `prompts/*.txt` | `LegalKnowledgeLoader.Load` | `AnalysisPrompts(ReadFacts, SelectArticles)` |

All four together make up a `LegalKnowledge` object (`ClaimCatalog.cs`), the single entry point for the law data at runtime.

---

## 4. All prompts

There are **six prompt files**. Two different mechanisms fill them, so be careful when editing:

| File | Filled by | Mechanism | Variables | System message |
|---|---|---|---|---|
| `document-types/…/prompts/system.ar.txt` | `DraftingService.BuildRequest`, `SectionReviser.BuildRequest` | read verbatim | — | *is* the system message |
| `document-types/…/prompts/grounds.ar.txt` | `DraftingService.BuildRequest` | **Scriban** (`TemplateRenderer.RenderFile`) | all fields, `section_notes`, `legal_texts` | `system.ar.txt` |
| `document-types/…/prompts/requests.ar.txt` | `DraftingService.BuildRequest` | **Scriban** | all fields, `section_notes` (`legal_texts` is passed but not used) | `system.ar.txt` |
| `document-types/…/prompts/revise.ar.txt` | `SectionReviser.BuildRequest` | **Scriban** | all fields, `section_title`, `section_notes`, `current_text`, `instruction`, `legal_texts` | `system.ar.txt` |
| `law/prompts/read-facts.ar.txt` | `GroundsAnalyzer.BuildReadingRequest` | **plain `string.Replace`** | `{{ subject }}`, `{{ facts }}`, `{{ claims }}` | none |
| `law/prompts/select-articles.ar.txt` | `GroundsAnalyzer.BuildSelectionRequest` | **plain `string.Replace`** | `{{ facts }}`, `{{ reading }}`, `{{ candidates }}` | none |

What each variable holds:
- `section_notes`: the section's `sourceField` value. For grounds this is `notes.grounds` *after* `DraftingInputs()` adds the ticked citations, see §5.3.
- `legal_texts`: `LegalText.Block(...)`, the official text of each ticked article that is in the library. It is `null` when there is none, so `{{ if legal_texts }}` is false (see §13).
- `{{ claims }}`: one line per catalog claim type of the court section's area, `id — label: description`, ending with `other`.
- `{{ reading }}`: the five lines from stage 1.
- `{{ candidates }}`: `key — citation — first 180 characters` for each candidate.

**Worked examples** in Scriban prompts are wrapped in `[[مثال]] … [[/مثال]]`:
- The model sees the content without the markers.
- `BuildRequest` keeps the example text aside as `ModelRequest.ExampleText`.
- `NarrativeChecks.RemoveCopiedExample` deletes output lines copied from it unless the lawyer's notes contain them.

**The expected answer formats are parsed in code.** If you change a format in a prompt, update its parser and its tests in the same commit:

| Prompt | Expected answer | Parser | Tests |
|---|---|---|---|
| read-facts | five labelled lines (relationship, breach, sought, claim type, terms) | `GroundsAnalyzer.ParseReading` (+ `FindClaims`, `NameVariants`, `ValueAfterLabel`) | `tests/DzLegal.Drafting.Tests/GroundsAnalyzerTests.cs` |
| select-articles | `key \| role \| reason` per line, or `لا شيء` | `GroundsAnalyzer.ParseSelection` | same |
| grounds, requests | Arabic prose | `DraftingService.CleanModelOutput` (removes think blocks, preambles, fences, a repeated title) | `DraftingServiceTests.cs` |
| revise | Arabic prose, or the refusal marker `خارج نطاق التعديل` | `SectionReviser.ReviseAsync` | `AssistantTests.cs` |

The tests that assert prompt content (`BuildRequest` tests in `DraftingServiceTests`, `AssistantTests` and `GroundsAnalyzerTests`) render the real files from `data/`, so a broken placeholder fails `dotnet test`.

---

## 5. Runtime flows

### 5.1 Startup

```
Program.Main  (src/DzLegal.App/Program.cs)
  └─ BuildAvaloniaApp().WithArabicFont()             Fonts.cs: embedded Noto Naskh Arabic
     └─ App.OnFrameworkInitializationCompleted        App.axaml.cs
        └─ App.CreateMainWindow
           ├─ AppSettings.Load(appsettings.json [+ appsettings.local.json])
           ├─ AppPaths.Default                        %LOCALAPPDATA%\DzLegalAssistant\{drafts,evaluations}
           └─ Composition.CreateMainViewModel         Composition.cs
              ├─ Loc.SetLanguage(settings.Language)
              ├─ DefinitionLoader.Load(bin/data/document-types/requete-introductive)
              ├─ CreateProviders(settings)  → [ProviderOption "ollama", ProviderOption "stub"]
              ├─ LegalKnowledgeLoader.Load(bin/data/law)
              └─ new MainWindowViewModel(definition, providers, files, EvaluationStore, loc,
                                         defaultProvider, autosaveFile, knowledge, links)
```

- Any load error (bad JSON, failed validation, `LawDataException`) shows a window containing the exception instead of crashing.
- `MainWindow.axaml.cs` starts a 30-second `DispatcherTimer` that does two things on every tick:
  - `AutosaveNow()`;
  - `CheckModelStatusAsync()`, which probes Ollama `/api/tags` through `ProviderOption.ProbeAsync` → `OllamaModelChoice.ProbeAsync` (which also picks the text-only model when it is installed).
- **Warm-up:** the first time the probe finds the model ready, `CheckModelStatusAsync` starts `ProviderOption.WarmUpAsync` in the background, once per session. It calls `Composition.WarmUpOllamaAsync`, then `OllamaModelProvider.WarmUpAsync`, which sends `POST /api/generate` with no prompt and `keep_alive` set to 30 minutes. This loads the model into memory while the lawyer fills the form.
  - The status dot reads "loading into memory…", then "ready in memory".
  - A failure is silent and not retried.
  - Turn it off with `ollama.warmUp: false` in the settings.
- **Waiting** (`MainWindowViewModel.Waiting.cs`):
  - *Time left.* `TaskTimings` keeps the last 5 durations of the analysis and of a full draft, per engine, in `%LOCALAPPDATA%\DzLegalAssistant\timings.json`. The activity panel shows the median minus the elapsed time, rounded up to 10 s, then "taking longer than usual". Before the first run on a computer it shows a hint instead (about 2 minutes for the analysis, 1–2 for a draft). Template mode is never timed.
  - *Power.* `WindowsPowerStatus` (`GetSystemPowerStatus`) reports battery or energy saver; the panel then says the model will be slower. Local engines only.
  - *Background analysis.* Leaving the facts step starts the grounds analysis when `analyseInBackground` is on (default), the facts are filled, nothing was analysed yet, and a local engine is ready. It is never cancelled by moving between steps. Nothing is ticked: the lawyer still confirms the claim type. An online engine never starts on its own.
- On close, the window autosaves and disposes.

### 5.2 Typing in the form

Every field change calls `MainWindowViewModel.OnInputChanged()`:

```
FieldViewModel.Value set ─► OnInputChanged
   ├─ UpdateVisibility()                fields' visibleWhen (Condition.All)
   ├─ DraftingService.Refresh(...)      re-render generated sections, keep the narrative text and overrides
   ├─ SyncSections(false)               mirror into the SectionViewModels
   ├─ UpdateAnalysisStaleness()         FactsHash(subject, facts, section) ≠ saved one → stale banner
   └─ Recheck()
        ├─ ComplianceChecker.Check(definition, draft)  → Findings, IssueFindings, PresentFindings, score
        ├─ UpdateStepsAndFields()        step badges and per-field "required by art. …"
        ├─ UpdateReviewHints()           section hints + drafting notes + NarrativeChecks on the narrative text
        └─ SchedulePreview(400 ms)       PdfRenderer.RenderPageImages on a worker thread, only if the preview tab is open
```

No model is called while typing. Everything here is deterministic and synchronous, except the preview.

### 5.3 Generate the draft («إنشاء المسودة»)

`MainWindowViewModel.GenerateAsync` (main file), a cancellable command:

1. `provider = SelectedProvider.Create()`: an `OllamaModelProvider` or a `StubModelProvider`.
2. `EditedNarrative()`: narrative sections the lawyer edited are kept and not redrafted.
3. `DraftingInputs()` (Grounds partial): prepends `- المادة N من <code>` for each ticked article to `notes.grounds`, unless the notes already cite it.
4. `SelectedLegalTexts()`: the official texts of the ticked articles that are in the library.
5. `DraftingService.DraftAsync(definition, inputs, DraftOptions{Progress, KeepNarrative, OnPartial, LegalTexts})`. For each included section:
   - **Generated section:** `RenderGenerated`, i.e. the Scriban template.
   - **Narrative section with empty notes:** the section stays empty, and the model is not called.
   - **Narrative section with notes:** `WriteNarrativeAsync`, up to 2 attempts:
     ```
     BuildRequest → provider.StreamAsync (OnPartial shows live text)
       → CleanModelOutput → RemoveCopiedExample → ApplyCorrections
       → NarrativeChecks.Check(text, notes, inputs, groundedTerms, legalTexts)
     ok (no hard issue, not invented law)        → keep
     invented law on attempt 1                   → retry (note "retried")
     hard issue / empty after 2 attempts         → fall back to the lawyer's notes (note "fellBackToNotes")
     ```
6. The overridden sections are put back, and the model text is remembered for redraft. Then `SyncSections(true)` and `Recheck()`.

**Live feedback:** `GenerationActivity`, a `ModelActivityViewModel` (see 5.9), has one step per narrative section.
- The progress callback calls `BeginStep` for the section being drafted.
- `MarkWriting` sets `SectionViewModel.IsWriting` on that section, which shows a pulsing "the model is writing…" marker in its header.
- `OnPartial` feeds both the section text and the activity's word count.

### 5.4 Grounds analysis («حلّل الوقائع واقترح الأسانيد»)

`MainWindowViewModel.AnalyseGroundsAsync` (Grounds partial) calls `GroundsAnalyzer.AnalyseAsync(knowledge, subject, facts, courtSection, progress, ct)`:

```
area   = LegalAreas.AreaOf(court.section)                    CatalogArticles.cs
claims = catalog claim types of that area (all when unknown)

Stage 1  Reading    BuildReadingRequest(read-facts.ar.txt)  → provider.StreamAsync (OnPartial)
                    ParseReading → FactsReading{Relationship, Breach, Sought, ClaimIds (≤2, "other" → none), SearchTerms}
Stage 2  Searching  Candidates(): catalog articles of ClaimIds first,
                    then library.Index.Search([(terms, 1.0), (subject+facts, 0.5)])   ArticleIndex.cs (BM25)
                    ≤ 12 CandidateArticle{Key, Ref, Citation, Excerpt(180), Source catalog|search}   → OnCandidates
Stage 3  Selecting  BuildSelectionRequest(select-articles.ar.txt) → provider.StreamAsync (OnPartial)
                    ParseSelection → ≤ 6 AnalysedArticle{Code, Number, Role, Reason, Source}
Result   GroundsAnalysis{FactsHash, Relationship, Breach, Sought, ClaimIds, Articles, Model}
```

Then `ShowAnalysis(analysis)` fills the UI:
- the reading card;
- `AnalysedArticles`: an `ArticleChoiceViewModel` per article, with role, model reason, official text, provenance (`LegalKnowledge.ProvenanceOf`) and link;
- `RequestChips` and `DocumentChips`, built from the claim types' `SuggestedRequests` and `RequiredDocuments`;
- `Prerequisites`.

**Nothing is applied until the lawyer ticks an article.**
- A tick goes `ArticleChoiceViewModel.IsSelected` → `OnArticleToggled` → `SetSelected`, which updates `_selectedArticles` and `SelectedGrounds`.
- The manual search box goes `ArticleQuery` → `SearchArticles` → `ArticleSearch.Find`, which matches a number with code hints, otherwise searches the BM25 index. Its results show in `ArticleResults`.
- `TickBasisArticlesCommand` ticks every article whose role is "basis".
- The analysis and the ticks are saved in the draft: `GroundsAnalysis` and `SelectedArticles`.
- `GroundsAnalyzer.Trace` is a hook used only by the bench, to record the prompts and answers.
- **Live feedback:** `GroundsAnalyzer.OnPartial` (the answer so far, per stage) and `OnCandidates` (the stage 2 result) drive `AnalysisActivity`. It shows three steps: reading → searching ("n candidate articles") → choosing, with the model's words live. Both model calls stream, but the parsers still run on the complete answer.

### 5.5 Section assistant

In `MainWindowViewModel.Assistant.cs`:
- A section's ✎ button → `SectionViewModel.OpenAssistantCommand` → `AssistantSection` is set.
- Sending an instruction (`SendInstructionAsync`), or clicking a shortcut, calls `SectionReviser.ReviseAsync(definition, sectionId, currentText, instruction, DraftingInputs(), onPartial, SelectedLegalTexts())`:
  - It streams, then runs `CleanModelOutput` and `ApplyCorrections`.
  - If the answer contains `خارج نطاق التعديل`, it returns a refusal.
  - Otherwise `NarrativeChecks.Check`. The instruction and the current text count as "known", so their numbers and articles are allowed.
  - It retries once on invented law. The proposal is unusable if it is empty or has a hard issue.
- `ShowProposal` shows a word diff (`TextDiff.Words`). `AcceptProposal` pushes undo history (`SectionViewModel.PushHistory`) and replaces the text. Reject, Retry and Undo are also available.
- The ↻ button (redraft one section) → `RedraftSectionAsync` → `DraftingService.DraftSectionAsync`. It uses `GenerationActivity` with one step, plus the "writing" marker.
- **Live feedback:** `AssistantActivity` shows one step, the waiting state and the timer. Its live text is hidden because `AssistantPartial` already shows the answer as it streams.

### 5.6 Preview and export

```
DocumentLayoutBuilder.Build(definition, draft)    Documents/DocumentModel.cs   (sections → DocBlocks with layout)
 ├─ PdfRenderer.RenderPageImages  → preview PNGs (PreviewPages)
 ├─ PdfRenderer.RenderPdf         → ExportPdfAsync   → IUserFiles.SaveAsync(SaveKind.Pdf)
 └─ DocxRenderer.Render           → ExportWordAsync  → IUserFiles.SaveAsync(SaveKind.Word)
DocumentText.Compose(definition, draft)  → CopyTextAsync (clipboard)
```

The draft watermark is `DocumentRenderOptions.DraftWatermark`. Fonts are embedded (`DocumentFonts.OpenAll`, Noto Naskh Arabic). In Word, the complex-script font is set to "Simplified Arabic".

### 5.7 Save, open, autosave, evaluation

- **Save:** `SaveAsync` → `DraftFile.SaveAsync(CurrentDraft(), stream)`.
- **Open:** `OpenAsync` → `DraftFile.LoadAsync` → `LoadDraft`, which loads the inputs, sections, ticks and analysis (`LoadGroundsState`, then `Recheck`).
- **Autosave:** `AutosaveNow()` → `DraftFile.Save` is **synchronous**: write a temp file, then move it into place. The target is `drafts/last-session.dzdraft.json`.
  - At startup, `HasRecoverableDraft` offers `RestoreLastSessionCommand` or `DismissRecoveryCommand`.
- **Evaluation:** `SaveEvaluationAsync` → `EvaluationStore.SaveAsync`. It stores the verdict, the comment, the generated snapshot against the final text, and the provider name, in `evaluations/`.
- `CurrentDraft()` is the single place that gathers inputs, ticks and analysis into a `DraftDocument`. Use it for anything that persists.

### 5.8 Speed of the local model

On a CPU-only 8 GB laptop each call costs three things:
- **Load:** reading the model from disk. It is paid once if the model stays in memory.
- **Prompt read:** usually the biggest cost. It grows with everything we send.
- **Write:** a few tokens per second for a 4B model.

What the code does about each:

| Lever | Where |
|---|---|
| Model kept in memory: `keep_alive` (setting `ollama.keepAlive`, default `15m`) sent with **every** request and the warm-up. Ollama's default is 5 minutes. Closing the window unloads it (`keep_alive: 0`, `OllamaModelProvider.UnloadAsync`), so about 3 GB are not held after the work. | `OllamaOptions.KeepAlive`, `OllamaModelProvider.Body`, `WarmUpAsync` |
| One fixed context window (`ContextLength`, 4096) for every call, the warm-up included. A different `num_ctx` makes Ollama reload the model. | `OllamaOptions.ContextLength` |
| A per-call answer cap, never above `maxTokens`: reading 300, selection 400, grounds 900, requests 450 (the revision uses its section's cap). | `ModelRequest.MaxTokens`, `GroundsAnalyzer.ReadingMaxTokens`/`SelectionMaxTokens`, `definition.json` → `sections[].maxTokens` |
| Less to read: at most 12 candidates of 180 characters; each ticked article at most 800 characters in the prompt. Quotations are still checked against the whole text. | `GroundsAnalyzer.MaxCandidates`/`ExcerptLength`, `LegalText.MaxPromptLength` |
| Prompt budgets: tests fail if the drafting prompts grow past their budget. | `LegalTextTests.DraftingPromptsStayWithinTheirBudget` |
| Measurement: Ollama's own timings (load, prompt tokens and duration, answer tokens and duration), plus time to first word. | `OllamaModelProvider.OnTimings` → `ModelTimings`; the benches print them with `SpeedLog` |

`ModelTimings.PromptMayBeTruncated` flags a prompt that filled the context window. Ollama drops the beginning of such a prompt silently.

### 5.9 Live model activity

`ViewModels/ModelActivityViewModel.cs` is one instance per long task: `AnalysisActivity`, `GenerationActivity` and `AssistantActivity` on `MainWindowViewModel`.

| Call | Effect |
|---|---|
| `Start(steps)` | Shows the panel and starts the timer (`Elapsed`, m:ss, ticked every second by a `DispatcherTimer`). |
| `BeginStep(i, waitsForModel)` | Earlier steps become done and step i becomes active. While the model has not answered, `IsWaiting` shows an indeterminate bar and "waking up the model…". After 15 s (`LoadingHintAfter`) it says the model is loading into memory. |
| `OnText(textSoFar)` | Ends the waiting state and updates `LiveText` (the last 240 characters) and the word count. |
| `SetDetail(i, text)` | A short note on a step, such as the number of candidates. |
| `Complete()` / `Stop()` | Hides the panel (success, or cancel and error). |

The view is one `DataTemplate`, `ModelActivityTemplate` in `MainWindow.axaml`, shown by `ContentControl`s:
- `AnalysisActivityView` under the Analyse button;
- `GenerationActivityView` above the draft;
- one in the assistant pane.

The pulse animation is the `Ellipse.pulse` style. For tests, the constructor takes a clock and `useTimer: false`, and `Tick()` is internal.

---

## 6. File reference

### `src/DzLegal.Domain`
| File | Contents |
|---|---|
| `DocumentTypeDefinition.cs` | `DocumentTypeDefinition`, `FieldDefinition`, `FieldKind`, `SectionDefinition`, `SectionKind`, `SectionLayout`, `SectionHint`, `Condition` (`visibleWhen`/`includeWhen` evaluation), `TextCorrection`, `RevisionShortcut` |
| `RequiredElement.cs` | `RequiredElement`, `Severity`, and the `RuleCheck` records |
| `ComplianceReport.cs` | `ComplianceReport`, `Finding`, `ElementStatus` |
| `DraftDocument.cs` | `DraftDocument`, `DraftSection`, `DraftingNoteCodes`, `ArticleRef`, `GroundsAnalysis`, `AnalysedArticle` |
| `ArabicScript.cs` | `Normalize`, `StripDiacritics`, `ToAsciiDigits`, `Tokens`, `MatchesWord`, `SearchKey`, `Ratio`. **All Arabic text comparison goes through here.** `Normalize` strips diacritics, converts Arabic-Indic digits, collapses spaces and maps أإآٱ→ا, ى→ي, ة→ه, ؤ→و, ئ→ي. |
| `LocalizedText.cs` | `{ar, en}` with `Get(lang)` |
| `Serialization/DefinitionLoader.cs` | `Load`, `LoadAll`, `Validate` (every reference, path, regex and id), `DefinitionValidationException` |
| `Serialization/DefinitionJson.cs` | the shared `JsonSerializerOptions`: comments and trailing commas allowed, camelCase, case-insensitive, enums as camelCase strings, nulls not written, Arabic not escaped. Used for definitions **and** draft files. The `RuleCheck` subtypes are chosen by `kind`, through attributes in `RequiredElement.cs`. |

### `src/DzLegal.FormEngine`
| File | Contents |
|---|---|
| `ComplianceChecker.cs` | `Check(definition, draft)`: for every element, applicability (`appliesWhen`) and then `Evaluate(RuleCheck)` → `Finding` with its missing parts. Pure function. |

### `src/DzLegal.Law`
| File | Contents |
|---|---|
| `LawModel.cs` | `LawSource`, `LawArticle`, `LawCode` (`FindArticle`), `LawLibrary` (`FindCode`, `FindArticle`, lazy `Index`) |
| `LawLibraryLoader.cs` | loads and validates `codes/*.json`; `LawDataException` |
| `LawSources.cs` | `LawSourceManifest`, `LawSourceEntry` (`sources.json`) |
| `ArticleNumbers.cs` | `Canonical` («مكرر», `73-4`, الأولى→1), `Base`, `IsBis` |
| `ArticleIndex.cs` | BM25 index (k1 1.2, b 0.75, headings ×2); `Search(queries with weights, max)`; `ArticleHit` |
| `ArticleSearch.cs` | the manual search box: an article number (+ code hint words) or full text |
| `ClaimCatalog.cs` | `ClaimType`, `ClaimArticle`, `ArticleRole`, `ClaimCatalogFile`, `AnalysisPrompts`, **`LegalKnowledge`** (`Resolve`, `ProvenanceOf`, `CitationName`) |
| `CatalogArticles.cs` | `ArticleSuggestion`; `LegalAreas.AreaOf(courtSection)` → civil, commercial, family, social or null |
| `LegalKnowledgeLoader.cs` | loads `data/law` as a whole; `Validate` (Arabic label and description required, cited codes exist, and once a code is imported, cited articles exist and are in force) |
| `OfficialLinks.cs` | `Hosts` (joradp.dz, mjustice.dz), `IsOfficial(url)` (https only), `ArticleProvenance` |
| `LawTextParser.cs` | import parser: splits on «المادة N», fixes reversed text and presentation forms, `Review` warnings |
| `LawDiff.cs` | added, removed, changed and repealed articles between two imports |

### `src/DzLegal.Drafting`
| File | Contents |
|---|---|
| `IModelProvider.cs` | `IModelProvider` (`Name`, `GenerateAsync`, default `StreamAsync`), `ModelRequest`, `ModelProviderException` |
| `DraftingService.cs` | `DraftAsync`, `Refresh`, `RenderGenerated`, **`BuildRequest`**, `DraftSectionAsync`, `WriteNarrativeAsync` (retry or fallback), `CleanModelOutput`, `ApplyCorrections`; `DraftOptions` |
| `SectionReviser.cs` | **`BuildRequest`** (revise prompt), `ReviseAsync`, `RevisionProposal`, `RefusalMarker` |
| `GroundsAnalyzer.cs` | the 3-stage analysis: **`BuildReadingRequest`**, **`BuildSelectionRequest`**, `Candidates`, `ParseReading`, `ParseSelection`, `FactsHash`; constants `MaxClaims` 2, `MaxCandidates` 12, `MaxSelected` 6, `ExcerptLength` 180, `MaxReasonWords` 30, `OtherClaimId` "other" |
| `NarrativeChecks.cs` | `Check` (the 8 issue kinds, §7), `RemoveCopiedExample`, `ArticleReferences`, `Numbers` |
| `LegalText.cs` | `LegalText(Number, CitationName, Text)`; `Block(texts)` builds the `legal_texts` prompt variable |
| `TemplateRenderer.cs` | Scriban rendering, the model (`BuildModel`), helpers, `Normalize` |
| `TextDiff.cs` | word-level LCS diff for the assistant |
| `CaseInputs.cs` | default input values (`today` → `dd/MM/yyyy`) |
| `DocumentText.cs` | plain-text composition of the whole document (clipboard, tests) |

### `src/DzLegal.ModelProviders.*`
| File | Contents |
|---|---|
| `Ollama/OllamaModelProvider.cs` | `POST /api/chat` (stream or not), `GET /api/tags`, `WarmUpAsync` (`POST /api/generate` with no prompt); maps errors to `ModelProviderException` with readable messages |
| `Ollama/OllamaOptions.cs` | endpoint, model, timeout, temperature, maxTokens (`num_predict`), repeatPenalty, think |
| `Ollama/LoopbackGuard.cs` | refuses any endpoint that is not loopback |
| `Stub/StubModelProvider.cs` | returns `request.SourceText`, i.e. the notes as written |

### `src/DzLegal.Documents`
| File | Contents |
|---|---|
| `DocumentModel.cs` | `DocumentModel`, `DocBlock`, `DocumentLayoutBuilder.Build` |
| `PdfRenderer.cs` | `RenderPdf`, `RenderPageImages`, `DocumentRenderOptions` (A4, margins, watermark) |
| `DocxRenderer.cs` | Word output (RTL paragraphs, bidi, fonts) |
| `DocumentFonts.cs` | embedded font streams |

### `src/DzLegal.Storage`
| File | Contents |
|---|---|
| `AppPaths.cs` | the per-user root, `Drafts`, `Evaluations` |
| `DraftFile.cs` | `.dzdraft.json` load and save (stream, path async, and synchronous `Save` for autosave) |
| `EvaluationRecord.cs` | `EvaluationRecord`, `DraftVerdict`, `EvaluationStore` |

### `src/DzLegal.App`
| File | Contents |
|---|---|
| `Program.cs`, `App.axaml(.cs)` | entry point, styles, window creation and the startup error window |
| `Composition.cs` | **composition root**: data paths, providers, Ollama probe, view model wiring |
| `AppSettings.cs` | `appsettings.json` + `appsettings.local.json` merge; `OllamaSettings` |
| `Loc.cs` | localization (`L["Key"]`, `L.Format`), language switch, RTL flag |
| `Fonts.cs` | embedded Arabic UI font |
| `Services/IUserFiles.cs`, `AvaloniaUserFiles.cs` | open and save dialogs, clipboard (an interface, so tests can fake it) |
| `Services/IExternalLinks.cs` | opening official links in the browser (`AvaloniaExternalLinks`, `NoExternalLinks` for tests) |
| `ViewModels/MainWindowViewModel.cs` | the core: steps, inputs, sections, compliance, generate, preview, export, save, open, autosave, language, theme, evaluation |
| `ViewModels/MainWindowViewModel.Grounds.cs` | grounds analysis, ticks, article search, chips, `DraftingInputs`, `SelectedLegalTexts`, official links |
| `ViewModels/MainWindowViewModel.Assistant.cs` | section assistant, redraft, model status |
| `ViewModels/GroundsViewModels.cs` | `ArticleChoiceViewModel`, `SelectedArticleViewModel`, `InsertChipViewModel` |
| `ViewModels/FieldViewModel.cs` | one field, its choice options, a step (`FieldGroupViewModel`, `StepStatus`) |
| `ViewModels/SectionViewModel.cs` | one editor section: undo history, override, redraft, review hints, `IsWriting` |
| `ViewModels/ModelActivityViewModel.cs` | live model activity: steps, timer, waiting state, live text (§5.9) |
| `ViewModels/FindingViewModel.cs` | one compliance finding as displayed |
| `ViewModels/ProviderOption.cs` | a selectable engine + `ModelStatus`, optional warm-up |
| `ViewModels/Converters.cs` | small XAML converters |
| `Views/MainWindow.axaml(.cs)` | the whole UI; the 30-second timer for autosave and the model probe |
| `Resources/Strings.resx`, `Strings.en.resx` | UI strings (Arabic default, English) |
| `appsettings.json` | shipped defaults |

### `src/DzLegal.Harness`
| File | Contents |
|---|---|
| `Program.cs` | argument dispatch: default (corpus), `draft`, `grounds`, `law import` |
| `CorpusCase.cs`, `CorpusEvaluator.cs` | Form Engine corpus (`tests/corpus/…`, `extends` + `setSections`) and per-element accuracy, precision and recall |
| `DraftBench.cs` | drafting bench over `tests/drafting-cases/*.json` |
| `GroundsEvaluator.cs` | grounds bench over `tests/grounds-cases/cases.json`; writes `<model>/<case>.md` traces and `summary.md` |
| `LawImport.cs` | `law import`: download (official hosts only) or file → text (`pdftotext`, HTML) → `LawTextParser` → `codes/<id>.json` + report + diff |

---

## 7. Guardrails on model output

The model only ever writes the `grounds` and `requests` sections, and the assistant's proposals. Each of those texts goes through, in order:

| Step | Where | Effect |
|---|---|---|
| Clean | `DraftingService.CleanModelOutput` | Removes `<think>` blocks and any non-Arabic lines before the first Arabic line ("here is…" preambles), code fences and `**`, and a first line that repeats the section title. |
| Example copy | `NarrativeChecks.RemoveCopiedExample` | Deletes lines copied from the prompt's worked example. |
| Corrections | `DraftingService.ApplyCorrections` | Exact find/replace from `definition.json`. |
| Checks | `NarrativeChecks.Check` | Produces `NarrativeIssue`s, listed below. |

`NarrativeChecks.Check` issue kinds:

| Kind | Meaning | Severity |
|---|---|---|
| `ForeignScript`, `NotArabic` | Wrong script, or mostly not Arabic (CPCA art. 8) | **hard**: retry, then fall back to the notes |
| `UngroundedTerm` | A `groundedTerms` qualification that is not in the inputs | **invented law**: one retry, then a hint |
| `UncitedArticle` | An article number that is neither in the notes nor ticked | invented law |
| `MisquotedArticle` | A «quotation» attributed to a ticked article that is not in its official text | invented law |
| `UnknownNumber` | A number that appears nowhere in the inputs | hint |
| `MissingNumber` | A number from the notes was dropped | hint |
| `UnexpectedLatinWord` | A Latin word that is not in the inputs | hint |

The hints are shown under the section (`UpdateReviewHints` → `DescribeIssue`) and recomputed on every edit.

For the grounds analysis, the guardrails are in the parsers:
- **Stage 1:** only catalog claim ids are accepted, and article numbers are stripped from the search terms.
- **Stage 3:** only candidate keys are accepted, with at most 6 articles. Reasons are cut to 30 words, and a reason that names another article is dropped.

---

## 8. Offline and security boundaries

Details in [OFFLINE.md](OFFLINE.md). In code:

| Boundary | Enforced in |
|---|---|
| The model endpoint must be loopback | `LoopbackGuard.EnsureLoopback` (in the `OllamaModelProvider` constructor); the HTTP handler uses `UseProxy = false` |
| No download at runtime | The only downloader is `Harness/LawImport.cs`, a maintainer tool, restricted to `OfficialLinks.Hosts` |
| Links open only on click, only to official hosts | `OpenOfficialLinkAsync` re-checks `OfficialLinks.IsOfficial` before calling `IExternalLinks.OpenAsync` |
| No telemetry | Nothing sends any; `DOTNET_CLI_TELEMETRY_OPTOUT` is set in CI |
| Article text only from official imports | `LegalText` comes from `LawLibrary` only; the model never writes article text into the library |

**Rule for new code: case data never leaves the machine.** Do not add an HTTP client, a crash reporter, an update check or an online font.

---

## 9. Persistence

| What | Where | Format |
|---|---|---|
| Draft | user-chosen `*.dzdraft.json` | `DraftDocument` serialized with `DefinitionJson.Options`: `documentTypeId`, `definitionVersion`, `inputs` (field id → string), `sections` (id, text, overridden, draftingNotes), `selectedArticles`, `groundsAnalysis`, `generatedBy` |
| Autosave | `%LOCALAPPDATA%\DzLegalAssistant\drafts\last-session.dzdraft.json` | same format |
| Evaluation | `%LOCALAPPDATA%\DzLegalAssistant\evaluations\*.json` | `EvaluationRecord` |
| Settings | next to the exe: `appsettings.json` (+ `appsettings.local.json`) | JSON with comments |

Fields added after the first release (`selectedArticles`, `groundsAnalysis`) are optional, so older drafts still load. Keep any new field nullable or defaulted. Unknown properties are ignored when reading, and nulls are not written. No dedicated test loads an old file yet; add one if you change the format.

---

## 10. Localization and UI

- Strings live in `Resources/Strings.resx` (Arabic, default) and `Strings.en.resx`. XAML uses `{Binding L[Key]}`, and code uses `L["Key"]` or `L.Format("Key", args)`. A missing key shows as `[Key]`.
- `SwitchLanguage` changes `Loc`, then calls `Relocalize()` on every child view model and re-runs `Recheck`.
- The flow direction comes from `Loc.IsRightToLeft`, bound on the window.
- The data carries its own bilingual labels (`LocalizedText`). The legal output is always Arabic, whatever the UI language.
- `MainWindow.axaml`:
  - compiled bindings (`x:DataType`);
  - reusable templates in `Window.Resources`: `ArticleChoiceTemplate`, `InsertChipTemplate`;
  - the step panels for grounds, requests and documents shown with `IsGroundsStep`, `IsRequestsStep` and `IsDocumentsStep`.
- Bind `IsVisible` to a `bool` such as `HasX`, never to an `int` count.

---

## 11. Tests, harness and CI

| Test project | Covers |
|---|---|
| `DzLegal.FormEngine.Tests` | rules, `DefinitionLoader` validation, the corpus harness (100% required), the draft bench plumbing |
| `DzLegal.Drafting.Tests` | `DraftingService` (prompts rendered from the real files, retry and fallback), `NarrativeChecks`, `SectionReviser`, `GroundsAnalyzer` parsers and pipeline (scripted fake provider), templates, `LegalText`, the Ollama provider (fake HTTP handler, loopback guard) |
| `DzLegal.Law.Tests` | article numbers, the parser, diff, BM25, search, loaders and validation (`Fixtures.cs`: **clearly fictitious** codes) |
| `DzLegal.Documents.Tests` | PDF and Word rendering |
| `DzLegal.Storage.Tests` | draft round-trip (Arabic kept readable), synchronous autosave save, evaluation records |
| `DzLegal.App.Tests` | headless Avalonia: view-model behaviour (`MainWindowViewModelTests`, `GroundsTests`), rendering and screenshots (`RenderingTests`), settings |

**Test data:**
- **Form Engine corpus:** `tests/corpus/requete-introductive/`, with `_base-*.json` plus `*.case.json` using `extends`. Each case states the expected status per element.
- **Drafting bench:** `tests/drafting-cases/*.json`.
- **Grounds bench:** `tests/grounds-cases/cases.json`, 44 cases. Each has `expect` (a claim id, or empty for out-of-catalog facts) and `expectArticles` (`code:number`).

**CI** (`.github/workflows/ci.yml`): build (warnings are errors) → test → corpus harness → `grounds --models stub`, which only checks the pipeline runs. Model quality is not gated: run the benches locally with Ollama.

---

## 12. Recipes

**Add an input field**
1. Add it to `definition.json` → `fields`, with `group`, `kind`, `label`, `hint` and `visibleWhen` as needed.
2. Use it in a template or prompt as `{{ group.name }}`.
3. If it is required, add or extend an element in `elements` and add corpus cases.
4. Bump `version`, then run `dotnet test` and the harness.

**Add or change a CPCA rule**
1. Edit `elements`; the check kinds are in §3.
2. Add a passing and a failing case under `tests/corpus/requete-introductive/`.
3. Run `dotnet run --project src/DzLegal.Harness`; it must stay at 100%.
4. Keep `verified: false` until a lawyer confirms the rule.

**Change a drafting prompt**
1. Edit the `.ar.txt` file. Keep the `[[مثال]]` markers and the `{{ if legal_texts }}` block.
2. Run `dotnet test --filter BuildRequest`, then `draft --models <m>` locally, and compare the `bench/` output.

**Change a grounds-analysis prompt**
1. Edit `data/law/prompts/*.ar.txt`. Remember: `{{ x }}` is plain replacement, so no `if` or loops.
2. If the answer format changes, update `ParseReading` or `ParseSelection` and `GroundsAnalyzerTests`.
3. Run `grounds --models gemma3:4b --out bench-grounds` and compare `summary.md` with the table in `LAW-LIBRARY.md`.

**Add a claim type**
1. Add it to `data/law/claims/<area>.json` with:
   - `id`, `label` (ar/en);
   - a one-line Arabic `description` that says how it differs from its neighbours;
   - `articles` (code, number, role, why);
   - `prerequisites`, `suggestedRequests`, `requiredDocuments` and `factsToState`.
2. Add 1–2 cases to `tests/grounds-cases/cases.json`.
3. `dotnet test` runs the loader validation.

**Import an official code** — see [LAW-LIBRARY.md](LAW-LIBRARY.md#importing-a-text). Afterwards the loader checks every catalog article against the imported text.

**Change the model**
- On one machine: `appsettings.local.json` → `{ "ollama": { "model": "qwen2.5:7b-instruct" } }`.
- For everyone: `appsettings.json`.
- Measure both benches before changing the default.

**Add a model provider**
1. Create a project that implements `IModelProvider`. Keep it loopback-only.
2. Register a `ProviderOption` in `Composition.CreateProviders`, add an `appsettings` section in `AppSettings.cs`, and add a resx display name.

**Add a document type**
1. Create `data/document-types/<id>/` with its own `definition.json`, templates and prompts. `DefinitionLoader.LoadAll` already supports several.
2. In `Composition.CreateMainViewModel`, which loads only `requete-introductive` today, add a type selector.
3. Add corpus cases under `tests/corpus/<id>/`.
4. The view models are generic over the definition, except the constants in `MainWindowViewModel.Grounds.cs`:
   - the step ids `facts`, `grounds`, `requests`, `documents`;
   - the field ids `notes.facts`, `notes.grounds`, `notes.requests`, `documents.list`, `claim.subject`, `court.section`.

---

## 13. Gotchas

- **Scriban treats an empty string as true.** That is why `LegalText.Block` returns `null` when there is nothing to show; use the same pattern for new optional blocks.
- **Never block on async code on the UI thread** (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`). This froze the app during autosave. Use synchronous I/O there, or `await`.
- **The data is copied at build time.** If an edit to `data/` seems ignored, rebuild.
- **Warnings are errors**, including the CA analyzers. Fix them; do not suppress them globally.
- **Every resx key goes in both files.** A key missing from `Strings.en.resx` shows the Arabic text in the English UI.
- **Bump `definition.json` `version`** when rules change. It is saved in each draft and evaluation, so the results can be traced to their rules.
- **Arabic comparison must go through `ArabicScript.Normalize`.** It maps ى→ي and ة→ه (among others), so a test expecting «ة» after normalization will fail.
- **Article numbers must go through `ArticleNumbers.Canonical`** before lookup («124 مكرر», «73-4», «الأولى»).
- **The two prompt mechanisms differ:** Scriban in `document-types/`, plain replacement in `law/prompts/`.
- **Test data is fictitious.** Never put real client data in tests, the corpus or the benches. The law fixtures are deliberately fake codes.
- **RTL SVG diagrams:** `text-anchor` start and end are mirrored under RTL. Use `direction="ltr"` and position explicitly.
- **Shell tip:** in scripts, avoid `pkill -f` patterns that match your own shell, and avoid a bare `cat` that waits for stdin.
