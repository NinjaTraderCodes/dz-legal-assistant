using System.Text.Json;
using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// The evaluator's counting against what the final <c>periods</c> record established (specs/unit-driver.md, "After periods
/// is final"): its own worked examples pass, and the counting methods the code names as sourced are the ones its deadlines
/// cite with those statements.
/// </summary>
public sealed class FinalPeriodsTests
{
    private static RuleRecord Periods()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, "docs", "research", "core-logic", "rules", "final", "periods.json");
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<RuleRecord>(File.ReadAllText(path), ContractJson.Options)!;
            }
        }

        throw new FileNotFoundException("rules/final/periods.json is not above the test's folder");
    }

    [Fact]
    public void EveryExampleOfTheFinalPeriodsRecordPasses()
    {
        var record = Periods();
        var results = WorkedExamples.Run(new RuleEvaluator(), record);
        Assert.Equal(record.Examples.Count, results.Count);
        Assert.All(results, result => Assert.True(result.Passed, $"{result.CaseId}: {string.Join("; ", result.Differences)}"));
    }

    [Fact]
    public void TheSourcedMethodsAreTheOnesItsDeadlinesCite()
    {
        var record = Periods();
        var cites = record.Basis.ToDictionary(b => b.Id, b => $"{b.Cite.Code} {b.Cite.Article}");
        foreach (var deadline in record.Deadlines)
        {
            var counting = deadline.Counting!;
            var basis = counting.Basis.Select(id => cites[id]).ToHashSet();
            if (counting.Method == CountingMethod.ExcludeFirstDay)
            {
                Assert.False(counting.ExtendToWorkingDay);
                Assert.Contains("civil-code 314", basis);
            }
            else
            {
                Assert.Equal(CountingMethod.ExcludeFirstAndLastDay, counting.Method);
                Assert.True(counting.ExtendToWorkingDay);
                Assert.Contains("cpca 405", basis);
            }
        }

        Assert.Equal(CountingMethod.ExcludeFirstDay, Rules.Periods.MethodWithoutCounting);
    }
}
