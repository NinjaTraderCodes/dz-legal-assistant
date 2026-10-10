# The public web demo

The product is an **offline desktop app**: the lawyer's case data never leaves the computer ([OFFLINE.md](OFFLINE.md)).
The web demo exists so that anyone, a judge of a competition for example, can **use the real product from a link**
without installing anything. It runs the same engine and the same screen logic. The one difference is where the
model runs:

| | Desktop app | Web demo |
|---|---|---|
| Form, 15-rule formal check, templates, PDF/Word | on the laptop | on the demo server |
| Model for the grounds analysis and the drafting | Gemma 3 4B, **local** (Ollama) | Gemma 4 (the next generation of the same open family), **hosted** (Google AI Studio) |
| Case data | never leaves the computer | sent to the hosted model: **fictitious cases only** |
| Screen logic (view models, strings) | `DzLegal.Presentation` | `DzLegal.Presentation` (the same code) |

A banner at the top of every page says this: fictitious cases only, and the real product runs the model offline.

## How it is built

```
Browser ──SignalR──► DzLegal.Web (ASP.NET Core, Blazor Server)
                      ├─ one MainWindowViewModel per visitor (DzLegal.Presentation, shared with the desktop)
                      ├─ FormEngine · Drafting · Law · Documents (QuestPDF, server-side) · Storage
                      └─ hosted engine chain, behind a queue (ModelGate):
                           Gemma 4 26B A4B → Gemma 4 31B → Gemma 3… (Google AI Studio, each with its own rate limit;
                                                                    only the models Google still serves)
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

## Where the Google AI Studio key goes

The key is a password to a paid-for quota. It goes **only** into an environment variable named `GOOGLE_API_KEY`, set on
whatever machine runs the demo. It never goes into `appsettings.json`, a commit, a chat or a screenshot.

| Where the demo runs | Where the key goes |
|---|---|
| Cloud Run (the public link) | Secret Manager. `deploy/cloud-run.sh` asks for it once, with the input hidden, and stores it there. |
| Your own computer, PowerShell | `$env:GOOGLE_API_KEY = "..."` in the window that runs `dotnet run` (gone when the window closes) |
| Your own computer, bash | `GOOGLE_API_KEY=... dotnet run --project src/DzLegal.Web` |

If a key ever leaks, delete it in [Google AI Studio](https://aistudio.google.com/apikey), create a new one and run
`UPDATE_KEY=1 ./deploy/cloud-run.sh`.

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
| `GoogleModels` | Gemma 4 26B A4B, Gemma 4 31B, then Gemma 3 27B/12B/4B | Gemma models tried in order. At startup the demo asks Google which ones the key can use and drops the rest; if none is left, any Gemma model the key can use takes over. |
| `MaxConcurrentCalls` | 2 | model calls running at once, all visitors together |
| `RunsPerSessionPerHour` | 30 | AI runs per browser session per hour |
| `MaxTokens`, `Temperature` | 1000, 0.2 | same as the desktop defaults |

Keys come **only** from environment variables: `GOOGLE_API_KEY` (required for AI), `GROQ_API_KEY` and `OPENROUTER_API_KEY` (optional fallbacks).

## Deploy it (Google Cloud Run)

The repository root has a `Dockerfile`; `deploy/cloud-run.sh` builds it on Google's servers and runs it on Cloud Run.
Nothing needs installing: everything happens in Cloud Shell, in the browser.

1. **Project and billing.** At [console.cloud.google.com](https://console.cloud.google.com), create a project (e.g.
   `dz-legal-demo`) and link a billing account (the card). Use a **new** project, not the `gen-lang-client-…` one
   Google AI Studio created for the API key: that one stays without billing, so the key keeps the free tier (Gemma
   is offered on the free tier only), and the deploy scripts refuse it. Cloud Run's free monthly allowance covers a demo of this
   size. In *Billing → Budgets & alerts*, add a small budget (e.g. $10) so any surprise sends an e-mail.
2. **Cloud Shell.** Open [shell.cloud.google.com](https://shell.cloud.google.com), then:

   ```bash
   gcloud config set project YOUR_PROJECT_ID
   git clone https://github.com/NinjaTraderCodes/dz-legal-assistant.git && cd dz-legal-assistant
   ./deploy/cloud-run.sh
   ```

   The first run asks for the Google AI Studio key (input hidden), stores it in Secret Manager, builds the image
   (about 5 minutes) and prints the demo URL, e.g. `https://dz-legal-demo-xxxxx.europe-west1.run.app`.
