# The public web demo

**Live:** https://dz-legal-assistant-ai.onrender.com

The product is an **offline desktop app**: the lawyer's case data never leaves the computer ([OFFLINE.md](OFFLINE.md)).
The web demo exists so that anyone, a judge of a competition for example, can **use the real product from a link**
without installing anything. It runs the same engine, the same law library and the same screen logic. The one
difference is where the AI model runs:

| | Desktop app | Web demo |
|---|---|---|
| Form, formal check, legal points, templates, PDF/Word | on the laptop | on the demo server |
| Model for the analysis and the drafting | Gemma 3 4B, **local** (Ollama); about 2 minutes per draft on a laptop CPU | Nemotron 3 Ultra **hosted** on OpenRouter first (much larger than the desktop's model, said on the page), then Gemma 4 26B on Google AI Studio, then Groq |
| Case data | never leaves the computer | sent to the hosted model: **fictitious cases only** |
| Screen logic (view models, strings) | `DzLegal.Presentation` | `DzLegal.Presentation` (the same code) |

The app bar says "Demo · fictitious cases only", and the guide explains that the product runs the model offline.

## The website at the same address

The public website (`site/`, Astro, static HTML) is served by the same app, so one address carries both: the site at
`/ar/` and `/en/` (Arabic first, the product's language), with its guide, download, law library, privacy and about pages
(`/<lang>/try/`, `/download/`, `/law/`, `/privacy/`, `/about/`), and the demo at `/app`. The guide ("Test it in 5 minutes") and
the demo's own guide describe the same five steps with the same button names: the website reads them from
`Strings.resx`, `WebText.cs` and the sample files at build time. Nothing is loaded from another origin,
the page's Content Security Policy stays as strict as the demo's (no inline scripts), and the fonts and icons are the
desktop's own, bundled with their licences.

- **Language:** a remembered choice (`/lang/{code}` sets the first-party cookie `dzlegal.lang` and comes back), then
  `?lang=`, then the browser (an Arabic browser gets Arabic, every other one English). `/app` follows the same rule, so
  the page is already in the right language and direction before the circuit connects.
- **Theme:** dark unless the visitor chose light (cookie `dzlegal.theme`, shared by the site and the demo). The
  browser's own dark preference is not consulted.
- **Design tokens:** `site/scripts/tokens.mjs` generates `site/src/styles/tokens.css` and
  `src/DzLegal.Web/wwwroot/tokens.css` from the desktop theme (`App.axaml`, `Styles/Workbench.axaml`); the build fails
  when they are stale, so the website never drifts from the product.
- **`/`:** the home page in the visitor's language (the `dzlegal.lang` cookie, then the browser's Accept-Language,
  then English), served at the address itself, no redirect; the demo lives at `/app`. The switch is `Site:ServeRoot`
  in `appsettings.json`: set it to `false` and the demo answers at `/` again (rewritten internally to `/app`), which
  is also what happens when the site folder is missing from the image. Unknown paths get the site's own not-found
  page.
- **Compression:** the site leaves the server compressed (Brotli, else gzip: the Arabic home is 22 KB instead of
  104 KB), as the host already does in front of it, so a local Lighthouse run sees what a visitor gets. Only the
  site is compressed; the demo page carries the antiforgery token, which compressing would expose (BREACH), and is
  left as it is, like the circuit. Fonts and images are compressed formats already.
- **Build:** the `Dockerfile` builds the site in a Node stage (`npm ci --omit=dev && npm run build`, about two minutes)
  and copies `site/dist` next to the app; the app finds it at `<base>/site` (setting `Site:Root`). Without the folder
  the app behaves exactly as before.
- **Local run with the site:** `cd site && npm ci && npm run build`, then
  `dotnet run --project src/DzLegal.Web --launch-profile http+site` (sets `Site__Root=../../site/dist`).
- **Checks, run locally** (GitHub Actions are off): `npm test` (Playwright: the routes, the switch, the headers, the
  demo shell; axe on every page; `tests/guide.spec.ts` follows the five-step guide in the demo against an instance
  whose AI is the local stand-in `tests/mock-model.mjs`, started as its header says, with `GUIDE_BASE_URL` set), `npm run shots -- --out ../docs/website/<phase>`,
  `npm run lighthouse -- --out ../docs/website/<phase>`, `npm run budget` (what each page loads and the Lighthouse scores
  against `site/budget.json`) and `npm run links` (a private window opens every page and follows every link).
  Each phase's reports live in `docs/website/`.

## How it is built

```
Browser ──SignalR──► DzLegal.Web (ASP.NET Core, Blazor Server, one instance)
                      ├─ one MainWindowViewModel per visitor (DzLegal.Presentation, shared with the desktop)
                      ├─ FormEngine · Law (official texts) · Drafting · Documents (QuestPDF) · Storage
                      └─ every model call goes through ProtectedModelProvider:
                           per-visitor and per-network hourly limits (charged per call)
                           → the shared queue (ModelGate, 2 calls at a time)
                           → the engine chain (FallbackModelProvider, one set of engines for everyone):
                                Nemotron 3 Ultra on OpenRouter    (when OPENROUTER_API_KEY is set; reasoning switched off)
                                → Gemma 4 on Google AI Studio     (when GOOGLE_API_KEY is set; models Google still serves, 26B first)
                                → Groq                            (optional)
```

> Nemotron 3 Ultra answers first because, on 31 October 2026, it matched or beat Gemma 4 26B on every test set, faster and with no failed call, while Google returned "internal error" on up to a third of its calls. (Before that, Gemma 4 26B led the chain: in the October audit OpenRouter's Gemma 3 4B left every argued paragraph of the live sample blank.) The page says the demo runs a larger hosted model than the desktop, lists the services the form may be sent to (Google, OpenRouter, Groq), and shows under the draft which engine answered. Each switch down the chain is logged: `Engine … skipped (…); the next engine in the chain answers instead`.
>
> The visitor's case is kept in the browser as a recovery copy for at most a day; «امسح نسخة المتصفح» deletes it and stops keeping one.

- **Failures never end a visitor's session.**
  - A dropped connection, an unreadable event, or an answer silent for 45 seconds becomes a model error.
  - A service that is busy, refuses the key or has no credit left (401/402/403/404/429/5xx, or an error inside the stream) hands the request to the next engine, as long as it has not written anything yet.
  - The visitor sees a plain message; the service's raw error goes to the server log only.
  - AI buttons run one task at a time per visitor. Closing the tab cancels running work.
- **The case is kept in the visitor's browser** (`localStorage`) every few seconds. After a server restart (the free plan sleeps after 15 minutes) or a closed tab, the page offers to restore it. Nothing is stored on the server.
- **The official law texts** (`data/law/codes`) and the PDFs they come from (`docs/official-law-texts`, served at `/law-sources/…`) ship with the image. Every article card links to its page.
- **Security headers:** a Content Security Policy with no inline scripts and no framing, `nosniff`, and no referrer.
- **`/healthz`** says only whether the server is up and whether AI is configured. The engines in use are logged at start-up.

## Run it locally

```bash
# Without any key: everything works except the AI steps, which use fill-in templates.
dotnet run --project src/DzLegal.Web            # http://localhost:5080

# With the hosted model:
OPENROUTER_API_KEY=... dotnet run --project src/DzLegal.Web
```

PowerShell: `$env:OPENROUTER_API_KEY = "..."` in the window that runs `dotnet run`.

To try the AI flow without any key, point the Groq slot at any OpenAI-compatible server, for example a local mock:
`GROQ_API_KEY=x Engines__GroqEndpoint=http://127.0.0.1:5090/v1/ Engines__GroqModel=mock dotnet run --project src/DzLegal.Web`.

Settings (`src/DzLegal.Web/appsettings.json`, section `Engines`; any of them can be overridden by an environment variable such as `Engines__MaxConcurrentCalls=3`):

| Setting | Default | Meaning |
|---|---|---|
| `Order` | openrouter, google, groq | which service is tried first; a service without a key is skipped |
| `OpenRouterModel` | `nvidia/nemotron-3-ultra-550b-a55b` | the online model chosen on 31 Oct 2026; reasoning is switched off for Nemotron ids |
| `GoogleModels` | Gemma 4 26B A4B, Gemma 4 31B, then Gemma 3 | tried in order; models Google no longer serves are dropped at start-up |
| `MaxConcurrentCalls` | 2 | model calls running at once, all visitors together |
| `CallsPerSessionPerHour` | 60 | model calls per browser session per hour (a full run is about six) |
| `CallsPerAddressPerHour` | 150 | model calls per hour for all sessions from one network address |
| `MaxTokens`, `Temperature` | 1000, 0.2 | same as the desktop defaults |

**Keys come only from environment variables** (`OPENROUTER_API_KEY`, `GOOGLE_API_KEY`, `GROQ_API_KEY`), set in the host's secret settings. They never go into `appsettings.json`, a commit, a chat or a screenshot.

## Deploy it (Render)

The repository root has a `Dockerfile` (ready-to-run build, workstation GC for a 512 MB instance) and a `render.yaml` blueprint.

1. On [render.com](https://render.com): *New → Web Service*, connect the repository, language **Docker**, region **Frankfurt**.
2. Environment variables: `PORT` = `7860`, `OPENROUTER_API_KEY`, optionally `GOOGLE_API_KEY`. Health check path: `/healthz`.
3. Every push to the deployed branch redeploys.

The free plan sleeps after 15 minutes without visitors and takes about a minute to wake. **For judging week, switch the
service to the smallest paid plan** (always on, five times the CPU), then back.

**OpenRouter:**
- Add a few dollars of credit and set a credit limit on the key.
- Use the paid `nvidia/nemotron-3-ultra-550b-a55b` rather than the `:free` variant. Free variants are capped at about 50 requests a day for accounts that never bought $10 of credit.
- When the credit runs out, the demo falls back to Google's free Gemma 4.

Other hosts (Google Cloud Run: `deploy/cloud-run.sh` and `.ps1`; a paid Hugging Face Space: the `deploy-web-demo` workflow) run the same image.

## No wake-up wait: the options (checked 20 Oct 2026)

**Vercel cannot run this demo as it is built.** The demo is Blazor Server: one long-running .NET process keeps each
visitor's screen state in memory and talks to the browser over a WebSocket (SignalR). Vercel runs static files and
short functions (Node.js, Python), not a .NET process or a Docker image, and a function, WebSocket or not, ends after
at most 300 s on the free plan, which would cut every visitor's session. Moving to Vercel means rewriting the page as
Blazor WebAssembly with the model calls behind a separate function, and the PDF engine (QuestPDF, native code) does not
run in the browser, so preview and PDF export would need a server again. Not before 31 October.

What works with the current image, cheapest first:

| Option | Cost | Wake-up wait | Effort |
|---|---|---|---|
| Render free + an external monitor pinging `/healthz` every 5 minutes (e.g. UptimeRobot's free plan) | free | none while the pings run | 5 minutes, no code |
| Render Starter plan | about $7 a month | none | one click in the dashboard |
| Fly.io, one shared-CPU 512 MB machine left running | about $3–4 a month | none | a `fly.toml` and `fly deploy` |
| Google Cloud Run, `MIN_INSTANCES=1 ./deploy/cloud-run.sh` | a few dollars a month | none | the script exists |

Notes on the free option: Render counts any inbound HTTP request as activity, so a ping every 5 minutes keeps the 15-minute
idle timer from running out. One service running all month uses about 744 of the workspace's 750 free hours, so it only
fits if no other free Render service runs in the same workspace. Don't use a GitHub workflow for the pings: this
repository is private, and a run every 10 minutes would take more than its 2,000 free Actions minutes a month.

### Setting up the free keep-awake ping (UptimeRobot)

1. Create a free account on [uptimerobot.com](https://uptimerobot.com).
2. *New monitor* → type **HTTP(s)**, URL `https://dz-legal-assistant-ai.onrender.com/healthz`, interval **5 minutes**
   (the free plan's shortest). `/healthz` answers GET and HEAD, so either method the monitor uses works.
3. Optional: as alert contact, your e-mail; the monitor then also tells you when the demo is down.
4. Check that it works: `powershell -ExecutionPolicy Bypass -File tools\web\Watch-DemoAwake.ps1` visits the demo every
   20 minutes for 3 hours (longer than Render's 15 idle minutes, so the script alone cannot keep it awake) and reports
   whether the server's start time (`started` in `/healthz`) ever changed. A redeploy also changes it once.

## Uptime

The `web-demo-uptime` workflow checks `DEMO_URL/healthz` and the page every 30 minutes. It needs the repository variable
`DEMO_URL`, and GitHub runs scheduled workflows only on the default branch. A failed check sends GitHub's usual failure
e-mail.

## Limits, stated plainly

- **AI quality:** the desktop's Gemma 3 4B is a small model, and even the demo's larger one makes mistakes. That is why the checks matter:
  - the articles a claim type requires are always proposed;
  - the program writes each ticked article's rule from the catalog's one-line wording, one paragraph per part of the claim; the AI only writes how the facts meet it, and an application that cites or quotes an article is rejected;
  - a paragraph that keeps attributing wrong content to an article is removed, and the lawyer sees what was removed;
  - the legal points flag a wrong basis, interest on a private loan, damages without formal notice, and an article from an unrelated area of law;
  - export is refused while the notes contain text addressed to an AI, a request line not in Arabic, or a prison sentence asked in a civil case.
- **Law texts:** 39 articles are loaded, those the four sample cases rely on. A build script checks each one against the letters of its official PDF page; that is not a lawyer's review. None is yet marked "checked by a lawyer"; that review is under way. The CPCA texts are the 2008 originals; the 2022 amendment still has to be checked.
- **Speed** depends on the hosted service and on Render's small CPU. The status bar shows the queue when visitors wait.
- The demo can't open files from the visitor's disk (it has sample cases instead), and it saves nothing on the server.
