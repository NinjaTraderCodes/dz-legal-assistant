# Website, phase 3: the demo restyled on the product's own tokens

Branch `fable/website`, on top of the default branch's phase-2 merge (dc7b17a). Built and checked locally on 2 November 2026; GitHub Actions are off, so every check below ran here. A restyle only: every command, the autosave and the recovery copy, the export blockers, the AI-authorship marks, the review-before-export rule and the demo's warnings are untouched, and nothing loads from the internet at run time.

## What this phase ships

- **`app.css` rewritten on `tokens.css`** (the file generated from the desktop theme, already linked before `app.css` since phase 1). The demo no longer carries a palette of its own: the brown bar (`--bar`) with the gold mark and the gavel, as the desktop's title bar; gold for the one primary action and for the AI buttons (`--gold` on `--on-gold`, the desktop's `PrimaryButtonBrush`); panels with radius 12, the card shadow and the dark theme's panel gradient; controls with radius 8, a hairline, and the gold edge as focus ring; the AI mark on the gold accent (the "AI" tag of the desktop) instead of the old purple; findings as the desktop draws them (radius 10 on the soft red and amber, no border); the score at 22 bold; base type 15 and headings at 18. The paper stays light in both themes, with its own fixed ink colours, and now carries the desktop editor's «مسودة» watermark when the "Draft" watermark box is ticked (the same setting the exports use).
- **The guide as the desktop's tour card:** the same `showGuide` state, buttons and texts; the card takes the tour surface, radius 16 and shadow, gold step numbers, and the two AI steps carry the app's own "AI" tag. It stays inline above the workbench rather than becoming a modal overlay: a modal would block the workbench while open, which the current guide does not, and that would be a behaviour change.
- **The app bar on a phone** keeps the sample picker, the theme and the language; New, Open, Save, Download for Windows and Guide fold behind one "More" button (`aria-expanded`), a screen state only. On a desktop nothing is folded. The mobile tabs are pills.
- **Dark and light** both come from the tokens; `html[data-theme]` is the source and `.app.dark` only sets `color-scheme`. The reconnect dialog of the framework takes the surface, text and font tokens instead of a white box.
- **Phase-2 review items:** the privacy page's demo section gained one line, in both languages, saying the demo also sets the framework's standard security cookie (its name starts with `.AspNetCore.Antiforgery`), which holds no case data. The Arabic of the four pages is otherwise unchanged; the complete list is at the end of this report for Khouloud.
- **Tests:** one Playwright test for the phone bar and the tour card; axe on the demo's first screen in both themes (already in the suite) found two muted lines inside the red finding cards at 4.38:1 with the new muted token, now on a darker shade (4.5:1 and above).

## No new claims

The demo's texts are unchanged (`WebText.cs` gains only the word "More"/«المزيد» for the phone button). The one new sentence on the site, the antiforgery cookie line, describes the framework's own cookie as the reviewer asked; source: the framework's antiforgery service registered in `src/DzLegal.Web/Program.cs`.

## Checks

| Check | Result |
|---|---|
| `dotnet build -c Release` (whole solution) | 0 warnings, 0 errors |
| `dotnet test -c Release` | all green: Storage 18, Documents 6, FormEngine 67, Drafting 399, Web 73, Law 116, App 305 (984 in all) |
| `npm run build` (prebuild checks, postbuild check-dist) | 12 pages, clean |
| Playwright `site.spec.ts` + `axe.spec.ts` | 40 passed, 1 skipped (the fallback test, which needs a second instance). axe: 0 violations on the 12 site pages in both themes and on the demo's first screen in both themes |
| `docker build` (local, through the ECR mirror of `node:22-alpine`, base images cached) | 209 s, success; the image answered `/healthz` with `"site": true`, `/app?lang=ar` and `/en/privacy/` with 200 on a spare port |

