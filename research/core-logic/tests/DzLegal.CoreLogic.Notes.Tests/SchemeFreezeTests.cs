namespace DzLegal.CoreLogic.Notes.Tests;

/// <summary>Phase A, step 1 (specs/notes-pipeline.md, section 1): the guide test's items, its comparison, the cells table.</summary>
public sealed class SchemeFreezeTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "notes-tests-" + Guid.NewGuid().ToString("N"));

    public SchemeFreezeTests() => Directory.CreateDirectory(folder);

    /// <summary>The real repository, found above the test's own folder (read only).</summary>
    private static NotesFiles Repo => NotesFiles.Find(null);

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private string Write(string name, string text)
    {
        var path = Path.Combine(folder, name);
        File.WriteAllText(path, text);
        return path;
    }

    private static GuideItem Item(int id, string expected) => new(id, "loan-repayment", "event:payment_made", "نص", expected, "rule 3");

    [Fact]
    public void IngestingNewItemsAddsLinesOnlyWithTheNextIdsAndReturnsTheProblems()
    {
        var before = File.ReadAllText(Repo.GuideTest).ReplaceLineEndings("\n");
        var path = Write("guide-test.json", before);
        var reply = Write("reply.json", """
            { "items": [ { "claim": "unpaid-rent", "ask": "slot:notice_served", "text": "لم يوجه أي إعذار.", "expected": "no", "tests": "6" },
                         { "claim": "unpaid-rent", "ask": "value", "text": "في 03/04/2025.", "expected": "2025-04-03", "tests": "8" } ],
              "problems": [ "case 30 allows two readings" ] }
            """);

        var (added, problems) = GuideTest.Ingest(path, reply);

        var last = GuideTest.Load(Repo.GuideTest).Max(item => item.Id);
        Assert.Equal([last + 1, last + 2], added.Select(item => item.Id));
        Assert.Equal(["case 30 allows two readings"], problems);
        var after = File.ReadAllText(path);
        var removed = before.Split('\n').Except(after.Split('\n'), StringComparer.Ordinal).ToList();
        Assert.Single(removed);  // only the old last item, which gains its comma
        Assert.Equal(removed[0] + ",", after.Split('\n').Single(line => line == removed[0] + ","));
        Assert.Equal(last + 2, GuideTest.Load(path).Count);  // still valid JSON, every item read back
        Assert.Contains("لم يوجه أي إعذار.", after, StringComparison.Ordinal);  // Arabic kept readable
    }

    [Fact]
    public void AnItemWhoseAskHasNoKnownFormIsRefused()
    {
        var path = Write("guide-test.json", File.ReadAllText(Repo.GuideTest));
        var reply = Write("reply.json", """{ "items": [ { "claim": "x", "ask": "polarity", "text": "t", "expected": "e", "tests": "t" } ], "problems": [] }""");
        Assert.Throws<InvalidDataException>(() => GuideTest.Ingest(path, reply));
    }

    [Fact]
    public void TheTestersPromptHoldsEveryItemButNeverAKey()
    {
        var prompt = GuideTest.PrepareTest(Repo);
        var items = GuideTest.Load(Repo.GuideTest);
        Assert.All(items, item => Assert.Contains($"\"id\":{item.Id},", prompt, StringComparison.Ordinal));
        Assert.DoesNotContain("\"expected\"", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("\"tests\"", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void TheComparisonListsEveryItemWhereTheThreeAnswersAreNotEqualAndTheUnsureOnes()
    {
        GuideItem[] items = [Item(1, "denied"), Item(2, "stated"), Item(3, "notMentioned"), Item(4, "stated")];
        var sonnet = new Dictionary<int, GuideAnswer>
        {
            [1] = new(1, "denied", "rule 3", false), [2] = new(2, "stated", "rule 1", false),
            [3] = new(3, "not mentioned", "case 27", true), [4] = new(4, "stated", "rule 1", false),
        };
        var opus = new Dictionary<int, GuideAnswer>
        {
            [1] = new(1, "Denied", "rule 3", false), [2] = new(2, "disputed", "rule 2", false), [3] = new(3, "notMentioned", "case 27", false),
        };

        var section = GuideTest.Compare(items, sonnet, opus, "2026-11-16");

        Assert.Contains("| Expected answer given | 4 of 4 | 2 of 4 |", section, StringComparison.Ordinal);
        Assert.Contains("### Items where the three answers are not equal (2)", section, StringComparison.Ordinal);
        Assert.Contains("| 2 |", section, StringComparison.Ordinal);
        Assert.Contains("| 4 | `loan-repayment`, `event:payment_made` | stated (rule 3) | stated: rule 1 | no answer |", section, StringComparison.Ordinal);
        Assert.Contains("### Items answered as expected but marked unsure (1)", section, StringComparison.Ordinal);  // item 3
        Assert.DoesNotContain("| 1 |", section, StringComparison.Ordinal);  // case and spacing do not make a difference
    }

    [Fact]
    public void ThereIsOneCellItemPerElementWithTheClaimTypesArabicLabel()
    {
        var items = CellsTable.Build(Repo);
        var elements = NotesJson.Load(Repo.ClaimElements)["claims"]!.AsArray().Sum(claim => claim!["elements"]!.AsArray().Count);
        Assert.Equal(elements, items.Count);
        Assert.Equal(Enumerable.Range(CellsTable.FirstId, items.Count), items.Select(item => item.Id));
        Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.ClaimLabel)));
        var prompt = CellsTable.Prepare(Repo, items);
        Assert.Contains($"- {CellsTable.FirstId}. The claim: ", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", prompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("no", "no", false)]
    [InlineData("no", "yes", true)]
    [InlineData("yes", "no", true)]
    [InlineData(null, "no", true)]
    public void ADeniedCellIsNotDrawnOnlyWhenBothAnswerNo(string? sonnet, string? opus, bool drawn) =>
        Assert.Equal(drawn, CellsTable.Drawn(sonnet, opus));

    [Fact]
    public void TheTableReplacesItsRowsAndNothingElse()
    {
        var scheme = File.ReadAllText(Repo.Scheme).ReplaceLineEndings("\n");
        CellItem[] items = [new(1001, "loan-repayment", "money_lent", "استرداد قرض", "سلّم"), new(1002, "loan-repayment", "debt_unpaid", "استرداد قرض", "لم يرد")];
        var written = CellsTable.WriteTable(scheme, items, new Dictionary<int, string> { [1001] = "no", [1002] = "no" },
            new Dictionary<int, string> { [1001] = "no", [1002] = "yes" });

        Assert.Contains("| `loan-repayment` | `money_lent` | no | no | no |\n| `loan-repayment` | `debt_unpaid` | yes | no | yes |\n", written, StringComparison.Ordinal);
        Assert.DoesNotContain("*(75 rows", written, StringComparison.Ordinal);
        var head = scheme.IndexOf(CellsTable.TableHead, StringComparison.Ordinal);
        Assert.Equal(scheme[..head], written[..head]);
        Assert.EndsWith(scheme[scheme.IndexOf("\n## 9.", StringComparison.Ordinal)..], written, StringComparison.Ordinal);
        Assert.Equal(written, CellsTable.WriteTable(written, items, new Dictionary<int, string> { [1001] = "no", [1002] = "no" },
            new Dictionary<int, string> { [1001] = "no", [1002] = "yes" }));  // written twice: the same file
    }

    [Fact]
    public void AFilledPromptRefusesAPlaceholderLeftEmpty()
    {
        Assert.Throws<KeyNotFoundException>(() => PromptTemplate.Fill("a {{one}} and {{two}}", new Dictionary<string, string> { ["one"] = "1" }));
        Assert.Throws<InvalidDataException>(() => PromptTemplate.Fill("a {{one}}", new Dictionary<string, string> { ["one"] = "{{two}}" }));
        Assert.Equal("a 1", PromptTemplate.Fill("a {{one}}", new Dictionary<string, string> { ["one"] = "1" }));
    }
}
