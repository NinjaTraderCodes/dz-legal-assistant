# المراجعة اللغوية والقانونية للعربية في الموقع والعرض التجريبي

لخلود عثمان شريف. كل نص عربي يظهر على الموقع العمومي مع مقابله الإنجليزي (المصدر `site/src/i18n/site.ar.json` و`site.en.json`)، ثم نصوص دليل العرض التجريبي (`src/DzLegal.Web/Services/WebText.cs`). الأقواس `{0}` و`{1}` تُستبدل آلياً بأسماء الأزرار أو الأرقام. عمود «ملاحظة» للمراجِعة: تصحيح أو «صحيح».

نصوص التطبيق نفسه (شاشات «حول التطبيق» و«المكتبة» و«المساعدة») ليست هنا: مصدرها `src/DzLegal.Presentation/Resources/Strings.resx` وتُراجَع مع التطبيق.

## التنقل والعناصر المشتركة (navigation, shared)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `skip` | <span dir="rtl">تخطَّ إلى المحتوى</span> | Skip to content | |
| `nav.label` | <span dir="rtl">التنقل الرئيسي</span> | Main navigation | |
| `nav.home` | <span dir="rtl">الرئيسية</span> | Home | |
| `nav.how` | <span dir="rtl">كيف يعمل</span> | How it works | |
| `nav.library` | <span dir="rtl">المكتبة القانونية</span> | Law library | |
| `nav.download` | <span dir="rtl">التحميل</span> | Download | |
| `nav.privacy` | <span dir="rtl">الخصوصية</span> | Privacy | |
| `nav.about` | <span dir="rtl">حول المشروع</span> | About | |
| `nav.demo` | <span dir="rtl">العرض التجريبي</span> | Live demo | |
| `nav.menu` | <span dir="rtl">القائمة</span> | Menu | |
| `theme.dark` | <span dir="rtl">التبديل إلى الوضع الداكن</span> | Switch to dark mode | |
| `theme.light` | <span dir="rtl">التبديل إلى الوضع الفاتح</span> | Switch to light mode | |
| `footer.label` | <span dir="rtl">روابط</span> | Links | |
| `footer.pages` | <span dir="rtl">صفحات الموقع</span> | Site pages | |
| `footer.github` | <span dir="rtl">المستودع على GitHub</span> | Repository on GitHub | |
| `footer.releases` | <span dir="rtl">الإصدارات</span> | Releases | |
| `footer.version` | <span dir="rtl">الإصدار {0}</span> | Version {0} | |
| `footer.licences` | <span dir="rtl">الخطوط والأيقونات المضمّنة ورخصها:</span> | Bundled fonts and icons and their licences: | |
| `source.label` | <span dir="rtl">المصدر:</span> | Source: | |
| `download.title` | <span dir="rtl">على حاسوبك، دون اتصال</span> | On your computer, offline | |
| `download.body` | <span dir="rtl">تطبيق ويندوز يعمل بنموذج Gemma 3 4B محلياً عبر Ollama، أو دون ذكاء اصطناعي بالقوالب الجاهزة.</span> | A Windows app that runs Gemma 3 4B locally through Ollama, or works without AI using ready templates. | |
| `download.req1` | <span dir="rtl">Ollama مع النموذج gemma3:4b (نحو 3.3 جيغابايت)</span> | Ollama with the gemma3:4b model (about 3.3 GB) | |
| `download.req2` | <span dir="rtl">يُنصح بذاكرة 16 جيغابايت؛ تستغرق صياغة الأسانيد نحو دقيقتين على حاسوب محمول عادي</span> | 16 GB of memory recommended; drafting the grounds takes about two minutes on an ordinary laptop | |
| `download.req3` | <span dir="rtl">يعمل دون ذكاء اصطناعي في وضع القوالب</span> | Works without AI in template mode | |
| `download.cta` | <span dir="rtl">تحميل نسخة ويندوز</span> | Download for Windows | |
| `download.note` | <span dir="rtl">الإصدار {0} على GitHub.</span> | Version {0} on GitHub. | |
| `download.more` | <span dir="rtl">التفاصيل والمتطلبات</span> | Details and requirements | |
| `team.title` | <span dir="rtl">الفريق</span> | Team | |
| `team.lead` | <span dir="rtl">كُتب معظم الشيفرة بوكلاء برمجة بالذكاء الاصطناعي، بتوجيه ومراجعة من قائد المشروع، واختُبر البرنامج على حاسوب محمول بلا بطاقة رسومية.</span> | Most of the code was written by AI coding agents, directed and reviewed by the project lead, and the app was tested on a laptop without a graphics card. | |
| `notFound.title` | <span dir="rtl">الصفحة غير موجودة</span> | Page not found | |
| `notFound.body` | <span dir="rtl">ربما تغيّر الرابط. يمكنك العودة إلى الصفحة الرئيسية أو فتح العرض التجريبي.</span> | The link may have changed. You can go back to the home page or open the live demo. | |
| `notFound.home` | <span dir="rtl">الصفحة الرئيسية</span> | Home page | |
| `nav.try` | <span dir="rtl">جرّبه في خمس دقائق</span> | Test it in 5 minutes | |

