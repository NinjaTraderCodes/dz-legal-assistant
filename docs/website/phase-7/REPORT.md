# Website, phase 7: the case file

The website's own visual world, chosen on 4 November 2026 from four directions (`site/DESIGN.md`, with the product facts in `site/PRODUCT.md`): the site is a lawyer's case file, cardboard folders with numbered tabs, paper sheets filed on them, and violet registry ink. Built on `claude/case-file-site`. The demo at `/app`, the desktop app and every app screenshot are unchanged. The rules of the earlier phases hold: no link to the repository (`check-dist`), nothing loaded from another origin, both languages and both themes at 390, 1366 and 1920 px, every figure with its source, budgets met, axe at 0 violations, 0 warnings and every test green.

## What changed

**The world layer** (`site/src/styles/world.css`): the folder colours (kraft ground and board, salmon, blue, green, yellow) for light and dark, the sheet, carbon text, violet ink, red for what is missing (on sheets only), and the primitives: folder and tab, sheet, clip, staple, stamp with its ink filter, register tick, tag. Every text colour measured at WCAG AA or better on every folder of its theme. `tokens.css` (the app's theme) is unchanged and still serves the pieces of the app.

**Fonts** (`site/fonts/`, OFL, licences beside them and in `public/licences/`): Markazi Text for titles, Readex Pro for text, Aref Ruqaa for the stamps only, each subset per script by `scripts/fonts.mjs` (Markazi 46 + 29 KB, Readex 27 + 32 KB, Ruqaa 20 KB cut to the stamp texts). A page preloads its script's two faces: 73.8 KB on Arabic pages, 61.1 KB on English ones.

**The home, eight folders**: 01 the hero (salmon: the title, the two actions, the laptop lying on the folder with the "works offline" stamp); 02 the problem (a fragment of a statement of claim with the domicile blank, the red note with art. 15-3, the check at 7 / 9); 03 the scroll story (a register sheet ticked in violet beside the statement of claim; at 9 / 9 the register is stamped «جاهزة للإيداع» / "Ready to file … once the lawyer has reviewed and signed"); 04 the AI as a numbered drafting log, with the hidden-set figures typed on a report sheet; 05 the guarantees as a list of exhibits; 06 the five-minute test (blue); 07 demo or download (green); 08 the team as a signature block. The five-minute strip moved from under the hero to 06, after the proof.

**Secondary pages**: one folder each (try and documents blue, privacy green, law yellow, download, about and 404 board); their former labels above the title became the folder's tab. Cards and panels are sheets.

**Social previews**: the 14 `public/og/*.png` images were regenerated from the new pages (`scripts/og.mjs`).

## Decisions worth a look

- **The stamp is on the register, not the statement of claim.** The sample case forces the app's «مسودة» watermark on the paper; "ready to file" over a draft mark would contradict the app. On the register it marks what is complete (9 / 9, reviewed). The approved mock-up had it on the statement of claim.
- **The problem's fragment of the statement of claim is set in Markazi**, not Noto Naskh: it is a drawn fragment, and on a phone it sits close to the first screen.
- **The fourth guarantee** attaches the reviewed row instead of the drafted-grounds capture, which the drafting log shows just above.
- **The hero laptop is preloaded** and asked for at high priority: without the photograph it is the first screen's largest element (Lighthouse names `img.laptop` as LCP on both homes).

## Performance

The first build measured 3.5 s (Arabic) and 3.0 s (English) mobile LCP against main's 2.3 and 2.0 s on the same machine. Cause: a phone fetched the faces of the statement of claim and the app (Noto Naskh Arabic, Inter, about 190 KB) with the first layout, although they are used only far down the home. Fix: `theme-init.js` marks the page `data-faces="later"`, `ui.js` lifts it after load, and until then a phone sets those texts in Markazi (desktops and visitors without scripts are unaffected); folder contents are laid out lazily on phones as the sections were before. Result below. The Arabic home's HTML went over its 30 KB budget once (30.6 KB); Tailwind had emitted utilities for words found only in comments (`shadow`, `transform`, `filter`… and their 34 `@property` rules): they are excluded, and the page is 29.4 KB.

Lighthouse 13, local instance, Chromium headless (`lighthouse-summary.md`):

| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |
|---|---|---|---|---|---|---|---|---|
| /ar/ | mobile | 98 | 100 | 100 | 100 | 2.4 s | 0 | 0 ms |
| /ar/ | desktop | 100 | 100 | 100 | 100 | 0.7 s | 0 | 0 ms |
| /en/ | mobile | 99 | 100 | 100 | 100 | 2.1 s | 0 | 0 ms |
| /en/ | desktop | 100 | 100 | 100 | 100 | 0.6 s | 0 | 0 ms |
| /ar/try/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 0 ms |
| /ar/try/ | desktop | 100 | 100 | 100 | 100 | 0.5 s | 0 | 0 ms |
| /en/law/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 0 ms |
| /en/law/ | desktop | 100 | 100 | 100 | 100 | 0.2 s | 0.001 | 0 ms |

## Checks

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean |
| Playwright `site.spec.ts`, `axe.spec.ts`, `guide.spec.ts` (Chromium, against the published app) | 54 passed, 2 skipped by design: the fallback test needs an instance without the site (`FALLBACK_BASE_URL`), and the guide's five-step run needs a demo with a model (`GUIDE_BASE_URL`); axe 0 violations in both themes |
| `npm run budget --lighthouse ../docs/website/phase-7/lighthouse-summary.md` | every budget met |
| `scripts/links.mjs` | 40 internal links, 0 problems (outbound hosts are unreachable from the build machine's network policy) |
| `dotnet build -c Release` | 0 warnings, 0 errors |
| `dotnet test -c Release` | 986 of 986 green |

**Test updated** (`site/tests/site.spec.ts`, the hero test): it expected the welcome photograph's preload and the laptop at low priority; it now expects the laptop's AVIF preload, high priority, no photograph, and the primary button's new class (`.btn-primary`, was `.btn-gold`). It checks the same layout as before: one picture for both languages, AVIF with a WebP fallback, beside the text from 900 px and under the actions on a phone.

## Captures in this folder

`home-{ar,en}-{dark,light}-{1366x768,390x844}.jpg` (the full home), `hero-{ar,en}-{dark,light}-1920x1080.jpg` (the first screen), `story-ar-light-{8of9,9of9}-1366x768.jpg` (the scroll story at 8 / 9 and at the end), and each secondary page at 1366 in Arabic dark and English light (`try`, `law`, `privacy`, `download`, `about`, `docs`, `404`). Captured at device scale 1 with reduced motion (final states).
