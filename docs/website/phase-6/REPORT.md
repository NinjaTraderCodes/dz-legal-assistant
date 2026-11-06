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
| `hero.laptopAlt` (the picture's alt text) | التطبيق المكتبي على حاسوب محمول: عريضة بأسانيد صاغها النموذج المحلي، مراجعة، والبيانات الإلزامية 9 من 9 | The desktop app on a laptop: a statement of claim with its grounds drafted by the local model, reviewed, 9 of 9 mandatory elements present |

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

## Push 2, second pass: readable proof pictures, no empty space, the dark-theme contrast

The reviewer's five points on `5b0ec4e`, in one commit after merging the main branch (`28409d7`).

### The capture run

The proof pictures of the home page now come from **one deliberate run** of the desktop app at scale 2, made for this purpose; nothing from earlier runs is used.

| | |
|---|---|
| Tool | a scratch console app outside the repository (`C:\Users\TheShadow\dz-shot2x`) built on the video recorder's pattern: the app's own `MainWindow` and view model, rendered by Avalonia's headless platform with `SetRenderScaling(2)`, so the 1920 x 1080 layout comes out at 3840 x 2160 |
| Started | 4 November 2026, 20:58:08 (+01:00); record written 21:03:35 |
| Model | gemma3:4b through Ollama on `127.0.0.1:11434`, the app's local engine (`sendsCaseData: false`); the tool refuses to start when any of `OPENROUTER_API_KEY`, `GOOGLE_API_KEY`, `GROQ_API_KEY`, `DEEPINFRA_API_KEY` is set, and it ran with all four unset |
| Case | the fictitious «Unpaid loan» sample; analysis 183 s, claim type confirmed, drafting 127 s (on battery), review ticked |
| Frames | `app-{en,ar}-{light,dark}.png`, the paper scrolled to the drafted grounds |
| Text check | for each of the four frames the tool read the paper's own rendered text for the grounds section and compared it with the section's text it dumped: **match in all four** (`renderedMatchesDump`); the dumped grounds are kept in `site/src/content/desktop-run.json` |

The tool's process stayed open after writing its record; I stopped it by its PID (21908) six minutes later. The outputs of two earlier runs (20:24, and one between 20:29 and 20:31 that the stopped command still started, from a build without the scroll fix) were moved aside to `C:\Users\TheShadow\dz-laptop-shots\stale-205534\` and are not used.

### What changed

1. **The video's caption band and progress frame are gone.** The drafting stage shows a drawn activity panel (`AppPiece` kind `activity`): the section's title, the engine «النموذج المحلي (gemma3:4b)» / "Local model (gemma3:4b)", a progress bar, «ما يكتبه الذكاء الاصطناعي الآن» / "What the AI is writing now" over the opening words of the model's first application from the same run, and `DraftFirstRun`. All strings are the app's own (`ProviderOllama`, `ActivityLive`, `DraftFirstRun`, exported by `strings.mjs`); caption «الصياغة جارية: كلمات النموذج تظهر كما يكتبها. كما يعرضها التطبيق.». The last two recording frames (`ai/stage4-drafting.png`, `ai/stage4-timing.png`) are deleted, with the `ai` section of `crops.mjs` and the caption `shot.live`. No picture on the site comes from the video any more.
2. **Readable pictures, from bigger crops.** Every crop is cut from the scale-2 frames, so each is twice the size it is shown at:

   | Picture | Crop (px) | Shown at (CSS px, 1366 screen) |
   |---|---|---|
   | AI stage 1, the model's reading of the facts | 710 x 357 (en), 713 x 392 (ar) | the card's full width, about 350: the app's own size |
   | AI stage 2, the proposed articles | 710 x 315 (en), 713 x 197 (ar) | the card's full width |
   | AI stage 3, the panel's "you decide" statement and the Analyse button | 710 x 258 (en), 700 x 272 (ar) | the card's full width |
   | AI stage 4 and the grounds guarantee, the drafted grounds | 1378 x 1030 (en), 1374 x 1020 (ar) | 560 |
   | Guide step 1, the nine steps / the formal check at 8 / 9 | 760 x 616 / 640 x 562 (en), 640 x 484 (ar) | 380 / 320 |
   | Guide step 3, the model's reading (demo, Nemotron) | 744 x 732 | 372 |
   | Guide step 4, the drafted grounds (demo, Nemotron) | 1032 x 807 | 516 |

   Guide step 1 used to be the whole demo window shown at about 320 px; it is now two parts at the app's own size, captured again from the demo at device scale 2 with no model call (`guide-shots.mjs --steps 1 --mode template`, the two clips around `#case-title` + `.form-panel .steps` and `#check-title` + `.check-panel .score-card`). The step 4 picture ended on a half-cut line; `guide-shots.mjs` now cuts a paper crop back to its last whole line (`trimToLastLine`, applied to the existing captures with `--retrim step4-grounds`, since capturing them again needs a paid model call). The AI stage 3 crop leaves out the confirmation note under the button: the app shows it in English in the Arabic interface (note 3 below).
3. **No empty space.**
   - The drafted grounds are cut inside the white sheet: no grey canvas, no margin wider than the text's own.
   - The "Works offline" card draws the app bar with its badge and the local model's status line «النموذج المحلي (gemma3:4b): جاهز» / "Local model (gemma3:4b): ready" (`AppTitle`, `OfflineStatus`, `ModelReady`, `ProviderOllama`).
   - The "Review before export" card shows the section's unticked review row above the refused-export notice; the "Drafting the legal grounds" card shows the ticked row under the paper. The section's title in both is the definition's own, «المناقشة القانونية» / "Legal grounds".
   - The guarantee cards and the AI stage cards size to their content (`align-items: start`), so no card keeps an empty band; the drafted-grounds figure no longer stretches to its grid row.
4. **Accessibility.** `.chip-plain` uses the frame's surface with a hairline border (4.5:1 or more in both themes); `axe.spec.ts` passes on `/en/` in dark again. The ticked box's check mark keeps the surface colour inside list cards (a global `.list li .icon` rule made it green on green).
5. **Weight.** Every new rendition is lazy WebP. The drafted-grounds picture is encoded at quality 50 (dense text keeps sharp at twice its size), which brings its 2x rendition to 82 to 84 KB, the 1x one to about 33 KB; every other picture rendition of these sections is under 55 KB. The fallback `src` of each picture is now its largest rendition, so no file at the crop's full size is built. The only rendition over 80 KB elsewhere on the site is the documentation figure `main-window-en` (90 KB, phase 5, unchanged).

Captions name the source: «التطبيق المكتبي، 4 نوفمبر 2026، النموذج المحلي gemma3:4b.» / "Desktop app, 4 November 2026, local model gemma3:4b." (the date before the model's Latin name keeps the Arabic order whole). The demo's captions now give the date of the runs that made them, 4 November 2026 (they said 3 November; the pictures were taken again on 4 November in phase 5's follow-up).

**Captures** in this folder, 1366 px, both languages and themes, each looked at for readability and empty space: `proofs-*`, `ai-*`, `try-*`.

### Checks (push 2, second pass)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 53 passed, 1 skipped; axe 0 violations (dark `/en/` included) |
| `npm run budget` | every budget met |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors with both (10.0.114 and 10.0.403) |
| `dotnet test -c Release` | the six other projects green (681 of 681); the desktop project 304 of 305 in the parallel run on battery (the timing-sensitive release test, note 2 below), 305 of 305 with the collections run one at a time |

### New Arabic strings (push 2, second pass), for Khouloud

| Key | Arabic | English |
|---|---|---|
| `shot.desktop` (reworded) | التطبيق المكتبي، 4 نوفمبر 2026، النموذج المحلي gemma3:4b. | Desktop app, 4 November 2026, local model gemma3:4b. |
| `ai.shot.activity` (new) | الصياغة جارية: كلمات النموذج تظهر كما يكتبها. | Drafting in progress: the model's words appear as it writes them. |
| `ai.shot.stage3` (reworded) | القرار للمحامي: يؤكد نوع الدعوى، ولا تُذكر في العريضة إلا المواد التي يؤشّر عليها. | The decision stays with the lawyer: they confirm the claim type, and only the articles they tick are cited. |
| `ai.shot.stage4` (reworded) | المناقشة القانونية التي صاغها النموذج المحلي، على الورقة. | The legal grounds the local model drafted, on the paper. |
| `proofs.shot.grounds` (reworded) | المناقشة القانونية التي صاغها النموذج المحلي، على الورقة. | The legal grounds the local model drafted, on the paper. |
| `try.shot.1` (reworded) | بعد فتح المثال: الخطوات التسع، والمراجعة الشكلية عند 8 من 9. | After opening the sample: the nine steps, and the formal check at 8 / 9. |
| `try.shot.real` (reworded, date corrected) | لقطة من العرض التجريبي، 4 نوفمبر 2026، مع نموذجه المستضاف (Nemotron 3 Ultra عبر OpenRouter). | From the demo, 4 November 2026, with its hosted model (Nemotron 3 Ultra through OpenRouter). |

The drawn pieces use only the app's existing strings (no new wording); `shot.live` was removed.

### Files changed in push 2, second pass

- `site/src/components/AppPiece.astro`, `Shot.astro`, `Proofs.astro`, `AiPanel.astro`, `TryGuide.astro`; `site/src/content/steps.ts` (`splitGrounds`, `groundsTitle`) and `site/src/content/desktop-run.json` (new); `site/scripts/crops.mjs`, `site/scripts/strings.mjs`, `site/src/i18n/strings.generated.json`, `site.ar.json`, `site.en.json`; `site/tests/guide-shots.mjs`, `site/tests/site.spec.ts`; `site/src/assets/shots/desktop/*.png` (16 crops re-cut), `site/src/assets/shots/guide/step1-steps-*`, `step1-check-*` (new), `step4-grounds-*` (trimmed); deleted: `ai/stage4-drafting.png`, `ai/stage4-timing.png`, `guide/step1-sample-*`; this report and its captures.
- The merge of the main branch (`28409d7`) brought only `docs/LAW-LIBRARY.md` and `docs/WEB-DEMO.md`.

## Push 3: the documentation pages (phase 6, part B)

The six documents were too long for a visitor. Each site page now opens with a short summary in the page's language, and the full document follows, folded. The files in `docs/` are unchanged.

### What was built

- **A summary at the top of each page,** in Arabic on `/ar/docs/…` and in English on `/en/docs/…`: 8 points and one line «ما يعنيه هذا للمحامي» / "What this means for a lawyer". One file per document and language, `site/src/content/docs-summaries/<slug>.<lang>.md` (the points as a Markdown list, the line in the front matter), read by a second content collection (`docSummaries` in `site/src/content.config.ts`, which requires the line). A missing file fails the build.
- **The full document folded under it,** in a native `<details>` closed by default, its summary «قراءة الوثيقة كاملة» / "Read the full document". The document stays English, and on the Arabic page the existing note «هذه الوثيقة متاحة بالإنجليزية فقط.» now sits inside the fold, above the document it describes.
- **The table of contents covers both parts:** «الخلاصة» / "Summary", the line for a lawyer, then "Read the full document" with the document's own headings nested under it (in English, left to right).
- **The fold opens on demand.** A click on a heading in the contents, a link from another page, or arriving with `#anchor` opens the fold and scrolls to the heading (`public/ui.js`, `unfold`: on load, on `hashchange` and on a click on an in-page link). Without the script the fold opens with a click on its summary line, as any `<details>`.
- **On a phone** the summary comes first, then the contents, then the fold, so the points are what a visitor reads first.
- **The Arabic page is described in Arabic:** its meta description is the summary's line for a lawyer (the English page keeps the document's first paragraph).

### Faithful to the documents

Every point is taken from a sentence of its document; no point adds a claim or a figure. The summaries were checked again against the two documents corrected on the main branch (`28409d7`): the law library summary already gave the 1,361 articles of the table at the top of `LAW-LIBRARY.md`, which the corrected "Status" section now repeats; in the web demo summary, the point about storage now follows the corrected "Limits" line (Open and Save use a draft file on the visitor's own disk; nothing is saved on the server).

### Where each point comes from

Every point is a sentence of its document, shortened or merged with the sentence beside it; no point adds a claim or a figure. The Arabic and English summaries carry the same points in the same order. Line numbers are those of the file in `docs/` at `28409d7`.

**`docs/OFFLINE.md`**

| # | Point | From |
|---|---|---|
| 1 | No case data leaves the lawyer's computer; client data is covered by professional secrecy | introduction (3) |
| 2 | Form, templates, storage, interface, template mode, grounds suggestions, library and search use no network | "What the application does on the network", table rows (13, 14, 16) |
| 3 | The local model only at 127.0.0.1:11434; packets never leave the machine | same table, "Local model engine" row (15) |
| 4 | Any other address refused without a DNS lookup; no proxy, no redirect; "cloud" models refused | "How it is enforced in code" (22 to 24) |
| 5 | No telemetry, analytics, crash reporting or update check | table, last row (18); "How it is enforced in code" (26) |
| 6 | Official-source links open only on a click; fixed data; only joradp.dz and mjustice.dz | table, "Official source" row (17) |
| 7 | Drafts saved where the lawyer chooses, local app-data folder by default, nothing synchronised | "Where data is stored" (38, 41) |
| 8 | The web demo is a separate program with a hosted model, for fictitious cases only | the note under the introduction (5) |
| Lawyer | Draft with the network unplugged; the facts stay on the computer, as professional secrecy requires | introduction (3); "Verify it yourself" (31 to 33) |

**`docs/HIDDEN-SET.md`**

| # | Point | From |
|---|---|---|
| 1 | It measures the fact reading on cases its developers have never seen | "Purpose" (3) |
| 2 | The 61 development cases give an optimistic score; the hidden set is the honest measure | introduction (5) |
| 3 | Written by someone who has not worked on the reader; may not read the prompts, cases or results | "Rules" 1 to 3 (9 to 18) |
| 4 | Never in the repository, never sent to the developers; only the summary is shared | "Rules" 4 and 6 (19, 23) |
| 5 | About 100 fictitious cases in formal Arabic: 4 or 5 per claim type, 10 outside the catalogue | "Rules" 5 (20); "Content" (27 to 31) |
| 6 | The cases read like a lawyer's notes | "Content" (33 to 42) |
| 7 | The three runs: 32 of 90; 64 of 90; 60 of 90, claim type 85 of 90, 10 of 10 declined | "Runs", table (123 to 125) |
| 8 | Once looked at, a set is no longer hidden; the next measure needs a new set | "Rules" 7 (25); "Runs" (129) |
| Lawyer | How often the reading is fully right on unseen cases: the honest measure, not the optimistic one | "Purpose" (3); introduction (5) |

**`docs/PRODUCT-DETAILS.md`**

| # | Point | From |
|---|---|---|
| 1 | Formal Arabic, the language of the statement of claim (CPCA art. 8); Darija or French notes accepted, never refused or cut | introduction (5 to 7) |
| 2 | 1,361 articles: Family Code and CPCA complete, 33 Civil and Commercial, 17 labour; none of these checks is a lawyer's review | "Law texts" (16 to 21) |
| 3 | Identification, court, facts, dates, signature and requests from the form and the lawyer's lines; the model writes only the grounds | "Principles", "Grounded, not free-generating" (29) |
| 4 | Reading, candidates without a model, the model's choice; nothing ticked before the claim type is confirmed | "Legal grounds: analysis of the facts" (39 to 41, 46) |
| 5 | The code writes each article and its rule, the AI how the facts meet it; a citing or unknown-number answer asked again, then «(...)», which blocks export | "How the draft stays grounded" (54) |
| 6 | Each AI section carries the review line; PDF, Word and copy refused until ticked and no «(...)»; editing is not reviewing | "Principles", "Assistive, never autonomous" (28) |
| 7 | Analysis 2 to 3 minutes on a CPU-only laptop (117 to 181 s on an i5-1335U); template mode without a model | "Legal grounds: analysis of the facts" (38); "Quick start" (103) |
| 8 | The claim catalogue is not yet verified by a lawyer; an audit found errors; corrections under way | the status note (59) |
| Lawyer | Text for the lawyer to edit and sign; nothing AI-written leaves unreviewed | "Principles", "Assistive, never autonomous" (28) |

**`docs/ARCHITECTURE.md`**

| # | Point | From |
|---|---|---|
| 1 | Desktop and demo share everything below the shell; only the shell and provider differ; the desktop never references the hosted providers | "Modules and dependencies" (24) |
| 2 | The formal check is one pure function: no I/O, no AI, no clock; runs on every keystroke | same section, FormEngine (27) |
| 3 | Identification from the form through templates; the model writes only narrative sections, which limits invention | same section, Drafting (29); "Key decisions", "Generated vs narrative sections" (98) |
| 4 | Each facts line becomes a «حيث إن» paragraph; small models reversed lender and borrower | "Data flow" step 0 (65) |
| 5 | The model's text is checked: foreign script, unknown numbers, dropped numbers, stray Latin words, mostly non-Arabic text | "NarrativeChecks" (35 to 40) |
| 6 | The checks cannot detect errors of meaning such as swapped roles; those remain the lawyer's review | same item (44) |
| 7 | Each element: its article, a severity, a check from a small closed set; anything new is a new kind with tests | "The rule model" (74 to 91) |
| 8 | Deterministic before AI: required articles from the catalogue, quotations checked against the text | "Key decisions", "Deterministic before AI" (104) |
| Lawyer | What must be exact comes from the form; the model writes, it does not decide the law | "Key decisions" (98, 104) |

**`docs/LAW-LIBRARY.md`**

| # | Point | From |
|---|---|---|
| 1 | Articles quoted only from the local official texts, never from a model's memory; a 4B model cannot recall them reliably | introduction (3) |
| 2 | 1,361 articles: Family Code 227 and CPCA 1,084 complete, 33 Civil and Commercial, 17 labour | the table at the top (5 to 11) |
| 3 | Each text says how it was checked: Official Journal page, letter by letter against the PDF, or page images read twice | the table's "Proof" column (9 to 11); the `check` paragraph (21) |
| 4 | No article marked verified until a lawyer has checked it | introduction (23) |
| 5 | Nothing scraped, searched online or read from PDFs while the lawyer works; the analysis starts at the click | "How the grounds analysis works" (27) |
| 6 | Under each suggestion: reference, place in the code, link, "not verified by a lawyer" tag | "Provenance shown under each suggested article" (129 to 133) |
| 7 | No recognised claim type, no proposal; a small model does not use this way out, so the lawyer judges the claim type | step 2 (42); "Conclusion and decision" (116) |
| 8 | The app never downloads anything; importing is a maintainer's task; the app reads offline | "Rules" 4 (140) |
| Lawyer | Every proposed article is quoted from an official text with its reference and link; none yet checked by a lawyer | introduction (3, 23); "Provenance" (129 to 133) |

**`docs/WEB-DEMO.md`**

| # | Point | From |
|---|---|---|
| 1 | An offline desktop app; the demo lets anyone use the real product from a link | introduction (5, 6) |
| 2 | The same engine, library and screen logic; only where the model runs differs | introduction (7, 8) |
| 3 | Desktop Gemma 3 4B local; demo Nemotron 3 Ultra on OpenRouter, then Gemma 4 26B, then Groq | the comparison table (13) |
| 4 | The demo sends the case to the hosted model: fictitious cases only | the comparison table (14) |
| 5 | Nothing saved on the server; Open and Save use a file on the visitor's disk; otherwise a recovery copy in the browser | "Limits, stated plainly" (195, the passage corrected on the main branch); "How it is built" (94) |
| 6 | Calls limited per visitor and per address; one shared queue, two at a time | "How it is built", the diagram (77, 78) |
| 7 | Failures never end a session; the next engine answers; the visitor sees a plain message | "How it is built" (89 to 92) |
| 8 | Even the larger model makes mistakes, so the checks matter: required articles always proposed, the program writes each rule | "Limits, stated plainly", AI quality (187 to 189) |
| Lawyer | Invented cases only in the demo; real files in the desktop app, where nothing leaves the computer | the comparison table (14); introduction (5) |

### Checks (push 3)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean (no repository link) |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 54 passed, 1 skipped; axe 0 violations (both product-details pages and `/en/docs/architecture/` are in the axe list) |
| New and updated tests | the documents test checks, on all twelve pages, 5 to 8 points in the page's language, the line for a lawyer, the closed fold and the English article, and opens the fold before following the sibling link; a new test opens the fold from a heading in the contents (Arabic and English) and on arriving with `#how-it-is-enforced-in-code` |
| `npm run budget` | every budget met; the heaviest page, `/ar/docs/product-details/`, is 23.6 KB of HTML (gzip), under 30 KB |
| `npm run links` | 37 internal, 11 outbound, 0 problems |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors with both (10.0.114 and 10.0.403); no file outside `site/` changed |
| `dotnet test -c Release` | 986 of 986 green, the desktop project 305 of 305 in the parallel run this time |

**Captures** in this folder: `docs-{ar,en}-{light,dark}-{390x844,1366x768,1920x1080}.jpg` (the product-details page, fold closed) and `docs-open-{ar,en}-{light,dark}-1366x768.jpg` (after a click on "Principles" in the contents: the fold open, the page at the heading).

### New Arabic strings (push 3), for Khouloud

| Key | Arabic | English |
|---|---|---|
| `docs.summary` | الخلاصة | Summary |
| `docs.lawyer` | ما يعنيه هذا للمحامي | What this means for a lawyer |
| `docs.readFull` | قراءة الوثيقة كاملة | Read the full document |

The summaries themselves, every Arabic line, beside the English one:

**`docs/PRODUCT-DETAILS.md`** (`site/src/content/docs-summaries/product-details.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | اللغة المدعومة هي العربية الفصحى، لغة العريضة نفسها (المادة 8 من قانون الإجراءات المدنية والإدارية)؛ وتُقبل الملاحظات المكتوبة بالدارجة أو بالفرنسية في معظمها، فلا تُرفض ولا تُقتطع. | The supported input is formal Arabic, the language the statement of claim must be in (CPCA art. 8); notes in Darija or mostly in French are still accepted, never refused or cut. |
| 2 | حُمّلت 1,361 مادة: قانون الأسرة كاملا، وقانون الإجراءات المدنية والإدارية كاملا مع تعديل 2022، و33 مادة من القانونين المدني والتجاري، و17 مادة من قانون العمل؛ ولا يُعدّ أي من هذه الفحوص مراجعةً من محام. | 1,361 articles are loaded: the complete Family Code, the complete CPCA with its 2022 amendment, 33 articles of the Civil and Commercial Codes and 17 labour-law articles; none of these checks is a lawyer's review. |
| 3 | الهوية والجهة القضائية والوقائع والتواريخ وخانة التوقيع والطلبات تأتي من الاستمارة ومن سطورك أنت؛ ولا يكتب النموذج إلا المناقشة القانونية. | The identification, the court, the facts, the dates, the signature block and the requests come from the form and your own lines; the model writes only the legal grounds. |
| 4 | يقرأ التحليل الوقائع، ثم يجمع المواد المرشحة من الفهرس دون أي نموذج، ثم يترك للنموذج الاختيار من بينها؛ ولا يُؤشَّر على أي شيء قبل أن تؤكد نوع الدعوى. | The analysis reads the facts, gathers the candidate articles from the catalogue without any model, and lets the model choose among them; nothing is ticked until you confirm the claim type. |
| 5 | في المناقشة القانونية، تكتب البرمجة كل مادة وقاعدتها في سطر واحد، ولا يكتب الذكاء الاصطناعي إلا كيف تنطبق الوقائع عليها؛ والجواب الذي يذكر مادة أو يحمل رقما لا يرد في ملاحظاتك يُطلب مرة أخرى، ثم يُترك «(...)»، وهو ما يمنع التصدير. | In the grounds, the code writes each article and its one-line rule; the AI writes only how the facts meet it, and an answer that cites an article or has a number found nowhere in your notes is asked again, then left as «(...)», which blocks export. |
| 6 | كل قسم كتبه الذكاء الاصطناعي يحمل «راجعت هذا القسم وأتحمل مضمونه»؛ ويُرفض التصدير إلى PDF وWord والنسخ حتى يُؤشَّر على كل قسم منها ولا يبقى أي «(...)»، وتعديل القسم لا يُعدّ مراجعة له. | Each AI-written section carries «راجعت هذا القسم وأتحمل مضمونه»; PDF, Word and copy are refused until every one is ticked and no «(...)» remains, and editing a section does not count as reviewing it. |
| 7 | يستغرق التحليل نحو دقيقتين إلى ثلاث على حاسوب محمول دون بطاقة رسومات (117 إلى 181 ثانية قيست على معالج Intel i5-1335U)؛ ودون أي نموذج مثبّت، يبني وضع القوالب المسودة من القوالب ومن ملاحظاتك. | The analysis takes about 2 to 3 minutes on a laptop without a graphics card (117 to 181 s measured on an Intel i5-1335U); with no model installed, the template mode builds the draft from the templates and your notes. |
| 8 | فهرس الدعاوى (أنواعها ومواده وما هو إلزامي منها) لم يتحقق منه محام بعد: وجدت فيه مراجعة مستقلة أخطاء، والتصحيحات جارية. | The claim catalogue (its claim types, articles and required flags) is not yet verified by a lawyer: an independent audit found errors in it, and corrections are under way. |
| «ما يعنيه هذا للمحامي» | لا ينتج التطبيق إلا نصا تعدّله أنت وتوقّعه، ولا يغادره شيء كتبه الذكاء الاصطناعي قبل أن تراجعه. | The app only produces text for you to edit and sign, and nothing the AI wrote leaves it until you have reviewed it. |

**`docs/ARCHITECTURE.md`** (`site/src/content/docs-summaries/architecture.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | يتقاسم التطبيق المكتبي والعرض التجريبي كل ما تحت واجهتيهما؛ ولا يختلفان إلا في الواجهة ومزوّد النموذج، والتطبيق المكتبي لا يرتبط أبدا بالمزوّدين المستضافين. | The desktop app and the web demo share everything below their shells; only the shell and the model provider differ, and the desktop never references the hosted providers. |
| 2 | المراجعة الشكلية دالة واحدة خالصة، بلا قراءة أو كتابة ولا ذكاء اصطناعي ولا ساعة: نتيجتها ثابتة، وهي سريعة بما يكفي لتعمل مع كل حرف تكتبه. | The formal check is a single pure function with no input or output, no AI and no clock: deterministic, and fast enough to run on every keystroke. |
| 3 | بيانات الهوية الحاسمة قانونا تأتي من الاستمارة مباشرة عبر القوالب؛ ولا يكتب النموذج إلا الأقسام السردية، وهو ما يحدّ من الاختلاق. | Legally critical identification data comes straight from the form, through templates; the model writes only the narrative sections, which limits invention. |
| 4 | يصبح كل سطر من ملاحظات وقائعك فقرة «حيث إن»: فالنماذج الصغيرة عكست من أقرض ومن اقترض حين أعادت كتابة الوقائع. | Each line of your facts notes becomes a «حيث إن» paragraph: small models reversed who lent and who borrowed when they rewrote facts. |
| 5 | يُفحص نص النموذج مقارنةً بمعطياتك: حروف من خط أجنبي، وأرقام لا ترد في أي مُدخل، وأرقام من ملاحظاتك أسقطها، وكلمات لاتينية دخيلة، ونص معظمه بغير العربية. | The model's text is checked against your material: foreign script, numbers found in no input, numbers from your notes that it dropped, stray Latin words, text mostly not in Arabic. |
| 6 | لا تكشف هذه الفحوص أخطاء المعنى، كتبادل أدوار الأطراف: تلك تبقى من مراجعة المحامي. | These checks cannot detect errors of meaning, such as swapped roles: those remain the lawyer's review. |
| 7 | يحمل كل بيان إلزامي مادته ودرجة خطورته وفحصا من مجموعة صغيرة مغلقة؛ وكل جديد يجب أن يكون نوعا جديدا له اختباراته، لا منطقا مخفيا في الواجهة. | Each mandatory element carries its article, a severity and a check from a small closed set; anything new must be a new kind with unit tests, never logic hidden in the interface. |
| 8 | البرمجة قبل الذكاء الاصطناعي: المواد التي يفرضها نوع الدعوى يقترحها الفهرس لا النموذج، والاقتباسات تُقارن بنص المادة. | Deterministic before AI: the articles a claim type requires are proposed by the catalogue, not by the model, and quotations are checked against the article's text. |
| «ما يعنيه هذا للمحامي» | ما يجب أن يكون دقيقا في عريضتك يأتي من استمارتك أنت، والنموذج يكتب ولا يقرر القانون. | What must be exact in your statement of claim comes from your own form, and the model writes; it does not decide the law. |

**`docs/OFFLINE.md`** (`site/src/content/docs-summaries/offline.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | بُني التطبيق المكتبي بحيث لا تغادر أي معطيات قضية حاسوبَ المحامي، لأن معطيات قضايا الموكلين يشملها السر المهني. | The desktop app is built so that no case data leaves the lawyer's computer, because client case data is covered by professional secrecy. |
| 2 | الاستمارة والقوالب والحفظ والواجهة ووضع القوالب واقتراح الأسانيد والمكتبة القانونية والبحث عن المواد لا تستعمل الشبكة إطلاقا. | The form, the templates, the storage, the interface, the template mode, the grounds suggestions, the law library and the article search use no network at all. |
| 3 | لا يُخاطَب النموذج المحلي إلا على العنوان 127.0.0.1:11434، وهو برنامج يعمل على الحاسوب نفسه: لا تغادر حزمه الجهاز. | The local model is reached only at 127.0.0.1:11434, a process on the same computer: its packets never leave the machine. |
| 4 | يُرفض أي عنوان آخر دون أي استعلام DNS؛ ولا يمر اتصال النموذج عبر أي وسيط (proxy) ولا يتبع أي إعادة توجيه، وتُرفض نماذج Ollama «السحابية». | Any other address is refused without even a DNS lookup; the model's connection uses no proxy and follows no redirect, and Ollama "cloud" models are refused. |
| 5 | لا يحتوي التطبيق على أي قياس عن بعد، ولا إحصاءات استعمال، ولا تقارير أعطال، ولا بحث عن التحديثات. | The app has no telemetry, analytics, crash reporting or update check. |
| 6 | لا يُفتح رابط «المصدر الرسمي» في المتصفح إلا حين تنقر عليه؛ وهو معطى ثابت لا يُبنى أبدا من القضية، ولا تُقبل إلا روابط joradp.dz وmjustice.dz. | An "official source" link opens in the browser only when you click it; it is fixed data, never built from the case, and only joradp.dz and mjustice.dz links are accepted. |
| 7 | تُحفظ المسودات حيث تختار، وافتراضيا في مجلد بيانات التطبيق المحلي على حاسوبك، ولا يُزامَن أي شيء مع أي مكان. | Drafts are saved where you choose, by default in your local application-data folder, and nothing is synchronised anywhere. |
| 8 | العرض التجريبي على الإنترنت برنامج منفصل يرسل طلباته إلى نموذج مستضاف، لذلك فهو للقضايا الخيالية فقط. | The public web demo is a separate program that sends its prompts to a hosted model, so it is for fictitious cases only. |
| «ما يعنيه هذا للمحامي» | يمكنك أن تصوغ عريضتك والحاسوب مفصول عن الشبكة: وقائع موكلك تبقى على حاسوبك، كما يقتضي السر المهني. | You can draft with the network unplugged: your client's facts stay on your computer, as professional secrecy requires. |

**`docs/LAW-LIBRARY.md`** (`site/src/content/docs-summaries/law-library.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | لا يقتبس التطبيق أي مادة إلا من نسخته المحلية من النصوص العربية الرسمية، لا من ذاكرة نموذج: فنموذج بأربعة مليارات معامل لا يتذكر أرقام المواد ولا صياغتها بدقة. | The app quotes an article only from its local copy of the official Arabic texts, never from a model's memory: a 4-billion-parameter model cannot recall article numbers or wording reliably. |
| 2 | حُمّلت اليوم 1,361 مادة: قانون الأسرة كاملا (227) وقانون الإجراءات المدنية والإدارية كاملا (1,084)، و33 مادة من القانونين المدني والتجاري، و17 مادة من قانون العمل. | 1,361 articles are loaded today: the complete Family Code (227) and the complete CPCA (1,084), 33 articles of the Civil and Commercial Codes, and 17 labour-law articles. |
| 3 | يذكر كل نص كيف فُحص: قُرئ على صفحة الجريدة الرسمية التي نشرته، أو قورن حرفا حرفا بملفه الرسمي PDF، أو نُقل من صور الصفحات وقُرئ مرتين. | Each text says how it was checked: read against the Official Journal page that published it, proved letter by letter against its official PDF, or transcribed from page images and read twice. |
| 4 | لا تُعلَّم أي مادة على أنها مُتحقَّق منها قبل أن يقارنها محام بصفحتها. | No article is marked verified until a lawyer has checked it against its page. |
| 5 | لا يُجلب شيء من الإنترنت ولا يُبحث فيه ولا تُقرأ ملفات PDF أثناء عملك؛ ولا يبدأ التحليل إلا حين تنقر. | Nothing is scraped, searched online or read from PDFs while you work; the analysis starts only when you click. |
| 6 | تحت كل مادة مقترحة: مرجعها الرسمي، وموضعها في القانون، ورابط إلى الوثيقة الرسمية، ووسم «لم يتحقق منها محام». | Under each suggested article: the official reference, its place in the code, a link to the official document, and a "not verified by a lawyer" tag. |
| 7 | إذا لم يُتعرَّف على أي نوع دعوى فلا مرشح ولا اقتراح؛ لكن النموذج الصغير لا يستعمل هذا المخرج، فالمحامي هو من يحكم على نوع الدعوى المقترح. | With no recognised claim type there is no candidate and nothing is proposed; a small model does not use this way out, so the lawyer judges the proposed claim type. |
| 8 | لا يُنزّل التطبيق أي شيء: استيراد النصوص مهمة يتولاها المطوّر وقت التطوير، والتطبيق المُوزَّع يقرأ نصوصه دون اتصال. | The app never downloads anything: importing a text is a maintainer's task at development time, and the shipped app reads its texts offline. |
| «ما يعنيه هذا للمحامي» | كل مادة يقترحها التطبيق مقتبسة من نص رسمي، مع مرجعها ورابط مصدرها، لكن لم تُعلَّم أي منها بعدُ على أن محاميا راجعها. | Every article the app proposes is quoted from an official text, with its reference and source link, but none is yet marked as checked by a lawyer. |

**`docs/HIDDEN-SET.md`** (`site/src/content/docs-summaries/hidden-set.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | تقيس المجموعة المخفية جودة قراءة التطبيق للوقائع، على قضايا لم يرها مطوّروه قط. | The hidden set measures how well the app reads the facts, on cases its developers have never seen. |
| 2 | القضايا الـ61 المستعملة في التطوير بُني عليها القارئ وضُبط، فنتيجته عليها متفائلة؛ والمجموعة المخفية هي القياس الصادق. | The 61 development cases were used to build and tune the reader, so its score on them is optimistic; the hidden set is the honest measure. |
| 3 | يكتبها شخص لم يعمل على القارئ، ولا يجوز له الاطلاع على تعليماته ولا على قضايا التطوير ولا على نتائج الاختبارات. | It is written by someone who has not worked on the reader, and who may not read its prompts, the development cases or the benchmark results. |
| 4 | لا تدخل المستودع أبدا ولا تُرسل إلى المطوّرين؛ ولا يُشارَك منها إلا ملخص نتائجها. | It never enters the repository and is never sent to the developers; only its summary is shared. |
| 5 | نحو 100 قضية خيالية بالعربية الفصحى: 4 أو 5 لكل نوع من أنواع الدعاوى الـ21 في الفهرس، و10 قضايا خارجه. | About 100 fictitious cases in formal Arabic: 4 or 5 for each of the 21 claim types of the catalogue, and 10 outside it. |
| 6 | تُكتب القضايا كما يكتب المحامي ملاحظاته: تفاصيل جانبية، وتواريخ ومبالغ عدة، ووقائع لم تحدث، ووقائع ينكرها أحد الطرفين. | The cases read like a lawyer's notes: side details, several dates and amounts, facts that did not happen and facts one party denies. |
| 7 | أصاب التشغيل الأول (30 أكتوبر 2026) القضية كاملةً في 32 من 90 مع إعطائه نوع الدعوى؛ وفي 31 أكتوبر 2026 أصاب نموذج Nemotron 3 Ultra عبر الإنترنت 64 من 90 مع إعطائه نوع الدعوى، و60 من 90 حين قرأ نوع الدعوى بنفسه، فأصاب نوع الدعوى في 85 من 90 وامتنع عن تصنيف القضايا العشر الخارجة عن الفهرس كلها. | The first run (30 October 2026) got 32 of 90 cases fully right with the claim type given; on 31 October 2026, Nemotron 3 Ultra online got 64 of 90 with the claim type given, and 60 of 90 with the claim type read, the claim type right on 85 of 90 and all 10 out-of-scope cases declined. |
| 8 | متى اطُّلع على نتائج مجموعةٍ لم تعد مخفية: فالقياس التالي لقارئ مُعدَّل يحتاج إلى مجموعة جديدة. | Once its results have been looked at, a set is no longer hidden: the next measure of a changed reader needs a new set. |
| «ما يعنيه هذا للمحامي» | تبيّن الأرقام كم مرة تكون قراءة الوقائع صحيحة كاملةً على قضايا لم يرها المطوّرون: هذا هو القياس الصادق، لا المتفائل. | The figures show how often the reading of the facts is fully right on cases the developers never saw: the honest measure, not the optimistic one. |

**`docs/WEB-DEMO.md`** (`site/src/content/docs-summaries/web-demo.ar.md`)

| # | Arabic | English |
|---|---|---|
| 1 | المنتج تطبيق مكتبي يعمل دون اتصال؛ ووُجد العرض التجريبي ليستعمل أي أحد المنتج الحقيقي من رابط، دون تثبيت أي شيء. | The product is an offline desktop app; the web demo exists so that anyone can use the real product from a link, without installing anything. |
| 2 | يشغّل المحرك نفسه والمكتبة القانونية نفسها ومنطق الشاشات نفسه؛ والفرق الوحيد هو مكان عمل نموذج الذكاء الاصطناعي. | It runs the same engine, the same law library and the same screen logic; the one difference is where the AI model runs. |
| 3 | في التطبيق المكتبي النموذج هو Gemma 3 4B، محليا؛ وفي العرض التجريبي هو Nemotron 3 Ultra المستضاف على OpenRouter أولا، ثم Gemma 4 26B على Google AI Studio، ثم Groq. | On the desktop the model is Gemma 3 4B, local; in the demo it is Nemotron 3 Ultra hosted on OpenRouter first, then Gemma 4 26B on Google AI Studio, then Groq. |
| 4 | يرسل العرض التجريبي القضية إلى النموذج المستضاف، لذلك فهو للقضايا الخيالية فقط. | The demo sends the case to the hosted model, so it is for fictitious cases only. |
| 5 | لا يحفظ العرض التجريبي أي شيء على الخادم: «فتح» و«حفظ» يقرآن ملف مسودة على قرص الزائر نفسه ويكتبانه، وما عدا ذلك لا تُحفظ القضية إلا نسخةً احتياطية في المتصفح. | The demo saves nothing on the server: Open and Save use a draft file on the visitor's own disk, and the case is otherwise kept only as a recovery copy in the browser. |
| 6 | نداءات النموذج محدودة لكل زائر ولكل عنوان شبكة، وتمر عبر طابور واحد مشترك، نداءان في وقت واحد. | Model calls are limited per visitor and per network address, and go through one shared queue, two calls at a time. |
| 7 | الأعطال لا تنهي جلسة الزائر أبدا: الخدمة المشغولة أو المتعطلة تُحيل الطلب إلى المحرك التالي، ويرى الزائر رسالة واضحة. | Failures never end a visitor's session: a busy or failing service hands the request to the next engine, and the visitor sees a plain message. |
| 8 | حتى النموذج الأكبر في العرض التجريبي يخطئ، ولهذا تهم الفحوص: المواد التي يفرضها نوع الدعوى تُقترح دائما، والبرنامج يكتب قاعدة كل مادة مؤشَّر عليها. | Even the demo's larger model makes mistakes, which is why the checks matter: the articles a claim type requires are always proposed, and the program writes each ticked article's rule. |
| «ما يعنيه هذا للمحامي» | جرّب العرض التجريبي بقضايا مختلَقة فقط: ملفاتك الحقيقية مكانها التطبيق المكتبي، حيث لا يغادر شيء حاسوبك. | Try the demo with invented cases only: your real files belong in the desktop app, where nothing leaves your computer. |

### Files changed in push 3

- `site/src/content/docs-summaries/*.md` (12, new), `site/src/content.config.ts`, `site/src/pages/[lang]/docs/[slug].astro`, `site/public/ui.js`, `site/src/i18n/site.ar.json` and `site.en.json` (three keys), `site/tests/site.spec.ts`; this report and its captures. Nothing outside `site/` and this folder.

## Three notes about the desktop app (to record, not to fix in this phase)

1. **A clipped first letter in the paper view.** In the 1920 x 1080 captures of item 0 (`C:\Users\TheShadow\dz-laptop-shots\app-*-1920x1080.png`, outside the repository), and again in the scale-2 frames of push 2's second pass, a few lines of the grounds start with a clipped first letter at the right margin: «وبأنه» shows as «؛بأنه». The paper's text block clips the glyph that overhangs the line's start.
2. **The release test is timing-sensitive under the parallel suite.** `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp` queues the release on the thread pool and waits 2.5 s; when the rest of the desktop suite holds every pool thread (on battery, in particular) the queued task has not started when the wait ends and the test fails on its counter (expected 1, actual 0). It passes alone and passes with the collections run one at a time (`-- xUnit.ParallelizeTestCollections=false`). Details in `docs/website/phase-5/REPORT.md`, follow-up section.
3. **One string missing from the Arabic resources.** `ClaimConfirmedStatus` («You confirmed the claim type; its required articles are ticked. Review the model's other proposals.») exists in `Strings.en.resx` only, so the Arabic interface shows it in English under «تحليل الوقائع» once the claim type is confirmed (visible in the scale-2 frames). The site's AI stage 3 crop stops above it.
