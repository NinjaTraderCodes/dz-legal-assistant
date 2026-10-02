# dz-legal-assistant

An **offline desktop assistant that helps Algerian lawyers draft legal documents** in formal Arabic, and **checks that every element the law requires is present** before filing.

The lawyer fills in the case details, the application produces a structured first draft, and a deterministic **Form Engine** reports which mandatory elements (if any) are missing. Each finding cites the article it comes from. The lawyer reviews, edits and signs. The application **never** files anything and never presents its output as final.

> **Target 1 (this milestone):** one document type, the *requête introductive d'instance* (عريضة افتتاح الدعوى), checked against CPCA (Loi 08-09) arts. 8, 14, 15 and 17.
>
> ⚠️ **The legal rules are not yet verified by a lawyer.** Every rule is marked `verified: false` in the data and flagged in the UI. See [docs/LEGAL-VERIFICATION.md](docs/LEGAL-VERIFICATION.md).

![Main window in Arabic](docs/images/main-window-ar.png)

## Principles

| Principle | How it is enforced |
|---|---|
| **Offline, no data leaves the machine** | No cloud APIs, no telemetry. The only model endpoint allowed is a loopback address (`127.0.0.1`, `::1`, `localhost`), checked by `LoopbackGuard`, with the system proxy bypassed. See [docs/OFFLINE.md](docs/OFFLINE.md). |
| **Assistive, never autonomous** | A permanent "draft, review required" banner. The app only produces text for the lawyer to edit and sign. |
| **Grounded, not free-generating** | Identification, court, **facts**, dates and the signature block come from templates, deterministically, with grammatical gender taken from the form. Each facts line becomes a «حيث إن / وحيث إن» paragraph exactly as the lawyer wrote it. Small local models swapped lender and borrower when rewriting facts, so they no longer touch them. The model only rewrites the notes for the legal grounds and the requests. If a section has no notes, the model is not called. |
| **Model output is checked** | Deterministic checks remove paragraphs copied from the prompt's worked example. They also flag foreign-script characters, stray Latin words, invented numbers and dropped numbers. Unusable answers are retried once, and otherwise replaced by the lawyer's notes. |
| **Verifiable** | The Form Engine is pure and deterministic. A 24-case corpus measures its accuracy per element, and CI requires 100%. |
| **Legal rules are data** | Elements, articles, templates and prompts live in `data/`. Changing a rule means editing JSON, not code. |

## Quick start

