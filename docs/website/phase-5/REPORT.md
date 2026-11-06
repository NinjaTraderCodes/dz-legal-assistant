# Website, phase 5: the owner's improvements

Branch `fable/website`, on top of the default branch at `693872b`. The demo at `/app` is untouched; nothing loads from the internet at run time; README unchanged. The phase is delivered in pushes the reviewer can merge one by one; each push adds its section here. Screenshots and summary tables only: no Lighthouse HTML.

## Item 0: the captures for the laptop mock-up (delivered outside the repository, 3 November 2026)

Four lossless PNGs of the real desktop window at 1920×1080 in `C:\Users\TheShadow\dz-laptop-shots\`: `app-<ar|en>-<light|dark>-1920x1080.png`. The «Unpaid loan» sample, the facts analysed and the legal grounds drafted by the local model (Ollama, gemma3:4b: 58 s and 43 s on the second, warm run), the review box ticked, 9 / 9, the grounds centred on the paper. The window was driven through UI Automation (`C:\dz-video\live\ui.ps1`) on a scratch copy of the video pipeline's profile. One finding on the way: the app's window refuses a 1080 px height when the OS resizes it (it answers with a 1220 px minimum) and crashes with an Avalonia `-40 is not a valid value for 'Height'` exception when the resize is forced past that; resizing through the UI Automation transform pattern, which goes through Avalonia's own `Width`/`Height`, works. Noted for the app, not changed here.

## Push A: the story, the grounds card, the drafting stage

### Item 2: the nine-step story

- **The frame.** While the story runs (wide screens, motion allowed), the sheet sits in a frame as high as the viewport under the header (`.story.framed .paper-canvas`, `--story-top` measured from the header) and slides inside it so the section being written is centred, clamped to the sheet's bounds (`yFor` in `site/src/scripts/story.ts`; targets re-measured on resize and after the document font arrives). Every section 01 to 09 is fully in view when its step is current, at 1366×768 and 1920×1080: `story-step<k>-*.jpg` and `story-grounds-*-*.jpg` beside this report. Phones and reduced motion keep the full sheet in its final state.
- **Writing order.** The form first (01 to 05, then 07 to 09), the check at 8 / 9 naming the missing grounds («الأسانيد التي تقوم عليها الدعوى», art. 15-5, from `data/document-types/requete-introductive/definition.json`), then step 06 as the signature moment: the sheet slides back to the grounds; the section opens with the gold side line and the «ذكاء اصطناعي» / "AI" tag; each rule sweeps in as a quotation with its green chip «برمجة · مقتبس من النص الرسمي» / "code · quoted from the official text"; the AI's lines write themselves one by one, right to left, with a gold caret; the review row appears and ticks; the check turns green, 9 / 9. The timeline is exactly 12 units over 2,470 px of scroll (`site/src/scripts/story.ts`), the story module weighs 46.8 KB gzip with GSAP (budget 60 KB).
- **The text.** `site/src/content/steps.ts` now tells rule lines (`by: "code"`, the sample's two sentences, unchanged) from the AI's lines (`by: "ai"`, five short sentences from the fictitious debt case: the loan of 1,500,000 DZD on 15 January 2025, the term of 30 June 2025, the formal notice of 10 July 2025, the notarised acknowledgement as proof). The lead and the caption say in words that the code quotes the law and the AI only applies it to the facts, with a `Source` line to README.md ("How it works", step 4) and `docs/PRODUCT-DETAILS.md`. **For Khouloud:** the five AI sentences and the chip wording are new Arabic text; listed in the addendum below.

### Item 3: "What the software guarantees", the legal grounds

A fourth card, «صياغة المناقشة القانونية» / "Drafting the legal grounds" (`proofs.grounds.*`): the code quotes each ticked article from the official text, the AI writes only how the facts meet it, the checks flag numbers absent from the facts and laws the lawyer did not give, the section carries the AI mark and cannot be exported before review. Sources under the section: README.md, `docs/PRODUCT-DETAILS.md` (lines 28 to 30 and 54 to 56), `docs/OFFLINE.md`, `data/law/codes`. The cards are now two by two with a picture each (`site/src/components/Shot.astro`: one lazy WebP `<img>` with its size set, the light or dark version matching the theme; the other has no box, so a lazy browser never fetches it):

| Card | Picture | Provenance |
|---|---|---|
| Official texts | The law library's first card (the Code of Civil and Administrative Procedure, complete, read against the Official Journal page) | Desktop app, headless UI tests (`LibraryTests`), Arabic and English, light and dark |
| Works offline | The app bar's end with «يعمل دون اتصال» / "Offline: no data leaves this computer" | Desktop app, `WorkbenchTests`, both languages and themes |
| Review before export | The demo's toolbar with the refused-export notice | The web demo with the hosted model, 3 November 2026, both languages and themes |
| Drafting the legal grounds | The drafted grounds on the paper with the review box | `docs/images/readme/live-ai-drafted-grounds.jpg`, the live recording with gemma3:4b (one theme) |

The crops are cut by `site/scripts/crops.mjs` (desktop frames from `DZLEGAL_SCREENSHOTS`, outside the repository; live frames from `docs/images/readme`) and `site/tests/guide-shots.mjs` (the demo) into `site/src/assets/shots/`, committed as PNG; every rendition the build emits is under 80 KB (WebP, quality 72), and a phone fetches none of them until it scrolls near (checked in the Lighthouse network log: the hero photo is the only image loaded).

**A measurement detour, so the next phase does not repeat it.** The first mobile Lighthouse run of the Arabic home scored 80 (blocking time 730 ms) against 91 to 96 in phase 4, and the figure moved between 79 and 88 across runs. Three alternating runs of the live phase-4 page and the local page, then Chrome traces of both, showed the same layout passes on both pages (three of about 130 ms, one per font arriving, over the same 110 to 194 layout objects: the sections below the fold are skipped by `content-visibility: auto` on phones, pictures included), and Chrome's own counters gave the same layout and style totals for both pages. The score moves because Lighthouse's simulation places the font re-layouts before or after the first paint depending on how fast the HTML and fonts arrived in the observed run: on the loopback server they arrive in tens of milliseconds, on the live host in about two hundred. Stubbing the pictures out, loading them eagerly, dropping their `srcset`, or containing their figures changed nothing beyond the noise. What did: the Arabic home now preloads its three faces (Regular, Inter, Bold: 188 KB, under the 220 KB budget) instead of one, so the re-layouts land before the first paint; three runs gave 91 to 96 with the LCP unchanged at 2.3 s (`site/src/layouts/Base.astro`, with the reason beside the tag). The English home keeps one preloaded face and scores 95 to 99. The method stays the same as in phase 4.

### Item 4: "The AI is a careful clerk", the drafting stage

A fourth stage, «صياغة المناقشة القانونية» / "Drafting the legal grounds" (`ai.stage4.*`): what the code writes (the article and its rule from the official text), what the model writes (how the facts meet it), the checks (a number absent from the facts or a law the lawyer did not give: asked once more, else «(...)», which blocks export), the review box. The pulsing dot moves to this stage. Each stage carries its frame from the live recording: reading the facts (the three stages and the timer), the candidate articles (code), the AI's reading with the question «هل هذا هو نوع الدعوى؟», and for drafting the recording's own timing line, the activity row and the drafted grounds. The time is stated as the recording shows it: 2 min 33 s on an ordinary laptop without a graphics card, with the local model (Gemma 3 4B); `Source` README.md (lines 26 and 33). The pictures come from `live-ai-reading-facts.jpg`, `live-ai-proposal.jpg`, `live-ai-drafting.jpg` and `live-ai-drafted-grounds.jpg`, cropped above the burned-in subtitles.

**Note fixed.** `ai.measureNote` said the hidden set measures "fact reading and article selection"; it measures fact reading and the claim type (`docs/HIDDEN-SET.md`: the gold answers carry no articles; `--claims read` adds the claim type). Both languages now say so, and the source is a `Source` line.

### Real AI captures, provenance

`site/tests/guide-shots.mjs` drove the local demo with the owner's keys (the demo's own engine order: Nemotron 3 Ultra through OpenRouter) on the fictitious «Unpaid loan» sample, once per language, on 3 November 2026: four model calls (analyse, draft, in Arabic and in English), repeated once after a clip fix. The pictures of steps 3 and 4 of the guide, and the refused export of step 5, come from those runs; dark first, then light through the demo's own theme button, no extra call. The stand-in model was never used for anything shown as AI output. The guide pictures (`site/src/assets/shots/guide/`) are committed now and placed on the `/try/` page in push B.

### Checks (push A)

| Check | Result |
|---|---|
| `npm run build` | 14 pages, `check-dist` clean |
| `npm run budget` | every budget met; home HTML 26.6 KB (ar) and 25.8 KB (en) gzip; home scripts with the story module 46.8 KB |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 44 passed, 2 skipped; axe 0 violations on every page in both themes |
| Lighthouse, both homes | the second of two full runs on the rebuilt site (the first: 98, 98, 98, 99): mobile 91 (ar) and 93 (en), desktop 97 and 97; LCP 2.4 s and 2.0 s; CLS 0; see the table below |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors, both |
| `dotnet test -c Release` | 985 passed, 0 failed, in 7 assemblies, with every local instance stopped (the desktop suite 7 min 30 s) |
| `docker build` (ECR mirror of `node:22-alpine`) | 259 s (the first attempt failed in 12 s while the laptop was offline); the image on port 7860 serves `/` in Arabic (25 KB compressed), `/en/`, the pictures and the three font preloads, and `/app` unchanged |

Lighthouse, the two homes (`lighthouse-summary.md`, the committed run):

| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |
|---|---|---|---|---|---|---|---|---|
| /ar/ | mobile | 91 | 100 | 100 | 100 | 2.4 s | 0 | 270 ms |
| /ar/ | desktop | 97 | 100 | 100 | 100 | 0.9 s | 0 | 10 ms |
| /en/ | mobile | 93 | 100 | 100 | 100 | 2.0 s | 0 | 280 ms |
| /en/ | desktop | 97 | 100 | 100 | 100 | 0.9 s | 0 | 110 ms |

Screenshots beside this report: `home-<ar|en>-<light|dark>-<390x844|1366x768|1920x1080>.jpg` (full page), `story-*` (the pinned timeline frame by frame: `step1` to `step7` are the plaintiff to the signature being written, the court being on the sheet from the start; `step8` and `grounds-rules`, `grounds-ai`, `grounds-review`, `grounds-done` are the signature moment; Arabic and English at 1366×768 and 1920×1080, dark; English light at 1366), `proofs-*` and `ai-*` (the two sections, both languages and themes at 1366).

### Files changed in push A

- `site/src/content/steps.ts`, `site/src/components/StepStory.astro`, `site/src/scripts/story.ts` (the story).
- `site/src/components/Proofs.astro`, `site/src/components/AiPanel.astro`, `site/src/components/Shot.astro` (new), `site/src/layouts/Base.astro` (the Arabic home's font preloads).
- `site/src/i18n/site.ar.json`, `site.en.json` (new and changed strings), `site/scripts/strings.mjs` (`ModelTag` exported), `site/src/i18n/strings.generated.json`.
- `site/scripts/crops.mjs`, `site/tests/guide-shots.mjs`, `site/tests/home-shots.mjs` (new), `site/src/assets/shots/**` (the committed PNG crops).
- Nothing outside `site/` and this report folder.

### Arabic strings added in push A (for Khouloud)

| Key | Arabic | English |
|---|---|---|
| `story.lead` | تملأ القضية خطوةً خطوة، ويُبنى نص العريضة على الورق أمامك. المراجعة الشكلية تنبّهك فوراً إلى البيان الناقص وتذكر المادة التي تفرضه. وعندما يصوغ الذكاء الاصطناعي المناقشة القانونية، يقتبس البرنامج قاعدة كل مادة من النص الرسمي، ولا يكتب النموذج إلا كيف تنطبق على الوقائع، ثم يؤشّر المحامي على خانة المراجعة قبل أي تصدير. | The case fills in step by step… the lawyer ticks the review box before anything is exported. |
| `story.caption` | لقطة من مكتب العمل: الحالة الشكلية تتغير مع كل حرف تكتبه، وتحمل المناقشة القانونية علامة الذكاء الاصطناعي إلى أن يراجعها المحامي. | From the workbench: … the legal grounds carry the AI mark until the lawyer has reviewed them. |
| `story.chip.code` | من النص الرسمي | From the official text |
| `story.finding.grounds.title` | الأسانيد التي تقوم عليها الدعوى | Grounds on which the claim rests |
| `story.finding.grounds.article` | المادة 15-5 من قانون الإجراءات المدنية والإدارية | Code of Civil and Administrative Procedure, art. 15-5 |
| sheet, AI line 1 | وحيث إن المدعى عليه اقترض من المدعي 1,500,000 دج بتاريخ 15 جانفي 2025، | (the document, Arabic only) |
| sheet, AI line 2 | وحيث إن الأجل حلّ في 30 جوان 2025 دون أي سداد، | |
| sheet, AI line 3 | رغم الإعذار الرسمي المبلّغ له بتاريخ 10 جويلية 2025. | |
| sheet, AI line 4 | وحيث إن الاعتراف بالدين الموثق يثبت الالتزام، ولم يثبت المدعى عليه الوفاء، | |
| sheet, AI line 5 | فيكون طلب إلزامه بدفع أصل الدين مؤسسا قانونا. | |
| `proofs.grounds.title` | صياغة المناقشة القانونية | Drafting the legal grounds |
| `proofs.grounds.body` | يقتبس البرنامج قاعدة كل مادة مؤشَّر عليها من النص الرسمي، ولا يكتب الذكاء الاصطناعي إلا كيف تنطبق على الوقائع. تنبّه المراجعات إلى أي رقم لا يرد في الوقائع وأي قانون لم يذكره المحامي، ويحمل القسم علامة الذكاء الاصطناعي ولا يُصدَّر قبل مراجعته. | The code quotes the rule of each ticked article… cannot be exported before review. |
| `proofs.shot.texts` | المكتبة القانونية في التطبيق: قانون الإجراءات المدنية والإدارية كاملاً، مقروءاً على صفحة الجريدة الرسمية. | The app's law library: … |
| `proofs.shot.offline` | شريط التطبيق: «يعمل دون اتصال: لا تغادر أي معطيات هذا الحاسوب». | The app bar: … |
| `proofs.shot.review` | التطبيق المكتبي: التصدير مرفوض ما دام قسم كتبه الذكاء الاصطناعي لم يُراجَع. | The desktop app: export refused … |
| `proofs.shot.grounds` | من التسجيل الحي: المناقشة القانونية التي صاغها النموذج المحلي، مع خانة المراجعة. | From the live recording: … |
| `ai.stage4.title` | صياغة المناقشة القانونية | Drafting the legal grounds |
| `ai.stage4.body` | يكتب البرنامج قاعدة كل مادة مؤشَّر عليها من النص الرسمي، ويكتب النموذج كيف تنطبق على الوقائع فقط. الفقرة التي تذكر رقماً لا يرد في الوقائع أو قانوناً لم يذكره المحامي تُطلب مرة أخرى، وإلا تُترك «(...)» للمحامي، وهو ما يمنع التصدير. يحمل القسم علامة الذكاء الاصطناعي، ولا يُصدَّر قبل أن يؤشّر المحامي على خانة المراجعة. | The code writes the rule… ticks the review box. |
| `ai.stage4.time` | في التسجيل الحي، صاغ النموذج المحلي (Gemma 3 4B) المناقشة القانونية في دقيقتين و33 ثانية على حاسوب محمول عادي بلا بطاقة رسومية. | In the live recording, the local model (Gemma 3 4B) drafted the legal grounds in 2 min 33 s… |
| `ai.shot.stage1` | من التسجيل الحي: مراحل التحليل الثلاث والمؤقّت. | From the live recording: the three analysis stages and the timer. |
| `ai.shot.stage2` | المواد المرشّحة من الفهرس، مع ما تفرضه الدعوى منها. | The candidate articles from the catalogue… |
| `ai.shot.stage3` | قراءة الذكاء الاصطناعي للوقائع وسؤال «هل هذا هو نوع الدعوى؟». | The AI's reading of the facts and the question… |
| `ai.shot.stage4` | الصياغة جارية، ثم المناقشة القانونية مع خانة المراجعة. | Drafting in progress, then the legal grounds with the review box. |
| `ai.measureNote` | تقيس هذه الأرقام قراءة الوقائع ونوع الدعوى على قضايا خيالية، لا اختيار المواد ولا الصحة القانونية للعريضة. | These figures measure fact reading and the claim type… |
| `shot.open` | تكبير الصورة | Open larger |
| `shot.live` | لقطة من التسجيل الحي للنموذج المحلي. | A frame of the live recording with the local model. |

### Unsure about (push A)

- The grounds on the sheet were illustrative wording in push A; the follow-up below replaces them with real model output, verbatim.
- The "Review before export" card shows the web demo rather than the desktop app: the desktop frame of that state exists in one language and theme only (`workbench-export-blocked.png`), and the demo shows the same screen in both languages and themes. The caption says "The demo".
- The site's header is two rows high at 1366 px (navigation, then language and theme); the story's pin now measures it instead of assuming 96 px. The header itself is unchanged.

## Push B: the documentation pages, the guide's pictures and motion, the team text

### Item 7: documentation rendered on the website

The six documents the About page links to are pages of the site: `/en/docs/<name>/` and `/ar/docs/<name>/` for `product-details`, `architecture`, `offline`, `law-library`, `hidden-set` and `web-demo`, built from the Markdown at build time, nothing else rendered.

- **How.** A content collection (`site/src/content.config.ts`, Astro's `glob` loader reading `../docs` for exactly the six files) and one page (`site/src/pages/[lang]/docs/[slug].astro`) with the site's layout: the document's H1 lifted into the page title, a sticky table of contents from its headings 2 and 3, code blocks highlighted by Shiki with CSS variables set from the site's tokens (so they follow the theme and load nothing at run time; fences without a language, the diagrams, stay plain), tables in a scrolling wrapper, pictures through `astro:assets` (lazy WebP with a `srcset`). The Markdown pipeline is Astro 7's Sätteri processor with one plugin, `site/src/content/docs-pages.ts`; `markdown.rehypePlugins` throws in Astro 7 without `@astrojs/markdown-remark`, so the plugin uses Sätteri's filtered visitors.
- **Links.** A link to one of the six goes to its page (`../law-library/`, language-neutral, so one rendering serves both languages). Following the main branch's rule that the site links nowhere into the repository (`c395b69`), any other link into the repository (`../README.md`, `ONLINE-TESTING.md`, code and data) becomes plain text; links to outside sites (the .NET download, QuestPDF, Ollama, Render, UptimeRobot) stay, as `check-dist` allows outbound links. The "Source:" lines follow the same rule (`Source.astro`): `docs/PRODUCT-DETAILS.md`, `docs/OFFLINE.md`, `docs/HIDDEN-SET.md`, `docs/WEB-DEMO.md` and `docs/LAW-LIBRARY.md` open on the site, every other file is named as text. The About page's documentation list opens the pages; the third-party notices stay plain text there.
- **Languages.** The documents are English; the Arabic page shows the English text with the Arabic line «هذه الوثيقة متاحة بالإنجليزية فقط.» under the title, the article itself marked `lang="en" dir="ltr"`.
- **Build.** The Dockerfile's site stage copies the six files and the nine pictures PRODUCT-DETAILS embeds; `.dockerignore` lets exactly those through under `docs/*`. One thing to know: Astro caches the rendered documents in `site/node_modules/.astro/`; after a change to the plugin that folder must go before `npm run build` (noted in `docs/WEB-DEMO.md`).
- **Checks.** `site.spec.ts` covers the twelve pages (status, policy, `lang`/`dir`, canonical, alternates, the generic `og/docs-<lang>.png`, the table of contents, one H1, the Arabic note, the adapted links on product-details, nine lazy WebP pictures, `tabindex` on code blocks, the sibling link landing on its page, the sitemap); axe runs on three of them; `scripts/links.mjs` resolves relative links so the crawl follows them; `og.mjs` and `shots.mjs` know the `docs` page. Every page's HTML stays under the 30 KB gzip budget (product-details 22 KB).

### Item 5: "Test it in 5 minutes", pictures and gentle motion

- **Pictures.** Each of the five steps on `/try/` shows what the demo looks like at that point, cropped on what matters: the first screen with the sample (step 1), the red card citing CPCA art. 15-3 (step 2), the AI's reading and the question «هل هذا هو نوع الدعوى؟» (step 3), the drafted grounds with the AI mark and the review row (step 4), the refused export and then the ticked box (step 5). Arabic pictures on `/ar/`, English on `/en/`, both themes captured and the one matching the visitor's theme shown. Steps 3, 4 and 5 come from the real model runs described under push A (Nemotron 3 Ultra through OpenRouter, 3 November 2026, the fictitious «Unpaid loan» sample); steps 1 and 2 from the demo without a model call; each picture says which under it. The stand-in model was never used. Every rendition is under 80 KB and lazy; the compact guide on the home stays text-only.
- **Lightbox.** A click (or Enter) on a picture opens it larger in one native `<dialog>` (`site/src/components/Lightbox.astro`, wired by `public/ui.js`): Esc, the close button and a click outside close it, and focus returns to the picture's button. No library, nothing inline.
- **Motion.** When motion is welcome (`prefers-reduced-motion: no-preference`), `ui.js` sets `data-motion` on `<html>`; each step then fades and slides in as it scrolls into view, and a gold line down the step numbers fills up to the last step shown. Without that attribute (reduced motion, or no script) everything is simply visible and the line is full. A Playwright test checks the pictures, the dialog, the focus return and both motion states.

### Item 6: team (the text; the photos follow in push C)

The home's team section shows the fuller descriptions of the About page (`about.team.did.*`) under each name and role. In Arabic every name is in Arabic script: `AboutTeamDesign` in `src/DzLegal.Presentation/Resources/Strings.resx` now reads «جهان بن جدو — تصميم الواجهة وتجربة الاستخدام» (the owner's spelling, and the owner agreed to the change in the app's own string), `strings.generated.json` was regenerated, and `tests/DzLegal.App.Tests/AboutTests.cs` expects the new line (same assertion, same order). The English string is unchanged.

### Checks (push B)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean |
| `npm run budget` | every budget met; the documentation pages 13 to 22 KB gzip |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 52 passed, 2 skipped; axe 0 violations on the 17 pages in both themes and the demo |
| Lighthouse, `/try/` and three documentation pages | 100 on every category, mobile and desktop; mobile LCP 0.9 to 1.2 s; the rows are in `lighthouse-summary.md` and below |
| `npm run links` | 37 internal paths and 11 outbound links, 0 problems (`links.md`); the twelve documentation pages crawled through the About page and their sibling links |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors, both |
| `dotnet test -c Release` | 985 tests; the full run had one failure, `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp`, the load-sensitive one, with Ollama still listening on 11434 from the captures of item 0; it passed on its own right after (8 of 8 in its class), as in earlier phases |
| `docker build` (ECR mirror of `node:22-alpine`) | 294 s after the lock file was regenerated inside the node image (the first attempt failed at `npm ci`: a lock written on Windows is not in sync for Linux); the image serves `/` in Arabic, `/en/try/`, `/en/docs/product-details/` with its pictures, `/ar/docs/offline/` and `/app` unchanged, with no link into the repository |

Lighthouse, the guide and three documents (`lighthouse-summary.md`):

| Page | Form | Performance | Accessibility | Best practices | SEO | LCP | CLS | TBT |
|---|---|---|---|---|---|---|---|---|
| /ar/try/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 40 ms |
| /ar/try/ | desktop | 100 | 100 | 100 | 100 | 0.5 s | 0 | 0 ms |
| /en/try/ | mobile | 100 | 100 | 100 | 100 | 0.9 s | 0 | 10 ms |
| /en/try/ | desktop | 100 | 100 | 100 | 100 | 0.5 s | 0 | 0 ms |
| /ar/docs/product-details/ | mobile | 100 | 100 | 100 | 100 | 1.2 s | 0 | 30 ms |
| /ar/docs/product-details/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0.001 | 0 ms |
| /en/docs/product-details/ | mobile | 100 | 100 | 100 | 100 | 1.0 s | 0 | 10 ms |
| /en/docs/product-details/ | desktop | 100 | 100 | 100 | 100 | 0.5 s | 0 | 0 ms |
| /en/docs/law-library/ | mobile | 100 | 100 | 100 | 100 | 0.9 s | 0 | 0 ms |
| /en/docs/law-library/ | desktop | 100 | 100 | 100 | 100 | 0.3 s | 0 | 0 ms |

Screenshots beside this report: `try-<ar|en>-<light|dark>-<390x844|1366x768|1920x1080>.jpg` and `docs-*` (the product-details page) at the same sizes, full page.

### Files changed in push B

- `site/src/content/docs-pages.ts`, `site/src/content.config.ts`, `site/src/pages/[lang]/docs/[slug].astro` (new); `site/astro.config.mjs`, `site/package.json` and the lock file (`@astrojs/markdown-satteri`, Astro's own processor, declared), `site/src/content/product.ts`, `site/src/components/Source.astro`, `site/src/pages/[lang]/about.astro`, `site/src/styles/global.css`.
- `site/src/components/TryGuide.astro`, `site/src/components/Lightbox.astro` (new), `site/src/components/Shot.astro`, `site/public/ui.js`, `site/src/pages/[lang]/try.astro`, `site/public/og/docs-<lang>.png` (new).
- `site/src/components/Team.astro`; `src/DzLegal.Presentation/Resources/Strings.resx` and `tests/DzLegal.App.Tests/AboutTests.cs` (the designer's name in Arabic script); `site/src/i18n/*.json`, `strings.generated.json`.
- `site/tests/site.spec.ts`, `site/tests/axe.spec.ts`, `site/scripts/links.mjs`, `site/scripts/og.mjs`, `site/scripts/shots.mjs`; `Dockerfile`, `.dockerignore`; `docs/WEB-DEMO.md`.

### Arabic strings added in push B (for Khouloud)

| Key | Arabic | English |
|---|---|---|
| `docs.englishOnly` | هذه الوثيقة متاحة بالإنجليزية فقط. | This document is in English. |
| `docs.toc` | في هذه الصفحة | On this page |
| `shot.close` | إغلاق | Close |
| `try.shot.1` | بعد فتح المثال: الخطوات التسع، والعريضة على الورق، والمراجعة الشكلية عند 8 من 9. | After opening the sample: the nine steps, the statement of claim on paper, the formal check at 8 / 9. |
| `try.shot.2` | البطاقة الحمراء: موطن المدعى عليه ناقص، المادة 15-3 من قانون الإجراءات المدنية والإدارية، و7 من 9. | The red card: the defendant's domicile missing, CPCA art. 15-3, 7 / 9. |
| `try.shot.3` | قراءة الذكاء الاصطناعي للوقائع وسؤال «هل هذا هو نوع الدعوى؟». | The AI's reading of the facts and the question… |
| `try.shot.4` | المناقشة القانونية المصوغة بعلامة الذكاء الاصطناعي، وخانة المراجعة تحتها. | The drafted legal grounds with the AI mark, and the review box under them. |
| `try.shot.5` | التصدير مرفوض قبل المراجعة، ثم مسموح بعد التأشير على الخانة. | Export refused before the review, then allowed once the box is ticked. |
| `try.shot.demo` | لقطة من العرض التجريبي، دون أي نداء للنموذج. | From the demo, without any model call. |
| `try.shot.real` | لقطة من العرض التجريبي مع نموذجه المستضاف (Nemotron 3 Ultra عبر OpenRouter)، 3 نوفمبر 2026. | From the demo with its hosted model… |
| `AboutTeamDesign` (app string) | جهان بن جدو — تصميم الواجهة وتجربة الاستخدام | Djihane Ben Djeddou — interface and experience design |

### Unsure about (push B)

- The documentation pages' meta description and Open Graph image are generic for the Arabic page (the document is English); the Arabic note leads the description.
- The guide's compact form on the home keeps no pictures, to keep the home light; the full guide carries them.

## Push C: the team pictures (item 6, the rest)

The home's team section shows a round picture beside each name (`site/src/components/Team.astro`): the project lead's and the designer's own photographs, provided by them and used with their permission, and for the legal reviewer an avatar illustration standing in for a photo, chosen by her (the "Notionists" set by Zoish, CC0 1.0, as the file's own metadata states). The files live in `site/src/assets/team/` with `NOTES.txt` saying exactly that. The photographs came as 512×512 JPEGs (31 KB and 40 KB) and are served as 96 px and 192 px WebP renditions of 1.4 to 4.2 KB; the avatar is served as its 18 KB SVG. Each picture has a meaningful alt text in the page's language («صورة هشام بن كيحول», "Avatar of Khouloud Otmane Cherif", …). The About page is unchanged.

### Checks (push C)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean |
| `npm run budget` | every budget met (home scripts 3.5 KB with `ui.js` grown by the reveal and the dialog, the story module unchanged) |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 52 passed, 2 skipped; axe 0 violations |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors, both |
| `dotnet test -c Release` | 986 passed, 0 failed, in 7 assemblies (the load-sensitive desktop test included this time), with every local instance stopped |
| `docker build` (ECR mirror of `node:22-alpine`) | 219 s; the image serves `/` in Arabic and `/en/` with the team pictures (WebP renditions of 1.4 to 3.2 KB), and `/app` unchanged |

Screenshots beside this report: `team-<ar|en>-<light|dark>-1366x768.jpg`.

### Files changed in push C

- `site/src/components/Team.astro`, `site/src/assets/team/` (`hicham.jpg`, `djihane.jpg`, `khouloud.svg`, `NOTES.txt`), `site/src/i18n/site.ar.json` and `site.en.json` (`team.photoAlt`, `team.avatarAlt`).

### Arabic strings added in push C (for Khouloud)

| Key | Arabic | English |
|---|---|---|
| `team.photoAlt` | صورة {0} | Photo of {0} |
| `team.avatarAlt` | رسم رمزي يمثّل {0} | Avatar of {0} |

## Still open after push C

- **Item 1, the hero with the laptop:** waits for the owner's laptop image, made from the four captures delivered under item 0, and the owner's instructions in a later session.

## Follow-up to the review of pushes A, B and C

One commit with the reviewer's five fixes, and the two notes asked for.

1. **Step 06 shows real model output, verbatim.** The sheet's grounds are no longer illustrative: they are the three paragraphs the demo's hosted model wrote for the «Unpaid loan» sample in the **Arabic run of 4 November 2026** (engine picker: Nemotron 3 Ultra (خادم العرض); the page's own line: أجاب: nvidia/nemotron-3-ultra-550b-a55b (OpenRouter)), read from the page's DOM by `tests/guide-shots.mjs --dump` and kept in `site/src/content/grounds-run-ar.json`, from which `steps.ts` builds the section at build time. The pictures of steps 3 to 5 on `/ar/try/` come from the same run (and those on `/en/try/` from the English run of the same day, whose text differs slightly, as every run does). The split keeps the app's own: the paragraph opens with the articles and the catalogue's rule (`data/law/claims/civil.json`, the loan claim's `why` lines, which the code writes), and the model's application follows; `steps.ts` finds where the catalogue's wording ends and marks the rest as the model's. Long sentences wrap on the sheet, which makes the section taller than the frame at 1366 x 768, so the sheet now follows the line being written (a small addition to `story.ts`: before each line sweeps in, and before the review row appears, the sheet slides just enough to keep that line in view, never above the top of the section; on a 1080 px screen the section fits and nothing moves). The text, as the page gave it:

> حيث إن المواد 106 و107 و450 من القانون المدني تقضي بأن العقد شريعة المتعاقدين، وبأنه يجب تنفيذ العقد طبقا لما اشتمل عليه وبحسن نية، وبأنه يلتزم المقترض برد نظير ما اقترضه عند نهاية القرض، وحيث إن المدعى عليه التزم بموجب عقد الاعتراف بالدين الموثق المؤرخ في 15/01/2025 برد المبلغ المقترض البالغ 1,500,000 دج في أجل أقصاه 30 جوان 2025، وأخل بهذا الالتزام بامتناعه عن السداد رغم حلول الأجل، فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي أصل الدين المقدر بـ 1,500,000 دج يكون مؤسسا قانونا.
>
> وحيث إن المادة 323 من القانون المدني تقضي بأن على الدائن إثبات الالتزام وعلى المدين إثبات التخلص منه، وحيث إن المدعي أثبت قيام الالتزام في ذمة المدعى عليه بنسخة عقد الاعتراف بالدين الموثق المؤرخ في 15/01/2025، ولم يثبت المدعى عليه التخلص منه.
>
> وحيث إن المادتين 179 و186 من القانون المدني تقضيان بأنه لا يستحق التعويض إلا بعد إعذار المدين، وبتعويض الدائن عن الضرر اللاحق من تأخر المدين في الوفاء بمبلغ من النقود، وحيث إن المدعى عليه تأخر في الوفاء بمبلغ الدين بعد إعذاره رسميا بمحضر المؤرخ في 10/07/2025، مما ألحق بالمدعي ضررا ناتجا عن المماطلة، فإن طلب المدعي إلزام المدعى عليه بأن يدفع للمدعي مبلغ 150,000 دج تعويضا عن الضرر الناتج عن المماطلة يكون مؤسسا قانونا.
>

2. **"Review before export" card:** the desktop app's own frame, `docs/images/workbench-export-blocked.png`, cropped to the refused-export notice (Arabic, light, one frame shown in both themes); the demo's toolbar is gone from the card.
3. **The quoted-law chip:** «من النص الرسمي» / "From the official text".
4. **Header:** one row from 900 px up, in both languages, at 1366 and 1920 (the page is capped at 1180 px at every width, so the same tighter spacing serves both; measured 65 px high in both languages at both widths, against 123 px before at 1920 and 115 px at 1366). Below 1200 px the controls may still wrap under the links; the phone layout is unchanged.
5. **Team cards in English:** the role opens with a capital through CSS (`::first-letter` under `html[lang="en"]`); the app's strings are unchanged.

**The desktop test.** `WaitingWorkflowTests.ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp` (in `tests/DzLegal.App.Tests/WaitingTests.cs`) builds a view model with one local provider whose release never answers, calls `ReleaseModels()` and asserts two things: the release was asked (its counter is 1) and the call returned within `ReleaseWait` plus two seconds. `ReleaseModels()` queues each release on the thread pool (`Task.Run`) and waits at most `ReleaseWait` plus half a second for them all. When it fails, it is the first assertion (expected 1, actual 0, the test itself taking 2 s): the wait ran out before the thread pool had started the queued release, because the rest of the desktop suite, which xunit runs in parallel, was holding every pool thread at that moment. It is a timing-dependent test, not a regression; this branch touches neither the test nor the code it exercises. The evidence: in push B's full run it failed once and passed alone right after (8 of 8 in 3 s); push C's full run passed it with the rest (986 of 986, laptop plugged in); in the two full runs of this follow-up, on battery (60 to 67 percent, the desktop suite taking 6 min instead of 1.5 min plugged in), it failed both times with the same message, passed alone (8 of 8 in 5 s), and passed with the whole desktop suite when the collections ran one at a time (`dotnet test tests/DzLegal.App.Tests -c Release -- xUnit.ParallelizeTestCollections=false`: 305 of 305 in 5 min 53 s). Ollama listening on 11434 at the time of push B was my first guess and is not the cause: the test talks to no server. I changed neither the test nor `ReleaseModels()` (outside this brief); if the reviewer wants it steady under load, the release could be started with `TaskCreationOptions.LongRunning` so it gets its own thread, or the test could wait for the counter rather than for the pool.

**The lock file.** Regenerating `site/package-lock.json` inside the node image changed 48 lines (41 added, 7 removed). Added: the optional packages `@emnapi/core` 1.11.3 and `@emnapi/runtime` 1.11.3, which `@img/sharp-wasm32` and `@tailwindcss/oxide-wasm32-wasi` (the WebAssembly fallbacks of sharp and Tailwind) list as optional dependencies and which the Windows install had left out of the lock, plus the bundled copies (1.11.1) that `@tailwindcss/oxide-wasm32-wasi` carries. Removed: six stale `"peer": true` flags. Every direct dependency keeps its version (`@astrojs/markdown-satteri` 0.4.6, `@astrojs/sitemap` 3.7.4, `@tailwindcss/vite` 4.3.3, `astro` 7.4.4, `fast-xml-parser` 5.11.2, `gsap` 3.15.0, `sharp` 0.35.5, `subset-font` 2.9.0, `tailwindcss` 4.3.3; dev: `@axe-core/playwright` 4.14.0, `@playwright/test` 1.64.1, `lighthouse` 13.6.0), and the 18 Windows packages (`@img/sharp-win32-x64`, `@rolldown/binding-win32-x64-msvc`, `@bruits/satteri-win32-x64-msvc`, …) are still listed; `npm ci` on this laptop after the change: succeeded in 20 s, the lock file untouched, `@img/sharp-win32-x64` and the other Windows packages installed.

### Checks (follow-up)

| Check | Result |
|---|---|
| `npm run build` | 26 pages, `check-dist` clean |
| Playwright `site.spec.ts` + `axe.spec.ts` (Chromium) | 52 passed, 1 skipped; axe 0 violations |
| `npm run budget` | every budget met (home scripts with the story module 47.8 KB of 60 KB) |
| `dotnet build -c Release` with SDK 10.0.114 pinned and with the default SDK | 0 warnings, 0 errors with both (10.0.114 and 10.0.403) |
| `dotnet test -c Release` | the six other projects green (681 of 681: Storage 18, Documents 6, FormEngine 67, Drafting 399, Web 75, Law 116); the desktop project 304 of 305 in both full parallel runs on battery (the one test above), 305 of 305 with the collections run one at a time |
| `docker build` (ECR mirror of `node:22-alpine`) | built in 4 min 7 s; run on 7860: 200 on /, /ar/, /en/, /ar/try/, /en/try/, both product-details pages and /app; the sheet's real text and the new pictures are served (the refused-export crop 2.4 KB, the step 4 picture 70.7 KB, WebP) |

The captures affected by the fixes were retaken (`story-*`, `proofs-*`, `team-*`, `home-*`, `try-*`).

### Files changed in the follow-up

- `site/src/content/steps.ts`, `site/src/scripts/story.ts`, `site/src/content/grounds-run-ar.json` and `grounds-run-en.json` (new, the runs' text), `site/tests/guide-shots.mjs` (`--dump`), `site/src/assets/shots/guide/step3-*`, `step4-*`, `step5-*` (the new runs), `site/src/assets/shots/proofs/export-blocked.png` (new), `site/scripts/crops.mjs`, `site/src/components/Proofs.astro`, `site/src/components/Header.astro`, `site/src/components/Team.astro`, `site/src/i18n/site.ar.json`, `site.en.json`, this report and its captures.