3. **Updates.** `git pull && ./deploy/cloud-run.sh` in Cloud Shell. The stored key is kept.

   From Windows instead (VS Code's terminal, PowerShell): the `.sh` script is for Linux and Cloud Shell, and Windows
   only offers to "select an app" for it. Install the [Google Cloud CLI](https://cloud.google.com/sdk/docs/install),
   run `gcloud auth login` and `gcloud config set project YOUR_PROJECT_ID` once, then `.\deploy\cloud-run.ps1`
   (same options: `-UpdateKey`, `-MinInstances 1`).
4. **Uptime.** In this repository's settings, add the variable `DEMO_URL` (the printed URL). The `web-demo-uptime`
   workflow then checks `DEMO_URL/healthz` and the page every 30 minutes; a failed check sends GitHub's usual failure
   e-mail.
5. **Judging week.** Cloud Run stops the instance when nobody is using it, and the first visitor then waits a few
   seconds. For 1–7 November, run `MIN_INSTANCES=1 ./deploy/cloud-run.sh` to keep one instance warm (a few dollars
   for the week), then `./deploy/cloud-run.sh` again afterwards.

Why these settings: Blazor Server keeps each visitor's session in the server's memory, over a WebSocket. The script
allows **one instance** (every visitor shares the same queue and limits, and the cost is capped), turns on **session
affinity**, and sets a one-hour request timeout so a reconnecting browser finds its session again.

Before a deadline, open the demo in a private window, logged out, and run the guided tour end to end.

**If the analysis says "gave no usable analysis".** Gemma 4 thinks before answering unless told not to, and the
thinking counts against the short answer limits of the analysis, so the answer came back empty. The demo now sends
`thinkingLevel: minimal` for Gemma 4 (and retries without it if a model refuses the setting); an answer cut off while
thinking is reported as such instead.

**If an AI step says "error 404 … not found".** Google has retired that model. The demo checks the list at startup,
so redeploy (`./deploy/cloud-run.sh`) and look at `DEMO_URL/healthz`: it lists the models in use. The server log
(Cloud Run → the service → *Logs*) has a line "Gemma models in use: …", or a warning listing every model the key can
use when no Gemma model is among them. Gemma on the Gemini API is a free-tier offer, so make the key in Google AI
Studio's own project rather than in the billed Cloud Run project.

**Alternative host.** Any host that runs a Docker image and supports WebSockets works: set `GOOGLE_API_KEY` in its
secret settings and expose port 7860. A Hugging Face Docker Space needs a paid plan; with one, the
`deploy-web-demo` workflow pushes the code there (repository secret `HF_TOKEN`, variable `HF_SPACE`).

## Limits, stated plainly

- AI quality: Google no longer serves Gemma 3 4B (the desktop default) on its API, so the demo uses Gemma 4, the next generation of the same open family. Gemma 4 26B A4B uses about 4B parameters per word, so it is fast; it usually writes better Arabic than the 4B desktop model. The status bar shows which engine answered.
- Speed depends on the hosted service's load. On Google's servers a full run usually takes seconds; on an 8 GB laptop it takes about 2 minutes.
- Free-tier limits are per Google project and per model. The queue, the fallback chain and the per-session cap keep a group of visitors within them. When every engine is busy, the visitor sees the error and can retry; template mode always works.
- The demo can't open files from the visitor's disk (sample cases instead), and it saves nothing on the server.