## الصفحة الرئيسية (home)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.home.description` | <span dir="rtl">مساعد لتحرير عريضة افتتاح الدعوى باللغة العربية، يعمل دون اتصال على حاسوب المحامي ويتحقق من كل بيان إلزامي قبل التصدير.</span> | An assistant that drafts the Algerian statement of claim in Arabic, offline on the lawyer's own computer, and checks every mandatory element before export. | |
| `hero.cta` | <span dir="rtl">جرّب العرض التجريبي</span> | Try the live demo | |
| `hero.download` | <span dir="rtl">تحميل نسخة ويندوز</span> | Download for Windows | |
| `hero.note` | <span dir="rtl">العرض التجريبي يعمل بنموذج مستضاف على قضايا خيالية فقط. المنتج نفسه يعمل دون اتصال.</span> | The demo runs a hosted model on fictitious cases only. The product itself runs offline. | |
| `site.offline` | <span dir="rtl">التطبيق المكتبي يعمل دون اتصال: ملفات الموكّلين لا تغادر حاسوب المحامي.</span> | The desktop app works offline: client files never leave the lawyer's computer. | |
| `problem.heading` | <span dir="rtl">المشكلة</span> | The problem | |
| `problem.title` | <span dir="rtl">بيان واحد ناقص، وتُرفض العريضة شكلاً قبل أن ينظر القاضي في الموضوع.</span> | One missing element, and the statement of claim is rejected on form before a judge reads the merits. | |
| `problem.body` | <span dir="rtl">تبدأ كل دعوى مدنية في الجزائر بعريضة افتتاح الدعوى. يحدّد قانون الإجراءات المدنية والإدارية البيانات التي يجب أن تتضمنها، وإذا غاب بيان واحد منها جاء الحكم بعدم القبول شكلاً. يحرّرها المحامي بيده، ولا يستطيع أن يرسل ملف موكّله إلى خدمة ذكاء اصطناعي في السحابة.</span> | Every civil case in Algeria starts with the statement of claim. The Code of Civil and Administrative Procedure lists what it must contain; miss one element and the court rules it inadmissible on form. Lawyers draft it by hand, and they cannot send a client's file to a cloud AI service. | |
| `problem.finding.title` | <span dir="rtl">اسم المدعى عليه ولقبه وموطنه</span> | Defendant's name, surname and domicile | |
| `problem.finding.article` | <span dir="rtl">المادة 15-3 من قانون الإجراءات المدنية والإدارية</span> | Code of Civil and Administrative Procedure, art. 15-3 | |
| `problem.finding.text` | <span dir="rtl">الموطن غير مذكور: عدم القبول شكلاً.</span> | Domicile missing: inadmissible on form. | |
| `proofs.title` | <span dir="rtl">ما يضمنه البرنامج</span> | What the software guarantees | |
| `proofs.texts.title` | <span dir="rtl">نصوص رسمية</span> | Official texts | |
| `proofs.texts.body` | <span dir="rtl">قانون الإجراءات المدنية والإدارية وقانون الأسرة كاملان، مع مواد مختارة من القانون المدني والقانون التجاري وقانون العمل، كلها مأخوذة من الجريدة الرسمية ومقروءة على صفحتها.</span> | The complete Code of Civil and Administrative Procedure and the complete Family Code, with selected articles of the Civil, Commercial and Labour codes, all taken from the Official Journal and read against its pages. | |
| `proofs.offline.title` | <span dir="rtl">يعمل دون اتصال</span> | Works offline | |
| `proofs.offline.body` | <span dir="rtl">المنفذ الوحيد المسموح به للنموذج هو عنوان الحاسوب نفسه. لا سحابة، ولا إحصاءات استعمال، ولا تحديثات صامتة: بيانات القضية لا تغادر الجهاز.</span> | The only endpoint the model may use is the computer's own address. No cloud, no usage statistics, no silent updates: case data never leaves the machine. | |
| `proofs.review.title` | <span dir="rtl">المراجعة قبل التصدير</span> | Review before export | |
| `proofs.review.body` | <span dir="rtl">كل قسم كتبه الذكاء الاصطناعي يحمل علامة، ولا يُصدَّر الملف بصيغة PDF أو Word قبل أن يراجعه المحامي ويؤكّد مسؤوليته عنه.</span> | Every section written by the AI carries a mark, and no PDF or Word file leaves the app until the lawyer has reviewed each one and taken responsibility for it. | |
| `story.title` | <span dir="rtl">تسع خطوات، وكل بيان إلزامي يُراجَع بالبرمجة لا بالذكاء الاصطناعي.</span> | Nine steps, and every mandatory element is checked by code, not by AI. | |
| `story.lead` | <span dir="rtl">تملأ القضية خطوةً خطوة، ويُبنى نص العريضة على الورق أمامك. المراجعة الشكلية تنبّهك فوراً إلى البيان الناقص وتذكر المادة التي تفرضه.</span> | The case fills in step by step, and the statement of claim takes shape on paper in front of you. The formal check points at the missing element at once and names the article that requires it. | |
| `story.missing` | <span dir="rtl">بيان إلزامي واحد ناقص: العريضة معرّضة للرفض شكلاً.</span> | One mandatory element missing: the statement of claim risks being declared inadmissible on form. | |
| `story.present` | <span dir="rtl">كل البيانات الإلزامية التي يتحقق منها البرنامج متوفرة. هذا لا يعني أن الدعوى مقبولة: الموضوع والاختصاص والآجال من شأن المحامي.</span> | Every mandatory element the app checks is present. That does not make the claim admissible: substance, jurisdiction and time limits remain the lawyer's call. | |
| `story.caption` | <span dir="rtl">لقطة من مكتب العمل: الحالة الشكلية تتغير مع كل حرف تكتبه.</span> | From the workbench: the formal state changes with every letter you type. | |
| `ai.title` | <span dir="rtl">الذكاء الاصطناعي كاتبٌ متأنٍّ، والمحامي هو من يوقّع.</span> | The AI is a careful clerk. The lawyer signs. | |
| `ai.lead` | <span dir="rtl">النموذج يعمل على حاسوبك ولا يرى من القانون إلا ما حُمِّل من الجريدة الرسمية. يقرأ الوقائع ويقترح نوع الدعوى والمواد، ثم يكتب كيف تنطبق المواد التي أشّرت عليها. أما نص المادة فيُقتبس بالبرمجة من النص الرسمي، لا من النموذج.</span> | The model runs on your computer and sees no law beyond what was loaded from the Official Journal. It reads the facts, proposes the claim type and the articles, then writes how the articles you ticked apply. The text of each article is quoted by code from the official source, never by the model. | |
| `ai.stage1.title` | <span dir="rtl">قراءة الوقائع</span> | Reading the facts | |
| `ai.stage1.body` | <span dir="rtl">يحدّد النموذج العلاقة القانونية والالتزام المُخلّ به وما يطلبه المدعي، ويقترح نوع الدعوى من الفهرس دون أن يسمّي أي مادة.</span> | The model states the legal relationship, the obligation breached and what the claimant asks, and proposes a claim type from the catalogue without naming any article. | |
| `ai.stage2.title` | <span dir="rtl">جمع المواد المرشّحة</span> | Gathering candidate articles | |
| `ai.stage2.body` | <span dir="rtl">بلا نموذج: يجمع البرنامج من الفهرس المواد التي يقوم عليها هذا النوع من الدعاوى، ويمتنع عندما لا ينطبق أي نوع.</span> | No model here: the software collects from the catalogue the articles this type of claim rests on, and abstains when no type applies. | |
| `ai.stage3.title` | <span dir="rtl">اختيار المواد</span> | Choosing the articles | |
| `ai.stage3.body` | <span dir="rtl">يختار النموذج من بين المواد المرشّحة فقط، ويذكر دور كل مادة وسبب اختيارها. لا يُؤشَّر على شيء قبل أن يؤكّد المحامي نوع الدعوى.</span> | The model picks only among the candidates and gives each article a role and a reason. Nothing is ticked until the lawyer confirms the claim type. | |
| `ai.measure` | <span dir="rtl">على مجموعة اختبار مخفيّة من 90 قضية خيالية (31 أكتوبر 2026)، أصاب النموذج الإلكتروني نوع الدعوى في 85 من 90 (94٪) وأصاب القضية كاملةً في 60 من 90 (67٪) في وضع القراءة. وأصاب النموذج المحلي Gemma 3 4B القضية كاملةً في 32 من 90 (36٪) مع إعطائه نوع الدعوى.</span> | On a hidden test set of 90 fictitious cases (31 October 2026), the online model found the right claim type in 85 of 90 (94%) and got the whole case right in 60 of 90 (67%) in read mode. The local Gemma 3 4B got the whole case right in 32 of 90 (36%) with the claim type given. | |
| `ai.measureNote` | <span dir="rtl">المصدر: docs/HIDDEN-SET.md في المستودع. تقيس هذه الأرقام قراءة الوقائع واختيار المواد على قضايا خيالية، لا الصحة القانونية للعريضة.</span> | Source: docs/HIDDEN-SET.md in the repository. These figures measure fact reading and article selection on fictitious cases, not the legal soundness of a statement of claim. | |
| `ai.sign` | <span dir="rtl">المحامي يراجع، والمحامي يوقّع.</span> | The lawyer reviews, and the lawyer signs. | |
| `demo.title` | <span dir="rtl">جرّبه الآن من المتصفح</span> | Try it now in the browser | |
| `demo.body` | <span dir="rtl">يشغّل العرض التجريبي الشاشات نفسها والمكتبة القانونية نفسها، بنموذج مستضاف أكبر من نموذج الحاسوب، على أربع قضايا خيالية. لا تُدخل فيه بيانات حقيقية: المنتج الحقيقي يعمل دون اتصال على حاسوبك.</span> | The demo runs the same screens and the same law library, with a hosted model larger than the desktop's, on four fictitious cases. Enter no real data: the real product runs offline on your computer. | |
| `demo.cta` | <span dir="rtl">افتح العرض التجريبي</span> | Open the live demo | |

