# Website, phase 1: foundation and home page

Branch `fable/website` (from `claude/stoic-fermat-mi1gko` at dc735e5, v0.9.1). Built and checked on 1 November 2026 on the owner's laptop; GitHub Actions are off, so every check below ran locally. Nothing was installed on the server; nothing loads from another origin at run time.

## What this phase ships

- **The public website** (`site/`, Astro 7, static HTML) served by `DzLegal.Web` at `/ar/` and `/en/`: the welcome, the problem, the three guarantees, the nine steps on paper with the formal check, the AI in its place, the demo and the download, the team. Arabic is written first; English is the second locale. Design tokens, fonts and icons are the desktop app's own (`site/scripts/tokens.mjs` generates them from `App.axaml` and `Styles/Workbench.axaml`; the build fails when they drift).
- **The demo at `/app`** as well as at `/` (unchanged for the judges): same page, same behaviour, plus a branded loading shell (scales mark, "Loading the demo…" in the visitor's language) until the circuit connects, and `<html lang/dir>` set before the circuit from the visitor's language.
- **Language and theme, shared by site and demo:** a remembered choice (`/lang/{code}` sets the cookie `dzlegal.lang`), then `?lang=`, then the browser (Arabic → Arabic, anything else → English); light by default, dark only when chosen (`dzlegal.theme`).
- **Server:** `Site/` (language choice, switch, static site serving, security headers), a single Content-Security-Policy header on every route (the framework's duplicate `frame-ancestors` is gone), the site's own 404 page, `/healthz` reports `site: true|false`, Node build stage in the `Dockerfile`, `Site__Root` for local runs (`--launch-profile http+site`).
- **Checks:** 18 new xunit tests (language choice, switch safety, site mapping, headers), a Playwright suite (routes, redirects, cookies, headers, demo shell, theme memory) and axe on every page in both languages and themes.

## Changes to the live demo's defaults (please read)

1. **Theme:** the demo was dark on machines whose system theme is dark; it is now **light by default** for everyone, dark when the visitor chooses it (remembered).
2. **Language:** the demo was English unless `?lang=ar`; it now **follows the browser**: an Arabic browser gets Arabic, every other one English, and the choice made with the language button is remembered.
3. **`/` is unchanged:** the demo, as submitted. The site is reviewed at `/ar/` and `/en/`; the flip (`Site:ServeRoot`) is a separate, one-line merge after your review.
4. **Not-found pages** (`/anything`) now show the site's 404 instead of the demo's.

## Figures on the site and their sources

| Figure on the site | Source |
|---|---|
| 1,361 articles from 6 legal texts; complete CPCA (with Law 22-13) and Family Code; selected Civil, Commercial and Labour articles | `README.md` ("Law texts"), the About screen's `AboutSources` string |
| "The only endpoint the model may use is the computer's own address… no usage statistics" | `README.md` ("Offline, no data leaves the machine"), `docs/OFFLINE.md`, `LoopbackGuard` |
| Review before export: AI sections marked, PDF/Word refused until reviewed | `README.md` ("Assistive, never autonomous") |
| Three stages of the analysis; "nothing is ticked until the lawyer confirms" | `README.md` ("Legal grounds: analysis of the facts") |
| Hidden set, 90 fictitious cases, 31 Oct 2026: online model, **read mode**, claim type right 85/90 (94 %), fully right 60/90 (67 %); local Gemma 3 4B, **claim type given**, fully right 32/90 (36 %) | `docs/HIDDEN-SET.md`, results table; baselines in `docs/baselines/` |
| About two minutes per draft on an ordinary laptop; 117–181 s measured on an i5-1335U | `README.md` ("Legal grounds"), `docs/WEB-DEMO.md` |
| Ollama + gemma3:4b about 3.3 GB; 16 GB RAM recommended; template mode without AI | `README.md` ("Run it") |
| Team and roles; ThirdUni 2026 | `README.md` ("Team and how it was built"), the About strings |
| Version 0.9.1 | `Directory.Build.props` |
| Formal-check example "7 / 9", article 15-3, domicile missing | the sample case in the demo (`src/DzLegal.Web/Samples/debt.json`) with the defendant's domicile cleared, as in the video |

The petition on paper is the fictitious sample case (`debt.json`); no real person, place or case.

## Checks

| Check | Result |
|---|---|
| `dotnet build -c Release` (whole solution) | 0 warnings, 0 errors |
| `dotnet test -c Release` (whole solution) | all green: Storage 18, Documents 6, FormEngine 67, Drafting 399, Law 116, Web 66, App 305 (headless UI, 5 min) |
| `tests/DzLegal.Web.Tests` | 66 passed (48 existing + 18 new) |
| `npm run build` (`site/`) | 4 pages, `check-dist` clean: no inline scripts, no reserved paths, `lang`/`dir` on every page |
| Playwright (`site/tests`) | 17 passed: routes, 301 for bare paths, 404 pages in both languages, cookie switch, one CSP header, immutable assets, demo shell and language, theme memory |
| axe (`wcag2a`, `wcag2aa`, `wcag21aa`, best-practice) on `/ar/`, `/en/`, both 404 pages, light and dark, and the demo's first screen | 0 violations |
| Lighthouse 13.6 (Edge, simulated mobile and desktop) | `/ar/` mobile 95 · desktop 100; `/en/` mobile 98 · desktop 99; accessibility, best practices and SEO 100 on all four runs (`lighthouse-summary.md`, HTML reports beside it) |
| `docker build` (with the Node stage) | see "Docker" below |

Lighthouse notes: the first run scored 74 and 83 on mobile. Three changes brought it to 95 and 98: the scroll-story code (GSAP, 111 KB) now loads only on wide screens without reduced motion, the stylesheet is inlined (one render-blocking round trip fewer on a far-away host), the page no longer uses a fixed background or a blurred header on phones, phones lay out sections only as they come near the screen, and the hero photograph is preloaded in its responsive set (9, 16 and 29 KB AVIF variants). The remaining mobile cost is the simulated slow 4G fetch of the hero and the Naskh font; TBT is 130 ms (Arabic) and 40 ms (English).

## Docker

The Dockerfile gained a Node stage (`npm ci --omit=dev`, then `npm run build`, whose prebuild checks that the committed tokens and strings are current, and whose postbuild checks the output against the CSP). `astro`, `tailwindcss`, `sharp` and the generator libraries are normal dependencies; only Playwright, axe and Lighthouse are dev dependencies. The lock file is generated inside the Linux image (`npm install --package-lock-only` in `node:22-alpine`) so that it lists the Linux optional packages behind `sharp`; a lock generated on Windows left them out and `npm ci` refused it. `sharp` loads on linux x64 (musl) in that image. The build time measured locally is given below; this laptop could not reach Docker Hub tonight, so the local build used `--build-arg NODE_IMAGE=public.ecr.aws/docker/library/node:22-alpine`, a mirror of the same official image; Render uses the default `node:22-alpine`.

**Node stage, run in the Linux image with the Dockerfile's exact file layout** (`docker run … node:22-alpine`, mirror image): `npm ci --omit=dev` 1 min, then `npm run build` with the token and string checks, the font subsetting, the four pages, the Open Graph images and the dist check: **115 s in all**, 1.1 MB of output, sharp loading on linux x64 (musl).

**Full `docker build`, 1 November, owner's laptop:** completed, exit 0, **1,901 s in all**, almost all of it network: the .NET SDK base image took 1,619 s to download and the runtime image 397 s (Docker Hub was unreachable that evening, so the Node image came from the mirror). The work itself: `npm ci --omit=dev` 148 s, the site build with its checks 11 s, `dotnet publish` 272 s. Image size 443 MB. The built image was then run locally (`docker run -p 7861:7860`): `/healthz` answers with `site: true`, `/` is the demo, `/ar/` and `/en/` serve the site, `/app` follows the browser's language, one Content-Security-Policy header, the site folder and its hashed assets are inside the image. On Render, with its own network, the build should take the work part only, about seven to eight minutes.

## Screenshots

`home-<ar|en>-<light|dark>-<WxH>.jpg` (full page) and `app-<ar|en>-<light|dark>-<WxH>.png` (first screen) at 390×844, 768×1024, 1366×768 and 1920×1080, captured with reduced motion so the scroll story shows its final state.

## How to review

After the merge, open `https://dz-legal-assistant-ai.onrender.com/ar/` and `/en/` in a private window; `/` is still the demo. Switch language and theme in the header; open the demo from the gold button; check `/healthz` shows `"site":true`. The flip to `/` is one setting (`Site:ServeRoot: true` in `appsettings.json`) plus removing `@page "/"` from `Home.razor`, prepared as phase 1b.

## Known limits of this phase

- The secondary pages (download, law library, privacy, about) are phase 2; the header links point to the home's sections until then.
- The scroll story pins the nine steps on screens wider than 900 px without reduced motion; phones and reduced-motion readers see the finished sheet.
- `app-*.png` show the demo as it is today (phase 3 restyles it); they are here so the shell, the language and the theme defaults can be checked.
