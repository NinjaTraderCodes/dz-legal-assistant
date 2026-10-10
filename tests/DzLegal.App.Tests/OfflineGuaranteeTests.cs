namespace DzLegal.App.Tests;

/// <summary>The desktop app runs offline: the hosted providers (web demo only) must stay out of it.</summary>
public class OfflineGuaranteeTests
{
    [Fact]
    public void TheDesktopAppDoesNotReferenceTheHostedProviders()
    {
        var assemblies = typeof(Composition).Assembly.GetReferencedAssemblies()
            .Concat(typeof(MainWindowViewModel).Assembly.GetReferencedAssemblies())
            .Select(a => a.Name);
        Assert.DoesNotContain("DzLegal.ModelProviders.Hosted", assemblies);

        // Not even an unused project reference, which would ship the assembly next to the app.
        var src = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));
        foreach (var project in new[] { "DzLegal.App/DzLegal.App.csproj", "DzLegal.Presentation/DzLegal.Presentation.csproj" })
        {
            Assert.DoesNotContain("ModelProviders.Hosted", File.ReadAllText(Path.Combine(src, project)), StringComparison.Ordinal);
        }
    }
}
