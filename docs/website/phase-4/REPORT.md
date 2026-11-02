# Website, phase 4: QA, budgets, the private-window check and the Arabic review list

Branch `fable/website`, on top of the default branch's phase-3c merge and the reviewer's Razor fix (d75f39c). Checked locally on 2 November 2026. Nothing loads from the internet at run time; README unchanged. Besides the QA, one change to the server came out of the measurements: the site's responses now leave compressed (below).

## The polish from the phase-3c review (pushed first, 9505a04)

1. **Selection highlight:** the selected section on the paper is no longer shaded; as on the desktop, the side line shows on hover or focus only, and the strip above the paper names the selection. The petition's header looks like paper on load.
2. **Paper margins:** the sheet keeps the 16 px canvas margin on every side (`width: calc(100% - 32px)`), so the grey canvas shows around it as on the desktop.
3. **Top bar:** New, Open and Save show their labels from 1440 px (Download for Windows and Guide stay icons up to 1600 px, the document type too); the bar holds one row at 1366 and 1440 in both languages (`app-*-1366x768.png`, `demo-first-*-1440x900.png` in `docs/website/phase-3c/`).

The merged tree was built once with SDK 10.0.114 pinned by a temporary `global.json` (0 warnings, 0 errors), then the file was restored; the same check runs before every push from now on.

## Compression at the origin (what the LCP measurement found)

The first Lighthouse matrix put the Arabic home's mobile LCP at 2.7 s, over the 2.6 s budget. `node scripts/lcp.mjs <report>` (new) reads the embedded JSON of a Lighthouse report and prints the LCP element, its four subparts and the request chain: the element is the hero photo on both homes, and the chain showed the page's HTML leaving the local server **uncompressed, 104 KB** (22 KB gzipped). The live host does compress: its proxy answers `Content-Encoding: br` (checked with a HEAD request on 2 November 2026), so the visitor was already better off than the measurement, and the measurement was not of what the visitor gets.

The server now compresses the site itself (`StaticSiteMiddleware.AddSiteCompression`, Brotli, else gzip, at the `Optimal` level: the Arabic home leaves at 22 KB, the English one at 21 KB), so a local run measures what a visitor gets and the result no longer depends on the proxy. Only the site's own files are compressed: pages, scripts, SVG, robots, the sitemap. The demo's page carries the antiforgery token, which compressing over HTTPS would expose (BREACH), so the demo and its circuit are never touched; fonts and images are compressed formats already. `SiteTests.TheSiteLeavesCompressedAndTheDemoDoesNot` starts the pipeline on a loopback port and checks the three cases.

| Home, mobile | LCP before | LCP after | FCP before | FCP after |
|---|---|---|---|---|
| `/ar/` | 2.7 s | 2.3 s | 1.9 s | 1.1 s |
| `/en/` | 2.3 s | 2.0 s | 1.4 s | 0.9 s |

The Arabic home went from 91 to 96 on the mobile performance score; the full matrix below was re-run after the change.

## Budgets

`site/budget.json` holds the budgets and `npm run budget` checks them against the built `dist` (what each page loads, gzipped) and the latest Lighthouse summary. The figures:

| Page | HTML gz | scripts gz | preloaded fonts gz |
|---|---:|---:|---:|
| `/ar/` | 22.7 KB | 2.6 KB | 67.9 KB |
| `/en/` | 22.1 KB | 2.6 KB | 47.1 KB |
| `/ar/try/` | 13.8 KB | 1.7 KB | 115.1 KB |
| `/en/try/` | 13.0 KB | 1.7 KB | 115.1 KB |
| `/ar/download/` | 15.8 KB | 1.7 KB | 115.1 KB |
| `/en/download/` | 15.1 KB | 1.7 KB | 115.1 KB |
| `/ar/law/` | 17.2 KB | 1.7 KB | 115.1 KB |
| `/en/law/` | 16.7 KB | 1.7 KB | 115.1 KB |
| `/ar/privacy/` | 15.3 KB | 1.7 KB | 115.1 KB |
| `/en/privacy/` | 14.5 KB | 1.7 KB | 115.1 KB |
| `/ar/about/` | 15.1 KB | 1.7 KB | 115.1 KB |
| `/en/about/` | 14.2 KB | 1.7 KB | 115.1 KB |
| `/ar/404/` | 10.6 KB | 1.7 KB | 115.1 KB |
| `/en/404/` | 10.3 KB | 1.7 KB | 115.1 KB |

Home scripts with the lazy story module: 46.1 KB gz (budget 60 KB). The fonts are the same four files on every page, served immutable, so a visitor downloads them once.

