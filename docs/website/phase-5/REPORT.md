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
| `story.chip.code` | برمجة · مقتبس من النص الرسمي | code · quoted from the official text |
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
| `proofs.shot.review` | العرض التجريبي: التصدير مرفوض ما دام قسم كتبه الذكاء الاصطناعي لم يُراجَع. | The demo: export refused … |
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

- The five AI sentences on the sheet are illustrative wording in the register of the real draft; they await Khouloud's eye like the rest of the Arabic.
- The "Review before export" card shows the web demo rather than the desktop app: the desktop frame of that state exists in one language and theme only (`workbench-export-blocked.png`), and the demo shows the same screen in both languages and themes. The caption says "The demo".
- The site's header is two rows high at 1366 px (navigation, then language and theme); the story's pin now measures it instead of assuming 96 px. The header itself is unchanged.