Behaviour checked on the restyled demo with a scripted run against a second instance started without any AI key (so no online credit was spent), then by eye in both languages and both themes: the first sample opens from the picker at 8 / 9 (the grounds not yet drafted); clearing the defendant's domicile drops the formal check to 7 / 9 with a red finding naming art. 15-3, and restoring it brings 8 / 9 back; "Draft legal grounds" in template mode answers ("stub (template mode, no AI)"), the paper carries the watermark, and the "Before export" notice lists the unfinished «(...)»; clicking "Export PDF" then shows the blockers as an alert and the status bar says the document cannot be exported; after a reload the recovery banner offers the case kept in the browser, and "Clear this browser's copy" deletes it with its confirmation; the theme and language buttons are remembered (Playwright).

## Screenshots

- `app-<ar|en>-<light|dark>-<WxH>.png` at 390×844 (the phone bar with More and the tab pills), 1366×768 and 1920×1080: the first screen with the tour card, the case form, the draft header and the formal check.
- `app-draft-<ar|en>-<light|dark>-<WxH>.jpg` at 1366×768 and 1920×1080, full page after "Draft legal grounds" in template mode: the engine's answer line, the "Before export" notice, the paper with its watermark, the amber review note on the paper, the formal check at 9 / 9 with the elements list, the legal point.
- `privacy-<ar|en>-<light|dark>-1366x768.jpg`: the privacy page with the new cookie line.

## Files changed

- `src/DzLegal.Web/wwwroot/app.css` (rewritten on the tokens), `Components/Pages/Home.razor` (the brand mark, the folded bar, the tour classes and AI tags, the watermark class), `Components/Pages/Home.razor.cs` (one screen-state field, `showMore`), `Services/WebText.cs` ("More"), `Components/Layout/ReconnectModal.razor.css` (tokens).
- `site/src/i18n/site.ar.json`, `site.en.json`, `site/src/pages/[lang]/privacy.astro` (the cookie line), `site/tests/site.spec.ts` (one test).
- README unchanged.

## Not changed, noted

- At 1366 px wide the app bar wraps onto two rows in Arabic (the tools row is wider than the space left beside the title and the chip); at 1920 it is one row. The desktop's own title bar has fewer tools.
- `Icon.razor` and `IconPaths.cs` are untouched, so the theme button keeps its moon in both themes (the icon set has no sun). Phase 4 can add the sun glyph if wanted.

## The Arabic strings of the four pages, for Khouloud

One list, in the order of the pages, from `site/src/i18n/site.ar.json` (the key is how the page refers to the text; the English counterpart is the same key in `site.en.json`). The strings taken from the app itself (About, Library and Help screens) are not repeated here: they are the app's own and live in `Strings.resx`.

### Shared (navigation, labels)

| Key | Arabic |
|---|---|
| `nav.home` | <span dir="rtl">الرئيسية</span> |
| `nav.library` | <span dir="rtl">المكتبة القانونية</span> |
| `nav.privacy` | <span dir="rtl">الخصوصية</span> |
| `nav.about` | <span dir="rtl">حول المشروع</span> |
| `nav.menu` | <span dir="rtl">القائمة</span> |
| `footer.pages` | <span dir="rtl">صفحات الموقع</span> |
| `source.label` | <span dir="rtl">المصدر:</span> |
| `download.more` | <span dir="rtl">التفاصيل والمتطلبات</span> |
| `nav.label` | <span dir="rtl">التنقل الرئيسي</span> |

### Download page