## جرّبه في خمس دقائق (/try/)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.try.description` | <span dir="rtl">خمس خطوات لتجربة مساعد صياغة العرائض في العرض التجريبي: افتح قضية نموذجية، شاهد المراجعة الشكلية، حلّل الوقائع، صغ الأسانيد، وصدّر قبل المراجعة وبعدها.</span> | Five steps to test DZ Legal Assistant in the live demo: open a sample, watch the formal check, analyse the facts, draft the grounds, export before and after the review. | |
| `try.eyebrow` | <span dir="rtl">للمحكّمين والمراجعين</span> | For reviewers and judges | |
| `try.title` | <span dir="rtl">جرّبه في خمس دقائق</span> | Test it in 5 minutes | |
| `try.lead` | <span dir="rtl">خمس خطوات في العرض التجريبي، مع ما تضغطه، وما ينبغي أن تراه، ولماذا يهم المحامي. يشغّل العرض الشاشات نفسها التي في التطبيق المكتبي بنموذج مستضاف: بيانات وهمية فقط.</span> | Five steps in the live demo, with what to click, what you should see and why it matters for a lawyer. The demo runs the same screens as the desktop app, with a hosted model: fictitious data only. | |
| `try.full` | <span dir="rtl">الدليل الكامل</span> | The full guide | |
| `try.click` | <span dir="rtl">اضغط</span> | Click | |
| `try.expect` | <span dir="rtl">ما ستراه</span> | You should see | |
| `try.time` | <span dir="rtl">المدة</span> | How long | |
| `try.why` | <span dir="rtl">لماذا يهم المحامي</span> | Why it matters | |
| `try.step1.title` | <span dir="rtl">افتح القضية النموذجية {0}</span> | Open the sample {0} | |
| `try.step1.click` | <span dir="rtl">في الشريط العلوي، افتح {0} واختر {1}.</span> | In the top bar, open {0} and choose {1}. | |
| `try.step1.expect` | <span dir="rtl">تمتلئ الخطوات التسع في {0}، وتظهر العريضة على الورق تحت {1}، وتعرض {2} النتيجة 8 / 9: كل البيانات موجودة ما عدا الأسانيد القانونية التي لم تُصغ بعد.</span> | The nine steps of {0} fill in, the statement of claim appears on paper under {1}, and {2} shows 8 / 9: every element is present except the legal grounds, not drafted yet. | |
| `try.step1.time` | <span dir="rtl">ثوانٍ قليلة. لا يُرسل شيء إلى الذكاء الاصطناعي.</span> | A few seconds. Nothing is sent to the AI. | |
| `try.step1.why` | <span dir="rtl">الاستمارة والورقة والمراجعة شيء واحد: كل بيان يفرضه القانون يُتابَع من أول حرف تكتبه.</span> | The form, the paper and the check are one object: every element the Code requires is tracked from the first keystroke. | |
| `try.step2.title` | <span dir="rtl">امسح موطن المدعى عليه</span> | Clear the defendant's address | |
| `try.step2.click` | <span dir="rtl">افتح خطوة المدعى عليه (أو اضغط {0} في الشريط العلوي ثم {1})، وامسح خانة الموطن، ثم انظر إلى {2}.</span> | Open the Defendant step (or press {0} in the top bar, then {1}), clear the domicile field, and look at {2}. | |
| `try.step2.expect` | <span dir="rtl">تنخفض النتيجة إلى 7 / 9، وتسمّي بطاقة حمراء موطن المدعى عليه وتذكر المادة 15-3 من قانون الإجراءات المدنية والإدارية. لا تمنع المراجعة الشكلية {0}: القرار للمحامي، والبطاقة تبقى حتى يعود الموطن. أعده: تعود النتيجة إلى 8 / 9.</span> | The score drops to 7 / 9 and a red card names the defendant's domicile and cites CPCA art. 15-3. The formal check does not block {0}: the lawyer decides, and the card stays until the address is back. Put it back: 8 / 9 again. | |
| `try.step2.time` | <span dir="rtl">أقل من دقيقة. دون ذكاء اصطناعي.</span> | Under a minute. No AI. | |
| `try.step2.why` | <span dir="rtl">عريضة ينقصها بيان إلزامي واحد قد يُحكم بعدم قبولها شكلاً قبل أن ينظر فيها أي قاضٍ. هذه المراجعة برمجة لا ذكاء اصطناعي، وتعمل كذلك دون اتصال.</span> | A statement of claim missing one mandatory element can be declared inadmissible on form before any judge reads it. This check is code, not AI, and works the same offline. | |
| `try.step3.title` | <span dir="rtl">{0}</span> | {0} | |
| `try.step3.click` | <span dir="rtl">افتح خطوة الأسانيد القانونية ({0} في الدليل) واضغط {1}.</span> | Open the Legal grounds step ({0} in the guide) and press {1}. | |
| `try.step3.expect` | <span dir="rtl">قراءة الذكاء الاصطناعي للوقائع (العلاقة القانونية، والالتزام المخل به، والمطلوب) والسؤال «هل هذا هو نوع الدعوى؟». لا يُؤشَّر على شيء قبل أن تضغط {0}؛ عندها تُؤشَّر المواد التي تقوم عليها هذه الدعوى، كل مادة بنصها الرسمي ومصدرها.</span> | The AI's reading of the facts (the legal relationship, the obligation breached, what is sought) and the question “Is this the type of claim?”. Nothing is ticked until you press {0}; then the articles this claim rests on are ticked, each with its official text and its source. | |
| `try.step3.time` | <span dir="rtl">في جولة المجموعة المخفية بتاريخ 31 أكتوبر 2026، قرأ النموذج الإلكتروني القضية في 8 ثوانٍ في المتوسط؛ وتعرض لوحة النشاط مؤقتاً.</span> | In the 31 October 2026 hidden-set run, the online model read a case in 8 s on average; the activity panel shows a timer. | |
| `try.step3.why` | <span dir="rtl">المحامي هو من يؤكد التكييف القانوني قبل أن تُستعمل أي مادة.</span> | The lawyer confirms the legal qualification before any article is used. | |
| `try.step4.title` | <span dir="rtl">{0}</span> | {0} | |
| `try.step4.click` | <span dir="rtl">تحت {0}، اضغط {1}.</span> | Under {0}, press {1}. | |
| `try.step4.expect` | <span dir="rtl">يُكتب قسم الأسانيد القانونية: يقتبس البرنامج قاعدة كل مادة مؤشَّر عليها من النص الرسمي، ويكتب الذكاء الاصطناعي كيف تنطبق على الوقائع. يحمل القسم العلامة {0} ومربع {1} تحته. الفقرات التي تخفق في الفحوص (رقم غير وارد في الوقائع، أو قانون لم يقدمه المحامي) تُعلَّم أو تُحذف، وتبيّن ملاحظة كهرمانية ما حدث.</span> | The Legal grounds section is written: the program quotes each ticked article's rule from the official text, the AI writes how the facts meet it. The section is marked {0} with the box {1} under it. Paragraphs that fail the checks (a number not in the facts, law the lawyer did not give) are flagged or removed, and an amber note says what happened. | |
| `try.step4.time` | <span dir="rtl">تعرض لوحة النشاط الوقت المنقضي. لا يوجد قياس منشور لمدة صياغة النموذج المستضاف، فلا نَعِد هنا بشيء.</span> | The activity panel shows the elapsed time. No published measurement exists for the hosted model's drafting time, so none is promised here. | |
| `try.step4.why` | <span dir="rtl">القاعدة من النص الرسمي والذكاء الاصطناعي يطبّقها فقط؛ والمحامي يراجع كل سطر كتبه الذكاء الاصطناعي.</span> | The rule comes from the official text and the AI only applies it; the lawyer reviews every AI-written line. | |
| `try.step5.title` | <span dir="rtl">صدّر قبل المراجعة وبعدها</span> | Export before and after the review | |
| `try.step5.click` | <span dir="rtl">اضغط {0} قبل التأشير على المربع. ثم أشّر على {1} واضغط {0} من جديد.</span> | Press {0} before ticking the box. Then tick {1} and press {0} again. | |
| `try.step5.expect` | <span dir="rtl">أولاً، تعرض قائمة {0} القسم الذي كتبه الذكاء الاصطناعي ولم يُراجَع، ولا يُنزَّل شيء. بعد التأشير، يُنزَّل ملف PDF (و{1} يعمل بالطريقة نفسها)؛ وتعرض {2} الصفحات أولاً إن شئت.</span> | First, {0} lists the unreviewed AI section and nothing downloads. After the tick, the PDF downloads ({1} works the same way); {2} shows the pages first if you like. | |
| `try.step5.time` | <span dir="rtl">ثوانٍ قليلة.</span> | A few seconds. | |
| `try.step5.why` | <span dir="rtl">لا تخرج عريضة من التطبيق وفيها قسم من الذكاء الاصطناعي لم يُراجَع: المحامي هو من يوقّع.</span> | No statement of claim leaves the app with an unreviewed AI section: the lawyer signs. | |
| `try.note.title` | <span dir="rtl">قبل أن تبدأ</span> | Before you start | |
| `try.note.1` | <span dir="rtl">قد تستغرق الزيارة الأولى دقيقة: يستيقظ الخادم المجاني عند الطلب، وتقول الصفحة ذلك أثناء التحميل.</span> | The first visit can take up to a minute: the free server wakes up on demand, and the page says so while it loads. | |
| `try.note.2` | <span dir="rtl">بيانات وهمية فقط. ما تكتبه يُرسل إلى نموذج مستضاف؛ والقضايا النموذجية الأربع مختلقة.</span> | Fictitious data only. What you type is sent to a hosted model; the four sample cases are invented. | |
| `try.note.3` | <span dir="rtl">استدعاءات الذكاء الاصطناعي محدودة: 60 لكل جلسة متصفح و150 لكل عنوان شبكة في الساعة. المراجعة الشكلية والتحرير والتصدير غير محدودة.</span> | AI calls are limited: 60 per browser session and 150 per network address an hour. The formal check, editing and export are not limited. | |
| `try.same` | <span dir="rtl">دليل العرض نفسه ({0} في شريطه العلوي) يصف الخطوات الخمس نفسها بأسماء الأزرار نفسها.</span> | The demo's own guide ({0} in its top bar) describes the same five steps with the same button names. | |

