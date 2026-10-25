# Rules for AI agents working in this repository

DZ Legal Assistant is an **offline Windows desktop app** for Algerian lawyers. It drafts the عريضة افتتاح دعوى (statement of claim) in Arabic with a local AI model. A public web demo reuses the same screen logic.

Read this file completely before changing anything. Your task brief (a PDF or prompt) adds the details of your task and may narrow these rules, never relax them.

## Hard rules

### Branch and git
- **Work only on the branch named in your brief.**
  - Never commit to `claude/stoic-fermat-mi1gko` (the default branch) or to anyone else's branch.
  - Never force-push or rebase a shared branch.
  - To bring in new work from the default branch, merge it.
- **Small commits with clear messages.** After every commit:
  - `dotnet build -c Release` → **0 warnings, 0 errors**. `TreatWarningsAsErrors` is on, with analysis level `latest-recommended`. Fix the cause; never suppress a warning to get past it.
  - `dotnet test -c Release` → all green.
- **Never delete, skip or weaken a test.** If a layout change legitimately changes what a rendering test sees, update that test so it still checks the same behaviour, and say so in your report.

### Offline and privacy
- **Nothing may load from the internet at run time:**
  - no web fonts, CDNs, remote images or analytics;
  - no automatic downloads;
  - no telemetry.
- **The AI model is reached only on loopback** (`127.0.0.1`).
- **Every bundled font, icon and image must be licensed for redistribution.** Add the licence file next to it.
- **Case data stays on the computer.** Tests and samples use **fictitious data only**: `tests/drafting-cases*`, `src/DzLegal.Web/Samples`.
- **Never put API keys, tokens or personal data in code, config, logs, screenshots or commits.**

### Areas you must not change unless your brief explicitly says so
- **The legal engine:**
  - `src/DzLegal.Drafting`
  - `src/DzLegal.Law`
  - `src/DzLegal.Domain`
  - `src/DzLegal.FormEngine`
  - `src/DzLegal.Documents`
  - `src/DzLegal.ModelProviders.*`
  - `src/DzLegal.Harness`
- **Legal data and prompts:**
  - `data/` (the form definition, law texts and claim catalogue)
  - `prompts/`
- **The law corpus being built on `codex/law-corpus`:**
  - `data/law/corpus/`
  - `tools/law-corpus/`
  - `src/DzLegal.Law/Corpus/`
- **Behaviour that keeps the lawyer safe.** Keep it exactly:
  - export blockers;
  - AI-authorship marks;
  - the review-before-export rule;
  - ask-before-replacing unsaved work;
  - the recovery copy;
  - over-long text kept whole.

## Project map

| Folder | What it is |
|---|---|
| `src/DzLegal.App` | The Avalonia 12.1.3 desktop app (.NET 10): `Views/MainWindow.axaml`, the theme in `App.axaml` and `Styles/Workbench.axaml`, `Composition.cs` (wiring), `AppSettings.cs` (shipped settings), `Services/` (file pickers, power status, links). |
| `src/DzLegal.Presentation` | The screen logic shared by desktop and web: `ViewModels/MainWindowViewModel*.cs` (CommunityToolkit.Mvvm, `[ObservableProperty]`, `[RelayCommand]`), `FieldViewModel`, `Services/` interfaces, `Resources/Strings.resx` (Arabic) and `Strings.en.resx` (English). |
| `src/DzLegal.Storage` | Local files: `AppPaths` (the per-user folder `%LOCALAPPDATA%\DzLegalAssistant`), `DraftFile` (the `.dzdraft.json` case file), evaluations. |
| `src/DzLegal.Web` | The Blazor Server web demo. It reuses `MainWindowViewModel` and has its own sample cases in `Samples/`. It must keep building and passing `DzLegal.Web.Tests`. |
| `data/document-types/requete-introductive/definition.json` | The form: fields (`court.*`, `plaintiff.*`, `defendant.*`, `claim.*`, `notes.*`, `documents.list`, `lawyer.*`, `document.place`, `document.date`), the document sections and the formal checks. Read it; do not change it. |
| `tests/DzLegal.App.Tests` | Headless Avalonia tests. `RenderingTests.cs` and `WorkbenchTests.cs` render real windows. With `DZLEGAL_SCREENSHOTS=<folder outside the repo>`, they save screenshots. |

## Conventions

- **Bindings and strings.**
  - Avalonia uses **compiled bindings**: `x:DataType` on every view.
  - Every visible string goes in **both** `Strings.resx` and `Strings.en.resx`, read through `L["Key"]` / `L.Format("Key", …)`. No hard-coded text in XAML or C#.
- **Arabic is the default language,** right to left.
  - The document is always Arabic and right to left, even when the interface is English.
  - Check every screen in **Arabic and English, light and dark, at 1366×768 and 1920×1080**.
- **Comments and naming:** match the surrounding code. Keep comment density modest, and explain *why*, not *what*.
- **New per-user files** go under `AppPaths.Default.Root`, never beside the executable (it may be installed read-only).
- **Readable files.** A missing or broken settings or profile file must never crash the app: fall back to defaults.

## Before you report a task done

1. The build has 0 warnings, and every test passes.
2. You ran the app (`dotnet run --project src/DzLegal.App`, engine «دون ذكاء اصطناعي» if Ollama is absent) and tried the feature by hand.
3. You captured the screenshots your brief asks for, and looked at each one yourself.
4. Your report lists:
   - what you built;
   - every file you changed outside the areas your brief names;
   - the tests you added or updated;
   - anything you were unsure about.
