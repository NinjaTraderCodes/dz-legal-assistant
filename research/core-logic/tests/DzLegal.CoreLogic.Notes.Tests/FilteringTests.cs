using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Notes.Filtering;
using DzLegal.CoreLogic.Notes.Writing;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>The local filter's sheet and its five-step rule (specs/notes-pipeline.md, section 4a), with a stand-in model.</summary>
public sealed class FilteringTests
{
    private static readonly Lazy<SheetLabels> LazyLabels = new(() => new SheetLabels(WritingTests.Data));

    private static SheetLabels Labels => LazyLabels.Value;

    private static JsonObject Written()
    {
        var scenario = WritingTests.Scenario();
        var note = WritingTests.Note();
        var pointed = WriterLabels.Point(scenario, note);
        return new JsonObject
        {
            ["id"] = "n-fx-00001",
            ["text"] = new JsonObject { ["subject"] = "", ["facts"] = (string)note["facts"]!, ["requests"] = (string)note["requests"]!, ["documents"] = "" },
            ["provenance"] = new JsonObject { ["writerLabels"] = WriterLabels.Labels(WritingTests.Data, scenario, pointed.Quotes) },
        };
    }

    private static FilterSheet Sheet(int attempt = 0) =>
        FilterSheet.Build(Labels, WritingTests.Scenario(), Written(), FilterCommand.SheetSeed("n-fx-00001", attempt));

    /// <summary>The letters that answer every question as the scenario's label says, with some questions answered otherwise.</summary>
    private static List<string> Answer(FilterSheet sheet, Func<SheetQuestion, bool>? wrong = null) =>
        [.. sheet.Questions.Select(q => (wrong?.Invoke(q) == true
            ? q.Options.First(o => !q.Expected.Contains(o.Value))
            : q.Options.First(o => q.Expected.Contains(o.Value))).Letter)];

    [Fact]
    public void TheSheetAsksWhatTheSpecificationLists()
    {
        var sheet = Sheet();
        int Count(string kind) => sheet.Questions.Count(q => q.Kind == kind);
        Assert.Equal(2, Count("claim"));
        Assert.Equal(3, Count("distractorClaim"));
        Assert.Equal(1, Count("role"));  // contract-damages offers a choice; unpaid-sale-price does not
        Assert.Equal(11, Count("element"));
        Assert.Equal(2, Count("decoyElement"));
        Assert.Equal(4, Count("slot"));  // the yes/no and choice slots; the date slots are values
        Assert.Equal(8, Count("value")); // every value with a role; the fee for copies has none
        Assert.Equal(2, Count("request"));
        Assert.Equal(1, Count("decoyRequest"));
        Assert.All(sheet.Questions.Where(q => q.Kind is "element" or "role" or "slot" or "value"), q =>
            Assert.Contains(q.Options[^1].Value, new[] { "notMentioned", "unknown", "none" }));
        Assert.All(sheet.Questions, q => Assert.InRange(q.Options.Count, 2, 12));
        Assert.DoesNotContain("{{", FilterCommand.Prompt(NotesFiles.Find(null), Written(), sheet), StringComparison.Ordinal);
    }

    [Fact]
    public void RightAnswersKeepTheNoteAndOneWrongElementBlanksIt()
    {
        var sheet = Sheet();
        Assert.Equal(FilterVerdict.Kept, FilterRule.Apply(sheet, Answer(sheet)).Verdict);
        var one = FilterRule.Apply(sheet, Answer(sheet, q => q.Path == "events/unpaid-sale-price/goods_delivered/polarity"));
        Assert.Equal(FilterVerdict.KeptWithBlanks, one.Verdict);
        Assert.Equal(["events/unpaid-sale-price/goods_delivered/polarity"], one.Blanked);
        var gold = FilterRule.Gold(Written()["provenance"]!["writerLabels"]!.AsObject(), one.Blanked);
        var ev = gold["events"]!.AsArray().Single(e => (string)e!["kind"]! == "goods_delivered")!;
        Assert.Equal("blanked", (string)ev["polarity"]!);
        Assert.Null(ev["actor"]);
    }

    [Fact]
    public void ADecoyAClaimAndTooManyBlanksAreDecidedByTheRule()
    {
        var sheet = Sheet();
        Assert.Equal(FilterVerdict.Unsure, FilterRule.Apply(sheet, Answer(sheet, q => q.Kind == "decoyElement")).Verdict);
        Assert.Equal(FilterVerdict.ClaimDisagreement, FilterRule.Apply(sheet, Answer(sheet, q => q.Kind == "claim")).Verdict);
        var many = 0;
        Assert.Equal(FilterVerdict.TooManyBlanks, FilterRule.Apply(sheet, Answer(sheet, q => q.Kind == "element" && many++ < 6)).Verdict);
        Assert.Equal(FilterVerdict.Invalid, FilterRule.Apply(sheet, ["a"]).Verdict);
        Assert.Equal(FilterVerdict.Invalid, FilterRule.Apply(sheet, [.. Answer(sheet).Take(sheet.Questions.Count - 1), "l"]).Verdict);
    }

    [Fact]
    public void TheLettersAreReadPastMarkerLines()
    {
        Assert.Equal(["a", "c"], LocalModel.Letters("<|im_start|>assistant\n{\"answers\": [\"a\", \"c\"]}"));
        Assert.Null(LocalModel.Letters("no json here"));
    }

    /// <summary>A stand-in for the local model: answers right, except as told for its first answers.</summary>
    private sealed class StandIn(Func<int, FilterSheet, List<string>> answer) : ISheetAnswerer
    {
        private int calls;

        public string Model => "stand-in";

        public string Digest => "0000000000000000";

        public ModelAnswer Answer(string prompt, int questions)
        {
            var n = calls++;
            var attempt = Math.Min(n, 1);  // the first call answers the first sheet, the next the rebuilt one
            var sheet = Sheet(attempt);
            return new ModelAnswer(answer(n, sheet), "", 1.0);
        }
    }

    [Fact]
    public void ADecoyFailureRebuildsTheSheetOnceAndAnInvalidAnswerIsAskedAgain()
    {
        var files = NotesFiles.Find(null);
        // First answer: a decoy present; second (the new sheet): right.
        var model = new StandIn((n, sheet) => n == 0 ? Answer(sheet, q => q.Kind == "decoyRequest") : Answer(sheet));
        var done = FilterCommand.Filter(files, Labels, model, WritingTests.Scenario(), Written());
        Assert.Equal(FilterVerdict.Kept, done.Result.Verdict);
        Assert.Equal(2, done.Asked.Count);

        // Twice unreadable: dropped as filterInvalid.
        var broken = new StandIn((_, _) => ["z"]);
        Assert.Equal("filterInvalid", FilterCommand.Filter(files, Labels, broken, WritingTests.Scenario(), Written()).Reason);
    }
}
