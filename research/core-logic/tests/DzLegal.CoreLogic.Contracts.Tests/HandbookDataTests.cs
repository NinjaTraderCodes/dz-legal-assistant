using System.Text.Json;
using DzLegal.Drafting;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>The handbook's other data files: they must stay checkable by the small validator and point at things that exist.</summary>
public sealed class HandbookDataTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "handbook");

    [Theory]
    [InlineData("rule-record.schema.json")]
    [InlineData("law-delta.schema.json")]
    [InlineData("note.schema.json")]
    public void EverySchemaOfTheHandbookUsesOnlyTheKeywordsTheValidatorChecks(string file) =>
        Assert.Empty(MiniSchema.UnsupportedKeywords(Parse(file)));

    [Fact]
    public void TheGuideTestNamesClaimTypesEventsAndSlotsThatExist()
    {
        var items = Parse("guide-test.json").GetProperty("items").EnumerateArray().ToList();
        var claims = HandbookFiles.ClaimIds();
        var slots = HandbookFiles.Slots().Slots.Select(slot => slot.Id).ToHashSet(StringComparer.Ordinal);
        string[] polarities = ["stated", "denied", "disputed", "notMentioned"];

        Assert.True(items.Count >= 40);
        Assert.Equal(items.Count, items.Select(item => item.GetProperty("id").GetInt32()).Distinct().Count());
        Assert.All(items, item =>
        {
            Assert.Contains(item.GetProperty("claim").GetString()!, claims);
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("text").GetString()));
            var expected = item.GetProperty("expected").GetString()!;
            var ask = item.GetProperty("ask").GetString()!;
            if (ask.StartsWith("event:", StringComparison.Ordinal))
            {
                Assert.Contains(ask["event:".Length..], FactVocabulary.Events);
                Assert.Contains(expected, polarities);
            }
            else if (ask.StartsWith("slot:", StringComparison.Ordinal))
            {
                Assert.Contains(ask["slot:".Length..], slots);
            }
            else
            {
                Assert.Equal("value", ask);
            }
        });
    }

    private static JsonElement Parse(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, file)));
        return document.RootElement.Clone();
    }
}