| Key | Arabic |
|---|---|
| `meta.download.description` | <span dir="rtl">تحميل مساعد صياغة العرائض لويندوز 10 أو 11: تطبيق مكتبي يعمل دون اتصال لتحرير عريضة افتتاح الدعوى، مع نموذج ذكاء اصطناعي محلي اختياري عبر Ollama.</span> |
| `dl.eyebrow` | <span dir="rtl">التحميل</span> |
| `dl.title` | <span dir="rtl">التطبيق المكتبي لويندوز</span> |
| `dl.lead` | <span dir="rtl">المنتج الذي يستعمله المحامون. يعمل دون اتصال على حاسوبك: ملفات الموكّلين لا تغادره، والذكاء الاصطناعي، إن أردته، يعمل محلياً هو أيضاً.</span> |
| `dl.cta` | <span dir="rtl">تحميل نسخة ويندوز (ملف zip)</span> |
| `dl.releases` | <span dir="rtl">كل الإصدارات على GitHub</span> |
| `dl.version` | <span dir="rtl">الإصدار {0}، منشور على GitHub.</span> |
| `dl.steps.title` | <span dir="rtl">التثبيت في ثلاث خطوات</span> |
| `dl.step1` | <span dir="rtl">نزّل ملف zip من آخر إصدار.</span> |
| `dl.step2` | <span dir="rtl">فكّ الضغط في أي مجلد. لا برنامج تثبيت: المجلد يحوي كل ما يلزم.</span> |
| `dl.step3` | <span dir="rtl">شغّل DzLegalAssistant.exe. في أول تشغيل: شاشة ترحيب، ثم إعداد في ثلاث خطوات (المحامي، المكتب، التفضيلات) يملأ خانة التوقيع في كل عريضة جديدة، ثم شاشة البداية.</span> |
| `dl.step4` | <span dir="rtl">دون نموذج ذكاء اصطناعي، اختر المحرّك «دون ذكاء اصطناعي (قوالب جاهزة)»: الاستمارة والمراجعة الشكلية والمكتبة القانونية والتصدير تعمل كلها.</span> |
| `dl.req.title` | <span dir="rtl">المتطلبات</span> |
| `dl.req.os` | <span dir="rtl">ويندوز 10 أو 11.</span> |
| `dl.req.runtime` | <span dir="rtl">لا حاجة إلى تثبيت .NET: الحزمة تحمل بيئة التشغيل والنصوص القانونية والخطوط.</span> |
| `dl.req.memory` | <span dir="rtl">دون ذكاء اصطناعي: أي حاسوب حديث. مع النموذج المحلي: 8 جيغابايت من الذاكرة على الأقل (gemma3:4b)، ويُنصح بـ16 جيغابايت.</span> |
| `dl.ai.title` | <span dir="rtl">النموذج المحلي، اختياري</span> |
| `dl.ai.body` | <span dir="rtl">يقرأ النموذج الوقائع ويقترح نوع الدعوى والمواد، ثم يصوغ الأسانيد القانونية. يعمل على حاسوبك عبر Ollama. لا يُرفق أيّ منهما بالتطبيق: تثبّتهما أنت وفق شروطهما.</span> |
| `dl.ai.step1` | <span dir="rtl">ثبّت Ollama. يعمل كبرنامج محلي على العنوان 127.0.0.1:11434.</span> |
| `dl.ai.step2` | <span dir="rtl">نفّذ الأمر ollama pull gemma3:4b (نحو 3.3 جيغابايت، تنزيل لمرة واحدة). بعدها لا حاجة إلى الشبكة.</span> |
| `dl.ai.step3` | <span dir="rtl">يبحث التطبيق عن Ollama عند التشغيل وكل 30 ثانية، ويقول بوضوح إن لم يكن يعمل أو إن لم يكن النموذج مثبّتاً.</span> |
| `dl.ai.speed` | <span dir="rtl">على حاسوب محمول عادي بلا بطاقة رسومية (Intel i5-1335U)، يُظهر التسجيل المباشر 3 دقائق و17 ثانية لقراءة الوقائع ودقيقتين و33 ثانية لصياغة الأسانيد. أول طلب بعد التشغيل يحمّل النموذج أيضاً (10 إلى 40 ثانية).</span> |
| `dl.data.title` | <span dir="rtl">أين تُحفظ بياناتك</span> |
| `dl.data.body` | <span dir="rtl">في مجلد المستخدم على هذا الحاسوب فقط: يحوي {0} المسودات والتقييمات وملف المحامي. لا شيء يُزامَن أو يُرسل إلى أي جهة.</span> |
| `dl.data.more` | <span dir="rtl">كل التفاصيل في صفحة الخصوصية.</span> |
| `dl.source.title` | <span dir="rtl">من الشيفرة المصدرية</span> |
| `dl.source.body` | <span dir="rtl">مع تثبيت .NET 10 SDK:</span> |
| `dl.status.title` | <span dir="rtl">أين يقف هذا الإصدار</span> |
| `dl.status.1` | <span dir="rtl">القواعد القانونية وفهرس أنواع الدعاوى لم يراجعهما محامٍ ممارس بعد؛ كل قاعدة معلَّمة «غير مراجَعة» في البيانات وفي التطبيق.</span> |
| `dl.status.2` | <span dir="rtl">نوع وثيقة واحد حتى الآن: عريضة افتتاح الدعوى.</span> |
| `dl.status.3` | <span dir="rtl">برنامج تثبيت لويندوز مخطَّط للإصدار 1.0.</span> |
| `dl.demo.title` | <span dir="rtl">لا تريد تثبيت شيء؟</span> |
| `dl.demo.body` | <span dir="rtl">العرض التجريبي في المتصفح يشغّل الشاشات نفسها والمكتبة القانونية نفسها بنموذج مستضاف أكبر، على قضايا خيالية فقط.</span> |