## التحميل (/download/)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.download.description` | <span dir="rtl">تحميل مساعد صياغة العرائض لويندوز 10 أو 11: تطبيق مكتبي يعمل دون اتصال لتحرير عريضة افتتاح الدعوى، مع نموذج ذكاء اصطناعي محلي اختياري عبر Ollama.</span> | Download DZ Legal Assistant for Windows 10 or 11: an offline desktop app for the Algerian statement of claim, with an optional local AI model through Ollama. | |
| `dl.eyebrow` | <span dir="rtl">التحميل</span> | Download | |
| `dl.title` | <span dir="rtl">التطبيق المكتبي لويندوز</span> | The desktop app for Windows | |
| `dl.lead` | <span dir="rtl">المنتج الذي يستعمله المحامون. يعمل دون اتصال على حاسوبك: ملفات الموكّلين لا تغادره، والذكاء الاصطناعي، إن أردته، يعمل محلياً هو أيضاً.</span> | The product lawyers use. It runs offline on your own computer: client files never leave it, and the AI, when you want it, runs locally too. | |
| `dl.cta` | <span dir="rtl">تحميل نسخة ويندوز (ملف zip)</span> | Download for Windows (zip) | |
| `dl.releases` | <span dir="rtl">كل الإصدارات على GitHub</span> | All releases on GitHub | |
| `dl.version` | <span dir="rtl">الإصدار {0}، منشور على GitHub.</span> | Version {0}, published on GitHub. | |
| `dl.steps.title` | <span dir="rtl">التثبيت في ثلاث خطوات</span> | Install in three steps | |
| `dl.step1` | <span dir="rtl">نزّل ملف zip من آخر إصدار.</span> | Download the zip from the latest release. | |
| `dl.step2` | <span dir="rtl">فكّ الضغط في أي مجلد. لا برنامج تثبيت: المجلد يحوي كل ما يلزم.</span> | Unzip it into any folder. No installer: the folder holds everything. | |
| `dl.step3` | <span dir="rtl">شغّل DzLegalAssistant.exe. في أول تشغيل: شاشة ترحيب، ثم إعداد في ثلاث خطوات (المحامي، المكتب، التفضيلات) يملأ خانة التوقيع في كل عريضة جديدة، ثم شاشة البداية.</span> | Run DzLegalAssistant.exe. On first launch: a welcome screen, then a three-step setup (the lawyer, the office, preferences) that fills the signature block of every new statement of claim, then the start screen. | |
| `dl.step4` | <span dir="rtl">دون نموذج ذكاء اصطناعي، اختر المحرّك «دون ذكاء اصطناعي (قوالب جاهزة)»: الاستمارة والمراجعة الشكلية والمكتبة القانونية والتصدير تعمل كلها.</span> | Without an AI model, choose the engine “Without AI (fill-in templates)”: the form, the formal check, the law library and the exports all work. | |
| `dl.req.title` | <span dir="rtl">المتطلبات</span> | Requirements | |
| `dl.req.os` | <span dir="rtl">ويندوز 10 أو 11.</span> | Windows 10 or 11. | |
| `dl.req.runtime` | <span dir="rtl">لا حاجة إلى تثبيت .NET: الحزمة تحمل بيئة التشغيل والنصوص القانونية والخطوط.</span> | No .NET to install: the package carries its runtime, the legal texts and the fonts. | |
| `dl.req.memory` | <span dir="rtl">دون ذكاء اصطناعي: أي حاسوب حديث. مع النموذج المحلي: 8 جيغابايت من الذاكرة على الأقل (gemma3:4b)، ويُنصح بـ16 جيغابايت.</span> | Without AI: any recent computer. With the local model: 8 GB of memory at least (gemma3:4b), 16 GB recommended. | |
| `dl.ai.title` | <span dir="rtl">النموذج المحلي، اختياري</span> | The local AI model, optional | |
| `dl.ai.body` | <span dir="rtl">يقرأ النموذج الوقائع ويقترح نوع الدعوى والمواد، ثم يصوغ الأسانيد القانونية. يعمل على حاسوبك عبر Ollama. لا يُرفق أيّ منهما بالتطبيق: تثبّتهما أنت وفق شروطهما.</span> | The model reads the facts, proposes the claim type and the articles, then writes the legal grounds. It runs on your computer through Ollama. Neither is bundled with the app: you install them under their own terms. | |
| `dl.ai.step1` | <span dir="rtl">ثبّت Ollama. يعمل كبرنامج محلي على العنوان 127.0.0.1:11434.</span> | Install Ollama. It runs as a local process at 127.0.0.1:11434. | |
| `dl.ai.step2` | <span dir="rtl">نفّذ الأمر ollama pull gemma3:4b (نحو 3.3 جيغابايت، تنزيل لمرة واحدة). بعدها لا حاجة إلى الشبكة.</span> | Run ollama pull gemma3:4b (about 3.3 GB, a one-time download). After that, no network is needed. | |
| `dl.ai.step3` | <span dir="rtl">يبحث التطبيق عن Ollama عند التشغيل وكل 30 ثانية، ويقول بوضوح إن لم يكن يعمل أو إن لم يكن النموذج مثبّتاً.</span> | The app looks for Ollama at start-up and every 30 seconds, and says plainly when it is not running or the model is not installed. | |
| `dl.ai.speed` | <span dir="rtl">على حاسوب محمول عادي بلا بطاقة رسومية (Intel i5-1335U)، يُظهر التسجيل المباشر 3 دقائق و17 ثانية لقراءة الوقائع ودقيقتين و33 ثانية لصياغة الأسانيد. أول طلب بعد التشغيل يحمّل النموذج أيضاً (10 إلى 40 ثانية).</span> | On an ordinary laptop without a graphics card (Intel i5-1335U), the live recording shows 3 min 17 s to read the facts and 2 min 33 s to draft the grounds. The first request after start-up also loads the model (10 to 40 s). | |
| `dl.data.title` | <span dir="rtl">أين تُحفظ بياناتك</span> | Where your data lives | |
| `dl.data.body` | <span dir="rtl">في مجلد المستخدم على هذا الحاسوب فقط: يحوي {0} المسودات والتقييمات وملف المحامي. لا شيء يُزامَن أو يُرسل إلى أي جهة.</span> | In your user folder on this computer only: {0} holds the drafts, the evaluations and the lawyer's profile. Nothing is synchronised or sent anywhere. | |
| `dl.data.more` | <span dir="rtl">كل التفاصيل في صفحة الخصوصية.</span> | Every detail on the privacy page. | |
| `dl.source.title` | <span dir="rtl">من الشيفرة المصدرية</span> | From the source code | |
| `dl.source.body` | <span dir="rtl">مع تثبيت .NET 10 SDK:</span> | With the .NET 10 SDK installed: | |
| `dl.status.title` | <span dir="rtl">أين يقف هذا الإصدار</span> | Where this version stands | |
| `dl.status.1` | <span dir="rtl">القواعد القانونية وفهرس أنواع الدعاوى لم يراجعهما محامٍ ممارس بعد؛ كل قاعدة معلَّمة «غير مراجَعة» في البيانات وفي التطبيق.</span> | The legal rules and the claim catalogue are not yet verified by a practising lawyer; every rule is marked unverified in the data and in the app. | |
| `dl.status.2` | <span dir="rtl">نوع وثيقة واحد حتى الآن: عريضة افتتاح الدعوى.</span> | One document type so far: the statement of claim. | |
| `dl.status.3` | <span dir="rtl">برنامج تثبيت لويندوز مخطَّط للإصدار 1.0.</span> | A Windows installer is planned for version 1.0. | |
| `dl.demo.title` | <span dir="rtl">لا تريد تثبيت شيء؟</span> | Nothing to install? | |
| `dl.demo.body` | <span dir="rtl">العرض التجريبي في المتصفح يشغّل الشاشات نفسها والمكتبة القانونية نفسها بنموذج مستضاف أكبر، على قضايا خيالية فقط.</span> | The live demo in the browser runs the same screens and the same law library with a larger hosted model, on fictitious cases only. | |

