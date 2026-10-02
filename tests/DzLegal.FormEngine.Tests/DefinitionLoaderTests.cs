using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using static DzLegal.FormEngine.Tests.TestData;

namespace DzLegal.FormEngine.Tests;

public class DefinitionLoaderTests
{
    [Fact]
    public void ShippedDefinitionsLoadAndValidate()
    {
        var all = DefinitionLoader.LoadAll(DocumentTypesRoot);
        Assert.Contains(all, d => d.Id == "requete-introductive");
    }

    [Fact]
    public void RequeteCoversEveryArticle15Item()
    {
        var articles = Requete.Elements.Select(e => e.Article).ToHashSet();
        foreach (var item in new[] { "15-1", "15-2", "15-3", "15-4", "15-5", "15-6" })
        {
            Assert.Contains(articles, a => a.Contains($"art. {item}", StringComparison.Ordinal));
        }

        Assert.Contains(Requete.Elements, e => e.Article == "CPCA art. 14");
        Assert.Contains(Requete.Elements, e => e.Article == "CPCA art. 17");
        Assert.Contains(Requete.Elements, e => e.Article == "CPCA art. 8");
    }

    [Fact]
    public void EveryElementHasBilingualTextAndASourceNote()
    {
        foreach (var e in Requete.Elements)
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Label.Ar), e.Id);
            Assert.False(string.IsNullOrWhiteSpace(e.Label.En), e.Id);
            Assert.False(string.IsNullOrWhiteSpace(e.MissingMessage.Ar), e.Id);
            Assert.False(string.IsNullOrWhiteSpace(e.MissingMessage.En), e.Id);
            Assert.False(string.IsNullOrWhiteSpace(e.SourceNote), e.Id);
        }
    }

    [Fact]
    public void ValidationReportsBrokenReferences()
    {
        var bad = Tiny(Element(new AllOfCheck([
            new FieldsPresentCheck(["nope"]),
            new SectionNotEmptyCheck("missing-section"),
            new SectionMatchesCheck("s1", "(unclosed"),
        ]), new Condition { Field = "ghost", Is = "true" }));

        var errors = DefinitionLoader.Validate(bad);

        Assert.Contains(errors, e => e.Contains("unknown field 'nope'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("unknown section 'missing-section'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("invalid pattern", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("unknown field 'ghost'", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidationReportsDuplicateIds()
    {
        var definition = Tiny(Element(new FieldsPresentCheck(["a"])), Element(new FieldsPresentCheck(["b"])));
        Assert.Contains(DefinitionLoader.Validate(definition), e => e.Contains("Duplicate element id 'e'", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadFailsLoudlyOnInvalidData()
    {
        var dir = Directory.CreateTempSubdirectory("dzlegal-def-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "definition.json"), """
                { "id": "x", "version": "1", "name": { "ar": "س", "en": "x" },
                  "sections": [ { "id": "s", "kind": "generated", "template": "missing.scriban", "title": { "ar": "س", "en": "s" } } ] }
                """);
            var ex = Assert.Throws<DefinitionValidationException>(() => DefinitionLoader.Load(dir.FullName));
            Assert.Contains(ex.Errors, e => e.Contains("missing.scriban", StringComparison.Ordinal));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ChoiceFieldsNeedOptionsAndAValidDefault()
    {
        var definition = Tiny() with
        {
            Fields =
            [
                new FieldDefinition { Id = "one", Label = new("س", "x"), Kind = FieldKind.Choice, Options = [new FieldOption("a", new("أ", "a"))] },
                new FieldDefinition
                {
                    Id = "bad", Label = new("س", "x"), Kind = FieldKind.Choice, Default = "z",
                    Options = [new FieldOption("a", new("أ", "a")), new FieldOption("b", new("ب", "b"))],
                },
            ],
        };

        var errors = DefinitionLoader.Validate(definition);

        Assert.Contains(errors, e => e.Contains("'one' needs at least two options", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("'bad' has a default that is not one of its options", StringComparison.Ordinal));
    }

    [Fact]
    public void SectionHintsAreValidated()
    {
        var definition = Tiny() with
        {
            Sections =
            [
                new SectionDefinition
                {
                    Id = "s1", Title = new("1", "1"),
                    Hints = [new SectionHint { When = [new Condition { Field = "ghost", Is = "" }], Message = new("تنبيه", "") }],
                },
            ],
        };

        var errors = DefinitionLoader.Validate(definition);

        Assert.Contains(errors, e => e.Contains("hint: condition references unknown field 'ghost'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("every hint needs an Arabic and an English message", StringComparison.Ordinal));
    }

    [Fact]
    public void CorrectionsNeedSomethingToFind()
    {
        var definition = Tiny() with { Corrections = [new TextCorrection(" ", "x")], GroundedTerms = ["عقد البيع", " "] };
        var errors = DefinitionLoader.Validate(definition);
        Assert.Contains(errors, e => e.Contains("non-empty 'find'", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("groundedTerms must not contain empty entries", StringComparison.Ordinal));
        Assert.NotEmpty(Requete.Corrections);
        Assert.Contains("عقد المقاولة", Requete.GroundedTerms);
    }

    [Fact]
    public void ShippedGenderFieldsAreChoices() =>
        Assert.All(new[] { "plaintiff.gender", "defendant.gender", "lawyer.gender" }, id =>
        {
            var field = Requete.FindField(id)!;
            Assert.Equal(FieldKind.Choice, field.Kind);
            Assert.Equal(["male", "female"], field.Options.Select(o => o.Value));
        });

    [Fact]
    public void ConditionTreatsMissingValuesAsEmpty()
    {
        var values = new Dictionary<string, string>();
        Assert.True(new Condition { Field = "f", IsNot = "true" }.Evaluate(values));
        Assert.False(new Condition { Field = "f", Is = "true" }.Evaluate(values));
        Assert.True(new Condition { Field = "f", Is = "TRUE" }.Evaluate(new Dictionary<string, string> { ["f"] = " true " }));
    }
}