### Law library page

| Key | Arabic |
|---|---|
| `meta.law.description` | <span dir="rtl">النصوص القانونية الجزائرية الرسمية المحمَّلة في مساعد صياغة العرائض: أيها كامل، وكيف قورن كل نص بالجريدة الرسمية، وأين مصدره.</span> |
| `law.lead` | <span dir="rtl">تعتمد اقتراحات الأسانيد على نسخة محلية من النصوص الرسمية بالعربية. لا يقتبس التطبيق مادةً إلا من هذه المكتبة، لا من ذاكرة النموذج أبداً.</span> |
| `law.complete.title` | <span dir="rtl">نصوص كاملة</span> |
| `law.partial.title` | <span dir="rtl">نصوص جزئية</span> |
| `law.card.jo` | <span dir="rtl">الجريدة الرسمية</span> |
| `law.card.amended` | <span dir="rtl">التعديلات المطبَّقة</span> |
| `law.card.retrieved` | <span dir="rtl">نُزّل في {0}</span> |
| `law.card.file` | <span dir="rtl">الملف الرسمي (PDF)</span> |
| `law.card.amendmentFile` | <span dir="rtl">ملف التعديل (PDF)</span> |
| `law.card.local` | <span dir="rtl">نسخة محلية</span> |
| `law.how.title` | <span dir="rtl">كيف قورن كل نص بمصدره</span> |
| `law.notes.title` | <span dir="rtl">ما ينبغي معرفته</span> |
| `law.claims.title` | <span dir="rtl">أنواع الدعاوى المغطاة</span> |
| `law.claims.lead` | <span dir="rtl">{0} نوعاً من الدعاوى في {1} مجالات قانونية. لكل نوع، يسجّل الفهرس موادَّه المعتادة وطلباته ومستنداته، ويمكن للمحامي مراجعتها.</span> |
| `law.honest.title` | <span dir="rtl">الحدود، بصراحة</span> |
| `law.honest.1` | <span dir="rtl">لم يراجع محامٍ ممارس القواعد القانونية والفهرس بعد. وجد تدقيق مستقل في أكتوبر 2026 أخطاءً في الفهرس، والتصحيحات جارية.</span> |
| `law.honest.2` | <span dir="rtl">القانون المدني والقانون التجاري وقوانين العمل محمَّلة جزئياً، بالمواد التي تعتمد عليها أنواع الدعاوى في التطبيق. القانون المدني الكامل قيد المقارنة بالجريدة الرسمية.</span> |
| `law.honest.3` | <span dir="rtl">المواد الملغاة غير مدرجة؛ النص المحمَّل هو النص الساري اليوم.</span> |
| `law.inApp` | <span dir="rtl">في التطبيق، تعرض المكتبة القانونية هذه النصوص نفسها مع البحث برقم المادة أو بكلمة، وزر لنسخ نص المادة، ورابط إلى المصدر الرسمي لكل نص.</span> |
| `area.civil` | <span dir="rtl">مدني</span> |
| `area.commercial` | <span dir="rtl">تجاري</span> |
| `area.family` | <span dir="rtl">أسرة</span> |
| `area.social` | <span dir="rtl">عمل</span> |

