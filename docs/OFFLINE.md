# Offline and privacy guarantees

Client case data is covered by professional secrecy. **Case files, prompts and answers stay on the lawyer's computer.** Template work needs no model or Internet. Local AI works offline after a supported model is installed.

This page describes the Windows desktop app's v0.10.0 setup. Its CPU-only Ollama engine is included; a model is acquired separately, only on the lawyer's action. There is no tray app, automatic updater, telemetry or automatic model download. The public **web demo** is a separate hosted program for fictitious cases only; see [WEB-DEMO.md](WEB-DEMO.md). Its hosted providers are not referenced by the normal desktop build.

The temporary `OnlineTesting=true` developer build is separate, visibly labelled and refused by publishing. It is not a lawyer's package; see [ONLINE-TESTING.md](ONLINE-TESTING.md).

## What the application does on the network

| Component/action | Network use |
|---|---|
| Forms, templates, storage, editing and formal checks | None. |
| Case analysis and drafting | Guarded loopback HTTP only, normally `127.0.0.1`. Case content stays on this computer. |
| Setup discovery, installed-model checks, readiness and case-free self-test | The same guarded loopback connection. Discovery never calls pull. |
| Download model / Retry, after the lawyer clicks | The app asks its local engine to pull. **The engine** contacts the registry and download servers over HTTPS. They receive the IP address, requested model, normal engine/platform and connection information, and any registry authentication required for the transfer. They receive no case files, prompts, answers or lawyer profile. |
| Install or save a local `.dzmodel` pack | Local file I/O; no model download. A local file/USB route works without Internet. |
| Law library, article search and grounds catalogue | None. The legal data ships with the app. |
| “Official source” links | A separate deliberate click opens a fixed official HTTPS link in the browser. It contains no case content. |
| Telemetry, analytics, crash uploads and app/managed-engine update checks | None. Updates arrive only in an app release the lawyer chooses to install. |

The managed engine binds an available **literal `127.0.0.1` port**, not a public/network interface. A compatible existing Ollama can instead be reused on its configured loopback endpoint (usually 11434). A foreign, old or unrecognised listener is left alone; the managed engine uses another port. The minimum for the pinned two-model inventory is **0.30.9**, with newer recognised stable versions accepted; see [the implementation record](MODEL-SETUP-IMPLEMENTATION.md). Model metadata must also identify a local GGUF, not a remote alias.

The app starts no Ollama tray application or updater. An independently installed Ollama retains its own settings, updater and unrelated activity; this app neither reconfigures nor stops it. Closing the app unloads only the model the app selected, including on a reused server. It never unloads its other models. Only an engine process launched and owned by this app is killed on exit.

## First-start choices

The non-blocking card recommends Gemma 4 E4B from **16 GiB of installed RAM**, otherwise Gemma 3 4B. Installed usable Gemma 4 is preferred, with thinking off by default; available RAM is checked separately before loading. A RAM warning does not mean another download is needed. Verified installed files can still be saved to a pack without loading the model, subject to the file operation's 512 MiB guard.

| Supported pinned CPU model | Public model bytes | Approximate size | Terms shown before acquisition |
|---|---:|---:|---|
| `gemma3:4b` | 3,350,272,403 | 3.1 GiB | Original Gemma terms embedded with the inventory |
| `gemma4:e4b` | 6,583,656,505 | 6.1 GiB | Original Apache 2.0 text embedded with the inventory |

Download shows progress, bytes, speed and estimated time left. Cancel stops this request; Retry reuses saved partial data where the engine supports it. Disk checks keep **2 GiB free** in the model-store drive, credit allocated partial files and recheck while transferring. “Not now” selects templates and remains available during setup; it does not secretly cancel an authorised transfer. The form and template workflow remain usable.

An unsupported, changed or damaged model fails its built-in identity check and is not used for case requests. Registry tags can change: a changed upstream payload is rejected rather than silently trusted. A matching offline pack or an app release with an updated built-in inventory is then needed. There is no automatic catalogue or engine update.

## USB packs: no signing key

On a computer with the supported model installed and verified, choose **Save model to a file…**. Choose a local destination or USB drive. Export hashes the expected files as it writes a temporary `.dzmodel`, then publishes the completed file. It contains only public model files and the exact supported manifest, including original terms. It excludes cases, prompts, answers, settings, history, profile, engine home and credentials. Use exFAT or NTFS for the Gemma 4 pack; FAT32 cannot hold a file over 4 GB.

