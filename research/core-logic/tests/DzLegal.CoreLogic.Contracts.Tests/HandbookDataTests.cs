using System.Text.Json;
using DzLegal.CoreLogic.Rules.Checks;
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
            else if (ask.StartsWith("role:", StringComparison.Ordinal))
            {
                // The plaintiff's role (guide 3b, since guide 1.2): asked for the item's own claim type; the answer is
                // "unknown" or a relation/position pair of the app's lists.
                Assert.Equal(item.GetProperty("claim").GetString(), ask["role:".Length..]);
                if (expected != "unknown")
                {
                    var pair = expected.Split('/');
                    Assert.Equal(2, pair.Length);
                    Assert.Contains(pair[0], FactVocabulary.Relations);
                    Assert.Contains(pair[1], FactVocabulary.Positions);
                }
            }
            else if (ask is "claims")
            {
                // Guide 1.3: the note's claim types joined by «+», or outside.
                Assert.All(expected.Split('+'), id => Assert.True(id == "outside" || claims.Contains(id), id));
            }
            else if (ask is "requests")
            {
                // Guide 1.3: the request kinds joined by «+», or none.
                Assert.All(expected.Split('+'), kind => Assert.True(kind == "none" || FactVocabulary.Requests.Contains(kind), kind));
            }
            else if (ask.StartsWith("amount:", StringComparison.Ordinal))
            {
                // Guide 1.3: the amount an event takes (digits), or none.
                Assert.Contains(ask["amount:".Length..], FactVocabulary.Events);
                Assert.True(expected == "none" || expected.All(c => char.IsAsciiDigit(c) || c == '.'), expected);
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
