namespace DzLegal.CoreLogic.Contracts;

/// <summary>
/// The lawyer's notes, as the app holds them: the same four fields that <c>CaseText.From</c> reads (claim.subject,
/// notes.facts, notes.requests, documents.list), plus the section when given. Only <see cref="Facts"/> is required.
/// </summary>
public sealed record NoteInput
{
    public required string Facts { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Requests { get; init; } = string.Empty;
    public string Documents { get; init; } = string.Empty;
    public string CourtSection { get; init; } = string.Empty;
}

public enum NoteField
{
    Subject,
    Facts,
    Requests,
    Documents,
}

/// <summary>
/// The words an answer comes from: a stretch of one field of the note, as the lawyer typed it.
/// <see cref="Start"/> and <see cref="Length"/> count UTF-16 code units of the original string, before any normalisation.
/// </summary>
public readonly record struct TextSpan(NoteField Field, int Start, int Length);

/// <summary>
/// How a fact stands in the notes. <see cref="Stated"/> is FactPolarity "affirmed" in DzLegal.Drafting; the other two keep
/// their names. <see cref="NotMentioned"/> is the absence of any of the three and carries no span.
/// </summary>
public enum Polarity
{
    NotMentioned,
    Stated,
    Denied,
    Disputed,
}

public enum ValueKind
{
    Date,
    Amount,
}

/// <summary>One candidate claim type. <see cref="ClaimId"/> is a catalogue id, or "outside" for a matter outside the catalogue.</summary>
public sealed record ClaimGuess
{
    public const string Outside = "outside";

    public required string ClaimId { get; init; }

    /// <summary>Calibrated: among answers given with confidence c, a share c are right on the development notes.</summary>
    public required double Confidence { get; init; }
    public IReadOnlyList<TextSpan> Evidence { get; init; } = [];
}

public sealed record ClaimReading
{
    /// <summary>The claim types the reader answers with: one, or several when the note carries several claims. Empty when it abstains.</summary>
    public required IReadOnlyList<ClaimGuess> Answers { get; init; }

    /// <summary>The other candidates, best first, for a pick list.</summary>
    public IReadOnlyList<ClaimGuess> Others { get; init; } = [];

    /// <summary>True when the reader does not answer. The app then asks the lawyer to choose; it never guesses.</summary>
    public required bool Abstained { get; init; }
}

/// <summary>A date or an amount found in the note, read exactly by code.</summary>
public sealed record ValueReading
{
    public required ValueKind Kind { get; init; }

    /// <summary>yyyy-mm-dd (or yyyy for a lone year) for a date; digits only, in dinars, for an amount.</summary>
    public required string Normalized { get; init; }
    public required TextSpan Span { get; init; }

    /// <summary>What the value is for: the event kind or request kind it belongs to, or a role of slots.json; empty when not linked.</summary>
    public string Role { get; init; } = string.Empty;
    public double Confidence { get; init; } = 1.0;
}

/// <summary>One event of FactVocabulary.Events, as the note tells it.</summary>
public sealed record EventReading
{
    public required string Kind { get; init; }
    public required Polarity Polarity { get; init; }
    public required double Confidence { get; init; }

    /// <summary>True when the reader does not answer for this event; <see cref="Polarity"/> is then its best guess, to be shown as a question.</summary>
    public bool Abstained { get; init; }

    /// <summary>plaintiff, defendant, other or unknown (FactVocabulary.Actors).</summary>
    public string Actor { get; init; } = "unknown";

    /// <summary>For a disputed event: who contests it (plaintiff, defendant, other), when the note says so.</summary>
    public string? DisputedBy { get; init; }
    public ValueReading? Date { get; init; }
    public ValueReading? Amount { get; init; }

    /// <summary>Empty exactly when <see cref="Polarity"/> is <see cref="Polarity.NotMentioned"/>.</summary>
    public IReadOnlyList<TextSpan> Evidence { get; init; } = [];
}

/// <summary>One slot of slots.json that the reader can propose from the note.</summary>
public sealed record SlotReading
{
    public required string Slot { get; init; }

    /// <summary>"yes", "no", "unknown", a date, digits, or a choice id; "unknown" carries no span.</summary>
    public required string Value { get; init; }
    public required double Confidence { get; init; }
    public bool Abstained { get; init; }
    public IReadOnlyList<TextSpan> Evidence { get; init; } = [];
}

public sealed record RequestReading
{
    /// <summary>A request kind of FactVocabulary.Requests.</summary>
    public required string Kind { get; init; }
    public required double Confidence { get; init; }
    public ValueReading? Amount { get; init; }
    public IReadOnlyList<TextSpan> Evidence { get; init; } = [];
}

public sealed record ReaderInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }

    /// <summary>SHA-256 of each file the reader loaded (model, vocabulary, thresholds), by file name.</summary>
    public IReadOnlyDictionary<string, string> Files { get; init; } = new Dictionary<string, string>();
}

public sealed record NoteReading
{
    public required ClaimReading Claim { get; init; }

    /// <summary>One entry for every element of the answered claim types (claim-elements.json), mentioned or not.</summary>
    public required IReadOnlyList<EventReading> Events { get; init; }
    public required IReadOnlyList<SlotReading> Slots { get; init; }
    public required IReadOnlyList<RequestReading> Requests { get; init; }

    /// <summary>Every date and amount found, linked or not.</summary>
    public required IReadOnlyList<ValueReading> Values { get; init; }
    public required ReaderInfo Info { get; init; }
}

/// <summary>
/// Reads a lawyer's notes into the facts the rules need. Runs on CPU, in process, without any network.
/// Every answer points to the words it comes from; an answer without words is refused by the reader itself.
/// The reader proposes: nothing it returns is a confirmed fact until the lawyer confirms it.
/// </summary>
public interface INoteReader
{
    ReaderInfo Info { get; }

    /// <param name="note">The notes as typed.</param>
    /// <param name="confirmedClaimIds">
    /// The claim types the lawyer already confirmed, or empty. When given, the reader does not classify: it reads the
    /// elements of these types (the app's real case, and the "claim type given" setting of the test bench).
    /// </param>
    NoteReading Read(NoteInput note, IReadOnlyList<string> confirmedClaimIds);
}
