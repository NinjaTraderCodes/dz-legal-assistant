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
        ["AboutDemo"] = (
            "This demo runs on hosted models (Gemma 4 26B first, then smaller ones), on fictitious cases only. The form is processed by this server, and AI actions may send it to Google, OpenRouter or Groq; this browser also keeps a recovery copy. The desktop product runs a smaller model offline on the lawyer's computer, where a draft takes about two minutes on an ordinary laptop. The engine that answered is shown under the draft.",
            "يعمل هذا العرض بنماذج مستضافة (Gemma 4 26B أولا، ثم نماذج أصغر)، على قضايا وهمية فقط. يعالج هذا الخادم الاستمارة، وقد ترسلها عمليات الذكاء الاصطناعي إلى Google أو OpenRouter أو Groq، ويحتفظ المتصفح بنسخة احتياطية. أما المنتج المكتبي فيشغل نموذجا أصغر دون اتصال على حاسوب المحامي، وتستغرق المسودة فيه نحو دقيقتين على حاسوب عادي. يظهر تحت المسودة المحرك الذي أجاب."),
        ["NoEngine"] = (
            "AI is not configured on this server: the form, the formal check, editing and export work, and drafting uses fill-in templates.",
            "لم يهيأ الذكاء الاصطناعي على هذا الخادم: تعمل الاستمارة والمراجعة الشكلية والتحرير والتصدير، والصياغة بالقوالب."),
        ["SavedCase"] = ("This browser kept your case from {0}.", "احتفظ هذا المتصفح بقضيتك من {0}."),
        ["RestoreCase"] = ("Restore it", "استعادتها"),
        ["DiscardCase"] = ("Discard", "تجاهلها"),
        ["Samples"] = ("Open a sample case", "افتح قضية نموذجية"),
        ["GuideTitle"] = ("Try it in three minutes", "جربه في ثلاث دقائق"),
        ["GuideHide"] = ("Hide", "إخفاء"),
        ["GuideShow"] = ("Guide", "الدليل"),
        ["Guide1Title"] = ("Open a sample case", "افتح قضية نموذجية"),
        ["Guide1"] = ("All four are fictitious. The form fills in and the formal check updates.", "القضايا الأربع وهمية. تمتلئ الاستمارة وتتحدث المراجعة الشكلية."),
        ["Guide2Title"] = ("See the rules work, without AI", "شاهد القواعد تعمل، دون ذكاء اصطناعي"),
        ["Guide2"] = ("Clear the defendant's address: the formal check drops and cites the Code of Civil Procedure, art. 15. Then put it back.",
            "امسح عنوان المدعى عليه: تنخفض المراجعة الشكلية وتذكر المادة 15 من قانون الإجراءات المدنية والإدارية. ثم أعده."),
        ["Guide3Title"] = ("Analyse the facts, with AI", "حلل الوقائع، بالذكاء الاصطناعي"),
        ["Guide3"] = (
            "The AI reads the facts and proposes a claim type and articles from the catalog. Check the claim type and confirm it: confirming ticks the articles it requires, and you can change them.",
            "يقرأ الذكاء الاصطناعي الوقائع ويقترح نوع الدعوى ومواد من الفهرس. راجع نوع الدعوى ثم أكده: عند التأكيد تحدد المواد اللازمة له، ويمكنك تغييرها."),
        ["Guide4Title"] = ("Draft, with AI", "صغ، بالذكاء الاصطناعي"),
        ["Guide4"] = (
            "Press “Draft legal grounds”. Facts and requests use your own wording; the program writes each article's rule from the catalog and the AI writes how the facts meet it. Checks catch some errors; the legal reasoning stays the lawyer's to verify. Tick “reviewed” under the grounds.",
            "اضغط «صياغة الأسانيد». تدرج الوقائع والطلبات كما كتبتها؛ يكتب البرنامج قاعدة كل مادة من الفهرس، ويكتب الذكاء الاصطناعي كيف تنطبق على الوقائع. تفحص الأداة بعض الأخطاء، ويبقى التحقق من الاستدلال القانوني على المحامي. أشر على «راجعت هذا القسم» تحت الأسانيد."),
        ["Guide5Title"] = ("Preview and export", "عاين وصدر"),
        ["Guide5"] = ("Open Preview, then export the statement of claim to PDF or Word.", "افتح المعاينة، ثم صدر العريضة بصيغة PDF أو Word."),
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
