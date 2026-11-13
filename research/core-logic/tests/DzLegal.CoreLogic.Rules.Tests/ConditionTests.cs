using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>The truth table of specs/rule-evaluator.md ("Conditions: three values"), every row, and the slots to ask for.</summary>
public sealed class ConditionTests
{
    private const string AIsYes = """{ "slot": "a", "is": "yes" }""";
    private const string AllAB = """{ "all": [ { "slot": "a", "is": "yes" }, { "slot": "b", "is": "yes" } ] }""";
    private const string AnyAB = """{ "any": [ { "slot": "a", "is": "yes" }, { "slot": "b", "is": "yes" } ] }""";

    [Theory]
    // is
    [InlineData(AIsYes, "a=yes", Truth.True, "")]
    [InlineData(AIsYes, "a=no", Truth.False, "")]
    [InlineData(AIsYes, "", Truth.Unknown, "a")]
    [InlineData(AIsYes, "a=unknown", Truth.Unknown, "a")]
    // in
    [InlineData("""{ "slot": "a", "in": ["x", "y"] }""", "a=y", Truth.True, "")]
    [InlineData("""{ "slot": "a", "in": ["x", "y"] }""", "a=z", Truth.False, "")]
    [InlineData("""{ "slot": "a", "in": ["x", "y"] }""", "", Truth.Unknown, "a")]
    // includes: the list of requests is always known, an empty one too
    [InlineData("""{ "slot": "requests", "includes": "wages" }""", "requests=damages,wages", Truth.True, "")]
    [InlineData("""{ "slot": "requests", "includes": "wages" }""", "requests=damages", Truth.False, "")]
    [InlineData("""{ "slot": "requests", "includes": "wages" }""", "", Truth.False, "")]
    // before: strictly before
    [InlineData("""{ "slot": "d", "before": "2026-01-10" }""", "d=2026-01-09", Truth.True, "")]
    [InlineData("""{ "slot": "d", "before": "2026-01-10" }""", "d=2026-01-10", Truth.False, "")]
    [InlineData("""{ "slot": "d", "before": "2026-01-10" }""", "", Truth.Unknown, "d")]
    // after: on or after
    [InlineData("""{ "slot": "d", "after": "2026-01-10" }""", "d=2026-01-10", Truth.True, "")]
    [InlineData("""{ "slot": "d", "after": "2026-01-10" }""", "d=2026-01-09", Truth.False, "")]
    [InlineData("""{ "slot": "d", "after": "2026-01-10" }""", "d=unknown", Truth.Unknown, "d")]
    // known: never unknown
    [InlineData("""{ "slot": "a", "known": true }""", "a=no", Truth.True, "")]
    [InlineData("""{ "slot": "a", "known": true }""", "", Truth.False, "")]
    [InlineData("""{ "slot": "a", "known": true }""", "a=unknown", Truth.False, "")]
    [InlineData("""{ "slot": "a", "known": false }""", "", Truth.True, "")]
    [InlineData("""{ "slot": "a", "known": false }""", "a=yes", Truth.False, "")]
    // all: unknown when no part is false and one is unknown; the parts already true are not asked
    [InlineData(AllAB, "a=yes;b=yes", Truth.True, "")]
    [InlineData(AllAB, "a=yes;b=no", Truth.False, "")]
    [InlineData(AllAB, "a=yes", Truth.Unknown, "b")]
    [InlineData(AllAB, "a=no", Truth.False, "")]
    [InlineData(AllAB, "", Truth.Unknown, "a,b")]
    // any: unknown when no part is true and one is unknown; the parts already false are not asked
    [InlineData(AnyAB, "a=no;b=no", Truth.False, "")]
    [InlineData(AnyAB, "a=yes", Truth.True, "")]
    [InlineData(AnyAB, "a=no", Truth.Unknown, "b")]
    [InlineData(AnyAB, "", Truth.Unknown, "a,b")]
    // not
    [InlineData("""{ "not": { "slot": "a", "is": "yes" } }""", "a=yes", Truth.False, "")]
    [InlineData("""{ "not": { "slot": "a", "is": "yes" } }""", "a=no", Truth.True, "")]
    [InlineData("""{ "not": { "slot": "a", "is": "yes" } }""", "", Truth.Unknown, "a")]
    // nested, and the order in which slots are written
    [InlineData("""{ "all": [ { "any": [ { "slot": "a", "is": "yes" }, { "slot": "b", "is": "yes" } ] }, { "slot": "c", "is": "yes" } ] }""", "a=no;c=yes", Truth.Unknown, "b")]
    [InlineData("""{ "all": [ { "any": [ { "slot": "a", "is": "yes" }, { "slot": "b", "is": "yes" } ] }, { "slot": "c", "is": "yes" } ] }""", "a=yes;c=yes", Truth.True, "")]
    [InlineData("""{ "all": [ { "slot": "b", "is": "yes" }, { "slot": "a", "is": "yes" }, { "slot": "b", "known": true } ] }""", "", Truth.False, "")]
    [InlineData("""{ "all": [ { "slot": "b", "is": "yes" }, { "slot": "a", "is": "yes" } ] }""", "", Truth.Unknown, "b,a")]
    public void EachRowOfTheTruthTable(string condition, string slots, Truth expected, string ask)
    {
        var result = Conditions.Evaluate(Build.Condition(condition), Build.Case(slots));
        Assert.Equal(expected, result.Value);
        Assert.Equal(ask.Split(',', StringSplitOptions.RemoveEmptyEntries), result.Ask);
    }

