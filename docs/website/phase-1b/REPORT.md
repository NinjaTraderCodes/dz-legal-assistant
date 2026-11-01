# Website, phase 1b: "/" serves the website

Branch `fable/website`, on top of the default branch's phase-1 merge (cebd25d). Built and checked locally on 1 November 2026.

## What changes

- **`/` is the website.** The app serves the home page at the address itself, no redirect, in the visitor's language: the `dzlegal.lang` cookie first, then the browser's Accept-Language (Arabic → Arabic), then English. The response carries `Vary: Cookie, Accept-Language` and `Content-Language`; the page's canonical link points to `/en/` or `/ar/`, so search engines see one copy per language.
- **The demo lives only at `/app`.** `@page "/"` is removed from the demo page. `/app?lang=…`, the loading shell, the language and theme memory and `/healthz` are unchanged.
- **The switch stays.** `Site:ServeRoot` in `appsettings.json` (now `true`). Set to `false`, the demo answers at `/` again: the request is rewritten internally to `/app`, so the submitted address keeps working with a 200 and no redirect. The same fallback applies by itself when the site folder is missing from the image (`/healthz` then reports `"site": false`).
- **Search engines.** The website pages carry no `robots` meta (they never did; the 404 pages and `/app` keep `noindex`); `robots.txt` allows `/` and disallows `/app`, `/lang/`, `/_blazor`, `/_framework/`; the sitemap lists `/ar/` and `/en/` with their hreflang pairs, which is where `/` canonicalises to.
- **Small fix:** `site/package.json` `engines.node` is `>=22.23.4`.
- README: a "Website" link beside the "Live demo" link, which now points to `/app`. `docs/WEB-DEMO.md` describes the switch and the fallback.

## Checks

| Check | Result |
|---|---|
| `dotnet build -c Release` (whole solution) | 0 warnings, 0 errors |
| `tests/DzLegal.Web.Tests` | 68 passed: the three new tests cover the root served by the site, the demo answering the root when the switch is off, and when the folder is missing |
| whole-solution `dotnet test -c Release` | all green: Storage 18, Documents 6, FormEngine 67, Drafting 399, Web 68, Law 116, App 305 (979 in all) |
| Playwright | 19 passed (the two new root tests and the fallback test included), against `dotnet run --launch-profile http+site` on port 5080 |
| Fallback over HTTP | a second instance started with `Site__Root=nowhere` on port 5081: `/` answered 200 with the demo's loading shell (Arabic for an Arabic browser), `/healthz` reported `"site": false`, `/app` answered 200, `/en/` answered 404 |

The Playwright suite gained two tests: `/` in the visitor's language (no header → English with the product's name and the `/en/` canonical; Arabic browser → Arabic; cookie wins over the browser; French browser → English; `Vary` present) and "every demo link on the site points to `/app`" (at least three per language, all `/app?lang=<lang>`; `/app?lang=ar` still answers with `noindex`; `/healthz` reports the site). The fallback test runs against a second instance started with `Site__Root=nowhere` (skipped when `FALLBACK_BASE_URL` is not set).

## Screenshots

`root-<ar|en>-<light|dark>-<WxH>.jpg`: `/` with the language cookie set to `ar` and `en`, light and dark, at 390×844 and 1920×1080, full page.

## Not changed

No Docker rebuild (the Dockerfile is untouched); Lighthouse unchanged (the same pages as phase 1).
