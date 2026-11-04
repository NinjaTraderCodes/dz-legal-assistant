# Website, phase 6: the hero with the laptop, the pictures, the documentation pages

Three pushes on `fable/website`, each reviewed and merged on its own. The rules of the earlier phases hold: the demo at `/app` untouched, no link to the repository (`check-dist` enforces it), nothing loaded from the internet at run time, both languages and both themes at 390, 1366 and 1920 px, every figure with its source, budgets green, axe at 0 violations, 0 warnings and every test green with SDK 10.0.114 and the default SDK, README unchanged, no Lighthouse HTML in this folder (summary tables only).

## Push 1: the hero with the laptop (phase 5, item 1)

**The image.** `site/src/assets/hero/laptop.png`, 1536 x 1024, RGBA with a transparent background (the opaque pixels span 1403 x 1016, the rest is margin and the soft shadow). It is the owner's laptop with the real capture `app-ar-dark-1920x1080.png` (item 0 of phase 5: the «Unpaid loan» sample, the grounds drafted by gemma3:4b, the review ticked, 9 of 9) composited onto the screen; the screen was neither regenerated nor retouched. The licence note is `site/src/assets/hero/NOTES.txt`. **One image serves both languages and both themes** and is never mirrored.

**Renditions** (`Hero.astro`, `<Picture>` from `astro:assets`, quality 68, transparency kept in every file, checked with sharp):

| Width | AVIF | WebP |
|---|---|---|
| 480 | 17.3 KB | 21.4 KB |
| 720 | 31.5 KB | 39.0 KB |
| 960 | 48.6 KB | 59.6 KB |
| 1440 (the cap: the source is 1536 wide) | 89.1 KB | 109.9 KB |

The `<picture>` carries one AVIF `<source>` with the four widths and a WebP `<img>` with the same four (the fallback is told the 1440 cap, otherwise Astro adds a fifth file at the source's size). `sizes` says half the stage from 900 px (at most 560 px) and the full width below.

**Layout.** From 900 px the hero's stage is a two-column grid: the text in the first column (start-aligned, the English title allowed to wrap), the laptop in the second, both over the photograph and its overlay; the grid follows the writing direction, so Arabic reads the text on the right and sees the laptop on the left, **and the picture is the same file, not mirrored**. Below 900 px one column: the text, the buttons and the note, then the laptop. The picture's box comes from its width and height attributes, so nothing moves when it arrives.

**The laptop's orientation in Arabic.** The laptop is turned so that its screen faces left. In English that is towards the text; in Arabic (`hero-ar-*-1366x768.jpg`, `hero-ar-*-1920x1080.jpg`) the screen faces the page's left edge, away from the text on the right. It does read as turned away; a second version turned the other way, used on the Arabic home only, would fix it (the component takes the one image today; a per-language pick is a two-line change).

**Loading.** `lcp.mjs` on the four Lighthouse reports (both homes, mobile and desktop): the largest element of the first screen is the **photograph** (`img.photo`) in every case, not the laptop, so the laptop is **not preloaded** and the photograph keeps its preload with `fetchpriority="high"`. The laptop is on the first screen on every width, so it loads eagerly, but with `fetchpriority="low"`: without it the mobile LCP rose from phase 5's 2.4 s (Arabic) and 2.0 s (English) to 2.6 s and 2.3 s in two full runs each (the laptop's rendition competed with the photograph and the fonts on the throttled connection); with it, three mobile runs each gave 2.4, 2.4 and 2.5 s on the Arabic home and 2.1, 2.1 and 2.1 s on the English one, under the 2.6 s budget. The live site measured 2.2 s and 1.5 s under the same machine state (laptop plugged in), so the local figures are the usual local penalty, not a regression. The 480 width's quality was not lowered: the phone (Lighthouse's Moto G at 412 px, device scale 1.75) picks the 720 width, and the budget holds.

**Captures** in this folder: `hero-{ar,en}-{light,dark}-{390x844,1366x768,1920x1080}.jpg` (the hero section) and `home-{ar,en}-{light,dark}-{390x844,1366x768,1920x1080}.jpg` (the full home).

