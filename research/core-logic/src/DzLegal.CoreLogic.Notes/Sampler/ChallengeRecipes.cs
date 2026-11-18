namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>
/// One difficulty of the challenge set (specs/scenario-sampler.md, "The challenge set's recipes"): its id, the owner's word
/// it answers, and the style value it forces (a card parameter and its value), if any.
/// </summary>
public sealed record ChallengeRecipe(string Id, string OwnersWord, string? StyleParameter = null, string? StyleValue = null);

/// <summary>
/// The recipes implemented since sampler 1.4 (18 November 2026). The owner kept a challenge set "for polarity only (negation,
/// restriction, partial admission, contested facts)": four rows of the recipes table, one per word. The other eight rows
/// stay unimplemented, and asking for one is an error.
/// </summary>
public static class ChallengeRecipes
{
    public static readonly IReadOnlyList<ChallengeRecipe> Implemented =
    [
        new("doubleNegation", "negation", "negation", "doubleNegation"),
        new("restriction", "restriction", "negation", "restriction"),
        new("contestOfAmount", "partial admission"),
        new("documentAgainstContest", "contested facts", "fields", "allFour"),
    ];

    /// <summary>The rows of the recipes table that are not implemented.</summary>
    public static readonly IReadOnlyList<string> NotImplemented =
        ["contestOfCause", "informalDemand", "recordAgainstDelivery", "correction", "despite", "intention", "twoClaims", "nearMiss"];

    /// <summary>
    /// The kinds of event a paper records, so that a document can be named after the event (the guide's rule 1 and its cases
    /// 16, 17, 33 and 35: a contract, an acknowledgment, a receipt, a delivery note, a bailiff's record, a judgment).
    /// </summary>
    public static readonly IReadOnlySet<string> DocumentKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "money_lent", "debt_acknowledged", "payment_made", "contract_concluded", "goods_delivered", "notice_served",
        "eviction_notice", "conciliation_failed", "marriage", "divorce", "custody_judgment", "criminal_conviction",
        "employment_started", "dismissal",
    };

    /// <summary>The English line a <c>documentAgainstContest</c> scenario puts on its disputed event (never Arabic).</summary>
    public const string DocumentLine =
        "the client holds a paper that records this fact: name it in \"documents\"; the other side's contest still stands";

    public static string Ids => string.Join(", ", Implemented.Select(r => r.Id));

    /// <summary>The recipe of an id; an unknown or unimplemented id is an error that names the implemented ones.</summary>
    public static ChallengeRecipe Get(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return Implemented.FirstOrDefault(r => r.Id == id)
               ?? throw new ArgumentException(NotImplemented.Contains(id)
                   ? $"difficulty {id} is a row of the recipes table that is not implemented; the implemented ones are {Ids}"
                   : $"no difficulty {id}; the implemented ones are {Ids}");
    }

    /// <summary>
    /// The sentence that replaces the card's own sentence for a forced parameter, naming the scenario's fact by its
    /// reference. The only Arabic is the cue words the recipe itself names.
    /// </summary>
    public static string ForcedSentence(string parameter, string value, string target) => (parameter, value) switch
    {
        ("negation", "restriction") =>
            $"Say the fact event:{target} by restriction, in one sentence: a negation, then «إلا», «سوى» or «غير» (as «لم … إلا»).",
        ("negation", "doubleNegation") =>
            $"Say the fact event:{target} by a double negation, in one sentence (as «لا ينكر», «لم يمتنع عن»): two negations that together say it happened.",
        _ => StyleCards.Sentence(parameter, value),
    };
}
