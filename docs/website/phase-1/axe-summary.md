# axe-core results, phase 1 (1 Nov 2026, local run)

`npx playwright test tests/axe.spec.ts` with `@axe-core/playwright` 4.14, tags `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `best-practice`, Chromium 1248, reduced motion (the page's content, not the scroll story's transient states).

| Page | Light | Dark |
|---|---|---|
| `/ar/` | 0 violations | 0 violations |
| `/en/` | 0 violations | 0 violations |
| `/ar/404/` | 0 violations | 0 violations |
| `/en/404/` | 0 violations | 0 violations |
| `/app?lang=en` (first screen, circuit connected; `wcag2a`, `wcag2aa`) | 0 violations | — |

What was fixed on the way: the product's muted text token (#6A706D) reaches 4.43:1 on the canvas, under the 4.5:1 small-text rule, so small text on the site uses `--muted-aa`, a slightly darker mix of the same token; the paper's waiting lines were faded by the scroll story (1.37:1) and are now clipped instead, so hidden text is hidden rather than faint.

# Playwright results, phase 1

`npx playwright test` (site.spec.ts + axe.spec.ts) against `dotnet run --launch-profile http+site`: 17 passed, 0 failed. Covered: the Arabic and English homes (language, direction, title, canonical, hreflang, no inline scripts), `/` still the demo and naming the product, the `/lang/{code}` switch (cookie, redirect, refusal of unknown codes and foreign targets), 301 for bare paths, the site's 404 in both languages, `no-cache` pages and immutable hashed assets, one Content-Security-Policy header, the demo shell with the browser's language, the demo's theme button remembered in the cookie.
