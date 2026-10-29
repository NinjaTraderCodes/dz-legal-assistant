using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

public sealed class ProfileHookTests
{
    private sealed class FakeProfile : IProfileDefaults
    {
        private readonly IReadOnlyDictionary<string, string> _values;
        private readonly bool _watermarkByDefault;

        public FakeProfile(IReadOnlyDictionary<string, string> values, bool watermarkByDefault)
        {
            _values = values;
            _watermarkByDefault = watermarkByDefault;
        }

        public IReadOnlyDictionary<string, string> Values => _values;

        public bool WatermarkByDefault => _watermarkByDefault;
    }

    private static MainWindowViewModel Create(IProfileDefaults? profileDefaults = null) =>
        new(
            definition: DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive")),
            providers: new List<ProviderOption>
            {
                new ProviderOption("stub", l => l["ProviderStub"], () => new StubModelProvider()),
            },
            files: new FakeFiles(),
            evaluations: new EvaluationStore(Vm.EvaluationsFolder),
            loc: new Loc(),
            defaultProvider: "stub",
            today: () => new DateOnly(2026, 9, 15),
            knowledge: Vm.ShippedKnowledge,
            profileDefaults: profileDefaults);

    private static readonly Dictionary<string, string> ProfileValues = new(StringComparer.Ordinal)
    {
        ["lawyer.name"] = "ع. خليل",
        ["lawyer.gender"] = "male",
        ["lawyer.bar"] = "avocat à la Cour d'Alger",
        ["lawyer.address"] = "12, rue des Lilas, Alger",
        ["document.place"] = "الجزائر",
        ["court.council"] = "الجزائر",
        ["court.name"] = "محكمة سيدي امحمد",
    };

    private static readonly FakeProfile Profile = new(ProfileValues, watermarkByDefault: false);

    [Fact]
    public void NewCaseWithoutProfileKeepsTheEmptyDefaults()
    {
        var vm = Create();

        Assert.False(vm.HasUnsavedChanges);
        Assert.True(vm.DraftWatermark);
        Assert.Equal(string.Empty, vm.Field("court.name").Value);
    }

    [Fact]
    public void NewCaseFromProfilePreFillsTheEmptyCase()
    {
        var vm = Create(Profile);

        Assert.Equal(ProfileValues["lawyer.name"], vm.Field("lawyer.name").Value);
        Assert.Equal(ProfileValues["lawyer.gender"], vm.Field("lawyer.gender").Value);
        Assert.Equal(ProfileValues["lawyer.bar"], vm.Field("lawyer.bar").Value);
        Assert.Equal(ProfileValues["lawyer.address"], vm.Field("lawyer.address").Value);
        Assert.Equal(ProfileValues["document.place"], vm.Field("document.place").Value);
        Assert.Equal(ProfileValues["court.council"], vm.Field("court.council").Value);
        Assert.Equal(ProfileValues["court.name"], vm.Field("court.name").Value);
        Assert.False(vm.DraftWatermark);
    }

    [Fact]
    public void NewCaseFromProfileKeepsTheDraftDateToday()
    {
        var vm = Create(Profile);

        Assert.Equal("15/09/2026", vm.Field("document.date").Value);
    }

    [Fact]
    public void NewCommandFromProfilePreFillsAnEmptyCaseWithoutAskingToReplace()
    {
        var vm = Create(Profile);

        vm.NewCommand.Execute(null);

        Assert.False(vm.IsConfirmingReplace);
        Assert.Equal(ProfileValues["lawyer.name"], vm.Field("lawyer.name").Value);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void OpenedCaseInputsAreNotOverwrittenByTheProfile()
    {
        var vm = Create(Profile);
        vm.OpenCase(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["lawyer.name"] = "م. أمين",
            ["court.name"] = "محكمة قضاء",
        });

        Assert.Equal("م. أمين", vm.Field("lawyer.name").Value);
        Assert.Equal("محكمة قضاء", vm.Field("court.name").Value);
    }
}
