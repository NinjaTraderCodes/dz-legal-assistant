# The public web demo

The product is an **offline desktop app**: the lawyer's case data never leaves the computer ([OFFLINE.md](OFFLINE.md)).
The web demo exists so that anyone, a judge of a competition for example, can **use the real product from a link**
without installing anything. It runs the same engine and the same screen logic. The one difference is where the
model runs:

| | Desktop app | Web demo |
|---|---|---|
| Form, 15-rule formal check, templates, PDF/Word | on the laptop | on the demo server |
| Model for the grounds analysis and the drafting | Gemma 3 4B, **local** (Ollama) | Gemma 3 (same open model family), **hosted** (Google AI Studio) |
| Case data | never leaves the computer | sent to the hosted model: **fictitious cases only** |
| Screen logic (view models, strings) | `DzLegal.Presentation` | `DzLegal.Presentation` (the same code) |

A banner at the top of every page says this: fictitious cases only, and the real product runs the model offline.

## How it is built

```
Browser ──SignalR──► DzLegal.Web (ASP.NET Core, Blazor Server)
                      ├─ one MainWindowViewModel per visitor (DzLegal.Presentation, shared with the desktop)
                      ├─ FormEngine · Drafting · Law · Documents (QuestPDF, server-side) · Storage
                      └─ hosted engine chain, behind a queue (ModelGate):
                           Gemma 3 4B → Gemma 3 12B → Gemma 3 27B   (Google AI Studio, each with its own rate limit)
                           → Groq / OpenRouter                       (optional, only when their keys are set)
                           → template mode, always available         (no AI)
```

- `src/DzLegal.ModelProviders.Hosted`:
  - `GoogleAiModelProvider` (Gemini API, `streamGenerateContent`; Gemma has no system role, so the rules go on top of the user turn);
  - `OpenAiCompatibleModelProvider` (Groq, OpenRouter…);
  - `FallbackModelProvider`, which hands a request to the next engine when one is busy (429/5xx/timeout) **before writing anything**. A failure in the middle of an answer is reported rather than mixed with another engine's text.

  The desktop app does not reference this project. `OfflineGuaranteeTests` fails the build if it ever does.
- `src/DzLegal.Web`:
  - `Services/WebEngine` loads the definition, the law library and the sample cases once, and creates each visitor's session.
  - `ModelGate` limits the number of model calls running at once across all visitors; the rest wait their turn. The status bar shows the queue.
  - `SessionQuota` caps the AI runs one session may start per hour (30 by default), so one visitor can't use up the shared quota.
  - `ViewModelWatcher` re-renders the page when the view model changes, which is what XAML bindings do on the desktop.
  - `BrowserFiles` turns exports into browser downloads; nothing is written on the server.
  - `Samples/*.json` holds four fictitious cases (loan, rent, undelivered goods, land under dispute). A sample holds case details only, so every step, AI included, runs live.
- `/healthz` reports the configured engines (names only, never keys) and the queue.

## Run it locally

```bash
# Without any key: everything works except the AI steps, which use template mode.
dotnet run --project src/DzLegal.Web            # http://localhost:5080

# With the real hosted model:
GOOGLE_API_KEY=... dotnet run --project src/DzLegal.Web
```

To try the AI flow without any key, point the Groq slot at any OpenAI-compatible server, for example a local mock:
`GROQ_API_KEY=x Engines__GroqEndpoint=http://127.0.0.1:5090/v1/ Engines__GroqModel=mock dotnet run --project src/DzLegal.Web`.

Settings (`src/DzLegal.Web/appsettings.json`, section `Engines`; any of them can be overridden by an environment variable such as `Engines__MaxConcurrentCalls=3`):

| Setting | Default | Meaning |
|---|---|---|
| `GoogleModels` | 4B, 12B, 27B | Gemma models tried in order |
| `MaxConcurrentCalls` | 2 | model calls running at once, all visitors together |
| `RunsPerSessionPerHour` | 30 | AI runs per browser session per hour |
| `MaxTokens`, `Temperature` | 1000, 0.2 | same as the desktop defaults |

Keys come **only** from environment variables: `GOOGLE_API_KEY` (required for AI), `GROQ_API_KEY` and `OPENROUTER_API_KEY` (optional fallbacks).

## Deploy it (Hugging Face Docker Space)

The repository root has a `Dockerfile`. It publishes `src/DzLegal.Web`, runs as user 1000 and listens on port 7860.

1. **Model key.** At [Google AI Studio](https://aistudio.google.com/apikey), create an API key.
2. **Space.** At huggingface.co, create a **Space**: SDK **Docker**, hardware **CPU basic** (free). In its settings, add the **secret** `GOOGLE_API_KEY`.
3. **Automatic deployment from GitHub.** In this repository's settings:
   - add the secret `HF_TOKEN` (a Hugging Face token with *write* access);
   - add the variable `HF_SPACE` (e.g. `your-user/dz-legal-assistant`);
   - add the variable `DEMO_URL` (e.g. `https://your-user-dz-legal-assistant.hf.space`).

   The `deploy-web-demo` workflow then pushes every change on the default branch to the Space, and the Space rebuilds the image.
4. **Uptime.** The `web-demo-uptime` workflow checks `DEMO_URL/healthz` and the page every 30 minutes. That also keeps the free Space from sleeping (free Spaces pause after 48 hours without visits). A failed check sends GitHub's usual failure e-mail.

Before a deadline, open the demo in a private window, logged out, and run the guided tour end to end.

## Limits, stated plainly

- AI quality: the hosted Gemma 3 4B is the same open model as the desktop default. The 12B and 27B models, used when 4B is busy, usually write better Arabic. The status bar shows which engine answered.
- Speed depends on the hosted service's load. On Google's servers a full run usually takes seconds; on an 8 GB laptop it takes about 2 minutes.
- Free-tier limits are per Google project and per model. The queue, the fallback chain and the per-session cap keep a group of visitors within them. When every engine is busy, the visitor sees the error and can retry; template mode always works.
- The demo can't open files from the visitor's disk (sample cases instead), and it saves nothing on the server.
