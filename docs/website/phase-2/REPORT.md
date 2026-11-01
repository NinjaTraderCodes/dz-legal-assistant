# Website, phase 2: the secondary pages, dark by default, a clean hand-over to the demo

Branch `fable/website`, on top of the default branch's phase-1b merge (317f10f). Built and checked locally on 2 November 2026 on the owner's laptop; GitHub Actions are off, so every check below ran here. Nothing loads from the internet at run time; the site still has no analytics, no other-origin resource and no inline script.

## What this phase ships

- **Four pages, Arabic first, English second,** each at `/ar/<page>/` and `/en/<page>/` with canonical and hreflang links, its own Open Graph image and a place in the sitemap:
  - **Download** (`/download/`): the Windows app, the three install steps, the requirements, the optional local model (Ollama + gemma3:4b) with the measured waits, where the data lives, the source-code route, where this version stands, and the demo as the no-install alternative.
  - **Law library** (`/law/`): the six official texts as cards (reference, edition, Official Journal issue, amendments applied, "current to" date, articles loaded, Complete/Partial, how it was checked, "not yet reviewed by a lawyer", the official PDF and the amendment PDFs), the three checking methods in the app's own words, what to know, the limits stated plainly, and the 21 claim types by area. The cards are generated from `data/law/codes/*.json`, `data/law/sources.json` and `data/law/claims/*.json` by `site/scripts/law.mjs`; the build fails if the committed summary is stale, so no figure on the page can drift from the data.
  - **Privacy** (`/privacy/`): what the desktop app does on the network (nothing with case data), how the code enforces it, how to verify it, where data is stored, how the web demo differs (hosted model, fictitious cases only, nothing stored on the server, the browser's recovery copy, the hourly limits) and what this website itself does (two first-party cookies, no tracking).
  - **About** (`/about/`): what it does and does not do, the team with what each person did, how it was built (the AI coding agents, the stack), the legal sources, the licences of everything bundled (the About screen's list, the welcome image's provenance, this site's fonts and icons), status and limits, what is next, and the documentation.
- **Every figure and claim names its source** in a "Source:" line that links to the file in the repository (table below).
- **Navigation:** the header lists the pages (the current one underlined); on phones the pages fold into a native `<details>` menu, no script; the footer lists them too.
- **Dark is the default theme, on the site and in the demo.** `<html data-theme="dark">` is written into every page; `theme-init.js` lifts it before the first paint only for a remembered "light". The demo reads the same cookie through one tested method (`LanguageSwitch.IsDarkTheme`: dark unless the cookie says `light`). A visitor who switches to light on either side keeps it on both; the `theme-color` of the browser frame follows the theme shown.
- **The demo link no longer aborts a page transition.** The site's pages cross-fade into each other (`@view-transition { navigation: auto }`); the demo at `/app` is another program that does not opt in, so Chrome started a transition towards it and then logged "Transition was aborted because of invalid state. ViewTransition opt-in disabled". `ui.js` now listens to `pageswap` and skips the transition when the destination is `/app` (or another origin). Verified in Chromium: without the handler the error is raised as a page error on arrival at `/app`; with it, nothing. A Playwright test clicks the link and asserts no such error.
- **Generators and build:** `scripts/law.mjs` (new, with `--check` in `prebuild`); `scripts/strings.mjs` exports the extra About, Library and Help strings the pages reuse, plus the version from `Directory.Build.props` (one source for "0.9.1" everywhere); the Dockerfile's Node stage copies `data/law/codes`, `data/law/claims`, `data/law/sources.json` and `Directory.Build.props`.
- **Open Graph images** for all five pages in both languages, captured in the dark default (`site/public/og/`).
- **Three small performance changes** found while measuring the new pages: balanced heading wrap only on h1 and h2 (each balanced heading costs extra layout passes, and the law page has many); both fonts preloaded on the text pages (each page shows the other language's name in its own script, and the late second font re-laid the page out), but not on the home, where the second font competed with the hero photograph on the simulated slow connection and cost the Arabic home 7 points; and the browser frame's colour taken from two attributes written on the `theme-color` tag at build time from the tokens instead of a computed style at load. The table in `lighthouse-summary.md` is the final build's.

## Changes to the live demo's defaults (please read)

1. **Theme:** the demo was light by default since phase 1; it is now **dark by default**, as the website, and light when the visitor chooses it (remembered on both). Nothing else in the demo's behaviour changed; the only edits to `src/DzLegal.Web` are the two theme reads and one comment in `app.css`.

## Figures on the site and their sources

| Figure or claim | Page | Source |
|---|---|---|
| 1,361 articles from 6 legal texts, 2 complete; per text: articles loaded, Complete/Partial, checking method, not reviewed by a lawyer, edition, Official Journal issue, amendments, "current to" date, retrieval date | Law library, About | `data/law/codes/*.json`, `data/law/sources.json` (generated into `site/src/content/law.generated.json`) |
| 21 claim types in 4 areas (civil 9, commercial 3, family 7, labour 2) and their names | Law library | `data/law/claims/*.json` |
| The three checking methods and their explanations; "Only loaded articles are used in automatic suggestions"; the two indicators per text | Law library | `Strings.resx` / `Strings.en.resx` (`LibraryCheck*`, `LibraryNote`, `Help7Body*`) |
| No practising lawyer has verified the rules and the catalogue; audit of October 2026; Civil Code being checked; one document type; installer planned for 1.0; the roadmap (months 1 to 3) | Law library, Download, About | `README.md` ("Status and limits", "What's next"), `docs/LEGAL-VERIFICATION.md` |
| Windows 10 or 11; zip from the latest release; `DzLegalAssistant.exe`; template mode; the three-step setup | Download | `README.md` ("Try it"), `docs/PRODUCT-DETAILS.md` |
| Ollama at 127.0.0.1:11434; `ollama pull gemma3:4b` about 3.3 GB; detection every 30 s; 8 GB minimum with gemma3:4b, 16 GB recommended; first request loads the model in 10 to 40 s; 3 min 17 s reading and 2 min 33 s drafting on an i5-1335U without a graphics card | Download | `docs/PRODUCT-DETAILS.md` ("Using the local model", "Speed on a CPU-only laptop"), `README.md` ("The AI at work") |
| `%LOCALAPPDATA%\DzLegalAssistant` (drafts, evaluations); the profile stays on the computer; the recovery copy | Download, Privacy | `docs/OFFLINE.md`, `Strings.resx` (`PrivacyNote`, `Help5Body2`) |
| The network table; loopback-only guard without DNS lookup; no proxy, no redirect; cloud models refused; the hosted providers in a separate project with a build-failing test; no telemetry | Privacy | `docs/OFFLINE.md`, `src/DzLegal.ModelProviders.Ollama/LoopbackGuard.cs` |
| The demo sends what is typed to Nemotron 3 Ultra on OpenRouter, then Google AI Studio, then Groq; nothing stored on the server; the browser copy lasts a day; 60 calls per session and 150 per address an hour; noindex | Privacy | `docs/WEB-DEMO.md`, `src/DzLegal.Web/wwwroot/dzlegal.js`, `src/DzLegal.Web/appsettings.json` |
| Two first-party cookies for a year; no inline script, no framing; nothing from another origin | Privacy | `src/DzLegal.Web/Site/SecurityHeaders.cs`, `site/public/ui.js`, `site/scripts/check-dist.mjs` |
| Team, roles and what each person did; the AI tools used to build it; the tech stack | About | `README.md` ("Team"), the About strings |
| The licences list; the welcome image's provenance | About | `src/DzLegal.Presentation/ViewModels/AboutViewModel.cs`, `src/DzLegal.App/THIRD-PARTY-NOTICES.txt`, `src/DzLegal.App/Assets/Images/welcome-bg.LICENSE.txt` |
| Version 0.9.1 | all | `Directory.Build.props` (read by `strings.mjs`) |

No new claim was written: the Arabic texts of the four pages translate the README and the docs, and the About, Library and Help wording is the app's own.

## Checks

| Check | Result |
|---|---|
| `dotnet build -c Release` (whole solution) | 0 warnings, 0 errors |
| `dotnet test -c Release` | Storage 18, Documents 6, FormEngine 67, Drafting 399, Web 73 (one new theory: dark unless the cookie says light), Law 116: all green. App 305: all green in a run on the quiet machine (7 m 34 s); see the note below on one load-sensitive test |
| `npm run build` (prebuild: tokens, strings and law up to date; postbuild: check-dist) | 12 pages, clean |
| Playwright `site.spec.ts` | 15 passed, 1 skipped (the fallback test, which needs a second instance). New: the site is dark by default and a chosen light theme is remembered for the demo too; the demo's buttons (dark default); the four pages in both languages with canonical, alternates, Open Graph image and sitemap entries (the 404 pages excluded); the law page's figures and every `/law-sources/` link answers 200 with a PDF; the demo link raises no aborted-transition error |
| axe (`wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, best-practice) on the 12 pages, light and dark, plus the demo's first screen in both themes | 26 tests, 0 violations. Two findings fixed on the way: the header and footer navigations shared an accessible name (`landmark-unique`), and the command block on the Arabic download page scrolled without keyboard access (`scrollable-region-focusable`) |
| Lighthouse 13.6 (Edge, simulated mobile and desktop) on the home and the four pages, both languages | Accessibility, best practices and SEO **100 on all 20 runs**. Performance: desktop 99 to 100; mobile 93 to 100 on the four new pages, and 96 (Arabic) and 98 (English) on the home, as in phase 1. LCP 1.1 to 1.6 s on the text pages, 2.3 to 2.6 s on the photo-led home (unchanged). Full table in `lighthouse-summary.md`, HTML reports beside it |
| `docker build` (local, through the ECR mirror of `node:22-alpine`, base images cached from phase 1) | 305 s, success; the image answered `/healthz` with `"site": true`, `/`, `/en/law/` and a `/law-sources/` PDF with 200 on a spare port |

Only the ten mobile Lighthouse reports are committed (the demanding form; every desktop run scored 99 or 100 and is in the summary table), to keep the folder at about half its weight.

## The demo in dark, checked by hand

`app-<lang>-dark-1366x768.png` and `app-<lang>-dark-1920x1080.png` (first screen with the tour card open, against the instance with AI configured) and `app-draft-<lang>-dark-<WxH>.png` (after "Draft legal grounds" in template mode, taken against a second instance started without any AI key so no online credit was spent; the demo scrolls inside its panels, so at 1366×768 the first screen ends at the draft's header and the 1920×1080 capture shows the draft state). In both languages and both sizes: the formal check ("8 / 9" with the red "missing" line and the "To fix" card on the keyed instance's first sample; "9 / 9" with the green line and the expanded "elements present" list after drafting), the draft badge ("DRAFT · REVIEW AND SIGN" / «مسودة · للمراجعة والتوقيع»), the tour card with its five steps, and the demo's own warnings (the "Demo · fictitious cases only" chip, the hosted-model paragraph, the "AI is not configured on this server" notice, the legal point in its amber card, the "Before export" notice naming the unfilled «(...)» placeholder, the engine's answer line under the draft) are all readable. The demo's stylesheet is untouched (phase 3 restyles it on the tokens).

## Screenshots

- `<page>-<ar|en>-<light|dark>-<WxH>.jpg` for `download`, `law`, `privacy`, `about` at 390×844 and 1366×768, full page; `home-<ar|en>-<light|dark>-1366x768.jpg` with the new header and footer.
- `app-<ar|en>-<light|dark>-<WxH>.png` at 1366×768 and 1920×1080 (first screen); `app-draft-<ar|en>-<light|dark>-<WxH>.png` at the same sizes after template-mode drafting.
- The social images themselves are in `site/public/og/`.

## Files changed outside `site/`

- `src/DzLegal.Web/Site/LanguageSwitch.cs` (`IsDarkTheme`), `Components/App.razor` and `Components/Pages/Home.razor.cs` (the two theme reads), `wwwroot/app.css` (one comment line).
- `tests/DzLegal.Web.Tests/SiteTests.cs` (the theme theory).
- `Dockerfile` (four `COPY` lines for the law data and the version).
- `docs/WEB-DEMO.md` (the theme bullet; the pages named in the website section).
- README: unchanged; no link needed to change.

## Noticed, not changed

- `docs/WEB-DEMO.md`, section "Limits, stated plainly", still says "39 articles are loaded"; the data holds 1,361 (the site uses the data). It is the demo's own document, outside this brief.
- The desktop test `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp` failed twice while the laptop was also running Playwright, Lighthouse and the screenshot captures, and passed when run alone and in the quiet full run reported above. No desktop code changed in this phase.
- The Arabic copy of the four pages is mine, translated from the README and the docs; a read by Khouloud of the legal wording (the claim-type names come from the catalogue, the rest is description) would be welcome before phase 4.