Prerequisite: [.NET 10 SDK](https://dotnet.microsoft.com/download) (the version is pinned in `global.json`).

```bash
dotnet build                                   # warnings are errors
dotnet test                                    # 163 tests: engine, drafting, providers, storage, UI (headless)
dotnet run --project src/DzLegal.Harness       # Form Engine accuracy report (exit 1 on any mismatch)
dotnet run --project src/DzLegal.App           # the desktop app
```

The app starts in Arabic and uses the local model by default. **With no model installed,** choose **"وضع القوالب (دون ذكاء اصطناعي)" / "Template mode (no AI)"** in the toolbar. The draft is then built from the templates plus your own notes, and everything else works the same.

### Using the local model (optional, recommended)

1. Install [Ollama](https://ollama.com). It runs as a local process on `127.0.0.1:11434`.
2. `ollama pull qwen2.5:7b-instruct` (about 4.7 GB, one-time download). After that, no network is needed.
3. Start the app and keep the default engine, "النموذج المحلي" / "Local model".

Recommended hardware: 16 GB RAM for 7B models (CPU-only works, at about a minute or more per section), or a GPU with 8 GB or more of VRAM. **On 8 GB RAM without a GPU, use `gemma3:4b`.** It was the best of the three models benchmarked on such a machine (about 2–4 minutes per requête, no fallbacks). `qwen2.5:3b-instruct` is weaker. `qwen3:4b` is **not supported** there: it writes its English reasoning into the document and takes over 10 minutes per requête. To use another model, edit `appsettings.json` next to the executable:

```jsonc
{
  "defaultProvider": "ollama",       // or "stub" for template mode
  "language": "ar",                  // or "en"
  "ollama": { "endpoint": "http://127.0.0.1:11434", "model": "qwen2.5:7b-instruct", "timeoutMinutes": 10 }
}
```

A non-loopback `endpoint` is refused. Further optional keys: `temperature` (0.2), `maxTokens` (1000), `repeatPenalty` (1.1), and `"think": false` for reasoning models such as `qwen3`.

### Comparing local models (drafting bench)

```bash
ollama pull gemma3:4b && ollama pull qwen2.5:3b-instruct     # once, online
dotnet run --project src/DzLegal.Harness -- draft --models gemma3:4b,qwen2.5:3b-instruct --out bench
```

This runs the fictitious cases in `tests/drafting-cases/` through each model. For every model and case it writes the full document, the time per section, any retries or fallbacks, and the automatic issues. It also writes `bench/summary.md`. On a CPU-only machine, expect several minutes per case. Automatic checks cannot detect swapped roles or invented reasoning, so read the outputs too; each file ends with a checklist. Use `--models stub` to try it without any model.

### Packaging for a lawyer's Windows PC

```bash
dotnet publish src/DzLegal.App -c Release -r win-x64 --self-contained -o out/win-x64
```

The output folder has everything it needs: runtime, `data/` rules, fonts and `appsettings.json`. Copy it to the machine and run `DzLegalAssistant.exe`.

## Demo walkthrough (for reviewers)

1. `dotnet run --project src/DzLegal.App` and click **English** (top right) if you don't read Arabic. The UI switches to English and left-to-right, while the document stays Arabic and right-to-left.
2. Choose **Template mode (no AI)** as the drafting engine, unless Ollama is installed.
3. The **Formal check** panel (right) already lists the missing mandatory elements: court, parties, facts…
4. Fill in the **Case details**: court, plaintiff, defendant, subject, notes on facts/grounds/requests, lawyer name. The **Draft** panel updates live and findings turn to *Present*.
5. Click **Draft narrative sections**. The facts, grounds and requests are written, and the summary shows *No mandatory element is missing*.
6. Clear the text of *Legal grounds* in the draft. The report immediately flags **CPCA art. 15-5**.
7. Tick **Claim concerns registered real property**. A new mandatory element (**CPCA art. 17**, land-registry publication) appears.
8. **Save** stores a local `.dzdraft.json`, and **Copy text** puts the whole document on the clipboard. The *Evaluate this draft* panel records a local verdict (used as is / minor edits / major rewrite) for later quality work.

![English UI](docs/images/main-window-en.png)

## Repository layout

```
src/
  DzLegal.Domain/                 models + validating JSON loader (no dependencies)
  DzLegal.FormEngine/             deterministic compliance checker (no AI, no network)
  DzLegal.Drafting/               template rendering, grounded prompts, IModelProvider
  DzLegal.ModelProviders.Stub/    template mode (no model)
  DzLegal.ModelProviders.Ollama/  local model over loopback only
  DzLegal.Storage/                local JSON drafts + evaluation records
  DzLegal.Harness/                Form Engine accuracy report over the corpus
  DzLegal.App/                    Avalonia desktop shell (Arabic/English)
data/document-types/requete-introductive/
  definition.json                 fields, sections, required elements (+ articles)
  templates/*.scriban             deterministic sections
  prompts/*.ar.txt                grounded prompts for narrative sections
tests/
  corpus/requete-introductive/    documents with known-correct outcomes
  DzLegal.*.Tests/                unit, integration and headless UI tests
docs/                             architecture, offline guarantees, rule updates, legal verification
```

## Further documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): modules, data flow, design decisions.
- [docs/UPDATING-LEGAL-RULES.md](docs/UPDATING-LEGAL-RULES.md): how to change rules, templates and prompts, add corpus cases, add a document type.
- [docs/OFFLINE.md](docs/OFFLINE.md): what "offline" means here and how to verify it.
- [docs/LEGAL-VERIFICATION.md](docs/LEGAL-VERIFICATION.md): the checklist for the practicing lawyer.
