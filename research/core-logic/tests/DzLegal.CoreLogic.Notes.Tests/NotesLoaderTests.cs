using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>The loader's refusals (specs/notes-pipeline.md, "The loader"): one test for each row.</summary>
public sealed class NotesLoaderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "loader-tests-" + Guid.NewGuid().ToString("N"));

    public NotesLoaderTests() => Directory.CreateDirectory(Path.Combine(root, "notes"));

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string TestIds => Path.Combine(root, "TEST-IDS.txt");

    private string MirrorRuns => Path.Combine(root, "RUNS.md");

    private NotesLoader Loader => new(Path.Combine(root, "notes"), TestIds, MirrorRuns);

    private static JsonObject Note(string id, string set, string writer = "openai", string? split = null, bool allowed = true,
        bool composed = false, bool filter = true, string? family = null, string? pack = null, string status = "kept")
    {
        var provenance = new JsonObject
        {
            ["writer"] = new JsonObject { ["family"] = writer, ["model"] = "m", ["date"] = "2026-11-16" },
            ["trainingAllowed"] = allowed,
            ["composedByCode"] = composed,
        };
        if (filter)
        {
            provenance["filter"] = new JsonObject { ["family"] = "local", ["model"] = "gemma", ["version"] = "filter-sheet-1", ["date"] = "2026-11-16" };
        }

        if (family is not null)
        {
            provenance["scenarioFamily"] = family;
        }

        if (pack is not null)
        {
            provenance["pack"] = pack;
        }

        var note = new JsonObject { ["id"] = id, ["set"] = set, ["status"] = status, ["provenance"] = provenance };
        if (split is not null)
        {
            note["split"] = split;
        }

        return note;
    }

    private void Set(string set, params JsonObject[] notes) =>
        File.WriteAllLines(Path.Combine(root, "notes", set + ".jsonl"), notes.Select(note => note.ToJsonString()));

    private LoadResult Load(LoadPurpose purpose, params string[] sets) => Loader.Load(new LoadRequest { Purpose = purpose, Sets = sets });

    private string Refusal(LoadRequest request) => Assert.Throws<LoaderRefusalException>(() => Loader.Load(request)).Code;

    private string Refusal(LoadPurpose purpose, params string[] sets) => Refusal(new LoadRequest { Purpose = purpose, Sets = sets });

    [Fact]
    public void ATrainingMixOfGoodNotesLoadsAndDroppedNotesNeverDo()
    {
        Set("train", Note("n1", "train"), Note("n2", "train"), Note("n3", "train", status: "dropped"));
        Assert.Equal(["n1", "n2"], Load(LoadPurpose.Train, "train").Notes.Select(note => note["id"]!.GetValue<string>()));
    }

    [Fact]
    public void TrainingRefusesANoteWithTrainingNotAllowed()
    {
        Set("train", Note("n1", "train", allowed: false));
        Assert.Equal("L10", Refusal(LoadPurpose.Train, "train"));
    }

    [Fact]
    public void TrainingRefusesANoteAClaudeModelWrote()
    {
        Set("train", Note("n1", "train", writer: "anthropic"));
        Assert.Equal("L11", Refusal(LoadPurpose.Train, "train"));
    }

    [Fact]
    public void TrainingRefusesANoteWithoutItsWriterOrItsFilter()
    {
        Set("train", Note("n1", "train", filter: false));
        Assert.Equal("L12", Refusal(LoadPurpose.Train, "train"));
    }

    [Fact]
    public void TrainingRefusesATestNote()
    {
        Set("train", Note("n1", "train"));
        File.WriteAllText(TestIds, "n9\nn1\n");
        Assert.Equal("L13", Refusal(LoadPurpose.Train, "train"));
    }

    [Fact]
    public void TrainingRefusesMoreThanAQuarterComposed()
    {
        Set("train", Note("n1", "train"), Note("n2", "train"), Note("n3", "train"));
        Set("composed", Note("c1", "composed", writer: "code", composed: true), Note("c2", "composed", writer: "code", composed: true));
        Assert.Equal("L14", Refusal(LoadPurpose.Train, "train", "composed"));
        Set("composed", Note("c1", "composed", writer: "code", composed: true));
        Assert.Equal(4, Load(LoadPurpose.Train, "train", "composed").Notes.Count);  // one in four is allowed
    }

    [Fact]
    public void TrainingRefusesAScenarioFamilyOnBothSidesOfASplit()
    {
        Set("train", Note("n1", "train", family: "f-1"));
        Set("dev", Note("d1", "dev", family: "f-1"));
        Assert.Equal("L15", Refusal(LoadPurpose.Train, "train"));
    }

    [Fact]
    public void TuningTakesTheCodexHalfAndRefusesAClaudeWrittenNote()
    {
        Set("dev", Note("d1", "dev"), Note("d2", "dev", writer: "anthropic"));
        Assert.Equal(["d1"], Load(LoadPurpose.Tune, "dev:codex").Notes.Select(note => note["id"]!.GetValue<string>()));
        Assert.Equal("L20", Refusal(LoadPurpose.Tune, "dev"));
    }

    [Theory]
    [InlineData(LoadPurpose.Tune, "train")]
    [InlineData(LoadPurpose.Tune, "mirror")]
    [InlineData(LoadPurpose.Measure, "train")]
    [InlineData(LoadPurpose.Train, "dev")]
    public void ASetIsReadOnlyForItsPurposes(LoadPurpose purpose, string set) => Assert.Equal("L03", Refusal(purpose, set));

    [Theory]
    [InlineData(LoadPurpose.Train)]
    [InlineData(LoadPurpose.Tune)]
    [InlineData(LoadPurpose.Measure)]
    public void HiddenV3IsNeverRead(LoadPurpose purpose) => Assert.Equal("L02", Refusal(purpose, "hiddenV3"));

    [Fact]
    public void TheMirrorSetIsMeasuredOnlyInARunRecordedAsStarted()
    {
        Set("mirror", Note("m1", "mirror", writer: "anthropic"));
        Assert.Equal("L05", Refusal(LoadPurpose.Measure, "mirror"));
        File.WriteAllText(MirrorRuns, "| Run | State |\n|---|---|\n| 1 | started 2026-12-10 |\n");
        Assert.Equal("L05", Refusal(new LoadRequest { Purpose = LoadPurpose.Measure, Sets = ["mirror"], MirrorRun = 2 }));
        Assert.Single(Loader.Load(new LoadRequest { Purpose = LoadPurpose.Measure, Sets = ["mirror"], MirrorRun = 1 }).Notes);
    }

    [Fact]
    public void ThePilotIsTrainedOnOnlyUnderItsRule()
    {
        Set("pilot", Note("p1", "pilot"));
        Assert.Equal("L04", Refusal(LoadPurpose.Train, "pilot"));
        Assert.Single(Loader.Load(new LoadRequest { Purpose = LoadPurpose.Train, Sets = ["pilot"], AllowPilot = true }).Notes);
    }

    [Fact]
    public void APathUnderTheHiddenFoldersIsNeverRead() =>
        Assert.Equal("L01", Assert.Throws<LoaderRefusalException>(() => new NotesLoader(@"C:\dz-hidden-v3\notes", TestIds).Load(
            new LoadRequest { Purpose = LoadPurpose.Measure, Sets = ["dev"] })).Code);

    [Fact]
    public void ACleanSplitIsReadByItsName()
    {
        Set("clean", Note("c1", "clean", writer: "local", split: "train"), Note("c2", "clean", writer: "local", split: "dev"),
            Note("c3", "clean", writer: "local", split: "test"));
        Assert.Equal(["c1"], Load(LoadPurpose.Train, "clean:train").Notes.Select(note => note["id"]!.GetValue<string>()));
        Assert.Equal(["c2"], Load(LoadPurpose.Tune, "clean:dev").Notes.Select(note => note["id"]!.GetValue<string>()));
        Assert.Equal("L03", Refusal(LoadPurpose.Tune, "clean:test"));
    }

    [Fact]
    public void APacksNotesComeOnlyWhenThePackIsNamed()
    {
        Set("train", Note("n1", "train"), Note("k1", "train", pack: "debt_due_date"));
        Assert.Single(Load(LoadPurpose.Train, "train").Notes);
        Assert.Equal(2, Loader.Load(new LoadRequest { Purpose = LoadPurpose.Train, Sets = ["train"], Pack = "debt_due_date" }).Notes.Count);
    }

    [Fact]
    public void AWritersNotesCanBeRemovedFromTheMix()
    {
        Set("train", Note("n1", "train"), Note("n2", "train", writer: "local"));
        var mix = Loader.Load(new LoadRequest { Purpose = LoadPurpose.Train, Sets = ["train"], ExcludeWriter = "openai" });
        Assert.Equal(["n2"], mix.Notes.Select(note => note["id"]!.GetValue<string>()));
        Assert.Equal(1, mix.Manifest()["perWriter"]!["local"]!.GetValue<int>());
    }

    [Fact]
    public void SubsetsNestAndDependOnlyOnTheSeedAndTheIds()
    {
        Set("train", [.. Enumerable.Range(1, 40).Select(n => Note($"n{n}", "train"))]);
        IReadOnlyList<string> Ids(int size, int seed = 7) => [.. Loader.Load(new LoadRequest { Purpose = LoadPurpose.Train, Sets = ["train"], Size = size, SubsetSeed = seed })
            .Notes.Select(note => note["id"]!.GetValue<string>())];
        Assert.Equal(Ids(10), Ids(20).Take(10));
        Assert.Equal(Ids(20), Ids(20));
        Assert.NotEqual(Ids(20), Ids(20, seed: 8));
    }
}
