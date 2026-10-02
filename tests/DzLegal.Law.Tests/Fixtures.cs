namespace DzLegal.Law.Tests;

/// <summary>
/// FICTITIOUS texts for tests only. They imitate the layout of an official code; they are not real law and are
/// never shipped in data/law.
/// </summary>
internal static class Fixtures
{
    public const string FictitiousCode = """
        قانون تجريبي (نص وهمي للاختبار فقط)
        الكتاب الأول
        أحكام عامة
        الباب الأول
        في الالتزامات التجريبية
        المادة الأولى: يسري هذا النص الوهمي على
        الأطراف التجريبيين فقط.
        المادة 2 : يلتزم الطرف التجريبي بما يأتي:
        1- أن يحترم النص الوهمي،
        2- أن يرد ما اقترضه.
        الفصل الأول
        المادة ٣ - كل نص وهمي يلزم من وقعه.
        المادة 3 مكرر: يضاف هذا الحكم الوهمي بعد المادة الثالثة.
        المادة 3 مكرر 1: وحكم وهمي ثان مكرر.
        الباب الثاني — التعويض التجريبي
        المادة 5: (ملغاة)
        المادة 6: كل خطأ وهمي يسبب ضررا وهميا للغير يلزم من ارتكبه بالتعويض الوهمي.
        """;

    public static LawCode Code(params LawArticle[] articles) => new()
    {
        Id = "fictitious-code",
        Title = new Domain.LocalizedText("قانون تجريبي", "Fictitious code"),
        CitationName = "القانون التجريبي",
        Reference = "نص وهمي للاختبار",
        Source = new LawSource { Url = "fixture" },
        Articles = articles,
    };

    public static LawArticle Article(string number, string text, bool repealed = false) =>
        new() { Number = number, Text = text, Repealed = repealed };
}