## المكتبة القانونية (/law/)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.law.description` | <span dir="rtl">النصوص القانونية الجزائرية الرسمية المحمَّلة في مساعد صياغة العرائض: أيها كامل، وكيف قورن كل نص بالجريدة الرسمية، وأين مصدره.</span> | The official Algerian legal texts loaded in DZ Legal Assistant: which are complete, how each was checked against the Official Journal, and where its source is. | |
| `law.lead` | <span dir="rtl">تعتمد اقتراحات الأسانيد على نسخة محلية من النصوص الرسمية بالعربية. لا يقتبس التطبيق مادةً إلا من هذه المكتبة، لا من ذاكرة النموذج أبداً.</span> | The grounds suggestions rest on a local copy of the official Arabic texts. The app quotes an article only from this library, never from a model's memory. | |
| `law.complete.title` | <span dir="rtl">نصوص كاملة</span> | Complete texts | |
| `law.partial.title` | <span dir="rtl">نصوص جزئية</span> | Partial texts | |
| `law.card.jo` | <span dir="rtl">الجريدة الرسمية</span> | Official Journal | |
| `law.card.amended` | <span dir="rtl">التعديلات المطبَّقة</span> | Amendments applied | |
| `law.card.retrieved` | <span dir="rtl">نُزّل في {0}</span> | Retrieved on {0} | |
| `law.card.file` | <span dir="rtl">الملف الرسمي (PDF)</span> | Official file (PDF) | |
| `law.card.amendmentFile` | <span dir="rtl">ملف التعديل (PDF)</span> | Amendment file (PDF) | |
| `law.card.local` | <span dir="rtl">نسخة محلية</span> | local copy | |
| `law.how.title` | <span dir="rtl">كيف قورن كل نص بمصدره</span> | How each text was checked | |
| `law.notes.title` | <span dir="rtl">ما ينبغي معرفته</span> | What to know | |
| `law.claims.title` | <span dir="rtl">أنواع الدعاوى المغطاة</span> | Claim types covered | |
| `law.claims.lead` | <span dir="rtl">{0} نوعاً من الدعاوى في {1} مجالات قانونية. لكل نوع، يسجّل الفهرس موادَّه المعتادة وطلباته ومستنداته، ويمكن للمحامي مراجعتها.</span> | {0} claim types in {1} areas of law. For each one, the catalogue records its usual articles, requests and documents, which a lawyer can review. | |
| `area.civil` | <span dir="rtl">مدني</span> | Civil | |
| `area.commercial` | <span dir="rtl">تجاري</span> | Commercial | |
| `area.family` | <span dir="rtl">أسرة</span> | Family | |
| `area.social` | <span dir="rtl">عمل</span> | Labour | |
| `law.honest.title` | <span dir="rtl">الحدود، بصراحة</span> | Limits, stated plainly | |
| `law.honest.1` | <span dir="rtl">لم يراجع محامٍ ممارس القواعد القانونية والفهرس بعد. وجد تدقيق مستقل في أكتوبر 2026 أخطاءً في الفهرس، والتصحيحات جارية.</span> | No practising lawyer has verified the legal rules and the catalogue yet. An independent audit in October 2026 found errors in the catalogue; corrections are under way. | |
| `law.honest.2` | <span dir="rtl">القانون المدني والقانون التجاري وقوانين العمل محمَّلة جزئياً، بالمواد التي تعتمد عليها أنواع الدعاوى في التطبيق. القانون المدني الكامل قيد المقارنة بالجريدة الرسمية.</span> | The Civil, Commercial and Labour codes are only partly loaded, with the articles the app's claim types rely on. The complete Civil Code is being checked against the Official Journal. | |
| `law.honest.3` | <span dir="rtl">المواد الملغاة غير مدرجة؛ النص المحمَّل هو النص الساري اليوم.</span> | Repealed articles are not included; the text loaded is the one in force today. | |
| `law.inApp` | <span dir="rtl">في التطبيق، تعرض المكتبة القانونية هذه النصوص نفسها مع البحث برقم المادة أو بكلمة، وزر لنسخ نص المادة، ورابط إلى المصدر الرسمي لكل نص.</span> | In the app, the law library shows these same texts with a search by article number or word, a copy button and a link to the official source of each text. | |

