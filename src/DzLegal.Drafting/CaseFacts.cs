using System.Globalization;
using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.Drafting;

/// <summary>One sentence of the lawyer's text, numbered across the fields the reader sees (s1, s2…).</summary>
public sealed record CaseSentence(string Id, string FieldId, string Text);

/// <summary>
/// The lawyer's text as the fact reader sees it: every field it reads, cut into numbered sentences, so each fact can
/// point to where it was written and the program can check that its date or amount is really there.
/// </summary>
public sealed partial class CaseText
{
    /// <summary>The fields read, in this order. The court section is given as a hint, not as a sentence.</summary>
    public static readonly IReadOnlyList<string> Fields = ["claim.subject", "notes.facts", "notes.requests", "documents.list"];

    private CaseText(IReadOnlyList<CaseSentence> sentences, string courtSection)
    {
        Sentences = sentences;
        CourtSection = courtSection;
    }

    public IReadOnlyList<CaseSentence> Sentences { get; }

    public string CourtSection { get; }

    public CaseSentence? Find(string id) => Sentences.FirstOrDefault(s => s.Id == id);

    public static CaseText From(IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var sentences = new List<CaseSentence>();
        foreach (var field in Fields)
        {
            foreach (var piece in Split(inputs.GetValueOrDefault(field) ?? string.Empty))
            {
                sentences.Add(new CaseSentence("s" + (sentences.Count + 1).ToString(CultureInfo.InvariantCulture), field, piece));
            }
        }

        return new CaseText(sentences, (inputs.GetValueOrDefault("court.section") ?? string.Empty).Trim());
    }

    /// <summary>Lines, then sentences ending in «.» «؟» «!» followed by a space; the dot inside 500.000 stays.</summary>
    public static IEnumerable<string> Split(string text) =>
        text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .SelectMany(line => SentenceEnd().Split(line))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);

    /// <summary>The numbered text given to the reader: «s1 [الوقائع] …».</summary>
    public string Render() => string.Join('\n', Sentences.Select(s => $"{s.Id} [{FieldLabel(s.FieldId)}] {s.Text}"));

    private static string FieldLabel(string field) => field switch
    {
        "claim.subject" => "الموضوع",
        "notes.facts" => "الوقائع",
        "notes.requests" => "الطلبات",
        "documents.list" => "المستندات",
        _ => field,
    };

    [GeneratedRegex(@"(?<=[.؟!])\s+")]
    private static partial Regex SentenceEnd();
}

/// <summary>How a fact stands in the lawyer's text.</summary>
public static class FactPolarity
{
    public const string Affirmed = "affirmed";

    /// <summary>The text says it did not happen («لم يوجه أي إعذار»).</summary>
    public const string Denied = "denied";

    /// <summary>One side asserts it and the other contests it («ينكر المدعى عليه…»).</summary>
    public const string Disputed = "disputed";

    public static readonly IReadOnlyList<string> All = [Affirmed, Denied, Disputed];
}

/// <summary>The closed vocabularies the reader chooses from, and that the rules work on.</summary>
public static class FactVocabulary
{
    public static readonly IReadOnlyList<string> Relations =
    [
        "loan", "debt_acknowledgment", "sale", "lease", "commercial_lease", "works_contract", "transport_contract",
        "supply", "employment", "marriage", "ownership", "none", "inheritance", "company", "administrative", "civil_status",
        "other",
    ];

    public static readonly IReadOnlyList<string> Positions =
    [
        "lender", "borrower", "creditor", "debtor", "seller", "buyer", "lessor", "lessee", "owner", "occupant", "contractor",
        "client", "supplier", "employer", "employee", "husband", "wife", "father", "mother", "victim", "heir", "partner",
        "other",
    ];

    public static readonly IReadOnlyList<string> Events =
    [
        "contract_concluded", "money_lent", "debt_acknowledged", "deadline_passed", "payment_made", "debt_unpaid",
        "price_unpaid", "rent_unpaid", "wages_unpaid", "maintenance_unpaid", "goods_delivered", "delivery_missing",
        "delivery_late", "work_stopped", "lease_started", "lease_renewal_refused", "eviction_notice", "notice_served",
        "occupation_without_title", "harm_by_person", "harm_by_animal", "harm_by_building", "harm_by_thing",
        "criminal_conviction", "concealment", "marriage", "divorce", "abandonment", "spouse_harm", "custody_held",
        "visitation_refused", "employment_started", "dismissal", "conciliation_failed", "death", "administrative_decision",
        "record_error", "custody_judgment", "custodian_remarried", "custody_renounced", "custodian_unfit", "custody_unclaimed",
        "custodian_abroad", "other",
    ];

    public static readonly IReadOnlyList<string> Requests =
    [
        "payment", "eviction", "rescission", "damages", "annulment", "restitution", "delivery", "divorce", "tatliq", "khul",
        "maintenance", "custody", "custody_transfer", "visitation", "housing", "reinstatement", "wages", "partition",
        "cancel_decision", "correction", "dissolution", "guardianship", "provisional_execution", "other",
    ];

    public static readonly IReadOnlyList<string> Actors = ["plaintiff", "defendant", "other", "unknown"];
}

/// <summary>A value (date or amount) as the reader gave it, and what the program found when checking it.</summary>
/// <param name="Text">As the reader wrote it.</param>
/// <param name="Normalized">dd/mm/yyyy or yyyy for a date, digits only for an amount; empty when unreadable.</param>
/// <param name="Check">Where the value was found: <see cref="ValueCheck"/>.</param>
public sealed record FactValue(string Text, string Normalized, string Check);

