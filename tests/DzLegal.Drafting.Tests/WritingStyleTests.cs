using System.Text.Json.Nodes;

namespace DzLegal.Drafting.Tests;

public sealed class WritingStyleTests
{
    [Theory]
    [InlineData("سلف المدعي للمدعى عليه 200 ألف دينار\nوعدو يرجعهم في الصيف وما رجعش")]
    [InlineData("المدعى عليه ساكن في دار المدعي بالكراء\nما خلصش الكراء من جانفي")]
    [InlineData("يخلص 240000 دج تاع الكرا لي راه فاتو.")]
    [InlineData("Condamner le défendeur à payer au demandeur 1.500.000 DA au principal.\nMettre les dépens à la charge du défendeur.")]
    public void DarijaAndMostlyFrenchTextIsPointedOut(string text) => Assert.True(WritingStyle.LooksInformal(text));

    [Theory]
    [InlineData("")]
    [InlineData("أقرض المدعي المدعى عليه مبلغ 500.000 دج بتاريخ 10/01/2024، ولم يرده رغم الإعذار.")]
    [InlineData("حرر المدعى عليه اعترافا بدين (reconnaissance de dette) لفائدة المدعي.")]
    [InlineData("تعاقدت شركة SARL Atlas Distribution مع المدعي على توريد سلع، وما زال المبلغ دون تسديد.")]
    [InlineData("ما فتئ المدعى عليه يماطل في الوفاء، ولا يزال الدين قائما.")]
    public void FormalArabicWithNamesOrAFrenchTermIsNot(string text) => Assert.False(WritingStyle.LooksInformal(text));

    /// <summary>Every formal case we have stays silent; every Darija or French one is pointed out.</summary>
    [Fact]
    public void TheTestCasesAreClassifiedAsTheirPartitionSays()
    {
        var narrative = new[] { "claim.subject", "notes.facts", "notes.grounds", "notes.requests", "documents.list" };
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "drafting-cases"), "*.json"))
        {
            var node = JsonNode.Parse(File.ReadAllText(file))!;
            var robustness = node["partition"]?.GetValue<string>() == "robustness";
            var informal = (node["inputs"]?.AsObject() ?? [])
                .Where(kv => narrative.Contains(kv.Key) && WritingStyle.LooksInformal(kv.Value?.GetValue<string>()))
                .Select(kv => kv.Key)
                .ToList();
            Assert.True(robustness == (informal.Count > 0), $"{Path.GetFileName(file)}: robustness={robustness}, informal fields: {string.Join(", ", informal)}");
        }

        var grounds = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "grounds-cases", "cases.json")))!["cases"]!.AsArray();
        Assert.All(grounds, c => Assert.True(
            (c!["partition"]?.GetValue<string>() == "robustness") == WritingStyle.LooksInformal(c["facts"]!.GetValue<string>()),
            c["name"]!.GetValue<string>()));
    }
}