## الخصوصية (/privacy/)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.privacy.description` | <span dir="rtl">ما يفعله مساعد صياغة العرائض على الشبكة: لا شيء ببيانات القضايا. النموذج المحلي يُستدعى على عنوان الحاسوب نفسه فقط، ولا قياس عن بُعد، والعرض التجريبي على الويب مختلف.</span> | What DZ Legal Assistant does on the network: nothing with case data. The local model is reached on the computer's own address only, there is no telemetry, and the web demo is different. | |
| `privacy.eyebrow` | <span dir="rtl">الخصوصية</span> | Privacy | |
| `privacy.lead2` | <span dir="rtl">بيانات الموكّلين مشمولة بالسر المهني. بُني التطبيق المكتبي بحيث لا تغادر أي بيانات قضية حاسوب المحامي.</span> | Client case data is covered by professional secrecy. The desktop app is built so that no case data leaves the lawyer's computer. | |
| `privacy.network.title` | <span dir="rtl">ما يفعله التطبيق المكتبي على الشبكة</span> | What the desktop app does on the network | |
| `privacy.network.col1` | <span dir="rtl">المكوّن</span> | Component | |
| `privacy.network.col2` | <span dir="rtl">الاستعمال الشبكي</span> | Network use | |
| `privacy.row1.c` | <span dir="rtl">محرّك الاستمارة، القوالب، التخزين، الواجهة</span> | Form engine, templates, storage, interface | |
| `privacy.row1.n` | <span dir="rtl">لا شيء.</span> | None. | |
| `privacy.row2.c` | <span dir="rtl">وضع القوالب (دون ذكاء اصطناعي)</span> | Template mode (without AI) | |
| `privacy.row2.n` | <span dir="rtl">لا شيء.</span> | None. | |
| `privacy.row3.c` | <span dir="rtl">النموذج المحلي (Ollama)</span> | Local model (Ollama) | |
| `privacy.row3.n` | <span dir="rtl">HTTP إلى 127.0.0.1:11434 فقط: حوار مع برنامج Ollama على الحاسوب نفسه. الحزم لا تغادر الجهاز أبداً.</span> | HTTP to 127.0.0.1:11434 only: a conversation with the Ollama process on the same computer. Packets never leave the machine. | |
| `privacy.row4.c` | <span dir="rtl">اقتراح الأسانيد، المكتبة القانونية، البحث في المواد</span> | Grounds suggestions, law library, article search | |
| `privacy.row4.n` | <span dir="rtl">لا شيء. النصوص وفهرس الدعاوى مرفقة بالتطبيق.</span> | None. The texts and the claim catalogue ship with the app. | |
| `privacy.row5.c` | <span dir="rtl">روابط «المصدر الرسمي» تحت المواد المقترحة</span> | “Official source” links under suggested articles | |
| `privacy.row5.n` | <span dir="rtl">لا شيء من التطبيق نفسه. عند النقر، يفتحها نظام التشغيل في متصفحك؛ الرابط بيان ثابت، لا يُبنى أبداً من محتوى القضية.</span> | None by the app itself. When you click one, the operating system opens it in your browser; the link is fixed data, never built from case content. | |
| `privacy.row6.c` | <span dir="rtl">القياس عن بُعد، التحليلات، تقارير الأعطال، التحقق من التحديثات</span> | Telemetry, analytics, crash reports, update checks | |
| `privacy.row6.n` | <span dir="rtl">لا وجود لها.</span> | None exist. | |
| `privacy.enforced.title` | <span dir="rtl">كيف تضمن الشيفرة ذلك</span> | How the code enforces it | |
| `privacy.enforced.1` | <span dir="rtl">عميل النموذج لا يقبل إلا عناوين الاسترجاع المحلي الحرفية (127.0.0.0/8 و::1 وlocalhost). أي مضيف آخر يُرفض دون حتى استعلام DNS.</span> | The model client accepts only literal loopback addresses (127.0.0.0/8, ::1, localhost). Any other host is refused without even a DNS lookup. | |
| `privacy.enforced.2` | <span dir="rtl">هذا العميل لا يستعمل وكيلاً (proxy) ولا يتبع أي إعادة توجيه، ويفتح كل اتصال بنفسه إلى عنوان استرجاع محلي، أياً كان المضيف الذي يذكره الطلب.</span> | That client uses no proxy, follows no redirect, and opens every connection itself to a loopback address, whatever host a request names. | |
| `privacy.enforced.3` | <span dir="rtl">نماذج Ollama «السحابية»، التي كانت ستُحيل الطلبات إلى ollama.com، مرفوضة.</span> | Ollama “cloud” models, which would forward prompts to ollama.com, are refused. | |
| `privacy.enforced.4` | <span dir="rtl">شيفرة النموذج المستضاف الخاصة بالعرض التجريبي تعيش في مشروع منفصل لا يشير إليه التطبيق المكتبي؛ اختبار يُفشل البناء إن حدث ذلك يوماً، والنشر بعلامة الاختبار الإلكتروني يفشل كذلك.</span> | The hosted-model code of the web demo lives in a separate project the desktop app does not reference; a test fails the build if it ever does, and publishing with the online testing flag fails too. | |
| `privacy.enforced.5` | <span dir="rtl">التطبيق لا يرسل أي قياس عن بُعد. الاختبارات الآلية تغطي الحارس ودورة صياغة ومراجعة كاملة دون شبكة.</span> | The running app sends no telemetry. Automated tests cover the guard and a full draft-and-check loop with no network. | |
| `privacy.verify.title` | <span dir="rtl">تحقّق بنفسك</span> | Verify it yourself | |
| `privacy.verify.1` | <span dir="rtl">افصل الحاسوب عن كل الشبكات، أو عطّل بطاقة الشبكة.</span> | Disconnect the computer from every network, or disable the adapter. | |
| `privacy.verify.2` | <span dir="rtl">شغّل التطبيق واستعمل وضع القوالب. كل شيء يعمل.</span> | Start the app and use template mode. Everything works. | |
| `privacy.verify.3` | <span dir="rtl">مع تثبيت Ollama وتنزيل النموذج مسبقاً، يعمل النموذج المحلي أيضاً، لأن Ollama نفسه يعمل محلياً.</span> | With Ollama installed and the model pulled beforehand, the local model works too, because Ollama itself runs locally. | |
| `privacy.verify.4` | <span dir="rtl">راقب الاتصالات إن شئت (netstat -ano في ويندوز): الاتصال الوحيد هو إلى 127.0.0.1:11434.</span> | Watch the connections if you like (netstat -ano on Windows): the only one is to 127.0.0.1:11434. | |
| `privacy.storage.title` | <span dir="rtl">أين تُحفظ البيانات</span> | Where data is stored | |
| `privacy.storage.1` | <span dir="rtl">تُحفظ المسودات حيث تختار؛ المجلد الافتراضي هو {0}.</span> | Drafts are saved where you choose; the default folder is {0}. | |
| `privacy.storage.2` | <span dir="rtl">تُحفظ سجلات التقييم في {0}.</span> | Evaluation records go to {0}. | |
| `privacy.storage.3` | <span dir="rtl">ملف المحامي من إعداد أول تشغيل يُحفظ على هذا الحاسوب فقط ولا يُرسل إلى أي جهة.</span> | The lawyer's profile from the first-run setup is stored on this computer only and never sent anywhere. | |
| `privacy.storage.4` | <span dir="rtl">يحفظ التطبيق نسخة استعادة أثناء العمل؛ إن أُغلق فجأة، يعرض عليك استئناف الجلسة. لا شيء يُزامَن.</span> | A recovery copy is kept while you work; after an unexpected close, the app offers to resume. Nothing is synchronised. | |
| `privacy.demo.title` | <span dir="rtl">العرض التجريبي على الويب مختلف</span> | The web demo is different | |
| `privacy.demo.lead` | <span dir="rtl">العرض التجريبي العمومي على /app برنامج منفصل لتجربة المنتج من رابط. الفرق الوحيد هو مكان تشغيل الذكاء الاصطناعي.</span> | The public demo at /app is a separate program for trying the product from a link. The one difference is where the AI runs. | |
| `privacy.demo.1` | <span dir="rtl">ما تكتبه يُرسل إلى نموذج مستضاف (Nemotron 3 Ultra عبر OpenRouter، مع Google AI Studio وGroq بديلين). أدخل قضايا خيالية فقط.</span> | What you type is sent to a hosted model (Nemotron 3 Ultra through OpenRouter, with Google AI Studio and Groq as fallbacks). Enter fictitious cases only. | |
| `privacy.demo.2` | <span dir="rtl">الخادم لا يحتفظ بحسابات ولا يخزّن أي قضية. تُحفظ قضيتك نسخةً للاستعادة في متصفحك أنت لمدة يوم على الأكثر؛ «امسح نسخة المتصفح» يحذفها فوراً.</span> | The server keeps no account and stores no case. Your case is kept as a recovery copy in your own browser for at most a day; “Clear browser copy” deletes it at once. | |
| `privacy.demo.3` | <span dir="rtl">استدعاءات الذكاء الاصطناعي محدودة لكل جلسة متصفح ولكل عنوان شبكة (60 و150 في الساعة)؛ يُستعمل العنوان لهذا العدّ.</span> | AI calls are limited per browser session and per network address (60 and 150 an hour); the address is used for that count. | |
| `privacy.demo.4` | <span dir="rtl">يقول شريط التطبيق «عرض تجريبي · قضايا خيالية فقط»، وتحمل الصفحة علامة noindex.</span> | The app bar says “Demo · fictitious cases only”, and the page carries noindex. | |
| `privacy.demo.5` | <span dir="rtl">يضع العرض التجريبي أيضاً ملف تعريف الأمان المعياري لإطار العمل (يبدأ اسمه بـ .AspNetCore.Antiforgery)، ولا يحمل أي بيانات قضية.</span> | The demo also sets the framework's standard security cookie (its name starts with .AspNetCore.Antiforgery), which holds no case data. | |
| `privacy.site.title` | <span dir="rtl">هذا الموقع</span> | This website | |
| `privacy.site.1` | <span dir="rtl">صفحات ثابتة تُقدَّم من العنوان نفسه الذي يقدّم العرض التجريبي. لا تحليلات، ولا تتبّع، ولا شيء يُحمَّل من مصدر آخر.</span> | Static pages served from the same address as the demo. No analytics, no tracking, nothing loaded from another origin. | |
| `privacy.site.2` | <span dir="rtl">ملفا تعريف (cookies) من الطرف الأول فقط، يُكتبان حين تختار لغة أو مظهراً (dzlegal.lang وdzlegal.theme)، ويبقيان سنة.</span> | Two first-party cookies, written only when you choose a language or a theme (dzlegal.lang, dzlegal.theme), kept for a year. | |
| `privacy.site.3` | <span dir="rtl">الخطوط والأيقونات مرفقة مع رخصها؛ سياسة أمان الصفحة لا تسمح بأي نص برمجي مضمّن ولا بالتأطير.</span> | The fonts and icons are bundled with their licences; the page's security policy allows no inline scripts and no framing. | |
| `privacy.site.4` | <span dir="rtl">روابط GitHub والجريدة الرسمية (joradp.dz) تخرج من هذا الموقع؛ هي روابط عادية لا تُحمَّل إلا حين تنقر عليها.</span> | Links to GitHub and to the Official Journal (joradp.dz) leave this site; they are plain links, loaded only when you click them. | |

