using System.Globalization;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// The case's answers, as the conditions read them. A slot absent from <see cref="CaseSlots.Values"/>, or present with the
/// value "unknown", is unknown: never "no" (specs/rule-evaluator.md, "Traps").
/// </summary>
public sealed class CaseSlotReader
{
    /// <summary>The one reserved slot name: the list of request kinds the petition asks. It is always known.</summary>
    public const string RequestsSlot = "requests";

    public const string UnknownValue = "unknown";

    private readonly Dictionary<string, SlotValue> values = new(StringComparer.Ordinal);
    private readonly HashSet<string> requests;

    public CaseSlotReader(CaseSlots slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        foreach (var value in slots.Values)
        {
            // Two answers to one question cannot both be right; refusing is safer than picking one.
            if (!values.TryAdd(value.Slot, value))
            {
                throw new ArgumentException($"The slot {value.Slot} is answered twice.", nameof(slots));
            }
        }

        requests = new HashSet<string>(slots.Requests, StringComparer.Ordinal);
    }

    public bool IsKnown(string slot) => slot == RequestsSlot || Value(slot) is not null;

    /// <summary>The slot's value, or null when it is unknown.</summary>
    public string? Value(string slot) =>
        values.TryGetValue(slot, out var value) && value.Value != UnknownValue ? value.Value : null;

    public bool IsProposed(string slot) => values.TryGetValue(slot, out var value) && value.Origin == SlotOrigin.Proposed;

    public bool HasRequest(string kind) => requests.Contains(kind);

    /// <summary>A slot's date. Dates are yyyy-mm-dd, read with the invariant culture so that the result never depends on the machine.</summary>
    public DateOnly Date(string slot)
    {
        var value = Value(slot) ?? throw new InvalidOperationException($"The slot {slot} is unknown.");
        return Dates.Parse(value, $"the value of the slot {slot}");
    }
}

/// <summary>The one date format of records, examples and cases.</summary>
public static class Dates
{
    public const string Format = "yyyy-MM-dd";

    public static DateOnly Parse(string text, string what) =>
        TryParse(text, out var date) ? date : throw new FormatException($"{what} is not a date written yyyy-mm-dd: \"{text}\".");

    public static bool TryParse(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static string Write(DateOnly date) => date.ToString(Format, CultureInfo.InvariantCulture);
}