/// <summary>What the check found for a value the reader gave.</summary>
public static class ValueCheck
{
    /// <summary>In one of the sentences the reader cited.</summary>
    public const string InCitedSentence = "cited";

    /// <summary>Elsewhere in the case, not in the cited sentences.</summary>
    public const string ElsewhereInCase = "elsewhere";

    /// <summary>Nowhere in the case: invented.</summary>
    public const string NotInCase = "invented";
}

public sealed record FactEvent(string Kind, FactValue? Date, FactValue? Amount, string Actor, string Polarity, IReadOnlyList<string> Sentences);

public sealed record FactRequest(string Kind, FactValue? Amount, IReadOnlyList<string> Sentences);

/// <summary>
/// The facts of a case as read: the relationship between the parties and the plaintiff's position in it, the events with
/// their dates, amounts, actor and polarity, and the requests. Every value has been checked against the text.
/// </summary>
public sealed record CaseFacts
{
    public string Relation { get; init; } = string.Empty;

    public string Position { get; init; } = string.Empty;

    public IReadOnlyList<FactEvent> Events { get; init; } = [];

    public IReadOnlyList<FactRequest> Requests { get; init; } = [];

    /// <summary>Answer lines that could not be read (unknown code, wrong shape), kept for diagnosis.</summary>
    public IReadOnlyList<string> Rejected { get; init; } = [];

    public IEnumerable<FactValue> Values =>
        Events.SelectMany(e => new[] { e.Date, e.Amount }).Concat(Requests.Select(r => r.Amount)).OfType<FactValue>();
}

/// <summary>Reads dates and amounts out of Arabic text and checks a reader's values against the sentences.</summary>
public static partial class FactValues
{
    /// <summary>dd/mm/yyyy (zero-padded), or yyyy for a lone year; empty when <paramref name="text"/> is not a date.</summary>
    public static string NormalizeDate(string? text)
    {
        var t = ArabicScript.ToAsciiDigits(text ?? string.Empty).Trim();
        if (FullDate().Match(t) is { Success: true } m)
        {
            return $"{int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture):00}/{int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture):00}/{m.Groups[3].Value}";
        }

        return Year().Match(t) is { Success: true } y ? y.Value : string.Empty;
    }

    /// <summary>The amount in digits («500.000 دج» → 500000, «200 ألف» → 200000); empty when there is no number.</summary>
    public static string NormalizeAmount(string? text)
    {
        var t = ArabicScript.ToAsciiDigits(text ?? string.Empty);
        var m = Amount().Match(t);
        if (!m.Success)
        {
            return string.Empty;
        }

        var digits = m.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(",", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return string.Empty;
        }

        var unit = m.Groups[2].Value;
        value *= unit.Length == 0 ? 1 : unit.StartsWith('م') ? 1_000_000 : 1000;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Every date written in the sentence, normalized.</summary>
    public static IReadOnlyList<string> DatesIn(string sentence)
    {
        var t = ArabicScript.ToAsciiDigits(sentence);
        var full = FullDate().Matches(t).Select(m => NormalizeDate(m.Value)).ToList();
        var withoutFull = FullDate().Replace(t, " ");
        return [.. full, .. Year().Matches(withoutFull).Select(m => m.Value)];
    }

    /// <summary>Every amount written in the sentence (dates excluded), normalized.</summary>
    public static IReadOnlyList<string> AmountsIn(string sentence)
    {
        var t = FullDate().Replace(ArabicScript.ToAsciiDigits(sentence), " ");
        return Amount().Matches(t).Select(m => NormalizeAmount(m.Value)).Where(a => a.Length > 0).ToList();
    }

    /// <summary>Where the value is: in a cited sentence, elsewhere in the case, or nowhere (invented).</summary>
    public static FactValue? Check(string? raw, bool isDate, IReadOnlyList<string> cited, CaseText text)
    {
        ArgumentNullException.ThrowIfNull(cited);
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(raw) || raw.Trim() is "-" or "—" or "لا يوجد")
        {
            return null;
        }

        var normalized = isDate ? NormalizeDate(raw) : NormalizeAmount(raw);
        if (normalized.Length == 0)
        {
            return new FactValue(raw.Trim(), string.Empty, ValueCheck.NotInCase);
        }

        bool Holds(CaseSentence s) => (isDate ? DatesIn(s.Text) : AmountsIn(s.Text)).Contains(normalized);
        var where = cited.Select(text.Find).OfType<CaseSentence>().Any(Holds) ? ValueCheck.InCitedSentence
            : text.Sentences.Any(Holds) ? ValueCheck.ElsewhereInCase
            : ValueCheck.NotInCase;
        return new FactValue(raw.Trim(), normalized, where);
    }

    [GeneratedRegex(@"\b(\d{1,2})\s*[/\-.]\s*(\d{1,2})\s*[/\-.]\s*(\d{4})\b")]
    private static partial Regex FullDate();

    [GeneratedRegex(@"\b(19|20)\d{2}\b")]
    private static partial Regex Year();

    /// <summary>A number of at least three digits (thousand separators allowed), or any number followed by ألف/مليون.</summary>
    [GeneratedRegex(@"(?<![\d/])(\d{1,3}(?:[.,\s]\d{3})+|\d{3,}|\d+(?=\s*(?:ألف|الف|مليون)))(?:\s*(ألف|الف|مليون))?(?![\d/])")]
    private static partial Regex Amount();
}