    [Theory]
    [InlineData("""{ }""", "exactly one of all, any, not, slot")]
    [InlineData("""{ "slot": "a", "is": "yes", "all": [ { "slot": "b", "is": "yes" } ] }""", "exactly one of all, any, not, slot")]
    [InlineData("""{ "slot": "a", "is": "yes", "in": ["yes"] }""", "exactly one of is, in, includes")]
    [InlineData("""{ "slot": "a" }""", "exactly one of is, in, includes")]
    [InlineData("""{ "slot": "a", "includes": "wages" }""", "includes tests the reserved slot requests only")]
    [InlineData("""{ "slot": "requests", "is": "wages" }""", "is tested with includes only")]
    [InlineData("""{ "all": [] }""", "an empty list")]
    [InlineData("""{ "any": [ { "slot": "a", "is": "yes" } ], "is": "yes" }""", "belong to a slot condition")]
    [InlineData("""{ "not": { "slot": "d", "before": "10/01/2026" } }""", "is not a date written yyyy-mm-dd")]
    public void AMalformedConditionIsRefusedByTheCheckAndByTheEvaluator(string condition, string problem)
    {
        var parsed = Build.Condition(condition);
        Assert.Contains(Conditions.ShapeProblems(parsed, "when"), message => message.Contains(problem, StringComparison.Ordinal));
        Assert.Throws<RuleRecordException>(() => Conditions.Evaluate(parsed, Build.Case("a=yes;d=2026-01-01")));
    }

    [Fact]
    public void EveryPartIsReadSoThatEverySlotUsedIsRecorded()
    {
        var used = new List<string>();
        Conditions.Evaluate(Build.Condition(AnyAB), new CaseSlotReader(Build.Case("a=yes;b=no")), used);
        Assert.Equal(["a", "b"], used);
    }

    [Fact]
    public void ASlotAnsweredTwiceIsRefused() =>
        Assert.Throws<ArgumentException>(() => Conditions.Evaluate(Build.Condition(AIsYes), Build.Case("a=yes;a=no")));

    [Fact]
    public void ADateThatIsNotWrittenYearMonthDayIsRefused() =>
        Assert.Throws<FormatException>(() => Conditions.Evaluate(Build.Condition("""{ "slot": "d", "after": "2026-01-10" }"""), Build.Case("d=10/01/2026")));
}
