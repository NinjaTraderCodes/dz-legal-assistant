using DzLegal.Harness;

namespace DzLegal.Law.Tests;

public sealed class RunEnvironmentTests
{
    [Fact]
    public void TheManifestRecordsThePowerStateAndTheMachineWithoutNamingIt()
    {
        // The hidden run of 30 Oct 2026 was 1.5 times slower than the same laptop the next day, and nothing said why.
        var env = RunEnvironment.Capture(["stub"], null);

        Assert.Contains(env["power"], new[] { "mains", "battery", "unknown" });
        Assert.False(string.IsNullOrWhiteSpace(env["powerScheme"]));
        Assert.True(int.Parse(env["processors"], System.Globalization.CultureInfo.InvariantCulture) > 0);
        Assert.False(string.IsNullOrWhiteSpace(env["os"]));
        Assert.DoesNotContain(env.Values, v => v.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || v.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(env.Keys, k => k.StartsWith("ollama", StringComparison.Ordinal) || k.StartsWith("digest", StringComparison.Ordinal)); // no local model, no server asked
    }

    [Fact]
    public void ALocalModelAsksTheServerAndAnUnreachableServerIsSaidSo()
    {
        // A port nothing listens on: the capture must not wait long nor throw.
        var env = RunEnvironment.Capture(["gemma3:4b"], "http://127.0.0.1:9");
        Assert.Equal("unreachable", env["ollamaVersion"]);
    }

    [Fact]
    public void TheManifestCarriesTheEnvironment()
    {
        var manifest = RunManifest.Create("facts", ["stub"], new Dictionary<string, string>(), new Dictionary<string, string> { ["endpoint"] = "http://127.0.0.1:9" });
        Assert.Equal("facts", manifest.Command);
        Assert.Contains("power", manifest.Environment.Keys);
    }
}
