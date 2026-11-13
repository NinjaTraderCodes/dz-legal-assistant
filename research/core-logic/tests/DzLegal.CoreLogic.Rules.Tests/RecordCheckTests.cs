using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts.Tests;
using DzLegal.CoreLogic.Rules.Checks;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// Each check of <c>rules check</c> catches the error it exists for. Every test plants one error in a copy of a handbook
/// example (which passes clean) and expects the check that owns it to name it.
/// </summary>
public sealed class RecordCheckTests
{
    [Fact]
    public void BothExamplesPassAndSoDoTheirDerivationStageCopies()
    {
        Assert.Empty(Check(Build.ModuleJson).Problems);
        Assert.Empty(Check(Build.ClaimJson).Problems);
        Assert.Empty(Check(Build.AsDerivation(Build.ModuleJson)).Problems);
        Assert.Empty(Check(Build.AsDerivation(Build.ClaimJson)).Problems);
    }

    [Fact]
    public void AFieldTheSchemaAndTheContractDoNotKnowIsRefusedByBoth()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root => root.Item("deadlines", "d1")["colour"] = "red"));
        Assert.NotEmpty(result.Of(RecordCheck.Schema));
        Assert.NotEmpty(result.Of(RecordCheck.Contract));
        Assert.Null(result.Record);
    }

    [Fact]
    public void AnIdUsedTwiceAndAReferenceToNothingAreRefused()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("basis", "b2")["id"] = "b1";
            root.Item("deadlines", "d1")["basis"]!.AsArray().Add("b99");
            root["checklist"]![0]!["refs"]!.AsArray().Add("p9");
            root["examples"]![0]!["expect"]!["findings"]![0]!["ref"] = "b7";
        }));

        var problems = result.Of(RecordCheck.References).Select(problem => problem.Message).ToList();
        Assert.Contains(problems, message => message.Contains("the item id b1 is used 2 times", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("b99 names no statement", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("p9 names no item", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("b7 is not a deadline, prior step, exit, document or boundary", StringComparison.Ordinal));
    }

    [Fact]
    public void AClaimNotYetFlattenedResolvesWhatItImportsAgainstTheModulesFinalRecord()
    {
        // Take the module's deadline out of the claim: the claim's references to it now resolve through the import.
        var json = Build.Edit(Build.ClaimJson, root =>
        {
            root["deadlines"]!.AsArray().Remove(root.Item("deadlines", "labour-prelitigation.d1"));
            foreach (var example in root["examples"]!.AsArray())
            {
                var findings = example!["expect"]!["findings"]!.AsArray();
                foreach (var finding in findings.Where(finding => finding!["ref"]!.GetValue<string>() == "labour-prelitigation.d1").ToList())
                {
                    findings.Remove(finding);
                }
            }
        });

        Assert.Empty(Check(json).Of(RecordCheck.References));

        var withoutModule = RecordChecks.Check(json, HandbookFiles.CheckContext() with { Module = _ => null });
        Assert.Contains(withoutModule.Of(RecordCheck.References), problem => problem.Message.Contains("rules/final/labour-prelitigation.json is not available", StringComparison.Ordinal));

        var wrongImport = Build.Edit(json, root => root["checklist"]![1]!["refs"]!.AsArray().Add("labour-prelitigation.t9"));
        Assert.Contains(Check(wrongImport).Of(RecordCheck.References), problem => problem.Message.Contains("does not import t9", StringComparison.Ordinal));
    }

    [Fact]
    public void ASlotMustBeDeclaredAndExistAndFitItsUse()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root["slots"]!.AsArray().Remove(root["slots"]!.AsArray().Single(slot => slot!.GetValue<string>() == "worker_is_public_servant"));
            root["slots"]!.AsArray().Add("made_up_slot");
            root.Item("deadlines", "d1")["startsFrom"]!["slot"] = "nonconciliation_record";
            root["examples"]![0]!["slots"]!["nonconciliation_record"] = "maybe";
        }));

        var problems = result.Of(RecordCheck.Slots).Select(problem => problem.Message).ToList();
        Assert.Contains(problems, message => message.Contains("the slot worker_is_public_servant is not in the record's slots", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("made_up_slot is neither in slots.json nor in the record's proposedSlots", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("deadlines[0].startsFrom: the slot nonconciliation_record is a YesNoUnknown slot; this use needs a Date slot", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("\"maybe\" is not a value", StringComparison.Ordinal));
    }

    [Fact]
    public void AProposedSlotIsAcceptedBeforeTheFinalStageOnly()
    {
        var proposed = new JsonObject
        {
            ["id"] = "new_fact",
            ["type"] = "yesNoUnknown",
            ["question"] = new JsonObject { ["ar"] = "؟", ["en"] = "?" },
            ["why"] = "a test",
        };

        string Plant(string json) => Build.Edit(json, root =>
        {
            root["slots"]!.AsArray().Add("new_fact");
            root["proposedSlots"] = new JsonArray(proposed.DeepClone());
            root.Item("boundaries", "n1")["when"] = new JsonObject { ["slot"] = "new_fact", ["is"] = "yes" };
        });

        Assert.Empty(Check(Plant(Build.AsDerivation(Build.ModuleJson))).Problems);
        Assert.Contains(Check(Plant(Build.ModuleJson)).Of(RecordCheck.Slots), problem => problem.Message.Contains("a final record proposes no slot", StringComparison.Ordinal));
    }

    [Fact]
    public void AnEventARequestKindOrAClaimIdThatDoesNotExistIsRefused()
    {
        var result = Check(Build.Edit(Build.ClaimJson, root =>
        {
            root.Item("elements", "e1")["events"]!.AsArray().Add("made_up_event");
            root.Item("requests", "r2")["kind"] = "made_up_request";
            root.Item("boundaries", "n1")["isActually"] = "claim:made-up-claim";
        }));

        var problems = result.Of(RecordCheck.Vocabulary).Select(problem => problem.Message).ToList();
        Assert.Contains(problems, message => message.Contains("made_up_event is not an event", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("made_up_request is not a request kind", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("made-up-claim is not a claim id", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("limitation")]
    [InlineData("interpretiveStep")]
    [InlineData("noCounting")]
    [InlineData("interpretiveStatement")]
    public void OnlyAFirmForfeitureOrAPriorStepMayDeclareABlock(string plant)
    {
        var json = plant switch
        {
            "limitation" => Build.Edit(Build.ClaimJson, root => root.Item("deadlines", "d1")["effectWhenExpired"] = "blockCleanExport"),
            "interpretiveStep" => Build.Edit(Build.ModuleJson, root => root.Item("priorSteps", "p1")["effectWhenMissing"] = "blockCleanExport"),
            "noCounting" => Build.Edit(Build.ModuleJson, root => root.Item("deadlines", "d1").Remove("counting")),
            _ => Build.Edit(Build.ModuleJson, root => root.Item("basis", "b7")["interpretive"] = true),
        };

        Assert.Single(Check(json).Of(RecordCheck.Effects));
    }

    [Fact]
    public void AConditionWithTwoShapesIsRefused()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("deadlines", "d1")["when"]!["in"] = new JsonArray("yes");
            root.Item("priorSteps", "p1")["requiredWhen"]!["all"] = new JsonArray(new JsonObject { ["slot"] = "nonconciliation_record", ["is"] = "yes" });
        }));

        Assert.Equal(2, result.Of(RecordCheck.Conditions).Count());
    }

    [Fact]
    public void AStepWithoutItsOwnSlotOrAPeriodBelowOneIsRefused()
    {
        var result = Check(Build.AsDerivation(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("priorSteps", "p1")["slots"] = new JsonArray();
            root.Item("deadlines", "d1")["period"]!["value"] = 0;
        })));

        Assert.Equal(2, result.Of(RecordCheck.Structure).Count());
    }

    [Fact]
    public void AnAgentsFileCarriesNoScriptFieldAndAQuotedCopyMayCarryQuotes()
    {
        var quoted = Build.Edit(Build.ModuleJson, root =>
        {
            root["stage"] = "reconciled";
            foreach (var node in Walk(root).OfType<JsonObject>())
            {
                foreach (var name in new[] { "grading", "shownEffect", "agreementBeforeReconciliation" })
                {
                    node.Remove(name);
                }

                if (node.ContainsKey("ref"))
                {
                    node.Remove("toCheck");
                }
            }
        });

        Assert.Equal(28, Check(quoted).Of(RecordCheck.ScriptFields).Count()); // 14 statements, a quote and a source in each
        Assert.Empty(RecordChecks.Check(quoted, HandbookFiles.CheckContext(quoted: true)).Problems);

        var graded = Build.Edit(Build.AsDerivation(Build.ModuleJson), root => root.Item("deadlines", "d1")["grading"] = new JsonObject { ["grade"] = "A", ["reasons"] = new JsonArray(), ["toCheck"] = false });
        Assert.Single(RecordChecks.Check(graded, HandbookFiles.CheckContext(quoted: true)).Of(RecordCheck.ScriptFields));
    }

    [Fact]
    public void AFinalRecordCarriesEveryScriptField()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("basis", "b1").Remove("grading");
            root.Item("documents", "doc1").Remove("grading");
            root["examples"]![0]!["expect"]!["findings"]![0]!.AsObject().Remove("shownEffect");
        }));

        Assert.Equal(3, result.Of(RecordCheck.ScriptFields).Count());
    }

    [Fact]
    public void AClaimCarriesEveryElementAndRequestKindOrAChecklistLineSaysWhyNot()
    {
        var withoutDamages = Build.Edit(Build.ClaimJson, root =>
        {
            root["requests"]!.AsArray().Remove(root.Item("requests", "r2"));
            root.Item("openPoints", "o3")["refs"] = new JsonArray("b4");
        });
        Assert.Contains(Check(withoutDamages).Of(RecordCheck.ClaimElements), problem => problem.Message.Contains("request kind damages", StringComparison.Ordinal));

        var explained = Build.Edit(withoutDamages, root => root["checklist"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "k9",
            ["item"] = "Is a request of kind damages usual for this claim?",
            ["from"] = new JsonArray("catalogue"),
            ["answer"] = "doesNotApply",
            ["note"] = "A test line.",
        }));
        Assert.Empty(Check(explained).Problems);
    }

    [Fact]
    public void WithTheStoreEveryCiteMustBeThereInForceWithItsParagraphs()
    {
        var module = Build.Load(Build.ModuleJson);
        JsonObject Entry(string code, string article, int paragraphs, bool repealed = false, string version = "v1") => new()
        {
            ["code"] = code,
            ["article"] = article,
            ["version"] = version,
            ["inForce"] = !repealed,
            ["repealed"] = repealed,
            ["paragraphs"] = new JsonArray([.. Enumerable.Range(1, paragraphs).Select(index => (JsonNode)$"¶{index}")]),
        };

        var entries = module.Basis
            .GroupBy(statement => (statement.Cite.Code, statement.Cite.Article))
            .Select(group => Entry(group.Key.Code, group.Key.Article, Math.Max(1, group.SelectMany(statement => statement.Cite.Paragraphs).DefaultIfEmpty(1).Max())))
            .ToList();
        string Store(IEnumerable<JsonObject> list) => new JsonObject { ["schemaVersion"] = "1.0", ["inputs"] = new JsonObject(), ["entries"] = new JsonArray([.. list.Select(entry => entry.DeepClone())]) }.ToJsonString();

        Assert.Empty(RecordChecks.Check(Build.ModuleJson, HandbookFiles.CheckContext(store: ArticleStoreIndex.Parse(Store(entries)))).Of(RecordCheck.Store));

        var planted = entries
            .Where(entry => entry["article"]!.GetValue<string>() != "504")
            .Select(entry => entry["article"]!.GetValue<string>() switch
            {
                "67" => Entry("cpca", "67", 1, repealed: true),
                "405" => Entry("cpca", "405", 3),
                _ => entry,
            });
        var withVersion = Build.Edit(Build.ModuleJson, root => root.Item("basis", "b13")["cite"]!["version"] = "law-99-99");

        var problems = RecordChecks.Check(withVersion, HandbookFiles.CheckContext(store: ArticleStoreIndex.Parse(Store(planted)))).Of(RecordCheck.Store).Select(problem => problem.Message).ToList();
        Assert.Equal(4, problems.Count);
        Assert.Contains(problems, message => message.Contains("cpca 504 is not in the store", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("cpca 67 is repealed", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("paragraph 4 does not exist (the article has 3)", StringComparison.Ordinal));
        Assert.Contains(problems, message => message.Contains("cpca 69 version law-99-99 is not in the store", StringComparison.Ordinal));
    }

    [Fact]
    public void AQuoteThatWasTouchedAndAnExtractThatIsNotInItAreRefused()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("basis", "b1")["quote"]![0]!["text"] = "نص آخر";
            root.Item("deadlines", "d1")["extract"]!["period"] = "سبعة (7) أشهر";
        }));

        Assert.Equal(2, result.Of(RecordCheck.Quotes).Count());
    }

    [Fact]
    public void TheMarkIsGradeDExactlyAndAShownEffectFollowsTheGradeTable()
    {
        var result = Check(Build.Edit(Build.ModuleJson, root =>
        {
            root.Item("deadlines", "d1")["grading"]!["toCheck"] = false;
            root["examples"]![1]!["expect"]!["findings"]![0]!["shownEffect"] = "blockCleanExport";
        }));

        Assert.Equal(2, result.Of(RecordCheck.Grades).Count());
    }

    private static RecordCheckResult Check(string json) => RecordChecks.Check(json, HandbookFiles.CheckContext());

    private static IEnumerable<JsonNode> Walk(JsonNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        yield return node;
        var children = node switch
        {
            JsonObject obj => obj.Select(member => member.Value),
            JsonArray array => array,
            _ => [],
        };

        foreach (var child in children.ToList())
        {
            foreach (var descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }
}