## حول المشروع (/about/)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `meta.about.description` | <span dir="rtl">من يبني مساعد صياغة العرائض، وكيف بُني، ومصادره القانونية وتراخيصه ووثائقه. طُوِّر في إطار ThirdUni 2026.</span> | Who builds DZ Legal Assistant, how it was built, its legal sources, licences and documentation. Built as part of ThirdUni 2026. | |
| `about.what.title` | <span dir="rtl">ماذا يفعل، وماذا لا يفعل</span> | What it does, and does not do | |
| `about.what.decide` | <span dir="rtl">التطبيق يصوغ ولا يقرّر. لا يحكم على موضوع الدعوى ولا يضمن نتيجتها، والمحامي يراجع كل عريضة ويوقّعها.</span> | The app drafts; it does not decide. It does not judge the merits or guarantee a result, and the lawyer reviews and signs every statement of claim. | |
| `about.team.did.lead` | <span dir="rtl">حدّد المنتج ومساره القانوني، ووجّه وراجع وكلاء البرمجة بالذكاء الاصطناعي الذين كتبوا معظم الشيفرة، وأجرى القياسات وحملات الاختبار، واختبر التطبيق.</span> | Defined the product and its legal pipeline, directed and reviewed the AI coding agents that wrote most of the code, ran the benchmarks and the test campaigns, and tested the app. | |
| `about.team.did.design` | <span dir="rtl">صمّمت وبنت مسار المحامي في التطبيق المكتبي: شاشة الترحيب، والإعداد، وملف المحامي، وشاشة البداية، والجولة الإرشادية بقضيتها النموذجية، وصفحات المساعدة، والمكتبة القانونية، بالعربية والإنجليزية، في المظهرين الفاتح والداكن.</span> | Designed and built the lawyer's journey in the desktop app: the welcome screen, the setup, the lawyer's profile, the start screen, the guided tour with its sample case, the help pages and the law library, in Arabic and English, light and dark. | |
| `about.team.did.legal` | <span dir="rtl">ماستر في القانون الخاص، تعمل في مكتب محاماة جزائري. راجعت نقاطاً قانونية في الصياغة وأجابت عن أسئلة الفريق في الإجراءات المدنية الجزائرية وأنواع الدعاوى، وهو ما شكّل دعاوى الأسرة.</span> | Master's in private law, works in an Algerian law office. Reviewed legal points of the drafting and answered the team's questions on Algerian civil procedure and claim types, which shaped the family claims. | |
| `about.built.title` | <span dir="rtl">كيف بُني</span> | How it was built | |
| `about.built.ai` | <span dir="rtl">كُتب معظم الشيفرة والاختبارات والوثائق بوكلاء برمجة بالذكاء الاصطناعي، بتوجيه ومراجعة من الفريق: Claude Code (معظم التطبيق)، وOpenAI Codex (مدوّنة النصوص القانونية، وجزء من مكتب العمل، وحالات الاختبار المستقلة)، وOpenCode (شاشات البداية). وأُنشئت صورة الترحيب بـChatGPT. داخل التطبيق المكتبي، الذكاء الاصطناعي الوحيد هو النموذج المحلي (Gemma 3 4B عبر Ollama)؛ أما العرض التجريبي على الويب فيستعمل Nemotron 3 Ultra عبر OpenRouter.</span> | Most of the code, tests and documentation were written by AI coding agents, directed and reviewed by the team: Claude Code (most of the app), OpenAI Codex (the law corpus, part of the workbench, the independent test cases) and OpenCode (the onboarding screens). The welcome image was generated with ChatGPT. Inside the desktop app, the only AI is the local model (Gemma 3 4B through Ollama); the web demo uses Nemotron 3 Ultra through OpenRouter. | |
| `about.built.stack.title` | <span dir="rtl">التقنيات</span> | Tech stack | |
| `about.built.stack` | <span dir="rtl">C# و.NET 10؛ Avalonia (واجهة سطح المكتب)؛ Blazor Server (العرض التجريبي على الويب)؛ QuestPDF (ملفات PDF) وOpen XML SDK (ملفات Word)؛ Scriban (القوالب)؛ Ollama (النموذج المحلي)؛ OpenRouter (النموذج المستضاف للعرض التجريبي).</span> | C# and .NET 10; Avalonia (desktop interface); Blazor Server (web demo); QuestPDF (PDF) and Open XML SDK (Word); Scriban (templates); Ollama (local model); OpenRouter (the web demo's hosted model). | |
| `about.licences.site` | <span dir="rtl">ما يرفقه هذا الموقع:</span> | What this website bundles: | |
| `about.licences.notices` | <span dir="rtl">ملف إشعارات الأطراف الأخرى الكامل للتطبيق المكتبي</span> | The full third-party notices of the desktop app | |
| `about.licences.image` | <span dir="rtl">أنشأها صاحب المشروع بأداة توليد الصور في ChatGPT في أكتوبر 2026؛ لا تمثّل شخصاً أو مكاناً أو علامة حقيقية.</span> | Generated by the project owner with ChatGPT image generation in October 2026; it depicts no real person, place or brand. | |
| `about.docs.title` | <span dir="rtl">الوثائق</span> | Documentation | |
| `about.docs.1` | <span dir="rtl">الوصف التقني الكامل: التحليل، والصياغة، والمراجعات، والتصدير، وإعداد النموذج المحلي.</span> | The full technical description: analysis, drafting, checks, exports, local model set-up. | |
| `about.docs.2` | <span dir="rtl">الوحدات، وتدفّق البيانات، وقرارات التصميم.</span> | Modules, data flow and design decisions. | |
| `about.docs.3` | <span dir="rtl">ماذا يعني «دون اتصال» هنا وكيف تتحقق منه.</span> | What “offline” means here and how to verify it. | |
| `about.docs.4` | <span dir="rtl">النصوص القانونية وكيف قورن كل منها بمصدره.</span> | The law texts and how each was checked. | |
| `about.docs.5` | <span dir="rtl">مجموعة الاختبار المستقلة ونتائجها.</span> | The independent test set and its results. | |
| `about.docs.6` | <span dir="rtl">العرض التجريبي على الويب، وهذا الموقع، وكيف يُشغَّلان.</span> | The web demo, this website and how to run them. | |
| `about.status.title` | <span dir="rtl">الحالة والحدود</span> | Status and limits | |
| `about.status.1` | <span dir="rtl">القواعد القانونية وفهرس أنواع الدعاوى لم يراجعهما محامٍ ممارس بعد. كل قاعدة معلَّمة «غير مراجَعة» في البيانات وفي التطبيق؛ وجد تدقيق مستقل في أكتوبر 2026 أخطاءً في الفهرس، والتصحيحات جارية.</span> | The legal rules and the claim catalogue are not yet verified by a practising lawyer. Every rule is marked unverified in the data and in the app; an independent audit in October 2026 found errors in the catalogue, and corrections are under way. | |
| `about.status.2` | <span dir="rtl">نوع وثيقة واحد حتى الآن: عريضة افتتاح الدعوى. القانون المدني والقانون التجاري وقوانين العمل محمَّلة جزئياً.</span> | One document type so far: the statement of claim. The Civil, Commercial and Labour codes are only partly loaded. | |
| `about.status.3` | <span dir="rtl">العرض التجريبي على الويب يرسل ما يُكتب فيه إلى نموذج مستضاف: قضايا خيالية فقط. التطبيق المكتبي لا يرسل شيئاً.</span> | The web demo sends what is typed to a hosted model: fictitious cases only. The desktop app sends nothing. | |
| `about.status.4` | <span dir="rtl">النموذج المحلي هو نقطة الضعف اليوم: أبطأ وأقل دقة من النموذج الإلكتروني. يبقى التطبيق المكتبي دون اتصال افتراضياً؛ وسيكون الوضع الإلكتروني خيار المحامي، قضيةً قضية.</span> | The local model is the weak point today: slower and less precise than the online model. The desktop app stays offline by default; an online mode will be the lawyer's choice, case by case. | |
| `about.next.title` | <span dir="rtl">ما التالي</span> | What is next | |
| `about.next.lead` | <span dir="rtl">الخطة كما هي مكتوبة في المستودع:</span> | The plan, as written in the repository: | |
| `about.next.1` | <span dir="rtl">الشهر الأول: وضع إلكتروني لذكاء اصطناعي أقوى، مع إخفاء أسماء الموكّلين على الحاسوب قبل الإرسال ومعاينة ما يُرسل بالضبط؛ وقواعد قانونية قابلة للتنفيذ تُراجَع مع المراجِعة القانونية.</span> | Month 1: an online mode for stronger AI, with client names hidden on the laptop before sending and a preview of exactly what is sent; executable legal rules reviewed with the legal reviewer. | |
| `about.next.2` | <span dir="rtl">الشهر الثاني: الإصدار 1.0 ببرنامج تثبيت لويندوز، مع القانون المدني الكامل ونصوص راجعها محامٍ ممارس.</span> | Month 2: version 1.0 as a Windows installer, with the complete Civil Code and the texts reviewed by a practising lawyer. | |
| `about.next.3` | <span dir="rtl">الشهر الثالث: تجربة في خمسة إلى عشرة مكاتب محاماة، لقياس الوقت الموفَّر والعيوب المكتشفة.</span> | Month 3: a pilot in five to ten law offices, measuring the time saved and the defects caught. | |

## العرض التجريبي: الدليل والتنبيهات (WebText.cs)