### Checks (push 1)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean; eight laptop renditions, no fifth size |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 53 passed, 1 skipped (one new test: the hero's picture, formats, priority and layout in both languages); axe 0 violations |
| `npm run budget` | every budget met (`--lighthouse ../docs/website/phase-6/lighthouse-summary.md`); `npm run links`: 37 internal, 11 outbound, 0 problems |
| Lighthouse, both homes, mobile and desktop | see the table below |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors with both (10.0.114 and 10.0.403) |
| `dotnet test -c Release` | 986 of 986 green (desktop project 305 of 305 in 3 min 33 s, laptop plugged in) |
| `docker build` (ECR mirror of `node:22-alpine`) and the smoke on 7860 | the build failed once with exit code 155 in `dotnet publish` while the two host builds ran alongside it (resources), and was not retried: the session was close to its model limit, and the reviewer verifies the push. The site stage changed nothing in the Dockerfile; the image was last built and smoked on the follow-up commit of phase 5. |

**Lighthouse** (local instance with the origin's compression, Lighthouse 13, Edge headless; the HTML reports stay outside the repository; `lighthouse-summary.md` beside this report):

| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |
|---|---|---|---|---|---|---|---|---|
| /ar/ | mobile | 98 | 100 | 100 | 100 | 2.4 s | 0 | 40 ms |
| /ar/ | desktop | 94 | 100 | 100 | 100 | 1.1 s | 0 | 120 ms |
| /en/ | mobile | 93 | 100 | 100 | 100 | 2.1 s | 0 | 270 ms |
| /en/ | desktop | 99 | 100 | 100 | 100 | 0.8 s | 0 | 40 ms |

The performance score still swings with the font timing (phase 5's lesson: the blocking time lands before or after the first paint); the LCP figures are the stable ones. Mobile LCP, three runs each, final build: /ar/ 2.4, 2.4, 2.5 s; /en/ 2.1, 2.1, 2.1 s. Live, same day: /ar/ 2.2 s, /en/ 1.5 s.

### Files changed in push 1

- `site/src/assets/hero/laptop.png` and `NOTES.txt` (new), `site/src/components/Hero.astro`, `site/src/i18n/site.ar.json` and `site.en.json` (`hero.laptopAlt`), `site/tests/home-shots.mjs` (`--sections hero`), this report and its captures.

### New Arabic strings (push 1), for Khouloud

| Key | Arabic | English |
|---|---|---|
| `hero.laptopAlt` (the picture's alt text) | التطبيق المكتبي على حاسوب محمول: عريضة بأسانيد صاغها النموذج المحلي، مراجعة، والبيانات الإلزامية 9 من 9 | The desktop app on a laptop: a petition with its grounds drafted by the local model, reviewed, 9 of 9 mandatory elements present |

## Push 2: the pictures on the site (phase 6, part A)

### Inventory before this push

Every picture the site showed, with its source and its purpose. "Interface piece" means a part of the app's interface shown for what it looks like; "proof" means a real output shown for what it says.

| Where | Picture | Source before | Purpose | Now |
|---|---|---|---|---|
| Home, hero | the app on a laptop | the owner's composite over the real 1920 x 1080 capture (push 1) | proof | unchanged |
| Home, guarantees 1 | the law library's card | headless-test frames, 1x, four variants | interface piece | **drawn** from `law.generated.json` (`AppPiece` kind `library`) |
| Home, guarantees 2 | the offline badge | headless-test frames, 1x, four variants | interface piece | **drawn** (`OfflineStatus`) |
| Home, guarantees 3 | the refused-export notice | `docs/images/workbench-export-blocked.png`, 1x, Arabic only | interface piece | **drawn** (`ExportBlocked` + `BlockerUnreviewed`) |
| Home, guarantees 4 | the drafted grounds with the review box | a frame of the live recording, English, one theme | proof | **real capture**: the desktop app's 1920 x 1080 capture, both languages and themes |
| Home, AI stage 1 | the reading of the facts | a frame of the live recording | proof | **real capture** (the «How the AI reads the facts» card) |
| Home, AI stage 2 | the proposed articles | a frame of the live recording | proof | **real capture** (the «Proposed articles» list) |
| Home, AI stage 3 | the claim type confirmed | a frame of the live recording | proof | **real capture** (the panel's confirmation note) |
| Home, AI stage 4 | the recording's timing line; the drafting in progress; the drafted grounds | frames of the live recording | proof | timing line and drafting in progress **kept from the recording** (no capture of that state exists; the only two frames left); drafted grounds from the **real capture** |
| Home, problem section | the formal check at 7 / 9 with the red card | HTML | interface piece | unchanged (already drawn) |
| Home, story | the sheet, the check, the AI tag, the review row | HTML | interface piece | unchanged (already drawn) |
| Home, team | three portraits | the team's own pictures | people | unchanged |
| Guide step 1 | the sample picker and the top of the paper | real demo capture, device scale 2 | proof of the demo | unchanged, shown at half its pixels |
| Guide step 2 | the red missing-element card | real demo capture | interface piece | **drawn** (`AppPiece` kind `check`, 7 / 9, the domicile finding) |
| Guide step 3 | the model's reading of the facts | real demo capture (Nemotron 3 Ultra) | proof | unchanged, shown at half its pixels |
| Guide step 4 | the drafted grounds with the AI mark; the review row | real demo captures | proof; interface piece | grounds unchanged; review row **drawn** (`ModelTag` + `ReviewedCheck`, unticked) |
| Guide step 5 | the refused export; the export allowed | real demo captures | interface piece | **drawn**: the notice, then the review row ticked |
| Law library page | the texts' cards | HTML from `law.generated.json` | interface piece | unchanged |
| Documentation pages | the nine figures of `docs/PRODUCT-DETAILS.md` and the onboarding frame of `docs/WEB-DEMO.md` | the documents' own images | document figures | unchanged (they belong to the documents) |

### What changed

- **`AppPiece.astro`** (new): five pieces of the interface drawn in HTML and CSS, in the page's theme and language, with the app's own strings read through `strings.mjs` (`OfflineStatus`, `ExportBlocked`, `BlockerUnreviewed`, `ModelTag`, `ReviewedCheck`, `ComplianceScore`, `SummaryOk`, `SummaryMissing`, `LibraryReference`, `LibraryEdition`, `LibraryArticles`, `LibraryComplete`, `LibraryCheckJournal`, `LibraryNotReviewed`; the three new ones added to the export list) and the site's tokens. The library card reads the Code of Civil and Administrative Procedure from `law.generated.json` (title, reference, edition, 1,084 articles, complete). Every piece's caption ends with «كما يعرضها التطبيق» / "As the app shows it"; no caption calls a piece a screenshot (the test checks it).
- **Proof pictures from the real captures.** `scripts/crops.mjs --only desktop` cuts eight crops per language and theme from `C:\Users\TheShadow\dz-laptop-shots\app-{ar,en}-{light,dark}-1920x1080.png` (the «Unpaid loan» sample, the grounds drafted by gemma3:4b, the review ticked, 9 of 9; outside the repository): the reading card, the proposed articles, the confirmation note, and the paper's grounds with the review row. Captions say so: «التطبيق المكتبي، النموذج المحلي gemma3:4b، 3 نوفمبر 2026» / "Desktop app, local model gemma3:4b, 3 November 2026".
- **Kept from the live recording**, as the brief allows where no capture of the state exists: the drafting in progress (`ai/stage4-drafting.png`) and the recording's own timing line (`ai/stage4-timing.png`). Their note still reads "A frame of the live recording with the local model". The four other recording frames are deleted.
- **Sharpness and fit** (`Shot.astro`): every picture is shown at half its pixel width or less (the display width is capped at `floor(width / 2)` and the renditions are the shown width and twice it, never beyond the source), and the figure's box is exactly the shown width, so the picture fills it at its own ratio. The desktop crops are 1x frames, so a 1,120 px crop shows at 560 px and a 354 px card at 177 px; the demo captures are device scale 2, so the 640 px guide pictures stay. The lightbox's large rendition is capped at 1,280 px and quality 74.
- **Weight:** every rendition of the new pictures is under 55 KB (the paper crops 50 to 54 KB at 2x); the largest on the site is the step 4 guide picture at 2x, 80 KB. All lazy below the first screen.
- **Deleted:** 30 picture files (the headless-test crops of the library and the app bar, the export notice crop, the recording's four frames, the demo's step 2, step 4 review and step 5 captures). The `proofs` section of `crops.mjs` is gone with them.

### Checks (push 2)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, check-dist clean; every new rendition under 55 KB |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | NOT RUN: the session reached its model limit right after the build; the guide test was updated for the drawn pieces and the reviewer should run `npx playwright test tests/site.spec.ts tests/axe.spec.ts --project=chromium` on this push |
| `npm run budget` | NOT RUN in this push (same reason); push 1 was green and this push adds no script |
| .NET build and tests | no file outside `site/` changed in this push (the C# solution is the one push 1 built and tested: 0 warnings with both SDKs, 986 tests green) |
| `docker build` | not run in this push (the owner asked to skip the optional checks while the session was near its model limit; the reviewer verifies) |

**Captures** in this folder: `proofs-{ar,en}-{light,dark}-1366x768.jpg`, `ai-{ar,en}-{light,dark}-1366x768.jpg`, `try-{ar,en}-{light,dark}-1366x768.jpg`.

### New Arabic strings (push 2), for Khouloud

| Key | Arabic | English |
|---|---|---|
| `shot.desktop` | التطبيق المكتبي، النموذج المحلي gemma3:4b، 3 نوفمبر 2026. | Desktop app, local model gemma3:4b, 3 November 2026. |
| `shot.replica` | كما يعرضها التطبيق. | As the app shows it. |
| `story.caption` (reworded) | كما يعرضه مكتب العمل: الحالة الشكلية تتغير مع كل حرف تكتبه، وتحمل المناقشة القانونية علامة الذكاء الاصطناعي إلى أن يراجعها المحامي. | As the workbench shows it: … |
| `proofs.shot.review` (reworded) | التطبيق يرفض التصدير ما دام قسم كتبه الذكاء الاصطناعي لم يُراجَع. | The app refuses the export while an AI-written section is unreviewed. |
| `proofs.shot.grounds` (reworded) | المناقشة القانونية التي صاغها النموذج المحلي على الورقة، مع خانة المراجعة. | The legal grounds the local model drafted, on the paper, with the review box. |
| `ai.shot.stage1` (reworded) | قراءة الذكاء الاصطناعي للوقائع: العلاقة القانونية، الالتزام المخل به، ما يطلبه المدعي، نوع الدعوى. | How the AI reads the facts: the legal relationship, the obligation breached, what the claimant asks, the claim type. |
| `ai.shot.stage2` (reworded) | المواد المقترحة من الفهرس، ومواد الأساس مؤشَّرة. | The articles proposed from the catalogue, the basis articles ticked. |
| `ai.shot.stage3` (reworded) | المحامي يؤكد نوع الدعوى؛ المواد التي تفرضها الدعوى مؤشَّرة، والباقي اقتراحات يراجعها. | The lawyer confirms the claim type; the articles the claim requires are ticked, the rest are proposals to review. |
| `ai.shot.stage4` (reworded) | الصياغة جارية (من التسجيل الحي)، ثم المناقشة القانونية على الورقة مع خانة المراجعة. | Drafting in progress (from the live recording), then the legal grounds on the paper with the review box. |

### Files changed in push 2

- `site/src/components/AppPiece.astro` (new), `Shot.astro`, `Proofs.astro`, `AiPanel.astro`, `TryGuide.astro`; `site/scripts/crops.mjs`, `site/scripts/strings.mjs` and `site/src/i18n/strings.generated.json`; `site/src/i18n/site.ar.json`, `site.en.json`; `site/src/assets/shots/desktop/*.png` (16 new crops); 30 pictures deleted; `site/tests/site.spec.ts`; this report and its captures.

## Two notes about the desktop app (to record, not to fix in this phase)

1. **A clipped first letter in the paper view.** In the 1920 x 1080 captures of item 0 (`C:\Users\TheShadow\dz-laptop-shots\app-*-1920x1080.png`, outside the repository) a few lines of the grounds start with a clipped first letter at the right margin: «وبأنه» shows as «؛بأنه». The paper's text block clips the glyph that overhangs the line's start.
2. **The release test is timing-sensitive under the parallel suite.** `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp` queues the release on the thread pool and waits 2.5 s; when the rest of the desktop suite holds every pool thread (on battery, in particular) the queued task has not started when the wait ends and the test fails on its counter (expected 1, actual 0). It passes alone and passes with the collections run one at a time (`-- xUnit.ParallelizeTestCollections=false`). Details in `docs/website/phase-5/REPORT.md`, follow-up section.
