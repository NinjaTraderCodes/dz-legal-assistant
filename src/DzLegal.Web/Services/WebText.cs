namespace DzLegal.Web.Services;

/// <summary>
/// Strings that exist only on the web demo (banner, guided tour, sample picker). Everything else reuses the desktop
/// app's translated strings through <c>Loc</c>.
/// </summary>
public static class WebText
{
    private static readonly Dictionary<string, (string En, string Ar)> Strings = new(StringComparer.Ordinal)
    {
        ["Banner"] = (
            "Public demo · fictitious cases only. AI steps run on a hosted Gemma model; the real product runs the same model offline on the lawyer's laptop.",
            "عرض تجريبي عام · قضايا وهمية فقط. خطوات الذكاء الاصطناعي تعمل على نموذج Gemma مستضاف؛ أما المنتج الفعلي فيشغّل النموذج نفسه دون اتصال على حاسوب المحامي."),
        ["NoEngine"] = (
            "No AI engine is configured on this server: the rule check, editing and export work; drafting uses template mode.",
            "لا يوجد محرك ذكاء اصطناعي مهيأ على هذا الخادم: المراجعة الشكلية والتحرير والتصدير تعمل، والصياغة بوضع القوالب."),
        ["Samples"] = ("Open a sample case", "افتح قضية نموذجية"),
        ["GuideTitle"] = ("Try it in 3 minutes", "جرّبه في 3 دقائق"),
        ["GuideHide"] = ("Hide the guide", "إخفاء الدليل"),
        ["GuideShow"] = ("Guide", "الدليل"),
        ["Guide1"] = ("Open a sample case (fictitious). The form fills in; the formal check reads 10 / 12, because the legal grounds and the requests are not written yet.",
            "افتح قضية نموذجية (وهمية). يمتلئ النموذج وتظهر المراجعة الشكلية 10 / 12 لأن الأسانيد والطلبات لم تُكتب بعد."),
        ["Guide2"] = ("Rule engine, no AI: in step 3 (Defendant), clear the address. The check drops and cites CPCA art. 15. Undo it.",
            "محرك القواعد دون ذكاء اصطناعي: في الخطوة 3 (المدعى عليه) امسح العنوان؛ تنخفض النتيجة وتذكر المادة 15 ق.إ.م.إ. ثم أعده."),
        ["Guide3"] = ("AI step 1: go to Legal grounds and click “Analyse the facts”. Watch the model read the facts, then tick the articles to keep.",
            "خطوة الذكاء الاصطناعي 1: انتقل إلى الأسانيد القانونية واضغط «حلّل الوقائع». تابع النموذج وهو يقرأ الوقائع، ثم اختر المواد."),
        ["Guide4"] = ("AI step 2: click “Draft narrative sections”. The legal discussion and the requests are written live, from the ticked articles only, and the check reaches 12 / 12.",
            "خطوة الذكاء الاصطناعي 2: اضغط «صياغة الأقسام السردية». تُكتب المناقشة القانونية والطلبات مباشرة من المواد المختارة فقط."),
        ["Guide5"] = ("Open Preview, then export the petition to PDF or Word. The document is in formal Arabic; the interface is in English.",
            "افتح المعاينة ثم صدّر العريضة بصيغة PDF أو Word."),
        ["OpenGrounds"] = ("Go to Legal grounds", "إلى الأسانيد القانونية"),
        ["OpenDefendant"] = ("Go to Defendant", "إلى المدعى عليه"),
        ["SampleOpened"] = ("Sample case opened: {0}. All names and facts are fictitious.", "فُتحت القضية النموذجية: {0}. جميع الأسماء والوقائع وهمية."),
        ["Present"] = ("Elements present", "البيانات المتوفرة"),
        ["Issues"] = ("To fix", "للتصحيح"),
        ["AboutDocument"] = ("The petition is drafted in formal Arabic, as Algerian courts require.", "تُحرَّر العريضة بالعربية الفصحى كما تشترط المحاكم الجزائرية."),
        ["Source"] = ("Source code", "الشيفرة المصدرية"),
        ["Engine"] = ("Engine", "المحرك"),
        ["Waiting"] = ("{0} request(s) ahead of yours", "{0} طلب قبل طلبك"),
    };

    public static string Get(string key, string language) =>
        Strings.TryGetValue(key, out var text) ? (language == "en" ? text.En : text.Ar) : $"[{key}]";
}
