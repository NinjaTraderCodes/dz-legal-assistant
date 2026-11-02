namespace DzLegal.Web.Services;

/// <summary>
/// Strings that exist only on the web demo (banner, guided tour, sample picker). Everything else reuses the desktop
/// app's translated strings through <c>Loc</c>.
/// </summary>
public static class WebText
{
    private static readonly Dictionary<string, (string En, string Ar)> Strings = new(StringComparer.Ordinal)
    {
        ["DemoChip"] = ("Demo · fictitious cases only", "عرض تجريبي · قضايا وهمية فقط"),
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
        ["DisclosureLine"] = (
            "This demo sends what you type to a hosted model: fictitious cases only.",
            "يرسل هذا العرض ما تكتبه إلى نموذج مستضاف: قضايا وهمية فقط."),
        ["Details"] = ("Details", "التفاصيل"),
        ["GuideHide"] = ("Hide", "إخفاء"),
        ["GuideShow"] = ("Guide", "الدليل"),
        ["More"] = ("More", "المزيد"),
        ["Guide1Title"] = ("Open the sample “Unpaid loan”", "افتح القضية النموذجية «قرض غير مسدد»"),
        ["Guide1"] = (
            "Pick it in “Open a sample case” (all four samples are fictitious). The form fills in, the petition appears on paper and the formal check shows 8 / 9: the legal grounds are not drafted yet.",
            "اختره من «افتح قضية نموذجية» (القضايا الأربع وهمية). تمتلئ الاستمارة، وتظهر العريضة على الورق، وتعرض المراجعة الشكلية 8 / 9: الأسانيد القانونية لم تُصغ بعد."),
        ["Guide2Title"] = ("See the rules work, without AI", "شاهد القواعد تعمل، دون ذكاء اصطناعي"),
        ["Guide2"] = (
            "Clear the defendant's domicile: the formal check drops to 7 / 9 and a red card cites the Code of Civil and Administrative Procedure, art. 15. Export still works (the lawyer decides), but the card stays until you put it back.",
            "امسح موطن المدعى عليه: تنخفض المراجعة الشكلية إلى 7 / 9، وتذكر بطاقة حمراء المادة 15 من قانون الإجراءات المدنية والإدارية. يبقى التصدير ممكناً (القرار للمحامي)، لكن البطاقة تبقى حتى تعيده."),
        ["Guide3Title"] = ("Analyse the facts, with AI", "حلل الوقائع، بالذكاء الاصطناعي"),
        ["Guide3"] = (
            "Press “Analyse the facts”. The AI reads the facts and proposes a claim type and articles from the catalog. Nothing is used before you press “Yes, this is the claim type”: confirming ticks the articles it requires, and you can change them.",
            "اضغط «تحليل الوقائع». يقرأ الذكاء الاصطناعي الوقائع ويقترح نوع الدعوى ومواد من الفهرس. لا يُستعمل شيء قبل أن تضغط «نعم، هذا نوع الدعوى»: عند التأكيد تحدد المواد اللازمة له، ويمكنك تغييرها."),
        ["Guide4Title"] = ("Draft, with AI", "صغ، بالذكاء الاصطناعي"),
        ["Guide4"] = (
            "Press “Draft legal grounds”. Facts and requests use your own wording; the program writes each article's rule from the catalog and the AI writes how the facts meet it. Checks catch some errors; the legal reasoning stays the lawyer's to verify. Tick “reviewed” under the grounds.",
            "اضغط «صياغة الأسانيد». تدرج الوقائع والطلبات كما كتبتها؛ يكتب البرنامج قاعدة كل مادة من الفهرس، ويكتب الذكاء الاصطناعي كيف تنطبق على الوقائع. تفحص الأداة بعض الأخطاء، ويبقى التحقق من الاستدلال القانوني على المحامي. أشر على «راجعت هذا القسم» تحت الأسانيد."),
        ["Guide5Title"] = ("Export, before and after the review", "صدّر، قبل المراجعة وبعدها"),
        ["Guide5"] = (
            "Press “Export PDF” before ticking “I have reviewed this section and take responsibility for it”: the export is refused and the reason is listed. Tick it, press “Export PDF” again, and the PDF downloads; “Export Word” works the same way. Preview shows the pages first if you like.",
            "اضغط «تصدير PDF» قبل أن تؤشر على «راجعت هذا القسم وأتحمل مضمونه»: يُرفض التصدير ويُذكر السبب. أشّر عليها، واضغط «تصدير PDF» من جديد، فيُنزَّل الملف؛ و«تصدير Word» يعمل بالطريقة نفسها. وتعرض المعاينة الصفحات أولا إن شئت."),
        ["OpenGrounds"] = ("Go to Legal grounds", "إلى الأسانيد القانونية"),
        ["OpenDefendant"] = ("Go to Defendant", "إلى المدعى عليه"),
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