| المفتاح | العربية | English | ملاحظة |
|---|---|---|---|
| `DemoChip` | <span dir="rtl">عرض تجريبي · قضايا وهمية فقط</span> | Demo · fictitious cases only | |
| `Title` | <span dir="rtl">مساعد صياغة العرائض · عرض تجريبي</span> | DZ Legal Assistant · demo | |
| `Loading` | <span dir="rtl">جارٍ تحميل العرض التجريبي…</span> | Loading the demo… | |
| `LoadingHint` | <span dir="rtl">يستيقظ الخادم عند أول زيارة، وقد يستغرق ذلك دقيقة.</span> | The server wakes up on the first visit; this can take up to a minute. | |
| `AboutDemo` | <span dir="rtl">يعمل هذا العرض بنماذج مستضافة (Nemotron 3 Ultra أولا، ثم Gemma 4 26B)، على قضايا وهمية فقط. يعالج هذا الخادم الاستمارة، وقد ترسلها عمليات الذكاء الاصطناعي إلى Google أو OpenRouter أو Groq، ويحتفظ المتصفح بنسخة احتياطية. أما المنتج المكتبي فيشغل نموذجا أصغر دون اتصال على حاسوب المحامي، وتستغرق المسودة فيه نحو دقيقتين على حاسوب عادي. يظهر تحت المسودة المحرك الذي أجاب.</span> | This demo runs on hosted models (Nemotron 3 Ultra first, then Gemma 4 26B), on fictitious cases only. The form is processed by this server, and AI actions may send it to Google, OpenRouter or Groq; this browser also keeps a recovery copy. The desktop product runs a smaller model offline on the lawyer's computer, where a draft takes about two minutes on an ordinary laptop. The engine that answered is shown under the draft. | |
| `DownloadWindows` | <span dir="rtl">تنزيل نسخة Windows</span> | Download for Windows | |
| `DownloadNote` | <span dir="rtl">يعمل التطبيق المكتبي على حاسوب المحامي نفسه: دون اتصال بنموذج محلي (Ollama وgemma3:4b)، أو دون ذكاء اصطناعي بالقوالب.</span> | The desktop app runs on the lawyer's own computer: offline with a local model (Ollama and gemma3:4b), or without AI using fill-in templates. | |
| `NoEngine` | <span dir="rtl">لم يهيأ الذكاء الاصطناعي على هذا الخادم: تعمل الاستمارة والمراجعة الشكلية والتحرير والتصدير، والصياغة بالقوالب.</span> | AI is not configured on this server: the form, the formal check, editing and export work, and drafting uses fill-in templates. | |
| `SavedCase` | <span dir="rtl">احتفظ هذا المتصفح بقضيتك من {0}.</span> | This browser kept your case from {0}. | |
| `RestoreCase` | <span dir="rtl">استعادتها</span> | Restore it | |
| `DiscardCase` | <span dir="rtl">تجاهلها</span> | Discard | |
| `Samples` | <span dir="rtl">افتح قضية نموذجية</span> | Open a sample case | |
| `GuideTitle` | <span dir="rtl">جرّبه في خمس دقائق</span> | Try it in 5 minutes | |
| `GuideInvite` | <span dir="rtl">جديد هنا؟ جرّبه في خمس دقائق.</span> | New here? Try it in 5 minutes. | |
| `GuideOpen` | <span dir="rtl">افتح الدليل</span> | Open the guide | |
| `DisclosureLine` | <span dir="rtl">يرسل هذا العرض ما تكتبه إلى نموذج مستضاف: قضايا وهمية فقط.</span> | This demo sends what you type to a hosted model: fictitious cases only. | |
| `Details` | <span dir="rtl">التفاصيل</span> | Details | |
| `GuideHide` | <span dir="rtl">إخفاء</span> | Hide | |
| `GuideShow` | <span dir="rtl">الدليل</span> | Guide | |
| `More` | <span dir="rtl">المزيد</span> | More | |
| `Guide1Title` | <span dir="rtl">افتح القضية النموذجية «قرض غير مسدد»</span> | Open the sample “Unpaid loan” | |
| `Guide1` | <span dir="rtl">اختره من «افتح قضية نموذجية» (القضايا الأربع وهمية). تمتلئ الاستمارة، وتظهر العريضة على الورق، وتعرض المراجعة الشكلية 8 / 9: الأسانيد القانونية لم تُصغ بعد.</span> | Pick it in “Open a sample case” (all four samples are fictitious). The form fills in, the statement of claim appears on paper and the formal check shows 8 / 9: the legal grounds are not drafted yet. | |
| `Guide2Title` | <span dir="rtl">شاهد القواعد تعمل، دون ذكاء اصطناعي</span> | See the rules work, without AI | |
| `Guide2` | <span dir="rtl">امسح موطن المدعى عليه: تنخفض المراجعة الشكلية إلى 7 / 9، وتذكر بطاقة حمراء المادة 15 من قانون الإجراءات المدنية والإدارية. يبقى التصدير ممكناً (القرار للمحامي)، لكن البطاقة تبقى حتى تعيده.</span> | Clear the defendant's domicile: the formal check drops to 7 / 9 and a red card cites the Code of Civil and Administrative Procedure, art. 15. Export still works (the lawyer decides), but the card stays until you put it back. | |
| `Guide3Title` | <span dir="rtl">حلل الوقائع، بالذكاء الاصطناعي</span> | Analyse the facts, with AI | |
| `Guide3` | <span dir="rtl">اضغط «تحليل الوقائع». يقرأ الذكاء الاصطناعي الوقائع ويقترح نوع الدعوى ومواد من الفهرس. لا يُستعمل شيء قبل أن تضغط «نعم، هذا نوع الدعوى»: عند التأكيد تحدد المواد اللازمة له، ويمكنك تغييرها.</span> | Press “Analyse the facts”. The AI reads the facts and proposes a claim type and articles from the catalog. Nothing is used before you press “Yes, this is the claim type”: confirming ticks the articles it requires, and you can change them. | |
| `Guide4Title` | <span dir="rtl">صغ، بالذكاء الاصطناعي</span> | Draft, with AI | |
| `Guide4` | <span dir="rtl">اضغط «صياغة الأسانيد». تدرج الوقائع والطلبات كما كتبتها؛ يكتب البرنامج قاعدة كل مادة من الفهرس، ويكتب الذكاء الاصطناعي كيف تنطبق على الوقائع. تفحص الأداة بعض الأخطاء، ويبقى التحقق من الاستدلال القانوني على المحامي. أشر على «راجعت هذا القسم» تحت الأسانيد.</span> | Press “Draft legal grounds”. Facts and requests use your own wording; the program writes each article's rule from the catalog and the AI writes how the facts meet it. Checks catch some errors; the legal reasoning stays the lawyer's to verify. Tick “reviewed” under the grounds. | |
| `Guide5Title` | <span dir="rtl">صدّر، قبل المراجعة وبعدها</span> | Export, before and after the review | |
| `Guide5` | <span dir="rtl">اضغط «تصدير PDF» قبل أن تؤشر على «راجعت هذا القسم وأتحمل مضمونه»: يُرفض التصدير ويُذكر السبب. أشّر عليها، واضغط «تصدير PDF» من جديد، فيُنزَّل الملف؛ و«تصدير Word» يعمل بالطريقة نفسها. وتعرض المعاينة الصفحات أولا إن شئت.</span> | Press “Export PDF” before ticking “I have reviewed this section and take responsibility for it”: the export is refused and the reason is listed. Tick it, press “Export PDF” again, and the PDF downloads; “Export Word” works the same way. Preview shows the pages first if you like. | |
| `OpenGrounds` | <span dir="rtl">إلى الأسانيد القانونية</span> | Go to Legal grounds | |
| `OpenDefendant` | <span dir="rtl">إلى المدعى عليه</span> | Go to Defendant | |
| `SampleOpened` | <span dir="rtl">فتحت القضية النموذجية: {0}. جميع الأسماء والوقائع وهمية.</span> | Sample case opened: {0}. All names and facts are fictitious. | |
| `Issues` | <span dir="rtl">للتصحيح</span> | To fix | |
| `Waiting` | <span dir="rtl">{0} طلب قبل طلبك</span> | {0} AI request(s) ahead of yours | |
| `TabCase` | <span dir="rtl">القضية</span> | Case | |
| `TabDraft` | <span dir="rtl">المسودة</span> | Draft | |
| `TabChecks` | <span dir="rtl">المراجعة</span> | Checks | |
| `Theme` | <span dir="rtl">داكن أو فاتح</span> | Dark or light | |
| `CopyDone` | <span dir="rtl">نسخ النص</span> | Copy text | |
| `ClearBrowserCopy` | <span dir="rtl">امسح نسخة المتصفح</span> | Clear this browser's copy | |
| `BrowserCopyCleared` | <span dir="rtl">حذفت النسخة المحفوظة في هذا المتصفح.</span> | The copy kept in this browser was deleted. | |

283 نصاً في المجموع.