### Privacy page

| Key | Arabic |
|---|---|
| `meta.privacy.description` | <span dir="rtl">ما يفعله مساعد صياغة العرائض على الشبكة: لا شيء ببيانات القضايا. النموذج المحلي يُستدعى على عنوان الحاسوب نفسه فقط، ولا قياس عن بُعد، والعرض التجريبي على الويب مختلف.</span> |
| `privacy.eyebrow` | <span dir="rtl">الخصوصية</span> |
| `privacy.lead2` | <span dir="rtl">بيانات الموكّلين مشمولة بالسر المهني. بُني التطبيق المكتبي بحيث لا تغادر أي بيانات قضية حاسوب المحامي.</span> |
| `privacy.network.title` | <span dir="rtl">ما يفعله التطبيق المكتبي على الشبكة</span> |
| `privacy.network.col1` | <span dir="rtl">المكوّن</span> |
| `privacy.network.col2` | <span dir="rtl">الاستعمال الشبكي</span> |
| `privacy.row1.c` | <span dir="rtl">محرّك الاستمارة، القوالب، التخزين، الواجهة</span> |
| `privacy.row1.n` | <span dir="rtl">لا شيء.</span> |
| `privacy.row2.c` | <span dir="rtl">وضع القوالب (دون ذكاء اصطناعي)</span> |
| `privacy.row2.n` | <span dir="rtl">لا شيء.</span> |
| `privacy.row3.c` | <span dir="rtl">النموذج المحلي (Ollama)</span> |
| `privacy.row3.n` | <span dir="rtl">HTTP إلى 127.0.0.1:11434 فقط: حوار مع برنامج Ollama على الحاسوب نفسه. الحزم لا تغادر الجهاز أبداً.</span> |
| `privacy.row4.c` | <span dir="rtl">اقتراح الأسانيد، المكتبة القانونية، البحث في المواد</span> |
| `privacy.row4.n` | <span dir="rtl">لا شيء. النصوص وفهرس الدعاوى مرفقة بالتطبيق.</span> |
| `privacy.row5.c` | <span dir="rtl">روابط «المصدر الرسمي» تحت المواد المقترحة</span> |
| `privacy.row5.n` | <span dir="rtl">لا شيء من التطبيق نفسه. عند النقر، يفتحها نظام التشغيل في متصفحك؛ الرابط بيان ثابت، لا يُبنى أبداً من محتوى القضية.</span> |
| `privacy.row6.c` | <span dir="rtl">القياس عن بُعد، التحليلات، تقارير الأعطال، التحقق من التحديثات</span> |
| `privacy.row6.n` | <span dir="rtl">لا وجود لها.</span> |
| `privacy.enforced.title` | <span dir="rtl">كيف تضمن الشيفرة ذلك</span> |
| `privacy.enforced.1` | <span dir="rtl">عميل النموذج لا يقبل إلا عناوين الاسترجاع المحلي الحرفية (127.0.0.0/8 و::1 وlocalhost). أي مضيف آخر يُرفض دون حتى استعلام DNS.</span> |
| `privacy.enforced.2` | <span dir="rtl">هذا العميل لا يستعمل وكيلاً (proxy) ولا يتبع أي إعادة توجيه، ويفتح كل اتصال بنفسه إلى عنوان استرجاع محلي، أياً كان المضيف الذي يذكره الطلب.</span> |
| `privacy.enforced.3` | <span dir="rtl">نماذج Ollama «السحابية»، التي كانت ستُحيل الطلبات إلى ollama.com، مرفوضة.</span> |
| `privacy.enforced.4` | <span dir="rtl">شيفرة النموذج المستضاف الخاصة بالعرض التجريبي تعيش في مشروع منفصل لا يشير إليه التطبيق المكتبي؛ اختبار يُفشل البناء إن حدث ذلك يوماً، والنشر بعلامة الاختبار الإلكتروني يفشل كذلك.</span> |
| `privacy.enforced.5` | <span dir="rtl">التطبيق لا يرسل أي قياس عن بُعد. الاختبارات الآلية تغطي الحارس ودورة صياغة ومراجعة كاملة دون شبكة.</span> |
| `privacy.verify.title` | <span dir="rtl">تحقّق بنفسك</span> |
| `privacy.verify.1` | <span dir="rtl">افصل الحاسوب عن كل الشبكات، أو عطّل بطاقة الشبكة.</span> |
| `privacy.verify.2` | <span dir="rtl">شغّل التطبيق واستعمل وضع القوالب. كل شيء يعمل.</span> |
| `privacy.verify.3` | <span dir="rtl">مع تثبيت Ollama وتنزيل النموذج مسبقاً، يعمل النموذج المحلي أيضاً، لأن Ollama نفسه يعمل محلياً.</span> |
| `privacy.verify.4` | <span dir="rtl">راقب الاتصالات إن شئت (netstat -ano في ويندوز): الاتصال الوحيد هو إلى 127.0.0.1:11434.</span> |
| `privacy.storage.title` | <span dir="rtl">أين تُحفظ البيانات</span> |
| `privacy.storage.1` | <span dir="rtl">تُحفظ المسودات حيث تختار؛ المجلد الافتراضي هو {0}.</span> |
| `privacy.storage.2` | <span dir="rtl">تُحفظ سجلات التقييم في {0}.</span> |
| `privacy.storage.3` | <span dir="rtl">ملف المحامي من إعداد أول تشغيل يُحفظ على هذا الحاسوب فقط ولا يُرسل إلى أي جهة.</span> |
| `privacy.storage.4` | <span dir="rtl">يحفظ التطبيق نسخة استعادة أثناء العمل؛ إن أُغلق فجأة، يعرض عليك استئناف الجلسة. لا شيء يُزامَن.</span> |
| `privacy.demo.title` | <span dir="rtl">العرض التجريبي على الويب مختلف</span> |
| `privacy.demo.lead` | <span dir="rtl">العرض التجريبي العمومي على /app برنامج منفصل لتجربة المنتج من رابط. الفرق الوحيد هو مكان تشغيل الذكاء الاصطناعي.</span> |
| `privacy.demo.1` | <span dir="rtl">ما تكتبه يُرسل إلى نموذج مستضاف (Nemotron 3 Ultra عبر OpenRouter، مع Google AI Studio وGroq بديلين). أدخل قضايا خيالية فقط.</span> |
| `privacy.demo.2` | <span dir="rtl">الخادم لا يحتفظ بحسابات ولا يخزّن أي قضية. تُحفظ قضيتك نسخةً للاستعادة في متصفحك أنت لمدة يوم على الأكثر؛ «امسح نسخة المتصفح» يحذفها فوراً.</span> |
| `privacy.demo.3` | <span dir="rtl">استدعاءات الذكاء الاصطناعي محدودة لكل جلسة متصفح ولكل عنوان شبكة (60 و150 في الساعة)؛ يُستعمل العنوان لهذا العدّ.</span> |
| `privacy.demo.4` | <span dir="rtl">يقول شريط التطبيق «عرض تجريبي · قضايا خيالية فقط»، وتحمل الصفحة علامة noindex.</span> |
| `privacy.demo.5` | <span dir="rtl">يضع العرض التجريبي أيضاً ملف تعريف الأمان المعياري لإطار العمل (يبدأ اسمه بـ .AspNetCore.Antiforgery)، ولا يحمل أي بيانات قضية.</span> |
| `privacy.site.title` | <span dir="rtl">هذا الموقع</span> |
| `privacy.site.1` | <span dir="rtl">صفحات ثابتة تُقدَّم من العنوان نفسه الذي يقدّم العرض التجريبي. لا تحليلات، ولا تتبّع، ولا شيء يُحمَّل من مصدر آخر.</span> |
| `privacy.site.2` | <span dir="rtl">ملفا تعريف (cookies) من الطرف الأول فقط، يُكتبان حين تختار لغة أو مظهراً (dzlegal.lang وdzlegal.theme)، ويبقيان سنة.</span> |
| `privacy.site.3` | <span dir="rtl">الخطوط والأيقونات مرفقة مع رخصها؛ سياسة أمان الصفحة لا تسمح بأي نص برمجي مضمّن ولا بالتأطير.</span> |
| `privacy.site.4` | <span dir="rtl">روابط GitHub والجريدة الرسمية (joradp.dz) تخرج من هذا الموقع؛ هي روابط عادية لا تُحمَّل إلا حين تنقر عليها.</span> |