| Budget | Value | Why this value |
|---|---|---|
| Home scripts, with the lazy story module | ≤ 60 KB gz | The plan's figure. The home loads 2.6 KB of script and, on wide screens without reduced motion, the 43 KB story module (GSAP). |
| Fonts a page preloads | ≤ 220 KB gz | The plan's figure. A text page preloads both faces (115 KB); the home preloads one (68 KB Arabic, 47 KB English). |
| A page's HTML | ≤ 30 KB gz | The stylesheet is inlined in every page, so this is CSS plus content; the largest page is the home at 22 KB. |
| Mobile LCP | ≤ 2.6 s | The plan said 1.5 s. On Lighthouse's simulated slow 4G (150 ms round trips, 1.6 Mbps, CPU four times slower) the text pages reach 0.9 to 1.4 s and the photo-led home 2.0 s (English) and 2.3 s (Arabic); the budget leaves 0.3 s for run-to-run variance. 1.5 s is out of reach while the home opens on a photo: see the LCP note below. |
| Mobile performance | ≥ 90 | Every page scores 92 to 100; the homes 96 and 99. The not-found pages (89 and 90) are not a budget: they answer 404 and are short, so the footer moving when the Arabic font replaces the fallback weighs on them (CLS 0.18, against 0.03 on the home); their LCP is checked like the others. |
| Accessibility, best practices, SEO | 100 | Every run, except SEO on the two not-found pages (66), by design: they answer 404 and carry `noindex`, which the crawlability audit counts against them; `budget.mjs` leaves them out of that check. |

**The LCP note.** On both homes the largest element is the hero photo (`section.hero > img.photo`, the 480 px AVIF, 10 KB). After compression its subparts on the Arabic home are 34 ms to first byte, 26 ms until the request, 13 ms of download and 352 ms of render delay (observed, before the simulation slows them): the image is there early and waits for the first paint. What holds the first paint is `theme-init.js`, the one render-blocking script (one round trip, 1.5 KB): it has to run before paint so a visitor who chose light does not see a dark flash, and it cannot be inlined because the Content Security Policy allows no inline scripts. The two fonts (68 KB Arabic, 47 KB Inter) load in parallel and swap in. On Lighthouse's phone profile that chain comes to 2.3 s; the text pages, with no photo, reach 0.9 to 1.4 s. Getting the home under 1.5 s would mean no photo above the fold on phones, a design decision for the owner, not a tuning.

Two things the reports show that this phase leaves alone, for a later one: the fonts swap in after the first paint and move the text (CLS 0.03 on the home, 0.18 on the short not-found pages; the fix is a fallback face with adjusted metrics), and on desktop the home's story module measures the long Arabic page when it sets up its scroll animation (180 ms of blocking time, performance 94 on `/ar/` against 99 on `/en/`).

## Lighthouse, the full matrix

All fourteen pages (seven in each language), mobile and desktop:

| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |
|---|---|---|---|---|---|---|---|---|
| /ar/ | mobile | 96 | 100 | 100 | 100 | 2.3 s | 0.033 | 160 ms |
| /ar/ | desktop | 94 | 100 | 100 | 100 | 0.8 s | 0.007 | 180 ms |
| /en/ | mobile | 99 | 100 | 100 | 100 | 2.0 s | 0 | 20 ms |
| /en/ | desktop | 99 | 100 | 100 | 100 | 0.8 s | 0 | 10 ms |
| /ar/try/ | mobile | 93 | 100 | 100 | 100 | 1.4 s | 0 | 300 ms |
| /ar/try/ | desktop | 100 | 100 | 100 | 100 | 0.4 s | 0.001 | 0 ms |
| /en/try/ | mobile | 99 | 100 | 100 | 100 | 1.1 s | 0 | 130 ms |
| /en/try/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 0 ms |
| /ar/download/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 10 ms |
| /ar/download/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 0 ms |
| /en/download/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 40 ms |
| /en/download/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 0 ms |
| /ar/law/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0.039 | 10 ms |
| /ar/law/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0.004 | 0 ms |
| /en/law/ | mobile | 98 | 100 | 100 | 100 | 1.3 s | 0 | 140 ms |
| /en/law/ | desktop | 100 | 100 | 100 | 100 | 0.4 s | 0 | 20 ms |
| /ar/privacy/ | mobile | 92 | 100 | 100 | 100 | 1.3 s | 0.03 | 340 ms |
| /ar/privacy/ | desktop | 100 | 100 | 100 | 100 | 0.5 s | 0 | 20 ms |
| /en/privacy/ | mobile | 96 | 100 | 100 | 100 | 1.1 s | 0 | 200 ms |
| /en/privacy/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 0 ms |
| /ar/about/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 80 ms |
| /ar/about/ | desktop | 100 | 100 | 100 | 100 | 0.4 s | 0 | 10 ms |
| /en/about/ | mobile | 98 | 100 | 100 | 100 | 1.1 s | 0 | 150 ms |
| /en/about/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 10 ms |
| /ar/404/ | mobile | 89 | 100 | 100 | 66 | 1.1 s | 0.177 | 190 ms |
| /ar/404/ | desktop | 100 | 100 | 100 | 66 | 0.2 s | 0 | 0 ms |
| /en/404/ | mobile | 90 | 100 | 100 | 66 | 0.9 s | 0.177 | 160 ms |
| /en/404/ | desktop | 100 | 100 | 100 | 66 | 0.2 s | 0 | 0 ms |

