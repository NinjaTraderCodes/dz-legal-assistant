using System.Xml.Linq;

namespace DzLegal.App.Tests;

/// <summary>
/// The desktop app runs offline: the hosted providers (web demo, and the temporary online testing build) must stay out
/// of every normal build.
/// </summary>
public class OfflineGuaranteeTests
{
    private static string Src => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));

    [Fact]
    public void TheDesktopAppDoesNotReferenceTheHostedProviders()
    {
        var assemblies = typeof(Composition).Assembly.GetReferencedAssemblies()
            .Concat(typeof(MainWindowViewModel).Assembly.GetReferencedAssemblies())
            .Select(a => a.Name);
        Assert.DoesNotContain("DzLegal.ModelProviders.Hosted", assemblies);
        Assert.DoesNotContain("ModelProviders.Hosted", File.ReadAllText(Path.Combine(Src, "DzLegal.Presentation/DzLegal.Presentation.csproj")), StringComparison.Ordinal);
    }

    [Fact]
    public void TheOnlineEngineIsOnlyInTheTestingBuild()
    {
        // The only reference to the hosted providers sits in an ItemGroup that applies with -p:OnlineTesting=true.
        var project = XDocument.Load(Path.Combine(Src, "DzLegal.App/DzLegal.App.csproj"));
        var references = project.Descendants("ProjectReference")
            .Where(r => ((string?)r.Attribute("Include") ?? string.Empty).Contains("ModelProviders.Hosted", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(references);
        Assert.All(references, r => Assert.Equal("'$(OnlineTesting)' == 'true'", (string?)r.Parent!.Attribute("Condition")));
    }

    [Fact]
    public void ANormalBuildOffersNoEngineThatSendsCaseDataEvenWithAKeySet()
    {
        var previous = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", "test-key");
        try
        {
            var providers = Composition.CreateProviders(new AppSettings());
            Assert.Equal(["ollama", "stub"], providers.Select(p => p.Key));
            Assert.DoesNotContain(providers, p => p.SendsCaseData);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", previous);
        }
    }

    /// <summary>Acts only when the tests run against the testing build (dotnet test -p:OnlineTesting=true).</summary>
    [Fact]
    public async Task InTheTestingBuildTheOnlineEngineAppearsWithAKeyAndSaysCaseDataLeaves()
    {
        if (typeof(Composition).Assembly.GetReferencedAssemblies().All(a => a.Name != "DzLegal.ModelProviders.Hosted"))
        {
            return; // normal build: covered by the tests above
        }

        var previous = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", null);
            Assert.DoesNotContain(Composition.CreateProviders(new AppSettings()), p => p.Key == "online");

            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", "test-key");
            var providers = Composition.CreateProviders(new AppSettings());
            Assert.Equal(["ollama", "online", "stub"], providers.Select(p => p.Key));
            var online = providers[1];
            Assert.True(online.SendsCaseData);
            Assert.Equal("ollama", new AppSettings().DefaultProvider); // never selected unless the tester picks it

            var vm = Vm.Create();
            online.Localize(vm.L);
            Assert.Contains("google/gemma-3-4b-it", online.DisplayName, StringComparison.Ordinal);
            Assert.Contains("(online testing build)", Composition.CreateMainViewModel(new AppSettings(), new FakeFiles(), new DzLegal.Storage.AppPaths(Path.Combine(Path.GetTempPath(), "dzlegal-online-test")), new Loc()).WindowTitle, StringComparison.Ordinal);
            await Task.CompletedTask;
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", previous);
        }
    }

    [Fact]
    public void AnEngineThatSendsCaseDataReplacesTheOfflineChipWithAWarning()
    {
        var online = new ProviderOption("online", _ => "online", () => new DzLegal.ModelProviders.Stub.StubModelProvider(), sendsCaseData: true);
        var vm = Vm.Create();
        Assert.True(vm.IsOfflineEngine);

        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.SelectedProvider = online;
        Assert.False(vm.IsOfflineEngine);
        Assert.Contains(nameof(MainWindowViewModel.IsOfflineEngine), changed);
    }
}