### About page

| Key | Arabic |
|---|---|
| `meta.about.description` | <span dir="rtl">من يبني مساعد صياغة العرائض، وكيف بُني، ومصادره القانونية وتراخيصه ووثائقه. طُوِّر في إطار ThirdUni 2026.</span> |
| `about.what.title` | <span dir="rtl">ماذا يفعل، وماذا لا يفعل</span> |
| `about.what.decide` | <span dir="rtl">التطبيق يصوغ ولا يقرّر. لا يحكم على موضوع الدعوى ولا يضمن نتيجتها، والمحامي يراجع كل عريضة ويوقّعها.</span> |
| `about.team.did.lead` | <span dir="rtl">حدّد المنتج ومساره القانوني، ووجّه وراجع وكلاء البرمجة بالذكاء الاصطناعي الذين كتبوا معظم الشيفرة، وأجرى القياسات وحملات الاختبار، واختبر التطبيق.</span> |
| `about.team.did.design` | <span dir="rtl">صمّمت وبنت مسار المحامي في التطبيق المكتبي: شاشة الترحيب، والإعداد، وملف المحامي، وشاشة البداية، والجولة الإرشادية بقضيتها النموذجية، وصفحات المساعدة، والمكتبة القانونية، بالعربية والإنجليزية، في المظهرين الفاتح والداكن.</span> |
| `about.team.did.legal` | <span dir="rtl">ماستر في القانون الخاص، تعمل في مكتب محاماة جزائري. راجعت نقاطاً قانونية في الصياغة وأجابت عن أسئلة الفريق في الإجراءات المدنية الجزائرية وأنواع الدعاوى، وهو ما شكّل دعاوى الأسرة.</span> |
| `about.built.title` | <span dir="rtl">كيف بُني</span> |
| `about.built.ai` | <span dir="rtl">كُتب معظم الشيفرة والاختبارات والوثائق بوكلاء برمجة بالذكاء الاصطناعي، بتوجيه ومراجعة من الفريق: Claude Code (معظم التطبيق)، وOpenAI Codex (مدوّنة النصوص القانونية، وجزء من مكتب العمل، وحالات الاختبار المستقلة)، وOpenCode (شاشات البداية). وأُنشئت صورة الترحيب بـChatGPT. داخل التطبيق المكتبي، الذكاء الاصطناعي الوحيد هو النموذج المحلي (Gemma 3 4B عبر Ollama)؛ أما العرض التجريبي على الويب فيستعمل Nemotron 3 Ultra عبر OpenRouter.</span> |
| `about.built.stack.title` | <span dir="rtl">التقنيات</span> |
| `about.built.stack` | <span dir="rtl">C# و.NET 10؛ Avalonia (واجهة سطح المكتب)؛ Blazor Server (العرض التجريبي على الويب)؛ QuestPDF (ملفات PDF) وOpen XML SDK (ملفات Word)؛ Scriban (القوالب)؛ Ollama (النموذج المحلي)؛ OpenRouter (النموذج المستضاف للعرض التجريبي).</span> |
| `about.licences.site` | <span dir="rtl">ما يرفقه هذا الموقع:</span> |
| `about.licences.notices` | <span dir="rtl">ملف إشعارات الأطراف الأخرى الكامل للتطبيق المكتبي</span> |
| `about.licences.image` | <span dir="rtl">أنشأها صاحب المشروع بأداة توليد الصور في ChatGPT في أكتوبر 2026؛ لا تمثّل شخصاً أو مكاناً أو علامة حقيقية.</span> |
| `about.docs.title` | <span dir="rtl">الوثائق</span> |
| `about.docs.1` | <span dir="rtl">الوصف التقني الكامل: التحليل، والصياغة، والمراجعات، والتصدير، وإعداد النموذج المحلي.</span> |
| `about.docs.2` | <span dir="rtl">الوحدات، وتدفّق البيانات، وقرارات التصميم.</span> |
| `about.docs.3` | <span dir="rtl">ماذا يعني «دون اتصال» هنا وكيف تتحقق منه.</span> |
| `about.docs.4` | <span dir="rtl">النصوص القانونية وكيف قورن كل منها بمصدره.</span> |
| `about.docs.5` | <span dir="rtl">مجموعة الاختبار المستقلة ونتائجها.</span> |
| `about.docs.6` | <span dir="rtl">العرض التجريبي على الويب، وهذا الموقع، وكيف يُشغَّلان.</span> |
| `about.status.title` | <span dir="rtl">الحالة والحدود</span> |
| `about.status.1` | <span dir="rtl">القواعد القانونية وفهرس أنواع الدعاوى لم يراجعهما محامٍ ممارس بعد. كل قاعدة معلَّمة «غير مراجَعة» في البيانات وفي التطبيق؛ وجد تدقيق مستقل في أكتوبر 2026 أخطاءً في الفهرس، والتصحيحات جارية.</span> |
| `about.status.2` | <span dir="rtl">نوع وثيقة واحد حتى الآن: عريضة افتتاح الدعوى. القانون المدني والقانون التجاري وقوانين العمل محمَّلة جزئياً.</span> |
| `about.status.3` | <span dir="rtl">العرض التجريبي على الويب يرسل ما يُكتب فيه إلى نموذج مستضاف: قضايا خيالية فقط. التطبيق المكتبي لا يرسل شيئاً.</span> |
| `about.status.4` | <span dir="rtl">النموذج المحلي هو نقطة الضعف اليوم: أبطأ وأقل دقة من النموذج الإلكتروني. يبقى التطبيق المكتبي دون اتصال افتراضياً؛ وسيكون الوضع الإلكتروني خيار المحامي، قضيةً قضية.</span> |
| `about.next.title` | <span dir="rtl">ما التالي</span> |
| `about.next.lead` | <span dir="rtl">الخطة كما هي مكتوبة في المستودع:</span> |
| `about.next.1` | <span dir="rtl">الشهر الأول: وضع إلكتروني لذكاء اصطناعي أقوى، مع إخفاء أسماء الموكّلين على الحاسوب قبل الإرسال ومعاينة ما يُرسل بالضبط؛ وقواعد قانونية قابلة للتنفيذ تُراجَع مع المراجِعة القانونية.</span> |
| `about.next.2` | <span dir="rtl">الشهر الثاني: الإصدار 1.0 ببرنامج تثبيت لويندوز، مع القانون المدني الكامل ونصوص راجعها محامٍ ممارس.</span> |
| `about.next.3` | <span dir="rtl">الشهر الثالث: تجربة في خمسة إلى عشرة مكاتب محاماة، لقياس الوقت الموفَّر والعيوب المكتشفة.</span> |

141 strings.