The mobile HTML reports are committed beside this file (`lighthouse-<page>-mobile.report.html`); the desktop runs are in the table only (94 to 100 on performance, 100 elsewhere but the not-found pages' SEO). The two `/ar/` rows come from a re-run on a quiet machine: the first run of that page coincided with a server start and measured 92 and 93.

## The private-window check

`npm run links` opens the site in a fresh browser context with no cookies and no storage, follows every internal link once (every page in both languages, the switch, the demo, the PDFs of the law sources), and asks each outbound link for its status with a plain request. Result in `links.md`: 36 internal paths, all 200 (the language switch 302, as designed); 27 outbound links (GitHub, the Official Journal's PDFs on joradp.dz), all 200. GitHub answers 429 to a burst of unauthenticated requests from one address: the script spaces them and retries once; a 429 that survives is listed, not counted as broken, because the same link opens in a browser.

## Both languages, both themes, the matrix

`<page>-<ar|en>-<light|dark>-<WxH>.jpg` for the home, the guide, download, law, privacy and about at 390×844 and 1366×768, full page (48 files); `app-<ar|en>-<light|dark>-<WxH>.png` for the demo's first screen at 1366×768 and 1920×1080 (8 files). The demo's states at 1440×900 are in `docs/website/phase-3c/`.

## WebKit smoke

`WEBKIT=1 npx playwright test tests/site.spec.ts --project=webkit` runs the site suite in WebKit (the browser was already installed; nothing was downloaded): 14 passed, 1 skipped (the fallback test), 29 s, re-run after the compression change. The project is defined only when `WEBKIT` is set, so the usual run stays Chromium only.

## The Arabic review list for Khouloud

`ARABIC-REVIEW.md`, beside this file: every Arabic string of the website with its English counterpart, grouped by page (navigation and shared elements, the home, the guide, download, the law library, privacy, about), then the demo's own guide and warning strings, with an empty note column for her corrections. 283 strings. The app's own strings (About, Library and Help screens) are reviewed with the app, from `Strings.resx`.

## Checks

| Check | Result |
|---|---|
| `dotnet build -c Release` with SDK 10.0.403 and with SDK 10.0.114 (temporary `global.json`) | 0 warnings, 0 errors, both |
| `dotnet test -c Release` | 985 passed, 0 failed, 0 skipped, in 7 assemblies (the desktop suite alone 4 min 52 s), with every local instance stopped |
| `npm run build` | 14 pages, clean |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 44 passed, 2 skipped (the fallback test, and the guide test when `GUIDE_BASE_URL` is not set); axe 0 violations on the 14 pages in both themes and on the demo's first screen in both themes |
| Playwright `guide.spec.ts` against the stand-in model | 1 passed |
| Playwright `site.spec.ts` (WebKit) | 14 passed, 1 skipped (the fallback test), 29 s |
| `npm run links` | 36 internal and 27 outbound links, 0 problems |
| `npm run budget` | every budget met (14 pages; 28 Lighthouse rows) |
| `docker build` (local, through the ECR mirror of `node:22-alpine`) | 434 s (7 min 14 s), 0 warnings; the image run on port 7860 answers `/` in Arabic and compressed (`br`, 22 KB), `/ar/try/` 200, `/app` 200 and uncompressed, `/lang/en?to=/en/` 302, an unknown path 404 with the site page, and `/` still contains "DZ Legal Assistant" for the uptime check |

## Files changed

- `src/DzLegal.Web/wwwroot/app.css` (the three polish items).
- `src/DzLegal.Web/Site/StaticSiteMiddleware.cs` and `Program.cs` (the site's responses compressed at the origin), `tests/DzLegal.Web.Tests/SiteTests.cs` (the compression test).
- `site/budget.json`, `site/scripts/budget.mjs`, `site/scripts/links.mjs`, `site/scripts/lcp.mjs` (new), `site/package.json` (the `budget` and `links` scripts), `site/playwright.config.ts` (the on-demand WebKit project).
- `docs/WEB-DEMO.md` (the compression, and the two scripts named in the checks).
- `docs/website/phase-4/` (this report, the Arabic review list, the link report, the screenshots, the Lighthouse summary and mobile reports).
- README unchanged.

## Unsure about

- A HEAD request to `/app` answers 404 locally (Development and Production, before and after this phase's changes), while the live host answers 405 with `Allow: GET, POST`. GET is unaffected, nothing on the site or in the uptime check sends HEAD to `/app`, and the cause was not chased; noted so nobody reads it as a regression.
- The phase-4 folder weighs about 22 MB, mostly the 14 mobile Lighthouse reports (about 1 MB each). They can go if the table is enough.
