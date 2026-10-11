# Offline and privacy guarantees

Client case data is covered by professional secrecy. The application is built so that **no case data leaves the lawyer's computer**.

> This page is about the **desktop app**, the product lawyers use. The public **web demo** is a separate program (`src/DzLegal.Web`) that sends prompts to a hosted model, so it is for fictitious cases only; see [WEB-DEMO.md](WEB-DEMO.md). Its hosted providers live in `DzLegal.ModelProviders.Hosted`, which the desktop app does not reference: `OfflineGuaranteeTests` fails the build if it ever does.

> **Temporary testing build.** For the AI testing phase only, the app can be built with `-p:OnlineTesting=true` to add an online test engine (OpenRouter), for fictitious cases. That build is the only exception to this page: it says so in its window title, and it shows an amber warning while the online engine is selected. The normal build and the published package don't contain it, and `OfflineGuaranteeTests` checks this. See [ONLINE-TESTING.md](ONLINE-TESTING.md).

## What the application does on the network

| Component | Network use |
|---|---|
| Form Engine, templates, storage, UI | **None.** |
| Template mode (stub engine) | **None.** |
| Local model engine (Ollama) | HTTP to **`127.0.0.1:11434`** only. This is inter-process communication with the Ollama process on the same computer; packets never leave the machine. |
| Grounds suggestions, law library, article search | **None.** The texts and the claim catalog ship with the app in `data/law/`; nothing is scraped or searched online. |
| "Official source" links under suggested articles | **The app itself: none.** When the lawyer clicks one, the operating system opens it in the default browser. The link is fixed data, never built from case data, and only https links to joradp.dz or mjustice.dz are accepted, both when the data loads and again before opening. |
| Telemetry, analytics, crash reporting, update checks | **None exist.** |

## How it is enforced in code

- `LoopbackGuard` (`src/DzLegal.ModelProviders.Ollama/LoopbackGuard.cs`) accepts only literal loopback IPs (`127.0.0.0/8`, `::1`) and `localhost`. Any other host is refused **without a DNS lookup**, so a name like `127.0.0.1.nip.io` can't sneak through. `OllamaModelProvider` can't be constructed with any other endpoint, and there is no fallback to another endpoint.
- The Ollama HTTP client is created with `UseProxy = false`, so a system or corporate proxy never sees the request.
- No NuGet package with telemetry is referenced. The .NET SDK's own telemetry is a *developer-machine* concern; set `DOTNET_CLI_TELEMETRY_OPTOUT=1` (CI does).
- Tests cover the guard (`LoopbackGuardTests`) and a full draft → check loop that runs with no network (template mode).

## Verify it yourself

1. Disconnect the computer from all networks (or disable the adapter).
2. Start the app and use **Template mode**. Everything works.
3. With Ollama installed and the model pulled beforehand, the **Local model** engine also works, because Ollama itself runs locally.
4. Optionally watch connections, for example `netstat -ano` on Windows or `ss -tnp` on Linux. The only connection is to `127.0.0.1:11434`.

## Where data is stored

- Drafts are saved wherever the lawyer chooses; the default folder is `%LOCALAPPDATA%\DzLegalAssistant\drafts`.
- Evaluation records go to `%LOCALAPPDATA%\DzLegalAssistant\evaluations`.

On Linux and macOS the equivalent local application-data folder is used. Nothing is synchronized anywhere.
