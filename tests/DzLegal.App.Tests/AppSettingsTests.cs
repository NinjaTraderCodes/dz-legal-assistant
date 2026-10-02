namespace DzLegal.App.Tests;

public sealed class AppSettingsTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("dzlegal-settings-");

    private string Shipped => Path.Combine(_dir.FullName, "appsettings.json");

    private string Local => Path.Combine(_dir.FullName, AppSettings.LocalFileName);

    [Fact]
    public void ShippedDefaultsAreUsedWithoutALocalFile()
    {
        File.WriteAllText(Shipped, """{ /* comment */ "language": "en", "ollama": { "model": "gemma3:4b", "timeoutMinutes": 7 } }""");

        var settings = AppSettings.Load(Shipped);

        Assert.Equal("en", settings.Language);
        Assert.Equal("gemma3:4b", settings.Ollama.Model);
        Assert.Equal(7, settings.Ollama.TimeoutMinutes);
    }

    [Fact]
    public void LocalFileOverridesOnlyTheKeysItContains()
    {
        File.WriteAllText(Shipped, """{ "defaultProvider": "ollama", "ollama": { "model": "gemma3:4b", "timeoutMinutes": 7 } }""");
        File.WriteAllText(Local, """{ "ollama": { "model": "qwen2.5:7b-instruct" } }""");

        var settings = AppSettings.Load(Shipped);

        Assert.Equal("qwen2.5:7b-instruct", settings.Ollama.Model);
        Assert.Equal(7, settings.Ollama.TimeoutMinutes);
        Assert.Equal("ollama", settings.DefaultProvider);
        Assert.Equal("http://127.0.0.1:11434", settings.Ollama.Endpoint);
    }

    [Fact]
    public void ShippedFileDefaultsToGemma()
    {
        var shipped = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Assert.Equal("gemma3:4b", AppSettings.Load(shipped).Ollama.Model);
    }

    public void Dispose() => _dir.Delete(recursive: true);
}
