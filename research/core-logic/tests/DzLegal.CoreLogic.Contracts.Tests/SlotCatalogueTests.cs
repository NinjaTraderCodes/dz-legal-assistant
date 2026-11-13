using DzLegal.Drafting;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>slots.json is the list of questions the app will ask the lawyer. Every rule record reads its slots from it.</summary>
public sealed class SlotCatalogueTests
{
    [Fact]
    public void TheCatalogueLoadsAndEverySlotIsAQuestionInBothLanguages()
    {
        var catalogue = HandbookFiles.Slots();
        Assert.Equal("1.0", catalogue.SchemaVersion);
        Assert.NotEmpty(catalogue.Slots);
        Assert.Equal(catalogue.Slots.Count, catalogue.Slots.Select(slot => slot.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(catalogue.Slots, slot =>
        {
            Assert.False(string.IsNullOrWhiteSpace(slot.Question.Ar));
            Assert.False(string.IsNullOrWhiteSpace(slot.Question.En));
            Assert.EndsWith("؟", slot.Question.Ar, StringComparison.Ordinal);
            Assert.DoesNotContain(slot.Id, HandbookFiles.ReservedSlots);
        });
    }

    [Fact]
    public void ChoicesBelongToChoiceSlotsOnly()
    {
        Assert.All(HandbookFiles.Slots().Slots, slot =>
        {
            if (slot.Type == SlotType.Choice)
            {
                Assert.True(slot.Choices.Count >= 2, slot.Id);
                Assert.DoesNotContain(slot.Choices, choice => choice.Id is "unknown" or "yes" or "no");
            }
            else
            {
                Assert.Empty(slot.Choices);
            }
        });
    }

    [Fact]
    public void WhatASlotPointsToExists()
    {
        var catalogue = HandbookFiles.Slots();
        var ids = catalogue.Slots.Select(slot => slot.Id).ToHashSet(StringComparer.Ordinal);
        var claims = HandbookFiles.ClaimIds();
        Assert.All(catalogue.Slots, slot =>
        {
            Assert.NotEmpty(slot.AskedFor);
            Assert.All(slot.AskedFor.Where(id => id != "*"), id => Assert.Contains(id, claims));
            if (slot.FromEvent is not null)
            {
                Assert.Contains(slot.FromEvent, FactVocabulary.Events);
            }

            if (slot.AskedWhen?.Slot is { } other)
            {
                Assert.Contains(other, ids);
            }
        });
    }
}
