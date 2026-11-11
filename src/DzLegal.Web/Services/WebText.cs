namespace DzLegal.Web.Services;

/// <summary>
/// Strings that exist only on the web demo (banner, guide, sample picker). Everything else reuses the desktop
/// app's translated strings through <c>Loc</c>.
/// </summary>
public static class WebText
{
    private static readonly Dictionary<string, (string En, string Ar)> Strings = new(StringComparer.Ordinal)
    {
        ["DemoChip"] = ("Demo · fictitious cases only", "عرض تجريبي · قضايا وهمية فقط"),
        ["BackToSite"] = ("Back to the website", "العودة إلى الموقع"),
        ["Title"] = ("DZ Legal Assistant · demo", "مساعد صياغة العرائض · عرض تجريبي"),
        ["Loading"] = ("Loading the demo…", "جارٍ تحميل العرض التجريبي…"),
        ["LoadingHint"] = (
            "The server wakes up on the first visit; this can take up to a minute.",
            "يستيقظ الخادم عند أول زيارة، وقد يستغرق ذلك دقيقة."),
        ["AboutDemo"] = (
            "This demo runs on hosted models (Nemotron 3 Ultra first, then Gemma 4 26B), on fictitious cases only. The form is processed by this server, and AI actions may send it to Google, OpenRouter or Groq; this browser also keeps a recovery copy. The desktop product runs a smaller model offline on the lawyer's computer, where a draft takes about two minutes on an ordinary laptop. The engine that answered is shown under the draft.",
            "يعمل هذا العرض بنماذج مستضافة (Nemotron 3 Ultra أولا، ثم Gemma 4 26B)، على قضايا وهمية فقط. يعالج هذا الخادم الاستمارة، وقد ترسلها عمليات الذكاء الاصطناعي إلى Google أو OpenRouter أو Groq، ويحتفظ المتصفح بنسخة احتياطية. أما المنتج المكتبي فيشغل نموذجا أصغر دون اتصال على حاسوب المحامي، وتستغرق المسودة فيه نحو دقيقتين على حاسوب عادي. يظهر تحت المسودة المحرك الذي أجاب."),
        ["DownloadWindows"] = ("Download for Windows", "تنزيل نسخة Windows"),
        ["DownloadNote"] = (
            "The desktop app runs on the lawyer's own computer: offline with a local model (Ollama and gemma3:4b), or without AI using fill-in templates.",
            "يعمل التطبيق المكتبي على حاسوب المحامي نفسه: دون اتصال بنموذج محلي (Ollama وgemma3:4b)، أو دون ذكاء اصطناعي بالقوالب."),
        ["NoEngine"] = (
            "AI is not configured on this server: the form, the formal check, editing and export work, and drafting uses fill-in templates.",
            "لم يهيأ الذكاء الاصطناعي على هذا الخادم: تعمل الاستمارة والمراجعة الشكلية والتحرير والتصدير، والصياغة بالقوالب."),
        ["SavedCase"] = ("This browser kept your case from {0}.", "احتفظ هذا المتصفح بقضيتك من {0}."),
        ["RestoreCase"] = ("Restore it", "استعادتها"),
        ["DiscardCase"] = ("Discard", "تجاهلها"),
        ["Samples"] = ("Open a sample case", "افتح قضية نموذجية"),
        ["GuideTitle"] = ("Try it in 5 minutes", "جرّبه في خمس دقائق"),
        ["GuideInvite"] = ("New here? Try it in 5 minutes.", "جديد هنا؟ جرّبه في خمس دقائق."),
        ["GuideOpen"] = ("Open the guide", "افتح الدليل"),
        // The next step under the red grounds finding; {0} and {1} are the app's own button names.
        ["NextAnalyse"] = (
            "Next: click here, press “{0}”, confirm the claim type, then “{1}”.",
            "الخطوة التالية: اضغط هنا، ثم «{0}»، وأكّد نوع الدعوى، ثم «{1}»."),
        ["NextDraft"] = ("Next: press “{0}” above the document.", "الخطوة التالية: اضغط «{0}» فوق المسودة."),
        ["DisclosureLine"] = (
            "This demo sends what you type to a hosted model: fictitious cases only.",
            "يرسل هذا العرض ما تكتبه إلى نموذج مستضاف: قضايا وهمية فقط."),
        ["Details"] = ("Details", "التفاصيل"),
        ["GuideHide"] = ("Hide", "إخفاء"),
        ["GuideShow"] = ("Guide", "الدليل"),
        ["More"] = ("More", "المزيد"),
        ["Guide1Title"] = ("Open the sample “Unpaid loan”", "افتح القضية النموذجية «قرض غير مسدد»"),
        ["Guide2Title"] = ("See the rules work, without AI", "شاهد القواعد تعمل، دون ذكاء اصطناعي"),
        ["Guide3Title"] = ("Analyse the facts, with AI", "حلل الوقائع، بالذكاء الاصطناعي"),
        ["Guide4Title"] = ("Draft, with AI", "صغ، بالذكاء الاصطناعي"),
        ["Guide5Title"] = ("Export, before and after the review", "صدّر، قبل المراجعة وبعدها"),
        // The guide's card: one step at a time, and for each step what to use next. {0} and {1} are the app's own button
        // and step names, quoted, so the hint always matches what is on screen.
        ["GuideCount"] = ("Step {0} of {1}", "الخطوة {0} من {1}"),
        ["GuidePoints"] = (
            "The guide lights up what to use next; you press it yourself, and it moves on once you have done the step.",
            "يضيء الدليل ما تستعمله بعد ذلك، وتضغطه أنت بنفسك، فينتقل إلى ما بعده متى أنجزت الخطوة."),
        ["GuideFinish"] = ("Finish", "إنهاء"),
        ["GuideLocked"] = ("Do this step to go on: the guide moves on by itself.", "أنجز هذه الخطوة للمتابعة: ينتقل الدليل وحده إلى ما بعدها."),
        ["GuideRetry"] = (
            "That did not finish this time; the app's own message says why. Press {0} to try again.",
            "لم يكتمل ذلك هذه المرة، ورسالة التطبيق تذكر السبب. اضغط {0} لإعادة المحاولة."),
        ["GuideDoneStep"] = ("This step is done already. Press {0}.", "أُنجزت هذه الخطوة من قبل. اضغط {0}."),
        ["GuideStep"] = ("Open the {0} step.", "افتح خطوة {0}."),
        ["Guide1Pick"] = (
            "Pick {0} in {1}: it opens on arrival, and picking it again starts it afresh. The form fills in, the statement of claim appears on paper and the check shows 8 / 9: the legal grounds are not drafted yet.",
            "اختر {0} من {1}: تُفتح عند الوصول، واختيارها من جديد يبدؤها من أولها. تمتلئ الاستمارة، وتظهر العريضة على الورق، وتعرض المراجعة الشكلية 8 / 9: الأسانيد القانونية لم تُصغ بعد."),
        ["Guide2Clear"] = ("Clear the defendant's domicile field.", "امسح خانة موطن المدعى عليه."),
        ["Guide2Look"] = (
            "The check dropped to {0} and a red card cites art. 15 of the Code of Civil and Administrative Procedure. Export still works: the lawyer decides. Type the domicile back (any address) to go on.",
            "انخفضت المراجعة الشكلية إلى {0}، وتذكر بطاقة حمراء المادة 15 من قانون الإجراءات المدنية والإدارية. يبقى التصدير ممكنا: القرار للمحامي. أعد كتابة الموطن (أي عنوان) للمتابعة."),
        ["Guide3Analyse"] = (
            "Press {0}. The AI reads the facts and proposes a claim type and articles from the catalogue.",
            "اضغط {0}. يقرأ الذكاء الاصطناعي الوقائع ويقترح نوع الدعوى ومواد من الفهرس."),
        ["Guide3Wait"] = ("The AI is reading the facts. Nothing is used before you confirm.", "يقرأ الذكاء الاصطناعي الوقائع. لا يُستعمل شيء قبل تأكيدك."),
        ["Guide3Confirm"] = (
            "Nothing is used yet. If the claim type is right, press {0}: the articles it requires are ticked, and you can change them.",
            "لم يُستعمل شيء بعد. إن كان نوع الدعوى صحيحا فاضغط {0}: تُحدَّد المواد اللازمة له، ويمكنك تغييرها."),
        ["Guide3Articles"] = (
            "No claim type was recognised: tick the articles the statement of claim should cite, then press {0}.",
            "لم يُتعرَّف على نوع الدعوى: حدّد المواد التي تذكرها العريضة، ثم اضغط {0}."),
        ["Guide4Generate"] = (
            "Press {0}. Facts and requests keep your wording; the program writes each article's rule from the catalogue and the AI writes how the facts meet it.",
            "اضغط {0}. تُدرج الوقائع والطلبات كما كتبتها؛ يكتب البرنامج قاعدة كل مادة من الفهرس، ويكتب الذكاء الاصطناعي كيف تنطبق على الوقائع."),
        ["Guide4Wait"] = (
            "The AI is writing the legal grounds on the paper. The legal reasoning stays the lawyer's to verify.",
            "يكتب الذكاء الاصطناعي الأسانيد القانونية على الورق، ويبقى التحقق من الاستدلال القانوني على المحامي."),
        ["Guide5Early"] = (
            "Press {0} now, before ticking the review box: the export is refused and the reason is listed.",
            "اضغط {0} الآن، قبل التأشير على خانة المراجعة: يُرفض التصدير ويُذكر السبب."),
        ["Guide5Tick"] = (
            "Tick {0} under the paper: you take responsibility for the section the AI wrote.",
            "أشّر على {0} تحت الورقة: تتحمل بذلك مضمون القسم الذي كتبه الذكاء الاصطناعي."),
        ["Guide5Again"] = ("Press {0} again: the PDF downloads. {1} works the same way.", "اضغط {0} من جديد: يُنزَّل الملف. و{1} يعمل بالطريقة نفسها."),
        ["Guide5Export"] = ("Press {0}: the PDF downloads. {1} works the same way.", "اضغط {0}: يُنزَّل الملف. و{1} يعمل بالطريقة نفسها."),
        ["Guide5Done"] = (
            "Done: the statement of claim was exported after your review. Press {0} to close the guide.",
            "انتهيت: صُدّرت العريضة بعد مراجعتك. اضغط {0} لإغلاق الدليل."),
        ["SampleOpened"] = ("Sample case opened: {0}. All names and facts are fictitious.", "فتحت القضية النموذجية: {0}. جميع الأسماء والوقائع وهمية."),
        ["Issues"] = ("To fix", "للتصحيح"),
        ["Waiting"] = ("{0} AI request(s) ahead of yours", "{0} طلب قبل طلبك"),
        ["TabCase"] = ("Case", "القضية"),
        ["TabDraft"] = ("Draft", "المسودة"),
        ["TabChecks"] = ("Checks", "المراجعة"),
        ["Theme"] = ("Dark or light", "داكن أو فاتح"),
        ["CopyDone"] = ("Copy text", "نسخ النص"),
        ["ClearBrowserCopy"] = ("Clear this browser's copy", "امسح نسخة المتصفح"),
        ["BrowserCopyCleared"] = ("The copy kept in this browser was deleted.", "حذفت النسخة المحفوظة في هذا المتصفح."),
    };

    public static string Get(string key, string language) =>
        Strings.TryGetValue(key, out var text) ? (language == "en" ? text.En : text.Ar) : $"[{key}]";
}
