using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Reader.Values;

namespace DzLegal.CoreLogic.Reader.Tests;

/// <summary>The date and amount parsers on the guide's own examples (labelling guide, section 8).</summary>
public sealed class ValueParserTests
{
    private static (string Normalized, string Quote) One(IReadOnlyList<ValueReading> found, string text)
    {
        var value = Assert.Single(found);
        return (value.Normalized, text.Substring(value.Span.Start, value.Span.Length));
    }

    [Theory]
    [InlineData("أبرم العقد في 03/04/2025.", "2025-04-03", "03/04/2025")]  // day before month, always
    [InlineData("بتاريخ 3-4-2025 وقع الطرفان", "2025-04-03", "3-4-2025")]
    [InlineData("بتاريخ 2025/04/03", "2025-04-03", "2025/04/03")]
    [InlineData("وجه إعذارا في 12 مارس 2025", "2025-03-12", "12 مارس 2025")]
    [InlineData("في 12 من شهر مارس 2025 تسلم", "2025-03-12", "12 من شهر مارس 2025")]
    [InlineData("التحق بمنصبه في شهر أوت 2019.", "2019-08", "أوت 2019")]
    [InlineData("منذ سنة 2015", "2015", "2015")]
    [InlineData("في ١٢ جانفي ٢٠٢٦", "2026-01-12", "١٢ جانفي ٢٠٢٦")]  // Arabic-Indic digits alike
    [InlineData("في 5 حزيران 2024", "2024-06-05", "5 حزيران 2024")]  // the Levantine names
    [InlineData("في 5 يونيو 2024", "2024-06-05", "5 يونيو 2024")]
    public void ADateIsReadWithItsNormalFormAndItsExactSpan(string text, string normalized, string quote) =>
        Assert.Equal((normalized, quote), One(DateParser.Read(text, NoteField.Facts), text));

    [Theory]
    [InlineData("لم يتقاض أجره منذ ثلاثة أشهر.")]  // a relative date: not emitted
    [InlineData("بتاريخ 31/02/2025")]  // no such day
    [InlineData("بتاريخ 03/04/25")]  // a two-digit year: never guessed
    [InlineData("القطعة رقم 1205")]
    public void NoDateIsEmittedWhereTheGuideGivesNone(string text) => Assert.Empty(DateParser.Read(text, NoteField.Facts));

    [Theory]
    [InlineData("يطلب الخلع بعشرة ملايين سنتيم", "100000", "عشرة ملايين سنتيم")]  // «بـ» attached to the number (pilot)
    [InlineData("قدر الدين ب500.000 دج", "500000", "500.000 دج")]  // «بـ» attached to the digits
    [InlineData("قيمتها ٤٤٧٬٠٠٠ دينار", "447000", "٤٤٧٬٠٠٠ دينار")]  // the Arabic thousands separator (pilot, 15 November 2026)
    [InlineData("قدره ١٢٣٫٥٠ دج", "123.50", "١٢٣٫٥٠ دج")]  // the Arabic decimal separator
    [InlineData("بقيمة واحدا وعشرين مليونا وتسعمائة ألف سنتيم", "219000", "واحدا وعشرين مليونا وتسعمائة ألف سنتيم")]  // «واحدا»
    [InlineData("تعويض قدره مليون وخمسون ألفا (1 050 000) دينار", "1050000", "مليون وخمسون ألفا (1 050 000) دينار")]  // spaced digits in brackets
    [InlineData("مبلغ 500.000 دج", "500000", "500.000 دج")]
    [InlineData("قدره 1.200.000,00 دج", "1200000", "1.200.000,00 دج")]  // decimals dropped only when zero
    [InlineData("قدره 1250,50 دج", "1250.50", "1250,50 دج")]  // a real fraction keeps two decimals
    [InlineData("قدره مليون ونصف دينار", "1500000", "مليون ونصف دينار")]
    [InlineData("قدره مليون ونصف مليون دينار", "1500000", "مليون ونصف مليون دينار")]  // the unit repeated after «ونصف»
    [InlineData("مبلغ ألف ونصف دينار", "1500", "ألف ونصف دينار")]
    [InlineData("بقيمة 50 مليون سنتيم", "500000", "50 مليون سنتيم")]  // 100 centimes = 1 dinar
    [InlineData("تبلغ 3 ملايير سنتيم", "30000000", "3 ملايير سنتيم")]
    [InlineData("مبلغ خمسمائة ألف (500.000) دينار", "500000", "خمسمائة ألف (500.000) دينار")]  // one amount, quoted whole
    [InlineData("مبلغ ٥٠٠ ألف دج", "500000", "٥٠٠ ألف دج")]
    [InlineData("قدره 500 000 دج", "500000", "500 000 دج")]
    [InlineData("قدره ثلاثمائة وخمسون ألف دينار جزائري", "350000", "ثلاثمائة وخمسون ألف دينار جزائري")]
    [InlineData("قدره 1,5 مليون دج", "1500000", "1,5 مليون دج")]
    public void AnAmountIsReadInDinarsWithItsExactSpan(string text, string normalized, string quote) =>
        Assert.Equal((normalized, quote), One(AmountParser.Read(text, NoteField.Facts), text));

    [Theory]
    [InlineData("الفاتورة رقم 4512 بتاريخ قديم")]  // a number that is not money
    [InlineData("مساحة 120 متر مربع")]
    [InlineData("مبلغ 5000 يورو")]  // a foreign currency: not emitted
    public void NoAmountIsEmittedWithoutTheDinarOrTheCentime(string text) => Assert.Empty(AmountParser.Read(text, NoteField.Facts));

    [Fact]
    public void TwoAmountsInOneSentenceAreTwoValues()
    {
        const string Text = "تقر المستأجرة بمبلغ 120.000 دج وينازع في الباقي البالغ 240.000 دج";
        Assert.Equal(["120000", "240000"], AmountParser.Read(Text, NoteField.Facts).Select(v => v.Normalized));
    }
}
