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
    public void ASentenceEndsAtAMarkButNotAtThePointsOfANumber()
    {
        var written = Written();
        var quote = new JsonObject { ["field"] = "facts", ["start"] = ((string)written["text"]!["facts"]!).IndexOf("1.200.000", StringComparison.Ordinal), ["length"] = 9 };
        var (_, start, end) = FilterTrial.Sentence(written, quote);
        var sentence = ((string)written["text"]!["facts"]!)[start..end];
        Assert.StartsWith("أبرمت شركة سيرتا", sentence, StringComparison.Ordinal);
        Assert.EndsWith("وسلمته البضاعة كاملة.", sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void APlantDeletesOnlyTheSentencesOfOneFactThatHoldNoOtherFact()
    {
        var sheet = Sheet();
        var plain = new FilteredNote(FilterRule.Apply(sheet, Answer(sheet)), sheet, [], "");
        var planted = Enumerable.Range(0, 40).Select(s => FilterTrial.Plant(Written(), plain, s)).Where(p => p is not null).Select(p => p!.Value).ToList();
        Assert.NotEmpty(planted);
        foreach (var (path, altered) in planted)
        {
            var before = (string)Written()["text"]!["facts"]! + (string)Written()["text"]!["requests"]!;
            var after = (string)altered["text"]!["facts"]! + (string)altered["text"]!["requests"]!;
            Assert.True(after.Length < before.Length, path);
        }

        // The goods were delivered in the contract's sentence, which holds other facts: never planted.
        Assert.DoesNotContain(planted, p => p.Path == "events/unpaid-sale-price/goods_delivered/polarity");
        Assert.Contains(planted, p => p.Path == "events/contract-damages/other/polarity");
    }

    [Theory]
    [InlineData(18, 128)]
    [InlineData(10, 120)]  // never under the prompt's figure
    [InlineData(37, 242)]  // a two-claim note's sheet: 120 tokens cut its letters off
    public void TheAnswersTokenBudgetGrowsWithTheSheet(int questions, int budget) =>
        Assert.Equal(budget, LocalModel.TokenBudget(questions));

    [Fact]
    public void EachAnswerIsConstrainedToItsOwnQuestionsLetters()
    {
        var sheet = Sheet();
        var options = sheet.Questions.Select(q => (IReadOnlyList<string>)[.. q.Options.Select(o => o.Letter)]).ToList();
        var schema = LocalModel.SheetSchema(options)["properties"]!["answers"]!;
        Assert.Equal(sheet.Questions.Count, (int)schema["minItems"]!);
        Assert.Equal(sheet.Questions.Count, (int)schema["maxItems"]!);
        var positions = schema["prefixItems"]!.AsArray();
        for (var i = 0; i < sheet.Questions.Count; i++)
        {
            Assert.Equal(sheet.Questions[i].Options.Select(o => o.Letter), positions[i]!["enum"]!.AsArray().Select(l => (string)l!));
        }
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

        public ModelAnswer Answer(string prompt, IReadOnlyList<IReadOnlyList<string>> options)
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
        // The first answer answered the first sheet: its decoys are scored there, not on the rebuilt sheet's.
        Assert.Equal(Sheet(0).Text(), done.FirstSheet!.Text());
        Assert.Equal(Sheet(1).Text(), done.Sheet.Text());

        // Twice unreadable: dropped as filterInvalid.
        var broken = new StandIn((_, _) => ["z"]);
        Assert.Equal("filterInvalid", FilterCommand.Filter(files, Labels, broken, WritingTests.Scenario(), Written()).Reason);
    }

    /// <summary>Answers the letter of the right option for every question of whichever sheet it is given; counts its calls.</summary>
    private sealed class Counting(Func<int, FilterSheet> sheetOf) : ISheetAnswerer
    {
        public int Calls { get; private set; }

        public string Model => "stand-in";

        public string Digest => "0000000000000000";

        public ModelAnswer Answer(string prompt, IReadOnlyList<IReadOnlyList<string>> options) => new(FilteringTests.Answer(sheetOf(Calls++)), "", 1.0);
    }

    private sealed class FixedPower(bool onBattery, int percent) : IPowerSource
    {
        public Power Read() => new(onBattery, percent);
    }

    /// <summary>The owner's decision of 16 November 2026 (Jais on 10 notes): a smaller trial takes the first notes of the list
    /// chosen for the first model, so both models are measured on the same notes.</summary>
    [Fact]
    public void ASmallerTrialTakesTheFirstNotesOfTheSameList()
    {
        var work = Path.Combine(Path.GetTempPath(), "chosen-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(work, "filter-trial"));
            NotesJson.Write(Path.Combine(work, "filter-trial", "trial-seed9.chosen.json"),
                """{ "seed": 9, "notes": ["n-a", "n-b", "n-c"] }""" + "\n");
            Assert.Equal(["n-a", "n-b"], FilterTrial.Chosen(work, 2, 9));
            Assert.Equal(["n-a", "n-b", "n-c"], FilterTrial.Chosen(work, 100, 9));
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    /// <summary>The owner's conditions of 16 November 2026: each note saved as it is done, a resumed trial asks nothing for the
    /// notes already done, and the battery floor stops the trial before a note, with nothing lost.</summary>
    [Fact]
    public void TheTrialSavesEachNoteResumesWithoutRepeatingAndStopsAtTheBatteryFloor()
    {
        var files = NotesFiles.Find(null);
        var work = Path.Combine(Path.GetTempPath(), "trial-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(work, "scenarios"));
            Directory.CreateDirectory(Path.Combine(work, "written"));
            NotesJson.Write(Path.Combine(work, "scenarios", "pilot.jsonl"), WritingTests.Scenario().ToJsonString(NotesJson.Compact) + "\n");
            var note = Written();
            note["set"] = "pilot";
            note["provenance"]!["scenario"] = "s-fx-00001";
            note["provenance"]!["writer"] = new JsonObject { ["family"] = "openai" };
            NotesJson.Write(Path.Combine(work, "written", "pilot.jsonl"), note.ToJsonString(NotesJson.Compact) + "\n");

            // The battery at the floor: nothing is asked, nothing written, the trial says it stopped.
            var low = new Counting(_ => Sheet());
            var (_, stopped) = FilterTrial.Measure(files, Labels, low, work, 1, 5, new FixedPower(true, 20), 20);
            Assert.True(stopped);
            Assert.Equal(0, low.Calls);

            // On mains: both passes are done and saved; the plain run answers right, so the plant is counted.
            var model = new Counting(_ => Sheet());
            var (report, done) = FilterTrial.Measure(files, Labels, model, work, 1, 5, new FixedPower(false, 100), 20);
            Assert.False(done);
            Assert.True((bool)report["complete"]!);
            Assert.Equal("0 of 2", (string)report["table"]!["notesTimedOnBattery"]!);  // the plain run and the counted plant
            Assert.Equal(1, (int)report["table"]!["countedPlants"]!);
            var calls = model.Calls;

            // Resumed: the notes already done are not asked again.
            var again = new Counting(_ => Sheet());
            FilterTrial.Measure(files, Labels, again, work, 1, 5, new FixedPower(false, 100), 20);
            Assert.Equal(0, again.Calls);
            Assert.True(calls >= 1);
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }
}