On the offline computer choose **Install model from a file…**, select the pack, review its model terms, then choose **Install this model**. Every entry must match the app's built-in manifest, SHA-256 and size list. No signing key, external trust list, arbitrary model or executable is accepted. Import rejects unsafe archive paths, extra/missing/duplicate entries, symlinks, bad sizes, excessive expansion and unbounded ZIP64 metadata. It checks all archive blobs even when a local copy exists, stages verified files, and publishes the manifest last. Cancelling or failing verification preserves the previous model; partial verified cache data from a later engine failure can be reused.

A release can recognise several fixed revisions of either supported model. Previously verified installations and matching packs remain usable when their revision is still in the built-in list. Export saves the exact installed revision. Fresh downloads use the reviewed current revision; if the online tag changes to unknown files, the card asks for an app update or a matching local pack instead of retrying the same incompatible download.

Keep the app release and its matching model pack together for colleagues. A pack for another inventory version is rejected. See [MODEL-SETUP-IMPLEMENTATION.md](MODEL-SETUP-IMPLEMENTATION.md) for the exact format and test record. Filesystem operations on a user-selected cloud-synced or shared location remain under that storage system's control; the offline route uses local storage/USB.

## Office proxies

Before starting the app, an office can set **HTTPS_PROXY** and **NO_PROXY** in the user's environment. Only these proxy settings are allowlisted into the managed engine, with loopback addresses added to the bypass list. HTTP_PROXY, ALL_PROXY and arbitrary certificate-file overrides are not copied. Restart the app after changing them. A proxy certificate must already be trusted by Windows; the app does not install certificates or weaken TLS validation. This follows [Ollama's proxy guidance](https://docs.ollama.com/faq#how-do-i-use-ollama-behind-a-proxy).

The app's loopback clients still disable proxies and redirects. Proxy values, credentials, raw engine output and raw pull errors are not logged, displayed or included in model packs. The card shows localised error categories. An existing Ollama keeps its own proxy configuration. If office proxy configuration is unavailable, use the local pack route.

## How it is enforced in code

- `LoopbackGuard` accepts only literal loopback IPs and localhost, rejects other hosts without DNS, disables proxies/redirects and opens connections only to loopback. It is used by inference and setup clients.
- Cloud model names and remote aliases are refused. The managed child has `OLLAMA_NO_CLOUD=1` and a cleared, allowlisted environment; its private home/config/temp/models live under the app's local root. Raw stdout/stderr is drained and discarded.
- The CPU engine is resolved from a packaged absolute path and checked against its built-in file hashes before execution. No runtime engine download is used. Runtime licence notices accompany it.
- Supported model files are verified before the existing provider receives case text. Inference/export blockers, AI authorship marks, lawyer review, recovery and unsaved-work safeguards are unchanged.
- Model packs are copied through bounded buffers and exact entry lists. Import/export have no pull route.
- Normal desktop builds do not reference hosted providers; publishing an online testing build fails. The web demo's service graph does not start desktop model setup.
- Tests cover loopback/cloud refusal, process ownership/coexistence, progress/cancel/resource checks, model identities, archive safety and template work without a network. Actual clean-account, interrupted-download and physically disconnected-network acceptance is recorded separately, not inferred from mock tests.

The .NET SDK and Avalonia build tasks may have developer-machine telemetry; set `DOTNET_CLI_TELEMETRY_OPTOUT=1` and `AVALONIA_TELEMETRY_OPTOUT=1` (CI does). These are build tools, not the running lawyer's app, and have no case content.

## Verify it yourself

1. With networking disconnected, start the app and use templates with fictitious notes; no model is required.
2. With a supported model already installed or imported, use local AI and restart offline. No online account is required.
3. With networking connected, opening/idle/reopening setup must not start a download. Only Download/Retry starts registry/CDN traffic from the engine.
4. Cancel an authorised pull, verify it stops, and retry. Model requests remain on loopback; watch only this app's owned engine processes when distinguishing unrelated Ollama/browser activity.
5. Import/export a matching local pack without a network. It must preserve cases and reject a changed pack.

## Where data is stored

New private files stay under `%LOCALAPPDATA%\DzLegalAssistant`:

- `models`: private model manifests, blobs, saved partial downloads and operation staging;
- `engine-home` / `engine-temp`: isolated engine files, never included in a pack;
- `model-setup.json`: the local preference for template mode;
- `drafts`: recovery/default case storage; explicit case saves still go where the lawyer chooses;
- `evaluations`: local evaluation records.

A reused Ollama's model store remains its own. The app does not remove or rewrite it. Explicit `.dzmodel` exports go to the chosen file. Nothing is synchronised by the app.