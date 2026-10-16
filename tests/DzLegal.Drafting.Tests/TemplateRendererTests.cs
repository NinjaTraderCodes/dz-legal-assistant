namespace DzLegal.Drafting.Tests;

public class TemplateRendererTests
{
    private static string Render(string template, Dictionary<string, string> inputs) =>
        TemplateRenderer.RenderText(Fixtures.Requete, template, inputs);

    [Fact]
    public void DottedFieldIdsBecomeNestedMembers()
    {
        var inputs = new Dictionary<string, string> { ["court.name"] = "محكمة الشراقة" };
        Assert.Equal("محكمة الشراقة", Render("{{ court.name }}", inputs));
    }

    [Fact]
    public void BlankValuesAreFalsy()
    {
        var inputs = new Dictionary<string, string> { ["court.council"] = "  " };
        Assert.Equal("none", Render("{{ if court.council }}some{{ else }}none{{ end }}", inputs));
    }

    [Theory]
    [InlineData("true", "yes")]
    [InlineData("false", "no")]
    [InlineData("", "no")]
    public void BooleanFieldsAreBooleans(string value, string expected)
    {
        var inputs = new Dictionary<string, string> { ["plaintiff.isLegalEntity"] = value };
        Assert.Equal(expected, Render("{{ if plaintiff.isLegalEntity }}yes{{ else }}no{{ end }}", inputs));
    }

    [Fact]
    public void LinesSplitsAndCleansAMultilineValue()
    {
        var inputs = new Dictionary<string, string> { ["documents.list"] = "- عقد\n\n  • إنذار \n" };
        Assert.Equal("عقد|إنذار", Render("{{ (lines documents.list) | array.join '|' }}", inputs));
    }

    [Theory]
    [InlineData("male", "السيد")]
    [InlineData("female", "السيدة")]
    [InlineData("", "السيد")]
    public void GenderedPicksTheAgreedForm(string gender, string expected)
    {
        var inputs = new Dictionary<string, string> { ["plaintiff.gender"] = gender };
        Assert.Equal(expected, Render("{{ gendered plaintiff.gender \"السيد\" \"السيدة\" }}", inputs));
    }

    [Theory]
    [InlineData(0, "المدعى عليه اقترض المبلغ.", "حيث إن المدعى عليه اقترض المبلغ.")]
    [InlineData(1, "المدعى عليه امتنع.", "وحيث إن المدعى عليه امتنع.")]
    [InlineData(0, "بتاريخ 15 جانفي 2025 اقترض المدعى عليه.", "حيث إنه بتاريخ 15 جانفي 2025 اقترض المدعى عليه.")]
    [InlineData(2, "اتفق الطرفان على الأجل.", "وحيث إنه اتفق الطرفان على الأجل.")]
    [InlineData(1, "وحيث إن المدعى عليه امتنع.", "وحيث إن المدعى عليه امتنع.")]
    [InlineData(1, "التزمت الشركة بالتسليم قبل 02/05/2025.", "وحيث إنه التزمت الشركة بالتسليم قبل 02/05/2025.")] // case 04: a verb, not «ال»
    [InlineData(1, "الالتزام ثابت.", "وحيث إن الالتزام ثابت.")]
    [InlineData(0, "حيث إنه بتاريخ 15 جانفي 2025 وقع العقد.", "حيث إنه بتاريخ 15 جانفي 2025 وقع العقد.")]
    public void WhereasFollowsArabicGrammar(int index, string line, string expected) =>
        Assert.Equal(expected, TemplateRenderer.Whereas(index, line));

    [Fact]
    public void NormalizeTrimsAndCollapsesBlankLines() =>
        Assert.Equal("أ\n\nب", TemplateRenderer.Normalize("\n\n  أ  \n\n\n\n ب \n\n"));
}
